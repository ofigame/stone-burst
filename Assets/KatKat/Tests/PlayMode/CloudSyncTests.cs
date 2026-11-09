using KatKat.Save;
using NUnit.Framework;
using UnityEngine;

namespace KatKat.Tests
{
    /// <summary>Bulut kaydı kuralları (kullanıcı, 2026-11-09).</summary>
    public class CloudSyncTests
    {
        private static SaveData Make(int done, int stars, int unlocked)
        {
            var d = new SaveData { highestUnlocked = unlocked };
            for (int i = 0; i < done; i++) d.completed.Add("L" + i);
            for (int i = 0; i < stars; i++) { d.starIds.Add("S" + i); d.starCounts.Add(1); }
            return d;
        }

        [Test]
        public void Ahead_ByCompletedFirst_ThenStars_ThenUnlocked()
        {
            Assert.IsTrue(CloudSync.CloudIsAhead(Make(0, 0, 0), Make(1, 0, 0)));
            Assert.IsFalse(CloudSync.CloudIsAhead(Make(5, 0, 5), Make(4, 99, 50)), "tamamlanan bölüm önce gelir");
            Assert.IsTrue(CloudSync.CloudIsAhead(Make(5, 5, 5), Make(5, 6, 5)), "eşitse yıldız");
            Assert.IsTrue(CloudSync.CloudIsAhead(Make(5, 5, 5), Make(5, 5, 6)), "eşitse açılan bölüm");
            Assert.IsFalse(CloudSync.CloudIsAhead(Make(5, 5, 5), Make(5, 5, 5)), "eşitse geri yüklenmez");
            Assert.IsFalse(CloudSync.CloudIsAhead(Make(0, 0, 0), null));
        }

        [Test]
        public void Parse_RejectsEmptyBrokenAndNewerVersion()
        {
            Assert.IsNull(CloudSync.Parse(null));
            Assert.IsNull(CloudSync.Parse(""));
            Assert.IsNull(CloudSync.Parse("{bozuk"));
            var newer = Make(1, 1, 1);
            newer.saveVersion = SaveData.CurrentVersion + 1;
            Assert.IsNull(CloudSync.Parse(JsonUtility.ToJson(newer)));
            var ok = CloudSync.Parse(JsonUtility.ToJson(Make(3, 2, 3)));
            Assert.IsNotNull(ok);
            Assert.AreEqual(3, ok.completed.Count);
            Assert.IsNotNull(ok.meta);
        }

        [Test]
        public void Replace_KeepsDeviceSettings_WritesFile()
        {
            string dir = System.IO.Path.Combine(Application.temporaryCachePath, "katkat_cloud_test");
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            var s = new SaveService(dir);
            s.Data.sound = false;
            s.Save();
            var cloud = Make(7, 3, 7);
            cloud.sound = true; cloud.vibration = false;
            s.Replace(cloud);
            Assert.AreEqual(7, s.Data.completed.Count);
            Assert.IsFalse(s.Data.sound, "ses yerelden");
            Assert.IsTrue(s.Data.vibration, "titreşim yerelden");
            Assert.IsTrue(System.IO.File.Exists(s.Path + ".precloud"), "önceki kayıt kopyalandı");
            Assert.AreEqual(7, new SaveService(dir).Data.completed.Count, "dosyaya yazıldı");
            System.IO.Directory.Delete(dir, true);
        }
    }
}
