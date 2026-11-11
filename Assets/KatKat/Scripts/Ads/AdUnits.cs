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
