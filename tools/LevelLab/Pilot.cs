using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KatKat.Core;
using KatKat.Levels;

/// <summary>
/// vr1 P2/P3 pilot kataloğu (docs/vr1/claude_code_paketi_v1/belgeler/05_TAHTA_YENILIKLER.md "12 bölümlük pilot"):
/// canlı 1–1000'e dokunmaz; çıktı Assets/KatKat/Resources/Pilot/pilot_NN.json ve docs/vr1/PILOT_URETIM.md.
///   P01 kompakt-derin (güvenli/ustalık yolu), P02 geniş-sığ, P03 dar bağlantı, P04 kanat açma + eski engel (buz),
///   P05–P07 aşamalı hedef, P08–P10 zincir mührü (derinlik 1/2/3), P11–P12 iki alan ortak tepsi.
/// Her aday tohumlu rastgele üretilir; kayıtlı çözüm motorla yeniden yürütülerek doğrulanmadan yazılmaz. Rapor, tasarım
/// varsayımını (amaç) ve ölçülen değerleri ayrı sütunlarda tutar. Zorluk iddiası yoktur; ölçüm botlarla yapılır.
/// Kullanım: dotnet run -- pilot [tohum]
/// </summary>
static partial class Program
{
    sealed class PilotSpec
    {
        public string Type, Title, Intent;
        public int W, H, Colors, MinLayers, MaxLayers;
        public bool Specials;
        public string[] Caps = Array.Empty<string>();
        /// <summary>Etkin olmayan hücre maskesi ('#') ve özel hücreler: 'G' geçit 1, 'B' düğme 1 (yığın dibinde), 'I' buz.
        /// Boşlukla değil karakter karakter (satır başına W).</summary>
        public string[] Mask;
        public Func<Random, LevelData, bool> Shape;
        public Func<PilotMetrics, LevelData, bool> Accept;
        public int Slack;
        public int Budget = 60000;
        /// <summary>Zincir mührü derinliği (0 = yok); geçit 1.</summary>
        public int SealWave;
    }

    sealed class PilotMetrics
    {
        public List<Command> Solution;
        public int Steps, Legal, Winners;
        public bool GreedyWins, BiggestWins, UsesTray;
        public int MaxChainDepth;
    }

    static int Pilot(string[] args)
    {
        int seed = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 20261111;
        string outDir = Path.Combine(RepoRoot(), "Assets", "KatKat", "Resources", "Pilot");
        Directory.CreateDirectory(outDir);
        var specs = PilotSpecs();
        var report = new StringBuilder();
        report.AppendLine("# Pilot katalog üretim raporu (LevelLab pilot, tohum " + seed + ")");
        report.AppendLine();
        report.AppendLine("Canlı 1–1000 bölüme dokunulmadı. Her bölümün kayıtlı çözümü motorla yeniden yürütülüp kazandığı doğrulandı.");
        report.AppendLine("\"Amaç\" tasarım varsayımıdır; \"Ölçüm\" sütunları bot ve çözücüyle bulunan gerçek değerlerdir (oyuncu testi değildir).");
        report.AppendLine();
        report.AppendLine("| # | Tür | Amaç (varsayım) | Tahta | Hamle | Kayıtlı çözüm | Kazandıran ilk hamle | Açgözlü | Büyük grup önce | En derin zincir | Deneme |");
        report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        string only = args.Length > 2 ? args[2] : null; // ör. "10": yalnız pilot_10 (diğer dosyalar korunur)
        for (int n = 0; n < specs.Count; n++)
        {
            if (only != null && (n + 1).ToString("00") != only) continue;
            var spec = specs[n];
            var rng = new Random(seed + n * 104729);
            LevelData found = null; PilotMetrics fm = null; int tries = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (found == null && tries < 20000 && sw.Elapsed.TotalMinutes < (spec.SealWave >= 3 ? 20 : 5))
            {
                tries++;
                var d = PilotBoard(rng, spec, $"pilot_{n + 1:00}");
                if (!spec.Shape(rng, d)) continue;
                var m = PilotMeasure(d, spec, 0);
                if (m == null) continue;
                d.MoveLimit = m.Steps + spec.Slack;
                var m2 = PilotMeasure(d, spec, d.MoveLimit);
                if (m2 == null || !spec.Accept(m2, d)) continue;
                found = d; fm = m2;
            }
            if (found == null)
            {
                Console.WriteLine($"pilot_{n + 1:00} {spec.Type}: bulunamadı ({tries} deneme)");
                report.AppendLine($"| P{n + 1:00} | {spec.Type} | {spec.Intent} | — | — | — | — | — | — | — | {tries} (bulunamadı) |");
                continue;
            }
            found.Title = spec.Title;
            found.TrialType = spec.Type;
            found.Solution.Clear();
            foreach (var c in fm.Solution) found.Solution.Add(TrialStep(c));
            var st = LevelLoader.ToState(found);
            foreach (var c in fm.Solution) RulesEngine.TryApply(st, c);
            if (st.Status != GameStatus.Won) throw new InvalidOperationException(spec.Type + ": kayıtlı çözüm kazanmıyor");
            found.StarThresholds = new[] { Math.Max(1, (int)(st.Score * 0.75)), Math.Max(2, st.Score) };
            File.WriteAllText(Path.Combine(outDir, found.Id + ".json"), PilotJson(found), new UTF8Encoding(false));
            Console.WriteLine($"{found.Id} {spec.Type}: {found.Width}x{found.Height} hamle {found.MoveLimit} çözüm {fm.Steps} ilk hamle {fm.Winners}/{fm.Legal} açgözlü {(fm.GreedyWins ? "kazanır" : "kaybeder")} zincir {fm.MaxChainDepth} deneme {tries}");
            report.AppendLine($"| P{n + 1:00} | {spec.Type} | {spec.Intent} | {found.Width}×{found.Height} | {found.MoveLimit} | {fm.Steps} adım{(fm.UsesTray ? " (tepsi)" : "")} | {fm.Winners}/{fm.Legal} | {(fm.GreedyWins ? "kazanır" : "kaybeder")} | {(fm.BiggestWins ? "kazanır" : "kaybeder")} | {fm.MaxChainDepth} | {tries} |");
        }
        report.AppendLine();
        report.AppendLine("Notlar: \"Kazandıran ilk hamle\" = ilk hamleden sonra bütçe içinde doğrulanmış çözüm bulunan yasal hamle sayısı / yasal hamle (alternatif yol göstergesi; bütçe dolan dal kazandırmaz sayılır). \"En derin zincir\" = kayıtlı çözümdeki en uzun otomatik zincir dalgası sayısı.");
        File.WriteAllText(Path.Combine(RepoRoot(), "docs", "vr1", "PILOT_URETIM.md"), report.ToString(), new UTF8Encoding(false));
        return 0;
    }

    /// <summary>Kümeli rastgele katmanlar + maske (etkin olmayan, geçit, düğme, buz, alan).</summary>
    static LevelData PilotBoard(Random rng, PilotSpec spec, string id)
    {
        var d = new LevelData { Id = id, Width = spec.W, Height = spec.H, SchemaVersion = 2 };
        d.TrialType = spec.Type; // boyut kapsamı (dikdörtgen tahta) türlü bölümde geçerli
        d.Capabilities.AddRange(spec.Caps);
        if (spec.SealWave > 0) { d.SealWave = spec.SealWave; d.SealGate = 1; }
        if (spec.Specials && !d.Capabilities.Contains("specials")) d.Capabilities.Add("specials");
        int layers = spec.MaxLayers;
        var col = new int[spec.W, spec.H, layers];
        for (int l = 0; l < layers; l++)
            for (int y = 0; y < spec.H; y++)
                for (int x = 0; x < spec.W; x++)
                {
                    int c = rng.Next(spec.Colors);
                    double p = rng.NextDouble();
                    if (p < 0.32 && x > 0) c = col[x - 1, y, l];
                    else if (p < 0.58 && y > 0) c = col[x, y - 1, l];
                    col[x, y, l] = c;
                }
        for (int y = 0; y < spec.H; y++)
            for (int x = 0; x < spec.W; x++)
            {
                char m = spec.Mask != null ? spec.Mask[y][x] : '.';
                var cell = new CellData { X = x, Y = y, Active = m != '#' && m != 'G' };
                if (m == 'R') cell.Area = 1;
                int n = spec.MinLayers + rng.Next(spec.MaxLayers - spec.MinLayers + 1);
                if (m == 'G')
                {
                    cell.GateId = 1;
                    for (int l = 0; l < n; l++) cell.GateLayers.Add(ColorNames[col[x, y, l]]);
                }
                else if (cell.Active)
                {
                    for (int l = 0; l < n; l++) cell.Layers.Add(ColorNames[col[x, y, l]]);
                    if (m == 'B') cell.ButtonGate = 1;
                    if (m == 'I') cell.IceHits = 1;
                }
                d.Cells.Add(cell);
            }
        return d;
    }

    /// <summary>Çözüm (önce BFS, sonra en-iyi-önce planlayıcı) ve ölçümler. limit 0: hamle sınırı geniş (ilk geçiş).</summary>
    static PilotMetrics PilotMeasure(LevelData d, PilotSpec spec, int limit)
    {
        d.MoveLimit = limit > 0 ? limit : 30;
        var verr = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
        if (verr.Count > 0) { if (Environment.GetEnvironmentVariable("KATKAT_PILOT_DEBUG") == "1") Console.WriteLine("  ret: " + verr[0]); return null; }
        LevelState s;
        try { s = LevelLoader.ToState(d); } catch (FormatException) { return null; }
        if (s.IsTerminal || !RulesEngine.HasLegalMove(s)) return null;
        List<Command> path = null;
        var bfs = LevelSolver.Solve(s, spec.Budget);
        if (bfs.Outcome == SolveOutcome.Solved) path = bfs.Solution;
        else
        {
            var plan = HintPlanner.Plan(s, spec.Budget * 2, 0);
            if (plan.IsVerified) path = plan.Path;
        }
        if (path == null || !HintPlanner.Verify(s, path)) return null;
        var m = new PilotMetrics { Solution = path, Steps = path.Count(c => c.Type != CommandType.UseSpecial), UsesTray = path.Any(c => c.Type == CommandType.UseSpecial) };
        var rep = s.Clone();
        foreach (var c in path)
        {
            var r = RulesEngine.TryApply(rep, c);
            m.MaxChainDepth = Math.Max(m.MaxChainDepth, r.Waves.Count(w => w.Kind == WaveKind.Chain));
        }
        if (limit <= 0) return m;
        foreach (var cmd in LevelSolver.Moves(s))
        {
            if (cmd.Type == CommandType.UseSpecial) continue;
            var next = s.Clone();
            if (!RulesEngine.TryApply(next, cmd).Accepted) continue;
            m.Legal++;
            if (next.Status == GameStatus.Won) { m.Winners++; continue; }
            if (next.IsTerminal) continue;
            if (HintPlanner.Plan(next, spec.Budget, 0).IsVerified) m.Winners++;
        }
        m.GreedyWins = Play(s, Greedy);
        m.BiggestWins = Play(s, Biggest);
        return m;
    }

    static GoalData StageGoal(string color, int amount, int stage) => new GoalData { Type = "collectColor", Color = color, Amount = amount, Stage = stage };

    static int Total(LevelData d, string color, bool gates = true) =>
        d.Cells.Sum(c => c.Layers.Count(l => l == color) + (gates ? c.GateLayers.Count(l => l == color) : 0));

    static string[] Colors2(Random r, LevelData d) => ColorNames.Where(c => Total(d, c) > 0).OrderByDescending(c => Total(d, c)).Take(3).OrderBy(_ => r.Next()).Take(2).ToArray();

    static List<PilotSpec> PilotSpecs()
    {
        // Ortak hedef koyucular.
        Func<Random, LevelData, bool> twoColors = (r, d) =>
        {
            var cs = Colors2(r, d);
            if (cs.Length < 2) return false;
            foreach (var c in cs) d.Goals.Add(Goal("collectColor", c, Math.Max(4, (int)(Total(d, c) * 0.6))));
            return true;
        };
        Func<int, Func<Random, LevelData, bool>> staged = stages => (r, d) =>
        {
            var order = ColorNames.Where(c => Total(d, c) >= 6).OrderBy(_ => r.Next()).Take(stages).ToList();
            if (order.Count < stages) return false;
            for (int i = 0; i < stages; i++) d.Goals.Add(StageGoal(order[i], Math.Max(4, (int)(Total(d, order[i]) * (stages == 2 ? 0.45 : 0.35))), i));
            return true;
        };
        // Mühür: hedef rengin bir kısmı yalnız mühürle açılan kanatta; hedef dışarıdakinden fazla → mühür gerekir.
        Func<Random, LevelData, bool> sealed_ = (r, d) =>
        {
            // Ön eleme: mühür derinliği tek hamlede ulaşılabilir değilse çözücüye hiç gönderme (derin mühür nadir).
            if (d.SealWave >= 2 && !ReachesDepth(d, d.SealWave)) return false;
            var c = ColorNames.OrderByDescending(x => d.Cells.Sum(cell => cell.GateLayers.Count(l => l == x))).First();
            int outside = Total(d, c, false), inside = Total(d, c) - outside;
            if (inside < 3) return false;
            d.Goals.Add(Goal("collectColor", c, outside + Math.Min(inside, 3)));
            return true;
        };
        // İki alan: her alandan bir hedef rengi (iki alanda da ilerleme gerekir).
        Func<Random, LevelData, bool> twoAreas = (r, d) =>
        {
            string Best(int area) => ColorNames.OrderByDescending(x => d.Cells.Where(c => c.Area == area).Sum(c => c.Layers.Count(l => l == x))).First();
            var a = Best(0); var b = Best(1);
            int na = d.Cells.Where(c => c.Area == 0).Sum(c => c.Layers.Count(l => l == a));
            int nb = d.Cells.Where(c => c.Area == 1).Sum(c => c.Layers.Count(l => l == b));
            if (a == b) { d.Goals.Add(Goal("collectColor", a, (int)((na + nb) * 0.6))); return na >= 4 && nb >= 4; }
            d.Goals.Add(Goal("collectColor", a, Math.Max(4, (int)(na * 0.7))));
            d.Goals.Add(Goal("collectColor", b, Math.Max(4, (int)(nb * 0.7))));
            return true;
        };
        string[] twoRooms = { "...#...", "...#...", "...#...", ".......", "...#...", "...#...", "...#..." };
        string[] wing = { "....GG", "....GG", "...IGG", "B..I..", "......", "......", "......" };
        string[] sealWing = { ".....GG", ".....GG", ".....GG", ".......", ".......", ".......", "......." };
        string[] areas = { "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR" };
        return new List<PilotSpec>
        {
            new PilotSpec { Type = "pilot_compact", Title = "Derin Kuyu", Intent = "Kompakt-derin: dar tahta, derin yığın; güvenli ve ustalık yolu", W = 4, H = 4, Colors = 3, MinLayers = 3, MaxLayers = 4,
                Shape = twoColors, Slack = 2, Budget = 120000, Accept = (m, d) => m.Winners >= 2 && m.Steps >= 4 },
            new PilotSpec { Type = "pilot_wide", Title = "Geniş Kıyı", Intent = "Geniş-sığ rahatlama: büyük tahta, 1–2 katman", W = 8, H = 8, Colors = 4, MinLayers = 1, MaxLayers = 2,
                Shape = twoColors, Slack = 3, Accept = (m, d) => m.GreedyWins && m.Steps >= 3 },
            new PilotSpec { Type = "pilot_bridge", Title = "Dar Geçit", Intent = "İki oda, 1 hücrelik bağlantı; bağlantıyı erken boşaltmanın sonucu görünür", W = 7, H = 7, Colors = 3, MinLayers = 2, MaxLayers = 3, Mask = twoRooms,
                Shape = twoColors, Slack = 2, Accept = (m, d) => m.Steps >= 4 && m.Winners >= 1 },
            new PilotSpec { Type = "pilot_wing", Title = "Kanat", Intent = "Düğmeyle kanat açma + buz (mevcut engeller)", W = 6, H = 7, Colors = 3, MinLayers = 2, MaxLayers = 3, Mask = wing, Caps = new[] { "gate", "ice" },
                Shape = (r, d) =>
                {
                    var c = ColorNames.OrderByDescending(x => d.Cells.Sum(cell => cell.GateLayers.Count(l => l == x))).First();
                    int outside = Total(d, c, false), inside = Total(d, c) - outside;
                    if (inside < 3) return false;
                    d.Goals.Add(Goal("collectColor", c, outside + 2));
                    return true;
                }, Slack = 2, Accept = (m, d) => m.Steps >= 4 },
            new PilotSpec { Type = "pilot_stages", Title = "Sıradaki Renk", Intent = "Aşamalı hedef tanışma: 2 renk sırası, tahta sıfırlanmaz", W = 5, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3, Caps = new[] { "stages" },
                Shape = staged(2), Slack = 2, Accept = (m, d) => m.Steps >= 3 },
            new PilotSpec { Type = "pilot_stages", Title = "Üç Aşama", Intent = "Aşamalı hedef uygulama: 3 renk sırası", W = 6, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3, Caps = new[] { "stages" },
                Shape = staged(3), Slack = 2, Accept = (m, d) => m.Steps >= 4 },
            new PilotSpec { Type = "pilot_stages", Title = "Buzlu Sıra", Intent = "Aşamalı hedef + mevcut engel (buz)", W = 6, H = 7, Colors = 3, MinLayers = 2, MaxLayers = 3, Caps = new[] { "stages", "ice" },
                Mask = new[] { "......", "..II..", "......", "......", "..II..", "......", "......" }, Shape = staged(2), Slack = 2, Accept = (m, d) => m.Steps >= 4 },
            new PilotSpec { Type = "pilot_seal", Title = "Birinci Mühür", Intent = "Zincir mührü: 1. otomatik dalga kanadı açar", W = 7, H = 7, Colors = 3, MinLayers = 2, MaxLayers = 3, Mask = sealWing, Caps = new[] { "seal", "gate" }, SealWave = 1,
                Shape = sealed_, Slack = 2, Accept = (m, d) => m.Steps >= 3 },
            new PilotSpec { Type = "pilot_seal", Title = "İkinci Mühür", Intent = "Zincir mührü: 2. dalga gerekir (alt katmanları hazırla)", W = 7, H = 7, Colors = 3, MinLayers = 3, MaxLayers = 3, Mask = sealWing, Caps = new[] { "seal", "gate" }, SealWave = 2,
                Shape = sealed_, Slack = 2, Budget = 90000, Accept = (m, d) => m.Steps >= 3 },
            new PilotSpec { Type = "pilot_seal", Title = "Üçüncü Mühür", Intent = "Zincir mührü: 3. dalga (zor; planlı zincir)", W = 7, H = 7, Colors = 3, MinLayers = 3, MaxLayers = 4, Mask = sealWing, Caps = new[] { "seal", "gate" }, SealWave = 3,
                Shape = sealed_, Slack = 3, Budget = 90000, Accept = (m, d) => m.Steps >= 3 },
            new PilotSpec { Type = "pilot_twin", Title = "İki Kıyı", Intent = "İki alan, ortak hamle ve tepsi; roket yalnız seçilen alanda", W = 7, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3, Mask = areas, Caps = new[] { "areas" }, Specials = true,
                Shape = twoAreas, Slack = 2, Accept = (m, d) => m.Steps >= 4 },
            new PilotSpec { Type = "pilot_twin", Title = "Ortak Tepsi", Intent = "İki alan: bir alanda üretilen özel taş diğerinde kullanılır", W = 7, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3, Mask = areas, Caps = new[] { "areas" }, Specials = true,
                Shape = twoAreas, Slack = 1, Accept = (m, d) => m.Steps >= 4 && m.UsesTray },
        };
    }

    static string PilotJson(LevelData d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"schemaVersion\": 2,");
        sb.AppendLine($"  \"id\": \"{d.Id}\",");
        sb.AppendLine($"  \"capabilities\": [{string.Join(", ", d.Capabilities.Distinct().Select(c => $"\"{c}\""))}],");
        sb.AppendLine($"  \"title\": \"{Esc(d.Title)}\",");
        sb.AppendLine($"  \"trialType\": \"{d.TrialType}\",");
        sb.AppendLine("  \"rules\": [],");
        sb.AppendLine($"  \"width\": {d.Width},");
        sb.AppendLine($"  \"height\": {d.Height},");
        sb.AppendLine($"  \"moveLimit\": {d.MoveLimit},");
        sb.AppendLine("  \"rollEnabled\": false,");
        sb.AppendLine("  \"startingRoll\": false,");
        sb.AppendLine($"  \"stars\": [{d.StarThresholds[0]}, {d.StarThresholds[1]}],");
        if (d.SealWave > 0) sb.AppendLine($"  \"seal\": {{ \"wave\": {d.SealWave}, \"gate\": {d.SealGate} }},");
        sb.AppendLine("  \"goals\": [");
        sb.AppendLine(string.Join(",\n", d.Goals.Select(g => $"    {{ \"type\": \"{g.Type}\", \"color\": \"{g.Color}\", \"amount\": {g.Amount}{(g.Stage > 0 ? $", \"stage\": {g.Stage}" : "")} }}")));
        sb.AppendLine("  ],");
        sb.AppendLine("  \"cells\": [");
        sb.AppendLine(string.Join(",\n", d.Cells.Select(c =>
        {
            var p = new List<string> { $"\"x\": {c.X}", $"\"y\": {c.Y}", $"\"active\": {(c.Active ? "true" : "false")}" };
            if (c.Area != 0) p.Add($"\"area\": {c.Area}");
            if (c.Layers.Count > 0) p.Add($"\"layers\": [{string.Join(", ", c.Layers.Select(l => $"\"{l}\""))}]");
            if (c.IceHits > 0) p.Add($"\"overlay\": {{ \"kind\": \"ice\", \"hits\": {c.IceHits} }}");
            if (c.ButtonGate > 0) p.Add($"\"base\": {{ \"kind\": \"button\", \"gate\": {c.ButtonGate} }}");
            if (c.GateId > 0) p.Add($"\"gate\": {c.GateId}, \"gateLayers\": [{string.Join(", ", c.GateLayers.Select(l => $"\"{l}\""))}]");
            return "    { " + string.Join(", ", p) + " }";
        })));
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
    /// <summary>Başlangıç tahtasında ya da bir hamle sonrasında (iki adım) tek hamlede bu derinlikte otomatik zincir var mı.</summary>
    static bool ReachesDepth(LevelData d, int depth)
    {
        d.MoveLimit = 30;
        LevelState s;
        try { s = LevelLoader.ToState(d); } catch (FormatException) { return false; }
        foreach (var a in LevelSolver.Moves(s))
        {
            var n = s.Clone();
            var r = RulesEngine.TryApply(n, a);
            if (!r.Accepted) continue;
            if (r.Waves.Count(w => w.Kind == WaveKind.Chain) >= depth) return true;
            if (n.IsTerminal) continue;
            foreach (var b in LevelSolver.Moves(n))
            {
                var m = n.Clone();
                var r2 = RulesEngine.TryApply(m, b);
                if (r2.Accepted && r2.Waves.Count(w => w.Kind == WaveKind.Chain) >= depth) return true;
            }
        }
        return false;
    }
}
