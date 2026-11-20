using System.Collections.Generic;
using KatKat.Core;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Mağaza, külçe ve güçlendiriciler (2026-11-20; kullanıcı: "Evet başla").</summary>
    public class ShopTests
    {
        private static StoneCatalog Catalog() => StoneCatalog.Parse(
            "{\"schemaVersion\":2,\"stones\":[" +
            "{\"stoneId\":\"a\",\"ingots\":150,\"nameKey\":\"n\",\"descKey\":\"d\",\"artKey\":\"x\",\"unlock\":{\"type\":\"levelCompleted\",\"value\":10}}," +
            "{\"stoneId\":\"b\",\"ingots\":100,\"nameKey\":\"n\",\"descKey\":\"d\",\"artKey\":\"x\",\"unlock\":{\"type\":\"starsTotal\",\"value\":60}}," +
            "{\"stoneId\":\"c\",\"nameKey\":\"n\",\"descKey\":\"d\",\"artKey\":\"x\",\"unlock\":{\"type\":\"starsTotal\",\"value\":150}}]}");

        [Test]
        public void StoneGift_OncePerStone_StoneStays()
        {
            var m = new MetaSave();
            var cat = Catalog();
            Assert.AreEqual(150, cat.Find("a").Ingots);
            Assert.IsFalse(Wallet.HasPendingGift(m, cat), "taş yok");
            m.stones.Add("a");
            m.stones.Add("c");
            Assert.AreEqual(1, Wallet.PendingGifts(m, cat).Count, "hediyesiz taş sayılmaz");
            Assert.AreEqual(150, Wallet.ClaimGift(m, cat, "a"));
            Assert.AreEqual(150, m.ingots);
            Assert.AreEqual(0, Wallet.ClaimGift(m, cat, "a"), "ikinci kez yok");
            Assert.AreEqual(0, Wallet.ClaimGift(m, cat, "b"), "kazanılmamış taş");
            Assert.IsFalse(Wallet.HasPendingGift(m, cat));
            Assert.Contains("a", m.stones, "taş koleksiyonda kalır");
        }

        [Test]
        public void Buy_SpendsIngots_FailsWhenShort()
        {
            var m = new MetaSave { ingots = 100 };
            Assert.IsTrue(Wallet.Buy(m, Booster.Recolor, 1));
            Assert.AreEqual(100 - ShopCatalog.Price(Booster.Recolor), m.ingots);
            Assert.AreEqual(1, m.boostRecolor);
            Assert.IsFalse(Wallet.Buy(m, Booster.Bomb, 3), "yetmez");
            Assert.AreEqual(0, m.boostBomb, "başarısız alım değiştirmez");
            Assert.IsFalse(Wallet.Buy(m, Booster.Bomb, 2), "yalnız 1 ya da 3");
            Assert.Less(ShopCatalog.Price3(Booster.Bomb), 3 * ShopCatalog.Price(Booster.Bomb), "3'lü indirimli");
        }

        [Test]
        public void Trials_GrantedOnceOnUnlock()
        {
            var m = new MetaSave();
            Assert.AreEqual(0, Wallet.GrantTrials(m, 10).Count, "10. bölümde açık yok");
            var got = Wallet.GrantTrials(m, ShopCatalog.UnlockLevel(Booster.Moves));
            CollectionAssert.AreEqual(new[] { Booster.Moves }, got);
            Assert.AreEqual(ShopCatalog.TrialCount, m.boostMoves);
            Assert.AreEqual(0, Wallet.GrantTrials(m, ShopCatalog.UnlockLevel(Booster.Moves)).Count, "bir kez");
            Assert.AreEqual(2, Wallet.GrantTrials(m, 500).Count, "kalan ikisi");
            Assert.IsTrue(Wallet.Consume(m, Booster.Bomb));
            Assert.AreEqual(ShopCatalog.TrialCount - 1, m.boostBomb);
        }

        [Test]
        public void CloudRestore_KeepsWallet()
        {
            var local = new MetaSave { ingots = 300, boostBomb = 2, noAds = true };
            local.stoneGifts.Add("a");
            var cloud = new MetaSave { ingots = 50, boostMoves = 4 };
            Wallet.MergeInto(cloud, local);
            Assert.AreEqual(300, cloud.ingots);
            Assert.AreEqual(4, cloud.boostMoves);
            Assert.AreEqual(2, cloud.boostBomb);
            Assert.IsTrue(cloud.noAds);
            Assert.Contains("a", cloud.stoneGifts);
        }

        private static LevelState Board3x3()
        {
            var b = new BoardState(5, 5);
            for (int i = 0; i < b.Count; i++)
            {
                b[i].Active = true;
                b[i].Layers.Add(LayerColor.Blue);
                b[i].Layers.Add(LayerColor.Coral);
            }
            return new LevelState { Board = b, MovesLeft = 4 };
        }

        [Test]
        public void Bomb_Peels3x3_NoMoveSpent_CornerClipped()
        {
            var s = Board3x3();
            var r = RulesEngine.TryApply(s, Command.Bomb(2, 2));
            Assert.IsTrue(r.Accepted, r.Reason.ToString());
            Assert.AreEqual(4, s.MovesLeft, "hamle harcanmaz");
            int peeled = 0;
            for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
            {
                bool inArea = x >= 1 && x <= 3 && y >= 1 && y <= 3;
                Assert.AreEqual(inArea ? 1 : 2, s.Board[x, y].Layers.Count, $"({x},{y})");
                if (inArea) peeled++;
            }
            Assert.AreEqual(9, peeled);
            var corner = Board3x3();
            Assert.IsTrue(RulesEngine.TryApply(corner, Command.Bomb(0, 0)).Accepted);
            Assert.AreEqual(1, corner.Board[1, 1].Layers.Count);
            Assert.AreEqual(2, corner.Board[2, 2].Layers.Count, "köşede 2×2");
        }

        [Test]
        public void Recolor_ChangesTopOfUpTo3()
        {
            var s = Board3x3();
            var session = new GameSession(s);
            var cells = new List<Coord> { new Coord(0, 0), new Coord(4, 4), new Coord(2, 2), new Coord(1, 1) };
            var done = session.RecolorTops(cells, LayerColor.Green);
            Assert.AreEqual(3, done.Count, "en çok 3");
            Assert.AreEqual(LayerColor.Green, session.State.Board[4, 4].Top);
            Assert.AreEqual(LayerColor.Blue, session.State.Board[4, 4].Layers[0], "alt kat aynı");
            Assert.AreNotEqual(LayerColor.Green, session.State.Board[1, 1].Top, "4. seçim yok");
            Assert.AreEqual(0, session.RecolorTops(new List<Coord> { new Coord(4, 4) }, LayerColor.Green).Count, "aynı renk atlanır");
        }

        [Test]
        public void HeartOffers_RefillAndUnlimited()
        {
            const long now = 1_000_000;
            var m = new MetaSave { ingots = 500 };
            Hearts.Spend(m, now); Hearts.Spend(m, now);
            Assert.AreEqual(3, Hearts.Count(m, now));
            Assert.IsTrue(Wallet.BuyHearts(m, HeartOffer.Refill, now));
            Assert.AreEqual(Hearts.Max, Hearts.Count(m, now));
            Assert.AreEqual(500 - ShopCatalog.HeartPrice(HeartOffer.Refill), m.ingots);
            Assert.IsFalse(Wallet.BuyHearts(m, HeartOffer.Refill, now), "dolu iken doldurma satılmaz");

            Assert.IsTrue(Wallet.BuyHearts(m, HeartOffer.Unlimited1h, now));
            Assert.IsTrue(Hearts.Unlimited(m, now + 3599));
            Assert.IsTrue(Hearts.Spend(m, now + 10));
            Assert.AreEqual(Hearts.Max, Hearts.Count(m, now + 10), "sınırsızda kalp gitmez");
            Assert.IsTrue(Wallet.BuyHearts(m, HeartOffer.Unlimited1h, now + 100), "süre üstüne biner");
            Assert.AreEqual(2 * 3600, Hearts.UnlimitedSeconds(m, now));
            Assert.IsFalse(Hearts.Unlimited(m, now + 2 * 3600));
            Hearts.Spend(m, now + 2 * 3600);
            Assert.AreEqual(Hearts.Max - 1, Hearts.Count(m, now + 2 * 3600), "süre bitince normal");

            var poor = new MetaSave { ingots = 10 };
            Assert.IsFalse(Wallet.BuyHearts(poor, HeartOffer.Unlimited3h, now));
            Assert.AreEqual(10, poor.ingots);
        }
    }
}
