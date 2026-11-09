using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// stress [oyun] [filtre]: derlenmiş bütün bölüm JSON'larına en sert sınama (v7, 2026-11-06).
//   - statik doğrulama, açılışta yasal hamle, hedef ≤ mevcut katman/yıldız/zarf/sünger
//   - kayıtlı çözüm kazanır ve hamle harcayan adım sayısı ≤ limit (tepsi "use" sayılmaz)
//   - iç boşluk / tahta dışı hücreler oyun boyunca etkin olmaz (geçit hariç)
//   - çok sayıda bot oyunu (yumuşak açgözlü, sıcak, düzgün rastgele + geçersiz komut yağmuru): istisna yok,
//     her adımda değişmezler (hamle ≥ 0, tepsi ≤ 4, hedef ilerlemesi azalmaz), geri al = önceki durum, determinizm
//   - bot en az bazen kazanır (üretimdeki 24 oyunluk tohum + ek oyunlar)
static partial class Program
{
    static int Stress(string[] args)
    {
        int games = args.Length > 1 ? int.Parse(args[1]) : 60;
        string filter = args.Length > 2 ? args[2] : null;
        var files = Directory.GetFiles(LevelsDir, "level_*.json").OrderBy(f => f, StringComparer.Ordinal)
            .Where(f => filter == null || Path.GetFileName(f).Contains(filter)).ToList();
        var lines = new string[files.Count];
        var botRates = new double[files.Count];
        int bad = 0;
        long totalGames = 0, totalSteps = 0; paintStalls = 0;
        var started = DateTime.Now;
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            var name = Path.GetFileNameWithoutExtension(files[i]);
            int n = LevelValidator.LevelNumber(name);
            var errs = new List<string>();
            long steps = 0; int played = 0;
            try
            {
                var d = LevelParser.Parse(File.ReadAllText(files[i]), Path.GetFileName(files[i]));
                errs.AddRange(LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)));
                // Hedef ≤ mevcut (statik doğrulamadan bağımsız ikinci sayım).
                foreach (var g in d.Goals)
                {
                    int avail = g.Type == "collectColor" || g.Type == "collectChain" ? d.Cells.Where(c => !c.Protected || g.Type == "collectColor").Sum(c => c.Layers.Count(l => l == g.Color) + c.GateLayers.Count(l => l == g.Color))
                        : g.Type == "collectStars" ? d.Cells.Count(c => c.HasBottomStar)
                        : g.Kind == "sponge" ? d.Cells.Count(c => c.SpongeColors.Count > 0)
                        : d.Cells.Count(c => !string.IsNullOrEmpty(c.EnvelopeColor));
                    if (g.Amount > avail) errs.Add($"hedef {g.Type} {g.Color}{g.Kind} {g.Amount} > mevcut {avail}");
                }
                if (errs.Count == 0)
                {
                    var rep = LevelValidator.ReplaySolution(d);
                    if (!rep.Success) errs.Add("kayıtlı çözüm: " + rep.Error);
                    int moveSteps = d.Solution.Count(s => s.Type != "use");
                    if (moveSteps > d.MoveLimit) errs.Add($"çözüm {moveSteps} hamle > limit {d.MoveLimit}");
                    var start = LevelLoader.ToState(d);
                    if (start.Status != GameStatus.Idle || RulesEngine.Evaluate(start) != GameStatus.Idle) errs.Add($"açılış durumu {RulesEngine.Evaluate(start)}");
                    if (LevelSolver.Moves(start).Count == 0) errs.Add("açılışta aday hamle yok");

                    // Tahta dışı (boşluk) hücreler: geçit olmayan etkin olmayan hücreler oyun boyunca öyle kalmalı.
                    var holes = new List<int>();
                    for (int k = 0; k < start.Board.Count; k++) if (!start.Board[k].Active && start.Board[k].GateId == 0) holes.Add(k);

                    var rng = new Random(n * 7717 + 3);
                    for (int g = 0; g < games && errs.Count < 5; g++)
                    {
                        int mode = g % 3; // 0 yumuşak açgözlü, 1 sıcak, 2 düzgün rastgele + geçersiz komutlar
                        var session = new GameSession(start.Clone());
                        int guard = 0;
                        while (!session.State.IsTerminal && guard++ < 400)
                        {
                            var s = session.State;
                            var moves = LevelSolver.Moves(s);
                            if (moves.Count == 0)
                            {
                                // Bilinen durum: yalnız grup kurmayan boya kaldıysa motor bölümü bitirmez (boya yasal), çözücü aday üretmez.
                                bool paintOnly = s.PaintLeft > 0 && s.MovesLeft > 0 && !GroupFinder.HasValidGroup(s.Board) && !RulesEngine.TrayUsable(s);
                                if (paintOnly)
                                {
                                    System.Threading.Interlocked.Increment(ref paintStalls);
                                    int sel = -1;
                                    for (int k = 0; k < s.Board.Count && sel < 0; k++) if (s.Board[k].IsSelectable) sel = k;
                                    var pc = Command.Paint(s.Board.X(sel), s.Board.Y(sel), (LayerColor)(((int)s.Board[sel].Top + 1) % 4));
                                    if (!session.Submit(pc).Accepted) { errs.Add($"oyun {g}: boya reddedildi"); break; }
                                    continue;
                                }
                                errs.Add($"oyun {g}: durum Idle ama aday hamle yok (HasLegalMove={RulesEngine.HasLegalMove(s)})"); break;
                            }
                            if (mode == 2 && rng.Next(3) == 0)
                            {
                                // Geçersiz/rasgele komut: reddedilmeli ya da kabul edilmeli, asla istisna atmamalı.
                                var junk = RandomCommand(s, rng);
                                var before = s.Clone();
                                var jr = RulesEngine.TryApply(s, junk);
                                if (!jr.Accepted && !s.ContentEquals(before)) errs.Add($"oyun {g}: reddedilen {junk} durumu değiştirdi");
                                if (s.IsTerminal) break;
                                continue;
                            }
                            Command cmd;
                            if (mode == 2) cmd = moves[rng.Next(moves.Count)];
                            else
                            {
                                var vals = new List<(Command c, double v)>();
                                foreach (var m in moves) { var t = s.Clone(); if (RulesEngine.TryApply(t, m).Accepted) vals.Add((m, Heur(t))); }
                                if (vals.Count == 0) { errs.Add($"oyun {g}: aday hamlelerin hiçbiri kabul edilmedi"); break; }
                                double temp = mode == 0 ? 0.35 : 1.5, max = vals.Max(t => t.v);
                                var w = vals.Select(t => Math.Exp((t.v - max) / (temp * 20))).ToList();
                                double roll = rng.NextDouble() * w.Sum(); int p = 0;
                                for (; p < w.Count - 1; p++) { roll -= w[p]; if (roll <= 0) break; }
                                cmd = vals[p].c;
                            }
                            var snap = s.Clone();
                            var goalsBefore = s.Goals.Select(x => x.Progress).ToArray();
                            // Determinizm: aynı komut kopyada aynı sonucu verir.
                            var twin = s.Clone();
                            var r = session.Submit(cmd);
                            RulesEngine.TryApply(twin, cmd);
                            var ns = session.State;
                            if (!r.Accepted) { errs.Add($"oyun {g}: aday {cmd} reddedildi ({r.Reason})"); break; }
                            steps++;
                            if (!twin.ContentEquals(ns)) errs.Add($"oyun {g}: {cmd} deterministik değil");
                            if (ns.MovesLeft < 0) errs.Add($"oyun {g}: hamle {ns.MovesLeft}");
                            if (ns.Tray.Count > LevelState.TrayCapacity) errs.Add($"oyun {g}: tepsi {ns.Tray.Count}");
                            for (int k = 0; k < goalsBefore.Length; k++) if (ns.Goals[k].Progress < goalsBefore[k]) errs.Add($"oyun {g}: hedef ilerlemesi azaldı");
                            foreach (var h in holes) if (ns.Board[h].Active || ns.Board[h].Layers.Count > 0) { errs.Add($"oyun {g}: boşluk hücresi {ns.Board.X(h)},{ns.Board.Y(h)} doldu/etkinleşti"); break; }
                            if (cmd.Type == CommandType.UseSpecial && ns.MovesLeft != snap.MovesLeft) errs.Add($"oyun {g}: tepsi kullanımı hamle harcadı");
                            if (ns.Status == GameStatus.Idle && RulesEngine.Evaluate(ns) != GameStatus.Idle) errs.Add($"oyun {g}: durum tutarsız");
                            // Geri al: ara sıra; önceki durumla aynı olmalı, sonra hamle yeniden uygulanır.
                            if (rng.Next(6) == 0 && !ns.IsTerminal)
                            {
                                if (!session.Undo() || !session.State.ContentEquals(snap)) errs.Add($"oyun {g}: geri al önceki durumu vermedi");
                                else session.Submit(cmd);
                            }
                            if (errs.Count >= 5) break;
                        }
                        if (guard >= 400) errs.Add($"oyun {g}: 400 adımda bitmedi");
                        if (session.State.Status == GameStatus.Won) { var c = session.State.Clone(); RulesEngine.Celebrate(c, CelebrationSeed(d.Id)); if (c.Status != GameStatus.Won) errs.Add("kutlama durumu bozdu"); }
                        played++;
                    }
                    // Bot en az bazen kazanır: üretim tohumu (24) + ek 40 oyun.
                    if (d.Capabilities.Contains("specials"))
                    {
                        double r1 = BotRate(ParseJsonRoundTrip(d), d.MoveLimit, n, 24);
                        var st = LevelLoader.ToState(d); var rr = new Random(n * 131 + 17); int w2 = 0;
                        for (int g = 0; g < 40; g++) if (BotPlay(st.Clone(), rr, 0.35)) w2++;
                        botRates[i] = (r1 * 24 + w2) / 64.0;
                        if (r1 * 24 + w2 == 0) errs.Add("bot 64 oyunda hiç kazanamadı");
                    }
                    else botRates[i] = -1;
                }
            }
            catch (Exception e) { errs.Add("İSTİSNA " + e.GetType().Name + ": " + e.Message + " @ " + (e.StackTrace ?? "").Split('\n').FirstOrDefault()?.Trim()); }
            System.Threading.Interlocked.Add(ref totalGames, played);
            System.Threading.Interlocked.Add(ref totalSteps, steps);
            if (errs.Count > 0) { System.Threading.Interlocked.Increment(ref bad); lines[i] = $"{name}: HATA {string.Join(" | ", errs.Distinct().Take(5))}"; }
            else lines[i] = $"{name}: tamam bot {(botRates[i] < 0 ? "-" : botRates[i].ToString("0.00", CultureInfo.InvariantCulture))}";
        });
        foreach (var l in lines) Console.WriteLine(l);
        Console.WriteLine($"stress: {files.Count - bad}/{files.Count} geçti; {totalGames} oyun, {totalSteps} adım, boya çıkmazı {paintStalls}, {(DateTime.Now - started).TotalMinutes:0.0} dk");
        return bad == 0 ? 0 : 1;
    }

    static long paintStalls;

    static Command RandomCommand(LevelState s, Random rng)
    {
        int w = s.Board.Width, h = s.Board.Height;
        int x = rng.Next(-1, w + 1), y = rng.Next(-1, h + 1);
        switch (rng.Next(5))
        {
            case 0: return Command.Tap(x, y);
            case 1: return Command.UseSpecial(rng.Next(-1, 5), x, y);
            case 2: return Command.Rotate(x, y);
            case 3: return Command.Paint(x, y, (LayerColor)rng.Next(4));
            default: return Command.Roll(rng.Next(-1, h + 1));
        }
    }
}
