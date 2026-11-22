using System;
using KatKat.Core;
using KatKat.View;
using UnityEngine;

namespace KatKat.UI
{
    /// <summary>
    /// Canlı maç girişi (kullanıcı 2026-11-13: "Mevcut oyunumuzun anasayfasında sıralama butonunun altına koyalım girişi oradan
    /// yapalım canlı oyuna"). Sıralama düğmesiyle aynı yuvarlak düğme ve lacivert kapsül; simge iki karşılıklı taş.
    /// </summary>
    public sealed partial class MenuView
    {
        public Action LivePressed;

        /// <summary>Sıralama düğmesinin bir sıra altı (sol sütun).</summary>
        internal const float LiveButtonDrop = 214f;

        private void BuildLiveButton(RectTransform shell)
        {
            if (LivePressed == null) return;
            var badge = BadgeArt("rozet-canli");
            if (badge != null)
            {
                BadgeButton("LiveButton", shell, badge, BadgeSlotY(2), Loc.T("live.button"), false, 26, () => LivePressed?.Invoke(), out _, new Color(0.25f, 0.8f, 1f, 1f));
                return;
            }
            var chip = PremiumArt.Chip(LayerColor.Blue) ?? UiArt.Chip(LayerColor.Blue);
            var b = ShellRoundButton("LiveButton", shell, chip, 132f, () => LivePressed?.Invoke());
            UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(-V11ColumnX, -V11ColumnZpeTop - V11LeftShift - 66f - LiveButtonDrop), new Vector2(132f, 132f));
            // İkinci taş (rakip): sağ altta, küçük ve mercan; "karşılıklı" okunsun.
            var icon = b.transform.Find("Icon") as RectTransform;
            if (icon != null)
            {
                icon.offsetMin = new Vector2(132f * 0.17f, 132f * 0.39f);
                icon.offsetMax = new Vector2(-132f * 0.39f, -132f * 0.17f);
                var other = Decor("Rival", b.transform, PremiumArt.Chip(LayerColor.Coral) ?? UiArt.Chip(LayerColor.Coral));
                other.preserveAspect = true;
                UiFactory.Stretch(other.rectTransform, 132f * 0.39f, 132f * 0.17f, 132f * 0.39f, 132f * 0.17f);
            }
            var cap = WhiteCapsule("LiveLabel", b.transform, V11Navy, new Vector2(0.5f, 0f), new Vector2(0f, -32f), new Vector2(190f, 54f), 5f);
            cap.raycastTarget = true;
            var t = UiFactory.Title("Text", cap.transform, Loc.T("live.button"), 32, Color.white, new Color32(0x06, 0x1A, 0x44, 0xFF));
            t.raycastTarget = false;
            UiFactory.Stretch(t.rectTransform, 14f, 14f, 2f, 4f);
            FitText(t, 20);
        }
        private RectTransform _liveIntro;

        /// <summary>Testler: canlı maç bilgi kartı açık mı.</summary>
        public bool LiveIntroShown => _liveIntro != null;

        /// <summary>
        /// Canlı maç bilgi kartı (kullanıcı 2026-11-22: "butona bastığımız gibi maça girmesin. Pop-up çıksın canlı maç hakkında
        /// bilgi versin hazırım butonu olsun"): başlık, simge, kısa kurallar ve "Hazırım"; dışına dokunmak kapatır.
        /// </summary>
        public void ShowLiveIntro(Action ready)
        {
            CloseLiveIntro();
            if (_page == null) { ready?.Invoke(); return; }
            const float W = 860f, H = 860f;
            var card = U3Modal(_page, "LiveIntroCard", W, H, CloseLiveIntro, out var root);
            _liveIntro = root;
            LiveTabsOn(); // 2026-11-22: alt menüde Canlı Maç seçili (diğer sekmeler gibi)
            var title = U3Text("Title", card, Loc.T("live.title"), 52, true, Ui3.Ink);
            U3In(title.rectTransform, W, 70f, 46f, 720f, 90f);
            FitText(title, 28);
            var art = BadgeArt("sekme-canli");
            if (art != null)
            {
                var ic = UiFactory.Image("Icon", card, art, Color.white);
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                U3In(ic.rectTransform, W, 310f, 150f, 240f, 200f);
            }
            var info = U3Text("Info", card, Loc.T("live.intro"), 32, false, Ui3.InkSecondary);
            info.textWrappingMode = TMPro.TextWrappingModes.Normal;
            U3In(info.rectTransform, W, 60f, 370f, 740f, 300f);
            FitText(info, 20);
            Ui1.Button("Ready", card, Loc.T("live.ready"), true, new Vector2(0.5f, 1f), new Vector2(0f, -(H - 130f)), new Vector2(620f, 112f),
                () => { CloseLiveIntro(); ready?.Invoke(); }, null, 44);
        }

        public void CloseLiveIntro()
        {
            if (_liveIntro != null) UiFactory.Kill(_liveIntro.gameObject);
            _liveIntro = null;
            LiveTabsOff();
        }

        private int _livePrevTab = -1;
        private Action _livePrevLevels;

        /// <summary>Bilgi kartı açıkken alt menü üstte ve Canlı Maç seçili; Harita sekmesi kartı kapatır.</summary>
        private void LiveTabsOn()
        {
            if (_tabsHolder == null || _tabsActive == TabLive) return;
            _livePrevTab = _tabsActive;
            _livePrevLevels = _tabsLevels;
            RetargetTabs(TabLive, _tabsActive == 1 ? (Action)CloseLiveIntro : _tabsLevels, true);
        }

        private void LiveTabsOff()
        {
            if (_livePrevTab < 0) return;
            int prev = _livePrevTab;
            _livePrevTab = -1;
            if (_tabsActive == TabLive && _tabsHolder != null) RetargetTabs(prev, _livePrevLevels, false);
        }
    }
}
