using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// v5 üretici (docs/v5/KURALLAR_v5.md): dikdörtgen ve daha kalabalık tahtalar, özel taşlar, yıldız eşikleri.
// En kısa çözüm araması 7×8 tahtada patladığı için:
//   - çözüm: ışın araması (beam) — her derinlikte hedef ilerlemesine göre en iyi B durum;
//   - zorluk: oyuncu botu oyunları (yumuşak açgözlü, sabit tohum) — hamle limitiyle kazanma oranı.
// Kayıtlı çözüm ışın çözümüdür (en kısa olmak zorunda değil); yayın onayı yine report'taki gerçek tekrar.
//
// Profil anahtarları (gen5): w,h tahta; layers; colors; holes (aynalı boşluk); ice1/ice2/clip1/lock/env/rotor/stars;
//   goals=c:0.5; slack=2; bot=0.3-0.7 (bot kazanma aralığı); len=6-12; specials=1 (çözümde en az özel taş);
//   chains=1; beam=60; iters=60; title; tutorial; seed.
static partial class Program
{
    static int GenerateV5(string[] args)
    {
        var planPath = Path.Combine(Root, args.Length > 1 ? args[1] : "tools/LevelLab/plan5_006_030.txt");
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
            Directory.CreateDirectory(Path.Combine(Root, "levels_src5"));
            var path = Path.Combine(Root, "levels_src5", $"level_{pr.Number:000}.txt");
            if (File.Exists(path)) { results[i] = $"{pr.Number:000}: kaynak var, atlandı"; return; }
            var (d, ev) = Climb5(pr);
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

    static (LevelData, Eval) Climb5(Profile pr)
    {
        var rng = new Random(pr.Number * 7919 + pr.I("seed", 1));
        LevelData best = null;
        var bestEv = new Eval();
        int starts = pr.I("starts", 6);
        for (int s = 0; s < starts || (bestEv.Solution == null && s < 30); s++)
        {
            var d = RandomBoard5(pr, rng);
            var ev = Evaluate5(pr, d);
            if (ev.Score > bestEv.Score) { best = d; bestEv = ev; }
        }
        int iters = pr.I("iters", 60);
        for (int i = 0; i < iters && best != null; i++)
        {
            var cand = Clone(best);
            int ops = 1 + rng.Next(3);
            for (int k = 0; k < ops; k++) MutateGen(cand, pr, rng);
            var ev = Evaluate5(pr, cand);
            if (ev.Score >= bestEv.Score) { best = cand; bestEv = ev; }
        }
        if (best != null && bestEv.Solution != null)
        {
            SetGoals5(best, pr, bestEv.MoveLimit);
            best.Solution.Clear();
            foreach (var c in bestEv.Solution) best.Solution.Add(ToStep(c));
        }
        return (best, bestEv);
    }

    static LevelData RandomBoard5(Profile pr, Random rng)
    {
        int w = pr.I("w", 7), h = pr.I("h", 8);
        var (lmin, lmax) = pr.R("layers", 2, 3);
        var letters = Letters(pr);
        var d = new LevelData { SchemaVersion = 2, Id = $"level_{pr.Number:000}", Title = pr.S("title"), Width = w, Height = h };
        // Aynalı boşluklar: tahta şekli simetrik ve düzenli kalır.
        var holes = new HashSet<int>();
        int holeCount = pr.I("holes");
        int guard = 0;
        while (holes.Count < holeCount && guard++ < 200)
        {
            int x = rng.Next(w), y = rng.Next(h);
            holes.Add(y * w + x);
            holes.Add(y * w + (w - 1 - x));
        }
        var depth = new int[w * h];
        for (int i = 0; i < w * h; i++)
        {
            if (holes.Contains(i)) continue;
            depth[i] = rng.Next(lmin, lmax + 1);
            d.Cells.Add(new CellData { X = i % w, Y = i / w, Active = true });
        }
        var byIndex = d.Cells.ToDictionary(c => c.Y * w + c.X);
        var colorAt = new Dictionary<(int, int), char>();
        int blobMin = pr.I("blobMin", 2), blobMax = pr.I("blobMax", 6);
        for (int level = 0; level < lmax; level++)
        {
            var open = byIndex.Keys.Where(i => depth[i] > level).OrderBy(_ => rng.Next()).ToList();
            var done = new HashSet<int>();
            foreach (var seed in open)
            {
                if (done.Contains(seed)) continue;
                char col = letters[rng.Next(letters.Length)];
                int size = blobMin + rng.Next(blobMax - blobMin + 1);
                var frontier = new List<int> { seed };
                while (frontier.Count > 0 && size > 0)
                {
                    int j = frontier[rng.Next(frontier.Count)];
                    frontier.Remove(j);
                    if (done.Contains(j)) continue;
                    done.Add(j);
                    colorAt[(j, level)] = col;
                    size--;
                    foreach (var nb in Neighbours5(j, w, h))
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
        foreach (var l in pr.S("lock").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = l.Split(':');
            filled[at].LockColor = ColorOf(p[0].Trim()[0]);
            filled[at++].LockRemaining = int.Parse(p[1], CultureInfo.InvariantCulture);
        }
        foreach (var e in pr.S("env").Split(',', StringSplitOptions.RemoveEmptyEntries))
            filled[at++].EnvelopeColor = ColorOf(e.Trim()[0]);
        for (int k = 0; k < pr.I("stars"); k++) filled[at++].HasBottomStar = true;
        // M09 sünger: sponge=cb;cby → her biri bir sünger ve gereken renkleri.
        foreach (var sp in pr.S("sponge").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (at >= filled.Count) break;
            foreach (var ch in sp.Trim()) filled[at].SpongeColors.Add(ColorOf(ch));
            at++;
        }
        // M10 bağlı ataş: link=2x1,3x2 → 2 üyeli 1 vuruşluk ve 3 üyeli 2 vuruşluk gruplar.
        int linkId = 1;
        foreach (var lg in pr.S("link").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = lg.Split('x');
            int members = int.Parse(p[0], CultureInfo.InvariantCulture), hits = int.Parse(p[1], CultureInfo.InvariantCulture);
            for (int k = 0; k < members && at < filled.Count; k++) { filled[at].LinkId = linkId; filled[at++].LinkHits = hits; }
            linkId++;
        }
        foreach (var c in filled.Skip(at).Where(c => c.Layers.Count >= 2).Take(pr.I("rotor"))) { c.HasRotor = true; at++; }
        // M11 düğme ve geçit: gate=N → N dolu hücre geçide dönüşür (başta kapalı, katmanları saklanır); bir düğme.
        int gateCells = pr.I("gate");
        if (gateCells > 0)
        {
            var rest = filled.Skip(at).Where(c => !Covered(c) && !c.HasBottomStar && !c.HasRotor).ToList();
            // Geçit: tahtanın bir kenarından bitişik hücreler (görünür, tutarlı bir "kapı").
            var gates = rest.OrderBy(c => c.Y).ThenBy(c => Math.Abs(c.X - w / 2)).Take(gateCells).ToList();
            foreach (var g in gates)
            {
                g.GateId = 1;
                g.GateLayers.AddRange(g.Layers);
                g.Layers.Clear();
                g.Active = false;
            }
            var button = rest.Except(gates).OrderBy(c => c.Layers.Count).ThenBy(_ => rng.Next()).FirstOrDefault();
            if (button != null) button.ButtonGate = 1;
        }
        d.Paint = pr.I("paint");
        Normalize5(d);
        return d;
    }

    static IEnumerable<int> Neighbours5(int i, int w, int h)
    {
        int x = i % w, y = i / w;
        if (x > 0) yield return i - 1;
        if (x < w - 1) yield return i + 1;
        if (y > 0) yield return i - w;
        if (y < h - 1) yield return i + w;
    }

    static void Normalize5(LevelData d)
    {
        d.SchemaVersion = 2;
        if (!d.Capabilities.Contains("specials")) d.Capabilities.Add("specials");
        var caps = CapabilitiesOf(d);
        d.Capabilities.Clear();
        d.Capabilities.AddRange(caps);
    }

    static void SetGoals5(LevelData d, Profile pr, int moveLimit)
    {
        SetGoalsAndRoll(d, pr, moveLimit);
        d.RollEnabled = false;
        d.StartingRoll = false;
        Normalize5(d);
    }

    static Eval Evaluate5(Profile pr, LevelData d)
    {
        var ev = new Eval();
        SetGoals5(d, pr, 40);
        if (d.Goals.Count == 0) { ev.Info = "hedef yok"; return ev; }
        var errors = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
        if (errors.Count > 0) { ev.Score = -1000; ev.Info = "statik: " + errors[0]; return ev; }

        var probe = LevelLoader.ToState(d);
        var sol = BeamSolve(probe, pr.I("beam", 60), pr.R("len", 6, 12).Item2 + 6);
        if (sol == null) { ev.Score = -500; ev.Info = "ışın çözüm bulamadı"; return ev; }
        int len = sol.Count;
        int limit = len + pr.I("slack", 2);
        d.MoveLimit = limit;
        var st = LevelLoader.ToState(d);

        // Çözüm istatistikleri (gerçek motorla tekrar).
        var replay = st.Clone();
        var covered = new HashSet<(int, int)>(d.Cells.Where(Covered).Select(c => (c.X, c.Y)));
        int chains = 0, specials = 0, fired = 0, coverUse = 0, rotates = 0, gatesOpened = 0, paints = 0, spongeOpens = 0;
        foreach (var c in sol)
        {
            if (c.Type == CommandType.Rotate) rotates++;
            if (c.Type == CommandType.Paint) paints++;
            var r = RulesEngine.TryApply(replay, c);
            if (!r.Accepted) { ev.Score = -500; ev.Info = "tekrar reddi"; return ev; }
            if (r.SpecialCreated != SpecialKind.None) specials++;
            foreach (var w in r.Waves)
            {
                if (w.Kind == WaveKind.Chain) chains++;
                if (w.Fired != SpecialKind.None) fired++;
                coverUse += w.Cells.Count(p => covered.Remove((p.X, p.Y)));
                gatesOpened += w.GateOpens.Count;
                spongeOpens += w.SpongeOpens.Count;
            }
        }
        if (replay.Status != GameStatus.Won) { ev.Score = -500; ev.Info = "tekrar kazanmadı"; return ev; }

        // Oyuncu botu: hamle limitiyle kazanma oranı (zorluk ölçüsü).
        int games = pr.I("games", 24), wins = 0;
        var botRng = new Random(pr.Number * 31 + 7);
        for (int g = 0; g < games; g++) if (BotPlay(st.Clone(), botRng, pr.D("temp", 0.35))) wins++;
        double wr = (double)wins / games;

        var (lenLo, lenHi) = pr.R("len", 6, 12);
        var (botLo, botHi) = RangeD(pr.S("bot", "0.3-0.7"));
        double score = 0;
        score -= wr < botLo ? (botLo - wr) * 30 : wr > botHi ? (wr - botHi) * 30 : 0;
        score -= len < lenLo ? (lenLo - len) * 4 : len > lenHi ? (len - lenHi) * 4 : 0;
        score += specials >= pr.I("specials", 1) ? 3 + Math.Min(specials, 3) : -6 * (pr.I("specials", 1) - specials);
        score += Math.Min(chains, pr.I("chains", 1) + 3) * 0.8;
        if (chains < pr.I("chains", 1)) score -= 4;
        if (d.Cells.Any(Covered)) score += coverUse >= 1 ? 3 + Math.Min(coverUse, 3) : -12;
        // v5: özel taşlar tahtayı döndürmeden de çözebildiği için döner taban kapısı ret değil, güçlü teşvik:
        // döndürme içeren çözüm +8 (+ her ek döndürme), içermeyen −6. Tırmanma döndürmeli tahtaları seçer.
        if (d.Cells.Any(c => c.HasRotor)) score += rotates >= pr.I("rotorMin", 1) ? 8 + Math.Min(rotates, 3) : -6;

        // M11 kalite kapısı: geçit dekor kalmasın, çözüm düğmeye basıp geçidi açmalı.
        if (d.Cells.Any(c => c.GateId > 0)) score += gatesOpened > 0 ? 4 : -150;
        ev.Score = score;
        ev.Solution = sol;
        ev.MoveLimit = limit;
        ev.Info = $"çözüm {len}/{limit} bot {wins}/{games} özel {specials} ateş {fired} domino {chains} örtülü soyma {coverUse} döndürme {rotates} geçit {gatesOpened} sünger {spongeOpens} boya {paints}";
        return ev;
    }

    static (double, double) RangeD(string s)
    {
        var p = s.Split('-');
        double a = double.Parse(p[0], CultureInfo.InvariantCulture);
        return (a, p.Length > 1 ? double.Parse(p[1], CultureInfo.InvariantCulture) : a);
    }

    /// <summary>Durum değeri: hedef ilerlemesi baskın; özel taş ve puan küçük katkı.</summary>
    static double Heur(LevelState s)
    {
        if (s.Status == GameStatus.Won) return 1e6;
        double v = 0;
        foreach (var g in s.Goals) v += 100.0 * Math.Min(g.Progress, g.Amount) / Math.Max(1, g.Amount);
        for (int i = 0; i < s.Board.Count; i++) if (s.Board[i].HasSpecial) v += s.Board[i].Special == SpecialKind.Rainbow ? 12 : 6;
        // v6 tepsi: biriken özel taş da değerli (kullanılmadan beklemek ödüllenmez, kullanım hedef ilerlemesiyle ölçülür).
        foreach (var t in s.Tray) v += t == SpecialKind.Rainbow ? 8 : 4;
        return v + s.Score * 0.002;
    }

    /// <summary>Işın araması: derinlik başına en iyi B durum; ilk kazanan yol döner (yoksa null).</summary>
    static List<Command> BeamSolve(LevelState start, int width, int maxDepth)
    {
        var beam = new List<(LevelState s, List<Command> path)> { (start.Clone(), new List<Command>()) };
        for (int depth = 0; depth < maxDepth && beam.Count > 0; depth++)
        {
            var next = new List<(LevelState s, List<Command> path, double h)>();
            var seen = new HashSet<string>();
            foreach (var (s, path) in beam)
            {
                foreach (var cmd in LevelSolver.Moves(s))
                {
                    var n = s.Clone();
                    var r = RulesEngine.TryApply(n, cmd);
                    if (!r.Accepted) continue;
                    var p = new List<Command>(path) { cmd };
                    if (n.Status == GameStatus.Won) return p;
                    if (n.IsTerminal) continue;
                    if (!seen.Add(StateKey(n))) continue;
                    next.Add((n, p, Heur(n)));
                }
            }
            beam = next.OrderByDescending(t => t.h).Take(width).Select(t => (t.s, t.path)).ToList();
        }
        return null;
    }

    static string StateKey(LevelState s)
    {
        var sb = new StringBuilder();
        sb.Append(s.PaintLeft).Append(';').Append(s.MovesLeft).Append(';');
        foreach (var t in s.Tray) sb.Append((int)t);
        sb.Append(';');
        foreach (var g in s.Goals) sb.Append(g.Progress).Append(',');
        for (int i = 0; i < s.Board.Count; i++)
        {
            var c = s.Board[i];
            foreach (var l in c.Layers) sb.Append((char)('0' + (int)l));
            sb.Append(c.ClipHitsRemaining).Append(c.IceHitsRemaining).Append(c.LockRemaining).Append(c.HasEnvelope ? 'e' : '-')
              .Append((int)c.Special).Append(c.SpongeHave).Append(c.LinkHits).Append(c.Active ? 'a' : '#').Append('/');
        }
        return sb.ToString();
    }

    /// <summary>Yumuşak açgözlü oyuncu: hamleleri bir adım sonrası değerine göre sıralar, sıcaklıkla rastgele seçer.</summary>
    static bool BotPlay(LevelState s, Random rng, double temp)
    {
        while (!s.IsTerminal)
        {
            var moves = LevelSolver.Moves(s);
            if (moves.Count == 0) return false;
            var vals = new List<(LevelState n, double v)>();
            foreach (var m in moves)
            {
                var n = s.Clone();
                if (!RulesEngine.TryApply(n, m).Accepted) continue;
                if (n.Status == GameStatus.LostProtected) continue; // v8: kristali hemen kıran hamle seçilmez (oyuncu kristali görür)
                vals.Add((n, Heur(n)));
            }
            if (vals.Count == 0) return false;
            double max = vals.Max(t => t.v);
            var weights = vals.Select(t => Math.Exp((t.v - max) / Math.Max(1e-3, temp * 20))).ToList();
            double sum = weights.Sum(), roll = rng.NextDouble() * sum;
            int pick = 0;
            for (; pick < weights.Count - 1; pick++) { roll -= weights[pick]; if (roll <= 0) break; }
            s = vals[pick].n;
        }
        return s.Status == GameStatus.Won;
    }

    /// <summary>v5 §5: kayıtlı çözüm + bitiş kutlaması puanı = 3 yıldız; %75 = 2 yıldız. Çözüm kazanmazsa null.</summary>
    static int[] StarThresholds(LevelData d)
    {
        if (d.Solution.Count == 0) return null;
        var s = LevelLoader.ToState(d);
        foreach (var step in d.Solution)
        {
            var r = RulesEngine.TryApply(s, LevelLoader.ToCommand(step));
            if (!r.Accepted) return null;
        }
        if (s.Status != GameStatus.Won) return null;
        RulesEngine.Celebrate(s, CelebrationSeed(d.Id));
        int e3 = Math.Max(10, s.Score);
        return new[] { (int)Math.Round(e3 * 0.75), e3 };
    }

    /// <summary>Oyunla aynı tohum: bölüm numarası (level_###).</summary>
    public static int CelebrationSeed(string id)
    {
        var digits = new string((id ?? "").Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : 1;
    }
}
