using System;
using System.Collections.Generic;
using System.Text;

namespace KatKat.Core.Live
{
    /// <summary>
    /// Ölçüm botları (gerçek oyuncu modeli değil; dengeyi kaba ölçmek için). Hepsi yalnız yasal hamle seçer, eşitlikte
    /// tohumlu rastgele.
    /// - Random: düz rastgele.
    /// - Greedy: en çok puanı alır; puan yoksa bir sonraki turda en iyi puanı hazırlayan değişimi yapar. Bilerek saldırmaz.
    /// - Aggressive: rakibin hemen alabileceği en iyi puanı kendi en iyi puanından büyük ya da eşitse onu en çok düşüren
    ///   saldırıyı yapar; değilse Greedy.
    /// - Balanced: her hamleyi (kendi kazancı − rakibin ardından alabileceği en iyi puan + 0,5 × kendi sonraki en iyi puanı)
    ///   ile değerlendirir.
    /// </summary>
    public enum LiveBotKind { Random, Greedy, Aggressive, Balanced }

    public static class LiveBots
    {
        public static LiveCommand Choose(LiveMatch m, LiveBotKind kind, LiveGenerator.Rng rng)
        {
            int p = m.Current;
            var moves = m.LegalMoves(p);
            if (moves.Count == 0) return LiveCommand.Pass(p, m.NextSeq(p));
            LiveMove pick;
            switch (kind)
            {
                case LiveBotKind.Random: pick = moves[rng.Range(moves.Count)]; break;
                case LiveBotKind.Greedy: pick = Greedy(m, p, moves, rng); break;
                case LiveBotKind.Aggressive: pick = Aggressive(m, p, moves, rng); break;
                default: pick = Balanced(m, p, moves, rng); break;
            }
            return m.ToCommand(p, pick);
        }

        private static LiveMove Best(List<LiveMove> moves, Func<LiveMove, double> value, LiveGenerator.Rng rng)
        {
            double best = double.NegativeInfinity;
            var ties = new List<LiveMove>();
            foreach (var mv in moves)
            {
                double v = value(mv);
                if (v > best + 1e-9) { best = v; ties.Clear(); ties.Add(mv); }
                else if (Math.Abs(v - best) <= 1e-9) ties.Add(mv);
            }
            return ties[rng.Range(ties.Count)];
        }

        /// <summary>Kendi tahtada hamleden sonra kalan elle bir sonraki en iyi puan.</summary>
        private static int NextBestAfterOwn(LiveMatch m, int p, LiveMove mv)
        {
            var b = Clone(m.Boards[p]);
            var hand = new List<LayerColor>(m.Hands[p]);
            if (mv.Type == LiveCommandType.TapGroup)
            {
                LiveMoves.Peel(b, GroupFinder.Component(b, b.X(mv.Target), b.Y(mv.Target)), null);
            }
            else
            {
                hand.RemoveAt(mv.Slot);
                LiveMoves.ApplyOwn(b, mv.Target, mv.Color, null);
            }
            return LiveMoves.BestImmediateScore(b, hand, m.Rules.TapReadyGroups);
        }

        private static int OppBestAfterAttack(LiveMatch m, int p, LiveMove mv)
        {
            int o = m.Opponent(p);
            var b = Clone(m.Boards[o]);
            if (mv.Side == LiveSide.Opponent && mv.Type == LiveCommandType.Swap) LiveMoves.ApplyAttack(b, mv.Target, mv.Color);
            return LiveMoves.BestImmediateScore(b, m.Hands[o], m.Rules.TapReadyGroups);
        }

        private static LiveMove Greedy(LiveMatch m, int p, List<LiveMove> moves, LiveGenerator.Rng rng)
        {
            int bestGain = 0;
            foreach (var mv in moves) bestGain = Math.Max(bestGain, mv.Gain);
            if (bestGain > 0) return Best(moves, mv => mv.Gain, rng);
            var own = moves.FindAll(mv => mv.Side == LiveSide.Own);
            if (own.Count == 0) return moves[rng.Range(moves.Count)];
            return Best(own, mv => NextBestAfterOwn(m, p, mv), rng);
        }

        private static LiveMove Aggressive(LiveMatch m, int p, List<LiveMove> moves, LiveGenerator.Rng rng)
        {
            int myBest = 0;
            foreach (var mv in moves) myBest = Math.Max(myBest, mv.Gain);
            int oppNow = LiveMoves.BestImmediateScore(m.Boards[m.Opponent(p)], m.Hands[m.Opponent(p)], m.Rules.TapReadyGroups);
            var attacks = moves.FindAll(mv => mv.Side == LiveSide.Opponent);
            if (attacks.Count > 0 && oppNow > 0 && oppNow >= myBest)
            {
                var pick = Best(attacks, mv => -OppBestAfterAttack(m, p, mv), rng);
                if (OppBestAfterAttack(m, p, pick) < oppNow) return pick;
            }
            return Greedy(m, p, moves, rng);
        }

        private static LiveMove Balanced(LiveMatch m, int p, List<LiveMove> moves, LiveGenerator.Rng rng)
        {
            return Best(moves, mv =>
            {
                double v = mv.Gain - OppBestAfterAttack(m, p, mv);
                if (mv.Side == LiveSide.Own) v += 0.5 * NextBestAfterOwn(m, p, mv);
                else v += 0.5 * LiveMoves.BestImmediateScore(m.Boards[p], WithoutSlot(m.Hands[p], mv.Slot), m.Rules.TapReadyGroups);
                return v;
            }, rng);
        }

        private static List<LayerColor> WithoutSlot(List<LayerColor> hand, int slot)
        {
            var h = new List<LayerColor>(hand);
            if (slot >= 0) h.RemoveAt(slot);
            return h;
        }

        public static BoardState Clone(BoardState b)
        {
            var c = new BoardState(b.Width, b.Height);
            for (int i = 0; i < b.Count; i++)
            {
                c[i].Active = b[i].Active;
                c[i].Layers.AddRange(b[i].Layers);
            }
            return c;
        }
    }

    /// <summary>
    /// Ölçüm: havuzdaki her dizilim, sıra ters çevrilerek iki kez oynatılır (aynı dizilim, P1 önce / P2 önce).
    /// İlk oyuncu avantajı = ilk oynayanın kazanma oranı − ikincinin; maç süresi = hamle (tur) sayısı, pas ve puan hamlesi
    /// sayısı; saldırı tercihi = kabul edilen hamle türlerinin payı ve saldırının rakibin en iyi hemen puanını ne kadar
    /// düşürdüğü.
    /// </summary>
    public static class LiveSim
    {
        public static readonly LiveTieBreak[] TieRules =
            { LiveTieBreak.None, LiveTieBreak.BiggestGroup, LiveTieBreak.SecondMover, LiveTieBreak.BiggestGroupThenSecond };

        public sealed class Summary
        {
            public string Label = "";
            public int Matches, FirstWins, SecondWins, Draws;
            public readonly int[] FirstWinsBy = new int[4], SecondWinsBy = new int[4], DrawsBy = new int[4];

            /// <summary>Eşitlik bozma tablosu satırı: her kural için ilk / ikinci / berabere yüzdesi.</summary>
            public string ToTieRow()
            {
                var sb = new StringBuilder("| ").Append(Label).Append(" |");
                for (int t = 0; t < TieRules.Length; t++)
                {
                    double n = Math.Max(1, Matches);
                    sb.Append($" {100 * FirstWinsBy[t] / n:0.0} / {100 * SecondWinsBy[t] / n:0.0} / {100 * DrawsBy[t] / n:0.0} |");
                }
                return sb.ToString();
            }

            public static string TieHeader()
            {
                var sb = new StringBuilder("| Eşleşme |");
                foreach (var t in TieRules) sb.Append(' ').Append(LiveRules.TieBreakName(t)).Append(" |");
                sb.Append("\n|---|");
                foreach (var _ in TieRules) sb.Append("---|");
                return sb.ToString();
            }
            public long FirstScore, SecondScore;
            public long Turns, Passes;
            public readonly long[] Kinds = new long[5];
            public long AttackDrop, AttacksMeasured;
            public long EmptyHandTurns;
            public int MaxScore;

            public double FirstWinRate => Matches == 0 ? 0 : (double)FirstWins / Matches;
            public double SecondWinRate => Matches == 0 ? 0 : (double)SecondWins / Matches;
            public double DrawRate => Matches == 0 ? 0 : (double)Draws / Matches;

            public string ToMarkdownRow()
            {
                long moves = 0;
                foreach (var k in Kinds) moves += k;
                string Pct(long a) => moves == 0 ? "0" : (100.0 * a / moves).ToString("0.0");
                return $"| {Label} | {Matches} | {100 * FirstWinRate:0.0} | {100 * SecondWinRate:0.0} | {100 * DrawRate:0.0} | " +
                       $"{(double)FirstScore / Math.Max(1, Matches):0.00} | {(double)SecondScore / Math.Max(1, Matches):0.00} | " +
                       $"{(double)Turns / Math.Max(1, Matches):0.0} | {(double)Passes / Math.Max(1, Matches):0.00} | " +
                       $"{Pct(Kinds[(int)LiveMoveKind.Score])} | {Pct(Kinds[(int)LiveMoveKind.Prep])} | {Pct(Kinds[(int)LiveMoveKind.Attack])} | " +
                       $"{Pct(Kinds[(int)LiveMoveKind.Tap])} | {Pct(Kinds[(int)LiveMoveKind.Pass])} | " +
                       $"{(AttacksMeasured == 0 ? 0 : (double)AttackDrop / AttacksMeasured):0.00} |";
            }

            public const string MarkdownHeader =
                "| Eşleşme | Maç | İlk kazanır % | İkinci kazanır % | Berabere % | İlk ort. puan | İkinci ort. puan | Hamle/maç | Pas/maç | Puan % | Hazırlık % | Saldırı % | Dokunma % | Pas % | Saldırı rakibin en iyisini düşürdü (ort.) |\n" +
                "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|";
        }

        /// <summary>Tek maç; botlar oyuncu sırasına göre (bots[0] = P1).</summary>
        public static LiveMatch Play(LiveLayout l, LiveRules rules, int first, LiveBotKind[] bots, long seed, Summary s = null)
        {
            var m = new LiveMatch(l, rules, first);
            var rng = new LiveGenerator.Rng(seed);
            int guard = 0;
            while (!m.IsOver && guard++ < 1000)
            {
                int p = m.Current;
                int oppBefore = s != null ? LiveMoves.BestImmediateScore(m.Boards[m.Opponent(p)], m.Hands[m.Opponent(p)], rules.TapReadyGroups) : 0;
                if (s != null && m.Hands[p].Count == 0) s.EmptyHandTurns++;
                var cmd = LiveBots.Choose(m, bots[p], rng);
                var r = m.Submit(cmd);
                if (!r.Accepted) throw new InvalidOperationException("Bot geçersiz hamle: " + cmd + " " + r.Reject);
                if (s == null) continue;
                s.Kinds[(int)r.Kind]++;
                s.Turns++;
                if (r.Kind == LiveMoveKind.Pass) s.Passes++;
                if (r.Kind == LiveMoveKind.Attack)
                {
                    int oppAfter = LiveMoves.BestImmediateScore(m.Boards[m.Opponent(p)], m.Hands[m.Opponent(p)], rules.TapReadyGroups);
                    s.AttackDrop += oppBefore - oppAfter;
                    s.AttacksMeasured++;
                }
            }
            if (s != null)
            {
                s.Matches++;
                int second = 1 - first;
                s.FirstScore += m.Scores[first];
                s.SecondScore += m.Scores[second];
                s.MaxScore = Math.Max(s.MaxScore, Math.Max(m.Scores[0], m.Scores[1]));
                // Ana sütunlar ham puanla (eşitlik bozmasız) — önceki tablolarla karşılaştırılabilir.
                int raw = m.WinnerBy(LiveTieBreak.None);
                if (raw == -1) s.Draws++;
                else if (raw == first) s.FirstWins++;
                else s.SecondWins++;
                // Aynı maç her eşitlik kuralına göre (botlar kuralı hesaba katmaz; kural yalnız sonucu okur).
                for (int t = 0; t < TieRules.Length; t++)
                {
                    int w = m.WinnerBy(TieRules[t]);
                    if (w == -1) s.DrawsBy[t]++;
                    else if (w == first) s.FirstWinsBy[t]++;
                    else s.SecondWinsBy[t]++;
                }
            }
            return m;
        }

        /// <summary>Havuzun her dizilimi iki sırayla; aynı bot türü iki tarafta (sıra etkisi) ya da karışık eşleşme.</summary>
        public static Summary Run(IList<LiveLayout> pool, LiveRules rules, LiveBotKind a, LiveBotKind b, long seed)
        {
            var s = new Summary { Label = a == b ? a.ToString() : a + " (ilk) – " + b };
            for (int i = 0; i < pool.Count; i++)
            {
                // Botlar ilk/ikinci rolüne bağlı: rol a ilk oynar. Sıra ters: aynı dizilimde diğer oyuncu ilk.
                Play(pool[i], rules, 0, new[] { a, b }, seed + i * 2, s);
                Play(pool[i], rules, 1, new[] { b, a }, seed + i * 2 + 1, s);
            }
            return s;
        }

        public static string Describe(IList<LiveLayout> pool, LiveRules rules)
        {
            var sb = new StringBuilder();
            var tagCount = new Dictionary<string, int>();
            int[] targets = new int[64];
            var sizes = new long[8];
            var rows = new long[3];
            int minDef = int.MaxValue;
            double meanDef = 0;
            double opps = 0, tg = 0, maxG = 0, colors = 0, attacks = 0;
            foreach (var l in pool)
            {
                var r = LiveOpening.Analyze(l.CreateBoard(), l.Hand, rules);
                foreach (var t in r.Tags) { tagCount.TryGetValue(t, out int k); tagCount[t] = k + 1; }
                targets[Math.Min(63, r.Targets)]++;
                for (int i = 0; i < 8; i++) sizes[i] += r.GroupSizes[i];
                for (int i = 0; i < 3; i++) rows[i] += r.Rows[i];
                minDef = Math.Min(minDef, r.MinDefenderOptions);
                meanDef += r.MeanDefenderOptions;
                opps += r.Opportunities; tg += r.Targets; maxG += r.MaxGroup; colors += r.ColorsWithOpportunity; attacks += r.Attacks;
            }
            int n = Math.Max(1, pool.Count);
            sb.Append("- Dizilim: ").Append(pool.Count).Append('\n');
            sb.Append($"- Açılışta geçerli hedef hücresi (ortalama): {tg / n:0.0}; fırsat (renk+hedef): {opps / n:0.0}; fırsatlı renk: {colors / n:0.00}; en büyük grup ort.: {maxG / n:0.00}\n");
            sb.Append($"- Yasal açılış saldırısı (ortalama): {attacks / n:0.0}; tek saldırıdan sonra savunanın puan hamlesi: en az {minDef}, ortalama {meanDef / n:0.0}\n");
            sb.Append("- Grup büyüklüğü dağılımı (bütün fırsatlar): ");
            for (int i = 3; i < 8; i++) sb.Append(i == 7 ? "7+" : i.ToString()).Append('=').Append(sizes[i]).Append(i < 7 ? ", " : "\n");
            sb.Append($"- Fırsat konumu (hedef satırı): üst üçte bir {rows[0]}, orta {rows[1]}, alt {rows[2]}\n");
            sb.Append("- Açılış türleri (dizilim sayısı): ");
            foreach (var t in LiveOpening.All) { tagCount.TryGetValue(t, out int k); sb.Append(t).Append('=').Append(k).Append(' '); }
            sb.Append('\n');
            sb.Append("- Geçerli hedef sayısı dağılımı: ");
            for (int i = 0; i < 64; i++) if (targets[i] > 0) sb.Append(i).Append(':').Append(targets[i]).Append(' ');
            sb.Append('\n');
            return sb.ToString();
        }
    }
}
