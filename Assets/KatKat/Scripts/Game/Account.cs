using System;
using UnityEngine;

namespace KatKat.Game
{
    public enum AccountState { SignedOut, SigningIn, SignedIn }

    /// <summary>Son giriş hatasının kısa kodu (ör. Play Games "Canceled"/"InternalError"); bildirimde parantez içinde gösterilir.</summary>
    public static class AccountDiagnostics
    {
        public static string LastSignInError;
    }

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
        /// <summary>
        /// Hesabı ve sunucudaki verileri kalıcı olarak siler (Apple 5.1.1(v); kullanıcı 2026-11-13): sıralama kaydı,
        /// bulut kaydı ve Firebase kullanıcısı. Cihazdaki yerel ilerleme kalır. Başarıda oturum kapanır.
        /// </summary>
        void DeleteAccount(Action<bool> done);
    }

    /// <summary>
    /// Adını oyuncunun kendisinin seçtiği hesap (AppGallery anonim REST hesabı; kullanıcı 2026-11-19: "huaweide
    /// sıralamaya üye olduğunda ismini kendi versin"). Play Games ve Game Center adı kendi hizmetinden gelir.
    /// </summary>
    public interface IRenamableAccount
    {
        /// <summary>Oyuncu bir ad seçti mi (seçmediyse "Oyuncu 1234" kullanılır).</summary>
        bool HasChosenName { get; }
        /// <summary>Adı kalıcı olarak değiştirir; <paramref name="name"/> <see cref="PlayerNameRules.Clean"/>'den geçmiş olmalı.</summary>
        void Rename(string name);
    }

    /// <summary>
    /// Oyuncunun yazdığı ad: baştaki/sondaki ve art arda boşluklar atılır; yalnız harf, rakam, boşluk ve . _ -
    /// kalır (emoji ve denetim karakterleri düşer). 3–16 karakter. Kaba sözcük içeren ad kabul edilmez.
    /// </summary>
    public static class PlayerNameRules
    {
        public const int MinLength = 3, MaxLength = 16;

        // Kısa, bilinçli olarak dar liste (tr/en/ar en yaygınlar ve hizmet adı taklidi). Kısa sözcükler (≤4) yalnız tam
        // sözcük olarak ("Klasik", "Iraq" geçer), uzunlar ayraçsız adın içinde de aranır ("xFuckerx" geçmez).
        private static readonly string[] BlockedWords = { "amk", "aq", "piç", "pic", "sik", "göt", "got", "oç", "oc", "dick", "cunt", "porn", "nazi", "كس", "زب" };
        private static readonly string[] BlockedParts =
        {
            "orospu", "yarrak", "ibne", "kahpe", "pezevenk", "gavat", "siktir", "sikis", "sikiş", "amcık", "amcik",
            "fuck", "shit", "bitch", "pussy", "nigger", "nigga", "whore", "slut", "hitler",
            "شرموط", "منيوك", "قحبة",
            "admin", "ofigame", "moderator",
        };

        public static string Clean(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new System.Text.StringBuilder(raw.Length);
            bool space = false;
            foreach (char ch in raw)
            {
                if (char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-')
                {
                    if (space && sb.Length > 0) sb.Append(' ');
                    sb.Append(ch);
                    space = false;
                }
                else if (char.IsWhiteSpace(ch)) space = true;
            }
            var s = sb.ToString();
            return s.Length > MaxLength ? s.Substring(0, MaxLength).TrimEnd() : s;
        }

        /// <summary>Temizlenmiş ad için hata anahtarı (yerelleştirme); uygunsa null.</summary>
        public static string Problem(string cleaned)
        {
            if (string.IsNullOrEmpty(cleaned) || cleaned.Length < MinLength) return "acct.name.short";
            string low = cleaned.ToLowerInvariant();
            var words = low.Split(new[] { ' ', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in words)
                foreach (var b in BlockedWords)
                    if (w == b) return "acct.name.bad";
            string compact = string.Concat(words);
            foreach (var b in BlockedParts)
                if (compact.Contains(b)) return "acct.name.bad";
            return null;
        }
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

        public void DeleteAccount(Action<bool> done)
        {
            PlayerPrefs.DeleteKey(IdKey);
            PlayerPrefs.DeleteKey(CloudKey);
            PlayerPrefs.Save();
            PlayerId = PlayerName = null;
            State = AccountState.SignedOut;
            done?.Invoke(true);
        }
    }
}
