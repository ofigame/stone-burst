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

        /// <summary>Bulunan boşluk kenara değmez, kare alanı tamamen boştur (hücre örtülmez).</summary>
        [Test]
        public void Holes_AreInterior_AndSquareIsEmpty()
        {
            int found = 0;
            for (int n = 1; n <= 1000; n++)
            {
                var b = LevelLoader.ToState(Level(n)).Board;
                var h = BoardHoles.Largest(b);
                if (!h.Has) continue;
                found++;
                Assert.Greater(h.MinX, 0, n.ToString()); Assert.Greater(h.MinY, 0, n.ToString());
                Assert.Less(h.MaxX, b.Width - 1, n.ToString()); Assert.Less(h.MaxY, b.Height - 1, n.ToString());
                Assert.GreaterOrEqual(h.Side, 1);
            }
            Assert.Greater(found, 0, "katalogda en az bir iç boşluk");
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
