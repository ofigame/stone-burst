using System;
using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Su kipi sunumu (2026-11-25, kullanıcı: "Damla olmayacak. Gerçekten hücrenin dibinde su olacak, su kovasını iptal edelim. hücre
    /// dibindeki su tepsinin içine akacak ve tepsinin dibinden yukarıya doğru su akıttıkça artacak. Su seviyesi yükseldikçe taşlar
    /// suyun içinde kalacak. Animasyonları çarpıcı ve eğlenceli hale getirelim … Gizli sayaç olacak ve o sayaca göre tepside seviye
    /// yükselecek ve tüm tepsi su dolunca bölüm bitecek").
    /// <para>Tepsi suyu tek ağ: etkin hücrelerin alanı alttan seviyeye kadar saydam mavi (derinleştikçe koyu, ışık oyunu), üstte
    /// dalgalı köpük çizgisi; taşlar suyun altında renkleriyle görünür. Hücre boşalınca suyu bir şerit halinde yüzeye dökülür
    /// (yerçekimiyle hızlanır, kıvrılır), değdiği yerde sıçrama halkası ve köpük; su yükselir (yaylı, hafif taşarak), yüzey
    /// çalkalanır, kabarcıklar çıkar. Yüzeyin altındaki hücreden su kabarcık patlamasıyla katılır.</para>
    /// <para>Seviye = varan su / gereken (gizli sayaç); dolu tepside su tahtanın üstünü (taş yüksekliği dahil) kaplar.</para>
    /// </summary>
    public sealed partial class BoardView
    {
        /// <summary>Bir dökülme yüzeye vardı (su birimi): ses ve titreşim.</summary>
        public Action<int> WaterArrived { get; set; }

        private bool _waterOn;
        private int _waterNeed, _waterLogic = -1;
        private float _waterHave, _waterShown, _waterVel, _waterSurge, _waterClock, _waterIdle, _waterBubbleT;
        private bool[] _waterActive;
        private GameObject _waterGo;
        private Mesh _waterMesh;
        private MeshRenderer _waterMr;
        /// <summary>Dökülen şeritler ayrı ağda, taşların üstünde (su gövdesi taşların arkasında).</summary>
        private GameObject _pourGo;
        private Mesh _pourMesh;
        private MeshRenderer _pourMr;
        private readonly List<Vector3> _wv = new List<Vector3>(4096);
        private readonly List<Color> _wc = new List<Color>(4096);
        private readonly List<int> _wt = new List<int>(8192);

        private sealed class Pour
        {
            public Vector3 From;
            public float Age, Width;
            public int Units;
            public bool Landed, Under;
        }

        private struct Ripple { public float X, Age, Amp; }

        private sealed class Bubble
        {
            public SpriteRenderer R;
            public Vector3 P;
            public float Vy, Size, Age, Life, Phase;
            public bool Foam;
            public Vector3 V;
        }

        private readonly List<Pour> _pours = new List<Pour>();
        private readonly List<Ripple> _ripples = new List<Ripple>();
        private readonly List<Bubble> _bubbles = new List<Bubble>();

        public bool WaterFlying => _pours.Count > 0;
        /// <summary>Testler: tepsiye varmış su (birim).</summary>
        public int WaterShown => Mathf.RoundToInt(_waterHave);
        /// <summary>Testler ve yakalama: gösterilen seviye (0–1).</summary>
        public float WaterLevel01 => _waterNeed > 0 ? Mathf.Clamp01(_waterShown / _waterNeed) : 0f;

        /// <summary>Su kipini kurar ya da kapatır (bölüm başı, geri al, yeniden başla): seviye hemen mantık değerinde.</summary>
        public void SetWater(int need, int have, BoardState board)
        {
            ClearWaterFx();
            _waterOn = need > 0 && board != null;
            _waterNeed = need;
            _waterHave = _waterShown = Mathf.Min(have, need);
            _waterVel = 0f;
            _waterSurge = 0f;
            _waterLogic = -1;
            if (!_waterOn)
            {
                if (_waterGo != null) _waterGo.SetActive(false);
                if (_pourGo != null) _pourGo.SetActive(false);
                if (_waterSr != null) _waterSr.enabled = false;
                return;
            }
            _waterActive = new bool[board.Count];
            for (int i = 0; i < board.Count; i++) _waterActive[i] = board[i].Active;
            if (_waterGo == null)
            {
                _waterGo = new GameObject("TrayWater");
                _waterGo.transform.SetParent(transform, false);
                _waterMesh = new Mesh { name = "TrayWater" };
                _waterMesh.MarkDynamic();
                _waterGo.AddComponent<MeshFilter>().sharedMesh = _waterMesh;
                _waterMr = _waterGo.AddComponent<MeshRenderer>();
                _waterMr.sharedMaterial = WaterArt.Material;
                _pourGo = new GameObject("WaterPour");
                _pourGo.transform.SetParent(transform, false);
                _pourMesh = new Mesh { name = "WaterPour" };
                _pourMesh.MarkDynamic();
                _pourGo.AddComponent<MeshFilter>().sharedMesh = _pourMesh;
                _pourMr = _pourGo.AddComponent<MeshRenderer>();
                _pourMr.sharedMaterial = WaterArt.Material;
            }
            _pourGo.SetActive(true);
            _pourMr.sortingOrder = 10 + _height * 4 + 5; // akış taşların üstünden dökülür
            _waterGo.SetActive(true);
            EnsureWaterSprite();
            // 2026-11-25 (kullanıcı: "Tepsiyi doldurduğumuzda anlaşılması zor oluyor taş renklerini"; "Taşların arasında yükselen
            // suyla devam et"): su gövdesi taşların arkasında (tepsi zemini ve çerçeveden sonra, yığınlardan önce); taş yüzleri hep açıkta.
            _waterMr.sortingOrder = 6;
            BuildWaterMesh();
        }

        /// <summary>Hamle sonu mantık değeri: dökülme kalmayınca gösterim buna eşitlenir.</summary>
        public void WaterSettle(int progress)
        {
            if (!_waterOn) return;
            _waterLogic = Mathf.Min(progress, _waterNeed);
            _waterIdle = 0f;
        }

        /// <summary>Ekran yakalama: (x, y) hücresinin suyunu döker (mantık değişmez).</summary>
        public void DebugPour(int x, int y, int units) => LaunchWater(x, y, units);

        public void AdvanceWater(float sec) { for (float t = 0f; t < sec - 1e-4f; t += 0.02f) TickWater(0.02f); }

        // ---- Geometri ----------------------------------------------------------------------------

        /// <summary>Su tepsinin iç kenarından başlar (alt boşluk dahil); dolu tepside üst boşluk da kaplanır.</summary>
        private float WaterBottomY => CellCenter(0, _height - 1).y - _rowPitch * 0.5f - _pitch * 0.2f;
        /// <summary>Dolu tepside su taşların üst yüzünü de örter (en üst satırın üstünde yığın payı).</summary>
        private float WaterTopY => CellCenter(0, 0).y + _rowPitch * 0.5f + _pitch * 0.25f;
        private float WaterCenterX => (CellCenter(0, 0).x + CellCenter(_width - 1, 0).x) * 0.5f;
        private float WaterSurfaceY => Mathf.Lerp(WaterBottomY, WaterTopY, _waterNeed > 0 ? Mathf.Clamp01(_waterShown / _waterNeed) : 0f);

        private float WaveAmp => _pitch * (0.035f + 0.09f * Mathf.Min(1.5f, _waterSurge));

        /// <summary>Yüzeyin x'teki yüksekliği: iki sinüs + yayılan sıçrama halkaları.</summary>
        private float SurfaceAt(float x)
        {
            float level = WaterSurfaceY;
            if (_waterShown <= 0.001f && _ripples.Count == 0) return level;
            float k = 1f / Mathf.Max(0.01f, _pitch);
            float y = level + _waterTilt * (x - WaterCenterX) + WaveAmp * (0.6f * Mathf.Sin(x * k * 2.1f + _waterClock * 2.4f) + 0.4f * Mathf.Sin(x * k * 4.7f - _waterClock * 3.3f));
            foreach (var r in _ripples)
            {
                float d = Mathf.Abs(x - r.X), front = r.Age * _pitch * 3.2f;
                if (d > front + _pitch) continue;
                float env = Mathf.Exp(-r.Age * 1.8f) * Mathf.Clamp01(1f - (d - front) / _pitch);
                y += r.Amp * _pitch * env * Mathf.Cos((d - front) * k * 5f);
            }
            return y;
        }

        private int Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb, Color cc, Color cd)
        {
            int i = _wv.Count;
            _wv.Add(a); _wv.Add(b); _wv.Add(c); _wv.Add(d);
            _wc.Add(ca); _wc.Add(cb); _wc.Add(cc); _wc.Add(cd);
            _wt.Add(i); _wt.Add(i + 1); _wt.Add(i + 2);
            _wt.Add(i); _wt.Add(i + 2); _wt.Add(i + 3);
            return i;
        }

        private Color BodyColor(float x, float y, float surf)
        {
            float span = Mathf.Max(0.01f, WaterTopY - WaterBottomY);
            float depth = Mathf.Clamp01((surf - y) / (span * 0.6f));
            var c = Color.Lerp(new Color(0.30f, 0.76f, 1f, 0.78f), new Color(0.04f, 0.30f, 0.78f, 0.92f), depth);
            float k = 1f / Mathf.Max(0.01f, _pitch);
            // Işık oyunu (kostik): kesişen iki dalganın parlak düğümleri.
            float caustic = Mathf.Max(0f, Mathf.Sin(x * k * 3.1f + y * k * 1.7f + _waterClock * 1.9f) * Mathf.Sin(x * k * 1.3f - y * k * 2.9f - _waterClock * 1.4f));
            c = Color.Lerp(c, new Color(0.85f, 0.98f, 1f, c.a), caustic * caustic * 0.45f);
            return c;
        }

        private void BuildWaterMesh()
        {
            _wv.Clear(); _wc.Clear(); _wt.Clear();
            if (_waterOn && _waterActive != null && _waterSr == null && (_waterShown > 0.001f || _ripples.Count > 0))
            {
                float bottom = WaterBottomY, top = WaterTopY;
                const int Cols = 4, Rows = 3;
                for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    if (!_waterActive[y * _width + x]) continue;
                    var c = CellCenter(x, y);
                    float x0 = c.x - _colPitch * 0.5f, x1 = c.x + _colPitch * 0.5f;
                    float y0 = y == _height - 1 ? bottom : c.y - _rowPitch * 0.5f;
                    float y1 = y == 0 ? top : c.y + _rowPitch * 0.5f;
                    var xs = new float[Cols + 1];
                    var tops = new float[Cols + 1];
                    var surf = new float[Cols + 1];
                    bool any = false;
                    for (int i = 0; i <= Cols; i++)
                    {
                        xs[i] = Mathf.Lerp(x0, x1, i / (float)Cols);
                        surf[i] = SurfaceAt(xs[i]);
                        tops[i] = Mathf.Clamp(surf[i], y0, y1);
                        any |= tops[i] > y0 + 1e-4f;
                    }
                    if (!any) continue;
                    for (int i = 0; i < Cols; i++)
                        for (int j = 0; j < Rows; j++)
                        {
                            float ya0 = Mathf.Lerp(y0, tops[i], j / (float)Rows), ya1 = Mathf.Lerp(y0, tops[i], (j + 1) / (float)Rows);
                            float yb0 = Mathf.Lerp(y0, tops[i + 1], j / (float)Rows), yb1 = Mathf.Lerp(y0, tops[i + 1], (j + 1) / (float)Rows);
                            Quad(new Vector3(xs[i], ya0), new Vector3(xs[i], ya1), new Vector3(xs[i + 1], yb1), new Vector3(xs[i + 1], yb0),
                                BodyColor(xs[i], ya0, surf[i]), BodyColor(xs[i], ya1, surf[i]), BodyColor(xs[i + 1], yb1, surf[i + 1]), BodyColor(xs[i + 1], yb0, surf[i + 1]));
                        }
                    // Köpük çizgisi: yüzey bu hücrenin içindeyse (dolu tepsinin tavanında yok).
                    float band = _pitch * 0.07f, line = _pitch * 0.018f;
                    for (int i = 0; i < Cols; i++)
                    {
                        float sa = surf[i], sb = surf[i + 1];
                        if ((sa < y0 && sb < y0) || (sa > y1 + band && sb > y1 + band) || surf[i] >= top - 1e-3f) continue;
                        float fa = Mathf.Clamp(sa, y0, y1), fb = Mathf.Clamp(sb, y0, y1);
                        var foam = new Color(0.75f, 0.96f, 1f, 0.75f);
                        var clear = new Color(0.55f, 0.9f, 1f, 0f);
                        Quad(new Vector3(xs[i], Mathf.Max(y0, fa - band)), new Vector3(xs[i], fa), new Vector3(xs[i + 1], fb), new Vector3(xs[i + 1], Mathf.Max(y0, fb - band)), clear, foam, foam, clear);
                        var white = new Color(1f, 1f, 1f, 0.95f);
                        Quad(new Vector3(xs[i], fa - line), new Vector3(xs[i], fa + line), new Vector3(xs[i + 1], fb + line), new Vector3(xs[i + 1], fb - line), white, white, white, white);
                    }
                }
            }
            // Dökülen şeritler (suyun üstünde).
            _waterMesh.Clear();
            _waterMesh.SetVertices(_wv);
            _waterMesh.SetColors(_wc);
            _waterMesh.SetTriangles(_wt, 0);
            _wv.Clear(); _wc.Clear(); _wt.Clear();
            foreach (var p in _pours) PourRibbon(p);
            _pourMesh.Clear();
            _pourMesh.SetVertices(_wv);
            _pourMesh.SetColors(_wc);
            _pourMesh.SetTriangles(_wt, 0);
        }

        /// <summary>Hücreden yüzeye dökülen kıvrımlı şerit: başı yerçekimiyle iner, kuyruğu 0,5 sn (+0,12 sn/birim) sonra.</summary>
        private void PourRibbon(Pour p)
        {
            // 2026-11-25 (kullanıcı: "hücreden akan su daha gerçekçi olsun"): şerit düştükçe hızlanır ve incelir (süreklilik:
            // genişlik ~ 1/√hız), kenarları yumuşak saydam, ortası açık; aşağı akan parlak çizgiler hareketi gösterir, bir yanda ışık
            // yansıması. Düşüşle salınım artar; baş yuvarlak, kuyruk kopunca sivrilir.
            if (p.Under) return;
            float g = _pitch * 26f;
            float head = p.From.y - 0.5f * g * p.Age * p.Age;
            float tt = Mathf.Max(0f, p.Age - 0.5f - 0.12f * p.Units);
            float tail = p.From.y - 0.5f * g * tt * tt;
            float surf = SurfaceAt(p.From.x);
            head = Mathf.Max(head, surf - _pitch * 0.05f);
            if (tail <= head) return;
            const int Seg = 22;
            float k = 1f / Mathf.Max(0.01f, _pitch);
            float v0 = _pitch * 2.2f; // dudaktan çıkış hızı
            bool detached = tt > 0f;
            var pl = new Vector3[5];
            var cl = new Color[5];
            Vector3[] prev = null;
            Color[] prevC = null;
            for (int s = 0; s <= Seg; s++)
            {
                float u = s / (float)Seg;
                float y = Mathf.Lerp(tail, head, u);
                float fallen = Mathf.Max(0f, p.From.y - y);
                float v = Mathf.Sqrt(v0 * v0 + 2f * g * fallen);
                float w = p.Width * Mathf.Clamp(Mathf.Sqrt(v0 / v), 0.6f, 1f);
                // Uçlar: baş yuvarlak, kuyruk (kopmuşsa) sivri, dudakta hafif geniş.
                float headCap = Mathf.Sqrt(Mathf.Clamp01((1f - u) * (tail - head) / (p.Width * 0.5f)));
                float tailCap = detached ? Mathf.Clamp01(u * (tail - head) / (p.Width * 1.2f)) : 1f + 0.25f * Mathf.Clamp01(1f - fallen * k * 2f);
                w *= headCap * tailCap;
                float wob = _pitch * (0.012f + 0.04f * Mathf.Clamp01(fallen * k / 4f));
                float cx = p.From.x + Mathf.Sin(y * k * 4.2f + _waterClock * 13f) * wob + Mathf.Sin(y * k * 9f - _waterClock * 21f) * wob * 0.35f;
                float streak = 0.5f + 0.5f * Mathf.Sin((fallen * k * 5f) - _waterClock * 18f + p.From.x * k);
                var body = Color.Lerp(new Color(0.08f, 0.45f, 0.95f), new Color(0.55f, 0.88f, 1f), 0.15f + 0.4f * streak);
                var edge = new Color(0.10f, 0.45f, 0.92f, 0f);
                float a = 0.95f * Mathf.Clamp01(1f - fallen * k * 0.03f);
                pl[0] = new Vector3(cx - w * 0.5f, y); cl[0] = edge;
                pl[1] = new Vector3(cx - w * 0.40f, y); cl[1] = new Color(body.r * 0.85f, body.g * 0.9f, body.b, a);
                pl[2] = new Vector3(cx - w * 0.12f, y); cl[2] = new Color(Mathf.Lerp(body.r, 1f, 0.55f), Mathf.Lerp(body.g, 1f, 0.55f), 1f, a); // ışık yansıması
                pl[3] = new Vector3(cx + w * 0.40f, y); cl[3] = new Color(body.r, body.g, body.b, a);
                pl[4] = new Vector3(cx + w * 0.5f, y); cl[4] = edge;
                if (prev != null)
                    for (int j = 0; j < 4; j++) Quad(prev[j], pl[j], pl[j + 1], prev[j + 1], prevC[j], cl[j], cl[j + 1], prevC[j + 1]);
                prev = (Vector3[])pl.Clone();
                prevC = (Color[])cl.Clone();
            }
        }

        // ---- Dökülme -----------------------------------------------------------------------------

        /// <summary>Hücre boşaldı: dipteki su (birim) tepsiye dökülür. Hücre birikintisi şişip söner.</summary>
        private void LaunchWater(int x, int y, int units)
        {
            if (!_waterOn || units <= 0) return;
            var stack = StackAt(x, y);
            if (stack != null) stack.SetWater(0, true);
            var from = CellCenter(x, y) + new Vector3(0f, -_pitch * 0.2f, 0f);
            if (KatKat.UI.Motion.Reduced) { Land(units, from.x); return; }
            bool under = from.y <= WaterSurfaceY;
            _pours.Add(new Pour { From = from, Units = units, Width = _pitch * (0.34f + 0.08f * units), Under = under });
            // Fışkırma: birikintiden yukarı köpük sıçrar.
            for (int i = 0; i < 4 + units * 2; i++)
                SpawnFoam(from + new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f) * _pitch, _pitch * 0.1f), new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(1.5f, 3.2f)) * _pitch, 0.12f);
            if (under) for (int i = 0; i < 6 + units * 4; i++) SpawnBubble(from + new Vector3(UnityEngine.Random.Range(-0.35f, 0.35f) * _pitch, UnityEngine.Random.Range(-0.2f, 0.2f) * _pitch), true);
        }

        private void Land(int units, float x)
        {
            _waterHave = Mathf.Min(_waterNeed, _waterHave + units);
            _waterSurge = Mathf.Min(1.8f, _waterSurge + 0.6f + 0.25f * units);
            _waterVel += 0.6f * units; // hafif taşma (yay)
            _ripples.Add(new Ripple { X = x, Age = 0f, Amp = 0.10f + 0.04f * units });
            if (_ripples.Count > 8) _ripples.RemoveAt(0);
            float s = WaterSurfaceY;
            if (!KatKat.UI.Motion.Reduced)
            {
                for (int i = 0; i < 6 + units * 3; i++)
                {
                    float a = UnityEngine.Random.Range(0.15f, 0.85f) * Mathf.PI;
                    SpawnFoam(new Vector3(x, s), new Vector3(Mathf.Cos(a) * UnityEngine.Random.Range(1f, 2.6f), Mathf.Sin(a) * UnityEngine.Random.Range(2f, 4f)) * _pitch, 0.11f);
                }
                for (int i = 0; i < 3 + units * 2; i++) SpawnBubble(new Vector3(x + UnityEngine.Random.Range(-0.4f, 0.4f) * _pitch, s - UnityEngine.Random.Range(0.2f, 0.9f) * _pitch), true);
            }
            if (_waterHave >= _waterNeed && !KatKat.UI.Motion.Reduced)
                for (int i = 0; i < 30; i++) SpawnBubble(new Vector3(Mathf.Lerp(CellCenter(0, 0).x, CellCenter(_width - 1, 0).x, UnityEngine.Random.value), Mathf.Lerp(WaterBottomY, WaterTopY, UnityEngine.Random.value)), true);
            WaterArrived?.Invoke(units);
        }

        // ---- Kabarcık ve köpük -------------------------------------------------------------------

        private void SpawnBubble(Vector3 p, bool burst)
        {
            if (_bubbles.Count > 90) return;
            var go = new GameObject("WaterBubble");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WaterArt.Bubble;
            sr.sortingOrder = 7; // suyun içinde, taşların arkasında
            _bubbles.Add(new Bubble { R = sr, P = p, Vy = _pitch * UnityEngine.Random.Range(0.9f, burst ? 2.2f : 1.4f), Size = _pitch * UnityEngine.Random.Range(0.07f, burst ? 0.2f : 0.13f), Life = 6f, Phase = UnityEngine.Random.value * 6f });
        }

        private void SpawnFoam(Vector3 p, Vector3 v, float size)
        {
            if (_bubbles.Count > 90) return;
            var go = new GameObject("WaterFoam");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpecialArt.Glow;
            sr.sortingOrder = 10 + _height * 4 + 7;
            _bubbles.Add(new Bubble { R = sr, P = p, V = v, Foam = true, Size = _pitch * size * UnityEngine.Random.Range(0.7f, 1.4f), Life = UnityEngine.Random.Range(0.45f, 0.7f) });
        }

        private void TickBubbles(float dt)
        {
            float g = _pitch * 9f;
            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                var b = _bubbles[i];
                b.Age += dt;
                if (b.R == null) { _bubbles.RemoveAt(i); continue; }
                float a;
                if (b.Foam)
                {
                    b.V.y -= g * dt;
                    b.P += b.V * dt;
                    a = Mathf.Clamp01(1f - b.Age / b.Life);
                    b.R.color = new Color(0.85f, 0.98f, 1f, a);
                    b.R.transform.localScale = Vector3.one * (b.Size * (0.6f + 0.4f * a) / Mathf.Max(0.01f, b.R.sprite.bounds.size.x));
                }
                else
                {
                    b.P.y += b.Vy * dt;
                    b.P.x += Mathf.Sin(b.Age * 7f + b.Phase) * _pitch * 0.25f * dt;
                    float surf = SurfaceAt(b.P.x);
                    if (b.P.y >= surf) { b.Age = b.Life; }
                    a = Mathf.Clamp01(b.Age / 0.15f) * 0.85f;
                    b.R.color = new Color(1f, 1f, 1f, a);
                    b.R.transform.localScale = Vector3.one * (b.Size * (1f + 0.15f * Mathf.Sin(b.Age * 11f)) / Mathf.Max(0.01f, b.R.sprite.bounds.size.x));
                }
                b.R.transform.localPosition = b.P;
                if (b.Age >= b.Life)
                {
                    if (!b.Foam && b.Size > _pitch * 0.12f) SpawnFoam(new Vector3(b.P.x, SurfaceAt(b.P.x)), new Vector3(0f, _pitch * 1.2f), 0.06f); // yüzeyde patlar
                    Destroy(b.R.gameObject);
                    _bubbles.RemoveAt(i);
                }
            }
        }

        // ---- Kare --------------------------------------------------------------------------------

        private void TickWater(float dt)
        {
            if (!_waterOn) return;
            _waterClock += dt;
            TickTilt(dt);
            // Dökülmeler: baş yüzeye değince su katılır; kuyruk da inince şerit biter.
            float g = _pitch * 26f;
            for (int i = _pours.Count - 1; i >= 0; i--)
            {
                var p = _pours[i];
                p.Age += dt;
                if (p.Under)
                {
                    if (!p.Landed && p.Age > 0.3f) { p.Landed = true; Land(p.Units, p.From.x); }
                    if (p.Age > 0.4f) _pours.RemoveAt(i);
                    continue;
                }
                float head = p.From.y - 0.5f * g * p.Age * p.Age;
                float surf = SurfaceAt(p.From.x);
                if (!p.Landed && head <= surf) { p.Landed = true; Land(p.Units, p.From.x); }
                if (p.Landed && !KatKat.UI.Motion.Reduced && UnityEngine.Random.value < dt * 34f)
                    SpawnFoam(new Vector3(p.From.x, surf), new Vector3(UnityEngine.Random.Range(-1.4f, 1.4f), UnityEngine.Random.Range(1.2f, 2.6f)) * _pitch, 0.08f);
                float tt = Mathf.Max(0f, p.Age - 0.5f - 0.12f * p.Units);
                float tail = p.From.y - 0.5f * g * tt * tt;
                if (p.Landed && tail <= surf) _pours.RemoveAt(i);
                else if (p.Age > 4f) { if (!p.Landed) Land(p.Units, p.From.x); _pours.RemoveAt(i); }
            }
            // Mantığa eşitleme: dökülme bitince (ya da 3 sn sonra).
            if (_waterLogic >= 0)
            {
                _waterIdle += dt;
                if (_pours.Count == 0 || _waterIdle > 3f)
                {
                    if (Mathf.Abs(_waterHave - _waterLogic) > 0.01f) _waterHave = _waterLogic;
                    _waterLogic = -1;
                }
            }
            // Seviye: yaylı yükseliş (hafif taşarak oturur).
            float target = _waterHave;
            _waterVel += (target - _waterShown) * 34f * dt;
            _waterVel *= Mathf.Exp(-7.5f * dt);
            _waterShown += _waterVel * dt;
            if (_waterShown < 0f) { _waterShown = 0f; _waterVel = 0f; }
            if (_waterShown > _waterNeed) { _waterShown = _waterNeed; _waterVel = Mathf.Min(0f, _waterVel); }
            _waterSurge = Mathf.MoveTowards(_waterSurge, 0f, dt * 0.8f);
            for (int i = _ripples.Count - 1; i >= 0; i--)
            {
                var r = _ripples[i];
                r.Age += dt;
                _ripples[i] = r;
                if (r.Age > 2.2f) _ripples.RemoveAt(i);
            }
            // Ortam kabarcıkları: su varken ara ara dipten yükselir.
            if (_waterShown > 0.05f && !KatKat.UI.Motion.Reduced)
            {
                _waterBubbleT -= dt;
                if (_waterBubbleT <= 0f)
                {
                    _waterBubbleT = UnityEngine.Random.Range(0.15f, 0.45f);
                    float bx = Mathf.Lerp(CellCenter(0, 0).x, CellCenter(_width - 1, 0).x, UnityEngine.Random.value);
                    float surf = WaterSurfaceY;
                    float by = Mathf.Lerp(WaterBottomY, surf, UnityEngine.Random.Range(0f, 0.4f));
                    if (surf - WaterBottomY > _pitch * 0.4f && IsWaterColumn(bx)) SpawnBubble(new Vector3(bx, by), false);
                }
            }
            TickBubbles(dt);
            FloatStacks();
            BuildWaterMesh();
            UpdateWaterShader();
        }

        /// <summary>
        /// Su altındaki taşlar yüzer (kullanıcı 2026-11-25: "suyun içinde kalan taşlar yavaş yüzme efekti olsun, yani oldukları yerden
        /// biraz yüzüyormuş gibi olsunlar"): her yığın kendi evresinde yavaşça salınır (yatay ve dikey, ~%3 hücre); su yüzeyine
        /// yaklaştıkça etki söner. Azaltılmış harekette yok.
        /// </summary>
        private void FloatStacks()
        {
            float t = _waterClock;
            for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
            {
                var st = StackAt(x, y);
                if (st == null) continue;
                var c = CellCenter(x, y);
                float k = KatKat.UI.Motion.Reduced ? 0f : Mathf.Clamp01((SurfaceAt(c.x) - (c.y + _pitch * 0.2f)) / (_pitch * 0.6f));
                float ph = x * 1.7f + y * 2.3f;
                var off = new Vector3(Mathf.Sin(t * 0.8f + ph) * _pitch * 0.028f, Mathf.Sin(t * 1.15f + ph * 1.4f) * _pitch * 0.04f, 0f) * k;
                st.transform.localPosition = new Vector3(c.x + off.x, c.y + off.y, st.transform.localPosition.z);
            }
        }

        private bool IsWaterColumn(float x)
        {
            int col = Mathf.Clamp(Mathf.RoundToInt((x - CellCenter(0, 0).x) / Mathf.Max(0.01f, _colPitch)), 0, _width - 1);
            return _waterActive != null && _waterActive[(_height - 1) * _width + col];
        }

        // ---- Doğal tepsi suyu (gölgelendirici, maske, eğim) ---------------------------------------------------------------

        private SpriteRenderer _waterSr;
        private Material _waterShaderMat;
        private Sprite _waterMaskSprite;
        private Texture2D _waterMaskTex;
        private float _waterTilt;
        /// <summary>Ekran yakalama / test: ivmeölçer yerine bu (x = sağa eğim, y = aşağı yerçekimi).</summary>
        public static Vector3? TiltOverride;

        /// <summary>Tepsi pişirilince (TrayArt) iç alan maskesi: aynı boy, pivot ve piksel/birim; aynı yerde durur.</summary>
        private void BuildWaterMask(int tw, int th, float ppu)
        {
            var m = TrayArt.LastWaterMask;
            if (m == null || m.Length != tw * th) { _waterMaskSprite = null; return; }
            if (_waterMaskTex == null || _waterMaskTex.width != tw || _waterMaskTex.height != th)
            {
                if (_waterMaskTex != null) DestroyImmediate(_waterMaskTex);
                _waterMaskTex = new Texture2D(tw, th, TextureFormat.Alpha8, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "tray_water_mask" };
            }
            _waterMaskTex.SetPixelData(m, 0);
            _waterMaskTex.Apply(false, false);
            if (_waterMaskSprite != null) DestroyImmediate(_waterMaskSprite);
            _waterMaskSprite = Sprite.Create(_waterMaskTex, new Rect(0, 0, tw, th), Vector2.zero, ppu);
            _waterMaskSprite.hideFlags = HideFlags.DontSave;
            if (_waterSr != null) PlaceWaterSprite();
        }

        private void EnsureWaterSprite()
        {
            if (_waterMaskSprite == null || _floorSr == null) { if (_waterSr != null) _waterSr.enabled = false; return; }
            if (_waterShaderMat == null)
            {
                var sh = KatKat.View.Res.Load<Shader>("Shaders/KatKatTrayWater");
                if (sh == null) return;
                _waterShaderMat = new Material(sh) { name = "KatKatTrayWater", hideFlags = HideFlags.DontSave };
            }
            if (_waterSr == null)
            {
                _waterSr = new GameObject("TrayWaterShaded").AddComponent<SpriteRenderer>();
                _waterSr.transform.SetParent(transform, false);
                _waterSr.sharedMaterial = _waterShaderMat;
                _waterSr.sortingOrder = 6; // taşların arkasında
            }
            PlaceWaterSprite();
        }

        private void PlaceWaterSprite()
        {
            _waterSr.sprite = _waterMaskSprite;
            _waterSr.transform.localPosition = _floorSr.transform.localPosition;
            _waterSr.transform.localScale = _floorSr.transform.localScale;
            _waterSr.enabled = _waterOn;
        }

        /// <summary>
        /// Ekran eğimi (kullanıcı 2026-11-25: "ekranı çevirdiğimiz yön doğrultusunda su eğim alsın"): yerçekiminin ekrandaki yönüne
        /// dik yüzey; telefon sağa yatınca su sağda yükselir. Yumuşak izler; azaltılmış harekette düz.
        /// </summary>
        private void TickTilt(float dt)
        {
            Vector3 a = TiltOverride ?? (SystemInfo.supportsAccelerometer ? Input.acceleration : Vector3.zero);
            float want = 0f;
            if (!KatKat.UI.Motion.Reduced && a.sqrMagnitude > 0.01f)
                want = Mathf.Clamp(a.x / Mathf.Max(0.35f, -a.y), -0.55f, 0.55f);
            _waterTilt = Mathf.Lerp(_waterTilt, want, 1f - Mathf.Exp(-4f * dt));
        }

        private void UpdateWaterShader()
        {
            if (_waterSr == null || _waterShaderMat == null) return;
            bool show = _waterOn && (_waterShown > 0.001f || _ripples.Count > 0);
            _waterSr.enabled = show;
            if (!show) return;
            var t = transform;
            float sc = Mathf.Abs(t.lossyScale.y);
            var m = _waterShaderMat;
            m.SetFloat("_Level", t.TransformPoint(new Vector3(0f, WaterSurfaceY, 0f)).y);
            m.SetFloat("_CenterX", t.TransformPoint(new Vector3(WaterCenterX, 0f, 0f)).x);
            m.SetFloat("_Tilt", _waterTilt);
            m.SetFloat("_Amp", WaveAmp * sc);
            m.SetFloat("_Pitch", _pitch * sc);
            m.SetFloat("_WTime", _waterClock);
            m.SetFloat("_Span", (WaterTopY - WaterBottomY) * sc);
            m.SetFloat("_Bottom", t.TransformPoint(new Vector3(0f, WaterBottomY, 0f)).y);
            for (int i = 0; i < 4; i++)
            {
                var v = Vector4.zero;
                int j = _ripples.Count - 1 - i;
                if (j >= 0) { var r = _ripples[j]; v = new Vector4(t.TransformPoint(new Vector3(r.X, 0f, 0f)).x, r.Age, r.Amp, 0f); }
                m.SetVector("_Rip" + i, v);
            }
        }

        /// <summary>Atlama: dökülmeler hemen yüzeye varır, kabarcıklar silinir.</summary>
        private void FinishWater()
        {
            foreach (var p in _pours) if (!p.Landed) { p.Landed = true; Land(p.Units, p.From.x); }
            _pours.Clear();
            ClearWaterFx();
            _waterShown = _waterHave;
            _waterVel = 0f;
            if (_waterOn && _waterMesh != null) BuildWaterMesh();
        }

        private void ClearWaterFx()
        {
            _pours.Clear();
            _ripples.Clear();
            foreach (var b in _bubbles) if (b.R != null) Destroy(b.R.gameObject);
            _bubbles.Clear();
        }
    }
}
