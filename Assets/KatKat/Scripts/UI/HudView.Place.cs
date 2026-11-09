using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Reklam ödülü taş yerleştirme şeridi (kullanıcı, 2026-11-09: "4 taş tahtanın hemen üst kısmında sıralansın,
    /// seçtiğini koyduğunda diğerleri yok olsun"). Hedef kartı ile tahta arasındaki şerit bandında; solda yönerge,
    /// sağda dört renk taşı. Seçili taş büyür ve altın halka alır.
    /// </summary>
    public sealed partial class HudView
    {
        public event Action<int> PlaceStoneChosen;

        private RectTransform _placeRow;
        private readonly List<RectTransform> _placeChips = new List<RectTransform>();
        private TMPro.TextMeshProUGUI _placeLabel;

        public bool PlaceRowVisible => _placeRow != null;

        public void ShowPlaceRow(IList<LayerColor> colors, int selected)
        {
            HidePlaceRow();
            HideToast();
            var bgSprite = UiFactory.Art("ui/hud-moves-pill", "hud_moves");
            var bg = UiFactory.Image("PlaceRow", _safe, bgSprite ?? UiArt.Box, bgSprite != null ? Color.white : new Color(1f, 0.97f, 0.9f, 0.96f), true);
            _placeRow = bg.rectTransform;
            _placeRow.anchorMin = new Vector2(0f, 1f);
            _placeRow.anchorMax = new Vector2(1f, 1f);
            _placeRow.pivot = new Vector2(0.5f, 1f);
            _placeRow.offsetMin = new Vector2(40f, 0f);
            _placeRow.offsetMax = new Vector2(-40f, 0f);
            _placeRow.sizeDelta = new Vector2(_placeRow.sizeDelta.x, ToastH + 6f);
            _placeRow.anchoredPosition = new Vector2(0f, -(GoalsBottom + _headShift + ToastGap - 2f));
            UiFactory.FitSliced(bg, ToastH);
            bg.raycastTarget = true;
            _placeLabel = UiFactory.Text("Label", _placeRow, "", 30, UiArt.Brown);
            _placeLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            _placeLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            _placeLabel.rectTransform.pivot = new Vector2(0f, 0.5f);
            _placeLabel.rectTransform.anchoredPosition = new Vector2(40f, 2f);
            _placeLabel.rectTransform.sizeDelta = new Vector2(400f, -10f);
            _placeLabel.enableAutoSizing = true; _placeLabel.fontSizeMax = 30; _placeLabel.fontSizeMin = 18;
            _placeChips.Clear();
            for (int i = 0; i < colors.Count; i++)
            {
                int k = i;
                var col = colors[i];
                var chip = UiFactory.SpriteButton("Stone" + i, _placeRow, PremiumArt.Chip(col) ?? UiArt.Chip(col), new Vector2(78f, 78f), () => PlaceStoneChosen?.Invoke(k));
                var rt = (RectTransform)chip.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-62f - (colors.Count - 1 - i) * 104f, 2f);
                chip.GetComponent<Image>().preserveAspect = true;
                _placeChips.Add(rt);
            }
            SetPlaceSelected(selected);
        }

        public void SetPlaceSelected(int selected)
        {
            if (_placeRow == null) return;
            Loc.Set(_placeLabel, Loc.T(selected < 0 ? "place.pick" : "place.where"));
            for (int i = 0; i < _placeChips.Count; i++)
            {
                var rt = _placeChips[i];
                bool on = i == selected;
                rt.sizeDelta = Vector2.one * (on ? 92f : 78f);
                var ring = rt.Find("Ring");
                if (on && ring == null)
                {
                    var r = UiFactory.Image("Ring", rt, UiArt.Icon(UiIcon.Ring), UiArt.Gold);
                    r.raycastTarget = false;
                    UiFactory.Stretch(r.rectTransform, -14f, -14f, -14f, -14f);
                }
                else if (!on && ring != null) UiFactory.Kill(ring.gameObject);
                var img = rt.GetComponent<Image>();
                img.color = selected < 0 || on ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }
        }

        /// <summary>Geçersiz yer: yönerge kısa süre uyarıya döner (sonraki seçimde eski yazı).</summary>
        public void FlashPlaceLabel(string text)
        {
            if (_placeLabel != null) Loc.Set(_placeLabel, text);
        }

        public void HidePlaceRow()
        {
            if (_placeRow != null) UiFactory.Kill(_placeRow.gameObject);
            _placeRow = null;
            _placeChips.Clear();
        }
    }
}
