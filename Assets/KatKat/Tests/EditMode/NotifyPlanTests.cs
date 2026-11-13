using System;
using System.Linq;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Yerel bildirim planı (2026-11-13): kalpler doldu, günlük bulmaca, seni özledik; sessiz saatler.</summary>
    public class NotifyPlanTests
    {
        private static DateTime D(int day, int h, int m = 0) => new DateTime(2026, 11, day, h, m, 0);

        [Test]
        public void Daily_TodayAt18_IfNotWonAndBefore18_ElseTomorrow()
        {
            var a = NotifyPlan.Build(D(13, 10), null, false);
            Assert.AreEqual(D(13, 18), a.First(p => p.Kind == NotifyKind.Daily).At);
            var b = NotifyPlan.Build(D(13, 10), null, true);
            Assert.AreEqual(D(14, 18), b.First(p => p.Kind == NotifyKind.Daily).At, "bugünkü kazanıldı → yarın");
            var c = NotifyPlan.Build(D(13, 19), null, false);
            Assert.AreEqual(D(14, 18), c.First(p => p.Kind == NotifyKind.Daily).At, "18:00 geçti → yarın");
        }

        [Test]
        public void HeartsFull_AtRefillTime_ShiftedOutOfQuietHours_SkippedIfFullOrNear()
        {
            var a = NotifyPlan.Build(D(13, 10), D(13, 12, 30), true);
            Assert.AreEqual(D(13, 12, 30), a.First(p => p.Kind == NotifyKind.HeartsFull).At);
            var night = NotifyPlan.Build(D(13, 21), D(13, 23, 10), true);
            Assert.AreEqual(D(14, 9), night.First(p => p.Kind == NotifyKind.HeartsFull).At, "22:00 sonrası → 09:00");
            var early = NotifyPlan.Build(D(13, 3), D(13, 5), true);
            Assert.AreEqual(D(13, 9), early.First(p => p.Kind == NotifyKind.HeartsFull).At, "09:00 öncesi → 09:00");
            Assert.IsFalse(NotifyPlan.Build(D(13, 10), null, true).Any(p => p.Kind == NotifyKind.HeartsFull), "kalpler dolu");
            Assert.IsFalse(NotifyPlan.Build(D(13, 10), D(13, 10, 3), true).Any(p => p.Kind == NotifyKind.HeartsFull), "5 dk'dan yakın");
        }

        [Test]
        public void HeartsFull_DroppedWhenWithinTwoHoursOfDaily()
        {
            var a = NotifyPlan.Build(D(13, 10), D(13, 17, 0), false);
            Assert.IsFalse(a.Any(p => p.Kind == NotifyKind.HeartsFull), "günlük 18:00'e 1 saat: kalp bildirimi düşer");
            Assert.IsTrue(a.Any(p => p.Kind == NotifyKind.Daily));
        }

        [Test]
        public void Miss_At3And7Days_18h_Sorted()
        {
            var a = NotifyPlan.Build(D(13, 10), D(13, 12), false);
            Assert.AreEqual(D(16, 18), a.First(p => p.Kind == NotifyKind.Miss3).At);
            Assert.AreEqual(D(20, 18), a.First(p => p.Kind == NotifyKind.Miss7).At);
            for (int i = 1; i < a.Count; i++) Assert.LessOrEqual(a[i - 1].At, a[i].At, "zamana göre sıralı");
            Assert.AreEqual(4, a.Count);
        }

        [Test]
        public void HeartsFullUnix_FromMissingAndStamp()
        {
            var m = new MetaSave { heartsMissing = 3, heartsStamp = 1000 };
            Assert.AreEqual(1000 + 3L * Hearts.RegenSeconds, NotifyPlan.HeartsFullUnix(m, 1000));
            var full = new MetaSave();
            Assert.IsNull(NotifyPlan.HeartsFullUnix(full, 1000));
        }
    }
}
