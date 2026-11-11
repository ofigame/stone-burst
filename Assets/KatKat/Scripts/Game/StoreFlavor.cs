namespace KatKat.Game
{
    /// <summary>
    /// Derlemenin hangi mağaza için olduğu (kullanıcı, 2026-11-11: "App Gallery'ye uygun bir versiyon oluştur").
    /// AppGallery derlemesi KATKAT_APPGALLERY tanımıyla yapılır (BuildScripts.BuildAndroidAppGallery): Google servisleri
    /// (AdMob, Play Games, Firebase) yoktur; reklam Huawei Ads; hesap/bulut kaydı ve çevrimiçi sıralama kapalıdır.
    /// </summary>
    public static class StoreFlavor
    {
#if KATKAT_APPGALLERY
        public const bool AppGallery = true;
#else
        public const bool AppGallery = false;
#endif

        /// <summary>Hesap (giriş, bulut kaydı, sıralama kaydı) gösterilsin mi. AppGallery'de gerçek hesap hizmeti
        /// bağlanana kadar kapalı (aksi hâlde "(test)" etiketli yerel test hesabı görünürdü).</summary>
        public static bool AccountsEnabled => !AppGallery || GameController.AccountProvider != null;
    }
}
