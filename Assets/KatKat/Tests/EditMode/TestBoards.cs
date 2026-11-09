using System;
using KatKat.Core;

namespace KatKat.Tests
{
    /// <summary>
    /// Test tahtası kısa yazımı. Her satır boşlukla ayrılmış hücrelerdir; harfler ALTTAN ÜSTE katmanlar:
    /// c=coral b=blue y=yellow p=purple, "." aktif boş hücre, "#" inactive.
    /// Sonek: "k1"/"k2" ataş (kalan hasar), "i1"/"i2" buz, "L&lt;renk&gt;&lt;n&gt;" renk kilidi (ör. "ybLc3": mercan 3),
    /// "E&lt;renk&gt;" mühürlü zarf (ör. "ybEc": mercan mühür), "*" dip yıldızı, "R" (en sonda) döner taban. Örn. "bck2" = iki noktalı ataşlı, "c*" = yıldızlı.
    /// Örn. "bc" = altta blue, üstte coral.
    /// </summary>
    public static class TestBoards
    {
        public static LevelState State(int moves, string[] rows, params (LayerColor color, int amount)[] goals)
        {
            var s = new LevelState { Board = Board(rows), MovesLeft = moves, Status = GameStatus.Idle };
            foreach (var g in goals) s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = g.color, Amount = g.amount });
            return s;
        }

        public static BoardState Board(string[] rows)
        {
            var first = rows[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var b = new BoardState(first.Length, rows.Length);
            for (int y = 0; y < rows.Length; y++)
            {
                var cells = rows[y].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (cells.Length != b.Width) throw new ArgumentException($"satır {y} genişliği farklı");
                for (int x = 0; x < cells.Length; x++)
                {
                    var c = b[x, y];
                    if (cells[x] == "#") continue;
                    c.Active = true;
                    if (cells[x] == ".") continue;
                    var t = cells[x];
                    if (t.EndsWith("*")) { c.HasBottomStar = true; t = t.Substring(0, t.Length - 1); }
                    if (t.EndsWith("R")) { c.HasRotor = true; t = t.Substring(0, t.Length - 1); }
                    int k = t.IndexOf('k');
                    if (k >= 0) { c.ClipHitsRemaining = t[k + 1] - '0'; t = t.Substring(0, k); }
                    int ice = t.IndexOf('i');
                    if (ice >= 0) { c.IceHitsRemaining = t[ice + 1] - '0'; t = t.Substring(0, ice); }
                    int env = t.IndexOf('E');
                    if (env >= 0) { c.HasEnvelope = true; c.EnvelopeColor = Color(t[env + 1]); t = t.Substring(0, env); }
                    int lk = t.IndexOf('L');
                    if (lk >= 0) { c.LockColor = Color(t[lk + 1]); c.LockRemaining = int.Parse(t.Substring(lk + 2)); t = t.Substring(0, lk); }
                    foreach (var ch in t) c.Layers.Add(Color(ch));
                }
            }
            return b;
        }

        public static LayerColor Color(char ch)
        {
            switch (ch)
            {
                case 'c': return LayerColor.Coral;
                case 'b': return LayerColor.Blue;
                case 'y': return LayerColor.Yellow;
                case 'p': return LayerColor.Purple;
                default: throw new ArgumentException("bilinmeyen renk harfi " + ch);
            }
        }

        /// <summary>Hücrenin katmanlarını aynı kısa yazımla döndürür.</summary>
        public static string Cell(LevelState s, int x, int y)
        {
            var c = s.Board[x, y];
            if (!c.Active) return "#";
            if (c.Layers.Count == 0) return ".";
            var chars = new char[c.Layers.Count];
            for (int i = 0; i < chars.Length; i++) chars[i] = "cbyp"[(int)c.Layers[i]];
            return new string(chars);
        }
    }
}
