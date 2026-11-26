using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Levels;
using NUnit.Framework;
using static KatKat.Tests.TestBoards;

namespace KatKat.Tests
{
    /// <summary>
    /// 2026-11-25 su kipi (kullanıcı: "her hücrenin içinde su olacak ve hücredeki taşlar gittiğinde dipteki su torba gibi mantıkla
    /// kovamız olacak ve kovaya dolacak. O bölümün görevi kovayı su doldurmak olacak"). Su yalnız hücre dibine kadar boşalınca akar.
    /// </summary>
    public class WaterRulesTests
    {
        private static LevelState WithWater(string[] rows, int need, params (int x, int y, int units)[] water)
        {
            var s = State(20, rows);
            foreach (var w in water) s.Board[w.x, w.y].Water = w.units;
            s.Goals.Add(new GoalState { Type = GoalType.FillWater, Amount = need });
            return s;
        }

        [Test]
        public void Water_FlowsOnlyWhenCellEmptied()
        {
            // (0,0) iki katman (alt sarı, üst mercan), 2 birim su; (1,0) ve (2,0) mercan.
            var s = WithWater(new[] { "yc c c b" }, 5, (0, 0, 2));
            var r = RulesEngine.TryApply(s, Command.Tap(1, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(0, s.Goals[0].Progress, "üstü soyulan hücrede su akmaz (dipte sarı var)");
            Assert.AreEqual(2, s.Board[0, 0].Water);
            Assert.AreEqual(0, r.Waves.Sum(w => w.WaterCollected));
        }

        [Test]
        public void Water_EmptiedCell_GivesUnitsOnce_Score()
        {
            var s = WithWater(new[] { "c c c b" }, 5, (0, 0, 2), (2, 0, 1));
            int before = s.Score;
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(3, s.Goals[0].Progress, "iki hücre boşaldı: 2 + 1 birim");
            Assert.AreEqual(3, r.Waves.Sum(w => w.WaterCollected));
            Assert.AreEqual(0, s.Board[0, 0].Water, "su bir kez akar");
            Assert.GreaterOrEqual(s.Score - before, 3 * Scoring.Water);
            var peel = r.Waves.SelectMany(w => w.Cells).First(c => c.X == 0 && c.Y == 0);
            Assert.AreEqual(2, peel.Water);
        }

        [Test]
        public void Water_ProtectedPeel_CracksTray_NoLoss()
        {
            // 2026-11-25 (kullanıcı: "Sulu bölümde yasaklı taşlar patlatılır ise tepsi delinsin ve su boşalsın"): kayıp yok, yarık.
            var s = WithWater(new[] { "c c c b", "y y b y" }, 5, (3, 0, 1));
            s.Board[1, 0].Protected = true;
            var r = RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.IsTrue(r.Accepted);
            Assert.IsFalse(s.ProtectedBroken);
            Assert.AreNotEqual(GameStatus.LostProtected, s.Status);
            CollectionAssert.Contains(r.Cracks, 1, "yarık bombanın sütununda");
            // Susuz bölümde aynı hamle kaybettirir.
            var dry = State(20, new[] { "c c c b", "y y b y" }, (LayerColor.Coral, 3));
            dry.Board[1, 0].Protected = true;
            RulesEngine.TryApply(dry, Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.LostProtected, dry.Status);
        }

        [Test]
        public void WaterBucket_FillsToCap_Spills_SendMovesToGoal()
        {
            // 2026-11-26 (kullanıcı: "kova doldukça gönder diyelim … kovayı en az 3 kez gönderebileceğimiz şekilde olmalı").
            var s = WithWater(new[] { "c c c b", "y y b y" }, 6, (0, 0, 2), (1, 0, 2));
            s.WaterCap = 3;
            Assert.IsFalse(RulesEngine.TryApply(s, Command.SendBucket()).Accepted, "boş kova gönderilmez");
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(3, s.WaterHave, "kapasiteye kadar; fazlası taştı");
            Assert.AreEqual(0, s.Goals[0].Progress, "görev gönderimden önce ilerlemez");
            int moves = s.MovesLeft;
            var r = RulesEngine.TryApply(s, Command.SendBucket());
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(3, r.WaterSent);
            Assert.AreEqual(3, s.Goals[0].Progress);
            Assert.AreEqual(0, s.WaterHave, "kova boşaldı");
            Assert.AreEqual(moves, s.MovesLeft, "gönderim hamle harcamaz");
        }

        [Test]
        public void Water_GoalComplete_Wins()
        {
            var s = WithWater(new[] { "c c c b" }, 2, (1, 0, 2));
            RulesEngine.TryApply(s, Command.Tap(0, 0));
            Assert.AreEqual(GameStatus.Won, s.Status);
        }

        [Test]
        public void Water_CloneAndEquality()
        {
            var s = WithWater(new[] { "c c c b" }, 2, (1, 0, 2));
            var copy = s.Clone();
            Assert.IsTrue(copy.ContentEquals(s));
            copy.Board[1, 0].Water = 1;
            Assert.IsFalse(copy.ContentEquals(s));
        }

        [Test]
        public void Validator_WaterNeedsCapability_AndEnoughWater()
        {
            string json = "{ \"schemaVersion\": 2, \"id\": \"w\", \"capabilities\": [WATERCAP], \"width\": 4, \"height\": 4, \"moveLimit\": 10, \"stars\": [100, 200], "
                + "\"goals\": [ { \"type\": \"fillWater\", \"amount\": NEED } ], \"cells\": [";
            var cells = new System.Collections.Generic.List<string>();
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                cells.Add($"{{ \"x\": {x}, \"y\": {y}, \"active\": true, \"layers\": [\"{(x % 2 == 0 ? "coral" : "blue")}\", \"{(y % 2 == 0 ? "yellow" : "coral")}\"]" + (x == 0 && y == 0 ? ", \"water\": 2" : "") + " }");
            string body = json + string.Join(", ", cells) + "] }";
            var ok = LevelParser.Parse(body.Replace("WATERCAP", "\"water\"").Replace("NEED", "2"), "w");
            Assert.IsEmpty(LevelValidator.ValidateStatic(ok, LevelValidator.ScopeFor(ok)).Where(e => e.Contains("su") || e.Contains("water")).ToList());
            Assert.AreEqual(2, LevelLoader.ToState(ok).Board[0, 0].Water);
            var noCap = LevelParser.Parse(body.Replace("WATERCAP", "").Replace("NEED", "2"), "w");
            Assert.IsTrue(LevelValidator.ValidateStatic(noCap, LevelValidator.ScopeFor(noCap)).Any(e => e.Contains("water")));
            var tooMuch = LevelParser.Parse(body.Replace("WATERCAP", "\"water\"").Replace("NEED", "3"), "w");
            Assert.IsTrue(LevelValidator.ValidateStatic(tooMuch, LevelValidator.ScopeFor(tooMuch)).Any(e => e.Contains("su hedefi")));
        }

        [Test]
        public void LabLevels_FirstIsWater_AllHaveBoardSpecials_ValidAndLoad()
        {
            // 2026-11-26 (kullanıcı: "Test bölümlerde 10 adet tüm özellikleri içeren ve bu son istediğimide içeren testler yap"):
            // 10 test bölümünün hepsi geçerli ve açılışta tahtada özel taş taşır; 1. bölüm su bölümü (kova oyuğu altta).
            var dir = Path.Combine(Path.GetDirectoryName(ProductionLevelTests.LevelsDir()), "Lab");
            for (int i = 1; i <= 10; i++)
            {
                var d = LevelParser.Parse(File.ReadAllText(Path.Combine(dir, $"lab_{i:00}.json")), "lab");
                Assert.IsEmpty(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)), d.Id);
                var s0 = LevelLoader.ToState(d);
                // 2026-11-26 (kullanıcı: "Roket ve rulo yine tahtanın üst kısmında olsun"): hazır özel taşlar açılışta tepside.
                Assert.Greater(s0.Tray.Count, 0, d.Id + " tepside hazır özel taş");
                if (i > 1) continue;
                Assert.IsTrue(d.Goals.Any(g => g.Type == "fillWater"), d.Id + " su görevi");
                Assert.Greater(d.TimeLimit, 0, d.Id + " süreli (2026-11-25)");
                var st = LevelLoader.ToState(d);
                int wet = 0, total = 0;
                for (int k = 0; k < st.Board.Count; k++) if (st.Board[k].Water > 0) { wet++; total += st.Board[k].Water; }
                Assert.Less(wet, st.Board.Count / 3, d.Id + " su yalnız bazı hücrelerde");
                Assert.GreaterOrEqual(total, d.Goals.First(g => g.Type == "fillWater").Amount);
                Assert.IsFalse(st.Board[(d.Width - 1) / 2, d.Height - 1].Active, d.Id + " alt ortada kova oyuğu (2026-11-26)");
                Assert.IsTrue(st.Board[(d.Width - 1) / 2, 0].Active, d.Id + " üstte oyuk yok");
            }
        }
    }
}
