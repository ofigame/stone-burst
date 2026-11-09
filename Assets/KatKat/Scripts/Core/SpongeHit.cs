namespace KatKat.Core
{
    /// <summary>M09: bir dalgada süngerin dolan yuvaları. Have == Need ise sünger açıldı.</summary>
    public readonly struct SpongeHit
    {
        public readonly int X;
        public readonly int Y;
        public readonly int HaveAfter;
        public readonly int Need;

        public SpongeHit(int x, int y, int haveAfter, int need)
        {
            X = x; Y = y; HaveAfter = haveAfter; Need = need;
        }
    }
}
