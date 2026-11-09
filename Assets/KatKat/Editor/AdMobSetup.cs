using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// AdMob ayarları (kullanıcı, 2026-11-09: "reklamsız çıkmayalım"). Her derlemede:
    /// - GoogleMobileAdsSettings: uygulama kimlikleri (şimdilik Google'ın TEST uygulama kimlikleri) ve iOS izin
    ///   metni (NSUserTrackingUsageDescription; App Tracking Transparency).
    /// - KATKAT_ADMOB tanımı iOS ve Android'de açılır (Scripts/Ads/AdMobNetwork.cs derlenir).
    /// Gerçek kimlikler: AdMob konsolu → Uygulamalar → Uygulama ayarları → "Uygulama kimliği" (ca-app-pub-…~…).
    /// Kullanıcı onayıyla <see cref="IosAppId"/> / <see cref="AndroidAppId"/> ve Scripts/Ads/AdUnits.cs değişir.
    /// </summary>
    public static class AdMobSetup
    {
        public const string SettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        public const string IosAppId = "ca-app-pub-2558895513508689~7151786494";      // Stone Burst iOS, gerçek (2026-11-09)
        public const string AndroidAppId = "ca-app-pub-3940256099942544~3347511713";  // Google TEST: Android uygulaması AdMob'da henüz açılmadı
        public const string TrackingText = "Your data will be used to show you more relevant ads. The game works the same either way.";
        public const string Define = "KATKAT_ADMOB";

        [MenuItem("Kat Kat/Kurulum/AdMob ayarları")]
        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder("Assets/GoogleMobileAds")) { Debug.LogWarning("[KatKat] AdMob eklentisi yok; reklam kapalı"); return; }
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsPath);
            if (settings == null)
            {
                var type = System.Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
                var load = type?.GetMethod("LoadInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                settings = load?.Invoke(null, null) as ScriptableObject;
            }
            if (settings == null) { Debug.LogError("[KatKat] GoogleMobileAdsSettings oluşturulamadı"); return; }
            var so = new SerializedObject(settings);
            Set(so, "adMobIOSAppId", IosAppId);
            Set(so, "adMobAndroidAppId", AndroidAppId);
            Set(so, "userTrackingUsageDescription", TrackingText);
            // Firebase 14.1.0 Google'ın EDM'sini (com.google.external-dependency-manager 1.2.190) ister; AdMob'un
            // kurduğu Unity EDM (com.unity.external-dependency-manager) ile aynı projede çakışır. AdMob'un EDM kurulumu
            // kapatılır (eklentinin kendi "opt-out" ayarı), bağımlılıkları Google EDM çözer (2026-11-09).
            var edm = so.FindProperty("enableExternalDependencyManager");
            if (edm != null) edm.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AddDefine(NamedBuildTarget.iOS, Define);
            AddDefine(NamedBuildTarget.Android, Define);
            // Çevrimiçi hesap ve sıralama (Firebase + Game Center): yalnız iOS. Android'de Firebase Android uygulaması ve
            // Play Games eklentisi henüz yok; orada test hesabı çalışır.
            if (System.IO.File.Exists("Assets/KatKat/Firebase/GoogleService-Info.plist")) AddDefine(NamedBuildTarget.iOS, OnlineDefine);
            Debug.Log("[KatKat] AdMob ayarları uygulandı (iOS gerçek, Android test kimliği); KATKAT_ONLINE yalnız iOS");
        }

        public const string OnlineDefine = "KATKAT_ONLINE";

        private static void Set(SerializedObject so, string field, string value)
        {
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning("[KatKat] AdMob ayar alanı yok: " + field); return; }
            p.stringValue = value;
        }

        private static void AddDefine(NamedBuildTarget t, string define)
        {
            var defs = PlayerSettings.GetScriptingDefineSymbols(t);
            if (System.Array.IndexOf(defs.Split(';'), define) >= 0) return;
            PlayerSettings.SetScriptingDefineSymbols(t, string.IsNullOrEmpty(defs) ? define : defs + ";" + define);
        }
    }
}
