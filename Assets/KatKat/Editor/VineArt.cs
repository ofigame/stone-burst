using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Sarmaşık görselleri (kullanıcı 2026-11-18: ataş "işlev olarak beğeniyorum fakat ataçla çok bağdaştıramıyorum …
    /// Ataç olmasın" → "Sarmaşık"). Görsel üretim aracı olmadığından kodla çizilir: gölgeli boru gövde (ışık sol üstten),
    /// orta damarlı, iki yanı farklı ışıklı yapraklar, kıvrık filizler ve yumuşak gölge.
    /// Çıktı Resources/PremiumV6/tripo: vine-thick (2 vuruş), vine-thin (1 vuruş), vine-leaf (parçacık), vine-flower
    /// (bağlı grup çiçeği, beyaz; renk SpriteRenderer.color ile). Daha iyi bir görsel gelirse aynı adla değiştirilir.
    /// Çalıştırma: tools/unity.ps1 vine-art.
    /// </summary>
    public static class VineArt
    {
        private const int N = 512;
        private const string Dir = "Assets/KatKat/Resources/PremiumV6/tripo";
        private static readonly Vector3 Light = new Vector3(-0.45f, 0.55f, 0.7f).normalized;

        public static void RunBatch()
        {
            try { Generate(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        [MenuItem("KatKat/Sarmaşık görselleri")]
        public static void Generate()
        {
            Save("vine-thick", Thick());
            Save("vine-thin", Thin());
            Save("vine-leaf", LeafOnly());
            Save("vine-flower", Flower());
            AssetDatabase.Refresh();
            foreach (var n in new[] { "vine-thick", "vine-thin", "vine-leaf", "vine-flower" })
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(Dir + "/" + n + ".png");
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = true;
                imp.maxTextureSize = 512;
                imp.SaveAndReimport();
            }
            Debug.Log("[KatKat] sarmaşık görselleri yazıldı");
        }

        // ---- Kompozisyonlar (koordinat: doku birimi, merkez 0, ±0.5; y yukarı) ----------------------------------

        private static Color[] Thick()
        {
            var c = new Canvas();
            var a = Bez(new Vector2(-0.46f, -0.30f), new Vector2(-0.15f, -0.05f), new Vector2(0.05f, -0.38f), new Vector2(0.45f, 0.20f));
            var b = Bez(new Vector2(-0.44f, 0.26f), new Vector2(-0.10f, 0.40f), new Vector2(0.10f, -0.02f), new Vector2(0.42f, -0.32f));
            c.Shadow(() => { Stem(c, a, 0.038f, 0.022f); Stem(c, b, 0.034f, 0.02f); }, LeavesFor(a, 7, 0.29f, 1), LeavesFor(b, 6, 0.27f, -1));
            Stem(c, b, 0.034f, 0.02f);
            Tendril(c, new Vector2(0.30f, 0.06f), 0.07f, 1);
            foreach (var l in LeavesFor(b, 6, 0.27f, -1)) Leaf(c, l);
            Stem(c, a, 0.038f, 0.022f);
            Tendril(c, new Vector2(-0.26f, -0.10f), 0.065f, -1);
            foreach (var l in LeavesFor(a, 7, 0.29f, 1)) Leaf(c, l);
            return c.Px;
        }

        private static Color[] Thin()
        {
            var c = new Canvas();
            var a = Bez(new Vector2(-0.44f, -0.18f), new Vector2(-0.18f, 0.22f), new Vector2(0.14f, -0.26f), new Vector2(0.43f, 0.14f));
            c.Shadow(() => Stem(c, a, 0.034f, 0.022f), LeavesFor(a, 6, 0.28f, 1));
            Stem(c, a, 0.034f, 0.022f);
            Tendril(c, new Vector2(0.18f, -0.02f), 0.06f, 1);
            foreach (var l in LeavesFor(a, 6, 0.28f, 1)) Leaf(c, l);
            return c.Px;
        }

        private static Color[] LeafOnly()
        {
            var c = new Canvas();
            Leaf(c, new LeafDef { Base = new Vector2(-0.32f, -0.30f), Angle = 48f, Len = 0.86f, Wid = 0.40f });
            return c.Px;
        }

        private static Color[] Flower()
        {
            var c = new Canvas();
            for (int k = 0; k < 5; k++)
            {
                float ang = k * 72f + 90f;
                var dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                Petal(c, dir * 0.03f, ang, 0.36f, 0.22f);
            }
            c.Ball(Vector2.zero, 0.09f, new Color(1f, 0.86f, 0.35f), new Color(0.78f, 0.5f, 0.1f));
            return c.Px;
        }

        // ---- Parçalar --------------------------------------------------------------------------------------------

        private struct LeafDef { public Vector2 Base; public float Angle, Len, Wid; }

        private static List<Vector2> Bez(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= 160; i++)
            {
                float t = i / 160f, u = 1f - t;
                pts.Add(u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3);
            }
            return pts;
        }

        /// <summary>Gövde boyunca, iki yana sırayla yapraklar (uçlara doğru küçülür).</summary>
        private static List<LeafDef> LeavesFor(List<Vector2> path, int count, float size, int firstSide)
        {
            var list = new List<LeafDef>();
            for (int k = 0; k < count; k++)
            {
                float t = (k + 0.6f) / (count + 0.4f);
                int i = Mathf.Clamp(Mathf.RoundToInt(t * (path.Count - 1)), 1, path.Count - 2);
                var tan = (path[i + 1] - path[i - 1]).normalized;
                int side = (k % 2 == 0 ? 1 : -1) * firstSide;
                float baseAng = Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg;
                float s = size * (0.8f + 0.35f * Mathf.Sin(t * Mathf.PI));
                list.Add(new LeafDef { Base = path[i], Angle = baseAng + side * (52f + 10f * Mathf.Sin(k * 1.7f)), Len = s, Wid = s * 0.56f });
            }
            return list;
        }

        private static void Stem(Canvas c, List<Vector2> path, float r0, float r1)
        {
            // Boru: en yakın örneğe uzaklık; yükseklikten normal, Lambert + parlama.
            float px = 1f / N;
            float minX = 1f, minY = 1f, maxX = -1f, maxY = -1f;
            foreach (var p in path) { minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y); maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y); }
            float pad = Mathf.Max(r0, r1) + 2f * px;
            c.Each(minX - pad, minY - pad, maxX + pad, maxY + pad, (x, y) =>
            {
                float best = 9f; int bi = 0;
                for (int i = 0; i < path.Count; i++)
                {
                    float d = (path[i].x - x) * (path[i].x - x) + (path[i].y - y) * (path[i].y - y);
                    if (d < best) { best = d; bi = i; }
                }
                float t = bi / (float)(path.Count - 1);
                float r = Mathf.Lerp(r0, r1, t);
                float d0 = Mathf.Sqrt(best);
                float cov = Mathf.Clamp01((r - d0) / px + 0.5f);
                if (cov <= 0f) return Color.clear;
                var off = new Vector2(x, y) - path[bi];
                float q = Mathf.Clamp01(d0 / r);
                var n = new Vector3(off.x / r, off.y / r, Mathf.Sqrt(Mathf.Max(0f, 1f - q * q)));
                n.Normalize();
                float lam = Mathf.Clamp01(Vector3.Dot(n, Light));
                float spec = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(Vector3.Reflect(-Light, n), Vector3.forward)), 18f);
                var col = Color.Lerp(new Color(0.16f, 0.30f, 0.10f), new Color(0.50f, 0.68f, 0.24f), lam);
                // Kabuk dokusu: gövde boyunca hafif halkalar.
                col *= 0.96f + 0.04f * Mathf.Sin(t * 90f);
                col += new Color(1f, 1f, 0.85f) * spec * 0.45f;
                col.a = cov;
                return col;
            });
        }

        private static void Leaf(Canvas c, LeafDef l)
        {
            float px = 1f / N;
            float ca = Mathf.Cos(l.Angle * Mathf.Deg2Rad), sa = Mathf.Sin(l.Angle * Mathf.Deg2Rad);
            float ext = l.Len + 0.02f;
            c.Each(l.Base.x - ext, l.Base.y - ext, l.Base.x + ext, l.Base.y + ext, (x, y) =>
            {
                float dx = x - l.Base.x, dy = y - l.Base.y;
                float u = (dx * ca + dy * sa) / l.Len, v = (-dx * sa + dy * ca) / l.Len;
                if (u < -0.05f || u > 1.02f) return Color.clear;
                // Sap (ilk %12) + sivri uçlu yaprak gövdesi.
                float stalkW = 0.035f;
                float uu = Mathf.Clamp01((u - 0.1f) / 0.9f);
                float w = (l.Wid / l.Len) * 0.5f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(uu, 0.8f)), 0.9f) * (u < 0.1f ? 0f : 1f);
                float edge = Mathf.Max(w, u < 0.14f ? stalkW * 0.5f : 0f);
                float dEdge = (edge - Mathf.Abs(v)) * l.Len;
                float cov = Mathf.Clamp01(dEdge / px + 0.5f);
                if (cov <= 0f) return Color.clear;
                if (w <= 0.0001f || u < 0.1f)
                {
                    var stalk = new Color(0.22f, 0.40f, 0.14f); stalk.a = cov; return stalk;
                }
                float s = Mathf.Clamp01(Mathf.Abs(v) / w);
                // Kubbe + orta damar boyunca kıvrım: iki yarı farklı ışık alır.
                float side = Mathf.Sign(v);
                var n = new Vector3(-sa * side * (0.35f + 0.6f * s), ca * side * (0.35f + 0.6f * s), 1f - 0.45f * s);
                n = new Vector3(n.x * 1f, n.y * 1f, n.z).normalized;
                float lam = Mathf.Clamp01(Vector3.Dot(n, Light));
                var dark = new Color(0.10f, 0.34f, 0.12f);
                var lite = new Color(0.46f, 0.80f, 0.30f);
                var col = Color.Lerp(dark, lite, lam * (0.75f + 0.25f * uu));
                // Orta damar ve yan damarlar.
                float rib = Mathf.Clamp01(1f - Mathf.Abs(v) * l.Len / (1.6f * px));
                col = Color.Lerp(col, new Color(0.62f, 0.84f, 0.40f), rib * 0.5f * (1f - uu * 0.7f));
                float vein = Mathf.Abs(Mathf.Sin((u * 7f - s * 1.6f) * Mathf.PI));
                if (vein < 0.12f && s > 0.15f && s < 0.85f) col = Color.Lerp(col, new Color(0.62f, 0.85f, 0.40f), 0.35f * (1f - vein / 0.12f));
                // Kenar koyulaşması ve parlama.
                col = Color.Lerp(col, new Color(0.06f, 0.22f, 0.08f), Mathf.Pow(s, 6f) * 0.6f);
                float spec = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(Vector3.Reflect(-Light, n), Vector3.forward)), 24f);
                col += new Color(1f, 1f, 0.9f) * spec * 0.35f;
                col.a = cov;
                return col;
            });
        }

        private static void Petal(Canvas c, Vector2 baseP, float angle, float len, float wid)
        {
            float px = 1f / N;
            float ca = Mathf.Cos(angle * Mathf.Deg2Rad), sa = Mathf.Sin(angle * Mathf.Deg2Rad);
            c.Each(baseP.x - len, baseP.y - len, baseP.x + len, baseP.y + len, (x, y) =>
            {
                float dx = x - baseP.x, dy = y - baseP.y;
                float u = (dx * ca + dy * sa) / len, v = (-dx * sa + dy * ca) / len;
                if (u < 0f || u > 1f) return Color.clear;
                float w = (wid / len) * 0.5f * Mathf.Sqrt(Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 1.05f)));
                float cov = Mathf.Clamp01(((w - Mathf.Abs(v)) * len) / px + 0.5f);
                if (cov <= 0f) return Color.clear;
                float s = Mathf.Clamp01(Mathf.Abs(v) / Mathf.Max(1e-4f, w));
                float shade = 0.72f + 0.28f * (1f - s * s) * (0.6f + 0.4f * u);
                var col = new Color(shade, shade, shade * 0.98f, cov);
                return col;
            });
        }

        private static void Tendril(Canvas c, Vector2 center, float radius, int dir)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= 120; i++)
            {
                float t = i / 120f;
                float ang = dir * t * Mathf.PI * 3.2f;
                float r = radius * (1f - 0.75f * t);
                pts.Add(center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r - new Vector2(radius, 0f) * (1 - 2 * Mathf.Clamp01(dir)) * 0f);
            }
            Stem(c, pts, 0.011f, 0.005f);
        }

        // ---- Tuval ----------------------------------------------------------------------------------------------

        private sealed class Canvas
        {
            public readonly Color[] Px = new Color[N * N];

            public void Each(float x0, float y0, float x1, float y1, Func<float, float, Color> f)
            {
                int i0 = Mathf.Clamp(Mathf.FloorToInt((x0 + 0.5f) * N), 0, N - 1), i1 = Mathf.Clamp(Mathf.CeilToInt((x1 + 0.5f) * N), 0, N - 1);
                int j0 = Mathf.Clamp(Mathf.FloorToInt((y0 + 0.5f) * N), 0, N - 1), j1 = Mathf.Clamp(Mathf.CeilToInt((y1 + 0.5f) * N), 0, N - 1);
                for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    var s = f((i + 0.5f) / N - 0.5f, (j + 0.5f) / N - 0.5f);
                    if (s.a <= 0f) continue;
                    int k = j * N + i;
                    var d = Px[k];
                    float a = s.a + d.a * (1f - s.a);
                    if (a <= 0f) continue;
                    Px[k] = new Color((s.r * s.a + d.r * d.a * (1f - s.a)) / a, (s.g * s.a + d.g * d.a * (1f - s.a)) / a, (s.b * s.a + d.b * d.a * (1f - s.a)) / a, a);
                }
            }

            public void Ball(Vector2 c0, float r, Color lite, Color dark)
            {
                float px = 1f / N;
                Each(c0.x - r, c0.y - r, c0.x + r, c0.y + r, (x, y) =>
                {
                    var off = new Vector2(x, y) - c0;
                    float d = off.magnitude;
                    float cov = Mathf.Clamp01((r - d) / px + 0.5f);
                    if (cov <= 0f) return Color.clear;
                    float q = Mathf.Clamp01(d / r);
                    var n = new Vector3(off.x / r, off.y / r, Mathf.Sqrt(Mathf.Max(0f, 1f - q * q))).normalized;
                    var col = Color.Lerp(dark, lite, Mathf.Clamp01(Vector3.Dot(n, Light)));
                    col.a = cov;
                    return col;
                });
            }

            /// <summary>Gölge: şekiller ayrı tuvale çizilir, alfa bulanıklaştırılıp sağ alta kaydırılarak alta konur.</summary>
            public void Shadow(Action stems, params List<LeafDef>[] leaves)
            {
                var keep = (Color[])Px.Clone();
                Array.Clear(Px, 0, Px.Length);
                stems();
                foreach (var g in leaves) foreach (var l in g) Leaf(this, l);
                var alpha = new float[N * N];
                for (int k = 0; k < Px.Length; k++) alpha[k] = Px[k].a;
                alpha = Blur(alpha, 7);
                Array.Copy(keep, Px, Px.Length);
                int ox = 6, oy = -9;
                for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    int si = i - ox, sj = j - oy;
                    if (si < 0 || sj < 0 || si >= N || sj >= N) continue;
                    float a = alpha[sj * N + si] * 0.45f;
                    if (a <= 0f) continue;
                    int k = j * N + i;
                    var d = Px[k];
                    float na = a + d.a * (1f - a);
                    Px[k] = new Color(0.02f, 0.06f, 0.02f, na);
                }
            }

            private static float[] Blur(float[] src, int r)
            {
                var tmp = new float[src.Length];
                var dst = new float[src.Length];
                for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float s = 0f; int n = 0;
                    for (int k = -r; k <= r; k++) { int x = i + k; if (x < 0 || x >= N) continue; s += src[j * N + x]; n++; }
                    tmp[j * N + i] = s / n;
                }
                for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float s = 0f; int n = 0;
                    for (int k = -r; k <= r; k++) { int y = j + k; if (y < 0 || y >= N) continue; s += tmp[y * N + i]; n++; }
                    dst[j * N + i] = s / n;
                }
                return dst;
            }
        }

        private static void Save(string name, Color[] px)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(Path.Combine(Dir, name + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
