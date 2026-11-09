using System.Collections.Generic;
using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Levels;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>v9 A/B saf kuralları: yıldız toplamı, günlük haklar, sahte reklam olayları, taş gücü, +1 hamle, içerik.</summary>
    public class MetaTests
    {
        // ---- A: yıldızlar ------------------------------------------------------------------------------

        [Test]
        public void Stars_3Then1Then3_TotalIncreasesOnlyBy3()
        {
            var ids = new List<string>(); var counts = new List<int>();
            Assert.AreEqual(3, ProgressRules.RecordBest(ids, counts, "level_005", 3));
            Assert.AreEqual(0, ProgressRules.RecordBest(ids, counts, "level_005", 1));
            Assert.AreEqual(0, ProgressRules.RecordBest(ids, counts, "level_005", 3));
            Assert.AreEqual(3, ProgressRules.TotalStars(ids, counts));
        }

        [Test]
        public void Stars_1Then3_IncreaseIs2()
        {
            var ids = new List<string>(); var counts = new List<int>();
            Assert.AreEqual(1, ProgressRules.RecordBest(ids, counts, "level_007", 1));
            Assert.AreEqual(2, ProgressRules.RecordBest(ids, counts, "level_007", 3));
            Assert.AreEqual(3, ProgressRules.TotalStars(ids, counts));
        }

        /// <summary>v10 P1.4 (plan §4): 1 → 3 → 2; kötüleşen sonuç kaydı düşürmez, toplam 3 kalır.</summary>
        [Test]
        public void Stars_1Then3Then2_KeepsBest()
        {
            var ids = new List<string>(); var counts = new List<int>();
            Assert.AreEqual(1, ProgressRules.RecordBest(ids, counts, "level_008", 1));
            Assert.AreEqual(2, ProgressRules.RecordBest(ids, counts, "level_008", 3));
            Assert.AreEqual(0, ProgressRules.RecordBest(ids, counts, "level_008", 2));
            Assert.AreEqual(3, ProgressRules.TotalStars(ids, counts));
            Assert.AreEqual(1, ids.Count, "aynı bölüm tek kayıt");
        }

        /// <summary>Plan §4: 1.000 bölümde en çok 3.000 yıldız; tekrar oynamak toplamı aşamaz.</summary>
        [Test]
        public void Stars_1000LevelsAllThree_Is3000_EvenWithReplays()
        {
            var ids = new List<string>(); var counts = new List<int>();
            for (int rep = 0; rep < 3; rep++)
                for (int i = 1; i <= 1000; i++) ProgressRules.RecordBest(ids, counts, $"level_{i:000}", 1 + (rep + i) % 3); // her bölüm üç turda 1, 2 ve 3 alır
            Assert.AreEqual(3000, ProgressRules.TotalStars(ids, counts));
            Assert.AreEqual(1000, ids.Count);
        }

        [Test]
        public void Stars_InvalidValuesIgnored_AndNormalizeKeepsBestPerId()
        {
            var ids = new List<string>(); var counts = new List<int>();
            Assert.AreEqual(0, ProgressRules.RecordBest(ids, counts, "level_001", 0));
            Assert.AreEqual(0, ProgressRules.RecordBest(ids, counts, "level_001", 4));
            Assert.AreEqual(0, ProgressRules.TotalStars(ids, counts));

            var oldIds = new List<string> { "level_001", "level_002", "level_001", "", "level_003", "level_004" };
            var oldCounts = new List<int> { 1, 2, 3, 3, 7, 0 };
            int removed = ProgressRules.Normalize(oldIds, oldCounts);
            CollectionAssert.AreEqual(new[] { "level_001", "level_002" }, oldIds);
            CollectionAssert.AreEqual(new[] { 3, 2 }, oldCounts);
            Assert.AreEqual(4, removed);
            Assert.AreEqual(5, ProgressRules.TotalStars(oldIds, oldCounts));
        }

        // ---- B: günlük -----------------------------------------------------------------------------------

        private sealed class Rig
        {
            public MetaSave Meta = new MetaSave();
            public FixedDayClock Clock = new FixedDayClock { DayId = "2026-11-06" };
            public FakeRewardedAds Ads = new FakeRewardedAds();
            public FakeAdFree AdFree = new FakeAdFree();
            public int Saves;
            public DailyService Svc;

            public Rig() { Svc = New(); }

            /// <summary>Uygulamanın yeniden açılması: aynı kayıt, yeni servis.</summary>
            public DailyService New() => new DailyService(Meta, Clock,
                d => new DailyChallengeInfo { ChallengeId = "daily_001", LevelFile = "daily_001", ContentVersion = "daily-v1" },
                () => Saves++, Ads, AdFree);

            public void Enter()
            {
                DailyEntryResult r = DailyEntryResult.NotAvailable;
                Svc.RequestEntry(x => r = x);
                Assert.AreEqual(DailyEntryResult.Granted, r);
            }
        }

        [Test]
        public void Daily_EntryGivesFirstTryAndOneFreeRetry_ThenNoMore()
        {
            var g = new Rig();
            Assert.AreEqual(DailyBeginResult.NotEntered, g.Svc.BeginAttempt());
            g.Enter();
            Assert.AreEqual("2026-11-06|daily_001|daily-v1", g.Svc.State.Key);
            Assert.AreEqual(2, g.Svc.AttemptsLeft);
            Assert.AreEqual(DailyBeginResult.Started, g.Svc.BeginAttempt());
            Assert.IsFalse(g.Svc.FinishAttempt(false));
            Assert.AreEqual(DailyBeginResult.Started, g.Svc.BeginAttempt(), "ücretsiz tekrar ayrı reklam istemez");
            Assert.AreEqual(1, g.Ads.Shown);
            g.Svc.FinishAttempt(false);
            Assert.AreEqual(DailyBeginResult.NoAttemptsLeft, g.Svc.BeginAttempt(), "üçüncü deneme prototipte kapalı");
            DailyEntryResult again = DailyEntryResult.Granted;
            g.Svc.RequestEntry(x => again = x);
            Assert.AreEqual(DailyEntryResult.AlreadyEntered, again);
            Assert.AreEqual(1, g.Ads.Shown, "aynı gün ikinci giriş reklamı yok");
        }

        [Test]
        public void Daily_AdCancel_DoesNotChangeRights()
        {
            var g = new Rig();
            g.Ads.Next = AdOutcome.Cancelled;
            DailyEntryResult r = DailyEntryResult.Granted;
            g.Svc.RequestEntry(x => r = x);
            Assert.AreEqual(DailyEntryResult.AdCancelled, r);
            Assert.IsFalse(g.Svc.State.entered);
            g.Ads.Next = AdOutcome.Cancelled;
            g.Svc.RequestEntry(x => r = x);
            Assert.AreEqual(DailyEntryResult.AdCancelled, r, "ikinci iptal de hak açmaz");
            Assert.IsFalse(g.Svc.State.entered);
            Assert.AreEqual(0, g.Svc.State.attemptsUsed);
            g.Ads.Next = AdOutcome.Rewarded;
            g.Enter();
            Assert.AreEqual(2, g.Svc.AttemptsLeft, "iptal sonrası hak eksilmedi");
        }

        [Test]
        public void Daily_AdNoFill_PlaysWithoutAd()
        {
            // v12 (kullanıcı, 2026-11-08): reklam 5 sn içinde bulunamazsa günlük bulmaca reklamsız açılır.
            var g = new Rig();
            g.Ads.Next = AdOutcome.Failed;
            DailyEntryResult r = DailyEntryResult.AdFailed;
            g.Svc.RequestEntry(x => r = x);
            Assert.AreEqual(DailyEntryResult.GrantedNoAd, r);
            Assert.IsTrue(g.Svc.State.entered);
            Assert.AreEqual("noFill", g.Svc.State.entrySource);
            Assert.AreEqual(2, g.Svc.AttemptsLeft, "normal girişle aynı haklar");
        }

        [Test]
        public void Daily_DuplicateAdCallback_DoesNotCreateExtraRights()
        {
            var g = new Rig();
            g.Ads.DuplicateCallback = true;
            var results = new List<DailyEntryResult>();
            g.Svc.RequestEntry(results.Add);
            CollectionAssert.AreEqual(new[] { DailyEntryResult.Granted, DailyEntryResult.DuplicateIgnored }, results);
            Assert.AreEqual(2, g.Svc.State.attemptsAllowed);
            Assert.AreEqual(1, g.Svc.State.adTokens.Count);
        }

        [Test]
        public void Daily_AdFreeEntersWithoutAd()
        {
            var g = new Rig();
            g.AdFree.Owned = true;
            g.Enter();
            Assert.AreEqual("adFree", g.Svc.State.entrySource);
            Assert.AreEqual(0, g.Ads.Shown);
        }

        [Test]
        public void Daily_AppClosedMidAttempt_ResumesSameAttemptWithoutSpending()
        {
            var g = new Rig();
            g.Enter();
            Assert.AreEqual(DailyBeginResult.Started, g.Svc.BeginAttempt());
            int savesAfterBegin = g.Saves;
            Assert.Greater(savesAfterBegin, 0, "hak kalıcı kayıtla harcanır");
            // Uygulama kapandı ve açıldı: aynı kayıt.
            g.Svc = g.New();
            Assert.AreEqual(DailyBeginResult.Resumed, g.Svc.BeginAttempt());
            Assert.AreEqual(1, g.Svc.State.attemptsUsed);
            Assert.AreEqual(1, g.Svc.AttemptsLeft);
        }

        [Test]
        public void Daily_WinStars_AddedOnceAndClamped_CountInTotalStars()
        {
            // Kullanıcı (2026-11-08): günlük performansa göre 1–3 yıldız, günde bir kez (ilk galibiyet).
            var g = new Rig();
            g.Enter();
            g.Svc.BeginAttempt();
            g.Svc.FinishAttempt(false);
            Assert.AreEqual(0, g.Meta.dailyStars, "kayıp yıldız vermez");
            Assert.AreEqual(DailyBeginResult.Started, g.Svc.BeginAttempt());
            Assert.IsTrue(g.Svc.FinishAttempt(true, 5));
            Assert.AreEqual(3, g.Svc.State.stars, "en çok 3");
            Assert.AreEqual(3, g.Meta.dailyStars);
            Assert.IsFalse(g.Svc.FinishAttempt(true, 3), "ikinci ödül yok");
            Assert.AreEqual(3, g.Meta.dailyStars);

            g.Clock.DayId = "2026-11-07";
            g.Svc.Refresh();
            Assert.AreEqual(0, g.Svc.State.stars, "yeni gün sıfır");
            g.Enter();
            g.Svc.BeginAttempt();
            Assert.IsTrue(g.Svc.FinishAttempt(true, 1));
            Assert.AreEqual(4, g.Meta.dailyStars);

            Assert.AreEqual(0, RankStars.Total(g.Meta), "sıralamaya sayılmaz");
        }

        [Test]
        public void Daily_WinRewardsOncePerDay_AndBonusOnlyTodayForUncompletedLevels()
        {
            var g = new Rig();
            Assert.AreEqual(0, g.Svc.MoveBonusFor(false));
            g.Enter();
            g.Svc.BeginAttempt();
            Assert.IsTrue(g.Svc.FinishAttempt(true));
            Assert.AreEqual(1, g.Meta.rewardLog.Count);
            Assert.AreEqual(DailyBeginResult.AlreadyWon, g.Svc.BeginAttempt());
            Assert.IsFalse(g.Svc.FinishAttempt(true), "ikinci ödül yok");
            Assert.AreEqual(1, g.Svc.MoveBonusFor(false));
            Assert.AreEqual(0, g.Svc.MoveBonusFor(true), "kazanılmış bölüme geriye dönük etki yok");

            // Gün değişti: günlük durum sıfırlanır, eski ödül yeni oturumlara taşınmaz, ödül günlüğü kalır.
            g.Clock.DayId = "2026-11-07";
            g.Svc.Refresh();
            Assert.AreEqual(0, g.Svc.MoveBonusFor(false));
            Assert.IsFalse(g.Svc.State.entered);
            Assert.IsFalse(g.Svc.State.won);
            Assert.AreEqual(1, g.Meta.rewardLog.Count);
            Assert.AreEqual("2026-11-07", g.Svc.State.dayId);
        }

        [Test]
        public void Daily_AdRewardArrivingAfterDayChange_DoesNotOpenNewDay()
        {
            var g = new Rig();
            var late = new LateAds();
            var svc = new DailyService(g.Meta, g.Clock, d => new DailyChallengeInfo { ChallengeId = "daily_001", LevelFile = "daily_001", ContentVersion = "daily-v1" }, () => { }, late, g.AdFree);
            DailyEntryResult r = DailyEntryResult.Granted;
            svc.RequestEntry(x => r = x);
            g.Clock.DayId = "2026-11-07";
            late.Fire();
            Assert.AreEqual(DailyEntryResult.DuplicateIgnored, r);
            Assert.IsFalse(svc.State.entered);
        }

        private sealed class LateAds : IRewardedAds
        {
            private System.Action<AdOutcome, string> _done;
            public void Show(string placement, System.Action<AdOutcome, string> done) => _done = done;
            public void Fire() => _done(AdOutcome.Rewarded, "late-1");
        }

        // ---- B: taş ve güç -------------------------------------------------------------------------------

        private static StoneCatalog Catalog() => StoneCatalog.Parse(
            "{\"stones\":[{\"stoneId\":\"stone_tide\",\"nameKey\":\"n\",\"descKey\":\"d\",\"artKey\":\"a\",\"powerId\":\"dailyRoll\",\"unlock\":{\"type\":\"levelCompleted\",\"value\":10}}]}");

        [Test]
        public void Stone_GrantedOnceWhenThresholdLevelCompleted()
        {
            var meta = new MetaSave();
            var done = new HashSet<string>();
            Assert.IsEmpty(CollectionRules.GrantUnlocked(meta, Catalog(), done.Contains));
            done.Add("level_010");
            Assert.AreEqual(1, CollectionRules.GrantUnlocked(meta, Catalog(), done.Contains).Count);
            Assert.IsEmpty(CollectionRules.GrantUnlocked(meta, Catalog(), done.Contains), "tek seferlik");
            CollectionAssert.AreEqual(new[] { "stone_tide" }, meta.stones);
            Assert.AreEqual("stone_tide", CollectionRules.ActiveStone(meta));
        }

        private static LevelState RollLevel()
        {
            var s = TestBoards.State(5, new[] { "cb cb yb", "pb yb cb" });
            s.RollEnabled = true;
            return s;
        }

        [Test]
        public void Power_WithoutStone_OrWithoutAttempt_IsRejected()
        {
            var g = new Rig();
            Assert.AreEqual(PowerResult.NoAttempt, g.Svc.TryUsePower(Catalog(), RollLevel()));
            g.Enter();
            g.Svc.BeginAttempt();
            Assert.AreEqual(PowerResult.NoStone, g.Svc.TryUsePower(Catalog(), RollLevel()));
        }

        [Test]
        public void Power_OncePerAttempt_InvalidTargetDoesNotSpend_ResetsOnNextAttempt()
        {
            var g = new Rig();
            g.Meta.stones.Add("stone_tide");
            g.Enter();
            g.Svc.BeginAttempt();
            var s = RollLevel();
            s.RollSlotFull = true; // yuva dolu: geçersiz hedef
            Assert.AreEqual(PowerResult.InvalidTarget, g.Svc.TryUsePower(Catalog(), s));
            Assert.IsFalse(g.Svc.State.powerUsed, "geçersiz hedef hakkı tüketmez");
            s.RollSlotFull = false;
            Assert.AreEqual(PowerResult.Used, g.Svc.TryUsePower(Catalog(), s));
            Assert.IsTrue(s.RollSlotFull);
            s.RollSlotFull = false;
            Assert.AreEqual(PowerResult.AlreadyUsed, g.Svc.TryUsePower(Catalog(), s));
            Assert.IsFalse(s.RollSlotFull, "ikinci kullanım durumu değiştirmez");
            g.Svc.FinishAttempt(false);
            g.Svc.BeginAttempt();
            Assert.AreEqual(PowerResult.Used, g.Svc.TryUsePower(Catalog(), RollLevel()), "ücretsiz tekrarda yeniden bir kez");
        }

        [Test]
        public void Power_RollDisabledLevel_IsInvalid()
        {
            var s = RollLevel();
            s.RollEnabled = false;
            Assert.IsFalse(StonePowers.Apply(StonePowers.DailyRoll, s));
            Assert.IsFalse(s.RollSlotFull);
        }

        // ---- İçerik ---------------------------------------------------------------------------------------

        private static string ResourcesDir() => Path.GetDirectoryName(ProductionLevelTests.LevelsDir());

        [Test]
        public void StoneCatalogFile_ParsesWithConfigurableThreshold()
        {
            var cat = StoneCatalog.Parse(File.ReadAllText(Path.Combine(ResourcesDir(), "Collection", "stones.json")));
            Assert.AreEqual(22, cat.Stones.Count, "ilk sürüm: 10 + 10 + 2");
            Assert.AreEqual("stone_tide", cat.Stones[0].StoneId);
            var s = cat.Stones[0];
            Assert.AreEqual(StonePowers.DailyRoll, s.PowerId);
            Assert.AreEqual("levelCompleted", s.UnlockType);
            Assert.Greater(s.UnlockValue, 0);
            Assert.AreNotEqual(s.StoneId, s.ArtKey, "mantıksal kimlik görsel anahtardan ayrı");
        }

        [Test]
        public void DailyCatalog_SeparateFromMain_SolvedWithoutPower_PowerApplicableAtStart()
        {
            var dir = Path.Combine(ResourcesDir(), "Daily");
            var idx = DailyIndex.Parse(File.ReadAllText(Path.Combine(dir, "daily_index.json")));
            Assert.GreaterOrEqual(idx.Days.Count, 1, "en az bir gün");
            Assert.AreEqual(8, idx.Tiers.Count, "8 seviye grubu");
            Assert.IsNotNull(idx.PickFor("2026-11-06", 1));

            // Mekaniklerin ana kataloğda ilk göründüğü bölüm: grup, oyuncunun henüz görmediği mekaniği içermez.
            var firstSeen = new Dictionary<string, int>();
            foreach (var f in Directory.GetFiles(ProductionLevelTests.LevelsDir(), "level_*.json").OrderBy(f => LevelValidator.LevelNumber(Path.GetFileNameWithoutExtension(f))))
            {
                int n = LevelValidator.LevelNumber(Path.GetFileNameWithoutExtension(f));
                foreach (var cap in LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f)).Capabilities)
                    if (!firstSeen.ContainsKey(cap)) firstSeen[cap] = n;
            }

            var seen = new HashSet<string>();
            foreach (var day in idx.Days)
            {
                Assert.AreEqual(idx.Tiers.Count, day.Count, "her gün her grubun bulmacası var");
                foreach (var kv in day)
                {
                    var file = kv.Value;
                    Assert.IsTrue(seen.Add(file), "aynı dosya iki kez: " + file);
                    var path = Path.Combine(dir, file + ".json");
                    Assert.IsTrue(File.Exists(path), file);
                    var e = ValidationReport.Check(Path.GetFileName(path), File.ReadAllText(path));
                    Assert.IsEmpty(e.StaticErrors, file + ": " + string.Join(" | ", e.StaticErrors));
                    Assert.IsTrue(e.Replay.Success, file + ": kayıtlı çözüm (güç kullanmadan) kazanmalı: " + e.Replay.Error);
                    var d = LevelParser.Parse(File.ReadAllText(path), file + ".json");
                    Assert.IsTrue(d.RollEnabled, file + ": taş gücü (rulo) bu düzende kurallara uygun olmalı");
                    Assert.IsTrue(StonePowers.CanApply(StonePowers.DailyRoll, LevelLoader.ToState(d)), file);
                    Assert.IsFalse(File.Exists(Path.Combine(ProductionLevelTests.LevelsDir(), file + ".json")), "ana katalogda değil");
                    int known = System.Math.Max(10, idx.TierInfo(kv.Key).MinLevel);
                    foreach (var cap in d.Capabilities)
                        if (cap != "specials" && firstSeen.TryGetValue(cap, out var first))
                            Assert.LessOrEqual(first, known, $"{file}: grup {kv.Key} oyuncusu '{cap}' mekaniğini henüz görmedi");
                }
            }
            Assert.AreEqual(1000, Directory.GetFiles(ProductionLevelTests.LevelsDir(), "level_*.json").Length, "ana 1.000 bölüm");
        }

        // ---- v10 P3.4: seviye grupları ------------------------------------------------------------------------

        private static DailyIndex TierIndex() => DailyIndex.Parse(
            "{\"contentVersion\":\"daily-v2\",\"tiers\":[{\"tier\":1,\"minLevel\":1,\"maxLevel\":40},{\"tier\":2,\"minLevel\":41,\"maxLevel\":100},{\"tier\":3,\"minLevel\":101,\"maxLevel\":1000}]," +
            "\"days\":[{\"day\":1,\"levels\":{\"1\":\"a1\",\"2\":\"a2\",\"3\":\"a3\"}},{\"day\":2,\"levels\":{\"1\":\"b1\",\"2\":\"b2\",\"3\":\"b3\"}}]}");

        [Test]
        public void DailyTiers_ByReachedLevel_WithEdges()
        {
            var idx = TierIndex();
            Assert.AreEqual(1, idx.TierFor(0));
            Assert.AreEqual(1, idx.TierFor(40));
            Assert.AreEqual(2, idx.TierFor(41));
            Assert.AreEqual(2, idx.TierFor(100));
            Assert.AreEqual(3, idx.TierFor(101));
            Assert.AreEqual(3, idx.TierFor(5000), "aralık üstü son grup");
            Assert.AreEqual("a2", idx.PickFor("2026-01-01", 2).LevelFile);
            Assert.AreEqual("b2", idx.PickFor("2026-01-02", 2).LevelFile);
            Assert.AreEqual("a2", idx.PickFor("2026-01-03", 2).LevelFile, "katalog günleri döner");
            Assert.AreEqual("daily-v2", idx.PickFor("2026-01-01", 1).ContentVersion);
            Assert.IsNull(idx.PickFor("2026-01-01", 9), "katalogda olmayan grup");
        }

        [Test]
        public void DailyTier_LockedForTheDay_EvenIfPlayerProgresses_NewDayRecomputes()
        {
            var meta = new MetaSave();
            var clock = new FixedDayClock { DayId = "2026-01-01" };
            var idx = TierIndex();
            int reached = 35;
            var svc = new DailyService(meta, clock, (d, t) => idx.PickFor(d, t), () => idx.TierFor(reached), null, new FakeRewardedAds(), new FakeAdFree());
            svc.Refresh();
            Assert.AreEqual(1, svc.Tier);
            Assert.AreEqual("a1", svc.Challenge.LevelFile);
            DailyEntryResult r = DailyEntryResult.NotAvailable;
            svc.RequestEntry(x => r = x);
            Assert.AreEqual(DailyEntryResult.Granted, r);

            reached = 120; // gün içinde grup 3'e ilerledi
            svc.Refresh();
            Assert.AreEqual(1, svc.Tier, "gün içinde sabit");
            Assert.AreEqual("a1", svc.Challenge.LevelFile);
            Assert.IsTrue(svc.State.entered, "açılmış hak sıfırlanmadı");

            var reopened = new DailyService(meta, clock, (d, t) => idx.PickFor(d, t), () => idx.TierFor(reached), null, new FakeRewardedAds(), new FakeAdFree());
            reopened.Refresh();
            Assert.AreEqual(1, reopened.Tier, "uygulama yeniden açılınca da sabit");

            clock.DayId = "2026-01-02";
            reopened.Refresh();
            Assert.AreEqual(3, reopened.Tier, "yeni gün yeni grup");
            Assert.AreEqual("b3", reopened.Challenge.LevelFile);
            Assert.IsFalse(reopened.State.entered);
        }
    }
}
