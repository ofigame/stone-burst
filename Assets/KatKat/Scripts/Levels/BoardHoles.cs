using System.Collections.Generic;
using KatKat.Core;

namespace KatKat.Levels
{
    /// <summary>Tahtadaki iç boşluk: kenara değmeyen, geçit olmayan etkin olmayan hücre bölgesinin sınırları (hücre).</summary>
    public struct BoardHole
    {
        public bool Has;
        public int MinX, MinY, MaxX, MaxY, Cells;
        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;
        /// <summary>Sınır kutusunun içindeki en büyük kare kenarı (hücre); görsel bu kareye sığar.</summary>
        public int Side;
        /// <summary>O karenin sol üst hücresi.</summary>
        public int SquareX, SquareY;
        public float SquareCenterX => SquareX + (Side - 1) * 0.5f;
        public float SquareCenterY => SquareY + (Side - 1) * 0.5f;
        public float CenterX => (MinX + MaxX) * 0.5f;
        public float CenterY => (MinY + MaxY) * 0.5f;
    }

    /// <summary>
    /// vr1 P1 (03_KOLEKSIYON_ODUL "Merkez görseli"): buzlu koleksiyon taşı yalnız tahtada zaten var olan iç boşluğa konur;
    /// yeni delik açılmaz. Boşluk: kenara değmeyen, açılacak geçit hücresi olmayan, 4-komşu bağlı etkin olmayan hücreler.
    /// Görselin hücreleri örtmemesi için yalnız tamamen boş kare alan (<see cref="BoardHole.Side"/>) kullanılır.
    /// </summary>
    public static class BoardHoles
    {
        public static BoardHole Largest(BoardState b)
        {
            var best = new BoardHole();
            if (b == null) return best;
            var seen = new bool[b.Width * b.Height];
            for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
            {
                int i = y * b.Width + x;
                if (seen[i] || !Empty(b, x, y)) continue;
                var h = new BoardHole { MinX = x, MaxX = x, MinY = y, MaxY = y };
                bool edge = false;
                var cells = new List<(int, int)>();
                var q = new Queue<(int x, int y)>();
                q.Enqueue((x, y));
                seen[i] = true;
                while (q.Count > 0)
                {
                    var (cx, cy) = q.Dequeue();
                    cells.Add((cx, cy));
                    if (cx == 0 || cy == 0 || cx == b.Width - 1 || cy == b.Height - 1) edge = true;
                    if (cx < h.MinX) h.MinX = cx; if (cx > h.MaxX) h.MaxX = cx;
                    if (cy < h.MinY) h.MinY = cy; if (cy > h.MaxY) h.MaxY = cy;
                    foreach (var (nx, ny) in new[] { (cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1) })
                    {
                        if (nx < 0 || ny < 0 || nx >= b.Width || ny >= b.Height) continue;
                        int j = ny * b.Width + nx;
                        if (seen[j] || !Empty(b, nx, ny)) continue;
                        seen[j] = true;
                        q.Enqueue((nx, ny));
                    }
                }
                if (edge) continue;
                h.Cells = cells.Count;
                h.Has = true;
                h.Side = LargestSquare(b, h, out h.SquareX, out h.SquareY);
                if (h.Side > best.Side || (h.Side == best.Side && h.Cells > best.Cells)) best = h;
            }
            return best;
        }

        private static bool Empty(BoardState b, int x, int y)
        {
            var c = b[x, y];
            return !c.Active && c.GateId <= 0;
        }

        /// <summary>Bölgenin sınır kutusu içinde tamamı boş en büyük kare (basit tarama; tahtalar küçük).</summary>
        private static int LargestSquare(BoardState b, BoardHole h, out int sx, out int sy)
        {
            int best = 0;
            sx = h.MinX; sy = h.MinY;
            for (int s = 1; s <= System.Math.Min(h.Width, h.Height); s++)
            {
                bool found = false;
                for (int y = h.MinY; y + s - 1 <= h.MaxY && !found; y++)
                for (int x = h.MinX; x + s - 1 <= h.MaxX && !found; x++)
                {
                    bool ok = true;
                    for (int yy = y; yy < y + s && ok; yy++)
                    for (int xx = x; xx < x + s && ok; xx++)
                        ok = Empty(b, xx, yy);
                    found = ok;
                    if (ok) { sx = x; sy = y; }
                }
                if (found) best = s; else break;
            }
            return best;
        }
    }
}
