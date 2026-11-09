using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.Meta;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// v12 reklam yerleşimleri (kullanıcı, 2026-11-08; docs/v12/PLAN.md §1):
    /// - Günlük giriş (ödüllü): 5 sn "Reklam aranıyor"; bulunamazsa reklamsız giriş (DailyService).
    /// - Kayıp kartı (ödüllü): hamle bitince "Reklam izle, +3 hamle", bölüm başına 1 kez. Bu oturum ZPE'ye girmez,
    ///   kazanılırsa yıldızlar sıralama yıldızına yazılmaz.
    /// - Zafer kartında Devam (geçiş): <see cref="InterstitialPolicy"/> kurallarıyla.
    /// Gerçek SDK (AdMob arabulucu + Unity Ads) gelene kadar oyunda <see cref="TestAdScreen"/>; editör dışı
    /// toplu çalıştırmada (testler) ve editör yakalamasında sahte adaptör.
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>null: oynatmada test reklam ekranı, toplu çalıştırma/editörde sahte. true/false: zorla.</summary>
        public static bool? AdScreenOverride;
        /// <summary>Sahte geçiş reklamı (testler): gösterilen sayısı.</summary>
        public static int FakeInterstitialShown;

        /// <summary>Gerçek reklam ağı (AdMob; <c>Scripts/Ads/AdMobNetwork.cs</c>, KATKAT_ADMOB ile). Cihazda bağlanır;
        /// null ya da hazır değilse test ekranı / sahte adaptör kullanılır.</summary>
        public static IAdNetwork AdNetwork;
        private TestAdScreen _adScreen;
        private AdConfig _adConfig = new AdConfig();
        private InterstitialPolicy _interstitial;
        private AdRouter _ads;
        private int _extraMovesUsed;

        public TestAdScreen AdScreen => _adScreen;
        public InterstitialPolicy Interstitial => _interstitial;
        public AdConfig AdSettings => _adConfig;

        private bool UseAdScreen => _adScreen != null && (AdScreenOverride ?? (Application.isPlaying && !Application.isBatchMode));

        /// <summary>Yerleşimler tek yönlendiriciden geçer (gerçek SDK geldiğinde yalnız burası değişir).</summary>
        private sealed class AdRouter : IRewardedAds, IInterstitialAds
        {
            private readonly GameController _gc;
            public AdRouter(GameController gc) { _gc = gc; }

            public void Show(string placement, Action<AdOutcome, string> done)
            {
                Action<AdOutcome, string> wrapped = (o, t) =>
                {
                    if (o == AdOutcome.Rewarded) _gc._interstitial?.RewardedShown(Time.realtimeSinceStartup);
                    done?.Invoke(o, t);
                };
                if (AdNetwork != null) AdNetwork.Show(placement, wrapped);
                else if (_gc.UseAdScreen) _gc._adScreen.Show(placement, wrapped);
                else FakeAds.Show(placement, wrapped);
            }

            public void ShowInterstitial(string placement, Action<bool> done)
            {
                if (AdNetwork != null) { AdNetwork.ShowInterstitial(placement, done); return; }
                if (_gc.UseAdScreen) { _gc._adScreen.ShowInterstitial(placement, done); return; }
                FakeInterstitialShown++;
                done?.Invoke(true);
            }
        }

        private AdRouter Ads => _ads ??= new AdRouter(this);

        private void SetupAds(Camera cam)
        {
            try { _adConfig = AdConfig.Parse(Resources.Load<TextAsset>("Ads/ad_config")?.text); }
            catch (FormatException e) { Debug.LogError("[KatKat] " + e.Message); _adConfig = new AdConfig(); }
            _interstitial = new InterstitialPolicy(_adConfig);
            _adScreen = TestAdScreen.Create(cam, transform);
            _adScreen.Config = _adConfig;
        }

        // ---- Kayıp kartında +3 hamle -----------------------------------------------------------------------

        /// <summary>Teklif: normal bölüm, hamle bittiği için kayıp, bu bölümde henüz kullanılmadı.</summary>
        private bool CanOfferExtraMoves(GameStatus status) =>
            _sessionKind == SessionKind.Normal && status == GameStatus.LostOutOfMoves
            && _extraMovesUsed < _adConfig.ExtraMovesPerLevel && _adConfig.ExtraMoves > 0;

        public bool ExtraMovesOffered { get; private set; }

        private void OnExtraMovesPressed()
        {
            if (CurrentPhase != Phase.Ended || _session == null || _session.State.Status != GameStatus.LostOutOfMoves) return;
            Ads.Show(AdPlacements.ExtraMoves, (o, token) =>
            {
                if (o == AdOutcome.Rewarded && _session != null && _session.State.Status == GameStatus.LostOutOfMoves)
                {
                    GrantExtraMoves(_adConfig.ExtraMoves);
                    return;
                }
                if (o == AdOutcome.Failed) _hud.ShowToast(Loc.T("ad.nofill"), 2.4f);
            });
        }

        /// <summary>Reklam ödülü: oturum sürer, hamle eklenir; ZPE kaydı geri çekilir ve oturum yardımlı işaretlenir.</summary>
        private void GrantExtraMoves(int n)
        {
            _extraMovesUsed++;
            _session.GrantMoves(n);
            OutcomeAssist(AssistFlags.RewardedAd);
            if (_outcome != null) _outcome.MoveBonus += n;
            OutcomeReopen();
            _hud.HidePanel();
            ResyncAll();
            SetPhase(Phase.Idle);
            BeginPlaceOffer();
        }

        // ---- Reklam ödülü: bir taş yerleştirme (2026-11-09) ------------------------------------------------

        private List<LayerColor> _placeColors;
        private int _placeSel = -1;

        public bool PlaceOfferActive => _placeColors != null;
        public IReadOnlyList<LayerColor> PlaceColors => _placeColors;

        /// <summary>Dört renk: önce hedef renkleri, sonra tahtada en çok görünen üst renkler (bölümde olan renkler).</summary>
        private void BeginPlaceOffer()
        {
            var s = _session.State;
            var count = new Dictionary<LayerColor, int>();
            for (int i = 0; i < s.Board.Count; i++)
            {
                var c = s.Board[i];
                if (!c.Active) continue;
                foreach (var l in c.Layers) count[l] = (count.TryGetValue(l, out var v) ? v : 0) + 1;
            }
            var colors = new List<LayerColor>();
            foreach (var g in s.Goals)
                if (g.Type == GoalType.CollectColor && !colors.Contains(g.Color)) colors.Add(g.Color);
            var rest = new List<LayerColor>(count.Keys);
            rest.Sort((a, b) => count[b].CompareTo(count[a]));
            foreach (var c in rest) if (!colors.Contains(c)) colors.Add(c);
            foreach (LayerColor c in Enum.GetValues(typeof(LayerColor))) if (colors.Count < 4 && !colors.Contains(c)) colors.Add(c);
            if (colors.Count > 4) colors.RemoveRange(4, colors.Count - 4);
            _placeColors = colors;
            _placeSel = -1;
            _hud.ShowPlaceRow(_placeColors, _placeSel);
        }

        private void OnPlaceStoneChosen(int i)
        {
            if (_placeColors == null || CurrentPhase != Phase.Idle) return;
            _placeSel = _placeSel == i ? -1 : i;
            if (_traySlot >= 0) { _traySlot = -1; _hud.SetTray(_session.State.Tray, -1); }
            _sound.Play(Sfx.Tap);
            _hud.SetPlaceSelected(_placeSel);
        }

        /// <summary>Seçili taş varken tahta dokunuşu: o yığının üstüne koyar. true: dokunuş tüketildi.</summary>
        private bool TryPlaceAt(int x, int y)
        {
            if (_placeColors == null || _placeSel < 0) return false;
            var b = _session.State.Board;
            bool fits = b.InBounds(x, y) && b[x, y].Layers.Count < Mathf.Max(1, _board.MaxLayers);
            if (!fits || !_session.PlaceLayer(x, y, _placeColors[_placeSel]))
            {
                _sound.Play(Sfx.Invalid);
                Haptics.Light();
                _hud.FlashPlaceLabel(Loc.T("place.bad"));
                return true;
            }
            EndPlaceOffer();
            _board.Sync(_session.State);
            _sound.Play(Sfx.Tap);
            Haptics.Medium();
            SetPhase(Phase.Idle);
            return true;
        }

        private void EndPlaceOffer()
        {
            _placeColors = null;
            _placeSel = -1;
            _hud.HidePlaceRow();
        }

        /// <summary>Test: yerleştirme (renk sırası, hücre).</summary>
        public bool PlaceForTest(int colorIndex, int x, int y)
        {
            OnPlaceStoneChosen(colorIndex);
            return TryPlaceAt(x, y) && _placeColors == null;
        }

        /// <summary>Test: reklam ödülünü doğrudan uygula (kayıp kartında).</summary>
        public void PressExtraMovesForTest() => OnExtraMovesPressed();

        // ---- Zafer kartında Devam: geçiş reklamı -------------------------------------------------------------

        private void AfterWinContinue(Action next)
        {
            int number = _levelIndex + 1;
            double now = Time.realtimeSinceStartup;
            if (_sessionKind == SessionKind.Normal && _interstitial != null && _interstitial.ShouldShow(number, now))
            {
                Ads.ShowInterstitial(AdPlacements.LevelComplete, shown =>
                {
                    if (shown) _interstitial.Shown(Time.realtimeSinceStartup);
                    next();
                });
                return;
            }
            next();
        }
    }
}
