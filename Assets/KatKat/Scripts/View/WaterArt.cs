using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Su kipi görselleri (2026-11-25, kullanıcı: "Gerçekten hücrenin dibinde su olacak … hücre dibindeki su tepsinin içine akacak ve
    /// tepsinin dibinden yukarıya doğru su akıttıkça artacak"). Kodla çizilir: hücre dibindeki su birikintisi, kabarcık, köpük.
    /// Tepsideki su ve akan şerit <see cref="BoardView"/>'de ağ olarak çizilir (<see cref="Material"/>).
    /// </summary>
    public static class WaterArt
    {
        public static readonly Color Surface = new Color(0.55f, 0.90f, 1f), Deep = new Color(0.06f, 0.36f, 0.86f);
        private static Sprite _puddle, _bubble;
        private static Material _mat;

        public static Sprite Puddle => _puddle ? _puddle : (_puddle = MakePuddle());
        public static Sprite Bubble => _bubble ? _bubble : (_bubble = MakeBubble());

        // ---- Oyuktaki su (2026-11-25, kullanıcı: "Su taşın ortasında resim gibi duruyor. Ben o boşluk şeklin su dolmasını istiyorum
        // doğal görünecek"; "su orada canlı olsun") -----------------------------------------------------------------------------
        // Taş görselindeki (PremiumV6/stones.png) oyuk şekiller ölçülerek tanımlı (birim: taş yüzü, merkez 0, y yukarı). Su oyuğu
        // kenarına kadar doldurur; kontur yok. Yarı saydam (oyuğun dibi altından görünür), kenarda hafif koyu (derinlik), üstten
        // bakılan havuz gibi kayan ışık ağı (kostik) ve parıltı. Kare: ışık evresi.

        public const int FillFrames = 16;
        private static readonly System.Collections.Generic.Dictionary<int, Sprite> _fills = new System.Collections.Generic.Dictionary<int, Sprite>();
        private static readonly System.Collections.Generic.Dictionary<int, float[]> _fillBase = new System.Collections.Generic.Dictionary<int, float[]>();
        /// <summary>Oyuğun dikey sınırları (doku v: −0,5..0,5): alt, üst.</summary>
        private static readonly System.Collections.Generic.Dictionary<int, Vector2> _fillBounds = new System.Collections.Generic.Dictionary<int, Vector2>();
        private const int FillN = 128;
        /// <summary>Atlas hücresi 512 px, taş yüzü ortadaki 496 px: hücre biriminden yüz birimine.</summary>
        private const float CellToFace = 512f / 496f * 0.95f; // oyuğun çeperine tam otursun (%5 geniş)

        public static Sprite SymbolFill(KatKat.Core.LayerColor color, int units, int frame)
        {
            frame = ((frame % FillFrames) + FillFrames) % FillFrames;
            int key = (int)color * FillFrames + frame;
            if (_fills.TryGetValue(key, out var sp) && sp) return sp;
            sp = MakeFill(color, frame);
            _fills[key] = sp;
            return sp;
        }

        /// <summary>Oyuğun içi mi (hücre birimi: merkez 0, y yukarı; görselden ölçüldü, kenar çizgisinin biraz içi).</summary>
        private static bool Hollow(KatKat.Core.LayerColor c, float x, float y)
        {
            switch (c)
            {
                case KatKat.Core.LayerColor.Coral: { float dx = x + 0.008f, dy = y - 0.03f; return dx * dx + dy * dy < 0.245f * 0.245f; }
                case KatKat.Core.LayerColor.Blue: // üçgen: tepe (0, 0.265), taban y = −0.17, taban yarı genişliği 0.255 (köşeler yuvarlak)
                {
                    if (y < -0.17f || y > 0.265f) return false;
                    float half = 0.255f * (0.265f - y) / 0.435f;
                    return Mathf.Abs(x) < half - 0.012f;
                }
                case KatKat.Core.LayerColor.Purple: return Mathf.Abs(x) / 0.258f + Mathf.Abs(y - 0.023f) / 0.275f < 0.97f;
                case KatKat.Core.LayerColor.Navy:
                {
                    float dx = x, dy = y - 0.012f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Atan2(dx, dy);
                    float seg = Mathf.PI * 2f / 5f;
                    float t = Mathf.Abs(Mathf.Repeat(a + seg * 0.5f, seg) - seg * 0.5f) / (seg * 0.5f); // 0 uç, 1 iç köşe
                    // Kenarlar düz: uç ile iç köşe arası doğrusal yarıçap (biraz kavisli).
                    float rr = Mathf.Lerp(0.272f, 0.122f, Mathf.Pow(t, 0.85f));
                    return r < rr;
                }
                case KatKat.Core.LayerColor.Green: // yaprak: 45° eğik mercek
                {
                    float ca = 0.7071f;
                    float u = (x + y - 0.01f) * ca, v = (y - x - 0.01f) * ca;
                    float hl = 0.34f, hw = 0.16f;
                    return Mathf.Abs(u) < hl && Mathf.Abs(v) < hw * (1f - (u / hl) * (u / hl));
                }
                default: // inci: iki dalga oyuğu
                {
                    if (x < -0.31f || x > 0.30f) return false;
                    for (int i = 0; i < 2; i++)
                    {
                        float c0 = i == 0 ? 0.125f : -0.045f;
                        float tt = (x + 0.31f) / 0.61f;
                        float yc = c0 + 0.028f * Mathf.Sin(tt * Mathf.PI * 2f + 0.6f) + 0.05f * Mathf.Pow(tt, 4f);
                        float th = 0.03f * Mathf.Sin(Mathf.Clamp01(tt) * Mathf.PI) + 0.004f;
                        if (Mathf.Abs(y - yc) < th) return true;
                    }
                    return false;
                }
            }
        }

        /// <summary>Rengin sabit katmanı: [kaplama, kenar yakınlığı] (piksel başına iki değer); bir kez hesaplanır.</summary>
        private static float[] FillBase(KatKat.Core.LayerColor color)
        {
            if (_fillBase.TryGetValue((int)color, out var b)) return b;
            const int N = FillN, SS = 3;
            b = new float[N * N * 2];
            float px = 1f / N;
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < SS; sy++)
                for (int sx = 0; sx < SS; sx++)
                {
                    float fu = ((x + (sx + 0.5f) / SS) / N - 0.5f) / CellToFace, fv = ((y + (sy + 0.5f) / SS) / N - 0.5f) / CellToFace;
                    if (Hollow(color, fu, fv)) inside++;
                }
                float cov = inside / (float)(SS * SS);
                float edge = 0f;
                if (cov > 0f)
                {
                    // Kenara yakınlık: 8 yönde 2 ve 4 piksel ötesi oyuğun dışındaysa koyulaşır.
                    float cu = ((x + 0.5f) / N - 0.5f) / CellToFace, cv = ((y + 0.5f) / N - 0.5f) / CellToFace;
                    int outN = 0;
                    for (int d = 0; d < 8; d++)
                    {
                        float ang = d * Mathf.PI / 4f;
                        for (int s = 1; s <= 2; s++)
                            if (!Hollow(color, cu + Mathf.Cos(ang) * px * 2.5f * s, cv + Mathf.Sin(ang) * px * 2.5f * s)) outN++;
                    }
                    edge = outN / 16f;
                }
                b[(y * N + x) * 2] = cov;
                b[(y * N + x) * 2 + 1] = edge;
            }
            float lo = 1f, hi = -1f;
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                if (b[(y * N + x) * 2] > 0.5f) { float v = (y + 0.5f) / N - 0.5f; lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v); }
            _fillBounds[(int)color] = new Vector2(lo, hi);
            _fillBase[(int)color] = b;
            return b;
        }

        private static Sprite MakeFill(KatKat.Core.LayerColor color, int frame)
        {
            const int N = FillN;
            var b = FillBase(color);
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "HollowWater" };
            var px = new Color[N * N];
            float ph = frame / (float)FillFrames * Mathf.PI * 2f;
            // 2026-11-25 (kullanıcı: "Yarısı dolu olsun şeklin"): su oyuğun alt yarısında; yüzey çalkalanır ve dalgalanır, üstü boş oyuk.
            var bnd = _fillBounds[(int)color];
            float mid = Mathf.Lerp(bnd.x, bnd.y, 0.5f);
            var deep = new Color(0.03f, 0.40f, 0.97f);
            var shallow = new Color(0.38f, 0.86f, 1f);
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float cov = b[(y * N + x) * 2];
                if (cov <= 0f) { px[y * N + x] = new Color(0, 0, 0, 0); continue; }
                float edge = b[(y * N + x) * 2 + 1];
                float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N - 0.5f;
                float surf = mid + 0.16f * Mathf.Sin(ph) * u + 0.012f * Mathf.Sin(u * 30f + ph * 2f) + 0.006f * Mathf.Sin(u * 61f - ph * 3f);
                float wet = Mathf.Clamp01((surf - v) * N + 0.5f);
                float meniscus = Mathf.Clamp01(1f - Mathf.Abs(v - surf) * N / 1.6f);
                if (wet <= 0f && meniscus <= 0f) { px[y * N + x] = new Color(0, 0, 0, 0); continue; }
                // Su gövdesi: ortası açık, kenara doğru derin (oyuğun çeperi); hafif saydam (taşın dibi altından okunur).
                var c = Color.Lerp(shallow, deep, 0.35f + 0.55f * edge);
                float a = 0.80f + 0.15f * edge;
                // Kayan ışık ağı (üstten bakılan havuz): iki dalga ağının parlak düğümleri, kare evresiyle kayar.
                float k1 = Mathf.Sin(u * 38f + v * 14f + ph) + Mathf.Sin(u * -17f + v * 33f + ph * 1f + 1.3f);
                float k2 = Mathf.Sin(u * 25f - v * 29f - ph + 2.1f);
                float caustic = Mathf.Clamp01((k1 + k2) * 0.4f - 0.3f);
                c = Color.Lerp(c, Color.white, caustic * 0.9f * (1f - edge * 0.5f));
                a = Mathf.Max(a, caustic * 0.85f);
                // Kenarın ışıklı yanı (ışık sol üstten): çeperde ince parlak çizgi (menisküs).
                float lit = Mathf.Clamp01((-u + v) * 2.2f + 0.2f);
                float rim = Mathf.Clamp01((edge - 0.45f) * 3f) * lit;
                c = Color.Lerp(c, new Color(0.9f, 0.98f, 1f), rim * 0.7f);
                // Gezinen parıltı.
                float gx = (u - (-0.06f + 0.05f * Mathf.Sin(ph))) / 0.05f, gy = (v - (mid - 0.04f)) / 0.025f;
                float glint = Mathf.Clamp01(1f - gx * gx - gy * gy);
                c = Color.Lerp(c, Color.white, glint * 0.8f);
                a = Mathf.Max(a, glint * 0.9f);
                // Yüzey çizgisi (menisküs): beyaz, tam opak.
                c = Color.Lerp(c, Color.white, meniscus * 0.9f);
                c.a = Mathf.Max(a * wet, meniscus * 0.95f) * cov;
                px[y * N + x] = c;
            }
            t.SetPixels(px);
            return Finish(t);
        }

        /// <summary>Tepsi suyu ve akan şerit: köşe renkli, saydam, aydınlatmasız.</summary>
        public static Material Material
        {
            get
            {
                if (_mat) return _mat;
                var sh = Shader.Find("Sprites/Default");
                _mat = new Material(sh) { name = "KatKatWater", hideFlags = HideFlags.DontSave };
                _mat.mainTexture = Texture2D.whiteTexture;
                return _mat;
            }
        }

        private static Sprite Finish(Texture2D t)
        {
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.width);
        }

        /// <summary>
        /// Hücre dibindeki su: yuvarlatılmış kare, kenarda koyu mavi derinlik, içte açık turkuaz, üst kenarda ışık çizgisi ve iki
        /// dalga kırışığı. Yığının altından taşan kenarı görünür (tepsi açık renkli; mavi kontrast verir).
        /// </summary>
        private static Sprite MakePuddle()
        {
            const int N = 128;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "WaterPuddle" };
            var px = new Color[N * N];
            float r = 0.2f;
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                float qx = Mathf.Max(Mathf.Abs(u - 0.5f) - (0.5f - r), 0f), qy = Mathf.Max(Mathf.Abs(v - 0.5f) - (0.5f - r), 0f);
                float dist = Mathf.Sqrt(qx * qx + qy * qy) - r; // < 0 içeride
                float inside = -dist * N;
                float a = Mathf.Clamp01(inside + 0.5f);
                if (a <= 0f) { px[y * N + x] = new Color(0, 0, 0, 0); continue; }
                float edge = Mathf.Clamp01(1f - inside / 14f); // kenara yakın koyu
                var c = Color.Lerp(Surface, Deep, edge * 0.9f + (1f - v) * 0.15f);
                // Üst kenar ışığı ve kırışıklar.
                float rim = Mathf.Clamp01(1f - Mathf.Abs(inside - 4f) / 2.5f) * Mathf.Clamp01((v - 0.45f) * 3f);
                c = Color.Lerp(c, Color.white, rim * 0.8f);
                float wave = Mathf.Sin(u * 18f + Mathf.Sin(v * 9f) * 1.5f) * 0.5f + 0.5f;
                float ripple = Mathf.Clamp01(1f - Mathf.Abs(v - 0.30f - 0.04f * Mathf.Sin(u * 12f)) / 0.02f) + Mathf.Clamp01(1f - Mathf.Abs(v - 0.70f - 0.04f * Mathf.Sin(u * 10f + 1f)) / 0.018f);
                c = Color.Lerp(c, Color.white, ripple * 0.35f * (1f - edge) + wave * 0.05f);
                c.a = a * Mathf.Lerp(0.95f, 0.85f, 1f - edge);
                px[y * N + x] = c;
            }
            t.SetPixels(px);
            return Finish(t);
        }

        /// <summary>Kabarcık: ince açık halka, sol üstte parıltı, içi çok saydam.</summary>
        private static Sprite MakeBubble()
        {
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "WaterBubble" };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N - 0.5f;
                float d = Mathf.Sqrt(u * u + v * v) * N; // piksel
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 27f) / 2.5f);
                float fill = Mathf.Clamp01(27f - d) * 0.18f;
                float hx = (u + 0.14f) / 0.08f, hy = (v - 0.15f) / 0.06f;
                float hl = Mathf.Clamp01(1f - (hx * hx + hy * hy));
                float a = Mathf.Max(ring * 0.9f, Mathf.Max(fill, hl));
                px[y * N + x] = new Color(1f, 1f, 1f, a);
            }
            t.SetPixels(px);
            return Finish(t);
        }
    }
}
