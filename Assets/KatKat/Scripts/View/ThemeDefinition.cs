using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Tek görsel tema kaydı (Görsel Şartname v3 §8, §12): dünya arka planları, tahta iç tepsisi, renkli çerçeve,
    /// boş hücre yuvası, temas gölgesi, sonraki bölüm önizleme zemini ve modal karartma rengi. Taş paleti temadan
    /// bağımsızdır (mercan/mavi/sarı/lila sabit). Tepsi hiçbir temada mor değildir.
    /// </summary>
    public sealed class ThemeDefinition
    {
        public string Id;
        /// <summary>Bölge türü (Sahil/Bahçe/Çöl) ve oyuncuya gösterilen bölge adı (Mavi Liman/Yeşil Vadi/Kum Vadisi).</summary>
        public string Name, RegionName;
        public string HomeBg, GameBg, MapBg, Backplate, Frame;
        /// <summary>Şartname tablosu: tepsi içi, çerçeve dolgusu, kenar vurgusu, gölge.</summary>
        public Color32 BackplateColor, RimFill, RimAccent, RimShadow;
        /// <summary>Boş hücre yuvası (tepsinin bir ton koyusu, yarı saydam) ve yığın altı temas gölgesi.</summary>
        public Color32 Slot, ContactShadow;
        public Color DimColor;
        /// <summary>Ortam parçacığı (v4 effects): sahil baloncuk, bahçe yaprak, çöl toz/parıltı.</summary>
        public string AmbientFx;

        /// <summary>Bölge türünün geçerli dildeki adı (Sahil/Bahçe/Çöl).</summary>
        public string LocalName => KatKat.UI.Loc.T("theme." + Id, Name);

        public Sprite HomeSprite => PremiumArt.V4(HomeBg);
        public Sprite GameSprite => PremiumArt.V4(GameBg);
        public Sprite MapSprite => PremiumArt.V4(MapBg);
        public Sprite BackplateSprite => PremiumArt.V4(Backplate);
        public Sprite FrameSprite => PremiumArt.V4(Frame);

        public static readonly ThemeDefinition Coast = new ThemeDefinition
        {
            Id = "coast", Name = "Sahil", RegionName = "Mavi Liman",
            HomeBg = "backgrounds/coast-home", GameBg = "backgrounds/coast-terrace", MapBg = "backgrounds/coast-map",
            Backplate = "boards/coast-backplate", Frame = "boards/coast-frame",
            BackplateColor = new Color32(0xD9, 0xED, 0xF2, 0xFF), RimFill = new Color32(0x91, 0xC8, 0xD6, 0xFF),
            RimAccent = new Color32(0xE5, 0xBC, 0x68, 0xFF), RimShadow = new Color32(0x59, 0x7E, 0x88, 0xFF),
            Slot = new Color32(0x9F, 0xC9, 0xD4, 0x8C), ContactShadow = new Color32(0x3E, 0x62, 0x6C, 0x50),
            DimColor = new Color(0.06f, 0.16f, 0.22f, 0.55f), AmbientFx = "effects/bubble",
        };

        public static readonly ThemeDefinition Garden = new ThemeDefinition
        {
            Id = "garden", Name = "Bahçe", RegionName = "Yeşil Vadi",
            HomeBg = "backgrounds/garden-home", GameBg = "backgrounds/garden-game", MapBg = "backgrounds/garden-map",
            Backplate = "boards/garden-backplate", Frame = "boards/garden-frame",
            BackplateColor = new Color32(0xE7, 0xEE, 0xD8, 0xFF), RimFill = new Color32(0xA7, 0xBE, 0x8B, 0xFF),
            RimAccent = new Color32(0xD6, 0xB4, 0x6B, 0xFF), RimShadow = new Color32(0x61, 0x75, 0x53, 0xFF),
            Slot = new Color32(0xB4, 0xC4, 0x9A, 0x8C), ContactShadow = new Color32(0x45, 0x55, 0x38, 0x50),
            DimColor = new Color(0.08f, 0.16f, 0.08f, 0.55f), AmbientFx = "effects/leaf",
        };

        public static readonly ThemeDefinition Desert = new ThemeDefinition
        {
            Id = "desert", Name = "Çöl", RegionName = "Kum Vadisi",
            HomeBg = "backgrounds/desert-home", GameBg = "backgrounds/desert-game", MapBg = "backgrounds/desert-map",
            Backplate = "boards/desert-backplate", Frame = "boards/desert-frame",
            BackplateColor = new Color32(0xF4, 0xE3, 0xC3, 0xFF), RimFill = new Color32(0xD9, 0xB4, 0x7C, 0xFF),
            RimAccent = new Color32(0xC6, 0x8E, 0x49, 0xFF), RimShadow = new Color32(0x8E, 0x76, 0x5A, 0xFF),
            Slot = new Color32(0xD8, 0xBE, 0x92, 0x8C), ContactShadow = new Color32(0x6E, 0x55, 0x38, 0x50),
            DimColor = new Color(0.2f, 0.13f, 0.06f, 0.55f), AmbientFx = "effects/sparkle",
        };

        public static readonly ThemeDefinition[] All = { Coast, Garden, Desert };

        /// <summary>
        /// Bölge eşlemesi (şartname §12 önerisi): 1–100 sahil, 101–200 bahçe, 201–300 çöl. 301–1000 için tema
        /// verisi tanımlanana dek aynı üçlü sırayla döner (301–400 sahil …); mor tepsiye düşülmez.
        /// </summary>
        public static ThemeDefinition ForLevel(int levelIndex) => All[Mathf.Max(0, levelIndex) / 100 % All.Length];

        /// <summary>Bölge numarası (0 tabanlı): her 100 bölüm bir bölge.</summary>
        public const int RegionSize = 100;
        public static int RegionOf(int levelIndex) => Mathf.Max(0, levelIndex) / RegionSize;

        // Her bölgenin kendi adı (tema üçlüsü döner ama ad tekrar etmez): sahil, bahçe, çöl sırasıyla.
        private static readonly string[] RegionNames =
        {
            "Mavi Liman", "Yeşil Vadi", "Kum Vadisi",
            "Fener Koyu", "Çiçek Bahçesi", "Vaha Yolu",
            "İnci Sahili", "Gül Vadisi", "Altın Kumlar",
            "Martı Limanı",
        };

        /// <summary>Bölge adı (0 tabanlı bölge). 10 bölgeden sonra numaralı devam eder.</summary>
        public static string RegionTitle(int region)
        {
            region = Mathf.Max(0, region);
            if (region < RegionNames.Length) return KatKat.UI.Loc.T("region." + region, RegionNames[region]);
            return KatKat.UI.Loc.T("region." + region % RegionNames.Length, RegionNames[region % RegionNames.Length]) + " " + (region / RegionNames.Length + 1);
        }
    }
}
