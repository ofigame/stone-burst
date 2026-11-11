using System;
using System.Collections.Generic;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// vr1 P1 prototipi (03_KOLEKSIYON_ODUL, 04_ANIMASYON_GORSEL_SES; yalnız <see cref="Vr1Proto.On"/> iken):
    /// - Yıldız hedefi kartında sıradaki koleksiyon taşı buz içinde: 4 evre (yoğun kırağı/siluet → ince çatlak →
    ///   saydam pencere → ince kabuk). Kırağı ve çatlak maskeleri çalışma anında deterministik üretilir; paketteki buz
    ///   görseli yalnız kenar kaplamasıdır. Taş görseli koleksiyondaki asıl görseldir (yeniden çizilmez).
    /// - Haritaya dönüş: kayıt farkından (GameController) net yeni yıldızlar sayaca yay çizerek gider (en çok 3 parça,
    ///   her biri 450–650 ms), sayaç bir kez artar, çubuk 400 ms eski→yeni dolar, evre değişince çatlak sesi. Taş açıldıysa
    ///   kabuk kırılır (0–1100 ms taslak zaman çizgisi) ve mevcut taş vitrini açılır. Animasyon veri yazmaz.
    /// Süreler taslaktır; görsel kabulden sonra kesinleşir.
    /// </summary>
    public sealed partial class MenuView
    {
        /// <summary>Haritaya dönüş sunumu (tek seferlik; ShowLevels tüketir).</summary>
        public struct Vr1ReturnInfo
        {
            public bool Has;
            public int StarsBefore, StarsAfter, NetStars, NewLevels;
            public bool ReplayOnly;
            /// <summary>Çubuk dolumu eski → yeni (aynı hedef taşı içinde; açılışta yeni = 1).</summary>
            public float FromP, ToP;
            /// <summary>Hedef taş bu dönüşte kazanıldı: kabuk kırılır, sonra <see cref="AfterReveal"/>.</summary>
            public bool Unlocking;
            public Action AfterReveal;
            public string Summary;
        }

        public Vr1ReturnInfo Vr1Return;

        /// <summary>vr1 pilot listesi (prototip; haritada Sıralama'nın altında).</summary>
        public Action PilotPressed;
        /// <summary>Deneme listesi pilot kataloğu için mi açılıyor (başlık ve alt yazı).</summary>
        public bool TrialListPilot;

        private void BuildPilotButton(RectTransform shell)
        {
            if (!Vr1Proto.On || PilotPressed == null) return;
            var b = ShellRoundButton("PilotButton", shell, RuleIcons.For("seal"), 120f, () => PilotPressed?.Invoke());
            UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(-V11ColumnX, -V11ColumnZpeTop - V11LeftShift - 66f - 214f), new Vector2(120f, 120f));
            var cap = WhiteCapsule("PilotLabel", b.transform, V11Navy, new Vector2(0.5f, 0f), new Vector2(0f, -30f), new Vector2(170f, 50f), 5f);
            cap.raycastTarget = true;
            var t = UiFactory.Title("Text", cap.transform, Loc.T("vr1.pilot.button"), 30, Color.white, new Color32(0x06, 0x1A, 0x44, 0xFF));
            t.raycastTarget = false;
            UiFactory.Stretch(t.rectTransform, 12f, 12f, 2f, 4f);
            FitText(t, 18);
        }

        /// <summary>Testler/yakalama: son dönüş animasyonu.</summary>
        public Vr1MapFx Vr1Fx { get; private set; }

        /// <summary>Buzlu taş katmanları (kartın üst kısmı, merkez −88). Taş: asıl görsel; kırağı, kabuk, çatlak, ışıltı.</summary>
        private void BuildIceStone(RectTransform card, StarGoalInfo g, out Vr1IceRefs refs)
        {
            refs = new Vr1IceRefs();
            var halo = Decor("Halo", card, SpecialArt.Glow);
            halo.color = new Color(g.Glow.r, g.Glow.g, g.Glow.b, 0.55f);
            UiFactory.Place(halo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(210f, 210f));
            var back = Decor("IceBack", card, Vr1IceArt.Frost);
            back.color = new Color(0.55f, 0.78f, 0.95f, 0.55f);
            UiFactory.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(160f, 160f));
            var stone = Decor("Stone", card, g.SharpSprite ?? g.StoneSprite);
            stone.preserveAspect = true;
            // %78: buz kabuğunun iç boşluğuna oranla (taş kabuğu örtmez).
            UiFactory.Place(stone.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(128f, 128f));
            var frost = Decor("Frost", card, Vr1IceArt.Frost);
            UiFactory.Place(frost.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(150f, 150f));
            var cracks = Decor("Cracks", card, null);
            UiFactory.Place(cracks.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(150f, 150f));
            var rim = Decor("IceRim", card, Vr1Proto.IceRim ?? SpecialArt.Glow);
            rim.preserveAspect = true;
            UiFactory.Place(rim.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(196f, 196f));
            var flash = Decor("Flash", card, SpecialArt.Glow);
            flash.color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Place(flash.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(230f, 230f));
            refs.Stone = stone; refs.Frost = frost; refs.Cracks = cracks; refs.Rim = rim; refs.Back = back; refs.Flash = flash;
            refs.Card = card;
            ApplyIceStage(refs, g.P, false);
        }

        internal sealed class Vr1IceRefs
        {
            public RectTransform Card;
            public Image Stone, Frost, Cracks, Rim, Back, Flash;
            public RectTransform Fill;
            public TMPro.TMP_Text Count;
            public int Stage = -1;
        }

        /// <summary>Evre görünümü: taş tonu (siluet → asıl renk), kırağı yoğunluğu, çatlak maskesi.</summary>
        internal static void ApplyIceStage(Vr1IceRefs r, float p, bool unlocked)
        {
            int stage = KatKat.Meta.CollectionProgress.IceStage(p, unlocked);
            r.Stage = stage;
            Color[] tint = { new Color(0.30f, 0.42f, 0.58f, 0.9f), new Color(0.58f, 0.70f, 0.86f, 0.95f), new Color(0.84f, 0.90f, 1f, 1f), Color.white, Color.white };
            float[] frost = { 0.90f, 0.62f, 0.34f, 0.14f, 0f };
            r.Stone.color = tint[stage];
            r.Frost.color = new Color(0.88f, 0.95f, 1f, frost[stage]);
            r.Back.enabled = stage < KatKat.Meta.CollectionProgress.Unlocked;
            r.Cracks.sprite = stage >= 1 && stage <= 3 ? Vr1IceArt.Cracks(stage) : null;
            r.Cracks.enabled = r.Cracks.sprite != null;
            r.Cracks.color = new Color(1f, 1f, 1f, 0.9f);
            r.Rim.enabled = stage < KatKat.Meta.CollectionProgress.Unlocked;
            r.Rim.color = new Color(1f, 1f, 1f, stage == 3 ? 0.75f : 1f);
        }

        private void StartVr1Return(Vr1IceRefs refs, RectTransform shell)
        {
            Vr1Fx = null;
            var info = Vr1Return;
            Vr1Return = default;
            if (!info.Has) return;
            Vr1Fx = shell.gameObject.AddComponent<Vr1MapFx>();
            Vr1Fx.Init(this, refs, shell, info);
        }

        /// <summary>Haritaya dönüş zaman çizgisi. Editörde/testte <see cref="Advance"/> ile ilerletilir.</summary>
        public sealed class Vr1MapFx : MonoBehaviour
        {
            private MenuView _menu;
            private Vr1IceRefs _r;
            private RectTransform _shell;
            private Vr1ReturnInfo _i;
            private float _t;
            private readonly List<(RectTransform rt, Image img, Vector2 from, float start)> _stars = new List<(RectTransform, Image, Vector2, float)>();
            private readonly List<(RectTransform rt, Image img, Vector2 dir)> _shards = new List<(RectTransform, Image, Vector2)>();
            private Text _counter;
            private string _countFinal;
            private Vector2 _counterPos;
            private int _arrived;
            private bool _counted, _cracked, _shattered, _revealed, _revealSound, _done;
            private float _barStart;
            private const float FlyMs = 550f, Stagger = 120f, BarMs = 400f;

            public bool Done => _done;
            public int StarsLaunched => _stars.Count;
            public bool Counted => _counted;
            public bool Revealed => _revealed;

            internal void Init(MenuView menu, Vr1IceRefs refs, RectTransform shell, Vr1ReturnInfo info)
            {
                _menu = menu; _r = refs; _shell = shell; _i = info;
                if (!string.IsNullOrEmpty(info.Summary)) menu.ShowToast(info.Summary, 3.2f);
                if (_r != null && _r.Fill != null) SetFill(info.FromP);
                if (_r != null) ApplyIceStage(_r, info.FromP, false);
                if (_r != null && _r.Count != null)
                {
                    _countFinal = _r.Count.text;
                    int slash = _countFinal.IndexOf('/');
                    if (slash > 0 && info.NetStars > 0 && int.TryParse(_countFinal.Substring(0, slash), out int now)) _r.Count.text = Mathf.Max(0, now - info.NetStars) + _countFinal.Substring(slash);
                }
                int n = Mathf.Min(3, Mathf.Max(0, info.NetStars));
                var starSprite = PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite;
                for (int k = 0; k < n; k++)
                {
                    var img = Decor("FlyStar", shell, starSprite);
                    img.preserveAspect = true;
                    var rt = img.rectTransform;
                    var from = new Vector2(-60f + 60f * k, -760f);
                    UiFactory.Place(rt, new Vector2(0.5f, 1f), from, new Vector2(72f, 72f));
                    rt.localScale = Vector3.zero;
                    _stars.Add((rt, img, from, k * Stagger));
                }
                _barStart = n > 0 ? (n - 1) * Stagger + FlyMs : 0f;
            }

            private void Update() => Advance(Time.unscaledDeltaTime * 1000f);

            private void ResolveCounter()
            {
                if (_counter != null) return;
                foreach (var t in _shell.GetComponentsInChildren<Text>(true))
                    if (t.name == "Stars" && t.transform.parent != null && t.transform.parent.name == "StarCapsule") { _counter = t; break; }
                if (_counter == null) return;
                if (!_counted) _counter.text = _i.StarsBefore.ToString();
                var cap = (RectTransform)_counter.transform.parent;
                Vector3 world = cap.TransformPoint(new Vector3(-cap.rect.width * 0.5f + 54f, 0f, 0f));
                _counterPos = (Vector2)_shell.InverseTransformPoint(world) + new Vector2(0f, _shell.rect.height * 0.5f);
            }

            public void Advance(float ms)
            {
                if (_done || ms <= 0f) return;
                // Büyük adımlar (yakalama) parçalara bölünür ki ses/olay sırası korunsun.
                while (ms > 0f) { float step = Mathf.Min(ms, 33f); ms -= step; Step(step); if (_done) return; }
            }

            private void Step(float ms)
            {
                _t += ms;
                ResolveCounter();
                for (int k = 0; k < _stars.Count; k++)
                {
                    var (rt, img, from, start) = _stars[k];
                    float u = (_t - start) / FlyMs;
                    if (u <= 0f) continue;
                    if (u >= 1f)
                    {
                        if (rt.gameObject.activeSelf)
                        {
                            rt.gameObject.SetActive(false);
                            _arrived++;
                            Vr1Proto.Play("reward-arrival");
                        }
                        continue;
                    }
                    float e = Ease.InOutSine(u);
                    Vector2 to = _counter != null ? _counterPos : new Vector2(160f, -56f);
                    Vector2 mid = (from + to) * 0.5f + new Vector2(220f, -40f); // sağdan yay: başlık yazısının üstünden geçmez
                    Vector2 p = Vector2.Lerp(Vector2.Lerp(from, mid, e), Vector2.Lerp(mid, to, e), e);
                    rt.anchoredPosition = p;
                    rt.localScale = Vector3.one * (u < 0.15f ? Ease.OutBack(u / 0.15f) : Mathf.Lerp(1f, 0.7f, e));
                }
                if (!_counted && _t >= _barStart)
                {
                    _counted = true;
                    if (_counter != null) { _counter.text = _i.StarsAfter.ToString(); _counter.transform.localScale = Vector3.one * 1.15f; }
                    if (_r != null && _r.Count != null && _countFinal != null) _r.Count.text = _countFinal;
                }
                if (_counter != null && _counted) _counter.transform.localScale = Vector3.Lerp(_counter.transform.localScale, Vector3.one, 0.25f);
                if (_r == null) { _done = _t > _barStart + BarMs; return; }
                float b = Mathf.Clamp01((_t - _barStart) / BarMs);
                float p01 = Mathf.Lerp(_i.FromP, _i.ToP, Ease.OutCubic(b));
                if (_r.Fill != null) SetFill(p01);
                if (!_cracked && b >= 1f)
                {
                    _cracked = true;
                    int before = _r.Stage;
                    ApplyIceStage(_r, _i.Unlocking ? 0.999f : _i.ToP, false);
                    if (_r.Stage != before) Vr1Proto.Play("ice-crack");
                }
                if (!_i.Unlocking) { _done = b >= 1f; return; }
                float s = _t - _barStart - BarMs; // kırılma zaman çizgisi (0–1100 ms)
                if (s < 0f) return;
                if (Motion.Reduced)
                {
                    // Azaltılmış hareket: titreme ve parçacık yok; 180 ms geçişle kabuk kalkar, taş görünür.
                    if (!_shattered) { _shattered = true; Vr1Proto.Play("ice-shatter"); }
                    float k = Mathf.Clamp01(s / 180f);
                    _r.Rim.color = new Color(1f, 1f, 1f, 1f - k);
                    _r.Frost.color = new Color(0.88f, 0.95f, 1f, 0.14f * (1f - k));
                    if (k >= 1f && !_revealed)
                    {
                        ApplyIceStage(_r, 1f, true);
                        _revealed = true;
                        _done = true;
                        _i.AfterReveal?.Invoke();
                    }
                    return;
                }
                // 0–180: çatlak ışığı.
                _r.Flash.color = new Color(1f, 0.97f, 0.85f, s < 180f ? s / 180f * 0.8f : Mathf.Max(0f, 0.8f - (s - 180f) / 600f));
                // 180–400: yalnız kabuk ±2 px titrer.
                float shake = s > 180f && s < 400f ? 2f * Mathf.Sin(s * 0.35f) : 0f;
                _r.Rim.rectTransform.anchoredPosition = new Vector2(shake, -88f);
                // 400–750: 10 buz parçası dışa ayrılır; kabuk ve kırağı kaybolur.
                if (!_shattered && s >= 400f)
                {
                    _shattered = true;
                    Vr1Proto.Play("ice-shatter");
                    ApplyIceStage(_r, 1f, true);
                    var shard = Vr1IceArt.Shard;
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * Mathf.PI * 2f / 10f + 0.3f;
                        var img = Decor("Shard", _r.Card, shard);
                        img.preserveAspect = true;
                        UiFactory.Place(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(26f + 6f * (k % 3), 26f + 6f * (k % 3)));
                        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, k * 37f);
                        _shards.Add((img.rectTransform, img, new Vector2(Mathf.Cos(a), Mathf.Sin(a))));
                    }
                }
                if (_shattered)
                {
                    float f = Mathf.Clamp01((s - 400f) / 350f);
                    foreach (var (rt, img, dir) in _shards)
                    {
                        rt.anchoredPosition = new Vector2(0f, -88f) + dir * Mathf.Lerp(30f, 130f, Ease.OutCubic(f));
                        img.color = new Color(1f, 1f, 1f, 1f - f);
                    }
                }
                // 550–1100: taş yükselir; 550'de ortaya çıkış sesi.
                if (s >= 550f && !_revealed)
                {
                    float r = Mathf.Clamp01((s - 550f) / 550f);
                    _r.Stone.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.18f, Ease.OutBack(r));
                    if (!_revealSound) { _revealSound = true; Vr1Proto.Play("collection-reveal"); }
                }
                if (s >= 1100f && !_revealed)
                {
                    _revealed = true;
                    _done = true;
                    _i.AfterReveal?.Invoke();
                }
            }

            private void SetFill(float p)
            {
                var f = _r.Fill;
                f.anchorMax = new Vector2(Mathf.Clamp01(p), 1f);
            }
        }
    }

    /// <summary>Kırağı ve çatlak maskeleri (çalışma anında, deterministik; evre başına bir kez).</summary>
    internal static class Vr1IceArt
    {
        private static Sprite _frost;
        private static Sprite _shard;

        /// <summary>Küçük buz kırıntısı (sivri dörtgen, açık mavi-beyaz, kenarı yumuşak).</summary>
        public static Sprite Shard
        {
            get
            {
                if (_shard) return _shard;
                const int n = 48;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "vr1-shard", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Abs(u) / 0.55f + Mathf.Abs(v) / 0.95f; // dikey uzun elmas
                    float a = Mathf.Clamp01((1f - d) * 6f);
                    float shade = 0.75f + 0.25f * Mathf.Clamp01(0.5f - u * 0.6f + v * 0.4f);
                    px[y * n + x] = new Color32((byte)(200 * shade + 55), (byte)(235 * shade + 20), 255, (byte)(255f * a * 0.9f));
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _shard = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
        }
        private static readonly Sprite[] _cracks = new Sprite[4];

        /// <summary>Yumuşak kenarlı yuvarlak-kare kırağı (ortası biraz daha yoğun, ince kristal desenli).</summary>
        public static Sprite Frost
        {
            get
            {
                if (_frost) return _frost;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "vr1-frost", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                var rnd = new System.Random(7);
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    // Elmas (45°) yuvarlak kare: kabuğun iç boşluğuna uyar.
                    float d = (Mathf.Abs(u + v) + Mathf.Abs(u - v)) * 0.5f;
                    float edge = 1f - Mathf.Clamp01((d - 0.72f) / 0.2f);
                    float grain = 0.82f + 0.18f * (float)rnd.NextDouble();
                    float a = edge * grain * (0.85f + 0.15f * (1f - d));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(a)));
                }
                tex.SetPixels32(px);
                tex.Apply();
                return _frost = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
        }

        /// <summary>Çatlak çizgileri: evre 1 (3 çatlak), 2 (6, dallı), 3 (9 ince). Sabit tohum: her açılışta aynı.</summary>
        public static Sprite Cracks(int stage)
        {
            stage = Mathf.Clamp(stage, 1, 3);
            if (_cracks[stage]) return _cracks[stage];
            const int n = 160;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "vr1-cracks-" + stage, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[n * n];
            var rnd = new System.Random(31 + stage);
            int count = stage * 3;
            for (int c = 0; c < count; c++)
            {
                float ang = c * Mathf.PI * 2f / count + (float)rnd.NextDouble() * 0.5f;
                Vector2 p = new Vector2(n * 0.5f, n * 0.5f) + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (6f + (float)rnd.NextDouble() * 10f);
                float len = n * (0.18f + 0.06f * stage + (float)rnd.NextDouble() * 0.08f);
                float w = stage == 3 ? 1.0f : 1.5f;
                for (int seg = 0; seg < 5; seg++)
                {
                    ang += ((float)rnd.NextDouble() - 0.5f) * 0.7f;
                    var q = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * len / 5f;
                    Line(px, n, p, q, w);
                    if (stage >= 2 && seg == 2)
                    {
                        float b = ang + (rnd.NextDouble() < 0.5 ? 0.8f : -0.8f);
                        Line(px, n, q, q + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * len / 6f, w * 0.8f);
                    }
                    p = q;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return _cracks[stage] = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        private static void Line(Color32[] px, int n, Vector2 a, Vector2 b, float w)
        {
            int x0 = Mathf.Max(0, (int)(Mathf.Min(a.x, b.x) - w - 2)), x1 = Mathf.Min(n - 1, (int)(Mathf.Max(a.x, b.x) + w + 2));
            int y0 = Mathf.Max(0, (int)(Mathf.Min(a.y, b.y) - w - 2)), y1 = Mathf.Min(n - 1, (int)(Mathf.Max(a.y, b.y) + w + 2));
            var ab = b - a;
            float len2 = Mathf.Max(0.0001f, ab.sqrMagnitude);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                float d = (p - (a + ab * t)).magnitude;
                float al = Mathf.Clamp01(w + 0.6f - d);
                if (al <= 0f) continue;
                var c = px[y * n + x];
                byte na = (byte)Mathf.Max(c.a, 255f * al);
                px[y * n + x] = new Color32(235, 248, 255, na);
            }
        }
    }
}
