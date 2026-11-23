using System.Collections.Generic;
using KatKat.Core;
using KatKat.Levels;
using KatKat.Save;
using KatKat.UI;
using KatKat.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KatKat.Game
{
    /// <summary>
    /// Oyun sahnesinin yöneticisi. Girdi kapısı: yalnızca Idle'da dokunuş kabul edilir; animasyon sürerken
    /// gelen dokunuşlar yok sayılır, kuyruğa alınmaz. Kural motoru komutu anında çözer; görünüm olay
    /// planını oynatır ve bitince mantıksal son durumdan yeniden çizer.
    /// </summary>
    public sealed partial class GameController : MonoBehaviour
    {
        public enum Phase { Loading, Idle, Animating, Targeting, Ended, Error }

        private Camera _cam;
        private BoardView _board;
        private PeelDirector _director;
        private HudView _hud;
        private TapHint _hint;
        private LevelCatalog _catalog;
        private GameSession _session;
        private LevelData _level;
        private int _levelIndex;
        private int _previewRow = -1;
        private Vector2Int _screen;
        private bool _hintActive;
        private SaveService _save;
        private SoundBank _sound;
        private MenuMusic _music;
        private KatKat.UI.StoneShowcase _showcase;
        /// <summary>Taş vitrini (testler ve yakalama).</summary>
        public KatKat.UI.StoneShowcase Showcase => _showcase;
        /// <summary>Menü müziği (testler için).</summary>
        public MenuMusic Music => _music;
        /// <summary>Efekt bankası (testler için).</summary>
        public SoundBank Sound => _sound;
        private bool _chainSounded;
        private MenuView _menu;
        private Backdrop _backdrop;
        private MoveResult _lastResult;

        public Phase CurrentPhase { get; private set; } = Phase.Loading;
        public GameSession Session => _session;
        public BoardView Board => _board;
        public HudView Hud => _hud;
        public MenuView Menu => _menu;
        public SaveService SaveService => _save;
        public int LevelIndex => _levelIndex;
        public LevelCatalog Catalog => _catalog;
        public LevelData CurrentLevel => _level;

        /// <summary>Sahne açılınca doğrudan başlatılacak bölüm; negatifse ana ekran açılır (testler ve editör menüsü kullanır).</summary>
        public static int RequestedLevel = -1;

        private void Start()
        {
            Setup(Camera.main, new SaveService(SaveDirectoryOverride));
            int requested = RequestedLevel;
#if UNITY_EDITOR
            // "Kat Kat/Seviyeler/Bölüm oyna" penceresi: sahneyi elle değiştirmeden bölüm seçimi.
            int editorLevel = UnityEditor.EditorPrefs.GetInt(EditorPlayLevelKey, -1);
            if (editorLevel >= 0)
            {
                requested = editorLevel;
                UnityEditor.EditorPrefs.DeleteKey(EditorPlayLevelKey);
            }
#endif
            if (_reopenSettings) { _reopenSettings = false; ShowSettings(); }
            else if (requested >= 0) StartLevel(Mathf.Clamp(requested, 0, Mathf.Max(0, _catalog.Count - 1)));
            else ShowHome();
        }

        private static bool _reopenSettings;

        /// <summary>Dil değişti: bütün ekranlar (HUD dahil) yeni dille kurulsun diye sahne yeniden yüklenir; ayarlar açık döner.</summary>
        private void OnLanguagePicked(string code)
        {
            Loc.Set(code);
            _reopenSettings = true;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }

        public const string EditorPlayLevelKey = "KatKat.EditorPlayLevel";

        /// <summary>Testler gerçek oyuncu kaydına yazmasın diye kayıt klasörü; boşsa persistentDataPath.</summary>
        public static string SaveDirectoryOverride;

        /// <summary>Sahne nesnelerini kurar. Editör ekran yakalama aracı da doğrudan çağırır.</summary>
        public void Setup(Camera cam, SaveService save)
        {
            _save = save;
            Haptics.Enabled = save.Data.vibration;
            _cam = cam;
            _cam.orthographic = true;
            _cam.backgroundColor = UiArt.Ivory;
            // Sahnelerde AudioListener yok: dinleyici olmadan efekt ve müzik çalar görünür ama cihazdan ses çıkmaz.
            if (FindAnyObjectByType<AudioListener>() == null) _cam.gameObject.AddComponent<AudioListener>();
            _backdrop = Backdrop.Create(transform);
            _backdrop.SetSprite(PremiumArt.Background(0));
            _director = new GameObject("PeelDirector").AddComponent<PeelDirector>();
            _director.transform.SetParent(transform, false);
            _board = new GameObject("Board").AddComponent<BoardView>();
            _board.transform.SetParent(transform, false);
            _board.Init(_director);
            _hint = TapHint.Create(_board.transform, UiArt.Icon(UiIcon.Ring), UiArt.Icon(UiIcon.Back), UiArt.Circle);
            _hud = HudView.Create(cam);
            _hud.transform.SetParent(transform, false);
            _hud.BackPressed += BackToLevels;
            _hud.PausePressed += OpenPause;
            _hud.UndoPressed += Undo;
            _hud.HintPressed += OnHintPressed;
            _hud.RestartPressed += Restart;
            _hud.RollPressed += ToggleRollTargeting;
            _hud.StonePowerPressed += () => { if (CurrentPhase == Phase.Idle) TryDailyPower(); };
            _hud.PaintPressed += TogglePaint;
            _hud.PaintColorChosen += OnPaintColor;
            _hud.TraySlotPressed += OnTraySlot;
            _hud.PlaceStoneChosen += OnPlaceStoneChosen;
            _board.TrayTarget = () => _hud.TraySlotPosition(Mathf.Max(0, _session.State.Tray.Count - 1));
            _board.SpongeTarget = () => _hud.TokenTarget(GoalType.CollectSponges);
            _board.EnvelopeTarget = _hud.EnvelopeTarget;
            _board.ScoreTarget = _hud.ScoreTarget;
            _board.ScoreBarRect = _hud.ScoreBarWorldRect;
            _board.VeilCamera = _cam; // taş akışı perdesi (2026-11-19)
            _board.GoalFocus = _hud.SetFlowFocus; // akıştaki rengin görev simgesi büyür (2026-11-19)
            _board.StarFocus = _hud.SetStarFocus; // sıradaki yıldız büyür
            // Taş akışı titreşim ve kasa sesi (2026-11-19): Titreşim ve Ses ayarlarına bağlı (Haptics.Enabled, SoundBank.Enabled).
            _board.FlowRise = lvl => Haptics.Level(lvl);
            // 2026-11-22 (kullanıcı: "yıldıza ve görev taşlarına gidince taş sesleri sayısına göre tekrar edecek"): varan her taşta
            // jöle taş sesi (hamle içinde yavaşça tizleşir), son taş varınca görev taşı sesi. Eski para sesleri yalnız V17 yoksa.
            _board.FlowTick = i => { if (!_sound.PlayV17("jelly-pop", Mathf.Min(7f, i * 0.5f), 0.8f)) _sound.Play(Sfx.Coin, i / 2, 0.75f); Haptics.Tick(); };
            _board.FlowFinal = () => { if (!_sound.PlayV17("goal-collect")) _sound.Play(Sfx.CoinFinal, 0, 0.85f); Haptics.Medium(); };
            _board.IsGoalColor = _hud.IsGoalColor;
            _sound = SoundBank.Create(transform);
            SetupVr1();
            _sound.Enabled = save.Data.sound;
            KatKat.UI.Motion.Reduced = save.Data.reducedMotion;
            _music = MenuMusic.Create(transform);
            _music.Enabled = save.Data.sound;
            _menu = MenuView.Create(cam);
            _menu.transform.SetParent(transform, false);
            _menu.LevelChosen += i => StartLevel(i);
            // v10: tek ses ayarı hem efektleri hem müziği anında kontrol eder.
            _menu.SoundChanged += v => { _save.Data.sound = v; _sound.Enabled = v; _music.Enabled = v; _save.Save(); };
            _menu.VibrationChanged += v => { _save.Data.vibration = v; Haptics.Enabled = v; _save.Save(); if (v) Haptics.Light(); };
            _menu.MotionChanged += v => { _save.Data.reducedMotion = v; KatKat.UI.Motion.Reduced = v; _save.Save(); };
            // Bildirimler (2026-11-13): açınca sistem izni istenir; izin yoksa kapalı kalır ve oyuncuya söylenir.
            _menu.NotifyEnabled = () => GameNotifications.Enabled;
            _menu.NotifyChanged += v =>
            {
                GameNotifications.Asked = true;
                if (!v) { GameNotifications.Enabled = false; GameNotifications.CancelAll(); GameAnalytics.Log("notify_off"); return; }
                StartCoroutine(GameNotifications.RequestPermission(ok =>
                {
                    GameNotifications.Enabled = ok;
                    GameAnalytics.Log("notify_permission", ("granted", ok ? 1 : 0));
                    _menu.ShowToast(Loc.T(ok ? "notify.on" : "notify.denied"), 2.8f);
                }));
            };
            GameNotifications.CancelAll(); // açılış: önceki planı temizle (oyun açıkken bildirim yok)
            _menu.LanguagePicked += OnLanguagePicked;
            _menu.Hide();
            _menu.UnlockAll = TestUnlockAll;
            _menu.CapsOf = i => i >= 0 && i < _catalog.Count ? (IList<string>)_catalog.Levels[i].Capabilities : null;
            _menu.StarsOf = i => i >= 0 && i < _catalog.Count ? _save.Data.StarsFor(_catalog.IdAt(i)) : 0;
            _menu.LevelCount = () => _catalog.Count; // ui1 üst çubuğu (03/09) yıldız toplamı
            _menu.ExtraStars = () => _save.Data.meta.dailyStars;
            _catalog = LevelCatalog.Load();
            SetupMeta();
            _menu.DailyPressed = ShowDaily;
            _menu.PilotPressed = ShowPilots; // vr1 pilot katalog (yalnız prototip anahtarı açıkken görünür)
            _menu.CollectionPressed = ShowCollection;
            // v10 taş vitrini: koleksiyonda kazanılmış karta dokununca ve taş kazanılınca (3D model ya da büyük PNG).
            _showcase = KatKat.UI.StoneShowcase.Create(cam, transform);
            // v12: reklam yapılandırması, geçiş kuralları ve test reklam ekranı.
            SetupAds(cam);
            SetupHearts();
            SetupAccount();
            SetupShop(); // 2026-11-20 mağaza, külçe, güçlendiriciler
            _menu.NeedsSignIn = () => Account.State != AccountState.SignedIn;
            _menu.StonePreviewRequested = id => ShowStone(id, false);
            _menu.ZpePressed = ShowZpeStats;
            _menu.RankPressed = ShowRanking;
            if (LiveMatchEnabled) _menu.LivePressed = () => _menu.ShowLiveIntro(ShowLive); // 2026-11-22: önce bilgi kartı, "Hazırım" ile maç // canlı maç (Sıralama altındaki giriş); kapalıysa düğme kurulmaz
            _menu.TrialPressed = ShowTrials;
            _menu.RankTabPressed = scope => ShowRanking(scope);
            _menu.RankRetryPressed = () => ShowRanking(_rankScope);
            FitCamera();
        }

        // ---- Menüler -------------------------------------------------------------------------------

        /// <summary>Test sürümü: haritadaki bütün bölümler açık (kayıt değişmez; mevcut bölüm yine kayıttan). Yayın hazırlığında kapatıldı (kullanıcı, 2026-11-09: "yayın için eksikleri tamamlayalım").</summary>
        public static bool TestUnlockAll =
#if KATKAT_UNLOCK_ALL
            true; // kullanıcı 2026-11-15: "tüm bölümleri açıp apk derler misin" → yalnız deneme APK'sı (BuildAndroidUnlockAll)
#else
            false;
#endif

        /// <summary>
        /// Test yapısı: SB Gold hiç bitmez (kullanıcı 2026-11-20: "ios a test yolla ve sınırsız külçem olsun"). Yalnız
        /// KATKAT_TEST_GOLD derlemesi (iOS iş akışında "Sınırsız SB Gold" seçeneği); bu yapı App Store incelemesine gönderilmez.
        /// </summary>
        public static bool TestGold =
#if KATKAT_TEST_GOLD
            true;
#else
            false;
#endif

        /// <summary>v5: ana sayfa = harita (eski ana ekran yakalama/karşılaştırma için korunur).</summary>
        public static bool UseMapAsHome = true;

        private int ContinueIndex => Mathf.Clamp(_save.Data.highestUnlocked, 0, Mathf.Max(0, _catalog.Count - 1));

        public void ShowHome()
        {
            AbandonDailyIfOpen();
            _hud.HidePanel();
            SetPhase(Phase.Loading);
            int done = 0;
            for (int i = 0; i < _catalog.Count; i++) if (_save.Data.IsCompleted(_catalog.IdAt(i))) done++;
            // v5 (kullanıcı referansı): ana sayfa doğrudan bölüm yoludur; açılışta mevcut bölüm ortada.
            if (UseMapAsHome) { ShowLevels(); return; }
            _menu.ShowHome(ContinueIndex, done, _catalog.Count, () => StartLevel(ContinueIndex), ShowLevels, ShowSettings);
        }

        /// <summary>ui3 oyun zemini: seçilen Z (0..4), anahtarı (oturum türü + bölüm) ve kozmetik RNG. Yakalama sabitleyebilir.</summary>
        private int _bgPick = -1;
        private string _bgKey;
        private static readonly System.Random CosmeticRng = new System.Random();
        public static int? BackgroundOverride;
        /// <summary>Yakalama/test: VR2 temasını (0..5) bölgeden bağımsız seç; null → <see cref="Vr2.SkinFor"/>.</summary>
        public static int? Vr2SkinOverride;
        /// <summary>Arka plan hareketi (BgMotion) açık mı; yakalama/test için kapatılabilir.</summary>
        public static bool BgMotionEnabled = true;
        public int BackgroundPick => _bgPick;

        public void ShowLevels()
        {
            HeartForfeit();
            ApplyCloudRestore(); // bölüm sırasında gelen bulut kaydı haritaya dönünce uygulanır
            _bgPick = -1; // menüye dönüş: sonraki girişte zemin yeniden seçilir
            AbandonDailyIfOpen();
            _hud.HidePanel();
            SetPhase(Phase.Loading);
            bool revealPending = PrepareMapGoal(); // vr1 P1: dönüş özeti ve (anahtar açıksa) buzlu taş
            var zpe = CurrentZpe();
            _menu.ZpeBadge = new MenuView.ZpeBadgeInfo { Has = true, Calibrating = zpe.Calibrating, Score = zpe.Score, Count = zpe.Count, Min = zpe.MinLevels };
            _menu.ShowLevels(_catalog.Count, ContinueIndex, i => _save.Data.IsCompleted(_catalog.IdAt(i)), ShowHome, -1, ShowSettings);
            _music.Enter();
            // Açılan taş: önce kabuk kırılır, vitrin sonra (vr1 P1). Kayıt hatırlatması (2026-11-18) vitrinlerden sonra.
            if (!revealPending) FlushStoneToasts(false, () => { if (_menu.IsMapShown) MaybeRemindSignIn(); });
            CloudRetry();
        }

        private void ShowSettings()
        {
            _music.Enter();
            _menu.ShowSettings(_save.Data.sound, _save.Data.vibration, ShowHome, ShowLevels);
        }

        private void BackToLevels()
        {
            if (CurrentPhase == Phase.Animating) return;
            if (CurrentPhase == Phase.Targeting) CancelTargeting();
            ShowLevels();
        }

        /// <summary>Tuval referans yüksekliği / 200 = ortografik boyut: 1 dünya birimi = 100 referans birimi.</summary>
        private void FitCamera()
        {
            float aspect = _cam.aspect > 0f ? _cam.aspect : 9f / 16f;
            // Telefonda genişlik 1080 birim; geniş ekranda (iPad) yükseklik 1920 birim (UiAspect).
            float refHeight = UiAspect.CanvasRef(aspect).y;
            _cam.orthographicSize = refHeight / 200f;
            _screen = new Vector2Int(Screen.width, Screen.height);
            _backdrop.Fit(_cam);
        }

        /// <summary>Öğretici halkanın (ilk hamle) gösterildiği bölümler. v14: 1–3 turlar, 4–5 vitrin mekanikleri; sonra gerçek tanıtımlar
        /// (ataç 21, yıldız 41, buz 61, renk kilidi 121, zarf 201, döner taban 301).</summary>
        private static readonly int[] HintLevels = { 0, 1, 2, 3, 4, 20, 40, 60, 120, 200, 300 };

        public void StartLevel(int index)
        {
            // Kalp (2026-11-09): bırakılan deneme kalp götürür; kalp yoksa bölüm açılmaz, kalp kartı çıkar.
            HeartForfeit();
            if (!HeartGate(index, () => StartLevel(index))) return;
            if (!OfflineGate()) return;
            _menu.Hide();
            _music.Leave();
            _hud.HidePanel();
            _levelIndex = index;
            if (index < 0 || index >= _catalog.Count)
            {
                ShowContentError(Loc.T("err.notfound"));
                return;
            }
            var level = _catalog.Levels[index];
            if (level == null) { ShowContentError(Loc.T("err.notfound")); return; }
            _nextKind = KatKat.Meta.SessionKind.Normal;
            Begin(level, index);
        }

        /// <summary>Katalog dışı bölüm (ekran yakalama aracı, mekanik denemesi); oynanış akışı aynıdır.</summary>
        public void StartCustom(LevelData level, int displayIndex)
        {
            _menu.Hide();
            _music.Leave();
            _hud.HidePanel();
            _levelIndex = displayIndex;
            _nextKind = KatKat.Meta.SessionKind.Custom;
            Begin(level, displayIndex);
        }

        private void Begin(LevelData level, int index)
        {
            _level = level;
            var errors = LevelValidator.ValidateStatic(_level, LevelValidator.ScopeFor(_level));
            if (errors.Count > 0)
            {
                // Hatalı veri oyuncunun kaybı değildir: oynatma, geliştiriciye raporla.
                foreach (var e in errors) Debug.LogError("[KatKat] İçerik hatası: " + e);
                ShowContentError(Loc.F("err.load", _level.Id));
                return;
            }
            _sessionKind = _nextKind;
            _nextKind = KatKat.Meta.SessionKind.Custom;
            var startState = LevelLoader.ToState(_level);
            // v9 B: günlük ödül +1 hamle yalnız oturum açılırken eklenir (açık oturumda limit değişmez; kaynak veri değişmez).
            int moveBonus = SessionMoveBonus(_level, _sessionKind);
            startState.MovesLeft += moveBonus;
            _session = new GameSession(startState);
            _extraMovesUsed = 0;
            _heartAtStake = false;
            EndPlaceOffer();
            ResetPaidHint();
            OutcomeBegin(_level, _sessionKind, moveBonus);
            _previewRow = -1;
            // Tema bölümün bölgesinden; dünya, tepsi, çerçeve ve yuva birlikte değişir (v3 §8).
            var theme = ThemeDefinition.ForLevel(index);
            var background = theme.GameSprite;
            background = BlurredBackdrop.For(background) ?? background; // v6: bütün temalarda bulanık
            // v10 oyun içi kabuk: mor bulutlu arka plan bütün oyun temalarında (harita teması değişmez).
            background = KatKat.View.GameShellArt.Background() ?? background;
            // ui3 (03_ZEMIN_VE_BORDER.md): Z1–Z5 eşit olasılık, ayrı kozmetik RNG; aynı bölümün denemeleri/tekrarları
            // boyunca sabit, menüden yeni girişte ya da başka bölümde yeniden seçilir. Core RNG/kayıt etkilenmez.
            if (Ui3.Ready)
            {
                string key = _sessionKind + ":" + _level.Id;
                if (_bgKey != key || _bgPick < 0) { _bgPick = BackgroundOverride ?? CosmeticRng.Next(5); _bgKey = key; }
                background = Ui3.GameBackground(_bgPick) ?? background;
            }
            // VR2 (docs/vr2/CLAUDE_CODE.md): bölgenin teması → tam ekran doku, hedef yuvası kenarı, tahta iç rengi.
            int vr2 = Vr2SkinOverride ?? Vr2.SkinFor(index);
            // Kullanıcı 2026-11-15: "arkaplan blurlu olsun" → VR2 dokusu da bulanık (BlurredBackdrop: 1/4 boyut, iki geçiş;
            // tema başına bir kez üretilir). Bulanıklaştırma başarısızsa keskin doku kalır.
            if (Vr2.Ready) { var vbg = Vr2.Background(vr2); background = (Vr2.BlurBackground ? BlurredBackdrop.For(vbg) : null) ?? vbg ?? background; }
            _backdrop.SetSprite(background ?? PremiumArt.Background(index));
            _backdrop.Fit(_cam);
            // 2026-11-18: arka plan görselinin parlak noktalarında ara ara ışıltı, yavaş tozlar, şelalede akan su (keskin VR2
            // dokusunda; azaltılmış harekette yok).
            bool motionOn = Vr2.Ready && !Vr2.BlurBackground && !KatKat.UI.Motion.Reduced && BgMotionEnabled;
            _backdrop.SetMotion(motionOn ? background : null, motionOn ? BgMotion.ForSkin(vr2) : (BgMotion.Profile?)null);
            _board.SetTheme(theme);
            _board.SetVr2Skin(vr2);
            _hud.SetTheme(theme);
            _hud.SetVr2Skin(vr2);
            // Başlık: altın dairede bölüm numarası, turkuaz hapta bölge türü (SAHİL/BAHÇE/ÇÖL).
            _hud.SetLevel(Loc.Upper(theme.LocalName), _session.State.MovesLeft, _session.State.Goals, _session.State.RollEnabled, index + 1);
            _hud.SetScore(_session.State.Score, _session.State.StarThresholds, false);
            _paintMode = false;
            _hud.HidePalette();
            _hud.SetPaint(_session.State.PaintLeft, false);
            // 2026-11-23 (130. bölüm): özel taşı kapalı ama çakıllı bölümde de Kürek tepsiye gelir; tepsi görünmeli.
            _hud.SetTrayVisible(_session.State.SpecialsEnabled || KatKat.Core.RulesEngine.HasGravel(_session.State.Board));
            _hud.SetTray(_session.State.Tray, _traySlot = -1);
            _board.SetLevelPalette(index);
            BoostersAtLevelStart();
            _celebrating = false;
            _celebration.Clear();
            // Eski alt öğretici bandı yerine odaklı tur (GameController.Tutorial); band tahta alanını daraltmaz.
            _hud.ShowTutorial(null);
            _hud.SetDailyLayout(_sessionKind == KatKat.Meta.SessionKind.Daily && Ui1.Ready); // ui1/17: başlık plakasına yer
            // 2026-11-20: güç bandı ve alt şerit tahtadan yer alır yalnız gerekirse (görev şeridi, güçlendirici/boya/günlük taş).
            _hud.ReserveHazard = _session.State.Hazard != null;
            _hud.ReserveBottom = BoostersVisible || _session.State.PaintLeft > 0 || _sessionKind != KatKat.Meta.SessionKind.Normal;
            _board.Build(_session.State, _hud.BoardWorldRect(_cam));
            ConceptHud(); // konsept pilotu (tehditsiz bölümde gizler)
            Vr1CenterIceBegin(); // vr1 P1 prototipi: ortası boş tahtada buzlu koleksiyon taşı
            bool normal = _sessionKind != KatKat.Meta.SessionKind.Daily && _sessionKind != KatKat.Meta.SessionKind.Trial;
            _hud.SetRuleIcons(null);
            _hud.SetDailyHeader(null);
            _hintActive = normal && System.Array.IndexOf(HintLevels, index) >= 0 && _level.Solution.Count > 0 && (_level.Solution[0].Type == "tap" || _level.Solution[0].Type == "rotate");
            if (normal) TourBegin(index); else { _tourOn = false; HideTour(); }
            // vr1 02_ZPE: zorunlu öğretici (tur, öğretici halka) yardım sayılmaz; isteğe bağlı yardımlar (ipucu, kayıp ipucu) sayılır.
            RefreshHint();
            if (normal) MainRulesBegin(); // v8: kural simgeleri, tür tanıtım / patron kartı
            SetPhase(Phase.Idle);
            Vr1LevelStart();
            AfterBegin();
        }

        private void ShowContentError(string message)
        {
            SetPhase(Phase.Error);
            _hud.ShowPanel(Loc.T("err.title"), message,
                new HudView.PanelButton(Loc.T("btn.home"), UiIcon.Back, true, ShowHome));
        }

        private void SetPhase(Phase p)
        {
            CurrentPhase = p;
            bool stable = p == Phase.Idle || p == Phase.Ended;
            _hud.SetButtons(stable && _session != null && _session.CanUndo, stable && _session != null);
            if (_session != null)
            {
                var s = _session.State;
                // v9 B: günlükte boş rulo düğmesi taş gücünü kullanır (güç hazırsa dokunulabilir).
                _hud.SetRoll(s.RollSlotFull, p == Phase.Targeting, (p == Phase.Idle && (s.RollSlotFull || DailyPowerReady)) || p == Phase.Targeting);
            }
            RefreshStonePower();
            if (_session != null) HintOnPhase(p); // vr1 P0: geçersiz plan yeniden çözülür, görsel tazelenir
            if (p == Phase.Idle) TourShowCurrent();
            else if (p != Phase.Targeting) HideTour();
        }

        private void RefreshHint()
        {
            _hud.HideHintMark();
            _board.HideHintGroup();
            // Satın alınmış ipucu öğretici halkasından önce gelir (2026-11-11: öğretici bölümlerde ücretli öneri görünmüyordu).
            if (PlanCurrent(_hintPlan) && ShowPlanHint()) return;
            if (_hintActive && _level.Solution.Count > 0)
            {
                var step = _level.Solution[0];
                // Döndürme öğreticisinde halka taban düğmesini gösterir.
                if (step.Type == "rotate") _hint.Show(_board.CellCenter(step.X, step.Y) + _board.StackAt(step.X, step.Y).RotorLocal, _board.Pitch * 0.45f);
                else _hint.Show(_board.CellCenter(step.X, step.Y), _board.Pitch);
            }
            else if (!ShowPlanHint()) _hint.Hide();
        }

        // ---- Girdi ---------------------------------------------------------------------------------

        private void Update()
        {
            if (_cam == null) return;
            UiClickSoundTick();
            if (_live != null) return; // canlı maç kendi girdisini ve yerleşimini yönetir
            OutcomeTick(Time.unscaledDeltaTime);
            TimedTick(Time.unscaledDeltaTime);
            HintTick();
            TrialTickPress();
            IdleHintTick(Time.unscaledDeltaTime);
            // 2026-11-21 (kullanıcı: "Taş kaydırmayı iptal edelim oyunun dinamiğini bozuyor"): sürükle-değiştir girdisi kapalı.
            if (_screen.x != Screen.width || _screen.y != Screen.height)
            {
                FitCamera();
                if (_session != null)
                {
                    _board.Layout(_hud.BoardWorldRect(_cam));
                    if (_centerIce != null) _centerIce.Layout(_board);
                    if (!_board.IsPlaying) _board.Sync(_session.State);
                    RefreshHint();
                    if (_tourOn && CurrentPhase == Phase.Idle) TourShowCurrent();
                }
            }

            // Öğretici bilgi adımı: ekranın herhangi bir yerine dokunuş (arayüz düğmeleri dahil) adımı geçer.
            if (_tourOn && _tourInfoShowing && !_hud.PanelOpen && !_menu.IsOpen) { TourConsumesPress(); return; }
            if (!TryGetPress(out var screenPos)) return;
            // Bitiş kutlaması dokununca atlanır (tekrar oynarken bekletmesin).
            if (_celebrating) { SkipCelebration(); return; }
            if (_hud.PanelOpen || _menu.IsOpen) return;
            var world = _cam.ScreenToWorldPoint(screenPos);
            // Döner taban düğmesi ayrı komuttur; yığının geri kalanına dokunuş grup seçer.
            if (CurrentPhase != Phase.Targeting && _board.TryRotorAt(world, out int rx, out int ry))
            {
                OnRotorPressed(rx, ry);
                return;
            }
            if (!_board.TryCellAt(world, out int x, out int y)) return;
            // v12 deneme: dokunuş bırakınca; basılı tutunca katman görünümü.
            if (TrialHandlePress(screenPos, x, y)) return;
            OnCellPressed(x, y);
        }

        private static bool TryGetPress(out Vector2 pos)
        {
            pos = default;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase != TouchPhase.Began) return false;
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId)) return false;
                pos = t.position;
                return true;
            }
            if (!Input.GetMouseButtonDown(0)) return false;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
            pos = Input.mousePosition;
            return true;
        }

        /// <summary>Tahta hücresine dokunuş. Testler ve yakalama aracı doğrudan çağırabilir.</summary>
        public void OnCellPressed(int x, int y)
        {
            _board.DropVeil(); // uçuş sırasında dokunuş: perde 0,1 sn'de kalkar
            if (CurrentPhase == Phase.Targeting)
            {
                SelectRollRow(y);
                return;
            }
            if (CurrentPhase != Phase.Idle) return;
            if (!TourAllowsCell(x, y)) return;
            if (BoosterCellPressed(x, y)) return; // Bomba hedefi ya da Taş Değiştir seçimi
            if (TryPlaceAt(x, y)) return; // reklam ödülü: seçili taşı koy
            if (_paintMode) { PickPaintCell(x, y); return; }
            if (_traySlot >= 0) { UseTraySlot(x, y); return; }
            var r = _session.Submit(Command.Tap(x, y));
            if (!r.Accepted)
            {
                if (r.Reason == RejectReason.NoMovesLeft)
                {
                    _sound.Play(Sfx.Invalid);
                    _hud.ShowToast(Loc.T("toast.nomoves.tray"));
                    _hud.PulseTray();
                    return;
                }
                if (r.Reason == RejectReason.GroupTooSmall)
                {
                    TooSmallFeedback(r.TappedGroup); // sürükle-değiştir kapalı (2026-11-21)
                }
                else if (r.Reason == RejectReason.NotSelectable && _session.State.Board.InBounds(x, y) && _session.State.Board[x, y].HasCover)
                {
                    var cell = _session.State.Board[x, y];
                    _sound.Play(Sfx.Invalid);
                    Haptics.Light();
                    _board.ShakeCells(new[] { new Coord(x, y) });
                    if (cell.ClipHitsRemaining > 0) _hud.ShowToast(Loc.T("toast.clip"));
                    else if (cell.IceHitsRemaining > 0) _hud.ShowToast(Loc.T("toast.ice"));
                    else if (cell.HasEnvelope) _hud.ShowToast(Loc.F("toast.envelope", LayerPalette.ColorName(cell.EnvelopeColor)));
                    else if (cell.Moss) _hud.ShowToast(Loc.T("concept.toast.moss")); // konsept pilotu
                    else if (cell.Gravel) _hud.ShowToast(Loc.T("toast.gravel"));
                    else _hud.ShowToast(Loc.F("toast.lock", cell.LockRemaining, LayerPalette.ColorName(cell.LockColor)));
                }
                return;
            }
            _hintActive = false;
            HintAfterCommand();
            _hint.Hide(); // ücretli ipucu tahta durunca (SetPhase Idle) yeniden görünür
            // 2026-11-22 (kullanıcı: "Taşlara patlatmak için bastığım anda tek bir taş sesi gelecek"): dokunuşta tek jöle sesi.
            PlayTapPop(r);
            Haptics.Light();
            HeartOnMove();
            TourOnAccepted(r);
            Play(r);
        }

        /// <summary>Döner taban düğmesine dokunuş (M08). Testler ve yakalama aracı doğrudan çağırabilir.</summary>
        public void OnRotorPressed(int x, int y)
        {
            if (CurrentPhase != Phase.Idle) return;
            if (!TourAllowsRotor(x, y)) return;
            var r = _session.Submit(Command.Rotate(x, y));
            if (!r.Accepted)
            {
                if (r.Reason != RejectReason.NotRotatable) return;
                var cell = _session.State.Board[x, y];
                _sound.Play(Sfx.Invalid);
                Haptics.Light();
                _board.ShakeCells(new[] { new Coord(x, y) });
                _hud.ShowToast(cell.HasCover ? Loc.T("toast.rotor.cover") : Loc.T("toast.rotor.two"));
                return;
            }
            _hintActive = false;
            HintAfterCommand();
            _hint.Hide(); // ücretli ipucu tahta durunca (SetPhase Idle) yeniden görünür
            _sound.Play(Sfx.Tap);
            Haptics.Light();
            HeartOnMove();
            TourOnAccepted(r);
            Play(r);
        }

        /// <summary>Kayıtlı çözüm adımını oyuncu girdisi gibi uygular (dokunuş, döndürme ya da rulo: yuva → satır önizleme → onay).</summary>
        public void PlayStep(KatKat.Levels.StepData step)
        {
            if (step.Type == "tap") { OnCellPressed(step.X, step.Y); return; }
            if (step.Type == "rotate") { OnRotorPressed(step.X, step.Y); return; }
            if (step.Type == "paint") { var cmd = LevelLoader.ToCommand(step); _paintMode = true; _paintCell = new Coord(cmd.X, cmd.Y); OnPaintColor(cmd.Color); return; }
            if (step.Type == "use") { _traySlot = -1; OnTraySlot(step.Row); OnCellPressed(step.X, step.Y); return; }
            if (CurrentPhase != Phase.Targeting) ToggleRollTargeting();
            OnCellPressed(0, step.Row);
            OnCellPressed(0, step.Row);
        }

        /// <summary>Rulo yuvasına dokunuş (iptal dahil).</summary>
        public void PressRollSlot() => ToggleRollTargeting();

        // ---- Rulo ----------------------------------------------------------------------------------

        private void ToggleRollTargeting()
        {
            if (CurrentPhase == Phase.Targeting)
            {
                CancelTargeting();
                return;
            }
            if (CurrentPhase == Phase.Idle && !_session.State.RollSlotFull && _sessionKind == KatKat.Meta.SessionKind.Daily) { TryDailyPower(); return; }
            if (CurrentPhase != Phase.Idle || !_session.State.RollSlotFull) return;
            _previewRow = -1;
            SetPhase(Phase.Targeting);
            _hud.ShowToast(Loc.T("toast.roll.pick"), 2.6f);
        }

        private void CancelTargeting()
        {
            _previewRow = -1;
            _board.HideRowPreview();
            _hud.HideToast();
            SetPhase(Phase.Idle);
        }

        private void SelectRollRow(int row)
        {
            var targets = RulesEngine.RollTargets(_session.State.Board, row);
            if (targets.Count == 0)
            {
                _hud.ShowToast(Loc.T("toast.roll.empty"));
                return;
            }
            if (_previewRow != row)
            {
                _previewRow = row;
                var cols = new List<int>();
                foreach (var i in targets) cols.Add(_session.State.Board.X(i));
                _board.ShowRowPreview(row, cols, UiArt.Box, new Color(1f, 0.72f, 0.24f, 0.55f));
                _hud.ShowToast(Loc.T("toast.roll.confirm"), 2.2f);
                return;
            }
            _board.HideRowPreview();
            _previewRow = -1;
            var r = _session.Submit(Command.Roll(row));
            if (!r.Accepted) { CancelTargeting(); return; }
            _hud.HideToast();
            HintAfterCommand();
            Play(r);
        }

        // ---- Oynatma -------------------------------------------------------------------------------

        private void Play(MoveResult r)
        {
            _lastResult = r;
            SetPhase(Phase.Animating);
            _hud.SetMoves(r.MovesLeftAfter);
            if (r.RollConsumed)
            {
                _hud.SetRoll(false, false, false);
                _hud.PlayRollUsed();
                // 2026-11-20 (kullanıcı: "skil kullanımında da tahta çevresinden animasyonlar oluşsun").
                _board.RimSweep(new Color(0.25f, 0.9f, 0.85f), new Color(1f, 0.88f, 0.4f), 1.1f);
            }
            var goals = _session.State.Goals;
            var shown = new int[goals.Count];
            for (int g = 0; g < goals.Count; g++) shown[g] = -1;
            int scoreShown = _session.State.Score - r.ScoreGained;
            _chainSounded = false;
            // 2026-11-19: yavaş taş akışı girdiyi kilitlemesin; yalnız bölümü bitiren hamle taşların varmasını bekler.
            _board.HoldUntilArrive = _session.State.Status != GameStatus.Idle;
            _board.Play(r, _hud.GoalTarget, _hud.StarTarget,
                wave =>
                {
                    Praise(r, wave);
                    bool opened = false;
                    foreach (var h in wave.ClipHits) opened |= h.RemainingAfter == 0;
                    foreach (var h in wave.IceHits) opened |= h.RemainingAfter == 0;
                    foreach (var h in wave.LockHits) opened |= h.RemainingAfter == 0;
                    opened |= wave.EnvelopeOpens.Count > 0;
                    if (opened) _sound.Play(Sfx.ClipOpen, 0, 0.8f);
                    if (wave.GateOpens.Count > 0) KatKat.UI.Vr1Proto.Play("gate-open");
                    // Dalga başına tek ana ses. v14 (kullanıcı 2026-11-16: zincir tamamen kaldırılsın): dört vuruşlu zincir
                    // WAV'ı artık büyük hamlenin (6+ grup ya da 8+ katmanlık özel taş) övgü dalgasında bir kez çalar;
                    // WAV yoksa övgü seviyesine göre kademeli sentez.
                    // 2026-11-18: tepsinin arkasından patlayan taş renginde ışıltılar — dokunulan grubun boyutuna göre
                    // (3–4 az, 5 daha çok + ateş kıvılcımı, 6+ en çok + alevler); büyük özel taş hamlesi de 6+ gibi.
                    bool moment = !_chainSounded && BigMove.IsMomentWave(r, wave);
                    if (wave.Kind == WaveKind.Tap && wave.Index == 0)
                    {
                        int n = r.TappedGroup.Count;
                        _board.TrayBurst(wave, n >= 6 ? 3 : n >= 5 ? 2 : n >= 2 ? 1 : 0); // 2026-11-20: ikili grupta da ("daha fazla olsun")
                    }
                    else if (moment) _board.TrayBurst(wave, 3);
                    // 2026-11-22: dalga başında ayrıca patlama/övgü sesi yok (eski "combo-blast" kalktı); taş sesi dokunuşta bir kez,
                    // varışta taş sayısınca. V17 yoksa eski davranış.
                    if (moment) _chainSounded = true;
                    if (!_sound.HasV17("jelly-pop"))
                    {
                        if (moment) _sound.Play(Sfx.Chain);
                        else _sound.Play(wave.Kind == WaveKind.Roll ? Sfx.RollUse : Sfx.Peel);
                    }
                    else if (wave.Kind == WaveKind.Roll) _sound.Play(Sfx.RollUse);
                    if (wave.Index == 0 && r.RollAwarded)
                    {
                        _hud.SetRoll(true, false, false);
                        _hud.PulseRoll();
                        _sound.Play(Sfx.RollGain);
                        _hud.ShowToast(Loc.T("toast.roll.won"), 2.4f);
                    }
                },
                wave =>
                {
                    if (wave.StarsCollected > 0 || wave.EnvelopeOpens.Count > 0) _sound.Play(Sfx.Star);
                    // vr1 P3: aşama tamamlandı → sonraki aşama öne çıkar.
                    if (wave.StageAdvanced) { _hud.SetGoalStages(_session.State.Goals, wave.StageAfter); _hud.ShowToast(Loc.F("vr1.stage.next", wave.StageAfter + 1, _session.State.StageCount), 2.2f); }
                    scoreShown += wave.Score;
                    _hud.SetScore(scoreShown, _session.State.StarThresholds, true);
                    // HUD hedefi görsel dalga tamamlandığında gösterir; mantıkta yeniden artırılmaz.
                    for (int g = 0; g < wave.GoalProgressAfter.Length; g++)
                    {
                        if (wave.GoalProgressAfter[g] == shown[g]) continue;
                        bool changed = shown[g] >= 0 || wave.GoalProgressAfter[g] > 0;
                        shown[g] = wave.GoalProgressAfter[g];
                        _hud.SetGoalProgress(g, shown[g], changed);
                    }
                },
                () => OnPlayFinished());
        }

        /// <summary>
        /// Büyük hamle ve özel taş anları: dalga başında övgü yazısı ve ses. v14 (kullanıcı 2026-11-16: zincir tamamen
        /// kaldırılsın): sözler ("Süper!", "Katman katman!", "Şahane!", "Efsane!") zincir dalgasına değil oyuncunun büyük
        /// hamlesine bağlı (<see cref="BigMove"/>: 6+ grup ya da 8+ katman soyan özel taş). Birleşimde Loc.T("praise.combo");
        /// doğan taşın adı büyük grupta büyük yazılır.
        /// </summary>
        private void Praise(MoveResult r, PeelWave wave)
        {
            int level = BigMove.IsMomentWave(r, wave) ? BigMove.Level(r) : 0;
            string[] words = { Loc.T("praise.1"), Loc.T("praise.2"), Loc.T("praise.3"), Loc.T("praise.4") };
            if (wave.Kind == WaveKind.Special)
            {
                // V17 (kullanıcı 2026-11-22): skilin kendi sesi; taş patlama sesi dalgada ayrıca çalar. Bomba sesini güçlendirici çalar.
                string skill = wave.Fired == SpecialKind.RocketRow || wave.Fired == SpecialKind.RocketColumn ? "rocket-sweep"
                    : wave.Fired == SpecialKind.Rainbow ? "roll-lightning" : wave.Fired == SpecialKind.Shovel ? "shovel-dig" : null;
                if (wave.Fired == SpecialKind.Bomb) { }
                else if (skill == null || !_sound.PlayV17(skill)) _sound.Play(Sfx.RollUse);
                Haptics.Medium();
                if (wave.ComboWith != SpecialKind.None) _hud.ShowPraise(Loc.T("praise.combo"), 3);
                else if (level > 0) _hud.ShowPraise(words[level - 1], level);
                return;
            }
            if (wave.Index == 0 && r.SpecialCreated != SpecialKind.None)
            {
                // 2026-11-22 (kullanıcı: "combolar ile roket veya altın rulo kazanılınca kazanma sesi olsun"): parlak yıldız sesi,
                // Altın Rulo'da daha tiz. V17 yoksa eski prototip sesi.
                if (!_sound.PlayV17("star-gain", r.SpecialCreated == SpecialKind.Rainbow ? 5f : 2f, 1.2f)) KatKat.UI.Vr1Proto.Play("special-charge");
                _hud.ShowPraise(r.SpecialCreated == SpecialKind.Rainbow ? Loc.T("praise.rainbow") : r.SpecialCreated == SpecialKind.Shovel ? Loc.T("praise.shovel") : Loc.T("praise.rocket"), Mathf.Max(1, level));
                return;
            }
            if (level > 0) _hud.ShowPraise(words[level - 1], level);
        }

        // ---- V17 sesleri (kullanıcı 2026-11-22) ------------------------------------------------------------

        /// <summary>Dokunuşta tek jöle sesi, grubun rengi tonunda.</summary>
        private void PlayTapPop(MoveResult r)
        {
            LayerColor c = LayerColor.Yellow;
            if (r.Waves.Count > 0 && r.Waves[0].Cells.Count > 0) c = r.Waves[0].Cells[0].Removed;
            if (!_sound.PlayV17("jelly-pop", PeelToneOf(c))) _sound.Play(Sfx.Tap);
        }

        /// <summary>Renk tonu (yarım ses): mercan −3, turkuaz −1, inci 0, lila +2, lacivert +4.</summary>
        public static float PeelToneOf(LayerColor c) => c switch
        {
            LayerColor.Coral => -3f, LayerColor.Blue => -1f, LayerColor.Yellow => 0f, LayerColor.Purple => 2f, LayerColor.Navy => 4f, _ => 0f,
        };

        /// <summary>Bütün arayüz düğmeleri: basıldığı anda V17 düğme sesi (etkin, tıklanabilir Button altında).</summary>
        private void UiClickSoundTick()
        {
            bool down = Input.GetMouseButtonDown(0);
            Vector2 pos = Input.mousePosition;
            if (Input.touchCount > 0) { var t = Input.GetTouch(0); down = t.phase == TouchPhase.Began; pos = t.position; }
            if (!down || EventSystem.current == null || _sound == null) return;
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pos }, hits);
            if (hits.Count == 0) return;
            var btn = hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>();
            if (btn != null && btn.IsInteractable()) _sound.PlayV17("ui-button");
        }

        // ---- v6 özel taş tepsisi -------------------------------------------------------------------------
        // Kullanıcı: "özel skiller taşların üzerinde olmasın; aşağıdaki alana gelsinler, oyuncu istediği zaman
        // kullansın". Yuva seçilir, sonra bir taşa dokunulur (roket o satırı/sütunu, Altın Rulo o rengi); hamle harcamaz.

        private int _traySlot = -1;

        /// <summary>Tepsi yuvasına dokunuş (testler ve yakalama aracı).</summary>
        public void PressTraySlot(int slot) => OnTraySlot(slot);

        private void OnTraySlot(int slot)
        {
            if (CurrentPhase != Phase.Idle || _session == null) return;
            var tray = _session.State.Tray;
            if (slot < 0 || slot >= tray.Count) return;
            if (!TourAllowsSlot(slot)) return;
            if (_paintMode) { _paintMode = false; _hud.HidePalette(); _hud.SetPaint(_session.State.PaintLeft, false); }
            _traySlot = _traySlot == slot ? -1 : slot;
            _hud.SetTray(tray, _traySlot);
            RefreshHint(); // ipucu: yuva seçilince hedef hücre
            if (_traySlot < 0) { _hud.HideToast(); return; }
            var kind = tray[_traySlot];
            _sound.Play(Sfx.Tap);
            if (_tourOn) { TourShowCurrent(); return; } // tur kendi açıklamasını gösterir
            _hud.ShowToast(kind == SpecialKind.Shovel ? Loc.T("toast.use.shovel")
                : kind == SpecialKind.Rainbow ? Loc.T("toast.use.rainbow")
                : kind == SpecialKind.RocketRow ? Loc.T("toast.use.row")
                : Loc.T("toast.use.col"), 2.6f);
        }

        private void UseTraySlot(int x, int y)
        {
            var usedKind = _traySlot < _session.State.Tray.Count ? _session.State.Tray[_traySlot] : SpecialKind.None;
            var r = _session.Submit(Command.UseSpecial(_traySlot, x, y));
            if (!r.Accepted)
            {
                _sound.Play(Sfx.Invalid);
                Haptics.Light();
                if (_session.State.Board.InBounds(x, y)) _board.ShakeCells(new[] { new Coord(x, y) });
                _hud.ShowToast(r.Reason == RejectReason.TrayNoEffect ? Loc.T("toast.use.noeffect") : Loc.T("toast.use.invalid"));
                return;
            }
            _traySlot = -1;
            _hud.SetTray(_session.State.Tray, -1);
            _hud.HideToast();
            _hintActive = false;
            // 2026-11-20: tepsi gücü kullanımında tahtanın çevresinde ışık dalgası (Altın Rulo gökkuşağı, roket turkuaz-altın).
            if (usedKind == SpecialKind.Rainbow) _board.RimSweep(Color.white, Color.white, 1.3f, rainbow: true);
            else if (usedKind == SpecialKind.Shovel) _board.RimSweep(new Color(0.95f, 0.85f, 0.62f), new Color(0.55f, 0.85f, 0.8f), 1.0f);
            else _board.RimSweep(new Color(0.25f, 0.9f, 0.85f), new Color(1f, 0.85f, 0.35f), 1.15f);
            HintAfterCommand();
            _hint.Hide(); // ücretli ipucu tahta durunca (SetPhase Idle) yeniden görünür
            Haptics.Medium();
            HeartOnMove();
            TourOnAccepted(r);
            Play(r);
        }

        // ---- M12 boya damlası --------------------------------------------------------------------------

        private bool _paintMode;
        private Coord _paintCell;

        private void TogglePaint()
        {
            if (CurrentPhase != Phase.Idle || _session.State.PaintLeft <= 0) return;
            _paintMode = !_paintMode;
            _hud.HidePalette();
            _hud.SetPaint(_session.State.PaintLeft, _paintMode);
            if (_paintMode) _hud.ShowToast(Loc.T("toast.paint.pick"), 2.2f);
            else _hud.HideToast();
            RefreshHint();
        }

        private void PickPaintCell(int x, int y)
        {
            var b = _session.State.Board;
            if (!b.InBounds(x, y) || !b[x, y].IsSelectable)
            {
                _hud.ShowToast(Loc.T("toast.paint.invalid"));
                return;
            }
            _paintCell = new Coord(x, y);
            var options = new List<LayerColor>();
            for (int c = 0; c < LayerColors.Count; c++)
            {
                var col = (LayerColor)c;
                if (col != b[x, y].Top && b.CountLayers(col) > 0) options.Add(col);
            }
            _hud.ShowPalette(b[x, y].Top, options);
            _board.ShakeCells(new[] { _paintCell });
            HintOnPaintCell(_paintCell);
        }

        private void OnPaintColor(LayerColor color)
        {
            if (BoosterColorChosen(color)) return;
            if (!_paintMode || CurrentPhase != Phase.Idle) return;
            var r = _session.Submit(Command.Paint(_paintCell.X, _paintCell.Y, color));
            _hud.HidePalette();
            if (!r.Accepted)
            {
                if (r.Reason == RejectReason.SameColor) _hud.ShowToast(Loc.T("toast.paint.same"));
                return;
            }
            _paintMode = false;
            _hud.SetPaint(_session.State.PaintLeft, false);
            _hud.HideToast();
            HintAfterCommand();
            _sound.Play(Sfx.Tap);
            Haptics.Light();
            Play(r);
        }

        // ---- Bitiş kutlaması (v5 §4) ------------------------------------------------------------------

        private bool _celebrating;
        private readonly Queue<MoveResult> _celebration = new Queue<MoveResult>();

        private void StartCelebration()
        {
            var s = _session.State;
            _celebration.Clear();
            if (s.SpecialsEnabled || s.MovesLeft > 0)
                foreach (var step in RulesEngine.Celebrate(s, CelebrationSeed(_level.Id))) _celebration.Enqueue(step);
            _hud.SetTray(s.Tray, -1); // tepside kalanlar kutlamada ateşlenir
            if (_celebration.Count == 0) { OnLevelWon(); return; }
            _celebrating = true;
            _hud.ShowPraise(Loc.T("praise.celebrate"), 2);
            NextCelebrationStep();
        }

        private void NextCelebrationStep()
        {
            if (!_celebrating) return;
            if (_celebration.Count == 0) { EndCelebration(); return; }
            var step = _celebration.Dequeue();
            _hud.SetMoves(step.MovesLeftAfter);
            int scoreShown = _session.State.Score;
            // Durum kutlamanın sonunu gösterir; ekrandaki puan adım adım artar.
            int before = _celebrationScore;
            _celebrationScore += step.ScoreGained;
            // Kutlamada düşen roket önce görünür, sonra ateşlenir.
            if (step.SpecialCreated != SpecialKind.None)
            {
                var at = step.SpecialCreatedAt;
                _board.StackAt(at.X, at.Y).SetSpecial(step.SpecialCreated, default);
                _board.StackAt(at.X, at.Y).PopSpecial();
            }
            _board.HoldUntilArrive = false; // kutlama adımları uçuşu beklemez; EndCelebration kalan uçuşu bitirir
            _board.Play(step, _hud.GoalTarget, _hud.StarTarget,
                w => _sound.Play(Sfx.RollUse),
                w => { before += w.Score; _hud.SetScore(before, _session.State.StarThresholds, true); },
                () => NextCelebrationStep());
        }

        private int _celebrationScore;

        /// <summary>Bitiş kutlaması oynuyor mu (zafer kartı kutlamadan sonra açılır).</summary>
        public bool IsCelebrating => _celebrating;

        public void SkipCelebration()
        {
            if (!_celebrating) return;
            _celebration.Clear();
            _board.CompleteImmediately();
            EndCelebration();
        }

        private void EndCelebration()
        {
            if (!_celebrating) return;
            _celebrating = false;
            var s = _session.State;
            _board.CompleteImmediately(); // kutlama adımlarının havadaki uçuşları
            _board.Sync(s);
            _hud.SetMoves(0);
            _hud.SetScore(s.Score, s.StarThresholds, false);
            OnLevelWon();
        }

        /// <summary>Üretici ile aynı tohum (yıldız eşikleri kayıtlı çözüm + bu kutlamadan hesaplanır).</summary>
        public static int CelebrationSeed(string id)
        {
            int n = 0;
            bool any = false;
            foreach (var ch in id ?? "") if (char.IsDigit(ch)) { n = n * 10 + (ch - '0'); any = true; }
            return any ? n : 1;
        }

        private void OnPlayFinished()
        {
            if (_celebrating) return;
            var s = _session.State;
            OutcomeMoves(s); // kutlama kalan hamleleri harcamadan önce
            _board.Sync(s);
            // 2026-11-19: hamle uçuşlar bitmeden biterse sayaçlar taşlar vardıkça (onWaveArrived) güncellenir; erken doldurulmaz.
            if (_board.HoldUntilArrive || !_board.IsPlaying) _hud.SetGoalsImmediate(s.Goals);
            _hud.SetGoalStages(s.Goals, s.ActiveStage); // vr1 P3 (tek aşamalıda etkisiz)
            _hud.SetMoves(s.MovesLeft);
            _hud.SetTray(s.Tray, _traySlot = -1);
            ConceptAfterPlay(_lastResult); // konsept pilotu: tehdit olayı, durum satırı
            if (s.Status == GameStatus.Idle && s.MovesLeft <= 0 && s.Tray.Count > 0)
            {
                _hud.ShowToast(Loc.T("toast.nomoves.tray2"), 2.6f);
                _hud.PulseTray();
            }
            switch (s.Status)
            {
                case GameStatus.Won:
                    _session.MarkVictoryRecorded();
                    SetPhase(Phase.Ended);
                    if (_centerIce != null && _sessionKind == KatKat.Meta.SessionKind.Normal) _centerIce.Flash(); // son hamle: kısa çatlak ışığı
                    _celebrationScore = s.Score;
                    StartCelebration();
                    break;
                case GameStatus.LostProtected:
                {
                    // 2026-11-22: soyulan yasak yığınlardaki bomba patlar, sonra kayıp kartı.
                    SetPhase(Phase.Ended);
                    var hit = new System.Collections.Generic.List<Coord>();
                    if (_lastResult != null)
                        foreach (var w in _lastResult.Waves)
                            foreach (var c in w.Cells)
                                if (s.Board[c.X, c.Y].Protected && !hit.Exists(h => h.X == c.X && h.Y == c.Y)) hit.Add(new Coord(c.X, c.Y));
                    if (hit.Count == 0) { ShowLost(s.Status); break; }
                    _sound.Play(Sfx.Boom);
                    Haptics.Strong();
                    var status = s.Status;
                    _board.ExplodeBombs(hit, () => ShowLost(status));
                    break;
                }
                case GameStatus.LostOutOfMoves:
                case GameStatus.LostNoLegalMoves:
                case GameStatus.LostHazard:
                    SetPhase(Phase.Ended);
                    ShowLost(s.Status);
                    break;
                case GameStatus.LostOutOfTime:
                    break; // süreli bölüm: patlama animasyonu bitince TimedTick gösterir
                default:
                    SetPhase(Phase.Idle);
                    break;
            }
        }

        private void OnLevelWon()
        {
            OutcomeFinish(true);
            // v9 B: günlük bulmaca ana ilerlemeye ve yıldızlara yazılmaz; ödül günlüğü ayrıdır.
            if (_sessionKind == KatKat.Meta.SessionKind.Daily) { OnDailyWon(); return; }
            if (_sessionKind == KatKat.Meta.SessionKind.Trial) { OnTrialWon(); return; }
            _save.MarkCompleted(_level.Id, _levelIndex, _catalog.Count);
            _save.RecordStars(_level.Id, EarnedStars); // reklamlı ipucu: −1 yıldız (en az 1)
            // v12: sıralama yıldızı yalnız reklamsız kazanımdan (kullanıcı kararı); geçiş reklamı sayacı.
            if (_sessionKind == KatKat.Meta.SessionKind.Normal)
            {
                if (_outcome == null || !(_outcome.Has(KatKat.Meta.AssistFlags.RewardedAd) || _outcome.Has(KatKat.Meta.AssistFlags.Booster)))
                    KatKat.Meta.RankStars.Record(_save.Data.meta, _level.Id, EarnedStars);
                _interstitial?.LevelCompleted(_levelIndex + 1);
                OfflineOnWin();
            }
            if (_sessionKind == KatKat.Meta.SessionKind.Normal) CheckStoneUnlocks();
            if (_sessionKind == KatKat.Meta.SessionKind.Normal) _winsSinceMap++;
            TourOnWon();
            HeartOnWon();
            EndPlaceOffer();
            CloudOnWon();
            // v10: normal bölüm ödül kartı ilk gösterilirken bir kez (yıldızlar ve kart yeniden çizimi tekrar çalmaz).
            _sound.PlayReward();
            Haptics.Medium();
            // Son bölümde olmayan bir sonrakini yükleme: bölüm seçimine dön (rehber §5).
            bool hasNext = _levelIndex + 1 < _catalog.Count;
            // Büyük kutlama yalnızca son hamle büyükse (v14: zincir yerine 6+ grup ya da 8+ katmanlık özel taş, BigMove);
            // küçük üçlüde Loc.T("win.big") yok (rehber §6).
            bool chain = BigMove.Level(_lastResult) > 0;
            // Sonraki bölüm kartı: gerçek katalog verisi ve onun teması (bölge geçişinde yeni tema). Katalog dışı
            // (StartCustom) ya da son bölümde sahte önizleme yok.
            NextLevelPreviewView.Data? next = null;
            bool inCatalog = _levelIndex < _catalog.Count && _catalog.Levels[_levelIndex] == _level;
            if (inCatalog)
                next = hasNext
                    ? new NextLevelPreviewView.Data(_catalog.Levels[_levelIndex + 1], ThemeDefinition.ForLevel(_levelIndex + 1), _levelIndex + 2,
                        ThemeDefinition.ForLevel(_levelIndex + 1) != ThemeDefinition.ForLevel(_levelIndex))
                    : new NextLevelPreviewView.Data(null, null, 0);
            _hud.ShowVictory(chain ? Loc.T("win.big") : Loc.T("win.title"), _session.State.Goals, next, EarnedStars,
                new HudView.PanelButton(hasNext ? Loc.T("btn.continue") : Loc.T("btn.levels"), UiIcon.Play, true, () => AfterWinContinue(() => { if (hasNext) StartLevel(_levelIndex + 1); else ShowLevels(); })),
                new HudView.PanelButton(Loc.T("btn.replay"), UiIcon.Restart, false, () => StartLevel(_levelIndex)));
            // v10 P5: bölüm sonu ZPE değişimi (normal bölüm).
            if (_sessionKind == KatKat.Meta.SessionKind.Normal) _hud.SetResultBadge(Vr1Badge(ZpeChangeText())); // vr1 P1: buzlu taş ilerlemesi sonuçta da
        }

        private void ShowLost(GameStatus status)
        {
            _sound.Play(Sfx.Lose);
            OutcomeFinish(false);
            EndPlaceOffer();
            if (_sessionKind == KatKat.Meta.SessionKind.Daily) { ShowDailyLost(status); return; }
            if (_sessionKind == KatKat.Meta.SessionKind.Trial) { ShowTrialLost(status); return; }
            if (status == GameStatus.LostOutOfTime) { ShowTimeLost(); return; }
            bool moves = status == GameStatus.LostOutOfMoves, crystal = status == GameStatus.LostProtected;
            // v12 (kullanıcı, 2026-11-08): hamle bitince "Reklam izle, +3 hamle", bölüm başına 1 kez.
            ExtraMovesOffered = CanOfferExtraMoves(status);
            var buttons = new System.Collections.Generic.List<HudView.PanelButton>();
            if (ExtraMovesOffered) buttons.Add(new HudView.PanelButton(Loc.F("ad.place.offer", _adConfig.ExtraMoves), UiIcon.Play, true, OnExtraMovesPressed, true));
            // 2026-11-23 hata: bombadan sonra geri alma yoktu ama kartta "Geri al" duruyordu; basınca kart kilitleniyordu.
            bool canUndo = UndoAvailable;
            if (canUndo) buttons.Add(new HudView.PanelButton(Loc.T("btn.undo"), UiIcon.Undo, !ExtraMovesOffered, Undo));
            buttons.Add(new HudView.PanelButton(Loc.T("btn.retry"), UiIcon.Restart, !ExtraMovesOffered && !canUndo, Restart));
            if (MainHintButton() is HudView.PanelButton hint) buttons.Add(hint); // v8: 2 kayıptan sonra ipucu
            string title = crystal ? Loc.T("lose.protect.title") : moves ? Loc.T("lose.moves.title") : Loc.T("lose.stuck.title");
            string body = crystal ? Loc.T("lose.protect.body") : moves ? Loc.T("lose.moves.body") : Loc.T("lose.stuck.body");
            _hud.ShowPanel(title, body, buttons.ToArray());
            if (_sessionKind == KatKat.Meta.SessionKind.Normal) _hud.SetResultBadge(ZpeChangeText());
            if (Ui1.Ready) { _hud.PanelMascot(); _hud.PanelGoals(_session.State.Goals); } // ui1/15
        }

        // ---- Kararlı durum eylemleri -----------------------------------------------------------------

        public void Undo()
        {
            if (CurrentPhase != Phase.Idle && CurrentPhase != Phase.Ended) return;
            if (TourBlocksUndo) { _hud.PanelUnlock(); return; } // yönlendirmeli öğretici adımları geri alınmaz
            // Geri alınamıyorsa (ör. bomba güçlendiricisi) kart düğmeleri kilitli kalmasın.
            if (!_session.Undo()) { _hud.PanelUnlock(); return; }
            OutcomeAssist(KatKat.Meta.AssistFlags.Undo);
            OutcomeReopen();
            _hud.HidePanel();
            ResyncAll();
        }

        /// <summary>Kayıp kartında "Geri al" yalnız gerçekten geri alınabilirken gösterilir (bomba güçlendiricisi geri alınmaz).</summary>
        public bool UndoAvailable => _session != null && _session.CanUndo && !TourBlocksUndo;

        public void Restart()
        {
            if (CurrentPhase != Phase.Idle && CurrentPhase != Phase.Ended && CurrentPhase != Phase.Targeting) { _hud.PanelUnlock(); return; }
            // v9 B: günlükte yeniden başlatma yeni deneme hakkı ister (ücretsiz tekrar); hak yoksa başlatılmaz.
            if (_sessionKind == KatKat.Meta.SessionKind.Daily) { DailyRestart(); return; }
            HeartForfeit();
            if (_sessionKind == KatKat.Meta.SessionKind.Normal && !HeartGate(_levelIndex, () => StartLevel(_levelIndex))) return;
            EndPlaceOffer();
            _hud.HidePanel();
            _session.Restart();
            ResetPaidHint(); // vr1 P0: yeniden başlatma yeni denemedir (ipucu hakkı ve bedeli denemeye bağlı)
            TourOnRestart();
            OutcomeRestart();
            TimedBegin(); // süreli bölüm: süre baştan (2026-11-23 hata: süre bitince "yeniden dene" sonrası süre durmuş kalıyordu)
            ResyncAll();
        }

        private void ResyncAll()
        {
            _board.CompleteImmediately(); // havadaki taşlar ve bekleyen varış güncellemeleri eski hamleye ait (geri al / yeniden başla)
            _board.HideRowPreview();
            _previewRow = -1;
            var s = _session.State;
            _board.Sync(s);
            _hud.SetMoves(s.MovesLeft);
            _hud.SetGoalsImmediate(s.Goals);
            _hud.SetGoalStages(s.Goals, s.ActiveStage); // vr1 P3 (tek aşamalıda etkisiz)
            _hud.SetScore(s.Score, s.StarThresholds, false);
            _paintMode = false;
            _hud.HidePalette();
            _hud.SetPaint(s.PaintLeft, false);
            _hud.SetTray(s.Tray, _traySlot = -1);
            _boosterMode = null;
            _recolorPicks.Clear();
            RefreshBoosters();
            ConceptHud();
            SetPhase(Phase.Idle);
        }

        /// <summary>Duraklatma kartı (ekran yakalama aracı için; oyuncu HUD düğmesiyle açar).</summary>
        public void OpenPauseForCapture() => OpenPause();

        private void OpenPause()
        {
            if (CurrentPhase == Phase.Animating || _hud.PanelOpen) return;
            if (CurrentPhase == Phase.Targeting) CancelTargeting();
            _hud.ShowPanel(Loc.T("pause.title"), Loc.F("pause.body", _levelIndex + 1, _session?.State.Score ?? 0),
                new HudView.PanelButton(Loc.T("btn.continue"), UiIcon.Play, true, () => _hud.HidePanel()),
                new HudView.PanelButton(Loc.T("btn.restart"), UiIcon.Restart, false, Restart),
                new HudView.PanelButton(Loc.T("btn.levels"), UiIcon.Menu, false, ShowLevels));
            if (Ui1.Ready) _hud.PanelClose(() => _hud.HidePanel()); // ui1/14
        }

        private void OnApplicationPause(bool paused)
        {
            _appFocused = !paused; // ZPE süresi: arka planda sayılmaz
            // Yarım soyulmuş katmanda kalmamak için animasyonu bitirip mantıksal son durumdan çiz.
            if (paused && CurrentPhase == Phase.Animating) _board.CompleteImmediately();
            if (paused && _save != null) { _cloudDirty = true; CloudAutoSave(true); }
            // Yerel bildirimler: arka planda planlanır, öne gelince temizlenir (oyundayken bildirim yok).
            if (paused) ScheduleNotifications();
            else GameNotifications.CancelAll();
        }
    }
}
