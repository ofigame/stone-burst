using System.Collections.Generic;
using System.IO;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>vr1 paketi 03_KOLEKSIYON_ODUL / 06_TEST_KABUL C01, C04, C06: buzlu taş ilerlemesi ve haritaya dönüş özeti.</summary>
    public class CollectionProgressTests
    {
        private static StoneCatalog Catalog()
        {
            var levels = ProductionLevelTests.LevelsDir();
            var path = Path.Combine(Path.GetDirectoryName(levels), "Collection", "stones.json");
            return StoneCatalog.Parse(File.ReadAllText(path));
        }

        private static List<StoneDefinition> StarStones(StoneCatalog c)
        {
            var l = c.Stones.FindAll(s => s.Category == StoneCategory.StarStone && s.UnlockType == StoneUnlock.StarsTotal);
            l.Sort((a, b) => a.UnlockValue.CompareTo(b.UnlockValue));
            return l;
        }

        /// <summary>Hedef ve eşikler katalogdan gelir (kodda sabit sayı yok); açılmış taş geri kilitlenmez.</summary>
        [Test]
        public void Goal_ComesFromCatalog_InThresholdOrder()
        {
            var c = Catalog();
            var stars = StarStones(c);
            Assert.GreaterOrEqual(stars.Count, 2);
            var owned = new List<string>();
            var g = CollectionProgress.NextStarGoal(c, owned, 0);
            Assert.AreEqual(stars[0].StoneId, g.StoneId);
            Assert.AreEqual(0, g.From);
            owned.Add(stars[0].StoneId);
            g = CollectionProgress.NextStarGoal(c, owned, stars[0].UnlockValue);
            Assert.AreEqual(stars[1].StoneId, g.StoneId);
            Assert.AreEqual(stars[0].UnlockValue, g.From);
            Assert.AreEqual(stars[1].UnlockValue, g.To);
            // Yıldız sonradan düşse de (ör. bulut eşitleme) sahip olunan taş geri kilitlenmez: hedef ilerdeki taştır.
            g = CollectionProgress.NextStarGoal(c, owned, 0);
            Assert.AreEqual(stars[1].StoneId, g.StoneId);
        }

        /// <summary>C01: eşik 60 → 58→59 açılmaz (p &lt; 1), 58→60 eşiğe ulaşır (kazanımı CollectionRules verir).</summary>
        [Test]
        public void Threshold_58to59_NotOpen_58to60_Reached()
        {
            var c = Catalog();
            var t = StarStones(c).Find(s => s.UnlockValue == 60);
            Assert.IsNotNull(t, "60 yıldız eşikli taş");
            var owned = new List<string>();
            foreach (var s in StarStones(c)) if (s.UnlockValue < 60) owned.Add(s.StoneId);
            var g59 = CollectionProgress.NextStarGoal(c, owned, 59);
            Assert.AreEqual(t.StoneId, g59.StoneId);
            Assert.Less(CollectionProgress.Progress(g59), 1f);
            Assert.AreEqual(3, CollectionProgress.IceStage(CollectionProgress.Progress(g59)));
            Assert.IsFalse(CollectionRules.IsMet(t, new UnlockContext { TotalStars = 59 }));
            Assert.IsTrue(CollectionRules.IsMet(t, new UnlockContext { TotalStars = 60 }));
            var meta = new MetaSave();
            meta.stones.AddRange(owned);
            var granted = CollectionRules.GrantUnlocked(meta, c, new UnlockContext { TotalStars = 60 });
            Assert.IsTrue(granted.Exists(s => s.StoneId == t.StoneId));
            Assert.AreEqual(CollectionProgress.Unlocked, CollectionProgress.IceStage(1f, unlocked: true));
            // Açılış öncesi görünüm (haritaya dönüş): yeni kazanılan taş yok sayılınca hedef hâlâ o taştır.
            var before = CollectionProgress.NextStarGoal(c, meta.stones, 58, new[] { t.StoneId });
            Assert.AreEqual(t.StoneId, before.StoneId);
        }

        [Test]
        public void IceStages_FollowPrototypeThresholds()
        {
            Assert.AreEqual(0, CollectionProgress.IceStage(0f));
            Assert.AreEqual(0, CollectionProgress.IceStage(0.249f));
            Assert.AreEqual(1, CollectionProgress.IceStage(0.25f));
            Assert.AreEqual(2, CollectionProgress.IceStage(0.5f));
            Assert.AreEqual(3, CollectionProgress.IceStage(0.75f));
            Assert.AreEqual(3, CollectionProgress.IceStage(0.999f), "kazanım olmadan tamamen açılmaz");
        }

        /// <summary>C01: önceki en iyi 2, bu sonuç en çok 3 → katkı 1; ipucu bedeli hesaba katılır.</summary>
        [Test]
        public void CanUnlockThisAttempt_UsesNetNewStars()
        {
            var g = new StarGoal { Has = true, Stars = 59, From = 10, To = 60 };
            Assert.IsTrue(CollectionProgress.CanUnlockThisAttempt(g, previousBest: 2));
            Assert.IsFalse(CollectionProgress.CanUnlockThisAttempt(g, previousBest: 3), "tekrar kazanım 0");
            Assert.IsFalse(CollectionProgress.CanUnlockThisAttempt(g, previousBest: 2, penalty: 1), "ipucu bedeliyle en çok 2");
            g.Stars = 57;
            Assert.IsTrue(CollectionProgress.CanUnlockThisAttempt(g, previousBest: 0));
            Assert.IsFalse(CollectionProgress.CanUnlockThisAttempt(g, previousBest: 1));
        }

        /// <summary>C06: harita özeti kayıt farkıdır; aynı yıldız tekrar sayılmaz, eski kayıtta (-1) fark yok.</summary>
        [Test]
        public void MapReturn_IsRecordDifference()
        {
            var r = CollectionProgress.Compare(40, 15, 49, 19, 4);
            Assert.AreEqual(9, r.NetStars);
            Assert.AreEqual(4, r.NewLevels);
            Assert.IsFalse(r.ReplayOnly);
            r = CollectionProgress.Compare(49, 19, 49, 19, 2);
            Assert.IsFalse(r.HasChange);
            Assert.IsTrue(r.ReplayOnly, "yeni yıldız yoksa sahte +0 kutlama yok");
            r = CollectionProgress.Compare(-1, -1, 49, 19, 3);
            Assert.IsFalse(r.HasChange);
            Assert.IsFalse(r.ReplayOnly);
        }
    }
}
