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
        /// <summary>Ayarlar → "Hesabı sil" → onay penceresinde "Sil" (Apple 5.1.1(v); 2026-11-13).</summary>
        public Action DeleteAccountPressed;
        private RectTransform _deleteCard;
        /// <summary>Sıralamada kayıt düğmesi gösterilsin mi (oyuncu girişli değilse true).</summary>
        public Func<bool> NeedsSignIn;
        /// <summary>Hesap özellikleri (sıralama kaydı, "İlerlemeyi kaydet") var mı; AppGallery derlemesinde false.</summary>
        public Func<bool> AccountsEnabled;
        private bool AccountsOn => AccountsEnabled == null || AccountsEnabled();
        /// <summary>Bulut kaydı ("İlerlemeyi kaydet") var mı; AppGallery'de false (anonim hesap cihaza bağlı).</summary>
        public Func<bool> CloudSaveEnabled;
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
                var bg = UiFactory.Image("MenuToast", _safe, Ui3.Rounded(40f, Ui3.Ink, Ui3.Ink, 2f, Ui3.Brass), Color.white, true);
                bg.pixelsPerUnitMultiplier = 1f;
                bg.raycastTarget = false;
                _toastBox = bg.rectTransform;
                UiFactory.Place(_toastBox, new Vector2(0.5f, 0f), new Vector2(0f, ShellBarH + 120f), new Vector2(900f, 100f));
                _toastText = U3Text("Text", _toastBox, "", 34, true, Ui3.OnTeal);
                _toastText.textWrappingMode = TMPro.TextWrappingModes.Normal;
                UiFactory.Stretch(_toastText.rectTransform, 30f, 30f, 8f, 8f);
                FitText(_toastText, 22);
                // 2026-11-21 (kullanıcı: "mağazadan güçlendirici aldığımda çıkan bilgilendirme yazısı ekrandaki objelerin altında
                // kalıyor"): satın alımdan sonra sayfa yeniden kurulup üstüne biniyordu. Kendi tuvali her zaman en üstte çizer.
                var cv = _toastBox.gameObject.AddComponent<Canvas>();
                cv.overrideSorting = true;
                cv.sortingOrder = (_canvas != null ? _canvas.sortingOrder : 0) + 200;
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

        /// <summary>Ayarlar kartındaki "Hesabı sil" satırı: düğme önce onay penceresini açar.</summary>
        private void DeleteRowUi3(RectTransform card, float y)
        {
            var row = UiFactory.Rect("DeleteAccount", card);
            UiFactory.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(748f, 132f));
            var ic = UiFactory.Image("Icon", row, Ui3.Art("ikon-dugmesi"), Color.white);
            ic.preserveAspect = true;
            ic.raycastTarget = false;
            UiFactory.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(72f, 72f));
            var mark = U3Text("Mark", ic.transform, "×", 44, true, Ui3.OnTeal);
            UiFactory.Stretch(mark.rectTransform);
            var t = U3Text("Label", row, Loc.T("acct.delete"), 38, true, Ui3.Ink, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(104f + 170f, 0f), new Vector2(340f, 72f));
            FitText(t, 26);
            Ui1.Button("Delete", row, Loc.T("acct.delete.btn"), false, new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(280f, 88f), ShowDeleteAccountConfirm, null, 38);
        }

        /// <summary>Silme onayı: ne silinir, ne kalır, geri alınamaz; "Sil" ve "Vazgeç".</summary>
        public void ShowDeleteAccountConfirm()
        {
            CloseDeleteAccountConfirm();
            const float W = 860f, H = 690f;
            var card = U3Modal(_page, "DeleteAccountCard", W, H, CloseDeleteAccountConfirm, out var root);
            _deleteCard = root;
            var title = U3Text("Title", card, Loc.T("acct.delete.title"), 52, true, Ui3.Ink);
            U3In(title.rectTransform, W, 56f, 52f, 748f, 80f);
            FitText(title, 32);
            var info = U3Text("Info", card, Loc.T("acct.delete.info"), 32, false, Ui3.InkSecondary);
            info.textWrappingMode = TMPro.TextWrappingModes.Normal;
            U3In(info.rectTransform, W, 60f, 150f, 740f, 200f);
            FitText(info, 22);
            Ui1.Button("Confirm", card, Loc.T("acct.delete.btn"), true, new Vector2(0.5f, 1f), new Vector2(0f, -(380f + 56f)), new Vector2(748f, 112f),
                () => { CloseDeleteAccountConfirm(); DeleteAccountPressed?.Invoke(); }, null, 42);
            Ui1.Button("Cancel", card, Loc.T("acct.delete.cancel"), false, new Vector2(0.5f, 1f), new Vector2(0f, -(510f + 50f)), new Vector2(748f, 100f), CloseDeleteAccountConfirm, null, 38);
        }

        public void CloseDeleteAccountConfirm()
        {
            if (_deleteCard != null) UiFactory.Kill(_deleteCard.gameObject);
            _deleteCard = null;
        }

        /// <summary>Testler: silme onayı açık mı.</summary>
        public bool DeleteConfirmShown => _deleteCard != null;

        // ---- Oyuncunun seçtiği ad (AppGallery; kullanıcı 2026-11-19: "huaweide sıralamaya üye olduğunda ismini kendi versin") ----
        /// <summary>Düzenlenebilir ad (girişli, adı oyuncunun seçtiği hesap); null: Ayarlar'da ad satırı yok.</summary>
        public Func<string> EditableName;
        public Action ChangeNamePressed;
        private RectTransform _nameCard;
        private TMPro.TextMeshProUGUI _nameRowValue;
        private bool NameRowOn => EditableName != null && !string.IsNullOrEmpty(EditableName());

        /// <summary>Ayarlar kartındaki "Sıralamadaki adın" satırı: üstte küçük başlık, altında ad; sağda "Değiştir".</summary>
        private void NameRowUi3(RectTransform card, float y)
        {
            var row = UiFactory.Rect("PlayerName", card);
            UiFactory.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(748f, 132f));
            var ic = UiFactory.Image("Icon", row, Ui3.Art("ikon-dugmesi"), Color.white);
            ic.preserveAspect = true;
            ic.raycastTarget = false;
            UiFactory.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(72f, 72f));
            var mark = U3Text("Mark", ic.transform, "Aa", 30, true, Ui3.OnTeal);
            UiFactory.Stretch(mark.rectTransform);
            var cap = U3Text("Caption", row, Loc.T("acct.name"), 26, false, Ui3.InkSecondary, TextAnchor.MiddleLeft);
            UiFactory.Place(cap.rectTransform, new Vector2(0f, 0.5f), new Vector2(104f + 170f, 22f), new Vector2(340f, 40f));
            FitText(cap, 18);
            _nameRowValue = U3Text("Value", row, EditableName?.Invoke() ?? "", 36, true, Ui3.Ink, TextAnchor.MiddleLeft);
            UiFactory.Place(_nameRowValue.rectTransform, new Vector2(0f, 0.5f), new Vector2(104f + 170f, -20f), new Vector2(340f, 52f));
            FitText(_nameRowValue, 22);
            Ui1.Button("Change", row, Loc.T("acct.name.btn"), false, new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(280f, 88f), () => ChangeNamePressed?.Invoke(), null, 38);
        }

        /// <summary>Ad değişince Ayarlar'daki ad satırını günceller (sayfa açık değilse bir şey yapmaz).</summary>
        public void RefreshSettings()
        {
            if (_nameRowValue != null) _nameRowValue.text = EditableName?.Invoke() ?? "";
        }

        /// <summary>
        /// Ad penceresi: başlık, açıklama, metin kutusu (en çok <see cref="KatKat.Game.PlayerNameRules.MaxLength"/>),
        /// hata satırı, "Kaydet" ve <paramref name="firstTime"/>'da "Sonra" (yoksa "Vazgeç"). Ad
        /// <see cref="KatKat.Game.PlayerNameRules"/>'tan geçmezse pencere kapanmaz, hata yazılır.
        /// </summary>
        public void ShowNameEditor(string current, bool firstTime, Action<string> save, Action later)
        {
            CloseNameEditor();
            const float W = 860f, H = 760f;
            Action skip = () => { CloseNameEditor(); later?.Invoke(); };
            var card = U3Modal(_page, "NameCard", W, H, skip, out var root);
            _nameCard = root;
            var title = U3Text("Title", card, Loc.T("acct.name.title"), 50, true, Ui3.Ink);
            title.textWrappingMode = TMPro.TextWrappingModes.Normal;
            U3In(title.rectTransform, W, 56f, 46f, 748f, 110f);
            FitText(title, 28);
            var info = U3Text("Info", card, Loc.F("acct.name.info", KatKat.Game.PlayerNameRules.MinLength, KatKat.Game.PlayerNameRules.MaxLength), 30, false, Ui3.InkSecondary);
            info.textWrappingMode = TMPro.TextWrappingModes.Normal;
            U3In(info.rectTransform, W, 60f, 160f, 740f, 100f);
            FitText(info, 20);

            var box = UiFactory.Image("Field", card, Ui3.Rounded(30f, Color.white, Ui3.IvoryTop, 3f, Ui3.Brass), Color.white, true);
            box.pixelsPerUnitMultiplier = 1f;
            U3In(box.rectTransform, W, 56f, 280f, 748f, 120f);
            box.gameObject.SetActive(false);
            var area = UiFactory.Stretch(UiFactory.Rect("TextArea", box.transform), 32f, 32f, 12f, 12f);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = U3Text("Placeholder", area, Loc.T("acct.name.hint"), 40, false, Ui3.InkMuted, TextAnchor.MiddleLeft);
            UiFactory.Stretch(ph.rectTransform);
            ph.enableAutoSizing = false;
            var tx = U3Text("Text", area, "", 46, true, Ui3.Ink, TextAnchor.MiddleLeft);
            UiFactory.Stretch(tx.rectTransform);
            tx.enableAutoSizing = false;
            tx.richText = false;
            var input = box.gameObject.AddComponent<TMPro.TMP_InputField>();
            input.textViewport = area;
            input.textComponent = tx;
            input.placeholder = ph;
            input.lineType = TMPro.TMP_InputField.LineType.SingleLine;
            input.characterLimit = KatKat.Game.PlayerNameRules.MaxLength;
            input.richText = false;
            box.gameObject.SetActive(true);
            // İlk kez soruluyorsa kutu boş başlar (yer tutucu görünür); değiştirmede mevcut ad yazılı gelir.
            input.text = firstTime ? "" : (current ?? "");
            _nameInput = input;

            var err = U3Text("Error", card, "", 28, true, Ui3.Coral);
            err.textWrappingMode = TMPro.TextWrappingModes.Normal;
            U3In(err.rectTransform, W, 60f, 408f, 740f, 56f);
            FitText(err, 20);
            _nameError = err;

            Action submit = () =>
            {
                string clean = KatKat.Game.PlayerNameRules.Clean(input.text);
                string problem = KatKat.Game.PlayerNameRules.Problem(clean);
                if (problem != null) { Loc.Set(err, Loc.F(problem, KatKat.Game.PlayerNameRules.MinLength)); return; }
                CloseNameEditor();
                save?.Invoke(clean);
            };
            input.onSubmit.AddListener(_ => submit());
            Ui1.Button("Save", card, Loc.T("acct.name.save"), true, new Vector2(0.5f, 1f), new Vector2(0f, -(480f + 56f)), new Vector2(748f, 112f), submit, null, 42);
            Ui1.Button("Later", card, Loc.T(firstTime ? "rank.remind.later" : "acct.delete.cancel"), false, new Vector2(0.5f, 1f), new Vector2(0f, -(612f + 46f)), new Vector2(748f, 92f), skip, null, 36);
        }

        private TMPro.TMP_InputField _nameInput;
        private TMPro.TextMeshProUGUI _nameError;

        public void CloseNameEditor()
        {
            if (_nameCard != null) UiFactory.Kill(_nameCard.gameObject);
            _nameCard = null;
            _nameInput = null;
            _nameError = null;
        }

        /// <summary>Testler: ad penceresi açık mı; metni yazıp "Kaydet"e basar; hata satırı.</summary>
        public bool NameEditorShown => _nameCard != null;
        public void SubmitNameForTest(string text)
        {
            if (_nameInput == null) return;
            _nameInput.text = text;
            _nameCard.Find("Card/Save").GetComponent<Button>().onClick.Invoke();
        }
        public string NameErrorText => _nameError != null ? _nameError.text : null;

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
    }
}
