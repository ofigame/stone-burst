using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KatKat.Core;
using KatKat.Levels;

// v14 ana bölüm üreticisi (docs/v14/PLAN.md; kullanıcı 2026-11-16: "zincir tamamen kaldırılsın", "full mantık gerektiren",
// "ileri bölümler kolay olmasın lütfen, zorla", "ortası delik tahtaları kaldıralım", "dış hatlar çeşitli şekillerde olabilir").
// Zincir döneminin hattı (gen5 → rebuild → v8 → fairfix → chainfit → families) kullanılmaz; iyi parçaları alınır:
// ışın çözücü (BeamSolve), yumuşak açgözlü bot (BotPlay), dış hat şekilleri (ShapeCut), bölüm türleri (zincirsiz), yıldız eşikleri.
//
// Kullanım (proje kökünden):
//   dotnet run -c Release --project tools/LevelLab -- v14 [ilk] [son]     → levels_src_v14/level_NNN.txt + _olcum/level_NNN.tsv
//   dotnet run -c Release --project tools/LevelLab -- v14install          → levels_src_v14 → levels_src, sonra build (JSON + yıldız)
//   dotnet run -c Release --project tools/LevelLab -- v14report           → docs/v14/ZORLUK.md (ölçüm tablosu) + iç boşluk denetimi
//   dotnet run -c Release --project tools/LevelLab -- v14sha              → Tests/EditMode/Data/levels_v14.sha256
// Ortam: KATKAT_ONLY="7,93" yalnız bu bölümler; KATKAT_FORCE=1 var olanı yeniden üretir. Tohumlar sabit: aynı komut aynı bölümü üretir.
//
// Her bölümde ölçülür (PLAN "Zorluk kabul ölçütleri"): ışın çözümü kazanır ve kayıtlı çözüm motorla doğrulanır; hamle payı
// (sınır − en iyi bulunan çözüm; genişlik 60 çözümü, genişlik 150 ışınla bir eksik hamlede yeniden aranır) bölge bandında;
// rastgele bot (geçerli hamlelerden düzgün rastgele, 200 oyun) tavanın altında; açgözlü botlar (en büyük grup / en çok hedef)
// 31'den sonra nefes dışı bölümlerde kaybeder; mekanik dekor kalmaz (örtü açılır, geçit açılır, döner taban/boya kullanılır).
static partial class Program
{
    // ---- Takvim ------------------------------------------------------------------------------------

    /// <summary>Mekanik takvimi (vitrinden sonra gerçek tanıtım): yetenek adı ve ilk bölüm.</summary>
    static readonly (string mech, int intro)[] V14Mechs =
    {
        ("clip", 21), ("star", 41), ("gravel", 45), ("ice", 61), ("colorLock", 121), ("envelope", 201), ("rotor", 301),
        ("sponge", 401), ("linkedClip", 501), ("gate", 601), ("paint", 701),
    };

    /// <summary>Zincirsiz bölüm türleri ve ilk bölümleri (eski v8 takvimi; chain/aha yok).</summary>
    static readonly (string type, int intro)[] V14Types =
    {
        ("exact", 27), ("reverse", 37), ("protect", 57), ("scarce", 67), ("pure", 77), ("resource", 87), ("order", 127),
    };

    // 2026-11-21 (kullanıcı: "Sadece yeşili iptal et kaldır oyundan"; "lacivert 61'de açılsın"): yeşil yok, beş renk.
    static readonly string[] V14AllColors = { "coral", "blue", "yellow", "purple", "navy" };

    /// <summary>Renk açılış sırası (kullanıcı 2026-11-16: "ilk beyaz ve turkuaz ile başla sonra kırmızı gelsin, sonra mor
    /// gelsin sonra yeşil sonra mavi. Bunu geniş bir bölüme yay tüm taşlar hemen çıkmasın"). Beyaz = inci (yellow),
    /// turkuaz = blue, kırmızı = coral, mor = purple, yeşil = green, mavi = navy (lacivert yıldız).</summary>
    static readonly (string color, int intro)[] V14ColorIntro =
    {
        ("yellow", 1), ("blue", 1), ("coral", 8), ("purple", 21), ("navy", 61),
    };

    /// <summary>Bölüm n'de açılmış renkler (açılış sırasıyla).</summary>
    static List<string> V14UnlockedColors(int n) => V14ColorIntro.Where(t => t.intro <= n).Select(t => t.color).ToList();

    /// <summary>Nefes bölümü: 10'dan sonra her 6. bölüm; usta bölümü olan x0 bölümlerine denk gelmez.</summary>
    static bool V14Breath(int n) => n > 10 && n % 6 == 0 && n % 10 != 0;

    /// <summary>Usta bölümü: her x0 bölümü (tek doğru yola yakın).</summary>
    /// <summary>2026-11-22 süreli bölüm (kullanıcı: "süreli bölüm sıklığı önerdiğin gibi olsun": 40'tan sonra her 15 bölümde bir,
    /// 60–90 sn; hamle sınırsız). Usta bölümüne denk gelmez.</summary>
    static bool V14Timed(int n) => n >= 41 && (n - 41) % 15 == 0 && !V14Master(n);

    static bool V14Master(int n) => n >= 20 && n % 10 == 0; // 2026-11-20: 10. bölüm de kolay ilk on içinde (usta 20'den)

    /// <summary>Hamle payı bandı (PLAN): 6–30 3–5; 31–100 2–3; 101–300 1–2; 301+ 0–1; nefes +4.</summary>
    static (int lo, int hi) V14SlackBand(int n)
    {
        // 2026-11-21 (kullanıcı: "hamle payını biraz arttırabiliriz zor bölümlerde ama bu sefer hamle sayısı artsın ki oyun eğlenceli olsun"):
        // 101–300 ve 301+ birer hamle daha bol.
        var b = n <= 10 ? (4, 5) : n <= 30 ? (3, 5) : n <= 100 ? (2, 3) : n <= 300 ? (2, 3) : (1, 2);
        if (V14Breath(n)) b = (b.Item1 + 4, b.Item2 + 4);
        return b;
    }

    /// <summary>Rastgele bot kazanma tavanı (PLAN): 1–30 %60; 31–100 %25; 101–300 %10; 301+ %3; nefeste sınır yok.</summary>
    static double V14RandomCap(int n) => V14Breath(n) ? 1.0 : n <= 30 ? 0.60 : n <= 100 ? 0.25 : n <= 300 ? 0.10 : 0.03;

    sealed class V14Spec
    {
        public int N, W, H, LMin, LMax, Colors, ColorGoals, BlobMin, BlobMax, LenLo, LenHi, SlackLo, SlackHi, Slack, MaxGoals;
        /// <summary>Katman düzlemi başına en büyük grup (0 = sınırsız): erken küçük tahtada Altın Rulo tek dokunuşta bitirmesin.</summary>
        public int MaxGroup;
        public double Frac, RandomCap;
        public bool Breath, Master, Showcase, GreedyMustLose, NoSpecialsInSolution;
        /// <summary>Vitrin 2/3: çözümün ilk iki adımı zorunlu (roket ya da Altın Rulo doğar → tepsiden kullanılır).</summary>
        public SpecialKind ForcedSpecial;
        /// <summary>Vitrin 5: çözüm döner tabanla başlar (döndürme hedef renginden 3+ grup kurar).</summary>
        public bool ForcedRotate;
        public string Type;
        public readonly List<string> Rules = new List<string>();
        public bool Protect, Scarce, Order, NoSpecials, NeedsSpecials, Exact;
        /// <summary>Günlük bulmaca: rulo açık (taş gücü "Gelgit" uygulanabilsin).</summary>
        public bool Roll;
        /// <summary>Mekanik adları ve sayıları (clip1, clip2, ice1, ice2, lock, env, stars, rotor, sponge, link, gate, paint).</summary>
        public readonly Dictionary<string, int> Mech = new Dictionary<string, int>();
        public string Kind => Showcase ? "vitrin" : Breath ? "nefes" : Master ? "usta" : Type != null ? "tür:" + Type : "normal";
        public int M(string k) => Mech.TryGetValue(k, out var v) ? v : 0;
    }

    /// <summary>Bölüm numarasından tarif (eğri: PLAN "Eğri" tablosu).</summary>
    static V14Spec V14SpecFor(int n)
    {
        var sp = new V14Spec { N = n, Breath = V14Breath(n), Master = V14Master(n), Showcase = n <= 1 }; // 2026-11-17 "ilk 5 bölüm öğreticisini 3 e düşürelim"; 2026-11-20 "İlk bölümde öğretsin sonra … 2 katmanlı sınırlı renkte" → yalnız 1. bölüm
        var rng = new Random(n * 104729 + 14);
        var band = V14SlackBand(n);
        sp.SlackLo = band.lo; sp.SlackHi = band.hi;
        sp.RandomCap = V14RandomCap(n);
        sp.GreedyMustLose = n >= 31 && !sp.Breath;
        sp.MaxGoals = n >= 501 ? 4 : 3;
        sp.BlobMin = 2; sp.BlobMax = 4;
        int m = n % 10;
        // Onluk içinde testere dişi: x1–x3 biraz kolay, x7–x9 biraz zor (zorluk dalgalı artar).
        double saw = m >= 1 && m <= 3 ? -0.04 : m >= 7 ? 0.04 : 0;
        sp.Frac = 0.42 + 0.26 * (1 - Math.Exp(-(n - 6) / 260.0)) + saw;

        if (sp.Showcase)
        {
            // Vitrin: geniş tahta (7×8), 2–3 kat, 4 renk, bol hamle; her bölüm bir özellik grubunu gösterir.
            sp.W = 7; sp.H = 8; sp.LMin = 2; sp.LMax = 3; sp.Colors = 6; sp.ColorGoals = 2; sp.LenLo = 4; sp.LenHi = 10; // 6 renk: öğreticide hepsi
            sp.BlobMin = 2; sp.BlobMax = 4; sp.Frac = 0.30;
            switch (n)
            {
                case 1: sp.LMax = 2; sp.Colors = 3; sp.NoSpecialsInSolution = true; sp.LenLo = 3; sp.LenHi = 7; sp.Frac = 0.24; sp.MaxGroup = 4; break; // dokun-soy ve katlar; 2026-11-20: 3 renk (kalabalık olmasın)
                case 2: sp.ForcedSpecial = SpecialKind.RocketRow; sp.ColorGoals = 2; break;                              // roket ve tepsi
                case 3: sp.ForcedSpecial = SpecialKind.Rainbow; sp.ColorGoals = 1; sp.Mech["stars"] = 2; sp.Mech["clip1"] = 2; sp.Mech["link"] = 2; break; // Altın Rulo, dip yıldızı, ataç, bağlı ataç
            }
            sp.Slack = 4;
            return sp;
        }

        // Boyutlar: 6–7 tek kat 3 renk; 8–14 iki kat; 15–30 üç kat 4 renk 2 görev; 31+ her ~10 bölümde bir boyut.
        // 2026-11-20 (kullanıcı: "tek katmanlı bölümlerle değilde 2 katmanlı sınırlı renkte taşlarla başlayalım"): 2–7 iki kat,
        // renk açılış takvimine göre (8'e kadar 2 renk), tek görev, cömert pay; 5'lik grup (roket) mümkün.
        // 2026-11-20 (kullanıcı: "10 bölüme kadar ise bol patlatmalı kolay bölümler olacak … il 10 bölümde tahta şekillerinide
        // değiştirelim. Çünkü bu 10 bölümde müşterimizi elimizde tutarız"): 2–10 elle çizilmiş şekiller (artı, piksel yuvarlak,
        // basamaklı çanak), iki kat, iri renk lekeleri (sık roket ve Altın Rulo), grup sınırı yok, tek görev, cömert pay.
        if (n <= 10)
        {
            var (ew, eh) = V14EarlySize(n);
            // İlk denemede (lekeler 4–8, grup sınırsız) iki renkli tahta 2 dokunuşta eriyordu; gruplar 6'ya kadar (roket bol),
            // iki renk görevi, çözüm 5–10 hamle.
            sp.W = ew; sp.H = eh; sp.LMin = sp.LMax = 2; sp.Colors = 3; sp.ColorGoals = 2; sp.LenLo = 4; sp.LenHi = 10;
            sp.BlobMin = 3; sp.BlobMax = 6; sp.Frac = 0.6; sp.MaxGroup = 6;
        }
        else if (n <= 14)
        {
            sp.W = n <= 10 ? 6 : 7; sp.H = 7; sp.LMin = sp.LMax = 2; sp.Colors = n <= 10 ? 3 : 4; sp.ColorGoals = n <= 12 ? 1 : 2;
            sp.LenLo = 4; sp.LenHi = 11; sp.BlobMin = 2; sp.BlobMax = 4; sp.MaxGroup = 5;
        }
        else if (n <= 30)
        {
            sp.W = 7; sp.H = n <= 20 ? 7 : 8; sp.LMin = sp.LMax = 3; sp.Colors = 4; sp.ColorGoals = 2; sp.LenLo = 5; sp.LenHi = 14;
        }
        else
        {
            // 31+: 40 görev 3, 51 tahta 8×8, 61 renk 5, 71 kat 3–4, 81 tahta 8×9, 91 kat 4 ağırlıklı, 201 renk 6, 501 görev 4.
            sp.W = n <= 50 ? 7 : 8; sp.H = n <= 80 ? 8 : 9;
            sp.LMin = 3; sp.LMax = n <= 70 ? 3 : 4;
            if (n >= 201) sp.LMin = 4;
            sp.Colors = n <= 60 ? 4 : n <= 200 ? 5 : 6;
            sp.ColorGoals = n <= 40 ? 2 : 3;
            sp.LenLo = n <= 100 ? 6 : n <= 300 ? 7 : 8;
            sp.LenHi = n <= 100 ? 18 : n <= 300 ? 22 : 26;
            // Çeşitlilik: bazı bölümler bir boy küçük tahta (aynı zorluk ölçütleriyle).
            if (n > 90 && rng.NextDouble() < 0.22) { if (rng.Next(2) == 0) sp.W = 7; else sp.H = 8; }
        }

        if (sp.Breath)
        {
            // Nefes: 1–2 kat, büyük tahta, büyük gruplar, bol özel taş, geniş hamle; kısa ve eğlenceli.
            sp.W = n < 31 ? 7 : 8; sp.H = n < 31 ? 8 : n <= 80 ? 8 : 9;
            sp.LMin = sp.LMax = 2; sp.Colors = n < 200 ? 3 : 4; sp.ColorGoals = 2; // 2026-11-22: eşit kat (2)
            sp.BlobMin = 4; sp.BlobMax = 9; sp.LenLo = 3; sp.LenHi = 10; sp.Frac = 0.55;
            sp.Slack = sp.SlackHi;
            return sp;
        }

        // Bölüm türleri: 10'luk döngüde 7 ve 8 (27'den), usta x0.
        if (sp.Master)
        {
            // 2026-11-21: usta bölümü dar hamle değil, +2 hamle payı (kullanıcı: zor bölümlerde hamle artsın).
            sp.Type = "boss"; sp.Exact = false; sp.Rules.Clear();
            sp.SlackLo += 2; sp.SlackHi += 2;
            var combos = new List<string>();
            if (n >= 57) combos.Add("protect");
            if (n >= 77) combos.Add("pure");
            if (n >= 67) combos.Add("scarce");
            if (combos.Count > 0)
            {
                int k = n / 10;
                V14ApplyRule(sp, combos[k % combos.Count]);
                if (n % 100 == 0 && combos.Count > 1)
                {
                    var second = combos[(k + 1) % combos.Count];
                    if ((second == "scarce" && sp.NoSpecials) || (second == "pure" && sp.Scarce)) second = "protect";
                    V14ApplyRule(sp, second);
                }
            }
            // 2026-11-22: usta bölümü artık dar hamle değil (+2 pay); "exact" kural kartı yazılmaz.
            sp.Frac += 0.04;
        }
        else if (n >= 27 && (m == 7 || m == 8))
        {
            string pick = null;
            foreach (var (type, intro) in V14Types) if (intro == n) pick = type;
            if (pick == null)
            {
                var pool = V14Types.Where(t => t.intro <= n).Select(t => t.type).ToList();
                int idx = (n / 10) * 2 + (m == 8 ? 1 : 0);
                pick = pool[idx % pool.Count];
                if (m == 8 && pool.Count > 1)
                {
                    int pidx = (n / 10) * 2;
                    if (pool[pidx % pool.Count] == pick) pick = pool[(idx + 1) % pool.Count];
                }
            }
            sp.Type = pick;
            switch (pick)
            {
                case "exact": sp.Exact = true; sp.Rules.Add("exact"); break;
                case "reverse": sp.Exact = true; sp.GreedyMustLose = true; sp.Rules.Add("exact"); break;
                case "protect": V14ApplyRule(sp, "protect"); if (n >= 300) { sp.Exact = true; sp.Rules.Add("exact"); } break;
                case "scarce": V14ApplyRule(sp, "scarce"); break;
                case "pure": V14ApplyRule(sp, "pure"); if (n >= 200) { sp.Exact = true; sp.Rules.Add("exact"); } break;
                case "resource": sp.Exact = true; sp.NeedsSpecials = true; sp.Rules.Add("exact"); break;
                case "order": sp.Order = true; if (n >= 300) { sp.Exact = true; sp.Rules.Add("exact"); } break;
            }
        }
        // Kesin hamle: sınır = çözüm (1–99'da +1, eski kural kartı metniyle aynı).
        sp.Slack = sp.Exact ? (n < 100 ? 1 : 0) : m >= 1 && m <= 3 ? sp.SlackHi : m >= 7 || m == 9 ? sp.SlackLo : (sp.SlackLo + sp.SlackHi + 1) / 2;
        // Saf plan (özel taş yok): renk 2 eksik (grup bulunabilsin), hedef küçük; kıt renkle birleşmez (tekil hedef taşı özel taşsız toplanamaz).
        if (sp.NoSpecials) { sp.LenHi += 4; sp.Colors = Math.Max(3, sp.Colors - 2); sp.Frac *= 0.8; }

        // Mekanikler (21+): o dönemin en yeni mekaniği çoğunlukla, eskilerden biri ara sıra; 801+ karışık.
        var avail = V14Mechs.Where(t => t.intro <= n).Select(t => t.mech).ToList();
        var chosen = new List<string>();
        if (avail.Count > 0)
        {
            if (n >= 801)
            {
                int k = 1 + rng.Next(2) + (rng.NextDouble() < 0.25 ? 1 : 0);
                foreach (var x in avail.OrderBy(_ => rng.Next()).Take(k)) chosen.Add(x);
            }
            else
            {
                if (rng.NextDouble() < (sp.Master ? 1.0 : 0.8)) chosen.Add(avail[avail.Count - 1]);
                foreach (var x in avail.Take(avail.Count - 1).OrderBy(_ => rng.Next()))
                    if (chosen.Count < 2 && rng.NextDouble() < 0.22) chosen.Add(x);
            }
        }
        // Kilit sırası türü kendi kilitlerini kurar; renk kilidi mekaniğiyle çakışmasın.
        if (sp.Order) chosen.Remove("colorLock");
        foreach (var x in chosen) V14MechAmount(sp, x, rng);
        V14Uniform(sp, rng);
        return sp;
    }

    /// <summary>
    /// 2026-11-22 (kullanıcı: "Katman sayıları tüm hücrelerde aynı olacak … hepsi ya 2 ya 3 katlı"; "seviyeye göre artan zorluk
    /// doğrusu olsun ve artan zevk hissi olsun"): bir bölümde bütün hücreler aynı katlı. 1–30 iki kat; usta üç kat; arada üç
    /// katın payı ilerledikçe artar (%30 → %65). En çok 5 renk (yeşil yok). Süreli bölümde dar hamle kuralı yok.
    /// </summary>
    static void V14Uniform(V14Spec sp, Random rng)
    {
        int n = sp.N;
        int l = n <= 30 ? 2 : sp.Master ? 3 : rng.NextDouble() < 0.30 + 0.35 * Math.Min(1.0, (n - 30) / 500.0) ? 3 : 2;
        sp.LMin = sp.LMax = l;
        sp.Colors = Math.Min(sp.Colors, 5);
        if (V14Timed(n))
        {
            sp.Exact = false;
            sp.Rules.Remove("exact");
            sp.Slack = Math.Max(sp.Slack, sp.SlackHi);
        }
    }

    static void V14ApplyRule(V14Spec sp, string rule)
    {
        switch (rule)
        {
            case "protect": sp.Protect = true; if (!sp.Rules.Contains("protect")) sp.Rules.Insert(0, "protect"); break;
            case "pure": sp.NoSpecials = true; if (!sp.Rules.Contains("noSpecials")) sp.Rules.Insert(0, "noSpecials"); break;
            case "scarce": sp.Scarce = true; if (!sp.Rules.Contains("scarce")) sp.Rules.Insert(0, "scarce"); break;
        }
    }

    /// <summary>Mekanik miktarı (bölüm ilerledikçe biraz artar).</summary>
    static void V14MechAmount(V14Spec sp, string mech, Random rng)
    {
        int n = sp.N;
        int up = n < 300 ? 0 : n < 700 ? 1 : 2;
        switch (mech)
        {
            case "clip": sp.Mech["clip1"] = 2 + rng.Next(2) + up; sp.Mech["clip2"] = n < 100 ? rng.Next(2) : 1 + rng.Next(2); break;
            case "star": sp.Mech["stars"] = 2 + rng.Next(2) + (n > 400 ? 1 : 0); break;
            case "ice": sp.Mech["ice1"] = 2 + rng.Next(2) + up; sp.Mech["ice2"] = n < 150 ? rng.Next(2) : 1 + rng.Next(2); break;
            case "colorLock": sp.Mech["lock"] = 1 + (n > 300 ? rng.Next(2) : 0); break;
            case "envelope": sp.Mech["env"] = 2 + rng.Next(2); break;
            case "rotor": sp.Mech["rotor"] = 2 + rng.Next(2); break;
            case "sponge": sp.Mech["sponge"] = 1 + (n > 600 ? rng.Next(2) : 0); break;
            case "linkedClip": sp.Mech["link"] = 2 + rng.Next(2); break;
            case "gate": sp.Mech["gate"] = 3 + rng.Next(2); break;
            case "paint": sp.Mech["paint"] = 1 + rng.Next(2); break;
            case "gravel": sp.Mech["gravel"] = 4 + rng.Next(3) + up; break; // 2026-11-22 çakıl: bitişik küme
        }
    }

    // ---- Tahta --------------------------------------------------------------------------------------

    static readonly string[] V14ShapeNames = { "kesik köşe", "L", "T", "artı", "merdiven", "kum saati", "U", "çatı", "elmas", "dikdörtgen", "kenar çentikleri", "zikzak", "dalga" };

    /// <summary>
    /// Dış hat: ShapeCut şekilleri, dikdörtgen ya da kenardan ısırık çentikleri. Kabul: kalan hücreler 4-bağlı, her satır ve
    /// sütunda hücre var, alanın en az %62'si, iç boşluk yok (her boş hücre boş hücrelerden geçerek tahta kutusunun dışına ulaşır).
    /// </summary>
    static HashSet<(int, int)> V14Shape(int w, int h, Random rng, out string name)
    {
        for (int tries = 0; tries < 40; tries++)
        {
            int kind = rng.Next(V14ShapeNames.Length);
            bool flip = rng.Next(2) == 0;
            HashSet<(int, int)> cut;
            if (kind <= 8) cut = ShapeCut(kind, w, h, flip);
            else if (kind == 9) cut = new HashSet<(int, int)>();
            else if (kind == 11)
            {
                // 2026-11-22 zikzak: sol ve sağ kenarda birer atlamalı hücre (6. test bölümündeki gibi).
                cut = new HashSet<(int, int)>();
                for (int y = 0; y < h; y++) cut.Add(((y % 2 == 0) == flip ? 0 : w - 1, y));
            }
            else if (kind == 12)
            {
                // 2026-11-22 dalga: üst ve alt sırada atlamalı hücreler.
                cut = new HashSet<(int, int)>();
                for (int x = 0; x < w; x++) cut.Add((x, (x % 2 == 0) == flip ? 0 : h - 1));
            }
            else
            {
                // Kenar çentikleri: 2–4 küçük dikdörtgen ısırık, her biri tahta kenarına değer (ayna simetrik olabilir).
                cut = new HashSet<(int, int)>();
                int bites = 2 + rng.Next(3);
                bool mirror = rng.Next(2) == 0;
                for (int b = 0; b < bites; b++)
                {
                    int side = rng.Next(4), bw = 1 + rng.Next(2), bh = 1 + rng.Next(2);
                    int x0, y0;
                    if (side == 0) { x0 = 1 + rng.Next(Math.Max(1, w - bw - 1)); y0 = 0; }
                    else if (side == 1) { x0 = 1 + rng.Next(Math.Max(1, w - bw - 1)); y0 = h - bh; }
                    else if (side == 2) { x0 = 0; y0 = 1 + rng.Next(Math.Max(1, h - bh - 1)); }
                    else { x0 = w - bw; y0 = 1 + rng.Next(Math.Max(1, h - bh - 1)); }
                    for (int y = y0; y < y0 + bh; y++)
                    for (int x = x0; x < x0 + bw; x++)
                    {
                        cut.Add((x, y));
                        if (mirror) cut.Add((w - 1 - x, y));
                    }
                }
            }
            var cells = new HashSet<(int, int)>();
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (!cut.Contains((x, y))) cells.Add((x, y));
            if (cells.Count < w * h * 0.62) continue;
            if (!Enumerable.Range(0, h).All(y => cells.Any(c => c.Item2 == y)) || !Enumerable.Range(0, w).All(x => cells.Any(c => c.Item1 == x))) continue;
            if (!SetConnected(cells) || V14HasInteriorHole(cells, w, h)) continue;
            name = V14ShapeNames[kind] + (flip && kind <= 8 ? " (ayna)" : "");
            return cells;
        }
        name = "dikdörtgen";
        var all = new HashSet<(int, int)>();
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) all.Add((x, y));
        return all;
    }

    /// <summary>2–10. bölümlerin sabit şekilleri (kullanıcının çizimleri, 2026-11-20): kollar eşit artı, köşeleri piksel piksel
    /// yuvarlak tahta, üstten basamaklı çanak (ve ters çevrilmiş kemer).</summary>
    static readonly (string kind, int w, int h)[] V14EarlyShapes =
    {
        ("artı", 7, 7), ("yuvarlak", 7, 7), ("çanak", 7, 7), ("artı", 8, 8), ("yuvarlak", 8, 8),
        ("çanak", 8, 8), ("kemer", 8, 8), ("artı", 8, 8), ("yuvarlak", 8, 9),
    };

    static (int w, int h) V14EarlySize(int n)
    {
        if (n < 2 || n > 10) return (6, 7);
        var s = V14EarlyShapes[n - 2];
        return (s.w, s.h);
    }

    static HashSet<(int, int)> V14EarlyShape(int n, out string name)
    {
        name = null;
        if (n < 2 || n > 10) return null;
        var (kind, w, h) = V14EarlyShapes[n - 2];
        var cells = new HashSet<(int, int)>();
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool keep;
            switch (kind)
            {
                case "artı":
                {
                    // Kollar eşit: kare tahtada dört köşeden 2×2 kesilir (7×7: kol genişliği 3; 8×8: 4; kol boyu 2).
                    const int cut = 2;
                    keep = !((x < cut || x >= w - cut) && (y < cut || y >= h - cut));
                    break;
                }
                case "yuvarlak":
                {
                    // Piksel daire/elips: hücre merkezi elipsin içindeyse (7×7: 3-5-7-7-7-5-3; 8×8: 4-6-8-8-8-8-6-4).
                    double dx = (x + 0.5 - w / 2.0) / (w / 2.0), dy = (y + 0.5 - h / 2.0) / (h / 2.0);
                    keep = dx * dx + dy * dy <= 1.0;
                    break;
                }
                default:
                {
                    // Çanak: üst kenarda ortadan iki basamaklı çentik (en üst satırda yalnız köşe sütunları, ikinci satırda
                    // ikişer sütun, sonra düz). Kemer: aynısı alt kenarda.
                    int r = kind == "kemer" ? h - 1 - y : y;
                    keep = r >= 2 || x <= r || x >= w - 1 - r;
                    break;
                }
            }
            if (keep) cells.Add((x, y));
        }
        name = $"{kind} {w}×{h}";
        return cells;
    }

    static bool SetConnected(HashSet<(int, int)> set)
    {
        if (set.Count == 0) return false;
        var start = set.First();
        var seen = new HashSet<(int, int)> { start };
        var q = new Queue<(int, int)>();
        q.Enqueue(start);
        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            foreach (var nb in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                if (set.Contains(nb) && seen.Add(nb)) q.Enqueue(nb);
        }
        return seen.Count == set.Count;
    }

    /// <summary>
    /// İç boşluk (PLAN kural 2: "ortası delik yok"): tahtada olmayan bir hücre, tahtada olmayan hücrelerden (4-komşu) geçerek
    /// tahta kutusunun dışına ulaşamıyorsa iç boşluktur. Geçit hücreleri tahtanın parçasıdır (açılınca taş olur).
    /// </summary>
    static bool V14HasInteriorHole(HashSet<(int, int)> board, int w, int h)
    {
        var outside = new HashSet<(int, int)>();
        var q = new Queue<(int, int)>();
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (board.Contains((x, y))) continue;
            if (x == 0 || y == 0 || x == w - 1 || y == h - 1) { outside.Add((x, y)); q.Enqueue((x, y)); }
        }
        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            foreach (var nb in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (nb.Item1 < 0 || nb.Item2 < 0 || nb.Item1 >= w || nb.Item2 >= h) continue;
                if (board.Contains(nb) || !outside.Add(nb)) continue;
                q.Enqueue(nb);
            }
        }
        return board.Count + outside.Count < w * h;
    }

    /// <summary>Bölüm verisinde iç boşluk var mı (geçit hücreleri tahtadan sayılır; açılışta boş etkin hücre de boşluktur).</summary>
    static bool V14LevelHasHole(LevelData d)
    {
        var set = new HashSet<(int, int)>(d.Cells.Where(c => c.GateId > 0 || (c.Active && c.Layers.Count > 0)).Select(c => (c.X, c.Y)));
        return V14HasInteriorHole(set, d.Width, d.Height) || d.Cells.Any(c => c.Active && c.Layers.Count == 0);
    }

    static LevelData V14Board(V14Spec sp, Random rng, List<string> palette, out string shape)
    {
        int w = sp.W, h = sp.H;
        shape = null;
        var cells = sp.Showcase ? null : V14EarlyShape(sp.N, out shape);
        if (cells == null) cells = V14Shape(w, h, rng, out shape);
        var d = new LevelData { SchemaVersion = 2, Id = $"level_{sp.N:000}", Width = w, Height = h };
        var depth = new Dictionary<int, int>();
        foreach (var (x, y) in cells.OrderBy(c => c.Item2).ThenBy(c => c.Item1))
        {
            d.Cells.Add(new CellData { X = x, Y = y, Active = true });
            depth[y * w + x] = rng.Next(sp.LMin, sp.LMax + 1);
        }
        var byIndex = d.Cells.ToDictionary(c => c.Y * w + c.X);
        var colorAt = new Dictionary<(int, int), string>();
        for (int level = 0; level < sp.LMax; level++)
        {
            var open = byIndex.Keys.Where(i => depth[i] > level).OrderBy(_ => rng.Next()).ToList();
            var done = new HashSet<int>();
            foreach (var seed in open)
            {
                if (done.Contains(seed)) continue;
                string col = palette[rng.Next(palette.Count)];
                int size = sp.BlobMin + rng.Next(sp.BlobMax - sp.BlobMin + 1);
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
        if (sp.MaxGroup > 0)
        {
            // Düzlem başına grup sınırı: aynı katmandaki bitişik aynı renkler en çok MaxGroup; fazlası komşularında olmayan
            // renge boyanır (tek katta açılışta özel taş doğmaz; iki katta Altın Rulo ilk dokunuşta doğmaz).
            for (int level = 0; level < sp.LMax; level++)
                for (int guard = 0; guard < 400; guard++)
                {
                    var seen = new HashSet<int>();
                    List<int> big = null;
                    foreach (var i in byIndex.Keys.Where(i => depth[i] > level))
                    {
                        if (seen.Contains(i)) continue;
                        var comp = new List<int> { i };
                        seen.Add(i);
                        for (int q = 0; q < comp.Count; q++)
                            foreach (var nb in Neighbours5(comp[q], w, h))
                                if (byIndex.ContainsKey(nb) && depth[nb] > level && !seen.Contains(nb) && colorAt[(nb, level)] == colorAt[(i, level)]) { seen.Add(nb); comp.Add(nb); }
                        if (comp.Count > sp.MaxGroup) { big = comp; break; }
                    }
                    if (big == null) break;
                    int pick = big[rng.Next(big.Count)];
                    var near = Neighbours5(pick, w, h).Where(nb => byIndex.ContainsKey(nb) && depth[nb] > level).Select(nb => colorAt[(nb, level)]).ToHashSet();
                    var free = palette.Where(p => !near.Contains(p)).ToList();
                    colorAt[(pick, level)] = free.Count > 0 ? free[rng.Next(free.Count)] : palette.Where(p => p != colorAt[(pick, level)]).OrderBy(_ => rng.Next()).First();
                }
        }
        foreach (var kv in byIndex)
            for (int level = 0; level < depth[kv.Key]; level++) kv.Value.Layers.Add(colorAt[(kv.Key, level)]);
        if (!sp.NoSpecials) d.Capabilities.Add("specials");
        return d;
    }

    /// <summary>
    /// Vitrin 2/3: üst katmanda tam <paramref name="size"/> hücrelik (komşusu başka renk) hedef renkli grup ekilir; böylece
    /// ilk dokunuş roket (5–7) ya da Altın Rulo (8+) doğurur.
    /// </summary>
    static bool V14Plant(LevelData d, Random rng, string color, int size, bool row)
    {
        var at = d.Cells.ToDictionary(c => (c.X, c.Y));
        for (int tries = 0; tries < 60; tries++)
        {
            int bw = row ? size : 3, bh = row ? 1 : (size + 2) / 3;
            int x0 = rng.Next(Math.Max(1, d.Width - bw + 1)), y0 = 1 + rng.Next(Math.Max(1, d.Height - bh - 1));
            var region = new List<CellData>();
            for (int y = y0; y < y0 + bh; y++) for (int x = x0; x < x0 + bw; x++) if (at.TryGetValue((x, y), out var c)) region.Add(c);
            if (region.Count < size) continue;
            region = region.Take(size).ToList();
            var set = new HashSet<(int, int)>(region.Select(c => (c.X, c.Y)));
            if (!SetConnected(set)) continue;
            foreach (var c in region) c.Layers[c.Layers.Count - 1] = color;
            var others = d.Cells.Select(c => c.Layers[^1]).Distinct().Where(x => x != color).ToList();
            foreach (var c in region)
                foreach (var nb in new[] { (c.X + 1, c.Y), (c.X - 1, c.Y), (c.X, c.Y + 1), (c.X, c.Y - 1) })
                    if (!set.Contains(nb) && at.TryGetValue(nb, out var o) && o.Layers[^1] == color)
                        o.Layers[o.Layers.Count - 1] = others[rng.Next(others.Count)];
            return true;
        }
        return false;
    }

    /// <summary>Mekanikleri yerleştirir (RandomBoard5 ile aynı yazım; geçit üst kenardan, düğme en az katmanlı yığına).</summary>
    static void V14Mechanics(LevelData d, V14Spec sp, Random rng, List<string> palette, List<string> goalColors)
    {
        int w = d.Width;
        var filled = d.Cells.Where(c => c.Layers.Count > 0).OrderBy(_ => rng.Next()).ToList();
        int at = 0;
        CellData Next() { while (at < filled.Count && (filled[at].Protected || IsMechanic(filled[at]))) at++; return at < filled.Count ? filled[at++] : null; }
        for (int k = 0; k < sp.M("clip2"); k++) { var c = Next(); if (c != null) c.ClipHitsRemaining = 2; }
        for (int k = 0; k < sp.M("clip1"); k++) { var c = Next(); if (c != null) c.ClipHitsRemaining = 1; }
        for (int k = 0; k < sp.M("ice2"); k++) { var c = Next(); if (c != null) c.IceHits = 2; }
        for (int k = 0; k < sp.M("ice1"); k++) { var c = Next(); if (c != null) c.IceHits = 1; }
        for (int k = 0; k < sp.M("lock"); k++)
        {
            var c = Next(); if (c == null) break;
            c.LockColor = palette[rng.Next(palette.Count)];
            c.LockRemaining = 3 + rng.Next(3) + (sp.N > 300 ? 1 : 0);
        }
        for (int k = 0; k < sp.M("env"); k++) { var c = Next(); if (c != null) c.EnvelopeColor = palette[rng.Next(palette.Count)]; }
        for (int k = 0; k < sp.M("stars"); k++) { var c = Next(); if (c != null) c.HasBottomStar = true; }
        for (int k = 0; k < sp.M("sponge"); k++)
        {
            var c = Next(); if (c == null) break;
            foreach (var col in palette.OrderBy(_ => rng.Next()).Take(1 + rng.Next(2))) c.SpongeColors.Add(col);
        }
        if (sp.M("link") > 0)
        {
            int hits = sp.N < 300 ? 1 : 1 + rng.Next(2);
            for (int k = 0; k < sp.M("link"); k++) { var c = Next(); if (c == null) break; c.LinkId = 1; c.LinkHits = hits; }
        }
        for (int k = 0; k < sp.M("rotor"); k++)
        {
            while (at < filled.Count && (filled[at].Layers.Count < 2 || IsMechanic(filled[at]) || filled[at].Protected)) at++;
            if (at >= filled.Count) break;
            var rc = filled[at++];
            rc.HasRotor = true;
            // Döner taban işe yarasın: üst renk komşularda yok (dokunarak soyulamaz), bir alttaki renk komşu hedef
            // grubuna katılır. Döndürme (1 hamle) o rengi üste getirir; özel taş dışında başka yol yok.
            var nbTops = d.Cells.Where(o => o.Active && o.Layers.Count > 0 && Math.Abs(o.X - rc.X) + Math.Abs(o.Y - rc.Y) == 1).Select(o => o.Layers[^1]).ToList();
            var join = goalColors.FirstOrDefault(g => nbTops.Count(t => t == g) >= 2) ?? goalColors.FirstOrDefault(g => nbTops.Contains(g));
            var lone = palette.FirstOrDefault(p => !nbTops.Contains(p) && p != join);
            if (join != null && lone != null) { rc.Layers[rc.Layers.Count - 2] = join; rc.Layers[rc.Layers.Count - 1] = lone; }
        }
        // 2026-11-22 çakıl: rastgele bir hücreden komşulara yayılan bitişik küme (mekaniksiz, korumasız, dolu yığınlar).
        int gravel = sp.M("gravel");
        if (gravel > 0)
        {
            var pool = d.Cells.Where(c => c.Active && c.Layers.Count > 0 && !IsMechanic(c) && !c.Protected).ToList();
            if (pool.Count > gravel * 2)
            {
                var set = new List<CellData> { pool[rng.Next(pool.Count)] };
                set[0].Gravel = true;
                for (int q = 0; q < set.Count && set.Count < gravel; q++)
                    foreach (var nb in pool.Where(o => !o.Gravel && Math.Abs(o.X - set[q].X) + Math.Abs(o.Y - set[q].Y) == 1).OrderBy(_ => rng.Next()).ToList())
                    {
                        if (set.Count >= gravel) break;
                        nb.Gravel = true;
                        set.Add(nb);
                    }
            }
        }
        int gateCells = sp.M("gate");
        if (gateCells > 0)
        {
            // Geçit: tahtanın üst kenarındaki bitişik hücreler (görünür bir "kapı"); kapalıyken kenara açılan çentik gibidir.
            var top = d.Cells.Where(c => c.Y == d.Cells.Min(o => o.Y) && !IsMechanic(c) && !c.Protected).OrderBy(c => Math.Abs(c.X * 2 - (w - 1))).Take(gateCells).ToList();
            foreach (var g in top)
            {
                g.GateId = 1;
                g.GateLayers.AddRange(g.Layers);
                g.Layers.Clear();
                g.Active = false;
            }
            var button = d.Cells.Where(c => c.Active && c.Layers.Count > 0 && !IsMechanic(c) && !c.Protected && c.Y > 1).OrderBy(c => c.Layers.Count).ThenBy(_ => rng.Next()).FirstOrDefault();
            if (button != null) button.ButtonGate = 1;
            else foreach (var g in top) { g.Active = true; g.Layers.AddRange(g.GateLayers); g.GateLayers.Clear(); g.GateId = 0; }
        }
        d.Paint = sp.M("paint");
        if (sp.Protect)
        {
            // Kristal: mekaniksiz yığınlar; önce kenardakiler ve hedef rengi az taşıyanlar (çözüm yolu daha az kesişir).
            int k = sp.N < 200 ? 2 : 3;
            int maxX = d.Cells.Max(c => c.X), maxY = d.Cells.Max(c => c.Y);
            double Prefer(CellData c) => (c.X == 0 || c.Y == 0 || c.X == maxX || c.Y == maxY ? 0 : 2) + c.Layers.Count(l => goalColors.Contains(l)) + rng.NextDouble() * 2.5;
            foreach (var c in d.Cells.Where(c => c.Active && c.Layers.Count > 0 && !IsMechanic(c)).OrderBy(Prefer).Take(k)) c.Protected = true;
        }
        if (sp.Order && goalColors.Count > 0)
        {
            // Kilit sırası: hedef rengi üstte taşıyan iki yığın, başka bir rengin anahtarıyla kilitli.
            var t = goalColors[0];
            var keys = palette.Where(c => c != t).ToList();
            var key = keys[rng.Next(keys.Count)];
            foreach (var c in d.Cells.Where(c => c.Active && c.Layers.Count > 1 && c.Layers[^1] == t && !IsMechanic(c) && !c.Protected).OrderBy(_ => rng.Next()).Take(2))
            { c.LockColor = key; c.LockRemaining = 3 + rng.Next(3); }
        }
        // Kilit üst sınırı (doğrulayıcı): kendi hücresi dışındaki o renk katmanı sayacı karşılamalı.
        foreach (var c in d.Cells.Where(c => c.LockRemaining > 0))
        {
            int others = d.Cells.Where(o => o != c).Sum(o => o.Layers.Count(l => l == c.LockColor) + o.GateLayers.Count(l => l == c.LockColor));
            if (others < c.LockRemaining) c.LockRemaining = Math.Max(1, others - 1);
        }
    }

    // ---- Hedefler ve ölçüm ----------------------------------------------------------------------------

    static void V14Goals(LevelData d, V14Spec sp, double frac, List<string> goalColors, double[] mult, Random rng)
    {
        d.Goals.Clear();
        int tokens = (d.Cells.Any(c => c.HasBottomStar) ? 1 : 0) + (d.Cells.Any(c => !string.IsNullOrEmpty(c.EnvelopeColor)) ? 1 : 0) + (d.Cells.Any(c => c.SpongeColors.Count > 0) ? 1 : 0);
        int colorGoals = Math.Max(1, Math.Min(sp.ColorGoals, sp.MaxGoals - tokens));
        for (int i = 0; i < colorGoals && i < goalColors.Count; i++)
        {
            var color = goalColors[i];
            int avail = LayersOf(d, color, c => c.Protected);
            if (avail < 4) continue;
            int amount = (int)Math.Round(avail * Math.Min(0.95, frac * mult[i]));
            amount = Math.Max(3, Math.Min(amount, (int)(avail * 0.95)));
            if (sp.Protect) amount = Math.Min(amount, avail - 1);
            d.Goals.Add(new GoalData { Type = "collectColor", Color = color, Amount = amount });
        }
        if (sp.Scarce && d.Goals.Count > 0)
        {
            // Renk kıtlığı: hedef rengin neredeyse hepsi gerekir (fazlası başka renge boyanır).
            var g = d.Goals[0];
            int spare = sp.N < 100 ? 3 : sp.N < 300 ? 2 : 1;
            var others = ColorsIn(d).Where(c => c != g.Color).ToList();
            int excess = LayersOf(d, g.Color) - (g.Amount + spare);
            if (excess > 0 && others.Count > 0)
            {
                var spots = new List<(CellData c, int i)>();
                foreach (var c in d.Cells.Where(c => c.Active && !c.Protected && c.LockRemaining == 0))
                    for (int i = 0; i < c.Layers.Count; i++) if (c.Layers[i] == g.Color) spots.Add((c, i));
                foreach (var (c, i) in spots.OrderBy(_ => rng.Next()).Take(excess)) c.Layers[i] = others[rng.Next(others.Count)];
            }
        }
        int stars = d.Cells.Count(c => c.HasBottomStar);
        if (stars > 0) d.Goals.Add(new GoalData { Type = "collectStars", Color = "", Amount = stars });
        int envelopes = d.Cells.Count(c => !string.IsNullOrEmpty(c.EnvelopeColor));
        if (envelopes > 0) d.Goals.Add(new GoalData { Type = "collectToken", Color = "", Kind = "envelope", Amount = envelopes });
        int sponges = d.Cells.Count(c => c.SpongeColors.Count > 0);
        if (sponges > 0) d.Goals.Add(new GoalData { Type = "collectToken", Color = "", Kind = "sponge", Amount = sponges });
        d.TrialType = sp.Type;
        d.Rules.Clear();
        d.Rules.AddRange(sp.Rules);
        d.Capabilities.RemoveAll(c => c != "specials");
        d.Capabilities.AddRange(CapabilitiesOf(d).Where(c => c != "specials"));
    }

    sealed class V14Eval
    {
        public bool Ok; public string Fail = ""; public int FailRank; public LevelData D; public List<Command> Sol;
        public int L, Uses, Limit, Slack, Specials, Rainbows, CoverUse, Rotates, Gates, Paints, LockOpens, Covers;
        public double Rand, Soft = -1, Frac; public bool GreedyBig, GreedyGoal, NeedsSpecialsOk = true;
        public int RandGames;
        /// <summary>Kabul dışı ölçütlerden uzaklık (yedek seçim için; küçük iyi).</summary>
        public double Violation = 1e9;
    }

    /// <summary>Rastgele bot hamleleri: her geçerli grup bir kez (tepside doğan özel taşın yeri dokunulan hücreye bağlı değil),
    /// tepsi kullanımları, döndürme, boya, rulo.</summary>
    static List<Command> V14RandomMoves(LevelState s)
    {
        var list = new List<Command>();
        if (s.IsTerminal) return list;
        if (s.MovesLeft > 0)
            foreach (var comp in GroupFinder.AllComponents(s.Board))
                if (comp.Count >= GroupFinder.MinGroup) list.Add(Command.Tap(s.Board.X(comp[0]), s.Board.Y(comp[0])));
        foreach (var m in LevelSolver.Moves(s))
            if (m.Type != CommandType.Tap || s.Board[m.X, m.Y].HasSpecial) list.Add(m);
        return list;
    }

    static double V14RandomRate(LevelState st, int games, int seed)
    {
        var rng = new Random(seed);
        int wins = 0;
        for (int g = 0; g < games; g++)
        {
            var s = st.Clone();
            for (int guard = 0; guard < 300 && !s.IsTerminal; guard++)
            {
                var moves = V14RandomMoves(s);
                if (moves.Count == 0) break;
                if (!RulesEngine.TryApply(s, moves[rng.Next(moves.Count)]).Accepted) break;
            }
            if (s.Status == GameStatus.Won) wins++;
        }
        return (double)wins / games;
    }

    /// <summary>Vitrin 2/3 zorunlu başlangıcı: ekili gruba dokun (özel taş tepsiye), sonra tepsiden en çok hedef ilerleten hücreye kullan.</summary>
    static List<Command> V14Prefix(LevelState st, V14Spec sp)
    {
        List<Command> best = null; double bh = double.MinValue;
        if (sp.ForcedRotate)
        {
            // Döndürme yeni üst renkle hedef rengi 3+ grup yapmalı (döner taban süs kalmasın).
            for (int i = 0; i < st.Board.Count; i++)
            {
                if (!st.Board[i].IsRotatable) continue;
                var rot = Command.Rotate(st.Board.X(i), st.Board.Y(i));
                var s1 = st.Clone();
                if (!RulesEngine.TryApply(s1, rot).Accepted || s1.IsTerminal) continue;
                var top = s1.Board[i].Top;
                if (!s1.Goals.Any(g => g.Type == GoalType.CollectColor && g.Color == top)) continue;
                var comp = GroupFinder.Component(s1.Board, rot.X, rot.Y);
                if (comp.Count < GroupFinder.MinGroup) continue;
                double h = comp.Count + Heur(s1) * 0.01;
                if (h > bh) { bh = h; best = new List<Command> { rot }; }
            }
            return best;
        }
        foreach (var comp in GroupFinder.AllComponents(st.Board))
        {
            bool rainbow = sp.ForcedSpecial == SpecialKind.Rainbow;
            if (rainbow ? comp.Count < RulesEngine.RainbowGroupSize : comp.Count < RulesEngine.RocketGroupSize || comp.Count >= RulesEngine.RainbowGroupSize) continue;
            var tap = Command.Tap(st.Board.X(comp[0]), st.Board.Y(comp[0]));
            var s1 = st.Clone();
            var r1 = RulesEngine.TryApply(s1, tap);
            if (!r1.Accepted || !r1.SpecialToTray || s1.IsTerminal) continue;
            if (rainbow != (r1.SpecialCreated == SpecialKind.Rainbow)) continue;
            // Hedef renkten bir grup olmalı (öğretici "bu rengi topla" diyebilsin).
            if (!s1.Goals.Any(g => g.Type == GoalType.CollectColor && g.Progress > 0)) continue;
            foreach (var use in LevelSolver.Moves(s1).Where(m => m.Type == CommandType.UseSpecial))
            {
                var s2 = s1.Clone();
                if (!RulesEngine.TryApply(s2, use).Accepted || (s2.IsTerminal && s2.Status != GameStatus.Won)) continue;
                double h = Heur(s2);
                if (h > bh) { bh = h; best = new List<Command> { tap, use }; }
            }
        }
        return best;
    }

    static LevelState V14Apply(LevelState st, IEnumerable<Command> cmds)
    {
        var s = st.Clone();
        foreach (var c in cmds) if (!RulesEngine.TryApply(s, c).Accepted) return null;
        return s;
    }

    /// <summary>
    /// v14 ışın değeri: <see cref="Heur"/> + geçit ve döner taban teşviki. Kapalı geçit hücresi başına −8 (açmak hedef
    /// rengini tahtaya getirir), döndürülmüş (ilk üst rengi değişmiş, katmanı eksilmemiş) taban başına +5. Yalnız aramayı
    /// yönlendirir; kabul yine gerçek kazanca bakar.
    /// </summary>
    static double V14Heur(LevelState s, LevelState start)
    {
        double v = Heur(s);
        if (s.Status == GameStatus.Won) return v;
        var b = s.Board; var b0 = start.Board;
        for (int i = 0; i < b.Count; i++)
        {
            var c = b[i];
            if (c.GateId > 0 && !c.Active) v -= 8;
            if (c.HasRotor && c.Layers.Count >= 2 && c.Layers.Count == b0[i].Layers.Count && c.Top != b0[i].Top) v += 5;
        }
        return v;
    }

    /// <summary>BeamSolve ile aynı; değer V14Heur (geçit/döner taban teşvikli).</summary>
    static List<Command> V14Beam(LevelState start, int width, int maxDepth)
    {
        bool shaped = false;
        for (int i = 0; i < start.Board.Count && !shaped; i++) shaped = start.Board[i].HasRotor || start.Board[i].GateId > 0;
        if (!shaped) return BeamSolve(start, width, maxDepth);
        var beam = new List<(LevelState s, List<Command> path)> { (start.Clone(), new List<Command>()) };
        for (int depth = 0; depth < maxDepth && beam.Count > 0; depth++)
        {
            var next = new List<(LevelState s, List<Command> path, double h)>();
            var seen = new HashSet<string>();
            foreach (var (s, path) in beam)
                foreach (var cmd in LevelSolver.Moves(s))
                {
                    var n = s.Clone();
                    if (!RulesEngine.TryApply(n, cmd).Accepted) continue;
                    var p = new List<Command>(path) { cmd };
                    if (n.Status == GameStatus.Won) return p;
                    if (n.IsTerminal || !seen.Add(StateKey(n))) continue;
                    next.Add((n, p, V14Heur(n, start)));
                }
            beam = next.OrderByDescending(t => t.h).Take(width).Select(t => (t.s, t.path)).ToList();
        }
        return null;
    }

    /// <summary>Çözümü motorla tekrar edip istatistikleri doldurur; mekanik dekor kaldıysa ya da vitrin kuralı bozulduysa neden döner.</summary>
    static string V14Stats(LevelData d, V14Spec sp, List<Command> sol, V14Eval ev)
    {
        ev.Rotates = ev.Paints = ev.Specials = ev.Rainbows = ev.CoverUse = ev.Gates = ev.LockOpens = 0;
        var probe = Clone(d);
        probe.MoveLimit = 99;
        var replay = LevelLoader.ToState(probe);
        var covered = new HashSet<(int, int)>(d.Cells.Where(Covered).Select(c => (c.X, c.Y)));
        ev.Covers = covered.Count;
        foreach (var c in sol)
        {
            if (c.Type == CommandType.Rotate) ev.Rotates++;
            if (c.Type == CommandType.Paint) ev.Paints++;
            var r = RulesEngine.TryApply(replay, c);
            if (!r.Accepted) return "tekrar reddi";
            if (r.SpecialCreated != SpecialKind.None) { ev.Specials++; if (r.SpecialCreated == SpecialKind.Rainbow) ev.Rainbows++; }
            foreach (var w in r.Waves)
            {
                ev.CoverUse += w.Cells.Count(p => covered.Remove((p.X, p.Y)));
                ev.Gates += w.GateOpens.Count;
                ev.LockOpens += w.LockHits.Count(hh => hh.RemainingAfter == 0);
            }
        }
        if (replay.Status != GameStatus.Won) return "tekrar kazanmadı";
        if (sp.NoSpecialsInSolution && (ev.Specials > 0 || sol.Any(c => c.Type == CommandType.UseSpecial))) return "vitrin1 özel taş";
        // Öğretici (1–3) tüm renkleri gösterir (kullanıcı 2026-11-17): tahtada 6 rengin hepsi bulunmalı.
        if (sp.Showcase && d.Cells.SelectMany(c => c.Layers).Distinct().Count() < Math.Min(sp.Colors, V14AllColors.Length)) return "vitrin renk eksik";
        var mechFail = new List<string>();
        if (ev.Covers > 0 && ev.CoverUse == 0) mechFail.Add("örtü");
        if (d.Cells.Any(c => c.LockRemaining > 0) && ev.LockOpens == 0) mechFail.Add("kilit");
        if (d.Cells.Any(c => c.HasRotor) && ev.Rotates == 0) mechFail.Add("rotor");
        if (d.Cells.Any(c => c.GateId > 0) && ev.Gates == 0) mechFail.Add("gate");
        // Boya bir araçtır (alt çubuk düğmesi), tahta süsü değil: kullanılmasa da kalır (oyuncunun seçeneği).
        return mechFail.Count > 0 ? "mek:" + string.Join("+", mechFail) : null;
    }

    /// <summary>Tek aday: hedef oranı ve payla ölç. Başarısızlık nedeni Fail (beam, kısa, uzun, mek:*, rastgele, açgözlü, tür).</summary>
    static V14Eval V14Measure(LevelData board, V14Spec sp, double frac, int slack, List<string> goalColors, double[] mult, int seed, bool full)
    {
        var ev = new V14Eval { Frac = frac, Slack = slack };
        var d = Clone(board);
        V14Goals(d, sp, frac, goalColors, mult, new Random(seed));
        d.SchemaVersion = 2; // özel taşsız, mekaniksiz tahta kopyası şema 1 yazılır; tür ve kurallar şema 2 ister
        ev.D = d;
        if (d.Goals.Count == 0) { ev.Fail = "hedef yok"; return ev; }
        d.MoveLimit = 99;
        var errors = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
        if (errors.Count > 0) { ev.Fail = "statik: " + errors[0]; return ev; }
        var st = LevelLoader.ToState(d);
        var prefix = new List<Command>();
        if (sp.ForcedSpecial != SpecialKind.None || sp.ForcedRotate)
        {
            prefix = V14Prefix(st, sp);
            if (prefix == null) { ev.Fail = "vitrin başlangıcı yok"; return ev; }
        }
        var afterPrefix = V14Apply(st, prefix);
        List<Command> rest = afterPrefix.Status == GameStatus.Won ? new List<Command>() : V14Beam(afterPrefix, 60, 90);
        if (rest == null) { ev.Fail = "beam"; return ev; }
        var sol = prefix.Concat(rest).ToList();
        int L = CountMoves(sol);
        ev.L = L;
        ev.Uses = sol.Count(c => c.Type == CommandType.UseSpecial);
        ev.Sol = sol;
        if (L < sp.LenLo) { ev.Fail = "kısa"; ev.Violation = 50 + (sp.LenLo - L) * 5; return ev; }
        if (L > sp.LenHi) { ev.Fail = "uzun"; ev.Violation = 50 + (L - sp.LenHi) * 5; return ev; }
        var sf = V14Stats(d, sp, sol, ev);
        if (sf != null) { ev.Fail = sf; ev.Violation = sf.StartsWith("mek:") ? 30 + 10 * sf.Split('+').Length : 40; return ev; }
        int limit = L + slack;
        d.MoveLimit = limit;
        ev.Limit = limit;
        if (!full) { ev.Ok = true; return ev; }

        // Zorluk: rastgele bot (200 oyun) ve açgözlü botlar, hamle sınırında.
        var start = LevelLoader.ToState(d);
        ev.RandGames = 200;
        ev.Rand = V14RandomRate(start, ev.RandGames, sp.N * 9173 + 5);
        ev.GreedyBig = Play(start, Biggest);
        ev.GreedyGoal = Play(start, Greedy);
        if (sp.NeedsSpecials)
        {
            var ns = Clone(d);
            ns.Capabilities.Remove("specials");
            ns.MoveLimit = limit;
            ev.NeedsSpecialsOk = BeamSolve(LevelLoader.ToState(ns), 60, limit + 12) == null;
        }
        double v = 0;
        bool exempt = sp.N <= 10; // 1: öğretici turu kayıtlı çözümü oynatır; 2–10 (2026-11-20) bilerek kolay
        if (!exempt && ev.Rand > sp.RandomCap) v += 10 + (ev.Rand - sp.RandomCap) * 100;
        if (sp.GreedyMustLose && (ev.GreedyBig || ev.GreedyGoal)) v += 8;
        if (!ev.NeedsSpecialsOk) v += 6;
        // 2–10 kolay (2026-11-20): rastgele oynayan bile çoğunlukla kazanmalı.
        bool tooHard = sp.N >= 2 && sp.N <= 10 && ev.Rand < 0.5;
        if (tooHard) v += 10 + (0.5 - ev.Rand) * 100;
        ev.Violation = v;
        if (v > 0)
        {
            ev.Fail = tooHard ? "zor" : !exempt && ev.Rand > sp.RandomCap ? "rastgele" : sp.GreedyMustLose && (ev.GreedyBig || ev.GreedyGoal) ? "açgözlü" : "tür";
            return ev;
        }

        // Sıkıştırma: daha kısa çözüm var mı (genişlik 150, bir eksik hamle sınırıyla; en çok iki tur). Pay en iyi bulunana göre.
        // Sınır küçülünce rastgele ve açgözlü botların kazanması ancak azalır (aynı tohum, aynı hamle dizisi), yeniden ölçülmez.
        for (int round = 0; round < 2 && L > 1; round++)
        {
            var tight = Clone(d);
            tight.MoveLimit = L - 1;
            var ts = V14Apply(LevelLoader.ToState(tight), prefix);
            if (ts == null || ts.IsTerminal) break;
            var shorter = V14Beam(ts, 150, L + 12);
            if (shorter == null) break;
            var cand = prefix.Concat(shorter).ToList();
            var probe = new V14Eval();
            if (V14Stats(d, sp, cand, probe) != null) break; // daha kısa yol mekaniği atlıyorsa eskisi kalır
            sol = cand; L = CountMoves(sol);
            ev.Sol = sol; ev.L = L; ev.Uses = sol.Count(c => c.Type == CommandType.UseSpecial);
            ev.Rotates = probe.Rotates; ev.Paints = probe.Paints; ev.Specials = probe.Specials; ev.Rainbows = probe.Rainbows;
            ev.CoverUse = probe.CoverUse; ev.Gates = probe.Gates; ev.LockOpens = probe.LockOpens;
            limit = L + slack;
            d.MoveLimit = limit;
            ev.Limit = limit;
        }
        // Sıkıştırılmış çözüm bandın altına indiyse bölüm sandığımızdan kısadır: daha büyük hedefle yeniden denenir.
        if (L < sp.LenLo) { ev.Fail = "kısa"; ev.Violation = 50 + (sp.LenLo - L) * 5; return ev; }
        if (sp.Showcase && sp.N >= 4)
        {
            // Vitrin "kolay": plan yapmayan iyi oyuncu yaklaşığı (yumuşak açgözlü bot) yeterince kazanmalı (5. bölüm dar hamle: %15).
            var s0 = LevelLoader.ToState(d);
            var sbr = new Random(sp.N * 31 + 7); int sw = 0;
            for (int g = 0; g < 48; g++) if (BotPlay(s0.Clone(), sbr, 0.35)) sw++;
            ev.Soft = sw / 48.0;
            if (ev.Soft < (sp.N == 5 ? 0.15 : 0.25)) { ev.Fail = "vitrin zor"; ev.Violation = 5; return ev; }
        }
        ev.Ok = true;
        return ev;
    }


    sealed class V14Result
    {
        public LevelData D; public V14Eval Ev; public V14Spec Sp; public string Shape; public int Boards, Evals; public double Seconds; public string Flags = "";
    }

    // KATKAT_SALT: üretilemeyen bölüm için başka tahta tohumu (2026-11-22, bölüm 400).
    static V14Result V14Make(int n) => V14Make(V14SpecFor(n), n * 7919 + 1416 + V14Salt(n), n * 31);
    static int V14Salt(int n) => n == 400 ? 1 : int.TryParse(Environment.GetEnvironmentVariable("KATKAT_SALT"), out var s) ? s : 0;

    /// <summary>Tarif ve tohumla üretim (ana bölüm: <see cref="V14Make(int)"/>; günlük bulmaca kendi tarif ve tohumunu verir).</summary>
    static V14Result V14Make(V14Spec sp, int boardSeed, int measureSeed)
    {
        int n = sp.N;
        var sw = Stopwatch.StartNew();
        var rng = new Random(boardSeed);
        var res = new V14Result { Sp = sp };
        V14Eval best = null; string bestShape = null;
        var mechMiss = new Dictionary<string, int>();
        int maxBoards = sp.Showcase ? 60 : n <= 30 ? 24 : 14;
        double budgetSec = sp.Showcase ? 600 : n <= 100 ? 240 : 420;
        for (int b = 0; b < maxBoards && sw.Elapsed.TotalSeconds < budgetSec; b++)
        {
            res.Boards++;
            // Palet yalnız o bölümde açılmış renklerden seçilir; renk sayısı açılmış renk sayısını aşmaz (V14ColorIntro).
            // Öğretici (vitrin, 1–3) tüm renkleri gösterir (kullanıcı 2026-11-17: "o 3 bölümde tüm taş renkleri de olsun"); sonra açılış sırası.
            var unlocked = sp.Showcase ? V14AllColors.Take(sp.N == 1 ? 3 : V14AllColors.Length).ToList() : V14UnlockedColors(sp.N); // 1. bölüm: açılış sırasının ilk 3 rengi
            var palette = unlocked.OrderBy(_ => rng.Next()).Take(Math.Min(sp.Colors, unlocked.Count)).ToList();
            var goalColors = palette.OrderBy(_ => rng.Next()).ToList();
            var mult = Enumerable.Range(0, 4).Select(_ => 0.9 + rng.NextDouble() * 0.2).ToArray();
            var board = V14Board(sp, rng, palette, out var shape);
            // Bölüm 1 iki renkle (beyaz, turkuaz): soyulan katın altı üstle aynı renk; gruplar birleşip roket doğurmaz (roket 2. bölümde).
            if (sp.NoSpecialsInSolution) foreach (var c in board.Cells) for (int i = 0; i < c.Layers.Count; i++) c.Layers[i] = c.Layers[^1];
            if (sp.ForcedSpecial != SpecialKind.None)
            {
                bool rainbow = sp.ForcedSpecial == SpecialKind.Rainbow;
                if (!V14Plant(board, rng, goalColors[0], rainbow ? 9 : 6, !rainbow && rng.Next(2) == 0)) continue;
            }
            V14Mechanics(board, sp, rng, palette, goalColors);
            if (V14LevelHasHole(board)) continue;
            if (sp.Roll) board.RollEnabled = true; // günlük: taş gücü (rulo) bu düzende kurallara uygun; ölçüm rulo açıkken
            double frac = sp.Frac;
            int slack = sp.Slack;
            for (int step = 0; step < 9; step++)
            {
                res.Evals++;
                var ev = V14Measure(board, sp, frac, slack, goalColors, mult, measureSeed + b, true);
                if (Environment.GetEnvironmentVariable("KATKAT_DEBUG") == "1") Console.WriteLine($"  {n:000} tahta {b} adım {step} oran {frac:0.00} pay {slack}: {(ev.Ok ? "TAMAM" : ev.Fail)} L={ev.L} rastgele {ev.Rand:0.00}");
                if (best == null || (ev.Sol != null && (best.Sol == null || ev.Violation < best.Violation))) { best = ev; bestShape = shape; }
                if (ev.Ok) { res.D = ev.D; res.Ev = ev; res.Shape = shape; res.Seconds = sw.Elapsed.TotalSeconds; return Finish(res); }
                if (ev.Fail == "zor") { if (slack < sp.SlackHi + 3) slack++; else frac *= 0.9; }
                else if (ev.Fail == "beam" || ev.Fail == "vitrin zor") frac *= 0.86;
                else if (ev.Fail == "uzun") frac *= 0.88;
                else if (ev.Fail == "kısa") frac = Math.Min(0.95, frac * 1.15);
                else if (ev.Fail == "rastgele" || ev.Fail == "açgözlü" || ev.Fail == "tür")
                {
                    if (slack > sp.SlackLo && !sp.Exact) slack--;
                    else frac = Math.Min(0.96, frac * 1.1);
                }
                else if (ev.Fail.StartsWith("mek:"))
                {
                    foreach (var mname in ev.Fail.Substring(4).Split('+')) mechMiss[mname] = mechMiss.TryGetValue(mname, out var k) ? k + 1 : 1;
                    break;
                }
                else
                {
                    // Zorunlu döndürme kurulamadıysa (işe yarar taban yok) taban kullanılmamış sayılır; üç tahtada çıkarılır.
                    if (ev.Fail == "vitrin başlangıcı yok" && sp.ForcedRotate && !sp.Showcase) mechMiss["rotor"] = mechMiss.TryGetValue("rotor", out var rk) ? rk + 1 : 1;
                    break;
                }
                if (frac >= 0.955 && (ev.Fail == "kısa" || ev.Fail == "rastgele")) break;
            }
            // Dekor kalan mekanik (döner taban, boya, geçit) üç tahtada kullanılmadıysa bölümden çıkar (yerine sade tahta).
            // Döner taban iki tahtada kullanılmadıysa önce zorunlu başlangıç denenir (çözüm işe yarar bir döndürmeyle açılır);
            // ancak bu da tutmazsa taban çıkarılır.
            if (!sp.ForcedRotate && sp.M("rotor") > 0 && mechMiss.TryGetValue("rotor", out var rm) && rm >= 2)
            { sp.ForcedRotate = true; mechMiss.Remove("rotor"); res.Flags += "döndürmeyle başlar; "; }
            foreach (var kv in mechMiss.Where(kv => kv.Value >= (sp.Showcase ? 30 : 3)).ToList())
            {
                string key = kv.Key == "rotor" ? "rotor" : kv.Key == "paint" ? "paint" : kv.Key == "gate" ? "gate" : null;
                if (key != null && sp.Mech.Remove(key)) res.Flags += $"{key} çıkarıldı; ";
                if (kv.Key == "örtü" && !sp.Showcase)
                    foreach (var ck in new[] { "clip1", "clip2", "ice1", "ice2", "link" }) if (sp.Mech.Remove(ck)) res.Flags += $"{ck} çıkarıldı; ";
                if (kv.Key == "kilit" && !sp.Showcase && sp.Mech.Remove("lock")) res.Flags += "lock çıkarıldı; ";
                mechMiss.Remove(kv.Key);
            }
        }
        // Yedek: kabul ölçütüne en yakın çözülmüş aday (bayrakla raporlanır).
        res.Seconds = sw.Elapsed.TotalSeconds;
        if (best == null || best.Sol == null) return res;
        if (best.Limit == 0) { best.Limit = best.L + best.Slack; }
        res.D = best.D; res.Ev = best; res.Shape = bestShape;
        res.Flags += "YEDEK: " + best.Fail + "; ";
        return Finish(res);

        V14Result Finish(V14Result r)
        {
            var d = r.D; var ev = r.Ev;
            d.MoveLimit = ev.Limit;
            d.Solution.Clear();
            foreach (var c in ev.Sol) d.Solution.Add(ToStep(c));
            if (!ReplayWins(d, ev.Sol)) { r.D = null; r.Flags += "son tekrar kazanmadı"; return r; }
            if (ev.RandGames == 0)
            {
                var st0 = LevelLoader.ToState(d);
                ev.RandGames = 200; ev.Rand = V14RandomRate(st0, 200, sp.N * 9173 + 5);
                ev.GreedyBig = Play(st0, Biggest); ev.GreedyGoal = Play(st0, Greedy);
            }
            // Bilgi: yumuşak açgözlü bot (plan yapmayan "iyi oyuncu" yaklaşığı), 48 oyun.
            var st = LevelLoader.ToState(d);
            var br = new Random(sp.N * 31 + 7);
            int w = 0;
            for (int g = 0; g < 48; g++) if (BotPlay(st.Clone(), br, 0.35)) w++;
            ev.Soft = w / 48.0;
            r.Seconds = sw.Elapsed.TotalSeconds;
            return r;
        }
    }

    static string V14MetricsHeader => "n\ttür\tW\tH\thücre\tkat\trenk\thedef\thedefler\tmekanik\tçözüm\ttepsi\tsınır\tpay\trastgele\taçgözlü_büyük\taçgözlü_hedef\tyumuşak\tözel\taltın\ttahta\tdeneme\tsn\tşekil\tbayrak";

    static string V14MetricsLine(V14Result r)
    {
        var d = r.D; var ev = r.Ev; var sp = r.Sp;
        var inv = CultureInfo.InvariantCulture;
        int layersMin = d.Cells.Where(c => c.Active || c.GateId > 0).Min(c => Math.Max(c.Layers.Count, c.GateLayers.Count));
        int layersMax = d.Cells.Max(c => Math.Max(c.Layers.Count, c.GateLayers.Count));
        string goals = string.Join(",", d.Goals.Select(g => g.Type == "collectColor" ? $"{g.Color} {g.Amount}" : g.Type == "collectStars" ? $"star {g.Amount}" : $"{g.Kind} {g.Amount}"));
        var mech = CapabilitiesOf(d).Where(c => c != "specials").ToList();
        if (d.Cells.Any(c => c.Protected)) mech.Add("crystal");
        return string.Join("\t", new[]
        {
            sp.N.ToString(inv), sp.Kind, d.Width.ToString(inv), d.Height.ToString(inv), d.Cells.Count.ToString(inv), $"{layersMin}-{layersMax}",
            ColorsIn(d).Count.ToString(inv), d.Goals.Count.ToString(inv), goals, mech.Count == 0 ? "-" : string.Join(",", mech),
            ev.L.ToString(inv), ev.Uses.ToString(inv), d.MoveLimit.ToString(inv), (d.MoveLimit - ev.L).ToString(inv),
            ev.Rand.ToString("0.000", inv), ev.GreedyBig ? "kazanır" : "kaybeder", ev.GreedyGoal ? "kazanır" : "kaybeder", ev.Soft.ToString("0.00", inv),
            ev.Specials.ToString(inv), ev.Rainbows.ToString(inv), r.Boards.ToString(inv), r.Evals.ToString(inv), r.Seconds.ToString("0", inv), r.Shape ?? "", r.Flags.Trim(),
        });
    }

    static string V14OutDir => Path.Combine(Root, "levels_src_v14");

    static int V14(string[] args)
    {
        int from = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 1, to = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 1000;
        var only = (Environment.GetEnvironmentVariable("KATKAT_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet();
        bool force = Environment.GetEnvironmentVariable("KATKAT_FORCE") == "1";
        var outDir = V14OutDir;
        var mDir = Path.Combine(outDir, "_olcum");
        Directory.CreateDirectory(mDir);
        // KATKAT_MECH=rotor: yalnız tarifinde bu mekanik anahtarı olan bölümler (mekanik kuralı değişince yeniden üretim).
        var mechOnly = Environment.GetEnvironmentVariable("KATKAT_MECH");
        var todo = Enumerable.Range(from, to - from + 1).Where(n => only.Count == 0 || only.Contains(n))
            .Where(n => string.IsNullOrEmpty(mechOnly) || V14SpecFor(n).M(mechOnly) > 0)
            .Where(n => force || !File.Exists(Path.Combine(outDir, $"level_{n:000}.txt"))).ToList();
        // Yük dengesi: büyük (geç) bölümler önce başlar.
        todo = todo.OrderByDescending(n => n).ToList();
        Console.WriteLine($"v14: {todo.Count} bölüm üretilecek ({from}–{to}), {Math.Max(1, Environment.ProcessorCount - 1)} iş parçacığı");
        var started = Stopwatch.StartNew();
        int done = 0, fallback = 0, failed = 0;
        Parallel.ForEach(todo, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, n =>
        {
            V14Result r;
            try { r = V14Make(n); }
            catch (Exception e) { r = new V14Result { Sp = V14SpecFor(n), Flags = "HATA " + e.Message }; }
            if (r.D == null)
            {
                Interlocked.Increment(ref failed);
                Console.WriteLine($"{n:000}: BAŞARISIZ {r.Flags} ({r.Seconds:0} sn)");
                return;
            }
            if (r.Flags.Contains("YEDEK")) Interlocked.Increment(ref fallback);
            if (V14Timed(n)) r.D.TimeLimit = Math.Clamp(20 + 4 * r.Ev.L, 60, 90); // 2026-11-22 süreli: çözüm uzunluğuna göre 60–90 sn
            string line = V14MetricsLine(r);
            var info = $"v14 {r.Sp.Kind}; şekil {r.Shape}; çözüm {r.Ev.L} + {r.Ev.Uses} tepsi / sınır {r.D.MoveLimit}; rastgele {r.Ev.Rand:0.000}; açgözlü {(r.Ev.GreedyBig || r.Ev.GreedyGoal ? "kazanır" : "kaybeder")}; yumuşak {r.Ev.Soft:0.00}{(r.Flags.Length > 0 ? "; " + r.Flags.Trim() : "")}";
            File.WriteAllText(Path.Combine(outDir, $"level_{n:000}.txt"), ToSource(r.D, new Eval { Info = info }), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(mDir, $"level_{n:000}.tsv"), line + "\n", new UTF8Encoding(false));
            int k = Interlocked.Increment(ref done);
            double el = started.Elapsed.TotalMinutes;
            Console.WriteLine($"{n:000} [{k}/{todo.Count} {el:0.0} dk, kalan ~{el / k * (todo.Count - k):0} dk] {r.Sp.Kind} {r.D.Width}x{r.D.Height} çözüm {r.Ev.L}/{r.D.MoveLimit} rastgele {r.Ev.Rand:0.000} açgözlü {(r.Ev.GreedyBig ? "B" : "-")}{(r.Ev.GreedyGoal ? "H" : "-")} {r.Seconds:0} sn {r.Flags}");
        });
        Console.WriteLine($"v14: {done} bölüm, {fallback} yedek, {failed} başarısız, {started.Elapsed.TotalMinutes:0.0} dk");
        return failed == 0 ? 0 : 1;
    }

    /// <summary>levels_src_v14 → levels_src (kaynaklar), sonra build: Resources/Levels JSON + yıldız eşikleri.</summary>
    static int V14Install(string[] args)
    {
        var files = Directory.GetFiles(V14OutDir, "level_*.txt").OrderBy(f => f, StringComparer.Ordinal).ToList();
        if (files.Count != 1000) { Console.Error.WriteLine($"v14install: {files.Count} kaynak (1000 bekleniyor)"); return 1; }
        if (Directory.Exists(VrSourceDir) && Directory.GetFiles(VrSourceDir, "*.json").Length > 0)
        { Console.Error.WriteLine("v14install: levels_vr1 hâlâ dolu; Build onu metin kaynağının üstüne kopyalar. Önce arşivle."); return 1; }
        var src = Path.Combine(Root, "levels_src");
        foreach (var f in files) File.Copy(f, Path.Combine(src, Path.GetFileName(f)), true);
        var sources = Directory.GetFiles(src, "level_*.txt").OrderBy(f => f, StringComparer.Ordinal).ToList();
        Console.WriteLine($"v14install: {files.Count} kaynak → levels_src");
        return Build(sources);
    }

    /// <summary>Üretilmiş JSON bölümlerin denetimi ve docs/v14/ZORLUK.md (bölge tablosu, örnekler).</summary>
    static int V14Report(string[] args)
    {
        var dir = Path.Combine(Root, "Assets", "KatKat", "Resources", "Levels");
        var mDir = Path.Combine(V14OutDir, "_olcum");
        var rows = new List<string[]>();
        for (int n = 1; n <= 1000; n++)
        {
            var p = Path.Combine(mDir, $"level_{n:000}.tsv");
            if (File.Exists(p)) rows.Add(File.ReadAllText(p).TrimEnd('\n', '\r').Split('\t'));
        }
        // Denetim: statik, kayıtlı çözüm, iç boşluk, zincir verisi yok, yıldız eşiği var.
        int bad = 0;
        var problems = new List<string>();
        foreach (var f in Directory.GetFiles(dir, "level_*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            var d = LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f));
            var errs = LevelValidator.ValidateStatic(d, LevelValidator.ScopeFor(d));
            var rep = LevelValidator.ReplaySolution(d);
            bool hole = V14LevelHasHole(d);
            if (errs.Count > 0 || !rep.Success || hole || d.StarThresholds == null)
            {
                bad++;
                problems.Add($"{d.Id}: {(errs.Count > 0 ? errs[0] : "")} {(rep.Success ? "" : rep.Error)} {(hole ? "iç boşluk" : "")} {(d.StarThresholds == null ? "yıldız eşiği yok" : "")}");
            }
        }
        foreach (var p in problems.Take(30)) Console.WriteLine("SORUN " + p);
        Console.WriteLine($"v14report: {rows.Count} ölçüm satırı, {bad} sorunlu bölüm");

        var inv = CultureInfo.InvariantCulture;
        int I(string[] r, int i) => int.Parse(r[i], inv);
        double Dd(string[] r, int i) => double.Parse(r[i], inv);
        var bands = new (int lo, int hi)[] { (1, 5), (6, 7), (8, 14), (15, 30), (31, 60), (61, 100), (101, 200), (201, 300), (301, 500), (501, 700), (701, 800), (801, 1000) };
        var md = new StringBuilder();
        md.AppendLine("# v14 zorluk tablosu (LevelLab `v14`, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", inv) + ")");
        md.AppendLine();
        md.AppendLine("Kaynak: `docs/v14/PLAN.md`. Bütün sayılar üreticinin ölçümüdür (`levels_src_v14/_olcum/*.tsv`), tahmin değildir.");
        md.AppendLine();
        md.AppendLine("- **Çözüm**: ışın çözücü (genişlik 60) + bir eksik hamle sınırıyla genişlik 150 ışınla sıkıştırma; kayıtlı çözüm gerçek motorla kazanır.");
        md.AppendLine("- **Pay** = hamle sınırı − en iyi bulunan çözüm (tepsi kullanımı hamle saymaz).");
        md.AppendLine("- **Rastgele bot**: her geçerli hamleden (grup başına bir dokunuş, tepsi, döndürme, boya) düzgün rastgele seçer; 200 oyun, sabit tohum.");
        md.AppendLine("- **Açgözlü**: (B) en çok katman soyan hamle, (H) en çok hedef ilerleten hamle; ikisi de sınırda oynar. \"Kaybeder\" = ikisi de kaybeder.");
        md.AppendLine("- **Yumuşak bot**: bir adım sonrası hedef değerine göre sıcaklıklı seçim (plan yapmayan iyi oyuncu yaklaşığı), 48 oyun. Bilgi amaçlı.");
        md.AppendLine("- Nefes: 10'dan sonra her 6. bölüm (x0 hariç). Usta: her x0 bölümü (tür `boss`, kesin hamle).");
        md.AppendLine();
        md.AppendLine($"Denetim: {bad} sorunlu bölüm (statik doğrulama, kayıtlı çözüm, iç boşluk, yıldız eşiği).");
        md.AppendLine();
        md.AppendLine("## Bölge tablosu");
        md.AppendLine();
        md.AppendLine("| Bölüm | Tahta | Kat | Renk | Görev | Çözüm (ort.) | Pay (nefes dışı) | Pay (nefes) | Rastgele bot ort. / en çok (nefes dışı) | Açgözlü kazanır (nefes dışı) | Yumuşak bot ort. | Nefes | Usta | Tür | Yedek |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var (lo, hi) in bands)
        {
            var rs = rows.Where(r => I(r, 0) >= lo && I(r, 0) <= hi).ToList();
            if (rs.Count == 0) continue;
            var nb = rs.Where(r => r[1] != "nefes").ToList();
            var br = rs.Where(r => r[1] == "nefes").ToList();
            string Range(IEnumerable<int> v) { var l = v.ToList(); return l.Count == 0 ? "—" : l.Min() == l.Max() ? $"{l.Min()}" : $"{l.Min()}–{l.Max()}"; }
            string boards = string.Join(", ", rs.GroupBy(r => r[2] + "×" + r[3]).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} ({g.Count()})"));
            string layers = string.Join(", ", rs.GroupBy(r => r[5]).OrderByDescending(g => g.Count()).Take(3).Select(g => g.Key));
            int greedyWins = nb.Count(r => r[15] == "kazanır" || r[16] == "kazanır");
            md.AppendLine($"| {lo}–{hi} | {boards} | {layers} | {Range(rs.Select(r => I(r, 6)))} | {Range(rs.Select(r => I(r, 7)))} | {rs.Average(r => I(r, 10)):0.0} | {Range(nb.Select(r => I(r, 13)))} | {Range(br.Select(r => I(r, 13)))} | "
                + $"{(nb.Count == 0 ? "—" : $"%{nb.Average(r => Dd(r, 14)) * 100:0.0} / %{nb.Max(r => Dd(r, 14)) * 100:0.0}")} | {greedyWins}/{nb.Count} | %{rs.Average(r => Dd(r, 17)) * 100:0} | {br.Count} | {rs.Count(r => r[1] == "usta")} | {rs.Count(r => r[1].StartsWith("tür:"))} | {rs.Count(r => r.Length > 24 && r[24].Contains("YEDEK"))} |");
        }
        md.AppendLine();
        md.AppendLine("## PLAN ölçütleri (sayım)");
        md.AppendLine();
        int SlackBad(string[] r)
        {
            int n = I(r, 0), s = I(r, 13);
            var (blo, bhi) = V14SlackBand(n);
            bool exact = r[1] == "usta" || r[1] == "tür:exact" || r[1] == "tür:reverse" || r[1] == "tür:resource" || n == 5
                || (n >= 300 && (r[1] == "tür:protect" || r[1] == "tür:order")) || (n >= 200 && r[1] == "tür:pure");
            if (exact) return s <= (n < 100 ? 1 : 0) ? 0 : 1;
            return s >= blo && s <= bhi ? 0 : 1;
        }
        int randBad = rows.Count(r => I(r, 0) > 3 && Dd(r, 14) > V14RandomCap(I(r, 0)));
        int greedyBad = rows.Count(r => I(r, 0) >= 31 && r[1] != "nefes" && (r[15] == "kazanır" || r[16] == "kazanır"));
        md.AppendLine($"- Pay bandı dışında: {rows.Sum(SlackBad)} bölüm (kesin hamle türleri ve usta bölümleri: pay 1–99'da ≤ 1, sonra 0).");
        md.AppendLine($"- Rastgele bot tavanı aşan: {randBad} bölüm (öğretici turlu 1–3 hariç).");
        md.AppendLine($"- 31+ nefes dışı açgözlü botun kazandığı: {greedyBad} bölüm.");
        md.AppendLine($"- Yedek aday (bir ölçüt tutmadı, en yakın aday): {rows.Count(r => r.Length > 24 && r[24].Contains("YEDEK"))} bölüm.");
        md.AppendLine();
        md.AppendLine("## Örnek bölümler");
        md.AppendLine();
        md.AppendLine("| Bölüm | Tür | Tahta | Kat | Renk | Hedefler | Mekanik | Çözüm / sınır | Rastgele | Açgözlü | Şekil |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (int n in new[] { 1, 2, 3, 4, 5, 6, 9, 15, 18, 21, 27, 40, 41, 57, 66, 100, 121, 127, 201, 246, 301, 401, 500, 501, 601, 701, 801, 900, 999, 1000 })
        {
            var r = rows.FirstOrDefault(x => I(x, 0) == n);
            if (r == null) continue;
            md.AppendLine($"| {n} | {r[1]} | {r[2]}×{r[3]} | {r[5]} | {r[6]} | {r[8]} | {r[9]} | {r[10]}{(I(r, 11) > 0 ? $"+{r[11]}t" : "")} / {r[12]} | %{Dd(r, 14) * 100:0.0} | {(r[15] == "kazanır" || r[16] == "kazanır" ? "kazanır" : "kaybeder")} | {r[23]} |");
        }
        var flagged = rows.Where(r => r.Length > 24 && r[24].Length > 0).ToList();
        if (flagged.Count > 0)
        {
            md.AppendLine();
            md.AppendLine("## Bayraklı bölümler");
            md.AppendLine();
            foreach (var r in flagged) md.AppendLine($"- {r[0]} ({r[1]}): {r[24]}");
        }
        Directory.CreateDirectory(Path.Combine(Root, "docs", "v14"));
        File.WriteAllText(Path.Combine(Root, "docs", "v14", "ZORLUK.md"), md.ToString(), new UTF8Encoding(false));
        Console.WriteLine("→ docs/v14/ZORLUK.md");
        return bad == 0 ? 0 : 1;
    }

    /// <summary>Bölüm sağlama toplamı (Tests/EditMode/Data/levels_v14.sha256): kullanıcı 1000 bölümün yenilenmesini istedi (2026-11-16).</summary>
    static int V14Sha(string[] args)
    {
        var dir = Path.Combine(Root, "Assets", "KatKat", "Resources", "Levels");
        var files = Directory.GetFiles(dir, "level_*.json").OrderBy(f => LevelValidator.LevelNumber(Path.GetFileNameWithoutExtension(f))).ToList();
        var sb = new StringBuilder();
        sb.Append("# Stone Burst üretim bölümleri v14 (zincirsiz, plan gerektiren 1000 bölüm; kullanıcı 2026-11-16: \"tüm bölümleri gözden geçirip istediğim her şeyi uygulayalım\", 1000 bölüm yeniden üretildi): SHA-256 özetleri, bayt bayt.\n");
        sb.Append("# Üretim: tools/LevelLab v14 (docs/v14/PLAN.md, docs/v14/ZORLUK.md). Önceki liste (vr1 tahta aileleri, 2026-11-11): arşiv KatKat_Arsiv_2026-11-06/Assets/KatKat/Tests/EditMode/Data/levels_v14_vr1_2026-11-11.sha256.\n");
        sb.Append("# Bilinçli güncelleme yalnız kullanıcı onayıyla: bu dosya yeniden üretilir (LevelLab v14sha).\n");
        using (var sha = System.Security.Cryptography.SHA256.Create())
            foreach (var f in files)
            {
                byte[] h;
                using (var s = File.OpenRead(f)) h = sha.ComputeHash(s);
                sb.Append(string.Concat(h.Select(b => b.ToString("x2")))).Append("  ").Append(Path.GetFileName(f)).Append('\n');
            }
        var outPath = Path.Combine(Root, "Assets", "KatKat", "Tests", "EditMode", "Data", "levels_v14.sha256");
        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"v14sha: {files.Count} bölüm → {outPath}");
        return 0;
    }
}
