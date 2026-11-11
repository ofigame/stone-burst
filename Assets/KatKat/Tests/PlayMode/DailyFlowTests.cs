using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using KatKat.Game;
using KatKat.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    /// <summary>
    /// v9 B kabul akışı (gerçek GameController ile): eski kayıtla açılış → ilk taş → günlük giriş (iptal edilen reklam
    /// hakkı eksiltmez) → ilk deneme → uygulama kapanıp açılınca aynı deneme → taş gücü (bir kez) → ücretsiz tekrar →
    /// başarı → normal bölümde +1 hamle → gün değişince ödül biter. Normal ilerleme ve ödül günlüğü karışmaz.
    /// </summary>
    public class DailyFlowTests
    {
        private const float Speed = 8f;
        private GameController _gc;
        private FixedDayClock _clock;

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(_gc);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameController.RequestedLevel = -1;
            GameController.ToursOverride = false;
            GameController.SaveDirectoryOverride = Path.Combine(Application.temporaryCachePath, "katkat_daily_test");
            if (Directory.Exists(GameController.SaveDirectoryOverride)) Directory.Delete(GameController.SaveDirectoryOverride, true);
            Directory.CreateDirectory(GameController.SaveDirectoryOverride);
            // Eski (v1) kayıt: 1–10 tamamlandı, bazı yıldızlar.
            var ids = new System.Text.StringBuilder();
            for (int i = 1; i <= 10; i++) ids.Append(i > 1 ? "," : "").Append($"\"level_{i:000}\"");
            File.WriteAllText(Path.Combine(GameController.SaveDirectoryOverride, KatKat.Save.SaveService.FileName),
                "{\"saveVersion\":1,\"highestUnlocked\":10,\"completed\":[" + ids + "],\"sound\":true,\"vibration\":true,\"starIds\":[\"level_001\"],\"starCounts\":[3]}");
            _clock = new FixedDayClock { DayId = "2026-11-06" };
            GameController.ClockOverride = _clock;
            GameController.FakeAds.Next = AdOutcome.Rewarded;
            GameController.FakeAds.DuplicateCallback = false;
            GameController.FakeAdFree.Owned = false;
            yield return Load();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameController.ClockOverride = null;
            GameController.ToursOverride = null;
            yield return null;
        }

        private IEnumerator WaitStable()
        {
            float limit = Time.realtimeSinceStartup + 20f;
            while (_gc.CurrentPhase == GameController.Phase.Animating && Time.realtimeSinceStartup < limit) yield return null;
            Assert.AreNotEqual(GameController.Phase.Animating, _gc.CurrentPhase, "Animasyon bitmedi");
        }

        [UnityTest]
        public IEnumerator FullDailyFlow_FromOldSave_ToMoveBonus()
        {
            var meta = _gc.SaveService.Data.meta;
            Assert.AreEqual(SaveDataVersion, _gc.SaveService.Data.saveVersion, "eski kayıt yükseltildi");
            CollectionAssert.AreEqual(new[] { "stone_tide" }, meta.stones, "öğretici sonrası eşik (10) geçilmiş: taş verildi");

            // 2026-11-11: oyuncunun ilk günlük girişi ücretsiz (MetaTests'te ayrı test); bu test reklamlı girişi sınar.
            meta.dailyFreeUsed = true;
            // Reklam iptali hakkı eksiltmez.
            _gc.ShowDaily();
            GameController.FakeAds.Next = AdOutcome.Cancelled;
            _gc.PressDailyEnterForTest();
            Assert.IsFalse(_gc.Daily.State.entered);
            GameController.FakeAds.Next = AdOutcome.Rewarded;
            _gc.PressDailyEnterForTest();
            Assert.IsTrue(_gc.Daily.State.entered);
            Assert.AreEqual(2, _gc.Daily.AttemptsLeft);

            // İlk deneme; uygulama kapanıp açılırsa aynı deneme döner, hak yeniden harcanmaz.
            Assert.IsTrue(_gc.StartDaily());
            Assert.AreEqual(SessionKind.Daily, _gc.CurrentSessionKind);
            Assert.AreEqual(1, _gc.Daily.State.attemptsUsed);
            yield return Load();
            Assert.IsTrue(_gc.Daily.State.attemptOpen);
            Assert.IsTrue(_gc.StartDaily());
            Assert.AreEqual(1, _gc.Daily.State.attemptsUsed, "kapanma sonrası aynı deneme");

            // v11: seçili taş alt güç alanında gerçek görseliyle; dokununca güç kullanılır.
            Assert.IsTrue(_gc.Hud.StonePowerShown, "günlükte taş yuvası görünür");
            Assert.AreSame(Resources.Load<Sprite>("Collection/stones/stone_tide"), _gc.Hud.StonePowerSprite, "taşın gerçek PNG'si");
            Assert.IsTrue(_gc.Hud.StonePowerReady);
            var slot = GameObject.Find("StonePower");
            Assert.IsNotNull(slot);
            Assert.IsFalse(_gc.Session.State.RollSlotFull);
            slot.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.IsTrue(_gc.Daily.State.powerUsed, "yuvaya dokunmak gücü kullandı");
            Assert.IsFalse(_gc.Hud.StonePowerReady, "kullanılınca soluk");
            Assert.IsTrue(_gc.Hud.StonePowerShown, "kullanıldıktan sonra da yerinde (soluk)");

            // Taş gücü: denemede bir kez.
            Assert.IsTrue(_gc.Session.State.RollSlotFull);
            Assert.IsTrue(_gc.LastOutcome.Has(AssistFlags.StonePower));
            _gc.Session.State.RollSlotFull = false;
            Assert.IsFalse(_gc.UseDailyPowerForTest(), "ikinci kullanım yok");
            Assert.IsFalse(_gc.Session.State.RollSlotFull);

            // Ücretsiz tekrar (yeniden başlat = yeni deneme).
            _gc.Restart();
            Assert.AreEqual(2, _gc.Daily.State.attemptsUsed);
            Assert.AreEqual(0, _gc.Daily.AttemptsLeft);
            Assert.IsFalse(_gc.Daily.State.powerUsed);

            // Kayıtlı çözümle (güç kullanmadan) başarı.
            _gc.Board.Director.TimeScale = Speed;
            var level = _gc.CurrentLevel;
            foreach (var step in level.Solution)
            {
                _gc.PlayStep(step);
                yield return WaitStable();
            }
            float limit = Time.realtimeSinceStartup + 60f;
            while (_gc.IsCelebrating && Time.realtimeSinceStartup < limit) yield return null;
            Assert.IsTrue(_gc.Daily.State.won);
            meta = _gc.SaveService.Data.meta; // sahne yeniden yüklendi: yeni kayıt nesnesi
            Assert.AreEqual(1, meta.rewardLog.Count);
            Assert.AreEqual("2026-11-06", meta.moveBonusDay);
            Assert.IsTrue(_gc.LastOutcome.Finished && _gc.LastOutcome.Success);
            Assert.AreEqual(SessionKind.Daily, _gc.LastOutcome.Kind);
            Assert.AreEqual("daily-v3", _gc.LastOutcome.ContentVersion);
            Assert.GreaterOrEqual(_gc.Daily.State.tier, 1, "günün seviye grubu kayda sabitlendi");
            Assert.AreEqual(2, _gc.Worlds.CountFor(_gc.Catalog.Count), "dünya kataloğu oyunda yüklü");
            _gc.ShowDaily();
            var tierGo = GameObject.Find("TierText");
            Assert.IsNotNull(tierGo, "günlük ekranında seviye grubu gösteriliyor");
            var tmp = tierGo.GetComponent("TextMeshProUGUI");
            string tierText = (string)tmp.GetType().GetProperty("text").GetValue(tmp);
            StringAssert.Contains("1–40", tierText, "10. bölümdeki oyuncu ilk grupta");

            // Normal ilerleme günlükten etkilenmedi.
            Assert.AreEqual(10, _gc.SaveService.Data.completed.Count);
            Assert.IsFalse(_gc.SaveService.Data.starIds.Contains(level.Id));

            // +1 hamle: yalnız kazanılmamış normal bölümde, oturum açılırken.
            _gc.StartLevel(11);
            Assert.IsFalse(_gc.Hud.StonePowerShown, "normal bölümde taş yuvası yok");
            Assert.AreEqual(_gc.Catalog.Levels[11].MoveLimit + 1, _gc.Session.State.MovesLeft);
            Assert.AreEqual(1, _gc.LastOutcome.MoveBonus);
            // ZPE v3 (2026-11-11): +1 hamle yalnız temel sınırı aşan hamle kullanılırsa (oturum sonunda) yardım sayılır.
            Assert.IsFalse(_gc.LastOutcome.Has(AssistFlags.DailyMoveBonus), "açılışta henüz yardım değil");
            Assert.IsTrue(_gc.LastOutcome.FirstAttempt);
            _gc.Restart();
            Assert.AreEqual(_gc.Catalog.Levels[11].MoveLimit + 1, _gc.Session.State.MovesLeft, "aynı oturumda limit sabit");
            Assert.AreEqual(2, _gc.LastOutcome.AttemptNumber);
            Assert.IsFalse(_gc.LastOutcome.FirstAttempt);
            _gc.StartLevel(0);
            Assert.AreEqual(_gc.Catalog.Levels[0].MoveLimit, _gc.Session.State.MovesLeft, "kazanılmış bölüme etki yok");

            // Gün değişti: ödül yeni oturumlara taşınmaz.
            _clock.DayId = "2026-11-07";
            _gc.StartLevel(11);
            Assert.AreEqual(_gc.Catalog.Levels[11].MoveLimit, _gc.Session.State.MovesLeft);
            _gc.ShowDaily();
            Assert.IsFalse(_gc.Daily.State.entered, "yeni gün: yeni giriş gerekir");
            Assert.AreEqual(1, meta.rewardLog.Count);
        }

        [UnityTest]
        public IEnumerator LeavingDailyMidAttempt_SpendsIt_NoFreeRestartByReentering()
        {
            _gc.ShowDaily();
            _gc.PressDailyEnterForTest();
            Assert.IsTrue(_gc.StartDaily());
            _gc.ShowLevels(); // bilerek çıkış
            Assert.IsFalse(_gc.Daily.State.attemptOpen);
            Assert.AreEqual(1, _gc.Daily.AttemptsLeft);
            Assert.IsTrue(_gc.LastOutcome.Finished && !_gc.LastOutcome.Success);
            yield return null;
        }

        /// <summary>v10 P2.6: koleksiyon 22 öğeyi kategorilerle gösterir; görseller gerçek sprite; kilitli hazinenin adı gizli.</summary>
        [Test]
        public void Collection_Shows12Items_WithRealArt_TreasuresHidden()
        {
            foreach (var s in _gc.Stones.Stones)
            {
                var sp = Resources.Load<Sprite>(s.ArtKey);
                Assert.IsNotNull(sp, s.StoneId + ": görsel sprite olarak yüklenmedi (" + s.ArtKey + ")");
            }
            _gc.ShowCollection();
            var cells = new List<GameObject>();
            foreach (var rt in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
                if (rt.gameObject.activeInHierarchy && rt.name.StartsWith("Item_")) cells.Add(rt.gameObject);
            // 2026-11-11: hazine bulma bölümlere bağlı olmadığından bulunmamış Gizli Hazineler gösterilmez (10 yıldız + 2 efsane).
            Assert.AreEqual(12, cells.Count);
            var tide = cells.Find(c => c.name == "Item_stone_tide");
            Assert.AreEqual("stone_tide", tide.transform.Find("Icon").GetComponent<Image>().sprite.name, "Gelgit Taşı gerçek görselle");
            Assert.IsNull(cells.Find(c => c.name == "Item_treasure_ruby_cup"), "bulunmamış hazine gösterilmez");
            Assert.AreEqual(1, Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Count(r => r.gameObject.activeInHierarchy && r.name == "Section"), "ilk kategori sabit kapsülde, efsaneler içerikte");
            Assert.IsNotNull(GameObject.Find("CategoryCapsule"), "sabit kategori kapsülü");
        }

        [UnityTest]
        public IEnumerator DailyAndCollection_BackButtonAndTabs_ReturnToMap()
        {
            string map = KatKat.UI.Loc.T("ui.map"), settings = KatKat.UI.Loc.T("ui.settings");

            // v10: günlük haritanın üstünde modal; kapatınca harita yerinde kalır.
            _gc.ShowDaily();
            Assert.IsTrue(_gc.Menu.IsMapShown, "günlük modalının altında harita");
            Assert.IsTrue(_gc.Menu.IsDailyShown);
            Assert.IsTrue(_gc.Menu.PressForTest("Back"), "günlük: kapat düğmesi");
            Assert.IsFalse(_gc.Menu.IsDailyShown);
            Assert.IsTrue(_gc.Menu.IsMapShown, "günlük → kapat → harita");

            _gc.ShowDaily();
            Assert.IsTrue(_gc.Menu.PressForTest("Dim"), "günlük: karartmaya dokunma kapatır");
            Assert.IsFalse(_gc.Menu.IsDailyShown);
            Assert.IsTrue(_gc.Menu.IsMapShown);

            // Koleksiyondan Günlük sekmesi: harita kurulur, modal üstünde açılır.
            _gc.ShowCollection();
            Assert.IsTrue(_gc.Menu.PressForTest(KatKat.UI.Loc.T("daily.short")), "taşlar: Günlük sekmesi");
            Assert.IsTrue(_gc.Menu.IsMapShown && _gc.Menu.IsDailyShown);
            _gc.Menu.CloseDaily();

            _gc.ShowCollection();
            Assert.IsFalse(_gc.Menu.IsMapShown);
            Assert.IsTrue(_gc.Menu.PressForTest("Back"), "taşlar: geri düğmesi");
            Assert.IsTrue(_gc.Menu.IsMapShown, "taşlar → geri → harita");

            _gc.ShowCollection();
            Assert.IsTrue(_gc.Menu.PressForTest(map), "taşlar: Harita sekmesi");
            Assert.IsTrue(_gc.Menu.IsMapShown);

            _gc.ShowCollection();
            Assert.IsTrue(_gc.Menu.PressForTest(settings), "taşlar: Ayarlar sekmesi");
            Assert.IsFalse(_gc.Menu.IsMapShown, "ayarlar açıldı");
            Assert.IsTrue(_gc.Menu.PressForTest(map), "ayarlar: Harita sekmesi");
            Assert.IsTrue(_gc.Menu.IsMapShown);

            // Alt menü: Harita | Günlük | Taşlar (Oyna yok); Ayarlar haritada sağ üstte.
            string daily = KatKat.UI.Loc.T("daily.short"), stones = KatKat.UI.Loc.T("collection.short");
            Assert.IsFalse(_gc.Menu.PressForTest("Play"), "alt menüde Oyna yok");
            Assert.IsTrue(_gc.Menu.PressForTest(daily), "harita: Günlük sekmesi");
            Assert.IsTrue(_gc.Menu.IsMapShown && _gc.Menu.IsDailyShown, "günlük harita üstünde modal");
            _gc.Menu.CloseDaily();
            Assert.IsTrue(_gc.Menu.PressForTest(stones), "harita: Taşlar sekmesi");
            Assert.IsTrue(_gc.Menu.PressForTest(map));
            Assert.IsTrue(_gc.Menu.PressForTest(settings), "harita: sağ üst Ayarlar");
            Assert.IsFalse(_gc.Menu.IsMapShown);
            yield return null;
        }

        private static int SaveDataVersion => KatKat.Save.SaveData.CurrentVersion;
    }
}
