using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Levels;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    /// <summary>
    /// 2026-11-23 kova kipi (kullanıcı: "kovaları rengine göre taşlarla dolduracağız … bir renkten ne kadar istiyorsa onu tek kovaya
    /// da doldurabiliriz, birden çok kovaya da … mavi taş kovasını dolduğu kadarını kovanın üstüne tıklayarak göndeririz ve yeni kova
    /// gelir"; "roket ve rulo kazanma ile kullanıldığında da kovanın rengi ise kova dolacak").
    /// </summary>
    public class BucketRulesTests
    {
        private static LevelState WithBuckets(LevelState s, params (LayerColor c, int n)[] orders)
        {
            s.Goals.Clear();
            foreach (var g in orders.GroupBy(o => o.c))
                s.Goals.Add(new GoalState { Type = GoalType.CollectColor, Color = g.Key, Amount = g.Sum(o => o.n) });
            s.Bucket = new BucketRun();
            s.Bucket.Queue.AddRange(orders.Select(o => new BucketOrder(o.c, o.n)));
            RulesEngine.NextBucket(s);
            return s;
        }

        private static GoalState Goal(LevelState s, LayerColor c) => s.Goals.Single(g => g.Color == c);

        [Test]
        public void BucketColorStones_FillBucket_OtherColorsDoNot_GoalOnlyOnDelivery()
        {
            var s = WithBuckets(State(9, new[] { "c c c b", "y y y b" }), (LayerColor.Coral, 5), (LayerColor.Yellow, 3));
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(3, s.Bucket.Have, "üç mercan kovaya");
            Assert.AreEqual(new[] { 0, 1, 2 }, r.Waves[0].BucketCells.ToArray());
            Assert.AreEqual(0, Goal(s, LayerColor.Coral).Progress, "hedef yalnız teslimle ilerler");
            RulesEngine.TryApply(s, Command.Tap(0, 1));
            Assert.AreEqual(3, s.Bucket.Have, "sarı mercan kovasına girmez");
            Assert.AreEqual(0, Goal(s, LayerColor.Yellow).Progress, "sarı da sayılmaz (sarı kovası sırada)");
        }

        [Test]
        public void Bucket_TakesUpToColorsRemainingGoal_NeverSendsItself()
        {
            // Kullanıcı: "Kovayı ne kadar doldurduysam o kadar taş alacak"; 2026-11-23 torba: "Torbayı gönder demeden gitmesin
            // otomatik gitmesin". Torba rengin kalan görevi kadar alır, dolsa da gönderilene dek bekler.
            var s = WithBuckets(State(9, new[] { "c c c c c", "y y b y y" }), (LayerColor.Coral, 2), (LayerColor.Coral, 2), (LayerColor.Yellow, 4));
            Assert.AreEqual(4, s.Bucket.Need, "mercan torbası mercan görevinin tamamını alabilir");
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(0, r.BucketSends.Count, "dolu torba kendiliğinden gitmez");
            Assert.AreEqual(4, s.Bucket.Have);
            Assert.AreEqual(4, r.Waves[0].BucketCells.Count, "beş mercandan dördü torbaya");
            Assert.AreEqual(0, Goal(s, LayerColor.Coral).Progress, "hedef yalnız teslimle");
            var send = RulesEngine.TryApply(s, Command.SendBucket());
            Assert.IsFalse(send.BucketSends[0].Partial);
            Assert.AreEqual(4, Goal(s, LayerColor.Coral).Progress);
            Assert.AreEqual(LayerColor.Yellow, s.Bucket.Color, "biten mercanın sıradaki torbası atlandı, sarı geldi");
        }

        [Test]
        public void TapBucket_SendsPartial_RemainderQueuedAtEnd_NoMoveSpent()
        {
            var s = WithBuckets(State(9, new[] { "c c c b", "y y y b" }), (LayerColor.Coral, 5), (LayerColor.Yellow, 3));
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            int moves = s.MovesLeft;
            var r = RulesEngine.TryApply(s, Command.SendBucket());
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(moves, s.MovesLeft, "gönderim hamle harcamaz");
            var send = r.BucketSends.Single();
            Assert.IsTrue(send.Partial);
            Assert.AreEqual(3, send.Have);
            Assert.AreEqual(3, Goal(s, LayerColor.Coral).Progress, "yarım kova teslim edildi");
            Assert.AreEqual(LayerColor.Yellow, s.Bucket.Color);
            Assert.AreEqual(LayerColor.Coral, s.Bucket.Queue.Last().Color, "mercan yine gelecek (sıranın sonunda)");
            Assert.AreEqual(2, s.Bucket.Queue.Last().Amount);
        }

        [Test]
        public void EmptyBag_CanBeSent_ColorComesBackLater()
        {
            // 2026-11-23: "Torba boşken de gönderilsin".
            var s = WithBuckets(State(9, new[] { "c c c b", "y y y b" }), (LayerColor.Coral, 5), (LayerColor.Yellow, 3));
            var r = RulesEngine.TryApply(s, Command.SendBucket());
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(0, r.BucketSends.Single().Have);
            Assert.AreEqual(LayerColor.Yellow, s.Bucket.Color, "sıradaki renk geldi");
            Assert.AreEqual(LayerColor.Coral, s.Bucket.Queue.Last().Color, "mercan sıranın sonunda");
            Assert.AreEqual(0, Goal(s, LayerColor.Coral).Progress);
            Assert.IsTrue(RulesEngine.HasLegalMove(s), "torba her zaman gönderilebilir");
        }

        [Test]
        public void AllDelivered_Wins()
        {
            var s = WithBuckets(State(9, new[] { "c c c b", "y y y b" }), (LayerColor.Coral, 3), (LayerColor.Yellow, 3));
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            RulesEngine.TryApply(s, Command.SendBucket());
            Assert.AreEqual(GameStatus.Idle, s.Status);
            RulesEngine.TryApply(s, Command.Tap(0, 1));
            Assert.AreEqual(GameStatus.Idle, s.Status, "dolu torba gönderilmeden kazanılmaz");
            var r = RulesEngine.TryApply(s, Command.SendBucket());
            Assert.AreEqual(GameStatus.Won, r.StatusAfter);
            Assert.IsFalse(s.Bucket.Active);
        }

        [Test]
        public void Rocket_PeelsBucketColor_FillsBucket()
        {
            var s = WithBuckets(State(9, new[] { "y c y c y", "b b b y y" }), (LayerColor.Coral, 2), (LayerColor.Yellow, 9));
            s.SpecialsEnabled = true;
            s.Tray.Add(SpecialKind.RocketRow);
            var r = RulesEngine.TryApply(s, Command.UseSpecial(0, 0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(2, s.Bucket.Have, "satır roketi iki mercanı torbaya attı");
            Assert.AreEqual(0, r.BucketSends.Count);
            RulesEngine.TryApply(s, Command.SendBucket());
            Assert.AreEqual(2, Goal(s, LayerColor.Coral).Progress);
        }

        [Test]
        public void BagLevels_SpecialsStayOnBoard()
        {
            // 2026-11-26 (kullanıcı: "Torba ise tahtanın üst kısmındaki altın ve roket rulolarının yerine gelsin"): torba bölümünde de
            // roket dokunulan taşın yerinde; tepsi kullanılmaz.
            var s = WithBuckets(State(9, new[] { "c c c c c", "y y y y y", "b b b b b" }), (LayerColor.Purple, 5));
            s.SpecialsEnabled = true;
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(0, s.Tray.Count);
            Assert.IsTrue(s.Board[0, 0].HasSpecial);
        }

        [Test]
        public void UndoSnapshot_ClonesBucket()
        {
            var s = WithBuckets(State(9, new[] { "c c c b" }), (LayerColor.Coral, 5));
            var copy = s.Clone();
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(0, copy.Bucket.Have);
            Assert.IsFalse(copy.ContentEquals(s));
        }

        [Test]
        public void CatalogBagLevels_AreBucketLevels_ValidAndLoad()
        {
            // 2026-11-25: test alanı su bölümlerine geçti; torba bölümleri katalogda (15'ten 8'de bir).
            var dir = ProductionLevelTests.LevelsDir();
            for (int n = 15; n <= 95; n += 8)
            {
                var d = LevelParser.Parse(File.ReadAllText(Path.Combine(dir, $"level_{n:000}.json")), $"level_{n:000}");
                Assert.IsEmpty(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)), d.Id);
                Assert.Greater(d.Buckets.Count, 0, d.Id + " kovalı");
                Assert.Greater(d.TimeLimit, 0, d.Id + " süreli");
                var st = LevelLoader.ToState(d);
                Assert.IsTrue(st.Bucket.Active, d.Id);
                int sum = 0;
                foreach (var g in d.Goals) sum += g.Amount;
                Assert.GreaterOrEqual(sum, 50, d.Id + " büyük görev");
                // 2026-11-26 (kullanıcı: "Torba ise tahtanın üst kısmındaki altın ve roket rulolarının yerine gelsin"): torba üst bantta,
                // tahtada üst oyuk yok.
                Assert.IsTrue(st.Board[(d.Width - 1) / 2, 0].Active, d.Id + " üstte torba oyuğu yok");
            }
        }
    }
}
