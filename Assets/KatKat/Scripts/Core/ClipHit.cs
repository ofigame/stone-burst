namespace KatKat.Core
{
    /// <summary>Bir dalgada ataşın aldığı tek hasar (belge Bölüm 7). RemainingAfter 0 ise kilit açıldı.</summary>
    public readonly struct ClipHit
    {
        public readonly int X;
        public readonly int Y;
        public readonly int RemainingAfter;

        public ClipHit(int x, int y, int remainingAfter)
        {
            X = x; Y = y; RemainingAfter = remainingAfter;
        }
    }
}
