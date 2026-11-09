using System;
using KatKat.Core;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    public class LevelDataTests
    {
        // Belge Bölüm 14 kural test fixture'ı (birebir).
        public const string ChainFixture = @"{
  ""schemaVersion"": 1,
  ""id"": ""fixture_chain_001"",
  ""width"": 4,
  ""height"": 4,
  ""moveLimit"": 1,
  ""rollEnabled"": false,
  ""startingRoll"": false,
  ""goals"": [
    { ""type"": ""collectColor"", ""color"": ""coral"", ""amount"": 3 },
    { ""type"": ""collectColor"", ""color"": ""blue"", ""amount"": 3 }
  ],
  ""cells"": [
    { ""x"": 0, ""y"": 0, ""active"": true,
      ""layers"": [""blue"", ""coral""],
      ""clipHitsRemaining"": 0, ""hasBottomStar"": false },
    { ""x"": 1, ""y"": 0, ""active"": true,
      ""layers"": [""blue"", ""coral""],
      ""clipHitsRemaining"": 0, ""hasBottomStar"": false },
    { ""x"": 2, ""y"": 0, ""active"": true,
      ""layers"": [""blue"", ""coral""],
      ""clipHitsRemaining"": 0, ""hasBottomStar"": false }
  ],
  ""solution"": [
    { ""type"": ""tap"", ""x"": 0, ""y"": 0 }
  ]
}";

        [Test]
        public void Fixture_LayerOrderIsBottomToTop()
        {
            var state = LevelLoader.ToState(LevelParser.Parse(ChainFixture));
            Assert.AreEqual(LayerColor.Coral, state.Board[0, 0].Top);
            Assert.AreEqual(LayerColor.Blue, state.Board[0, 0].Layers[0]);
            Assert.IsFalse(state.Board[3, 3].Active, "Belirtilmeyen hücre inactive.");
        }

        [Test]
        public void Fixture_PassesStaticValidation_AndSolutionReplaysToWin()
        {
            var d = LevelParser.Parse(ChainFixture);
            CollectionAssert.IsEmpty(LevelValidator.ValidateStatic(d, LevelValidator.FirstRelease));
            var rep = LevelValidator.ReplaySolution(d);
            Assert.IsTrue(rep.Success, rep.Error);
            Assert.AreEqual(2, rep.Waves);
            Assert.AreEqual(6, rep.LayersPeeled);
            Assert.AreEqual(0, rep.MovesLeft);
            Assert.AreEqual(GameStatus.Won, rep.FinalStatus);
        }

        [Test]
        public void Fixture_EngineEventsMatchDocument()
        {
            var state = LevelLoader.ToState(LevelParser.Parse(ChainFixture));
            var r = RulesEngine.TryApply(state, Command.Tap(0, 0));
            Assert.AreEqual(2, r.Waves.Count);
            Assert.IsTrue(r.Waves[0].Cells.TrueForAll(c => c.Removed == LayerColor.Coral && c.NewTop == LayerColor.Blue));
            Assert.IsTrue(r.Waves[1].Cells.TrueForAll(c => c.Removed == LayerColor.Blue && c.Remaining == 0));
        }

        [Test]
        public void Parser_RejectsMalformedJson_WithReadableMessage()
        {
            var ex = Assert.Throws<FormatException>(() => LevelParser.Parse("{ \"id\": \"x\", }", "bozuk.json"));
            StringAssert.Contains("bozuk.json", ex.Message);
        }

        [Test]
        public void StaticValidation_ReportsStructuralErrors()
        {
            var d = LevelParser.Parse(ChainFixture);
            d.MoveLimit = 0;
            d.StartingRoll = true;
            d.Cells.Add(new CellData { X = 0, Y = 0, Active = true });
            d.Cells.Add(new CellData { X = 9, Y = 0, Active = true });
            var bad = new CellData { X = 1, Y = 1, Active = true, ClipHitsRemaining = 1 };
            bad.Layers.Add("green");
            d.Cells.Add(bad);
            d.Goals.Add(new GoalData { Type = "collectColor", Color = "blue", Amount = 3 });
            d.Goals.Add(new GoalData { Type = "collectColor", Color = "yellow", Amount = 1 });
            var errors = string.Join("\n", LevelValidator.ValidateStatic(d, LevelValidator.FirstRelease));
            StringAssert.Contains("moveLimit", errors);
            StringAssert.Contains("startingRoll", errors);
            StringAssert.Contains("(0,0) iki kez", errors);
            StringAssert.Contains("(9,0) tahta dışında", errors);
            StringAssert.Contains("bilinmeyen renk 'green'", errors);
            StringAssert.Contains("ataş bu bölümün kapsamı dışında", errors);
            StringAssert.Contains("yinelenen hedef", errors);
            StringAssert.Contains("yellow hedefi 1, tahtada 0", errors);
        }

        [Test]
        public void StaticValidation_FlagsNoLegalStartingMove()
        {
            var d = LevelParser.Parse(ChainFixture);
            d.Cells.RemoveAt(2);
            d.Goals.Clear();
            d.Goals.Add(new GoalData { Type = "collectColor", Color = "coral", Amount = 2 });
            var errors = LevelValidator.ValidateStatic(d, LevelValidator.FirstRelease);
            Assert.IsTrue(errors.Exists(e => e.Contains("yasal hamle yok")), string.Join(" | ", errors));
        }

        [Test]
        public void Replay_FailsForWrongOrIncompleteSolution()
        {
            var d = LevelParser.Parse(ChainFixture);
            d.Solution[0].X = 3;
            d.Solution[0].Y = 3;
            var rep = LevelValidator.ReplaySolution(d);
            Assert.IsFalse(rep.Success);
            StringAssert.Contains("geçersiz", rep.Error);
        }
    }
}
