namespace KatKat.Levels
{
    /// <summary>Statik doğrulamanın içerik sınırları.</summary>
    public sealed class ValidationScope
    {
        public int[] AllowedSizes;
        public int MaxLayers;
        public bool AllowClips;
        public bool AllowStars;
        public bool AllowIce;
        public bool AllowColorLock;
        public bool AllowEnvelope;
        public bool AllowRotor;
    }
}
