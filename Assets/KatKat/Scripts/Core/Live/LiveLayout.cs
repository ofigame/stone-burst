using System;
using System.Collections.Generic;
using System.Text;

namespace KatKat.Core.Live
{
    /// <summary>
    /// Ortak başlangıç dizilimi: hücre maskesi, alt ve üst renk, ilk el. İki oyuncunun tahtası ve eli bundan bire bir aynı
    /// kurulur. Metin satırı (havuz dosyası):
    /// <c>id|gen|seed|W|H|maske(1/0)|alt(0-3, etkin değilse .)|üst|el|türler</c>.
    /// </summary>
    public sealed class LiveLayout
    {
        public string Id = "";
        public int GenVersion;
        public long Seed;
        public int Width, Height;
        public bool[] Mask;
        public LayerColor[] Bottom, Top;
        public LayerColor[] Hand;
        /// <summary>Açılış türleri (<see cref="LiveOpening"/> etiketleri, virgülle).</summary>
        public string Tags = "";

        public int Count => Width * Height;

        /// <summary>Bir oyuncunun başlangıç tahtası (her çağrıda yeni kopya).</summary>
        public BoardState CreateBoard()
        {
            var b = new BoardState(Width, Height);
            for (int i = 0; i < Count; i++)
            {
                var c = b[i];
                c.Active = Mask[i];
                if (!Mask[i]) continue;
                c.Layers.Add(Bottom[i]);
                c.Layers.Add(Top[i]);
            }
            return b;
        }

        public List<LayerColor> CreateHand() => new List<LayerColor>(Hand);

        public string ToLine()
        {
            var sb = new StringBuilder();
            sb.Append(Id).Append('|').Append(GenVersion).Append('|').Append(Seed).Append('|')
              .Append(Width).Append('|').Append(Height).Append('|');
            for (int i = 0; i < Count; i++) sb.Append(Mask[i] ? '1' : '0');
            sb.Append('|');
            for (int i = 0; i < Count; i++) sb.Append(Mask[i] ? (char)('0' + (int)Bottom[i]) : '.');
            sb.Append('|');
            for (int i = 0; i < Count; i++) sb.Append(Mask[i] ? (char)('0' + (int)Top[i]) : '.');
            sb.Append('|');
            foreach (var h in Hand) sb.Append((char)('0' + (int)h));
            sb.Append('|').Append(Tags);
            return sb.ToString();
        }

        public static LiveLayout Parse(string line)
        {
            var p = line.Trim().Split('|');
            if (p.Length < 9) throw new FormatException("Canlı maç dizilimi: alan eksik: " + line);
            var l = new LiveLayout
            {
                Id = p[0], GenVersion = int.Parse(p[1]), Seed = long.Parse(p[2]), Width = int.Parse(p[3]), Height = int.Parse(p[4]),
                Tags = p.Length > 9 ? p[9] : "",
            };
            int n = l.Count;
            if (p[5].Length != n || p[6].Length != n || p[7].Length != n) throw new FormatException("Canlı maç dizilimi: boyut tutmuyor: " + l.Id);
            l.Mask = new bool[n];
            l.Bottom = new LayerColor[n];
            l.Top = new LayerColor[n];
            for (int i = 0; i < n; i++)
            {
                l.Mask[i] = p[5][i] == '1';
                if (!l.Mask[i]) continue;
                l.Bottom[i] = (LayerColor)(p[6][i] - '0');
                l.Top[i] = (LayerColor)(p[7][i] - '0');
            }
            l.Hand = new LayerColor[p[8].Length];
            for (int i = 0; i < p[8].Length; i++) l.Hand[i] = (LayerColor)(p[8][i] - '0');
            return l;
        }

        /// <summary>Havuz dosyası: # ile başlayan satırlar açıklama.</summary>
        public static List<LiveLayout> ParsePool(string text)
        {
            var list = new List<LiveLayout>();
            foreach (var raw in text.Split('\n'))
            {
                var s = raw.Trim();
                if (s.Length == 0 || s[0] == '#') continue;
                list.Add(Parse(s));
            }
            return list;
        }

        public bool HasTag(string tag) => (","+ Tags + ",").Contains("," + tag + ",");
    }
}
