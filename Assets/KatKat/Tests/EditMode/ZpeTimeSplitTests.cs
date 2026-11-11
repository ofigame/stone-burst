using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>
    /// vr1 paketi 02_ZPE / 06_TEST_KABUL Z01–Z04: karar süresi animasyon ve öğretici süresinden ayrı sayılır; eski kayıtlar
    /// değişmeden okunur.
    /// </summary>
    public class ZpeTimeSplitTests
    {
        private static readonly ZpeConfig C = new ZpeConfig();
        private static readonly ZpeLevelRef Ref8 = new ZpeLevelRef { Id = "r", Steps = 8, Limit = 14, Bot = 0.5 };

        private static SessionOutcome Played(double decision, double animation, double tutorial = 0)
        {
            var o = new SessionOutcome { Kind = SessionKind.Normal, LevelId = "r", BaseMoveLimit = 14, MovesUsed = 8, FirstAttempt = true };
            // Kare kare: 1/60 sn adımlarla (büyük kare kırpması karar süresini bozmasın).
            for (double t = 0; t < decision - 1e-9; t += 1.0 / 60) o.Tick(PlayTime.Decision, 1.0 / 60);
            for (double t = 0; t < animation - 1e-9; t += 1.0 / 60) o.Tick(PlayTime.Animation, 1.0 / 60);
            for (double t = 0; t < tutorial - 1e-9; t += 1.0 / 60) o.Tick(PlayTime.Tutorial, 1.0 / 60);
            o.Finished = true;
            o.Success = true;
            return o;
        }

        /// <summary>Z01: aynı karar süresi, 0/5/20 sn animasyon → aynı yeni karar süresi ve aynı bölüm puanı.</summary>
        [Test]
        public void SameDecision_DifferentAnimation_SameScore()
        {
            var a = ZpeRecord.From(Played(30, 0), 1);
            var b = ZpeRecord.From(Played(30, 5), 1);
            var c = ZpeRecord.From(Played(30, 20), 1);
            Assert.AreEqual(a.seconds, b.seconds, 0.05);
            Assert.AreEqual(a.seconds, c.seconds, 0.05);
            Assert.AreEqual(Zpe.Score(a, Ref8, C).Total, Zpe.Score(c, Ref8, C).Total, 1e-3);
            Assert.AreEqual(20f, c.animSeconds, 0.05f);
            Assert.AreEqual(SessionOutcome.TimeMeasureVersion, c.timeVer);
        }

        /// <summary>02_ZPE kontrol örneği: referans 8 adım → beklenen 38 sn; 38 sn karar = tam hız puanı.</summary>
        [Test]
        public void Reference8Steps_Expected38Seconds()
        {
            Assert.AreEqual(38.0, C.TimeBase + C.TimePerStep * Ref8.Steps, 1e-9);
            Assert.AreEqual(1.0, Zpe.Score(ZpeRecord.From(Played(38, 12), 1), Ref8, C).Time, 0.01);
            // Eski ölçümde 38 sn karar + 12 sn animasyon tek sayaçta 50 sn olurdu (hız ≈ 0,76).
            var legacy = new ZpeRecord { levelId = "r", success = true, firstAttempt = true, movesUsed = 8, moveLimit = 14, seconds = 50f };
            Assert.AreEqual(38.0 / 50.0, Zpe.Score(legacy, Ref8, C).Time, 1e-6);
        }

        /// <summary>Z02: sayılmayan zaman (None: duraklatma, reklam, modal, odak kaybı, ipucu hazırlığı) hiçbir sayaca girmez;
        /// öğretici turu karar süresine girmez.</summary>
        [Test]
        public void NoneAndTutorial_DoNotCountAsDecision()
        {
            var o = Played(10, 0, tutorial: 25);
            o.Finished = false;
            for (int i = 0; i < 600; i++) o.Tick(PlayTime.None, 1.0 / 60);
            Assert.AreEqual(10.0, o.DecisionSeconds, 0.05);
            Assert.AreEqual(25.0, o.TutorialSeconds, 0.05);
            Assert.AreEqual(35.0, o.ActiveSeconds, 0.1);
        }

        /// <summary>Arka plandan dönüşteki büyük kare en çok 0,5 sn; bitmiş oturuma süre eklenmez.</summary>
        [Test]
        public void LargeFrame_Clamped_FinishedIgnored()
        {
            var o = new SessionOutcome();
            o.Tick(PlayTime.Decision, 120);
            Assert.AreEqual(SessionOutcome.MaxFrame, o.DecisionSeconds, 1e-9);
            o.Finished = true;
            o.Tick(PlayTime.Decision, 0.2);
            Assert.AreEqual(SessionOutcome.MaxFrame, o.DecisionSeconds, 1e-9);
        }

#if UNITY_5_3_OR_NEWER
        /// <summary>Z04: eski kayıt (timeVer yok) JSON'dan veri kaybı olmadan okunur; süresi aynen kullanılır (geriye dönük
        /// düzeltilemez), yeni alanlar 0.</summary>
        [Test]
        public void LegacyRecord_LoadsUnchanged()
        {
            const string json = "{\"levelId\":\"r\",\"success\":true,\"firstAttempt\":true,\"assisted\":false,\"movesUsed\":8,\"moveLimit\":14,\"moveBonus\":0,\"seconds\":44.5,\"algo\":1,\"time\":1700000000}";
            var r = UnityEngine.JsonUtility.FromJson<ZpeRecord>(json);
            Assert.AreEqual(44.5f, r.seconds, 1e-6f);
            Assert.AreEqual(0, r.timeVer);
            Assert.AreEqual(0f, r.animSeconds);
            Assert.AreEqual(1700000000L, r.time);
            var back = UnityEngine.JsonUtility.FromJson<ZpeRecord>(UnityEngine.JsonUtility.ToJson(r));
            Assert.AreEqual(Zpe.Score(r, Ref8, C).Total, Zpe.Score(back, Ref8, C).Total, 1e-9);
        }
#endif

        /// <summary>02_ZPE kontrol örneği: 200 kayıtta 70 ortalama + 90 puan → 70,0995 (tam sayıda artış görünmeyebilir).</summary>
        [Test]
        public void Average200_Plus90()
        {
            double avg = (70.0 * 200 + 90.0) / 201;
            Assert.AreEqual(70.0995, avg, 1e-4);
            Assert.AreEqual(70, (int)System.Math.Round(avg));
        }
    }
}
