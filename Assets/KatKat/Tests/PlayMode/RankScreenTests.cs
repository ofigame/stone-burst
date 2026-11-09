using System.Collections;
using KatKat.Game;
using KatKat.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    /// <summary>v12 sıralama ekranı (docs/v12/siralama): ilk 10, podyum 2-1-3, sabit kart, sekmeler, durumlar.</summary>
    public class RankScreenTests
    {
        private GameController _gc;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameController.RequestedLevel = -1;
            GameController.ToursOverride = false;
            GameController.SaveDirectoryOverride = System.IO.Path.Combine(Application.temporaryCachePath, "katkat_rank_test");
            if (System.IO.Directory.Exists(GameController.SaveDirectoryOverride)) System.IO.Directory.Delete(GameController.SaveDirectoryOverride, true);
            System.IO.Directory.CreateDirectory(GameController.SaveDirectoryOverride);
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameController.DevLeaderboardSample = false;
            GameController.CountryOverride = null;
            GameController.ToursOverride = null;
            GameController.SaveDirectoryOverride = null;
            yield return null;
        }

        private void Progress(int n)
        {
            for (int i = 0; i < n; i++)
            {
                _gc.SaveService.MarkCompleted(_gc.Catalog.IdAt(i), i, _gc.Catalog.Count);
                _gc.SaveService.RecordStars(_gc.Catalog.IdAt(i), 3);
                RankStars.Record(_gc.SaveService.Data.meta, _gc.Catalog.IdAt(i), 3);
            }
        }

        private static GameObject Child(string root, string path)
        {
            var r = GameObject.Find(root);
            Assert.IsNotNull(r, root);
            var t = r.transform.Find(path);
            Assert.IsNotNull(t, root + "/" + path);
            return t.gameObject;
        }

        private static string Text(GameObject go)
        {
            var tmp = go.GetComponent("TextMeshProUGUI");
            return (string)tmp.GetType().GetProperty("text").GetValue(tmp);
        }

        [UnityTest]
        public IEnumerator DevSample_Top10_Podium213_SelfCard_TabSwitch()
        {
            GameController.DevLeaderboardSample = true;
            GameController.CountryOverride = "TR";
            Progress(25);
            _gc.ShowLevels();
            var btn = GameObject.Find("RankButton");
            Assert.IsNotNull(btn, "haritada sıralama düğmesi");
            btn.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(_gc.Menu.IsRankShown);
            for (int i = 1; i <= 10; i++) Assert.IsNotNull(GameObject.Find("Row" + i), $"ilk 10: {i}. satır");
            Assert.IsNull(GameObject.Find("Row11"));
            var p1 = GameObject.Find("Podium1").transform.position.x;
            var p2 = GameObject.Find("Podium2").transform.position.x;
            var p3 = GameObject.Find("Podium3").transform.position.x;
            Assert.Less(p2, p1, "ikinci solda");
            Assert.Less(p1, p3, "üçüncü sağda");
            Assert.AreEqual(Text(Child("Row1", "Name")), Text(Child("Podium1", "Name")), "podyum ve liste aynı veri");
            Assert.IsNotNull(GameObject.Find("DevSample"), "geliştirme verisi etiketli");
            var card = GameObject.Find("SelfCard");
            Assert.IsNotNull(card);
            Assert.AreEqual("284", Text(Child("SelfCard", "RankPill/Rank")));
            // Liste kayınca kart yerinde kalır (kart kaydırma içeriğinde değil).
            Assert.IsNull(card.GetComponentInParent<UnityEngine.UI.ScrollRect>());
            // Ülke sekmesi: adı ülkeden.
            Assert.AreEqual("Türkiye", GameController.CountryName("TR"));
            _gc.ShowRanking(LeaderboardScope.Country);
            yield return null;
            Assert.IsNotNull(GameObject.Find("Row10"));
            Assert.AreNotEqual("284", Text(Child("SelfCard", "RankPill/Rank")), "ülke sırası ayrı");
        }

        [UnityTest]
        public IEnumerator NoServer_ShowsOfflineAndRetry_NoRows_NoRank()
        {
            GameController.DevLeaderboardSample = false;
            GameController.CountryOverride = "TR";
            Progress(25);
            _gc.ShowRanking();
            yield return null;
            Assert.IsNull(GameObject.Find("Row1"), "sahte satır yok");
            Assert.IsNotNull(GameObject.Find("Retry"), "yeniden dene");
            Assert.AreEqual("—", Text(Child("SelfCard", "RankPill/Rank")), "sıra yoksa 0 yazılmaz");
        }

        [UnityTest]
        public IEnumerator NoCountry_IsNotInvented_NotEligible_ShowsProgress()
        {
            GameController.DevLeaderboardSample = true;
            GameController.CountryOverride = "";
            Progress(5);
            _gc.ShowRanking(LeaderboardScope.Country);
            yield return null;
            Assert.IsNull(GameObject.Find("Row1"));
            Assert.IsNotNull(GameObject.Find("Message"));
            StringAssert.Contains("5/20", Text(Child("SelfCard", "Foot")));
            Assert.AreEqual("—", Text(Child("SelfCard", "RankPill/Rank")));
        }
    }
}
