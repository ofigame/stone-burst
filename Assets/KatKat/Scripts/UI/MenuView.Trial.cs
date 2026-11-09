using System;
using System.Collections.Generic;
using KatKat.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// v12 deneme seti girişi (kullanıcı, 2026-11-08: "haritada deneme düğmesi olsun"). Haritanın sol sütununda
    /// Sıralama'nın altında "Deneme" düğmesi; dokununca harita üstünde liste: 10 bölüm, tür adı, kural simgeleri,
    /// bitirilenlerde onay. Ana harita, yıldız ve ilerleme etkilenmez.
    /// </summary>
    public sealed partial class MenuView
    {
        public struct TrialItem
        {
            public int Index;
            public string Title, Intro;
            public List<string> Rules;
            public bool Done, Boss;
        }

        public Action TrialPressed;
        private RectTransform _trialModal;
        public bool IsTrialListShown => _trialModal != null;

        private void BuildTrialButton(RectTransform shell)
        {
            var b = ShellRoundButton("TrialButton", shell, RuleIcons.For("exact"), 120f, () => TrialPressed?.Invoke());
            UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(-432f, -V11ColumnZpeTop - V11LeftShift - 250f), new Vector2(120f, 120f));
            var t = UiFactory.Title("TrialLabel", shell, Loc.T("trial.button"), 30, Color.white, V11Navy);
            t.raycastTarget = false;
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(-432f, -V11ColumnZpeTop - V11LeftShift - 330f), new Vector2(200f, 40f));
            FitText(t, 20);
        }

        public void CloseTrials()
        {
            if (_trialModal != null) UiFactory.Kill(_trialModal.gameObject);
            _trialModal = null;
        }

        public void ShowTrials(IList<TrialItem> items, Action<int> pick)
        {
            CloseTrials();
            if (_page == null) return;
            if (Ui1.Ready) { ShowTrialsUi1(items, pick); return; } // ui1/09
            var root = UiFactory.Rect("TrialModal", _page);
            UiFactory.Stretch(root, -200f, -200f, -400f, -400f);
            _trialModal = root;
            var dim = UiFactory.SpriteButton("Dim", root, UiArt.Box, new Vector2(10f, 10f), CloseTrials);
            dim.GetComponent<Image>().color = new Color(0.04f, 0.1f, 0.22f, 0.62f);
            UiFactory.Stretch((RectTransform)dim.transform);
            const float rowH = 104f;
            float h = 250f + items.Count * (rowH + 10f);
            var card = WhiteCapsule("Card", root, new Color32(0xF7, 0xFB, 0xFF, 0xFF), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(940f, h), 8f);
            card.raycastTarget = true;
            var title = UiFactory.Title("Title", card.transform, Loc.T("trial.list.title"), 52, V11Navy, Color.white);
            title.raycastTarget = false;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(760f, 70f));
            FitText(title, 32);
            var sub = UiFactory.Text("Sub", card.transform, Loc.T("trial.list.sub"), 28, new Color32(0x50, 0x67, 0x7A, 0xFF), TextAnchor.MiddleCenter, FontStyle.Normal);
            sub.raycastTarget = false;
            UiFactory.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(820f, 60f));
            sub.enableAutoSizing = true; sub.fontSizeMin = 20; sub.fontSizeMax = 28;
            float y = -200f - rowH * 0.5f;
            foreach (var it in items)
            {
                int idx = it.Index;
                var row = UiFactory.SpriteButton("Trial" + (idx + 1), card.transform, UiArt.Box, new Vector2(860f, rowH), () => { CloseTrials(); pick?.Invoke(idx); });
                var img = row.GetComponent<Image>();
                img.type = Image.Type.Sliced;
                img.color = it.Boss ? new Color32(0xFF, 0xEC, 0xC4, 0xFF) : it.Done ? new Color32(0xDD, 0xF7, 0xEC, 0xFF) : new Color32(0xE6, 0xF4, 0xFF, 0xFF);
                UiFactory.Place((RectTransform)row.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(860f, rowH));
                PressFeedback.Attach(row, null, 80f, 120f);
                var num = UiFactory.Image("Num", row.transform, UiArt.Circle, it.Boss ? new Color32(0xF2, 0xB8, 0x3A, 0xFF) : new Color32(0x14, 0x86, 0x9A, 0xFF));
                num.raycastTarget = false;
                UiFactory.Place(num.rectTransform, new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(72f, 72f));
                var nt = UiFactory.Title("N", num.transform, (idx + 1).ToString(), 36, Color.white, V11Navy);
                nt.raycastTarget = false;
                UiFactory.Stretch(nt.rectTransform);
                var tt = UiFactory.Text("Name", row.transform, it.Title, 36, V11Navy, TextAnchor.MiddleLeft);
                tt.raycastTarget = false;
                UiFactory.Place(tt.rectTransform, new Vector2(0f, 0.5f), new Vector2(110f + 220f, 0f), new Vector2(440f, 60f));
                tt.textWrappingMode = TextWrappingModes.NoWrap;
                tt.overflowMode = TextOverflowModes.Ellipsis;
                for (int k = 0; k < it.Rules.Count; k++)
                {
                    var ic = UiFactory.Image("Rule", row.transform, RuleIcons.For(it.Rules[k]), Color.white);
                    ic.raycastTarget = false;
                    UiFactory.Place(ic.rectTransform, new Vector2(1f, 0.5f), new Vector2(-150f - k * 58f, 0f), new Vector2(52f, 52f));
                }
                if (it.Done)
                {
                    var ok = UiFactory.Image("Done", row.transform, UiArt.Circle, UiArt.CheckGreen);
                    ok.raycastTarget = false;
                    UiFactory.Place(ok.rectTransform, new Vector2(1f, 0.5f), new Vector2(-56f, 0f), new Vector2(58f, 58f));
                    var mk = UiFactory.Image("Mark", ok.transform, UiArt.Icon(UiIcon.Check), Color.white);
                    mk.raycastTarget = false;
                    UiFactory.Stretch(mk.rectTransform, 8, 8, 8, 8);
                }
                y -= rowH + 10f;
            }
            var close = ShellRoundButton("Close", card.transform, PremiumArt.V4("icons/back") ?? UiArt.Icon(UiIcon.Back), 84f, CloseTrials);
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(84f, 84f));
        }
    }
}
