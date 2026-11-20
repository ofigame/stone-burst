using System;
using KatKat.Meta;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// Güçlendiriciler (2026-11-20; kullanıcı: Candy Crush'taki alt alan gibi, "+3 Hamle", "Taş Değiştir", "Bomba"; külçeyle
    /// alınır). Alt şeritte tepsinin (oyunda kazanılan roket/Altın Rulo) sağında; tepsiden ayrılsın diye turkuaz pirinç
    /// diskli. Sağ altta adet rozeti (0'da "+", dokununca satın alma penceresi), açılmamışta kilit. İlk güçlendirici
    /// açılana kadar (bölüm 11) hiç görünmez. Şerit kalabalıkta bütün öğeler birlikte küçülür (<see cref="LayoutBottom"/>).
    /// </summary>
    public sealed partial class HudView
    {
        public event Action<Booster> BoosterPressed;

        private readonly Button[] _boosters = new Button[3];
        private readonly Image[] _boosterIcons = new Image[3], _boosterLocks = new Image[3], _boosterSel = new Image[3];
        private readonly Text[] _boosterCounts = new Text[3];
        private readonly Image[] _boosterBadges = new Image[3];
        private RectTransform _boosterRoot;
        private Image _boosterPlate;
        private int _boosterSelected = -1;
        private float _bottomScale = 1f, _boosterScale = 1f;
        private const float BoosterSize = 140f; // 2026-11-20 (kullanıcı: "iconlar küçük … büyütebilir miyiz"): 118 → 140

        private void BuildBoosters(RectTransform bottom)
        {
            _boosterRoot = UiFactory.Rect("Boosters", bottom);
            UiFactory.Stretch(_boosterRoot);
            // 2026-11-20 (kullanıcı: güçlendiricileri "candy crush görselindeki gibi bir zemin üzerine" → "yalnız güçlendirici altına"):
            // fildişi seramik plaka, pirinç kenar; konum ve boy LayoutBottom'da düğmelere göre.
            _boosterPlate = MenuView.U3Plate("BoosterPlate", _boosterRoot, 40f, 150f);
            _boosterPlate.raycastTarget = false;
            _boosterPlate.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                var b = ShopCatalog.All[i];
                var btn = UiFactory.SpriteButton("Booster_" + ShopCatalog.Key(b), _boosterRoot, Ui3.Art("ikon-dugmesi") ?? UiArt.Circle,
                    new Vector2(BoosterSize, BoosterSize), () => BoosterPressed?.Invoke(b));
                btn.GetComponent<Image>().preserveAspect = true;
                UiFactory.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(BoosterSize, BoosterSize));
                var sel = UiFactory.Image("Sel", btn.transform, SpecialArt.Glow, new Color(1f, 0.85f, 0.35f, 0f));
                sel.raycastTarget = false;
                UiFactory.Place(sel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * BoosterSize * 1.9f);
                sel.transform.SetAsFirstSibling();
                var icon = UiFactory.Image("Icon", btn.transform, MenuView.ShopArt(ShopCatalog.Key(b)), Color.white);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), Vector2.one * BoosterSize * 1.02f);
                var lk = UiFactory.Image("Lock", btn.transform, PremiumArt.V4("icons/lock") ?? UiArt.Icon(UiIcon.Back), Color.white);
                lk.preserveAspect = true;
                lk.raycastTarget = false;
                UiFactory.Place(lk.rectTransform, new Vector2(1f, 0f), new Vector2(-16f, 18f), new Vector2(52f, 52f));
                var badge = UiFactory.Image("CountBadge", btn.transform, Ui3.Rounded(24f, Ui3.TealTop, Ui3.TealBottom, 3f, Ui3.Brass), Color.white, true);
                badge.pixelsPerUnitMultiplier = 1f;
                badge.raycastTarget = false;
                UiFactory.Place(badge.rectTransform, new Vector2(1f, 0f), new Vector2(-12f, 14f), new Vector2(54f, 48f));
                var cnt = UiFactory.Title("Count", badge.transform, "0", 30, Color.white, Ui3.TealPressed);
                cnt.raycastTarget = false;
                UiFactory.Stretch(cnt.rectTransform, 2f, 2f, 2f, 4f);
                _boosters[i] = btn;
                _boosterIcons[i] = icon;
                _boosterLocks[i] = lk;
                _boosterSel[i] = sel;
                _boosterBadges[i] = badge;
                _boosterCounts[i] = cnt;
                btn.gameObject.SetActive(false);
            }
        }

        /// <param name="visible">En az biri açılmış (bölüm ≥ 11) ve normal bölüm.</param>
        /// <param name="counts">Adetler (Booster sırasıyla).</param>
        /// <param name="unlocked">Açık mı (kilitli olanda kilit, dokununca açılış bölümü söylenir).</param>
        /// <param name="selected">Hedef seçimi bekleyen güçlendirici (Bomba, Taş Değiştir); -1 yok.</param>
        public void SetBoosters(bool visible, int[] counts, bool[] unlocked, int selected)
        {
            if (_boosterRoot == null) return;
            bool changed = false;
            _boosterSelected = selected;
            for (int i = 0; i < 3; i++)
            {
                var btn = _boosters[i];
                if (btn.gameObject.activeSelf != visible) { btn.gameObject.SetActive(visible); changed = true; }
                if (_boosterPlate != null) _boosterPlate.gameObject.SetActive(visible);
                if (!visible) continue;
                bool open = unlocked != null && unlocked[i];
                int n = counts != null ? counts[i] : 0;
                _boosterLocks[i].gameObject.SetActive(!open);
                _boosterBadges[i].gameObject.SetActive(open);
                _boosterCounts[i].text = n > 0 ? n.ToString() : "+";
                _boosterIcons[i].color = open ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                ((Image)btn.targetGraphic).color = open ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                bool sel = i == selected;
                _boosterSel[i].color = new Color(1f, 0.85f, 0.35f, sel ? 0.95f : 0f);
                btn.transform.localScale = Vector3.one * _boosterScale * (sel ? TraySelScale : 1f);
            }
            if (changed) LayoutBottom();
        }

        /// <summary>Testler: güçlendirici düğmesi görünür mü, rozet metni.</summary>
        public bool BoostersShown => _boosters[0] != null && _boosters[0].gameObject.activeSelf;
        public string BoosterCountText(int i) => _boosterCounts[i] != null ? _boosterCounts[i].text : null;
        public void PressBoosterForTest(Booster b) => _boosters[(int)b].onClick.Invoke();

        // ---- "Bomba bitti" penceresi (tasarım docs/yeni_v2 "Bomba Bitti_ Güçlendirici Mağazası") ---------------------

        private RectTransform _offerCard;
        public bool BoosterOfferShown => _offerCard != null;

        /// <summary>Güçlendirici bitti: simge, açıklama, külçe bakiyesi, "[külçe] 60 ile al" ve "Mağazaya git".</summary>
        public void ShowBoosterOffer(Booster b, int ingots, Action buy, Action goShop)
        {
            CloseBoosterOffer();
            const float W = 860f, H = 860f;
            var card = MenuView.U3Modal(_safe, "BoosterOfferCard", W, H, CloseBoosterOffer, out var root);
            _offerCard = root;
            var icon = UiFactory.Image("Icon", card, MenuView.ShopArt(ShopCatalog.Key(b)), Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            MenuView.U3In(icon.rectTransform, W, 300f, 40f, 260f, 240f);
            var title = UiFactory.Text("Title", card, Loc.T("booster." + ShopCatalog.Key(b) + ".empty"), 56, Ui3.Ink);
            title.raycastTarget = false;
            MenuView.U3In(title.rectTransform, W, 56f, 292f, 748f, 84f);
            title.enableAutoSizing = true; title.fontSizeMax = 56; title.fontSizeMin = 30;
            var info = UiFactory.Text("Info", card, Loc.T("booster." + ShopCatalog.Key(b) + ".desc"), 32, Ui3.InkSecondary, TextAnchor.MiddleCenter, FontStyle.Normal);
            info.raycastTarget = false;
            info.textWrappingMode = TMPro.TextWrappingModes.Normal;
            MenuView.U3In(info.rectTransform, W, 70f, 384f, 720f, 100f);
            info.enableAutoSizing = true; info.fontSizeMax = 32; info.fontSizeMin = 20;
            var bal = MenuView.U3Plate("Balance", card, 36f, 76f);
            MenuView.U3In(bal.rectTransform, W, 300f, 498f, 260f, 76f);
            var bi = UiFactory.Image("Ingot", bal.transform, MenuView.ShopArt("ingot"), Color.white);
            bi.preserveAspect = true;
            bi.raycastTarget = false;
            UiFactory.Place(bi.rectTransform, new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(60f, 60f));
            var bt = UiFactory.Text("Value", bal.transform, ingots.ToString(), 42, Ui3.Ink);
            bt.raycastTarget = false;
            UiFactory.Place(bt.rectTransform, new Vector2(0f, 0.5f), new Vector2(170f, 0f), new Vector2(150f, 60f));
            int price = ShopCatalog.Price(b);
            var buyBtn = Ui1.Button("Buy", card, Loc.F("booster.buy", price), true, new Vector2(0.5f, 1f), new Vector2(0f, -(600f + 56f)), new Vector2(748f, 112f),
                () => { CloseBoosterOffer(); buy?.Invoke(); }, MenuView.ShopArt("ingot"), 44);
            if (ingots < price) buyBtn.gameObject.AddComponent<CanvasGroup>().alpha = 0.6f;
            Ui1.Button("Shop", card, Loc.T("booster.goshop"), false, new Vector2(0.5f, 1f), new Vector2(0f, -(730f + 46f)), new Vector2(748f, 92f),
                () => { CloseBoosterOffer(); goShop?.Invoke(); }, null, 38);
        }

        public void CloseBoosterOffer()
        {
            if (_offerCard != null) UiFactory.Kill(_offerCard.gameObject);
            _offerCard = null;
        }

        public void PressBoosterOfferForTest(bool buy)
        {
            if (_offerCard == null) return;
            _offerCard.Find("Card/" + (buy ? "Buy" : "Shop")).GetComponent<Button>().onClick.Invoke();
        }
    }
}
