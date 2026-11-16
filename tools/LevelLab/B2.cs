using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Levels;
using KatKat.Meta;

// b2check (v14 Faz B2, docs/v14/PLAN.md "Diğer kataloglar"): ana katalog dışındaki kataloglar (Trial, Pilot, Concept, Daily)
// zincirsiz kurala göre denetlenir — EditMode testlerinin istediği şartlar dosya başına tek satırda. Hiçbir dosyaya yazmaz.
//   dotnet tools/LevelLab/bin/Release/net9.0/LevelLab.dll b2check [trial|pilot|concept|daily]
static partial class Program
{
    static int B2Check(string[] args)
    {
        string only = args.Length > 1 ? args[1] : null;
        var res = Path.Combine(Root, "Assets", "KatKat", "Resources");
        int bad = 0;
        string Replay(LevelData d, out LevelState s, out int taps)
        {
            s = LevelLoader.ToState(d);
            taps = 0;
            foreach (var step in d.Solution)
            {
                var cmd = LevelLoader.ToCommand(step);
                if (cmd.Type != CommandType.UseSpecial) taps++;
                if (!RulesEngine.TryApply(s, cmd).Accepted) return "adım reddedildi: " + step.Type;
            }
            return s.Status == GameStatus.Won ? null : "kazanmadı (" + s.Status + ")";
        }
        void Line(string file, string err, string info)
        {
            if (err != null) bad++;
            Console.WriteLine($"{(err == null ? "TAMAM" : "HATA ")} {file}: {info}{(err != null ? " — " + err : "")}");
        }
        foreach (var cat in new[] { "Trial", "Pilot", "Concept" })
        {
            if (only != null && !string.Equals(only, cat, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var f in Directory.GetFiles(Path.Combine(res, cat), "*.json").OrderBy(x => x, StringComparer.Ordinal))
            {
                var d = LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f));
                var errs = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
                string err = errs.Count > 0 ? errs[0] : Replay(d, out var s, out var taps);
                if (err == null && d.Rules.Contains("exact") && cat == "Trial")
                {
                    Replay(d, out _, out var t);
                    if (t != d.MoveLimit) err = $"kesin hamle: sınır {d.MoveLimit} ≠ çözüm {t}";
                }
                if (err == null && cat == "Concept" && d.Hazard == null) err = "tehdit yok";
                if (err == null && cat == "Trial" && d.TrialType == "protect")
                {
                    // PlayMode TrialFlowTests.Crystal_BreakLoses: kristali kıran bir ilk hamle olmalı.
                    var s0 = LevelLoader.ToState(d);
                    if (!LevelSolver.Moves(s0).Any(c => { var p = s0.Clone(); return RulesEngine.TryApply(p, c).Accepted && p.Status == GameStatus.LostProtected; }))
                        err = "kristali kıran ilk hamle yok";
                }
                Line(Path.GetFileName(f), err, $"{d.TrialType} [{string.Join("+", d.Rules)}] {d.Width}x{d.Height} hamle {d.MoveLimit} çözüm {d.Solution.Count}{(d.Hazard != null ? " tehdit " + d.Hazard.Kind : "")}");
            }
        }
        if (only == null || only == "daily")
        {
            var dir = Path.Combine(res, "Daily");
            var idx = DailyIndex.Parse(File.ReadAllText(Path.Combine(dir, "daily_index.json")));
            int n = 0;
            var kinds = new Dictionary<string, int>();
            foreach (var day in idx.Days)
                foreach (var kv in day)
                {
                    n++;
                    var path = Path.Combine(dir, kv.Value + ".json");
                    if (!File.Exists(path)) { Line(kv.Value, "dosya yok", ""); continue; }
                    var e = ValidationReport.Check(Path.GetFileName(path), File.ReadAllText(path));
                    var d = LevelParser.Parse(File.ReadAllText(path), kv.Value + ".json");
                    string err = e.StaticErrors.Count > 0 ? e.StaticErrors[0] : !e.Replay.Success ? e.Replay.Error
                        : !d.RollEnabled ? "rulo kapalı" : !StonePowers.CanApply(StonePowers.DailyRoll, LevelLoader.ToState(d)) ? "güç başta uygulanamaz" : null;
                    string k = d.TrialType ?? "normal";
                    kinds[k] = kinds.TryGetValue(k, out var c) ? c + 1 : 1;
                    if (err != null || only == "daily") Line(kv.Value, err, $"{k} [{string.Join("+", d.Rules)}] {d.Width}x{d.Height} hamle {d.MoveLimit}");
                }
            Console.WriteLine($"günlük: {idx.Days.Count} gün, {n} bulmaca; türler: {string.Join(", ", kinds.OrderByDescending(x => x.Value).Select(x => $"{x.Key} {x.Value}"))}");
        }
        Console.WriteLine(bad == 0 ? "b2check: hepsi geçti" : $"b2check: {bad} HATA");
        return bad == 0 ? 0 : 1;
    }
}
