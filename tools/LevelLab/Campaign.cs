using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// v8 [from] [to] (kullanıcı onayı 2026-11-08; docs/v12/PLAN.md §6): deneme setinin bölüm türlerini 1000 bölüme yayar.
// v7 tahtaları, şekilleri ve mekanikleri korunur; yalnız takvimdeki tür bölümleri dönüştürülüp çözücüyle yeniden ölçülür:
//   * 10'luk döngü: 7. ve 8. bölüm tür bölümü, 10. bölüm patron (2 kural), her 100. bölüm sınav (3 kural).
//   * Tanıtım: kesin hamle 27, ters düşünme 37, zincir 47, kristal 57, renk kıtlığı 67, saf plan 77, doğru an 87,
//     kilit sırası 127 (renk kilidi 121'de gelir). Tek dokunuş: 49, 99, 149… (küçük 5×6 tahta; "nefes" bulmacası).
//   * Hamle: "kesin hamle" kuralında sınır = ışın çözümü; diğerlerinde ışın çözümü + pay (1–99: 3, 100–299: 2, 300+: 1).
//   * Strateji verilmez: bölüme yalnız tür ve kural yazılır (oyun kural kartı / simgeleriyle gösterir).
// Girdi levels_src (v7), çıktı levels_src_v8 (takas ayrı adım). Normal bölümler bayt bayt kopyalanır.
static partial class Program
{
    static readonly (string type, int intro)[] V8Types =
    {
        ("exact", 27), ("reverse", 37), ("chain", 47), ("protect", 57), ("scarce", 67), ("pure", 77), ("resource", 87), ("order", 127),
    };
    const int V8AhaFirst = 49;
    /// <summary>Adillik tabanı (null: bölgeye göre). Günlük üretimi kendi tabanını verir.</summary>
    static double? V8FloorOverride;

    sealed class V8Slot
    {
        public string Type;
        public bool Exact, Protect, Chain, NoSpecials, Scarce, Lock, GreedyLoses, NeedsSpecials, Intro;
        public List<string> Rules()
        {
            var r = new List<string>();
            if (Protect) r.Add("protect");
            if (Chain) r.Add("chainOnly");
            if (NoSpecials) r.Add("noSpecials");
            if (Scarce) r.Add("scarce");
            if (Exact) r.Add("exact");
            return r;
        }
        public override string ToString() => Type + (Rules().Count > 0 ? " [" + string.Join("+", Rules()) + "]" : "");
    }

    static bool V8Unlocked(string type, int n) => V8Types.Any(t => t.type == type && t.intro <= n);

    static V8Slot V8TypeSlot(string type, int n, bool intro)
    {
        var s = new V8Slot { Type = type, Intro = intro };
        switch (type)
        {
            case "exact": s.Exact = true; break;
            case "reverse": s.Exact = true; s.GreedyLoses = true; break;
            case "chain": s.Chain = true; s.Exact = n >= 300; break;
            case "protect": s.Protect = true; s.Exact = n >= 300; break;
            case "scarce": s.Scarce = true; break;
            case "pure": s.NoSpecials = true; s.Exact = n >= 200; break;
            case "resource": s.Exact = true; s.NeedsSpecials = true; break;
            case "order": s.Lock = true; s.Exact = n >= 300; break;
            case "aha": s.Exact = true; break;
        }
        return s;
    }

    /// <summary>v8 takvimi: bölüm numarasına göre tür (yoksa null = v7 bölümü aynen).</summary>
    static V8Slot V8Plan(int n)
    {
        if (n < 27) return null;
        if (n >= V8AhaFirst && n % 50 == 49) return V8TypeSlot("aha", n, n == V8AhaFirst);
        int m = n % 10;
        if (m == 0 && n >= 30)
        {
            // Patron: kurallar birleşir; her 100. bölüm sınav (3 kural).
            var boss = new V8Slot { Type = "boss", Exact = true };
            bool exam = n % 100 == 0;
            var combos = new List<Action<V8Slot>>();
            if (V8Unlocked("protect", n)) combos.Add(b => b.Protect = true);
            if (V8Unlocked("chain", n)) combos.Add(b => b.Chain = true);
            if (V8Unlocked("pure", n)) combos.Add(b => b.NoSpecials = true);
            if (V8Unlocked("scarce", n)) combos.Add(b => b.Scarce = true);
            if (combos.Count > 0)
            {
                int k = n / 10;
                combos[k % combos.Count](boss);
                if (exam && combos.Count > 1) combos[(k + 1) % combos.Count](boss);
            }
            return boss;
        }
        if (m != 7 && m != 8) return null;
        foreach (var (type, intro) in V8Types) if (intro == n) return V8TypeSlot(type, n, true);
        var pool = V8Types.Where(t => t.intro <= n).Select(t => t.type).ToList();
        int idx = (n / 10) * 2 + (m == 8 ? 1 : 0);
        string pick = pool[idx % pool.Count];
        if (m == 8 && pool.Count > 1 && V8Plan(n - 1) is V8Slot prev && prev.Type == pick) pick = pool[(idx + 1) % pool.Count];
        return V8TypeSlot(pick, n, false);
    }

    static int V8Slack(int n) => n < 100 ? 3 : n < 300 ? 2 : 1;

    static List<Command> V8Beam(LevelData d, int limit)
    {
        var c = Clone(d);
        c.MoveLimit = limit;
        var s = BeamSolve(LevelLoader.ToState(c), 60, limit + 16);
        return s != null && ReplayWins(c, s) ? s : null;
    }

    static int LayersOf(LevelData d, string color, Func<CellData, bool> skip = null) =>
        d.Cells.Where(c => skip == null || !skip(c)).Sum(c => c.Layers.Count(l => l == color) + c.GateLayers.Count(l => l == color));

    static List<string> ColorsIn(LevelData d) => d.Cells.SelectMany(c => c.Layers.Concat(c.GateLayers)).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();

    static void ScaleColorGoals(LevelData d, double f)
    {
        foreach (var g in d.Goals.Where(g => g.Type == "collectColor"))
        {
            int avail = LayersOf(d, g.Color, c => c.Protected);
            g.Amount = Math.Max(3, Math.Min((int)(avail * 0.95), (int)Math.Round(g.Amount * f)));
        }
    }

    /// <summary>Yapısal kurallar (kristal, zincir hedefi, özel taş yok, renk kıtlığı, kilit). false: uygulanamadı.</summary>
    static bool V8Mutate(LevelData d, V8Slot slot, int n, Random rng, StringBuilder note)
    {
        if (slot.NoSpecials) d.Capabilities.Remove("specials");
        if (slot.Protect)
        {
            int k = n < 200 ? 2 : 3;
            // Kristal adayları: mekaniksiz yığınlar; önce kenardakiler ve hedef rengi taşımayanlar (çözüm yolu daha az kesişir).
            var goalColors = d.Goals.Where(g => g.Type == "collectColor").Select(g => g.Color).ToHashSet();
            int maxX = d.Cells.Max(c => c.X), maxY = d.Cells.Max(c => c.Y);
            var cand = d.Cells.Where(c => c.Active && c.GateId == 0 && c.Layers.Count > 0 && !IsMechanic(c) && !c.Protected).ToList();
            if (cand.Count < k + 4) return false;
            double Prefer(CellData c) => (c.X == 0 || c.Y == 0 || c.X == maxX || c.Y == maxY ? 0 : 2) + c.Layers.Count(l => goalColors.Contains(l)) + rng.NextDouble() * 2.5;
            foreach (var c in cand.OrderBy(Prefer).Take(k)) c.Protected = true;
            foreach (var g in d.Goals.Where(g => g.Type == "collectColor"))
            {
                int avail = LayersOf(d, g.Color, c => c.Protected);
                if (g.Amount > avail - 1) g.Amount = Math.Max(3, avail - 1);
            }
            note.Append($"kristal {k}; ");
        }
        if (slot.Scarce)
        {
            var goals = d.Goals.Where(g => g.Type == "collectColor").ToList();
            if (goals.Count == 0) return false;
            var g = goals[rng.Next(goals.Count)];
            var others = ColorsIn(d).Where(c => c != g.Color).ToList();
            if (others.Count == 0) return false;
            int spare = n < 100 ? 3 : n < 300 ? 2 : 1;
            int total = LayersOf(d, g.Color);
            int excess = total - (g.Amount + spare);
            if (excess > 0)
            {
                var spots = new List<(CellData c, int i)>();
                foreach (var c in d.Cells.Where(c => c.Active && !c.Protected))
                    for (int i = 0; i < c.Layers.Count; i++) if (c.Layers[i] == g.Color) spots.Add((c, i));
                foreach (var (c, i) in spots.OrderBy(_ => rng.Next()).Take(excess)) c.Layers[i] = others[rng.Next(others.Count)];
            }
            note.Append($"kıt renk {g.Color} {LayersOf(d, g.Color)}/{g.Amount}; ");
        }
        if (slot.Chain)
        {
            var colors = d.Goals.Where(g => g.Type == "collectColor").Select(g => g.Color).ToList();
            if (colors.Count == 0) colors = ColorsIn(d);
            var color = colors[rng.Next(colors.Count)];
            d.Goals.RemoveAll(g => g.Type == "collectColor" && g.Color == color);
            d.Goals.Add(new GoalData { Type = "collectChain", Color = color, Amount = 0 }); // miktar V8Chain'de seçilir
        }
        if (slot.Lock)
        {
            var goals = d.Goals.Where(g => g.Type == "collectColor").ToList();
            if (goals.Count == 0) return false;
            var t = goals[rng.Next(goals.Count)].Color;
            var keys = ColorsIn(d).Where(c => c != t).ToList();
            if (keys.Count == 0) return false;
            var key = keys[rng.Next(keys.Count)];
            var cells = d.Cells.Where(c => c.Active && c.Layers.Count > 1 && c.Layers[^1] == t && !IsMechanic(c) && !c.Protected).OrderBy(_ => rng.Next()).Take(2).ToList();
            if (cells.Count < 2) return false;
            foreach (var c in cells) { c.LockColor = key; c.LockRemaining = 3 + rng.Next(3); }
            note.Append($"kilit {key}→{t}; ");
        }
        return true;
    }

    /// <summary>Zincir hedefi: çözülebilen en büyük miktar (önce büyük).</summary>
    static List<Command> V8Chain(LevelData d, int budget, int n)
    {
        var g = d.Goals.First(x => x.Type == "collectChain");
        int top = n < 100 ? 6 : n < 300 ? 8 : 10;
        foreach (int a in new[] { top, top - 2, top - 3, 4, 3 }.Where(a => a >= 3).Distinct())
        {
            g.Amount = a;
            var s = V8Beam(d, budget);
            if (s != null) return s;
        }
        return null;
    }

    /// <summary>Bir dönüşüm denemesi. Başarılıysa d hamle sınırı ve kayıtlı çözümle döner.</summary>
    static LevelData V8Try(LevelData orig, V8Slot slot, int n, int attempt, out string info, int seed = 0)
    {
        info = null;
        var rng = new Random(n * 7907 + attempt * 131 + 17 + seed * 7919);
        var d = Clone(orig);
        var note = new StringBuilder();
        if (!V8Mutate(d, slot, n, rng, note)) return null;
        if ((slot.GreedyLoses || slot.NeedsSpecials) && attempt > 0) ScaleColorGoals(d, 1 + 0.06 * attempt);
        // Yapısal kural tutmazsa hedef biraz gevşer (en çok %30).
        if ((slot.Protect || slot.NoSpecials || slot.Lock || slot.Scarce) && attempt >= 2) ScaleColorGoals(d, Math.Max(0.7, 1 - 0.06 * (attempt - 1)));
        if (slot.NeedsSpecials && !d.Capabilities.Contains("specials")) return null;
        int budget = orig.MoveLimit + 2 + (slot.NoSpecials ? 5 : 0) + (slot.Protect ? 2 : 0);
        var sol = slot.Chain ? V8Chain(d, budget, n) : V8Beam(d, budget);
        if (sol == null) return null;
        int L = CountMoves(sol);
        // Bölge 1 (1–99) öğretici kalır: kesin hamlede de 1 hamle pay (plan: tuzaklı bölümler bölge 2'den sonra yoğunlaşır).
        int limit = slot.Exact ? L + (n < 100 ? 1 : 0) : L + V8Slack(n);
        // Adillik tabanı (kullanıcı: "adil zorluk", v7 ilkesi; stres testi botun en az bazen kazanmasını ister): bot kazanma
        // oranı bölgeye göre tabana ulaşana kadar sınır birer artar (en çok v7 sınırı + 2). Tavanı aşarsa aday reddedilir.
        // 48 oyunla: bölge 1 %10, sonra %6, patron %4 (en az 2 galibiyet; tek şanslı galibiyet yetmez — denetim/stres başka tohumla ölçer).
        double floor = V8FloorOverride ?? (slot.Type == "boss" ? 0.04 : n < 100 ? 0.10 : 0.06);
        int cap = Math.Max(orig.MoveLimit + 2, L + (slot.NoSpecials ? 6 : 3));
        double fair = BotRate(Clone(d), limit, n * 13 + seed, 48);
        while (fair < floor && limit < cap) { limit++; fair = BotRate(Clone(d), limit, n * 13 + seed, 48); }
        if (fair < floor) return null;
        d.MoveLimit = limit;
        var st = LevelLoader.ToState(d);
        bool greedy = Play(st, Greedy);
        if (slot.GreedyLoses && greedy) return null;
        if (slot.NeedsSpecials)
        {
            var ns = Clone(d);
            ns.Capabilities.Remove("specials");
            if (V8Beam(ns, limit) != null) return null; // özel taşsız da çözülüyor: tür anlamsız
        }
        d.TrialType = slot.Type;
        d.Rules.Clear();
        d.Rules.AddRange(slot.Rules());
        d.Solution.Clear();
        foreach (var c in sol) d.Solution.Add(ToStep(c));
        if (!ReplayWins(d, sol)) return null;
        var chain = d.Goals.FirstOrDefault(x => x.Type == "collectChain");
        info = $"{note}{(chain != null ? $"zincir {chain.Color} {chain.Amount}; " : "")}çözüm {L} / sınır {limit} (v7 {orig.MoveLimit}); açgözlü {(greedy ? "kazanır" : "kaybeder")}; bot48 {fair:0.00} (taban {floor:0.00})";
        return d;
    }

    /// <summary>Tek dokunuş: deneme üreticisinin küçük 5×6 tahtası (tanıtım/nefes), bölüm kimliğiyle.</summary>
    static LevelData V8Aha(int n, out string info)
    {
        info = null;
        var spec = TrialSpecs()[0];
        var rng = new Random(n * 7919 + 3);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int tries = 1; tries < 4000 && sw.Elapsed.TotalMinutes < 4; tries++)
        {
            var d = RandomBoard(rng, spec, $"level_{n:000}");
            if (!spec.Shape(rng, d)) continue;
            var m = Measure(d, spec);
            if (m == null) continue;
            d.MoveLimit = m.MinMoves + spec.Slack;
            if (!spec.Accept(m, d)) continue;
            var m2 = Measure(d, spec, d.MoveLimit);
            if (m2 == null || !spec.Accept(m2, d)) continue;
            d.TrialType = "aha";
            d.Rules.Add("exact");
            d.Solution.Clear();
            foreach (var c in m2.Solution) d.Solution.Add(ToStep(c));
            info = $"5×6 tek dokunuş; ilk dokunuş {m2.Winners}/{m2.Legal}; deneme {tries}";
            return d;
        }
        return null;
    }

    static int V8(string[] args)
    {
        int from = args.Length > 1 ? int.Parse(args[1]) : 1, to = args.Length > 2 ? int.Parse(args[2]) : 1000;
        // Kaynak v7 olmalı (takastan sonra arşivden: KATKAT_V8_SRC=<arşiv>/levels_src).
        var srcDir = Environment.GetEnvironmentVariable("KATKAT_V8_SRC") ?? Path.Combine(Root, "levels_src");
        var outDir = Path.Combine(Root, Environment.GetEnvironmentVariable("KATKAT_V8_OUT") ?? "levels_src_v8");
        Directory.CreateDirectory(outDir);
        var only = (Environment.GetEnvironmentVariable("KATKAT_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
        var files = Directory.GetFiles(srcDir, "level_*.txt")
            .Select(f => (f, n: int.Parse(Path.GetFileNameWithoutExtension(f).Substring(6), CultureInfo.InvariantCulture)))
            .Where(t => t.n >= from && t.n <= to && (only.Count == 0 || only.Contains(t.n))).OrderBy(t => t.n).ToList();
        var rows = new string[files.Count];
        var kinds = new string[files.Count];
        int failed = 0, done = 0;
        var started = DateTime.Now;
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            var (path, n) = files[i];
            var outPath = Path.Combine(outDir, Path.GetFileName(path));
            var slot = V8Plan(n);
            if (slot == null)
            {
                File.Copy(path, outPath, true);
                kinds[i] = "normal";
                return;
            }
            try
            {
                var orig = ParseSource(path);
                LevelData best = null; string info = null; string used = slot.ToString();
                if (slot.Type == "aha") best = V8Aha(n, out info);
                else
                {
                    for (int a = 0; a < 10 && best == null; a++) best = V8Try(orig, slot, n, a, out info);
                    if (best == null && slot.Type != "exact")
                    {
                        // Yedek: kesin hamle (tahta aynı). Rapor ve sayımda ayrı görünür.
                        var fb = slot.Type == "boss" ? new V8Slot { Type = "boss", Exact = true } : V8TypeSlot("exact", n, false); // patron patron kalır
                        for (int a = 0; a < 3 && best == null; a++) best = V8Try(orig, fb, n, a, out info);
                        if (best != null) used = fb + " (yedek; istenen " + slot + ")";
                    }
                }
                if (best == null) throw new Exception("dönüşüm bulunamadı: " + slot);
                best.Id = orig.Id;
                if (slot.Type == "aha") best.Title = null;
                double wr = BotRate(Clone(best), best.MoveLimit, n, 12);
                File.WriteAllText(outPath, ToSource(best, new Eval { Info = $"v8: {used}; {info}; bot {wr:0.00}" }), new UTF8Encoding(false));
                kinds[i] = used.Contains("(yedek") ? "yedek" : best.TrialType;
                rows[i] = $"| {n} | {used}{(slot.Intro ? " · **tanıtım**" : "")} | {info} | {wr:0.00} |";
            }
            catch (Exception e)
            {
                System.Threading.Interlocked.Increment(ref failed);
                File.Copy(path, outPath, true); // v7 aynen kalır
                kinds[i] = "hata";
                rows[i] = $"| {n} | {slot} | HATA: {e.Message} (v7 korundu) | — |";
            }
            int k = System.Threading.Interlocked.Increment(ref done);
            if (k % 20 == 0) Console.WriteLine($"... {k} tür bölümü ({(DateTime.Now - started).TotalMinutes:0.0} dk)");
        });
        var md = new StringBuilder();
        md.AppendLine($"# v8 bölüm üretimi (LevelLab v8, {DateTime.Now:yyyy-MM-dd HH:mm})");
        md.AppendLine();
        md.AppendLine("Kaynak: v7 (`levels_src`). Normal bölümler aynen; tür bölümleri dönüştürülüp ışın çözücüyle (genişlik 60) yeniden çözüldü. Bot: yumuşak açgözlü, 12 oyun, son hamle sınırında.");
        md.AppendLine();
        md.AppendLine("## Sayım");
        foreach (var gr in kinds.Where(k => k != null).GroupBy(k => k).OrderByDescending(g => g.Count())) md.AppendLine($"- {gr.Key}: {gr.Count()}");
        md.AppendLine();
        md.AppendLine("## Tür bölümleri");
        md.AppendLine("| Bölüm | Tür [kurallar] | Ölçüm | Bot |");
        md.AppendLine("|---|---|---|---|");
        foreach (var r in rows.Where(r => r != null)) md.AppendLine(r);
        File.WriteAllText(Path.Combine(Root, "docs", "v12", "V8_URETIM.md"), md.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"v8: {files.Count} bölüm, {kinds.Count(k => k != "normal")} tür bölümü, {failed} hata, {(DateTime.Now - started).TotalMinutes:0.0} dk → docs/v12/V8_URETIM.md");
        return failed == 0 ? 0 : 1;
    }
}

// daily3 (docs/v12/PLAN.md §6.3): günlük bulmacalar yeni kurallarla — daily-v2 bulmacaları taban alınır, her güne haftalık
// dönen bir tür verilir (oyuncunun seviye grubunda açılmamış tür atlanır), aynı v8 dönüştürücüsüyle yeniden çözülür.
// Girdi daily_src (v2), çıktı daily_src_v3 (takas ayrı adım). "Çok zor" korunur: pay en çok 1 (grup 1'de kesin hamle +1).
static partial class Program
{
    static readonly string[] DailyTypeCycle = { "exact", "chain", "protect", "reverse", "scarce", "pure", "resource", "order" };

    static int Daily3(string[] args)
    {
        V8FloorOverride = 0.04; // günlük "çok zor": v2 hedefi ~%4 (24 oyunda en az 1 galibiyet)
        var dailySrc = Environment.GetEnvironmentVariable("KATKAT_DAILY_SRC");
        var srcDir = dailySrc ?? Path.Combine(Root, "daily_src");
        var outDir = Path.Combine(Root, "daily_src_v3");
        Directory.CreateDirectory(outDir);
        var files = Directory.GetFiles(srcDir, "daily_d*_t*.txt").OrderBy(f => f, StringComparer.Ordinal).ToList();
        var rows = new string[files.Count];
        var kinds = new string[files.Count];
        var started = DateTime.Now;
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            var name = Path.GetFileNameWithoutExtension(files[i]); // daily_dNNN_tK
            int day = int.Parse(name.Substring(7, 3), CultureInfo.InvariantCulture), tier = int.Parse(name.Substring(name.Length - 1), CultureInfo.InvariantCulture);
            var (lo, hi) = DailyTiers[tier - 1];
            int n = Math.Max(40, hi); // pay ve boyutlar grubun üst sınırına göre (grup 1: bölge 1 ölçüsü)
            var orig = ParseSource(files[i]);
            LevelData best = null; string info = null, used = null;
            for (int k = 0; k < DailyTypeCycle.Length && best == null; k++)
            {
                string type = DailyTypeCycle[(day - 1 + k) % DailyTypeCycle.Length];
                if (!V8Types.Any(t => t.type == type && t.intro <= Math.Max(lo, 27))) continue; // grupta açılmamış tür yok
                var slot = V8TypeSlot(type, n, false);
                if (n >= 100) slot.Exact = slot.Exact || type == "chain" || type == "protect"; // çok zor: kısıtlı günlerde de pay yok
                for (int a = 0; a < 6 && best == null; a++) best = V8Try(orig, slot, n, a, out info, day * 10 + tier);
                if (best != null) used = slot.ToString() + (k > 0 ? $" (sıradaki tür; {DailyTypeCycle[(day - 1) % DailyTypeCycle.Length]} tutmadı)" : "");
            }
            if (best == null) { best = orig; used = "v2 aynen"; info = "tür uygulanamadı"; }
            best.Id = orig.Id;
            best.Title = orig.Title;
            File.WriteAllText(Path.Combine(outDir, name + ".txt"), ToSource(best, new Eval { Info = $"günlük v3: grup {tier}; {used}; {info}" }), new UTF8Encoding(false));
            kinds[i] = best.TrialType ?? "v2";
            rows[i] = $"| {name} | {used} | {info} |";
        });
        var md = new StringBuilder();
        md.AppendLine($"# Günlük v3 üretimi (LevelLab daily3, {DateTime.Now:yyyy-MM-dd HH:mm})");
        md.AppendLine();
        md.AppendLine("Taban: daily-v2 bulmacaları (240 = 30 gün × 8 grup). Gün türü haftalık döner: " + string.Join(" → ", DailyTypeCycle) + ". Grupta açılmamış tür atlanır.");
        md.AppendLine();
        foreach (var gr in kinds.GroupBy(k => k).OrderByDescending(g => g.Count())) md.AppendLine($"- {gr.Key}: {gr.Count()}");
        md.AppendLine();
        md.AppendLine("| Bulmaca | Tür [kurallar] | Ölçüm |");
        md.AppendLine("|---|---|---|");
        foreach (var r in rows) md.AppendLine(r);
        File.WriteAllText(Path.Combine(Root, "docs", "v12", "GUNLUK_V3_URETIM.md"), md.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"daily3: {files.Count} bulmaca, {kinds.Count(k => k != "v2")} türlü, {(DateTime.Now - started).TotalMinutes:0.0} dk → docs/v12/GUNLUK_V3_URETIM.md");
        return 0;
    }

    /// <summary>daily_src (v3 takastan sonra) için katalog: içerik sürümü daily-v3.</summary>
    static int DailyIndex3(string[] args)
    {
        DailyVersion = "daily-v3";
        int days = Directory.GetFiles(Path.Combine(Root, "daily_src"), "daily_d*_t1.txt").Length;
        WriteDailyIndex(days);
        return 0;
    }
}

// fairfix: v8 tür bölümlerinde adillik onarımı (kullanıcı: "zorluğunu, seviyeye uygunluğunu test et"). Her tür bölümü iki
// bağımsız tohum dizisiyle (24+40 ve 48 oyun = 112) ölçülür; taban: patron ≥ 3 galibiyet, diğer türler ≥ %5, bölge 1 ≥ %8.
// Altındaysa hamle sınırı birer artar (en çok +4); kaynak (levels_src) yerinde güncellenir, JSON derleme ayrı adım.
static partial class Program
{
    static int FairWins(LevelData d, int n)
    {
        int w = (int)Math.Round(BotRate(Clone(d), d.MoveLimit, n, 24) * 24);
        var st = LevelLoader.ToState(d); var rr = new Random(n * 131 + 17);
        for (int g = 0; g < 40; g++) if (BotPlay(st.Clone(), rr, 0.35)) w++;
        w += (int)Math.Round(BotRate(Clone(d), d.MoveLimit, n * 1000 + 77, 48) * 48);
        return w; // 112 oyun
    }

    static int FairFix(string[] args)
    {
        var dir = Path.Combine(Root, "levels_src");
        var files = Directory.GetFiles(dir, "level_*.txt").OrderBy(f => f, StringComparer.Ordinal).ToList();
        var lines = new System.Collections.Concurrent.ConcurrentBag<string>();
        int changed = 0, stillLow = 0;
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            var d = ParseSource(files[i]);
            int n = int.Parse(Path.GetFileNameWithoutExtension(files[i]).Substring(6), CultureInfo.InvariantCulture);
            // KATKAT_ONLY verilirse yalnız o bölümler (normal v7 bölümü de; ör. denetimde bot hiç kazanamayan).
            var onlyFix = (Environment.GetEnvironmentVariable("KATKAT_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
            if (onlyFix.Count > 0 ? !onlyFix.Contains(n) : string.IsNullOrEmpty(d.TrialType) || d.TrialType == "aha") return;
            bool intro = V8Plan(n)?.Intro == true;
            int need = intro ? 14 : d.TrialType == "boss" ? 5 : n < 100 ? 9 : 6; // 112 oyunda: tanıtım %12,5, patron ~%4,5, bölge 1 %8, diğer %5
            int start = d.MoveLimit, w = FairWins(d, n);
            while (w < need && d.MoveLimit < start + 4) { d.MoveLimit++; w = FairWins(d, n); }
            if (d.MoveLimit != start)
            {
                var sol = d.Solution.Select(LevelLoader.ToCommand).ToList();
                if (!ReplayWins(d, sol)) throw new Exception(d.Id + ": çözüm bozuldu");
                var head = File.ReadLines(files[i]).FirstOrDefault(l => l.StartsWith("//")) ?? "//";
                var info = head.TrimStart('/', ' ').Replace("LevelLab gen adayı (BFS en kısa çözüm kayıtlı; yayın onayı analyze/report ile). ", "");
                File.WriteAllText(files[i], ToSource(d, new Eval { Info = $"{info}; adillik onarımı: sınır {start}→{d.MoveLimit} (112 oyunda {w} galibiyet)" }), new UTF8Encoding(false));
                System.Threading.Interlocked.Increment(ref changed);
                lines.Add($"| {n} | {d.TrialType} | {start} → {d.MoveLimit} | {w}/112 |");
            }
            if (w < need)
            {
                if (string.IsNullOrEmpty(d.TrialType)) { lines.Add($"| {n} | normal | {start} → {d.MoveLimit} (tavan) | {w}/112 — taban altında (v7 bölümü; elle bakılmalı) |"); return; }
                // Tavanda da adil değil: bölüm v7 haline döner (kural uğruna adil olmayan bölüm bırakılmaz).
                var v7 = Path.Combine(Environment.GetEnvironmentVariable("KATKAT_V8_SRC") ?? throw new Exception("KATKAT_V8_SRC gerekli"), Path.GetFileName(files[i]));
                File.Copy(v7, files[i], true);
                System.Threading.Interlocked.Increment(ref stillLow);
                lines.Add($"| {n} | {d.TrialType} | {start} → {d.MoveLimit} (tavan) | {w}/112 — taban {need} altında: **v7 bölümüne döndü** |");
            }
        });
        var md = new StringBuilder();
        md.AppendLine($"# v8 adillik onarımı (LevelLab fairfix, {DateTime.Now:yyyy-MM-dd HH:mm})");
        md.AppendLine();
        md.AppendLine($"Tür bölümleri iki bağımsız tohum dizisiyle 112 bot oyunu. Taban: tanıtım ≥ 14 (%12,5), patron ≥ 5, bölge 1 ≥ 9 (%8), diğer ≥ 6 (%5). Sınırı artan {changed}, tavanda kalıp v7'ye dönen {stillLow}.");
        md.AppendLine();
        md.AppendLine("| Bölüm | Tür | Hamle sınırı | Galibiyet |");
        md.AppendLine("|---|---|---|---|");
        foreach (var l in lines.OrderBy(l => int.Parse(l.Split('|')[1].Trim()))) md.AppendLine(l);
        File.WriteAllText(Path.Combine(Root, "docs", "v12", "V8_ADILLIK.md"), md.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"fairfix: {changed} bölüm sınırı arttı, {stillLow} bölüm v7'ye döndü → docs/v12/V8_ADILLIK.md");
        return 0;
    }
}
