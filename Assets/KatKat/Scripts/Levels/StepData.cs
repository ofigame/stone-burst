namespace KatKat.Levels
{
    /// <summary>Kayıtlı çözüm adımı: {"type":"tap","x":..,"y":..} veya {"type":"roll","row":..}.</summary>
    public sealed class StepData
    {
        public string Type;
        public int X = -1;
        public int Y = -1;
        /// <summary>roll: satır; use (v6 tepsi): yuva.</summary>
        public int Row = -1;
        /// <summary>M12 boya adımı: yeni üst renk kimliği.</summary>
        public string Color;
    }
}
