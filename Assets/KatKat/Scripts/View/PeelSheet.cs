using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Soyulan tek katmanın ortak ızgara ağı. Bütün renkler aynı ağı ve aynı kıvrım matematiğini kullanır.
    /// Kıvrım: sağ üst köşeden sol alta ilerleyen kıvrım çizgisinin gerisindeki yüzey, çizgi üzerinde duran
    /// içe doğru daralan bir silindire sarılır (rulo). Yükseklik ekranda yukarı kaydırma ile 3/4 görünüm verir.
    /// </summary>
    public sealed class PeelSheet : MonoBehaviour
    {
        public const int Grid = 16;
        public const float Tilt = 0.55f;
        private static readonly Vector2 Dir = new Vector2(-0.70710678f, -0.70710678f);
        private static readonly Vector3 Light = new Vector3(-0.45f, 0.55f, 0.70f).normalized;

        private Mesh _mesh;
        private MeshRenderer _renderer;
        private MeshRenderer _shadow;
        private Mesh _shadowMesh;
        private readonly Color32[] _shadowCols = new Color32[4];
        private Vector2[] _rest;
        private Vector3[] _pos;
        private Vector2[] _face;
        private Color32[] _col;
        private Vector4[] _icon;
        private float _half;

        public float Size => _half * 2f;
        public float Diagonal => Size * 1.41421356f;

        public static PeelSheet Create(Transform parent)
        {
            var go = new GameObject("PeelSheet");
            go.transform.SetParent(parent, false);
            var sheet = go.AddComponent<PeelSheet>();
            sheet.Build();
            return sheet;
        }

        private void Build()
        {
            int vc = (Grid + 1) * (Grid + 1);
            _rest = new Vector2[vc];
            _pos = new Vector3[vc];
            _face = new Vector2[vc];
            _col = new Color32[vc];
            _icon = new Vector4[vc];
            var uv = new Vector2[vc];
            for (int y = 0; y <= Grid; y++)
            for (int x = 0; x <= Grid; x++)
            {
                int i = y * (Grid + 1) + x;
                uv[i] = new Vector2((float)x / Grid, (float)y / Grid);
            }
            var tri = new int[Grid * Grid * 6];
            int t = 0;
            for (int y = 0; y < Grid; y++)
            for (int x = 0; x < Grid; x++)
            {
                int a = y * (Grid + 1) + x, b = a + Grid + 1;
                tri[t++] = a; tri[t++] = b; tri[t++] = b + 1;
                tri[t++] = a; tri[t++] = b + 1; tri[t++] = a + 1;
            }
            _mesh = new Mesh { name = "PeelSheet" };
            _mesh.MarkDynamic();
            _mesh.vertices = _pos;
            _mesh.uv = uv;
            _mesh.triangles = tri;
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = ProceduralArt.PeelMaterial;

            // Kıvrımın alttaki yüzeye düşen yumuşak gölgesi.
            var sh = new GameObject("CurlShadow");
            sh.transform.SetParent(transform, false);
            var shMesh = _shadowMesh = new Mesh { name = "CurlShadow" };
            var b2 = new LayerMeshBuilder();
            b2.AddQuad(Vector2.zero, Vector2.one, new Color32(80, 50, 40, 90), Vector2.zero, 0f, 1f, LayerMeshBuilder.Flat);
            b2.ApplyTo(shMesh);
            sh.AddComponent<MeshFilter>().sharedMesh = shMesh;
            _shadow = sh.AddComponent<MeshRenderer>();
            _shadow.sharedMaterial = ProceduralArt.ShadowMaterial;
            gameObject.SetActive(false);
        }

        public void Begin(float size, Color32 color, Vector2 iconCell, int sortingOrder)
        {
            _half = size * 0.5f;
            for (int y = 0; y <= Grid; y++)
            for (int x = 0; x <= Grid; x++)
            {
                int i = y * (Grid + 1) + x;
                _rest[i] = new Vector2(((float)x / Grid - 0.5f) * size, ((float)y / Grid - 0.5f) * size);
                _col[i] = color;
                _icon[i] = new Vector4(iconCell.x, iconCell.y, 1f, 0f);
            }
            _mesh.SetUVs(2, _icon);
            _renderer.sortingOrder = sortingOrder;
            _shadow.sortingOrder = sortingOrder - 1;
            gameObject.SetActive(true);
        }

        public void End()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Ağın pozu. fold: köşeden kıvrım çizgisine uzaklık; radius: rulo yarıçapı;
        /// release: 0..1 hedefe uçuş; goal: uçuş hedefi (ağ uzayı, rulo merkezi buraya varır); alpha: opaklık.
        /// </summary>
        public void Pose(float fold, float radius, float release, Vector2 goal, float alpha, float shadowAlpha)
        {
            var c0 = new Vector2(_half, _half);
            byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
            float scale = Mathf.Lerp(1f, 0.28f, release);
            float spin = -28f * Mathf.Deg2Rad * release;
            float cs = Mathf.Cos(spin), sn = Mathf.Sin(spin);
            float arc = Mathf.Sin(Mathf.PI * release) * Size * 0.55f;
            // Uçuşta ölçek/dönme rulonun merkezi etrafında.
            float tubeS = Mathf.Min(fold, Diagonal);
            var pivot = c0 + tubeS * Dir + new Vector2(0f, radius * Tilt);
            var offset = (goal - pivot) * release + new Vector2(0f, arc);

            for (int i = 0; i < _rest.Length; i++)
            {
                Deform(_rest[i], c0, fold, radius, out var p, out float z, out float face, out float shade);
                var q = new Vector2(p.x, p.y + z * Tilt);
                float depth = z;
                if (release > 0f)
                {
                    var d = (q - pivot) * scale;
                    q = pivot + new Vector2(d.x * cs - d.y * sn, d.x * sn + d.y * cs) + offset;
                    depth = z * scale + release;
                }
                _pos[i] = new Vector3(q.x, q.y, -depth);
                _face[i] = new Vector2(face, shade);
                _col[i].a = a;
            }
            _mesh.vertices = _pos;
            _mesh.SetUVs(1, _face);
            _mesh.colors32 = _col;
            _mesh.RecalculateBounds();

            // Gölge: kıvrım çizgisi boyunca uzanan yumuşak elips.
            float chord = 2f * Mathf.Min(fold, Mathf.Max(0f, Diagonal - fold));
            shadowAlpha *= Mathf.Clamp01(fold / (Size * 0.25f));
            bool showShadow = shadowAlpha > 0.01f && release <= 0f;
            _shadow.enabled = showShadow;
            if (showShadow)
            {
                var sc = c0 + (fold - radius * 0.2f) * Dir + new Vector2(0f, -radius * 0.25f);
                var t = _shadow.transform;
                t.localPosition = new Vector3(sc.x, sc.y, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, -45f);
                t.localScale = new Vector3(Mathf.Max(chord, radius), radius * 1.9f, 1f);
                byte sa = (byte)Mathf.RoundToInt(90f * Mathf.Clamp01(shadowAlpha));
                for (int i = 0; i < 4; i++) _shadowCols[i] = new Color32(80, 50, 40, sa);
                _shadowMesh.colors32 = _shadowCols;
            }
        }

        /// <summary>Tek noktanın kıvrım dönüşümü. Saf matematik; zaman veya durum tutmaz.</summary>
        public static void Deform(Vector2 p, Vector2 corner, float fold, float radius,
            out Vector2 pos, out float height, out float face, out float shade)
        {
            var rel = p - corner;
            float s = Vector2.Dot(rel, Dir);
            if (s >= fold || radius <= 0f)
            {
                pos = p; height = 0f; face = 1f; shade = 1f;
                return;
            }
            var w = rel - s * Dir;
            float d = fold - s;
            float th = d / radius;
            // Sarım içe doğru daralır: ilk kalkan köşe rulonun merkezinde kalır.
            float r = radius * Mathf.Max(0.5f, 1f - 0.04f * th);
            float sp = fold - r * Mathf.Sin(th);
            height = radius - r * Mathf.Cos(th);
            pos = corner + sp * Dir + w;

            float nz = Mathf.Cos(th), ns = Mathf.Sin(th);
            face = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.12f, 0.12f, nz));
            var n = new Vector3(Dir.x * ns, Dir.y * ns, nz);
            if (nz < 0f) n = -n;
            shade = 0.8f + 0.2f * Mathf.Max(0f, Vector3.Dot(n, Light)) / Light.z;
        }
    }
}
