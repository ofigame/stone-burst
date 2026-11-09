namespace KatKat.Core
{
    /// <summary>Oyuncu komutu. X/Y Tap, Rotate ve Paint; Row yalnızca Roll; Color yalnızca Paint için anlamlıdır.</summary>
    public readonly struct Command
    {
        public readonly CommandType Type;
        public readonly int X;
        public readonly int Y;
        public readonly int Row;
        public readonly LayerColor Color;

        private Command(CommandType type, int x, int y, int row, LayerColor color = default)
        {
            Type = type; X = x; Y = y; Row = row; Color = color;
        }

        public static Command Tap(int x, int y) => new Command(CommandType.Tap, x, y, -1);
        public static Command Roll(int row) => new Command(CommandType.Roll, -1, -1, row);
        public static Command Rotate(int x, int y) => new Command(CommandType.Rotate, x, y, -1);
        public static Command Paint(int x, int y, LayerColor color) => new Command(CommandType.Paint, x, y, -1, color);
        /// <summary>v6: tepsinin <paramref name="slot"/>. özel taşını (x, y) hücresinde ateşler (Row = yuva).</summary>
        public static Command UseSpecial(int slot, int x, int y) => new Command(CommandType.UseSpecial, x, y, slot);

        public int Slot => Row;

        public override string ToString() =>
            Type == CommandType.Tap ? $"tap({X},{Y})" : Type == CommandType.Rotate ? $"rotate({X},{Y})"
            : Type == CommandType.Paint ? $"paint({X},{Y},{Color})" : Type == CommandType.UseSpecial ? $"use({Row},{X},{Y})" : $"roll(row {Row})";
    }
}
