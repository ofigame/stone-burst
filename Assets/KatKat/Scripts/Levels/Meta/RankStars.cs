using System;
using System.Globalization;

namespace KatKat.Meta
{
    /// <summary>
    /// v12 (kullanıcı, 2026-11-08): reklamla kazanılan yıldızlar sıralamada sayılmaz. Sıralama yıldızı, reklam
    /// yardımı olmadan kazanılmış en iyi yıldızdır (bölüm başına 1–3). Oyundaki yıldızlar (harita, taş eşikleri)
    /// değişmez; bu yalnız sıralama puanı içindir.
    /// </summary>
    public static class RankStars
    {
        public static void Record(MetaSave meta, string levelId, int stars)
        {
            if (meta == null || string.IsNullOrEmpty(levelId) || levelId.IndexOf('|') >= 0) return;
            stars = Math.Max(1, Math.Min(3, stars));
            meta.rankStars ??= new System.Collections.Generic.List<string>();
            string prefix = levelId + "|";
            for (int i = 0; i < meta.rankStars.Count; i++)
            {
                if (!meta.rankStars[i].StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (Parse(meta.rankStars[i]) < stars) meta.rankStars[i] = prefix + stars.ToString(CultureInfo.InvariantCulture);
                return;
            }
            meta.rankStars.Add(prefix + stars.ToString(CultureInfo.InvariantCulture));
        }

        public static int For(MetaSave meta, string levelId)
        {
            if (meta?.rankStars == null) return 0;
            string prefix = levelId + "|";
            foreach (var e in meta.rankStars) if (e.StartsWith(prefix, StringComparison.Ordinal)) return Parse(e);
            return 0;
        }

        public static int Total(MetaSave meta)
        {
            int t = 0;
            if (meta?.rankStars != null) foreach (var e in meta.rankStars) t += Parse(e);
            return t;
        }

        private static int Parse(string e)
        {
            int k = e.LastIndexOf('|');
            return k >= 0 && int.TryParse(e.Substring(k + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? Math.Max(0, Math.Min(3, v)) : 0;
        }
    }
}
