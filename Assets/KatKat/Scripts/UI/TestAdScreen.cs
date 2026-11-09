using System;
using KatKat.Meta;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// v12 TEST reklam ekranı (kullanıcı, 2026-11-08: "reklam yerlerine test reklamı ekranı yap"). Gerçek AdMob +
    /// Unity Ads arabulucusu bağlanana kadar her reklam yerinde bu ekran çıkar; gerçek SDK aynı arayüzü uygular.
    ///
    /// Ödüllü: önce "Reklam aranıyor… N" (en çok <see cref="AdConfig.SearchSeconds"/> sn). Reklam bu sürede
    /// "bulunursa" tam ekran TEST REKLAMI, <see cref="RewardedSeconds"/> sn geri sayım, sonra "Ödülü al" (ödül) ya da
    /// erken kapatma (iptal). Bulunamazsa <see cref="AdOutcome.Failed"/>. Geçiş: hazırsa tam ekran, <see cref="InterstitialSeconds"/>
    /// sn sonra kapatılabilir. Deneme ayarları: <see cref="FillSeconds"/> (sahte bulunma süresi), <see cref="NoFill"/>.
    /// Zaman: gerçek zaman (Update) ya da yakalamada <see cref="Advance"/>.
    /// </summary>
    public sealed class TestAdScreen : MonoBehaviour, IRewardedAds, IInterstitialAds
    {
        public const float RewardedSeconds = 5f, InterstitialSeconds = 3f;
        /// <summary>Sahte reklamın bulunma süresi (sn). Arama süresinden uzunsa "bulunamadı".</summary>
        public static float FillSeconds = 1.2f;
        /// <summary>Deneme: hiç reklam bulunmasın (yedek akışları sınamak için).</summary>
        public static bool NoFill;

        private enum Mode { None, Search, Rewarded, Interstitial }

        private Canvas _canvas;
        private RectTransform _root;
        private Text _title, _sub, _count;
        private Button _claim, _close;
        private Mode _mode;
        private float _t;
        private string _placement;
        private Action<AdOutcome, string> _rewardDone;
        private Action<bool> _interDone;
        private int _serial;

        public AdConfig Config = new AdConfig();
        public bool IsOpen => _mode != Mode.None;
        public string Placement => _placement;
        /// <summary>Ekrandaki aşama (test/yakalama): "search", "rewarded", "interstitial" ya da "".</summary>
        public string Stage => _mode == Mode.Search ? "search" : _mode == Mode.Rewarded ? "rewarded" : _mode == Mode.Interstitial ? "interstitial" : "";

        public static TestAdScreen Create(Camera worldCam, Transform parent)
        {
            var go = new GameObject("TestAdScreen", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5;
            var s = go.AddComponent<TestAdScreen>();
            s.Build(worldCam);
            return s;
        }

        private void Build(Camera worldCam)
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = worldCam;
            _canvas.planeDistance = 2f;
            _canvas.sortingOrder = 950;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(HudView.RefWidth, HudView.RefHeight);
            scaler.matchWidthOrHeight = 0f;
            gameObject.AddComponent<GraphicRaycaster>();
            _root = UiFactory.Stretch(UiFactory.Rect("Root", transform));
            _root.gameObject.SetActive(false);
        }

        /// <summary>Yakalama/editör: canvas ölçeği elle (oynatma dışında CanvasScaler çalışmaz).</summary>
        public Canvas Canvas => _canvas;

        // ---- IRewardedAds / IInterstitialAds ---------------------------------------------------------------

        public void Show(string placement, Action<AdOutcome, string> done)
        {
            if (IsOpen) { done?.Invoke(AdOutcome.Failed, null); return; }
            _placement = placement;
            _rewardDone = done;
            _mode = Mode.Search;
            _t = 0f;
            Draw();
        }

        public void ShowInterstitial(string placement, Action<bool> done)
        {
            if (IsOpen || NoFill) { done?.Invoke(false); return; } // geçiş reklamı beklenmez: hazır değilse atlanır
            _placement = placement;
            _interDone = done;
            _mode = Mode.Interstitial;
            _t = 0f;
            Draw();
        }

        private void Update()
        {
            if (_mode != Mode.None) Advance(Time.unscaledDeltaTime * 1000f);
        }

        public void Advance(float ms)
        {
            if (_mode == Mode.None) return;
            _t += ms / 1000f;
            switch (_mode)
            {
                case Mode.Search:
                    if (!NoFill && _t >= FillSeconds && FillSeconds <= Config.SearchSeconds) { _mode = Mode.Rewarded; _t = 0f; Draw(); return; }
                    if (_t >= Config.SearchSeconds) { FinishRewarded(AdOutcome.Failed); return; }
                    _count.text = Mathf.CeilToInt(Config.SearchSeconds - _t).ToString();
                    break;
                case Mode.Rewarded:
                {
                    float left = RewardedSeconds - _t;
                    _count.text = left > 0f ? Mathf.CeilToInt(left).ToString() : "";
                    _claim.gameObject.SetActive(left <= 0f);
                    break;
                }
                case Mode.Interstitial:
                {
                    float left = InterstitialSeconds - _t;
                    _count.text = left > 0f ? Mathf.CeilToInt(left).ToString() : "";
                    _close.interactable = left <= 0f;
                    _close.gameObject.SetActive(left <= 0f);
                    break;
                }
            }
        }

        /// <summary>Kapat (X): ödüllüde süre dolmadan iptal; geçişte süre dolduysa kapanır.</summary>
        public void PressClose()
        {
            if (_mode == Mode.Rewarded) FinishRewarded(_t >= RewardedSeconds ? AdOutcome.Rewarded : AdOutcome.Cancelled);
            else if (_mode == Mode.Search) FinishRewarded(AdOutcome.Cancelled);
            else if (_mode == Mode.Interstitial && _t >= InterstitialSeconds) FinishInterstitial();
        }

        /// <summary>"Ödülü al" (yalnız süre dolunca görünür).</summary>
        public void PressClaim()
        {
            if (_mode == Mode.Rewarded && _t >= RewardedSeconds) FinishRewarded(AdOutcome.Rewarded);
        }

        private void FinishRewarded(AdOutcome o)
        {
            var done = _rewardDone;
            _rewardDone = null;
            Hide();
            done?.Invoke(o, o == AdOutcome.Rewarded ? $"test-{_placement}-{++_serial}-{DateTime.UtcNow.Ticks}" : null);
        }

        private void FinishInterstitial()
        {
            var done = _interDone;
            _interDone = null;
            Hide();
            done?.Invoke(true);
        }

        private void Hide()
        {
            _mode = Mode.None;
            for (int i = _root.childCount - 1; i >= 0; i--) UiFactory.Kill(_root.GetChild(i).gameObject);
            _root.gameObject.SetActive(false);
        }

        // ---- Çizim ---------------------------------------------------------------------------------------

        private static readonly Color Navy = new Color32(0x12, 0x2E, 0x6E, 0xFF);
        private static readonly Color Cream = new Color32(0xFF, 0xF4, 0xE0, 0xFF);

        private void Draw()
        {
            for (int i = _root.childCount - 1; i >= 0; i--) UiFactory.Kill(_root.GetChild(i).gameObject);
            _root.gameObject.SetActive(true);
            // Tam ekran engel: arkadaki oyuna dokunulmaz.
            var block = UiFactory.Image("Block", _root, UiArt.Box, new Color(0.04f, 0.06f, 0.14f, _mode == Mode.Search ? 0.72f : 0.96f));
            block.raycastTarget = true;
            UiFactory.Stretch(block.rectTransform, -200f, -200f, -400f, -400f);
            if (Ui3.Ready) { DrawUi3(block); return; }

            if (_mode == Mode.Search)
            {
                var card = UiFactory.Image("Card", _root, UiArt.Box, Cream, true);
                UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 420f));
                _title = UiFactory.Title("Title", card.transform, Loc.T("ad.searching"), 50, Navy, Color.white);
                UiFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(680f, 70f));
                var ring = UiFactory.Image("Ring", card.transform, UiArt.Circle, new Color32(0x0A, 0x8F, 0xD8, 0xFF));
                UiFactory.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(150f, 150f));
                _count = UiFactory.Title("Count", ring.transform, Mathf.CeilToInt(Config.SearchSeconds).ToString(), 80, Color.white, Navy);
                UiFactory.Stretch(_count.rectTransform);
                _sub = UiFactory.Text("Sub", card.transform, Loc.T("ad.searching.sub"), 28, Navy, TextAnchor.MiddleCenter, FontStyle.Normal);
                UiFactory.Place(_sub.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(680f, 60f));
                _sub.enableAutoSizing = true; _sub.fontSizeMin = 18; _sub.fontSizeMax = 28;
                AddClose(card.rectTransform, new Vector2(-14f, -14f));
                return;
            }

            bool rewarded = _mode == Mode.Rewarded;
            var frame = UiFactory.Image("Ad", _root, UiArt.Box, new Color32(0x1E, 0x24, 0x3A, 0xFF), true);
            UiFactory.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1500f));
            var tag = UiFactory.Image("Tag", frame.transform, UiArt.Box, new Color32(0xFF, 0xC8, 0x3A, 0xFF), true);
            UiFactory.Place(tag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(560f, 96f));
            var tt = UiFactory.Title("TagText", tag.transform, Loc.T("ad.test"), 52, Navy, Color.white);
            UiFactory.Stretch(tt.rectTransform);
            _title = UiFactory.Title("Kind", frame.transform, Loc.T(rewarded ? "ad.rewarded" : "ad.interstitial"), 48, Color.white, Navy);
            UiFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(860f, 70f));
            _sub = UiFactory.Text("Placement", frame.transform, _placement, 34, new Color(0.75f, 0.82f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Place(_sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(860f, 50f));
            var ring2 = UiFactory.Image("Ring", frame.transform, UiArt.Circle, new Color32(0x0A, 0x8F, 0xD8, 0xFF));
            UiFactory.Place(ring2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(240f, 240f));
            _count = UiFactory.Title("Count", ring2.transform, "", 120, Color.white, Navy);
            UiFactory.Stretch(_count.rectTransform);
            var note = UiFactory.Text("Note", frame.transform, Loc.T("ad.test.note"), 28, new Color(0.75f, 0.82f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(820f, 90f));
            note.enableAutoSizing = true; note.fontSizeMin = 18; note.fontSizeMax = 28;

            _claim = UiFactory.SpriteButton("Claim", frame.transform, UiArt.Box, new Vector2(520f, 120f), PressClaim);
            _claim.GetComponent<Image>().color = new Color32(0x2F, 0xB8, 0x48, 0xFF);
            UiFactory.Place((RectTransform)_claim.transform, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(520f, 120f));
            var ct = UiFactory.Title("Text", _claim.transform, Loc.T("ad.claim"), 46, Color.white, new Color32(0x1E, 0x6E, 0x2C, 0xFF));
            UiFactory.Stretch(ct.rectTransform);
            _claim.gameObject.SetActive(false);
            if (!rewarded) _claim.gameObject.SetActive(false);
            AddClose(frame.rectTransform, new Vector2(-20f, -20f));
            if (!rewarded) { _close.gameObject.SetActive(false); }
            Advance(0f);
        }

        /// <summary>
        /// ui3 (sayfalar/18–20, modal-varyantlari adLoading/testAd): karartma #111C24/0.72; arama kartı 760×460 seramik
        /// (başlık, sayaç halkası 152, not); test reklamı 860×1480 seramik kart (TEST REKLAMI etiketi, tür, yer kimliği,
        /// gösterge 260, SDK notu, ödüllüde "Ödülü al" 748×116). Kapat ve süreler aynı.
        /// </summary>
        private void DrawUi3(Image block)
        {
            block.color = Ui3.ScrimColor;
            float Y(float top, float h) => -(top + h * 0.5f);
            if (_mode == Mode.Search)
            {
                var card = UiFactory.Image("Card", _root, Ui1.Art("panel-modal"), Color.white, true);
                card.pixelsPerUnitMultiplier = card.sprite.rect.width / 760f;
                UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 460f));
                _title = Ui1.Label("Title", card.transform, Loc.T("ad.searching"), 50, Ui3.Ink);
                UiFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(56f, 80f)), new Vector2(664f, 80f));
                _title.enableAutoSizing = true; _title.fontSizeMax = 50; _title.fontSizeMin = 32;
                var ring = UiFactory.Image("Ring", card.transform, UiArt.Circle, new Color32(0x0A, 0x8F, 0xD8, 0xFF));
                UiFactory.Place(ring.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(160f, 152f)), new Vector2(152f, 152f));
                _count = Ui1.Label("Count", ring.transform, Mathf.CeilToInt(Config.SearchSeconds).ToString(), 64, Ui3.OnTeal);
                UiFactory.Stretch(_count.rectTransform);
                _sub = Ui1.Label("Sub", card.transform, Loc.T("ad.searching.sub"), 28, Ui3.InkSecondary, false);
                UiFactory.Place(_sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(344f, 72f)), new Vector2(664f, 72f));
                _sub.enableAutoSizing = true; _sub.fontSizeMin = 26; _sub.fontSizeMax = 28;
                AddClose(card.rectTransform, new Vector2(-14f, -14f));
                return;
            }
            bool rewarded = _mode == Mode.Rewarded;
            var frame = UiFactory.Image("Ad", _root, Ui1.Art("card-ivory"), Color.white, true);
            frame.pixelsPerUnitMultiplier = 1f;
            UiFactory.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 1480f));
            var tag = Ui3.Surface("Tag", frame.transform, 44f, new Color32(0xFF, 0xD7, 0x5A, 0xFF), new Color32(0xF2, 0xB8, 0x3A, 0xFF), 88f, 3f, Ui3.Brass);
            UiFactory.Place(tag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(64f, 88f)), new Vector2(560f, 88f));
            var tt = Ui1.Label("TagText", tag.transform, Loc.T("ad.test"), 46, Ui3.Ink);
            UiFactory.Stretch(tt.rectTransform, 20, 20, 4, 6);
            tt.enableAutoSizing = true; tt.fontSizeMax = 46; tt.fontSizeMin = 28;
            _title = Ui1.Label("Kind", frame.transform, Loc.T(rewarded ? "ad.rewarded" : "ad.interstitial"), 54, Ui3.Ink);
            UiFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(200f, 80f)), new Vector2(748f, 80f));
            _sub = Ui1.Label("Placement", frame.transform, _placement, 28, Ui3.InkSecondary, false);
            UiFactory.Place(_sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(300f, 56f)), new Vector2(748f, 56f));
            var ring2 = UiFactory.Image("Ring", frame.transform, UiArt.Circle, new Color32(0x0A, 0x8F, 0xD8, 0xFF));
            UiFactory.Place(ring2.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(568f, 260f)), new Vector2(260f, 260f));
            _count = Ui1.Label("Count", ring2.transform, "", 96, Ui3.OnTeal);
            UiFactory.Stretch(_count.rectTransform);
            var note = Ui1.Label("Note", frame.transform, Loc.T("ad.test.note"), 26, Ui3.Ink, false);
            UiFactory.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(1100f, 112f)), new Vector2(748f, 112f));
            note.textWrappingMode = TMPro.TextWrappingModes.Normal;
            _claim = Ui1.Button("Claim", frame.transform, Loc.T("ad.claim"), true, new Vector2(0.5f, 1f), new Vector2(0f, Y(1288f, 116f)), new Vector2(748f, 116f), PressClaim);
            _claim.gameObject.SetActive(false);
            AddClose(frame.rectTransform, new Vector2(-24f, -24f));
            if (!rewarded) _close.gameObject.SetActive(false);
            Advance(0f);
        }

        private void AddClose(RectTransform parent, Vector2 pos)
        {
            if (Ui3.Ready)
            {
                // ui3: fildişi daire (pirinç kenar) içinde lacivert X; görsel 88, dokunma 104.
                _close = UiFactory.SpriteButton("Close", parent, Ui3.Rounded(44f, Ui3.IvoryTop, Ui3.IvoryBottom, 3f, Ui3.Brass), new Vector2(88f, 88f), PressClose);
                _close.GetComponent<Image>().type = Image.Type.Sliced;
                UiFactory.Place((RectTransform)_close.transform, new Vector2(1f, 1f), pos - new Vector2(44f, 44f), new Vector2(88f, 88f));
                var x3 = Ui1.Label("X", _close.transform, "X", 44, Ui3.Ink);
                UiFactory.Stretch(x3.rectTransform, 0, 0, 0, 4);
                return;
            }
            _close = UiFactory.SpriteButton("Close", parent, UiArt.Circle, new Vector2(84f, 84f), PressClose);
            _close.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            UiFactory.Place((RectTransform)_close.transform, new Vector2(1f, 1f), pos - new Vector2(42f, 42f), new Vector2(84f, 84f));
            var x = UiFactory.Title("X", _close.transform, "X", 48, Navy, Color.white);
            UiFactory.Stretch(x.rectTransform);
        }
    }
}
