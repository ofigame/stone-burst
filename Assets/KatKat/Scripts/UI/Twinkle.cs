using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Işıltılı yıldızcıklar (kullanıcı, 2026-11-20: "mağazada külçelere ışıltılı yıldızlar koyalım"; güçlendiriciler "ışıltılı
    /// olsun"). Ev sahibinin dikdörtgeni içinde rastgele noktada beliren, dönerek büyüyüp sönen dört kollu yıldızlar; her biri
    /// kendi fazında, söndükçe yeni yerde doğar. Dokunma yakalamaz. Ölçeklenmemiş zaman (duraklatmadan bağımsız).
    /// Azaltılmış harekette yıldızlar gizli.
    /// </summary>
    public sealed class Twinkle : MonoBehaviour
    {
        public const float Life = 1.15f;

        private Image[] _stars;
        private float[] _age, _size;
        private float _spread = 0.5f, _maxSize = 40f;
        private Color _tint = Color.white;
        private RectTransform _host;
        private bool _on = true;

        /// <param name="host">Yıldızların doğduğu alan (çocuk olarak eklenir).</param>
        /// <param name="count">Aynı anda yıldız sayısı.</param>
        /// <param name="maxSize">En büyük yıldız boyu (referans px).</param>
        /// <param name="spread">Alanın merkezden ne kadarına yayılır (0,5 = kenara kadar).</param>
        public static Twinkle Attach(RectTransform host, int count, float maxSize, float spread = 0.5f, Color? tint = null)
        {
            var root = UiFactory.Rect("Twinkle", host);
            UiFactory.Stretch(root);
            var tw = root.gameObject.AddComponent<Twinkle>();
            tw._host = host;
            tw._maxSize = maxSize;
            tw._spread = spread;
            tw._tint = tint ?? new Color(1f, 0.98f, 0.85f, 1f);
            var sprite = PremiumArt.V4("effects/sparkle") ?? FxKit.Star;
            tw._stars = new Image[count];
            tw._age = new float[count];
            tw._size = new float[count];
            for (int i = 0; i < count; i++)
            {
                var img = UiFactory.Image("Star" + i, root, sprite, Color.clear);
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                tw._stars[i] = img;
                tw._age[i] = -i * Life / count; // eşit aralıklı fazlar
                tw.Respawn(i);
            }
            return tw;
        }

        /// <summary>Yıldızlar görünsün mü (ör. güçlendirici elde yokken kapalı).</summary>
        public void SetOn(bool on)
        {
            if (_on == on) return;
            _on = on;
            if (!on && _stars != null) foreach (var s in _stars) s.color = Color.clear;
        }

        private void Respawn(int i)
        {
            var r = _host != null ? _host.rect : new Rect(-50f, -50f, 100f, 100f);
            float w = Mathf.Max(20f, r.width), h = Mathf.Max(20f, r.height);
            // Dikdörtgenin kenarına yakın bir nokta (ortası nesnenin kendisi): elips üzerinde, biraz içe/dışa sapmalı.
            float a = Random.Range(0f, Mathf.PI * 2f), k = Random.Range(0.55f, 1f) * _spread * 2f;
            var p = new Vector2(Mathf.Cos(a) * w * 0.5f * k, Mathf.Sin(a) * h * 0.5f * k) + r.center;
            _stars[i].rectTransform.anchoredPosition = p;
            _size[i] = _maxSize * Random.Range(0.55f, 1f);
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float dt)
        {
            if (_stars == null) return;
            bool show = _on && !Motion.Reduced;
            for (int i = 0; i < _stars.Length; i++)
            {
                var s = _stars[i];
                if (!show) { s.color = Color.clear; continue; }
                _age[i] += dt;
                if (_age[i] >= Life) { _age[i] -= Life; Respawn(i); }
                if (_age[i] < 0f) { s.color = Color.clear; continue; }
                float t = _age[i] / Life;
                // Hızlı doğuş, yavaş sönüş; ortada kısa bir parlama.
                float g = t < 0.35f ? Ease.OutBack(t / 0.35f) : 1f - Mathf.Pow((t - 0.35f) / 0.65f, 2f);
                g = Mathf.Max(0f, g);
                float sz = _size[i] * g;
                s.rectTransform.sizeDelta = new Vector2(sz, sz);
                s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
                var c = _tint;
                c.a = Mathf.Clamp01(g * 1.1f) * _tint.a;
                s.color = c;
            }
        }
    }
}
