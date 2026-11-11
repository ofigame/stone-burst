using System;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// v10 "Mavi Liman" kabuğu (docs/v10/TriKat-Mavi-Liman-Claude, onaylı referans 940 × 1673 → 1080 birim, ×1,149):
    /// ana sayfa haritasının sabit üst başlığı (mor bulut sahnesi, bölge illüstrasyonu, logo, altın dalgalı şerit,
    /// bölge etiketi/adı/aralığı, Ayarlar, yıldız kapsülü) ve bütün menü sayfalarında ortak dört sekmeli alt menü.
    ///
    /// Her parça ayrı katmandır; metinler canlı (Loc). Final görseli olmayan parçalar <see cref="ShellArt"/> ile
    /// Resources/PremiumV6/shell/ altından aranır, yoksa koddan çizilmiş GEÇİCİ sürüm kullanılır (final sayılmaz):
    /// top-clouds, top-ribbon, laurel, bottom-bar, icon-crystal, icon-chest. Kabuk şeritleri tek kapsülün esnetilmesi
    /// değildir: kontur 1080 birim genişlik için çizilir (referans konturu).
    /// </summary>
    public sealed partial class MenuView
    {
        // ---- Ölçüler (1080 birim; üst öğeler güvenli alanın üstünden, alt öğeler altından) ------------------------
        private const float ShellTopPlateH = 300f, ShellTopBleed = 160f;      // mor sahne + çentik/üst taşma payı
        private const float RibbonTop = 160f, RibbonH = 200f;                 // altın şerit kutusu (üstten)
        private const float ShellBarH = 208f, ShellBarBleed = 60f;            // alt kabuğun görünür yüksekliği + alt taşma
        private const float TabIconY = 171f, TabLabelY = 93f;                 // alttan merkezler
        // Kullanıcı (2026-11-07): Ödüller kaldırıldı; üç eşit sekme (Harita · Günlük · Taşlar).
        private static readonly float[] TabX = { -330f, 0f, 330f };
        private const float TabSlotW = 300f;
        private static readonly float[] TabDividerX = { -165f, 165f };

        private static readonly Color ShellNavy = new Color32(0x1C, 0x36, 0x63, 0xFF);
        private static readonly Color ShellAmber = new Color32(0xA8, 0x64, 0x16, 0xFF);
        private static readonly Color ShellTeal = new Color32(0x3E, 0xCC, 0xC4, 0xFF);
        private static readonly Color ShellTealDark = new Color32(0x17, 0x8E, 0x88, 0xFF);
        private static readonly Color ShellGold = new Color32(0xF2, 0xBE, 0x4A, 0xFF);
        private static readonly Color ShellPurple = new Color32(0x6B, 0x3A, 0xB4, 0xFF);

        public const int TabRewards = 5;

        /// <summary>Final kabuk görseli (Resources/PremiumV6/shell/&lt;key&gt;); yoksa null → geçici çizim.</summary>
        private static Sprite ShellArt(string key) => SpecialArt.V6("shell/" + key);

        /// <summary>Hangi kabuk parçaları geçici (testler ve rapor için).</summary>
        public static string[] ShellTemporaryParts()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var k in new[] { "top-clouds", "top-ribbon", "laurel", "bottom-bar", "icon-crystal", "icon-chest" })
                if (ShellArt(k) == null) list.Add(k);
            return list.ToArray();
        }

        private static Image Decor(string name, Transform parent, Sprite sprite)
        {
            var img = UiFactory.Image(name, parent, sprite, Color.white);
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Opak kabuk parçası: arkasındaki görünmez düğümler tıklanmasın; şeffaf payı kaydırmayı engellemesin.</summary>
        private static void BlockOpaque(Image img)
        {
            var tex = img.sprite != null ? img.sprite.texture : null;
            if (tex == null || !tex.isReadable) { img.raycastTarget = false; return; }
            img.raycastTarget = true;
            img.alphaHitTestMinimumThreshold = 0.5f;
        }

        /// <summary>Altın kenarlı kapsül: altta biraz büyük altın hap, üstte renkli hap (kalın, her yönde eşit kenar).</summary>
        internal static Image GoldCapsule(string name, Transform parent, Color fill, Vector2 anchor, Vector2 pos, Vector2 size, float rim = 7f)
        {
            var back = UiFactory.Image(name, parent, UiArt.Box, ShellGold, true);
            back.raycastTarget = false;
            UiFactory.Place(back.rectTransform, anchor, pos, size + new Vector2(rim * 2f, rim * 2f));
            var sh = back.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.25f, 0.1f, 0.3f, 0.45f);
            sh.effectDistance = new Vector2(0f, -5f);
            var front = UiFactory.Image("Fill", back.transform, UiArt.Box, fill, true);
            front.raycastTarget = false;
            UiFactory.Stretch(front.rectTransform, rim, rim, rim, rim);
            return front;
        }

        // ---- Üst başlık -------------------------------------------------------------------------------------------

        /// <summary>
        /// Haritanın sabit üst kabuğu. Dinamik bölge parçaları (illüstrasyon, etiket, ad, aralık) <see cref="HeaderCard"/>
        /// ile <paramref name="headBack"/> (şeridin arkası) ve <paramref name="head"/> (şeridin önü) katmanlarına kurulur.
        /// </summary>
        // ---- v11 mavi-beyaz kabuk (docs/v11, seçilen 01-secilen-ana-ekran.png 940 × 1673 → 1080 birim, ×1,149) --------

        /// <summary>v11 kabuk parçası (Resources/PremiumV6/shell/v11-*); yoksa null → v10 kabuğu.</summary>
        private static Sprite V11(string key) => ShellArt("v11-" + key);
        /// <summary>Yakalama aracı için: v10 kabuğunu zorla (aynı kayıtla önce/sonra). Oyunda false.</summary>
        public static bool ForceLegacyShell;
        private static bool UseV11 => !ForceLegacyShell && V11("panel") != null;

        private const float V11TopPanelBottom = 336f;              // mavi üst panelin alt kenarı (güvenli üstten)
        private const float V11CardTop = 168f, V11CardW = 1064f;   // beyaz bölge kartı
        private static readonly Color V11Navy = new Color32(0x12, 0x2E, 0x6E, 0xFF);
        private static readonly Color V11Blue = new Color32(0x0A, 0x8F, 0xD8, 0xFF);
        private static readonly Color V11BlueDeep = new Color32(0x06, 0x6C, 0xB8, 0xFF);

        private static Sprite _vShadow;
        /// <summary>Dikey gölge geçişi (üstte koyu, altta saydam); kısa panel gölgeleri için.</summary>
        private static Sprite VShadow()
        {
            if (_vShadow) return _vShadow;
            var tex = new Texture2D(4, 64, TextureFormat.RGBA32, false) { name = "v11_shadow", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[4 * 64];
            for (int y = 0; y < 64; y++)
            {
                float t = y / 63f; // 0 alt, 1 üst
                byte a = (byte)(255f * t * t);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return _vShadow = Sprite.Create(tex, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 64f);
        }

        /// <summary>Mavi panel zemini: oran korunarak "cover" ölçek (yanlardan kırpılır, ezilmez). Üst panel çizgili
        /// zemin (docs/v11/arayuz/01-mavi-panel-zemini_üst.png → v11-panel-top), alt menü düz zemin (…-menü.png → v11-panel).</summary>
        private static Image V11Panel(string name, RectTransform parent, Vector2 anchor, Vector2 pivot, float y, float height, bool top = false)
        {
            var art = (top ? V11("panel-top") : null) ?? V11("panel");
            float w = Mathf.Max(1080f, height * art.rect.width / art.rect.height);
            float h = w * art.rect.height / art.rect.width;
            var img = UiFactory.Image(name, parent, art, Color.white);
            img.raycastTarget = true; // opak panel: arkasındaki harita düğümlerine dokunulmaz
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(w, Mathf.Max(h, height));
            rt.anchoredPosition = new Vector2(0f, y);
            return img;
        }

        /// <summary>Beyaz kenarlı kapsül (v11 sağ sütun): GoldCapsule gövdesi, kenar beyaz.</summary>
        private static Image WhiteCapsule(string name, Transform parent, Color fill, Vector2 anchor, Vector2 pos, Vector2 size, float rim = 5f)
        {
            var front = GoldCapsule(name, parent, fill, anchor, pos, size, rim);
            var back = front.transform.parent.GetComponent<Image>();
            back.color = Color.white;
            var sh = back.GetComponent<Shadow>();
            if (sh != null) sh.effectColor = new Color(0.02f, 0.18f, 0.4f, 0.35f);
            return front;
        }

        private void BuildMapShellV11(RectTransform page, int stars, Action settings, out RectTransform headBack, out RectTransform head)
        {
            if (Ui3.Ready) { BuildMapShellUi3(page, stars, settings, out headBack, out head); return; }
            _shellStars = stars; _shellSettings = settings; // ui1 üst çubuğu (03/09) aynı değerleri gösterir
            var shell = UiFactory.Rect("TopShell", page);
            shell.anchorMin = new Vector2(0.5f, 1f); shell.anchorMax = new Vector2(0.5f, 1f); shell.pivot = new Vector2(0.5f, 1f);
            shell.sizeDelta = new Vector2(1080f, V11TopPanelBottom);
            shell.anchoredPosition = Vector2.zero;

            // 1) Mavi üst panel: güvenli alanın üstüne (çentik) taşar; altında kısa gölge (harita içeride hissi).
            var fill = UiFactory.Image("PanelFill", shell, UiArt.Box, V11BlueDeep);
            fill.raycastTarget = false;
            UiFactory.Place(fill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, ShellTopBleed + 100f), new Vector2(1600f, 220f));
            V11Panel("Panel", shell, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), -V11TopPanelBottom, V11TopPanelBottom + ShellTopBleed, top: true);
            var shadow = Decor("PanelShadow", shell, VShadow());
            shadow.color = new Color(0.02f, 0.16f, 0.34f, 0.30f);
            var srt = shadow.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(1600f, 34f);
            srt.anchoredPosition = new Vector2(0f, -V11TopPanelBottom);

            // 2) Logo solda (referans x 40–380).
            var logoSprite = PremiumArt.V4("ui/logo-stoneburst");
            if (logoSprite != null)
            {
                var logo = Decor("Logo", shell, logoSprite);
                logo.preserveAspect = true;
                var ls = logo.gameObject.AddComponent<Shadow>();
                ls.effectColor = new Color(0.02f, 0.14f, 0.36f, 0.45f);
                ls.effectDistance = new Vector2(0f, -6f);
                float w = 392f, h = Mathf.Min(150f, w * logoSprite.rect.height / logoSprite.rect.width);
                UiFactory.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(-300f, -84f), new Vector2(w, h));
            }

            // 3) Beyaz bölge kartı (fener, ad ve kapsül HeaderCard ile önüne).
            var cardArt = V11("region");
            var card = Decor("RegionShell", shell, cardArt);
            float ch = V11CardW * cardArt.rect.height / cardArt.rect.width;
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -V11CardTop - ch * 0.5f), new Vector2(V11CardW, ch));
            headBack = UiFactory.Rect("RegionBack", shell);
            UiFactory.Stretch(headBack);
            head = UiFactory.Rect("RegionFront", shell);
            UiFactory.Stretch(head);

            // 4) Sağ sütun: ZPE göstergesi ve altında kilidi açılmayı bekleyen taş (kullanıcı, 2026-11-08).
            BuildZpeBadge(shell);
            BuildRankButton(shell); // v12: sol sütunda Sıralama (kupa)
            // v8 (kullanıcı onayı 2026-11-08): türler ana bölümlerde; Deneme düğmesi haritadan kalktı (liste kodda duruyor).
            BuildStarGoal(shell);

            // 5) Ayarlar ve krem yıldız kapsülü.
            if (settings != null) TopGear(page, settings, new Vector2(-96f, -86f));
            var capArt = V11("star-capsule");
            var cap = Decor("StarCapsule", shell, capArt);
            float cw = 262f;
            UiFactory.Place(cap.rectTransform, new Vector2(0.5f, 1f), new Vector2(200f, -86f), new Vector2(cw, cw * capArt.rect.height / capArt.rect.width));
            var star = Decor("Star", cap.transform, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            star.preserveAspect = true;
            UiFactory.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(66f, 3f), new Vector2(80f, 80f));
            var n = UiFactory.Title("Stars", cap.transform, stars.ToString(), 54, V11Navy, PillCream);
            n.alignment = UiTypography.Alignment(TextAnchor.MiddleCenter);
            n.raycastTarget = false;
            UiFactory.Place(n.rectTransform, new Vector2(0f, 0.5f), new Vector2(172f, 2f), new Vector2(130f, 72f));
            FitText(n, 24);
        }

        private void BuildMapShell(RectTransform page, int stars, Action settings, out RectTransform headBack, out RectTransform head)
        {
            if (UseV11) { BuildMapShellV11(page, stars, settings, out headBack, out head); return; }
            var shell = UiFactory.Rect("TopShell", page);
            shell.anchorMin = new Vector2(0.5f, 1f); shell.anchorMax = new Vector2(0.5f, 1f); shell.pivot = new Vector2(0.5f, 1f);
            shell.sizeDelta = new Vector2(1080f, 420f);
            shell.anchoredPosition = Vector2.zero;

            // 1) Mor bulut sahnesi (en arka; üstte güvenli alan dışına taşar).
            var cloudsArt = ShellArt("top-clouds");
            if (cloudsArt != null)
            {
                // Final (2172×698, alt kenarı alfa geçişli): genişlik 1080, oran korunur; üstü güvenli alanın 100 birim
                // üstünden başlar. Çentik payı için daha yukarısı görselin üst rengiyle doldurulur.
                float h = 1080f * cloudsArt.rect.height / cloudsArt.rect.width;
                var fill = UiFactory.Image("CloudsFill", shell, UiArt.Box, new Color32(0x75, 0x2A, 0xB2, 0xFF));
                fill.raycastTarget = false;
                UiFactory.Place(fill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 100f + 200f), new Vector2(1080f, 402f));
                var plate = Decor("Clouds", shell, cloudsArt);
                UiFactory.Place(plate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 100f - h * 0.5f), new Vector2(1080f, h));
            }
            else
            {
                var plate = Decor("Clouds", shell, TopCloudsTemp());
                UiFactory.Place(plate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, ShellTopBleed - (ShellTopPlateH + ShellTopBleed) * 0.5f), new Vector2(1080f, ShellTopPlateH + ShellTopBleed));
                BlockOpaque(plate);
            }

            // 2) Logo ortada (bölge illüstrasyonu şeridin önünde, aşağıda).
            var logoSprite = PremiumArt.V4("ui/logo-stoneburst");
            if (logoSprite != null)
            {
                var logo = Decor("Logo", shell, logoSprite);
                logo.preserveAspect = true;
                var ls = logo.gameObject.AddComponent<Shadow>();
                ls.effectColor = new Color(0.16f, 0.06f, 0.24f, 0.45f);
                ls.effectDistance = new Vector2(0f, -6f);
                float w = 494f, h = Mathf.Min(170f, w * logoSprite.rect.height / logoSprite.rect.width);
                UiFactory.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(-3f, -78f), new Vector2(w, h));
            }

            // Bölge illüstrasyonu (dinamik, fener): rehber v10 — bulut zeminin önünde, altın şeridin arkasında.
            headBack = UiFactory.Rect("RegionBack", shell);
            UiFactory.Stretch(headBack);

            // 3) Altın dalgalı şerit (final görselde defneler gövdeye dahil).
            var ribbonArt = ShellArt("top-ribbon");
            if (ribbonArt != null)
            {
                // 2098×349: genişlik 1080, oran korunur (dalgalı kontur ezilmez); referans görünür merkezi üstten 256.
                float h = 1080f * ribbonArt.rect.height / ribbonArt.rect.width;
                var ribbon = Decor("Ribbon", shell, ribbonArt);
                UiFactory.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -256f), new Vector2(1080f, h));
            }
            else
            {
                var ribbon = Decor("Ribbon", shell, TopRibbonTemp());
                UiFactory.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -RibbonTop - RibbonH * 0.5f), new Vector2(1080f, RibbonH));
                BlockOpaque(ribbon);
                var laurel = ShellArt("laurel") ?? LaurelTemp();
                for (int side = -1; side <= 1; side += 2)
                {
                    var l = Decor(side < 0 ? "LaurelL" : "LaurelR", shell, laurel);
                    l.preserveAspect = true;
                    UiFactory.Place(l.rectTransform, new Vector2(0.5f, 1f), new Vector2(side * 212f, -262f), new Vector2(104f, 146f));
                    if (side > 0) l.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                }
            }
            // Yıldız eşiği çubuğu (şeridin altında; sıradaki taş).
            BuildStarGoal(shell);
            // 4) Bölge etiketi/ad/aralık (dinamik, şeridin önünde).
            head = UiFactory.Rect("RegionFront", shell);
            UiFactory.Stretch(head);

            // 5) Ayarlar ve yıldız kapsülü.
            if (settings != null) TopGear(page, settings, new Vector2(-74f, -70f));
            Image cap;
            var capArt = ShellArt("star-capsule");
            if (capArt != null)
            {
                cap = Decor("StarCapsule", shell, capArt);
                cap.preserveAspect = true;
                UiFactory.Place(cap.rectTransform, new Vector2(0.5f, 1f), new Vector2(540f - 145f, -193f), new Vector2(225f, 225f * capArt.rect.height / capArt.rect.width));
            }
            else cap = GoldCapsule("StarCapsule", shell, ShellPurple, new Vector2(0.5f, 1f), new Vector2(540f - 145f, -193f), new Vector2(212f, 76f), 6f);
            var star = Decor("Star", cap.transform, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            star.preserveAspect = true;
            UiFactory.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(56f, 2f), new Vector2(78f, 78f));
            var n = UiFactory.Title("Stars", cap.transform, stars.ToString(), 50, Color.white, new Color32(0x3A, 0x1A, 0x66, 0xFF));
            n.alignment = UiTypography.Alignment(TextAnchor.MiddleCenter);
            n.raycastTarget = false;
            UiFactory.Place(n.rectTransform, new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(120f, 70f));
            FitText(n, 24);
        }

        /// <summary>Ana sayfa yıldız eşiği çubuğu verisi (kullanıcı, 2026-11-07).</summary>
        public struct StarGoalInfo
        {
            public bool Has;
            public int Stars, From, To;
            public Sprite StoneSprite;
            public Color Glow;
            /// <summary>vr1 P1: buzlu taş (asıl, bulanık olmayan görsel) ve ilerleme p (0..1, kazanılmadıkça &lt;1).</summary>
            public Sprite SharpSprite;
            public float P;
            public bool Ice;
        }

        /// <summary>Sıradaki yıldız taşı hedefi (GameController ShowLevels öncesi doldurur; Has=false ise çubuk yok).</summary>
        public StarGoalInfo NextStarGoal;

        /// <summary>
        /// Yıldız eşiği göstergesi (kullanıcı, 2026-11-07): başlığın altında sağda dikey kart. Üstte kazanılacak taş buzlu
        /// çerçevenin içinde (buz kabuğu önde, üstünde kilit), altında seviye durumu: taşın renginde dolan çubuk ve
        /// "yıldız / eşik". Dokununca Koleksiyon. Bütün yıldız taşları alındıysa gösterilmez. Solda ZPE kapsülü.
        /// </summary>
        private void BuildStarGoal(RectTransform shell)
        {
            var g = NextStarGoal;
            Vr1Fx = null;
            if (!g.Has) return;

            const float W = 190f, H = 262f;
            var card = UiFactory.Rect("StarGoal", shell);
            float top = UseV11 ? V11ColumnStoneTop : 380f;
            UiFactory.Place(card, new Vector2(0.5f, 1f), new Vector2(V11ColumnX, -top - H * 0.5f), new Vector2(W, H));
            var btn = card.gameObject.AddComponent<Image>();
            btn.color = new Color(0f, 0f, 0f, 0f);
            var b = card.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => CollectionPressed?.Invoke());
            PressFeedback.Attach(b, null);

            Vr1IceRefs iceRefs = null;
            if (g.Ice) BuildIceStone(card, g, out iceRefs);
            else
            {
            // Buzlu çerçeve: koyu cam zemin + taş (soluk) + buz kabuğu + kilit.
            var glass = UiFactory.Image("Glass", card, UiArt.Box, UseV11 ? new Color32(0x2C, 0x7F, 0xC8, 0xC0) : new Color32(0x3A, 0x55, 0xA8, 0xB0), true);
            glass.raycastTarget = false;
            UiFactory.Place(glass.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(176f, 176f));
            var halo = Decor("Halo", card, SpecialArt.Glow);
            halo.color = new Color(g.Glow.r, g.Glow.g, g.Glow.b, 0.65f);
            UiFactory.Place(halo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(210f, 210f));
            var stone = Decor("Stone", card, g.StoneSprite);
            stone.preserveAspect = true;
            stone.color = new Color(1f, 1f, 1f, 0.85f);
            UiFactory.Place(stone.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(150f, 150f));
            var ice = Decor("Ice", card, SpecialArt.Glow);
            ice.color = new Color(0.86f, 0.93f, 1f, 0.30f);
            UiFactory.Place(ice.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(170f, 170f));
            var frostArt = SpecialArt.V6("collection/frost");
            if (frostArt != null)
            {
                var frame = Decor("Frost", card, frostArt);
                UiFactory.Place(frame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(186f, 186f));
            }
            var lk = Decor("Lock", card, PremiumArt.V4("icons/lock") ?? UiArt.Icon(UiIcon.Back));
            lk.preserveAspect = true;
            // Kullanıcı (2026-11-08): kilit buzlu çerçevenin tam ortasında (çerçeve merkezi −88).
            UiFactory.Place(lk.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(64f, 64f));
            }

            // Seviye durumu: taşın renginde dolum + sayı.
            var track = UseV11
                ? WhiteCapsule("Track", card, new Color32(0x12, 0x3E, 0x82, 0xF2), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(178f, 50f), 5f)
                : GoldCapsule("Track", card, new Color32(0x3E, 0x22, 0x6E, 0xF2), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(178f, 50f), 5f);
            float k = g.To > g.From ? Mathf.Clamp01((g.Stars - g.From) / (float)(g.To - g.From)) : 1f;
            if (k > 0.01f || iceRefs != null)
            {
                var fill = UiFactory.Image("Fill", track.transform, UiArt.Box, g.Glow, true);
                if (iceRefs != null) iceRefs.Fill = fill.rectTransform;
                fill.raycastTarget = false;
                var fr = fill.rectTransform;
                fr.anchorMin = new Vector2(0f, 0f); fr.anchorMax = new Vector2(k, 1f);
                fr.offsetMin = new Vector2(3f, 3f); fr.offsetMax = new Vector2(-3f, -3f);
                var gloss = UiFactory.Image("Gloss", fill.transform, UiArt.Box, new Color(1f, 1f, 1f, 0.35f), true);
                gloss.raycastTarget = false;
                gloss.rectTransform.anchorMin = new Vector2(0f, 0.55f); gloss.rectTransform.anchorMax = new Vector2(1f, 0.9f);
                gloss.rectTransform.offsetMin = new Vector2(6f, 0f); gloss.rectTransform.offsetMax = new Vector2(-6f, 0f);
            }
            var star = Decor("Star", track.transform, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            star.preserveAspect = true;
            UiFactory.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(-2f, 2f), new Vector2(50f, 50f));
            var n = UiFactory.Title("Count", track.transform, $"{g.Stars}/{g.To}", 28, Color.white, UseV11 ? (Color)new Color32(0x08, 0x24, 0x5A, 0xFF) : new Color32(0x2A, 0x10, 0x4E, 0xFF));
            n.raycastTarget = false;
            UiFactory.Stretch(n.rectTransform, 26, 8, 0, 2);
            FitText(n, 16);
            if (iceRefs != null) { iceRefs.Count = n; StartVr1Return(iceRefs, shell); }
        }

        // ---- v10 P5: ZPE (Zekâ Performans Endeksi; IQ değildir) ana sayfa kapsülü ve istatistik paneli -----------

        /// <summary>Ana sayfa ZPE kapsülü verisi (GameController ShowLevels öncesi doldurur).</summary>
        public struct ZpeBadgeInfo
        {
            public bool Has, Calibrating;
            public int Score, Count, Min;
        }

        public ZpeBadgeInfo ZpeBadge;
        /// <summary>ZPE kapsülüne dokununca (istatistik paneli).</summary>
        public Action ZpePressed;

        // v11 sütunlar. Kullanıcı (2026-11-08, ikinci karar): ZPE sol sütunun en üstünde, altında Sıralama ve Deneme;
        // sağ sütunda kilitli taş kartı en üste (ZPE'nin boşalttığı yere) çıktı.
        private const float V11ColumnX = 426f, V11ColumnZpeTop = 332f, V11ColumnStoneTop = 332f;
        /// <summary>Sol sütunda ZPE'nin altına kayan düğmelerin (Sıralama, Deneme) ek inişi.</summary>
        private const float V11LeftShift = 210f;

        /// <summary>
        /// v11 ZPE rozeti (kullanıcı, 2026-11-08: "ZPE kilidi açılmayı bekleyen taşın üstünde, ona özel ikon, altında
        /// 100 üzerinden puan"): gösterge (ibre) simgesi, içinde "ZPE"; altında beyaz kenarlı lacivert kapsülde puan
        /// "72/100". Dokununca istatistik paneli.
        /// </summary>
        private void BuildZpeBadge(RectTransform root)
        {
            if (!ZpeBadge.Has) return;
            var z = ZpeBadge;
            var block = UiFactory.Rect("Zpe", root);
            UiFactory.Place(block, new Vector2(0.5f, 1f), new Vector2(-V11ColumnX, -V11ColumnZpeTop - 92f), new Vector2(190f, 184f));
            var halo = Decor("Halo", block, SpecialArt.Glow);
            halo.color = new Color(0.55f, 0.92f, 1f, 0.55f);
            UiFactory.Place(halo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(190f, 190f));
            // ui1 (docs/ui1/01): yalnız ikon görseli değişir — yazısız yeni ZPE ikonu (yükselen grafik). Konum, boyut,
            // puan kapsülü ve dokunma davranışı aynı; "ZPE" adı nesne/erişilebilirlik adı olarak kalır.
            var newIcon = Ui1.Art("icon-zpe");
            var icon = Decor("Icon", block, newIcon ?? ZpeIcon());
            icon.preserveAspect = true;
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(128f, 128f));
            var sh = icon.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.02f, 0.14f, 0.36f, 0.45f);
            sh.effectDistance = new Vector2(0f, -4f);
            if (newIcon == null)
            {
                var lab = UiFactory.Title("Label", icon.transform, Loc.T("zpe.short"), 26, Color.white, V11Navy);
                lab.raycastTarget = false;
                UiFactory.Place(lab.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(90f, 32f));
            }
            var cap = WhiteCapsule("Score", block, V11Navy, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(172f, 54f), 5f);
            string value = z.Calibrating || z.Count == 0 ? "—" : $"{z.Score}<size=60%><color=#9FD8FF>/100</color></size>";
            var v = UiFactory.Title("Value", cap.transform, value, 40, Color.white, new Color32(0x06, 0x1A, 0x44, 0xFF));
            v.richText = true;
            v.raycastTarget = false;
            UiFactory.Stretch(v.rectTransform, 8, 8, 0, 2);
            FitText(v, 22);
            var hit = UiFactory.SpriteButton("ZpeButton", block, null, new Vector2(190f, 184f), () => ZpePressed?.Invoke());
            hit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Stretch((RectTransform)hit.transform);
            PressFeedback.Attach(hit, null);
        }

        private static Sprite _zpeIcon;

        /// <summary>
        /// ZPE'ye özel simge (koddan, 256 px): beyaz kenarlı mavi madalyon, kırmızıdan yeşile beş dilimli gösterge
        /// yayı, yüksek değeri gösteren beyaz ibre ve altın göbek. Alt yarıda "ZPE" yazısına yer bırakır.
        /// </summary>
        internal static Sprite ZpeIcon()
        {
            if (_zpeIcon) return _zpeIcon;
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "zpe_icon", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[N * N];
            const float aa = 1.4f / N;
            float Cv(float d) => Mathf.Clamp01(0.5f - d / aa);
            Color Ov(Color dst, Color src, float a)
            {
                a *= src.a;
                float o = a + dst.a * (1f - a);
                if (o <= 1e-5f) return Color.clear;
                var c = (src * a + dst * dst.a * (1f - a)) / o;
                c.a = o;
                return c;
            }
            Color[] seg =
            {
                new Color32(0xFF, 0x5A, 0x5A, 0xFF), new Color32(0xFF, 0x9F, 0x3A, 0xFF), new Color32(0xFF, 0xD8, 0x3A, 0xFF),
                new Color32(0x9C, 0xE0, 0x4A, 0xFF), new Color32(0x2F, 0xD1, 0x7A, 0xFF),
            };
            const float needleAng = 38f * Mathf.Deg2Rad; // sağ üst (yüksek puan)
            Vector2 nd = new Vector2(Mathf.Cos(needleAng), Mathf.Sin(needleAng));
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float x = (i + 0.5f) / N - 0.5f, y = (j + 0.5f) / N - 0.5f;
                float r = Mathf.Sqrt(x * x + y * y);
                var c = Color.clear;
                c = Ov(c, new Color(0.02f, 0.12f, 0.3f, 0.35f), Mathf.Clamp01(0.5f - (Mathf.Sqrt(x * x + (y + 0.018f) * (y + 0.018f)) - 0.48f) / 0.02f));
                c = Ov(c, Color.white, Cv(r - 0.47f));
                var body = Color.Lerp(new Color32(0x06, 0x5E, 0xB8, 0xFF), new Color32(0x2E, 0xB8, 0xF2, 0xFF), Mathf.Clamp01(0.5f + y * 1.3f));
                c = Ov(c, body, Cv(r - 0.425f));
                c = Ov(c, new Color(1f, 1f, 1f, 0.22f), Cv(Mathf.Sqrt(x * x * 0.7f + (y - 0.25f) * (y - 0.25f) * 3f) - 0.2f) * Cv(r - 0.42f));
                // Gösterge yayı (merkez biraz yukarıda): 200° → -20°, beş dilim, aralarda ince boşluk.
                float gx = x, gy = y + 0.04f;
                float gr = Mathf.Sqrt(gx * gx + gy * gy);
                float ang = Mathf.Atan2(gy, gx) * Mathf.Rad2Deg;
                if (ang < -90f) ang += 360f;
                float t = Mathf.InverseLerp(200f, -20f, ang);
                if (ang <= 200f && ang >= -20f)
                {
                    float band = Mathf.Max(gr - 0.355f, 0.275f - gr);
                    float local = t * 5f;
                    int k = Mathf.Clamp((int)local, 0, 4);
                    float gap = Mathf.Min(local - k, k + 1 - local) * (220f / 5f) * Mathf.Deg2Rad * gr; // yay uzunluğu
                    float a = Cv(band) * Mathf.Clamp01((gap - 0.006f) / aa);
                    c = Ov(c, Color.Lerp(seg[k], Color.white, Mathf.Clamp01((gr - 0.33f) * 12f) * 0.25f), a);
                }
                // İbre: göbekten yaya, beyaz gövde ve lacivert kenar.
                float along = Mathf.Clamp(gx * nd.x + gy * nd.y, 0f, 0.31f);
                float px0 = gx - nd.x * along, py0 = gy - nd.y * along;
                float width = 0.03f * (1f - along / 0.36f) + 0.006f;
                float dn = Mathf.Sqrt(px0 * px0 + py0 * py0) - width;
                c = Ov(c, new Color32(0x0A, 0x24, 0x5C, 0xFF), Cv(dn - 0.012f));
                c = Ov(c, Color.white, Cv(dn));
                // Göbek.
                c = Ov(c, new Color32(0x0A, 0x24, 0x5C, 0xFF), Cv(gr - 0.062f));
                c = Ov(c, new Color32(0xFF, 0xC8, 0x3A, 0xFF), Cv(gr - 0.048f));
                c = Ov(c, new Color(1f, 1f, 1f, 0.7f), Cv(Mathf.Sqrt((gx + 0.014f) * (gx + 0.014f) + (gy - 0.016f) * (gy - 0.016f)) - 0.014f));
                px[j * N + i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            _zpeIcon = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            _zpeIcon.name = "zpe_icon";
            return _zpeIcon;
        }

        private void BuildZpeBadgeV10(RectTransform root)
        {
            if (!ZpeBadge.Has) return;
            var z = ZpeBadge;
            var cap = GoldCapsule("ZpeCapsule", root, new Color32(0x17, 0x8E, 0x88, 0xFF), new Vector2(0.5f, 0.5f), new Vector2(-385f, 0f), new Vector2(230f, 70f), 6f);
            var label = UiFactory.Text("Label", cap.transform, Loc.T("zpe.short"), 24, new Color32(0xD9, 0xFF, 0xF8, 0xFF), TextAnchor.MiddleLeft);
            label.raycastTarget = false;
            UiFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(46f, 0f), new Vector2(70f, 40f));
            string value = z.Calibrating ? $"{z.Count}/{z.Min}" : z.Score.ToString();
            var v = UiFactory.Title("Value", cap.transform, value, 40, Color.white, new Color32(0x0E, 0x45, 0x42, 0xFF));
            v.raycastTarget = false;
            UiFactory.Place(v.rectTransform, new Vector2(0f, 0.5f), new Vector2(148f, 0f), new Vector2(130f, 60f));
            FitText(v, 22);
            var info = UiFactory.Text("Info", cap.transform, "i", 26, new Color32(0x0E, 0x45, 0x42, 0xFF));
            info.raycastTarget = false;
            var infoBg = UiFactory.Image("InfoBg", cap.transform, UiArt.Circle, new Color32(0xD9, 0xFF, 0xF8, 0xFF));
            infoBg.raycastTarget = false;
            UiFactory.Place(infoBg.rectTransform, new Vector2(1f, 1f), new Vector2(-4f, 4f), new Vector2(34f, 34f));
            info.transform.SetParent(infoBg.transform, false);
            UiFactory.Stretch(info.rectTransform);
            var hit = UiFactory.SpriteButton("ZpeButton", root, null, new Vector2(250f, 90f), () => ZpePressed?.Invoke());
            hit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Place((RectTransform)hit.transform, new Vector2(0.5f, 0.5f), new Vector2(-385f, 0f), new Vector2(250f, 90f));
        }

        /// <summary>ZPE istatistik paneli verisi.</summary>
        public struct ZpeStatsModel
        {
            public bool Calibrating;
            public int Score, Count, Min, Trend, WinPct, FirstPct;
            public float Efficiency, Difficulty, FirstTry, Time, NoAssist;
        }

        private RectTransform _zpeModal;
        public bool IsZpeShown => _zpeModal != null;

        public void CloseZpe()
        {
            if (_zpeModal != null) UiFactory.Kill(_zpeModal.gameObject);
            _zpeModal = null;
        }

        /// <summary>
        /// ZPE paneli (kullanıcı, 2026-11-07: puana dokununca istatistik): puan ya da kalibrasyon, beş bileşen çubuğu,
        /// bölüm sayısı, başarı ve ilk deneme oranı, son 10 bölüm eğilimi, "IQ değildir" notu. Harita üstünde modal.
        /// </summary>
        public void ShowZpeStats(ZpeStatsModel m)
        {
            CloseZpe();
            if (_page == null) return;
            if (Ui1.Ready) { ShowZpeStatsUi1(m); return; } // ui1/06
            var root = UiFactory.Rect("ZpeModal", _page);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _zpeModal = root;
            var dim = UiFactory.SpriteButton("Dim", root, UiArt.Box, new Vector2(10f, 10f), CloseZpe);
            dim.GetComponent<Image>().color = new Color(0.10f, 0.06f, 0.18f, 0.6f);
            UiFactory.Stretch((RectTransform)dim.transform);
            const float W = 820f, H = 1120f;
            var card = GoldCapsule("Card", root, new Color32(0xFF, 0xF4, 0xE0, 0xFF), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(W, H), 12f);
            card.raycastTarget = true;
            var c = card.rectTransform;
            Color ink = ShellNavy;

            var title = UiFactory.Title("Title", c, Loc.T("zpe.title"), 46, ink, PillCream);
            title.raycastTarget = false;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(680f, 70f));
            FitText(title, 26);
            // v11: 100 üzerinden, oynanan bütün oyunların ortalaması.
            var big = UiFactory.Title("Score", c, m.Calibrating ? Loc.F("zpe.calibrating", m.Count, m.Min) : m.Score + "<size=50%>/100</size>", m.Calibrating ? 52 : 110, ShellTealDark, PillCream);
            big.richText = true;
            big.raycastTarget = false;
            UiFactory.Place(big.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(680f, 120f));
            FitText(big, 30);
            string sub = m.Calibrating ? Loc.F("zpe.stat.calib", Mathf.Max(0, m.Min - m.Count)) : Loc.F("zpe.stat.trend", (m.Trend > 0 ? "+" : "") + m.Trend);
            var st = UiFactory.Text("Sub", c, sub, 30, ink, TextAnchor.MiddleCenter, FontStyle.Normal);
            st.raycastTarget = false;
            UiFactory.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -258f), new Vector2(700f, 50f));
            FitText(st, 18);

            var parts = new (string key, float v)[]
            {
                ("zpe.part.efficiency", m.Efficiency), ("zpe.part.difficulty", m.Difficulty), ("zpe.part.firsttry", m.FirstTry),
                ("zpe.part.time", m.Time), ("zpe.part.noassist", m.NoAssist),
            };
            float y = -330f;
            foreach (var (key, v) in parts)
            {
                var lab = UiFactory.Text("Part", c, Loc.T(key), 30, ink, TextAnchor.MiddleLeft, FontStyle.Normal);
                lab.raycastTarget = false;
                UiFactory.Place(lab.rectTransform, new Vector2(0.5f, 1f), new Vector2(-150f, y), new Vector2(400f, 44f));
                FitText(lab, 18);
                var tr = UiFactory.Image("Track", c, UiArt.Box, new Color32(0xE8, 0xDA, 0xC4, 0xFF), true);
                tr.raycastTarget = false;
                UiFactory.Place(tr.rectTransform, new Vector2(0.5f, 1f), new Vector2(200f, y), new Vector2(260f, 30f));
                if (v > 0.01f)
                {
                    var fl = UiFactory.Image("Fill", tr.transform, UiArt.Box, ShellTeal, true);
                    fl.raycastTarget = false;
                    fl.rectTransform.anchorMin = Vector2.zero; fl.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
                    fl.rectTransform.offsetMin = new Vector2(3f, 3f); fl.rectTransform.offsetMax = new Vector2(-3f, -3f);
                }
                var pct = UiFactory.Text("Pct", c, Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "", 28, ink, TextAnchor.MiddleRight);
                pct.raycastTarget = false;
                UiFactory.Place(pct.rectTransform, new Vector2(0.5f, 1f), new Vector2(370f, y), new Vector2(60f, 40f));
                y -= 76f;
            }
            y -= 10f;
            foreach (var line in new[] { Loc.F("zpe.stat.levels", m.Count), Loc.F("zpe.stat.win", m.WinPct), Loc.F("zpe.stat.first", m.FirstPct) })
            {
                var t = UiFactory.Text("Stat", c, line, 30, ink, TextAnchor.MiddleCenter, FontStyle.Normal);
                t.raycastTarget = false;
                UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(700f, 44f));
                FitText(t, 18);
                y -= 52f;
            }
            var note = UiFactory.Text("Note", c, Loc.T("zpe.note"), 26, new Color32(0x8A, 0x5A, 0x2A, 0xFF), TextAnchor.MiddleCenter, FontStyle.Normal);
            note.raycastTarget = false;
            UiFactory.Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(720f, 90f));
            note.enableAutoSizing = true; note.fontSizeMax = 26; note.fontSizeMin = 16;
            var close = ShellRoundButton("Back", c, PremiumArt.V4("icons/back") ?? UiArt.Icon(UiIcon.Back), 92f, CloseZpe);
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(92f, 92f));
        }

        /// <summary>Görünen bölgenin başlık parçaları (harita kayınca yeniden kurulur).</summary>
        private void HeaderCard(RectTransform parent, int region, int first, int last, int stars)
        {
            var theme = ThemeDefinition.ForLevel(region * ThemeDefinition.RegionSize);
            var m = _map;
            if (m != null && m.CardBack != null) UiFactory.Kill(m.CardBack.gameObject);
            var back = UiFactory.Rect("Card", m != null && m.HeadBack != null ? m.HeadBack : parent);
            UiFactory.Stretch(back);
            var front = UiFactory.Rect("Card", parent);
            UiFactory.Stretch(front);
            if (m != null) { m.Card = front; m.CardBack = back; }

            if (UseV11) { HeaderCardV11(back, front, theme, region, first, last); return; }

            var icon = SpecialArt.V6("ui/region-" + theme.Id);
            if (icon != null)
            {
                var img = Decor("RegionIcon", back, icon);
                img.preserveAspect = true;
                UiFactory.Place(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(-394f, -144f), new Vector2(264f, 252f));
            }

            var chipArt = ShellArt("region-chip");
            Image chip;
            if (chipArt != null)
            {
                // 1678×432 turkuaz etiket: altın orta plakanın üst sınırına biner.
                chip = Decor("Chip", front, chipArt);
                chip.preserveAspect = true;
                UiFactory.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -189f), new Vector2(222f, 222f * chipArt.rect.height / chipArt.rect.width));
            }
            else
            {
                chip = UiFactory.Image("Chip", front, UiArt.Box, ShellTeal, true);
                chip.raycastTarget = false;
                UiFactory.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -189f), new Vector2(216f, 56f));
                var co = chip.gameObject.AddComponent<Outline>();
                co.effectColor = new Color32(0xD9, 0xFF, 0xF8, 0xFF);
                co.effectDistance = new Vector2(2f, -2f);
            }
            // Kullanıcı (2026-11-07): bölge numarası/adı/aralığı kaldırıldı; şeridin orta plakasında ZPE (Zekâ Performans
            // Endeksi; IQ değildir): etikette "ZPE", ortada büyük puan (kalibrasyonda ilerleme), altında açık ad.
            // Plakaya dokununca istatistik paneli.
            var z = ZpeBadge;
            var ct = UiFactory.Text("Text", chip.transform, Loc.T("zpe.short"), 32, ShellNavy);
            UiFactory.Stretch(ct.rectTransform, 12, 12, 0, 2);
            ct.raycastTarget = false;
            FitText(ct, 18);

            string value = !z.Has ? "" : z.Calibrating ? Loc.F("zpe.calibrating", z.Count, z.Min) : z.Score.ToString();
            var title = UiFactory.Title("ZpeValue", front, value, z.Calibrating ? 38 : 56, ShellNavy, PillCream);
            title.raycastTarget = false;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -254f), new Vector2(330f, 62f));
            FitText(title, 24);
            var range = UiFactory.Text("ZpeName", front, Loc.T("zpe.title"), 24, ShellAmber, TextAnchor.MiddleCenter, FontStyle.Normal);
            range.raycastTarget = false;
            UiFactory.Place(range.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(330f, 34f));
            FitText(range, 16);
            var hit = UiFactory.SpriteButton("ZpeButton", front, null, new Vector2(360f, 150f), () => ZpePressed?.Invoke());
            hit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Place((RectTransform)hit.transform, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(360f, 150f));
        }

        /// <summary>
        /// v11 bölge kartının canlı parçaları (referans: solda fener, ortada bölge adı, ince ayırıcı, sağda turkuaz
        /// kapsülde "BÖLGE n • ilk–son"). Ad ve aralık kaydırılan bölgeden gelir.
        /// </summary>
        private void HeaderCardV11(RectTransform back, RectTransform front, ThemeDefinition theme, int region, int first, int last)
        {
            if (Ui3.Ready) { HeaderCardUi3(front, theme, region, first, last); return; }
            const float cy = -V11CardTop - 76f; // kartın görünür ortası (alt turkuaz kenar hariç)
            var icon = SpecialArt.V6("ui/region-" + theme.Id);
            if (icon != null)
            {
                var img = Decor("RegionIcon", front, icon);
                img.preserveAspect = true;
                UiFactory.Place(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(-424f, cy + 12f), new Vector2(186f, 186f));
            }
            var title = UiFactory.Title("RegionName", front, ThemeDefinition.RegionTitle(region), 62, V11Navy, Color.white);
            title.raycastTarget = false;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(-104f, cy), new Vector2(400f, 84f));
            FitText(title, 30);
            var sep = UiFactory.Image("Separator", front, UiArt.Box, new Color(0.07f, 0.18f, 0.43f, 0.22f));
            sep.raycastTarget = false;
            UiFactory.Place(sep.rectTransform, new Vector2(0.5f, 1f), new Vector2(132f, cy), new Vector2(3f, 66f));

            var chipArt = V11("region-chip");
            var chip = Decor("Chip", front, chipArt);
            float w = 344f;
            UiFactory.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(330f, cy), new Vector2(w, w * chipArt.rect.height / chipArt.rect.width));
            var ct = UiFactory.Text("Text", chip.transform, Loc.F("ui.region", region + 1) + " • " + first + "–" + last, 32, V11Navy);
            ct.raycastTarget = false;
            UiFactory.Stretch(ct.rectTransform, 22, 22, 0, 4);
            FitText(ct, 18);
        }

        /// <summary>Sağ üst köşede Ayarlar: turkuaz gövde, altın çerçeve, krem dişli.</summary>
        private static void TopGear(RectTransform page, Action settings, Vector2 pos)
        {
            var b = ShellRoundButton(Loc.T("ui.settings"), page, PremiumArt.V4("icons/settings") ?? UiArt.Icon(UiIcon.SoundOn), 116f, settings);
            UiFactory.Place((RectTransform)b.transform, new Vector2(1f, 1f), pos, new Vector2(116f, 116f));
        }

        // ---- Alt menü ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Ortak alt menü: Harita · Günlük · Taşlar · Ödüller. active: 1 harita, TabDaily, TabCollection, diğerleri
        /// (0 eski ana sayfa, 2 ayarlar) hiçbiri seçili değil. Seçili sekmeye basmak sayfayı yeniden kurmaz.
        /// Ödüller için doğrulanmış hedef ekran yok: sekme görünür, basınca "yakında" bildirimi (ekonomi yok).
        /// </summary>
        // Son kurulan alt menü (günlük modalı açılınca seçili sekmeyi değiştirmek için yeniden kurulur).
        private RectTransform _tabsHolder, _tabsPage;
        private int _tabsActive;
        private Action _tabsHome, _tabsLevels, _tabsSettings;

        /// <summary>
        /// Kullanıcı (2026-11-11: "günlük modalı açıksa günlük menüsü seçili olsun"): alt menü <paramref name="active"/> seçili
        /// olarak yeniden kurulur. <paramref name="onTop"/>: modalın üstünde (karartmanın dışında, dokunulabilir).
        /// </summary>
        private void RetargetTabs(int active, Action levels, bool onTop)
        {
            if (_tabsHolder == null || _tabsPage == null) return;
            int idx = _tabsHolder.GetSiblingIndex();
            UiFactory.Kill(_tabsHolder.gameObject);
            Tabs(_tabsPage, active, _tabsHome, levels, _tabsSettings);
            if (onTop) _tabsHolder.SetAsLastSibling(); else _tabsHolder.SetSiblingIndex(idx);
        }

        private void Tabs(RectTransform page, int active, Action home, Action levels, Action settings)
        {
            var holder = UiFactory.Rect("Tabs", page);
            _tabsHolder = holder; _tabsPage = page; _tabsActive = active; _tabsHome = home; _tabsLevels = levels; _tabsSettings = settings;
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0f);
            holder.pivot = new Vector2(0.5f, 0f);
            holder.sizeDelta = new Vector2(1080f, ShellBarH + ShellBarBleed);
            holder.anchoredPosition = new Vector2(0f, -ShellBarBleed);

            var rt = (RectTransform)holder;
            float baseY = ShellBarBleed; // güvenli alanın alt kenarı (holder içinde)
            if (UseV11)
            {
                TabsV11(rt, baseY, active, levels);
                return;
            }
            BottomPlate(holder, ShellBarBleed);
            foreach (float x in TabDividerX)
            {
                var d = UiFactory.Image("Divider", rt, UiArt.Box, DividerColor);
                d.raycastTarget = false;
                UiFactory.Place(d.rectTransform, new Vector2(0.5f, 0f), new Vector2(x, baseY + 60f + 55f), new Vector2(3f, 110f));
            }

            bool mapOn = active == 1;
            ShellTab(rt, 0, baseY, Loc.T("ui.map"), MapIcon(), mapOn, mapOn ? null : levels);
            ShellTab(rt, 1, baseY, Loc.T("daily.short"), CalendarIcon(), active == TabDaily, active == TabDaily ? null : (Action)(() => DailyPressed?.Invoke()));
            ShellTab(rt, 2, baseY, Loc.T("collection.short"), ShellArt("icon-crystal") ?? CrystalIconTemp(), active == TabCollection, active == TabCollection ? null : (Action)(() => CollectionPressed?.Invoke()));
        }

        /// <summary>Menü öğesi (v11 düzeni için: ad, simge, seçili mi, eylem).</summary>
        internal struct TabItem
        {
            public string Label;
            public Sprite Icon;
            public bool On;
            public Action Act;
        }

        /// <summary>
        /// v11 düz mavi alt menü (referans: dalgasız, alttan bağlı; mavi zemin cihazın gerçek alt kenarına uzanır;
        /// ikon ve yazılar güvenli alanın üstünde). Seçili öğenin arkasında krem kart. N öğe W iç genişliğe eşit
        /// dağılır (x0 + (i + ½)·W/N); seçilen tasarımda ayırıcı yok. Kullanıcı kararı (2026-11-07): Ödüller kaldırıldı,
        /// üç öğe (Harita · Günlük · Taşlar).
        /// </summary>
        private void TabsV11(RectTransform holder, float baseY, int active, Action levels)
        {
            bool mapOn = active == 1;
            var items = new[]
            {
                new TabItem { Label = Loc.T("ui.map"), Icon = Vr1Proto.On && Vr1Proto.NavMap ? Vr1Proto.NavMap : MapIcon(), On = mapOn, Act = mapOn ? null : levels },
                new TabItem { Label = Loc.T("daily.short"), Icon = Vr1Proto.On && Vr1Proto.NavDaily ? Vr1Proto.NavDaily : CalendarIcon(), On = active == TabDaily, Act = active == TabDaily ? null : (Action)(() => DailyPressed?.Invoke()) },
                new TabItem { Label = Loc.T("collection.short"), Icon = Vr1Proto.NavStones ? Vr1Proto.NavStones : V11("icon-stones") ?? ShellArt("icon-crystal") ?? CrystalIconTemp(), On = active == TabCollection, Act = active == TabCollection ? null : (Action)(() => CollectionPressed?.Invoke()) },
            };
            BuildTabBarV11(holder, baseY, items);
        }

        /// <summary>
        /// Yalnız yakalama/doğrulama: alt menüyü N (0–5) öğeyle yeniden kurar (v11 §Teslim: üç/dört/beş öğe yerleşimi).
        /// Üretime sekme eklemez; dokunmalar etkisiz.
        /// </summary>
        public void PreviewTabBar(int n)
        {
            if (_page == null || !UseV11) return;
            var old = _page.Find("Tabs");
            if (old != null) UiFactory.Kill(old.gameObject);
            var holder = UiFactory.Rect("Tabs", _page);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0f);
            holder.pivot = new Vector2(0.5f, 0f);
            holder.sizeDelta = new Vector2(1080f, ShellBarH + ShellBarBleed);
            holder.anchoredPosition = new Vector2(0f, -ShellBarBleed);
            var all = new[]
            {
                new TabItem { Label = Loc.T("ui.map"), Icon = MapIcon(), On = true },
                new TabItem { Label = Loc.T("daily.short"), Icon = CalendarIcon() },
                new TabItem { Label = Loc.T("collection.short"), Icon = V11("icon-stones") },
                new TabItem { Label = Loc.T("rewards.short"), Icon = ShellArt("icon-chest") ?? ChestIconTemp() },
                new TabItem { Label = Loc.T("ui.settings"), Icon = PremiumArt.V4("icons/settings") ?? UiArt.Icon(UiIcon.SoundOn) },
            };
            n = Mathf.Clamp(n, 0, all.Length);
            var items = new TabItem[n];
            Array.Copy(all, items, n);
            BuildTabBarV11(holder, ShellBarBleed, items);
        }

        /// <summary>Mavi alt menü gövdesi + N öğe (yeniden çizimde eski öğeler holder ile birlikte gider; birikme yok).</summary>
        internal static void BuildTabBarV11(RectTransform holder, float baseY, TabItem[] items)
        {
            if (Ui3.Ready) { BuildTabBarUi3(holder, baseY, items); return; }
            // Zemin: üst kenarı güvenli alttan ShellBarH; altı ekran kenarının da altına uzanır (çentikli telefon).
            var deep = UiFactory.Image("BarFill", holder, UiArt.Box, V11BlueDeep);
            deep.raycastTarget = false;
            var drt = deep.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0f);
            drt.pivot = new Vector2(0.5f, 1f);
            drt.sizeDelta = new Vector2(1600f, 600f);
            drt.anchoredPosition = new Vector2(0f, baseY);
            V11Panel("Bar", holder, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), baseY + ShellBarH, ShellBarH + 300f);
            var shadow = Decor("BarShadow", holder, VShadow());
            shadow.color = new Color(0.02f, 0.16f, 0.34f, 0.26f);
            var srt = shadow.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(1600f, 30f);
            srt.anchoredPosition = new Vector2(0f, baseY + ShellBarH + 15f);
            srt.localScale = new Vector3(1f, -1f, 1f); // koyu taraf panelin üst kenarında, yukarı söner

            int n = items?.Length ?? 0;
            if (n == 0) return; // güvenli boş durum
            const float inner = 1040f;
            float slot = inner / n, x0 = -inner * 0.5f;
            for (int i = 0; i < n; i++) TabV11(holder, x0 + (i + 0.5f) * slot, Mathf.Min(slot, 330f), baseY, items[i]);
        }

        private static void TabV11(RectTransform bar, float x, float slot, float baseY, TabItem it)
        {
            var hit = UiFactory.SpriteButton(it.Label, bar, null, new Vector2(slot, ShellBarH), it.Act);
            hit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Place((RectTransform)hit.transform, new Vector2(0.5f, 0f), new Vector2(x, baseY + ShellBarH * 0.5f), new Vector2(slot, ShellBarH));
            if (it.On)
            {
                var cardArt = V11("tab-card");
                float cw = Mathf.Min(232f, slot - 16f);
                var card = Decor("Card", hit.transform, cardArt);
                UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(cw, cw * cardArt.rect.height / cardArt.rect.width));
            }
            var ic = Decor("Icon", hit.transform, it.Icon);
            ic.preserveAspect = true;
            UiFactory.Place(ic.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 146f), new Vector2(108f, 108f));
            float maxW = Mathf.Min(slot, 330f) - 50f;
            if (it.On)
            {
                var probe = UiFactory.Text("Label", hit.transform, it.Label, 36, Color.white);
                float w = Mathf.Clamp(probe.GetPreferredValues(it.Label, maxW, 50f).x + 40f, 120f, Mathf.Min(maxW, 196f));
                UiFactory.Kill(probe.gameObject);
                var pill = UiFactory.Image("Active", hit.transform, UiArt.Box, new Color32(0x14, 0x86, 0x9A, 0xFF), true);
                pill.raycastTarget = false;
                UiFactory.Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(w, 54f));
                var po = pill.gameObject.AddComponent<Outline>();
                po.effectColor = new Color32(0x0B, 0x5A, 0x6A, 0xFF);
                po.effectDistance = new Vector2(2f, -2f);
            }
            var t = UiFactory.Title("Label", hit.transform, it.Label, 36, Color.white, it.On ? (Color)new Color32(0x0B, 0x4A, 0x58, 0xFF) : V11Navy);
            t.raycastTarget = false;
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(it.On ? Mathf.Min(maxW, 196f) - 24f : maxW, 50f));
            FitText(t, 22);
        }

        /// <summary>
        /// Alt kabuk gövdesi (menü alt çubuğu ve oyun içi alt kabuk ortak). <paramref name="holder"/> alttan pivotlu;
        /// <paramref name="safeY"/> güvenli alanın alt kenarının holder içindeki yüksekliği. Final görsel
        /// (Koleksiyon-Parcalari/01-alt-menu-kabugu, 2172×450) oranı korunarak 1080 genişlikte çizilir; altında hareket
        /// çubuğu payına krem dolgu uzanır. Final yoksa GEÇİCİ çizim. Dekor tıklama almaz.
        /// </summary>
        internal static Image BottomPlate(RectTransform holder, float safeY)
        {
            var art = ShellArt("bottom-bar");
            if (art == null)
            {
                var tmp = Decor("Bar", holder, BottomBarTemp());
                tmp.rectTransform.anchorMin = tmp.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                tmp.rectTransform.pivot = new Vector2(0.5f, 0f);
                tmp.rectTransform.sizeDelta = new Vector2(1080f, ShellBarH + ShellBarBleed);
                tmp.rectTransform.anchoredPosition = new Vector2(0f, safeY - ShellBarBleed);
                BlockOpaque(tmp);
                return tmp;
            }
            float h = 1080f * art.rect.height / art.rect.width;
            var fill = UiFactory.Image("BarFill", holder, UiArt.Box, new Color32(0xFC, 0xE1, 0xA3, 0xFF));
            fill.raycastTarget = false;
            fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            fill.rectTransform.pivot = new Vector2(0.5f, 1f);
            fill.rectTransform.sizeDelta = new Vector2(1080f, 400f);
            fill.rectTransform.anchoredPosition = new Vector2(0f, safeY + 40f);
            var plate = Decor("Bar", holder, art);
            plate.rectTransform.anchorMin = plate.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            plate.rectTransform.pivot = new Vector2(0.5f, 0f);
            plate.rectTransform.sizeDelta = new Vector2(1080f, h);
            plate.rectTransform.anchoredPosition = new Vector2(0f, safeY - 8f);
            return plate;
        }

        /// <summary>
        /// Sekme (kullanıcı, 2026-11-07: seçili sekme turkuaz; kapsül sekme alanının dışına taşmaz). Simge ve yazı ayrı
        /// öğeler; dokunma alanı ikisini kapsar. Turkuaz kapsül yazının genişliğine göre, en çok sekme genişliği − 50.
        /// </summary>
        private static void ShellTab(RectTransform bar, int i, float baseY, string text, Sprite icon, bool on, Action act)
        {
            float x = TabX[i], slot = TabSlotW;
            var hit = UiFactory.SpriteButton(text, bar, null, new Vector2(slot, 200f), act);
            hit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Place((RectTransform)hit.transform, new Vector2(0.5f, 0f), new Vector2(x, baseY + 100f), new Vector2(slot, 200f));
            var ic = Decor("Icon", hit.transform, icon);
            ic.preserveAspect = true;
            UiFactory.Place(ic.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, TabIconY), new Vector2(112f, 112f));
            var t = UiFactory.Text("Label", hit.transform, text, 38, on ? PillCream : ShellNavy);
            t.raycastTarget = false;
            float maxW = slot - 50f;
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, TabLabelY), new Vector2(maxW - 28f, 50f));
            FitText(t, 22);
            if (!on) return;
            float w = Mathf.Clamp(t.GetPreferredValues(text, maxW - 28f, 50f).x + 44f, 120f, maxW);
            var pill = UiFactory.Image("Active", hit.transform, UiArt.Box, ShellTealDark, true);
            pill.raycastTarget = false;
            UiFactory.Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, TabLabelY), new Vector2(w, 58f));
            var po = pill.gameObject.AddComponent<Outline>();
            po.effectColor = ShellTeal;
            po.effectDistance = new Vector2(2f, -2f);
            pill.transform.SetSiblingIndex(t.transform.GetSiblingIndex()); // yazının arkasında
        }

        // ---- GEÇİCİ çizimler (final görsel gelene kadar) -----------------------------------------------------------

        private static Sprite _tmpClouds, _tmpRibbon, _tmpLaurel, _tmpBar, _tmpCrystal, _tmpChest;

        private static Texture2D NewTex(int w, int h, string name) =>
            new Texture2D(w, h, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };

        private static Sprite Finish(Texture2D tex, Color32[] px)
        {
            tex.SetPixels32(px);
            tex.Apply(true, false); // okunabilir kalır: opak alan tıklama testi için
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float S(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        /// <summary>Şerit konturu (şerit kutusunun üstünden aşağı, birim). Uçlar yukarı kıvrılır, ortada geniş oyuk.</summary>
        private static void RibbonEdges(float x, out float top, out float bottom)
        {
            float curlL = 1f - S(0f, 230f, x), curlR = S(850f, 1080f, x);
            float curl = Mathf.Max(curlL, curlR);
            float plate = S(255f, 345f, x) * (1f - S(735f, 825f, x));
            float sideTop = 70f - 62f * curl * curl;
            float sideBot = sideTop + 70f - 14f * curl;
            top = Mathf.Lerp(sideTop, 20f, plate);
            bottom = Mathf.Lerp(sideBot, 182f, plate);
        }

        private static Sprite TopRibbonTemp()
        {
            if (_tmpRibbon) return _tmpRibbon;
            int w = 1080, h = (int)RibbonH;
            var tex = NewTex(w, h, "shell_ribbon_tmp");
            var px = new Color32[w * h];
            Color gold = new Color32(0xF4, 0xC4, 0x52, 0xFF), goldDark = new Color32(0xC8, 0x86, 0x22, 0xFF), goldHi = new Color32(0xFF, 0xE8, 0x9A, 0xFF);
            Color cream = new Color32(0xFF, 0xF3, 0xDC, 0xFF), creamDeep = new Color32(0xF6, 0xDF, 0xB8, 0xFF), rim = new Color32(0xE2, 0x84, 0x2C, 0xFF);
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = i + 0.5f, y = h - (j + 0.5f); // y: üstten
                RibbonEdges(x, out float t, out float b);
                Color c = new Color(0, 0, 0, 0);
                if (y > b && y < b + 14f)
                {
                    float a = (1f - (y - b) / 14f);
                    c = new Color(0.35f, 0.18f, 0.05f, 0.30f * a * a);
                }
                float d = Mathf.Min(y - t, b - y);
                if (d > -1f)
                {
                    float cov = Mathf.Clamp01(d + 0.5f);
                    float k = (y - t) / Mathf.Max(1f, b - t);
                    Color fill;
                    if (d < 11f)
                    {
                        // Altın çerçeve: üstte parlak, altta koyu; alt kenarda sıcak turuncu.
                        fill = b - y < 11f ? Color.Lerp(rim, goldDark, (b - y) / 11f) : Color.Lerp(goldHi, gold, d / 11f);
                    }
                    else if (d < 15f) fill = Color.Lerp(gold, goldDark, (d - 11f) / 4f);
                    else fill = Color.Lerp(cream, creamDeep, k * k);
                    fill.a = cov;
                    c = cov >= 1f ? fill : Color.Lerp(c, fill, cov);
                    if (cov < 1f) c.a = Mathf.Max(c.a, cov);
                }
                px[j * w + i] = c;
            }
            return _tmpRibbon = Finish(tex, px);
        }

        internal static Sprite TopCloudsTemp()
        {
            if (_tmpClouds) return _tmpClouds;
            int w = 540, h = (int)((ShellTopPlateH + ShellTopBleed) / 2f); // yarım çözünürlük (yumuşak sahne)
            var tex = NewTex(w, h, "shell_clouds_tmp");
            var px = new Color32[w * h];
            Color top = new Color32(0x4E, 0x23, 0x8E, 0xFF), bottom = new Color32(0x9C, 0x6C, 0xD8, 0xFF);
            Color cloud = new Color32(0xE9, 0xDD, 0xFA, 0xFF), cloudShade = new Color32(0xB9, 0x9C, 0xE6, 0xFF);
            // Bulut kümeleri (x, y üstten, yarıçap) tam ölçek birimde.
            var blobs = new[] { new Vector3(70, 330, 70), new Vector3(170, 350, 80), new Vector3(300, 340, 60), new Vector3(420, 360, 70),
                new Vector3(560, 350, 75), new Vector3(690, 345, 65), new Vector3(800, 355, 80), new Vector3(930, 335, 70), new Vector3(1040, 345, 80),
                new Vector3(140, 230, 42), new Vector3(960, 220, 46), new Vector3(760, 250, 34), new Vector3(330, 255, 30) };
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = (i + 0.5f) * 2f, y = (h - (j + 0.5f)) * 2f; // tam ölçek, üstten (taşma dahil)
                Color c = Color.Lerp(top, bottom, S(60f, 400f, y));
                float best = 0f, shade = 0f;
                foreach (var bl in blobs)
                {
                    float dx = x - bl.x, dy = (y - bl.y) * 1.15f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 1f - S(bl.z - 10f, bl.z + 6f, r);
                    if (a > best) { best = a; shade = Mathf.Clamp01((y - bl.y + bl.z * 0.2f) / bl.z); }
                }
                if (best > 0f) c = Color.Lerp(c, Color.Lerp(cloud, cloudShade, shade * 0.8f), best * 0.92f);
                // Alt kenar şeridin arkasında kalır; şerit konturunun altına taşmasın.
                RibbonEdges(x, out _, out float rb);
                float limit = RibbonTop + rb - 8f + ShellTopBleed;
                c.a = 1f - S(limit - 2f, limit + 2f, y);
                px[j * w + i] = c;
            }
            return _tmpClouds = Finish(tex, px);
        }

        private static Sprite LaurelTemp()
        {
            if (_tmpLaurel) return _tmpLaurel;
            int w = 96, h = 136;
            var tex = NewTex(w, h, "shell_laurel_tmp");
            var px = new Color32[w * h];
            Color leaf = new Color32(0xE9, 0xB0, 0x3A, 0xFF), leafHi = new Color32(0xFF, 0xDD, 0x7A, 0xFF), stem = new Color32(0xB9, 0x7A, 0x1E, 0xFF);
            // Sol dal: alttan yukarı sola kavis (merkez (88, 10), yarıçap 76, açı 200°→105°).
            Vector2 center = new Vector2(92f, 18f);
            const float R = 74f;
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                var p = new Vector2(i + 0.5f, j + 0.5f);
                Color c = new Color(0, 0, 0, 0);
                float ang = Mathf.Atan2(p.y - center.y, p.x - center.x) * Mathf.Rad2Deg;
                float rr = (p - center).magnitude;
                if (ang > 95f && ang < 205f && Mathf.Abs(rr - R) < 3.2f) c = stem;
                for (int k = 0; k < 6; k++)
                {
                    float a = (190f - k * 17f) * Mathf.Deg2Rad;
                    var on = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R;
                    var tan = new Vector2(-Mathf.Sin(a), Mathf.Cos(a));
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var dir = (tan + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (0.9f * s)).normalized;
                        var lc = on + dir * 15f;
                        var q = p - lc;
                        float u = Vector2.Dot(q, dir), v = Vector2.Dot(q, new Vector2(-dir.y, dir.x));
                        float e = (u * u) / (15f * 15f) + (v * v) / (7.5f * 7.5f);
                        if (e < 1f) c = Color.Lerp(leafHi, leaf, Mathf.Clamp01(e + (v > 0 ? 0.3f : 0f)));
                    }
                }
                px[j * w + i] = c;
            }
            return _tmpLaurel = Finish(tex, px);
        }

        internal static Sprite BottomBarTemp()
        {
            if (_tmpBar) return _tmpBar;
            int w = 1080, h = (int)(ShellBarH + ShellBarBleed);
            var tex = NewTex(w, h, "shell_bar_tmp");
            var px = new Color32[w * h];
            Color gold = new Color32(0xF2, 0xBE, 0x4A, 0xFF), goldDark = new Color32(0xC4, 0x84, 0x24, 0xFF), goldHi = new Color32(0xFF, 0xE6, 0x96, 0xFF);
            Color cream = new Color32(0xFF, 0xF4, 0xE0, 0xFF), creamDeep = new Color32(0xF4, 0xDB, 0xB2, 0xFF), bevel = new Color32(0xD9, 0x92, 0x48, 0xFF);
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = i + 0.5f, yb = j + 0.5f; // alttan
                // Üst kontur: düz, uçlarda yukarı kıvrık (görünür üst kenar güvenli alttan ShellBarH).
                float curl = Mathf.Max(1f - S(0f, 150f, x), S(930f, 1080f, x));
                float top = ShellBarBleed + ShellBarH - 34f + 34f * curl * curl;
                float d = top - yb;
                Color c = new Color(0, 0, 0, 0);
                if (d > -10f && d <= 0f) c = new Color(0.3f, 0.16f, 0.05f, 0.18f * (1f + d / 10f)); // üst gölge payı yok; hafif hale
                if (d > -1f)
                {
                    float cov = Mathf.Clamp01(d + 0.5f);
                    Color fill;
                    if (d < 9f) fill = Color.Lerp(goldHi, gold, d / 9f);
                    else if (d < 13f) fill = Color.Lerp(gold, goldDark, (d - 9f) / 4f);
                    else if (d > 24f && d < 28f && x > 40f && x < 1040f) fill = Color.Lerp(gold, goldDark, 0.3f);
                    else fill = Color.Lerp(cream, creamDeep, S(13f, top - ShellBarBleed, d));
                    // Alt sıcak bevel: güvenli alt kenarın hemen üstünde ve altında.
                    float bv = S(ShellBarBleed + 26f, ShellBarBleed - 6f, yb);
                    fill = Color.Lerp(fill, bevel, bv * 0.75f);
                    fill.a = 1f;
                    c = Color.Lerp(c, fill, cov);
                    c.a = Mathf.Max(c.a, cov);
                }
                px[j * w + i] = c;
            }
            return _tmpBar = Finish(tex, px);
        }

        private static Sprite CrystalIconTemp()
        {
            if (_tmpCrystal) return _tmpCrystal;
            const int n = 128;
            var tex = NewTex(n, n, "shell_crystal_tmp");
            var px = new Color32[n * n];
            Color light = new Color32(0x9C, 0xE4, 0xFF, 0xFF), mid = new Color32(0x36, 0x9C, 0xF0, 0xFF), deep = new Color32(0x1A, 0x5C, 0xC8, 0xFF), edge = new Color32(0x12, 0x3E, 0x8E, 0xFF);
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = (i + 0.5f) / n - 0.5f, y = (j + 0.5f) / n;
                Color c = new Color(0, 0, 0, 0);
                // Taç (0.62–0.84) ve alt piramit (0.12–0.62).
                bool crown = y >= 0.62f && y <= 0.84f && Mathf.Abs(x) <= 0.30f + (y - 0.62f) * 0.45f - (y - 0.62f) * 1.1f + 0.10f;
                bool pav = y >= 0.12f && y < 0.62f && Mathf.Abs(x) <= 0.40f * (y - 0.12f) / 0.50f;
                if (crown || pav)
                {
                    float shade = crown ? (Mathf.Abs(x) < 0.12f ? 0f : x < 0 ? 0.25f : 0.55f) : (x < -0.05f ? 0.35f : x < 0.12f ? 0.6f : 0.9f);
                    c = shade < 0.4f ? Color.Lerp(light, mid, shade / 0.4f) : Color.Lerp(mid, deep, (shade - 0.4f) / 0.6f);
                    if (Mathf.Abs(y - 0.62f) < 0.012f) c = edge;
                    if (crown && Mathf.Abs(x) < 0.05f && y > 0.7f) c = Color.Lerp(c, Color.white, 0.6f);
                }
                px[j * n + i] = c;
            }
            return _tmpCrystal = Finish(tex, px);
        }

        private static Sprite ChestIconTemp()
        {
            if (_tmpChest) return _tmpChest;
            const int n = 128;
            var tex = NewTex(n, n, "shell_chest_tmp");
            var px = new Color32[n * n];
            Color red = new Color32(0xD8, 0x3E, 0x34, 0xFF), redDark = new Color32(0xA0, 0x24, 0x22, 0xFF), gold = new Color32(0xF4, 0xC0, 0x46, 0xFF), goldDark = new Color32(0xB8, 0x7A, 0x1C, 0xFF);
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = (i + 0.5f) / n - 0.5f, y = (j + 0.5f) / n;
                Color c = new Color(0, 0, 0, 0);
                bool body = Mathf.Abs(x) <= 0.40f && y >= 0.14f && y <= 0.56f;
                float lidTop = 0.56f + 0.24f * Mathf.Sqrt(Mathf.Max(0f, 1f - (x / 0.40f) * (x / 0.40f)));
                bool lid = Mathf.Abs(x) <= 0.40f && y > 0.56f && y <= lidTop;
                if (body || lid)
                {
                    c = lid ? Color.Lerp(red, redDark, (y - 0.56f) / 0.3f * 0.5f) : Color.Lerp(redDark, red, (y - 0.14f) / 0.42f);
                    bool band = Mathf.Abs(Mathf.Abs(x) - 0.28f) < 0.045f || Mathf.Abs(x) > 0.36f || y < 0.19f || Mathf.Abs(y - 0.56f) < 0.03f || (lid && lidTop - y < 0.035f);
                    if (band) c = Mathf.Abs(y - 0.56f) < 0.03f ? goldDark : gold;
                    if (Mathf.Abs(x) < 0.07f && y > 0.42f && y < 0.62f) c = Mathf.Abs(x) < 0.025f && y < 0.52f ? goldDark : gold; // kilit
                }
                px[j * n + i] = c;
            }
            return _tmpChest = Finish(tex, px);
        }
    }
}
