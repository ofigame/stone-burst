using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Patlatılması yasak yığının (kural "protect") işareti (kullanıcı, 2026-11-21: "Patlatılması yasak olan iconunu
    /// değiştirelim ipi kıvılcım saçan bomba koyalım hareketli olsun icon"; "bomba kızıl renkte olsun"). Kodla çizilir: parlak kızıl küre, altın
    /// ağız halkası ve kıvrık ip fitil. Fitilin ucundaki kıvılcım ve saçılan çakmalar StackView'de canlandırılır
    /// (<see cref="FuseTip"/>). Mağazadaki bomba güçlendiricisinden ayrı görünsün diye renkleri farklı.
    /// </summary>
    public static class FuseBombArt
    {
        private const int N = 256;
        private static Sprite _body;

        /// <summary>Fitil ucunun sprite merkezine göre yeri (sprite genişliği oranı, -0.5..0.5).</summary>
        public static readonly Vector2 FuseTip = new Vector2(0.30f, 0.43f);

        public static Sprite Body => _body != null ? _body : (_body = Build());

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

        private static Sprite Build()
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "fuse_bomb", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[N * N];
            float aa = 1.4f / N;
            float Cov(float d) => Mathf.Clamp01(0.5f - d / aa);
            // Gövde merkezi biraz aşağıda (fitile yer kalsın).
            const float bx = -0.03f, by = -0.08f, br = 0.34f;
            var navy = new Color32(0x12, 0x2E, 0x6E, 0xFF);
            // Kullanıcı (2026-11-21): "bomba kızıl renkte olsun".
            var deep = new Color(0.36f, 0.03f, 0.05f);
            var mid = new Color(0.93f, 0.20f, 0.14f);
            var gold = new Color(1f, 0.78f, 0.28f);
            var goldDark = new Color(0.72f, 0.46f, 0.12f);
            var rope = new Color(0.80f, 0.62f, 0.38f);
            var ropeDark = new Color(0.52f, 0.36f, 0.20f);
            // Fitil: ağızdan sağ üste kıvrık eğri (ikinci dereceden Bézier örneklenir).
            const int Steps = 24;
            var fuse = new Vector2[Steps + 1];
            Vector2 p0 = new Vector2(0.14f, 0.21f), p1 = new Vector2(0.17f, 0.42f), p2 = new Vector2(FuseTip.x, FuseTip.y);
            for (int i = 0; i <= Steps; i++)
            {
                float t = i / (float)Steps;
                fuse[i] = (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;
            }
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float x = (i + 0.5f) / N - 0.5f, y = (j + 0.5f) / N - 0.5f;
                var c = Color.clear;
                // Yumuşak gölge.
                float sh = Mathf.Sqrt((x - bx - 0.02f) * (x - bx - 0.02f) + (y - by + 0.03f) * (y - by + 0.03f) * 1.1f) - br;
                c = Over(c, new Color(0f, 0f, 0.1f, 0.35f), Mathf.Clamp01(0.5f - sh / 0.05f));
                // Fitil (ip): koyu dış hat + açık iç, çapraz örgü çizgileri.
                float fd = 10f;
                float along = 0f;
                for (int k = 0; k < Steps; k++)
                {
                    float d = Seg(x, y, fuse[k].x, fuse[k].y, fuse[k + 1].x, fuse[k + 1].y);
                    if (d < fd) { fd = d; along = k; }
                }
                c = Over(c, navy, Cov(fd - 0.034f));
                c = Over(c, ropeDark, Cov(fd - 0.026f));
                float braid = 0.5f + 0.5f * Mathf.Sin(along * 2.1f + (x + y) * 60f);
                c = Over(c, Color.Lerp(ropeDark, rope, 0.45f + 0.55f * braid), Cov(fd - 0.018f));
                // Gövde: lacivert dış hat, koyu küre, sol üstten ışık.
                float r = Mathf.Sqrt((x - bx) * (x - bx) + (y - by) * (y - by));
                c = Over(c, navy, Cov(r - br - 0.014f));
                float lx = (x - bx) / br, ly = (y - by) / br;
                float light = Mathf.Clamp01(0.55f - 0.45f * lx + 0.45f * ly);
                var body = Color.Lerp(deep, mid, light * light);
                c = Over(c, body, Cov(r - br));
                // Parlak yansıma (sol üst) ve alt kenarda sıcak ışık.
                float hl = Mathf.Sqrt((x - bx + 0.12f) * (x - bx + 0.12f) * 1.3f + (y - by - 0.14f) * (y - by - 0.14f) * 2.2f) - 0.075f;
                c = Over(c, new Color(1f, 0.95f, 0.9f, 0.85f), Cov(hl) * (r < br ? 1f : 0f));
                float rim = Mathf.Abs(r - br + 0.03f) - 0.012f;
                if (y < by - 0.12f) c = Over(c, new Color(1f, 0.62f, 0.35f, 0.5f), Cov(rim) * (r < br ? 1f : 0f));
                // Altın ağız halkası (sağ üstte, eğik kısa silindir).
                float ang = -0.6f, ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                float mx = x - 0.13f, my = y - 0.19f;
                float ux = mx * ca - my * sa, uy = mx * sa + my * ca;
                float cap = Mathf.Max(Mathf.Abs(ux) - 0.075f, Mathf.Abs(uy) - 0.045f);
                c = Over(c, navy, Cov(cap - 0.012f));
                c = Over(c, Color.Lerp(goldDark, gold, Mathf.Clamp01(0.5f + uy * 8f)), Cov(cap));
                float capTop = Mathf.Sqrt(ux * ux / (0.075f * 0.075f) + (uy - 0.045f) * (uy - 0.045f) / (0.022f * 0.022f)) - 1f;
                c = Over(c, new Color(1f, 0.92f, 0.6f), Cov(capTop * 0.02f));
                px[j * N + i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            var s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            s.name = tex.name;
            return s;
        }
    }
}
