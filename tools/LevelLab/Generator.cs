using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// Profil tabanlı aday üretimi (belge Bölüm 15: üretici adayları çözücü onayı olmadan yayına giremez).
// Her profil satırı bir bölümü tarif eder; üretici rastgele "leke" boyamayla bir tahta kurar, sonra yerel
// değişikliklerle (renk, katman takası, ataş/yıldız yeri) hedef profile tırmanır. Çıktı kaynak dosyasıdır:
// en kısa çözüm BFS'ten kayıtlı çözüm olarak yazılır, hamle limiti = en kısa + boşluk. Sonra analyze/report
// ile gerçek kural motoru üzerinden yeniden doğrulanır. Var olan kaynak dosyalar üzerine yazılmaz
// (onaylı seviyeler sessizce yeniden üretilmez); yeniden üretmek için dosyayı elle sil.
//
// Profil satırı:  021 | anahtar=değer | ...
//   title, tutorial   metin
//   size=4|5|6        kare tahta
//   layers=2-3        hücre başına katman aralığı
//   colors=cby        kullanılan renk harfleri
//   clip1=N clip2=N   tek/iki hasarlı ataş sayısı
//   ice1=N ice2=N     tek/iki vuruşlu buz sayısı (Premium-v2 M05)
//   lock=c:3,b:4      renk kilitleri: renk ve gereken katman (Premium-v2 M06)
//   env=c,b           mühürlü zarflar ve mühür renkleri; hepsi collectToken(envelope) hedefi olur (M07)
//   rotor=N           döner taban sayısı (en az iki katmanlı hücrelere); rotorMin=N çözümde en az döndürme (M08)
//   seed=N            sabit tohum (varsayılan 1); aynı profil + tohum aynı adayı üretir
//   stars=N           dip yıldızı sayısı (hepsi hedef olur)
//   holes=N           inactive hücre sayısı
//   roll=off|on|start
//   goals=c:0.7,b:0.5 renk hedefi, tahtadaki toplamın oranı
//   len=3-5           en kısa çözüm uzunluğu aralığı
//   slack=1           hamle limiti - en kısa çözüm
//   win=0.4           kazandırabilen ilk hamle oranı hedefi
//   chains=1          en kısa çözümde istenen en az zincir dalgası
//   iters=120         tırmanma adımı
static partial class Program
{
    sealed class Profile
    {
        public int Number;
        public Dictionary<string, string> Kv = new Dictionary<string, string>();
        public string S(string k, string def = "") => Kv.TryGetValue(k, out var v) ? v : def;
        public int I(string k, int def = 0) => Kv.TryGetValue(k, out var v) ? int.Parse(v, CultureInfo.InvariantCulture) : def;
        public double D(string k, double def = 0) => Kv.TryGetValue(k, out var v) ? double.Parse(v, CultureInfo.InvariantCulture) : def;
        public (int, int) R(string k, int a, int b)
        {
            if (!Kv.TryGetValue(k, out var v)) return (a, b);
            var p = v.Split('-');
            int lo = int.Parse(p[0], CultureInfo.InvariantCulture);
            return (lo, p.Length > 1 ? int.Parse(p[1], CultureInfo.InvariantCulture) : lo);
        }
    }

    sealed class Eval
    {
        public double Score = -1e9;
        public string Info = "";
        public List<Command> Solution;
        public int MoveLimit;
    }

    static int Generate(string[] args)
    {
        var planPath = Path.Combine(Root, args.Length > 1 ? args[1] : "tools/LevelLab/plan_21_60.txt");
        var filter = args.Length > 2 ? args[2] : null;
        var profiles = new List<Profile>();
        foreach (var raw in File.ReadAllLines(planPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;
            var parts = line.Split('|').Select(p => p.Trim()).ToArray();
            var pr = new Profile { Number = int.Parse(parts[0], CultureInfo.InvariantCulture) };
            foreach (var p in parts.Skip(1))
            {
                int eq = p.IndexOf('=');
                pr.Kv[p.Substring(0, eq).Trim()] = p.Substring(eq + 1).Trim();
            }
            if (filter == null || pr.Number.ToString("000").Contains(filter)) profiles.Add(pr);
        }

        var results = new string[profiles.Count];
        int failed = 0;
        Parallel.For(0, profiles.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            var pr = profiles[i];
            var path = Path.Combine(Root, "levels_src", $"level_{pr.Number:000}.txt");
            if (File.Exists(path)) { results[i] = $"{pr.Number:000}: kaynak var, atlandı"; return; }
            var (d, ev) = Climb(pr);
            if (ev.Solution == null || ev.Score < -100)
            {
                results[i] = $"{pr.Number:000}: UYGUN ADAY BULUNAMADI ({ev.Info})";
                System.Threading.Interlocked.Increment(ref failed);
                return;
            }
            File.WriteAllText(path, ToSource(d, ev), new UTF8Encoding(false));
            results[i] = $"{pr.Number:000}: skor {ev.Score:0.0} {ev.Info}";
        });
        foreach (var r in results) Console.WriteLine(r);
        return failed == 0 ? 0 : 1;
    }

    static (LevelData, Eval) Climb(Profile pr)
    {
        var rng = new Random(pr.Number * 7919 + pr.I("seed", 1));
        LevelData best = null;
        Eval bestEv = new Eval();
        // Birkaç bağımsız başlangıç (en az biri çözülebilir olana dek); en iyisinden tırmanılır.
        for (int s = 0; s < 8 || (bestEv.Solution == null && s < 80); s++)
        {
            var d = RandomBoard(pr, rng);
            var ev = Evaluate(pr, d);
            if (ev.Score > bestEv.Score) { best = d; bestEv = ev; }
        }
        int iters = pr.I("iters", 400);
        for (int i = 0; i < iters && best != null; i++)
        {
            var cand = Clone(best);
            int ops = 1 + rng.Next(2);
            for (int k = 0; k < ops; k++) MutateGen(cand, pr, rng);
            var ev = Evaluate(pr, cand);
            if (ev.Score >= bestEv.Score) { best = cand; bestEv = ev; }
        }
        if (best != null && bestEv.Solution != null) ApplyEval(best, pr, bestEv);
        return (best, bestEv);
    }

    static string Letters(Profile pr) => pr.S("colors", "cby");

    static string ColorOf(char ch) => ch switch { 'c' => "coral", 'b' => "blue", 'y' => "yellow", 'p' => "purple", _ => throw new FormatException("renk harfi " + ch) };

    static LevelData RandomBoard(Profile pr, Random rng)
    {
        int n = pr.I("size", 5);
        var (lmin, lmax) = pr.R("layers", 2, 3);
        var letters = Letters(pr);
        var d = new LevelData { SchemaVersion = 1, Id = $"level_{pr.Number:000}", Title = pr.S("title"), Width = n, Height = n };
        var holes = new HashSet<int>();
        int holeCount = pr.I("holes");
        while (holes.Count < holeCount) holes.Add(rng.Next(n * n));
        var depth = new int[n * n];
        for (int i = 0; i < n * n; i++)
        {
            if (holes.Contains(i)) continue;
            depth[i] = rng.Next(lmin, lmax + 1);
            d.Cells.Add(new CellData { X = i % n, Y = i / n, Active = true });
        }
        // Katman düzeyi başına leke boyama: bağlı küçük bölgeler aynı rengi alır, böylece gruplar oluşur.
        var byIndex = d.Cells.ToDictionary(c => c.Y * n + c.X);
        var colorAt = new Dictionary<(int, int), char>();
        for (int level = 0; level < lmax; level++)
        {
            var open = byIndex.Keys.Where(i => depth[i] > level).OrderBy(_ => rng.Next()).ToList();
            var done = new HashSet<int>();
            foreach (var seed in open)
            {
                if (done.Contains(seed)) continue;
                char col = letters[rng.Next(letters.Length)];
                int size = 2 + rng.Next(4);
                var frontier = new List<int> { seed };
                while (frontier.Count > 0 && size > 0)
                {
                    int j = frontier[rng.Next(frontier.Count)];
                    frontier.Remove(j);
                    if (done.Contains(j)) continue;
                    done.Add(j);
                    colorAt[(j, level)] = col;
                    size--;
                    foreach (var nb in Neighbours(j, n))
                        if (byIndex.ContainsKey(nb) && depth[nb] > level && !done.Contains(nb)) frontier.Add(nb);
                }
            }
        }
        foreach (var kv in byIndex)
            for (int level = 0; level < depth[kv.Key]; level++) kv.Value.Layers.Add(ColorOf(colorAt[(kv.Key, level)]));

        var filled = d.Cells.Where(c => c.Layers.Count > 0).OrderBy(_ => rng.Next()).ToList();
        int at = 0;
        for (int k = 0; k < pr.I("clip2"); k++) filled[at++].ClipHitsRemaining = 2;
        for (int k = 0; k < pr.I("clip1"); k++) filled[at++].ClipHitsRemaining = 1;
        for (int k = 0; k < pr.I("ice2"); k++) filled[at++].IceHits = 2;
        for (int k = 0; k < pr.I("ice1"); k++) filled[at++].IceHits = 1;
        // lock=c:3,b:4 → mercan 3 ve mavi 4 isteyen iki kilit.
        foreach (var l in pr.S("lock").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = l.Split(':');
            filled[at].LockColor = ColorOf(p[0].Trim()[0]);
            filled[at++].LockRemaining = int.Parse(p[1], CultureInfo.InvariantCulture);
        }
        // env=c,b → mercan ve mavi mühürlü iki zarf (hepsi hedef olur).
        foreach (var e in pr.S("env").Split(',', StringSplitOptions.RemoveEmptyEntries))
            filled[at++].EnvelopeColor = ColorOf(e.Trim()[0]);
        for (int k = 0; k < pr.I("stars"); k++) filled[at++].HasBottomStar = true;
        // rotor=N: en az iki katmanlı, örtüsüz ve yıldızsız hücrelere döner taban.
        var rotorCells = filled.Skip(at).Where(c => c.Layers.Count >= 2).Take(pr.I("rotor")).ToList();
        foreach (var c in rotorCells) c.HasRotor = true;
        Normalize(d);
        return d;
    }

    /// <summary>Yeni örtü varsa şema 2 ve yetenek listesi (1–60 biçimine dokunmaz).</summary>
    static void Normalize(LevelData d)
    {
        d.Capabilities.Clear();
        d.SchemaVersion = NeedsV2(d) ? 2 : 1;
        if (d.SchemaVersion == 2) d.Capabilities.AddRange(CapabilitiesOf(d));
    }

    static bool Covered(CellData c) => c.ClipHitsRemaining > 0 || c.IceHits > 0 || c.LockRemaining > 0 || !string.IsNullOrEmpty(c.EnvelopeColor) || c.SpongeColors.Count > 0 || c.LinkHits > 0;

    static IEnumerable<int> Neighbours(int i, int n)
    {
        int x = i % n, y = i / n;
        if (x > 0) yield return i - 1;
        if (x < n - 1) yield return i + 1;
        if (y > 0) yield return i - n;
        if (y < n - 1) yield return i + n;
    }

    static void MutateGen(LevelData d, Profile pr, Random rng)
    {
        var letters = Letters(pr);
        var cells = d.Cells.Where(c => c.Layers.Count > 0).ToList();
        var a = cells[rng.Next(cells.Count)];
        switch (rng.Next(6))
        {
            case 0:
            case 1: a.Layers[rng.Next(a.Layers.Count)] = ColorOf(letters[rng.Next(letters.Length)]); break;
            case 2:
                if (a.Layers.Count > 1) { int i = rng.Next(a.Layers.Count), j = rng.Next(a.Layers.Count); (a.Layers[i], a.Layers[j]) = (a.Layers[j], a.Layers[i]); }
                break;
            case 3:
            {
                var b = cells[rng.Next(cells.Count)];
                var tmp = a.Layers.ToList();
                a.Layers.Clear(); a.Layers.AddRange(b.Layers);
                b.Layers.Clear(); b.Layers.AddRange(tmp);
                break;
            }
            default:
            {
                // Örtüyü (ataş/buz/kilit) ya da yıldızı başka dolu hücreye taşı.
                var marked = cells.Where(c => Covered(c) || c.HasBottomStar || c.HasRotor).ToList();
                var free = cells.Where(c => !Covered(c) && !c.HasBottomStar && !c.HasRotor && c.ButtonGate == 0).ToList();
                if (marked.Count == 0 || free.Count == 0) goto case 0;
                var from = marked[rng.Next(marked.Count)];
                var to = free[rng.Next(free.Count)];
                to.ClipHitsRemaining = from.ClipHitsRemaining; to.HasBottomStar = from.HasBottomStar;
                to.IceHits = from.IceHits; to.LockColor = from.LockColor; to.LockRemaining = from.LockRemaining; to.EnvelopeColor = from.EnvelopeColor;
                to.HasRotor = from.HasRotor; from.HasRotor = false;
                from.ClipHitsRemaining = 0; from.HasBottomStar = false;
                from.IceHits = 0; from.LockColor = null; from.LockRemaining = 0; from.EnvelopeColor = null;
                break;
            }
        }
    }

    /// <summary>Hedefleri ve rulo ayarını profile göre yazar (hamle limiti değerlendirmeden gelir).</summary>
    static void SetGoalsAndRoll(LevelData d, Profile pr, int moveLimit)
    {
        d.Goals.Clear();
        foreach (var g in pr.S("goals").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = g.Split(':');
            var color = ColorOf(p[0].Trim()[0]);
            int total = d.Cells.Sum(c => c.Layers.Count(l => l == color));
            int amount = Math.Max(3, (int)Math.Round(total * double.Parse(p[1], CultureInfo.InvariantCulture)));
            d.Goals.Add(new GoalData { Type = "collectColor", Color = color, Amount = Math.Min(amount, Math.Max(1, total)) });
        }
        int stars = d.Cells.Count(c => c.HasBottomStar);
        if (stars > 0) d.Goals.Add(new GoalData { Type = "collectStars", Color = "", Amount = stars });
        int envelopes = d.Cells.Count(c => !string.IsNullOrEmpty(c.EnvelopeColor));
        if (envelopes > 0) d.Goals.Add(new GoalData { Type = "collectToken", Color = "", Kind = "envelope", Amount = envelopes });
        int sponges = d.Cells.Count(c => c.SpongeColors.Count > 0);
        if (sponges > 0) d.Goals.Add(new GoalData { Type = "collectToken", Color = "", Kind = "sponge", Amount = sponges });
        var roll = pr.S("roll", "off");
        d.RollEnabled = roll != "off";
        d.StartingRoll = roll == "start";
        d.MoveLimit = moveLimit;
        d.Tutorial = pr.S("tutorial", null);
        d.Title = pr.S("title");
    }

    static Eval Evaluate(Profile pr, LevelData d)
    {
        var ev = new Eval();
        SetGoalsAndRoll(d, pr, 16);
        if (d.Goals.Count == 0) { ev.Info = "hedef yok"; return ev; }
        Normalize(d);
        var errors = LevelValidator.ValidateStatic(d, d.SchemaVersion >= 2 ? LevelValidator.ScopeFor(d) : LevelValidator.SecondStage);
        if (errors.Count > 0) { ev.Score = -1000; ev.Info = "statik: " + errors[0]; return ev; }

        var probe = LevelLoader.ToState(d);
        var sol = LevelSolver.Solve(probe, pr.I("budget", 50000));
        if (sol.Outcome != SolveOutcome.Solved || sol.Solution.Count == 0) { ev.Score = -500; ev.Info = sol.Outcome.ToString(); return ev; }
        int shortest = sol.Solution.Count;
        int limit = shortest + pr.I("slack", 1);
        d.MoveLimit = limit;
        var st = LevelLoader.ToState(d);

        var firsts = LevelSolver.Moves(st);
        int win = 0;
        foreach (var m in firsts)
        {
            var s = st.Clone();
            RulesEngine.TryApply(s, m);
            if (s.Status == GameStatus.Won || (!s.IsTerminal && LevelSolver.Solve(s, pr.I("budget1", 12000)).Outcome == SolveOutcome.Solved)) win++;
        }

        var replay = st.Clone();
        // Örtülü hücreler: çözüm en az birini açıp altındaki katmanı soymalı (mekanik gerçekten kullanılsın).
        var clipped = new HashSet<(int, int)>(d.Cells.Where(Covered).Select(c => (c.X, c.Y)));
        int chains = 0, rolls = 0, clipUse = 0, unlocks = 0, rotates = 0;
        foreach (var c in sol.Solution)
        {
            if (c.Type == CommandType.Rotate) rotates++;
            var r = RulesEngine.TryApply(replay, c);
            chains += r.Waves.Count(w => w.Kind == WaveKind.Chain);
            if (r.RollConsumed) rolls++;
            foreach (var w in r.Waves)
            {
                unlocks += w.ClipHits.Count(h => h.RemainingAfter == 0) + w.IceHits.Count(h => h.RemainingAfter == 0) + w.LockHits.Count(h => h.RemainingAfter == 0) + w.EnvelopeOpens.Count;
                clipUse += w.Cells.Count(p => clipped.Remove((p.X, p.Y)));
            }
        }

        double wr = firsts.Count == 0 ? 0 : (double)win / firsts.Count;
        var (lenLo, lenHi) = pr.R("len", 3, 6);
        int wantChains = pr.I("chains");
        double score = 0;
        score -= Math.Abs(wr - pr.D("win", 0.4)) * 14;
        score -= shortest < lenLo ? (lenLo - shortest) * 6 : shortest > lenHi ? (shortest - lenHi) * 6 : 0;
        score += Math.Min(firsts.Count, 6) * 0.5;
        score += chains >= wantChains ? Math.Min(chains, wantChains + 2) * 0.8 : -4 * (wantChains - chains);
        if (clipped.Count + clipUse > 0) score += clipUse >= 1 ? 3 + Math.Min(clipUse, 3) : -12;
        // Açılmadan kalan kilit dekor gibi durur (143/144 notu): her açılmayan renk kilidi için ceza.
        int locks = d.Cells.Count(c => c.LockRemaining > 0);
        if (locks > 1) score -= 3 * Math.Max(0, locks - LockOpens(st, sol.Solution));
        if (d.RollEnabled) score += rolls > 0 ? 4 : -8;
        // Döner taban dekor kalmasın: en kısa çözüm en az bir döndürme içermeli (rotorMin ile artırılabilir).
        if (d.Cells.Any(c => c.HasRotor)) score += rotates >= pr.I("rotorMin", 1) ? 3 + Math.Min(rotates, 2) : -12;
        // Kalite kapısı: mekaniği hiç kullanmayan en kısa çözüm kabul edilmez (skor −100 altı = aday yok).
        // Tırmanma sürebilsin diye çözümsüzden (−500) iyi tutulur.
        if (d.Cells.Any(c => c.HasRotor) && rotates == 0) score = Math.Min(score, -150);
        ev.Score = score;
        ev.Solution = sol.Solution;
        ev.MoveLimit = limit;
        ev.Info = $"en kısa {shortest}/{limit} ilk {win}/{firsts.Count} zincir {chains} rulo {rolls} örtü açma {unlocks} örtülü soyma {clipUse} döndürme {rotates}";
        return ev;
    }

    static int LockOpens(LevelState start, List<Command> solution)
    {
        var s = start.Clone();
        int n = 0;
        foreach (var c in solution)
            foreach (var w in RulesEngine.TryApply(s, c).Waves) n += w.LockHits.Count(h => h.RemainingAfter == 0);
        return n;
    }

    static void ApplyEval(LevelData d, Profile pr, Eval ev)
    {
        SetGoalsAndRoll(d, pr, ev.MoveLimit);
        d.Solution.Clear();
        foreach (var c in ev.Solution)
            d.Solution.Add(ToStep(c));
    }

    static string ToSource(LevelData d, Eval ev)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"// LevelLab gen adayı (BFS en kısa çözüm kayıtlı; yayın onayı analyze/report ile). {ev.Info}");
        sb.AppendLine($"id: {d.Id}");
        if (!string.IsNullOrEmpty(d.Title)) sb.AppendLine($"title: {d.Title}");
        if (!string.IsNullOrEmpty(d.Tutorial)) sb.AppendLine($"tutorial: {d.Tutorial}");
        sb.AppendLine($"moves: {d.MoveLimit}");
        sb.AppendLine($"roll: {(d.RollEnabled ? (d.StartingRoll ? "start" : "on") : "off")}");
        if (d.Capabilities.Contains("specials")) sb.AppendLine("specials: on");
        if (d.Paint > 0) sb.AppendLine($"paint: {d.Paint}");
        if (!string.IsNullOrEmpty(d.TrialType)) sb.AppendLine($"type: {d.TrialType}");
        if (d.Rules.Count > 0) sb.AppendLine("rules: " + string.Join(", ", d.Rules));
        sb.AppendLine("goals: " + string.Join(", ", d.Goals.Select(g => g.Type == "collectStars" ? $"star {g.Amount}" : g.Type == "collectToken" ? $"{g.Kind} {g.Amount}"
            : g.Type == "collectChain" ? $"chain {g.Color} {g.Amount}" : $"{g.Color} {g.Amount}")));
        sb.AppendLine("grid:");
        for (int y = 0; y < d.Height; y++)
        {
            var row = new List<string>();
            for (int x = 0; x < d.Width; x++) row.Add(CellToken(d.Cells.FirstOrDefault(c => c.X == x && c.Y == y)));
            sb.AppendLine(string.Join(" ", row.Select(r => r.PadRight(5))).TrimEnd());
        }
        sb.AppendLine("solution: " + string.Join(" ; ", d.Solution.Select(FmtStep)));
        return sb.ToString();
    }
}
