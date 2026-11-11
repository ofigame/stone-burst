package com.ofigame.stoneburst.hw;

import android.app.Activity;

import com.unity3d.ads.IUnityAdsInitializationListener;
import com.unity3d.ads.IUnityAdsLoadListener;
import com.unity3d.ads.IUnityAdsShowListener;
import com.unity3d.ads.UnityAds;
import com.unity3d.ads.UnityAdsShowOptions;
import com.unity3d.ads.metadata.MetaData;

/**
 * Stone Burst AppGallery derlemesi: doğrudan Unity Ads (AdMob yok). Huawei Ads reklam bulamazsa yedek olarak kullanılır
 * (2026-11-12, kullanıcı: "app gallery içinde unity ads aktif olsun"). Yalnız AppGallery derlemesinde Android'e girer.
 * Rıza formu yok: GDPR/CCPA rızası "hayır" olarak bildirilir, yani yalnız kişiselleştirilmemiş reklam.
 * Not (2026-11-12): Unity'nin yeni paneli yalnız teklifli (BP_) yer açıyor ve "yalnız arabuluculuk kullananları destekliyor";
 * doğrudan kullanımda reklam gelip gelmediği Huawei cihazda denenmeli.
 */
public class UnityAdsBridge {
    /** kind: u_init, u_init_failed, u_reward_loaded, u_reward_load_failed, u_rewarded, u_reward_closed,
     *  u_reward_show_failed, u_inter_loaded, u_inter_failed, u_inter_closed. */
    public interface Listener {
        void onEvent(String kind, int code);
    }

    private final Activity activity;
    private final Listener listener;
    private final String gameId, rewardedId, interstitialId;

    public UnityAdsBridge(Activity activity, Listener listener, String gameId, String rewardedId, String interstitialId) {
        this.activity = activity;
        this.listener = listener;
        this.gameId = gameId;
        this.rewardedId = rewardedId;
        this.interstitialId = interstitialId;
    }

    private void emit(String kind, int code) {
        try { listener.onEvent(kind, code); } catch (Throwable ignored) { }
    }

    public void init() {
        activity.runOnUiThread(() -> {
            try {
                MetaData gdpr = new MetaData(activity);
                gdpr.set("gdpr.consent", false);
                gdpr.commit();
                MetaData privacy = new MetaData(activity);
                privacy.set("privacy.consent", false);
                privacy.commit();
                UnityAds.initialize(activity.getApplicationContext(), gameId, false, new IUnityAdsInitializationListener() {
                    @Override public void onInitializationComplete() {
                        emit("u_init", 0);
                        loadRewarded();
                        loadInterstitial();
                    }
                    @Override public void onInitializationFailed(UnityAds.UnityAdsInitializationError error, String message) {
                        emit("u_init_failed", error == null ? -1 : error.ordinal());
                    }
                });
            } catch (Throwable t) { emit("u_init_failed", -2); }
        });
    }

    public void loadRewarded() { load(rewardedId, "u_reward_loaded", "u_reward_load_failed"); }

    public void loadInterstitial() { load(interstitialId, "u_inter_loaded", "u_inter_failed"); }

    private void load(String placement, String ok, String fail) {
        activity.runOnUiThread(() -> {
            try {
                UnityAds.load(placement, new IUnityAdsLoadListener() {
                    @Override public void onUnityAdsAdLoaded(String placementId) { emit(ok, 0); }
                    @Override public void onUnityAdsFailedToLoad(String placementId, UnityAds.UnityAdsLoadError error, String message) {
                        emit(fail, error == null ? -1 : error.ordinal() + 1);
                    }
                });
            } catch (Throwable t) { emit(fail, -2); }
        });
    }

    public void showRewarded() { show(rewardedId, true); }

    public void showInterstitial() { show(interstitialId, false); }

    private void show(String placement, boolean rewarded) {
        activity.runOnUiThread(() -> {
            try {
                UnityAds.show(activity, placement, new UnityAdsShowOptions(), new IUnityAdsShowListener() {
                    @Override public void onUnityAdsShowFailure(String placementId, UnityAds.UnityAdsShowError error, String message) {
                        emit(rewarded ? "u_reward_show_failed" : "u_inter_failed", error == null ? -1 : error.ordinal() + 1);
                    }
                    @Override public void onUnityAdsShowStart(String placementId) { }
                    @Override public void onUnityAdsShowClick(String placementId) { }
                    @Override public void onUnityAdsShowComplete(String placementId, UnityAds.UnityAdsShowCompletionState state) {
                        if (rewarded && state == UnityAds.UnityAdsShowCompletionState.COMPLETED) emit("u_rewarded", 0);
                        emit(rewarded ? "u_reward_closed" : "u_inter_closed", 0);
                    }
                });
            } catch (Throwable t) { emit(rewarded ? "u_reward_show_failed" : "u_inter_failed", -4); }
        });
    }
}
