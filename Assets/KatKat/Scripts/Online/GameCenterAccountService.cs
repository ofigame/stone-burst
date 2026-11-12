#if KATKAT_ONLINE && UNITY_IOS
using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

namespace KatKat.Game.Online
{
    /// <summary>
    /// iOS hesabı (kullanıcı, 2026-11-09: "ilk ios a yayınlayacağız"). Giriş Apple Game Center (Unity'nin yerleşik
    /// Social API'si, ek eklenti yok), ad Game Center takma adı. Game Center kimliğiyle Firebase Auth'a girer;
    /// sıralama Android ile aynı Firestore tablosu. Bulut kaydı Firestore <c>saves/{uid}</c> (Game Center kayıtlı
    /// oyunları Social API'de yok; Firestore iki platformda aynı çalışır).
    /// Not (Apple): oyuncu açılıştaki Game Center penceresini kapatırsa aynı oturumda pencere bir daha açılmaz;
    /// düğme o durumda başarısız döner ve bildirim Ayarlar → Game Center'ı gösterir (acct.signin.fail).
    /// YALNIZ KATKAT_ONLINE tanımlıyken derlenir. CİHAZDA DENENMEDİ.
    /// </summary>
    public sealed class GameCenterAccountService : IAccountService
    {
        public const string SaveCollection = "saves";

        public AccountState State { get; private set; } = AccountState.SignedOut;
        public string PlayerName { get; private set; }
        public string PlayerId { get; private set; }
        public bool IsTest => false;

        public void SignIn(bool silent, Action<bool> done)
        {
            if (State == AccountState.SignedIn) { done?.Invoke(true); return; }
            State = AccountState.SigningIn;
            Social.localUser.Authenticate(ok =>
            {
                if (!ok || !Social.localUser.authenticated) { State = AccountState.SignedOut; done?.Invoke(false); return; }
                PlayerName = Social.localUser.userName;
                PlayerId = Social.localUser.id;
                LinkFirebase(_ => { State = AccountState.SignedIn; done?.Invoke(true); });
            });
        }

        private static void LinkFirebase(Action<bool> done)
        {
            var auth = FirebaseAuth.DefaultInstance;
            if (auth.CurrentUser != null) { done(true); return; }
            GameCenterAuthProvider.GetCredentialAsync().ContinueWithOnMainThread(c =>
            {
                if (c.IsFaulted || c.IsCanceled) { done(false); return; }
                auth.SignInAndRetrieveDataWithCredentialAsync(c.Result).ContinueWithOnMainThread(t =>
                    done(!t.IsFaulted && !t.IsCanceled && auth.CurrentUser != null));
            });
        }

        private static DocumentReference SaveDoc()
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            return user == null ? null : FirebaseFirestore.DefaultInstance.Collection(SaveCollection).Document(user.UserId);
        }

        public void SaveProgress(string json, Action<bool> done)
        {
            var doc = SaveDoc();
            if (State != AccountState.SignedIn || doc == null || string.IsNullOrEmpty(json)) { done?.Invoke(false); return; }
            var data = new Dictionary<string, object> { { "data", json }, { "updated", FieldValue.ServerTimestamp } };
            doc.SetAsync(data).ContinueWithOnMainThread(t => done?.Invoke(!t.IsFaulted && !t.IsCanceled));
        }

        public void LoadProgress(Action<bool, string> done)
        {
            var doc = SaveDoc();
            if (State != AccountState.SignedIn || doc == null) { done?.Invoke(false, null); return; }
            doc.GetSnapshotAsync().ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted || t.IsCanceled) { done?.Invoke(false, null); return; }
                done?.Invoke(true, t.Result.Exists && t.Result.TryGetValue("data", out string s) ? s : null);
            });
        }

        /// <summary>Sıralama + bulut kaydı + Firebase kullanıcısı silinir; Game Center hesabı Apple'ın, kalır.</summary>
        public void DeleteAccount(Action<bool> done)
        {
            OnlineAccountDeletion.Run(
                give => GameCenterAuthProvider.GetCredentialAsync().ContinueWithOnMainThread(c =>
                    give(c.IsFaulted || c.IsCanceled ? null : c.Result)),
                ok =>
                {
                    if (ok) State = AccountState.SignedOut;
                    done?.Invoke(ok);
                });
        }
    }
}
#endif
