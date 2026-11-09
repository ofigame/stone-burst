using UnityEngine;

namespace KatKat.UI
{
    /// <summary>
    /// v12 deneme kural simgeleri (koddan, 128 px; plan §6.1: az sayıda sabit simge, 9 dilde aynı):
    /// protect (kristal), chainOnly (zincir halkaları), noSpecials (üstü çizili roket), exact (hedef tahtası),
    /// scarce (damla ve eksi). Kural kartında, hedef alanının yanındaki kalıcı simgelerde ve zincir hedefinde kullanılır.
    /// </summary>
    public static class RuleIcons
    {
        private const int N = 128;
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> Cache = new System.Collections.Generic.Dictionary<string, Sprite>();

        public static readonly Color Navy = new Color32(0x12, 0x2E, 0x6E, 0xFF);
        public static readonly Color Teal = new Color32(0x14, 0xB8, 0xC8, 0xFF);
        public static readonly Color Ice = new Color32(0x8F, 0xE4, 0xFF, 0xFF);
        public static readonly Color Red = new Color32(0xF2, 0x4A, 0x4A, 0xFF);

        public static Sprite For(string rule)
        {
            if (Cache.TryGetValue(rule, out var s) && s) return s;
            Pixel f = rule switch
            {
                "protect" => Crystal,
                "chainOnly" => Chain,
                "noSpecials" => NoSpecials,
                "exact" => Target,
                "scarce" => Scarce,
                _ => Target,
            };
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "rule_" + rule, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    float x = (i + 0.5f) / N - 0.5f, y = (j + 0.5f) / N - 0.5f;
                    px[j * N + i] = f(x, y);
                }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            s.name = tex.name;
            return Cache[rule] = s;
        }

        private delegate Color Pixel(float x, float y);
        private const float Aa = 1.5f / N;
        private static float Cov(float d) => Mathf.Clamp01(0.5f - d / Aa);
        private static float Circle(float x, float y, float cx, float cy, float r) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;
        private static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay, wx = px - ax, wy = py - ay;
            float t = Mathf.Clamp01((wx * vx + wy * vy) / (vx * vx + vy * vy));
            float dx = wx - vx * t, dy = wy - vy * t;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
        private static Color Over(Color dst, Color src, float a)
        {
            a *= src.a;
            float o = a + dst.a * (1f - a);
            if (o <= 1e-5f) return Color.clear;
            var c = (src * a + dst * dst.a * (1f - a)) / o;
            c.a = o;
            return c;
        }

        /// <summary>Beyaz daire zemin + lacivert kenar (bütün kural simgelerinde ortak rozet).</summary>
        private static Color Badge(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            var c = Over(Color.clear, Navy, Cov(r - 0.48f));
            return Over(c, Color.white, Cov(r - 0.43f));
        }

        private static Color Crystal(float x, float y)
        {
            var c = Badge(x, y);
            // Elmas: |x|/0.22 + |y-0.03|/0.3 ≤ 1 (üst yüz ayrı ton).
            float d = (Mathf.Abs(x) / 0.22f + Mathf.Abs(y - 0.0f) / 0.32f - 1f) * 0.2f;
            c = Over(c, new Color32(0x1E, 0x7F, 0xC8, 0xFF), Cov(d));
            c = Over(c, Ice, Cov(d + 0.02f));
            if (y > 0.06f) c = Over(c, new Color(1f, 1f, 1f, 0.55f), Cov(d + 0.02f));
            c = Over(c, Color.white, Cov(Circle(x, y, -0.06f, 0.12f, 0.025f)));
            return c;
        }

        private static Color Chain(float x, float y)
        {
            var c = Badge(x, y);
            // İki iç içe halka (çapraz).
            float a = Mathf.Abs(Circle(x * 0.85f, y, -0.1f, 0.06f, 0.14f)) - 0.04f;
            float b = Mathf.Abs(Circle(x * 0.85f, y, 0.1f, -0.06f, 0.14f)) - 0.04f;
            c = Over(c, Teal, Cov(Mathf.Min(a, b)));
            return c;
        }

        private static Color NoSpecials(float x, float y)
        {
            var c = Badge(x, y);
            // Yatay roket gövdesi + uçlar, üstünde kırmızı çapraz çizgi.
            float body = Mathf.Max(Mathf.Abs(x) - 0.2f, Mathf.Abs(y) - 0.07f);
            c = Over(c, Teal, Cov(body));
            float tip = Mathf.Min(Seg(x, y, 0.2f, 0.09f, 0.31f, 0f), Seg(x, y, 0.2f, -0.09f, 0.31f, 0f));
            c = Over(c, Teal, Cov(tip - 0.03f));
            float tip2 = Mathf.Min(Seg(x, y, -0.2f, 0.09f, -0.31f, 0f), Seg(x, y, -0.2f, -0.09f, -0.31f, 0f));
            c = Over(c, Teal, Cov(tip2 - 0.03f));
            c = Over(c, Red, Cov(Seg(x, y, -0.27f, -0.27f, 0.27f, 0.27f) - 0.045f));
            return c;
        }

        private static Color Target(float x, float y)
        {
            var c = Badge(x, y);
            float r = Mathf.Sqrt(x * x + y * y);
            c = Over(c, Red, Cov(Mathf.Abs(r - 0.27f) - 0.045f));
            c = Over(c, Red, Cov(Mathf.Abs(r - 0.15f) - 0.04f));
            c = Over(c, Red, Cov(r - 0.06f));
            return c;
        }

        private static Color Scarce(float x, float y)
        {
            var c = Badge(x, y);
            float drop = Mathf.Min(Circle(x, y, -0.04f, -0.08f, 0.15f), Mathf.Max(Mathf.Abs(x + 0.04f) - (0.2f - (y + 0.08f)) * 0.6f, -(y + 0.08f)));
            drop = Mathf.Max(drop, y - 0.2f);
            c = Over(c, new Color32(0xB0, 0x6C, 0xF0, 0xFF), Cov(drop));
            c = Over(c, Navy, Cov(Mathf.Max(Mathf.Abs(x - 0.2f) - 0.09f, Mathf.Abs(y - 0.17f) - 0.035f)));
            return c;
        }
    }
}
