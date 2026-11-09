using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Dokunulacak hücreyi gösteren işaret (öğretici, deneme ipucu, reklamlı ipucu).
    /// Normal kip: nabız atan turkuaz halka.
    /// Güçlü kip (reklamlı ipucu; kullanıcı 2026-11-09: "ipucu aldığımda taş üzerinde göstergesi daha belirgin olsun hiç
    /// anlaşılmıyor"): hücreyi saran kalın altın çerçeve (iki kat), dışa yayılan altın nabız ve taşın üstünde zıplayan
    /// büyük altın ok. Kök ölçeklenmez; yalnız nabız halkası büyür (ok ve çerçeve sabit kalır).
    /// </summary>
    public sealed class TapHint : MonoBehaviour
    {
        private static readonly Color Teal = new Color(0.12f, 0.47f, 0.49f), Gold = new Color(1f, 0.80f, 0.25f);
        private SpriteRenderer _pulse, _frameA, _frameB, _arrow, _glow;
        private float _glowUnit = 1f;
        private float _ringUnit, _cell;
        private bool _strong;

        public static TapHint Create(Transform parent, Sprite ring, Sprite arrow = null, Sprite glow = null)
        {
            var go = new GameObject("TapHint");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<TapHint>();
            if (glow != null) { h._glow = Child(go.transform, "Glow", glow, 899); h._glowUnit = glow.rect.width / glow.pixelsPerUnit; }
            h._pulse = Child(go.transform, "Pulse", ring, 900);
            h._frameA = Child(go.transform, "FrameOuter", ring, 901);
            h._frameB = Child(go.transform, "FrameInner", ring, 902);
            if (arrow != null)
            {
                h._arrow = Child(go.transform, "Arrow", arrow, 903);
                // Sol ok görseli 90° döndürülür: aşağıyı (taşı) gösterir.
                h._arrow.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            h._ringUnit = ring != null ? ring.rect.width / ring.pixelsPerUnit : 1f;
            go.SetActive(false);
            return h;
        }

        private static SpriteRenderer Child(Transform parent, string name, Sprite sprite, int order)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        public void Show(Vector3 position, float cellSize) => Show(position, cellSize, false);

        public void Show(Vector3 position, float cellSize, bool strong)
        {
            _strong = strong;
            _cell = cellSize;
            transform.localPosition = position;
            transform.localScale = Vector3.one;
            _frameA.gameObject.SetActive(strong);
            _frameB.gameObject.SetActive(strong);
            if (_glow != null) _glow.gameObject.SetActive(strong);
            if (_arrow != null)
            {
                _arrow.gameObject.SetActive(strong);
                float a = cellSize * 1.15f / Mathf.Max(0.0001f, _arrow.sprite.rect.width / _arrow.sprite.pixelsPerUnit);
                _arrow.transform.localScale = new Vector3(a, a, 1f);
            }
            gameObject.SetActive(true);
            Update(); // ilk karede doğal boyut/beyaz renkte görünmesin
        }

        public void Hide() => gameObject.SetActive(false);

        public bool Visible => gameObject.activeSelf;

        private float RingScale(float cells) => _cell * cells / Mathf.Max(0.0001f, _ringUnit);

        private void Update()
        {
            float now = Time.unscaledTime;
            if (_strong)
            {
                // Dışa yayılan nabız: hücrenin 0,9 → 1,7 katı, solarak.
                float t = now * 1.2f % 1f;
                float s = RingScale(Mathf.Lerp(1.3f, 2.3f, t));
                _pulse.transform.localScale = new Vector3(s, s, 1f);
                _pulse.color = new Color(Gold.r, Gold.g, Gold.b, Mathf.Lerp(0.9f, 0f, t));
                // Sabit kalın çerçeve: iki halka üst üste (dış altın, iç parlak), hafif nefes.
                float breathe = 1f + 0.04f * Mathf.Sin(now * 5f);
                float fa = RingScale(1.5f * breathe), fb = RingScale(1.38f * breathe);
                _frameA.transform.localScale = new Vector3(fa, fa, 1f);
                _frameB.transform.localScale = new Vector3(fb, fb, 1f);
                _frameA.color = new Color(Gold.r, Gold.g, Gold.b, 1f);
                _frameB.color = new Color(1f, 0.96f, 0.7f, 0.85f + 0.15f * Mathf.Sin(now * 6f));
                // Taşın üstünde yarı saydam altın parıltı (taşı aydınlatır).
                if (_glow != null)
                {
                    float g = _cell * 1.25f / Mathf.Max(0.0001f, _glowUnit);
                    _glow.transform.localScale = new Vector3(g, g, 1f);
                    _glow.color = new Color(1f, 0.88f, 0.35f, 0.28f + 0.14f * Mathf.Sin(now * 5f));
                }
                // Zıplayan ok: taşın üstünde, aşağıyı gösterir.
                if (_arrow != null)
                {
                    float bounce = Mathf.Abs(Mathf.Sin(now * 4.2f));
                    _arrow.transform.localPosition = new Vector3(0f, _cell * (0.95f + 0.25f * bounce), 0f);
                    _arrow.color = Gold;
                }
                return;
            }
            float u = now * 2.2f % 1f;
            float sc = RingScale(0.75f * Mathf.Lerp(0.55f, 1.05f, u));
            _pulse.transform.localScale = new Vector3(sc, sc, 1f);
            _pulse.color = new Color(Teal.r, Teal.g, Teal.b, (1f - u) * 0.9f);
        }
    }
}
