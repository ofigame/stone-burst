using System.Collections.Generic;
using System.IO;
using KatKat.Core;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Konsept pilotu (Resources/Concept, LevelLab concept): 10 bölüm ve tehdit kuralları.</summary>
    public class ConceptTests
    {
        private static string Dir => Path.Combine(Path.GetDirectoryName(ProductionLevelTests.LevelsDir()), "Concept");

        private static LevelData Load(int n) => LevelParser.Parse(File.ReadAllText(Path.Combine(Dir, $"concept_{n:00}.json")), $"concept_{n:00}");

        [Test]
        public void TenConcepts_DistinctKinds_RecordedSolutionWins()
        {
            var kinds = new HashSet<string>();
            for (int n = 1; n <= 10; n++)
            {
                var d = Load(n);
                Assert.IsNotNull(d.Hazard, d.Id);
                Assert.IsTrue(kinds.Add(d.Hazard.Kind), d.Id + " konsept tekrar etmez");
                CollectionAssert.IsEmpty(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)), d.Id + " oyunun açılış doğrulaması");
                var s = LevelLoader.ToState(d);
                Assert.IsNotNull(s.Hazard, d.Id);
                foreach (var step in d.Solution)
                {
                    var r = RulesEngine.TryApply(s, LevelLoader.ToCommand(step));
                    Assert.IsTrue(r.Accepted, d.Id);
                }
                Assert.AreEqual(GameStatus.Won, s.Status, d.Id + " kayıtlı çözüm kazanır");
            }
            Assert.AreEqual(10, kinds.Count);
        }

        [Test]
        public void Concepts_NotInProductionFolder_AndProductionHasNoHazard()
        {
            Assert.IsEmpty(Directory.GetFiles(ProductionLevelTests.LevelsDir(), "concept_*.json"));
            var any = Directory.GetFiles(ProductionLevelTests.LevelsDir(), "*.json");
            Assert.IsNotEmpty(any);
            var d = LevelParser.Parse(File.ReadAllText(any[0]), "ilk");
            Assert.IsNull(d.Hazard);
        }

        /// <summary>Tek renk satırlar: 'c' mercan, 'b' turkuaz, 'y' inci, 'p' mor; rakam = katman sayısı (aynı renk).</summary>
        private static LevelState Board(params string[] rows)
        {
            int h = rows.Length, w = rows[0].Length / 2;
            var b = new BoardState(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var cell = b[x, y];
                    char c = rows[y][x * 2]; int n = rows[y][x * 2 + 1] - '0';
                    cell.Active = c != '#';
                    if (!cell.Active) continue;
                    var col = c == 'c' ? LayerColor.Coral : c == 'b' ? LayerColor.Blue : c == 'y' ? LayerColor.Yellow : LayerColor.Purple;
                    for (int k = 0; k < n; k++) cell.Layers.Add(col);
                }
            return new LevelState { Board = b, MovesLeft = 20, Status = GameStatus.Idle };
        }

        [Test]
        public void Rot_BottomRowCollapses_AndEmptyingDelays()
        {
            var s = Board("b1b1b1y1", "c1c1c1y1", "p1p1y1y1");
            s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Blue, Amount = 3 });
            s.Hazard = new HazardState { Kind = HazardKind.Rot, Every = 2, Counter = 2, Row = 2 };
            RulesEngine.TryApply(s, Command.Tap(0, 1)); // mercan sırası: alt sıra boşalmadı → sayaç 1
            Assert.AreEqual(1, s.Hazard.Counter);
            RulesEngine.TryApply(s, Command.Tap(3, 0)); // inci: (3,0),(3,1),(2,2),(3,2) → alt sırada 2 yığın boşaldı → +2, −1
            Assert.AreEqual(1, s.Hazard.Counter, "boşalan iki alt yığın çöküşü erteler (ertelemesiz bu hamlede çökerdi)");
            Assert.AreEqual(2, s.Hazard.Row);
        }

        [Test]
        public void Rot_Collapse_RemovesCells_AndUnreachableLoses()
        {
            var s = Board("y1y1y1", "c1c1c1", "b1b1b1");
            s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Blue, Amount = 3 });
            s.Hazard = new HazardState { Kind = HazardKind.Rot, Every = 1, Counter = 1, Row = 2 };
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsFalse(s.Board[0, 2].Active, "alt sıra çöktü");
            Assert.AreEqual(3, s.Hazard.Fallen);
            Assert.AreEqual(HazardNote.RotCrumble, r.HazardNote);
            Assert.AreEqual(GameStatus.LostHazard, s.Status);
            Assert.AreEqual(HazardLoss.Unreachable, s.Hazard.Loss);
        }

        [Test]
        public void Fuse_DefusedWhenEmptied_ExplodesOtherwise()
        {
            var s = Board("c1c1c1", "y2y2b2", "p1p1p1");
            s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Yellow, Amount = 4 });
            var fuse = new int[9];
            fuse[s.Board.Index(0, 0)] = 2;
            fuse[s.Board.Index(2, 1)] = 2;
            s.Hazard = new HazardState { Kind = HazardKind.Fuse, Fuse = fuse };
            RulesEngine.TryApply(s, Command.Tap(0, 0)); // mercan satırı boşaldı → (0,0) söndü; (2,1) 1'e indi
            Assert.AreEqual(0, s.Hazard.Fuse[s.Board.Index(0, 0)]);
            Assert.AreEqual(1, s.Hazard.Fuse[s.Board.Index(2, 1)]);
            RulesEngine.TryApply(s, Command.Tap(0, 2));
            Assert.AreEqual(GameStatus.LostHazard, s.Status);
            Assert.AreEqual(HazardLoss.Fuse, s.Hazard.Loss);
        }

        [Test]
        public void Cycle_OnlyActiveColourCounts()
        {
            var s = Board("c1c1c1", "b2b2b2", "y1y1y1");
            s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Coral, Amount = 3 });
            s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Blue, Amount = 3 });
            s.Hazard = new HazardState { Kind = HazardKind.Cycle, Every = 5, Counter = 5, Colors = new[] { LayerColor.Coral, LayerColor.Blue } };
            RulesEngine.TryApply(s, Command.Tap(0, 1)); // turkuaz etkin değil
            Assert.AreEqual(0, s.Goals[1].Progress);
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(3, s.Goals[0].Progress);
        }

        [Test]
        public void Raven_StealsAndWaitsAtEmptyStack()
        {
            var s = Board("c3c3c3b1", "y1y1y1b1", "p1p1p1b1");
            s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Coral, Amount = 9 });
            var b = s.Board;
            s.Hazard = new HazardState { Kind = HazardKind.Raven, Path = new[] { b.Index(0, 1), b.Index(1, 1), b.Index(2, 1), b.Index(3, 1) } };
            RulesEngine.TryApply(s, Command.Tap(0, 2)); // mor satır; kuzgun (1,1)'e kondu ve inciyi çaldı
            Assert.AreEqual(1, s.Hazard.Index);
            Assert.AreEqual(0, b[1, 1].Layers.Count);
            Assert.AreEqual(1, s.Hazard.Stolen);
            // (2,1) dolu: ilerler; (3,1) yuva → kayıp
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, s.Hazard.Index);
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.LostHazard, s.Status);
            Assert.AreEqual(HazardLoss.Raven, s.Hazard.Loss);
        }

        [Test]
        public void Moss_SpreadsAndIsClearedByNeighbourPeel()
        {
            var s = Board("c1c1c1y2", "b2b2b2y2", "p1p1p1y2");
            s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Purple, Amount = 3 });
            s.Board[3, 1].Moss = true;
            s.Hazard = new HazardState { Kind = HazardKind.Moss, Every = 1, Counter = 1, Max = 99 };
            Assert.IsFalse(s.Board[3, 1].IsSelectable, "yosun örtüdür");
            RulesEngine.TryApply(s, Command.Tap(0, 1)); // turkuaz satır (2,1) yosunun komşusu → temizlenir
            Assert.IsFalse(s.Board[3, 1].Moss);
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(0, HazardRules.MossCount(s.Board));
        }

        [Test]
        public void Balloon_RisesPopsAndEscapes()
        {
            var s = Board("c1c1c1", "y1b1b1", "p1p1p1");
            s.Goals.Add(new GoalState { Type = GoalType.CollectBalloons, Amount = 1 });
            var bal = new bool[9];
            bal[s.Board.Index(0, 2)] = true;
            s.Hazard = new HazardState { Kind = HazardKind.Balloon, Every = 1, Counter = 1, Balloon = bal };
            RulesEngine.TryApply(s, Command.Tap(0, 0)); // mercan satırı boşaldı; balon (0,1)'e yükseldi
            Assert.IsTrue(s.Hazard.Balloon[s.Board.Index(0, 1)]);
            RulesEngine.TryApply(s, Command.Tap(0, 2)); // balon boş (0,0)'a girer → patlar
            Assert.AreEqual(GameStatus.Won, s.Status);
            Assert.AreEqual(1, s.Hazard.Popped);
        }

        [Test]
        public void Fire_BigGroupFeeds_SmallGroupBurns()
        {
            // v14 (kullanıcı 2026-11-16: zincir tamamen kaldırılsın): ateşi 5+ taşlık dokunuş grubu besler, küçük grup söndürür.
            var s = Board("c1c1c1c1c1", "b1b1b1y1p1", "y1y1y1p1y1");
            s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Purple, Amount = 2 });
            s.Hazard = new HazardState { Kind = HazardKind.Fire, Max = 2, Value = 1 };
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0)); // 5 mercan → ateş dolar
            Assert.AreEqual(2, s.Hazard.Value);
            Assert.AreEqual(HazardNote.FireFed, r.HazardNote);
            RulesEngine.TryApply(s, Command.Tap(0, 1)); // 3 turkuaz → −1
            Assert.AreEqual(1, s.Hazard.Value);
            RulesEngine.TryApply(s, Command.Tap(0, 2)); // 3 inci → söner
            Assert.AreEqual(GameStatus.LostHazard, s.Status);
            Assert.AreEqual(HazardLoss.Fire, s.Hazard.Loss);
        }

        [Test]
        public void TrayUse_DoesNotAdvanceHazard_AndUndoRestores()
        {
            var s = Board("c1c1c1", "b1b1b1", "y1y1y1");
            s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Yellow, Amount = 3 });
            s.Hazard = new HazardState { Kind = HazardKind.Rival, Max = 9 };
            s.Tray.Add(SpecialKind.RocketRow);
            var session = new GameSession(s);
            session.Submit(Command.UseSpecial(0, 0, 0));
            Assert.AreEqual(0, session.State.Hazard.Value, "tepsi hamle harcamaz, rakip ilerlemez");
            session.Submit(Command.Tap(0, 1));
            Assert.AreEqual(1, session.State.Hazard.Value);
            session.Undo();
            Assert.AreEqual(0, session.State.Hazard.Value);
        }
    }
}
