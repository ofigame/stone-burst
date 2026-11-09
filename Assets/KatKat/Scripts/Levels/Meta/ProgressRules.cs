using System.Collections.Generic;

namespace KatKat.Meta
{
    /// <summary>
    /// v9 A: yıldız ilerlemesi. Her bölüm kimliği için yalnız en yüksek geçerli (1–3) yıldız sayılır; tekrar oynayarak
    /// yıldız birikmez. Kayıt paralel listelerde tutulur (JsonUtility sözlük yazmaz).
    /// </summary>
    public static class ProgressRules
    {
        public const int MinStars = 1, MaxStars = 3;

        public static bool IsValid(int stars) => stars >= MinStars && stars <= MaxStars;

        /// <summary>Bölüm kimliği başına en yüksek geçerli yıldızların toplamı (yinelenen kimlik bir kez sayılır).</summary>
        public static int TotalStars(IList<string> ids, IList<int> counts)
        {
            var best = new Dictionary<string, int>();
            int n = ids == null || counts == null ? 0 : System.Math.Min(ids.Count, counts.Count);
            for (int i = 0; i < n; i++)
            {
                if (string.IsNullOrEmpty(ids[i]) || !IsValid(counts[i])) continue;
                if (!best.TryGetValue(ids[i], out var b) || counts[i] > b) best[ids[i]] = counts[i];
            }
            int total = 0;
            foreach (var v in best.Values) total += v;
            return total;
        }

        /// <summary>
        /// En iyi yıldızı yazar; toplamdaki artışı döndürür (0–3). Geçersiz yıldız (1–3 dışı) yok sayılır.
        /// 3 → 1 → 3: +3, +0, +0. 1 → 3: +1, +2.
        /// </summary>
        public static int RecordBest(List<string> ids, List<int> counts, string id, int stars)
        {
            if (string.IsNullOrEmpty(id) || !IsValid(stars)) return 0;
            int i = ids.IndexOf(id);
            if (i < 0) { ids.Add(id); counts.Add(stars); return stars; }
            if (stars <= counts[i]) return 0;
            int delta = stars - counts[i];
            counts[i] = stars;
            return delta;
        }

        /// <summary>
        /// Eski kayıt yükseltmesi: listeler eşitlenir, aynı kimlik tekrarlanıyorsa en yüksek geçerli değer kalır,
        /// geçersiz değerler (1–3 dışı, boş kimlik) atılır. Değişen kayıt sayısını döndürür.
        /// </summary>
        public static int Normalize(List<string> ids, List<int> counts)
        {
            int n = System.Math.Min(ids.Count, counts.Count);
            int removed = ids.Count + counts.Count - 2 * n;
            var order = new List<string>();
            var best = new Dictionary<string, int>();
            for (int i = 0; i < n; i++)
            {
                if (string.IsNullOrEmpty(ids[i]) || !IsValid(counts[i])) { removed++; continue; }
                if (best.TryGetValue(ids[i], out var b)) { removed++; if (counts[i] > b) best[ids[i]] = counts[i]; continue; }
                best[ids[i]] = counts[i];
                order.Add(ids[i]);
            }
            ids.Clear(); counts.Clear();
            foreach (var id in order) { ids.Add(id); counts.Add(best[id]); }
            return removed;
        }
    }
}
