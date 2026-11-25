using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Su kovası görselleri (2026-11-26, kullanıcı çizimi: "Su bölümlerini kovaya çeviriyoruz. Ve kova tepsinin alt kısmında bu şekilde
    /// konumlanacak … Bomba tuzaklarında kova yarılacak. Yarık doğal ve gerçekçi olmalı"). Cam kova: suyun seviyesi camdan görünür.
    /// Dokular 256 px kare, alt-orta pivot; gövde tabanı y=0,06, ağız y=0,82 (doku birimi). Kod çizimi; istenirse üretilmiş
    /// görselle değişir.
    /// </summary>
    public static partial class WaterArt
    {
        /// <summary>Kova gövdesi (doku birimi): taban, ağız, taban ve ağız yarı genişliği.</summary>
        // 2026-11-26: kullanıcının cam kova görselinden ölçüldü (docs/varliklar/gorseller/kova/cam-kova.png, 1254 px): taban 0,12, üst
        // çember 0,773; su iç gövdede 0,215–0,625; iç dip yarı genişliği 0,266, çember dışı 0,384.
        public const float BucketBottom = 0.12f, BucketMouth = 0.773f, BucketHalfBottom = 0.266f, BucketHalfMouth = 0.384f;
        public const float WaterInBottom = 0.215f, WaterInTop = 0.625f;
        private static Sprite _bBack, _bFront, _bMask, _glassCrack;

        public static Sprite BucketBack => _bBack ? _bBack : (_bBack = Resources.Load<Sprite>("Bucket/bucket-back") ?? MakeBucket(0));
        public static Sprite BucketFront => _bFront ? _bFront : (_bFront = Resources.Load<Sprite>("Bucket/bucket-front") ?? MakeBucket(1));
        public static Sprite BucketMask => _bMask ? _bMask : (_bMask = Resources.Load<Sprite>("Bucket/bucket-mask") ?? MakeBucket(2));
        /// <summary>Camda darbe çatlağı: merkezden dallanan kırık çizgiler ve kısmi halkalar (pivot merkez).</summary>
        public static Sprite GlassCrack => _glassCrack ? _glassCrack : (_glassCrack = Resources.Load<Sprite>("Bucket/glass-crack") ?? MakeGlassCrack());

        public static float BucketHalfAt(float y) => Mathf.Lerp(BucketHalfBottom, BucketHalfMouth, Mathf.InverseLerp(BucketBottom, BucketMouth, y));

        /// <summary>0 arka iç cam, 1 ön cam + metal çemberler + kulp, 2 iç maske (suyun kırpıldığı gövde).</summary>
        private static Sprite MakeBucket(int kind)
        {
            const int N = 256;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "WaterBucket" + kind };
            var px = new Color[N * N];
            var rimHi = new Color(0.97f, 0.98f, 1f); var rimLo = new Color(0.55f, 0.62f, 0.70f); var line = new Color(0.20f, 0.28f, 0.38f);
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N;
                var c = new Color(0, 0, 0, 0);
                float half = BucketHalfAt(v);
                float din = (half - Mathf.Abs(u)) * N;
                float dv = Mathf.Min((v - BucketBottom) * N, (BucketMouth - v) * N);
                float inside = Mathf.Min(din, dv);
                float rr = 0.07f, ox = Mathf.Abs(u) - (BucketHalfBottom - rr), oy = (BucketBottom + rr) - v;
                if (ox > 0f && oy > 0f) inside = Mathf.Min(inside, (rr - Mathf.Sqrt(ox * ox + oy * oy)) * N);
                float a = Mathf.Clamp01(inside + 0.5f);
                float k = u / Mathf.Max(0.01f, half);
                if (kind == 2) c = new Color(1f, 1f, 1f, Mathf.Clamp01(inside - 3f + 0.5f)); // camın iç yüzü
                else if (kind == 0)
                {
                    if (a > 0f)
                    {
                        // Arka cam: hafif mavi-gri, kenarlara doğru koyulaşır (silindir).
                        float edge = Mathf.Pow(Mathf.Abs(k), 3f);
                        c = Color.Lerp(new Color(0.86f, 0.93f, 0.98f), new Color(0.55f, 0.66f, 0.78f), edge * 0.8f);
                        c.a = a * (0.30f + 0.25f * edge);
                    }
                }
                else
                {
                    if (a > 0f)
                    {
                        // Ön cam: silindir yansıması (solda geniş parlak şerit, sağda ince), kenarda kalınlaşan cam.
                        float shine = Mathf.Clamp01(1f - Mathf.Abs(k + 0.55f) / 0.13f) * 0.55f + Mathf.Clamp01(1f - Mathf.Abs(k - 0.62f) / 0.05f) * 0.35f;
                        float thick = Mathf.Pow(Mathf.Abs(k), 6f) * 0.5f;
                        c = new Color(1f, 1f, 1f, Mathf.Max(shine, thick * 0.6f));
                        if (thick > shine) c = new Color(0.70f, 0.82f, 0.92f, thick * 0.6f);
                        // Metal çemberler: ağız (geniş), alt; ışık sol üstten.
                        bool band = v > BucketMouth - 0.07f || v < BucketBottom + 0.05f;
                        if (band)
                        {
                            float g = v > BucketMouth - 0.07f ? Mathf.InverseLerp(BucketMouth - 0.07f, BucketMouth, v) : Mathf.InverseLerp(BucketBottom, BucketBottom + 0.05f, v);
                            var bc = Color.Lerp(rimLo, rimHi, 0.35f + 0.65f * Mathf.Sin(g * Mathf.PI));
                            bc = Color.Lerp(bc, Color.white, Mathf.Clamp01(1f - Mathf.Abs(k + 0.5f) / 0.18f) * 0.6f);
                            bc = Color.Lerp(bc, rimLo * 0.7f, Mathf.Clamp01((k - 0.3f) / 0.7f) * 0.5f);
                            c = new Color(bc.r, bc.g, bc.b, 1f);
                        }
                        float edgeL = Mathf.Clamp01(1f - (inside - 1f) / 3f);
                        c = Color.Lerp(c, line, edgeL * 0.8f);
                        c.a = Mathf.Max(c.a, edgeL * 0.85f) * a;
                    }
                    // 2026-11-26 (kullanıcı: "Kulp tepsinin üzerinde gelir gereksiz alan kaplar kulpsuz olsun"): kulp yok.
                }
                px[y * N + x] = c;
            }
            t.SetPixels(px);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0f), N);
        }

        /// <summary>
        /// Gerçekçi cam çatlağı: darbe noktasında ezik (beyaz yıldız), oradan zikzaklarla dallanan 7 kırık çizgi (incelerek biter) ve
        /// iki kısmi halka. Çizgilerin bir yanı ışıklı (beyaz), öbür yanı gölgeli (koyu): kırık camın kenar kırılması.
        /// </summary>
        private static Sprite MakeGlassCrack()
        {
            const int N = 192;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "GlassCrack" };
            var rng = new System.Random(11);
            var segs = new System.Collections.Generic.List<Vector4>(); // x0,y0,x1,y1 (px)
            var widths = new System.Collections.Generic.List<Vector2>();
            var c0 = new Vector2(N * 0.5f, N * 0.5f);
            for (int r = 0; r < 7; r++)
            {
                float ang = r / 7f * Mathf.PI * 2f + (float)rng.NextDouble() * 0.5f;
                float len = N * (0.28f + 0.2f * (float)rng.NextDouble());
                var p = c0;
                int steps = 5;
                for (int s = 0; s < steps; s++)
                {
                    float a2 = ang + ((float)rng.NextDouble() - 0.5f) * 0.7f;
                    var q = p + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * (len / steps);
                    float w0 = Mathf.Lerp(2.2f, 0.5f, s / (float)steps), w1 = Mathf.Lerp(2.2f, 0.5f, (s + 1f) / steps);
                    segs.Add(new Vector4(p.x, p.y, q.x, q.y)); widths.Add(new Vector2(w0, w1));
                    // Yan dal.
                    if (s == 2 && rng.NextDouble() < 0.6)
                    {
                        float ab = a2 + (rng.NextDouble() < 0.5 ? 0.9f : -0.9f);
                        var b = q + new Vector2(Mathf.Cos(ab), Mathf.Sin(ab)) * (len * 0.25f);
                        segs.Add(new Vector4(q.x, q.y, b.x, b.y)); widths.Add(new Vector2(1f, 0.4f));
                    }
                    p = q;
                }
            }
            // Kısmi halkalar.
            for (int ring = 0; ring < 2; ring++)
            {
                float rad = N * (ring == 0 ? 0.13f : 0.24f);
                float a0 = (float)rng.NextDouble() * 6.28f, span = 1.6f + (float)rng.NextDouble();
                var prev = c0 + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * rad;
                for (int s = 1; s <= 8; s++)
                {
                    float aa = a0 + span * s / 8f;
                    var q = c0 + new Vector2(Mathf.Cos(aa), Mathf.Sin(aa)) * (rad + ((float)rng.NextDouble() - 0.5f) * 3f);
                    segs.Add(new Vector4(prev.x, prev.y, q.x, q.y)); widths.Add(new Vector2(0.8f, 0.8f));
                    prev = q;
                }
            }
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float best = 99f, side = 0f;
                for (int i = 0; i < segs.Count; i++)
                {
                    var sg = segs[i];
                    var a = new Vector2(sg.x, sg.y); var b = new Vector2(sg.z, sg.w);
                    var ab = b - a;
                    float tt = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                    var cp = a + ab * tt;
                    float d = (p - cp).magnitude - Mathf.Lerp(widths[i].x, widths[i].y, tt) * 0.5f;
                    if (d < best) { best = d; side = Mathf.Sign(ab.x * (p.y - a.y) - ab.y * (p.x - a.x)); }
                }
                float core = Mathf.Clamp01(0.8f - best);
                float halo = Mathf.Clamp01(1f - (best - 0.5f) / 1.8f) * (1f - core);
                var lit = new Color(1f, 1f, 1f, 1f);
                var dark = new Color(0.15f, 0.22f, 0.30f, 1f);
                var col = side > 0f ? lit : dark;
                var c = Color.Lerp(new Color(col.r, col.g, col.b, halo * 0.55f), new Color(0.96f, 0.99f, 1f, 0.95f), core);
                // Darbe noktası: ezik camın beyaz parıltısı.
                float dc = (p - c0).magnitude;
                float crush = Mathf.Clamp01(1f - dc / (N * 0.06f));
                c = Color.Lerp(c, new Color(1f, 1f, 1f, 0.95f), crush * crush);
                px[y * N + x] = c;
            }
            t.SetPixels(px);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        }
    }
}
