using System.IO;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>vr1 P1: tahta ortasında buzlu taş yalnız var olan iç boşluğa konur (yeni delik yok).</summary>
    public class BoardHolesTests
    {
        private static LevelData Level(int n)
        {
            var f = Path.Combine(ProductionLevelTests.LevelsDir(), $"level_{n:000}.json");
            return LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f));
        }

        /// <summary>
        /// Bulunan boşluk kenara değmez, kare alanı tamamen boştur (hücre örtülmez). v14 (kullanıcı 2026-11-16: "ortası delik
        /// tahtaları kaldıralım"): ana katalogda iç boşluk yok, bu yüzden olumlu durum yapay tahtada sınanır; kenar çentiği boşluk sayılmaz.
        /// </summary>
        [Test]
        public void Holes_AreInterior_AndSquareIsEmpty()
        {
            var b = new KatKat.Core.BoardState(6, 6);
            for (int i = 0; i < b.Count; i++) { b[i].Active = true; b[i].Layers.Add(KatKat.Core.LayerColor.Coral); }
            foreach (var (x, y) in new[] { (2, 2), (3, 2), (2, 3), (3, 3), (0, 0), (1, 0) }) { b[x, y].Active = false; b[x, y].Layers.Clear(); }
            var h = BoardHoles.Largest(b);
            Assert.IsTrue(h.Has, "ortadaki 2×2 boşluk");
            Assert.AreEqual((2, 2, 3, 3), (h.MinX, h.MinY, h.MaxX, h.MaxY), "köşe çentiği (kenara değen) boşluk değil");
            Assert.AreEqual(2, h.Side);
            for (int n = 1; n <= 1000; n++)
                Assert.IsFalse(BoardHoles.Largest(LevelLoader.ToState(Level(n)).Board).Has, $"level_{n:000}: v14'te iç boşluk yok");
        }

        /// <summary>Ölçüm (elle): iç boşluk dağılımı.</summary>
        [Test, Explicit]
        public void Measure_HoleDistribution()
        {
            int none = 0, s1 = 0, s2 = 0, s3 = 0;
            var ex2 = new System.Text.StringBuilder();
            for (int n = 1; n <= 1000; n++)
            {
                var h = BoardHoles.Largest(LevelLoader.ToState(Level(n)).Board);
                if (!h.Has) none++;
                else if (h.Side == 1) s1++;
                else if (h.Side == 2) { s2++; if (ex2.Length < 200) ex2.Append(n).Append(' '); }
                else { s3++; if (ex2.Length < 400) ex2.Append(n).Append('*').Append(' '); }
            }
            TestContext.Progress.WriteLine($"BOŞLUK yok {none}, 1x1 {s1}, 2x2 {s2}, 3x3+ {s3}; örnek: {ex2}");
        }
    }
}
