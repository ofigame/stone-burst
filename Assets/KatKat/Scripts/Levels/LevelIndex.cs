using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KatKat.Levels
{
    /// <summary>
    /// v10 P1.6: bölüm dizin dosyası (Resources/LevelIndex/levels_index.json). Açılışta bütün bölüm metinlerini
    /// yüklemek yerine yalnız bu küçük dosya okunur. Üretim: <c>tools/LevelLab index</c>. Dosya bölümlerle
    /// birebir aynı değilse test düşer (LevelIndexTests); oyun dizin okunamazsa eski taramaya döner.
    /// Biçim: {"version":1,"count":N,"levels":["level_001",...]} — sayısal sırada.
    /// </summary>
    public static class LevelIndex
    {
        public const int Version = 1;
        public const string ResourcePath = "LevelIndex/levels_index";

        /// <summary>Sayısal sıra (level_1000, level_101'den sonra gelir); eşitse metin sırası.</summary>
        public static int Compare(string a, string b)
        {
            int na = LevelValidator.LevelNumber(a), nb = LevelValidator.LevelNumber(b);
            return na != nb ? na.CompareTo(nb) : string.CompareOrdinal(a, b);
        }

        public static string Write(IEnumerable<string> names)
        {
            var list = new List<string>(names);
            list.Sort(Compare);
            var sb = new StringBuilder();
            sb.Append("{\n  \"version\": ").Append(Version.ToString(CultureInfo.InvariantCulture))
              .Append(",\n  \"count\": ").Append(list.Count.ToString(CultureInfo.InvariantCulture))
              .Append(",\n  \"levels\": [");
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(i % 10 == 0 ? "\n    " : " ").Append('"').Append(list[i]).Append('"');
                if (i < list.Count - 1) sb.Append(',');
            }
            sb.Append("\n  ]\n}\n");
            return sb.ToString();
        }

        /// <summary>Dizini okur. Bozuk, sürümü bilinmeyen, sayısı tutmayan, yinelenen ya da sırasız dizinde FormatException.</summary>
        public static string[] Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) throw new FormatException("levels_index: boş");
            if (!(MiniJson.Parse(text) is Dictionary<string, object> root)) throw new FormatException("levels_index: kök nesne olmalı");
            int version = root.TryGetValue("version", out var v) && v != null ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : 0;
            if (version != Version) throw new FormatException("levels_index: bilinmeyen sürüm " + version);
            if (!(root.TryGetValue("levels", out var l) && l is List<object> arr)) throw new FormatException("levels_index: \"levels\" yok");
            int count = root.TryGetValue("count", out var c) && c != null ? Convert.ToInt32(c, CultureInfo.InvariantCulture) : -1;
            if (count != arr.Count) throw new FormatException($"levels_index: count {count} ≠ liste {arr.Count}");
            var names = new string[arr.Count];
            var seen = new HashSet<string>();
            for (int i = 0; i < arr.Count; i++)
            {
                var n = arr[i] as string;
                if (string.IsNullOrEmpty(n) || !n.StartsWith("level_", StringComparison.Ordinal)) throw new FormatException($"levels_index: geçersiz ad #{i}");
                if (!seen.Add(n)) throw new FormatException("levels_index: yinelenen " + n);
                if (i > 0 && Compare(names[i - 1], n) >= 0) throw new FormatException("levels_index: sıra bozuk: " + n);
                names[i] = n;
            }
            return names;
        }
    }
}
