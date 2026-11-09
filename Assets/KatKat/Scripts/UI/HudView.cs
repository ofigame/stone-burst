using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// Oyun ekranı arayüzü (Premium-v2 rehberi §4, yerleşim 17-layout-guide): üstte bölüm başlığı; altında geri,
    /// kalan hamle, duraklat; hedef çubuğu; altta rulo ve geri alma. Öğretici bandı, kısa bildirim ve modal kartlar.
    /// Bileşenler paket sprite'larıdır, bütün sayı ve yazılar gerçek metindir. Kural çalıştırmaz.
    /// Tuval kamerada çizilir (ScreenSpaceCamera) ki ekran yakalama aracı da görebilsin.
    /// </summary>
    public sealed partial class HudView : MonoBehaviour
    {
        public const float RefWidth = 1080f, RefHeight = 1920f;
        // v6 üst kısım (kullanıcı referansı 2026-11-05): maskot payı + üst kart (hamle, yıldız çubuğu, duraklat) + hedef kartı.
        private const float CardSide = 18f;
        private static readonly Color DeepTeal = new Color32(0x1A, 0x5C, 0x58, 0xFF);
        private static readonly Color PillTeal = new Color32(0x1F, 0x6E, 0x69, 0xFF);
        private MascotView _mascot;

        /// <summary>Hareketli maskot (heyecan/endişe tepkileri için).</summary>
        public MascotView Mascot => _mascot;

        private void BuildHeader()
        {
            // v10 oyun içi kabuk (docs/v10/TriKat-Oyun-Ici-Claude, referans 941 × 1672 → 1080, ×1,148):
            // sol üst duraklat, sağ üst geri al (alttaki ile aynı aksiyon; ekranda tek geri al), ortada maskotlu mor hamle
            // kurdelesi, altında yıldız kabuğu ve hedef kabuğu. Değerler mevcut veriden; metinler Loc.
            var head = UiFactory.Rect("Header", _safe);
            head.anchorMin = head.anchorMax = new Vector2(0.5f, 1f);
            head.pivot = new Vector2(0.5f, 1f);
            head.sizeDelta = new Vector2(RefWidth, HeadH);
            head.anchoredPosition = Vector2.zero;
            _head = head;

            var pause = MenuView.ShellRoundButton("Pause", head, UiArt.Icon(UiIcon.Pause), 122f, () => PausePressed?.Invoke());
            UiFactory.Place((RectTransform)pause.transform, new Vector2(0f, 1f), new Vector2(109f, -118f), new Vector2(122f, 122f));
            _undo = MenuView.ShellRoundButton("Undo", head, UiFactory.Art("icons/undo", "icon_undo") ?? UiArt.Icon(UiIcon.Undo), 122f, OnUndoClicked);
            UiFactory.Place((RectTransform)_undo.transform, new Vector2(1f, 1f), new Vector2(-107f, -118f), new Vector2(122f, 122f));
            var undoBody = _undo.transform.Find("Body");
            _undoBg = undoBody != null ? undoBody.GetComponent<Image>() : _undo.GetComponent<Image>();
            _undoIcon = _undo.transform.Find("Icon").GetComponent<Image>();

            // Hamle grubu: yapraklar + iki yıldız (arkada), maskot (kurdelenin arkasından bakar, elleri önde), kurdele.
            var ribbonPos = new Vector2(0f, -154f);
            var ribbonSize = new Vector2(377f, 106f);
            for (int side = -1; side <= 1; side += 2)
            {
                var leaf = UiFactory.Image(side < 0 ? "LeafL" : "LeafR", head, GameShellLeaf(), Color.white);
                leaf.raycastTarget = false;
                leaf.preserveAspect = true;
                UiFactory.Place(leaf.rectTransform, new Vector2(0.5f, 1f), new Vector2(side * 222f, -140f), new Vector2(150f, 110f));
                if (side > 0) leaf.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                var st = UiFactory.Image(side < 0 ? "StarL" : "StarR", head, PremiumArt.V4("icons/star") ?? ProceduralArt.StarSprite, Color.white);
                st.raycastTarget = false;
                st.preserveAspect = true;
                UiFactory.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2(side * 220f, -150f), new Vector2(64f, 64f));
            }
            var mascotLayer = UiFactory.Rect("MascotLayer", head);
            UiFactory.Stretch(mascotLayer);
            var ribbon = UiFactory.Image("MovesPill", head, GameShellRibbon(), Color.white);
            ribbon.raycastTarget = false;
            UiFactory.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), ribbonPos, ribbonSize);
            _movesPill = ribbon.rectTransform;
            var label = UiFactory.Text("MovesLabel", ribbon.transform, Loc.T("hud.moves"), 52, Color.white, TextAnchor.MiddleRight);
            label.enableAutoSizing = true; label.fontSizeMax = 52; label.fontSizeMin = 26; label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-56f, 0f), new Vector2(190f, 66f));
            _moves = UiFactory.Title("Moves", ribbon.transform, "0", 73, new Color32(0xFF, 0xCE, 0x3A, 0xFF), new Color32(0x3A, 0x18, 0x6A, 0xFF));
            _moves.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            _moves.raycastTarget = false;
            UiFactory.Place(_moves.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(124f, 2f), new Vector2(116f, 88f));
            _moves.fontSizeMax = 73;
            _moves.fontSizeMin = 40;
            _moves.enableAutoSizing = true;
            if (Ui1.Ready)
            {
                // ui1/12: duraklat ve geri al yazısız ui1 simgeleri (tek parça). Hamle kapsülü kullanıcı kararıyla
                // (2026-11-08: "Mor (konsept gibi)") paketteki krem hud-moves-pill'in mor türevi (tools/ui1_mor_kapsul.ps1:
                // altın çerçeve aynı, iç mor): beyaz "Hamle", altın sayı.
                SkinRound(pause, "icon-pause");
                SkinRound(_undo, "icon-undo");
                _undoBg = _undoIcon = _undo.GetComponent<Image>();
                var purple = Ui1.Art("hud-moves-pill-purple");
                if (purple != null)
                {
                    ribbon.sprite = purple;
                    ribbon.type = Image.Type.Sliced;
                    ribbon.pixelsPerUnitMultiplier = purple.rect.height / 124f;
                }
                UiFactory.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), ribbonPos, new Vector2(420f, 124f));
            }
            if (Ui3.Ready)
            {
                // ui3/12 (00 referans + moves rolü onTeal): duraklat (48,64) ve geri al (920,64) 112; hamle kapsülü koyu
                // mor (purple→purpleDark) pirinç kenarlı, merkez y 172; "Hamle" açık, sayı altın. Yapraklar/yıldızlar yanda.
                UiFactory.Place((RectTransform)pause.transform, new Vector2(0f, 1f), new Vector2(104f, -120f), new Vector2(112f, 112f));
                UiFactory.Place((RectTransform)_undo.transform, new Vector2(1f, 1f), new Vector2(-104f, -120f), new Vector2(112f, 112f));
                ribbonPos = new Vector2(0f, -172f);
                ribbon.sprite = Ui3.Rounded(52f, Ui3.Purple, Ui3.PurpleDark, 3f, Ui3.Brass, 3f, new Color(1f, 0.98f, 0.93f, 0.55f), 104);
                ribbon.type = Image.Type.Sliced;
                ribbon.pixelsPerUnitMultiplier = 1f;
                UiFactory.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), ribbonPos, new Vector2(380f, 104f));
                label.color = Ui3.OnTeal;
                label.fontSizeMax = 44;
                UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-44f, 0f), new Vector2(200f, 64f));
                _moves.fontSizeMax = 64;
                UiFactory.Place(_moves.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(118f, 2f), new Vector2(110f, 80f));
                foreach (var n in new[] { "LeafL", "LeafR", "StarL", "StarR" })
                {
                    var c = (RectTransform)head.Find(n);
                    float side = n.EndsWith("L") ? -1f : 1f;
                    bool leaf = n.StartsWith("Leaf");
                    c.anchoredPosition = new Vector2(side * (leaf ? 232f : 228f), leaf ? -160f : -170f);
                }
            }
            var handsLayer = UiFactory.Rect("HandsLayer", head);
            UiFactory.Stretch(handsLayer);
            // Maskot çapası: tuval merkezine göre (head ortası = üstten HeadH/2).
            _mascot = MascotView.Create(mascotLayer, new Vector2(ribbonPos.x, HeadH * 0.5f + ribbonPos.y + ribbonSize.y * 0.5f - 6f), handsLayer);

            // Yıldız kabuğu: krem/altın dış gövde, koyu mor oyuk, sarı dolgu, üç eşik yıldızı.
            var score = UiFactory.Rect("ScorePresentation", head);
            UiFactory.Place(score, new Vector2(0.5f, 1f), new Vector2(0f, -259f), new Vector2(910f, 96f));
            if (Ui3.Ready) UiFactory.Place(score, new Vector2(0.5f, 1f), new Vector2(0f, -298f), new Vector2(920f, 84f)); // ui3 (80,256,920,84)
            BuildScoreBar(score);

            // Hedef kabuğu: geniş krem kabuk; çipler (mevcut hedef simgeleri ve canlı sayılar) içte.
            var goals = MenuView.GoldCapsule("GoalsCard", head, new Color32(0xFF, 0xF4, 0xE0, 0xFF), new Vector2(0.5f, 1f), new Vector2(0f, Ui1.Ready ? -372f : -368f), new Vector2(GoalsCardW, Ui1.Ready ? 108f : 96f), 5f);
            if (Ui1.Ready)
            {
                // ui1/12: hedef alanı krem kapsül (ince altın çerçeve); çipler aynı canlı hedeflerden.
                var back = goals.transform.parent.GetComponent<Image>();
                back.sprite = Ui1.Art("chip-objective");
                back.type = Image.Type.Sliced;
                back.color = Color.white;
                back.pixelsPerUnitMultiplier = back.sprite.rect.height / 106f;
                goals.color = new Color(1f, 1f, 1f, 0f);
            }
            if (Ui3.Ready)
            {
                // ui3 hedef bandı (48,364,984,112): fildişi (ivoryTop→ivoryBottom), 3 px pirinç, açık iç dudak.
                var back = goals.transform.parent.GetComponent<Image>();
                back.sprite = Ui3.Rounded(56f, Ui3.IvoryTop, Ui3.IvoryBottom, 3f, Ui3.Brass, 3f, Ui3.RimHighlight, 112);
                back.type = Image.Type.Sliced;
                back.pixelsPerUnitMultiplier = 1f;
                back.color = Color.white;
                var bsh = back.GetComponent<Shadow>();
                if (bsh != null) bsh.effectColor = new Color(Ui3.Shadow.r, Ui3.Shadow.g, Ui3.Shadow.b, 0.18f);
                UiFactory.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -420f), new Vector2(984f, 112f));
            }
            _goalsBar = Ui1.Ready
                ? UiFactory.Stretch(UiFactory.Rect("Goals", goals.transform), GoalsPadU1, GoalsPadU1, 0f, 0f) // ui1: yuvarlak uçların içinde
                : UiFactory.Stretch(UiFactory.Rect("Goals", goals.transform), 12f, 12f, 0f, 0f);
            _goalsRow = UiFactory.Stretch(UiFactory.Rect("Row", _goalsBar));
            var layout = _goalsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        /// <summary>Üst grubun yüksekliği (güvenli alanın üstünden); hedef kabuğunun alt kenarı = tahta alanının üstü.</summary>
        private const float HeadH = 500f;
        /// <summary>Hedef bandının alt kenarı (güvenli üstten): ui3 476 (48,364,984,112), önceki 418.</summary>
        private static float GoalsBottom => Ui3.Ready ? 476f : 418f;
        /// <summary>ui1/17: günlükte üst başlık plakası için hamle/yıldız/hedef grubunun aşağı kayması (duraklat ve geri al yerinde).</summary>
        private const float DailyShift = 72f;
        private float _headShift;
        private RectTransform _head;

        /// <summary>Günlük düzeni: true → hamle kapsülü, yıldız şeridi ve hedefler <see cref="DailyShift"/> aşağıda; tahta alanı aynı
        /// kadar kısalır (BoardWorldRect). Tahta kurulmadan önce çağrılır.</summary>
        public void SetDailyLayout(bool on)
        {
            float shift = on ? DailyShift : 0f;
            if (_head == null || Mathf.Approximately(shift, _headShift)) return;
            float d = shift - _headShift;
            foreach (RectTransform c in _head)
            {
                if (c.name == "Pause" || c.name == "Undo") continue;
                c.anchoredPosition -= new Vector2(0f, d);
            }
            _headShift = shift;
        }
        private const float GoalsCardW = 1000f, GoalsPadU1 = 40f;

        private static Sprite _tmpRibbon, _tmpLeaf;

        /// <summary>Mor hamle kurdelesi (final: Resources/PremiumV6/game/moves-ribbon.png; yoksa GEÇİCİ çizim, dikiş detaylı).</summary>
        private static Sprite GameShellRibbon()
        {
            var final = KatKat.View.SpecialArt.V6("game/moves-ribbon");
            if (final != null) return final;
            if (_tmpRibbon) return _tmpRibbon;
            const int w = 377, h = 106;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "moves_ribbon_tmp", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[w * h];
            Color top = new Color32(0x8A, 0x4C, 0xD8, 0xFF), bot = new Color32(0x5A, 0x2A, 0x9E, 0xFF), edge = new Color32(0x3E, 0x1A, 0x74, 0xFF), stitch = new Color32(0xE6, 0xD0, 0xFF, 0xFF);
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = i + 0.5f, y = j + 0.5f;
                // Yuvarlatılmış dikdörtgen (r = 22) + uçlarda hafif çentik.
                float dx = Mathf.Max(0f, Mathf.Max(22f - x, x - (w - 22f))), dy = Mathf.Max(0f, Mathf.Max(22f - y, y - (h - 22f)));
                float d = 22f - Mathf.Sqrt(dx * dx + dy * dy);
                d = Mathf.Min(d, Mathf.Min(Mathf.Min(x, w - x), Mathf.Min(y, h - y)) + 22f);
                Color c = new Color(0, 0, 0, 0);
                if (d > -1f)
                {
                    float cov = Mathf.Clamp01(d + 0.5f);
                    c = d < 4f ? edge : Color.Lerp(bot, top, y / h);
                    // Dikiş: kenardan 10 px içeride kesikli çizgi.
                    if (d > 9f && d < 12f && ((int)((x + y) / 9f)) % 2 == 0) c = Color.Lerp(c, stitch, 0.8f);
                    c.a = cov;
                }
                px[j * w + i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            return _tmpRibbon = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Yeşil yaprak demeti (final: Resources/PremiumV6/game/leaves.png; yoksa GEÇİCİ çizim).</summary>
        private static Sprite GameShellLeaf()
        {
            var final = KatKat.View.SpecialArt.V6("game/leaves");
            if (final != null) return final;
            if (_tmpLeaf) return _tmpLeaf;
            const int w = 150, h = 110;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "leaves_tmp", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[w * h];
            Color dark = new Color32(0x2E, 0x8E, 0x3A, 0xFF), light = new Color32(0x7A, 0xD0, 0x5A, 0xFF);
            var leaves = new[] { new Vector4(60, 60, 50, 20), new Vector4(80, 78, 46, 18), new Vector4(70, 40, 44, 17), new Vector4(100, 62, 36, 14) };
            float[] ang = { 20f, 45f, -10f, 30f };
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                Color c = new Color(0, 0, 0, 0);
                for (int k = 0; k < leaves.Length; k++)
                {
                    var L = leaves[k];
                    float a = ang[k] * Mathf.Deg2Rad;
                    float qx = i - L.x, qy = j - L.y;
                    float u = qx * Mathf.Cos(a) + qy * Mathf.Sin(a), v = -qx * Mathf.Sin(a) + qy * Mathf.Cos(a);
                    float e = (u * u) / (L.z * L.z) + (v * v) / (L.w * L.w);
                    if (e < 1f) { c = Color.Lerp(light, dark, Mathf.Clamp01(e + (v < 0 ? 0.3f : 0f))); c.a = Mathf.Clamp01((1f - e) * 8f); }
                }
                px[j * w + i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            return _tmpLeaf = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void CardShadow(Image card)
        {
            var sh = card.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.25f, 0.15f, 0.06f, 0.3f);
            sh.effectDistance = new Vector2(0f, -6f);
        }
        // Kullanıcı: "alttaki geri alma ve skill alanı çok altta" → ekran altından 70 birim yukarı.
        // v10 alt kabuk: dalgalı krem gövde (görünür 222) ve içinde mor yuvalar; düğme sırası merkezi alttan 108.
        private const float ShellH = 222f, BottomH = 112f, BottomGap = 52f, TutorialH = 104f;

        public event Action BackPressed, PausePressed, UndoPressed, RestartPressed, RollPressed;

        private Canvas _canvas;
        private RectTransform _safe;
        private float _boardBottom = ShellH + 22f;
        private Text _moves, _toast, _tutorial;
        private RectTransform _goalsBar;
        private RectTransform _tutorialBox, _toastBox;
        private RectTransform _goalsRow;
        private Button _undo, _roll;
        private Image _rollBg, _rollIcon, _rollGlow;
        private readonly List<GoalChip> _chips = new List<GoalChip>();
        
        private ResultPanel _panel;
        private float _toastUntil;
        private bool _targeting;

        public Canvas Canvas => _canvas;
        public bool PanelOpen => _panel != null && _panel.gameObject.activeSelf;
        public ResultPanel Panel => _panel;

        public static HudView Create(Camera cam)
        {
            var go = new GameObject("Hud", typeof(RectTransform));
            go.layer = 5;
            var hud = go.AddComponent<HudView>();
            hud.Build(cam);
            return hud;
        }

        private void Build(Camera cam)
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = cam;
            _canvas.planeDistance = 5f;
            _canvas.sortingOrder = 500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            gameObject.AddComponent<GraphicRaycaster>();

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform.parent, false);
            }

            _safe = UiFactory.Stretch(UiFactory.Rect("SafeArea", transform));
            _safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildHeader();

            // Alt sıra: rulo (altın krem hap) ve geri al (turkuaz hap). Yeniden başlat duraklatma kartında.
            // Alt kabuk gövdesi (dekor; arkasındaki tahta hücreleri yoktur, tıklama yakalamaz).
            var shellHolder = UiFactory.Rect("BottomShell", _safe);
            shellHolder.anchorMin = shellHolder.anchorMax = new Vector2(0.5f, 0f);
            shellHolder.pivot = new Vector2(0.5f, 0f);
            shellHolder.sizeDelta = new Vector2(RefWidth, ShellH + 60f);
            shellHolder.anchoredPosition = new Vector2(0f, -60f);
            // Kullanıcı (2026-11-08): güçlerin arkasında krem/altın alt kabuk yok; doğrudan oyun zemininde.
            var bottom = UiFactory.Band(UiFactory.Rect("BottomBar", _safe), false, BottomGap, BottomH);
            BuildRoll(bottom);
            BuildStonePower(bottom);
            BuildPaint(bottom);
            BuildTray(bottom);
            BuildHint(bottom);

            // Öğretici bandı ve kısa bildirim: krem hap üzerinde gerçek metin.
            var tutSprite = UiFactory.Art("ui/button-secondary-cream", "btn_cream");
            var tut = UiFactory.Image("Tutorial", _safe, tutSprite ?? UiArt.Box, tutSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.92f), true);
            _tutorialBox = UiFactory.Band(tut.rectTransform, false, ShellH + 18f, TutorialH);
            _tutorialBox.offsetMin = new Vector2(40f, _tutorialBox.offsetMin.y);
            _tutorialBox.offsetMax = new Vector2(-40f, _tutorialBox.offsetMax.y);
            UiFactory.FitSliced(tut, TutorialH);
            _tutorial = UiFactory.Text("Text", tut.transform, "", 38, UiArt.Brown, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Stretch(_tutorial.rectTransform, 64, 64, 20, 20);
            // Uzun öğretici metin bandı aşmaz: sığmazsa küçülür (401'de üç satıra taşıyordu).
            _tutorial.fontSizeMax = 32;
            _tutorial.fontSizeMin = 22;
            _tutorial.enableAutoSizing = true;
            _tutorialBox.gameObject.SetActive(false);

            var toastSprite = UiFactory.Art("ui/hud-moves-pill", "hud_moves");
            var toastBg = UiFactory.Image("Toast", _safe, toastSprite ?? UiArt.Box, toastSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.92f), true);
            _toastBox = UiFactory.Band(toastBg.rectTransform, false, ShellH + 190f, 96f);
            _toastBox.offsetMin = new Vector2(60f, _toastBox.offsetMin.y);
            _toastBox.offsetMax = new Vector2(-60f, _toastBox.offsetMax.y);
            UiFactory.FitSliced(toastBg, 96f);
            _toast = UiFactory.Text("Text", toastBg.transform, "", 36, UiArt.Brown);
            UiFactory.Stretch(_toast.rectTransform, 50, 50, 0, 6);
            _toastBox.gameObject.SetActive(false);
        }

        private static readonly Vector2 BottomButton = new Vector2(122f, 122f);
        private Image _undoBg, _undoIcon;

        // v10 mor yuva: altın halka (düğme grafiği) + mor disk; dolu/boş/devre dışı tonları ayrı.
        // Kullanıcı (2026-11-08): güç alanında yuva/halka yok; taşlar serbest, her birinin arkasında ayrı bir bulut,
        // yerinde büyüyüp küçülür (nefes) ve içindeki renge göre ışıldar. "Disk" artık bulut: renk tonları beyaz/saydam.
        private static readonly Color SlotOn = Color.white, SlotOff = new Color(1f, 1f, 1f, 0.6f), SlotEmpty = new Color(1f, 1f, 1f, 0.38f);
        private const float BreathPeriod = 1.7f, BreathAmp = 0.07f;

        /// <summary>Nefes alan güç öğeleri: simge, renk ışıltısı, faz (her biri ayrı ritimde).</summary>
        private sealed class SkillFx
        {
            public RectTransform Icon;
            public Image Aura;
            public Color Glow;
            public bool Rainbow, Lit;
            public float Phase;
        }
        private readonly List<SkillFx> _skillFx = new List<SkillFx>();
        private readonly Dictionary<Button, SkillFx> _skillOf = new Dictionary<Button, SkillFx>();

        /// <summary>
        /// Güç öğesinin zemini: düğme grafiği görünmez (dokunma alanı kalır), arkada renk ışıltısı ve bulut.
        /// Dönüş: bulut (dolu/boş/devre dışı tonu buna verilir). Simge sonradan eklenir ve bulutun önündedir.
        /// </summary>
        private Image PurpleSlot(Button b)
        {
            var hit = (Image)b.targetGraphic;
            hit.sprite = UiArt.Circle;
            hit.type = Image.Type.Simple;
            hit.color = new Color(1f, 1f, 1f, 0f);
            var size = ((RectTransform)b.transform).sizeDelta.x > 1f ? ((RectTransform)b.transform).sizeDelta.x : BottomButton.x;
            var aura = UiFactory.Image("Aura", b.transform, SpecialArt.Glow, Color.clear);
            aura.raycastTarget = false;
            UiFactory.Place(aura.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), Vector2.one * size * 2.1f);
            // Kullanıcı (2026-11-08): bulut da kaldırıldı; ton ayarları için görünmez taban kalır.
            var cloud = UiFactory.Image("Base", b.transform, null, SlotOn);
            cloud.enabled = false;
            cloud.raycastTarget = false;
            UiFactory.Place(cloud.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(size * 1.36f, size * 1.36f * 0.62f));
            var fx = new SkillFx { Aura = aura, Phase = _skillFx.Count * 0.37f };
            _skillFx.Add(fx);
            _skillOf[b] = fx;
            return cloud;
        }

        /// <summary>Güç simgesini nefese ve ışıltıya bağlar (renk: simgenin baskın rengi; Rainbow üç rengi dolaşır).</summary>
        private void SetSkill(Button b, Image icon, Color glow, bool lit, bool rainbow = false)
        {
            if (!_skillOf.TryGetValue(b, out var fx)) return;
            fx.Icon = icon != null ? icon.rectTransform : null;
            fx.Glow = glow;
            fx.Lit = lit;
            fx.Rainbow = rainbow;
            if (!lit) fx.Aura.color = Color.clear;
            else if (!rainbow) fx.Aura.color = new Color(glow.r, glow.g, glow.b, 0.85f);
            else fx.Aura.color = new Color(RainbowGlow[0].r, RainbowGlow[0].g, RainbowGlow[0].b, 0.85f);
        }

        private static readonly Color[] RainbowGlow =
        {
            new Color32(0xFF, 0x5A, 0x6A, 0xFF), new Color32(0x2F, 0xD8, 0xC8, 0xFF), new Color32(0xB0, 0x6C, 0xF0, 0xFF),
        };
        private static readonly Color RocketGlow = new Color32(0x3F, 0xE6, 0xD6, 0xFF);
        private static readonly Color RollGlow = new Color32(0x3F, 0xE6, 0xD0, 0xFF); // v11 rulo: turkuaz gövde
        private static readonly Color DropGlow = new Color32(0x4F, 0xD8, 0xE0, 0xFF);

        /// <summary>Taşlar yerinde büyüyüp küçülür (her biri kendi fazında); ışıltı aynı ritimde nabız atar.</summary>
        private void TickSkills(float dt)
        {
            float t = Time.unscaledTime;
            foreach (var fx in _skillFx)
            {
                if (fx.Icon == null || !fx.Icon.gameObject.activeInHierarchy) continue;
                float w = Mathf.Sin((t / BreathPeriod + fx.Phase) * 2f * Mathf.PI);
                bool busy = _rollIcon != null && fx.Icon == _rollIcon.rectTransform && (_rollPop < 1f || _rollUse < 1f);
                if (!busy) fx.Icon.localScale = Vector3.one * (1f + BreathAmp * w);
                if (!fx.Lit) continue;
                var c = fx.Glow;
                if (fx.Rainbow)
                {
                    float h = Mathf.Repeat(t / 2.4f + fx.Phase, 1f) * RainbowGlow.Length;
                    int a = (int)h;
                    c = Color.Lerp(RainbowGlow[a % RainbowGlow.Length], RainbowGlow[(a + 1) % RainbowGlow.Length], h - a);
                }
                c.a = 0.78f + 0.2f * w;
                fx.Aura.color = c;
                fx.Aura.rectTransform.localScale = Vector3.one * (1f + 0.06f * w);
            }
        }

        private static Sprite _cloud;

        /// <summary>Yumuşak beyaz bulut (koddan, 256 × 160): üst üste daireler, alt kenarda açık mavi gölge.</summary>
        private static Sprite CloudPuff()
        {
            if (_cloud) return _cloud;
            const int W = 256, H = 160;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "skill_cloud", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[W * H];
            var puffs = new[]
            {
                new Vector3(-0.30f, -0.06f, 0.15f), new Vector3(-0.13f, 0.05f, 0.20f), new Vector3(0.08f, 0.09f, 0.22f),
                new Vector3(0.28f, -0.02f, 0.17f), new Vector3(-0.02f, -0.10f, 0.19f), new Vector3(0.20f, -0.12f, 0.15f),
                new Vector3(-0.22f, -0.13f, 0.14f),
            };
            const float aa = 1.6f / W;
            for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                float x = (i + 0.5f) / W - 0.5f, y = ((j + 0.5f) / H - 0.5f) * H / W;
                float d = 1f;
                foreach (var p in puffs) d = Mathf.Min(d, Mathf.Sqrt((x - p.x) * (x - p.x) + (y - p.y) * (y - p.y)) - p.z);
                float flat = -0.20f - y; // alt kenar düzleşir
                d = Mathf.Max(d, flat);
                float a = Mathf.Clamp01(0.5f - d / aa);
                if (a <= 0f) { px[j * W + i] = new Color32(0, 0, 0, 0); continue; }
                float shade = Mathf.Clamp01((0.06f - y) / 0.28f);
                var c = Color.Lerp(Color.white, new Color(0.80f, 0.88f, 1f), shade * 0.75f);
                float rim = Mathf.Clamp01(1f + d / 0.03f); // kenara yakın hafif koyulaşma
                c = Color.Lerp(c, new Color(0.74f, 0.83f, 0.98f), rim * 0.25f);
                c.a = a * 0.96f;
                px[j * W + i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            _cloud = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W);
            _cloud.name = "skill_cloud";
            return _cloud;
        }

        private void BuildRoll(RectTransform bottom)
        {
            _rollGlow = UiFactory.Image("Glow", bottom, SpecialArt.Glow, Color.clear);
            UiFactory.Place(_rollGlow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 170f);
            _roll = UiFactory.SpriteButton("RollSlot", bottom, UiArt.Circle, BottomButton, () => RollPressed?.Invoke());
            _rollBg = PurpleSlot(_roll);
            _rollIcon = UiFactory.Image("Icon", _roll.transform, SpecialArt.RollIcon, Color.white);
            _rollIcon.preserveAspect = true;
            UiFactory.Place(_rollIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 150f);
            UiFactory.Place((RectTransform)_roll.transform, new Vector2(0.5f, 0.5f), Vector2.zero, BottomButton);
        }

        // ---- v11 günlük taş gücü yuvası (kullanıcı, 2026-11-08) ------------------------------------------
        // "Günlük bulmacada kullanılan özel taşlar oyun ekranında gerçek görüntüsüyle skill alanında yer alacak":
        // seçili koleksiyon taşı alt güç alanında kendi PNG'siyle durur; dokununca günlük gücü kullanılır
        // (doğrulama ve harcama GameController.TryDailyPower → DailyService.TryUsePower). Kullanılınca soluk, rozet 0.

        public event Action StonePowerPressed;
        private Button _stone;
        private Image _stoneIcon, _stoneDisc, _stoneGlow;
        private Text _stoneBadge;
        private Text _stoneCaption;
        private bool _stoneReady;

        private void BuildStonePower(RectTransform bottom)
        {
            _stoneGlow = UiFactory.Image("StoneGlow", bottom, SpecialArt.Glow, Color.clear);
            _stoneGlow.raycastTarget = false;
            UiFactory.Place(_stoneGlow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 190f);
            _stone = UiFactory.SpriteButton("StonePower", bottom, UiArt.Circle, BottomButton, () => StonePowerPressed?.Invoke());
            _stoneDisc = PurpleSlot(_stone);
            _stoneIcon = UiFactory.Image("Icon", _stone.transform, null, Color.white);
            _stoneIcon.preserveAspect = true;
            _stoneIcon.raycastTarget = false;
            UiFactory.Place(_stoneIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(172f, 172f));
            var badge = UiFactory.Image("CountBadge", _stone.transform, UiArt.Circle, new Color32(0x2F, 0xB8, 0x48, 0xFF));
            badge.raycastTarget = false;
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 0f), new Vector2(-12f, 12f), new Vector2(48f, 48f));
            _stoneBadge = UiFactory.Title("Count", badge.transform, "1", 32, Color.white, new Color32(0x1E, 0x6E, 0x2C, 0xFF));
            _stoneBadge.raycastTarget = false;
            UiFactory.Stretch(_stoneBadge.rectTransform);
            // ui1/17: taşın solunda, doğrudan mor zeminde "Gelgit Taşı · 1 kullanım" (krem tepsi yok).
            _stoneCaption = UiFactory.Text("Caption", _stone.transform, "", 40, Color.white, TextAnchor.MiddleRight);
            _stoneCaption.raycastTarget = false;
            UiFactory.Place(_stoneCaption.rectTransform, new Vector2(0f, 0.5f), new Vector2(-270f, 4f), new Vector2(500f, 70f));
            _stoneCaption.enableAutoSizing = true; _stoneCaption.fontSizeMax = 40; _stoneCaption.fontSizeMin = 22;
            var csh = _stoneCaption.gameObject.AddComponent<Shadow>(); csh.effectColor = new Color(0.12f, 0.04f, 0.25f, 0.8f); csh.effectDistance = new Vector2(0f, -3f);
            _stoneCaption.gameObject.SetActive(false);
            UiFactory.Place((RectTransform)_stone.transform, new Vector2(0.5f, 0.5f), Vector2.zero, BottomButton);
            _stone.gameObject.SetActive(false);
        }

        /// <summary>Taş gücü yuvası görünür mü (testler).</summary>
        public bool StonePowerShown => _stone != null && _stone.gameObject.activeSelf;
        /// <summary>Yuvadaki taş görseli (testler).</summary>
        public Sprite StonePowerSprite => _stoneIcon != null ? _stoneIcon.sprite : null;
        public bool StonePowerReady => _stoneReady;

        /// <param name="sprite">Taşın gerçek görseli; null → yuva gizli (normal bölüm ya da seçili taş yok).</param>
        /// <param name="used">Bu denemede güç kullanıldı (soluk, rozet 0).</param>
        /// <param name="ready">Şu an uygulanabilir (dokunulabilir, hafif parıltı).</param>
        public void SetStonePower(Sprite sprite, bool used, bool ready, Color glow = default, string caption = null)
        {
            if (_stone == null) return;
            bool show = sprite != null;
            if (_stone.gameObject.activeSelf != show) { _stone.gameObject.SetActive(show); LayoutBottom(); }
            _stoneGlow.gameObject.SetActive(show);
            if (!show) { _stoneReady = false; return; }
            _stoneIcon.sprite = sprite;
            _stoneReady = ready && !used;
            _stoneIcon.color = used ? new Color(1f, 1f, 1f, 0.38f) : ready ? Color.white : PassiveIcon;
            _stoneDisc.color = used ? SlotOff : SlotOn;
            _stoneBadge.text = used ? "0" : "1";
            _stoneCaption.gameObject.SetActive(Ui1.Ready && !string.IsNullOrEmpty(caption));
            if (caption != null) _stoneCaption.text = Loc.F("daily.power.caption", caption, used ? 0 : 1);
            _stone.interactable = !used;
            _stoneGlow.color = Color.clear; // ışıltı artık taşın kendi renginde (SetSkill)
            SetSkill(_stone, _stoneIcon, glow.a > 0f ? glow : new Color(0.6f, 0.9f, 1f), !used);
        }

        // ---- M12 boya damlası düğmesi ve renk seçici ----------------------------------------------------

        public event Action PaintPressed;
        public event Action<LayerColor> PaintColorChosen;
        private Button _paint;
        private Text _paintCount;
        private Image _paintDisc;
        private RectTransform _palette, _bottomBar;

        private void BuildPaint(RectTransform bottom)
        {
            _bottomBar = bottom;
            _paint = UiFactory.SpriteButton("Paint", bottom, UiArt.Circle, BottomButton, () => PaintPressed?.Invoke());
            _paintDisc = PurpleSlot(_paint);
            var icon = UiFactory.Image("Icon", _paint.transform, View.SpecialArt.Drop, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(136f, 136f));
            SetSkill(_paint, icon, DropGlow, true);
            // Sayı rozeti (sağ alt).
            var badge = UiFactory.Image("CountBadge", _paint.transform, UiArt.Circle, new Color32(0x2F, 0xB8, 0x48, 0xFF));
            badge.raycastTarget = false;
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 0f), new Vector2(-12f, 12f), new Vector2(48f, 48f));
            _paintCount = UiFactory.Title("Count", badge.transform, "1", 32, Color.white, new Color32(0x1E, 0x6E, 0x2C, 0xFF));
            _paintCount.raycastTarget = false;
            UiFactory.Stretch(_paintCount.rectTransform);
            _paint.gameObject.SetActive(false);
        }

        /// <summary>
        /// v9 B (günlükte tepsi + rulo): v10'da yerleşim <see cref="LayoutBottom"/> ile otomatik (görünür yuvalar eşit
        /// aralıklı). İmza geriye uyum için korunur.
        /// </summary>
        public void PlaceRollBesideTray(bool beside) => LayoutBottom();

        /// <summary>Boya düğmesi: kaynak varsa görünür (sayıyla); hedefleme modunda vurgulu.</summary>
        public void SetPaint(int left, bool targeting)
        {
            if (_paint == null) return;
            bool show = left > 0 || targeting;
            _paint.gameObject.SetActive(show);
            _paintCount.text = left.ToString();
            _paint.transform.localScale = Vector3.one * (targeting ? 1.1f : 1f);
            _paintDisc.color = targeting ? new Color32(0xB0, 0x6C, 0xF0, 0xFF) : SlotOn;
            LayoutBottom();
        }

        /// <summary>
        /// Alt kabuktaki gerçek kontroller (rulo, tepsi yuvaları, boya) yalnız o bölümde varsa görünür ve eşit aralıkla
        /// ortalanır. Boş kabuk alanı tıklama yakalamaz.
        /// </summary>
        private void LayoutBottom()
        {
            if (_bottomBar == null || _trayRoot == null) return;
            var items = new List<RectTransform>();
            bool roll = _roll != null && _roll.gameObject.activeSelf;
            bool tray = _trayRoot.gameObject.activeSelf;
            bool paint = _paint != null && _paint.gameObject.activeSelf;
            if (roll) items.Add((RectTransform)_roll.transform);
            int trayFirst = items.Count;
            if (tray) for (int i = 0; i < LevelState.TrayCapacity; i++) items.Add((RectTransform)_traySlots[i].transform);
            if (paint) items.Add((RectTransform)_paint.transform);
            bool stone = _stone != null && _stone.gameObject.activeSelf;
            if (stone) items.Add((RectTransform)_stone.transform);
            if (_hintBtn != null && _hintBtn.gameObject.activeSelf) items.Add((RectTransform)_hintBtn.transform);
            if (items.Count == 0) return;
            float step = Mathf.Min(244f, 960f / items.Count);
            float x0 = -(items.Count - 1) * step * 0.5f;
            for (int k = 0; k < items.Count; k++)
            {
                float x = x0 + k * step;
                if (tray && k >= trayFirst && k < trayFirst + LevelState.TrayCapacity) continue; // tepsi kökü ayrıca
                var rt = items[k];
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, rt == (RectTransform)_roll?.transform ? 0f : rt.anchoredPosition.y);
                if (rt == (RectTransform)_roll?.transform) _rollGlow.rectTransform.anchoredPosition = new Vector2(x, 0f);
                if (_stone != null && rt == (RectTransform)_stone.transform) _stoneGlow.rectTransform.anchoredPosition = new Vector2(x, 0f);
            }
            if (tray)
            {
                // Tepsi kökü yuvaları kapsar (öğretici odağı ve nabız için); yuvalar kök içinde.
                float a = x0 + trayFirst * step, b = x0 + (trayFirst + LevelState.TrayCapacity - 1) * step;
                _trayRoot.anchorMin = _trayRoot.anchorMax = new Vector2(0.5f, 0.5f);
                _trayRoot.anchoredPosition = new Vector2((a + b) * 0.5f, 0f);
                _trayRoot.sizeDelta = new Vector2(b - a + TraySlot + 24f, TraySlot + 24f);
                for (int i = 0; i < LevelState.TrayCapacity; i++)
                {
                    var srt = (RectTransform)_traySlots[i].transform;
                    srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
                    srt.anchoredPosition = new Vector2(a + i * step - (a + b) * 0.5f, srt.anchoredPosition.y);
                }
            }
        }

        // ---- v6 özel taş tepsisi (v10: alt kabukta mor yuvalar) ---------------------------------------------

        public event Action<int> TraySlotPressed;
        private RectTransform _trayRoot;
        private readonly Button[] _traySlots = new Button[LevelState.TrayCapacity];
        private readonly Image[] _trayIcons = new Image[LevelState.TrayCapacity], _trayRings = new Image[LevelState.TrayCapacity], _trayDiscs = new Image[LevelState.TrayCapacity];
        private int _traySelected = -1;
        private float _trayPulse = 9f;
        private const float TraySlot = 122f;
        /// <summary>Tepsi yuvaları alt çubuktan bu kadar yukarıda (seçili yuva ayrıca <see cref="TraySelLift"/>).</summary>
        private const float TrayRaise = 26f, TraySelLift = 14f, TraySelScale = 1.24f;
        private readonly Image[] _traySel = new Image[LevelState.TrayCapacity];
        private readonly Image[][] _twinkles = new Image[LevelState.TrayCapacity][];
        private readonly bool[] _trayFull = new bool[LevelState.TrayCapacity];

        private void BuildTray(RectTransform bottom)
        {
            _trayRoot = UiFactory.Rect("Tray", bottom);
            _trayBg = _trayRoot;
            for (int i = 0; i < LevelState.TrayCapacity; i++)
            {
                int slot = i;
                var b = UiFactory.SpriteButton("Slot" + i, _trayRoot, UiArt.Circle, new Vector2(TraySlot, TraySlot), () => TraySlotPressed?.Invoke(slot));
                UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TraySlot, TraySlot));
                var disc = PurpleSlot(b);
                var ring = UiFactory.Image("Ring", b.transform, SpecialArt.Glow, new Color(1f, 0.85f, 0.35f, 0f));
                ring.raycastTarget = false;
                UiFactory.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TraySlot * 1.6f, TraySlot * 1.6f));
                ring.transform.SetAsFirstSibling();
                var icon = UiFactory.Image("Icon", b.transform, SpecialArt.For(SpecialKind.RocketRow), Color.white);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TraySlot * 0.74f, TraySlot * 0.74f));
                _traySlots[i] = b;
                _trayIcons[i] = icon;
                _trayRings[i] = ring;
                _trayDiscs[i] = disc;
                // Kullanıcı (2026-11-08): özel taşlar "daha görünür, ışıltılı; seçildiğinde seçim belli olsun".
                var sel = UiFactory.Image("SelRing", b.transform, UiArt.Icon(UiIcon.Ring), new Color(1f, 0.84f, 0.3f, 0f));
                sel.raycastTarget = false;
                UiFactory.Place(sel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TraySlot * 1.45f, TraySlot * 1.45f));
                _traySel[i] = sel;
                _twinkles[i] = new Image[3];
                for (int k = 0; k < 3; k++)
                {
                    var tw = UiFactory.Image("Twinkle" + k, b.transform, SpecialArt.Glow, new Color(1f, 1f, 0.9f, 0f));
                    tw.raycastTarget = false;
                    UiFactory.Place(tw.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
                    _twinkles[i][k] = tw;
                }
            }
            SetTray(Array.Empty<SpecialKind>(), -1);
        }

        /// <summary>Tepsi yuvasının simgesi (testler).</summary>
        public Image TrayIcon(int i) => _trayIcons[i];
        /// <summary>Rulo yuvasındaki simge (testler).</summary>
        public Sprite RollIconSprite => _rollIcon != null ? _rollIcon.sprite : null;

        /// <summary>Tepsi yuvaları: dolu yuvada roket (sütun roketi dik) ya da Altın Rulo; seçili yuva büyür ve parlar.</summary>
        public void SetTray(IList<SpecialKind> tray, int selected)
        {
            if (_trayRoot == null) return;
            _traySelected = selected;
            for (int i = 0; i < LevelState.TrayCapacity; i++)
            {
                bool full = i < tray.Count;
                var icon = _trayIcons[i];
                icon.gameObject.SetActive(full);
                _traySlots[i].interactable = full;
                _trayDiscs[i].color = full ? SlotOn : SlotEmpty;
                ((Image)_traySlots[i].targetGraphic).color = new Color(1f, 1f, 1f, 0f); // yuva yok: yalnız dokunma alanı
                SetSkill(_traySlots[i], full ? icon : null, full && tray[i] != SpecialKind.Rainbow ? RocketGlow : Color.white, full, full && tray[i] == SpecialKind.Rainbow);
                if (full)
                {
                    var kind = tray[i];
                    icon.sprite = SpecialArt.For(kind);
                    icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, SpecialArt.Angle(kind)); // v11: dikey simge kendi yönünde
                    // Kullanıcı (2026-11-08): biraz daha büyük (yuva çerçevesi kalktı).
                    float s = kind == SpecialKind.Rainbow ? (SpecialArt.HasV11 ? 1.22f : 1.0f) : (SpecialArt.HasV11 ? 1.38f : 1.1f);
                    icon.rectTransform.sizeDelta = new Vector2(TraySlot * s, TraySlot * s);
                }
                bool sel = i == selected;
                _trayFull[i] = full;
                _traySlots[i].transform.localScale = Vector3.one * (sel ? TraySelScale : 1f);
                var srt = (RectTransform)_traySlots[i].transform;
                srt.anchoredPosition = new Vector2(srt.anchoredPosition.x, TrayRaise + (sel ? TraySelLift : 0f));
                _trayRings[i].color = new Color(1f, 0.85f, 0.35f, sel ? 0.95f : (full ? 0.4f : 0f));
                _trayRings[i].rectTransform.sizeDelta = Vector2.one * TraySlot * (sel ? 2.0f : 1.6f);
                if (_traySel[i] != null) _traySel[i].color = new Color(1f, 0.84f, 0.3f, sel ? 1f : 0f);
                var ol = _traySlots[i].GetComponent<Outline>() ?? _traySlots[i].gameObject.AddComponent<Outline>();
                ol.effectColor = new Color32(0xFF, 0xE4, 0x7A, 0xFF);
                ol.effectDistance = new Vector2(4f, -4f);
                ol.enabled = sel;
            }
        }

        /// <summary>Yuvanın dünya konumu (doğan özel taş buraya uçar).</summary>
        public Vector3 TraySlotPosition(int slot)
        {
            slot = Mathf.Clamp(slot, 0, LevelState.TrayCapacity - 1);
            return _traySlots[slot].transform.position;
        }

        /// <summary>Tepsi yalnızca özel taşlı bölümlerde görünür (1–5 öğreticilerinde rulo yuvası solda).</summary>
        public void SetTrayVisible(bool on)
        {
            if (_trayRoot != null) _trayRoot.gameObject.SetActive(on);
            LayoutBottom();
        }

        /// <summary>Hamle bitince tepsiyi hatırlatan kısa nabız.</summary>
        public void PulseTray() => _trayPulse = 0f;

        private void TickTray(float dt)
        {
            if (_trayRoot == null) return;
            if (_trayPulse < 1.2f)
            {
                _trayPulse += dt;
                float k = 1f + 0.06f * Mathf.Sin(Mathf.Min(1f, _trayPulse / 1.2f) * Mathf.PI * 3f) * (1f - _trayPulse / 1.2f);
                _trayRoot.localScale = new Vector3(k, k, 1f);
            }
            float t = Time.unscaledTime;
            for (int i = 0; i < LevelState.TrayCapacity; i++)
            {
                bool full = _trayFull[i], sel = i == _traySelected;
                // Dolu yuvada sürekli altın hale nabzı; seçili yuvada daha güçlü.
                if (full)
                {
                    float p = Mathf.Sin(t * 2f * Mathf.PI / HaloPeriod + i * 1.3f);
                    _trayRings[i].color = new Color(1f, 0.85f, 0.35f, sel ? 0.75f + 0.2f * p : 0.38f + 0.14f * p);
                }
                // Seçim halkası döner ve nabız atar.
                if (_traySel[i] != null && sel)
                {
                    var rt = _traySel[i].rectTransform;
                    rt.localRotation = Quaternion.Euler(0f, 0f, -t * 90f);
                    rt.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 6f));
                }
                // Işıltı: taşın çevresinde dönen, yanıp sönen üç küçük yıldızcık.
                var tws = _twinkles[i];
                if (tws == null) continue;
                for (int k = 0; k < tws.Length; k++)
                {
                    var tw = tws[k];
                    if (!full) { tw.color = new Color(1f, 1f, 0.9f, 0f); continue; }
                    float ph = t * 0.55f + k / 3f + i * 0.21f;
                    float ang = ph * 2f * Mathf.PI;
                    float r = TraySlot * (sel ? 0.66f : 0.56f);
                    tw.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r * 0.8f);
                    float blink = Mathf.Clamp01(Mathf.Sin(t * 3.1f + k * 2.1f + i) * 0.5f + 0.5f);
                    tw.color = new Color(1f, 1f, 0.88f, 0.25f + 0.75f * blink);
                    tw.rectTransform.sizeDelta = Vector2.one * (22f + 22f * blink);
                }
            }
        }

        /// <summary>Seçilen yığının üstünde mevcut renk dışındaki renkler; dokununca boyar.</summary>
        public void ShowPalette(LayerColor current, IList<LayerColor> options)
        {
            HidePalette();
            _palette = UiFactory.Rect("Palette", _bottomBar);
            UiFactory.Place(_palette, new Vector2(0.5f, 1f), new Vector2(0f, 110f), new Vector2(560f, 130f));
            var bg = UiFactory.Image("Bg", _palette, UiArt.Box, new Color(1f, 0.97f, 0.9f, 0.96f), true);
            UiFactory.Stretch(bg.rectTransform);
            bg.pixelsPerUnitMultiplier = UiArt.Box.border.x / 60f;
            var o = bg.gameObject.AddComponent<Outline>();
            o.effectColor = UiArt.Gold;
            o.effectDistance = new Vector2(3f, -3f);
            for (int i = 0; i < options.Count; i++)
            {
                var col = options[i];
                var chip = UiFactory.SpriteButton("Color" + i, _palette, PremiumArt.Chip(col) ?? UiArt.Chip(col), new Vector2(104f, 104f), () => PaintColorChosen?.Invoke(col));
                UiFactory.Place((RectTransform)chip.transform, new Vector2(0.5f, 0.5f), new Vector2((i - (options.Count - 1) * 0.5f) * 130f, 0f), new Vector2(104f, 104f));
            }
        }

        public void HidePalette()
        {
            if (_palette != null) UiFactory.Kill(_palette.gameObject);
            _palette = null;
        }

        private void OnUndoClicked()
        {
            if (!_undo.interactable) return;
            _undoKick = 0f; // ok tepkisi: kısa geri dönüş + pop (v3 §7)
            UndoPressed?.Invoke();
        }

        // ---- Alt düğme durum animasyonları (unscaled; kural olayına bağlı değil) -------------------------

        public const float RollPopMs = 250f, RollUseMs = 220f, UndoKickMs = 170f, HaloPeriod = 1.8f;
        private float _rollPop = 1f, _rollUse = 1f, _undoKick = 1f;

        /// <summary>Rulo kullanıldı: ikon kısa rulo hareketiyle sağa kayıp söner, ardından boş duruma döner.</summary>
        public void PlayRollUsed() => _rollUse = 0f;

        private void TickBottomButtons(float dt)
        {
            if (_rollPop < 1f)
            {
                _rollPop = Mathf.Min(1f, _rollPop + dt * 1000f / RollPopMs);
                // 0.85 → 1.08 → 1.0
                float s = _rollPop < 0.6f ? Mathf.Lerp(0.85f, 1.08f, Ease.OutCubic(_rollPop / 0.6f)) : Mathf.Lerp(1.08f, 1f, (_rollPop - 0.6f) / 0.4f);
                _rollIcon.rectTransform.localScale = new Vector3(s, s, 1f);
            }
            if (_rollUse < 1f)
            {
                _rollUse = Mathf.Min(1f, _rollUse + dt * 1000f / RollUseMs);
                var rt = _rollIcon.rectTransform;
                // v11 rulo nesnesi dönmez: yuvarlanır gibi sağa kayar, hafif öne eğilir.
                rt.localRotation = Quaternion.Euler(0f, 0f, SpecialArt.HasRollIcon ? -8f * Mathf.Sin(_rollUse * Mathf.PI) : -200f * _rollUse);
                rt.anchoredPosition = new Vector2(46f * _rollUse, 8f);
                var c = _rollIcon.color;
                c.a = _rollUse < 1f ? 1f - _rollUse * 0.8f : c.a;
                _rollIcon.color = c;
                if (_rollUse >= 1f)
                {
                    rt.localRotation = Quaternion.identity;
                    rt.anchoredPosition = new Vector2(0f, 8f);
                    SetRoll(_rollFull, _targeting, _roll.interactable);
                }
            }
            if (_undoKick < 1f)
            {
                _undoKick = Mathf.Min(1f, _undoKick + dt * 1000f / UndoKickMs);
                float k = Mathf.Sin(_undoKick * Mathf.PI);
                _undoIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f * k);
                float s = 1f + 0.1f * k;
                _undoIcon.rectTransform.localScale = new Vector3(s, s, 1f);
            }
        }

        // ---- İçerik ------------------------------------------------------------------------------

        /// <param name="title">Başlık hapındaki yazı (bölge adı, ör. "SAHİL").</param>
        /// <param name="levelNumber">Altın dairedeki bölüm numarası (1 tabanlı).</param>
        public void SetLevel(string title, int moves, IList<GoalState> goals, bool rollEnabled, int levelNumber = 0)
        {
            SetMoves(moves);
            foreach (var c in _chips) UiFactory.Kill(c.Root.gameObject);
            _chips.Clear();
            // Çip genişliği: en çok 196; çok hedefte kart genişliğine sığacak kadar daralır.
            float avail = RefWidth - 2f * CardSide - 24f;
            float w = goals.Count == 0 ? 196f : Mathf.Min(196f, (avail - 8f * (goals.Count - 1)) / goals.Count);
            if (Ui1.Ready && goals.Count > 0)
            {
                // ui1/12–13: çipler kapsülün iç genişliğine sığar (5 hedefte taşmaz); az hedefte daha geniş, eşit dağılır.
                float inner = GoalsCardW - 2f * GoalsPadU1;
                w = Mathf.Min(goals.Count <= 2 ? 300f : 220f, (inner - 8f * (goals.Count - 1)) / goals.Count);
            }
            for (int i = 0; i < goals.Count; i++)
                _chips.Add(GoalChip.CreatePill(_goalsRow, goals[i], new Vector2(w, Ui1.Ready ? 94f : 82f)));
            _mascot?.SetWorried(false);
            _roll.gameObject.SetActive(rollEnabled);
            _rollGlow.gameObject.SetActive(rollEnabled);
            LayoutBottom();
            HideToast();
        }

        public void SetMoves(int moves)
        {
            _moves.text = moves.ToString();
            _mascot?.SetWorried(moves > 0 && moves <= 3);
        }

        public void SetGoalProgress(int index, int progress, bool pulse)
        {
            if (index < 0 || index >= _chips.Count) return;
            _chips[index].Set(progress, pulse);
        }

        public void SetGoalsImmediate(IList<GoalState> goals)
        {
            for (int i = 0; i < goals.Count && i < _chips.Count; i++) _chips[i].Set(goals[i].Progress, false);
        }

        /// <param name="restart">Yeniden başlat artık duraklatma kartında; imza geriye uyum için korunur.</param>
        // Pasif durum (v3 §7): bulanıklık veya düşük alfa yok; doygunluk/kontrast azalır, geometrik kenar net kalır.
        private static readonly Color PassiveBase = new Color(0.96f, 0.92f, 0.86f, 1f);
        private static readonly Color PassiveIcon = new Color(0.86f, 0.86f, 0.86f, 0.85f);

        public void SetButtons(bool undo, bool restart)
        {
            _undo.interactable = undo;
            // v10: üst turkuaz geri al; pasifte gri-turkuaz gövde, soluk simge (alfa yok, kenar net).
            _undoBg.color = undo ? (Color)new Color32(0x17, 0x8E, 0x88, 0xFF) : new Color(0.45f, 0.55f, 0.56f, 1f);
            _undoIcon.color = undo ? Color.white : new Color(0.82f, 0.84f, 0.84f, 1f);
        }

        private bool _rollFull;

        /// <param name="full">Yuvada rulo var mı.</param>
        /// <param name="targeting">Satır seçme modunda mı.</param>
        public void SetRoll(bool full, bool targeting, bool interactable)
        {
            _targeting = targeting;
            _rollFull = full;
            if (_rollUse >= 1f) _rollIcon.color = full ? Color.white : PassiveIcon;
            if (_rollBg != null) _rollBg.color = full ? SlotOn : SlotOff;
            SetSkill(_roll, _rollIcon, RollGlow, full);
            if (!targeting) _rollGlow.color = new Color(1f, 1f, 1f, 0f);
            _roll.interactable = interactable;
        }

        /// <summary>Rulo kazanıldı: ikon 0.85→1.08→1.0 (~250 ms), tek sefer.</summary>
        public void PulseRoll() => _rollPop = 0f;

        // ---- Öğretici turu odak dikdörtgenleri (HUD kökü yerel koordinatı) -----------------------------------

        private RectTransform _movesPill;
        private RectTransform _trayBg;

        public Rect? RectOf(RectTransform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return null;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var root = (RectTransform)transform;
            Vector2 a = root.InverseTransformPoint(corners[0]), b = root.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        public Rect? GoalsRect() => RectOf(_goalsBar != null ? _goalsBar : _goalsRow);
        public Rect? MovesRect() => RectOf(_movesPill);
        public Rect? TrayRect() => RectOf(_trayBg);
        public Rect? PaintRect() => _paint != null ? RectOf((RectTransform)_paint.transform) : null;
        public Rect? TraySlotRect(int slot) => RectOf((RectTransform)_traySlots[Mathf.Clamp(slot, 0, LevelState.TrayCapacity - 1)].transform);

        public void ShowTutorial(string text)
        {
            bool on = !string.IsNullOrEmpty(text);
            _tutorialBox.gameObject.SetActive(on);
            if (on) Loc.Set(_tutorial, text);
            _boardBottom = ShellH + 22f + (on ? TutorialH + 18f : 0f);
        }

        public void ShowToast(string text, float seconds = 1.6f)
        {
            Loc.Set(_toast, text);
            _toastBox.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + seconds;
        }

        public void HideToast() => _toastBox.gameObject.SetActive(false);

        /// <summary>Bu renk bir renk toplama görevi mi (kırılan taş parçaları görev alanına gider).</summary>
        public bool IsGoalColor(LayerColor color)
        {
            foreach (var c in _chips)
                if (c.Goal.Type == GoalType.CollectColor && c.Goal.Color == color) return true;
            return false;
        }

        /// <summary>Yıldız (puan) çubuğunda dolumun ucu: kırılan taş parçalarının uçtuğu nokta.</summary>
        public Vector3 ScoreTarget()
        {
            if (_scoreFill != null && _scoreFill.gameObject.activeInHierarchy && _scoreFill.rect.width > 6f)
                return _scoreFill.TransformPoint(new Vector3(_scoreFill.rect.width, 0f, 0f));
            if (_scoreTrack != null) return _scoreTrack.TransformPoint(new Vector3(-_scoreTrack.rect.width * 0.5f + 20f, 0f, 0f));
            return _goalsRow.position + new Vector3(0f, 0.5f, 0f);
        }

        /// <summary>Rengin hedef simgesinin dünya konumu; hedef yoksa üst orta.</summary>
        public Vector3 GoalTarget(LayerColor color)
        {
            foreach (var c in _chips)
                if (c.Goal.Type == GoalType.CollectColor && c.Goal.Color == color) return c.Icon.position;
            return _goalsRow.position + new Vector3(0f, 0.5f, 0f);
        }

        /// <summary>Zarf hedefinin dünya konumu; hedef yoksa üst orta.</summary>
        public Vector3 EnvelopeTarget()
        {
            foreach (var c in _chips)
                if (c.Goal.Type == GoalType.CollectEnvelopes) return c.Icon.position;
            return _goalsRow.position + new Vector3(0f, 0.5f, 0f);
        }

        /// <summary>Token hedefinin (ör. sünger) dünya konumu; hedef yoksa üst orta.</summary>
        public Vector3 TokenTarget(GoalType type)
        {
            foreach (var c in _chips)
                if (c.Goal.Type == type) return c.Icon.position;
            return _goalsRow.position + new Vector3(0f, 0.5f, 0f);
        }

        /// <summary>Yıldız hedefinin dünya konumu; hedef yoksa üst orta.</summary>
        public Vector3 StarTarget()
        {
            foreach (var c in _chips)
                if (c.Goal.Type == GoalType.CollectStars) return c.Icon.position;
            return _goalsRow.position + new Vector3(0f, 0.5f, 0f);
        }

        /// <summary>
        /// Üst arayüz ile alt çubuk arasındaki tahta alanı (dünya birimi). Tuvalin o an boyutlanmış olmasına
        /// bağlı değildir: kamera oranı, referans genişliği (genişliğe eşleme) ve güvenli alandan hesaplanır.
        /// </summary>
        /// <summary>Son tahta alanı hesabında 1 referans px'in dünya ölçüsü (ui3 tahta kenarı px cinsinden çizilir).</summary>
        public static float WorldPerRef { get; private set; } = 0.01f;

        public Rect BoardWorldRect(Camera cam)
        {
            float refH = RefWidth / (cam.aspect > 0f ? cam.aspect : 9f / 16f);
            var n = SafeAreaFitter.CurrentNormalized();
            float left = n.xMin * RefWidth + 16f, right = n.xMax * RefWidth - 16f;
            float bottom = n.yMin * refH + _boardBottom, top = n.yMax * refH - (GoalsBottom + _headShift + 12f);
            float k = cam.orthographicSize * 2f / refH;
            WorldPerRef = k;
            var c = cam.transform.position;
            return Rect.MinMaxRect((left - RefWidth * 0.5f) * k + c.x, (bottom - refH * 0.5f) * k + c.y,
                (right - RefWidth * 0.5f) * k + c.x, (top - refH * 0.5f) * k + c.y);
        }

        // ---- Kartlar ------------------------------------------------------------------------------

        public readonly struct PanelButton
        {
            public readonly string Label;
            public readonly UiIcon? Icon;
            public readonly bool Primary;
            public readonly Action Action;
            public PanelButton(string label, UiIcon? icon, bool primary, Action action) { Label = label; Icon = icon; Primary = primary; Action = action; }
        }

        /// <summary>Sade kart: duraklatma, kayıp, hata.</summary>
        /// <summary>Açık karta maskot ekler (kayıp kartı).</summary>
        public void PanelMascot() { if (_panel != null) _panel.AddMascot(); }
        /// <summary>Açık karta kazanılan yıldızlar (günlük galibiyeti); 0 → yok.</summary>
        public void PanelStars(int stars) { if (_panel != null && stars > 0) _panel.SetStars(stars); }
        /// <summary>ui1/15: kayıp kartında kalan hedef özeti.</summary>
        public void PanelGoals(IList<GoalState> goals) => _panel?.SetGoalSummary(goals);
        /// <summary>ui1/14: kartın sağ üst kapat düğmesi.</summary>
        public void PanelClose(System.Action act) => _panel?.AddClose(act);

        public void ShowPanel(string title, string subtitle, params PanelButton[] buttons)
        {
            HidePanel();
            CoverTutorial();
            _panel = ResultPanel.Create(transform, ResultPanel.Style.Plain, title, subtitle, null, null, _theme, buttons);
        }

        // Kart açıkken öğretici bandı gizlenir (kartın iki yanından yazı taşıyordu); kart kapanınca geri gelir.
        private bool _tutorialCovered;
        private void CoverTutorial()
        {
            if (_tutorialBox.gameObject.activeSelf) { _tutorialBox.gameObject.SetActive(false); _tutorialCovered = true; }
        }

        /// <summary>Zafer kartı: madalya, bant, gerçek hedef satırı, konfeti, sonraki bölüm kartı.</summary>
        public void ShowVictory(string title, IList<GoalState> goals, NextLevelPreviewView.Data? next, params PanelButton[] buttons) => ShowVictory(title, goals, next, 0, buttons);

        /// <param name="stars">0: yıldız satırı yok; 1–3 kazanılan yıldız.</param>
        public void ShowVictory(string title, IList<GoalState> goals, NextLevelPreviewView.Data? next, int stars, params PanelButton[] buttons)
        {
            HidePanel();
            CoverTutorial();
            _panel = ResultPanel.Create(transform, ResultPanel.Style.Victory, title, null, goals, next, _theme, buttons);
            _panel.SetStars(stars);
        }

        private ThemeDefinition _theme;
        private Image _veil;
        private float _veilT = 1f;

        /// <summary>Tema geçişi süresi (v3 §11: 250–400 ms). Yeni tema hazır olduktan sonra perde açılır.</summary>
        public const float ThemeFadeMs = 320f;

        /// <summary>
        /// Kartların karartma ve ortam efekti için oynanan bölümün teması. Tema değiştiyse (ör. 100→101) yeni dünya
        /// kurulduktan sonra temanın renginde bir perde 320 ms'de açılır; aynı temada perde yok.
        /// </summary>
        public void SetTheme(ThemeDefinition theme)
        {
            bool changed = _theme != null && theme != _theme;
            _theme = theme;
            if (!changed) return;
            if (_veil == null)
            {
                _veil = UiFactory.Image("ThemeVeil", transform, null, Color.clear);
                UiFactory.Stretch(_veil.rectTransform);
            }
            _veil.transform.SetAsLastSibling();
            _veil.color = new Color(theme.DimColor.r, theme.DimColor.g, theme.DimColor.b, 1f);
            _veilT = 0f;
        }

        private void TickVeil(float dt)
        {
            if (_veil == null || _veilT >= 1f) return;
            _veilT = Mathf.Min(1f, _veilT + dt * 1000f / ThemeFadeMs);
            var c = _veil.color;
            c.a = 1f - Ease.InOutSine(_veilT);
            _veil.color = c;
            _veil.raycastTarget = _veilT < 1f;
        }

        /// <summary>Tema perdesini elle ilerletir (oynatma dışı ekran yakalama).</summary>
        public void AdvanceVeil(float ms) => TickVeil(ms / 1000f);

        /// <summary>Açık kartın giriş akışını elle ilerletir (oynatma dışı ekran yakalama).</summary>
        public void AdvancePanel(float ms)
        {
            if (_panel != null) _panel.Advance(ms);
        }

        /// <summary>Hedef çiplerinin parlamasını elle ilerletir (oynatma dışı ekran yakalama).</summary>
        public void AdvanceChips(float ms)
        {
            foreach (var c in _chips) c.Tick(ms / 1000f);
        }

        /// <summary>Güvenli alanı hemen uygular (oynatma dışı araçlar için).</summary>
        public void ApplySafeArea() => _safe.GetComponent<SafeAreaFitter>().Apply(true);

        /// <summary>Açık sonuç kartına ZPE değişim rozeti.</summary>
        public void SetResultBadge(string text) => _panel?.SetBadge(text);

        public void HidePanel()
        {
            if (_panel != null) UiFactory.Kill(_panel.gameObject);
            _panel = null;
            if (_tutorialCovered) { _tutorialCovered = false; _tutorialBox.gameObject.SetActive(!string.IsNullOrEmpty(_tutorial.text)); }
        }

        private void Update()
        {
            if (_toastBox.gameObject.activeSelf && Time.unscaledTime > _toastUntil) HideToast();
            foreach (var c in _chips) c.Tick(Time.unscaledDeltaTime);
            TickBottomButtons(Time.unscaledDeltaTime);
            TickTray(Time.unscaledDeltaTime);
            TickSkills(Time.unscaledDeltaTime);
            TickVeil(Time.unscaledDeltaTime);
            TickScore(Time.unscaledDeltaTime);
            TickPraise(Time.unscaledDeltaTime);
            if (_targeting)
            {
                // Satır seçme modunda altın hale düşük genlikle ~1.8 s periyotla nabız atar (v3 §7).
                float a = 0.6f + 0.3f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / HaloPeriod);
                _rollGlow.color = new Color(1f, 1f, 1f, a);
            }
        }

        // ---- v5 puan çubuğu ve yıldızlar (başlık hapının içinde) -----------------------------------------

        private Text _scoreText;
        private RectTransform _scoreFill, _scoreTrack;
        private readonly Image[] _scoreStars = new Image[3];
        private readonly float[] _starPop = { 9f, 9f, 9f };
        private int[] _thresholds;
        private float _scoreShown, _scoreTarget;
        private int _starsLit;

        /// <summary>Başlık hapını puan çubuğuna çevirir: üstte puan, altta dolan çubuk ve üç yıldız işareti.</summary>
        private void BuildScoreBar(RectTransform pill)
        {
            // v6: krem çerçeve (hafif koyu kenar) içinde koyu turkuaz oluk; altın dolgu üstte parlak şeritli; yıldızlar
            // oluğun içinde (kazanılan altın, kalan krem). Puan sayısı gösterilmez (referans), takip sürer.
            var size = pill.sizeDelta;
            var frame = UiFactory.Image("Frame", pill, UiArt.Box, new Color32(0xFF, 0xF6, 0xE6, 0xFF), true);
            frame.pixelsPerUnitMultiplier = UiArt.Box.border.x / (size.y * 0.5f);
            UiFactory.Stretch(frame.rectTransform);
            var fo = frame.gameObject.AddComponent<Outline>();
            fo.effectColor = new Color32(0xF2, 0xBE, 0x4A, 0xFF);
            fo.effectDistance = new Vector2(4f, -4f);
            float th = size.y - 18f;
            var track = UiFactory.Image("Track", frame.transform, UiArt.Box, new Color32(0x3E, 0x22, 0x6E, 0xFF), true); // v10: koyu mor oyuk
            track.pixelsPerUnitMultiplier = UiArt.Box.border.x / (th * 0.5f);
            UiFactory.Place(track.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 18f, th));
            var inner = track.gameObject.AddComponent<Shadow>();
            inner.effectColor = new Color(0f, 0f, 0f, 0.25f);
            inner.effectDistance = new Vector2(0f, 3f);
            _scoreTrack = track.rectTransform;
            if (Ui1.Ready)
            {
                // ui1/12: yıldız şeridi lacivert iz + krem kenar (progress-track); dolgu ve yıldızlar aynı.
                frame.sprite = Ui1.Art("progress-track");
                frame.color = Color.white;
                frame.pixelsPerUnitMultiplier = frame.sprite.rect.height / size.y;
                fo.enabled = false;
                track.color = new Color(0f, 0f, 0f, 0f);
                inner.enabled = false;
            }
            if (Ui3.Ready)
            {
                // ui3 hudStars (920×84): purpleDark, yarıçap 42, 3 px pirinç; ilerleme alanı (12,12,896,60).
                frame.sprite = Ui3.Rounded(42f, Ui3.PurpleDark, Ui3.PurpleDark, 3f, Ui3.Brass);
                frame.type = Image.Type.Sliced;
                frame.pixelsPerUnitMultiplier = 1f;
                fo.enabled = false;
                track.color = new Color(0f, 0f, 0f, 0f);
                inner.enabled = false;
                th = 60f;
                UiFactory.Place(track.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(896f, 60f));
            }
            var fill = UiFactory.Image("Fill", track.transform, UiArt.Box, new Color32(0xFF, 0xC4, 0x2E, 0xFF), true);
            fill.pixelsPerUnitMultiplier = UiArt.Box.border.x / ((th - 8f) * 0.5f);
            _scoreFill = fill.rectTransform;
            _scoreFill.anchorMin = new Vector2(0f, 0f);
            _scoreFill.anchorMax = new Vector2(0f, 1f);
            _scoreFill.pivot = new Vector2(0f, 0.5f);
            _scoreFill.anchoredPosition = new Vector2(4f, 0f);
            _scoreFill.sizeDelta = new Vector2(0f, -8f);
            var gloss = UiFactory.Image("Gloss", fill.transform, UiArt.Box, new Color(1f, 0.97f, 0.75f, 0.55f), true);
            gloss.pixelsPerUnitMultiplier = UiArt.Box.border.x / 6f;
            gloss.rectTransform.anchorMin = new Vector2(0f, 1f);
            gloss.rectTransform.anchorMax = new Vector2(1f, 1f);
            gloss.rectTransform.pivot = new Vector2(0.5f, 1f);
            gloss.rectTransform.offsetMin = new Vector2(14f, -18f);
            gloss.rectTransform.offsetMax = new Vector2(-14f, -6f);
            for (int i = 0; i < 3; i++)
            {
                var st = UiFactory.Image("Star" + (i + 1), track.transform, MascotView.CreamStar, Color.white);
                st.preserveAspect = true;
                _scoreStars[i] = st;
            }
            _scoreText = UiFactory.Title("Score", track.transform, "0", 28, Color.white, UiArt.HudTealDark);
            _scoreText.gameObject.SetActive(false);
            PlaceStars();
        }

        private void PlaceStars()
        {
            if (_scoreTrack == null) return;
            float w = _scoreTrack.sizeDelta.x;
            for (int i = 0; i < 3; i++)
            {
                float f = StarFraction(i);
                // Son yıldız oluğun sağ ucunda içeride kalır.
                float x = Mathf.Min(f * w, w - 34f);
                UiFactory.Place(_scoreStars[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(58f, 58f));
            }
        }

        /// <summary>Yıldızların çubuktaki yeri: 1★ kazanmak (sabit %30), 2★ ve 3★ eşik oranları.</summary>
        private float StarFraction(int i)
        {
            if (_thresholds == null || _thresholds.Length < 2 || _thresholds[1] <= 0) return i == 0 ? 0.3f : i == 1 ? 0.65f : 0.97f;
            return i == 0 ? 0.3f : i == 1 ? Mathf.Clamp(_thresholds[0] / (float)_thresholds[1], 0.45f, 0.85f) : 0.97f;
        }

        /// <summary>Puanı gösterir; <paramref name="animate"/> ise sayı ve çubuk yumuşakça yükselir, geçilen yıldız patlar.</summary>
        public void SetScore(int score, int[] thresholds, bool animate)
        {
            if (_scoreText == null) return;
            if (!ReferenceEquals(thresholds, _thresholds)) { _thresholds = thresholds; PlaceStars(); }
            _scoreTarget = score;
            if (!animate)
            {
                _scoreShown = score;
                _starsLit = LitStars(score);
                for (int i = 0; i < 3; i++) _starPop[i] = 9f;
                ApplyScore();
            }
        }

        private int LitStars(float score)
        {
            if (_thresholds == null || _thresholds.Length < 2) return 0;
            float max = _thresholds[1];
            int n = 0;
            for (int i = 0; i < 3; i++) if (score >= StarFraction(i) * max - 0.5f) n = i + 1;
            return n;
        }

        private void TickScore(float dt)
        {
            if (_scoreText == null) return;
            if (Mathf.Abs(_scoreShown - _scoreTarget) > 0.5f)
            {
                _scoreShown = Mathf.MoveTowards(_scoreShown, _scoreTarget, Mathf.Max(60f, Mathf.Abs(_scoreTarget - _scoreShown) * 4f) * dt);
                int lit = LitStars(_scoreShown);
                for (int i = _starsLit; i < lit; i++) { _starPop[i] = 0f; _mascot?.Excite(2); }
                _starsLit = Mathf.Max(_starsLit, lit);
                ApplyScore();
            }
            for (int i = 0; i < 3; i++)
            {
                if (_starPop[i] > 1f) continue;
                _starPop[i] += dt / 0.45f;
                float x = Mathf.Clamp01(_starPop[i]);
                _scoreStars[i].rectTransform.localScale = Vector3.one * (1f + 0.8f * Mathf.Sin(x * Mathf.PI) * (1f - x * 0.4f));
                _scoreStars[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 30f * Mathf.Sin(x * Mathf.PI));
            }
        }

        private void ApplyScore()
        {
            _scoreText.text = Mathf.RoundToInt(_scoreShown).ToString("N0", new System.Globalization.CultureInfo("tr-TR"));
            float max = _thresholds != null && _thresholds.Length >= 2 && _thresholds[1] > 0 ? _thresholds[1] : 1000f;
            float f = Mathf.Clamp01(_scoreShown / max);
            float fw = (_scoreTrack.sizeDelta.x - 8f) * f;
            _scoreFill.sizeDelta = new Vector2(fw, -8f);
            _scoreFill.gameObject.SetActive(fw > 6f);
            var gold = PremiumArt.V4("icons/star") ?? PremiumArt.Get("icon_star") ?? ProceduralArt.StarSprite;
            for (int i = 0; i < 3; i++)
            {
                _scoreStars[i].sprite = i < _starsLit ? gold : MascotView.CreamStar;
                // ui3: kazanılmamış yıldız #D4C8AA.
                _scoreStars[i].color = i < _starsLit || !Ui3.Ready ? Color.white : Ui3.UnearnedStar;
            }
        }

        /// <summary>Puan, yıldız ve övgü animasyonlarını elle ilerletir (ekran yakalama).</summary>
        public void AdvanceEffects(float ms)
        {
            TickScore(ms / 1000f);
            TickPraise(ms / 1000f);
            _mascot?.Advance(ms);
        }

        /// <summary>Şu ana dek yanan yıldız sayısı (zafer kartı bunu gösterir).</summary>
        public int StarsLit => _starsLit;

        // ---- v5 övgü yazısı ---------------------------------------------------------------------------

        private Text _praise;
        private float _praiseT = 9f;

        /// <summary>Tahtanın ortasında büyüyerek beliren, sonra süzülüp sönen övgü (seviye 1–3 büyüklük ve renk).</summary>
        public void ShowPraise(string text, int level)
        {
            Color[] outline = { UiArt.HudTealDark, UiArt.CoralDark, new Color32(0x6A, 0x2C, 0x9A, 0xFF), new Color32(0xB0, 0x5A, 0x00, 0xFF) };
            if (_praise != null) UiFactory.Kill(_praise.gameObject);
            level = Mathf.Clamp(level, 0, 3);
            _mascot?.Excite(Mathf.Max(1, level));
            // Ekran genişliğini aşmaz: 860 birimlik alan, sığmazsa küçülür.
            _praise = UiFactory.Title("Praise", _safe, text, 80 + level * 8, UiArt.Cream, outline[level]);
            UiFactory.Place(_praise.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(860f, 160f));
            _praise.fontSizeMax = 80 + level * 8;
            _praise.fontSizeMin = 48;
            _praise.enableAutoSizing = true;
            _praise.raycastTarget = false;
            _praiseT = 0f;
            TickPraise(0f);
        }

        private void TickPraise(float dt)
        {
            if (_praise == null) return;
            _praiseT += dt;
            float t = _praiseT;
            // 0–0.28 s: 0.3 → 1.15 → 1 büyüme; 0.28–0.85 s bekler ve hafif yükselir; 0.85–1.15 s söner.
            float s = t < 0.18f ? Mathf.Lerp(0.3f, 1.15f, Ease.OutCubic(t / 0.18f)) : t < 0.28f ? Mathf.Lerp(1.15f, 1f, (t - 0.18f) / 0.1f) : 1f;
            _praise.rectTransform.localScale = Vector3.one * s;
            _praise.rectTransform.anchoredPosition = new Vector2(0f, 120f + Mathf.Max(0f, t - 0.28f) * 60f);
            _praise.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f * Mathf.Sin(Mathf.Min(1f, t / 0.28f) * Mathf.PI));
            float a = t < 0.85f ? 1f : 1f - (t - 0.85f) / 0.3f;
            _praise.alpha = Mathf.Clamp01(a);
            if (t > 1.15f) { UiFactory.Kill(_praise.gameObject); _praise = null; }
        }

        /// <summary>Geri al ok tepkisini komut çalıştırmadan başlatır (yalnızca ekran yakalama aracı).</summary>
        public void KickUndoForCapture() => _undoKick = 0f;

        /// <summary>Alt düğme animasyonlarını elle ilerletir (oynatma dışı ekran yakalama).</summary>
        public void AdvanceButtons(float ms) => TickBottomButtons(ms / 1000f);
    }
}
