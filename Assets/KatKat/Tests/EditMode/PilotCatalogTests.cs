using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>
    /// vr1 P2/P3 pilot kataloğu (Resources/Pilot): her bölüm statik doğrulamadan geçer, kayıtlı çözümü motorla kazanır ve
    /// türünün yeni kuralını gerçekten kullanır (aşama ilerler, mühür açılır, iki alan vardır). Ana katalog ayrı testlerle.
    /// </summary>
    public class PilotCatalogTests
    {
        private static string Dir() => Path.Combine(Path.GetDirectoryName(ProductionLevelTests.LevelsDir()), "Pilot");

        private static string[] Files() => Directory.Exists(Dir()) ? Directory.GetFiles(Dir(), "pilot_*.json").OrderBy(f => f).ToArray() : new string[0];

        [Test]
        public void PilotsExist()
        {
            Assert.GreaterOrEqual(Files().Length, 11, "en az 11 pilot bölüm");
        }

        [Test]
        public void EveryPilot_Validates_AndSavedSolutionWins_UsingItsRule()
        {
            foreach (var f in Files())
            {
                var d = LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f));
                var errors = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
                Assert.IsEmpty(errors, d.Id + ": " + string.Join(" | ", errors));
                StringAssert.StartsWith("pilot_", d.TrialType, d.Id);
                var s = LevelLoader.ToState(d);
                bool stageAdvanced = false, sealOpened = false;
                foreach (var step in d.Solution)
                {
                    var r = RulesEngine.TryApply(s, LevelLoader.ToCommand(step));
                    Assert.IsTrue(r.Accepted, d.Id + " " + step.Type);
                    stageAdvanced |= r.Waves.Any(w => w.StageAdvanced);
                    sealOpened |= r.Waves.Any(w => w.SealOpened);
                }
                Assert.AreEqual(GameStatus.Won, s.Status, d.Id + ": kayıtlı çözüm kazanmalı");
                if (d.TrialType == "pilot_stages") Assert.IsTrue(stageAdvanced && s.StageCount >= 2, d.Id + ": aşama ilerlemeli");
                if (d.TrialType == "pilot_seal") Assert.IsTrue(sealOpened, d.Id + ": mühür açılmalı");
                if (d.TrialType == "pilot_twin") Assert.IsTrue(RulesEngine.HasAreas(s.Board), d.Id + ": iki alan");
            }
        }
    }
}
