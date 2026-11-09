using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Dokunulacak hücreyi gösteren nabız atan halka (öğretici, deneme ipucu, reklamlı ipucu).
    /// Güçlü kip (reklamlı ipucu, kullanıcı 2026-11-09: "ipucu hemen yok oluyor"): altın renk, halka hiç tam
    /// sönmez ve hücrenin üstünde sabit bir parıltı kalır; ipucu bakarken kaybolmaz.
    /// </summary>
    public sealed class TapHint : MonoBehaviour
    {
        private static readonly Color Teal = new Color(0.12f, 0.47f, 0.49f), Gold = new Color(1f, 0.80f, 0.25f);
        private SpriteRenderer _sr, _glow;
        private float _baseScale;
        private bool _strong;

        public static TapHint Create(Transform parent, Sprite ring)
        {
            var go = new GameObject("TapHint");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<TapHint>();
            h._sr = go.AddComponent<SpriteRenderer>();
            h._sr.sprite = ring;
            h._sr.sortingOrder = 900;
            var glow = new GameObject("Steady");
            glow.transform.SetParent(go.transform, false);
            h._glow = glow.AddComponent<SpriteRenderer>();
            h._glow.sprite = ring;
            h._glow.sortingOrder = 901;
            glow.SetActive(false);
            go.SetActive(false);
            return h;
        }

        public void Show(Vector3 position, float cellSize) => Show(position, cellSize, false);

        public void Show(Vector3 position, float cellSize, bool strong)
        {
            _strong = strong;
            transform.localPosition = position;
            _baseScale = cellSize * 0.75f / (_sr.sprite.rect.width / _sr.sprite.pixelsPerUnit);
            _glow.gameObject.SetActive(strong);
            gameObject.SetActive(true);
            Update(); // ilk karede doğal boyut/beyaz renkte görünmesin (çerçeve dışına taşıyordu)
        }

        public void Hide() => gameObject.SetActive(false);

        public bool Visible => gameObject.activeSelf;

        private void Update()
        {
            if (_strong)
            {
                float t = Time.unscaledTime * 1.4f % 1f;
                float s = _baseScale * Mathf.Lerp(0.85f, 1.3f, t);
                transform.localScale = new Vector3(s, s, 1f);
                _sr.color = new Color(Gold.r, Gold.g, Gold.b, Mathf.Lerp(0.95f, 0.25f, t));
                // Sabit halka: dış nabzın ölçeğinden bağımsız, hücreyi saran altın çerçeve.
                float k = 0.95f / Mathf.Lerp(0.85f, 1.3f, t);
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
                _glow.transform.localScale = new Vector3(k, k, 1f);
                _glow.color = new Color(1f, 0.92f, 0.55f, pulse);
                return;
            }
            float u = Time.unscaledTime * 2.2f % 1f;
            float sc = _baseScale * Mathf.Lerp(0.55f, 1.05f, u);
            transform.localScale = new Vector3(sc, sc, 1f);
            _sr.color = new Color(Teal.r, Teal.g, Teal.b, (1f - u) * 0.9f);
        }
    }
}
