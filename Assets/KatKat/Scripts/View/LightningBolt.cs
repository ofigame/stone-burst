using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Altın Rulo (renk temizleyici) yıldırımı (kullanıcı, 2026-11-08: "patlayacak olan tüm renklere yıldırım gibi ağ
    /// fırlatılsın"). Kaynaktan hedefe kırık çizgi; her ~45 ms'de yeniden kırılır (titreşim), uçtan uca hızla uzar,
    /// sonda söner. Parçalar sprite (beyaz çekirdek + renkli hale); ayrı malzeme/gölgelendirici gerekmez.
    /// Süre ve silme <see cref="BoardView"/> etki listesinden yönetilir (Tick).
    /// </summary>
    public sealed class LightningBolt : MonoBehaviour
    {
        private const int Segments = 9;
        private const float JitterMs = 45f, GrowMs = 120f;
        private static Sprite _bar;

        private SpriteRenderer[] _core, _halo;
        private Vector3 _from, _to;
        private float _thick;
        private System.Random _rng;
        private int _lastJitter = -1;
        private readonly Vector3[] _pts = new Vector3[Segments + 1];

        /// <summary>Yatayda dolu, dikeyde kenara doğru sönen şerit.</summary>
        private static Sprite Bar
        {
            get
            {
                if (_bar) return _bar;
                const int w = 4, h = 32;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "bolt-bar" };
                for (int y = 0; y < h; y++)
                {
                    float d = Mathf.Abs((y + 0.5f) / h - 0.5f) * 2f;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    for (int x = 0; x < w; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                return _bar = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), h);
            }
        }

        /// <param name="from">Dünya konumu (kaynak taşın üst yüzü).</param>
        /// <param name="pitch">Hücre aralığı (yerel birim); kalınlık ve kırılma genliği buna göre.</param>
        public static LightningBolt Create(Transform parent, Vector3 from, Vector3 to, float pitch, int order, int seed)
        {
            var go = new GameObject("Lightning");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<LightningBolt>();
            float ls = Mathf.Max(1e-4f, parent.lossyScale.x);
            b._from = from;
            b._to = to;
            b._thick = pitch * ls;
            b._rng = new System.Random(seed);
            b._core = new SpriteRenderer[Segments];
            b._halo = new SpriteRenderer[Segments];
            for (int i = 0; i < Segments; i++)
            {
                b._halo[i] = Part(go.transform, "Halo", new Color(0.78f, 0.62f, 1f, 0.8f), order);
                b._core[i] = Part(go.transform, "Core", new Color(1f, 1f, 0.92f, 1f), order + 1);
            }
            b.Tick(0f, 0f);
            return b;
        }

        private static SpriteRenderer Part(Transform parent, string name, Color c, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.sprite = Bar;
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        /// <param name="ms">Başlangıçtan geçen süre.</param>
        /// <param name="x">0..1 yaşam oranı (sonda söner).</param>
        public void Tick(float ms, float x)
        {
            int j = Mathf.FloorToInt(ms / JitterMs);
            if (j != _lastJitter)
            {
                _lastJitter = j;
                var d = _to - _from;
                var n = new Vector3(-d.y, d.x, 0f).normalized;
                float amp = Mathf.Min(_thick * 0.38f, d.magnitude * 0.12f);
                for (int i = 0; i <= Segments; i++)
                {
                    float t = (float)i / Segments;
                    float off = i == 0 || i == Segments ? 0f : ((float)_rng.NextDouble() * 2f - 1f) * amp * Mathf.Sin(t * Mathf.PI);
                    _pts[i] = _from + d * t + n * off;
                }
            }
            float grow = Mathf.Clamp01(ms / GrowMs);
            float fade = x < 0.8f ? 1f : Mathf.Clamp01((1f - x) / 0.2f);
            float flicker = 0.75f + 0.25f * Mathf.Sin(ms * 0.09f);
            for (int i = 0; i < Segments; i++)
            {
                float t0 = (float)i / Segments;
                bool on = t0 < grow;
                Place(_core[i], _pts[i], _pts[i + 1], _thick * 0.07f, on, fade);
                Place(_halo[i], _pts[i], _pts[i + 1], _thick * 0.26f, on, fade * 0.8f * flicker);
            }
        }

        private static void Place(SpriteRenderer sr, Vector3 a, Vector3 b, float thick, bool on, float alpha)
        {
            sr.enabled = on;
            if (!on) return;
            var d = b - a;
            float len = d.magnitude;
            var tr = sr.transform;
            tr.position = a;
            tr.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            // Bar: 4×32 px, 32 ppu → 0,125 × 1 birim (pivot sol orta). Dünya ölçüsünü ebeveyn ölçeğinden bağımsız ver.
            var ps = tr.parent != null ? tr.parent.lossyScale : Vector3.one;
            tr.localScale = new Vector3(len * 1.04f / (0.125f * Mathf.Max(1e-4f, ps.x)), thick / Mathf.Max(1e-4f, ps.y), 1f);
            var c = sr.color;
            c.a = alpha * (sr.name == "Core" ? 1f : 0.8f);
            sr.color = c;
        }
    }
}
