using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Çakıl örtüsü ve Kürek görselleri (kullanıcı, 2026-11-21: "Oyun tahtamızın bir bölümünde taşların üzeri çakıl taşları
    /// ile örtülü olacak. Örtülü olmayan alanda kazanılan özel skil ile bu çakıl taşları temizlenecek"). Kodla çizilir:
    /// <list type="bullet">
    /// <item><see cref="Pile"/>: taş yüzünü örten yuvarlatılmış kare çakıl yığını — sıcak gri, bej ve kahve tonlarında kubbe
    /// gölgeli, parlak benekli çakıllar; araları koyu kum. Üç çeşit (komşu hücreler aynı görünmesin).</item>
    /// <item><see cref="Shovel"/>: tepsi simgesi — ahşap saplı, turkuaz ağızlı oyuncak kürek ve önünde birkaç çakıl.</item>
    /// <item><see cref="Pebble"/>: temizlenme efektinde saçılan tek çakıl.</item>
    /// </list>
    /// </summary>
    public static class GravelArt
    {
        private const int N = 256;
        private static readonly Sprite[] _piles = new Sprite[3];
        private static Sprite _shovel, _pebble;

        private static readonly Color[] Stones =
        {
            new Color(0.78f, 0.74f, 0.68f), new Color(0.66f, 0.62f, 0.57f), new Color(0.86f, 0.80f, 0.70f),
            new Color(0.58f, 0.50f, 0.43f), new Color(0.72f, 0.66f, 0.58f), new Color(0.52f, 0.55f, 0.58f),
            new Color(0.90f, 0.86f, 0.79f),
        };

        public static Sprite Pile(int variant)
        {
            int v = ((variant % 3) + 3) % 3;
            if (_piles[v] != null) return _piles[v];
            return _piles[v] = BuildPile(v);
        }

        private struct Rock { public float X, Y, Rx, Ry, Cos, Sin; public Color C; }

        private static Sprite BuildPile(int v)
        {
            var rng = new System.Random(4127 + v * 7919);
            // Titreşimli ızgara: 5×5 çakıl, alttakiler üstte (yukarıdan bakan yığın hissi için y'ye göre sıralı).
            var rocks = new System.Collections.Generic.List<Rock>();
            const int G = 5;
            for (int gy = 0; gy < G; gy++)
            for (int gx = 0; gx < G; gx++)
            {
                float cx = -0.42f + (gx + 0.5f) * (0.84f / G) + (float)(rng.NextDouble() - 0.5) * 0.07f;
                float cy = -0.42f + (gy + 0.5f) * (0.84f / G) + (float)(rng.NextDouble() - 0.5) * 0.07f;
                float r = 0.088f + (float)rng.NextDouble() * 0.03f;
                float ang = (float)(rng.NextDouble() * Mathf.PI);
                rocks.Add(new Rock
                {
                    X = cx, Y = cy, Rx = r * (1.05f + (float)rng.NextDouble() * 0.3f), Ry = r * (0.78f + (float)rng.NextDouble() * 0.2f),
                    Cos = Mathf.Cos(ang), Sin = Mathf.Sin(ang), C = Stones[rng.Next(Stones.Length)] * (0.93f + (float)rng.NextDouble() * 0.12f),
                });
            }
            rocks.Sort((a, b) => b.Y.CompareTo(a.Y)); // yukarıdakiler önce çizilir, aşağıdakiler üstüne biner
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "gravel_pile" + v, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[N * N];
            float aa = 1.4f / N;
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float x = (i + 0.5f) / N - 0.5f, y = (j + 0.5f) / N - 0.5f;
                float body = RoundRect(x, y, 0.485f, 0.485f, 0.15f);
                float cover = Mathf.Clamp01(0.5f - body / aa);
                if (cover <= 0f) { px[j * N + i] = new Color(0, 0, 0, 0); continue; }
                // Koyu kum zemin, kenara doğru kararır (yığın kenarı gölgesi).
                float edge = Mathf.Clamp01(-body / 0.08f);
                var c = Color.Lerp(new Color(0.33f, 0.26f, 0.20f), new Color(0.47f, 0.39f, 0.30f), edge);
                float sand = Hash(i, j, v) * 0.06f;
                c += new Color(sand, sand, sand * 0.8f, 0f);
                // Çakıllar.
                foreach (var r in rocks)
                {
                    float dx = x - r.X, dy = y - r.Y;
                    float u = (dx * r.Cos + dy * r.Sin) / r.Rx, w = (-dx * r.Sin + dy * r.Cos) / r.Ry;
                    float q = u * u + w * w;
                    // Temas gölgesi (sağ alta düşer).
                    float sdx = dx - 0.014f, sdy = dy + 0.018f;
                    float su = (sdx * r.Cos + sdy * r.Sin) / r.Rx, sw = (-sdx * r.Sin + sdy * r.Cos) / r.Ry;
                    float sq = su * su + sw * sw;
                    if (sq < 1.25f && q >= 1f) c = Color.Lerp(c, c * 0.55f, Mathf.Clamp01((1.25f - sq) * 3f) * 0.7f);
                    if (q >= 1f) continue;
                    float rim = Mathf.Clamp01((1f - q) * (r.Rx * N * 0.5f) * 0.35f); // yumuşak kenar
                    // Kubbe normali (ekran uzayında); ışık sol üstten.
                    float nx = dx / r.Rx, ny = dy / r.Ry, nz = Mathf.Sqrt(Mathf.Max(0f, 1f - q));
                    float lambert = Mathf.Clamp01(-nx * 0.45f + ny * 0.55f + nz * 0.72f);
                    var stone = r.C * (0.55f + 0.6f * lambert);
                    // Benekler ve parlak sır.
                    float speck = Hash(i * 3 + 1, j * 5 + 2, v) > 0.93f ? -0.08f : 0f;
                    stone += new Color(speck, speck, speck, 0f);
                    float hx = nx + 0.38f, hy = ny - 0.42f;
                    float spec = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) / 0.34f);
                    stone = Color.Lerp(stone, Color.white, spec * spec * 0.75f);
                    stone.a = 1f;
                    c = Color.Lerp(c, stone, rim);
                }
                // Üst kenarda ince açık hat, alt kenarda koyu (kabarık yığın).
                float lip = Mathf.Exp(-Mathf.Abs(body + 0.012f) * 90f);
                c = Color.Lerp(c, y > 0f ? new Color(1f, 0.95f, 0.86f) : new Color(0.22f, 0.16f, 0.12f), lip * 0.35f);
                c.a = cover;
                px[j * N + i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            sp.name = tex.name;
            return sp;
        }

        /// <summary>Tepsi simgesi: çapraz duran oyuncak kürek (sap sol üstte, ağız sağ altta) ve önünde çakıllar.</summary>
        public static Sprite Shovel => _shovel != null ? _shovel : (_shovel = Make("shovel_icon", ShovelPixel));

        /// <summary>Tek çakıl (efekt parçacığı), açık gri kubbe.</summary>
        public static Sprite Pebble => _pebble != null ? _pebble : (_pebble = Make("gravel_pebble", (x, y) =>
        {
            float q = (x * x) / (0.40f * 0.40f) + (y * y) / (0.30f * 0.30f);
            if (q >= 1f) return Color.clear;
            float nx = x / 0.4f, ny = y / 0.3f, nz = Mathf.Sqrt(1f - q);
            float l = Mathf.Clamp01(-nx * 0.45f + ny * 0.55f + nz * 0.72f);
            var c = new Color(0.74f, 0.69f, 0.62f) * (0.55f + 0.6f * l);
            float hx = nx + 0.38f, hy = ny - 0.42f;
            float s = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) / 0.34f);
            c = Color.Lerp(c, Color.white, s * s * 0.7f);
            c.a = Mathf.Clamp01((1f - q) * 18f);
            return c;
        }));

        private static Color ShovelPixel(float x, float y)
        {
            var c = Color.clear;
            // Eksen: sol üstten sağ alta (−45°). u eksen boyunca (ağız tarafı +), v dik.
            const float k = 0.70710678f;
            float u = (x - y) * k, v = (x + y) * k;
            // Gölge.
            float su = (x - 0.02f - (y + 0.025f)) * k, sv = (x - 0.02f + (y + 0.025f)) * k;
            float shadow = Mathf.Min(Capsule(su + 0.17f, sv, 0.2f, 0.045f), Blade(su - 0.17f, sv));
            c = Over(c, new Color(0.25f, 0.15f, 0.05f, 0.35f), Mathf.Clamp01(0.5f - shadow / 0.03f));
            // Sap (ahşap) ve tutamak.
            float handle = Capsule(u + 0.17f, v, 0.2f, 0.042f);
            c = Over(c, new Color(0.45f, 0.24f, 0.08f), Cov(handle));
            if (handle < -0.01f)
            {
                var wood = Color.Lerp(new Color(0.93f, 0.62f, 0.30f), new Color(0.72f, 0.40f, 0.14f), Mathf.Clamp01(v / 0.04f * 0.5f + 0.5f));
                float grain = Mathf.Sin(u * 90f + Mathf.Sin(v * 40f) * 2f) * 0.04f;
                wood += new Color(grain, grain * 0.7f, 0f, 0f);
                c = Over(c, wood, Cov(handle + 0.012f));
            }
            float grip = Capsule(u + 0.39f, v, 0.035f, 0.07f);
            c = Over(c, new Color(0.62f, 0.10f, 0.12f), Cov(grip));
            c = Over(c, new Color(0.96f, 0.34f, 0.30f), Cov(grip + 0.014f));
            c = Over(c, new Color(1f, 0.8f, 0.75f, 0.8f), Cov(Capsule(u + 0.40f, v + 0.03f, 0.02f, 0.014f)));
            // Ağız (turkuaz metal kürek).
            float blade = Blade(u - 0.17f, v);
            c = Over(c, new Color(0.03f, 0.34f, 0.38f), Cov(blade));
            if (blade < -0.012f)
            {
                float t = Mathf.Clamp01((v + 0.15f) / 0.3f);
                var metal = Color.Lerp(new Color(0.42f, 0.92f, 0.88f), new Color(0.07f, 0.58f, 0.62f), t);
                // Orta oluk.
                float groove = Mathf.Exp(-v * v * 900f) * Mathf.Clamp01((u - 0.08f) * 6f);
                metal = Color.Lerp(metal, new Color(0.04f, 0.44f, 0.48f), groove * 0.6f);
                c = Over(c, metal, Cov(blade + 0.014f));
                float hl = Capsule(u - 0.2f, v + 0.075f, 0.07f, 0.018f);
                c = Over(c, new Color(1f, 1f, 1f, 0.75f), Cov(hl));
            }
            // Ağzın önünde üç çakıl.
            c = PebbleAt(c, x, y, 0.30f, -0.36f, 0.075f, 0.055f, new Color(0.80f, 0.75f, 0.68f));
            c = PebbleAt(c, x, y, 0.15f, -0.41f, 0.06f, 0.045f, new Color(0.62f, 0.56f, 0.50f));
            c = PebbleAt(c, x, y, 0.40f, -0.24f, 0.05f, 0.04f, new Color(0.88f, 0.83f, 0.74f));
            return c;
        }

        /// <summary>Kürek ağzı: ucu sivri yuvarlak kalkan (u ekseni ağız boyu).</summary>
        private static float Blade(float u, float v)
        {
            // Elips gövde + sapa doğru boyun.
            float e = Mathf.Sqrt((u / 0.19f) * (u / 0.19f) + (v / 0.15f) * (v / 0.15f)) - 1f;
            float neck = Capsule(u + 0.17f, v, 0.04f, 0.05f) / 0.15f;
            float d = Mathf.Min(e, neck);
            // Uç: u > 0.12'de iki yandan daralan sivrilik.
            float tip = (Mathf.Abs(v) * 1.1f + (u - 0.12f) * 1.0f) - 0.16f;
            d = Mathf.Max(d, u > 0.05f ? tip / 0.15f : -1f);
            return d * 0.15f;
        }

        private static Color PebbleAt(Color c, float x, float y, float cx, float cy, float rx, float ry, Color col)
        {
            float dx = x - cx, dy = y - cy;
            float q = (dx * dx) / (rx * rx) + (dy * dy) / (ry * ry);
            float sh = ((dx - 0.01f) * (dx - 0.01f)) / (rx * rx) + ((dy + 0.014f) * (dy + 0.014f)) / (ry * ry);
            if (sh < 1.1f) c = Over(c, new Color(0.2f, 0.12f, 0.05f, 0.35f), Mathf.Clamp01((1.1f - sh) * 4f));
            if (q >= 1f) return c;
            float nx = dx / rx, ny = dy / ry, nz = Mathf.Sqrt(1f - q);
            float l = Mathf.Clamp01(-nx * 0.45f + ny * 0.55f + nz * 0.72f);
            var s = col * (0.55f + 0.6f * l);
            float hx = nx + 0.38f, hy = ny - 0.42f;
            float sp = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) / 0.34f);
            s = Color.Lerp(s, Color.white, sp * sp * 0.7f);
            s.a = 1f;
            return Over(c, s, Mathf.Clamp01((1f - q) * 14f));
        }

        // ---- Yardımcılar ------------------------------------------------------------------------------

        private delegate Color Pixel(float x, float y);

        private static Sprite Make(string name, Pixel f)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
                px[j * N + i] = f((i + 0.5f) / N - 0.5f, (j + 0.5f) / N - 0.5f);
            tex.SetPixels32(px);
            tex.Apply(true, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            sp.name = name;
            return sp;
        }

        private const float Aa = 1.2f / N;
        private static float Cov(float d) => Mathf.Clamp01(0.5f - d / Aa);

        private static float RoundRect(float x, float y, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x) - hx + r, qy = Mathf.Abs(y) - hy + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float Capsule(float x, float y, float halfLen, float r)
        {
            float qx = Mathf.Max(Mathf.Abs(x) - halfLen, 0f);
            return Mathf.Sqrt(qx * qx + y * y) - r;
        }

        private static float Hash(int i, int j, int s)
        {
            uint h = (uint)(i * 73856093) ^ (uint)(j * 19349663) ^ (uint)(s * 83492791);
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            return (h & 0xFFFF) / 65535f;
        }

        private static Color Over(Color dst, Color src, float a)
        {
            a *= src.a;
            float outA = a + dst.a * (1f - a);
            if (outA <= 1e-5f) return Color.clear;
            var c = (src * a + dst * dst.a * (1f - a)) / outA;
            c.a = outA;
            return c;
        }
    }
}
