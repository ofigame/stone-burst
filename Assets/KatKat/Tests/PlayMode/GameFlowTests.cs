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

        /// <summary>2026-11-11 cihaz hatası: kayıp kartında reklam bulunamayınca kart kilitli kalıyordu (bütün düğmeler
        /// ölü). Gerçek kart düğmesinden basılır: reklam yok/iptal → kart açık, düğmeler yeniden basılabilir, yeniden reklam çalışır.</summary>
        [UnityTest]
        public IEnumerator LossCard_AdNoFill_CardStaysUsable()
        {
            _gc.StartLevel(3);
            _gc.OnCellPressed(0, 0);
            yield return WaitStable();
            var next = LevelSolver.Moves(_gc.Session.State);
            _gc.OnCellPressed(next[0].X, next[0].Y);
            yield return WaitStable();
            Assert.AreEqual(GameStatus.LostOutOfMoves, _gc.Session.State.Status);
            Assert.IsTrue(_gc.ExtraMovesOffered);
            yield return new WaitForSeconds(1f); // kart açılış animasyonu

            GameController.FakeAds.Next = KatKat.Meta.AdOutcome.Failed;
            _gc.Hud.Panel.Press(0);
            yield return null;
            Assert.AreEqual(GameStatus.LostOutOfMoves, _gc.Session.State.Status, "reklam yok: ödül yok");
            Assert.IsTrue(_gc.Hud.PanelOpen, "kayıp kartı açık kalır");
            Assert.IsTrue(_gc.Hud.PanelUnlocked, "kart düğmeleri yeniden basılabilir (kilit kalkar)");

            GameController.FakeAds.Next = KatKat.Meta.AdOutcome.Cancelled;
            _gc.Hud.Panel.Press(0);
            yield return null;
            Assert.IsTrue(_gc.Hud.PanelUnlocked, "iptalde de kilit kalkar");

            GameController.FakeAds.Next = KatKat.Meta.AdOutcome.Rewarded;
            _gc.Hud.Panel.Press(0);
            yield return null;
            Assert.AreEqual(GameStatus.Idle, _gc.Session.State.Status, "yeniden denenen reklam ödülü verdi");
            Assert.AreEqual(1, _gc.Session.State.MovesLeft);
        }

        [UnityTest]
        public IEnumerator V12_LossCard_AdExtraMoves_OncePerLevel_NotCountedInZpe()
        {
            _gc.StartLevel(3);
            int zpeGamesBefore = /* ZPE v3: kayıtlar geçmiş listesinde; zpeGames v2 toplamıyla dondu */ (_gc.SaveService.Data.meta.zpeHistory?.Count ?? 0);
            _gc.OnCellPressed(0, 0);
            yield return WaitStable();
            var next = LevelSolver.Moves(_gc.Session.State);
            _gc.OnCellPressed(next[0].X, next[0].Y);
            yield return WaitStable();
            Assert.AreEqual(GameStatus.LostOutOfMoves, _gc.Session.State.Status);
            Assert.IsTrue(_gc.ExtraMovesOffered, "kayıp kartında reklam teklifi (+1 hamle ve 1 taş)");
            Assert.AreEqual(zpeGamesBefore + 1, (_gc.SaveService.Data.meta.zpeHistory?.Count ?? 0), "kayıp ZPE'ye yazıldı");
            KatKat.Meta.FakeRewardedAds fake = GameController.FakeAds;
            fake.Next = KatKat.Meta.AdOutcome.Rewarded;
            _gc.PressExtraMovesForTest();
            Assert.AreEqual(GameStatus.Idle, _gc.Session.State.Status, "oturum sürüyor");
            Assert.AreEqual(1, _gc.Session.State.MovesLeft, "2026-11-09: +1 hamle");
            Assert.IsTrue(_gc.PlaceOfferActive, "dört taşlık yerleştirme şeridi açık");
            Assert.IsTrue(_gc.Hud.PlaceRowVisible);
            Assert.LessOrEqual(_gc.PlaceColors.Count, 4);
            Assert.Greater(_gc.PlaceColors.Count, 0);
            // Uygun bir yığına (etkin, örtüsüz, en yüksek yığından alçak) ilk taşı koy.
            var board = _gc.Session.State.Board;
            int px = -1, py = -1;
            for (int y = 0; y < board.Height && px < 0; y++)
            for (int x = 0; x < board.Width && px < 0; x++)
            {
                var c = board[x, y];
                if (c.Active && !c.HasCover && !c.HasSpecial && !c.Protected && c.Layers.Count < _gc.Board.MaxLayers) { px = x; py = y; }
            }
            Assert.GreaterOrEqual(px, 0, "yerleştirilecek yığın var");
            int layersBefore = board[px, py].Layers.Count;
            var color = _gc.PlaceColors[0];
            Assert.IsTrue(_gc.PlaceForTest(0, px, py), "taş kondu, şerit kapandı");
            Assert.AreEqual(layersBefore + 1, _gc.Session.State.Board[px, py].Layers.Count);
            Assert.AreEqual(color, _gc.Session.State.Board[px, py].Top);
            Assert.IsFalse(_gc.Hud.PlaceRowVisible, "diğer taşlar kayboldu");
            Assert.AreEqual(1, _gc.Session.State.MovesLeft, "yerleştirme hamle harcamaz");
            Assert.IsTrue(_gc.LastOutcome.Has(KatKat.Meta.AssistFlags.RewardedAd));
            Assert.AreEqual(zpeGamesBefore, (_gc.SaveService.Data.meta.zpeHistory?.Count ?? 0), "kayıp geri çekildi; reklamlı oturum ZPE'ye girmez");
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
            Assert.AreEqual(zpeGamesBefore, (_gc.SaveService.Data.meta.zpeHistory?.Count ?? 0), "reklamlı oturumun sonucu da ZPE'ye girmez");
        }

        [UnityTest]
        public IEnumerator Hearts_RestartAfterMoveCostsHeart_EmptyBlocksLevel_AdRefills()
        {
            // 2026-11-09 kalp sistemi: 21+ bölümde hamleden sonra yeniden başlatma 1 kalp; kalp yoksa bölüm açılmaz.
            const long now = 1_800_000_000;
            GameController.HeartClock = () => now;
            try
            {
                var meta = _gc.SaveService.Data.meta;
                meta.heartsMissing = 0; meta.heartsStamp = 0;
                _gc.StartLevel(25);
                _gc.Hud.HideRuleCard(false);
                Assert.AreEqual(5, _gc.HeartCount);
                _gc.Restart();
                Assert.AreEqual(5, _gc.HeartCount, "hamlesiz yeniden başlatma kalp götürmez");
                var m = LevelSolver.Moves(_gc.Session.State);
                Assert.IsNotEmpty(m);
                _gc.OnCellPressed(m[0].X, m[0].Y);
                yield return WaitStable();
                yield return WaitCelebration();
                Assert.IsTrue(_gc.HeartAtStake);
                _gc.Restart();
                Assert.AreEqual(4, _gc.HeartCount, "bırakılan deneme 1 kalp");
                Assert.IsFalse(_gc.HeartAtStake);
                // İlk 20 bölüm kalpsiz.
                _gc.StartLevel(5);
                var m5 = LevelSolver.Moves(_gc.Session.State);
                _gc.OnCellPressed(m5[0].X, m5[0].Y);
                yield return WaitStable();
                yield return WaitCelebration();
                _gc.ShowLevels();
                Assert.AreEqual(4, _gc.HeartCount, "ilk 20 bölüm kalp harcamaz");
                // Kalp yok: bölüm açılmaz, haritada kalp kartı.
                meta.heartsMissing = 5; meta.heartsStamp = now;
                _gc.StartLevel(25);
                Assert.IsTrue(_gc.Menu.IsOpen, "harita açık kaldı");
                Assert.IsTrue(_gc.Menu.IsHeartsCardShown, "kalp kartı açık");
                // 30 dakika sonra 1 kalp.
                GameController.HeartClock = () => now + KatKat.Meta.Hearts.RegenSeconds;
                Assert.AreEqual(1, _gc.HeartCount);
                // Reklamla dolum.
                GameController.FakeAds.Next = KatKat.Meta.AdOutcome.Rewarded;
                _gc.RefillHeartsForTest();
                Assert.AreEqual(5, _gc.HeartCount);
                _gc.Menu.CloseHeartsCard();
            }
            finally { GameController.HeartClock = null; }
        }

        [UnityTest]
        public IEnumerator Analytics_LevelEvents_And_ReviewOnlyAfterHappyWin()
        {
            // 2026-11-09: oyun olayları (Firebase Analytics) ve uygulama içi puan isteği kuralları.
            var sink = new RecordingAnalytics();
            GameAnalytics.Sink = sink;
            GameController.ReviewRequester = () => { };
            GameController.ReviewRequestsForTest = 0;
            const long now = 2_000_000_000L;
            GameController.ReviewClock = () => now;
            try
            {
                // Kayıp: level_end success=0.
                _gc.StartLevel(3);
                Assert.AreEqual(1, sink.Count("level_start"));
                Assert.AreEqual(4, sink.Last("level_start")["level_number"]);
                _gc.OnCellPressed(0, 0);
                yield return WaitStable();
                var next = LevelSolver.Moves(_gc.Session.State);
                _gc.OnCellPressed(next[0].X, next[0].Y);
                yield return WaitStable();
                Assert.AreEqual(GameStatus.LostOutOfMoves, _gc.Session.State.Status);
                Assert.AreEqual(0, sink.Last("level_end")["success"]);
                Assert.AreEqual(4, sink.Last("level_end")["level_number"]);

                // Kazanım: oyunu tanımış oyuncu (20 bölüm bitmiş), öğreticisiz bölüm 3.
                for (int i = 0; i < 20; i++) _gc.SaveService.MarkCompleted(_gc.Catalog.IdAt(i), i, _gc.Catalog.Count);
                _gc.StartLevel(2);
                _gc.Board.Director.TimeScale = Speed;
                foreach (var step in _gc.Catalog.Levels[2].Solution) { _gc.PlayStep(step); yield return WaitStable(); }
                yield return WaitCelebration();
                var end = sink.Last("level_end");
                Assert.AreEqual(1, end["success"]);
                Assert.AreEqual(3, end["level_number"]);
                Assert.AreEqual("normal", end["kind"]);
                bool happy = (int)end["stars"] >= 3 && (int)end["assists"] == 0;
                Assert.AreEqual(0, GameController.ReviewRequestsForTest, "zafer kartı açıkken istenmez");
                _gc.Hud.Panel.Skip();
                _gc.Hud.Panel.Press(0);
                yield return null;
                Assert.AreEqual(happy ? 1 : 0, GameController.ReviewRequestsForTest, "yalnız yardımsız 3 yıldızda");
                Assert.AreEqual(happy ? 1 : 0, sink.Count("review_prompt"));
                Assert.AreEqual(happy ? 1 : 0, _gc.SaveService.Data.meta.reviewAsks);
                Assert.IsTrue(sink.Properties.ContainsKey("highest_level"));
            }
            finally
            {
                GameAnalytics.Sink = null;
                GameController.ReviewRequester = null;
                GameController.ReviewClock = null;
            }
        }

        /// <summary>Bellekte bulut: giriş her zaman başarılı, kayıtlar sayılır.</summary>
        private sealed class FakeCloudAccount : IAccountService
        {
            public string Cloud;
            public int Saves;
            public AccountState State { get; private set; }
            public string PlayerName => "Test";
            public string PlayerId => "test";
            public bool IsTest => true;
            public void SignIn(bool silent, System.Action<bool> done) { State = AccountState.SignedIn; done?.Invoke(true); }
            public void SaveProgress(string json, System.Action<bool> done) { Cloud = json; Saves++; done?.Invoke(true); }
            public void LoadProgress(System.Action<bool, string> done) => done?.Invoke(true, Cloud);
        }

        [UnityTest]
        public IEnumerator Cloud_AheadRestoresOnMap_NotMidLevel_NeverOverwritesBeforeCheck()
        {
            // 2026-11-09: yeni kurulumda buluttaki ilerleme geri gelir; boş yerel kayıt buluttakini ezmez.
            var ahead = new KatKat.Save.SaveData { highestUnlocked = 30, sound = false };
            for (int i = 0; i < 30; i++) { ahead.completed.Add(_gc.Catalog.IdAt(i)); ahead.starIds.Add(_gc.Catalog.IdAt(i)); ahead.starCounts.Add(3); }
            var fake = new FakeCloudAccount { Cloud = JsonUtility.ToJson(ahead) };
            _gc.SetAccountForTest(fake);
            var local = _gc.SaveService.Data;
            Assert.AreEqual(0, local.completed.Count);
            Assert.IsTrue(local.sound);

            // Bölüm oynanırken gelen bulut kaydı bekler; o sırada buluta yazılmaz.
            _gc.StartLevel(0);
            yield return null;
            Assert.AreNotEqual(GameController.Phase.Loading, _gc.CurrentPhase);
            _gc.CloudSignInForTest(null);
            Assert.IsTrue(_gc.CloudChecked);
            Assert.IsTrue(_gc.CloudRestorePending, "bölüm sırasında geri yükleme bekler");
            Assert.AreEqual(0, _gc.SaveService.Data.completed.Count);
            _gc.PauseForTest();
            Assert.AreEqual(0, fake.Saves, "geride kalan yerel kayıt buluta yazılmadı");

            // Haritaya dönünce uygulanır; ses ayarı cihazda kalır.
            _gc.ShowLevels();
            Assert.IsFalse(_gc.CloudRestorePending);
            Assert.AreEqual(30, _gc.SaveService.Data.completed.Count);
            Assert.AreEqual(30, _gc.SaveService.Data.highestUnlocked);
            Assert.AreEqual(90, _gc.SaveService.Data.LevelStars);
            Assert.IsTrue(_gc.SaveService.Data.sound, "ses ayarı yerelden");
            Assert.AreEqual(0, fake.Saves);

            // Arka plana geçince güncel kayıt buluta gider.
            _gc.PauseForTest();
            Assert.AreEqual(1, fake.Saves);
            Assert.AreEqual(30, KatKat.Save.CloudSync.Parse(fake.Cloud).completed.Count);
        }

        [UnityTest]
        public IEnumerator Cloud_BehindOrEmpty_LocalKept_UploadedAfterCheck()
        {
            var fake = new FakeCloudAccount();
            _gc.SetAccountForTest(fake);
            _gc.SaveService.Data.completed.Add(_gc.Catalog.IdAt(0));
            _gc.SaveService.Data.highestUnlocked = 1;
            _gc.PauseForTest();
            Assert.AreEqual(0, fake.Saves, "giriş ve kontrol yokken yazılmaz");
            _gc.ShowLevels();
            yield return null;
            _gc.CloudSignInForTest(null);
            Assert.IsFalse(_gc.CloudRestorePending);
            Assert.AreEqual(1, _gc.SaveService.Data.completed.Count);
            Assert.AreEqual(1, fake.Saves, "kontrolden sonra yerel kayıt buluta gitti");
            Assert.AreEqual(1, KatKat.Save.CloudSync.Parse(fake.Cloud).completed.Count);
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

        /// <summary>Bir komutu oyuncu girdisi gibi uygular (tepsi: yuva → hücre; boya ve rulo: PlayStep).</summary>
        private void ApplyAsPlayer(Command c)
        {
            switch (c.Type)
            {
                case CommandType.Tap: _gc.OnCellPressed(c.X, c.Y); break;
                case CommandType.Rotate: _gc.OnRotorPressed(c.X, c.Y); break;
                case CommandType.UseSpecial: _gc.PressTraySlot(c.Slot); _gc.OnCellPressed(c.X, c.Y); break;
                default:
                    _gc.PlayStep(new StepData { Type = c.Type == CommandType.Paint ? "paint" : "roll", X = c.X, Y = c.Y, Row = c.Row,
                        Color = c.Type == CommandType.Paint ? ColorIds.ToId(c.Color) : null });
                    break;
            }
        }

        /// <summary>
        /// Kullanıcı (2026-11-09): "ipucunu aldığımda o ipucuna tekrar tekrar bakabilmem lazım". vr1 P0 (01_IPUCU): öneri
        /// doğrulanmış plandır; başka hamleden sonra aynı hakla ücretsiz yeniden çözülür, düğme reklamsız yeniden gösterir;
        /// önerilen komut uygulanınca hak biter.
        /// </summary>
        [UnityTest]
        public IEnumerator PaidHint_VerifiedPlan_StaysUntilApplied_OtherMoveReplansFree()
        {
            _gc.StartLevel(40);
            _gc.Board.Director.TimeScale = Speed;
            yield return WaitStable();
            var fake = GameController.FakeAds;
            fake.Next = KatKat.Meta.AdOutcome.Rewarded;
            int shown = fake.Shown;
            _gc.PressHintForTest();
            Assert.IsTrue(_gc.HintPreparing, "plan arka planda hazırlanır");
            Assert.AreEqual(shown, fake.Shown, "plan hazır olmadan reklam yok");
            _gc.CompleteHintJobForTest();
            Assert.IsTrue(_gc.HintConfirmShown, "bedel reklamdan önce onay kartında");
            Assert.AreEqual(shown, fake.Shown, "onaydan önce reklam yok");
            _gc.ConfirmHintAdForTest();
            Assert.AreEqual(shown + 1, fake.Shown, "reklam bir kez");
            Assert.Greater(_gc.Board.HintGroupMarks + (_gc.PaidHintCommand.Value.Type == CommandType.Tap ? 0 : 1), 0, "dokunuşta grup sınırı");
            Assert.IsTrue(_gc.PaidHintUsed, "öneri teslim edildi: bedel işlendi");
            Assert.IsTrue(_gc.PaidHintCommand.HasValue, "ipucu komutu");
            Assert.IsTrue(_gc.HintRingVisible || _gc.Hud.HintMarkTarget != KatKat.UI.HudView.HintUi.None, "öneri görünür");

            // Planın beklediği sonuca ulaşmayan başka bir dokunuş: plan düşer, aynı hakla ücretsiz yeniden çözülür.
            var expected = _gc.Session.State.Clone();
            RulesEngine.TryApply(expected, _gc.PaidHintCommand.Value);
            string expectedKey = LevelSolver.StateKey(expected);
            Command? other = null;
            foreach (var c in LevelSolver.Moves(_gc.Session.State))
            {
                if (c.Type != CommandType.Tap) continue;
                var probe = _gc.Session.State.Clone();
                if (RulesEngine.TryApply(probe, c).Accepted && !probe.IsTerminal && LevelSolver.StateKey(probe) != expectedKey) { other = c; break; }
            }
            Assert.IsTrue(other.HasValue, "ipucu dışında oynanabilir dokunuş");
            _gc.OnCellPressed(other.Value.X, other.Value.Y);
            yield return WaitStable();
            _gc.CompleteHintJobForTest(); _gc.ConfirmHintAdForTest();
            Assert.AreEqual(GameController.Phase.Idle, _gc.CurrentPhase);
            Assert.IsFalse(_gc.HintConsumed, "başka hamle hakkı bitirmez");
            Assert.AreEqual(shown + 1, fake.Shown, "yeniden çözüm reklamsız");
            if (!_gc.PaidHintCommand.HasValue)
            {
                Assert.IsTrue(_gc.HintCreditOpen, "plan bulunamazsa hak durur (tahmin gösterilmez)");
                yield break;
            }
            Assert.IsTrue(_gc.HintRingVisible || _gc.Hud.HintMarkTarget != KatKat.UI.HudView.HintUi.None, "yeni plan görünür");
            _gc.PressHintForTest();
            Assert.AreEqual(shown + 1, fake.Shown, "ikinci basışta reklam yok");

            // Önerilen komut uygulanınca hak biter.
            ApplyAsPlayer(_gc.PaidHintCommand.Value);
            yield return WaitStable();
            yield return WaitCelebration();
            Assert.IsTrue(_gc.HintConsumed, "önerilen komut uygulandı");
            Assert.IsFalse(_gc.PaidHintCommand.HasValue);
            Assert.IsTrue(_gc.PaidHintUsed);
            if (!_gc.Session.State.IsTerminal)
            {
                _gc.PressHintForTest();
                Assert.AreEqual(shown + 1, fake.Shown, "hak bitince yeni reklam yok");
                Assert.IsFalse(_gc.HintPreparing);
            }
        }

        /// <summary>H06: reklam iptal/başarısız → yıldız bedeli, hak ve yardım bayrağı değişmez.</summary>
        [UnityTest]
        public IEnumerator PaidHint_AdCancelledOrFailed_ChangesNothing()
        {
            _gc.StartLevel(40);
            _gc.Board.Director.TimeScale = Speed;
            yield return WaitStable();
            var fake = GameController.FakeAds;
            try
            {
                foreach (var o in new[] { KatKat.Meta.AdOutcome.Cancelled, KatKat.Meta.AdOutcome.Failed })
                {
                    fake.Next = o;
                    int shown = fake.Shown;
                    _gc.PressHintForTest();
                    _gc.CompleteHintJobForTest(); _gc.ConfirmHintAdForTest();
                    Assert.AreEqual(shown + 1, fake.Shown, o.ToString());
                    Assert.IsFalse(_gc.PaidHintUsed, o + ": bedel yok");
                    Assert.IsFalse(_gc.HintCreditOpen, o + ": hak yok");
                    Assert.IsFalse(_gc.PaidHintCommand.HasValue, o + ": öneri yok");
                }
            }
            finally { fake.Next = KatKat.Meta.AdOutcome.Rewarded; }
        }

        /// <summary>H07: yinelenen ödül geri çağrısı tek teslim üretir.</summary>
        [UnityTest]
        public IEnumerator PaidHint_DuplicateRewardCallback_SingleGrant()
        {
            _gc.StartLevel(40);
            _gc.Board.Director.TimeScale = Speed;
            yield return WaitStable();
            var fake = GameController.FakeAds;
            fake.Next = KatKat.Meta.AdOutcome.Rewarded;
            fake.DuplicateCallback = true;
            try
            {
                int grants = GameController.HintGrantsForTest;
                _gc.PressHintForTest();
                _gc.CompleteHintJobForTest(); _gc.ConfirmHintAdForTest();
                Assert.AreEqual(grants + 1, GameController.HintGrantsForTest, "tek teslim");
                Assert.IsTrue(_gc.PaidHintUsed);
            }
            finally { fake.DuplicateCallback = false; }
        }

        /// <summary>H04/H06: güvenilir plan bulunamazsa reklam açılmaz; hak, yıldız ve yardım değişmez.</summary>
        [UnityTest]
        public IEnumerator PaidHint_NoPlan_NoAd()
        {
            _gc.StartLevel(40);
            _gc.Board.Director.TimeScale = Speed;
            yield return WaitStable();
            // Kayıtlı çözümden sap (kayıtlı yol aday olarak doğrulanamasın): son kabul edilen dokunuş.
            Command? dev = null;
            foreach (var c in LevelSolver.Moves(_gc.Session.State))
            {
                if (c.Type != CommandType.Tap) continue;
                var probe = _gc.Session.State.Clone();
                if (RulesEngine.TryApply(probe, c).Accepted && !probe.IsTerminal) dev = c;
            }
            Assert.IsTrue(dev.HasValue);
            _gc.OnCellPressed(dev.Value.X, dev.Value.Y);
            yield return WaitStable();
            int nodes = GameController.HintNodeBudget;
            GameController.HintNodeBudget = 1;
            try
            {
                var fake = GameController.FakeAds;
                int shown = fake.Shown;
                _gc.PressHintForTest();
                _gc.CompleteHintJobForTest(); _gc.ConfirmHintAdForTest();
                if (_gc.PaidHintCommand.HasValue) Assert.Inconclusive("1 düğümle de doğrulanmış yol bulundu");
                Assert.AreEqual(shown, fake.Shown, "plan yoksa reklam yok");
                Assert.IsFalse(_gc.PaidHintUsed);
                Assert.IsFalse(_gc.HintCreditOpen);
            }
            finally { GameController.HintNodeBudget = nodes; }
        }

        /// <summary>Yeniden başlatma yeni denemedir: ipucu hakkı ve bedeli sıfırlanır (önceden bedel sonraki denemeye taşınıyordu).</summary>
        [UnityTest]
        public IEnumerator PaidHint_Restart_ResetsAttempt()
        {
            _gc.StartLevel(40);
            _gc.Board.Director.TimeScale = Speed;
            yield return WaitStable();
            GameController.FakeAds.Next = KatKat.Meta.AdOutcome.Rewarded;
            _gc.PressHintForTest();
            _gc.CompleteHintJobForTest(); _gc.ConfirmHintAdForTest();
            Assert.IsTrue(_gc.PaidHintUsed);
            _gc.Restart();
            yield return WaitStable();
            Assert.IsFalse(_gc.PaidHintUsed, "yeni deneme: bedel yok");
            Assert.IsFalse(_gc.HintCreditOpen);
            Assert.IsFalse(_gc.PaidHintCommand.HasValue);
        }

        /// <summary>
        /// Kullanıcı (2026-11-09): "geri al ve durdur butonları tıklandığında iç iconları yok oluyor". HUD düğme yüzü
        /// sonradan simgeli görselle değişir; basıp bırakınca görsel aynı kalmalı.
        /// </summary>
        [UnityTest]
        public IEnumerator HudRoundButtons_KeepIconSpriteAfterPress()
        {
            _gc.StartLevel(5);
            yield return null;
            foreach (var name in new[] { "Pause", "Undo" })
            {
                var t = FindDeep(_gc.Hud.transform, name);
                Assert.IsNotNull(t, name);
                var img = t.GetComponent<UnityEngine.UI.Image>();
                var before = img.sprite;
                var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                UnityEngine.EventSystems.ExecuteEvents.Execute(t.gameObject, ev, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
                yield return null;
                UnityEngine.EventSystems.ExecuteEvents.Execute(t.gameObject, ev, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
                yield return null;
                Assert.AreSame(before, img.sprite, name + ": bırakınca düğme yüzü değişmemeli");
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root) { var f = FindDeep(c, name); if (f != null) return f; }
            return null;
        }
    }
}
