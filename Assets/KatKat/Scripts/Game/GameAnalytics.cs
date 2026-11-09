using System;
using System.Collections.Generic;

namespace KatKat.Game
{
    /// <summary>Olay hedefi (cihazda Firebase Analytics; testlerde kaydedici).</summary>
    public interface IAnalyticsSink
    {
        void Event(string name, IReadOnlyList<KeyValuePair<string, object>> parameters);
        void UserProperty(string name, string value);
    }

    /// <summary>
    /// Oyun olayları (kullanıcı, 2026-11-09: "istatistikleri görebilecek miyim" → Firebase Analytics). Kişisel veri
    /// yok: yalnız bölüm numarası, oturum türü, sonuç, hamle, yıldız ve reklam yeri. Hedef yoksa hiçbir şey yapılmaz.
    /// Olay adları (Firebase konsolunda görünür):
    /// - level_start: level_number, level_id, kind (normal/daily/trial/custom), attempt, first_attempt (0/1)
    /// - level_end: aynıları + success (0/1), moves_used, move_limit, stars, assists
    /// - ad_rewarded: placement, outcome (rewarded/skipped/failed…)
    /// - ad_interstitial: placement, shown (0/1)
    /// - hearts_empty: level_number (kalp bitti, bölüm açılmadı)
    /// - review_prompt: level_number (sistem puan penceresi istendi; Apple gösterip göstermemeye kendisi karar verir)
    /// Kullanıcı özelliği: highest_level.
    /// </summary>
    public static class GameAnalytics
    {
        public static IAnalyticsSink Sink;

        public static void Log(string name, params (string key, object value)[] p)
        {
            var s = Sink;
            if (s == null) return;
            var list = new List<KeyValuePair<string, object>>(p.Length);
            foreach (var (k, v) in p) list.Add(new KeyValuePair<string, object>(k, v ?? ""));
            try { s.Event(name, list); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[KatKat] Analytics: " + e.Message); }
        }

        public static void Property(string name, string value)
        {
            try { Sink?.UserProperty(name, value); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[KatKat] Analytics: " + e.Message); }
        }
    }

    /// <summary>Testler için: olayları bellekte tutar.</summary>
    public sealed class RecordingAnalytics : IAnalyticsSink
    {
        public readonly List<(string name, Dictionary<string, object> p)> Events = new List<(string, Dictionary<string, object>)>();
        public readonly Dictionary<string, string> Properties = new Dictionary<string, string>();

        public void Event(string name, IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            var d = new Dictionary<string, object>();
            foreach (var kv in parameters) d[kv.Key] = kv.Value;
            Events.Add((name, d));
        }

        public void UserProperty(string name, string value) => Properties[name] = value;

        public int Count(string name) => Events.FindAll(e => e.name == name).Count;
        public Dictionary<string, object> Last(string name) => Events.FindLast(e => e.name == name).p;
    }
}
