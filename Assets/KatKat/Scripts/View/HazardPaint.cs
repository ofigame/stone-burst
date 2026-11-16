using System;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Konsept pilotu karakter görselleri (kullanıcı, 2026-11-15: "tüm eklenen görsellerin oyunumuza uygun tasarımda olması
    /// gerekiyor"; Higgsfield hesabı askıda olduğu için kodla). Oyunun parlak, kabartmalı taş/kupa tarzı: her parça yumuşak
    /// eğimli bir kabartma olarak gölgelenir (sol üstten ışık, parlama noktası, alt kenarda koyulaşma) ve bütün şeklin
    /// altına yumuşak gölge düşer. 256 px, bir kez üretilir. Koordinat: x sağa, y yukarı, [-0.5, 0.5].
    /// </summary>
    public static partial class HazardPaint
    {
        private const int N = 256;
        private static Sprite _raven, _nest, _chevron, _balloon, _balloonDanger, _clock;

        private delegate float Sd(float x, float y);
        private delegate Color Tint(float x, float y);

        private struct Part
        {
            public Sd Shape;
            public Tint Color;
            /// <summary>Kabartma genişliği (kenardan içeri yükselme mesafesi); 0 = düz boya.</summary>
            public float Bevel;
            public float Gloss;
        }

        private static Part P(Sd s, Color c, float bevel = 0.06f, float gloss = 0.6f) => new Part { Shape = s, Color = (x, y) => c, Bevel = bevel, Gloss = gloss };
        private static Part P(Sd s, Tint c, float bevel = 0.06f, float gloss = 0.6f) => new Part { Shape = s, Color = c, Bevel = bevel, Gloss = gloss };

        // ---- Şekiller --------------------------------------------------------------------------------

        private static float Len(float x, float y) => Mathf.Sqrt(x * x + y * y);
        private static Sd Circle(float cx, float cy, float r) => (x, y) => Len(x - cx, y - cy) - r;

        /// <summary>Elips (yaklaşık uzaklık; kenarda doğru, içeride yumuşak).</summary>
        private static Sd Ellipse(float cx, float cy, float rx, float ry, float deg = 0f)
        {
            float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float u = dx * c + dy * s, v = -dx * s + dy * c;
                float k = Len(u / rx, v / ry);
                return (k - 1f) * Mathf.Min(rx, ry);
            };
        }

        private static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay, wx = px - ax, wy = py - ay;
            float t = Mathf.Clamp01((wx * vx + wy * vy) / (vx * vx + vy * vy));
            return Len(wx - vx * t, wy - vy * t);
        }

        private static Sd Capsule(float ax, float ay, float bx, float by, float r) => (x, y) => Seg(x, y, ax, ay, bx, by) - r;

        /// <summary>Yuvarlatılmış üçgen (köşe yarıçapı r).</summary>
        private static Sd Tri(float ax, float ay, float bx, float by, float cx, float cy, float r)
        {
            // Köşe sırası ne olursa olsun içerisi negatif: ağırlık merkezinde kenar işareti pozitifse çevir.
            float mx = (ax + bx + cx) / 3f, my = (ay + by + cy) / 3f;
            float sg = Edge(mx, my, ax, ay, bx, by) > 0f ? -1f : 1f;
            return TriSd(ax, ay, bx, by, cx, cy, r, sg);
        }

        private static Sd TriSd(float ax, float ay, float bx, float by, float cx, float cy, float r, float sg) => (x, y) =>
        {
            float e0 = sg * Edge(x, y, ax, ay, bx, by), e1 = sg * Edge(x, y, bx, by, cx, cy), e2 = sg * Edge(x, y, cx, cy, ax, ay);
            float inside = Mathf.Max(e0, Mathf.Max(e1, e2));
            if (inside < 0) return inside - r;
            float d = Mathf.Min(Seg(x, y, ax, ay, bx, by), Mathf.Min(Seg(x, y, bx, by, cx, cy), Seg(x, y, cx, cy, ax, ay)));
            return d - r;
        };

        /// <summary>Saat yönünde verilen kenarın işaretli uzaklığı (dışarısı pozitif).</summary>
        private static float Edge(float x, float y, float ax, float ay, float bx, float by)
        {
            float nx = by - ay, ny = -(bx - ax), l = Len(nx, ny);
            return ((x - ax) * nx + (y - ay) * ny) / l;
        }

        private static Sd Union(Sd a, Sd b) => (x, y) => Mathf.Min(a(x, y), b(x, y));
        private static Sd Cut(Sd a, Sd b) => (x, y) => Mathf.Max(a(x, y), -b(x, y));
        private static Sd Below(Sd a, float yMax) => (x, y) => Mathf.Max(a(x, y), y - yMax);

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        // ---- Görseller --------------------------------------------------------------------------------

        /// <summary>Sevimli kuzgun (sağa bakar): mor-lacivert gövde, açık göğüs, kanat, kuyruk, turuncu gaga ve ayaklar, iri göz.</summary>
        public static Sprite Raven => _raven ? _raven : _raven = Render("hz-raven-v2",
            P(Capsule(-0.06f, -0.30f, -0.07f, -0.36f, 0.025f), Hex(0xF39A2B), 0.02f),
            P(Capsule(0.06f, -0.30f, 0.07f, -0.36f, 0.025f), Hex(0xF39A2B), 0.02f),
            P(Ellipse(-0.33f, -0.05f, 0.17f, 0.07f, 28f), Hex(0x2B2750), 0.05f),
            P(Ellipse(-0.31f, -0.11f, 0.15f, 0.06f, 12f), Hex(0x332E5E), 0.05f),
            P(Ellipse(-0.02f, -0.08f, 0.28f, 0.22f), Hex(0x423C78), 0.10f),
            P(Ellipse(0.06f, -0.13f, 0.15f, 0.12f), Hex(0x6E68B0), 0.07f, 0.4f),
            P(Ellipse(-0.10f, -0.04f, 0.18f, 0.11f, -18f), Hex(0x2F2A58), 0.07f),
            P(Circle(0.19f, 0.13f, 0.16f), Hex(0x463F80), 0.09f),
            P(Tri(0.30f, 0.19f, 0.47f, 0.11f, 0.30f, 0.06f, 0.018f), Hex(0xF5A533), 0.04f, 0.9f),
            P(Circle(0.23f, 0.17f, 0.062f), Color.white, 0.03f, 0.3f),
            P(Circle(0.245f, 0.165f, 0.033f), Hex(0x161230), 0.015f, 1f),
            P(Circle(0.235f, 0.185f, 0.012f), Color.white, 0f),
            P(Circle(0.16f, 0.07f, 0.026f), new Color(1f, 0.55f, 0.65f, 0.55f), 0f));

        /// <summary>Kuzgun yuvası: örgü çanak (çubuk desenli), koyu iç, iki açık mavi yumurta.</summary>
        public static Sprite Nest => _nest ? _nest : _nest = Render("hz-nest",
            P(Ellipse(0f, 0.03f, 0.42f, 0.13f), Hex(0x7A4C27), 0.05f),
            P(Ellipse(0f, 0.05f, 0.33f, 0.08f), Hex(0x3A2210), 0.03f, 0.1f),
            P(Circle(-0.09f, 0.08f, 0.10f), Hex(0xD6EEF7), 0.09f, 0.8f),
            P(Circle(0.09f, 0.09f, 0.095f), Hex(0xC9E6F3), 0.09f, 0.8f),
            P(Below(Ellipse(0f, -0.02f, 0.44f, 0.24f), 0.05f), (x, y) =>
            {
                float s = Mathf.Sin(46f * (x * 0.7f + y * 1.4f)) * 0.5f + Mathf.Sin(52f * (x * 0.8f - y * 1.2f)) * 0.5f;
                return Color.Lerp(Hex(0x9C6534), Hex(0xC98C4E), 0.5f + 0.5f * s);
            }, 0.08f, 0.35f),
            P(Capsule(-0.40f, 0.04f, -0.10f, -0.02f, 0.02f), Hex(0xB57A41), 0.02f),
            P(Capsule(0.12f, 0.00f, 0.42f, 0.06f, 0.02f), Hex(0xB57A41), 0.02f));

        /// <summary>Yol oku (sağa): beyaz kenarlı altın çift ok; döndürülerek yön gösterir.</summary>
        public static Sprite Chevron => _chevron ? _chevron : _chevron = Render("hz-chevron",
            P(Union(Capsule(-0.20f, 0.20f, 0.10f, 0f, 0.11f), Capsule(-0.20f, -0.20f, 0.10f, 0f, 0.11f)), Color.white, 0.04f, 0.3f),
            P(Union(Capsule(-0.20f, 0.20f, 0.10f, 0f, 0.065f), Capsule(-0.20f, -0.20f, 0.10f, 0f, 0.065f)), Hex(0xF2B22E), 0.05f, 0.9f));

        /// <summary>Parlak balon (mercan kırmızısı), düğüm ve kıvrık ip.</summary>
        public static Sprite Balloon => _balloon ? _balloon : _balloon = MakeBalloon("hz-balloon-v2", Hex(0xF2545B), Hex(0xFF8A80));

        /// <summary>Kaçmak üzere olan balon (altın, uyarı).</summary>
        public static Sprite BalloonDanger => _balloonDanger ? _balloonDanger : _balloonDanger = MakeBalloon("hz-balloon-danger", Hex(0xF2A62E), Hex(0xFFD36E));

        private static Sprite MakeBalloon(string name, Color body, Color light) => Render(name,
            P((x, y) => Seg(x + 0.035f * Mathf.Sin(y * 26f), y, 0f, -0.27f, 0f, -0.48f) - 0.012f, Hex(0x6B4A3A), 0f),
            P(Tri(-0.045f, -0.29f, 0.045f, -0.29f, 0f, -0.21f, 0.012f), body * 0.85f, 0.03f),
            P(Ellipse(0f, 0.08f, 0.28f, 0.33f), (x, y) => Color.Lerp(body, light, Mathf.Clamp01(0.5f - x * 1.2f + y * 0.9f) * 0.6f), 0.16f, 1f),
            P(Ellipse(-0.10f, 0.20f, 0.05f, 0.09f, -25f), new Color(1f, 1f, 1f, 0.75f), 0f));

        /// <summary>Kum saati/saat simgesi (sipariş süresi): krem kadran, lacivert ibreler.</summary>
        public static Sprite Clock => _clock ? _clock : _clock = Render("hz-clock",
            P(Circle(0f, 0f, 0.40f), Hex(0xF2B22E), 0.08f, 0.9f),
            P(Circle(0f, 0f, 0.31f), Hex(0xFFF6E2), 0.04f, 0.3f),
            P(Capsule(0f, 0f, 0f, 0.20f, 0.035f), Hex(0x122E6E), 0.02f),
            P(Capsule(0f, 0f, 0.14f, -0.06f, 0.035f), Hex(0x122E6E), 0.02f),
            P(Circle(0f, 0f, 0.05f), Hex(0xC2412D), 0.02f));

        // ---- Çizici --------------------------------------------------------------------------------

        private static readonly Vector3 Light = new Vector3(-0.45f, 0.6f, 0.66f).normalized;
        private static readonly Vector3 Half = (new Vector3(-0.45f, 0.6f, 0.66f).normalized + Vector3.forward).normalized;

        private static float Fill(float sd) => Mathf.Clamp01(0.5f - sd * N / 1.4f);

        private static float Height(Part p, float x, float y)
        {
            float sd = p.Shape(x, y);
            if (p.Bevel <= 0f) return 0f;
            float t = Mathf.Clamp01(-sd / p.Bevel);
            return Mathf.Sqrt(1f - (1f - t) * (1f - t)) * p.Bevel;
        }

        private static Sprite Render(string name, params Part[] parts)
        {
            var px = new Color[N * N];
            float e = 1f / N;
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float x = (i + 0.5f) / N - 0.5f, y = (j + 0.5f) / N - 0.5f;
                    // Yumuşak gölge: bütün şeklin aşağı kaydırılmış birleşimi.
                    float sh = float.MaxValue;
                    foreach (var p in parts) if (p.Color(0, 0).a > 0.9f) sh = Mathf.Min(sh, p.Shape(x + 0.01f, y + 0.035f));
                    var col = new Color(0.10f, 0.08f, 0.18f, 0.28f * Mathf.Clamp01(0.5f - sh / 0.03f));
                    foreach (var p in parts)
                    {
                        float sd = p.Shape(x, y);
                        float a = Fill(sd);
                        if (a <= 0f) continue;
                        var c = p.Color(x, y);
                        if (p.Bevel > 0f)
                        {
                            float hx = (Height(p, x + e, y) - Height(p, x - e, y)) / (2f * e);
                            float hy = (Height(p, x, y + e) - Height(p, x, y - e)) / (2f * e);
                            var n = new Vector3(-hx, -hy, 1f).normalized;
                            float diff = Mathf.Max(0f, Vector3.Dot(n, Light));
                            float spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(n, Half)), 36f) * p.Gloss;
                            // Alt kenarda hafif koyulaşma (kabartma hissi), üstte aydınlık.
                            float edge = Mathf.Clamp01(-sd / p.Bevel);
                            float shade = 0.62f + 0.48f * diff - 0.12f * (1f - edge);
                            c = new Color(c.r * shade + spec, c.g * shade + spec, c.b * shade + spec, c.a);
                        }
                        a *= c.a;
                        // Üst üste bindirme (önceden çarpılmamış renk).
                        float outA = a + col.a * (1f - a);
                        if (outA > 1e-5f)
                        {
                            col.r = (c.r * a + col.r * col.a * (1f - a)) / outA;
                            col.g = (c.g * a + col.g * col.a * (1f - a)) / outA;
                            col.b = (c.b * a + col.b * col.a * (1f - a)) / outA;
                        }
                        col.a = outA;
                    }
                    col.r = Mathf.Clamp01(col.r); col.g = Mathf.Clamp01(col.g); col.b = Mathf.Clamp01(col.b);
                    px[j * N + i] = col;
                }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            tex.SetPixels(px);
            tex.Apply(true, true);
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        }

        /// <summary>Yakalama ve görsel kontrol: görseli PNG olarak yazar (okunabilir doku gerekmez; yeniden çizer).</summary>
        public static void ExportPng(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            foreach (var (name, make) in new (string, Func<Sprite>)[] { ("raven", () => Raven), ("nest", () => Nest), ("chevron", () => Chevron), ("balloon", () => Balloon), ("balloon-danger", () => BalloonDanger), ("clock", () => Clock), ("rule-exact", () => RuleExact), ("rule-scarce", () => RuleScarce), ("rule-nospecials", () => RuleNoSpecials) })
            {
                var s = make();
                var rt = RenderTexture.GetTemporary(N, N, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(s.texture, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
                t.ReadPixels(new Rect(0, 0, N, N), 0, 0);
                t.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), t.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(t);
            }
        }
    }
}
