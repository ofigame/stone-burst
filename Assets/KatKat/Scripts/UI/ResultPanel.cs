using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Bölüm sonu ve diğer modal kartlar. Zafer kartı tek resim değildir: karartma → boş krem panel → ayrı
    /// madalya → ayrı mercan bant → gerçek başlık metni → gerçek hedef satırı → ayrı nane düğme (rehber §5).
    /// Akış (ms): 0–120 karartma, 120–340 panel 0.85→1.03, 340–430 →1.0, 430 madalya + konfeti, 500 bant,
    /// 600+70·i hedef onayları, 760 düğmeler etkin. Karta dokunmak girişi atlar. Zaman ölçeksiz saatle akar;
    /// Advance(ms) ekran yakalama ve testler için aynı akışı elle ilerletir.
    /// </summary>
    public sealed class ResultPanel : MonoBehaviour
    {
        public enum Style { Victory, Plain }

        public const float DimEnd = 120f, PopEnd = 340f, SettleEnd = 430f, MedalAt = 430f, RibbonAt = 500f, ChecksAt = 600f, ButtonsAt = 760f;
        private const int ConfettiCount = 26;

        private Style _style;
        private float _t;
        private Image _dim;
        private RectTransform _card, _medal, _ribbon, _fxLayer;
        private CanvasGroup _cardGroup, _buttonsGroup;
        private readonly List<RectTransform> _checks = new List<RectTransform>();
        private readonly List<Button> _buttons = new List<Button>();
        private readonly List<Piece> _confetti = new List<Piece>();
        private bool _confettiStarted;

        public float IntroEnd => Mathf.Max(_style == Style.Victory ? Mathf.Max(ButtonsAt + 120f, _preview != null ? PreviewAt + PreviewMs : 0f) : PopEnd + 120f,
            _stars.Count > 0 ? MedalAt + 180f * 2f + 260f : 0f); // yıldızlar (günlük kartında da) dönüşünü bitirir
        public bool IntroDone => _t >= IntroEnd;

        /// <summary>Sonraki bölüm kartı (v3 §11): verisi hazırsa 850 ms'de 200 ms fade + 16 px yukarı yerleşme.</summary>
        public const float PreviewAt = 850f, PreviewMs = 200f, PreviewRise = 16f;

        private Color _dimColor = new Color(0.16f, 0.08f, 0.22f, 0.55f);
        private RectTransform _preview;
        private CanvasGroup _previewGroup;
        private float _previewY;
        private bool _acted;
        private string _ambientFx;

        public NextLevelPreviewView Preview { get; private set; }

        public static ResultPanel Create(Transform parent, Style style, string title, string subtitle, IList<GoalState> goals, params HudView.PanelButton[] buttons)
            => Create(parent, style, title, subtitle, goals, null, null, buttons);

        /// <param name="preview">Zafer kartında sonraki bölüm verisi (son bölümde IsEnd); null ise kart yok.</param>
        /// <param name="theme">Biten bölümün teması: karartma rengi ve ortam parçacığı; null ise varsayılan.</param>
        public static ResultPanel Create(Transform parent, Style style, string title, string subtitle, IList<GoalState> goals,
            NextLevelPreviewView.Data? preview, ThemeDefinition theme, params HudView.PanelButton[] buttons)
        {
            var root = UiFactory.Stretch(UiFactory.Rect("Panel", parent));
            var p = root.gameObject.AddComponent<ResultPanel>();
            p._style = style;
            if (theme != null)
            {
                p._dimColor = theme.DimColor;
                p._ambientFx = theme.AmbientFx;
            }
            p.Build(root, title, subtitle, goals, preview, buttons);
            p.Apply();
            return p;
        }

        private void Build(RectTransform root, string title, string subtitle, IList<GoalState> goals, NextLevelPreviewView.Data? preview, HudView.PanelButton[] buttons)
        {
            if (Ui3.Ready) _dimColor = Ui3.ScrimColor; // ui3: bütün modallar #111C24 / 0.72 (tema rengi yok)
            _dim = root.gameObject.AddComponent<Image>();
            _dim.color = new Color(_dimColor.r, _dimColor.g, _dimColor.b, 0f);
            _dim.raycastTarget = true;
            var skip = root.gameObject.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(Skip);

            bool victory = _style == Style.Victory;
            int goalRows = victory && goals != null && goals.Count > 0 ? 1 : 0;
            bool hasPreview = victory && preview.HasValue;
            float h = (victory ? 300f : 320f) + goalRows * 130f + buttons.Length * 166f + (string.IsNullOrEmpty(subtitle) ? 0f : 70f)
                + (hasPreview ? NextLevelPreviewView.Height - 30f : 0f);
            // Konfeti kartın arkasında: başlık ve düğmeleri örtmez.
            _fxLayer = UiFactory.Stretch(UiFactory.Rect("Fx", root));
            _card = UiFactory.Rect("Card", root);
            UiFactory.Place(_card, new Vector2(0.5f, 0.5f), new Vector2(0f, victory ? (hasPreview ? -90f : -60f) : -20f), new Vector2(Ui1.Ready ? 880f : 800f, h));
            _cardGroup = _card.gameObject.AddComponent<CanvasGroup>();
            var panelSprite = UiFactory.Art("ui/modal-panel", "panel");
            var bg = UiFactory.Image("Bg", _card, panelSprite ?? UiArt.Box, panelSprite != null ? Color.white : UiArt.Ivory, true);
            UiFactory.Stretch(bg.rectTransform);
            bool u1 = Ui1.Ready;
            if (u1)
            {
                bg.sprite = Ui1.Art("panel-modal"); bg.type = Image.Type.Sliced; bg.color = Color.white; bg.pixelsPerUnitMultiplier = bg.sprite.border.x / 60f; // ui1/14–16
                BuildUi1(root, title, subtitle, goals, preview, buttons, victory, hasPreview);
                return;
            }

            float y;
            if (victory)
            {
                var medalSprite = UiFactory.Art("ui/victory-medal", "medal");
                if (medalSprite != null)
                {
                    var m = UiFactory.Image("Medal", _card, medalSprite, Color.white);
                    m.preserveAspect = true;
                    UiFactory.Place(m.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 170f), new Vector2(460f, 330f));
                    _medal = m.rectTransform;
                }
                var ribbonSprite = u1 ? Ui1.Art("victory-ribbon") : UiFactory.Art("ui/victory-ribbon", "ribbon");
                var rib = UiFactory.Rect("Ribbon", _card);
                UiFactory.Place(rib, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(980f, 252f));
                if (ribbonSprite != null)
                {
                    var ri = UiFactory.Image("Band", rib, ribbonSprite, Color.white);
                    ri.preserveAspect = true;
                    UiFactory.Stretch(ri.rectTransform);
                }
                var t = UiFactory.Title("Title", rib, u1 ? Ui1.Case(title, false) : title, 92, UiArt.Cream, UiArt.CoralDark);
                UiFactory.Stretch(t.rectTransform, 120f, 120f, 40f, 70f);
                _ribbon = rib;
                y = -190f;
                if (goalRows > 0)
                {
                    var row = UiFactory.Rect("Goals", _card);
                    // Kartın iç alanında (çerçeveye değmez): 600 genişlik, taşlar hap yüksekliğinin içinde.
                    UiFactory.Place(row, new Vector2(0.5f, 1f), new Vector2(0f, y - 60f), new Vector2(600f, 112f));
                    var bar = UiFactory.Image("Bar", row, u1 ? Ui1.Art("chip-objective") : UiArt.Box, u1 ? Color.white : new Color(1f, 0.95f, 0.84f, 1f), true);
                    UiFactory.Stretch(bar.rectTransform);
                    bar.pixelsPerUnitMultiplier = u1 ? bar.sprite.rect.height / 112f : UiArt.Box.border.x / 56f;
                    var bo = bar.gameObject.AddComponent<Outline>();
                    bo.enabled = !u1;
                    bo.effectColor = new Color(UiArt.Gold.r, UiArt.Gold.g, UiArt.Gold.b, 0.9f);
                    bo.effectDistance = new Vector2(3f, -3f);
                    float step = 540f / goals.Count;
                    for (int i = 0; i < goals.Count; i++)
                    {
                        var g = goals[i];
                        var chip = UiFactory.Image("Goal", row, GoalChip.IconFor(g), Color.white);
                        chip.preserveAspect = true;
                        float x = -270f + step * (i + 0.5f) - 18f;
                        UiFactory.Place(chip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(80f, 80f));
                        var badge = UiFactory.Image("Check", row, UiArt.Circle, UiArt.CheckGreen);
                        UiFactory.Place(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x + 50f, -14f), new Vector2(46f, 46f));
                        var o = badge.gameObject.AddComponent<Outline>();
                        o.effectColor = Color.white;
                        o.effectDistance = new Vector2(2.5f, -2.5f);
                        var mark = UiFactory.Image("Mark", badge.transform, UiArt.Icon(UiIcon.Check), Color.white);
                        UiFactory.Stretch(mark.rectTransform, 4, 4, 4, 4);
                        _checks.Add(badge.rectTransform);
                    }
                    y -= 140f;
                }
            }
            else if (u1)
            {
                // ui1/14–15: lacivert başlık doğrudan krem panelde.
                var t = Ui1.Label("Title", _card, Ui1.Case(title, false), 76, Ui1.Navy);
                UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(760f, 100f));
                t.enableAutoSizing = true; t.fontSizeMax = 76; t.fontSizeMin = 34;
                y = -160f;
            }
            else
            {
                var pillSprite = UiFactory.Art("ui/hud-title-pill", "hud_title");
                var pill = UiFactory.Image("TitlePill", _card, pillSprite ?? UiArt.Box, pillSprite != null ? Color.white : UiArt.Teal, true);
                // Başlık hapı kartın içinde (önceden üst kenarın üstüne biniyordu: iki çerçeve üst üste).
                UiFactory.Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(600f, 124f));
                UiFactory.FitSliced(pill, 124f);
                var t = UiFactory.Title("Title", pill.transform, title, 52, UiArt.Cream, UiArt.HudTealDark);
                UiFactory.Stretch(t.rectTransform, 40f, 40f, 0f, 8f);
                t.fontSizeMax = 52;
                t.fontSizeMin = 30;
                t.enableAutoSizing = true;
                y = -190f;
            }

            if (!string.IsNullOrEmpty(subtitle) && u1)
            {
                // ui1: alt başlık krem kapsülde (Bölüm 38 · Puan / Hamlen kalmadı).
                var chip = Ui1.Chip("SubtitleChip", _card, new Vector2(0.5f, 1f), new Vector2(0f, y - 50f), new Vector2(660f, 104f));
                var st = Ui1.Label("Subtitle", chip.transform, subtitle, 38, Ui1.Navy);
                UiFactory.Stretch(st.rectTransform, 40, 40, 6, 10);
                st.enableAutoSizing = true; st.fontSizeMax = 38; st.fontSizeMin = 22;
                y -= 110f;
            }
            else if (!string.IsNullOrEmpty(subtitle))
            {
                var st = UiFactory.Text("Subtitle", _card, subtitle, 42, UiArt.Brown, TextAnchor.MiddleCenter, FontStyle.Normal);
                UiFactory.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 30f), new Vector2(700f, 80f));
                y -= 80f;
            }

            var btnRoot = UiFactory.Rect("Buttons", _card);
            UiFactory.Stretch(btnRoot);
            _buttonsGroup = btnRoot.gameObject.AddComponent<CanvasGroup>();
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                var size = b.Primary ? new Vector2(580f, 156f) : new Vector2(540f, 140f);
                var act = b.Action;
                // Hızlı çift dokunuş tek eylem: ilk basışta bütün kart düğmeleri kilitlenir (v3 §11, V20).
                Action guarded = () =>
                {
                    if (_acted) return;
                    _acted = true;
                    _buttonsGroup.interactable = false;
                    act?.Invoke();
                };
                Button btn;
                if (u1)
                {
                    size = b.Primary ? new Vector2(700f, 148f) : new Vector2(640f, 128f);
                    btn = Ui1.Button(b.Label, btnRoot, Ui1.Case(b.Label, true), b.Primary, new Vector2(0.5f, 1f), new Vector2(0f, y - 30f - size.y * 0.5f), size, guarded,
                        b.ShowsAdIcon ? UiArt.Icon(UiIcon.Play) : null, b.Primary ? 50 : 42);
                }
                else
                {
                    btn = UiFactory.PillButton(b.Label, btnRoot, b.Label, b.Primary, size, guarded);
                    UiFactory.Place((RectTransform)btn.transform, new Vector2(0.5f, 1f), new Vector2(0f, y - 30f - size.y * 0.5f), size);
                }
                y -= size.y + 18f;
                _buttons.Add(btn);
            }

            if (hasPreview)
            {
                // Sonraki bölüm kartı ayrı alt modül; etkileşimsiz (yalnızca DEVAM eylemdir).
                Preview = NextLevelPreviewView.Create(_card, preview.Value);
                _preview = (RectTransform)Preview.transform;
                _previewY = y - 12f - NextLevelPreviewView.Height * 0.5f;
                UiFactory.Place(_preview, new Vector2(0.5f, 1f), new Vector2(0f, _previewY), new Vector2(NextLevelPreviewView.Width, NextLevelPreviewView.Height));
                _previewGroup = _preview.gameObject.AddComponent<CanvasGroup>();
                _previewGroup.blocksRaycasts = false;
                y -= 12f + NextLevelPreviewView.Height;
            }
            FitCard(root, -y + (hasPreview ? 78f : 52f), victory);
        }

        // ---- ui1 yerleşimi (docs/ui1/14–16 yerlesim.json; 1080×1920 referans, kart üstüne göre) -------------------

        private bool _u1;
        private float _u1BadgeY = float.NaN;
        private Image _u1Chip;
        private TMPro.TextMeshProUGUI _u1Subtitle;

        /// <summary>
        /// ui1 kartları: 14 duraklat (929 genişlik, üst y 422), 15 kayıp (üst y 461; maskot kartın üstünden bakar),
        /// 16 zafer (940 genişlik, üst y 346: maskot + yıldızlar, kurdele, hedef "7/7 ✓", sonraki bölüm önizlemesi,
        /// kart içinde ZPE rozeti, Sonraki Bölüm, Tekrar Oyna). Düğme eylemleri, isimleri ve giriş akışı aynıdır.
        /// </summary>
        private void BuildUi1(RectTransform root, string title, string subtitle, IList<GoalState> goals, NextLevelPreviewView.Data? preview,
            HudView.PanelButton[] buttons, bool victory, bool hasPreview)
        {
            _u1 = true;
            if (Ui3.Ready) { BuildUi3(root, title, subtitle, goals, preview, buttons, victory, hasPreview); return; }
            float y;
            if (victory)
            {
                _card.sizeDelta = new Vector2(940f, _card.sizeDelta.y);
                // Maskot (göz kırpan, mevcut) kurdelenin arkasından sol üstte; yıldızlar SetStars ile sağda.
                var mascot = UiFactory.Image("Mascot", _card, SpecialArt.Mascot(SpecialArt.Mood.Happy), Color.white);
                mascot.preserveAspect = true;
                mascot.raycastTarget = false;
                UiFactory.Place(mascot.rectTransform, new Vector2(0.5f, 1f), new Vector2(-265f, 150f), new Vector2(300f, 290f));
                var rib = UiFactory.Rect("Ribbon", _card);
                UiFactory.Place(rib, new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(950f, 288f));
                var band = UiFactory.Image("Band", rib, Ui1.Art("victory-ribbon"), Color.white);
                band.preserveAspect = true;
                UiFactory.Stretch(band.rectTransform);
                var t = UiFactory.Title("Title", rib, Ui1.Case(title, false), 92, UiArt.Cream, UiArt.CoralDark);
                UiFactory.Stretch(t.rectTransform, 150f, 150f, 50f, 90f);
                _ribbon = rib;
                y = -249f;
                if (goals != null && goals.Count > 0)
                {
                    var chip = GoalRow(goals, y - 67f, 134f, true);
                    chip.name = "Goals";
                    y -= 134f;
                }
                if (hasPreview)
                {
                    Preview = NextLevelPreviewView.Create(_card, preview.Value);
                    _preview = (RectTransform)Preview.transform;
                    _previewY = y - 30f - NextLevelPreviewView.Height * 0.5f;
                    UiFactory.Place(_preview, new Vector2(0.5f, 1f), new Vector2(0f, _previewY), new Vector2(NextLevelPreviewView.Width, NextLevelPreviewView.Height));
                    _previewGroup = _preview.gameObject.AddComponent<CanvasGroup>();
                    _previewGroup.blocksRaycasts = false;
                    y -= 30f + NextLevelPreviewView.Height;
                }
                // ZPE rozeti yuvası (SetBadge): kart içinde, düğmelerin üstünde (16 ↔ yerlesim zpe 432×106).
                _u1BadgeY = y - 30f - 53f;
                y -= 30f + 106f + 20f;
            }
            else
            {
                _card.sizeDelta = new Vector2(929f, _card.sizeDelta.y);
                var t = Ui1.Label("Title", _card, Ui1.Case(title, false), 80, Ui1.Navy);
                UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(800f, 110f));
                t.enableAutoSizing = true; t.fontSizeMax = 80; t.fontSizeMin = 40;
                y = -215f;
                if (!string.IsNullOrEmpty(subtitle))
                {
                    // Bölüm/puan ya da kayıp nedeni kapsülü; kayıp kartında SetGoalSummary kalan hedefle değiştirir.
                    _u1Chip = Ui1.Chip("SubtitleChip", _card, new Vector2(0.5f, 1f), new Vector2(0f, y - 65f), new Vector2(640f, 130f));
                    _u1Subtitle = Ui1.Label("Subtitle", _u1Chip.transform, subtitle, 46, Ui1.Navy);
                    UiFactory.Stretch(_u1Subtitle.rectTransform, 50, 50, 8, 12);
                    _u1Subtitle.enableAutoSizing = true; _u1Subtitle.fontSizeMax = 46; _u1Subtitle.fontSizeMin = 24;
                    y -= 130f;
                }
                y -= 60f;
            }

            var btnRoot = UiFactory.Rect("Buttons", _card);
            UiFactory.Stretch(btnRoot);
            _buttonsGroup = btnRoot.gameObject.AddComponent<CanvasGroup>();
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                var act = b.Action;
                Action guarded = () =>
                {
                    if (_acted) return;
                    _acted = true;
                    _buttonsGroup.interactable = false;
                    act?.Invoke();
                };
                var size = b.Primary ? new Vector2(780f, 168f) : new Vector2(740f, 152f);
                if (i > 0 || !b.Primary) y -= 44f;
                // 16: ana eylem "Sonraki Bölüm" (aynı eylem; son bölümde mevcut etiket kalır).
                string text = victory && i == 0 && hasPreview && !preview.Value.IsEnd
                    ? Loc.T("ui.next").Replace("\\n", " ").Replace("\n", " ") : Ui1.Case(b.Label, true);
                var btn = Ui1.Button(b.Label, btnRoot, text, b.Primary, new Vector2(0.5f, 1f), new Vector2(0f, y - size.y * 0.5f), size, guarded,
                    b.ShowsAdIcon ? UiArt.Icon(UiIcon.Play) : null, b.Primary ? 58 : 50);
                y -= size.y;
                _buttons.Add(btn);
            }
            float h = -y + (victory ? 100f : 150f);
            _card.sizeDelta = new Vector2(_card.sizeDelta.x, h);
            // Kartın üst kenarı tasarımdaki yerde (duraklat 422, kayıp 461 → 440; zafer 346); üst HUD'un üstüne binebilir.
            float H = root.rect.height > 100f ? root.rect.height : HudView.RefHeight;
            float top = victory ? 346f : 440f;
            float topExtra = victory ? 330f : 0f;
            _fitScale = h + topExtra + 60f > H ? H / (h + topExtra + 60f) : 1f;
            float c = H * 0.5f - top - h * 0.5f;
            float cMax = H * 0.5f - 20f - _fitScale * (h * 0.5f + topExtra), cMin = -H * 0.5f + 30f + _fitScale * h * 0.5f;
            _card.anchoredPosition = new Vector2(0f, Mathf.Clamp(c, Mathf.Min(cMin, cMax), cMax));
        }

        // ---- ui3 (docs/ui3_onayli_tasarim/detaylar/sayfalar/14–16, 25, 27, 29, 30, 32; modal-varyantlari) ----------

        private bool _u3;
        private float _u3Top; // kart üst kenarının ekran üstünden uzaklığı (ref px)

        /// <summary>
        /// ui3 sonuç kartı: genişlik 860, kart yerel köken sol üst. Sade kart: başlık (56,80,748,80) modalTitle 54/800,
        /// alt başlık (56,184,748,104) bodyCentered, düğmeler (56, 320+140i, 748, 116); yükseklik 384+116n+24(n−1).
        /// Zafer: yıldızlar (230,−124,400,172), kurdele (16,16,828,152), hedefler (96,204,668,104), önizleme
        /// (96,344,668,300), ZPE (240,680,380,72), düğmeler 804/944. Grup dikeyde ortalanır, üst en az 48.
        /// Düğme sayısı, sırası, eylemleri ve giriş akışı aynı.
        /// </summary>
        private void BuildUi3(RectTransform root, string title, string subtitle, IList<GoalState> goals, NextLevelPreviewView.Data? preview,
            HudView.PanelButton[] buttons, bool victory, bool hasPreview)
        {
            _u3 = true;
            const float W = 860f;
            var bg = _card.Find("Bg").GetComponent<Image>();
            // Seramik modal tek katsayıyla (635 px → 860); üst omuz kartın 32 üstüne yükselir.
            bg.pixelsPerUnitMultiplier = bg.sprite.rect.width / W;
            UiFactory.Stretch(bg.rectTransform, 0f, 0f, -32f, 0f);
            Ui3Layers(_card);
            float Y(float top, float h) => -(top + h * 0.5f); // kart yerel üstten → çapa (0.5,1) merkez y
            float y;
            float groupAbove = 32f, groupBelow = 0f;
            if (victory)
            {
                var mascot = UiFactory.Image("Mascot", _card, SpecialArt.Mascot(SpecialArt.Mood.Happy), Color.white);
                mascot.preserveAspect = true;
                mascot.raycastTarget = false;
                UiFactory.Place(mascot.rectTransform, new Vector2(0.5f, 1f), new Vector2(-300f, 70f), new Vector2(220f, 210f));
                var rib = UiFactory.Rect("Ribbon", _card);
                UiFactory.Place(rib, new Vector2(0.5f, 1f), new Vector2(0f, Y(16f, 152f) - 6f), new Vector2(900f, 282f));
                var band = UiFactory.Image("Band", rib, Ui1.Art("victory-ribbon"), Color.white);
                band.preserveAspect = true;
                UiFactory.Stretch(band.rectTransform);
                var t = UiFactory.Text("Title", rib, Ui1.Case(title, false), 58, Ui3.OnTeal);
                t.raycastTarget = false;
                var ol = t.gameObject.AddComponent<Shadow>();
                ol.effectColor = new Color(0.36f, 0.16f, 0.12f, 0.55f);
                ol.effectDistance = new Vector2(0f, -3f);
                UiFactory.Stretch(t.rectTransform, 170f, 170f, 70f, 110f);
                t.enableAutoSizing = true; t.fontSizeMax = 58; t.fontSizeMin = 32;
                _ribbon = rib;
                y = 204f;
                if (goals != null && goals.Count > 0)
                {
                    var chip = GoalRowUi3(goals, Y(y, 104f), 104f, true);
                    chip.name = "Goals";
                    y += 104f + 36f;
                }
                if (hasPreview)
                {
                    Preview = NextLevelPreviewView.Create(_card, preview.Value);
                    _preview = (RectTransform)Preview.transform;
                    _previewY = Y(y, 300f);
                    UiFactory.Place(_preview, new Vector2(0.5f, 1f), new Vector2(0f, _previewY), new Vector2(668f, 300f));
                    _previewGroup = _preview.gameObject.AddComponent<CanvasGroup>();
                    _previewGroup.blocksRaycasts = false;
                    y += 300f + 36f;
                }
                _u1BadgeY = Y(y, 72f);
                y += 72f + 52f;
                groupAbove = 124f; // yıldızlar kartın üstünde
            }
            else
            {
                var t = Ui1.Label("Title", _card, Ui1.Case(title, false), 54, Ui3.Ink);
                UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Y(80f, 80f)), new Vector2(748f, 80f));
                t.enableAutoSizing = true; t.fontSizeMax = 54; t.fontSizeMin = 36;
                y = 184f;
                if (!string.IsNullOrEmpty(subtitle))
                {
                    // Alt başlık kapsülü (kayıpta SetGoalSummary kalan hedeflere döner).
                    _u1Chip = Ui1.Chip("SubtitleChip", _card, new Vector2(0.5f, 1f), new Vector2(0f, Y(y, 104f)), new Vector2(560f, 80f));
                    _u1Subtitle = Ui1.Label("Subtitle", _u1Chip.transform, subtitle, 36, Ui3.Ink);
                    UiFactory.Stretch(_u1Subtitle.rectTransform, 36, 36, 6, 8);
                    _u1Subtitle.enableAutoSizing = true; _u1Subtitle.fontSizeMax = 36; _u1Subtitle.fontSizeMin = 26;
                    float pw = _u1Subtitle.GetPreferredValues(subtitle, 9999f, 60f).x;
                    if (pw > 488f) _u1Chip.rectTransform.sizeDelta = new Vector2(748f, 80f);
                    y += 104f + 32f;
                }
            }

            var btnRoot = UiFactory.Rect("Buttons", _card);
            UiFactory.Stretch(btnRoot);
            _buttonsGroup = btnRoot.gameObject.AddComponent<CanvasGroup>();
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                var act = b.Action;
                Action guarded = () =>
                {
                    if (_acted) return;
                    _acted = true;
                    _buttonsGroup.interactable = false;
                    act?.Invoke();
                };
                if (i > 0) y += 24f;
                string text = victory && i == 0 && hasPreview && !preview.Value.IsEnd
                    ? Loc.T("ui.next").Replace("\\n", " ").Replace("\n", " ") : Ui1.Case(b.Label, true);
                float bh = b.ShowsAdIcon ? 136f : 116f; // reklam düğmesi biraz daha yüksek (2026-11-09)
                var btn = Ui1.Button(b.Label, btnRoot, text, b.Primary, new Vector2(0.5f, 1f), new Vector2(0f, Y(y, bh)), new Vector2(748f, bh), guarded,
                    b.ShowsAdIcon ? UiArt.Icon(UiIcon.Play) : null, 44);
                y += bh;
                _buttons.Add(btn);
            }
            float h = y + 56f;
            _card.sizeDelta = new Vector2(W, h);
            float H = root.rect.height > 100f ? root.rect.height : HudView.RefHeight;
            float group = groupAbove + h + groupBelow;
            _fitScale = group + 96f > H ? (H - 96f) / group : 1f;
            float top = Mathf.Max(48f, (H - group * _fitScale) * 0.5f) + groupAbove * _fitScale;
            _u3Top = top;
            _card.anchoredPosition = new Vector2(0f, H * 0.5f - top - h * 0.5f);
        }

        /// <summary>Seramik kartın katmanlı görünüşü (makette): arkada 18 px taşan kum tonlu ikinci kat, üstte ve altta pirinç baklava.</summary>
        internal static void Ui3Layers(RectTransform card)
        {
            var bg = card.Find("Bg").GetComponent<Image>();
            var step = UiFactory.Image("Step", card, bg.sprite, new Color(0.89f, 0.82f, 0.71f, 1f), true);
            step.raycastTarget = false;
            step.pixelsPerUnitMultiplier = bg.pixelsPerUnitMultiplier;
            var bo = bg.rectTransform;
            var srt = step.rectTransform;
            srt.anchorMin = bo.anchorMin; srt.anchorMax = bo.anchorMax;
            srt.offsetMin = bo.offsetMin + new Vector2(-16f, -16f);
            srt.offsetMax = bo.offsetMax + new Vector2(16f, 10f);
            step.transform.SetSiblingIndex(bg.transform.GetSiblingIndex());
            var sh = step.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(Ui3.Shadow.r, Ui3.Shadow.g, Ui3.Shadow.b, 0.28f);
            sh.effectDistance = new Vector2(0f, -14f);
            KatKat.UI.MenuView.U3Diamond(card, new Vector2(0.5f, 0f), new Vector2(0f, 16f), 16f);
        }

        private Image GoalRowUi3(IList<GoalState> goals, float centerY, float height, bool checks)
        {
            var chip = Ui1.Chip("GoalChip", _card, new Vector2(0.5f, 1f), new Vector2(0f, centerY), new Vector2(Mathf.Clamp(goals.Count * 230f + 80f, 400f, 668f), height));
            FillGoals(chip.rectTransform, goals, checks, height);
            return chip;
        }

        /// <summary>ui1 hedef kapsülü: her hedef için simge + "ilerleme/miktar" (+ zaferde yeşil onay), ortalı.</summary>
        private Image GoalRow(IList<GoalState> goals, float centerY, float height, bool checks)
        {
            const float itemW = 250f;
            float w = Mathf.Clamp(goals.Count * itemW + 90f, 605f, 880f);
            var chip = Ui1.Chip("GoalChip", _card, new Vector2(0.5f, 1f), new Vector2(0f, centerY), new Vector2(w, height));
            FillGoals(chip.rectTransform, goals, checks, height);
            return chip;
        }

        private void FillGoals(RectTransform chip, IList<GoalState> goals, bool checks, float height)
        {
            float itemW = Mathf.Min(goals.Count == 1 ? 330f : 250f, (chip.sizeDelta.x - 80f) / goals.Count);
            float icon = Mathf.Min(86f, height - 44f);
            for (int i = 0; i < goals.Count; i++)
            {
                var g = goals[i];
                float cx = -itemW * goals.Count * 0.5f + itemW * (i + 0.5f);
                var ic = UiFactory.Image("Goal", chip, GoalChip.IconFor(g), Color.white);
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                UiFactory.Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(cx - itemW * 0.5f + 20f + icon * 0.5f, 2f), new Vector2(icon, icon));
                var n = Ui1.Label("Count", chip, Mathf.Min(g.Progress, g.Amount) + "/" + g.Amount, 50, Ui1.TealDark);
                n.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                float tx = 20f + icon + 10f, tw = itemW - tx - (checks ? 56f : 10f);
                UiFactory.Place(n.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(cx - itemW * 0.5f + tx + tw * 0.5f, 2f), new Vector2(tw, height - 30f));
                n.enableAutoSizing = true; n.fontSizeMax = 50; n.fontSizeMin = 24;
                if (!checks || g.Progress < g.Amount) continue;
                var badge = UiFactory.Image("Check", chip, UiArt.Circle, UiArt.CheckGreen);
                UiFactory.Place(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(cx + itemW * 0.5f - 34f, 2f), new Vector2(48f, 48f));
                var mark = UiFactory.Image("Mark", badge.transform, UiArt.Icon(UiIcon.Check), Color.white);
                UiFactory.Stretch(mark.rectTransform, 5, 5, 5, 5);
                _checks.Add(badge.rectTransform);
            }
        }

        /// <summary>ui1/15: kayıp kartında alt başlık kapsülü kalan hedef özetine döner (gerçek hedeften).</summary>
        public void SetGoalSummary(IList<GoalState> goals)
        {
            if (!_u1 || _u1Chip == null || goals == null || goals.Count == 0) return;
            _u1Subtitle.gameObject.SetActive(false);
            float w = Mathf.Clamp(goals.Count * 250f + 90f, 605f, 880f);
            _u1Chip.rectTransform.sizeDelta = new Vector2(w, _u1Chip.rectTransform.sizeDelta.y);
            FillGoals(_u1Chip.rectTransform, goals, false, _u1Chip.rectTransform.sizeDelta.y);
        }

        /// <summary>ui1/16: kart içinde mor ZPE kapsülü (altın çerçeve), gerçek sonuç/değişim metni.</summary>
        private void BadgeUi1(string text)
        {
            if (_u3)
            {
                var p3 = Ui3.Surface("ZpeBadge", _card, 36f, Ui3.Purple, Ui3.PurpleDark, 72f, 3f, Ui3.Brass);
                UiFactory.Place(p3.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, _u1BadgeY), new Vector2(380f, 72f));
                var t3 = Ui1.Label("Text", p3.transform, text, 34, new Color32(0xFF, 0xDD, 0x6A, 0xFF));
                UiFactory.Stretch(t3.rectTransform, 20, 20, 4, 6);
                t3.enableAutoSizing = true; t3.fontSizeMax = 34; t3.fontSizeMin = 22;
                return;
            }
            var pill = UiFactory.Image("ZpeBadge", _card, UiArt.Box, Ui1.Purple, true);
            pill.raycastTarget = false;
            pill.pixelsPerUnitMultiplier = UiArt.Box.border.x / 53f;
            UiFactory.Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, _u1BadgeY), new Vector2(460f, 106f));
            var o = pill.gameObject.AddComponent<Outline>();
            o.effectColor = Ui1.Gold;
            o.effectDistance = new Vector2(4f, -4f);
            var t = Ui1.Label("Text", pill.transform, text, 50, new Color32(0xFF, 0xDD, 0x4A, 0xFF));
            UiFactory.Stretch(t.rectTransform, 24, 24, 6, 10);
            t.enableAutoSizing = true; t.fontSizeMax = 50; t.fontSizeMin = 24;
        }

        /// <summary>Düğme kilidini kaldırır: kart açık kalan bir eylem sonuçsuz dönünce (reklam bulunamadı/iptal)
        /// düğmeler yeniden basılabilir (2026-11-11 cihaz hatası: kayıp kartı kilitli kalıyordu).</summary>
        public void Unlock()
        {
            _acted = false;
            if (_buttonsGroup == null) return;
            _buttonsGroup.interactable = true;
            _buttonsGroup.blocksRaycasts = true;
        }

        /// <summary>Test: kart düğmeleri basılabilir mi.</summary>
        public bool Unlocked => !_acted && _buttonsGroup != null && _buttonsGroup.interactable;

        /// <summary>ui1/14: sağ üst kapat (duraklatmada "devam" ile aynı eylem).</summary>
        public void AddClose(Action act)
        {
            if (!_u1 || _card == null) return;
            var b = _u3
                ? Ui1.Round("Close", _card, "icon-close", new Vector2(1f, 1f), new Vector2(-24f, -16f), 96f, () => { if (_acted) return; _acted = true; act?.Invoke(); })
                : Ui1.Round("Close", _card, "icon-close", new Vector2(1f, 1f), new Vector2(-70f, -40f), 130f, () => { if (_acted) return; _acted = true; act?.Invoke(); });
            b.transform.SetParent(_buttonsGroup.transform, true);
        }

        private float _fitScale = 1f;

        /// <summary>
        /// Kart yüksekliği içerikten (alt boşluk kalmaz) ve yerleşim ekrandan: madalya üstteki HUD başlığına
        /// taşmasın, kart alta değmesin; sığmazsa kart bütün olarak küçülür (tablet/uzun içerik).
        /// </summary>
        private void FitCard(RectTransform root, float h, bool victory)
        {
            _card.sizeDelta = new Vector2(_card.sizeDelta.x, h);
            float H = root.rect.height > 100f ? root.rect.height : HudView.RefHeight;
            float topExtra = victory ? 335f : 10f;          // madalya (ya da başlık hapı) kartın üstünden taşan kısım
            const float hudTop = 262f, bottomPad = 30f;     // üst HUD (başlık + hamle sırası), alt kenar payı
            float avail = H - hudTop - bottomPad;
            float need = h + topExtra;
            _fitScale = need > avail ? avail / need : 1f;
            float top = H * 0.5f - hudTop, bottom = -H * 0.5f + bottomPad;
            float cMax = top - _fitScale * (h * 0.5f + topExtra), cMin = bottom + _fitScale * h * 0.5f;
            float c = Mathf.Clamp(_card.anchoredPosition.y, Mathf.Min(cMin, cMax), cMax);
            _card.anchoredPosition = new Vector2(0f, c);
        }

        /// <summary>Düğme eylemi bir kez çalıştı mı (çift dokunuş denetimi).</summary>
        public bool Acted => _acted;

        /// <summary>Test/araç için: düğmeyi oyuncu basışı gibi tetikler.</summary>
        public void Press(int index) => _buttons[index].onClick.Invoke();

        public void Skip()
        {
            if (_t < IntroEnd) Advance(IntroEnd - _t);
        }

        private void Update() => Advance(Time.unscaledDeltaTime * 1000f);

        private readonly List<RectTransform> _stars = new List<RectTransform>();
        private int _starsWon;

        /// <summary>v5: 1–3 yıldız madalyanın yerine yay şeklinde (orta büyük); kazanılmayanlar gri. 0 → değişiklik yok.</summary>
        public void SetStars(int won)
        {
            if (won <= 0 || _card == null) return;
            _starsWon = Mathf.Clamp(won, 0, 3);
            if (_medal != null) _medal.gameObject.SetActive(false);
            var sprite = PremiumArt.Get("icon_star") ?? ProceduralArt.StarSprite;
            var pos = new[] { new Vector2(-190f, 105f), new Vector2(0f, 160f), new Vector2(190f, 105f) };
            var size = new[] { 170f, 220f, 170f };
            if (_u1 && _style != Style.Victory)
            {
                // ui1 günlük kartı: yıldızlar kartın üst kenarının üstünde ortalı.
                pos = new[] { new Vector2(-170f, 120f), new Vector2(0f, 170f), new Vector2(170f, 120f) };
                size = new[] { 150f, 190f, 150f };
            }
            else if (_u3)
            {
                // ui3/16: yıldız grubu (230,−124,400,172): kartın üstünde ortada, ortadaki büyük.
                pos = new[] { new Vector2(-128f, 46f), new Vector2(0f, 84f), new Vector2(128f, 46f) };
                size = new[] { 128f, 164f, 128f };
            }
            else if (_u1)
            {
                // ui1/16: yıldızlar kurdelenin üstünde sağda, maskotun yanında.
                pos = new[] { new Vector2(-70f, 190f), new Vector2(75f, 240f), new Vector2(220f, 180f) };
                size = new[] { 150f, 190f, 150f };
            }
            for (int i = 0; i < 3; i++)
            {
                var img = UiFactory.Image("Star" + (i + 1), _card, sprite, i < _starsWon ? Color.white : new Color(0.5f, 0.48f, 0.55f, 0.9f));
                img.preserveAspect = true;
                UiFactory.Place(img.rectTransform, new Vector2(0.5f, 1f), pos[i], new Vector2(size[i], size[i]));
                img.rectTransform.SetSiblingIndex(1);
                img.rectTransform.localScale = Vector3.zero;
                _stars.Add(img.rectTransform);
            }
            Apply();
        }

        /// <summary>v10 P5: kartın üst kenarında küçük turkuaz rozet (bölüm sonu ZPE değişimi, ör. "ZPE 642 (+12)").</summary>
        /// <summary>ui1/15: kayıp kartının üstünden bakan mevcut maskot (yeni karakter yok).</summary>
        public void AddMascot()
        {
            if (_card == null) return;
            var m = UiFactory.Image("Mascot", _card, SpecialArt.Mascot(SpecialArt.Mood.Happy), Color.white);
            m.preserveAspect = true;
            m.raycastTarget = false;
            UiFactory.Place(m.rectTransform, new Vector2(0.5f, 1f), _u3 ? new Vector2(0f, 70f) : _u1 ? new Vector2(0f, 90f) : new Vector2(0f, 120f), _u3 ? new Vector2(300f, 270f) : _u1 ? new Vector2(440f, 400f) : new Vector2(340f, 300f));
            m.transform.SetAsFirstSibling();
        }

        public void SetBadge(string text)
        {
            if (string.IsNullOrEmpty(text) || _card == null) return;
            if (_u1 && !float.IsNaN(_u1BadgeY)) { BadgeUi1(text); return; }
            if (_u3)
            {
                // ui3 footer ZPE: kartın 24 altında, 64 yükseklik; petrol yeşili, pirinç kenar.
                var f3 = Ui3.Surface("ZpeBadge", _card, 32f, Ui3.TealTop, Ui3.TealBottom, 64f, 3f, Ui3.Brass);
                UiFactory.Place(f3.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -56f), new Vector2(300f, 64f));
                var ft = UiFactory.Text("Text", f3.transform, text, 30, Ui3.OnTeal);
                ft.raycastTarget = false;
                UiFactory.Stretch(ft.rectTransform, 16, 16, 2, 4);
                ft.enableAutoSizing = true; ft.fontSizeMax = 30; ft.fontSizeMin = 18;
                return;
            }
            var pill = UiFactory.Image("ZpeBadge", _card, UiArt.Box, new Color32(0x17, 0x8E, 0x88, 0xFF), true);
            pill.raycastTarget = false;
            UiFactory.Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -36f), new Vector2(360f, 64f));
            var o = pill.gameObject.AddComponent<Outline>();
            o.effectColor = new Color32(0xF2, 0xBE, 0x4A, 0xFF);
            o.effectDistance = new Vector2(3f, -3f);
            var t = UiFactory.Text("Text", pill.transform, text, 32, Color.white);
            t.raycastTarget = false;
            UiFactory.Stretch(t.rectTransform, 16, 16, 2, 4);
            t.enableAutoSizing = true; t.fontSizeMax = 32; t.fontSizeMin = 18;
        }

        /// <summary>Giriş akışını ve konfetiyi verilen süre (ms) kadar ilerletir.</summary>
        public void Advance(float ms)
        {
            if (ms <= 0f) return;
            float prev = _t;
            _t += ms;
            if (_style == Style.Victory && !_confettiStarted && _t >= MedalAt) StartConfetti();
            TickConfetti(ms);
            if (prev < IntroEnd || _confetti.Count > 0) Apply();
        }

        private void Apply()
        {
            _dim.color = new Color(_dimColor.r, _dimColor.g, _dimColor.b, _dimColor.a * Mathf.Clamp01(_t / DimEnd));
            if (_preview != null)
            {
                float p = Mathf.Clamp01((_t - PreviewAt) / PreviewMs);
                _previewGroup.alpha = Ease.OutCubic(p);
                _preview.anchoredPosition = new Vector2(0f, _previewY - PreviewRise * (1f - Ease.OutCubic(p)));
            }
            float pop = Mathf.Clamp01((_t - DimEnd) / (PopEnd - DimEnd));
            float settle = Mathf.Clamp01((_t - PopEnd) / (SettleEnd - PopEnd));
            float s = _t < PopEnd ? Mathf.Lerp(0.85f, 1.03f, Ease.OutCubic(pop)) : Mathf.Lerp(1.03f, 1f, Ease.InOutSine(settle));
            _card.localScale = new Vector3(s * _fitScale, s * _fitScale, 1f);
            _cardGroup.alpha = Mathf.Clamp01((_t - DimEnd) / 100f);

            if (_medal != null) _medal.localScale = Vector3.one * Overshoot((_t - MedalAt) / 220f);
            if (_ribbon != null)
            {
                float r = Mathf.Clamp01((_t - RibbonAt) / 180f);
                _ribbon.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, Ease.OutCubic(r)) * Overshoot((_t - RibbonAt) / 180f, 0.06f), 1f, 1f);
                _ribbon.gameObject.SetActive(_t >= RibbonAt);
            }
            for (int i = 0; i < _checks.Count; i++) _checks[i].localScale = Vector3.one * Overshoot((_t - ChecksAt - 70f * i) / 200f);
            // v5 yıldızlar: madalyanın yerinde yay şeklinde; kazanılanlar sırayla dönerek patlar.
            for (int i = 0; i < _stars.Count; i++)
            {
                float at = MedalAt + 180f * i;
                float x = (_t - at) / 260f;
                _stars[i].localScale = Vector3.one * (i < _starsWon ? Overshoot(x, 0.35f) : Mathf.Clamp01((_t - MedalAt) / 200f));
                _stars[i].localRotation = Quaternion.Euler(0f, 0f, i < _starsWon ? 180f * (1f - Mathf.Clamp01(x)) : 0f);
            }

            float bt = _style == Style.Victory ? ButtonsAt : PopEnd;
            float ba = Mathf.Clamp01((_t - bt + 120f) / 120f);
            _buttonsGroup.alpha = ba;
            bool active = _t >= bt;
            _buttonsGroup.interactable = active;
            _buttonsGroup.blocksRaycasts = active;
        }

        /// <summary>0'dan 1'e, ortasında hafif taşma (madalya, bant, onay rozeti).</summary>
        private static float Overshoot(float x, float amount = 0.14f)
        {
            if (x <= 0f) return 0f;
            if (x >= 1f) return 1f;
            return x < 0.65f ? Mathf.Lerp(0f, 1f + amount, Ease.OutCubic(x / 0.65f)) : Mathf.Lerp(1f + amount, 1f, (x - 0.65f) / 0.35f);
        }

        // ---- Konfeti: en çok 26 parça, belirlenimli tohum (yakalamalar tekrarlanabilir) ------------------

        private struct Piece
        {
            public RectTransform T;
            public Image I;
            public Vector2 P, V;
            public float Rot, Spin, Life, Age;
        }

        private void StartConfetti()
        {
            _confettiStarted = true;
            // v4 tek parçalı konfeti/şerit (renklenir) + temanın ortam parçası (renklenmez): sahil baloncuk,
            // bahçe yaprak, çöl parıltı. Hepsi kartın arkasındaki katmanda; metni örtmez.
            var confetti = UiFactory.Art("effects/confetti", "fx_confetti");
            var ribbon = UiFactory.Art("effects/motion-ribbon", "fx_ribbon");
            var ambient = PremiumArt.V4(_ambientFx ?? "effects/sparkle");
            var sprites = new[] { confetti, confetti, ribbon, ambient };
            var colors = new Color[]
            {
                new Color32(0xFF, 0x74, 0x73, 0xFF), new Color32(0x21, 0xBD, 0xD8, 0xFF), new Color32(0xFF, 0xD9, 0x4B, 0xFF),
                new Color32(0xB9, 0x85, 0xEE, 0xFF), new Color32(0x4C, 0xD6, 0x9A, 0xFF),
            };
            var rnd = new System.Random(7);
            var origin = (Vector2)_card.anchoredPosition + new Vector2(0f, _card.sizeDelta.y * 0.5f + 60f);
            for (int i = 0; i < ConfettiCount; i++)
            {
                int kind = i % sprites.Length;
                var sp = sprites[kind] ?? UiArt.Box;
                var tint = colors[rnd.Next(colors.Length)];
                var img = UiFactory.Image("Confetti", _fxLayer, sp, kind == 3 ? Color.white : tint);
                img.preserveAspect = true;
                float size = 30f + (float)rnd.NextDouble() * 22f;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size, size);
                // İki yana açılan yelpaze: parçalar kartın kenarlarından dışarı düşer.
                bool left = i % 2 == 0;
                float ang = Mathf.Deg2Rad * (left ? 105f + 55f * (float)rnd.NextDouble() : 20f + 55f * (float)rnd.NextDouble());
                float speed = 1100f + 800f * (float)rnd.NextDouble();
                _confetti.Add(new Piece
                {
                    T = rt, I = img, P = origin + new Vector2((left ? -1f : 1f) * (200f + (float)rnd.NextDouble() * 140f), 0f),
                    V = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed,
                    Rot = (float)rnd.NextDouble() * 360f, Spin = ((float)rnd.NextDouble() * 2f - 1f) * 420f,
                    Life = 1500f + 600f * (float)rnd.NextDouble(),
                });
            }
        }

        private void TickConfetti(float ms)
        {
            float dt = ms / 1000f;
            for (int i = _confetti.Count - 1; i >= 0; i--)
            {
                var c = _confetti[i];
                c.Age += ms;
                if (c.Age >= c.Life || c.T == null)
                {
                    if (c.T != null) UiFactory.Kill(c.T.gameObject);
                    _confetti.RemoveAt(i);
                    continue;
                }
                c.V.y -= 2400f * dt;
                c.V *= 1f - 0.9f * dt;
                c.P += c.V * dt;
                c.Rot += c.Spin * dt;
                c.T.anchoredPosition = c.P;
                c.T.localRotation = Quaternion.Euler(0f, 0f, c.Rot);
                var col = c.I.color;
                col.a = 1f - Mathf.Clamp01((c.Age / c.Life - 0.7f) / 0.3f);
                c.I.color = col;
                _confetti[i] = c;
            }
        }

        public int ConfettiAlive => _confetti.Count;
    }
}
