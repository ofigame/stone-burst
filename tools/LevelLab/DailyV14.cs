using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// daily14 [gün sayısı] (v14, docs/v14/PLAN.md "Diğer kataloglar"; kullanıcı 2026-11-16: "zincir tamamen kaldırılsın"):
// 240 günlük bulmaca (30 gün × 8 grup) v14 ana bölüm üreticisiyle (V14Make) yeniden üretilir. Günün türü haftalık döner,
// zincir türü döngüden çıktı: exact → protect → reverse → scarce → pure → resource → order (grupta açılmamış tür atlanır).
//   * Tarif: grubun alt sınırına yakın bir v14 bölüm numarasının tarifi (tahta, kat, renk, görev, ölçütler) + günün türü;
//     grubun alt sınırından sonra tanıtılan mekanik çıkarılır (oyuncu henüz görmedi). Pay: kesin türde 0 (1–99'da 1), diğerlerinde
//     bandın alt ucu ("çok zor").
//   * Rulo açık (taş gücü "Gelgit" başta uygulanabilir olmalı); ölçüm ve kayıtlı çözüm rulo açıkken, güç KULLANILMADAN.
// Çıktı: daily_src_v14/daily_dNNN_tK.txt (+ _olcum). Kurulum ayrı adım: daily14install (daily_src'ye kopya, JSON derleme, katalog).
static partial class Program
{
    static readonly string[] DailyTypeCycleV14 = { "exact", "protect", "reverse", "scarce", "pure", "resource", "order" };

    static readonly Dictionary<string, string[]> V14MechKeys = new Dictionary<string, string[]>
    {
        ["clip"] = new[] { "clip1", "clip2" }, ["star"] = new[] { "stars" }, ["ice"] = new[] { "ice1", "ice2" }, ["colorLock"] = new[] { "lock" },
        ["envelope"] = new[] { "env" }, ["rotor"] = new[] { "rotor" }, ["sponge"] = new[] { "sponge" }, ["linkedClip"] = new[] { "link" },
        ["gate"] = new[] { "gate" }, ["paint"] = new[] { "paint" },
    };

    static string DailyTypeFor(int day, int tier)
    {
        int lo = DailyTiers[tier - 1].Lo;
        for (int k = 0; k < DailyTypeCycleV14.Length; k++)
        {
            var type = DailyTypeCycleV14[(day - 1 + k) % DailyTypeCycleV14.Length];
            if (V14Types.Any(t => t.type == type && t.intro <= Math.Max(lo, 27))) return type;
        }
        return "exact";
    }

    /// <summary>Günlük tarif: grubun temsilci bölüm numarası (normal bölüm: x0/x7/x8 ve nefes değil) + günün türü.</summary>
    static V14Spec DailySpecV14(int day, int tier, int attempt, out string type)
    {
        var (lo, hi) = DailyTiers[tier - 1];
        int from = tier == 1 ? 15 : lo, to = tier == 1 ? 30 : lo + Math.Max(20, (hi - lo) / 3);
        var cands = Enumerable.Range(from, to - from + 1).Where(k => k > 5 && k % 10 != 0 && k % 10 != 7 && k % 10 != 8 && !V14Breath(k)).ToList();
        int n = cands[(day * 37 + tier * 11 + attempt * 5) % cands.Count];
        var sp = V14SpecFor(n);
        // Grubun alt sınırından sonra tanıtılan mekanik çıkar (grup 1: 10'a kadar mekanik yok).
        int known = Math.Max(10, lo);
        foreach (var (mech, intro) in V14Mechs)
            if (intro > known) foreach (var key in V14MechKeys[mech]) sp.Mech.Remove(key);
        type = DailyTypeFor(day, tier);
        sp.Type = type;
        sp.Rules.Clear();
        switch (type)
        {
            case "exact": sp.Exact = true; sp.Rules.Add("exact"); break;
            case "reverse": sp.Exact = true; sp.GreedyMustLose = true; sp.Rules.Add("exact"); break;
            case "protect": V14ApplyRule(sp, "protect"); if (n >= 100) { sp.Exact = true; sp.Rules.Add("exact"); } break;
            case "scarce": V14ApplyRule(sp, "scarce"); break;
            case "pure": V14ApplyRule(sp, "pure"); if (n >= 200) { sp.Exact = true; sp.Rules.Add("exact"); } break;
            case "resource": sp.Exact = true; sp.NeedsSpecials = true; sp.Rules.Add("exact"); break;
            case "order": sp.Order = true; sp.Mech.Remove("lock"); if (n >= 300) { sp.Exact = true; sp.Rules.Add("exact"); } break;
        }
        sp.Slack = sp.Exact ? (n < 100 ? 1 : 0) : sp.SlackLo;
        if (sp.NoSpecials) { sp.LenHi += 4; sp.Colors = Math.Max(3, sp.Colors - 2); sp.Frac *= 0.8; }
        sp.Roll = true;
        return sp;
    }

    static string DailyV14Dir => Path.Combine(Root, "daily_src_v14");

    static int Daily14(string[] args)
    {
        int days = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 30;
        bool force = Environment.GetEnvironmentVariable("KATKAT_FORCE") == "1";
        var outDir = DailyV14Dir;
        var mDir = Path.Combine(outDir, "_olcum");
        Directory.CreateDirectory(mDir);
        var jobs = new List<(int day, int tier)>();
        for (int day = 1; day <= days; day++) for (int t = DailyTiers.Length; t >= 1; t--) jobs.Add((day, t));
        // Yük dengesi: büyük (geç) gruplar önce.
        jobs = jobs.Where(j => force || !File.Exists(Path.Combine(outDir, $"daily_d{j.day:000}_t{j.tier}.txt"))).OrderByDescending(j => j.tier).ToList();
        Console.WriteLine($"daily14: {jobs.Count} bulmaca üretilecek, {Math.Max(1, Environment.ProcessorCount - 1)} iş parçacığı");
        int done = 0, failed = 0, fallback = 0;
        var started = Stopwatch.StartNew();
        Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, job =>
        {
            var (day, tier) = job;
            string id = $"daily_d{day:000}_t{tier}";
            string err = "";
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var sp = DailySpecV14(day, tier, attempt, out var type);
                V14Result r;
                try { r = V14Make(sp, day * 1000003 + tier * 7919 + attempt * 104729 + 14, day * 131 + tier * 17 + attempt * 7); }
                catch (Exception e) { err = e.Message; continue; }
                if (r.D == null) { err = r.Flags; continue; }
                var d = r.D;
                d.Id = id;
                d.Title = $"Günlük {day}";
                d.Tutorial = "";
                d.RollEnabled = true;
                d.StartingRoll = false;
                var sol = d.Solution.Select(LevelLoader.ToCommand).ToList();
                if (!ReplayWins(d, sol)) { err = "son tekrar kazanmadı"; continue; }
                if (!KatKat.Meta.StonePowers.CanApply(KatKat.Meta.StonePowers.DailyRoll, LevelLoader.ToState(d))) { err = "güç başta uygulanamaz"; continue; }
                if (r.Flags.Contains("YEDEK")) Interlocked.Increment(ref fallback);
                var info = $"günlük v14: grup {tier} ({DailyTiers[tier - 1].Lo}–{DailyTiers[tier - 1].Hi}); tarif {sp.N} + tür {type}; şekil {r.Shape}; çözüm {r.Ev.L} + {r.Ev.Uses} tepsi / sınır {d.MoveLimit}; rastgele {r.Ev.Rand:0.000}; açgözlü {(r.Ev.GreedyBig || r.Ev.GreedyGoal ? "kazanır" : "kaybeder")}; yumuşak {r.Ev.Soft:0.00}{(r.Flags.Length > 0 ? "; " + r.Flags.Trim() : "")}";
                File.WriteAllText(Path.Combine(outDir, id + ".txt"), ToSource(d, new Eval { Info = info }), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(mDir, id + ".tsv"), $"{id}\t{tier}\t{sp.N}\t{type}\t{V14MetricsLine(r)}\n", new UTF8Encoding(false));
                int k = Interlocked.Increment(ref done);
                Console.WriteLine($"{id} [{k}/{jobs.Count} {started.Elapsed.TotalMinutes:0.0} dk] tarif {sp.N} {type} {d.Width}x{d.Height} çözüm {r.Ev.L}/{d.MoveLimit} rastgele {r.Ev.Rand:0.000} açgözlü {(r.Ev.GreedyBig ? "B" : "-")}{(r.Ev.GreedyGoal ? "H" : "-")} {r.Seconds:0} sn {r.Flags}");
                return;
            }
            Interlocked.Increment(ref failed);
            Console.WriteLine($"{id}: BAŞARISIZ {err}");
        });
        Console.WriteLine($"daily14: {done} bulmaca, {fallback} yedek, {failed} başarısız, {started.Elapsed.TotalMinutes:0.0} dk");
        return failed == 0 ? 0 : 1;
    }

    /// <summary>daily_src_v14 → daily_src (aynı adların üzerine; eski v3 kaynakları arşivde), JSON derleme ve katalog (daily-v4).</summary>
    static int Daily14Install(string[] args)
    {
        var files = Directory.GetFiles(DailyV14Dir, "daily_d*_t*.txt").OrderBy(f => f, StringComparer.Ordinal).ToList();
        int days = files.Count / DailyTiers.Length;
        if (days == 0 || files.Count != days * DailyTiers.Length) { Console.Error.WriteLine($"daily14install: {files.Count} kaynak (8'in katı bekleniyor)"); return 1; }
        var src = Path.Combine(Root, "daily_src");
        Directory.CreateDirectory(src);
        foreach (var f in files) File.Copy(f, Path.Combine(src, Path.GetFileName(f)), true);
        var outDir = Path.Combine(Root, "Assets", "KatKat", "Resources", "Daily");
        foreach (var f in files.Select(f => Path.Combine(src, Path.GetFileName(f))))
        {
            var d = ParseSource(f);
            d.StarThresholds = StarThresholds(d);
            File.WriteAllText(Path.Combine(outDir, d.Id + ".json"), ToJson(d), new UTF8Encoding(false));
        }
        DailyVersion = "daily-v4";
        WriteDailyIndex(days);
        Console.WriteLine($"daily14install: {files.Count} bulmaca → {outDir}");
        return 0;
    }
}
