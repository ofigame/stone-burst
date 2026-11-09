#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace KatKat.EditorTools
{
    /// <summary>
    /// iOS Xcode projesi son ayarları (kullanıcı, 2026-11-09: iOS ilk yayın; derleme GitHub Actions'ta).
    /// - Info.plist: şifreleme beyanı (yalnız HTTPS → muaf), dil listesi (9 dil), Game Center gereksinimi yok.
    /// - Yetenek: Game Center (giriş ve sıralama kimliği).
    /// Reklam SDK'sı gelince NSUserTrackingUsageDescription ve SKAdNetwork listesi burada eklenecek.
    /// </summary>
    public static class IosPostBuild
    {
        private static readonly string[] Languages = { "tr", "en", "es", "ar", "de", "it", "fr", "ja", "ko" };

        [PostProcessBuild(100)]
        public static void OnPostBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;

            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;
            root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            var langs = root.CreateArray("CFBundleLocalizations");
            foreach (var l in Languages) langs.AddString(l);
            root.SetString("CFBundleDisplayName", "Stone Burst");
            // Oyun kategorisi: iOS "Oyun Modu"nu bu bilgiyle açar (kullanıcı, 2026-11-09: "candy crush'a giriyorum
            // oyun modu açık diyor ama bizimkinde demiyor").
            root.SetString("LSApplicationCategoryType", "public.app-category.puzzle-games");
            root.SetBoolean("GCSupportsGameMode", true);
            plist.WriteToFile(plistPath);

            string projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            string main = proj.GetUnityMainTargetGuid();
            // ATT (2026-11-09): KatKatNative.mm izin penceresini ister; çerçeve UnityFramework hedefine zayıf bağlanır.
            proj.AddFrameworkToProject(proj.GetUnityFrameworkTargetGuid(), "AppTrackingTransparency.framework", true);
            proj.WriteToFile(projPath);
            var caps = new ProjectCapabilityManager(projPath, "StoneBurst.entitlements", null, main);
            caps.AddGameCenter();
            caps.WriteToFile();
            UnityEngine.Debug.Log("[KatKat] iOS son ayarları uygulandı (Info.plist + Game Center)");
        }
    }
}
#endif
