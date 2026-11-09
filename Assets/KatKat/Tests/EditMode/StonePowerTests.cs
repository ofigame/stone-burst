using System.Collections.Generic;
using System.IO;
using KatKat.Core;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>
    /// v10 P3.2: taş gücü ortak yapısı. Her güç gerçek <see cref="RulesEngine"/> ile sınanır; geçersiz durumda
    /// <see cref="LevelState"/> bayt bayt aynı kalır (ContentEquals). Güçler hamle harcamaz.
    /// </summary>
    public class StonePowerTests
    {
        private static LevelState Specials(int moves = 5)
        {
            var s = TestBoards.State(moves, new[]
            {
                "cb  pb  yb  cb",
                "bc  yc  pc  bc",
                "yp  cp  bp  yp",
            }, (LayerColor.Blue, 99)); // yazımda son harf üst katman: ilk satırın üstleri turkuaz
            s.SpecialsEnabled = true;
            return s;
        }

        private static int RowLayers(LevelState s, int y) { int n = 0; for (int x = 0; x < s.Board.Width; x++) n += s.Board[x, y].Layers.Count; return n; }
        private static int ColLayers(LevelState s, int x) { int n = 0; for (int y = 0; y < s.Board.Height; y++) n += s.Board[x, y].Layers.Count; return n; }

        [Test]
        public void Registry_HasLaunchFamilies_UnknownIsRejected_StateUnchanged()
        {
            CollectionAssert.IsSubsetOf(new[] { "dailyRoll", "ocean", "storm", "galaxy", "moon" }, new List<string>(StonePowers.Ids));
            var s = Specials(); var before = s.Clone();
            Assert.IsFalse(StonePowers.CanApply("fire_unknown", s));
            Assert.IsFalse(StonePowers.Apply("fire_unknown", s));
            Assert.IsFalse(StonePowers.Apply(null, s));
            Assert.IsTrue(s.ContentEquals(before));
        }

        [Test]
        public void Ocean_GivesRowRocket_ThatPeelsTheRow_WithoutSpendingMoves()
        {
            var s = Specials();
            Assert.IsTrue(StonePowers.Apply(StonePowers.Ocean, s));
            CollectionAssert.AreEqual(new[] { SpecialKind.RocketRow }, s.Tray);
            int moves = s.MovesLeft, row = RowLayers(s, 1), other = RowLayers(s, 0);
            var r = RulesEngine.TryApply(s, Command.UseSpecial(0, 2, 1));
            Assert.IsTrue(r.Accepted, r.Reason.ToString());
            Assert.Less(RowLayers(s, 1), row, "satır soyuldu");
            Assert.AreEqual(moves, s.MovesLeft, "tepsi kullanımı hamle harcamaz");
            Assert.IsEmpty(s.Tray);
            Assert.GreaterOrEqual(other, RowLayers(s, 0)); // zincir olabilir; satır 1 kesin soyuldu
        }

        [Test]
        public void Storm_GivesColumnRocket_ThatPeelsTheColumn()
        {
            var s = Specials();
            Assert.IsTrue(StonePowers.Apply(StonePowers.Storm, s));
            CollectionAssert.AreEqual(new[] { SpecialKind.RocketColumn }, s.Tray);
            int col = ColLayers(s, 3);
            Assert.IsTrue(RulesEngine.TryApply(s, Command.UseSpecial(0, 3, 0)).Accepted);
            Assert.Less(ColLayers(s, 3), col, "sütun soyuldu");
        }

        [Test]
        public void Galaxy_GivesGoldenRoll_ThatCollectsTargetColor()
        {
            var s = Specials();
            Assert.IsTrue(StonePowers.Apply(StonePowers.Galaxy, s));
            CollectionAssert.AreEqual(new[] { SpecialKind.Rainbow }, s.Tray);
            int coralBefore = s.Goals[0].Progress;
            Assert.IsTrue(RulesEngine.TryApply(s, Command.UseSpecial(0, 0, 0)).Accepted); // (0,0) üstü turkuaz
            Assert.Greater(s.Goals[0].Progress, coralBefore, "turkuaz toplandı");
        }

        [Test]
        public void Moon_AddsMoves()
        {
            var s = Specials(moves: 3);
            Assert.IsTrue(StonePowers.Apply(StonePowers.Moon, s));
            Assert.AreEqual(3 + StonePowers.MoonMoves, s.MovesLeft);
        }

        [Test]
        public void TrayPowers_RejectedWhenTrayFull_OrSpecialsOff_StateUnchanged()
        {
            var full = Specials();
            for (int i = 0; i < LevelState.TrayCapacity; i++) full.Tray.Add(SpecialKind.RocketRow);
            var off = Specials(); off.SpecialsEnabled = false;
            foreach (var s in new[] { full, off })
                foreach (var id in new[] { StonePowers.Ocean, StonePowers.Storm, StonePowers.Galaxy })
                {
                    var before = s.Clone();
                    Assert.IsFalse(StonePowers.Apply(id, s), id);
                    Assert.IsTrue(s.ContentEquals(before), id + " durum değişmedi");
                }
        }

        [Test]
        public void TrayPowers_RejectedWhenBoardHasNoTarget()
        {
            var s = TestBoards.State(5, new[] { ". .", ". ." });
            s.SpecialsEnabled = true;
            var before = s.Clone();
            foreach (var id in new[] { StonePowers.Ocean, StonePowers.Storm, StonePowers.Galaxy })
                Assert.IsFalse(StonePowers.Apply(id, s), id);
            Assert.IsTrue(s.ContentEquals(before));
        }

        [Test]
        public void AllPowers_RejectedWhenNotIdle()
        {
            foreach (var status in new[] { GameStatus.Won, GameStatus.LostOutOfMoves })
            {
                var s = Specials(); s.RollEnabled = true; s.Status = status;
                var before = s.Clone();
                foreach (var id in StonePowers.Ids) Assert.IsFalse(StonePowers.Apply(id, s), id + " " + status);
                Assert.IsTrue(s.ContentEquals(before));
            }
        }

        [Test]
        public void Powers_AreDeterministic()
        {
            foreach (var id in StonePowers.Ids)
            {
                var a = Specials(); a.RollEnabled = true;
                var b = a.Clone();
                Assert.AreEqual(StonePowers.Apply(id, a), StonePowers.Apply(id, b), id);
                Assert.IsTrue(a.ContentEquals(b), id + " aynı girdi → aynı sonuç");
            }
        }

        [Test]
        public void ContentStones_UseRegisteredPowers()
        {
            var path = Path.Combine(Path.GetDirectoryName(ProductionLevelTests.LevelsDir()), "Collection", "stones.json");
            foreach (var st in StoneCatalog.Parse(File.ReadAllText(path)).Stones)
                if (!string.IsNullOrEmpty(st.PowerId)) // boş = gücü henüz yok (ayrı karar); yazılan her güç kayıtlı olmalı
                    Assert.IsNotNull(StonePowers.Get(st.PowerId), $"{st.StoneId}: bilinmeyen güç '{st.PowerId}'");
        }
    }
}
