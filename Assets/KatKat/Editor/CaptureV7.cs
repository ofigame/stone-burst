using System;
using System.IO;
using System.Linq;
using KatKat.Game;
using KatKat.Levels;
using KatKat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v7 doğrulama yakalamaları (2026-11-06 istekleri): yükseltilmiş tepsi, yeni font ve düğme boyları, yeni ayarlar
    /// (dil seçimi), dokuz dil örnekleri, OFIGAME açılış ekranı, odaklı öğretici turlar, içi boş tahta süsleri.
    /// Batch: -executeMethod KatKat.EditorTools.CaptureV7.Run → docs/captures/v7/
    /// </summary>
    public static class CaptureV7
    {
        private const string V = "v7/";

        [MenuItem("Kat Kat/Yakalama/v7 doğrulama")]
        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v7"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            GameController.ToursOverride = true;
            GameController.TourIgnoreSaved = true;
            try
            {
                Splash();
                foreach (var lang in new[] { "tr", "en", "ar", "ja", "de" })
                {
                    Loc.Set(lang, false);
                    Shot($"ayarlar_{lang}.png", gc => gc.ShowSettingsForCapture());
                    Shot($"oyun_l038_{lang}.png", gc => gc.StartLevel(37));
                    Shot($"duraklat_{lang}.png", gc => { gc.StartLevel(37); gc.OpenPauseForCapture(); gc.Hud.AdvancePanel(2000f); });
                    Shot($"harita_{lang}.png", gc => gc.ShowLevels());
                    Shot($"giris_{lang}.png", gc => gc.ShowHome());
                }
                Loc.Set("tr", false);
                Shot("dil_secimi.png", gc => { gc.ShowSettingsForCapture(); gc.Menu.OpenLanguagePickerForCapture(); });
                foreach (var lang in new[] { "es", "it", "fr", "ko" })
                {
                    Loc.Set(lang, false);
                    Shot($"ayarlar_{lang}.png", gc => gc.ShowSettingsForCapture());
                }
                Loc.Set("tr", false);
                // Kayıp kartı (düğme boyları)
                Shot("kayip_tr.png", gc => { gc.StartLevel(37); gc.ShowLostForCapture(); gc.Hud.AdvancePanel(2000f); });

                // Öğretici turlar
                Shot("tur_l001_adim1.png", gc => gc.StartLevel(0));
                Shot("tur_l001_hedef.png", gc => { gc.StartLevel(0); Step(gc, 0); });
                Shot("tur_l001_hamle.png", gc => { gc.StartLevel(0); Step(gc, 0); gc.TourTapForCapture(); });
                Shot("tur_l001_adim2.png", gc => { gc.StartLevel(0); Step(gc, 0); gc.TourTapForCapture(); gc.TourTapForCapture(); });
                Shot("tur_l002.png", gc => gc.StartLevel(1));
                Shot("tur_l006_tepsi.png", gc => { gc.StartLevel(5); PlayUntilInfo(gc); });
                Shot("tur_l007_yuva.png", gc => { gc.StartLevel(6); PlayUntilUse(gc, false); });
                Shot("tur_l007_hedef.png", gc => { gc.StartLevel(6); PlayUntilUse(gc, true); });
                Shot("tur_l012_biriktir.png", gc => gc.StartLevel(11));
                foreach (int n in new[] { 21, 41, 61, 121, 201, 301 })
                    Shot($"tur_l{n:000}_mekanik.png", gc => gc.StartLevel(n - 1));
                Shot("tur_ar_l001.png", gc => { Loc.Set("ar", false); gc.StartLevel(0); });
                Loc.Set("tr", false);

                // İçi boş tahta süsleri (katalogdan içi boş bölüm; yoksa ortası oyulmuş örnek)
                int shown = 0;
                var cat = LevelCatalog.Load();
                for (int i = 0; i < cat.Count && shown < 4; i++)
                {
                    var d = cat.Levels[i];
                    if (!HasInteriorHole(d)) continue;
                    int idx = i;
                    Shot($"icibos_l{i + 1:000}.png", gc => { gc.StartLevel(idx); for (int k = 0; k < 40; k++) gc.Board.Decor.Tick(0.03f); });
                    shown++;
                }
                for (int k = 0; k < 4; k++)
                {
                    int src = new[] { 51, 99, 149, 179 }[k];
                    int kk = k;
                    Shot($"icibos_ornek_{k}.png", gc =>
                    {
                        var d = Hollow(gc.Catalog.Levels[src]);
                        gc.StartCustom(d, src + kk);
                        for (int s = 0; s < 25; s++) gc.Board.Decor?.Tick(0.04f);
                    });
                }
                File.WriteAllText(Path.Combine(PeelCapture.OutDir, "v7", "RAPOR.md"),
                    $"# v7 yakalamaları\n\nKatalogdaki içi boş bölümlerden gösterilen: {shown}.\nKareler editör render'ıdır; cihazda doğrulama değildir.\n");
            }
            finally
            {
                GameController.ToursOverride = null;
                GameController.TourIgnoreSaved = false;
                Loc.Set(keepLang, false);
            }
            Debug.Log("[KatKat] v7 yakalamaları tamam");
        }

        private static void Splash() => SplashTo(Path.Combine(PeelCapture.OutDir, V + "acilis_ofigame.png"));

        /// <summary>Açılış ekranını 1080×1920 PNG olarak yazar.</summary>
        internal static void SplashTo(string path)
        {
            var logo = Resources.Load<Texture2D>("Branding/ofigame-logo");
            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;
            cam.transform.position = new Vector3(0, 0, -10);
            var rt = new RenderTexture(1080, 1920, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            var go = Bootstrap.CreateSplash(logo, cam, out _, out _);
            go.GetComponent<Canvas>().scaleFactor = 1f;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(camGo);
        }

        private static void Shot(string file, Action<GameController> act) => GameCapture.ShotPublic(V + file, 1080, 1920, act);

        private static void Settle(GameController gc)
        {
            for (int k = 0; k < 600 && (gc.Board.IsPlaying || gc.Board.HasFx); k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
            gc.Hud.AdvanceEffects(1500f);
        }

        private static void Step(GameController gc, int i)
        {
            gc.PlayStep(gc.CurrentLevel.Solution[i]);
            Settle(gc);
        }

        private static void PlayUntilInfo(GameController gc)
        {
            var sol = gc.CurrentLevel.Solution;
            for (int i = 0; i < sol.Count && !gc.TourInfoShowing; i++) Step(gc, i);
        }

        private static void PlayUntilUse(GameController gc, bool slotPressed)
        {
            var sol = gc.CurrentLevel.Solution;
            int use = sol.FindIndex(s => s.Type == "use");
            if (use < 0) return;
            for (int i = 0; i < use; i++)
            {
                Step(gc, i);
                while (gc.TourInfoShowing) gc.TourTapForCapture();
            }
            if (slotPressed) gc.PressTraySlot(sol[use].Row);
        }

        private static bool HasInteriorHole(LevelData d)
        {
            var inShape = new bool[d.Width * d.Height];
            foreach (var c in d.Cells) if (c.Active || c.GateId > 0) inShape[c.Y * d.Width + c.X] = true;
            return KatKat.View.BoardDecor.InteriorHoles(inShape, d.Width, d.Height).Any(h => h.Count >= 2);
        }

        /// <summary>Örnek: tahtanın ortasından 2×2 hücre çıkarır, hedefleri kalan katmana göre kırpar.</summary>
        private static LevelData Hollow(LevelData src)
        {
            var asset = Resources.Load<TextAsset>("Levels/" + src.Id);
            var d = LevelParser.Parse(asset.text, src.Id + ".json");
            int cx = d.Width / 2 - 1, cy = d.Height / 2 - 1;
            d.Cells.RemoveAll(c => c.X >= cx && c.X <= cx + 1 && c.Y >= cy && c.Y <= cy + 1 && c.ClipHitsRemaining == 0 && !c.HasBottomStar && c.IceHits == 0 && string.IsNullOrEmpty(c.LockColor));
            foreach (var g in d.Goals.Where(g => g.Type == "collectColor"))
            {
                int avail = d.Cells.Sum(c => c.Layers.Count(l => l == g.Color));
                g.Amount = Math.Max(1, Math.Min(g.Amount, avail - 1));
            }
            return d;
        }
    }
}
