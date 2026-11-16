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

        /// <summary>
        /// v14 (kullanıcı 2026-11-16: "ortası delik tahtaları kaldıralım", docs/v14/PLAN.md kural 2): hiçbir bölümde iç boşluk yok
        /// (tahta kenarına değmeyen boş bölge); açılışta boş etkin hücre yok; zincire bağlı veri yok; yıldız eşikleri yazılı.
        /// </summary>
        [Test]
        public void EveryLevel_HasNoInteriorHole_NoChainData_HasStarThresholds()
        {
            foreach (var f in Files())
            {
                var d = LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f));
                var s = LevelLoader.ToState(d);
                Assert.IsFalse(BoardHoles.Largest(s.Board).Has, $"{d.Id}: iç boşluk");
                Assert.IsFalse(d.Cells.Exists(c => c.Active && c.Layers.Count == 0), $"{d.Id}: açılışta boş hücre");
                Assert.IsFalse(d.Rules.Contains("chainOnly") || d.TrialType == "chain" || d.TrialType == "aha" || d.Capabilities.Contains("seal"), $"{d.Id}: zincir verisi");
                Assert.IsFalse(d.Goals.Exists(g => g.Type == "collectChain"), $"{d.Id}: zincir hedefi");
                Assert.IsNotNull(d.StarThresholds, $"{d.Id}: yıldız eşikleri");
            }
        }
    }
}
