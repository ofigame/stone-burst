using KatKat.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// 2026-11-27 (kullanıcı: "çift roket görsel olarakta çift olsun paralel yan yana iki roket olsun"): arayüzdeki özel taş
    /// simgesi (tepsi yuvası, sürükleme hayaleti) çift roketse kendi görseli gizlenir, yerine iki paralel roket çizilir: satır çiftinde
    /// alt alta, sütun çiftinde yan yana. Kaynak simgenin sprite ve rengi her karede kopyalanır (soluklaşma, parlama aynen geçer).
    /// </summary>
    public sealed class TwinIcon : MonoBehaviour
    {
        /// <summary>İkiz roketlerin simge boyuna oranı ve merkezden kayması (simge kenarı oranında).</summary>
        public const float Scale = 0.78f, Gap = 0.2f;

        private Image _src, _a, _b;
        private SpecialKind _kind;

        public static TwinIcon On(Image icon)
        {
            var t = icon.GetComponent<TwinIcon>();
            return t != null ? t : icon.gameObject.AddComponent<TwinIcon>();
        }

        public bool IsTwin => _kind.IsDoubleRocket();

        public void Set(SpecialKind kind)
        {
            _kind = kind;
            Sync();
        }

        private void LateUpdate() => Sync();

        private void OnDisable()
        {
            if (_src != null) _src.enabled = true;
        }

        private void Sync()
        {
            if (_src == null) _src = GetComponent<Image>();
            if (_src == null) return;
            bool twin = IsTwin;
            if (twin && _a == null) { _a = Make("TwinA"); _b = Make("TwinB"); }
            if (_a != null) { _a.gameObject.SetActive(twin); _b.gameObject.SetActive(twin); }
            _src.enabled = !twin;
            if (!twin) return;
            var size = _src.rectTransform.rect.size;
            var shift = _kind.IsRowRocket() ? new Vector2(0f, size.y * Gap) : new Vector2(size.x * Gap, 0f);
            Place(_a, shift, size);
            Place(_b, -shift, size);
        }

        private void Place(Image img, Vector2 at, Vector2 size)
        {
            img.sprite = _src.sprite;
            img.color = _src.color;
            img.preserveAspect = true;
            var rt = img.rectTransform;
            rt.anchoredPosition = at;
            rt.sizeDelta = size * Scale;
            rt.localRotation = Quaternion.identity;
        }

        private Image Make(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }
    }
}
