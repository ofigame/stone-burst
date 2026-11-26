using System.IO;
using KatKat.Core;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>v12 reklam kuralları (kullanıcı, 2026-11-08; docs/v12/PLAN.md §1).</summary>
    public class AdsTests
    {
        [Test]
        public void Interstitial_FromLevel8_EveryThreeLevels_150s_NotAfterRewarded()
        {
            // 2026-11-26 (kullanıcı: "hem oyuncuyu boğmayalım hem de para kazanalım"): ilk 7 bölüm yok, 3 bölümde bir, 150 sn,
            // ödüllüden sonra 90 sn.
            var p = new InterstitialPolicy(new AdConfig());
            for (int lv = 1; lv <= 7; lv++) { p.LevelCompleted(lv); Assert.IsFalse(p.ShouldShow(lv, 1000), $"bölüm {lv}"); }
            p.LevelCompleted(8); p.LevelCompleted(9);
            Assert.IsFalse(p.ShouldShow(9, 1000), "8. bölümden beri yalnız 2 bölüm");
            p.LevelCompleted(10);
            Assert.IsTrue(p.ShouldShow(10, 1000));
            p.Shown(1000);
            for (int lv = 11; lv <= 13; lv++) p.LevelCompleted(lv);
            Assert.IsFalse(p.ShouldShow(13, 1100), "150 sn dolmadı");
            Assert.IsTrue(p.ShouldShow(13, 1151));
            p.RewardedShown(1100);
            Assert.IsFalse(p.ShouldShow(13, 1151), "ödüllüden 90 sn geçmedi");
            Assert.IsTrue(p.ShouldShow(13, 1191));
        }

        [Test]
        public void Interstitial_SessionCap()
        {
            var p = new InterstitialPolicy(new AdConfig { InterstitialSessionCap = 2, InterstitialMinSeconds = 0, InterstitialEveryLevels = 1 });
            double t = 0;
            for (int i = 0; i < 2; i++) { p.LevelCompleted(20); Assert.IsTrue(p.ShouldShow(20, t)); p.Shown(t); t += 1; }
            p.LevelCompleted(20);
            Assert.IsFalse(p.ShouldShow(20, t));
        }

        [Test]
        public void ShippedAdConfig_Parses()
        {
            var res = Path.GetDirectoryName(ProductionLevelTests.LevelsDir());
            var c = AdConfig.Parse(File.ReadAllText(Path.Combine(res, "Ads", "ad_config.json")));
            Assert.AreEqual(5f, c.SearchSeconds);
            Assert.AreEqual(1, c.ExtraMoves);
            Assert.AreEqual(1, c.ExtraMovesPerLevel);
            Assert.AreEqual(8, c.InterstitialFirstLevel);
            Assert.AreEqual(3, c.InterstitialEveryLevels);
            Assert.AreEqual(150f, c.InterstitialMinSeconds);
            Assert.AreEqual(90f, c.InterstitialAfterRewardedSeconds);
            Assert.AreEqual(12, c.InterstitialSessionCap);
        }

        [Test]
        public void GrantMoves_RevivesOutOfMoves_KeepsUndoConsistent()
        {
            var s = new LevelState { Board = new BoardState(1, 1), MovesLeft = 0, Status = GameStatus.LostOutOfMoves };
            var session = new GameSession(s);
            Assert.IsTrue(session.GrantMoves(3));
            Assert.AreEqual(3, session.State.MovesLeft);
            Assert.AreEqual(GameStatus.Idle, session.State.Status);
            var stuck = new GameSession(new LevelState { Board = new BoardState(1, 1), Status = GameStatus.LostNoLegalMoves });
            Assert.IsFalse(stuck.GrantMoves(3), "hamle yok kaybında ek hamle işe yaramaz");
        }

        [Test]
        public void Zpe_Retract_UndoesAdd()
        {
            var meta = new MetaSave();
            var a = new ZpeRecord { levelId = "x", success = true, firstAttempt = true, movesUsed = 4, moveLimit = 10, seconds = 20 };
            Zpe.Add(meta, a);
            double sum = meta.zpeSum;
            var lost = new ZpeRecord { levelId = "y", success = false, movesUsed = 10, moveLimit = 10 };
            Zpe.Add(meta, lost);
            Zpe.Retract(meta, lost);
            Assert.AreEqual(1, meta.zpeGames);
            Assert.AreEqual(sum, meta.zpeSum, 1e-9);
            CollectionAssert.DoesNotContain(meta.zpeHistory, lost);
        }

        [Test]
        public void RankStars_BestWithoutAds()
        {
            var meta = new MetaSave();
            RankStars.Record(meta, "level_001", 1);
            RankStars.Record(meta, "level_001", 3);
            RankStars.Record(meta, "level_001", 2);
            RankStars.Record(meta, "level_002", 2);
            Assert.AreEqual(3, RankStars.For(meta, "level_001"));
            Assert.AreEqual(5, RankStars.Total(meta));
        }
    }
}
