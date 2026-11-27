using System;
using KatKat.Core;
using KatKat.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// 2026-11-26 (kullanıcı: "Roket ve rulo yine tahtanın üst kısmında olsun böyle sorunlar var … Belli olmuyorlar … Roketlere
    /// tıklandığında anlaşılmıyor"): tepsi yuvasından tahtaya sürükle-bırak. Dolu yuvaya basıp sürükleyince parmağı büyütülmüş, ışıklı
    /// bir hayalet izler (<see cref="TrayDragBegan"/> / <see cref="TrayDragMoved"/> / <see cref="TrayDragEnded"/>; hedef önizlemesini
    /// oyun denetleyicisi tahtada çizer). Geçersiz bırakmada hayalet yuvasına yumuşakça döner. Seçili yuva yükselir ve salınır; doğan
    /// özel taş yuvaya varınca yuva büyüyüp ışıltı saçar (<see cref="PopTraySlot"/>). Ayarlar <see cref="BoosterFxConfig.Tray"/>.
    /// </summary>
    public sealed partial class HudView
    {
        public event Action<int, Vector2> TrayDragBegan;
        public event Action<Vector2> TrayDragMoved, TrayDragEnded;

        internal void RaiseTrayDragBegan(int slot, Vector2 screen) => TrayDragBegan?.Invoke(slot, screen);
        internal void RaiseTrayDragMoved(Vector2 screen) => TrayDragMoved?.Invoke(screen);
        internal void RaiseTrayDragEnded(Vector2 screen) => TrayDragEnded?.Invoke(screen);

        private RectTransform _ghost;
        private Image _ghostIcon, _ghostGlow, _ghostRing;
        private int _ghostSlot = -1;
        private float _ghostAge, _ghostSnap = -1f, _ghostBase;
        private bool _ghostOnTarget;
        private Vector3 _ghostSnapFrom;
        private readonly float[] _slotPop = { 9f, 9f, 9f, 9f };
        private readonly Image[] _slotPopRing = new Image[LevelState.TrayCapacity];

        /// <summary>Sürükleme hayaleti görünür mü (testler, yakalama).</summary>
        public bool TrayGhostShown => _ghost != null && _ghost.gameObject.activeSelf;
        /// <summary>Hayaletin dünya konumu (kullanım uçuşu buradan kalkar); görünmüyorsa null.</summary>
        public Vector3? TrayGhostWorld => TrayGhostShown && _ghostSnap < 0f ? _ghost.position : (Vector3?)null;
        /// <summary>Hayaletin dünya boyu (simgenin uzun kenarı).</summary>
        public float TrayGhostWorldSize => _ghostIcon != null ? WorldSpan(_ghostIcon.rectTransform) : 0f;

        /// <summary>Yuvadaki simgenin dünya boyu (doğan taş uçuşu bu boya küçülür; kullanım uçuşu bu boyla kalkar).</summary>
        public float TrayIconWorldSize(int slot)
        {
            slot = Mathf.Clamp(slot, 0, LevelState.TrayCapacity - 1);
            if (_trayIcons[slot] == null) return 0f;
            // Boş yuvada simge kapalı ve boyu eski: dolu yuvadaki roket simgesi boyu (yuvanın 1,38 katı).
            if (!_trayIcons[slot].gameObject.activeSelf) return WorldSpan((RectTransform)_traySlots[slot].transform) * 1.38f;
            return WorldSpan(_trayIcons[slot].rectTransform);
        }

        private static float WorldSpan(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Mathf.Max((c[2] - c[0]).magnitude / 1.4142f, 0f);
        }

        private Vector3 ScreenToCanvasWorld(Vector2 screen)
        {
            var root = (RectTransform)_canvas.transform;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(root, screen, _canvas.worldCamera, out var w);
            return w;
        }

        /// <summary>Sürükleme başladı: yuvadaki simge soluklaşır, yerine parmağı izleyen büyük ışıklı hayalet.</summary>
        public void ShowTrayGhost(SpecialKind kind, int slot, Vector2 screen)
        {
            if (_ghost == null) BuildGhost();
            slot = Mathf.Clamp(slot, 0, LevelState.TrayCapacity - 1);
            _ghostSlot = slot;
            _ghostAge = 0f;
            _ghostSnap = -1f;
            _ghostOnTarget = false;
            _ghost.SetAsLastSibling();
            _ghost.gameObject.SetActive(true);
            _ghostIcon.sprite = SpecialArt.For(kind);
            _ghostIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, SpecialArt.Angle(kind));
            TwinIcon.On(_ghostIcon).Set(kind); // 2026-11-27 çift roket hayaleti de ikiz
            // Boy: yuvadaki simgenin kanvas boyu × GhostScale (yuva ölçeği dahil).
            var icon = _trayIcons[slot];
            float span = Mathf.Max(icon.rectTransform.sizeDelta.x, icon.rectTransform.sizeDelta.y) * _bottomScale;
            _ghostBase = span * BoosterFxConfig.Current.Tray.GhostScale;
            _ghostIcon.rectTransform.sizeDelta = Vector2.one * _ghostBase;
            _ghostGlow.rectTransform.sizeDelta = Vector2.one * _ghostBase * 2.1f;
            _ghostRing.rectTransform.sizeDelta = Vector2.one * _ghostBase * 1.15f;
            var c = (Color)BoardView.SpecialTone(kind);
            _ghostGlow.color = new Color(c.r, c.g, c.b, BoosterFxConfig.Current.Tray.GhostGlow);
            _ghostRing.color = new Color(1f, 0.92f, 0.6f, 0f);
            _ghost.position = icon.rectTransform.position; // yuvadan kalkar, ilk karede parmağa geçer
            MoveTrayGhost(screen, false);
            _ghost.localScale = Vector3.one * 0.85f; // ilk karede de doğru boy (kalkışta hafif büyür)
            icon.color = new Color(1f, 1f, 1f, 0.28f);
        }

        /// <summary>Hayalet parmağı izler; geçerli hedef üstündeyken halka yanar.</summary>
        public void MoveTrayGhost(Vector2 screen, bool onTarget)
        {
            if (!TrayGhostShown || _ghostSnap >= 0f) return;
            var p = ScreenToCanvasWorld(screen);
            _ghost.position = new Vector3(p.x, p.y, _ghost.position.z);
            _ghostOnTarget = onTarget;
        }

        /// <summary>Geçersiz bırakma: hayalet yuvasına yumuşakça döner, sonra söner.</summary>
        public void SnapTrayGhost()
        {
            if (!TrayGhostShown) return;
            _ghostSnap = 0f;
            _ghostSnapFrom = _ghost.position;
        }

        /// <summary>Hayaleti hemen kaldırır (kullanım: simge tahtadaki uçuşa geçer).</summary>
        public void HideTrayGhost()
        {
            if (_ghost != null) _ghost.gameObject.SetActive(false);
            if (_ghostSlot >= 0 && _ghostSlot < _trayIcons.Length && _trayIcons[_ghostSlot] != null) _trayIcons[_ghostSlot].color = Color.white;
            _ghostSlot = -1;
            _ghostSnap = -1f;
        }

        private void BuildGhost()
        {
            _ghost = UiFactory.Rect("TrayGhost", _canvas.transform);
            _ghost.anchorMin = _ghost.anchorMax = new Vector2(0.5f, 0.5f);
            _ghost.sizeDelta = Vector2.one * 10f;
            _ghostGlow = UiFactory.Image("Glow", _ghost, SpecialArt.Glow, Color.white);
            _ghostGlow.raycastTarget = false;
            UiFactory.Place(_ghostGlow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 200f);
            _ghostRing = UiFactory.Image("Ring", _ghost, UiArt.Icon(UiIcon.Ring), Color.white);
            _ghostRing.raycastTarget = false;
            UiFactory.Place(_ghostRing.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 140f);
            _ghostIcon = UiFactory.Image("Icon", _ghost, null, Color.white);
            _ghostIcon.raycastTarget = false;
            _ghostIcon.preserveAspect = true;
            UiFactory.Place(_ghostIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 120f);
            _ghost.gameObject.SetActive(false);
        }

        /// <summary>Hayalet, yuva varış patlaması ve seçili yuvanın salınımı (Update ve yakalama için AdvanceEffects).</summary>
        private void TickTrayFx(float dt)
        {
            var tc = BoosterFxConfig.Current.Tray;
            if (TrayGhostShown)
            {
                _ghostAge += dt;
                if (_ghostSnap >= 0f)
                {
                    _ghostSnap += dt * 1000f / Mathf.Max(60f, tc.SnapBackMs);
                    float u = Mathf.Clamp01(_ghostSnap);
                    float e = 1f - (1f - u) * (1f - u) * (1f - u);
                    var to = _ghostSlot >= 0 ? _trayIcons[_ghostSlot].rectTransform.position : _ghostSnapFrom;
                    _ghost.position = Vector3.Lerp(_ghostSnapFrom, to, e);
                    _ghost.localScale = Vector3.one * Mathf.Lerp(1f, 1f / Mathf.Max(0.1f, tc.GhostScale), e);
                    _ghostGlow.color = new Color(_ghostGlow.color.r, _ghostGlow.color.g, _ghostGlow.color.b, tc.GhostGlow * (1f - u));
                    _ghostRing.color = new Color(1f, 0.92f, 0.6f, 0f);
                    if (u >= 1f)
                    {
                        int s = _ghostSlot;
                        HideTrayGhost();
                        if (s >= 0) _slotPop[s] = 0.55f; // küçük oturma sekmesi
                    }
                }
                else
                {
                    // Kalkış: 0,85 → 1 (EaseOutBack); sonra hafif nefes. Hedef üstündeyken halka dönerek yanar, hale parlar.
                    float x = Mathf.Clamp01(_ghostAge / 0.14f);
                    float pop = x < 1f ? Mathf.LerpUnclamped(0.85f, 1f, EaseOutBackUi(x)) : 1f + 0.04f * Mathf.Sin(_ghostAge * 7f);
                    _ghost.localScale = Vector3.one * pop * (_ghostOnTarget ? 1.06f : 1f);
                    _ghostIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _ghostIcon.rectTransform.localEulerAngles.z);
                    float gl = tc.GhostGlow * (_ghostOnTarget ? 1f : 0.7f) * (0.85f + 0.15f * Mathf.Sin(_ghostAge * 9f));
                    _ghostGlow.color = new Color(_ghostGlow.color.r, _ghostGlow.color.g, _ghostGlow.color.b, gl);
                    _ghostRing.color = new Color(1f, 0.92f, 0.6f, _ghostOnTarget ? 0.95f : 0.2f);
                    _ghostRing.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -_ghostAge * 160f);
                }
            }
            for (int i = 0; i < LevelState.TrayCapacity; i++)
            {
                var srt = (RectTransform)_traySlots[i].transform;
                bool sel = i == _traySelected;
                float popK = 1f;
                if (_slotPop[i] < 1f)
                {
                    _slotPop[i] += dt * 1000f / Mathf.Max(60f, tc.ArrivePopMs);
                    float u = Mathf.Clamp01(_slotPop[i]);
                    // Büyüyüp (ArrivePop) taşarak yerine oturur.
                    popK = u < 0.3f ? Mathf.Lerp(1f, tc.ArrivePop, EaseOutBackUi(u / 0.3f)) : 1f + (tc.ArrivePop - 1f) * Mathf.Cos((u - 0.3f) / 0.7f * Mathf.PI * 1.5f) * (1f - (u - 0.3f) / 0.7f);
                    var ring = _slotPopRing[i];
                    if (ring != null)
                    {
                        ring.gameObject.SetActive(u < 1f);
                        ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.4f, 1f - (1f - u) * (1f - u));
                        ring.color = new Color(1f, 0.9f, 0.55f, 0.95f * (1f - u));
                    }
                }
                srt.localScale = Vector3.one * _bottomScale * (sel ? TraySelScale : 1f) * popK;
                float y = TrayRaise + (sel ? tc.SelectLift + tc.SelectBob * Mathf.Sin(Time.unscaledTime * 5f) : 0f);
                srt.anchoredPosition = new Vector2(srt.anchoredPosition.x, y);
            }
        }

        /// <summary>Doğan özel taş yuvaya vardı: yuva büyüyüp oturur, altın halka genişler, ışıltılar saçılır.</summary>
        public void PopTraySlot(int slot)
        {
            if (slot < 0 || slot >= LevelState.TrayCapacity || _traySlots[slot] == null) return;
            _slotPop[slot] = 0f;
            if (_slotPopRing[slot] == null)
            {
                var r = UiFactory.Image("PopRing", _traySlots[slot].transform, UiArt.Icon(UiIcon.Ring), Color.clear);
                r.raycastTarget = false;
                UiFactory.Place(r.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * TraySlot * 1.2f);
                _slotPopRing[slot] = r;
            }
            if (Motion.Reduced) return;
            int n = BoosterFxConfig.Current.Tray.ArriveSparks;
            var world = _traySlots[slot].transform.position;
            for (int i = 0; i < n; i++)
            {
                var sp = UiFactory.Image("TraySpark", _trayRoot, SpecialArt.Glow, i % 3 == 0 ? Color.white : new Color(1f, 0.86f, 0.42f, 1f));
                sp.raycastTarget = false;
                sp.rectTransform.sizeDelta = Vector2.one * UnityEngine.Random.Range(20f, 40f);
                sp.transform.position = world;
                float a = i / (float)n * Mathf.PI * 2f + UnityEngine.Random.value * 0.4f;
                sp.gameObject.AddComponent<BucketSpark>().Init(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * UnityEngine.Random.Range(200f, 380f));
            }
        }

        private static float EaseOutBackUi(float x) { const float c1 = 1.70158f, c3 = c1 + 1f; x = Mathf.Clamp01(x) - 1f; return 1f + c3 * x * x * x + c1 * x * x; }
    }

    /// <summary>Tepsi yuvasının sürükleme girdisi (yuva düğmesinin üstünde; sürükleme başlarsa dokunuş tıklama sayılmaz).</summary>
    public sealed class TraySlotDrag : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public HudView Hud;
        public int Slot;

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = true;

        public void OnBeginDrag(PointerEventData e)
        {
            e.eligibleForClick = false;
            Hud?.RaiseTrayDragBegan(Slot, e.position);
        }

        public void OnDrag(PointerEventData e) => Hud?.RaiseTrayDragMoved(e.position);

        public void OnEndDrag(PointerEventData e) => Hud?.RaiseTrayDragEnded(e.position);
    }
}
