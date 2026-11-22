using System;
using KatKat.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// 3D harita denemesi menü bağlantısı (kullanıcı 2026-11-22). Anahtar açıkken haritanın kaydırılan resmi yerine
    /// <see cref="Map3DWorld"/> görüntüsü gösterilir; üst kart, rozetler ve alt menü üstte kalır. Sağ sütundaki
    /// "3D Harita / 2D Harita" düğmesi geçiş yapar (cihazda saklanır). Bölge: sıradaki bölümün 5 öncesi ile 14 sonrası.
    /// </summary>
    public partial class MenuView
    {
        // 2026-11-22 kullanıcı: "harita direkt 3d açılsın" → varsayılan 3D (yeni anahtar: eski 2D seçimi taşınmaz).
        private const string Map3DPref = "katkat.map3d.v2";

        /// <summary>Yakalama/test: tercih yerine bunu kullan.</summary>
        public static bool? Map3DOverride;

        public static bool Map3DOn
        {
            get
            {
                if (Map3DOverride.HasValue) return Map3DOverride.Value;
                try { return PlayerPrefs.GetInt(Map3DPref, 1) == 1; } catch { return true; }
            }
            set
            {
                if (Map3DOverride.HasValue) { Map3DOverride = value; return; }
                try { PlayerPrefs.SetInt(Map3DPref, value ? 1 : 0); PlayerPrefs.Save(); } catch { }
            }
        }

        /// <summary>Testler: bölüm başlangıç kartı açık mı.</summary>
        public bool StartCardShown => _startCard != null;

        private Map3DWorld _map3d;
        private Action _reshowLevels;

        /// <summary>Testler/yakalama: 3D dünya (kapalıysa null).</summary>
        public Map3DWorld Map3D => _map3d != null && _map3d.gameObject.activeSelf ? _map3d : null;

        private void HideMap3D()
        {
            if (_map3d != null) _map3d.gameObject.SetActive(false);
        }

        /// <summary>Haritanın üstüne 3D görüntü yüzeyi (viewport'un hemen üstünde kardeş; kabuk sonra eklenir, üstte kalır).</summary>
        private void BuildMap3DSurface(RectTransform page, RectTransform viewport, int total, int current, Func<int, bool> isCompleted)
        {
            if (!Map3DOn) { HideMap3D(); return; }
            int region = ThemeDefinition.RegionOf(current);
            int regionStart = region * ThemeDefinition.RegionSize;
            int regionEnd = Mathf.Min(total, regionStart + ThemeDefinition.RegionSize);
            // 2026-11-22 (kullanıcı): önde en çok 20 açılmamış bölüm; ilerledikçe o kadar açılır, sonu sis bulutu.
            int first = Mathf.Max(regionStart, current - 6);
            int last = Mathf.Min(regionEnd - 1, current + 20);
            int count = Mathf.Max(1, last - first + 1);

            if (_map3d == null) _map3d = Map3DWorld.Create(transform.parent);
            _map3d.gameObject.SetActive(true);

            Canvas.ForceUpdateCanvases();
            var refSize = UiAspect.CanvasRef(UiAspect.Current);
            float w = viewport.rect.width > 1f ? viewport.rect.width : refSize.x;
            float h = viewport.rect.height > 1f ? viewport.rect.height : refSize.y;
            float k = Mathf.Min(1f, 1440f / Mathf.Max(w, h * 0.5625f));
            var size = new Vector2Int(Mathf.RoundToInt(w * k), Mathf.RoundToInt(h * k));

            int cur = current;
            bool unlockAll = UnlockAll;
            _map3d.Build(first, count, current, isCompleted, StarsOf,
                i => unlockAll || i <= cur || (isCompleted != null && isCompleted(i)), size,
                i => { _map3d.ResetAfterEnter(); ShowStartCard(i); });

            var img = UiFactory.Rect("Map3D", page).gameObject.AddComponent<RawImage>();
            img.texture = _map3d.Texture;
            img.raycastTarget = true;
            UiFactory.Stretch(img.rectTransform);
            img.rectTransform.SetSiblingIndex(viewport.GetSiblingIndex() + 1);
            var input = img.gameObject.AddComponent<Map3DInput>();
            input.World = _map3d;
        }

        /// <summary>
        /// 2D/3D seçici (kullanıcı 2026-11-22: "haritanın üst kısmında ortada olsun. Sadece 2D 3D yazsın"): bölge plakasının
        /// altında, ortada iki parçalı kapsül; seçili olan turkuaz, diğeri krem. Seçim cihazda saklanır.
        /// </summary>
        private void BuildMap3DToggle(RectTransform page)
        {
            bool on = Map3DOn;
            var holder = UiFactory.Rect("Map3DToggle", page);
            UiFactory.Place(holder, new Vector2(0.5f, 1f), new Vector2(0f, -U3MapToggleY), new Vector2(236f, 76f));
            for (int k = 0; k < 2; k++)
            {
                bool is3d = k == 1, active = is3d == on;
                var b = UiFactory.PillButton(is3d ? "Map3DOn" : "Map2DOn", holder, is3d ? "3D" : "2D", active, new Vector2(112f, 72f), () =>
                {
                    if (Map3DOn == is3d) return;
                    Map3DOn = is3d;
                    _reshowLevels?.Invoke();
                });
                UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(is3d ? 60f : -60f, 0f), new Vector2(112f, 72f));
            }
        }

        /// <summary>Seçicinin üstten merkezi: bölge plakasının (176–296) hemen altı.</summary>
        private const float U3MapToggleY = 344f;
    }

    /// <summary>3D harita yüzeyi: sürükleme kamerayı yol boyunca kaydırır, dokunma kapıyı seçer.</summary>
    public sealed class Map3DInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IBeginDragHandler
    {
        public Map3DWorld World;
        private bool _dragged;

        private RectTransform Rt => (RectTransform)transform;

        private float ScreenHeight(PointerEventData e)
        {
            var c = GetComponentInParent<Canvas>();
            float s = c != null ? c.rootCanvas.scaleFactor : 1f;
            return Mathf.Max(1f, Rt.rect.height * s);
        }

        public void OnPointerDown(PointerEventData e) => _dragged = false;
        public void OnBeginDrag(PointerEventData e) => _dragged = true;

        public void OnDrag(PointerEventData e)
        {
            _dragged = true;
            if (World != null) World.Drag(e.delta.y / ScreenHeight(e));
        }

        public void OnEndDrag(PointerEventData e) { if (World != null) World.Release(); }

        public void OnPointerClick(PointerEventData e)
        {
            if (_dragged || World == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rt, e.position, e.pressEventCamera, out var local)) return;
            var r = Rt.rect;
            var uv = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
            World.Tap(uv);
        }
    }
}
