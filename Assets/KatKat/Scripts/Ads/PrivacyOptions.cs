using UnityEngine;

namespace KatKat.Game.Ads
{
    /// <summary>
    /// Ayarlar → "Gizlilik seçenekleri" (kullanıcı, 2026-11-09). Google UMP, oyuncunun bölgesinde gerekli gördüğünde
    /// (AEA, Birleşik Krallık, İsviçre; ABD eyaletleri) reklam rızasını yeniden açan düğmenin gösterilmesini zorunlu
    /// tutar. Gerekmeyen bölgelerde satır gösterilmez. Gizlilik politikası (ofigame.com/legal/privacy) bu düğmeye atıf yapar.
    /// </summary>
    public static class PrivacyOptions
    {
        /// <summary>Yakalama/test: satırı her durumda göster.</summary>
        public static bool ForceVisible;

        public static bool Required
        {
            get
            {
                if (ForceVisible) return true;
#if KATKAT_ADMOB && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
                return AdMobNetwork.PrivacyOptionsRequired;
#else
                return false;
#endif
            }
        }

        public static void Show()
        {
#if KATKAT_ADMOB && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            AdMobNetwork.ShowPrivacyOptions();
#else
            Debug.Log("[KatKat] Gizlilik seçenekleri: yalnız cihazda (UMP formu)");
#endif
        }
    }
}
