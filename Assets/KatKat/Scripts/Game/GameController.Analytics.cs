using KatKat.Meta;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Oyun olaylarının bağlandığı yerler (<see cref="GameAnalytics"/>; kullanıcı, 2026-11-09) ve uygulama içi puan
    /// isteği (kullanıcı, 2026-11-09: "kurallar çerçevesinde uygulama puanı da isteyelim").
    /// </summary>
    public sealed partial class GameController
    {
        private int AnalyticsLevelNumber => _sessionKind == SessionKind.Normal ? _levelIndex + 1 : 0;

        private static string KindName(SessionKind k) => k.ToString().ToLowerInvariant();

        private void AnalyticsLevelStart()
        {
            var o = _outcome;
            if (o == null) return;
            GameAnalytics.Log("level_start",
                ("level_number", AnalyticsLevelNumber), ("level_id", o.LevelId), ("kind", KindName(o.Kind)),
                ("attempt", o.AttemptNumber), ("first_attempt", o.FirstAttempt ? 1 : 0));
        }

        private void AnalyticsLevelEnd(bool success)
        {
            var o = _outcome;
            if (o == null) return;
            int stars = success && _session != null ? EarnedStars : 0;
            GameAnalytics.Log("level_end",
                ("level_number", AnalyticsLevelNumber), ("level_id", o.LevelId), ("kind", KindName(o.Kind)),
                ("attempt", o.AttemptNumber), ("first_attempt", o.FirstAttempt ? 1 : 0), ("success", success ? 1 : 0),
                ("moves_used", o.MovesUsed), ("move_limit", o.MoveLimit), ("stars", stars), ("assists", (int)o.Assists));
            if (success && o.Kind == SessionKind.Normal)
                GameAnalytics.Property("highest_level", (_save.Data.highestUnlocked + 1).ToString());
        }

        // ---- Uygulama içi puan isteği ----------------------------------------------------------------------------

        /// <summary>Sistem puan penceresi istendiğinde (cihazda iOS StoreKit; testlerde sayılır).</summary>
        public static System.Action ReviewRequester;
        public static int ReviewRequestsForTest;
        /// <summary>Test: sabit saat (Unix sn).</summary>
        public static System.Func<long> ReviewClock;

        /// <summary>
        /// Normal bölüm kazanılınca (zafer kartından Devam'a basınca): kurallar <see cref="ReviewRules"/>. Apple yalnız
        /// sistem penceresine izin verir; yılda en çok 3 kez gösterir ve göstermemeye de karar verebilir.
        /// </summary>
        private void MaybeRequestReview()
        {
            if (_sessionKind != SessionKind.Normal || _outcome == null || !_outcome.Success) return;
            long now = ReviewClock?.Invoke() ?? System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var m = _save.Data.meta;
            if (!ReviewRules.ShouldAsk(_save.Data.completed.Count, EarnedStars, _outcome.Assisted, m.reviewAsks, m.reviewLast, now)) return;
            m.reviewAsks++;
            m.reviewLast = now;
            _save.Save();
            GameAnalytics.Log("review_prompt", ("level_number", AnalyticsLevelNumber), ("ask", m.reviewAsks));
            ReviewRequestsForTest++;
            if (ReviewRequester != null) ReviewRequester();
            else RequestStoreReview();
        }

        private static void RequestStoreReview()
        {
#if UNITY_IOS && !UNITY_EDITOR
            UnityEngine.iOS.Device.RequestStoreReview();
#else
            if (!Application.isBatchMode) Debug.Log("[KatKat] Puan isteği (yalnız iOS cihazda sistem penceresi)");
#endif
        }
    }
}
