namespace KatKat.Core
{
    /// <summary>Bir dalgada renk kilidinin sayacından düşülen miktar. RemainingAfter 0 ise kilit açıldı.</summary>
    public readonly struct LockHit
    {
        public readonly int X;
        public readonly int Y;
        public readonly LayerColor Color;
        public readonly int Amount;
        public readonly int RemainingAfter;

        public LockHit(int x, int y, LayerColor color, int amount, int remainingAfter)
        {
            X = x; Y = y; Color = color; Amount = amount; RemainingAfter = remainingAfter;
        }
    }
}
