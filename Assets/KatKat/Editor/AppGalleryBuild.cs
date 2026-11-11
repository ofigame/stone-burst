using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Huawei AppGallery derlemesi (kullanıcı, 2026-11-11: "App Gallery için reklamları, Google bağlılıklarını gözden
    /// geçir. App Gallery'ye uygun bir versiyon oluştur").
    /// - Tanımlar: KATKAT_APPGALLERY (KATKAT_ADMOB ve KATKAT_ONLINE yok → AdMob, Play Games, Firebase kodu derlenmez).
    /// - Android eklentileri: AdMob/Play Games/Firebase Android kütüphaneleri bu derlemede kapalı; HwAdsBridge.java açık.
    /// - Gradle: üretilen projeden Google Play Services / Firebase / AdMob / UMP / Unity Ads bağımlılıkları çıkarılır,
    ///   Huawei Ads (ads-lite) ve Huawei Maven deposu eklenir (EDM derlemede şablonu yeniden yazsa da çıktıya uygulanır).
    /// Bütün ayarlar derlemeden sonra geri alınır; Google Play derlemesi değişmez.
    /// </summary>
    public static class AppGalleryBuild
    {
        public const string Define = "KATKAT_APPGALLERY";
        public const string HuaweiAds = "com.huawei.hms:ads-lite:13.4.73.301";
        public const string HuaweiRepo = "https://developer.huawei.com/repo/";
        public const string KotlinBom = "org.jetbrains.kotlin:kotlin-bom:1.8.22";
        public const string BridgePath = "Assets/Plugins/Android/HwAdsBridge.java";

        /// <summary>Bu derlemede Android'den çıkarılan Google eklentileri.</summary>
        private static readonly string[] GooglePlugins =
        {
            "Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib",
            "Assets/Plugins/Android/googlemobileads-unity.aar",
            "Assets/Plugins/Android/GooglePlayGamesManifest.androidlib",
            "Assets/Plugins/Android/FirebaseApp.androidlib",
            "Assets/GooglePlayGames/com.google.play.games/Runtime/Plugins/Android/gpgs-plugin-support.aar",
        };

        /// <summary>Gradle çıktısından çıkarılan bağımlılık kalıpları.</summary>
        private static readonly string[] StripDeps =
        {
            "com.google.android.gms:", "com.google.firebase:", "com.google.android.ump:", "com.google.ads.mediation:",
            "com.unity3d.ads:", "googlemobileads-unity", "gpgs-plugin-support",
            "GoogleMobileAdsPlugin", "GooglePlayGamesManifest", "FirebaseApp",
        };

        /// <summary>Derleme sürerken true (Gradle kancası yalnız o zaman çalışır).</summary>
        public static bool Active { get; private set; }

        private static Dictionary<string, bool> _pluginState;
        private static string _definesBefore;

        /// <summary>Derleme öncesi: tanımlar ve eklenti ayarları.</summary>
        public static void Begin()
        {
            Active = true;
            var target = NamedBuildTarget.Android;
            _definesBefore = PlayerSettings.GetScriptingDefineSymbols(target);
            var defs = _definesBefore.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(d => d != "KATKAT_ADMOB" && d != "KATKAT_ONLINE" && d != Define).ToList();
            defs.Add(Define);
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defs));
            _pluginState = new Dictionary<string, bool>();
            foreach (var p in GooglePlugins) SetAndroid(p, false);
            SetAndroid(BridgePath, true);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Derleme sonrası: her şey eski hâline.</summary>
        public static void End()
        {
            if (_definesBefore != null) PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, _definesBefore);
            if (_pluginState != null)
                foreach (var kv in _pluginState)
                {
                    var imp = AssetImporter.GetAtPath(kv.Key) as PluginImporter;
                    if (imp == null) continue;
                    imp.SetCompatibleWithPlatform(BuildTarget.Android, kv.Value);
                    imp.SaveAndReimport();
                }
            _pluginState = null;
            _definesBefore = null;
            Active = false;
            AssetDatabase.SaveAssets();
        }

        private static void SetAndroid(string path, bool on)
        {
            var imp = AssetImporter.GetAtPath(path) as PluginImporter;
            if (imp == null) { Debug.LogWarning("[KatKat] AppGallery: eklenti bulunamadı: " + path); return; }
            _pluginState[path] = imp.GetCompatibleWithPlatform(BuildTarget.Android);
            if (_pluginState[path] == on) return;
            imp.SetCompatibleWithPlatform(BuildTarget.Android, on);
            imp.SaveAndReimport();
        }

        /// <summary>Google Play derlemesinde köprü dosyası Android'e girmesin (Huawei kütüphanesi yokken derlenemez).</summary>
        public static void EnsureBridgeOffForGoogle()
        {
            var imp = AssetImporter.GetAtPath(BridgePath) as PluginImporter;
            if (imp == null || !imp.GetCompatibleWithPlatform(BuildTarget.Android)) return;
            imp.SetCompatibleWithPlatform(BuildTarget.Android, false);
            imp.SaveAndReimport();
        }

        /// <summary>unityLibrary/build.gradle: Google bağımlılıklarını çıkar, Huawei Ads ekle (saf metin; test edilebilir).</summary>
        public static string PatchLibraryGradle(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            lines.RemoveAll(l => IsGoogleLine(l));
            // Unity üretilen dosyayı her derlemede yeniden yazmayabilir: her satır ayrı ayrı denetlenir (önceki sürüm satırları silinir).
            lines.RemoveAll(l => l.Contains("// AppGallery:"));
            int i = lines.FindIndex(l => Regex.IsMatch(l.Trim(), @"^dependencies\s*\{"));
            if (i < 0) throw new InvalidOperationException("AppGallery: build.gradle'da dependencies bloğu yok");
            // Huawei Ads kotlin-stdlib-jdk7/jdk8 1.6 getiriyor; androidx kotlin-stdlib 1.8 → yinelenen sınıf hatası.
            // Bu iki eski parça dışlanır (1.8'de içerikleri kotlin-stdlib'de) ve BOM Kotlin sürümlerini hizalar.
            lines.Insert(i + 1, "    implementation('" + HuaweiAds + "') { exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk7'; exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk8' } // AppGallery: Huawei Ads");
            lines.Insert(i + 2, "    implementation platform('" + KotlinBom + "') // AppGallery: Kotlin sürüm hizalama");
            return string.Join("\n", lines);
        }

        /// <summary>Google bağımlılığı/eklenti betiği satırı mı (implementation, project(...), apply from: ...).
        /// AdMob eklentisi kendi androidlib klasöründeki betikleri "apply from" ile ekler; klasör kapalıyken kalmamalı.</summary>
        public static bool IsGoogleLine(string l) =>
            (l.Contains("implementation") || l.Contains("api ") || l.Contains("project(") || l.TrimStart().StartsWith("apply from"))
            && StripDeps.Any(l.Contains);

        /// <summary>launcher/build.gradle: Google eklenti betiklerini çıkar.</summary>
        public static string PatchLauncherGradle(string text) =>
            string.Join("\n", text.Replace("\r\n", "\n").Split('\n').Where(l => !IsGoogleLine(l)));

        /// <summary>settings.gradle: Huawei Maven deposu (dependencyResolutionManagement içine).</summary>
        public static string PatchSettingsGradle(string text)
        {
            if (text.Contains(HuaweiRepo)) return text;
            int drm = text.IndexOf("dependencyResolutionManagement", StringComparison.Ordinal);
            if (drm < 0) throw new InvalidOperationException("AppGallery: settings.gradle'da dependencyResolutionManagement yok");
            int repos = text.IndexOf("repositories {", drm, StringComparison.Ordinal);
            if (repos < 0) throw new InvalidOperationException("AppGallery: settings.gradle'da repositories yok");
            int at = repos + "repositories {".Length;
            return text.Insert(at, "\n        maven { url \"" + HuaweiRepo + "\" } // AppGallery: Huawei Ads");
        }

        /// <summary>Üretilen Gradle projesine uygulanır (yalnız AppGallery derlemesi sürerken).</summary>
        private sealed class GradleHook : IPostGenerateGradleAndroidProject
        {
            public int callbackOrder => 1000;

            public void OnPostGenerateGradleAndroidProject(string path)
            {
                if (!Active) return;
                var lib = Path.Combine(path, "build.gradle");
                File.WriteAllText(lib, PatchLibraryGradle(File.ReadAllText(lib)));
                var launcher = Path.Combine(Directory.GetParent(path).FullName, "launcher", "build.gradle");
                if (File.Exists(launcher)) File.WriteAllText(launcher, PatchLauncherGradle(File.ReadAllText(launcher)));
                var settings = Path.Combine(Directory.GetParent(path).FullName, "settings.gradle");
                if (File.Exists(settings))
                {
                    var s = File.ReadAllText(settings);
                    // Kapatılan Google androidlib projeleri ayarlarda kalmasın.
                    s = string.Join("\n", s.Replace("\r\n", "\n").Split('\n').Where(l => !(l.TrimStart().StartsWith("include") && StripDeps.Concat(new[] { "GoogleMobileAdsPlugin", "GooglePlayGamesManifest", "FirebaseApp" }).Any(l.Contains))));
                    File.WriteAllText(settings, PatchSettingsGradle(s));
                }
                Debug.Log("[KatKat] AppGallery: Gradle projesi Google servislerinden arındırıldı, Huawei Ads eklendi");
            }
        }
    }
}
