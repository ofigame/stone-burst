#if KATKAT_ONLINE && UNITY_ANDROID
using System;
using System.Text;
using Firebase.Auth;
using Firebase.Extensions;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;

namespace KatKat.Game.Online
{
    /// <summary>
    /// Gerçek hesap (kullanıcı kararları 2026-11-09: sıralama Firebase, oyuncu adı Play Games adı).
    /// Akış: Play Games v2 girişi (açılışta sessiz, düğmeyle elle) → sunucu erişim kodu → Firebase Auth (Play Games
    /// sağlayıcısı) → Firestore sıralaması bu Firebase kimliğiyle yazar. İlerleme Play Games Kayıtlı Oyunlar'a yazılır.
    /// YALNIZ KATKAT_ONLINE tanımlıyken derlenir (eklentiler + google-services.json kurulunca açılır).
    /// CİHAZDA DENENMEDİ.
    /// </summary>
    public sealed class PlayGamesAccountService : IAccountService
    {
        private const string SaveName = "katkat_save";

        public AccountState State { get; private set; } = AccountState.SignedOut;
        public string PlayerName { get; private set; }
        public string PlayerId { get; private set; }
        public bool IsTest => false;

        public PlayGamesAccountService() => PlayGamesPlatform.Activate();

        public void SignIn(bool silent, Action<bool> done)
        {
            if (State == AccountState.SignedIn) { done?.Invoke(true); return; }
            State = AccountState.SigningIn;
            Action<SignInStatus> onStatus = status =>
            {
                if (status != SignInStatus.Success) { State = AccountState.SignedOut; done?.Invoke(false); return; }
                PlayerName = PlayGamesPlatform.Instance.GetUserDisplayName();
                PlayerId = PlayGamesPlatform.Instance.GetUserId();
                LinkFirebase(ok =>
                {
                    // Firebase olmadan da kayıtlı oyun çalışır; sıralama "bağlı değil" görünür.
                    State = AccountState.SignedIn;
                    done?.Invoke(true);
                });
            };
            if (silent) PlayGamesPlatform.Instance.Authenticate(onStatus);
            else PlayGamesPlatform.Instance.ManuallyAuthenticate(onStatus);
        }

        private static void LinkFirebase(Action<bool> done)
        {
            var auth = FirebaseAuth.DefaultInstance;
            if (auth.CurrentUser != null) { done(true); return; }
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, code =>
            {
                if (string.IsNullOrEmpty(code)) { done(false); return; }
                var cred = PlayGamesAuthProvider.GetCredential(code);
                auth.SignInAndRetrieveDataWithCredentialAsync(cred).ContinueWithOnMainThread(t =>
                    done(!t.IsFaulted && !t.IsCanceled && auth.CurrentUser != null));
            });
        }

        public void SaveProgress(string json, Action<bool> done)
        {
            if (State != AccountState.SignedIn || string.IsNullOrEmpty(json)) { done?.Invoke(false); return; }
            var sg = PlayGamesPlatform.Instance.SavedGame;
            sg.OpenWithAutomaticConflictResolution(SaveName, DataSource.ReadNetworkOnly,
                ConflictResolutionStrategy.UseLongestPlaytime, (status, meta) =>
                {
                    if (status != SavedGameRequestStatus.Success) { done?.Invoke(false); return; }
                    var update = new SavedGameMetadataUpdate.Builder().WithUpdatedDescription("Stone Burst").Build();
                    sg.CommitUpdate(meta, update, Encoding.UTF8.GetBytes(json),
                        (st, _) => done?.Invoke(st == SavedGameRequestStatus.Success));
                });
        }

        public void LoadProgress(Action<bool, string> done)
        {
            if (State != AccountState.SignedIn) { done?.Invoke(false, null); return; }
            var sg = PlayGamesPlatform.Instance.SavedGame;
            sg.OpenWithAutomaticConflictResolution(SaveName, DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime, (status, meta) =>
                {
                    if (status != SavedGameRequestStatus.Success) { done?.Invoke(false, null); return; }
                    sg.ReadBinaryData(meta, (st, data) =>
                    {
                        if (st != SavedGameRequestStatus.Success) { done?.Invoke(false, null); return; }
                        done?.Invoke(true, data == null || data.Length == 0 ? null : Encoding.UTF8.GetString(data));
                    });
                });
        }
    }
}
#endif
