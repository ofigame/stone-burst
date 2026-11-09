using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Bütün katman görselleri birkaç koddan üretilmiş gri tonlu dokudan gelir:
    /// yuvarlatılmış kare maskesi, yumuşak gölge ve 2x2 kabartma işaret atlası.
    /// Renk köşe verisiyle verilir; her renk için ayrı varlık yoktur.
    /// </summary>
    public static class ProceduralArt
    {
        public const float CornerRadius = 0.25f;

        private static Texture2D _shapeMask;
        private static Texture2D _shadowMask;
        private static Texture2D _iconAtlas;
        private static Material _layerMaterial;
        private static Material _peelMaterial;
        private static Material _shadowMaterial;

        public static Texture2D ShapeMask => _shapeMask ? _shapeMask : (_shapeMask = BuildShapeMaskV4(512) ?? BuildShapeMask(512));
        public static Texture2D ShadowMask => _shadowMask ? _shadowMask : (_shadowMask = BuildShadowMask(128));
        public static Texture2D IconAtlas => _iconAtlas ? _iconAtlas : (_iconAtlas = BuildIconAtlasV4(512) ?? BuildIconAtlas(512));

        private static Texture2D _stoneAtlas;
        private static bool _stoneAtlasTried;

        /// <summary>
        /// v6 renkli taş atlası (Resources/PremiumV6/stones, tools/ArtV6/prepare.ps1 üretir). 2x2 hücre, IconCell sırası;
        /// her taş hücrenin ortasındaki 496 px. Yoksa null: shader eski maske + renk yoluna döner.
        /// </summary>
        public static Texture2D StoneAtlas
        {
            get
            {
                if (!_stoneAtlasTried) { _stoneAtlasTried = true; _stoneAtlas = Resources.Load<Texture2D>("PremiumV6/stones"); ResourceUsage.Note("PremiumV6/stones"); }
                return _stoneAtlas;
            }
        }

        // ---- v4 taş kaynakları (tiles/face-neutral, tiles/symbol-*) ---------------------------------------------
        // Shader ve soyulma ağı aynı kalır; yalnızca maske ve işaret atlası v4 nötr kaynaklardan üretilir:
        // r = yüzün ışık tonu (kaynak parlaklığından), g = kaynaktaki en parlak parlama, b = hafif ışıltı, a = şekil.
        // Kaynak okunamazsa (Resources yok / isReadable kapalı) koddan üretilen maskeye düşülür.

        private static Texture2D ReadableV4(string key)
        {
            ResourceUsage.Note("PremiumV4/" + key);
            var sp = Resources.Load<Sprite>("PremiumV4/" + key);
            return sp != null && sp.texture != null && sp.texture.isReadable ? sp.texture : null;
        }

        /// <summary>Alfa > 0.5 çekirdek sınırı (yüzün kendisi; yumuşak gölge payı hariç).</summary>
        private static RectInt Core(Texture2D t)
        {
            var px = t.GetPixels32();
            int x0 = t.width, y0 = t.height, x1 = -1, y1 = -1;
            for (int y = 0; y < t.height; y++)
            for (int x = 0; x < t.width; x++)
                if (px[y * t.width + x].a > 128) { x0 = Mathf.Min(x0, x); y0 = Mathf.Min(y0, y); x1 = Mathf.Max(x1, x); y1 = Mathf.Max(y1, y); }
            return x1 < 0 ? new RectInt(0, 0, t.width, t.height) : new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        private static Texture2D BuildShapeMaskV4(int size)
        {
            var src = ReadableV4("tiles/face-neutral");
            if (src == null) return null;
            var core = Core(src);
            // Parlaklık aralığı: yüzün içindeki en koyu/en açık ton → tonu [0.55, 1] aralığına yay.
            float lo = 1f, hi = 0f;
            var samples = new float[size * size];
            var alphas = new float[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var c = src.GetPixelBilinear((core.x + (x + 0.5f) / size * core.width) / src.width, (core.y + (y + 0.5f) / size * core.height) / src.height);
                float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                samples[y * size + x] = lum;
                alphas[y * size + x] = c.a;
                if (c.a > 0.9f) { lo = Mathf.Min(lo, lum); hi = Mathf.Max(hi, lum); }
            }
            // v4 yüz tonu + koddaki parlak silikon lekesi ve ışıltı (onaylı yönün "parlak" yüzeyi; v4 kaynağı daha mat).
            var gloss = BuildShapeMask(size);
            var gpx = gloss.GetPixels();
            UnityEngine.Object.DestroyImmediate(gloss);
            var tex = NewTexture(size, "KatKatShapeMaskV4");
            var px = new Color[size * size];
            float range = Mathf.Max(0.05f, hi - lo);
            for (int i = 0; i < px.Length; i++)
            {
                float n = Mathf.Clamp01((samples[i] - lo) / range);
                float tone = Mathf.Lerp(0.6f, 1f, n);
                float spec = Mathf.Max(Mathf.Clamp01((n - 0.86f) / 0.14f) * 0.4f, gpx[i].g);
                float a = Mathf.Clamp01((alphas[i] - 0.1f) / 0.8f);
                px[i] = new Color(tone, spec, Mathf.Max(n * 0.25f, gpx[i].b), a);
            }
            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        private static Texture2D BuildIconAtlasV4(int cell)
        {
            // Atlas sırası LayerPalette.IconCell ile aynı: (0,0) daire, (1,0) üçgen, (0,1) dalga, (1,1) elmas.
            string[] keys = { "tiles/symbol-circle", "tiles/symbol-triangle", "tiles/symbol-wave", "tiles/symbol-diamond" };
            var srcs = new Texture2D[4];
            for (int i = 0; i < 4; i++) if ((srcs[i] = ReadableV4(keys[i])) == null) return null;
            int size = cell * 2;
            var tex = NewTexture(size, "KatKatIconAtlasV4");
            var px = new Color[size * size];
            for (int c = 0; c < 4; c++)
            {
                var s = srcs[c];
                var core = Core(s);
                // İşaret yüzün ~%52'sini kaplar (oran korunur, hücre ortası).
                float fit = 0.52f / Mathf.Max(core.width, core.height);
                int ox = (c % 2) * cell, oy = (c / 2) * cell;
                for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    float u = ((x + 0.5f) / cell - 0.5f) / fit + core.x + core.width * 0.5f;
                    float v = ((y + 0.5f) / cell - 0.5f) / fit + core.y + core.height * 0.5f;
                    float groove = 0f, glint = 0f;
                    if (u >= core.x && v >= core.y && u < core.xMax && v < core.yMax)
                    {
                        var col = s.GetPixelBilinear(u / s.width, v / s.height);
                        float lum = col.r * 0.3f + col.g * 0.59f + col.b * 0.11f;
                        groove = col.a;
                        glint = col.a * Mathf.Clamp01((lum - 0.72f) / 0.2f);
                    }
                    px[(oy + y) * size + ox + x] = new Color(groove, glint, 0f, 1f);
                }
            }
            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>Statik yığınlar: derinlik yazmaz, çizim sırası sortingOrder ile.</summary>
        public static Material LayerMaterial => _layerMaterial ? _layerMaterial : (_layerMaterial = CreateMaterial(ShapeMask, false, "KatKatLayer"));

        /// <summary>Soyulan yüzey: rulo sarımlarının kendini doğru örtmesi için derinlik yazar.</summary>
        public static Material PeelMaterial => _peelMaterial ? _peelMaterial : (_peelMaterial = CreateMaterial(ShapeMask, true, "KatKatPeel"));

        public static Material ShadowMaterial => _shadowMaterial ? _shadowMaterial : (_shadowMaterial = CreateMaterial(ShadowMask, false, "KatKatShadow"));

        private static Material CreateMaterial(Texture2D mask, bool zWrite, string name)
        {
            var shader = Resources.Load<Shader>("Shaders/KatKatLayer");
            if (shader == null) throw new MissingReferenceException("Resources/Shaders/KatKatLayer.shader bulunamadı.");
            var m = new Material(shader) { name = name, hideFlags = HideFlags.DontSave };
            m.SetTexture("_MainTex", mask);
            m.SetTexture("_IconTex", IconAtlas);
            if (StoneAtlas != null)
            {
                m.SetTexture("_FaceTex", StoneAtlas);
                m.SetFloat("_UseFace", 1f);
            }
            m.SetFloat("_ZWrite", zWrite ? 1f : 0f);
            m.renderQueue = zWrite ? 3100 : 3000;
            return m;
        }

        private static Texture2D NewTexture(int size, string name)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 1,
                hideFlags = HideFlags.DontSave,
            };
        }

        /// <summary>Merkezli yuvarlatılmış karenin işaretli uzaklığı (q ∈ [-0.5, 0.5]).</summary>
        public static float RoundedBoxSdf(Vector2 q, float halfExtent, float radius)
        {
            float dx = Mathf.Abs(q.x) - halfExtent + radius;
            float dy = Mathf.Abs(q.y) - halfExtent + radius;
            float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            return outside + inside - radius;
        }

        /// <summary>
        /// Parlak silikon yüzey maskesi. a: şekil; r: yastık tonu (kenara doğru yuvarlanır, sol üst aydınlık,
        /// sağ alt koyu); g: sol üstte yumuşak parlama lekesi + kenar boyunca ince ışık; b: geniş sol üst ışıltı.
        /// </summary>
        private static Texture2D BuildShapeMask(int size)
        {
            var tex = NewTexture(size, "KatKatShapeMask");
            var px = new Color[size * size];
            float aa = 1.25f / size;
            var lightDir = new Vector2(-0.6f, 0.8f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var q = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
                float sd = RoundedBoxSdf(q, 0.495f, CornerRadius);
                float a = 1f - Ease.Smooth(-aa, aa, sd);
                float inner = -sd;
                var n = q.sqrMagnitude > 1e-6f ? q.normalized : Vector2.zero;
                float lit = Vector2.Dot(n, lightDir);

                // Yastık: iç bölge düz ve aydınlık, son ~%11'de kenara yuvarlanır.
                float bevel = Ease.Smooth(0f, 0.11f, inner);
                float tone = 0.80f + 0.12f * bevel + 0.06f * q.y * 2f * bevel + (1f - bevel) * 0.16f * lit;
                tone -= (1f - bevel) * 0.10f;

                // Parlama lekesi: sol üstte eğik elips, kenarları çok yumuşak.
                var hp = q - new Vector2(-0.15f, 0.27f);
                const float rot = -0.38f;
                var hr = new Vector2(Mathf.Cos(rot) * hp.x - Mathf.Sin(rot) * hp.y, Mathf.Sin(rot) * hp.x + Mathf.Cos(rot) * hp.y);
                float e = new Vector2(hr.x / 0.25f, hr.y / 0.1f).magnitude;
                float spec = Mathf.Pow(1f - Ease.Smooth(0f, 1f, e), 2f) * 0.75f;
                // Kenar boyunca ince ışık (yalnızca ışığa bakan kenar).
                float rim = (1f - Ease.Smooth(0.006f, 0.03f, Mathf.Abs(inner - 0.022f))) * Mathf.Clamp01(lit) * 0.55f;
                float sheen = Mathf.Clamp01(0.55f - (q.x - q.y) * 0.9f) * bevel;

                px[y * size + x] = new Color(Mathf.Clamp01(tone), Mathf.Clamp01(spec + rim), sheen, a);
            }
            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        private static Texture2D BuildShadowMask(int size)
        {
            var tex = NewTexture(size, "KatKatShadowMask");
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var q = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
                float sd = RoundedBoxSdf(q, 0.42f, CornerRadius);
                float a = 1f - Ease.Smooth(-0.06f, 0.075f, sd);
                px[y * size + x] = new Color(1f, 1f, 1f, a * a);
            }
            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        private static Sprite _tray;

        /// <summary>
        /// Tahta tepsisi zemini (9-dilim, beyaz; rengi SpriteRenderer verir). Sol üstten ışıkta üst/sol iç kenarda
        /// yumuşak gölge: zemin çerçevenin içine gömülü görünür.
        /// </summary>
        public static Sprite TraySprite
        {
            get
            {
                if (_tray) return _tray;
                const int s = 160, r = 52;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "KatKatTray", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
                var px = new Color[s * s];
                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    var q = new Vector2((x + 0.5f) / s - 0.5f, (y + 0.5f) / s - 0.5f);
                    float sd = RoundedBoxSdf(q, 0.5f - 1f / s, (float)r / s);
                    float a = 1f - Ease.Smooth(-1f / s, 1f / s, sd);
                    // İç gölge: üst ve sol kenara yakın koyulaşma, alt-sağ kenarda hafif açık kenar.
                    float depth = Ease.Smooth(0f, 0.16f, -sd);
                    var n = q.sqrMagnitude > 1e-6f ? q.normalized : Vector2.zero;
                    float toward = Vector2.Dot(n, new Vector2(-0.6f, 0.8f));
                    float tone = 1f - (1f - depth) * (0.16f + 0.1f * toward);
                    px[y * s + x] = new Color(tone, tone, tone, a);
                }
                tex.SetPixels(px);
                tex.Apply(false, false);
                _tray = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 8, r + 8, r + 8, r + 8));
                _tray.name = "KatKatTray";
                return _tray;
            }
        }

        // ---- İşaret atlası -------------------------------------------------------------

        private static Texture2D BuildIconAtlas(int cell)
        {
            int size = cell * 2;
            var tex = NewTexture(size, "KatKatIconAtlas");
            var px = new Color[size * size];
            float aa = 1.3f / cell;
            const float stroke = 0.032f;
            var hiOffset = new Vector2(-0.018f, 0.018f);
            for (int c = 0; c < 4; c++)
            {
                int ox = (c % 2) * cell, oy = (c / 2) * cell;
                for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    var q = new Vector2((x + 0.5f) / cell - 0.5f, (y + 0.5f) / cell - 0.5f);
                    float groove = Stroke(IconSdf(c, q), stroke, aa);
                    float lit = Stroke(IconSdf(c, q + hiOffset), stroke, aa);
                    float highlight = Mathf.Clamp01(lit - groove);
                    px[(oy + y) * size + ox + x] = new Color(groove, highlight, 0f, 1f);
                }
            }
            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        private static float Stroke(float sd, float halfWidth, float aa)
        {
            return 1f - Ease.Smooth(-aa, aa, Mathf.Abs(sd) - halfWidth);
        }

        /// <summary>0 daire, 1 üçgen, 2 dalga, 3 elmas.</summary>
        private static float IconSdf(int shape, Vector2 q)
        {
            switch (shape)
            {
                case 0: return q.magnitude - 0.2f;
                case 1: return TriangleSdf(new Vector2(q.x, q.y + 0.045f), 0.225f) - 0.025f;
                case 2: return Mathf.Min(WaveSdf(q - new Vector2(0f, 0.07f)), WaveSdf(q + new Vector2(0f, 0.07f)));
                default: return (Mathf.Abs(q.x) + Mathf.Abs(q.y) - 0.235f) * 0.7071f;
            }
        }

        // İnigo Quilez eşkenar üçgen SDF'i; r = yarım kenar.
        private static float TriangleSdf(Vector2 p, float r)
        {
            const float k = 1.7320508f;
            p.x = Mathf.Abs(p.x) - r;
            p.y = p.y + r / k;
            if (p.x + k * p.y > 0f) p = new Vector2(p.x - k * p.y, -k * p.x - p.y) * 0.5f;
            p.x -= Mathf.Clamp(p.x, -2f * r, 0f);
            return -p.magnitude * Mathf.Sign(p.y);
        }

        // ---- Ataş, hasar noktası, yıldız (Faz 5) ----------------------------------------------------

        private static Sprite _clip;
        private static Sprite _dot;
        private static Sprite _star;

        /// <summary>Tek ataş çizimi: gümüş tel, koyu dış çizgi. Kalan hasar ayrıca noktalarla gösterilir.</summary>
        public static Sprite ClipSprite => _clip ? _clip : (_clip = BakedSprite(160, "KatKatClip", q =>
        {
            // Hafif eğik duran ataş.
            const float ang = -0.42f;
            float cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
            var p = new Vector2(cs * q.x - sn * q.y, sn * q.x + cs * q.y);
            float d = PaperclipSdf(p);
            float aa = 1.4f / 160f;
            float fill = 1f - Ease.Smooth(-aa, aa, d - 0.028f);
            float line = 1f - Ease.Smooth(-aa, aa, d - 0.046f);
            float shine = Mathf.Clamp01(1f - Mathf.Abs(d + 0.006f) / 0.012f) * 0.5f;
            var steel = Color.Lerp(new Color32(0xC9, 0xD3, 0xD6, 0xFF), Color.white, shine);
            var c = Color.Lerp(new Color32(0x4C, 0x5E, 0x64, 0xFF), steel, fill);
            c.a = line;
            return c;
        }));

        /// <summary>Kalan ataş hasarı noktası.</summary>
        public static Sprite DotSprite => _dot ? _dot : (_dot = BakedSprite(64, "KatKatDot", q =>
        {
            float r = q.magnitude, aa = 1.4f / 64f;
            var c = Color.Lerp(new Color32(0x24, 0x4E, 0x52, 0xFF), Color.white, Ease.Smooth(0.3f - aa, 0.3f + aa, r));
            c.a = 1f - Ease.Smooth(0.44f - aa, 0.44f + aa, r);
            return c;
        }));

        /// <summary>Dip yıldızı: tahta tabanı, HUD hedefi ve uçuş için tek çizim.</summary>
        public static Sprite StarSprite => _star ? _star : (_star = BakedSprite(128, "KatKatStar", q =>
        {
            float d = Star5Sdf(new Vector2(q.x, q.y + 0.02f), 0.4f, 0.5f) - 0.035f;
            float aa = 1.4f / 128f;
            float inner = 1f - Ease.Smooth(-aa, aa, d + 0.045f);
            float outer = 1f - Ease.Smooth(-aa, aa, d);
            float lit = Mathf.Clamp01(0.5f + (q.y - q.x) * 0.9f);
            var gold = Color.Lerp(new Color32(0xF7, 0xB5, 0x2C, 0xFF), new Color32(0xFF, 0xE0, 0x7A, 0xFF), lit);
            var c = Color.Lerp(new Color32(0xC9, 0x7A, 0x12, 0xFF), gold, inner);
            c.a = outer;
            return c;
        }));

        private static Sprite _disc;

        /// <summary>Krem mühür diski: altın ince kenarlı, sol üstten hafif aydınlık. Zarf mühür taşının zemini.</summary>
        public static Sprite DiscSprite => _disc ? _disc : (_disc = BakedSprite(128, "KatKatDisc", q =>
        {
            float r = q.magnitude, aa = 1.4f / 128f;
            float lit = Mathf.Clamp01(0.5f + (q.y - q.x) * 0.8f);
            var cream = Color.Lerp(new Color32(0xF6, 0xE2, 0xC0, 0xFF), new Color32(0xFF, 0xF6, 0xE6, 0xFF), lit);
            var c = Color.Lerp(cream, new Color32(0xE2, 0xA6, 0x3C, 0xFF), Ease.Smooth(0.4f - aa, 0.4f + aa, r));
            c.a = 1f - Ease.Smooth(0.47f - aa, 0.47f + aa, r);
            return c;
        }));

        private static Sprite BakedSprite(int s, string name, System.Func<Vector2, Color> shade)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                px[y * s + x] = shade(new Vector2((x + 0.5f) / s - 0.5f, (y + 0.5f) / s - 0.5f));
            tex.SetPixels(px);
            tex.Apply(true, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            sp.name = name;
            return sp;
        }

        /// <summary>Klasik ataş teli: iç içe iki U, açık uçlu.</summary>
        private static float PaperclipSdf(Vector2 p)
        {
            float d = Seg(p, new Vector2(-0.12f, -0.26f), new Vector2(-0.12f, 0.2f));
            d = Mathf.Min(d, HalfArc(p, new Vector2(0f, 0.2f), 0.12f, true));
            d = Mathf.Min(d, Seg(p, new Vector2(0.12f, 0.2f), new Vector2(0.12f, -0.2f)));
            d = Mathf.Min(d, HalfArc(p, new Vector2(0.035f, -0.2f), 0.085f, false));
            d = Mathf.Min(d, Seg(p, new Vector2(-0.05f, -0.2f), new Vector2(-0.05f, 0.15f)));
            d = Mathf.Min(d, HalfArc(p, new Vector2(0.005f, 0.15f), 0.055f, true));
            d = Mathf.Min(d, Seg(p, new Vector2(0.06f, 0.15f), new Vector2(0.06f, -0.08f)));
            return d;
        }

        private static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        private static float HalfArc(Vector2 p, Vector2 c, float r, bool top)
        {
            var d = p - c;
            if (top ? d.y >= 0f : d.y <= 0f) return Mathf.Abs(d.magnitude - r);
            return Mathf.Min((d - new Vector2(r, 0f)).magnitude, (d + new Vector2(r, 0f)).magnitude);
        }

        // İnigo Quilez beş köşeli yıldız SDF'i.
        private static float Star5Sdf(Vector2 p, float r, float rf)
        {
            var k1 = new Vector2(0.809016994f, -0.587785252f);
            var k2 = new Vector2(-k1.x, k1.y);
            p.x = Mathf.Abs(p.x);
            p -= 2f * Mathf.Max(Vector2.Dot(k1, p), 0f) * k1;
            p -= 2f * Mathf.Max(Vector2.Dot(k2, p), 0f) * k2;
            p.x = Mathf.Abs(p.x);
            p.y -= r;
            var ba = rf * new Vector2(-k1.y, k1.x) - new Vector2(0f, 1f);
            float h = Mathf.Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0f, r);
            return (p - ba * h).magnitude * Mathf.Sign(p.y * ba.x - p.x * ba.y);
        }

        private static float WaveSdf(Vector2 q)
        {
            const float half = 0.22f, amp = 0.04f, k = 2f * Mathf.PI / 0.29f;
            float x = Mathf.Clamp(q.x, -half, half);
            float y = amp * Mathf.Sin(k * x);
            if (Mathf.Abs(q.x) > half) return new Vector2(q.x - x, q.y - y).magnitude;
            float slope = amp * k * Mathf.Cos(k * x);
            return Mathf.Abs(q.y - y) / Mathf.Sqrt(1f + slope * slope);
        }
    }
}
