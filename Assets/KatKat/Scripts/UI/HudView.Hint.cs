using System;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Reklamlı ipucu düğmesi (kullanıcı, 2026-11-08). Alt çubukta, özel taş yuvalarıyla aynı sırada en sağda: ui3 petrol
    /// diski + krem ampul; sağ üstte bedeli gösteren "−1 ★" rozeti. Hak kullanılınca düğme soluklaşır
    /// (aynı denemede ikinci reklam yok). Yalnız normal bölümde görünür.
    /// </summary>
    public sealed partial class HudView
    {
        public event Action HintPressed;
        private Button _hintBtn;
        private CanvasGroup _hintGroup;
        private const float HintSize = 112f;
        private static Sprite _bulb;

        private void BuildHint(RectTransform bottom)
        {
            _hintBtn = MenuView.ShellRoundButton("Hint", bottom, Bulb, HintSize, () => HintPressed?.Invoke());
            UiFactory.Place((RectTransform)_hintBtn.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(HintSize, HintSize));
            _hintGroup = _hintBtn.gameObject.AddComponent<CanvasGroup>();
            var badge = UiFactory.Image("Cost", _hintBtn.transform, Ui3.Rounded(22f, Ui3.Purple, Ui3.PurpleDark, 2f, Ui3.Brass), Color.white);
            badge.type = Image.Type.Sliced;
            badge.raycastTarget = false;
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(-4f, 6f), new Vector2(84f, 44f));
            var t = UiFactory.Title("Text", badge.transform, "−1", 30, Ui3.OnTeal, Ui3.PurpleDark);
            t.raycastTarget = false;
            t.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 1f), new Vector2(44f, 40f));
            var star = UiFactory.Image("Star", badge.transform, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite, Color.white);
            star.preserveAspect = true;
            star.raycastTarget = false;
            UiFactory.Place(star.rectTransform, new Vector2(1f, 0.5f), new Vector2(-20f, 1f), new Vector2(30f, 30f));
            _hintBtn.gameObject.SetActive(false);
        }

        /// <summary>İpucu düğmesi: <paramref name="visible"/> normal bölümde; <paramref name="used"/> hak alındıysa soluk.</summary>
        public void SetHintButton(bool visible, bool used)
        {
            if (_hintBtn == null) return;
            _hintBtn.gameObject.SetActive(visible);
            _hintGroup.alpha = used ? 0.5f : 1f;
            LayoutBottom();
        }

        /// <summary>Testler: ipucu düğmesi görünür mü.</summary>
        public bool HintButtonVisible => _hintBtn != null && _hintBtn.gameObject.activeSelf;

        // ---- vr1 P0: arayüz denetimini gösteren ipucu işareti (tepsi yuvası, rulo, boya) ------------------------

        /// <summary>İpucunun gösterdiği alt çubuk denetimi (tahta hücresi değil).</summary>
        public enum HintUi { None, TraySlot, Roll, Paint }

        private Image _hintMark;
        private HintUi _hintMarkOn;

        /// <summary>Altın nabız halkası denetimin üstüne (çocuk olarak) yerleşir; dokunuşu engellemez.</summary>
        public void ShowHintMark(HintUi target, int slot = 0)
        {
            Button b = target == HintUi.TraySlot && slot >= 0 && slot < _traySlots.Length ? _traySlots[slot]
                : target == HintUi.Roll ? _roll : target == HintUi.Paint ? _paint : null;
            if (b == null || !b.gameObject.activeInHierarchy) { HideHintMark(); return; }
            if (_hintMark == null)
            {
                _hintMark = UiFactory.Image("HintMark", b.transform, UiArt.Icon(UiIcon.Ring), new Color(1f, 0.80f, 0.25f, 1f));
                _hintMark.raycastTarget = false;
                _hintMark.preserveAspect = true;
            }
            _hintMark.transform.SetParent(b.transform, false);
            var size = ((RectTransform)b.transform).rect.size;
            float d = Mathf.Max(size.x, size.y) * 1.45f;
            UiFactory.Place(_hintMark.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(d, d));
            _hintMark.transform.SetAsLastSibling();
            _hintMark.gameObject.SetActive(true);
            _hintMarkOn = target;
        }

        public void HideHintMark()
        {
            if (_hintMark != null) _hintMark.gameObject.SetActive(false);
            _hintMarkOn = HintUi.None;
        }

        /// <summary>Testler: gösterilen arayüz ipucu hedefi.</summary>
        public HintUi HintMarkTarget => _hintMark != null && _hintMark.gameObject.activeInHierarchy ? _hintMarkOn : HintUi.None;

        private void TickHintMark()
        {
            if (_hintMark == null || !_hintMark.gameObject.activeSelf) return;
            float t = Time.unscaledTime * 1.6f % 1f;
            _hintMark.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.12f, t);
            _hintMark.color = new Color(1f, 0.80f, 0.25f, Mathf.Lerp(1f, 0.35f, t));
        }

        /// <summary>Krem ampul glifi (128 px, 4× örnekli kenar).</summary>
        private static Sprite Bulb
        {
            get
            {
                if (_bulb) return _bulb;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "hint-bulb" };
                var col = Ui3.Ivory;
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < 4; sy++)
                    for (int sx = 0; sx < 4; sx++)
                        if (InBulb((x + (sx + 0.5f) / 4f) / n, (y + (sy + 0.5f) / 4f) / n)) hit++;
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, hit / 16f));
                }
                tex.Apply();
                return _bulb = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
        }

        /// <summary>Ampul: tepe dairesi, daralan boyun, iki duy çizgisi ve uç; iç parıltı boşluğu.</summary>
        private static bool InBulb(float u, float v)
        {
            float dx = u - 0.5f, dy = v - 0.62f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            // Parıltı boşluğu (sol üstte kavis).
            float gx = u - 0.43f, gy = v - 0.68f, gr = Mathf.Sqrt(gx * gx + gy * gy);
            if (gr > 0.13f && gr < 0.18f && gx < 0f && gy > -0.02f) return false;
            if (r < 0.29f) return true;
            // Boyun: dairenin altından duy üstüne daralır (v 0.30..0.42).
            if (v > 0.30f && v < 0.44f)
            {
                float half = Mathf.Lerp(0.13f, 0.2f, (v - 0.30f) / 0.14f);
                if (Mathf.Abs(dx) < half) return true;
            }
            // Duy çizgileri.
            if (v > 0.22f && v < 0.28f && Mathf.Abs(dx) < 0.14f) return true;
            if (v > 0.13f && v < 0.19f && Mathf.Abs(dx) < 0.12f) return true;
            if (v > 0.06f && v < 0.11f && Mathf.Abs(dx) < 0.06f) return true;
            return false;
        }
    }
}
