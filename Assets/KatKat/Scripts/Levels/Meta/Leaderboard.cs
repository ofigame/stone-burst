using System;
using System.Collections.Generic;

namespace KatKat.Meta
{
    public enum LeaderboardScope { World, Country }

    /// <summary>Bir sıralama kaydı (sunucudan; oyuncunun kendi kaydı da aynı biçimde).</summary>
    public sealed class LeaderboardEntry
    {
        public int Rank;            // 1 tabanlı; 0 = sıralama yok
        public string Name = "";
        public string AvatarId = ""; // ileride oyuncu avatarı; boşsa baş harf
        public int Zpe;             // 0–100
        public int Level;           // ulaşılan bölüm (1 tabanlı)
        public int Stars;           // sıralama yıldızı (reklamsız)
        public int Score;           // sıralama puanı 0–1000
        public bool IsSelf;
    }

    public enum LeaderboardStatus { Ready, Empty, Error, NoCountry, NotConnected }

    /// <summary>Bir sekmenin sonucu: ilk 10 + oyuncunun kendi kaydı (Rank 0: henüz sıralaması yok).</summary>
    public sealed class LeaderboardResult
    {
        public LeaderboardScope Scope;
        public string Country;      // ISO 3166-1 alfa-2 (Country kapsamında)
        public LeaderboardStatus Status;
        public string Error;
        public readonly List<LeaderboardEntry> Top = new List<LeaderboardEntry>();
        public LeaderboardEntry Self;
        /// <summary>Geliştirme örnek verisi mi (ekranda açıkça etiketlenir; canlı veri değildir).</summary>
        public bool DevSample;
    }

    /// <summary>
    /// Sıralama verisi sağlayıcısı (v12; sunucu: Firebase planlanıyor). İstemci puanı yalnız gönderir; sıra ve ilk 10
    /// sunucudan gelir. Uygulamalar: <see cref="NotConnectedLeaderboard"/> (sunucu yok — dürüst durum),
    /// <see cref="DevSampleLeaderboard"/> (yalnız geliştirme), ileride Firebase.
    /// </summary>
    public interface ILeaderboardProvider
    {
        void Fetch(LeaderboardScope scope, string country, LeaderboardEntry self, Action<LeaderboardResult> done);
    }

    /// <summary>
    /// Sıralama puanı (kullanıcı kararı 2026-11-08): 0–1000 = 10 × (0,5 × ZPE + 0,3 × bölüm ilerlemesi + 0,2 × yıldız
    /// oranı), her bileşen 0–100. Bölüm ilerlemesi = ulaşılan bölüm / toplam bölüm. Yıldız oranı = sıralama yıldızı
    /// (reklamla kazanılanlar hariç) / (3 × tamamlanan bölüm). Sıralamaya giriş: en az <see cref="MinLevels"/> bölüm.
    /// </summary>
    public static class RankScore
    {
        public const double WZpe = 0.5, WLevel = 0.3, WStars = 0.2;
        public const int MinLevels = 20;

        public static bool Eligible(int completedLevels) => completedLevels >= MinLevels;

        public static int Compute(int zpe, int reachedLevel, int totalLevels, int rankStars, int completedLevels)
        {
            double z = Clamp(zpe, 0, 100);
            double lv = totalLevels > 0 ? Clamp(reachedLevel * 100.0 / totalLevels, 0, 100) : 0;
            double st = completedLevels > 0 ? Clamp(rankStars * 100.0 / (3.0 * completedLevels), 0, 100) : 0;
            return (int)Math.Round(10.0 * (WZpe * z + WLevel * lv + WStars * st));
        }

        private static double Clamp(double v, double a, double b) => v < a ? a : v > b ? b : v;
    }

    /// <summary>Sunucu bağlı değil: liste yok, açıkça "bağlı değil" durumu (sahte veri yok).</summary>
    public sealed class NotConnectedLeaderboard : ILeaderboardProvider
    {
        public void Fetch(LeaderboardScope scope, string country, LeaderboardEntry self, Action<LeaderboardResult> done)
        {
            if (scope == LeaderboardScope.Country && string.IsNullOrEmpty(country))
            { done?.Invoke(new LeaderboardResult { Scope = scope, Status = LeaderboardStatus.NoCountry, Self = Unranked(self) }); return; }
            done?.Invoke(new LeaderboardResult { Scope = scope, Country = country, Status = LeaderboardStatus.NotConnected, Self = Unranked(self) });
        }

        private static LeaderboardEntry Unranked(LeaderboardEntry self)
        {
            if (self == null) return null;
            self.Rank = 0; // sunucu olmadan sıra bilinmez
            return self;
        }
    }

    /// <summary>
    /// YALNIZ GELİŞTİRME: ekran düzenini sınamak için sabit örnek veri (ekranda "Geliştirme verisi" etiketiyle).
    /// Oyuncu yayın derlemesinde bu sağlayıcıyı görmez. İsimler uydurmadır, gerçek oyuncu değildir.
    /// </summary>
    public sealed class DevSampleLeaderboard : ILeaderboardProvider
    {
        private static readonly string[] Names = { "Deniz", "Mira", "Atlas", "Luna", "Efe", "Nova", "Ada", "Leo", "Aras", "Maya", "Kuzey", "Elif" };
        public int SelfRank = 284;
        public bool Fail;

        public void Fetch(LeaderboardScope scope, string country, LeaderboardEntry self, Action<LeaderboardResult> done)
        {
            var r = new LeaderboardResult { Scope = scope, Country = country, DevSample = true };
            if (Fail) { r.Status = LeaderboardStatus.Error; r.Error = "dev"; r.Self = self; done?.Invoke(r); return; }
            if (scope == LeaderboardScope.Country && string.IsNullOrEmpty(country)) { r.Status = LeaderboardStatus.NoCountry; r.Self = self; done?.Invoke(r); return; }
            int seed = scope == LeaderboardScope.World ? 0 : 2;
            for (int i = 0; i < 10; i++)
            {
                int zpe = 92 - i * 2 - seed, lv = 940 - i * 14 - seed * 20, stars = Math.Min(3 * lv, 2600 - i * 35 - seed * 50);
                r.Top.Add(new LeaderboardEntry
                {
                    Rank = i + 1, Name = Names[(i + seed) % Names.Length], Zpe = zpe, Level = lv, Stars = stars,
                    Score = RankScore.Compute(zpe, lv, 1000, stars, lv),
                });
            }
            r.Status = LeaderboardStatus.Ready;
            if (self != null)
            {
                self.Rank = self.Score > 0 ? (scope == LeaderboardScope.World ? SelfRank : Math.Max(11, SelfRank / 9)) : 0;
                r.Self = self;
            }
            done?.Invoke(r);
        }
    }
}
