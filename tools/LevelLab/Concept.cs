using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KatKat.Core;
using KatKat.Levels;

/// <summary>
/// Konsept pilotu (kullanıcı, 2026-11-15: "10 bölüm yap pilota koy kontrol edeyim"; "mantık zeka gerektiren bulmaca olacaklar
/// aynı zamanda"). Her konseptten bir bölüm: Resources/Concept/concept_NN.json, rapor docs/konsept/URETIM.md.
/// Kabul: (1) kayıtlı çözüm motorla kazanır; (2) tehdit bağlayıcı: açgözlü bot tehditle kaybeder, tehdit kaldırılınca aynı
/// hamle sınırıyla kazanır (yani kaybettiren tehdittir, hamle sınırı değil); (3) her ilk hamle kazandırmaz. Tehdit ayarı
/// (dönem, sayaç, sınır) en sıkıdan gevşeğe denenir; ilk geçen seçilir. Zorluk iddiası yok; ölçüm botlarla.
/// Kullanım: dotnet run -- concept [tohum] [NN]
/// </summary>
static partial class Program
{
    sealed class ConceptSpec
    {
        public string Type, Title, Intent;
        public int W, H, Colors, MinLayers, MaxLayers, Slack = 3, Budget = 80000;
        public string[] Mask;
        public bool Specials;
        public Func<Random, LevelData, bool> Goals;
        /// <summary>Tehdit adayları (en sıkıdan gevşeğe). null dönen aday atlanır.</summary>
        public Func<Random, LevelData, List<HazardData>> Hazards;
        /// <summary>Sipariş: aşama süreleri çözümden hesaplanır.</summary>
        public bool OrdersFromSolution;
    }

    sealed class ConceptMetrics
    {
        public List<Command> Solution;
        public int Steps, Legal, Winners;
        public bool GreedyWins, BiggestWins, GreedyWinsFree;
        public string GreedyLoss;
    }

    static int Concept(string[] args)
    {
        int seed = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 20261115;
        string only = args.Length > 2 ? args[2] : null;
        // KATKAT_CONCEPT_OUT: deneme çıktısı başka klasöre (ör. tohum taraması; kabul edilen dosya elle kopyalanır).
        string outDir = Environment.GetEnvironmentVariable("KATKAT_CONCEPT_OUT") ?? Path.Combine(RepoRoot(), "Assets", "KatKat", "Resources", "Concept");
        Directory.CreateDirectory(outDir);
        var specs = ConceptSpecs();
        var rows = new List<string>();
        for (int n = 0; n < specs.Count; n++)
        {
            if (only != null && (n + 1).ToString("00") != only) continue;
            var spec = specs[n];
            var rng = new Random(seed + n * 7919);
            LevelData found = null; ConceptMetrics fm = null; int tries = 0;
            var why = new Dictionary<string, int>(); void W(string k) => why[k] = why.TryGetValue(k, out var v) ? v + 1 : 1;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (found == null && sw.Elapsed.TotalMinutes < 6)
            {
                tries++;
                var d = ConceptBoard(rng, spec, $"concept_{n + 1:00}");
                if (!spec.Goals(rng, d)) continue;
                // Tehditsiz en kısa çözüm → hamle sınırı.
                var free = ConceptSolve(Free(d), spec, 30);
                if (free == null) { W("tehditsiz çözüm yok"); continue; }
                int steps = free.Count(c => c.Type != CommandType.UseSpecial);
                if (steps < 5) { W("kısa"); continue; }
                d.MoveLimit = steps + spec.Slack;
                var cands = spec.OrdersFromSolution ? OrdersCandidates(d, free) : spec.Hazards(rng, d);
                if (cands == null) continue;
                foreach (var hz in cands)
                {
                    if (hz == null) continue;
                    d.Hazard = hz;
                    var m = ConceptMeasure(d, spec);
                    if (m == null) { W("tehditle çözüm yok"); continue; }
                    if (m.GreedyWins || !m.GreedyWinsFree || m.Steps < 5 || m.Winners * 2 > m.Legal) { W(m.GreedyWins ? "açgözlü kazanır/tehdit dışı kayıp" : !m.GreedyWinsFree ? "tehditsiz açgözlü kaybeder" : m.Steps < 5 ? "kısa2" : "çok kazandıran ilk hamle"); continue; }
                    found = d; fm = m;
                    break;
                }
                if (found == null) d.Hazard = null;
            }
            if (found == null)
            {
                Console.WriteLine($"concept_{n + 1:00} {spec.Type}: bulunamadı ({tries} deneme; ret: {string.Join(", ", why.Select(k => $"{k.Key} {k.Value}"))})");
                rows.Add($"| K{n + 1:00} | {spec.Title} | {spec.Intent} | — | — | — | — | — | — | {tries} (bulunamadı) |");
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
            // Okuma doğrulaması: yazılan JSON yeniden okunup aynı çözümle kazanmalı.
            string json = ConceptJson(found);
            var back = LevelParser.Parse(json, found.Id);
            var bs = LevelLoader.ToState(back);
            foreach (var c in fm.Solution) RulesEngine.TryApply(bs, c);
            if (bs.Status != GameStatus.Won) throw new InvalidOperationException(spec.Type + ": JSON geri okumada çözüm kazanmıyor");
            File.WriteAllText(Path.Combine(outDir, found.Id + ".json"), json, new UTF8Encoding(false));
            string hz2 = HazardSummary(found.Hazard);
            Console.WriteLine($"{found.Id} {spec.Type}: {found.Width}x{found.Height} hamle {found.MoveLimit} çözüm {fm.Steps} {hz2} ilk hamle {fm.Winners}/{fm.Legal} açgözlü kaybı: {fm.GreedyLoss} deneme {tries}");
            rows.Add($"| K{n + 1:00} | {spec.Title} | {spec.Intent} | {found.Width}×{found.Height} | {found.MoveLimit} | {fm.Steps} | {hz2} | {fm.Winners}/{fm.Legal} | {fm.GreedyLoss} | {tries} |");
        }
        if (only == null)
        {
            var report = new StringBuilder();
            report.AppendLine("# Konsept pilotu üretim raporu (LevelLab concept, tohum " + seed + ")");
            report.AppendLine();
            report.AppendLine("Ana 1–1000 bölüme dokunulmadı. Her bölümün kayıtlı çözümü motorla (ve JSON geri okunarak) yeniden yürütülüp kazandığı doğrulandı.");
            report.AppendLine("Kabul şartı: açgözlü bot (her hamlede hedefe en çok katkı) tehditle **kaybeder**, tehdit kaldırılınca aynı hamle sınırıyla **kazanır**; yani bölümü kaybettiren tehdittir, oyuncu tehdide göre plan yapmalıdır. Ölçümler bot ve çözücüdür, oyuncu testi değildir.");
            report.AppendLine();
            report.AppendLine("| # | Konsept | Amaç | Tahta | Hamle | En kısa çözüm | Tehdit ayarı | Kazandıran ilk hamle | Açgözlü botun kaybı | Deneme |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (var r in rows) report.AppendLine(r);
            Directory.CreateDirectory(Path.Combine(RepoRoot(), "docs", "konsept"));
            File.WriteAllText(Path.Combine(RepoRoot(), "docs", "konsept", "URETIM.md"), report.ToString(), new UTF8Encoding(false));
        }
        return 0;
    }

    static string HazardSummary(HazardData h) => h.Kind switch
    {
        "rot" => $"her {h.Every} hamlede çöküş",
        "fuse" => $"fitil {string.Join("/", h.Cells.Select(c => c[2]))}",
        "cycle" => $"{h.Every} hamlede renk değişir",
        "raven" => $"yol {h.Cells.Count} hücre",
        "rain" => $"her {h.Every} hamlede yağmur, sınır {h.Max}",
        "rival" => $"rakip bitişi {h.Max}",
        "moss" => $"her {h.Every} hamlede yayılır, sınır {h.Max}",
        "balloon" => $"{h.Cells.Count} balon, her {h.Every} hamlede yükselir",
        "orders" => $"sipariş süreleri {string.Join("/", h.Orders)}",
        "fire" => $"ateş {h.Max} hamle",
        _ => h.Kind,
    };

    static LevelData ConceptBoard(Random rng, ConceptSpec spec, string id)
    {
        var d = new LevelData { Id = id, Width = spec.W, Height = spec.H, SchemaVersion = 2, TrialType = spec.Type };
        if (spec.Specials) d.Capabilities.Add("specials");
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
                var cell = new CellData { X = x, Y = y, Active = m != '#' };
                if (cell.Active)
                {
                    int n = spec.MinLayers + rng.Next(spec.MaxLayers - spec.MinLayers + 1);
                    for (int l = 0; l < n; l++) cell.Layers.Add(ColorNames[col[x, y, l]]);
                }
                d.Cells.Add(cell);
            }
        return d;
    }

    static List<Command> ConceptSolve(LevelData d, ConceptSpec spec, int limit)
    {
        int keep = d.MoveLimit;
        d.MoveLimit = limit;
        LevelState s;
        try { s = LevelLoader.ToState(d); } catch (FormatException) { d.MoveLimit = keep; return null; }
        d.MoveLimit = keep;
        if (s.IsTerminal || !RulesEngine.HasLegalMove(s)) return null;
        var bfs = LevelSolver.Solve(s, spec.Budget);
        List<Command> path = bfs.Outcome == SolveOutcome.Solved ? bfs.Solution : null;
        if (path == null)
        {
            var plan = HintPlanner.Plan(s, spec.Budget * 2, 0);
            if (plan.IsVerified) path = plan.Path;
        }
        return path != null && HintPlanner.Verify(s, path) ? path : null;
    }

    static ConceptMetrics ConceptMeasure(LevelData d, ConceptSpec spec)
    {
        var path = ConceptSolve(d, spec, d.MoveLimit);
        if (path == null) return null;
        var s = LevelLoader.ToState(d);
        var m = new ConceptMetrics { Solution = path, Steps = path.Count(c => c.Type != CommandType.UseSpecial) };
        var g = s.Clone();
        for (int guard = 0; guard < 60 && !g.IsTerminal; guard++) if (!RulesEngine.TryApply(g, Greedy(g)).Accepted) break;
        m.GreedyWins = g.Status == GameStatus.Won;
        m.GreedyLoss = g.Status == GameStatus.LostHazard ? LossName(g.Hazard.Loss) : g.Status == GameStatus.Won ? "kazanır" : g.Status == GameStatus.LostOutOfMoves ? "hamle biter" : "hamle kalmaz";
        if (m.GreedyWins || g.Status != GameStatus.LostHazard) { m.GreedyWins = true; return m; } // kaybı tehdit değilse kabul edilmez
        m.GreedyWinsFree = Play(LevelLoader.ToState(Free(d)), Greedy);
        m.BiggestWins = Play(s, Biggest);
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
        return m;
    }

    static string LossName(HazardLoss l) => l switch
    {
        HazardLoss.Unreachable => "hedef taşları kaybolur",
        HazardLoss.Fuse => "fitil patlar",
        HazardLoss.Raven => "kuzgun yuvaya varır",
        HazardLoss.Overflow => "yığın taşar",
        HazardLoss.Rival => "rakip önce bitirir",
        HazardLoss.Moss => "yosun tahtayı sarar",
        HazardLoss.Balloon => "balon kaçar",
        HazardLoss.Order => "sipariş yanar",
        HazardLoss.Fire => "ateş söner",
        _ => l.ToString(),
    };

    /// <summary>Tehditsiz kopya (balon hedefi de çıkar: balon yalnız tehditle var).</summary>
    static LevelData Free(LevelData d)
    {
        var f = LevelParser.Parse(ConceptJson(WithStars(d)), d.Id);
        f.Hazard = null;
        f.Goals.RemoveAll(g => g.Type == "collectToken" && g.Kind == "balloon");
        f.MoveLimit = d.MoveLimit;
        return f;
    }

    static LevelData WithStars(LevelData d) { if (d.StarThresholds == null) d.StarThresholds = new[] { 1, 2 }; return d; }

    static HazardData Hz(string kind, int every = 1, int max = 0) => new HazardData { Kind = kind, Every = every, Max = max };

    /// <summary>Sipariş süreleri: tehditsiz çözümün her aşamada harcadığı hamle (+1 pay), sonra sıkı → gevşek.</summary>
    static List<HazardData> OrdersCandidates(LevelData d, List<Command> free)
    {
        var s = LevelLoader.ToState(d);
        var used = new List<int> { 0 };
        foreach (var c in free)
        {
            int before = s.ActiveStage;
            RulesEngine.TryApply(s, c);
            if (c.Type != CommandType.UseSpecial) used[used.Count - 1]++;
            if (s.ActiveStage != before) used.Add(0);
        }
        if (used.Count < 3) return null;
        var list = new List<HazardData>();
        for (int pad = 0; pad <= 2; pad++)
        {
            var h = Hz("orders");
            foreach (var u in used.Take(3)) h.Orders.Add(Math.Max(2, u + pad));
            list.Add(h);
        }
        return list;
    }

    static List<ConceptSpec> ConceptSpecs()
    {
        Func<double, Func<Random, LevelData, bool>> oneColor = share => (r, d) =>
        {
            var c = ColorNames.OrderByDescending(x => Total(d, x)).First();
            d.Goals.Add(Goal("collectColor", c, Math.Max(6, (int)(Total(d, c) * share))));
            return true;
        };
        Func<double, Func<Random, LevelData, bool>> twoColors = share => (r, d) =>
        {
            var cs = ColorNames.Where(c => Total(d, c) >= 6).OrderByDescending(c => Total(d, c)).Take(2).ToArray();
            if (cs.Length < 2) return false;
            foreach (var c in cs) d.Goals.Add(Goal("collectColor", c, Math.Max(5, (int)(Total(d, c) * share))));
            return true;
        };
        return new List<ConceptSpec>
        {
            // 1 Çürüyen Zemin: alt sıra çöker, hedef rengi taşlar düşerse hedef tamamlanamaz.
            new ConceptSpec { Type = "concept_rot", Title = "Çürüyen Zemin", Intent = "Alttan baskı: önce alt sıraları temizle", W = 6, H = 7, Colors = 3, MinLayers = 2, MaxLayers = 3,
                Goals = oneColor(0.8),
                Hazards = (r, d) => new List<HazardData> { Hz("rot", 2), Hz("rot", 3), Hz("rot", 4) } },
            // 2 Fitil: iki sayaçlı yığın, sayaç bitmeden boşaltılmalı.
            new ConceptSpec { Type = "concept_fuse", Title = "Fitil", Intent = "Yerel geri sayım: doğru yığını zamanında boşalt", W = 6, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3,
                Goals = twoColors(0.55),
                Hazards = (r, d) =>
                {
                    var cells = d.Cells.Where(c => c.Active && c.Layers.Count >= 3 && c.X > 0 && c.X < d.Width - 1 && c.Y > 0 && c.Y < d.Height - 1).OrderBy(_ => r.Next()).Take(2).ToList();
                    if (cells.Count < 2 || Math.Abs(cells[0].X - cells[1].X) + Math.Abs(cells[0].Y - cells[1].Y) < 3) return null;
                    var list = new List<HazardData>();
                    foreach (var (a, b) in new[] { (3, 5), (4, 5), (4, 6), (5, 6), (5, 7) })
                    {
                        var h = Hz("fuse");
                        h.Cells.Add(new[] { cells[0].X, cells[0].Y, a });
                        h.Cells.Add(new[] { cells[1].X, cells[1].Y, b });
                        list.Add(h);
                    }
                    return list;
                } },
            // 3 Dönen Hedef: iki renk sırayla etkin.
            new ConceptSpec { Type = "concept_cycle", Title = "Dönen Hedef", Intent = "Zamanlama: doğru renk etkinken topla", W = 6, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3,
                Goals = twoColors(0.5),
                Hazards = (r, d) =>
                {
                    var cs = d.Goals.Select(g => g.Color).ToList();
                    return new List<HazardData> { Cyc(cs, 2), Cyc(cs, 3) };
                } },
            // 4 Kuzgun: ortadaki satırda soldan sağa, sonra yukarı yuvaya.
            new ConceptSpec { Type = "concept_raven", Title = "Kuzgun", Intent = "Hareketli düşman: yolunu bir yığını boşaltarak kes", W = 7, H = 7, Colors = 3, MinLayers = 2, MaxLayers = 3,
                Goals = twoColors(0.5),
                Hazards = (r, d) =>
                {
                    var path = new List<int[]>();
                    for (int x = 0; x < d.Width; x++) path.Add(new[] { x, 4 });
                    for (int y = 3; y >= 1; y--) path.Add(new[] { d.Width - 1, y });
                    var list = new List<HazardData>();
                    for (int cut = 4; cut >= 0; cut--)
                    {
                        var h = Hz("raven");
                        h.Cells.AddRange(path.Skip(cut));
                        list.Add(h);
                    }
                    return list;
                } },
            // 5 Yağmur: üç sabit yağmur noktası, sınır aşılırsa kayıp.
            new ConceptSpec { Type = "concept_rain", Title = "Yağmur", Intent = "Taş eklenir: yağmur alan yığınları alçak tut", W = 6, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3,
                Goals = twoColors(0.55),
                Hazards = (r, d) =>
                {
                    var spots = d.Cells.Where(c => c.Active && c.Layers.Count == 3).OrderBy(_ => r.Next()).Take(3).ToList();
                    if (spots.Count < 3) return null;
                    var junk = ColorNames.Take(3).Where(c => d.Goals.All(g => g.Color != c)).DefaultIfEmpty("purple").First();
                    var list = new List<HazardData>();
                    foreach (var (every, max) in new[] { (1, 4), (2, 4), (1, 5), (2, 5) })
                    {
                        var h = Hz("rain", every, max);
                        foreach (var s in spots) h.Volleys.Add(new List<(int, int, string)> { (s.X, s.Y, junk) });
                        list.Add(h);
                    }
                    return list;
                } },
            // 6 Gölge Rakip: rakip her hamlede bir adım, 5+ grup geri iter.
            // v14 (zincir yok): 4 renkle gruplar küçük; 5+ grubu oyuncu kurar (rakip bitişi 2–10, pay 4).
            new ConceptSpec { Type = "concept_rival", Title = "Gölge Rakip", Intent = "Saf yarış: büyük gruplarla rakibi geri it", W = 7, H = 7, Colors = 4, MinLayers = 1, MaxLayers = 2, Slack = 4,
                Goals = twoColors(0.6),
                Hazards = (r, d) => Enumerable.Range(2, 9).Select(k => Hz("rival", 1, k)).ToList() },
            // 7 Yayılan Yosun: iki kaynak, her iki hamlede yayılır.
            new ConceptSpec { Type = "concept_moss", Title = "Yayılan Yosun", Intent = "Yayılma: yosunu kaynağında söndür", W = 6, H = 7, Colors = 3, MinLayers = 2, MaxLayers = 3,
                Goals = twoColors(0.5),
                Hazards = (r, d) =>
                {
                    var src = d.Cells.Where(c => c.Active && c.Layers.Count >= 2 && c.X > 0 && c.X < d.Width - 1 && c.Y > 0 && c.Y < d.Height - 1).OrderBy(_ => r.Next()).Take(2).ToList();
                    if (src.Count < 2) return null;
                    var list = new List<HazardData>();
                    foreach (var (every, max) in new[] { (1, 6), (1, 8), (2, 5), (2, 6), (2, 8) })
                    {
                        var h = Hz("moss", every, max);
                        foreach (var s in src) h.Cells.Add(new[] { s.X, s.Y });
                        list.Add(h);
                    }
                    return list;
                } },
            // 8 Balonlar: alt sırada üç balon, yükselirken patlatılmalı.
            new ConceptSpec { Type = "concept_balloon", Title = "Balonlar", Intent = "Kaçan hedef: balonun yolundaki yığını boşalt", W = 6, H = 7, Colors = 3, MinLayers = 2, MaxLayers = 3,
                Goals = (r, d) => { oneColor(0.45)(r, d); d.Goals.Add(new GoalData { Type = "collectToken", Kind = "balloon", Amount = 3 }); return true; },
                Hazards = (r, d) =>
                {
                    var xs = Enumerable.Range(0, d.Width).OrderBy(_ => r.Next()).Take(3).OrderBy(x => x).ToArray();
                    var list = new List<HazardData>();
                    foreach (var every in new[] { 1, 2 })
                    {
                        var h = Hz("balloon", every);
                        foreach (var x in xs) h.Cells.Add(new[] { x, d.Height - 1 });
                        list.Add(h);
                    }
                    return list;
                } },
            // 9 Sipariş Kuyruğu: üç aşama, her birinin kendi süresi.
            new ConceptSpec { Type = "concept_orders", Title = "Sipariş Kuyruğu", Intent = "Sıralama: siparişleri süresinde teslim et", W = 6, H = 6, Colors = 3, MinLayers = 2, MaxLayers = 3, OrdersFromSolution = true,
                Goals = (r, d) =>
                {
                    var order = ColorNames.Take(3).Where(c => Total(d, c) >= 6).OrderBy(_ => r.Next()).ToList();
                    if (order.Count < 3) return false;
                    d.Capabilities.Add("stages");
                    for (int i = 0; i < 3; i++) d.Goals.Add(StageGoal(order[i], Math.Max(4, (int)(Total(d, order[i]) * 0.35)), i));
                    return true;
                } },
            // 10 Kombo Ateşi: 5+ grup patlatmadan en çok N hamle (v14, kullanıcı 2026-11-16: zincir tamamen kaldırılsın).
            new ConceptSpec { Type = "concept_fire", Title = "Kombo Ateşi", Intent = "Ritim: büyük grupları kesmeden sürdür", W = 6, H = 6, Colors = 3, MinLayers = 3, MaxLayers = 4,
                Goals = oneColor(0.6),
                Hazards = (r, d) => new List<HazardData> { Hz("fire", 1, 2), Hz("fire", 1, 3) } },
        };
    }

    static HazardData Cyc(List<string> colors, int every)
    {
        var h = Hz("cycle", every);
        h.Colors.AddRange(colors);
        return h;
    }

    static string ConceptJson(LevelData d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"schemaVersion\": 2,");
        sb.AppendLine($"  \"id\": \"{d.Id}\",");
        sb.AppendLine($"  \"capabilities\": [{string.Join(", ", d.Capabilities.Distinct().Select(c => $"\"{c}\""))}],");
        sb.AppendLine($"  \"title\": \"{Esc(d.Title ?? "")}\",");
        sb.AppendLine($"  \"trialType\": \"{d.TrialType}\",");
        sb.AppendLine("  \"rules\": [],");
        sb.AppendLine($"  \"width\": {d.Width},");
        sb.AppendLine($"  \"height\": {d.Height},");
        sb.AppendLine($"  \"moveLimit\": {d.MoveLimit},");
        sb.AppendLine("  \"rollEnabled\": false,");
        sb.AppendLine("  \"startingRoll\": false,");
        sb.AppendLine($"  \"stars\": [{d.StarThresholds[0]}, {d.StarThresholds[1]}],");
        if (d.Hazard != null) sb.AppendLine($"  \"hazard\": {HazardJson(d.Hazard)},");
        sb.AppendLine("  \"goals\": [");
        sb.AppendLine(string.Join(",\n", d.Goals.Select(g => g.Type == "collectToken"
            ? $"    {{ \"type\": \"collectToken\", \"kind\": \"{g.Kind}\", \"amount\": {g.Amount}{(g.Stage > 0 ? $", \"stage\": {g.Stage}" : "")} }}"
            : $"    {{ \"type\": \"{g.Type}\", \"color\": \"{g.Color}\", \"amount\": {g.Amount}{(g.Stage > 0 ? $", \"stage\": {g.Stage}" : "")} }}")));
        sb.AppendLine("  ],");
        sb.AppendLine("  \"cells\": [");
        sb.AppendLine(string.Join(",\n", d.Cells.Select(c =>
        {
            var p = new List<string> { $"\"x\": {c.X}", $"\"y\": {c.Y}", $"\"active\": {(c.Active ? "true" : "false")}" };
            if (c.Layers.Count > 0) p.Add($"\"layers\": [{string.Join(", ", c.Layers.Select(l => $"\"{l}\""))}]");
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

    static string HazardJson(HazardData h)
    {
        var p = new List<string> { $"\"kind\": \"{h.Kind}\"" };
        if (h.Every != 1) p.Add($"\"every\": {h.Every}");
        if (h.Max != 0) p.Add($"\"max\": {h.Max}");
        if (h.Colors.Count > 0) p.Add($"\"colors\": [{string.Join(", ", h.Colors.Select(c => $"\"{c}\""))}]");
        string key = h.Kind == "raven" ? "path" : h.Kind == "fuse" ? "fuses" : "cells";
        if (h.Cells.Count > 0) p.Add($"\"{key}\": [{string.Join(", ", h.Cells.Select(c => "[" + string.Join(", ", c) + "]"))}]");
        if (h.Volleys.Count > 0) p.Add($"\"volleys\": [{string.Join(", ", h.Volleys.Select(v => "[" + string.Join(", ", v.Select(t => $"[{t.X}, {t.Y}, \"{t.Color}\"]")) + "]"))}]");
        if (h.Orders.Count > 0) p.Add($"\"orders\": [{string.Join(", ", h.Orders)}]");
        return "{ " + string.Join(", ", p) + " }";
    }
}
