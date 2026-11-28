using System;
using System.Collections.Generic;
using KatKat.UI;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.Game
{
    /// <summary>
    /// Stone Burst yükleme ekranı (2026-11-28; kullanıcı: "ofigame açılış ekranımızdan sonra stone burst logolu loading ekranı
    /// gelecek. Bu ekran tasarımı bir zarf gibi olacak üst tarafta zarfın kapağı ve zarfın kapağının ucunda stone burst logomuz.
    /// Loading %100 olunca zarf kapağı kalkacak ve oyun anasayfası açılacak. animasyon ile"; zemin: "Parlak Mor Kavisli Gradyan.png",
    /// tasarım kullanıcının örnek görseli: kapağın ucunda macenta mum mühür üstünde logo, altta turkuaz çizgili yükleme çubuğu).
    /// Zemin görselinin kavisin üstü kapaktır: aynı görsel kavisi izleyen bir ağla ayrıca çizilir (kapak), altında zarfın koyu içi.
    /// %100'de mühür sıçrar, kapak menteşesinden yukarı katlanır, zarf aşağı kayıp söner, altında hazır ana sayfa görünür.
    /// </summary>
    public sealed class LoadingEnvelope : MonoBehaviour
    {
        /// <summary>Hedef ilerleme (0–1); gösterilen değer yumuşakça yetişir.</summary>
        public float Target { get; set; }
        /// <summary>Oyun hazır: gösterilen %100'e varınca açılış animasyonu başlar.</summary>
        public bool Ready { get; set; }
        public float Shown => _shown;
        public event Action Done;

        private const float MinShow = 1.1f, OpenDur = 1.05f;
        /// <summary>Zemin görselinde (941×1672) kapağın alt kenarı: y = 889 − 340u² + 90u⁴, u = |x − 470,5| / 470,5 (ölçüldü).</summary>
        private const float ImgW = 941f, ImgH = 1672f;
        private const int CurveSteps = 48;

        private RectTransform _root, _flap, _seal, _body;
        private CanvasGroup _group;
        private RectTransform _fill;
        private RawImage _stripes;
        private Graphic _shade;
        private float _shown, _age, _openT = -1f;
        private float _w = 1080f, _h = 1920f;

        /// <param name="cam">Yakalama aracı için kamera (null: ekran üstü).</param>
        public static LoadingEnvelope Create(Camera cam = null)
        {
            var go = new GameObject("LoadingEnvelope", typeof(RectTransform));
            if (cam == null && Application.isPlaying) DontDestroyOnLoad(go);
            var env = go.AddComponent<LoadingEnvelope>();
            env.Build(cam);
            return env;
        }

        /// <summary>Kapağın alt kenarı (zemin görselinin üstünden oran, 0–1) yatay orana (0–1) göre.</summary>
        private static float CurveFromTop(float xn)
        {
            float u = Mathf.Abs(xn * ImgW - ImgW * 0.5f) / (ImgW * 0.5f);
            return (889f - 340f * u * u + 90f * u * u * u * u) / ImgH;
        }

        private float SealY => -(CurveFromTop(0.5f) * _h - _seal.sizeDelta.x * 0.2f);

        private void Build(Camera cam)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = cam != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 999; // ofigame açılışının (1000) hemen altında: açılış sönünce görünür
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = UiAspect.Match;
            gameObject.AddComponent<GraphicRaycaster>(); // açılana dek alttaki ana sayfaya dokunulmasın
            _group = gameObject.AddComponent<CanvasGroup>();
            _root = (RectTransform)transform;
            Canvas.ForceUpdateCanvases();
            var size = _root.rect.size;
            if (size.x > 10f && size.y > 10f) { _w = size.x; _h = size.y; }

            var bgTex = Res.Load<Texture2D>("Branding/yukleme-zarf");
            if (bgTex != null) bgTex.wrapMode = TextureWrapMode.Clamp;

            // Gövde: zemin görselinin tamamı; kapak bölgesinin üstünde zarfın koyu içi (kapak kalkınca görünür).
            _body = UiFactory.Rect("Body", _root);
            Stretch(_body);
            // 2026-11-28 (kullanıcı: "üst kapak kalktığında siyah bir zemin yerine oyunun anasayfasının denk gelen kısmı olsun"):
            // gövde yalnız kavisin altı; kapak bölgesinde zarf boştur, kapak kalkınca alttaki ana sayfa görünür. Açılana dek dokunuşu
            // tam ekran görünmez bir katman tutar.
            var full = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            var blocker = Poly("Blocker", _body, full, null, new[] { 0, 1, 2, 0, 2, 3 });
            blocker.color = new Color(1f, 1f, 1f, 0f);
            blocker.raycastTarget = true;
            var (fpts, ftris) = FlapMesh();
            var (bpts, btris) = BodyMesh();
            Poly("Back", _body, bpts, bgTex, btris);

            // Yükleme çubuğu (örnekteki gibi: koyu mor oluk, pembe kenar; turkuaz dolgu, kayan çizgiler).
            var track = UiFactory.Image("Track", _body, Ui3.Rounded(22f, Hex("#3B0A5C"), Hex("#22053A"), 4f, Hex("#E978F2")), Color.white);
            track.type = Image.Type.Sliced;
            var tr = track.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0f);
            float trackW = Mathf.Min(680f, _w * 0.63f);
            tr.sizeDelta = new Vector2(trackW, 46f); // 2026-11-28 kullanıcı: "Loadingi inceltelim ve birazdaha aşağıya alalım"
            tr.anchoredPosition = new Vector2(0f, _h * 0.27f);
            var fillImg = UiFactory.Image("Fill", tr, Ui3.Rounded(16f, Hex("#7EF5EA"), Hex("#16AFA8")), Color.white);
            fillImg.type = Image.Type.Sliced;
            _fill = fillImg.rectTransform;
            _fill.anchorMin = new Vector2(0f, 0f); _fill.anchorMax = new Vector2(0f, 1f); _fill.pivot = new Vector2(0f, 0.5f);
            _fill.offsetMin = new Vector2(7f, 7f); _fill.offsetMax = new Vector2(7f, -7f);
            fillImg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _stripes = new GameObject("Stripes", typeof(RectTransform)).AddComponent<RawImage>();
            _stripes.transform.SetParent(_fill, false);
            _stripes.texture = StripeTexture();
            _stripes.raycastTarget = false;
            var sr = _stripes.rectTransform;
            sr.anchorMin = Vector2.zero; sr.anchorMax = new Vector2(0f, 1f); sr.pivot = new Vector2(0f, 0.5f);
            sr.offsetMin = Vector2.zero; sr.offsetMax = new Vector2(trackW, 0f);
            _stripes.uvRect = new Rect(0f, 0f, trackW / 34f, 1f);
            var shine = UiFactory.Image("Shine", _fill, Ui3.Rounded(8f, new Color(1f, 1f, 1f, 0.45f), new Color(1f, 1f, 1f, 0.05f)), Color.white);
            shine.type = Image.Type.Sliced;
            var shr = shine.rectTransform;
            shr.anchorMin = new Vector2(0f, 0.55f); shr.anchorMax = new Vector2(1f, 0.92f); shr.offsetMin = new Vector2(7f, 0f); shr.offsetMax = new Vector2(-7f, 0f);

            // Kapak: aynı görselin kavis üstü (menteşe üst kenarda; açılırken scaleY 1 → −1).
            _flap = UiFactory.Rect("Flap", _root);
            Stretch(_flap);
            _flap.pivot = new Vector2(0.5f, 1f);
            _flap.anchoredPosition = Vector2.zero;
            Poly("Face", _flap, fpts, bgTex, ftris);
            _shade = Poly("Shade", _flap, fpts, null, ftris);
            _shade.color = new Color(0.08f, 0f, 0.15f, 0f);

            // Mum mühür: kapağın ucunun biraz üstünde (örnekteki gibi büyük); üstünde Stone Burst logosu.
            _seal = UiFactory.Rect("Seal", _root);
            _seal.anchorMin = _seal.anchorMax = new Vector2(0.5f, 1f);
            float sealD = Mathf.Min(560f, _w * 0.5f);
            _seal.sizeDelta = new Vector2(sealD, sealD);
            _seal.anchoredPosition = new Vector2(0f, SealY);
            var wax = new GameObject("Wax", typeof(RectTransform)).AddComponent<RawImage>();
            wax.transform.SetParent(_seal, false);
            wax.texture = SealTexture();
            wax.raycastTarget = false;
            var wr = wax.rectTransform;
            wr.anchorMin = wr.anchorMax = new Vector2(0.5f, 0.5f);
            wr.sizeDelta = new Vector2(sealD * 1.18f, sealD * 1.18f); // dokuda gölge payı
            var logoSprite = PremiumArt.V4("ui/logo-stoneburst");
            var logo = UiFactory.Image("Logo", _seal, logoSprite, Color.white);
            logo.preserveAspect = true;
            float lw = sealD * 0.74f;
            float aspect = logoSprite != null ? logoSprite.rect.width / Mathf.Max(1f, logoSprite.rect.height) : 1.53f;
            logo.rectTransform.sizeDelta = new Vector2(lw, lw / aspect);
            logo.rectTransform.anchoredPosition = new Vector2(0f, sealD * 0.01f);
            SetProgress(0f);
        }

        /// <summary>Gövde bölgesi (kavis ile alt kenar arası) şerit üçgenleri; noktalar dikdörtgen oranında (0–1, alt köken).</summary>
        private static (Vector2[] pts, int[] tris) BodyMesh()
        {
            var pts = new Vector2[(CurveSteps + 1) * 2];
            var tris = new List<int>();
            for (int i = 0; i <= CurveSteps; i++)
            {
                float x = i / (float)CurveSteps;
                pts[i * 2] = new Vector2(x, 1f - CurveFromTop(x));
                pts[i * 2 + 1] = new Vector2(x, 0f);
                if (i == 0) continue;
                int a = (i - 1) * 2, b = i * 2;
                tris.AddRange(new[] { a, a + 1, b + 1, a, b + 1, b });
            }
            return (pts, tris.ToArray());
        }

        /// <summary>Kapak bölgesi (üst kenar ile kavis arası) şerit üçgenleri; noktalar dikdörtgen oranında (0–1, alt köken).</summary>
        private static (Vector2[] pts, int[] tris) FlapMesh()
        {
            var pts = new Vector2[(CurveSteps + 1) * 2];
            var tris = new List<int>();
            for (int i = 0; i <= CurveSteps; i++)
            {
                float x = i / (float)CurveSteps;
                pts[i * 2] = new Vector2(x, 1f);
                pts[i * 2 + 1] = new Vector2(x, 1f - CurveFromTop(x));
                if (i == 0) continue;
                int a = (i - 1) * 2, b = i * 2;
                tris.AddRange(new[] { a, a + 1, b + 1, a, b + 1, b });
            }
            return (pts, tris.ToArray());
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            _age += dt;
            if (_stripes != null) { var r = _stripes.uvRect; r.x -= dt * 0.9f; _stripes.uvRect = r; }
            if (_openT >= 0f) { TickOpen(dt); return; }
            float cap = Ready ? 1f : Mathf.Min(0.97f, Target);
            float speed = Ready ? 1.6f : 0.9f;
            _shown = Mathf.MoveTowards(_shown, cap, speed * dt);
            if (!Ready) _shown = Mathf.Min(_shown, Mathf.Lerp(0f, 0.97f, _age / MinShow));
            SetProgress(_shown);
            float b = 1f + 0.02f * Mathf.Sin(_age * 2.6f); // mühür çok hafif nefes alır
            _seal.localScale = new Vector3(b, b, 1f);
            if (Ready && _shown >= 0.999f && _age >= MinShow) _openT = 0f;
        }

        /// <summary>
        /// Oyun sahnesi etkinleşirken çağrılır: ana sayfa (ya da ilk bölüm) hazır olana dek %60–95, sonra %100 ve zarf açılır.
        /// Eşyordam zarfın üstünde çalışır (DontDestroyOnLoad); Boot sahnesinin silinmesinden etkilenmez. En çok 8 sn bekler.
        /// </summary>
        public void WaitForGame() => StartCoroutine(WaitForGameRoutine());

        private System.Collections.IEnumerator WaitForGameRoutine()
        {
            float w = 0f, readyFor = 0f;
            GameController gc = null;
            while (w < 8f)
            {
                w += Time.unscaledDeltaTime;
                if (gc == null) gc = FindFirstObjectByType<GameController>();
                bool ready = gc != null && (gc.CurrentPhase != GameController.Phase.Loading || (gc.Menu != null && gc.Menu.IsOpen));
                readyFor = ready ? readyFor + Time.unscaledDeltaTime : 0f;
                Target = Mathf.Lerp(0.6f, 0.95f, Mathf.Clamp01(w / 1.2f));
                if (readyFor > 0.25f) break; // ilk karelerin takılması (ısınma) zarfın altında geçsin
                yield return null;
            }
            Ready = true;
        }

        /// <summary>Yakalama aracı: verilen ilerlemede ya da açılışın verilen anında (sn, ≥0) durur.</summary>
        public void PoseForTest(float progress, float openAt = -1f)
        {
            _shown = progress;
            SetProgress(progress);
            if (openAt < 0f) return;
            _openT = Mathf.Max(0f, openAt - 0.0001f);
            TickOpen(Mathf.Min(0.0001f, openAt));
        }

        private void SetProgress(float p)
        {
            float full = ((RectTransform)_fill.parent).rect.width - 14f;
            _fill.sizeDelta = new Vector2(Mathf.Max(30f, full * Mathf.Clamp01(p)), _fill.sizeDelta.y);
        }

        /// <summary>
        /// Açılış: 0–0,2 s mühür sıçrar, 0,1–0,65 s kapak menteşeden yukarı katlanır (arka yüzü koyulaşır), mühür kapağın ucuyla
        /// yükselip söner; 0,45–1,05 s zarf aşağı kayar ve söner.
        /// </summary>
        private void TickOpen(float dt)
        {
            _openT += dt;
            float t = _openT;
            float pop = t < 0.2f ? Mathf.Sin(Mathf.PI * t / 0.2f) * 0.12f : 0f;
            float fu = Mathf.Clamp01((t - 0.1f) / 0.55f);
            float fe = fu * fu * (3f - 2f * fu);
            float sy = Mathf.Lerp(1f, -1f, fe);
            _flap.localScale = new Vector3(1f, sy, 1f);
            _shade.color = new Color(0.08f, 0f, 0.15f, Mathf.Clamp01(sy < 0f ? 0.55f : 0.45f * (1f - sy)));
            float ls = (1f + pop) * Mathf.Lerp(1f, 0.7f, fe);
            _seal.localScale = new Vector3(ls, ls, 1f);
            _seal.anchoredPosition = new Vector2(0f, SealY * Mathf.Lerp(1f, 0.15f, fe) + 140f * fe);
            float sa = 1f - Mathf.Clamp01((t - 0.3f) / 0.35f);
            foreach (var g in _seal.GetComponentsInChildren<Graphic>()) { var c = g.color; c.a = sa; g.color = c; }
            float bu = Mathf.Clamp01((t - 0.45f) / 0.6f);
            _body.anchoredPosition = new Vector2(0f, -_h * 0.6f * bu * bu);
            _group.alpha = 1f - Mathf.Clamp01((t - 0.55f) / 0.5f);
            if (t >= OpenDur)
            {
                Done?.Invoke();
                if (Application.isPlaying) Destroy(gameObject);
            }
        }

        // ---- Dokular ---------------------------------------------------------------------------------------------------------

        /// <summary>Kayan çizgiler: çapraz açık şeritler (yatay tekrar).</summary>
        private static Texture2D StripeTexture()
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "load_stripes", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float v = Mathf.Repeat((x + y) / (float)S, 1f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.5f - Mathf.Abs(v - 0.5f) - 0.22f) / 0.05f));
                px[y * S + x] = new Color(1f, 1f, 1f, 0.28f * a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// Macenta mum mühür (örnek görseldeki gibi): dalgalı kenarlı (10 dilim) parlak disk, içte hafif kabarık halka, sol üstte
        /// parlama, altta yumuşak gölge. Doku 512², disk çapı dokunun %85'i (kalan gölge payı).
        /// </summary>
        private static Texture2D SealTexture()
        {
            const int S = 512;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "load_seal", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color[S * S];
            var light = Hex("#E64FE0"); var mid = Hex("#B417B5"); var dark = Hex("#760C80"); var rimDark = Hex("#560561");
            float R = S * 0.5f * 0.85f, cx = S * 0.5f, cy = S * 0.5f + 6f;
            float Edge(float ang) => R * (1f + 0.055f * Mathf.Cos(10f * ang));
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
                float e = Edge(ang);
                // Gölge (aşağı kaymış, yumuşak).
                float sdx = dx, sdy = dy + 18f;
                float sd = Mathf.Sqrt(sdx * sdx + sdy * sdy) - Edge(Mathf.Atan2(sdy, sdx));
                float shadowA = 0.45f * Mathf.Clamp01(1f - (sd + 4f) / 26f);
                var c = new Color(0.18f, 0.0f, 0.25f, shadowA);
                if (d < e + 1.2f)
                {
                    float k = d / e; // 0 merkez, 1 kenar
                    // Işık sol üstten: üst-sol açık, alt-sağ koyu.
                    float lightDir = Mathf.Clamp01(0.5f + 0.5f * (-dx * 0.55f + dy * 0.85f) / Mathf.Max(1f, e));
                    var body = Color.Lerp(mid, light, 0.4f * (1f - k) + 0.3f * lightDir);
                    body = Color.Lerp(body, dark, Step(0.82f, 1f, k) * 0.75f); // kabarık kenar
                    // İç halka: ince açık çizgi ve hemen dışında koyu çizgi (baskı izi).
                    float ring = Mathf.Exp(-Mathf.Pow((k - 0.80f) / 0.018f, 2f));
                    float ringD = Mathf.Exp(-Mathf.Pow((k - 0.835f) / 0.02f, 2f));
                    body = Color.Lerp(body, light, 0.55f * ring);
                    body = Color.Lerp(body, rimDark, 0.35f * ringD);
                    body = Color.Lerp(body, rimDark, Step(0.965f, 1f, k) * 0.8f); // kenar çizgisi
                    // Parlama: sol üstte yumuşak beyaz oval.
                    float hx = (dx + e * 0.32f) / (e * 0.42f), hy = (dy - e * 0.40f) / (e * 0.22f);
                    float hl = Mathf.Clamp01(1f - (hx * hx + hy * hy));
                    body = Color.Lerp(body, Color.white, 0.55f * hl * hl);
                    float a = Mathf.Clamp01(e + 1.2f - d);
                    body.a = 1f;
                    c = Color.Lerp(c, body, a);
                    c.a = Mathf.Max(shadowA, a);
                }
                px[y * S + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        // ---- Çizim yardımcıları -------------------------------------------------------------------------------------------------

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; r.pivot = new Vector2(0.5f, 0.5f);
        }

        private static PolyGraphic Poly(string name, RectTransform parent, Vector2[] pts, Texture tex, int[] tris)
        {
            var r = UiFactory.Rect(name, parent);
            Stretch(r);
            if (r.GetComponent<CanvasRenderer>() == null) r.gameObject.AddComponent<CanvasRenderer>(); // RequireComponent alt sınıfta işlemedi
            var g = r.gameObject.AddComponent<PolyGraphic>();
            g.Points = pts; g.Tex = tex; g.Triangles = tris; g.Normalized = true;
            g.raycastTarget = false;
            g.SetVerticesDirty(); g.SetMaterialDirty(); // noktalar eklemeden sonra verildi: ağı yeniden kur
            return g;
        }

        private static Color Hex(string s) { ColorUtility.TryParseHtmlString(s, out var c); return c; }

        /// <summary>Yumuşak eşik (e0–e1 arasında 0→1; Mathf.SmoothStep ara değerleme yapar, eşik değildir).</summary>
        private static float Step(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }
    }

    /// <summary>
    /// Köşe renkli / dokulu çokgen ağı. Normalized: noktalar dikdörtgen oranında (0–1, sol alt köken), doku koordinatı = nokta.
    /// Triangles yoksa yelpaze üçgenleme (dışbükey).
    /// </summary>
    public sealed class PolyGraphic : MaskableGraphic
    {
        public Vector2[] Points;
        public Color[] Colors;
        public int[] Triangles;
        public Texture Tex;
        public bool Normalized;

        public override Texture mainTexture => Tex != null ? Tex : s_WhiteTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Points == null || Points.Length < 3) return;
            var r = rectTransform.rect;
            var tint = color;
            for (int i = 0; i < Points.Length; i++)
            {
                var c = Colors != null && i < Colors.Length ? Colors[i] : Color.white;
                c *= tint;
                var p = Points[i];
                var pos = Normalized ? new Vector2(r.xMin + p.x * r.width, r.yMin + p.y * r.height) : new Vector2(r.xMin + p.x, r.yMin + p.y);
                vh.AddVert(new Vector3(pos.x, pos.y), c, Normalized ? p : Vector2.zero);
            }
            if (Triangles != null) { for (int i = 0; i + 2 < Triangles.Length; i += 3) vh.AddTriangle(Triangles[i], Triangles[i + 1], Triangles[i + 2]); }
            else for (int i = 1; i < Points.Length - 1; i++) vh.AddTriangle(0, i, i + 1);
        }
    }
}
