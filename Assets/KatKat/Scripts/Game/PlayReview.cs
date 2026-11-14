using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Google Play uygulama içi puan penceresi (kullanıcı 2026-11-14: "android için de ekle"). Play In-App Review API
    /// (com.google.android.play:review, Editor/PlayReviewDependencies.xml) doğrudan Java köprüsüyle çağrılır:
    /// ReviewManagerFactory.create → requestReviewFlow → launchReviewFlow. Pencerenin gösterilip gösterilmeyeceğine
    /// Google karar verir (kota); sonuç bildirilmez, akış ödül ya da ek soru içermez (Play politikası).
    /// Yalnız Google Play derlemesinde (AppGallery'de Play hizmeti yok); hata olursa sessizce geçilir.
    /// </summary>
    public static class PlayReview
    {
        /// <summary>Bu derlemede gerçek puan penceresi istenebilir mi.</summary>
        public static bool Supported =>
#if UNITY_ANDROID && !UNITY_EDITOR && !KATKAT_APPGALLERY
            true;
#else
            false;
#endif

        public static void Request()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && !KATKAT_APPGALLERY
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                var factory = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory");
                var manager = factory.CallStatic<AndroidJavaObject>("create", activity);
                var request = manager.Call<AndroidJavaObject>("requestReviewFlow");
                request.Call<AndroidJavaObject>("addOnCompleteListener", new Listener(task =>
                {
                    if (!task.Call<bool>("isSuccessful")) { Debug.Log("[KatKat] Play puan akışı alınamadı"); return; }
                    var info = task.Call<AndroidJavaObject>("getResult");
                    var launch = manager.Call<AndroidJavaObject>("launchReviewFlow", activity, info);
                    launch.Call<AndroidJavaObject>("addOnCompleteListener", new Listener(_ => Debug.Log("[KatKat] Play puan akışı bitti")));
                }));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[KatKat] Play puan isteği başarısız: " + e.Message);
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR && !KATKAT_APPGALLERY
        /// <summary>com.google.android.gms.tasks.OnCompleteListener (Play Review 2.x görevleri).</summary>
        private sealed class Listener : AndroidJavaProxy
        {
            private readonly System.Action<AndroidJavaObject> _done;
            public Listener(System.Action<AndroidJavaObject> done) : base("com.google.android.gms.tasks.OnCompleteListener") { _done = done; }

            // Java: void onComplete(Task task)
            public void onComplete(AndroidJavaObject task)
            {
                try { _done(task); }
                catch (System.Exception e) { Debug.LogWarning("[KatKat] Play puan akışı hata: " + e.Message); }
            }
        }
#endif
    }
}
