#if KATKAT_ONLINE
using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;

namespace KatKat.Game.Online
{
    /// <summary>
    /// Hesap silme (Apple 5.1.1(v); kullanıcı 2026-11-13: "Evet, ekle"). Sıra:
    /// 1) mümkünse yeniden kimlik doğrulama (Firebase kullanıcı silmeyi "yakın zamanda giriş" ister),
    /// 2) Firestore <c>scores/{uid}</c> ve <c>saves/{uid}</c> belgelerini siler (kurallar sahibine silme izni verir),
    /// 3) Firebase kullanıcısını siler ve oturumu kapatır.
    /// Belgeler ve kullanıcı silinirse başarılı; bir adım başarısızsa false (oyuncu tekrar deneyebilir; silinen belgeyi
    /// yeniden silmek sorun değil). Game Center / Play Games hesabının kendisi Apple'ın / Google'ın, silinmez.
    /// YALNIZ KATKAT_ONLINE tanımlıyken derlenir. CİHAZDA DENENMEDİ.
    /// </summary>
    internal static class OnlineAccountDeletion
    {
        /// <param name="credential">Yeniden doğrulama kimliğini üretir (yoksa ya da başarısızsa null verir).</param>
        public static void Run(Action<Action<Credential>> credential, Action<bool> done)
        {
            var auth = FirebaseAuth.DefaultInstance;
            var user = auth.CurrentUser;
            if (user == null) { done?.Invoke(true); return; } // sunucuda bu oturuma bağlı kayıt yok
            credential(cred =>
            {
                if (cred == null) { DeleteData(auth, user, done); return; }
                user.ReauthenticateAsync(cred).ContinueWithOnMainThread(_ => DeleteData(auth, user, done));
            });
        }

        private static void DeleteData(FirebaseAuth auth, FirebaseUser user, Action<bool> done)
        {
            var db = FirebaseFirestore.DefaultInstance;
            string uid = user.UserId;
            var scores = db.Collection(FirestoreLeaderboard.Collection).Document(uid).DeleteAsync();
            var saves = db.Collection(SaveCollection).Document(uid).DeleteAsync();
            Task.WhenAll(scores, saves).ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted || t.IsCanceled) { done?.Invoke(false); return; }
                user.DeleteAsync().ContinueWithOnMainThread(d =>
                {
                    bool ok = !d.IsFaulted && !d.IsCanceled;
                    if (ok) auth.SignOut();
                    done?.Invoke(ok);
                });
            });
        }

        /// <summary>iOS bulut kaydı koleksiyonu (Android'de boş; silmek zararsız).</summary>
        public const string SaveCollection = "saves";
    }
}
#endif
