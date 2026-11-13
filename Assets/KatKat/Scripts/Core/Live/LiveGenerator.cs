using System;
using System.Collections.Generic;
using System.Text;

namespace KatKat.Core.Live
{
    /// <summary>Açılış türü etiketleri (belge §4) ve açılış çözümlemesi.</summary>
    public static class LiveOpening
    {
        /// <summary>Uçtan tamamlama: hedef, düz grubun ucunda (grup içinde tek komşu).</summary>
        public const string End = "uc";
        /// <summary>Aradan tamamlama: hedef iki ya da daha çok grup komşusunu birleştiriyor.</summary>
        public const string Middle = "ara";
        /// <summary>L biçimi (düz olmayan) grup.</summary>
        public const string LShape = "L";
        /// <summary>Farklı bölgelerde iki fırsat: farklı renk, grupları ayrık ve kenardan değmiyor.</summary>
        public const string TwoRegions = "iki_bolge";
        /// <summary>Üçlü ile dörtlü (ya da büyük) arasında seçim.</summary>
        public const string ThreeVsFour = "3_4";

        public static readonly string[] All = { End, Middle, LShape, TwoRegions, ThreeVsFour };

        /// <summary>Açılış ölçüleri (çeşitlilik raporu).</summary>
        public sealed class Report
        {
            public int Targets;
            public int Opportunities;
            public int ColorsWithOpportunity;
            public int MaxGroup;
            public readonly int[] GroupSizes = new int[8]; // 3,4,5,6,7,8+ (indeks = boyut, 7 = 7+)
            /// <summary>Fırsat hedeflerinin bölgesi: 0 üst, 1 orta, 2 alt üçte bir.</summary>
            public readonly int[] Rows = new int[3];
            public int Attacks;
            /// <summary>Tek saldırıdan sonra savunanın en az / ortalama puanlı hamle sayısı.</summary>
            public int MinDefenderOptions;
            public double MeanDefenderOptions;
            public readonly List<string> Tags = new List<string>();
        }

        public static bool IsStraight(BoardState b, List<int> g)
        {
            bool sameX = true, sameY = true;
            foreach (int i in g)
            {
                if (b.X(i) != b.X(g[0])) sameX = false;
                if (b.Y(i) != b.Y(g[0])) sameY = false;
            }
            return sameX || sameY;
        }

        public static int DegreeIn(BoardState b, List<int> g, int i)
        {
            int x = b.X(i), y = b.Y(i), d = 0;
            foreach (int j in g)
            {
                int dx = Math.Abs(b.X(j) - x), dy = Math.Abs(b.Y(j) - y);
                if (dx + dy == 1) d++;
            }
            return d;
        }

        private static bool Touching(BoardState b, List<int> a, List<int> c)
        {
            foreach (int i in a)
                foreach (int j in c)
                    if (i == j || Math.Abs(b.X(i) - b.X(j)) + Math.Abs(b.Y(i) - b.Y(j)) <= 1) return true;
            return false;
        }

        public static Report Analyze(BoardState board, IList<LayerColor> hand, LiveRules rules)
        {
            var r = new Report();
            var opps = LiveMoves.Opportunities(board, hand);
            r.Opportunities = opps.Count;
            var targets = new HashSet<int>();
            var colors = new HashSet<LayerColor>();
            bool end = false, mid = false, lshape = false, two = false, three = false, four = false;
            foreach (var o in opps)
            {
                targets.Add(o.Target);
                colors.Add(o.Color);
                int n = o.Group.Count;
                if (n > r.MaxGroup) r.MaxGroup = n;
                r.GroupSizes[Math.Min(n, 7)]++;
                r.Rows[Math.Min(2, board.Y(o.Target) * 3 / board.Height)]++;
                bool straight = IsStraight(board, o.Group);
                if (!straight) lshape = true;
                else if (DegreeIn(board, o.Group, o.Target) == 1) end = true;
                else mid = true;
                if (n == 3) three = true; else four = true;
            }
            for (int a = 0; a < opps.Count && !two; a++)
                for (int c = a + 1; c < opps.Count && !two; c++)
                    if (opps[a].Color != opps[c].Color && !Touching(board, opps[a].Group, opps[c].Group)) two = true;
            r.Targets = targets.Count;
            r.ColorsWithOpportunity = colors.Count;
            if (end) r.Tags.Add(End);
            if (mid) r.Tags.Add(Middle);
            if (lshape) r.Tags.Add(LShape);
            if (two) r.Tags.Add(TwoRegions);
            if (three && four) r.Tags.Add(ThreeVsFour);

            // Tek saldırıdan sonra savunanın kalan puan hamleleri.
            int min = int.MaxValue;
            long sum = 0;
            foreach (var c in LiveMoves.DistinctColors(hand))
                for (int i = 0; i < board.Count; i++)
                {
                    if (LiveMoves.CheckAttack(board, i, c, rules) != LiveReject.None) continue;
                    var top = board[i].Top;
                    LiveMoves.ApplyAttack(board, i, c);
                    int k = LiveMoves.Opportunities(board, hand).Count;
                    LiveMoves.ApplyAttack(board, i, top);
                    r.Attacks++;
                    sum += k;
                    if (k < min) min = k;
                }
            r.MinDefenderOptions = r.Attacks == 0 ? r.Opportunities : min;
            r.MeanDefenderOptions = r.Attacks == 0 ? r.Opportunities : (double)sum / r.Attacks;
            return r;
        }
    }

    /// <summary>
    /// Doğrulanmış ortak tahta üreticisi (belge §2, §4). Tohumlu; aynı tohum ve sürüm her zaman aynı adayı verir.
    /// Filtreler:
    /// 1) Açılışta kenardan bağlı 3+ aynı renk yok.
    /// 2) İlk elle en az iki farklı renkte, farklı hedef hücrelerinde 3+ grup tamamlanabilir.
    /// 3) Her yasal açılış saldırısı ayrı ayrı denenir; savunanın elinde her durumda en az bir puan hamlesi kalır.
    /// </summary>
    public static class LiveGenerator
    {
        public const int Version = 1;

        /// <summary>Maske kalıpları: 0 tam, 1 köşeler kesik, 2 ortada iki delik, 3 rastgele üç delik.</summary>
        public const int MaskKinds = 4;

        /// <summary>Deterministik, platformdan bağımsız rastgele (xorshift64*).</summary>
        public sealed class Rng
        {
            private ulong _s;
            public Rng(long seed) { _s = (ulong)seed * 0x9E3779B97F4A7C15UL + 0x2545F4914F6CDD1DUL; if (_s == 0) _s = 1; Next(); }
            public ulong Next() { _s ^= _s >> 12; _s ^= _s << 25; _s ^= _s >> 27; return _s * 0x2545F4914F6CDD1DUL; }
            public int Range(int n) => (int)(Next() % (ulong)n);
        }

        /// <summary>Tohumdan aday üretir (henüz doğrulanmamış).</summary>
        public static LiveLayout Candidate(long seed, LiveRules rules)
        {
            var rng = new Rng(seed);
            int w = rules.Width, h = rules.Height, n = w * h;
            var l = new LiveLayout { GenVersion = Version, Seed = seed, Width = w, Height = h, Mask = new bool[n], Bottom = new LayerColor[n], Top = new LayerColor[n] };
            for (int i = 0; i < n; i++) l.Mask[i] = true;
            switch (rng.Range(MaskKinds))
            {
                case 1:
                    l.Mask[0] = l.Mask[w - 1] = l.Mask[n - w] = l.Mask[n - 1] = false;
                    break;
                case 2:
                    l.Mask[(h / 2 - 1) * w + w / 2 - 1] = false;
                    l.Mask[(h / 2) * w + w / 2] = false;
                    break;
                case 3:
                    for (int k = 0; k < 3; k++) l.Mask[rng.Range(n)] = false;
                    break;
            }
            // Üst renkler: hücre hücre; 3'lü kuran renk yeniden çekilir (filtre 1'i kurulumda sağlar, filtre yine denetler).
            var board = new BoardState(w, h);
            for (int i = 0; i < n; i++)
            {
                if (!l.Mask[i]) continue;
                board[i].Active = true;
                board[i].Layers.Add(LayerColor.Coral);
                int start = rng.Range(LiveRules.ColorCount);
                for (int k = 0; k < LiveRules.ColorCount; k++)
                {
                    var c = (LayerColor)((start + k) % LiveRules.ColorCount);
                    board[i].Layers[0] = c;
                    if (GroupFinder.Component(board, board.X(i), board.Y(i)).Count < LiveRules.MinGroup) break;
                }
                l.Top[i] = board[i].Top;
                l.Bottom[i] = (LayerColor)rng.Range(LiveRules.ColorCount);
            }
            l.Hand = new LayerColor[rules.HandSize];
            for (int k = 0; k < rules.HandSize; k++) l.Hand[k] = (LayerColor)rng.Range(LiveRules.ColorCount);
            if (rules.DifferentHands)
            {
                l.GenVersion = VersionDifferentHands;
                l.Hand2 = PickSecondHand(l, rules, rng);
            }
            return l;
        }

        /// <summary>Farklı ama denk eller havuzunun üretim sürümü (kimlik öneki LM2).</summary>
        public const int VersionDifferentHands = 2;

        /// <summary>İki elin açılış değeri farkına izin: fırsat sayısında en çok %15 (en az 2) fark.</summary>
        public const double HandTolerance = 0.15;

        private static readonly int[][] Perms = BuildPerms();

        private static int[][] BuildPerms()
        {
            var list = new List<int[]>();
            for (int a = 0; a < 4; a++)
            for (int b = 0; b < 4; b++)
            for (int c = 0; c < 4; c++)
            for (int d = 0; d < 4; d++)
                if (a != b && a != c && a != d && b != c && b != d && c != d) list.Add(new[] { a, b, c, d });
            return list.ToArray();
        }

        /// <summary>
        /// İkinci el = birinci elin renk değişimi (aynı renk sayı profili: "malzeme" eşit). Renk çoklu kümesi birinciden farklı
        /// olan değişimler arasından açılış değeri birinciye en yakın olan seçilir (eşitlikte tohumlu rastgele). Uygun yoksa null
        /// (Validate reddeder).
        /// </summary>
        private static LayerColor[] PickSecondHand(LiveLayout l, LiveRules rules, Rng rng)
        {
            var b = l.CreateBoard();
            var a = HandValue(b, l.Hand);
            LayerColor[] best = null;
            int bestDiff = int.MaxValue, ties = 0;
            foreach (var perm in Perms)
            {
                var h = new LayerColor[l.Hand.Length];
                for (int i = 0; i < h.Length; i++) h[i] = (LayerColor)perm[(int)l.Hand[i]];
                if (SameMultiset(h, l.Hand)) continue;
                var v = HandValue(b, h);
                if (v.Best != a.Best) continue;
                int diff = Math.Abs(v.Opportunities - a.Opportunities);
                if (diff < bestDiff) { bestDiff = diff; best = h; ties = 1; }
                else if (diff == bestDiff && rng.Range(++ties) == 0) best = h;
            }
            return best;
        }

        public struct Value
        {
            public int Opportunities, Targets, Best;
        }

        /// <summary>Açılış değeri: fırsat (renk + hedef) sayısı, hedef hücre sayısı, en iyi hemen puan.</summary>
        public static Value HandValue(BoardState b, IList<LayerColor> hand)
        {
            var v = new Value();
            var targets = new HashSet<int>();
            foreach (var o in LiveMoves.Opportunities(b, hand))
            {
                v.Opportunities++;
                targets.Add(o.Target);
                if (o.Group.Count > v.Best) v.Best = o.Group.Count;
            }
            v.Targets = targets.Count;
            return v;
        }

        public static bool SameMultiset(IList<LayerColor> x, IList<LayerColor> y)
        {
            var n = new int[4];
            foreach (var c in x) n[(int)c]++;
            foreach (var c in y) n[(int)c]--;
            foreach (var k in n) if (k != 0) return false;
            return true;
        }

        private static string TwoColorCheck(BoardState b, IList<LayerColor> hand)
        {
            var opps = LiveMoves.Opportunities(b, hand);
            for (int a = 0; a < opps.Count; a++)
                for (int c = a + 1; c < opps.Count; c++)
                    if (opps[a].Color != opps[c].Color && opps[a].Target != opps[c].Target) return null;
            return "iki renkli fırsat yok";
        }

        /// <summary>Saldıranın her yasal açılış saldırısından sonra savunanın en az bir puan hamlesi kalır.</summary>
        private static string DefenseCheck(BoardState b, IList<LayerColor> attacker, IList<LayerColor> defender, LiveRules rules)
        {
            foreach (var c in LiveMoves.DistinctColors(attacker))
                for (int i = 0; i < b.Count; i++)
                {
                    if (LiveMoves.CheckAttack(b, i, c, rules) != LiveReject.None) continue;
                    var top = b[i].Top;
                    LiveMoves.ApplyAttack(b, i, c);
                    bool ok = LiveMoves.Opportunities(b, defender).Count > 0;
                    LiveMoves.ApplyAttack(b, i, top);
                    if (!ok) return "saldırı sonrası savunma yok";
                }
            return null;
        }

        /// <summary>
        /// Filtre sonucu; null ise geçerli, değilse ret nedeni. Farklı ellerde filtreler her el için ayrı (saldırı denetimi iki
        /// yönde) ve denklik: aynı renk sayı profili, farklı renk kümesi, eşit en iyi hemen puan, fırsat sayısı farkı en çok %15
        /// (en az 2).
        /// </summary>
        public static string Validate(LiveLayout l, LiveRules rules)
        {
            if (l.Width != rules.Width || l.Height != rules.Height) return "boyut";
            if (l.Hand == null || l.Hand.Length != rules.HandSize) return "el boyu";
            if (rules.DifferentHands != l.DifferentHands) return rules.DifferentHands ? "denk ikinci el yok" : "beklenmeyen ikinci el";
            var b = l.CreateBoard();
            for (int i = 0; i < b.Count; i++) if (b[i].Active && b[i].Layers.Count != LiveRules.LayersPerCell) return "iki katman";
            if (LiveMoves.HasReadyGroup(b)) return "hazır 3'lü";
            if (!l.DifferentHands)
                return TwoColorCheck(b, l.Hand) ?? DefenseCheck(b, l.Hand, l.Hand, rules);
            var h2 = l.Hand2;
            if (h2.Length != l.Hand.Length) return "el boyu";
            if (SameMultiset(h2, l.Hand)) return "eller aynı";
            var c1 = new int[4];
            var c2 = new int[4];
            foreach (var c in l.Hand) c1[(int)c]++;
            foreach (var c in h2) c2[(int)c]++;
            Array.Sort(c1);
            Array.Sort(c2);
            for (int k = 0; k < 4; k++) if (c1[k] != c2[k]) return "renk sayı profili farklı";
            var v1 = HandValue(b, l.Hand);
            var v2 = HandValue(b, h2);
            if (v1.Best != v2.Best) return "en iyi puan farklı";
            if (Math.Abs(v1.Opportunities - v2.Opportunities) > Math.Max(2, HandTolerance * Math.Max(v1.Opportunities, v2.Opportunities))) return "açılış değeri farklı";
            return TwoColorCheck(b, l.Hand) ?? TwoColorCheck(b, h2)
                ?? DefenseCheck(b, l.Hand, h2, rules) ?? DefenseCheck(b, h2, l.Hand, rules);
        }

        // ---- Eşdeğer kopyalar (renk adı değişimi, aynalama, döndürme) ----------------------------------------------

        /// <summary>
        /// Kanonik anahtar: maskeyi koruyan her simetri (kare tahtada 8, değilse 4) ve her renk yeniden adlandırması altında
        /// en küçük gösterim. Alt katman ve ilk el de anahtara girer. Aynı anahtar = aynı aile.
        /// </summary>
        public static string CanonicalKey(LiveLayout l)
        {
            string best = null;
            int w = l.Width, h = l.Height;
            for (int s = 0; s < 8; s++)
            {
                bool square = w == h;
                if (!square && (s & 4) != 0) continue; // devrik yalnız karede
                int tw = (s & 4) != 0 ? h : w, th = (s & 4) != 0 ? w : h;
                var map = new int[w * h];
                for (int y = 0; y < th; y++)
                    for (int x = 0; x < tw; x++)
                    {
                        int sx = x, sy = y;
                        if ((s & 4) != 0) { sx = y; sy = x; }
                        if ((s & 1) != 0) sx = w - 1 - sx;
                        if ((s & 2) != 0) sy = h - 1 - sy;
                        map[y * tw + x] = sy * w + sx;
                    }
                bool maskSame = true;
                for (int i = 0; i < map.Length && maskSame; i++) if (l.Mask[map[i]] != l.Mask[i]) maskSame = false;
                if (!maskSame) continue;
                // Renk: ilk görünme sırasına göre yeniden adlandır (üst, sonra alt, sonra el sıralı).
                var rename = new int[] { -1, -1, -1, -1 };
                int next = 0;
                int Name(LayerColor c) { int k = (int)c; if (rename[k] < 0) rename[k] = next++; return rename[k]; }
                var sb = new StringBuilder(map.Length * 2 + 16);
                for (int i = 0; i < map.Length; i++) sb.Append(l.Mask[map[i]] ? (char)('0' + Name(l.Top[map[i]])) : '.');
                sb.Append('/');
                for (int i = 0; i < map.Length; i++) sb.Append(l.Mask[map[i]] ? (char)('0' + Name(l.Bottom[map[i]])) : '.');
                sb.Append('/');
                // El yalnız renk sayılarıyla (sırası önemsiz); tahtada olmayan renkler sayıya göre adlandırılır.
                var counts = new int[4];
                foreach (var c in l.Hand) counts[(int)c]++;
                var order = new List<int> { 0, 1, 2, 3 };
                order.Sort((a, b) => counts[b].CompareTo(counts[a]));
                foreach (int c in order) if (counts[c] > 0) Name((LayerColor)c);
                var hand = new int[4];
                for (int c = 0; c < 4; c++) if (counts[c] > 0) hand[rename[c]] = counts[c];
                for (int c = 0; c < 4; c++) sb.Append(hand[c]);
                if (l.Hand2 != null)
                {
                    // İkinci el (P2): aynı adlandırmayla renk sayıları.
                    var c2 = new int[4];
                    foreach (var c in l.Hand2) c2[(int)c]++;
                    var order2 = new List<int> { 0, 1, 2, 3 };
                    order2.Sort((a, b) => c2[b].CompareTo(c2[a]));
                    foreach (int c in order2) if (c2[c] > 0) Name((LayerColor)c);
                    var hand2 = new int[4];
                    for (int c = 0; c < 4; c++) if (c2[c] > 0) hand2[rename[c]] = c2[c];
                    sb.Append('/');
                    for (int c = 0; c < 4; c++) sb.Append(hand2[c]);
                }
                var key = sb.ToString();
                if (best == null || string.CompareOrdinal(key, best) < 0) best = key;
            }
            return w + "x" + h + ":" + best;
        }

        public static string IdFor(string key, int version = Version)
        {
            uint hsh = 2166136261;
            foreach (char c in key) { hsh ^= c; hsh *= 16777619; }
            return "LM" + version + "-" + hsh.ToString("x8");
        }

        /// <summary>Üretim istatistiği (rapor).</summary>
        public sealed class Stats
        {
            public int Tried, Accepted, Duplicates, WrongTag;
            public readonly Dictionary<string, int> Rejects = new Dictionary<string, int>();
        }

        /// <summary>
        /// Havuz: tohum <paramref name="baseSeed"/>'den başlayıp sırayla dener. Türler karıştırılır: her yuva sıradaki açılış
        /// türünü ister (<see cref="LiveOpening.All"/> döngüsü); o türü taşımayan geçerli aday o yuva için atlanır.
        /// Eşdeğer kopyalar (kanonik anahtar) ayıklanır. Aynı girdiyle her zaman aynı havuz.
        /// </summary>
        public static List<LiveLayout> BuildPool(int count, long baseSeed, LiveRules rules, Stats stats = null, int maxTries = 200000)
        {
            stats ??= new Stats();
            var pool = new List<LiveLayout>();
            var keys = new HashSet<string>();
            long seed = baseSeed;
            while (pool.Count < count && stats.Tried < maxTries)
            {
                string want = LiveOpening.All[pool.Count % LiveOpening.All.Length];
                var l = Candidate(seed++, rules);
                stats.Tried++;
                var why = Validate(l, rules);
                if (why != null)
                {
                    stats.Rejects.TryGetValue(why, out int k);
                    stats.Rejects[why] = k + 1;
                    continue;
                }
                var rep = LiveOpening.Analyze(l.CreateBoard(), l.Hand, rules);
                if (!rep.Tags.Contains(want)) { stats.WrongTag++; continue; }
                var key = CanonicalKey(l);
                if (!keys.Add(key)) { stats.Duplicates++; continue; }
                l.Id = IdFor(key, l.GenVersion);
                l.Tags = string.Join(",", rep.Tags);
                pool.Add(l);
                stats.Accepted++;
            }
            return pool;
        }
    }
}
