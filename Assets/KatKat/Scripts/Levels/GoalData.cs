namespace KatKat.Levels
{
    public sealed class GoalData
    {
        /// <summary>"collectColor", "collectStars" veya "collectToken" (schemaVersion 2).</summary>
        public string Type;
        /// <summary>collectColor için renk kimliği; diğerleri için boş.</summary>
        public string Color;
        /// <summary>collectToken için tür: "envelope".</summary>
        public string Kind;
        public int Amount;
    }
}
