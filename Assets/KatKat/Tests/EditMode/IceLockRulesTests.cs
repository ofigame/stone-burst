using System.Linq;
using KatKat.Core;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    /// <summary>Premium-v2 Aşama B: buz kapağı (M05) ve renk kilidi (M06), rehber §10 ve §14 test matrisi.</summary>
    public class IceLockRulesTests
    {
        private static readonly (LayerColor, int) FarGoal = (LayerColor.Purple, 99);

        // ---- Buz -------------------------------------------------------------------------------------

        [Test]
        public void IcedStack_IsNotSelectable_AndSplitsGroups()
        {
            var s = State(5, new[] { "c ci1 c", "y y   b" }, FarGoal);
            Assert.AreEqual(RejectReason.NotSelectable, RulesEngine.TryApply(s, Command.Tap(1, 0)).Reason);
            Assert.AreEqual(RejectReason.GroupTooSmall, RulesEngine.TryApply(s, Command.Tap(0, 0)).Reason);
            Assert.AreEqual(5, s.MovesLeft);
        }

        [Test]
        public void FourNeighboursPeeledInSameWave_GiveOneIceHit()
        {
            var s = State(5, new[] { "c c   c", "c yi2 c", "c c   c" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(1, r.Waves.Count);
            Assert.AreEqual(1, r.Waves[0].IceHits.Count);
            Assert.AreEqual(1, s.Board[1, 1].IceHitsRemaining);
            Assert.AreEqual("y", Cell(s, 1, 1));
        }

        [Test]
        public void IceBreaks_StackStaysIntact_NotPeeledNorChainSeed()
        {
            // v13: mercanlar soyulur, üç sarı birlikte açılıp zincirle patlar; buzu kırılan hücrenin sarısı soyulmadığı
            // için zincire katılmaz, yerinde kalır.
            var s = State(5, new[] { "yc yc  yc", "b  yi1 b" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(0, r.Waves[0].IceHits.Single().RemainingAfter);
            Assert.AreEqual("y", Cell(s, 1, 1));
            Assert.AreEqual(0, s.Board[1, 1].IceHitsRemaining);
            Assert.IsTrue(s.Board[1, 1].IsSelectable);
        }

        [Test]
        public void TwoChainWavesNextToIce_GiveTwoHits()
        {
            var s = State(5, new[] { "bc bc  bc", "y  bi2 y" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(1, r.Waves[0].IceHits.Single().RemainingAfter);
            Assert.AreEqual(0, r.Waves[1].IceHits.Single().RemainingAfter);
            Assert.AreEqual("b", Cell(s, 1, 1));
        }

        [Test]
        public void IceBreaking_DoesNotCountTowardGoals()
        {
            var s = State(5, new[] { "c c c", "y yi1 y" }, (LayerColor.Yellow, 1));
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(0, s.Goals[0].Progress);
            Assert.AreEqual(GameStatus.Idle, s.Status);
        }

        [Test]
        public void Roll_SkipsIce_ButNeighbourPeelDamagesIt()
        {
            var s = State(5, new[] { "bc bci1 bc", "y y y" }, FarGoal);
            s.RollEnabled = true;
            s.RollSlotFull = true;
            var r = RulesEngine.TryApply(s, Command.Roll(0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(2, r.Waves[0].Cells.Count);
            Assert.AreEqual("bc", Cell(s, 1, 0));
            Assert.AreEqual(0, s.Board[1, 0].IceHitsRemaining);
        }

        // ---- Renk kilidi -------------------------------------------------------------------------------

        [Test]
        public void Lock_CountsMatchingLayersAnywhere_WithoutAdjacency()
        {
            // Kilit sol üstte; dört mercan uzakta, kilide komşu değil.
            var s = State(5, new[] { "ybLc6 y y y", "p p p p", "c c c c" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 2));
            var hit = r.Waves[0].LockHits.Single();
            Assert.AreEqual(4, hit.Amount);
            Assert.AreEqual(2, hit.RemainingAfter);
            Assert.AreEqual(2, s.Board[0, 0].LockRemaining);
            Assert.IsFalse(s.Board[0, 0].IsSelectable);
        }

        [Test]
        public void Lock_WrongColorDoesNotCount()
        {
            var s = State(5, new[] { "ybLc3 y y", "p p p" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 1));
            Assert.AreEqual(0, r.Waves[0].LockHits.Count);
            Assert.AreEqual(3, s.Board[0, 0].LockRemaining);
        }

        [Test]
        public void Lock_StopsAtZero_AndOpenedStackIsNotPeeledOrCounted()
        {
            // Kilit 2 ister, 3 mercan kalkar: düşüş 2, sayaç 0. Kilitli hücrenin üstü de mercan; aynı dalgada
            // soyulmaz, hedefe sayılmaz, zincir tohumu olmaz.
            var s = State(5, new[] { "bcLc2 b b", "c c c" }, (LayerColor.Coral, 9));
            var r = RulesEngine.TryApply(s, Command.Tap(0, 1));
            Assert.AreEqual(1, r.Waves.Count);
            var hit = r.Waves[0].LockHits.Single();
            Assert.AreEqual(2, hit.Amount);
            Assert.AreEqual(0, hit.RemainingAfter);
            Assert.AreEqual("bc", Cell(s, 0, 0));
            Assert.AreEqual(3, s.Goals[0].Progress);
            Assert.IsTrue(s.Board[0, 0].IsSelectable);
        }

        [Test]
        public void Lock_CountsEachChainWaveSeparately()
        {
            // Dalga 0: 3 mercan, dalga 1 (zincir): 3 mavi, dalga 2 (zincir): 3 mercan → kilit 6 → 3 → 3 → 0.
            var s = State(5, new[] { "pbLc6 p p", "cbc cbc cbc", "y y y" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 1));
            Assert.AreEqual(3, r.Waves.Count);
            Assert.AreEqual(3, r.Waves[0].LockHits.Single().RemainingAfter);
            Assert.AreEqual(0, r.Waves[1].LockHits.Count);
            Assert.AreEqual(0, r.Waves[2].LockHits.Single().RemainingAfter);
        }

        [Test]
        public void Lock_CountsRollRemovals_AndRollSkipsLockedStack()
        {
            var s = State(5, new[] { "yc ycLc1 yc", "p p p" }, FarGoal);
            s.RollEnabled = true;
            s.RollSlotFull = true;
            var r = RulesEngine.TryApply(s, Command.Roll(0));
            Assert.AreEqual(2, r.Waves[0].Cells.Count);
            Assert.AreEqual(0, s.Board[1, 0].LockRemaining);
            Assert.AreEqual("yc", Cell(s, 1, 0));
        }

        [Test]
        public void Undo_RestoresIceAndLock()
        {
            var s = State(5, new[] { "c c c", "yi1 yLc3 y" }, FarGoal);
            var session = new GameSession(s);
            var before = session.State.Clone();
            Assert.IsTrue(session.Submit(Command.Tap(0, 0)).Accepted);
            Assert.AreEqual(0, session.State.Board[0, 1].IceHitsRemaining);
            Assert.AreEqual(0, session.State.Board[1, 1].LockRemaining);
            Assert.IsTrue(session.Undo());
            Assert.IsTrue(before.ContentEquals(session.State));
            Assert.AreEqual(1, session.State.Board[0, 1].IceHitsRemaining);
            Assert.AreEqual(3, session.State.Board[1, 1].LockRemaining);
        }

        [Test]
        public void NoGroupButCoveredStacks_IsNoLegalMoves()
        {
            // Yasal komut yalnızca grup veya etkili rulodur; örtülü yığınlar kendiliğinden hamle sayılmaz.
            var s = State(5, new[] { "c c c", "yi2 b p" }, FarGoal);
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.LostNoLegalMoves, s.Status);
        }
    }
}
