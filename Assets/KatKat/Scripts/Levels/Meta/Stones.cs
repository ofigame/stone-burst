using System;
using System.Collections.Generic;
using System.Globalization;
using KatKat.Core;
using KatKat.Levels;

namespace KatKat.Meta
{
    /// <summary>
    /// v9 B1: koleksiyon taşı tanımı (içerik yapılandırması, Resources/Collection/stones.json). Mantıksal kimlik
    /// (<see cref="StoneId"/>) görsel anahtardan (<see cref="ArtKey"/>) ayrıdır: nihai görsel gelince yalnız artKey değişir.
    /// </summary>
    public sealed class StoneDefinition
    {
        public string StoneId;
        public string NameKey;
        public string DescKey;
        /// <summary>Görsel anahtarı; sprite yoksa ortak yer tutucu kullanılır.</summary>
        public string ArtKey;
        /// <summary>Güç ailesi (taş başına ayrı kod yolu yok).</summary>
        public string PowerId;
        /// <summary>Kazanım koşulu türü (<see cref="StoneUnlock"/>): levelCompleted, starsTotal, treasure, worldCompleted.</summary>
        public string UnlockType;
        /// <summary>Sayısal koşul: bölüm numarası (levelCompleted) ya da yıldız toplamı (starsTotal).</summary>
        public int UnlockValue;
        /// <summary>Kimlik koşulu: hazine kimliği (treasure) ya da dünya kimliği (worldCompleted, "world_01").</summary>
        public string UnlockKey = "";
        /// <summary>v10 P2.1: <see cref="StoneCategory"/>; eski şemada yok → starStone.</summary>
        public string Category = StoneCategory.StarStone;
        /// <summary>v10 P2.1: <see cref="StoneRarity"/>; eski şemada yok → common.</summary>
        public string Rarity = StoneRarity.Common;
        /// <summary>Sunum vurgusu (yalnız görünüm): "cool" (mavi/turkuaz kenar ışığı) ya da "warm" (altın). Varsayılan cool.</summary>
        public string Accent = "cool";
        /// <summary>Önizleme/kazanım ışıltı rengi "#RRGGBB" (yalnız görünüm; görselin baskın renginden). Boşsa vurgudan.</summary>
        public string Glow = "";
    }

    /// <summary>v10 P2.1: koleksiyon öğesi kategorileri.</summary>
    public static class StoneCategory
    {
        public const string StarStone = "starStone", HiddenTreasure = "hiddenTreasure", LegendaryStone = "legendaryStone";
        public static readonly string[] All = { StarStone, HiddenTreasure, LegendaryStone };
    }

    /// <summary>v10 P2.1: nadirlik (yalnız sunum ve sıralama; güç ya da kazanımı etkilemez).</summary>
    public static class StoneRarity
    {
        public const string Common = "common", Rare = "rare", Epic = "epic", Legendary = "legendary";
        public static readonly string[] All = { Common, Rare, Epic, Legendary };
    }

    /// <summary>v10 P2.1: kazanım türleri. Bilinmeyen tür ayrıştırılır ama hiç kazandırmaz (ileri uyumluluk).</summary>
    public static class StoneUnlock
    {
        public const string LevelCompleted = "levelCompleted", StarsTotal = "starsTotal", Treasure = "treasure", WorldCompleted = "worldCompleted";
        public static readonly string[] Known = { LevelCompleted, StarsTotal, Treasure, WorldCompleted };
    }

    /// <summary>Kazanım koşullarının oyuncu durumu. Boş bırakılan alan o türü sağlanmamış sayar.</summary>
    public sealed class UnlockContext
    {
        public Func<string, bool> IsLevelCompleted;
        public int TotalStars;
        public Func<string, bool> HasTreasure;
        public Func<string, bool> IsWorldCompleted;
    }

    public sealed class StoneCatalog
    {
        public const int SchemaVersion = 2;
        public readonly List<StoneDefinition> Stones = new List<StoneDefinition>();
        /// <summary>Okunan dosyanın şeması (1 = v9, 2 = v10).</summary>
        public int Schema { get; private set; } = SchemaVersion;

        public StoneDefinition Find(string id) => Stones.Find(s => s.StoneId == id);
        public List<StoneDefinition> InCategory(string category) => Stones.FindAll(s => s.Category == category);

        public static StoneCatalog Parse(string json)
        {
            var cat = new StoneCatalog();
            if (string.IsNullOrEmpty(json)) return cat;
            if (!(MiniJson.Parse(json) is Dictionary<string, object> root) || !(root.TryGetValue("stones", out var list) && list is List<object> items))
                throw new FormatException("stones.json: kökte \"stones\" dizisi olmalı");
            // Şema: alan yoksa 1 (v9); 2 = v10 P2.1 (kategori, nadirlik, yeni kazanım türleri). Daha yenisi okunmaz.
            int schema = root.TryGetValue("schemaVersion", out var sv) && sv != null ? Convert.ToInt32(sv, CultureInfo.InvariantCulture) : 1;
            if (schema < 1 || schema > SchemaVersion) throw new FormatException("stones.json: desteklenmeyen şema " + schema);
            cat.Schema = schema;
            foreach (var it in items)
            {
                if (!(it is Dictionary<string, object> o)) throw new FormatException("stones.json: taş nesne olmalı");
                var s = new StoneDefinition
                {
                    StoneId = Str(o, "stoneId"),
                    NameKey = Str(o, "nameKey"),
                    DescKey = Str(o, "descKey"),
                    ArtKey = Str(o, "artKey"),
                    PowerId = Str(o, "powerId"),
                };
                if (o.TryGetValue("unlock", out var u) && u is Dictionary<string, object> uo)
                {
                    s.UnlockType = Str(uo, "type");
                    // value: sayı (bölüm, yıldız) ya da metin (hazine/dünya kimliği). Ayrıca "key" alanı da kabul edilir.
                    if (uo.TryGetValue("value", out var v) && v is string vs) s.UnlockKey = vs;
                    else s.UnlockValue = Convert.ToInt32(v ?? 0, CultureInfo.InvariantCulture);
                    if (uo.ContainsKey("key")) s.UnlockKey = Str(uo, "key");
                }
                if (o.ContainsKey("category")) s.Category = Str(o, "category");
                if (o.ContainsKey("rarity")) s.Rarity = Str(o, "rarity");
                if (o.ContainsKey("accent")) s.Accent = Str(o, "accent") == "warm" ? "warm" : "cool";
                if (o.ContainsKey("glow")) s.Glow = Str(o, "glow");
                if (string.IsNullOrEmpty(s.StoneId)) throw new FormatException("stones.json: stoneId boş");
                if (Array.IndexOf(StoneCategory.All, s.Category) < 0) throw new FormatException($"stones.json: {s.StoneId} bilinmeyen kategori '{s.Category}'");
                if (Array.IndexOf(StoneRarity.All, s.Rarity) < 0) throw new FormatException($"stones.json: {s.StoneId} bilinmeyen nadirlik '{s.Rarity}'");
                switch (s.UnlockType)
                {
                    case StoneUnlock.LevelCompleted:
                    case StoneUnlock.StarsTotal:
                        if (s.UnlockValue <= 0) throw new FormatException($"stones.json: {s.StoneId} {s.UnlockType} değeri pozitif sayı olmalı");
                        break;
                    case StoneUnlock.Treasure:
                    case StoneUnlock.WorldCompleted:
                        if (string.IsNullOrEmpty(s.UnlockKey)) throw new FormatException($"stones.json: {s.StoneId} {s.UnlockType} kimlik ister");
                        break;
                }
                if (cat.Find(s.StoneId) != null) throw new FormatException("stones.json: yinelenen stoneId " + s.StoneId);
                cat.Stones.Add(s);
            }
            return cat;
        }

        private static string Str(Dictionary<string, object> o, string k) => o.TryGetValue(k, out var v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
    }

    public static class CollectionRules
    {
        /// <summary>Koşulu sağlanmış ama henüz kazanılmamış taşları kazandırır (tek seferlik). Yeni kazanılanları döndürür.</summary>
        public static List<StoneDefinition> GrantUnlocked(MetaSave meta, StoneCatalog cat, Func<string, bool> isLevelCompleted) =>
            GrantUnlocked(meta, cat, new UnlockContext { IsLevelCompleted = isLevelCompleted });

        public static List<StoneDefinition> GrantUnlocked(MetaSave meta, StoneCatalog cat, UnlockContext ctx)
        {
            var granted = new List<StoneDefinition>();
            if (meta == null || cat == null) return granted;
            foreach (var s in cat.Stones)
            {
                if (meta.stones.Contains(s.StoneId) || !IsMet(s, ctx)) continue;
                meta.stones.Add(s.StoneId);
                // Günlükte kullanılacak taş: ilk kazanılan güçlü taş (gücü olmayan taş seçili olursa güç düğmesi hiç açılmaz).
                if (string.IsNullOrEmpty(meta.selectedStone) && !string.IsNullOrEmpty(s.PowerId)) meta.selectedStone = s.StoneId;
                granted.Add(s);
            }
            return granted;
        }

        public static bool IsMet(StoneDefinition s, Func<string, bool> isLevelCompleted) =>
            IsMet(s, new UnlockContext { IsLevelCompleted = isLevelCompleted });

        public static bool IsMet(StoneDefinition s, UnlockContext c)
        {
            if (s == null || c == null) return false;
            switch (s.UnlockType)
            {
                case StoneUnlock.LevelCompleted: return s.UnlockValue > 0 && c.IsLevelCompleted != null && c.IsLevelCompleted($"level_{s.UnlockValue:000}");
                case StoneUnlock.StarsTotal: return s.UnlockValue > 0 && c.TotalStars >= s.UnlockValue;
                case StoneUnlock.Treasure: return !string.IsNullOrEmpty(s.UnlockKey) && c.HasTreasure != null && c.HasTreasure(s.UnlockKey);
                case StoneUnlock.WorldCompleted: return !string.IsNullOrEmpty(s.UnlockKey) && c.IsWorldCompleted != null && c.IsWorldCompleted(s.UnlockKey);
                default: return false; // bilinmeyen koşul kazandırmaz
            }
        }

        /// <summary>Seçili taş sahipliyse o, değilse sahip olunan ilk taş; hiç yoksa null.</summary>
        public static string ActiveStone(MetaSave meta)
        {
            if (meta == null || meta.stones.Count == 0) return null;
            return meta.stones.Contains(meta.selectedStone) ? meta.selectedStone : meta.stones[0];
        }
    }

    /// <summary>
    /// v10 P3.2: taş gücü ortak yapısı. Her güç belirlenimli ve kural motoruna uygun: tahtayı doğrudan değiştirmez,
    /// oyunun zaten var olan mekaniklerini verir (rulo yuvası, tepsi özel taşı, hamle). Böylece etkiyi gerçek
    /// <see cref="RulesEngine"/> kuralları çözer; bölüm kuralları ve 1.000 bölüm etkilenmez.
    /// </summary>
    public interface IStonePower
    {
        string Id { get; }
        /// <summary>Durumu değiştirmeden: şu an uygulanabilir ve işe yarar mı.</summary>
        bool CanApply(LevelState s);
        /// <summary>Yalnız <see cref="CanApply"/> doğruyken çağrılır.</summary>
        void Apply(LevelState s);
    }

    /// <summary>
    /// Güç aileleri (taş başına ayrı kod yolu yok; taş tanımı <see cref="StoneDefinition.PowerId"/> ile aileyi seçer):
    /// <list type="bullet">
    /// <item>dailyRoll (gelgit): boş rulo yuvasını doldurur; oyuncu satır rulosunu kurallara göre kullanır.</item>
    /// <item>ocean (okyanus, satır): tepsiye satır roketi.</item>
    /// <item>storm (fırtına, sütun): tepsiye sütun roketi.</item>
    /// <item>galaxy (galaksi, renk): tepsiye Altın Rulo (hedefin üst rengini toplar).</item>
    /// <item>moon (ay, hamle): +<see cref="MoonMoves"/> hamle.</item>
    /// </list>
    /// Ateş (alan) gibi aileler kural motorunda karşılığı olan bir etki gerektirir; ayrı karar.
    /// </summary>
    public static class StonePowers
    {
        public const string DailyRoll = "dailyRoll", Ocean = "ocean", Storm = "storm", Galaxy = "galaxy", Moon = "moon";
        public const int MoonMoves = 2;

        private static readonly Dictionary<string, IStonePower> _all = new Dictionary<string, IStonePower>();

        static StonePowers()
        {
            Register(new RollPower());
            Register(new TrayPower(Ocean, SpecialKind.RocketRow));
            Register(new TrayPower(Storm, SpecialKind.RocketColumn));
            Register(new TrayPower(Galaxy, SpecialKind.Rainbow));
            Register(new MovesPower(Moon, MoonMoves));
        }

        public static void Register(IStonePower p) => _all[p.Id] = p;

        public static IStonePower Get(string powerId) => powerId != null && _all.TryGetValue(powerId, out var p) ? p : null;

        public static IEnumerable<string> Ids => _all.Keys;

        public static bool CanApply(string powerId, LevelState s)
        {
            if (s == null || s.Status != GameStatus.Idle) return false;
            var p = Get(powerId);
            return p != null && p.CanApply(s);
        }

        /// <summary>Doğrulama ve uygulama tek adımda; geçersizse durum değişmez.</summary>
        public static bool Apply(string powerId, LevelState s)
        {
            if (!CanApply(powerId, s)) return false;
            Get(powerId).Apply(s);
            return true;
        }

        /// <summary>Gelgit: boş rulo yuvasını doldurur (rulosu açık düzende ve hedefi olan satır varken).</summary>
        private sealed class RollPower : IStonePower
        {
            public string Id => DailyRoll;
            public bool CanApply(LevelState s)
            {
                if (!s.RollEnabled || s.RollSlotFull) return false;
                for (int y = 0; y < s.Board.Height; y++) if (RulesEngine.RollTargets(s.Board, y).Count > 0) return true;
                return false;
            }
            public void Apply(LevelState s) => s.RollSlotFull = true;
        }

        /// <summary>Tepsiye özel taş: özel taşlı düzende, tepsi doluysa değil ve tahtada etkili bir hedef varken.</summary>
        private sealed class TrayPower : IStonePower
        {
            private readonly SpecialKind _kind;
            public string Id { get; }
            public TrayPower(string id, SpecialKind kind) { Id = id; _kind = kind; }
            public bool CanApply(LevelState s)
            {
                if (!s.SpecialsEnabled || s.Tray.Count >= LevelState.TrayCapacity) return false;
                var b = s.Board;
                for (int i = 0; i < b.Count; i++) if (RulesEngine.TrayHasEffect(b, _kind, b.X(i), b.Y(i))) return true;
                return false;
            }
            public void Apply(LevelState s) => s.Tray.Add(_kind);
        }

        /// <summary>Hamle ekler (bölüm sürerken).</summary>
        private sealed class MovesPower : IStonePower
        {
            private readonly int _moves;
            public string Id { get; }
            public MovesPower(string id, int moves) { Id = id; _moves = moves; }
            public bool CanApply(LevelState s) => _moves > 0;
            public void Apply(LevelState s) => s.MovesLeft += _moves;
        }
    }
}
