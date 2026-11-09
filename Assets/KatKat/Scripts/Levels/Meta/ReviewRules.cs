namespace KatKat.Meta
{
    /// <summary>
    /// Uygulama içi puan isteği kuralları (kullanıcı, 2026-11-09: "kurallar çerçevesinde uygulama puanı da isteyelim").
    /// Apple App Store Yönergeleri 1.1.7 / 5.6.1: yalnız sistemin puan penceresi (SKStoreReviewController), özel
    /// soru ekranı ya da ödül yok; sistem yılda en çok 3 kez gösterir. Bizim ek sınırlarımız:
    /// - en az <see cref="MinCompleted"/> bölüm bitmiş olmalı (oyunu tanımış oyuncu);
    /// - yalnız yardımsız (reklam, geri alma, ipucu yok) 3 yıldızlı bir kazanımdan sonra (mutlu an);
    /// - iki istek arası en az <see cref="GapDays"/> gün; toplam en çok <see cref="MaxAsks"/> istek.
    /// Saf mantık (test edilebilir).
    /// </summary>
    public static class ReviewRules
    {
        public const int MinCompleted = 15;
        public const int GapDays = 120;
        public const int MaxAsks = 3;

        public static bool ShouldAsk(int completed, int stars, bool assisted, int asks, long last, long now)
        {
            if (completed < MinCompleted || stars < 3 || assisted) return false;
            if (asks >= MaxAsks) return false;
            if (asks > 0 && now - last < GapDays * 86400L) return false;
            return true;
        }
    }
}
