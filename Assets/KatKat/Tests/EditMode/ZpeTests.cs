using System;
using System.Collections.Generic;
using System.IO;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>v10 P5: ZPE (Zekâ Performans Endeksi) hesap kuralları — plan §7 test listesi.</summary>
    public class ZpeTests
    {
        private static readonly ZpeConfig C = new ZpeConfig();
        private static readonly ZpeLevelRef Easy = new ZpeLevelRef { Id = "e", Steps = 4, Limit = 10, Bot = 0.7 };
        private static readonly ZpeLevelRef Hard = new ZpeLevelRef { Id = "h", Steps = 4, Limit = 10, Bot = 0.05 };

        private static ZpeRecord Win(string id = "e", int used = 4, bool first = true, bool assisted = false, int bonus = 0, float sec = 20f) =>
            new ZpeRecord { levelId = id, success = true, firstAttempt = first, movesUsed = used, moveLimit = 10 + bonus, moveBonus = bonus, assisted = assisted, seconds = sec };

        [Test]
        public void HardLevelWin_ScoresHigherThanEasy()
        {
            Assert.Greater(Zpe.Score(Win("h"), Hard, C).Total, Zpe.Score(Win("e"), Easy, C).Total);
        }

        [Test]
        public void Efficiency_RefStepsIsMax_LimitIsMin_BonusMovesZero()
        {
            Assert.AreEqual(1.0, Zpe.Score(Win(used: 3), Easy, C).Efficiency, 1e-9);
            Assert.AreEqual(1.0, Zpe.Score(Win(used: 4), Easy, C).Efficiency, 1e-9);
            Assert.Greater(Zpe.Score(Win(used: 6), Easy, C).Efficiency, Zpe.Score(Win(used: 9), Easy, C).Efficiency);
            Assert.AreEqual(0.0, Zpe.Score(Win(used: 11, bonus: 1, assisted: true), Easy, C).Efficiency, 1e-9, "ek hamleyle geçmek verim saymaz");
        }

        [Test]
        public void Failure_ScoresZero_ReplayLessThanFirstTry()
        {
            Assert.AreEqual(0.0, Zpe.Score(new ZpeRecord { levelId = "e", success = false, movesUsed = 10, moveLimit = 10 }, Easy, C).Total);
            Assert.Greater(Zpe.Score(Win(first: true), Easy, C).Total, Zpe.Score(Win(first: false), Easy, C).Total);
        }

        [Test]
        public void Speed_FasterIsBetter_Capped()
        {
            Assert.AreEqual(1.0, Zpe.Score(Win(sec: 5f), Easy, C).Time, 1e-9);
            Assert.Greater(Zpe.Score(Win(sec: 20f), Easy, C).Time, Zpe.Score(Win(sec: 120f), Easy, C).Time);
        }

        [Test]
        public void Assisted_NoRankingAdvantage()
        {
            var plain = Zpe.Score(Win(), Easy, C).Total;
            var assisted = Zpe.Score(Win(assisted: true), Easy, C).Total;
            var bonus = Zpe.Score(Win(used: 11, bonus: 1, assisted: true), Easy, C).Total;
            Assert.Less(assisted, plain);
            Assert.Less(bonus, assisted);
        }

        [Test]
        public void MissingReference_UsesNeutralDefaults()
        {
            var p = Zpe.Score(Win(), null, C);
            Assert.Greater(p.Total, 0);
            Assert.AreEqual(0.35 + 0.65 * 0.5, p.Difficulty, 1e-9);
        }

        [Test]
        public void V11_ScoreOutOf100_FromFirstGame()
        {
            var meta = new MetaSave();
            Assert.IsTrue(Zpe.Compute(meta, _ => Easy, C).Calibrating, "hiç oyun yok");
            Zpe.Add(meta, Win(), _ => Easy, C);
            var r = Zpe.Compute(meta, _ => Easy, C);
            Assert.IsFalse(r.Calibrating, "ilk oyundan itibaren puan görünür");
            Assert.AreEqual(1, r.Count);
            Assert.That(r.Score, Is.InRange(0, 100));
            Assert.AreEqual((int)Math.Round(Zpe.Score(Win(), Easy, C).Total * 100), r.Score);
        }

        [Test]
        public void V11_PlainAverageOfAllGames_BeyondHistoryCap()
        {
            var meta = new MetaSave();
            double sum = 0;
            int games = Zpe.HistoryCap + 40;
            for (int i = 0; i < games; i++)
            {
                var rec = i % 4 == 0 ? new ZpeRecord { levelId = "e", success = false, movesUsed = 10, moveLimit = 10 } : Win(used: 4 + i % 5);
                sum += Zpe.Score(rec, Easy, C).Total;
                Zpe.Add(meta, rec, _ => Easy, C);
            }
            var r = Zpe.Compute(meta, _ => Easy, C);
            Assert.AreEqual(games, r.Count, "bütün oynanan oyunlar sayılır");
            Assert.AreEqual(Zpe.HistoryCap, meta.zpeHistory.Count, "ham girdi sınırlı");
            Assert.AreEqual((int)Math.Round(sum / games * 100), r.Score, "düz ortalama × 100");
        }

        [Test]
        public void V11_OldSaveWithoutTotals_SeedsFromHistory()
        {
            var meta = new MetaSave();
            for (int i = 0; i < 5; i++) meta.zpeHistory.Add(Win());
            var r = Zpe.Compute(meta, _ => Easy, C);
            Assert.AreEqual(5, r.Count);
            Assert.AreEqual((int)Math.Round(Zpe.Score(Win(), Easy, C).Total * 100), r.Score);
        }

        [Test]
        public void Stability_SingleBadLevelMovesScoreLittle()
        {
            var meta = new MetaSave();
            for (int i = 0; i < 40; i++) Zpe.Add(meta, Win(), _ => Easy, C);
            int before = Zpe.Compute(meta, _ => Easy, C).Score;
            Zpe.Add(meta, new ZpeRecord { levelId = "e", success = false, movesUsed = 10, moveLimit = 10 }, _ => Easy, C);
            int after = Zpe.Compute(meta, _ => Easy, C).Score;
            Assert.Less(before - after, 8, "41 oyunda tek kötü bölüm ~1/41 etkiler");
            Assert.Greater(before, after);
        }

        [Test]
        public void RecentLevelsWeighMore_TrendSign()
        {
            var h = new List<ZpeRecord>();
            for (int i = 0; i < 20; i++) h.Add(Win(used: 9, first: false, sec: 90f));
            for (int i = 0; i < 10; i++) h.Add(Win(used: 4, first: true, sec: 15f));
            var r = Zpe.Compute(h, _ => Easy, C);
            Assert.Greater(r.Trend, 0, "son bölümler daha iyi → artış");
            Assert.AreEqual(1.0, r.WinRate, 1e-9);
        }

        [Test]
        public void History_CappedAndVersioned()
        {
            var meta = new MetaSave();
            for (int i = 0; i < Zpe.HistoryCap + 10; i++) Zpe.Add(meta, Win());
            Assert.AreEqual(Zpe.HistoryCap, meta.zpeHistory.Count);
            Assert.AreEqual(Zpe.AlgoVersion, meta.zpeHistory[0].algo);
        }

        [Test]
        public void ShippedReferenceAndConfig_Parse()
        {
            var res = Path.GetDirectoryName(ProductionLevelTests.LevelsDir());
            var cfg = ZpeConfig.Parse(File.ReadAllText(Path.Combine(res, "Zpe", "zpe_config.json")));
            // v3 (vr1 02_ZPE): hız ağırlığı 0; kalan dördü kendi toplamına (0,90) bölünerek kullanılır.
            Assert.AreEqual(0.90, cfg.WEfficiency + cfg.WDifficulty + cfg.WFirstTry + cfg.WTime + cfg.WNoAssist, 1e-9);
            Assert.AreEqual(0.0, cfg.WTime, 1e-9);
            var refs = Zpe.ParseRefs(File.ReadAllText(Path.Combine(res, "Zpe", "level_ref.json")));
            Assert.AreEqual(1000, refs.Count);
            foreach (var r in refs.Values)
            {
                Assert.That(r.Bot, Is.InRange(0.0, 1.0), r.Id);
                Assert.That(r.Steps, Is.InRange(1, r.Limit), r.Id + " çözüm ≤ sınır");
            }
        }
    }
}
