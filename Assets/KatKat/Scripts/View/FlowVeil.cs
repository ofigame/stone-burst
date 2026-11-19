using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Taş akışı perdesi (2026-11-19; kullanıcı: "taşlar yükselecek arkaplan kararacak sadece yükselen taşlar ve gideceği
    /// hedefler kararmayacak. Hedeflere gittikten sonra tekrar kararma kalkacak", "az bir kararma yapalım göreyim").
    /// Kameranın tamamını örten yarı saydam koyu perde (%30): tahta, arka plan ve üst panel (HUD kanvası, sıra 500) kararır;
    /// uçan taşlar ve yıldızlar (sıra 1000+) perdenin üstünde parlak kalır. Hedefler (görev simgesi, yıldız çubuğu) perde
    /// dokusundaki yumuşak kenarlı ışık deliklerinden görünür. ~0,2 sn'de iner, akış bitince ~0,3 sn'de kalkar; uçuşta
    /// tahtaya dokunulursa 0,1 sn'de kalkar ve akış bitene dek yeniden inmez. Azaltılmış harekette inmez.
    /// </summary>
    public sealed class FlowVeil : MonoBehaviour
    {
        public const float Strength = 0.3f;
        public const int Order = 900;
        private const float InSec = 0.2f, OutSec = 0.3f, DropSec = 0.1f;
        private const int TexH = 160;

        private SpriteRenderer _sr;
        private Texture2D _tex;
        private Camera _cam;
        private float _a, _target, _speed = 1f / InSec;
        private bool _suppressed;
        private readonly List<Vector2> _spots = new List<Vector2>();
        private readonly List<Rect> _bars = new List<Rect>();
        private float _spotRadius;
        private bool _dirty;

        public bool IsUp => _target > 0f;
        public float Alpha => _a;

        public static FlowVeil Create(Transform parent, Camera cam)
        {
            var go = new GameObject("FlowVeil");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<FlowVeil>();
            v._cam = cam;
            v._sr = go.AddComponent<SpriteRenderer>();
            v._sr.sortingOrder = Order;
            v._sr.enabled = false;
            return v;
        }

        /// <summary>Perdeyi indirir (ya da inikken yeni hedef delikleri ekler).</summary>
        /// <param name="spot">Aydınlık kalacak hedef (görev simgesi); yoksa null.</param>
        /// <param name="bar">Aydınlık kalacak yıldız çubuğu; boşsa yok.</param>
        /// <param name="spotRadius">Hedef deliğinin yarıçapı (dünya).</param>
        public void Raise(Vector3? spot, Rect bar, float spotRadius)
        {
            if (KatKat.UI.Motion.Reduced || _suppressed || _cam == null) return;
            if (_target <= 0f && _a <= 0.001f) { _spots.Clear(); _bars.Clear(); _dirty = true; }
            _spotRadius = spotRadius;
            if (spot.HasValue) AddSpot(spot.Value);
            if (bar.width > 0f && !_bars.Contains(bar)) { _bars.Add(bar); _dirty = true; }
            _target = 1f;
            _speed = 1f / InSec;
        }

        private void AddSpot(Vector3 p)
        {
            var v = new Vector2(p.x, p.y);
            foreach (var s in _spots) if ((s - v).sqrMagnitude < 1e-4f) return;
            _spots.Add(v);
            _dirty = true;
        }

        /// <summary>Akış bitti: perde yavaşça kalkar.</summary>
        public void Lower()
        {
            if (_target <= 0f) return;
            _target = 0f;
            _speed = 1f / OutSec;
        }

        /// <summary>Uçuş sırasında tahtaya dokunuş: perde hızla kalkar, akış bitene dek yeniden inmez.</summary>
        public void Drop()
        {
            if (_target <= 0f && _a <= 0f) return;
            _suppressed = true;
            _target = 0f;
            _speed = 1f / DropSec;
        }

        /// <summary>Akış tamamen bitti: sonraki hamlede yeniden inebilir.</summary>
        public void Rearm() => _suppressed = false;

        /// <summary>Anında kaldırır (geri al, yeniden başla, bölüm değişimi).</summary>
        public void Clear()
        {
            _target = 0f;
            _a = 0f;
            _suppressed = false;
            if (_sr != null) _sr.enabled = false;
            HideHalos();
        }

        /// <summary>Perdeyi verilen süre kadar ilerletir (Update ve kare yakalama aracı).</summary>
        public void Advance(float ms)
        {
            _a = Mathf.MoveTowards(_a, _target, _speed * ms / 1000f);
            _t += ms / 1000f;
            if (_sr == null) return;
            if (_a <= 0.001f)
            {
                _sr.enabled = false;
                HideHalos();
                return;
            }
            Fit();
            if (_dirty) Rebuild();
            _sr.enabled = true;
            _sr.color = new Color(1f, 1f, 1f, _a);
            TickHalos();
        }

        // ---- Hedef haleleri (2026-11-19, kullanıcı: "taşlar ve hedefler daha ışıltılı olsun") ----
        // Perde inikken görev simgesi ve yıldız çubuğu nabız gibi atan sıcak bir haleyle parlar (perdenin üstünde, taşların altında).
        private const int HaloOrder = 950;
        private readonly List<SpriteRenderer> _halos = new List<SpriteRenderer>();
        private float _t;

        private SpriteRenderer HaloAt(int i)
        {
            while (_halos.Count <= i)
            {
                var sr = new GameObject("VeilHalo").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(transform.parent, false);
                FxKit.MakeAdditive(sr);
                sr.sprite = FxKit.Glow;
                sr.sortingOrder = HaloOrder;
                _halos.Add(sr);
            }
            return _halos[i];
        }

        private void TickHalos()
        {
            if (KatKat.UI.Motion.Reduced) { HideHalos(); return; }
            float pulse = 0.7f + 0.3f * Mathf.Sin(_t * 6.5f);
            float z = transform.position.z;
            int n = 0;
            foreach (var s in _spots)
            {
                var h = HaloAt(n++);
                Place(h, new Vector3(s.x, s.y, z), _spotRadius * 3.2f, _spotRadius * 3.2f);
                h.color = new Color(1f, 0.93f, 0.75f, 0.42f * _a * pulse);
                h.enabled = true;
            }
            foreach (var b in _bars)
            {
                var h = HaloAt(n++);
                Place(h, new Vector3(b.center.x, b.center.y, z), b.width * 1.12f, b.height * 2.6f);
                h.color = new Color(1f, 0.85f, 0.45f, 0.3f * _a * pulse);
                h.enabled = true;
            }
            for (int i = n; i < _halos.Count; i++) if (_halos[i] != null) _halos[i].enabled = false;
        }

        private static void Place(SpriteRenderer h, Vector3 pos, float w, float hgt)
        {
            h.transform.position = pos;
            var b = h.sprite != null ? h.sprite.bounds.size : Vector3.one;
            var parent = h.transform.parent;
            float ps = parent != null ? Mathf.Max(1e-4f, Mathf.Abs(parent.lossyScale.x)) : 1f;
            h.transform.localScale = new Vector3(w / Mathf.Max(1e-4f, b.x) / ps, hgt / Mathf.Max(1e-4f, b.y) / ps, 1f);
        }

        private void HideHalos()
        {
            foreach (var h in _halos) if (h != null) h.enabled = false;
        }

        private float CamH => _cam.orthographicSize * 2f;
        private float CamW => CamH * Mathf.Max(0.1f, _cam.aspect);

        private void Fit()
        {
            var c = _cam.transform.position;
            var parent = transform.parent;
            float z = parent != null ? parent.position.z : 0f;
            transform.position = new Vector3(c.x, c.y, z);
            transform.rotation = Quaternion.identity;
            float ps = parent != null ? Mathf.Max(1e-4f, Mathf.Abs(parent.lossyScale.x)) : 1f;
            // Sprite 1 birim yüksekliğinde; biraz taşırarak kenar boşluğu kalmasın.
            float s = CamH * 1.04f / ps;
            transform.localScale = new Vector3(s, s, 1f);
        }

        private void Rebuild()
        {
            _dirty = false;
            int h = TexH;
            int w = Mathf.Max(8, Mathf.RoundToInt(h * Mathf.Max(0.1f, _cam.aspect)));
            if (_tex == null || _tex.width != w || _tex.height != h)
            {
                _tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    name = "flow_veil", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave
                };
                _sr.sprite = Sprite.Create(_tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h);
            }
            var c = _cam.transform.position;
            float camH = CamH * 1.04f, camW = camH * Mathf.Max(0.1f, _cam.aspect);
            float x0 = c.x - camW * 0.5f, y0 = c.y - camH * 0.5f;
            float px = camW / w, py = camH / h;
            float soft = _spotRadius * 0.6f;
            var dark = new Color32(8, 6, 22, 0);
            var pixels = new Color32[w * h];
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                var p = new Vector2(x0 + (i + 0.5f) * px, y0 + (j + 0.5f) * py);
                float hole = 0f;
                foreach (var s in _spots)
                {
                    float d = Vector2.Distance(p, s);
                    hole = Mathf.Max(hole, 1f - Mathf.SmoothStep(0f, 1f, (d - _spotRadius) / soft));
                }
                foreach (var b in _bars)
                {
                    // Yuvarlatılmış dikdörtgen uzaklığı (kenara göre), yumuşak kenar.
                    float dx = Mathf.Max(b.xMin - p.x, 0f, p.x - b.xMax);
                    float dy = Mathf.Max(b.yMin - p.y, 0f, p.y - b.yMax);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    hole = Mathf.Max(hole, 1f - Mathf.SmoothStep(0f, 1f, d / soft));
                }
                var col = dark;
                col.a = (byte)Mathf.RoundToInt(255f * Strength * (1f - hole));
                pixels[j * w + i] = col;
            }
            _tex.SetPixels32(pixels);
            _tex.Apply(false);
        }

        private void OnDestroy()
        {
            if (_tex != null) Destroy(_tex);
        }
    }
}
