using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// v12 deneme bölümleri (docs/v12/PLAN.md §6.1 "kural verilir, strateji verilmez"):
    /// - Kural kartı: bölüm açılırken tür adı, türün tek cümlelik tanıtımı ve kurallar (büyük simge + tek cümle);
    ///   dokununca kapanır. Çözüm söylenmez.
    /// - Kalıcı kural simgeleri: hedef kabuğunun sağ ucunda; dokununca kural cümlesi bildirim olarak.
    /// - Katman görünümü: yığına uzun basınca alttan üste bütün katmanlar (tam bilgi).
    /// </summary>
    public sealed partial class HudView
    {
        public struct RuleLine
        {
            public string Rule, Text;
        }

        private RectTransform _ruleCard, _ruleIcons, _peek;
        private Action _ruleCardClosed;
        public bool RuleCardOpen => _ruleCard != null;
        public bool PeekShown => _peek != null && _peek.gameObject.activeSelf;

        public void ShowRuleCard(string title, string intro, IList<RuleLine> rules, Action closed)
        {
            HideRuleCard(false);
            _ruleCardClosed = closed;
            if (Ui3.Ready) { ShowRuleCardUi3(title, intro, rules); return; } // ui3/10, 23, 24, 28
            if (Ui1.Ready) { ShowRuleCardUi1(title, intro, rules); return; } // ui1/10
            var root = UiFactory.Rect("RuleCard", _safe);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _ruleCard = root;
            var dim = UiFactory.SpriteButton("Dim", root, UiArt.Box, new Vector2(10f, 10f), () => HideRuleCard(true));
            dim.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.16f, 0.62f);
            UiFactory.Stretch((RectTransform)dim.transform);
            float h = 330f + rules.Count * 120f;
            var card = MenuView.GoldCapsule("Card", root, new Color32(0xFF, 0xF4, 0xE0, 0xFF), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(880f, h), 10f);
            card.raycastTarget = true;
            var tag = UiFactory.Text("Tag", card.transform, Loc.T("trial.tag"), 30, new Color32(0xA8, 0x64, 0x16, 0xFF));
            tag.raycastTarget = false;
            UiFactory.Place(tag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(760f, 40f));
            var t = UiFactory.Title("Title", card.transform, title, 54, new Color32(0x12, 0x2E, 0x6E, 0xFF), Color.white);
            t.raycastTarget = false;
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(780f, 70f));
            t.enableAutoSizing = true; t.fontSizeMin = 32; t.fontSizeMax = 54;
            var i = UiFactory.Text("Intro", card.transform, intro, 32, new Color32(0x50, 0x67, 0x7A, 0xFF), TextAnchor.MiddleCenter, FontStyle.Normal);
            i.raycastTarget = false;
            UiFactory.Place(i.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(780f, 90f));
            i.enableAutoSizing = true; i.fontSizeMin = 22; i.fontSizeMax = 32;
            float y = -300f;
            foreach (var r in rules)
            {
                var icon = UiFactory.Image("Icon", card.transform, RuleIcons.For(r.Rule), Color.white);
                icon.raycastTarget = false;
                UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(-320f, y), new Vector2(96f, 96f));
                var rt = UiFactory.Text("Rule", card.transform, r.Text, 34, new Color32(0x12, 0x2E, 0x6E, 0xFF), TextAnchor.MiddleLeft);
                rt.raycastTarget = false;
                UiFactory.Place(rt.rectTransform, new Vector2(0.5f, 1f), new Vector2(60f, y), new Vector2(660f, 100f));
                rt.enableAutoSizing = true; rt.fontSizeMin = 22; rt.fontSizeMax = 34;
                y -= 120f;
            }
            var go = UiFactory.Text("Go", card.transform, Loc.T("trial.tapstart"), 30, new Color32(0x14, 0x86, 0x9A, 0xFF));
            go.raycastTarget = false;
            UiFactory.Place(go.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(760f, 44f));
            var hit = card.gameObject.AddComponent<Button>();
            hit.transition = Selectable.Transition.None;
            hit.onClick.AddListener(() => HideRuleCard(true));
        }

        /// <summary>ui1/10: krem panel, fener amblemi, mor kurdelede tür adı, tanıtım, kural satırları, Başla.</summary>
        private void ShowRuleCardUi1(string title, string intro, IList<RuleLine> rules)
        {
            var root = UiFactory.Rect("RuleCard", _safe);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _ruleCard = root;
            Ui1.Dimmer(root, () => HideRuleCard(true));
            // yerlesim 10: panel 972 genişlik, üst y 480; amblem kartın üst kenarına biner; kurdele 810×144; kural
            // satırları ~799×180; Başla 713×125. Panel boyu kural sayısından (3 kuralda ~1277).
            float h = 700f + rules.Count * 192f;
            var c = Ui1.Panel("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0f, Mathf.Min(0f, 960f - 480f - h * 0.5f)), new Vector2(972f, h)).rectTransform;
            var emblem = Ui1.Decor("Emblem", c, "region-coast");
            UiFactory.Place(emblem.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(300f, 348f));
            Ui1.Round("Close", c, "icon-close", new Vector2(1f, 1f), new Vector2(-80f, -50f), 130f, () => HideRuleCard(true));
            // "Deneme" etiketi ui1'de yok (tasarımda amblem doğrudan kurdeleye oturur; bağlam başlıktan belli).
            Ui1.Ribbon(c, title, new Vector2(0.5f, 1f), new Vector2(0f, -331f), new Vector2(810f, 144f), 56);
            var i = Ui1.Label("Intro", c, intro, 38, Ui1.Navy, false);
            UiFactory.Place(i.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -445f), new Vector2(840f, 100f));
            i.enableAutoSizing = true; i.fontSizeMin = 24; i.fontSizeMax = 38;
            float y = -590f;
            foreach (var r in rules)
            {
                var row = Ui1.Row("RuleRow", c, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(820f, 176f));
                var icon = UiFactory.Image("Icon", row.transform, RuleIcons.For(r.Rule), Color.white);
                icon.raycastTarget = false;
                UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(100f, 2f), new Vector2(130f, 130f));
                var rt = Ui1.Label("Rule", row.transform, r.Text, 38, Ui1.Navy, true, TextAnchor.MiddleLeft);
                UiFactory.Place(rt.rectTransform, new Vector2(0f, 0.5f), new Vector2(190f + 300f, 2f), new Vector2(600f, 150f));
                rt.enableAutoSizing = true; rt.fontSizeMin = 24; rt.fontSizeMax = 38;
                y -= 192f;
            }
            Ui1.Button("Start", c, Loc.T("trial.start"), true, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(713f, 140f), () => HideRuleCard(true), UiArt.Icon(UiIcon.Play), 60);
        }

        /// <summary>
        /// ui3 kural kartı (sayfalar/10, 23, 24, 28): genişlik 860; amblem (308,−68,244,200) kartın üstüne taşar; kurdele
        /// başlık (56,152,748,88); tanıtım (56,264,748,112) bodyCentered 36/600; kural satırları 748 genişlik, yükseklik
        /// max(128, metin+40), aralık 16, simge (20,36,88,88), metin (132,20,592) body 36/600; Başla 748×116. Kart ve
        /// amblem güvenli alanda dikey ortalı. Kapat ve dışa dokunma mevcut eylem (HideRuleCard).
        /// </summary>
        private void ShowRuleCardUi3(string title, string intro, IList<RuleLine> rules)
        {
            var root = UiFactory.Rect("RuleCard", _safe);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _ruleCard = root;
            Ui1.Dimmer(root, () => HideRuleCard(true));
            var card = UiFactory.Rect("Card", root);
            var blocker = card.gameObject.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0f); // kartın içine dokunmak kapatmaz
            var bg = UiFactory.Image("Bg", card, Ui1.Art("panel-modal"), Color.white, true);
            bg.raycastTarget = false;
            bg.pixelsPerUnitMultiplier = bg.sprite.rect.width / 860f;
            UiFactory.Stretch(bg.rectTransform, 0f, 0f, -32f, 0f);
            ResultPanel.Ui3Layers(card);

            float Y(float top, float hh) => -(top + hh * 0.5f);
            var emblem = Ui1.Decor("Emblem", card, "region-coast");
            UiFactory.Place(emblem.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(-68f, 200f)), new Vector2(244f, 200f));
            Ui1.Round("Close", card, "icon-close", new Vector2(1f, 1f), new Vector2(-24f, -16f), 96f, () => HideRuleCard(true));
            Ui1.Ribbon(card, title, new Vector2(0.5f, 1f), new Vector2(0f, Y(146f, 100f)), new Vector2(820f, 120f), 50);
            var i = Ui1.Label("Intro", card, intro, 36, Ui3.Ink, false);
            i.textWrappingMode = TMPro.TextWrappingModes.Normal;
            float ih = Mathf.Max(112f, i.GetPreferredValues(intro, 748f, 0f).y + 16f);
            UiFactory.Place(i.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(264f, ih)), new Vector2(748f, ih));
            float y = 264f + ih + 32f;
            foreach (var r in rules)
            {
                var probe = Ui1.Label("Rule", card, r.Text, 36, Ui3.Ink, false, TextAnchor.MiddleLeft);
                probe.textWrappingMode = TMPro.TextWrappingModes.Normal;
                float rh = Mathf.Max(128f, probe.GetPreferredValues(r.Text, 592f, 0f).y + 40f);
                var row = Ui1.Row("RuleRow", card, new Vector2(0.5f, 1f), new Vector2(0f, Y(y, rh)), new Vector2(748f, rh));
                var icon = UiFactory.Image("Icon", row.transform, RuleIcons.For(r.Rule), Color.white);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(88f, 88f));
                probe.transform.SetParent(row.transform, false);
                UiFactory.Place(probe.rectTransform, new Vector2(0f, 0.5f), new Vector2(132f + 296f, 0f), new Vector2(592f, rh - 40f));
                y += rh + 16f;
            }
            y += 28f;
            Ui1.Button("Start", card, Loc.T("trial.start"), true, new Vector2(0.5f, 1f), new Vector2(0f, Y(y, 116f)), new Vector2(748f, 116f), () => HideRuleCard(true), UiArt.Icon(UiIcon.Play), 44);
            float h = y + 116f + 56f;
            float group = 68f + h;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(860f, h);
            card.anchoredPosition = new Vector2(0f, group * 0.5f - 68f - h * 0.5f);
            float safeH = _safe.rect.height > 100f ? _safe.rect.height : RefHeight;
            if (group + 96f > safeH) card.localScale = Vector3.one * ((safeH - 96f) / group);
        }

        /// <summary>ui1: yuvarlak düğmeyi tek parça ui1 simgesiyle giydirir (iç gövde/simge katmanları gizlenir).</summary>
        private static void SkinRound(Button b, string key)
        {
            var sp = Ui1.Art(key);
            if (b == null || sp == null) return;
            var img = b.GetComponent<Image>();
            img.sprite = sp;
            img.color = Color.white;
            img.preserveAspect = true;
            foreach (var n in new[] { "Body", "Hi", "Icon" })
            {
                var t = b.transform.Find(n);
                if (t != null && t.GetComponent<Image>() is Image ci) ci.enabled = false;
            }
        }

        private RectTransform _dailyHeader;

        /// <summary>
        /// ui1/17: günlük bulmacada üstte krem plakada takvim + "Günlük Bulmaca" + tarih (HUD çakışmasın diye maskot
        /// gizlenir). Boş/null: kaldırılır (normal bölüm).
        /// </summary>
        public void SetDailyHeader(string date)
        {
            if (_dailyHeader != null) UiFactory.Kill(_dailyHeader.gameObject);
            _dailyHeader = null;
            if (_mascot != null) _mascot.gameObject.SetActive(string.IsNullOrEmpty(date));
            if (string.IsNullOrEmpty(date) || !Ui1.Ready) return;
            var plate = Ui1.Decor("DailyHeader", _safe, "header-ivory");
            plate.preserveAspect = false;
            // yerlesim 17: plaka üstte duraklat ile geri al arasında; hamle grubu SetDailyLayout ile aşağıda.
            UiFactory.Place(plate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(620f, 128f));
            _dailyHeader = plate.rectTransform;
            var cal = UiFactory.Image("Calendar", plate.transform, MenuView.CalendarSprite(), Color.white);
            cal.preserveAspect = true; cal.raycastTarget = false;
            UiFactory.Place(cal.rectTransform, new Vector2(0f, 0.5f), new Vector2(92f, 4f), new Vector2(80f, 80f));
            var t = Ui1.Label("Title", plate.transform, Loc.T("daily.title"), 46, Ui1.Navy);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(150f + 210f, 20f), new Vector2(420f, 58f));
            t.enableAutoSizing = true; t.fontSizeMax = 46; t.fontSizeMin = 26;
            var d = Ui1.Label("Date", plate.transform, date, 32, Ui1.Navy, false);
            UiFactory.Place(d.rectTransform, new Vector2(0f, 0.5f), new Vector2(150f + 210f, -26f), new Vector2(420f, 42f));
        }

        public void HideRuleCard(bool notify)
        {
            if (_ruleCard != null) UiFactory.Kill(_ruleCard.gameObject);
            _ruleCard = null;
            var c = _ruleCardClosed;
            _ruleCardClosed = null;
            if (notify) c?.Invoke();
        }

        /// <summary>Kalıcı kural simgeleri (hedef kabuğunun sağ ucu). Boş liste: yok.</summary>
        public void SetRuleIcons(IList<RuleLine> rules)
        {
            if (_ruleIcons != null) UiFactory.Kill(_ruleIcons.gameObject);
            _ruleIcons = null;
            if (rules == null || rules.Count == 0 || _goalsBar == null) return;
            var holder = UiFactory.Rect("RuleIcons", _goalsBar.parent);
            holder.anchorMin = holder.anchorMax = new Vector2(1f, 0.5f);
            holder.pivot = new Vector2(1f, 0.5f);
            holder.sizeDelta = new Vector2(rules.Count * 66f, 64f);
            holder.anchoredPosition = new Vector2(-14f, 0f);
            _ruleIcons = holder;
            for (int k = 0; k < rules.Count; k++)
            {
                var r = rules[k];
                var b = UiFactory.SpriteButton("Rule_" + r.Rule, holder, RuleIcons.For(r.Rule), new Vector2(60f, 60f), () => ShowToast(r.Text, 2.6f));
                UiFactory.Place((RectTransform)b.transform, new Vector2(0f, 0.5f), new Vector2(33f + k * 66f, 0f), new Vector2(60f, 60f));
                PressFeedback.Attach(b);
            }
        }

        /// <summary>Uzun basış: yığının bütün katmanları (alttan üste, üstteki en yukarıda) ekran konumunun üstünde.</summary>
        public void ShowStackPeek(Vector2 screenPos, IList<LayerColor> layers)
        {
            HideStackPeek();
            if (layers == null || layers.Count == 0 || _safe == null) return;
            var canvasRt = (RectTransform)_safe;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPos, Canvas != null && Canvas.renderMode != RenderMode.ScreenSpaceOverlay ? Canvas.worldCamera : null, out var local);
            const float chip = 64f, gap = 6f;
            float h = layers.Count * (chip + gap) + 70f;
            var peek = UiFactory.Rect("StackPeek", _safe);
            peek.anchorMin = peek.anchorMax = new Vector2(0.5f, 0.5f);
            peek.pivot = new Vector2(0.5f, 0f);
            peek.sizeDelta = new Vector2(110f, h);
            var half = canvasRt.rect.size * 0.5f;
            float x = Mathf.Clamp(local.x, -half.x + 70f, half.x - 70f);
            float yy = Mathf.Min(local.y + 70f, half.y - h - 10f);
            peek.anchoredPosition = new Vector2(x, yy);
            _peek = peek;
            var bg = UiFactory.Image("Bg", peek, UiArt.Box, new Color32(0xFF, 0xF4, 0xE0, 0xF6), true);
            bg.raycastTarget = false;
            UiFactory.Stretch(bg.rectTransform);
            var ol = bg.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color32(0x12, 0x2E, 0x6E, 0xFF);
            ol.effectDistance = new Vector2(3f, -3f);
            var top = UiFactory.Text("Top", peek, Loc.T("trial.peek.top"), 20, new Color32(0x50, 0x67, 0x7A, 0xFF));
            top.raycastTarget = false;
            UiFactory.Place(top.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(104f, 26f));
            for (int k = 0; k < layers.Count; k++)
            {
                // k=0 en alt katman → en aşağıda.
                var c = UiFactory.Image("Layer" + k, peek, PremiumArt.Chip(layers[k]) ?? UiArt.Chip(layers[k]), Color.white);
                c.preserveAspect = true;
                c.raycastTarget = false;
                UiFactory.Place(c.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 22f + chip * 0.5f + k * (chip + gap)), new Vector2(chip, chip));
            }
        }

        public void HideStackPeek()
        {
            if (_peek != null) UiFactory.Kill(_peek.gameObject);
            _peek = null;
        }
    }
}
