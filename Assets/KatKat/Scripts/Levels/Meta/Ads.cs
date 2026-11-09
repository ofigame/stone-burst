using System;
using System.Collections.Generic;
using System.Globalization;
using KatKat.Levels;

namespace KatKat.Meta
{
    /// <summary>
    /// v12 reklam altyapısı (kullanıcı, 2026-11-08): arabulucu AdMob (AdMob + Unity Ads), reklamsız paket yok, 13+.
    /// Yerleşimler: günlük giriş (ödüllü), kayıp kartında +3 hamle (ödüllü), zafer kartında Devam (geçiş).
    /// Gerçek SDK bağlanana kadar oyunda TEST reklam ekranı (<c>TestAdScreen</c>) çalışır.
    /// </summary>
    public static class AdPlacements
    {
        public const string DailyEntry = "daily_entry";
        public const string ExtraMoves = "extra_moves";
        public const string LevelComplete = "level_complete";
        public const string ExtraLife = "extra_life"; // hak sistemiyle gelecek
        /// <summary>Oyun içi ipucu (kullanıcı, 2026-11-08): ödüllü reklamla 1 ipucu, bölümün yıldızından 1 düşer.</summary>
        public const string Hint = "hint";
    }

    /// <summary>Geçiş (tam ekran, ödülsüz) reklam adaptörü. Geri çağrı: gösterildi mi.</summary>
    public interface IInterstitialAds
    {
        void ShowInterstitial(string placement, Action<bool> done);
    }

    /// <summary>Gerçek reklam ağı (ödüllü + geçiş). Yükleme yoksa ödüllü <see cref="AdOutcome.Failed"/> döner.</summary>
    public interface IAdNetwork : IRewardedAds, IInterstitialAds
    {
    }

    /// <summary>
    /// Reklam yapılandırması (Resources/Ads/ad_config.json; ileride uzaktan ayar). Değerler ana plan §9 ve kullanıcı
    /// kararları: 2026-11-09 seyreltildi (kullanıcı: "kullanıcıyı çok bunaltmasın"): ilk 10 bölüm geçiş yok, 4 bölümde bir, en az 240 sn ara, ödüllüden sonra 120 sn geçiş yok, oturumda en çok 8,
    /// reklam arama 5 sn, kayıpta reklamla +1 hamle ve 1 taş yerleştirme (bölüm başına 1).
    /// </summary>
    public sealed class AdConfig
    {
        public int Version = 1;
        public float SearchSeconds = 5f;
        public int ExtraMoves = 1; // 2026-11-09: +1 hamle ve 1 taş yerleştirme (kullanıcı)
        public int ExtraMovesPerLevel = 1;
        public int InterstitialFirstLevel = 11;
        public int InterstitialEveryLevels = 4;
        public float InterstitialMinSeconds = 240f;
        public float InterstitialAfterRewardedSeconds = 120f;
        public int InterstitialSessionCap = 8;

        public static AdConfig Parse(string json)
        {
            var c = new AdConfig();
            if (string.IsNullOrEmpty(json)) return c;
            if (!(MiniJson.Parse(json) is Dictionary<string, object> o)) throw new FormatException("ad_config: kök nesne olmalı");
            double D(string k, double d) => o.TryGetValue(k, out var v) && v != null ? Convert.ToDouble(v, CultureInfo.InvariantCulture) : d;
            c.Version = (int)D("version", c.Version);
            c.SearchSeconds = (float)D("searchSeconds", c.SearchSeconds);
            c.ExtraMoves = (int)D("extraMoves", c.ExtraMoves);
            c.ExtraMovesPerLevel = (int)D("extraMovesPerLevel", c.ExtraMovesPerLevel);
            c.InterstitialFirstLevel = (int)D("interstitialFirstLevel", c.InterstitialFirstLevel);
            c.InterstitialEveryLevels = Math.Max(1, (int)D("interstitialEveryLevels", c.InterstitialEveryLevels));
            c.InterstitialMinSeconds = (float)D("interstitialMinSeconds", c.InterstitialMinSeconds);
            c.InterstitialAfterRewardedSeconds = (float)D("interstitialAfterRewardedSeconds", c.InterstitialAfterRewardedSeconds);
            c.InterstitialSessionCap = (int)D("interstitialSessionCap", c.InterstitialSessionCap);
            if (c.SearchSeconds < 0 || c.ExtraMoves < 0) throw new FormatException("ad_config: negatif değer");
            return c;
        }
    }

    /// <summary>
    /// Geçiş reklamı kuralları (saf mantık, saat dışarıdan): yalnız zafer kartında Devam'da sorulur; ilk
    /// <see cref="AdConfig.InterstitialFirstLevel"/> öncesi bölümlerde yok; son gösterimden bu yana en az
    /// <see cref="AdConfig.InterstitialEveryLevels"/> bölüm ve <see cref="AdConfig.InterstitialMinSeconds"/> sn;
    /// ödüllü reklamdan hemen sonra yok; oturum üst sınırı.
    /// </summary>
    public sealed class InterstitialPolicy
    {
        private readonly AdConfig _c;
        private int _levelsSince;
        private double _lastShown = double.NegativeInfinity, _lastRewarded = double.NegativeInfinity;
        public int ShownThisSession { get; private set; }

        public InterstitialPolicy(AdConfig c) { _c = c ?? new AdConfig(); }

        /// <summary>Normal bölüm kazanıldı (1 tabanlı bölüm numarası).</summary>
        public void LevelCompleted(int levelNumber)
        {
            if (levelNumber >= _c.InterstitialFirstLevel) _levelsSince++;
        }

        public void RewardedShown(double now) => _lastRewarded = now;

        public bool ShouldShow(int levelNumber, double now)
        {
            if (levelNumber < _c.InterstitialFirstLevel) return false;
            if (ShownThisSession >= _c.InterstitialSessionCap) return false;
            if (_levelsSince < _c.InterstitialEveryLevels) return false;
            if (now - _lastShown < _c.InterstitialMinSeconds) return false;
            if (now - _lastRewarded < _c.InterstitialAfterRewardedSeconds) return false;
            return true;
        }

        public void Shown(double now)
        {
            _lastShown = now;
            _levelsSince = 0;
            ShownThisSession++;
        }
    }
}
