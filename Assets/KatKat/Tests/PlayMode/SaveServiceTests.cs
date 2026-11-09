using System.IO;
using KatKat.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    public class SaveServiceTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Application.temporaryCachePath, "katkat_save_tests");
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void DailyStars_CountInTotal_AndSurviveSaveLoad()
        {
            // Kullanıcı (2026-11-08): günlük yıldızları toplam yıldıza (taş eşikleri, harita) sayılır.
            var s = new SaveService(_dir);
            s.Data.meta.dailyStars = 4;
            Assert.AreEqual(4, s.Data.TotalStars);
            Assert.AreEqual(0, s.Data.LevelStars);
            s.Save();
            var again = new SaveService(_dir);
            Assert.AreEqual(4, again.Data.meta.dailyStars);
            Assert.AreEqual(4, again.Data.TotalStars);
        }

        [Test]
        public void MissingSave_StartsSafely()
        {
            var s = new SaveService(_dir);
            Assert.AreEqual(0, s.Data.highestUnlocked);
            Assert.IsTrue(s.Data.sound);
            Assert.IsTrue(s.Data.vibration);
        }

        [Test]
        public void RoundTrip_AndRepeatedCompletionIsIdempotent()
        {
            var s = new SaveService(_dir);
            Assert.IsTrue(s.MarkCompleted("level_001", 0, 20));
            Assert.IsFalse(s.MarkCompleted("level_001", 0, 20), "Aynı tamamlama ikinci kez etki üretmez");
            s.Data.sound = false;
            s.Save();
            var r = new SaveService(_dir);
            Assert.AreEqual(1, r.Data.highestUnlocked);
            CollectionAssert.AreEqual(new[] { "level_001" }, r.Data.completed);
            Assert.IsFalse(r.Data.sound);
        }

        [Test]
        public void CorruptMainFile_FallsBackToBackup()
        {
            var s = new SaveService(_dir);
            s.MarkCompleted("level_001", 0, 20);
            s.MarkCompleted("level_002", 1, 20); // ikinci yazım ilkini .bak yapar
            File.WriteAllText(s.Path, "{ bozuk");
            LogAssert.ignoreFailingMessages = true;
            var r = new SaveService(_dir);
            Assert.AreEqual(1, r.Data.highestUnlocked, "Yedekte level_001 sonrası durum var");
        }

        [Test]
        public void CorruptMainAndBackup_StartsSafely()
        {
            Directory.CreateDirectory(_dir);
            var path = Path.Combine(_dir, SaveService.FileName);
            File.WriteAllText(path, "çöp");
            File.WriteAllText(path + ".bak", "");
            LogAssert.ignoreFailingMessages = true;
            var r = new SaveService(_dir);
            Assert.AreEqual(0, r.Data.highestUnlocked);
        }

        /// <summary>v9 A: v1 kaydı açılır, yükseltilir ve yazılır; ikinci açılışta yükseltme tekrar uygulanmaz.</summary>
        [Test]
        public void V1Save_UpgradesOnce_KeepingProgressSettingsAndValidStars()
        {
            Directory.CreateDirectory(_dir);
            var path = Path.Combine(_dir, SaveService.FileName);
            File.WriteAllText(path, "{\"saveVersion\":1,\"highestUnlocked\":12,\"completed\":[\"level_001\",\"level_002\"],\"sound\":false,\"vibration\":true," +
                "\"starIds\":[\"level_001\",\"level_002\",\"level_001\",\"level_003\"],\"starCounts\":[2,3,1,9]}");
            LogAssert.ignoreFailingMessages = true;
            var s = new SaveService(_dir);
            Assert.AreEqual(SaveData.CurrentVersion, s.Data.saveVersion);
            Assert.AreEqual(1, s.Data.upgradedFrom);
            Assert.AreEqual(12, s.Data.highestUnlocked);
            Assert.IsFalse(s.Data.sound);
            CollectionAssert.AreEqual(new[] { "level_001", "level_002" }, s.Data.completed);
            Assert.AreEqual(5, s.Data.TotalStars, "kimlik başına en yüksek geçerli yıldız (2+3), 9 atılır");
            Assert.IsNotNull(s.Data.meta);
            Assert.IsEmpty(s.Data.meta.stones);
            StringAssert.Contains("\"saveVersion\": 2", File.ReadAllText(path), "yükseltme kalıcı yazıldı");

            var written = File.ReadAllText(path);
            var r = new SaveService(_dir);
            Assert.AreEqual(1, r.Data.upgradedFrom);
            Assert.AreEqual(5, r.Data.TotalStars);
            Assert.AreEqual(written, File.ReadAllText(path), "ikinci açılışta yükseltme yazılmadı");
        }

        [Test]
        public void RecordStars_3_1_3_AddsThree_And1To3AddsTwo()
        {
            var s = new SaveService(_dir);
            Assert.AreEqual(3, s.RecordStars("level_004", 3));
            Assert.AreEqual(0, s.RecordStars("level_004", 1));
            Assert.AreEqual(0, s.RecordStars("level_004", 3));
            Assert.AreEqual(1, s.RecordStars("level_005", 1));
            Assert.AreEqual(2, s.RecordStars("level_005", 3));
            var r = new SaveService(_dir);
            Assert.AreEqual(6, r.Data.TotalStars);
            Assert.AreEqual(3, r.Data.StarsFor("level_004"));
        }

        /// <summary>v10 P1.3: yükseltmeden önceki dosya bir kez, değişmeden saklanır; sonraki yazışlar ona dokunmaz.</summary>
        [Test]
        public void V1Save_KeepsPreUpgradeCopy_OnceAndUnchanged()
        {
            Directory.CreateDirectory(_dir);
            var path = Path.Combine(_dir, SaveService.FileName);
            const string v1 = "{\"saveVersion\":1,\"highestUnlocked\":3,\"completed\":[\"level_001\"],\"sound\":true,\"vibration\":true}";
            File.WriteAllText(path, v1);
            var s = new SaveService(_dir);
            var copy = s.PreUpgradePath(1);
            Assert.IsTrue(File.Exists(copy), "yükseltme öncesi kopya");
            Assert.AreEqual(v1, File.ReadAllText(copy));
            s.MarkCompleted("level_002", 1, 20);
            s.MarkCompleted("level_003", 2, 20);
            var r = new SaveService(_dir);
            Assert.AreEqual(v1, File.ReadAllText(copy), "sonraki yazışlar ve açılışlar kopyaya dokunmaz");
            Assert.AreEqual(3, r.Data.highestUnlocked);
        }

        [Test]
        public void NewSave_HasNoPreUpgradeCopy()
        {
            var s = new SaveService(_dir);
            s.MarkCompleted("level_001", 0, 20);
            var r = new SaveService(_dir);
            Assert.IsFalse(File.Exists(r.PreUpgradePath(1)));
        }

        /// <summary>v10 P1.3: bozuk ana dosyanın kopyası alınır (bir sonraki yazış onu .bak ile değiştirir).</summary>
        [Test]
        public void CorruptMainFile_IsKeptAsCorruptCopy()
        {
            var s = new SaveService(_dir);
            s.MarkCompleted("level_001", 0, 20);
            s.MarkCompleted("level_002", 1, 20);
            File.WriteAllText(s.Path, "{ bozuk");
            LogAssert.ignoreFailingMessages = true;
            var r = new SaveService(_dir);
            Assert.AreEqual("{ bozuk", File.ReadAllText(r.Path + ".corrupt"));
            r.MarkCompleted("level_003", 2, 20);
            Assert.AreEqual(3, new SaveService(_dir).Data.highestUnlocked, "yedekten devam edilen ilerleme yazıldı");
        }

        /// <summary>v10 P1.4: 1 → 3 → 2 yıldız; toplam yalnız en iyi değerle (3) kalır, kaydedilip yeniden açılınca da.</summary>
        [Test]
        public void RecordStars_1_3_2_KeepsBestThree()
        {
            var s = new SaveService(_dir);
            Assert.AreEqual(1, s.RecordStars("level_010", 1));
            Assert.AreEqual(2, s.RecordStars("level_010", 3));
            Assert.AreEqual(0, s.RecordStars("level_010", 2));
            Assert.AreEqual(3, s.Data.TotalStars);
            var r = new SaveService(_dir);
            Assert.AreEqual(3, r.Data.StarsFor("level_010"));
            Assert.AreEqual(3, r.Data.TotalStars);
        }

        /// <summary>E paketi arama tabloları: listeler değişince (ekleme, güncelleme, yeni liste) sonuç güncel kalır.</summary>
        [Test]
        public void Lookups_StayCorrect_AfterMutations()
        {
            var d = new SaveData();
            Assert.IsFalse(d.IsCompleted("level_001"));
            d.completed.Add("level_001");
            Assert.IsTrue(d.IsCompleted("level_001"));
            d.completed = new System.Collections.Generic.List<string> { "level_009" };
            Assert.IsFalse(d.IsCompleted("level_001"));
            Assert.IsTrue(d.IsCompleted("level_009"));

            Assert.AreEqual(0, d.StarsFor("level_002"));
            KatKat.Meta.ProgressRules.RecordBest(d.starIds, d.starCounts, "level_002", 1);
            Assert.AreEqual(1, d.StarsFor("level_002"));
            KatKat.Meta.ProgressRules.RecordBest(d.starIds, d.starCounts, "level_002", 3); // yerinde güncelleme
            Assert.AreEqual(3, d.StarsFor("level_002"));
            d.starIds.Insert(0, "level_007"); d.starCounts.Insert(0, 2); // sıra kayması
            Assert.AreEqual(3, d.StarsFor("level_002"));
            Assert.AreEqual(2, d.StarsFor("level_007"));
        }

        [Test]
        public void Unlock_NeverExceedsLastLevel()
        {
            var s = new SaveService(_dir);
            s.MarkCompleted("level_020", 19, 20);
            Assert.AreEqual(19, s.Data.highestUnlocked);
        }

        // v10 P0.3: gerçek biçimdeki örnek kayıtlar (Tests/PlayMode/Data). Her yeni kayıt sürümünde bu dosyalar
        // değiştirilmeden açılmaya devam etmeli; yeni sürüm için yeni örnek eklenir.
        private string Fixture(string name)
        {
            var src = Path.Combine(Application.dataPath, "KatKat", "Tests", "PlayMode", "Data", name);
            Assert.IsTrue(File.Exists(src), "örnek kayıt yok: " + src);
            Directory.CreateDirectory(_dir);
            var path = Path.Combine(_dir, SaveService.FileName);
            File.Copy(src, path, true);
            return File.ReadAllText(src);
        }

        /// <summary>v10 P0.3: v8 dönemi (v1) örnek kayıt kayıpsız yükselir; tekrar açılınca değişmez.</summary>
        [Test]
        public void SampleV1_UpgradesWithoutLoss()
        {
            var original = Fixture("save_v1_sample.json");
            LogAssert.ignoreFailingMessages = true;
            var s = new SaveService(_dir);
            Assert.AreEqual(SaveData.CurrentVersion, s.Data.saveVersion);
            Assert.AreEqual(1, s.Data.upgradedFrom);
            Assert.AreEqual(120, s.Data.highestUnlocked);
            Assert.AreEqual(120, s.Data.completed.Count);
            Assert.IsTrue(s.Data.IsCompleted("level_001") && s.Data.IsCompleted("level_120"));
            Assert.IsFalse(s.Data.IsCompleted("level_121"));
            Assert.IsFalse(s.Data.sound);
            Assert.IsTrue(s.Data.vibration);
            Assert.AreEqual(3, s.Data.StarsFor("level_005"), "yinelenen kayıtta en iyi yıldız");
            Assert.AreEqual(0, s.Data.StarsFor("level_200"), "geçersiz yıldız (9) atıldı");
            Assert.AreEqual(240, s.Data.TotalStars);
            Assert.IsNotNull(s.Data.meta);
            Assert.IsEmpty(s.Data.meta.stones, "taş yükseltmede verilmez; oyun açılışta kuralı uygular");
            Assert.AreEqual(original, File.ReadAllText(s.PreUpgradePath(1)), "yükseltme öncesi kopya birebir");

            var written = File.ReadAllText(Path.Combine(_dir, SaveService.FileName));
            var r = new SaveService(_dir);
            Assert.AreEqual(240, r.Data.TotalStars);
            Assert.AreEqual(120, r.Data.completed.Count);
            Assert.AreEqual(written, File.ReadAllText(Path.Combine(_dir, SaveService.FileName)), "ikinci açılış dosyayı değiştirmedi");
        }

        /// <summary>v10 P0.3: v9 dönemi (v2, günlük grup alanı öncesi) örnek kayıt olduğu gibi açılır; yükseltme ve kopya yok.</summary>
        [Test]
        public void SampleV2_LoadsAsIs_AndRoundTrips()
        {
            Fixture("save_v2_sample.json");
            var s = new SaveService(_dir);
            Assert.AreEqual(2, s.Data.saveVersion);
            Assert.AreEqual(1, s.Data.upgradedFrom);
            Assert.IsFalse(File.Exists(s.PreUpgradePath(2)) || File.Exists(s.PreUpgradePath(1)), "yükseltme olmadı, kopya yok");
            Assert.AreEqual(37, s.Data.highestUnlocked);
            Assert.AreEqual(12, s.Data.completed.Count);
            Assert.IsTrue(s.Data.sound);
            Assert.IsFalse(s.Data.vibration);
            Assert.AreEqual(29, s.Data.TotalStars);
            var m = s.Data.meta;
            CollectionAssert.AreEqual(new[] { "stone_tide" }, m.stones);
            Assert.AreEqual("stone_tide", m.selectedStone);
            Assert.AreEqual(3, m.attemptedLevels.Count);
            Assert.AreEqual("2026-11-05", m.daily.dayId);
            Assert.AreEqual("daily_001", m.daily.challengeId);
            Assert.AreEqual("daily-v1", m.daily.contentVersion);
            Assert.AreEqual(0, m.daily.tier, "eski kayıtta grup alanı yok → 0");
            Assert.IsTrue(m.daily.won && m.daily.entered);
            Assert.AreEqual(2, m.daily.attemptsUsed);
            CollectionAssert.AreEqual(new[] { "fake-1" }, m.daily.adTokens);
            CollectionAssert.AreEqual(new[] { "2026-11-05|daily_001|daily-v1" }, m.rewardLog);
            Assert.AreEqual("2026-11-05", m.moveBonusDay);

            // Yazıp yeniden açınca hiçbir alan kaybolmaz.
            s.Save();
            var r = new SaveService(_dir);
            Assert.AreEqual(29, r.Data.TotalStars);
            Assert.AreEqual(JsonUtility.ToJson(s.Data), JsonUtility.ToJson(r.Data), "gidiş-dönüş kayıpsız");
        }
    }
}
