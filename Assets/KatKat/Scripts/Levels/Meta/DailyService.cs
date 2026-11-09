using System;
using System.Collections.Generic;
using System.Globalization;
using KatKat.Core;
using KatKat.Levels;

namespace KatKat.Meta
{
    /// <summary>Günün kimliği. Canlıda sunucudan gelir; yerel saat ödül/hak otoritesi değildir.</summary>
    public interface IDayClock
    {
        string DayId { get; }
    }

    /// <summary>
    /// YEREL PROTOTİP saati: cihaz saati + sıfırlama saati. Canlı ödül ve hak otoritesi olarak kullanılmaz
    /// (saat dilimi ve sıfırlama saati yayın öncesi karar; kaynak sunucu olacak).
    /// </summary>
    public sealed class LocalDayClock : IDayClock
    {
        public Func<DateTime> Now = () => DateTime.Now;
        public int ResetHour;
        public string DayId => Now().AddHours(-ResetHour).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>Test ve elle deneme için sabit gün.</summary>
    public sealed class FixedDayClock : IDayClock
    {
        public string DayId { get; set; } = "2026-11-06";
    }

    public enum AdOutcome { Rewarded, Cancelled, Failed }

    /// <summary>Ödüllü reklam adaptörü. Geri çağrı: sonuç + ödül kimliği (aynı ödül iki kez gelebilir).</summary>
    public interface IRewardedAds
    {
        void Show(string placement, Action<AdOutcome, string> done);
    }

    /// <summary>Reklamsız paket hakkı (canlıda mağaza + sunucu doğrulaması).</summary>
    public interface IAdFreeEntitlement
    {
        bool Owned { get; }
    }

    /// <summary>SAHTE reklam adaptörü (yerel prototip): gerçek reklam göstermez, istenen sonucu hemen döndürür.</summary>
    public sealed class FakeRewardedAds : IRewardedAds
    {
        public AdOutcome Next = AdOutcome.Rewarded;
        /// <summary>Aynı ödül geri çağrısını iki kez gönder (tekrar gelen olay denemesi).</summary>
        public bool DuplicateCallback;
        private int _n;
        public int Shown { get; private set; }

        public void Show(string placement, Action<AdOutcome, string> done)
        {
            Shown++;
            string token = $"fake-{placement}-{++_n}";
            done(Next, token);
            if (DuplicateCallback) done(Next, token);
        }
    }

    /// <summary>SAHTE reklamsız hak (yerel prototip).</summary>
    public sealed class FakeAdFree : IAdFreeEntitlement
    {
        public bool Owned { get; set; }
    }

    /// <summary>Günlük bulmaca sınırları (yapılandırılabilir prototip değerleri).</summary>
    public sealed class DailyConfig
    {
        /// <summary>İlk girişle gelen ücretsiz tekrar sayısı.</summary>
        public int FreeRetries = 1;
        /// <summary>Ek reklamla üçüncü deneme: prototipte kapalı (0).</summary>
        public int AdExtraAttempts = 0;
        /// <summary>Deneme başına taş gücü kullanımı.</summary>
        public int PowerUsesPerAttempt = 1;
        /// <summary>Başarı ödülü: aynı gün açılan, henüz kazanılmamış normal bölüm oturumlarına eklenen hamle.</summary>
        public int MoveBonus = 1;
    }

    /// <summary>Günlük katalog kaydı (Resources/Daily/daily_index.json).</summary>
    public sealed class DailyChallengeInfo
    {
        public string ChallengeId;
        public string LevelFile;
        public string ContentVersion;
        /// <summary>v10 P3.4: seviye grubu (1 tabanlı; eski tek bulmacalı katalogda 1).</summary>
        public int Tier = 1;
    }

    /// <summary>Seviye grubu: oyuncunun ulaştığı en yüksek bölüm numarası bu aralıktaysa (dahil).</summary>
    public sealed class DailyTier
    {
        public int Tier, MinLevel, MaxLevel;
    }

    /// <summary>
    /// Günlük katalog. v10 P3.4 (kullanıcı: "kişinin seviyesine orantılı, çok zor"): gün × seviye grubu tablosu.
    /// Eski biçim ("challenges" listesi) de okunur (tek grup).
    /// </summary>
    public sealed class DailyIndex
    {
        public readonly List<DailyChallengeInfo> Challenges = new List<DailyChallengeInfo>();
        public readonly List<DailyTier> Tiers = new List<DailyTier>();
        /// <summary>Gün sırası (katalogdaki gün numarasına göre) → grup → bölüm dosyası.</summary>
        public readonly List<Dictionary<int, string>> Days = new List<Dictionary<int, string>>();
        public string ContentVersion = "";

        public static DailyIndex Parse(string json)
        {
            var idx = new DailyIndex();
            if (string.IsNullOrEmpty(json)) return idx;
            if (!(MiniJson.Parse(json) is Dictionary<string, object> root)) throw new FormatException("daily_index.json: kök nesne olmalı");
            if (root.TryGetValue("days", out var dl) && dl is List<object> days)
            {
                idx.ContentVersion = root.TryGetValue("contentVersion", out var cv) ? cv as string ?? "" : "";
                if (root.TryGetValue("tiers", out var tl) && tl is List<object> tiers)
                    foreach (var t in tiers)
                        if (t is Dictionary<string, object> to)
                            idx.Tiers.Add(new DailyTier { Tier = Int(to, "tier"), MinLevel = Int(to, "minLevel"), MaxLevel = Int(to, "maxLevel") });
                if (idx.Tiers.Count == 0) throw new FormatException("daily_index.json: \"tiers\" boş");
                foreach (var d in days)
                {
                    if (!(d is Dictionary<string, object> dob) || !(dob.TryGetValue("levels", out var lv) && lv is Dictionary<string, object> levels)) continue;
                    var map = new Dictionary<int, string>();
                    foreach (var kv in levels)
                        if (int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tier) && kv.Value is string file) map[tier] = file;
                    if (map.Count > 0) idx.Days.Add(map);
                }
                return idx;
            }
            if (!(root.TryGetValue("challenges", out var l) && l is List<object> items))
                throw new FormatException("daily_index.json: kökte \"days\" ya da \"challenges\" olmalı");
            foreach (var it in items)
                if (it is Dictionary<string, object> o)
                    idx.Challenges.Add(new DailyChallengeInfo
                    {
                        ChallengeId = o.TryGetValue("challengeId", out var a) ? a as string : null,
                        LevelFile = o.TryGetValue("level", out var b) ? b as string : null,
                        ContentVersion = o.TryGetValue("contentVersion", out var c) ? c as string : null,
                    });
            return idx;
        }

        private static int Int(Dictionary<string, object> o, string k) =>
            o.TryGetValue(k, out var v) && v != null ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : 0;

        /// <summary>Oyuncunun ulaştığı en yüksek bölüm numarasına (1 tabanlı) göre grup; aralık dışıysa en yakın uç grup.</summary>
        public int TierFor(int reachedLevel)
        {
            if (Tiers.Count == 0) return 1;
            foreach (var t in Tiers) if (reachedLevel >= t.MinLevel && reachedLevel <= t.MaxLevel) return t.Tier;
            return reachedLevel < Tiers[0].MinLevel ? Tiers[0].Tier : Tiers[Tiers.Count - 1].Tier;
        }

        public DailyTier TierInfo(int tier) => Tiers.Find(t => t.Tier == tier);

        private static int DayNumber(string dayId) =>
            DateTime.TryParseExact(dayId, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? (int)(d - new DateTime(2026, 1, 1)).TotalDays : 0;

        private static int Wrap(int n, int count) => ((n % count) + count) % count;

        /// <summary>Güne ve gruba göre bulmaca (gün numarası mod katalog günü). Canlıda sunucu seçer.</summary>
        public DailyChallengeInfo PickFor(string dayId, int tier)
        {
            if (Days.Count > 0)
            {
                var map = Days[Wrap(DayNumber(dayId), Days.Count)];
                if (!map.TryGetValue(tier, out var file)) return null;
                return new DailyChallengeInfo { ChallengeId = file, LevelFile = file, ContentVersion = ContentVersion, Tier = tier };
            }
            return PickFor(dayId);
        }

        /// <summary>Eski biçim: tek liste, gün numarası mod sayı.</summary>
        public DailyChallengeInfo PickFor(string dayId)
        {
            if (Days.Count > 0) return PickFor(dayId, Tiers.Count > 0 ? Tiers[0].Tier : 1);
            if (Challenges.Count == 0) return null;
            return Challenges[Wrap(DayNumber(dayId), Challenges.Count)];
        }
    }

    /// <summary>GrantedNoAd: reklam bulunamadı/yüklenemedi (5 sn arama) → kullanıcı kararı 2026-11-08: reklamsız giriş.</summary>
    public enum DailyEntryResult { Granted, AlreadyEntered, AdCancelled, AdFailed, DuplicateIgnored, NotAvailable, GrantedNoAd }
    public enum DailyBeginResult { Started, Resumed, NotEntered, NoAttemptsLeft, AlreadyWon, NotAvailable }
    public enum PowerResult { Used, NoAttempt, NoStone, AlreadyUsed, InvalidTarget }

    /// <summary>
    /// v9 B2: günlük bulmaca durum makinesi (saf; kaydı <c>persist</c> ile yazdırır).
    /// Giriş (ödüllü reklam ya da reklamsız hak) bir ilk deneme + bir ücretsiz tekrar açar. Hak, kalıcı oturum kaydı
    /// açıldığında harcanmış sayılır; uygulama kapanırsa aynı deneme yeniden açılır. Reklam iptali/hatası hakkı
    /// eksiltmez; aynı ödül geri çağrısı iki kez gelirse yeni hak oluşmaz. Başarı ödülü gün başına bir kez yazılır.
    /// </summary>
    public sealed class DailyService
    {
        private readonly MetaSave _meta;
        private readonly IDayClock _clock;
        private readonly Action _persist;
        private readonly Func<string, int, DailyChallengeInfo> _pick;
        private readonly Func<int> _tierFor;
        public DailyConfig Config;
        public IRewardedAds Ads;
        public IAdFreeEntitlement AdFree;

        /// <summary>Tek gruplu (eski) seçim.</summary>
        public DailyService(MetaSave meta, IDayClock clock, Func<string, DailyChallengeInfo> pick, Action persist,
            IRewardedAds ads, IAdFreeEntitlement adFree, DailyConfig config = null)
            : this(meta, clock, pick == null ? (Func<string, int, DailyChallengeInfo>)null : (day, tier) => pick(day), () => 1, persist, ads, adFree, config) { }

        /// <summary>
        /// v10 P3.4: seviye gruplu seçim. <paramref name="tierFor"/> oyuncunun şu anki grubunu verir; grup o günün ilk
        /// açılışında sabitlenir (gün içinde bölüm geçmek bulmacayı değiştirmez, açık hakları sıfırlamaz).
        /// </summary>
        public DailyService(MetaSave meta, IDayClock clock, Func<string, int, DailyChallengeInfo> pick, Func<int> tierFor, Action persist,
            IRewardedAds ads, IAdFreeEntitlement adFree, DailyConfig config = null)
        {
            _meta = meta ?? throw new ArgumentNullException(nameof(meta));
            _clock = clock;
            _pick = pick;
            _tierFor = tierFor ?? (() => 1);
            _persist = persist ?? (() => { });
            Ads = ads;
            AdFree = adFree;
            Config = config ?? new DailyConfig();
        }

        /// <summary>Bugünün seviye grubu (sabitlenmişse o).</summary>
        public int Tier => _meta.daily.dayId == Today && _meta.daily.tier > 0 ? _meta.daily.tier : Math.Max(1, _tierFor());

        public DailySave State => _meta.daily;
        public string Today => _clock.DayId;
        public DailyChallengeInfo Challenge { get; private set; }
        public bool Available => Challenge != null;

        /// <summary>Gün ya da bulmaca/içerik sürümü değiştiyse günlük durum sıfırlanır (ödül günlüğü ve taşlar kalır).</summary>
        public void Refresh()
        {
            string today = Today;
            int tier = Tier;
            Challenge = _pick?.Invoke(today, tier);
            string cid = Challenge?.ChallengeId ?? "", ver = Challenge?.ContentVersion ?? "";
            var d = _meta.daily;
            if (d.dayId == today && d.challengeId == cid && d.contentVersion == ver)
            {
                if (d.tier != tier) { d.tier = tier; _persist(); } // eski kayıt: grup alanı boştu
                return;
            }
            _meta.daily = new DailySave { dayId = today, challengeId = cid, contentVersion = ver, tier = tier };
            _persist();
        }

        public int AttemptsAllowed => State.entered ? State.attemptsAllowed : 0;
        public int AttemptsLeft => Math.Max(0, AttemptsAllowed - State.attemptsUsed);
        public bool WonToday { get { Refresh(); return State.won; } }
        public bool HasMoveBonusToday => _meta.moveBonusDay == Today;

        public void RequestEntry(Action<DailyEntryResult> done)
        {
            Refresh();
            if (!Available) { done?.Invoke(DailyEntryResult.NotAvailable); return; }
            if (State.entered) { done?.Invoke(DailyEntryResult.AlreadyEntered); return; }
            if (AdFree != null && AdFree.Owned) { Grant("adFree"); done?.Invoke(DailyEntryResult.Granted); return; }
            if (Ads == null) { done?.Invoke(DailyEntryResult.AdFailed); return; }
            string day = Today;
            Ads.Show("daily_entry", (outcome, token) =>
            {
                if (outcome == AdOutcome.Cancelled) { done?.Invoke(DailyEntryResult.AdCancelled); return; }
                if (outcome == AdOutcome.Failed)
                {
                    // v12 (kullanıcı, 2026-11-08): reklam yüklenmezse günlük bulmaca reklamsız oynanır.
                    Refresh();
                    if (Today != day || State.entered) { done?.Invoke(DailyEntryResult.DuplicateIgnored); return; }
                    Grant("noFill");
                    done?.Invoke(DailyEntryResult.GrantedNoAd);
                    return;
                }
                if (outcome != AdOutcome.Rewarded) { done?.Invoke(DailyEntryResult.AdFailed); return; }
                Refresh();
                // Ödül, istendiği günden sonra geldiyse yeni günün hakkını açmaz.
                if (Today != day || string.IsNullOrEmpty(token) || State.adTokens.Contains(token) || State.entered)
                {
                    done?.Invoke(DailyEntryResult.DuplicateIgnored);
                    return;
                }
                State.adTokens.Add(token);
                Grant("ad");
                done?.Invoke(DailyEntryResult.Granted);
            });
        }

        /// <summary>Elle deneme için reklamsız giriş (yalnız geliştirici menüsü).</summary>
        public void GrantForDebug() { Refresh(); if (!State.entered) Grant("debug"); }

        private void Grant(string source)
        {
            State.entered = true;
            State.entrySource = source;
            // İlk deneme + ücretsiz tekrar. Ek reklamla deneme (AdExtraAttempts) ayrı akış ister; prototipte kapalı.
            State.attemptsAllowed = 1 + Math.Max(0, Config.FreeRetries);
            _persist();
        }

        /// <summary>Denemeyi açar ya da açık denemeye döner. Yeni deneme hakkı yalnız burada, kayıt yazılarak harcanır.</summary>
        public DailyBeginResult BeginAttempt()
        {
            Refresh();
            if (!Available) return DailyBeginResult.NotAvailable;
            if (!State.entered) return DailyBeginResult.NotEntered;
            if (State.won) return DailyBeginResult.AlreadyWon;
            if (State.attemptOpen) return DailyBeginResult.Resumed;
            if (State.attemptsUsed >= State.attemptsAllowed) return DailyBeginResult.NoAttemptsLeft;
            State.attemptsUsed++;
            State.attemptOpen = true;
            State.powerUsed = false;
            _persist();
            return DailyBeginResult.Started;
        }

        /// <summary>Açık denemeyi kapatır. Başarıda ödül gün başına bir kez yazılır ve +1 hamle bugüne bağlanır.</summary>
        /// <param name="stars">Kazanılan denemenin performans yıldızı (1–3); günün ilk galibiyetinde kayda eklenir.</param>
        public bool FinishAttempt(bool won, int stars = 0)
        {
            if (!State.attemptOpen || State.dayId != Today) return false;
            State.attemptOpen = false;
            bool rewarded = false;
            if (won && !State.won)
            {
                State.won = true;
                string dayPrefix = State.dayId + "|";
                if (!_meta.rewardLog.Exists(k => k.StartsWith(dayPrefix, StringComparison.Ordinal)))
                {
                    _meta.rewardLog.Add(State.Key);
                    _meta.moveBonusDay = State.dayId;
                    rewarded = true;
                    if (stars > 0)
                    {
                        State.stars = Math.Min(3, Math.Max(1, stars));
                        _meta.dailyStars += State.stars;
                    }
                }
            }
            _persist();
            return rewarded;
        }

        /// <summary>
        /// Seçili taşın gücü: doğrulama ve harcama aynı işlemde. Geçersiz hedef ya da güçsüz durum hakkı tüketmez.
        /// </summary>
        public PowerResult TryUsePower(StoneCatalog stones, LevelState state)
        {
            if (!State.attemptOpen) return PowerResult.NoAttempt;
            string stone = CollectionRules.ActiveStone(_meta);
            var def = stone != null ? stones?.Find(stone) : null;
            if (def == null) return PowerResult.NoStone;
            if (State.powerUsed || Config.PowerUsesPerAttempt <= 0) return PowerResult.AlreadyUsed;
            if (!StonePowers.Apply(def.PowerId, state)) return PowerResult.InvalidTarget;
            State.powerUsed = true;
            _persist();
            return PowerResult.Used;
        }

        /// <summary>
        /// Normal bölüm oturumu açılırken eklenecek hamle: bugün günlük kazanıldıysa ve bölüm daha önce kazanılmadıysa
        /// <see cref="DailyConfig.MoveBonus"/>, değilse 0. Geçmiş yıldızları ve açık oturumu etkilemez.
        /// </summary>
        public int MoveBonusFor(bool levelAlreadyCompleted) =>
            !levelAlreadyCompleted && !string.IsNullOrEmpty(_meta.moveBonusDay) && _meta.moveBonusDay == Today ? Math.Max(0, Config.MoveBonus) : 0;
    }
}
