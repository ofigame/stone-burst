using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Yolculuk haritası hareketi (2026-11-19; kullanıcı: "yolculuk görselimizdeki su ağaç çicek gibi objelere hareket
    /// katabilir miyiz" → "evet bahçeden başla"). Şerit parçasının maskesi varsa (Resources/PremiumV6/map/&lt;ad&gt;-motion.png,
    /// tools/map_motion_masks.ps1) karo <c>KatKat/MapMotion</c> malzemesiyle çizilir: yapraklar salınır, çiçekler kıpırdar,
    /// su dalgalanıp parlar. Üstüne birkaç kelebek (çiçekten çiçeğe) ve suda yayılan halkalar biner. Yol, taş ve köprü
    /// sabit; dokunma alanı yok. Azaltılmış hareket açıksa hiçbiri kurulmaz. Zaman menü saatinden (<see cref="Tick"/>).
    /// </summary>
    public static class MapMotion
    {
        private static readonly int TimeId = Shader.PropertyToID("_KatKatMapTime");
        private static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();
        private static readonly Dictionary<string, (List<Vector2> water, List<Vector2> flowers)> _points = new Dictionary<string, (List<Vector2>, List<Vector2>)>();
        private static Shader _shader;
        private static bool _shaderTried;
        private static readonly List<MapTileLife> _lives = new List<MapTileLife>();

        /// <summary>Kapatılabilir (testler, karşılaştırma görüntüsü).</summary>
        public static bool Enabled = true;

        /// <summary>Parçanın hareket malzemesi; maske/shader yoksa ya da hareket kapalıysa null (düz çizim).</summary>
        public static Material For(Sprite part)
        {
            if (!Enabled || Motion.Reduced || part == null) return null;
            if (_mats.TryGetValue(part.name, out var m)) return m;
            if (!_shaderTried)
            {
                _shaderTried = true;
                _shader = KatKat.View.Res.Load<Shader>("Shaders/KatKatMapMotion");
                // Cihazda hareket görünmezse (2026-11-19 kullanıcı: "yolculukta hareket yoktu") logcat'te sebep görünsün.
                Debug.Log("[KatKat] Harita hareketi: shader " + (_shader == null ? "yok" : _shader.isSupported ? "hazır" : "desteklenmiyor"));
                if (_shader != null && !_shader.isSupported) _shader = null;
            }
            Texture2D mask = null;
            if (_shader != null) mask = KatKat.View.Res.Load<Texture2D>("PremiumV6/map/" + part.name + "-motion");
            if (mask != null)
            {
                m = new Material(_shader) { name = "MapMotion_" + part.name, hideFlags = HideFlags.DontSave };
                m.SetTexture("_Motion", mask);
            }
            _mats[part.name] = m;
            return m;
        }

        /// <summary>Kelebek ve su halkaları: karonun dünya dikdörtgeninin çocuğu olarak kurulur.</summary>
        public static void AddLife(RectTransform tile, Sprite part, int seed)
        {
            if (For(part) == null) return;
            var pts = Points(part.name);
            if (pts.water.Count == 0 && pts.flowers.Count == 0) return;
            var life = tile.gameObject.AddComponent<MapTileLife>();
            life.Build(tile, pts.water, pts.flowers, seed);
            _lives.Add(life);
        }

        /// <summary>Menü saatiyle çağrılır (her kare; video kaydında kayıt saatiyle).</summary>
        public static void Tick(float t)
        {
            Shader.SetGlobalFloat(TimeId, t);
            for (int i = _lives.Count - 1; i >= 0; i--)
            {
                if (_lives[i] == null) { _lives.RemoveAt(i); continue; }
                _lives[i].Tick(t);
            }
        }

        private static (List<Vector2> water, List<Vector2> flowers) Points(string name)
        {
            if (_points.TryGetValue(name, out var p)) return p;
            var w = new List<Vector2>(); var f = new List<Vector2>();
            var ta = KatKat.View.Res.Load<TextAsset>("PremiumV6/map/" + name + "-motion");
            if (ta != null)
                foreach (var line in ta.text.Split('\n'))
                {
                    var s = line.Trim().Split(' ');
                    if (s.Length < 3) continue;
                    var v = new Vector2(float.Parse(s[1], CultureInfo.InvariantCulture), float.Parse(s[2], CultureInfo.InvariantCulture));
                    (s[0] == "w" ? w : f).Add(v);
                }
            return _points[name] = (w, f);
        }

        private static Sprite _wing;

        /// <summary>Kelebeğin tek kanadı (üst büyük, alt küçük lob; kenara doğru koyulaşır). Renk Image.color ile.</summary>
        internal static Sprite Wing()
        {
            if (_wing != null) return _wing;
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "map_kelebek", hideFlags = HideFlags.DontSave };
            var p = new Color32[N * N];
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float x = (i + 0.5f) / N, y = (j + 0.5f) / N * 2f - 1f; // x: gövdeden dışa 0..1
                float up = Ellipse(x - 0.5f, y - 0.36f, 0.48f, 0.52f), lo = Ellipse(x - 0.38f, y + 0.42f, 0.36f, 0.42f);
                float d = Mathf.Min(up, lo);
                float a = Mathf.Clamp01((1f - d) * 8f);
                float shade = 0.72f + 0.28f * Mathf.Clamp01(1f - d * 0.9f) - (d > 0.82f ? 0.35f : 0f); // koyu kenar çizgisi
                byte s = (byte)(Mathf.Clamp01(shade) * 255);
                p[j * N + i] = new Color32(s, s, s, (byte)(a * 255));
            }
            t.SetPixels32(p); t.Apply(false, true);
            return _wing = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0f, 0.5f), N);
        }

        private static float Ellipse(float x, float y, float rx, float ry) => Mathf.Sqrt(x * x / (rx * rx) + y * y / (ry * ry));
    }

    /// <summary>Bir karo üstündeki kelebekler ve su halkaları (konumlar karonun 0..1 uv'sinde).</summary>
    public sealed class MapTileLife : MonoBehaviour
    {
        private sealed class Fly { public RectTransform Root; public Image L, R; public int A, B; public float Phase, Speed; }
        private sealed class Ripple { public Image Img; public Vector2 At; public float Phase, Period; }

        private RectTransform _tile;
        private List<Vector2> _flowers;
        private readonly List<Fly> _flies = new List<Fly>();
        private readonly List<Ripple> _ripples = new List<Ripple>();
        private static readonly Color[] FlyColors = { new Color(1f, 0.78f, 0.25f), new Color(0.55f, 0.8f, 1f), new Color(1f, 0.6f, 0.82f) };

        public void Build(RectTransform tile, List<Vector2> water, List<Vector2> flowers, int seed)
        {
            _tile = tile; _flowers = flowers;
            var rng = new System.Random(seed * 7919 + 13);
            float w = tile.rect.width;
            if (flowers.Count >= 2)
                for (int i = 0; i < 2; i++)
                {
                    var root = UiFactory.Rect("Kelebek", tile);
                    root.sizeDelta = Vector2.one;
                    var col = FlyColors[(seed + i) % FlyColors.Length];
                    var l = Wing(root, col, true, w); var r = Wing(root, col, false, w);
                    var body = UiFactory.Image("Govde", root, View.SpecialArt.Disc, new Color(0.25f, 0.18f, 0.12f));
                    body.raycastTarget = false;
                    body.rectTransform.sizeDelta = new Vector2(w * 0.008f, w * 0.03f);
                    _flies.Add(new Fly { Root = root, L = l, R = r, A = rng.Next(flowers.Count), B = rng.Next(flowers.Count),
                        Phase = (float)rng.NextDouble() * 10f, Speed = 0.8f + 0.4f * (float)rng.NextDouble() });
                }
            for (int i = 0; i < Mathf.Min(4, water.Count); i++)
            {
                var img = UiFactory.Image("Halka", tile, View.FxKit.Ring, new Color(1f, 1f, 1f, 0f));
                img.raycastTarget = false;
                img.transform.SetSiblingIndex(0);
                _ripples.Add(new Ripple { Img = img, At = water[(i * 3 + seed) % water.Count], Phase = (float)rng.NextDouble() * 6f, Period = 3.2f + 1.6f * (float)rng.NextDouble() });
            }
        }

        private static Image Wing(RectTransform root, Color col, bool left, float w)
        {
            var img = UiFactory.Image(left ? "Sol" : "Sag", root, MapMotion.Wing(), col);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(w * 0.028f, w * 0.042f);
            rt.anchoredPosition = Vector2.zero;
            if (left) rt.localScale = new Vector3(-1f, 1f, 1f);
            return img;
        }

        public void Tick(float t)
        {
            if (_tile == null) return;
            float w = _tile.rect.width, h = _tile.rect.height;
            Vector2 P(Vector2 uv) => new Vector2((uv.x - 0.5f) * w, (uv.y - 0.5f) * h);
            foreach (var f in _flies)
            {
                // 6 sn'lik döngü: 4,2 sn uçuş (çiçekten çiçeğe, kavisli), 1,8 sn çiçek üstünde (kanatlar yavaş).
                float cyc = 6f / f.Speed, tt = t + f.Phase * cyc;
                int n = Mathf.FloorToInt(tt / cyc);
                float u = tt / cyc - n;
                int a = (f.A + n * 3) % _flowers.Count, b = (f.A + n * 3 + 1 + f.B % 3) % _flowers.Count;
                var pa = P(_flowers[a]); var pb = P(_flowers[b]);
                float k = Mathf.Clamp01(u / 0.7f); k = k * k * (3f - 2f * k);
                var pos = Vector2.Lerp(pa, pb, k);
                float arc = Mathf.Sin(k * Mathf.PI) * w * 0.06f;
                pos += new Vector2(Mathf.Sin(tt * 2.1f) * w * 0.012f, arc + Mathf.Sin(tt * 3.3f) * w * 0.008f);
                bool resting = u >= 0.7f;
                f.Root.anchoredPosition = pos + new Vector2(0f, resting ? w * 0.012f : 0f);
                float flapHz = resting ? 1.1f : 7.5f;
                float open = resting ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(tt * flapHz * Mathf.PI)) : 0.2f + 0.8f * Mathf.Abs(Mathf.Sin(tt * flapHz * Mathf.PI));
                f.L.rectTransform.localScale = new Vector3(-open, 1f, 1f);
                f.R.rectTransform.localScale = new Vector3(open, 1f, 1f);
                float dir = pb.x >= pa.x ? -1f : 1f;
                f.Root.localEulerAngles = new Vector3(0f, 0f, resting ? Mathf.Sin(tt * 0.8f) * 6f : dir * 14f + Mathf.Sin(tt * 4f) * 6f);
            }
            foreach (var r in _ripples)
            {
                float x = Mathf.Repeat(t / r.Period + r.Phase, 1f);
                float s = w * (0.02f + 0.07f * x);
                var rt = r.Img.rectTransform;
                rt.anchoredPosition = P(r.At);
                rt.sizeDelta = new Vector2(s, s * 0.45f);
                r.Img.color = new Color(1f, 1f, 1f, 0.55f * (1f - x) * Mathf.Clamp01(x * 6f));
            }
        }
    }
}
