using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Yolculuktaki etkin bölüm düğmesinin altında dönen ışıltılı halka (kullanıcı, 2026-11-20: "yolculuktaki aktif olan
    /// bölümün butonunun altına ışıltı yapalım halka şeklinde olsun ve dönsün etrafında ışıltı"): kesik çizgili altın halka
    /// yavaşça döner, ters yönde ince ikinci halka; çevresinde yörüngede dolaşan ve yanıp sönen yıldızcıklar. Dokunma
    /// yakalamaz. Azaltılmış harekette halka durur, yıldızlar gizli.
    /// </summary>
    public sealed class CurrentNodeRing : MonoBehaviour
    {
        private static Sprite _dashed, _thin;
        private RectTransform _outer, _inner, _glow;
        private Image[] _stars;
        private float _radius;

        public static CurrentNodeRing Create(RectTransform parent, float size)
        {
            var root = UiFactory.Rect("CurrentRing", parent);
            var r = root.gameObject.AddComponent<CurrentNodeRing>();
            r.Build(root, size);
            return r;
        }

        private void Build(RectTransform root, float size)
        {
            _radius = size * 0.5f;
            // Kum ve çimen zeminde de seçilsin: arkada sıcak altın ışıma, halka kalın ve parlak.
            var glow = UiFactory.Image("Glow", root, SpecialArt.Glow, new Color(1f, 0.78f, 0.25f, 0.55f));
            glow.raycastTarget = false;
            glow.rectTransform.sizeDelta = new Vector2(size * 1.35f, size * 1.35f);
            _glow = glow.rectTransform;
            var outer = UiFactory.Image("Outer", root, Dashed(), new Color(1f, 0.95f, 0.62f, 1f));
            outer.raycastTarget = false;
            _outer = outer.rectTransform;
            _outer.sizeDelta = new Vector2(size, size);
            var inner = UiFactory.Image("Inner", root, Thin(), new Color(1f, 0.97f, 0.8f, 0.7f));
            inner.raycastTarget = false;
            _inner = inner.rectTransform;
            _inner.sizeDelta = new Vector2(size * 0.84f, size * 0.84f);
            var sprite = PremiumArt.V4("effects/sparkle") ?? FxKit.Star;
            _stars = new Image[5];
            for (int i = 0; i < _stars.Length; i++)
            {
                var s = UiFactory.Image("Star" + i, root, sprite, Color.clear);
                s.raycastTarget = false;
                s.preserveAspect = true;
                _stars[i] = s;
            }
            Advance(0f);
        }

        private void Update() => Advance(Time.unscaledTime);

        public void Advance(float t)
        {
            if (_outer == null) return;
            bool still = Motion.Reduced;
            _outer.localRotation = Quaternion.Euler(0f, 0f, still ? 0f : -t * 40f);
            _inner.localRotation = Quaternion.Euler(0f, 0f, still ? 0f : t * 65f);
            float pulse = still ? 1f : 1f + 0.04f * Mathf.Sin(t * 3f);
            _outer.localScale = new Vector3(pulse, pulse, 1f);
            _glow.localScale = new Vector3(pulse * pulse, pulse * pulse, 1f);
            for (int i = 0; i < _stars.Length; i++)
            {
                var s = _stars[i];
                if (still) { s.color = Color.clear; continue; }
                float a = -t * 0.9f + i * Mathf.PI * 2f / _stars.Length;
                float r = _radius * (1.02f + 0.06f * Mathf.Sin(t * 2f + i));
                s.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                float blink = Mathf.Clamp01(Mathf.Sin(t * 3.4f + i * 1.7f) * 0.5f + 0.5f);
                float sz = 18f + 34f * blink;
                s.rectTransform.sizeDelta = new Vector2(sz, sz);
                s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 120f + i * 30f);
                s.color = new Color(1f, 0.97f, 0.82f, 0.35f + 0.65f * blink);
            }
        }

        /// <summary>Kesik çizgili parlak altın halka (12 parça, yumuşak kenar ve dış ışıma), 256 px.</summary>
        private static Sprite Dashed() => _dashed != null ? _dashed : (_dashed = RingSprite("ring_dashed", 0.40f, 0.05f, 12));

        /// <summary>İnce sürekli halka.</summary>
        private static Sprite Thin() => _thin != null ? _thin : (_thin = RingSprite("ring_thin", 0.45f, 0.018f, 0));

        private static Sprite RingSprite(string name, float radius, float half, int dashes)
        {
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N - 0.5f;
                float d = Mathf.Sqrt(u * u + v * v);
                float core = Mathf.Clamp01(1f - Mathf.Abs(d - radius) / half);
                float glow = Mathf.Clamp01(1f - Mathf.Abs(d - radius) / (half * 4f)) * 0.35f;
                float a = Mathf.Max(core, glow);
                if (dashes > 0)
                {
                    float ang = Mathf.Atan2(v, u) / (Mathf.PI * 2f) * dashes;
                    float f = ang - Mathf.Floor(ang);
                    a *= Mathf.Clamp01((0.68f - Mathf.Abs(f - 0.5f) * 2f + 0.3f) * 4f) * 0.85f + 0.15f;
                }
                px[y * N + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            s.name = name;
            return s;
        }
    }
}
