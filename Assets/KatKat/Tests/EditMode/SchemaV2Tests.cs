using System.Linq;
using KatKat.Core;
using KatKat.Levels;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Seviye şeması 2: örtü (overlay), yetenek listesi, kapsam ve statik kontroller (rehber §12).</summary>
    public class SchemaV2Tests
    {
        // Buz öğretici fikstürü: üç mercan soyulunca buz kırılır, mavi grup açılır.
        private const string IceFixture = @"{
  ""schemaVersion"": 2,
  ""id"": ""fixture_ice_001"",
  ""capabilities"": [""ice""],
  ""width"": 4, ""height"": 4, ""moveLimit"": 2,
  ""rollEnabled"": false, ""startingRoll"": false,
  ""goals"": [ { ""type"": ""collectColor"", ""color"": ""blue"", ""amount"": 3 } ],
  ""cells"": [
    { ""x"": 0, ""y"": 0, ""active"": true, ""layers"": [""coral""], ""overlay"": { ""kind"": ""none"" } },
    { ""x"": 1, ""y"": 0, ""active"": true, ""layers"": [""coral""], ""overlay"": { ""kind"": ""none"" } },
    { ""x"": 2, ""y"": 0, ""active"": true, ""layers"": [""coral""], ""overlay"": { ""kind"": ""none"" } },
    { ""x"": 0, ""y"": 1, ""active"": true, ""layers"": [""blue""], ""overlay"": { ""kind"": ""ice"", ""hits"": 1 } },
    { ""x"": 1, ""y"": 1, ""active"": true, ""layers"": [""blue""], ""overlay"": { ""kind"": ""none"" } },
    { ""x"": 2, ""y"": 1, ""active"": true, ""layers"": [""blue""], ""overlay"": { ""kind"": ""none"" } }
  ],
  ""solution"": [ { ""type"": ""tap"", ""x"": 0, ""y"": 0 }, { ""type"": ""tap"", ""x"": 0, ""y"": 1 } ]
}";

        private static LevelData Ice() => LevelParser.Parse(IceFixture);

        [Test]
        public void IceFixture_ParsesValidatesAndReplays()
        {
            var d = Ice();
            Assert.AreEqual(2, d.SchemaVersion);
            Assert.AreEqual(new[] { "ice" }, d.Capabilities.ToArray());
            Assert.AreEqual(1, LevelLoader.ToState(d).Board[0, 1].IceHitsRemaining);
            CollectionAssert.IsEmpty(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)));
            Assert.IsTrue(LevelValidator.ReplaySolution(d).Success);
        }

        [Test]
        public void Schema2Scope_ComesFromCapabilities_NotLevelNumber()
        {
            var d = Ice();
            d.Capabilities.Clear();
            var errors = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
            Assert.IsTrue(errors.Any(e => e.Contains("buz bu bölümün kapsamı dışında")), string.Join("\n", errors));
        }

        [Test]
        public void Schema1_CannotCarryIce()
        {
            var d = Ice();
            d.SchemaVersion = 1;
            d.Capabilities.Clear();
            var errors = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
            Assert.IsTrue(errors.Any(e => e.Contains("schemaVersion 2 gerekli")), string.Join("\n", errors));
        }

        [Test]
        public void TwoCoversOnOneCell_AreRejected()
        {
            var d = Ice();
            d.Capabilities.Add("clip");
            d.Cells.First(c => c.IceHits > 0).ClipHitsRemaining = 1;
            Assert.IsTrue(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)).Any(e => e.Contains("birden fazla örtü")));
        }

        [Test]
        public void LockAskingMoreThanBoardHas_IsRejected()
        {
            var d = Ice();
            d.Capabilities.Add("colorLock");
            var cell = d.Cells.First(c => c.X == 2 && c.Y == 1);
            cell.LockColor = "coral";
            cell.LockRemaining = 4; // tahtada 3 mercan var
            Assert.IsTrue(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)).Any(e => e.Contains("kilit 4 coral")));
            cell.LockRemaining = 3;
            CollectionAssert.IsEmpty(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)));
        }

        [Test]
        public void Signature_CatchesMirrorRecolorAndMoveLimitOnlyCopies()
        {
            var a = Ice();
            var b = Ice();
            b.Id = "copy";
            b.MoveLimit = 9;
            foreach (var c in b.Cells)
            {
                c.X = 3 - c.X; // yatay ayna
                for (int i = 0; i < c.Layers.Count; i++) c.Layers[i] = c.Layers[i] == "coral" ? "yellow" : c.Layers[i];
            }
            Assert.AreEqual(LevelSignature.Canonical(a), LevelSignature.Canonical(b));
            Assert.AreEqual(1, LevelSignature.Duplicates(new[] { a, b }).Count);
            // Buz vuruşu farklıysa farklı bölüm.
            b.Cells.First(c => c.IceHits > 0).IceHits = 2;
            Assert.AreNotEqual(LevelSignature.Canonical(a), LevelSignature.Canonical(b));
        }

        [Test]
        public void EnvelopeAndTokenGoal_ParseValidateAndReplay()
        {
            var json = IceFixture
                .Replace("\"capabilities\": [\"ice\"]", "\"capabilities\": [\"ice\", \"envelope\"]")
                .Replace("{ \"x\": 2, \"y\": 1, \"active\": true, \"layers\": [\"blue\"], \"overlay\": { \"kind\": \"none\" } }",
                         "{ \"x\": 2, \"y\": 1, \"active\": true, \"layers\": [\"blue\"], \"overlay\": { \"kind\": \"envelope\", \"sealColor\": \"coral\" } }")
                .Replace("\"goals\": [ { \"type\": \"collectColor\", \"color\": \"blue\", \"amount\": 3 } ]",
                         "\"goals\": [ { \"type\": \"collectToken\", \"kind\": \"envelope\", \"amount\": 1 } ]")
                .Replace(", { \"type\": \"tap\", \"x\": 0, \"y\": 1 }", "");
            var d = LevelParser.Parse(json);
            Assert.AreEqual("coral", d.Cells.First(c => c.X == 2 && c.Y == 1).EnvelopeColor);
            var st = LevelLoader.ToState(d);
            Assert.IsTrue(st.Board[2, 1].HasEnvelope);
            Assert.AreEqual(GoalType.CollectEnvelopes, st.Goals[0].Type);
            CollectionAssert.IsEmpty(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)));
            Assert.IsTrue(LevelValidator.ReplaySolution(d).Success, "Mercanlar soyulunca zarf açılır, tek hamlede kazanılır.");

            d.Goals[0].Amount = 2;
            Assert.IsTrue(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)).Any(e => e.Contains("zarf hedefi 2")));
            d.Capabilities.Remove("envelope");
            Assert.IsTrue(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)).Any(e => e.Contains("zarf bu bölümün kapsamı dışında")));
        }

        [Test]
        public void UnknownCapabilityAndOverlay_AreReported()
        {
            var d = Ice();
            d.Capabilities.Add("teleport");
            Assert.IsTrue(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)).Any(e => e.Contains("bilinmeyen yetenek")));
            Assert.Throws<System.FormatException>(() => LevelParser.Parse(IceFixture.Replace("\"kind\": \"ice\"", "\"kind\": \"lava\"")));
        }
    }
}
