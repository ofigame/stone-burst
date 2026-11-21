using KatKat.Core;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    /// <summary>2026-11-21 sürükle-değiştir: üst taş komşunun aynı katındaki taşla yer değiştirir; 3+ grup oluşursa patlar.</summary>
    public class SwapRulesTests
    {
        private static readonly (LayerColor, int) FarGoal = (LayerColor.Purple, 99);
        private const int Right = 0, Down = 1, Left = 2, Up = 3;

        [Test]
        public void Swap_CompletingThree_PopsGroup_AndCostsAMove()
        {
            var s = State(5, new[] { "c c b", "y y c" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Swap(2, 1, Up));
            Assert.IsTrue(r.Accepted, r.Reason.ToString());
            Assert.AreEqual(4, s.MovesLeft);
            Assert.AreEqual(3, r.TappedGroup.Count);
            Assert.AreEqual(".", Cell(s, 0, 0));
            Assert.AreEqual(".", Cell(s, 2, 0));
            Assert.AreEqual("b", Cell(s, 2, 1));
            Assert.AreEqual(new Coord(2, 1), r.SwapFrom);
            Assert.AreEqual(new Coord(2, 0), r.SwapTo);
            Assert.AreEqual(LayerColor.Coral, r.SwapFromColor);
            Assert.AreEqual(LayerColor.Blue, r.SwapToColor);
        }

        [Test]
        public void Swap_UsesSameLayerOfTallerNeighbour()
        {
            // (2,0) alttan üste [y, b]; (2,1) tek katlı c. Kat 0 değişir: (2,1) y olur ve alt satır üçlenir; (2,0) [c, b] kalır.
            var s = State(5, new[] { "c c yb", "y y c" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Swap(2, 1, Up));
            Assert.IsTrue(r.Accepted, r.Reason.ToString());
            Assert.AreEqual(0, r.SwapLayer);
            Assert.AreEqual("cb", Cell(s, 2, 0));
            Assert.AreEqual(".", Cell(s, 0, 1));
            Assert.AreEqual(".", Cell(s, 2, 1));
        }

        [Test]
        public void Swap_WithoutMatch_IsRejected_AndStateUnchanged()
        {
            var s = State(5, new[] { "c b y", "p p y" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Swap(0, 0, Right));
            Assert.AreEqual(RejectReason.SwapNoMatch, r.Reason);
            Assert.AreEqual(5, s.MovesLeft);
            Assert.AreEqual("c", Cell(s, 0, 0));
            Assert.AreEqual("b", Cell(s, 1, 0));
        }

        [Test]
        public void Swap_NeighbourWithoutThatLayer_IsInvalid()
        {
            var s = State(5, new[] { "cy b c", "y y c" }, FarGoal);
            Assert.AreEqual(RejectReason.SwapInvalid, RulesEngine.TryApply(s, Command.Swap(0, 0, Right)).Reason);
        }

        [Test]
        public void Swap_SameColour_IsRejected()
        {
            var s = State(5, new[] { "c c b", "y y c" }, FarGoal);
            Assert.AreEqual(RejectReason.SameColor, RulesEngine.TryApply(s, Command.Swap(0, 0, Right)).Reason);
        }

        [Test]
        public void Swap_OffBoard_IsRejected()
        {
            var s = State(5, new[] { "c c b", "y y c" }, FarGoal);
            Assert.AreEqual(RejectReason.OutOfBounds, RulesEngine.TryApply(s, Command.Swap(0, 0, Left)).Reason);
        }

        [Test]
        public void Swap_IntoGravel_IsInvalid()
        {
            var s = State(5, new[] { "c c bG", "y y c" }, FarGoal);
            Assert.AreEqual(RejectReason.SwapInvalid, RulesEngine.TryApply(s, Command.Swap(2, 1, Up)).Reason);
        }
    }
}
