namespace KatKat.Core
{
    public enum WaveKind
    {
        Tap = 0,
        Roll = 1,
        // 2 = Chain (domino) v14'te kaldırıldı (kullanıcı 2026-11-16: zincir tamamen kaldırılsın); numara boş bırakıldı.
        /// <summary>Özel taş etkisi (roket, gökkuşağı, birleşim, bitiş kutlaması).</summary>
        Special = 3,
    }
}
