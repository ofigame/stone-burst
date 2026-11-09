using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// chaincmp [oyun] [kurallar]: zincir kuralı seçeneklerini 1000 bölümde karşılaştırır (kullanıcı, 2026-11-08: "zincir kuralı
// anlaşılmıyor, daha anlaşılır ve biraz kolay olmalı"). Kural 0 = mevcut (tamamı yeni), N = 3+ grupta en az N yeni soyulmuş.
// Ölçüler: kayıtlı çözüm kazanıyor mu, yumuşak açgözlü bot kazanma oranı, bot oyunlarında hamle başına zincir dalgası,
// zincirli hamle oranı. Bölüm dilimlerine göre özet.
static partial class Program
{
    static int ChainCompare(string[] args)
    {
        int games = args.Length > 1 ? int.Parse(args[1]) : 12;
        var rules = args.Length > 2 ? args[2].Split(',').Select(int.Parse).ToArray() : new[] { 0, 1, 2, 3 };
        var files = Directory.GetFiles(LevelsDir, "level_*.json").OrderBy(f => f, StringComparer.Ordinal).ToList();
        var levels = files.Select(f => LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f))).ToList();
        var bands = new[] { (1, 20), (21, 100), (101, 300), (301, 600), (601, 1000) };
        Console.WriteLine($"chaincmp: {levels.Count} bölüm, kural başına {games} bot oyunu");
        foreach (var rule in rules)
        {
            var solWin = new bool[levels.Count];
            var rate = new double[levels.Count];
            var chainPerMove = new double[levels.Count];
            var chainMoveShare = new double[levels.Count];
            var solChains = new int[levels.Count];
            Parallel.For(0, levels.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
            {
                RulesEngine.ChainTouch = rule;
                var d = levels[i];
                var s = LevelLoader.ToState(d);
                bool ok = true;
                foreach (var step in d.Solution)
                {
                    var r = RulesEngine.TryApply(s, LevelLoader.ToCommand(step));
                    if (!r.Accepted) { ok = false; break; }
                    foreach (var w in r.Waves) if (w.Kind == WaveKind.Chain) solChains[i]++;
                }
                solWin[i] = ok && s.Status == GameStatus.Won;
                var start = LevelLoader.ToState(d);
                var rng = new Random(LevelValidator.LevelNumber(d.Id) * 31 + 7);
                int wins = 0; long moves = 0, chainWaves = 0, chainMoves = 0;
                for (int g = 0; g < games; g++)
                {
                    var st = start.Clone();
                    var (won, mv, cw, cm) = BotPlayStats(st, rng, 0.35);
                    if (won) wins++;
                    moves += mv; chainWaves += cw; chainMoves += cm;
                }
                rate[i] = (double)wins / games;
                chainPerMove[i] = moves > 0 ? (double)chainWaves / moves : 0;
                chainMoveShare[i] = moves > 0 ? (double)chainMoves / moves : 0;
            });
            RulesEngine.ResetChainRule();
            Console.WriteLine();
            Console.WriteLine($"== kural {rule} {(rule == 0 ? "(mevcut: tamamı yeni)" : $"(3+ grupta en az {rule} yeni)")} ==");
            Console.WriteLine($"  kayıtlı çözüm kazanıyor: {solWin.Count(x => x)}/{levels.Count}");
            foreach (var (a, b) in bands)
            {
                var idx = Enumerable.Range(0, levels.Count).Where(k => { int n = LevelValidator.LevelNumber(levels[k].Id); return n >= a && n <= b; }).ToList();
                if (idx.Count == 0) continue;
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  {0,4}-{1,-4} bot kazanma %{2,5:0.0}  zincirli hamle %{3,5:0.0}  hamle başına zincir dalgası {4:0.00}  çözüm kazanır {5}/{6}",
                    a, b, idx.Average(k => rate[k]) * 100, idx.Average(k => chainMoveShare[k]) * 100, idx.Average(k => chainPerMove[k]),
                    idx.Count(k => solWin[k]), idx.Count));
            }
            var lost = Enumerable.Range(0, levels.Count).Where(k => !solWin[k]).Select(k => levels[k].Id).Take(15).ToList();
            if (lost.Count > 0) Console.WriteLine("  kazanmayan çözümler (ilk 15): " + string.Join(", ", lost));
        }
        return 0;
    }

    static (bool won, int moves, int chainWaves, int chainMoves) BotPlayStats(LevelState s, Random rng, double temp)
    {
        int moves = 0, cw = 0, cm = 0;
        while (!s.IsTerminal)
        {
            var cands = LevelSolver.Moves(s);
            if (cands.Count == 0) break;
            var vals = new List<(LevelState n, double v, MoveResult r)>();
            foreach (var m in cands)
            {
                var n = s.Clone();
                var r = RulesEngine.TryApply(n, m);
                if (!r.Accepted) continue;
                if (n.Status == GameStatus.LostProtected) continue;
                vals.Add((n, Heur(n), r));
            }
            if (vals.Count == 0) break;
            double max = vals.Max(t => t.v);
            var weights = vals.Select(t => Math.Exp((t.v - max) / Math.Max(1e-3, temp * 20))).ToList();
            double sum = weights.Sum(), roll = rng.NextDouble() * sum;
            int pick = 0;
            for (; pick < weights.Count - 1; pick++) { roll -= weights[pick]; if (roll <= 0) break; }
            var res = vals[pick].r;
            int c = res.Waves.Count(w => w.Kind == WaveKind.Chain);
            moves++; cw += c; if (c > 0) cm++;
            s = vals[pick].n;
        }
        return (s.Status == GameStatus.Won, moves, cw, cm);
    }
}

// chainfit <kaynak klasör> <levels|daily>: v13 zincir kuralına geçiş (kullanıcı, 2026-11-08: "Altından birlikte çıkan 3+
// aynı renk patlar"). Tahta, hedef, tür ve kurallar korunur. Kayıtlı çözüm yeni kuralla kazanıyorsa aynen kalır; kazanmıyorsa
// ışın aramasıyla yeni çözüm bulunur ve hamle sınırı = yeni çözüm + bölümün eski payı (eski sınır − eski çözüm). Sonra
// adillik tabanı (fairfix ile aynı 112 bot oyunu) altındaysa sınır en çok +4 artar. Dosya yerinde yazılır (arşiv önce).
static partial class Program
{
    static int ChainFit(string[] args)
    {
        string rel = args.Length > 1 ? args[1] : "levels_src";
        bool daily = args.Length > 2 && args[2] == "daily";
        bool json = args.Length > 2 && args[2] == "json"; // deneme seti (Resources/Trial): kaynak yok, JSON yerinde
        var dir = Path.Combine(Root, rel);
        var files = Directory.GetFiles(dir, json ? "*.json" : "*.txt").OrderBy(f => f, StringComparer.Ordinal).ToList();
        var rows = new string[files.Count];
        int kept = 0, resolved = 0, failed = 0, raised = 0, low = 0;
        var started = DateTime.Now;
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            RulesEngine.ResetChainRule();
            string name = Path.GetFileNameWithoutExtension(files[i]);
            var d = json ? LevelParser.Parse(File.ReadAllText(files[i]), Path.GetFileName(files[i])) : ParseSource(files[i]);
            int n = json ? int.Parse(name.Substring(6), CultureInfo.InvariantCulture) : daily ? int.Parse(name.Substring(7, 3), CultureInfo.InvariantCulture) * 10 + int.Parse(name.Substring(12), CultureInfo.InvariantCulture)
                          : int.Parse(name.Substring(6), CultureInfo.InvariantCulture);
            int oldLimit = d.MoveLimit;
            int oldSol = d.Solution.Count(s => s.Type != "use");
            int slack = Math.Max(0, oldLimit - oldSol);
            var sol = d.Solution.Select(LevelLoader.ToCommand).ToList();
            string how;
            if (ReplayWins(d, sol)) { how = "çözüm aynı"; System.Threading.Interlocked.Increment(ref kept); }
            else
            {
                var c = Clone(d);
                c.MoveLimit = oldLimit + 20;
                List<Command> found = null;
                foreach (int width in new[] { 60, 250 })
                {
                    found = BeamSolve(LevelLoader.ToState(c), width, c.MoveLimit);
                    if (found != null && ReplayWins(c, found)) break;
                    found = null;
                }
                if (found == null)
                {
                    System.Threading.Interlocked.Increment(ref failed);
                    rows[i] = $"| {name} | {d.TrialType ?? "normal"} | {oldLimit} | — | — | **çözüm bulunamadı** |";
                    return;
                }
                int moves = found.Count(x => x.Type != CommandType.UseSpecial);
                d.MoveLimit = moves + slack;
                d.Solution.Clear();
                foreach (var x in found) d.Solution.Add(ToStep(x));
                if (!ReplayWins(d, found)) throw new Exception(name + ": yeni çözüm sınırda kazanmıyor");
                how = $"yeni çözüm {moves} (eski {oldSol})";
                System.Threading.Interlocked.Increment(ref resolved);
            }
            bool intro = !daily && V8Plan(n)?.Intro == true;
            // Öğretici bölümler (1–5) tam hamle sayısıyla ders verir: adillik payı uygulanmaz (bölüm 4 elle 2'de tutuldu).
            int need = json ? 0 : daily ? 5 : intro ? 14 : d.TrialType == "boss" ? 5 : n < 100 ? 9 : 6;
            int before = d.MoveLimit, w = FairWins(d, n);
            while (w < need && d.MoveLimit < before + 4) { d.MoveLimit++; w = FairWins(d, n); }
            if (d.MoveLimit != before) System.Threading.Interlocked.Increment(ref raised);
            if (w < need) System.Threading.Interlocked.Increment(ref low);
            if (json)
            {
                d.StarThresholds = StarThresholds(d);
                File.WriteAllText(files[i], ToJson(d), new UTF8Encoding(false));
                rows[i] = $"| {name} | {d.TrialType} | {oldLimit} → {d.MoveLimit} | {how} | {w}/112 | |";
                return;
            }
            var head = File.ReadLines(files[i]).FirstOrDefault(l => l.StartsWith("//")) ?? "//";
            var info = head.TrimStart('/', ' ').Replace("LevelLab gen adayı (BFS en kısa çözüm kayıtlı; yayın onayı analyze/report ile). ", "");
            File.WriteAllText(files[i], ToSource(d, new Eval { Info = $"{info}; zincir v13: {how}, sınır {oldLimit}→{d.MoveLimit}, bot {w}/112" }), new UTF8Encoding(false));
            rows[i] = $"| {name} | {(string.IsNullOrEmpty(d.TrialType) ? "normal" : d.TrialType)} | {oldLimit} → {d.MoveLimit} | {how} | {w}/112 | {(w < need ? "taban altında" : "")} |";
        });
        RulesEngine.ResetChainRule();
        var md = new StringBuilder();
        md.AppendLine($"# Zincir v13 geçişi: {rel} (LevelLab chainfit, {DateTime.Now:yyyy-MM-dd HH:mm})");
        md.AppendLine();
        md.AppendLine($"{files.Count} dosya: çözüm aynı {kept}, yeniden çözülen {resolved}, çözülemeyen {failed}; adillik için sınırı artan {raised}, +4'te de taban altında {low}. Süre {(DateTime.Now - started).TotalMinutes:0.0} dk.");
        md.AppendLine();
        md.AppendLine("| Bölüm | Tür | Hamle sınırı | Çözüm | Bot (112) | Not |");
        md.AppendLine("|---|---|---|---|---|---|");
        foreach (var r in rows) if (r != null) md.AppendLine(r);
        Directory.CreateDirectory(Path.Combine(Root, "docs", "v13"));
        File.WriteAllText(Path.Combine(Root, "docs", "v13", $"ZINCIR_GECIS_{(json ? "DENEME" : daily ? "GUNLUK" : "BOLUM")}.md"), md.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"chainfit {rel}: aynı {kept}, yeni {resolved}, çözülemeyen {failed}, sınırı artan {raised}, taban altında {low}; {(DateTime.Now - started).TotalMinutes:0.0} dk");
        return failed == 0 ? 0 : 1;
    }
}

// chaindiag <kaynak dosya> [kurallar]: bot oyunlarının bitiş nedenleri (kazanma / hamle bitti / yasal hamle yok / kristal).
static partial class Program
{
    static int ChainDiag(string[] args)
    {
        var d = ParseSource(Path.Combine(Root, args[1]));
        var rules = args.Length > 2 ? args[2].Split(',').Select(int.Parse).ToArray() : new[] { 0, -2 };
        foreach (var rule in rules)
        {
            RulesEngine.ChainTouch = rule;
            var counts = new Dictionary<GameStatus, int>();
            var rng = new Random(5);
            int movesLeftSum = 0;
            for (int g = 0; g < 200; g++)
            {
                var s = LevelLoader.ToState(d);
                while (!s.IsTerminal)
                {
                    var vals = new List<(LevelState n, double v)>();
                    foreach (var m in LevelSolver.Moves(s)) { var n = s.Clone(); if (RulesEngine.TryApply(n, m).Accepted && n.Status != GameStatus.LostProtected) vals.Add((n, Heur(n))); }
                    if (vals.Count == 0) break;
                    double max = vals.Max(t => t.v);
                    var w = vals.Select(t => Math.Exp((t.v - max) / 7.0)).ToList();
                    double sum = w.Sum(), roll = rng.NextDouble() * sum; int pick = 0;
                    for (; pick < w.Count - 1; pick++) { roll -= w[pick]; if (roll <= 0) break; }
                    s = vals[pick].n;
                }
                counts[s.Status] = counts.TryGetValue(s.Status, out var c) ? c + 1 : 1;
                if (s.Status == GameStatus.LostNoLegalMoves) movesLeftSum += s.MovesLeft;
            }
            Console.WriteLine($"kural {rule}: " + string.Join(", ", counts.Select(kv => kv.Key + " " + kv.Value)) + $"  (yasal hamle yokken ort. kalan hamle {(counts.TryGetValue(GameStatus.LostNoLegalMoves, out var k) && k > 0 ? (double)movesLeftSum / k : 0):0.0})");
        }
        RulesEngine.ResetChainRule();
        return 0;
    }
}

// fairlist <kaynak klasör (Root'a göre ya da mutlak)> <kural>: her bölüm için 112 bot oyununda galibiyet (fairfix ölçüsü).
static partial class Program
{
    static int FairList(string[] args)
    {
        var dir = Path.IsPathRooted(args[1]) ? args[1] : Path.Combine(Root, args[1]);
        int rule = int.Parse(args[2]);
        var files = Directory.GetFiles(dir, "level_*.txt").OrderBy(f => f, StringComparer.Ordinal).ToList();
        var rows = new string[files.Count];
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            RulesEngine.ChainTouch = rule;
            var d = ParseSource(files[i]);
            int n = int.Parse(Path.GetFileNameWithoutExtension(files[i]).Substring(6), CultureInfo.InvariantCulture);
            rows[i] = $"{n} {FairWins(d, n)} {(string.IsNullOrEmpty(d.TrialType) ? "normal" : d.TrialType)}";
            RulesEngine.ResetChainRule();
        });
        File.WriteAllLines(Path.Combine(Root, "Logs", $"fair_{rule}.txt"), rows);
        Console.WriteLine($"fairlist: {files.Count} → Logs/fair_{rule}.txt");
        return 0;
    }
}

// chainease <kaynak klasör> <levels|daily> <eski arşiv klasörü>: chainfit sonrası adillik tabanının altında kalan ya da çözülemeyen
// bölümler. Hamle payı (+4) yetmediğinde (yeni kuralda tahta çabuk kilitleniyor) renk hedefleri adım adım %6 düşer (en çok %36);
// her adımda yeni çözüm bulunur, sınır = çözüm + eski pay, sonra +0..+4 adillik. Tabanı tutan ilk adım yazılır.
static partial class Program
{
    static int ChainEase(string[] args)
    {
        string rel = args[1];
        bool daily = args[2] == "daily";
        string oldDir = args[3];
        var dir = Path.Combine(Root, rel);
        var files = Directory.GetFiles(dir, "*.txt").OrderBy(f => f, StringComparer.Ordinal).ToList();
        var rows = new System.Collections.Concurrent.ConcurrentBag<string>();
        int eased = 0, still = 0, ok = 0;
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            RulesEngine.ResetChainRule();
            string name = Path.GetFileNameWithoutExtension(files[i]);
            var d = ParseSource(files[i]);
            int n = daily ? int.Parse(name.Substring(7, 3), CultureInfo.InvariantCulture) * 10 + int.Parse(name.Substring(12), CultureInfo.InvariantCulture)
                          : int.Parse(name.Substring(6), CultureInfo.InvariantCulture);
            bool intro = !daily && V8Plan(n)?.Intro == true;
            int need = daily ? 5 : intro ? 14 : d.TrialType == "boss" ? 5 : n < 100 ? 9 : 6;
            bool solves = ReplayWins(d, d.Solution.Select(LevelLoader.ToCommand).ToList());
            bool forced = (Environment.GetEnvironmentVariable("KATKAT_ONLY") ?? "").Length > 0;
            if (!forced && solves && FairWins(d, n) >= need) { System.Threading.Interlocked.Increment(ref ok); return; }
            var old = ParseSource(Path.Combine(oldDir, Path.GetFileName(files[i])));
            int slack = Math.Max(0, old.MoveLimit - old.Solution.Count(s => s.Type != "use"));
            var baseGoals = old.Goals.Select(g => g.Amount).ToList();
            int maxK = int.TryParse(Environment.GetEnvironmentVariable("KATKAT_EASE_MAX"), out var mk) ? mk : 6;
            var only = (Environment.GetEnvironmentVariable("KATKAT_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
            if (only.Count > 0 && !only.Contains(n)) return;
            for (int k = 1; k <= maxK; k++)
            {
                var c = Clone(old);
                for (int g = 0; g < c.Goals.Count; g++)
                    if (c.Goals[g].Type == "collectColor" || c.Goals[g].Type == "collectChain")
                        c.Goals[g].Amount = Math.Max(1, (int)Math.Ceiling(baseGoals[g] * (1 - 0.06 * k)));
                var probe = Clone(c);
                probe.MoveLimit = old.MoveLimit + 20;
                List<Command> found = null;
                foreach (int width in new[] { 60, 250 })
                {
                    found = BeamSolve(LevelLoader.ToState(probe), width, probe.MoveLimit);
                    if (found != null && ReplayWins(probe, found)) break;
                    found = null;
                }
                if (found == null) continue;
                int moves = found.Count(x => x.Type != CommandType.UseSpecial);
                c.MoveLimit = moves + slack;
                c.Solution.Clear();
                foreach (var x in found) c.Solution.Add(ToStep(x));
                int before = c.MoveLimit, w = FairWins(c, n);
                while (w < need && c.MoveLimit < before + 4) { c.MoveLimit++; w = FairWins(c, n); }
                if (w < need && k < maxK) continue;
                var head = File.ReadLines(files[i]).FirstOrDefault(l => l.StartsWith("//")) ?? "//";
                var info = head.TrimStart('/', ' ').Replace("LevelLab gen adayı (BFS en kısa çözüm kayıtlı; yayın onayı analyze/report ile). ", "");
                File.WriteAllText(files[i], ToSource(c, new Eval { Info = $"{info}; zincir v13 kolaylaştırma: hedef −%{6 * k}, sınır {c.MoveLimit}, bot {w}/112" }), new UTF8Encoding(false));
                if (w >= need) System.Threading.Interlocked.Increment(ref eased); else System.Threading.Interlocked.Increment(ref still);
                rows.Add($"| {name} | {(string.IsNullOrEmpty(c.TrialType) ? "normal" : c.TrialType)} | −%{6 * k} | {old.MoveLimit} → {c.MoveLimit} | {w}/112 | {(w < need ? "taban altında" : "")} |");
                return;
            }
            System.Threading.Interlocked.Increment(ref still);
            rows.Add($"| {name} | {d.TrialType} | — | — | — | **kolaylaştırılamadı** ({(solves ? "çözüm var" : "çözüm yok")}) |");
        });
        RulesEngine.ResetChainRule();
        var md = new StringBuilder();
        md.AppendLine($"# Zincir v13 kolaylaştırma: {rel} (LevelLab chainease, {DateTime.Now:yyyy-MM-dd HH:mm})");
        md.AppendLine();
        md.AppendLine($"Tabanı zaten tutan {ok}; hedef düşürülerek tabana çıkan {eased}; hâlâ altında ya da çözümsüz {still}.");
        md.AppendLine();
        md.AppendLine("| Bölüm | Tür | Hedef | Sınır (eski → yeni) | Bot (112) | Not |");
        md.AppendLine("|---|---|---|---|---|---|");
        foreach (var r in rows.OrderBy(r => r, StringComparer.Ordinal)) md.AppendLine(r);
        File.WriteAllText(Path.Combine(Root, "docs", "v13", $"ZINCIR_KOLAY_{(daily ? "GUNLUK" : "BOLUM")}.md"), md.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"chainease {rel}: zaten uygun {ok}, kolaylaştırılan {eased}, hâlâ altında {still}");
        return 0;
    }
}
