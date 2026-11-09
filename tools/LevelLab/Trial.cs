using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KatKat.Core;
using KatKat.Levels;

/// <summary>
/// v12 deneme seti (docs/v12/PLAN.md §6): her bölüm türünden bir örnek + patron. Ana 1000 bölüme ve günlüğe
/// dokunmaz; çıktı Assets/KatKat/Resources/Trial/trial_NN.json. Her tür, çözücüyle ölçülen ölçütleri sağlayana
/// kadar rastgele (tohumlu) tahta üretir:
///   en kısa çözüm (BFS), kazandıran ilk hamle oranı (tuzak oranı), açgözlü botun sonucu, özel taşsız çözülebilirlik.
/// Kullanım: dotnet run -- trial [tohum]
/// </summary>
static partial class Program
{
    sealed class TrialSpec
    {
        public string Type, Title;
        public int W, H, Colors, MinLayers, MaxLayers;
        public bool Specials;
        public string[] Rules = Array.Empty<string>();
        public Func<Random, LevelData, bool> Shape; // hedefleri/kristalleri/kilitleri koyar
        public Func<Metrics, LevelData, bool> Accept;
        public int Slack;       // hamle sınırı = en kısa + Slack
        public int Budget = 400000;
    }

    sealed class Metrics
    {
        public int MinMoves;
        public List<Command> Solution;
        public int Legal, Winners;
        public bool GreedyWins;
        public bool BiggestFirstWins;
        public bool SolvableWithoutSpecials;
        public bool UsesTray;
        public double Trap => Legal == 0 ? 0 : 1.0 - Winners / (double)Legal;
    }

    static readonly string[] ColorNames = { "coral", "blue", "yellow", "purple" };

    static int Trial(string[] args)
    {
        int seed = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 20261108;
        string outDir = Path.Combine(RepoRoot(), "Assets", "KatKat", "Resources", "Trial");
        Directory.CreateDirectory(outDir);
        var specs = TrialSpecs();
        var report = new StringBuilder();
        report.AppendLine("# Deneme seti üretim raporu (LevelLab trial, tohum " + seed + ")");
        report.AppendLine();
        report.AppendLine("| # | Tür | Tahta | Hamle | En kısa | İlk hamle (kazandıran/yasal) | Tuzak | Açgözlü | Büyük grup önce | Özel taşsız | Deneme |");
        report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        for (int n = 0; n < specs.Count; n++)
        {
            var spec = specs[n];
            var rng = new Random(seed + n * 7919);
            LevelData found = null; Metrics fm = null; int tries = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (found == null && tries < 4000 && sw.Elapsed.TotalMinutes < 6)
            {
                tries++;
                var d = RandomBoard(rng, spec, $"trial_{n + 1:00}");
                if (!spec.Shape(rng, d)) continue;
                var m = Measure(d, spec);
                if (m == null) continue;
                d.MoveLimit = m.MinMoves + spec.Slack;
                if (!spec.Accept(m, d)) continue;
                // Sınır konduktan sonra ilk hamle ölçüsünü yeniden al (tuzak oranı sınırla anlamlı).
                var m2 = Measure(d, spec, d.MoveLimit);
                if (m2 == null || !spec.Accept(m2, d)) continue;
                found = d; fm = m2;
            }
            if (found == null) { Console.WriteLine($"{spec.Type}: bulunamadı ({tries} deneme)"); report.AppendLine($"| {n + 1} | {spec.Type} | — | — | — | — | — | — | — | — | {tries} (bulunamadı) |"); continue; }
            found.Title = spec.Title;
            found.TrialType = spec.Type;
            found.Rules.AddRange(spec.Rules);
            found.Solution.Clear();
            foreach (var c in fm.Solution) found.Solution.Add(TrialStep(c));
            // Yıldız eşikleri: kayıtlı çözüm puanının %75'i ve tamamı.
            var st = LevelLoader.ToState(found);
            foreach (var c in fm.Solution) RulesEngine.TryApply(st, c);
            if (st.Status != GameStatus.Won) throw new InvalidOperationException(spec.Type + ": kayıtlı çözüm kazanmıyor");
            found.StarThresholds = new[] { Math.Max(1, (int)(st.Score * 0.75)), Math.Max(2, st.Score) };
            File.WriteAllText(Path.Combine(outDir, found.Id + ".json"), TrialJson(found), new UTF8Encoding(false));
            Console.WriteLine($"{found.Id} {spec.Type}: {found.Width}x{found.Height} hamle {found.MoveLimit} (en kısa {fm.MinMoves}) ilk hamle {fm.Winners}/{fm.Legal} açgözlü {(fm.GreedyWins ? "kazanır" : "kaybeder")} deneme {tries}");
            report.AppendLine($"| {n + 1} | {spec.Type} | {found.Width}×{found.Height} | {found.MoveLimit} | {fm.MinMoves} | {fm.Winners}/{fm.Legal} | {fm.Trap:P0} | {(fm.GreedyWins ? "kazanır" : "kaybeder")} | {(fm.BiggestFirstWins ? "kazanır" : "kaybeder")} | {(fm.SolvableWithoutSpecials ? "evet" : "hayır")} | {tries} |");
        }
        File.WriteAllText(Path.Combine(RepoRoot(), "docs", "v12", "DENEME_URETIM.md"), report.ToString(), new UTF8Encoding(false));
        return 0;
    }

    static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "KatKat"))) dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }

    static StepData TrialStep(Command c) => c.Type switch
    {
        CommandType.Tap => new StepData { Type = "tap", X = c.X, Y = c.Y },
        CommandType.UseSpecial => new StepData { Type = "use", Row = c.Row, X = c.X, Y = c.Y },
        CommandType.Roll => new StepData { Type = "roll", Row = c.Row },
        CommandType.Rotate => new StepData { Type = "rotate", X = c.X, Y = c.Y },
        _ => throw new InvalidOperationException("deneme setinde beklenmeyen adım " + c.Type),
    };

    /// <summary>Kümeli rastgele tahta: her katmanda renk %55 olasılıkla sol/üst komşudan kopyalanır.</summary>
    static LevelData RandomBoard(Random rng, TrialSpec spec, string id)
    {
        var d = new LevelData { Id = id, Width = spec.W, Height = spec.H, SchemaVersion = 2 };
        if (spec.Specials) d.Capabilities.Add("specials");
        var grid = new List<string>[spec.W, spec.H];
        int layers = spec.MaxLayers;
        var col = new int[spec.W, spec.H, layers];
        for (int l = 0; l < layers; l++)
            for (int y = 0; y < spec.H; y++)
                for (int x = 0; x < spec.W; x++)
                {
                    int c = rng.Next(spec.Colors);
                    double p = rng.NextDouble();
                    if (p < 0.30 && x > 0) c = col[x - 1, y, l];
                    else if (p < 0.55 && y > 0) c = col[x, y - 1, l];
                    col[x, y, l] = c;
                }
        for (int y = 0; y < spec.H; y++)
            for (int x = 0; x < spec.W; x++)
            {
                var cell = new CellData { X = x, Y = y, Active = true };
                int n = spec.MinLayers + rng.Next(spec.MaxLayers - spec.MinLayers + 1);
                for (int l = 0; l < n; l++) cell.Layers.Add(ColorNames[col[x, y, l]]);
                d.Cells.Add(cell);
            }
        return d;
    }

    static int ColorTotal(LevelData d, string color) => d.Cells.Sum(c => c.Layers.Count(l => l == color));

    /// <summary>Ölçüm: en kısa çözüm (sınır yoksa geniş), ilk hamle başına kazanılabilirlik, açgözlü bot.</summary>
    static Metrics Measure(LevelData d, TrialSpec spec, int limit = 0)
    {
        d.MoveLimit = limit > 0 ? limit : 12;
        LevelState s;
        try { s = LevelLoader.ToState(d); } catch (FormatException) { return null; }
        if (!RulesEngine.HasLegalMove(s)) return null;
        var sol = LevelSolver.Solve(s, spec.Budget);
        if (sol.Outcome != SolveOutcome.Solved) return null;
        var m = new Metrics { Solution = sol.Solution };
        m.MinMoves = sol.Solution.Count(c => c.Type != CommandType.UseSpecial);
        m.UsesTray = sol.Solution.Any(c => c.Type == CommandType.UseSpecial);
        if (limit <= 0) return m; // ilk geçiş: yalnız en kısa
        var moves = LevelSolver.Moves(s);
        foreach (var cmd in moves)
        {
            if (cmd.Type == CommandType.UseSpecial) continue;
            m.Legal++;
            var next = s.Clone();
            if (!RulesEngine.TryApply(next, cmd).Accepted) { m.Legal--; continue; }
            if (next.Status == GameStatus.Won) { m.Winners++; continue; }
            if (next.IsTerminal) continue;
            var r = LevelSolver.Solve(next, spec.Budget);
            if (r.Outcome == SolveOutcome.BudgetExceeded) return null; // belirsiz: aday reddedilir
            if (r.Outcome == SolveOutcome.Solved) m.Winners++;
        }
        m.GreedyWins = Play(s, Greedy);
        m.BiggestFirstWins = Play(s, Biggest);
        if (spec.Specials)
        {
            var ns = s.Clone();
            ns.SpecialsEnabled = false;
            var r = LevelSolver.Solve(ns, spec.Budget);
            m.SolvableWithoutSpecials = r.Outcome == SolveOutcome.Solved;
        }
        else m.SolvableWithoutSpecials = true;
        return m;
    }

    /// <summary>Açgözlü: en çok hedef ilerlemesi, eşitse en çok soyulan katman.</summary>
    static Command Greedy(LevelState s) => Best(s, (a, before) => Progress(a) * 1000 + Peeled(a, before));
    /// <summary>"Büyük grup önce": en çok soyulan katman.</summary>
    static Command Biggest(LevelState s) => Best(s, (a, before) => Peeled(a, before));

    static int Progress(LevelState s) => s.Goals.Sum(g => Math.Min(g.Progress, g.Amount));
    static int Peeled(LevelState after, LevelState before) => before.Board.TotalLayers() - after.Board.TotalLayers();

    static Command Best(LevelState s, Func<LevelState, LevelState, int> score)
    {
        Command best = default; int bs = int.MinValue; bool any = false;
        foreach (var cmd in LevelSolver.Moves(s))
        {
            var n = s.Clone();
            if (!RulesEngine.TryApply(n, cmd).Accepted) continue;
            int v = n.Status == GameStatus.Won ? int.MaxValue : n.Status == GameStatus.LostProtected ? int.MinValue + 1 : score(n, s);
            if (!any || v > bs) { best = cmd; bs = v; any = true; }
        }
        return any ? best : default;
    }

    static bool Play(LevelState start, Func<LevelState, Command> policy)
    {
        var s = start.Clone();
        for (int guard = 0; guard < 60 && !s.IsTerminal; guard++)
        {
            var c = policy(s);
            if (!RulesEngine.TryApply(s, c).Accepted) return false;
        }
        return s.Status == GameStatus.Won;
    }

    static GoalData Goal(string type, string color, int amount) => new GoalData { Type = type, Color = color, Amount = amount };

    /// <summary>Tahtada en çok (ya da en az) bulunan renk.</summary>
    static string ColorBy(LevelData d, bool most) =>
        (most ? ColorNames.OrderByDescending(c => ColorTotal(d, c)) : ColorNames.Where(c => ColorTotal(d, c) > 0).OrderBy(c => ColorTotal(d, c))).First();

    static List<TrialSpec> TrialSpecs() => new List<TrialSpec>
    {
        // 1 Tek dokunuş: 1 hamle; yalnız bir dokunuş zinciri başlatıp hedefi tamamlar.
        new TrialSpec
        {
            Type = "aha", Title = "Tek Dokunuş", W = 5, H = 6, Colors = 3, MinLayers = 3, MaxLayers = 3, Rules = new[] { "exact" },
            Shape = (r, d) =>
            {
                // Her ilk dokunuşun ilerlemesine bak; en iyisi tek başına olsun.
                d.MoveLimit = 1;
                var s = LevelLoader.ToState(d);
                var best = new Dictionary<string, List<int>>();
                foreach (var c in ColorNames) best[c] = new List<int>();
                foreach (var cmd in LevelSolver.Moves(s))
                {
                    var n = s.Clone();
                    var res = RulesEngine.TryApply(n, cmd);
                    if (!res.Accepted || res.Waves.Count < 3) { foreach (var c in ColorNames) best[c].Add(0); continue; }
                    foreach (var c in ColorNames)
                    {
                        ColorIds.TryParse(c, out var lc);
                        best[c].Add(res.Waves.Sum(w => w.Cells.Count(p => p.Removed == lc)));
                    }
                }
                foreach (var c in ColorNames.OrderBy(_ => r.Next()))
                {
                    var v = best[c].OrderByDescending(x => x).ToList();
                    if (v.Count >= 5 && v[0] >= 7 && v[0] - v[1] >= 2) { d.Goals.Add(Goal("collectColor", c, v[0])); return true; }
                }
                return false;
            },
            Slack = 0,
            Accept = (m, d) => m.MinMoves == 1 && (m.Legal == 0 || (m.Winners == 1 && m.Legal >= 5)),
        },
        // 2 Kesin hamle: pay yok, 4–5 hamle, kazandıran ilk hamle az.
        new TrialSpec
        {
            Type = "exact", Title = "Kesin Hamle", W = 5, H = 6, Colors = 4, MinLayers = 2, MaxLayers = 3, Rules = new[] { "exact" },
            Shape = (r, d) =>
            {
                var a = ColorBy(d, true);
                var b = ColorNames.Where(c => c != a).OrderByDescending(c => ColorTotal(d, c)).First();
                d.Goals.Add(Goal("collectColor", a, (int)(ColorTotal(d, a) * 0.7)));
                d.Goals.Add(Goal("collectColor", b, (int)(ColorTotal(d, b) * 0.6)));
                return true;
            },
            Slack = 0,
            Accept = (m, d) => m.MinMoves >= 4 && m.MinMoves <= 5 && (m.Legal == 0 || (m.Legal >= 5 && m.Trap >= 0.6)),
        },
        // 3 Ters düşünme: açgözlü ve "büyük grup önce" kaybeder.
        new TrialSpec
        {
            Type = "reverse", Title = "Ters Düşünme", W = 5, H = 6, Colors = 3, MinLayers = 3, MaxLayers = 3, Rules = new[] { "exact" },
            Shape = (r, d) =>
            {
                var a = ColorBy(d, true);
                d.Goals.Add(Goal("collectColor", a, (int)(ColorTotal(d, a) * 0.75)));
                return true;
            },
            Slack = 0,
            Accept = (m, d) => m.MinMoves >= 3 && m.MinMoves <= 5 && (m.Legal == 0 || (!m.GreedyWins && !m.BiggestFirstWins && m.Trap >= 0.5)),
        },
        // 4 Renk kıtlığı: az bulunan rengin neredeyse hepsi hedef.
        new TrialSpec
        {
            Type = "scarce", Title = "Renk Kıtlığı", W = 5, H = 6, Colors = 4, MinLayers = 2, MaxLayers = 3, Rules = new[] { "scarce" },
            Shape = (r, d) =>
            {
                // Bir rengi kıt yap: fazla katmanları diğer renklere çevirerek toplamı 7–10'a indir.
                var rare = ColorNames[r.Next(4)];
                int want = 7 + r.Next(4);
                var spots = new List<(CellData c, int i)>();
                foreach (var c in d.Cells) for (int i = 0; i < c.Layers.Count; i++) if (c.Layers[i] == rare) spots.Add((c, i));
                foreach (var (c, i) in spots.OrderBy(_ => r.Next()).Skip(want))
                    c.Layers[i] = ColorNames.Where(x => x != rare).ElementAt(r.Next(3));
                int t = ColorTotal(d, rare);
                if (t < 6 || t > 14) return false;
                d.Goals.Add(Goal("collectColor", rare, t - 1));
                return true;
            },
            Slack = 1,
            Accept = (m, d) => m.MinMoves >= 3 && m.MinMoves <= 6 && (m.Legal == 0 || m.Trap >= 0.5),
        },
        // 5 Zincir ustası: yalnız zincirle soyulan katmanlar sayılır.
        new TrialSpec
        {
            Type = "chain", Title = "Zincir Ustası", W = 5, H = 6, Colors = 3, MinLayers = 3, MaxLayers = 3, Rules = new[] { "chainOnly" },
            Shape = (r, d) =>
            {
                var a = ColorNames[r.Next(3)];
                d.Goals.Add(Goal("collectChain", a, 5 + r.Next(4)));
                return true;
            },
            Slack = 0,
            Accept = (m, d) => m.MinMoves >= 2 && m.MinMoves <= 4 && (m.Legal == 0 || m.Trap >= 0.6),
        },
        // 6 Kristal koruma: kristal yığınlar soyulursa kayıp; açgözlü kristali kırar.
        new TrialSpec
        {
            Type = "protect", Title = "Kristal Koruma", W = 5, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3, Rules = new[] { "protect" },
            Shape = (r, d) =>
            {
                var a = ColorBy(d, true);
                d.Goals.Add(Goal("collectColor", a, (int)(ColorTotal(d, a) * 0.55)));
                for (int k = 0; k < 3; k++) d.Cells[r.Next(d.Cells.Count)].Protected = true;
                return d.Cells.Count(c => c.Protected) >= 2;
            },
            Slack = 1,
            Accept = (m, d) => m.MinMoves >= 3 && m.MinMoves <= 5 && (m.Legal == 0 || (!m.BiggestFirstWins && m.Trap >= 0.5)),
        },
        // 7 Saf plan: özel taş yok, kesin hamle, daha büyük tahta.
        new TrialSpec
        {
            Type = "pure", Title = "Saf Plan", W = 6, H = 6, Colors = 4, MinLayers = 2, MaxLayers = 3, Rules = new[] { "noSpecials", "exact" },
            Shape = (r, d) =>
            {
                var a = ColorBy(d, true);
                var b = ColorNames.Where(c => c != a).OrderByDescending(c => ColorTotal(d, c)).First();
                d.Goals.Add(Goal("collectColor", a, (int)(ColorTotal(d, a) * 0.65)));
                d.Goals.Add(Goal("collectColor", b, (int)(ColorTotal(d, b) * 0.55)));
                return true;
            },
            Slack = 0,
            Accept = (m, d) => m.MinMoves >= 4 && m.MinMoves <= 6 && (m.Legal == 0 || m.Trap >= 0.6),
        },
        // 8 Doğru an: özel taş şart (özel taşsız çözülemez), tepsi doğru yerde kullanılmalı.
        new TrialSpec
        {
            Type = "resource", Title = "Doğru An", W = 6, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3, Specials = true, Rules = new[] { "exact" },
            Shape = (r, d) =>
            {
                var a = ColorBy(d, true);
                d.Goals.Add(Goal("collectColor", a, (int)(ColorTotal(d, a) * 0.8)));
                return true;
            },
            Slack = 0,
            Accept = (m, d) => m.MinMoves >= 3 && m.MinMoves <= 5 && m.UsesTray && (m.Legal == 0 || (!m.SolvableWithoutSpecials && m.Trap >= 0.4)),
        },
        // 9 Kilit sırası: renk kilitleri; doğru renk önce toplanmazsa hedef açılmaz.
        new TrialSpec
        {
            Type = "order", Title = "Kilit Sırası", W = 5, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3, Rules = new[] { "exact" },
            Shape = (r, d) =>
            {
                var target = ColorBy(d, true);
                var key = ColorNames.Take(3).Where(c => c != target).OrderBy(_ => r.Next()).First();
                // Hedef rengin üstte olduğu 2 hücre kilitlenir; kilit, anahtar rengi toplanınca açılır.
                var cand = d.Cells.Where(c => c.Layers.Count > 0 && c.Layers[^1] == target).OrderBy(_ => r.Next()).Take(2).ToList();
                if (cand.Count < 2) return false;
                foreach (var c in cand) { c.LockColor = key; c.LockRemaining = 3 + r.Next(3); }
                d.Goals.Add(Goal("collectColor", target, (int)(ColorTotal(d, target) * 0.7)));
                d.Capabilities.Add("colorLock");
                return true;
            },
            Slack = 0,
            Accept = (m, d) => m.MinMoves >= 3 && m.MinMoves <= 5 && (m.Legal == 0 || m.Trap >= 0.55),
        },
        // 10 Patron: kristal + zincir hedefi + kesin hamle, büyük tahta.
        new TrialSpec
        {
            Type = "boss", Title = "Patron: Liman Feneri", W = 6, H = 7, Colors = 3, MinLayers = 3, MaxLayers = 3, Rules = new[] { "protect", "chainOnly", "exact" },
            Budget = 900000,
            Shape = (r, d) =>
            {
                var a = ColorBy(d, true);
                var b = ColorNames.Take(3).Where(c => c != a).OrderBy(_ => r.Next()).First();
                d.Goals.Add(Goal("collectChain", b, 4 + r.Next(3)));
                d.Goals.Add(Goal("collectColor", a, (int)(ColorTotal(d, a) * 0.5)));
                for (int k = 0; k < 3; k++) d.Cells[r.Next(d.Cells.Count)].Protected = true;
                return d.Cells.Count(c => c.Protected) >= 2;
            },
            Slack = 0,
            Accept = (m, d) => m.MinMoves >= 5 && m.MinMoves <= 7 && (m.Legal == 0 || (!m.GreedyWins && m.Trap >= 0.6)),
        },
    };

    /// <summary>Deneme JSON'u (şema 2 + v12 alanları: trialType, rules, protected, collectChain).</summary>
    static string TrialJson(LevelData d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"schemaVersion\": 2,");
        sb.AppendLine($"  \"id\": \"{d.Id}\",");
        sb.AppendLine($"  \"capabilities\": [{string.Join(", ", d.Capabilities.Distinct().Select(c => $"\"{c}\""))}],");
        sb.AppendLine($"  \"title\": \"{Esc(d.Title)}\",");
        sb.AppendLine($"  \"trialType\": \"{d.TrialType}\",");
        sb.AppendLine($"  \"rules\": [{string.Join(", ", d.Rules.Select(r => $"\"{r}\""))}],");
        sb.AppendLine($"  \"width\": {d.Width},");
        sb.AppendLine($"  \"height\": {d.Height},");
        sb.AppendLine($"  \"moveLimit\": {d.MoveLimit},");
        sb.AppendLine("  \"rollEnabled\": false,");
        sb.AppendLine("  \"startingRoll\": false,");
        sb.AppendLine($"  \"stars\": [{d.StarThresholds[0]}, {d.StarThresholds[1]}],");
        sb.AppendLine("  \"goals\": [");
        sb.AppendLine(string.Join(",\n", d.Goals.Select(g => $"    {{ \"type\": \"{g.Type}\", \"color\": \"{g.Color}\", \"amount\": {g.Amount} }}")));
        sb.AppendLine("  ],");
        sb.AppendLine("  \"cells\": [");
        sb.AppendLine(string.Join(",\n", d.Cells.Select(c =>
            $"    {{ \"x\": {c.X}, \"y\": {c.Y}, \"active\": true, \"layers\": [{string.Join(", ", c.Layers.Select(l => $"\"{l}\""))}], \"overlay\": {(c.LockRemaining > 0 ? $"{{ \"kind\": \"colorLock\", \"color\": \"{c.LockColor}\", \"remaining\": {c.LockRemaining} }}" : "{ \"kind\": \"none\" }")}, \"hasBottomStar\": false{(c.Protected ? ", \"protected\": true" : "")} }}")));
        sb.AppendLine("  ],");
        sb.AppendLine("  \"solution\": [");
        sb.AppendLine(string.Join(",\n", d.Solution.Select(s => s.Type == "use"
            ? $"    {{ \"type\": \"use\", \"slot\": {s.Row}, \"x\": {s.X}, \"y\": {s.Y} }}"
            : $"    {{ \"type\": \"{s.Type}\", \"x\": {s.X}, \"y\": {s.Y} }}")));
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }
}

static partial class Program
{
    /// <summary>dotnet run -- show &lt;json&gt;: kayıtlı çözümü adım adım; her adımda tahta (üst/alt katmanlar) ve dalgalar.</summary>
    static int ShowSolution(string[] args)
    {
        var d = LevelParser.Parse(File.ReadAllText(args[1]), Path.GetFileName(args[1]));
        var s = LevelLoader.ToState(d);
        string Ch(LayerColor c) => c switch { LayerColor.Coral => "M", LayerColor.Blue => "T", LayerColor.Yellow => "i", _ => "L" };
        void Print()
        {
            for (int y = 0; y < s.Board.Height; y++)
            {
                var sb = new StringBuilder("  ");
                for (int x = 0; x < s.Board.Width; x++)
                {
                    var c = s.Board[x, y];
                    var st = string.Concat(Enumerable.Reverse(c.Layers).Select(Ch)); // üstten alta
                    sb.Append((c.Protected ? "*" : "") + (st.Length == 0 ? "." : st)).Append(new string(' ', Math.Max(1, 6 - st.Length - (c.Protected ? 1 : 0))));
                }
                Console.WriteLine(sb);
            }
            Console.WriteLine("  hedef: " + string.Join(", ", s.Goals.Select(g => $"{g.Type} {g.Color} {g.Progress}/{g.Amount}")) + $"  hamle {s.MovesLeft}");
        }
        Console.WriteLine($"{d.Id} {d.Title}  (sütun x soldan, satır y üstten; hücrede katmanlar ÜSTTEN alta: M=mercan T=turkuaz i=inci L=lila, *=kristal)");
        Print();
        int n = 0;
        foreach (var step in d.Solution)
        {
            var r = RulesEngine.TryApply(s, LevelLoader.ToCommand(step));
            Console.WriteLine($"\n{++n}. hamle: {step.Type} ({step.X},{step.Y})");
            foreach (var w in r.Waves)
                Console.WriteLine($"   dalga {w.Index} [{w.Kind}]: " + string.Join(" ", w.Cells.Select(c => $"({c.X},{c.Y}){Ch(c.Removed)}")));
            Print();
        }
        Console.WriteLine("\nsonuç: " + s.Status);
        return 0;
    }
}
