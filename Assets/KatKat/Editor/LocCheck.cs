using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using KatKat.UI;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Dil tablosu denetimi: koddaki her Loc.T/Loc.F anahtarı tabloda var mı, her dilde dolu mu, her karakter
    /// Nunito ya da yedek fontlarda (Noto Sans Arabic / KR) var mı, yer tutucular ({0} {1}) dillerde aynı mı.
    /// Batch: -executeMethod KatKat.EditorTools.LocCheck.RunBatch  → Logs/loc_check.txt
    /// </summary>
    public static class LocCheck
    {
        [MenuItem("Kat Kat/Dil tablosunu denetle")]
        public static void Run() => Check(out _);

        public static void RunBatch()
        {
            bool ok = Check(out _);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Check(out string report)
        {
            var sb = new StringBuilder();
            int problems = 0;
            var cols = Loc.Columns;
            var rows = new Dictionary<string, string[]>();
            foreach (var kv in Loc.Rows) rows[kv.Key] = kv.Value;
            sb.AppendLine($"Diller: {string.Join(", ", cols)}; anahtar: {rows.Count}");

            // 1) Kodda kullanılan anahtarlar
            var used = new SortedSet<string>();
            var rx = new Regex("Loc\\.(?:T|F)\\(\"([a-zA-Z0-9_.]+)\"");
            foreach (var f in Directory.GetFiles("Assets/KatKat/Scripts", "*.cs", SearchOption.AllDirectories))
                foreach (Match m in rx.Matches(File.ReadAllText(f))) used.Add(m.Groups[1].Value);
            // Dinamik anahtarlar
            foreach (var t in new[] { "coast", "garden", "desert" }) used.Add("theme." + t);
            for (int i = 0; i < 10; i++) used.Add("region." + i);
            foreach (var c in new[] { "clip", "star", "ice", "colorLock", "envelope", "rotor", "sponge", "linkedClip", "gate", "paint" }) used.Add("tut.mech." + c);
            used.RemoveWhere(k => k.EndsWith("."));
            foreach (var k in used)
                if (!rows.ContainsKey(k)) { sb.AppendLine("EKSİK ANAHTAR: " + k); problems++; }

            // 2) Boş hücre, yer tutucu, glif
            var ph = new Regex("\\{\\d\\}");
            var missingChars = new Dictionary<string, SortedSet<char>>();
            foreach (var kv in rows)
            {
                var vals = kv.Value;
                var srcPh = PlaceHolders(ph, vals[0]);
                for (int i = 0; i < cols.Length; i++)
                {
                    string v = i < vals.Length ? vals[i] : "";
                    if (string.IsNullOrEmpty(v)) { sb.AppendLine($"BOŞ: {kv.Key} [{cols[i]}]"); problems++; continue; }
                    if (PlaceHolders(ph, v) != srcPh) { sb.AppendLine($"YER TUTUCU: {kv.Key} [{cols[i]}] '{v}'"); problems++; }
                    if (!UiTypography.CanRender(v, out var miss))
                    {
                        if (!missingChars.TryGetValue(cols[i], out var set)) missingChars[cols[i]] = set = new SortedSet<char>();
                        foreach (char c in miss) set.Add(c);
                        sb.AppendLine($"GLİF YOK: {kv.Key} [{cols[i]}] '{miss}'");
                        problems++;
                    }
                }
            }
            foreach (var l in Loc.Languages)
                if (!UiTypography.CanRender(l.NativeName, out var miss)) { sb.AppendLine($"GLİF YOK (dil adı): {l.NativeName} '{miss}'"); problems++; }
            foreach (var kv in missingChars) sb.AppendLine($"Eksik karakterler [{kv.Key}]: {new string(new List<char>(kv.Value).ToArray())}");
            sb.AppendLine(problems == 0 ? "SONUÇ: TAMAM" : $"SONUÇ: {problems} sorun");
            report = sb.ToString();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/loc_check.txt", report, new UTF8Encoding(false));
            Debug.Log("[KatKat] Dil denetimi:\n" + report);
            return problems == 0;
        }

        private static string PlaceHolders(Regex rx, string s)
        {
            var list = new List<string>();
            foreach (Match m in rx.Matches(s ?? "")) list.Add(m.Value);
            list.Sort();
            return string.Join(",", list);
        }
    }
}
