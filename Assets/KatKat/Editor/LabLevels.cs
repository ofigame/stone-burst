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
        private static readonly string[] Palette = { "coral", "blue", "yellow", "purple", "navy" }; // 2026-11-21: yeşil yok

        private sealed class Spec
        {
            public string Title;
            public int W = 7, H = 8, Colors = 4, MinL = 1, MaxL = 3, Moves = 20, Seed;
            public Func<int, int, bool> Gravel = (x, y) => false;
            public Func<int, int, bool> Hole = (x, y) => false;
            public int Protect, Clips, Ice, Stars, GoalColors = 2;
            /// <summary>Süreli bölüm (sn); 0 → hamleli. Süreli bölümde hamle sınırsız; açgözlü oyuncu süre/4 hamle oynar.</summary>
            public int Timed;
            public int SimMoves => Timed > 0 ? Timed / 4 : Moves;
            public float GoalShare = 0.7f;
            /// <summary>2026-11-23 kova kipi: sipariş (kova) sayısı; 0 → kovasız. Kova renkleri ve miktar aralığı.</summary>
            public int Orders, BucketColors = 3, OrderMin = 4, OrderMax = 8;
            public bool IsBucket => Orders > 0;
        }

        // 2026-11-23 (kullanıcı: "bazı bölümlerde roket ve altın rulonun olduğu alana kovalar gelecek … test alanımıza örnek bölümler
        // oluştur. animasyonlarına oynanışına bakacağım"; 2026-11-21: "eski testler kalmasın"): 10 kovalı süreli bölüm. Kova ortada,
        // solda Altın Rulo, sağda roket yuvası; kovanın rengindeki taşlar kovaya akar, yarım kova dokunarak gönderilir.
        private static List<Spec> Specs() => new List<Spec>
        {
            new Spec { Title = "Kova: İlk Tanışma", Seed = 111, MinL = 2, MaxL = 2, Colors = 3, Timed = 90, Orders = 4, BucketColors = 2, OrderMin = 4, OrderMax = 6 },
            new Spec { Title = "Kova: Üç Renk", Seed = 222, MinL = 2, MaxL = 2, Colors = 4, Timed = 100, Orders = 6, BucketColors = 3, Hole = (x, y) => (x == 0 || x == 6) && (y == 0 || y == 7) },
            new Spec { Title = "Kova: Derindeki Renk", Seed = 333, MinL = 3, MaxL = 3, Colors = 4, Timed = 110, Orders = 6, BucketColors = 3, OrderMin = 6, OrderMax = 9 },
            new Spec { Title = "Kova: Hızlı Sipariş", Seed = 444, MinL = 2, MaxL = 2, Colors = 3, Timed = 70, Orders = 8, BucketColors = 3, OrderMin = 3, OrderMax = 5 },
            new Spec { Title = "Kova: Beş Renk", Seed = 555, MinL = 2, MaxL = 3, Colors = 5, Timed = 120, Orders = 7, BucketColors = 4, Hole = (x, y) => (x == 0 && y % 2 == 0) || (x == 6 && y % 2 == 1) },
            new Spec { Title = "Kova: Artı Tahta", Seed = 666, MinL = 3, MaxL = 3, Colors = 4, Timed = 110, Orders = 6, BucketColors = 3, Hole = (x, y) => (x <= 1 || x >= 5) && (y <= 1 || y >= 6) },
            new Spec { Title = "Kova: Roket ve Rulo", Seed = 777, MinL = 2, MaxL = 3, Colors = 3, Timed = 100, Orders = 5, BucketColors = 2, OrderMin = 8, OrderMax = 12 },
            new Spec { Title = "Kova: Büyük Kovalar", Seed = 888, MinL = 3, MaxL = 3, Colors = 4, Timed = 120, Orders = 5, BucketColors = 3, OrderMin = 9, OrderMax = 13 },
            new Spec { Title = "Kova: Uzun Zincir", Seed = 999, MinL = 2, MaxL = 3, Colors = 4, Timed = 130, Orders = 10, BucketColors = 4, OrderMin = 3, OrderMax = 6 },
            new Spec { Title = "Kova: Patron", Seed = 1212, W = 8, H = 9, MinL = 3, MaxL = 3, Colors = 5, Timed = 150, Orders = 9, BucketColors = 4, OrderMin = 5, OrderMax = 10,
                Hole = (x, y) => Math.Min(x, 7 - x) + Math.Min(y, 8 - y) < 2 },
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
                if (sp.IsBucket)
                {
                    string bj = BucketLevel(sp, id, out var bd, out var bv, out var bOrders);
                    File.WriteAllText(Path.Combine(OutDir, id + ".json"), bj, new UTF8Encoding(false));
                    report.Append($"| {n + 1} | {sp.Title} | {sp.W}x{sp.H} | {sp.Timed} sn | {bOrders} | {bv} |\n");
                    Debug.Log($"[Lab] {id} {sp.Title}: {bv} ({bOrders})");
                    continue;
                }
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
                    got = Greedy(probe, sp.SimMoves, out score, out _);
                    int total = 0; foreach (var v in got.Values) total += v;
                    if (total >= sp.SimMoves * 3) { json = Compose(sp, id, new System.Random(sp.Seed * 131 + attempt), got, score, 0); break; }
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
                    Greedy(LevelLoader.ToState(d), sp.SimMoves, out _, out var status);
                    verdict = status.ToString();
                    if (status == GameStatus.Won) break;
                    json = Compose(sp, id, null, got, score, shrink + 1, json);
                }
                if (verdict != "Won") throw new Exception($"[Lab] {id}: açgözlü oyuncu kazanamadı ({verdict})");
                File.WriteAllText(Path.Combine(OutDir, id + ".json"), json, new UTF8Encoding(false));
                var goals = new StringBuilder();
                foreach (var g in d.Goals) goals.Append(g.Color).Append(' ').Append(g.Amount).Append(", ");
                report.Append($"| {n + 1} | {sp.Title} | {sp.W}x{sp.H} | {(sp.Timed > 0 ? sp.Timed + " sn" : sp.Moves + " hamle")} | {goals.ToString().TrimEnd(',', ' ')} | {verdict} |\n");
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
            sb.Append($"  \"width\": {sp.W},\n  \"height\": {sp.H},\n  \"moveLimit\": {(sp.Timed > 0 ? 99 : sp.Moves)},\n");
            if (sp.Timed > 0) sb.Append($"  \"timeLimit\": {sp.Timed},\n");
            sb.Append("  \"rollEnabled\": false,\n  \"startingRoll\": false,\n");
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

        /// <summary>
        /// Kovalı bölüm: tahta üretilir, renk bolluğuna göre sipariş sırası (kova renkleri sırayla, ardışık aynı renk yok, renk
        /// başına talep tahtadakinin en çok yarısı) kurulur; kovayı bilen açgözlü oyuncu süre/3 dokunuşta kazanmalı (kazanamazsa
        /// miktarlar %12 küçülür, olmazsa yeni tahta).
        /// </summary>
        private static string BucketLevel(Spec sp, string id, out LevelData d, out string verdict, out string orderText)
        {
            int taps = sp.Timed / 3;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                var rng = new System.Random(sp.Seed * 131 + attempt);
                string probeJson = Compose(sp, id, rng, null, 0, 0);
                int cellsAt = probeJson.IndexOf("\"cells\"", StringComparison.Ordinal);
                string cells = probeJson.Substring(cellsAt);
                var d0 = LevelParser.Parse(probeJson, id);
                if (LevelValidator.ValidateStatic(d0, LevelValidator.ScopeFor(d0)).Count > 0) continue;
                var avail = new Dictionary<string, int>();
                foreach (var c in d0.Cells) foreach (var l in c.Layers) avail[l] = (avail.TryGetValue(l, out var v) ? v : 0) + 1;
                var colors = new List<string>(avail.Keys);
                colors.Sort((x, y) => avail[y].CompareTo(avail[x]));
                if (colors.Count > sp.BucketColors) colors.RemoveRange(sp.BucketColors, colors.Count - sp.BucketColors);
                var orders = new List<KeyValuePair<string, int>>();
                string last = null;
                for (int k = 0; k < sp.Orders; k++)
                {
                    string c = colors[rng.Next(colors.Count)];
                    if (c == last && colors.Count > 1) c = colors[(colors.IndexOf(c) + 1) % colors.Count];
                    last = c;
                    orders.Add(new KeyValuePair<string, int>(c, rng.Next(sp.OrderMin, sp.OrderMax + 1)));
                }
                for (int shrink = 0; shrink < 6; shrink++)
                {
                    float k = (float)Math.Pow(0.88, shrink);
                    var sized = new List<KeyValuePair<string, int>>();
                    var demand = new Dictionary<string, int>();
                    foreach (var o in orders) demand[o.Key] = (demand.TryGetValue(o.Key, out var v) ? v : 0) + o.Value;
                    foreach (var o in orders)
                    {
                        float cap = Math.Min(1f, avail[o.Key] * 0.5f / demand[o.Key]);
                        sized.Add(new KeyValuePair<string, int>(o.Key, Math.Max(2, (int)Math.Round(o.Value * cap * k))));
                    }
                    var probe = LevelParser.Parse(ComposeBucket(sp, id, cells, sized, 0), id);
                    Greedy(LevelLoader.ToState(probe), taps, out int score, out var status);
                    if (status != GameStatus.Won) continue;
                    string json = ComposeBucket(sp, id, cells, sized, score);
                    d = LevelParser.Parse(json, id);
                    var errs = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
                    if (errs.Count > 0) throw new Exception($"[Lab] {id}: {string.Join("; ", errs)}");
                    verdict = "Won";
                    orderText = string.Join(" ", sized.ConvertAll(o => o.Key + " " + o.Value));
                    return json;
                }
            }
            throw new Exception($"[Lab] {id}: kazanılabilir kovalı tahta bulunamadı");
        }

        private static string ComposeBucket(Spec sp, string id, string cells, List<KeyValuePair<string, int>> orders, int score)
        {
            var sums = new List<KeyValuePair<string, int>>();
            foreach (var o in orders)
            {
                int i = sums.FindIndex(p => p.Key == o.Key);
                if (i < 0) sums.Add(o); else sums[i] = new KeyValuePair<string, int>(o.Key, sums[i].Value + o.Value);
            }
            var sb = new StringBuilder();
            sb.Append("{\n  \"schemaVersion\": 2,\n  \"id\": \"").Append(id).Append("\",\n");
            sb.Append("  \"capabilities\": [\"specials\", \"buckets\"],\n");
            sb.Append("  \"title\": \"").Append(sp.Title).Append("\",\n  \"trialType\": \"lab\",\n  \"rules\": [],\n");
            sb.Append($"  \"width\": {sp.W},\n  \"height\": {sp.H},\n  \"moveLimit\": 99,\n  \"timeLimit\": {sp.Timed},\n");
            sb.Append("  \"rollEnabled\": false,\n  \"startingRoll\": false,\n");
            int s1 = Math.Max(100, (int)(score * 0.55f) / 10 * 10), s2 = Math.Max(s1 + 50, (int)(score * 0.8f) / 10 * 10);
            sb.Append($"  \"stars\": [{s1}, {s2}],\n  \"goals\": [\n");
            for (int i = 0; i < sums.Count; i++)
                sb.Append("    { \"type\": \"collectColor\", \"color\": \"").Append(sums[i].Key).Append("\", \"amount\": ").Append(sums[i].Value).Append(" }").Append(i < sums.Count - 1 ? ",\n" : "\n");
            sb.Append("  ],\n  \"buckets\": [\n");
            for (int i = 0; i < orders.Count; i++)
                sb.Append("    { \"color\": \"").Append(orders[i].Key).Append("\", \"amount\": ").Append(orders[i].Value).Append(" }").Append(i < orders.Count - 1 ? ",\n" : "\n");
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
            int guard = 0, made = 0;
            while (s.Status == GameStatus.Idle && s.MovesLeft > 0 && made < maxMoves && guard++ < maxMoves * 4)
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
                    foreach (var w in r.Waves) gain += w.BucketCells.Count * 8; // kova kipi: kovaya giden taş öncelikli
                    gain += r.BucketSends.Count * 20;
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
                    for (int slot = 0; slot < s.Tray.Count && best == null; slot++)
                        for (int i = 0; i < b.Count; i++) Try(Command.UseSpecial(slot, b.X(i), b.Y(i)));
                // Kova kipi: hiçbir dokunuş kovaya taş vermiyorsa ve kovada taş varsa kova gönderilir.
                if (s.Bucket != null && s.Bucket.Active && s.Bucket.Have > 0)
                {
                    int bucketGain = 0;
                    if (best != null) foreach (var w in best.Waves) bucketGain += w.BucketCells.Count;
                    if (bucketGain == 0)
                    {
                        var t = s.Clone();
                        var r = RulesEngine.TryApply(t, Command.SendBucket());
                        if (r.Accepted) { best = r; bestState = t; }
                    }
                }
                if (best == null) break;
                foreach (var w in best.Waves)
                    foreach (var c in w.Cells)
                    {
                        string key = ColorIds.ToId(c.Removed);
                        got[key] = got.TryGetValue(key, out var v) ? v + 1 : 1;
                    }
                if (best.Command.Type != CommandType.UseSpecial && best.Command.Type != CommandType.SendBucket) made++; // tepsi ve kova hamle harcamaz
                s = bestState;
            }
            score = s.Score;
            status = s.Status;
            return got;
        }
    }
}
