using System;
using KatKat.Meta;
using KatKat.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// v12 Sıralama ekranı (onaylı referans docs/v12/siralama/00-onayli-referans.png, 940 × 1672 → 1080 birim ×1,149;
    /// parçalar Resources/PremiumV6/rank). Üstte geri · logo · "Sıralama" kapsülü · bilgi; Dünya / ülke sekmeleri;
    /// podyum (2 · 1 · 3) ve ilk 10 listesi aynı veriden; altta liste kayarken yerinde duran "Senin sıran" kartı.
    /// Bütün metin ve sayılar dinamik TMP. Sunucu yoksa dürüst durum metni; örnek veri yalnız geliştirmede ve etiketli.
    /// </summary>
    public sealed partial class MenuView
    {
        // ---- Model -------------------------------------------------------------------------------------------

        public struct RankScreenModel
        {
            public LeaderboardScope Scope;
            public string CountryCode, CountryName;  // null: ülke bilinmiyor (uydurulmaz)
            public bool Loading;
            public LeaderboardResult Result;
            public LeaderboardEntry Self;            // yerel değerler (puan, ZPE, bölüm, yıldız); Rank sunucudan
            public int Completed, MinLevels;
            public Sprite SelfAvatar;
        }

        public Action<LeaderboardScope> RankTabPressed;
        public Action RankRetryPressed, RankPressed;

        private RectTransform _rankList;
        private RankFx _rankFx;
        public bool IsRankShown => _rankFx != null;

        // ---- Ölçüler ve renkler (1080 birim) ------------------------------------------------------------------

        private static readonly Color RankInk = new Color32(0x07, 0x3A, 0x68, 0xFF);   // belge: ana renk
        private static readonly Color RankInk2 = new Color32(0x50, 0x67, 0x7A, 0xFF);  // belge: ikincil
        private static readonly Color RankFill = new Color32(0xF7, 0xF8, 0xF8, 0xFF);  // belge: liste iç dolgusu
        private static readonly Color RankDivider = new Color32(0x8F, 0xB8, 0xD0, 0xFF);
        private const float RankListTop = 586f, RankListBottom = 362f, RankRowH = 76f, RankRowGap = 4f;
        private const float RankCardH = 253f, RankCardW = 1046f, RankCardBottom = 14f;

        /// <summary>
        /// Sıralama görselleri. ui1 (docs/ui1/07–08) yüklüyse ortak parçalar: geri/bilgi simgesi, turkuaz/krem sekme
        /// (ana/ikincil düğme), krem panel (liste, kendi kartı, bilgi), krem satır. Podyum, kupa, madalya, halka, maskot,
        /// rozet ve arka plan sıralama paketinden kalır.
        /// </summary>
        private static Sprite RankArt(string key)
        {
            if (Ui1.Ready)
            {
                string u = key switch
                {
                    "btn-back" => "icon-back", "btn-info" => "icon-info", "tab-on" => "button-primary", "tab-off" => "button-secondary",
                    "list-panel" => "panel-modal", "self-panel" => "panel-modal", "row-blue" => "row-ivory", "row-white" => "row-ivory",
                    "title-capsule" => "chip-objective", _ => null,
                };
                if (u != null && Ui1.Art(u) is Sprite s) return s;
            }
            return SpecialArt.V6("rank/" + key);
        }

        /// <summary>9 dilim köşesini gösterimde <paramref name="units"/> birime oturtan çarpan (kaynak sınırına göre).</summary>
        private static float CornerMul(Sprite s, float units) => s != null && s.border.x > 0f ? s.border.x / units : 1f;

        // ---- Ekran -------------------------------------------------------------------------------------------

        /// <summary>Sıralama ekranını kurar ya da (aynı ekrandayken) yalnız içeriği yeniler; giriş animasyonu bir kez.</summary>
        public void ShowRanking(RankScreenModel m, Action back, Action info)
        {
            bool first = !IsRankShown;
            var page = NewPage(Ui3.Ready ? Ui3.GameBackground(0) ?? RankArt("bg") : RankArt("bg")); // ui3/07: şeftali çizgili zemin (Z1)
            _rankFx = page.gameObject.AddComponent<RankFx>();
            _rankFx.FadeIn = first;
            var cg = page.gameObject.AddComponent<CanvasGroup>();
            _rankFx.Group = cg;

            RankHeader(page, back, info);
            RankTabs(page, m);
            var middle = UiFactory.Rect("Middle", page);
            UiFactory.Stretch(middle);
            _rankFx.Content = middle.gameObject.AddComponent<CanvasGroup>();
            RankPodium(middle, m);
            RankListPanel(middle, m);
            RankSelfCard(page, m);
            RankSignInButton(page);
        }

        public void CloseRanking() { _rankFx = null; }

        private void RankHeader(RectTransform page, Action back, Action info)
        {
            var b = RankButton("Back", page, RankArt("btn-back"), new Vector2(-462f, -66f), 100f, back);
            var logoSprite = PremiumArt.V4("ui/logo-stoneburst");
            if (logoSprite != null)
            {
                var logo = Decor("Logo", page, logoSprite);
                logo.preserveAspect = true;
                UiFactory.Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(-238f, -66f), new Vector2(270f, 112f)); // iki satırlı logo kutuya sığar
            }
            var div = UiFactory.Image("Divider", page, UiArt.Box, new Color(1f, 1f, 1f, 0.8f));
            div.raycastTarget = false;
            UiFactory.Place(div.rectTransform, new Vector2(0.5f, 1f), new Vector2(-82f, -66f), new Vector2(4f, 70f));
            var cap = RankSliced("Title", page, RankArt("title-capsule"), new Vector2(0.5f, 1f), new Vector2(172f, -66f), new Vector2(418f, 96f));
            var trophy = Decor("Trophy", cap.transform, RankArt("trophy"));
            trophy.preserveAspect = true;
            UiFactory.Place(trophy.rectTransform, new Vector2(0f, 0.5f), new Vector2(70f, 2f), new Vector2(88f, 76f));
            var t = RankText("TitleText", cap.transform, Loc.T("rank.title"), 56);
            t.raycastTarget = false;
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(262f, 2f), new Vector2(270f, 70f));
            FitText(t, 34);
            RankButton("Info", page, RankArt("btn-info"), new Vector2(460f, -66f), 100f, info);
        }

        private Button RankButton(string name, RectTransform parent, Sprite sprite, Vector2 pos, float size, Action act)
        {
            var b = UiFactory.SpriteButton(name, parent, sprite, new Vector2(size, size), act);
            b.GetComponent<Image>().preserveAspect = true;
            UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), pos, new Vector2(size, size));
            PressFeedback.Attach(b, null, 80f, 120f);
            return b;
        }

        /// <summary>9 dilimli görsel: köşe/uç ölçüsü gösterim yüksekliğine oturur (kaynak yeniden örneklenmez).</summary>
        private static Image RankSliced(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var img = UiFactory.Image(name, parent, sprite, Color.white, true);
            img.raycastTarget = false;
            UiFactory.Place(img.rectTransform, anchor, pos, size);
            // Hap/satır/sekme: uç sınırı kaynak yüksekliğinin yarısı → çarpan = kaynak yükseklik / gösterim yüksekliği
            // (uçlar tam yarım daire kalır, yalnız orta yatay esner).
            if (sprite != null) img.pixelsPerUnitMultiplier = sprite.rect.height / Mathf.Max(1f, size.y);
            return img;
        }

        private void RankTabs(RectTransform page, RankScreenModel m)
        {
            const float y = -192f, w = 482f, h = 112f;
            RankTab(page, new Vector2(-246f, y), new Vector2(w, h), Loc.T("rank.world"), null, m.Scope == LeaderboardScope.World,
                () => RankTabPressed?.Invoke(LeaderboardScope.World));
            string cname = string.IsNullOrEmpty(m.CountryName) ? Loc.T("rank.country") : m.CountryName;
            RankTab(page, new Vector2(246f, y), new Vector2(w, h), cname, m.CountryCode, m.Scope == LeaderboardScope.Country,
                () => RankTabPressed?.Invoke(LeaderboardScope.Country));
        }

        private static void RankTab(RectTransform page, Vector2 pos, Vector2 size, string label, string code, bool on, Action act)
        {
            var b = UiFactory.SpriteButton(label, page, null, size, on ? null : act);
            b.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            var rt = (RectTransform)b.transform;
            UiFactory.Place(rt, new Vector2(0.5f, 1f), pos, size);
            PressFeedback.Attach(b, null, 80f, 120f);
            var bg = RankSliced("Bg", rt, RankArt(on ? "tab-on" : "tab-off"), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            float textX = 0f;
            if (!string.IsNullOrEmpty(code))
            {
                // Ülke bayrağı varlığı yok (paket notu: mevcut bayraklar kullanılmalı; projede bayrak yok) → ISO kodu.
                var chip = UiFactory.Image("Code", rt, UiArt.Box, Color.white, true);
                chip.raycastTarget = false;
                UiFactory.Place(chip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-150f, 2f), new Vector2(84f, 56f));
                var ol = chip.gameObject.AddComponent<Outline>();
                ol.effectColor = new Color32(0xD0, 0x2A, 0x2A, 0xFF);
                ol.effectDistance = new Vector2(3f, -3f);
                var ct = UiFactory.Text("CodeText", chip.transform, code, 32, new Color32(0xD0, 0x2A, 0x2A, 0xFF));
                ct.raycastTarget = false;
                UiFactory.Stretch(ct.rectTransform);
                textX = 46f;
            }
            var t = RankText("Label", rt, label, 44);
            t.raycastTarget = false;
            if (Ui3.Ready) t.color = on ? Ui3.OnTeal : Ui3.Ink; // ui3/07: seçili sekme petrol, yazı onTeal
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(textX, 2f), new Vector2(size.x - 80f - textX * 2f, 64f));
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            FitText(t, 32);
        }

        // ---- Podyum ------------------------------------------------------------------------------------------

        private void RankPodium(RectTransform parent, RankScreenModel m)
        {
            var r = m.Result;
            if (m.Loading || r == null || r.Status != LeaderboardStatus.Ready || r.Top.Count == 0) return;
            // Sıra: solda 2, ortada 1, sağda 3 (aynı ilk 10 verisinden).
            // ui1/07: baş harf halkaları küçük (altın halka sekmelerin altında kalır; ilk 10 listesi yerinde).
            bool u1 = Ui1.Ready;
            PodiumSpot(parent, r, 1, "silver", new Vector2(-308f, 0f), 300f, u1 ? 140f : 172f, u1 ? -22f : -16f);
            PodiumSpot(parent, r, 2, "bronze", new Vector2(308f, 0f), 312f, u1 ? 140f : 172f, u1 ? -22f : -16f);
            PodiumSpot(parent, r, 0, "gold", Vector2.zero, 326f, u1 ? 160f : 198f, u1 ? -12f : 8f);
        }

        private void PodiumSpot(RectTransform parent, LeaderboardResult r, int index, string metal, Vector2 x, float width, float ring, float lift)
        {
            if (index >= r.Top.Count) return;
            var e = r.Top[index];
            var holder = UiFactory.Rect("Podium" + (index + 1), parent);
            UiFactory.Place(holder, new Vector2(0.5f, 1f), new Vector2(x.x, 0f), new Vector2(width, 10f));
            var ped = RankArt("podium-" + metal);
            float ph = width * ped.rect.height / ped.rect.width;
            float baseY = -575f + ph * 0.5f; // tabanlar aynı çizgide
            var p = Decor("Base", holder, ped);
            p.preserveAspect = true;
            UiFactory.Place(p.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, baseY), new Vector2(width, ph));
            float ringY = baseY + ph * 0.5f + ring * 0.42f + lift;
            Avatar(holder, e, new Vector2(0f, ringY), ring, metal);
            var badge = Decor("Medal", holder, RankArt("medal-" + metal));
            badge.preserveAspect = true;
            UiFactory.Place(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, ringY - ring * 0.46f), new Vector2(60f, 78f));
            var n = RankText("Num", badge.transform, (index + 1).ToString(), 34);
            n.raycastTarget = false;
            UiFactory.Place(n.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -27f), new Vector2(50f, 44f));
            // İsim ve puan ön levhada (levha görselin ~%55–90 yüksekliğinde).
            float plateY = baseY - ph * 0.08f;
            var name = RankText("Name", holder, e.Name, 36);
            name.raycastTarget = false;
            UiFactory.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, plateY + 20f), new Vector2(width * 0.62f, 44f));
            name.overflowMode = TextOverflowModes.Ellipsis; name.textWrappingMode = TextWrappingModes.NoWrap;
            FitText(name, 24);
            ScoreWithTrophy(holder, e.Score, new Vector2(0f, plateY - 22f), 34, width * 0.62f);
        }

        /// <summary>Ana sıralama puanı: kupa simgesi + sayı (yıldız toplamıyla karışmasın).</summary>
        private static void ScoreWithTrophy(RectTransform parent, int score, Vector2 pos, int size, float maxW)
        {
            var holder = UiFactory.Rect("Score", parent);
            UiFactory.Place(holder, new Vector2(0.5f, 1f), pos, new Vector2(maxW, size + 8f));
            var t = RankText("Value", holder, Num(score), size);
            t.raycastTarget = false;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            float tw = Mathf.Min(maxW - size - 8f, t.GetPreferredValues(Num(score), maxW, size + 8f).x);
            float total = size + 6f + tw;
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-total * 0.5f + size + 6f + tw * 0.5f, 0f), new Vector2(tw + 4f, size + 8f));
            var cup = Decor("Trophy", holder, RankArt("trophy"));
            cup.preserveAspect = true;
            UiFactory.Place(cup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-total * 0.5f + size * 0.5f, 0f), new Vector2(size, size));
        }

        /// <summary>Referanstaki kalın lacivert yazı (kontursuz; başlık stili değil).</summary>
        private static Text RankText(string name, Transform parent, string text, int size, Color? color = null)
        {
            var t = UiFactory.Text(name, parent, text, size, color ?? RankInk, TextAnchor.MiddleCenter, FontStyle.Bold);
            // Kullanıcı (2026-11-08): ana metin Nunito 800 (ExtraBold = UiTypography.Font, varsayılan).
            t.raycastTarget = false;
            return t;
        }

        private static string Num(int v) => v.ToString("N0", Loc.Culture);

        // ---- Avatar ------------------------------------------------------------------------------------------

        private static readonly Color[] AvatarTones =
        {
            new Color32(0x3E, 0xCC, 0xC4, 0xFF), new Color32(0xF2, 0x6A, 0x5C, 0xFF), new Color32(0xB0, 0x6C, 0xF0, 0xFF),
            new Color32(0xF2, 0xB8, 0x3A, 0xFF), new Color32(0x4A, 0x9C, 0xF0, 0xFF), new Color32(0x5C, 0xC0, 0x6A, 0xFF),
        };

        /// <summary>
        /// Oyuncu avatarı. Oyunda henüz avatar sistemi yok (paket: mevcut avatarlar kullanılmalı) → kendi kartında maskot,
        /// diğerlerinde adın baş harfi renkli dairede. Halka: podyumda metal, listede ince beyaz kenar.
        /// </summary>
        private static void Avatar(RectTransform parent, LeaderboardEntry e, Vector2 pos, float size, string metal, Sprite face = null, bool centered = false)
        {
            var bgc = UiFactory.Image("Avatar", parent, UiArt.Circle, AvatarTones[Mathf.Abs((e.Name ?? "").GetHashCode()) % AvatarTones.Length]);
            bgc.raycastTarget = false;
            float inner = metal != null ? size * 0.78f : size * 0.9f;
            var anchor = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
            UiFactory.Place(bgc.rectTransform, anchor, pos, new Vector2(inner, inner));
            if (face != null)
            {
                bgc.color = new Color32(0xE8, 0xFA, 0xFF, 0xFF);
                var f = Decor("Face", bgc.transform, face);
                f.preserveAspect = true;
                UiFactory.Stretch(f.rectTransform, inner * 0.08f, inner * 0.08f, inner * 0.06f, inner * 0.1f);
            }
            else
            {
                string init = string.IsNullOrEmpty(e.Name) ? "?" : e.Name.Substring(0, 1).ToUpper(Loc.Culture);
                var t = UiFactory.Title("Initial", bgc.transform, init, Mathf.RoundToInt(inner * 0.5f), Color.white, new Color(0f, 0f, 0f, 0.25f));
                t.raycastTarget = false;
                UiFactory.Stretch(t.rectTransform);
            }
            if (metal != null)
            {
                var ring = Decor("Ring", parent, RankArt("ring-" + metal));
                ring.preserveAspect = true;
                UiFactory.Place(ring.rectTransform, anchor, pos, new Vector2(size, size));
            }
            else
            {
                var ol = bgc.gameObject.AddComponent<Outline>();
                ol.effectColor = Color.white;
                ol.effectDistance = new Vector2(3f, -3f);
            }
        }

        // ---- İlk 10 listesi -------------------------------------------------------------------------------------

        private void RankListPanel(RectTransform parent, RankScreenModel m)
        {
            var panel = UiFactory.Rect("List", parent);
            panel.anchorMin = new Vector2(0.5f, 0f); panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1040f, -(RankListTop + RankListBottom));
            panel.anchoredPosition = new Vector2(0f, (RankListBottom - RankListTop) * 0.5f);
            // Çerçevenin iç alfası var: arkasına, sınırları içinde düz #F7F8F8 dolgu (deniz liste içinden görünmesin).
            var fill = UiFactory.Image("Fill", panel, UiArt.Box, RankFill, true);
            fill.raycastTarget = false;
            UiFactory.Stretch(fill.rectTransform, 24f, 24f, 24f, 24f);
            var frame = UiFactory.Image("Frame", panel, RankArt("list-panel"), Color.white, true);
            frame.raycastTarget = false;
            UiFactory.Stretch(frame.rectTransform);
            frame.pixelsPerUnitMultiplier = CornerMul(frame.sprite, 58f); // köşe ~58 birim
            // Çerçeve PNG'sinin iç alanında yarı saydam gri gölge var (boş listede bulut gibi görünüyordu): kenar
            // şeridinin içinden ikinci bir düz dolgu çerçevenin önüne gelir; inci kenar ve iç gölge çizgisi görünür kalır.
            if (!Ui1.Ready) // ui1 krem paneli opak: iç dolgu gerekmez
            {
                var inner = UiFactory.Image("InnerFill", panel, UiArt.Box, RankFill, true);
                inner.raycastTarget = false;
                UiFactory.Stretch(inner.rectTransform, 30f, 30f, 30f, 32f);
            }
            else fill.color = Ui1.Cream;

            // Başlık: "İLK 10" | "Sıralama puanı"
            var top = RankText("Top10", panel, Loc.T("rank.top10"), 56);
            top.raycastTarget = false;
            UiFactory.Place(top.rectTransform, new Vector2(0.5f, 1f), new Vector2(-150f, -56f), new Vector2(300f, 64f));
            FitText(top, 36);
            var sep = UiFactory.Image("Sep", panel, UiArt.Box, RankDivider);
            sep.raycastTarget = false;
            UiFactory.Place(sep.rectTransform, new Vector2(0.5f, 1f), new Vector2(10f, -56f), new Vector2(3f, 46f));
            var cap = RankSliced("ScoreCap", panel, RankArt("title-capsule"), new Vector2(0.5f, 1f), new Vector2(170f, -56f), new Vector2(290f, 58f));
            var ct = UiFactory.Text("Text", cap.transform, Loc.T("rank.score"), 26, RankInk);
            ct.raycastTarget = false;
            UiFactory.Stretch(ct.rectTransform, 24f, 24f, 0f, 2f);
            FitText(ct, 18);

            // Sütun başlığı bandı.
            var band = RankSliced("Header", panel, RankArt("row-blue"), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(984f, 50f));
            var h1 = UiFactory.Text("Player", band.transform, Loc.T("rank.player"), 26, RankInk);
            h1.raycastTarget = false;
            UiFactory.Place(h1.rectTransform, new Vector2(0f, 0.5f), new Vector2(230f, 1f), new Vector2(220f, 40f));
            var h2 = UiFactory.Text("Cols", band.transform, Loc.T("rank.cols"), 24, RankInk);
            h2.raycastTarget = false;
            UiFactory.Place(h2.rectTransform, new Vector2(0f, 0.5f), new Vector2(620f, 1f), new Vector2(380f, 40f));
            FitText(h2, 16);

            // Kaydırılabilir satır alanı (kısa ekranda liste kayar; kart ve sekmeler sabit).
            var viewport = UiFactory.Rect("Viewport", panel);
            UiFactory.Stretch(viewport, 26f, 26f, 150f, 30f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var content = UiFactory.Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            _rankList = content;

            var r = m.Result;
            if (m.Loading) { ListMessage(viewport, Loc.T("rank.loading"), false); return; }
            if (r == null || r.Status == LeaderboardStatus.Error) { ListMessage(viewport, Loc.T("rank.error"), true); return; }
            if (r.Status == LeaderboardStatus.NotConnected) { ListMessage(viewport, Loc.T("rank.offline"), true); return; }
            if (r.Status == LeaderboardStatus.NoCountry) { ListMessage(viewport, Loc.T("rank.nocountry"), false); return; }
            if (r.Status == LeaderboardStatus.Empty || r.Top.Count == 0) { ListMessage(viewport, Loc.T("rank.empty"), false); return; }

            content.sizeDelta = new Vector2(0f, r.Top.Count * (RankRowH + RankRowGap));
            for (int i = 0; i < r.Top.Count; i++) RankRow(content, r.Top[i], i);
            if (r.DevSample)
            {
                // Geliştirme verisi açıkça etiketli (canlı sıralama gibi sunulmaz).
                var dev = UiFactory.Image("DevSample", panel, UiArt.Box, new Color32(0xF2, 0x6A, 0x5C, 0xFF), true);
                dev.raycastTarget = false;
                UiFactory.Place(dev.rectTransform, new Vector2(0.5f, 0f), new Vector2(-150f, 6f), new Vector2(560f, 34f));
                var dt = UiFactory.Text("Text", dev.transform, Loc.T("rank.dev"), 20, Color.white);
                dt.raycastTarget = false;
                UiFactory.Stretch(dt.rectTransform, 10f, 10f, 0f, 0f);
                FitText(dt, 14);
            }
        }

        private void ListMessage(RectTransform viewport, string text, bool retry)
        {
            var t = UiFactory.Text("Message", viewport, text, 34, RankInk2, TextAnchor.MiddleCenter, FontStyle.Normal); // ikincil: Nunito 600
            t.raycastTarget = false;
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, retry ? 50f : 0f), new Vector2(820f, 160f));
            t.enableAutoSizing = true; t.fontSizeMin = 24; t.fontSizeMax = 34;
            if (!retry) return;
            var b = UiFactory.SpriteButton("Retry", viewport, null, new Vector2(340f, 92f), () => RankRetryPressed?.Invoke());
            b.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(340f, 92f));
            RankSliced("Bg", b.transform, RankArt("tab-on"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 92f));
            var bt = RankText("Text", b.transform, Loc.T("rank.retry"), 36);
            bt.raycastTarget = false;
            UiFactory.Stretch(bt.rectTransform, 30f, 30f, 0f, 4f);
            FitText(bt, 24);
            PressFeedback.Attach(b, null, 80f, 120f);
        }

        private void RankRow(RectTransform content, LeaderboardEntry e, int i)
        {
            var row = UiFactory.Rect("Row" + e.Rank, content);
            row.anchorMin = new Vector2(0f, 1f); row.anchorMax = new Vector2(1f, 1f); row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, RankRowH);
            row.anchoredPosition = new Vector2(0f, -i * (RankRowH + RankRowGap));
            // Zemin: 1 ve 3 sıcak krem (altın/bronz), 2 mavi; 4+ beyaz/mavi dönüşümlü.
            bool blue = e.Rank == 2 || (e.Rank > 3 && e.Rank % 2 == 1);
            var bg = UiFactory.Image("Bg", row, RankArt(blue ? "row-blue" : "row-white"), Color.white, true);
            bg.raycastTarget = false;
            UiFactory.Stretch(bg.rectTransform);
            var sp = bg.sprite;
            if (sp != null) bg.pixelsPerUnitMultiplier = sp.rect.height / RankRowH;
            if (Ui1.Ready && blue) bg.color = new Color(0.9f, 0.97f, 1f);
            if (e.Rank == 1) bg.color = new Color(1f, 0.93f, 0.76f);
            else if (e.Rank == 3) bg.color = new Color(1f, 0.9f, 0.8f);

            const float left = 0f; // satır sol kenarı (content genişliği ~988)
            if (e.Rank >= 1 && e.Rank <= 3)
            {
                string metal = e.Rank == 1 ? "gold" : e.Rank == 2 ? "silver" : "bronze";
                var md = Decor("Medal", row, RankArt("medal-" + metal));
                md.preserveAspect = true;
                UiFactory.Place(md.rectTransform, new Vector2(0f, 0.5f), new Vector2(left + 62f, -2f), new Vector2(56f, 72f));
                var mn = RankText("Num", md.transform, e.Rank.ToString(), 30);
                mn.raycastTarget = false;
                UiFactory.Place(mn.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(46f, 40f));
            }
            else
            {
                var bd = Decor("Badge", row, RankArt("rank-badge"));
                bd.preserveAspect = true;
                UiFactory.Place(bd.rectTransform, new Vector2(0f, 0.5f), new Vector2(left + 62f, 0f), new Vector2(58f, 58f));
                var bn = RankText("Num", bd.transform, e.Rank.ToString(), 30);
                bn.raycastTarget = false;
                UiFactory.Stretch(bn.rectTransform, 4f, 4f, 0f, 2f);
                FitText(bn, 20);
            }
            var av = UiFactory.Rect("AvatarHolder", row);
            UiFactory.Place(av, new Vector2(0f, 0.5f), new Vector2(left + 160f, 0f), new Vector2(10f, 10f));
            Avatar(av, e, Vector2.zero, 72f, e.Rank <= 3 ? (e.Rank == 1 ? "gold" : e.Rank == 2 ? "silver" : "bronze") : null, null, true);

            // Ad (tek satır, taşarsa üç nokta; puan alanını ezmez) ve ikincil değerler.
            var name = RankText("Name", row, e.Name, 34);
            name.raycastTarget = false;
            // Önce konum (genişlik), sonra taşma ayarı: genişlik 0 iken üç nokta hesaplanırsa metin boş kalıyordu.
            UiFactory.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(left + 215f + 200f, 14f), new Vector2(400f, 52f));
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            Metrics(row, e, new Vector2(left + 215f, -19f), 25, 420f);

            var div = UiFactory.Image("Div", row, UiArt.Box, RankDivider);
            div.raycastTarget = false;
            UiFactory.Place(div.rectTransform, new Vector2(1f, 0.5f), new Vector2(-290f, 0f), new Vector2(3f, 44f));
            var score = RankText("Score", row, Num(e.Score), 42);
            score.raycastTarget = false;
            score.alignment = TextAlignmentOptions.MidlineRight;
            UiFactory.Place(score.rectTransform, new Vector2(1f, 0.5f), new Vector2(-24f - 100f, 0f), new Vector2(200f, 50f));
            FitText(score, 28);
            var cup = Decor("Trophy", row, RankArt("trophy"));
            cup.preserveAspect = true;
            UiFactory.Place(cup.rectTransform, new Vector2(1f, 0.5f), new Vector2(-252f, 0f), new Vector2(40f, 40f));
        }

        /// <summary>"ZPE 72 · Bölüm 860 · ★ 2450" — sol hizalı; yıldız simgesi yalnız yıldız metriğinde.</summary>
        private static void Metrics(RectTransform parent, LeaderboardEntry e, Vector2 leftPos, int size, float maxW)
        {
            string a = $"ZPE {Num(e.Zpe)} · {Loc.F("rank.level", Num(e.Level))} ·";
            var t = UiFactory.Text("Metrics", parent, a, size, RankInk2, TextAnchor.MiddleLeft, FontStyle.Normal); // ikincil: Nunito 600
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            float w = Mathf.Min(maxW - 90f, t.GetPreferredValues(a, maxW, size + 6f).x);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(leftPos.x + w * 0.5f, leftPos.y), new Vector2(w + 2f, size + 8f));
            var star = Decor("Star", parent, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            star.preserveAspect = true;
            UiFactory.Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(leftPos.x + w + 6f + size * 0.5f, leftPos.y + 1f), new Vector2(size + 4f, size + 4f));
            var s = UiFactory.Text("Stars", parent, Num(e.Stars), size, RankInk2, TextAnchor.MiddleLeft, FontStyle.Normal); // ikincil: Nunito 600
            s.raycastTarget = false;
            UiFactory.Place(s.rectTransform, new Vector2(0f, 0.5f), new Vector2(leftPos.x + w + size + 14f + 45f, leftPos.y), new Vector2(90f, size + 8f));
        }

        // ---- Senin sıran (sabit kart) ---------------------------------------------------------------------------

        private void RankSelfCard(RectTransform page, RankScreenModel m)
        {
            var card = UiFactory.Rect("SelfCard", page);
            UiFactory.Place(card, new Vector2(0.5f, 0f), new Vector2(0f, RankCardBottom + RankCardH * 0.5f), new Vector2(RankCardW, RankCardH));
            var bg = UiFactory.Image("Bg", card, RankArt("self-panel"), Color.white, true);
            bg.raycastTarget = true; // arkasındaki liste satırlarına dokunulmaz
            UiFactory.Stretch(bg.rectTransform);
            bg.pixelsPerUnitMultiplier = CornerMul(bg.sprite, 92f);

            // Maskot kartın üstünden bakar (statik PNG; yalnız hafif salınım).
            var mascot = Decor("Mascot", card, RankArt("mascot"));
            mascot.preserveAspect = true;
            UiFactory.Place(mascot.rectTransform, new Vector2(0.5f, 1f), new Vector2(270f, 76f), new Vector2(214f, 187f));
            _rankFx.Mascot = mascot.rectTransform;

            var label = RankSliced("YourRank", card, RankArt("title-capsule"), new Vector2(0f, 1f), new Vector2(190f, 2f), new Vector2(350f, 76f));
            var lt = RankText("Text", label.transform, Loc.T("rank.your"), 40);
            lt.raycastTarget = false;
            UiFactory.Stretch(lt.rectTransform, 26f, 26f, 0f, 4f);
            FitText(lt, 26);

            var self = m.Self ?? new LeaderboardEntry();
            bool eligible = m.Completed >= m.MinLevels;
            int rank = m.Result?.Self != null ? m.Result.Self.Rank : 0;
            // Sıra: yoksa "—" (0 yazılmaz).
            var rp = RankSliced("RankPill", card, RankArt("title-capsule"), new Vector2(0.5f, 0.5f), new Vector2(-404f, 36f), new Vector2(170f, 76f));
            var rt = RankText("Rank", rp.transform, eligible && rank > 0 ? Num(rank) : "—", 56);
            rt.raycastTarget = false;
            UiFactory.Stretch(rt.rectTransform, 12f, 12f, 0f, 4f);
            FitText(rt, 30);
            Divider(card, new Vector2(-306f, 36f));
            var av = UiFactory.Rect("AvatarHolder", card);
            UiFactory.Place(av, new Vector2(0.5f, 0.5f), new Vector2(-236f, 36f), new Vector2(10f, 10f));
            Avatar(av, self, Vector2.zero, 96f, "gold", m.SelfAvatar, true);
            var you = RankText("You", card, Loc.T("rank.you"), 50);
            you.raycastTarget = false;
            you.alignment = TextAlignmentOptions.MidlineLeft;
            UiFactory.Place(you.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-50f, 38f), new Vector2(220f, 60f));
            FitText(you, 30);
            Divider(card, new Vector2(150f, 36f));
            ScoreWithTrophy(card, eligible ? self.Score : 0, new Vector2(330f, -(RankCardH * 0.5f - 36f)), 56, 300f);
            if (!eligible)
            {
                var sc = card.Find("Score/Value") as RectTransform;
                if (sc != null) sc.GetComponent<Text>().text = "—";
            }

            Pill(card, new Vector2(-290f, -38f), $"ZPE {Num(self.Zpe)}", false);
            Pill(card, new Vector2(0f, -38f), Loc.F("rank.level", Num(self.Level)), false);
            Pill(card, new Vector2(290f, -38f), Num(self.Stars), true);
            string foot = !eligible ? Loc.F("rank.need", m.Completed, m.MinLevels)
                : rank <= 0 ? Loc.T("rank.notranked") : Loc.T("rank.formula");
            var ft = UiFactory.Text("Foot", card, foot, 26, RankInk2, TextAnchor.MiddleCenter, FontStyle.Normal); // ikincil: Nunito 600
            ft.raycastTarget = false;
            UiFactory.Place(ft.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -92f), new Vector2(820f, 34f));
            FitText(ft, 18);
        }

        private static void Divider(RectTransform parent, Vector2 pos)
        {
            var d = UiFactory.Image("Div", parent, UiArt.Box, RankDivider);
            d.raycastTarget = false;
            UiFactory.Place(d.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(3f, 58f));
        }

        private static void Pill(RectTransform parent, Vector2 pos, string text, bool star)
        {
            var p = RankSliced("Metric", parent, RankArt("metric-pill"), new Vector2(0.5f, 0.5f), pos, new Vector2(226f, 52f));
            if (Ui3.Ready)
            {
                // ui3/07: kendi sıra kapsülleri petrol (tealTop→tealBottom), pirinç kenar, yazı onTeal.
                p.sprite = Ui3.Rounded(26f, Ui3.TealTop, Ui3.TealBottom, 2f, Ui3.Brass, 0f, default, 52);
                p.type = Image.Type.Sliced;
                p.pixelsPerUnitMultiplier = 1f;
                p.color = Color.white;
            }
            float x = 0f;
            if (star)
            {
                var s = Decor("Star", p.transform, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
                s.preserveAspect = true;
                UiFactory.Place(s.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-50f, 1f), new Vector2(38f, 38f));
                x = 20f;
            }
            var t = RankText("Text", p.transform, text, 30);
            t.raycastTarget = false;
            if (Ui3.Ready) t.color = Ui3.OnTeal;
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 1f), new Vector2(190f - x, 44f));
            FitText(t, 18);
        }

        // ---- Bilgi -------------------------------------------------------------------------------------------

        private RectTransform _rankInfo;

        public void ShowRankInfo()
        {
            if (_page == null) return;
            if (_rankInfo != null) UiFactory.Kill(_rankInfo.gameObject);
            if (Ui1.Ready) { ShowRankInfoUi1(); return; } // ui1/08
            var root = UiFactory.Rect("RankInfo", _page);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _rankInfo = root;
            var dim = UiFactory.SpriteButton("Dim", root, UiArt.Box, new Vector2(10f, 10f), CloseRankInfo);
            dim.GetComponent<Image>().color = new Color(0.04f, 0.1f, 0.2f, 0.6f);
            UiFactory.Stretch((RectTransform)dim.transform);
            var card = UiFactory.Image("Card", root, RankArt("list-panel"), Color.white, true);
            card.raycastTarget = true;
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(900f, 760f));
            card.pixelsPerUnitMultiplier = CornerMul(card.sprite, 58f);
            var fill = UiFactory.Image("Fill", card.transform, UiArt.Box, RankFill, true);
            fill.raycastTarget = false;
            UiFactory.Stretch(fill.rectTransform, 24f, 24f, 24f, 24f);
            fill.transform.SetAsFirstSibling();
            var t = RankText("Title", card.transform, Loc.T("rank.score"), 50);
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(760f, 70f));
            var body = UiFactory.Text("Body", card.transform, Loc.T("rank.info.body"), 32, RankInk2, TextAnchor.UpperLeft, FontStyle.Normal); // ikincil: Nunito 600
            body.raycastTarget = false;
            UiFactory.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(760f, 560f));
            body.enableAutoSizing = true; body.fontSizeMin = 22; body.fontSizeMax = 32;
            var close = UiFactory.SpriteButton("Close", card.transform, RankArt("btn-back"), new Vector2(90f, 90f), CloseRankInfo);
            close.GetComponent<Image>().preserveAspect = true;
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-60f, -60f), new Vector2(90f, 90f));
            PressFeedback.Attach(close, null, 80f, 120f);
        }

        public void CloseRankInfo()
        {
            if (_rankInfo != null) UiFactory.Kill(_rankInfo.gameObject);
            _rankInfo = null;
        }

        /// <summary>Harita: sol sütunda sıralama düğmesi (kupa), altında etiket.</summary>
        private void BuildRankButton(RectTransform shell)
        {
            var cup = RankArt("trophy");
            if (cup == null) return;
            var b = ShellRoundButton("RankButton", shell, cup, 132f, () => RankPressed?.Invoke());
            UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(-V11ColumnX, -V11ColumnZpeTop - V11LeftShift - 66f), new Vector2(132f, 132f));
            var t = Ui3.Ready ? UiFactory.Title("RankLabel", shell, Loc.T("rank.title"), 30, Ui3.Ink, Ui3.IvoryTop) : UiFactory.Title("RankLabel", shell, Loc.T("rank.title"), 30, Color.white, V11Navy);
            t.raycastTarget = false;
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(-V11ColumnX, -V11ColumnZpeTop - V11LeftShift - 152f), new Vector2(200f, 40f));
            FitText(t, 20);
        }
    }

    /// <summary>
    /// Sıralama ekranı hafif animasyonları (belge §Etkileşim): ilk girişte 180 ms görünme; sekme değişiminde 160 ms
    /// içerik geçişi; maskot 2,8 sn ±3 birim salınım (azaltılmış hareket seçeneğinde kapalı). Satırlar zıplatılmaz.
    /// </summary>
    public sealed class RankFx : MonoBehaviour
    {
        public static bool ReducedMotion;
        public CanvasGroup Group, Content;
        public RectTransform Mascot;
        public bool FadeIn;
        private float _t;
        private Vector2 _mascotBase;
        private bool _based;

        private void Start()
        {
            if (Group != null) Group.alpha = FadeIn ? 0f : 1f;
            if (Content != null && !FadeIn) Content.alpha = 0f; // sekme/yenileme: yalnız içerik geçişi
        }

        private void Update() => Tick(Time.unscaledDeltaTime);

        public void Tick(float dt)
        {
            _t += dt;
            if (Group != null && FadeIn) Group.alpha = Mathf.Clamp01(_t / 0.18f);
            if (Content != null && !FadeIn) Content.alpha = Mathf.Clamp01(_t / 0.16f);
            if (Mascot == null || ReducedMotion) return;
            if (!_based) { _mascotBase = Mascot.anchoredPosition; _based = true; }
            Mascot.anchoredPosition = _mascotBase + new Vector2(0f, 3f * Mathf.Sin(_t * 2f * Mathf.PI / 2.8f));
        }
    }
}
