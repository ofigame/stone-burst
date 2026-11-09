using System.Collections;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.Game;
using KatKat.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    /// <summary>Gerçek GameController, BoardView ve HUD ile oynanış akışı.</summary>
    public class GameFlowTests
    {
        private GameController _gc;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            GameController.RequestedLevel = 0;
            GameController.SaveDirectoryOverride = System.IO.Path.Combine(Application.temporaryCachePath, "katkat_test_save");
            if (System.IO.Directory.Exists(GameController.SaveDirectoryOverride)) System.IO.Directory.Delete(GameController.SaveDirectoryOverride, true);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            _gc = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(_gc);
            Assert.GreaterOrEqual(_gc.Catalog.Count, 5);
            CollectionAssert.IsEmpty(_gc.Catalog.ParseAll(), "bütün bölümler okunur, kimlik = dosya adı");
        }

        private IEnumerator WaitStable()
        {
            float limit = Time.realtimeSinceStartup + 10f * Mathf.Max(1f, 10f / Speed);
            while (_gc.CurrentPhase == GameController.Phase.Animating && Time.realtimeSinceStartup < limit) yield return null;
            Assert.AreNotEqual(GameController.Phase.Animating, _gc.CurrentPhase, "Animasyon bitmedi");
        }

        /// <summary>Kazanınca bitiş kutlaması (v5 §4) oynar; zafer kartı sonunda açılır.</summary>
        private IEnumerator WaitCelebration()
        {
            float limit = Time.realtimeSinceStartup + 60f;
            while (_gc.IsCelebrating && Time.realtimeSinceStartup < limit) yield return null;
            Assert.IsFalse(_gc.IsCelebrating, "Kutlama bitmedi");
        }

        private void AssertViewMatchesState()
        {
            var b = _gc.Session.State.Board;
            for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
            {
                var s = _gc.Board.StackAt(x, y);
                // M11: açılmamış geçit hücresi görünümde soluk önizleme olarak etkindir.
                Assert.AreEqual(b[x, y].Active || b[x, y].GateId > 0, s.gameObject.activeSelf, $"({x},{y}) etkinlik");
                if (b[x, y].Active) CollectionAssert.AreEqual(b[x, y].Layers, s.Layers, $"({x},{y}) katmanlar");
            }
        }

        /// <summary>
        /// Görsel v3 §6, V10/V12/V20: zafer kartındaki önizleme gerçek sonraki bölümün verisidir; DEVAM'a hızlı iki
        /// basış tek bölüm yükler; son katalog bölümünde sahte önizleme yok.
        /// </summary>
        [UnityTest]
        public IEnumerator VictoryCard_RealNextPreview_DoublePressLoadsOnce_LastLevelHasNoFakePreview()
        {
            _gc.StartLevel(0);
            _gc.Board.Director.TimeScale = Speed;
            foreach (var step in _gc.Catalog.Levels[0].Solution)
            {
                _gc.PlayStep(step);
                yield return WaitStable();
            }
            yield return WaitCelebration();
            var panel = _gc.Hud.Panel;
            Assert.IsNotNull(panel.Preview, "Önizleme kartı");
            Assert.AreEqual(_gc.Catalog.Levels[1].Id, panel.Preview.LevelId);
            Assert.Greater(panel.Preview.CellCount, 0);
            panel.Skip();
            panel.Press(0);
            panel.Press(0);
            yield return null;
            Assert.AreEqual(1, _gc.LevelIndex, "Çift basış tek yükleme");
            Assert.AreEqual(GameController.Phase.Idle, _gc.CurrentPhase);

            int last = _gc.Catalog.Count - 1;
            _gc.StartLevel(last);
            _gc.Board.Director.TimeScale = Speed;
            foreach (var step in _gc.Catalog.Levels[last].Solution)
            {
                _gc.PlayStep(step);
                yield return WaitStable();
            }
            yield return WaitCelebration();
            Assert.IsNotNull(_gc.Hud.Panel.Preview);
            Assert.IsNull(_gc.Hud.Panel.Preview.LevelId, "Son bölümde sonraki bölüm verisi yok");
            Assert.AreEqual(0, _gc.Hud.Panel.Preview.CellCount);
        }

        /// <summary>Döner taban (M08): düğmenin dokunma alanı hücreden ayrı; tek katmanlı taban hamle harcamaz; dönüş görünümle eşleşir.</summary>
        [UnityTest]
        public IEnumerator Rotor_ButtonHitArea_InvalidRotateCostsNothing_ValidRotateMatchesView()
        {
            var d = new LevelData { SchemaVersion = 2, Id = "test_rotor", Width = 4, Height = 4, MoveLimit = 5 };
            d.Capabilities.Add("rotor");
            d.Goals.Add(new GoalData { Type = "collectColor", Color = "blue", Amount = 3, Kind = "" });
            string[][] layers = { new[] { "blue", "coral" }, new[] { "blue" }, new[] { "blue" }, new[] { "yellow" } };
            for (int x = 0; x < 4; x++)
            {
                var c = new CellData { X = x, Y = 0, Active = true, HasRotor = x == 0 };
                c.Layers.AddRange(layers[x]);
                d.Cells.Add(c);
                var below = new CellData { X = x, Y = 1, Active = true, HasRotor = x == 3 };
                below.Layers.AddRange(x == 3 ? new[] { "yellow", "coral" } : new[] { "purple" });
                d.Cells.Add(below);
            }
            _gc.StartCustom(d, 0);
            var stack = _gc.Board.StackAt(0, 0);
            Assert.IsTrue(_gc.Board.TryRotorAt(stack.transform.position + stack.RotorLocal, out int rx, out int ry));
            Assert.AreEqual((0, 0), (rx, ry));
            Assert.IsFalse(_gc.Board.TryRotorAt(stack.transform.position, out _, out _), "Yığının ortası grup dokunuşudur.");

            int moves = _gc.Session.State.MovesLeft;
            _gc.OnRotorPressed(1, 0); // tabansız
            Assert.AreEqual(moves, _gc.Session.State.MovesLeft);

            _gc.OnRotorPressed(0, 0);
            Assert.AreEqual(moves - 1, _gc.Session.State.MovesLeft);
            yield return WaitStable();
            AssertViewMatchesState();
            Assert.AreEqual(LayerColor.Blue, _gc.Session.State.Board[0, 0].Top);
            Assert.AreEqual(GameStatus.Idle, _gc.Session.State.Status, "Döndürme otomatik soyma başlatmaz.");
            _gc.OnCellPressed(0, 0);
            yield return WaitStable();
            Assert.AreEqual(GameStatus.Won, _gc.Session.State.Status);
        }

        /// <summary>
        /// Animasyon hızı: varsayılan 10x (toplu çalıştırma). İzlemek için ortam değişkeni KATKAT_TEST_SPEED
        /// (ör. 1 gerçek hız, 2 iki kat) — tools/unity.ps1 test-play-visible bunu ayarlar.
        /// </summary>
        private static float Speed
        {
            get
            {
                var v = System.Environment.GetEnvironmentVariable("KATKAT_TEST_SPEED");
                return float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s) && s > 0f ? s : 10f;
            }
        }

        /// <summary>İzleme modunda bölüm başında ve sonunda kısa duraklama (gerçek zaman); toplu çalıştırmada yok.</summary>
        private static IEnumerator Breather()
        {
            if (Speed < 10f) yield return new WaitForSecondsRealtime(0.6f);
        }

        // Bütün katalog gerçek animasyonla oynanır; süre bölüm sayısıyla büyür (300 bölüm 6x hızda ~3 dk'yı aştı).
        // Üst sınır 3 saat: toplu 10x çalıştırma ~2–3 dk, izleme modunda (1–2x) bütün katalog uzun sürer.
        [UnityTest, Timeout(10800000)]
        public IEnumerator AllLevels_RecordedSolutionsWinThroughRealController_AndUnlockNext()
        {
            // Kullanıcı (2026-11-08): varsayılan her 100 bölümlük dilimden rastgele 5 bölüm (1000 bölümde 50). Tohum günlüğe
            // yazılır; aynı tohumla tekrar: KATKAT_PLAY_SEED. Bütün bölümler (mağaza derlemesi öncesi): KATKAT_PLAY_ALL=1 (unity.ps1 -All).
            // Kurallar 1000 bölümün tamamında CoreTests/LevelLab ile zaten doğrulanıyor; bu test görünüm katmanını sınar.
            int count = _gc.Catalog.Count;
            var order = new List<int>();
            if (System.Environment.GetEnvironmentVariable("KATKAT_PLAY_ALL") == "1")
                for (int i = 0; i < count; i++) order.Add(i);
            else
            {
                int seed = int.TryParse(System.Environment.GetEnvironmentVariable("KATKAT_PLAY_SEED"), out var s) ? s : System.Environment.TickCount & 0x7FFFFFFF;
                Debug.Log($"[KatKat] Bölüm örneklemi tohumu: {seed} (tekrar: KATKAT_PLAY_SEED={seed})");
                var rng = new System.Random(seed);
                // Kullanıcı (2026-11-08): her 100'lük dilimden rastgele 5 bölüm (1000 bölümde 50).
                var pick = new SortedSet<int>();
                for (int start = 0; start < count; start += 100)
                {
                    int end = Mathf.Min(count, start + 100);
                    int want = Mathf.Min(5, end - start);
                    var block = new SortedSet<int>();
                    while (block.Count < want) block.Add(start + rng.Next(end - start));
                    pick.UnionWith(block);
                }
                order.AddRange(pick);
                Debug.Log("[KatKat] Örneklem: " + string.Join(",", order.ConvertAll(i => (i + 1).ToString())));
            }
            int expectedUnlocked = 0;
            foreach (int i in order)
            {
                _gc.StartLevel(i);
                _gc.Board.Director.TimeScale = Speed;
                foreach (var step in _gc.Catalog.Levels[i].Solution)
                {
                    Assert.AreEqual(GameController.Phase.Idle, _gc.CurrentPhase, $"{i + 1}. bölüm");
                    _gc.PlayStep(step);
                    yield return WaitStable();
                    yield return WaitCelebration();
                    AssertViewMatchesState();
                }
                yield return Breather();
                Assert.AreEqual(GameStatus.Won, _gc.Session.State.Status, $"{i + 1}. bölüm");
                Assert.AreEqual(GameController.Phase.Ended, _gc.CurrentPhase);
                Assert.IsTrue(_gc.Hud.PanelOpen, "Kazanma paneli açılmalı");
                Assert.IsFalse(_gc.Session.CanUndo, "Zafer kaydından sonra geri alma kapalı");
                Assert.IsTrue(_gc.SaveService.Data.IsCompleted(_gc.Catalog.Levels[i].Id));
                expectedUnlocked = System.Math.Max(expectedUnlocked, System.Math.Min(i + 1, count - 1));
                Assert.AreEqual(expectedUnlocked, _gc.SaveService.Data.highestUnlocked, $"{i + 1}. bölüm sonrası açılan");
            }
        }

        [UnityTest]
        public IEnumerator RapidTapsDuringAnimation_OnlyFirstCommandProcessed()
        {
            _gc.StartLevel(2);
            int moves = _gc.Session.State.MovesLeft;
            _gc.OnCellPressed(0, 0);
            for (int i = 0; i < 6; i++) _gc.OnCellPressed(i % 4, 3);
            yield return null;
            _gc.OnCellPressed(2, 3);
            Assert.AreEqual(moves - 1, _gc.Session.State.MovesLeft);
            yield return WaitStable();
            Assert.AreEqual(moves - 1, _gc.Session.State.MovesLeft, "Animasyon sırasındaki dokunuşlar kuyruğa alınmaz");
            AssertViewMatchesState();
        }

        /// <summary>
        /// v11 özel güç simgeleri: bölüm 6'nın kayıtlı çözümüyle özel taş normal yoldan oluşur, tepsiye gelir (yeni
        /// PNG, döndürülmeden), yuva seçilince hedefleme, tahtada çalışır; geri alma tepsiyi geri getirir. Roket
        /// ateşlenince simge ikiye bölünür (iki "RocketHalf"). Kurallar motor testlerinde (SpecialRulesTests).
        /// </summary>
        [UnityTest]
        public IEnumerator V11_Special_CreatedToTray_Targeted_Used_Undone_WithNewIcons()
        {
            Assert.IsTrue(KatKat.View.SpecialArt.HasV11, "v11 simgeleri yüklü");
            _gc.StartLevel(5);
            _gc.Board.Director.TimeScale = Speed;
            var sol = _gc.CurrentLevel.Solution;
            int useAt = sol.FindIndex(s => s.Type == "use");
            Assert.GreaterOrEqual(useAt, 1, "bölüm 6 çözümünde tepsi kullanımı var");
            for (int i = 0; i < useAt; i++) { _gc.PlayStep(sol[i]); yield return WaitStable(); }

            var tray = _gc.Session.State.Tray;
            Assert.Greater(tray.Count, 0, "özel taş normal yoldan tepsiye geldi");
            var kind = tray[0];
            var icon = _gc.Hud.TrayIcon(0);
            Assert.IsTrue(icon.gameObject.activeSelf);
            Assert.AreSame(KatKat.View.SpecialArt.For(kind), icon.sprite, "tepsi yeni simgeyi kullanır");
            Assert.AreEqual(0f, icon.rectTransform.localEulerAngles.z, 0.01f, "dikey simge de döndürülmez");

            // Hedefleme ve çalıştırma: kayıtlı "use" adımı yuvayı seçer (hedefleme) ve hücreye uygular.
            var before = _gc.Session.State.Clone();
            var step = sol[useAt];
            _gc.PlayStep(step);
            int halves = 0;
            float limit = Time.realtimeSinceStartup + 20f;
            // Ateşleme efekti tahtanın kendi zaman çizelgesinde sürer (hamle mantığı bitse de); yarılar en çok 2 sn izlenir.
            float fxUntil = Time.realtimeSinceStartup + 2f;
            while ((_gc.CurrentPhase == GameController.Phase.Animating || (halves < 2 && Time.realtimeSinceStartup < fxUntil)) && Time.realtimeSinceStartup < limit)
            {
                int n = 0;
                foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)) if (sr.name == "RocketHalf") n++;
                halves = Mathf.Max(halves, n);
                yield return null;
            }
            Assert.IsFalse(_gc.Session.State.ContentEquals(before), "özel güç tahtada çalıştı");
            Assert.Less(_gc.Session.State.Tray.Count, before.Tray.Count, "tepsiden harcandı");
            if (kind.IsRocket()) Assert.GreaterOrEqual(halves, 2, "roket simgesi ikiye bölünüp zıt yönlere gitti");
            if (_gc.Session.CanUndo)
            {
                _gc.Undo();
                Assert.IsTrue(_gc.Session.State.ContentEquals(before), "geri alma özel taşı tepsiye geri getirir");
                Assert.AreSame(KatKat.View.SpecialArt.For(kind), _gc.Hud.TrayIcon(0).sprite);
                AssertViewMatchesState();
            }
        }

        [Test]
        public void V11_SpecialIcons_SingleSource_CenteredBoxes_HalvesSplit()
        {
            foreach (var k in new[] { SpecialKind.Rainbow, SpecialKind.RocketRow, SpecialKind.RocketColumn })
            {
                Assert.IsNotNull(KatKat.View.SpecialArt.For(k), k.ToString());
                Assert.AreSame(KatKat.View.SpecialArt.For(k), KatKat.View.SpecialArt.For(k), "önbellek");
                Assert.AreEqual(0f, KatKat.View.TileSkin.SpecialAngle(k), "yön görselde");
                Assert.AreEqual(Vector2.zero, KatKat.View.TileSkin.SpecialOffset(k), "yüz merkezinde");
            }
            Assert.AreEqual(new Vector2(0.85f, 0.30f), KatKat.View.TileSkin.SpecialBox(SpecialKind.RocketRow));
            Assert.AreEqual(new Vector2(0.30f, 0.85f), KatKat.View.TileSkin.SpecialBox(SpecialKind.RocketColumn));
            Assert.AreEqual(new Vector2(0.70f, 0.70f), KatKat.View.TileSkin.SpecialBox(SpecialKind.Rainbow));
            var row = KatKat.View.SpecialArt.For(SpecialKind.RocketRow);
            Assert.Greater(row.rect.width, row.rect.height * 3f, "yatay simge yatay");
            var col = KatKat.View.SpecialArt.For(SpecialKind.RocketColumn);
            Assert.Greater(col.rect.height, col.rect.width * 3f, "dikey simge dikey (döndürülmemiş kaynak)");
            var l = KatKat.View.SpecialArt.Half(SpecialKind.RocketRow, -1);
            var r = KatKat.View.SpecialArt.Half(SpecialKind.RocketRow, 1);
            Assert.AreEqual(row.rect.width * 0.5f, l.rect.width, 0.6f);
            Assert.Less(l.rect.x, r.rect.x, "sol yarı solda");
            var down = KatKat.View.SpecialArt.Half(SpecialKind.RocketColumn, -1);
            var up = KatKat.View.SpecialArt.Half(SpecialKind.RocketColumn, 1);
            Assert.AreEqual(col.rect.height * 0.5f, up.rect.height, 0.6f);
            Assert.Less(down.rect.y, up.rect.y, "alt yarı altta");
            // v11 rulo simgesi (docs/v11/skiller/04-rulo.png): HUD rulo yuvası aynı kaynağı kullanır.
            Assert.IsTrue(KatKat.View.SpecialArt.HasRollIcon);
            Assert.AreSame(Resources.Load<Sprite>("PremiumV6/skills/special-roll"), KatKat.View.SpecialArt.RollIcon);
            Assert.AreSame(KatKat.View.SpecialArt.RollIcon, _gc.Hud.RollIconSprite, "rulo yuvası yeni simgeyi kullanır");
        }

        [UnityTest]
        public IEnumerator Undo_RestoresFullMove_AndRedrawsView()
        {
            _gc.StartLevel(2);
            var before = _gc.Session.State.Clone();
            _gc.OnCellPressed(0, 0); // kazandırmayan hamle (zafer sonrası geri alma kapalıdır)
            yield return WaitStable();
            Assert.IsFalse(_gc.Session.State.ContentEquals(before));
            _gc.Undo();
            Assert.IsTrue(_gc.Session.State.ContentEquals(before));
            Assert.AreEqual(GameController.Phase.Idle, _gc.CurrentPhase);
            AssertViewMatchesState();
        }

        [UnityTest]
        public IEnumerator V12_LossCard_AdExtraMoves_OncePerLevel_NotCountedInZpe()
        {
            _gc.StartLevel(3);
            int zpeGamesBefore = _gc.SaveService.Data.meta.zpeGames;
            _gc.OnCellPressed(0, 0);
            yield return WaitStable();
            var next = LevelSolver.Moves(_gc.Session.State);
            _gc.OnCellPressed(next[0].X, next[0].Y);
            yield return WaitStable();
            Assert.AreEqual(GameStatus.LostOutOfMoves, _gc.Session.State.Status);
            Assert.IsTrue(_gc.ExtraMovesOffered, "kayıp kartında +3 hamle teklifi");
            Assert.AreEqual(zpeGamesBefore + 1, _gc.SaveService.Data.meta.zpeGames, "kayıp ZPE'ye yazıldı");
            KatKat.Meta.FakeRewardedAds fake = GameController.FakeAds;
            fake.Next = KatKat.Meta.AdOutcome.Rewarded;
            _gc.PressExtraMovesForTest();
            Assert.AreEqual(GameStatus.Idle, _gc.Session.State.Status, "oturum sürüyor");
            Assert.AreEqual(3, _gc.Session.State.MovesLeft);
            Assert.IsTrue(_gc.LastOutcome.Has(KatKat.Meta.AssistFlags.RewardedAd));
            Assert.AreEqual(zpeGamesBefore, _gc.SaveService.Data.meta.zpeGames, "kayıp geri çekildi; reklamlı oturum ZPE'ye girmez");
            Assert.IsFalse(_gc.Hud.PanelOpen);
            AssertViewMatchesState();
            // Bölüm başına bir kez: yeniden kaybedince teklif yok.
            while (_gc.Session.State.Status == GameStatus.Idle && _gc.Session.State.MovesLeft > 0)
            {
                var m = LevelSolver.Moves(_gc.Session.State);
                if (m.Count == 0) break;
                _gc.OnCellPressed(m[0].X, m[0].Y);
                yield return WaitStable();
                yield return WaitCelebration();
            }
            if (_gc.Session.State.Status == GameStatus.LostOutOfMoves) Assert.IsFalse(_gc.ExtraMovesOffered, "ikinci teklif yok");
            Assert.AreEqual(zpeGamesBefore, _gc.SaveService.Data.meta.zpeGames, "reklamlı oturumun sonucu da ZPE'ye girmez");
        }

        [UnityTest]
        public IEnumerator WrongChoice_LosesOutOfMoves_ThenUndoFromLossScreen()
        {
            _gc.StartLevel(3); // "Hangisi Önce?": mavi grubu önce soymak kaybettirir.
            _gc.OnCellPressed(0, 0);
            yield return WaitStable();
            var next = LevelSolver.Moves(_gc.Session.State);
            Assert.IsNotEmpty(next);
            _gc.OnCellPressed(next[0].X, next[0].Y);
            yield return WaitStable();
            Assert.AreEqual(GameStatus.LostOutOfMoves, _gc.Session.State.Status);
            Assert.IsTrue(_gc.Hud.PanelOpen);
            _gc.Undo();
            Assert.AreEqual(GameStatus.Idle, _gc.Session.State.Status);
            Assert.AreEqual(1, _gc.Session.State.MovesLeft);
            Assert.IsFalse(_gc.Hud.PanelOpen);
            AssertViewMatchesState();
        }

        [UnityTest]
        public IEnumerator InvalidTap_SpendsNoMove()
        {
            _gc.StartLevel(3);
            int moves = _gc.Session.State.MovesLeft;
            _gc.OnCellPressed(0, 3); // iki mavi: geçersiz
            yield return null;
            Assert.AreEqual(moves, _gc.Session.State.MovesLeft);
            Assert.AreEqual(GameController.Phase.Idle, _gc.CurrentPhase);
        }

        [UnityTest]
        public IEnumerator InterruptedAnimation_RedrawsFromLogicalState()
        {
            _gc.StartLevel(1);
            _gc.OnCellPressed(0, 0);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(GameController.Phase.Animating, _gc.CurrentPhase);
            _gc.Board.CompleteImmediately();
            if (_gc.IsCelebrating) _gc.SkipCelebration();
            Assert.AreNotEqual(GameController.Phase.Animating, _gc.CurrentPhase);
            Assert.IsFalse(_gc.Board.IsPlaying);
            AssertViewMatchesState();
        }

        /// <summary>
        /// Kullanıcı (2026-11-09): "ipucunu aldığımda o ipucuna tekrar tekrar bakabilmem lazım". Reklamlı ipucu başka
        /// hamleden sonra da kalır, düğme reklamsız yeniden gösterir; önerilen taş oynanınca hak biter.
        /// </summary>
        [UnityTest]
        public IEnumerator PaidHint_StaysUntilHintedTileIsPlayed_ButtonShowsItAgain()
        {
            _gc.StartLevel(40);
            _gc.Board.Director.TimeScale = Speed;
            yield return WaitStable();
            var fake = GameController.FakeAds;
            fake.Next = KatKat.Meta.AdOutcome.Rewarded;
            int shown = fake.Shown;
            _gc.PressHintForTest();
            Assert.AreEqual(shown + 1, fake.Shown, "reklam bir kez");
            Assert.IsTrue(_gc.PaidHintCell.HasValue, "ipucu hücresi");
            Assert.IsTrue(_gc.HintRingVisible, "halka görünür");
            var hint = _gc.PaidHintCell.Value;

            // Başka bir dokunuş: ipucu kalır.
            Command? other = null;
            foreach (var c in LevelSolver.Moves(_gc.Session.State))
            {
                if (c.Type != CommandType.Tap || (c.X == hint.X && c.Y == hint.Y)) continue;
                var probe = _gc.Session.State.Clone();
                if (RulesEngine.TryApply(probe, c).Accepted && !probe.IsTerminal) { other = c; break; }
            }
            Assert.IsTrue(other.HasValue, "ipucu dışında oynanabilir dokunuş");
            _gc.OnCellPressed(other.Value.X, other.Value.Y);
            yield return WaitStable();
            Assert.AreEqual(GameController.Phase.Idle, _gc.CurrentPhase);
            Assert.IsTrue(_gc.PaidHintCell.HasValue, "başka hamleden sonra ipucu sürüyor");
            Assert.IsTrue(_gc.HintRingVisible, "tahta durunca halka yeniden görünür");

            // Düğme: reklamsız yeniden gösterir.
            _gc.PressHintForTest();
            Assert.AreEqual(shown + 1, fake.Shown, "ikinci basışta reklam yok");
            Assert.IsTrue(_gc.HintRingVisible);

            // Önerilen taş oynanınca hak biter.
            hint = _gc.PaidHintCell.Value;
            _gc.OnCellPressed(hint.X, hint.Y);
            yield return WaitStable();
            yield return WaitCelebration();
            Assert.IsFalse(_gc.PaidHintCell.HasValue, "önerilen taş oynandı");
            Assert.IsTrue(_gc.PaidHintUsed);
            if (!_gc.Session.State.IsTerminal)
            {
                _gc.PressHintForTest();
                Assert.AreEqual(shown + 1, fake.Shown, "hak bitince yeni reklam yok");
                Assert.IsFalse(_gc.HintRingVisible);
            }
        }
    }
}
