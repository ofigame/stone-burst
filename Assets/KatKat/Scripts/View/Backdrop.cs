using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Oyun tahtasının arkasındaki bahçe arka planı. Oran korunarak ekranı doldurur, taşan kısım
    /// ortadan kırpılır (rehber §3). Etkileşim alanı değildir; yalnızca çizilir.
    /// </summary>
    public sealed class Backdrop : MonoBehaviour
    {
        public const int SortingOrder = -100;
        private SpriteRenderer _sr;

        public static Backdrop Create(Transform parent)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<Backdrop>();
            b._sr = go.AddComponent<SpriteRenderer>();
            b._sr.sortingOrder = SortingOrder;
            return b;
        }

        public bool HasArt => _sr.sprite != null;

        public void SetSprite(Sprite sprite)
        {
            _sr.sprite = sprite;
            _sr.enabled = sprite != null;
        }

        /// <summary>Kameranın ortografik görüş alanını oran koruyarak kaplar.</summary>
        public void Fit(Camera cam)
        {
            if (_sr.sprite == null) return;
            float viewH = cam.orthographicSize * 2f, viewW = viewH * cam.aspect;
            var size = _sr.sprite.bounds.size;
            float s = Mathf.Max(viewW / size.x, viewH / size.y);
            var c = cam.transform.position;
            transform.position = new Vector3(c.x, c.y, 0f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
