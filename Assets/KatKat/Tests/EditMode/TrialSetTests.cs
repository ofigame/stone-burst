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
        public void ChainRules_AreRejected_StaleDataCaught()
        {
            // v14 (kullanıcı 2026-11-16: zincir tamamen kaldırılsın): collectChain, chainOnly ve zincir türleri okunur ama reddedilir.
            const string json = "{\"schemaVersion\":2,\"id\":\"chain_old\",\"width\":5,\"height\":5,\"moveLimit\":5,\"trialType\":\"chain\",\"rules\":[\"chainOnly\"],"
                + "\"goals\":[{\"type\":\"collectChain\",\"color\":\"blue\",\"amount\":3}],\"cells\":[]}";
            var d = LevelParser.Parse(json, "chain_old");
            Assert.AreEqual("collectChain", d.Goals[0].Type);
            var errors = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
            Assert.IsTrue(errors.Any(e => e.Contains("collectChain")), string.Join(" | ", errors));
            Assert.IsTrue(errors.Any(e => e.Contains("chainOnly")), string.Join(" | ", errors));
            Assert.IsTrue(errors.Any(e => e.Contains("'chain'")), string.Join(" | ", errors));
            var s = LevelLoader.ToState(d); // eski veri çökmez: düz renk hedefi gibi okunur
            Assert.AreEqual(GoalType.CollectColor, s.Goals[0].Type);
        }
    }
}
