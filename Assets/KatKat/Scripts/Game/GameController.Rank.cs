using System;
using System.Globalization;
using KatKat.Meta;
using KatKat.UI;
using KatKat.View;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// v12 Sıralama ekranı denetimi (docs/v12/siralama, PLAN.md §4). Puan: <see cref="RankScore"/> (kullanıcı
    /// kararı %50 ZPE + %30 ulaşılan bölüm + %20 yıldız, 0–1000, giriş eşiği 20 bölüm). Sunucu (Firebase) henüz yok:
    /// varsayılan sağlayıcı <see cref="NotConnectedLeaderboard"/> (ekranda "bağlı değil"); örnek veri yalnız
    /// <see cref="DevLeaderboardSample"/> açıkken (editör/geliştirme) ve ekranda etiketli.
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>Gerçek sunucu sağlayıcısı geldiğinde buraya bağlanır (null: bağlı değil).</summary>
        public static ILeaderboardProvider LeaderboardProvider;
        /// <summary>YALNIZ GELİŞTİRME: ekran düzeni için etiketli örnek veri.</summary>
        public static bool DevLeaderboardSample;
        /// <summary>Test/yakalama: cihaz ülkesi yerine (ISO alfa-2; "" = ülke yok).</summary>
        public static string CountryOverride;

        private static readonly DevSampleLeaderboard DevSample = new DevSampleLeaderboard();
        private static readonly NotConnectedLeaderboard NotConnected = new NotConnectedLeaderboard();
        private LeaderboardScope _rankScope = LeaderboardScope.World;
        private int _rankRequest;

        private ILeaderboardProvider Provider => DevLeaderboardSample ? DevSample : LeaderboardProvider ?? NotConnected;

        public void ShowRanking() => ShowRanking(LeaderboardScope.World);

        public void ShowRanking(LeaderboardScope scope)
        {
            AbandonDailyIfOpen();
            _hud.HidePanel();
            _rankScope = scope;
            string code = DeviceCountry();
            var self = SelfEntry();
            int completed = _save.Data.completed.Count;
            var model = new MenuView.RankScreenModel
            {
                Scope = scope, CountryCode = code, CountryName = CountryName(code), Loading = true, Self = self,
                Completed = completed, MinLevels = RankScore.MinLevels, SelfAvatar = SpecialArt.Mascot(SpecialArt.Mood.Happy),
            };
            _menu.ShowRanking(model, ShowLevels, _menu.ShowRankInfo);
            _music.Enter();
            int req = ++_rankRequest;
            Provider.Fetch(scope, code, self, r =>
            {
                if (req != _rankRequest || !_menu.IsRankShown) return; // eski istek ya da ekrandan çıkıldı
                model.Loading = false;
                model.Result = r;
                _menu.ShowRanking(model, ShowLevels, _menu.ShowRankInfo);
            });
        }

        /// <summary>Oyuncunun yerel değerleri (sıra sunucudan). Uygun değilse puan 0 (ekranda "—").</summary>
        private LeaderboardEntry SelfEntry()
        {
            int completed = _save.Data.completed.Count;
            int reached = Mathf.Clamp(_save.Data.highestUnlocked + 1, 1, Mathf.Max(1, _catalog.Count));
            int zpe = CurrentZpe().Count > 0 ? CurrentZpe().Score : 0;
            int stars = RankStars.Total(_save.Data.meta);
            return new LeaderboardEntry
            {
                Name = Loc.T("rank.you"), IsSelf = true, Zpe = zpe, Level = reached, Stars = stars,
                Score = RankScore.Eligible(completed) ? RankScore.Compute(zpe, reached, _catalog.Count, stars, completed) : 0,
            };
        }

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern string KatKat_CountryCode();
#endif

        /// <summary>Cihaz ülkesi (ISO alfa-2). Bilinmiyorsa null — ülke uydurulmaz.</summary>
        public static string DeviceCountry()
        {
            if (CountryOverride != null) return CountryOverride.Length == 2 ? CountryOverride.ToUpperInvariant() : null;
            string code = null;
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var loc = new AndroidJavaClass("java.util.Locale"))
                using (var def = loc.CallStatic<AndroidJavaObject>("getDefault"))
                    code = def.Call<string>("getCountry");
#elif UNITY_IOS && !UNITY_EDITOR
                code = KatKat_CountryCode(); // NSLocale (Assets/Plugins/iOS/KatKatNative.mm)
#else
                code = RegionInfo.CurrentRegion.TwoLetterISORegionName;
#endif
            }
            catch (Exception) { code = null; }
            if (string.IsNullOrEmpty(code) || code.Length != 2 || code == "IV" || code == "ZZ") return null;
            foreach (char c in code) if (c < 'A' || c > 'Z') return null;
            return code;
        }

        /// <summary>Ülke adı: dil dosyasında varsa oradan (ör. country.TR = Türkiye), yoksa ülkenin kendi dilindeki adı.</summary>
        public static string CountryName(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            string native = code;
            try { native = new RegionInfo(code).NativeName; } catch (Exception) { }
            return Loc.T("country." + code, native);
        }
    }
}
