using System;
using System.Collections.Generic;
using System.Globalization;

namespace KatKat.Levels
{
    /// <summary>JSON metninden LevelData üretir. Yapı hatalarını kimlik ve konumla bildirir.</summary>
    public static class LevelParser
    {
        public static LevelData Parse(string json, string sourceName = "?")
        {
            object root;
            try { root = MiniJson.Parse(json); }
            catch (FormatException e) { throw new FormatException($"{sourceName}: {e.Message}", e); }
            if (!(root is Dictionary<string, object> o)) throw new FormatException($"{sourceName}: kök nesne olmalı");

            var d = new LevelData
            {
                SchemaVersion = Int(o, "schemaVersion", sourceName),
                Id = Str(o, "id", sourceName),
                Title = OptStr(o, "title"),
                Tutorial = OptStr(o, "tutorial"),
                Width = Int(o, "width", sourceName),
                Height = Int(o, "height", sourceName),
                MoveLimit = Int(o, "moveLimit", sourceName),
                RollEnabled = OptBool(o, "rollEnabled"),
                StartingRoll = OptBool(o, "startingRoll"),
            };
            string where = d.Id ?? sourceName;
            if (o.TryGetValue("capabilities", out var caps) && caps != null)
            {
                if (!(caps is List<object> cl)) throw new FormatException($"{where}: capabilities dizi olmalı");
                foreach (var cap in cl)
                {
                    if (!(cap is string cs)) throw new FormatException($"{where}: capabilities elemanı metin olmalı");
                    d.Capabilities.Add(cs);
                }
            }
            d.TrialType = OptStr(o, "trialType");
            if (o.TryGetValue("rules", out var rules) && rules != null)
            {
                if (!(rules is List<object> rl)) throw new FormatException($"{where}: rules dizi olmalı");
                foreach (var r in rl) d.Rules.Add(r as string ?? throw new FormatException($"{where}: rules elemanı metin olmalı"));
            }
            // v5 §5: 2 ve 3 yıldız puan eşikleri.
            if (o.TryGetValue("stars", out var st) && st != null)
            {
                if (!(st is List<object> sl) || sl.Count != 2) throw new FormatException($"{where}: stars iki sayılık dizi olmalı");
                d.StarThresholds = new int[2];
                for (int i = 0; i < 2; i++)
                {
                    if (sl[i] is double dv) d.StarThresholds[i] = (int)dv;
                    else if (sl[i] is long lv) d.StarThresholds[i] = (int)lv;
                    else if (sl[i] is int iv) d.StarThresholds[i] = iv;
                    else throw new FormatException($"{where}: stars elemanı sayı olmalı");
                }
            }

            foreach (var g in List(o, "goals", where))
            {
                var go = AsObj(g, where, "goals");
                d.Goals.Add(new GoalData { Type = Str(go, "type", where), Color = OptStr(go, "color") ?? "", Kind = OptStr(go, "kind") ?? "", Amount = Int(go, "amount", where), Stage = go.ContainsKey("stage") ? Int(go, "stage", where) : 0 });
            }
            foreach (var c in List(o, "cells", where))
            {
                var co = AsObj(c, where, "cells");
                var cell = new CellData
                {
                    X = Int(co, "x", where),
                    Y = Int(co, "y", where),
                    Active = OptBool(co, "active", true),
                    ClipHitsRemaining = co.ContainsKey("clipHitsRemaining") ? Int(co, "clipHitsRemaining", where) : 0,
                    HasBottomStar = OptBool(co, "hasBottomStar"),
                    Protected = OptBool(co, "protected"),
                };
                if (co.TryGetValue("overlay", out var ov) && ov != null)
                {
                    // schemaVersion 2 örtüsü: hücre başına tek fiziksel örtü.
                    var oo = AsObj(ov, where, "overlay");
                    string kind = Str(oo, "kind", where);
                    switch (kind)
                    {
                        case "none": break;
                        case "clip": cell.ClipHitsRemaining = Int(oo, "hits", where); break;
                        case "ice": cell.IceHits = Int(oo, "hits", where); break;
                        case "envelope": cell.EnvelopeColor = Str(oo, "sealColor", where); break;
                        case "colorLock":
                            cell.LockColor = Str(oo, "color", where);
                            cell.LockRemaining = Int(oo, "remaining", where);
                            break;
                        case "sponge":
                            foreach (var sc in List(oo, "colors", where)) cell.SpongeColors.Add(sc as string ?? throw new FormatException($"{where}: sünger rengi metin olmalı"));
                            break;
                        case "linkedClip":
                            cell.LinkId = Int(oo, "linkId", where);
                            cell.LinkHits = Int(oo, "hits", where);
                            break;
                        default: throw new FormatException($"{where}: ({cell.X},{cell.Y}) bilinmeyen örtü '{kind}'");
                    }
                }
                if (co.TryGetValue("base", out var bv) && bv != null)
                {
                    // schemaVersion 2 taban etkisi: hücre başına en çok bir.
                    var bo = AsObj(bv, where, "base");
                    string kind = Str(bo, "kind", where);
                    switch (kind)
                    {
                        case "none": break;
                        case "star": cell.HasBottomStar = true; break;
                        case "rotor": cell.HasRotor = true; break;
                        case "button": cell.ButtonGate = Int(bo, "gate", where); break;
                        default: throw new FormatException($"{where}: ({cell.X},{cell.Y}) bilinmeyen taban '{kind}'");
                    }
                }
                if (co.TryGetValue("layers", out var layers) && layers != null)
                {
                    if (!(layers is List<object> ll)) throw new FormatException($"{where}: ({cell.X},{cell.Y}) layers dizi olmalı");
                    foreach (var l in ll)
                    {
                        if (!(l is string ls)) throw new FormatException($"{where}: ({cell.X},{cell.Y}) katman metin olmalı");
                        cell.Layers.Add(ls);
                    }
                }
                // M11 geçit hücresi (başta etkin değil): gate + gateLayers.
                if (co.ContainsKey("gate"))
                {
                    cell.GateId = Int(co, "gate", where);
                    foreach (var gl in List(co, "gateLayers", where)) cell.GateLayers.Add(gl as string ?? throw new FormatException($"{where}: geçit katmanı metin olmalı"));
                }
                if (co.ContainsKey("area")) cell.Area = Int(co, "area", where); // vr1 P3
                d.Cells.Add(cell);
            }
            if (o.ContainsKey("paint")) d.Paint = Int(o, "paint", where);
            if (o.TryGetValue("seal", out var sv) && sv != null) // vr1 P3 zincir mührü
            {
                var so2 = AsObj(sv, where, "seal");
                d.SealWave = Int(so2, "wave", where);
                d.SealGate = Int(so2, "gate", where);
            }
            if (o.ContainsKey("solution"))
            {
                foreach (var s in List(o, "solution", where))
                {
                    var so = AsObj(s, where, "solution");
                    var step = new StepData { Type = Str(so, "type", where) };
                    if (step.Type == "tap" || step.Type == "rotate") { step.X = Int(so, "x", where); step.Y = Int(so, "y", where); }
                    else if (step.Type == "roll") { step.Row = Int(so, "row", where); }
                    else if (step.Type == "use") { step.Row = Int(so, "slot", where); step.X = Int(so, "x", where); step.Y = Int(so, "y", where); }
                    else if (step.Type == "paint") { step.X = Int(so, "x", where); step.Y = Int(so, "y", where); step.Color = Str(so, "color", where); }
                    d.Solution.Add(step);
                }
            }
            return d;
        }

        private static Dictionary<string, object> AsObj(object v, string where, string field)
        {
            if (v is Dictionary<string, object> d) return d;
            throw new FormatException($"{where}: '{field}' elemanı nesne olmalı");
        }

        private static List<object> List(Dictionary<string, object> o, string key, string where)
        {
            if (o.TryGetValue(key, out var v) && v is List<object> l) return l;
            throw new FormatException($"{where}: '{key}' dizisi eksik");
        }

        private static int Int(Dictionary<string, object> o, string key, string where)
        {
            if (!o.TryGetValue(key, out var v) || !(v is double dv)) throw new FormatException($"{where}: '{key}' sayısı eksik");
            if (Math.Abs(dv - Math.Round(dv)) > 1e-9) throw new FormatException($"{where}: '{key}' tam sayı olmalı ({dv.ToString(CultureInfo.InvariantCulture)})");
            return (int)Math.Round(dv);
        }

        private static string Str(Dictionary<string, object> o, string key, string where)
        {
            if (o.TryGetValue(key, out var v) && v is string s) return s;
            throw new FormatException($"{where}: '{key}' metni eksik");
        }

        private static string OptStr(Dictionary<string, object> o, string key) => o.TryGetValue(key, out var v) ? v as string : null;

        private static bool OptBool(Dictionary<string, object> o, string key, bool fallback = false)
        {
            return o.TryGetValue(key, out var v) && v is bool b ? b : fallback;
        }
    }
}
