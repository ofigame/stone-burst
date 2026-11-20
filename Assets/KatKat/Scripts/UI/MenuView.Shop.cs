using System;
using System.Collections.Generic;
using KatKat.Meta;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// Mağaza (2026-11-20; kullanıcı: "oyunun mağazası olacak", tasarım docs/yeni_v2 "Renkli Fantastik Oyun Mağazası UI"):
    /// üstte külçe kapsülü (haritada yıldızın yerinde), alt menüde "Mağaza" sekmesi (Taşlar sekmesi kalktı; koleksiyona
    /// taş dolum göstergesinden girilir). Sayfa: öne çıkanlar (başlangıç paketi, reklamsız oyna), güçlendiriciler (külçeyle),
    /// külçe paketleri (gerçek para; <see cref="ShopCatalog.IapEnabled"/> kapalıyken "Yakında").
    /// </summary>
    public sealed partial class MenuView
    {
        public const int TabShop = 6;

        /// <summary>Oyuncunun külçesi (üst kapsül ve mağaza).</summary>
        public Func<int> IngotCount;
        /// <summary>Külçe kapsülü ve "Mağaza" sekmesi.</summary>
        public Action ShopPressed;
        /// <summary>Koleksiyonda alınmayı bekleyen taş hediyesi var mı (taş dolum göstergesinde işaret).</summary>
        public Func<bool> GiftPending;
        /// <summary>Koleksiyon kartındaki "Taştan hediyeni al".</summary>
        public Action<string> ClaimGiftPressed;
        /// <summary>Mağazada güçlendirici satın al (adet 1 ya da 3).</summary>
        public Action<Booster, int> BuyBoosterPressed;
        /// <summary>Gerçek para ürünü (paket, başlangıç, reklamsız). Satın alma açık değilken çağrılmaz.</summary>
        public Action<string> BuyProductPressed;
        /// <summary>Mağazada ya da kalp kartında SB Gold ile kalp teklifi (2026-11-20).</summary>
        public Action<HeartOffer> BuyHeartsPressed;

        public struct ShopModel
        {
            public int Ingots, LevelNumber;
            public int[] Owned; // Booster sırasıyla
            public bool NoAds, StarterBought;
        }

        private readonly List<Text> _ingotLabels = new List<Text>();
        private RectTransform _shopPage;

        public bool IsShopShown => _shopPage != null && _page == _shopPage;

        internal static Sprite ShopArt(string key)
        {
            KatKat.View.ResourceUsage.Note("Shop/" + key);
            return Resources.Load<Sprite>("Shop/" + key);
        }

        /// <summary>Külçe kapsülü 208×88 (yıldız kapsülünün yerinde): külçe simgesi, sayı; dokununca mağaza.</summary>
        private void U3IngotCapsule(RectTransform parent, float x, float y)
        {
            var cap = U3Plate("IngotCapsule", parent, 44f, 88f);
            U3Place(cap.rectTransform, x, y, 208f, 88f);
            var icon = Decor("Ingot", cap.transform, ShopArt("ingot") ?? PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            icon.preserveAspect = true;
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(52f, 2f), new Vector2(76f, 76f));
            Twinkle.Attach(icon.rectTransform, 2, 30f, 0.55f); // 2026-11-20: külçede ışıltılı yıldızlar
            var n = U3Text("Ingots", cap.transform, (IngotCount?.Invoke() ?? 0).ToString(), 50, true, Ui3.Ink);
            UiFactory.Place(n.rectTransform, new Vector2(0f, 0.5f), new Vector2(144f, 0f), new Vector2(112f, 62f));
            FitText(n, 28);
            _ingotLabels.RemoveAll(t => t == null);
            _ingotLabels.Add(n);
            var btn = cap.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => ShopPressed?.Invoke());
            PressFeedback.Attach(btn, null, 70f, 110f);
        }

        /// <summary>Külçe değişti: görünen bütün kapsülleri günceller.</summary>
        public void RefreshIngots()
        {
            int v = IngotCount?.Invoke() ?? 0;
            _ingotLabels.RemoveAll(t => t == null);
            foreach (var t in _ingotLabels) t.text = v.ToString();
        }

        /// <summary>Testler: görünen külçe sayısı (kapsül yoksa -1).</summary>
        public int IngotShownForTest
        {
            get
            {
                _ingotLabels.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);
                return _ingotLabels.Count > 0 && int.TryParse(_ingotLabels[_ingotLabels.Count - 1].text, out var v) ? v : -1;
            }
        }

        // ---- Mağaza sayfası ------------------------------------------------------------------------------------------

        public void ShowShop(ShopModel m, Action back)
        {
            // 2026-11-20 (kullanıcı: "mağazanın arkaplanını bulanık yapalım biraz"): harita görselinin bulanık kopyası; tema
            // henüz yoksa (haritadan önce açılırsa) düz.
            var mapBg = Theme != null ? Theme.MapSprite : null;
            var page = NewPage(mapBg != null ? (KatKat.View.BlurredBackdrop.For(mapBg) ?? mapBg) : null);
            _shopPage = page;

            var bb = ShellRoundButton("Back", page, PremiumArt.V4("icons/back") ?? UiArt.Icon(UiIcon.Back), 110f, back);
            UiFactory.Place((RectTransform)bb.transform, new Vector2(0f, 1f), new Vector2(83f, -78f), new Vector2(110f, 110f));
            var tp = U3Plate("TitlePlate", page, 44f, 96f);
            U3Place(tp.rectTransform, 330f, 30f, 420f, 96f);
            var title = U3Text("Title", tp.transform, Loc.T("shop.title"), 54, true, Ui3.Ink);
            UiFactory.Stretch(title.rectTransform, 20f, 20f, 4f, 8f);
            FitText(title, 30);
            U3IngotCapsule(page, 840f, 34f);

            var viewport = UiFactory.Rect("Viewport", page);
            viewport.anchorMin = new Vector2(0f, 0f); viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(24f, ShellBarH + 12f);
            viewport.offsetMax = new Vector2(-24f, -150f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var content = UiFactory.Rect("Content", viewport);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);

            const float W = 1000f;
            float y = -10f;

            // 1) Öne çıkanlar: başlangıç paketi (tek sefer) ve reklamsız oyna.
            const float FeatW = 490f, FeatH = 330f;
            if (!m.StarterBought)
                FeatureCard(content, "Starter", new Vector2(-(FeatW + 20f) * 0.5f, y - FeatH * 0.5f), FeatW, FeatH, ShopArt("starter"),
                    Loc.T("shop.starter"), Loc.F("shop.starter.desc", ShopCatalog.StarterIngots), Loc.T("shop.once"), false, ShopCatalog.StarterId);
            FeatureCard(content, "NoAds", new Vector2(m.StarterBought ? 0f : (FeatW + 20f) * 0.5f, y - FeatH * 0.5f), m.StarterBought ? W : FeatW, FeatH,
                ShopArt("noads"), Loc.T("shop.noads"), Loc.F("shop.noads.desc", ShopCatalog.NoAdsIngots), null, m.NoAds, ShopCatalog.NoAdsId);
            y -= FeatH + 30f;

            // 1b) Kalpler (2026-11-20; kullanıcı: "mağazada can da satın alınsın"): doldur, sınırsız 1 saat, sınırsız 3 saat.
            y = SectionTitle(content, Loc.T("hearts.title"), y);
            const float HW = 320f, HH = 420f;
            for (int i = 0; i < ShopCatalog.HeartOffers.Length; i++)
                HeartCard(content, ShopCatalog.HeartOffers[i], new Vector2((i - 1) * (HW + 20f), y - HH * 0.5f), HW, HH, m.Ingots);
            y -= HH + 30f;

            // 2) Güçlendiriciler.
            y = SectionTitle(content, Loc.T("shop.boosters"), y);
            const float BW = 320f, BH = 470f;
            for (int i = 0; i < ShopCatalog.All.Length; i++)
            {
                var b = ShopCatalog.All[i];
                BoosterCard(content, b, new Vector2((i - 1) * (BW + 20f), y - BH * 0.5f), BW, BH,
                    m.Owned != null && i < m.Owned.Length ? m.Owned[i] : 0, m.Ingots, Wallet.Unlocked(b, m.LevelNumber));
            }
            y -= BH + 30f;

            // 3) Külçe paketleri (2×2).
            y = SectionTitle(content, Loc.T("shop.packs"), y);
            const float PW = 490f, PH = 300f;
            for (int i = 0; i < ShopCatalog.Packs.Length; i++)
            {
                var p = ShopCatalog.Packs[i];
                float px = (i % 2 == 0 ? -1f : 1f) * (PW + 20f) * 0.5f;
                float py = y - (i / 2) * (PH + 20f) - PH * 0.5f;
                PackCard(content, p, new Vector2(px, py), PW, PH, i == 1 ? Loc.T("shop.popular") : i == 3 ? Loc.T("shop.bestvalue") : null);
            }
            y -= 2f * PH + 20f + 30f;

            // 4) Alt not.
            var note = U3Text("Legal", content, Loc.T(ShopCatalog.IapEnabled ? "shop.legal" : "shop.soon.note"), 26, false, Ui3.InkSecondary);
            note.textWrappingMode = TMPro.TextWrappingModes.Normal;
            UiFactory.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 50f), new Vector2(W - 40f, 100f));
            FitText(note, 18);
            y -= 120f;

            content.sizeDelta = new Vector2(W, -y + 20f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 60f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;

            Tabs(page, TabShop, back, back, null);
        }

        private float SectionTitle(RectTransform content, string text, float y)
        {
            var plate = U3Plate("Section", content, 30f, 76f);
            UiFactory.Place(plate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 38f), new Vector2(460f, 76f));
            var t = U3Text("Text", plate.transform, text, 40, true, Ui3.Ink);
            UiFactory.Stretch(t.rectTransform, 16f, 16f, 2f, 6f);
            FitText(t, 24);
            return y - 100f;
        }

        private Image ShopCard(RectTransform content, string name, Vector2 pos, float w, float h)
        {
            var card = U3Plate(name, content, 36f, h);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 1f), pos, new Vector2(w, h));
            return card;
        }

        private void FeatureCard(RectTransform content, string name, Vector2 pos, float w, float h, Sprite art, string title, string desc, string tag, bool owned, string productId)
        {
            var card = ShopCard(content, name, pos, w, h);
            var ct = card.rectTransform;
            var icon = Decor("Icon", ct, art);
            icon.preserveAspect = true;
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(130f, -120f), new Vector2(210f, 190f));
            Twinkle.Attach(icon.rectTransform, 4, 46f, 0.5f);
            var t = U3Text("Name", ct, title, 40, true, Ui3.Ink, TextAnchor.MiddleLeft);
            t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            UiFactory.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(240f + (w - 260f) * 0.5f, -62f), new Vector2(w - 260f, 90f));
            FitText(t, 24);
            var d = U3Text("Desc", ct, desc, 26, false, Ui3.InkSecondary, TextAnchor.UpperLeft);
            d.textWrappingMode = TMPro.TextWrappingModes.Normal;
            UiFactory.Place(d.rectTransform, new Vector2(0f, 1f), new Vector2(240f + (w - 260f) * 0.5f, -158f), new Vector2(w - 260f, 100f));
            FitText(d, 16);
            if (tag != null) Ribbon(ct, tag, true);
            string label = owned ? Loc.T("shop.owned") : Loc.T("shop.soon");
            var btn = Ui1.Button("Buy", ct, label, !owned && ShopCatalog.IapEnabled, new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(w - 60f, 88f),
                owned || !ShopCatalog.IapEnabled ? (Action)null : () => BuyProductPressed?.Invoke(productId), null, 36);
            btn.interactable = !owned && ShopCatalog.IapEnabled;
        }

        private void BoosterCard(RectTransform content, Booster b, Vector2 pos, float w, float h, int owned, int ingots, bool unlocked)
        {
            var card = ShopCard(content, "Booster_" + ShopCatalog.Key(b), pos, w, h);
            var ct = card.rectTransform;
            var icon = Decor("Icon", ct, ShopArt(ShopCatalog.Key(b)));
            icon.preserveAspect = true;
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(180f, 180f));
            if (!unlocked) icon.color = new Color(1f, 1f, 1f, 0.45f);
            var badge = U3Text("Count", ct, "×" + owned, 32, true, Ui3.OnTeal);
            var bg = UiFactory.Image("CountBg", ct, Ui3.Rounded(22f, Ui3.TealTop, Ui3.TealBottom, 2f, Ui3.Brass), Color.white, true);
            bg.pixelsPerUnitMultiplier = 1f;
            bg.raycastTarget = false;
            UiFactory.Place(bg.rectTransform, new Vector2(1f, 1f), new Vector2(-52f, -40f), new Vector2(84f, 52f));
            badge.transform.SetParent(bg.transform, false);
            UiFactory.Stretch(badge.rectTransform, 4f, 4f, 2f, 4f);
            var n = U3Text("Name", ct, Loc.T("booster." + ShopCatalog.Key(b)), 36, true, Ui3.Ink);
            UiFactory.Place(n.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(w - 30f, 50f));
            FitText(n, 22);
            var d = U3Text("Desc", ct, Loc.T("booster." + ShopCatalog.Key(b) + ".desc"), 24, false, Ui3.InkSecondary);
            d.textWrappingMode = TMPro.TextWrappingModes.Normal;
            UiFactory.Place(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(w - 36f, 64f));
            FitText(d, 15);
            if (!unlocked)
            {
                var lk = Decor("Lock", ct, PremiumArt.V4("icons/lock") ?? UiArt.Icon(UiIcon.Back));
                lk.preserveAspect = true;
                UiFactory.Place(lk.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(76f, 76f));
                var u = U3Text("Unlock", ct, Loc.F("shop.unlock", ShopCatalog.UnlockLevel(b)), 28, true, Ui3.InkSecondary);
                UiFactory.Place(u.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(w - 30f, 60f));
                FitText(u, 18);
                return;
            }
            IngotButton(ct, "Buy1", "", ShopCatalog.Price(b), ingots, new Vector2(0f, 116f), new Vector2(w - 90f, 62f), () => BuyBoosterPressed?.Invoke(b, 1));
            IngotButton(ct, "Buy3", "3× ", ShopCatalog.Price3(b), ingots, new Vector2(0f, 44f), new Vector2(w - 90f, 62f), () => BuyBoosterPressed?.Invoke(b, 3));
        }

        private void HeartCard(RectTransform content, HeartOffer o, Vector2 pos, float w, float h, int ingots)
        {
            var card = ShopCard(content, "Heart_" + ShopCatalog.HeartKey(o), pos, w, h);
            var ct = card.rectTransform;
            var heart = Decor("Icon", ct, HeartSprite());
            heart.preserveAspect = true;
            UiFactory.Place(heart.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(150f, 140f));
            var n = HeartNumber(heart.transform, o == HeartOffer.Refill ? 64 : 72);
            Loc.Set(n, o == HeartOffer.Refill ? Hearts.Max.ToString() : "∞");
            heart.gameObject.AddComponent<IconBreath>();
            Twinkle.Attach(heart.rectTransform, 3, 40f, 0.5f, new Color(1f, 0.85f, 0.9f, 1f));
            var t = U3Text("Name", ct, Loc.T("heart.offer." + ShopCatalog.HeartKey(o)), 32, true, Ui3.Ink);
            t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(w - 30f, 80f));
            FitText(t, 20);
            var d = U3Text("Desc", ct, Loc.T(o == HeartOffer.Refill ? "heart.offer.refill.desc" : "heart.offer.inf.desc"), 24, false, Ui3.InkSecondary);
            d.textWrappingMode = TMPro.TextWrappingModes.Normal;
            UiFactory.Place(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(w - 36f, 64f));
            FitText(d, 15);
            IngotButton(ct, "Buy", "", ShopCatalog.HeartPrice(o), ingots, new Vector2(0f, 44f), new Vector2(w - 90f, 62f), () => BuyHeartsPressed?.Invoke(o));
        }

        /// <summary>
        /// Külçe fiyatlı düğme: [külçe] fiyat; yeşil gradyan, büyüyüp küçülür (2026-11-20). Yetmiyorsa soluk (basınca yine
        /// çağrılır: denetleyici "Külçe al" der).
        /// </summary>
        private void IngotButton(RectTransform parent, string name, string prefix, int price, int ingots, Vector2 pos, Vector2 size, Action act)
        {
            bool afford = ingots >= price;
            var btn = Ui1.PriceButton(name, parent, prefix + price, new Vector2(0.5f, 0f), pos, size, act, ShopArt("ingot"), 34);
            if (!afford)
            {
                var cg = btn.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0.6f;
            }
        }

        private void PackCard(RectTransform content, IngotPack p, Vector2 pos, float w, float h, string ribbon)
        {
            var card = ShopCard(content, "Pack_" + p.ProductId, pos, w, h);
            var ct = card.rectTransform;
            var icon = Decor("Icon", ct, ShopArt(p.Art));
            icon.preserveAspect = true;
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(130f, 6f), new Vector2(210f, 210f));
            Twinkle.Attach(icon.rectTransform, 4 + Array.IndexOf(ShopCatalog.Packs, p), 50f, 0.5f); // büyük pakette daha çok yıldız
            var amt =U3Text("Amount", ct, p.Ingots.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")), 50, true, Ui3.Ink);
            UiFactory.Place(amt.rectTransform, new Vector2(1f, 1f), new Vector2(-130f, -64f), new Vector2(210f, 70f));
            FitText(amt, 30);
            var ing = Decor("AmountIcon", ct, ShopArt("ingot"));
            ing.preserveAspect = true;
            UiFactory.Place(ing.rectTransform, new Vector2(1f, 1f), new Vector2(-250f, -64f), new Vector2(56f, 56f));
            Twinkle.Attach(ing.rectTransform, 1, 26f, 0.5f);
            if (p.BonusPercent > 0)
            {
                var tag = UiFactory.Image("Bonus", ct, Ui3.Rounded(20f, Ui3.Success, Ui3.Success, 0f, default), Color.white, true);
                tag.pixelsPerUnitMultiplier = 1f;
                tag.raycastTarget = false;
                UiFactory.Place(tag.rectTransform, new Vector2(1f, 1f), new Vector2(-130f, -128f), new Vector2(120f, 44f));
                var tt = U3Text("Text", tag.transform, "+%" + p.BonusPercent, 28, true, Ui3.OnTeal);
                UiFactory.Stretch(tt.rectTransform, 4f, 4f, 2f, 4f);
            }
            var btn = Ui1.Button("Buy", ct, Loc.T("shop.soon"), ShopCatalog.IapEnabled, new Vector2(1f, 0f), new Vector2(-130f, 62f), new Vector2(220f, 84f),
                ShopCatalog.IapEnabled ? (Action)(() => BuyProductPressed?.Invoke(p.ProductId)) : null, null, 32);
            btn.interactable = ShopCatalog.IapEnabled;
            if (ribbon != null) Ribbon(ct, ribbon, false);
        }

        /// <summary>Kartın sol üst köşesinde eğik şerit ("Tek seferlik", "En popüler").</summary>
        private static void Ribbon(RectTransform card, string text, bool coral)
        {
            var r = UiFactory.Image("Ribbon", card, Ui3.Rounded(14f, coral ? Ui3.Coral : Ui3.Brass, coral ? Ui3.Coral : Ui3.Brass, 0f, default), Color.white, true);
            r.pixelsPerUnitMultiplier = 1f;
            r.raycastTarget = false;
            UiFactory.Place(r.rectTransform, new Vector2(0f, 1f), new Vector2(92f, -30f), new Vector2(210f, 46f));
            r.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            var t = U3Text("Text", r.transform, text, 26, true, Ui3.OnTeal);
            UiFactory.Stretch(t.rectTransform, 8f, 8f, 2f, 4f);
            FitText(t, 16);
        }

    }
}
