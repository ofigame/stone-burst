using System.Collections.Generic;
using System.Text;

namespace KatKat.Core.Live
{
    /// <summary>Kabul edilen hamlenin türü (ölçüm: puan / hazırlık / saldırı / dokunma / pas).</summary>
    public enum LiveMoveKind { Score, Prep, Attack, Tap, Pass }

    /// <summary>Komut sonucu. Reddedilen komut durumu hiç değiştirmez.</summary>
    public sealed class LiveResult
    {
        public bool Accepted;
        public LiveReject Reject;
        public LiveMoveKind Kind;
        public int Player;
        public int Gained;
        public LayerColor Color;
        public int Target = -1;
        /// <summary>Katmanı soyulan hücreler (hamle yapanın tahtasında).</summary>
        public readonly List<int> Peeled = new List<int>();
    }

    /// <summary>Bir oyuncu için olası hamle (botlar, pas denetimi, önizleme). Aynı renkli el taşları bir kez sayılır.</summary>
    public struct LiveMove
    {
        public LiveCommandType Type;
        public LiveSide Side;
        public int Slot;
        public LayerColor Color;
        public int Target;
        /// <summary>Kendi tahtada hemen kazanılacak puan (saldırı ve hazırlıkta 0).</summary>
        public int Gain;
    }

    /// <summary>
    /// İki oyunculu, sırayla oynanan canlı maç durumu (arayüzden bağımsız). Tahtalar ve eller aynı dizilimden kurulur,
    /// sonra bağımsız değişir. Her oyuncu <see cref="LiveRules.TurnsPerPlayer"/> tur oynar; en çok katman soyan kazanır.
    /// </summary>
    public sealed class LiveMatch
    {
        public readonly LiveRules Rules;
        public readonly LiveLayout Layout;
        public readonly BoardState[] Boards = new BoardState[2];
        public readonly List<LayerColor>[] Hands = new List<LayerColor>[2];
        public readonly int[] Scores = new int[2];
        public readonly int[] TurnsUsed = new int[2];
        private readonly int[] _lastSeq = { 0, 0 };
        public readonly int FirstPlayer;
        public int Current { get; private set; }
        public readonly LiveRecord Record;

        public LiveMatch(LiveLayout layout, LiveRules rules, int firstPlayer = 0)
        {
            Layout = layout;
            Rules = rules.Clone();
            FirstPlayer = firstPlayer;
            Current = firstPlayer;
            for (int p = 0; p < 2; p++)
            {
                Boards[p] = layout.CreateBoard();
                Hands[p] = layout.CreateHand();
            }
            Record = new LiveRecord { LayoutId = layout.Id, GenVersion = layout.GenVersion, FirstPlayer = firstPlayer, Rules = Rules.ToString() };
            Record.Hand.AddRange(layout.Hand);
        }

        public bool IsOver => TurnsUsed[0] >= Rules.TurnsPerPlayer && TurnsUsed[1] >= Rules.TurnsPerPlayer;
        public int Opponent(int p) => 1 - p;
        public int NextSeq(int p) => _lastSeq[p] + 1;

        /// <summary>-1 berabere, yoksa kazanan oyuncu (yalnız maç bitince anlamlı).</summary>
        public int Winner => WinnerBy(Rules.TieBreak);

        /// <summary>Oyuncunun maçtaki en büyük tek soyması (bir hamlede alınan puan).</summary>
        public readonly int[] BestMove = new int[2];

        /// <summary>-1 berabere, yoksa kazanan; eşitlikte verilen kurala göre (ölçüm aynı maçı her kurala göre okur).</summary>
        public int WinnerBy(LiveTieBreak rule) => Decide(Scores, BestMove, FirstPlayer, rule);

        /// <summary>Kazanan kararı (saf işlev; test edilir).</summary>
        public static int Decide(int[] scores, int[] best, int first, LiveTieBreak rule)
        {
            if (scores[0] != scores[1]) return scores[0] > scores[1] ? 0 : 1;
            if ((rule == LiveTieBreak.BiggestGroup || rule == LiveTieBreak.BiggestGroupThenSecond) && best[0] != best[1])
                return best[0] > best[1] ? 0 : 1;
            if (rule == LiveTieBreak.SecondMover || rule == LiveTieBreak.BiggestGroupThenSecond) return 1 - first;
            return -1;
        }

        /// <summary>Eşit puanda kazananı belirleyen gerekçe (ekran için); eşit değilse null.</summary>
        public string TieReason()
        {
            if (Scores[0] != Scores[1]) return null;
            var r = Rules.TieBreak;
            if ((r == LiveTieBreak.BiggestGroup || r == LiveTieBreak.BiggestGroupThenSecond) && BestMove[0] != BestMove[1])
                return $"puan eşit; en büyük tek grup {BestMove[0]}–{BestMove[1]}";
            if (r == LiveTieBreak.SecondMover || r == LiveTieBreak.BiggestGroupThenSecond) return "puan ve en büyük grup eşit; ikinci oynayan kazanır";
            return null;
        }

        public BoardState TargetBoard(int player, LiveSide side) => Boards[side == LiveSide.Own ? player : Opponent(player)];

        /// <summary>Komutu denetler; geçerliyse uygular ve sırayı verir.</summary>
        public LiveResult Submit(LiveCommand cmd, long elapsedMs = 0)
        {
            var r = new LiveResult { Player = cmd.Player };
            r.Reject = Validate(cmd, out var color, out int target);
            if (r.Reject != LiveReject.None) return r;
            _lastSeq[cmd.Player] = cmd.Seq;
            r.Accepted = true;
            r.Color = color;
            r.Target = target;
            int p = cmd.Player;
            switch (cmd.Type)
            {
                case LiveCommandType.Swap:
                    Hands[p].RemoveAt(cmd.HandSlot);
                    if (cmd.Side == LiveSide.Own)
                    {
                        r.Gained = LiveMoves.ApplyOwn(Boards[p], target, color, r.Peeled);
                        r.Kind = r.Gained > 0 ? LiveMoveKind.Score : LiveMoveKind.Prep;
                    }
                    else
                    {
                        LiveMoves.ApplyAttack(Boards[Opponent(p)], target, color);
                        r.Kind = LiveMoveKind.Attack;
                    }
                    break;
                case LiveCommandType.TapGroup:
                {
                    var g = GroupFinder.Component(Boards[p], cmd.X, cmd.Y);
                    LiveMoves.Peel(Boards[p], g, r.Peeled);
                    r.Gained = g.Count;
                    r.Kind = LiveMoveKind.Tap;
                    break;
                }
                default:
                    r.Kind = LiveMoveKind.Pass;
                    break;
            }
            Scores[p] += r.Gained;
            if (r.Gained > BestMove[p]) BestMove[p] = r.Gained;
            TurnsUsed[p]++;
            Record.Moves.Add(new LiveRecord.Entry { Command = cmd, Kind = r.Kind, Gained = r.Gained, ElapsedMs = elapsedMs });
            AdvanceTurn();
            if (IsOver)
            {
                Record.FinalScores[0] = Scores[0];
                Record.FinalScores[1] = Scores[1];
                Record.Winner = Winner;
            }
            return r;
        }

        private void AdvanceTurn()
        {
            int o = Opponent(Current);
            if (TurnsUsed[o] < Rules.TurnsPerPlayer) Current = o;
        }

        public LiveReject Validate(LiveCommand cmd, out LayerColor color, out int target)
        {
            color = default;
            target = -1;
            if (IsOver) return LiveReject.MatchOver;
            if (cmd.Player != Current) return LiveReject.NotYourTurn;
            if (cmd.Seq <= _lastSeq[cmd.Player]) return LiveReject.Duplicate;
            int p = cmd.Player;
            switch (cmd.Type)
            {
                case LiveCommandType.Swap:
                {
                    if (cmd.HandSlot < 0 || cmd.HandSlot >= Hands[p].Count) return LiveReject.BadHandSlot;
                    var b = TargetBoard(p, cmd.Side);
                    if (!b.InBounds(cmd.X, cmd.Y)) return LiveReject.OutOfBounds;
                    color = Hands[p][cmd.HandSlot];
                    target = b.Index(cmd.X, cmd.Y);
                    return cmd.Side == LiveSide.Own ? LiveMoves.CheckOwn(b, target, color) : LiveMoves.CheckAttack(b, target, color, Rules);
                }
                case LiveCommandType.TapGroup:
                {
                    if (!Rules.TapReadyGroups) return LiveReject.TapDisabled;
                    var b = Boards[p];
                    if (!b.InBounds(cmd.X, cmd.Y)) return LiveReject.OutOfBounds;
                    target = b.Index(cmd.X, cmd.Y);
                    if (GroupFinder.Component(b, cmd.X, cmd.Y).Count < LiveRules.MinGroup) return LiveReject.NoReadyGroup;
                    return LiveReject.None;
                }
                default:
                    return HasLegalMove(p) ? LiveReject.PassWithLegalMove : LiveReject.None;
            }
        }

        /// <summary>Oyuncunun pas dışındaki bütün yasal hamleleri (renk + hedef bir kez).</summary>
        public List<LiveMove> LegalMoves(int p)
        {
            var list = new List<LiveMove>();
            var hand = Hands[p];
            var colors = LiveMoves.DistinctColors(hand);
            var own = Boards[p];
            var opp = Boards[Opponent(p)];
            foreach (var c in colors)
            {
                int slot = hand.IndexOf(c);
                for (int i = 0; i < own.Count; i++)
                {
                    if (LiveMoves.CheckOwn(own, i, c) != LiveReject.None) continue;
                    var g = LiveMoves.GroupIfSwapped(own, i, c);
                    list.Add(new LiveMove { Type = LiveCommandType.Swap, Side = LiveSide.Own, Slot = slot, Color = c, Target = i, Gain = g.Count >= LiveRules.MinGroup ? g.Count : 0 });
                }
                for (int i = 0; i < opp.Count; i++)
                {
                    if (LiveMoves.CheckAttack(opp, i, c, Rules) != LiveReject.None) continue;
                    list.Add(new LiveMove { Type = LiveCommandType.Swap, Side = LiveSide.Opponent, Slot = slot, Color = c, Target = i });
                }
            }
            if (Rules.TapReadyGroups)
                foreach (var g in LiveMoves.ReadyGroups(own))
                    list.Add(new LiveMove { Type = LiveCommandType.TapGroup, Side = LiveSide.Own, Slot = -1, Target = g[0], Gain = g.Count });
            return list;
        }

        public bool HasLegalMove(int p)
        {
            var hand = Hands[p];
            if (Rules.TapReadyGroups && LiveMoves.HasReadyGroup(Boards[p])) return true;
            if (hand.Count == 0) return false;
            var colors = LiveMoves.DistinctColors(hand);
            foreach (var b in Boards)
                for (int i = 0; i < b.Count; i++)
                {
                    if (!LiveMoves.IsTarget(b, i)) continue;
                    foreach (var c in colors)
                    {
                        if (b[i].Top == c) continue;
                        if (b == Boards[p] || LiveMoves.CheckAttack(b, i, c, Rules) == LiveReject.None) return true;
                    }
                }
            return false;
        }

        /// <summary>Olası hamleyi komuta çevirir (sıradaki numarayla).</summary>
        public LiveCommand ToCommand(int p, LiveMove m)
        {
            var b = m.Type == LiveCommandType.TapGroup ? Boards[p] : TargetBoard(p, m.Side);
            int x = b.X(m.Target), y = b.Y(m.Target);
            return m.Type == LiveCommandType.TapGroup ? LiveCommand.Tap(p, NextSeq(p), x, y) : LiveCommand.Swap(p, NextSeq(p), m.Slot, m.Side, x, y);
        }
    }

    /// <summary>Maç kaydı: dizilim kimliği, üretim sürümü, başlangıç eli, ilk oynayan, kurallar, hamleler ve sonuç.</summary>
    public sealed class LiveRecord
    {
        public struct Entry
        {
            public LiveCommand Command;
            public LiveMoveKind Kind;
            public int Gained;
            public long ElapsedMs;
        }

        public string LayoutId = "";
        public int GenVersion;
        public int FirstPlayer;
        public string Rules = "";
        public readonly List<LayerColor> Hand = new List<LayerColor>();
        public readonly List<Entry> Moves = new List<Entry>();
        public readonly int[] FinalScores = new int[2];
        /// <summary>-1 berabere; eşitlik bozma kuralı uygulanmış sonuç.</summary>
        public int Winner = -1;

        public string ToText()
        {
            var sb = new StringBuilder();
            sb.Append("dizilim=").Append(LayoutId).Append(" gen=").Append(GenVersion).Append(" ilk=P").Append(FirstPlayer + 1).Append(" el=");
            foreach (var h in Hand) sb.Append((int)h);
            sb.Append(" kurallar=[").Append(Rules).Append("]\n");
            foreach (var e in Moves) sb.Append(e.Command).Append(" → ").Append(e.Kind).Append(" +").Append(e.Gained).Append(" ").Append(e.ElapsedMs).Append("ms\n");
            sb.Append("sonuç P1=").Append(FinalScores[0]).Append(" P2=").Append(FinalScores[1])
              .Append(" kazanan=").Append(Winner < 0 ? "berabere" : "P" + (Winner + 1)).Append('\n');
            return sb.ToString();
        }
    }
}
