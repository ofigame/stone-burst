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
        /// <summary>2026-11-20 Bomba güçlendiricisi: (x, y) merkezli 3×3 alan.</summary>
        public static Command Bomb(int x, int y) => new Command(CommandType.Bomb, x, y, -1);

        /// <summary>2026-11-21 sürükle-değiştir: (x, y) üst taşı <paramref name="dir"/> yönündeki komşunun aynı katıyla (0 sağ, 1 aşağı, 2 sol, 3 yukarı).</summary>
        public static Command Swap(int x, int y, int dir) => new Command(CommandType.Swap, x, y, dir);

        /// <summary>2026-11-23 kova kipi: etkin kovayı içindeki kadarıyla gönderir.</summary>
        public static Command SendBucket() => new Command(CommandType.SendBucket, -1, -1, -1);

        public int Slot => Row;
        /// <summary>Swap komutunun komşu hücresi.</summary>
        public int SwapX => X + (Row == 0 ? 1 : Row == 2 ? -1 : 0);
        public int SwapY => Y + (Row == 1 ? 1 : Row == 3 ? -1 : 0);

        public override string ToString() =>
            Type == CommandType.Tap ? $"tap({X},{Y})" : Type == CommandType.Rotate ? $"rotate({X},{Y})"
            : Type == CommandType.Paint ? $"paint({X},{Y},{Color})" : Type == CommandType.UseSpecial ? $"use({Row},{X},{Y})" : Type == CommandType.Bomb ? $"bomb({X},{Y})" : Type == CommandType.Swap ? $"swap({X},{Y}->{SwapX},{SwapY})" : Type == CommandType.SendBucket ? "sendBucket" : $"roll(row {Row})";
    }
}
