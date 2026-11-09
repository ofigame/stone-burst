namespace KatKat.Core
{
    /// <summary>Koordinat: (x,y), sol üst (0,0), x sağa, y aşağı. Hücreler y*Width+x sırasında.</summary>
    public sealed class BoardState
    {
        public readonly int Width;
        public readonly int Height;
        private readonly CellState[] _cells;

        public BoardState(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new CellState[width * height];
            for (int i = 0; i < _cells.Length; i++) _cells[i] = new CellState();
        }

        public int Count => _cells.Length;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public CellState this[int x, int y] => _cells[y * Width + x];
        public CellState this[int index] => _cells[index];
        public int Index(int x, int y) => y * Width + x;
        public int X(int index) => index % Width;
        public int Y(int index) => index / Width;

        public int TotalLayers()
        {
            int n = 0;
            foreach (var c in _cells) n += c.Layers.Count;
            return n;
        }

        public int CountLayers(LayerColor color)
        {
            int n = 0;
            foreach (var c in _cells)
                foreach (var l in c.Layers)
                    if (l == color) n++;
            return n;
        }

        public BoardState Clone()
        {
            var b = new BoardState(Width, Height);
            for (int i = 0; i < _cells.Length; i++) b._cells[i] = _cells[i].Clone();
            return b;
        }

        public bool ContentEquals(BoardState o)
        {
            if (Width != o.Width || Height != o.Height) return false;
            for (int i = 0; i < _cells.Length; i++) if (!_cells[i].ContentEquals(o._cells[i])) return false;
            return true;
        }
    }
}
