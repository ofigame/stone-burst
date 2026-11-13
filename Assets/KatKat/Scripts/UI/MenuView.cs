using System;
using System.Collections.Generic;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// Ana sayfa, bölüm haritası ve ayarlar (docs/Anasayfa-Bolum-Secenekleri/TASARIM_NOTLARI.md, Oyun-Ici-Temalar,
    /// Görsel Şartname v3 §3–5). Tema ve bölge bölümden gelir: 1–100 Mavi Liman (sahil), 101–200 Yeşil Vadi (bahçe),
    /// 201–300 Kum Vadisi (çöl); sonra sıra tekrar eder.
    /// Ana sayfa: ekran etiketi, logo + iki yanında katman yığınları, kaide üstünde katman heykeli, büyük OYNA
    /// (▶ + gerçek bölüm), bölge kartı, yazılı alt sekmeler. Bölümler: bölge başlık kartı (‹ ›), kaydırılabilir
    /// kıvrımlı patika, yalnızca o bölgenin düğümleri (en çok 100), mevcut bölüm ortada. Numaralar gerçek metindir.
    /// Kayıt veya kural bilgisi tutmaz; kendisine verileni gösterir.
    /// </summary>
    public sealed partial class MenuView : MonoBehaviour
    {
        public event Action<int> LevelChosen;
        /// <summary>Test: bütün düğümler açık ve seçilebilir.</summary>
        public bool UnlockAll;
        /// <summary>v5: bölüm indeksinin en iyi yıldızı (0–3); tamamlanan düğümün altında küçük yıldızlar.</summary>
        public Func<int, int> StarsOf;
        public event Action<bool> SoundChanged, VibrationChanged, MotionChanged;
        /// <summary>Ayarlar → "Bildirimler" anahtarı (2026-11-13).</summary>
        public event Action<bool> NotifyChanged;
        public Func<bool> NotifyEnabled;

        private Canvas _canvas;
        private RectTransform _safe;
        private RectTransform _page;
        private Image _bg;
        private AspectRatioFitter _bgFit;
        private RectTransform _hero, _play, _halo, _mapContent;
        private float _clock;
        private readonly List<(RectTransform t, Image img, Vector2 home, float speed, float phase)> _ambient = new List<(RectTransform, Image, Vector2, float, float)>();

        public bool IsOpen => gameObject.activeSelf;
        /// <summary>Haritada gösterilen bölge (0 tabanlı, 100'lük).</summary>
        public int CurrentSheet { get; private set; }
        public ThemeDefinition Theme { get; private set; }

        private static readonly Color Gold = new Color32(0xFF, 0xC8, 0x3A, 0xFF);

        public static MenuView Create(Camera cam)
        {
            var go = new GameObject("Menu", typeof(RectTransform));
            go.layer = 5;
            var m = go.AddComponent<MenuView>();
            m._canvas = go.AddComponent<Canvas>();
            m._canvas.renderMode = RenderMode.ScreenSpaceCamera;
            m._canvas.worldCamera = cam;
            m._canvas.planeDistance = 4f;
            m._canvas.sortingOrder = 700;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(HudView.RefWidth, HudView.RefHeight);
            scaler.matchWidthOrHeight = KatKat.UI.UiAspect.Match; // iPad: yüksekliğe eşleme (UiAspect)
            go.AddComponent<GraphicRaycaster>();
            var under = UiFactory.Image("Ground", go.transform, null, UiArt.Ivory);
            under.raycastTarget = true;
            UiFactory.Stretch(under.rectTransform);
            m._bg = UiFactory.Image("Background", go.transform, null, Color.white);
            UiFactory.Place(m._bg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(HudView.RefWidth, HudView.RefHeight));
            m._bgFit = m._bg.gameObject.AddComponent<AspectRatioFitter>();
            m._bgFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            m._bgFade = UiFactory.Image("BackgroundFade", go.transform, null, Color.white);
            UiFactory.Place(m._bgFade.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(HudView.RefWidth, HudView.RefHeight));
            m._bgFade.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            m._bgFade.raycastTarget = false;
            m._bgFade.enabled = false;
            m._safe = UiFactory.Stretch(UiFactory.Rect("SafeArea", go.transform));
            m._safe.gameObject.AddComponent<SafeAreaFitter>();
            return m;
        }

        public Canvas Canvas => _canvas;

        public void ApplySafeArea() => _safe.GetComponent<SafeAreaFitter>().Apply(true);

        public void Hide() => gameObject.SetActive(false);

        private RectTransform NewPage(Sprite background)
        {
            gameObject.SetActive(true);
            if (_page != null) UiFactory.Kill(_page.gameObject);
            _hero = _play = _halo = _mapContent = null;
            _heroShadow = _pinHolder = null;
            _ret = _retMascot = _retArrow = null;
            _map = null;
            _dailyModal = null;
            _dailyPrevTab = -1; // yeni sayfa kendi alt menüsünü kurar
            _tabsHolder = null;
            _zpeModal = null;
            _heroBase = HeroY;
            _startCard = null;
            if (_bgFade != null) _bgFade.enabled = false;
            _bgFadeT = 1f;
            _ambient.Clear();
            _clouds.Clear();
            _bg.sprite = background;
            _bg.enabled = background != null;
            if (background != null) _bgFit.aspectRatio = background.rect.width / background.rect.height;
            _page = UiFactory.Stretch(UiFactory.Rect("Page", _safe));
            return _page;
        }

        // ---- Ortak parçalar ---------------------------------------------------------------------------

        /// <summary>Sol üst ekran etiketi (Loc.T("ui.home") / Loc.T("btn.levels")).</summary>
        private static void ScreenTag(RectTransform page, string text)
        {
            var pill = UiFactory.Image("ScreenTag", page, UiFactory.Art("ui/hud-title-pill", "hud_title") ?? UiArt.Box, Color.white, true);
            UiFactory.Place(pill.rectTransform, new Vector2(0f, 1f), new Vector2(150f, -54f), new Vector2(250f, 72f));
            UiFactory.FitSliced(pill, 72f);
            var t = UiFactory.Text("Text", pill.transform, text, 30, UiArt.Cream);
            UiFactory.Stretch(t.rectTransform, 10, 10, 0, 4);
        }

        /// <summary>Bölgenin küçük resmi: ana sayfa arka planından ortalanmış kırpım (maskeli).</summary>
        private static void Thumb(RectTransform parent, ThemeDefinition theme, Vector2 pos, Vector2 size)
        {
            var frame = UiFactory.Image("Thumb", parent, UiArt.Box, new Color(1f, 1f, 1f, 0.95f), true);
            UiFactory.Place(frame.rectTransform, new Vector2(0f, 0.5f), pos, size + new Vector2(8f, 8f));
            frame.pixelsPerUnitMultiplier = UiArt.Box.border.x / 16f;
            var mask = UiFactory.Rect("Mask", frame.transform);
            UiFactory.Stretch(mask, 4, 4, 4, 4);
            mask.gameObject.AddComponent<RectMask2D>();
            var sp = theme.HomeSprite;
            if (sp == null) return;
            var img = UiFactory.Image("Image", mask, sp, Color.white);
            // Ana sayfa resminin orta-alt bölümü (heykel/iskele) kartta okunur.
            float w = size.x * 1.25f;
            UiFactory.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -size.y * 0.15f), new Vector2(w, w * sp.rect.height / sp.rect.width));
        }

        /// <summary>Bölge numarası etiketi ("1 — SAHİL").</summary>
        private static void ChapterChip(RectTransform parent, int region, ThemeDefinition theme, Vector2 pos, TextAnchor anchor)
        {
            var chip = UiFactory.Image("Chapter", parent, UiArt.Box, new Color(1f, 0.97f, 0.9f, 1f), true);
            chip.pixelsPerUnitMultiplier = UiArt.Box.border.x / 16f;
            UiFactory.Place(chip.rectTransform, new Vector2(0f, 0.5f), pos, new Vector2(190f, 40f));
            var o = chip.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(UiArt.Gold.r, UiArt.Gold.g, UiArt.Gold.b, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
            var t = UiFactory.Text("Text", chip.transform, $"{region + 1} — {theme.Name.ToUpperInvariant().Replace("I", "İ")}", 24, UiArt.HudTealDark);
            UiFactory.Stretch(t.rectTransform);
        }

        // ---- v6 ana ekran üst kartı ve alt menü (kullanıcı referansı, 2026-11-05) ------------------------------
        // Ölçüler referansın 455 px genişliğinden 1080'e (×2,37) çevrildi. Üst kart: krem hap (oyun içi üst barla aynı
        // zemin), solda bölge simgesi (kartın üstünden taşar), ortada "BÖLGE n" çipi + bölge adı + aralık, sağda ayraç +
        // yıldız toplamı. Alt menü: krem hap; Harita (etkinse altında nokta) | büyük turkuaz Oyna (▶ + bölüm numarası) | Ayarlar.

        private const float HeaderTop = 24f, HeaderH = 190f, HeaderW = 1000f;
        // v10 Mavi Liman: alt kabuk güvenli alanın alt kenarından ShellBarH yükseklikte (MenuView.Shell.cs).
        private const float BarBottom = 0f, BarH = ShellBarH;
        private static readonly Color PillCream = new Color32(0xFF, 0xF4, 0xE0, 0xFF);
        private static readonly Color DividerColor = new Color(0.64f, 0.37f, 0.19f, 0.28f);
        private static readonly Color DeepTeal = new Color32(0x15, 0x5E, 0x5A, 0xFF);
        private static readonly Color RangeBrown = new Color32(0x86, 0x6A, 0x55, 0xFF);

        private int _playIndex;
        private Action _onPlay;

        private static Image Divider(Transform parent, Vector2 pos, float height)
        {
            var d = UiFactory.Image("Divider", parent, UiArt.Box, DividerColor);
            UiFactory.Place(d.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(3f, height));
            return d;
        }




        public const int TabDaily = 3, TabCollection = 4;


        private static Sprite _calendarIcon;

        /// <summary>Günlük simgesi (yer tutucu): koddan çizilmiş takvim yaprağı; kırmızı üst şerit, iki halka, nokta ızgara.</summary>
        private static Sprite CalendarIcon()
        {
            if (_calendarIcon) return _calendarIcon;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "calendar_icon", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[n * n];
            Color paper = new Color32(0xFF, 0xF8, 0xEC, 0xFF), red = new Color32(0xE8, 0x5A, 0x48, 0xFF), edge = new Color32(0x3C, 0x8E, 0x86, 0xFF);
            Color dot = new Color32(0x1E, 0x9C, 0x92, 0xFF), hot = new Color32(0xF2, 0x9A, 0x3A, 0xFF), ring = new Color32(0x5A, 0x4A, 0x40, 0xFF);
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = (i + 0.5f) / n, y = (j + 0.5f) / n;
                var c = new Color(0f, 0f, 0f, 0f);
                // Yuvarlak köşeli yaprak: 0.10–0.90 × 0.08–0.86.
                float dx = Mathf.Max(0f, Mathf.Max(0.22f - x, x - 0.78f)), dy = Mathf.Max(0f, Mathf.Max(0.20f - y, y - 0.74f));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < 0.12f)
                {
                    bool border = d > 0.095f;
                    c = border ? edge : (y > 0.62f ? red : paper);
                    if (!border && y <= 0.62f)
                    {
                        // 3 × 3 gün noktası; ortadaki "bugün" turuncu.
                        for (int a = 0; a < 3; a++)
                        for (int b = 0; b < 3; b++)
                        {
                            float cx = 0.30f + a * 0.20f, cy = 0.48f - b * 0.15f;
                            if ((x - cx) * (x - cx) + (y - cy) * (y - cy) < 0.0028f) c = a == 1 && b == 1 ? hot : dot;
                        }
                    }
                }
                // Asma halkaları.
                foreach (float rx in new[] { 0.34f, 0.66f })
                    if (Mathf.Abs(x - rx) < 0.035f && y > 0.70f && y < 0.94f) c = ring;
                px[j * n + i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return _calendarIcon = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }


        private static Sprite _mapIcon;

        /// <summary>Harita simgesi: PremiumV6/ui/map varsa o; yoksa koddan katlanmış harita + kırmızı iğne.</summary>
        private static Sprite MapIcon()
        {
            if (_mapIcon) return _mapIcon;
            var v6 = SpecialArt.V6("ui/map");
            if (v6 != null) return _mapIcon = v6;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "map_icon", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[n * n];
            Color paper = new Color32(0xF6, 0xE7, 0xC6, 0xFF), green = new Color32(0x7C, 0xC2, 0x6A, 0xFF), blue = new Color32(0x6C, 0xC4, 0xE0, 0xFF);
            Color edge = new Color32(0x3C, 0x8E, 0x86, 0xFF), red = new Color32(0xE8, 0x4E, 0x40, 0xFF), redDark = new Color32(0xB0, 0x30, 0x28, 0xFF);
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = (i + 0.5f) / n, y = (j + 0.5f) / n;
                var c = new Color(0f, 0f, 0f, 0f);
                // Üç panelli katlanmış harita (paneller hafif zikzak).
                int panel = Mathf.Clamp((int)((x - 0.08f) / 0.28f), 0, 2);
                float lx = (x - 0.08f) - panel * 0.28f;
                float skew = (panel % 2 == 0 ? 1f : -1f) * 0.05f;
                float top = 0.80f + skew * (lx / 0.28f - 0.5f) * 2f, bottom = 0.14f - skew * (lx / 0.28f - 0.5f) * 2f;
                if (x > 0.08f && x < 0.92f && y > bottom && y < top)
                {
                    bool border = x < 0.11f || x > 0.89f || y < bottom + 0.03f || y > top - 0.03f || Mathf.Abs(lx) < 0.012f;
                    var fill = panel == 1 ? Color.Lerp(paper, blue, 0.55f) : Color.Lerp(paper, green, 0.35f);
                    if (panel == 0 && (x - 0.22f) * (x - 0.22f) + (y - 0.35f) * (y - 0.35f) < 0.006f) fill = green;
                    fill *= panel == 1 ? 0.92f : 1f;
                    fill.a = 1f;
                    c = border ? edge : fill;
                }
                // İğne (damla): tepe daire + alt sivri uç, beyaz göbek.
                float dx = x - 0.56f, dy = y - 0.62f;
                float circle = Mathf.Sqrt(dx * dx + dy * dy) - 0.17f;
                float tipY = y - 0.30f;
                bool inTip = tipY > 0f && dy < 0f && Mathf.Abs(dx) < (y - 0.30f) * 0.62f;
                if (circle < 0f || inTip)
                {
                    c = Color.Lerp(red, redDark, Mathf.Clamp01(0.5f - dy * 2f + dx * 1.2f));
                    if (Mathf.Sqrt(dx * dx + dy * dy) < 0.065f) c = Color.white;
                    if ((x - 0.50f) * (x - 0.50f) + (y - 0.70f) * (y - 0.70f) < 0.0012f) c = Color.Lerp(c, Color.white, 0.6f);
                    c.a = 1f;
                }
                px[j * n + i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            _mapIcon = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            _mapIcon.hideFlags = HideFlags.DontSave;
            return _mapIcon;
        }

        /// <summary>Altın ışık çizgileri: bir odağın iki yanında üçer kısa ışın (logo, OYNA, mevcut düğüm).</summary>
        private static void Rays(RectTransform parent, Vector2 center, float halfWidth, float len)
        {
            for (int side = -1; side <= 1; side += 2)
            for (int k = -1; k <= 1; k++)
            {
                var ray = UiFactory.Image("Ray", parent, UiArt.Box, Gold, true);
                ray.pixelsPerUnitMultiplier = UiArt.Box.border.x / 7f;
                var rt = ray.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                float ang = k * 28f;
                var dir = Quaternion.Euler(0f, 0f, ang) * new Vector3(side, 0f, 0f);
                rt.anchoredPosition = center + new Vector2(side * halfWidth, 0f) + (Vector2)(dir * len * 0.6f);
                rt.sizeDelta = new Vector2(len, 15f);
                rt.localRotation = Quaternion.Euler(0f, 0f, side > 0 ? ang : -ang);
                var sh = ray.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0.7f, 0.42f, 0.05f, 0.5f);
                sh.effectDistance = new Vector2(0f, -3f);
            }
        }

        // ---- Ana sayfa ---------------------------------------------------------------------------

        public void ShowHome(int continueIndex, int completedCount, int total, Action onContinue, Action onLevels, Action onSettings)
        {
            Theme = ThemeDefinition.ForLevel(continueIndex);
            int region = ThemeDefinition.RegionOf(continueIndex);
            _playIndex = continueIndex;
            _onPlay = onContinue;
            var page = NewPage(Theme.HomeSprite);
            AmbientFx(page);

            ScreenTag(page, Loc.T("ui.home"));
            var gear = UiFactory.CircleButton("Settings", page, false, UiIcon.SoundOn, 104f, onSettings, "icons/settings");
            UiFactory.Place((RectTransform)gear.transform, new Vector2(1f, 1f), new Vector2(-80f, -54f), new Vector2(104f, 104f));

            // Logo + iki yanında küçük katman yığınları ve ışık çizgileri.
            var logoArea = UiFactory.Rect("LogoArea", page);
            UiFactory.Place(logoArea, new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(1000f, 240f));
            // Kullanıcı 2026-11-09: oyunun adı Stone Burst (docs/gorseller/arayuz/logo-yeni.png, iki satırlı logo).
            var logoSprite = PremiumArt.V4("ui/logo-stoneburst");
            if (logoSprite != null)
            {
                var logo = UiFactory.Image("Logo", logoArea, logoSprite, Color.white);
                logo.preserveAspect = true;
                UiFactory.Place(logo.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 200f));
            }
            else
            {
                var title = UiFactory.Title("Logo", logoArea, "Stone Burst", 110, UiArt.Cream, UiArt.HudTealDark);
                UiFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 210f));
            }
            var stackSprite = PremiumArt.V4("icons/levels");
            for (int side = -1; side <= 1; side += 2)
            {
                if (stackSprite == null) break;
                var st = UiFactory.Image("LogoStack", logoArea, stackSprite, Color.white);
                st.preserveAspect = true;
                UiFactory.Place(st.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(side * 435f, -6f), new Vector2(120f, 120f));
                st.rectTransform.localRotation = Quaternion.Euler(0f, 0f, side * -8f);
                if (side > 0) st.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
            Rays(logoArea, new Vector2(0f, 6f), 450f, 46f);

            // Katman heykeli: bölgeye göre taş kaide üstünde, dünyanın içinde ve ölçülü.
            var plinthTop = Theme.Id == "desert" ? new Color32(0xE2, 0xB0, 0x78, 0xFF) : Theme.Id == "garden" ? new Color32(0xE9, 0xDF, 0xC9, 0xFF) : new Color32(0xEC, 0xDF, 0xCB, 0xFF);
            var plinthSide = Theme.Id == "desert" ? new Color32(0xB8, 0x83, 0x52, 0xFF) : new Color32(0xC4, 0xB4, 0x9A, 0xFF);
            var plinth = UiFactory.Rect("Plinth", page);
            UiFactory.Place(plinth, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(380f, 150f));
            var side1 = UiFactory.Image("Side", plinth, UiArt.Box, plinthSide, true);
            UiFactory.Place(side1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(380f, 90f));
            side1.pixelsPerUnitMultiplier = UiArt.Box.border.x / 45f;
            var top = UiFactory.Image("Top", plinth, UiArt.Circle, plinthTop);
            UiFactory.Place(top.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(380f, 110f));
            var shade = top.gameObject.AddComponent<Shadow>();
            shade.effectColor = new Color(0.3f, 0.2f, 0.1f, 0.35f);
            shade.effectDistance = new Vector2(0f, -6f);
            var heroSprite = PremiumArt.V4("ui/hero-layer-stack");
            if (heroSprite != null)
            {
                var hero = UiFactory.Image("Hero", page, heroSprite, Color.white);
                hero.preserveAspect = true;
                UiFactory.Place(hero.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, HeroY), new Vector2(330f, 330f));
                _hero = hero.rectTransform;
            }

            // OYNA: ▶ + gerçek bölüm numarası, iki yanında ışık çizgileri.
            var playArea = UiFactory.Rect("PlayArea", page);
            UiFactory.Place(playArea, new Vector2(0.5f, 0f), new Vector2(0f, 490f), new Vector2(1000f, 200f));
            Rays(playArea, Vector2.zero, 345f, 40f);
            var play = UiFactory.PillButton("Play", playArea, Loc.T("ui.playbig"), true, new Vector2(640f, 196f), onContinue);
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 196f));
            var label = (RectTransform)play.transform.Find("Label");
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 0.5f);
            label.sizeDelta = new Vector2(330f, 104f);
            label.anchoredPosition = new Vector2(-34f, 26f);
            label.GetComponent<Text>().fontSize = 82;
            var tri = UiFactory.Image("PlayIcon", play.transform, UiArt.Icon(UiIcon.Play), UiArt.Cream);
            UiFactory.Place(tri.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(170f, 26f), new Vector2(78f, 78f));
            var lv = UiFactory.Text("Level", play.transform, Loc.F("ui.level", continueIndex + 1), 36, UiArt.Cream);
            UiFactory.Place(lv.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(400f, 44f));
            var lvShadow = lv.gameObject.AddComponent<Shadow>();
            lvShadow.effectColor = new Color(UiArt.HudTealDark.r, UiArt.HudTealDark.g, UiArt.HudTealDark.b, 0.7f);
            _play = (RectTransform)play.transform;

            // Bölge kartı: küçük resim + bölge numarası + "Mavi Liman · 1–100" + ok; dokununca harita.
            int first = region * ThemeDefinition.RegionSize + 1, last = Mathf.Min(total, (region + 1) * ThemeDefinition.RegionSize);
            var cardSprite = UiFactory.Art("ui/next-preview-panel", "btn_cream");
            var card = UiFactory.SpriteButton("RegionCard", page, cardSprite ?? UiArt.Box, new Vector2(700f, 150f), onLevels);
            UiFactory.Place((RectTransform)card.transform, new Vector2(0.5f, 0f), new Vector2(0f, 340f), new Vector2(700f, 150f));
            var cr = (RectTransform)card.transform;
            Thumb(cr, Theme, new Vector2(98f, 0f), new Vector2(130f, 100f));
            ChapterChip(cr, region, Theme, new Vector2(282f, 26f), TextAnchor.MiddleLeft);
            var rn = UiFactory.Text("Region", cr, $"{ThemeDefinition.RegionTitle(region)} · {first}–{last}", 38, UiArt.Brown, TextAnchor.MiddleLeft);
            UiFactory.Place(rn.rectTransform, new Vector2(0f, 0.5f), new Vector2(395f, -24f), new Vector2(420f, 50f));
            FitText(rn, 24);
            var chev = UiFactory.Image("Chevron", cr, PremiumArt.V4("icons/next") ?? UiArt.Icon(UiIcon.Play), UiArt.HudTeal);
            chev.preserveAspect = true;
            UiFactory.Place(chev.rectTransform, new Vector2(1f, 0.5f), new Vector2(-62f, 0f), new Vector2(40f, 56f));
            // İlerleme: okunaklı krem hap üstünde koyu yazı (önceden açık zemin üstünde beyaz, ince yazıydı).
            var donePill = UiFactory.Image("DonePill", page, UiArt.Box, new Color(1f, 0.97f, 0.9f, 0.92f), true);
            UiFactory.Place(donePill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(440f, 46f));
            donePill.pixelsPerUnitMultiplier = UiArt.Box.border.x / 23f;
            var done = UiFactory.Text("Done", donePill.transform, $"{completedCount}/{total} bölüm tamamlandı", 26, UiArt.Brown);
            UiFactory.Stretch(done.rectTransform, 16, 16, 0, 2);
            FitText(done, 18);

            Tabs(page, 0, () => { }, onLevels, onSettings);
            Tick(0f);
        }

        private const float HeroY = 70f;

        /// <summary>Ortam parçacıkları (v3 §3–5): az sayıda, düşük opaklık, yavaş; etkileşim alanlarının arkasında.</summary>
        private void AmbientFx(RectTransform page)
        {
            var fx = PremiumArt.V4(Theme.AmbientFx);
            if (fx == null) return;
            var rnd = new System.Random(3);
            for (int i = 0; i < 6; i++)
            {
                var img = UiFactory.Image("Ambient", page, fx, new Color(1f, 1f, 1f, 0f));
                img.preserveAspect = true;
                var home = new Vector2(-430f + (float)rnd.NextDouble() * 860f, -700f + (float)rnd.NextDouble() * 1100f);
                float size = 40f + (float)rnd.NextDouble() * 34f;
                UiFactory.Place(img.rectTransform, new Vector2(0.5f, 0.5f), home, new Vector2(size, size));
                _ambient.Add((img.rectTransform, img, home, 18f + (float)rnd.NextDouble() * 16f, (float)rnd.NextDouble() * 10f));
            }
        }

        // ---- Bölüm haritası: bütün bölümler tek, kesintisiz yol ---------------------------------------
        //
        // Yol 1. bölümden son bölüme kadar kesintisiz yukarı akar; her bölgenin başında yolun üstünde bölge tabelası
        // ("2 — BAHÇE · Yeşil Vadi · 101–200"), en üstte Loc.T("ui.soon"). Düğümler ve yol parçaları
        // yalnızca görünen aralık için kurulur (1000 bölümde de birkaç düzine nesne). Arka plan ve başlık kartı
        // ekranın ortasındaki bölgeyi izler; ‹ › bir önceki/sonraki bölgenin başına kaydırır.

        private const float NodeStep = 230f, PathAmp = 230f, SignGap = 300f, StartY = 170f, ChunkLevels = 25;
        private const float Margin = 700f;

        private sealed class MapState
        {
            public int Total, Current, Regions, HeaderRegion = -1, StarSum;
            public int HighestUnlocked;
            public Func<int, bool> IsCompleted;
            public ScrollRect Scroll;
            public RectTransform Content, Viewport, Head, Card, HeadBack, CardBack, LayerShadow, LayerEdge, LayerFill, LayerDash, LayerNodes, LayerPin, LayerFog, LayerPuffs;
            public float CloudEdge, CloudFrom;
            public readonly Dictionary<int, List<CloudPuff>> Clouds = new Dictionary<int, List<CloudPuff>>();
            public Button Prev, Next;
            public readonly Dictionary<int, GameObject> Nodes = new Dictionary<int, GameObject>();
            public readonly Dictionary<int, GameObject[]> Chunks = new Dictionary<int, GameObject[]>();
            public float ViewH, Height;
            public float? ScrollTarget;
        }

        private MapState _map;
        private RectTransform _layerBg;
        private readonly Dictionary<int, GameObject> _bgTiles = new Dictionary<int, GameObject>();
        private Image _bgFade;
        private float _bgFadeT = 1f;

        /// <summary>i. bölümün (0 tabanlı, bütün katalog) yoldaki yeri: alttan yukarı S kıvrımlı; her bölge başında tabela boşluğu.</summary>
        private static Vector2 NodePos(int i)
        {
            float y = NodeY(i);
            return new Vector2(RoadX(y), y);
        }

        // ---- v6 harita şeridi: kullanıcının üç parçası (docs/v6/map a,b,c → tools/ArtV6/prepare.ps1) ------------
        // Parçalar Resources/PremiumV6/map/coast-0..2 (1080 × StripPart), ek yerleri nehrin ortasında kaynaşmış;
        // şerit yukarıdan aşağı a→b→c→a… döner. Haritada içerik alttan yukarı aktığı için k. karo (alttan) parça
        // (3 − k mod 3) mod 3'tür. coast-path: şeritte yukarıdan her 4 satırda yolun orta çizgisi (0..1).
        // Düğümler ve bölüm çizgisi bu orta çizgiden geçer: resimdeki yolla birebir.

        // v6: her tema kendi şeridine sahip olabilir (coast/garden/desert-0..2 + -path.txt). Karo yüksekliği
        // bütün temalarda ortaktır (_stripPart, ilk bulunan şerit); farklı temaların parçaları 1–2 px farkla bu
        // yüksekliğe gerilir. Karo k'nın teması karonun ortasının bölgesidir; tema değişen eklem köprüdür (yol orada
        // her şeritte ortada), böylece bölüm çizgisi bölgeler arasında da kesintisiz.

        private const float StripW = 1080f;
        private sealed class RoadStrip
        {
            public float[] X;
            public int Period, Step, Part;
            public Sprite[] Sprites;
        }

        private static readonly Dictionary<string, RoadStrip> _roads = new Dictionary<string, RoadStrip>();
        private static int _stripPart;
        private static bool _roadTried;

        private static RoadStrip Road(ThemeDefinition theme)
        {
            LoadRoad();
            return _roads.TryGetValue(theme.Id, out var r) ? r : null;
        }

        private static bool LoadRoad()
        {
            if (_roadTried) return _stripPart > 0;
            _roadTried = true;
            foreach (var theme in ThemeDefinition.All)
            {
                var ta = Resources.Load<TextAsset>("PremiumV6/map/" + theme.Id + "-path");
                ResourceUsage.Note("PremiumV6/map/" + theme.Id + "-path");
                var parts = new Sprite[3];
                for (int i = 0; i < 3; i++) parts[i] = SpecialArt.V6("map/" + theme.Id + "-" + i);
                if (ta == null || parts[0] == null || parts[1] == null || parts[2] == null) continue;
                var lines = ta.text.Split('\n');
                var head = lines[0].Trim().Split(' ');
                var xs = new List<float>();
                for (int i = 1; i < lines.Length; i++)
                {
                    var s = lines[i].Trim();
                    if (s.Length > 0) xs.Add(float.Parse(s, System.Globalization.CultureInfo.InvariantCulture));
                }
                var road = new RoadStrip { X = xs.ToArray(), Period = int.Parse(head[0]), Step = int.Parse(head[1]), Sprites = parts };
                road.Part = road.Period / 3;
                if (_stripPart == 0) _stripPart = road.Part;
                _roads[theme.Id] = road;
            }
            return _stripPart > 0;
        }

        /// <summary>y yüksekliğinin bölgesi (0 tabanlı); harita durumundan bağımsız.</summary>
        private static int RegionAtY(float y)
        {
            // E paketi: SignY bölgeyle doğrusal; tahmin + küçük düzeltme (önceden 0'dan döngü, her düğüm çağrısında).
            float period = SignY(1) - SignY(0);
            int r = Mathf.Clamp(Mathf.FloorToInt((y - SignY(0)) / period), 0, 999);
            while (r > 0 && SignY(r) > y) r--;
            while (r < 999 && SignY(r + 1) <= y) r++;
            return r;
        }

        private static ThemeDefinition ThemeAtY(float y) => ThemeDefinition.ForLevel(RegionAtY(y) * ThemeDefinition.RegionSize);

        /// <summary>Şerit karosunun (alttan k.) parça indeksi.</summary>
        private static int StripPartOf(int k) => (3 - ((k % 3) + 3) % 3) % 3;

        // ---- Açılmamış bölümlerin bulut örtüsü (kullanıcı: "ilerledikçe bulutlar kalksın, doğal olsun") -----------
        // Örtü oyuncunun ulaştığı bölümün hemen üstünden başlar: altta düzensiz kabarık bulut sırası, üstünde yumuşak
        // sis dolgusu ve farklı boy/tonda bulut kümeleri. Hepsi yavaşça yana süzülür. Örtü 420 birimlik parçalar
        // hâlinde yalnızca görünen aralıkta kurulur (rastgelelik parça numarasından: her açılışta aynı görünüm).
        // Oturumda ilerleme olduysa eski sınırla yeni sınır arasındaki bulutlar iki yana açılarak kalkar.

        private sealed class CloudPuff
        {
            public RectTransform T;
            public Image Img;
            public Vector2 Home;
            public float BaseA, Amp, Speed, Phase, Dir;
            public bool Dissolve;
        }

        private const float CloudChunk = 420f;
        private static int _seenFrontier = -1;
        private float _cloudStart;

        /// <summary>Bulut örtüsünün alt sınırı: mevcut düğümün üst kenarının biraz üstü.</summary>
        private static float CloudEdgeY(int current) => NodeY(current) + 95f;

        private List<CloudPuff> BuildClouds(int c)
        {
            var m = _map;
            var list = new List<CloudPuff>();
            var rnd = new System.Random(c * 7919 + 17);
            float r() => (float)rnd.NextDouble();
            float y0 = c * CloudChunk, y1 = y0 + CloudChunk;
            float from = m.CloudFrom;
            bool opening = m.CloudFrom < m.CloudEdge;

            // Sis dolgusu (büyük yumuşak lekeler): sınırın biraz üstünden başlar.
            for (int i = 0; i < 7; i++)
            {
                float y = y0 + r() * CloudChunk, x = -520f + r() * 1040f, s = 700f + r() * 300f;
                if (y < from + 130f) continue;
                list.Add(MakePuff(m.LayerFog, View.SpecialArt.Glow, new Vector2(x, y), new Vector2(s, s * 0.8f),
                    Color.Lerp(Color.white, new Color(0.9f, 0.94f, 1f), r() * 0.6f), 0.97f, r, opening && y < m.CloudEdge + 150f));
            }
            // Bulut kümeleri: farklı boy, arkadakiler hafif mavi-gri.
            for (int i = 0; i < 12; i++)
            {
                float w = 260f + r() * 300f, x = -620f + r() * 1240f, y = y0 + r() * CloudChunk;
                if (y < from + w * 0.18f + 60f) continue;
                list.Add(MakePuff(m.LayerPuffs, CloudSprite, new Vector2(x, y), new Vector2(w, w * CloudAspect),
                    Color.Lerp(Color.white, new Color(0.84f, 0.9f, 0.98f), r() * 0.7f), 0.96f, r, opening && y < m.CloudEdge + w * 0.18f + 60f, r() < 0.5f));
            }
            // Alt kenar sırası: eski ve yeni sınırda (kenar düz çizgi olmasın diye yükseklik ve boy düzensiz).
            foreach (var edge in opening ? new[] { from, m.CloudEdge } : new[] { m.CloudEdge })
            {
                if (edge < y0 || edge >= y1) continue;
                bool old = opening && edge < m.CloudEdge;
                for (float x = -640f; x <= 640f; x += 120f + r() * 70f)
                {
                    float w = 280f + r() * 190f;
                    var home = new Vector2(x + (r() - 0.5f) * 50f, edge + w * 0.18f + r() * 45f);
                    list.Add(MakePuff(m.LayerPuffs, CloudSprite, home, new Vector2(w, w * CloudAspect),
                        Color.Lerp(Color.white, new Color(0.88f, 0.92f, 0.99f), r() * 0.4f), 0.98f, r, old, r() < 0.5f));
                }
            }
            return list;
        }

        private CloudPuff MakePuff(RectTransform layer, Sprite sprite, Vector2 home, Vector2 size, Color tint, float alpha, Func<float> r, bool dissolve, bool flip = false)
        {
            var img = UiFactory.Image("Cloud", layer, sprite, new Color(tint.r, tint.g, tint.b, alpha));
            img.raycastTarget = false;
            Pos(img.rectTransform, home, size);
            if (flip) img.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            return new CloudPuff
            {
                T = img.rectTransform, Img = img, Home = home, BaseA = alpha, Amp = 8f + r() * 16f, Speed = 0.12f + r() * 0.18f,
                Phase = r() * 6.28f, Dir = home.x >= 0f ? 1f : -1f, Dissolve = dissolve,
            };
        }

        private void UpdateClouds(float y0, float y1)
        {
            var m = _map;
            if (m.LayerPuffs == null) return;
            int c0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Max(y0, m.CloudFrom - CloudChunk)) / CloudChunk));
            int c1 = Mathf.FloorToInt(Mathf.Min(y1, m.Height + 300f) / CloudChunk);
            var keep = new HashSet<int>();
            for (int c = c0; c <= c1; c++)
            {
                keep.Add(c);
                if (!m.Clouds.ContainsKey(c)) m.Clouds[c] = BuildClouds(c);
            }
            var drop = new List<int>();
            foreach (var kv in m.Clouds) if (!keep.Contains(kv.Key)) drop.Add(kv.Key);
            foreach (var c in drop) { foreach (var p in m.Clouds[c]) if (p.T != null) UiFactory.Kill(p.T.gameObject); m.Clouds.Remove(c); }
        }

        private void TickClouds()
        {
            if (_map == null) return;
            float open = Mathf.Clamp01((_clock - _cloudStart - 0.5f) / 1.6f);
            float e = open * open * (3f - 2f * open);
            foreach (var kv in _map.Clouds)
            foreach (var p in kv.Value)
            {
                if (p.T == null) continue;
                float x = p.Home.x + p.Amp * Mathf.Sin(_clock * p.Speed + p.Phase);
                float y = p.Home.y + 3f * Mathf.Sin(_clock * p.Speed * 1.7f + p.Phase);
                if (p.Dissolve)
                {
                    if (e >= 1f) { if (p.T.gameObject.activeSelf) p.T.gameObject.SetActive(false); continue; }
                    x += p.Dir * 320f * e;
                    y += 40f * e;
                    var col = p.Img.color;
                    col.a = p.BaseA * (1f - e);
                    p.Img.color = col;
                    float s = 1f + 0.25f * e;
                    p.T.localScale = new Vector3(Mathf.Sign(p.T.localScale.x) * s, s, 1f);
                }
                p.T.anchoredPosition = new Vector2(x, y);
            }
        }

        // ---- "Bölümüme dön" düğmesi (kullanıcı: kaydırınca kaldığımız bölüme hızlı dönüş; maskot + aşağı ok) ------
        // Mevcut düğüm görünüm alanının dışındaysa alt menünün üstünde belirir: zıplayan maskot, altında ok (düğüm
        // aşağıdaysa aşağı, yukarıdaysa yukarı bakar). Dokununca mevcut bölüm ekranın ortasına kayar.

        private RectTransform _ret, _retMascot, _retArrow;
        private CanvasGroup _retGroup;
        private float _retShow;

        private void BuildReturnButton(RectTransform page)
        {
            var btn = UiFactory.SpriteButton("ReturnToLevel", page, null, new Vector2(150f, 190f), () =>
            {
                if (_map == null) return;
                _map.Scroll.StopMovement();
                _map.ScrollTarget = NodePos(_map.Current).y - _map.ViewH * 0.5f;
            });
            btn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            _ret = (RectTransform)btn.transform;
            UiFactory.Place(_ret, new Vector2(0.5f, 0f), new Vector2(0f, BarBottom + BarH + 112f), new Vector2(150f, 190f));
            _retGroup = btn.gameObject.AddComponent<CanvasGroup>();
            var glow = UiFactory.Image("Glow", _ret, SpecialArt.Glow, new Color(1f, 0.96f, 0.8f, 0.85f));
            glow.raycastTarget = false;
            UiFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(200f, 200f));
            // Dönüş düğmesinde de karşıya bakan, hareketli maskot (kap ayak tabanında).
            var mascotHop = UiFactory.Rect("Mascot", _ret);
            UiFactory.Place(mascotHop, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), Vector2.zero);
            const float rw = 100f;
            MascotView.Create(mascotHop, new Vector2(0f, MascotView.FeetBelowAnchor(rw)), mascotHop, rw);
            foreach (var gr in mascotHop.GetComponentsInChildren<Graphic>()) gr.raycastTarget = false;
            _retMascot = mascotHop;
            // Ok: turkuaz daire içinde beyaz üçgen.
            var disc = UiFactory.Image("ArrowDisc", _ret, UiArt.Circle, new Color32(0x1E, 0x9C, 0x92, 0xFF));
            disc.raycastTarget = false;
            UiFactory.Place(disc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(64f, 64f));
            var dOl = disc.gameObject.AddComponent<Outline>();
            dOl.effectColor = Color.white;
            dOl.effectDistance = new Vector2(3f, -3f);
            var arrow = UiFactory.Image("Arrow", disc.transform, UiArt.Icon(UiIcon.Play), Color.white);
            arrow.raycastTarget = false;
            arrow.preserveAspect = true;
            UiFactory.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            _retArrow = disc.rectTransform;
            _retShow = 0f;
            _retGroup.alpha = 0f;
            _ret.gameObject.SetActive(false);
        }

        private void TickReturn(float dt)
        {
            if (_ret == null || _map == null) return;
            float nodeY = NodePos(_map.Current).y, top = ScrollY, bottom = ScrollY + _map.ViewH;
            // Görünür sayılan bant: kartın ve alt menünün arkasında kalan pay hariç.
            bool below = nodeY < top + 230f, above = nodeY > bottom - 330f;
            bool want = (below || above) && !_map.ScrollTarget.HasValue;
            _retShow = Mathf.MoveTowards(_retShow, want ? 1f : 0f, dt / 0.2f);
            _ret.gameObject.SetActive(_retShow > 0.001f);
            if (_retShow <= 0.001f) return;
            _retGroup.alpha = _retShow;
            _ret.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Ease.OutCubic(_retShow));
            // Ok yönü: düğüm aşağıdaysa aşağı (Play simgesi sağa bakar → −90°), yukarıdaysa yukarı.
            _retArrow.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, below ? -90f : 90f);
            float ph = Mathf.Repeat(_clock / 0.9f, 1f);
            float h = Mathf.Sin(ph * Mathf.PI) * 14f;
            float land = ph < 0.12f ? 1f - ph / 0.12f : 0f;
            _retMascot.anchoredPosition = new Vector2(0f, -14f + h);
            _retMascot.localScale = new Vector3(1f + 0.1f * land, 1f - 0.1f * land, 1f);
            _retMascot.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(_clock * 2.6f));
            _retArrow.anchoredPosition = new Vector2(0f, -58f + (below ? -1f : 1f) * 6f * Mathf.Abs(Mathf.Sin(_clock * Mathf.PI / 0.6f)));
        }

        // ---- Logo bulutları (kullanıcı: "logomuzun arkaplanına bulut efekti verelim") -------------------------

        private readonly List<(RectTransform t, Vector2 home, float amp, float speed, float phase, float flip)> _clouds =
            new List<(RectTransform, Vector2, float, float, float, float)>();
        private static Sprite _cloud;
        private const float CloudAspect = 0.625f;

        /// <summary>Logonun arkasında birbirine binen yumuşak bulut kümeleri; yavaşça süzülür (Tick).</summary>
        private void LogoClouds(RectTransform page, Vector2 center, float logoW, float logoH)
        {
            var layer = UiFactory.Rect("LogoClouds", page);
            UiFactory.Place(layer, new Vector2(0.5f, 1f), center, new Vector2(logoW, logoH));
            // (x, y, genişlik, saydamlık, yön): arkadakiler soluk ve küçük, öndekiler beyaz ve büyük.
            var puffs = new (float x, float y, float w, float a, float flip)[]
            {
                (-420f, 34f, 300f, 0.78f, 1f), (430f, 40f, 320f, 0.78f, -1f), (-150f, 52f, 360f, 0.8f, -1f), (190f, 56f, 380f, 0.8f, 1f),
                (-330f, -18f, 470f, 0.96f, 1f), (330f, -14f, 480f, 0.96f, -1f), (0f, -30f, 620f, 0.97f, 1f),
            };
            var rnd = new System.Random(11);
            foreach (var p in puffs)
            {
                var img = UiFactory.Image("Cloud", layer, CloudSprite, new Color(1f, 1f, 1f, p.a));
                img.raycastTarget = false;
                var home = new Vector2(p.x, p.y);
                UiFactory.Place(img.rectTransform, new Vector2(0.5f, 0.5f), home, new Vector2(p.w, p.w * CloudAspect));
                _clouds.Add((img.rectTransform, home, 10f + (float)rnd.NextDouble() * 10f, 0.25f + (float)rnd.NextDouble() * 0.2f, (float)rnd.NextDouble() * 6f, p.flip));
            }
        }

        /// <summary>Kabarık bulut: üst üste daireler, düz taban, üstte beyaz, altta hafif mavi-gri gölge.</summary>
        private static Sprite CloudSprite
        {
            get
            {
                if (_cloud) return _cloud;
                // Doku oranı CloudAspect (0.625): bütün daireler dokunun içinde kalır (kenarlar kesik görünmez);
                // alt kenar da küçük dairelerle kabarık, yalnız en altı yumuşakça erir.
                const int w = 256, h = 160;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "logo_cloud", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
                var px = new Color32[w * h];
                var circles = new[]
                {
                    new Vector3(-0.30f, -0.04f, 0.15f), new Vector3(-0.12f, 0.05f, 0.19f), new Vector3(0.08f, 0.08f, 0.21f),
                    new Vector3(0.27f, 0.0f, 0.16f), new Vector3(0.38f, -0.09f, 0.09f), new Vector3(-0.41f, -0.10f, 0.08f),
                    new Vector3(-0.22f, -0.13f, 0.11f), new Vector3(0.0f, -0.12f, 0.13f), new Vector3(0.21f, -0.12f, 0.11f),
                };
                for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float x = (i + 0.5f) / w - 0.5f, y = ((j + 0.5f) / h - 0.5f) * CloudAspect;
                    float d = 1f;
                    foreach (var c in circles) d = Mathf.Min(d, Mathf.Sqrt((x - c.x) * (x - c.x) + (y - c.y) * (y - c.y)) - c.z);
                    // Yumuşak kenar: dış sınır ~0.045 genişlikte erir (keskin köşe/çizgi yok).
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.012f - d) / 0.045f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((y + 0.28f) / 0.07f));
                    if (a <= 0f) { px[j * w + i] = new Color32(255, 255, 255, 0); continue; }
                    float shade = Mathf.Clamp01((y + 0.2f) / 0.32f);
                    var col = Color.Lerp(new Color(0.82f, 0.88f, 0.97f), Color.white, shade * 0.85f + Mathf.Clamp01(-d / 0.05f) * 0.15f);
                    col.a = a;
                    px[j * w + i] = col;
                }
                tex.SetPixels32(px);
                tex.Apply(true, false);
                _cloud = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
                _cloud.hideFlags = HideFlags.DontSave;
                return _cloud;
            }
        }

        /// <summary>Haritada y yüksekliğinde yolun ortası (içerik x'i, merkez 0). Şerit yoksa eski S eğrisi.</summary>
        private static float RoadX(float y)
        {
            if (!LoadRoad()) return PathAmp * Mathf.Sin((y - StartY - SignGap) / NodeStep * 0.82f);
            int k = Mathf.FloorToInt(y / _stripPart);
            var road = Road(ThemeAtY((k + 0.5f) * _stripPart));
            if (road == null) return PathAmp * Mathf.Sin((y - StartY - SignGap) / NodeStep * 0.82f);
            float u = (y - k * _stripPart) / _stripPart;
            float row = StripPartOf(k) * road.Part + (1f - u) * road.Part;
            float t = Mathf.Repeat(row, road.Period) / road.Step;
            int i = Mathf.FloorToInt(t);
            float a = road.X[i % road.X.Length], b = road.X[(i + 1) % road.X.Length];
            return (Mathf.Lerp(a, b, t - i) - 0.5f) * StripW;
        }

        private static float NodeY(int i) => StartY + SignGap + i * NodeStep + (i / ThemeDefinition.RegionSize) * SignGap;

        /// <summary>Bölge tabelası: bölgenin ilk düğümüyle bir önceki düğüm arasındaki boşluğun ortası (yolun üstü).</summary>
        private static float SignY(int region) => NodeY(region * ThemeDefinition.RegionSize) - (SignGap + NodeStep) * 0.5f;

        /// <summary>Yolun kontrol noktası: -1 başlangıç (ilk tabelanın altı), total bitiş (son tabelanın üstü).</summary>
        private static Vector2 PathPoint(int k, int total)
        {
            if (k < 0) return new Vector2(RoadX(20f), 20f);
            if (k >= total) { float ye = NodePos(total - 1).y + NodeStep + 260f; return new Vector2(RoadX(ye), ye); }
            return NodePos(k);
        }

        private static float EndSignY(int total) => NodePos(total - 1).y + NodeStep + 120f;

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        /// <param name="isCompleted">Bölüm indeksi tamamlandı mı.</param>
        /// <param name="sheet">Odaklanılacak bölge (100'lük, 0 tabanlı). Mevcut bölüm o bölgedeyse ona, değilse bölgenin başına
        /// odaklanır; negatifse mevcut bölüme.</param>
        public void ShowLevels(int total, int highestUnlocked, Func<int, bool> isCompleted, Action back, int sheet = -1, Action onSettings = null)
        {
            total = Mathf.Max(1, total);
            int regions = Mathf.Max(1, (total + ThemeDefinition.RegionSize - 1) / ThemeDefinition.RegionSize);
            int current = Mathf.Clamp(highestUnlocked, 0, total - 1);
            int focusRegion = sheet < 0 ? ThemeDefinition.RegionOf(current) : Mathf.Clamp(sheet, 0, regions - 1);
            CurrentSheet = focusRegion;
            Theme = ThemeDefinition.ForLevel(focusRegion * ThemeDefinition.RegionSize);
            var page = NewPage(Theme.MapSprite);
            var m = _map = new MapState { Total = total, Current = current, Regions = regions, HighestUnlocked = highestUnlocked, IsCompleted = isCompleted };
            if (StarsOf != null) for (int i = 0; i < total; i++) m.StarSum += StarsOf(i);
            if (ExtraStars != null) m.StarSum += ExtraStars(); // günlük bulmaca yıldızları
            _playIndex = current;
            _onPlay = () => LevelChosen?.Invoke(current);

            // Kaydırılabilir harita: tam ekran; üst kart ve alt menü haritanın üstünde yüzer (referans).
            var viewport = UiFactory.Rect("Viewport", page);
            UiFactory.Stretch(viewport, 0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var content = UiFactory.Rect("Content", viewport);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            m.Height = PathPoint(total, total).y + 120f;
            content.sizeDelta = new Vector2(1000f, m.Height);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 60f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            m.Scroll = scroll;
            m.Content = content;
            m.Viewport = viewport;
            _mapContent = content;

            // Katmanlar: gölge < kenar < dolgu < taş çizgisi < tabelalar < düğümler (parçalar sonradan eklense de sıra korunur).
            // v5: dünya resmi yolla birlikte kayar (bölgenin teması, sanal karolar; eklemlerde sis bandı).
            _bgTiles.Clear();
            _layerBg = Layer(content, "World");
            m.LayerShadow = Layer(content, "RoadShadow");
            m.LayerEdge = Layer(content, "RoadEdge");
            m.LayerFill = Layer(content, "RoadFill");
            m.LayerDash = Layer(content, "RoadStones");
            var signs = Layer(content, "Signs");
            m.LayerNodes = Layer(content, "Nodes");
            m.LayerFog = Layer(content, "CloudFog");     // açılmamış bölümlerin üstünde bulut örtüsü
            m.LayerPuffs = Layer(content, "CloudPuffs");
            m.LayerPin = Layer(content, "MascotPin"); // maskot bütün düğümlerin üstünde
            // Bulut sınırı: oyuncunun ulaştığı bölümün hemen üstü. Oturumda önceki gösterimden ilerlediyse aradaki
            // bulutlar açılış animasyonuyla kalkar.
            m.CloudEdge = CloudEdgeY(current);
            // Açılış animasyonu yalnızca birkaç bölümlük ilerlemede (oyna → kazan → haritaya dön).
            m.CloudFrom = _seenFrontier >= 0 && current > _seenFrontier && current - _seenFrontier <= 3 ? CloudEdgeY(_seenFrontier) : m.CloudEdge;
            _seenFrontier = current;
            _cloudStart = _clock;

            for (int r = 0; r < regions; r++) RegionSign(signs, r, total);
            EndSign(signs, total);

            // v10 Mavi Liman: sabit üst kabuk (mor sahne, logo, altın şerit, bölge etiketi, Ayarlar, yıldız kapsülü).
            // Dinamik bölge parçaları SetHeaderRegion → HeaderCard ile kurulur (MenuView.Shell.cs).
            BuildReturnButton(page);
            BuildMapShell(page, m.StarSum, onSettings, out m.HeadBack, out m.Head);
            m.Prev = UiFactory.CircleButton("Prev", m.Head, false, UiIcon.Back, 96f, () => ScrollToRegion(m.HeaderRegion - 1), "icons/back");
            UiFactory.Place((RectTransform)m.Prev.transform, new Vector2(0.5f, 0.5f), new Vector2(-400f, 0f), new Vector2(96f, 96f));
            m.Next = UiFactory.CircleButton("Next", m.Head, false, UiIcon.Back, 96f, () => ScrollToRegion(m.HeaderRegion + 1), "icons/next");
            UiFactory.Place((RectTransform)m.Next.transform, new Vector2(0.5f, 0.5f), new Vector2(400f, 0f), new Vector2(96f, 96f));

            // Açılış konumu: mevcut bölüm (ya da istenen bölgenin başı) görünüm alanının ortasında.
            Canvas.ForceUpdateCanvases();
            m.ViewH = viewport.rect.height > 1f ? viewport.rect.height
                : UiAspect.CanvasRef(UiAspect.Current).y - 500f;
            float focusY = ThemeDefinition.RegionOf(current) == focusRegion ? NodePos(current).y : NodePos(focusRegion * ThemeDefinition.RegionSize).y;
            SetScroll(focusY - m.ViewH * 0.5f);
            scroll.onValueChanged.AddListener(_ => UpdateMap());
            UpdateMap();

            Tabs(page, 1, back, () => { }, null);
            Tick(0f);
        }

        private static RectTransform Layer(RectTransform content, string name)
        {
            var rt = UiFactory.Rect(name, content);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private float ScrollY => _map == null ? 0f : -_map.Content.anchoredPosition.y;

        private void SetScroll(float y)
        {
            float max = Mathf.Max(0f, _map.Height - _map.ViewH);
            _map.Content.anchoredPosition = new Vector2(0f, -Mathf.Clamp(y, 0f, max));
        }

        private void ScrollToRegion(int region)
        {
            if (_map == null || region < 0 || region >= _map.Regions) return;
            _map.Scroll.StopMovement();
            // Tabela görünüm alanının alt üçte birinde: bölgenin ilk düğümleri ve tabelası birlikte görünür.
            _map.ScrollTarget = SignY(region) - _map.ViewH * 0.25f;
        }

        /// <summary>
        /// Ölçüm (E paketi): haritayı adım adım kaydırıp her adımda görünür aralığı güncellemenin süresini ölçer.
        /// Dönüş: ms cinsinden ortalama, en yüksek ve toplam adım; harita yoksa null.
        /// </summary>
        public double[] MeasureMapScroll(int steps, float stepPx)
        {
            if (_map == null || _map.Content == null) return null;
            var sw = new System.Diagnostics.Stopwatch();
            double sum = 0, max = 0;
            float y = ScrollY, top = Mathf.Max(0f, _map.Height - _map.ViewH);
            for (int s = 0; s < steps; s++)
            {
                y += stepPx;
                if (y > top) { y = top; stepPx = -Mathf.Abs(stepPx); }
                if (y < 0f) { y = 0f; stepPx = Mathf.Abs(stepPx); }
                SetScroll(y);
                sw.Restart();
                UpdateMap();
                sw.Stop();
                double ms = sw.Elapsed.TotalMilliseconds;
                sum += ms; if (ms > max) max = ms;
            }
            return new[] { sum / Mathf.Max(1, steps), max, steps };
        }

        /// <summary>Haritayı verilen kaydırma konumuna götürür (ölçüm/test).</summary>
        public void ScrollMapForTest(float y) { if (_map != null) { SetScroll(y); UpdateMap(); } }

        /// <summary>Haritanın şu anki kaydırma konumu (ölçüm/test/tanıtım kaydı).</summary>
        public float ScrollYForTest => ScrollY;

        /// <summary>
        /// Tanıtım açılışı (2026-11-12): harita dışındaki her şeyi gizler (üst kabuk, alt menü, dönüş düğmesi, harita
        /// maskotu); <paramref name="numbers"/> false ise düğüm numaraları da gizlenir (harita 3D zeminde ters dönebilir).
        /// Kaydırınca yeni kurulan düğümler için yeniden çağrılır.
        /// </summary>
        public void IsolateMapForTrailer(bool numbers)
        {
            if (_map == null || _map.Viewport == null) return;
            foreach (Transform c in _map.Viewport.parent) if (c != _map.Viewport) c.gameObject.SetActive(false);
            if (_map.LayerPin != null) _map.LayerPin.gameObject.SetActive(false);
            foreach (var g in _map.LayerNodes.GetComponentsInChildren<Graphic>(true)) if (g.name == "Num") g.enabled = numbers;
        }

        /// <summary>Tanıtım açılışı: i. düğümün harita içerik konumu ve y yüksekliğinde yolun ortası.</summary>
        public static Vector2 NodePosForTrailer(int i) => NodePos(i);
        public static float RoadXForTrailer(float y) => RoadX(y);

        /// <summary>Haritanın toplam yüksekliği (ölçüm/test).</summary>
        public float MapHeightForTest => _map != null ? _map.Height : 0f;

        /// <summary>NodeY(i) ≥ y olan en küçük i (yoksa total). NodeY kesin artan.</summary>
        private static int FirstNodeAtOrAbove(float y, int total)
        {
            int lo = 0, hi = total;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (NodeY(mid) < y) lo = mid + 1; else hi = mid;
            }
            return lo;
        }

        /// <summary>Görünen aralığın düğüm ve yol parçalarını kurar, uzaktakileri kaldırır; başlık ve arka planı bölgeye eşitler.</summary>
        private void UpdateMap()
        {
            var m = _map;
            if (m == null || m.Content == null) return;
            float y0 = ScrollY - Margin, y1 = ScrollY + m.ViewH + Margin;

            // Düğümler. E paketi: NodeY artan olduğu için ilk görünür düğüm ikili aramayla bulunur (önceden her
            // kaydırmada 0'dan taranıyordu; 10.000 bölümde haritanın üst ucunda adım başı ~5,6 ms).
            int first = FirstNodeAtOrAbove(y0, m.Total);
            var keep = new HashSet<int>();
            for (int i = first; i < m.Total; i++)
            {
                float y = NodeY(i);
                if (y < y0) continue;
                if (y > y1) break;
                keep.Add(i);
                if (m.Nodes.ContainsKey(i)) continue;
                var holder = Layer(m.LayerNodes, "L" + (i + 1));
                // Görünüm gerçek ilerlemeden (kapalı bölüm numarasız, bulut altında); test "hepsi açık" yalnızca dokunmayı açar.
                bool unlocked = i <= m.HighestUnlocked;
                Node(holder, NodePos(i), i, unlocked, m.IsCompleted(i), i == m.Current, PremiumArt.V4("ui/level-node-neutral"), UnlockAll);
                m.Nodes[i] = holder.gameObject;
            }
            var drop = new List<int>();
            foreach (var kv in m.Nodes) if (!keep.Contains(kv.Key)) drop.Add(kv.Key);
            foreach (var i in drop) { UiFactory.Kill(m.Nodes[i]); m.Nodes.Remove(i); }

            // Yol parçaları (25 bölümlük; komşu parçalar aynı uç noktayı paylaşır, yuvarlak uç birleşimi gizler).
            int chunks = Mathf.CeilToInt((m.Total + 1) / ChunkLevels);
            var keepC = new HashSet<int>();
            for (int c = Mathf.Max(0, first / (int)ChunkLevels - 1); c < chunks; c++)
            {
                int k0 = c * (int)ChunkLevels - 1, k1 = Mathf.Min(m.Total, (c + 1) * (int)ChunkLevels - 1);
                float a = PathPoint(k0, m.Total).y, b = PathPoint(k1, m.Total).y;
                if (a > y1) break;
                if (b < y0) continue;
                keepC.Add(c);
                if (!m.Chunks.ContainsKey(c)) m.Chunks[c] = BuildChunk(k0, k1);
            }
            var dropC = new List<int>();
            foreach (var kv in m.Chunks) if (!keepC.Contains(kv.Key)) dropC.Add(kv.Key);
            foreach (var c in dropC) { foreach (var go in m.Chunks[c]) UiFactory.Kill(go); m.Chunks.Remove(c); }

            // Dünya karoları (görünen aralık).
            // v6: şerit varsa karo yüksekliği şerit parçasıdır; sahil bölgelerinde parçalar ek yeri görünmeden
            // birleşir (sis yok). Şeridi olmayan temalar eski resmi kullanır; tema değişen eklemde sis bandı kalır.
            bool strip = LoadRoad();
            float tileH = strip ? _stripPart : 1240f * 1672f / 941f * 0.72f;
            int t0 = Mathf.Max(0, Mathf.FloorToInt(y0 / tileH)), t1 = Mathf.FloorToInt(y1 / tileH);
            var keepT = new HashSet<int>();
            for (int k = t0; k <= t1; k++)
            {
                if (k * tileH > m.Height) break;
                keepT.Add(k);
                if (_bgTiles.ContainsKey(k)) continue;
                // Tema RoadX ile aynı kuraldan (karonun ortası): resim ve bölüm çizgisi aynı şeride bakar.
                var theme = ThemeAtY((k + 0.5f) * tileH);
                var below = k > 0 ? ThemeAtY((k - 0.5f) * tileH) : theme;
                var road = strip ? Road(theme) : null;
                bool useStrip = road != null;
                bool belowStrip = k == 0 || (strip && Road(below) != null);
                var holder = Layer(_layerBg, "Tile" + k);
                var rt = UiFactory.Rect("World", holder);
                rt.gameObject.AddComponent<CanvasRenderer>();
                var raw = rt.gameObject.AddComponent<RawImage>();
                raw.raycastTarget = false;
                float cy = (k + 0.5f) * tileH, tileW = StripW;
                if (useStrip)
                {
                    var sp = road.Sprites[StripPartOf(k)];
                    raw.texture = sp.texture;
                    var r0 = sp.textureRect;
                    float tw = sp.texture.width, th = sp.texture.height;
                    raw.uvRect = new Rect(r0.x / tw, r0.y / th, r0.width / tw, r0.height / th);
                }
                else
                {
                    // Resmin gökyüzü kısmı (üst %28) kesilir; oran korunur (yanlar taşar).
                    var sp = theme.MapSprite ?? theme.HomeSprite;
                    if (sp != null)
                    {
                        raw.texture = sp.texture;
                        var r0 = sp.textureRect;
                        float tw = sp.texture.width, th = sp.texture.height;
                        raw.uvRect = new Rect(r0.x / tw, r0.y / th, r0.width / tw, r0.height / th * 0.72f);
                        tileW = tileH * r0.width / (r0.height * 0.72f);
                    }
                    if (k % 2 == 1) rt.localScale = new Vector3(-1f, 1f, 1f);
                }
                Pos(rt, new Vector2(0f, cy), new Vector2(tileW, tileH));
                // Şeritten şeride eklem nehrin ortasıdır (iki şeritte de su + köprü): sis gerekmez.
                if (!useStrip || !belowStrip)
                {
                    // Karo eklemi: yumuşak sis bandı (iki farklı resim arasındaki çizgi görünmesin).
                    var mist = UiFactory.Image("Mist", holder, View.SpecialArt.Glow, new Color(1f, 0.99f, 0.95f, 0.85f));
                    Pos(mist.rectTransform, new Vector2(0f, k * tileH), new Vector2(tileW * 1.6f, 360f));
                }
                _bgTiles[k] = holder.gameObject;
            }
            var dropT = new List<int>();
            foreach (var kv in _bgTiles) if (!keepT.Contains(kv.Key)) dropT.Add(kv.Key);
            foreach (var k in dropT) { UiFactory.Kill(_bgTiles[k]); _bgTiles.Remove(k); }

            UpdateClouds(y0, y1);

            // Ekranın ortasındaki bölge: başlık kartı ve arka plan.
            float mid = ScrollY + m.ViewH * 0.5f;
            int region = 0;
            for (int r = 0; r < m.Regions; r++) if (SignY(r) <= mid) region = r;
            if (region != m.HeaderRegion) SetHeaderRegion(region);
        }

        private static ThemeDefinition TileTheme(MapState m, float y)
        {
            int reg = 0;
            for (int r = 0; r < m.Regions; r++) if (SignY(r) <= y) reg = r;
            return ThemeDefinition.ForLevel(reg * ThemeDefinition.RegionSize);
        }

        private GameObject[] BuildChunk(int k0, int k1)
        {
            var m = _map;
            // v6: bölüm çizgisi resimdeki yolun orta çizgisini birebir izler (12 birimde bir örnek).
            var pts = new List<Vector2>();
            float ya = PathPoint(k0, m.Total).y, yb = PathPoint(k1, m.Total).y;
            for (float y = ya; y < yb; y += 12f) pts.Add(new Vector2(RoadX(y), y));
            pts.Add(new Vector2(RoadX(yb), yb));
            var theme = ThemeDefinition.ForLevel(Mathf.Clamp(k0 + 1, 0, m.Total - 1));
            bool desert = theme.Id == "desert";
            Color edge = desert ? new Color32(0xB9, 0x8A, 0x55, 0xFF) : theme.Id == "garden" ? new Color32(0xB9, 0xA5, 0x80, 0xFF) : new Color32(0xC2, 0xA8, 0x82, 0xFF);
            Color fill = desert ? new Color32(0xF4, 0xDF, 0xB8, 0xFF) : new Color32(0xF7, 0xEC, 0xD6, 0xFF);
            Color accent = desert ? new Color32(0xEE, 0x8C, 0x4A, 0xFF) : theme.Id == "garden" ? new Color32(0x6F, 0xC2, 0x5E, 0xFF) : new Color32(0x3F, 0xB8, 0xD4, 0xFF);
            var shadowPts = new List<Vector2>(pts.Count);
            foreach (var p in pts) shadowPts.Add(p + new Vector2(0f, -10f));
            return new[]
            {
                // v5 (kullanıcı referansı): ince, iki renkli çizgili şerit; dünya resminin içinden akar.
                Road(m.LayerShadow, shadowPts, 50f, new Color(0.18f, 0.12f, 0.05f, 0.25f)),
                Road(m.LayerEdge, pts, 42f, Color.white),
                Road(m.LayerFill, pts, 32f, fill),
                Road(m.LayerDash, pts, 32f, accent, 16f, 16f),
            };
        }

        private static GameObject Road(RectTransform layer, List<Vector2> pts, float width, Color color, float dash = 0f, float gap = 0f)
        {
            var rt = Layer(layer, "Road");
            rt.gameObject.AddComponent<CanvasRenderer>();
            var g = rt.gameObject.AddComponent<TrailGraphic>();
            g.color = color;
            g.SetPath(pts, width, dash, gap);
            return rt.gameObject;
        }

        /// <summary>Bölge tabelası yolun üstünde: "2 — BAHÇE" çipi, bölge adı ve aralık; kenarda küçük resim.</summary>
        private void RegionSign(RectTransform layer, int region, int total)
        {
            var theme = ThemeDefinition.ForLevel(region * ThemeDefinition.RegionSize);
            int first = region * ThemeDefinition.RegionSize + 1, last = Mathf.Min(total, (region + 1) * ThemeDefinition.RegionSize);
            var holder = Layer(layer, "Sign" + (region + 1));
            var card = UiFactory.Image("Card", holder, UiFactory.Art("ui/next-preview-panel", "btn_cream") ?? UiArt.Box, Color.white, true);
            Pos(card.rectTransform, new Vector2(0f, SignY(region)), new Vector2(660f, 150f));
            UiFactory.FitSliced(card, 150f);
            Thumb(card.rectTransform, theme, new Vector2(98f, 0f), new Vector2(124f, 96f));
            ChapterChip(card.rectTransform, region, theme, new Vector2(280f, 30f), TextAnchor.MiddleLeft);
            Line(card.rectTransform, $"{first}–{last}", 26, new Vector2(-60f, 30f), 150f, TextAnchor.MiddleRight, UiArt.Brown);
            var title = UiFactory.Title("Region", card.rectTransform, ThemeDefinition.RegionTitle(region), 46, UiArt.HudTealDark, Color.white);
            title.alignment = UiTypography.Alignment(TextAnchor.MiddleLeft);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(180f + 210f, -24f), new Vector2(420f, 60f));
            FitText(title, 28);
        }

        private static void EndSign(RectTransform layer, int total)
        {
            var holder = Layer(layer, "EndSign");
            var card = UiFactory.Image("Card", holder, UiFactory.Art("ui/button-secondary-cream", "btn_cream") ?? UiArt.Box, Color.white, true);
            Pos(card.rectTransform, new Vector2(0f, EndSignY(total)), new Vector2(560f, 120f));
            UiFactory.FitSliced(card, 120f);
            var t = UiFactory.Text("Text", card.transform, Loc.T("ui.soon"), 36, UiArt.Brown);
            UiFactory.Stretch(t.rectTransform, 50, 50, 0, 8);
            FitText(t, 22);
        }

        /// <summary>Başlık kartını bölgeye göre yeniden kurar; arka planı yumuşak geçişle değiştirir.</summary>
        private void SetHeaderRegion(int region)
        {
            var m = _map;
            m.HeaderRegion = region;
            CurrentSheet = region;
            var theme = ThemeDefinition.ForLevel(region * ThemeDefinition.RegionSize);
            if (m.Card != null) UiFactory.Kill(m.Card.gameObject);
            int first = region * ThemeDefinition.RegionSize + 1, last = Mathf.Min(m.Total, (region + 1) * ThemeDefinition.RegionSize);
            HeaderCard(m.Head, region, first, last, m.StarSum);
            // v5: bölge okları yerine üst çubukta yıldız toplamı ve ayarlar (kaydırma yeterli).
            m.Prev.gameObject.SetActive(false);
            m.Next.gameObject.SetActive(false);

            if (theme != Theme)
            {
                // Arka plan geçişi: eski resim üstte sönümlenir (320 ms).
                if (_bgFade != null && _bg.sprite != null)
                {
                    _bgFade.sprite = _bg.sprite;
                    _bgFade.enabled = true;
                    _bgFade.GetComponent<AspectRatioFitter>().aspectRatio = _bgFit.aspectRatio;
                    _bgFadeT = 0f;
                }
                Theme = theme;
                if (theme.MapSprite != null)
                {
                    _bg.sprite = theme.MapSprite;
                    _bg.enabled = true;
                    _bgFit.aspectRatio = theme.MapSprite.rect.width / theme.MapSprite.rect.height;
                }
            }
        }

        private static Text Line(RectTransform parent, string text, int size, Vector2 pos, float width, TextAnchor anchor, Color color)
        {
            var t = UiFactory.Text("Line", parent, text, size, color, anchor, FontStyle.Normal);
            UiFactory.Place(t.rectTransform, new Vector2(anchor == TextAnchor.MiddleRight ? 1f : 0f, 0.5f), pos + new Vector2(anchor == TextAnchor.MiddleRight ? -width * 0.5f : width * 0.5f, 0f), new Vector2(width, size + 12f));
            return t;
        }

        /// <summary>Yazı alana sığmazsa küçülür (taşma yok).</summary>
        private static void FitText(Text t, int min)
        {
            t.fontSizeMax = t.fontSize;
            t.fontSizeMin = min;
            t.enableAutoSizing = true;
        }

        /// <summary>
        /// v5 düğüm (kullanıcı referansı): kilitli = küçük sade disk, numarasız; tamamlanan = turkuaz numaralı disk + altında
        /// yıldızlar; mevcut = büyük, hale, üstünde zıplayan maskot iğnesi. Bölümün mekaniği küçük karakter simgesi olarak
        /// kilitli düğümde bile görünür (oyuncu ileride ne geleceğini görür); 10'un katı final bölümleri altın halkalı.
        /// </summary>
        private void Node(RectTransform content, Vector2 pos, int index, bool unlocked, bool done, bool isCurrent, Sprite nodeSprite, bool tappable = false)
        {
            bool final = (index + 1) % 10 == 0;
            if (isCurrent)
            {
                var halo = UiFactory.Image("Halo", content, UiFactory.Art("effects/glow", "fx_glow") ?? UiArt.Circle, new Color(1f, 0.85f, 0.35f, 0.6f));
                Pos(halo.rectTransform, pos, new Vector2(260f, 260f));
                _halo = halo.rectTransform;
            }
            float size = isCurrent ? 150f : unlocked ? 112f : 78f;
            var tint = !unlocked ? new Color(0.96f, 0.92f, 0.84f) : isCurrent ? new Color(0.35f, 0.9f, 0.86f) : new Color(0.28f, 0.74f, 0.66f);
            var node = UiFactory.Image("Level" + (index + 1), content, nodeSprite ?? UiArt.Circle, tint);
            node.preserveAspect = true;
            node.raycastTarget = unlocked || tappable;
            Pos(node.rectTransform, pos, new Vector2(size, size));
            if (isCurrent || final)
            {
                var ring = node.gameObject.AddComponent<Outline>();
                ring.effectColor = Gold;
                ring.effectDistance = final && !isCurrent ? new Vector2(3f, -3f) : new Vector2(5f, -5f);
            }
            if (unlocked)
            {
                var num = UiFactory.Title("Num", node.transform, (index + 1).ToString(), index + 1 >= 1000 ? (isCurrent ? 40 : 32) : index + 1 >= 100 ? (isCurrent ? 50 : 40) : (isCurrent ? 60 : 48),
                    UiArt.Cream, UiArt.HudTealDark);
                UiFactory.Stretch(num.rectTransform, 6, 6, 0, 8);
                num.fontSizeMax = num.fontSize;
                num.fontSizeMin = 22;
                num.enableAutoSizing = true;
            }
            int stars = done && StarsOf != null ? StarsOf(index) : 0;
            if (stars > 0)
            {
                var starSprite = PremiumArt.Get("icon_star") ?? ProceduralArt.StarSprite;
                for (int k = 0; k < 3; k++)
                {
                    var st = UiFactory.Image("Star", node.transform, starSprite, k < stars ? Color.white : new Color(0.35f, 0.35f, 0.4f, 0.75f));
                    st.preserveAspect = true;
                    UiFactory.Place(st.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((k - 1) * 32f, k == 1 ? -66f : -60f), new Vector2(34f, 34f));
                }
            }
            // Mekanik karakteri (kilitli düğümde de görünür).
            var mech = MechanicIcon(index);
            if (mech.sprite != null)
            {
                float ms = isCurrent ? 64f : unlocked ? 54f : 50f;
                var bubble = UiFactory.Image("MechBubble", node.transform, UiArt.Circle, new Color(1f, 0.98f, 0.92f, 1f));
                UiFactory.Place(bubble.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(size * 0.42f, size * 0.42f), new Vector2(ms, ms));
                var bo = bubble.gameObject.AddComponent<Outline>();
                bo.effectColor = new Color(UiArt.Gold.r, UiArt.Gold.g, UiArt.Gold.b, 0.9f);
                bo.effectDistance = new Vector2(2f, -2f);
                var ic = UiFactory.Image("Mech", bubble.transform, mech.sprite, mech.tint);
                ic.preserveAspect = true;
                UiFactory.Stretch(ic.rectTransform, 6, 6, 6, 6);
            }
            if (isCurrent)
            {
                // v6 (kullanıcı: "bölüm noktasının altında kalıyor… hareketli ve belirgin olsun"): maskot bütün
                // düğümlerin üstündeki katmanda, büyük, beyaz kenarlı ve gölgeli; düğümün üstünde zıplar, ezilip
                // uzar, sallanır; yerde gölgesi zıplamayla küçülür (Tick → _hero, _heroShadow).
                var layer = _map != null && _map.LayerPin != null ? _map.LayerPin : content;
                if (_pinHolder != null) UiFactory.Kill(_pinHolder.gameObject); // düğüm yeniden kurulunca tek maskot
                var holder = Layer(layer, "Mascot");
                _pinHolder = holder;
                var shadow = UiFactory.Image("MascotShadow", holder, View.SpecialArt.Glow, new Color(0.1f, 0.08f, 0.05f, 0.45f));
                Pos(shadow.rectTransform, pos + new Vector2(0f, 60f), new Vector2(120f, 30f));
                _heroShadow = shadow.rectTransform;
                // Kullanıcı: "haritadaki maskotun gözleri ve ağzı karşıya baksın, oyun alanındaki gibi": sabit resim
                // yerine oyun alanının parçalı, hareketli maskotu (göz kırpar, bakar). Kap ayak tabanında; zıplama,
                // ezilme ve sallanma kaba uygulanır (Tick → _hero).
                var hop = UiFactory.Rect("MascotPin", holder);
                Pos(hop, pos + new Vector2(0f, 52f), Vector2.zero);
                const float mw = 150f;
                MascotView.Create(hop, new Vector2(0f, MascotView.FeetBelowAnchor(mw)), hop, mw);
                _hero = hop;
                _heroBase = hop.anchoredPosition.y;
            }
            if (unlocked || tappable)
            {
                var btn = node.gameObject.AddComponent<Button>();
                btn.targetGraphic = node;
                btn.transition = Selectable.Transition.None;
                int i = index;
                btn.onClick.AddListener(() => ShowStartCard(i));
                PressFeedback.Attach(btn);
            }
        }

        /// <summary>Bölümün kullandığı en belirgin mekaniğin simgesi (yoksa boş).</summary>
        private (Sprite sprite, Color tint) MechanicIcon(int index)
        {
            var caps = CapsOf?.Invoke(index);
            if (caps == null) return (null, Color.white);
            string[] order = { "gate", "paint", "linkedClip", "sponge", "rotor", "envelope", "colorLock", "ice", "clip", "star" };
            foreach (var cap in order)
            {
                if (!caps.Contains(cap)) continue;
                switch (cap)
                {
                    case "gate": return (View.SpecialArt.Button, Color.white);
                    case "paint": return (View.SpecialArt.Drop, Color.white);
                    case "linkedClip": return (TileSkin.Sprite("mechanics/clip-closed") ?? ProceduralArt.ClipSprite, new Color(0.55f, 0.9f, 0.95f));
                    case "sponge": return (View.SpecialArt.Sponge, Color.white);
                    case "rotor": return (PremiumArt.V4("mechanics/rotor-base") ?? ProceduralArt.StarSprite, Color.white);
                    case "envelope": return (PremiumArt.Get("ov_envelope"), Color.white);
                    case "colorLock": return (PremiumArt.V4("icons/lock") ?? UiArt.Icon(UiIcon.Lock), Color.white);
                    case "ice": return (View.SpecialArt.Ice(false), Color.white);
                    case "clip": return (TileSkin.Sprite("mechanics/clip-closed") ?? ProceduralArt.ClipSprite, Color.white);
                    case "star": return (PremiumArt.Get("icon_star") ?? ProceduralArt.StarSprite, Color.white);
                }
            }
            return (null, Color.white);
        }

        /// <summary>v5: bölüm başına yetenek listesi (mekanik simgesi için).</summary>
        public Func<int, IList<string>> CapsOf;
        private float _heroBase = HeroY;
        private RectTransform _heroShadow, _pinHolder;
        private RectTransform _startCard;

        /// <summary>Başlangıç kartı: bölüm numarası, yıldızlar, mekanik ve OYNA; dışına dokununca kapanır.</summary>
        private void ShowStartCard(int index)
        {
            if (_startCard != null) UiFactory.Kill(_startCard.gameObject);
            if (Ui3.Ready) { ShowStartCardUi3(index); return; } // ui3/21
            var root = UiFactory.Stretch(UiFactory.Rect("StartCard", _page));
            _startCard = root;
            var dim = UiFactory.Image("Dim", root, null, new Color(0.05f, 0.1f, 0.15f, 0.45f));
            UiFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var close = dim.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(() => { UiFactory.Kill(root.gameObject); _startCard = null; });
            var card = UiFactory.Image("Card", root, UiFactory.Art("ui/modal-panel", "panel") ?? UiArt.Box, Color.white, true);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(760f, 640f));
            card.raycastTarget = true;
            var title = UiFactory.Title("Title", card.transform, Loc.F("ui.levelbig", index + 1), 74, UiArt.Cream, UiArt.HudTealDark);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(620f, 100f));
            title.fontSizeMax = 74; title.fontSizeMin = 40; title.enableAutoSizing = true;
            int stars = StarsOf != null ? StarsOf(index) : 0;
            var starSprite = PremiumArt.Get("icon_star") ?? ProceduralArt.StarSprite;
            for (int k = 0; k < 3; k++)
            {
                var st = UiFactory.Image("Star", card.transform, starSprite, k < stars ? Color.white : new Color(0.55f, 0.52f, 0.58f, 0.8f));
                st.preserveAspect = true;
                UiFactory.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2((k - 1) * 110f, k == 1 ? -230f : -215f), new Vector2(k == 1 ? 110f : 90f, k == 1 ? 110f : 90f));
            }
            var mech = MechanicIcon(index);
            string mechName = MechanicName(index);
            if (mech.sprite != null)
            {
                var ic = UiFactory.Image("Mech", card.transform, mech.sprite, mech.tint);
                ic.preserveAspect = true;
                UiFactory.Place(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(-170f, -345f), new Vector2(90f, 90f));
            }
            var mt = UiFactory.Text("MechName", card.transform, mechName, 38, UiArt.Brown, mech.sprite != null ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            UiFactory.Place(mt.rectTransform, new Vector2(0.5f, 1f), new Vector2(mech.sprite != null ? 60f : 0f, -345f), new Vector2(mech.sprite != null ? 360f : 600f, 60f));
            mt.fontSizeMax = 38; mt.fontSizeMin = 24; mt.enableAutoSizing = true;
            int i = index;
            var play = UiFactory.PillButton("Play", card.transform, Loc.T("ui.playbig"), true, new Vector2(480f, 156f), () => { UiFactory.Kill(root.gameObject); _startCard = null; LevelChosen?.Invoke(i); });
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(460f, 140f));
        }

        private string MechanicName(int index)
        {
            var caps = CapsOf?.Invoke(index);
            if (caps == null) return Loc.T("intro.default");
            if (caps.Contains("gate")) return Loc.T("intro.gate");
            if (caps.Contains("paint")) return Loc.T("intro.paint");
            if (caps.Contains("linkedClip")) return Loc.T("intro.linkedClip");
            if (caps.Contains("sponge")) return Loc.T("intro.sponge");
            if (caps.Contains("rotor")) return Loc.T("intro.rotor");
            if (caps.Contains("envelope")) return Loc.T("intro.envelope");
            if (caps.Contains("colorLock")) return Loc.T("intro.colorLock");
            if (caps.Contains("ice")) return Loc.T("intro.ice");
            if (caps.Contains("clip")) return Loc.T("intro.clip");
            if (caps.Contains("star")) return Loc.T("intro.star");
            return caps.Contains("specials") ? Loc.T("intro.specials") : Loc.T("intro.default");
        }

        private static void Pos(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // ---- Ayarlar ------------------------------------------------------------------------------

        // Kullanıcı: "ayarlar kısmını yeniden tasarla, arka planında resim olmasın". Kodla üretilen yumuşak
        // turkuaz→krem renk geçişi + süzülen yarı saydam kabarcıklar; ortada krem kart: Ses ve Titreşim anahtarları,
        // Dil seçimi, kısa not; altta sürüm.

        /// <summary>Dil seçildi (kod). Oyun ekranları yeni dille yeniden kurar.</summary>
        public event Action<string> LanguagePicked;

        public void ShowSettings(bool sound, bool vibration, Action back, Action onLevels = null)
        {
            if (Ui1.Ready) { ShowSettingsUi1(sound, vibration, back, onLevels); return; } // ui1/05
            var page = NewPage(null);
            var grad = UiFactory.Image("Gradient", page, GradientSprite, Color.white);
            UiFactory.Stretch(grad.rectTransform, -200f, -200f, -400f, -400f);
            SettingsBubbles(page);

            var title = UiFactory.Title("Title", page, Loc.T("ui.settings"), 92, UiArt.Cream, UiArt.HudTealDark);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(900f, 130f));
            FitText(title, 56);

            const float cardW = 920f, rowH = 150f;
            var panelSprite = UiFactory.Art("ui/modal-panel");
            var card = UiFactory.Image("Card", page, panelSprite ?? UiArt.Box, panelSprite != null ? Color.white : UiArt.Ivory, true);
            float cardH = 3 * rowH + 330f;
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(cardW, cardH));
            UiFactory.FitSliced(card, 260f);
            var cr = card.rectTransform;
            float y = cardH * 0.5f - 90f - rowH * 0.5f;
            SwitchRow(cr, y, UiIcon.SoundOn, UiIcon.SoundOff, Loc.T("ui.sound"), sound, v => SoundChanged?.Invoke(v));
            y -= rowH;
            RowDivider(cr, y + rowH * 0.5f);
            SwitchRow(cr, y, UiIcon.VibrateOn, UiIcon.VibrateOff, Loc.T("ui.vibration"), vibration, v => VibrationChanged?.Invoke(v));
            y -= rowH;
            RowDivider(cr, y + rowH * 0.5f);
            LanguageRow(cr, y);
            y -= rowH * 0.5f;
            var note = UiFactory.Text("Note", cr, Loc.T("ui.settingsnote"), 30, new Color(UiArt.Brown.r, UiArt.Brown.g, UiArt.Brown.b, 0.75f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Place(note.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y - 80f), new Vector2(cardW - 160f, 120f));
            note.enableAutoSizing = true; note.fontSizeMax = 30; note.fontSizeMin = 20;

            var ver = UiFactory.Text("Version", page, "Stone Burst " + Application.version + "  ·  OFIGAME", 26, new Color32(0x6B, 0x55, 0x48, 0xC0), TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Place(ver.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, BarBottom + BarH + 50f), new Vector2(800f, 40f));
            Tabs(page, 2, back, onLevels ?? back, () => { });
        }

        private static void RowDivider(RectTransform card, float y)
        {
            var d = UiFactory.Image("Divider", card, UiArt.Box, new Color(0.29f, 0.21f, 0.18f, 0.12f), true);
            UiFactory.Place(d.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(760f, 4f));
        }

        /// <summary>Simge + yazı solda, sağda kayan anahtar (turkuaz açık / gri kapalı, krem topuz). Satırın tamamı dokunulur.</summary>
        private void SwitchRow(RectTransform card, float y, UiIcon onIcon, UiIcon offIcon, string label, bool initial, Action<bool> changed)
        {
            bool state = initial;
            var row = UiFactory.SpriteButton(label, card, null, new Vector2(820f, 140f), null);
            UiFactory.Place((RectTransform)row.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(820f, 140f));
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            var disc = UiFactory.Image("IconDisc", row.transform, UiArt.Circle, UiArt.HudTeal);
            UiFactory.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(96f, 96f));
            var icon = UiFactory.Image("Icon", disc.transform, UiArt.Icon(onIcon), UiArt.Cream);
            UiFactory.Stretch(icon.rectTransform, 18, 18, 18, 18);
            var t = UiFactory.Text("Label", row.transform, label, 46, UiArt.Brown, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(130f + 220f, 0f), new Vector2(440f, 90f));
            FitText(t, 28);
            var track = UiFactory.Image("Track", row.transform, UiArt.Box, UiArt.HudTeal, true);
            track.pixelsPerUnitMultiplier = UiArt.Box.border.x / 38f;
            UiFactory.Place(track.rectTransform, new Vector2(1f, 0.5f), new Vector2(-96f, 0f), new Vector2(172f, 76f));
            var knob = UiFactory.Image("Knob", track.transform, UiArt.Circle, UiArt.Cream);
            var ks = knob.gameObject.AddComponent<Shadow>();
            ks.effectColor = new Color(0f, 0f, 0f, 0.25f);
            ks.effectDistance = new Vector2(0f, -3f);
            var stateText = UiFactory.Text("State", track.transform, "", 24, Color.white);
            UiFactory.Stretch(stateText.rectTransform);
            stateText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            stateText.enableAutoSizing = true; stateText.fontSizeMax = 24; stateText.fontSizeMin = 13;
            void Refresh()
            {
                track.color = state ? UiArt.HudTeal : new Color32(0xC9, 0xBF, 0xB2, 0xFF);
                disc.color = state ? UiArt.HudTeal : new Color32(0xB5, 0xAA, 0x9C, 0xFF);
                icon.sprite = UiArt.Icon(state ? onIcon : offIcon);
                UiFactory.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(state ? 48f : -48f, 0f), new Vector2(62f, 62f));
                Loc.Set(stateText, state ? Loc.T("ui.on") : Loc.T("ui.off"));
                stateText.rectTransform.offsetMin = new Vector2(state ? 10f : 82f, 0f);
                stateText.rectTransform.offsetMax = new Vector2(state ? -82f : -10f, 0f);
                stateText.color = state ? UiArt.Cream : new Color(0.29f, 0.21f, 0.18f, 0.75f);
            }
            row.onClick.AddListener(() => { state = !state; Refresh(); changed(state); });
            Refresh();
        }

        private void LanguageRow(RectTransform card, float y)
        {
            var row = UiFactory.Rect("Language", card);
            UiFactory.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(820f, 140f));
            var disc = UiFactory.Image("IconDisc", row, UiArt.Circle, UiArt.HudTeal);
            UiFactory.Place(disc.rectTransform, new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(96f, 96f));
            var glyph = UiFactory.Text("Glyph", disc.transform, "Aa", 40, UiArt.Cream);
            UiFactory.Stretch(glyph.rectTransform, 0, 0, 0, 4);
            var t = UiFactory.Text("Label", row, Loc.T("ui.language"), 46, UiArt.Brown, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(130f + 140f, 0f), new Vector2(280f, 90f));
            FitText(t, 28);
            var btn = UiFactory.PillButton("Pick", row, Loc.Current.NativeName, true, new Vector2(340f, 120f), ShowLanguagePicker);
            UiFactory.Place((RectTransform)btn.transform, new Vector2(1f, 0.5f), new Vector2(-170f, 0f), new Vector2(340f, 120f));
            var label = btn.GetComponentInChildren<Text>();
            if (label != null) label.text = ArabicShaper.ShapeLines(Loc.Current.NativeName);
        }

        public void OpenLanguagePickerForCapture() => ShowLanguagePicker();

        /// <summary>Dokuz dil, her biri kendi yazısıyla; geçerli dil turkuaz. Dışarı dokununca kapanır.</summary>
        private void ShowLanguagePicker()
        {
            if (Ui3.Ready) { ShowLanguagePickerUi3(); return; } // ui3/22
            var root = UiFactory.Stretch(UiFactory.Rect("LanguagePicker", _page));
            var dim = UiFactory.Image("Dim", root, UiArt.Box, new Color(0.03f, 0.08f, 0.1f, 0.6f));
            UiFactory.Stretch(dim.rectTransform, -200f, -200f, -400f, -400f);
            dim.raycastTarget = true;
            var dimBtn = dim.gameObject.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(() => UiFactory.Kill(root.gameObject));
            var panelSprite = UiFactory.Art("ui/modal-panel");
            var card = UiFactory.Image("Card", root, panelSprite ?? UiArt.Box, panelSprite != null ? Color.white : UiArt.Ivory, true);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(980f, 940f));
            UiFactory.FitSliced(card, 260f);
            card.raycastTarget = true;
            var title = UiFactory.Title("Title", card.transform, Loc.T("ui.language"), 64, UiArt.HudTealDark, UiArt.Cream);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(700f, 100f));
            var langs = Loc.Languages;
            for (int i = 0; i < langs.Length; i++)
            {
                var lang = langs[i];
                bool cur = lang.Code == Loc.Code;
                int r = i / 3, c = i % 3;
                var b = UiFactory.PillButton(lang.Code, card.transform, lang.NativeName, cur, new Vector2(280f, 136f), () =>
                {
                    UiFactory.Kill(root.gameObject);
                    if (lang.Code != Loc.Code) LanguagePicked?.Invoke(lang.Code);
                });
                UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2((c - 1) * 296f, -250f - r * 168f), new Vector2(280f, 136f));
                // Diller kendi yazısıyla: Arapça her dilde şekillendirilmiş görünür.
                var label = b.GetComponentInChildren<Text>();
                if (label != null) label.text = ArabicShaper.ShapeLines(lang.NativeName);
            }
            var close = UiFactory.PillButton("Close", card.transform, Loc.T("ui.close"), false, new Vector2(380f, 132f), () => UiFactory.Kill(root.gameObject));
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(380f, 132f));
        }

        private static Sprite _gradient;

        /// <summary>Ayarlar zemini: üstte turkuaz, ortada nane, altta krem (resim değil, kodla üretilir).</summary>
        private static Sprite GradientSprite
        {
            get
            {
                if (_gradient) return _gradient;
                const int h = 256;
                var tex = new Texture2D(4, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "settings_gradient", hideFlags = HideFlags.DontSave };
                Color top = new Color32(0x22, 0xA3, 0xA0, 0xFF), mid = new Color32(0x9A, 0xDB, 0xCD, 0xFF), bottom = new Color32(0xF7, 0xEC, 0xD8, 0xFF);
                var px = new Color32[4 * h];
                for (int y = 0; y < h; y++)
                {
                    float t = y / (h - 1f);
                    var c = t > 0.5f ? Color.Lerp(mid, top, (t - 0.5f) * 2f) : Color.Lerp(bottom, mid, t * 2f);
                    for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return _gradient = Sprite.Create(tex, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        /// <summary>Zeminde yavaş yükselen yarı saydam kabarcıklar (ortam hareketiyle canlanır).</summary>
        private void SettingsBubbles(RectTransform page)
        {
            var rnd = new System.Random(7);
            for (int i = 0; i < 14; i++)
            {
                float s = 40f + (float)rnd.NextDouble() * 120f;
                var img = UiFactory.Image("Bubble", page, UiArt.Circle, new Color(1f, 1f, 1f, 0.2f));
                var home = new Vector2(-480f + (float)rnd.NextDouble() * 960f, -900f + (float)rnd.NextDouble() * 1500f);
                UiFactory.Place(img.rectTransform, new Vector2(0.5f, 0.5f), home, new Vector2(s, s));
                _ambient.Add((img.rectTransform, img, home, 6f + (float)rnd.NextDouble() * 8f, (float)rnd.NextDouble() * 12f));
            }
        }

        // ---- Ortam hareketi (unscaled; v3 §3: düşük genlik, hitbox sabit) ----------------------------------

        private void Update() => Tick(Time.unscaledDeltaTime);

        /// <summary>Heykel ±4 birim/2.8 s, OYNA 1.00→1.02/2.5 s, mevcut düğüm halesi 0.45→0.75/2 s, ortam parçacıkları.</summary>
        public void Tick(float dt)
        {
            _clock += dt;
            TickToast();
            TickReturn(dt);
            TickClouds();
            if (_map != null && _map.ScrollTarget.HasValue)
            {
                // ‹ › ile bölge geçişi: yumuşak kaydırma (~0.35 s).
                float cur = ScrollY, target = Mathf.Clamp(_map.ScrollTarget.Value, 0f, Mathf.Max(0f, _map.Height - _map.ViewH));
                float next = Mathf.Lerp(cur, target, 1f - Mathf.Exp(-12f * dt));
                if (Mathf.Abs(next - target) < 2f || dt <= 0f) { next = target; _map.ScrollTarget = null; }
                SetScroll(next);
                UpdateMap();
            }
            if (_bgFade != null && _bgFade.enabled)
            {
                _bgFadeT += dt / 0.32f;
                _bgFade.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(_bgFadeT));
                if (_bgFadeT >= 1f) _bgFade.enabled = false;
            }
            if (_hero != null && _map != null)
            {
                // Harita maskotu: 1,1 s'lik zıplama; yere değerken ezilir, havada uzar, hafif sallanır; her 4. zıplama
                // daha yüksek ve bir tur döner gibi eğilir (heyecan). Gölge yükseklikle küçülür.
                const float hop = 1.1f;
                float t = _clock / hop, ph = t - Mathf.Floor(t);
                bool big = Mathf.FloorToInt(t) % 4 == 3;
                float h = Mathf.Sin(ph * Mathf.PI) * (big ? 46f : 24f);
                float land = ph < 0.12f ? 1f - ph / 0.12f : ph > 0.92f ? (ph - 0.92f) / 0.08f : 0f;
                float air = Mathf.Sin(ph * Mathf.PI);
                float sx = 1f + 0.12f * land - 0.05f * air, sy = 1f - 0.12f * land + 0.07f * air;
                _hero.anchoredPosition = new Vector2(_hero.anchoredPosition.x, _heroBase + h);
                _hero.localScale = new Vector3(sx, sy, 1f);
                _hero.localRotation = Quaternion.Euler(0f, 0f, 5f * Mathf.Sin(_clock * 2.3f) + (big ? -8f * air * Mathf.Sin(ph * 2f * Mathf.PI) : 0f));
                if (_heroShadow != null)
                {
                    float k = 1f - h / 80f;
                    _heroShadow.localScale = new Vector3(k, k, 1f);
                }
            }
            else if (_hero != null) _hero.anchoredPosition = new Vector2(_hero.anchoredPosition.x, _heroBase + 4f * Mathf.Sin(_clock * 2f * Mathf.PI / 2.8f));
            if (_play != null && _play.GetComponent<PressFeedback>().Idle)
            {
                float s = 1.01f + 0.01f * Mathf.Sin(_clock * 2f * Mathf.PI / 2.5f);
                _play.localScale = new Vector3(s, s, 1f);
            }
            foreach (var cl in _clouds)
            {
                // Logo bulutları: yavaş yana süzülme ve hafif kabarma.
                float ph = _clock * cl.speed + cl.phase;
                cl.t.anchoredPosition = cl.home + new Vector2(cl.amp * Mathf.Sin(ph), 3f * Mathf.Sin(ph * 1.7f));
                float s = 1f + 0.025f * Mathf.Sin(ph * 1.3f + 1f);
                cl.t.localScale = new Vector3(cl.flip * s, s, 1f);
            }
            foreach (var a in _ambient)
            {
                // 12 s döngü: yavaş yükselir, hafif yana salınır, başta ve sonda söner.
                float cyc = ((_clock + a.phase) % 12f) / 12f;
                a.t.anchoredPosition = a.home + new Vector2(14f * Mathf.Sin((_clock + a.phase) * 0.9f), a.speed * 12f * cyc);
                a.t.localRotation = Quaternion.Euler(0f, 0f, 20f * Mathf.Sin((_clock + a.phase) * 0.6f));
                var c = a.img.color;
                c.a = 0.55f * Mathf.Sin(cyc * Mathf.PI);
                a.img.color = c;
            }
            if (_halo != null)
            {
                var img = _halo.GetComponent<Image>();
                var c = img.color;
                c.a = 0.6f + 0.15f * Mathf.Sin(_clock * 2f * Mathf.PI / 2f);
                img.color = c;
            }
        }
    }
}
