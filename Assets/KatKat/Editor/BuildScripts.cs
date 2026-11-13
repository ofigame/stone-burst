using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Kat Kat build girişleri. Crate Boss betikleriyle ilgisi yoktur.
    /// Android: Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod KatKat.EditorTools.BuildScripts.BuildAndroid
    /// </summary>
    public static class BuildScripts
    {
        [MenuItem("Kat Kat/Build/Android APK")]
        public static void BuildAndroid()
        {
            Build(BuildTarget.Android, "Builds/Android/StoneBurst.apk");
        }

        /// <summary>
        /// vr1 P1 cihaz deneme APK'sı (kullanıcı, 2026-11-11: "Evet, hazırla"): KATKAT_VR1PROTO tanımıyla prototip varsayılan
        /// açık; Ayarlar'da sürüm yazısına dokununca açılıp kapanır. Hata ayıklama imzalı yerel APK; mağazaya gitmez.
        /// </summary>
        [MenuItem("Kat Kat/Build/Android APK (vr1 prototipi)")]
        public static void BuildAndroidVr1()
        {
            _extraDefines = new[] { "KATKAT_VR1PROTO" };
            try { Build(BuildTarget.Android, "Builds/Android/StoneBurst-vr1.apk"); }
            finally { _extraDefines = null; }
        }

        /// <summary>
        /// Play Store paketi (kullanıcı, 2026-11-10: "play store hazırlığımızı da yapalım"): imzalı AAB.
        /// Yükleme anahtarı depoda değil: KATKAT_ANDROID_KEYSTORE (varsayılan E:\OFIGAME\OYUNLAR\keys\stoneburst-upload.jks),
        /// şifre dosyası KATKAT_ANDROID_KEYPASS_FILE (varsayılan aynı klasörde stoneburst-upload-sifre.txt), takma ad
        /// "stoneburst". Sürüm kodu KATKAT_ANDROID_VERSION_CODE (her yüklemede artmalı; varsayılan 1).
        /// Derlemeden sonra imza ve AAB ayarları geri alınır (ProjectSettings'e yerel yol yazılmasın).
        /// </summary>
        [MenuItem("Kat Kat/Build/Android Play Store (AAB)")]
        public static void BuildAndroidStore()
        {
            string ks = Environment.GetEnvironmentVariable("KATKAT_ANDROID_KEYSTORE");
            if (string.IsNullOrEmpty(ks)) ks = @"E:\OFIGAME\OYUNLAR\keys\stoneburst-upload.jks";
            string pf = Environment.GetEnvironmentVariable("KATKAT_ANDROID_KEYPASS_FILE");
            if (string.IsNullOrEmpty(pf)) pf = Path.Combine(Path.GetDirectoryName(ks), "stoneburst-upload-sifre.txt");
            if (!File.Exists(ks) || !File.Exists(pf))
            {
                Debug.LogError("[KatKat] Play derlemesi: yükleme anahtarı ya da şifre dosyası yok (" + ks + ")");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            int code = int.TryParse(Environment.GetEnvironmentVariable("KATKAT_ANDROID_VERSION_CODE"), out var c) && c > 0 ? c : 1;
            string pass = File.ReadAllText(pf).Trim();
            _beforeBuild = () =>
            {
                PlayerSettings.Android.bundleVersionCode = code;
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ks;
                PlayerSettings.Android.keystorePass = pass;
                PlayerSettings.Android.keyaliasName = "stoneburst";
                PlayerSettings.Android.keyaliasPass = pass;
                EditorUserBuildSettings.buildAppBundle = true;
            };
            // AAB veri paketi LZ4HC ile (2026-11-10 ilk denemede sıkıştırmasız veri 236 MB tutuyordu; APK 130 MB).
            _deferExit = true;
            _extraOptions = BuildOptions.CompressWithLz4HC;
            try { Build(BuildTarget.Android, "Builds/Android/StoneBurst-" + PlayerSettings.bundleVersion + "-" + code + ".aab"); }
            finally
            {
                _beforeBuild = null;
                _deferExit = false;
                _extraOptions = BuildOptions.None;
                PlayerSettings.Android.useCustomKeystore = false;
                PlayerSettings.Android.keystoreName = "";
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasName = "";
                PlayerSettings.Android.keyaliasPass = "";
                EditorUserBuildSettings.buildAppBundle = false;
                PlayerSettings.Android.bundleVersionCode = 1;
                AssetDatabase.SaveAssets();
            }
            if (Application.isBatchMode) EditorApplication.Exit(_lastOk ? 0 : 1);
        }

        /// <summary>
        /// Huawei AppGallery paketi (kullanıcı, 2026-11-11): Google servisleri olmadan, Huawei Ads ile, imzalı APK
        /// (Play ile aynı yükleme anahtarı). Ayrıntı <see cref="AppGalleryBuild"/>. Sürüm kodu KATKAT_ANDROID_VERSION_CODE.
        /// </summary>
        [MenuItem("Kat Kat/Build/Android AppGallery (APK)")]
        public static void BuildAndroidAppGallery()
        {
            string ks = Environment.GetEnvironmentVariable("KATKAT_ANDROID_KEYSTORE");
            if (string.IsNullOrEmpty(ks)) ks = @"E:\OFIGAME\OYUNLAR\keys\stoneburst-upload.jks";
            string pf = Environment.GetEnvironmentVariable("KATKAT_ANDROID_KEYPASS_FILE");
            if (string.IsNullOrEmpty(pf)) pf = Path.Combine(Path.GetDirectoryName(ks), "stoneburst-upload-sifre.txt");
            if (!File.Exists(ks) || !File.Exists(pf))
            {
                Debug.LogError("[KatKat] AppGallery derlemesi: yükleme anahtarı ya da şifre dosyası yok (" + ks + ")");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            int code = int.TryParse(Environment.GetEnvironmentVariable("KATKAT_ANDROID_VERSION_CODE"), out var c) && c > 0 ? c : 1;
            string pass = File.ReadAllText(pf).Trim();
            _beforeBuild = () =>
            {
                AppGalleryBuild.Begin(); // AdMobSetup.Apply'dan sonra: Google tanımlarını kaldırır
                PlayerSettings.Android.bundleVersionCode = code;
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ks;
                PlayerSettings.Android.keystorePass = pass;
                PlayerSettings.Android.keyaliasName = "stoneburst";
                PlayerSettings.Android.keyaliasPass = pass;
                EditorUserBuildSettings.buildAppBundle = false;
            };
            _deferExit = true;
            try { Build(BuildTarget.Android, "Builds/Android/StoneBurst-AppGallery-" + PlayerSettings.bundleVersion + "-" + code + ".apk"); }
            finally
            {
                _beforeBuild = null;
                _deferExit = false;
                AppGalleryBuild.End();
                PlayerSettings.Android.useCustomKeystore = false;
                PlayerSettings.Android.keystoreName = "";
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasName = "";
                PlayerSettings.Android.keyaliasPass = "";
                PlayerSettings.Android.bundleVersionCode = 1;
                AssetDatabase.SaveAssets();
            }
            if (Application.isBatchMode) EditorApplication.Exit(_lastOk ? 0 : 1);
        }

        private static Action _beforeBuild;

        [MenuItem("Kat Kat/Build/Windows (önizleme)")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/StoneBurst.exe");
        }

        /// <summary>
        /// iOS Xcode projesi (kullanıcı, 2026-11-09: "githuba bağlayacağız orada derlenecek"). Windows'ta da çalışır;
        /// IPA'yı GitHub Actions'taki macOS adımı Xcode ile derleyip imzalar (.github/workflows/ios.yml).
        /// Çıkış klasörü KATKAT_IOS_OUT ile değişir (CI), yoksa Builds/iOS.
        /// </summary>
        [MenuItem("Kat Kat/Build/iOS Xcode projesi")]
        public static void BuildIOS()
        {
            // GameCI (unity-builder) ortam değişkenlerini Unity kapsayıcısına aktarmıyor; çıkış yolunu -customBuildPath ile
            // veriyor (2026-11-09 ilk CI denemesi: proje Builds/iOS'a yazılmış, Mac adımı bulamamıştı).
            string output = CommandLineArg("-customBuildPath") ?? Environment.GetEnvironmentVariable("KATKAT_IOS_OUT");
            Build(BuildTarget.iOS, string.IsNullOrEmpty(output) ? "Builds/iOS" : output);
        }

        private static string CommandLineArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        public const string IconPath = "Assets/KatKat/Art/app-icon.png";

        /// <summary>Uygulama ikonu (2026-11-10, 1.0.1 için): docs/gorseller/arayuz/uygulama-ikonu-yeni_v2.png (patlayan kırmızı katmanlı taş yığını, mavi ışın zemin; 1024). Önceki ikon arşivde: KatKat_Arsiv_2026-11-06/ikon_1.0_2026-11-10.
        /// Android uyarlanabilir: app-icon-bg = yeni görselin renkleriyle çizilmiş ışınlar + güvenli bölgede %68 ikon (kenarı yumuşak), app-icon-fg = saydam.</summary>
        private static Texture2D IconTex(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("[KatKat] ikon yok: " + path); return null; }
            if (imp.textureType != TextureImporterType.Default || imp.mipmapEnabled || imp.textureCompression != TextureImporterCompression.Uncompressed || !imp.alphaIsTransparency)
            {
                imp.textureType = TextureImporterType.Default;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.maxTextureSize = 1024;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// Uygulama ikonu. Masaüstü: kullanıcının görseli (app-icon). Android (kullanıcı, Redmi: "logo büyük çıktı,
        /// taşıyor, önünde Unity logosu var"): uyarlanabilir ikon katmanları verilmezse Android 8+ Unity'nin
        /// varsayılan ön katmanını gösteriyor ve kare ikonu kırpıyordu. Uyarlanabilir: zemin (app-icon-bg) + güvenli
        /// bölgede maskot (app-icon-fg); yuvarlak ve eski yuvalar paylı birleşik görsel (app-icon-legacy).
        /// </summary>
        public static void ApplyIcon()
        {
            var main = IconTex(IconPath);
            if (main == null) return;
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { main }, IconKind.Any);
            var bg = IconTex("Assets/KatKat/Art/app-icon-bg.png");
            var fg = IconTex("Assets/KatKat/Art/app-icon-fg.png");
            var legacy = IconTex("Assets/KatKat/Art/app-icon-legacy.png") ?? main;
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            int n = 0;
            // Unity 6.6: Android'de yalnızca uyarlanabilir ikon var (yuvarlak/eski türler kaldırıldı).
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(android))
            {
                var icons = PlayerSettings.GetPlatformIcons(android, kind);
                bool adaptive = kind.ToString().Contains("Adaptive");
                foreach (var icon in icons)
                {
                    if (adaptive && bg != null && fg != null) icon.SetTextures(bg, fg);
                    else icon.SetTexture(legacy);
                    n++;
                }
                PlayerSettings.SetPlatformIcons(android, kind, icons);
            }
            Debug.Log("[KatKat] Uygulama ikonu uygulandı (" + n + " Android yuvası, uyarlanabilir katmanlı)");
        }

        /// <summary>
        /// Android doku bütçesi (kullanıcı: "oyun boyutu büyük, kaliteyi bozmadan küçültelim"). Resources dokuları
        /// ham RGBA paketleniyordu (harita parçası 7,8 MB). Android için ASTC: tam ekran resimler (arka plan, harita
        /// şeridi, >1024) 5×5, diğerleri (arayüz, taş, simge) 4×4 — gözle fark edilmeyen en keskin ayarlar.
        /// Kodun piksel okuduğu dokular (isReadable: tiles/face-neutral, tiles/symbol-*) dokunulmaz.
        /// Masaüstü (exe) ayarları değişmez.
        /// </summary>
        [MenuItem("Kat Kat/Build/Android doku bütçesini uygula")]
        public static void ApplyTextureBudget()
        {
            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/KatKat/Resources" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                // V6 dokularını CPU okumaz (yalnızca sprite/shader); okunabilirlik bellekte kopya tutar.
                if (path.Contains("/PremiumV6/") && imp.isReadable) { imp.isReadable = false; }
                if (imp.isReadable) continue;
                // v10: kendi içe aktarma kuralı Android ASTC ayarını veren klasörler (ShellAssetImporter,
                // CollectionArtImporter, StoneModelImporter) burada yeniden ayarlanmaz (her derlemede çekişme olmasın).
                if (path.Contains("/PremiumV6/shell/") || path.Contains("/PremiumV6/collection/") || path.Contains("/PremiumV6/game/") || path.Contains("/PremiumV6/skills/") || path.Contains("/PremiumV6/rank/") || path.Contains("/Resources/UI1/")
                    || path.Contains("/Collection/stones") || path.Contains("/Models/Stones/")) continue;
                imp.GetSourceTextureWidthAndHeight(out int w, out int h);
                bool big = Mathf.Max(w, h) > 1024 || path.Contains("/backgrounds/") || path.Contains("/map/");
                var want = big ? TextureImporterFormat.ASTC_5x5 : TextureImporterFormat.ASTC_4x4;
                // Unity 6.6: 2'nin kuvveti olmayan (NPOT) dokuda mipmap açıkken ASTC uygulanmıyor, doku ham RGBA kalıyordu
                // (harita parçası 10,4 MB). Bu dokular ekranda ~kendi boyutunda çizilir; mipmap kapatılır.
                // 2'nin kuvveti olanlar (taş atlası 1024) mipmap'i korur: küçültülen taşlar keskin kalır.
                bool pot = Mathf.IsPowerOfTwo(w) && Mathf.IsPowerOfTwo(h);
                bool mipChanged = false;
                if (!pot && imp.mipmapEnabled) { imp.mipmapEnabled = false; mipChanged = true; }
                var s = imp.GetPlatformTextureSettings("Android");
                if (!mipChanged && s.overridden && s.format == want && s.maxTextureSize >= 2048) { if (EditorUtility.IsDirty(imp)) imp.SaveAndReimport(); continue; }
                s.overridden = true;
                s.format = want;
                s.maxTextureSize = 2048;
                s.compressionQuality = 100;
                imp.SetPlatformTextureSettings(s);
                imp.SaveAndReimport();
                changed++;
            }
            Debug.Log("[KatKat] Android doku bütçesi: " + changed + " doku güncellendi");
        }

        /// <summary>
        /// iOS doku bütçesi: Android'de ASTC'ye ayarlanmış her dokunun iOS ayarı aynı biçim ve boyutla eşlenir
        /// (iPhone 6s+ ASTC destekler; iOS 15 hedefi bunu garanti eder). Ayarsız iOS dokuları ham RGBA paketlenirdi.
        /// </summary>
        [MenuItem("Kat Kat/Build/iOS doku bütçesini uygula")]
        public static void ApplyIosTextureBudget()
        {
            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/KatKat" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                var a = imp.GetPlatformTextureSettings("Android");
                if (!a.overridden) continue;
                var i = imp.GetPlatformTextureSettings("iPhone");
                if (i.overridden && i.format == a.format && i.maxTextureSize == a.maxTextureSize) continue;
                i.overridden = true;
                i.format = a.format;
                i.maxTextureSize = a.maxTextureSize;
                i.compressionQuality = a.compressionQuality;
                imp.SetPlatformTextureSettings(i);
                imp.SaveAndReimport();
                changed++;
            }
            Debug.Log("[KatKat] iOS doku bütçesi: " + changed + " doku güncellendi");
        }

        /// <summary>Mağaza derlemesi: toplu modda çıkışı çağıran yapar (önce imza ayarlarını geri alır).</summary>
        private static bool _deferExit;
        private static BuildOptions _extraOptions = BuildOptions.None;
        private static string[] _extraDefines;
        private static bool _lastOk;

        private static void Build(BuildTarget target, string output)
        {
            ProjectSetup.ApplyPlayerSettings();
            // Unity 6'da Android "Fullscreen Mode" ortak fullScreenMode'dan okunuyor: masaüstü önizlemesinin
            // "pencereli" ayarı Android'de sanal tuşları açık bırakıyordu. Hedefe göre ayrılır.
            PlayerSettings.fullScreenMode = target == BuildTarget.Android ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (target == BuildTarget.iOS) PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            if (target == BuildTarget.iOS || target == BuildTarget.Android) { AdMobSetup.Apply(); NotificationSetup.Apply(); }
            // Google Play derlemesi: Huawei köprüsü Android'e girmez (AppGallery derlemesi kendi açar).
            if (target == BuildTarget.Android && !AppGalleryBuild.Active) AppGalleryBuild.EnsureBridgeOffForGoogle();
            ApplyIcon();
            ApplyTextureBudget();
            if (target == BuildTarget.iOS) ApplyIosTextureBudget();
            _beforeBuild?.Invoke(); // Play mağaza derlemesi: sürüm kodu, imza, AAB
            Directory.CreateDirectory(target == BuildTarget.iOS ? output : Path.GetDirectoryName(output));
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("Build sahnesi yok; önce ProjectSetup.RunAll çalıştır.");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.None | _extraOptions,
                extraScriptingDefines = _extraDefines,
            });
            var s = report.summary;
            Debug.Log($"[KatKat] Build {target}: {s.result}, {s.totalSize} bayt, {s.totalTime}, hata={s.totalErrors} → {output}");
            _lastOk = s.result == BuildResult.Succeeded;
            if (Application.isBatchMode && !_deferExit) EditorApplication.Exit(_lastOk ? 0 : 1);
        }
    }
}
