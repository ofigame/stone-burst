using KatKat.Core;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Kalp sistemi (kullanıcı, 2026-11-09) ve reklam ödülü taş yerleştirme.</summary>
    public class HeartsTests
    {
        [Test]
        public void Hearts_FullByDefault_SpendStartsTimer_RegenEvery30Min_CapsAtMax()
        {
            var m = new MetaSave();
            const long t = 1_000_000;
            Assert.AreEqual(5, Hearts.Count(m, t));
            Assert.AreEqual(0, Hearts.SecondsToNext(m, t));
            Assert.IsTrue(Hearts.Spend(m, t));
            Assert.IsTrue(Hearts.Spend(m, t + 60));
            Assert.AreEqual(3, Hearts.Count(m, t + 60));
            Assert.AreEqual(Hearts.RegenSeconds, Hearts.SecondsToNext(m, t), "sayaç ilk harcamada başlar");
            Assert.AreEqual(4, Hearts.Count(m, t + Hearts.RegenSeconds), "30 dk sonra +1");
            Assert.AreEqual(Hearts.RegenSeconds, Hearts.SecondsToNext(m, t + Hearts.RegenSeconds));
            Assert.AreEqual(5, Hearts.Count(m, t + 10 * Hearts.RegenSeconds), "en çok 5");
            Assert.AreEqual(0, m.heartsMissing);
        }

        [Test]
        public void Hearts_EmptyCannotSpend_RefillFills_ClockBackwardsSafe()
        {
            var m = new MetaSave();
            const long t = 5_000_000;
            for (int i = 0; i < 5; i++) Assert.IsTrue(Hearts.Spend(m, t));
            Assert.AreEqual(0, Hearts.Count(m, t));
            Assert.IsFalse(Hearts.Spend(m, t));
            Assert.AreEqual(0, Hearts.Count(m, t - 99999), "saat geri alınınca kalp eksilmez, sayaç yeniden başlar");
            Hearts.Refill(m);
            Assert.AreEqual(5, Hearts.Count(m, t));
            Assert.AreEqual("29:59", Hearts.Clock(29 * 60 + 59));
            Assert.IsFalse(Hearts.LevelUses(19));
            Assert.IsTrue(Hearts.LevelUses(20));
        }

        [Test]
        public void PlaceLayer_AddsTopColor_OnlyOnFreeActiveStack_ClearsUndo()
        {
            var b = new BoardState(2, 1);
            b[0, 0].Active = true; b[0, 0].Layers.Add(LayerColor.Blue);
            b[1, 0].Active = true; b[1, 0].Layers.Add(LayerColor.Coral); b[1, 0].IceHitsRemaining = 1;
            var s = new GameSession(new LevelState { Board = b, MovesLeft = 1, Status = GameStatus.Idle });
            Assert.IsTrue(s.PlaceLayer(0, 0, LayerColor.Yellow));
            Assert.AreEqual(LayerColor.Yellow, s.State.Board[0, 0].Top);
            Assert.AreEqual(2, s.State.Board[0, 0].Layers.Count);
            Assert.IsFalse(s.PlaceLayer(1, 0, LayerColor.Yellow), "buzlu yığına konmaz");
            Assert.IsFalse(s.PlaceLayer(5, 0, LayerColor.Yellow), "tahta dışı");
            Assert.IsFalse(s.CanUndo);
        }
    }
}
