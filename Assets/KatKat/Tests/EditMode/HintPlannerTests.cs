using System.Collections.Generic;
using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>
    /// vr1 paketi 01_IPUCU / 06_TEST_KABUL (H01–H05): güvenilir ipucu planı. Öneri yalnız motorla yeniden yürütülüp
    /// kazanca ulaştığı doğrulanan yolun ilk komutudur; kayba götüren ya da doğrulanamayan hamle önerilmez.
    /// </summary>
    public class HintPlannerTests
    {
        private static LevelData Level(int n)
        {
            var f = Path.Combine(ProductionLevelTests.LevelsDir(), $"level_{n:000}.json");
            return LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f));
        }

        private static List<Command> Saved(LevelData d) => d.Solution.Select(LevelLoader.ToCommand).ToList();

        private static LevelState After(LevelData d, int steps)
        {
            var s = LevelLoader.ToState(d);
            var sol = Saved(d);
            for (int i = 0; i < steps; i++) Assert.IsTrue(RulesEngine.TryApply(s, sol[i]).Accepted);
            return s;
        }

        /// <summary>Kayıtlı çözümü doğrudan aday olarak verince plan onu doğrular (ilk komut bütün türlerde olabilir).</summary>
        [Test]
        public void SavedSolution_IsVerifiedCandidate_ForEveryCommandType()
        {
            var seen = new HashSet<CommandType>();
            foreach (int n in new[] { 1, 21, 41, 61, 121, 201, 301, 401, 501, 601, 701, 801, 901, 1000 })
            {
                var d = Level(n);
                var sol = Saved(d);
                for (int k = 0; k < sol.Count; k++)
                {
                    var s = After(d, k);
                    if (s.IsTerminal) break;
                    var plan = HintPlanner.Plan(s, candidates: new[] { sol.GetRange(k, sol.Count - k) });
                    Assert.IsTrue(plan.IsVerified, $"{d.Id} adım {k}");
                    Assert.AreEqual(sol[k].ToString(), plan.Command.ToString(), $"{d.Id} adım {k}");
                    seen.Add(plan.Command.Type);
                }
            }
            Assert.Contains(CommandType.Tap, seen.ToList());
        }

        /// <summary>H01/H02: ilk adımı tepsi, döndürme, boya ya da rulo olan kayıtlı çözüm adımları elenmez.</summary>
        [Test]
        public void NonTapFirstSteps_AreRecommended()
        {
            var found = new Dictionary<CommandType, string>();
            for (int n = 1; n <= 1000 && found.Count < 4; n += 3)
            {
                var d = Level(n);
                var sol = Saved(d);
                for (int k = 0; k < sol.Count; k++)
                {
                    if (sol[k].Type == CommandType.Tap || found.ContainsKey(sol[k].Type)) continue;
                    var s = After(d, k);
                    var plan = HintPlanner.Plan(s, candidates: new[] { sol.GetRange(k, sol.Count - k) });
                    Assert.IsTrue(plan.IsVerified, $"{d.Id} adım {k}");
                    Assert.AreEqual(sol[k].Type, plan.Command.Type, $"{d.Id} adım {k}");
                    found[sol[k].Type] = d.Id;
                    break;
                }
            }
            Assert.IsTrue(found.ContainsKey(CommandType.UseSpecial), "tepsi ile başlayan çözüm adımı bulunmalı");
        }

        /// <summary>H02/H04: önerilen yolun bütün adımları kabul edilir, ara durum bitmez, son durum kazançtır.</summary>
        [Test]
        public void SearchPlan_IsReplayVerified_AndFirstMoveDoesNotLose()
        {
            int verified = 0;
            foreach (int n in new[] { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89 })
            {
                var d = Level(n);
                var s = LevelLoader.ToState(d);
                var plan = HintPlanner.Plan(s, millisBudget: 0); // yalnız düğüm bütçesi (deterministik)
                if (!plan.IsVerified) continue;
                verified++;
                Assert.IsTrue(HintPlanner.Verify(s, plan.Path), d.Id);
                var probe = s.Clone();
                Assert.IsTrue(RulesEngine.TryApply(probe, plan.Command).Accepted, d.Id);
                Assert.IsTrue(probe.Status == GameStatus.Won || !probe.IsTerminal, $"{d.Id}: ilk hamle kaybettirmemeli");
                Assert.IsTrue(HintPlanner.IsFollowed(plan, probe), d.Id);
            }
            Assert.Greater(verified, 5, "kolay bölümlerin çoğu bütçe içinde çözülmeli");
        }

        /// <summary>H04: bütçe dolunca sonuç "çözümsüz" değil BudgetExceeded; öneri yok.</summary>
        [Test]
        public void TinyBudget_GivesNoPlan_NotUnsolvable()
        {
            var d = Level(900);
            var plan = HintPlanner.Plan(LevelLoader.ToState(d), nodeBudget: 3, millisBudget: 0);
            Assert.IsFalse(plan.IsVerified);
            Assert.AreEqual(HintConfidence.NoPlan, plan.Confidence);
            Assert.AreEqual(HintPlanStatus.BudgetExceeded, plan.Status);
        }

        /// <summary>H03: kristali kıran (ya da hamleyi tüketip kaybettiren) yol doğrulamadan geçmez.</summary>
        [Test]
        public void LosingPath_FailsVerification()
        {
            for (int n = 1; n <= 1000; n++)
            {
                var d = Level(n);
                var s = LevelLoader.ToState(d);
                foreach (var c in LevelSolver.Moves(s))
                {
                    var probe = s.Clone();
                    if (!RulesEngine.TryApply(probe, c).Accepted || !probe.IsTerminal || probe.Status == GameStatus.Won) continue;
                    Assert.IsFalse(HintPlanner.Verify(s, new[] { c }), $"{d.Id} {c}: kayba götüren hamle doğrulanmamalı");
                    return;
                }
            }
            Assert.Inconclusive("ilk hamlede kaybettiren bölüm bulunamadı");
        }

        /// <summary>H05: hamle 0 + etkili tepsi taşı: plan tepsi komutudur (dokunuş önerilmez).</summary>
        [Test]
        public void ZeroMoves_WithTray_RecommendsTrayOrNothing()
        {
            for (int n = 1; n <= 1000; n += 7)
            {
                var d = Level(n);
                var sol = Saved(d);
                var s = LevelLoader.ToState(d);
                for (int k = 0; k < sol.Count && !s.IsTerminal; k++)
                {
                    RulesEngine.TryApply(s, sol[k]);
                    if (s.IsTerminal || s.Tray.Count == 0) continue;
                    var zero = s.Clone();
                    zero.MovesLeft = 0;
                    var plan = HintPlanner.Plan(zero, millisBudget: 0);
                    if (plan.IsVerified) Assert.AreEqual(CommandType.UseSpecial, plan.Command.Type, d.Id);
                    else Assert.AreEqual(HintConfidence.NoPlan, plan.Confidence);
                    return;
                }
            }
            Assert.Inconclusive("tepside taş olan durum bulunamadı");
        }

        /// <summary>
        /// Ölçüm (elle çalıştırılır): oyun ortasındaki durumlarda eski ilk-hamle çözücüsüyle (yalnız dokunuş, BFS 40k)
        /// yeni planlayıcının doğrulanmış plan bulma oranı ve süresi. Masaüstü ölçümüdür, cihaz süresi değildir.
        /// </summary>
        [Test, Explicit]
        public void Measure_MidGameCoverage()
        {
            int total = 0, oldOk = 0, newOk = 0, newSearchOk = 0;
            long maxMs = 0, sumMs = 0;
            for (int n = 1; n <= 1000; n += 10)
            {
                var d = Level(n);
                var sol = Saved(d);
                int k = sol.Count / 2;
                var s = After(d, k);
                // Kayıtlı çözümden sap: ilk farklı kabul edilen ve bitirmeyen dokunuş.
                foreach (var c in LevelSolver.Moves(s))
                {
                    if (c.ToString() == sol[k].ToString()) continue;
                    var p = s.Clone();
                    if (RulesEngine.TryApply(p, c).Accepted && !p.IsTerminal) { s = p; break; }
                }
                if (s.IsTerminal) continue;
                total++;
                var old = LevelSolver.Solve(s, 40000);
                if (old.Outcome == SolveOutcome.Solved && old.Solution.Count > 0 && old.Solution[0].Type == CommandType.Tap) oldOk++;
                var plan = HintPlanner.Plan(s);
                if (plan.IsVerified) newOk++;
                if (plan.IsVerified) newSearchOk++;
                maxMs = System.Math.Max(maxMs, plan.Millis);
                sumMs += plan.Millis;
                TestContext.Progress.WriteLine($"{d.Id} k={k}: eski={old.Outcome}/{old.NodesExpanded} yeni={plan.Status}/{plan.Nodes} {plan.Millis}ms");
            }
            TestContext.Progress.WriteLine($"TOPLAM {total}: eski {oldOk}, yeni {newOk}; ort {sumMs / System.Math.Max(1, total)}ms, en çok {maxMs}ms");
        }
    }
}
