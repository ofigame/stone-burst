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

// daily15 [gün sayısı] (2026-11-28, kullanıcı: "günlük bulmacayı da güncel oyunumuza uyarlayalım … Günlük bulmaca zor olsun mesela
// 244. bölüm çok zordu. Ondan örnek alabilirsin"): 240 günlük bulmaca (30 gün × 8 grup) ana bölümlerin bugünkü üreticisiyle
// (V14Make: yukarıdan dolma, tepsiden özel taş, en az 10 hamle, büyük grup sınırı) yeniden üretilir. Rulo kapalı (ana oyunda yok),
// kural türü yok (normal bölüm). Zorluk 244 örneğinde: rastgele bot ≤ %1–3, açgözlü bot kaybeder, yumuşak açgözlü bot ≤ %5–15,
// hamle payı bandın alt ucu. Grubun alt sınırından sonra tanıtılan mekanik çıkarılır (oyuncu henüz görmedi).
// Çıktı: daily_src_v15/daily_dNNN_tK.txt (+ _olcum). Kurulum ayrı adım: daily15install (daily_src'ye kopya, JSON, katalog daily-v5).
static partial class Program
{
    static string DailyV15Dir => Path.Combine(Root, "daily_src_v15");

    static V14Spec DailySpecV15(int day, int tier, int attempt)
    {
        var (lo, hi) = DailyTiers[tier - 1];
        int from = tier == 1 ? 31 : lo + (hi - lo) / 5, to = tier == 1 ? 40 : lo + (hi - lo) / 2;
        var cands = Enumerable.Range(from, to - from + 1).Where(k => k % 10 != 0 && k % 10 != 7 && k % 10 != 8 && !V14Breath(k)).ToList();
        int n = cands[(day * 37 + tier * 11 + attempt * 5) % cands.Count];
        var sp = V14SpecFor(n);
        int known = Math.Max(10, lo);
        foreach (var (mech, intro) in V14Mechs)
            if (intro > known) foreach (var key in V14MechKeys[mech]) sp.Mech.Remove(key);
        sp.Type = null; sp.Rules.Clear(); sp.Exact = false;
        sp.RefillFrac = V14RefillFrac(n);
        sp.LenLo = Math.Max(3, sp.LenLo - 2);
        sp.LenLo = Math.Max(sp.LenLo, MinMoveLimit - sp.SlackLo - 1);
        if (sp.LenHi < sp.LenLo + 4) sp.LenHi = sp.LenLo + 4;
        // 244 zorluğu: rastgele ve açgözlü oyun kaybeder, plan ister; pay bandın alt ucu (denemede gerekirse açılır).
        sp.RandomCap = Math.Min(sp.RandomCap, tier <= 2 ? 0.03 : 0.01);
        sp.GreedyMustLose = true;
        sp.SoftCap = tier == 1 ? 0.15 : tier == 2 ? 0.10 : 0.05;
        sp.Slack = sp.SlackLo;
        sp.Roll = false;
        return sp;
    }

    static int Daily15(string[] args)
    {
        int days = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 30;
        bool force = Environment.GetEnvironmentVariable("KATKAT_FORCE") == "1";
        var outDir = DailyV15Dir;
        var mDir = Path.Combine(outDir, "_olcum");
        Directory.CreateDirectory(mDir);
        var jobs = new List<(int day, int tier)>();
        for (int day = 1; day <= days; day++) for (int t = DailyTiers.Length; t >= 1; t--) jobs.Add((day, t));
        jobs = jobs.Where(j => force || !File.Exists(Path.Combine(outDir, $"daily_d{j.day:000}_t{j.tier}.txt"))).OrderByDescending(j => j.tier).ToList();
        Console.WriteLine($"daily15: {jobs.Count} bulmaca üretilecek, {Math.Max(1, Environment.ProcessorCount - 1)} iş parçacığı");
        int done = 0, failed = 0, fallback = 0;
        var started = Stopwatch.StartNew();
        Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, job =>
        {
            var (day, tier) = job;
            string id = $"daily_d{day:000}_t{tier}";
            string err = "";
            V14Result keep = null; V14Spec keepSp = null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var sp = DailySpecV15(day, tier, attempt);
                V14Result r;
                try { r = V14Make(sp, day * 1000003 + tier * 7919 + attempt * 104729 + 15, day * 131 + tier * 17 + attempt * 7); }
                catch (Exception e) { err = e.Message; continue; }
                if (r.D == null) { err = r.Flags; continue; }
                if (!ReplayWins(r.D, r.D.Solution.Select(LevelLoader.ToCommand).ToList())) { err = "son tekrar kazanmadı"; continue; }
                keep = r; keepSp = sp;
                if (!r.Flags.Contains("YEDEK")) break; // yedekse bir başka tarifle bir daha dene
            }
            if (keep == null) { Interlocked.Increment(ref failed); Console.WriteLine($"{id}: BAŞARISIZ {err}"); return; }
            var d = keep.D;
            d.RefillSeed = d.Id; // dolma dizisi ölçüldüğü kimliğin tohumuyla kalır (kayıtlı çözüm geçerli)
            d.Id = id;
            d.Title = $"Günlük {day}";
            d.Tutorial = "";
            d.RollEnabled = false;
            d.StartingRoll = false;
            if (keep.Flags.Contains("YEDEK")) Interlocked.Increment(ref fallback);
            var info = $"günlük v15: grup {tier} ({DailyTiers[tier - 1].Lo}–{DailyTiers[tier - 1].Hi}); tarif {keepSp.N}; şekil {keep.Shape}; çözüm {keep.Ev.L} + {keep.Ev.Uses} tepsi / sınır {d.MoveLimit}; rastgele {keep.Ev.Rand:0.000}; açgözlü {(keep.Ev.GreedyBig || keep.Ev.GreedyGoal ? "kazanır" : "kaybeder")}; yumuşak {keep.Ev.Soft:0.00}{(keep.Flags.Length > 0 ? "; " + keep.Flags.Trim() : "")}";
            File.WriteAllText(Path.Combine(outDir, id + ".txt"), ToSource(d, new Eval { Info = info }), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(mDir, id + ".tsv"), $"{id}\t{tier}\t{keepSp.N}\t{V14MetricsLine(keep)}\n", new UTF8Encoding(false));
            int k = Interlocked.Increment(ref done);
            Console.WriteLine($"{id} [{k}/{jobs.Count} {started.Elapsed.TotalMinutes:0.0} dk] tarif {keepSp.N} {d.Width}x{d.Height} çözüm {keep.Ev.L}/{d.MoveLimit} rastgele {keep.Ev.Rand:0.000} yumuşak {keep.Ev.Soft:0.00} {keep.Flags}");
        });
        Console.WriteLine($"daily15: {done} bulmaca, {fallback} yedek, {failed} başarısız, {started.Elapsed.TotalMinutes:0.0} dk");
        return failed == 0 ? 0 : 1;
    }

    /// <summary>daily_src_v15 → daily_src (aynı adların üzerine), JSON derleme ve katalog (daily-v5: eski günlük durumları sıfırlanır).</summary>
    static int Daily15Install(string[] args)
    {
        var files = Directory.GetFiles(DailyV15Dir, "daily_d*_t*.txt").OrderBy(f => f, StringComparer.Ordinal).ToList();
        int days = files.Count / DailyTiers.Length;
        if (days == 0 || files.Count != days * DailyTiers.Length) { Console.Error.WriteLine($"daily15install: {files.Count} kaynak (8'in katı bekleniyor)"); return 1; }
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
        DailyVersion = "daily-v5";
        WriteDailyIndex(days);
        Console.WriteLine($"daily15install: {files.Count} bulmaca → {outDir}");
        return 0;
    }
}
