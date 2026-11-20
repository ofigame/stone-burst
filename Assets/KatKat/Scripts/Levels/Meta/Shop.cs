using System;
using System.Collections.Generic;

namespace KatKat.Meta
{
    /// <summary>Külçeyle alınan güçlendiriciler (2026-11-20; kullanıcı: "+3 Hamle", "Taş Değiştir", "Bomba").</summary>
    public enum Booster { Moves = 0, Recolor = 1, Bomb = 2 }

    /// <summary>
    /// Mağaza ve güçlendirici değerleri (kullanıcı kararları 2026-11-20): külçe yalnız koleksiyon taşlarından hediye ve
    /// (ileride) uygulama içi satın almayla gelir; tahta temizleme ödülü yok. Güçlendirici fiyatları "tanınmayan oyun için"
    /// düşük tutuldu. Gerçek para paketleri mağaza hesapları hazır olunca açılır (<see cref="IapEnabled"/>).
    /// </summary>
    public static class ShopCatalog
    {
        /// <summary>Uygulama içi satın alma (gerçek para) açık mı. Banka/vergi kurulumu bitene kadar kapalı: paketler
        /// mağazada "Yakında" görünür.</summary>
        public const bool IapEnabled = false;

        public const int MovesAmount = 3;
        public const int RecolorStones = 3;
        /// <summary>Güçlendirici ilk açıldığında denemek için bedava verilen adet.</summary>
        public const int TrialCount = 2;

        public static readonly Booster[] All = { Booster.Moves, Booster.Recolor, Booster.Bomb };

        /// <summary>Tek adet fiyatı (külçe).</summary>
        public static int Price(Booster b) => b == Booster.Moves ? 50 : b == Booster.Recolor ? 30 : 60;

        /// <summary>3'lü paket fiyatı (külçe, indirimli).</summary>
        public static int Price3(Booster b) => b == Booster.Moves ? 130 : b == Booster.Recolor ? 75 : 150;

        /// <summary>Açıldığı bölüm (1 tabanlı; bu bölüm başlarken kullanılabilir). İlk koleksiyon taşı 10. bölümde 150 külçe verir.</summary>
        public static int UnlockLevel(Booster b) => b == Booster.Moves ? 11 : b == Booster.Recolor ? 16 : 25;

        public static string Key(Booster b) => b == Booster.Moves ? "moves" : b == Booster.Recolor ? "recolor" : "bomb";

        /// <summary>Gerçek para paketleri (fiyat metni mağazadan gelecek; burada yalnız içerik ve kimlik).</summary>
        public static readonly IngotPack[] Packs =
        {
            new IngotPack("ingots_120", 120, 0, "pack1"),
            new IngotPack("ingots_400", 400, 20, "pack2"),
            new IngotPack("ingots_750", 750, 25, "pack3"),
            new IngotPack("ingots_1700", 1700, 40, "pack4"),
        };

        /// <summary>
        /// Can teklifleri (kullanıcı 2026-11-20: "mağazada can da satın alınsın … bu tarz oyunlarda nasıl oluyor araştır"). Türdeki
        /// yaygın model: canlar 30 dk'da bir dolar; oyuncu premium parayla bütün canları bir anda doldurur ya da süreli "sınırsız
        /// can" alır (süre kısaltma yaygın değil). Fiyatlar güçlendiricilere göre: doldurma ucuz, süreli sınırsız daha değerli.
        /// </summary>
        public static readonly HeartOffer[] HeartOffers = { HeartOffer.Refill, HeartOffer.Unlimited1h, HeartOffer.Unlimited3h };

        public static int HeartPrice(HeartOffer o) => o == HeartOffer.Refill ? 40 : o == HeartOffer.Unlimited1h ? 90 : 220;

        /// <summary>Sınırsız can süresi (sn); doldurmada 0.</summary>
        public static int HeartSeconds(HeartOffer o) => o == HeartOffer.Unlimited1h ? 3600 : o == HeartOffer.Unlimited3h ? 3 * 3600 : 0;

        public static string HeartKey(HeartOffer o) => o == HeartOffer.Refill ? "refill" : o == HeartOffer.Unlimited1h ? "inf1h" : "inf3h";

        public const string StarterId = "starter_pack", NoAdsId = "no_ads";
        public const int StarterIngots = 200, NoAdsIngots = 150;
    }

    /// <summary>Can teklifi: bütün canları doldur ya da süreli sınırsız can.</summary>
    public enum HeartOffer { Refill = 0, Unlimited1h = 1, Unlimited3h = 2 }

    public readonly struct IngotPack
    {
        public readonly string ProductId;
        public readonly int Ingots, BonusPercent;
        public readonly string Art;
        public IngotPack(string id, int ingots, int bonus, string art) { ProductId = id; Ingots = ingots; BonusPercent = bonus; Art = art; }
    }

    /// <summary>Cüzdan işlemleri (saf; kayıt <see cref="MetaSave"/> alanlarında). Kaydı çağıran yazar.</summary>
    public static class Wallet
    {
        public static int Count(MetaSave m, Booster b) => b == Booster.Moves ? m.boostMoves : b == Booster.Recolor ? m.boostRecolor : m.boostBomb;

        public static void Add(MetaSave m, Booster b, int n)
        {
            if (n == 0) return;
            if (b == Booster.Moves) m.boostMoves = Math.Max(0, m.boostMoves + n);
            else if (b == Booster.Recolor) m.boostRecolor = Math.Max(0, m.boostRecolor + n);
            else m.boostBomb = Math.Max(0, m.boostBomb + n);
        }

        public static bool Unlocked(Booster b, int levelNumber) => levelNumber >= ShopCatalog.UnlockLevel(b);

        /// <summary>Bir adet harcar (kullanım). Yoksa false.</summary>
        public static bool Consume(MetaSave m, Booster b)
        {
            if (Count(m, b) <= 0) return false;
            Add(m, b, -1);
            return true;
        }

        /// <summary>Külçeyle satın alır (adet 1 ya da 3). Külçe yetmezse false, hiçbir şey değişmez.</summary>
        public static bool Buy(MetaSave m, Booster b, int qty)
        {
            int price = qty == 3 ? ShopCatalog.Price3(b) : qty == 1 ? ShopCatalog.Price(b) : -1;
            if (price < 0 || m.ingots < price) return false;
            m.ingots -= price;
            Add(m, b, qty);
            return true;
        }

        /// <summary>Can teklifini SB Gold ile alır. Yetmezse ya da canlar zaten dolu ve teklif doldurmaysa false.</summary>
        public static bool BuyHearts(MetaSave m, HeartOffer o, long now)
        {
            int price = ShopCatalog.HeartPrice(o);
            if (m.ingots < price) return false;
            if (o == HeartOffer.Refill && Hearts.Count(m, now) >= Hearts.Max) return false;
            m.ingots -= price;
            if (o == HeartOffer.Refill) Hearts.Refill(m);
            else Hearts.AddUnlimited(m, now, ShopCatalog.HeartSeconds(o));
            return true;
        }

        /// <summary>Açılmış ama deneme adedi henüz verilmemiş güçlendiricilere <see cref="ShopCatalog.TrialCount"/> verir.</summary>
        public static List<Booster> GrantTrials(MetaSave m, int levelNumber)
        {
            var got = new List<Booster>();
            foreach (var b in ShopCatalog.All)
            {
                string k = ShopCatalog.Key(b);
                if (!Unlocked(b, levelNumber) || m.boostTrials.Contains(k)) continue;
                m.boostTrials.Add(k);
                Add(m, b, ShopCatalog.TrialCount);
                got.Add(b);
            }
            return got;
        }

        /// <summary>Kazanılmış, külçe hediyesi olan ve henüz alınmamış taşlar (kazanım sırasıyla).</summary>
        public static List<StoneDefinition> PendingGifts(MetaSave m, StoneCatalog cat)
        {
            var list = new List<StoneDefinition>();
            if (cat == null) return list;
            foreach (var id in m.stones)
            {
                var s = cat.Find(id);
                if (s != null && s.Ingots > 0 && !m.stoneGifts.Contains(id)) list.Add(s);
            }
            return list;
        }

        public static bool HasPendingGift(MetaSave m, StoneCatalog cat) => PendingGifts(m, cat).Count > 0;

        /// <summary>Taşın külçe hediyesini verir (bir kez). Verilen miktar; verilmediyse 0. Taş koleksiyonda kalır.</summary>
        public static int ClaimGift(MetaSave m, StoneCatalog cat, string stoneId)
        {
            var s = cat?.Find(stoneId);
            if (s == null || s.Ingots <= 0 || !m.stones.Contains(stoneId) || m.stoneGifts.Contains(stoneId)) return 0;
            m.stoneGifts.Add(stoneId);
            m.ingots += s.Ingots;
            return s.Ingots;
        }

        /// <summary>
        /// Bulut kaydı yerelin yerine geçerken cüzdan kaybolmasın (satın alınan külçe yerelde kazanılmış olabilir):
        /// külçe ve güçlendirici adetlerinde büyük olan, reklamsız ve başlangıç paketinde "ya da", alınan hediyelerde birleşim.
        /// </summary>
        public static void MergeInto(MetaSave target, MetaSave local)
        {
            if (target == null || local == null) return;
            target.ingots = Math.Max(target.ingots, local.ingots);
            target.boostMoves = Math.Max(target.boostMoves, local.boostMoves);
            target.boostRecolor = Math.Max(target.boostRecolor, local.boostRecolor);
            target.boostBomb = Math.Max(target.boostBomb, local.boostBomb);
            target.heartsInfiniteUntil = Math.Max(target.heartsInfiniteUntil, local.heartsInfiniteUntil);
            target.noAds |= local.noAds;
            target.starterBought |= local.starterBought;
            foreach (var g in local.stoneGifts) if (!target.stoneGifts.Contains(g)) target.stoneGifts.Add(g);
            foreach (var t in local.boostTrials) if (!target.boostTrials.Contains(t)) target.boostTrials.Add(t);
        }
    }
}
