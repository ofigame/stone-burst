using System;
using System.Collections.Generic;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// v9 B: Günlük Bulmaca ve Koleksiyon ekranları. Ayarlar sayfasıyla aynı dil (resimsiz renk geçişi + krem kart).
    /// Koleksiyon taşı görselleri geçicidir (yer tutucu); nihai görseller geldiğinde yalnız artKey eşlemesi değişir.
    /// </summary>
    public sealed partial class MenuView
    {
        public Action DailyPressed, CollectionPressed;

        /// <summary>Harita (bölüm yolu) sayfası açık mı (testler için).</summary>
        public bool IsMapShown => _mapContent != null && _page != null && gameObject.activeSelf;

        /// <summary>Açık menü sayfasındaki adı verilen düğmeye basar (testler için). Bulamazsa false.</summary>
        public bool PressForTest(string buttonName)
        {
            if (_page == null) return false;
            foreach (var b in _page.GetComponentsInChildren<Button>(false))
                if (b.name == buttonName && b.interactable) { b.onClick.Invoke(); return true; }
            return false;
        }

        public struct DailyModel
        {
            public bool Available, Entered, Won, AttemptOpen, AdFree, BonusToday;
            public int AttemptsLeft, AttemptsAllowed;
            /// <summary>v10 P3.4: günün seviye grubu ve bölüm aralığı (TierMin 0 ise gösterilmez).</summary>
            public int Tier, TierMin, TierMax;
            public string StoneName, DayId;
            public Sprite StoneSprite;
        }

        public struct StoneItem
        {
            public string Id, Name, Desc, Section, SectionTitle;
            public Sprite Sprite;
            public bool Owned, Selected, Hidden, Warm;
            /// <summary>ui1/03: yıldız eşiğiyle açılan kilitli taşta eşik (stones.json); diğerlerinde 0.</summary>
            public int Stars;
        }

        private RectTransform MetaPage(string title, out RectTransform card, float cardH, Action back)
        {
            var page = NewPage(null);
            var grad = UiFactory.Image("Gradient", page, GradientSprite, Color.white);
            UiFactory.Stretch(grad.rectTransform, -200f, -200f, -400f, -400f);
            SettingsBubbles(page);
            var t = UiFactory.Title("Title", page, title, 88, UiArt.Cream, UiArt.HudTealDark);
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(720f, 130f));
            FitText(t, 50);
            // Sol üstte geri düğmesi: haritaya döner (alt çubuktaki Harita sekmesiyle aynı).
            var b = UiFactory.CircleButton("Back", page, false, UiIcon.Back, 120f, back);
            UiFactory.Place((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(100f, -150f), new Vector2(120f, 120f));
            var panelSprite = UiFactory.Art("ui/modal-panel");
            var c = UiFactory.Image("Card", page, panelSprite ?? UiArt.Box, panelSprite != null ? Color.white : UiArt.Ivory, true);
            UiFactory.Place(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(920f, cardH));
            UiFactory.FitSliced(c, 260f);
            card = c.rectTransform;
            return page;
        }

        private static Text Body(RectTransform parent, string text, float y, float h, int size = 36, float w = 760f)
        {
            var t = UiFactory.Text("Body", parent, text, size, UiArt.Brown, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(w, h));
            t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = Mathf.Max(18, size / 2);
            return t;
        }

        /// <summary>
        /// v10 (kullanıcı, 2026-11-07): Günlük bulmaca ana sayfanın (harita) üstünde modal açılır; ayrı sayfa değil.
        /// Kullanıcının referans görseli: krem gövde + altın çerçeve, takvimli tarih kapsülü, lavanta seviye kapsülü
        /// ("Çok zor" kırmızı), kalın lacivert durum, iç panelde taş satırı, yıldızlı ayraç, yeşil oyna düğmesi.
        /// Kapatma: sağ üst düğme ya da karartmaya dokunma (harita yerinde kalır). Reklam SAHTE adaptörle çalışır.
        /// </summary>
        public void ShowDaily(DailyModel m, Action enter, Func<bool> play, Action back, Action collection, Action settings = null)
        {
            CloseDaily();
            if (_page == null) return;
            if (Ui1.Ready) { ShowDailyUi1(m, enter, play); return; } // ui1/02
            var root = UiFactory.Rect("DailyModal", _page);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _dailyModal = root;
            var dim = UiFactory.SpriteButton("Dim", root, UiArt.Box, new Vector2(10f, 10f), CloseDaily);
            dim.GetComponent<Image>().color = new Color(0.10f, 0.06f, 0.18f, 0.55f);
            UiFactory.Stretch((RectTransform)dim.transform);

            const float W = 780f, H = 880f;
            var card = GoldCapsule("Card", root, new Color32(0xFF, 0xF4, 0xE0, 0xFF), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(W, H), 12f);
            card.raycastTarget = true; // kart içine dokunma kapatmaz
            var c = card.rectTransform;
            Color ink = new Color32(0x1C, 0x36, 0x63, 0xFF);

            // Tarih kapsülü (takvim + tarih).
            var date = GoldCapsule("Date", c, new Color32(0xFF, 0xF8, 0xEC, 0xFF), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(340f, 78f), 5f);
            var cal = Decor("Calendar", date.transform, CalendarIcon());
            cal.preserveAspect = true;
            UiFactory.Place(cal.rectTransform, new Vector2(0f, 0.5f), new Vector2(56f, 2f), new Vector2(62f, 62f));
            var dt = UiFactory.Text("Day", date.transform, m.DayId ?? "", 40, ink);
            dt.raycastTarget = false;
            UiFactory.Place(dt.rectTransform, new Vector2(0f, 0.5f), new Vector2(205f, 0f), new Vector2(220f, 60f));
            FitText(dt, 24);

            float y = -160f;
            if (m.Available && m.TierMin > 0)
            {
                var tier = UiFactory.Image("Tier", c, UiArt.Box, new Color32(0xEC, 0xDE, 0xFA, 0xFF), true);
                tier.raycastTarget = false;
                UiFactory.Place(tier.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 30f), new Vector2(660f, 60f));
                var to = tier.gameObject.AddComponent<Outline>();
                to.effectColor = new Color32(0xD2, 0xBC, 0xF0, 0xFF); to.effectDistance = new Vector2(2f, -2f);
                // "... · Çok zor": ayraçtan sonrası kırmızı (tek yerelleştirilmiş metin).
                string s = Loc.F("daily.tier", m.TierMin, m.TierMax);
                int dot = s.LastIndexOf('·');
                string rich = dot > 0 ? s.Substring(0, dot + 1) + "<color=#E2463A>" + s.Substring(dot + 1) + "</color>" : s;
                var tt = UiFactory.Text("TierText", tier.transform, rich, 30, ink);
                tt.richText = true;
                tt.raycastTarget = false;
                UiFactory.Stretch(tt.rectTransform, 18, 18, 4, 4);
                tt.enableAutoSizing = true; tt.fontSizeMax = 30; tt.fontSizeMin = 16;
                y -= 76f;
            }

            string status;
            if (!m.Available) status = Loc.T("daily.none");
            else if (m.Won) status = Loc.T("daily.status.won");
            else if (!m.Entered) status = Loc.T("daily.status.locked");
            else if (m.AttemptOpen) status = Loc.T("daily.status.open");
            else if (m.AttemptsLeft > 0) status = Loc.F("daily.status.left", m.AttemptsLeft, m.AttemptsAllowed);
            else status = Loc.T("daily.status.none");
            var st = UiFactory.Text("Status", c, status, 46, ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            st.raycastTarget = false;
            UiFactory.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 80f), new Vector2(680f, 150f));
            st.enableAutoSizing = true; st.fontSizeMax = 46; st.fontSizeMin = 26;
            y -= 170f;

            // Taş satırı (iç panel).
            var row = UiFactory.Image("Stone", c, UiArt.Box, new Color32(0xF8, 0xEA, 0xD2, 0xFF), true);
            row.raycastTarget = false;
            UiFactory.Place(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 82f), new Vector2(680f, 164f));
            var icon = Decor("Icon", row.transform, m.StoneSprite != null ? m.StoneSprite : (CollArt("chest") ?? ChestIconTemp()));
            icon.preserveAspect = true;
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(92f, 0f), new Vector2(136f, 136f));
            var stx = UiFactory.Text("StoneText", row.transform, m.StoneName != null ? Loc.F("daily.stone", m.StoneName) : Loc.T("daily.nostone"), 32, new Color32(0x4A, 0x3A, 0x5C, 0xFF), TextAnchor.MiddleLeft, FontStyle.Normal);
            stx.raycastTarget = false;
            UiFactory.Place(stx.rectTransform, new Vector2(0f, 0.5f), new Vector2(180f + 240f, 0f), new Vector2(480f, 140f));
            stx.enableAutoSizing = true; stx.fontSizeMax = 32; stx.fontSizeMin = 18;
            y -= 182f;
            if (m.BonusToday)
            {
                var bn = UiFactory.Text("Bonus", c, Loc.T("daily.bonus.active"), 28, new Color32(0x2E, 0x8C, 0x3C, 0xFF));
                bn.raycastTarget = false;
                UiFactory.Place(bn.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 22f), new Vector2(680f, 44f));
                FitText(bn, 18);
                y -= 44f;
            }

            // Yıldızlı ayraç.
            for (int side = -1; side <= 1; side += 2)
            {
                var ln = UiFactory.Image("Line", c, UiArt.Box, new Color32(0xE8, 0xB8, 0x52, 0xFF));
                ln.raycastTarget = false;
                UiFactory.Place(ln.rectTransform, new Vector2(0.5f, 1f), new Vector2(side * 180f, y - 30f), new Vector2(260f, 4f));
            }
            var star = Decor("Star", c, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite);
            star.preserveAspect = true;
            UiFactory.Place(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 30f), new Vector2(54f, 54f));

            // Ana düğme: yeşil (oyna üçgeni + metin).
            string label = null; Action act = null;
            if (m.Available && !m.Won)
            {
                if (!m.Entered) { label = m.AdFree ? Loc.T("daily.enter.free") : Loc.T("daily.enter.ad"); act = enter; }
                else if (m.AttemptOpen || m.AttemptsLeft > 0) { label = m.AttemptOpen ? Loc.T("daily.resume") : Loc.T("daily.play"); act = () => play(); }
            }
            if (label != null)
            {
                var btn = UiFactory.SpriteButton(!m.Entered ? "Enter" : "Play", c, UiArt.Box, new Vector2(660f, 118f), act);
                var bi = btn.GetComponent<Image>();
                bi.type = Image.Type.Sliced;
                bi.color = new Color32(0x2F, 0xB8, 0x48, 0xFF);
                UiFactory.Place((RectTransform)btn.transform, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(660f, 118f));
                var bo = btn.gameObject.AddComponent<Outline>();
                bo.effectColor = new Color32(0xF4, 0xC6, 0x4E, 0xFF); bo.effectDistance = new Vector2(4f, -4f);
                var gloss = Decor("Gloss", btn.transform, UiArt.Box);
                gloss.type = Image.Type.Sliced;
                gloss.color = new Color(1f, 1f, 1f, 0.18f);
                UiFactory.Place(gloss.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(600f, 28f));
                var tri = Decor("PlayIcon", btn.transform, UiArt.Icon(UiIcon.Play));
                tri.color = new Color32(0xFF, 0xD8, 0x3A, 0xFF);
                tri.preserveAspect = true;
                UiFactory.Place(tri.rectTransform, new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(58f, 58f));
                var bl = UiFactory.Title("Label", btn.transform, label, 40, Color.white, new Color32(0x1E, 0x6E, 0x2C, 0xFF));
                bl.raycastTarget = false;
                UiFactory.Stretch(bl.rectTransform, 130, 40, 6, 10);
                FitText(bl, 22);
            }

            // Kapat (sağ üst). "Prototip" notu kaldırıldı (2026-11-11): mağaza sürümünde oyuncuya görünüyordu.
            var close = ShellRoundButton("Back", c, PremiumArt.V4("icons/back") ?? UiArt.Icon(UiIcon.Back), 92f, CloseDaily);
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(92f, 92f));
        }

        private RectTransform _dailyModal;

        /// <summary>Günlük modal açık mı (testler için).</summary>
        public bool IsDailyShown => _dailyModal != null;

        public void CloseDaily()
        {
            if (_dailyModal != null) UiFactory.Kill(_dailyModal.gameObject);
            _dailyModal = null;
        }




        /// <summary>Menü üstünde kısa bildirim (yeni taş, reklam iptali). 3 sn sonra kaybolur.</summary>
        public void ShowStoneToast(string text, Sprite icon)
        {
            if (_page == null) return;
            var pill = UiFactory.Image("StoneToast", _page, UiArt.Box, new Color32(0xFF, 0xF4, 0xE0, 0xF5), true);
            UiFactory.Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, BarBottom + BarH + 140f), new Vector2(900f, 130f));
            var o = pill.gameObject.AddComponent<Outline>();
            o.effectColor = new Color32(0xE0, 0xB2, 0x5E, 0xFF);
            o.effectDistance = new Vector2(3f, -3f);
            float left = 40f;
            if (icon != null)
            {
                var ic = UiFactory.Image("Icon", pill.transform, icon, Color.white);
                ic.preserveAspect = true;
                UiFactory.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(96f, 96f));
                left = 140f;
            }
            var t = UiFactory.Text("Text", pill.transform, text, 34, UiArt.Brown, TextAnchor.MiddleLeft);
            UiFactory.Stretch(t.rectTransform, left, 30f, 10f, 10f);
            t.enableAutoSizing = true; t.fontSizeMax = 34; t.fontSizeMin = 20;
            pill.gameObject.AddComponent<TimedFade>().Life = 3.2f;
        }

        /// <summary>Kendini söndürüp yok eden kısa ömürlü öğe (yalnız oynatmada; editör yakalamasında kalır).</summary>
        private sealed class TimedFade : MonoBehaviour
        {
            public float Life = 3f;
            private float _t;
            private CanvasGroup _g;

            private void Update()
            {
                _t += Time.unscaledDeltaTime;
                if (_g == null)
                {
                    // Unity "sahte null" döndürür; ?? işleci burada çalışmaz.
                    _g = gameObject.GetComponent<CanvasGroup>();
                    if (_g == null) _g = gameObject.AddComponent<CanvasGroup>();
                }
                _g.alpha = Mathf.Clamp01((Life - _t) / 0.4f);
                if (_t >= Life) Destroy(gameObject);
            }
        }
    }
}
