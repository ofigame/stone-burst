using System;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// ui1 tasarım dilinin ortak bileşenleri (docs/ui1/TASARIM_DILI.md): krem panel, ince altın çerçeve, turkuaz ana
    /// düğme, krem ikincil düğme, kapsül/satır, başlık plakası, mor kurdele, kapat/geri/bilgi. Görseller yazısız
    /// (Resources/UI1); bütün metinler ayrı TMP (Nunito 800 ana, 600 ikincil). Sayfalar bu yardımcılarla kurulur.
    /// Kaynak yoksa null döner; çağıran eski görünüme düşer.
    /// </summary>
    public static class Ui1
    {
        // ui3 (docs/ui3_onayli_tasarim/detaylar/tasarim-tokenlari.json): renkler token değerleri; adlar ui1'den kalır.
        public static readonly Color Teal = Ui3.Teal;
        public static readonly Color TealDark = Ui3.TealBottom;
        public static readonly Color Cream = Ui3.Ivory;
        public static readonly Color Gold = new Color32(0xF3, 0xBE, 0x49, 0xFF);
        public static readonly Color Navy = Ui3.Ink;
        public static readonly Color Purple = Ui3.Purple;
        public static readonly Color Ink2 = Ui3.InkSecondary;
        /// <summary>Modal karartması (ui3): #111C24, alfa 0.72; bütün fiziksel ekran.</summary>
        public static readonly Color Dim = new Color(Ui3.Scrim.r, Ui3.Scrim.g, Ui3.Scrim.b, 0.72f);

        /// <summary>ui3 karşılığı olan ui1 anahtarları (seramik panel/kart/düğme/plaka, yeni yuvarlak simgeler).</summary>
        private static string Ui3Key(string key) => key switch
        {
            "panel-modal" => "modal-seramik",
            "card-ivory" => "kart-seramik",
            "button-primary" => "buton-birincil",
            "button-secondary" => "buton-ikincil",
            "header-ivory" => "baslik-plakasi",
            "ribbon-purple" => "ribbon-purple",
            "ribbon-teal" => "ribbon-teal",
            _ when key.StartsWith("icon-", StringComparison.Ordinal) && key != "icon-zpe" => key,
            _ => null,
        };

        public static Sprite Art(string key)
        {
            var k3 = Ui3Key(key);
            if (k3 != null) { var s3 = Ui3.Art(k3); if (s3 != null) return s3; }
            KatKat.View.ResourceUsage.Note("UI1/" + key);
            return Resources.Load<Sprite>("UI1/" + key);
        }
        public static bool Ready => Art("panel-modal") != null;

        /// <summary>9 dilimli görsel; köşe ölçüsü gösterimde <paramref name="corner"/> birim (kaynak sınırına göre çarpan).</summary>
        public static Image Sliced(string name, Transform parent, string key, float corner)
        {
            var sp = Art(key);
            var img = UiFactory.Image(name, parent, sp, Color.white, true);
            img.raycastTarget = false;
            if (sp != null && sp.border.x > 0f) img.pixelsPerUnitMultiplier = sp.border.x / Mathf.Max(1f, corner);
            return img;
        }

        /// <summary>
        /// ui1 metin düzeni: kaynak dizgi tümüyle büyük harfse (eski arayüz, ör. "HAMLELER BİTTİ") dil kültürüyle
        /// küçültülür. words=false → cümle düzeni ("Hamleler bitti"), true → her sözcük büyük ("Tekrar Dene").
        /// Karışık harfli dizgiye dokunulmaz. Dizgi dosyası değişmez (eski arayüz aynı kalır).
        /// </summary>
        public static string Case(string s, bool words)
        {
            if (string.IsNullOrEmpty(s) || s != s.ToUpper(Loc.Culture) || s == s.ToLower(Loc.Culture)) return s;
            var c = s.ToLower(Loc.Culture).ToCharArray();
            bool start = true;
            for (int i = 0; i < c.Length; i++)
            {
                if (char.IsLetter(c[i]))
                {
                    if (start) c[i] = char.ToUpper(c[i], Loc.Culture);
                    start = false;
                }
                else if (words && c[i] == ' ') start = true;
                else if (c[i] == '-' && Loc.Current != null && Loc.Current.Code == "de") start = true; // "Gratis-Versuch"
            }
            var r = new string(c);
            // Almanca adlar büyük harfle yazılır (2026-11-09 dil incelemesi: "Keine züge mehr" hatalıydı).
            if (Loc.Current != null && Loc.Current.Code == "de")
                foreach (var n in GermanNouns)
                    r = System.Text.RegularExpressions.Regex.Replace(r, "\\b" + n.ToLower(Loc.Culture) + "\\b", n);
            return r;
        }

        private static readonly string[] GermanNouns = { "Züge", "Versuch", "Level", "Neustart", "Pause", "Testwerbung", "Rang", "Ladefehler", "Start" };

        /// <summary>Hap biçimli (düğme, kapsül, satır): uçlar yüksekliğe oturur.</summary>
        public static Image Pill(string name, Transform parent, string key, float height)
        {
            var sp = Art(key);
            var img = UiFactory.Image(name, parent, sp, Color.white, true);
            img.raycastTarget = false;
            if (sp != null) img.pixelsPerUnitMultiplier = sp.rect.height / Mathf.Max(1f, height);
            return img;
        }

        public static Image Decor(string name, Transform parent, string key)
        {
            var img = UiFactory.Image(name, parent, Art(key), Color.white);
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        /// <summary>Krem panel (ince altın çerçeve). Köşe ~60 birim.</summary>
        public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, bool blocks = true)
        {
            var p = Sliced(name, parent, "panel-modal", 60f);
            UiFactory.Place(p.rectTransform, anchor, pos, size);
            p.raycastTarget = blocks;
            // ui3 seramik modal: üst omuz (crest) ve köşeler tek katsayıyla (860 genişlik ↔ 635 px); kısa kartta köşe sığsın.
            if (p.sprite != null && p.sprite.name.StartsWith("modal-seramik"))
                p.pixelsPerUnitMultiplier = Mathf.Max(p.sprite.rect.width / Mathf.Max(1f, size.x), (p.sprite.border.y + p.sprite.border.w) / Mathf.Max(1f, size.y - 8f));
            return p;
        }

        /// <summary>Krem kart (koleksiyon/açıklama). Köşe ~44 birim.</summary>
        public static Image Card(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var p = Sliced(name, parent, "card-ivory", 40f);
            UiFactory.Place(p.rectTransform, anchor, pos, size);
            return p;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color, bool bold = true, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var t = UiFactory.Text(name, parent, text, size, color, anchor, bold ? FontStyle.Bold : FontStyle.Normal);
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Turkuaz ana (primary) ya da krem ikincil düğme; metin ayrı TMP; isteğe bağlı simge solda.</summary>
        public static Button Button(string name, Transform parent, string text, bool primary, Vector2 anchor, Vector2 pos, Vector2 size, Action act, Sprite icon = null, int fontSize = 44)
        {
            var b = UiFactory.SpriteButton(name, parent, null, size, act);
            var hit = b.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Place((RectTransform)b.transform, anchor, pos, size);
            var bg = Pill("Bg", b.transform, primary ? "button-primary" : "button-secondary", size.y);
            UiFactory.Stretch(bg.rectTransform);
            float tx = 0f;
            if (icon != null)
            {
                var ic = UiFactory.Image("Icon", b.transform, icon, Color.white);
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                float s = size.y * 0.5f;
                UiFactory.Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-size.x * 0.5f + size.y * 0.55f, 2f), new Vector2(s, s));
                tx = (size.y * 0.8f - 40f) * 0.5f + 6f;
            }
            var t = Label("Text", b.transform, text, fontSize, primary ? Ui3.OnTeal : Navy);
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(tx, 3f), new Vector2(icon != null ? size.x - size.y * 0.8f - 52f : size.x - 70f, size.y - 20f));
            t.enableAutoSizing = true; t.fontSizeMax = fontSize; t.fontSizeMin = Mathf.Max(22, Mathf.RoundToInt(fontSize * 0.5f));
            t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; // tek satır; uzun etiket küçülür (iki satıra kırılmaz)
            PressFeedback.Attach(b, null, 80f, 120f);
            return b;
        }

        /// <summary>Krem kapsül (hedef/tarih/bölüm etiketi); içerik ayrı.</summary>
        public static Image Chip(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            // ui3 hedef kapsülü: ivoryTop→ivoryBottom, yarıçap yüksekliğin yarısı, 2 px kum kontur.
            var c = Ui3.Surface(name, parent, size.y * 0.5f, Ui3.IvoryTop, Ui3.IvoryBottom, size.y, 2f, Ui3.Sand);
            UiFactory.Place(c.rectTransform, anchor, pos, size);
            return c;
        }

        /// <summary>Krem satır (liste/ayar/kural satırı).</summary>
        public static Image Row(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            // ui3 satır (kural/liste): ivory, yarıçap 28, 2 px kum kontur.
            var c = Ui3.Surface(name, parent, 28f, Ui3.Ivory, 2f, Ui3.Sand);
            UiFactory.Place(c.rectTransform, anchor, pos, size);
            return c;
        }

        /// <summary>Krem başlık plakası (taş adı, sayfa başlığı); başlık ayrı TMP.</summary>
        public static Text Header(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize = 60)
        {
            var h = Pill("Header", parent, "header-ivory", size.y);
            UiFactory.Place(h.rectTransform, anchor, pos, size);
            var t = Label("Title", h.transform, text, fontSize, Navy);
            UiFactory.Stretch(t.rectTransform, 40f, 40f, 14f, 18f);
            t.enableAutoSizing = true; t.fontSizeMax = fontSize; t.fontSizeMin = 32;
            return t;
        }

        /// <summary>Mor kurdele başlık (koleksiyon, deneme, patron); yazı ayrı TMP.</summary>
        public static Text Ribbon(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize = 52, string key = "ribbon-purple")
        {
            var r = Decor("Ribbon", parent, key);
            r.preserveAspect = false;
            UiFactory.Place(r.rectTransform, anchor, pos, size);
            var t = Label("Title", r.transform, text, fontSize, Ui3.OnTeal);
            var ol = t.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            ol.effectColor = new Color(Ui3.Shadow.r, Ui3.Shadow.g, Ui3.Shadow.b, 0.35f);
            ol.effectDistance = new Vector2(0f, -3f);
            UiFactory.Stretch(t.rectTransform, size.x * 0.16f, size.x * 0.16f, 12f, 22f);
            t.enableAutoSizing = true; t.fontSizeMax = fontSize; t.fontSizeMin = 28;
            return t;
        }

        /// <summary>Yuvarlak simge düğmesi (kapat, geri, bilgi, duraklat, geri al). En az 96 birim dokunma alanı.</summary>
        public static Button Round(string name, Transform parent, string key, Vector2 anchor, Vector2 pos, float size, Action act)
        {
            var b = UiFactory.SpriteButton(name, parent, Art(key), new Vector2(size, size), act);
            b.GetComponent<Image>().preserveAspect = true;
            UiFactory.Place((RectTransform)b.transform, anchor, pos, new Vector2(size, size));
            PressFeedback.Attach(b, null, 80f, 120f);
            return b;
        }

        /// <summary>Tam ekran karartma (dokununca isteğe bağlı kapanır).</summary>
        public static Button Dimmer(Transform parent, Action onTap)
        {
            var d = UiFactory.SpriteButton("Dim", parent, UiArt.Box, new Vector2(10f, 10f), onTap);
            d.GetComponent<Image>().color = Dim;
            d.transition = Selectable.Transition.None;
            UiFactory.Stretch((RectTransform)d.transform, -200f, -200f, -400f, -400f);
            return d;
        }
    }
}
