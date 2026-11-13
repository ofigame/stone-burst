using System;
using System.Collections.Generic;

namespace KatKat.Meta
{
    public enum NotifyKind { HeartsFull = 1, Daily = 2, Miss3 = 3, Miss7 = 4 }

    /// <summary>Planlanacak tek yerel bildirim (yerel saat).</summary>
    public struct PlannedNotify
    {
        public NotifyKind Kind;
        public DateTime At;
        public override string ToString() => Kind + "@" + At.ToString("yyyy-MM-dd HH:mm");
    }

    /// <summary>
    /// Yerel bildirim planı (kullanıcı, 2026-11-13: "oyunumuza bildirimler ekleyelim"; seçilen türler: kalpler doldu,
    /// yeni günlük bulmaca, seni özledik). Uygulama arka plana geçerken hesaplanır, açılınca hepsi iptal edilir.
    /// Kurallar:
    /// - Sessiz saatler 22:00–09:00: bu aralığa düşen bildirim ertesi 09:00'a kayar.
    /// - Kalpler doldu: eksik kalp varsa dolma anında (5 dk'dan yakınsa gönderilmez).
    /// - Günlük bulmaca: <see cref="DailyHour"/>'da; bugünkü kazanılmadıysa ve saat geçmediyse bugün, yoksa yarın.
    /// - Seni özledik: son oynamadan 3 ve 7 gün sonra <see cref="DailyHour"/>'da (en çok 2, sonra durur).
    /// - Birbirine 2 saatten yakın iki bildirimden kalp bildirimi düşer (günlük hatırlatma zaten oyuna çağırır).
    /// </summary>
    public static class NotifyPlan
    {
        public const int DailyHour = 18;
        public const int QuietStart = 22, QuietEnd = 9;
        public static readonly TimeSpan MinGap = TimeSpan.FromHours(2);

        public static DateTime OutOfQuiet(DateTime t)
        {
            if (t.Hour >= QuietStart) return t.Date.AddDays(1).AddHours(QuietEnd);
            if (t.Hour < QuietEnd) return t.Date.AddHours(QuietEnd);
            return t;
        }

        /// <param name="now">Şu an (yerel).</param>
        /// <param name="heartsFullAt">Kalplerin dolacağı an (yerel); dolu ise null.</param>
        /// <param name="dailyWonToday">Bugünkü günlük bulmaca kazanıldı mı.</param>
        public static List<PlannedNotify> Build(DateTime now, DateTime? heartsFullAt, bool dailyWonToday)
        {
            var list = new List<PlannedNotify>();

            var todayDaily = now.Date.AddHours(DailyHour);
            var daily = !dailyWonToday && now < todayDaily ? todayDaily : todayDaily.AddDays(1);
            list.Add(new PlannedNotify { Kind = NotifyKind.Daily, At = daily });

            if (heartsFullAt.HasValue && heartsFullAt.Value > now.AddMinutes(5))
            {
                var h = OutOfQuiet(heartsFullAt.Value);
                if ((h - daily).Duration() >= MinGap) list.Add(new PlannedNotify { Kind = NotifyKind.HeartsFull, At = h });
            }

            list.Add(new PlannedNotify { Kind = NotifyKind.Miss3, At = now.Date.AddDays(3).AddHours(DailyHour) });
            list.Add(new PlannedNotify { Kind = NotifyKind.Miss7, At = now.Date.AddDays(7).AddHours(DailyHour) });
            list.Sort((a, b) => a.At.CompareTo(b.At));
            return list;
        }

        /// <summary>Kalplerin dolacağı Unix saniyesi (dolu ise null). Hearts kurallarıyla: her eksik kalp RegenSeconds.</summary>
        public static long? HeartsFullUnix(MetaSave m, long now)
        {
            Hearts.Settle(m, now);
            if (m.heartsMissing <= 0) return null;
            return m.heartsStamp + (long)m.heartsMissing * Hearts.RegenSeconds;
        }
    }
}
