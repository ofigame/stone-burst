using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>v10 P1.5: dünya katmanı yalnız gruplama; 100'lük bölgeler aynen kalır.</summary>
    public class WorldTests
    {
        private static WorldCatalog Shipped()
        {
            var p = Path.Combine(Directory.GetParent(ProductionLevelTests.LevelsDir()).FullName, "Worlds", "worlds.json");
            Assert.IsTrue(File.Exists(p), "worlds.json yok: " + p);
            return WorldCatalog.Parse(File.ReadAllText(p));
        }

        [Test]
        public void Shipped_1000Levels_TwoWorldsOfFiveRegions()
        {
            var c = Shipped();
            Assert.AreEqual(500, c.WorldSize);
            var all = c.All(1000);
            Assert.AreEqual(2, all.Count);
            Assert.AreEqual("world_01", all[0].WorldId);
            Assert.AreEqual((0, 499), (all[0].FirstIndex, all[0].LastIndex));
            Assert.AreEqual((0, 4), (all[0].FirstRegion(WorldCatalog.RegionSize), all[0].LastRegion(WorldCatalog.RegionSize)));
            Assert.AreEqual("world_02", all[1].WorldId);
            Assert.AreEqual((500, 999), (all[1].FirstIndex, all[1].LastIndex));
            Assert.AreEqual((5, 9), (all[1].FirstRegion(WorldCatalog.RegionSize), all[1].LastRegion(WorldCatalog.RegionSize)));
            Assert.IsTrue(all.All(w => !w.Derived && w.NameKey == ""), "adlar onaylanmadı: boş");
        }

        [Test]
        public void WorldOf_Edges()
        {
            var c = Shipped();
            Assert.AreEqual(1, c.WorldOf(0, 1000).Number);
            Assert.AreEqual(1, c.WorldOf(499, 1000).Number, "bölüm 500 dünya 1'in sonu");
            Assert.AreEqual(2, c.WorldOf(500, 1000).Number, "bölüm 501 dünya 2'nin başı");
            Assert.AreEqual(2, c.WorldOf(999, 1000).Number);
            Assert.Throws<ArgumentOutOfRangeException>(() => c.WorldOf(1000, 1000));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.WorldOf(-1, 1000));
        }

        [Test]
        public void EveryLevelAndRegion_InExactlyOneWorld_UpTo10k()
        {
            var c = Shipped();
            foreach (int total in new[] { 1, 99, 500, 501, 1000, 1234, 10000 })
            {
                var all = c.All(total);
                Assert.AreEqual(all.Sum(w => w.LevelCount), total, "toplam " + total);
                for (int i = 1; i < all.Count; i++) Assert.AreEqual(all[i - 1].LastIndex + 1, all[i].FirstIndex, "boşluksuz ve çakışmasız");
                for (int r = 0; r * WorldCatalog.RegionSize < total; r++)
                    Assert.AreEqual(1, all.Count(w => w.FirstRegion(WorldCatalog.RegionSize) <= r && r <= w.LastRegion(WorldCatalog.RegionSize)), $"bölge {r} tek dünyada");
            }
        }

        [Test]
        public void Beyond_Configured_WorldsAreDerived_WithStableIds()
        {
            var c = Shipped();
            var all = c.All(10000);
            Assert.AreEqual(20, all.Count);
            Assert.AreEqual("world_03", all[2].WorldId);
            Assert.IsTrue(all[2].Derived);
            Assert.AreEqual("world_20", all[19].WorldId);
            Assert.AreEqual((9500, 9999), (all[19].FirstIndex, all[19].LastIndex));
            var partial = c.All(1234);
            Assert.AreEqual(3, partial.Count);
            Assert.AreEqual((1000, 1233), (partial[2].FirstIndex, partial[2].LastIndex), "son dünya kırpılır");
        }

        [Test]
        public void CompletedIn_CountsOnlyThatWorld()
        {
            var c = Shipped();
            var done = new HashSet<string> { "level_001", "level_500", "level_501", "level_1000" };
            Func<int, string> id = i => "level_" + (i + 1).ToString("000");
            Assert.AreEqual(2, WorldCatalog.CompletedIn(c.World(1, 1000), id, done.Contains));
            Assert.AreEqual(2, WorldCatalog.CompletedIn(c.World(2, 1000), id, done.Contains));
        }

        [Test]
        public void Parse_DefaultsAndRejectsBadConfig()
        {
            var d = WorldCatalog.Parse(null);
            Assert.AreEqual(500, d.WorldSize);
            Assert.AreEqual(2, d.CountFor(1000));
            Assert.AreEqual("world_01", d.World(1, 1000).WorldId);
            Assert.Throws<FormatException>(() => WorldCatalog.Parse("{\"version\":2}"));
            Assert.Throws<FormatException>(() => WorldCatalog.Parse("{\"version\":1,\"worldSize\":250}"), "bölge katı değil");
            Assert.Throws<FormatException>(() => WorldCatalog.Parse("{\"version\":1,\"worlds\":[{\"id\":\"a\"},{\"id\":\"a\"}]}"), "yinelenen");
            Assert.Throws<FormatException>(() => WorldCatalog.Parse("{\"version\":1,\"worlds\":[{}]}"), "kimliksiz");
        }
    }
}
