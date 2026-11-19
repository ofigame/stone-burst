namespace KatKat.Game
{
    public enum Sfx
    {
        Tap,
        Invalid,
        Peel,
        Chain,
        RollGain,
        RollUse,
        Win,
        Lose,
        ClipOpen,
        Star,
        /// <summary>Taş akışı: hedefe giden her taşta jeton şıngırtısı (perde yükselerek tekrarlar; 2026-11-19).</summary>
        Coin,
        /// <summary>Taş akışı: son varışta kapanış "ka-ching".</summary>
        CoinFinal,
    }
}
