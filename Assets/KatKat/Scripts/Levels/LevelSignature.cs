using System.Collections.Generic;
using System.Text;

namespace KatKat.Levels
{
    /// <summary>
    /// Tekrar filtresi için kanonik tahta imzası (Premium-v2 rehberi §11–12). Kare tahtanın 8 dönüşümü (döndürme,
    /// ayna) × renklerin ilk görünüş sırasına göre yeniden adlandırılması altında en küçük metin. Hamle limiti,
    /// hedefler ve rulo ayarı imzaya girmez: yalnızca bunlarda farklı olan iki bölüm aynı sayılır.
    /// Örtüler (ataş/buz/kilit, kilit rengi dahil) ve yıldızlar imzaya girer.
    /// </summary>
    public static class LevelSignature
    {
        public static string Canonical(LevelData d)
        {
            var cells = new Dictionary<(int, int), CellData>();
            foreach (var c in d.Cells) cells[(c.X, c.Y)] = c;
            int n = d.Width;
            string best = null;
            for (int t = 0; t < 8; t++)
            {
                var s = Render(cells, n, d.Height, t);
                if (best == null || string.CompareOrdinal(s, best) < 0) best = s;
            }
            return $"{d.Width}x{d.Height}:{best}";
        }

        private static string Render(Dictionary<(int, int), CellData> cells, int w, int h, int t)
        {
            var relabel = new Dictionary<string, char>();
            var sb = new StringBuilder();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var (sx, sy) = Source(x, y, w, t);
                if (!cells.TryGetValue((sx, sy), out var c) || !c.Active) { sb.Append('#'); continue; }
                foreach (var l in c.Layers) sb.Append(Label(relabel, l));
                if (c.ClipHitsRemaining > 0) sb.Append('k').Append(c.ClipHitsRemaining);
                if (c.IceHits > 0) sb.Append('i').Append(c.IceHits);
                if (c.LockRemaining > 0) sb.Append('L').Append(Label(relabel, c.LockColor)).Append(c.LockRemaining);
                if (!string.IsNullOrEmpty(c.EnvelopeColor)) sb.Append('E').Append(Label(relabel, c.EnvelopeColor));
                if (c.HasBottomStar) sb.Append('*');
                if (c.HasRotor) sb.Append('R');
                sb.Append('/');
            }
            return sb.ToString();
        }

        private static char Label(Dictionary<string, char> relabel, string color)
        {
            if (!relabel.TryGetValue(color ?? "", out var ch)) relabel[color ?? ""] = ch = (char)('A' + relabel.Count);
            return ch;
        }

        /// <summary>Kare tahtada dönüşüm t için hedef (x,y)'ye karşılık gelen kaynak hücre.</summary>
        private static (int, int) Source(int x, int y, int n, int t)
        {
            int m = n - 1;
            switch (t)
            {
                case 0: return (x, y);
                case 1: return (m - x, y);
                case 2: return (x, m - y);
                case 3: return (m - x, m - y);
                case 4: return (y, x);
                case 5: return (m - y, x);
                case 6: return (y, m - x);
                default: return (m - y, m - x);
            }
        }

        /// <summary>Aynı imzayı paylaşan bölüm kimliği grupları (yalnızca 2+ üyeli).</summary>
        public static List<List<string>> Duplicates(IEnumerable<LevelData> levels)
        {
            var groups = new Dictionary<string, List<string>>();
            foreach (var d in levels)
            {
                var key = Canonical(d);
                if (!groups.TryGetValue(key, out var g)) groups[key] = g = new List<string>();
                g.Add(d.Id);
            }
            var result = new List<List<string>>();
            foreach (var g in groups.Values) if (g.Count > 1) result.Add(g);
            return result;
        }
    }
}
