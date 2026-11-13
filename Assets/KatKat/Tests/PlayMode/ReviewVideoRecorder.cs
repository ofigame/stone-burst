using System.Collections;
using System.IO;
using KatKat.Core;
using KatKat.Game;
using KatKat.Levels;
using KatKat.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KatKat.Tests
{
    /// <summary>
    /// App Review videosu (2026-11-14, App Store 4.3): gerçek oyun, sabit 30 kare/sn, İngilizce, ilerideki kurallı
    /// bölümler dahil. Test değildir; yalnız elle çalıştırılır:
    /// <c>tools/unity.ps1 test-play -Filter ReviewVideoRecorder</c> → kareler <c>docs/magaza/review/frames/</c>,
    /// video <c>tools/review_video.ps1</c> ile.
    /// </summary>
    [Explicit("Yalnız elle: App Review videosu kaydı")]
    public sealed class ReviewVideoRecorder
    {
        private const int W = 720, H = 1280, Fps = 30;
        private GameController _gc;
        private RenderTexture _rt;
        private Texture2D _read;
        private string _dir;
        private int _frame;
        private TMPro.TextMeshProUGUI _caption;
        private GameObject _band;

        [UnityTest]
        public IEnumerator Record()
        {
            _dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "docs", "magaza", "review", "frames");
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Directory.CreateDirectory(_dir);

            Loc.Set("en", false);
            GameController.ToursOverride = false;
            GameController.CountryOverride = "TR";
            GameController.ClockOverride = new KatKat.Meta.FixedDayClock { DayId = "2026-11-08" };
            GameController.RequestedLevel = -1;
            GameController.SaveDirectoryOverride = Path.Combine(Application.temporaryCachePath, "katkat_review_video");
            if (Directory.Exists(GameController.SaveDirectoryOverride)) Directory.Delete(GameController.SaveDirectoryOverride, true);
            SafeAreaFitter.NormalizedOverride = new Rect(0f, 0f, 1f, 1f);
            UiAspect.Override = (float)W / H;

            var camGo = new GameObject("ReviewCam") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            _rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = _rt;
            cam.aspect = (float)W / H;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            _read = new Texture2D(W, H, TextureFormat.RGB24, false);

            var root = new GameObject("Game");
            _gc = root.AddComponent<GameController>();
            yield return null; // Start → Setup + ana sayfa
            yield return null;
            BuildCaption(cam);
            Time.captureFramerate = Fps;
            try
            {
                // 1) Katmanlar: her hücre bir yığın; uzun basma gizli katmanları gösterir.
                // Sade bölüm (41): kural türü yok; tür bölümünü burada açmak sonraki kural kartını "görüldü" yapıyordu.
                _gc.StartLevel(40);
                _gc.Hud.HideRuleCard(false);
                Caption("Every cell is a stack of up to 4 layers.\nOnly the top layer is visible.");
                yield return Rec(2.2f);
                PeekBiggest(true);
                Caption("Long-press a stack to see its hidden colors.");
                yield return Rec(2.6f);
                PeekBiggest(false);

                // 2) Zincir: üst katman soyulur, alttan çıkan 3+ aynı renk kendiliğinden patlar.
                _gc.StartLevel(1);
                yield return Rec(0.6f);
                _gc.Hud.HideRuleCard(false);
                Caption("Tap 3+ touching stones: one layer peels.\nNewly revealed groups of 3 burst on their own.");
                yield return Rec(1.2f);
                foreach (var step in _gc.CurrentLevel.Solution)
                {
                    if (_gc.Session.State.Status != GameStatus.Idle) break;
                    _gc.PlayStep(step);
                    yield return RecUntilIdle(6f);
                    yield return Rec(0.5f);
                }
                yield return Rec(1.0f);

                // 3) Özel taş tepsisi: Altın Rulo tepside bekler, hamle harcamadan istenince ateşlenir.
                _gc.StartLevel(218);
                _gc.Hud.HideRuleCard(false);
                var st = _gc.Session.State;
                st.SpecialsEnabled = true; // tepsi HUD'da görünsün
                st.Tray.Clear(); st.Tray.Add(SpecialKind.RocketRow); st.Tray.Add(SpecialKind.Rainbow);
                _gc.Hud.SetTrayVisible(true); _gc.Hud.SetTray(st.Tray, -1);
                // Tepsi alt çubukta: yazı bu sahnede üstte.
                Caption("Big groups make power stones that wait in the tray.\nFire them any time, without using a move.", true);
                yield return Rec(2.6f);
                var c = CenterCell();
                _gc.PlayStep(new StepData { Type = "use", Row = 1, X = c.X, Y = c.Y }); // Altın Rulo (2. yuva)
                yield return Rec(0.2f);
                yield return RecUntilIdle(6f);
                yield return Rec(1.4f);
                Caption("", false);

                // 4) Kurallı bölüm: Kesin Hamle (27).
                _gc.StartLevel(26);
                Caption("Rule levels start at level 27.\nExact Moves: no move to spare.");
                yield return Rec(3.0f);
                _gc.Hud.HideRuleCard(false);
                foreach (var step in _gc.CurrentLevel.Solution)
                {
                    if (_gc.Session.State.Status != GameStatus.Idle) break;
                    _gc.PlayStep(step);
                    yield return RecUntilIdle(6f);
                    yield return Rec(0.3f);
                }
                yield return Rec(1.5f);

                // 5) Patron (30) ve diğer kurallar.
                _gc.Hud.HidePanel();
                _gc.StartLevel(29);
                Caption("Every 10th level is a boss that combines rules.");
                yield return Rec(2.6f);
                _gc.StartLevel(36);
                Caption("Think Backwards: the tempting move loses.");
                yield return Rec(2.4f);
                _gc.StartLevel(46);
                Caption("Chain Master: goals fill only through chains.");
                yield return Rec(2.4f);
                _gc.StartLevel(86);
                Caption("The Right Moment: power stones, at the right time.");
                yield return Rec(2.4f);

                // 6) Günlük bulmaca: baştan açık, ilk giriş ücretsiz, kural kartıyla.
                _gc.Hud.HideRuleCard(false);
                _gc.ShowLevels();
                _gc.ShowDaily();
                Caption("Daily puzzle: open from day one, with its own rule.");
                yield return Rec(1.6f);
                _gc.PressDailyEnterForTest();
                yield return Rec(0.4f);
                _gc.StartDaily();
                yield return Rec(2.8f);
            }
            finally
            {
                Time.captureFramerate = 0;
                GameController.ToursOverride = null;
                GameController.CountryOverride = null;
                GameController.ClockOverride = null;
                GameController.SaveDirectoryOverride = null;
                SafeAreaFitter.NormalizedOverride = null;
                UiAspect.Override = null;
                Object.Destroy(root);
                Object.Destroy(camGo);
                Object.Destroy(_band != null ? _band.transform.root.gameObject : null);
                _rt.Release();
                Object.Destroy(_read);
            }
            Debug.Log($"[KatKat] İnceleme videosu: {_frame} kare → {_dir}");
            Assert.Greater(_frame, Fps * 20);
        }

        private void BuildCaption(Camera cam)
        {
            var go = new GameObject("ReviewCaption", typeof(RectTransform));
            go.layer = 5;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 5000;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            var band = UiFactory.Image("Band", go.transform, UiArt.Box, new Color(0.07f, 0.11f, 0.14f, 0.86f));
            var br = band.rectTransform;
            br.anchorMin = new Vector2(0f, 0f); br.anchorMax = new Vector2(1f, 0f); br.pivot = new Vector2(0.5f, 0f);
            br.sizeDelta = new Vector2(0f, 230f); br.anchoredPosition = Vector2.zero;
            _band = band.gameObject;
            _caption = UiFactory.Text("Caption", band.transform, "", 44, Color.white);
            UiFactory.Stretch(_caption.rectTransform, 40f, 40f, 24f, 24f);
            _caption.enableAutoSizing = true; _caption.fontSizeMax = 44; _caption.fontSizeMin = 26;
        }

        /// <summary>Alt şeritte yazı; <paramref name="top"/> ise üstte (alt çubuktaki tepsi görünsün diye).</summary>
        private void Caption(string text, bool top = false)
        {
            _caption.text = text; // İngilizce, şekillendirme gerekmez
            var br = (RectTransform)_band.transform;
            float y = top ? 1f : 0f;
            br.anchorMin = new Vector2(0f, y); br.anchorMax = new Vector2(1f, y); br.pivot = new Vector2(0.5f, y);
            br.anchoredPosition = Vector2.zero;
            _band.SetActive(!string.IsNullOrEmpty(text));
        }

        private IEnumerator Rec(float seconds)
        {
            int n = Mathf.RoundToInt(seconds * Fps);
            for (int i = 0; i < n; i++)
            {
                yield return null;
                Save();
            }
        }

        private IEnumerator RecUntilIdle(float maxSeconds)
        {
            int n = Mathf.RoundToInt(maxSeconds * Fps);
            for (int i = 0; i < n && (_gc.CurrentPhase == GameController.Phase.Animating || _gc.IsCelebrating); i++)
            {
                yield return null;
                Save();
            }
        }

        private void Save()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            _read.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            _read.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(_dir, $"f{_frame:00000}.jpg"), _read.EncodeToJPG(92));
            _frame++;
        }

        private void PeekBiggest(bool on)
        {
            var b = _gc.Session.State.Board;
            int bx = 0, by = 0, best = -1;
            for (int x = 0; x < b.Width; x++)
                for (int y = 0; y < b.Height; y++)
                    if (b[x, y].Layers != null && b[x, y].Layers.Count > best) { best = b[x, y].Layers.Count; bx = x; by = y; }
            _gc.PeekForTest(bx, by, on);
        }

        private Coord CenterCell()
        {
            var b = _gc.Session.State.Board;
            Coord c = default; float best = float.MaxValue;
            for (int x = 0; x < b.Width; x++)
                for (int y = 0; y < b.Height; y++)
                {
                    if (!b[x, y].IsSelectable) continue;
                    float d = (x - b.Width * 0.5f) * (x - b.Width * 0.5f) + (y - b.Height * 0.5f) * (y - b.Height * 0.5f);
                    if (d < best) { best = d; c = new Coord(x, y); }
                }
            return c;
        }
    }
}
