#if KATKAT_ONLINE
using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using KatKat.Meta;

namespace KatKat.Game.Online
{
    /// <summary>
    /// Firestore sıralaması (kullanıcı kararı 2026-11-09). Koleksiyon <c>scores/{firebaseUid}</c>:
    /// name (Play Games adı), country (ISO alfa-2 ya da ""), score (yıldız + ZPE; 2026-11-19 öncesi 0–1000), zpe, level, stars, updated.
    /// Fetch: oyuncu uygunsa (Score &gt; 0) önce kendi kaydını yazar, sonra ilk 10'u ve kendi sırasını
    /// (kendinden yüksek puan sayısı + 1) okur. Kurallar ve dizinler: <c>firebase/</c> klasörü.
    /// YALNIZ KATKAT_ONLINE tanımlıyken derlenir. CİHAZDA DENENMEDİ.
    /// </summary>
    public sealed class FirestoreLeaderboard : ILeaderboardProvider, ILeaderboardWriter
    {
        public const string Collection = "scores";
        private readonly Func<string> _playerName;

        public FirestoreLeaderboard(Func<string> playerName) => _playerName = playerName;

        public void Fetch(LeaderboardScope scope, string country, LeaderboardEntry self, Action<LeaderboardResult> done)
        {
            var r = new LeaderboardResult { Scope = scope, Country = country, Self = self };
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null) { if (self != null) self.Rank = 0; r.Status = LeaderboardStatus.NotConnected; done?.Invoke(r); return; }
            if (scope == LeaderboardScope.Country && string.IsNullOrEmpty(country)) { r.Status = LeaderboardStatus.NoCountry; done?.Invoke(r); return; }

            var db = FirebaseFirestore.DefaultInstance;
            var col = db.Collection(Collection);
            if (self != null && self.Score > 0)
                col.Document(user.UserId).SetAsync(Doc(self)).ContinueWithOnMainThread(_ => Query(col, scope, country, self, user.UserId, r, done));
            else Query(col, scope, country, self, user.UserId, r, done);
        }

        /// <summary>Kazanılan bölümden sonra: Sıralama açılmadan kendi kaydını yazar (2026-11-18).</summary>
        public void Submit(LeaderboardEntry self, Action<bool> done)
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null || self == null || self.Score <= 0) { done?.Invoke(false); return; }
            FirebaseFirestore.DefaultInstance.Collection(Collection).Document(user.UserId).SetAsync(Doc(self))
                .ContinueWithOnMainThread(t => done?.Invoke(!t.IsFaulted && !t.IsCanceled));
        }

        private Dictionary<string, object> Doc(LeaderboardEntry self) => new Dictionary<string, object>
        {
            { "name", Trim(_playerName?.Invoke()) }, { "country", GameController.DeviceCountry() ?? "" },
            { "score", self.Score }, { "zpe", self.Zpe }, { "level", self.Level }, { "stars", self.Stars },
            { "updated", FieldValue.ServerTimestamp },
        };

        private static void Query(CollectionReference col, LeaderboardScope scope, string country, LeaderboardEntry self,
            string uid, LeaderboardResult r, Action<LeaderboardResult> done)
        {
            Query q = col;
            if (scope == LeaderboardScope.Country) q = q.WhereEqualTo("country", country);
            q.OrderByDescending("score").Limit(10).GetSnapshotAsync().ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted || t.IsCanceled) { r.Status = LeaderboardStatus.Error; r.Error = t.Exception?.GetBaseException().Message; done?.Invoke(r); return; }
                int rank = 0;
                foreach (var d in t.Result.Documents)
                {
                    rank++;
                    bool me = d.Id == uid;
                    r.Top.Add(new LeaderboardEntry
                    {
                        Rank = rank, IsSelf = me, Name = Get<string>(d, "name") ?? "",
                        Score = Get<int>(d, "score"), Zpe = Get<int>(d, "zpe"), Level = Get<int>(d, "level"), Stars = Get<int>(d, "stars"),
                    });
                    if (me && self != null) self.Rank = rank;
                }
                r.Status = r.Top.Count == 0 ? LeaderboardStatus.Empty : LeaderboardStatus.Ready;
                if (self == null || self.Score <= 0 || self.Rank > 0) { done?.Invoke(r); return; }
                q.WhereGreaterThan("score", self.Score).Count.GetSnapshotAsync(AggregateSource.Server).ContinueWithOnMainThread(c =>
                {
                    if (!c.IsFaulted && !c.IsCanceled) self.Rank = (int)c.Result.Count + 1;
                    done?.Invoke(r);
                });
            });
        }

        private static T Get<T>(DocumentSnapshot d, string field) => d.TryGetValue(field, out T v) ? v : default;
        private static string Trim(string s) => string.IsNullOrEmpty(s) ? "?" : s.Length > 40 ? s.Substring(0, 40) : s;
    }
}
#endif
