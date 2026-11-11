using System.Collections;
using System.IO;
using KatKat.Game;
using KatKat.Meta;
using KatKat.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace KatKat.Tests
{
    /// <summary>
    /// vr1 P1 prototipi (03_KOLEKSIYON_ODUL, 06_TEST_KABUL C01/C03/C06) gerçek GameController ile: anahtar kapalıyken eski
    /// kart; açıkken buzlu taş, haritaya dönüşte tek özet (tekrar girişte çift yıldız yok), eşikte taş bir kez kazanılır,
    /// kabuk kırılınca vitrin açılır.
    /// </summary>
    public class Vr1FlowTests
    {
        private GameController _gc;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameController.RequestedLevel = -1;
            GameController.ToursOverride = false;
            GameController.SaveDirectoryOverride = Path.Combine(Application.temporaryCachePath, "katkat_vr1_test");
            if (Directory.Exists(GameController.SaveDirectoryOverride)) Directory.Delete(GameController.SaveDirectoryOverride, true);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(_gc);
        }

        [TearDown]
        public void TearDown()
        {
            Vr1Proto.Override = null;
            GameController.ToursOverride = null;
        }

        private void Progress(int n, int extraStars = 0)
        {
            var d = _gc.SaveService.Data;
            for (int i = 0; i < n; i++)
            {
                _gc.SaveService.MarkCompleted(_gc.Catalog.IdAt(i), i, _gc.Catalog.Count);
                _gc.SaveService.RecordStars(_gc.Catalog.IdAt(i), 2);
            }
            d.meta.dailyStars = extraStars;
        }

        private static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.gameObject.activeInHierarchy && t.name == name) return t.gameObject;
            return null;
        }

        [UnityTest]
        public IEnumerator ProtoOff_KeepsOldCard_NoReturnFx()
        {
            Vr1Proto.Override = false;
            Progress(10);
            _gc.SaveService.Data.meta.mapSeenStars = 0;
            _gc.SaveService.Data.meta.mapSeenLevels = 0;
            _gc.ShowLevels();
            yield return null;
            Assert.IsNotNull(Find("Lock"), "eski kilitli kart");
            Assert.IsNull(Find("IceRim"));
            Assert.IsNull(_gc.Menu.Vr1Fx, "anahtar kapalı: dönüş animasyonu yok");
            Assert.AreEqual(_gc.SaveService.Data.TotalStars, _gc.SaveService.Data.meta.mapSeenStars, "görülme işareti yine güncellenir");
        }

        /// <summary>C06: haritaya dönüş özeti tek receipt; harita yeniden açılınca aynı yıldızlar tekrar uçmaz.</summary>
        [UnityTest]
        public IEnumerator ReturnSummary_ShownOnce()
        {
            Vr1Proto.Override = true;
            Progress(10);
            var meta = _gc.SaveService.Data.meta;
            int total = _gc.SaveService.Data.TotalStars;
            meta.mapSeenStars = total - 4;
            meta.mapSeenLevels = 8;
            _gc.ShowLevels();
            yield return null;
            Assert.IsNotNull(Find("IceRim"), "buzlu taş");
            var fx = _gc.Menu.Vr1Fx;
            Assert.IsNotNull(fx);
            Assert.AreEqual(3, fx.StarsLaunched, "en çok 3 uçan yıldız");
            StringAssert.Contains("2", _gc.Menu.ToastText ?? "", "özet: 2 yeni bölüm");
            fx.Advance(3000f);
            Assert.IsTrue(fx.Counted);
            Assert.IsTrue(fx.Done);
            Assert.AreEqual(total, meta.mapSeenStars);
            _gc.ShowLevels();
            yield return null;
            Assert.IsNull(_gc.Menu.Vr1Fx, "ikinci açılışta özet yok");
        }

        /// <summary>C01/C03: 58 → 61 yıldız: 60 eşikli taş bir kez kazanılır; kabuk kırılınca vitrin açılır, sonra harita sıradaki hedefe geçer.</summary>
        [UnityTest]
        public IEnumerator Unlock_ShatterThenShowcase_Once()
        {
            Vr1Proto.Override = true;
            Progress(10);
            var d = _gc.SaveService.Data;
            CollectionRules.GrantUnlocked(d.meta, _gc.Stones, d.IsCompleted);
            d.meta.dailyStars = 0;
            d.meta.dailyStars = Mathf.Max(0, 58 - d.TotalStars);
            _gc.ShowLevels(); // görülme işareti 58
            yield return null;
            d.meta.dailyStars += 3; // 61
            var target = _gc.Stones.Stones.Find(s => s.Category == StoneCategory.StarStone && s.UnlockType == StoneUnlock.StarsTotal && s.UnlockValue == 60);
            Assert.IsNotNull(target);
            Assert.IsFalse(d.meta.stones.Contains(target.StoneId));
            typeof(GameController).GetMethod("CheckStoneUnlocks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(_gc, null);
            Assert.IsTrue(d.meta.stones.Contains(target.StoneId), "kazanım kayıtta (sunumdan önce)");
            int owned = d.meta.stones.Count;
            _gc.ShowLevels();
            yield return null;
            var fx = _gc.Menu.Vr1Fx;
            Assert.IsNotNull(fx);
            Assert.IsFalse(_gc.Showcase.IsOpen, "vitrin kabuk kırılmadan açılmaz");
            fx.Advance(4000f);
            Assert.IsTrue(fx.Revealed);
            Assert.IsTrue(_gc.Showcase.IsOpen, "kırılmadan sonra vitrin");
            Assert.AreEqual(owned, d.meta.stones.Count, "sunum taşı ikinci kez vermez");
        }
    }
}
