using System.Collections.Generic;
using System.Text;
using KatKat.Core;

namespace KatKat.Levels
{
    /// <summary>
    /// Düğüm bütçeli genişlik öncelikli arama. Seviye tasarımında en kısa çözümü ve ilk hamlelerin
    /// sonucunu raporlar. Bütçe aşımı "çözümsüz" değil Unknown olarak döner.
    /// Çözüm garantisi için yayın havuzu yine kayıtlı çözüm tekrarına dayanır.
    /// </summary>
    public static class LevelSolver
    {
        public static SolveResult Solve(LevelState initial, int nodeBudget = 200000)
        {
            var result = new SolveResult();
            var start = initial.Clone();
            if (start.Status == GameStatus.Won) { result.Outcome = SolveOutcome.Solved; return result; }

            var visited = new HashSet<string> { Key(start) };
            var queue = new Queue<Node>();
            queue.Enqueue(new Node { State = start, Path = new List<Command>() });
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var cmd in Moves(node.State))
                {
                    var next = node.State.Clone();
                    var r = RulesEngine.TryApply(next, cmd);
                    if (!r.Accepted) continue;
                    result.NodesExpanded++;
                    var path = new List<Command>(node.Path) { cmd };
                    if (next.Status == GameStatus.Won)
                    {
                        result.Outcome = SolveOutcome.Solved;
                        result.Solution = path;
                        return result;
                    }
                    if (next.IsTerminal) continue;
                    if (!visited.Add(Key(next))) continue;
                    if (result.NodesExpanded >= nodeBudget) { result.Outcome = SolveOutcome.BudgetExceeded; return result; }
                    queue.Enqueue(new Node { State = next, Path = path });
                }
            }
            result.Outcome = SolveOutcome.Unsolvable;
            return result;
        }

        /// <summary>Her bağlı grup için tek temsilci dokunuş (y, x sırasında ilk hücre) ve dolu ruloda etkili satırlar.</summary>
        public static List<Command> Moves(LevelState s)
        {
            var list = new List<Command>();
            if (s.IsTerminal) return list;
            // Hamle bitince (v6: tepside etkili taş varken oyun sürer) yalnız tepsi kullanımı yasal; dokunuş adayı üretilmez.
            if (s.MovesLeft > 0)
            foreach (var comp in GroupFinder.AllComponents(s.Board))
            {
                if (comp.Count < GroupFinder.MinGroup) continue;
                list.Add(Command.Tap(s.Board.X(comp[0]), s.Board.Y(comp[0])));
                // v5: özel taş dokunulan hücrede doğar; büyük grupta yer önemlidir → orta ve son hücre de denenir.
                if (s.SpecialsEnabled && comp.Count >= RulesEngine.RocketGroupSize)
                {
                    int mid = comp[comp.Count / 2], last = comp[comp.Count - 1];
                    list.Add(Command.Tap(s.Board.X(mid), s.Board.Y(mid)));
                    list.Add(Command.Tap(s.Board.X(last), s.Board.Y(last)));
                }
            }
            // v6 tepsi: her özel taş türü için ilk yuva; roket satır/sütun başına bir hedef, Altın Rulo renk başına bir.
            int seenKinds = 0;
            for (int slot = 0; slot < s.Tray.Count; slot++)
            {
                var kind = s.Tray[slot];
                if ((seenKinds & (1 << (int)kind)) != 0) continue;
                seenKinds |= 1 << (int)kind;
                var b = s.Board;
                if (kind == SpecialKind.RocketRow)
                {
                    // vr1 P3: alan başına ilk etkin hücre (iki alanlı tahtada roket yalnız kendi alanında işler); tek alanlı
                    // tahtada eskisi gibi satırın ilk etkin hücresi (aday sırası değişmez).
                    for (int y = 0; y < b.Height; y++)
                    {
                        int areas = 0;
                        for (int x = 0; x < b.Width; x++)
                        {
                            if (!b[x, y].Active || (areas & (1 << b[x, y].Area)) != 0) continue;
                            areas |= 1 << b[x, y].Area;
                            if (RulesEngine.TrayHasEffect(b, kind, x, y)) list.Add(Command.UseSpecial(slot, x, y));
                        }
                    }
                }
                else if (kind == SpecialKind.RocketColumn)
                {
                    for (int x = 0; x < b.Width; x++)
                    {
                        int areas = 0;
                        for (int y = 0; y < b.Height; y++)
                        {
                            if (!b[x, y].Active || (areas & (1 << b[x, y].Area)) != 0) continue;
                            areas |= 1 << b[x, y].Area;
                            if (RulesEngine.TrayHasEffect(b, kind, x, y)) list.Add(Command.UseSpecial(slot, x, y));
                        }
                    }
                }
                else
                {
                    int colors = 0;
                    for (int i = 0; i < b.Count; i++)
                    {
                        var c = b[i];
                        if (!c.IsSelectable) continue;
                        int key = CellState.Bit(c.Top) << (4 * c.Area); // vr1 P3: renk başına, alan başına
                        if ((colors & key) != 0) continue;
                        colors |= key;
                        list.Add(Command.UseSpecial(slot, b.X(i), b.Y(i)));
                    }
                }
            }
            if (s.MovesLeft <= 0) return list;
            if (s.RollSlotFull)
                for (int y = 0; y < s.Board.Height; y++)
                    if (RulesEngine.RollTargets(s.Board, y).Count > 0) list.Add(Command.Roll(y));
            for (int i = 0; i < s.Board.Count; i++)
            {
                var c = s.Board[i];
                if (c.IsRotatable) list.Add(Command.Rotate(s.Board.X(i), s.Board.Y(i)));
                // v5: özel taşa dokunuş (birleşim ortağı komşudan kendiliğinden seçilir).
                if (c.Active && c.HasSpecial) list.Add(Command.Tap(s.Board.X(i), s.Board.Y(i)));
                // M12 boya: yalnızca komşu açık yığının rengine (anlamlı adaylar; dal sayısı sınırlı).
                if (s.PaintLeft > 0 && c.IsSelectable)
                {
                    int x = s.Board.X(i), y = s.Board.Y(i), seen = CellState.Bit(c.Top);
                    int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dx[d], ny = y + dy[d];
                        if (!s.Board.InBounds(nx, ny) || !s.Board[nx, ny].IsSelectable) continue;
                        var col = s.Board[nx, ny].Top;
                        if ((seen & CellState.Bit(col)) != 0) continue;
                        seen |= CellState.Bit(col);
                        list.Add(Command.Paint(x, y, col));
                    }
                }
            }
            return list;
        }

        /// <summary>Durum anahtarı (ipucu eşdeğerliği de kullanır): puan hariç bütün mantıksal durum.</summary>
        public static string StateKey(LevelState s) => Key(s);

        private static string Key(LevelState s)
        {
            var sb = new StringBuilder();
            sb.Append(s.MovesLeft).Append('p').Append(s.PaintLeft).Append('|').Append(s.RollSlotFull ? 'R' : '-').Append('|');
            // vr1 P3: aşama ve mühür durumu (eski bölümlerde yazılmaz: anahtar değişmez).
            if (s.ActiveStage != 0 || s.SealWave != 0) sb.Append('a').Append(s.ActiveStage).Append(s.SealOpen ? 'S' : 's').Append('|');
            foreach (var g in s.Goals) sb.Append(g.DisplayProgress).Append(',');
            sb.Append('|');
            foreach (var t in s.Tray) sb.Append((int)t);
            sb.Append('|');
            for (int i = 0; i < s.Board.Count; i++)
            {
                var c = s.Board[i];
                foreach (var l in c.Layers) sb.Append((char)('0' + (int)l));
                if (c.ClipHitsRemaining > 0) sb.Append('k').Append(c.ClipHitsRemaining);
                if (c.IceHitsRemaining > 0) sb.Append('i').Append(c.IceHitsRemaining);
                if (c.LockRemaining > 0) sb.Append('l').Append((int)c.LockColor).Append(':').Append(c.LockRemaining);
                if (c.HasEnvelope) sb.Append('e').Append((int)c.EnvelopeColor);
                if (c.HasRotor) sb.Append('r');
                if (c.HasBottomStar) sb.Append('*');
                if (c.HasSpecial) sb.Append('s').Append((int)c.Special).Append((int)c.SpecialColor);
                if (c.SpongeNeed != 0) sb.Append('g').Append(c.SpongeNeed).Append(':').Append(c.SpongeHave);
                if (c.LinkHits > 0) sb.Append('n').Append(c.LinkId).Append(':').Append(c.LinkHits);
                if (c.ButtonGate > 0) sb.Append('B');
                if (!c.Active) sb.Append('#');
                sb.Append('/');
            }
            return sb.ToString();
        }

        private sealed class Node
        {
            public LevelState State;
            public List<Command> Path;
        }
    }
}
