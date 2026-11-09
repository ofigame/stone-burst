using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// daily [gün sayısı] [bot oyunu]: v10 P3.4 günlük bulmaca kataloğu (kullanıcı: "kişinin seviyesine orantılı, çok zor").
//   Oyuncunun ilerlemesine göre seviye grupları (Tiers). Her gün × grup için bir bulmaca:
//   1) Kaynak: grubun alt sınırına yakın, oyuncunun ulaşmış olduğu bir bölüm; yalnız o sınıra kadar tanıtılmış mekanikler.
//   2) Dönüşüm: yatay ayna (gün tohumlu) + renk permütasyonu; rulo ve özel taş açık (taş güçleri için).
//   3) Sertleştirme: renk hedefleri büyür, hamle sınırı = ışın çözümü + 1..3; plansız bot kazanma oranı hedefi
//      ~%4 (kabul %0,5–%12). Kayıtlı çözüm güç kullanmadan kazanmak zorunda.
// Çıktı: daily_src/daily_dNNN_tK.txt ve Assets/KatKat/Resources/Daily/daily_index.json.
// JSON derleme ayrı adım: KATKAT_SRC=daily_src KATKAT_OUT=Assets/KatKat/Resources/Daily dotnet run -c Release -- analyze
// Ana 1.000 bölüm dosyasına yazılmaz; kaynaklar yalnız okunur.
static partial class Program
{
    /// <summary>Seviye grupları: oyuncunun ulaştığı en yüksek bölüm numarasına göre (dahil).</summary>
    public static readonly (int Lo, int Hi)[] DailyTiers =
    {
        (1, 40), (41, 100), (101, 200), (201, 350), (351, 500), (501, 700), (701, 850), (851, 1000),
    };

    static string DailyVersion = "daily-v2";
    const double DailyTarget = 0.04, DailyMinWr = 0.005, DailyMaxWr = 0.12;

    /// <summary>Kaynak bölüm aralığı: grup 1 için 10–30, diğerleri için alt sınırın %75'i – alt sınır.</summary>
    static (int from, int to) DailySourceRange(int tier)
    {
        var (lo, _) = DailyTiers[tier - 1];
        return tier == 1 ? (10, 30) : (Math.Max(10, (int)(lo * 0.75)), lo);
    }

    static HashSet<string> Features(LevelData d)
    {
        var f = new HashSet<string>(CapabilitiesOf(d));
        if (d.RollEnabled) f.Add("roll");
        return f;
    }

    static readonly string[] DailyColors = { "coral", "blue", "yellow", "purple" };

    /// <summary>Ayna + renk permütasyonu; çözüm silinir (yeniden çözülür).</summary>
    static LevelData DailyTransform(LevelData src, Random rng)
    {
        var d = Clone(src);
        var perm = DailyColors.OrderBy(_ => rng.Next()).ToArray();
        if (perm.SequenceEqual(DailyColors)) perm = new[] { "blue", "yellow", "purple", "coral" };
        var map = DailyColors.Select((c, i) => (c, perm[i])).ToDictionary(t => t.c, t => t.Item2);
        string M(string c) => string.IsNullOrEmpty(c) || !map.ContainsKey(c) ? c : map[c];
        bool mirror = rng.Next(2) == 1;
        foreach (var c in d.Cells)
        {
            if (mirror) c.X = d.Width - 1 - c.X;
            for (int i = 0; i < c.Layers.Count; i++) c.Layers[i] = M(c.Layers[i]);
            for (int i = 0; i < c.GateLayers.Count; i++) c.GateLayers[i] = M(c.GateLayers[i]);
            for (int i = 0; i < c.SpongeColors.Count; i++) c.SpongeColors[i] = M(c.SpongeColors[i]);
            c.LockColor = M(c.LockColor);
            c.EnvelopeColor = M(c.EnvelopeColor);
        }
        if (mirror)
        {
            var sorted = d.Cells.OrderBy(c => c.Y).ThenBy(c => c.X).ToList();
            d.Cells.Clear(); d.Cells.AddRange(sorted);
        }
        foreach (var g in d.Goals) g.Color = M(g.Color);
        d.Solution.Clear();
        d.Tutorial = "";
        d.RollEnabled = true;
        d.StartingRoll = false;
        if (!d.Capabilities.Contains("specials")) d.Capabilities.Add("specials");
        return d;
    }

    sealed class DailyPick
    {
        public LevelData D; public List<Command> Sol; public int Limit, Slack, Plan = -1, Source; public double Gf, Wr, Score;
    }

    static int Daily(string[] args)
    {
        int days = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 30;
        int games = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 48;
        var srcDir = Path.Combine(Root, "levels_src");
        var outDir = Path.Combine(Root, "daily_src");
        Directory.CreateDirectory(outDir);

        // Kaynaklar ve her mekaniğin ilk göründüğü bölüm.
        var src = Directory.GetFiles(srcDir, "level_*.txt")
            .Select(p => (n: int.Parse(Path.GetFileNameWithoutExtension(p).Substring(6), CultureInfo.InvariantCulture), d: ParseSource(p)))
            .ToDictionary(t => t.n, t => t.d);
        var firstSeen = new Dictionary<string, int>();
        foreach (var n in src.Keys.OrderBy(k => k))
            foreach (var f in Features(src[n])) if (!firstSeen.ContainsKey(f)) firstSeen[f] = n;
        Console.WriteLine("mekaniklerin ilk bölümü: " + string.Join(", ", firstSeen.OrderBy(k => k.Value).Select(k => $"{k.Key}={k.Value}")));

        var eligible = new Dictionary<int, List<int>>();
        for (int t = 1; t <= DailyTiers.Length; t++)
        {
            var (from, to) = DailySourceRange(t);
            int known = Math.Max(10, DailyTiers[t - 1].Lo);
            eligible[t] = src.Keys.Where(n => n >= from && n <= to && Features(src[n]).All(f => f == "specials" || f == "roll" || firstSeen[f] <= known)).OrderBy(n => n).ToList();
            Console.WriteLine($"grup {t} ({DailyTiers[t - 1].Lo}–{DailyTiers[t - 1].Hi}): kaynak {from}–{to}, uygun {eligible[t].Count}");
            if (eligible[t].Count == 0) throw new Exception($"grup {t}: uygun kaynak yok");
        }

        var jobs = new List<(int day, int tier)>();
        for (int day = 1; day <= days; day++) for (int t = 1; t <= DailyTiers.Length; t++) jobs.Add((day, t));
        var lines = new string[jobs.Count];
        int failed = 0, done = 0;
        var started = DateTime.Now;
        Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, j =>
        {
            var (day, tier) = jobs[j];
            string id = $"daily_d{day:000}_t{tier}";
            var outPath = Path.Combine(outDir, id + ".txt");
            if (File.Exists(outPath)) { lines[j] = $"{id}: var, atlandı"; return; }
            var rng = new Random(day * 1009 + tier * 97 + 13);
            var used = new HashSet<int>();
            string lastErr = "";
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var pool = eligible[tier].Where(n => !used.Contains(n)).ToList();
                if (pool.Count == 0) break;
                int n = pool[rng.Next(pool.Count)];
                used.Add(n);
                try
                {
                    var pick = HardenDaily(src[n], n, id, rng, games, day * 131 + tier);
                    if (pick == null) { lastErr = $"kaynak {n}: çözüm yok"; continue; }
                    if (pick.Wr < DailyMinWr || pick.Wr > DailyMaxWr) { lastErr = $"kaynak {n}: bot {pick.Wr:0.000} aralık dışı"; continue; }
                    var best = pick.D;
                    best.Id = id;
                    best.Title = $"Günlük {day}";
                    best.MoveLimit = pick.Limit;
                    best.Solution.Clear();
                    foreach (var c in pick.Sol) best.Solution.Add(ToStep(c));
                    if (!ReplayWins(best, pick.Sol)) { lastErr = $"kaynak {n}: son tekrar kazanmadı"; continue; }
                    int uses = pick.Sol.Count(c => c.Type == CommandType.UseSpecial);
                    var ev = new Eval { Info = $"günlük: grup {tier} ({DailyTiers[tier - 1].Lo}–{DailyTiers[tier - 1].Hi}); kaynak level_{n:000} (ayna+renk); çözüm {CountMoves(pick.Sol)} hamle + {uses} tepsi / limit {pick.Limit} (pay +{pick.Slack}) hedef×{pick.Gf:0.00} bot {pick.Wr:0.000} plan {pick.Plan}" };
                    File.WriteAllText(outPath, ToSource(best, ev), new UTF8Encoding(false));
                    lines[j] = $"{id}: {ev.Info}";
                    break;
                }
                catch (Exception e) { lastErr = $"kaynak {n}: {e.Message}"; }
            }
            if (lines[j] == null) { System.Threading.Interlocked.Increment(ref failed); lines[j] = $"{id}: HATA {lastErr}"; }
            int k = System.Threading.Interlocked.Increment(ref done);
            if (k % 20 == 0) Console.WriteLine($"... {k}/{jobs.Count} ({(DateTime.Now - started).TotalMinutes:0.0} dk)");
        });
        foreach (var l in lines) Console.WriteLine(l);
        WriteDailyIndex(days);
        Console.WriteLine($"daily: {jobs.Count - failed}/{jobs.Count} tamam, {failed} hata, {(DateTime.Now - started).TotalMinutes:0.0} dk");
        return failed == 0 ? 0 : 1;
    }

    /// <summary>Hedef çarpanı ve hamle payı taranır; bot oranı ~%4'e en yakın, plan derinliği yüksek aday seçilir.</summary>
    static DailyPick HardenDaily(LevelData source, int n, string id, Random rng, int games, int seed)
    {
        var shaped = DailyTransform(source, rng);
        shaped.Id = id;
        int budget = source.MoveLimit + 4;
        LevelData Scaled(double gf)
        {
            var d = Clone(shaped);
            foreach (var g in d.Goals.Where(g => g.Type == "collectColor"))
            {
                int avail = d.Cells.Sum(c => c.Layers.Count(l => l == g.Color) + c.GateLayers.Count(l => l == g.Color));
                g.Amount = Math.Max(3, Math.Min((int)(avail * 0.95), (int)Math.Round(g.Amount * gf)));
            }
            return d;
        }
        var cands = new List<DailyPick>();
        int misses = 0;
        foreach (var gf in new[] { 1.0, 1.15, 1.3, 1.5, 1.7, 2.0, 2.3 })
        {
            var d = Scaled(gf);
            if (LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)).Count > 0) break;
            d.MoveLimit = budget + 6;
            var s = BeamSolve(LevelLoader.ToState(d), 60, budget + 16);
            if (s == null || !ReplayWins(d, s)) { if (cands.Count > 0 && ++misses >= 2) break; continue; }
            int L = CountMoves(s);
            double wrMin = 1;
            foreach (var slack in new[] { 1, 2, 3 })
            {
                int m = L + slack;
                double wr = BotRate(Clone(d), m, seed, games);
                wrMin = Math.Min(wrMin, wr);
                double score = Math.Abs(wr - DailyTarget) + (wr < DailyMinWr ? 0.3 : 0) + (wr > DailyMaxWr ? 0.2 : 0);
                cands.Add(new DailyPick { D = d, Sol = s, Gf = gf, Limit = m, Slack = slack, Wr = wr, Score = score, Source = n });
            }
            // Daha büyük hedef yalnız daha zor: en dar payda bile bot ~0 ise dur.
            if (wrMin <= 0.0) break;
        }
        if (cands.Count == 0) return null;
        double bestScore = cands.Min(c => c.Score);
        foreach (var c in cands.Where(c => c.Score <= bestScore + 0.03).OrderBy(c => c.Score).Take(4))
            c.Plan = PlanLevel(c.D, c.Limit);
        var pick = cands.OrderBy(c => c.Score - (c.Plan >= 0 ? 0.01 * c.Plan : 0)).First();
        if (pick.Plan < 0) pick.Plan = PlanLevel(pick.D, pick.Limit);
        return pick;
    }

    /// <summary>Katalog: gruplar ve gün × grup bulmaca dosyaları (yalnız var olan kaynaklar).</summary>
    static void WriteDailyIndex(int days)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"_not\": \"v10 P3.4 günlük katalog (ana 1.000 bölümden ayrı). Grup, oyuncunun ulaştığı en yüksek bölüme göre; o gün ilk girişte sabitlenir. Kaynak: daily_src/ (LevelLab daily), derleme: KATKAT_SRC=daily_src KATKAT_OUT=Assets/KatKat/Resources/Daily dotnet run -c Release -- analyze. Gün seçimi yerelde gün numarasına göre; canlıda sunucu.\",");
        sb.AppendLine($"  \"contentVersion\": \"{DailyVersion}\",");
        sb.AppendLine("  \"tiers\": [");
        sb.AppendLine(string.Join(",\n", DailyTiers.Select((t, i) => $"    {{ \"tier\": {i + 1}, \"minLevel\": {t.Lo}, \"maxLevel\": {t.Hi} }}")));
        sb.AppendLine("  ],");
        sb.AppendLine("  \"days\": [");
        var dayLines = new List<string>();
        for (int day = 1; day <= days; day++)
        {
            var levels = new List<string>();
            for (int t = 1; t <= DailyTiers.Length; t++)
            {
                string id = $"daily_d{day:000}_t{t}";
                if (File.Exists(Path.Combine(Root, "daily_src", id + ".txt"))) levels.Add($"\"{t}\": \"{id}\"");
            }
            if (levels.Count == DailyTiers.Length) dayLines.Add($"    {{ \"day\": {day}, \"levels\": {{ {string.Join(", ", levels)} }} }}");
        }
        sb.AppendLine(string.Join(",\n", dayLines));
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        var path = Path.Combine(Root, "Assets", "KatKat", "Resources", "Daily", "daily_index.json");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"katalog: {dayLines.Count} tam gün → {path}");
    }
}
