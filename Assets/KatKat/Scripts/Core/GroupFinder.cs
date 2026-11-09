using System.Collections.Generic;

namespace KatKat.Core
{
    /// <summary>
    /// Kenar komşuluğuyla (köşegen yok) aynı üst renkteki seçilebilir hücrelerin bağlı bileşenleri.
    /// Sonuç listeleri y, sonra x artan sıradadır.
    /// </summary>
    public static class GroupFinder
    {
        public const int MinGroup = 3;

        private static readonly int[] Dx = { 1, -1, 0, 0 };
        private static readonly int[] Dy = { 0, 0, 1, -1 };

        /// <summary>(x,y) hücresinin bileşenini bulur. Hücre seçilemezse boş liste.</summary>
        public static List<int> Component(BoardState board, int x, int y)
        {
            var result = new List<int>();
            if (!board.InBounds(x, y) || !board[x, y].IsSelectable) return result;
            var visited = new bool[board.Count];
            Flood(board, board.Index(x, y), visited, result);
            result.Sort();
            return result;
        }

        /// <summary>Tahtadaki bütün bileşenler (boyut sınırı olmadan), ilk hücre sırasıyla.</summary>
        public static List<List<int>> AllComponents(BoardState board)
        {
            var all = new List<List<int>>();
            var visited = new bool[board.Count];
            for (int i = 0; i < board.Count; i++)
            {
                if (visited[i] || !board[i].IsSelectable) continue;
                var comp = new List<int>();
                Flood(board, i, visited, comp);
                comp.Sort();
                all.Add(comp);
            }
            return all;
        }

        public static bool HasValidGroup(BoardState board)
        {
            foreach (var c in AllComponents(board)) if (c.Count >= MinGroup) return true;
            return false;
        }

        private static void Flood(BoardState board, int start, bool[] visited, List<int> output)
        {
            var color = board[start].Top;
            var stack = new Stack<int>();
            stack.Push(start);
            visited[start] = true;
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                output.Add(i);
                int x = board.X(i), y = board.Y(i);
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + Dx[d], ny = y + Dy[d];
                    if (!board.InBounds(nx, ny)) continue;
                    int n = board.Index(nx, ny);
                    if (visited[n]) continue;
                    var c = board[n];
                    if (!c.IsSelectable || c.Top != color) continue;
                    visited[n] = true;
                    stack.Push(n);
                }
            }
        }
    }
}
