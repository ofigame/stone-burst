using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using KatKat.Core;

namespace KatKat.Levels
{
    /// <summary>İpucu güveni (vr1 paketi 01_IPUCU): yalnız <see cref="VerifiedPath"/> oyuncuya gösterilir.</summary>
    public enum HintConfidence { NoPlan = 0, Unknown = 1, VerifiedPath = 2 }

    /// <summary>Plan aramasının sonucu (telemetri: hint_plan_result.status).</summary>
    public enum HintPlanStatus
    {
        /// <summary>Komut + devam yolu motorla yeniden yürütülüp kazanç doğrulandı.</summary>
        Verified = 0,
        /// <summary>Düğüm ya da süre bütçesi doldu. Çözümsüz anlamına GELMEZ.</summary>
        BudgetExceeded = 1,
        /// <summary>Aday hamle kapsamı tükendi (boya adayları sınırlı). Kesin çözümsüzlük kanıtı değildir.</summary>
        CandidatesExhausted = 2,
        Cancelled = 3,
        /// <summary>Oyun bitmiş durumda.</summary>
        Terminal = 4,
    }

    /// <summary>
    /// Bir ipucu planı (01_IPUCU "Hedef veri modeli"). Durum anahtarı ve revizyon, planın hangi tahta için çıkarıldığını
    /// bağlar; tahta değişince plan geçersizdir ve yeniden çözülür.
    /// </summary>
    public sealed class HintPlan
    {
        public int PlanId;
        public string LevelId;
        public string ContentVersion;
        public int StateRevision;
        /// <summary>Planın çıkarıldığı durumun anahtarı (<see cref="LevelSolver.StateKey"/>).</summary>
        public string StateHash;
        public HintConfidence Confidence;
        public HintPlanStatus Status;
        /// <summary>Önerilen ilk komut (yalnız Verified ise anlamlı).</summary>
        public Command Command;
        /// <summary>Doğrulanmış tam yol (ilk komut dahil).</summary>
        public List<Command> Path;
        /// <summary>İlk komuttan sonraki durumun anahtarı: oyuncunun hamlesi buna ulaşırsa öneri uygulanmış sayılır
        /// (aynı grubun başka hücresine dokunuş dahil; sonucu farklıysa sayılmaz).</summary>
        public string ExpectedNextKey;
        public int Nodes;
        public long Millis;

        public bool IsVerified => Confidence == HintConfidence.VerifiedPath && Path != null && Path.Count > 0;
    }

    /// <summary>
    /// Güvenilir ipucu planlayıcısı. Saf C# (Unity'ye erişmez): oyun durumu kopyasında arka planda çalışabilir.
    /// Sıra: (1) verilen aday yollar (kayıtlı çözüm, önceki planın kalanı) mevcut durumda yeniden yürütülür;
    /// (2) bulunamazsa hedef ilerlemesine göre en-iyi-önce arama (düğüm + duvar süresi bütçeli).
    /// Bulunan her yol motorla baştan yeniden yürütülerek doğrulanır; doğrulanmayan yol önerilmez, tahmine geçilmez.
    /// "En iyi hamle" iddiası yoktur: en kısa, en ucuz ya da en çok yıldız garantisi değildir.
    /// </summary>
    public static class HintPlanner
    {
        public const int DefaultNodeBudget = 40000;
        public const long DefaultMillisBudget = 750;

        public static HintPlan Plan(LevelState state, int nodeBudget = DefaultNodeBudget, long millisBudget = DefaultMillisBudget,
            IEnumerable<IList<Command>> candidates = null, CancellationToken cancel = default)
        {
            var sw = Stopwatch.StartNew();
            var plan = new HintPlan { Confidence = HintConfidence.NoPlan };
            if (state == null || state.IsTerminal) { plan.Status = HintPlanStatus.Terminal; return plan; }
            plan.StateHash = LevelSolver.StateKey(state);

            if (candidates != null)
                foreach (var c in candidates)
                    if (c != null && c.Count > 0 && Verify(state, c))
                    {
                        Fill(plan, state, new List<Command>(c));
                        plan.Millis = sw.ElapsedMilliseconds;
                        return plan;
                    }

            var found = Search(state, nodeBudget, millisBudget, sw, cancel, out plan.Status, out plan.Nodes);
            if (found != null && Verify(state, found)) Fill(plan, state, found);
            else if (found != null) plan.Status = HintPlanStatus.CandidatesExhausted; // doğrulanamayan yol: önerme
            plan.Millis = sw.ElapsedMilliseconds;
            return plan;
        }

        private static void Fill(HintPlan plan, LevelState state, List<Command> path)
        {
            plan.Path = path;
            plan.Command = path[0];
            plan.Confidence = HintConfidence.VerifiedPath;
            plan.Status = HintPlanStatus.Verified;
            var next = state.Clone();
            RulesEngine.TryApply(next, path[0]);
            plan.ExpectedNextKey = LevelSolver.StateKey(next);
        }

        /// <summary>Yolu durumun kopyasında yürütür: her komut kabul edilmeli, ara durum bitmemeli, son durum kazanç olmalı.</summary>
        public static bool Verify(LevelState state, IList<Command> path)
        {
            if (state == null || state.IsTerminal || path == null || path.Count == 0) return false;
            var s = state.Clone();
            for (int i = 0; i < path.Count; i++)
            {
                if (s.IsTerminal) return false;
                if (!RulesEngine.TryApply(s, path[i]).Accepted) return false;
            }
            return s.Status == GameStatus.Won;
        }

        /// <summary>Önerilen ilk komut uygulandıktan sonra durum bu plana göre mi (eşdeğer dokunuş dahil)?</summary>
        public static bool IsFollowed(HintPlan plan, LevelState after) =>
            plan != null && plan.IsVerified && after != null && LevelSolver.StateKey(after) == plan.ExpectedNextKey;

        /// <summary>Plan uygulandıysa kalan yol (yeniden doğrulama adayı); değilse null.</summary>
        public static List<Command> Remaining(HintPlan plan) =>
            plan != null && plan.IsVerified && plan.Path.Count > 1 ? plan.Path.GetRange(1, plan.Path.Count - 1) : null;

        // ---- En-iyi-önce arama ----------------------------------------------------------------------------------

        private static List<Command> Search(LevelState start, int nodeBudget, long millisBudget, Stopwatch sw, CancellationToken cancel,
            out HintPlanStatus status, out int nodes)
        {
            nodes = 0;
            var visited = new HashSet<string> { LevelSolver.StateKey(start) };
            var heap = new MinHeap();
            long seq = 0;
            heap.Push(new Node { State = start.Clone(), Path = new List<Command>() }, Priority(start, 0), seq++);
            while (heap.Count > 0)
            {
                if (cancel.IsCancellationRequested) { status = HintPlanStatus.Cancelled; return null; }
                if (millisBudget > 0 && sw.ElapsedMilliseconds > millisBudget) { status = HintPlanStatus.BudgetExceeded; return null; }
                var node = heap.Pop();
                foreach (var cmd in LevelSolver.Moves(node.State))
                {
                    var next = node.State.Clone();
                    if (!RulesEngine.TryApply(next, cmd).Accepted) continue;
                    nodes++;
                    var path = new List<Command>(node.Path) { cmd };
                    if (next.Status == GameStatus.Won) { status = HintPlanStatus.Verified; return path; }
                    if (next.IsTerminal) continue; // kayba götüren dal (kristal, hamle bitişi) önerilmez
                    if (!visited.Add(LevelSolver.StateKey(next))) continue;
                    if (nodes >= nodeBudget) { status = HintPlanStatus.BudgetExceeded; return null; }
                    heap.Push(new Node { State = next, Path = path }, Priority(next, path.Count), seq++);
                }
            }
            status = HintPlanStatus.CandidatesExhausted;
            return null;
        }

        /// <summary>Küçük öncelik önce: hedef ilerlemesi yüksek, kullanılan adım az.</summary>
        private static double Priority(LevelState s, int depth)
        {
            double h = 0;
            foreach (var g in s.Goals)
            {
                int need = g.Amount <= 0 ? 1 : g.Amount;
                h += (g.Progress >= need ? 1.0 : g.Progress / (double)need) * 10.0;
                if (g.IsComplete) h += 4.0;
            }
            h += s.Tray.Count * 0.6 + (s.RollSlotFull ? 0.6 : 0);
            return depth * 1.0 - h;
        }

        private sealed class Node
        {
            public LevelState State;
            public List<Command> Path;
        }

        /// <summary>İkili yığın (Unity'nin .NET profilinde PriorityQueue yok). Eşit öncelikte ekleme sırası (deterministik).</summary>
        private sealed class MinHeap
        {
            private readonly List<(double p, long s, Node n)> _a = new List<(double, long, Node)>();
            public int Count => _a.Count;

            private static bool Less((double p, long s, Node n) x, (double p, long s, Node n) y) => x.p < y.p || (x.p == y.p && x.s < y.s);

            public void Push(Node n, double p, long s)
            {
                _a.Add((p, s, n));
                int i = _a.Count - 1;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (!Less(_a[i], _a[parent])) break;
                    (_a[i], _a[parent]) = (_a[parent], _a[i]);
                    i = parent;
                }
            }

            public Node Pop()
            {
                var top = _a[0].n;
                int last = _a.Count - 1;
                _a[0] = _a[last];
                _a.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = 2 * i + 1, r = l + 1, m = i;
                    if (l < _a.Count && Less(_a[l], _a[m])) m = l;
                    if (r < _a.Count && Less(_a[r], _a[m])) m = r;
                    if (m == i) break;
                    (_a[i], _a[m]) = (_a[m], _a[i]);
                    i = m;
                }
                return top;
            }
        }
    }
}
