using System;
using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// 2026-11-26 (kullanıcı: "Roket ve rulo yine tahtanın üst kısmında olsun böyle sorunlar var … Belli olmuyorlar … Roketlere
    /// tıklandığında anlaşılmıyor"): tepsiden sürüklenen (ya da seçilen) özel taşın hedef önizlemesi ve tepsiden hedefe kullanım uçuşu.
    /// <list type="bullet">
    /// <item>Roket: şerit(ler)in bütün hücreleri; ışık hedeften dışa doğru akar (roketin gideceği yol), çift roket iki şerit.</item>
    /// <item>Altın Rulo: üst rengi hedefle aynı bütün açık yığınlar o renkte parlar ve nabız atar.</item>
    /// <item>Kürek: çakıllar ziyaret sırasıyla; nabız en yakından uzağa sırayla geçer, aralarında ince yol noktaları.</item>
    /// <item>Geçersiz hedef: hücrede soluk kırmızı çerçeve.</item>
    /// </list>
    /// Önizleme yalnız görünüm; ayarlar <see cref="BoosterFxConfig.Tray"/>. Tahta zamanıyla (<see cref="Advance"/>) canlanır.
    /// </summary>
    public sealed partial class BoardView
    {
        public enum AimStyle { None, Lane, Color, Gravel, Invalid }

        private readonly List<SpriteRenderer> _aimGlows = new List<SpriteRenderer>(), _aimFrames = new List<SpriteRenderer>(), _aimDots = new List<SpriteRenderer>();
        private readonly List<Coord> _aimCells = new List<Coord>();
        private readonly List<float> _aimPhase = new List<float>();
        private readonly List<StackView> _aimPulsed = new List<StackView>();
        private AimStyle _aimStyle;
        private Color _aimColor;
        private Coord _aimTarget;
        private float _aimClock;
        private int _aimDotCount;

        /// <summary>Gösterilen önizleme (testler, yakalama).</summary>
        public AimStyle AimShown => _aimStyle;
        public int AimCellCount => _aimStyle == AimStyle.None ? 0 : _aimCells.Count;

        /// <summary>
        /// Hedef önizlemesi. <paramref name="cells"/> vurulacak hücreler (Kürek: ziyaret sırası); <paramref name="target"/> parmağın
        /// altındaki hücre; renk önizlemenin tonu (Altın Rulo: taşın rengi).
        /// </summary>
        public void ShowAim(AimStyle style, Coord target, IList<Coord> cells, Color color)
        {
            HideAim();
            if (style == AimStyle.None) return;
            _aimStyle = style;
            _aimColor = color;
            _aimTarget = target;
            if (style == AimStyle.Invalid) _aimCells.Add(target);
            else if (cells != null) _aimCells.AddRange(cells);
            // Hücre ışığı: yuvarlak köşeli kare (rulo önizlemesiyle aynı kutu), eklemeli; yumuşak radyal ışık taşın üstünde seçilmiyordu.
            var glow = KatKat.UI.UiArt.Box;
            var frame = HintFrame;
            for (int i = 0; i < _aimCells.Count; i++)
            {
                var c = _aimCells[i];
                if (c.X < 0 || c.Y < 0 || c.X >= _width || c.Y >= _height) { _aimPhase.Add(0f); continue; }
                // Faz: roket hedeften uzaklık (ışık dışa akar), Kürek sıra, renk hepsi birlikte.
                float ph = style == AimStyle.Lane ? Mathf.Abs(c.X - target.X) + Mathf.Abs(c.Y - target.Y) : style == AimStyle.Gravel ? i : 0f;
                _aimPhase.Add(ph);
                var g = AimSprite(_aimGlows, i, "AimGlow", glow, 10 + _height * 4 + 3, true);
                g.transform.localPosition = CellCenter(c.X, c.Y) + new Vector3(0f, _pitch * 0.05f, 0f);
                g.transform.localScale = Vector3.one * (_pitch * 0.96f / glow.bounds.size.x);
                var f = AimSprite(_aimFrames, i, "AimFrame", frame, 896, false);
                f.transform.localPosition = CellCenter(c.X, c.Y) + new Vector3(0f, _pitch * 0.04f, 0f);
                f.transform.localScale = Vector3.one * (_pitch * 0.98f / (frame.rect.width / frame.pixelsPerUnit));
                // İkinci çerçeve biraz büyük: çizgi kalınlaşır (ince ipucu çerçevesi uzaktan seçilsin).
                var f2 = AimSprite(_aimFrames, _aimCells.Count + i, "AimFrame2", frame, 896, false);
                f2.transform.localPosition = f.transform.localPosition;
                f2.transform.localScale = f.transform.localScale * 1.035f;
                if (style == AimStyle.Color)
                {
                    var st = StackAt(c.X, c.Y);
                    if (st != null) _aimPulsed.Add(st);
                }
            }
            // Kürek: ardışık çakıllar arasında ince yol noktaları (ziyaret sırası).
            _aimDotCount = 0;
            if (style == AimStyle.Gravel)
                for (int i = 0; i + 1 < _aimCells.Count && _aimDotCount < 120; i++)
                {
                    var a = CellCenter(_aimCells[i].X, _aimCells[i].Y);
                    var b = CellCenter(_aimCells[i + 1].X, _aimCells[i + 1].Y);
                    int n = Mathf.Clamp(Mathf.RoundToInt((b - a).magnitude / (_pitch * 0.3f)), 1, 8);
                    for (int k = 1; k < n && _aimDotCount < 120; k++)
                    {
                        var d = AimSprite(_aimDots, _aimDotCount++, "AimDot", FxKit.Dot, 897, true);
                        d.transform.localPosition = Vector3.Lerp(a, b, k / (float)n) + new Vector3(0f, _pitch * 0.04f, 0f);
                        d.transform.localScale = Vector3.one * (_pitch * 0.14f / FxKit.Dot.bounds.size.x);
                    }
                }
            TickAim(0f);
        }

        public void HideAim()
        {
            foreach (var s in _aimGlows) s.gameObject.SetActive(false);
            foreach (var s in _aimFrames) s.gameObject.SetActive(false);
            foreach (var s in _aimDots) s.gameObject.SetActive(false);
            foreach (var st in _aimPulsed) if (st != null) st.transform.localScale = Vector3.one;
            _aimPulsed.Clear();
            _aimCells.Clear();
            _aimPhase.Clear();
            _aimStyle = AimStyle.None;
            _aimDotCount = 0;
        }

        private SpriteRenderer AimSprite(List<SpriteRenderer> pool, int i, string name, Sprite sprite, int order, bool additive)
        {
            while (pool.Count <= i)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (additive) FxKit.MakeAdditive(sr);
                pool.Add(sr);
            }
            var r = pool[i];
            r.sprite = sprite;
            r.sortingOrder = order;
            r.gameObject.SetActive(true);
            return r;
        }

        private void TickAim(float ms)
        {
            if (_aimStyle == AimStyle.None) return;
            _aimClock += ms;
            var tc = BoosterFxConfig.Current.Tray;
            float period = Mathf.Max(120f, tc.PreviewPulseMs);
            bool invalid = _aimStyle == AimStyle.Invalid;
            var baseCol = invalid ? new Color(1f, 0.35f, 0.3f, 1f) : _aimColor;
            int n = _aimCells.Count;
            for (int i = 0; i < n && i < _aimGlows.Count; i++)
            {
                float ph = _aimPhase[i];
                float wave;
                if (_aimStyle == AimStyle.Lane) wave = 0.5f + 0.5f * Mathf.Cos((_aimClock / period - ph * 0.12f) * Mathf.PI * 2f); // hedeften dışa akan ışık
                else if (_aimStyle == AimStyle.Gravel)
                {
                    // Nabız sırayla en yakından uzağa geçer; sıradaki çakıl en parlak.
                    float cycle = Mathf.Max(1, n) * tc.PreviewGravelStepMs + 300f;
                    float head = (_aimClock % cycle) / tc.PreviewGravelStepMs;
                    float dd = head - ph;
                    wave = dd >= 0f && dd < 2.5f ? Mathf.Max(0.45f, 1f - dd / 2.5f) : 0.45f;
                }
                else wave = 0.5f + 0.5f * Mathf.Sin(_aimClock / period * Mathf.PI * 2f);
                bool isTarget = _aimCells[i].X == _aimTarget.X && _aimCells[i].Y == _aimTarget.Y;
                var g = _aimGlows[i];
                float ga = invalid ? 0.35f : tc.PreviewGlow * (0.35f + 0.65f * wave) * (isTarget ? 1.15f : 1f);
                var gc = _aimStyle == AimStyle.Color ? new Color(1f, 0.85f, 0.4f, 1f) : Color.Lerp(baseCol, Color.white, 0.3f); // taşın kendi rengi kendi üstünde seçilmez: Altın Rulo altın
                g.color = new Color(gc.r, gc.g, gc.b, Mathf.Clamp01(ga * 0.6f));
                float gs = _pitch * (0.92f + 0.06f * wave + (isTarget ? 0.06f : 0f)) / g.sprite.bounds.size.x;
                g.transform.localScale = Vector3.one * gs;
                var f = _aimFrames[i];
                float fa = tc.PreviewFrame * (invalid ? 0.9f : 0.55f + 0.45f * wave);
                // Çerçeve: Altın Rulo altın (taş rengi ışıkta), diğerleri tonun parlak hali.
                var fc = invalid ? new Color(1f, 0.45f, 0.4f, fa) : _aimStyle == AimStyle.Color ? new Color(1f, 0.85f, 0.3f, 1f) : Color.Lerp(baseCol, Color.white, 0.2f);
                fc.a = fa;
                f.color = fc;
                float fs = _pitch * (isTarget ? 1.04f + 0.04f * wave : 0.98f) / (f.sprite.rect.width / f.sprite.pixelsPerUnit);
                f.transform.localScale = Vector3.one * fs;
                if (n + i < _aimFrames.Count)
                {
                    var f2 = _aimFrames[n + i];
                    f2.color = fc;
                    f2.transform.localScale = Vector3.one * fs * 1.035f;
                }
            }
            for (int i = 0; i < _aimDotCount && i < _aimDots.Count; i++)
            {
                float a = 0.45f + 0.35f * Mathf.Sin(_aimClock * 0.01f - i * 0.6f);
                _aimDots[i].color = new Color(1f, 0.95f, 0.8f, a);
            }
            // Altın Rulo: hedef renkteki yığınlar hafifçe nefes alır.
            if (_aimStyle == AimStyle.Color)
            {
                float k = 1f + 0.09f * Mathf.Sin(_aimClock / period * Mathf.PI * 2f);
                foreach (var st in _aimPulsed) if (st != null) st.transform.localScale = new Vector3(k, k, 1f);
            }
        }

        // ---- Tepsiden kullanım uçuşu ------------------------------------------------------------------

        /// <summary>Sıradaki <see cref="Play"/> tepsi kullanımıysa simgenin kalkış noktası (yuva ya da sürükleme hayaleti, dünya).</summary>
        public Vector3? LaunchFrom { get; set; }
        /// <summary>Kalkıştaki simge boyu (dünya; 0: hücre boyu).</summary>
        public float LaunchSize { get; set; }
        /// <summary>Tepsi yuvasındaki simge boyu (dünya; doğan taş uçuşu bu boya küçülür).</summary>
        public float TraySize { get; set; }
        /// <summary>Doğan özel taş tepsideki yuvasına vardı.</summary>
        public Action<SpecialKind> TrayTokenArrived { get; set; }

        /// <summary>Özel taşın tepsi tonu (uçuş izi, önizleme): roket turkuaz, Altın Rulo altın, Kürek kum.</summary>
        public static Color SpecialTone(SpecialKind k) => k == SpecialKind.Rainbow ? new Color(1f, 0.84f, 0.36f, 1f)
            : k == SpecialKind.Shovel ? new Color(0.95f, 0.85f, 0.62f, 1f) : new Color(0.3f, 0.95f, 0.88f, 1f);

        /// <summary>Kullanım uçuşunu başlatır; süresini (ms) döndürür (dalgalar bu kadar gecikir).</summary>
        private float PlayUseFly(MoveResult r, Vector3 from, float size0)
        {
            var kind = r.UsedSpecial;
            var at = r.UsedAt;
            if (kind == SpecialKind.None || at.X < 0 || at.Y < 0 || at.X >= _width || at.Y >= _height) return 0f;
            var st = StackAt(at.X, at.Y);
            var to = st.transform.position + st.TopFaceLocal;
            var tc = BoosterFxConfig.Current.Tray;
            Booster.FlyIcon(SpecialArt.For(kind), SpecialArt.Angle(kind), from, to, tc.UseFlyMs, size0 > 0f ? size0 : WorldCell, WorldCell * 0.95f,
                tc.UseFlyArc, tc.UseFlySwell, tc.UseTrailRate, SpecialTone(kind), true, null);
            return tc.UseFlyMs;
        }
    }
}
