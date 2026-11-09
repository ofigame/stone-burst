using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// Öğretici turu katmanı (kullanıcı: "öğreticilerde odak yer açık, diğer taraflar biraz karartılmış olsun").
    /// Ekran karartılır, odak dikdörtgeni yuvarlak köşeli bir pencere olarak açık kalır; çevresinde nabız atan parlak
    /// halka, yanında krem konuşma kartı ve maskot yüzü. Dokunuşları engellemez (karar GameController'da).
    /// </summary>
    public sealed class TutorialOverlay : MonoBehaviour
    {
        private const float DimAlpha = 0.62f, Pad = 18f, Corner = 34f;
        /// <summary>ui3: öğretici karartması #111C24 / 0.76 (yalnız mevcut odak delikleri açık).</summary>
        private static Color Dim => Ui3.Ready ? Ui3.TutorialScrimColor : new Color(0.03f, 0.08f, 0.10f, DimAlpha);

        private RectTransform _root, _hole, _ring, _bubble, _hand;
        private Image _top, _bottom, _left, _right, _full;
        private Text _text, _continue;
        private CanvasGroup _group;
        private float _shownAt, _clock;
        private bool _hasHole;

        public bool Visible => gameObject.activeSelf;
        public RectTransform Root => _root;

        public static TutorialOverlay Create(RectTransform parent)
        {
            var rt = UiFactory.Stretch(UiFactory.Rect("Tutorial", parent));
            var o = rt.gameObject.AddComponent<TutorialOverlay>();
            o.Build(rt);
            rt.gameObject.SetActive(false);
            return o;
        }

        private void Build(RectTransform rt)
        {
            _root = rt;
            _group = rt.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _full = UiFactory.Image("Full", rt, null, Dim);
            UiFactory.Stretch(_full.rectTransform);
            _top = Panel("Top"); _bottom = Panel("Bottom"); _left = Panel("Left"); _right = Panel("Right");

            var holeImg = UiFactory.Image("Hole", rt, HoleSprite, Dim, true);
            _hole = holeImg.rectTransform;
            _hole.anchorMin = _hole.anchorMax = new Vector2(0.5f, 0.5f);
            holeImg.pixelsPerUnitMultiplier = 1f;
            var ringImg = UiFactory.Image("Ring", rt, RingSprite, new Color(1f, 0.93f, 0.62f, 0.95f), true);
            _ring = ringImg.rectTransform;
            _ring.anchorMin = _ring.anchorMax = new Vector2(0.5f, 0.5f);

            var cream = UiFactory.Art("ui/button-secondary-cream", "btn_cream");
            var bubble = UiFactory.Image("Bubble", rt, cream ?? UiArt.Box, cream != null ? Color.white : UiArt.Pill, true);
            _bubble = bubble.rectTransform;
            _bubble.anchorMin = _bubble.anchorMax = new Vector2(0.5f, 0.5f);
            _bubble.sizeDelta = new Vector2(900f, 210f);
            UiFactory.FitSliced(bubble, 140f);
            _text = UiFactory.Text("Text", _bubble, "", 40, UiArt.Brown, TextAnchor.MiddleCenter);
            UiFactory.Stretch(_text.rectTransform, 150f, 60f, 24f, 64f);
            _text.enableAutoSizing = true;
            _text.fontSizeMax = 40;
            _text.fontSizeMin = 24;
            _continue = UiFactory.Text("Continue", _bubble, "", 26, new Color(UiArt.Brown.r, UiArt.Brown.g, UiArt.Brown.b, 0.7f), TextAnchor.LowerCenter, FontStyle.Normal);
            UiFactory.Stretch(_continue.rectTransform, 150f, 60f, 0f, 34f);
            _continue.rectTransform.offsetMax = new Vector2(-60f, -150f);

            // Konuşan maskot: kartın solunda kristal maskot (gözler bakar).
            var face = UiFactory.Rect("Mascot", _bubble);
            UiFactory.Place(face, new Vector2(0f, 0.5f), new Vector2(78f, -6f), new Vector2(120f, 120f));
            MascotView.Create(face, new Vector2(0f, -54f), face, 128f);

            // Dokunma işareti: aşağı bakan ok, odak penceresinin üstünde zıplar.
            var hand = UiFactory.Image("Pointer", rt, UiArt.Icon(UiIcon.Back), UiArt.Cream);
            _hand = hand.rectTransform;
            _hand.anchorMin = _hand.anchorMax = new Vector2(0.5f, 0.5f);
            _hand.sizeDelta = new Vector2(84f, 84f);
            _hand.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var sh = hand.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.45f);
            sh.effectDistance = new Vector2(3f, -4f);
        }

        private Image Panel(string name)
        {
            var img = UiFactory.Image(name, _root, null, Dim);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        /// <summary>
        /// Katmanı gösterir. <paramref name="hole"/> kök (tuval) yerel koordinatında odak dikdörtgeni; null ise bütün ekran
        /// karartılır. <paramref name="pointer"/> odak üstünde dokunma oku. <paramref name="continueText"/> "devam için dokun".
        /// </summary>
        public void Show(Rect? hole, string text, bool pointer, string continueText) => Show(hole, text, pointer, continueText, null);

        private readonly System.Collections.Generic.List<Image> _cellDims = new System.Collections.Generic.List<Image>();

        /// <summary><paramref name="dimCells"/>: odak kutusu içinde kalan ama odakta olmayan hücreler (ayrıca karartılır).</summary>
        public void Show(Rect? hole, string text, bool pointer, string continueText, System.Collections.Generic.IList<Rect> dimCells)
        {
            for (int i = 0; i < _cellDims.Count; i++) _cellDims[i].enabled = false;
            if (dimCells != null)
                for (int i = 0; i < dimCells.Count; i++)
                {
                    if (i >= _cellDims.Count)
                    {
                        var img = UiFactory.Image("CellDim", _root, UiArt.Box, Dim, true);
                        img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                        img.pixelsPerUnitMultiplier = UiArt.Box.border.x / 18f;
                        _cellDims.Add(img);
                    }
                    var d = _cellDims[i];
                    d.enabled = true;
                    d.transform.SetSiblingIndex(_ring.GetSiblingIndex());
                    Set(d.rectTransform, dimCells[i]);
                }
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _shownAt = Time.unscaledTime;
            _hasHole = hole.HasValue;
            _full.enabled = !_hasHole;
            _top.enabled = _bottom.enabled = _left.enabled = _right.enabled = _hasHole;
            _hole.gameObject.SetActive(_hasHole);
            _ring.gameObject.SetActive(_hasHole);
            _hand.gameObject.SetActive(_hasHole && pointer);
            var size = _root.rect.size;
            float halfW = size.x * 0.5f + 4f, halfH = size.y * 0.5f + 4f;
            if (_hasHole)
            {
                var r = hole.Value;
                r = Rect.MinMaxRect(r.xMin - Pad, r.yMin - Pad, r.xMax + Pad, r.yMax + Pad);
                Set(_hole, r);
                Set(_ring, Rect.MinMaxRect(r.xMin - 10f, r.yMin - 10f, r.xMax + 10f, r.yMax + 10f));
                Set(_top.rectTransform, Rect.MinMaxRect(-halfW, r.yMax, halfW, halfH));
                Set(_bottom.rectTransform, Rect.MinMaxRect(-halfW, -halfH, halfW, r.yMin));
                Set(_left.rectTransform, Rect.MinMaxRect(-halfW, r.yMin, r.xMin, r.yMax));
                Set(_right.rectTransform, Rect.MinMaxRect(r.xMax, r.yMin, halfW, r.yMax));
                _handBase = new Vector2(r.center.x, r.yMax + 64f);
                // Kart odak penceresinin karşı yarısında; ekran dışına taşmaz.
                float h = _bubble.sizeDelta.y;
                bool below = r.center.y > 0f;
                float y = below ? r.yMin - 60f - h * 0.5f : r.yMax + (pointer ? 150f : 60f) + h * 0.5f;
                y = Mathf.Clamp(y, -halfH + h * 0.5f + 40f, halfH - h * 0.5f - 40f);
                // Kart odakla çakışıyorsa (çok büyük odak) ekranın alt kenarına.
                if (y + h * 0.5f > r.yMin && y - h * 0.5f < r.yMax) y = below ? -halfH + h * 0.5f + 60f : halfH - h * 0.5f - 60f;
                _bubble.anchoredPosition = new Vector2(0f, y);
            }
            else _bubble.anchoredPosition = Vector2.zero;
            Loc.Set(_text, text);
            Loc.Set(_continue, continueText ?? "");
            _continue.gameObject.SetActive(!string.IsNullOrEmpty(continueText));
            Tick();
        }

        public void Hide() => gameObject.SetActive(false);

        private Vector2 _handBase;

        private static void Set(RectTransform rt, Rect r)
        {
            rt.anchoredPosition = r.center;
            rt.sizeDelta = new Vector2(Mathf.Max(0f, r.width), Mathf.Max(0f, r.height));
        }

        private void Update() => Tick();

        private void Tick()
        {
            _clock = Time.unscaledTime - _shownAt;
            _group.alpha = Application.isPlaying ? Mathf.Clamp01(_clock / 0.25f) : 1f; // yakalama aracında zaman akmaz
            float p = 0.5f + 0.5f * Mathf.Sin(_clock * 5f);
            var ringImg = _ring.GetComponent<Image>();
            ringImg.color = new Color(1f, 0.93f, 0.62f, 0.55f + 0.4f * p);
            float s = 1f + 0.03f * p;
            _ring.localScale = new Vector3(s, s, 1f);
            if (_hand.gameObject.activeSelf) _hand.anchoredPosition = _handBase + new Vector2(0f, 22f * Mathf.Abs(Mathf.Sin(_clock * 4f)));
            float b = 1f + 0.015f * Mathf.Sin(_clock * 2.4f);
            _bubble.localScale = new Vector3(b, b, 1f);
        }

        // ---- Kodla üretilen 9-dilim dokular ----------------------------------------------------------------

        private static Sprite _holeSprite, _ringSprite;

        /// <summary>Ters yuvarlak köşe: köşeler karanlık, iç tamamen saydam (çevre panelleriyle aynı renk).</summary>
        private static Sprite HoleSprite => _holeSprite ? _holeSprite : (_holeSprite = MakeRounded(false));

        /// <summary>Odak çevresinde yumuşak parlak halka.</summary>
        private static Sprite RingSprite => _ringSprite ? _ringSprite : (_ringSprite = MakeRounded(true));

        private static Sprite MakeRounded(bool ring)
        {
            const int n = 96;
            float rad = Corner + (ring ? 10f : 0f);
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = ring ? "tut_ring" : "tut_hole" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // Yuvarlak dikdörtgene işaretli uzaklık (içeride negatif).
                float cx = Mathf.Clamp(x + 0.5f, rad, n - rad), cy = Mathf.Clamp(y + 0.5f, rad, n - rad);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - rad;
                float a;
                if (ring)
                {
                    float edge = -6f; // halka şeridi kenarın 6 px içinde
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - edge) / 3.2f);
                    a = Mathf.Max(a, 0.35f * Mathf.Clamp01(1f - Mathf.Abs(d - edge) / 9f));
                }
                else a = Mathf.Clamp01(d + 0.5f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float border = rad + 2f;
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }
}
