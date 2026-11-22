using System.Collections;
using KatKat.Game;
using KatKat.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    /// <summary>
    /// 3D harita denemesi (kullanıcı 2026-11-22): anahtar açıkken harita 3D kurulur; maskot sıradaki kapıda; kilitli
    /// kapıya dokunmak yürütmez; açık kapıya dokununca maskot yürür, girer ve başlangıç kartı açılır. Anahtar kapalıyken
    /// 3D dünya gizli.
    /// </summary>
    public sealed class Map3DTests
    {
        private GameController _gc;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            GameController.RequestedLevel = -1;
            GameController.SaveDirectoryOverride = System.IO.Path.Combine(Application.temporaryCachePath, "katkat_map3d_test");
            if (System.IO.Directory.Exists(GameController.SaveDirectoryOverride)) System.IO.Directory.Delete(GameController.SaveDirectoryOverride, true);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(_gc);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MenuView.Map3DOverride = null;
            GameController.SaveDirectoryOverride = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator On_BuildsGates_LockedTapIgnored_OpenTapWalksEntersAndOpensStartCard()
        {
            for (int i = 0; i < 3; i++) _gc.SaveService.MarkCompleted(_gc.Catalog.Levels[i].Id, i, _gc.Catalog.Count);
            MenuView.Map3DOverride = true;
            _gc.ShowLevels();
            yield return null;
            var w = _gc.Menu.Map3D;
            Assert.IsNotNull(w, "3D harita kuruldu");
            Assert.IsNotNull(GameObject.Find("Map3D"), "menüde 3D yüzey");
            Assert.Greater(w.GateCount, 3);
            Assert.AreEqual(0, w.FirstIndex, "ilk bölge başı");
            // Önceki testten kalan ilerleme varsa maskot eski kapıdan yürüyerek gelir; bitmesini bekle.
            for (float wt = 0f; w.Walking && wt < 12f; wt += Time.unscaledDeltaTime) yield return null;
            Assert.IsFalse(w.Walking);

            // Kilitli kapı (sıradakinin 2 sonrası) yürütmez.
            Assert.AreEqual(-1, w.TapGate(5));
            Assert.IsFalse(w.Walking);

            // Ekrandaki sıradaki kapıya ışınla dokunma kapıyı bulur (kapı yürüyüşü başlar; burada yalnız isabet).
            Physics.SyncTransforms();
            Assert.AreEqual(3, w.Tap(w.GateViewport(3)), "sıradaki kapıya dokunma");
            for (float wt = 0f; !_gc.Menu.StartCardShown && wt < 8f; wt += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(_gc.Menu.StartCardShown, "sıradaki kapıya varınca kart");
            _gc.ShowLevels();
            yield return null;
            w = _gc.Menu.Map3D;
            // Tamamlanmış (geride kalan) kapı: maskot geri yürür, varınca kart açılır.
            float z0 = w.MascotZ;
            Assert.AreEqual(1, w.TapGate(1));
            Assert.IsTrue(w.Walking);
            float t = 0f;
            while (!_gc.Menu.StartCardShown && t < 8f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(_gc.Menu.StartCardShown, "kapıdan girince başlangıç kartı");
            Assert.Less(w.MascotZ, z0, "maskot geri yürüdü");
        }

        [UnityTest]
        public IEnumerator Off_NoWorld()
        {
            MenuView.Map3DOverride = false;
            _gc.ShowLevels();
            yield return null;
            Assert.IsNull(_gc.Menu.Map3D);
            Assert.IsNull(GameObject.Find("Map3D"));
        }
    }
}
