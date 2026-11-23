using System.Linq;
using KatKat.Core;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    /// <summary>
    /// 2026-11-24 yukarıdan dolma (kullanıcı: "torba bölümlerinde katlar boşaldıkça üstten alta dolsun. Belli sayıda yeni taşlar da en
    /// üstten oyuna girsin. Belli sayıda girsin ve ondan sonra girmesin. Yine üstten alta dolsun ama yeni taş girmesin").
    /// </summary>
    public class RefillRulesTests
    {
        private static LevelState WithRefill(LevelState s, int left)
        {
            s.Refill = new RefillRun { Left = left, MinLayers = 1, MaxLayers = 1 };
            s.Refill.Colors.Add(LayerColor.Yellow);
            return s;
        }

        [Test]
        public void EmptiedCells_FilledFromAbove_ThenNewStonesUntilBudgetEnds()
        {
            var s = WithRefill(State(9, new[] { "b", "c", "c", "c" }, (LayerColor.Coral, 99)), 2);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 1));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(LayerColor.Blue, s.Board[0, 3].Top, "mavi en alta kaydı");
            Assert.AreEqual(LayerColor.Yellow, s.Board[0, 2].Top, "yeni taş girdi");
            Assert.AreEqual(LayerColor.Yellow, s.Board[0, 1].Top, "yeni taş girdi");
            Assert.IsTrue(s.Board[0, 0].IsEmpty, "hak bitti: en üst boş kaldı");
            Assert.AreEqual(0, s.Refill.Left);
            Assert.AreEqual(1, r.Falls.Count(f => !f.Spawned));
            Assert.AreEqual(2, r.Falls.Count(f => f.Spawned));
            Assert.IsTrue(r.Falls.Where(f => f.Spawned).All(f => f.From.Y < 0), "yeni taş tahtanın üstünden gelir");
        }

        [Test]
        public void NoBudget_StillSlidesDown_NoNewStones()
        {
            var s = WithRefill(State(9, new[] { "b", "y", "c", "c", "c" }, (LayerColor.Coral, 99)), 0);
            RulesEngine.TryApply(s, Command.Tap(0, 2));
            Assert.AreEqual(LayerColor.Yellow, s.Board[0, 4].Top);
            Assert.AreEqual(LayerColor.Blue, s.Board[0, 3].Top);
            Assert.IsTrue(s.Board[0, 0].IsEmpty && s.Board[0, 1].IsEmpty && s.Board[0, 2].IsEmpty, "yeni taş girmez");
        }

        [Test]
        public void InactiveHole_IsSkipped_StonesFallPastIt()
        {
            var s = WithRefill(State(9, new[] { "b", "#", "c", "c", "c" }, (LayerColor.Coral, 99)), 0);
            RulesEngine.TryApply(s, Command.Tap(0, 2));
            Assert.AreEqual(LayerColor.Blue, s.Board[0, 4].Top, "boşluğun üstünden düştü");
            Assert.IsFalse(s.Board[0, 1].Active);
        }

        [Test]
        public void WithoutRefill_NothingMoves()
        {
            var s = State(9, new[] { "b", "c", "c", "c" }, (LayerColor.Coral, 99));
            var r = RulesEngine.TryApply(s, Command.Tap(0, 1));
            Assert.AreEqual(LayerColor.Blue, s.Board[0, 0].Top);
            Assert.AreEqual(0, r.Falls.Count);
        }

        [Test]
        public void Refill_IsDeterministic_AndClonedForUndo()
        {
            var a = WithRefill(State(9, new[] { "b", "c", "c", "c" }, (LayerColor.Coral, 99)), 3);
            a.Refill.MaxLayers = 3;
            a.Refill.Colors.Add(LayerColor.Purple);
            var b = a.Clone();
            Assert.IsTrue(a.ContentEquals(b));
            RulesEngine.TryApply(a, Command.Tap(0, 1));
            Assert.IsFalse(a.ContentEquals(b), "kopya etkilenmez (geri al)");
            RulesEngine.TryApply(b, Command.Tap(0, 1));
            Assert.IsTrue(a.ContentEquals(b), "aynı tohum aynı taşlar");
        }
    }
}
