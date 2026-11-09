using System;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;
using TMPro;
using Object = UnityEngine.Object;

namespace KatKat.UI
{
    /// <summary>Koddan uGUI öğeleri kurmak için kısa yardımcılar.</summary>
    public static class UiFactory
    {
        /// <summary>Oyunda Destroy, editör araçlarında (oynatma dışı) DestroyImmediate.</summary>
        public static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Ebeveynin üst/alt kenarına yaslı, tam genişlikte şerit.</summary>
        public static RectTransform Band(RectTransform rt, bool top, float offset, float height)
        {
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rt.anchoredPosition = new Vector2(0f, top ? -offset : offset);
            rt.sizeDelta = new Vector2(0f, height);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, bool sliced = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        public static Text Text(string name, Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            // Ağırlık fonttan gelir (Nunito ExtraBold / SemiBold); TMP sahte kalınlığı kullanılmaz.
            bool bold = style == FontStyle.Bold || style == FontStyle.BoldAndItalic;
            t.font = bold ? UiTypography.Font : UiTypography.Body;
            t.fontSize = size;
            t.fontStyle = style == FontStyle.Italic || style == FontStyle.BoldAndItalic ? FontStyles.Italic : FontStyles.Normal;
            t.color = color;
            t.alignment = UiTypography.Alignment(anchor);
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.richText = false;
            t.extraPadding = true;
            Loc.Set(t, text);
            return t;
        }

        /// <summary>Yuvarlak ikon düğmesi (en az 110 referans birimi ≈ 48 dp üzeri dokunma alanı).</summary>
        public static Button IconButton(string name, Transform parent, UiIcon icon, Color bg, Color fg, float size, Action onClick)
        {
            var img = Image(name, parent, UiArt.Circle, bg);
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            var ic = Image("Icon", img.transform, UiArt.Icon(icon), fg);
            Stretch(ic.rectTransform, size * 0.16f, size * 0.16f, size * 0.16f, size * 0.16f);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        // ---- Premium-v2 bileşenleri -------------------------------------------------------------------
        // Paket sprite'ı bulunamazsa (Resources/Premium yoksa) eski koddan üretilen görünüme düşer.

        /// <summary>Başlık yazısı: ince koyu kontur + aşağı yumuşak gölge (rehber §6). Metin her zaman gerçek metindir.</summary>
        public static Text Title(string name, Transform parent, string text, int size, Color color, Color outline)
        {
            var t = Text(name, parent, text, size, color);
            UiTypography.StyleTitle(t, outline);
            return t;
        }

        /// <summary>
        /// 9-dilim uç kapaklarını gösterim yüksekliğine oturtur: yüksek çözünürlüklü hap sprite'ının (ör. 338 px yükseklik,
        /// 168 px kenarlık) 116 birimlik alana sıkışıp elips olmasını önler. Kaynak yeniden örneklenmez; yalnızca
        /// dilim ölçeği (pixelsPerUnitMultiplier) değişir. Kenarlık toplamı yükseklikten küçükse dokunulmaz.
        /// </summary>
        public static Image FitSliced(Image img, float height)
        {
            if (img == null || img.sprite == null || img.type != UnityEngine.UI.Image.Type.Sliced || height <= 0f) return img;
            var b = img.sprite.border;
            img.pixelsPerUnitMultiplier = Mathf.Max(1f, (b.y + b.w) / height);
            return img;
        }

        /// <summary>Sprite'lı düğme. Kenarlıklı sprite 9-dilim çizilir; basış ölçeği PressFeedback ile.</summary>
        public static Button SpriteButton(string name, Transform parent, Sprite sprite, Vector2 size, Action onClick, Sprite pressed = null)
        {
            bool sliced = sprite != null && sprite.border.sqrMagnitude > 0f;
            var img = Image(name, parent, sprite, Color.white, sliced);
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = size;
            FitSliced(img, size.y);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            PressFeedback.Attach(b, pressed);
            return b;
        }

        /// <summary>Hap düğme: birincil nane yeşili + krem yazı, ikincil krem + kahve yazı.</summary>
        public static Button PillButton(string name, Transform parent, string label, bool primary, Vector2 size, Action onClick)
        {
            // v4 ayrı düğme tabanları: birincil turkuaz, ikincil krem (yazısız; metin canlı).
            var sprite = Art(primary ? "ui/button-primary-teal" : "ui/button-secondary-cream", primary ? "btn_mint" : "btn_cream");
            if (sprite == null) return TextButton(name, parent, label, null, primary ? UiArt.Teal : UiArt.Pill, primary ? Color.white : UiArt.Ink, size, onClick);
            var b = SpriteButton(name, parent, sprite, size, onClick);
            // Kullanıcı: "butonlarda yazılar butona göre büyük kalıyor". Hap sprite'ının görünen yüzü dikdörtgenden
            // incedir (gölge + kenar): yazı yüksekliğin ~%32'si; uzun metin (ör. Almanca) genişliğe sığana dek küçülür.
            int fs = Mathf.RoundToInt(size.y * (primary ? 0.34f : 0.31f));
            var t = primary
                ? Title("Label", b.transform, label, fs, UiArt.Cream, UiArt.HudTealDark)
                : Text("Label", b.transform, label, fs, UiArt.Brown);
            Stretch(t.rectTransform, size.y * 0.45f, size.y * 0.45f, size.y * 0.12f, size.y * 0.18f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true;
            t.fontSizeMax = fs;
            t.fontSizeMin = Mathf.RoundToInt(fs * 0.55f);
            return b;
        }

        /// <summary>v4 bağımsız varlık, yoksa v2 sayfa kesimi.</summary>
        public static Sprite Art(string v4Key, string v2Name = null)
        {
            var s = KatKat.View.PremiumArt.V4(v4Key);
            return s != null || v2Name == null ? s : KatKat.View.PremiumArt.Get(v2Name);
        }

        /// <summary>
        /// Yuvarlak ikon düğmesi: altın kenarlı hap kare boyutta çizilince daire olur (9-dilim yarım yükseklik).
        /// İkon ayrı PNG (v4 icons/*); yoksa kodla üretilen ikon ailesi.
        /// </summary>
        public static Button CircleButton(string name, Transform parent, bool teal, UiIcon icon, float size, Action onClick, string v4Icon = null)
        {
            var sprite = Art(teal ? "ui/hud-title-pill" : "ui/hud-moves-pill", teal ? "hud_title" : "hud_moves");
            if (sprite == null) return IconButton(name, parent, icon, UiArt.Teal, Color.white, size, onClick);
            var b = SpriteButton(name, parent, sprite, new Vector2(size, size), onClick);
            var iconSprite = v4Icon != null ? KatKat.View.PremiumArt.V4(v4Icon) : null;
            var ic = Image("Icon", b.transform, iconSprite ?? UiArt.Icon(icon), iconSprite != null ? Color.white : teal ? UiArt.Cream : UiArt.HudTeal);
            ic.preserveAspect = true;
            // Krem ikon krem zeminde kaybolmasın: turkuaz ile çarpılır (ikonun kendi gölgelemesi korunur).
            if (iconSprite != null && !teal) ic.color = UiArt.HudTeal;
            Stretch(ic.rectTransform, size * 0.24f, size * 0.24f, size * 0.22f, size * 0.26f);
            return b;
        }

        /// <summary>Metinli, yuvarlak köşeli düğme; isteğe bağlı solda ikon.</summary>
        public static Button TextButton(string name, Transform parent, string label, UiIcon? icon, Color bg, Color fg, Vector2 size, Action onClick)
        {
            var img = Image(name, parent, UiArt.Box, bg, true);
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = size;
            float pad = size.y * 0.22f;
            if (icon.HasValue)
            {
                var ic = Image("Icon", img.transform, UiArt.Icon(icon.Value), fg);
                Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(pad + size.y * 0.3f, 0f), new Vector2(size.y * 0.62f, size.y * 0.62f));
            }
            var t = Text("Label", img.transform, label, Mathf.RoundToInt(size.y * 0.38f), fg);
            Stretch(t.rectTransform, icon.HasValue ? size.y * 0.8f : pad, pad, 0, 0);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }
    }
}
