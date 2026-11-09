namespace KatKat.Core
{
    public enum GoalType
    {
        CollectColor = 0,
        /// <summary>Dip yıldızı toplama (Faz 5).</summary>
        CollectStars = 1,
        /// <summary>Zarf toplama: veride collectToken(kind=envelope) (Premium-v2 M07).</summary>
        CollectEnvelopes = 2,
        /// <summary>Sünger toplama: veride collectToken(kind=sponge) (M09).</summary>
        CollectSponges = 3,
        /// <summary>v12 deneme: yalnız zincirle (domino dalgası) soyulan bu renkteki katmanlar sayılır. Veride collectChain.</summary>
        CollectChain = 4,
    }
}
