using System;
using UnityEngine;
using UnityEngine.UI;
using KatKat.View;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// ui3 onaylı tasarım kabuğu (docs/ui3_onayli_tasarim/detaylar/sayfalar/01_harita_ana_sayfa.json, ortak-bilesenler
    /// nav): homeTop→homeBottom üst yüzey (y 0–312), logo 360×108, yıldız kapsülü 208×88, ayarlar 96, bölge plakası
    /// 984×120, alt menü 1080×208 (tabUnselected; seçili kapsül tabSelected/onTeal). Koordinatlar 1080×1920 sol üst
    /// köken; <see cref="U3Place"/> güvenli alanın üst ortasına göre yerleştirir. Mevcut eylemler aynen.
    /// </summary>
    public sealed partial class MenuView
    {
        private const float U3TopH = 312f;

        /// <summary>Sol üst köken (x, y, w, h) → üst-orta çapa.</summary>
        internal static void U3Place(RectTransform rt, float x, float y, float w, float h) =>
            UiFactory.Place(rt, new Vector2(0.5f, 1f), new Vector2(x + w * 0.5f - 540f, -(y + h * 0.5f)), new Vector2(w, h));

        private static Text U3Text(string name, Transform parent, string text, int size, bool heavy, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var t = UiFactory.Text(name, parent, text, size, color, anchor, heavy ? FontStyle.Bold : FontStyle.Normal);
            t.raycastTarget = false;
            return t;
        }

        private static Sprite _u3Stripes;
        /// <summary>Üst yüzeyde maketteki ince dikey çizgiler (60/120 periyot, açık bant hafif).</summary>
        private static Sprite U3Stripes()
        {
            if (_u3Stripes) return _u3Stripes;
            var tex = new Texture2D(120, 4, TextureFormat.RGBA32, false) { name = "ui3_stripes", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[120 * 4];
            for (int x = 0; x < 120; x++)
            {
                byte a = (byte)(x < 60 ? 14 : 0);
                for (int y = 0; y < 4; y++) px[y * 120 + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _u3Stripes = Sprite.Create(tex, new Rect(0, 0, 120, 4), new Vector2(0.5f, 0.5f), 100f);
            _u3Stripes.hideFlags = HideFlags.DontSave;
            return _u3Stripes;
        }

        /// <summary>Panel üst ortasındaki küçük pirinç baklava (makette plaka/menü süsü).</summary>
        internal static Image U3Diamond(Transform parent, Vector2 anchor, Vector2 pos, float s = 18f)
        {
            var d = UiFactory.Image("Diamond", parent, Ui3.Rounded(3f, Ui3.Brass, Ui3.Brass), Color.white, true);
            d.raycastTarget = false;
            UiFactory.Place(d.rectTransform, anchor, pos, new Vector2(s, s));
            d.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return d;
        }

        /// <summary>Fildişi seramik yüzey (ivoryTop→ivoryBottom, 3 px pirinç, 4 px açık iç dudak) + alt gölge.</summary>
        internal static Image U3Plate(string name, Transform parent, float radius, float height)
        {
            var p = Ui3.Surface(name, parent, radius, Ui3.IvoryTop, Ui3.IvoryBottom, height, 3f, Ui3.Brass, 3f, Ui3.RimHighlight);
            var sh = p.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(Ui3.Shadow.r, Ui3.Shadow.g, Ui3.Shadow.b, 0.20f);
            sh.effectDistance = new Vector2(0f, -8f);
            return p;
        }

        /// <summary>Yıldız kapsülü 208×88: fildişi, pirinç kenar; yıldız 64 solda, sayı metric 50/800 ink.</summary>
        private static void U3StarCapsule(RectTransform parent, float x, float y, int stars)
        {
            var cap = U3Plate("StarCapsule", parent, 44f, 88f);
            U3Place(cap.rectTransform, x, y, 208f, 88f);
            var star = Decor("Star", cap.transform, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            star.preserveAspect = true;
            UiFactory.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(54f, 2f), new Vector2(64f, 64f));
            var n = U3Text("Stars", cap.transform, stars.ToString(), 50, true, Ui3.Ink);
            UiFactory.Place(n.rectTransform, new Vector2(0f, 0.5f), new Vector2(142f, 0f), new Vector2(112f, 62f));
            FitText(n, 30);
        }

        private void BuildMapShellUi3(RectTransform page, int stars, Action settings, out RectTransform headBack, out RectTransform head)
        {
            _shellStars = stars; _shellSettings = settings;
            var shell = UiFactory.Rect("TopShell", page);
            shell.anchorMin = new Vector2(0.5f, 1f); shell.anchorMax = new Vector2(0.5f, 1f); shell.pivot = new Vector2(0.5f, 1f);
            shell.sizeDelta = new Vector2(1080f, U3TopH);
            shell.anchoredPosition = Vector2.zero;

            // 1) Üst yüzey: homeTop→homeBottom (y 0–312); güvenli alanın üstüne (çentik) taşar. Harita üstüne çizgi yok.
            var panel = UiFactory.Image("Panel", shell, Ui3.HomeGradient(), Color.white);
            panel.raycastTarget = true; // opak: arkasındaki harita düğümlerine dokunulmaz
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.sizeDelta = new Vector2(1600f, U3TopH + ShellTopBleed);
            prt.anchoredPosition = new Vector2(0f, -U3TopH);
            var stripes = UiFactory.Image("Stripes", panel.transform, U3Stripes(), Color.white);
            stripes.type = Image.Type.Tiled;
            stripes.raycastTarget = false;
            UiFactory.Stretch(stripes.rectTransform);
            var shadow = Decor("PanelShadow", shell, VShadow());
            shadow.color = new Color(Ui3.Shadow.r, Ui3.Shadow.g, Ui3.Shadow.b, 0.24f);
            var srt = shadow.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(1600f, 26f);
            srt.anchoredPosition = new Vector2(0f, -U3TopH);

            // 2) Logo (150, 24, 360, 140): Stone Burst iki satırlı logo (2026-11-09); fenerin üstüne binmesin diye sağa alındı.
            var logoSprite = PremiumArt.V4("ui/logo-stoneburst");
            if (logoSprite != null)
            {
                var logo = Decor("Logo", shell, logoSprite);
                logo.preserveAspect = true;
                U3Place(logo.rectTransform, 64f, 24f, 360f, 140f); // 2026-11-09: kalp göstergesine yer için sola (kullanıcı)
            }

            // 3) Bölge plakası (48, 176, 984, 120): fildişi, pirinç kenar, üstte baklava; içerik HeaderCardV11.
            var plate = U3Plate("RegionShell", shell, 34f, 120f);
            U3Place(plate.rectTransform, 48f, 176f, 984f, 120f);
            U3Diamond(shell, new Vector2(0.5f, 1f), new Vector2(0f, -176f));
            headBack = UiFactory.Rect("RegionBack", shell);
            UiFactory.Stretch(headBack);
            head = UiFactory.Rect("RegionFront", shell);
            UiFactory.Stretch(head);

            // 4) Sütunlar: ZPE, Sıralama, kilitli taş (mevcut eylemler).
            BuildZpeBadge(shell);
            BuildRankButton(shell);
            BuildStarGoal(shell);

            // 5) Yıldız kapsülü (700, 56, 208, 88) ve ayarlar (936, 52, 96, 96).
            U3StarCapsule(shell, 700f, 56f, stars);
            BuildHeartsWidget(shell); // 2026-11-09: logo ile yıldız arasında kalp
            if (settings != null) U3Gear(page, settings);
        }

        private static void U3Gear(RectTransform page, Action settings)
        {
            var b = ShellRoundButton(Loc.T("ui.settings"), page, PremiumArt.V4("icons/settings") ?? UiArt.Icon(UiIcon.SoundOn), 96f, settings);
            U3Place((RectTransform)b.transform, 936f, 52f, 96f, 96f);
        }

        /// <summary>Bölge plakası içeriği (merkez y −236): fener, ad (ink), ayırıcı, petrol kapsülde aralık (onTeal).</summary>
        private void HeaderCardUi3(RectTransform front, ThemeDefinition theme, int region, int first, int last)
        {
            const float cy = -236f;
            var icon = SpecialArt.V6("ui/region-" + theme.Id);
            if (icon != null)
            {
                var img = Decor("RegionIcon", front, icon);
                img.preserveAspect = true;
                UiFactory.Place(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(-418f, cy + 8f), new Vector2(160f, 160f));
            }
            var title = U3Text("RegionName", front, ThemeDefinition.RegionTitle(region), 56, true, Ui3.Ink);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(-100f, cy), new Vector2(400f, 72f));
            FitText(title, 32);
            var sep = UiFactory.Image("Separator", front, UiArt.Box, new Color(Ui3.Divider.r, Ui3.Divider.g, Ui3.Divider.b, 1f));
            sep.raycastTarget = false;
            UiFactory.Place(sep.rectTransform, new Vector2(0.5f, 1f), new Vector2(128f, cy), new Vector2(2f, 64f));
            var chip = Ui3.Surface("Chip", front, 36f, Ui3.TealTop, Ui3.TealBottom, 72f, 3f, Ui3.Brass);
            UiFactory.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(326f, cy), new Vector2(316f, 72f));
            var ct = U3Text("Text", chip.transform, Loc.F("ui.region", region + 1) + " • " + first + "–" + last, 32, true, Ui3.OnTeal);
            UiFactory.Stretch(ct.rectTransform, 22, 22, 4, 6);
            FitText(ct, 20);
        }

        /// <summary>Alt menü 1080×208 (ortak-bilesenler nav): tabUnselected yüzey, pirinç üst kenar, üç öğe 296×176.</summary>
        internal static void BuildTabBarUi3(RectTransform holder, float baseY, TabItem[] items)
        {
            const float H = 208f;
            var bar = Ui3.Surface("Bar", holder, 40f, Ui3.TabUnselected, Ui3.TabUnselected, 0f, 3f, Ui3.Brass, 3f, Ui3.RimHighlight);
            bar.raycastTarget = true; // opak: arkasındaki haritaya dokunulmaz
            var brt = bar.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(1092f, H + 400f); // alt kenar cihazın gerçek alt kenarının da altına
            brt.anchoredPosition = new Vector2(0f, baseY + H);
            var sh = bar.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(Ui3.Shadow.r, Ui3.Shadow.g, Ui3.Shadow.b, 0.18f);
            sh.effectDistance = new Vector2(0f, 6f);
            U3Diamond(holder, new Vector2(0.5f, 0f), new Vector2(0f, baseY + H));

            int n = items?.Length ?? 0;
            if (n == 0) return;
            // Üç öğe: x 48, 392, 736 (genişlik 296); diğer sayılarda 984 içinde eşit.
            float slot = n == 3 ? 344f : 984f / n;
            for (int i = 0; i < n; i++)
            {
                float cx = n == 3 ? (i - 1) * 344f : -492f + (i + 0.5f) * slot;
                TabUi3(holder, cx, Mathf.Min(296f, slot), baseY, items[i]);
            }
        }

        private static void TabUi3(RectTransform bar, float x, float w, float baseY, TabItem it)
        {
            // Öğe 296×176, menünün üstünden 16 aşağıda: merkez y = baseY + 208 − 16 − 88.
            var hit = UiFactory.SpriteButton(it.Label, bar, null, new Vector2(w, 176f), it.Act);
            if (it.Act != null) hit.onClick.AddListener(() => Vr1Proto.Play("ui-stone-tap")); // vr1 P1 prototip sesi (anahtar kapalıyken sessiz)
            hit.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Place((RectTransform)hit.transform, new Vector2(0.5f, 0f), new Vector2(x, baseY + 104f), new Vector2(w, 176f));
            if (it.On)
            {
                // Seçili öğenin arkasında fildişi kart (makette); diğerlerinin arkası saydam.
                var card = Ui3.Surface("Card", hit.transform, 30f, Ui3.IvoryTop, Ui3.IvoryBottom, 168f, 2f, Ui3.Brass);
                UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(Mathf.Min(256f, w - 8f), 168f));
            }
            // Simge (104, 8, 88, 88) → merkez öğe üstünden 52 aşağı.
            var ic = Decor("Icon", hit.transform, it.Icon);
            ic.preserveAspect = true;
            if (Vr1Proto.On)
            {
                // vr1 prototipi (kullanıcı, 2026-11-11: "iconlar büyük olsun ve alanı da taşsınlar"): ikon menünün üst
                // kenarından ~40 birim taşar; yazı yerinde kalır. Aday PNG'lerde saydam pay olduğu için kutuları daha büyük.
                bool cand = it.Icon != null && (it.Icon == Vr1Proto.NavMap || it.Icon == Vr1Proto.NavDaily);
                UiFactory.Place(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), cand ? new Vector2(168f, 150f) : new Vector2(132f, 132f));
                ic.transform.SetAsLastSibling();
            }
            else UiFactory.Place(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(88f, 88f));
            if (it.On) ic.gameObject.AddComponent<IconBreath>(); // seçili ikon büyüyüp küçülür (2026-11-11)
            // Yazı (16, 108, 264, 48) nav 32/800; seçili kapsül (28, 100, 240, 64) tabSelected, yazı onTeal.
            if (it.On)
            {
                var pill = Ui3.Surface("Active", hit.transform, 32f, Ui3.TabSelected, Ui3.TabSelected);
                UiFactory.Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(Mathf.Min(240f, w - 40f), 64f));
            }
            var t = U3Text("Label", hit.transform, it.Label, 32, true, it.On ? Ui3.OnTeal : Ui3.Ink);
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(Mathf.Min(264f, w - 32f) - (it.On ? 24f : 0f), 48f));
            FitText(t, 22);
        }

        // ---- ui3 modallar (modal-varyantlari.json: start, language) ------------------------------------------

        /// <summary>
        /// ui3 modal: tam ekran karartma (#111C24 / 0.72; dışa dokunma <paramref name="outside"/> — mevcut davranış),
        /// seramik kart (genişlik <paramref name="w"/>, yükseklik <paramref name="h"/>, üst omuz 32), dikeyde ortalı.
        /// Dönen dikdörtgen kartın kendisi; içerik üst-orta çapayla <see cref="U3In"/> ile yerleşir.
        /// </summary>
        internal static RectTransform U3Modal(RectTransform parent, string name, float w, float h, Action outside, out RectTransform root)
        {
            root = UiFactory.Stretch(UiFactory.Rect(name, parent));
            var dim = Ui1.Dimmer(root, outside);
            if (outside == null) dim.interactable = true; // dokunuşu yut (altta kalan sayfaya geçmez)
            var card = UiFactory.Rect("Card", root);
            var blocker = card.gameObject.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0f);
            var bg = UiFactory.Image("Bg", card, Ui1.Art("panel-modal"), Color.white, true);
            bg.raycastTarget = false;
            bg.pixelsPerUnitMultiplier = bg.sprite.rect.width / Mathf.Max(600f, w);
            UiFactory.Stretch(bg.rectTransform, 0f, 0f, -32f, 0f);
            ResultPanel.Ui3Layers(card);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(w, h);
            card.anchoredPosition = new Vector2(0f, -16f);
            return card;
        }

        /// <summary>Kart yerel (x, y, w, h; sol üst köken) → kartın üst-orta çapası.</summary>
        internal static void U3In(RectTransform rt, float cardW, float x, float y, float w, float h) =>
            UiFactory.Place(rt, new Vector2(0.5f, 1f), new Vector2(x + w * 0.5f - cardW * 0.5f, -(y + h * 0.5f)), new Vector2(w, h));

        /// <summary>ui3/21 başlangıç kartı (860×600): başlık, yıldızlar (176,164,508,104), mekanik adı, OYNA (56,428,748,116).</summary>
        private void ShowStartCardUi3(int index)
        {
            Action close = () => { if (_startCard != null) UiFactory.Kill(_startCard.gameObject); _startCard = null; };
            var card = U3Modal(_page, "StartCard", 860f, 600f, close, out var root);
            _startCard = root;
            var title = U3Text("Title", card, Loc.F("ui.levelbig", index + 1), 54, true, Ui3.Ink);
            U3In(title.rectTransform, 860f, 56f, 56f, 748f, 80f);
            int stars = StarsOf != null ? StarsOf(index) : 0;
            var starSprite = PremiumArt.V4("icons/star") ?? PremiumArt.Get("icon_star") ?? ProceduralArt.StarSprite;
            for (int k = 0; k < 3; k++)
            {
                var st = UiFactory.Image("Star", card, starSprite, k < stars ? Color.white : Ui3.UnearnedStar);
                st.preserveAspect = true;
                st.raycastTarget = false;
                float s = k == 1 ? 104f : 88f;
                UiFactory.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2((k - 1) * 112f, -(164f + 52f) + (k == 1 ? 6f : 0f)), new Vector2(s, s));
            }
            var mech = MechanicIcon(index);
            string mechName = MechanicName(index);
            var mt = U3Text("MechName", card, mechName, 36, false, Ui3.Ink);
            U3In(mt.rectTransform, 860f, 56f, 300f, 748f, 88f);
            if (mech.sprite != null)
            {
                float tw = Mathf.Min(640f, mt.GetPreferredValues(mechName, 640f, 88f).x);
                var ic = UiFactory.Image("Mech", card, mech.sprite, mech.tint);
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                UiFactory.Place(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(-tw * 0.5f - 52f, -344f), new Vector2(72f, 72f));
                mt.rectTransform.anchoredPosition += new Vector2(40f, 0f);
            }
            int i = index;
            Ui1.Button("Play", card, Loc.T("ui.playbig"), true, new Vector2(0.5f, 1f), new Vector2(0f, -(428f + 58f)), new Vector2(748f, 116f), () => { close(); LevelChosen?.Invoke(i); });
        }

        /// <summary>ui3/22 dil seçimi (860 genişlik): 3×3 dil düğmesi 236×116 (geçerli dil petrol), Kapat 748×116.</summary>
        private void ShowLanguagePickerUi3()
        {
            RectTransform root = null;
            Action close = () => { if (root != null) UiFactory.Kill(root.gameObject); };
            var card = U3Modal(_page, "LanguagePicker", 860f, 996f, close, out root);
            var title = U3Text("Title", card, Loc.T("ui.language"), 54, true, Ui3.Ink);
            U3In(title.rectTransform, 860f, 56f, 56f, 748f, 80f);
            var langs = Loc.Languages;
            for (int i = 0; i < langs.Length; i++)
            {
                var lang = langs[i];
                bool cur = lang.Code == Loc.Code;
                int r = i / 3, c = i % 3;
                var b = Ui1.Button(lang.Code, card, ArabicShaper.ShapeLines(lang.NativeName), cur, new Vector2(0.5f, 1f),
                    new Vector2(56f + c * 256f + 118f - 430f, -(192f + r * 164f + 58f)), new Vector2(236f, 116f), () =>
                    {
                        close();
                        if (lang.Code != Loc.Code) LanguagePicked?.Invoke(lang.Code);
                    }, null, 34);
            }
            Ui1.Button("Close", card, Loc.T("ui.close"), false, new Vector2(0.5f, 1f), new Vector2(0f, -(824f + 58f)), new Vector2(748f, 116f), close);
        }
        private static Sprite _u3Sage;
        /// <summary>
        /// Menü sayfa zemini (makette koleksiyon, deneme listesi, taş vitrini): homeTop→homeBottom dikey geçiş, 60/120
        /// periyotlu hafif açık dikey şerit. Koddan 540×960 (1080×1920 tuvalin yarısı), zarflı ölçekle tam ekran.
        /// </summary>
        internal static Sprite U3SageBg()
        {
            if (_u3Sage) return _u3Sage;
            const int w = 540, h = 960;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "ui3_sage", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[w * h];
            Color top = Color.Lerp(Ui3.HomeTop, Color.white, 0.06f), bottom = Ui3.HomeBottom;
            for (int y = 0; y < h; y++)
            {
                Color row = Color.Lerp(bottom, top, y / (h - 1f));
                for (int x = 0; x < w; x++)
                {
                    Color c = row;
                    if ((x / 30) % 2 == 0) c = Color.Lerp(c, Color.white, 0.055f);
                    // Kenarlarda hafif koyulaşma (ortası açık).
                    float dx = (x - w * 0.5f) / (w * 0.5f);
                    c = Color.Lerp(c, bottom * 0.85f, Mathf.Clamp01(dx * dx - 0.55f) * 0.5f);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _u3Sage = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            _u3Sage.hideFlags = HideFlags.DontSave;
            return _u3Sage;
        }
        // ---- ui3/05 Ayarlar ----------------------------------------------------------------------------------

        /// <summary>
        /// ui3/05: geri (48,48,112), başlık plakası (pageTitle 56), fildişi kart panel 860 genişlik (y 444), üç satır
        /// 748×132 (simge 72, rowTitle 38/800, anahtar 176×88: açık teal / kapalı track, 2 px pirinç, başparmak 72
        /// ivoryTop). Dil satırında mevcut dil düğmesi. Zemin mevcut sahil resmi, makettaki koyu tonda.
        /// </summary>
        private void ShowSettingsUi3(bool sound, bool vibration, Action back, Action onLevels)
        {
            // Kullanıcı (2026-11-08): "ayarlar sayfasının arkaplanını Sıralama sayfasının arkaplanından yap" (Z1, açık zemin).
            var page = NewPage(Ui3.GameBackground(0) ?? RankArt("bg"));
            Ui1.Round("Back", page, "icon-back", new Vector2(0f, 1f), new Vector2(104f, -104f), 112f, onLevels ?? back);
            Ui1.Header(page, Loc.T("ui.settings"), new Vector2(0.5f, 1f), new Vector2(0f, -234f), new Vector2(620f, 128f), 56);
            // 2026-11-08: 4. satır "İlerlemeyi kaydet"; 2026-11-09: 5. satır "Gizlilik seçenekleri" (yalnız UMP gerekli
            // gördüğünde, ör. AB). Satır aralığı 164; panel her satırda 164 büyür.
            bool privacy = PrivacyOptionsVisible != null && PrivacyOptionsVisible();
            const float PanelTop = 444f, RowStep = 164f;
            // 2026-11-11: 3. satır "Azaltılmış hareket" (vr1 erişilebilirlik); dil, kayıt, gizlilik birer aşağı.
            int rows = privacy ? 6 : 5;
            float PanelH = 724f + (rows - 4) * RowStep;
            float RowY(int i) => (rows - 1) * RowStep * 0.5f - i * RowStep;
            var panel = Ui1.Card("Card", page, new Vector2(0.5f, 1f), new Vector2(0f, -(PanelTop + PanelH * 0.5f)), new Vector2(860f, PanelH));
            panel.pixelsPerUnitMultiplier = 1f;
            var c = panel.rectTransform;
            SwitchRowUi3(c, RowY(0), "icon-sound", Loc.T("ui.sound"), sound, v => SoundChanged?.Invoke(v));
            U3Divider(c, RowY(0) - RowStep * 0.5f);
            SwitchRowUi3(c, RowY(1), "icon-vibration", Loc.T("ui.vibration"), vibration, v => VibrationChanged?.Invoke(v));
            U3Divider(c, RowY(1) - RowStep * 0.5f);
            SwitchRowUi3(c, RowY(2), "icon-sound", Loc.T("ui.motion"), Motion.Reduced, v => MotionChanged?.Invoke(v), UiArt.Icon(UiIcon.Ring));
            U3Divider(c, RowY(2) - RowStep * 0.5f);
            var lrow = UiFactory.Rect("Language", c);
            UiFactory.Place(lrow, new Vector2(0.5f, 0.5f), new Vector2(0f, RowY(3)), new Vector2(748f, 132f));
            var lic = Ui1.Decor("Icon", lrow.transform, "icon-language");
            UiFactory.Place(lic.rectTransform, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(72f, 72f));
            var lt = U3Text("Label", lrow.transform, Loc.T("ui.language"), 38, true, Ui3.Ink, TextAnchor.MiddleLeft);
            // Etiket düğmenin (sağda, 280) altına girmesin: [104, 454] aralığı (Arapçada sağa hizalanınca çakışıyordu).
            UiFactory.Place(lt.rectTransform, new Vector2(0f, 0.5f), new Vector2(104f + 175f, 0f), new Vector2(350f, 72f));
            FitText(lt, 28);
            Ui1.Button("Pick", lrow.transform, ArabicShaper.ShapeLines(Loc.Current.NativeName) + "  ›", true, new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(280f, 88f), ShowLanguagePicker, null, 38);
            U3Divider(c, RowY(3) - RowStep * 0.5f);
            SaveRowUi3(c, RowY(4));
            if (privacy)
            {
                U3Divider(c, RowY(4) - RowStep * 0.5f);
                PrivacyRowUi3(c, RowY(5));
            }
            var note = U3Text("Note", page, Loc.T("ui.settingsnote"), 28, false, Ui3.InkSecondary);
            note.textWrappingMode = TMPro.TextWrappingModes.Normal;
            UiFactory.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -(PanelTop + PanelH + 70f)), new Vector2(860f, 90f));
            var ver = U3Text("Version", page, "Stone Burst " + Application.version + "  ·  OFIGAME", 26, false, Ui3.InkMuted);
            UiFactory.Place(ver.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, BarBottom + BarH + 50f), new Vector2(800f, 40f));
#if KATKAT_VR1PROTO
            // vr1 test derlemesi (cihazda karşılaştırma): sürüm yazısına dokununca prototip açılır/kapanır. Mağazada yok.
            ver.text = "Stone Burst " + Application.version + "  ·  vr1 prototipi " + (Vr1Proto.On ? "AÇIK" : "KAPALI") + " (dokun)";
            ver.raycastTarget = true;
            ver.gameObject.AddComponent<Button>().onClick.AddListener(() => { Vr1Proto.SetPref(!Vr1Proto.On); ShowSettingsUi3(sound, vibration, back, onLevels); });
            UiFactory.Place(ver.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, BarBottom + BarH + 50f), new Vector2(980f, 70f));
#endif
            Tabs(page, 2, back, onLevels ?? back, () => { });
        }

        private static void U3Divider(RectTransform card, float y)
        {
            var d = UiFactory.Image("Divider", card, UiArt.Box, new Color(Ui3.Divider.r, Ui3.Divider.g, Ui3.Divider.b, 0.9f));
            d.raycastTarget = false;
            UiFactory.Place(d.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(748f, 2f));
        }

        private void SwitchRowUi3(RectTransform card, float y, string iconKey, string label, bool initial, Action<bool> changed, Sprite iconOverride = null)
        {
            bool state = initial;
            var row = UiFactory.SpriteButton(label, card, null, new Vector2(748f, 132f), null);
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Place((RectTransform)row.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(748f, 132f));
            var icon = Ui1.Decor("Icon", row.transform, iconKey);
            if (iconOverride != null) { icon.sprite = iconOverride; icon.color = Ui3.Ink; }
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(72f, 72f));
            var t = U3Text("Label", row.transform, label, 38, true, Ui3.Ink, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(104f + 198f, 0f), new Vector2(396f, 72f));
            FitText(t, 28);
            // Anahtar (572,22,176,88): açık teal / kapalı track, 2 px pirinç kontur; başparmak 72 ivoryTop.
            var track = UiFactory.Image("Track", row.transform, Ui3.Rounded(44f, Color.white, Color.white, 2f, Ui3.Brass), Color.white, true);
            track.pixelsPerUnitMultiplier = 1f;
            track.raycastTarget = false;
            UiFactory.Place(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(572f + 88f, 0f), new Vector2(176f, 88f));
            var fill = UiFactory.Image("Fill", track.transform, Ui3.Rounded(42f, Color.white, Color.white), Color.white, true);
            fill.pixelsPerUnitMultiplier = 1f;
            fill.raycastTarget = false;
            UiFactory.Stretch(fill.rectTransform, 2f, 2f, 2f, 2f);
            var knob = UiFactory.Image("Knob", track.transform, Ui3.Rounded(36f, Ui3.IvoryTop, Ui3.IvoryTop, 2f, Ui3.Sand), Color.white, true);
            knob.pixelsPerUnitMultiplier = 1f;
            knob.raycastTarget = false;
            var ks = knob.gameObject.AddComponent<Shadow>();
            ks.effectColor = new Color(Ui3.Shadow.r, Ui3.Shadow.g, Ui3.Shadow.b, 0.25f); ks.effectDistance = new Vector2(0f, -3f);
            var stateText = U3Text("State", track.transform, "", 22, true, Ui3.OnTeal);
            stateText.enabled = false; // durum yazısı erişilebilirlik için nesnede (tasarımda yazısız)
            void Refresh()
            {
                fill.color = state ? Ui3.Teal : Ui3.Track;
                UiFactory.Place(knob.rectTransform, new Vector2(0f, 0.5f), new Vector2(state ? 132f : 44f, 0f), new Vector2(72f, 72f));
                Loc.Set(stateText, state ? Loc.T("ui.on") : Loc.T("ui.off"));
            }
            row.onClick.AddListener(() => { state = !state; Refresh(); changed(state); });
            PressFeedback.Attach(row, null, 70f, 110f);
            Refresh();
        }    }
}
