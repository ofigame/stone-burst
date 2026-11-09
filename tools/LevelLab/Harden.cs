using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// harden [from] [to]: mevcut bölümleri zorlaştırır (kullanıcı: "otomatik patlayanlardan dolayı kolaylaşmış").
// Tahta aynı kalır; renk hedefleri büyür, hamle limiti bot kazanma oranı yeni hedefe inene kadar daralır.
//   1) renk hedefleri × gf (1.15 → 1.29, bölüm ilerledikçe); ışın araması eski hamle limitinde çözüm arar,
//      bulamazsa gf küçülür (1.10, 1.05, 1.00 — 1.00'da kayıtlı çözüm korunur).
//   2) hamle limiti eskiden çözüm uzunluğuna doğru iner; hedefe en yakın bot oranı seçilir.
// Çıktı: levels_src_hard/ (kaynaklar yerinde değişmez; takas ayrı adım).
static partial class Program
{
    static double HardTarget(int n)
    {
        double b = 1 - Math.Exp(-(n - 6) / 350.0);
        double t = 0.65 - 0.42 * b;
        int r = n % 10;
        if (r == 0) t -= 0.05;            // x0: zor bölüm
        else if (r == 1 || r == 2) t += 0.05; // nefes
        return Math.Max(0.15, Math.Min(0.75, t));
    }

    static double BotRate(LevelData d, int moves, int n, int games)
    {
        d.MoveLimit = moves;
        var st = LevelLoader.ToState(d);
        var rng = new Random(n * 31 + 7);
        int wins = 0;
        for (int g = 0; g < games; g++) if (BotPlay(st.Clone(), rng, 0.35)) wins++;
        return (double)wins / games;
    }

    static bool ReplayWins(LevelData d, IList<Command> sol)
    {
        var s = LevelLoader.ToState(d);
        foreach (var c in sol) if (!RulesEngine.TryApply(s, c).Accepted) return false;
        return s.Status == GameStatus.Won;
    }

    static int Harden(string[] args)
    {
        int from = args.Length > 1 ? int.Parse(args[1]) : 6, to = args.Length > 2 ? int.Parse(args[2]) : 1000;
        int games = args.Length > 3 ? int.Parse(args[3]) : 16;
        var outDir = Path.Combine(Root, "levels_src_hard");
        Directory.CreateDirectory(outDir);
        var files = Directory.GetFiles(Path.Combine(Root, "levels_src"), "level_*.txt")
            .Select(f => (f, n: int.Parse(Path.GetFileNameWithoutExtension(f).Substring(6), CultureInfo.InvariantCulture)))
            .Where(t => t.n >= from && t.n <= to).OrderBy(t => t.n).ToList();
        var lines = new string[files.Count];
        int failed = 0;
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            var (path, n) = files[i];
            var outPath = Path.Combine(outDir, Path.GetFileName(path));
            if (File.Exists(outPath)) { lines[i] = $"{n:000}: var, atlandı"; return; }
            try
            {
                var orig = ParseSource(path);
                int oldMoves = orig.MoveLimit;
                var oldSol = orig.Solution.Select(LevelLoader.ToCommand).ToList();
                double oldWr = BotRate(Clone(orig), oldMoves, n, games);
                double target = HardTarget(n);
                double b = 1 - Math.Exp(-(n - 6) / 350.0);

                LevelData best = null; List<Command> sol = null; double usedGf = 1;
                foreach (var gf in new[] { 1.15 + 0.14 * b, 1.10, 1.05, 1.0 }.Distinct())
                {
                    var d = Clone(orig);
                    foreach (var g in d.Goals.Where(g => g.Type == "collectColor"))
                        g.Amount = (int)Math.Round(g.Amount * gf);
                    if (gf == 1.0) { best = d; sol = oldSol; usedGf = 1; break; }
                    if (LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)).Count > 0) continue;
                    d.MoveLimit = oldMoves;
                    var s = BeamSolve(LevelLoader.ToState(d), 40, oldMoves);
                    if (s != null && ReplayWins(d, s)) { best = d; sol = s; usedGf = gf; break; }
                }

                int L = sol.Count;
                int pick = oldMoves; double pickWr = usedGf == 1.0 ? oldWr : BotRate(Clone(best), oldMoves, n, games);
                double bestDiff = Math.Abs(pickWr - target);
                for (int m = oldMoves - 1; m >= L && pickWr > target; m--)
                {
                    double wr = BotRate(Clone(best), m, n, games);
                    double diff = Math.Abs(wr - target);
                    if (diff <= bestDiff) { bestDiff = diff; pick = m; }
                    if (wr <= target) { pickWr = wr; break; }
                    pickWr = wr;
                }
                // Hedef büyümesi tek başına fazla sertleştirdiyse limit en çok +3 açılır.
                if (pick == oldMoves && pickWr < target - 0.12)
                    for (int m = oldMoves + 1; m <= oldMoves + 3; m++)
                    {
                        double wr = BotRate(Clone(best), m, n, games);
                        double diff = Math.Abs(wr - target);
                        if (diff < bestDiff) { bestDiff = diff; pick = m; }
                        if (wr >= target) break;
                    }
                double finalWr = BotRate(Clone(best), pick, n, games);
                best.MoveLimit = pick;
                best.Solution.Clear();
                foreach (var c in sol) best.Solution.Add(ToStep(c));
                if (!ReplayWins(best, sol)) throw new Exception("son tekrar kazanmadı");
                var ev = new Eval { Info = $"zor: çözüm {L}/{pick} (eski {oldSol.Count}/{oldMoves}) hedef×{usedGf:0.00} bot {finalWr:0.00} (eski {oldWr:0.00}, hedef {target:0.00})" };
                File.WriteAllText(outPath, ToSource(best, ev), new UTF8Encoding(false));
                lines[i] = $"{n:000}: {ev.Info}";
            }
            catch (Exception e)
            {
                System.Threading.Interlocked.Increment(ref failed);
                lines[i] = $"{n:000}: HATA {e.Message}";
            }
        });
        foreach (var l in lines) Console.WriteLine(l);
        return failed == 0 ? 0 : 1;
    }
}
