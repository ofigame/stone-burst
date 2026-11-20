using System.Collections.Generic;
using KatKat.Core;
using KatKat.Meta;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Mağaza, külçe ve güçlendiriciler (2026-11-20; kullanıcı kararları: külçe koleksiyon taşı hediyesiyle ve uygulama içi
    /// satın almayla gelir, tahta temizleme ödülü yok; güçlendiriciler "+3 Hamle", "Taş Değiştir" (1 hak = seçilen 3 taşın
    /// üst rengi), "Bomba" (3×3); reklamsız paket külçe hediyeli; Taşlar sekmesi yerine Mağaza). Gerçek para satın alma
    /// kapalı (<see cref="ShopCatalog.IapEnabled"/>). Güçlendirici kullanılan oturum sıralama yıldızına ve ZPE'ye yazılmaz.
    /// </summary>
    public sealed partial class GameController
    {
        private MetaSave Meta => _save.Data.meta;

        /// <summary>Hedef seçimi bekleyen güçlendirici (Bomba, Taş Değiştir); null yok.</summary>
        private Booster? _boosterMode;
        private readonly List<Coord> _recolorPicks = new List<Coord>();

        public int Ingots => Meta.ingots;
        public bool BoosterModeActive => _boosterMode != null;

        private void SetupShop()
        {
            _menu.IngotCount = () => Meta.ingots;
            _menu.ShopPressed = ShowShop;
            _menu.GiftPending = () => Wallet.HasPendingGift(Meta, _stones);
            _menu.ClaimGiftPressed = ClaimStoneGift;
            _menu.BuyBoosterPressed = BuyBoosterFromShop;
            _menu.BuyProductPressed = id => _menu.ShowToast(Loc.T("shop.soon.note"));
            _hud.BoosterPressed += OnBoosterPressed;
            if (TestUnlockAll)
            {
                // Deneme APK'sı (kullanıcı 2026-11-20: "tüm bölümler açık olsun ve hamlelerde açık olsun deneyeyim"):
                // güçlendiriciler her bölümde açık ve dolu, külçe bol. Yalnız KATKAT_UNLOCK_ALL derlemesi.
                foreach (var b in ShopCatalog.All) if (Wallet.Count(Meta, b) < 20) Wallet.Add(Meta, b, 20 - Wallet.Count(Meta, b));
                if (Meta.ingots < 2000) Meta.ingots = 2000;
                _save.Save();
            }
        }

        // ---- Mağaza sayfası --------------------------------------------------------------------------------------------

        public void ShowShop()
        {
            _hud.HidePanel();
            SetPhase(Phase.Loading);
            _menu.ShowShop(ShopModel(), ShowLevels);
            _music.Enter();
            GameAnalytics.Log("shop_open", ("ingots", Meta.ingots));
        }

        private MenuView.ShopModel ShopModel() => new MenuView.ShopModel
        {
            Ingots = Meta.ingots,
            LevelNumber = TestUnlockAll ? 99999 : ContinueIndex + 1,
            Owned = new[] { Meta.boostMoves, Meta.boostRecolor, Meta.boostBomb },
            NoAds = Meta.noAds,
            StarterBought = Meta.starterBought,
        };

        private void BuyBoosterFromShop(Booster b, int qty)
        {
            if (!TryBuy(b, qty)) { _menu.ShowToast(Loc.T("shop.noingots")); return; }
            _menu.ShowToast(Loc.F("shop.bought", Loc.T("booster." + ShopCatalog.Key(b)), qty));
            ShowShopKeepScroll();
        }

        /// <summary>Satın alımdan sonra sayfa yeniden kurulur (adetler ve külçe).</summary>
        private void ShowShopKeepScroll() => _menu.ShowShop(ShopModel(), ShowLevels);

        private bool TryBuy(Booster b, int qty)
        {
            if (!Wallet.Buy(Meta, b, qty)) return false;
            _save.Save();
            _sound.Play(Sfx.CoinFinal);
            Haptics.Light();
            GameAnalytics.Log("booster_buy", ("booster", ShopCatalog.Key(b)), ("qty", qty), ("ingots_left", Meta.ingots));
            _menu.RefreshIngots();
            return true;
        }

        // ---- Koleksiyon taşı hediyesi ----------------------------------------------------------------------------------

        private void ClaimStoneGift(string stoneId)
        {
            int got = Wallet.ClaimGift(Meta, _stones, stoneId);
            if (got <= 0) return;
            _save.Save();
            _sound.Play(Sfx.CoinFinal);
            Haptics.Medium();
            GameAnalytics.Log("stone_gift", ("stone", stoneId), ("ingots", got));
            float y = _menu.CollectionScrollY;
            ShowCollection();
            _menu.ScrollCollectionForTest(y);
            _menu.ShowToast(Loc.F("collection.gift.got", got));
        }

        // ---- Oyun içi güçlendiriciler ----------------------------------------------------------------------------------

        private int LevelNumberNow => _levelIndex + 1;

        /// <summary>Normal bölümde ve en az bir güçlendirici açıldıysa şerit görünür.</summary>
        private bool BoostersVisible => _sessionKind == SessionKind.Normal && BoosterOpen(Booster.Moves);

        /// <summary>Açık mı (deneme APK'sında hep açık).</summary>
        private bool BoosterOpen(Booster b) => TestUnlockAll || Wallet.Unlocked(b, LevelNumberNow);

        /// <summary>Bölüm başı: yeni açılan güçlendiricinin deneme adedi verilir, şerit kurulur.</summary>
        private void BoostersAtLevelStart()
        {
            _boosterMode = null;
            _recolorPicks.Clear();
            _hud.CloseBoosterOffer();
            if (_sessionKind == SessionKind.Normal)
            {
                var got = Wallet.GrantTrials(Meta, LevelNumberNow);
                if (got.Count > 0)
                {
                    _save.Save();
                    _hud.ShowToast(Loc.F("booster.trial", Loc.T("booster." + ShopCatalog.Key(got[got.Count - 1])), ShopCatalog.TrialCount), 3f);
                    foreach (var b in got) GameAnalytics.Log("booster_unlock", ("booster", ShopCatalog.Key(b)), ("level_number", LevelNumberNow));
                }
            }
            RefreshBoosters();
        }

        private void RefreshBoosters()
        {
            var unlocked = new bool[3];
            for (int i = 0; i < 3; i++) unlocked[i] = BoosterOpen(ShopCatalog.All[i]);
            _hud.SetBoosters(BoostersVisible, new[] { Meta.boostMoves, Meta.boostRecolor, Meta.boostBomb }, unlocked, _boosterMode.HasValue ? (int)_boosterMode.Value : -1);
        }

        private void OnBoosterPressed(Booster b)
        {
            if (CurrentPhase != Phase.Idle || _session.State.Status != GameStatus.Idle) return;
            if (!BoosterOpen(b))
            {
                _hud.ShowToast(Loc.F("shop.unlock", ShopCatalog.UnlockLevel(b)));
                return;
            }
            // Aynı güçlendiriciye ikinci dokunuş: seçim iptal (hak harcanmaz).
            if (_boosterMode == b) { CancelBoosterMode(); return; }
            CancelBoosterMode();
            if (Wallet.Count(Meta, b) <= 0)
            {
                _hud.ShowBoosterOffer(b, Meta.ingots, () => { if (TryBuy(b, 1)) { RefreshBoosters(); OnBoosterPressed(b); } else _hud.ShowToast(Loc.T("shop.noingots")); },
                    () => ShowShop());
                return;
            }
            // Diğer seçim kipleri kapanır (boya, tepsi).
            if (_paintMode) TogglePaint();
            if (_traySlot >= 0) { _traySlot = -1; _hud.SetTray(_session.State.Tray, -1); }
            switch (b)
            {
                case Booster.Moves: UseMovesBooster(); break;
                case Booster.Bomb:
                    _boosterMode = b;
                    _hud.PlayBoosterBurst(b, false);
                    _hud.ShowToast(Loc.T("booster.bomb.pick"), 2.4f);
                    break;
                case Booster.Recolor:
                    _boosterMode = b;
                    _hud.PlayBoosterBurst(b, false);
                    _recolorPicks.Clear();
                    _hud.ShowToast(Loc.F("booster.recolor.pick", ShopCatalog.RecolorStones), 2.6f);
                    break;
            }
            RefreshBoosters();
        }

        private void CancelBoosterMode()
        {
            if (_boosterMode == null) return;
            _boosterMode = null;
            _recolorPicks.Clear();
            _hud.HidePalette();
            _hud.HideToast();
            RefreshBoosters();
        }

        private void BoosterUsed(Booster b)
        {
            Wallet.Consume(Meta, b);
            _save.Save();
            OutcomeAssist(AssistFlags.Booster);
            GameAnalytics.Log("booster_use", ("booster", ShopCatalog.Key(b)), ("level_number", LevelNumberNow), ("left", Wallet.Count(Meta, b)));
            _boosterMode = null;
            _recolorPicks.Clear();
            RefreshBoosters();
        }

        private void UseMovesBooster()
        {
            if (!_session.GrantMoves(ShopCatalog.MovesAmount)) return;
            BoosterUsed(Booster.Moves);
            if (_outcome != null) _outcome.MoveBonus += ShopCatalog.MovesAmount;
            // 2026-11-20 (kullanıcı: güçlendirici kullanımı "daha görsel şölenli"): "+3" yıldız iziyle hamle sayacına uçar, varınca
            // sayı güncellenir ve zıplar; tahtanın çevresinde altın ışık dalgası.
            _hud.PlayMovesGain(ShopCatalog.MovesAmount, () => { if (_session != null) _hud.SetMoves(_session.State.MovesLeft); });
            _board.RimSweep(new Color(1f, 0.85f, 0.35f), new Color(1f, 0.98f, 0.75f), 1.1f);
            _hud.ShowToast(Loc.F("booster.moves.done", ShopCatalog.MovesAmount), 1.6f);
            _sound.Play(Sfx.CoinFinal);
            Haptics.Medium();
            RefreshHint();
        }

        /// <summary>Tahta dokunuşu güçlendirici kipindeyse işlenir (true: dokunuş tüketildi).</summary>
        private bool BoosterCellPressed(int x, int y)
        {
            if (_boosterMode == null) return false;
            if (_boosterMode == Booster.Bomb) { UseBombAt(x, y); return true; }
            PickRecolorCell(x, y);
            return true;
        }

        private void UseBombAt(int x, int y)
        {
            var r = _session.Submit(Command.Bomb(x, y));
            if (!r.Accepted)
            {
                _sound.Play(Sfx.Invalid);
                _board.ShakeCells(new[] { new Coord(x, y) });
                _hud.ShowToast(Loc.T("booster.bomb.invalid"));
                return;
            }
            _session.DropUndo(); // külçeyle ödenen etki geri alınmaz
            BoosterUsed(Booster.Bomb);
            _hud.HideToast();
            _hud.PlayBoosterBurst(Booster.Bomb, true);
            _board.RimSweep(new Color(1f, 0.55f, 0.15f), new Color(1f, 0.9f, 0.45f), 1.5f, fire: true);
            HintAfterCommand();
            _sound.Play(Sfx.Win);
            Haptics.Strong();
            Play(r);
        }

        private void PickRecolorCell(int x, int y)
        {
            var b = _session.State.Board;
            var c = new Coord(x, y);
            if (!b.InBounds(x, y) || !GameSession.CanRecolor(b[x, y]))
            {
                _sound.Play(Sfx.Invalid);
                _hud.ShowToast(Loc.T("booster.recolor.invalid"));
                return;
            }
            if (_recolorPicks.Contains(c)) _recolorPicks.Remove(c);
            else if (_recolorPicks.Count < ShopCatalog.RecolorStones) _recolorPicks.Add(c);
            else { _hud.ShowToast(Loc.F("booster.recolor.max", ShopCatalog.RecolorStones)); return; }
            _sound.Play(Sfx.Tap);
            Haptics.Light();
            _board.ShakeCells(_recolorPicks);
            if (_recolorPicks.Count == 0) { _hud.HidePalette(); return; }
            // Palet: tahtadaki renkler (seçili taşların hepsiyle aynı olan renk de seçilebilir; aynı renktekiler atlanır).
            var options = new List<LayerColor>();
            for (int i = 0; i < LayerColors.Count; i++)
            {
                var col = (LayerColor)i;
                if (b.CountLayers(col) > 0) options.Add(col);
            }
            _hud.ShowPalette(b[_recolorPicks[0].X, _recolorPicks[0].Y].Top, options);
            _hud.ShowToast(Loc.F("booster.recolor.color", _recolorPicks.Count, ShopCatalog.RecolorStones), 2.4f);
        }

        /// <summary>Palet seçimi Taş Değiştir kipindeyse işlenir (true: tüketildi).</summary>
        private bool BoosterColorChosen(LayerColor color)
        {
            if (_boosterMode != Booster.Recolor) return false;
            if (_recolorPicks.Count == 0 || CurrentPhase != Phase.Idle) return true;
            var done = _session.RecolorTops(_recolorPicks, color);
            _hud.HidePalette();
            if (done.Count == 0) { _hud.ShowToast(Loc.T("toast.paint.same")); return true; }
            BoosterUsed(Booster.Recolor);
            _hud.HideToast();
            _hud.PlayBoosterBurst(Booster.Recolor, true);
            _board.PlayRecolor(done, color);
            _board.RimSweep(KatKat.View.LayerPalette.FrontColor(color), Color.white, 1.2f);
            _sound.Play(Sfx.CoinFinal);
            Haptics.Medium();
            HintAfterCommand();
            RefreshHint();
            return true;
        }

        // ---- Testler ---------------------------------------------------------------------------------------------------

        public void AddIngotsForTest(int n) { Meta.ingots += n; _save.Save(); }
        public void PressBoosterForTest(Booster b) => OnBoosterPressed(b);
        public void ChooseRecolorForTest(LayerColor c) => BoosterColorChosen(c);
    }
}
