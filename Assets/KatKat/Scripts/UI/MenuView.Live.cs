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
    }
}
