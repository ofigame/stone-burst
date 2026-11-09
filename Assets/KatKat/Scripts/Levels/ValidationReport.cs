using System;
using System.Collections.Generic;
using System.Text;

namespace KatKat.Levels
{
    /// <summary>
    /// Bütün seviyeler için statik kontrol + kayıtlı çözüm tekrarı raporu (docs/SEVIYE_DOGRULAMA.md).
    /// Hem Unity editör menüsü hem tools/LevelLab aynı kodu kullanır.
    /// </summary>
    public static class ValidationReport
    {
        public sealed class Entry
        {
            public string Source;
            public string Id;
            public List<string> StaticErrors = new List<string>();
            public ReplayReport Replay;
            public bool Passed => StaticErrors.Count == 0 && Replay != null && Replay.Success;
        }

        public static Entry Check(string sourceName, string json)
        {
            var e = new Entry { Source = sourceName };
            LevelData d;
            try { d = LevelParser.Parse(json, sourceName); }
            catch (FormatException ex) { e.StaticErrors.Add(ex.Message); return e; }
            e.Id = d.Id;
            e.StaticErrors.AddRange(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)));
            if (e.StaticErrors.Count == 0) e.Replay = LevelValidator.ReplaySolution(d);
            return e;
        }

        public static string Markdown(IList<Entry> entries, string producedBy, string date)
        {
            int ok = 0;
            foreach (var e in entries) if (e.Passed) ok++;
            var sb = new StringBuilder();
            sb.AppendLine("# Kat Kat — Seviye Doğrulama Raporu");
            sb.AppendLine();
            sb.AppendLine($"Üretildi: {date} • Araç: {producedBy} • Sonuç: **{ok}/{entries.Count} geçti**");
            sb.AppendLine();
            sb.AppendLine("Statik kontrol (Bölüm 14; 1–20 ilk sürüm, 21+ ikinci aşama kapsamı) + kayıtlı çözümün gerçek `RulesEngine` ile tekrarı. Bu bir çözüm varlığı kanıtıdır; en kısa çözüm veya eğlence kanıtı değildir.");
            sb.AppendLine();
            sb.AppendLine("| Seviye | Statik | Çözüm adımı / limit | Dalga | Soyulan katman | Rulo (kazanılan/kullanılan) | Son hedefler | Sonuç |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var e in entries)
            {
                string stat = e.StaticErrors.Count == 0 ? "OK" : string.Join("; ", e.StaticErrors);
                var r = e.Replay;
                if (r == null)
                {
                    sb.AppendLine($"| {e.Id ?? e.Source} | {stat} | — | — | — | — | — | **HATA** |");
                    continue;
                }
                string result = r.Success ? "Won ✔" : $"**HATA**: {r.Error}";
                sb.AppendLine($"| {r.LevelId} | {stat} | {r.Steps} / {r.MoveLimit} | {r.Waves} | {r.LayersPeeled} | {r.RollsAwarded}/{r.RollsUsed} | {string.Join(", ", r.Goals)} | {result} |");
            }
            return sb.ToString();
        }
    }
}
