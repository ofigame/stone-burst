using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.Levels;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Öğretici turlar (kullanıcı: "tur şeklinde olsun, ilk bölümlerde oyuncuyu direkt yönlendirerek oynatsın; bölümler
    /// ilerledikçe yeni bir özellik gelmişse ona da öğretici koyalım; odak yer açık, diğer taraflar karartılmış").
    ///   • Bölüm 1–3: kayıtlı çözüm adım adım gösterilir; oyuncu yalnızca gösterilen gruba dokunabilir.
    ///   • Bölüm 1: ilk hamleden sonra hedef kartı ve hamle sayacı tanıtılır.
    ///   • Bölüm 6/7/9/12: özel taş tepsisi (roket doğar → tepsi, kullanım iki adımda, Altın Rulo, biriktirme).
    ///   • Her mekaniğin ilk çıktığı bölüm: o mekanikli ilk hücre açık, kısa açıklama.
    /// Bilgi adımı ekrana dokununca geçer. Tur bölüm kazanılınca bir daha gösterilmez (PlayerPrefs).
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>Toplu kipte (testler, yakalamalar) tur kapalı; yakalama aracı açabilir.</summary>
        public static bool? ToursOverride;

        /// <summary>Yakalama aracı: "gösterildi" kaydını yok say.</summary>
        public static bool TourIgnoreSaved;

        private sealed class TourInfo
        {
            public string Key;
            public Func<Rect?> Hole;
        }

        private TutorialOverlay _tourOverlay;
        private readonly List<TourInfo> _tourQueue = new List<TourInfo>();
        private readonly Dictionary<int, string> _guideText = new Dictionary<int, string>();
        private int _tourCursor, _guideUntil;
        private bool _tourOn, _tourInfoShowing;
        private float _tourInfoSince;
        private int _tourSpecialSeen;

        private const string TourPrefPrefix = "katkat.tour.";

        public bool TourActive => _tourOn;
        public bool TourInfoShowing => _tourOn && _tourInfoShowing;

        /// <summary>Yakalama aracı: bilgi adımına dokunmuş gibi geç.</summary>
        public void TourTapForCapture()
        {
            if (!_tourOn || !_tourInfoShowing || _tourQueue.Count == 0) return;
            _tourQueue.RemoveAt(0);
            _tourInfoShowing = false;
            TourShowCurrent();
        }

        public void ShowSettingsForCapture() => ShowSettings();
        public void ShowLostForCapture() { SetPhase(Phase.Ended); ShowLost(GameStatus.LostOutOfMoves); }
        public void ShowLostForCapture(GameStatus status) { SetPhase(Phase.Ended); ShowLost(status); }
        private bool Guiding => _tourOn && _level != null && _tourCursor < _guideUntil && _tourCursor < _level.Solution.Count;

        private TutorialOverlay TourOverlay
        {
            get
            {
                if (_tourOverlay == null) _tourOverlay = TutorialOverlay.Create((RectTransform)_hud.transform);
                return _tourOverlay;
            }
        }

        private void TourBegin(int index)
        {
            _tourQueue.Clear();
            _guideText.Clear();
            _tourCursor = 0;
            _guideUntil = 0;
            _tourSpecialSeen = 0;
            _tourInfoShowing = false;
            bool enabled = ToursOverride ?? !Application.isBatchMode;
            _tourOn = enabled && _level != null && (TourIgnoreSaved || PlayerPrefs.GetInt(TourPrefPrefix + _level.Id, 0) == 0) && Configure(index);
            if (!_tourOn) { HideTour(); return; }
            _hintActive = false;
            RefreshHint();
        }

        /// <summary>Bölüme göre tur içeriği; tur yoksa false.</summary>
        private bool Configure(int index)
        {
            int all = int.MaxValue;
            switch (index)
            {
                case 0: _guideUntil = all; _guideText[0] = "tut.l1.tap"; return true;
                case 1: _guideUntil = all; _guideText[0] = "tut.l2.domino"; return true;
                case 2: _guideUntil = all; _guideText[0] = "tut.l3.plan"; return true;
                case 5: _guideUntil = all; _guideText[0] = "tut.l6.big"; return true;
                case 6: _guideUntil = all; return true;
                case 8: _guideUntil = all; _guideText[0] = "tut.l9.big"; return true;
                case 11: _tourQueue.Add(new TourInfo { Key = "tut.save", Hole = () => _hud.TrayRect() }); return true;
            }
            // Yeni mekanik: katalogdaki ilk bölümü bu mu?
            string cap = NewCapability(index);
            if (cap == null) return false;
            var at = FirstMechanicCell(cap);
            if (cap == "paint") _tourQueue.Add(new TourInfo { Key = "tut.mech.paint", Hole = () => _hud.PaintRect() });
            else _tourQueue.Add(new TourInfo { Key = "tut.mech." + cap, Hole = at.HasValue ? () => CellsRect(new[] { at.Value }) : (Func<Rect?>)null });
            return true;
        }

        private static readonly string[] Mechanics = { "clip", "star", "ice", "colorLock", "envelope", "rotor", "sponge", "linkedClip", "gate", "paint" };

        private string NewCapability(int index)
        {
            if (_catalog == null || index < 0 || index >= _catalog.Count || _catalog.Levels[index] != _level) return null;
            foreach (var cap in Mechanics)
            {
                if (!_level.Capabilities.Contains(cap)) continue;
                bool earlier = false;
                for (int i = 0; i < index && !earlier; i++) earlier = _catalog.Levels[i].Capabilities.Contains(cap);
                if (!earlier) return cap;
            }
            return null;
        }

        private Coord? FirstMechanicCell(string cap)
        {
            var b = _session.State.Board;
            for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
            {
                var c = b[x, y];
                bool hit = cap switch
                {
                    "clip" => c.ClipHitsRemaining > 0 && c.LinkHits == 0,
                    "linkedClip" => c.LinkHits > 0,
                    "star" => c.HasBottomStar,
                    "ice" => c.IceHitsRemaining > 0,
                    "colorLock" => c.LockRemaining > 0,
                    "envelope" => c.HasEnvelope,
                    "rotor" => c.HasRotor,
                    "sponge" => c.SpongeNeed != 0,
                    "gate" => c.GateId > 0 || c.ButtonGate > 0,
                    _ => false,
                };
                if (hit) return new Coord(x, y);
            }
            return null;
        }

        // ---- Gösterim -----------------------------------------------------------------------------------

        /// <summary>Kararlı duruma her dönüşte (Idle) çağrılır: sıradaki bilgi ya da yönlendirme adımı.</summary>
        private void TourShowCurrent()
        {
            if (!_tourOn || _session == null || CurrentPhase != Phase.Idle || _hud.PanelOpen) { if (_tourOverlay != null && !_tourOn) HideTour(); return; }
            if (_tourQueue.Count > 0)
            {
                var info = _tourQueue[0];
                _tourInfoShowing = true;
                _tourInfoSince = Time.unscaledTime;
                TourOverlay.Show(info.Hole?.Invoke(), Loc.T(info.Key), false, Loc.T("tut.continue"));
                TourOverlay.Root.GetComponent<CanvasGroup>().blocksRaycasts = true;
                EnsureBlocker();
                return;
            }
            _tourInfoShowing = false;
            if (_tourOverlay != null) _tourOverlay.Root.GetComponent<CanvasGroup>().blocksRaycasts = false;
            if (!Guiding)
            {
                HideTour();
                if (_tourQueue.Count == 0 && _tourCursor >= Math.Min(_guideUntil, _level.Solution.Count)) _tourOn = _tourSpecialPending;
                return;
            }
            var step = _level.Solution[_tourCursor];
            string text = _guideText.TryGetValue(_tourCursor, out var k) ? Loc.T(k) : Loc.T("tut.next");
            switch (step.Type)
            {
                case "tap":
                {
                    var group = GroupFinder.Component(_session.State.Board, step.X, step.Y);
                    var cells = new List<Coord>();
                    foreach (var i in group) cells.Add(new Coord(_session.State.Board.X(i), _session.State.Board.Y(i)));
                    if (cells.Count == 0) cells.Add(new Coord(step.X, step.Y));
                    // Kutu içinde kalan grup dışı hücreler ayrıca karartılır (L/T şekilli gruplar).
                    int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
                    foreach (var c in cells) { x0 = Math.Min(x0, c.X); y0 = Math.Min(y0, c.Y); x1 = Math.Max(x1, c.X); y1 = Math.Max(y1, c.Y); }
                    var dims = new List<Rect>();
                    for (int yy = y0; yy <= y1; yy++)
                    for (int xx = x0; xx <= x1; xx++)
                    {
                        if (cells.Contains(new Coord(xx, yy))) continue;
                        var r = CellsRect(new[] { new Coord(xx, yy) });
                        if (r.HasValue) dims.Add(r.Value);
                    }
                    TourOverlay.Show(CellsRect(cells), text, true, null, dims);
                    break;
                }
                case "use":
                    if (_traySlot != step.Row)
                        TourOverlay.Show(_hud.TraySlotRect(step.Row), Loc.T("tut.use.slot"), true, null);
                    else
                        TourOverlay.Show(CellsRect(new[] { new Coord(step.X, step.Y) }), Loc.T("tut.use.cell"), true, null);
                    break;
                case "rotate":
                    TourOverlay.Show(CellsRect(new[] { new Coord(step.X, step.Y) }), Loc.T("tut.mech.rotor"), true, null);
                    break;
                default:
                    // Rulo / boya adımları yönlendirilmez: tur burada serbest oyuna bırakır.
                    _guideUntil = _tourCursor;
                    HideTour();
                    break;
            }
        }

        // Bilgi adımında arayüz düğmeleri (duraklat, geri al, tepsi) çalışmasın: görünmez tam ekran engel.
        private void EnsureBlocker()
        {
            var root = TourOverlay.Root;
            if (root.Find("Blocker") != null) return;
            var img = UiFactory.Image("Blocker", root, null, Color.clear);
            UiFactory.Stretch(img.rectTransform);
            img.raycastTarget = true;
            img.transform.SetAsFirstSibling();
        }

        private void HideTour()
        {
            if (_tourOverlay != null) _tourOverlay.Hide();
            _tourInfoShowing = false;
        }

        // ---- Girdi kapısı -------------------------------------------------------------------------------

        /// <summary>Bilgi adımı açıkken herhangi bir dokunuş adımı geçer (UI üstü dahil). true: dokunuş tüketildi.</summary>
        private bool TourConsumesPress()
        {
            if (!_tourOn || !_tourInfoShowing) return false;
            bool pressed = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (!pressed) return true; // bilgi açıkken başka girdi yok
            if (Time.unscaledTime - _tourInfoSince < 0.35f) return true; // yanlışlıkla hemen geçmesin
            _tourQueue.RemoveAt(0);
            _tourInfoShowing = false;
            _sound.Play(Sfx.Tap);
            TourShowCurrent();
            return true;
        }

        private bool TourAllowsCell(int x, int y)
        {
            if (!_tourOn) return true;
            if (_tourInfoShowing) return false;
            if (!Guiding) return true;
            var step = _level.Solution[_tourCursor];
            bool ok;
            if (step.Type == "tap")
            {
                var b = _session.State.Board;
                ok = b.InBounds(x, y) && GroupFinder.Component(b, step.X, step.Y).Contains(b.Index(x, y));
            }
            else if (step.Type == "use") ok = _traySlot == step.Row && x == step.X && y == step.Y;
            else ok = false;
            if (!ok) NudgeTour();
            return ok;
        }

        private bool TourAllowsSlot(int slot)
        {
            if (!_tourOn) return true;
            if (_tourInfoShowing) return false;
            if (!Guiding) return true;
            var step = _level.Solution[_tourCursor];
            bool ok = step.Type == "use" && slot == step.Row && _traySlot != slot;
            if (!ok) NudgeTour();
            return ok;
        }

        private bool TourAllowsRotor(int x, int y)
        {
            if (!_tourOn) return true;
            if (_tourInfoShowing) return false;
            if (!Guiding) return true;
            var step = _level.Solution[_tourCursor];
            bool ok = step.Type == "rotate" && step.X == x && step.Y == y;
            if (!ok) NudgeTour();
            return ok;
        }

        private bool TourBlocksUndo => _tourOn && (Guiding || _tourInfoShowing);

        private void NudgeTour()
        {
            _sound.Play(Sfx.Invalid);
            Haptics.Light();
            if (_tourOverlay != null && _tourOverlay.Visible) TourShowCurrent(); // halka/ok yeniden canlanır
        }

        /// <summary>Komut kabul edildi (oynatma başlamadan): yönlendirme bir adım ilerler, katman gizlenir.</summary>
        private void TourOnAccepted(MoveResult r)
        {
            if (!_tourOn) return;
            if (Guiding) _tourCursor++;
            HideTour();
            if (_levelIndex == 0 && _tourCursor == 1 && _guideUntil > 1)
            {
                _tourQueue.Add(new TourInfo { Key = "tut.goals", Hole = () => _hud.GoalsRect() });
                _tourQueue.Add(new TourInfo { Key = "tut.moves", Hole = () => _hud.MovesRect() });
            }
            if (r != null && r.SpecialCreated != SpecialKind.None && _tourSpecialSeen == 0 && (_levelIndex == 5 || _levelIndex == 8))
            {
                _tourSpecialSeen++;
                string key = r.SpecialCreated == SpecialKind.Rainbow ? "tut.rainbow" : "tut.tray";
                _tourQueue.Add(new TourInfo { Key = key, Hole = () => _hud.TraySlotRect(Mathf.Max(0, _session.State.Tray.Count - 1)) });
            }
        }

        private bool _tourSpecialPending => _tourQueue.Count > 0;

        private void TourOnRestart()
        {
            if (_level == null) return;
            TourBegin(_levelIndex);
        }

        private void TourOnWon()
        {
            if (_level != null) PlayerPrefs.SetInt(TourPrefPrefix + _level.Id, 1);
            PlayerPrefs.Save();
            _tourOn = false;
            HideTour();
        }

        // ---- Koordinatlar --------------------------------------------------------------------------------

        /// <summary>Hücrelerin kapsayan dikdörtgeni (HUD tuvali yerel koordinatı).</summary>
        private Rect? CellsRect(IEnumerable<Coord> cells)
        {
            Canvas.ForceUpdateCanvases(); // tuval kök ölçeği güncel olsun (ilk karede eski kalabiliyor)
            float half = _board.Pitch * 0.5f;
            bool any = false;
            Vector2 min = default, max = default;
            var canvas = (RectTransform)_hud.transform;
            foreach (var c in cells)
            {
                var center = _board.transform.TransformPoint(_board.CellCenter(c.X, c.Y));
                foreach (var corner in new[] { new Vector3(-half, -half), new Vector3(half, half) })
                {
                    var w = center + _board.transform.TransformVector(corner);
                    // Dünya → ekran pikseli → tuval birimi (kök merkezli; ölçek = tuval scaleFactor). Kök dönüşümüne
                    // dayanmaz (editör yakalamasında ve ilk karede güncel olmayabiliyor).
                    var sp = _cam.WorldToScreenPoint(w);
                    float sf = Mathf.Max(0.0001f, _hud.Canvas.scaleFactor);
                    Vector2 p = new Vector2((sp.x - _cam.pixelWidth * 0.5f) / sf, (sp.y - _cam.pixelHeight * 0.5f) / sf);
                    if (!any) { min = max = p; any = true; }
                    else { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
                }
            }
            if (!any) return null;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
