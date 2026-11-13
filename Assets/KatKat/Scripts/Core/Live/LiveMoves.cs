using System.Collections.Generic;

namespace KatKat.Core.Live
{
    /// <summary>Hamle türü. Swap: elden renk değişimi. TapGroup: hazır gruba dokunma (ayar açıksa). Pass: yasal hamle yoksa.</summary>
    public enum LiveCommandType { Swap, TapGroup, Pass }

    /// <summary>Hedef tahta: hamleyi yapanın kendi tahtası ya da rakibinki.</summary>
    public enum LiveSide { Own = 0, Opponent = 1 }

    public enum LiveReject
    {
        None,
        MatchOver,
        NotYourTurn,
        Duplicate,
        BadHandSlot,
        OutOfBounds,
        EmptyCell,
        SameColor,
        AttackMakesGroup,
        TapDisabled,
        NoReadyGroup,
        PassWithLegalMove,
    }

    /// <summary>
    /// Bir oyuncu komutu. <see cref="Seq"/> oyuncunun artan komut numarası: aynı ya da eski numara ikinci kez işlenmez
    /// (ağlı sürüm için; yerelde de denetlenir).
    /// </summary>
    public struct LiveCommand
    {
        public LiveCommandType Type;
        public int Player;
        public int Seq;
        public int HandSlot;
        public LiveSide Side;
        public int X, Y;

        public static LiveCommand Swap(int player, int seq, int slot, LiveSide side, int x, int y) =>
            new LiveCommand { Type = LiveCommandType.Swap, Player = player, Seq = seq, HandSlot = slot, Side = side, X = x, Y = y };
        public static LiveCommand Tap(int player, int seq, int x, int y) =>
            new LiveCommand { Type = LiveCommandType.TapGroup, Player = player, Seq = seq, X = x, Y = y };
        public static LiveCommand Pass(int player, int seq) => new LiveCommand { Type = LiveCommandType.Pass, Player = player, Seq = seq };

        public override string ToString() => Type switch
        {
            LiveCommandType.Swap => $"P{Player + 1} #{Seq} el[{HandSlot}] → {(Side == LiveSide.Own ? "kendi" : "rakip")} ({X},{Y})",
            LiveCommandType.TapGroup => $"P{Player + 1} #{Seq} dokun ({X},{Y})",
            _ => $"P{Player + 1} #{Seq} pas",
        };
    }

    /// <summary>
    /// Bir el rengiyle bir hedef hücrede tamamlanan grup (belge §2: fırsat = el rengi + hedef koordinat + oluşan grup).
    /// Aynı renkteki birden çok el taşı aynı hedefte ek fırsat sayılmaz.
    /// </summary>
    public struct LiveOpportunity
    {
        public LayerColor Color;
        public int Target;
        public List<int> Group;
        public override string ToString() => $"{Color}@{Target} ({Group.Count})";
    }

    /// <summary>Arayüzden bağımsız kural hesapları (zincirsiz, iki katmanlı canlı maç).</summary>
    public static class LiveMoves
    {
        /// <summary>Değişim hedefi olabilir: etkin ve en az bir katmanı var.</summary>
        public static bool IsTarget(BoardState b, int i) => b[i].Active && b[i].Layers.Count > 0;

        /// <summary>
        /// Hedef hücrenin üstü <paramref name="color"/> olsaydı hedefi içeren kenar bağlı grup (geçici değiştirip geri alır).
        /// </summary>
        public static List<int> GroupIfSwapped(BoardState b, int target, LayerColor color)
        {
            var cell = b[target];
            int top = cell.Layers.Count - 1;
            var old = cell.Layers[top];
            cell.Layers[top] = color;
            var g = GroupFinder.Component(b, b.X(target), b.Y(target));
            cell.Layers[top] = old;
            return g;
        }

        /// <summary>Tahtada hazır (≥3) gruplar.</summary>
        public static List<List<int>> ReadyGroups(BoardState b)
        {
            var list = new List<List<int>>();
            foreach (var c in GroupFinder.AllComponents(b)) if (c.Count >= LiveRules.MinGroup) list.Add(c);
            return list;
        }

        public static bool HasReadyGroup(BoardState b) => GroupFinder.HasValidGroup(b);

        /// <summary>Eldeki farklı renkler (sırası el sırası).</summary>
        public static List<LayerColor> DistinctColors(IList<LayerColor> hand)
        {
            var list = new List<LayerColor>(4);
            foreach (var h in hand) if (!list.Contains(h)) list.Add(h);
            return list;
        }

        /// <summary>Kendi tahtada hemen puan getiren bütün değişimler (renk + hedef bir kez).</summary>
        public static List<LiveOpportunity> Opportunities(BoardState b, IList<LayerColor> hand)
        {
            var list = new List<LiveOpportunity>();
            var colors = DistinctColors(hand);
            for (int i = 0; i < b.Count; i++)
            {
                if (!IsTarget(b, i)) continue;
                foreach (var c in colors)
                {
                    if (b[i].Top == c) continue;
                    var g = GroupIfSwapped(b, i, c);
                    if (g.Count >= LiveRules.MinGroup) list.Add(new LiveOpportunity { Color = c, Target = i, Group = g });
                }
            }
            return list;
        }

        /// <summary>Kendi tahtadaki en iyi hemen puan (değişim; ayar açıksa hazır gruba dokunma da).</summary>
        public static int BestImmediateScore(BoardState b, IList<LayerColor> hand, bool tapReady)
        {
            int best = 0;
            foreach (var o in Opportunities(b, hand)) if (o.Group.Count > best) best = o.Group.Count;
            if (tapReady) foreach (var g in ReadyGroups(b)) if (g.Count > best) best = g.Count;
            return best;
        }

        /// <summary>Rakip tahtasına <paramref name="color"/> ile <paramref name="target"/> saldırısı geçerli mi.</summary>
        public static LiveReject CheckAttack(BoardState opp, int target, LayerColor color, LiveRules rules)
        {
            if (!IsTarget(opp, target)) return LiveReject.EmptyCell;
            if (opp[target].Top == color) return LiveReject.SameColor;
            if (rules.ForbidAttackGroup && GroupIfSwapped(opp, target, color).Count >= LiveRules.MinGroup) return LiveReject.AttackMakesGroup;
            return LiveReject.None;
        }

        /// <summary>Kendi tahtaya değişim geçerli mi (grup oluşması gerekmez; hazırlık hamlesi).</summary>
        public static LiveReject CheckOwn(BoardState own, int target, LayerColor color)
        {
            if (!IsTarget(own, target)) return LiveReject.EmptyCell;
            if (own[target].Top == color) return LiveReject.SameColor;
            return LiveReject.None;
        }

        /// <summary>
        /// Kendi tahtada değişim: üst renk değişir, hedefi içeren grup ≥3 ise her hücresinden bir katman soyulur (zincir yok).
        /// Döner: soyulan katman sayısı (= puan).
        /// </summary>
        public static int ApplyOwn(BoardState own, int target, LayerColor color, List<int> peeled)
        {
            var cell = own[target];
            cell.Layers[cell.Layers.Count - 1] = color;
            var g = GroupFinder.Component(own, own.X(target), own.Y(target));
            if (g.Count < LiveRules.MinGroup) return 0;
            Peel(own, g, peeled);
            return g.Count;
        }

        /// <summary>Grubun her hücresinden tam bir katman. Alttan çıkan renkler bu hamlede yeniden değerlendirilmez.</summary>
        public static void Peel(BoardState b, List<int> group, List<int> peeled)
        {
            foreach (int i in group)
            {
                var l = b[i].Layers;
                l.RemoveAt(l.Count - 1);
                peeled?.Add(i);
            }
        }

        public static void ApplyAttack(BoardState opp, int target, LayerColor color)
        {
            var cell = opp[target];
            cell.Layers[cell.Layers.Count - 1] = color;
        }
    }
}
