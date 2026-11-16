using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Canlı maç (kullanıcı 2026-11-13): anasayfadaki girişten açılır; rakip şimdilik bot. Ekran açıkken ana oyunun tahtası,
    /// HUD'u ve menüsü gizlenir, girdiyi ekran kendisi okur; çıkınca haritaya dönülür.
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>Anasayfada canlı maç girişi. AppGallery canlı sürümünde kapalı (kullanıcı 2026-11-16: "huawei için canlı
        /// versiyonu hazırlayalım. Gerçek reklamlı. Canlı maç kapalı"). Mağaza çekimleri de kapatır (CaptureStore).</summary>
#if KATKAT_APPGALLERY
        public static bool LiveMatchEnabled = false;
#else
        public static bool LiveMatchEnabled = true;
#endif

        private LiveMatchView _live;
        public LiveMatchView Live => _live;
        public bool LiveOpen => _live != null;

        public void ShowLive()
        {
            if (_live != null) return;
            HeartForfeit();
            _menu.Hide();
            _hud.HidePanel();
            _hud.gameObject.SetActive(false);
            _board.gameObject.SetActive(false);
            _backdrop.gameObject.SetActive(false);
            SetPhase(Phase.Loading);
            _live = LiveMatchView.Create(_cam, transform);
            _live.Sound = _sound;
            _live.Exit += CloseLive;
            GameAnalytics.Log("live_open");
        }

        public void CloseLive()
        {
            if (_live == null) return;
            var m = _live.Match;
            if (m != null) GameAnalytics.Log("live_close", ("over", m.IsOver ? 1 : 0), ("you", m.Scores[0]), ("bot", m.Scores[1]));
            Destroy(_live.gameObject);
            _live = null;
            _hud.gameObject.SetActive(true);
            _board.gameObject.SetActive(true);
            _backdrop.gameObject.SetActive(true);
            ShowLevels();
        }
    }
}
