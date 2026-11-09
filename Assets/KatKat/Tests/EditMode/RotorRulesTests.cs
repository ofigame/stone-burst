using KatKat.Core;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    /// <summary>Premium-v2 M08 döner taban: rehber §10 kuralı ve §14 test matrisi.</summary>
    public class RotorRulesTests
    {
        private static readonly (LayerColor, int) FarGoal = (LayerColor.Purple, 99);

        [Test]
        public void Rotate_MovesOldTopToBottom_CostsOneMove_CollectsNothing()
        {
            // Alttan üste [sarı, mavi, mercan] → [mercan, sarı, mavi].
            var s = State(5, new[] { "ybcR p p", "c c c" }, (LayerColor.Coral, 9));
            var r = RulesEngine.TryApply(s, Command.Rotate(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(0, r.Waves.Count);
            Assert.AreEqual(0, r.Rotated.X);
            Assert.AreEqual("cyb", Cell(s, 0, 0));
            Assert.AreEqual(4, s.MovesLeft);
            Assert.AreEqual(0, s.Goals[0].Progress);
            Assert.IsTrue(s.Board[0, 0].HasRotor, "Taban kalıcıdır.");
        }

        [Test]
        public void Rotate_SingleLayer_IsInvalid_AndCostsNothing()
        {
            var s = State(5, new[] { "cR p p", "y y y" }, FarGoal);
            Assert.AreEqual(RejectReason.NotRotatable, RulesEngine.TryApply(s, Command.Rotate(0, 0)).Reason);
            Assert.AreEqual(5, s.MovesLeft);
        }

        [Test]
        public void Rotate_CoveredOrBaselessCell_IsInvalid()
        {
            var s = State(5, new[] { "ybi1R yb p", "c c c" }, FarGoal);
            Assert.AreEqual(RejectReason.NotRotatable, RulesEngine.TryApply(s, Command.Rotate(0, 0)).Reason, "Örtülü taban dönmez.");
            Assert.AreEqual(RejectReason.NotRotatable, RulesEngine.TryApply(s, Command.Rotate(1, 0)).Reason, "Tabansız hücre dönmez.");
            Assert.AreEqual(5, s.MovesLeft);
        }

        [Test]
        public void RotationThatFormsAGroup_DoesNotAutoPeel()
        {
            // Döndürme üstü mavi yapar; mavi üçlü hazır kalır, otomatik soyulmaz; sonraki hamlede seçilir.
            var s = State(5, new[] { "bcR b b", "y p y" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Rotate(0, 0));
            Assert.AreEqual(0, r.Waves.Count);
            Assert.AreEqual("cb", Cell(s, 0, 0));
            Assert.AreEqual(GameStatus.Idle, s.Status);
            var tap = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(tap.Accepted);
            Assert.AreEqual(3, tap.Waves[0].Cells.Count);
        }

        [Test]
        public void NoGroupButRotatableBase_IsNotALoss()
        {
            var s = State(5, new[] { "c c c", "ybR p y" }, FarGoal);
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.Idle, s.Status, "Döndürülebilir taban yasal komuttur.");
            Assert.IsTrue(RulesEngine.HasLegalMove(s));
        }

        [Test]
        public void RotateOnLastMove_WithoutWin_IsOutOfMoves()
        {
            var s = State(1, new[] { "bcR b b", "y p y" }, FarGoal);
            RulesEngine.TryApply(s, Command.Rotate(0, 0));
            Assert.AreEqual(GameStatus.LostOutOfMoves, s.Status);
        }

        [Test]
        public void Undo_RestoresLayerOrder()
        {
            var session = new GameSession(State(5, new[] { "ybcR p p", "c c c" }, FarGoal));
            var before = session.State.Clone();
            session.Submit(Command.Rotate(0, 0));
            Assert.IsTrue(session.Undo());
            Assert.IsTrue(before.ContentEquals(session.State));
            Assert.AreEqual("ybc", Cell(session.State, 0, 0));
        }

        [Test]
        public void ThreeRotations_ReturnToOriginalOrder()
        {
            var s = State(5, new[] { "ybcR p p", "c c c" }, FarGoal);
            for (int i = 0; i < 3; i++) RulesEngine.TryApply(s, Command.Rotate(0, 0));
            Assert.AreEqual("ybc", Cell(s, 0, 0));
            Assert.AreEqual(2, s.MovesLeft);
        }
    }
}
