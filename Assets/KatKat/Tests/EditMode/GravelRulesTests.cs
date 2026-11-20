using System.Linq;
using KatKat.Core;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    /// <summary>2026-11-21 çakıl örtüsü ve Kürek: çakıllı yığın seçilemez, yalnız Kürek temizler; 4'lü grup Kürek verir.</summary>
    public class GravelRulesTests
    {
        private static readonly (LayerColor, int) FarGoal = (LayerColor.Purple, 99);

        [Test]
        public void GravelStack_IsNotSelectable_AndNeighbourPeelDoesNotClearIt()
        {
            var s = State(5, new[] { "c c c", "c bG c" }, FarGoal);
            Assert.AreEqual(RejectReason.NotSelectable, RulesEngine.TryApply(s, Command.Tap(1, 1)).Reason);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.IsTrue(s.Board[1, 1].Gravel);
            Assert.AreEqual("b", Cell(s, 1, 1));
        }

        [Test]
        public void GroupOfFour_WithGravelOnBoard_GivesShovel()
        {
            var s = State(5, new[] { "c c c c", "y y bG y" }, FarGoal);
            s.SpecialsEnabled = true;
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(SpecialKind.Shovel, r.SpecialCreated);
            Assert.AreEqual(SpecialKind.Shovel, s.Tray.Single());
        }

        [Test]
        public void GroupOfFour_WithoutGravel_GivesNothing()
        {
            var s = State(5, new[] { "c c c c", "y y b y" }, FarGoal);
            s.SpecialsEnabled = true;
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(SpecialKind.None, r.SpecialCreated);
            Assert.AreEqual(0, s.Tray.Count);
        }

        [Test]
        public void GroupOfFive_WithGravel_StillGivesRocket()
        {
            var s = State(5, new[] { "c c c c c", "y y bG y y" }, FarGoal);
            s.SpecialsEnabled = true;
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.SpecialCreated.IsRocket());
        }

        [Test]
        public void Shovel_Clears3x3Gravel_NoMoveSpent_NoPeel()
        {
            var s = State(5, new[] { "bG bG bG y", "bG bG bG y", "y y y yG" }, FarGoal);
            s.Tray.Add(SpecialKind.Shovel);
            var r = RulesEngine.TryApply(s, Command.UseSpecial(0, 1, 1));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(5, s.MovesLeft);
            Assert.AreEqual(6, r.Waves.Sum(w => w.GravelCleared.Count));
            Assert.AreEqual(0, r.Waves.Sum(w => w.Cells.Count));
            Assert.IsTrue(s.Board[1, 1].IsSelectable);
            Assert.IsTrue(s.Board[3, 2].Gravel); // alan dışında kalır
        }

        [Test]
        public void Shovel_WithoutGravelInArea_HasNoEffect()
        {
            var s = State(5, new[] { "y y y bG", "y y y y" }, FarGoal);
            s.Tray.Add(SpecialKind.Shovel);
            Assert.AreEqual(RejectReason.TrayNoEffect, RulesEngine.TryApply(s, Command.UseSpecial(0, 0, 0)).Reason);
        }

        [Test]
        public void RocketAndBomb_DoNotTouchGravel()
        {
            var s = State(5, new[] { "c bG c", "y y y" }, FarGoal);
            s.Tray.Add(SpecialKind.RocketRow);
            RulesEngine.TryApply(s, Command.UseSpecial(0, 0, 0));
            Assert.IsTrue(s.Board[1, 0].Gravel);
            Assert.AreEqual("b", Cell(s, 1, 0));
            Assert.IsFalse(RulesEngine.BombHasEffect(State(5, new[] { "bG bG", "bG bG" }, FarGoal).Board, 0, 0));
        }
    }
}
