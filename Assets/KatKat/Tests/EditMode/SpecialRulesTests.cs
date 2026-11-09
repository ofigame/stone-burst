using KatKat.Core;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>v5 KURALLAR §2–5: rulo roketi, gökkuşağı, birleşimler, domino sırası, puan, yıldız ve bitiş kutlaması.</summary>
    public class SpecialRulesTests
    {
        private static LevelState S(int moves, string[] rows, params (LayerColor, int)[] goals)
        {
            var s = TestBoards.State(moves, rows, goals);
            s.SpecialsEnabled = true;
            return s;
        }

        [Test]
        public void FiveWideGroup_CreatesRowRocket_InTray_NotOnBoard()
        {
            var s = S(5, new[]
            {
                "bc yc bc yc bc",
                "y  y  b  y  b",
            }, (LayerColor.Coral, 99));
            var r = RulesEngine.TryApply(s, Command.Tap(2, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(SpecialKind.RocketRow, r.SpecialCreated);
            Assert.AreEqual(new Coord(2, 0), r.SpecialCreatedAt, "uçuşun başladığı hücre");
            Assert.IsTrue(r.SpecialToTray);
            CollectionAssert.AreEqual(new[] { SpecialKind.RocketRow }, s.Tray);
            Assert.IsFalse(s.Board[2, 0].HasSpecial, "v6: özel taş tahtada durmaz");
            Assert.IsTrue(s.Board[2, 0].IsSelectable);
        }

        [Test]
        public void TrayRocket_FiresAtChosenCell_WithoutSpendingMove()
        {
            var s = S(5, new[] { "yb c yb", "c  y c", "b  c b" }, (LayerColor.Coral, 99));
            s.Tray.Add(SpecialKind.RocketColumn);
            var r = RulesEngine.TryApply(s, Command.UseSpecial(0, 1, 2));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(5, s.MovesLeft, "tepsi hamle harcamaz");
            Assert.AreEqual(0, s.Tray.Count);
            Assert.AreEqual(SpecialKind.RocketColumn, r.UsedSpecial);
            Assert.AreEqual(".", TestBoards.Cell(s, 1, 0));
            Assert.AreEqual(".", TestBoards.Cell(s, 1, 1));
            Assert.AreEqual(".", TestBoards.Cell(s, 1, 2));
            Assert.AreEqual("yb", TestBoards.Cell(s, 0, 0), "sütun dışı etkilenmez");
        }

        [Test]
        public void TrayGoldRoll_CollectsTargetColor()
        {
            var s = S(5, new[] { "yc b  yc", "b  yc y" }, (LayerColor.Coral, 99));
            s.Tray.Add(SpecialKind.Rainbow);
            RulesEngine.TryApply(s, Command.UseSpecial(0, 2, 0));
            Assert.AreEqual("y", TestBoards.Cell(s, 0, 0));
            Assert.AreEqual("y", TestBoards.Cell(s, 1, 1));
            Assert.AreEqual(3, s.Goals[0].Progress);
        }

        [Test]
        public void Tray_KeepsGameAliveAfterLastMove_AndRejectsUselessTarget()
        {
            var s = S(1, new[] { "c c c y", "y b y b" }, (LayerColor.Coral, 3), (LayerColor.Yellow, 9));
            s.Tray.Add(SpecialKind.RocketRow);
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(0, s.MovesLeft);
            Assert.AreEqual(GameStatus.Idle, s.Status, "hamle bitti ama tepside roket var");
            var bad = S(5, new[] { "c .", "y b" }, (LayerColor.Coral, 9));
            bad.Tray.Add(SpecialKind.Rainbow);
            Assert.IsFalse(RulesEngine.TryApply(bad, Command.UseSpecial(0, 1, 0)).Accepted, "boş hücrede Altın Rulo etkisiz");
            Assert.IsFalse(RulesEngine.TryApply(bad, Command.UseSpecial(1, 0, 0)).Accepted, "olmayan yuva");
        }

        [Test]
        public void Tray_IsCappedAtCapacity()
        {
            var s = S(9, new[] { "c c c c c", "b b b b b", "c c c c c", "b b b b b", "c c c c c" }, (LayerColor.Coral, 99));
            for (int i = 0; i < LevelState.TrayCapacity; i++) s.Tray.Add(SpecialKind.RocketRow);
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(SpecialKind.RocketRow, r.SpecialCreated);
            Assert.IsFalse(r.SpecialToTray);
            Assert.AreEqual(LevelState.TrayCapacity, s.Tray.Count);
        }

        [Test]
        public void TallGroup_CreatesColumnRocket_AndFourGroupCreatesNothing()
        {
            var s = S(5, new[] { "bc y", "bc y", "bc b", "bc y", "bc b" }, (LayerColor.Coral, 99));
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(SpecialKind.RocketColumn, r.SpecialCreated);

            var t = S(5, new[] { "bc bc bc bc y" }, (LayerColor.Coral, 99));
            var r2 = RulesEngine.TryApply(t, Command.Tap(0, 0));
            Assert.AreEqual(SpecialKind.None, r2.SpecialCreated);
        }

        [Test]
        public void EightGroup_CreatesRainbowWithGroupColor()
        {
            var s = S(5, new[] { "bc bc bc bc", "bc bc bc bc", "y b y b" }, (LayerColor.Coral, 99));
            var r = RulesEngine.TryApply(s, Command.Tap(1, 1));
            Assert.AreEqual(SpecialKind.Rainbow, r.SpecialCreated);
            Assert.AreEqual(LayerColor.Coral, r.SpecialCreatedColor);
            CollectionAssert.AreEqual(new[] { SpecialKind.Rainbow }, s.Tray);
        }

        [Test]
        public void SpecialsDisabled_NoSpecialCreated()
        {
            var s = TestBoards.State(5, new[] { "bc bc bc bc bc bc bc bc" }, (LayerColor.Coral, 99));
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(SpecialKind.None, r.SpecialCreated);
            Assert.IsFalse(s.Board[0, 0].HasSpecial);
        }

        [Test]
        public void RowRocket_PeelsTopOfEveryCellInRow_HitsCoversAndCostsOneMove()
        {
            var s = S(5, new[]
            {
                "yb c ybi2 pc .",
                "y  y y    b  b",
            }, (LayerColor.Coral, 99));
            s.Board[1, 0].Special = SpecialKind.RocketRow;
            var r = RulesEngine.TryApply(s, Command.Tap(1, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(4, s.MovesLeft);
            Assert.AreEqual(WaveKind.Special, r.Waves[0].Kind);
            Assert.AreEqual(SpecialKind.RocketRow, r.Waves[0].Fired);
            Assert.AreEqual("y", TestBoards.Cell(s, 0, 0));
            Assert.AreEqual(".", TestBoards.Cell(s, 1, 0), "roketin kendi hücresi de soyulur");
            Assert.AreEqual("yb", TestBoards.Cell(s, 2, 0), "örtülü yığın soyulmaz");
            Assert.AreEqual(1, s.Board[2, 0].IceHitsRemaining, "doğrudan vuruş: buz −1 (dalga başına bir)");
            Assert.AreEqual("p", TestBoards.Cell(s, 3, 0));
            Assert.IsFalse(s.Board[1, 0].HasSpecial);
        }

        [Test]
        public void Rocket_TriggersOtherSpecialInItsLine()
        {
            var s = S(5, new[]
            {
                "yb y yb",
                "c  b c",
                "b  c b",
            }, (LayerColor.Coral, 99));
            s.Board[0, 0].Special = SpecialKind.RocketRow;
            s.Board[2, 0].Special = SpecialKind.RocketColumn;
            // Komşu değiller (arada (1,0) var) → birleşim yok; satır roketi sütun roketini tetikler.
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves.Count, "satır dalgası + tetiklenen sütun dalgası");
            Assert.AreEqual(new Coord(2, 0), r.Waves[0].Triggered[0]);
            Assert.AreEqual(SpecialKind.RocketColumn, r.Waves[1].Fired);
            Assert.AreEqual("y", TestBoards.Cell(s, 2, 0));
            Assert.AreEqual(".", TestBoards.Cell(s, 2, 1));
            Assert.AreEqual(".", TestBoards.Cell(s, 2, 2));
        }

        [Test]
        public void Rainbow_PeelsAllOpenStacksOfItsColor_NotCovered()
        {
            var s = S(5, new[]
            {
                "yc b  yc",
                "b  yc ycLb2",
            }, (LayerColor.Coral, 99));
            s.Board[1, 0].Special = SpecialKind.Rainbow;
            s.Board[1, 0].SpecialColor = LayerColor.Coral;
            var r = RulesEngine.TryApply(s, Command.Tap(1, 0));
            Assert.AreEqual("y", TestBoards.Cell(s, 0, 0));
            Assert.AreEqual("y", TestBoards.Cell(s, 2, 0));
            Assert.AreEqual("y", TestBoards.Cell(s, 1, 1));
            Assert.AreEqual("yc", TestBoards.Cell(s, 2, 1), "kilitli yığın gökkuşağı alanında değil");
            Assert.AreEqual(3, s.Goals[0].Progress);
        }

        [Test]
        public void RocketPlusRocket_RowAndColumn()
        {
            var s = S(5, new[]
            {
                "c c c",
                "c b c",
                "c c c",
            }, (LayerColor.Coral, 99));
            s.Board[1, 1].Special = SpecialKind.RocketRow;
            s.Board[2, 1].Special = SpecialKind.RocketColumn;
            var r = RulesEngine.TryApply(s, Command.Tap(1, 1));
            Assert.AreEqual(SpecialKind.RocketColumn, r.Waves[0].ComboWith);
            Assert.AreEqual(".", TestBoards.Cell(s, 1, 0));
            Assert.AreEqual(".", TestBoards.Cell(s, 0, 1));
            Assert.AreEqual(".", TestBoards.Cell(s, 1, 2));
            Assert.AreEqual(".", TestBoards.Cell(s, 1, 1));
            Assert.AreEqual(".", TestBoards.Cell(s, 2, 1));
            Assert.AreEqual("c", TestBoards.Cell(s, 0, 0), "köşe etkilenmez");
            Assert.IsFalse(s.Board[2, 1].HasSpecial, "ortak taş da harcanır");
        }

        [Test]
        public void RainbowPlusRainbow_PeelsWholeBoardOnce()
        {
            var s = S(5, new[] { "yc yb y", "bc yy yp" }, (LayerColor.Coral, 99));
            s.Board[0, 0].Special = SpecialKind.Rainbow;
            s.Board[1, 0].Special = SpecialKind.Rainbow;
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual("y", TestBoards.Cell(s, 0, 0));
            Assert.AreEqual("y", TestBoards.Cell(s, 1, 0));
            Assert.AreEqual(".", TestBoards.Cell(s, 2, 0));
            Assert.AreEqual("b", TestBoards.Cell(s, 0, 1));
            Assert.AreEqual("y", TestBoards.Cell(s, 1, 1));
            Assert.AreEqual("y", TestBoards.Cell(s, 2, 1));
        }

        [Test]
        public void RocketPlusRainbow_TurnsColorStacksIntoRocketsFiredInOrder()
        {
            var s = S(9, new[]
            {
                "y  y  yb",
                "y  y  y",
                "yb y  y",
            }, (LayerColor.Blue, 99));
            s.Board[0, 0].Special = SpecialKind.RocketRow;
            s.Board[1, 0].Special = SpecialKind.Rainbow;
            s.Board[1, 0].SpecialColor = LayerColor.Blue;
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves[0].Triggered.Count, "iki mavi yığın rokete döndü");
            Assert.AreEqual(3, r.Waves.Count, "dönüşüm + iki roket");
            Assert.AreEqual(SpecialKind.RocketRow, r.Waves[1].Fired);
            Assert.AreEqual(SpecialKind.RocketColumn, r.Waves[2].Fired);
            Assert.AreEqual(2, s.Goals[0].Progress);
        }

        [Test]
        public void RocketOpensNewSameColorGroup_DominoContinues()
        {
            var s = S(5, new[]
            {
                "cb cb cb",
                "y  b  y",
            }, (LayerColor.Coral, 99));
            s.Board[0, 1].Special = SpecialKind.RocketColumn;
            // Sütun 0: (0,0) cb → c açılır. Domino için 3 yeni c gerekir; yalnızca 1 → domino yok.
            var r = RulesEngine.TryApply(s, Command.Tap(0, 1));
            Assert.AreEqual(1, r.Waves.Count);

            var t = S(5, new[] { "cb cb cb", "y y y" }, (LayerColor.Coral, 99));
            t.Board[0, 0].Special = SpecialKind.RocketRow;
            var r2 = RulesEngine.TryApply(t, Command.Tap(0, 0));
            // Satır 0: (1,0),(2,0) b soyulur → c,c; (0,0) roket hücresi de b → c: üç yeni mercan → domino.
            Assert.AreEqual(2, r2.Waves.Count);
            Assert.AreEqual(WaveKind.Chain, r2.Waves[1].Kind);
            Assert.AreEqual(".", TestBoards.Cell(t, 1, 0));
        }

        [Test]
        public void SpecialOnBoard_IsLegalMove_AndUndoRestoresIt()
        {
            var s = S(5, new[] { "y b y", "b y b" }, (LayerColor.Coral, 1));
            Assert.IsFalse(RulesEngine.HasLegalMove(s));
            s.Board[0, 0].Special = SpecialKind.RocketRow;
            Assert.IsTrue(RulesEngine.HasLegalMove(s));

            var session = new GameSession(s);
            session.Submit(Command.Tap(0, 0));
            Assert.IsFalse(session.State.Board[0, 0].HasSpecial);
            Assert.IsTrue(session.Undo());
            Assert.AreEqual(SpecialKind.RocketRow, session.State.Board[0, 0].Special);
            Assert.AreEqual(0, session.State.Score);
        }

        [Test]
        public void Score_GrowsWithWaveIndex_AndStarsUseThresholds()
        {
            Assert.AreEqual(10, Scoring.Layer(0));
            Assert.AreEqual(30, Scoring.Layer(2));
            var s = S(5, new[] { "cb cb cb", "y y y" }, (LayerColor.Coral, 99));
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            // 3 mavi (dalga 0) + 3 mercan domino (dalga 1): 3×10 + 3×20.
            Assert.AreEqual(90, r.ScoreGained);
            Assert.AreEqual(90, s.Score);
            Assert.AreEqual(0, Scoring.Stars(false, 999, new[] { 10, 20 }));
            Assert.AreEqual(1, Scoring.Stars(true, 5, new[] { 10, 20 }));
            Assert.AreEqual(2, Scoring.Stars(true, 10, new[] { 10, 20 }));
            Assert.AreEqual(3, Scoring.Stars(true, 20, new[] { 10, 20 }));
            Assert.AreEqual(3, Scoring.Stars(true, 0, null));
        }

        [Test]
        public void Celebrate_FiresLeftoverSpecialsThenOneRocketPerMove_Deterministic()
        {
            LevelState Make()
            {
                var s = S(6, new[] { "c c c y", "yb yb yb yb", "pb yp by cy" }, (LayerColor.Coral, 3));
                return s;
            }
            var a = Make();
            RulesEngine.TryApply(a, Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.Won, a.Status);
            a.Board[3, 1].Special = SpecialKind.RocketColumn;
            int moves = a.MovesLeft;
            var steps = RulesEngine.Celebrate(a, 42);
            Assert.AreEqual(GameStatus.Won, a.Status);
            Assert.AreEqual(0, a.MovesLeft);
            Assert.GreaterOrEqual(steps.Count, 2);
            Assert.AreEqual(SpecialKind.RocketColumn, steps[0].Waves[0].Fired, "önce tahtadaki özel taş");

            var b = Make();
            RulesEngine.TryApply(b, Command.Tap(0, 0));
            b.Board[3, 1].Special = SpecialKind.RocketColumn;
            RulesEngine.Celebrate(b, 42);
            Assert.IsTrue(a.ContentEquals(b), "aynı tohum aynı sonuç");
            Assert.Greater(a.Score, 30);
        }

        [Test]
        public void ExistingRulesUnchanged_WhenSpecialsDisabled_ChainStillWorks()
        {
            var s = TestBoards.State(5, new[] { "cb cb cb", "y y y" }, (LayerColor.Coral, 3));
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.AreEqual(GameStatus.Won, s.Status);
        }
    }
}
