using System.Linq;
using KatKat.Core;
using KatKat.Levels;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    /// <summary>Faz 5 kuralları: ataş (belge Bölüm 7), dip yıldızı ve collectStars hedefi (Bölüm 5 adım 7, Bölüm 9).</summary>
    public class ClipStarRulesTests
    {
        private static readonly (LayerColor, int) FarGoal = (LayerColor.Purple, 99);

        private static LevelState WithStarGoal(LevelState s, int amount)
        {
            s.Goals.Add(new GoalState { Type = GoalType.CollectStars, Amount = amount });
            return s;
        }

        [Test]
        public void ClippedStack_IsNotSelectable_AndDoesNotJoinGroups()
        {
            var s = State(5, new[]
            {
                "c ck1 c",
                "y y   b",
            }, FarGoal);
            Assert.AreEqual(RejectReason.NotSelectable, RulesEngine.TryApply(s, Command.Tap(1, 0)).Reason);
            // Ataş ortadaki mercanı gruptan çıkarır: soldaki ve sağdaki mercanlar tekil kalır.
            Assert.AreEqual(RejectReason.GroupTooSmall, RulesEngine.TryApply(s, Command.Tap(0, 0)).Reason);
            Assert.AreEqual(5, s.MovesLeft);
        }

        [Test]
        public void FourNeighboursPeeledInSameWave_GiveOneHit()
        {
            var s = State(5, new[]
            {
                "c c   c",
                "c yk2 c",
                "c c   c",
            }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(1, r.Waves.Count);
            Assert.AreEqual(8, r.Waves[0].Cells.Count);
            Assert.AreEqual(1, r.Waves[0].ClipHits.Count);
            Assert.AreEqual(new[] { 1 }, r.Waves[0].ClipHits.Select(h => h.RemainingAfter).ToArray());
            Assert.AreEqual(1, s.Board[1, 1].ClipHitsRemaining);
            Assert.AreEqual("y", Cell(s, 1, 1));
        }

        [Test]
        public void TwoChainWavesNextToClip_GiveTwoHits_UnlockedLayerNotPeeledInSameWave()
        {
            var s = State(5, new[]
            {
                "bc bc  bc",
                "y  bk2 y",
            }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(WaveKind.Chain, r.Waves[1].Kind);
            Assert.AreEqual(1, r.Waves[0].ClipHits.Single().RemainingAfter);
            Assert.AreEqual(0, r.Waves[1].ClipHits.Single().RemainingAfter);
            // Açılan ataşın mavisi ikinci dalgada soyulmadı ve sonra da zincire katılmadı.
            Assert.IsFalse(r.Waves.SelectMany(w => w.Cells).Any(c => c.X == 1 && c.Y == 1));
            Assert.AreEqual("b", Cell(s, 1, 1));
            Assert.AreEqual(0, s.Board[1, 1].ClipHitsRemaining);
            Assert.AreEqual(4, s.MovesLeft);
        }

        [Test]
        public void UnlockedCell_IsNotChainSeed_AndDoesNotJoinChain()
        {
            // v13 zincir: mercanlar soyulunca üç sarı birlikte açılır ve patlar. Açılan ataşın sarısı onlara bağlı olsa da
            // soyulmadığı için zincire katılmaz; yerinde kalır.
            var s = State(5, new[]
            {
                "yc yc  yc",
                "#  yk1 #",
            }, FarGoal);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(3, r.Waves[1].Cells.Count);
            Assert.AreEqual(0, s.Board[1, 1].ClipHitsRemaining);
            Assert.AreEqual("y", Cell(s, 1, 1));
            Assert.AreEqual(GameStatus.LostNoLegalMoves, s.Status, "küçük tahtada 3'lü grup kalmadı");
        }

        [Test]
        public void UnlockingClip_DoesNotCountAsCollecting()
        {
            var s = State(5, new[]
            {
                "c c c",
                "# bk1 #",
            }, (LayerColor.Blue, 1));
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(0, s.Board[1, 1].ClipHitsRemaining);
            Assert.AreEqual(0, s.Goals[0].Progress);
            Assert.AreEqual(GameStatus.LostNoLegalMoves, s.Status);
        }

        [Test]
        public void Roll_SkipsClippedStack_ButDamagesItThroughNeighbours()
        {
            var s = State(5, new[]
            {
                "yc ck1 yb",
                "p  b   p",
            }, FarGoal);
            s.RollEnabled = true;
            s.RollSlotFull = true;
            var r = RulesEngine.TryApply(s, Command.Roll(0));
            Assert.IsTrue(r.Accepted);
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, r.Waves[0].Cells.Select(c => c.X));
            Assert.AreEqual(0, s.Board[1, 0].ClipHitsRemaining);
            Assert.AreEqual("c", Cell(s, 1, 0));
        }

        [Test]
        public void Roll_RowWithOnlyClippedStacks_IsNoEffect()
        {
            var s = State(5, new[]
            {
                "ck1 . #",
                "c   c c",
            }, FarGoal);
            s.RollEnabled = true;
            s.RollSlotFull = true;
            Assert.AreEqual(RejectReason.RollNoEffect, RulesEngine.TryApply(s, Command.Roll(0)).Reason);
            Assert.AreEqual(5, s.MovesLeft);
            Assert.IsTrue(s.RollSlotFull);
        }

        [Test]
        public void StarCell_LastLayer_GivesOneStar_NotCollectedTwice()
        {
            var s = WithStarGoal(State(5, new[]
            {
                "bc* bc bc",
                "y   p  y",
            }), 1);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            // Üst mercan kalkar: hücre boşalmadı, yıldız yok. Maviler zincirle soyulur: yıldız bir kez.
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(0, r.Waves[0].StarsCollected);
            Assert.AreEqual(1, r.Waves[1].StarsCollected);
            Assert.IsTrue(r.Waves[1].Cells.Single(c => c.X == 0).StarCollected);
            Assert.AreEqual(new[] { 0 }, r.Waves[0].GoalProgressAfter);
            Assert.AreEqual(new[] { 1 }, r.Waves[1].GoalProgressAfter);
            Assert.IsFalse(s.Board[0, 0].HasBottomStar);
            Assert.AreEqual(GameStatus.Won, s.Status);
        }

        [Test]
        public void StarsGoal_NeedsAllStars_WithColorGoal()
        {
            var s = WithStarGoal(State(5, new[]
            {
                "c* c c",
                "y* y y",
            }, (LayerColor.Coral, 3)), 2);
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(1, s.Goals[1].Progress);
            Assert.AreEqual(GameStatus.Idle, s.Status);
            RulesEngine.TryApply(s, Command.Tap(0, 1));
            Assert.AreEqual(2, s.Goals[1].Progress);
            Assert.AreEqual(GameStatus.Won, s.Status);
        }

        [Test]
        public void Undo_RestoresClipsAndStars()
        {
            var start = WithStarGoal(State(5, new[]
            {
                "c* c   c",
                "y  yk2 y",
            }), 1);
            start.Goals.Insert(0, new GoalState { Type = GoalType.CollectColor, Color = LayerColor.Yellow, Amount = 3 });
            var session = new GameSession(start.Clone());
            var r = session.Submit(Command.Tap(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(1, session.State.Board[1, 1].ClipHitsRemaining);
            Assert.IsFalse(session.State.Board[0, 0].HasBottomStar);
            Assert.IsTrue(session.Undo());
            Assert.IsTrue(session.State.ContentEquals(start));
        }

        [Test]
        public void Validation_SecondStageScope_AllowsClipsStarsAndSixBySix_FirstReleaseRejects()
        {
            var d = new LevelData { SchemaVersion = 1, Id = "level_041", Width = 6, Height = 6, MoveLimit = 3 };
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 6; x++)
                {
                    var c = new CellData { X = x, Y = y, Active = !(x == 5 && y == 5) };
                    if (c.Active) c.Layers.Add(y == 0 ? "coral" : "blue");
                    d.Cells.Add(c);
                }
            d.Cells[7].ClipHitsRemaining = 2;
            d.Cells[8].HasBottomStar = true;
            d.Cells[9].Layers.AddRange(new[] { "yellow", "coral", "blue" });
            d.Goals.Add(new GoalData { Type = "collectStars", Amount = 1 });
            d.Goals.Add(new GoalData { Type = "collectColor", Color = "coral", Amount = 6 });

            Assert.AreSame(LevelValidator.SecondStage, LevelValidator.ScopeFor(d));
            CollectionAssert.IsEmpty(LevelValidator.ValidateStatic(d, LevelValidator.SecondStage));
            var first = string.Join("\n", LevelValidator.ValidateStatic(d, LevelValidator.FirstRelease));
            StringAssert.Contains("6x6", first);
            StringAssert.Contains("ataş", first);
            StringAssert.Contains("yıldız", first);
            StringAssert.Contains("4 katman", first);

            d.Goals[0].Amount = 2;
            StringAssert.Contains("yıldız hedefi 2", string.Join("\n", LevelValidator.ValidateStatic(d, LevelValidator.SecondStage)));
            Assert.AreSame(LevelValidator.FirstRelease, LevelValidator.ScopeFor(new LevelData { Id = "level_020" }));
        }
    }
}
