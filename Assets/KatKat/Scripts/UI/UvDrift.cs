using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// 2026-11-28 ana sayfa üst mor zemini: RawImage dokusunu çok yavaşça kaydırır (yumuşak hareket hissi). Hız: doku tekrarı/saniye.
    /// Azaltılmış harekette durur.
    /// </summary>
    public sealed class UvDrift : MonoBehaviour
    {
        public Vector2 Speed;
        private RawImage _img;

        private void Awake() => _img = GetComponent<RawImage>();

        private void Update()
        {
            if (_img == null || Motion.Reduced) return;
            var r = _img.uvRect;
            float dt = Time.unscaledDeltaTime;
            r.x = Mathf.Repeat(r.x + Speed.x * dt, 1f);
            r.y = Mathf.Repeat(r.y + Speed.y * dt, 1f);
            _img.uvRect = r;
        }
    }
}
