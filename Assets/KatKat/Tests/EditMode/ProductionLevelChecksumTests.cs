using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>
    /// v10 P0.2: kullanıcının test ettiği 1.000 bölüm dosyası bayt bayt korunur. Özetler
    /// Tests/EditMode/Data/levels_v13.sha256 içinde; bir bölüm eklenir, silinir ya da tek bayt değişirse test düşer.
    /// Bilinçli güncelleme yalnız kullanıcı onayıyla, özet dosyası yeniden üretilerek yapılır.
    /// </summary>
    public class ProductionLevelChecksumTests
    {
        private static string ChecksumFile()
        {
            var levels = ProductionLevelTests.LevelsDir(); // .../Assets/KatKat/Resources/Levels
            var kat = Directory.GetParent(Directory.GetParent(levels).FullName).FullName; // .../Assets/KatKat
            var p = Path.Combine(kat, "Tests", "EditMode", "Data", "levels_v13.sha256");
            Assert.IsTrue(File.Exists(p), "Özet dosyası yok: " + p);
            return p;
        }

        private static Dictionary<string, string> Expected()
        {
            var d = new Dictionary<string, string>();
            foreach (var raw in File.ReadAllLines(ChecksumFile()))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                var parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                Assert.AreEqual(2, parts.Length, "Bozuk satır: " + line);
                d[parts[1]] = parts[0].ToLowerInvariant();
            }
            return d;
        }

        private static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var s = File.OpenRead(path))
                return string.Concat(sha.ComputeHash(s).Select(b => b.ToString("x2")));
        }

        [Test]
        public void ProductionLevels_AreExactlyTheTested1000()
        {
            var expected = Expected();
            Assert.AreEqual(1000, expected.Count, "özet dosyasında 1000 bölüm");
            var files = Directory.GetFiles(ProductionLevelTests.LevelsDir(), "level_*.json")
                .ToDictionary(Path.GetFileName, f => f);

            var missing = expected.Keys.Where(k => !files.ContainsKey(k)).ToList();
            var extra = files.Keys.Where(k => !expected.ContainsKey(k)).ToList();
            var changed = expected.Where(kv => files.ContainsKey(kv.Key) && Sha256(files[kv.Key]) != kv.Value).Select(kv => kv.Key).ToList();

            Assert.IsEmpty(missing, "silinmiş bölüm: " + string.Join(", ", missing.Take(10)));
            Assert.IsEmpty(extra, "özet listesinde olmayan yeni bölüm: " + string.Join(", ", extra.Take(10)));
            Assert.IsEmpty(changed, $"{changed.Count} bölüm değişmiş: " + string.Join(", ", changed.Take(10)));
        }

        [Test]
        public void ChecksumList_IsSequential_level_001_to_1000()
        {
            var names = Expected().Keys.OrderBy(n => LevelValidator.LevelNumber(Path.GetFileNameWithoutExtension(n))).ToList();
            for (int i = 0; i < names.Count; i++) Assert.AreEqual($"level_{i + 1:000}.json", names[i]);
        }
    }
}
