using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// audit [oyun=24]: v8 zorluk ve seviyeye uygunluk denetimi (kullanıcı, 2026-11-08: "tüm bölümlerin çözümünü, zorluğunu,
// seviyeye uygunluğunu test et"). Derlenmiş bölüm JSON'ları üzerinde:
//   1) Çözüm: kayıtlı çözüm kural motoruyla kazanır; hamle harcayan adım ≤ sınır.
//   2) Zorluk: yumuşak açgözlü bot kazanma oranı (üretimden farklı tohum), çözüm uzunluğu, pay, açgözlü sonucu.
//   3) Uygunluk: mekanik tanıtım takvimi (OYUN.md §4) ve tür tanıtım takvimi (v8) öncesinde görünmez; bölge 1'de tür
//      bölümleri öğretici kalır (üretim tabanı %10; denetimde gürültü payıyla ≥ %8); özel taşlı her bölümde bot en az bazen kazanır.
//   4) Eğri: 100'lük dilimlerde normal / tür / patron ortalamaları (tür ve patron çevresinden zor olmalı).
// Çıktı: docs/v12/V8_ZORLUK.md. Hata (uygunsuz bölüm) varsa çıkış kodu 1.
static partial class Program
{
    static readonly (string cap, int from)[] MechanicIntro =
    {
        ("clip", 21), ("star", 41), ("ice", 61), ("colorLock", 121), ("envelope", 201), ("rotor", 301), ("sponge", 401),
        ("linkedClip", 501), ("gate", 601), ("paint", 701),
    };

    static int Audit(string[] args)
    {
        int games = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 48;
        var files = Directory.GetFiles(LevelsDir, "level_*.json").OrderBy(f => f, StringComparer.Ordinal).ToList();
        var rows = new (int n, string type, int limit, int sol, double wr, bool greedy, List<string> issues)[files.Count];
        var started = DateTime.Now;
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            var d = LevelParser.Parse(File.ReadAllText(files[i]), Path.GetFileName(files[i]));
            int n = LevelValidator.LevelNumber(d.Id);
            var issues = new List<string>();
            // 1) Çözüm
            var s = LevelLoader.ToState(d);
            int moves = 0;
            foreach (var step in d.Solution)
            {
                var c = LevelLoader.ToCommand(step);
                if (!RulesEngine.TryApply(s, c).Accepted) { issues.Add("çözüm adımı reddedildi"); break; }
                if (c.Type != CommandType.UseSpecial) moves++;
            }
            if (s.Status != GameStatus.Won) issues.Add("kayıtlı çözüm kazanmıyor");
            if (moves > d.MoveLimit) issues.Add($"çözüm {moves} > sınır {d.MoveLimit}");
            // 2) Zorluk (üretimden farklı tohum)
            double wr = BotRate(Clone(d), d.MoveLimit, n * 1000 + 77, games);
            bool greedy = Play(LevelLoader.ToState(d), Greedy);
            // 3) Uygunluk
            var caps = CapabilitiesOf(d);
            foreach (var (cap, from) in MechanicIntro)
                if (caps.Contains(cap) && n < from && d.TrialType != "order") issues.Add($"mekanik {cap} tanıtımdan ({from}) önce");
            if (!string.IsNullOrEmpty(d.TrialType) && d.TrialType != "boss" && d.TrialType != "aha")
            {
                int intro = V8Types.FirstOrDefault(t => t.type == d.TrialType).intro;
                if (intro == 0) issues.Add("bilinmeyen tür " + d.TrialType);
                else if (n < intro) issues.Add($"tür {d.TrialType} tanıtımdan ({intro}) önce");
            }
            if (d.TrialType == "aha" && n < V8AhaFirst) issues.Add("tek dokunuş tanıtımdan önce");
            if (n < 100 && !string.IsNullOrEmpty(d.TrialType) && d.TrialType != "aha" && wr < 0.08 - 1e-9 && d.TrialType != "boss") issues.Add($"bölge 1 tür bölümü fazla zor (bot {wr:0.00})");
            if (d.Capabilities.Contains("specials") && wr <= 0)
            {
                // Ek 40 oyunla doğrula (stres ölçütü: 64 oyunda en az 1 galibiyet).
                var st = LevelLoader.ToState(d); var rr = new Random(n * 131 + 17); int w2 = 0;
                for (int g = 0; g < 40; g++) if (BotPlay(st.Clone(), rr, 0.35)) w2++;
                if (w2 == 0) issues.Add($"bot {games + 40} oyunda hiç kazanamadı");
            }
            rows[i] = (n, string.IsNullOrEmpty(d.TrialType) ? "normal" : d.TrialType, d.MoveLimit, moves, wr, greedy, issues);
        });
        var md = new StringBuilder();
        md.AppendLine($"# v8 zorluk ve uygunluk denetimi (LevelLab audit, {DateTime.Now:yyyy-MM-dd HH:mm})");
        md.AppendLine();
        md.AppendLine($"{files.Count} bölüm. Bot: yumuşak açgözlü, {games} oyun, üretimden farklı tohum. Pay = sınır − kayıtlı çözüm hamlesi.");
        md.AppendLine();
        int bad = rows.Count(r => r.issues.Count > 0);
        md.AppendLine($"**Sonuç:** {files.Count - bad}/{files.Count} bölüm uygun; {bad} sorunlu. Kayıtlı çözüm kazanan: {rows.Count(r => !r.issues.Any(x => x.Contains("çözüm")))}/{files.Count}.");
        md.AppendLine();
        md.AppendLine("## Zorluk eğrisi (ortalama bot kazanma oranı; düşük = zor)");
        md.AppendLine("| Dilim | Normal | Tür | Patron | Tür bölümü sayısı | Ort. pay (normal / tür) | Açgözlü kazanır (tür) |");
        md.AppendLine("|---|---|---|---|---|---|---|");
        for (int b = 0; b < 10; b++)
        {
            var blk = rows.Where(r => r.n > b * 100 && r.n <= (b + 1) * 100).ToList();
            var nor = blk.Where(r => r.type == "normal").ToList();
            var typ = blk.Where(r => r.type != "normal" && r.type != "boss" && r.type != "aha").ToList();
            var bos = blk.Where(r => r.type == "boss").ToList();
            string Avg(IEnumerable<double> v) => v.Any() ? v.Average().ToString("0.00", CultureInfo.InvariantCulture) : "—";
            md.AppendLine($"| {b * 100 + 1}–{(b + 1) * 100} | {Avg(nor.Select(r => r.wr))} | {Avg(typ.Select(r => r.wr))} | {Avg(bos.Select(r => r.wr))} | {typ.Count + bos.Count} | {Avg(nor.Select(r => (double)(r.limit - r.sol)))} / {Avg(typ.Concat(bos).Select(r => (double)(r.limit - r.sol)))} | {typ.Count(r => r.greedy)}/{typ.Count} |");
        }
        md.AppendLine();
        md.AppendLine("## Türlere göre");
        md.AppendLine("| Tür | Sayı | Ort. bot | En düşük bot | Ort. pay |");
        md.AppendLine("|---|---|---|---|---|");
        foreach (var g in rows.GroupBy(r => r.type).OrderBy(g => g.Key, StringComparer.Ordinal))
            md.AppendLine($"| {g.Key} | {g.Count()} | {g.Average(r => r.wr):0.00} | {g.Min(r => r.wr):0.00} | {g.Average(r => (double)(r.limit - r.sol)):0.0} |");
        md.AppendLine();
        md.AppendLine("## Sorunlu bölümler");
        if (bad == 0) md.AppendLine("Yok.");
        foreach (var r in rows.Where(r => r.issues.Count > 0)) md.AppendLine($"- level_{r.n:000} ({r.type}): {string.Join("; ", r.issues)}");
        File.WriteAllText(Path.Combine(Root, "docs", "v12", "V8_ZORLUK.md"), md.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"audit: {files.Count - bad}/{files.Count} uygun, {(DateTime.Now - started).TotalMinutes:0.0} dk → docs/v12/V8_ZORLUK.md");
        return bad == 0 ? 0 : 1;
    }
}
