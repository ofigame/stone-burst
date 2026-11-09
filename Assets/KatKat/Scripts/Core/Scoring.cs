namespace KatKat.Core
{
    /// <summary>Puan ve yıldız (v5 §5). Saf hesap; zaman ve sunum bilgisi yok.</summary>
    public static class Scoring
    {
        public const int LayerBase = 10;
        public const int CoverHit = 20;
        public const int Envelope = 50;
        public const int BottomStar = 50;
        public const int RocketCreated = 50;
        public const int RainbowCreated = 100;

        /// <summary>Dalga sırasına göre katman puanı: domino uzadıkça her katman daha değerli.</summary>
        public static int Layer(int waveIndex) => LayerBase * (1 + waveIndex);

        /// <summary>0 (kazanılmadı) – 3 yıldız. Eşik yoksa kazanmak 3 yıldız sayılır (eski veri).</summary>
        public static int Stars(bool won, int score, int[] thresholds)
        {
            if (!won) return 0;
            if (thresholds == null || thresholds.Length < 2) return 3;
            int s = 1;
            if (score >= thresholds[0]) s++;
            if (score >= thresholds[1]) s++;
            return s;
        }
    }
}
