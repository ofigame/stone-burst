using System;
using System.Collections.Generic;
using System.IO;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>v10 P2.1: taş kataloğu şeması (kategori, nadirlik, kazanım türleri); eski v9 dosyası geçerli kalır.</summary>
    public class StoneSchemaTests
    {
        // v9'da yayımlanan stones.json'un birebir içeriği (şema alanı yok).
        private const string V9File = "{\"_not\":\"v9\",\"stones\":[{\"stoneId\":\"stone_tide\",\"nameKey\":\"stone.tide.name\",\"descKey\":\"stone.tide.desc\"," +
            "\"artKey\":\"collection/stone_tide\",\"powerId\":\"dailyRoll\",\"unlock\":{\"type\":\"levelCompleted\",\"value\":10}}]}";

        private static string One(string extra, string unlock) =>
            "{\"schemaVersion\":2,\"stones\":[{\"stoneId\":\"s1\",\"powerId\":\"moon\"" + extra + ",\"unlock\":" + unlock + "}]}";

        [Test]
        public void OldV9File_StillParses_WithDefaults_AndSameBehaviour()
        {
            var cat = StoneCatalog.Parse(V9File);
            Assert.AreEqual(1, cat.Schema);
            var s = cat.Stones[0];
            Assert.AreEqual(StoneCategory.StarStone, s.Category);
            Assert.AreEqual(StoneRarity.Common, s.Rarity);
            Assert.AreEqual(StoneUnlock.LevelCompleted, s.UnlockType);
            Assert.AreEqual(10, s.UnlockValue);
            Assert.AreEqual("", s.UnlockKey);
            var meta = new MetaSave();
            var done = new HashSet<string> { "level_009" };
            Assert.IsEmpty(CollectionRules.GrantUnlocked(meta, cat, done.Contains));
            done.Add("level_010");
            Assert.AreEqual(1, CollectionRules.GrantUnlocked(meta, cat, done.Contains).Count);
        }

        [Test]
        public void ShippedFile_IsSchema2_AndKeepsV9Stones()
        {
            var p = Path.Combine(Path.GetDirectoryName(ProductionLevelTests.LevelsDir()), "Collection", "stones.json");
            var now = StoneCatalog.Parse(File.ReadAllText(p));
            var old = StoneCatalog.Parse(V9File);
            Assert.AreEqual(2, now.Schema);
            // v9'da kazanılmış taşların anlamı değişmez (görsel anahtarı hariç: görsel değişebilsin diye ayrı).
            foreach (var a in old.Stones)
            {
                var b = now.Find(a.StoneId);
                Assert.IsNotNull(b, a.StoneId + " katalogdan çıkmamalı");
                Assert.AreEqual((a.NameKey, a.DescKey, a.PowerId, a.UnlockType, a.UnlockValue, a.Category),
                    (b.NameKey, b.DescKey, b.PowerId, b.UnlockType, b.UnlockValue, b.Category), "kazanılmış taşın anlamı değişmedi");
            }
        }

        /// <summary>v10 ilk sürüm içeriği (kullanıcı onayı 2026-11-07): 10 yıldız taşı + 10 gizli hazine + 2 efsanevi.</summary>
        [Test]
        public void ShippedFile_LaunchContent_22Items()
        {
            var res = Path.GetDirectoryName(ProductionLevelTests.LevelsDir());
            var cat = StoneCatalog.Parse(File.ReadAllText(Path.Combine(res, "Collection", "stones.json")));
            Assert.AreEqual(10, cat.InCategory(StoneCategory.StarStone).Count);
            Assert.AreEqual(10, cat.InCategory(StoneCategory.HiddenTreasure).Count);
            Assert.AreEqual(2, cat.InCategory(StoneCategory.LegendaryStone).Count);
            var keys = new HashSet<string>();
            foreach (var s in cat.Stones)
            {
                Assert.IsTrue(File.Exists(Path.Combine(res, s.ArtKey + ".png")), $"{s.StoneId}: görsel yok ({s.ArtKey})");
                Assert.IsTrue(keys.Add(s.UnlockType + "|" + s.UnlockValue + "|" + s.UnlockKey), $"{s.StoneId}: aynı koşul iki öğede");
                Assert.IsNotEmpty(s.NameKey);
                Assert.IsNotEmpty(s.DescKey);
            }
            // Yıldız taşı eşikleri artan ve 1.000 bölümün tavanı (3.000) içinde; efsanevi dünya kimlikleri gerçek dünyalar.
            int last = 0;
            foreach (var s in cat.InCategory(StoneCategory.StarStone))
                if (s.UnlockType == StoneUnlock.StarsTotal) { Assert.Greater(s.UnlockValue, last); Assert.LessOrEqual(s.UnlockValue, 3000); last = s.UnlockValue; }
            var worlds = WorldCatalog.Parse(File.ReadAllText(Path.Combine(res, "Worlds", "worlds.json"))).All(1000).ConvertAll(w => w.WorldId);
            foreach (var s in cat.InCategory(StoneCategory.LegendaryStone))
                CollectionAssert.Contains(worlds, s.UnlockKey);
        }

        [Test]
        public void NewFields_Parse()
        {
            var cat = StoneCatalog.Parse("{\"schemaVersion\":2,\"stones\":[" +
                "{\"stoneId\":\"a\",\"category\":\"starStone\",\"rarity\":\"rare\",\"unlock\":{\"type\":\"starsTotal\",\"value\":250}}," +
                "{\"stoneId\":\"b\",\"category\":\"hiddenTreasure\",\"rarity\":\"epic\",\"unlock\":{\"type\":\"treasure\",\"value\":\"tr_042\"}}," +
                "{\"stoneId\":\"c\",\"category\":\"legendaryStone\",\"rarity\":\"legendary\",\"unlock\":{\"type\":\"worldCompleted\",\"key\":\"world_01\"}}]}");
            Assert.AreEqual((StoneUnlock.StarsTotal, 250, StoneRarity.Rare), (cat.Stones[0].UnlockType, cat.Stones[0].UnlockValue, cat.Stones[0].Rarity));
            Assert.AreEqual((StoneCategory.HiddenTreasure, "tr_042"), (cat.Stones[1].Category, cat.Stones[1].UnlockKey));
            Assert.AreEqual((StoneCategory.LegendaryStone, "world_01"), (cat.Stones[2].Category, cat.Stones[2].UnlockKey));
            Assert.AreEqual(1, cat.InCategory(StoneCategory.HiddenTreasure).Count);
        }

        [Test]
        public void EachUnlockType_GrantsOnlyWhenMet_AndOnce()
        {
            var cat = StoneCatalog.Parse("{\"schemaVersion\":2,\"stones\":[" +
                "{\"stoneId\":\"lvl\",\"unlock\":{\"type\":\"levelCompleted\",\"value\":10}}," +
                "{\"stoneId\":\"stars\",\"unlock\":{\"type\":\"starsTotal\",\"value\":250}}," +
                "{\"stoneId\":\"trs\",\"category\":\"hiddenTreasure\",\"unlock\":{\"type\":\"treasure\",\"value\":\"tr_1\"}}," +
                "{\"stoneId\":\"wld\",\"category\":\"legendaryStone\",\"unlock\":{\"type\":\"worldCompleted\",\"value\":\"world_01\"}}," +
                "{\"stoneId\":\"future\",\"unlock\":{\"type\":\"someDayMaybe\",\"value\":1}}]}");
            var meta = new MetaSave();
            var done = new HashSet<string>();
            var treasures = new HashSet<string>();
            var worlds = new HashSet<string>();
            var ctx = new UnlockContext { IsLevelCompleted = done.Contains, TotalStars = 249, HasTreasure = treasures.Contains, IsWorldCompleted = worlds.Contains };
            Assert.IsEmpty(CollectionRules.GrantUnlocked(meta, cat, ctx), "hiçbiri sağlanmadı (249 yıldız)");

            ctx.TotalStars = 250;
            CollectionAssert.AreEqual(new[] { "stars" }, Ids(CollectionRules.GrantUnlocked(meta, cat, ctx)), "eşik dahil");
            treasures.Add("tr_2");
            Assert.IsEmpty(CollectionRules.GrantUnlocked(meta, cat, ctx), "başka hazine");
            treasures.Add("tr_1");
            worlds.Add("world_02");
            CollectionAssert.AreEqual(new[] { "trs" }, Ids(CollectionRules.GrantUnlocked(meta, cat, ctx)));
            worlds.Add("world_01");
            done.Add("level_010");
            CollectionAssert.AreEqual(new[] { "lvl", "wld" }, Ids(CollectionRules.GrantUnlocked(meta, cat, ctx)));
            ctx.TotalStars = 10; // yıldız düşse (olmaz ama) kazanılmış taş geri alınmaz
            Assert.IsEmpty(CollectionRules.GrantUnlocked(meta, cat, ctx), "tek seferlik");
            CollectionAssert.AreEquivalent(new[] { "stars", "trs", "lvl", "wld" }, meta.stones);
            Assert.IsFalse(meta.stones.Contains("future"), "bilinmeyen tür kazandırmaz");
        }

        [Test]
        public void MissingContext_GrantsNothingForThatType()
        {
            var cat = StoneCatalog.Parse(One("", "{\"type\":\"treasure\",\"value\":\"tr_1\"}"));
            Assert.IsEmpty(CollectionRules.GrantUnlocked(new MetaSave(), cat, new UnlockContext()));
            Assert.IsFalse(CollectionRules.IsMet(cat.Stones[0], (UnlockContext)null));
        }

        [Test]
        public void BadConfig_IsRejected()
        {
            Assert.Throws<FormatException>(() => StoneCatalog.Parse("{\"schemaVersion\":3,\"stones\":[]}"), "gelecek şema");
            Assert.Throws<FormatException>(() => StoneCatalog.Parse(One(",\"category\":\"gem\"", "{\"type\":\"levelCompleted\",\"value\":1}")));
            Assert.Throws<FormatException>(() => StoneCatalog.Parse(One(",\"rarity\":\"mythic\"", "{\"type\":\"levelCompleted\",\"value\":1}")));
            Assert.Throws<FormatException>(() => StoneCatalog.Parse(One("", "{\"type\":\"starsTotal\",\"value\":0}")));
            Assert.Throws<FormatException>(() => StoneCatalog.Parse(One("", "{\"type\":\"treasure\"}")));
            Assert.Throws<FormatException>(() => StoneCatalog.Parse(One("", "{\"type\":\"worldCompleted\",\"value\":\"\"}")));
        }

        private static List<string> Ids(List<StoneDefinition> l) => l.ConvertAll(s => s.StoneId);
    }
}
