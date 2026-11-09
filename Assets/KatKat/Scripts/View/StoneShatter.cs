using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// v10 taş kırılma efektinin görselleri (kullanıcı, 2026-11-07: "kâğıt efekti var oysa onlar taş; taş kırılma
    /// efekti yapalım"). Koddan üretilen yontulmuş taş parçası sprite'ları (3 biçim, ışıklı yüzeyli; katman rengiyle
    /// renklendirilir), çatlak deseni ve parça havuzu. Taş yüzü çizimleri (atlas, shader) değişmez.
    /// </summary>
    public static class StoneShatter
    {
        private static Sprite[] _chips;
        private static Sprite _crack;
        private static readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>();

        public static Sprite Chip(int i)
        {
            if (_chips == null) _chips = new[] { MakeChip(0), MakeChip(1), MakeChip(2) };
            return _chips[((i % 3) + 3) % 3];
        }

        public static SpriteRenderer Take(Transform parent)
        {
            SpriteRenderer sr = null;
            while (_pool.Count > 0 && sr == null) sr = _pool.Pop();
            if (sr == null)
            {
                sr = new GameObject("Shard").AddComponent<SpriteRenderer>();
            }
            sr.transform.SetParent(parent, false);
            sr.gameObject.SetActive(true);
            return sr;
        }

        public static void Give(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
            _pool.Push(sr);
        }

        /// <summary>Yontulmuş taş parçası: düzensiz dışbükey çokgen, sol üstten ışık alan yüzeyler, ince açık kenar.</summary>
        private static Sprite MakeChip(int variant)
        {
            const int n = 64;
            var rng = new System.Random(41 + variant * 17);
            int k = 6 + variant;
            var pts = new Vector2[k];
            for (int i = 0; i < k; i++)
            {
                float a = (i + (float)rng.NextDouble() * 0.5f) / k * Mathf.PI * 2f;
                float r = 0.30f + (float)rng.NextDouble() * 0.16f;
                pts[i] = new Vector2(0.5f + Mathf.Cos(a) * r, 0.5f + Mathf.Sin(a) * r * 0.86f);
            }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "shard_" + variant, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[n * n];
            var center = new Vector2(0.5f, 0.5f);
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                var p = new Vector2((i + 0.5f) / n, (j + 0.5f) / n);
                // İçeride mi: her kenara uzaklık (dışbükey).
                float inside = 1f;
                float minEdge = 1f;
                for (int e = 0; e < k; e++)
                {
                    var a = pts[e]; var b = pts[(e + 1) % k];
                    var ed = b - a;
                    var nrm = new Vector2(ed.y, -ed.x).normalized;
                    float d = Vector2.Dot(p - a, -nrm);
                    if (Vector2.Dot(center - a, -nrm) < 0) d = -d;
                    minEdge = Mathf.Min(minEdge, d);
                }
                inside = Mathf.Clamp01(minEdge * n + 0.5f);
                if (inside <= 0f) { px[j * n + i] = new Color32(0, 0, 0, 0); continue; }
                // Yüzeyler: merkezden yöne göre üç ton (ışık sol üstten).
                var dir = (p - center).normalized;
                float light = Vector2.Dot(dir, new Vector2(-0.6f, 0.8f));
                float facet = light > 0.35f ? 1.0f : light > -0.3f ? 0.82f : 0.62f;
                float edgeGlow = minEdge * n < 2.2f ? 0.25f : 0f;
                float v = Mathf.Clamp01(facet + edgeGlow);
                px[j * n + i] = new Color(v, v, v, inside);
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        /// <summary>Çatlak deseni: merkezden dallanan koyu çizgiler, kenarları açık (yüzün üstüne biner).</summary>
        public static Sprite Crack
        {
            get
            {
                if (_crack) return _crack;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "crack", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
                var px = new Color32[n * n];
                var rng = new System.Random(9);
                var segs = new List<(Vector2 a, Vector2 b, float w)>();
                var c = new Vector2(0.52f, 0.48f);
                for (int r = 0; r < 7; r++)
                {
                    float ang = r / 7f * Mathf.PI * 2f + (float)rng.NextDouble() * 0.5f;
                    var p = c;
                    for (int s = 0; s < 4; s++)
                    {
                        ang += ((float)rng.NextDouble() - 0.5f) * 0.9f;
                        var q = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (0.08f + (float)rng.NextDouble() * 0.06f);
                        segs.Add((p, q, 0.012f * (1f - s * 0.18f)));
                        if (s == 1 && rng.NextDouble() < 0.6)
                        {
                            float b2 = ang + (rng.NextDouble() < 0.5 ? 0.9f : -0.9f);
                            segs.Add((q, q + new Vector2(Mathf.Cos(b2), Mathf.Sin(b2)) * 0.09f, 0.008f));
                        }
                        p = q;
                    }
                }
                for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    var p = new Vector2((i + 0.5f) / n, (j + 0.5f) / n);
                    float best = 1f, bw = 0.01f;
                    foreach (var sg in segs)
                    {
                        var ab = sg.b - sg.a;
                        float t = Mathf.Clamp01(Vector2.Dot(p - sg.a, ab) / ab.sqrMagnitude);
                        float d = (p - (sg.a + ab * t)).magnitude;
                        if (d / sg.w < best / bw) { best = d; bw = sg.w; }
                    }
                    float x = best / bw;
                    Color col = new Color(0, 0, 0, 0);
                    if (x < 1f) col = new Color(0.12f, 0.08f, 0.10f, 0.85f * (1f - x * 0.5f));
                    else if (x < 2.2f) col = new Color(1f, 1f, 1f, 0.55f * (1f - (x - 1f) / 1.2f));
                    px[j * n + i] = col;
                }
                tex.SetPixels32(px);
                tex.Apply(true, true);
                return _crack = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
        }
    }
}
