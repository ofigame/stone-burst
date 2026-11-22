using System.Collections;
using KatKat.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    /// <summary>
    /// v10 ses: dört WAV yüklü; menü müziği tek kaynak ve yeniden çizimde baştan başlamaz; bölüm başlarken söner;
    /// ses ayarı müziği ve efektleri birlikte kontrol eder; zincir WAV'ı hamle başına bir kez; ödül zaferde bir kez.
    /// (Gerçek kulak testi değildir; telefonda dinleme ayrıca yapılmalı.)
    /// </summary>
    public class AudioShellTests
    {
        private GameController _gc;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameController.RequestedLevel = -1;
            GameController.ToursOverride = false;
            GameController.SaveDirectoryOverride = System.IO.Path.Combine(Application.temporaryCachePath, "katkat_audio_test");
            if (System.IO.Directory.Exists(GameController.SaveDirectoryOverride)) System.IO.Directory.Delete(GameController.SaveDirectoryOverride, true);
            System.IO.Directory.CreateDirectory(GameController.SaveDirectoryOverride);
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(_gc);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameController.ToursOverride = null;
            GameController.SaveDirectoryOverride = null;
            if (_gc != null) _gc.Sound.OnPlayed = null;
            yield return null;
        }

        /// <summary>Dinleyici yoksa kaynaklar "çalar" ama cihazdan ses çıkmaz (APK'da sessizlik hatası).</summary>
        [Test]
        public void Scene_HasExactlyOneEnabledAudioListener()
        {
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            Assert.AreEqual(1, listeners.Length, "tek AudioListener");
            Assert.IsTrue(listeners[0].isActiveAndEnabled, "dinleyici etkin");
        }

        [Test]
        public void FourWavs_LoadFromResources()
        {
            foreach (var n in new[] { "peel-pop", "chain-four", "level-reward" }) // menu-loop 2026-11-23 silindi (müzik game-music)
            {
                var c = Resources.Load<AudioClip>("Audio/TriKat/" + n);
                Assert.IsNotNull(c, n);
                Assert.AreEqual(2, c.channels, n + " stereo");
                Assert.AreEqual(44100, c.frequency, n + " 44,1 kHz");
            }
            Assert.IsTrue(_gc.Sound.HasChainWav);
        }

        [Test]
        public void MenuMusic_SingleSource_NoRestartOnRedraw_FadesOnLevel_FollowsSoundSetting()
        {
            Assert.AreEqual(1, Object.FindObjectsByType<MenuMusic>(FindObjectsSortMode.None).Length, "tek müzik oynatıcı");
            _gc.ShowLevels();
            Assert.IsTrue(_gc.Music.Wanted);
            Assert.IsTrue(_gc.Music.IsPlaying, "haritada çalıyor");
            Assert.AreEqual("game-music", _gc.Music.Clip.name); // 2026-11-20: docs/muzik parçası
            var src = _gc.Music.GetComponent<AudioSource>();
            Assert.IsTrue(src.loop);
            Assert.AreEqual(1, _gc.Music.GetComponents<AudioSource>().Length);
            // Menü yeniden çizimi (harita tekrar, koleksiyon, harita) döngüyü baştan başlatmaz: kaynak hiç durmaz.
            _gc.ShowLevels();
            _gc.ShowCollection();
            _gc.ShowLevels();
            Assert.IsTrue(_gc.Music.IsPlaying);

            // Bölüm başlarken durmaz, çok kısılır (2026-11-20: "müzik oyun sırasında çok kısık olsun").
            _gc.StartLevel(0);
            Assert.IsFalse(_gc.Music.Wanted);
            Assert.IsTrue(_gc.Music.InGame);
            _gc.Music.Step(MenuMusic.FadeInMs + 50f);
            Assert.IsTrue(_gc.Music.IsPlaying, "bölümde kısık çalar");
            Assert.AreEqual(MenuMusic.InGameVolume, src.volume, 0.001f);

            // Menüye dönüş: yumuşak başlar.
            _gc.ShowLevels();
            Assert.IsTrue(_gc.Music.IsPlaying);

            // Ses ayarı kapalı: müzik ve efektler hemen susar, yeni ses başlamaz.
            _gc.Music.Enabled = false; _gc.Sound.Enabled = false;
            Assert.IsFalse(_gc.Music.IsPlaying);
            int before = _gc.Sound.PlayCount;
            _gc.Sound.Play(Sfx.Peel);
            _gc.Sound.PlayReward();
            Assert.AreEqual(before, _gc.Sound.PlayCount, "kapalıyken efekt çalmaz");
            _gc.ShowLevels();
            Assert.IsFalse(_gc.Music.IsPlaying, "kapalıyken menüye girmek müziği başlatmaz");
            _gc.Music.Enabled = true; _gc.Sound.Enabled = true;
            Assert.IsTrue(_gc.Music.IsPlaying, "açılınca menüde sürer");
        }

        /// <summary>V16 sesleri (kullanıcı 2026-11-18: "bu sesleri kullanalım"): dosyalar yüklenir, dokunma yeni klibi çalar.</summary>
        [Test]
        public void V16Sounds_LoadAndMapToEvents()
        {
            foreach (var n in new[] { "stone-tap", "stone-pop", "combo-blast" })
                Assert.IsNotNull(Resources.Load<AudioClip>("Audio/V16/" + n), n);
            _gc.Sound.Play(Sfx.Tap);
            Assert.AreEqual("ui-button", _gc.Sound.LastClip); // 2026-11-22: genel tık V17 düğme sesi
        }

        [Test]
        public void PeelUsesPop_ChainWavOncePerMove()
        {
            int c0 = _gc.Sound.PlayCount;
            _gc.Sound.Play(Sfx.Peel);
            Assert.AreEqual("jelly-pop", _gc.Sound.LastClip); // 2026-11-22 V17 jöle sesi (docs/muzik/tas_sesi.wav)
            foreach (var n in new[] { "jelly-pop", "goal-collect", "star-gain", "ui-button", "booster-moves", "booster-recolor", "bomb-charge", "bomb-blast", "rocket-sweep", "roll-lightning", "shovel-dig" })
                Assert.IsTrue(_gc.Sound.HasV17(n), n);
            _gc.Sound.Play(Sfx.Chain, 3);
            Assert.AreEqual("combo-blast", _gc.Sound.LastClip);
            _gc.Sound.PlayReward();
            Assert.AreEqual("level-reward", _gc.Sound.LastClip);
            Assert.AreEqual(c0 + 3, _gc.Sound.PlayCount);
        }

        /// <summary>
        /// Gerçek oyun akışı (v14: zincir yok; övgü sesi oyuncunun büyük hamlesine bağlı, <see cref="KatKat.Core.BigMove"/>):
        /// bölüm 3 (index 2, Altın Rulo turu: ilk dokunuş 8+ taşlık grup) kayıtlı çözümle kazanılır. Övgü WAV'ı hamle başına
        /// en çok bir kez ve yalnız büyük hamlede çalar (sayısı motor tekrarındaki büyük hamle sayısına eşit); ödül bir kez.
        /// </summary>
        [UnityTest]
        public IEnumerator RealLevel_BigMovePraiseOncePerMove_RewardOnce()
        {
            int chainMoves = 0, chainPlays = 0, rewardPlays = 0;
            _gc.StartLevel(2);
            // Beklenen: kayıtlı çözüm motorla tekrar edilir, BigMove seviyesi > 0 olan hamleler sayılır.
            var probe = KatKat.Levels.LevelLoader.ToState(_gc.CurrentLevel);
            int expectedBig = 0;
            foreach (var st in _gc.CurrentLevel.Solution)
                if (KatKat.Core.BigMove.Level(KatKat.Core.RulesEngine.TryApply(probe, KatKat.Levels.LevelLoader.ToCommand(st))) > 0) expectedBig++;
            Assert.Greater(expectedBig, 0, "bölüm 3 çözümünde büyük hamle var (Altın Rulo doğuran grup)");
            _gc.Board.Director.TimeScale = 6f;
            foreach (var step in _gc.CurrentLevel.Solution)
            {
                int chainBefore = chainPlays;
                // Bitiş kutlamasının roketleri sayılmaz (oyuncu hamlesi değil).
                _gc.Sound.OnPlayed = n => { if (n == "combo-blast" && !_gc.IsCelebrating) chainPlays++; if (n == "level-reward") rewardPlays++; };
                _gc.PlayStep(step);
                float limit = Time.realtimeSinceStartup + 20f;
                while (_gc.CurrentPhase == GameController.Phase.Animating && Time.realtimeSinceStartup < limit) yield return null;
                Assert.LessOrEqual(chainPlays - chainBefore, 1, "bir hamlede övgü WAV'ı en çok bir kez");
                if (chainPlays > chainBefore) chainMoves++;
            }
            // 2026-11-22 V17: övgü WAV'ı (combo-blast) kalktı; taş sesi dokunuşta ve varışta (kullanıcı: "eski seslerde var").
            if (_gc.Sound.HasV17("jelly-pop")) Assert.AreEqual(0, chainMoves, "V17: büyük hamlede eski övgü sesi yok");
            else Assert.AreEqual(expectedBig, chainMoves, "övgü sesi yalnız büyük hamlelerde");
            float l2 = Time.realtimeSinceStartup + 30f;
            while (_gc.IsCelebrating && Time.realtimeSinceStartup < l2) yield return null;
            yield return null;
            Assert.AreEqual(1, rewardPlays, "ödül sesi zaferde bir kez");
            _gc.Sound.OnPlayed = null;
        }

        /// <summary>v10 taş vitrini: kazanılmış karta dokununca açılır (3D model varsa döner), kilitli kart açılmaz.</summary>
        [Test]
        public void Showcase_OwnedCardOpens3D_LockedDoesNot()
        {
            var meta = _gc.SaveService.Data.meta;
            meta.stones.Clear();
            meta.stones.Add("stone_ocean");
            meta.stones.Add("stone_tide");
            _gc.ShowCollection();
            var ocean = GameObject.Find("Item_stone_ocean");
            Assert.IsNotNull(ocean.GetComponent<UnityEngine.UI.Button>(), "kazanılmış kart dokunulabilir");
            ocean.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.IsTrue(_gc.Showcase.IsOpen);
            Assert.IsTrue(_gc.Showcase.ShowingModel, "Okyanus Taşı 3D modelle");
            Assert.AreEqual("stone_ocean", _gc.Showcase.ShownId);
            _gc.Showcase.Close();
            Assert.IsFalse(_gc.Showcase.IsOpen);

            var tide = GameObject.Find("Item_stone_tide");
            tide.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.IsTrue(_gc.Showcase.IsOpen && !_gc.Showcase.ShowingModel, "3D modeli olmayan taş büyük PNG ile");
            _gc.Showcase.Close();

            var storm = GameObject.Find("Item_stone_storm");
            Assert.IsNull(storm.GetComponent<UnityEngine.UI.Button>(), "kilitli kart açılmaz");
            _gc.ShowStone("stone_storm", false);
            Assert.IsFalse(_gc.Showcase.IsOpen, "sahip olunmayan taş vitrine çıkmaz");
        }

        [Test]
        public void AllNineModels_LoadWithMaterial()
        {
            foreach (var id in new[] { "stone_ember", "stone_ocean", "treasure_crowned_relic", "treasure_emerald_throne", "treasure_ice_pedestal", "treasure_obelisk", "treasure_rose_throne", "treasure_ruby_cup", "treasure_rune_pedestal" })
            {
                Assert.IsNotNull(Resources.Load<GameObject>("Models/Stones/" + id + "/model"), id + " model");
                Assert.IsNotNull(Resources.Load<Material>("Models/Stones/" + id + "/mat"), id + " malzeme");
            }
        }
    }
}
