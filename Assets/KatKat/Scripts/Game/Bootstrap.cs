using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KatKat.Game
{
    /// <summary>
    /// Uygulama giriş noktası. Boot sahnesindeki tek nesnede bulunur; ayarları yapar, OFIGAME açılış ekranını
    /// gösterir (kullanıcı: "oyunun açılış ekranına OFIGAME'i koy") ve oyun sahnesini arka planda yükler.
    /// Beyaz zemin, ortada OFIGAME logosu: 0,35 s belirir, ~1,3 s kalır (logo hafifçe büyür), sahne hazırsa
    /// 0,3 s'de söner. Toplu kipte (testler) açılış ekranı atlanır.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] private string _firstScene = "Game";

        public static bool HasBooted { get; private set; }

        private const float FadeIn = 0.35f, Hold = 1.3f, FadeOut = 0.3f;
        /// <summary>OFIGAME logosunun kaynak en/boy oranı (docs/gorseller/arayuz/ofigame-logo.png, 2440×1490).</summary>
        public const float LogoAspect = 2440f / 1490f;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            // Gezinme ve durum çubukları gizli (kenardan sürükleyince geçici görünür); her dönüşte yeniden.
            ImmersiveMode.Ensure();
            HasBooted = true;
            Debug.Log("[KatKat] Boot tamam.");
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(_firstScene)) return;
            KatKat.View.ResourceUsage.Note("Branding/ofigame-logo");
            var logo = KatKat.View.Res.Load<Texture2D>("Branding/ofigame-logo");
            if (Application.isBatchMode || logo == null) { SceneManager.LoadScene(_firstScene); return; }
            StartCoroutine(Splash(logo));
        }

        private IEnumerator Splash(Texture2D logo)
        {
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = SplashEdge;
            var go = CreateSplash(logo, null, out var lr, out var group);
            go.name = "Splash";
            DontDestroyOnLoad(go);
            var splashGroup = go.AddComponent<CanvasGroup>();
            // 2026-11-28: ofigame açılışının ardından Stone Burst zarf yükleme ekranı (altta hazır bekler, açılış sönünce görünür).
            var env = LoadingEnvelope.Create();
            env.gameObject.SetActive(false);
            var load = SceneManager.LoadSceneAsync(_firstScene);
            load.allowSceneActivation = false;
            float t = 0f;
            // 2026-11-28 (kullanıcı: "Logo büyümesin sabit boyutta kalsın"): logo yalnız belirir, boyutu değişmez.
            while (t < FadeIn + Hold)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / FadeIn);
                env.Target = 0.35f * Mathf.Clamp01(load.progress / 0.9f);
                yield return null;
            }
            env.gameObject.SetActive(true);
            float f = 0f;
            while (f < FadeOut)
            {
                f += Time.unscaledDeltaTime;
                splashGroup.alpha = 1f - Mathf.Clamp01(f / FadeOut);
                env.Target = 0.35f * Mathf.Clamp01(load.progress / 0.9f);
                yield return null;
            }
            Destroy(go);
            while (load.progress < 0.9f) { env.Target = 0.6f * load.progress / 0.9f; yield return null; }
            env.Target = 0.6f;
            // 2026-11-28 düzeltme (kullanıcı: "%60 larda kaldı"): sahne açılınca bu nesne (Boot sahnesi) silinir ve eşyordam yarıda
            // kalıyordu; oyunun hazır olmasını sahne değişiminde yaşayan zarf kendisi bekler.
            env.WaitForGame();
            load.allowSceneActivation = true;
        }

        /// <summary>Açılış zemininin rengi (kamera arka planı da bu).</summary>
        private static readonly Color SplashEdge = Color.white;
        private static Texture2D _splashTile;
        /// <summary>Desen karosunun boyu (doku pikseli) ve ekrandaki boyu (referans birimi).</summary>
        private const int TileTex = 320;
        private const float TileUnits = 360f;

        /// <summary>
        /// 2026-11-28 (kullanıcı: "ofigame kapağını ise beğenmedim. alt kısımdaki farklı katman renkler olmasın beyaz zeminde sönük
        /// desenler olsun oyunları andıran ama telif olacak görseller kullanma"): beyaz zemin üstünde tekrar eden çok soluk çizgi
        /// simgeler: yön tuşu (artı), kumanda, zar, yıldız, kalp, halka, üçgen, kıvılcım. Hepsi koddan basit geometri; görsel yok.
        /// </summary>
        private static Texture2D SplashTile()
        {
            if (_splashTile != null) return _splashTile;
            const int S = TileTex;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "splash_pattern", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[S * S];
            var ink = new Color(0.62f, 0.66f, 0.74f);
            const float Alpha = 0.16f, Stroke = 3.2f;
            // Simge: tür, merkez (doku pikseli), boy (yarıçap), açı (derece).
            var icons = new (int kind, float x, float y, float r, float rot)[]
            {
                (0, 52, 58, 26, 12), (1, 190, 46, 34, -10), (2, 290, 120, 22, 18), (3, 118, 150, 24, -15),
                (4, 238, 214, 22, 8), (5, 52, 246, 20, 0), (6, 150, 272, 24, 22), (7, 290, 292, 12, 0),
                (7, 96, 96, 8, 30), (5, 236, 128, 10, 0), (7, 30, 160, 9, 15), (2, 300, 20, 12, -20),
            };
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float best = 1e9f;
                foreach (var ic in icons)
                {
                    // Karo sınırında tekrar: en yakın kopyaya göre.
                    float dx = x + 0.5f - ic.x, dy = y + 0.5f - ic.y;
                    dx -= Mathf.Round(dx / S) * S; dy -= Mathf.Round(dy / S) * S;
                    if (Mathf.Abs(dx) > ic.r * 1.9f || Mathf.Abs(dy) > ic.r * 1.9f) continue;
                    float a = -ic.rot * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    var p = new Vector2(dx * ca - dy * sa, dx * sa + dy * ca);
                    best = Mathf.Min(best, IconDistance(ic.kind, p, ic.r, Stroke));
                }
                float cov = Mathf.Clamp01(0.75f - best); // kenar yumuşatma (~1,5 px)
                var c = Color.Lerp(Color.white, ink, Alpha * cov);
                c.a = 1f;
                px[y * S + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return _splashTile = tex;
        }

        /// <summary>Simgenin çizgisine uzaklık (px; ≤0 çizginin içi). Çizgi simgeler: dış hat kalınlığı <paramref name="w"/>.</summary>
        private static float IconDistance(int kind, Vector2 p, float r, float w)
        {
            float Ring(float d) => Mathf.Abs(d) - w * 0.5f; // dolu şeklin sınırından çizgi
            switch (kind)
            {
                case 0: // yön tuşu (artı)
                {
                    float half = r * 0.36f;
                    return Ring(Mathf.Min(Box(p, new Vector2(r, half), half * 0.4f), Box(p, new Vector2(half, r), half * 0.4f)));
                }
                case 1: // kumanda: yuvarlak gövde + artı + iki düğme
                {
                    float body = Ring(Box(p, new Vector2(r, r * 0.55f), r * 0.5f));
                    var q = p - new Vector2(-r * 0.45f, 0f);
                    float plus = Mathf.Min(Box(q, new Vector2(r * 0.2f, r * 0.06f), 1f), Box(q, new Vector2(r * 0.06f, r * 0.2f), 1f));
                    float b1 = (p - new Vector2(r * 0.42f, r * 0.12f)).magnitude - r * 0.09f;
                    float b2 = (p - new Vector2(r * 0.6f, -r * 0.1f)).magnitude - r * 0.09f;
                    return Mathf.Min(body, Mathf.Min(plus, Mathf.Min(b1, b2)));
                }
                case 2: // yıldız
                    return Ring(Star(p, r, r * 0.45f));
                case 3: // zar: yuvarlak kare + üç nokta
                {
                    float box = Ring(Box(p, new Vector2(r, r), r * 0.28f));
                    float d1 = (p - new Vector2(-r * 0.45f, r * 0.45f)).magnitude - r * 0.13f;
                    float d2 = p.magnitude - r * 0.13f;
                    float d3 = (p - new Vector2(r * 0.45f, -r * 0.45f)).magnitude - r * 0.13f;
                    return Mathf.Min(box, Mathf.Min(d1, Mathf.Min(d2, d3)));
                }
                case 4: // kalp: iki daire + aşağı sivri üçgen
                {
                    var q = new Vector2(Mathf.Abs(p.x), p.y);
                    float c = (q - new Vector2(r * 0.42f, r * 0.2f)).magnitude - r * 0.5f;
                    float t = Tri(new Vector2(p.x, -p.y + r * 0.1f), r * 0.95f);
                    return Ring(Mathf.Min(c, t));
                }
                case 5: // halka
                    return Ring(p.magnitude - r);
                case 6: // üçgen
                    return Ring(Tri(p, r));
                default: // kıvılcım: dört kollu ince yıldız
                    return Ring(Star4(p, r));
            }
        }

        private static float Box(Vector2 p, Vector2 b, float round)
        {
            var d = new Vector2(Mathf.Abs(p.x) - b.x + round, Mathf.Abs(p.y) - b.y + round);
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - round;
        }

        /// <summary>Eşkenar üçgen (tepe yukarı), çevrel yarıçap ~r.</summary>
        private static float Tri(Vector2 p, float r)
        {
            const float k = 1.7320508f;
            float a = r * 0.866f;
            p.x = Mathf.Abs(p.x) - a;
            p.y = p.y + r * 0.5f;
            if (p.x + k * p.y > 0f) p = new Vector2(p.x - k * p.y, -k * p.x - p.y) / 2f;
            p.x -= Mathf.Clamp(p.x, -2f * a, 0f);
            return -p.magnitude * Mathf.Sign(p.y);
        }

        /// <summary>Beş köşeli yıldız (dış r, iç ri): kutupsal yaklaşık uzaklık.</summary>
        private static float Star(Vector2 p, float r, float ri)
        {
            float seg = Mathf.PI * 2f / 5f;
            float a = Mathf.Repeat(Mathf.Atan2(p.x, p.y) + seg * 0.5f, seg) - seg * 0.5f; // uç 0°
            float edge = Mathf.Lerp(r, ri, Mathf.Abs(a) / (seg * 0.5f));
            return (p.magnitude - edge) * 0.8f;
        }

        private static float Star4(Vector2 p, float r)
        {
            float seg = Mathf.PI * 0.5f;
            float a = Mathf.Repeat(Mathf.Atan2(p.y, p.x) + seg * 0.5f, seg) - seg * 0.5f;
            float edge = Mathf.Lerp(r, r * 0.3f, Mathf.Sqrt(Mathf.Abs(a) / (seg * 0.5f)));
            return (p.magnitude - edge) * 0.7f;
        }

        /// <summary>Açılış ekranı tuvali (cam null: ekran üstü; yakalama aracı kamera verir).</summary>
        public static GameObject CreateSplash(Texture2D logo, Camera cam, out RectTransform lr, out CanvasGroup group)
        {
            var go = new GameObject("Splash", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = cam != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 1000;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = KatKat.UI.UiAspect.Match; // iPad: yüksekliğe eşleme (UiAspect)
            // Beyaz zemin + soluk oyun simgesi deseni (karo tekrarı).
            var bg = new GameObject("Bg", typeof(RectTransform)).AddComponent<RawImage>();
            bg.transform.SetParent(go.transform, false);
            bg.texture = SplashTile();
            bg.color = Color.white;
            var bgr = bg.rectTransform;
            bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one; bgr.offsetMin = bgr.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            var area = ((RectTransform)go.transform).rect.size;
            if (area.x < 10f) area = new Vector2(1080f, 1920f);
            bg.uvRect = new Rect(0f, 0f, area.x / TileUnits, area.y / TileUnits);
            var img = new GameObject("Logo", typeof(RectTransform)).AddComponent<RawImage>();
            img.transform.SetParent(go.transform, false);
            img.texture = logo;
            lr = img.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
            // Kullanıcı (2026-11-08): "logo alttan üstten veya yandan basık olmasın". Doku içe aktarmada 2'nin kuvvetine
            // esnetilirse width/height bozuk oran verir; oran kaynak görselden (2440×1490) sabit alınır.
            // 2026-11-28 (kullanıcı: "ofigame logosunu tam ortalayalım ve biraz küçültelim"): 820 → 600; görünen kısım (saydam kenarlar
            // hariç, x 67–2356, y 113–1348 / 2440×1490) ekranın tam ortasına gelecek şekilde kaydırılır.
            float w = 600f, h = w / LogoAspect;
            lr.sizeDelta = new Vector2(w, h);
            lr.anchoredPosition = new Vector2((1220f - 1211.5f) / 2440f * w, (730.5f - 745f) / 1490f * h);
            group = img.gameObject.AddComponent<CanvasGroup>();
            return go;
        }
    }
}
