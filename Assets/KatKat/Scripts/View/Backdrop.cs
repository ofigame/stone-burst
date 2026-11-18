using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Oyun tahtasının arkasındaki bahçe arka planı. Oran korunarak ekranı doldurur, taşan kısım
    /// ortadan kırpılır (rehber §3). Etkileşim alanı değildir; yalnızca çizilir.
    /// Önünde yarı saydam siyah filtre (kullanıcı 2026-11-18: "arkaplanı daha koyu yapmak için önüne siyah opaklaştırılmış
    /// bir filtre koyalım"): görselle birlikte ölçeklenir, sırası arka plan + 1 (hareket katmanları ve tahta üstünde kalır).
    /// </summary>
    public sealed class Backdrop : MonoBehaviour
    {
        public const int SortingOrder = -100;
        /// <summary>Siyah filtrenin opaklığı (0 = filtre yok).</summary>
        public const float DimAlpha = 0.48f; // önce 0,35; kullanıcı 2026-11-18 "biraz daha koyu olsun"
        private SpriteRenderer _sr, _dim;
        private static Sprite _white;

        public static Backdrop Create(Transform parent)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<Backdrop>();
            b._sr = go.AddComponent<SpriteRenderer>();
            b._sr.sortingOrder = SortingOrder;
            var dim = new GameObject("BackdropDim");
            dim.transform.SetParent(go.transform, false);
            b._dim = dim.AddComponent<SpriteRenderer>();
            b._dim.sprite = White();
            b._dim.color = new Color(0f, 0f, 0f, DimAlpha);
            b._dim.sortingOrder = SortingOrder + 1;
            b._dim.enabled = false;
            return b;
        }

        private static Sprite White()
        {
            if (_white != null) return _white;
            var t = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "backdrop_dim", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            t.SetPixels32(px); t.Apply(false, true);
            return _white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        public bool HasArt => _sr.sprite != null;

        public void SetSprite(Sprite sprite)
        {
            _sr.sprite = sprite;
            _sr.enabled = sprite != null;
            if (_dim != null)
            {
                _dim.enabled = sprite != null && DimAlpha > 0f;
                if (sprite != null)
                {
                    // Görselin yerel sınırını kapla (biraz taşarak; yakınlaşma/kayma sırasında kenar açılmasın).
                    var b = sprite.bounds;
                    _dim.transform.localPosition = new Vector3(b.center.x, b.center.y, 0f);
                    _dim.transform.localScale = new Vector3(b.size.x * 1.1f, b.size.y * 1.1f, 1f);
                }
            }
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
