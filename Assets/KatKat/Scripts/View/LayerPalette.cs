using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>Renk kimliğinden ekran rengine ve işaret atlası hücresine eşleme.</summary>
    public static class LayerPalette
    {
        private static readonly Color32[] Front =
        {
            new Color32(0xF4, 0x68, 0x60, 0xFF), // coral
            new Color32(0x16, 0xAF, 0xA9, 0xFF), // blue
            new Color32(0xF2, 0xEE, 0xE8, 0xFF), // yellow
            new Color32(0xA3, 0x62, 0xD8, 0xFF), // purple
            new Color32(0x3C, 0xC8, 0x3A, 0xFF), // green (yaprak)
            new Color32(0x1E, 0x5F, 0xE0, 0xFF), // navy (yıldız): koyu mavi, turkuaz blue ile karışmaz
        };

        public static Color32 FrontColor(LayerColor c) => Front[(int)c];

        /// <summary>
        /// Katmanın yan şeridi (alt katmanların görünen rengi). v10 (kullanıcı: "altta olan taşların renkleri bazen
        /// anlaşılmıyor"): eskiden yüz renginin ×0,9'u + shader'da ×0,6–0,85 koyulaşma mercan/mor ve krem/kahveyi
        /// birbirine yaklaştırıyordu. Şimdi her renk için ayrı, doygun şerit tonu; krem taşın şeridi sıcak bej (krem
        /// zeminden ayrılsın). Üst yüz çizimleri değişmez.
        /// </summary>
        private static readonly Color32[] Rim =
        {
            new Color32(0xF2, 0x4E, 0x3E, 0xFF), // coral: canlı mercan-kırmızı
            new Color32(0x0E, 0xB8, 0xB0, 0xFF), // blue: parlak turkuaz
            new Color32(0xDD, 0xB3, 0x7E, 0xFF), // yellow (krem): sıcak bej
            new Color32(0x9B, 0x4C, 0xE0, 0xFF), // purple: doygun mor
            new Color32(0x2C, 0xB0, 0x2A, 0xFF), // green: doygun yaprak yeşili
            new Color32(0x17, 0x4C, 0xD2, 0xFF), // navy: derin lacivert-mavi
        };

        public static Color32 RimColor(LayerColor c) => Rim[(int)c];

        /// <summary>
        /// Taş atlası hücresi (UV, y yukarı). Atlas 2x2 bloklardan oluşur, her blokta dört renk:
        /// coral(0,0) daire, blue(1,0) üçgen, yellow(0,1) dalga, purple(1,1) elmas; ikinci blokta green(2,0) yaprak,
        /// navy(3,0) yıldız, (2,1)/(3,1) gelecekteki iki renge boş. İlk dört rengin hücresi eskisiyle aynıdır.
        /// </summary>
        public static Vector2 IconCell(LayerColor c)
        {
            int i = (int)c;
            return new Vector2((i / 4) * 2 + i % 2, (i / 2) % 2);
        }

        /// <summary>Eski 2x2 kabartma işaret atlasında (ProceduralArt.IconAtlas) işareti olan renkler: ilk dört.</summary>
        public static bool HasLegacySymbol(LayerColor c) => (int)c < 4;

        public static string ColorName(LayerColor c)
        {
            switch (c)
            {
                case LayerColor.Coral: return KatKat.UI.Loc.T("color.coral");
                case LayerColor.Blue: return KatKat.UI.Loc.T("color.blue");
                case LayerColor.Yellow: return KatKat.UI.Loc.T("color.yellow");
                case LayerColor.Green: return KatKat.UI.Loc.T("color.green");
                case LayerColor.Navy: return KatKat.UI.Loc.T("color.navy");
                default: return KatKat.UI.Loc.T("color.purple");
            }
        }

        public static string ShapeName(LayerColor c)
        {
            switch (c)
            {
                case LayerColor.Coral: return "daire";
                case LayerColor.Blue: return "üçgen";
                case LayerColor.Yellow: return "dalga";
                case LayerColor.Green: return "yaprak";
                case LayerColor.Navy: return "yıldız";
                default: return "elmas";
            }
        }
    }
}
