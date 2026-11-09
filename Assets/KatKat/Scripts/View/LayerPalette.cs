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
        };

        public static Color32 RimColor(LayerColor c) => Rim[(int)c];

        /// <summary>İşaret atlası hücresi: coral(0,0) daire, blue(1,0) üçgen, yellow(0,1) dalga, purple(1,1) elmas.</summary>
        public static Vector2 IconCell(LayerColor c)
        {
            int i = (int)c;
            return new Vector2(i % 2, i / 2);
        }

        public static string ColorName(LayerColor c)
        {
            switch (c)
            {
                case LayerColor.Coral: return KatKat.UI.Loc.T("color.coral");
                case LayerColor.Blue: return KatKat.UI.Loc.T("color.blue");
                case LayerColor.Yellow: return KatKat.UI.Loc.T("color.yellow");
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
                default: return "elmas";
            }
        }
    }
}
