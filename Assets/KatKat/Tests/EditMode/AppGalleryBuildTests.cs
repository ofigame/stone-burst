using KatKat.EditorTools;
using NUnit.Framework;

namespace KatKat.Tests
{
    /// <summary>AppGallery derlemesi (2026-11-11): üretilen Gradle projesinden Google servisleri çıkar, Huawei Ads girer.</summary>
    public class AppGalleryBuildTests
    {
        private const string LibGradle =
            "apply plugin: 'com.android.library'\n" +
            "dependencies {\n" +
            "    implementation fileTree(dir: 'libs', include: ['*.jar'])\n" +
            "    implementation 'androidx.fragment:fragment:1.7.1' // x\n" +
            "    implementation 'com.google.android.gms:play-services-ads:25.5.0' // x\n" +
            "    implementation 'com.google.android.gms:play-services-games-v2:22.1.0' // x\n" +
            "    implementation 'com.google.firebase:firebase-firestore:26.6.0' // x\n" +
            "    implementation 'com.google.android.ump:user-messaging-platform:4.0.0' // x\n" +
            "    implementation 'com.google.ads.mediation:unity:4.21.0.0' // x\n" +
            "    implementation 'com.unity3d.ads:unity-ads:4.21.0' // x\n" +
            "    implementation(name: 'googlemobileads-unity', ext:'aar')\n" +
            "    implementation project('GoogleMobileAdsPlugin.androidlib')\n" +
            "}\n" +
            "apply from: 'GoogleMobileAdsPlugin.androidlib\\\\packaging_options.gradle'\n";

        [Test]
        public void LauncherGradle_DropsAdMobScripts()
        {
            const string l = "apply plugin: 'com.android.application'\napply from: '../shared/common.gradle'\n" +
                             "apply from: '..\\\\unityLibrary\\\\GoogleMobileAdsPlugin.androidlib\\\\packaging_options.gradle'\n";
            var o = AppGalleryBuild.PatchLauncherGradle(l);
            StringAssert.DoesNotContain("GoogleMobileAdsPlugin", o);
            StringAssert.Contains("../shared/common.gradle", o, "Unity'nin kendi betikleri kalır");
        }

        [Test]
        public void LibraryGradle_StripsGoogle_AddsHuaweiAds()
        {
            var o = AppGalleryBuild.PatchLibraryGradle(LibGradle);
            StringAssert.DoesNotContain("com.google.android.gms", o);
            StringAssert.DoesNotContain("com.google.firebase", o);
            StringAssert.DoesNotContain("user-messaging-platform", o);
            StringAssert.DoesNotContain("com.google.ads.mediation", o);
            StringAssert.DoesNotContain("unity-ads", o);
            StringAssert.DoesNotContain("googlemobileads-unity", o);
            StringAssert.DoesNotContain("GoogleMobileAdsPlugin.androidlib", o);
            StringAssert.Contains("androidx.fragment:fragment", o, "Google dışı bağımlılık kalır");
            StringAssert.Contains("implementation('" + AppGalleryBuild.HuaweiAds + "')", o);
            StringAssert.Contains(AppGalleryBuild.KotlinBom, o, "Kotlin sürüm hizalama");
            Assert.AreEqual(o, AppGalleryBuild.PatchLibraryGradle(o), "ikinci uygulama değiştirmez");
        }

        [Test]
        public void SettingsGradle_AddsHuaweiRepo_InResolutionBlock()
        {
            const string s = "pluginManagement {\n    repositories {\n        google()\n    }\n}\n" +
                             "dependencyResolutionManagement {\n    repositories {\n        google()\n        mavenCentral()\n    }\n}\n";
            var o = AppGalleryBuild.PatchSettingsGradle(s);
            int drm = o.IndexOf("dependencyResolutionManagement");
            Assert.Greater(o.IndexOf(AppGalleryBuild.HuaweiRepo), drm, "depo bağımlılık çözümü bloğunda");
            Assert.AreEqual(o, AppGalleryBuild.PatchSettingsGradle(o), "ikinci uygulama değiştirmez");
        }
    }
}
