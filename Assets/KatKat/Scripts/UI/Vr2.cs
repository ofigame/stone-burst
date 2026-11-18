using UnityEngine;

namespace KatKat.UI
{
    /// <summary>
    /// VR2 oyun ekranı (docs/vr2/CLAUDE_CODE.md; kullanıcı 2026-11-14: "VR2 oyun ekranı tasarımını uygula"):
    /// altı temanın tam ekran dokusu, krem üst panel, gömülü hedef yuvası ve tahta iç alanının tema rengi.
    /// Renkler docs/vr2/cerceveler/palet.json'dan aynen; doku seçimi bölgenin (100 bölüm) mevcut tema sırasını izler.
    /// Oyun kuralı, kayıt ve dokunma alanı değiştirmez; yalnız görünüş.
    /// </summary>
    public static class Vr2
    {
        /// <summary>Kapatılırsa ui3 görünümü (Z1–Z5 zemin, fildişi tahta kenarı) aynen geri gelir.</summary>
        public static bool Enabled = true;

        /// <summary>Tam ekran doku bulanık mı (kullanıcı 2026-11-15: "arkaplan blurlu olsun").</summary>
        public static bool BlurBackground = false; // 2026-11-18 yeni arka planlar: keskin ("bulanıklığı kaldır öyle göreyim" → "aktaralım")

        public readonly struct Skin
        {
            public readonly string Id, Name;
            public readonly Color32 Highlight, Base, Shadow, Floor;
            public Skin(string id, string name, uint hi, uint b, uint sh, uint floor)
            { Id = id; Name = name; Highlight = Hex(hi); Base = Hex(b); Shadow = Hex(sh); Floor = Hex(floor); }
        }

        /// <summary>palet.json sırası (01–06).</summary>
        public static readonly Skin[] Skins =
        {
            // Kullanıcı 2026-11-18: docs/yeni_v1/arkaplan "bu arkaplanları dener misin … tepsinin renkleri yine arkaplana göre" →
            // "aktaralım". Dosya adları (kimlik) aynı kaldı, görseller yeni; palet görselin oyun alanı ortalamasından
            // (CaptureBgTrial.PaletteFrom). Eski görseller arşivde (arkaplan_vr2_eski_2026-11-18).
            new Skin("01-mavi-liman", "Gizemli Şelale", 0x7CBFBF, 0x1D6161, 0x102E2E, 0x245857),
            new Skin("02-yesil-vadi", "Büyülü Bahçe", 0x98C597, 0x3A6C3A, 0x1F361F, 0x426C42),
            new Skin("03-kum-vadisi", "Çöl Harabeleri", 0xF2B281, 0x9E5218, 0x522A0C, 0xDC7B31),
            new Skin("04-fener-koyu", "Kozmik Harabeler", 0x8079C5, 0x21196C, 0x131036, 0x2D276C),
            new Skin("05-vaha-yolu", "Yeşil Vaha", 0x87BF83, 0x296525, 0x173215, 0x336530),
            new Skin("06-inci-sahili", "Mistik Orman", 0x82BFAA, 0x24654F, 0x153329, 0x2F6553),
        };

        /// <summary>Tahta iç gölgesi. frame.mjs: #101C27, opaklık .76, kalınlık .30, σ .085, aşağı .10 (hücre). Kullanıcı 2026-11-14:
        /// "iç gölge fazla koyu … kusursuz bir geçişle olsun" → .40/σ .17; sonra "çok yumuşak olmasın, derinlik hissini daha iyi ver" → .55/σ .12; "gölge yine fazla yaygın, çizgisine doğru kısalsın" → opaklık .55, σ .07, kalınlık .24, aşağı .07.</summary>
        public static readonly Color InnerShadow = Hex(0x101C27);
        public const float CornerCut = 0.14f; // köşe: belge 0,22; kullanıcı 2026-11-14 "yuvarlama çok az olsun" → 0,10; 2026-11-15 "radiusu çok az arttıralım" → 0,14

        // Kullanıcı 2026-11-15: "iç gölge çok uyumsuz duruyor … basit duruyor … daha kısa ve daha az belirgin olmalı ama
        // derinliği de yansıtmalı" → tek katman yerine iki katman (hücre birimi; Vr2Floor):
        // temas: kenarın dibinde ince ve keskin (bant .10, σ .025, aşağı .025, %42) — derinlik buradan;
        // yumuşak: kısa, hafif geçiş (bant .16, σ .07, aşağı .05, %20). Renk temanın kendi koyu tonu (ShadowTint).
        public const float ContactStroke = 0.10f, ContactSigma = 0.025f, ContactOffset = 0.025f, ContactOpacity = 0.42f;
        public const float SoftStroke = 0.16f, SoftSigma = 0.07f, SoftOffset = 0.05f, SoftOpacity = 0.20f;

        /// <summary>İç gölge rengi: temanın paletteki koyu tonu (palet.json shadow), zeminle aynı renk ailesi.</summary>
        public static Color ShadowTint(int skin) => Skins[Mathf.Clamp(skin, 0, Skins.Length - 1)].Shadow;
        /// <summary>İç alanın hücre kenarından dışa payı (hücre): taşlarla kenar arası boşluk (kullanıcı 2026-11-14; önce 0,18, "border ile iç boşluğu arttıralım" → 0,28; "şuankinin 2 katı olsun" → 0,56; "%25 azaltalım" → 0,42; "%25 daha azaltalım" → 0,32; 2026-11-15 "boşluğu da azaltalım" → 0,24). Köşe yarıçapı ayrı: CornerCut.</summary>
        public const float Inset = 0.24f;
        /// <summary>Dar boşlukta (tek hücrelik delik/oyuk) pay sınırı: boşlukta en az 1 − 2×0,28 = 0,44 hücre açıklık kalır.</summary>
        public const float NarrowInset = 0.28f;
        /// <summary>Kullanıcı 2026-11-14: "iç tahtanın tonu arkaplan renginin açık tonu olsun" → palet floorColor (arka plan ortasından
        /// örneklenmiş) bu oranda beyaza açılır. Tahta iç alanı <see cref="InnerFloor"/>; palet değeri değişmez.</summary>
        public const float FloorLighten = 0.35f;

        /// <summary>Tahta iç alanının rengi: temanın arka plan renginin açık tonu.</summary>
        public static Color32 InnerFloor(int skin) => Color.Lerp(Skins[Mathf.Clamp(skin, 0, Skins.Length - 1)].Floor, Color.white, FloorLighten);

        /// <summary>Boş hücre yuvası (kullanıcı 2026-11-14: "taşlar tamamen kalkınca alt zemininde yerini belli eden hücresi
        /// olsun"): iç alanın temanın koyu tonuna doğru %18 koyusu, taş biçiminde (yuvarlak köşeli) yuva. Taş varken altında kalır.</summary>
        public const float EmptySlotDarken = 0.18f;

        public static Color32 EmptySlot(int skin) => Color.Lerp(InnerFloor(skin), Skins[Mathf.Clamp(skin, 0, Skins.Length - 1)].Shadow, EmptySlotDarken);

        // Bölge türü sırası mevcut tema eşlemesiyle aynı (ThemeDefinition.ForLevel: sahil, bahçe, çöl). Bölge adları
        // VR2 dokularıyla birebir eşleşir: 0 Mavi Liman, 1 Yeşil Vadi, 2 Kum Vadisi, 3 Fener Koyu, 5 Vaha Yolu,
        // 6 İnci Sahili. Dokusu olmayan bölgeler türlerinin dokularını sırayla kullanır (4, 7 → Yeşil Vadi; 8 → Kum
        // Vadisi; 9 → Mavi Liman).
        private static readonly int[][] ByType = { new[] { 0, 3, 5 }, new[] { 1 }, new[] { 2, 4 } };

        /// <summary>Bölümün (0 tabanlı) VR2 teması (0..5).</summary>
        public static int SkinFor(int levelIndex)
        {
            int region = Mathf.Max(0, levelIndex) / KatKat.View.ThemeDefinition.RegionSize;
            var list = ByType[region % 3];
            return list[(region / 3) % list.Length];
        }

        private static int _bgSkin = -1;
        private static Sprite _bg;
        private static bool? _ready;

        /// <summary>VR2 dokuları projede var mı (yoksa ui3 görünümü kalır).</summary>
        public static bool Ready
        {
            get
            {
                if (!Enabled) return false;
                if (_ready == null) _ready = Background(0) != null;
                return _ready.Value;
            }
        }

        /// <summary>Temanın tam ekran dokusu. Yalnız etkin tema bellekte tutulur: başka tema istenince önceki boşaltılır.</summary>
        /// <summary>Krem üst panelin opaklığı (kullanıcı 2026-11-18: "üst kısmın opaklığını azaltalım arkaplan görünsün" → 0,5; "çok saydam olmuş" → 0,78; önce 1).</summary>
        public const float PanelAlpha = 0.78f;

        /// <summary>Yalnız editör denemesi (CaptureBgTrial): doluysa bütün temalarda bu doku kullanılır.</summary>
        public static Sprite PreviewBackground;

        public static Sprite Background(int skin)
        {
            if (PreviewBackground != null) return PreviewBackground;
            skin = Mathf.Clamp(skin, 0, Skins.Length - 1);
            for (int i = 0; i < Skins.Length; i++) KatKat.View.ResourceUsage.Note("VR2/bg/" + Skins[i].Id);
            if (_bg != null && _bgSkin == skin) return _bg;
            var next = Resources.Load<Sprite>("VR2/bg/" + Skins[skin].Id);
            if (next == null) return null;
            if (_bg != null && _bg != next && Application.isPlaying) Resources.UnloadAsset(_bg.texture);
            _bg = next;
            _bgSkin = skin;
            return _bg;
        }

        // ---- Üst panel ---------------------------------------------------------------------------------------------

        private static Sprite _panel, _panelShade;

        /// <summary>Panelin alt köşe yarıçapı (UI birimi; kullanıcı 2026-11-18: üst panelin alt uç köşeleri "yuvarlak yapalım").</summary>
        public const int PanelCorner = 40;
        /// <summary>Panel altı gölgesinin yüksekliği (UI birimi).</summary>
        public const int PanelShadeH = 28;

        /// <summary>Yuvarlak köşe kenarında 1 px yumuşak geçiş: (cx, cy) merkezli R yarıçaplı çeyrek dairenin dışında 0.</summary>
        private static float CornerCover(float x, float y, float cx, float cy, float r)
        {
            float dx = x - cx, dy = y - cy;
            return Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
        }

        /// <summary>ust-panel.svg: krem dikey gradyan (#FFF3DE → .48 #FCE9CB → #F5DDB9), çizgisiz. Alt köşeler
        /// <see cref="PanelCorner"/> yarıçaplı yuvarlak; 9 dilim (sol/sağ/alt kenar = yarıçap), gövde yatay ve dikey esner.</summary>
        public static Sprite PanelSprite()
        {
            if (_panel != null) return _panel;
            const int R = PanelCorner, w = 2 * R + 4, h = 320;
            var tex = NewTex("vr2_panel", w, h);
            var px = new Color32[w * h];
            Color a = Hex(0xFFF3DE), b = Hex(0xFCE9CB), c = Hex(0xF5DDB9);
            for (int j = 0; j < h; j++)
            {
                float t = 1f - (j + 0.5f) / h; // 0 üst
                Color col = t < 0.48f ? Color.Lerp(a, b, t / 0.48f) : Color.Lerp(b, c, (t - 0.48f) / 0.52f);
                for (int i = 0; i < w; i++)
                {
                    float x = i + 0.5f, y = j + 0.5f, cov = 1f;
                    if (y < R && x < R) cov = CornerCover(x, y, R, R, R);
                    else if (y < R && x > w - R) cov = CornerCover(x, y, w - R, R, R);
                    var k = col; k.a = cov;
                    px[j * w + i] = k;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return _panel = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R + 1, R + 1, R + 1, 0));
        }

        /// <summary>Panelin altındaki dış gölge (üst katman hissi; kullanıcı 2026-11-14): panel biçiminin (yuvarlak alt köşeler)
        /// dışında, kenara uzaklıkla sönen koyu bant. Panelin alt <see cref="PanelCorner"/> birimiyle örtüşecek biçimde yerleşir;
        /// 9 dilim (sol/sağ = yarıçap).</summary>
        public static Sprite PanelShadeSprite()
        {
            if (_panelShade != null) return _panelShade;
            const int R = PanelCorner, S = PanelShadeH, w = 2 * R + 4, h = R + S;
            var tex = NewTex("vr2_panel_shade", w, h);
            var px = new Color32[w * h];
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = i + 0.5f, y = j + 0.5f;
                // Panel biçimine uzaklık: alt kenar y = S; köşelerde (R, S+R) ve (w−R, S+R) merkezli R yarıçaplı daire.
                float d;
                if (x < R && y < S + R) d = Mathf.Sqrt((x - R) * (x - R) + (y - S - R) * (y - S - R)) - R;
                else if (x > w - R && y < S + R) d = Mathf.Sqrt((x - w + R) * (x - w + R) + (y - S - R) * (y - S - R)) - R;
                else d = S - y;
                float a = d <= 0f ? 0f : Mathf.Pow(Mathf.Clamp01(1f - d / S), 1.5f) * Mathf.Clamp01(d * 2f);
                px[j * w + i] = new Color(0.07f, 0.09f, 0.11f, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return _panelShade = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R + 1, 0, R + 1, 0));
        }

        // ---- Hedef yuvası --------------------------------------------------------------------------------------------

        private static Sprite _slot;
        private static int _slotSkin = -1;

        /// <summary>
        /// hedef-yuvasi.svg (960×144): dış kapsül kenarı temanın paletinden (üst base → orta shadow → alt highlight),
        /// 5 px içeride koyu iç duvar #704C32, onun içinde 7 px aşağıda başlayan krem zemin (#FFF2D6 → #F7E1B7 → #EBCB98).
        /// Üst iç kenarda duvar daha kalın görünür (gömülü yuva); zeminin üst kenarı ayrıca hafif gölgelenir.
        /// 9 dilim: sol/sağ 72, üst/alt 20 (belge). Uçlar yüksekliğin yarısı yarıçaplı; köşeler esnetilmez.
        /// </summary>
        public static Sprite GoalSlotSprite(int skin)
        {
            skin = Mathf.Clamp(skin, 0, Skins.Length - 1);
            if (_slot != null && _slotSkin == skin) return _slot;
            var s = Skins[skin];
            const int w = 960, h = 144;
            var tex = NewTex("vr2_goal_slot", w, h);
            var px = new Color32[w * h];
            Color fTop = Hex(0xFFF2D6), fMid = Hex(0xF7E1B7), fBot = Hex(0xEBCB98), wall = Hex(0x704C32);
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = i + 0.5f, y = h - (j + 0.5f); // SVG koordinatı (y aşağı)
                float dRim = RoundRect(x, y, 3f, 3f, 954f, 138f, 69f);
                float dWall = RoundRect(x, y, 8f, 8f, 944f, 128f, 64f);
                float dFloor = RoundRect(x, y, 11f, 15f, 938f, 118f, 59f);
                float ty = (y - 3f) / 138f;
                Color rim = ty < 0.48f ? Color.Lerp(s.Base, s.Shadow, ty / 0.48f) : Color.Lerp(s.Shadow, s.Highlight, (ty - 0.48f) / 0.52f);
                float fy = Mathf.Clamp01((y - 15f) / 118f);
                Color floor = fy < 0.48f ? Color.Lerp(fTop, fMid, fy / 0.48f) : Color.Lerp(fMid, fBot, (fy - 0.48f) / 0.52f);
                // Üst iç kenar gölgesi: zeminin ilk ~14 px'i duvar rengine doğru yumuşak koyulaşır.
                float shade = Mathf.Clamp01(1f - (y - 15f) / 14f);
                floor = Color.Lerp(floor, wall, 0.32f * shade * shade);
                Color col = rim;
                col = Color.Lerp(col, wall, Cover(dWall));
                col = Color.Lerp(col, floor, Cover(dFloor));
                col.a = Cover(dRim);
                px[j * w + i] = col;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            if (_slot != null)
            {
                if (Application.isPlaying) Object.Destroy(_slot.texture);
                else Object.DestroyImmediate(_slot.texture);
            }
            _slotSkin = skin;
            return _slot = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(72f, 20f, 72f, 20f));
        }

        // ---- Yardımcılar ---------------------------------------------------------------------------------------------

        /// <summary>Yuvarlak dikdörtgene işaretli uzaklık (içeride negatif); (x, y) sol üst, w×h, yarıçap r.</summary>
        private static float RoundRect(float px, float py, float x, float y, float w, float h, float r)
        {
            float cx = x + w * 0.5f, cy = y + h * 0.5f;
            float qx = Mathf.Abs(px - cx) - (w * 0.5f - r), qy = Mathf.Abs(py - cy) - (h * 0.5f - r);
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float Cover(float d) => Mathf.Clamp01(0.5f - d);

        private static Texture2D NewTex(string name, int w, int h) =>
            new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };

        public static Color32 Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 0xFF);
    }
}
