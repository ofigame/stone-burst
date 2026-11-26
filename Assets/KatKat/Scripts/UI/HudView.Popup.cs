using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// 2026-11-20 (kullanıcı: "oyun tahtasından kazanılan skiller ise görev çubuğu ile tahtanın arasındaki boşlukta olsun. O
    /// boşluğa gelen başka şeyleri ise ona göre ayarlayalım. Mesela bilgi metinleri pop up olsun"):
    /// <list type="bullet">
    /// <item>Güç bandı (<see cref="_skillBar"/>): hedef kartının altında; rulo ve tepsi yuvaları ortalı. Görev şeridi (tehdit
    /// bölümleri) varsa bant onun altına iner; reklam ödülü taş şeridi açıkken bant gizlenir (aynı yer).</item>
    /// <item>Bilgi metni artık açılır pencere: tahtanın üst kısmında, güç bandının hemen altında beliren yuvarlak kart
    /// (yaylanarak büyür, süresi dolunca söner). Dokunma yakalamaz; tahtaya dokunmak engellenmez. Tahta alanından yer almaz.</item>
    /// </list>
    /// </summary>
    public sealed partial class HudView
    {
        /// <summary>Güç bandı yüksekliği (referans px) ve yuva aralığı; simge bandı aşmasın diye ölçek tavanı.</summary>
        public const float SkillBandH = 128f, SkillStep = 190f, SkillScaleMax = 0.76f;
        /// <summary>2026-11-26 (kullanıcı: "Torba ise tahtanın üst kısmındaki altın ve roket rulolarının yerine gelsin ama ne üst alana
        /// nede tahtaya taşmasın … torba şiştiğindede taşma olmasın"): torba bölümünde bant torbanın tam şişmiş ve zıplayan boyuna
        /// göre yüksek.</summary>
        public const float BagBandH = 210f;
        /// <summary>Torbanın bant yüksekliğine oranı (şişme + zıplama payı: zıplama en çok %12).</summary>
        public const float BagBandFill = 0.86f;
        private float BandH => _bucketMode ? BagBandH : SkillBandH;

        private RectTransform _skillBar;
        private bool _reserveHazard;

        /// <summary>Bölümde görev (tehdit) şeridi olacak mı: tahta kurulmadan önce denetleyici söyler; güç bandı altına iner.</summary>
        public bool ReserveHazard
        {
            get => _reserveHazard;
            set { if (_reserveHazard == value) return; _reserveHazard = value; PlaceSkillBar(); }
        }

        /// <summary>Hedef kartının altı (güvenli alan üstünden, referans px).</summary>
        private float BandTop => GoalsBottom + _headShift + ToastGap;

        private float SkillBarTop => BandTop + (_reserveHazard ? HazardH + 8f : 2f);

        /// <summary>Güç bandında görünen öğe var mı (rulo ya da tepsi).</summary>
        public bool SkillsShown => (_roll != null && _roll.gameObject.activeSelf) || (_trayRoot != null && _trayRoot.gameObject.activeSelf) || _bucketMode;

        /// <summary>Tahtanın üstünde ayrılan pay: hedef kartı + (görev şeridi) + (güç bandı).</summary>
        private float TopReserve => SkillsShown ? SkillBarTop + BandH + 4f : BandTop + (_reserveHazard ? HazardH + 8f : 10f);

        private void PlaceSkillBar()
        {
            if (_skillBar == null) return;
            _skillBar.anchoredPosition = new Vector2(0f, -SkillBarTop);
            _skillBar.sizeDelta = new Vector2(0f, BandH);
            _skillBar.gameObject.SetActive(_placeRow == null);
        }

        /// <summary>Güç bandının dikdörtgeni (testler, öğretici).</summary>
        public Rect? SkillBarRect() => RectOf(_skillBar);

        // ---- Bilgi açılır penceresi ------------------------------------------------------------------------------------

        private CanvasGroup _toastGroup;
        private float _toastAge = 9f, _toastLife;
        private const float ToastInS = 0.26f, ToastOutS = 0.22f, ToastW = 900f;

        private void BuildToastPopup()
        {
            var plate = MenuView.U3Plate("Toast", _safe, 40f, 120f);
            plate.raycastTarget = false;
            _toastBox = plate.rectTransform;
            _toastBox.anchorMin = _toastBox.anchorMax = new Vector2(0.5f, 1f);
            _toastBox.pivot = new Vector2(0.5f, 1f);
            _toastBox.sizeDelta = new Vector2(ToastW, 120f);
            var sh = plate.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.1f, 0.05f, 0.2f, 0.45f);
            sh.effectDistance = new Vector2(0f, -8f);
            _toastGroup = plate.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.blocksRaycasts = false;
            _toastGroup.interactable = false;
            _toast = UiFactory.Text("Text", plate.transform, "", 34, Ui3.Ink);
            _toast.raycastTarget = false;
            UiFactory.Stretch(_toast.rectTransform, 48, 48, 18, 22);
            _toast.textWrappingMode = TMPro.TextWrappingModes.Normal;
            _toastBox.gameObject.SetActive(false);
        }

        /// <summary>Bilgi metni: tahtanın üstünde açılır kart; <paramref name="seconds"/> sonra söner.</summary>
        public void ShowToast(string text, float seconds = 1.6f)
        {
            Loc.Set(_toast, text);
            _toast.enableAutoSizing = false;
            _toast.fontSize = 34f;
            float pref = _toast.GetPreferredValues(_toast.text, ToastW - 96f, 0f).y;
            float h = Mathf.Clamp(pref + 44f, 112f, 34f * 1.25f * 3f + 44f);
            if (pref + 44f > h) { _toast.enableAutoSizing = true; _toast.fontSizeMax = 34; _toast.fontSizeMin = 20; }
            _toastBox.sizeDelta = new Vector2(ToastW, h);
            // Güç bandı varsa onun altında, yoksa hedef kartının (ya da görev şeridinin) hemen altında: tahtanın üst sırası.
            float y = (SkillsShown && _skillBar.gameObject.activeSelf ? SkillBarTop + BandH : BandTop + (_reserveHazard ? HazardH + 8f : 0f)) + 10f;
            _toastBox.anchoredPosition = new Vector2(0f, -y);
            _toastBox.SetAsLastSibling();
            bool wasOn = _toastBox.gameObject.activeSelf && _toastAge < _toastLife;
            _toastBox.gameObject.SetActive(true);
            _toastLife = seconds + ToastInS;
            // Açıkken yeni metin: yeniden yaylanmadan yerinde kalır (art arda yönergelerde titremesin).
            _toastAge = wasOn ? Mathf.Min(_toastAge, ToastInS) : 0f;
            _toastUntil = Time.unscaledTime + seconds;
            ApplyToast();
        }

        public void HideToast()
        {
            if (_toastBox == null) return;
            _toastBox.gameObject.SetActive(false);
            _toastAge = 9f;
        }

        /// <summary>Açılır pencere açık mı (testler).</summary>
        public bool ToastShown => _toastBox != null && _toastBox.gameObject.activeSelf;
        public string ToastText => ToastShown ? _toast.text : null;

        private void TickToast(float dt)
        {
            if (_toastBox == null || !_toastBox.gameObject.activeSelf) return;
            _toastAge += dt;
            if (_toastAge >= _toastLife + ToastOutS) { HideToast(); return; }
            ApplyToast();
        }

        private void ApplyToast()
        {
            float s, a;
            if (Motion.Reduced) { s = 1f; a = _toastAge > _toastLife ? 0f : 1f; }
            else if (_toastAge < ToastInS) { float k = _toastAge / ToastInS; s = Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(k)); a = Mathf.Clamp01(k * 2.2f); }
            else if (_toastAge > _toastLife) { float k = Mathf.Clamp01((_toastAge - _toastLife) / ToastOutS); s = 1f - 0.08f * k; a = 1f - k; }
            else { s = 1f; a = 1f; }
            _toastBox.localScale = new Vector3(s, s, 1f);
            _toastGroup.alpha = a;
        }
    }
}
