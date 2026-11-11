using System.Collections;
using System.IO;
using KatKat.Game;
using KatKat.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KatKat.Tests
{
    /// <summary>
    /// Kullanıcı (2026-11-11): "Günlük seçildiğinde ekranda modal açılıyor ve harita menüsü seçili kalıyor. günlük modalı
    /// açıksa günlük menüsü seçili olsun." ve "seçili olan menü iconu büyüyüp küçülsün".
    /// </summary>
    public class MenuTabTests
    {
        private GameController _gc;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameController.RequestedLevel = -1;
            GameController.ToursOverride = false;
            GameController.SaveDirectoryOverride = Path.Combine(Application.temporaryCachePath, "katkat_tab_test");
            if (Directory.Exists(GameController.SaveDirectoryOverride)) Directory.Delete(GameController.SaveDirectoryOverride, true);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(_gc);
        }

        [TearDown]
        public void TearDown() => GameController.ToursOverride = null;

        /// <summary>Etkin "Tabs" altında seçili sekmenin adı (arkasında "Active" kapsülü olan öğe).</summary>
        private static string SelectedTab()
        {
            string sel = null;
            int holders = 0;
            foreach (var t in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
            {
                if (!t.gameObject.activeInHierarchy || t.name != "Tabs") continue;
                holders++;
                foreach (var b in t.GetComponentsInChildren<Button>())
                    if (b.transform.Find("Active") != null) sel = b.name;
            }
            Assert.AreEqual(1, holders, "tek alt menü");
            return sel;
        }

        [UnityTest]
        public IEnumerator DailyModal_SelectsDailyTab_CloseRestoresMap()
        {
            _gc.ShowLevels();
            yield return null;
            string map = Loc.T("ui.map"), daily = Loc.T("daily.short");
            Assert.AreEqual(map, SelectedTab());
            _gc.ShowDaily();
            yield return null;
            Assert.IsTrue(_gc.Menu.IsDailyShown);
            Assert.AreEqual(daily, SelectedTab(), "günlük açıkken Günlük seçili");
            _gc.Menu.CloseDaily();
            yield return null;
            Assert.AreEqual(map, SelectedTab(), "kapanınca Harita seçili");
        }

        [UnityTest]
        public IEnumerator SelectedIcon_Breathes_OthersStatic()
        {
            _gc.ShowLevels();
            yield return null;
            var all = Object.FindObjectsByType<IconBreath>(FindObjectsSortMode.None);
            Assert.AreEqual(1, all.Length, "yalnız seçili ikon");
            all[0].Advance(IconBreath.Period * 0.5f);
            Assert.Greater(all[0].transform.localScale.x, 1.05f);
            all[0].Advance(IconBreath.Period * 0.5f);
            Assert.AreEqual(1f, all[0].transform.localScale.x, 0.01f);
        }
    }
}
