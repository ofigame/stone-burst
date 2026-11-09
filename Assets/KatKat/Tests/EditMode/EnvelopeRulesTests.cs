using System.Linq;
using KatKat.Core;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    /// <summary>Premium-v2 M07 mühürlü zarf: rehber §10 kuralı ve §14 test matrisi.</summary>
    public class EnvelopeRulesTests
    {
        private static LevelState WithEnvelopeGoal(LevelState s, int amount)
        {
            s.Goals.Add(new GoalState { Type = GoalType.CollectEnvelopes, Amount = amount });
            return s;
        }

        private static readonly (LayerColor, int) FarGoal = (LayerColor.Purple, 99);

        [Test]
        public void Envelope_IsNotSelectable()
        {
            var s = State(5, new[] { "c cEc c", "y y   b" }, FarGoal);
            Assert.AreEqual(RejectReason.NotSelectable, RulesEngine.TryApply(s, Command.Tap(1, 0)).Reason);
            Assert.AreEqual(5, s.MovesLeft);
        }

        [Test]
        public void WrongColorNextToEnvelope_DoesNotOpen()
        {
            var s = WithEnvelopeGoal(State(5, new[] { "y y y", "bEc p p" }, FarGoal), 1);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(0, r.Waves[0].EnvelopeOpens.Count);
            Assert.IsTrue(s.Board[0, 1].HasEnvelope);
            Assert.AreEqual(0, s.Goals[1].Progress);
        }

        [Test]
        public void SealColorNextToEnvelope_OpensOnce_CollectsOneToken_StackIntact()
        {
            // Dört mercan komşusu aynı dalgada soyulur: yine tek zarf.
            var s = WithEnvelopeGoal(State(5, new[] { "c c   c", "c ybEc c", "c c   c" }, FarGoal), 3);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(1, r.Waves[0].EnvelopeOpens.Count);
            Assert.IsFalse(s.Board[1, 1].HasEnvelope);
            Assert.AreEqual("yb", Cell(s, 1, 1));
            Assert.AreEqual(1, s.Goals[1].Progress);
            Assert.IsFalse(r.Waves.SelectMany(w => w.Cells).Any(c => c.X == 1 && c.Y == 1));
        }

        [Test]
        public void SealColorFarAway_DoesNotOpen()
        {
            var s = WithEnvelopeGoal(State(5, new[] { "bEc p p", "y y y", "c c c" }, FarGoal), 1);
            RulesEngine.TryApply(s, Command.Tap(0, 2));
            Assert.IsTrue(s.Board[0, 0].HasEnvelope);
            Assert.AreEqual(0, s.Goals[1].Progress);
        }

        [Test]
        public void ChainWaveWithSealColor_OpensEnvelope()
        {
            // Dalga 0 maviyi soyar (zarf mercan ister), zincir dalgası mercanı: zarf ikinci dalgada açılır.
            var s = WithEnvelopeGoal(State(5, new[] { "cb cb cb", "p  yEc p" }, FarGoal), 1);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(0, r.Waves[0].EnvelopeOpens.Count);
            Assert.AreEqual(1, r.Waves[1].EnvelopeOpens.Count);
            Assert.AreEqual(1, r.Waves[1].GoalProgressAfter[1]);
        }

        [Test]
        public void OpenedEnvelope_IsNotChainSeed()
        {
            // v13: mercanlar kalkınca üç sarı birlikte açılıp patlar; zarfın sarısı onlara bağlı ama soyulmadığı için katılmaz.
            var s = WithEnvelopeGoal(State(5, new[] { "yc yc  yc", "b  yEc b" }, FarGoal), 1);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(3, r.Waves[1].Cells.Count);
            Assert.AreEqual("y", Cell(s, 1, 1));
            Assert.AreEqual(1, s.Goals[1].Progress);
            Assert.IsTrue(s.Board[1, 1].IsSelectable);
        }

        [Test]
        public void Roll_SkipsEnvelope_ButAdjacentSealColorRemovalOpensIt()
        {
            var s = WithEnvelopeGoal(State(5, new[] { "bc bcEc bc", "y y y" }, FarGoal), 1);
            s.RollEnabled = true;
            s.RollSlotFull = true;
            var r = RulesEngine.TryApply(s, Command.Roll(0));
            Assert.AreEqual(2, r.Waves[0].Cells.Count);
            Assert.AreEqual("bc", Cell(s, 1, 0));
            Assert.AreEqual(1, s.Goals[1].Progress);
        }

        [Test]
        public void EnvelopeGoal_CanWinTheLevel()
        {
            var s = State(5, new[] { "c c c", "yEc b p" });
            WithEnvelopeGoal(s, 1);
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.Won, s.Status);
        }

        [Test]
        public void Undo_RestoresEnvelopeAndTokenGoal()
        {
            var s = WithEnvelopeGoal(State(5, new[] { "c c c", "yEc b p" }, FarGoal), 2);
            var session = new GameSession(s);
            var before = session.State.Clone();
            session.Submit(Command.Tap(0, 0));
            Assert.AreEqual(1, session.State.Goals[1].Progress);
            Assert.IsTrue(session.Undo());
            Assert.IsTrue(before.ContentEquals(session.State));
            Assert.IsTrue(session.State.Board[0, 1].HasEnvelope);
        }
    }
}
