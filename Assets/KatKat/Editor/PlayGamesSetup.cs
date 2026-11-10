using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Google Play Games v2 kurulumu (2026-11-10). Eklentinin "Android setup" penceresinin yaptığını koddan yapar:
    /// GameInfo.cs ve eklentinin AndroidManifest'ine uygulama kimliğini ve web (oyun sunucusu) istemci kimliğini yazar.
    /// Kimlikler gizli değildir: Play Console → Play Oyun Hizmetleri → Yapılandırma.
    /// Eklenti `Assets/GooglePlayGames` (2.3.0); içindeki eski EDM (1.2.182) projeye alınmadı, Google EDM 1.2.190 UPM'de.
    /// </summary>
    public static class PlayGamesSetup
    {
        /// <summary>Play Oyun Hizmetleri proje kimliği (stone-burst bulut projesi).</summary>
        public const string AppId = "216600100064";
        /// <summary>"Stone Burst game server (Play Games)" web OAuth istemcisi: Firebase Auth için sunucu erişim kodu.</summary>
        public const string WebClientId = "216600100064-ui1c4pooqecssl9fcokbuns90uvndg90.apps.googleusercontent.com";

        [MenuItem("Kat Kat/Kurulum/Play Games ayarları")]
        public static void Apply()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("GooglePlayGames.Editor.GPGSAndroidSetupUI"))
                .FirstOrDefault(t => t != null);
            var method = type?.GetMethod("PerformSetup", new[] { typeof(string), typeof(string), typeof(string) });
            if (method == null) { Debug.LogWarning("[KatKat] Play Games eklentisi yok; kurulum atlandı"); return; }
            bool ok = (bool)method.Invoke(null, new object[] { WebClientId, AppId, null });
            Debug.Log("[KatKat] Play Games kurulumu: " + (ok ? "tamam" : "BAŞARISIZ"));
            if (!ok) throw new InvalidOperationException("Play Games kurulumu başarısız");
        }
    }
}
