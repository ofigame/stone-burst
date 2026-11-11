using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

/// <summary>
/// vr1 (kullanıcı, 2026-11-11: "1000 bölüme uygula yeni tahtalar yap … pilottaki iki ayrı tahta güzel, bunlardan yap,
/// çeşit çeşit yap"; "pilotla yaptığın değişiklikleri gerçek oyunumuza uygula"): pilotta denenen tahta aileleri ana
/// kataloğa dağıtılır.
///   Dokunulmayan: 1–20, öğretici halka bölümleri, türlü bölümler (patron, kesin hamle, kristal, sıra, zincir…).
///   Seçilen: 21+ sade bölümlerin her ikincisi. Aile sırası bölüm ilerledikçe açılır; iki alan en sık.
///   Her aday tohumlu üretilir, kayıtlı çözüm motorla yeniden yürütülüp kazandığı doğrulanır; bulunamazsa sıradaki aile,
///   o da olmazsa eski bölüm korunur. Çıktı: Resources/Levels/level_NNN.json + levels_vr1/level_NNN.json (kaynak; build
///   bu dosyayı metin kaynağına tercih eder) + docs/vr1/YENI_TAHTALAR_RAPORU.md.
/// Kullanım: dotnet run -c Release -- families [tohum] [başlangıç] [bitiş]
/// </summary>
static partial class Program
{
    static readonly int[] TutorialLevels = { 1, 2, 11, 21, 41, 61, 121, 201, 301 };
    static readonly string[] FamilyNames = { "twin", "wide", "compact", "bridge", "stages", "seal", "twin_stages", "seal_room", "twin_seal", "wing" };

    static string VrSourceDir => Path.Combine(Root, "levels_vr1");
    static bool FamDry => Environment.GetEnvironmentVariable("KATKAT_FAM_DRY") == "1";

    /// <summary>Bölüm numarasına göre açık aileler (ilk görünüşte tür tanıtım kartı oyunda kendiliğinden çıkar).</summary>
    static List<string> FamiliesAt(int n)
    {
        // Kullanıcı (2026-11-11): "pilottakinden daha farklı, daha gelişmiş olsunlar" → birleşik aileler (twin_stages,
        // seal_room, twin_seal) ve 200+ bölümlerde rastgele buz.
        var order = new List<string> { "twin", "wide", "stages", "twin_stages", "compact", "twin", "bridge", "seal", "seal_room", "twin_seal", "wing" };
        return order.Where(f => f switch
        {
            "wide" => n >= 22, "compact" => n >= 31, "twin" => n >= 41, "bridge" => n >= 61,
            "stages" => n >= 81, "seal" => n >= 151, "twin_stages" => n >= 201, "seal_room" => n >= 251, "twin_seal" => n >= 351,
            "wing" => n >= 601, // geçit/düğme ana oyunda 601'de tanıtılıyor
            _ => false,
        }).ToList();
    }

    static int Families(string[] args)
    {
        int seed = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 20261111;
        int from = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 21;
        int to = args.Length > 3 ? int.Parse(args[3], CultureInfo.InvariantCulture) : 1000;
        Directory.CreateDirectory(VrSourceDir);
        var dir = Path.Combine(Root, "Assets", "KatKat", "Resources", "Levels");
        // Aday bölümler: sade (tür yok, kural yok), öğretici değil, 21+; her ikincisi.
        var plain = new List<int>();
        for (int n = 21; n <= 1000; n++)
        {
            if (Array.IndexOf(TutorialLevels, n) >= 0) continue;
            var d = LevelParser.Parse(File.ReadAllText(Path.Combine(dir, $"level_{n:000}.json")), $"level_{n:000}");
            // Yeniden çalıştırmada kararlı aday listesi: önceki turda aileye dönüştürülmüş bölümler de aday sayılır.
            if ((!string.IsNullOrEmpty(d.TrialType) && Array.IndexOf(FamilyNames, d.TrialType) < 0) || d.Rules.Count > 0) continue;
            plain.Add(n);
        }
        var picked = plain.Where((n, i) => i % 2 == 1).Where(n => n >= from && n <= to).ToList();
        var reportList = picked.ToList(); // tamamlama turunda picked daralır; rapor bütün seçilenleri yazar
        // Aile ataması: bölüm sırasıyla dönen liste (her bölümün açık aileleri içinde).
        var assign = new Dictionary<int, string>();
        int k = 0;
        foreach (var n in plain.Where((x, i) => i % 2 == 1))
        {
            var fams = FamiliesAt(n);
            if (fams.Count == 0) { k++; continue; }
            assign[n] = fams[k % fams.Count];
            k++;
        }
        Console.WriteLine($"families: sade {plain.Count}, seçilen {picked.Count} ({from}–{to})");
        var results = new ConcurrentDictionary<int, string>();
        // Tamamlama turu (KATKAT_FAM_REFILL=1): önceki raporda planlanan aileyi alamamış bölümler (korunan ya da yedek
        // aileye düşen) yeniden denenir; diğer satırlar ve dosyalar olduğu gibi kalır. Başarısız denemede mevcut dosya korunur.
        var refill = Environment.GetEnvironmentVariable("KATKAT_FAM_REFILL") == "1";
        var keptOnly = new HashSet<int>(); var prevLines = new Dictionary<int, string>();
        if (refill)
        {
            var reportPath = Path.Combine(Root, "docs", "vr1", "YENI_TAHTALAR_RAPORU.md");
            foreach (var line in File.ReadAllLines(reportPath))
            {
                var cols = line.Split('|');
                if (cols.Length < 3 || !int.TryParse(cols[1].Trim(), out var ln)) continue;
                var fcol = cols[2].Trim();
                if (fcol.StartsWith("korundu")) keptOnly.Add(ln);
                prevLines[ln] = line;
                if (!fcol.StartsWith("korundu") && !fcol.Contains("(planlanan")) results[ln] = line;
            }
            picked = picked.Where(x => !results.ContainsKey(x)).ToList();
            var only = Environment.GetEnvironmentVariable("KATKAT_FAM_ONLY");
            if (!string.IsNullOrEmpty(only))
            {
                var set = only.Split(',').Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture)).ToHashSet();
                foreach (var kv in prevLines) if (!set.Contains(kv.Key)) results[kv.Key] = kv.Value; // diğer satırlar aynen
                foreach (var x in set) { results.TryRemove(x, out _); keptOnly.Add(x); }
                picked = reportList.Where(set.Contains).ToList();
            }
            Console.WriteLine($"families tamamlama: {picked.Count} bölüm yeniden denenecek");
        }
        Parallel.ForEach(picked, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, n =>
        {
            if (!assign.TryGetValue(n, out var fam)) { results[n] = "| " + n + " | — | korundu (aile yok) |"; return; }
            var original = LevelParser.Parse(File.ReadAllText(Path.Combine(dir, $"level_{n:000}.json")), $"level_{n:000}");
            string sibling = fam switch
            {
                "twin_stages" => "stages", "seal_room" => "seal", "twin_seal" => "seal", "wing" => "seal", "bridge" => "twin",
                "stages" => "twin_stages", "seal" => "twin_seal", "compact" => "bridge", _ => "bridge",
            };
            if (FamiliesAt(n).IndexOf(sibling) < 0) sibling = "twin";
            // Tamamlamada yedek "geniş"e düşmüş bölüm (dosyası zaten yeni) yalnız planlanan aile + kardeşiyle denenir.
            var tryOrder = (refill && !keptOnly.Contains(n) ? new List<string> { fam, sibling } : new List<string> { fam, sibling, "twin", "wide" }).Distinct();
            string prevLine = refill && prevLines.TryGetValue(n, out var pl) ? pl : null;
            foreach (var f in tryOrder)
            {
                var spec = FamilySpec(f, n, new Random(seed + n * 7919 + f.GetHashCode() % 997));
                var rng = new Random(seed + n * 104729);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                LevelData found = null; PilotMetrics fm = null; int tries = 0; var diag = new int[5]; long stepSum = 0;
                while (found == null && sw.Elapsed.TotalSeconds < (f.Contains("seal") ? 150 : 90))
                {
                    tries++;
                    var d = PilotBoard(rng, spec, $"level_{n:000}");
                    if (!spec.Shape(rng, d)) { diag[0]++; continue; }
                    var m = PilotMeasure(d, spec, 0);
                    if (m == null) { diag[1]++; continue; }
                    d.MoveLimit = m.Steps + spec.Slack;
                    var m2 = PilotMeasure(d, spec, d.MoveLimit);
                    if (m2 == null) { diag[2]++; continue; }
                    if (!spec.Accept(m2, d)) { diag[3]++; stepSum += m2.Steps; if (m2.GreedyWins) diag[4]++; continue; }
                    // Stres kapısı (LevelLab stress ile aynı ölçü): bot 64 oyunda en az bir kez kazanmalı.
                    if (!StressBotWins(d, n)) { diag[2]++; continue; }
                    found = d; fm = m2;
                }
                if (Environment.GetEnvironmentVariable("KATKAT_FAM_DIAG") == "1")
                    Console.WriteLine($"  diag {n} {f}: deneme {tries} şekil {diag[0]} çözümsüz {diag[1]} sınırda {diag[2]} red {diag[3]} (ort. çözüm {(diag[3] > 0 ? stepSum / (double)diag[3] : 0):0.0}, açgözlü kazanır {diag[4]})");
                if (found == null) continue;
                found.Title = original.Title;
                found.TrialType = f;
                found.Solution.Clear();
                foreach (var c in fm.Solution) found.Solution.Add(TrialStep(c));
                var st = LevelLoader.ToState(found);
                foreach (var c in fm.Solution) RulesEngine.TryApply(st, c);
                if (st.Status != GameStatus.Won) continue;
                found.StarThresholds = new[] { Math.Max(1, (int)(st.Score * 0.75)), Math.Max(2, st.Score) };
                var json = PilotJson(found);
                if (!FamDry) File.WriteAllText(Path.Combine(dir, found.Id + ".json"), json, new UTF8Encoding(false));
                if (!FamDry) File.WriteAllText(Path.Combine(VrSourceDir, found.Id + ".json"), json, new UTF8Encoding(false));
                results[n] = $"| {n} | {f}{(f != fam ? $" (planlanan {fam})" : "")} | {found.Width}×{found.Height} | {found.MoveLimit} | {fm.Steps} | {fm.Winners}/{fm.Legal} | {(fm.GreedyWins ? "kazanır" : "kaybeder")} | {fm.MaxChainDepth} | {tries} |";
                Console.WriteLine($"level_{n:000} {f}: {found.Width}x{found.Height} hamle {found.MoveLimit} çözüm {fm.Steps} açgözlü {(fm.GreedyWins ? "+" : "-")} deneme {tries}");
                return;
            }
            results[n] = prevLine ?? $"| {n} | korundu ({fam} bulunamadı) | | | | | | | |";
            Console.WriteLine($"level_{n:000}: korundu ({fam})");
        });
        var sb = new StringBuilder();
        sb.AppendLine("# Yeni tahta aileleri — ana katalog (LevelLab families, tohum " + seed + ")");
        sb.AppendLine();
        sb.AppendLine("Pilotta denenen aileler 21+ sade bölümlerin her ikincisine dağıtıldı. 1–20, öğretici bölümler ve türlü bölümler değişmedi.");
        sb.AppendLine("Her bölümün kayıtlı çözümü motorla yeniden yürütülüp kazandığı doğrulandı. Ölçümler bot/çözücü ölçümüdür; oyuncu testi değildir.");
        sb.AppendLine();
        sb.AppendLine("| Bölüm | Aile | Tahta | Hamle | Kayıtlı çözüm | Kazandıran ilk hamle | Açgözlü | En derin zincir | Deneme |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var n in reportList) sb.AppendLine(results.TryGetValue(n, out var line) ? line : $"| {n} | ? |");
        var counts = results.Values.Select(l => l.Split('|')[2].Trim().Split(' ')[0]).GroupBy(x => x).Select(g => $"{g.Key}: {g.Count()}");
        sb.AppendLine();
        sb.AppendLine("Dağılım: " + string.Join(", ", counts));
        if (!FamDry) File.WriteAllText(Path.Combine(Root, "docs", "vr1", "YENI_TAHTALAR_RAPORU.md"), sb.ToString(), new UTF8Encoding(false));
        return 0;
    }

    /// <summary>Aile + bölüm numarasına göre tahta tarifi (renk, katman, pay bölüm ilerledikçe zorlaşır).</summary>
    static PilotSpec FamilySpec(string fam, int n, Random r)
    {
        // Zorluk eğrisi (2026-11-11 kalite turu: ilk üretimde geç bölümler fazla kolaydı): en kısa çözüm, hedef oranı,
        // katman ve pay bölüm ilerledikçe sıkılaşır; 250+ açgözlü bot kaybetmeli, 500+ ilk hamlelerin en çok %70'i kazandırmalı.
        int slack = n < 100 ? 3 : n < 300 ? 2 : 1;
        int colors = n < 300 ? 3 : 4;
        int minL = n < 200 ? 2 : 3, maxL = n < 100 ? 3 : n < 400 ? 3 : 4;
        // 2026-11-11 tamamlama: 8–9 adım şartı çözücü bütçesinde neredeyse hiç karşılanmıyordu (teşhis: twin/stages çoğu çözümsüz,
        // çözülenler 6–7 adım) → üst sınır 7; zorluk az pay + açgözlü botun kaybetmesi + kazandıran ilk hamle oranıyla gelir.
        int minSteps = n < 150 ? 5 : n < 300 ? 6 : 7;
        _goalBoost = n < 150 ? 0.0 : n < 300 ? 0.05 : n < 500 ? 0.10 : n < 750 ? 0.15 : 0.20;
        Func<PilotMetrics, LevelData, bool> accept = (m, d) => m.Steps >= minSteps && (n < 250 || !m.GreedyWins) && (n < 500 || m.Winners * 10 <= m.Legal * 7);
        string[] Pick(params string[][] masks) => masks[r.Next(masks.Length)];
        var sp = new PilotSpec { Type = fam, Specials = true, Slack = slack, Accept = accept, Budget = n >= 500 ? 100000 : 60000 };
        if (n >= 200 && fam != "compact" && fam != "wide") sp.IceChance = n < 500 ? 0.06 : 0.10; // eski engel karışımı
        switch (fam)
        {
            case "twin":
            {
                // Çeşitler: yan yana (dikey boşluk), üst üste (yatay boşluk), basamaklı.
                var masks = new[]
                {
                    new[] { "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR" },
                    new[] { "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR" },
                    new[] { "....#RRR", "....#RRR", "....#RRR", "....#RRR", "....#RRR", "....#RRR" },
                    new[] { "......", "......", "......", "######", "RRRRRR", "RRRRRR", "RRRRRR" },
                    new[] { ".......", ".......", ".......", "#######", "RRRRRRR", "RRRRRRR", "RRRRRRR", "RRRRRRR" },
                    new[] { "...##RR", "...##RR", "....#RR", "....#RR", "...#RRR", "...#RRR" },
                };
                sp.Mask = Pick(masks); sp.H = sp.Mask.Length; sp.W = sp.Mask[0].Length; sp.Caps = new[] { "areas" };
                sp.Colors = colors; sp.MinLayers = minL; sp.MaxLayers = maxL; sp.Shape = TwinGoals; break;
            }
            case "wide":
                sp.W = r.Next(2) == 0 ? 8 : 7; sp.H = 8; sp.Colors = Math.Min(4, colors + 1); sp.MinLayers = n < 300 ? 1 : 2; sp.MaxLayers = n < 300 ? 2 : 3;
                sp.Slack = slack + 1; sp.Shape = TwoColorGoals; sp.Accept = (m, d) => m.Steps >= Math.Max(4, minSteps - 2) && (n < 400 || !m.GreedyWins); break;
            case "compact":
                sp.W = r.Next(2) == 0 ? 4 : 5; sp.H = sp.W == 4 ? 4 : 5; sp.Colors = 3; sp.MinLayers = 3; sp.MaxLayers = 4; sp.Budget = 120000;
                sp.Shape = TwoColorGoals; sp.Accept = (m, d) => m.Steps >= Math.Max(4, minSteps - 1) && m.Winners >= 2 && (n < 300 || !m.GreedyWins); break;
            case "bridge":
                sp.Mask = Pick(
                    new[] { "...#...", "...#...", "...#...", ".......", "...#...", "...#...", "...#..." },
                    new[] { "...#...", ".......", "...#...", "...#...", "...#...", ".......", "...#..." },
                    new[] { ".......", ".......", ".......", "###.###", ".......", ".......", "......." });
                sp.W = 7; sp.H = 7; sp.Colors = colors; sp.MinLayers = minL; sp.MaxLayers = maxL; sp.Shape = TwoColorGoals; break;
            case "stages":
                sp.W = r.Next(2) == 0 ? 6 : 7; sp.H = 7; sp.Colors = 3; sp.MinLayers = minL; sp.MaxLayers = Math.Max(3, maxL); sp.Caps = new[] { "stages" };
                sp.Shape = StagedGoals(n < 300 ? 2 : r.Next(2) == 0 ? 2 : 3); break;
            case "seal":
            {
                int wave = n < 301 ? 1 : 1 + r.Next(2);
                sp.Mask = Pick(
                    new[] { ".....GG", ".....GG", ".....GG", ".......", ".......", ".......", "......." },
                    new[] { "GG.....", "GG.....", "GG.....", ".......", ".......", ".......", "......." },
                    new[] { "GGGGGGG", ".......", ".......", ".......", ".......", ".......", "......." });
                sp.W = 7; sp.H = 7; sp.Colors = 3; sp.MinLayers = wave >= 2 ? 3 : 2; sp.MaxLayers = wave >= 3 ? 4 : 3; sp.SealWave = wave;
                sp.Caps = new[] { "seal", "gate" }; sp.Shape = SealGoal; sp.Budget = 90000; sp.Accept = (m, d) => m.Steps >= minSteps - 2 && (n < 400 || !m.GreedyWins); break;
            }
            case "twin_stages":
            {
                // İki alan + aşama: aşama 0 rengi alan 0'da, aşama 1 rengi alan 1'de baskın → alanlar arası gidip gelme.
                sp.Mask = Pick(
                    new[] { "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR" },
                    new[] { ".......", ".......", ".......", "#######", "RRRRRRR", "RRRRRRR", "RRRRRRR", "RRRRRRR" });
                sp.H = sp.Mask.Length; sp.W = sp.Mask[0].Length; sp.Caps = new[] { "areas", "stages" };
                sp.Colors = 3; sp.MinLayers = minL; sp.MaxLayers = Math.Max(3, maxL); sp.Shape = TwinStageGoals; break;
            }
            case "seal_room":
            {
                // Mühürlü oda: ikinci odanın tamamı kapalı; tek hamlede zincir kurulunca açılır.
                int wave = 1;
                sp.Mask = Pick(
                    new[] { "....GGG", "....GGG", "....GGG", ".......", "....GGG", "....GGG", "....GGG" },
                    new[] { "GGGGGGG", "GGGGGGG", ".......", ".......", ".......", ".......", "......." });
                sp.W = 7; sp.H = 7; sp.Colors = 3; sp.MinLayers = 2; sp.MaxLayers = 3; sp.SealWave = wave; sp.Caps = new[] { "seal", "gate" };
                sp.Shape = SealGoal; sp.Budget = 90000; sp.Accept = (m, d) => m.Steps >= minSteps - 1 && (n < 400 || !m.GreedyWins); break;
            }
            case "twin_seal":
            {
                // İki alan + mühür: sağ alanın üst kısmı mühürlü (H); roket yalnız kendi alanında.
                sp.Mask = Pick(
                    new[] { "...#HHH", "...#HHH", "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR" },
                    new[] { "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#RRR", "...#HHH", "...#HHH" });
                sp.W = 7; sp.H = 7; sp.Colors = 3; sp.MinLayers = 2; sp.MaxLayers = 3; sp.SealWave = n < 601 ? 1 : 1 + r.Next(2);
                sp.Caps = new[] { "areas", "seal", "gate" }; sp.Shape = SealGoal; sp.Budget = 90000; sp.Accept = (m, d) => m.Steps >= minSteps - 1 && (n < 400 || !m.GreedyWins); break;
            }
            case "wing":
                sp.Mask = Pick(
                    new[] { "....GG", "....GG", "...IGG", "B..I..", "......", "......", "......" },
                    new[] { "GG....", "GG....", "GGI...", "..I..B", "......", "......", "......" });
                sp.W = 6; sp.H = 7; sp.Colors = 3; sp.MinLayers = 2; sp.MaxLayers = 3; sp.Caps = new[] { "gate", "ice" }; sp.Shape = WingGoal; break;
        }
        return sp;
    }

    /// <summary>Hedef oranı artışı (bölüm numarasına göre; iş parçacığı başına, FamilySpec ayarlar).</summary>
    [ThreadStatic] static double _goalBoost;

    static bool TwoColorGoals(Random r, LevelData d)
    {
        var cs = Colors2(r, d);
        if (cs.Length < 2) return false;
        foreach (var c in cs) d.Goals.Add(Goal("collectColor", c, Math.Max(4, (int)(Total(d, c) * (0.6 + _goalBoost)))));
        return true;
    }

    static Func<Random, LevelData, bool> StagedGoals(int stages) => (r, d) =>
    {
        var order = ColorNames.Where(c => Total(d, c) >= 6).OrderBy(_ => r.Next()).Take(stages).ToList();
        if (order.Count < stages) return false;
        for (int i = 0; i < stages; i++) d.Goals.Add(StageGoal(order[i], Math.Max(4, (int)(Total(d, order[i]) * ((stages == 2 ? 0.45 : 0.35) + _goalBoost * 0.6))), i));
        return true;
    };

    static bool SealGoal(Random r, LevelData d)
    {
        if (d.SealWave >= 2 && !ReachesDepth(d, d.SealWave)) d.SealWave = 1; // derin dalga kurulamıyorsa tahtayı atma, 1. dalgaya düş
        var c = ColorNames.OrderByDescending(x => d.Cells.Sum(cell => cell.GateLayers.Count(l => l == x))).First();
        int outside = Total(d, c, false), inside = Total(d, c) - outside;
        if (inside < 3) return false;
        d.Goals.Add(Goal("collectColor", c, outside + Math.Min(inside, 3)));
        return true;
    }

    static bool WingGoal(Random r, LevelData d)
    {
        var c = ColorNames.OrderByDescending(x => d.Cells.Sum(cell => cell.GateLayers.Count(l => l == x))).First();
        int outside = Total(d, c, false), inside = Total(d, c) - outside;
        if (inside < 3) return false;
        d.Goals.Add(Goal("collectColor", c, outside + 2));
        return true;
    }

    static bool TwinGoals(Random r, LevelData d)
    {
        string Best(int area) => ColorNames.OrderByDescending(x => d.Cells.Where(c => c.Area == area).Sum(c => c.Layers.Count(l => l == x))).First();
        var a = Best(0); var b = Best(1);
        int na = d.Cells.Where(c => c.Area == 0).Sum(c => c.Layers.Count(l => l == a));
        int nb = d.Cells.Where(c => c.Area == 1).Sum(c => c.Layers.Count(l => l == b));
        if (na < 4 || nb < 4) return false;
        if (a == b) { d.Goals.Add(Goal("collectColor", a, (int)((na + nb) * 0.6))); return true; }
        d.Goals.Add(Goal("collectColor", a, Math.Max(4, (int)(na * (0.6 + _goalBoost * 0.75)))));
        d.Goals.Add(Goal("collectColor", b, Math.Max(4, (int)(nb * (0.6 + _goalBoost * 0.75)))));
        return true;
    }
}

static partial class Program
{
    /// <summary>İki alan + aşama: aşama 0 alan 0'ın baskın rengi, aşama 1 alan 1'in (farklı) baskın rengi.</summary>
    static bool TwinStageGoals(Random r, LevelData d)
    {
        int In(int area, string c) => d.Cells.Where(x => x.Area == area).Sum(x => x.Layers.Count(l => l == c));
        var a = ColorNames.OrderByDescending(c => In(0, c) - In(1, c)).First();
        var b = ColorNames.Where(c => c != a).OrderByDescending(c => In(1, c) - In(0, c)).First();
        if (In(0, a) < 5 || In(1, b) < 5) return false;
        d.Goals.Add(StageGoal(a, Math.Max(4, (int)(Total(d, a) * 0.5)), 0));
        d.Goals.Add(StageGoal(b, Math.Max(4, (int)(Total(d, b) * 0.5)), 1));
        return true;
    }
}

static partial class Program
{
    /// <summary>LevelLab stress'in bot kapısı: üretim tohumlu 24 oyun + 40 yumuşak açgözlü oyunda en az bir galibiyet.</summary>
    static bool StressBotWins(LevelData d, int n)
    {
        int keep = d.MoveLimit;
        double r1 = BotRate(ParseJsonRoundTrip(d), d.MoveLimit, n, 24);
        d.MoveLimit = keep;
        if (r1 > 0) return true;
        var st = LevelLoader.ToState(d); var rr = new Random(n * 131 + 17);
        for (int g = 0; g < 40; g++) if (BotPlay(st.Clone(), rr, 0.35)) return true;
        return false;
    }
}
