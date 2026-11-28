using System.Collections.Generic;
using KatKat.Core;
using KatKat.UI;
using KatKat.View;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// 2026-11-26 (kullanıcı: "Roket ve rulo yine tahtanın üst kısmında olsun böyle sorunlar var … Belli olmuyorlar ve rulo sürüklenme
    /// efekti yok. Roketlere tıklandığında anlaşılmıyor"): tepsiden sürükle-bırak ve hedef önizlemesi.
    /// <list type="bullet">
    /// <item>Dolu yuvaya basıp sürükle: parmağı ışıklı hayalet izler; geçerli hücrenin üstünde tam olarak vurulacak yer önizlenir
    /// (roket şerit(ler)i, Altın Rulo o renkteki bütün açık yığınlar, Kürek çakıllar ziyaret sırasıyla). Bırakınca
    /// <see cref="Command.UseSpecial"/> (bir hamle); tahta dışında ya da geçersiz hedefte hayalet yuvasına döner.</item>
    /// <item>Yuvaya dokunup seçmek de çalışır: tahtaya basılı tutulan parmağın (masaüstünde imlecin) altında aynı önizleme; bırakınca
    /// kullanılır. Testlerin ve yakalamanın <see cref="OnCellPressed"/> yolu hemen kullanır.</item>
    /// </list>
    /// </summary>
    public sealed partial class GameController
    {
        private bool _tdOn, _aimOn;
        private int _aimX = -1, _aimY = -1;
        private bool _aimValid, _aimInBoard;

        /// <summary>Tepsiden sürükleme sürüyor mu (testler).</summary>
        public bool TrayDragging => _tdOn;
        /// <summary>Gösterilen önizlemenin hücresi geçerli bir hedef mi (testler, yakalama).</summary>
        public bool TrayAimValid => _aimValid && _aimInBoard;

        private void TrayDragWire()
        {
            _hud.TrayDragBegan += TrayDragBegin;
            _hud.TrayDragMoved += TrayDragMove;
            _hud.TrayDragEnded += TrayDragEnd;
            // Doğan özel taş yuvasına vardığında yuva dolu görünür ve patlar.
            _board.TrayTokenArrived = kind =>
            {
                if (_session == null) return;
                var tray = _session.State.Tray;
                _hud.SetTray(tray, _traySlot);
                int slot = tray.LastIndexOf(kind);
                _hud.PopTraySlot(slot >= 0 ? slot : tray.Count - 1);
                Haptics.Light();
            };
        }

        /// <summary>Hücre merkezinin ekran noktası (testler, yakalama: sürükleme benzetimi).</summary>
        public Vector2 CellScreenPoint(int x, int y) => _cam.WorldToScreenPoint(_board.transform.TransformPoint(_board.CellCenter(x, y)));

        /// <summary>Tepsi yuvasının ekran noktası (testler, yakalama).</summary>
        public Vector2 TraySlotScreenPoint(int slot) => RectTransformUtility.WorldToScreenPoint(_cam, _hud.TraySlotPosition(slot));

        /// <summary>Yuvadan sürükleme başladı (HUD; testler doğrudan çağırabilir).</summary>
        public void TrayDragBegin(int slot, Vector2 screen)
        {
            if (_tdOn || CurrentPhase != Phase.Idle || _session == null) return;
            var tray = _session.State.Tray;
            if (slot < 0 || slot >= tray.Count) return;
            if (_traySlot != slot && !TourAllowsSlot(slot)) return;
            if (_paintMode) { _paintMode = false; _hud.HidePalette(); _hud.SetPaint(_session.State.PaintLeft, false); }
            _aimOn = false;
            _traySlot = slot;
            _hud.SetTray(tray, _traySlot);
            RefreshHint();
            _tdOn = true;
            _aimX = _aimY = -1;
            _hud.ShowTrayGhost(tray[slot], slot, screen);
            _sound.Play(Sfx.Tap);
            Haptics.Light();
            _hud.HideToast(); // sürüklemede yönerge yok: önizleme anlatır, bildirim tahtanın üst sırasını kapatmasın
            TrayDragMove(screen);
        }

        /// <summary>Sürüklenen hayalet parmağı izler; hücre değişince önizleme yenilenir.</summary>
        public void TrayDragMove(Vector2 screen)
        {
            if (!_tdOn) return;
            AimAtScreen(screen);
            _hud.MoveTrayGhost(screen, _aimInBoard && _aimValid);
        }

        /// <summary>Bırakma: geçerli hedefte kullanılır, değilse hayalet yuvasına döner.</summary>
        public void TrayDragEnd(Vector2 screen)
        {
            if (!_tdOn) return;
            TrayDragMove(screen);
            _tdOn = false;
            bool fire = _aimInBoard && _aimValid && _traySlot >= 0 && CurrentPhase == Phase.Idle && TourAllowsCell(_aimX, _aimY);
            if (fire) { UseTraySlot(_aimX, _aimY); return; }
            if (_aimInBoard && !_aimValid)
            {
                // Tahtada ama etkisiz hücre: kısa geri bildirim (hamle harcanmaz).
                _sound.Play(Sfx.Invalid);
                Haptics.Light();
                _board.ShakeCells(new[] { new Coord(_aimX, _aimY) });
                _hud.ShowToast(Loc.T("toast.use.noeffect"));
            }
            else _hud.HideToast();
            _board.HideAim();
            _hud.SnapTrayGhost();
            _traySlot = -1;
            _hud.SetTray(_session.State.Tray, -1);
            RefreshHint();
            _aimX = _aimY = -1;
        }

        /// <summary>2026-11-28: sürükleme sürerken bölüm değişirse (tepsi kapanınca OnEndDrag gelmez) tahta girdisi kilitli kalmasın.</summary>
        private void TrayDragReset()
        {
            if (!_tdOn) return;
            _tdOn = false;
            _aimInBoard = _aimValid = false;
            _aimX = _aimY = -1;
            _board.HideAim();
            _hud.SnapTrayGhost();
        }

        private void AimAtScreen(Vector2 screen)
        {
            var world = _cam.ScreenToWorldPoint(screen);
            if (_board.TryCellAt(world, out int x, out int y) && _session.State.Board[x, y].Active) UpdateAim(x, y);
            else if (_aimInBoard || _board.AimShown != BoardView.AimStyle.None)
            {
                _aimInBoard = false;
                _aimValid = false;
                _aimX = _aimY = -1;
                _board.HideAim();
            }
        }

        /// <summary>
        /// Önizleme: komut doğrulanır, durumun kopyasında uygulanır; özel taş dalgasının hücreleri (Altın Rulo), çakılları (Kürek, sırayla)
        /// ya da şeritleri (roket, çift roket) gösterilir. Aynı hücrede yeniden hesaplanmaz.
        /// </summary>
        private void UpdateAim(int x, int y)
        {
            if (_aimInBoard && x == _aimX && y == _aimY) return;
            _aimX = x;
            _aimY = y;
            _aimInBoard = true;
            var st = _session.State;
            if (_traySlot < 0 || _traySlot >= st.Tray.Count) { _aimValid = false; _board.HideAim(); return; }
            var kind = st.Tray[_traySlot];
            var cmd = Command.UseSpecial(_traySlot, x, y);
            if (RulesEngine.Validate(st, cmd, out _) != RejectReason.None)
            {
                _aimValid = false;
                _board.ShowAim(BoardView.AimStyle.Invalid, new Coord(x, y), null, Color.red);
                return;
            }
            _aimValid = true;
            var tone = BoardView.SpecialTone(kind);
            var cells = new List<Coord>();
            if (kind.IsRocket())
            {
                bool row = kind.IsRowRocket();
                var b = st.Board;
                var lanes = new List<int> { row ? y : x };
                if (kind.IsDoubleRocket()) lanes.Add(row ? (y + 1 < b.Height ? y + 1 : y - 1) : (x + 1 < b.Width ? x + 1 : x - 1));
                foreach (int li in lanes)
                    for (int k = 0; k < (row ? b.Width : b.Height); k++)
                    {
                        int cx = row ? k : li, cy = row ? li : k;
                        if (b.InBounds(cx, cy) && b[cx, cy].Active) cells.Add(new Coord(cx, cy));
                    }
                _board.ShowAim(BoardView.AimStyle.Lane, new Coord(x, y), cells, tone);
                return;
            }
            var sim = st.Clone();
            var r = RulesEngine.TryApply(sim, cmd);
            PeelWave w = null;
            if (r != null && r.Accepted) foreach (var wv in r.Waves) if (wv.Kind == WaveKind.Special) { w = wv; break; }
            if (kind == SpecialKind.Shovel)
            {
                if (w != null) cells.AddRange(w.GravelCleared);
                if (cells.Count == 0) cells.Add(new Coord(x, y));
                _board.ShowAim(BoardView.AimStyle.Gravel, new Coord(x, y), cells, tone);
                return;
            }
            // Altın Rulo: hedefin üst rengindeki bütün açık yığınlar (dalganın hücreleri), o renkte.
            if (w != null) foreach (var c in w.Cells) cells.Add(new Coord(c.X, c.Y));
            var cell = st.Board[x, y];
            Color col = cell.IsSelectable ? (Color)LayerPalette.FrontColor(cell.Top) : tone;
            if (!cells.Exists(c => c.X == x && c.Y == y)) cells.Insert(0, new Coord(x, y));
            _board.ShowAim(BoardView.AimStyle.Color, new Coord(x, y), cells, col);
        }

        /// <summary>
        /// Seçili yuvayla tahtaya basış (Update): parmak basılı kaldıkça önizleme izler, bırakınca kullanılır. Masaüstünde imleç
        /// basılmadan da önizler. Bu karede girdi tüketildiyse true.
        /// </summary>
        private bool TrayAimTick()
        {
            if (_tdOn) return true; // HUD sürüklemesi sürüyor: tahta girdisi yok
            if (_session == null) return false;
            if (_aimOn)
            {
                Vector2 pos;
                bool held;
                if (Input.touchCount > 0)
                {
                    var t = Input.GetTouch(0);
                    pos = t.position;
                    held = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                }
                else
                {
                    pos = Input.mousePosition;
                    held = Input.GetMouseButton(0);
                }
                if (_traySlot < 0 || CurrentPhase != Phase.Idle) { _aimOn = false; _board.HideAim(); return false; }
                AimAtScreen(pos);
                if (held) return true;
                _aimOn = false;
                if (_aimInBoard) OnCellPressed(_aimX, _aimY); // geçerliyse kullanır, değilse sallanır ve uyarır
                _board.HideAim();
                _aimX = _aimY = -1;
                _aimInBoard = false;
                return true;
            }
            // Seçim kalktıysa önizleme de kalkar; masaüstünde imlecin altında önizleme.
            if (_traySlot < 0 || CurrentPhase != Phase.Idle)
            {
                if (_board.AimShown != BoardView.AimStyle.None) { _board.HideAim(); _aimInBoard = false; _aimX = _aimY = -1; }
                return false;
            }
            if (Input.touchCount == 0 && !Input.GetMouseButton(0) && Input.mousePresent) AimAtScreen(Input.mousePosition);
            return false;
        }

        /// <summary>Seçili yuvayla tahtaya basıldı: önizleme parmağı izler (Update).</summary>
        private bool BeginTrayAim(int x, int y)
        {
            if (_traySlot < 0 || CurrentPhase != Phase.Idle || _paintMode || _session == null) return false;
            _aimOn = true;
            _aimInBoard = false;
            UpdateAim(x, y);
            Haptics.Tick();
            return true;
        }

        /// <summary>Seçim / sürükleme bildirimi: tür başına kısa yönerge (çift roket ayrı).</summary>
        private static string TrayToastKey(SpecialKind kind) => kind == SpecialKind.Shovel ? "toast.use.shovel"
            : kind == SpecialKind.Rainbow ? "toast.use.rainbow"
            : kind == SpecialKind.RocketRow2 ? "toast.use.row2"
            : kind == SpecialKind.RocketColumn2 ? "toast.use.col2"
            : kind.IsRowRocket() ? "toast.use.row" : "toast.use.col";
    }
}
