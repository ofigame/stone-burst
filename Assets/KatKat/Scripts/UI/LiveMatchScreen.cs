using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using KatKat.Core;
using KatKat.Core.Live;
using KatKat.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace KatKat.UI
{
    /// <summary>
    /// Canlı maç prototipi: tek cihazda iki oyuncu sırayla (docs/vr1/CLAUDE_CANLI_MAC.md §5). Yalnız geliştirici girişinden
    /// açılır (Kat Kat/Canlı maç/Prototipi oyna); mağaza sürümünde görünmez. Metinler yalnız Türkçe (deneme ekranı).
    /// Bakış sıradaki oyuncudandır: üstte rakibin tahtası ve eli, altta sıradakinin tahtası, en altta eli. İki el de açık.
    /// Akış: el taşına dokun → kendi tahtada puan hedefleri ve grupları, rakipte değişebilecek hücreler önizlenir →
    /// hücreye dokun (önizleme) → aynı hücreye yeniden dokun (onay). Ayar açıksa hazır gruba el taşı seçmeden dokunulur.
    /// Kurallar ve doğrulama <see cref="LiveMatch"/>'tedir; bu sınıf yalnız gösterir ve komut gönderir.
    /// </summary>
    public sealed class LiveMatchScreen : MonoBehaviour
    {
        public const float RefW = 1080f, RefH = 1920f;
        private const float OwnCell = 120f, OppCell = 92f, HandCell = 112f;

        private static readonly Color Bg = new Color32(0x16, 0x2A, 0x33, 0xFF);
        private static readonly Color Slot = new Color32(0x22, 0x3D, 0x48, 0xFF);
        private static readonly Color Teal = new Color32(0x3F, 0xD0, 0xC0, 0xFF);
        private static readonly Color Gold = new Color32(0xFF, 0xC8, 0x4A, 0xFF);
        private static readonly Color Attack = new Color32(0xFF, 0x6B, 0x5E, 0xFF);
        private static readonly Color TextCol = new Color32(0xF2, 0xF6, 0xF7, 0xFF);
        private static readonly Color Dim = new Color32(0x0B, 0x16, 0x1B, 0xB0);

        public LiveRules Rules = new LiveRules();
        public List<LiveLayout> Pool;
        public int PoolIndex;
        public LiveMatch Match { get; private set; }

        private int _first;
        private int _selSlot = -1;
        private LiveSide _pendSide;
        private int _pendTarget = -1;
        private bool _pendTap;
        private string _message = "";
        private readonly Stopwatch _moveClock = new Stopwatch();

        private RectTransform _root;
        private CellUi[] _own, _opp;
        private Image[] _hand, _oppHand;
        private Image[] _handFrame;
        private TMP_Text _title, _oppHeader, _ownHeader, _status, _msg, _tapLabel;
        private Button _passBtn;
        private GameObject _endPanel;
        private TMP_Text _endText;

        private sealed class CellUi
        {
            public Image Stone, Overlay, Frame;
            public TMP_Text Count;
        }

        private void Awake()
        {
            if (_root == null) Build(null);
        }

        private void Start()
        {
            if (Match == null) NewMatch(0, 0);
        }

        /// <summary>Havuzu Resources'tan yükler (Live/live_pool_v1).</summary>
        public static List<LiveLayout> LoadPool()
        {
            var ta = Resources.Load<TextAsset>("Live/live_pool_v1");
            return ta != null ? LiveLayout.ParsePool(ta.text) : new List<LiveLayout>();
        }

        // ---- Kurulum ----------------------------------------------------------------------------------------------

        /// <summary>Tuvali kurar. cam null: ekran üstü; yakalama aracı kamera verir.</summary>
        public void Build(Camera cam)
        {
            Pool ??= LoadPool();
            var canvasGo = new GameObject("LiveMatchCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = cam != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefW, RefH);
            scaler.matchWidthOrHeight = UiAspect.Current > UiAspect.Phone + 0.01f ? 1f : 0f;
            canvasGo.AddComponent<GraphicRaycaster>();
            if (cam == null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var bg = NewImage(canvasGo.transform, "Bg", Bg);
            Stretch(bg.rectTransform);
            var safe = new GameObject("Safe", typeof(RectTransform));
            safe.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)safe.transform);
            safe.AddComponent<SafeAreaFitter>();
            var root = new GameObject("Root", typeof(RectTransform));
            root.transform.SetParent(safe.transform, false);
            _root = (RectTransform)root.transform;
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(RefW, RefH);

            float y = 24f;
            _title = Label(_root, "Title", "Canlı maç · prototip", 34, new Vector2(0, y), new Vector2(1000, 50), TextAlignmentOptions.Center);
            y += 58f;
            Button(_root, "NewLayout", "Sonraki dizilim", new Vector2(-350, y), new Vector2(320, 76), () => NewMatch(PoolIndex + 1, 0));
            Button(_root, "Flip", "Aynısı, sıra ters", new Vector2(0, y), new Vector2(320, 76), () => NewMatch(PoolIndex, 1 - _first));
            _tapLabel = Button(_root, "TapToggle", "", new Vector2(350, y), new Vector2(320, 76), ToggleTap).GetComponentInChildren<TMP_Text>();
            y += 96f;

            _oppHeader = Label(_root, "OppHeader", "", 32, new Vector2(0, y), new Vector2(1000, 46), TextAlignmentOptions.Center);
            y += 50f;
            _oppHand = HandRow(_root, "OppHand", new Vector2(0, y), 60f, null, out _);
            y += 74f;
            _opp = Grid(_root, "OppBoard", new Vector2(0, y), OppCell, LiveSide.Opponent);
            y += OppCell * Rules.Height + 18f;

            _status = Label(_root, "Status", "", 38, new Vector2(-120, y), new Vector2(780, 52), TextAlignmentOptions.Center);
            _passBtn = Button(_root, "Pass", "Pas", new Vector2(400, y + 6), new Vector2(200, 76), Pass);
            y += 56f;
            _msg = Label(_root, "Msg", "", 28, new Vector2(-120, y), new Vector2(780, 76), TextAlignmentOptions.Center);
            _msg.color = Gold;
            y += 84f;

            _own = Grid(_root, "OwnBoard", new Vector2(0, y), OwnCell, LiveSide.Own);
            y += OwnCell * Rules.Height + 14f;
            _ownHeader = Label(_root, "OwnHeader", "", 34, new Vector2(0, y), new Vector2(1000, 48), TextAlignmentOptions.Center);
            y += 56f;
            _hand = HandRow(_root, "Hand", new Vector2(0, y), HandCell, SelectSlot, out _handFrame);

            _endPanel = new GameObject("EndPanel", typeof(RectTransform));
            _endPanel.transform.SetParent(_root, false);
            var ep = (RectTransform)_endPanel.transform;
            Stretch(ep);
            var shade = _endPanel.AddComponent<Image>();
            shade.color = new Color(0, 0, 0, 0.72f);
            var card = NewImage(_endPanel.transform, "Card", Slot);
            Place(card.rectTransform, new Vector2(0, 700), new Vector2(880, 520));
            _endText = Label(card.transform, "EndText", "", 44, new Vector2(0, 40), new Vector2(800, 260), TextAlignmentOptions.Center);
            Button(card.transform, "Again", "Aynısı, sıra ters", new Vector2(-210, 340), new Vector2(380, 96), () => NewMatch(PoolIndex, 1 - _first));
            Button(card.transform, "Next", "Sonraki dizilim", new Vector2(210, 340), new Vector2(380, 96), () => NewMatch(PoolIndex + 1, 0));
            _endPanel.SetActive(false);
        }

        public void NewMatch(int poolIndex, int first)
        {
            if (Pool == null || Pool.Count == 0) { _message = "Havuz bulunamadı (Resources/Live/live_pool_v1)."; Refresh(); return; }
            PoolIndex = ((poolIndex % Pool.Count) + Pool.Count) % Pool.Count;
            _first = first;
            Match = new LiveMatch(Pool[PoolIndex], Rules, first);
            ClearSelection();
            _message = "El taşı seç.";
            _moveClock.Restart();
            Refresh();
        }

        private void ToggleTap()
        {
            Rules.TapReadyGroups = !Rules.TapReadyGroups;
            NewMatch(PoolIndex, _first); // ayar maç başında sabitlenir
            _message = "Dokunarak patlatma " + (Rules.TapReadyGroups ? "açık" : "kapalı") + ". Maç yeniden başladı.";
            Refresh();
        }

        // ---- Etkileşim (testler ve yakalama da bunları çağırır) ---------------------------------------------------

        public void SelectSlot(int slot)
        {
            if (Match == null || Match.IsOver) return;
            var hand = Match.Hands[Match.Current];
            _pendTarget = -1;
            _pendTap = false;
            _selSlot = slot == _selSlot || slot < 0 || slot >= hand.Count ? -1 : slot;
            _message = _selSlot < 0 ? "El taşı seç." : ColorName(hand[_selSlot]) + " seçildi: turkuaz çerçeve puan, kırmızı çerçeve saldırı hedefi.";
            Refresh();
        }

        /// <summary>Hücreye dokunma: önizleme, aynı hücreye ikinci dokunuş onay.</summary>
        public void TapCell(LiveSide side, int index)
        {
            if (Match == null || Match.IsOver) return;
            int p = Match.Current;
            var board = Match.TargetBoard(p, side);
            if (_selSlot < 0)
            {
                if (side == LiveSide.Own && Match.Rules.TapReadyGroups && GroupFinder.Component(board, board.X(index), board.Y(index)).Count >= LiveRules.MinGroup)
                {
                    if (_pendTap && _pendTarget == index) { Send(LiveCommand.Tap(p, Match.NextSeq(p), board.X(index), board.Y(index))); return; }
                    _pendTap = true;
                    _pendTarget = index;
                    _pendSide = LiveSide.Own;
                    _message = "Hazır grup: +" + GroupFinder.Component(board, board.X(index), board.Y(index)).Count + " (el taşı harcanmaz). Onay için yeniden dokun.";
                    Refresh();
                    return;
                }
                _message = "Önce el taşı seç.";
                Refresh();
                return;
            }
            var cmd = LiveCommand.Swap(p, Match.NextSeq(p), _selSlot, side, board.X(index), board.Y(index));
            var why = Match.Validate(cmd, out var color, out _);
            if (why != LiveReject.None) { _message = RejectText(why); _pendTarget = -1; Refresh(); return; }
            if (!_pendTap && _pendTarget == index && _pendSide == side) { Send(cmd); return; }
            _pendTap = false;
            _pendTarget = index;
            _pendSide = side;
            if (side == LiveSide.Opponent) _message = "Saldırı: rakibin taşı " + ColorName(color) + " olur, puan yok. Onay için yeniden dokun.";
            else
            {
                var g = LiveMoves.GroupIfSwapped(board, index, color);
                _message = g.Count >= LiveRules.MinGroup ? $"Puan: {g.Count} katman soyulur (+{g.Count}). Onay için yeniden dokun." : "Hazırlık: renk değişir, puan yok. Onay için yeniden dokun.";
            }
            Refresh();
        }

        public void Pass()
        {
            if (Match == null || Match.IsOver) return;
            int p = Match.Current;
            Send(LiveCommand.Pass(p, Match.NextSeq(p)));
        }

        private void Send(LiveCommand cmd)
        {
            var r = Match.Submit(cmd, _moveClock.ElapsedMilliseconds);
            if (!r.Accepted) { _message = RejectText(r.Reject); Refresh(); return; }
            _moveClock.Restart();
            ClearSelection();
            string who = "Oyuncu " + (r.Player + 1);
            _message = r.Kind switch
            {
                LiveMoveKind.Score => $"{who}: +{r.Gained} puan.",
                LiveMoveKind.Tap => $"{who}: hazır gruba dokundu, +{r.Gained}.",
                LiveMoveKind.Attack => $"{who}: saldırdı ({ColorName(r.Color)}).",
                LiveMoveKind.Prep => $"{who}: hazırlık hamlesi.",
                _ => $"{who}: pas.",
            };
            if (Match.IsOver) SaveRecord();
            Refresh();
        }

        private void ClearSelection()
        {
            _selSlot = -1;
            _pendTarget = -1;
            _pendTap = false;
        }

        private void SaveRecord()
        {
            var text = Match.Record.ToText();
            Debug.Log("[KatKat] Canlı maç kaydı\n" + text);
            try { File.AppendAllText(Path.Combine(Application.persistentDataPath, "live_match_records.txt"), text + "\n"); }
            catch (System.Exception e) { Debug.LogWarning("[KatKat] Canlı maç kaydı yazılamadı: " + e.Message); }
        }

        // ---- Gösterim ---------------------------------------------------------------------------------------------

        public void Refresh()
        {
            if (_root == null) return;
            _tapLabel.text = "Dokunma: " + (Rules.TapReadyGroups ? "Açık" : "Kapalı");
            if (Match == null) { _msg.text = _message; return; }
            int p = Match.Current, o = Match.Opponent(p);
            var lay = Match.Layout;
            _title.text = $"Canlı maç · prototip · {lay.Id} ({PoolIndex + 1}/{Pool.Count}) · ilk: Oyuncu {_first + 1}";
            _oppHeader.text = $"Rakip: Oyuncu {o + 1}   puan {Match.Scores[o]}   tur {Match.TurnsUsed[o]}/{Match.Rules.TurnsPerPlayer}";
            _ownHeader.text = $"Oyuncu {p + 1}   puan {Match.Scores[p]}   tur {Match.TurnsUsed[p]}/{Match.Rules.TurnsPerPlayer}";
            _status.text = Match.IsOver ? "Maç bitti" : $"Sıra: Oyuncu {p + 1}";
            _msg.text = _message;
            bool canPass = !Match.IsOver && !Match.HasLegalMove(p);
            _passBtn.interactable = canPass;

            LayerColor? sel = _selSlot >= 0 ? Match.Hands[p][_selSlot] : (LayerColor?)null;
            var ownB = Match.Boards[p];
            var oppB = Match.Boards[o];
            var preview = new HashSet<int>();
            if (_pendTarget >= 0 && _pendSide == LiveSide.Own)
            {
                var g = _pendTap ? GroupFinder.Component(ownB, ownB.X(_pendTarget), ownB.Y(_pendTarget)) : LiveMoves.GroupIfSwapped(ownB, _pendTarget, sel.Value);
                if (g.Count >= LiveRules.MinGroup) foreach (int i in g) preview.Add(i);
            }
            var scoring = new HashSet<int>();
            if (sel.HasValue)
                foreach (var op in LiveMoves.Opportunities(ownB, new[] { sel.Value })) scoring.Add(op.Target);
            var ready = new HashSet<int>();
            if (Match.Rules.TapReadyGroups) foreach (var g in LiveMoves.ReadyGroups(ownB)) foreach (int i in g) ready.Add(i);

            for (int i = 0; i < ownB.Count; i++)
            {
                var c = _own[i];
                ShowCell(c, ownB[i], sel.HasValue ? !LiveMoves.IsTarget(ownB, i) || ownB[i].Top == sel.Value : false);
                Color? frame = null;
                if (ready.Contains(i)) frame = Gold;
                if (scoring.Contains(i)) frame = Teal;
                if (_pendTarget == i && _pendSide == LiveSide.Own) frame = Color.white;
                SetFrame(c, frame);
                c.Overlay.enabled = preview.Contains(i);
                c.Overlay.color = new Color(Gold.r, Gold.g, Gold.b, 0.42f);
                if (_pendTarget == i && _pendSide == LiveSide.Own && !_pendTap && sel.HasValue)
                {
                    c.Stone.sprite = UiStoneArt.Chip(sel.Value); // değişecek renk önizlemesi
                    c.Stone.color = Color.white;
                }
            }
            for (int i = 0; i < oppB.Count; i++)
            {
                var c = _opp[i];
                bool ok = sel.HasValue && LiveMoves.CheckAttack(oppB, i, sel.Value, Match.Rules) == LiveReject.None;
                ShowCell(c, oppB[i], sel.HasValue && !ok);
                SetFrame(c, _pendTarget == i && _pendSide == LiveSide.Opponent ? Color.white : ok ? Attack : (Color?)null);
                c.Overlay.enabled = false;
                if (_pendTarget == i && _pendSide == LiveSide.Opponent && sel.HasValue)
                {
                    c.Stone.sprite = UiStoneArt.Chip(sel.Value); // değişecek hücre önizlemesi
                    c.Overlay.enabled = true;
                    c.Overlay.color = new Color(Attack.r, Attack.g, Attack.b, 0.35f);
                }
            }
            ShowHand(_hand, Match.Hands[p], Match.Rules.HandSize);
            ShowHand(_oppHand, Match.Hands[o], Match.Rules.HandSize);
            for (int i = 0; i < _handFrame.Length; i++) _handFrame[i].color = i == _selSlot ? Gold : new Color(0, 0, 0, 0);

            _endPanel.SetActive(Match.IsOver);
            if (Match.IsOver)
            {
                int w = Match.Winner;
                var tie = Match.TieReason();
                _endText.text = $"Oyuncu 1: {Match.Scores[0]}   Oyuncu 2: {Match.Scores[1]}\n" + (w < 0 ? "Berabere" : $"Kazanan: Oyuncu {w + 1}") +
                                (tie != null ? $"\n<size=28>{tie}</size>" : "") +
                                $"\n<size=28>Dizilim {lay.Id} · ilk oynayan Oyuncu {_first + 1}</size>";
            }
        }

        private static void ShowCell(CellUi c, CellState s, bool dim)
        {
            bool has = s.Active && s.Layers.Count > 0;
            c.Stone.enabled = has;
            if (has) c.Stone.sprite = UiStoneArt.Chip(s.Top);
            c.Stone.color = dim ? new Color(1, 1, 1, 0.35f) : Color.white;
            c.Count.text = !has ? "" : s.Layers.Count.ToString();
            c.Frame.transform.parent.GetComponent<Image>().color = s.Active ? Slot : new Color(0, 0, 0, 0);
        }

        private static void SetFrame(CellUi c, Color? col)
        {
            c.Frame.enabled = col.HasValue;
            if (col.HasValue) c.Frame.color = col.Value;
        }

        private static void ShowHand(Image[] row, List<LayerColor> hand, int size)
        {
            for (int i = 0; i < row.Length; i++)
            {
                bool has = i < hand.Count;
                row[i].enabled = has;
                if (has) row[i].sprite = UiStoneArt.Chip(hand[i]);
            }
        }

        public static string ColorName(LayerColor c) => c switch
        {
            LayerColor.Coral => "Mercan",
            LayerColor.Blue => "Turkuaz",
            LayerColor.Yellow => "İnci",
            _ => "Mor",
        };

        public static string RejectText(LiveReject r) => r switch
        {
            LiveReject.MatchOver => "Maç bitti.",
            LiveReject.NotYourTurn => "Sıra sende değil.",
            LiveReject.Duplicate => "Bu komut zaten işlendi.",
            LiveReject.BadHandSlot => "El taşı yok.",
            LiveReject.OutOfBounds => "Tahta dışı.",
            LiveReject.EmptyCell => "Boş hücre değiştirilemez.",
            LiveReject.SameColor => "Taş zaten bu renk.",
            LiveReject.AttackMakesGroup => "Rakipte 3'lü kuran saldırı geçersiz.",
            LiveReject.TapDisabled => "Dokunarak patlatma kapalı.",
            LiveReject.NoReadyGroup => "Burada hazır grup yok.",
            LiveReject.PassWithLegalMove => "Yasal hamlen varken pas geçemezsin.",
            _ => "",
        };

        // ---- Yapı yardımcıları ------------------------------------------------------------------------------------

        private CellUi[] Grid(Transform parent, string name, Vector2 topCenter, float cell, LiveSide side)
        {
            int w = Rules.Width, h = Rules.Height;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, topCenter, new Vector2(cell * w, cell * h));
            var cells = new CellUi[w * h];
            for (int i = 0; i < cells.Length; i++)
            {
                int x = i % w, y = i / w, idx = i;
                var slot = NewImage(go.transform, "Cell" + i, Slot);
                var rt = slot.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(x * cell + 3, -y * cell - 3);
                rt.sizeDelta = new Vector2(cell - 6, cell - 6);
                var btn = slot.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => TapCell(side, idx));
                var frame = NewImage(slot.transform, "Frame", Teal);
                Stretch(frame.rectTransform);
                var inner = NewImage(frame.transform, "Hole", Slot);
                Stretch(inner.rectTransform, 6f);
                frame.raycastTarget = inner.raycastTarget = false;
                var stone = NewImage(slot.transform, "Stone", Color.white);
                Stretch(stone.rectTransform, cell * 0.08f);
                stone.preserveAspect = true;
                stone.raycastTarget = false;
                var ov = NewImage(slot.transform, "Overlay", Gold);
                Stretch(ov.rectTransform);
                ov.raycastTarget = false;
                ov.enabled = false;
                var count = Label(slot.transform, "Count", "", cell * 0.26f, Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomRight);
                Stretch(count.rectTransform, 2f);
                count.raycastTarget = false;
                count.outlineWidth = 0.3f;
                count.outlineColor = new Color32(0x0B, 0x16, 0x1B, 0xFF);
                cells[i] = new CellUi { Stone = stone, Overlay = ov, Frame = frame, Count = count };
            }
            return cells;
        }

        private Image[] HandRow(Transform parent, string name, Vector2 topCenter, float cell, System.Action<int> onTap, out Image[] frames)
        {
            int n = Rules.HandSize;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, topCenter, new Vector2(cell * n, cell));
            var row = new Image[n];
            frames = new Image[n];
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var slot = NewImage(go.transform, "Slot" + i, Slot);
                var rt = slot.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(i * cell + 3, -3);
                rt.sizeDelta = new Vector2(cell - 6, cell - 6);
                var fr = NewImage(slot.transform, "Sel", new Color(0, 0, 0, 0));
                Stretch(fr.rectTransform, -4f);
                fr.raycastTarget = false;
                frames[i] = fr;
                var hole = NewImage(slot.transform, "Hole", Slot);
                Stretch(hole.rectTransform, 2f);
                hole.raycastTarget = false;
                var stone = NewImage(slot.transform, "Stone", Color.white);
                Stretch(stone.rectTransform, cell * 0.08f);
                stone.preserveAspect = true;
                stone.raycastTarget = false;
                row[i] = stone;
                if (onTap != null)
                {
                    var b = slot.gameObject.AddComponent<Button>();
                    b.transition = Selectable.Transition.None;
                    b.onClick.AddListener(() => onTap(idx));
                }
            }
            return row;
        }

        private static Image NewImage(Transform parent, string name, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            return img;
        }

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Üst ortaya göre konum: x merkezden, y üstten aşağı.</summary>
        private static void Place(RectTransform rt, Vector2 topCenter, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(topCenter.x, -topCenter.y);
            rt.sizeDelta = size;
        }

        private static TMP_Text Label(Transform parent, string name, string text, float size, Vector2 topCenter, Vector2 box, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = UiTypography.Font;
            t.fontSize = size;
            t.color = TextCol;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            Place(t.rectTransform, topCenter, box);
            return t;
        }

        private static Button Button(Transform parent, string name, string text, Vector2 topCenter, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var img = NewImage(parent, name, new Color32(0x2E, 0x6F, 0x76, 0xFF));
            Place(img.rectTransform, topCenter, size);
            var b = img.gameObject.AddComponent<Button>();
            b.onClick.AddListener(onClick);
            var t = Label(img.transform, "Text", text, 30, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            Stretch(t.rectTransform, 6f);
            return b;
        }
    }
}
