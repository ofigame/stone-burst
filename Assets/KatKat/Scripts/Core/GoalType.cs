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
        // 4 = CollectChain v14'te kaldırıldı (kullanıcı 2026-11-16: zincir tamamen kaldırılsın); numara boş bırakıldı.
        /// <summary>Konsept pilotu "Balonlar": patlayan balon. Veride collectToken(kind=balloon).</summary>
        CollectBalloons = 5,
    }
}
