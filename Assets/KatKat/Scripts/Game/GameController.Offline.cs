using System;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Çevrimdışı oyun kuralları (kullanıcı, 2026-11-22: "oyunu offline oynayanlar reklamsız oynayabilecek. Bu bütün gelir
    /// planımızı alt üst eder" → "1, 2 ve 3'ü uygula. Ama reklamsız paket aldıysa offline oynayabilsin"):
    /// <list type="number">
    /// <item>Ödüller yalnız çevrimiçi: günlük bulmaca ve koleksiyon taşı külçe hediyesi internet ister (reklamlı ödüller zaten
    /// reklam gelmeden verilmez).</item>
    /// <item>Dönüşte reklam: çevrimdışı bölüm kazanıldıysa internete dönünce ilk zaferde bir kez geçiş reklamı (birikmez).</item>
    /// <item>Çevrimdışı sınır: internetsiz en çok <see cref="OfflineLevelLimit"/> bölüm kazanılır; sonra "internete bağlan".</item>
    /// </list>
    /// Reklamsız paket (<c>meta.noAds</c>) alan oyuncuya 2 ve 3 uygulanmaz.
    /// </summary>
    public sealed partial class GameController
    {
        public const int OfflineLevelLimit = 5;

        /// <summary>Testler: bağlantı durumunu sabitler (null → gerçek durum).</summary>
        public static Func<bool> OnlineOverride;

        /// <summary>İnternet var mı (cihazın ağ durumu; sunucu yoklaması değil).</summary>
        public static bool IsOnline => OnlineOverride != null ? OnlineOverride() : Application.internetReachability != NetworkReachability.NotReachable;

        private bool OfflineExempt => _save.Data.meta.noAds;

        /// <summary>Normal bölüm kazanıldı: çevrimdışıysa sayaç ve dönüş reklamı borcu.</summary>
        private void OfflineOnWin()
        {
            if (OfflineExempt || IsOnline) return;
            _save.Data.meta.offlineWins++;
            _save.Data.meta.offlineAdDue = true;
            _save.Save();
        }

        /// <summary>Bölüme giriş: çevrimiçiyse sayaç sıfırlanır; çevrimdışı sınır dolduysa haritada uyarı, false.</summary>
        private bool OfflineGate()
        {
            var m = _save.Data.meta;
            if (OfflineExempt) return true;
            if (IsOnline)
            {
                if (m.offlineWins != 0) { m.offlineWins = 0; _save.Save(); }
                return true;
            }
            if (m.offlineWins < OfflineLevelLimit) return true;
            GameAnalytics.Log("offline_limit", ("wins", m.offlineWins));
            if (!_menu.IsMapShown) ShowLevels();
            _sound.Play(Sfx.Invalid);
            _menu.ShowToast(Loc.F("offline.limit", OfflineLevelLimit), 4f);
            return false;
        }

        /// <summary>Yalnız çevrimiçi ödüller (günlük bulmaca, taş hediyesi): internet yoksa uyarı, false.</summary>
        private bool RequireOnline()
        {
            if (IsOnline) return true;
            _sound.Play(Sfx.Invalid);
            _menu.ShowToast(Loc.T("offline.reward"), 3f);
            return false;
        }

        /// <summary>Dönüş reklamı borcu var ve şimdi gösterilebilir mi (çevrimiçi, reklamsız paket yok).</summary>
        private bool OfflineAdDueNow => !OfflineExempt && _save.Data.meta.offlineAdDue && IsOnline;

        private void ClearOfflineAdDue()
        {
            if (!_save.Data.meta.offlineAdDue) return;
            _save.Data.meta.offlineAdDue = false;
            _save.Save();
        }
    }
}
