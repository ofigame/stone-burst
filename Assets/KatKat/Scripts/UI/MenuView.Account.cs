using System;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Hesap düğmeleri ve menü bildirimi (kullanıcı, 2026-11-08): Ayarlar'da "İlerlemeyi kaydet", Sıralama'da
    /// "Sıralama için kayıt ol" (giriş yoksa). Bildirim menü tuvalinde (HUD bildirimi menü sayfasının altında kalır).
    /// </summary>
    public sealed partial class MenuView
    {
        public Action SaveProgressPressed, RankSignInPressed;
        /// <summary>Sıralamada kayıt düğmesi gösterilsin mi (oyuncu girişli değilse true).</summary>
        public Func<bool> NeedsSignIn;
        /// <summary>Ayarlar → "Gizlilik seçenekleri" satırı (yalnız UMP gerekli gördüğünde).</summary>
        public Func<bool> PrivacyOptionsVisible;
        public Action PrivacyOptionsPressed;

        private RectTransform _toastBox;
        private TMPro.TextMeshProUGUI _toastText;
        private float _toastUntil;

        /// <summary>Menü üstünde kısa bildirim (alt sekmelerin üstünde, ~2,4 sn).</summary>
        public void ShowToast(string text, float seconds = 2.4f)
        {
            if (_toastBox == null)
            {
                var bg = UiFactory.Image("MenuToast", _safe, Ui3.Rounded(40f, Ui3.Ink, Ui3.Ink, 2f, Ui3.Brass), new Color(1f, 1f, 1f, 0.95f), true);
                bg.pixelsPerUnitMultiplier = 1f;
                bg.raycastTarget = false;
                _toastBox = bg.rectTransform;
                UiFactory.Place(_toastBox, new Vector2(0.5f, 0f), new Vector2(0f, ShellBarH + 120f), new Vector2(900f, 100f));
                _toastText = U3Text("Text", _toastBox, "", 34, true, Ui3.OnTeal);
                _toastText.textWrappingMode = TMPro.TextWrappingModes.Normal;
                UiFactory.Stretch(_toastText.rectTransform, 30f, 30f, 8f, 8f);
                FitText(_toastText, 22);
            }
            Loc.Set(_toastText, text);
            _toastBox.SetAsLastSibling();
            _toastBox.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + seconds;
        }

        /// <summary>Testler: görünen menü bildirimi (yoksa null).</summary>
        public string ToastText => _toastBox != null && _toastBox.gameObject.activeSelf ? _toastText.text : null;

        private void TickToast()
        {
            if (_toastBox != null && _toastBox.gameObject.activeSelf && Time.unscaledTime > _toastUntil) _toastBox.gameObject.SetActive(false);
        }

        /// <summary>Ayarlar kartındaki "İlerlemeyi kaydet" satırı (ui3 satır düzeni: simge yok, sağda ikincil düğme).</summary>
        private void SaveRowUi3(RectTransform card, float y)
        {
            var row = UiFactory.Rect("SaveProgress", card);
            UiFactory.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(748f, 132f));
            var ic = UiFactory.Image("Icon", row, Ui3.Art("ikon-dugmesi"), Color.white);
            ic.preserveAspect = true;
            ic.raycastTarget = false;
            UiFactory.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(72f, 72f));
            var up = U3Text("Arrow", ic.transform, "↑", 40, true, Ui3.OnTeal);
            UiFactory.Stretch(up.rectTransform);
            var t = U3Text("Label", row, Loc.T("acct.save"), 38, true, Ui3.Ink, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(104f + 170f, 0f), new Vector2(340f, 72f));
            FitText(t, 26);
            Ui1.Button("Save", row, Loc.T("acct.save.btn"), true, new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(280f, 88f), () => SaveProgressPressed?.Invoke(), null, 38);
        }

        /// <summary>Ayarlar kartındaki "Gizlilik seçenekleri" satırı (reklam rızası; Google UMP formu).</summary>
        private void PrivacyRowUi3(RectTransform card, float y)
        {
            var row = UiFactory.Rect("PrivacyOptions", card);
            UiFactory.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(748f, 132f));
            var ic = UiFactory.Image("Icon", row, Ui3.Art("ikon-dugmesi"), Color.white);
            ic.preserveAspect = true;
            ic.raycastTarget = false;
            UiFactory.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(72f, 72f));
            var mark = U3Text("Mark", ic.transform, "i", 40, true, Ui3.OnTeal);
            UiFactory.Stretch(mark.rectTransform);
            var t = U3Text("Label", row, Loc.T("ui.privacy"), 38, true, Ui3.Ink, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(104f + 170f, 0f), new Vector2(340f, 72f));
            FitText(t, 26);
            Ui1.Button("Manage", row, Loc.T("ui.privacy.btn"), true, new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(280f, 88f), () => PrivacyOptionsPressed?.Invoke(), null, 38);
        }

        /// <summary>Sıralama: kendi kartının üstünde "Sıralama için kayıt ol" (giriş yoksa).</summary>
        private void RankSignInButton(RectTransform page)
        {
            if (NeedsSignIn == null || !NeedsSignIn()) return;
            var b = Ui1.Button("SignIn", page, Loc.T("acct.signin.rank"), true, new Vector2(0.5f, 0f),
                new Vector2(0f, RankCardBottom + RankCardH + 61f), new Vector2(600f, 60f), () => RankSignInPressed?.Invoke(), null, 30); // liste ile "SENİN SIRAN" başlığı arasındaki boşluk (üstüne binmesin, 2026-11-09)
            b.transform.SetAsLastSibling();
        }
    }
}
