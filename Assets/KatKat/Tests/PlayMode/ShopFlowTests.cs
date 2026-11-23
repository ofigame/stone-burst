using System.Collections;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.Game;
using KatKat.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    /// <summary>Mağaza, külçe, koleksiyon hediyesi ve oyun içi güçlendiriciler (2026-11-20).</summary>
    public class ShopFlowTests
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
        }

        private IEnumerator WaitStable()
        {
            float limit = Time.realtimeSinceStartup + 20f;
            while (_gc.CurrentPhase == GameController.Phase.Animating && Time.realtimeSinceStartup < limit) yield return null;
            Assert.AreNotEqual(GameController.Phase.Animating, _gc.CurrentPhase, "Animasyon bitmedi");
        }

        private MetaSave Meta => _gc.SaveService.Data.meta;

        private static UnityEngine.UI.Button PanelButton(string label)
        {
            foreach (var b in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (b.name == label && b.GetComponentInParent<KatKat.UI.ResultPanel>() != null) return b;
            return null;
        }

        private IEnumerator WaitPanel()
        {
            for (float until = Time.realtimeSinceStartup + 10f; !_gc.Hud.PanelOpen && Time.realtimeSinceStartup < until;) yield return null;
            Assert.IsTrue(_gc.Hud.PanelOpen, "kayıp kartı açıldı");
            yield return new WaitForSeconds(1f); // kart açılış animasyonu
        }

        private Coord BombTarget()
        {
            var b = _gc.Session.State.Board;
            for (int y = 1; y < b.Height - 1; y++)
            for (int x = 1; x < b.Width - 1; x++)
                if (b[x, y].IsSelectable) return new Coord(x, y);
            Assert.Fail("bomba hedefi yok");
            return default;
        }

        /// <summary>
        /// 2026-11-23 cihaz hatası: son hamlede bomba güçlendiricisiyle kaybedince kayıp kartında "Geri al"a basılınca ekran
        /// kilitleniyordu (bomba geri alınmaz; düğme hiçbir şey yapmadan kartı kilitliyordu). Kayıp kartındaki her düğme ya iş
        /// görür ya da kart basılabilir kalır; geri alınamayan durumda "Geri al" hiç gösterilmez.
        /// </summary>
        [UnityTest]
        public IEnumerator BombLoss_LostCard_NoDeadUndo_AllButtonsWork()
        {
            _gc.StartLevel(29);
            yield return WaitStable();
            _gc.Board.Director.TimeScale = 8f;
            // Hamle bitmiş ama tepside özel taş olduğu için oyun süren an (v6 kuralı): bomba son eylem olur.
            var st = _gc.Session.State;
            st.MovesLeft = 0;
            st.Tray.Clear();
            var t = BombTarget();
            _gc.PressBoosterForTest(Booster.Bomb);
            Assert.IsTrue(_gc.BoosterModeActive);
            _gc.OnCellPressed(t.X, t.Y);
            yield return WaitStable();
            Assert.AreEqual(GameStatus.LostOutOfMoves, _gc.Session.State.Status, "bomba sonrası kayıp");
            yield return WaitPanel();
            Assert.IsFalse(_gc.Session.CanUndo, "bomba geri alınmaz");
            Assert.IsNull(PanelButton(KatKat.UI.Loc.T("btn.undo")), "geri alınamazken kartta Geri al yok");

            // Doğrudan geri alma denemesi (eski düğme yolu) kartı kilitlemez.
            _gc.Undo();
            Assert.IsTrue(_gc.Hud.PanelOpen && _gc.Hud.PanelUnlocked, "geri alma olmadı: kart basılabilir");

            // Yeniden dene çalışır.
            var retry = PanelButton(KatKat.UI.Loc.T("btn.retry"));
            Assert.IsNotNull(retry, "Yeniden dene düğmesi");
            retry.onClick.Invoke();
            yield return null;
            Assert.IsFalse(_gc.Hud.PanelOpen, "kart kapandı");
            Assert.AreEqual(GameStatus.Idle, _gc.Session.State.Status);
            Assert.AreEqual(GameController.Phase.Idle, _gc.CurrentPhase, "oyun sürüyor");

            // Dokunuşla kaybedince Geri al var ve çalışır (tahta açılır).
            yield return WaitStable();
            st = _gc.Session.State;
            st.MovesLeft = 1;
            st.Tray.Clear();
            Coord tap = default; bool found = false;
            var b = st.Board;
            for (int y = 0; y < b.Height && !found; y++)
            for (int x = 0; x < b.Width && !found; x++)
                if (b[x, y].IsSelectable && GroupFinder.Component(b, x, y).Count >= GroupFinder.MinGroup) { tap = new Coord(x, y); found = true; }
            Assert.IsTrue(found, "geçerli dokunuş");
            _gc.OnCellPressed(tap.X, tap.Y);
            yield return WaitStable();
            if (_gc.Session.State.Status == GameStatus.Idle && _gc.Session.State.Tray.Count > 0) { _gc.Session.State.Tray.Clear(); Assert.Inconclusive("dokunuş tepsiye özel taş verdi"); }
            Assert.AreNotEqual(GameStatus.Idle, _gc.Session.State.Status, "son hamleyle kayıp");
            if (_gc.Session.State.Status == GameStatus.Won) Assert.Inconclusive("son hamle kazandırdı");
            yield return WaitPanel();
            var undo = PanelButton(KatKat.UI.Loc.T("btn.undo"));
            Assert.IsNotNull(undo, "geri alınabilirken Geri al var");
            undo.onClick.Invoke();
            yield return null;
            Assert.IsFalse(_gc.Hud.PanelOpen, "geri alındı, kart kapandı");
            Assert.AreEqual(GameStatus.Idle, _gc.Session.State.Status);
            Assert.AreEqual(1, _gc.Session.State.MovesLeft);
            Assert.AreEqual(GameController.Phase.Idle, _gc.CurrentPhase);
        }

        [UnityTest]
        public IEnumerator TopBarIngots_ShopTab_BuyBooster()
        {
            _gc.AddIngotsForTest(200);
            _gc.ShowLevels();
            yield return null;
            Assert.AreEqual(200, _gc.Menu.IngotShownForTest, "üstte külçe (yıldız yerine)");
            Assert.IsTrue(_gc.Menu.PressForTest(KatKat.UI.Loc.T("shop.tab")), "alt menüde Mağaza");
            yield return null;
            Assert.IsTrue(_gc.Menu.IsShopShown);
            _gc.Menu.BuyBoosterPressed(Booster.Bomb, 1);
            Assert.AreEqual(200 - ShopCatalog.Price(Booster.Bomb), Meta.ingots);
            Assert.AreEqual(1, Meta.boostBomb);
            Assert.AreEqual(Meta.ingots, _gc.Menu.IngotShownForTest, "sayaç güncellendi");
            _gc.Menu.BuyBoosterPressed(Booster.Bomb, 3);
            Assert.AreEqual(1, Meta.boostBomb, "külçe yetmez: değişmez");
            Assert.IsTrue(_gc.Menu.IsShopShown);
        }

        [UnityTest]
        public IEnumerator CollectionGift_MarkOnStoneGoal_ClaimOnce()
        {
            Meta.stones.Add("stone_tide");
            _gc.SaveService.Save();
            _gc.ShowLevels();
            yield return null;
            Assert.IsTrue(_gc.Menu.GiftMarkShown, "taş göstergesinde hediye işareti");
            _gc.ShowCollection();
            yield return null;
            int before = Meta.ingots;
            _gc.Menu.ClaimGiftPressed("stone_tide");
            Assert.AreEqual(before + 50, Meta.ingots);
            Assert.Contains("stone_tide", Meta.stones, "taş koleksiyonda kalır");
            _gc.Menu.ClaimGiftPressed("stone_tide");
            Assert.AreEqual(before + 50, Meta.ingots, "bir kez");
            _gc.ShowLevels();
            yield return null;
            Assert.IsFalse(_gc.Menu.GiftMarkShown, "işaret kalktı");
        }

        [UnityTest]
        public IEnumerator Boosters_HiddenEarly_TrialsAt30_MovesBombRecolor()
        {
            _gc.StartLevel(4);
            yield return WaitStable();
            Assert.IsFalse(_gc.Hud.BoostersShown, "5. bölümde güçlendirici yok");

            _gc.StartLevel(29);
            yield return WaitStable();
            Assert.IsTrue(_gc.Hud.BoostersShown);
            Assert.AreEqual(ShopCatalog.TrialCount, Meta.boostMoves, "deneme adedi");
            Assert.AreEqual(ShopCatalog.TrialCount, Meta.boostBomb);

            // +3 Hamle
            int moves = _gc.Session.State.MovesLeft;
            _gc.PressBoosterForTest(Booster.Moves);
            Assert.AreEqual(moves + ShopCatalog.MovesAmount, _gc.Session.State.MovesLeft);
            Assert.AreEqual(ShopCatalog.TrialCount - 1, Meta.boostMoves);

            // Bomba: hamle harcamaz, 3×3 soyar, görünüm durumla aynı.
            var b = _gc.Session.State.Board;
            Coord target = default;
            bool found = false;
            for (int y = 1; y < b.Height - 1 && !found; y++)
            for (int x = 1; x < b.Width - 1 && !found; x++)
                if (b[x, y].IsSelectable) { target = new Coord(x, y); found = true; }
            Assert.IsTrue(found);
            int layersBefore = b.TotalLayers();
            moves = _gc.Session.State.MovesLeft;
            _gc.PressBoosterForTest(Booster.Bomb);
            Assert.IsTrue(_gc.BoosterModeActive);
            _gc.OnCellPressed(target.X, target.Y);
            yield return WaitStable();
            Assert.IsFalse(_gc.BoosterModeActive);
            Assert.Less(_gc.Session.State.Board.TotalLayers(), layersBefore, "katlar soyuldu");
            Assert.AreEqual(moves, _gc.Session.State.MovesLeft, "hamle harcanmaz");
            Assert.AreEqual(ShopCatalog.TrialCount - 1, Meta.boostBomb);
            Assert.IsFalse(_gc.Session.CanUndo, "bomba geri alınmaz");

            // Taş Değiştir: 3 taş, tek renk.
            if (_gc.Session.State.Status == GameStatus.Idle)
            {
                b = _gc.Session.State.Board;
                // 2026-11-21 akışı: önce renk, sonra taşlara tek tek dokunulur (3 → 0); üçüncüde kip kendiliğinden kapanır.
                var color = LayerColor.Blue;
                var picks = new List<Coord>();
                for (int i = 0; i < b.Count && picks.Count < 3; i++)
                    if (GameSession.CanRecolor(b[i]) && b[i].Top != color) picks.Add(new Coord(b.X(i), b.Y(i)));
                Assert.AreEqual(3, picks.Count);
                _gc.PressBoosterForTest(Booster.Recolor);
                _gc.OnCellPressed(picks[0].X, picks[0].Y);
                Assert.AreNotEqual(color, b[picks[0].X, picks[0].Y].Top, "renk seçilmeden taş değişmez");
                Assert.AreEqual(ShopCatalog.TrialCount, Meta.boostRecolor, "renk seçilmeden hak düşmez");
                _gc.ChooseRecolorForTest(color);
                foreach (var p in picks) _gc.OnCellPressed(p.X, p.Y);
                Assert.IsFalse(_gc.BoosterModeActive, "üç taştan sonra kip kapanır");
                foreach (var p in picks)
                    if (b[p.X, p.Y].Layers.Count > 0) Assert.AreEqual(color, b[p.X, p.Y].Top, $"({p.X},{p.Y}) yeni renk");
                Assert.AreEqual(ShopCatalog.TrialCount - 1, Meta.boostRecolor);
                yield return new WaitForSeconds(0.6f);
            }

            // Bitince satın alma penceresi.
            _gc.PressBoosterForTest(Booster.Moves);
            Assert.AreEqual(0, Meta.boostMoves);
            _gc.PressBoosterForTest(Booster.Moves);
            Assert.IsTrue(_gc.Hud.BoosterOfferShown, "bitince 'Bomba bitti' benzeri pencere");
            _gc.AddIngotsForTest(100);
            int movesBefore = _gc.Session.State.MovesLeft;
            _gc.Hud.PressBoosterOfferForTest(true);
            Assert.AreEqual(movesBefore + ShopCatalog.MovesAmount, _gc.Session.State.MovesLeft, "alınıp hemen kullanıldı");
            Assert.AreEqual(100 - ShopCatalog.Price(Booster.Moves), Meta.ingots);
        }
    }
}
