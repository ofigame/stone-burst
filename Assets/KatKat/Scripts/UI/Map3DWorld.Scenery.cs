using System;
using UnityEngine;

namespace KatKat.UI
{
    /// <summary>
    /// 3D harita manzarası (kullanıcı 2026-11-22: "farklı görüntüler de gerek: şelale, çöl, dik kayalar, tepeler";
    /// referans "Renkli Şelaleli Masal Vadisi": lila kayalar, basamaklı kayalıktan dökülen şelaleler, nilüferli su, fener,
    /// çit; ortam "kuzey ışıklarının renkli, aydınlık geceleri"). Her 5 bölüm bir manzara: Çayır → Şelale vadisi → Tepeler
    /// → Çöl (kayalıklar 2026-11-22 kaldırıldı) (bölüm numarasına bağlı; aynı yer hep aynı görünür). Kurulan son kapının ötesi sis bulutu.
    /// Gökyüzü Resources/Map3D/sky.png (kuzey ışıklı gece, kodla üretildi) kameraya bağlı uzak düzlemde.
    /// </summary>
    public sealed partial class Map3DWorld
    {
        private enum Biome { Meadow, Waterfall, Hills, Desert }

        private const int BiomeLength = 5;

        private static readonly Color Grass = new Color32(0x4C, 0xB4, 0x3A, 0xFF);
        private static readonly Color GrassDark = new Color32(0x40, 0xA0, 0x38, 0xFF);
        private static readonly Color DesertSand = new Color32(0xE2, 0xBE, 0x86, 0xFF);
        private static readonly Color DesertSandDark = new Color32(0xCF, 0xA4, 0x6E, 0xFF);
        private static readonly Color Sand = new Color32(0xEE, 0xCB, 0x9E, 0xFF);
        private static readonly Color SandEdge = new Color32(0xC9, 0x96, 0x68, 0xFF);
        private static readonly Color Water = new Color32(0x3C, 0xD2, 0xF6, 0xFF);
        private static readonly Color WaterDeep = new Color32(0x22, 0xA6, 0xE6, 0xFF);
        private static readonly Color Lilac = new Color32(0xB0, 0x9E, 0xD8, 0xFF);
        private static readonly Color LilacLight = new Color32(0xC8, 0xBA, 0xEA, 0xFF);
        private static readonly Color Moss = new Color32(0x7A, 0xD0, 0x4E, 0xFF);

        private Material _glowBase;
        private Transform _sky;

        // ---------------------------------------------------------------- yardımcılar

        private Biome BiomeAtZ(float z)
        {
            int slot = Mathf.Clamp(Mathf.RoundToInt((z - FirstGateZ) / GateSpacing), 0, Mathf.Max(0, _count - 1));
            int level = Mathf.Max(0, _first + slot);
            // 2026-11-22 (kullanıcı): kaya sütunlu "Kayalıklar" kaldırıldı; döngü Çayır → Şelale → Tepeler → Çöl.
            Biome[] order = { Biome.Meadow, Biome.Waterfall, Biome.Hills, Biome.Desert };
            return order[level / BiomeLength % order.Length];
        }

        /// <summary>Kendinden ışıklı malzeme (fener, ateş böceği, şelale, bulut): glow.mat kopyası.</summary>
        private Material GlowMat(Color c, float intensity)
        {
            var key = new Color(c.r + 10f, c.g + 10f, c.b + 10f, intensity);
            if (_mats.TryGetValue(key, out var m) && m != null) return m;
            if (_glowBase == null) _glowBase = Resources.Load<Material>("Map3D/glow");
            m = _glowBase != null ? new Material(_glowBase) : new Material(Mat(c));
            m.color = c;
            m.SetColor("_EmissionColor", c * intensity);
            m.EnableKeyword("_EMISSION");
            m.SetFloat("_Glossiness", 0.3f);
            _mats[key] = m;
            return m;
        }

        private Transform Glow(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color c, float intensity)
        {
            var t = Part(type, parent, pos, scale, c, 0.3f);
            var r = t.GetComponent<MeshRenderer>();
            r.sharedMaterial = GlowMat(c, intensity);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }

        /// <summary>Büyük kaya/tepe bloğu: gölge düşürmez (gece sahnesinde büyük kara lekeler oluşuyordu).</summary>
        private Transform Block(Transform parent, Vector3 pos, Vector3 size, Color c, float gloss = 0.2f)
        {
            var t = MeshPart("Block", parent, CachedMesh("block", () => RoundedBox(Vector3.one, 0.18f, 4)), pos, size, c, gloss);
            t.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }

        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        // ---------------------------------------------------------------- kurulum

        private void BuildScenery(float endZ)
        {
            float z0 = -40f, zFar = endZ + 300f;
            // Taban çimen (ufka kadar); çöl parçalarında üstüne kum. Geniş şeritler düz (kıvrım uzak kenarda üçgenleri katlıyordu).
            Strip("Grass", z => 0f, 400f, -0.03f, z0, zFar, Grass, 0.05f);
            Strip("GrassBand", PathX, 10f, -0.025f, -6f, endZ, GrassDark, 0.05f);
            ForEachRun(endZ, (b, a, e) =>
            {
                if (b == Biome.Desert)
                {
                    Strip("Desert", z => 0f, 400f, -0.02f, a - 1f, e + 1f, DesertSand, 0.05f);
                    Strip("DesertBand", PathX, 10f, -0.015f, a - 1f, e + 1f, DesertSandDark, 0.05f);
                }
            });
            BuildRiver(endZ);
            var rng = new System.Random(23 + _first);
            for (float z = -8f; z < endZ - 2f; z += 8.5f) // 2026-11-22: sade harita (kullanıcı: "çok fazla nesne olmasın")
            {
                switch (BiomeAtZ(z))
                {
                    case Biome.Meadow: MeadowAt(z, rng); break;
                    case Biome.Waterfall: MeadowAt(z, rng, 0.5f); break;
                    case Biome.Hills: HillsAt(z, rng); break;
                    case Biome.Desert: DesertAt(z, rng); break;
                }
            }
            // Şelale vadileri: her şelale parçasının ortasında basamaklı kayalık + iki şelale + gölcük.
            ForEachRun(endZ, (b, a, e) => { if (b == Biome.Waterfall) WaterfallAt((a + e) * 0.5f, rng); });
            // Fenerler: her kapının solunda (çölde meşale rengi), ateş böcekleri.
            for (int i = 0; i < _count; i += 2)
            {
                float gz = GateZ(i) - 3.5f;
                Lantern(PathPoint(gz) + new Vector3(-2.1f, 0f, 0f), BiomeAtZ(gz) == Biome.Desert);
            }
            for (float z = -4f; z < endZ - 3f; z += 9f)
            {
                if (BiomeAtZ(z) == Biome.Desert) continue;
                var p = PathPoint(z) + new Vector3(R(rng, -6f, 5f), R(rng, 0.7f, 2.6f), R(rng, -1f, 1f));
                Glow(PrimitiveType.Sphere, _root, p, Vector3.one * R(rng, 0.1f, 0.16f), new Color(0.85f, 1f, 0.55f), 1.6f);
            }
            EndClouds(endZ, rng);
            BuildSky();
        }

        /// <summary>Aynı manzaranın ardışık parçaları (z aralıkları).</summary>
        private void ForEachRun(float endZ, Action<Biome, float, float> act)
        {
            float start = -40f;
            var cur = BiomeAtZ(start);
            for (float z = start; z <= endZ + 0.01f; z += 0.5f)
            {
                var b = BiomeAtZ(z);
                if (b != cur || z + 0.5f > endZ)
                {
                    act(cur, start, z);
                    start = z;
                    cur = b;
                }
            }
        }

        private void BuildRiver(float endZ)
        {
            float zFar = endZ + 20f;
            ForEachRun(zFar, (b, a, e) =>
            {
                if (b == Biome.Desert)
                {
                    // Kuru dere yatağı.
                    Strip("DryBed", RiverX, 2.6f, -0.01f, a, e, DesertSandDark, 0.05f);
                    return;
                }
                Strip("RiverBank", RiverX, 3.4f, -0.005f, a - 0.3f, e + 0.3f, new Color32(0xE8, 0xD8, 0xB0, 0xFF), 0.1f);
                var river = Strip("River", RiverX, 2.5f, 0.006f, a - 0.3f, e + 0.3f, Water, 0.75f);
                river.GetComponent<MeshRenderer>().sharedMaterial = GlowMat(Water, 0.22f);
                var deep = Strip("RiverDeep", RiverX, 1.0f, 0.01f, a - 0.3f, e + 0.3f, WaterDeep, 0.8f);
                deep.GetComponent<MeshRenderer>().sharedMaterial = GlowMat(WaterDeep, 0.18f);
            });
            var rng = new System.Random(11 + _first);
            for (float z = -6f; z < endZ + 10f; z += 7f)
            {
                if (BiomeAtZ(z) == Biome.Desert) continue;
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                Rock(new Vector3(RiverX(z) + side * 1.5f, 0f, z), R(rng, 0.35f, 0.65f), rng);
                if (rng.NextDouble() < 0.3) LilyPad(new Vector3(RiverX(z + 1f) + R(rng, -0.5f, 0.5f), 0.03f, z + 1f), rng);
            }
        }

        private void LilyPad(Vector3 p, System.Random rng)
        {
            var pad = Part(PrimitiveType.Cylinder, _root, p, new Vector3(0.55f, 0.01f, 0.55f), new Color32(0x5C, 0xC0, 0x48, 0xFF), 0.3f);
            pad.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (rng.NextDouble() < 0.7) Glow(PrimitiveType.Sphere, _root, p + new Vector3(0.08f, 0.08f, 0f), new Vector3(0.24f, 0.14f, 0.24f), new Color32(0xFF, 0x9C, 0xD6, 0xFF), 0.45f);
        }

        // ---------------------------------------------------------------- öğeler

        private void Rock(Vector3 p, float s, System.Random rng, bool sandy = false)
        {
            Color a = sandy ? new Color32(0xD0, 0x9C, 0x70, 0xFF) : Lilac, b = sandy ? new Color32(0xE0, 0xB2, 0x84, 0xFF) : LilacLight;
            var r = MeshPart("Rock", _root, CachedMesh("rock", () => RoundedBox(Vector3.one, 0.32f, 4)), p + new Vector3(0f, s * 0.32f, 0f), new Vector3(s * 1.4f, s * 0.75f, s * 1.1f), a, 0.25f);
            r.localRotation = Quaternion.Euler(R(rng, -8f, 8f), R(rng, 0f, 360f), R(rng, -8f, 8f));
            if (rng.NextDouble() < 0.5)
            {
                var r2 = MeshPart("Rock", _root, CachedMesh("rock", () => RoundedBox(Vector3.one, 0.32f, 4)), p + new Vector3(s * 0.8f, s * 0.2f, s * 0.3f), new Vector3(s * 0.75f, s * 0.45f, s * 0.65f), b, 0.25f);
                r2.localRotation = Quaternion.Euler(0f, R(rng, 0f, 360f), 0f);
            }
        }

        private void Tree(Vector3 p, float s, System.Random rng)
        {
            var t = new GameObject("Tree").transform;
            t.SetParent(_root, false);
            t.localPosition = p;
            t.localScale = Vector3.one * s;
            t.gameObject.layer = Layer;
            Part(PrimitiveType.Cylinder, t, new Vector3(0f, 0.7f, 0f), new Vector3(0.32f, 0.7f, 0.32f), new Color32(0xA0, 0x68, 0x3C, 0xFF), 0.1f);
            Color leaf = rng.NextDouble() < 0.5 ? (Color)new Color32(0x5E, 0xC6, 0x4C, 0xFF) : new Color32(0x74, 0xD2, 0x52, 0xFF);
            Part(PrimitiveType.Sphere, t, new Vector3(0f, 1.9f, 0f), new Vector3(1.6f, 1.35f, 1.6f), leaf, 0.15f);
            Part(PrimitiveType.Sphere, t, new Vector3(-0.55f, 1.6f, 0.2f), new Vector3(1.0f, 0.9f, 1.0f), leaf * 0.94f, 0.15f);
            Part(PrimitiveType.Sphere, t, new Vector3(0.55f, 1.65f, -0.15f), new Vector3(1.05f, 0.9f, 1.05f), leaf * 1.05f, 0.15f);
            Part(PrimitiveType.Sphere, t, new Vector3(0.1f, 2.45f, 0.1f), new Vector3(0.9f, 0.7f, 0.9f), leaf * 1.08f, 0.15f);
        }

        private void Bush(Vector3 p, float s, System.Random rng)
        {
            Color leaf = new Color32(0x62, 0xC8, 0x4E, 0xFF);
            for (int i = 0; i < 3; i++)
                Part(PrimitiveType.Sphere, _root, p + new Vector3((i - 1) * 0.45f * s, 0.3f * s, R(rng, -0.2f, 0.2f)), new Vector3(0.8f, 0.6f, 0.7f) * s, leaf * (0.92f + 0.06f * i), 0.2f);
        }

        private void Flowers(Vector3 p, System.Random rng)
        {
            Color[] cs = { new Color32(0xFF, 0xD9, 0x3A, 0xFF), new Color32(0xFF, 0xFF, 0xFF, 0xFF), new Color32(0xFF, 0x8F, 0xD0, 0xFF) };
            var c = cs[rng.Next(cs.Length)];
            for (int i = 0; i < 3; i++)
            {
                var q = p + new Vector3(R(rng, -0.3f, 0.3f), 0.08f, R(rng, -0.3f, 0.3f));
                Glow(PrimitiveType.Sphere, _root, q, new Vector3(0.18f, 0.1f, 0.18f), c, 0.35f);
            }
        }

        /// <summary>Fener: ahşap direk, kol, sıcak ışıklı lamba (çölde turuncu meşale).</summary>
        private void Lantern(Vector3 p, bool desert)
        {
            var wood = new Color32(0x9A, 0x5E, 0x34, 0xFF);
            Part(PrimitiveType.Cylinder, _root, p + new Vector3(0f, 1.1f, 0f), new Vector3(0.16f, 1.1f, 0.16f), wood, 0.1f);
            Part(PrimitiveType.Cube, _root, p + new Vector3(0.28f, 2.1f, 0f), new Vector3(0.62f, 0.1f, 0.12f), wood, 0.1f);
            Part(PrimitiveType.Cube, _root, p + new Vector3(0.52f, 1.86f, 0f), new Vector3(0.3f, 0.06f, 0.3f), new Color32(0x4A, 0x36, 0x4E, 0xFF), 0.3f);
            Glow(PrimitiveType.Sphere, _root, p + new Vector3(0.52f, 1.66f, 0f), new Vector3(0.28f, 0.34f, 0.28f), desert ? new Color(1f, 0.62f, 0.25f) : new Color(1f, 0.86f, 0.45f), 2.2f);
            // Yerde hafif ışık halesi.
            var halo = Glow(PrimitiveType.Cylinder, _root, p + new Vector3(0.5f, 0.012f, 0f), new Vector3(1.8f, 0.004f, 1.8f), new Color(1f, 0.9f, 0.55f), 0.35f);
            halo.GetComponent<MeshRenderer>().receiveShadows = false;
        }

        private void Fence(Vector3 a, Vector3 b)
        {
            var wood = new Color32(0xC6, 0x84, 0x4A, 0xFF);
            int posts = 3;
            for (int i = 0; i < posts; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)(posts - 1));
                Part(PrimitiveType.Cylinder, _root, p + new Vector3(0f, 0.45f, 0f), new Vector3(0.22f, 0.45f, 0.22f), wood, 0.15f);
            }
            var mid = (a + b) * 0.5f + new Vector3(0f, 0.6f, 0f);
            var rail = Part(PrimitiveType.Cylinder, _root, mid, new Vector3(0.1f, (b - a).magnitude * 0.5f, 0.1f), wood, 0.15f);
            rail.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
        }

        // ---------------------------------------------------------------- manzaralar

        private void MeadowAt(float z, System.Random rng, float density = 1f)
        {
            float lx = PathX(z) - R(rng, 3.6f, 10f);
            double d = rng.NextDouble();
            if (d < 0.25 * density) Tree(new Vector3(lx, 0f, z + R(rng, 0f, 2f)), R(rng, 0.85f, 1.3f), rng);
            else if (d < 0.6) Rock(new Vector3(lx, 0f, z), R(rng, 0.5f, 1.0f), rng);
            else Bush(new Vector3(lx, 0f, z), R(rng, 0.8f, 1.2f), rng);
            if (rng.NextDouble() < 0.5) Flowers(new Vector3(PathX(z) - R(rng, 2.1f, 3.4f), 0f, z + 1.2f), rng);
            if (rng.NextDouble() < 0.15)
            {
                float fz = z + 1.5f;
                Fence(PathPoint(fz) + new Vector3(2.3f, 0f, 0f), PathPoint(fz + 2.4f) + new Vector3(2.3f, 0f, 0f));
            }
            float rx = RiverX(z) + R(rng, 3.4f, 9f);
            if (rng.NextDouble() < 0.25 * density) Tree(new Vector3(rx, 0f, z), R(rng, 0.9f, 1.4f), rng);
        }

        private void HillsAt(float z, System.Random rng)
        {
            // Yuvarlak çimen tepeler (iki yanda), tepelerde yuvarlak ağaçlar.
            for (int side = -1; side <= 1; side += 2)
            {
                if (rng.NextDouble() < 0.35) continue;
                float x = side < 0 ? PathX(z) - R(rng, 9f, 17f) : RiverX(z) + R(rng, 8f, 15f);
                float rx = R(rng, 7f, 11f), h = R(rng, 2.5f, 4.5f);
                Part(PrimitiveType.Sphere, _root, new Vector3(x, 0f, z + R(rng, -1f, 1f)), new Vector3(rx, h * 2f, rx * R(rng, 0.8f, 1.1f)), rng.NextDouble() < 0.5 ? Grass : (Color)new Color32(0x62, 0xBE, 0x44, 0xFF), 0.05f)
                    .GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (rng.NextDouble() < 0.6) Tree(new Vector3(x + R(rng, -1.5f, 1.5f), h * 0.92f, z), R(rng, 0.8f, 1.1f), rng);
            }
            if (rng.NextDouble() < 0.6) Flowers(new Vector3(PathX(z) - R(rng, 2.1f, 3.4f), 0f, z + 1.2f), rng);
            if (rng.NextDouble() < 0.4) Rock(new Vector3(PathX(z) - R(rng, 3.5f, 6f), 0f, z), R(rng, 0.4f, 0.8f), rng);
        }

        private void DesertAt(float z, System.Random rng)
        {
            float lx = PathX(z) - R(rng, 3.6f, 10f);
            double d = rng.NextDouble();
            if (d < 0.4) Cactus(new Vector3(lx, 0f, z), R(rng, 0.8f, 1.3f), rng);
            else if (d < 0.7) Rock(new Vector3(lx, 0f, z), R(rng, 0.6f, 1.1f), rng, true);
            else Part(PrimitiveType.Sphere, _root, new Vector3(lx - 2f, 0f, z), new Vector3(R(rng, 5f, 8f), R(rng, 1.4f, 2.4f), R(rng, 3f, 5f)), DesertSand, 0.05f);
            // Uzakta kızıl masa tepeler.
            if (rng.NextDouble() < 0.45)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    if (rng.NextDouble() < 0.5) continue;
                    float x = side < 0 ? PathX(z) - R(rng, 11f, 20f) : RiverX(z) + R(rng, 7f, 16f);
                    float w = R(rng, 5f, 8f), h = R(rng, 2.5f, 4.5f);
                    Block(_root, new Vector3(x, h * 0.5f, z), new Vector3(w, h, w * 0.8f), new Color32(0xD8, 0x84, 0x58, 0xFF), 0.1f);
                    Block(_root, new Vector3(x, h + 0.1f, z), new Vector3(w * 0.95f, 0.3f, w * 0.78f), new Color32(0xEC, 0xA6, 0x74, 0xFF), 0.1f);
                }
            }
            float rx = RiverX(z) + R(rng, 3.4f, 9f);
            if (rng.NextDouble() < 0.35) Cactus(new Vector3(rx, 0f, z), R(rng, 0.8f, 1.2f), rng);
        }

        private void Cactus(Vector3 p, float s, System.Random rng)
        {
            var green = new Color32(0x4E, 0xB0, 0x5C, 0xFF);
            Part(PrimitiveType.Capsule, _root, p + new Vector3(0f, 1.1f * s, 0f), new Vector3(0.55f, 1.1f, 0.55f) * s, green, 0.25f);
            for (int side = -1; side <= 1; side += 2)
            {
                if (rng.NextDouble() < 0.3) continue;
                float h = R(rng, 0.8f, 1.4f) * s;
                Part(PrimitiveType.Capsule, _root, p + new Vector3(side * 0.42f * s, h, 0f), new Vector3(0.3f, 0.32f, 0.3f) * s, green, 0.25f).localRotation = Quaternion.Euler(0f, 0f, 90f);
                Part(PrimitiveType.Capsule, _root, p + new Vector3(side * 0.62f * s, h + 0.32f * s, 0f), new Vector3(0.3f, 0.36f, 0.3f) * s, green, 0.25f);
            }
            if (rng.NextDouble() < 0.5) Glow(PrimitiveType.Sphere, _root, p + new Vector3(0f, 2.25f * s, 0f), Vector3.one * 0.22f * s, new Color32(0xFF, 0x7C, 0xC0, 0xFF), 0.5f);
        }

        /// <summary>Basamaklı lila kayalık; üstten gölcüğe ve alt basamaktan dereye iki şelale, köpük, nilüferli gölcük.</summary>
        private void WaterfallAt(float zm, System.Random rng)
        {
            float bx = RiverX(zm) + 4.6f;
            // Gölcük (derenin genişlediği yer).
            var pond = Part(PrimitiveType.Cylinder, _root, new Vector3(RiverX(zm) + 1.2f, 0.004f, zm), new Vector3(7f, 0.01f, 8f), Water, 0.75f);
            pond.GetComponent<MeshRenderer>().sharedMaterial = GlowMat(Water, 0.22f);
            pond.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            LilyPad(new Vector3(RiverX(zm) - 0.6f, 0.03f, zm - 2f), rng);
            LilyPad(new Vector3(RiverX(zm) + 1.6f, 0.03f, zm + 2.2f), rng);
            // Alt basamak.
            float h1 = 3.6f, h2 = 6.6f;
            Block(_root, new Vector3(bx + 3f, h1 * 0.5f, zm), new Vector3(6f, h1, 11f), Lilac, 0.2f);
            Block(_root, new Vector3(bx + 3f, h1 + 0.2f, zm), new Vector3(6.2f, 0.55f, 11.2f), Moss, 0.1f);
            // Üst basamak (arkada, yüksek).
            Block(_root, new Vector3(bx + 7.5f, h2 * 0.5f, zm + 3f), new Vector3(6f, h2, 12f), LilacLight, 0.2f);
            Block(_root, new Vector3(bx + 7.5f, h2 + 0.2f, zm + 3f), new Vector3(6.2f, 0.55f, 12.2f), Moss, 0.1f);
            // Şelaleler: ince parlak su perdeleri + dipte köpük.
            var fall = new Color32(0x7C, 0xEE, 0xFF, 0xFF);
            Glow(PrimitiveType.Cube, _root, new Vector3(bx - 0.05f, h1 * 0.5f + 0.1f, zm - 1f), new Vector3(0.25f, h1 + 0.2f, 2.2f), fall, 0.55f);
            Glow(PrimitiveType.Cube, _root, new Vector3(bx + 4.45f, h1 + (h2 - h1) * 0.5f + 0.2f, zm + 2.2f), new Vector3(0.25f, h2 - h1 + 0.4f, 2f), fall, 0.55f);
            for (int i = 0; i < 3; i++)
            {
                Glow(PrimitiveType.Sphere, _root, new Vector3(bx - 0.6f + R(rng, -0.4f, 0.4f), 0.15f, zm - 1f + R(rng, -1.1f, 1.1f)), Vector3.one * R(rng, 0.4f, 0.75f), new Color(0.92f, 0.98f, 1f), 0.6f);
                Glow(PrimitiveType.Sphere, _root, new Vector3(bx + 3.9f + R(rng, -0.3f, 0.3f), h1 + 0.5f, zm + 2.2f + R(rng, -1f, 1f)), Vector3.one * R(rng, 0.35f, 0.6f), new Color(0.92f, 0.98f, 1f), 0.6f);
            }
            // Kayalığın üstünde çalılar ve ağaç; dibinde lila kayalar.
            Bush(new Vector3(bx + 2.5f, h1 + 0.4f, zm - 3f), 1f, rng);
            Tree(new Vector3(bx + 8f, h2 + 0.3f, zm + 5f), 1f, rng);
            Bush(new Vector3(bx + 7f, h2 + 0.4f, zm), 1.1f, rng);
            Rock(new Vector3(bx - 1.2f, 0f, zm + 2.6f), 0.9f, rng);
            Rock(new Vector3(bx - 1.4f, 0f, zm - 3.6f), 0.7f, rng);
        }

        /// <summary>Kurulan son kapının ötesi: alçak, ışıklı lavanta sis bulutu duvarı (ilerledikçe açılır).</summary>
        private void EndClouds(float endZ, System.Random rng)
        {
            float zc = endZ + 1.5f;
            var c1 = new Color32(0xEE, 0xE8, 0xFF, 0xFF);
            var c2 = new Color32(0xD8, 0xCC, 0xFF, 0xFF);
            for (float x = -46f; x <= 46f; x += 3.4f)
            {
                float cx = PathX(zc) * 0.6f + x;
                float r = R(rng, 2.6f, 4.4f);
                Glow(PrimitiveType.Sphere, _root, new Vector3(cx, r * 0.35f, zc + R(rng, -1f, 1f)), new Vector3(r * 1.5f, r, r * 1.2f), c1, 0.35f);
                if (rng.NextDouble() < 0.7) Glow(PrimitiveType.Sphere, _root, new Vector3(cx + 1.6f, r * 0.9f, zc + 2.5f), new Vector3(r * 1.3f, r * 0.9f, r), c2, 0.3f);
            }
            for (float x = -60f; x <= 60f; x += 6f)
            {
                float r = R(rng, 5f, 8f);
                Glow(PrimitiveType.Sphere, _root, new Vector3(PathX(zc) * 0.6f + x, r * 0.5f, zc + 8f + R(rng, -2f, 2f)), new Vector3(r * 1.6f, r, r * 1.2f), c2, 0.28f);
            }
        }

        // ---------------------------------------------------------------- gece ortam ışığı

        /// <summary>Gece ortam ışığı (yalnız bu kamera çizerken): mor-lacivert ortam, zayıf yansıma; sonra eski hâline döner.</summary>
        public static readonly Color NightAmbient = new Color(0.34f, 0.3f, 0.56f);

        private UnityEngine.Rendering.SphericalHarmonicsL2 _savedProbe;
        private float _savedReflect;
        private UnityEngine.Rendering.AmbientMode _savedMode;
        private Color _savedAmbient;
        private bool _ambientSwapped;

        private void OnEnable()
        {
            Camera.onPreRender += NightPre;
            Camera.onPostRender += NightPost;
        }

        private void OnDisable()
        {
            Camera.onPreRender -= NightPre;
            Camera.onPostRender -= NightPost;
        }

        private void NightPre(Camera c)
        {
            if (c == null || c != _cam || _ambientSwapped) return;
            _savedProbe = RenderSettings.ambientProbe;
            _savedReflect = RenderSettings.reflectionIntensity;
            _savedMode = RenderSettings.ambientMode;
            _savedAmbient = RenderSettings.ambientLight;
            var sh = new UnityEngine.Rendering.SphericalHarmonicsL2();
            sh.AddAmbientLight(NightAmbient);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = NightAmbient;
            RenderSettings.ambientProbe = sh;
            RenderSettings.reflectionIntensity = 0.25f;
            _ambientSwapped = true;
        }

        private void NightPost(Camera c)
        {
            if (c == null || c != _cam || !_ambientSwapped) return;
            RenderSettings.ambientMode = _savedMode;
            RenderSettings.ambientLight = _savedAmbient;
            RenderSettings.ambientProbe = _savedProbe;
            RenderSettings.reflectionIntensity = _savedReflect;
            _ambientSwapped = false;
        }

        // ---------------------------------------------------------------- gökyüzü

        private void BuildSky()
        {
            var tex = Resources.Load<Texture2D>("Map3D/sky");
            var sh = Resources.Load<Shader>("Shaders/KatKatMapSky");
            if (tex == null || sh == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Kill(go.GetComponent<Collider>());
            go.name = "Sky";
            go.layer = Layer;
            go.transform.SetParent(_root, false);
            var mat = new Material(sh) { name = "map3d_sky", mainTexture = tex };
            _mats[new Color(-2f, -2f, -2f, -2f)] = mat;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _sky = go.transform;
        }

        /// <summary>Gökyüzü düzlemi kameranın önünde 200 birimde; alt kenarı ufkun biraz altında, üstü görüş alanının tepesinde.</summary>
        private void PlaceSky()
        {
            if (_sky == null || _cam == null) return;
            const float dist = 200f;
            var ct = _cam.transform;
            var f = ct.forward; f.y = 0f;
            if (f.sqrMagnitude < 1e-4f) return;
            f.Normalize();
            float pitch = Mathf.Asin(Mathf.Clamp(-ct.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            float half = _cam.fieldOfView * 0.5f;
            float top = Mathf.Tan((half - pitch + 2f) * Mathf.Deg2Rad) * dist;
            float bottom = -Mathf.Tan(2.5f * Mathf.Deg2Rad) * dist;
            float hHalf = Mathf.Atan(Mathf.Tan(half * Mathf.Deg2Rad) * Mathf.Max(0.3f, _cam.aspect));
            float width = 2f * dist * Mathf.Tan(hHalf) * 1.2f;
            _sky.position = ct.position + f * dist + Vector3.up * ((top + bottom) * 0.5f);
            _sky.rotation = Quaternion.LookRotation(f, Vector3.up);
            _sky.localScale = new Vector3(width, top - bottom, 1f);
        }
    }
}
