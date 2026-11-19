using System.Collections.Generic;
using KatKat.EditorTools;
using KatKat.Game.Online;
using KatKat.Levels;
using KatKat.Meta;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>Huawei için Firebase REST (2026-11-18): gövde kurma/çözme, yapılandırma, misafir adı, hatırlatma takvimi.</summary>
    public sealed class FirebaseRestTests
    {
        private static Dictionary<string, object> Obj(string json) => (Dictionary<string, object>)MiniJson.Parse(json);

        [Test]
        public void ScoreCommit_MatchesRulesShape()
        {
            var self = new LeaderboardEntry { Score = 712, Zpe = 64, Level = 45, Stars = 101 };
            var root = Obj(FirestoreRestCodec.ScoreCommit("proj", "uid1", "Oyuncu \"1\"", "TR", self));
            var w = (Dictionary<string, object>)((List<object>)root["writes"])[0];
            var upd = (Dictionary<string, object>)w["update"];
            Assert.AreEqual("projects/proj/databases/(default)/documents/scores/uid1", upd["name"]);
            var f = (Dictionary<string, object>)upd["fields"];
            CollectionAssert.AreEquivalent(new[] { "name", "country", "score", "zpe", "level", "stars" }, f.Keys);
            Assert.AreEqual("Oyuncu \"1\"", ((Dictionary<string, object>)f["name"])["stringValue"]);
            Assert.AreEqual("712", ((Dictionary<string, object>)f["score"])["integerValue"]);
            var tr = (Dictionary<string, object>)((List<object>)w["updateTransforms"])[0];
            Assert.AreEqual("updated", tr["fieldPath"]);
            Assert.AreEqual("REQUEST_TIME", tr["setToServerValue"]);
        }

        [Test]
        public void Queries_AreValidJson_AndFilterCountry()
        {
            var world = Obj(FirestoreRestCodec.TopQuery(null));
            Assert.IsFalse(((Dictionary<string, object>)world["structuredQuery"]).ContainsKey("where"));
            var country = Obj(FirestoreRestCodec.TopQuery("DE"));
            StringAssert.Contains("\"DE\"", FirestoreRestCodec.TopQuery("DE"));
            Assert.IsTrue(((Dictionary<string, object>)country["structuredQuery"]).ContainsKey("where"));
            Assert.IsNotNull(Obj(FirestoreRestCodec.HigherCount(null, 500)));
            StringAssert.Contains("compositeFilter", FirestoreRestCodec.HigherCount("TR", 500));
        }

        [Test]
        public void ParseTop_And_Count()
        {
            const string top = "[{\"document\":{\"name\":\"projects/p/databases/(default)/documents/scores/a\",\"fields\":{\"name\":{\"stringValue\":\"Ada\"},\"score\":{\"integerValue\":\"900\"},\"zpe\":{\"integerValue\":\"80\"},\"level\":{\"integerValue\":\"120\"},\"stars\":{\"integerValue\":\"300\"}}},\"readTime\":\"x\"},"
                             + "{\"document\":{\"name\":\"projects/p/databases/(default)/documents/scores/me\",\"fields\":{\"name\":{\"stringValue\":\"Ben\"},\"score\":{\"integerValue\":\"500\"}}}},{\"readTime\":\"x\"}]";
            var list = FirestoreRestCodec.ParseTop(top, "me");
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("Ada", list[0].Name);
            Assert.AreEqual(900, list[0].Score);
            Assert.AreEqual(1, list[0].Rank);
            Assert.IsTrue(list[1].IsSelf);
            Assert.AreEqual(2, list[1].Rank);
            Assert.AreEqual(7, FirestoreRestCodec.ParseCount("[{\"result\":{\"aggregateFields\":{\"n\":{\"integerValue\":\"7\"}}},\"readTime\":\"x\"}]"));
            Assert.AreEqual(-1, FirestoreRestCodec.ParseCount("{\"error\":1}"));
            Assert.AreEqual(0, FirestoreRestCodec.ParseTop("bozuk", "me").Count);
        }

        [Test]
        public void BuildRestConfig_PicksPackageKey()
        {
            const string gs = "{\"project_info\":{\"project_id\":\"demo-proj\"},\"client\":[" +
                              "{\"client_info\":{\"android_client_info\":{\"package_name\":\"com.other\"}},\"api_key\":[{\"current_key\":\"K-OTHER\"}]}," +
                              "{\"client_info\":{\"android_client_info\":{\"package_name\":\"com.ofigame.stoneburst\"}},\"api_key\":[{\"current_key\":\"K-MINE\"}]}]}";
            var cfg = FirebaseRestConfig.Parse(AppGalleryBuild.BuildRestConfig(gs, "com.ofigame.stoneburst"));
            Assert.IsNotNull(cfg);
            Assert.AreEqual("K-MINE", cfg.ApiKey);
            Assert.AreEqual("demo-proj", cfg.ProjectId);
            Assert.IsNull(AppGalleryBuild.BuildRestConfig(gs, "com.missing"));
            Assert.IsNull(AppGalleryBuild.BuildRestConfig(null, "x"));
        }

        [Test]
        public void RealGoogleServices_HasKeyForGamePackage()
        {
            string path = "Assets/KatKat/Firebase/google-services.json";
            if (!System.IO.File.Exists(path)) Assert.Ignore("google-services.json yok");
            var cfg = FirebaseRestConfig.Parse(AppGalleryBuild.BuildRestConfig(System.IO.File.ReadAllText(path), "com.ofigame.stoneburst"));
            Assert.IsNotNull(cfg, "Stone Burst paketi için anahtar bulunamadı");
        }

        [Test]
        public void GuestName_IsStableFourDigits()
        {
            string a = RestAccountService.GuestName("abcDEF123", "Oyuncu {0}");
            Assert.AreEqual(a, RestAccountService.GuestName("abcDEF123", "Oyuncu {0}"));
            StringAssert.IsMatch(@"^Oyuncu \d{4}$", a);
            StringAssert.IsMatch(@"^Player \d{4}$", RestAccountService.GuestName("x", null));
        }

        [Test]
        public void Reminder_Schedule()
        {
            Assert.IsFalse(RankReminder.Due(9, RankReminder.First));
            Assert.IsTrue(RankReminder.Due(10, RankReminder.First));
            // İlk gösterim 10.da → 25; ileri bölümdeki oyuncu (300) ilk gösterimden sonra hemen yeniden sorulmaz.
            Assert.AreEqual(25, RankReminder.Next(1, 10));
            Assert.AreEqual(315, RankReminder.Next(1, 300));
            Assert.AreEqual(60, RankReminder.Next(2, 35));
            Assert.AreEqual(100, RankReminder.Next(3, 60));
            Assert.AreEqual(200, RankReminder.Next(4, 100));
            Assert.IsFalse(RankReminder.Due(314, 315));
        }
    }
}
