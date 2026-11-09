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
            _board.IsGoalColor = _hud.IsGoalColor;
            _sound = SoundBank.Create(transform);
            _sound.Enabled = save.Data.sound;
            _music = MenuMusic.Create(transform);
            _music.Enabled = save.Data.sound;
            _menu = MenuView.Create(cam);
            _menu.transform.SetParent(transform, false);
            _menu.LevelChosen += i => StartLevel(i);
            // v10: tek ses ayarı hem efektleri hem müziği anında kontrol eder.
            _menu.SoundChanged += v => { _save.Data.sound = v; _sound.Enabled = v; _music.Enabled = v; _save.Save(); };
            _menu.VibrationChanged += v => { _save.Data.vibration = v; Haptics.Enabled = v; _save.Save(); if (v) Haptics.Light(); };
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
            _menu.CollectionPressed = ShowCollection;
            // v10 taş vitrini: koleksiyonda kazanılmış karta dokununca ve taş kazanılınca (3D model ya da büyük PNG).
            _showcase = KatKat.UI.StoneShowcase.Create(cam, transform);
            // v12: reklam yapılandırması, geçiş kuralları ve test reklam ekranı.
            SetupAds(cam);
            SetupHearts();
            SetupAccount();
            _menu.NeedsSignIn = () => Account.State != AccountState.SignedIn;
            _menu.StonePreviewRequested = id => ShowStone(id, false);
            _menu.ZpePressed = ShowZpeStats;
            _menu.RankPressed = ShowRanking;
            _menu.TrialPressed = ShowTrials;
            _menu.RankTabPressed = scope => ShowRanking(scope);
            _menu.RankRetryPressed = () => ShowRanking(_rankScope);
            FitCamera();
        }

        // ---- Menüler -------------------------------------------------------------------------------

        /// <summary>Test sürümü: haritadaki bütün bölümler açık (kayıt değişmez; mevcut bölüm yine kayıttan). Yayın hazırlığında kapatıldı (kullanıcı, 2026-11-09: "yayın için eksikleri tamamlayalım").</summary>
        public static bool TestUnlockAll = false;

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
        public int BackgroundPick => _bgPick;

        public void ShowLevels()
        {
            HeartForfeit();
            _bgPick = -1; // menüye dönüş: sonraki girişte zemin yeniden seçilir
            AbandonDailyIfOpen();
            _hud.HidePanel();
            SetPhase(Phase.Loading);
            _menu.NextStarGoal = NextStarGoal();
            var zpe = CurrentZpe();
            _menu.ZpeBadge = new MenuView.ZpeBadgeInfo { Has = true, Calibrating = zpe.Calibrating, Score = zpe.Score, Count = zpe.Count, Min = zpe.MinLevels };
            _menu.ShowLevels(_catalog.Count, ContinueIndex, i => _save.Data.IsCompleted(_catalog.IdAt(i)), ShowHome, -1, ShowSettings);
            _music.Enter();
            FlushStoneToasts(false);
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
            float refHeight = HudView.RefWidth / aspect;
            _cam.orthographicSize = refHeight / 200f;
            _screen = new Vector2Int(Screen.width, Screen.height);
            _backdrop.Fit(_cam);
        }

        /// <summary>Öğretici halkanın gösterildiği mekanik tanıtım bölümleri: dokunma, zincir, rulo, ataş, yıldız, buz, renk kilidi, zarf.</summary>
        private static readonly int[] HintLevels = { 0, 1, 10, 20, 40, 60, 120, 200, 300 };

        public void StartLevel(int index)
        {
            // Kalp (2026-11-09): bırakılan deneme kalp götürür; kalp yoksa bölüm açılmaz, kalp kartı çıkar.
            HeartForfeit();
            if (!HeartGate(index, () => StartLevel(index))) return;
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
            _backdrop.SetSprite(background ?? PremiumArt.Background(index));
            _backdrop.Fit(_cam);
            _board.SetTheme(theme);
            _hud.SetTheme(theme);
            // Başlık: altın dairede bölüm numarası, turkuaz hapta bölge türü (SAHİL/BAHÇE/ÇÖL).
            _hud.SetLevel(Loc.Upper(theme.LocalName), _session.State.MovesLeft, _session.State.Goals, _session.State.RollEnabled, index + 1);
            _hud.SetScore(_session.State.Score, _session.State.StarThresholds, false);
            _paintMode = false;
            _hud.HidePalette();
            _hud.SetPaint(_session.State.PaintLeft, false);
            _hud.SetTrayVisible(_session.State.SpecialsEnabled);
            _hud.SetTray(_session.State.Tray, _traySlot = -1);
            _board.SetLevelPalette(index);
            _celebrating = false;
            _celebration.Clear();
            // Eski alt öğretici bandı yerine odaklı tur (GameController.Tutorial); band tahta alanını daraltmaz.
            _hud.ShowTutorial(null);
            _hud.SetDailyLayout(_sessionKind == KatKat.Meta.SessionKind.Daily && Ui1.Ready); // ui1/17: başlık plakasına yer
            _board.Build(_session.State, _hud.BoardWorldRect(_cam));
            bool normal = _sessionKind != KatKat.Meta.SessionKind.Daily && _sessionKind != KatKat.Meta.SessionKind.Trial;
            _hud.SetRuleIcons(null);
            _hud.SetDailyHeader(null);
            _hintActive = normal && System.Array.IndexOf(HintLevels, index) >= 0 && _level.Solution.Count > 0 && (_level.Solution[0].Type == "tap" || _level.Solution[0].Type == "rotate");
            if (normal) TourBegin(index); else { _tourOn = false; HideTour(); }
            if (_hintActive || _tourOn) OutcomeAssist(KatKat.Meta.AssistFlags.Guidance);
            RefreshHint();
            if (normal) MainRulesBegin(); // v8: kural simgeleri, tür tanıtım / patron kartı
            SetPhase(Phase.Idle);
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
            if (p == Phase.Idle && _paidHint.HasValue && _session != null)
            {
                PaidHintAfterMove(null); // geri al vb. sonrası önerilen dokunuş hâlâ geçerli mi
                RefreshHint();
            }
            if (p == Phase.Idle) TourShowCurrent();
            else if (p != Phase.Targeting) HideTour();
        }

        private void RefreshHint()
        {
            if (_hintActive && _level.Solution.Count > 0)
            {
                var step = _level.Solution[0];
                // Döndürme öğreticisinde halka taban düğmesini gösterir.
                if (step.Type == "rotate") _hint.Show(_board.CellCenter(step.X, step.Y) + _board.StackAt(step.X, step.Y).RotorLocal, _board.Pitch * 0.45f);
                else _hint.Show(_board.CellCenter(step.X, step.Y), _board.Pitch);
            }
            else if (_paidHint.HasValue) _hint.Show(_board.CellCenter(_paidHint.Value.X, _paidHint.Value.Y), _board.Pitch, true);
            else _hint.Hide();
        }

        // ---- Girdi ---------------------------------------------------------------------------------

        private void Update()
        {
            if (_cam == null) return;
            OutcomeTick(Time.unscaledDeltaTime);
            TrialTickPress();
            if (_screen.x != Screen.width || _screen.y != Screen.height)
            {
                FitCamera();
                if (_session != null)
                {
                    _board.Layout(_hud.BoardWorldRect(_cam));
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
            if (CurrentPhase == Phase.Targeting)
            {
                SelectRollRow(y);
                return;
            }
            if (CurrentPhase != Phase.Idle) return;
            if (!TourAllowsCell(x, y)) return;
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
                    _sound.Play(Sfx.Invalid);
                    Haptics.Light();
                    _board.ShakeCells(r.TappedGroup);
                    _hud.ShowToast(Loc.T("toast.min3"));
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
                    else _hud.ShowToast(Loc.F("toast.lock", cell.LockRemaining, LayerPalette.ColorName(cell.LockColor)));
                }
                return;
            }
            _hintActive = false;
            PaidHintAfterMove(new Coord(x, y));
            _hint.Hide(); // ücretli ipucu tahta durunca (SetPhase Idle) yeniden görünür
            _sound.Play(Sfx.Tap);
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
            PaidHintAfterMove(null);
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
            }
            var goals = _session.State.Goals;
            var shown = new int[goals.Count];
            for (int g = 0; g < goals.Count; g++) shown[g] = -1;
            int scoreShown = _session.State.Score - r.ScoreGained;
            _chainSounded = false;
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
                    // Dalga başına tek ana ses. v10: dört vuruşlu zincir WAV'ı hamlenin ilk zincir dalgasında bir kez
                    // (sonrakilerde üst üste binmez, perde yükseltmesi yok); WAV yoksa eski kademeli sentez.
                    if (wave.Kind == WaveKind.Chain)
                    {
                        if (!_sound.HasChainWav) _sound.Play(Sfx.Chain, wave.Index - 1);
                        else if (!_chainSounded) { _chainSounded = true; _sound.Play(Sfx.Chain); }
                    }
                    else _sound.Play(wave.Kind == WaveKind.Roll ? Sfx.RollUse : Sfx.Peel);
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
        /// Domino ve özel taş anları: dalga başında övgü yazısı ve ses. 2. dalgadan itibaren büyüyen sözler
        /// ("Süper!", "Kat Kat!", "Şahane!", "Efsane!"); birleşimde Loc.T("praise.combo"); doğan taşın adı.
        /// </summary>
        private void Praise(MoveResult r, PeelWave wave)
        {
            if (wave.Kind == WaveKind.Special)
            {
                _sound.Play(Sfx.RollUse);
                Haptics.Medium();
                if (wave.ComboWith != SpecialKind.None) _hud.ShowPraise(Loc.T("praise.combo"), 3);
                return;
            }
            if (wave.Index == 0 && r.SpecialCreated != SpecialKind.None)
                _hud.ShowPraise(r.SpecialCreated == SpecialKind.Rainbow ? Loc.T("praise.rainbow") : Loc.T("praise.rocket"), 1);
            if (wave.Kind != WaveKind.Chain) return;
            string[] words = { Loc.T("praise.1"), Loc.T("praise.2"), Loc.T("praise.3"), Loc.T("praise.4") };
            int k = Mathf.Clamp(wave.Index - 1, 0, words.Length - 1);
            _hud.ShowPraise(words[k], k + 1);
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
            if (_traySlot < 0) { _hud.HideToast(); return; }
            var kind = tray[_traySlot];
            _sound.Play(Sfx.Tap);
            if (_tourOn) { TourShowCurrent(); return; } // tur kendi açıklamasını gösterir
            _hud.ShowToast(kind == SpecialKind.Rainbow ? Loc.T("toast.use.rainbow")
                : kind == SpecialKind.RocketRow ? Loc.T("toast.use.row")
                : Loc.T("toast.use.col"), 2.6f);
        }

        private void UseTraySlot(int x, int y)
        {
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
            PaidHintAfterMove(null);
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
            for (int c = 0; c < 4; c++)
            {
                var col = (LayerColor)c;
                if (col != b[x, y].Top && b.CountLayers(col) > 0) options.Add(col);
            }
            _hud.ShowPalette(b[x, y].Top, options);
            _board.ShakeCells(new[] { _paintCell });
        }

        private void OnPaintColor(LayerColor color)
        {
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
            _hud.SetGoalsImmediate(s.Goals);
            _hud.SetMoves(s.MovesLeft);
            _hud.SetTray(s.Tray, _traySlot = -1);
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
                    _celebrationScore = s.Score;
                    StartCelebration();
                    break;
                case GameStatus.LostOutOfMoves:
                case GameStatus.LostNoLegalMoves:
                case GameStatus.LostProtected:
                    SetPhase(Phase.Ended);
                    ShowLost(s.Status);
                    break;
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
                if (_outcome == null || !_outcome.Has(KatKat.Meta.AssistFlags.RewardedAd))
                    KatKat.Meta.RankStars.Record(_save.Data.meta, _level.Id, EarnedStars);
                _interstitial?.LevelCompleted(_levelIndex + 1);
            }
            if (_sessionKind == KatKat.Meta.SessionKind.Normal) CheckStoneUnlocks();
            TourOnWon();
            HeartOnWon();
            EndPlaceOffer();
            // v10: normal bölüm ödül kartı ilk gösterilirken bir kez (yıldızlar ve kart yeniden çizimi tekrar çalmaz).
            _sound.PlayReward();
            Haptics.Medium();
            // Son bölümde olmayan bir sonrakini yükleme: bölüm seçimine dön (rehber §5).
            bool hasNext = _levelIndex + 1 < _catalog.Count;
            // Büyük kutlama yalnızca son hamle zincirle bittiyse; küçük üçlüde Loc.T("win.big") yok (rehber §6).
            bool chain = _lastResult != null && _lastResult.Waves.Count > 1;
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
            if (_sessionKind == KatKat.Meta.SessionKind.Normal) _hud.SetResultBadge(ZpeChangeText());
        }

        private void ShowLost(GameStatus status)
        {
            _sound.Play(Sfx.Lose);
            OutcomeFinish(false);
            EndPlaceOffer();
            if (_sessionKind == KatKat.Meta.SessionKind.Daily) { ShowDailyLost(status); return; }
            if (_sessionKind == KatKat.Meta.SessionKind.Trial) { ShowTrialLost(status); return; }
            bool moves = status == GameStatus.LostOutOfMoves, crystal = status == GameStatus.LostProtected;
            // v12 (kullanıcı, 2026-11-08): hamle bitince "Reklam izle, +3 hamle", bölüm başına 1 kez.
            ExtraMovesOffered = CanOfferExtraMoves(status);
            var buttons = new System.Collections.Generic.List<HudView.PanelButton>();
            if (ExtraMovesOffered) buttons.Add(new HudView.PanelButton(Loc.F("ad.place.offer", _adConfig.ExtraMoves), UiIcon.Play, true, OnExtraMovesPressed, true));
            buttons.Add(new HudView.PanelButton(Loc.T("btn.undo"), UiIcon.Undo, !ExtraMovesOffered, Undo));
            buttons.Add(new HudView.PanelButton(Loc.T("btn.retry"), UiIcon.Restart, false, Restart));
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
            if (TourBlocksUndo) return; // yönlendirmeli öğretici adımları geri alınmaz
            if (!_session.Undo()) return;
            OutcomeAssist(KatKat.Meta.AssistFlags.Undo);
            OutcomeReopen();
            _hud.HidePanel();
            ResyncAll();
        }

        public void Restart()
        {
            if (CurrentPhase != Phase.Idle && CurrentPhase != Phase.Ended && CurrentPhase != Phase.Targeting) return;
            // v9 B: günlükte yeniden başlatma yeni deneme hakkı ister (ücretsiz tekrar); hak yoksa başlatılmaz.
            if (_sessionKind == KatKat.Meta.SessionKind.Daily) { DailyRestart(); return; }
            HeartForfeit();
            if (_sessionKind == KatKat.Meta.SessionKind.Normal && !HeartGate(_levelIndex, () => StartLevel(_levelIndex))) return;
            EndPlaceOffer();
            _hud.HidePanel();
            _session.Restart();
            TourOnRestart();
            OutcomeRestart();
            ResyncAll();
        }

        private void ResyncAll()
        {
            _board.HideRowPreview();
            _previewRow = -1;
            var s = _session.State;
            _board.Sync(s);
            _hud.SetMoves(s.MovesLeft);
            _hud.SetGoalsImmediate(s.Goals);
            _hud.SetScore(s.Score, s.StarThresholds, false);
            _paintMode = false;
            _hud.HidePalette();
            _hud.SetPaint(s.PaintLeft, false);
            _hud.SetTray(s.Tray, _traySlot = -1);
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
            // Yarım soyulmuş katmanda kalmamak için animasyonu bitirip mantıksal son durumdan çiz.
            if (paused && CurrentPhase == Phase.Animating) _board.CompleteImmediately();
        }
    }
}
