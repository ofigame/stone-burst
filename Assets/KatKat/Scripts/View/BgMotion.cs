using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Arka plan hareketi (kullanıcı 2026-11-18: "bu görsellerin arkaplanlarına hareket katabilir miyiz" → "önerini yap"):
    /// görsel aynı kalır, üstüne hafif katmanlar biner — akan su parıltısı (görselden çıkarılan su maskesi, döngülü kareler),
    /// süzülen ışık tozları, nefes alan ışık hüzmeleri ve çok yavaş yakınlaşma. Tahta ve taşlar sabit; dokunma alanı yok.
    /// Backdrop'un çocuğu olarak çalışır; yerel birim = kaynak piksel / PPU, böylece katmanlar görselle hizalı kalır.
    /// </summary>
    public sealed class BgMotion : MonoBehaviour
    {
        /// <summary>Tema başına hareket ayarı.</summary>
        public struct Profile
        {
            public bool Water;          // görselde şelale/su var: akan parıltı
            public Rect WaterArea;      // suyun arandığı bölge (0..1, sol alt köken)
            public int Motes;           // ışık tozu sayısı
            public Color MoteColor;
            public float MoteRise;      // tozların yükselme hızı (görsel yüksekliği / sn)
            public Vector2[] Rays;      // ışık hüzmesi tepe noktaları (0..1); boşsa hüzme yok
            public float RayAngle;      // derece, aşağı yönden saat yönünde
            public float Zoom;          // yakınlaşma genliği (0,02 = %2)
            public int Leaves;          // süzülerek düşen yaprak/taç yaprağı sayısı
            public Color LeafColor;
            public int Twinkles;        // görseldeki parlak noktalarda (kristal, su, fener, yıldız) ara ara parlayan ışıltılar
        }

        /// <summary>
        /// Oyundaki hafif hareket (kullanıcı 2026-11-18: "arkaplan görsellerdeki uygun yerlerde ışıltılar olsun hareket katalım çok
        /// yoğun olmayan"): her temada görselin parlak noktalarında 9 ışıltı ve 10 yavaş ışık tozu; şelaleli iki temada (0 Gizemli
        /// Şelale, 1 Büyülü Bahçe) akan su parıltısı. Yakınlaşma/kayma ve hüzme yok.
        /// </summary>
        public static Profile ForSkin(int skin) => new Profile
        {
            Water = skin == 0 || skin == 1, WaterArea = new Rect(0.05f, 0.62f, 0.9f, 0.38f),
            Motes = 10, MoteColor = new Color(1f, 0.93f, 0.7f), MoteRise = 0.008f,
            Rays = null, Zoom = 0f, Leaves = 0, Twinkles = 9,
        };

        // Kullanıcı 2026-11-18: "daha fazla hareket lazım" denendi (48 toz, yapraklar, hızlı su ve kayma), sonra "arkaplanın
        // hareketini azaltalım ilk yaptığın gibi olsun" → ilk sürüm: 26 toz, yaprak yok, su 12 kare/sn, %2,2 yakınlaşma 26 sn.
        public static Profile Garden => new Profile
        {
            Water = true, WaterArea = new Rect(0.05f, 0.62f, 0.9f, 0.38f),
            Motes = 26, MoteColor = new Color(1f, 0.93f, 0.62f), MoteRise = 0.012f,
            Rays = new[] { new Vector2(0.62f, 1.02f), new Vector2(0.80f, 1.02f) }, RayAngle = -18f,
            Zoom = 0.022f,
            Leaves = 0, LeafColor = new Color(0.55f, 0.82f, 0.36f),
        };

        private const int WaterFrames = 24;
        private const int SortBase = Backdrop.SortingOrder + 2; // siyah filtrenin (sıra +1) üstünde: ışıltılar kararmaz

        private SpriteRenderer _bg;
        private Profile _p;
        private Vector3 _baseScale, _basePos;
        private float _w, _h; // görsel yerel boyutu
        private SpriteRenderer _water;
        private Sprite[] _waterFrames;
        private readonly List<(SpriteRenderer sr, Vector2 home, float phase, float size, float speed)> _motes = new List<(SpriteRenderer, Vector2, float, float, float)>();
        private readonly List<(SpriteRenderer sr, float phase)> _rays = new List<(SpriteRenderer, float)>();
        private readonly List<(SpriteRenderer sr, Vector2 home, float phase, float size, float speed)> _leaves = new List<(SpriteRenderer, Vector2, float, float, float)>();
        private System.Random _rng;

        /// <summary>Doluysa saat yerine bu zaman kullanılır (editör video kaydı).</summary>
        public static float? ClockOverride;

        public static BgMotion Attach(SpriteRenderer backdrop, Texture2D source, Profile p, int seed = 7)
        {
            var m = backdrop.gameObject.GetComponent<BgMotion>();
            if (m == null) m = backdrop.gameObject.AddComponent<BgMotion>();
            m.Build(backdrop, source, p, seed);
            return m;
        }

        private void Build(SpriteRenderer bg, Texture2D src, Profile p, int seed)
        {
            Clear();
            _bg = bg; _p = p; _rng = new System.Random(seed);
            _baseScale = transform.localScale; _basePos = transform.localPosition;
            var b = bg.sprite.bounds.size;
            _w = b.x; _h = b.y;
            float ppu = bg.sprite.pixelsPerUnit;
            Texture2D temp = null;
            if (src != null && !src.isReadable && (p.Water || p.Twinkles > 0)) src = temp = ReadableCopy(src);
            if (src != null && src.isReadable)
            {
                if (p.Water) BuildWater(src, ppu);
                if (p.Twinkles > 0) BuildTwinkles(src);
            }
            if (temp != null) { if (Application.isPlaying) Destroy(temp); else DestroyImmediate(temp); }
            BuildRays();
            BuildMotes();
            BuildLeaves();
            Tick(Now);
        }

        /// <summary>Okunamayan (sıkıştırılmış) dokunun GPU üzerinden okunabilir kopyası (tek seferlik, ardından silinir).</summary>
        private static Texture2D ReadableCopy(Texture2D src)
        {
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            t.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return t;
        }

        /// <summary>Hareketi kaldırır (tema hareketsizse ya da azaltılmış hareket açıksa).</summary>
        public void Detach()
        {
            Clear();
            if (Application.isPlaying) Destroy(this); else DestroyImmediate(this);
        }

        // ---- Işıltılar: görselin parlak noktaları -------------------------------------------------------------------

        private readonly List<(SpriteRenderer sr, float phase, float period, float size)> _twinkles = new List<(SpriteRenderer, float, float, float)>();
        private static Sprite _star;

        private void BuildTwinkles(Texture2D src)
        {
            int W = src.width, H = src.height, step = Mathf.Max(4, W / 160);
            var px = src.GetPixels32();
            var cand = new List<(float score, int x, int y, Color c)>();
            for (int y = step; y < H - step; y += step)
            for (int x = step; x < W - step; x += step)
            {
                float u = (float)x / W, v = (float)y / H;
                // Tahta (orta) ve üst panel bölgesi dışında; kenarlara çok yapışmadan.
                if (u > 0.1f && u < 0.9f && v > 0.16f && v < 0.8f) continue;
                if (v > 0.9f) continue;
                Color c = px[y * W + x];
                Color.RGBToHSV(c, out _, out float s, out float val);
                float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                float score = lum + 0.25f * s * val;
                if (lum < 0.55f) continue;
                cand.Add((score, x, y, c));
            }
            cand.Sort((a, b) => b.score.CompareTo(a.score));
            var picked = new List<(int x, int y, Color c)>();
            float minD = W * 0.11f;
            foreach (var k in cand)
            {
                if (picked.Count >= _p.Twinkles) break;
                bool far = true;
                foreach (var q in picked) if ((q.x - k.x) * (q.x - k.x) + (q.y - k.y) * (q.y - k.y) < minD * minD) { far = false; break; }
                if (far) picked.Add((k.x, k.y, k.c));
            }
            var star = Star();
            foreach (var q in picked)
            {
                var go = new GameObject("BgMotion_isilti");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(((float)q.x / W - 0.5f) * _w, ((float)q.y / H - 0.5f) * _h, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = star; sr.sortingOrder = SortBase + 1;
                var col = Color.Lerp(q.c, Color.white, 0.55f); col.a = 0f; sr.color = col;
                _twinkles.Add((sr, (float)(_rng.NextDouble() * 6.28), 2.6f + 2.4f * (float)_rng.NextDouble(), _w * (0.035f + 0.025f * (float)_rng.NextDouble())));
            }
        }

        private static Sprite Star()
        {
            if (_star != null) return _star;
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "BgMotion_yildiz" };
            var p = new Color32[N * N];
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float dx = (i + 0.5f) / N * 2f - 1f, dy = (j + 0.5f) / N * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float arms = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(dx) / 0.08f) * Mathf.Clamp01(1f - Mathf.Abs(dy)),
                                       Mathf.Clamp01(1f - Mathf.Abs(dy) / 0.08f) * Mathf.Clamp01(1f - Mathf.Abs(dx)));
                float glow = Mathf.Pow(Mathf.Clamp01(1f - r / 0.5f), 2f);
                float a = Mathf.Clamp01(arms * arms + glow * 0.7f + Mathf.Clamp01(1f - r / 0.12f));
                p[j * N + i] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(p); t.Apply(false, true);
            return _star = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        }

        private void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c.name.StartsWith("BgMotion")) { if (Application.isPlaying) Destroy(c.gameObject); else DestroyImmediate(c.gameObject); }
            }
            if (_waterFrames != null)
                foreach (var f in _waterFrames)
                    if (f != null) { var tx = f.texture; if (Application.isPlaying) { Destroy(f); Destroy(tx); } else { DestroyImmediate(f); DestroyImmediate(tx); } }
            _motes.Clear(); _rays.Clear(); _leaves.Clear(); _twinkles.Clear(); _water = null; _waterFrames = null;
        }

        private static float Now => ClockOverride ?? Time.time;

        private void Update() { if (ClockOverride == null) Tick(Time.time); }

        public void Tick(float t)
        {
            // Yakınlaşma: 26 sn'de bir nefes; hafif yatay kayma.
            if (_p.Zoom > 0f)
            {
                float z = 1f + _p.Zoom * (0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f / 26f));
                transform.localScale = _baseScale * z;
                transform.localPosition = _basePos + new Vector3(Mathf.Sin(t * Mathf.PI * 2f / 37f) * _w * _baseScale.x * 0.004f, 0f, 0f);
            }
            if (_water != null) _water.sprite = _waterFrames[(int)(t * 12f) % WaterFrames];
            foreach (var r in _rays)
            {
                float a = 0.10f + 0.12f * (0.5f + 0.5f * Mathf.Sin(t * 0.55f + r.phase));
                var c = r.sr.color; c.a = a; r.sr.color = c;
            }
            foreach (var l in _leaves)
            {
                float y = Mathf.Repeat(l.home.y - t * l.speed * 0.045f, 1.1f) - 0.05f; // yukarıdan aşağı süzülür
                float x = l.home.x + Mathf.Sin(t * 0.9f * l.speed + l.phase) * 0.06f + t * 0.004f;
                x = Mathf.Repeat(x, 1f);
                l.sr.transform.localPosition = new Vector3((x - 0.5f) * _w, (y - 0.5f) * _h, 0f);
                float flip = Mathf.Sin(t * 2.2f * l.speed + l.phase); // yaprak dönerek düşer: yatay ölçek -1..1
                l.sr.transform.localScale = new Vector3(l.size * (0.25f + 0.75f * Mathf.Abs(flip)), l.size, 1f);
                l.sr.transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 1.3f * l.speed + l.phase) * 50f + l.phase * 30f);
                var c = _p.LeafColor; c.a = 0.95f * Mathf.Clamp01(Mathf.Min(y + 0.05f, 1.05f - y) * 10f);
                c = Color.Lerp(c, Color.white, flip < 0 ? 0.25f : 0f); // arka yüz açık
                l.sr.color = c;
            }
            foreach (var tw in _twinkles)
            {
                // Çoğu zaman sönük; her periyotta kısa bir parlama (sinüsün tepesi, üs 4).
                float k = Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 2f / tw.period + tw.phase));
                k = k * k * k * k;
                var c = tw.sr.color; c.a = k; tw.sr.color = c;
                float s = tw.size * (0.35f + 0.65f * k);
                tw.sr.transform.localScale = new Vector3(s, s, 1f);
                tw.sr.transform.localEulerAngles = new Vector3(0f, 0f, t * 25f + tw.phase * 20f);
            }
            foreach (var m in _motes)
            {
                float y = Mathf.Repeat(m.home.y + t * _p.MoteRise * m.speed, 1f);
                float x = m.home.x + Mathf.Sin(t * 0.6f * m.speed + m.phase) * 0.02f;
                m.sr.transform.localPosition = new Vector3((x - 0.5f) * _w, (y - 0.5f) * _h, 0f);
                float tw = 0.5f + 0.5f * Mathf.Sin(t * 1.7f * m.speed + m.phase * 3f);
                float edge = Mathf.Clamp01(Mathf.Min(y, 1f - y) * 8f); // üst/alt uçta sönerek döner
                var c = _p.MoteColor; c.a = (0.35f + 0.65f * tw) * edge; m.sr.color = c;
                float s = m.size * (0.85f + 0.3f * tw);
                m.sr.transform.localScale = new Vector3(s, s, 1f);
            }
        }

        // ---- Su: görselden maske, döngülü akış kareleri --------------------------------------------------------------

        private void BuildWater(Texture2D src, float ppu)
        {
            const int Q = 2; // yarım çözünürlük
            int W = src.width, H = src.height;
            var px = src.GetPixels32();
            int x0 = (int)(_p.WaterArea.xMin * W), x1 = (int)(_p.WaterArea.xMax * W);
            int y0 = (int)(_p.WaterArea.yMin * H), y1 = Mathf.Min(H, (int)(_p.WaterArea.yMax * H));
            int mw = (x1 - x0) / Q, mh = (y1 - y0) / Q;
            var mask = new float[mw * mh];
            int minX = mw, minY = mh, maxX = -1, maxY = -1;
            for (int j = 0; j < mh; j++)
            for (int i = 0; i < mw; i++)
            {
                var c = px[(y0 + j * Q) * W + x0 + i * Q];
                Color.RGBToHSV(c, out _, out float s, out float v);
                // Köpüklü su: açık, az doygun, mavimsi beyaz (sütunlar krem: r > b, dışarıda kalır).
                float m = Mathf.Clamp01((v - 0.62f) / 0.2f) * Mathf.Clamp01((0.42f - s) / 0.2f) * (c.b + 6 >= c.r ? 1f : 0f);
                mask[j * mw + i] = m;
                if (m > 0.05f) { minX = Mathf.Min(minX, i); maxX = Mathf.Max(maxX, i); minY = Mathf.Min(minY, j); maxY = Mathf.Max(maxY, j); }
            }
            if (maxX < 0) return;
            // Yumuşak kenar
            mask = Blur3(mask, mw, mh);
            int bw = maxX - minX + 1, bh = maxY - minY + 1;
            _waterFrames = new Sprite[WaterFrames];
            var noise = new float[64 * 64];
            var rng = new System.Random(11);
            for (int k = 0; k < noise.Length; k++) noise[k] = (float)rng.NextDouble();
            for (int f = 0; f < WaterFrames; f++)
            {
                var tex = new Texture2D(bw, bh, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "BgMotion_su" };
                var o = new Color32[bw * bh];
                float shift = f * 64f / WaterFrames;
                for (int j = 0; j < bh; j++)
                for (int i = 0; i < bw; i++)
                {
                    float m = mask[(minY + j) * mw + minX + i];
                    if (m <= 0.01f) continue;
                    // Dikey çizgili akış: x'te keskin, y'de uzun; aşağı akar (y artı kayma).
                    float n = Streak(noise, (minX + i) * 0.9f, (minY + j) * 0.18f + shift);
                    float a = m * Mathf.Clamp01((n - 0.42f) * 2.6f) * 0.8f;
                    o[j * bw + i] = new Color32(255, 255, 255, (byte)(a * 255));
                }
                tex.SetPixels32(o); tex.Apply(false, true);
                _waterFrames[f] = Sprite.Create(tex, new Rect(0, 0, bw, bh), new Vector2(0.5f, 0.5f), ppu / Q);
            }
            var go = new GameObject("BgMotion_su");
            go.transform.SetParent(transform, false);
            float cx = x0 + (minX + bw * 0.5f) * Q, cy = y0 + (minY + bh * 0.5f) * Q;
            go.transform.localPosition = new Vector3((cx - W * 0.5f) / ppu, (cy - H * 0.5f) / ppu, 0f);
            _water = go.AddComponent<SpriteRenderer>();
            _water.sortingOrder = SortBase;
        }

        private static float Streak(float[] n, float x, float y)
        {
            // 64x64 döşenen değer gürültüsü, çift doğrusal; y'de 64 periyot (kareler döngüsü dikişsiz).
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = n[(yi & 63) * 64 + (xi & 63)], b = n[(yi & 63) * 64 + ((xi + 1) & 63)];
            float c = n[((yi + 1) & 63) * 64 + (xi & 63)], d = n[((yi + 1) & 63) * 64 + ((xi + 1) & 63)];
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float[] Blur3(float[] m, int w, int h)
        {
            var o = new float[m.Length];
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float s = 0; int n = 0;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = i + dx, y = j + dy;
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    s += m[y * w + x]; n++;
                }
                o[j * w + i] = s / n;
            }
            return o;
        }

        // ---- Işık hüzmeleri ve tozlar ---------------------------------------------------------------------------------

        private static Sprite _glow, _beam;

        private static Sprite Glow()
        {
            if (_glow != null) return _glow;
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "BgMotion_toz" };
            var p = new Color32[N * N];
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float dx = (i + 0.5f) / N * 2f - 1f, dy = (j + 0.5f) / N * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r); a = a * a * (0.6f + 0.4f * Mathf.Clamp01(1f - r * 3f) * 2.5f);
                p[j * N + i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
            }
            t.SetPixels32(p); t.Apply(false, true);
            return _glow = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        }

        private static Sprite Beam()
        {
            if (_beam != null) return _beam;
            const int W = 64, H = 256;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "BgMotion_huzme" };
            var p = new Color32[W * H];
            for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                float v = (j + 0.5f) / H;          // 1 üst (kaynak)
                float halfW = 0.18f + 0.82f * (1f - v); // aşağı doğru genişler
                float u = Mathf.Abs((i + 0.5f) / W * 2f - 1f) / halfW;
                float a = Mathf.Clamp01(1f - u); a *= a;
                a *= Mathf.SmoothStep(0f, 1f, v * 1.4f) * Mathf.SmoothStep(0f, 1f, (1f - v) * 6f + 0.3f);
                p[j * W + i] = new Color32(255, 250, 225, (byte)(Mathf.Clamp01(a) * 255));
            }
            t.SetPixels32(p); t.Apply(false, true);
            return _beam = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 1f), H);
        }

        private void BuildRays()
        {
            if (_p.Rays == null) return;
            for (int i = 0; i < _p.Rays.Length; i++)
            {
                var go = new GameObject("BgMotion_huzme");
                go.transform.SetParent(transform, false);
                var a = _p.Rays[i];
                go.transform.localPosition = new Vector3((a.x - 0.5f) * _w, (a.y - 0.5f) * _h, 0f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, _p.RayAngle + i * 6f);
                go.transform.localScale = new Vector3(_w * 0.9f, _h * 0.62f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Beam(); sr.sortingOrder = SortBase; sr.color = new Color(1f, 1f, 1f, 0.1f);
                _rays.Add((sr, i * 2.1f));
            }
        }

        private static Sprite _leaf;

        /// <summary>Sivri uçlu yaprak: beyaz gövde (renk SpriteRenderer'dan), orta damar ve kenara doğru hafif koyulaşma.</summary>
        private static Sprite Leaf()
        {
            if (_leaf != null) return _leaf;
            const int W = 48, H = 96;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "BgMotion_yaprak" };
            var p = new Color32[W * H];
            for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                float v = (j + 0.5f) / H, u = (i + 0.5f) / W * 2f - 1f;
                float half = Mathf.Sin(v * Mathf.PI) * 0.92f * (1f - 0.25f * v); // uçlar sivri, alt taraf dolgun
                float d = half <= 0f ? 9f : Mathf.Abs(u) / half;
                float a = Mathf.Clamp01((1f - d) * 6f);
                float shade = 0.78f + 0.22f * (1f - d) - (Mathf.Abs(u) < 0.06f ? 0.18f : 0f);
                byte s = (byte)(Mathf.Clamp01(shade) * 255);
                p[j * W + i] = new Color32(s, s, s, (byte)(a * 255));
            }
            t.SetPixels32(p); t.Apply(false, true);
            return _leaf = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), H);
        }

        private void BuildLeaves()
        {
            for (int i = 0; i < _p.Leaves; i++)
            {
                var go = new GameObject("BgMotion_yaprak");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Leaf(); sr.sortingOrder = SortBase + 1;
                var home = new Vector2((float)_rng.NextDouble(), (float)_rng.NextDouble() * 1.1f);
                float size = _w * (0.035f + 0.025f * (float)_rng.NextDouble());
                _leaves.Add((sr, home, (float)(_rng.NextDouble() * 6.28), size, 0.7f + 0.6f * (float)_rng.NextDouble()));
            }
        }

        private void BuildMotes()
        {
            for (int i = 0; i < _p.Motes; i++)
            {
                var go = new GameObject("BgMotion_toz");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Glow(); sr.sortingOrder = SortBase + 1;
                var home = new Vector2(0.06f + 0.88f * (float)_rng.NextDouble(), (float)_rng.NextDouble());
                float size = _w * (0.024f + 0.026f * (float)_rng.NextDouble());
                _motes.Add((sr, home, (float)(_rng.NextDouble() * 6.28), size, 0.6f + 0.8f * (float)_rng.NextDouble()));
            }
        }
    }
}
