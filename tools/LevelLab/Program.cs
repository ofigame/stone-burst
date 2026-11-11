using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KatKat.Core;
using KatKat.Levels;

// Kat Kat seviye yazım aracı. Kaynak biçimi (levels_src/level_XXX.txt):
//   id: level_001
//   title: İlk Soyma
//   tutorial: İsteğe bağlı öğretici metni
//   moves: 4
//   roll: off | on | start
//   goals: coral 6, yellow 3, star 2
//   grid:
//   yc yc yc by        (her hücre ALTTAN ÜSTE harfler: c b y p; "." aktif boş, "#" inactive;
//   ...                 sonek "k1"/"k2" ataş, "*" dip yıldızı: "bck2", "yc*")
//   solution: 0,0 ; 1,2 ; roll 2
//
// gen <plan dosyası> [filtre]: tools/LevelLab/plan_21_60.txt profillerinden aday üretir (Generator.cs).
static partial class Program
{
    static string Root;

    static int Main(string[] args)
    {
        Root = FindRoot();
        string cmd = args.Length > 0 ? args[0] : "analyze";
        var filter = args.Length > 1 ? args[1] : null;
        var sources = Directory.GetFiles(Path.Combine(Root, Environment.GetEnvironmentVariable("KATKAT_SRC") ?? "levels_src"), "*.txt").OrderBy(f => f, StringComparer.Ordinal)
            .Where(f => filter == null || Path.GetFileName(f).Contains(filter)).ToList();
        switch (cmd)
        {
            case "build": return Build(sources);
            case "analyze": Build(sources); return Analyze(sources);
            case "report": Build(sources); return Report();
            case "tune": return Tune(sources, args);
            case "gen": return Generate(args);
            case "gen5": return GenerateV5(args);
            case "harden": return Harden(args);
            case "rebuild": return Rebuild(args);
            case "stress": return Stress(args);
            case "daily": return Daily(args);
            case "index": return WriteLevelIndex();
            case "zperef": return ZpeRef(args);
            case "trial": return Trial(args);
            case "pilot": return Pilot(args);
            case "v8": return V8(args);
            case "audit": return Audit(args);
            case "fairfix": return FairFix(args);
            case "daily3": return Daily3(args);
            case "dailyindex3": return DailyIndex3(args);
            case "show": return ShowSolution(args);
            case "chaincmp": return ChainCompare(args);
            case "chainfit": return ChainFit(args);
            case "chaindiag": return ChainDiag(args);
            case "fairlist": return FairList(args);
            case "chainease": return ChainEase(args);
            default: Console.Error.WriteLine("komut: build | analyze [filtre] | report | index | daily"); return 2;
        }
    }

    static string FindRoot()
    {
        var d = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Assets", "KatKat"))) d = d.Parent;
        if (d == null) throw new InvalidOperationException("Kat Kat proje kökü bulunamadı");
        return d.FullName;
    }

    /// <summary>v10 P1.6: ana bölüm dizin dosyasını (Resources/LevelIndex/levels_index.json) ana Levels klasöründen üretir.</summary>
    static int WriteLevelIndex()
    {
        var dir = Path.Combine(Root, "Assets", "KatKat", "Resources", "Levels");
        var names = Directory.GetFiles(dir, "level_*.json").Select(Path.GetFileNameWithoutExtension).ToList();
        var outDir = Path.Combine(Root, "Assets", "KatKat", "Resources", "LevelIndex");
        Directory.CreateDirectory(outDir);
        var path = Path.Combine(outDir, "levels_index.json");
        File.WriteAllText(path, KatKat.Levels.LevelIndex.Write(names), new UTF8Encoding(false));
        Console.WriteLine($"index: {names.Count} bölüm → {path}");
        return 0;
    }

    static string LevelsDir => Environment.GetEnvironmentVariable("KATKAT_OUT") is string o ? Path.Combine(Root, o) : Path.Combine(Root, "Assets", "KatKat", "Resources", "Levels");

    static int Build(List<string> sources)
    {
        Directory.CreateDirectory(LevelsDir);
        foreach (var src in sources)
        {
            var d = ParseSource(src);
            d.StarThresholds = StarThresholds(d);
            var path = Path.Combine(LevelsDir, d.Id + ".json");
            File.WriteAllText(path, ToJson(d), new UTF8Encoding(false));
        }
        Console.WriteLine($"build: {sources.Count} seviye → {LevelsDir}");
        return 0;
    }

    static int Analyze(List<string> sources)
    {
        int failures = 0;
        foreach (var src in sources)
        {
            var d = ParseSource(src);
            Console.WriteLine($"=== {d.Id} \"{d.Title}\" {d.Width}x{d.Height} hamle {d.MoveLimit} rulo {(d.RollEnabled ? (d.StartingRoll ? "start" : "on") : "off")}");
            var errors = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
            foreach (var e in errors) Console.WriteLine("  STATİK: " + e);
            if (errors.Count > 0) { failures++; continue; }
            var rep = LevelValidator.ReplaySolution(d);
            Console.WriteLine($"  kayıtlı çözüm: {(rep.Success ? "Won" : "HATA " + rep.Error)}; adım {rep.Steps}, dalga {rep.Waves}, katman {rep.LayersPeeled}, rulo {rep.RollsAwarded}/{rep.RollsUsed}; hedef {string.Join(", ", rep.Goals)}");
            if (!rep.Success) failures++;

            var state = LevelLoader.ToState(d);
            var best = LevelSolver.Solve(state);
            Console.WriteLine($"  BFS: {best.Outcome} {(best.Solution != null ? $"en kısa {best.Solution.Count} [{string.Join(" ; ", best.Solution.Select(Fmt))}]" : "")} düğüm {best.NodesExpanded}");

            var firsts = LevelSolver.Moves(state);
            var parts = new List<string>();
            int winning = 0;
            foreach (var m in firsts)
            {
                var s = state.Clone();
                var r = RulesEngine.TryApply(s, m);
                var sub = s.Status == GameStatus.Won ? new SolveResult { Outcome = SolveOutcome.Solved, Solution = new List<Command>() } : LevelSolver.Solve(s, 60000);
                bool win = sub.Outcome == SolveOutcome.Solved;
                if (win) winning++;
                string tag = sub.Outcome == SolveOutcome.Solved ? $"W{sub.Solution.Count + 1}" : sub.Outcome == SolveOutcome.Unsolvable ? "L" : "?";
                parts.Add($"{Fmt(m)}[{r.TappedGroup.Count}{(r.Waves.Count > 1 ? $"+{r.Waves.Count - 1}z" : "")}]={tag}");
            }
            Console.WriteLine($"  ilk hamleler {winning}/{firsts.Count} kazanılabilir: {string.Join("  ", parts)}");
        }
        Console.WriteLine(failures == 0 ? "SONUÇ: hepsi geçti" : $"SONUÇ: {failures} seviye HATALI");
        return failures == 0 ? 0 : 1;
    }

    static int Report()
    {
        var entries = new List<ValidationReport.Entry>();
        foreach (var f in Directory.GetFiles(LevelsDir, "*.json").OrderBy(f => f, StringComparer.Ordinal))
            entries.Add(ValidationReport.Check(Path.GetFileName(f), File.ReadAllText(f)));
        var md = ValidationReport.Markdown(entries, "tools/LevelLab (dotnet)", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        File.WriteAllText(Path.Combine(Root, "docs", "SEVIYE_DOGRULAMA.md"), md, new UTF8Encoding(false));
        int bad = entries.Count(e => !e.Passed);
        Console.WriteLine($"report: {entries.Count - bad}/{entries.Count} geçti → docs/SEVIYE_DOGRULAMA.md");
        // Tekrar filtresi: ayna/döndürme, renk değişimi veya yalnızca hamle limiti/hedef farkı.
        var levels = Directory.GetFiles(LevelsDir, "level_*.json").Select(f => LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f)));
        var dupes = LevelSignature.Duplicates(levels);
        foreach (var g in dupes) Console.WriteLine("TEKRAR: " + string.Join(", ", g));
        Console.WriteLine($"tekrar filtresi: {dupes.Count} grup");
        return bad == 0 && dupes.Count == 0 ? 0 : 1;
    }

    // tune <filtre> <yineleme> <boşluk> <kazanma oranı> <tohum>
    // Kaynak tahtanın hücre katman sayılarını ve renk kümesini koruyarak yerel değişikliklerle hedef profile yaklaşır.
    // Çıktı yalnızca öneridir: kaynağa elle kopyalanır ve analyze/report ile yeniden doğrulanır.
    static int Tune(List<string> sources, string[] args)
    {
        int iters = args.Length > 2 ? int.Parse(args[2]) : 300;
        int slack = args.Length > 3 ? int.Parse(args[3]) : 0;
        double ratio = args.Length > 4 ? double.Parse(args[4], CultureInfo.InvariantCulture) : 0.34;
        var rng = new Random(args.Length > 5 ? int.Parse(args[5]) : 1);
        var d = ParseSource(sources[0]);
        var colors = d.Cells.SelectMany(c => c.Layers).Distinct().ToList();
        var best = Clone(d);
        var (bestScore, bestInfo) = Score(best, slack, ratio);
        Console.WriteLine($"başlangıç {bestScore:0.0} {bestInfo}");
        for (int i = 0; i < iters; i++)
        {
            var cand = Clone(best);
            int ops = 1 + rng.Next(2);
            for (int k = 0; k < ops; k++) Mutate(cand, colors, rng);
            var (s, info) = Score(cand, slack, ratio);
            if (s >= bestScore)
            {
                if (s > bestScore) Console.WriteLine($"  #{i} {s:0.0} {info}");
                best = cand; bestScore = s; bestInfo = info;
            }
        }
        Console.WriteLine($"en iyi {bestScore:0.0} {bestInfo}");
        for (int y = 0; y < best.Height; y++)
        {
            var row = new List<string>();
            for (int x = 0; x < best.Width; x++)
            {
                var c = best.Cells.FirstOrDefault(cc => cc.X == x && cc.Y == y);
                row.Add(CellToken(c));
            }
            Console.WriteLine(string.Join(" ", row.Select(r => r.PadRight(3))));
        }
        return 0;
    }

    static LevelData Clone(LevelData d) => ParseJsonRoundTrip(d);

    static LevelData ParseJsonRoundTrip(LevelData d) => LevelParser.Parse(ToJson(d));

    static void Mutate(LevelData d, List<string> colors, Random rng)
    {
        var cells = d.Cells.Where(c => c.Layers.Count > 0).ToList();
        var a = cells[rng.Next(cells.Count)];
        switch (rng.Next(3))
        {
            case 0: a.Layers[rng.Next(a.Layers.Count)] = colors[rng.Next(colors.Count)]; break;
            case 1:
                if (a.Layers.Count > 1) { int i = rng.Next(a.Layers.Count), j = rng.Next(a.Layers.Count); (a.Layers[i], a.Layers[j]) = (a.Layers[j], a.Layers[i]); }
                break;
            default:
                var b = cells[rng.Next(cells.Count)];
                var tmp = a.Layers.ToList();
                a.Layers.Clear(); a.Layers.AddRange(b.Layers);
                b.Layers.Clear(); b.Layers.AddRange(tmp);
                break;
        }
    }

    static (double, string) Score(LevelData d, int slack, double ratio)
    {
        if (LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)).Count > 0) return (-1000, "statik hata");
        var st = LevelLoader.ToState(d);
        var sol = LevelSolver.Solve(st, 40000);
        if (sol.Outcome != SolveOutcome.Solved) return (-500, sol.Outcome.ToString());
        int shortest = sol.Solution.Count;
        var firsts = LevelSolver.Moves(st);
        int win = 0;
        foreach (var m in firsts)
        {
            var s = st.Clone();
            RulesEngine.TryApply(s, m);
            if (s.Status == GameStatus.Won || LevelSolver.Solve(s, 8000).Outcome == SolveOutcome.Solved) win++;
        }
        // Çözümdeki zincir dalgaları.
        var replay = st.Clone();
        int chains = 0, rolls = 0;
        foreach (var c in sol.Solution)
        {
            var r = RulesEngine.TryApply(replay, c);
            chains += r.Waves.Count - 1;
            if (r.RollConsumed) rolls++;
        }
        double wr = firsts.Count == 0 ? 0 : (double)win / firsts.Count;
        double score = -Math.Abs(d.MoveLimit - shortest - slack) * 10 - Math.Abs(wr - ratio) * 12 + Math.Min(firsts.Count, 5) + Math.Min(chains, 4) * 1.5 + (d.RollEnabled ? (rolls > 0 ? 4 : -6) : 0);
        return (score, $"en kısa {shortest}/{d.MoveLimit} ilk {win}/{firsts.Count} zincir {chains} rulo {rolls} [{string.Join(" ; ", sol.Solution.Select(Fmt))}]");
    }

    static string Fmt(Command c) => c.Type == CommandType.Tap ? $"{c.X},{c.Y}" : c.Type == CommandType.Rotate ? $"rot {c.X},{c.Y}"
        : c.Type == CommandType.UseSpecial ? $"use {c.Row},{c.X},{c.Y}" : $"roll {c.Row}";

    /// <summary>Kaynak dosyasındaki çözüm adımı yazımı: "x,y", "roll r", "rot x,y", "use yuva,x,y".</summary>
    static string FmtStep(StepData s) => s.Type == "tap" ? $"{s.X},{s.Y}" : s.Type == "rotate" ? $"rot {s.X},{s.Y}" : s.Type == "paint" ? $"paint {s.X},{s.Y},{s.Color[0]}"
        : s.Type == "use" ? $"use {s.Row},{s.X},{s.Y}" : $"roll {s.Row}";

    static StepData ToStep(Command c) => c.Type == CommandType.Tap ? new StepData { Type = "tap", X = c.X, Y = c.Y }
        : c.Type == CommandType.UseSpecial ? new StepData { Type = "use", Row = c.Row, X = c.X, Y = c.Y }
        : c.Type == CommandType.Rotate ? new StepData { Type = "rotate", X = c.X, Y = c.Y }
        : c.Type == CommandType.Paint ? new StepData { Type = "paint", X = c.X, Y = c.Y, Color = ColorIds.ToId(c.Color) } : new StepData { Type = "roll", Row = c.Row };

    static LevelData ParseSource(string path)
    {
        var d = new LevelData { SchemaVersion = 1 };
        var lines = File.ReadAllLines(path);
        var grid = new List<string>();
        bool inGrid = false;
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (line.Length == 0 || line.StartsWith("//")) continue;
            // Izgara satırı: "anahtar:" ile başlamayan satır (bağlı ataş K1:2 ve geçit G1:... hücreleri iki nokta içerir).
            if (inGrid && !System.Text.RegularExpressions.Regex.IsMatch(line, @"^[a-z]+:")) { grid.Add(line.Trim()); continue; }
            inGrid = false;
            int colon = line.IndexOf(':');
            string key = line.Substring(0, colon).Trim(), val = line.Substring(colon + 1).Trim();
            switch (key)
            {
                case "id": d.Id = val; break;
                case "title": d.Title = val; break;
                case "tutorial": d.Tutorial = val; break;
                case "moves": d.MoveLimit = int.Parse(val, CultureInfo.InvariantCulture); break;
                case "roll": d.RollEnabled = val != "off"; d.StartingRoll = val == "start"; break;
                case "specials": if (val == "on" && !d.Capabilities.Contains("specials")) d.Capabilities.Add("specials"); break;
                case "paint": d.Paint = int.Parse(val, CultureInfo.InvariantCulture); break;
                case "type": d.TrialType = val; break; // v8 bölüm türü (exact, chain, protect, boss…)
                case "rules": d.Rules.AddRange(val.Split(',').Select(r => r.Trim()).Where(r => r.Length > 0)); break;
                case "goals":
                    foreach (var g in val.Split(','))
                    {
                        var p = g.Trim().Split(' ');
                        if (p[0] == "chain") d.Goals.Add(new GoalData { Type = "collectChain", Color = p[1], Amount = int.Parse(p[2], CultureInfo.InvariantCulture) });
                        else if (p[0] == "star") d.Goals.Add(new GoalData { Type = "collectStars", Color = "", Amount = int.Parse(p[1], CultureInfo.InvariantCulture) });
                        else if (p[0] == "envelope" || p[0] == "sponge") d.Goals.Add(new GoalData { Type = "collectToken", Color = "", Kind = p[0], Amount = int.Parse(p[1], CultureInfo.InvariantCulture) });
                        else d.Goals.Add(new GoalData { Type = "collectColor", Color = p[0], Amount = int.Parse(p[1], CultureInfo.InvariantCulture) });
                    }
                    break;
                case "grid": inGrid = true; break;
                case "solution":
                    foreach (var s in val.Split(';'))
                    {
                        var t = s.Trim();
                        if (t.Length == 0) continue;
                        if (t.StartsWith("roll")) d.Solution.Add(new StepData { Type = "roll", Row = int.Parse(t.Substring(4).Trim(), CultureInfo.InvariantCulture) });
                        else if (t.StartsWith("use"))
                        {
                            var p = t.Substring(3).Trim().Split(',');
                            d.Solution.Add(new StepData { Type = "use", Row = int.Parse(p[0]), X = int.Parse(p[1]), Y = int.Parse(p[2]) });
                        }
                        else if (t.StartsWith("paint"))
                        {
                            var p = t.Substring(5).Trim().Split(',');
                            d.Solution.Add(new StepData { Type = "paint", X = int.Parse(p[0]), Y = int.Parse(p[1]), Color = ColorName(p[2].Trim()[0], path, 0, 0) });
                        }
                        else if (t.StartsWith("rot"))
                        {
                            var xy = t.Substring(3).Trim().Split(',');
                            d.Solution.Add(new StepData { Type = "rotate", X = int.Parse(xy[0]), Y = int.Parse(xy[1]) });
                        }
                        else
                        {
                            var xy = t.Split(',');
                            d.Solution.Add(new StepData { Type = "tap", X = int.Parse(xy[0]), Y = int.Parse(xy[1]) });
                        }
                    }
                    break;
                default: throw new FormatException($"{path}: bilinmeyen alan '{key}'");
            }
        }
        d.Height = grid.Count;
        for (int y = 0; y < grid.Count; y++)
        {
            var cells = grid[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (y == 0) d.Width = cells.Length;
            if (cells.Length != d.Width) throw new FormatException($"{path}: satır {y} genişliği {cells.Length}");
            for (int x = 0; x < cells.Length; x++)
            {
                if (cells[x] == "#") continue;
                if (cells[x].StartsWith("G"))
                {
                    // M11 geçit hücresi: G<id>:<katmanlar>, başta etkin değil.
                    var gp = cells[x].Substring(1).Split(':');
                    var gc = new CellData { X = x, Y = y, Active = false, GateId = int.Parse(gp[0], CultureInfo.InvariantCulture) };
                    foreach (var ch in gp[1]) gc.GateLayers.Add(ColorName(ch, path, x, y));
                    d.Cells.Add(gc);
                    continue;
                }
                d.Cells.Add(ParseCell(cells[x], x, y, path));
            }
        }
        if (NeedsV2(d))
        {
            d.SchemaVersion = 2;
            var caps = CapabilitiesOf(d);
            d.Capabilities.Clear();
            d.Capabilities.AddRange(caps);
        }
        return d;
    }

    static CellData ParseCell(string token, int x, int y, string path)
    {
        var c = new CellData { X = x, Y = y, Active = true };
        if (token == ".") return c;
        var t = token;
        if (t.EndsWith("P")) { c.Protected = true; t = t.Substring(0, t.Length - 1); } // v8 kristal yığın
        if (t.EndsWith("*")) { c.HasBottomStar = true; t = t.Substring(0, t.Length - 1); }
        if (t.EndsWith("R")) { c.HasRotor = true; t = t.Substring(0, t.Length - 1); }
        // M11 düğme B<geçit>, M10 bağlı ataş K<id>:<vuruş>, M09 sünger S<renkler> (bu sırayla sondan kesilir).
        int bi = t.IndexOf('B');
        if (bi >= 0) { c.ButtonGate = int.Parse(t.Substring(bi + 1), CultureInfo.InvariantCulture); t = t.Substring(0, bi); }
        int ki = t.IndexOf('K');
        if (ki >= 0) { var kp = t.Substring(ki + 1).Split(':'); c.LinkId = int.Parse(kp[0], CultureInfo.InvariantCulture); c.LinkHits = int.Parse(kp[1], CultureInfo.InvariantCulture); t = t.Substring(0, ki); }
        int si = t.IndexOf('S');
        if (si >= 0) { foreach (var ch in t.Substring(si + 1)) c.SpongeColors.Add(ColorName(ch, path, x, y)); t = t.Substring(0, si); }
        // Örtü sonekleri: k<n> ataş, i<n> buz, L<renk harfi><n> renk kilidi (ör. "ybLc3").
        int k = t.IndexOf('k');
        if (k >= 0) { c.ClipHitsRemaining = int.Parse(t.Substring(k + 1), CultureInfo.InvariantCulture); t = t.Substring(0, k); }
        int ice = t.IndexOf('i');
        if (ice >= 0) { c.IceHits = int.Parse(t.Substring(ice + 1), CultureInfo.InvariantCulture); t = t.Substring(0, ice); }
        int env = t.IndexOf('E');
        if (env >= 0) { c.EnvelopeColor = ColorName(t[env + 1], path, x, y); t = t.Substring(0, env); }
        int lk = t.IndexOf('L');
        if (lk >= 0)
        {
            c.LockColor = ColorName(t[lk + 1], path, x, y);
            c.LockRemaining = int.Parse(t.Substring(lk + 2), CultureInfo.InvariantCulture);
            t = t.Substring(0, lk);
        }
        foreach (var ch in t) c.Layers.Add(ColorName(ch, path, x, y));
        return c;
    }

    static string ColorName(char ch, string path, int x, int y) =>
        ch switch { 'c' => "coral", 'b' => "blue", 'y' => "yellow", 'p' => "purple", _ => throw new FormatException($"{path}: ({x},{y}) harf '{ch}'") };

    /// <summary>schemaVersion 2 yalnızca yeni örtü (buz/kilit) kullanan bölümlerde; 1–60 biçimi bayt bayt aynı kalır.</summary>
    static bool NeedsV2(LevelData d) => d.Capabilities.Contains("specials") || d.Paint > 0
        || !string.IsNullOrEmpty(d.TrialType) || d.Rules.Count > 0 || d.Goals.Any(g => g.Type == "collectChain") || d.Cells.Any(c => c.Protected)
        || d.Cells.Any(c => c.SpongeColors.Count > 0 || c.LinkHits > 0 || c.ButtonGate > 0 || c.GateId > 0) || d.Cells.Any(c => c.IceHits > 0 || c.LockRemaining > 0 || !string.IsNullOrEmpty(c.EnvelopeColor) || c.HasRotor);

    static List<string> CapabilitiesOf(LevelData d)
    {
        var caps = new List<string>();
        if (d.Cells.Any(c => c.ClipHitsRemaining > 0)) caps.Add("clip");
        if (d.Cells.Any(c => c.HasBottomStar) || d.Goals.Any(g => g.Type == "collectStars")) caps.Add("star");
        if (d.Cells.Any(c => c.IceHits > 0)) caps.Add("ice");
        if (d.Cells.Any(c => c.LockRemaining > 0)) caps.Add("colorLock");
        if (d.Cells.Any(c => !string.IsNullOrEmpty(c.EnvelopeColor))) caps.Add("envelope");
        if (d.Cells.Any(c => c.HasRotor)) caps.Add("rotor");
        if (d.Cells.Any(c => c.SpongeColors.Count > 0)) caps.Add("sponge");
        if (d.Cells.Any(c => c.LinkHits > 0)) caps.Add("linkedClip");
        if (d.Cells.Any(c => c.ButtonGate > 0 || c.GateId > 0)) caps.Add("gate");
        if (d.Paint > 0) caps.Add("paint");
        if (d.Capabilities.Contains("specials")) caps.Add("specials");
        return caps;
    }

    /// <summary>Kaynak biçimindeki hücre yazımı (ParseCell'in tersi).</summary>
    static string CellToken(CellData c)
    {
        if (c != null && !c.Active && c.GateId > 0) return "G" + c.GateId + ":" + string.Concat(c.GateLayers.Select(l => l[0]));
        if (c == null || !c.Active) return "#";
        if (c.Layers.Count == 0) return ".";
        var s = string.Concat(c.Layers.Select(l => l[0]));
        if (c.ClipHitsRemaining > 0) s += "k" + c.ClipHitsRemaining;
        if (c.IceHits > 0) s += "i" + c.IceHits;
        if (c.LockRemaining > 0) s += "L" + c.LockColor[0] + c.LockRemaining;
        if (!string.IsNullOrEmpty(c.EnvelopeColor)) s += "E" + c.EnvelopeColor[0];
        if (c.SpongeColors.Count > 0) s += "S" + string.Concat(c.SpongeColors.Select(l => l[0]));
        if (c.LinkHits > 0) s += "K" + c.LinkId + ":" + c.LinkHits;
        if (c.ButtonGate > 0) s += "B" + c.ButtonGate;
        if (c.HasRotor) s += "R";
        if (c.HasBottomStar) s += "*";
        if (c.Protected) s += "P";
        return s;
    }

    static string Overlay(CellData c)
    {
        if (c.ClipHitsRemaining > 0) return $"{{ \"kind\": \"clip\", \"hits\": {c.ClipHitsRemaining} }}";
        if (c.IceHits > 0) return $"{{ \"kind\": \"ice\", \"hits\": {c.IceHits} }}";
        if (c.LockRemaining > 0) return $"{{ \"kind\": \"colorLock\", \"color\": \"{c.LockColor}\", \"remaining\": {c.LockRemaining} }}";
        if (!string.IsNullOrEmpty(c.EnvelopeColor)) return $"{{ \"kind\": \"envelope\", \"sealColor\": \"{c.EnvelopeColor}\" }}";
        if (c.SpongeColors.Count > 0) return $"{{ \"kind\": \"sponge\", \"colors\": [{string.Join(", ", c.SpongeColors.Select(l => $"\"{l}\""))}] }}";
        if (c.LinkHits > 0) return $"{{ \"kind\": \"linkedClip\", \"linkId\": {c.LinkId}, \"hits\": {c.LinkHits} }}";
        return "{ \"kind\": \"none\" }";
    }

    static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    static string ToJson(LevelData d)
    {
        var sb = new StringBuilder();
        bool v2 = NeedsV2(d);
        sb.AppendLine("{");
        sb.AppendLine($"  \"schemaVersion\": {(v2 ? 2 : 1)},");
        sb.AppendLine($"  \"id\": \"{Esc(d.Id)}\",");
        if (v2) sb.AppendLine($"  \"capabilities\": [{string.Join(", ", CapabilitiesOf(d).Select(c => $"\"{c}\""))}],");
        if (!string.IsNullOrEmpty(d.Title)) sb.AppendLine($"  \"title\": \"{Esc(d.Title)}\",");
        if (!string.IsNullOrEmpty(d.Tutorial)) sb.AppendLine($"  \"tutorial\": \"{Esc(d.Tutorial)}\",");
        if (!string.IsNullOrEmpty(d.TrialType)) sb.AppendLine($"  \"trialType\": \"{d.TrialType}\",");
        if (d.Rules.Count > 0) sb.AppendLine($"  \"rules\": [{string.Join(", ", d.Rules.Select(r => $"\"{r}\""))}],");
        sb.AppendLine($"  \"width\": {d.Width},");
        sb.AppendLine($"  \"height\": {d.Height},");
        sb.AppendLine($"  \"moveLimit\": {d.MoveLimit},");
        sb.AppendLine($"  \"rollEnabled\": {(d.RollEnabled ? "true" : "false")},");
        sb.AppendLine($"  \"startingRoll\": {(d.StartingRoll ? "true" : "false")},");
        if (d.StarThresholds != null) sb.AppendLine($"  \"stars\": [{d.StarThresholds[0]}, {d.StarThresholds[1]}],");
        if (d.Paint > 0) sb.AppendLine($"  \"paint\": {d.Paint},");
        sb.AppendLine("  \"goals\": [");
        sb.AppendLine(string.Join(",\n", d.Goals.Select(g => g.Type == "collectToken"
            ? $"    {{ \"type\": \"collectToken\", \"kind\": \"{g.Kind}\", \"amount\": {g.Amount} }}"
            : $"    {{ \"type\": \"{g.Type}\", \"color\": \"{g.Color}\", \"amount\": {g.Amount} }}")));
        sb.AppendLine("  ],");
        sb.AppendLine("  \"cells\": [");
        sb.AppendLine(string.Join(",\n", d.Cells.Select(c => v2 && c.GateId > 0 && !c.Active
            ? $"    {{ \"x\": {c.X}, \"y\": {c.Y}, \"active\": false, \"gate\": {c.GateId}, \"gateLayers\": [{string.Join(", ", c.GateLayers.Select(l => $"\"{l}\""))}] }}"
            : v2
            ? $"    {{ \"x\": {c.X}, \"y\": {c.Y}, \"active\": true, \"layers\": [{string.Join(", ", c.Layers.Select(l => $"\"{l}\""))}], \"overlay\": {Overlay(c)}, \"hasBottomStar\": {(c.HasBottomStar ? "true" : "false")}{(c.Protected ? ", \"protected\": true" : "")}{(c.HasRotor ? ", \"base\": { \"kind\": \"rotor\" }" : c.ButtonGate > 0 ? $", \"base\": {{ \"kind\": \"button\", \"gate\": {c.ButtonGate} }}" : "")} }}"
            : $"    {{ \"x\": {c.X}, \"y\": {c.Y}, \"active\": true, \"layers\": [{string.Join(", ", c.Layers.Select(l => $"\"{l}\""))}], \"clipHitsRemaining\": {c.ClipHitsRemaining}, \"hasBottomStar\": {(c.HasBottomStar ? "true" : "false")} }}")));
        sb.AppendLine("  ],");
        sb.AppendLine("  \"solution\": [");
        sb.AppendLine(string.Join(",\n", d.Solution.Select(s => s.Type == "paint"
            ? $"    {{ \"type\": \"paint\", \"x\": {s.X}, \"y\": {s.Y}, \"color\": \"{s.Color}\" }}"
            : s.Type == "use"
            ? $"    {{ \"type\": \"use\", \"slot\": {s.Row}, \"x\": {s.X}, \"y\": {s.Y} }}"
            : s.Type == "tap" || s.Type == "rotate"
            ? $"    {{ \"type\": \"{s.Type}\", \"x\": {s.X}, \"y\": {s.Y} }}"
            : $"    {{ \"type\": \"roll\", \"row\": {s.Row} }}")));
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
