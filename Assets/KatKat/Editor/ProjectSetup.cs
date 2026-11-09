using System.Collections.Generic;
using System.IO;
using KatKat.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Proje ayarlarını ve sahneleri koddan, tekrarlanabilir biçimde kurar.
    /// Batch: Unity -batchmode -quit -projectPath . -executeMethod KatKat.EditorTools.ProjectSetup.RunAll
    /// </summary>
    public static class ProjectSetup
    {
        public const string ScenesDir = "Assets/KatKat/Scenes";
        public const string BootScene = ScenesDir + "/Boot.unity";
        public const string PeelLabScene = ScenesDir + "/PeelLab.unity";
        public const string GameScene = ScenesDir + "/Game.unity";
        /// <summary>Paket kimliği (kullanıcı, 2026-11-09: "com.ofigame.stoneburst yap ios ve android için"). Önce com.ofigame.katkat idi.</summary>
        public const string AndroidId = "com.ofigame.stoneburst";
        /// <summary>iOS paket kimliği (kullanıcı, 2026-11-09: marka StoneBurst; önce com.ofigame.trikat idi). Android kimliği değişmedi.</summary>
        public const string IosId = AndroidId;

        public static readonly Color Ivory = new Color32(0xFB, 0xF6, 0xEE, 0xFF);

        [MenuItem("Kat Kat/Kurulum/Hepsini Uygula")]
        public static void RunAll()
        {
            ApplyPlayerSettings();
            CreateBootScene();
            CreatePeelLabScene();
            CreateGameScene();
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[KatKat] ProjectSetup.RunAll tamam.");
        }

        [MenuItem("Kat Kat/Kurulum/Player Ayarları")]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "OFIGAME";
            PlayerSettings.productName = "Stone Burst";
            PlayerSettings.bundleVersion = "1.0.0"; // ilk mağaza sürümü (2026-11-09)
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AndroidId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, IosId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            ApplyIosSettings();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            // ARMv7 + ARM64: 32-bit Android tabletler (ör. bazı Huawei MatePad) yalnızca ARM64 APK'yı
            // "CPU_ABI uyumsuz" diye reddediyor. Google Play 64-bit şartını ARM64 kopya karşılar.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 1;
            // Masaüstü önizleme için dikey 9:16 pencere.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;
            // Android (kullanıcı, Redmi: "alt kısımda geri/uygulamalar tuşları çıkıyor"): pencereli ayar Android'e de
            // uygulanıyordu. Telefonda sürükleyince geri gelen tam ekran (gezinme çubuğu gizli); çentik alanına da çizilir
            // (arayüz SafeAreaFitter ile güvenli alanda kalır).
            // Unity 6.6: startInFullscreen kaldırıldı; görünecek sistem çubukları requestedVisibleInsets ile seçilir.
            // Varsayılan gezinme çubuğunu görünür tutuyordu (Redmi'de tuşlar kapanmıyordu). Hiçbiri görünmez;
            // kenardan kaydırınca geçici görünür (systemBarsBehavior = ShowTransientBarsBySwipe).
            PlayerSettings.Android.requestedVisibleInsets = 0;
            PlayerSettings.Android.systemBarsBehavior = AndroidSystemBarsBehavior.ShowTransientBarsBySwipe;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            // Açılışta Unity logosu yok (kullanıcı: "logomuzun önünde Unity'nin logosu var").
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }

        /// <summary>Yayının ilk platformu iOS (kullanıcı, 2026-11-09). Plan: docs/v13/IOS_PLANI.md.</summary>
        public const string IosTargetVersion = "15.0";

        /// <summary>
        /// iOS ayarları (kullanıcı, 2026-11-09: "ilk ios a yayınlayacağız"). Derleme GitHub Actions'ta macOS'ta yapılır;
        /// imzalama orada (fastlane), bu yüzden Unity otomatik imzalamayı kapatır. Ekip kimliği ortam değişkeninden
        /// (KATKAT_IOS_TEAM) gelir, yoksa boş kalır.
        /// </summary>
        public static void ApplyIosSettings()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1); // ARM64
            PlayerSettings.iOS.targetOSVersionString = IosTargetVersion;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.requiresFullScreen = true;      // iPad'de bölünmüş ekran yok: oyun yalnız dikey
            PlayerSettings.iOS.hideHomeButton = false;
            PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.BottomEdge;
            PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Suspend;
            PlayerSettings.iOS.statusBarStyle = iOSStatusBarStyle.LightContent;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            string team = System.Environment.GetEnvironmentVariable("KATKAT_IOS_TEAM");
            if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;
            // Yapı numarası: CI'da her derlemede artan sayı (KATKAT_BUILD_NUMBER), yerelde 1.
            string build = System.Environment.GetEnvironmentVariable("KATKAT_BUILD_NUMBER");
            PlayerSettings.iOS.buildNumber = string.IsNullOrEmpty(build) ? "1" : build;
            // Yalnız dikey (Android ile aynı).
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            // Açılış: Unity'nin varsayılan beyaz storyboard'u yerine koyu zemin; logo oyunun kendi açılışında.
            PlayerSettings.iOS.SetiPhoneLaunchScreenType(iOSLaunchScreenType.Default);
            PlayerSettings.SplashScreen.backgroundColor = new Color32(0x0B, 0x2A, 0x55, 0xFF);
        }

        [MenuItem("Kat Kat/Kurulum/Boot Sahnesi")]
        public static void CreateBootScene()
        {
            Directory.CreateDirectory(ScenesDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            var boot = new GameObject("Bootstrap");
            boot.AddComponent<Bootstrap>();
            EditorSceneManager.SaveScene(scene, BootScene);
        }

        [MenuItem("Kat Kat/Kurulum/PeelLab Sahnesi")]
        public static void CreatePeelLabScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            new GameObject("PeelLab").AddComponent<KatKat.View.PeelLab>();
            EditorSceneManager.SaveScene(scene, PeelLabScene);
        }

        [MenuItem("Kat Kat/Kurulum/Game Sahnesi")]
        public static void CreateGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            new GameObject("Game").AddComponent<KatKat.Game.GameController>();
            EditorSceneManager.SaveScene(scene, GameScene);
        }

        public static Camera CreateCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Ivory;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            return cam;
        }

        public static void RegisterBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var path in new[] { BootScene, GameScene, PeelLabScene })
            {
                if (File.Exists(path)) list.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
