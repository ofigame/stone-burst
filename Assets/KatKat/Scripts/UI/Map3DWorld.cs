using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KatKat.UI
{
    /// <summary>
    /// 3D harita denemesi (kullanıcı 2026-11-22: "haritanın 3 boyutlu oyun gibi olmasını istiyorum. Maskotumuzu ilerleterek
    /// bölüm kapısından gireceğiz"; geçici modellerle başla). Bölgenin bölümleri kıvrımlı taş yol üstünde kapılar; yanda
    /// dere, ağaç, kaya, çiçek. Maskot (geçici: koddan şekiller) sıradaki kapıda bekler; açık bir kapıya dokununca yolda
    /// yürüyüp kapıdan girer ve bölüm başlangıç kartı açılır. Ayrı katmanda (30) kendi kamerasıyla RenderTexture'a çizilir;
    /// menüde haritanın yerine gösterilir (üst kart, rozetler, alt menü üstte kalır). Gerçek modeller geldikçe
    /// <see cref="BuildMascot"/>, <see cref="BuildGate"/> ve süsler Resources/Map3D'deki modellerle değişecek.
    /// </summary>
    public sealed partial class Map3DWorld : MonoBehaviour
    {
        public const int Layer = 30;
        public const float GateSpacing = 13f, FirstGateZ = 6f, WalkSpeed = 7.5f, MascotScale = 1.2f;
        private static readonly Vector3 Origin = new Vector3(0f, -3000f, 0f);

        /// <summary>Bir önceki gösterimde maskotun durduğu bölüm (oturum içi; ilerleyince aradaki yolu yürür).</summary>
        private static int _lastShownGate = -1;

        private Camera _cam;
        private RenderTexture _rt;
        private Transform _root, _mascot, _mascotBody;
        private readonly List<Transform> _gates = new List<Transform>();
        private readonly List<Transform> _currentRing = new List<Transform>();
        private readonly Dictionary<Color, Material> _mats = new Dictionary<Color, Material>();
        private Material _base;
        private int _first, _count, _current;
        private Func<int, bool> _isOpen;
        private float _focusZ, _focusVel, _t;
        private float _mascotZ, _walkTo = -1f, _walkT;
        private int _walkGate = -1;
        private bool _entering, _following;
        private float _enterT;
        private Action<int> _onEnter;

        public RenderTexture Texture => _rt;
        public int GateCount => _gates.Count;
        public int FirstIndex => _first;
        public bool Walking => _walkTo >= 0f || _entering || _flying || _landT >= 0f;
        public bool Flying => _flying;
        public float MascotZ => _mascotZ;
        public Camera Camera => _cam;

        public static Map3DWorld Create(Transform parent)
        {
            var go = new GameObject("Map3DWorld");
            go.transform.SetParent(parent, false);
            go.transform.position = Origin;
            var w = go.AddComponent<Map3DWorld>();
            return w;
        }

        /// <summary>
        /// Bölgeyi kurar: <paramref name="first"/>'ten <paramref name="count"/> bölüm. <paramref name="current"/> sıradaki
        /// bölüm (maskot onun kapısında). <paramref name="isCompleted"/> ve <paramref name="starsOf"/> kapı görünümü için.
        /// </summary>
        public void Build(int first, int count, int current, Func<int, bool> isCompleted, Func<int, int> starsOf, Func<int, bool> isOpen, Vector2Int size, Action<int> onEnter)
        {
            Clear();
            _first = first; _count = count; _current = Mathf.Clamp(current, first, first + count - 1);
            _isOpen = isOpen; _onEnter = onEnter;
            _base = Resources.Load<Material>("Map3D/base");
            _root = new GameObject("World").transform;
            _root.SetParent(transform, false);
            SetLayer(_root.gameObject);

            float endZ = GateZ(count - 1) + 9f;
            BuildScenery(endZ); // zemin, dere, bölüm bölüm manzara, sondaki sis bulutu (Map3DWorld.Scenery)
            BuildPath(endZ);
            for (int i = 0; i < count; i++)
            {
                int idx = first + i;
                bool done = isCompleted != null && isCompleted(idx);
                int stars = done && starsOf != null ? starsOf(idx) : 0;
                _gates.Add(BuildGate(i, idx, done, stars, idx == _current, isOpen == null || isOpen(idx)));
            }
            _mascot = BuildMascot();

            // Maskot: önceki gösterimden birkaç kapı ilerlediyse eski kapıdan yürüyerek gelir.
            int from = _lastShownGate >= first && _lastShownGate < _current && _current - _lastShownGate <= 3 ? _lastShownGate : _current;
            // 2026-11-22 (kullanıcı): maskot her zaman iki kapının ortasında bekler (son geçilen ile sıradaki arası).
            _mascotZ = RestZ(from - first);
            if (from != _current) { _walkTo = RestZ(_current - first); _walkGate = -1; _following = true; }
            _lastShownGate = _current;
            PlaceMascot(0f);
            _focusZ = _mascotZ;

            BuildCamera(size);
            BuildLights();
            PlaceCamera();
            _snapTurn = true; // açılışta yön anında (yumuşak dönüş beklenmez)
            Advance(0f);
        }

        public void Clear()
        {
            if (_root != null) Kill(_root.gameObject);
            _root = null;
            _gates.Clear();
            _currentRing.Clear();
            _walkTo = -1f; _entering = false; _following = false; _flying = false; _landT = -1f;
        }

        private void OnDestroy()
        {
            Clear();
            foreach (var m in _mats.Values) Kill(m);
            _mats.Clear();
            if (_rt != null) { _rt.Release(); Kill(_rt); }
        }

        private static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        // ---------------------------------------------------------------- yol eğrisi

        public static float GateZ(int i) => FirstGateZ + i * GateSpacing;

        /// <summary>Maskotun bekleme yeri: kapı slot’unun yarım aralık gerisi (bir önceki kapı ile arasındaki orta).</summary>
        public static float RestZ(int slot) => GateZ(slot) - GateSpacing * 0.5f;

        /// <summary>Kapıya varınca durduğu yer: kapının hemen önü.</summary>
        private const float GateStop = 1.3f;

        /// <summary>Yolun z'deki yatay sapması (iki dalga: yumuşak kıvrım).</summary>
        public static float PathX(float z) => 2.4f * Mathf.Sin(z * 0.16f) + 0.9f * Mathf.Sin(z * 0.053f + 1.3f);

        private static Vector3 PathPoint(float z) => new Vector3(PathX(z), 0f, z);

        private static Vector3 PathTangent(float z)
        {
            var d = PathPoint(z + 0.1f) - PathPoint(z - 0.1f);
            return d.normalized;
        }

        /// <summary>Derenin x'i: yolun sağında, kendi kıvrımıyla.</summary>
        private static float RiverX(float z) => PathX(z) + 6.2f + 1.3f * Mathf.Sin(z * 0.11f + 0.6f);

        // ---------------------------------------------------------------- malzeme ve parça

        private Material Mat(Color c, float gloss = 0.2f)
        {
            var key = new Color(c.r, c.g, c.b, gloss);
            if (_mats.TryGetValue(key, out var m) && m != null) return m;
            m = _base != null ? new Material(_base) : new Material(Shader.Find("Standard"));
            m.color = c;
            m.SetFloat("_Glossiness", gloss);
            m.enableInstancing = true;
            _mats[key] = m;
            return m;
        }

        private Transform Part(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color c, float gloss = 0.2f, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            if (!keepCollider) Kill(go.GetComponent<Collider>());
            go.layer = Layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Mat(c, gloss);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
            return go.transform;
        }

        private static void SetLayer(GameObject go)
        {
            go.layer = Layer;
            foreach (Transform t in go.transform) SetLayer(t.gameObject);
        }

        /// <summary>z boyunca kıvrımlı şerit ağı: merkez x(z), genişlik, yükseklik.</summary>
        private Transform Strip(string name, Func<float, float> centerX, float width, float y, float z0, float z1, Color c, float gloss = 0.15f)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt((z1 - z0) / 0.5f));
            var v = new Vector3[(n + 1) * 2];
            var uv = new Vector2[v.Length];
            var tri = new int[n * 6];
            for (int i = 0; i <= n; i++)
            {
                float z = Mathf.Lerp(z0, z1, i / (float)n);
                float x = centerX(z);
                float dx = (centerX(z + 0.1f) - centerX(z - 0.1f)) / 0.2f;
                var nrm = new Vector3(1f, 0f, -dx).normalized * (width * 0.5f);
                v[i * 2] = new Vector3(x, y, z) - nrm;
                v[i * 2 + 1] = new Vector3(x, y, z) + nrm;
                uv[i * 2] = new Vector2(0f, z * 0.25f);
                uv[i * 2 + 1] = new Vector2(1f, z * 0.25f);
                if (i < n)
                {
                    int k = i * 6, a = i * 2;
                    tri[k] = a; tri[k + 1] = a + 2; tri[k + 2] = a + 1;
                    tri[k + 3] = a + 1; tri[k + 4] = a + 2; tri[k + 5] = a + 3;
                }
            }
            var mesh = new Mesh { name = name, vertices = v, uv = uv, triangles = tri };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = Layer;
            go.transform.SetParent(_root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Mat(c, gloss);
            r.receiveShadows = true;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        // ---------------------------------------------------------------- sahne

        private void BuildPath(float endZ)
        {
            Strip("PathEdge", PathX, 3.1f, 0.0f, -6f, endZ, SandEdge, 0.1f);
            Strip("Path", PathX, 2.6f, 0.012f, -6f, endZ, Sand, 0.1f);
            // Taş döşeme: yol boyunca hafif kabarık yuvarlak taşlar.
            var rng = new System.Random(7);
            for (float z = -4f; z < endZ; z += 1.3f)
            {
                for (int k = -1; k <= 1; k++)
                {
                    if (rng.NextDouble() < 0.55) continue;
                    // Yolun dik ekseninde (kıvrımda yoldan taşmasın; kullanıcı 2026-11-22), yol yönüne döndürülmüş.
                    float zz = z + (float)rng.NextDouble() * 0.4f;
                    var tan = PathTangent(zz);
                    var nrm = new Vector3(tan.z, 0f, -tan.x);
                    var pos = PathPoint(zz) + nrm * (k * 0.72f + (float)(rng.NextDouble() - 0.5) * 0.2f);
                    var p = Part(PrimitiveType.Sphere, _root, pos, new Vector3(0.5f, 0.06f, 0.4f), new Color32(0xD9, 0xA8, 0x66, 0xFF), 0.1f);
                    p.localRotation = Quaternion.LookRotation(tan, Vector3.up);
                    p.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        // ---------------------------------------------------------------- kapı

        private static readonly Color Gold = new Color32(0xFF, 0xC8, 0x3A, 0xFF);

        // Kapı renkleri (kullanıcının görseli, 2026-11-22: "Indirilenler/image.png"): adaçayı yeşili direkler, bal sarısı
        // ahşap kemer ve bilezikler, krem asılı tabela, dipte yeşil yapraklar ve pembe tomurcuklar.
        private static readonly Color GatePillar = new Color32(0x6F, 0xA8, 0x9C, 0xFF);
        private static readonly Color GateArch = new Color32(0xF2, 0xBE, 0x4E, 0xFF);
        private static readonly Color GateCollar = new Color32(0xF5, 0xB8, 0x3A, 0xFF);
        private static readonly Color GateSign = new Color32(0xF6, 0xE6, 0xD2, 0xFF);
        private static readonly Color GateLeaf = new Color32(0x6C, 0xC8, 0x50, 0xFF);
        private static readonly Color GateBud = new Color32(0xF2, 0x8C, 0xB4, 0xFF);
        private static readonly Color LockPillar = new Color32(0x9C, 0xA2, 0xAE, 0xFF);
        private static readonly Color LockArch = new Color32(0xC4, 0xC0, 0xB8, 0xFF);

        private const float GateHalf = 1.55f, PillarH = 1.55f, PillarR = 0.42f, ArchY = 1.72f, ArchTube = 0.3f;

        /// <summary>
        /// Kapı (kullanıcının görselinden koddan modellendi): iki yuvarlak başlı direk, üstlerinde kalın bilezik, aralarında
        /// yarım halka ahşap kemer, kemerden iki iple asılı yuvarlak köşeli tabela (bölüm numarası), dipte yapraklar ve
        /// tomurcuklar. Tamamlanan kapıda kemerin tepesinde yıldızlar; kilitli kapı soluk gri, yapraksız.
        /// </summary>
        private Transform BuildGate(int slot, int index, bool done, int stars, bool current, bool open)
        {
            float z = GateZ(slot);
            var g = new GameObject("Gate" + (index + 1)).transform;
            g.SetParent(_root, false);
            g.localPosition = PathPoint(z);
            g.localRotation = Quaternion.LookRotation(PathTangent(z), Vector3.up);
            g.gameObject.layer = Layer;

            Color pillar = open ? GatePillar : LockPillar, arch = open ? GateArch : LockArch, collar = open ? GateCollar : LockArch;
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * GateHalf;
                // Direk: hafif daralan gövde + yuvarlak dip ve tepe.
                MeshPart("Pillar", g, CachedMesh("pillar", () => Lathe(new[]
                {
                    new Vector2(0f, 0f), new Vector2(0.9f, 0f), new Vector2(1f, 0.08f), new Vector2(0.96f, 0.6f), new Vector2(0.9f, 0.97f), new Vector2(0.75f, 1f), new Vector2(0f, 1f),
                }, 24)), new Vector3(x, 0f, 0f), new Vector3(PillarR * 2f, PillarH, PillarR * 2f), pillar, 0.3f);
                // Bilezik: direğin tepesinde kalın halka.
                MeshPart("Collar", g, CachedMesh("collar", () => Torus(1f, 0.42f, 28, 14, 0f, 360f)), new Vector3(x, PillarH + 0.02f, 0f), new Vector3(0.5f, 0.55f, 0.5f), collar, 0.35f)
                    .localRotation = Quaternion.identity;
                MeshPart("CollarTop", g, CachedMesh("disc", () => Lathe(new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.95f, 0.6f), new Vector2(0.6f, 1f), new Vector2(0f, 1f) }, 24)),
                    new Vector3(x, PillarH - 0.1f, 0f), new Vector3(1.1f, 0.4f, 1.1f), collar, 0.35f);
                if (open)
                {
                    // Dipte yapraklar ve tomurcuklar (kameraya bakan önde).
                    for (int l = 0; l < 3; l++)
                    {
                        float a = (l - 1) * 38f + s * 10f;
                        var leaf = Part(PrimitiveType.Sphere, g, new Vector3(x + (l - 1) * 0.3f, 0.28f, -PillarR - 0.05f), new Vector3(0.24f, 0.55f, 0.12f), GateLeaf * (l == 1 ? 1f : 0.9f), 0.35f);
                        leaf.localRotation = Quaternion.Euler(-15f, 0f, -a);
                    }
                    Part(PrimitiveType.Sphere, g, new Vector3(x - s * 0.42f, 0.52f, -PillarR - 0.02f), Vector3.one * 0.2f, GateBud, 0.5f);
                    Part(PrimitiveType.Sphere, g, new Vector3(x + s * 0.18f, 0.36f, -PillarR - 0.1f), Vector3.one * 0.13f, GateBud, 0.5f);
                }
            }
            // Kemer: iki direğin üstünü bağlayan yarım halka (dünya: kapı düzleminde, x–y).
            MeshPart("Arch", g, CachedMesh("arch", () => Torus(1f, ArchTube / GateHalf, 40, 14, 0f, 180f)), new Vector3(0f, ArchY, 0f), Vector3.one * GateHalf, arch, 0.25f)
                .localRotation = Quaternion.Euler(-90f, 0f, 0f);
            // Asılı tabela: iki ip, yuvarlak köşeli kutu; kameraya doğru hafif eğik.
            float signY = ArchY + 0.75f;
            var hang = new GameObject("Sign").transform;
            hang.SetParent(g, false);
            hang.localPosition = new Vector3(0f, signY, -0.05f);
            hang.localRotation = Quaternion.Euler(18f, 0f, 0f);
            hang.gameObject.layer = Layer;
            for (int s = -1; s <= 1; s += 2)
            {
                var rope = Part(PrimitiveType.Cylinder, hang, new Vector3(s * 0.38f, 0.62f, 0.02f), new Vector3(0.06f, 0.22f, 0.06f), new Color32(0xC9, 0x8A, 0x4E, 0xFF), 0.2f);
                rope.localRotation = Quaternion.Euler(0f, 0f, s * 18f);
            }
            MeshPart("Board", hang, CachedMesh("board", () => RoundedBox(new Vector3(1.4f, 1.0f, 0.24f), 0.16f, 6)), Vector3.zero, Vector3.one, open ? GateSign : new Color32(0xD6, 0xD4, 0xD8, 0xFF), 0.3f);
            var num = new GameObject("Number", typeof(RectTransform)).AddComponent<TextMeshPro>();
            num.gameObject.layer = Layer;
            num.transform.SetParent(hang, false);
            num.rectTransform.sizeDelta = new Vector2(1.25f, 0.85f);
            num.transform.localPosition = new Vector3(0f, 0f, -0.18f); // tabelanın ön yüzü -0,16’da (köşe yarıçapı kalınlıktan büyük)
            num.font = UiTypography.Font;
            num.text = open ? (index + 1).ToString() : ""; // açılmamış bölümde numara yok (kullanıcı 2026-11-22)
            num.fontSize = 6f;
            num.enableAutoSizing = true; num.fontSizeMin = 2f; num.fontSizeMax = 6f;
            num.alignment = TextAlignmentOptions.Center;
            num.color = !open ? new Color32(0x8E, 0x8B, 0x99, 0xFF) : new Color32(0x0B, 0x2F, 0x6E, 0xFF);
            // Yıldızlar: kemerin tepesinde (kazanılmamış gri).
            if (done)
                for (int s = 0; s < 3; s++)
                    Part(PrimitiveType.Sphere, g, new Vector3((s - 1) * 0.5f, ArchY + GateHalf + ArchTube + 0.18f + (s == 1 ? 0.12f : 0f), 0f), Vector3.one * 0.34f, s < stars ? Gold : new Color32(0x9A, 0x96, 0xA4, 0xFF), 0.6f);
            // Sıradaki kapı: yerde parlayan halka (nabız Advance'te).
            if (current)
            {
                var ring = Part(PrimitiveType.Cylinder, g, new Vector3(0f, 0.03f, 0f), new Vector3(3f, 0.01f, 3f), new Color(1f, 0.82f, 0.25f, 1f), 0.6f);
                ring.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _currentRing.Add(ring);
            }
            // Dokunma alanı: kapının tamamı.
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 2f, 0f);
            col.size = new Vector3(4.2f, 4.2f, 2.4f);
            return g;
        }

        // ---------------------------------------------------------------- örgü üreticiler

        private static readonly Dictionary<string, Mesh> _meshCache = new Dictionary<string, Mesh>();

        private static Mesh CachedMesh(string key, Func<Mesh> make)
        {
            if (_meshCache.TryGetValue(key, out var m) && m != null) return m;
            m = make();
            m.name = "map3d_" + key;
            m.hideFlags = HideFlags.DontSave;
            _meshCache[key] = m;
            return m;
        }

        private Transform MeshPart(string name, Transform parent, Mesh mesh, Vector3 pos, Vector3 scale, Color c, float gloss)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = Layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Mat(c, gloss);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
            return go.transform;
        }

        /// <summary>Halka (x–z düzleminde, merkez 0): ana yarıçap R, boru r; yay a0→a1 derece (yarım kemer 0→180).</summary>
        private static Mesh Torus(float R, float r, int segU, int segV, float a0, float a1)
        {
            bool closed = Mathf.Approximately(a1 - a0, 360f);
            int nu = segU + 1, nv = segV + 1;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var tri = new List<int>();
            for (int i = 0; i < nu; i++)
            {
                float u = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, i / (float)segU);
                var c = new Vector3(Mathf.Cos(u), 0f, Mathf.Sin(u));
                for (int j = 0; j < nv; j++)
                {
                    float w = j / (float)segV * Mathf.PI * 2f;
                    var d = c * Mathf.Cos(w) + Vector3.up * Mathf.Sin(w);
                    v.Add(c * R + d * r);
                    n.Add(d);
                }
            }
            for (int i = 0; i < segU; i++)
                for (int j = 0; j < segV; j++)
                {
                    int a = i * nv + j, b = (i + 1) * nv + j;
                    tri.Add(a); tri.Add(a + 1); tri.Add(b);
                    tri.Add(b); tri.Add(a + 1); tri.Add(b + 1);
                }
            if (!closed)
            {
                // Uç kapakları (kemer direğin bileziğine oturur; görünmese de delik kalmasın).
                for (int e = 0; e < 2; e++)
                {
                    float u = Mathf.Deg2Rad * (e == 0 ? a0 : a1);
                    var c = new Vector3(Mathf.Cos(u), 0f, Mathf.Sin(u));
                    var t = new Vector3(-Mathf.Sin(u), 0f, Mathf.Cos(u)) * (e == 0 ? -1f : 1f);
                    int centre = v.Count;
                    v.Add(c * R); n.Add(t);
                    for (int j = 0; j < nv; j++)
                    {
                        float w = j / (float)segV * Mathf.PI * 2f;
                        v.Add(c * R + (c * Mathf.Cos(w) + Vector3.up * Mathf.Sin(w)) * r); n.Add(t);
                    }
                    for (int j = 0; j < segV; j++)
                    {
                        if (e == 0) { tri.Add(centre); tri.Add(centre + 1 + j); tri.Add(centre + 2 + j); }
                        else { tri.Add(centre); tri.Add(centre + 2 + j); tri.Add(centre + 1 + j); }
                    }
                }
            }
            var m = new Mesh();
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Dönel gövde: profil (x yarıçap, y yükseklik, 0–1) y ekseni etrafında; yumuşak normaller.</summary>
        private static Mesh Lathe(Vector2[] profile, int seg)
        {
            var v = new List<Vector3>();
            var tri = new List<int>();
            int np = profile.Length;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float cs = Mathf.Cos(a) * 0.5f, sn = Mathf.Sin(a) * 0.5f;
                foreach (var p in profile) v.Add(new Vector3(p.x * cs, p.y, p.x * sn));
            }
            for (int i = 0; i < seg; i++)
                for (int k = 0; k < np - 1; k++)
                {
                    int a = i * np + k, b = (i + 1) * np + k;
                    tri.Add(a); tri.Add(a + 1); tri.Add(b);
                    tri.Add(a + 1); tri.Add(b + 1); tri.Add(b);
                }
            var m = new Mesh();
            m.SetVertices(v); m.SetTriangles(tri, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Yuvarlak köşeli kutu (boyut, köşe yarıçapı): alt bölümlenmiş küpün köşeleri yuvarlatılır.</summary>
        private static Mesh RoundedBox(Vector3 size, float radius, int div)
        {
            var h = size * 0.5f;
            var inner = new Vector3(Mathf.Max(0f, h.x - radius), Mathf.Max(0f, h.y - radius), Mathf.Max(0f, h.z - radius));
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var tri = new List<int>();
            Vector3[] fn = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var f in fn)
            {
                var a = new Vector3(f.y, f.z, f.x);
                var b = Vector3.Cross(f, a);
                int start = v.Count;
                for (int i = 0; i <= div; i++)
                    for (int j = 0; j <= div; j++)
                    {
                        float s = i / (float)div * 2f - 1f, t = j / (float)div * 2f - 1f;
                        var p = f + a * s + b * t; // birim küp yüzeyi
                        var q = Vector3.Scale(p, h);
                        var c = new Vector3(Mathf.Clamp(q.x, -inner.x, inner.x), Mathf.Clamp(q.y, -inner.y, inner.y), Mathf.Clamp(q.z, -inner.z, inner.z));
                        var d = q - c;
                        var dn = d.sqrMagnitude > 1e-8f ? d.normalized : f;
                        v.Add(c + dn * radius);
                        n.Add(dn);
                    }
                for (int i = 0; i < div; i++)
                    for (int j = 0; j < div; j++)
                    {
                        int k = start + i * (div + 1) + j;
                        tri.Add(k); tri.Add(k + div + 1); tri.Add(k + 1);
                        tri.Add(k + 1); tri.Add(k + div + 1); tri.Add(k + div + 2);
                    }
            }
            var m = new Mesh();
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }

        // ---------------------------------------------------------------- maskot (geçici)

        /// <summary>Geçici maskot: krem yuvarlak blok gövde, turkuaz perçem, mor taban, gözler ve yanaklar.</summary>
        private Transform BuildMascot()
        {
            var m = new GameObject("Mascot").transform;
            m.SetParent(_root, false);
            m.gameObject.layer = Layer;
            var body = new GameObject("Body").transform;
            body.SetParent(m, false);
            body.gameObject.layer = Layer;
            _mascotBody = body;
            if (RealMascot(body)) { Shadow(m); m.localScale = Vector3.one * MascotScale; return m; }
            Part(PrimitiveType.Cylinder, body, new Vector3(0f, 0.16f, 0f), new Vector3(0.95f, 0.13f, 0.95f), new Color32(0x8E, 0x3C, 0xD8, 0xFF), 0.4f);
            Part(PrimitiveType.Cube, body, new Vector3(0f, 0.72f, 0f), new Vector3(1.05f, 0.95f, 0.95f), new Color32(0xFF, 0xEE, 0xE2, 0xFF), 0.35f);
            Part(PrimitiveType.Sphere, body, new Vector3(0f, 0.72f, 0f), new Vector3(1.22f, 1.0f, 1.12f), new Color32(0xFF, 0xEE, 0xE2, 0xFF), 0.35f);
            Part(PrimitiveType.Sphere, body, new Vector3(0.05f, 1.28f, 0.05f), new Vector3(1.05f, 0.42f, 0.95f), new Color32(0x1F, 0xB8, 0xA8, 0xFF), 0.45f);
            Part(PrimitiveType.Sphere, body, new Vector3(0.42f, 1.42f, 0.1f), new Vector3(0.5f, 0.3f, 0.4f), new Color32(0xE8, 0x44, 0x44, 0xFF), 0.45f);
            // Yüz (+z ileri; maskot kameradan uzağa yürür, yürürken arkası görünür, dururken kameraya döner).
            Part(PrimitiveType.Sphere, body, new Vector3(-0.22f, 0.8f, 0.55f), new Vector3(0.2f, 0.24f, 0.1f), new Color32(0x22, 0x22, 0x2A, 0xFF), 0.8f);
            Part(PrimitiveType.Sphere, body, new Vector3(0.22f, 0.8f, 0.55f), new Vector3(0.2f, 0.24f, 0.1f), new Color32(0x22, 0x22, 0x2A, 0xFF), 0.8f);
            Part(PrimitiveType.Sphere, body, new Vector3(-0.38f, 0.6f, 0.52f), new Vector3(0.18f, 0.1f, 0.06f), new Color32(0xFF, 0x9C, 0xA8, 0xFF), 0.2f);
            Part(PrimitiveType.Sphere, body, new Vector3(0.38f, 0.6f, 0.52f), new Vector3(0.18f, 0.1f, 0.06f), new Color32(0xFF, 0x9C, 0xA8, 0xFF), 0.2f);
            Part(PrimitiveType.Sphere, body, new Vector3(0f, 0.55f, 0.57f), new Vector3(0.2f, 0.1f, 0.06f), new Color32(0xD8, 0x3A, 0x4A, 0xFF), 0.3f);
            Shadow(m);
            m.localScale = Vector3.one * MascotScale;
            return m;
        }

        /// <summary>Gölge yerine yerde yumuşak koyu leke (gölge kapalı cihazlarda da yere bassın).</summary>
        private void Shadow(Transform m)
        {
            var blob = Part(PrimitiveType.Cylinder, m, new Vector3(0f, 0.02f, 0f), new Vector3(1.1f, 0.005f, 1.1f), new Color(0.35f, 0.5f, 0.25f, 1f), 0f);
            blob.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Asıl maskot boyu (dünya birimi, ölçek öncesi) ve modelin kendi yüzünün +z’ye dönmesi için açı.</summary>
        public const float MascotHeight = 1.5f, MascotYaw = 0f;

        /// <summary>
        /// Tripo maskotu (kullanıcı 2026-11-22; Resources/Map3D/Mascot: model.fbx en uzun kenar 1, taban 0; color/normal
        /// 1024; mat.mat Standard). İskeletsiz: yürüyüş zıplama/esneme/sallanma koddan. Yoksa geçici şekiller.
        /// </summary>
        private Material _mascotMat;

        /// <summary>Maskot malzemesi: mat.mat kopyası, dokular (color, normal) Resources’tan bağlanır.</summary>
        private Material MascotMaterial()
        {
            if (_mascotMat != null) return _mascotMat;
            var src = Resources.Load<Material>("Map3D/Mascot/mat");
            if (src == null) return null;
            _mascotMat = new Material(src) { name = "mascot_rt" };
            var color = Resources.Load<Texture2D>("Map3D/Mascot/color");
            var normal = Resources.Load<Texture2D>("Map3D/Mascot/normal");
            if (color != null) _mascotMat.mainTexture = color;
            if (normal != null) { _mascotMat.SetTexture("_BumpMap", normal); _mascotMat.EnableKeyword("_NORMALMAP"); }
            _mats[new Color(-1f, -1f, -1f, -1f)] = _mascotMat; // OnDestroy ile silinsin
            return _mascotMat;
        }

        private bool RealMascot(Transform body)
        {
            var prefab = Resources.Load<GameObject>("Map3D/Mascot/model");
            if (prefab == null) return false;
            var go = Instantiate(prefab, body, false);
            go.name = "Model";
            go.transform.localPosition = Vector3.zero;
            // FBX kökünün eksen düzeltmesi (Blender Z-yukarı → -90° X) korunur.
            go.transform.localRotation = Quaternion.Euler(0f, MascotYaw, 0f) * prefab.transform.localRotation;
            go.transform.localScale = prefab.transform.localScale * MascotHeight;
            var mat = MascotMaterial();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (mat != null) r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            SetLayer(go);
            return true;
        }

        private bool _snapTurn;

        private void PlaceMascot(float hop) => PlaceMascot(hop, Vector3.one);

        private void PlaceMascot(float hop, Vector3 squash)
        {
            if (_mascot == null) return;
            var p = PathPoint(_mascotZ);
            _mascot.localPosition = p;
            bool moving = _walkTo >= 0f || _entering || _flying;
            // Yürürken gittiği yöne, dururken kameraya bakar (kullanıcı 2026-11-22: "yan duruyor").
            Vector3 dir;
            if (moving) dir = PathTangent(_mascotZ) * (_walkSign < 0f ? -1f : 1f);
            else if (_cam != null) { dir = _cam.transform.position - _mascot.position; dir.y = 0f; if (dir.sqrMagnitude < 1e-4f) dir = -PathTangent(_mascotZ); }
            else dir = -PathTangent(_mascotZ);
            var want = Quaternion.LookRotation(dir.normalized, Vector3.up);
            _mascot.localRotation = _snapTurn ? want : Quaternion.Slerp(_mascot.localRotation, want, 0.25f);
            _snapTurn = false;
            if (_mascotBody != null)
            {
                _mascotBody.localPosition = new Vector3(0f, hop, 0f);
                float sq = 1f + 0.08f * Mathf.Sin(_t * 6f) * (moving ? 0f : 1f);
                float stretch = moving ? 1f + hop * 0.5f : sq;
                var sc = new Vector3(1f / Mathf.Sqrt(stretch), stretch, 1f / Mathf.Sqrt(stretch));
                if (_flying || _landT >= 0f) sc = squash;
                _mascotBody.localScale = sc;
                _mascotBody.localRotation = Quaternion.Euler(0f, 0f, moving && !_flying ? 6f * Mathf.Sin(_walkT * 12f) : 0f);
            }
        }

        // ---------------------------------------------------------------- kamera ve ışık

        private void BuildCamera(Vector2Int size)
        {
            int w = Mathf.Clamp(size.x, 64, 1440), h = Mathf.Clamp(size.y, 64, 2560);
            if (_rt == null || _rt.width != w || _rt.height != h)
            {
                if (_rt != null) { _rt.Release(); Kill(_rt); }
                _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "map3d_rt", antiAliasing = 4 };
                _rt.Create();
            }
            if (_cam == null)
            {
                var go = new GameObject("Map3DCamera");
                go.transform.SetParent(transform, false);
                _cam = go.AddComponent<Camera>();
            }
            _cam.targetTexture = _rt;
            _cam.cullingMask = 1 << Layer;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color32(0x2A, 0x22, 0x66, 0xFF);
            _cam.fieldOfView = 42f;
            _cam.nearClipPlane = 0.5f;
            _cam.farClipPlane = 260f;
            _cam.depth = -50f;
            _cam.aspect = (float)w / h;
        }

        private void BuildLights()
        {
            if (transform.Find("Sun") != null) return;
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            sun.color = new Color(0.86f, 0.9f, 1f); // ay ışığı: aydınlık gece (kullanıcı 2026-11-22)
            sun.intensity = 0.95f;
            sun.shadows = LightShadows.None; // 2026-11-22: sade gece sahnesi; uzak gölgeler kara leke yapıyordu
            sun.shadowStrength = 0.32f;
            sun.cullingMask = 1 << Layer;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.transform.SetParent(transform, false);
            fill.type = LightType.Directional;
            fill.color = new Color(0.8f, 0.6f, 1f); // kuzey ışığından eflatun dolgu
            fill.intensity = 0.4f;
            fill.shadows = LightShadows.None;
            fill.cullingMask = 1 << Layer;
            fill.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
        }

        /// <summary>Kamera hedefin arkasından ve yukarıdan bakar; yol kıvrımını hafifçe izler.</summary>
        private void PlaceCamera()
        {
            if (_cam == null) return;
            var target = transform.position + new Vector3(PathX(_focusZ) * 0.45f, 0f, _focusZ);
            // 2026-11-22 (kullanıcı): daha uzak ve daha yatay; yolun ilerisi görünsün.
            // Kamera maskotun 16 gerisinde, 13 yukarıda, 8 ileri bakar (≈28°): maskot ekranın alt üçte birinde, yolun ilerisi
            // görünür; arkadaki kapı kemeri maskotu örtmez.
            _cam.transform.position = target + new Vector3(0f, 8f, -18f);
            _cam.transform.LookAt(target + new Vector3(0f, 2f, 12f)); // ≈11°: üstte gökyüzü, ortada kapılar, altta maskot
            PlaceSky();
        }

        // ---------------------------------------------------------------- etkileşim

        /// <summary>Sürükleme: ekran yüksekliğine oranla dikey kayma (yukarı → ileri).</summary>
        public void Drag(float normalizedDy)
        {
            if (_entering) return;
            _following = false;
            _focusZ = ClampFocus(_focusZ + normalizedDy * 26f);
            _focusVel = normalizedDy * 26f / Mathf.Max(0.016f, Time.unscaledDeltaTime);
        }

        public void Release() { }

        private float ClampFocus(float z) => Mathf.Clamp(z, GateZ(0) - 2f, GateZ(Mathf.Max(0, _count - 1)));

        /// <summary>Dokunma (görüntüde 0–1 konum): kapıya isabet ederse o bölüm. Açık değilse -1.</summary>
        public int Tap(Vector2 uv)
        {
            if (_cam == null || _entering) return -1;
            var ray = _cam.ViewportPointToRay(new Vector3(uv.x, uv.y, 0f));
            if (!Physics.Raycast(ray, out var hit, 200f, 1 << Layer)) return -1;
            int slot = _gates.IndexOf(hit.collider.transform);
            if (slot < 0) return -1;
            return TapGate(_first + slot);
        }

        /// <summary>Kapı seçimi: açık değilse sallanır (-1), açıksa maskot yürür.</summary>
        public int TapGate(int index)
        {
            int slot = index - _first;
            if (_entering || _flying || _landT >= 0f || slot < 0 || slot >= _gates.Count) return -1;
            if (_isOpen != null && !_isOpen(index)) { Shake(slot); return -1; }
            // Geçmiş bölüm: maskot uçarak gelir (kullanıcı 2026-11-22); sıradaki kapıya yürür.
            if (index < _current) FlyTo(index); else WalkInto(index);
            return index;
        }

        private float _walkSign = 1f;

        /// <summary>Maskot o kapının önüne yürür; varınca <c>onEnter(index)</c> (oynama kartı).</summary>
        public void WalkInto(int index)
        {
            int slot = Mathf.Clamp(index - _first, 0, _count - 1);
            _walkGate = _first + slot;
            _walkTo = GateZ(slot) - GateStop;
            _following = true;
        }

        private bool _flying;
        private float _flyT, _flyDur, _flyFrom, _flyTo, _flyH, _landT = -1f;
        public const float LandTime = 0.55f;

        /// <summary>Kapıya yay çizerek uçar; konunca zıplayıp şişip normale döner, sonra oynama kartı.</summary>
        public void FlyTo(int index)
        {
            int slot = Mathf.Clamp(index - _first, 0, _count - 1);
            _walkGate = _first + slot;
            _walkTo = -1f;
            _flyFrom = _mascotZ;
            _flyTo = GateZ(slot) - GateStop;
            float dist = Mathf.Abs(_flyTo - _flyFrom);
            _flyDur = Mathf.Clamp(0.55f + dist * 0.01f, 0.65f, 1.5f);
            _flyH = Mathf.Clamp(2.5f + dist * 0.05f, 3f, 8f);
            _walkSign = Mathf.Sign(_flyTo - _flyFrom);
            _flyT = 0f;
            _flying = true;
            _following = true;
        }

        private float _shakeT = -1f;
        private int _shakeSlot = -1;

        private void Shake(int slot) { _shakeSlot = slot; _shakeT = 0f; }

        /// <summary>Kapıdan girdikten sonra maskotu kapının önüne geri koyar (başlangıç kartı kapatılırsa).</summary>
        public void ResetAfterEnter()
        {
            _entering = false;
            if (_mascot != null) _mascot.localScale = Vector3.one * MascotScale;
            PlaceMascot(0f);
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float dt)
        {
            _t += dt;
            float hop = 0f;
            if (_walkTo >= 0f)
            {
                _walkT += dt;
                float dir = Mathf.Sign(_walkTo - _mascotZ);
                if (Mathf.Abs(_walkTo - _mascotZ) > 1e-3f) _walkSign = dir;
                float step = WalkSpeed * dt;
                if (Mathf.Abs(_walkTo - _mascotZ) <= step)
                {
                    _mascotZ = _walkTo;
                    _walkTo = -1f;
                    if (_walkGate >= 0)
                    {
                        // Kapıya vardı: kapıdan girmeden oynama kartı açılır.
                        int g = _walkGate;
                        _walkGate = -1;
                        _onEnter?.Invoke(g);
                    }
                }
                else _mascotZ += dir * step;
                hop = 0.28f * Mathf.Abs(Mathf.Sin(_walkT * 9f));
            }
            Vector3 squash = Vector3.one;
            if (_flying)
            {
                _flyT += dt;
                float k = Mathf.Clamp01(_flyDur > 0f ? _flyT / _flyDur : 1f);
                float e = k * k * (3f - 2f * k);
                _mascotZ = Mathf.Lerp(_flyFrom, _flyTo, e);
                hop = 4f * _flyH * k * (1f - k);
                squash = new Vector3(0.92f, 1.12f, 0.92f); // uçarken hafif uzar
                if (k >= 1f) { _flying = false; _landT = 0f; hop = 0f; }
            }
            else if (_landT >= 0f)
            {
                // Konma: ezilme → şişme (zıplayarak) → normale dönüş.
                _landT += dt;
                float t = _landT;
                if (t < 0.12f) { float a = t / 0.12f; squash = new Vector3(1f + 0.3f * a, 1f - 0.35f * a, 1f + 0.3f * a); }
                else if (t < 0.32f) { float a = (t - 0.12f) / 0.2f; float sw = Mathf.Sin(a * Mathf.PI); squash = Vector3.Lerp(new Vector3(1.3f, 0.65f, 1.3f), new Vector3(1.15f, 1.15f, 1.15f), a); hop = 0.6f * sw; }
                else if (t < LandTime) { float a = (t - 0.32f) / (LandTime - 0.32f); squash = Vector3.Lerp(new Vector3(1.15f, 1.15f, 1.15f), Vector3.one, a * a * (3f - 2f * a)); }
                if (t >= LandTime)
                {
                    _landT = -1f;
                    squash = Vector3.one;
                    int g = _walkGate;
                    _walkGate = -1;
                    _onEnter?.Invoke(g);
                }
            }
            if (_entering)
            {
                _enterT += dt;
                float k = Mathf.Clamp01(_enterT / 0.45f);
                _mascotZ += dt * 3.2f;
                if (_mascot != null) _mascot.localScale = Vector3.one * MascotScale * (1f - k);
                hop = 0.3f * Mathf.Sin(k * Mathf.PI);
                if (k >= 1f)
                {
                    int g = _walkGate;
                    _walkGate = -1;
                    _entering = false;
                    _onEnter?.Invoke(g);
                }
            }
            PlaceMascot(hop, squash);

            if (_following) _focusZ = Mathf.Lerp(_focusZ, ClampFocus(_mascotZ), dt <= 0f ? 1f : Mathf.Clamp01(dt * 4f));
            else if (Mathf.Abs(_focusVel) > 0.05f && dt > 0f)
            {
                _focusZ = ClampFocus(_focusZ + _focusVel * dt);
                _focusVel *= Mathf.Pow(0.04f, dt);
            }

            foreach (var r in _currentRing)
                if (r != null)
                {
                    float s = 3f + 0.3f * Mathf.Sin(_t * 3f);
                    r.localScale = new Vector3(s, 0.01f, s);
                }
            if (_shakeT >= 0f && _shakeSlot >= 0 && _shakeSlot < _gates.Count)
            {
                _shakeT += dt;
                var g = _gates[_shakeSlot];
                float a = _shakeT < 0.4f ? Mathf.Sin(_shakeT * 50f) * 4f * (1f - _shakeT / 0.4f) : 0f;
                g.localRotation = Quaternion.LookRotation(PathTangent(GateZ(_shakeSlot)), Vector3.up) * Quaternion.Euler(0f, 0f, a);
                if (_shakeT >= 0.4f) _shakeT = -1f;
            }
            PlaceCamera();
            if (!Application.isPlaying && _cam != null) RenderNight();
        }

        /// <summary>Yakalama/test: kamerayı elle çizer.</summary>
        public void RenderNow() { PlaceCamera(); if (_cam != null) RenderNight(); }

        /// <summary>Elle çizim (düzenleyicide OnEnable çalışmaz): gece ortam ışığıyla.</summary>
        private void RenderNight() { NightPre(_cam); _cam.Render(); NightPost(_cam); }

        /// <summary>Yakalama/test: kamerayı bir kapıya götürür (maskot yerinde kalır).</summary>
        public void FocusGate(int index)
        {
            _following = false;
            _focusVel = 0f;
            _focusZ = ClampFocus(RestZ(Mathf.Clamp(index - _first, 0, _count - 1)));
            PlaceCamera();
        }

        /// <summary>Kameranın odaklandığı z (testler).</summary>
        public float FocusZ => _focusZ;

        /// <summary>Kapının ekrandaki yeri (görüntüde 0–1); testler ve yakalama için.</summary>
        public Vector2 GateViewport(int index)
        {
            int slot = index - _first;
            if (_cam == null || slot < 0 || slot >= _gates.Count) return new Vector2(-1f, -1f);
            var v = _cam.WorldToViewportPoint(_gates[slot].position + new Vector3(0f, 1.5f, 0f));
            return new Vector2(v.x, v.y);
        }
    }
}
