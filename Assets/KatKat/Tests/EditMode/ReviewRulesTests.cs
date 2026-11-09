using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Uygulama içi puan isteği kuralları (kullanıcı, 2026-11-09).</summary>
    public class ReviewRulesTests
    {
        private const long Day = 86400L, T = 2_000_000_000L;

        [Test]
        public void AsksOnlyAfterEnoughLevels_ThreeStars_Unassisted()
        {
            Assert.IsFalse(ReviewRules.ShouldAsk(14, 3, false, 0, 0, T), "15 bölümden önce yok");
            Assert.IsTrue(ReviewRules.ShouldAsk(15, 3, false, 0, 0, T));
            Assert.IsFalse(ReviewRules.ShouldAsk(30, 2, false, 0, 0, T), "3 yıldız değil");
            Assert.IsFalse(ReviewRules.ShouldAsk(30, 3, true, 0, 0, T), "reklam/geri alma/ipucu ile kazanım");
        }

        [Test]
        public void GapAndCap()
        {
            Assert.IsFalse(ReviewRules.ShouldAsk(30, 3, false, 1, T, T + 119 * Day), "120 günden önce ikinci istek yok");
            Assert.IsTrue(ReviewRules.ShouldAsk(30, 3, false, 1, T, T + 120 * Day));
            Assert.IsFalse(ReviewRules.ShouldAsk(99, 3, false, ReviewRules.MaxAsks, T, T + 999 * Day), "en çok 3");
        }
    }
}
