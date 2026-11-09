using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>v12 sıralama puanı (kullanıcı kararı 2026-11-08): %50 ZPE + %30 ulaşılan bölüm + %20 yıldız oranı, 0–1000.</summary>
    public class RankScoreTests
    {
        [Test]
        public void Formula_Weights_And_Range()
        {
            Assert.AreEqual(1000, RankScore.Compute(100, 1000, 1000, 3000, 1000), "her şey tam");
            Assert.AreEqual(0, RankScore.Compute(0, 0, 1000, 0, 0));
            Assert.AreEqual(500, RankScore.Compute(100, 0, 1000, 0, 10), "yalnız ZPE = %50");
            Assert.AreEqual(300, RankScore.Compute(0, 1000, 1000, 0, 10), "yalnız bölüm = %30");
            Assert.AreEqual(200, RankScore.Compute(0, 0, 1000, 30, 10), "yalnız yıldız oranı = %20");
            // 72 ZPE, 500. bölüm, 400 bölümde 1000 yıldız (%83,3): 10 × (36 + 15 + 16,67) = 677
            Assert.AreEqual(677, RankScore.Compute(72, 500, 1000, 1000, 400));
            Assert.That(RankScore.Compute(150, 2000, 1000, 9999, 1), Is.InRange(0, 1000), "taşma sınırlanır");
        }

        [Test]
        public void Eligibility_Needs20Levels()
        {
            Assert.IsFalse(RankScore.Eligible(19));
            Assert.IsTrue(RankScore.Eligible(20));
        }

        [Test]
        public void NotConnected_IsHonest_NoRank_NoFakeRows()
        {
            LeaderboardResult r = null;
            new NotConnectedLeaderboard().Fetch(LeaderboardScope.World, "TR", new LeaderboardEntry { Score = 600, Rank = 5 }, x => r = x);
            Assert.AreEqual(LeaderboardStatus.NotConnected, r.Status);
            Assert.AreEqual(0, r.Top.Count, "sahte satır yok");
            Assert.AreEqual(0, r.Self.Rank, "sıra bilinmez");
            Assert.IsFalse(r.DevSample);
            new NotConnectedLeaderboard().Fetch(LeaderboardScope.Country, null, null, x => r = x);
            Assert.AreEqual(LeaderboardStatus.NoCountry, r.Status, "ülke uydurulmaz");
        }

        [Test]
        public void DevSample_IsLabeled_Top10Complete()
        {
            LeaderboardResult r = null;
            new DevSampleLeaderboard().Fetch(LeaderboardScope.World, "TR", new LeaderboardEntry { Score = 500 }, x => r = x);
            Assert.IsTrue(r.DevSample, "örnek veri işaretli");
            Assert.AreEqual(10, r.Top.Count);
            for (int i = 0; i < 10; i++) Assert.AreEqual(i + 1, r.Top[i].Rank);
            for (int i = 1; i < 10; i++) Assert.GreaterOrEqual(r.Top[i - 1].Score, r.Top[i].Score, "puana göre sıralı");
        }
    }
}
