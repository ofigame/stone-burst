using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Seçili alt menü simgesinin beyaz konturu (kullanıcı, 2026-11-21: "seçili menü iconunun … çevresini beyaz border
    /// yapalım"): simgenin silüeti (KatKat/UISilhouette, yalnız alfa) çevresinde 16 yöne kaydırılmış beyaz kopyalar olarak
    /// simgenin arkasına çizilir; çıkartma gibi kalın beyaz kenar. Simgenin nefes ölçeğini (IconBreath) izler. Gölge yok.
    /// Shader yoksa (eski cihaz) kontur çizilmez.
    /// </summary>
    public sealed class IconOutline : MonoBehaviour
    {
        private static Material _mat;
        private static bool _tried;
        private RectTransform _icon, _root;

        public const int Taps = 16;

        public static IconOutline Attach(Image icon, float thickness)
        {
            if (icon == null || icon.sprite == null) return null;
            var mat = Material();
            if (mat == null) return null;
            var irt = icon.rectTransform;
            var root = UiFactory.Rect("IconOutline", (RectTransform)irt.parent);
            root.anchorMin = irt.anchorMin; root.anchorMax = irt.anchorMax; root.pivot = irt.pivot;
            root.anchoredPosition = irt.anchoredPosition;
            root.sizeDelta = irt.sizeDelta;
            root.SetSiblingIndex(irt.GetSiblingIndex());
            for (int i = 0; i < Taps; i++)
            {
                float a = i * Mathf.PI * 2f / Taps;
                var c = UiFactory.Rect("Tap" + i, root);
                c.anchorMin = Vector2.zero; c.anchorMax = Vector2.one;
                c.offsetMin = c.offsetMax = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * thickness;
                var img = c.gameObject.AddComponent<Image>();
                img.sprite = icon.sprite;
                img.preserveAspect = icon.preserveAspect;
                img.material = mat;
                img.color = Color.white;
                img.raycastTarget = false;
            }
            var o = root.gameObject.AddComponent<IconOutline>();
            o._icon = irt;
            o._root = root;
            return o;
        }

        private static Material Material()
        {
            if (_tried) return _mat;
            _tried = true;
            var sh = Resources.Load<Shader>("Shaders/KatKatUISilhouette");
            if (sh != null && sh.isSupported) _mat = new Material(sh) { name = "UISilhouette", hideFlags = HideFlags.DontSave };
            return _mat;
        }

        private void LateUpdate()
        {
            if (_icon == null || _root == null) return;
            _root.localScale = _icon.localScale;
            _root.localRotation = _icon.localRotation;
        }
    }
}
