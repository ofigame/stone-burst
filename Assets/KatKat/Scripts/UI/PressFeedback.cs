using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Düğme basış geri bildirimi (rehber §8): basılınca 60 ms'de 0.96 ölçek, bırakınca 100 ms'de 1.0.
    /// Pasif düğmede tepki yok. İsteğe bağlı basılı sprite.
    /// </summary>
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float Pressed = 0.96f;
        private float DownMs = 60f, UpMs = 100f;

        private Selectable _sel;
        private Image _img;
        private Sprite _normal, _pressedSprite;
        private float _from = 1f, _to = 1f, _t = 1f, _dur = 1f;

        /// <param name="downMs">Basış süresi (varsayılan 60; v12 sıralama ekranı 80).</param>
        /// <param name="upMs">Bırakış süresi (varsayılan 100; v12 sıralama ekranı 120).</param>
        public static PressFeedback Attach(Selectable s, Sprite pressed = null, float downMs = 60f, float upMs = 100f)
        {
            var f = s.gameObject.AddComponent<PressFeedback>();
            f.DownMs = downMs;
            f.UpMs = upMs;
            f._sel = s;
            f._img = s.targetGraphic as Image;
            f._normal = f._img != null ? f._img.sprite : null;
            f._pressedSprite = pressed;
            return f;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_sel != null && !_sel.IsInteractable()) return;
            Go(Pressed, DownMs);
            if (_pressedSprite != null && _img != null)
            {
                // Basış anındaki görüntü saklanır: düğme yüzü sonradan değişmiş olabilir (ör. HUD'da SkinRound simgeli
                // tek parça görsel koyar). Kullanıcı (2026-11-09): "geri al ve durdur butonları tıklandığında iç iconları
                // yok oluyor" — bırakınca Attach anındaki eski (simgesiz) disk geri konuyordu.
                _normal = _img.sprite;
                _img.sprite = _pressedSprite;
            }
        }

        /// <summary>Basış/bırakış animasyonu yok (ortam nefesi ölçeği devralabilir).</summary>
        public bool Idle => _t >= 1f && _to == 1f;

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        private void Release()
        {
            if (_to == 1f) return;
            Go(1f, UpMs);
            // Yalnız basılı sprite takıldıysa geri al; aksi halde düğmenin o anki yüzüne dokunma.
            if (_pressedSprite != null && _img != null && _normal != null) _img.sprite = _normal;
        }

        private void Go(float to, float ms)
        {
            _from = transform.localScale.x;
            _to = to;
            _dur = ms / 1000f;
            _t = 0f;
        }

        private void Update()
        {
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / _dur);
            float s = Mathf.Lerp(_from, _to, 1f - (1f - _t) * (1f - _t));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
