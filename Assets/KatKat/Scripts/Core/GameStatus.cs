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
    }
}
