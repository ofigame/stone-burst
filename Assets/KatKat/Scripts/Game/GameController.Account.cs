using KatKat.Save;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Hesap akışları (kullanıcı, 2026-11-08; plan docs/v13/KAYIT_PLANI.md):
    /// - Açılışta sessiz giriş ("game center kayıtları yapalım oyun açıldığında"): Play Games v2 bunu kendisi yapar.
    /// - Ayarlar → "İlerlemeyi kaydet": giriş yoksa önce tek dokunuşla giriş, sonra kayıt dosyası buluta.
    /// - Sıralama → "Sıralama için kayıt ol": hızlı kayıt (form yok, tek dokunuş), sonra sıralama yenilenir.
    /// - Otomatik bulut kaydı ve geri yükleme (kullanıcı, 2026-11-09; kurallar <see cref="CloudSync"/>): girişten sonra
    ///   buluttaki kayıt okunur, ilerideyse yerelin yerine geçer (bölüm oynanırken değil, haritaya dönünce). Ondan sonra
    ///   her bölüm kazanılınca ve uygulama arka plana geçince kayıt buluta gider.
    /// Gerçek hizmet bağlanana kadar <see cref="TestAccountService"/>; bildirimler "(test)" etiketli.
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>Gerçek hizmet (Play Games) bağlandığında buraya atanır; null: test hesabı.</summary>
        public static IAccountService AccountProvider;
        private IAccountService _account;

        public IAccountService Account => _account ??= AccountProvider ?? new TestAccountService();

        /// <summary>Bu oturumda buluttaki kayıt okundu mu (okunmadan buluta yazılmaz).</summary>
        private bool _cloudChecked, _cloudChecking, _cloudDirty;
        private SaveData _cloudPending;
        private float _cloudLastUpload = -999f;

        public bool CloudChecked => _cloudChecked;
        public bool CloudRestorePending => _cloudPending != null;

        private void SetupAccount()
        {
            _menu.SaveProgressPressed = SaveProgressToCloud;
            _menu.PrivacyOptionsVisible = () => global::KatKat.Game.Ads.PrivacyOptions.Required;
            _menu.PrivacyOptionsPressed = global::KatKat.Game.Ads.PrivacyOptions.Show;
            _menu.RankSignInPressed = SignInForRanking;
            _menu.DeleteAccountPressed = DeleteAccount;
            // Toplu çalıştırmada (testler) sessiz giriş bile yapılmaz: kayıt durumu testleri etkilemesin.
            // AppGallery: hesap hizmeti yok → sessiz giriş (yerel test hesabı) da yapılmaz; kayıt/bulut düğmeleri gizli.
            // Hesabını silen oyuncuda sessiz giriş yapılmaz (yoksa açılışta hesap kendiliğinden yeniden oluşurdu);
            // oyuncu bir hesap düğmesine kendisi basınca yeniden açılır.
            _menu.AccountsEnabled = () => StoreFlavor.AccountsEnabled;
            if (!Application.isBatchMode && StoreFlavor.AccountsEnabled && !AccountDeletedByPlayer)
                Account.SignIn(true, ok => { if (ok) CloudCheck(null); });
        }

        // ---- Hesap silme (Apple 5.1.1(v); kullanıcı 2026-11-13) -----------------------------------------------------
        private const string AccountDeletedKey = "katkat.account.deleted";

        /// <summary>Oyuncu hesabını sildi ve o günden beri bir hesap düğmesine basmadı: sessiz giriş yok.</summary>
        public static bool AccountDeletedByPlayer
        {
            get => PlayerPrefs.GetInt(AccountDeletedKey, 0) == 1;
            private set { if (value) PlayerPrefs.SetInt(AccountDeletedKey, 1); else PlayerPrefs.DeleteKey(AccountDeletedKey); PlayerPrefs.Save(); }
        }

        /// <summary>Ayarlar → Hesabı sil → onay: sunucudaki kayıtlar ve hesap silinir; cihazdaki ilerleme kalır.</summary>
        private void DeleteAccount()
        {
            Account.DeleteAccount(ok =>
            {
                if (!ok) { _menu.ShowToast(Loc.T("acct.delete.fail")); return; }
                AccountDeletedByPlayer = true;
                _cloudChecked = _cloudChecking = _cloudDirty = false;
                _cloudPending = null;
                _menu.ShowToast(Loc.T("acct.deleted") + TestTag);
            });
        }

        private string TestTag => Account.IsTest ? " (test)" : "";

        private void SaveProgressToCloud()
        {
            AccountDeletedByPlayer = false; // oyuncu hesabı kendisi istedi
            if (Account.State == AccountState.SignedIn) { CloudCheck(DoCloudSave); return; }
            Account.SignIn(false, ok =>
            {
                if (!ok) { _menu.ShowToast(Loc.T("acct.signin.fail")); return; }
                CloudCheck(DoCloudSave);
            });
        }

        private void DoCloudSave()
        {
            // Kontrol başarısızsa (ağ yok) buluttaki daha ileri bir kaydı ezmemek için yazılmaz.
            if (!_cloudChecked) { _menu.ShowToast(Loc.T("acct.save.fail")); return; }
            if (ApplyCloudRestore()) { ShowLevels(); return; } // bulut ileride: kaydetmek yerine geri yüklendi
            _save.Save();
            string json = JsonUtility.ToJson(_save.Data);
            Account.SaveProgress(json, ok =>
            {
                if (ok) { _cloudDirty = false; _cloudLastUpload = Time.unscaledTime; }
                _menu.ShowToast(ok ? Loc.T("acct.saved") + TestTag : Loc.T("acct.save.fail"));
            });
        }

        private void SignInForRanking()
        {
            AccountDeletedByPlayer = false;
            Account.SignIn(false, ok =>
            {
                if (!ok) { _menu.ShowToast(Loc.T("acct.signin.fail")); return; }
                _menu.ShowToast(Loc.F("acct.signed", Account.PlayerName));
                if (_menu.IsRankShown) ShowRanking(_rankScope);
                CloudCheck(null);
            });
        }

        /// <summary>Buluttaki kaydı okur; ilerideyse geri yükler (ya da haritaya dönüşe bırakır). Sonra <paramref name="then"/>.</summary>
        private void CloudCheck(System.Action then)
        {
            if (_cloudChecked) { then?.Invoke(); return; }
            if (_cloudChecking || Account.State != AccountState.SignedIn) return;
            _cloudChecking = true;
            Account.LoadProgress((ok, json) =>
            {
                _cloudChecking = false;
                if (!ok) { then?.Invoke(); return; } // bir sonraki girişte/kayıtta yeniden denenir
                _cloudChecked = true;
                var cloud = CloudSync.Parse(json);
                if (CloudSync.CloudIsAhead(_save.Data, cloud))
                {
                    _cloudPending = cloud;
                    if (CurrentPhase == Phase.Loading) { ApplyCloudRestore(); ShowLevels(); }
                }
                else _cloudDirty = true; // yerel en az bulut kadar ileride: ilk fırsatta buluta yazılır
                then?.Invoke();
                CloudAutoSave(false);
            });
        }

        private float _cloudRetryAt;

        /// <summary>Haritaya dönüş: açılıştaki okuma başarısız olduysa (ağ yok) en çok dakikada bir yeniden dener.</summary>
        private void CloudRetry()
        {
            if (_cloudChecked || _cloudChecking || Time.unscaledTime < _cloudRetryAt) return;
            if (_account == null || _account.State != AccountState.SignedIn) return;
            _cloudRetryAt = Time.unscaledTime + 60f;
            CloudCheck(null);
        }

        /// <summary>Bekleyen geri yüklemeyi uygular (haritada; bölüm sırasında değil). Uygulandıysa true.</summary>
        private bool ApplyCloudRestore()
        {
            if (_cloudPending == null) return false;
            var d = _cloudPending;
            _cloudPending = null;
            if (!CloudSync.CloudIsAhead(_save.Data, d)) return false;
            _save.Replace(d);
            Haptics.Enabled = _save.Data.vibration;
            CreateDaily(); // günlük hizmet eski meta nesnesini tutuyordu
            _cloudDirty = false;
            _menu.ShowToast(Loc.T("acct.restored"));
            return true;
        }

        /// <summary>Kazanılan bölüm ya da arka plana geçiş: giriş varsa ve kontrol bittiyse kayıt sessizce buluta.</summary>
        private void CloudAutoSave(bool force)
        {
            // Bekleyen geri yükleme varken yazılmaz: geride kalan yerel kayıt buluttakini ezmesin.
            if (!_cloudChecked || !_cloudDirty || _cloudPending != null || Account.State != AccountState.SignedIn) return;
            if (!force && Time.unscaledTime - _cloudLastUpload < CloudSync.MinUploadGap) return;
            _cloudLastUpload = Time.unscaledTime;
            string json = JsonUtility.ToJson(_save.Data);
            _cloudDirty = false;
            Account.SaveProgress(json, ok => { if (!ok) _cloudDirty = true; });
        }

        /// <summary>Bölüm kazanıldı: kayıt değişti, buluta gönderilir.</summary>
        private void CloudOnWon()
        {
            _cloudDirty = true;
            CloudAutoSave(false);
        }

        /// <summary>Test: sahte hesap hizmeti (bulut kaydı denemesi; gerçek PlayerPrefs'e yazılmasın).</summary>
        public void SetAccountForTest(IAccountService account)
        {
            _account = account;
            _cloudChecked = _cloudChecking = _cloudDirty = false;
            _cloudPending = null;
            _cloudLastUpload = -999f;
        }

        /// <summary>Test: Ayarlar'daki silme onayı (hesap silme akışı).</summary>
        public void DeleteAccountForTest() => DeleteAccount();

        /// <summary>Test: silme işaretini temizler (testler birbirini etkilemesin).</summary>
        public static void ClearAccountDeletedForTest() => AccountDeletedByPlayer = false;

        /// <summary>Test: uygulama arka plana geçti.</summary>
        public void PauseForTest() => OnApplicationPause(true);

        /// <summary>Test: hesabı bağlı say ve bulut kontrolünü çalıştır (toplu çalıştırmada sessiz giriş yok).</summary>
        public void CloudSignInForTest(System.Action done) => Account.SignIn(false, ok => { if (ok) CloudCheck(done); else done?.Invoke(); });
    }
}
