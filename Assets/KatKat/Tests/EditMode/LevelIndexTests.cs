using System;
using System.IO;
using System.Linq;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>
    /// v10 P1.6: bölüm dizin dosyası (Resources/LevelIndex/levels_index.json) bölüm dosyalarıyla birebir aynı olmalı.
    /// Bölüm eklenir ya da silinirse dizin <c>tools/LevelLab index</c> ile yeniden üretilmeli; yoksa bu test düşer.
    /// </summary>
    public class LevelIndexTests
    {
        private static string IndexPath()
        {
            var levels = ProductionLevelTests.LevelsDir(); // .../Resources/Levels
            return Path.Combine(Directory.GetParent(levels).FullName, "LevelIndex", "levels_index.json");
        }

        [Test]
        public void IndexFile_MatchesLevelFilesExactly()
        {
            Assert.IsTrue(File.Exists(IndexPath()), "dizin yok: tools/LevelLab index");
            var text = File.ReadAllText(IndexPath());
            var names = Directory.GetFiles(ProductionLevelTests.LevelsDir(), "level_*.json").Select(Path.GetFileNameWithoutExtension).ToList();
            Assert.AreEqual(LevelIndex.Write(names), text, "dizin güncel değil: tools/LevelLab index ile yeniden üret");
            var parsed = LevelIndex.Parse(text);
            Assert.AreEqual(1000, parsed.Length);
            Assert.AreEqual("level_001", parsed[0]);
            Assert.AreEqual("level_1000", parsed[999], "sayısal sıra");
        }

        [Test]
        public void Write_SortsNumerically_AndRoundTrips()
        {
            var text = LevelIndex.Write(new[] { "level_1000", "level_101", "level_002", "level_10000" });
            CollectionAssert.AreEqual(new[] { "level_002", "level_101", "level_1000", "level_10000" }, LevelIndex.Parse(text));
        }

        [Test]
        public void Parse_RejectsBrokenIndexes()
        {
            Assert.Throws<FormatException>(() => LevelIndex.Parse(""));
            Assert.Throws<FormatException>(() => LevelIndex.Parse("[]"));
            Assert.Throws<FormatException>(() => LevelIndex.Parse("{\"version\":2,\"count\":1,\"levels\":[\"level_001\"]}"), "bilinmeyen sürüm");
            Assert.Throws<FormatException>(() => LevelIndex.Parse("{\"version\":1,\"count\":2,\"levels\":[\"level_001\"]}"), "sayı tutmuyor");
            Assert.Throws<FormatException>(() => LevelIndex.Parse("{\"version\":1,\"count\":2,\"levels\":[\"level_001\",\"level_001\"]}"), "yinelenen");
            Assert.Throws<FormatException>(() => LevelIndex.Parse("{\"version\":1,\"count\":2,\"levels\":[\"level_002\",\"level_001\"]}"), "sırasız");
            Assert.Throws<FormatException>(() => LevelIndex.Parse("{\"version\":1,\"count\":1,\"levels\":[\"daily_001\"]}"), "bölüm adı değil");
        }
    }
}
