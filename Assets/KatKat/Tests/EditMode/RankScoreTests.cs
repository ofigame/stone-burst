using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Sıralama puanı (kullanıcı kararı 2026-11-19, "A ile yap"): toplam sıralama yıldızı + ZPE (0–100).</summary>
    public class RankScoreTests
    {
        [Test]
        public void Formula_StarsPlusZpe()
        {
            Assert.AreEqual(1090, RankScore.Compute(70, 1020));
            Assert.AreEqual(0, RankScore.Compute(0, 0));
            Assert.AreEqual(100, RankScore.Compute(150, 0), "ZPE en çok 100");
            Assert.AreEqual(30, RankScore.Compute(-5, 30), "eksi ZPE sayılmaz");
        }

        [Test]
        public void Beginner_CannotPass_Veteran()
        {
            // Kullanıcının örneği: kusursuz yeni başlayan (20 bölüm, 60 yıldız, ZPE 100) vs 400. bölümdeki oyuncu
            // (ortalama 2 yıldız, ZPE 40). Eski formülde yeni başlayan öndeydi.
            Assert.Less(RankScore.Compute(100, 60), RankScore.Compute(40, 800));
            // Yıldızları eşit olanları ZPE ayırır.
            Assert.Greater(RankScore.Compute(80, 500), RankScore.Compute(60, 500));
        }

        [Test]
        public void Eligibility_Needs10Levels()
        {
            Assert.IsFalse(RankScore.Eligible(9));
            Assert.IsTrue(RankScore.Eligible(10));
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
