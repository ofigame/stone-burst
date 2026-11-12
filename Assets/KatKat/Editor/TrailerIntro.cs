using System;
using System.Collections.Generic;
using KatKat.Game;
using KatKat.UI;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Tanıtım açılışı (2026-11-12, kullanıcı: "3d bir animasyonla başlayacak; yolunu kaybetmiş maskotumuz haritadaki
    /// yolumuz gibi bir yoldan ilerliyor ve güzel bir bölümün taşının üzerine çıkıyor, olduğu yerde zıplıyor seviniyor
    /// ve oyun başlıyor").
    /// Gerçek harita (791. bölüm güncel) uzun bir doku olarak yakalanır ve perspektif kamerayla 3D zemine serilir; yanlarda
    /// aynalanmış kopyalar ve sis ufku kapatır. Onaylı kristal maskot (MascotView, dünya uzayı tuvali) sisin içinde
    /// kaybolmuş bakınır, hedef düğümde ışık yükselir, yol boyunca düğüm düğüm zıplar; 791'in taşı yerden sütun gibi
    /// yükselir (düğüm görüntüsünden üst üste diskler), maskot takla atarak üstüne çıkar, sevinir (konfeti, yıldız),
    /// kamera dalar ve beyaz parlamayla oyuna (p1 = 791. bölüm) geçilir. Kamera sahne boyunca maskotun etrafında döner.
    /// Süre 6 sn (120 BPM'de 3 ölçü); müzik IntroMusic, sesler koddan.
    /// </summary>
    public static partial class TrailerCapture
    {
        private const int IntroLayer = 30;
        private const float U = 0.01f;          // harita pikseli → dünya birimi
        private const int IntroTarget = 790;    // 791. bölüm (kanca sahnesi p1 de L791)
        public const float IntroSeconds = 6f;

        private static Vector3 Wp(Vector2 c, float y0) => new Vector3(c.x * U, 0f, (c.y - y0) * U);

        private static void Grab(Texture2D into)
        {
            Canvas.ForceUpdateCanvases();
            _cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = _cam.targetTexture;
            into.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            into.Apply();
            RenderTexture.active = prev;
        }

        private static Texture2D Tex(int w, int h, Func<float, float, Color> px, bool mip = true)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, mip) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var c = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) c[y * w + x] = px((x + 0.5f) / w, (y + 0.5f) / h);
            t.SetPixels(c);
            t.Apply(mip);
            return t;
        }

        private static float S01(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        private static float OutBack(float x) { x = Mathf.Clamp01(x); const float c = 1.9f; float u = x - 1f; return 1f + (c + 1f) * u * u * u + c * u * u; }

        /// <summary>Catmull-Rom anahtar kareler: (t, a, b, c).</summary>
        private static Vector3 Keys(float[][] k, float t)
        {
            if (t <= k[0][0]) return new Vector3(k[0][1], k[0][2], k[0][3]);
            int i = 0;
            while (i < k.Length - 2 && t > k[i + 1][0]) i++;
            float u = Mathf.Clamp01((t - k[i][0]) / (k[i + 1][0] - k[i][0]));
            Vector3 V(int j) { j = Mathf.Clamp(j, 0, k.Length - 1); return new Vector3(k[j][1], k[j][2], k[j][3]); }
            Vector3 p0 = V(i - 1), p1 = V(i), p2 = V(i + 1), p3 = V(i + 2);
            return 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u);
        }

        private static void IntroBody(GameController gc, Action rec)
        {
            var made = new List<UnityEngine.Object>();
            var keepFog = RenderSettings.fog;
            try { IntroRun(gc, rec, made); }
            finally
            {
                RenderSettings.fog = keepFog;
                for (int i = made.Count - 1; i >= 0; i--) if (made[i] != null) UnityEngine.Object.DestroyImmediate(made[i]);
            }
        }

        private static void IntroRun(GameController gc, Action rec, List<UnityEngine.Object> made)
        {
            T Keep<T>(T o) where T : UnityEngine.Object { made.Add(o); return o; }
            var cam = _cam;

            // ---- 1) Harita dokusu: 791 güncel, öncekiler yıldızlı; numarasız (zemin kameradan ters görünebilir) ----
            for (int i = 240; i < IntroTarget; i++) { gc.SaveService.MarkCompleted(gc.Catalog.IdAt(i), i, gc.Catalog.Count); gc.SaveService.RecordStars(gc.Catalog.IdAt(i), i % 5 == 2 ? 2 : 3); }
            gc.Menu.ShowLevels(gc.Catalog.Count, IntroTarget, i => i < IntroTarget, () => { });
            for (int f = 0; f < 20; f++) gc.Menu.Tick(1f / 30f);
            var np = MenuView.NodePosForTrailer(IntroTarget);
            float y0 = np.y;
            const int Screens = 5;
            float yLo = y0 - 5600f;
            var shot = Keep(new Texture2D(W, H, TextureFormat.RGB24, false));
            var ground = Keep(new Texture2D(W, Screens * H, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 9 });
            for (int s = 0; s < Screens; s++)
            {
                gc.Menu.ScrollMapForTest(yLo + s * H);
                gc.Menu.IsolateMapForTrailer(false);
                gc.Menu.Tick(0f);
                float got = gc.Menu.ScrollYForTest;
                if (Mathf.Abs(got - (yLo + s * H)) > 0.5f) Debug.LogWarning($"[KatKat] giriş: kaydırma {got} ≠ {yLo + s * H}");
                Grab(shot);
                ground.SetPixels(0, s * H, W, H, shot.GetPixels());
            }
            ground.Apply(true);

            // Hedef taşın üst yüzü: numaralı tek kare, düğüm çevresinden daire kırpılır.
            gc.Menu.ScrollMapForTest(y0 - H * 0.5f);
            gc.Menu.IsolateMapForTrailer(true);
            gc.Menu.Tick(0f);
            Grab(shot);
            float ncx = W * 0.5f + np.x, ncy = y0 - gc.Menu.ScrollYForTest;
            const int Cr = 96; const float Rad = 79f;
            Color rimAvg = Color.black; int rimN = 0;
            for (int a = 0; a < 64; a++)
            {
                float ang = a / 64f * Mathf.PI * 2f;
                rimAvg += shot.GetPixel((int)(ncx + Mathf.Cos(ang) * (Rad - 8f)), (int)(ncy + Mathf.Sin(ang) * (Rad - 8f))); rimN++;
            }
            rimAvg /= rimN;
            Color Crop(float u, float v)
            {
                float dx = (u - 0.5f) * 2f * Cr, dy = (v - 0.5f) * 2f * Cr;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                var c = shot.GetPixel((int)(ncx + dx), (int)(ncy + dy));
                c.a = Mathf.Clamp01((Rad - d) / 2f);
                return c;
            }
            var topTex = Keep(Tex(2 * Cr, 2 * Cr, Crop));
            Color Side(float u, float v, float k)
            {
                float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f, d = Mathf.Sqrt(dx * dx + dy * dy) * Cr;
                var c = rimAvg * k; c.a = Mathf.Clamp01((Rad - d) / 2f);
                return c;
            }
            var sideTex = Keep(Tex(2 * Cr, 2 * Cr, (u, v) => Side(u, v, 0.52f)));
            var rimTex = Keep(Tex(2 * Cr, 2 * Cr, (u, v) => Side(u, v, 0.78f)));

            gc.Menu.Canvas.enabled = false;
            gc.Hud.Canvas.enabled = false;

            // ---- 2) 3D sahne ----
            var sky = new Color(0.74f, 0.88f, 0.97f);
            cam.orthographic = false;
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.backgroundColor = sky;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.cullingMask = 1 << IntroLayer;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = sky;
            RenderSettings.fogStartDistance = 15f;
            RenderSettings.fogEndDistance = 44f;

            GameObject Go(string name)
            {
                var g = Keep(new GameObject(name));
                g.layer = IntroLayer;
                return g;
            }
            MeshRenderer Quad(string name, Material m)
            {
                var g = Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));
                g.name = name;
                g.layer = IntroLayer;
                UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
                var r = g.GetComponent<MeshRenderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                return r;
            }
            Material Mat(string shader, Texture t)
            {
                var sh = Shader.Find(shader) ?? Shader.Find("Unlit/Transparent");
                var m = Keep(new Material(sh) { mainTexture = t });
                return m;
            }

            // Zemin: ortada gerçek harita, iki yanda aynalanmış ve düz kopyalar (dikiş yerinde kesintisiz).
            var gMat = Mat("Unlit/Texture", ground);
            var gMir = Mat("Unlit/Texture", ground);
            gMir.mainTextureScale = new Vector2(-1f, 1f);
            gMir.mainTextureOffset = new Vector2(1f, 0f);
            float gw = W * U, gh = Screens * H * U;
            var gc0 = Wp(new Vector2(0f, yLo + Screens * H * 0.5f), y0);
            for (int k = -2; k <= 2; k++)
            {
                var q = Quad("Ground" + k, k % 2 == 0 ? gMat : gMir);
                q.transform.SetPositionAndRotation(gc0 + new Vector3(k * gw, 0f, 0f), Quaternion.Euler(90f, 0f, 0f));
                q.transform.localScale = new Vector3(gw, gh, 1f);
            }

            // Hedef taş: üst üste diskler (alttakiler koyu taş yanı, üstte numaralı düğüm yüzü). Derinlik yazar (kesik alfa).
            const int Discs = 26;
            const float PedR = 1.05f, PedH = 0.62f;
            var target = Wp(np, y0);
            var mSide = Mat("Unlit/Transparent Cutout", sideTex);
            var mRim = Mat("Unlit/Transparent Cutout", rimTex);
            var mTop = Mat("Unlit/Transparent Cutout", topTex);
            var discs = new Transform[Discs];
            for (int i = 0; i < Discs; i++)
            {
                var q = Quad("Disc" + i, i == Discs - 1 ? mTop : i >= Discs - 3 ? mRim : mSide);
                // Üst yüz: kamera sonda kuzeyden güneye bakar → numara ondan okunur dursun diye 180° döner.
                q.transform.rotation = Quaternion.Euler(90f, 180f, 0f);
                float sc = 2f * Cr * U * (PedR / (Rad * U)) * (i == Discs - 1 ? 1f : 1.0f - 0.025f * (1f - i / (float)(Discs - 1)));
                q.transform.localScale = new Vector3(sc, sc, 1f);
                q.gameObject.SetActive(false);
                discs[i] = q.transform;
            }

            // Yumuşak lekeler: gölge, toz, sis, bulut, ışık.
            var blobTex = Keep(Tex(128, 128, (u, v) => { float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f; float a = Mathf.Clamp01(1f - d); return new Color(1f, 1f, 1f, a * a * (3f - 2f * a)); }));
            var blob = Keep(Sprite.Create(blobTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128f));
            var shadowMat = Mat("Unlit/Transparent", Keep(Tex(128, 128, (u, v) => { float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f; return new Color(0.08f, 0.1f, 0.12f, 0.55f * Mathf.Clamp01(1f - d * d)); })));
            var shadow = Quad("Shadow", shadowMat);
            shadow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var beamTex = Keep(Tex(64, 256, (u, v) => { float x = Mathf.Abs(u - 0.5f) * 2f; float a = Mathf.Pow(Mathf.Clamp01(1f - x), 2.2f) * Mathf.Pow(1f - v, 1.3f) * Mathf.Clamp01(v * 12f); return new Color(1f, 0.92f, 0.6f, a); }));
            var addSh = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Mobile/Particles/Additive");
            var beamMat = Keep(new Material(addSh ?? Shader.Find("Unlit/Transparent")) { mainTexture = beamTex });
            var beam = Quad("Beam", beamMat);
            var ringTex = Keep(Tex(128, 128, (u, v) => { float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f; float a = Mathf.Exp(-Mathf.Pow((d - 0.8f) / 0.09f, 2f)); return new Color(1f, 0.9f, 0.55f, a); }));
            var ringMat = Keep(new Material(addSh ?? Shader.Find("Unlit/Transparent")) { mainTexture = ringTex });
            var ring = Quad("Ring", ringMat);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            void Tint(Material m, Color c) { if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c); else m.color = c; }

            SpriteRenderer Spr(string name, Sprite sp, Color c, int order = 0)
            {
                var g = Go(name);
                var r = g.AddComponent<SpriteRenderer>();
                r.sprite = sp; r.color = c; r.sortingOrder = order;
                return r;
            }
            var rnd = new System.Random(11);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

            // Yol: başlangıç (kaybolduğu yer) 786. bölüm; 4 zıplama 790'a, büyük sıçrama 791'in taşına.
            const int StartNode = IntroTarget - 5;
            Vector3 NodeW(int i) => Wp(MenuView.NodePosForTrailer(i), y0);
            Vector3 RoadW(float y) => Wp(new Vector2(MenuView.RoadXForTrailer(y), y), y0);
            float NodeY(int i) => MenuView.NodePosForTrailer(i).y;
            var start = NodeW(StartNode);

            // Sis (kaybolmuşluk): başlangıç çevresinde süzülen bulutlar; hedef parlayınca dağılır.
            var mist = new List<(SpriteRenderer r, Vector3 p, float a, float drift)>();
            for (int i = 0; i < 16; i++)
            {
                float ang = R(0f, Mathf.PI * 2f), dist = R(1.6f, 6.5f);
                var p = start + new Vector3(Mathf.Cos(ang) * dist, R(0.2f, 1.9f), Mathf.Sin(ang) * dist * 0.8f - 0.5f);
                var r = Spr("Mist" + i, blob, Color.white, 5);
                float s = R(2.6f, 4.8f);
                r.transform.localScale = new Vector3(s * 1.4f, s, 1f);
                mist.Add((r, p, R(0.45f, 0.7f), R(0.15f, 0.4f)));
            }
            // Kilitli bölgenin üstünde süzülen bulutlar (hedefin ötesi bilinmeyen).
            for (int i = 0; i < 14; i++)
            {
                var p = target + new Vector3(R(-7f, 7f), R(0.8f, 2.8f), R(4f, 22f));
                var r = Spr("Cloud" + i, blob, Color.white, 5);
                float s = R(3f, 6f);
                r.transform.localScale = new Vector3(s * 1.5f, s, 1f);
                mist.Add((r, p, R(0.75f, 0.95f), R(0.05f, 0.2f)));
            }

            // Maskot: dünya uzayı tuvalinde onaylı kristal maskot (gözler bakar, kırpar; endişe; heyecan).
            const float Mw = 165f;
            var mgo = Go("IntroMascot");
            var crt = mgo.AddComponent<RectTransform>();
            var canvas = mgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            crt.sizeDelta = new Vector2(700f, 700f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            var hands = UiFactory.Rect("Hands", crt);
            UiFactory.Stretch(hands, 0, 0, 0, 0);
            var mascot = MascotView.Create(crt, new Vector2(0f, MascotView.FeetBelowAnchor(Mw)), hands, Mw);
            hands.SetAsLastSibling();
            mascot.SetWorried(true);
            // Konuşma balonu ("?" kayıp, "!" buldu).
            var bubble = UiFactory.Rect("Bubble", crt);
            UiFactory.Place(bubble, new Vector2(0.5f, 0.5f), new Vector2(118f, 250f), new Vector2(110f, 110f));
            var bImg = UiFactory.Image("Back", bubble, UiArt.Circle, Color.white);
            UiFactory.Stretch(bImg.rectTransform, 0, 0, 0, 0);
            var bo = bImg.gameObject.AddComponent<Outline>();
            bo.effectColor = new Color(0.04f, 0.24f, 0.23f, 0.9f); bo.effectDistance = new Vector2(4f, -4f);
            var tail = UiFactory.Image("Tail", bubble, UiArt.Circle, Color.white);
            UiFactory.Place(tail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-40f, -50f), new Vector2(30f, 30f));
            var qText = UiFactory.Title("Mark", bubble, "?", 86, new Color(0.04f, 0.3f, 0.29f), Color.white);
            UiFactory.Stretch(qText.rectTransform, 0, 0, 0, 6);
            qText.alignment = TMPro.TextAlignmentOptions.Center;
            bubble.localScale = Vector3.zero;
            foreach (var t in mgo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = IntroLayer;

            // Konfeti ve yıldızlar.
            var white = Keep(Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f));
            Color[] pal = { new Color(0.18f, 0.8f, 0.72f), new Color(1f, 0.82f, 0.25f), new Color(1f, 0.45f, 0.55f), new Color(0.62f, 0.4f, 0.95f), new Color(1f, 0.6f, 0.2f), Color.white };
            var conf = new List<(SpriteRenderer r, Vector3 p, Vector3 v, float spin, float flip, float w)>();
            var stars = new List<(SpriteRenderer r, Vector3 dir, float s)>();
            var dust = new List<(SpriteRenderer r, Vector3 p, Vector3 dir, float t0, float s)>();
            void Dust(Vector3 at, float t0, int n, float size)
            {
                for (int i = 0; i < n; i++)
                {
                    float ang = i / (float)n * Mathf.PI * 2f + R(-0.3f, 0.3f);
                    var r = Spr("Dust", blob, new Color(1f, 0.97f, 0.9f, 0f), 6);
                    dust.Add((r, at + new Vector3(Mathf.Cos(ang), 0.3f, Mathf.Sin(ang)) * 0.35f, new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)), t0, size * R(0.8f, 1.2f)));
                }
            }
            bool burst = false;

            // ---- Sesler (koddan) ----
            const int Sr = SoundBank.SampleRate;
            var events = new List<(float t, float[] data, float vol)>();
            float[] Synth(float dur, Func<float, float> f) { var a = new float[(int)(dur * Sr)]; for (int i = 0; i < a.Length; i++) a[i] = f(i / (float)Sr); return a; }
            var nr = new System.Random(5);
            float Nz() => (float)nr.NextDouble() * 2f - 1f;
            float[] Boing(float f0, float f1, float dur) { float ph = 0f; return Synth(dur, t => { float u = t / dur; float f = Mathf.Lerp(f0, f1, Mathf.Sqrt(u)) * (1f + 0.04f * Mathf.Sin(t * 70f)); ph += f / Sr; return Mathf.Sin(2f * Mathf.PI * ph) * Mathf.Exp(-u * 3.2f) * Mathf.Clamp01(t / 0.004f); }); }
            float[] Thump(float g) => Synth(0.14f, t => (Mathf.Sin(2f * Mathf.PI * (90f * t + 60f * (1f - Mathf.Exp(-t * 40f)) / 40f)) * 0.9f + Nz() * 0.25f * Mathf.Exp(-t * 60f)) * Mathf.Exp(-t * 26f) * g);
            float[] Bell(float f, float dur) => Synth(dur, t => (Mathf.Sin(2f * Mathf.PI * f * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 6f)) * Mathf.Exp(-t * 4.5f) * Mathf.Clamp01(t / 0.002f));
            float[] Blip(float f0, float f1) { float ph = 0f; return Synth(0.09f, t => { ph += Mathf.Lerp(f0, f1, t / 0.09f) / Sr; return Mathf.Sin(2f * Mathf.PI * ph) * Mathf.Sin(Mathf.PI * t / 0.09f); }); }
            float lp = 0f;
            float[] Rumble() => Synth(0.55f, t => { lp = Mathf.Lerp(lp, Nz(), 0.04f); return (lp * 3f + 0.5f * Mathf.Sin(2f * Mathf.PI * 55f * t)) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / 0.55f)) ; });
            float[] Pop() { float l2 = 0f; return Synth(0.25f, t => { l2 = Mathf.Lerp(l2, Nz(), 0.5f); return l2 * Mathf.Exp(-t * 30f) + 0.6f * Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 25f); }); }
            events.Add((0.38f, Blip(700f, 1150f), 0.35f));          // "?" balonu
            events.Add((1.25f, Bell(1318.5f, 1.2f), 0.30f));        // "!" buldu
            for (int i = 0; i < 5; i++) events.Add((1.3f + i * 0.07f, Bell(new[] { 880f, 1046.5f, 1318.5f, 1568f, 1760f }[i], 0.9f), 0.12f)); // ışık yükselir
            for (int k = 0; k < 4; k++) { events.Add((1.5f + 0.5f * k, Boing(260f + 25f * k, 520f + 40f * k, 0.22f), 0.32f)); events.Add((2.0f + 0.5f * k, Thump(1f), 0.35f)); }
            events.Add((3.0f, Rumble(), 0.5f));                     // taş yükselir
            events.Add((3.6f, Boing(220f, 900f, 0.45f), 0.38f));    // büyük sıçrama
            events.Add((4.5f, Thump(1.4f), 0.55f));                 // taşa iniş
            events.Add((4.5f, Pop(), 0.45f));                       // konfeti
            for (int i = 0; i < 4; i++) events.Add((4.52f + i * 0.06f, Bell(new[] { 1046.5f, 1318.5f, 1568f, 2093f }[i], 1.0f), 0.16f)); // sevinç
            events.Add((4.62f, Boing(420f, 760f, 0.16f), 0.22f));
            events.Add((5.06f, Boing(440f, 800f, 0.16f), 0.22f));
            events.Add((5.62f, Boing(300f, 1100f, 0.3f), 0.3f));
            var clips = new List<AudioClip>();
            int ev = 0;

            // ---- 3) Zaman çizelgesi ----
            float PedHeight(float t)
            {
                if (t < 3.0f) return 0f;
                float h = PedH * OutBack((t - 3.0f) / 0.45f);
                if (t > 4.5f && t < 4.62f) h -= 0.05f * Mathf.Sin(Mathf.PI * (t - 4.5f) / 0.12f);
                return h;
            }
            // Maskot: yer konumu, yükseklik, ezilme (x, y), takla, sapma (derece).
            void MascotAt(float t, out Vector3 g, out float h, out float sx, out float sy, out float roll, out float yaw)
            {
                h = 0f; sx = sy = 1f; roll = 0f; yaw = 0f;
                if (t < 1.5f)
                {
                    g = start;
                    // Kaybolmuş: sola, sağa bakınır; "!" ile irkilir, zıplamaya hazırlanır.
                    yaw = -32f * S01((t - 0.2f) / 0.2f) + 64f * S01((t - 0.62f) / 0.22f) - 32f * S01((t - 1.02f) / 0.16f);
                    if (t > 1.25f && t < 1.36f) { float u = (t - 1.25f) / 0.11f; h = 0.18f * Mathf.Sin(Mathf.PI * u); sy = 1f + 0.08f * Mathf.Sin(Mathf.PI * u); }
                    if (t >= 1.36f) { float u = (t - 1.36f) / 0.14f; sy = 1f - 0.14f * Mathf.Sin(Mathf.PI * 0.5f * u); }
                    sx = 2f - sy;
                    return;
                }
                if (t < 3.5f)
                {
                    int k = Mathf.Min(3, (int)((t - 1.5f) / 0.5f));
                    float u = (t - 1.5f - 0.5f * k) / 0.5f;
                    float ya = NodeY(StartNode + k), yb = NodeY(StartNode + k + 1);
                    g = RoadW(Mathf.Lerp(ya, yb, u));
                    h = 0.8f * 4f * u * (1f - u);
                    sy = u < 0.1f ? 0.86f + 0.14f * u / 0.1f + 0.0f : 1f + 0.1f * Mathf.Sin(Mathf.PI * u);
                    if (k == 0 && u < 0.1f) sy = 0.86f + 0.14f * u / 0.1f;
                    sx = 2f - sy;
                    yaw = 10f * Mathf.Sin(Mathf.PI * 2f * (t - 1.5f));
                    return;
                }
                float ped = PedHeight(t);
                if (t < 3.62f)
                {
                    g = NodeW(IntroTarget - 1);
                    float u = (t - 3.5f) / 0.12f;
                    sy = 1f - 0.18f * Mathf.Sin(Mathf.PI * 0.5f * u); sx = 2f - sy;
                    return;
                }
                if (t < 4.5f)
                {
                    float u = (t - 3.62f) / 0.88f;
                    g = RoadW(Mathf.Lerp(NodeY(IntroTarget - 1), NodeY(IntroTarget), S01(u)));
                    h = PedH * S01(u) + 2.5f * 4f * u * (1f - u);
                    roll = -360f * S01((u - 0.18f) / 0.6f);
                    sy = 1f + 0.12f * Mathf.Sin(Mathf.PI * u); sx = 2f - sy;
                    return;
                }
                g = target;
                h = ped;
                if (t < 4.62f) { float u = (t - 4.5f) / 0.12f; sy = 1f - 0.2f * Mathf.Sin(Mathf.PI * u); }
                else if (t < 5.46f)
                {
                    float u = ((t - 4.62f) % 0.42f) / 0.42f;
                    h += 0.55f * 4f * u * (1f - u);
                    sy = u < 0.12f ? 0.88f + u : 1f + 0.08f * Mathf.Sin(Mathf.PI * u);
                    yaw = 14f * Mathf.Sin(Mathf.PI * 2f * (t - 4.62f) / 0.84f);
                }
                else if (t < 5.6f) { float u = (t - 5.46f) / 0.14f; sy = 1f - 0.16f * Mathf.Sin(Mathf.PI * 0.5f * u); }
                else { float u = (t - 5.6f) / 0.4f; h += 1.3f * S01(u * 1.2f); sy = 1.12f; }
                sx = 2f - sy;
            }

            // Kamera: odak çevresinde açı (0 = güneyden kuzeye bakar), yükselim, uzaklık. Sona doğru öne geçip dalar.
            float[][] camKeys =
            {
                new[] { 0.0f, -38f, 60f, 15.5f },
                new[] { 1.5f, -24f, 42f, 8.6f },
                new[] { 2.5f, 60f, 33f, 7.9f },
                new[] { 3.5f, 158f, 27f, 7.3f },
                new[] { 4.5f, 176f, 22f, 6.6f },
                new[] { 5.5f, 183f, 19f, 5.9f },
                new[] { 6.0f, 186f, 11f, 2.3f },
            };
            Vector3 focus = start + Vector3.up * 0.9f;
            int frames = Mathf.RoundToInt(IntroSeconds * Fps);
            for (int f = 0; f < frames; f++)
            {
                float t = f / (float)Fps;
                bool At(float x) => t <= x + 1e-4f && x < t + 1f / Fps - 1e-4f;
                MascotAt(t, out var g, out var h, out var sx, out var sy, out var roll, out var yaw);
                float ped = PedHeight(t);
                bool onPed = t >= 4.5f;
                var feet = g + Vector3.up * (h + 0.02f);

                // Kamera.
                var key = Keys(camKeys, t);
                Vector3 want = t < 3.4f ? g + Vector3.up * 0.95f : Vector3.Lerp(g, target, S01((t - 3.4f) / 0.6f)) + Vector3.up * (0.95f + ped);
                if (t >= 5.5f) want = feet + Vector3.up * 0.9f;
                focus = f == 0 ? want : Vector3.Lerp(focus, want, t >= 5.5f ? 0.45f : 0.16f);
                float az = key.x * Mathf.Deg2Rad, el = key.y * Mathf.Deg2Rad, dist = key.z;
                var camPos = focus + dist * new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), -Mathf.Cos(az) * Mathf.Cos(el));
                cam.transform.position = camPos;
                cam.transform.rotation = Quaternion.LookRotation(focus - camPos, Vector3.up);

                // Maskot (kameraya dönük; yüksekten bakarken hafif geriye yaslanır).
                var toM = feet - camPos;
                var flat = new Vector3(toM.x, 0f, toM.z).normalized;
                var look = Quaternion.LookRotation(Vector3.Lerp(flat, toM.normalized, 0.3f).normalized, Vector3.up) * Quaternion.Euler(0f, yaw, 0f);
                var full = look * Quaternion.Euler(0f, 0f, roll);
                var c = Vector3.up * 0.9f;
                mgo.transform.SetPositionAndRotation(feet + look * c - full * c, full);
                mgo.transform.localScale = new Vector3(U * sx, U * sy, U);
                if (At(1.25f)) { mascot.SetWorried(false); mascot.Excite(2); }
                if (At(4.5f)) mascot.Excite(3);
                mascot.Advance(1000f / Fps);

                // Balon.
                float bs = 0f;
                if (t >= 0.38f && t < 1.2f) bs = OutBack((t - 0.38f) / 0.22f) * (1f - S01((t - 1.08f) / 0.12f));
                if (t >= 1.25f && t < 1.75f) bs = OutBack((t - 1.25f) / 0.16f) * (1f - S01((t - 1.6f) / 0.15f));
                if (t >= 1.25f) qText.text = "!";
                bubble.localScale = Vector3.one * bs;
                bubble.localRotation = Quaternion.Euler(0f, 0f, t < 1.2f ? 8f * Mathf.Sin(t * 9f) : 0f);

                // Gölge: altındaki yüzeyde, yükseldikçe küçülür ve solar.
                float surf = (Vector3.Distance(new Vector3(g.x, 0f, g.z), new Vector3(target.x, 0f, target.z)) < PedR * 0.95f) ? ped : 0f;
                float above = Mathf.Max(0f, feet.y - surf);
                float ss = 1.25f / (1f + above * 0.45f);
                shadow.transform.position = new Vector3(g.x, surf + 0.012f, g.z);
                shadow.transform.localScale = new Vector3(ss, ss * 0.75f, 1f);

                // Hedef taş.
                for (int i = 0; i < Discs; i++)
                {
                    discs[i].gameObject.SetActive(ped > 0.01f);
                    discs[i].position = target + Vector3.up * (ped * i / (Discs - 1f) + 0.004f);
                }

                // Işık sütunu ve halka (hedef belirir).
                float beamA = t < 1.25f ? 0f : S01((t - 1.25f) / 0.25f) * (1f - S01((t - 2.9f) / 0.5f));
                var toB = target - camPos; toB.y = 0f;
                beam.transform.SetPositionAndRotation(target + Vector3.up * 3.2f, Quaternion.LookRotation(toB.normalized, Vector3.up));
                beam.transform.localScale = new Vector3(1.7f * (0.9f + 0.1f * Mathf.Sin(t * 12f)), 6.4f, 1f);
                Tint(beamMat, new Color(1f, 0.9f, 0.6f, 0.75f * beamA));
                float rp = t < 1.25f ? -1f : ((t - 1.25f) % 0.7f) / 0.7f;
                ring.gameObject.SetActive(rp >= 0f && t < 3.4f);
                ring.transform.position = target + Vector3.up * 0.03f;
                ring.transform.localScale = Vector3.one * (1.2f + 2.4f * rp);
                Tint(ringMat, new Color(1f, 0.9f, 0.6f, 0.8f * (1f - rp) * beamA));

                // Sis ve bulutlar (kameraya dönük; yakındakiler solar; sis hedef görününce dağılır).
                for (int i = 0; i < mist.Count; i++)
                {
                    var m = mist[i];
                    var p = m.p + new Vector3(m.drift * t, 0.08f * Mathf.Sin(t * 1.3f + i), 0f);
                    bool isMist = i < 16;
                    float a = m.a * (isMist ? 1f - S01((t - 1.25f) / 0.8f) : 1f);
                    float dc = Vector3.Distance(p, camPos);
                    a *= Mathf.Clamp01((dc - 2.2f) / 3f);
                    if (isMist) p += (p - start).normalized * 2.5f * S01((t - 1.25f) / 0.8f);
                    m.r.transform.SetPositionAndRotation(p, cam.transform.rotation);
                    m.r.color = new Color(1f, 1f, 1f, a);
                    m.r.enabled = a > 0.003f;
                }

                // Toz: her inişte.
                if (At(4.5f)) Dust(target + Vector3.up * ped, t, 10, 0.9f);
                for (int k = 1; k <= 4; k++) if (At(1.5f + 0.5f * k)) Dust(NodeW(StartNode + k), t, 7, 0.6f);
                if (At(3.0f)) Dust(target, t, 14, 1.1f);
                foreach (var d in dust)
                {
                    float u = (t - d.t0) / 0.6f;
                    if (u < 0f || u > 1f) { d.r.enabled = false; continue; }
                    d.r.enabled = true;
                    d.r.transform.SetPositionAndRotation(d.p + d.dir * 0.9f * Mathf.Sqrt(u) + Vector3.up * 0.25f * u, cam.transform.rotation);
                    d.r.transform.localScale = Vector3.one * d.s * (0.4f + 0.8f * u);
                    d.r.color = new Color(1f, 0.97f, 0.9f, 0.7f * (1f - u) * (1f - u));
                }

                // Sevinç patlaması: konfeti ve yıldızlar.
                if (!burst && t >= 4.5f)
                {
                    burst = true;
                    var head = target + Vector3.up * (PedH + 1.4f);
                    for (int i = 0; i < 80; i++)
                    {
                        var r = Spr("Confetti", white, pal[i % pal.Length], 8);
                        float ang = R(0f, Mathf.PI * 2f);
                        conf.Add((r, head, new Vector3(Mathf.Cos(ang) * R(0.8f, 3.2f), R(3.5f, 7.5f), Mathf.Sin(ang) * R(0.8f, 3.2f)), R(-720f, 720f), R(4f, 14f), R(0.10f, 0.17f)));
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        var r = Spr("Star", MascotView.CreamStar, new Color(1f, 0.86f, 0.3f), 9);
                        float ang = i / 10f * Mathf.PI * 2f;
                        stars.Add((r, new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f), R(0.35f, 0.55f)));
                    }
                }
                for (int i = 0; i < conf.Count; i++)
                {
                    var q = conf[i];
                    q.v += Vector3.down * 9f / Fps;
                    q.v *= 1f - 1.6f / Fps;
                    q.p += q.v / Fps;
                    conf[i] = q;
                    float tt = t - 4.5f;
                    q.r.transform.SetPositionAndRotation(q.p, cam.transform.rotation * Quaternion.Euler(0f, 0f, q.spin * tt));
                    q.r.transform.localScale = new Vector3(q.w * Mathf.Cos(q.flip * tt), q.w * 0.6f, 1f);
                }
                foreach (var s in stars)
                {
                    float u = (t - 4.5f) / 0.8f;
                    s.r.enabled = u >= 0f && u <= 1f;
                    if (!s.r.enabled) continue;
                    var center = target + Vector3.up * (PedH + 0.9f);
                    var off = cam.transform.rotation * s.dir * (0.6f + 2.2f * Mathf.Sqrt(u));
                    s.r.transform.SetPositionAndRotation(center + off, cam.transform.rotation * Quaternion.Euler(0f, 0f, 200f * u));
                    s.r.transform.localScale = Vector3.one * s.s * (u < 0.15f ? u / 0.15f : 1f - (u - 0.15f) / 0.85f * 0.6f);
                    s.r.color = new Color(1f, 0.86f, 0.3f, 1f - u * u);
                }

                // Ses olayları bu karede.
                while (ev < events.Count && events[ev].t <= t + 0.5f / Fps)
                {
                    var e = events[ev++];
                    if (SoundBank.CaptureTap == null) continue;
                    var clip = AudioClip.Create("intro" + ev, Mathf.Max(1, e.data.Length), 1, Sr, false);
                    clips.Add(clip);
                    SoundBank.Samples[clip] = e.data;
                    SoundBank.CaptureTap(clip, 1f, e.vol);
                }
                rec();
            }
            foreach (var cl in clips) UnityEngine.Object.DestroyImmediate(cl);
        }

        /// <summary>
        /// Açılış müziği (6 sn, 120 BPM, La minör): 0–1,5 sn yalnız müzik kutusu (kaybolmuşluk, yankılı); 1,25'te hedef
        /// parlayınca yükselen çan; 1,5–4,5 her zıplamada tıngırdayan bas ve 8'lik tıkırtı; 4,5'te sevinç akoru,
        /// 4,5–6 gerilim (yükselen gürültü + hızlanan trampet) → 6. sn'de ana müziğin sert girişi (oyun başlar).
        /// </summary>
        private static float[] IntroMusic(float seconds)
        {
            const int Sr = SoundBank.SampleRate;
            int n = (int)(seconds * Sr);
            var L = new float[n]; var R = new float[n];
            float Hz(int m) => 440f * Mathf.Pow(2f, (m - 69) / 12f);
            void Add(float[] b, float t0, float dur, Func<float, float> v, float g)
            {
                int s0 = (int)(t0 * Sr), len = (int)(dur * Sr);
                for (int k = 0; k < len && s0 + k < b.Length; k++) if (s0 + k >= 0) b[s0 + k] += v(k / (float)Sr) * g;
            }
            var rnd = new System.Random(9);
            float Nz() => (float)rnd.NextDouble() * 2f - 1f;
            Func<float, float> Box(float f) => t => (Mathf.Sin(2f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 3.01f * t) * Mathf.Exp(-t * 9f)) * Mathf.Exp(-t * 2.6f) * Mathf.Clamp01(t / 0.003f);
            void Note(float t0, int m, float g, bool echo = true)
            {
                Add(L, t0, 2f, Box(Hz(m)), g); Add(R, t0 + 0.008f, 2f, Box(Hz(m)), g * 0.9f);
                if (echo) { Add(R, t0 + 0.25f, 2f, Box(Hz(m)), g * 0.35f); Add(L, t0 + 0.5f, 2f, Box(Hz(m)), g * 0.18f); }
            }
            // Kayıp: yalnız, soran ezgi (A5 C6 E6 … D6 B5?).
            int[] mel = { 81, 84, 88, 86, 83 };
            float[] at = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
            for (int i = 0; i < mel.Length; i++) Note(at[i], mel[i], 0.11f);
            // Alçak pad (Am), 0–1,5.
            foreach (var m in new[] { 45, 52, 57 })
            {
                float f = Hz(m);
                Add(L, 0f, 1.6f, t => Mathf.Sin(2f * Mathf.PI * f * 0.998f * t) * Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((1.6f - t) / 0.3f), 0.05f);
                Add(R, 0f, 1.6f, t => Mathf.Sin(2f * Mathf.PI * f * 1.002f * t) * Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((1.6f - t) / 0.3f), 0.05f);
            }
            // Buldu: yükselen çan arpeji.
            int[] up = { 81, 84, 88, 93, 96 };
            for (int i = 0; i < up.Length; i++) Note(1.25f + i * 0.06f, up[i], 0.07f, false);
            // Yolculuk: her vuruşta zıplayan bas + akor tıngırtısı, 8'lik tıkırtı (Am F C G).
            int[] bass = { 45, 41, 48, 43, 45, 41 };
            int[][] ch = { new[] { 69, 72, 76 }, new[] { 65, 69, 72 }, new[] { 72, 76, 79 }, new[] { 67, 71, 74 }, new[] { 69, 72, 76 }, new[] { 65, 69, 72 } };
            for (int b = 0; b < 6; b++)
            {
                float t0 = 1.5f + b * 0.5f;
                float fb = Hz(bass[b]);
                Func<float, float> bv = t => { float ph = fb * t * (1f + 0.6f * Mathf.Exp(-t * 30f)); float s = 0f; for (int h = 1; h <= 3; h++) s += Mathf.Sin(2f * Mathf.PI * ph * h) / (h * h); return s * Mathf.Exp(-t * 7f) * Mathf.Clamp01(t / 0.003f); };
                Add(L, t0, 0.45f, bv, 0.22f); Add(R, t0, 0.45f, bv, 0.22f);
                foreach (var m in ch[b]) { Add(L, t0 + 0.25f, 0.4f, Box(Hz(m)), 0.035f); Add(R, t0 + 0.26f, 0.4f, Box(Hz(m)), 0.035f); }
                for (int e = 0; e < 2; e++) { Func<float, float> hat = t => Nz() * Mathf.Exp(-t * 90f); Add(L, t0 + e * 0.25f + 0.125f, 0.05f, hat, 0.04f); Add(R, t0 + e * 0.25f + 0.125f, 0.05f, hat, 0.035f); }
            }
            // Sevinç akoru 4,5.
            foreach (var m in new[] { 81, 84, 88, 93 }) Note(4.5f, m, 0.07f);
            // Gerilim 4,5–6: yükselen süzgeçli gürültü ve hızlanan trampet.
            float prev = 0f;
            Add(L, 4.5f, 1.5f, t => { float a = Mathf.Lerp(0.02f, 0.5f, t / 1.5f); prev = Mathf.Lerp(prev, Nz(), a); return prev * (t / 1.5f) * (t / 1.5f); }, 0.4f);
            prev = 0f;
            Add(R, 4.5f, 1.5f, t => { float a = Mathf.Lerp(0.02f, 0.5f, t / 1.5f); prev = Mathf.Lerp(prev, Nz(), a); return prev * (t / 1.5f) * (t / 1.5f); }, 0.4f);
            float tr = 5.0f; int hits = 0;
            while (tr < 5.98f && hits < 40)
            {
                float step = Mathf.Lerp(0.125f, 0.035f, (tr - 5.0f) / 1.0f);
                float g = 0.05f + 0.1f * (tr - 5.0f);
                Func<float, float> sn = t => (Nz() * 0.8f + 0.4f * Mathf.Sin(2f * Mathf.PI * 190f * t)) * Mathf.Exp(-t * 40f);
                Add(L, tr, 0.08f, sn, g); Add(R, tr + 0.003f, 0.08f, sn, g);
                tr += step; hits++;
            }
            var o = Normalize(L, R, n);
            for (int i = 0; i < o.Length; i++) o[i] *= 0.85f;
            return o;
        }

        /// <summary>Açılış müziği + ana vurucu müzik (açılış bitince sert giriş).</summary>
        private static float[] IntroAndPunchMusic(float punchSeconds)
        {
            var a = IntroMusic(IntroSeconds);
            var b = PunchMusic(punchSeconds);
            var o = new float[a.Length + b.Length];
            Array.Copy(a, o, a.Length);
            Array.Copy(b, 0, o, a.Length, b.Length);
            return o;
        }
    }
}
