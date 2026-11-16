using System;
using System.Collections.Generic;
using System.Globalization;
using KatKat.Core;

namespace KatKat.Levels
{
    /// <summary>
    /// Konsept pilotu tehdit verisi (bölüm JSON'unda "hazard" nesnesi; ana katalogda yok). Biçim:
    /// <code>
    /// { "kind": "rot", "every": 3 }                                  Çürüyen Zemin
    /// { "kind": "fuse", "fuses": [[x, y, n], ...] }                  Fitil
    /// { "kind": "cycle", "every": 3, "colors": ["coral", "blue"] }   Dönen Hedef
    /// { "kind": "raven", "path": [[x, y], ...] }                     Kuzgun (ilk hücre başlangıç, son hücre yuva)
    /// { "kind": "rain", "every": 2, "max": 5, "volleys": [[[x, y, "coral"], ...], ...] }  Yağmur
    /// { "kind": "rival", "max": 9 }                                  Gölge Rakip
    /// { "kind": "moss", "every": 2, "max": 9, "cells": [[x, y], ...] } Yayılan Yosun
    /// { "kind": "balloon", "every": 1, "cells": [[x, y], ...] }      Balonlar (hedef: collectToken kind balloon)
    /// { "kind": "orders", "orders": [4, 4, 3] }                      Sipariş Kuyruğu (aşamalı hedefler)
    /// { "kind": "fire", "max": 3 }                                   Kombo Ateşi
    /// </code>
    /// </summary>
    public sealed class HazardData
    {
        public string Kind;
        public int Every = 1, Max;
        public readonly List<string> Colors = new List<string>();
        public readonly List<int[]> Cells = new List<int[]>();
        public readonly List<List<(int X, int Y, string Color)>> Volleys = new List<List<(int, int, string)>>();
        public readonly List<int> Orders = new List<int>();

        public static readonly string[] Kinds = { "rot", "fuse", "cycle", "raven", "rain", "rival", "moss", "balloon", "orders", "fire" };

        public static HazardData Parse(object v, string where)
        {
            if (!(v is Dictionary<string, object> o)) throw new FormatException($"{where}: hazard nesne olmalı");
            var h = new HazardData { Kind = o.TryGetValue("kind", out var k) ? k as string : null };
            if (Array.IndexOf(Kinds, h.Kind) < 0) throw new FormatException($"{where}: bilinmeyen hazard '{h.Kind}'");
            if (o.TryGetValue("every", out var e)) h.Every = Num(e, where);
            if (o.TryGetValue("max", out var m)) h.Max = Num(m, where);
            if (o.TryGetValue("colors", out var cs) && cs is List<object> cl) foreach (var c in cl) h.Colors.Add(c as string);
            foreach (var key in new[] { "cells", "path", "fuses" })
                if (o.TryGetValue(key, out var cv) && cv is List<object> list)
                    foreach (var item in list)
                    {
                        if (!(item is List<object> p) || p.Count < 2) throw new FormatException($"{where}: hazard.{key} [x, y] olmalı");
                        var a = new int[p.Count];
                        for (int i = 0; i < p.Count; i++) a[i] = Num(p[i], where);
                        h.Cells.Add(a);
                    }
            if (o.TryGetValue("volleys", out var vv) && vv is List<object> vl)
                foreach (var vol in vl)
                {
                    var drops = new List<(int, int, string)>();
                    foreach (var d in (List<object>)vol)
                    {
                        var p = (List<object>)d;
                        drops.Add((Num(p[0], where), Num(p[1], where), p[2] as string));
                    }
                    h.Volleys.Add(drops);
                }
            if (o.TryGetValue("orders", out var ov) && ov is List<object> ol) foreach (var n in ol) h.Orders.Add(Num(n, where));
            if (h.Every < 1) throw new FormatException($"{where}: hazard.every en az 1");
            return h;
        }

        private static int Num(object v, string where)
        {
            if (v is double d) return (int)Math.Round(d);
            throw new FormatException($"{where}: hazard sayısı bekleniyordu ({Convert.ToString(v, CultureInfo.InvariantCulture)})");
        }

        /// <summary>Başlangıç durumu (tahta kurulduktan sonra; yosun hücreye yazılır).</summary>
        public HazardState ToState(BoardState b, string where)
        {
            var s = new HazardState { Every = Every, Counter = Every, Max = Max };
            switch (Kind)
            {
                case "rot":
                    s.Kind = HazardKind.Rot;
                    s.Row = HazardRules.NextRotRow(b, b.Height - 1);
                    s.Cleared = s.Row >= 0 ? HazardRules.EmptyInRow(b, s.Row) : 0;
                    break;
                case "fuse":
                    s.Kind = HazardKind.Fuse;
                    s.Fuse = new int[b.Count];
                    foreach (var c in Cells) s.Fuse[b.Index(c[0], c[1])] = c.Length > 2 ? c[2] : 3;
                    break;
                case "cycle":
                    s.Kind = HazardKind.Cycle;
                    s.Colors = new LayerColor[Colors.Count];
                    for (int i = 0; i < Colors.Count; i++)
                        if (!ColorIds.TryParse(Colors[i], out s.Colors[i])) throw new FormatException($"{where}: hazard rengi '{Colors[i]}'");
                    if (s.Colors.Length < 2) throw new FormatException($"{where}: cycle en az iki renk ister");
                    break;
                case "raven":
                    s.Kind = HazardKind.Raven;
                    s.Path = new int[Cells.Count];
                    for (int i = 0; i < Cells.Count; i++) s.Path[i] = b.Index(Cells[i][0], Cells[i][1]);
                    if (s.Path.Length < 2) throw new FormatException($"{where}: kuzgun yolu en az iki hücre");
                    break;
                case "rain":
                    s.Kind = HazardKind.Rain;
                    s.Volleys = new int[Volleys.Count][];
                    for (int v = 0; v < Volleys.Count; v++)
                    {
                        s.Volleys[v] = new int[Volleys[v].Count];
                        for (int i = 0; i < Volleys[v].Count; i++)
                        {
                            var (x, y, col) = Volleys[v][i];
                            if (!ColorIds.TryParse(col, out var lc)) throw new FormatException($"{where}: yağmur rengi '{col}'");
                            s.Volleys[v][i] = b.Index(x, y) * 8 + (int)lc;
                        }
                    }
                    if (s.Volleys.Length == 0 || s.Max <= 0) throw new FormatException($"{where}: yağmur sağanak ve max ister");
                    break;
                case "rival": s.Kind = HazardKind.Rival; if (s.Max <= 0) throw new FormatException($"{where}: rival max ister"); break;
                case "moss":
                    s.Kind = HazardKind.Moss;
                    foreach (var c in Cells) b[c[0], c[1]].Moss = true;
                    if (s.Max <= 0) throw new FormatException($"{where}: moss max ister");
                    break;
                case "balloon":
                    s.Kind = HazardKind.Balloon;
                    s.Balloon = new bool[b.Count];
                    foreach (var c in Cells) s.Balloon[b.Index(c[0], c[1])] = true;
                    break;
                case "orders":
                    s.Kind = HazardKind.Orders;
                    s.Orders = Orders.ToArray();
                    if (s.Orders.Length == 0) throw new FormatException($"{where}: orders süre listesi ister");
                    s.Value = s.Orders[0];
                    break;
                case "fire":
                    s.Kind = HazardKind.Fire;
                    if (s.Max <= 0) throw new FormatException($"{where}: fire max ister");
                    s.Value = s.Max;
                    break;
            }
            return s;
        }
    }
}
