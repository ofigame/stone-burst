using System.IO;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>
    /// vr1 02_ZPE (kullanıcı onayı 2026-11-11): v3 yapılandırması — hız bileşeni yok; puan son 50 farklı normal bölümden,
    /// bölüm başına penceredeki en iyi sonuç; aynı bölümü tekrar oynamak pencereyi doldurmaz; eski (v2) toplam donuk kalır.
    /// </summary>
    public class ZpeV3Tests
    {
        private static ZpeConfig V3()
        {
            var path = Path.Combine(Path.GetDirectoryName(ProductionLevelTests.LevelsDir()), "Zpe", "zpe_config.json");
            return ZpeConfig.Parse(File.ReadAllText(path));
        }

        private static ZpeRecord Rec(string id, bool success = true, float sec = 20f, int used = 4) =>
            new ZpeRecord { levelId = id, success = success, firstAttempt = true, movesUsed = used, moveLimit = 10, seconds = sec };

        [Test]
        public void Config_IsV3_NoTime_Distinct50()
        {
            var c = V3();
            Assert.AreEqual(3, c.Version);
            Assert.AreEqual(0.0, c.WTime, 1e-9, "hız bileşeni kaldırıldı");
            Assert.IsTrue(c.Distinct);
            Assert.AreEqual(50, c.Window);
        }

        [Test]
        public void Time_DoesNotAffectScore()
        {
            var c = V3();
            Assert.AreEqual(Zpe.Score(Rec("a", sec: 5f), null, c).Total, Zpe.Score(Rec("a", sec: 500f), null, c).Total, 1e-9);
        }

        /// <summary>Aynı kolay bölümü tekrar oynamak endeksi sınırsız artırmaz: bölüm bir kez, en iyi sonucuyla sayılır.</summary>
        [Test]
        public void Replays_CountOnce_BestResult()
        {
            var c = V3();
            var meta = new MetaSave();
            Zpe.Add(meta, Rec("hard", success: false), null, c);
            for (int i = 0; i < 30; i++) Zpe.Add(meta, Rec("easy"), null, c);
            var z = Zpe.Compute(meta, null, c);
            Assert.AreEqual(2, z.Count, "iki farklı bölüm");
            double easy = Zpe.Score(Rec("easy"), null, c).Total;
            Assert.AreEqual((int)System.Math.Round(easy / 2 * 100), z.Score, "kayıp bölüm 0 + kolay bölüm bir kez");
            Zpe.Add(meta, Rec("hard", used: 9), null, c); // sonradan kazanıldı: en iyi sonuç kullanılır
            Assert.Greater(Zpe.Compute(meta, null, c).Score, z.Score);
        }

        [Test]
        public void Window_Last50DistinctLevels()
        {
            var c = V3();
            var meta = new MetaSave();
            for (int i = 0; i < 60; i++) Zpe.Add(meta, Rec("l" + i, success: i >= 10), null, c);
            var z = Zpe.Compute(meta, null, c);
            Assert.AreEqual(50, z.Count);
            Assert.AreEqual((int)System.Math.Round(Zpe.Score(Rec("x"), null, c).Total * 100), z.Score, "eski 10 kayıp pencere dışında");
        }

        /// <summary>Eski kayıt: v2 toplamı silinmez ve yeni kayıtlarla karışmaz (LegacyScore donuk).</summary>
        [Test]
        public void Legacy_FrozenAndKept()
        {
            var meta = new MetaSave { zpeSum = 7.0, zpeGames = 10 };
            var c = V3();
            Zpe.Add(meta, Rec("n1"), null, c);
            Assert.AreEqual(7.0, meta.zpeSum, 1e-9);
            Assert.AreEqual(10, meta.zpeGames);
            Assert.AreEqual(70, Zpe.Compute(meta, null, c).LegacyScore);
        }
    }
}
