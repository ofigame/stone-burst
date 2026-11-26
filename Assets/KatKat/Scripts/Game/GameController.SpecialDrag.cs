using KatKat.Core;
using KatKat.UI;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// 2026-11-26 (kullanıcı: "Roket ve altın rulolar tıkladığımız taşın olduğu yere konacak ve 4 dibinde olan taşlardan birine
    /// sürüklediğimiz zaman o renkteki taşları patlatacak altın rulo. roket ise … tıklandığında kendi özelliğine göre karar verecek";
    /// "Çift roket … İkisi de"): tahtadaki özel taşa basılınca bırakılana ya da sürüklenene kadar beklenir. Bırakınca dokunuş
    /// (roket, çift roket, Kürek ateşlenir; Altın Rulo sürükle uyarısı), hücrenin üçte biri kadar sürükleyince baskın yöndeki
    /// komşuya sürükleme komutu (Altın Rulo → o renk; roket → komşu roketle çift roket). Her ikisi bir hamle.
    /// </summary>
    public sealed partial class GameController
    {
        private bool _sdOn;
        private Vector2 _sdStart;
        private int _sdX, _sdY;
        /// <summary>Sürüklemenin komut sayılması için hücre boyuna oranı.</summary>
        private const float SpecialDragCell = 0.34f;

        private bool BeginSpecialDrag(Vector2 screenPos, int x, int y)
        {
            if (_session == null || CurrentPhase != Phase.Idle || _paintMode || _traySlot >= 0) return false;
            var b = _session.State.Board;
            if (!b.InBounds(x, y) || !b[x, y].Active || !b[x, y].HasSpecial) return false;
            _sdOn = true;
            _sdStart = screenPos;
            _sdX = x;
            _sdY = y;
            Haptics.Tick();
            return true;
        }

        /// <summary>Basılı özel taş: sürükleme eşiği ya da bırakma. Bu karede girdi tüketildiyse true.</summary>
        private bool SpecialDragTick()
        {
            if (!_sdOn) return false;
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
            var d = pos - _sdStart;
            if (d.magnitude > CellScreenSize() * SpecialDragCell)
            {
                _sdOn = false;
                int dir = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? (d.x > 0f ? 0 : 2) : (d.y > 0f ? 3 : 1); // ekranda y yukarı → satır −1
                OnSpecialDrag(_sdX, _sdY, dir);
                return true;
            }
            if (!held)
            {
                _sdOn = false;
                OnCellPressed(_sdX, _sdY);
            }
            return true;
        }

        private float CellScreenSize()
        {
            if (_cam == null || _board == null) return 80f;
            var a = _cam.WorldToScreenPoint(_board.transform.TransformPoint(_board.CellCenter(0, 0)));
            var c = _cam.WorldToScreenPoint(_board.transform.TransformPoint(_board.CellCenter(1, 0)));
            return Mathf.Max(20f, Mathf.Abs(c.x - a.x));
        }

        /// <summary>Özel taş sürüklemesi (testler doğrudan çağırabilir): dir 0 sağ, 1 aşağı, 2 sol, 3 yukarı.</summary>
        public void OnSpecialDrag(int x, int y, int dir)
        {
            _board.DropVeil();
            if (CurrentPhase != Phase.Idle) return;
            var r = _session.Submit(Command.Swap(x, y, dir));
            if (!r.Accepted)
            {
                _sound.Play(Sfx.Invalid);
                Haptics.Light();
                _board.ShakeCells(new[] { new Coord(x, y) });
                var kind = _session.State.Board.InBounds(x, y) ? _session.State.Board[x, y].Special : SpecialKind.None;
                if (r.Reason == RejectReason.NoMovesLeft) { }
                else if (kind == SpecialKind.Rainbow) _hud.ShowToast(Loc.T("special.drag.rainbow"));
                else if (kind.IsRocket()) _hud.ShowToast(Loc.T("special.drag.rocket"));
                else if (kind == SpecialKind.Shovel) _hud.ShowToast(Loc.T("special.drag.shovel"));
                return;
            }
            HeartOnMove();
            TourOnAccepted(r);
            Play(r);
        }
    }
}
