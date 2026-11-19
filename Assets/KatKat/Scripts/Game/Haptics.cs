using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Kısa titreşim. Android'de Vibrator ile milisaniye süreli; iOS'ta dokunsal geri bildirim; diğer platformlarda sessiz.
    /// Temel bilgi yalnızca titreşimle verilmez; kapalıyken oyun aynı şekilde anlaşılır.
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static bool _failed;
#endif
#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void KatKat_Haptic(int strength);
#endif

        public static void Light() => Pulse(14);
        /// <summary>Taş akışı varış "tıkı" (çok kısa; 2026-11-19).</summary>
        public static void Tick() => Pulse(8);
        /// <summary>Taş akışı yükselişi: kombo kademesine göre hafif (0) → orta (1) → güçlü (2).</summary>
        public static void Level(int level) => Pulse(level <= 0 ? 12 : level == 1 ? 24 : 40);
        public static void Medium() => Pulse(28);
        public static void Strong() => Pulse(40);

        private static void Pulse(long ms)
        {
            if (!Enabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            // iOS: UIImpactFeedbackGenerator (Assets/Plugins/iOS/KatKatNative.mm); süre yerine hafif/orta.
            KatKat_Haptic(ms < 20 ? 0 : ms < 36 ? 1 : 2);
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_failed) return;
            try
            {
                if (_vibrator == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                _vibrator.Call("vibrate", ms);
            }
            catch (AndroidJavaException e)
            {
                _failed = true;
                Debug.LogWarning("[KatKat] Titreşim kullanılamıyor: " + e.Message);
            }
#endif
        }

#if UNITY_ANDROID
        // Unity, VIBRATE iznini bu çağrının kodda bulunmasına bakarak manifest'e ekler. Çağrılmaz.
        private static void EnsureVibratePermission() => Handheld.Vibrate();
#endif
    }
}
