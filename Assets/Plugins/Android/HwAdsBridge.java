package com.ofigame.stoneburst.hw;

import android.app.Activity;

import com.huawei.hms.ads.AdListener;
import com.huawei.hms.ads.AdParam;
import com.huawei.hms.ads.HwAds;
import com.huawei.hms.ads.InterstitialAd;
import com.huawei.hms.ads.NonPersonalizedAd;
import com.huawei.hms.ads.RequestOptions;
import com.huawei.hms.ads.reward.Reward;
import com.huawei.hms.ads.reward.RewardAd;
import com.huawei.hms.ads.reward.RewardAdLoadListener;
import com.huawei.hms.ads.reward.RewardAdStatusListener;

/**
 * Stone Burst AppGallery derlemesi: Huawei Ads (ads-lite) köprüsü. Yalnız AppGallery derlemesinde Android'e dahil edilir
 * (BuildScripts.BuildAndroidAppGallery eklenti ayarını açar; Google Play derlemesinde kapalıdır).
 * Huawei dinleyicileri sınıf olduğu için (AndroidJavaProxy yalnız arayüz destekler) olaylar tek arayüzle Unity'ye iletilir.
 * Bütün reklam çağrıları UI iş parçacığında yapılır. Kişiselleştirilmemiş reklam istenir (rıza formu gerekmez).
 */
public class HwAdsBridge {
    /** Unity tarafı (C# AndroidJavaProxy). kind: reward_loaded, reward_load_failed, reward_opened, rewarded,
     *  reward_closed, reward_show_failed, inter_loaded, inter_failed, inter_closed. */
    public interface Listener {
        void onEvent(String kind, int code);
    }

    private final Activity activity;
    private final Listener listener;
    private final String rewardedId;
    private final String interstitialId;
    private RewardAd rewardAd;
    private InterstitialAd interstitialAd;

    public HwAdsBridge(Activity activity, Listener listener, String rewardedId, String interstitialId) {
        this.activity = activity;
        this.listener = listener;
        this.rewardedId = rewardedId;
        this.interstitialId = interstitialId;
    }

    private void emit(String kind, int code) {
        try { listener.onEvent(kind, code); } catch (Throwable ignored) { }
    }

    public void init() {
        activity.runOnUiThread(() -> {
            try {
                HwAds.init(activity);
                RequestOptions opts = HwAds.getRequestOptions().toBuilder()
                        .setNonPersonalizedAd(NonPersonalizedAd.ALLOW_NON_PERSONALIZED).build();
                HwAds.setRequestOptions(opts);
            } catch (Throwable t) {
                emit("reward_load_failed", -1);
                return;
            }
            loadRewardedOnUi();
            loadInterstitialOnUi();
        });
    }

    public void loadRewarded() { activity.runOnUiThread(this::loadRewardedOnUi); }

    private void loadRewardedOnUi() {
        try {
            rewardAd = new RewardAd(activity, rewardedId);
            rewardAd.loadAd(new AdParam.Builder().build(), new RewardAdLoadListener() {
                @Override public void onRewardedLoaded() { emit("reward_loaded", 0); }
                @Override public void onRewardAdFailedToLoad(int errorCode) { emit("reward_load_failed", errorCode); }
            });
        } catch (Throwable t) { emit("reward_load_failed", -2); }
    }

    public void showRewarded() {
        activity.runOnUiThread(() -> {
            if (rewardAd == null || !rewardAd.isLoaded()) { emit("reward_show_failed", -3); return; }
            try {
                rewardAd.show(activity, new RewardAdStatusListener() {
                    @Override public void onRewardAdOpened() { emit("reward_opened", 0); }
                    @Override public void onRewarded(Reward reward) { emit("rewarded", 0); }
                    @Override public void onRewardAdClosed() { emit("reward_closed", 0); }
                    @Override public void onRewardAdFailedToShow(int errorCode) { emit("reward_show_failed", errorCode); }
                });
            } catch (Throwable t) { emit("reward_show_failed", -4); }
        });
    }

    public void loadInterstitial() { activity.runOnUiThread(this::loadInterstitialOnUi); }

    private void loadInterstitialOnUi() {
        try {
            interstitialAd = new InterstitialAd(activity);
            interstitialAd.setAdId(interstitialId);
            interstitialAd.setAdListener(new AdListener() {
                @Override public void onAdLoaded() { emit("inter_loaded", 0); }
                @Override public void onAdFailed(int errorCode) { emit("inter_failed", errorCode); }
                @Override public void onAdClosed() { emit("inter_closed", 0); }
            });
            interstitialAd.loadAd(new AdParam.Builder().build());
        } catch (Throwable t) { emit("inter_failed", -2); }
    }

    public void showInterstitial() {
        activity.runOnUiThread(() -> {
            if (interstitialAd == null || !interstitialAd.isLoaded()) { emit("inter_failed", -3); return; }
            try { interstitialAd.show(activity); } catch (Throwable t) { emit("inter_failed", -4); }
        });
    }
}
