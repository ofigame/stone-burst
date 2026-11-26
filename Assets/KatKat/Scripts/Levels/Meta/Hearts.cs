using System;

namespace KatKat.Meta
{
    /// <summary>
    /// Kalp (can) sistemi (kullanıcı, 2026-11-09: "enerji olsun kalp olsun ... 5 ten az ise yarım saate bir +1 eklenecek
    /// ... reklam izleyerek 5 kalbi fullesin"). Saf mantık; saat dışarıdan (Unix saniye) verilir.
    /// - En çok 5 kalp; eksik kalp her 30 dakikada bir dolar (sayaç ilk eksilmede başlar).
    /// - Kalp yalnız ana bölümde, deneme kaybedilip bırakılınca (yeniden başlat / çık) gider; kazanınca gitmez.
    ///   Geri al ya da reklamla devam ederken gitmez. İlk <see cref="FreeLevels"/> bölüm kalpsizdir.
    /// - Ödüllü reklam 1 kalp verir (2026-11-26: önerildi, kullanıcı "Tamam öyle yapalım"; önce bütün kalpler).
    /// Kayıtta eksik kalp sayısı tutulur (eski kayıtta 0 = dolu).
    /// </summary>
    public static class Hearts
    {
        public const int Max = 5;
        public const int RegenSeconds = 30 * 60;
        public const int FreeLevels = 20;
        /// <summary>Ödüllü reklamın verdiği kalp (2026-11-26).</summary>
        public const int AdHearts = 1;

        /// <summary>0 tabanlı bölüm sırası kalp kullanır mı.</summary>
        public static bool LevelUses(int index) => index >= FreeLevels;

        /// <summary>Geçen süreyi kalbe çevirir (saat geri alınmışsa sayaç şimdiden başlar).</summary>
        public static void Settle(MetaSave m, long now)
        {
            if (m.heartsMissing <= 0) { m.heartsMissing = 0; m.heartsStamp = 0; return; }
            if (m.heartsMissing > Max) m.heartsMissing = Max;
            if (m.heartsStamp <= 0 || m.heartsStamp > now) m.heartsStamp = now;
            long n = (now - m.heartsStamp) / RegenSeconds;
            if (n <= 0) return;
            int add = (int)Math.Min(n, m.heartsMissing);
            m.heartsMissing -= add;
            m.heartsStamp = m.heartsMissing == 0 ? 0 : m.heartsStamp + (long)add * RegenSeconds;
        }

        public static int Count(MetaSave m, long now)
        {
            Settle(m, now);
            if (Unlimited(m, now)) return Max;
            return Max - m.heartsMissing;
        }

        /// <summary>Sonraki kalbe kalan saniye; dolu ise 0.</summary>
        public static int SecondsToNext(MetaSave m, long now)
        {
            Settle(m, now);
            if (m.heartsMissing == 0 || Unlimited(m, now)) return 0;
            return (int)Math.Max(0, RegenSeconds - (now - m.heartsStamp));
        }

        /// <summary>Bir kalp harcar; kalp yoksa false.</summary>
        public static bool Spend(MetaSave m, long now)
        {
            Settle(m, now);
            if (Unlimited(m, now)) return true; // sınırsız can: harcanmaz
            if (m.heartsMissing >= Max) return false;
            if (m.heartsMissing == 0) m.heartsStamp = now;
            m.heartsMissing++;
            return true;
        }

        /// <summary>Sınırsız can süresi içinde mi (2026-11-20; mağazadan SB Gold ile).</summary>
        public static bool Unlimited(MetaSave m, long now) => m.heartsInfiniteUntil > now;

        /// <summary>Sınırsız canın kalan saniyesi; yoksa 0.</summary>
        public static int UnlimitedSeconds(MetaSave m, long now) => (int)Math.Max(0, m.heartsInfiniteUntil - now);

        /// <summary>Sınırsız can ekler (süre varsa üstüne biner) ve kalpleri doldurur.</summary>
        public static void AddUnlimited(MetaSave m, long now, int seconds)
        {
            m.heartsInfiniteUntil = Math.Max(m.heartsInfiniteUntil, now) + seconds;
            Refill(m);
        }

        /// <summary>Kalp ekler (en çok dolu olana kadar); dolunca sayaç durur, değilse kalan süre korunur.</summary>
        public static void Gain(MetaSave m, long now, int n)
        {
            Settle(m, now);
            m.heartsMissing = Math.Max(0, m.heartsMissing - n);
            if (m.heartsMissing == 0) m.heartsStamp = 0;
        }

        public static void Refill(MetaSave m)
        {
            m.heartsMissing = 0;
            m.heartsStamp = 0;
        }

        /// <summary>"29:59" biçimi.</summary>
        public static string Clock(int seconds)
        {
            if (seconds < 0) seconds = 0;
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
    }
}
