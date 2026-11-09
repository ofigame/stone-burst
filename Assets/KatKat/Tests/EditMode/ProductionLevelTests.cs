using System.IO;
using System.Linq;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Yayın seviyeleri: statik doğrulama + kayıtlı çözümün gerçek kural motoruyla tekrarı.</summary>
    public class ProductionLevelTests
    {
        public static string LevelsDir()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), System.AppContext.BaseDirectory })
            {
                var d = new DirectoryInfo(start);
                while (d != null)
                {
                    var p = Path.Combine(d.FullName, "Assets", "KatKat", "Resources", "Levels");
                    if (Directory.Exists(p)) return p;
                    d = d.Parent;
                }
            }
            Assert.Fail("Resources/Levels bulunamadı");
            return null;
        }

        // Sayısal sıra (level_1000, level_101'den sonra gelir; oyun kataloğuyla aynı).
        private static string[] Files() => Directory.GetFiles(LevelsDir(), "level_*.json")
            .OrderBy(f => LevelValidator.LevelNumber(Path.GetFileNameWithoutExtension(f))).ThenBy(f => f, System.StringComparer.Ordinal).ToArray();

        [Test]
        public void LevelsExist_WithSequentialUniqueIds()
        {
            var files = Files();
            Assert.GreaterOrEqual(files.Length, 5);
            for (int i = 0; i < files.Length; i++)
            {
                var d = LevelParser.Parse(File.ReadAllText(files[i]), Path.GetFileName(files[i]));
                Assert.AreEqual($"level_{i + 1:000}", d.Id);
                Assert.AreEqual(d.Id + ".json", Path.GetFileName(files[i]));
            }
        }

        [Test]
        public void EveryLevel_PassesStaticValidation_AndRecordedSolutionWins()
        {
            foreach (var f in Files())
            {
                var e = ValidationReport.Check(Path.GetFileName(f), File.ReadAllText(f));
                Assert.IsEmpty(e.StaticErrors, $"{e.Source}: {string.Join(" | ", e.StaticErrors)}");
                Assert.IsTrue(e.Replay.Success, $"{e.Source}: {e.Replay.Error}");
                Assert.LessOrEqual(e.Replay.Steps, e.Replay.MoveLimit);
            }
        }
    }
}
