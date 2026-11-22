namespace KatKat.Game.Ads
{
    /// <summary>
    /// AdMob reklam birimi kimlikleri.
    /// - iOS: gerçek Stone Burst birimleri (kullanıcının AdMob hesabı, 2026-11-09). Unity Ads bu birimlere AdMob
    ///   arabuluculuğuyla bağlı (gruplar "Stone Burst Ödüllü/Geçiş iOS - Unity").
    /// - Android (Google Play): gerçek Stone Burst birimleri (2026-11-12); Unity Ads AdMob arabuluculuğuyla bağlı.
    /// Uygulama kimlikleri Editor/AdMobSetup.cs içinde.
    /// Not: geliştirme sırasında gerçek birimlere kendi reklamına tıklamak hesabı riske atar; cihaz testinde AdMob
    /// konsolunda test cihazı tanımlanmalı.
    /// </summary>
    public static class AdUnits
    {
#if KATKAT_APPGALLERY
        // AppGallery (Huawei Ads, 2026-11-11): Huawei'nin herkese açık TEST birimleri (gelir getirmez). Gerçek birimler
        // Huawei Ads Publisher Service'te uygulama için açılınca buraya yazılır (kullanıcının hesabı).
        public const bool UsingTestIds = true;
        public const string Rewarded = "testx9dtjwj8hp";      // Huawei test: ödüllü video
        public const string Interstitial = "teste9ih9j0rc3";  // Huawei test: geçiş (görsel)
        // Doğrudan Unity Ads (Huawei'de reklam yoksa yedek; 2026-11-12). "Stone Burst Android" uygulaması, gerçek kimlikler.
        public const string UnityGameId = "800434959";
        // 2026-11-22 (Huawei tablette logcat): BP_ yerleri açık artırmalı, yalnız arabuluculukla dolar; doğrudan SDK'da her istek
        // NO_FILL. Panelde Stone Burst Android'e normal yerler açıldı (kullanıcı: "Evet, aç").
        public const string UnityRewarded = "Rewarded_Android";
        public const string UnityInterstitial = "Interstitial_Android";
#elif KATKAT_TEST_ADS
        // Yerel deneme APK'ları (kullanıcı 2026-11-14: "gerçek reklam geldi. Test reklamları olsun apk derlerken"): Google'ın
        // herkese açık AdMob TEST birimleri; gelir getirmez, kendi reklamına tıklama riski yok. Mağaza paketleri (AAB, iOS) bu
        // tanımı almaz; gerçek birimler aşağıda.
        public const bool UsingTestIds = true;
#if UNITY_IOS
        public const string Rewarded = "ca-app-pub-3940256099942544/1712485313";      // Google test: ödüllü (iOS)
        public const string Interstitial = "ca-app-pub-3940256099942544/4411468910";  // Google test: geçiş (iOS)
#else
        public const string Rewarded = "ca-app-pub-3940256099942544/5224354917";      // Google test: ödüllü (Android)
        public const string Interstitial = "ca-app-pub-3940256099942544/1033173712";  // Google test: geçiş (Android)
#endif
#elif UNITY_IOS
        public const bool UsingTestIds = false;
        public const string Rewarded = "ca-app-pub-2558895513508689/3403938167";      // stoneburst_rewarded
        public const string Interstitial = "ca-app-pub-2558895513508689/2320142547";  // stoneburst_interstitial
#else
        // Android / Google Play (2026-11-12): gerçek birimler; Unity Ads AdMob arabuluculuğuyla bağlı
        // (gruplar "Stone Burst Ödüllü/Geçiş Android - Unity", Unity oyun kimliği 800434959, BP_Rewarded/BP_Interstitial_Android).
        public const bool UsingTestIds = false;
        public const string Rewarded = "ca-app-pub-2558895513508689/9721541294";      // stoneburst_rewarded
        public const string Interstitial = "ca-app-pub-2558895513508689/6260662649";  // stoneburst_interstitial
#endif
    }
}
