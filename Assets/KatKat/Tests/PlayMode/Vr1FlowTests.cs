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
            KatKat.UI.Vr2.Enabled = true;
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

        private IEnumerator WaitStable()
        {
            float limit = Time.realtimeSinceStartup + 20f;
            while ((_gc.CurrentPhase == GameController.Phase.Animating || _gc.IsCelebrating) && Time.realtimeSinceStartup < limit) yield return null;
        }

        /// <summary>
        /// vr1 P2/P3 pilot: haritada Pilot düğmesi (yalnız prototip); aşamalı pilot bölüm kayıtlı çözümle kazanılır,
        /// etkin olmayan aşamanın hedefi soluk görünür, ana ilerleme/yıldız değişmez.
        /// </summary>
        [UnityTest]
        public IEnumerator Pilot_StagedLevel_SavedSolutionWins_MainProgressUntouched()
        {
            Vr1Proto.Override = false;
            _gc.ShowLevels();
            yield return null;
            Assert.IsNull(Find("PilotButton"), "anahtar kapalıyken pilot yok");
            Vr1Proto.Override = true;
            _gc.ShowLevels();
            yield return null;
            Assert.IsNotNull(Find("PilotButton"));
            int completed = _gc.SaveService.Data.completed.Count, stars = _gc.SaveService.Data.TotalStars;
            _gc.ShowPilots();
            _gc.StartTrial(GameController.LabCount + 4); // pilot_05 aşamalı hedef (2026-11-21: Test Bölümlerinde ilk 10 yeni bölüm)
            yield return null;
            Assert.IsTrue(_gc.InPilot);
            Assert.AreEqual("pilot_stages", _gc.CurrentLevel.TrialType);
            Assert.Less(_gc.Hud.GoalChipAlpha(1), 1f, "sonraki aşama soluk");
            _gc.Board.Director.TimeScale = 10f;
            foreach (var step in _gc.CurrentLevel.Solution)
            {
                _gc.PlayStep(step);
                yield return WaitStable();
            }
            Assert.AreEqual(KatKat.Core.GameStatus.Won, _gc.Session.State.Status);
            Assert.AreEqual(completed, _gc.SaveService.Data.completed.Count, "ana ilerleme değişmez");
            Assert.AreEqual(stars, _gc.SaveService.Data.TotalStars, "yıldız değişmez");
        }

        /// <summary>C03: taş kazanıldı, uygulama sunumdan önce kapandı → sonraki açılışta vitrin bir kez; taş ikinci kez verilmez.</summary>
        [UnityTest]
        public IEnumerator PendingReveal_SurvivesRestart_ShownOnce()
        {
            var d = _gc.SaveService.Data;
            var stone = _gc.Stones.Stones.Find(s => s.Category == StoneCategory.StarStone);
            if (!d.meta.stones.Contains(stone.StoneId)) d.meta.stones.Add(stone.StoneId);
            d.meta.pendingReveal.Add(stone.StoneId);
            int owned = d.meta.stones.Count;
            _gc.SaveService.Save();
            // Uygulama yeniden açılır (aynı kayıt klasörü).
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
            yield return null;
            Assert.IsTrue(_gc.Showcase.IsOpen, "bekleyen sunum yeniden gösterilir");
            _gc.Showcase.Close();
            yield return null;
            d = _gc.SaveService.Data;
            Assert.IsFalse(d.meta.pendingReveal.Contains(stone.StoneId), "görüldü kaydı");
            Assert.AreEqual(owned, d.meta.stones.Count, "ödül yeniden verilmez");
        }

        /// <summary>Azaltılmış hareket: kabuk titremesi ve parçacık yok; 180 ms geçişle açılır, vitrin yine gelir.</summary>
        [UnityTest]
        public IEnumerator ReducedMotion_ShatterIsCrossfade()
        {
            Vr1Proto.Override = true;
            KatKat.UI.Motion.Reduced = true;
            try
            {
                Progress(10);
                var d = _gc.SaveService.Data;
                CollectionRules.GrantUnlocked(d.meta, _gc.Stones, d.IsCompleted);
                d.meta.dailyStars = 0;
                d.meta.dailyStars = Mathf.Max(0, 58 - d.TotalStars);
                _gc.ShowLevels();
                yield return null;
                d.meta.dailyStars += 3;
                typeof(GameController).GetMethod("CheckStoneUnlocks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(_gc, null);
                _gc.ShowLevels();
                yield return null;
                var fx = _gc.Menu.Vr1Fx;
                Assert.IsNotNull(fx);
                fx.Advance(4000f);
                Assert.IsTrue(fx.Revealed);
                Assert.IsNull(Find("Shard"), "parçacık yok");
                Assert.IsTrue(_gc.Showcase.IsOpen);
            }
            finally { KatKat.UI.Motion.Reduced = false; }
        }

        /// <summary>Anahtar kapalı/açık gerçek süreli tek hamle (kayıtlı çözümün ilk adımı) oynatma süresi.</summary>
        private IEnumerator TimeFirstMove(int level, bool proto, System.Action<float> result)
        {
            Vr1Proto.Override = proto;
            _gc.StartLevel(level);
            _gc.Board.Director.TimeScale = 1f;
            yield return null;
            var before = _gc.Session.State.Clone();
            float t0 = Time.realtimeSinceStartup;
            _gc.PlayStep(_gc.CurrentLevel.Solution[0]);
            while (_gc.CurrentPhase == GameController.Phase.Animating && Time.realtimeSinceStartup - t0 < 15f) yield return null;
            result(Time.realtimeSinceStartup - t0);
        }

        /// <summary>
        /// 04 animasyon tablosu (taslak): prototipte hamle oynatması kısalır; motor sonucu aynıdır (süre ZPE karar süresine
        /// zaten girmez, P0). Ölçüm editörde, cihaz kare süresi değildir.
        /// </summary>
        [UnityTest]
        public IEnumerator AnimationTable_ShorterPlayback_SameResult()
        {
            float off = 0f, on = 0f;
            yield return TimeFirstMove(10, false, t => off = t);
            var stateOff = _gc.Session.State.Clone();
            yield return TimeFirstMove(10, true, t => on = t);
            var stateOn = _gc.Session.State.Clone();
            Debug.Log($"[KatKat] vr1 animasyon: ilk hamle eski {off * 1000f:0} ms, prototip {on * 1000f:0} ms");
            Assert.IsTrue(stateOff.ContentEquals(stateOn), "aynı komut aynı sonuç");
            Assert.Less(on, off * 0.8f, "prototip oynatma belirgin kısa"); // v14: zincirsiz tek dalga; oran ~0,75 sınırda oynuyordu
        }

        /// <summary>
        /// C05 → v14 (kullanıcı 2026-11-16: "ortası delik tahtaları kaldıralım"): ana katalogda iç boşluklu tahta yok; buzlu
        /// merkez taşı (yalnız var olan iç boşluğa konur, yeni delik açılmaz) bu yüzden hiçbir ana bölümde çıkmaz — eski
        /// görünüm (VR2 kapalı) ve prototip anahtarı açıkken de.
        /// </summary>
        [UnityTest]
        public IEnumerator Catalog_HasNoInteriorHoles_CenterIceNeverPlaced()
        {
            for (int i = 0; i < _gc.Catalog.Count; i++)
            {
                var st = KatKat.Levels.LevelLoader.ToState(_gc.Catalog.Levels[i]);
                Assert.IsFalse(KatKat.Levels.BoardHoles.Largest(st.Board).Has, $"{_gc.Catalog.Levels[i].Id}: iç boşluk olmamalı");
            }
            KatKat.UI.Vr2.Enabled = false;
            Vr1Proto.Override = true;
            foreach (int i in new[] { 0, 99, 150, 499, _gc.Catalog.Count - 1 })
            {
                _gc.StartLevel(i);
                yield return null;
                Assert.IsNull(_gc.CenterIce, $"{i + 1}. bölüm: boşluk yok, yeni delik açılmaz");
            }
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
