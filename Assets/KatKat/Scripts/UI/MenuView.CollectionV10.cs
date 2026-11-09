using System;
using System.Collections.Generic;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// v10 Koleksiyon ekranı (docs/v10/TriKat-Koleksiyon-Claude, onaylı referans 941 × 1672 → 1080 birim, ×1,148).
    /// Sabit: arka plan, sandık illüstrasyonu, "Koleksiyon" kurdelesi, köşe düğmeleri, panel çerçevesi, kategori
    /// kapsülü + sayaç (görünen kategoriye göre güncellenir) ve alt menü. Yalnız kartlar panel maskesi içinde kayar.
    /// Final görseli olmayan parçalar Resources/PremiumV6/collection/ altında aranır, yoksa GEÇİCİ çizim:
    /// bg, chest, ribbon, panel, badge-check, badge-diamond.
    /// </summary>
    public sealed partial class MenuView
    {
        private const float CollPanelTop = 349f, CollPanelSide = 15f, CollPanelGapBottom = 22f;
        private const float CollCardW = 300f, CollCardH = 360f, CollGap = 20f;
        private static readonly Color CollPillGold = new Color32(0xF7, 0xC8, 0x5A, 0xFF);
        private static readonly Color CollInk = new Color32(0x2A, 0x1C, 0x4E, 0xFF);
        private static readonly Color CollCool = new Color32(0x5E, 0xD6, 0xFF, 0xFF);
        private static readonly Color CollWarm = new Color32(0xFF, 0xC8, 0x48, 0xFF);

        private static Sprite CollArt(string key) => SpecialArt.V6("collection/" + key);

        public static string[] CollectionTemporaryParts()
        {
            var list = new List<string>();
            foreach (var k in new[] { "bg", "chest", "ribbon", "panel", "badge-check", "badge-diamond" })
                if (CollArt(k) == null) list.Add(k);
            return list.ToArray();
        }

        private ScrollRect _collectionScroll;

        /// <summary>Test/yakalama: koleksiyon kaydırması (0 = alt, 1 = üst).</summary>
        public void ScrollCollectionForTest(float normalizedY)
        {
            if (_collectionScroll == null) return;
            Canvas.ForceUpdateCanvases();
            _collectionScroll.verticalNormalizedPosition = normalizedY;
            _collectionScroll.onValueChanged.Invoke(_collectionScroll.normalizedPosition);
        }

        /// <summary>Turkuaz gövdeli, altın kenarlı yuvarlak düğme (geri, ayarlar).</summary>
        internal static Button ShellRoundButton(string name, Transform parent, Sprite icon, float s, Action act)
        {
            var disc = Ui3.Art("ikon-dugmesi");
            if (disc != null)
            {
                // ui3: petrol yeşili disk + pirinç kenar (ikon-dugmesi), glif çapın yarısı, ortada.
                var b3 = UiFactory.SpriteButton(name, parent, disc, new Vector2(s, s), act);
                b3.GetComponent<Image>().preserveAspect = true;
                var ic3 = Decor("Icon", b3.transform, icon);
                ic3.preserveAspect = true;
                UiFactory.Stretch(ic3.rectTransform, s * 0.25f, s * 0.25f, s * 0.25f, s * 0.27f);
                return b3;
            }
            var b = UiFactory.SpriteButton(name, parent, UiArt.Circle, new Vector2(s, s), act);
            b.GetComponent<Image>().color = ShellGold;
            var body = Decor("Body", b.transform, UiArt.Circle);
            body.color = ShellTealDark;
            UiFactory.Stretch(body.rectTransform, s * 0.08f, s * 0.08f, s * 0.08f, s * 0.08f);
            var hi = Decor("Hi", b.transform, UiArt.Circle);
            hi.color = ShellTeal;
            UiFactory.Stretch(hi.rectTransform, s * 0.11f, s * 0.11f, s * 0.10f, s * 0.15f);
            var ic = Decor("Icon", b.transform, icon);
            ic.preserveAspect = true;
            UiFactory.Stretch(ic.rectTransform, s * 0.23f, s * 0.23f, s * 0.21f, s * 0.25f);
            return b;
        }

        public void ShowCollection(IList<StoneItem> items, Action daily, Action back, Action settings = null)
        {
            if (Ui1.Ready) { ShowCollectionUi1(items, back, settings); return; } // ui1/03
            // Final arka plan menünün tam ekran arka plan katmanında (tuval boyu, cover; güvenli alanın dışını da kaplar).
            var page = NewPage(CollArt("bg"));

            // 1) Arka plan (tam ekran, güvenli alan dışına taşar).
            if (CollArt("bg") == null)
            {
                var bg = Decor("Background", page, CollBgTemp());
                UiFactory.Stretch(bg.rectTransform, -60f, -60f, -260f, -260f);
            }

            // 2) Sandık + kristaller (kurdelenin arkasında) ve kurdele.
            var chestArt = CollArt("chest");
            var chest = Decor("Chest", page, chestArt ?? ChestIconTemp());
            chest.preserveAspect = true;
            if (chestArt != null)
            {
                // 1250×869: genişlik 406 (referans görünür sınır), oran korunur; alt kısmı kurdelenin arkasında kalır.
                float ch = 406f * chestArt.rect.height / chestArt.rect.width;
                UiFactory.Place(chest.rectTransform, new Vector2(0.5f, 1f), new Vector2(-7f, -4f - ch * 0.5f), new Vector2(406f, ch));
            }
            else UiFactory.Place(chest.rectTransform, new Vector2(0.5f, 1f), new Vector2(-7f, -101f), new Vector2(190f, 189f));
            if (chestArt == null)
                for (int k = 0; k < 4; k++)
                {
                    var cr = Decor("Crystal", page, CrystalIconTemp());
                    cr.preserveAspect = true;
                    cr.color = k % 2 == 0 ? new Color(0.78f, 0.6f, 1f) : Color.white;
                    float cx = (k < 2 ? -1 : 1) * (120f + (k % 2) * 46f);
                    UiFactory.Place(cr.rectTransform, new Vector2(0.5f, 1f), new Vector2(cx, -136f + (k % 2) * 18f), new Vector2(70f - (k % 2) * 16f, 84f));
                }
            var ribbonArt = CollArt("ribbon");
            var ribbon = Decor("Ribbon", page, ribbonArt ?? CollRibbonTemp());
            ribbon.preserveAspect = ribbonArt != null;
            UiFactory.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -227f),
                ribbonArt != null ? new Vector2(700f, 700f * ribbonArt.rect.height / ribbonArt.rect.width) : new Vector2(660f, 128f));
            var title = UiFactory.Title("Title", page, Loc.T("collection.title"), 61, new Color32(0xFF, 0xEC, 0xC2, 0xFF), new Color32(0x3E, 0x1C, 0x5E, 0xFF));
            title.raycastTarget = false;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -222f), new Vector2(430f, 80f));
            FitText(title, 34);

            // Köşe düğmeleri: geri ve ayarlar (mevcut aksiyonlar).
            var bb = ShellRoundButton("Back", page, PremiumArt.V4("icons/back") ?? UiArt.Icon(UiIcon.Back), 110f, back);
            UiFactory.Place((RectTransform)bb.transform, new Vector2(0f, 1f), new Vector2(83f, -78f), new Vector2(110f, 110f));
            if (settings != null)
            {
                var gb = ShellRoundButton(Loc.T("ui.settings"), page, PremiumArt.V4("icons/settings") ?? UiArt.Icon(UiIcon.SoundOn), 110f, settings);
                UiFactory.Place((RectTransform)gb.transform, new Vector2(1f, 1f), new Vector2(-82f, -78f), new Vector2(110f, 110f));
            }

            // 3) Panel: üstten CollPanelTop, alttan alt menünün üstüne kadar (güvenli alana göre).
            var panelArt = CollArt("panel");
            var panel = UiFactory.Image("Panel", page, panelArt ?? CollPanelTemp(), Color.white, true);
            panel.raycastTarget = false;
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0f, 0f); prt.anchorMax = new Vector2(1f, 1f); prt.pivot = new Vector2(0.5f, 0.5f);
            prt.offsetMin = new Vector2(CollPanelSide, ShellBarH + CollPanelGapBottom);
            prt.offsetMax = new Vector2(-CollPanelSide, -CollPanelTop);
            if (panelArt != null)
            {
                // Final çerçeve (1122×1393, 9 dilim: üst 130, alt 250, yan 0): yatayda eşit ölçek, köşeler ve alt elmas
                // ezilmez; ekran oranı değişince yalnız orta bölüm dikey uzar. Elmas çerçevenin içinde.
                panel.pixelsPerUnitMultiplier = panelArt.rect.width / (1080f - 2f * CollPanelSide);
            }
            else
            {
                var badge = Decor("Diamond", page, CollArt("badge-diamond") ?? DiamondBadgeTemp());
                badge.preserveAspect = true;
                UiFactory.Place(badge.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, ShellBarH + CollPanelGapBottom + 22f), new Vector2(96f, 96f));
            }

            // Kaydırma alanı (panel içi maske; final çerçevede altın kenarın ve alt dalganın içinde).
            var viewport = UiFactory.Rect("Viewport", prt);
            if (panelArt != null) UiFactory.Stretch(viewport, 48f, 48f, 66f, 165f);
            else UiFactory.Stretch(viewport, 36f, 36f, 58f, 44f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var content = UiFactory.Rect("Content", viewport);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);

            float y = -14f;
            string section = null;
            int col = 0;
            var sectionTops = new List<(float y, string id, string title, int owned, int total)>();
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.Section != section)
                {
                    if (col != 0) { y -= CollCardH + CollGap; col = 0; }
                    int owned = 0, total = 0;
                    foreach (var o in items) if (o.Section == it.Section) { total++; if (o.Owned) owned++; }
                    if (section != null)
                    {
                        // Sonraki kategoriler: içerikte ayraç başlığı (sabit kapsül de buna göre güncellenir).
                        y -= 8f;
                        var sep = UiFactory.Image("SectionLine", content, UiArt.Box, new Color(0.96f, 0.76f, 0.30f, 0.55f));
                        sep.raycastTarget = false;
                        UiFactory.Place(sep.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(900f, 3f));
                        var st = UiFactory.Title("Section", content, it.SectionTitle, 38, CollPillGold, CollInk);
                        st.raycastTarget = false;
                        UiFactory.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 44f), new Vector2(700f, 60f));
                        FitText(st, 24);
                        y -= 90f;
                    }
                    sectionTops.Add((y, it.Section, it.SectionTitle, owned, total));
                    section = it.Section;
                }
                CollCard(content, it, new Vector2((col - 1) * (CollCardW + CollGap), y - CollCardH * 0.5f), StonePreviewRequested);
                if (++col == 3) { col = 0; y -= CollCardH + CollGap; }
            }
            if (col != 0) y -= CollCardH + CollGap;
            content.sizeDelta = new Vector2(3f * CollCardW + 2f * CollGap, -y + 20f);

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 60f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            _collectionScroll = scroll;

            // 4) Sabit kategori kapsülü + sayaç (panelin üst kenarına biner).
            var cap = GoldCapsule("CategoryCapsule", page, ShellPurple, new Vector2(0.5f, 1f), new Vector2(-244f, -CollPanelTop), new Vector2(506f, 88f), 8f);
            var cstar = Decor("Star", cap.transform, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            cstar.preserveAspect = true;
            UiFactory.Place(cstar.rectTransform, new Vector2(0f, 0.5f), new Vector2(82f, 2f), new Vector2(84f, 84f));
            var ctitle = UiFactory.Title("CategoryTitle", cap.transform, "", 46, new Color32(0xFF, 0xEC, 0xC2, 0xFF), new Color32(0x3E, 0x1C, 0x5E, 0xFF));
            ctitle.alignment = UiTypography.Alignment(TextAnchor.MiddleLeft);
            ctitle.raycastTarget = false;
            UiFactory.Place(ctitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(140f + 175f, 0f), new Vector2(350f, 80f));
            FitText(ctitle, 24);
            var cnt = GoldCapsule("CounterCapsule", page, ShellPurple, new Vector2(0.5f, 1f), new Vector2(366f, -CollPanelTop - 4f), new Vector2(211f, 80f), 8f);
            var ccount = UiFactory.Title("Count", cnt.transform, "", 46, Color.white, new Color32(0x3E, 0x1C, 0x5E, 0xFF));
            ccount.raycastTarget = false;
            UiFactory.Stretch(ccount.rectTransform, 14, 14, 6, 6);
            FitText(ccount, 24);

            int shown = -1;
            void SyncHeader()
            {
                if (sectionTops.Count == 0) return;
                float top = content.anchoredPosition.y + 40f; // görünür üst kenar (içerik koordinatı, aşağı negatif)
                int idx = 0;
                for (int k = 0; k < sectionTops.Count; k++) if (-sectionTops[k].y <= top) idx = k;
                if (idx == shown) return;
                shown = idx;
                var s = sectionTops[idx];
                ctitle.text = s.title;
                ccount.text = Loc.F("collection.count", s.owned, s.total);
            }
            scroll.onValueChanged.AddListener(_ => SyncHeader());
            SyncHeader();

            // 5) Alt menü (Taşlar seçili, referanstaki altın kapsül).
            Tabs(page, TabCollection, back, back, null);
        }

        /// <summary>Kart: kazanılmış (ışıklı çerçeve + onay), kilitli (buzlu cam + kilit + soru), gizli hazine (siluet).</summary>
        /// <summary>v10: kazanılmış karta dokununca taş vitrini (3D model ya da büyük görsel). Kilitli kart açılmaz.</summary>
        public Action<string> StonePreviewRequested;

        private static void CollCard(RectTransform content, StoneItem it, Vector2 pos, Action<string> preview)
        {
            var card = UiFactory.Rect("Item_" + it.Id, content);
            UiFactory.Place(card, new Vector2(0.5f, 1f), pos, new Vector2(CollCardW, CollCardH));
            if (it.Owned && preview != null)
            {
                // Dokunma alanı (kaydırmayı engellemez: ScrollRect sürüklemeyi üst öğeye iletir).
                var hit = card.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                var btn = card.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                string id = it.Id;
                btn.onClick.AddListener(() => preview(id));
                PressFeedback.Attach(btn, null);
            }
            Color accent = it.Warm ? CollWarm : CollCool;
            if (it.Owned)
            {
                var glow = Decor("Frame", card, UiArt.Box);
                glow.type = Image.Type.Sliced;
                glow.color = accent;
                UiFactory.Stretch(glow.rectTransform, -4f, -4f, -4f, -4f);
                var fill = Decor("Fill", card, UiArt.Box);
                fill.type = Image.Type.Sliced;
                fill.color = it.Warm ? new Color32(0x4A, 0x2A, 0x6E, 0xFF) : new Color32(0x1E, 0x3C, 0x9A, 0xFF);
                UiFactory.Stretch(fill.rectTransform, 3f, 3f, 3f, 3f);
                var halo = Decor("Halo", card, SpecialArt.Glow);
                halo.color = new Color(accent.r, accent.g, accent.b, 0.45f);
                UiFactory.Place(halo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(260f, 220f));
            }
            else
            {
                var frost = Decor("Frost", card, UiArt.Box);
                frost.type = Image.Type.Sliced;
                frost.color = new Color32(0x4C, 0x70, 0xC8, 0x9C);
                UiFactory.Stretch(frost.rectTransform);
                if (CollArt("frost") == null)
                {
                    var edge = frost.gameObject.AddComponent<Outline>();
                    edge.effectColor = new Color(1f, 1f, 1f, 0.75f);
                    edge.effectDistance = new Vector2(3f, -3f);
                }
            }
            // Kullanıcı (2026-11-07): açılmamış taş tanınmamalı, üstü buzlu. Kilitli kartta önceden bulanıklaştırılmış
            // sürüm (yalnız renk lekesi) + buzlu örtü; gizli hazine karanlık siluet. Kazanılmış taş net.
            if (!it.Owned && !it.Hidden) KatKat.View.ResourceUsage.Note("Collection/stones_blur/" + it.Id);
            var blur = !it.Owned && !it.Hidden ? Resources.Load<Sprite>("Collection/stones_blur/" + it.Id) : null;
            var art = Decor("Icon", card, blur != null ? blur : it.Sprite);
            art.preserveAspect = true;
            art.color = it.Owned ? Color.white : it.Hidden ? new Color(0.12f, 0.10f, 0.20f, 0.55f) : blur != null ? new Color(1f, 1f, 1f, 0.85f) : new Color(1f, 1f, 1f, 0.55f);
            UiFactory.Place(art.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(206f, 200f));
            if (!it.Owned && !it.Hidden)
            {
                // Buzlu örtü: lekenin üstünde açık mavi buz camı (taş biçimi seçilmez).
                var ice = Decor("Ice", card, SpecialArt.Glow);
                ice.color = new Color(0.86f, 0.93f, 1f, 0.42f);
                UiFactory.Place(ice.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(250f, 230f));
            }
            if (!it.Owned)
            {
                // Buz kabuğu (final: Koleksiyon-Parcalari/07): taş siluetinin önünde; kilit, soru, ad ve koşulun arkasında.
                var frostArt = CollArt("frost");
                if (frostArt != null)
                {
                    var shellImg = Decor("FrostShell", card, frostArt);
                    UiFactory.Stretch(shellImg.rectTransform, -6f, -6f, -6f, -6f);
                }
                else
                {
                    var haze = Decor("Haze", card, UiArt.Box);
                    haze.type = Image.Type.Sliced;
                    haze.color = new Color(0.70f, 0.82f, 1f, 0.16f);
                    UiFactory.Place(haze.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(250f, 200f));
                }
                var lk = Decor("Lock", card, PremiumArt.V4("icons/lock") ?? UiArt.Icon(UiIcon.Back));
                lk.preserveAspect = true;
                UiFactory.Place(lk.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(62f, 62f));
                var q = Decor("Question", card, UiArt.Circle);
                q.color = new Color32(0x24, 0x2E, 0x54, 0xF0);
                UiFactory.Place(q.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(60f, 60f));
                var qo = q.gameObject.AddComponent<Outline>();
                qo.effectColor = new Color(1f, 1f, 1f, 0.85f); qo.effectDistance = new Vector2(2f, -2f);
                var qt = UiFactory.Text("Q", q.transform, "?", 40, Color.white);
                qt.raycastTarget = false;
                UiFactory.Stretch(qt.rectTransform);
            }
            else
            {
                var ok = Decor("Owned", card, CollArt("badge-check") ?? CheckBadgeTemp());
                ok.preserveAspect = true;
                UiFactory.Place(ok.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(64f, 64f));
            }
            if (it.Selected)
            {
                // Günlükte kullanılan (seçili) taş: sahiplikten ayrı küçük işaret.
                var sel = Decor("Active", card, UiArt.Circle);
                sel.color = new Color32(0x3E, 0xCC, 0xC4, 0xFF);
                UiFactory.Place(sel.rectTransform, new Vector2(0f, 1f), new Vector2(26f, -26f), new Vector2(26f, 26f));
            }
            var pill = Decor("NamePill", card, UiArt.Box);
            pill.type = Image.Type.Sliced;
            pill.color = CollPillGold;
            UiFactory.Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(250f, 50f));
            var po = pill.gameObject.AddComponent<Shadow>();
            po.effectColor = new Color(0.45f, 0.24f, 0.05f, 0.45f); po.effectDistance = new Vector2(0f, -4f);
            var n = UiFactory.Text("Name", pill.transform, it.Name, 29, CollInk);
            n.raycastTarget = false;
            UiFactory.Stretch(n.rectTransform, 12, 12, 2, 4);
            n.enableAutoSizing = true; n.fontSizeMax = 29; n.fontSizeMin = 15;
            var d = UiFactory.Text("Desc", card, it.Desc, 22, Color.white, TextAnchor.UpperCenter, FontStyle.Normal);
            d.raycastTarget = false;
            UiFactory.Place(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -305f), new Vector2(CollCardW - 26f, 84f));
            d.enableAutoSizing = true; d.fontSizeMax = 22; d.fontSizeMin = 13;
        }

        // ---- GEÇİCİ çizimler ------------------------------------------------------------------------------------

        private static Sprite _tmpCollBg, _tmpCollRibbon, _tmpCollPanel, _tmpCheck, _tmpDiamond;

        private static Sprite CollBgTemp()
        {
            if (_tmpCollBg) return _tmpCollBg;
            int w = 270, h = 600; // düşük çözünürlük, yumuşak geçiş
            var tex = NewTex(w, h, "coll_bg_tmp");
            var px = new Color32[w * h];
            Color sky = new Color32(0x6E, 0xB8, 0xF2, 0xFF), haze = new Color32(0x8E, 0x78, 0xD6, 0xFF), wall = new Color32(0x3E, 0x2A, 0x6E, 0xFF), wallDark = new Color32(0x2A, 0x1C, 0x4E, 0xFF);
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float y = (j + 0.5f) / h; // alttan
                Color c = y > 0.55f ? Color.Lerp(haze, sky, S(0.55f, 0.95f, y)) : Color.Lerp(wallDark, wall, S(0.0f, 0.55f, y));
                if (y < 0.55f)
                {
                    // Taş duvar: tuğla derzleri.
                    int row = (int)(y * 40f);
                    float u = (i + 0.5f) / w * 9f + (row % 2) * 0.5f;
                    bool joint = (y * 40f - row) < 0.08f || (u - Mathf.Floor(u)) < 0.03f;
                    if (joint) c = Color.Lerp(c, wallDark, 0.55f);
                }
                px[j * w + i] = c;
            }
            return _tmpCollBg = Finish(tex, px);
        }

        private static Sprite CollRibbonTemp()
        {
            if (_tmpCollRibbon) return _tmpCollRibbon;
            int w = 660, h = 128;
            var tex = NewTex(w, h, "coll_ribbon_tmp");
            var px = new Color32[w * h];
            Color purple = new Color32(0x6E, 0x34, 0xB2, 0xFF), purpleDark = new Color32(0x45, 0x1E, 0x7A, 0xFF), gold = new Color32(0xF4, 0xC2, 0x4E, 0xFF), goldDark = new Color32(0xC0, 0x82, 0x20, 0xFF);
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = i + 0.5f, y = j + 0.5f;
                Color c = new Color(0, 0, 0, 0);
                // Ana bant (90..570), uçlarda içe katlanmış kuyruklar (eğimli).
                float slant = (x < 330f ? 330f - x : x - 330f) / 330f;
                float top = 112f - 6f * slant, bottom = 22f + 10f * slant;
                bool tail = x < 100f || x > 560f;
                if (tail) { float t = x < 100f ? x : w - x; top = 92f; bottom = 8f + (100f - t) * 0.12f; if (t < 26f) { float notch = Mathf.Abs(y - 50f); if (notch < (26f - t) * 1.4f) continue; } }
                float d = Mathf.Min(y - bottom, top - y);
                if (d > -1f)
                {
                    float cov = Mathf.Clamp01(d + 0.5f);
                    Color fill = d < 7f ? Color.Lerp(goldDark, gold, d / 7f) : Color.Lerp(purpleDark, purple, S(0f, top - bottom, y - bottom));
                    if (tail && d >= 7f) fill = Color.Lerp(fill, purpleDark, 0.45f);
                    fill.a = cov;
                    c = fill;
                }
                px[j * w + i] = c;
            }
            return _tmpCollRibbon = Finish(tex, px);
        }

        /// <summary>9-dilim panel: mor-lacivert dolgu, çift altın kenar, yuvarlak köşeler (kontur esnemez).</summary>
        private static Sprite CollPanelTemp()
        {
            if (_tmpCollPanel) return _tmpCollPanel;
            const int n = 192, r = 56;
            var tex = NewTex(n, n, "coll_panel_tmp");
            var px = new Color32[n * n];
            Color gold = new Color32(0xF4, 0xC2, 0x4E, 0xFF), goldDark = new Color32(0xB8, 0x7A, 0x1C, 0xFF), fillTop = new Color32(0x4E, 0x2C, 0x86, 0xF4), fillBot = new Color32(0x26, 0x24, 0x6A, 0xF4);
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = i + 0.5f, y = j + 0.5f;
                float dx = Mathf.Max(0f, Mathf.Max(r - x, x - (n - r))), dy = Mathf.Max(0f, Mathf.Max(r - y, y - (n - r)));
                float dist = r - Mathf.Sqrt(dx * dx + dy * dy); // kenara içten uzaklık (köşede)
                float edgeIn = Mathf.Min(Mathf.Min(x, n - x), Mathf.Min(y, n - y));
                float d = (dx > 0f && dy > 0f) ? dist : Mathf.Min(edgeIn, dist);
                Color c = new Color(0, 0, 0, 0);
                if (d > -1f)
                {
                    float cov = Mathf.Clamp01(d + 0.5f);
                    Color fill;
                    if (d < 14f) fill = Color.Lerp(goldDark, new Color32(0xFF, 0xE2, 0x86, 0xFF), Mathf.Min(d, 14f - d) / 7f);
                    else if (d < 18f) fill = new Color32(0x3A, 0x1E, 0x60, 0xFF);
                    else if (d < 24f) fill = Color.Lerp(gold, goldDark, (d - 18f) / 6f);
                    else fill = Color.Lerp(fillBot, fillTop, y / n);
                    fill.a *= cov;
                    c = fill;
                }
                px[j * n + i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            return _tmpCollPanel = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 12, r + 12, r + 12, r + 12));
        }

        private static Sprite CheckBadgeTemp()
        {
            if (_tmpCheck) return _tmpCheck;
            const int n = 96;
            var tex = NewTex(n, n, "badge_check_tmp");
            var px = new Color32[n * n];
            Color green = new Color32(0x3C, 0xC0, 0x4A, 0xFF), greenDark = new Color32(0x1E, 0x84, 0x2C, 0xFF), rim = Color.white;
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                var p = new Vector2(i + 0.5f, j + 0.5f) - new Vector2(n / 2f, n / 2f);
                float r = p.magnitude;
                Color c = new Color(0, 0, 0, 0);
                if (r < 46f)
                {
                    c = r > 40f ? rim : Color.Lerp(green, greenDark, (p.y + 40f) < 30f ? 0.5f : 0f);
                    c.a = Mathf.Clamp01(46f - r);
                    // Onay işareti: iki kalın çizgi.
                    float d1 = SegDist(p, new Vector2(-18f, 2f), new Vector2(-5f, -12f));
                    float d2 = SegDist(p, new Vector2(-5f, -12f), new Vector2(20f, 16f));
                    if (Mathf.Min(d1, d2) < 6.5f && r <= 40f) c = Color.white;
                }
                px[j * n + i] = c;
            }
            return _tmpCheck = Finish(tex, px);
        }

        private static Sprite DiamondBadgeTemp()
        {
            if (_tmpDiamond) return _tmpDiamond;
            const int n = 96;
            var tex = NewTex(n, n, "badge_diamond_tmp");
            var px = new Color32[n * n];
            Color gold = new Color32(0xF4, 0xC2, 0x4E, 0xFF), pink = new Color32(0xF2, 0x5E, 0xD8, 0xFF), pinkDark = new Color32(0xA0, 0x2E, 0xB8, 0xFF);
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.Abs(i + 0.5f - n / 2f), y = Mathf.Abs(j + 0.5f - n / 2f);
                float d = 44f - (x * 0.8f + y);
                Color c = new Color(0, 0, 0, 0);
                if (d > -1f)
                {
                    c = d < 8f ? gold : Color.Lerp(pinkDark, pink, (i + j) / (2f * n) + 0.2f);
                    c.a = Mathf.Clamp01(d + 0.5f);
                }
                px[j * n + i] = c;
            }
            return _tmpDiamond = Finish(tex, px);
        }

        private static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude;
        }
    }
}
