using System;
using System.Collections.Generic;
using KatKat.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// v10 taş vitrini (kullanıcı, 2026-11-07): koleksiyonda kazanılmış karta dokununca ve taş kazanıldığında açılan
    /// tam ekran gösterim. 3D modeli olan taş (Resources/Models/Stones/&lt;id&gt;: Blender ile sadeleştirilmiş, 30 bin
    /// üçgen, 1024 doku) ayrı katmanda kendi kamerasıyla RenderTexture'a çizilir, yavaşça döner ve parmakla çevrilir.
    /// Taşın renginde ışıltı: iki renkli nokta ışık + hafif emisyon (3D), arkada nabız atan hale ve yörüngede
    /// parıltılar (arayüz). Modeli olmayan taş büyük PNG ile aynı ışıltıda gösterilir. Kilitli taş burada açılmaz.
    /// </summary>
    public sealed class StoneShowcase : MonoBehaviour
    {
        public const int PreviewLayer = 31;
        private static readonly Vector3 RigOrigin = new Vector3(0f, -500f, 0f);

        private Canvas _canvas;
        private RectTransform _root, _safe;
        private RawImage _view;
        private Image _png, _halo, _halo2;
        private Text _title, _name, _desc, _hint;
        private readonly List<Image> _sparkles = new List<Image>();
        private Camera _cam;
        private RenderTexture _rt;
        private Transform _pivot;
        private GameObject _model;
        private Light _key, _glowL, _glowR;
        private GameObject _rig;
        private Color _glow = Color.white;
        private float _t, _spin = 26f, _yaw, _dragVel;
        private bool _hasModel;
        private Action _onClose;

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        public bool ShowingModel => IsOpen && _hasModel;
        public string ShownId { get; private set; } = "";

        public static StoneShowcase Create(Camera worldCam, Transform parent)
        {
            var go = new GameObject("StoneShowcase", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5;
            var s = go.AddComponent<StoneShowcase>();
            s.Build(worldCam);
            return s;
        }

        private void Build(Camera worldCam)
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = worldCam;
            _canvas.planeDistance = 3f;
            _canvas.sortingOrder = 900;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(HudView.RefWidth, HudView.RefHeight);
            scaler.matchWidthOrHeight = 0f;
            gameObject.AddComponent<GraphicRaycaster>();

            _root = UiFactory.Stretch(UiFactory.Rect("Root", transform));
            var dim = UiFactory.SpriteButton("Dim", _root, UiArt.Box, new Vector2(10f, 10f), Close);
            dim.GetComponent<Image>().color = new Color(0.07f, 0.04f, 0.14f, 0.9f);
            UiFactory.Stretch((RectTransform)dim.transform, -200f, -200f, -400f, -400f);
            _safe = UiFactory.Stretch(UiFactory.Rect("Safe", _root));
            _safe.gameObject.AddComponent<SafeAreaFitter>();

            _title = UiFactory.Title("Title", _safe, "", 58, new Color32(0xFF, 0xEC, 0xC2, 0xFF), new Color32(0x3E, 0x1C, 0x5E, 0xFF));
            _title.raycastTarget = false;
            UiFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(940f, 110f));
            _title.enableAutoSizing = true; _title.fontSizeMax = 58; _title.fontSizeMin = 30;

            _halo2 = UiFactory.Image("Halo2", _safe, SpecialArt.Glow, Color.clear);
            _halo2.raycastTarget = false;
            UiFactory.Place(_halo2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(1180f, 1180f));
            _halo = UiFactory.Image("Halo", _safe, SpecialArt.Glow, Color.clear);
            _halo.raycastTarget = false;
            UiFactory.Place(_halo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(820f, 820f));

            _view = UiFactory.Rect("View", _safe).gameObject.AddComponent<RawImage>();
            UiFactory.Place(_view.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(860f, 860f));
            _view.gameObject.AddComponent<DragArea>().Owner = this;
            _png = UiFactory.Image("Png", _safe, null, Color.white);
            _png.preserveAspect = true;
            _png.raycastTarget = false;
            UiFactory.Place(_png.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(620f, 700f));

            var sparkle = PremiumArt.V4("effects/sparkle") ?? ProceduralArt.StarSprite;
            for (int i = 0; i < 12; i++)
            {
                var sp = UiFactory.Image("Sparkle" + i, _safe, sparkle, Color.clear);
                sp.raycastTarget = false;
                sp.preserveAspect = true;
                UiFactory.Place(sp.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f));
                _sparkles.Add(sp);
            }

            if (Ui1.Ready) { BuildUi1Chrome(); _root.gameObject.SetActive(false); return; }
            var pill = MenuView.GoldCapsule("NamePill", _safe, new Color32(0x6B, 0x3A, 0xB4, 0xFF), new Vector2(0.5f, 0f), new Vector2(0f, 470f), new Vector2(620f, 96f), 7f);
            _name = UiFactory.Title("Name", pill.transform, "", 50, new Color32(0xFF, 0xEC, 0xC2, 0xFF), new Color32(0x3E, 0x1C, 0x5E, 0xFF));
            _name.raycastTarget = false;
            UiFactory.Stretch(_name.rectTransform, 24, 24, 6, 8);
            _name.enableAutoSizing = true; _name.fontSizeMax = 50; _name.fontSizeMin = 24;
            _desc = UiFactory.Text("Desc", _safe, "", 36, Color.white, TextAnchor.UpperCenter, FontStyle.Normal);
            _desc.raycastTarget = false;
            UiFactory.Place(_desc.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 340f), new Vector2(880f, 120f));
            _desc.enableAutoSizing = true; _desc.fontSizeMax = 36; _desc.fontSizeMin = 20;
            _hint = UiFactory.Text("Hint", _safe, "", 26, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Normal);
            _hint.raycastTarget = false;
            UiFactory.Place(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 278f), new Vector2(880f, 40f));

            var close = MenuView.ShellRoundButton("Close", _safe, PremiumArt.V4("icons/back") ?? UiArt.Icon(UiIcon.Back), 112f, Close);
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-90f, -90f), new Vector2(112f, 112f));
            var ok = UiFactory.PillButton("Ok", _safe, Loc.T("btn.continue"), true, new Vector2(520f, 130f), Close);
            UiFactory.Place((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(520f, 130f));

            _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// ui1/04: koyu mor ödül vitrini — sol üstte geri, üstte krem başlık plakasında taş adı, ortada taş (3D model ya
        /// da mevcut PNG), altta krem kartta güç açıklaması (stones.json + yerelleştirme), tek ana eylem Devam.
        /// </summary>
        private void BuildUi1Chrome()
        {
            _title.color = new Color32(0xFF, 0xEC, 0xC2, 0xFF);
            UiFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -385f), new Vector2(820f, 56f));
            _title.fontSizeMax = 36; // "Yeni koleksiyon taşı" ad plakasının altında küçük satır
            var head = Ui1.Decor("NamePill", _safe, "header-ivory");
            head.preserveAspect = false;
            UiFactory.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -249f), new Vector2(810f, 200f));
            _name = Ui1.Label("Name", head.transform, "", 76, Ui1.Navy);
            UiFactory.Stretch(_name.rectTransform, 50, 50, 14, 24);
            _name.enableAutoSizing = true; _name.fontSizeMax = 76; _name.fontSizeMin = 30;
            var card = Ui1.Card("DescCard", _safe, new Vector2(0.5f, 0f), new Vector2(0f, 427f), new Vector2(907f, 250f));
            _desc = Ui1.Label("Desc", card.transform, "", 48, Ui1.Navy, true);
            UiFactory.Stretch(_desc.rectTransform, 50, 50, 26, 26);
            _desc.enableAutoSizing = true; _desc.fontSizeMax = 48; _desc.fontSizeMin = 24;
            if (Ui3.Ready)
            {
                // ui3/04: ad plakası (148,188,784,112) pageTitle 56; yeni taş notu (96,328,888,92) açık;
                // açıklama kartı (96,1300,888,176) bodyCentered 36; döndürme ipucu (96,1500,888,72) caption.
                head.type = Image.Type.Sliced;
                head.pixelsPerUnitMultiplier = head.sprite.rect.height / 112f;
                UiFactory.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -244f), new Vector2(784f, 112f));
                _name.fontSizeMax = 56; _name.color = Ui3.Ink;
                UiFactory.Stretch(_name.rectTransform, 32, 32, 20, 20);
                _title.color = Ui3.OnTeal;
                _title.fontSizeMax = 36;
                UiFactory.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -374f), new Vector2(888f, 92f));
                UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 532f), new Vector2(888f, 176f));
                card.pixelsPerUnitMultiplier = 1f;
                _desc.fontSizeMax = 36; _desc.color = Ui3.Ink;
                UiFactory.Stretch(_desc.rectTransform, 40, 40, 20, 20);
            }
            _hint = Ui1.Label("Hint", _safe, "", 26, new Color(1f, 1f, 1f, Ui3.Ready ? 0.9f : 0.65f), false);
            UiFactory.Place(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, Ui3.Ready ? 384f : 270f), new Vector2(880f, 44f));
            if (Ui3.Ready) Ui1.Round("Close", _safe, "icon-back", new Vector2(0f, 1f), new Vector2(104f, -104f), 112f, Close); // ui3/04 (48,48,112,112)
            else Ui1.Round("Close", _safe, "icon-back", new Vector2(0f, 1f), new Vector2(108f, -130f), 140f, Close);
            Ui1.Button("Ok", _safe, Ui1.Case(Loc.T("btn.continue"), true), true, new Vector2(0.5f, 0f), new Vector2(0f, Ui3.Ready ? 242f : 173f), Ui3.Ready ? new Vector2(748f, 116f) : new Vector2(670f, 160f), Close, null, Ui3.Ready ? 44 : 62); // ui3/04 devam (166,1620,748,116)
            // Arka plan: koyu mor vitrin (mevcut karartma rengi morlaştırıldı).
            var dim = _root.Find("Dim");
            // ui1/04: opak mor sahne (oyun kabuğunun mor zemini); arkadaki koleksiyon görünmez.
            var stage = dim != null ? (Ui3.Ready ? MenuView.U3SageBg() : GameShellArt.Background()) : null; // ui3/04: koyu adaçayı çizgili sahne
            if (stage != null) { var di = dim.GetComponent<Image>(); di.sprite = stage; di.type = Image.Type.Simple; di.preserveAspect = false; di.color = Color.white; }
            else
            if (dim != null) dim.GetComponent<Image>().color = new Color(0.17f, 0.08f, 0.32f, 0.94f);
        }

        /// <summary>Taşı gösterir. <paramref name="title"/>: "Yeni koleksiyon taşı" ya da boş (koleksiyondan önizleme).</summary>
        public void Show(string stoneId, string title, string name, string desc, Sprite png, Color glow, Action onClose = null)
        {
            ClearModel();
            ShownId = stoneId ?? "";
            _onClose = onClose;
            _glow = glow;
            Loc.Set(_title, title ?? "");
            _title.gameObject.SetActive(!string.IsNullOrEmpty(title));
            Loc.Set(_name, name ?? "");
            Loc.Set(_desc, desc ?? "");
            _t = 0f;
            _yaw = 200f;
            _dragVel = 0f;

            KatKat.View.ResourceUsage.Note("Models/Stones/" + stoneId + "/model");
            var prefab = Resources.Load<GameObject>("Models/Stones/" + stoneId + "/model");
            _hasModel = prefab != null;
            _view.gameObject.SetActive(_hasModel);
            _png.gameObject.SetActive(!_hasModel);
            _png.sprite = png;
            Loc.Set(_hint, _hasModel ? Loc.T("stone.preview.drag") : "");
            if (_hasModel) BuildRig(prefab);
            _root.gameObject.SetActive(true);
            _root.transform.SetAsLastSibling();
            _safe.GetComponent<SafeAreaFitter>().Apply(true);
            Tick(0f);
            RenderNow();
        }

        public void Close()
        {
            if (!IsOpen) return;
            _root.gameObject.SetActive(false);
            ClearModel();
            var cb = _onClose;
            _onClose = null;
            cb?.Invoke();
        }

        private void BuildRig(GameObject prefab)
        {
            if (_cam == null)
            {
                // 3D sahne tuvalin DIŞINDA (tuval ölçeği uygulanmasın); vitrin yok olunca birlikte silinir.
                var rig = new GameObject("StoneRig");
                // Tuvalin değil, vitrinin üst nesnesinin (oyun denetleyicisi, ölçek 1) altında: denetleyiciyle birlikte silinir.
                rig.transform.SetParent(transform.parent, false);
                rig.transform.position = RigOrigin;
                rig.transform.rotation = Quaternion.identity;
                rig.transform.localScale = Vector3.one;
                _rig = rig;
                _pivot = new GameObject("Pivot").transform;
                _pivot.SetParent(rig.transform, false);
                _cam = new GameObject("StoneCam").AddComponent<Camera>();
                _cam.transform.SetParent(rig.transform, false);
                _cam.transform.localPosition = new Vector3(0f, 0.62f, -2.55f);
                _cam.transform.LookAt(rig.transform.position + new Vector3(0f, 0.47f, 0f));
                _cam.fieldOfView = 30f;
                _cam.nearClipPlane = 0.1f;
                _cam.farClipPlane = 10f;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                _cam.cullingMask = 1 << PreviewLayer;
                _cam.enabled = false; // elle çizilir (Tick); oyun kamerasının sırasını etkilemez
                _key = NewLight("Key", LightType.Directional, rig.transform, Color.white, 0.85f);
                _key.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
                _glowL = NewLight("GlowL", LightType.Point, rig.transform, Color.white, 1.4f);
                _glowL.transform.localPosition = new Vector3(-0.9f, 0.7f, -0.5f);
                _glowR = NewLight("GlowR", LightType.Point, rig.transform, Color.white, 1.1f);
                _glowR.transform.localPosition = new Vector3(0.9f, 0.35f, 0.6f);
            }
            if (_rt == null)
            {
                _rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "StoneShowcaseRT" };
                _cam.targetTexture = _rt;
                _view.texture = _rt;
            }
            _glowL.color = _glow;
            _glowR.color = Color.Lerp(_glow, Color.white, 0.35f);
            _model = Instantiate(prefab, _pivot, false);
            _model.transform.localPosition = Vector3.zero;
            KatKat.View.ResourceUsage.Note("Models/Stones/" + ShownId + "/mat");
            var mat = Resources.Load<Material>("Models/Stones/" + ShownId + "/mat");
            foreach (var r in _model.GetComponentsInChildren<Renderer>())
            {
                if (mat != null)
                {
                    // Taş rengine göre hafif emisyon (ışıltı); paylaşılan malzeme değiştirilmez.
                    var m = new Material(mat);
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", _glow * 0.10f);
                    r.sharedMaterial = m;
                }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            foreach (var t in _model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = PreviewLayer;
        }

        private static Light NewLight(string name, LightType type, Transform parent, Color c, float intensity)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.type = type;
            l.color = c;
            l.intensity = intensity;
            l.range = 4f;
            l.cullingMask = 1 << PreviewLayer;
            l.shadows = LightShadows.None;
            return l;
        }

        private void ClearModel()
        {
            if (_model != null)
            {
                foreach (var r in _model.GetComponentsInChildren<Renderer>()) if (r.sharedMaterial != null) Kill(r.sharedMaterial);
                Kill(_model);
            }
            _model = null;
        }

        private void OnDestroy()
        {
            ClearModel();
            if (_rt != null) { _rt.Release(); Kill(_rt); }
            if (_rig != null) Kill(_rig);
        }

        /// <summary>Oyunda Destroy, editör yakalamasında (oynatma dışı) DestroyImmediate.</summary>
        private static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        private void Update()
        {
            if (IsOpen) Tick(Time.unscaledDeltaTime);
        }

        /// <summary>Animasyonu elle ilerletir (ekran yakalama).</summary>
        public void Advance(float ms) => Tick(ms / 1000f);

        private void Tick(float dt)
        {
            _t += dt;
            // Dönüş: kendiliğinden yavaş + sürüklemeden kalan ivme.
            _yaw += (_spin + _dragVel) * dt;
            _dragVel = Mathf.MoveTowards(_dragVel, 0f, 220f * dt);
            if (_pivot != null) _pivot.localRotation = Quaternion.Euler(0f, _yaw, 0f) * Quaternion.Euler(0f, 0f, 0f);
            if (_png.gameObject.activeSelf) _png.rectTransform.anchoredPosition = new Vector2(0f, 110f + 14f * Mathf.Sin(_t * 1.6f));

            // Işıltı: iki katmanlı nabız atan hale + yörüngede parlayan parıltılar (taş renginde).
            float pulse = 0.5f + 0.5f * Mathf.Sin(_t * 2.1f);
            _halo.color = new Color(_glow.r, _glow.g, _glow.b, 0.55f + 0.25f * pulse);
            _halo.rectTransform.localScale = Vector3.one * (0.94f + 0.08f * pulse);
            _halo2.color = new Color(_glow.r, _glow.g, _glow.b, 0.22f + 0.10f * (1f - pulse));
            for (int i = 0; i < _sparkles.Count; i++)
            {
                float a = _t * (0.35f + 0.04f * (i % 4)) + i * Mathf.PI * 2f / _sparkles.Count;
                float r = 330f + 70f * Mathf.Sin(i * 1.7f + _t * 0.6f);
                var sp = _sparkles[i];
                sp.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a) * r, 110f + Mathf.Sin(a) * r * 0.85f);
                float tw = Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(_t * 3.1f + i * 2.3f));
                var c = Color.Lerp(_glow, Color.white, 0.45f);
                sp.color = new Color(c.r, c.g, c.b, tw);
                sp.rectTransform.localScale = Vector3.one * (0.5f + 0.7f * tw);
                sp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _t * 40f + i * 30f);
            }
            if (_hasModel && _glowL != null)
            {
                _glowL.intensity = 1.1f + 0.5f * pulse;
                _glowR.intensity = 0.9f + 0.4f * (1f - pulse);
                RenderNow();
            }
        }

        /// <summary>3D vitrini RenderTexture'a çizer (oynatma dışı yakalamada da çalışır).</summary>
        public void RenderNow()
        {
            if (_hasModel && _cam != null && _model != null) _cam.Render();
        }

        private void Drag(float dx) => _dragVel = Mathf.Clamp(dx * 18f, -600f, 600f) - _spin * 0.0f;

        private sealed class DragArea : MonoBehaviour, IDragHandler, IBeginDragHandler
        {
            public StoneShowcase Owner;
            public void OnBeginDrag(PointerEventData e) { }
            public void OnDrag(PointerEventData e)
            {
                if (Owner == null) return;
                Owner._yaw += e.delta.x * 0.45f;
                Owner.Drag(e.delta.x);
            }
        }
    }
}
