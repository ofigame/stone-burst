using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Konsept pilotu: tehdidin görev şeridi (hedef kartı ile tahta arasındaki bilgi şeridi bandında). Kullanıcı
    /// (2026-11-15): "bilgilendirme yerleri çok anlaşılmıyor … görseller ile iyi anlatabilmek lazım; Dönen Hedefte … 2 hamle
    /// şu taştan (taşın şekli olmalı)". Üst satır simge + sayı (taş şekli, kuzgun, balon, saat), alt satır kısa ne yapmalı
    /// cümlesi. Bilgi şeridiyle aynı yerde ve onun altında: geçici bildirim çıkınca üstünü örter.
    /// </summary>
    public sealed partial class HudView
    {
        public struct StripItem
        {
            public Sprite Icon;
            public string Text;
            public bool Dim;
            public float Size;
            public static StripItem Of(Sprite icon, float size = 54f, bool dim = false) => new StripItem { Icon = icon, Size = size, Dim = dim };
            public static StripItem Say(string text, bool dim = false) => new StripItem { Text = text, Dim = dim };
        }

        private const float HazardH = 128f;
        private bool _goalFocusOn;
        private RectTransform _hazardBox, _hazardRow;
        private TMPro.TextMeshProUGUI _hazardText, _hazardHint;

        /// <summary>Yalnız metin (diğer konseptler); null ya da boş → gizli.</summary>
        public void SetHazardLine(string text)
        {
            if (string.IsNullOrEmpty(text)) { if (_hazardBox != null) _hazardBox.gameObject.SetActive(false); return; }
            EnsureHazardBox();
            ClearRow();
            _hazardHint.gameObject.SetActive(false);
            _hazardText.gameObject.SetActive(true);
            Loc.Set(_hazardText, text);
        }

        /// <summary>Simge şeridi + alt satırda ne yapmalı cümlesi.</summary>
        public void SetHazardStrip(IList<StripItem> items, string hint)
        {
            EnsureHazardBox();
            ClearRow();
            _hazardText.gameObject.SetActive(false);
            bool two = !string.IsNullOrEmpty(hint);
            _hazardHint.gameObject.SetActive(two);
            if (two) Loc.Set(_hazardHint, hint);
            _hazardRow.anchoredPosition = new Vector2(0f, two ? 16f : 0f);
            // Ölçüp ortala.
            const float gap = 10f;
            var widths = new List<float>();
            var made = new List<RectTransform>();
            foreach (var it in items)
            {
                if (it.Icon != null)
                {
                    var img = UiFactory.Image("Icon", _hazardRow, it.Icon, it.Dim ? new Color(1f, 1f, 1f, 0.45f) : Color.white);
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    img.rectTransform.sizeDelta = Vector2.one * it.Size;
                    made.Add(img.rectTransform); widths.Add(it.Size);
                }
                else
                {
                    var t = UiFactory.Text("Text", _hazardRow, "", 36, it.Dim ? new Color(0.45f, 0.36f, 0.30f, 0.75f) : (Color)UiArt.Brown);
                    t.richText = true;
                    t.raycastTarget = false;
                    t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                    Loc.Set(t, it.Text);
                    float w = Mathf.Ceil(t.GetPreferredValues(t.text, 2000f, 64f).x) + 2f;
                    t.rectTransform.sizeDelta = new Vector2(w, 64f);
                    made.Add(t.rectTransform); widths.Add(w);
                }
            }
            float total = 0f;
            foreach (var w in widths) total += w;
            total += gap * Mathf.Max(0, widths.Count - 1);
            // Sığmazsa küçült (bant ~900 px).
            float avail = ((RectTransform)_hazardBox).rect.width - 60f;
            float k = avail > 0f && total > avail ? avail / total : 1f;
            _hazardRow.localScale = Vector3.one * k;
            float x = -total * 0.5f;
            for (int i = 0; i < made.Count; i++)
            {
                var rt = made[i];
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x + widths[i] * 0.5f, 0f);
                x += widths[i] + gap;
            }
        }

        private void ClearRow()
        {
            for (int i = _hazardRow.childCount - 1; i >= 0; i--) UiFactory.Kill(_hazardRow.GetChild(i).gameObject);
        }

        private void EnsureHazardBox()
        {
            if (_hazardBox == null)
            {
                var sprite = UiFactory.Art("ui/hud-moves-pill", "hud_moves");
                var bg = UiFactory.Image("HazardLine", _safe, sprite ?? UiArt.Box, sprite != null ? new Color(1f, 0.95f, 0.88f, 1f) : new Color(1f, 0.95f, 0.9f, 0.95f), true);
                bg.raycastTarget = false;
                _hazardBox = UiFactory.Band(bg.rectTransform, true, GoalsBottom + ToastGap, ToastH);
                _hazardBox.offsetMin = new Vector2(48f, _hazardBox.offsetMin.y);
                _hazardBox.offsetMax = new Vector2(-48f, _hazardBox.offsetMax.y);
                UiFactory.FitSliced(bg, HazardH);
                _hazardText = UiFactory.Text("Text", bg.transform, "", 32, UiArt.Brown);
                UiFactory.Stretch(_hazardText.rectTransform, 40, 40, 4, 8);
                _hazardText.richText = true;
                _hazardText.enableAutoSizing = true;
                _hazardText.fontSizeMax = 32;
                _hazardText.fontSizeMin = 20;
                _hazardText.raycastTarget = false;
                _hazardRow = UiFactory.Rect("Row", bg.transform);
                _hazardRow.anchorMin = _hazardRow.anchorMax = new Vector2(0.5f, 0.5f);
                _hazardRow.sizeDelta = new Vector2(10f, 64f);
                _hazardHint = UiFactory.Text("Hint", bg.transform, "", 21, new Color(0.42f, 0.30f, 0.22f, 1f));
                _hazardHint.rectTransform.anchorMin = new Vector2(0f, 0f);
                _hazardHint.rectTransform.anchorMax = new Vector2(1f, 0f);
                _hazardHint.rectTransform.pivot = new Vector2(0.5f, 0f);
                _hazardHint.rectTransform.anchoredPosition = new Vector2(0f, 12f);
                _hazardHint.rectTransform.sizeDelta = new Vector2(-60f, 34f);
                _hazardHint.enableAutoSizing = true;
                _hazardHint.fontSizeMax = 26;
                _hazardHint.fontSizeMin = 15;
                _hazardHint.richText = true;
                _hazardHint.raycastTarget = false;
                // Bilgi şeridinin altında (aynı yer): şerit görünürken onu örter.
                _hazardBox.SetSiblingIndex(_toastBox.GetSiblingIndex());
            }
            _hazardBox.anchoredPosition = new Vector2(0f, -(GoalsBottom + _headShift + ToastGap + 6f));
            // Görev şeridi bilgi şeridinden yüksek (simge + iki satır); hedef kartı ile tahta arasında yer var.
            _hazardBox.sizeDelta = new Vector2(_hazardBox.sizeDelta.x, HazardH);
            _hazardBox.gameObject.SetActive(true);
        }

        public string HazardLineText => _hazardBox != null && _hazardBox.gameObject.activeSelf ? (_hazardText.gameObject.activeSelf ? _hazardText.text : _hazardHint.text) : null;

        /// <summary>
        /// Dönen Hedef: yalnız etkin hedef çipi tam görünür ve altın halkayla öne çıkar; diğerleri soluk ("şimdi sayılmaz").
        /// -1 → odak yok (hepsi normal).
        /// </summary>
        public void SetGoalFocus(int index)
        {
            // Odak hiç açılmadıysa dokunma: aşamalı hedeflerin soluklaştırması (SetGoalStages) korunur.
            if (index < 0 && !_goalFocusOn) return;
            _goalFocusOn = index >= 0;
            for (int i = 0; i < _chips.Count; i++)
            {
                var root = _chips[i].Root;
                if (!root.TryGetComponent<CanvasGroup>(out var cg)) cg = root.gameObject.AddComponent<CanvasGroup>();
                bool on = index < 0 || i == index;
                cg.alpha = on ? 1f : 0.36f;
                var ring = root.Find("Focus");
                if (index >= 0 && i == index && ring == null)
                {
                    var r = UiFactory.Image("Focus", root, UiArt.Icon(UiIcon.Ring), UiArt.Gold);
                    r.raycastTarget = false;
                    var ic = _chips[i].Icon;
                    r.rectTransform.anchorMin = ic.anchorMin; r.rectTransform.anchorMax = ic.anchorMax; r.rectTransform.pivot = ic.pivot;
                    r.rectTransform.anchoredPosition = ic.anchoredPosition;
                    r.rectTransform.sizeDelta = ic.sizeDelta + Vector2.one * 22f;
                    r.transform.SetSiblingIndex(ic.GetSiblingIndex());
                }
                else if ((index < 0 || i != index) && ring != null) UiFactory.Kill(ring.gameObject);
            }
        }
    }
}
