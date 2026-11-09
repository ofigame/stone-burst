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
            var logo = Resources.Load<Texture2D>("Branding/ofigame-logo");
            if (Application.isBatchMode || logo == null) { SceneManager.LoadScene(_firstScene); return; }
            StartCoroutine(Splash(logo));
        }

        private IEnumerator Splash(Texture2D logo)
        {
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = Color.white;
            var go = CreateSplash(logo, null, out var lr, out var group);
            go.name = "Splash";
            var load = SceneManager.LoadSceneAsync(_firstScene);
            load.allowSceneActivation = false;
            float t = 0f;
            while (t < FadeIn + Hold || load.progress < 0.9f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / FadeIn);
                float s = 0.94f + 0.06f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / (FadeIn + Hold)));
                lr.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            float f = 0f;
            while (f < FadeOut)
            {
                f += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(f / FadeOut);
                yield return null;
            }
            load.allowSceneActivation = true;
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
            var bg = new GameObject("Bg", typeof(RectTransform)).AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            bg.color = Color.white;
            var bgr = bg.rectTransform;
            bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one; bgr.offsetMin = bgr.offsetMax = Vector2.zero;
            var img = new GameObject("Logo", typeof(RectTransform)).AddComponent<RawImage>();
            img.transform.SetParent(go.transform, false);
            img.texture = logo;
            lr = img.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
            // Kullanıcı (2026-11-08): "logo alttan üstten veya yandan basık olmasın". Doku içe aktarmada 2'nin kuvvetine
            // esnetilirse width/height bozuk oran verir; oran kaynak görselden (2440×1490) sabit alınır.
            float w = 820f;
            lr.sizeDelta = new Vector2(w, w / LogoAspect);
            group = img.gameObject.AddComponent<CanvasGroup>();
            return go;
        }
    }
}
