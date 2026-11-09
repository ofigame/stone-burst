#if KATKAT_ADMOB && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using KatKat.Meta;
using UnityEngine;

namespace KatKat.Game.Ads
{
    /// <summary>
    /// Gerçek reklam ağı: Google AdMob (kullanıcı, 2026-11-09: "reklamsız çıkmayalım"; ağ kararı docs/v12/PLAN.md:
    /// AdMob arabulucu). Açılışta önce UMP rıza akışı (AB/İngiltere'de rıza formu, iOS'ta ATT açıklaması AdMob
    /// konsolundaki IDFA mesajıyla), rıza alınabiliyorsa SDK başlatılır; ödüllü ve geçiş reklamı önceden yüklenir.
    /// Reklam yoksa ödüllü <see cref="AdOutcome.Failed"/> (oyun "reklam bulunamadı" bildirimini gösterir), geçiş atlanır.
    /// Kimlikler <see cref="AdUnits"/>. YALNIZ KATKAT_ADMOB tanımlıyken cihazda derlenir. CİHAZDA DENENMEDİ.
    /// </summary>
    public sealed class AdMobNetwork : IAdNetwork
    {
        private RewardedAd _rewarded;
        private InterstitialAd _interstitial;
        private bool _started, _loadingRewarded, _loadingInterstitial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var net = new AdMobNetwork();
            GameController.AdNetwork = net;
            net.RequestConsentThenStart();
        }

        private void RequestConsentThenStart()
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            var req = new ConsentRequestParameters();
            ConsentInformation.Update(req, updateError =>
            {
                if (updateError != null) Debug.LogWarning("[KatKat] UMP güncellenemedi: " + updateError.Message);
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null) Debug.LogWarning("[KatKat] Rıza formu: " + formError.Message);
                    if (ConsentInformation.CanRequestAds()) Start();
                });
            });
            // Önceki oturumdan rıza varsa formu beklemeden başla.
            if (ConsentInformation.CanRequestAds()) Start();
        }

        private void Start()
        {
            if (_started) return;
            _started = true;
            MobileAds.Initialize(_ => { LoadRewarded(); LoadInterstitial(); });
        }

        /// <summary>Ayarlar → "Gizlilik seçenekleri" (AB'de zorunlu düğme). Gerekmiyorsa false.</summary>
        public static bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public static void ShowPrivacyOptions() => ConsentForm.ShowPrivacyOptionsForm(_ => { });

        private void LoadRewarded()
        {
            if (!_started || _loadingRewarded || (_rewarded != null && _rewarded.CanShowAd())) return;
            _loadingRewarded = true;
            RewardedAd.Load(AdUnits.Rewarded, new AdRequest(), (ad, error) =>
            {
                _loadingRewarded = false;
                if (error != null || ad == null) { Debug.LogWarning("[KatKat] Ödüllü reklam yüklenemedi: " + error?.GetMessage()); return; }
                _rewarded = ad;
            });
        }

        private void LoadInterstitial()
        {
            if (!_started || _loadingInterstitial || (_interstitial != null && _interstitial.CanShowAd())) return;
            _loadingInterstitial = true;
            InterstitialAd.Load(AdUnits.Interstitial, new AdRequest(), (ad, error) =>
            {
                _loadingInterstitial = false;
                if (error != null || ad == null) { Debug.LogWarning("[KatKat] Geçiş reklamı yüklenemedi: " + error?.GetMessage()); return; }
                _interstitial = ad;
            });
        }

        public void Show(string placement, Action<AdOutcome, string> done)
        {
            var ad = _rewarded;
            if (ad == null || !ad.CanShowAd()) { LoadRewarded(); done?.Invoke(AdOutcome.Failed, null); return; }
            _rewarded = null;
            bool earned = false, finished = false;
            string token = placement + "-" + Guid.NewGuid().ToString("N");
            void Finish(AdOutcome o)
            {
                if (finished) return;
                finished = true;
                ad.Destroy();
                LoadRewarded();
                done?.Invoke(o, token);
            }
            ad.OnAdFullScreenContentClosed += () => Finish(earned ? AdOutcome.Rewarded : AdOutcome.Cancelled);
            ad.OnAdFullScreenContentFailed += _ => Finish(AdOutcome.Failed);
            ad.Show(_ => earned = true);
        }

        public void ShowInterstitial(string placement, Action<bool> done)
        {
            var ad = _interstitial;
            if (ad == null || !ad.CanShowAd()) { LoadInterstitial(); done?.Invoke(false); return; }
            _interstitial = null;
            bool finished = false;
            void Finish(bool shown)
            {
                if (finished) return;
                finished = true;
                ad.Destroy();
                LoadInterstitial();
                done?.Invoke(shown);
            }
            ad.OnAdFullScreenContentClosed += () => Finish(true);
            ad.OnAdFullScreenContentFailed += _ => Finish(false);
            ad.Show();
        }
    }
}
#endif
