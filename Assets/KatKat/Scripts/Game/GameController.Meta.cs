using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.Levels;
using KatKat.Meta;
using KatKat.UI;
using KatKat.View;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// v9 A/B (docs/v9 yol haritası): oturum sonucu modeli, koleksiyon taşı kazanımı, günlük bulmaca akışı ve günlük
    /// başarı ödülü (+1 hamle). Reklam ve reklamsız hak SAHTE adaptörlerle çalışır (yerel prototip); gün kimliği yerel
    /// saatten gelir ve canlı ödül otoritesi değildir.
    /// </summary>
    public sealed partial class GameController
    {
        // ---- A: oturum sonucu --------------------------------------------------------------------------------

        private SessionKind _sessionKind = SessionKind.Normal;
        private SessionKind _nextKind = SessionKind.Custom;
        private SessionOutcome _outcome;

        public SessionOutcome LastOutcome => _outcome;
        public SessionKind CurrentSessionKind => _sessionKind;

        /// <summary>Biten her oturumun sonucu (ileride telemetri/ZPE için; bu pakette yalnız günlüğe yazılır).</summary>
        public static event Action<SessionOutcome> OutcomeRecorded;

        private void OutcomeBegin(LevelData level, SessionKind kind, int bonus)
        {
            bool completed = _save.Data.IsCompleted(level.Id);
            bool first = kind == SessionKind.Normal && !completed && _save.MarkAttempted(level.Id);
            _outcome = new SessionOutcome
            {
                Kind = kind,
                LevelId = level.Id,
                ContentVersion = kind == SessionKind.Daily ? _daily?.Challenge?.ContentVersion ?? "" : kind == SessionKind.Normal ? ContentVersions.Levels : "custom",
                BaseMoveLimit = level.MoveLimit,
                MoveBonus = bonus,
                FirstAttempt = first,
                AttemptNumber = kind == SessionKind.Daily && _daily != null ? Math.Max(1, _daily.State.attemptsUsed) : 1,
                DailyKey = kind == SessionKind.Daily ? _daily?.State.Key : null,
                Assists = bonus > 0 ? AssistFlags.DailyMoveBonus : AssistFlags.None,
            };
            AnalyticsLevelStart();
        }

        private void OutcomeAssist(AssistFlags f)
        {
            if (_outcome != null) _outcome.Assists |= f;
        }

        /// <summary>Aynı oturumda yeniden başlatma: yeni deneme (ilk deneme değil); oturum ek hamlesi korunur.</summary>
        private void OutcomeRestart()
        {
            if (_outcome == null) return;
            var o = _outcome;
            _outcome = new SessionOutcome
            {
                Kind = o.Kind, LevelId = o.LevelId, ContentVersion = o.ContentVersion, BaseMoveLimit = o.BaseMoveLimit,
                MoveBonus = o.MoveBonus, FirstAttempt = false, AttemptNumber = o.AttemptNumber + 1, DailyKey = o.DailyKey,
                Assists = o.Assists & (AssistFlags.DailyMoveBonus | AssistFlags.Guidance),
            };
            AnalyticsLevelStart();
        }

        /// <summary>Bu oturumun son ZPE kaydı (oturum yeniden açılırsa geri çekilir; çift sayım yok).</summary>
        private ZpeRecord _zpeLast;
        private SessionOutcome _zpeLastOutcome;

        /// <summary>Kayıp kartından geri alma ya da reklamla ek hamle: oturum sürer; kayda geçmiş kayıp ZPE'den çekilir.</summary>
        private void OutcomeReopen()
        {
            if (_outcome == null) return;
            if (_zpeLast != null && ReferenceEquals(_zpeLastOutcome, _outcome))
            {
                Zpe.Retract(_save.Data.meta, _zpeLast, ZpeRef, _zpeConfig ?? new ZpeConfig());
                _zpeLast = null;
                _zpeLastOutcome = null;
                _save.Save();
            }
            _outcome.Finished = false;
            _outcome.Success = false;
        }

        private void OutcomeMoves(LevelState s)
        {
            if (_outcome != null && !_outcome.Finished) _outcome.MovesUsed = Math.Max(0, _outcome.MoveLimit - s.MovesLeft);
        }

        private void OutcomeFinish(bool success)
        {
            if (_outcome == null || _outcome.Finished) return;
            _outcome.Finished = true;
            _outcome.Success = success;
            if (!Application.isBatchMode) Debug.Log("[KatKat] Oturum sonucu: " + _outcome);
            OutcomeRecorded?.Invoke(_outcome);
            AnalyticsLevelEnd(success);
            ZpeRecordOutcome(_outcome);
        }

        // ---- v10 P5: ZPE (Zekâ Performans Endeksi; IQ değildir) ------------------------------------------------

        private ZpeConfig _zpeConfig;
        private Dictionary<string, ZpeLevelRef> _zpeRefs;

        private void SetupZpe()
        {
            try { _zpeConfig = ZpeConfig.Parse(Resources.Load<TextAsset>("Zpe/zpe_config")?.text); }
            catch (FormatException e) { Debug.LogError("[KatKat] " + e.Message); _zpeConfig = new ZpeConfig(); }
            try { _zpeRefs = Zpe.ParseRefs(Resources.Load<TextAsset>("Zpe/level_ref")?.text); }
            catch (FormatException e) { Debug.LogError("[KatKat] " + e.Message); _zpeRefs = new Dictionary<string, ZpeLevelRef>(); }
        }

        /// <summary>Güncel ZPE (kayıttaki ham girdilerden yeniden hesaplanır; algoritma sürümü değişse de tutarlı).</summary>
        public ZpeResult CurrentZpe() =>
            Zpe.Compute(_save.Data.meta, ZpeRef, _zpeConfig ?? new ZpeConfig());

        private ZpeLevelRef ZpeRef(string id) => id != null && _zpeRefs != null && _zpeRefs.TryGetValue(id, out var r) ? r : null;

        /// <summary>Son kayıttan önceki ZPE (bölüm sonu değişimi için).</summary>
        private int _zpeBefore = -1;

        /// <summary>Yalnız normal (katalog) bölüm sayılır; günlük bulmaca ve katalog dışı bölüm sayılmaz.</summary>
        private void ZpeRecordOutcome(SessionOutcome o)
        {
            if (o == null || o.Kind != SessionKind.Normal || string.IsNullOrEmpty(o.LevelId)) return;
            // v12 (kullanıcı, 2026-11-08): reklam yardımıyla (ek hamle) oynanan oturum ZPE'ye girmez.
            if (o.Has(AssistFlags.RewardedAd)) return;
            var before = CurrentZpe();
            _zpeBefore = before.Calibrating ? -1 : before.Score;
            _zpeLastOutcome = o;
            // vr1 P0 (02_ZPE): hız bileşeni yalnız karar süresinden (animasyon/öğretici/reklam/modal hariç).
            Zpe.Add(_save.Data.meta, _zpeLast = ZpeRecord.From(o, DateTimeOffset.UtcNow.ToUnixTimeSeconds()), ZpeRef, _zpeConfig ?? new ZpeConfig());
            _save.Save();
        }

        /// <summary>ZPE istatistik paneli (ana sayfada kapsüle dokununca).</summary>
        public void ShowZpeStats()
        {
            var z = CurrentZpe();
            _menu.ShowZpeStats(new MenuView.ZpeStatsModel
            {
                Calibrating = z.Calibrating,
                Score = z.Score,
                Count = z.Count,
                Min = z.MinLevels,
                Trend = z.Trend,
                WinPct = Mathf.RoundToInt((float)z.WinRate * 100f),
                FirstPct = Mathf.RoundToInt((float)z.FirstTryRate * 100f),
                Efficiency = (float)z.Average.Efficiency,
                Difficulty = (float)z.Average.Difficulty,
                FirstTry = (float)z.Average.FirstTry,
                Time = (float)z.Average.Time,
                NoAssist = (float)z.Average.NoAssist,
            });
        }

        /// <summary>Bölüm sonu ZPE değişimi metni ("ZPE 642 (+12)"); kalibrasyonda ilerleme; değişim yoksa null.</summary>
        public string ZpeChangeText()
        {
            var z = CurrentZpe();
            if (z.Count == 0) return null;
            if (z.Calibrating) return Loc.F("zpe.calibrating", z.Count, z.MinLevels);
            if (_zpeBefore < 0) return Loc.F("zpe.value", z.Score);
            int d = z.Score - _zpeBefore;
            return d == 0 ? null : Loc.F("zpe.change", z.Score, (d > 0 ? "+" : "") + d);
        }

        /// <summary>Uygulama odağı (bildirim perdesi, tam ekran reklam): odak yokken süre sayılmaz.</summary>
        private bool _appFocused = true;

        private void OnApplicationFocus(bool focus) => _appFocused = focus;

        /// <summary>
        /// vr1 P0 (02_ZPE): süre üç sayaca ayrılır. Karar: oyuncu girdi verebilirken (Idle/Targeting). Animasyon: hamle
        /// oynatması. Öğretici: zorunlu tur. Duraklatma/modal, menü, reklam, odak kaybı ve ipucu hazırlığı sayılmaz.
        /// </summary>
        private PlayTime OutcomeTimeKind()
        {
            if (_outcome == null || _outcome.Finished || _menu == null || _menu.IsOpen || _hud.PanelOpen || !_appFocused) return PlayTime.None;
            if (_adScreen != null && _adScreen.IsOpen) return PlayTime.None;
            if (_hintStage != HintStage.Idle) return PlayTime.None;
            if (CurrentPhase == Phase.Animating) return PlayTime.Animation;
            if (CurrentPhase != Phase.Idle && CurrentPhase != Phase.Targeting) return PlayTime.None;
            return _tourOn ? PlayTime.Tutorial : PlayTime.Decision;
        }

        private void OutcomeTick(float dt) => _outcome?.Tick(OutcomeTimeKind(), dt);

        // ---- B: koleksiyon ve günlük bulmaca ------------------------------------------------------------------

        /// <summary>SAHTE adaptörler (yerel prototip). Testler ve yakalama aracı değiştirebilir.</summary>
        public static readonly FakeRewardedAds FakeAds = new FakeRewardedAds();
        public static readonly FakeAdFree FakeAdFree = new FakeAdFree();
        /// <summary>Test/yakalama için gün sağlayıcısı (null: yerel saat).</summary>
        public static IDayClock ClockOverride;

        private DailyService _daily;
        private StoneCatalog _stones;
        private DailyIndex _dailyIndex;
        private WorldCatalog _worlds;
        private readonly List<StoneDefinition> _pendingStoneToasts = new List<StoneDefinition>();

        public DailyService Daily => _daily;
        public StoneCatalog Stones => _stones;
        /// <summary>v10 P1.5: dünya gruplaması (yalnız veri; harita görünümü değişmez).</summary>
        public WorldCatalog Worlds => _worlds;

        /// <summary>Günlük hizmeti kayıttaki meta nesnesine bağlar (açılışta ve buluttan geri yüklemeden sonra).</summary>
        private void CreateDaily()
        {
            // v10 P3.4: grup oyuncunun ulaştığı en yüksek bölüme göre (highestUnlocked 0 tabanlı → bölüm numarası +1).
            _daily = new DailyService(_save.Data.meta, ClockOverride ?? new LocalDayClock(), (d, t) => _dailyIndex.PickFor(d, t),
                () => _dailyIndex.TierFor(_save.Data.highestUnlocked + 1), _save.Save, Ads, FakeAdFree);
            _daily.Refresh();
        }

        private void SetupMeta()
        {
            SetupZpe();
            try { _stones = StoneCatalog.Parse(Resources.Load<TextAsset>("Collection/stones")?.text); }
            catch (FormatException e) { Debug.LogError("[KatKat] " + e.Message); _stones = new StoneCatalog(); }
            try { _worlds = WorldCatalog.Parse(Resources.Load<TextAsset>("Worlds/worlds")?.text); }
            catch (FormatException e) { Debug.LogError("[KatKat] " + e.Message); _worlds = WorldCatalog.Parse(null); }
            try { _dailyIndex = DailyIndex.Parse(Resources.Load<TextAsset>("Daily/daily_index")?.text); }
            catch (FormatException e) { Debug.LogError("[KatKat] " + e.Message); _dailyIndex = new DailyIndex(); }
            CreateDaily();
            // Eski kayıtla açılış: koşulu daha önce sağlanmış taş ilk açılışta verilir (tek seferlik).
            // v12: sıralama yıldızı alanı yeni; eski ilerlemede reklam yoktu, mevcut yıldızlar reklamsız sayılır (tek seferlik).
            if (_save.Data.meta.rankStars.Count == 0 && _save.Data.completed.Count > 0)
            {
                foreach (var id in _save.Data.completed) RankStars.Record(_save.Data.meta, id, Mathf.Max(1, _save.Data.StarsFor(id)));
                _save.Save();
            }
            var granted = CollectionRules.GrantUnlocked(_save.Data.meta, _stones, UnlockCtx());
            if (granted.Count > 0) { _save.Save(); _pendingStoneToasts.AddRange(granted); }
        }

        private int SessionMoveBonus(LevelData level, SessionKind kind) =>
            kind == SessionKind.Normal && _daily != null ? _daily.MoveBonusFor(_save.Data.IsCompleted(level.Id)) : 0;

        /// <summary>Hazine bulma bölümlere bağlı mı? Değilse Gizli Hazineler koleksiyonda gösterilmez.</summary>
        private const bool TreasuresAvailable = false;

        /// <summary>v10 P2.1: kazanım koşullarının oyuncu durumu. Hazine (P2.3) henüz yok: hiçbiri sağlanmaz.</summary>
        private UnlockContext UnlockCtx() => new UnlockContext
        {
            IsLevelCompleted = _save.Data.IsCompleted,
            TotalStars = _save.Data.TotalStars,
            HasTreasure = null,
            IsWorldCompleted = IsWorldCompleted,
        };

        private bool IsWorldCompleted(string worldId)
        {
            if (_worlds == null) return false;
            foreach (var w in _worlds.All(_catalog.Count))
                if (w.WorldId == worldId) return WorldCatalog.CompletedIn(w, _catalog.IdAt, _save.Data.IsCompleted) == w.LevelCount;
            return false;
        }

        /// <summary>Kilitli taşın kazanım koşulu metni (türüne göre). Gizli hazine koşulu açık edilmez.</summary>
        private string UnlockText(StoneDefinition s)
        {
            switch (s.UnlockType)
            {
                case StoneUnlock.StarsTotal: return Loc.F("stone.unlock.stars", s.UnlockValue);
                case StoneUnlock.WorldCompleted:
                    int n = 0;
                    if (_worlds != null) foreach (var w in _worlds.All(_catalog.Count)) if (w.WorldId == s.UnlockKey) n = w.Number;
                    return Loc.F("stone.unlock.world", n);
                case StoneUnlock.Treasure: return Loc.T("stone.unlock.treasure");
                default: return Loc.F("stone.unlock.level", s.UnlockValue);
            }
        }

        private void CheckStoneUnlocks()
        {
            var granted = CollectionRules.GrantUnlocked(_save.Data.meta, _stones, UnlockCtx());
            if (granted.Count == 0) return;
            _save.Save();
            _pendingStoneToasts.AddRange(granted);
        }

        /// <summary>Kazanılan taş bildirimi: kazanç kartından sonraki ilk ekranda (harita ya da sonraki bölüm) bir kez.</summary>
        private void FlushStoneToasts(bool inGame)
        {
            if (_pendingStoneToasts.Count == 0) return;
            // v10 (kullanıcı): kazanılan taş ekrana vitrinle çıkar (3D model varsa döner, yoksa büyük görsel); birden
            // fazlaysa kapatınca sıradaki gelir.
            var s = _pendingStoneToasts[0];
            _pendingStoneToasts.RemoveAt(0);
            ShowStone(s.StoneId, true, () => FlushStoneToasts(inGame));
        }

        /// <summary>
        /// Taş vitrini: <paramref name="isNew"/> ise "Yeni koleksiyon taşı" başlığıyla (kazanım anı). Yalnız sahip olunan
        /// taş gösterilir (kilitli taşın görünümü açık edilmez).
        /// </summary>
        public void ShowStone(string stoneId, bool isNew, Action onClose = null)
        {
            var s = _stones.Find(stoneId);
            if (s == null || !_save.Data.meta.stones.Contains(stoneId)) { onClose?.Invoke(); return; }
            string name = Loc.T(s.NameKey);
            _showcase.Show(s.StoneId, isNew ? Loc.F("stone.new", name) : "", name, Loc.T(s.DescKey), StoneSprite(s), GlowColor(s), onClose);
        }

        /// <summary>
        /// Ana sayfa yıldız çubuğu: sıradaki kazanılmamış yıldız eşiği taşı (toplam yıldız eşiğine göre sıralı). Önceki
        /// eşikten (ya da 0) bu eşiğe dolum; renk taşın ışıltı rengi. Bütün yıldız taşları alınmışsa çubuk yok.
        /// </summary>
        public MenuView.StarGoalInfo NextStarGoal()
        {
            var info = new MenuView.StarGoalInfo();
            if (_stones == null) return info;
            int stars = _save.Data.TotalStars;
            var list = _stones.Stones.FindAll(s => s.Category == StoneCategory.StarStone && s.UnlockType == StoneUnlock.StarsTotal);
            list.Sort((a, b) => a.UnlockValue.CompareTo(b.UnlockValue));
            int from = 0;
            foreach (var s in list)
            {
                if (_save.Data.meta.stones.Contains(s.StoneId) || stars >= s.UnlockValue) { from = s.UnlockValue; continue; }
                info.Has = true;
                info.Stars = stars;
                info.From = from;
                info.To = s.UnlockValue;
                // Kullanıcı (2026-11-08): kilidi açılmayı bekleyen taş yoğun bulanık (koleksiyondaki kilitli kartla aynı kaynak).
                KatKat.View.ResourceUsage.Note("Collection/stones_blur/" + s.StoneId);
            info.StoneSprite = Resources.Load<Sprite>("Collection/stones_blur/" + s.StoneId) ?? StoneSprite(s);
                info.Glow = GlowColor(s);
                break;
            }
            return info;
        }

        /// <summary>Işıltı rengi: katalogdaki "glow"; yoksa vurguya göre (sıcak altın / soğuk turkuaz).</summary>
        public static Color GlowColor(StoneDefinition s)
        {
            if (s != null && !string.IsNullOrEmpty(s.Glow) && ColorUtility.TryParseHtmlString(s.Glow, out var c)) return c;
            return s != null && s.Accent == "warm" ? new Color32(0xFF, 0xC8, 0x48, 0xFF) : new Color32(0x5E, 0xD6, 0xFF, 0xFF);
        }

        /// <summary>Taş görseli: artKey'e ait sprite yoksa ortak yer tutucu (mevcut renk taşı simgesi).</summary>
        public static Sprite StoneSprite(StoneDefinition s)
        {
            if (s != null) KatKat.View.ResourceUsage.Note(s.ArtKey);
            var sp = s != null && !string.IsNullOrEmpty(s.ArtKey) ? Resources.Load<Sprite>(s.ArtKey) : null;
            return sp != null ? sp : KatKat.View.PremiumArt.Chip(LayerColor.Blue);
        }

        private bool DailyPowerReady =>
            _sessionKind == SessionKind.Daily && _daily != null && _daily.State.attemptOpen && !_daily.State.powerUsed
            && CollectionRules.ActiveStone(_save.Data.meta) != null && _session != null && StonePowers.CanApply(_stones.Find(CollectionRules.ActiveStone(_save.Data.meta))?.PowerId, _session.State);

        private void AfterBegin()
        {
            if (_sessionKind == SessionKind.Trial) { SetPhase(Phase.Idle); TrialAfterBegin(); return; }
            _hud.PlaceRollBesideTray(_sessionKind == SessionKind.Daily && _session.State.RollEnabled && _session.State.SpecialsEnabled);
            if (_sessionKind == SessionKind.Daily)
            {
                var s = _session.State;
                _hud.SetLevel(Loc.Upper(Loc.T("daily.tag")), s.MovesLeft, s.Goals, s.RollEnabled, _daily.State.attemptsUsed);
                _hud.SetDailyHeader(_daily.State.dayId); // ui1/17
                if (HasRules(_level))
                {
                    // daily-v3 (PLAN.md §6.3): günün türü — kural simgeleri ve her günlükte kural kartı (strateji verilmez).
                    var rules = RuleLines(_level);
                    _hud.SetRuleIcons(rules.Count > 0 ? rules : null);
                    _hud.ShowRuleCard(MainTypeTitle(_level), Loc.T("trial.intro." + _level.TrialType), rules, null);
                }
                SetPhase(Phase.Idle);
                if (CollectionRules.ActiveStone(_save.Data.meta) != null) _hud.ShowToast(Loc.T("daily.power.hint"), 3f);
            }
            else if (_outcome != null && _outcome.MoveBonus > 0)
                _hud.ShowToast(Loc.F("daily.bonus.toast", _outcome.MoveBonus), 2.6f);
            else if (_sessionKind == SessionKind.Normal)
                FlushStoneToasts(true);
        }

        /// <summary>
        /// v11 (kullanıcı, 2026-11-08): günlük bulmacada seçili taş alt güç alanında gerçek görseliyle; normal bölümde
        /// ve seçili taş yokken gizli. Kullanılınca soluk; hazırsa dokunulabilir ve parlar.
        /// </summary>
        private void RefreshStonePower()
        {
            if (_hud == null) return;
            Sprite sp = null;
            bool used = false, ready = false;
            if (_sessionKind == SessionKind.Daily && _daily != null && _session != null && CollectionRules.ActiveStone(_save.Data.meta) is string id)
            {
                sp = StoneSprite(_stones.Find(id));
                used = _daily.State.powerUsed;
                ready = CurrentPhase == Phase.Idle && DailyPowerReady;
            }
            var def = sp != null ? _stones.Find(CollectionRules.ActiveStone(_save.Data.meta)) : null;
            _hud.SetStonePower(sp, used, ready, def != null ? GlowColor(def) : default, def != null ? Loc.T(def.NameKey) : null);
        }

        /// <summary>Günlük: taş gücü yuvası (ve geriye uyum için boş rulo düğmesi) gücü kullanır (doğrulama + harcama tek işlemde).</summary>
        private bool TryDailyPower()
        {
            if (_sessionKind != SessionKind.Daily || CurrentPhase != Phase.Idle) return false;
            var r = _daily.TryUsePower(_stones, _session.State);
            switch (r)
            {
                case PowerResult.Used:
                    OutcomeAssist(AssistFlags.StonePower);
                    // v10 P3.2: güç ailesine göre değişen şey gösterilir (rulo yuvası, tepsi, hamle).
                    var st = _session.State;
                    if (st.RollSlotFull) _hud.PulseRoll();
                    _hud.SetTray(st.Tray, _traySlot = -1);
                    _hud.SetMoves(st.MovesLeft);
                    _hud.ShowToast(Loc.T("daily.power.used"), 2.4f);
                    SetPhase(Phase.Idle);
                    return true;
                case PowerResult.AlreadyUsed: _hud.ShowToast(Loc.T("daily.power.spent"), 2.2f); return false;
                case PowerResult.NoStone: _hud.ShowToast(Loc.T("daily.power.nostone"), 2.2f); return false;
                default: _hud.ShowToast(Loc.T("daily.power.invalid"), 2.2f); return false;
            }
        }

        // ---- Günlük ekran ve oturum ----------------------------------------------------------------------

        public void ShowDaily()
        {
            _bgPick = -1; // ui3: menüden yeni girişte oyun zemini yeniden seçilir
            AbandonDailyIfOpen();
            _hud.HidePanel();
            // v10 (kullanıcı): günlük, haritanın üstünde modal; harita açık değilse önce harita kurulur.
            if (!_menu.IsMapShown) ShowLevels();
            SetPhase(Phase.Loading);
            _daily.Refresh();
            var d = _daily.State;
            var model = new MenuView.DailyModel
            {
                Available = _daily.Available,
                Entered = d.entered,
                Won = d.won,
                AttemptOpen = d.attemptOpen,
                AttemptsLeft = _daily.AttemptsLeft,
                AttemptsAllowed = _daily.AttemptsAllowed,
                AdFree = FakeAdFree.Owned,
                BonusToday = _daily.HasMoveBonusToday,
                StoneName = CollectionRules.ActiveStone(_save.Data.meta) is string sid ? Loc.T(_stones.Find(sid)?.NameKey ?? sid) : null,
                StoneSprite = CollectionRules.ActiveStone(_save.Data.meta) is string sid2 ? StoneSprite(_stones.Find(sid2)) : null,
                DayId = d.dayId,
                Tier = _daily.Tier,
                TierMin = _dailyIndex.TierInfo(_daily.Tier)?.MinLevel ?? 0,
                TierMax = _dailyIndex.TierInfo(_daily.Tier)?.MaxLevel ?? 0,
            };
            _menu.ShowDaily(model, OnDailyEnter, StartDaily, ShowLevels, ShowCollection, ShowSettings);
        }

        private void OnDailyEnter()
        {
            _daily.RequestEntry(r =>
            {
                ShowDaily();
                if (r == DailyEntryResult.AdCancelled || r == DailyEntryResult.AdFailed)
                    _menu.ShowStoneToast(Loc.T(r == DailyEntryResult.AdCancelled ? "daily.ad.cancel" : "daily.ad.fail"), null);
            });
        }

        /// <summary>Elle deneme/yakalama: sahte reklamın sonraki sonucunu ayarlayıp girişe basar.</summary>
        public void PressDailyEnterForTest() => OnDailyEnter();

        /// <summary>Günlük denemeyi açar (ya da kapanmadan kalan aynı denemeye döner) ve bulmacayı başlatır.</summary>
        public bool StartDaily()
        {
            var r = _daily.BeginAttempt();
            if (r != DailyBeginResult.Started && r != DailyBeginResult.Resumed) { ShowDaily(); return false; }
            var level = LoadDailyLevel();
            if (level == null) { _daily.FinishAttempt(false); ShowContentError(Loc.T("err.notfound")); return false; }
            _menu.Hide();
            _music.Leave();
            _hud.HidePanel();
            _levelIndex = 0;
            _nextKind = SessionKind.Daily;
            Begin(level, DailyThemeIndex);
            return true;
        }

        /// <summary>Günlük bulmacanın teması (ilk bölge: sahil). Görsel tema, bölüm ilerlemesi değildir.</summary>
        private const int DailyThemeIndex = 0;

        private LevelData LoadDailyLevel()
        {
            var info = _daily.Challenge;
            var asset = info != null ? Resources.Load<TextAsset>("Daily/" + info.LevelFile) : null;
            if (asset == null) return null;
            try { return LevelParser.Parse(asset.text, info.LevelFile + ".json"); }
            catch (FormatException e) { Debug.LogError("[KatKat] Günlük bulmaca okunamadı: " + e.Message); return null; }
        }

        private void OnDailyWon()
        {
            // Kullanıcı (2026-11-08): günlük de yıldız kazandırır — performansa göre 1–3 (bölüm eşikleriyle aynı), günde bir
            // kez (ilk galibiyet). Toplam yıldıza ve taş eşiklerine sayılır; sıralamaya sayılmaz (giriş reklamla).
            int stars = Mathf.Clamp(_session.State.Stars, 1, 3);
            bool rewarded = _daily.FinishAttempt(true, stars);
            if (rewarded) CheckStoneUnlocks();
            _sound.Play(Sfx.Win);
            Haptics.Medium();
            _hud.ShowPanel(Loc.T("daily.win.title"), rewarded ? Loc.F("daily.win.stars", stars, _daily.Config.MoveBonus) : Loc.T("daily.win.again"),
                new HudView.PanelButton(Loc.T("btn.levels"), UiIcon.Play, true, ShowLevels),
                new HudView.PanelButton(Loc.T("daily.title"), UiIcon.Menu, false, ShowDaily));
            _hud.PanelStars(rewarded ? stars : 0);
        }

        private void ShowDailyLost(GameStatus status)
        {
            bool moves = status == GameStatus.LostOutOfMoves;
            bool canRetry = _daily.AttemptsLeft > 0;
            var buttons = new List<HudView.PanelButton>
            {
                new HudView.PanelButton(Loc.T("btn.undo"), UiIcon.Undo, true, Undo),
            };
            if (canRetry) buttons.Add(new HudView.PanelButton(Loc.T("daily.retry"), UiIcon.Restart, false, DailyRestart));
            buttons.Add(new HudView.PanelButton(Loc.T("daily.leave"), UiIcon.Menu, false, LeaveDaily));
            _hud.ShowPanel(moves ? Loc.T("lose.moves.title") : Loc.T("lose.stuck.title"),
                canRetry ? Loc.T("daily.lost.retry") : Loc.T("daily.lost.final"), buttons.ToArray());
        }

        /// <summary>Günlükte yeniden başlatma = açık denemeyi bitirip ücretsiz tekrarı açmak (hak yoksa olmaz).</summary>
        private void DailyRestart()
        {
            if (_daily.AttemptsLeft <= 0) { _hud.ShowToast(Loc.T("daily.lost.final"), 2.4f); return; }
            _daily.FinishAttempt(false);
            StartDaily();
        }

        /// <summary>Günlükten bilerek çıkış denemeyi bitirir (kapanma/çökme değil; o durumda aynı deneme yeniden açılır).</summary>
        private void LeaveDaily()
        {
            AbandonDailyIfOpen();
            ShowDaily();
        }

        public void ShowCollection()
        {
            _hud.HidePanel();
            SetPhase(Phase.Loading);
            var items = new List<MenuView.StoneItem>();
            string active = CollectionRules.ActiveStone(_save.Data.meta);
            // Bölüm sırası: yıldız taşları, gizli hazineler, efsanevi taşlar; bölüm içinde katalog sırası.
            foreach (var (cat, titleKey) in new[] { (StoneCategory.StarStone, "collection.cat.star"), (StoneCategory.HiddenTreasure, "collection.cat.treasure"), (StoneCategory.LegendaryStone, "collection.cat.legend") })
                foreach (var s in _stones.InCategory(cat))
                {
                    bool owned = _save.Data.meta.stones.Contains(s.StoneId);
                    // Hazine bulma (P2.3) bölümlere bağlanmadı (UnlockCtx: HasTreasure = null): kazanılamayan taşlar
                    // koleksiyonda gösterilmez (2026-11-11). Hazine bağlanınca TreasuresAvailable açılır.
                    if (cat == StoneCategory.HiddenTreasure && !TreasuresAvailable && !owned) continue;
                    bool hidden = !owned && cat == StoneCategory.HiddenTreasure; // hazinenin adı bulunana kadar gizli
                    items.Add(new MenuView.StoneItem
                    {
                        Id = s.StoneId,
                        Section = cat,
                        SectionTitle = Loc.T(titleKey),
                        Name = hidden ? "???" : Loc.T(s.NameKey),
                        Desc = owned ? Loc.T(s.DescKey) : UnlockText(s),
                        Stars = !owned && s.UnlockType == StoneUnlock.StarsTotal ? s.UnlockValue : 0,
                        Sprite = StoneSprite(s),
                        Owned = owned,
                        Hidden = hidden,
                        Warm = s.Accent == "warm",
                        Selected = owned && active == s.StoneId,
                    });
                }
            _menu.ShowCollection(items, ShowDaily, ShowLevels, ShowSettings);
            _music.Enter();
        }

        /// <summary>Günlük oturumdan menüye dönülürken açık deneme bitirilir (bilerek çıkış hakkı harcar).</summary>
        private void AbandonDailyIfOpen()
        {
            if (_sessionKind != SessionKind.Daily) return;
            if (_daily != null && _daily.State.attemptOpen)
            {
                OutcomeFinish(false);
                _daily.FinishAttempt(false);
            }
            _sessionKind = SessionKind.Custom;
        }

        // ---- Yakalama/test yardımcıları -------------------------------------------------------------------------

        public bool UseDailyPowerForTest() => TryDailyPower();
    }
}
