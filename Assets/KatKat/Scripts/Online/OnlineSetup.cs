#if KATKAT_ONLINE && (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using Firebase;
using Firebase.Extensions;
using UnityEngine;

namespace KatKat.Game.Online
{
    /// <summary>
    /// Gerçek hizmetleri sahne yüklenmeden bağlar: hesap iOS'ta Game Center, Android'de Play Games; sıralama iki
    /// platformda Firestore. Firebase bağımlılıkları eksikse sıralama bağlanmaz (ekranda "bağlı değil").
    /// YALNIZ KATKAT_ONLINE tanımlıyken ve cihazda derlenir. CİHAZDA DENENMEDİ.
    /// </summary>
    internal static class OnlineSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
#if UNITY_IOS
            var account = new GameCenterAccountService();
#else
            var account = new PlayGamesAccountService();
#endif
            GameController.AccountProvider = account;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(t =>
            {
                if (t.Result == DependencyStatus.Available)
                    GameController.LeaderboardProvider = new FirestoreLeaderboard(() => account.PlayerName);
                else Debug.LogWarning("KatKat: Firebase kullanılamıyor: " + t.Result);
            });
        }
    }
}
#endif
