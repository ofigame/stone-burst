using System;
using UnityEngine;

namespace KatKat.Game
{
    public enum AccountState { SignedOut, SigningIn, SignedIn }

    /// <summary>
    /// Oyuncu hesabı (kullanıcı, 2026-11-08: "sıralamanın oluşabilmesi için kullanıcı kayıtları"; plan
    /// docs/v13/KAYIT_PLANI.md). Android'de hedef Google Play Games (v2: açılışta otomatik giriş, ad ve kimlik Play'den,
    /// form yok) + kayıtlı oyun (bulut ilerleme). Gerçek eklenti ve hesaplar kullanıcı onayıyla gelene kadar
    /// <see cref="TestAccountService"/> çalışır ve ekranda "(test)" diye etiketlidir.
    /// </summary>
    public interface IAccountService
    {
        AccountState State { get; }
        /// <summary>Görünen oyuncu adı (giriş yoksa null).</summary>
        string PlayerName { get; }
        /// <summary>Kalıcı oyuncu kimliği (sıralama kaydı anahtarı; giriş yoksa null).</summary>
        string PlayerId { get; }
        /// <summary>Gerçek hizmet değilse true (arayüz etiketler).</summary>
        bool IsTest { get; }
        /// <param name="silent">Açılışta sessiz deneme (pencere açmaz); false: oyuncu düğmeye bastı.</param>
        void SignIn(bool silent, Action<bool> done);
        /// <summary>Kayıt dosyasını buluta yazar.</summary>
        void SaveProgress(string json, Action<bool> done);
        /// <summary>Buluttaki kaydı okur (yoksa null).</summary>
        void LoadProgress(Action<bool, string> done);
    }

    /// <summary>
    /// Yerel test hesabı: sessiz girişte daha önce kayıt olunduysa oturum açılır; düğmeyle anında "Oyuncu 1234 (test)"
    /// oluşturulur. "Bulut" kaydı PlayerPrefs'e yazar (yalnız akışı denemek için; cihaz değişince taşınmaz).
    /// </summary>
    public sealed class TestAccountService : IAccountService
    {
        private const string IdKey = "katkat.account.test.id", CloudKey = "katkat.account.test.cloud";

        public AccountState State { get; private set; } = AccountState.SignedOut;
        public string PlayerName { get; private set; }
        public string PlayerId { get; private set; }
        public bool IsTest => true;

        public void SignIn(bool silent, Action<bool> done)
        {
            string id = PlayerPrefs.GetString(IdKey, "");
            if (string.IsNullOrEmpty(id))
            {
                if (silent) { done?.Invoke(false); return; }
                id = "test-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                PlayerPrefs.SetString(IdKey, id);
                PlayerPrefs.Save();
            }
            PlayerId = id;
            int n = Math.Abs(id.GetHashCode()) % 9000 + 1000;
            PlayerName = "Oyuncu " + n + " (test)";
            State = AccountState.SignedIn;
            done?.Invoke(true);
        }

        public void SaveProgress(string json, Action<bool> done)
        {
            if (State != AccountState.SignedIn || string.IsNullOrEmpty(json)) { done?.Invoke(false); return; }
            PlayerPrefs.SetString(CloudKey, json);
            PlayerPrefs.Save();
            done?.Invoke(true);
        }

        public void LoadProgress(Action<bool, string> done)
        {
            if (State != AccountState.SignedIn) { done?.Invoke(false, null); return; }
            string s = PlayerPrefs.GetString(CloudKey, "");
            done?.Invoke(true, string.IsNullOrEmpty(s) ? null : s);
        }
    }
}
