#if KATKAT_ONLINE && (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using System.Collections.Generic;
using Firebase.Analytics;

namespace KatKat.Game.Online
{
    /// <summary>
    /// Firebase Analytics hedefi (kullanıcı, 2026-11-09). Reklam amaçlı sinyaller kapalı: ad_storage, ad_user_data,
    /// ad_personalization reddedilmiş sayılır; yalnız analytics_storage açık (olay sayımı). CİHAZDA DENENMEDİ.
    /// </summary>
    internal sealed class FirebaseAnalyticsSink : IAnalyticsSink
    {
        public FirebaseAnalyticsSink()
        {
            FirebaseAnalytics.SetConsent(new Dictionary<ConsentType, ConsentStatus>
            {
                { ConsentType.AnalyticsStorage, ConsentStatus.Granted },
                { ConsentType.AdStorage, ConsentStatus.Denied },
                { ConsentType.AdUserData, ConsentStatus.Denied },
                { ConsentType.AdPersonalization, ConsentStatus.Denied },
            });
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        }

        public void Event(string name, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            var list = new List<Parameter>(parameters.Count);
            foreach (var kv in parameters)
            {
                switch (kv.Value)
                {
                    case int i: list.Add(new Parameter(kv.Key, i)); break;
                    case long l: list.Add(new Parameter(kv.Key, l)); break;
                    case double d: list.Add(new Parameter(kv.Key, d)); break;
                    case float f: list.Add(new Parameter(kv.Key, f)); break;
                    case bool b: list.Add(new Parameter(kv.Key, b ? 1L : 0L)); break;
                    default: list.Add(new Parameter(kv.Key, kv.Value?.ToString() ?? "")); break;
                }
            }
            FirebaseAnalytics.LogEvent(name, list.ToArray());
        }

        public void UserProperty(string name, string value) => FirebaseAnalytics.SetUserProperty(name, value);
    }
}
#endif
