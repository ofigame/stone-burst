#if KATKAT_APPGALLERY && UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading;
using KatKat.Meta;
using UnityEngine;

namespace KatKat.Game.Ads
{
    /// <summary>
    /// AppGallery derlemesinin reklam ağı (Google servisleri yok):
    /// - Önce Huawei Ads (ads-lite; Java köprüsü Plugins/Android/HwAdsBridge.java), kişiselleştirilmemiş reklam.
    ///   2026-11-12: Huawei Ads kapalı (KATKAT_HWADS yok, banka hesabı bekleniyor) → yalnız Unity Ads.
    /// - Huawei'de hazır reklam yoksa doğrudan Unity Ads (Plugins/Android/UnityAdsBridge.java; kullanıcı, 2026-11-12:
    ///   "app gallery içinde unity ads aktif olsun"), GDPR/CCPA rızası "hayır" → kişiselleştirilmemiş.
    /// - İkisinde de yoksa ödüllü <see cref="AdOutcome.Failed"/> (oyun "reklam bulunamadı" der, kart açık kalır), geçiş atlanır.
    /// Kimlikler <see cref="AdUnits"/>. YALNIZ KATKAT_APPGALLERY ile cihazda derlenir. HUAWEI CİHAZDA DENENMEDİ.
    /// </summary>
    public sealed class HuaweiAdsNetwork : IAdNetwork
    {
        private AndroidJavaObject _hw, _unity;
        private SynchronizationContext _main;
        private bool _hwReward, _hwInter, _uReward, _uInter, _granted;
        private Action<AdOutcome, string> _pendingReward;
        private Action<bool> _pendingInter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var net = new HuaweiAdsNetwork { _main = SynchronizationContext.Current };
            AndroidJavaObject activity = null;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            }
            catch (Exception e) { Debug.LogWarning("[KatKat] AppGallery reklam: etkinlik alınamadı: " + e.Message); return; }
#if KATKAT_HWADS
            try
            {
                net._hw = new AndroidJavaObject("com.ofigame.stoneburst.hw.HwAdsBridge", activity, new Listener(net, "com.ofigame.stoneburst.hw.HwAdsBridge$Listener"),
                    AdUnits.Rewarded, AdUnits.Interstitial);
                net._hw.Call("init");
            }
            catch (Exception e) { net._hw = null; Debug.LogWarning("[KatKat] Huawei Ads başlatılamadı: " + e.Message); }
#else
            // Huawei Ads kapalı (kullanıcı, 2026-11-12: banka hesabı henüz yok). Açmak: AppGalleryBuild.HuaweiAdsEnabled = true.
            net._hw = null;
#endif
            try
            {
                net._unity = new AndroidJavaObject("com.ofigame.stoneburst.hw.UnityAdsBridge", activity, new Listener(net, "com.ofigame.stoneburst.hw.UnityAdsBridge$Listener"),
                    AdUnits.UnityGameId, AdUnits.UnityRewarded, AdUnits.UnityInterstitial);
                net._unity.Call("init");
            }
            catch (Exception e) { net._unity = null; Debug.LogWarning("[KatKat] Unity Ads başlatılamadı: " + e.Message); }
            if (net._hw != null || net._unity != null) GameController.AdNetwork = net;
        }

        private sealed class Listener : AndroidJavaProxy
        {
            private readonly HuaweiAdsNetwork _net;
            public Listener(HuaweiAdsNetwork net, string iface) : base(iface) { _net = net; }
            // Java UI iş parçacığından gelir: Unity ana iş parçacığına aktarılır.
            public void onEvent(string kind, int code)
            {
                if (_net._main != null) _net._main.Post(_ => _net.OnEvent(kind, code), null);
                else _net.OnEvent(kind, code);
            }
        }

        /// <summary>Gösterimde olan ağ (sonuç ona göre beklenir).</summary>
        private AndroidJavaObject _showing;

        private void OnEvent(string kind, int code)
        {
            switch (kind)
            {
                // Huawei
                case "reward_loaded": _hwReward = true; break;
                case "reward_load_failed": _hwReward = false; break;
                case "rewarded": _granted = true; break;
                case "reward_closed": _hwReward = false; FinishReward(_granted ? AdOutcome.Rewarded : AdOutcome.Cancelled); _hw?.Call("loadRewarded"); break;
                case "reward_show_failed": _hwReward = false; FinishReward(AdOutcome.Failed); _hw?.Call("loadRewarded"); break;
                case "inter_loaded": _hwInter = true; break;
                case "inter_failed": _hwInter = false; if (_showing == _hw) FinishInter(false); break;
                case "inter_closed": _hwInter = false; FinishInter(true); _hw?.Call("loadInterstitial"); break;
                // Unity
                case "u_reward_loaded": _uReward = true; break;
                case "u_reward_load_failed": _uReward = false; break;
                case "u_rewarded": _granted = true; break;
                case "u_reward_closed": _uReward = false; FinishReward(_granted ? AdOutcome.Rewarded : AdOutcome.Cancelled); _unity?.Call("loadRewarded"); break;
                case "u_reward_show_failed": _uReward = false; FinishReward(AdOutcome.Failed); _unity?.Call("loadRewarded"); break;
                case "u_inter_loaded": _uInter = true; break;
                case "u_inter_failed": _uInter = false; if (_showing == _unity) FinishInter(false); break;
                case "u_inter_closed": _uInter = false; FinishInter(true); _unity?.Call("loadInterstitial"); break;
            }
            if (code != 0) Debug.Log($"[KatKat] AppGallery reklam {kind} kod {code}");
        }

        private void FinishReward(AdOutcome o)
        {
            var done = _pendingReward;
            if (done == null) return;
            _pendingReward = null;
            _granted = false;
            _showing = null;
            done(o, null);
        }

        private void FinishInter(bool shown)
        {
            var done = _pendingInter;
            if (done == null) return;
            _pendingInter = null;
            _showing = null;
            done(shown);
        }

        public void Show(string placement, Action<AdOutcome, string> done)
        {
            if (_pendingReward != null) { done?.Invoke(AdOutcome.Failed, null); return; }
            AndroidJavaObject net = _hwReward && _hw != null ? _hw : _uReward && _unity != null ? _unity : null;
            if (net == null)
            {
                _hw?.Call("loadRewarded");
                _unity?.Call("loadRewarded");
                done?.Invoke(AdOutcome.Failed, null);
                return;
            }
            _pendingReward = done;
            _granted = false;
            _showing = net;
            net.Call("showRewarded");
        }

        public void ShowInterstitial(string placement, Action<bool> done)
        {
            if (_pendingInter != null) { done?.Invoke(false); return; }
            AndroidJavaObject net = _hwInter && _hw != null ? _hw : _uInter && _unity != null ? _unity : null;
            if (net == null)
            {
                if (_hw != null && !_hwInter) _hw.Call("loadInterstitial");
                if (_unity != null && !_uInter) _unity.Call("loadInterstitial");
                done?.Invoke(false);
                return;
            }
            _pendingInter = done;
            _showing = net;
            net.Call("showInterstitial");
        }
    }
}
#endif
