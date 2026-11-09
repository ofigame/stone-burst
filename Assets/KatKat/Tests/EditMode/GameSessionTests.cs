using KatKat.Core;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    public class GameSessionTests
    {
        private static LevelState Sample()
        {
            var s = State(4, new[] { "pb pb pb pb pb", "y  c  y  c  y", "yc yc yc y  y" }, (LayerColor.Blue, 5), (LayerColor.Coral, 3));
            s.RollEnabled = true;
            return s;
        }

        [Test]
        public void Undo_RestoresEveryField_IncludingRollAndGoals()
        {
            var session = new GameSession(Sample());
            session.Submit(Command.Tap(0, 2));
            var before = session.State.Clone();
            var r = session.Submit(Command.Tap(0, 0));
            Assert.IsTrue(r.RollAwarded);
            Assert.IsFalse(session.State.ContentEquals(before));
            Assert.IsTrue(session.Undo());
            Assert.IsTrue(session.State.ContentEquals(before));
            Assert.IsFalse(session.State.RollSlotFull);
            Assert.IsFalse(session.CanUndo, "Geri alma tek adımlıdır.");
        }

        [Test]
        public void RejectedCommand_DoesNotReplaceUndoSnapshot()
        {
            var session = new GameSession(Sample());
            var start = session.State.Clone();
            session.Submit(Command.Tap(0, 0));
            Assert.IsFalse(session.Submit(Command.Tap(0, 1)).Accepted); // tek sarı: geçersiz
            session.Undo();
            Assert.IsTrue(session.State.ContentEquals(start));
        }

        [Test]
        public void Undo_WorksFromLossScreen()
        {
            var s = State(1, new[] { "c c c", "y y y" }, (LayerColor.Yellow, 3));
            var session = new GameSession(s);
            session.Submit(Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.LostOutOfMoves, session.State.Status);
            Assert.IsTrue(session.Undo());
            Assert.AreEqual(GameStatus.Idle, session.State.Status);
            Assert.AreEqual(1, session.State.MovesLeft);
        }

        [Test]
        public void Undo_ClosedAfterVictoryRecorded_RestartStillWorks()
        {
            var s = State(1, new[] { "c c c", "y b y" }, (LayerColor.Coral, 3));
            var session = new GameSession(s);
            session.Submit(Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.Won, session.State.Status);
            session.MarkVictoryRecorded();
            Assert.IsFalse(session.Undo());
            session.Restart();
            Assert.IsTrue(session.State.ContentEquals(s));
            Assert.IsFalse(session.CanUndo);
        }

        [Test]
        public void Session_DoesNotMutateInitialState()
        {
            var s = Sample();
            var copy = s.Clone();
            var session = new GameSession(s);
            session.Submit(Command.Tap(0, 0));
            Assert.IsTrue(s.ContentEquals(copy));
        }
    }
}
