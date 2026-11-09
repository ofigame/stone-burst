using UnityEngine;

namespace KatKat.UI
{
    /// <summary>Çentik/köşe boşluğu: RectTransform'u Screen.safeArea'ya sığdırır.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        /// <summary>Ekran yakalama/test için normalize (0..1) güvenli alan. Boşsa Screen.safeArea kullanılır.</summary>
        public static Rect? NormalizedOverride;

        private Rect _applied = new Rect(-1, -1, -1, -1);

        private void OnEnable() => Apply(true);

        private void Update() => Apply(false);

        /// <summary>Geçerli güvenli alan, ekranın 0..1 oranı olarak.</summary>
        public static Rect CurrentNormalized()
        {
            if (NormalizedOverride.HasValue) return NormalizedOverride.Value;
            if (Screen.width <= 0 || Screen.height <= 0) return new Rect(0f, 0f, 1f, 1f);
            var a = Screen.safeArea;
            return new Rect(a.x / Screen.width, a.y / Screen.height, a.width / Screen.width, a.height / Screen.height);
        }

        public void Apply(bool force)
        {
            var n = CurrentNormalized();
            if (!force && n == _applied) return;
            _applied = n;
            var rt = (RectTransform)transform;
            rt.anchorMin = n.min;
            rt.anchorMax = n.max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
