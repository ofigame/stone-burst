using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>v12 deneme seti (Resources/Trial, LevelLab trial) ve yeni kurallar (kristal, zincir hedefi).</summary>
    public class TrialSetTests
    {
        private static string Dir => Path.Combine(Path.GetDirectoryName(ProductionLevelTests.LevelsDir()), "Trial");

        private static LevelData Load(int n) => LevelParser.Parse(File.ReadAllText(Path.Combine(Dir, $"trial_{n:00}.json")), $"trial_{n:00}");

        [Test]
        public void TenTrials_Parse_Validate_RecordedSolutionWins_WithinLimit()
        {
            var types = new System.Collections.Generic.HashSet<string>();
            for (int n = 1; n <= 10; n++)
            {
                var d = Load(n);
                Assert.IsNotEmpty(d.TrialType, d.Id);
                Assert.IsTrue(types.Add(d.TrialType), d.Id + " tür tekrar etmez");
                CollectionAssert.IsEmpty(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)), d.Id);
                var s = LevelLoader.ToState(d);
                foreach (var step in d.Solution)
                {
                    var r = RulesEngine.TryApply(s, LevelLoader.ToCommand(step));
                    Assert.IsTrue(r.Accepted, d.Id);
                }
                Assert.AreEqual(GameStatus.Won, s.Status, d.Id + " kayıtlı çözüm kazanır");
                if (d.Rules.Contains("exact"))
                {
                    int taps = d.Solution.Count(x => x.Type != "use");
                    Assert.AreEqual(d.MoveLimit, taps, d.Id + " kesin hamle: sınır = en kısa çözüm");
                }
            }
            Assert.IsTrue(types.Contains("boss"));
        }

        [Test]
        public void Trials_DoNotTouchProductionLevels()
        {
            // Deneme dosyaları ana bölüm klasöründe değil (özet testi ve dizin etkilenmez).
            Assert.IsFalse(Directory.GetFiles(ProductionLevelTests.LevelsDir(), "trial_*.json").Any());
        }

        private static LevelState Board(string[] rows, params (int x, int y)[] crystals)
        {
            // Her satır: hücre başına tek katman (c/b/y/p).
            int h = rows.Length, w = rows[0].Length;
            var b = new BoardState(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = b[x, y];
                    c.Active = true;
                    c.Layers.Add(rows[y][x] == 'c' ? LayerColor.Coral : rows[y][x] == 'b' ? LayerColor.Blue : rows[y][x] == 'y' ? LayerColor.Yellow : LayerColor.Purple);
                }
            foreach (var (x, y) in crystals) b[x, y].Protected = true;
            return new LevelState { Board = b, MovesLeft = 5 };
        }

        [Test]
        public void Protected_PeeledCrystal_LosesEvenIfGoalDone()
        {
            var s = Board(new[] { "ccc", "byp", "pyb" }, (2, 0));
            s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Coral, Amount = 3 });
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(s.ProtectedBroken);
            Assert.AreEqual(GameStatus.LostProtected, s.Status, "kristal kuralı kazanmadan önce");
            var clone = s.Clone();
            Assert.IsTrue(clone.ProtectedBroken && clone.Board[2, 0].Protected, "kopya kuralı taşır");
        }

        [Test]
        public void ChainGoal_CountsOnlyChainWaves()
        {
            // Üst sıra mercan (dokunulur); altında mavi satır açılınca zincirle soyulur.
            var b = new BoardState(3, 2);
            for (int x = 0; x < 3; x++)
            {
                var top = b[x, 0]; top.Active = true; top.Layers.Add(LayerColor.Blue); top.Layers.Add(LayerColor.Coral);
                var bot = b[x, 1]; bot.Active = true; bot.Layers.Add(LayerColor.Purple);
            }
            var s = new LevelState { Board = b, MovesLeft = 3 };
            s.Goals.Add(new GoalState { Type = GoalType.CollectChain, Color = LayerColor.Blue, Amount = 3 });
            s.Goals.Add(new GoalState { Type = GoalType.CollectChain, Color = LayerColor.Coral, Amount = 3 });
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(0, s.Goals[1].Progress, "dokunulan mercan zincir sayılmaz");
            Assert.AreEqual(3, s.Goals[0].Progress, "zincirle açılan mavi sayılır");
        }
    }
}
