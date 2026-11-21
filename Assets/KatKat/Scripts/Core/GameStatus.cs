namespace KatKat.Core
{
    public enum GameStatus
    {
        Idle = 0,
        Won = 1,
        LostOutOfMoves = 2,
        LostNoLegalMoves = 3,
        /// <summary>v12 deneme: kristal (korunan) yığından katman soyuldu.</summary>
        LostProtected = 4,
        /// <summary>Konsept pilotu: tehdit kazandı (nedeni <see cref="HazardState.Loss"/>).</summary>
        LostHazard = 5,
        /// <summary>2026-11-21 süreli bölüm: süre bitti (oyun akışı koyar; kural motoru zamana bakmaz).</summary>
        LostOutOfTime = 6,
    }
}
