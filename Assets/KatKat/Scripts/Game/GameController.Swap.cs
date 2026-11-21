using System.Collections.Generic;
using KatKat.Core;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// 2026-11-21 sürükle-değiştir (kullanıcı: "taşı sürükleyerek 3'lüyü tamamlama olsun … kendi katındaki taşla yer
    /// değiştirir") ve boşta ipucu (kullanıcı: "oyuncu uzun süre işlem yapmaz ise … patlatılabilinecek taşları dikkat çekici
    /// yapalım yanıp sönme gibi olsun").
    /// <para>Girdi: grubu 3'ü bulan taşa dokunuş eskisi gibi anında patlar. Grubu yetersiz taşa basılıyken parmak komşu
    /// hücreye doğru kayarsa (adımın %40'ı) yer değiştirme denenir; kaymadan bırakılırsa eski "en az 3" uyarısı çıkar.</para>
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>Boşta ipucunun belirmesi için hareketsiz süre (sn).</summary>
        public const float IdleHintDelay = 5f;

        private bool _swapPending;
        private int _swapX, _swapY;
        private List<Coord> _swapGroup;
        private float _idleSec;
        private bool _idleShown;

        /// <summary>Testler: sürükleme bekleniyor mu.</summary>
        public bool SwapPending => _swapPending;
        /// <summary>Testler: boşta ipucu görünüyor mu.</summary>
        public bool IdleHintShown => _idleShown;

        private bool SwapCandidate(int x, int y)
        {
            if (_tourOn || _boosterMode != null || _paintMode || _traySlot >= 0 || _session == null) return false;
            var b = _session.State.Board;
            return b.InBounds(x, y) && b[x, y].IsSelectable && !b[x, y].Protected;
        }

        private static bool PointerHeld(out Vector2 pos)
        {
            pos = default;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) return false;
                pos = t.position;
                return true;
            }
            if (!Input.GetMouseButton(0)) return false;
            pos = Input.mousePosition;
            return true;
        }

        /// <summary>Grubu yetersiz taşa basıldı: parmak basılıysa sürüklemeyi bekle (true), değilse uyarı gösterilsin (false).</summary>
        private bool BeginSwapDrag(int x, int y, List<Coord> group)
        {
            if (!SwapCandidate(x, y) || !PointerHeld(out _)) return false;
            _swapPending = true;
            _swapX = x; _swapY = y;
            _swapGroup = group;
            return true;
        }

        private void TooSmallFeedback(IEnumerable<Coord> group)
        {
            _sound.Play(Sfx.Invalid);
            Haptics.Light();
            _board.ShakeCells(group);
            _hud.ShowToast(Loc.T("toast.min3"));
        }

        /// <summary>Her kare: bekleyen sürüklemeyi izler. true: girdi bu karede tüketildi.</summary>
        private bool SwapDragTick()
        {
            if (!_swapPending) return false;
            if (CurrentPhase != Phase.Idle || _hud.PanelOpen || _menu.IsOpen) { _swapPending = false; return false; }
            if (PointerHeld(out var pos))
            {
                Vector3 world = _cam.ScreenToWorldPoint(pos);
                var center = _board.transform.TransformPoint(_board.CellCenter(_swapX, _swapY));
                var d = world - center;
                float th = _board.Pitch * _board.transform.lossyScale.x * 0.4f;
                if (Mathf.Abs(d.x) < th && Mathf.Abs(d.y) < th) return true;
                int dir = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? (d.x > 0f ? 0 : 2) : (d.y > 0f ? 3 : 1);
                _swapPending = false;
                OnSwap(_swapX, _swapY, dir);
                return true;
            }
            _swapPending = false;
            TooSmallFeedback(_swapGroup ?? new List<Coord> { new Coord(_swapX, _swapY) });
            return true;
        }

        /// <summary>Sürükle-değiştir komutu (0 sağ, 1 aşağı, 2 sol, 3 yukarı). Testler ve yakalama aracı doğrudan çağırabilir.</summary>
        public void OnSwap(int x, int y, int dir)
        {
            if (CurrentPhase != Phase.Idle || _session == null) return;
            var r = _session.Submit(Command.Swap(x, y, dir));
            if (!r.Accepted)
            {
                _sound.Play(Sfx.Invalid);
                Haptics.Light();
                _board.ShakeCells(new[] { new Coord(x, y) });
                if (r.Reason == RejectReason.NoMovesLeft) { _hud.ShowToast(Loc.T("toast.nomoves.tray")); _hud.PulseTray(); }
                else if (r.Reason == RejectReason.SwapNoMatch || r.Reason == RejectReason.SameColor) _hud.ShowToast(Loc.T("toast.swap.nomatch"));
                return;
            }
            ResetIdleHint();
            _hintActive = false;
            HintAfterCommand();
            _hint.Hide();
            _sound.Play(Sfx.Tap);
            Haptics.Light();
            HeartOnMove();
            TourOnAccepted(r);
            Play(r);
        }

        // ---- Boşta ipucu --------------------------------------------------------------------------

        private void ResetIdleHint()
        {
            _idleSec = 0f;
            if (_idleShown) { _idleShown = false; _board.HideIdleHint(); }
        }

        /// <summary>Her kare: oyuncu <see cref="IdleHintDelay"/> sn bir şey yapmazsa en büyük patlatılabilir grup yanıp söner.</summary>
        private void IdleHintTick(float dt)
        {
            bool input = Input.touchCount > 0 || Input.GetMouseButton(0) || Input.anyKeyDown;
            bool calm = !input && CurrentPhase == Phase.Idle && _session != null && _session.State.Status == GameStatus.Idle
                && !_hud.PanelOpen && !_menu.IsOpen && !_tourOn && _boosterMode == null && !_paintMode && _traySlot < 0
                && !_swapPending && !_board.IsPlaying;
            if (!calm) { ResetIdleHint(); return; }
            _idleSec += dt;
            if (_idleShown || _idleSec < IdleHintDelay) return;
            var cells = IdleHintCells();
            if (cells.Count == 0) return;
            _idleShown = true;
            _board.ShowIdleHint(cells);
        }

        /// <summary>Tahtadaki en büyük patlatılabilir grup (eşitse ilk bulunan); yoksa boş.</summary>
        public List<Coord> IdleHintCells()
        {
            var list = new List<Coord>();
            if (_session == null) return list;
            var b = _session.State.Board;
            List<int> best = null;
            foreach (var g in GroupFinder.AllComponents(b))
                if (g.Count >= GroupFinder.MinGroup && (best == null || g.Count > best.Count)) best = g;
            if (best != null) foreach (var i in best) list.Add(new Coord(b.X(i), b.Y(i)));
            return list;
        }

        /// <summary>Testler ve yakalama: boşta süresini ilerletir (girdi denetimi olmadan).</summary>
        public void AdvanceIdleForTest(float sec)
        {
            _idleSec += sec;
            if (_idleShown || _idleSec < IdleHintDelay) return;
            var cells = IdleHintCells();
            if (cells.Count == 0) return;
            _idleShown = true;
            _board.ShowIdleHint(cells);
        }
    }
}
