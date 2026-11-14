using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.Core.Live;
using KatKat.Game;
using KatKat.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Canlı maç ekranı (kullanıcı 2026-11-13: "Tasarım mevcut tasarımımızla olacak"). Oyunun kendi tahta görünümüyle
    /// (<see cref="BoardView"/>, iki katman, alt katman şerit olarak görünür) eşit boyda iki tahta: üstte rakip (bot), altta oyuncu.
    /// Aradaki şeritte skor ve sıra; sırası olanın tahtası çevresinde renkli kenar. Elden taş seçilir, kendi ya da rakip tahtada
    /// bir hücreye dokunulur; öneri ya da önizleme gösterilmez. 3'lü kendiliğinden patlar, zincir yoktur (<see cref="LiveMatch"/>).
    /// </summary>
    public sealed class LiveMatchView : MonoBehaviour
    {
        public const int Human = 0, Bot = 1;

        public event Action Exit;
        public LiveRules Rules = new LiveRules();
        public LiveBotKind BotKind = LiveBotKind.Balanced;
        /// <summary>Botun düşünme süresi (sn); testler 0 yapar.</summary>
        public float BotDelay = 0.9f;
        public SoundBank Sound;
        public List<LiveLayout> Pool;
        public int PoolIndex;
        public LiveMatch Match { get; private set; }
        public bool Animating => _animating > 0;
        public int Selected => _selected;
        public string Message => _message != null ? _message.text : null;
        public bool EndShown => _end != null;

        private Camera _cam;
        private Backdrop _backdrop;
        private PeelDirector _director;
        private readonly BoardView[] _boards = new BoardView[2];
        private readonly LevelState[] _states = new LevelState[2];
        private readonly SpriteRenderer[] _rims = new SpriteRenderer[2];
        private RectTransform _safe, _handRow, _botHandRow, _end;
        private TMPro.TextMeshProUGUI _scoreBot, _scoreYou, _turn, _message, _nameBot, _nameYou;
        private Image _pillBot, _pillYou;
        private readonly List<RectTransform> _chips = new List<RectTransform>();
        private int _selected = -1, _animating, _seq, _first;
        private float _botAt = -1f, _passAt = -1f, _clock;
        private LiveGenerator.Rng _rng = new LiveGenerator.Rng(20261114);
        private Vector2Int _screen;

        private static readonly Color YouColor = new Color32(0x1B, 0xA9, 0x9E, 0xFF), BotColor = new Color32(0xE0, 0x6A, 0x50, 0xFF);

        public static LiveMatchView Create(Camera cam, Transform parent)
        {
            var go = new GameObject("LiveMatch");
            if (parent != null) go.transform.SetParent(parent, false);
            var v = go.AddComponent<LiveMatchView>();
            v.Build(cam);
            return v;
        }

        /// <summary>Doğrulanmış havuz (Resources/Live/live_pool_v3: farklı ama denk eller, alt katman üstten farklı).</summary>
        public static List<LiveLayout> LoadPool()
        {
            var ta = Resources.Load<TextAsset>("Live/live_pool_v3");
            return ta != null ? LiveLayout.ParsePool(ta.text) : new List<LiveLayout>();
        }

        private void Build(Camera cam)
        {
            _cam = cam;
            FitCamera();
            _backdrop = Backdrop.Create(transform);
            _backdrop.SetSprite(Ui3.GameBackground(0) ?? PremiumArt.Background(0));
            _backdrop.Fit(_cam);
            _director = new GameObject("PeelDirector").AddComponent<PeelDirector>();
            _director.transform.SetParent(transform, false);
            for (int p = 0; p < 2; p++)
            {
                var rim = new GameObject(p == Human ? "RimYou" : "RimBot").AddComponent<SpriteRenderer>();
                rim.transform.SetParent(transform, false);
                rim.sprite = ProceduralArt.TraySprite;
                rim.drawMode = SpriteDrawMode.Sliced;
                rim.sortingOrder = 0;
                _rims[p] = rim;
                var b = new GameObject(p == Human ? "BoardYou" : "BoardBot").AddComponent<BoardView>();
                b.transform.SetParent(transform, false);
                b.Init(_director);
                b.SetTheme(ThemeDefinition.Coast);
                b.SetLevelPalette(0);
                _boards[p] = b;
            }
            BuildCanvas();
            ComputeLayout();
            PlaceUi();
            Pool ??= LoadPool();
            NewMatch(0, 0);
        }

        private void FitCamera()
        {
            float aspect = _cam.aspect > 0f ? _cam.aspect : 9f / 16f;
            _cam.orthographic = true;
            _cam.orthographicSize = UiAspect.CanvasRef(aspect).y / 200f;
            _screen = new Vector2Int(Screen.width, Screen.height);
        }

        // ---- Yerleşim (referans birimi; güvenli alanın üstünden) ---------------------------------------------------
        // Kullanıcı (2026-11-14): "iki tarafın tahtası da yanlara doğru genişlesin, üste dayalı olmasın, oyun alanı ortada olsun".
        // Üst çubuk sabit; oyun bloğu (rakibin eli, rakip tahtası, skor şeridi, oyuncu tahtası, el, mesaj) kalan yükseklikte
        // tahtalar en büyük olacak biçimde boyutlanır ve dikeyde ortalanır. İki tahta bandı her zaman eşit (eşit boy).

        private const float TopBarH = 160f, BotHandH = 56f, StripH = 124f, HandH = 140f, MsgH = 48f, Gap = 6f, SideM = 22f;
        /// <summary>6×6, iki katmanlı tahtanın yükseklik/genişlik oranı (BoardView.Layout: satır adımı ~1,1, tepsi payı dahil).</summary>
        private const float BoardHPerW = 6.97f / 6.39f;
        private float _botTop, _botBottom, _youTop, _youBottom, _stripY, _handY, _msgY, _botHandY;

        /// <summary>Güvenli sütunun referans boyutu (genişlik, yükseklik).</summary>
        private Vector2 SafeRefSize()
        {
            var canvasRef = UiAspect.CanvasRef(_cam.aspect);
            var n = UiAspect.Column(SafeAreaFitter.CurrentNormalizedRaw(), _cam.aspect);
            return new Vector2(canvasRef.x * n.width, canvasRef.y * n.height);
        }

        /// <summary>Bantları güvenli yüksekliğe göre hesaplar (referans birimi, güvenli alanın üstünden aşağı).</summary>
        private void ComputeLayout()
        {
            var safe = SafeRefSize();
            float avail = safe.y - TopBarH;
            float fixedH = BotHandH + Gap + StripH + 2f * Gap + HandH + Gap + MsgH + 2f * Gap;
            // Tahta genişliği sütunu doldurunca daha uzun bandın yararı yok: üst sınır genişlikten.
            float bandMax = (safe.x - 2f * SideM) * BoardHPerW;
            float band = Mathf.Max(260f, Mathf.Min((avail - fixedH) * 0.5f, bandMax));
            float block = fixedH + 2f * band;
            float y = TopBarH + Mathf.Max(0f, (avail - block) * 0.5f);
            _botHandY = y + BotHandH * 0.5f; y += BotHandH + Gap;
            _botTop = y; _botBottom = y + band; y = _botBottom + Gap;
            _stripY = y + StripH * 0.5f; y += StripH + Gap;
            _youTop = y; _youBottom = y + band; y = _youBottom + Gap;
            _handY = y + HandH * 0.5f; y += HandH + Gap;
            _msgY = y + MsgH * 0.5f;
        }

        /// <summary>Referans birimiyle (güvenli sütunun üstünden) verilen dikey bandın dünya dikdörtgeni.</summary>
        private Rect WorldBand(float topRef, float bottomRef)
        {
            var canvasRef = UiAspect.CanvasRef(_cam.aspect);
            float refH = canvasRef.y, refW = canvasRef.x;
            var n = UiAspect.Column(SafeAreaFitter.CurrentNormalizedRaw(), _cam.aspect);
            float left = n.xMin * refW + SideM, right = n.xMax * refW - SideM;
            float top = n.yMax * refH - topRef, bottom = n.yMax * refH - bottomRef;
            float k = _cam.orthographicSize * 2f / refH;
            var c = _cam.transform.position;
            return Rect.MinMaxRect((left - refW * 0.5f) * k + c.x, (bottom - refH * 0.5f) * k + c.y,
                (right - refW * 0.5f) * k + c.x, (top - refH * 0.5f) * k + c.y);
        }

        private Rect BoardArea(int p) => p == Human ? WorldBand(_youTop, _youBottom) : WorldBand(_botTop, _botBottom);

        /// <summary>Arayüz parçalarını hesaplanan bantlara taşır.</summary>
        private void PlaceUi()
        {
            if (_strip == null) return;
            _botHandRow.anchoredPosition = new Vector2(0f, -_botHandY);
            _strip.anchoredPosition = new Vector2(0f, -_stripY);
            _handRow.anchoredPosition = new Vector2(0f, -_handY);
            _message.rectTransform.anchoredPosition = new Vector2(0f, -_msgY);
        }

        private RectTransform _strip;

        private void LayoutBoards()
        {
            float keep = BoardView.AreaScale;
            BoardView.AreaScale = 1f; // ana oyunun genel küçültmesi burada uygulanmaz (iki tahta alanı zaten dar)
            try
            {
                for (int p = 0; p < 2; p++)
                {
                    _boards[p].Layout(BoardArea(p));
                    if (!_boards[p].IsPlaying) _boards[p].Sync(_states[p]);
                    LayoutRim(p);
                }
            }
            finally { BoardView.AreaScale = keep; }
        }

        private void BuildBoards()
        {
            float keep = BoardView.AreaScale;
            BoardView.AreaScale = 1f;
            try
            {
                for (int p = 0; p < 2; p++)
                {
                    _boards[p].CompleteImmediately();
                    _states[p] = new LevelState { Board = Match.Boards[p] };
                    _boards[p].Build(_states[p], BoardArea(p));
                    LayoutRim(p);
                }
            }
            finally { BoardView.AreaScale = keep; }
        }

        /// <summary>Sırası olanın kenarı: tahta tepsisinin biraz dışında, renkli (oyuncu turkuaz, bot mercan).</summary>
        private void LayoutRim(int p)
        {
            var b = _boards[p];
            if (b.Width == 0) return;
            float pitch = b.Pitch, row = b.RowPitch;
            var a = b.CellCenter(0, 0);
            var z = b.CellCenter(b.Width - 1, b.Height - 1);
            // Tahtanın zemin plakası hücrelerin 0,25 hücre dışına taşar (BoardView.LayoutFloorTiles); kenar onun da dışında.
            const float Pad = 0.25f, Rim = 0.2f;
            float cx = (a.x + z.x) * 0.5f, cy = (a.y + z.y) * 0.5f - pitch * 0.06f;
            var size = new Vector2(pitch * (b.Width + 2f * Pad) + 2f * Rim * pitch, row * (b.Height + 2f * Pad) + 2f * Rim * pitch);
            var rim = _rims[p];
            float border = Mathf.Max(1f, rim.sprite.border.x);
            float s = pitch * 0.3f / (border / 100f);
            rim.transform.localPosition = new Vector3(cx, cy, 0f);
            rim.transform.localScale = new Vector3(s, s, 1f);
            rim.size = size / s;
        }

        // ---- Arayüz -------------------------------------------------------------------------------------------

        private void BuildCanvas()
        {
            var go = new GameObject("LiveHud", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 600;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = UiAspect.Match;
            go.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }
            _safe = UiFactory.Stretch(UiFactory.Rect("SafeArea", go.transform));
            _safe.gameObject.AddComponent<SafeAreaFitter>();

            // Üst: geri düğmesi ve başlık plakası.
            var back = MenuView.ShellRoundButton("Back", _safe, Ui3.Art("icon-back") ?? UiArt.Icon(UiIcon.Back), 110f, () => Exit?.Invoke());
            UiFactory.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(90f, -95f), new Vector2(110f, 110f));
            var title = Ui3.Surface("TitlePill", _safe, 44f, Ui3.Purple, Ui3.PurpleDark, 92f, 3f, Ui3.Brass);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(460f, 92f));
            var tt = UiFactory.Title("Title", title.transform, Loc.T("live.title"), 50, Color.white, Ui3.PurpleDark);
            UiFactory.Stretch(tt.rectTransform, 20f, 20f, 4f, 8f);
            tt.enableAutoSizing = true; tt.fontSizeMax = 50; tt.fontSizeMin = 28;

            // Rakibin eli (küçük, açık): neyle saldırabileceği görünür.
            _botHandRow = UiFactory.Rect("BotHand", _safe);
            UiFactory.Place(_botHandRow, new Vector2(0.5f, 1f), new Vector2(0f, -184f), new Vector2(900f, 56f));

            // Orta şerit: skorlar ve sıra.
            var strip = Ui3.Surface("ScoreStrip", _safe, 40f, Ui3.IvoryTop, Ui3.IvoryBottom, 124f, 3f, Ui3.Brass);
            UiFactory.Place(strip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -950f), new Vector2(1000f, 124f));
            _strip = strip.rectTransform;
            _pillBot = Ui3.Surface("BotPill", strip.transform, 36f, BotColor, BotColor * 0.85f, 104f, 0f);
            UiFactory.Place(_pillBot.rectTransform, new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(260f, 104f));
            _pillYou = Ui3.Surface("YouPill", strip.transform, 36f, YouColor, YouColor * 0.85f, 104f, 0f);
            UiFactory.Place(_pillYou.rectTransform, new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(260f, 104f));
            _nameBot = Label("BotName", _pillBot.transform, Loc.T("live.bot"), 26, new Vector2(0f, 26f));
            _scoreBot = Label("BotScore", _pillBot.transform, "0", 52, new Vector2(0f, -16f));
            _nameYou = Label("YouName", _pillYou.transform, Loc.T("live.you"), 26, new Vector2(0f, 26f));
            _scoreYou = Label("YouScore", _pillYou.transform, "0", 52, new Vector2(0f, -16f));
            _turn = UiFactory.Text("Turn", strip.transform, "", 34, Ui3.Ink);
            UiFactory.Place(_turn.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 110f));
            _turn.enableAutoSizing = true; _turn.fontSizeMax = 34; _turn.fontSizeMin = 20;

            // Alt: oyuncunun eli ve mesaj satırı.
            var hand = Ui3.Surface("HandBar", _safe, 40f, Ui3.IvoryTop, Ui3.IvoryBottom, 140f, 3f, Ui3.Brass);
            UiFactory.Place(hand.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -1752f), new Vector2(1000f, 140f));
            _handRow = hand.rectTransform;
            _message = UiFactory.Text("Message", _safe, "", 34, Ui3.Ink);
            UiFactory.Place(_message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -1862f), new Vector2(960f, 56f));
            _message.enableAutoSizing = true; _message.fontSizeMax = 34; _message.fontSizeMin = 20;
        }

        private static TMPro.TextMeshProUGUI Label(string name, Transform parent, string text, int size, Vector2 pos)
        {
            var t = UiFactory.Title(name, parent, text, size, Color.white, new Color(0f, 0f, 0f, 0.35f));
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(240f, size + 12f));
            t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = 18;
            return t;
        }

        // ---- Maç ----------------------------------------------------------------------------------------------

        /// <summary>Havuzdaki <paramref name="index"/>. dizilimle yeni maç; <paramref name="first"/> ilk oynayan.</summary>
        public void NewMatch(int index, int first)
        {
            if (_end != null) { UiFactory.Kill(_end.gameObject); _end = null; }
            if (Pool == null || Pool.Count == 0) { SetMessage(Loc.T("live.nopool")); return; }
            PoolIndex = (index % Pool.Count + Pool.Count) % Pool.Count;
            _first = first;
            Match = new LiveMatch(Pool[PoolIndex], Rules, first);
            _seq = 0;
            _selected = -1;
            _animating = 0;
            _botAt = _passAt = -1f;
            _shown[0] = _shown[1] = 0;
            _popAt[0] = _popAt[1] = -9f;
            BuildBoards();
            SetMessage(Match.Current == Human ? Loc.T(Rules.TapReadyGroups ? "live.pick.tap" : "live.pick") : "");
            Refresh();
        }

        /// <summary>El yuvası seçimi (aynı yuva ikinci kez: seçim kalkar). Testler doğrudan çağırır.</summary>
        public void SelectSlot(int slot)
        {
            if (Match == null || Match.IsOver || Match.Current != Human || Animating) return;
            if (slot < 0 || slot >= Match.Hands[Human].Count) return;
            _selected = _selected == slot ? -1 : slot;
            Sound?.Play(Sfx.Tap, 0, 0.6f);
            SetMessage(Loc.T(_selected < 0 ? "live.pick" : "live.where"));
            Refresh();
        }

        /// <summary>Hücreye dokunuş: kendi tahtada değiştir-patlat, rakip tahtada değiştir. Önizleme yok; geçerliyse hemen uygulanır.</summary>
        public void TapCell(LiveSide side, int x, int y)
        {
            if (Match == null || Match.IsOver || Match.Current != Human || Animating) return;
            // Hazır 3'lü (taş koymadan patlatma; kullanıcı 2026-11-14): seçim yokken ya da seçili taş zaten aynı renkteyken.
            if (side == LiveSide.Own && Match.Rules.TapReadyGroups && IsReady(Match.Boards[Human], x, y)
                && (_selected < 0 || Match.Hands[Human][_selected] == Match.Boards[Human][x, y].Top))
            {
                var color = Match.Boards[Human][x, y].Top;
                var rt = Match.Submit(LiveCommand.Tap(Human, ++_seq, x, y), 0);
                if (rt.Accepted)
                {
                    _selected = -1;
                    PlayTap(Human, x, y, color, rt);
                    return;
                }
                _seq--;
            }
            if (_selected < 0) { SetMessage(Loc.T(Match.Rules.TapReadyGroups ? "live.pick.tap" : "live.pick")); Sound?.Play(Sfx.Invalid); return; }
            var cmd = LiveCommand.Swap(Human, ++_seq, _selected, side, x, y);
            var r = Match.Submit(cmd, 0);
            if (!r.Accepted)
            {
                _seq--;
                SetMessage(RejectText(r.Reject));
                Sound?.Play(Sfx.Invalid);
                var board = _boards[side == LiveSide.Own ? Human : Bot];
                board.ShakeCells(new[] { new Coord(x, y) });
                return;
            }
            _selected = -1;
            Play(Human, side, r);
        }

        private static string RejectText(LiveReject r) => r switch
        {
            LiveReject.SameColor => Loc.T("live.rej.same"),
            LiveReject.AttackMakesGroup => Loc.T("live.rej.group"),
            LiveReject.EmptyCell => Loc.T("live.rej.empty"),
            LiveReject.OutOfBounds => Loc.T("live.rej.empty"),
            _ => Loc.T("live.rej.other"),
        };

        /// <summary>Kabul edilen hamlenin sunumu: hedef taş boyanır (oyunun boya animasyonu), kendi tahtada grup soyulur.</summary>
        private void Play(int player, LiveSide side, LiveResult r)
        {
            int boardOwner = side == LiveSide.Own ? player : 1 - player;
            var board = _boards[boardOwner];
            var bs = Match.Boards[boardOwner];
            int tx = bs.X(r.Target), ty = bs.Y(r.Target);
            var paint = new MoveResult { Accepted = true, Command = Command.Paint(tx, ty, r.Color), Painted = new Coord(tx, ty), PaintTo = r.Color };
            _animating++;
            Refresh();
            board.Play(paint, _ => ScoreTarget(player), null, null, () =>
            {
                if (r.Peeled.Count > 0) Peel(player, boardOwner, tx, ty, r.Color, r);
                else Done(boardOwner);
            });
            if (r.Kind == LiveMoveKind.Attack) SetMessage(Loc.T(player == Human ? "live.attack.you" : "live.attack.bot"));
            else if (r.Gained > 0) SetMessage(Loc.F(player == Human ? "live.score.you" : "live.score.bot", r.Gained));
            else SetMessage(Loc.T(player == Human ? "live.prep.you" : "live.prep.bot"));
        }

        /// <summary>Hazır gruba dokunarak patlatma: boyama yok, doğrudan soyulma.</summary>
        private void PlayTap(int player, int x, int y, LayerColor color, LiveResult r)
        {
            _animating++;
            Refresh();
            Peel(player, player, x, y, color, r);
            SetMessage(Loc.F(player == Human ? "live.tap.you" : "live.tap.bot", r.Gained));
        }

        /// <summary>
        /// Soyulma dalgası: oyunun soyulma animasyonu; parçalar oyuncunun skor kutusuna uçar (kullanıcı 2026-11-14: "Patlayan
        /// taşların parçaları skor alanına gider"). Skor yazısı parçalar varınca artar ve zıplar.
        /// </summary>
        private void Peel(int player, int boardOwner, int tx, int ty, LayerColor removed, LiveResult r)
        {
            var bs = Match.Boards[boardOwner];
            Sound?.Play(Sfx.Peel);
            var wave = new PeelWave { Kind = WaveKind.Tap, Index = 0, GoalProgressAfter = new int[0] };
            foreach (int i in r.Peeled)
            {
                var cell = bs[i];
                int rem = cell.Layers.Count;
                wave.Cells.Add(new CellPeel(bs.X(i), bs.Y(i), removed, rem, rem > 0 ? cell.Top : default));
            }
            var peel = new MoveResult { Accepted = true, Command = Command.Tap(tx, ty) };
            peel.Waves.Add(wave);
            _boards[boardOwner].Play(peel, _ => ScoreTarget(player), null, _ => ScoreArrived(player), () => Done(boardOwner));
        }

        private readonly int[] _shown = new int[2];
        private readonly float[] _popAt = { -9f, -9f };

        /// <summary>Skor kutusunda görünen puan (parçalar varmadan eski değer).</summary>
        public int ShownScore(int player) => _shown[player];

        private void ScoreArrived(int player)
        {
            _shown[player] = Match.Scores[player];
            _popAt[player] = _clock;
            Sound?.Play(Sfx.Tap, 2, 0.7f);
            Refresh();
        }

        private void Done(int boardOwner)
        {
            _boards[boardOwner].Sync(_states[boardOwner]);
            _animating = Mathf.Max(0, _animating - 1);
            for (int p = 0; p < 2; p++) if (_shown[p] != Match.Scores[p]) { _shown[p] = Match.Scores[p]; _popAt[p] = _clock; }
            Refresh();
            if (Match.IsOver) ShowEnd();
        }

        private static bool IsReady(BoardState b, int x, int y) =>
            b[x, y].Active && b[x, y].Layers.Count > 0 && GroupFinder.Component(b, x, y).Count >= LiveRules.MinGroup;

        private Vector3 ScoreTarget(int player)
        {
            var t = player == Human ? _scoreYou : _scoreBot;
            return t != null ? t.transform.position : Vector3.zero;
        }

        private void Update()
        {
            if (_cam == null || Match == null) return;
            if (_screen.x != Screen.width || _screen.y != Screen.height) { FitCamera(); _backdrop.Fit(_cam); ComputeLayout(); PlaceUi(); LayoutBoards(); }
            _clock += Time.unscaledDeltaTime;
            PulseRim();
            PopScores();
            if (Match.IsOver || Animating || _end != null) return;
            if (Match.Current == Bot)
            {
                if (_botAt < 0f) { _botAt = _clock + BotDelay; SetMessage(Loc.T("live.bot.thinking")); Refresh(); }
                if (_clock >= _botAt) { _botAt = -1f; BotMove(); }
                return;
            }
            if (!Match.HasLegalMove(Human))
            {
                if (_passAt < 0f) { _passAt = _clock + 1.2f; SetMessage(Loc.T("live.pass.you")); }
                if (_clock >= _passAt) { _passAt = -1f; Match.Submit(LiveCommand.Pass(Human, ++_seq), 0); Refresh(); if (Match.IsOver) ShowEnd(); }
                return;
            }
            HandleInput();
        }

        /// <summary>Botun hamlesi (testler doğrudan çağırabilir).</summary>
        public void BotMove()
        {
            if (Match == null || Match.IsOver || Match.Current != Bot) return;
            var cmd = LiveBots.Choose(Match, BotKind, _rng);
            cmd.Seq = Match.NextSeq(Bot);
            var tapColor = cmd.Type == LiveCommandType.TapGroup ? Match.Boards[Bot][cmd.X, cmd.Y].Top : default;
            var r = Match.Submit(cmd, 0);
            if (r.Accepted && r.Kind == LiveMoveKind.Tap) { PlayTap(Bot, cmd.X, cmd.Y, tapColor, r); return; }
            if (!r.Accepted || r.Kind == LiveMoveKind.Pass)
            {
                if (!r.Accepted) Match.Submit(LiveCommand.Pass(Bot, Match.NextSeq(Bot)), 0);
                SetMessage(Loc.T("live.pass.bot"));
                Refresh();
                if (Match.IsOver) ShowEnd();
                return;
            }
            Play(Bot, cmd.Side, r);
        }

        private void HandleInput()
        {
            if (!TryGetPress(out var screen)) return;
            var world = (Vector2)_cam.ScreenToWorldPoint(screen);
            for (int p = 0; p < 2; p++)
            {
                if (_boards[p].TryCellAt(world, out int x, out int y))
                {
                    var c = Match.Boards[p][x, y];
                    if (!c.Active) return;
                    TapCell(p == Human ? LiveSide.Own : LiveSide.Opponent, x, y);
                    return;
                }
            }
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

        /// <summary>Skor zıplaması: parçalar varınca yazı kısa süre büyüyüp geri döner.</summary>
        private void PopScores()
        {
            for (int p = 0; p < 2; p++)
            {
                var t = p == Human ? _scoreYou : _scoreBot;
                if (t == null) continue;
                float k = Mathf.Clamp01((_clock - _popAt[p]) / 0.35f);
                float s = 1f + 0.45f * Mathf.Sin(k * Mathf.PI);
                t.rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }

        private void PulseRim()
        {
            if (Match == null) return;
            float a = 0.75f + 0.25f * Mathf.Sin(_clock * 4f);
            for (int p = 0; p < 2; p++)
            {
                bool on = !Match.IsOver && Match.Current == p;
                var c = p == Human ? YouColor : BotColor;
                _rims[p].enabled = on;
                _rims[p].color = new Color(c.r, c.g, c.b, a);
            }
        }

        // ---- Görünüm --------------------------------------------------------------------------------------------

        private void SetMessage(string text)
        {
            if (_message != null) Loc.Set(_message, text ?? "");
        }

        /// <summary>Skor, sıra, eller ve seçim (durumdan yeniden çizilir).</summary>
        public void Refresh()
        {
            if (Match == null || _turn == null) return;
            // Skor, parçalar skor kutusuna varınca artar (ScoreArrived); hamle bitince gerçek skor (Done).
            Loc.Set(_scoreYou, _shown[Human].ToString());
            Loc.Set(_scoreBot, _shown[Bot].ToString());
            int turn = Mathf.Min(Match.Rules.TurnsPerPlayer, Match.TurnsUsed[Match.Current] + 1);
            string who = Match.IsOver ? Loc.T("live.over") : Loc.T(Match.Current == Human ? "live.turn.you" : "live.turn.bot");
            Loc.Set(_turn, who + "\n" + Loc.F("live.round", turn, Match.Rules.TurnsPerPlayer));
            _pillYou.color = Match.Current == Human && !Match.IsOver ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            _pillBot.color = Match.Current == Bot && !Match.IsOver ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            BuildHand();
            BuildBotHand();
            PulseRim();
        }

        private void BuildHand()
        {
            foreach (var c in _chips) if (c != null) UiFactory.Kill(c.gameObject);
            _chips.Clear();
            var hand = Match.Hands[Human];
            const float S = 104f, Gap = 116f;
            float x0 = -(hand.Count - 1) * Gap * 0.5f;
            bool active = Match.Current == Human && !Match.IsOver && !Animating;
            for (int i = 0; i < hand.Count; i++)
            {
                int k = i;
                bool on = i == _selected;
                var chip = UiFactory.SpriteButton("Stone" + i, _handRow, PremiumArt.Chip(hand[i]) ?? UiArt.Chip(hand[i]), Vector2.one * (on ? 118f : S), () => SelectSlot(k));
                chip.GetComponent<Image>().preserveAspect = true;
                chip.GetComponent<Image>().color = active ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                var rt = (RectTransform)chip.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x0 + i * Gap, on ? 10f : 0f);
                if (on)
                {
                    var ring = UiFactory.Image("Ring", rt, UiArt.Icon(UiIcon.Ring), UiArt.Gold);
                    UiFactory.Stretch(ring.rectTransform, -14f, -14f, -14f, -14f);
                }
                _chips.Add(rt);
            }
        }

        private readonly List<RectTransform> _botChips = new List<RectTransform>();

        private void BuildBotHand()
        {
            foreach (var c in _botChips) if (c != null) UiFactory.Kill(c.gameObject);
            _botChips.Clear();
            var hand = Match.Hands[Bot];
            const float S = 56f, Gap = 64f;
            float x0 = -(hand.Count - 1) * Gap * 0.5f;
            for (int i = 0; i < hand.Count; i++)
            {
                var img = UiFactory.Image("BotStone" + i, _botHandRow, PremiumArt.Chip(hand[i]) ?? UiArt.Chip(hand[i]), Color.white);
                img.preserveAspect = true;
                UiFactory.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x0 + i * Gap, 0f), new Vector2(S, S));
                _botChips.Add(img.rectTransform);
            }
        }

        // ---- Maç sonu -----------------------------------------------------------------------------------------

        private void ShowEnd()
        {
            if (_end != null) return;
            int w = Match.Winner;
            Sound?.Play(w == Human ? Sfx.Peel : Sfx.Tap);
            var scrim = UiFactory.Image("EndScrim", _safe.parent, null, Ui3.ScrimColor);
            scrim.raycastTarget = true;
            UiFactory.Stretch(scrim.rectTransform);
            _end = scrim.rectTransform;
            var card = Ui3.Surface("Card", scrim.transform, 48f, Ui3.IvoryTop, Ui3.IvoryBottom, 620f, 4f, Ui3.Brass);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 620f));
            string head = w == Human ? Loc.T("live.win") : w == Bot ? Loc.T("live.lose") : Loc.T("live.draw");
            var t = UiFactory.Title("Result", card.transform, head, 72, w == Human ? YouColor : w == Bot ? BotColor : Ui3.Ink, Ui3.RimShadow);
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(760f, 100f));
            var s = UiFactory.Text("Scores", card.transform, Loc.F("live.final", Match.Scores[Human], Match.Scores[Bot]), 44, Ui3.Ink);
            UiFactory.Place(s.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(760f, 64f));
            string reason = TieReason();
            if (reason != null)
            {
                var rr = UiFactory.Text("Reason", card.transform, reason, 30, Ui3.InkSecondary);
                UiFactory.Place(rr.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -265f), new Vector2(740f, 80f));
                rr.enableAutoSizing = true; rr.fontSizeMax = 30; rr.fontSizeMin = 20;
            }
            Ui1.Button("Again", card.transform, Loc.T("live.again"), true, new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(560f, 110f),
                () => NewMatch(PoolIndex + 1, 1 - _first));
            Ui1.Button("Home", card.transform, Loc.T("live.home"), false, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(560f, 100f),
                () => Exit?.Invoke(), null, 38);
        }

        /// <summary>Eşit puanda karar gerekçesi (oyuncunun dilinde); eşit değilse null.</summary>
        private string TieReason()
        {
            if (Match.Scores[0] != Match.Scores[1]) return null;
            var r = Match.Rules.TieBreak;
            if ((r == LiveTieBreak.BiggestGroup || r == LiveTieBreak.BiggestGroupThenSecond) && Match.BestMove[Human] != Match.BestMove[Bot])
                return Loc.F("live.tie.biggest", Match.BestMove[Human], Match.BestMove[Bot]);
            if (r == LiveTieBreak.SecondMover || r == LiveTieBreak.BiggestGroupThenSecond) return Loc.T("live.tie.second");
            return null;
        }

        /// <summary>Testler/yakalama: animasyonları hemen bitirir.</summary>
        public void CompleteAnimations()
        {
            for (int k = 0; k < 400 && Animating; k++)
                for (int p = 0; p < 2; p++) { _boards[p].Advance(20f); _director.Advance(20f); }
        }

        /// <summary>Yakalama: animasyonları <paramref name="seconds"/> kadar ilerletir (parça uçuşunun ortası).</summary>
        public void AdvanceAnimations(float seconds)
        {
            // BoardView/Director.Advance milisaniye alır; 20 ms adımlarla (CompleteAnimations ile aynı).
            for (float ms = seconds * 1000f; ms > 0f; ms -= 20f)
            {
                float d = Mathf.Min(20f, ms);
                for (int p = 0; p < 2; p++) _boards[p].Advance(d);
                _director.Advance(d);
                _clock += d / 1000f;
            }
            PopScores();
        }

        public BoardView BoardOf(int player) => _boards[player];
    }
}
