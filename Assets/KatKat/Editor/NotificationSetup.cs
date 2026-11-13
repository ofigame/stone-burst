using Unity.Notifications;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Yerel bildirim ayarları (kullanıcı, 2026-11-13). Android ve iOS derlemesinden önce uygulanır:
    /// - Android küçük simge "icon_0" = Art/notify-small.png (beyaz siluet; durum çubuğunda tek renk).
    /// - Cihaz yeniden başlayınca planlı bildirimler yeniden kurulur.
    /// - iOS: izin açılışta İSTENMEZ (oyun ilk kalp bitişinde kendi kartıyla sorar).
    /// </summary>
    public static class NotificationSetup
    {
        public const string SmallIconPath = "Assets/KatKat/Art/notify-small.png";
        /// <summary>Bildirimin sağındaki renkli simge (icon_1) = uygulama simgesi 256 px.</summary>
        public const string LargeIconPath = "Assets/KatKat/Art/notify-large.png";

        private static Texture2D Readable(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (!imp.isReadable || imp.textureCompression != TextureImporterCompression.Uncompressed || imp.mipmapEnabled))
            {
                imp.textureType = TextureImporterType.Default;
                imp.isReadable = true;
                imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        [MenuItem("Kat Kat/Build/Bildirim ayarlarını uygula")]
        public static void Apply()
        {
            var imp = AssetImporter.GetAtPath(SmallIconPath) as TextureImporter;
            if (imp != null && (!imp.isReadable || imp.textureCompression != TextureImporterCompression.Uncompressed || imp.mipmapEnabled || !imp.alphaIsTransparency))
            {
                imp.textureType = TextureImporterType.Default;
                imp.isReadable = true;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SmallIconPath);
            if (tex == null) Debug.LogWarning("[KatKat] Bildirim simgesi yok: " + SmallIconPath);
            else
            {
                NotificationSettings.AndroidSettings.ClearDrawableResources();
                NotificationSettings.AndroidSettings.AddDrawableResource("icon_0", tex, NotificationIconType.Small);
                var large = Readable(LargeIconPath);
                if (large != null) NotificationSettings.AndroidSettings.AddDrawableResource("icon_1", large, NotificationIconType.Large);
                else Debug.LogWarning("[KatKat] Büyük bildirim simgesi yok: " + LargeIconPath);
            }
            NotificationSettings.AndroidSettings.RescheduleOnDeviceRestart = true;
            // Google Play: SCHEDULE_EXACT_ALARM / USE_EXACT_ALARM yalnız alarm/takvim uygulamalarına; yaklaşık zamanlama yeter.
            NotificationSettings.AndroidSettings.ExactSchedulingOption = 0;
            NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[KatKat] Bildirim ayarları uygulandı (icon_0, yeniden başlatmada yeniden planla, iOS açılışta izin yok)");
        }
    }
}
