#if KATKAT_APPGALLERY && UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading;
using KatKat.Meta;
using UnityEngine;

namespace KatKat.Game.Ads
{
    /// <summary>
    /// AppGallery derlemesinin reklam ağı: Huawei Ads (ads-lite; kullanıcı, 2026-11-11: "App Gallery için reklamları,
    /// Google bağlılıklarını gözden geçir"). Java köprüsü Plugins/Android/HwAdsBridge.java. Ödüllü ve geçiş reklamı
    /// önceden yüklenir; hazır değilse ödüllü <see cref="AdOutcome.Failed"/> (oyun "reklam bulunamadı" der ve kart
    /// açık kalır), geçiş atlanır. Kişiselleştirilmemiş reklam. Kimlikler <see cref="AdUnits"/> (şimdilik Huawei TEST).
    /// YALNIZ KATKAT_APPGALLERY ile cihazda derlenir. HUAWEI CİHAZDA DENENMEDİ.
    /// </summary>
    public sealed class HuaweiAdsNetwork : IAdNetwork
    {
        private AndroidJavaObject _bridge;
        private SynchronizationContext _main;
        private bool _rewardReady, _interReady, _rewardedGranted;
        private Action<AdOutcome, string> _pendingReward;
        private Action<bool> _pendingInter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var net = new HuaweiAdsNetwork();
            try
            {
                net._main = SynchronizationContext.Current;
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    net._bridge = new AndroidJavaObject("com.ofigame.stoneburst.hw.HwAdsBridge", activity, new Listener(net),
                        AdUnits.Rewarded, AdUnits.Interstitial);
                }
                net._bridge.Call("init");
                GameController.AdNetwork = net;
            }
            catch (Exception e) { Debug.LogWarning("[KatKat] Huawei Ads başlatılamadı: " + e.Message); }
        }

        private sealed class Listener : AndroidJavaProxy
        {
            private readonly HuaweiAdsNetwork _net;
            public Listener(HuaweiAdsNetwork net) : base("com.ofigame.stoneburst.hw.HwAdsBridge$Listener") { _net = net; }
            // Java UI iş parçacığından gelir: Unity ana iş parçacığına aktarılır.
            public void onEvent(string kind, int code)
            {
                if (_net._main != null) _net._main.Post(_ => _net.OnEvent(kind, code), null);
                else _net.OnEvent(kind, code);
            }
        }

        private void OnEvent(string kind, int code)
        {
            switch (kind)
            {
                case "reward_loaded": _rewardReady = true; break;
                case "reward_load_failed": _rewardReady = false; break;
                case "rewarded": _rewardedGranted = true; break;
                case "reward_closed":
                    _rewardReady = false;
                    FinishReward(_rewardedGranted ? AdOutcome.Rewarded : AdOutcome.Cancelled);
                    _bridge?.Call("loadRewarded");
                    break;
                case "reward_show_failed":
                    _rewardReady = false;
                    FinishReward(AdOutcome.Failed);
                    _bridge?.Call("loadRewarded");
                    break;
                case "inter_loaded": _interReady = true; break;
                case "inter_failed":
                    _interReady = false;
                    FinishInter(false);
                    break;
                case "inter_closed":
                    _interReady = false;
                    FinishInter(true);
                    _bridge?.Call("loadInterstitial");
                    break;
            }
            if (code != 0) Debug.Log($"[KatKat] Huawei Ads {kind} kod {code}");
        }

        private void FinishReward(AdOutcome o)
        {
            var done = _pendingReward;
            _pendingReward = null;
            _rewardedGranted = false;
            done?.Invoke(o, null);
        }

        private void FinishInter(bool shown)
        {
            var done = _pendingInter;
            _pendingInter = null;
            done?.Invoke(shown);
        }

        public void Show(string placement, Action<AdOutcome, string> done)
        {
            if (_bridge == null || !_rewardReady || _pendingReward != null)
            {
                _bridge?.Call("loadRewarded");
                done?.Invoke(AdOutcome.Failed, null);
                return;
            }
            _pendingReward = done;
            _rewardedGranted = false;
            _bridge.Call("showRewarded");
        }

        public void ShowInterstitial(string placement, Action<bool> done)
        {
            if (_bridge == null || !_interReady || _pendingInter != null)
            {
                if (_bridge != null && !_interReady) _bridge.Call("loadInterstitial");
                done?.Invoke(false);
                return;
            }
            _pendingInter = done;
            _bridge.Call("showInterstitial");
        }
    }
}
#endif
