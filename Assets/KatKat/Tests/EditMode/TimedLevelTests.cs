using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>2026-11-21 süreli bölüm: süre bölüm verisinden okunur, hamle sınırsız başlar; süresiz bölüm eskisi gibi.</summary>
    public class TimedLevelTests
    {
        private const string Json = @"{ ""schemaVersion"": 2, ""id"": ""t_01"", ""capabilities"": [""specials""], ""trialType"": ""lab"",
            ""width"": 5, ""height"": 5, ""moveLimit"": 99, ""timeLimit"": 60,
            ""goals"": [ { ""type"": ""collectColor"", ""color"": ""coral"", ""amount"": 3 } ],
            ""cells"": [ { ""x"": 0, ""y"": 0, ""active"": true, ""layers"": [""coral"", ""coral""] },
                         { ""x"": 1, ""y"": 0, ""active"": true, ""layers"": [""coral"", ""coral""] },
                         { ""x"": 2, ""y"": 0, ""active"": true, ""layers"": [""coral"", ""coral""] } ] }";

        [Test]
        public void TimeLimit_IsParsed_AndMovesAreUnlimited()
        {
            var d = LevelParser.Parse(Json, "t_01");
            Assert.AreEqual(60, d.TimeLimit);
            Assert.AreEqual(LevelLoader.UnlimitedMoves, LevelLoader.ToState(d).MovesLeft);
        }

        [Test]
        public void WithoutTimeLimit_MovesComeFromMoveLimit()
        {
            var d = LevelParser.Parse(Json.Replace(@"""timeLimit"": 60,", ""), "t_02");
            Assert.AreEqual(0, d.TimeLimit);
            Assert.AreEqual(99, LevelLoader.ToState(d).MovesLeft);
        }
    }
}
