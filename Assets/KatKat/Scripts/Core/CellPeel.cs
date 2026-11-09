namespace KatKat.Core
{
    /// <summary>Bir hücreden tek katman kaldırılması.</summary>
    public readonly struct CellPeel
    {
        public readonly int X;
        public readonly int Y;
        public readonly LayerColor Removed;
        /// <summary>Kalan katman sayısı; 0 ise NewTop anlamsızdır.</summary>
        public readonly int Remaining;
        public readonly LayerColor NewTop;
        /// <summary>Hücre boşaldı ve dip yıldızı bu soyulmada toplandı.</summary>
        public readonly bool StarCollected;

        public CellPeel(int x, int y, LayerColor removed, int remaining, LayerColor newTop, bool starCollected = false)
        {
            X = x; Y = y; Removed = removed; Remaining = remaining; NewTop = newTop; StarCollected = starCollected;
        }
    }
}
