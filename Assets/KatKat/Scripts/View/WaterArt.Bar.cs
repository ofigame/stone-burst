using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Görev kartındaki su ve uçan su görselleri (2026-11-26, kullanıcı: "Görev çubuğundaki su efekti kötü ve sorunlu duruyor. İyice
    /// analiz et ve düzelt kusursuz bir görünüm olsun"; "Suyun hücreden akması ve kovadan görev çubuğuna giden suda da sorun var").
    /// <para>Kart suyu tek parça: kartın oranında kapsül (kenar yumuşatılmış, maskesiz), içinde dikey derinlik (dipte koyu, üstte açık),
    /// üst kenarda ince yüzey ışığı ve altta hafif gölge. Dolum ucu aynı degradede, yumuşak sinüs kenarlı (dikey kayar).
    /// Damla ve su topu: hafif saydam küre, kenarı koyu (kırılma), sol üstte parlak benek.</para>
    /// </summary>
    public static partial class WaterArt
    {
        private static Sprite _droplet;
        private static Texture2D _barEdge;
        private static float _bodyAspect = -1f;
        private static Sprite _barBody;

        /// <summary>Kart suyunun rengi y'de (0 dip, 1 üst): dipte derin mavi, ortada canlı mavi, üstte açık turkuaz.</summary>
        public static Color BarColor(float v)
        {
            var deep = new Color(0.05f, 0.36f, 0.86f);
            var mid = new Color(0.16f, 0.58f, 0.98f);
            var top = new Color(0.52f, 0.88f, 1f);
            var c = v < 0.55f ? Color.Lerp(deep, mid, v / 0.55f) : Color.Lerp(mid, top, (v - 0.55f) / 0.45f);
            // Üst kenarda yüzey ışığı (ince açık bant), hemen altında hafif koyu çizgi (derinlik).
            float sheen = Mathf.Clamp01(1f - Mathf.Abs(v - 0.86f) / 0.05f);
            c = Color.Lerp(c, new Color(0.88f, 0.98f, 1f), sheen * 0.75f);
            return c;
        }

        /// <summary>Kart suyu gövdesi: verilen en/boy oranında kapsül, kenar yumuşak (Filled/Horizontal ile soldan dolar).</summary>
        public static Sprite BarBody(float aspect)
        {
            if (_barBody && Mathf.Abs(aspect - _bodyAspect) < 0.02f) return _barBody;
            int h = 96, w = Mathf.Clamp(Mathf.RoundToInt(h * aspect), h, 1536);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "WaterBarBody", hideFlags = HideFlags.DontSave };
            var px = new Color[w * h];
            float r = h * 0.5f;
            var rows = new Color[h];
            for (int y = 0; y < h; y++) rows[y] = BarColor((y + 0.5f) / h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, r, w - r);
                float dx = x + 0.5f - cx, dy = y + 0.5f - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d + 0.5f);
                var c = rows[y];
                c.a = a;
                px[y * w + x] = c;
            }
            t.SetPixels(px);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.Apply(false, true);
            if (_barBody) Object.Destroy(_barBody.texture);
            _bodyAspect = aspect;
            return _barBody = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Dolum ucu: gövdeyle aynı dikey degrade, sağ kenarı yumuşak sinüs; kenarda ince açık çizgi (menisküs). Dikey döşenir.</summary>
        public static Texture2D BarEdge
        {
            get
            {
                if (_barEdge) return _barEdge;
                const int W = 32, H = 96;
                var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "WaterBarEdge" };
                for (int y = 0; y < H; y++)
                {
                    float v = (y + 0.5f) / H;
                    float ph = v * Mathf.PI * 2f;
                    float edge = W * (0.5f + 0.16f * Mathf.Sin(ph * 2f) + 0.05f * Mathf.Sin(ph * 4f + 1.2f));
                    var body = BarColor(v);
                    for (int x = 0; x < W; x++)
                    {
                        float d = edge - (x + 0.5f);
                        var c = body;
                        float meniscus = Mathf.Clamp01(1f - Mathf.Abs(d - 1.6f) / 1.6f);
                        c = Color.Lerp(c, new Color(0.9f, 0.99f, 1f), meniscus * 0.8f);
                        c.a = Mathf.Clamp01(d + 0.8f) * Mathf.Clamp01((x + 0.5f) / (W * 0.35f)); // sol yarı gövdeye karışır
                        t.SetPixel(x, y, c);
                    }
                }
                t.wrapMode = TextureWrapMode.Repeat;
                t.filterMode = FilterMode.Bilinear;
                t.Apply(false, true);
                return _barEdge = t;
            }
        }

        /// <summary>Damla / su topu: saydam mavi küre, kenarda koyu kırılma halkası, sol üstte beyaz benek, sağ altta ince yansıma.</summary>
        public static Sprite Droplet
        {
            get
            {
                if (_droplet) return _droplet;
                const int N = 96;
                var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "WaterDroplet" };
                for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Clamp01((1f - d) * N * 0.5f);
                    if (a <= 0f) { t.SetPixel(x, y, new Color(0, 0, 0, 0)); continue; }
                    var c = Color.Lerp(new Color(0.55f, 0.88f, 1f), new Color(0.10f, 0.45f, 0.95f), Mathf.Pow(d, 1.6f));
                    float alpha = Mathf.Lerp(0.55f, 0.95f, Mathf.Pow(d, 2f)); // ortası daha saydam (su)
                    float hx = (u + 0.35f) / 0.22f, hy = (v - 0.38f) / 0.16f;
                    float spec = Mathf.Clamp01(1f - hx * hx - hy * hy);
                    c = Color.Lerp(c, Color.white, spec);
                    alpha = Mathf.Max(alpha, spec);
                    float refl = Mathf.Clamp01(1f - Mathf.Abs(d - 0.72f) / 0.08f) * Mathf.Clamp01(-u - v) * 0f + Mathf.Clamp01(1f - Mathf.Abs(d - 0.74f) / 0.07f) * Mathf.Clamp01((u - v) * 0.8f);
                    c = Color.Lerp(c, new Color(0.85f, 0.97f, 1f), refl * 0.6f);
                    c.a = a * alpha;
                    t.SetPixel(x, y, c);
                }
                t.wrapMode = TextureWrapMode.Clamp;
                t.filterMode = FilterMode.Bilinear;
                t.Apply(false, true);
                return _droplet = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            }
        }
    }
}
