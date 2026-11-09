using System.Linq;
using KatKat.Core;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    public class RulesEngineTests
    {
        private static readonly (LayerColor, int) FarGoal = (LayerColor.Purple, 99);

        [Test]
        public void TapOnSingleOrPair_IsRejected_NoMoveSpent_NoChange()
        {
            var s = State(5, new[]
            {
                "bc y  y",
                "p  c  p",
                "y  p  y",
            }, FarGoal);
            var before = s.Clone();
            var single = RulesEngine.TryApply(s, Command.Tap(0, 0));
            var pair = RulesEngine.TryApply(s, Command.Tap(1, 0));
            Assert.IsFalse(single.Accepted);
            Assert.AreEqual(RejectReason.GroupTooSmall, single.Reason);
            Assert.AreEqual(1, single.TappedGroup.Count);
            Assert.IsFalse(pair.Accepted);
            Assert.AreEqual(2, pair.TappedGroup.Count);
            Assert.IsTrue(s.ContentEquals(before));
        }

        [Test]
        public void DiagonalThree_IsNotAGroup()
        {
            var s = State(5, new[]
            {
                "c y y",
                "y c y",
                "y y c",
            }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(1, 1));
            Assert.IsFalse(r.Accepted);
            Assert.AreEqual(1, r.TappedGroup.Count);
        }

        [Test]
        public void TapOutsideOrOnEmptyOrInactive_IsRejected()
        {
            var s = State(5, new[] { "c c c", ". # c" }, FarGoal);
            Assert.AreEqual(RejectReason.OutOfBounds, RulesEngine.TryApply(s, Command.Tap(5, 0)).Reason);
            Assert.AreEqual(RejectReason.NotSelectable, RulesEngine.TryApply(s, Command.Tap(0, 1)).Reason);
            Assert.AreEqual(RejectReason.NotSelectable, RulesEngine.TryApply(s, Command.Tap(1, 1)).Reason);
            Assert.AreEqual(5, s.MovesLeft);
        }

        [Test]
        public void ConnectedGroup_WholeComponentLosesExactlyOneLayer()
        {
            var s = State(5, new[]
            {
                "yc pc yc b",
                "b  b  yc b",
                "b  pc pc b",
            }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(6, r.TappedGroup.Count);
            Assert.AreEqual("y", Cell(s, 0, 0));
            Assert.AreEqual("p", Cell(s, 1, 0));
            Assert.AreEqual("y", Cell(s, 2, 1));
            Assert.AreEqual("p", Cell(s, 1, 2));
            Assert.AreEqual("p", Cell(s, 2, 2));
            Assert.AreEqual("b", Cell(s, 0, 1));
            Assert.AreEqual(4, s.MovesLeft);
        }

        [Test]
        public void ThreeBlueUnderCoral_ChainsInOneMove_AndWinsOnLastMove()
        {
            var s = State(1, new[]
            {
                "bc bc bc y",
                "y  p  p  y",
            }, (LayerColor.Coral, 3), (LayerColor.Blue, 3));
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(WaveKind.Tap, r.Waves[0].Kind);
            Assert.AreEqual(WaveKind.Chain, r.Waves[1].Kind);
            Assert.AreEqual(6, r.TotalPeeled);
            Assert.AreEqual(0, s.MovesLeft);
            Assert.AreEqual(GameStatus.Won, s.Status);
            CollectionAssert.AreEqual(new[] { 3, 0 }, r.Waves[0].GoalProgressAfter);
            CollectionAssert.AreEqual(new[] { 3, 3 }, r.Waves[1].GoalProgressAfter);
        }

        [Test]
        public void DifferentColorsRevealed_NoChain()
        {
            var s = State(5, new[] { "bc yc bc", "p  p  y" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(1, r.Waves.Count);
            Assert.AreEqual("b y b", $"{Cell(s, 0, 0)} {Cell(s, 1, 0)} {Cell(s, 2, 0)}");
        }

        [Test]
        public void ExistingGroupElsewhere_IsNotAutoCleared()
        {
            var s = State(5, new[]
            {
                "pc yc pc b",
                "b  b  b  y",
                "y  y  y  b",
            }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(1, r.Waves.Count);
            Assert.AreEqual("b", Cell(s, 0, 1));
            Assert.AreEqual("y", Cell(s, 0, 2));
        }

        [Test]
        public void NewGroupNextToExistingSameColor_PeelsOnlyNewStones()
        {
            // v13 zincir: üç yeni mavi, önceden açık maviye (3,0) değse de birlikte çıktıkları için patlar; eski mavi katılmaz.
            var s = State(5, new[]
            {
                "bc bc bc b",
                "y  p  y  p",
            }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(WaveKind.Chain, r.Waves[1].Kind);
            Assert.AreEqual(3, r.Waves[1].Cells.Count);
            Assert.AreEqual(". . . b", $"{Cell(s, 0, 0)} {Cell(s, 1, 0)} {Cell(s, 2, 0)} {Cell(s, 3, 0)}");
            Assert.AreEqual(GameStatus.LostNoLegalMoves, s.Status, "küçük tahtada 3'lü grup kalmadı");
        }

        [Test]
        public void SameColorRevealedUnderSameColor_IsNotNew_NoChain()
        {
            var s = State(5, new[] { "cc cc cc", "y  p  y" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(1, r.Waves.Count);
            Assert.AreEqual("c", Cell(s, 1, 0));
        }

        [Test]
        public void TwoSeparateNewGroupsInSameWave_BothPeelOnceInSameChainWave()
        {
            var s = State(5, new[]
            {
                "bc bc bc yc",
                "p  p  p  yc",
                "yc yc yc yc",
            }, FarGoal);
            // Coral grubu: üst satır 4 + (3,1) + alt satır 4 = 9 hücre.
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(9, r.TappedGroup.Count);
            Assert.AreEqual(2, r.Waves.Count);
            // Maviler (3) ve sarılar (6) ayrı bileşenler ama aynı zincir dalgasında.
            Assert.AreEqual(9, r.Waves[1].Cells.Count);
            Assert.AreEqual(18, r.TotalPeeled);
        }

        [Test]
        public void MultiWaveChain_ContinuesUntilNoCandidate()
        {
            var s = State(5, new[] { "ybc ybc ybc", "p   p   p" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(3, r.Waves.Count);
            Assert.AreEqual(".", Cell(s, 0, 0));
            Assert.AreEqual(9, r.TotalPeeled);
        }

        [Test]
        public void ChainSeedRequiresPeeledCellInComponent()
        {
            // Mercan soyulunca hiçbir şey açılmaz (boşalır); uzaktaki hazır sarı grup tetiklenmez.
            var s = State(5, new[] { "c c c", "y y y" }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(1, r.Waves.Count);
            Assert.AreEqual("y", Cell(s, 0, 1));
        }

        [Test]
        public void RollAward_FourGivesNone_FiveGivesOne()
        {
            var four = State(5, new[] { "c c c c", "y b y b" }, FarGoal);
            four.RollEnabled = true;
            Assert.IsFalse(RulesEngine.TryApply(four, Command.Tap(0, 0)).RollAwarded);
            Assert.IsFalse(four.RollSlotFull);

            var five = State(5, new[] { "c c c c c", "y b y b y" }, FarGoal);
            five.RollEnabled = true;
            Assert.IsTrue(RulesEngine.TryApply(five, Command.Tap(0, 0)).RollAwarded);
            Assert.IsTrue(five.RollSlotFull);
        }

        [Test]
        public void RollAward_DisabledFeature_GivesNone()
        {
            var s = State(5, new[] { "c c c c c", "y b y b y" }, FarGoal);
            Assert.IsFalse(RulesEngine.TryApply(s, Command.Tap(0, 0)).RollAwarded);
            Assert.IsFalse(s.RollSlotFull);
        }

        [Test]
        public void RollAward_FullSlot_KeepsSingleRoll()
        {
            var s = State(5, new[] { "c c c c c", "y b y b y" }, FarGoal);
            s.RollEnabled = true;
            s.RollSlotFull = true;
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsFalse(r.RollAwarded);
            Assert.IsTrue(s.RollSlotFull);
        }

        [Test]
        public void ChainOfFive_AwardsNoRoll()
        {
            // Rulo bir satırı soyar; altta açılan beşli mavi zincir otomatik soyulur ama rulo üretmez.
            var s = State(5, new[] { "bc bc bc bc bc", "y  p  y  p  y" }, FarGoal);
            s.RollEnabled = true;
            s.RollSlotFull = true;
            var r = RulesEngine.TryApply(s, Command.Roll(0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(5, r.Waves[1].Cells.Count);
            Assert.IsFalse(r.RollAwarded);
            Assert.IsFalse(s.RollSlotFull);
        }

        [Test]
        public void FiveTapWithFiveChain_AwardsExactlyOneRoll()
        {
            var s = State(5, new[] { "pb pb pb pb pb", "c  y  c  y  c" }, FarGoal);
            s.RollEnabled = true;
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.RollAwarded);
            Assert.AreEqual(2, r.Waves.Count);
            Assert.IsTrue(s.RollSlotFull);
        }

        [Test]
        public void ChainWaves_NeverExceedPreviousWave_RollOnlyFromDirectGroup()
        {
            var s = State(5, new[] { "bpc bpc bpc", "y   y   y" }, FarGoal);
            s.RollEnabled = true;
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(3, r.Waves.Count);
            Assert.AreEqual(9, r.TotalPeeled);
            Assert.IsFalse(r.RollAwarded, "Toplam 9 katman ama doğrudan grup 3: rulo yok.");
            for (int i = 1; i < r.Waves.Count; i++) Assert.LessOrEqual(r.Waves[i].Cells.Count, r.Waves[i - 1].Cells.Count);
        }

        [Test]
        public void Roll_EmptySlot_NoEffectRow_AreRejected_KeepMovesAndRoll()
        {
            var s = State(5, new[] { "c y b", "# . #", "c c c" }, FarGoal);
            Assert.AreEqual(RejectReason.RollEmpty, RulesEngine.TryApply(s, Command.Roll(0)).Reason);
            s.RollEnabled = true;
            s.RollSlotFull = true;
            var r = RulesEngine.TryApply(s, Command.Roll(1));
            Assert.AreEqual(RejectReason.RollNoEffect, r.Reason);
            Assert.AreEqual(RejectReason.OutOfBounds, RulesEngine.TryApply(s, Command.Roll(3)).Reason);
            Assert.AreEqual(5, s.MovesLeft);
            Assert.IsTrue(s.RollSlotFull);
        }

        [Test]
        public void Roll_Valid_SpendsMoveAndRoll_PeelsOneLayerPerEligibleStack_SkippingGaps()
        {
            var s = State(5, new[] { "yc # . bp yy", "p  p p p  p" }, FarGoal);
            s.RollEnabled = true;
            s.RollSlotFull = true;
            var r = RulesEngine.TryApply(s, Command.Roll(0));
            Assert.IsTrue(r.Accepted);
            Assert.IsTrue(r.RollConsumed);
            Assert.IsFalse(s.RollSlotFull);
            Assert.AreEqual(4, s.MovesLeft);
            Assert.AreEqual(WaveKind.Roll, r.Waves[0].Kind);
            Assert.AreEqual(3, r.Waves[0].Cells.Count);
            Assert.AreEqual("y # . b y", $"{Cell(s, 0, 0)} {Cell(s, 1, 0)} {Cell(s, 2, 0)} {Cell(s, 3, 0)} {Cell(s, 4, 0)}");
        }

        [Test]
        public void Roll_UsesSameChainSystem()
        {
            var s = State(5, new[] { "bc bc bc", "y  p  y" }, (LayerColor.Blue, 3));
            s.RollEnabled = true;
            s.RollSlotFull = true;
            var r = RulesEngine.TryApply(s, Command.Roll(0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(GameStatus.Won, s.Status);
            Assert.IsFalse(r.RollAwarded, "Rulo kullanımı yeni rulo üretmez.");
        }

        [Test]
        public void LastMoveCompletingGoals_IsWon_NotLost()
        {
            var s = State(1, new[] { "c c c", "y b y" }, (LayerColor.Coral, 3));
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(0, s.MovesLeft);
            Assert.AreEqual(GameStatus.Won, s.Status);
        }

        [Test]
        public void ZeroMovesWithoutGoals_IsOutOfMoves()
        {
            var s = State(1, new[] { "c c c", "y y y" }, (LayerColor.Yellow, 3));
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.LostOutOfMoves, s.Status);
            Assert.AreEqual(RejectReason.NotIdle, RulesEngine.TryApply(s, Command.Tap(0, 1)).Reason);
        }

        [Test]
        public void NoGroupButFullRollWithEffectiveRow_ContinuesIdle()
        {
            var s = State(5, new[] { "c c c", "y b p" }, FarGoal);
            s.RollEnabled = true;
            s.RollSlotFull = true;
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.Idle, s.Status);
        }

        [Test]
        public void NoGroupAndNoRoll_IsNoLegalMoves()
        {
            var s = State(5, new[] { "c c c", "y b p" }, FarGoal);
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.LostNoLegalMoves, s.Status);
        }

        [Test]
        public void InvalidCommand_DoesNotMutateState()
        {
            var s = State(3, new[] { "c y c", "y c y" }, FarGoal);
            s.RollEnabled = true;
            var before = s.Clone();
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            RulesEngine.TryApply(s, Command.Tap(-1, 0));
            RulesEngine.TryApply(s, Command.Roll(0));
            Assert.IsTrue(s.ContentEquals(before));
        }

        [Test]
        public void SameStateAndCommand_GiveSameResult()
        {
            var a = State(5, new[] { "ybc ybc ybc p", "p   cy  c   p", "pc  pc  pc  y" }, FarGoal);
            var b = a.Clone();
            var ra = RulesEngine.TryApply(a, Command.Tap(0, 0));
            var rb = RulesEngine.TryApply(b, Command.Tap(0, 0));
            Assert.IsTrue(a.ContentEquals(b));
            Assert.AreEqual(ra.Waves.Count, rb.Waves.Count);
            for (int i = 0; i < ra.Waves.Count; i++)
                CollectionAssert.AreEqual(ra.Waves[i].Cells.Select(c => (c.X, c.Y, c.Removed)), rb.Waves[i].Cells.Select(c => (c.X, c.Y, c.Removed)));
        }

        [Test]
        public void EventPlan_MatchesStateChange_AndNeverInventsColors()
        {
            var s = State(5, new[] { "ybc ybc ybc p", "p   cy  c   p", "pc  pc  pc  y" }, (LayerColor.Coral, 2), (LayerColor.Blue, 1));
            var before = s.Clone();
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(before.Board.TotalLayers() - r.TotalPeeled, s.Board.TotalLayers());
            foreach (LayerColor col in System.Enum.GetValues(typeof(LayerColor)))
            {
                int removed = r.Waves.Sum(w => w.Cells.Count(c => c.Removed == col));
                Assert.AreEqual(before.Board.CountLayers(col) - removed, s.Board.CountLayers(col), col.ToString());
            }
            // Her dalgada hücreler y, sonra x sırasında; aynı dalgada bir hücre bir kez.
            foreach (var w in r.Waves)
            {
                var keys = w.Cells.Select(c => c.Y * 100 + c.X).ToList();
                CollectionAssert.AreEqual(keys.OrderBy(k => k), keys);
                CollectionAssert.AllItemsAreUnique(keys);
            }
            // Son dalganın hedef görüntüsü durumla aynı; hedef sayaçları yalnızca kurallarda artar.
            CollectionAssert.AreEqual(s.Goals.Select(g => g.Progress), r.Waves.Last().GoalProgressAfter);
            Assert.AreEqual(r.Waves.Sum(w => w.Cells.Count(c => c.Removed == LayerColor.Coral)), s.Goals[0].Progress);
        }

        [Test]
        public void GoalDisplay_IsCappedAtAmount_LogicKeepsTotal()
        {
            var s = State(5, new[] { "c c c c", "y b y b" }, (LayerColor.Coral, 2));
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(4, s.Goals[0].Progress);
            Assert.AreEqual(2, s.Goals[0].DisplayProgress);
        }
    }
}
