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
            public int Orders, BucketColors = 3;
            /// <summary>2026-11-23 (kullanıcı: "10 tane 15 tane değil 50 60 gibi büyük adetlerde görev olsun"): renk başına görev aralığı.</summary>
            public int TargetMin = 50, TargetMax = 60;
            /// <summary>2026-11-24 yukarıdan dolma (kullanıcı: "Belli sayıda yeni taşlar da en üstten oyuna girsin"): girecek yeni yığın sayısı.</summary>
            public int Refill = 24;
            /// <summary>Üst rengin komşusunu kopyalama olasılığı (&lt;0: eski kural). 2026-11-24 (kullanıcı: "aynı taşlar çok fazla büyük
            /// gruplarla oluşturulmuş"): torba bölümlerinde düşük, gruplar küçük.</summary>
            public double Copy = -1;
            /// <summary>Torba bölümü süresi: açgözlü oyuncunun eylem sayısı × bu (sn). 2026-11-24 (kullanıcı: "süre çok uzun … zor ve
            /// eğlence odaklı olmalı").</summary>
            public float Pace = 2.2f;
            /// <summary>Katalog bölümü (2026-11-24): başlıksız, trialType "bag", çözüm kaydı; renkler o bölümde açılmış olanlar.</summary>
            public bool Catalog;
            public string[] Palette;
            public bool IsBucket => Orders > 0;

            /// <summary>Torba oyuğu (kullanıcı çizimi): üst ortada basamaklı boşluk; satır r'de r+1..W-2-r sütunları boş (3 satır).</summary>
            public bool Notch(int x, int y) => IsBucket && y < 3 && x >= y + 1 && x <= W - 2 - y;
            public bool Empty(int x, int y) => Hole(x, y) || Notch(x, y);
        }

        // 2026-11-23 (kullanıcı: "bazı bölümlerde roket ve altın rulonun olduğu alana kovalar gelecek … test alanımıza örnek bölümler
        // oluştur. animasyonlarına oynanışına bakacağım"; 2026-11-21: "eski testler kalmasın"): 10 kovalı süreli bölüm. Kova ortada,
        // solda Altın Rulo, sağda roket yuvası; kovanın rengindeki taşlar kovaya akar, yarım kova dokunarak gönderilir.
        private static List<Spec> Specs() => new List<Spec>
        {
            new Spec { Title = "Torba: İlk Tanışma", Seed = 111, W = 8, H = 9, MinL = 3, MaxL = 4, Colors = 3, Timed = 240, Orders = 4, BucketColors = 2, TargetMin = 50, TargetMax = 50, Copy = 0.3, Pace = 2.6f },
            new Spec { Title = "Torba: Üç Renk", Seed = 222, W = 8, H = 9, MinL = 4, MaxL = 4, Colors = 3, Timed = 240, Orders = 6, BucketColors = 3, Copy = 0.4 },
            new Spec { Title = "Torba: Derindeki Renk", Seed = 333, W = 8, H = 9, MinL = 4, MaxL = 4, Colors = 3, Timed = 300, Orders = 4, BucketColors = 2, Copy = 0.25 },
            new Spec { Title = "Torba: Hızlı Sipariş", Seed = 444, W = 8, H = 9, MinL = 4, MaxL = 4, Colors = 3, Timed = 300, Orders = 6, BucketColors = 2, Copy = 0.22, Pace = 2.0f },
            new Spec { Title = "Torba: Yan Boşluklar", Seed = 555, W = 8, H = 9, MinL = 4, MaxL = 4, Colors = 3, Timed = 200, Orders = 4, BucketColors = 2, Copy = 0.25, Hole = (x, y) => y >= 3 && ((x == 0 && y % 2 == 0) || (x == 7 && y % 2 == 1)) },
            new Spec { Title = "Torba: Köşeli Tahta", Seed = 666, W = 8, H = 9, MinL = 4, MaxL = 4, Colors = 3, Timed = 300, Orders = 4, BucketColors = 2, Copy = 0.25, Hole = (x, y) => y >= 7 && (x <= 1 || x >= 6) },
            new Spec { Title = "Torba: Roket ve Rulo", Seed = 777, W = 8, H = 9, MinL = 4, MaxL = 4, Colors = 3, Timed = 300, Orders = 4, BucketColors = 2, Copy = 0.3 },
            new Spec { Title = "Torba: Büyük Torbalar", Seed = 888, W = 8, H = 9, MinL = 4, MaxL = 4, Colors = 3, Timed = 300, Orders = 2, BucketColors = 2, TargetMin = 60, TargetMax = 60, Copy = 0.25 },
            new Spec { Title = "Torba: Uzun Zincir", Seed = 999, W = 8, H = 9, MinL = 4, MaxL = 4, Colors = 3, Timed = 240, Orders = 9, BucketColors = 3, Copy = 0.35, Refill = 32 },
            new Spec { Title = "Torba: Patron", Seed = 1212, W = 8, H = 9, MinL = 4, MaxL = 4, Colors = 3, Timed = 200, Orders = 6, BucketColors = 2, Copy = 0.3, Refill = 32, Pace = 2.2f,
                Hole = (x, y) => y >= 3 && Math.Min(x, 7 - x) + Math.Min(y, 8 - y) < 2 },
        };

        /// <summary>Katalogdaki torba bölümleri: 15'ten başlayıp 8'de bir (2026-11-24, kullanıcı: "süreli bölümleri torba bölümüne
        /// çevirelim, ve ilk bölüm 15 te olsun. zorluğu bölüm seviyesine göre olsun. ve 8 bölümde bir olsun süreli bölümler. tüm 1000
        /// bölümü yeniden üretme 15. bölümden +8 yaparak o bölümleri yeniden üret").</summary>
        public static bool IsCatalogBag(int n) => n >= 15 && (n - 15) % 8 == 0;

        /// <summary>Bölüm numarasına göre torba bölümü ayarı: hedef, renk sayısı, grup boyu, tempo ve yeni yığın hakkı ilerledikçe zorlaşır.</summary>
        private static Spec CatalogSpec(int n)
        {
            float t = Mathf.Clamp01((n - 15) / 600f);
            // O bölümde açılmış renkler (LevelLab V14ColorIntro: inci ve turkuaz 1, mercan 8, mor 21, lacivert 61).
            var open = new List<string> { "yellow", "blue" };
            if (n >= 8) open.Add("coral");
            if (n >= 21) open.Add("purple");
            if (n >= 61) open.Add("navy");
            // Toplam görev taşı ilerledikçe 60 → 110; üç torba rengi yalnız 500 sonrası iki bölümde bir (toplam aynı, renk başına az).
            int bagColors = n >= 500 && ((n - 15) / 8) % 2 == 1 ? 3 : 2;
            int colors = Math.Min(open.Count, bagColors == 3 ? 3 : bagColors + 1); // üç torba renginde fazladan renk yok (gruplar büyük kalsın)
            bagColors = Math.Min(bagColors, colors - 1);
            float per = Mathf.Lerp(60f, 110f, t) / bagColors;
            int tmin = Math.Max(15, (int)Math.Round(per * 0.92f / 5f) * 5), tmax = Math.Max(tmin, (int)Math.Round(per * 1.08f / 5f) * 5);
            int kind = ((n - 15) / 8) % 3;
            Func<int, int, bool> hole = (x, y) => false;
            if (n >= 40 && kind == 1) hole = (x, y) => y >= 7 && (x <= 1 || x >= 6);
            else if (n >= 40 && kind == 2) hole = (x, y) => y >= 3 && ((x == 0 && y % 2 == 0) || (x == 7 && y % 2 == 1));
            return new Spec
            {
                Title = "level_" + n, Seed = n * 7919, W = 8, H = 9, MinL = n < 100 ? 3 : 4, MaxL = 4, Colors = colors,
                Timed = 300, Orders = bagColors * 2 + (n > 300 ? 1 : 0), BucketColors = bagColors,
                TargetMin = tmin, TargetMax = Math.Max(tmin, tmax), Copy = Mathf.Lerp(0.3f, 0.2f, t), Pace = Mathf.Lerp(3.0f, 2.1f, t),
                Refill = (int)Mathf.Lerp(16f, 32f, t), Catalog = true, Palette = open.ToArray(), Hole = hole,
            };
        }

        [MenuItem("KatKat/Katalog torba bölümlerini üret")]
        public static void BuildCatalogBags()
        {
            const string Dir = "Assets/KatKat/Resources/Levels";
            var report = new StringBuilder("# Katalog torba bölümleri\n\n| Bölüm | Görev | Süre |\n|---|---|---|\n");
            int made = 0;
            for (int n = 15; n <= 1000; n += 8)
            {
                var sp = CatalogSpec(n);
                string id = $"level_{n:000}";
                string json = BucketLevel(sp, id, out var d, out var v, out var orders);
                File.WriteAllText(Path.Combine(Dir, id + ".json"), json, new UTF8Encoding(false));
                report.Append($"| {n} | {orders} | {d.TimeLimit} sn |\n");
                Debug.Log($"[Katalog] {id}: {orders}");
                made++;
            }
            // Eski süreli bölümler (41'den 15'te bir) torba dizisinde değilse süresiz olur (kendi hamle sınırıyla).
            int untimed = 0;
            for (int n = 1; n <= 1000; n++)
            {
                if (IsCatalogBag(n)) continue;
                string path = Path.Combine(Dir, $"level_{n:000}.json");
                if (!File.Exists(path)) continue;
                string text = File.ReadAllText(path);
                var m = System.Text.RegularExpressions.Regex.Match(text, "\n  \"timeLimit\": *[0-9]+,\r?");
                if (!m.Success) continue;
                File.WriteAllText(path, text.Remove(m.Index, m.Length), new UTF8Encoding(false));
                untimed++;
            }
            File.WriteAllText("Logs/catalog_bags.md", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"[Katalog] {made} torba bölümü yazıldı; {untimed} eski süreli bölüm süresiz oldu");
        }

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
            int taps = 400; // süre sonradan açgözlü oyuncunun eylem sayısından
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
                // Renk başına 50–60 (5'in katı) görev; tahtadaki o rengin en çok %85'i. Sıra renkleri döndürür, rengin görevi
                // sıradaki torbalarına bölünür (miktar yalnız görev toplamı için; torba rengin kalanı kadar alır).
                var target = new Dictionary<string, int>();
                foreach (var c in colors)
                {
                    int t = sp.TargetMin + 5 * rng.Next((sp.TargetMax - sp.TargetMin) / 5 + 1);
                    target[c] = Math.Min(t, (int)((avail[c] + sp.Refill * 2.5f / Math.Max(1, sp.Colors)) * 0.8f)); // yeni yığınlar da sayılır
                }
                bool small = false;
                foreach (var c in colors) if (target[c] < sp.TargetMin) small = true;
                if (small) continue;
                var seq = new List<string>();
                for (int k = 0; k < sp.Orders; k++) seq.Add(colors[k % colors.Count]);
                var orders = new List<KeyValuePair<string, int>>();
                foreach (var c in colors)
                {
                    int parts = seq.FindAll(x => x == c).Count;
                    int each = target[c] / Math.Max(1, parts), rest = target[c] - each * Math.Max(1, parts);
                    for (int k = 0, seen = 0; k < seq.Count; k++)
                        if (seq[k] == c) { orders.Add(new KeyValuePair<string, int>(c, each + (seen == 0 ? rest : 0))); seen++; }
                }
                // Sıra: seq düzeninde (renkler dönüşümlü).
                var sized = new List<KeyValuePair<string, int>>();
                foreach (var c in seq)
                {
                    int i = orders.FindIndex(o => o.Key == c);
                    sized.Add(orders[i]);
                    orders.RemoveAt(i);
                }
                var probe = LevelParser.Parse(ComposeBucket(sp, id, cells, sized, 0, 999), id);
                Greedy(LevelLoader.ToState(probe), taps, out int score, out var status);
                if (status != GameStatus.Won) continue;
                int timed = Math.Max(60, Math.Min(sp.Timed, (int)Math.Ceiling(_greedyActions * sp.Pace / 5f) * 5));
                foreach (var c in _greedyCmds) if (c.Type != CommandType.Tap && c.Type != CommandType.UseSpecial && c.Type != CommandType.SendBucket) throw new Exception($"[Lab] {id}: kaydedilemeyen komut {c.Type}");
                string json = ComposeBucket(sp, id, cells, sized, score, timed, new List<Command>(_greedyCmds));
                d = LevelParser.Parse(json, id);
                var errs = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
                if (errs.Count > 0) throw new Exception($"[Lab] {id}: {string.Join("; ", errs)}");
                verdict = "Won";
                var sums = new List<string>();
                foreach (var c in colors) sums.Add(c + " " + target[c]);
                orderText = string.Join(", ", sums) + $" ({sized.Count} torba, {timed} sn, açgözlü {_greedyActions} eylem)";
                return json;
            }
            throw new Exception($"[Lab] {id}: kazanılabilir kovalı tahta bulunamadı");
        }

        private static string ComposeBucket(Spec sp, string id, string cells, List<KeyValuePair<string, int>> orders, int score, int timed, List<Command> solution = null)
        {
            var sums = new List<KeyValuePair<string, int>>();
            foreach (var o in orders)
            {
                int i = sums.FindIndex(p => p.Key == o.Key);
                if (i < 0) sums.Add(o); else sums[i] = new KeyValuePair<string, int>(o.Key, sums[i].Value + o.Value);
            }
            var sb = new StringBuilder();
            sb.Append("{\n  \"schemaVersion\": 2,\n  \"id\": \"").Append(id).Append("\",\n");
            sb.Append("  \"capabilities\": [\"specials\", \"buckets\"").Append(sp.Refill > 0 ? ", \"refill\"" : "").Append("],\n");
            if (sp.Catalog) sb.Append("  \"trialType\": \"bag\",\n  \"rules\": [],\n");
            else sb.Append("  \"title\": \"").Append(sp.Title).Append("\",\n  \"trialType\": \"lab\",\n  \"rules\": [],\n");
            sb.Append($"  \"width\": {sp.W},\n  \"height\": {sp.H},\n  \"moveLimit\": 99,\n  \"timeLimit\": {timed},\n");
            sb.Append("  \"rollEnabled\": false,\n  \"startingRoll\": false,\n");
            int s1 = Math.Max(100, (int)(score * 0.55f) / 10 * 10), s2 = Math.Max(s1 + 50, (int)(score * 0.8f) / 10 * 10);
            sb.Append($"  \"stars\": [{s1}, {s2}],\n  \"goals\": [\n");
            for (int i = 0; i < sums.Count; i++)
                sb.Append("    { \"type\": \"collectColor\", \"color\": \"").Append(sums[i].Key).Append("\", \"amount\": ").Append(sums[i].Value).Append(" }").Append(i < sums.Count - 1 ? ",\n" : "\n");
            sb.Append("  ],\n");
            if (sp.Refill > 0) sb.Append($"  \"refill\": {{ \"count\": {sp.Refill}, \"minLayers\": 2, \"maxLayers\": 3 }},\n");
            sb.Append("  \"buckets\": [\n");
            for (int i = 0; i < orders.Count; i++)
                sb.Append("    { \"color\": \"").Append(orders[i].Key).Append("\", \"amount\": ").Append(orders[i].Value).Append(" }").Append(i < orders.Count - 1 ? ",\n" : "\n");
            sb.Append("  ],\n");
            if (sp.Catalog && solution != null)
            {
                sb.Append("  \"solution\": [\n");
                for (int i = 0; i < solution.Count; i++)
                {
                    var c = solution[i];
                    string step = c.Type == CommandType.SendBucket ? "{ \"type\": \"send\" }"
                        : c.Type == CommandType.UseSpecial ? $"{{ \"type\": \"use\", \"slot\": {c.Slot}, \"x\": {c.X}, \"y\": {c.Y} }}"
                        : $"{{ \"type\": \"tap\", \"x\": {c.X}, \"y\": {c.Y} }}";
                    sb.Append("    ").Append(step).Append(i < solution.Count - 1 ? ",\n" : "\n");
                }
                sb.Append("  ],\n");
            }
            sb.Append("  ").Append(cells);
            return sb.ToString();
        }

        private static string Cells(Spec sp, System.Random rng)
        {
            var palette = new List<string>(sp.Palette ?? Palette);
            // Renk seti tohuma göre döner (bölümler farklı renklerle).
            int rot = rng.Next(palette.Count);
            var colors = new List<string>();
            for (int i = 0; i < sp.Colors; i++) colors.Add(palette[(rot + i) % palette.Count]);
            var top = new string[sp.W, sp.H];
            var layers = new List<string>[sp.W, sp.H];
            for (int y = 0; y < sp.H; y++)
            for (int x = 0; x < sp.W; x++)
            {
                if (sp.Empty(x, y)) continue;
                int n = rng.Next(sp.MinL, sp.MaxL + 1);
                var l = new List<string>();
                for (int k = 0; k < n; k++) l.Add(colors[rng.Next(colors.Count)]);
                // Üst renk komşusuna benzesin (gruplar oluşsun; sürükle-değiştir bölümünde daha az).
                double copy = sp.Copy >= 0 ? sp.Copy : sp.MaxL <= 2 ? 0.25 : 0.45;
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
        /// <summary>Son açgözlü oyunun bütün eylemleri (dokunuş, tepsi, torba gönderme).</summary>
        private static int _greedyActions;
        /// <summary>Son açgözlü oyunun komutları (katalog bölümünün kayıtlı çözümü).</summary>
        private static readonly List<Command> _greedyCmds = new List<Command>();

        private static Dictionary<string, int> Greedy(LevelState s, int maxMoves, out int score, out GameStatus status)
        {
            var got = new Dictionary<string, int>();
            int guard = 0, made = 0, emptySends = 0;
            _greedyActions = 0;
            _greedyCmds.Clear();
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
                // Torba kipi: hiçbir dokunuş torbaya taş vermiyorsa torba gönderilir (doluysa teslim; boşsa sıradaki renk gelir).
                // Torba kendiliğinden gitmez (2026-11-23); boş gönderim arka arkaya sıradaki renk sayısını aşmaz.
                if (s.Bucket != null && s.Bucket.Active && (s.Bucket.Have > 0 || emptySends < s.Bucket.Queue.Count + 1))
                {
                    int bucketGain = 0;
                    if (best != null) foreach (var w in best.Waves) bucketGain += w.BucketCells.Count;
                    if (bucketGain == 0 || s.Bucket.Have >= s.Bucket.Need)
                    {
                        var t = s.Clone();
                        bool empty = s.Bucket.Have == 0;
                        var r = RulesEngine.TryApply(t, Command.SendBucket());
                        if (r.Accepted) { best = r; bestState = t; emptySends = empty ? emptySends + 1 : 0; }
                    }
                }
                if (best != null && best.Command.Type != CommandType.SendBucket) emptySends = 0;
                if (best == null) break;
                foreach (var w in best.Waves)
                    foreach (var c in w.Cells)
                    {
                        string key = ColorIds.ToId(c.Removed);
                        got[key] = got.TryGetValue(key, out var v) ? v + 1 : 1;
                    }
                if (best.Command.Type != CommandType.UseSpecial && best.Command.Type != CommandType.SendBucket) made++; // tepsi ve kova hamle harcamaz
                _greedyActions++;
                _greedyCmds.Add(best.Command);
                s = bestState;
            }
            score = s.Score;
            status = s.Status;
            return got;
        }
    }
}
