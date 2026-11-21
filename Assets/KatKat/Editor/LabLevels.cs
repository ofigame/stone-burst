using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KatKat.Core;
using KatKat.Levels;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// 10 yeni özellik test bölümü (kullanıcı, 2026-11-21: "10 tane test bölümü oluştur tüm özelliklerin olduğu"): çakıl,
    /// Kürek (4'lü grup), çakıl yayılması, patlatılması yasak bomba yığınları, sürükle-değiştir, özel taşlar; bazılarında ataş,
    /// buz, dip yıldızı ve şekilli tahta. Çıktı Resources/Lab/lab_NN.json; Test Bölümleri listesinin ilk 10'u.
    /// <para>Her bölüm: önce doğrulayıcı; sonra kural motoruyla açgözlü oyuncu (Kürek → en çok taş toplayan güvenli dokunuş →
    /// güvenli sürükle-değiştir) hamle sınırı kadar oynar; hedefler onun topladığının altında, yıldız eşikleri puanından.
    /// Son olarak aynı oyuncu gerçek hedeflerle bölümü kazanmalı (kazanamazsa hedefler küçültülür).</para>
    /// tools/unity.ps1 lab-levels
    /// </summary>
    public static class LabLevels
    {
        private const string OutDir = "Assets/KatKat/Resources/Lab";
        private static readonly string[] Palette = { "coral", "blue", "yellow", "purple", "green" };

        private sealed class Spec
        {
            public string Title;
            public int W = 7, H = 8, Colors = 4, MinL = 1, MaxL = 3, Moves = 20, Seed;
            public Func<int, int, bool> Gravel = (x, y) => false;
            public Func<int, int, bool> Hole = (x, y) => false;
            public int Protect, Clips, Ice, Stars, GoalColors = 2;
            public float GoalShare = 0.7f;
        }

        private static List<Spec> Specs() => new List<Spec>
        {
            new Spec { Title = "Çakıl ve Kürek", Seed = 101, Gravel = (x, y) => (x * 2 + y * 3) % 5 == 0 },
            new Spec { Title = "Yayılan Çakıl", Seed = 202, Gravel = (x, y) => y <= 1 && x >= 2 && x <= 4, Moves = 22 },
            new Spec { Title = "Bombaya Dokunma", Seed = 303, Protect = 3, Gravel = (x, y) => (x + y * 2) % 6 == 0 && y < 5 },
            new Spec { Title = "Sürükle ve Tamamla", Seed = 404, Colors = 4, MinL = 1, MaxL = 2, Gravel = (x, y) => (x == 0 || x == 6) && y % 3 == 1 },
            new Spec { Title = "Çakıl ve Ataş", Seed = 505, Clips = 5, Gravel = (x, y) => (x * 3 + y) % 7 == 2 },
            new Spec { Title = "Çakıl ve Buz", Seed = 606, Ice = 5, Gravel = (x, y) => (x + y) % 5 == 0 && y > 0 },
            new Spec { Title = "Yıldız Avı", Seed = 707, Stars = 3, Gravel = (x, y) => y == 0 && x % 2 == 0 || y == 7 && x % 3 == 1 },
            new Spec { Title = "Haç Tahta", Seed = 808, Hole = (x, y) => (x <= 1 || x >= 5) && (y <= 1 || y >= 6), Gravel = (x, y) => (x * 2 + y) % 5 == 1 },
            new Spec { Title = "Hepsi Bir Arada", Seed = 909, Protect = 2, Clips = 3, Ice = 3, Stars = 2, Gravel = (x, y) => (x * 2 + y * 3) % 6 == 0, Moves = 22 },
            new Spec { Title = "Patron", Seed = 1010, W = 8, H = 9, Colors = 4, Protect = 3, Clips = 3, Ice = 3, Stars = 2, GoalColors = 3, GoalShare = 0.8f,
                Hole = (x, y) => (x == 0 || x == 7) && (y == 0 || y == 8), Gravel = (x, y) => (x + y * 2) % 5 == 0, Moves = 24 },
        };

        [MenuItem("KatKat/Test bölümlerini üret (Lab)")]
        public static void Build()
        {
            Directory.CreateDirectory(OutDir);
            var report = new StringBuilder("# Test bölümleri (Lab)\n\n| # | Başlık | Tahta | Hamle | Hedef | Açgözlü oyuncu |\n|---|---|---|---|---|---|\n");
            var specs = Specs();
            for (int n = 0; n < specs.Count; n++)
            {
                var sp = specs[n];
                string id = $"lab_{n + 1:00}";
                LevelData d = null;
                string json = null;
                Dictionary<string, int> got = null;
                int score = 0;
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    var rng = new System.Random(sp.Seed * 131 + attempt);
                    json = Compose(sp, id, rng, null, 0, 0);
                    d = LevelParser.Parse(json, id);
                    var errs = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
                    if (errs.Count > 0) { Debug.LogWarning($"[Lab] {id} deneme {attempt}: {string.Join("; ", errs)}"); continue; }
                    var probe = LevelLoader.ToState(d);
                    foreach (var g in probe.Goals) g.Amount = 99999;
                    got = Greedy(probe, sp.Moves, out score, out _);
                    int total = 0; foreach (var v in got.Values) total += v;
                    if (total >= sp.Moves * 3) { json = Compose(sp, id, new System.Random(sp.Seed * 131 + attempt), got, score, 0); break; }
                    got = null;
                }
                if (got == null) throw new Exception($"[Lab] {id}: uygun tahta bulunamadı");
                // Gerçek hedeflerle kazanılabilirlik; kazanamazsa hedefler %10 küçülür.
                string verdict = "?";
                for (int shrink = 0; shrink < 6; shrink++)
                {
                    d = LevelParser.Parse(json, id);
                    var errs = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
                    if (errs.Count > 0) throw new Exception($"[Lab] {id}: {string.Join("; ", errs)}");
                    Greedy(LevelLoader.ToState(d), sp.Moves, out _, out var status);
                    verdict = status.ToString();
                    if (status == GameStatus.Won) break;
                    json = Compose(sp, id, null, got, score, shrink + 1, json);
                }
                if (verdict != "Won") throw new Exception($"[Lab] {id}: açgözlü oyuncu kazanamadı ({verdict})");
                File.WriteAllText(Path.Combine(OutDir, id + ".json"), json, new UTF8Encoding(false));
                var goals = new StringBuilder();
                foreach (var g in d.Goals) goals.Append(g.Color).Append(' ').Append(g.Amount).Append(", ");
                report.Append($"| {n + 1} | {sp.Title} | {sp.W}x{sp.H} | {sp.Moves} | {goals.ToString().TrimEnd(',', ' ')} | {verdict} |\n");
                Debug.Log($"[Lab] {id} {sp.Title}: {verdict}");
            }
            File.WriteAllText("Logs/lab_levels.md", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[Lab] 10 test bölümü yazıldı");
        }

        /// <summary>Bölüm JSON'u. <paramref name="got"/> null ise yer tutucu hedef (oyun bitmez); değilse en çok toplanan renklerden
        /// hedefler. <paramref name="keep"/> verilirse hücreler ondan alınır (yalnız hedef küçültme).</summary>
        private static string Compose(Spec sp, string id, System.Random rng, Dictionary<string, int> got, int score, int shrink, string keep = null)
        {
            string cells;
            if (keep != null)
            {
                int a = keep.IndexOf("\"cells\"", StringComparison.Ordinal);
                cells = keep.Substring(a);
            }
            else cells = Cells(sp, rng);
            var caps = new List<string> { "specials", "gravel" };
            if (sp.Clips > 0) caps.Add("clip");
            if (sp.Ice > 0) caps.Add("ice");
            if (sp.Stars > 0) caps.Add("star");
            var sb = new StringBuilder();
            sb.Append("{\n  \"schemaVersion\": 2,\n  \"id\": \"").Append(id).Append("\",\n");
            sb.Append("  \"capabilities\": [").Append(string.Join(", ", caps.ConvertAll(c => "\"" + c + "\""))).Append("],\n");
            sb.Append("  \"title\": \"").Append(sp.Title).Append("\",\n  \"trialType\": \"lab\",\n");
            sb.Append("  \"rules\": [").Append(sp.Protect > 0 ? "\"protect\"" : "").Append("],\n");
            sb.Append($"  \"width\": {sp.W},\n  \"height\": {sp.H},\n  \"moveLimit\": {sp.Moves},\n  \"rollEnabled\": false,\n  \"startingRoll\": false,\n");
            int s1 = Math.Max(100, (int)(score * 0.55f) / 10 * 10), s2 = Math.Max(s1 + 50, (int)(score * 0.8f) / 10 * 10);
            sb.Append($"  \"stars\": [{s1}, {s2}],\n  \"goals\": [\n");
            if (got == null)
            {
                // Yer tutucu: tahtadaki ilk renkten 1 (doğrulayıcı geçsin); keşif oyununda miktar büyütülür, bölüm bitmez.
                const string Mark = "\"layers\": [\"";
                int q = cells.IndexOf(Mark, StringComparison.Ordinal) + Mark.Length;
                string first = cells.Substring(q, cells.IndexOf('"', q) - q);
                sb.Append("    { \"type\": \"collectColor\", \"color\": \"").Append(first).Append("\", \"amount\": 1 }\n");
            }
            else
            {
                var top = new List<KeyValuePair<string, int>>(got);
                top.Sort((x, y) => y.Value.CompareTo(x.Value));
                int k = Math.Min(sp.GoalColors, top.Count);
                float share = sp.GoalShare * (float)Math.Pow(0.9, shrink);
                for (int i = 0; i < k; i++)
                {
                    int amt = Math.Max(3, (int)(top[i].Value * share));
                    sb.Append("    { \"type\": \"collectColor\", \"color\": \"").Append(top[i].Key).Append("\", \"amount\": ").Append(amt).Append(" }").Append(i < k - 1 ? ",\n" : "\n");
                }
            }
            sb.Append("  ],\n  ").Append(cells);
            return sb.ToString();
        }

        private static string Cells(Spec sp, System.Random rng)
        {
            var palette = new List<string>(Palette);
            // Renk seti tohuma göre döner (bölümler farklı renklerle).
            int rot = rng.Next(palette.Count);
            var colors = new List<string>();
            for (int i = 0; i < sp.Colors; i++) colors.Add(palette[(rot + i) % palette.Count]);
            var top = new string[sp.W, sp.H];
            var layers = new List<string>[sp.W, sp.H];
            for (int y = 0; y < sp.H; y++)
            for (int x = 0; x < sp.W; x++)
            {
                if (sp.Hole(x, y)) continue;
                int n = rng.Next(sp.MinL, sp.MaxL + 1);
                var l = new List<string>();
                for (int k = 0; k < n; k++) l.Add(colors[rng.Next(colors.Count)]);
                // Üst renk komşusuna benzesin (gruplar oluşsun; sürükle-değiştir bölümünde daha az).
                double copy = sp.MaxL <= 2 ? 0.25 : 0.45;
                if (rng.NextDouble() < copy)
                {
                    string near = x > 0 && top[x - 1, y] != null && (y == 0 || top[x, y - 1] == null || rng.Next(2) == 0) ? top[x - 1, y] : y > 0 ? top[x, y - 1] : null;
                    if (near != null) l[l.Count - 1] = near;
                }
                layers[x, y] = l;
                top[x, y] = l[l.Count - 1];
            }
            // Özel hücre seçimi: çakıl, bomba (korunan), ataş, buz, yıldız aynı hücreye düşmez.
            var used = new HashSet<int>();
            var gravel = new HashSet<int>();
            for (int y = 0; y < sp.H; y++)
            for (int x = 0; x < sp.W; x++)
                if (layers[x, y] != null && sp.Gravel(x, y)) { gravel.Add(y * sp.W + x); used.Add(y * sp.W + x); }
            HashSet<int> Pick(int count, Func<int, int, bool> ok)
            {
                var set = new HashSet<int>();
                for (int guard = 0; set.Count < count && guard < 500; guard++)
                {
                    int x = rng.Next(sp.W), y = rng.Next(sp.H), i = y * sp.W + x;
                    if (layers[x, y] == null || used.Contains(i) || !ok(x, y)) continue;
                    set.Add(i); used.Add(i);
                }
                return set;
            }
            var protect = Pick(sp.Protect, (x, y) => y >= 2);
            var clips = Pick(sp.Clips, (x, y) => true);
            var ice = Pick(sp.Ice, (x, y) => true);
            var stars = Pick(sp.Stars, (x, y) => layers[x, y].Count >= 2);
            var sb = new StringBuilder("\"cells\": [\n");
            bool first = true;
            for (int y = 0; y < sp.H; y++)
            for (int x = 0; x < sp.W; x++)
            {
                if (!first) sb.Append(",\n");
                first = false;
                int i = y * sp.W + x;
                if (layers[x, y] == null) { sb.Append($"    {{ \"x\": {x}, \"y\": {y}, \"active\": false, \"layers\": [] }}"); continue; }
                string ov = gravel.Contains(i) ? "{ \"kind\": \"gravel\" }" : clips.Contains(i) ? "{ \"kind\": \"clip\", \"hits\": 1 }" : ice.Contains(i) ? "{ \"kind\": \"ice\", \"hits\": 1 }" : "{ \"kind\": \"none\" }";
                sb.Append($"    {{ \"x\": {x}, \"y\": {y}, \"active\": true, \"layers\": [")
                  .Append(string.Join(", ", layers[x, y].ConvertAll(c => "\"" + c + "\"")))
                  .Append("], \"overlay\": ").Append(ov)
                  .Append(", \"hasBottomStar\": ").Append(stars.Contains(i) ? "true" : "false");
                if (protect.Contains(i)) sb.Append(", \"protected\": true");
                sb.Append(" }");
            }
            sb.Append("\n  ]\n}\n");
            return sb.ToString();
        }

        /// <summary>Açgözlü oyuncu: tepside Kürek varsa çakıla; yoksa en çok katman toplayan güvenli dokunuş; yoksa güvenli
        /// sürükle-değiştir; yoksa tepsideki başka özel taş. Toplanan renkleri sayar.</summary>
        private static Dictionary<string, int> Greedy(LevelState s, int maxMoves, out int score, out GameStatus status)
        {
            var got = new Dictionary<string, int>();
            int guard = 0;
            while (s.Status == GameStatus.Idle && s.MovesLeft > 0 && guard++ < maxMoves * 3)
            {
                MoveResult best = null;
                LevelState bestState = null;
                int bestGain = -1;
                void Try(Command c)
                {
                    var t = s.Clone();
                    var r = RulesEngine.TryApply(t, c);
                    if (!r.Accepted || t.ProtectedBroken) return;
                    int gain = 0;
                    foreach (var w in r.Waves) gain += w.Cells.Count;
                    foreach (var w in r.Waves) gain += w.GravelCleared.Count * 2;
                    if (t.Status == GameStatus.Won) gain += 1000;
                    if (t.Status == GameStatus.LostNoLegalMoves) gain -= 1000;
                    if (gain > bestGain) { bestGain = gain; best = r; bestState = t; }
                }
                var b = s.Board;
                int shovel = s.Tray.IndexOf(SpecialKind.Shovel);
                if (shovel >= 0)
                    for (int i = 0; i < b.Count; i++) if (b[i].Active && b[i].Gravel) { Try(Command.UseSpecial(shovel, b.X(i), b.Y(i))); break; }
                if (best == null)
                {
                    foreach (var g in GroupFinder.AllComponents(b)) if (g.Count >= GroupFinder.MinGroup) Try(Command.Tap(b.X(g[0]), b.Y(g[0])));
                    for (int i = 0; i < b.Count; i++) if (b[i].Active && b[i].HasSpecial) Try(Command.Tap(b.X(i), b.Y(i)));
                }
                if (best == null)
                    for (int i = 0; i < b.Count; i++) for (int dir = 0; dir < 4; dir++) Try(Command.Swap(b.X(i), b.Y(i), dir));
                if (best == null)
                    for (int slot = 0; slot < s.Tray.Count && best == null; slot++)
                        for (int i = 0; i < b.Count; i++) Try(Command.UseSpecial(slot, b.X(i), b.Y(i)));
                if (best == null) break;
                foreach (var w in best.Waves)
                    foreach (var c in w.Cells)
                    {
                        string key = ColorIds.ToId(c.Removed);
                        got[key] = got.TryGetValue(key, out var v) ? v + 1 : 1;
                    }
                s = bestState;
            }
            score = s.Score;
            status = s.Status;
            return got;
        }
    }
}
