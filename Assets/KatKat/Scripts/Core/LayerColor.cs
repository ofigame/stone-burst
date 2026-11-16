namespace KatKat.Core
{
    /// <summary>
    /// Sabit renk kimlikleri. Şekil eşlemesi: coral=daire, blue=üçgen (turkuaz), yellow=dalga, purple=elmas,
    /// green=yaprak, navy=yıldız (koyu mavi). Green/Navy (2026-11-16) isteğe bağlıdır: üreticiler ve canlı maç
    /// hâlâ ilk dört rengi kullanır; yalnız seviye dosyası "green"/"navy" yazarsa görünür. Sayısal değerler
    /// kayıt/seviye biçimlerinde kullanılır, değiştirilmez.
    /// </summary>
    public enum LayerColor
    {
        Coral = 0,
        Blue = 1,
        Yellow = 2,
        Purple = 3,
        Green = 4,
        Navy = 5,
    }

    /// <summary>Renk sayısı (renge göre dizilerin boyu). Seviye üreticilerinin kullandığı renk sayısı ayrıdır.</summary>
    public static class LayerColors
    {
        public const int Count = 6;
    }
}
