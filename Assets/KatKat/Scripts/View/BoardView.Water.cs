using System;
using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Su kipi sunumu: kova (2026-11-26, kullanıcı çizimi: "Su bölümlerini kovaya çeviriyoruz. Ve kova tepsinin alt kısmında bu şekilde
    /// konumlanacak. Çeşitli tepsi şekilleri olabilir. Kovayı dolduracağız").
    /// <para>Kova tepsinin alt ortasındaki oyukta (cam kova; suyu camdan görünür). Oyuğun üst kenarı boşalma yeri: hücrenin oyuğundaki
    /// su, hücre dibine kadar boşalınca alttan akar, tepsi üzerinden boşalma yerine kıvrılarak gider ve oradan kovaya dökülür
    /// (oyuğun yanındaki alçak hücrelerden su yandan kovanın ağzına akar). Varışta sıçrama, halkalar, kabarcık; kova suyu yaylı
    /// yükselir, telefon eğimiyle eğilir (gölgelendirici: KatKatTrayWater, maske: kovanın iç yüzü).</para>
    /// <para>Seviye = varan su / gereken (gizli sayaç). Çatlak ve sızıntı: <see cref="OpenCrack"/> (BoardView.Crack).</para>
    /// </summary>
    public sealed partial class BoardView
    {
        /// <summary>Bir dökülme kovaya vardı (su birimi): ses ve titreşim.</summary>
        public Action<int> WaterArrived { get; set; }

        private bool _waterOn;
        private int _waterNeed, _waterLogic = -1;
        private float _waterHave, _waterShown, _waterVel, _waterSurge, _waterClock, _waterIdle, _waterBubbleT;
        private GameObject _pourGo;
        private Mesh _pourMesh;
        private MeshRenderer _pourMr;
        private readonly List<Vector3> _wv = new List<Vector3>(4096);
        private readonly List<Color> _wc = new List<Color>(4096);
        private readonly List<int> _wt = new List<int>(8192);

        // Kova geometrisi (tahta yerel): merkez x, taban y, boy (doku birimi = S), boşalma yeri.
        private float _bucketX, _bucketBottom, _bucketS, _placedPitch = -1f;
        /// <summary>Kovanın oturduğu yerleşimin imzası (ölçü, satır/sütun adımı, katman sayısı, ilk hücre). 2026-11-26 (kullanıcı:
        /// "123. bölümde kova tahtanın üstüne çıkıyor bir kısmı"; "bölüme tekrar girince sorun görünmedi"): su kurulumu tahta
        /// kurulmadan önce çalışıyor (önceki bölümün ölçüleri); yerleşim herhangi bir şekilde değişince kova yeniden oturur.</summary>
        private Vector4 _placedKey = new Vector4(-1f, -1f, -1f, -1f);
        private int _placedLayers = -1;

        private int _placedBoard = -1;
        private Vector4 PlaceKey() { var c = CellCenter(0, 0); return new Vector4(_pitch, _rowPitch, c.x, c.y); }
        private BoardState _waterBoard;
        private Vector3 _drain;

        // ---- Gönderim uçuşu (2026-11-26, kullanıcı: "kovayı gönderirken sadece su gitmesin. Kova gitsin suyu boşaltsın animasyonu
        // böyle olsun"): kova oyuktan kalkar, görev kartının dolu ucunun üstüne uçar, eğilip suyu karta boşaltır, boş döner. ----
        private float _flyT = -1f, _flyAng, _flyScale = 1f, _flySide = 1f, _flyStartHave;
        private Vector3 _flyOff, _flyTo;
        private bool _flyArrived;
        private const float FlyLift = 0.22f, FlyGo = 0.5f, FlyTilt = 0.26f, FlyPour = 0.62f, FlyBack = 0.55f;
        /// <summary>Döküm açısı (derece) ve uçarken kovanın boyu (oyuktaki boya oranı).</summary>
        private const float FlyAngle = 112f, FlySize = 0.78f;
        /// <summary>Uçarken kova arayüzün (görev kartı, tuval 500) önünde çizilir.</summary>
        private const int FlyOrder = 520;
        public bool WaterBucketFlying => _flyT >= 0f;
        private Transform _bucketRoot;
        private SpriteRenderer _bucketBack, _bucketFront, _waterSr;
        private Material _waterShaderMat;
        private float _waterTilt;
        /// <summary>Ekran yakalama / test: ivmeölçer yerine bu (x = sağa eğim, y = aşağı yerçekimi).</summary>
        public static Vector3? TiltOverride;

        private sealed class Pour
        {
            /// <summary>Sabit yol: hücreden tepsi üzerinden kenarlığın dudağına ve dudağın altına (ya da kova ağzına). Son noktadan
            /// kovadaki su yüzeyine düşüş her karede eklenir (yüzey hareket eder); tek kesintisiz şerit.</summary>
            public Vector3[] Path;
            public float[] Arc;
            public float Length, Age, Width;
            public int Units;
            public bool Landed;
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
        /// <summary>Testler: kovaya varmış su (birim).</summary>
        public int WaterShown => Mathf.RoundToInt(_waterHave);
        /// <summary>Gösterilen doluluk (0–1): hedef yazısındaki yüzde.</summary>
        public float WaterLevel01 => _waterNeed > 0 ? Mathf.Clamp01(_waterShown / _waterNeed) : 0f;
        /// <summary>Testler / yakalama: kovanın tahta yerel merkezi ve boyu.</summary>
        public Vector3 BucketLocal => new Vector3(_bucketX, _bucketBottom, _bucketS);

        /// <summary>Su kipini kurar ya da kapatır (bölüm başı, geri al, yeniden başla): seviye hemen mantık değerinde.</summary>
        public void SetWater(int need, int have, BoardState board)
        {
            ClearWaterFx();
            ClearCracks();
            _waterOn = need > 0 && board != null;
            _waterNeed = need;
            _waterHave = _waterShown = Mathf.Min(have, need);
            _waterVel = 0f;
            _waterSurge = 0f;
            _waterLogic = -1;
            if (_bucketRoot != null) _bucketRoot.gameObject.SetActive(_waterOn);
            if (_pourGo != null) _pourGo.SetActive(_waterOn);
            if (!_waterOn) return;
            _waterBoard = board;
            PlaceBucket(board);
            if (_pourGo == null)
            {
                _pourGo = new GameObject("WaterPour");
                _pourGo.transform.SetParent(transform, false);
                _pourMesh = new Mesh { name = "WaterPour" };
                _pourMesh.MarkDynamic();
                _pourGo.AddComponent<MeshFilter>().sharedMesh = _pourMesh;
                _pourMr = _pourGo.AddComponent<MeshRenderer>();
                _pourMr.sharedMaterial = WaterArt.Material;
            }
            _pourMr.sortingOrder = 10 + _height * 4 + 5; // akış taşların üstünden
            UpdateWaterShader();
            BuildPourMesh();
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

        // ---- Kova yerleşimi --------------------------------------------------------------------------

        /// <summary>
        /// Oyuk: alt satırdan yukarı, ortadaki sütun(lar) etkin olmayan satırlar. En iç satırın genişliği kovanın sığacağı genişlik;
        /// üst kenarı boşalma yeri. Tepsi kenarı hücre kenarından <see cref="TrayArt.Reach"/> taşar (oyuğun görünen ağzı daralır).
        /// Oyuk yoksa kova tahtanın altında.
        /// </summary>
        private void PlaceBucket(BoardState b)
        {
            _placedPitch = _pitch;
            _placedKey = PlaceKey();
            _placedLayers = _maxLayers;
            _placedBoard = b.Width * 1000 + b.Height;
            int w = b.Width, h = b.Height;
            int c0 = (w - 1) / 2, c1 = w / 2;
            int top = h;
            for (int y = h - 1; y >= 0; y--)
            {
                if (b[c0, y].Active || b[c1, y].Active) break;
                top = y;
            }
            float reach = TrayArt.Reach * _pitch;
            // Tepsi zemini yığın yüksekliği kadar kaydırılarak çizilir (BoardView.LayoutFloorTiles: mid); oyuk ve kenarlar oradan.
            StackExtent(out float exTop, out float exBottom);
            float mid = (exTop + exBottom) * 0.5f * _pitch;
            // Tepsinin alt kenarı: etkin hücresi olan en alt satır (köşe boşlukları ve oyuk bütün alt satırı boşaltabilir).
            int low = h - 1;
            for (; low > 0; low--) { bool any = false; for (int x = 0; x < w; x++) any |= b[x, low].Active; if (any) break; }
            float gridBottom = CellCenter(0, low).y - _rowPitch * 0.5f + mid;
            float cx = (CellCenter(c0, 0).x + CellCenter(c1, 0).x) * 0.5f;
            float visW, drainY;
            if (top < h)
            {
                int l = c0, r = c1;
                while (l - 1 >= 0 && !b[l - 1, top].Active) l--;
                while (r + 1 < w && !b[r + 1, top].Active) r++;
                visW = (r - l + 1) * _colPitch - 2f * reach;
                drainY = CellCenter(0, top).y + _rowPitch * 0.5f - reach + mid;
            }
            else
            {
                visW = 3f * _colPitch;
                drainY = gridBottom - reach;
            }
            float s = Mathf.Max(_pitch, visW * 0.92f / (2f * WaterArt.BucketHalfMouth));
            // Ağız boşalma yerinin biraz altında. 2026-11-26 (kullanıcı: "testteki 10. bölümde kova güçlendiricilerin altında kalmış"):
            // kovanın görünen tabanı tepsinin alt kenarını geçmez (alt çubukla çakışmaz); oyuk sığsa kova küçülür.
            float mouthY = drainY - _pitch * 0.18f;
            float maxS = (mouthY - (gridBottom - _pitch * 0.08f)) / (WaterArt.BucketMouth - WaterArt.BucketBottom);
            s = Mathf.Min(s, Mathf.Max(_pitch * 0.8f, maxS));
            _bucketS = s;
            _bucketX = cx;
            _bucketBottom = drainY - _pitch * 0.18f - s * WaterArt.BucketMouth;
            _drain = new Vector3(cx, drainY, 0f);
            if (_bucketRoot == null)
            {
                _bucketRoot = new GameObject("WaterBucket").transform;
                _bucketRoot.SetParent(transform, false);
                _bucketBack = BucketLayer("Back", WaterArt.BucketBack, 5);
                _waterSr = BucketLayer("Water", WaterArt.BucketMask, 6);
                var sh = KatKat.View.Res.Load<Shader>("Shaders/KatKatTrayWater");
                if (sh != null)
                {
                    _waterShaderMat = new Material(sh) { name = "KatKatBucketWater", hideFlags = HideFlags.DontSave };
                    _waterSr.sharedMaterial = _waterShaderMat;
                }
                _bucketFront = BucketLayer("Front", WaterArt.BucketFront, 8);
            }
            _bucketRoot.gameObject.SetActive(true);
            ApplyFly();
        }

        private Vector3 BucketToBoard(float lx, float ly) =>
            new Vector3(_bucketX, _bucketBottom, 0f) + _flyOff + Quaternion.Euler(0f, 0f, _flyAng) * new Vector3(lx * _flyScale, ly * _flyScale, 0f);

        private void ApplyFly()
        {
            if (_bucketRoot == null) return;
            _bucketRoot.localPosition = BucketToBoard(0f, 0f);
            _bucketRoot.localRotation = Quaternion.Euler(0f, 0f, _flyAng);
            _bucketRoot.localScale = Vector3.one * _bucketS * _flyScale;
        }

        private void SetFlySorting(bool on)
        {
            if (_bucketBack != null) _bucketBack.sortingOrder = on ? FlyOrder : 5;
            if (_waterSr != null) _waterSr.sortingOrder = on ? FlyOrder + 1 : 6;
            if (_bucketFront != null) _bucketFront.sortingOrder = on ? FlyOrder + 3 : 8;
            if (_pourMr != null) _pourMr.sortingOrder = on ? FlyOrder + 2 : 10 + _height * 4 + 5;
        }

        private SpriteRenderer BucketLayer(string name, Sprite sp, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_bucketRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = order;
            return sr;
        }

        // ---- Kova suyu ---------------------------------------------------------------------------------

        private float WaterBottomY => _bucketBottom + _bucketS * WaterArt.WaterInBottom;
        private float WaterTopY => _bucketBottom + _bucketS * WaterArt.WaterInTop;
        private float WaterCenterX => _bucketX;
        private float WaterSurfaceY => Mathf.Lerp(WaterBottomY, WaterTopY, _waterNeed > 0 ? Mathf.Clamp01(_waterShown / _waterNeed) : 0f);
        /// <summary>Dalga ölçeği: kovanın boyuna göre.</summary>
        private float WavePitch => Mathf.Max(0.001f, _bucketS * 0.28f);
        private float WaveAmp => WavePitch * (0.05f + 0.12f * Mathf.Min(1.5f, _waterSurge));

        /// <summary>Yüzeyin x'teki yüksekliği: eğim + iki sinüs + yayılan sıçrama halkaları (gölgelendiriciyle aynı formül).</summary>
        private float SurfaceAt(float x)
        {
            float level = WaterSurfaceY;
            float k = 1f / Mathf.Max(0.01f, WavePitch);
            float y = level + _waterTilt * (x - WaterCenterX) + WaveAmp * (0.6f * Mathf.Sin(x * k * 2.1f + _waterClock * 2.4f) + 0.4f * Mathf.Sin(x * k * 4.7f - _waterClock * 3.3f));
            foreach (var r in _ripples)
            {
                float d = Mathf.Abs(x - r.X), front = r.Age * WavePitch * 3.2f;
                if (d > front + WavePitch) continue;
                float env = Mathf.Exp(-r.Age * 1.8f) * Mathf.Clamp01(1f - (d - front) / WavePitch);
                y += r.Amp * WavePitch * env * Mathf.Cos((d - front) * k * 5f);
            }
            return y;
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
            m.SetFloat("_Level", t.TransformPoint(BucketToBoard(0f, WaterSurfaceY - _bucketBottom)).y);
            m.SetFloat("_CenterX", t.TransformPoint(BucketToBoard(0f, 0f)).x);
            m.SetFloat("_Tilt", _waterTilt);
            m.SetFloat("_Amp", WaveAmp * sc);
            m.SetFloat("_Pitch", WavePitch * sc);
            m.SetFloat("_WTime", _waterClock);
            m.SetFloat("_Span", (WaterTopY - WaterBottomY) * sc * _flyScale);
            m.SetFloat("_Bottom", t.TransformPoint(BucketToBoard(0f, WaterBottomY - _bucketBottom)).y);
            for (int i = 0; i < 4; i++)
            {
                var v = Vector4.zero;
                int j = _ripples.Count - 1 - i;
                if (j >= 0 && _flyT < 0f) { var r = _ripples[j]; v = new Vector4(t.TransformPoint(new Vector3(r.X, 0f, 0f)).x, r.Age, r.Amp, 0f); }
                m.SetVector("_Rip" + i, v);
            }
        }

        // ---- Akış --------------------------------------------------------------------------------------

        // 2026-11-26 (kullanıcı: "suyun rengini ve tazyiğini daha belirgin yapalım"): daha hızlı ve kalın akış.
        private const float PourAccel = 26f, PourV0 = 3.4f; // adım/sn², adım/sn

        /// <summary>
        /// Hücre boşaldı: oyuktaki su alttan akar. Yol: hücrenin altından aşağı inip boşalma yerine kıvrılır (ikinci derece eğri);
        /// boşalma yerinin altındaki hücrelerden ise yatay akıp kovanın ağzına yandan döner. Sonra kovaya düşer.
        /// </summary>
        private void LaunchWater(int x, int y, int units)
        {
            if (!_waterOn || units <= 0) return;
            var stack = StackAt(x, y);
            if (stack != null) stack.SetWater(0, true);
            var from = CellCenter(x, y) + new Vector3(0f, -_pitch * 0.28f, 0f); // oyuğun en alt noktası
            if (KatKat.UI.Motion.Reduced) { Land(units, _bucketX); return; }
            // 2026-11-26 (kullanıcı: "suyun kovaya akma efektinde bordera denk gelen yerinde sorun var"): yol tek parça. Üstteki
            // hücreden: aşağı inip tepsi üzerinde boşalma yerine kıvrılır, kenarlığın üstünden dudağa, dudaktan aşağı (kova suyuna düşüş
            // aynı şeritte devam eder). Oyuğun yanındaki alçak hücreden: yatay akıp kova ağzının üstünden düşer.
            var pts = new List<Vector3>(40);
            float rimTop = _drain.y + TrayArt.Reach * _pitch + _pitch * 0.04f;
            if (from.y > rimTop + _pitch * 0.1f)
            {
                var lip = new Vector3(_drain.x, rimTop, 0f);
                var ctrl = new Vector3(from.x, Mathf.Lerp(from.y, lip.y, 0.8f), 0f);
                const int N = 22;
                for (int i = 0; i <= N; i++)
                {
                    float t = i / (float)N;
                    pts.Add((1 - t) * (1 - t) * from + 2 * (1 - t) * t * ctrl + t * t * lip);
                }
                // Dudak: kenarlığın üstünden hafif ileri taşıp aşağı kıvrılır.
                for (int i = 1; i <= 6; i++)
                {
                    float t = i / 6f;
                    float bulge = Mathf.Sin(t * Mathf.PI) * _pitch * 0.04f;
                    pts.Add(new Vector3(_drain.x + bulge, Mathf.Lerp(rimTop, _drain.y - _pitch * 0.06f, t), 0f));
                }
            }
            else
            {
                float mouth = _bucketS * WaterArt.BucketHalfMouth * 0.5f;
                var end = new Vector3(_bucketX + Mathf.Sign(from.x - _bucketX) * mouth, _bucketBottom + _bucketS * WaterArt.BucketMouth + _pitch * 0.08f, 0f);
                var ctrl = new Vector3(end.x, from.y, 0f);
                const int N = 22;
                for (int i = 0; i <= N; i++)
                {
                    float t = i / (float)N;
                    pts.Add((1 - t) * (1 - t) * from + 2 * (1 - t) * t * ctrl + t * t * end);
                }
            }
            var path = pts.ToArray();
            var arc = new float[path.Length];
            for (int i = 1; i < path.Length; i++) arc[i] = arc[i - 1] + (path[i] - path[i - 1]).magnitude;
            _pours.Add(new Pour { Path = path, Arc = arc, Length = arc[arc.Length - 1], Units = units, Width = _pitch * (0.2f + 0.05f * units) }); // 2026-11-26: ince su ipliği (kalın boru değil)
        }

        private float PourDist(float age) => Mathf.Max(0f, PourV0 * _pitch * age + 0.5f * PourAccel * _pitch * age * age);
        private float TailDelay(Pour p) => 0.45f + 0.12f * p.Units;

        /// <summary>Toplam yol: sabit yol + son noktadan kova suyuna düşüş.</summary>
        private float TotalLen(Pour p)
        {
            var o = p.Path[p.Path.Length - 1];
            return p.Length + Mathf.Max(0f, o.y - SurfaceAt(o.x));
        }

        private Vector3 PointAt(Pour p, float s)
        {
            if (s <= p.Length) return PathAt(p, s);
            var o = p.Path[p.Path.Length - 1];
            return new Vector3(o.x, o.y - (s - p.Length), 0f);
        }

        private Vector3 PathAt(Pour p, float s)
        {
            s = Mathf.Clamp(s, 0f, p.Length);
            int i = 1;
            while (i < p.Arc.Length - 1 && p.Arc[i] < s) i++;
            float seg = Mathf.Max(1e-5f, p.Arc[i] - p.Arc[i - 1]);
            return Vector3.Lerp(p.Path[i - 1], p.Path[i], (s - p.Arc[i - 1]) / seg);
        }

        private void Land(int units, float x)
        {
            // 2026-11-26 su kovası: dolu kovaya gelen su taşar (ağızdan köpük ve sıçrama).
            if (_waterHave + units > _waterNeed + 0.01f && !KatKat.UI.Motion.Reduced)
            {
                float top = _bucketBottom + _bucketS * WaterArt.BucketMouth;
                for (int i = 0; i < 10; i++)
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    SpawnFoam(new Vector3(_bucketX + side * _bucketS * WaterArt.BucketHalfMouth * 0.9f, top), new Vector3(side * UnityEngine.Random.Range(0.6f, 1.6f), UnityEngine.Random.Range(0.2f, 1.4f)) * _pitch, 0.08f);
                }
            }
            _waterHave = Mathf.Min(_waterNeed, _waterHave + units);
            _waterSurge = Mathf.Min(1.8f, _waterSurge + 0.6f + 0.25f * units);
            _waterVel += 0.6f * units;
            _ripples.Add(new Ripple { X = x, Age = 0f, Amp = 0.10f + 0.04f * units });
            if (_ripples.Count > 8) _ripples.RemoveAt(0);
            float s = SurfaceAt(x);
            if (!KatKat.UI.Motion.Reduced)
            {
                for (int i = 0; i < 9 + units * 4; i++)
                {
                    float a = UnityEngine.Random.Range(0.15f, 0.85f) * Mathf.PI;
                    SpawnFoam(new Vector3(x, s), new Vector3(Mathf.Cos(a) * UnityEngine.Random.Range(0.8f, 2.4f), Mathf.Sin(a) * UnityEngine.Random.Range(2f, 3.8f)) * _pitch, 0.1f);
                }
                for (int i = 0; i < 3 + units * 2; i++) SpawnBubble(new Vector3(x + UnityEngine.Random.Range(-0.3f, 0.3f) * _pitch, s - UnityEngine.Random.Range(0.15f, 0.6f) * _pitch), true);
            }
            WaterArrived?.Invoke(units);
        }

        // ---- Çizim --------------------------------------------------------------------------------------

        private int Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb, Color cc, Color cd)
        {
            int i = _wv.Count;
            _wv.Add(a); _wv.Add(b); _wv.Add(c); _wv.Add(d);
            _wc.Add(ca); _wc.Add(cb); _wc.Add(cc); _wc.Add(cd);
            _wt.Add(i); _wt.Add(i + 1); _wt.Add(i + 2);
            _wt.Add(i); _wt.Add(i + 2); _wt.Add(i + 3);
            return i;
        }

        /// <summary>
        /// Gerçekçi su şeridi bir çizgi boyunca: yumuşak saydam kenarlar, açık gövde, bir yanda ışık yansıması, akışla kayan parlak
        /// çizgiler; uçlar yuvarlanır. <paramref name="speedAt"/>: noktadaki hız (genişlik ~ 1/√hız).
        /// </summary>
        private void Ribbon(List<Vector3> pts, float width, Func<int, float> speedAt, bool roundHead, bool sharpTail)
        {
            int n = pts.Count;
            if (n < 2) return;
            float k = 1f / Mathf.Max(0.01f, _pitch);
            var pl = new Vector3[5];
            var cl = new Color[5];
            Vector3[] prev = null;
            Color[] prevC = null;
            float along = 0f;
            for (int i = 0; i < n; i++)
            {
                var p = pts[i];
                var dir = (i < n - 1 ? pts[i + 1] - p : p - pts[i - 1]).normalized;
                if (dir.sqrMagnitude < 1e-6f) dir = Vector3.down;
                var nrm = new Vector3(-dir.y, dir.x, 0f);
                if (i > 0) along += (p - pts[i - 1]).magnitude;
                float u = i / (float)(n - 1);
                float w = width * Mathf.Clamp(Mathf.Sqrt(PourV0 * _pitch / Mathf.Max(PourV0 * _pitch, speedAt(i))), 0.6f, 1f);
                if (roundHead) w *= Mathf.Sqrt(Mathf.Clamp01((1f - u) * (n - 1) / 2.5f));
                if (sharpTail) w *= Mathf.Clamp01(u * (n - 1) / 3f);
                // 2026-11-26 (kullanıcı: "Suyun hücreden akması … gerçekçi durmuyor"): su ipliği; kalınlık akışla hafifçe titrer,
                // gövde yarı saydam (altındaki taş seçilir), kenarda koyu kırılma çizgisi, bir yanda dar parlak yansıma, aşağı kayan
                // açık çizgiler akışı gösterir.
                float sway = Mathf.Sin(along * k * 4.2f - _waterClock * 13f) * _pitch * 0.012f;
                w *= 1f + 0.16f * Mathf.Sin(along * k * 9f - _waterClock * 24f) + 0.08f * Mathf.Sin(along * k * 17f - _waterClock * 31f);
                var c = p + nrm * sway;
                float streak = 0.5f + 0.5f * Mathf.Sin(along * k * 6f - _waterClock * 22f);
                var body = Color.Lerp(new Color(0.20f, 0.60f, 1f), new Color(0.62f, 0.92f, 1f), 0.2f + 0.4f * streak);
                var edge = new Color(0.10f, 0.40f, 0.90f, 0f);
                pl[0] = c - nrm * w * 0.5f; cl[0] = edge;
                pl[1] = c - nrm * w * 0.34f; cl[1] = new Color(0.12f, 0.46f, 0.92f, 0.85f);
                pl[2] = c - nrm * w * 0.14f; cl[2] = new Color(0.95f, 1f, 1f, 0.92f);
                pl[3] = c + nrm * w * 0.34f; cl[3] = new Color(body.r, body.g, body.b, 0.72f);
                pl[4] = c + nrm * w * 0.5f; cl[4] = edge;
                if (prev != null)
                    for (int j = 0; j < 4; j++) Quad(prev[j], pl[j], pl[j + 1], prev[j + 1], prevC[j], cl[j], cl[j + 1], prevC[j + 1]);
                prev = (Vector3[])pl.Clone();
                prevC = (Color[])cl.Clone();
            }
        }

        private readonly List<Vector3> _rp = new List<Vector3>(64);

        private void PourRibbons(Pour p)
        {
            float total = TotalLen(p);
            float head = Mathf.Min(PourDist(p.Age), total);
            float tailAge = p.Age - TailDelay(p);
            float tail = PourDist(tailAge);
            if (tail >= total || head <= tail) return;
            _rp.Clear();
            const int S = 30;
            for (int i = 0; i <= S; i++) _rp.Add(PointAt(p, Mathf.Lerp(tail, head, i / (float)S)));
            float age = p.Age;
            Ribbon(_rp, p.Width, i => PourV0 * _pitch + PourAccel * _pitch * Mathf.Max(0f, age - (1f - i / (float)S) * 0.25f), true, tailAge > 0f);
        }

        private void BuildPourMesh()
        {
            _wv.Clear(); _wc.Clear(); _wt.Clear();
            foreach (var p in _pours) PourRibbons(p);
            LeakRibbons();
            SendRibbons();
            FlyStreamRibbon();
            _pourMesh.Clear();
            _pourMesh.SetVertices(_wv);
            _pourMesh.SetColors(_wc);
            _pourMesh.SetTriangles(_wt, 0);
        }

        // ---- Gönderim (2026-11-26, kullanıcı: "kova doldukça gönder diyelim") ----------------------------------------------

        private sealed class SendArc { public Vector3[] Path; public float[] Arc; public float Length, Age, Width; public bool Arrived; public SpriteRenderer R; public Vector3 Prev; }
        private readonly List<SendArc> _sends = new List<SendArc>();
        private Action _jetDone;
        private const float SendHead = 0.75f, SendTail = 0.45f;

        /// <summary>"Gönder" düğmesinin dünya noktası: kovanın gövdesinin önü.</summary>
        // 2026-11-26 (kullanıcı: "Kovanın üzerinde gönder yazdığı için çatlak görünmüyor. Gönderi kovanın altına konumlandıralım").
        /// <summary>Kovanın dünya alanı: alt-sol ve üst-sağ köşe (kovaya dokununca gönderilir; 2026-11-26).</summary>
        public Vector3 WaterBucketMinWorld => transform.TransformPoint(new Vector3(_bucketX - _bucketS * WaterArt.BucketHalfMouth, _bucketBottom + _bucketS * WaterArt.BucketBottom, 0f));
        public Vector3 WaterBucketMaxWorld => transform.TransformPoint(new Vector3(_bucketX + _bucketS * WaterArt.BucketHalfMouth, _bucketBottom + _bucketS * WaterArt.BucketMouth, 0f));

        public Vector3 WaterSendAnchorWorld => transform.TransformPoint(new Vector3(_bucketX, _bucketBottom + _bucketS * WaterArt.BucketBottom - _pitch * 0.32f, 0f));

        /// <summary>
        /// Kovadaki su gönderilir: kova çalkalanarak boşalır, su ağızdan kalın bir şerit halinde fışkırıp yay çizerek görev kartına akar
        /// (aynı gerçekçi su şeridi). Baş vardığında <paramref name="onArrive"/> (kart dolar).
        /// </summary>
        public void SendWater(Vector3 targetWorld, Action onArrive)
        {
            if (!_waterOn) { onArrive?.Invoke(); return; }
            float amount = Mathf.Clamp01(_waterHave / Mathf.Max(1f, _waterNeed));
            if (_jetDone != null) { var prev = _jetDone; _jetDone = null; prev(); }
            if (_flyT >= 0f) EndFly();
            _jetDone = onArrive;
            if (KatKat.UI.Motion.Reduced) { _waterHave = 0f; var d = _jetDone; _jetDone = null; d?.Invoke(); return; }
            var to = transform.InverseTransformPoint(targetWorld);
            to.z = 0f;
            if (FlySend) { StartFly(to); return; }
            _waterHave = 0f;
            _waterSurge = Mathf.Min(1.8f, _waterSurge + 1f);
            var from = new Vector3(_bucketX, _bucketBottom + _bucketS * WaterArt.BucketMouth - _pitch * 0.05f, 0f);
            // 2026-11-26 (kullanıcı: "Kovayı gönder dediğimizde görev çubuğunun altından doluyor su. Üstünden ve çubuğun dolu olduğu
            // uçtan dolmalı"): su kovadan yükselir, kartın üstünden döner ve dolu uca yukarıdan iner (üçüncü derece eğri; son teğet aşağı).
            float side = Mathf.Sign(to.x - from.x + 0.01f);
            var c1 = new Vector3(from.x - side * _pitch * 1.2f, from.y + _pitch * 3f, 0f);
            var c2 = new Vector3(to.x - side * _pitch * 0.6f, to.y + _pitch * 3.2f, 0f);
            const int N = 48;
            var path = new Vector3[N + 1];
            var arc = new float[N + 1];
            for (int i = 0; i <= N; i++)
            {
                float t = i / (float)N, mt = 1f - t;
                path[i] = mt * mt * mt * from + 3f * mt * mt * t * c1 + 3f * mt * t * t * c2 + t * t * t * to;
                if (i > 0) arc[i] = arc[i - 1] + (path[i] - path[i - 1]).magnitude;
            }
            // 2026-11-26 (kullanıcı: "kovadan görev çubuğuna giden suda da sorun var gerçekçi durmuyor"): boru değil, sallanan su topu.
            var go = new GameObject("WaterBlob");
            go.transform.SetParent(transform, false);
            var blob = go.AddComponent<SpriteRenderer>();
            blob.sprite = WaterArt.Droplet;
            blob.sortingOrder = 10 + _height * 4 + 9;
            go.transform.localPosition = from;
            go.transform.localScale = Vector3.zero;
            _sends.Add(new SendArc { Path = path, Arc = arc, Length = arc[N], Width = _pitch * (0.7f + 0.45f * amount), R = blob, Prev = from });
            for (int i = 0; i < 14; i++)
            {
                float a = UnityEngine.Random.Range(0.2f, 0.8f) * Mathf.PI;
                SpawnFoam(from, new Vector3(Mathf.Cos(a) * UnityEngine.Random.Range(0.6f, 1.8f), Mathf.Sin(a) * UnityEngine.Random.Range(1.5f, 3.2f)) * _pitch, 0.1f);
            }
        }

        /// <summary>Gönderimde kova uçar (kapalıysa eski su topu).</summary>
        public static bool FlySend = true;

        private void StartFly(Vector3 to)
        {
            _flyTo = to;
            _flySide = Mathf.Sign(to.x - _bucketX + 0.001f);
            _flyStartHave = _waterHave;
            _flyArrived = false;
            _flyT = 0f;
            _ripples.Clear();
            SetFlySorting(true);
            foreach (var c in _cracks) if (c.R != null) c.R.enabled = false;
            if (!KatKat.UI.Motion.Reduced)
                for (int i = 0; i < 10; i++)
                {
                    float a = UnityEngine.Random.Range(0.2f, 0.8f) * Mathf.PI;
                    SpawnFoam(BucketToBoard(0f, _bucketS * WaterArt.BucketMouth), new Vector3(Mathf.Cos(a) * UnityEngine.Random.Range(0.5f, 1.4f), Mathf.Sin(a) * UnityEngine.Random.Range(1f, 2.2f)) * _pitch, 0.08f);
                }
        }

        private static float FlyEase(float u) { u = Mathf.Clamp01(u); return u * u * (3f - 2f * u); }

        private static Vector3 Bez(Vector3 a, Vector3 b, Vector3 c, float u) { float m = 1f - u; return m * m * a + 2f * m * u * b + u * u * c; }

        /// <summary>Kova ağzının döküm dudağı (kova yereli, boysuz): karta bakan kenar.</summary>
        private Vector3 FlyLip => new Vector3(_flySide * WaterArt.BucketHalfMouth * _bucketS, WaterArt.BucketMouth * _bucketS, 0f);

        /// <summary>Döküm sırasında kovanın ötelemesi: eğik kovanın dudağı kartın dolu ucunun biraz üstünde.</summary>
        private Vector3 FlyHover()
        {
            var lip = Quaternion.Euler(0f, 0f, -_flySide * FlyAngle) * (FlyLip * FlySize);
            return _flyTo + new Vector3(0f, _pitch * 0.5f, 0f) - lip - new Vector3(_bucketX, _bucketBottom, 0f);
        }

        private float FlyDrainStart => FlyLift + FlyGo + FlyTilt * 0.45f;
        private float FlyDrainEnd => FlyLift + FlyGo + FlyTilt + FlyPour;

        private void TickFly(float dt)
        {
            if (_flyT < 0f) return;
            _flyT += dt;
            float t = _flyT;
            float a0 = FlyLift, a1 = a0 + FlyGo, a2 = a1 + FlyTilt, a3 = a2 + FlyPour, a4 = a3 + FlyBack;
            var lift = new Vector3(0f, _pitch * 0.45f, 0f);
            var hover = FlyHover();
            float lean = -_flySide * 16f, angEnd = -_flySide * FlyAngle;
            if (t < a0)
            {
                float u = t / a0;
                _flyOff = lift * FlyEase(u);
                _flyAng = Mathf.Sin(u * Mathf.PI * 2f) * 4f;
                _flyScale = 1f + 0.06f * Mathf.Sin(u * Mathf.PI);
            }
            else if (t < a1)
            {
                float u = FlyEase((t - a0) / FlyGo);
                var ctrl = new Vector3(Mathf.Lerp(lift.x, hover.x, 0.25f), Mathf.Max(lift.y, hover.y) + _pitch * 1.6f, 0f);
                _flyOff = Bez(lift, ctrl, hover, u);
                _flyAng = Mathf.Lerp(0f, lean, u) + Mathf.Sin(u * Mathf.PI) * _flySide * -6f;
                _flyScale = Mathf.Lerp(1f, FlySize, u);
            }
            else if (t < a3)
            {
                float u = FlyEase((t - a1) / FlyTilt);
                float shake = t > a2 ? Mathf.Sin(t * 38f) * 1.6f * (1f - (t - a2) / FlyPour) : 0f;
                _flyOff = hover + new Vector3(0f, Mathf.Sin(Mathf.Min(1f, (t - a1) / FlyTilt) * Mathf.PI) * _pitch * 0.08f, 0f);
                _flyAng = Mathf.Lerp(lean, angEnd, u) + shake;
                _flyScale = FlySize;
            }
            else if (t < a4)
            {
                float u = FlyEase((t - a3) / FlyBack);
                var ctrl = new Vector3(Mathf.Lerp(hover.x, 0f, 0.6f), hover.y + _pitch * 0.8f, 0f);
                _flyOff = Bez(hover, ctrl, Vector3.zero, u);
                _flyAng = Mathf.Lerp(angEnd, 0f, FlyEase(Mathf.Min(1f, u * 1.5f)));
                _flyScale = Mathf.Lerp(FlySize, 1f, u);
            }
            // Boşalma: dudak aşağı bakınca su akar; kovadaki su azalır, kart akış başlayınca dolmaya başlar.
            if (t >= FlyDrainStart)
            {
                float u = Mathf.Clamp01((t - FlyDrainStart) / (FlyDrainEnd - FlyDrainStart));
                float left = _flyStartHave * (1f - u * u * 0.4f - u * 0.6f);
                _waterHave = Mathf.Min(_waterHave, Mathf.Max(0f, left));
                _waterShown = Mathf.Min(_waterShown, _waterHave + 0.2f);
                if (!_flyArrived && t >= FlyDrainStart + 0.14f)
                {
                    _flyArrived = true;
                    if (!KatKat.UI.Motion.Reduced)
                        for (int k = 0; k < 14; k++)
                        {
                            float a = UnityEngine.Random.Range(0.1f, 0.9f) * Mathf.PI;
                            SpawnFoam(_flyTo, new Vector3(Mathf.Cos(a) * UnityEngine.Random.Range(0.8f, 2.2f), Mathf.Sin(a) * UnityEngine.Random.Range(1.0f, 2.6f)) * _pitch, 0.1f);
                        }
                    var d = _jetDone; _jetDone = null; d?.Invoke();
                }
                else if (_flyArrived && t < FlyDrainEnd && !KatKat.UI.Motion.Reduced && UnityEngine.Random.value < dt * 30f)
                {
                    float a = UnityEngine.Random.Range(0.15f, 0.85f) * Mathf.PI;
                    SpawnFoam(_flyTo, new Vector3(Mathf.Cos(a) * UnityEngine.Random.Range(0.6f, 1.6f), Mathf.Sin(a) * UnityEngine.Random.Range(0.8f, 2f)) * _pitch, 0.07f);
                }
            }
            if (t >= a4) EndFly();
            else ApplyFly();
        }

        /// <summary>Uçuş biter (ya da kesilir): kova oyuğunda, boş; kart dolmadıysa doldurulur.</summary>
        private void EndFly()
        {
            if (_flyT < 0f) return;
            _flyT = -1f;
            _flyOff = Vector3.zero;
            _flyAng = 0f;
            _flyScale = 1f;
            _waterHave = Mathf.Min(_waterHave, Mathf.Max(0f, _waterHave - _flyStartHave));
            if (_waterHave < 0.01f) { _waterHave = 0f; _waterShown = 0f; _waterVel = 0f; }
            SetFlySorting(false);
            foreach (var c in _cracks) if (c.R != null) c.R.enabled = true;
            ApplyFly();
            if (!_flyArrived) { _flyArrived = true; var d = _jetDone; _jetDone = null; d?.Invoke(); }
        }

        /// <summary>Dökülen su: eğik kovanın dudağından karta inen kalın şerit (yerçekimiyle kıvrılır).</summary>
        private void FlyStreamRibbon()
        {
            if (_flyT < FlyDrainStart || _flyT > FlyDrainEnd + 0.18f) return;
            float head = Mathf.Clamp01((_flyT - FlyDrainStart) / 0.12f);
            float tail = Mathf.Clamp01((_flyT - FlyDrainEnd) / 0.18f);
            if (head <= tail) return;
            var lip = BucketToBoard(FlyLip.x, FlyLip.y);
            var to = _flyTo;
            float flow = Mathf.Clamp01(1f - Mathf.Clamp01((_flyT - FlyDrainStart) / (FlyDrainEnd - FlyDrainStart))) * 0.7f + 0.3f;
            _rp.Clear();
            const int S = 16;
            var ctrl = new Vector3(Mathf.Lerp(lip.x, to.x, 0.15f) + _flySide * _pitch * 0.12f, lip.y, 0f);
            for (int i = 0; i <= S; i++)
            {
                float u = Mathf.Lerp(tail, head, i / (float)S);
                _rp.Add(Bez(lip, ctrl, to, u));
            }
            Ribbon(_rp, _pitch * (0.16f + 0.26f * flow * Mathf.Clamp01(_flyStartHave / Mathf.Max(1f, _waterNeed) + 0.3f)), i => PourV0 * _pitch * 1.6f, true, tail > 0f);
        }

        private Vector3 SendAt(SendArc p, float s)
        {
            s = Mathf.Clamp(s, 0f, p.Length);
            int i = 1;
            while (i < p.Arc.Length - 1 && p.Arc[i] < s) i++;
            float seg = Mathf.Max(1e-5f, p.Arc[i] - p.Arc[i - 1]);
            return Vector3.Lerp(p.Path[i - 1], p.Path[i], (s - p.Arc[i - 1]) / seg);
        }

        private void TickJets(float dt)
        {
            for (int i = _sends.Count - 1; i >= 0; i--)
            {
                var p = _sends[i];
                p.Age += dt;
                float u = Mathf.Clamp01(p.Age / SendHead);
                float e = u * u * (3f - 2f * u);
                var pos = SendAt(p, p.Length * e);
                if (p.R != null && !p.Arrived)
                {
                    // Uçan su topu: hız yönünde uzar, yana basılır; kendi içinde jöle gibi salınır; arkasında damla izi.
                    var vel = (pos - p.Prev) / Mathf.Max(1e-4f, dt);
                    float sp = vel.magnitude;
                    float stretch = 1f + Mathf.Clamp(sp / (_pitch * 14f), 0f, 0.6f);
                    float wob = 1f + 0.08f * Mathf.Sin(p.Age * 32f);
                    float grow = Mathf.Clamp01(p.Age / 0.12f);
                    float size = p.Width * grow / Mathf.Max(0.01f, p.R.sprite.bounds.size.x);
                    p.R.transform.localPosition = pos;
                    p.R.transform.localScale = new Vector3(size * stretch * wob, size / Mathf.Sqrt(stretch) / wob, 1f);
                    if (sp > 1e-4f) p.R.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg);
                    if (!KatKat.UI.Motion.Reduced && sp > 1e-4f && UnityEngine.Random.value < dt * 45f)
                        SpawnFoam(pos - vel.normalized * p.Width * 0.3f, -vel * 0.08f + new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), UnityEngine.Random.Range(-0.2f, 0.6f)) * _pitch, 0.09f);
                }
                p.Prev = pos;
                if (!p.Arrived && p.Age >= SendHead)
                {
                    p.Arrived = true;
                    var end = p.Path[p.Path.Length - 1];
                    if (p.R != null) Destroy(p.R.gameObject);
                    for (int k = 0; k < 16; k++)
                    {
                        float a = UnityEngine.Random.Range(0.1f, 0.9f) * Mathf.PI;
                        SpawnFoam(end, new Vector3(Mathf.Cos(a) * UnityEngine.Random.Range(0.8f, 2.4f), Mathf.Sin(a) * UnityEngine.Random.Range(1.2f, 3.2f)) * _pitch, 0.11f);
                    }
                    var d = _jetDone; _jetDone = null; d?.Invoke();
                }
                if (p.Age >= SendHead + 0.05f) { if (p.R != null) Destroy(p.R.gameObject); _sends.RemoveAt(i); }
            }
        }

        private void SendRibbons()
        {
            if (_sends.Count >= 0) return; // 2026-11-26: gönderim su topu (TickJets); şerit yok
            foreach (var p in _sends)
            {
                float u = Mathf.Clamp01(p.Age / SendHead);
                float head = p.Length * (1f - (1f - u) * (1f - u)); // fışkırır, sonra yavaşlar
                float v = Mathf.Clamp01((p.Age - SendTail) / SendHead);
                float tail = p.Length * v * v;
                if (head <= tail) continue;
                _rp.Clear();
                const int S = 30;
                for (int i = 0; i <= S; i++) _rp.Add(SendAt(p, Mathf.Lerp(tail, head, i / (float)S)));
                Ribbon(_rp, p.Width, i => PourV0 * _pitch * 2f, true, p.Age > SendTail);
            }
        }

        // ---- Kabarcık ve köpük ------------------------------------------------------------------------

        private void SpawnBubble(Vector3 p, bool burst)
        {
            if (_bubbles.Count > 90) return;
            var go = new GameObject("WaterBubble");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WaterArt.Bubble;
            sr.sortingOrder = 7; // kova suyunun içinde, ön camın arkasında
            _bubbles.Add(new Bubble { R = sr, P = p, Vy = _pitch * UnityEngine.Random.Range(0.8f, burst ? 1.8f : 1.2f), Size = _pitch * UnityEngine.Random.Range(0.05f, burst ? 0.14f : 0.09f), Life = 6f, Phase = UnityEngine.Random.value * 6f });
        }

        private void SpawnFoam(Vector3 p, Vector3 v, float size)
        {
            if (_bubbles.Count > 90) return;
            var go = new GameObject("WaterFoam");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WaterArt.Droplet; // 2026-11-26: ışık lekesi değil gerçek su damlası
            sr.sortingOrder = _flyT >= 0f ? FlyOrder + 4 : 10 + _height * 4 + 7;
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
                if (b.Foam)
                {
                    b.V.y -= g * dt;
                    b.P += b.V * dt;
                    float a = Mathf.Clamp01(1f - b.Age / b.Life);
                    b.R.color = new Color(1f, 1f, 1f, Mathf.Clamp01(a * 1.4f));
                    b.R.transform.localScale = Vector3.one * (b.Size * (0.6f + 0.4f * a) / Mathf.Max(0.01f, b.R.sprite.bounds.size.x));
                }
                else
                {
                    b.P.y += b.Vy * dt;
                    b.P.x += Mathf.Sin(b.Age * 7f + b.Phase) * _pitch * 0.18f * dt;
                    if (b.P.y >= SurfaceAt(b.P.x)) b.Age = b.Life;
                    b.R.color = new Color(1f, 1f, 1f, Mathf.Clamp01(b.Age / 0.15f) * 0.85f);
                    b.R.transform.localScale = Vector3.one * (b.Size * (1f + 0.15f * Mathf.Sin(b.Age * 11f)) / Mathf.Max(0.01f, b.R.sprite.bounds.size.x));
                }
                b.R.transform.localPosition = b.P;
                if (b.Age >= b.Life)
                {
                    Destroy(b.R.gameObject);
                    _bubbles.RemoveAt(i);
                }
            }
        }

        // ---- Kare --------------------------------------------------------------------------------------

        private void TickWater(float dt)
        {
            if (!_waterOn) return;
            // Tahta ölçüsü sonradan (ilk yerleşim, ekran boyu) değişirse kova yeniden oturur (ölçüsüz yerleşimde boy 0 kalıyordu).
            if (_waterBoard != null && (Mathf.Abs(_placedPitch - _pitch) > 1e-5f || _placedLayers != _maxLayers || (PlaceKey() - _placedKey).sqrMagnitude > 1e-8f || _placedBoard != _waterBoard.Width * 1000 + _waterBoard.Height)) PlaceBucket(_waterBoard);
            if (_bucketS <= 0f) return;
            _waterClock += dt;
            TickTilt(dt);
            float g = 26f * _pitch;
            for (int i = _pours.Count - 1; i >= 0; i--)
            {
                var p = _pours[i];
                p.Age += dt;
                float total = TotalLen(p);
                var o = p.Path[p.Path.Length - 1];
                float surf = SurfaceAt(o.x);
                if (!p.Landed && PourDist(p.Age) >= total) { p.Landed = true; Land(p.Units, o.x); }
                // Tazyik: düştüğü yerde sürekli köpük ve sıçrama tacı.
                if (!KatKat.UI.Motion.Reduced && UnityEngine.Random.value < dt * 10f)
                {
                    // İplikten kopan damlacık: yol üzerinde rastgele bir noktadan yana savrulur, düşer.
                    float hd = Mathf.Min(PourDist(p.Age), total), tl = PourDist(p.Age - TailDelay(p));
                    if (hd > tl) SpawnFoam(PointAt(p, UnityEngine.Random.Range(tl, hd)), new Vector3(UnityEngine.Random.Range(-0.9f, 0.9f), UnityEngine.Random.Range(-0.4f, 0.6f)) * _pitch, 0.05f);
                }
                if (p.Landed && !KatKat.UI.Motion.Reduced && UnityEngine.Random.value < dt * 40f)
                {
                    float a = UnityEngine.Random.Range(0.15f, 0.85f) * Mathf.PI;
                    SpawnFoam(new Vector3(o.x, surf), new Vector3(Mathf.Cos(a) * UnityEngine.Random.Range(0.8f, 1.8f), Mathf.Sin(a) * UnityEngine.Random.Range(1.4f, 2.8f)) * _pitch, 0.08f);
                }
                if (p.Landed && PourDist(p.Age - TailDelay(p)) >= total) _pours.RemoveAt(i);
                else if (p.Age > 6f) { if (!p.Landed) Land(p.Units, _bucketX); _pours.RemoveAt(i); }
            }
            if (_waterLogic >= 0)
            {
                _waterIdle += dt;
                if (_pours.Count == 0 || _waterIdle > 3f)
                {
                    if (Mathf.Abs(_waterHave - _waterLogic) > 0.01f) _waterHave = _waterLogic;
                    _waterLogic = -1;
                }
            }
            _waterVel += (_waterHave - _waterShown) * 34f * dt;
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
            // Kova suyunda ara ara dipten kabarcık.
            if (_waterShown > 0.3f && !KatKat.UI.Motion.Reduced && _flyT < 0f)
            {
                _waterBubbleT -= dt;
                if (_waterBubbleT <= 0f)
                {
                    _waterBubbleT = UnityEngine.Random.Range(0.25f, 0.7f);
                    float half = _bucketS * WaterArt.BucketHalfBottom * 0.8f;
                    SpawnBubble(new Vector3(_bucketX + UnityEngine.Random.Range(-half, half), WaterBottomY + _pitch * 0.05f), false);
                }
            }
            TickFly(dt);
            TickCracks(dt);
            TickJets(dt);
            TickBubbles(dt);
            UpdateWaterShader();
            BuildPourMesh();
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
                want = Mathf.Clamp(a.x / Mathf.Max(0.35f, -a.y) * 1.3f, -0.9f, 0.9f);
            _waterTilt = Mathf.Lerp(_waterTilt, want, 1f - Mathf.Exp(-4f * dt));
        }

        /// <summary>Atlama: dökülmeler hemen kovaya varır, kabarcıklar silinir (çatlaklar sürer).</summary>
        private void FinishWater()
        {
            foreach (var p in _pours) if (!p.Landed) { p.Landed = true; Land(p.Units, _bucketX); }
            ClearWaterFx();
            _waterShown = _waterHave;
            _waterVel = 0f;
            if (_waterOn && _pourMesh != null) { UpdateWaterShader(); BuildPourMesh(); }
        }

        private void ClearWaterFx()
        {
            foreach (var p in _sends) if (p.R != null) Destroy(p.R.gameObject);
            _sends.Clear();
            EndFly();
            if (_jetDone != null) { var d = _jetDone; _jetDone = null; d(); }
            _pours.Clear();
            _ripples.Clear();
            foreach (var b in _bubbles) if (b.R != null) Destroy(b.R.gameObject);
            _bubbles.Clear();
        }
    }
}
