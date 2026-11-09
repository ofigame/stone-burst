using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Hesap akışları (kullanıcı, 2026-11-08; plan docs/v13/KAYIT_PLANI.md):
    /// - Açılışta sessiz giriş ("game center kayıtları yapalım oyun açıldığında"): Play Games v2 bunu kendisi yapar.
    /// - Ayarlar → "İlerlemeyi kaydet": giriş yoksa önce tek dokunuşla giriş, sonra kayıt dosyası buluta.
    /// - Sıralama → "Sıralama için kayıt ol": hızlı kayıt (form yok, tek dokunuş), sonra sıralama yenilenir.
    /// Gerçek hizmet bağlanana kadar <see cref="TestAccountService"/>; bildirimler "(test)" etiketli.
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>Gerçek hizmet (Play Games) bağlandığında buraya atanır; null: test hesabı.</summary>
        public static IAccountService AccountProvider;
        private IAccountService _account;

        public IAccountService Account => _account ??= AccountProvider ?? new TestAccountService();

        private void SetupAccount()
        {
            _menu.SaveProgressPressed = SaveProgressToCloud;
            _menu.PrivacyOptionsVisible = () => global::KatKat.Game.Ads.PrivacyOptions.Required;
            _menu.PrivacyOptionsPressed = global::KatKat.Game.Ads.PrivacyOptions.Show;
            _menu.RankSignInPressed = SignInForRanking;
            // Toplu çalıştırmada (testler) sessiz giriş bile yapılmaz: kayıt durumu testleri etkilemesin.
            if (!Application.isBatchMode) Account.SignIn(true, null);
        }

        private string TestTag => Account.IsTest ? " (test)" : "";

        private void SaveProgressToCloud()
        {
            if (Account.State == AccountState.SignedIn) { DoCloudSave(); return; }
            Account.SignIn(false, ok =>
            {
                if (!ok) { _menu.ShowToast(Loc.T("acct.signin.fail")); return; }
                DoCloudSave();
            });
        }

        private void DoCloudSave()
        {
            _save.Save();
            string json = JsonUtility.ToJson(_save.Data);
            Account.SaveProgress(json, ok => _menu.ShowToast(ok ? Loc.T("acct.saved") + TestTag : Loc.T("acct.save.fail")));
        }

        private void SignInForRanking()
        {
            Account.SignIn(false, ok =>
            {
                if (!ok) { _menu.ShowToast(Loc.T("acct.signin.fail")); return; }
                _menu.ShowToast(Loc.F("acct.signed", Account.PlayerName));
                if (_menu.IsRankShown) ShowRanking(_rankScope);
            });
        }
    }
}
