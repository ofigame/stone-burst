using System;
using KatKat.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Anasayfa sol sütun rozetleri (kullanıcı 2026-11-22, "Renkli Oyun Menüsü İkonları"): ZPE, Sıralama, Canlı Maç ve
    /// Test düğmeleri altın çerçeveli, alt tabelalı rozet görselidir (Resources/UI3/rozet). Görsellerin tabela yazısı
    /// silindi; yazı oyunda her dilde tabelanın üstüne yazılır. Arkada yavaşça nefes alan ışıltı ve üstte ışıltılı
    /// yıldızcıklar ("bunların arkasına ışıltı verelim"). Görsel yoksa eski yuvarlak düğmeler kurulur.
    /// </summary>
    public partial class MenuView
    {
        /// <summary>Rozet genişliği; yükseklik görsel oranından (sütun adımı <see cref="BadgeStep"/>).</summary>
        internal const float BadgeW = 170f; // 2026-11-22: 216 çok büyüktü (kullanıcı)

        /// <summary>Rozetler arası dikey adım (rozet ~152 yüksek).</summary>
        private const float BadgeStep = 172f;

        /// <summary>Tabela yazı alanı (rozet boyuna oranla): merkezi alttan %19, genişlik %76, yükseklik %21.</summary>
        private const float PlaqueY = 0.19f, PlaqueW = 0.76f, PlaqueH = 0.21f;

        internal static readonly Color32 BadgeInk = new Color32(0x0B, 0x2F, 0x6E, 0xFF);

        /// <summary>Sütundaki n. rozetin (0 ZPE, 1 Sıralama, 2 Canlı Maç, 3 Test) üstten merkezi; eşit aralık.</summary>
        internal static float BadgeSlotY(int n) => -V11ColumnZpeTop - 84f - n * BadgeStep;

        internal static Sprite BadgeArt(string key) => Ui3.Art("rozet/" + key);

        /// <summary>Altın günlük sekmesi takvimi (rozetin tabelasız üst kısmı); yoksa eski çizim.</summary>
        private static Sprite DailyTabIcon() => BadgeArt("sekme-gunluk") ?? CalendarIcon();

        /// <summary>
        /// Rozet düğmesi: <paramref name="centerY"/> sütunda üstten merkez. Tabela yazısı <paramref name="label"/>; açık
        /// tabelada lacivert, koyu (mor) tabelada beyaz. Arkasında <paramref name="glow"/> renginde ışıltı.
        /// </summary>
        private Button BadgeButton(string name, RectTransform shell, Sprite art, float centerY, string label, bool darkPlaque, int size, Action act, out TextMeshProUGUI text, Color? glow = null)
        {
            float h = BadgeW * art.rect.height / art.rect.width;
            var at = new Vector2(-V11ColumnX, centerY);

            // Işıltı: rozetten büyük yumuşak hale, düğmenin arkasında (dokunma yakalamaz).
            var halo = Decor(name + "Glow", shell, GlowSprite);
            halo.raycastTarget = false;
            var tint = glow ?? new Color(1f, 0.78f, 0.2f, 1f);
            halo.color = new Color(tint.r, tint.g, tint.b, BadgeGlow.Max);
            UiFactory.Place(halo.rectTransform, new Vector2(0.5f, 1f), at, new Vector2(BadgeW * 1.55f, h * 1.5f));
            halo.gameObject.AddComponent<BadgeGlow>();

            var b = UiFactory.SpriteButton(name, shell, art, new Vector2(BadgeW, h), act);
            b.GetComponent<Image>().preserveAspect = true;
            UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), at, new Vector2(BadgeW, h));
            var col = darkPlaque ? (Color)Color.white : (Color)BadgeInk;
            var outline = darkPlaque ? (Color)new Color32(0x3A, 0x14, 0x66, 0xFF) : new Color(1f, 1f, 1f, 0f);
            text = UiFactory.Title("Label", b.transform, label, size, col, outline);
            text.raycastTarget = false;
            text.richText = true;
            text.textWrappingMode = TextWrappingModes.NoWrap; // tek satır; uzun ad küçülerek sığar
            UiFactory.Place(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, h * PlaqueY), new Vector2(BadgeW * PlaqueW, h * PlaqueH));
            FitText(text, 18);

            // Işıltılı yıldızcıklar: rozetin üst (resimli) kısmında.
            var sparkArea = UiFactory.Rect("Sparkle", b.transform);
            sparkArea.anchorMin = new Vector2(0f, 0.32f);
            sparkArea.anchorMax = new Vector2(1f, 1f);
            sparkArea.offsetMin = sparkArea.offsetMax = Vector2.zero;
            Twinkle.Attach(sparkArea, 2, 34f, 0.5f);
            return b;
        }

        private static Sprite _glowSprite;

        /// <summary>
        /// Rozet ışıltısı (128 px, koddan): merkezden yarıçapın %50 sine dek tam, sonra yumuşak sönen hale. Açık zeminde
        /// (bulut, çimen) de görünsün diye düz kare düşüşlü SpecialArt.Glow yerine geniş dolu gövde.
        /// </summary>
        private static Sprite GlowSprite
        {
            get
            {
                if (_glowSprite) return _glowSprite;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "badge_glow", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
                var px = new Color32[n * n];
                for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = (i + 0.5f) / n - 0.5f, y = (j + 0.5f) / n - 0.5f;
                    float r = Mathf.Sqrt(x * x + y * y) / 0.5f;
                    float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, r));
                    px[j * n + i] = new Color(1f, 1f, 1f, a * a);
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                _glowSprite.name = "badge_glow";
                return _glowSprite;
            }
        }

        /// <summary>Günlük Bulmaca rozeti (2026-11-22: canlı maçla yer değiştirdi; Sıralama altı).</summary>
        private void BuildDailyBadge(RectTransform shell)
        {
            var art = BadgeArt("rozet-gunluk");
            if (art == null || DailyPressed == null) return;
            BadgeButton("DailyButton", shell, art, BadgeSlotY(2), Loc.T("daily.title"), false, 26, () => DailyPressed?.Invoke(), out _, new Color(1f, 0.55f, 0.3f, 1f));
        }

        /// <summary>ZPE rozeti: tabelada puan (kalibrasyonda "—").</summary>
        private bool BuildZpeBadgeArt(RectTransform root)
        {
            var art = BadgeArt("rozet-zpe");
            if (art == null) return false;
            var z = ZpeBadge;
            string value = z.Calibrating || z.Count == 0 ? "—" : $"{z.Score}<size=62%><color=#4A78B8>/100</color></size>";
            BadgeButton("ZpeButton", root, art, BadgeSlotY(0), value, false, 38, () => ZpePressed?.Invoke(), out _, new Color(0.2f, 0.85f, 1f, 1f));
            return true;
        }
    }

    /// <summary>Rozet ışıltısı yavaşça nefes alır (saydamlık ve ölçek); azaltılmış harekette sabit.</summary>
    public sealed class BadgeGlow : MonoBehaviour
    {
        public const float Max = 0.9f, Min = 0.5f, Period = 2.6f;
        private Image _img;
        private float _t;

        private void Awake()
        {
            _img = GetComponent<Image>();
            _t = UnityEngine.Random.value * Period; // rozetler aynı anda atmasın
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float dt)
        {
            if (_img == null) return;
            var c = _img.color;
            if (Motion.Reduced) { c.a = Max; _img.color = c; transform.localScale = Vector3.one; return; }
            _t += dt;
            float k = 0.5f * (1f - Mathf.Cos(_t * Mathf.PI * 2f / Period));
            c.a = Mathf.Lerp(Min, Max, k);
            _img.color = c;
            float s = 1f + 0.05f * k;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
