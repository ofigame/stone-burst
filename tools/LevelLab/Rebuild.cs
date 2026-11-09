using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// rebuild [from] [to] [games]: v6/v7 kural ve tasarım geçişi.
//   1) Özel taş tepsisi: çözüm ve bot yeni kuralla (tepsi hamle harcamaz) yeniden üretilir.
//   2) Tahta şekli: dış hat (kesik köşe, L, T, artı, merdiven, kum saati, U, çatı, elmas) ya da v7 iç boşluklu şekil
//      (halka, çift göz, kesik halka; 21+ bölümlerin her üçüncüsü). Açılışta boş hücreler katmanla doldurulur;
//      mekanik hücreleri (örtü, yıldız, sünger, ataş grubu, düğme/geçit, döner taban) oyulmaz.
//   3) Zorluk (v7, kullanıcı: "zorluk mantık yönünde olacak"): hamle limiti = ışın çözümü + makul pay (erken 4–6,
//      geç 2–4). Zorluk renk hedefi çarpanı ve aday seçimiyle gelir: hedef bot oranına yakın adaylar arasında
//      açgözlü / dar ışının çözemediği (plan isteyen) aday tercih edilir.
// Girdi KATKAT_REBUILD_SRC (varsayılan levels_src), çıktı KATKAT_REBUILD_OUT (varsayılan levels_src_v7); takas ayrı adım.
// 1–5 öğretici bölümler (özel taş yok) dokunulmaz.
// Girdi levels_src/, çıktı levels_src_v6/ (takas ayrı adım). 1–5 öğretici bölümler (özel taş yok) dokunulmaz.
static partial class Program
{
    static readonly string[] ShapeNames = { "kesik köşe", "L", "T", "artı", "merdiven", "kum saati", "U", "çatı", "elmas" };

    /// <summary>Şeklin oyduğu hücreler (dikdörtgen içinde, x ayna <paramref name="flip"/>).</summary>
    static HashSet<(int, int)> ShapeCut(int kind, int w, int h, bool flip)
    {
        var cut = new HashSet<(int, int)>();
        void Add(int x, int y) { if (x >= 0 && y >= 0 && x < w && y < h) cut.Add((flip ? w - 1 - x : x, y)); }
        int qw = Math.Max(1, w / 3), qh = Math.Max(1, h / 4);
        switch (kind)
        {
            case 0: // kesik köşe: dört köşede 3 hücrelik üçgen
                foreach (var (cx, cy, sx, sy) in new[] { (0, 0, 1, 1), (w - 1, 0, -1, 1), (0, h - 1, 1, -1), (w - 1, h - 1, -1, -1) })
                { Add(cx, cy); Add(cx + sx, cy); Add(cx, cy + sy); }
                break;
            case 1: // L: sağ üst blok
                for (int y = 0; y < (h + 1) / 2 - 1; y++) for (int x = w - qw - 1; x < w; x++) Add(x, y);
                break;
            case 2: // T: alt iki yan blok
                for (int y = h - qh - 1; y < h; y++) { for (int x = 0; x < qw; x++) Add(x, y); for (int x = w - qw; x < w; x++) Add(x, y); }
                break;
            case 3: // artı: dört köşe blok
                for (int y = 0; y < qh; y++) for (int x = 0; x < qw; x++) { Add(x, y); Add(w - 1 - x, y); Add(x, h - 1 - y); Add(w - 1 - x, h - 1 - y); }
                break;
            case 4: // merdiven: sağ üstten basamak
                for (int y = 0; y < h / 2; y++) for (int x = w - (h / 2 - y); x < w; x++) Add(x, y);
                break;
            case 5: // kum saati: ortada iki yan girinti
                for (int y = h / 3; y < h - h / 3; y++) { Add(0, y); Add(w - 1, y); }
                break;
            case 6: // U: üst ortada çentik
                for (int y = 0; y < Math.Max(1, h / 3); y++)
                {
                    if (w % 2 == 1) Add(w / 2, y);
                    else { Add(w / 2 - 1, y); Add(w / 2, y); }
                }
                break;
            case 7: // çatı: üst köşelerde basamaklı eğim
                for (int y = 0; y < 2; y++) for (int x = 0; x < 2 - y; x++) { Add(x, y); Add(w - 1 - x, y); }
                Add(2, 0); Add(w - 3, 0);
                break;
            default: // elmas: dört köşede basamaklı eğim (2+1)
                foreach (var (cx, cy, sx, sy) in new[] { (0, 0, 1, 1), (w - 1, 0, -1, 1), (0, h - 1, 1, -1), (w - 1, h - 1, -1, -1) })
                { Add(cx, cy); Add(cx + sx, cy); Add(cx + 2 * sx, cy); Add(cx, cy + sy); Add(cx + sx, cy + sy); Add(cx, cy + 2 * sy); }
                break;
        }
        return cut;
    }

    static bool IsMechanic(CellData c) => c.ClipHitsRemaining > 0 || c.IceHits > 0 || c.LockRemaining > 0 || !string.IsNullOrEmpty(c.EnvelopeColor)
        || c.HasBottomStar || c.HasRotor || c.SpongeColors.Count > 0 || c.LinkHits > 0 || c.ButtonGate > 0 || c.GateId > 0;

    /// <summary>Etkin + geçit hücreleri 4-bağlı mı.</summary>
    static bool Connected(LevelData d)
    {
        var set = new HashSet<(int, int)>(d.Cells.Select(c => (c.X, c.Y)));
        if (set.Count == 0) return false;
        var start = set.First();
        var seen = new HashSet<(int, int)> { start };
        var q = new Queue<(int, int)>();
        q.Enqueue(start);
        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            foreach (var n in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                if (set.Contains(n) && seen.Add(n)) q.Enqueue(n);
        }
        return seen.Count == set.Count;
    }

    /// <summary>Eksik/boş (geçit olmayan) hücreleri komşu renklerinden 2–3 katmanla doldurur: açılışta iç boşluk kalmaz.</summary>
    static void FillHoles(LevelData d, Random rng)
    {
        var at = d.Cells.ToDictionary(c => (c.X, c.Y));
        var letters = d.Cells.SelectMany(c => c.Layers).Distinct().ToList();
        if (letters.Count == 0) letters.Add("coral");
        for (int y = 0; y < d.Height; y++)
        for (int x = 0; x < d.Width; x++)
        {
            at.TryGetValue((x, y), out var c);
            if (c != null && (c.Layers.Count > 0 || c.GateId > 0 || !c.Active)) continue;
            if (c == null) { c = new CellData { X = x, Y = y, Active = true }; d.Cells.Add(c); at[(x, y)] = c; }
            int depth = 2 + rng.Next(2);
            var near = new List<string>();
            foreach (var n in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                if (at.TryGetValue(n, out var o) && o.Layers.Count > 0) near.AddRange(o.Layers);
            for (int k = 0; k < depth; k++)
                c.Layers.Add(near.Count > 0 && rng.Next(3) > 0 ? near[rng.Next(near.Count)] : letters[rng.Next(letters.Count)]);
        }
    }

    // v7 iç boşluklu şekiller (2026-11-06, kullanıcı: "ortası boş tahtalar"; boşluğa oyun maskot/fener/kaya çizer).
    static readonly string[] HollowNames = { "halka", "çift göz", "kesik halka" };

    /// <summary>Boyutta ortalanabilen boşluk uzunluğu: tek kenarda 3 (7+), çiftte 2.</summary>
    static int HoleLen(int size) => size % 2 == 1 ? (size >= 7 ? 3 : 2) : 2;

    /// <summary>Boşluk dikdörtgenleri (x, y, w, h). <paramref name="kind"/>: 0 halka, 1 çift göz, 2 kesik halka.</summary>
    static List<(int x, int y, int w, int h)> HollowRects(int kind, int w, int h, int dx, int dy)
    {
        var list = new List<(int x, int y, int w, int h)>();
        if (w < 6 || h < 6) return list;
        if (kind == 1)
        {
            // İki ayrı boşluk: genişse (7+) yan yana, aralarında 1–2 sütun köprü; değilse üst üste.
            if (w >= 7)
            {
                int gap = w % 2 == 0 ? 2 : 1, ring = (w - 4 - gap) / 2;
                int rh = HoleLen(h), y0 = (h - rh) / 2;
                if (ring < 1) return list;
                list.Add((ring + dx, y0 + dy, 2, rh));
                list.Add((ring + 2 + gap + dx, y0 + dy, 2, rh));
            }
            else
            {
                int gap = h % 2 == 0 ? 2 : 1, ring = (h - 4 - gap) / 2;
                int rw = HoleLen(w), x0 = (w - rw) / 2;
                if (ring < 1) return list;
                list.Add((x0 + dx, ring + dy, rw, 2));
                list.Add((x0 + dx, ring + 2 + gap + dy, rw, 2));
            }
            return list;
        }
        int hw = HoleLen(w), hh = HoleLen(h);
        list.Add(((w - hw) / 2 + dx, (h - hh) / 2 + dy, hw, hh));
        return list;
    }

    /// <summary>
    /// İç boşluk şeklini dener: boşluk her yönde (köşegen dahil) en az bir etkin hücre halkasıyla çevrili, tahta kutusunun
    /// kenarına değmez, mekanik hücre oyulmaz, tahta 4-bağlı kalır. Mekanik çakışırsa boşluk 1 hücre kaydırılarak denenir.
    /// </summary>
    static bool TryHollow(LevelData d, int kind, bool flip, out string name)
    {
        name = null;
        var offsets = new[] { (0, 0), (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, 1), (1, -1), (-1, 1) };
        foreach (var (dx, dy) in offsets)
        {
            var rects = HollowRects(kind, d.Width, d.Height, flip ? -dx : dx, dy);
            if (rects.Count == 0) return false;
            var cut = new HashSet<(int, int)>();
            bool ok = true;
            foreach (var (x0, y0, rw, rh) in rects)
            {
                if (x0 < 1 || y0 < 1 || x0 + rw > d.Width - 1 || y0 + rh > d.Height - 1) { ok = false; break; }
                for (int y = y0; y < y0 + rh; y++) for (int x = x0; x < x0 + rw; x++) cut.Add((x, y));
            }
            if (!ok) continue;
            var hollow = new HashSet<(int, int)>(cut);
            if (kind == 2)
            {
                // Kesik halka: iki çapraz köşede 3 hücrelik üçgen (ayna: öbür çapraz).
                var corners = flip ? new[] { (d.Width - 1, 0, -1, 1), (0, d.Height - 1, 1, -1) } : new[] { (0, 0, 1, 1), (d.Width - 1, d.Height - 1, -1, -1) };
                foreach (var (cx, cy, sx, sy) in corners) { cut.Add((cx, cy)); cut.Add((cx + sx, cy)); cut.Add((cx, cy + sy)); }
            }
            if (d.Cells.Any(c => cut.Contains((c.X, c.Y)) && IsMechanic(c))) continue;
            var trial = Clone(d);
            trial.Cells.RemoveAll(c => cut.Contains((c.X, c.Y)));
            var at = trial.Cells.ToDictionary(c => (c.X, c.Y));
            // Halka: boşluğun 8 komşusu tahtada, etkin (kapalı geçit değil) ve kendisi boşluk değil.
            foreach (var (x0, y0, rw, rh) in rects)
                for (int y = y0 - 1; y <= y0 + rh; y++)
                for (int x = x0 - 1; x <= x0 + rw; x++)
                {
                    if (hollow.Contains((x, y))) continue;
                    if (!at.TryGetValue((x, y), out var rc) || !rc.Active || rc.GateId > 0) ok = false;
                }
            if (!ok) continue;
            if (trial.Cells.Count < d.Width * d.Height * 0.62 || !Connected(trial)) continue;
            bool rowsOk = Enumerable.Range(0, d.Height).All(y => trial.Cells.Any(c => c.Y == y));
            bool colsOk = Enumerable.Range(0, d.Width).All(x => trial.Cells.Any(c => c.X == x));
            if (!rowsOk || !colsOk) continue;
            d.Cells.RemoveAll(c => cut.Contains((c.X, c.Y)));
            name = HollowNames[kind] + " " + string.Join("+", rects.Select(r => $"{r.w}x{r.h}"))
                + (dx != 0 || dy != 0 ? " (kaydırılmış)" : "") + (flip && kind == 2 ? " (ayna)" : "");
            return true;
        }
        return false;
    }

    /// <summary>v7 takvim: 21+ bölümlerin her üçüncüsü (n % 3 == 0; art arda gelmez) iç boşluklu; olmazsa dış hat.</summary>
    static bool WantsHollow(int n) => n >= 21 && n % 3 == 0;

    /// <summary>Bölüme şekil uygular; olmazsa (mekanik çakışması / bağlantı) sıradaki şekil denenir. Döner: şekil adı.</summary>
    static string ApplyShape(LevelData d, int n, int attempt = 0)
    {
        bool flip = (n / 3) % 2 == 1;
        if (WantsHollow(n))
        {
            int hk = (n / 3 + attempt) % HollowNames.Length;
            for (int t = 0; t < HollowNames.Length; t++)
                if (TryHollow(d, (hk + t) % HollowNames.Length, flip, out var hn)) return hn;
        }
        int first = (n * 5 + n / 10 + attempt) % ShapeNames.Length;
        for (int t = 0; t < ShapeNames.Length; t++)
        {
            int kind = (first + t) % ShapeNames.Length;
            var cut = ShapeCut(kind, d.Width, d.Height, flip);
            if (cut.Count == 0) continue;
            if (d.Cells.Any(c => cut.Contains((c.X, c.Y)) && IsMechanic(c))) continue;
            var trial = Clone(d);
            trial.Cells.RemoveAll(c => cut.Contains((c.X, c.Y)));
            if (trial.Cells.Count < d.Width * d.Height * 0.62 || !Connected(trial)) continue;
            // Satır/sütun tamamen boşalmasın (tahta kutusu küçülmesin).
            bool rowsOk = Enumerable.Range(0, d.Height).All(y => trial.Cells.Any(c => c.Y == y));
            bool colsOk = Enumerable.Range(0, d.Width).All(x => trial.Cells.Any(c => c.X == x));
            if (!rowsOk || !colsOk) continue;
            d.Cells.RemoveAll(c => cut.Contains((c.X, c.Y)));
            return ShapeNames[kind] + (flip ? " (ayna)" : "");
        }
        return "dikdörtgen";
    }

    static string V6Tutorial(int n, string old) => n switch
    {
        6 => "Beş ya da daha fazla taşı birden soy: rulo roketi alttaki tepsine gelir!",
        7 => "Tepsideki rokete dokun, sonra bir taşa: o satırı (ya da sütunu) sarar. Hamle harcamaz!",
        9 => "Sekiz ya da daha fazla taş: Altın Rulo tepsine gelir. Bir taşa dokun, o rengin hepsini toplar!",
        12 => "Özel taşları biriktir: tepside dört yer var, istediğin an kullan!",
        _ => old,
    };

    static int CountMoves(IEnumerable<Command> sol) => sol.Count(c => c.Type != CommandType.UseSpecial);

    /// <summary>
    /// v7 bot hedefi: 6–15 v6 ile aynı (HardTarget − 0.05); 16–30 arasında 0.06 daha aşağı iner ve öyle kalır.
    /// Alt sınır 6–99'da 0.15, sonra 0.10 (geç oyunda ~0 kazanma oranı istenmez).
    /// </summary>
    static double V7Target(int n)
    {
        double extra = n <= 15 ? 0 : 0.06 * Math.Min(1.0, (n - 15) / 15.0);
        return Math.Max(n < 100 ? 0.15 : 0.10, HardTarget(n) - 0.05 - extra);
    }

    /// <summary>Hamle limiti payı (ışın çözümünün hamlesi üstüne): erken cömert, geç 2–4; x1–x2 nefes bölümlerinde +1.</summary>
    static int[] SlackRange(int n)
    {
        int[] r = n <= 20 ? new[] { 4, 5, 6 } : n <= 60 ? new[] { 3, 4, 5 } : n <= 300 ? new[] { 3, 4 } : new[] { 2, 3, 4 };
        int m = n % 10;
        if (m == 1 || m == 2) r = r.Select(s => s + 1).ToArray();
        return r;
    }

    static readonly int[] PlanWidths = { 1, 3, 10 };

    /// <summary>
    /// Plan derinliği: bölümü verilen limitte çözen en dar ışın. 0 = açgözlü (genişlik 1) çözer, 1 = ışın 3, 2 = ışın 10,
    /// 3 = yalnız geniş ışın (60) çözer. Yüksek değer, zorluğun hamle kıtlığından değil sıra/plan gereğinden geldiğini gösterir.
    /// </summary>
    static int PlanLevel(LevelData d, int limit)
    {
        var c = Clone(d);
        c.MoveLimit = limit;
        var st = LevelLoader.ToState(c);
        for (int i = 0; i < PlanWidths.Length; i++)
        {
            var s = BeamSolve(st, PlanWidths[i], limit + 16);
            if (s != null && ReplayWins(c, s)) return i;
        }
        return PlanWidths.Length;
    }

    sealed class Cand
    {
        public LevelData D; public List<Command> Sol; public double Gf; public int Limit, Slack, Plan = -1; public double Wr, Score;
    }

    static int Rebuild(string[] args)
    {
        int from = args.Length > 1 ? int.Parse(args[1]) : 6, to = args.Length > 2 ? int.Parse(args[2]) : 1000;
        int games = args.Length > 3 ? int.Parse(args[3]) : 24;
        var srcDir = Path.Combine(Root, Environment.GetEnvironmentVariable("KATKAT_REBUILD_SRC") ?? "levels_src");
        var outDir = Path.Combine(Root, Environment.GetEnvironmentVariable("KATKAT_REBUILD_OUT") ?? "levels_src_v7");
        Directory.CreateDirectory(outDir);
        // Yalnız belirli bölümler: KATKAT_ONLY="7,93,410" (yeniden deneme turu).
        var only = (Environment.GetEnvironmentVariable("KATKAT_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
        var files = Directory.GetFiles(srcDir, "level_*.txt")
            .Select(f => (f, n: int.Parse(Path.GetFileNameWithoutExtension(f).Substring(6), CultureInfo.InvariantCulture)))
            .Where(t => t.n >= from && t.n <= to && (only.Count == 0 || only.Contains(t.n))).OrderBy(t => t.n).ToList();
        int attempt = int.TryParse(Environment.GetEnvironmentVariable("KATKAT_SHAPE_TRY"), out var at) ? at : 0;
        var lines = new string[files.Count];
        int failed = 0, done = 0;
        var started = DateTime.Now;
        Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, i =>
        {
            var (path, n) = files[i];
            var outPath = Path.Combine(outDir, Path.GetFileName(path));
            if (File.Exists(outPath)) { lines[i] = $"{n:000}: var, atlandı"; return; }
            try
            {
                var orig = ParseSource(path);
                if (!orig.Capabilities.Contains("specials"))
                {
                    File.Copy(path, outPath);
                    lines[i] = $"{n:000}: özel taşsız, aynen";
                    return;
                }
                int oldMoves = orig.MoveLimit;
                var rng = new Random(n * 7919 + 61 + attempt * 101);
                var shaped = Clone(orig);
                FillHoles(shaped, rng);
                // Yedek: KATKAT_SHAPE_TRY=k ise k. sıradaki şekil (çözüm bulunamayan bölümler için yeniden deneme).
                string shape = ApplyShape(shaped, n, attempt);
                shaped.Tutorial = V6Tutorial(n, shaped.Tutorial);
                double b = 1 - Math.Exp(-(n - 6) / 350.0);
                double target = V7Target(n);
                double minWr = n < 100 ? 0.12 : 0.08;

                int budget = oldMoves;
                double baseGf = 1.08 + 0.07 * b;
                LevelData Scaled(double gf)
                {
                    var d = Clone(shaped);
                    foreach (var g in d.Goals.Where(g => g.Type == "collectColor"))
                    {
                        int avail = d.Cells.Sum(c => c.Layers.Count(l => l == g.Color) + c.GateLayers.Count(l => l == g.Color));
                        int origAvail = orig.Cells.Sum(c => c.Layers.Count(l => l == g.Color) + c.GateLayers.Count(l => l == g.Color));
                        double share = origAvail > 0 ? (double)g.Amount / origAvail : 0.6;
                        g.Amount = Math.Max(3, Math.Min((int)(avail * 0.95), (int)Math.Round(avail * share * gf)));
                    }
                    return d;
                }
                // Her çözülebilir hedef çarpanında: limit = ışın çözümü + pay (SlackRange); bot oranı ölçülür.
                // Puan = |bot − hedef| (+ ~0 oran cezası); en iyi adaylar arasında plan derinliği yüksek olan tercih edilir.
                int floorLimit = Math.Max(5, (int)Math.Ceiling(oldMoves * 0.6));
                var cands = new List<Cand>();
                int misses = 0;
                foreach (var gf in new[] { 0.7, 0.85, 1.0, 1.15, 1.3, 1.45, 1.6, 1.8, 2.0, 2.3 }.Select(f => f * baseGf))
                {
                    var d = Scaled(gf);
                    if (LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d)).Count > 0) break;
                    d.MoveLimit = budget + 6;
                    var s = BeamSolve(LevelLoader.ToState(d), 60, budget + 16);
                    if (s == null || !ReplayWins(d, s)) { if (cands.Count > 0 && ++misses >= 2) break; continue; }
                    int L = CountMoves(s);
                    double wrMax = 0;
                    // Tercih edilen pay aralığı; bot hâlâ hedefin altındaysa pay +2'şer en çok 10'a açılır (sıfır pay yok).
                    var slacks = SlackRange(n).ToList();
                    for (int si = 0; si < slacks.Count; si++)
                    {
                        int m = Math.Max(L + slacks[si], floorLimit);
                        if (cands.Any(c => c.D == d && c.Limit == m)) continue;
                        double wr = BotRate(Clone(d), m, n, games);
                        wrMax = Math.Max(wrMax, wr);
                        double score = Math.Abs(wr - target) + (wr < 0.05 ? 0.5 : wr < minWr ? 0.08 : 0);
                        cands.Add(new Cand { D = d, Sol = s, Gf = gf, Limit = m, Slack = m - L, Wr = wr, Score = score });
                        if (si == slacks.Count - 1 && wr < target - 0.04 && slacks[si] < 10) slacks.Add(Math.Min(10, slacks[si] + 2));
                    }
                    // Daha büyük hedef yalnız daha zor: en geniş payda bile bot hedefin epey altındaysa dur.
                    if (wrMax < target - 0.12) break;
                }
                if (cands.Count == 0) throw new Exception($"çözüm bulunamadı (şekil {shape}, bütçe {budget})");
                // Plan derinliği ağırlıklı seçim: hedefe yakın (±0.15 puan) adaylardan açgözlünün çözemediği tercih edilir.
                double bestScore = cands.Min(c => c.Score);
                foreach (var c in cands.Where(c => c.Score <= bestScore + 0.15).OrderBy(c => c.Score).Take(6))
                    c.Plan = PlanLevel(c.D, c.Limit);
                var pickC = cands.OrderBy(c => c.Score - (c.Plan >= 0 ? 0.05 * c.Plan : 0)).First();
                if (pickC.Plan < 0) pickC.Plan = PlanLevel(pickC.D, pickC.Limit);
                // ~0 kazanma oranı kabul edilmez (kullanıcı: adil zorluk): başka şekille yeniden denenir (KATKAT_SHAPE_TRY).
                if (pickC.Wr < 0.06) throw new Exception($"bot oranı {pickC.Wr:0.00} çok düşük (şekil {shape})");
                var best = pickC.D; var sol = pickC.Sol;
                best.MoveLimit = pickC.Limit;
                best.Solution.Clear();
                foreach (var c in sol) best.Solution.Add(ToStep(c));
                if (!ReplayWins(best, sol)) throw new Exception("son tekrar kazanmadı");
                int uses = sol.Count(c => c.Type == CommandType.UseSpecial);
                int Lm = CountMoves(sol);
                var ev = new Eval { Info = $"v7: şekil {shape}; çözüm {Lm} hamle + {uses} tepsi / limit {pickC.Limit} (pay +{pickC.Slack}, eski {oldMoves}) hedef×{pickC.Gf:0.00} bot {pickC.Wr:0.00} (hedef {target:0.00}) plan {pickC.Plan}" };
                File.WriteAllText(outPath, ToSource(best, ev), new UTF8Encoding(false));
                lines[i] = $"{n:000}: {ev.Info}";
            }
            catch (Exception e)
            {
                System.Threading.Interlocked.Increment(ref failed);
                lines[i] = $"{n:000}: HATA {e.Message}";
            }
            int k = System.Threading.Interlocked.Increment(ref done);
            if (k % 25 == 0) Console.WriteLine($"... {k}/{files.Count} ({(DateTime.Now - started).TotalMinutes:0.0} dk)");
        });
        foreach (var l in lines) Console.WriteLine(l);
        Console.WriteLine($"rebuild: {files.Count - failed}/{files.Count} tamam, {failed} hata, {(DateTime.Now - started).TotalMinutes:0.0} dk");
        return failed == 0 ? 0 : 1;
    }
}
