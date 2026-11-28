using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KatKat.UI
{
    /// <summary>
    /// 2026-11-28 deneme (kullanıcı: "pop-upları bundan yap yazılarıda ona uygun renklendir"; "bundan" = ana sayfa üst zeminindeki mor
    /// görsel, Branding/anasayfa-ust-mor): seramik modalın pirinç kenarı kalır, içi aynı şeklin içeriden kesilmiş hâliyle mor görselle
    /// dolar. Doğrudan mor üstünde kalan koyu yazılar açık renge çekilir (büyük başlık altın, diğerleri açık lila); krem satır, düğme,
    /// şerit gibi kendi zemini olan öğelerin üstündeki yazılara dokunulmaz. <see cref="Enabled"/> false: eski krem modallar.
    /// </summary>
    public static class PurplePopup
    {
        public const bool Enabled = false; // 2026-11-28 kullanıcı: "moru kaldırıp eski hallerine çevirelim"
        /// <summary>Kenar kalınlığı (modal görselinin pikseli, 635 px genişlikte pirinç kenar + iç gölge).</summary>
        private const float RimPx = 26f;
        public static readonly Color TitleColor = new Color(1f, 0.89f, 0.6f), BodyColor = new Color(0.97f, 0.92f, 1f);

        /// <summary>Modal gövdesine (dilimli seramik görsel) mor iç katman ekler; içerik kökü <paramref name="contentRoot"/> yazı uyarlayıcı alır.</summary>
        public static void Apply(Image body, RectTransform contentRoot)
        {
            if (!Enabled || body == null || body.sprite == null) return;
            if (body.transform.Find("PurpleBody") != null) return;
            // 2026-11-28 kullanıcı: "Canlı Mor Degrade Doku.png … bunu kullan" → dikey, ortası parlak mor doku (Branding/popup-mor).
            var tex = KatKat.View.Res.Load<Texture2D>("Branding/popup-mor");
            if (tex == null) return;
            tex.wrapMode = TextureWrapMode.Clamp;
            var mask = UiFactory.Image("PurpleBody", body.transform, body.sprite, Color.white, true);
            mask.raycastTarget = false;
            mask.pixelsPerUnitMultiplier = body.pixelsPerUnitMultiplier;
            float inset = RimPx / Mathf.Max(0.05f, body.pixelsPerUnitMultiplier);
            UiFactory.Stretch(mask.rectTransform, inset, inset, inset, inset);
            mask.transform.SetSiblingIndex(0);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var fill = new GameObject("Fill", typeof(RectTransform)).AddComponent<RawImage>();
            fill.transform.SetParent(mask.transform, false);
            fill.texture = tex;
            fill.raycastTarget = false;
            // Dikey görsel pencereyi kaplar (ortası parlak).
            fill.uvRect = new Rect(0f, 0f, 1f, 1f);
            UiFactory.Stretch(fill.rectTransform);
            if (contentRoot != null && contentRoot.GetComponent<PopupTextAdapter>() == null)
            {
                var ad = contentRoot.gameObject.AddComponent<PopupTextAdapter>();
                ad.Body = mask.rectTransform;
                ad.Init();
            }
        }
    }

    /// <summary>Mor modalda doğrudan mor üstündeki koyu yazıları açık renge çeker (içerik sonradan değişse de: 0,2 sn'de bir).</summary>
    public sealed class PopupTextAdapter : MonoBehaviour
    {
        public RectTransform Body;
        private float _next;
        private static readonly Vector3[] Corners = new Vector3[4];

        /// <summary>Ekran çizilmeden önce (oyunda her kare, yakalama aracında her çekimde) en çok 0,2 sn'de bir uyarlar.</summary>
        public void Init() { Canvas.willRenderCanvases += OnWillRender; }

        private void OnWillRender()
        {
            if (this == null) { Canvas.willRenderCanvases -= OnWillRender; return; }
            if (!isActiveAndEnabled) return;
            float now = Time.realtimeSinceStartup;
            if (now < _next) return;
            _next = now + 0.2f;
            Adapt();
        }

        private void OnDestroy() => Canvas.willRenderCanvases -= OnWillRender;

        public void Adapt()
        {
            if (Body == null) return;
            var images = GetComponentsInChildren<Graphic>(false);
            foreach (var t in GetComponentsInChildren<TMP_Text>(false))
            {
                var c = t.color;
                float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                if (lum > 0.55f) continue;
                if (!OnPurple(t, images)) continue;
                var nc = t.fontSize >= 46f ? PurplePopup.TitleColor : PurplePopup.BodyColor;
                nc.a = c.a;
                t.color = nc;
            }
        }

        /// <summary>Yazının ortası mor gövdenin içinde ve altında kendi zemini olan (opak) bir görsel yok.</summary>
        private bool OnPurple(TMP_Text t, Graphic[] graphics)
        {
            var center = t.rectTransform.TransformPoint(t.rectTransform.rect.center);
            if (!Contains(Body, center)) return false;
            foreach (var g in graphics)
            {
                if (g == null || g is TMP_Text || !g.enabled || g.color.a < 0.35f) continue;
                if (g.rectTransform == Body || g.transform.IsChildOf(Body)) continue; // mor katmanın kendisi
                if (Body.IsChildOf(g.transform)) continue; // modal gövdesi (Body'nin ebeveyni)
                if (Covers(g.rectTransform, Body)) continue; // gövdeyi kaplayan arka katmanlar (seramik basamak, gölge)
                if (g is Image img && img.sprite == null && g.GetComponent<Mask>() == null) { if (g.color.a < 0.9f) continue; }
                if (t.transform.IsChildOf(g.transform) || Contains(g.rectTransform, center)) return false;
            }
            return true;
        }

        /// <summary>r, gövdenin (b) alanının en az %85'ini kaplıyor mu (arka katman).</summary>
        private static bool Covers(RectTransform r, RectTransform b)
        {
            r.GetWorldCorners(Corners);
            float rw = Corners[2].x - Corners[0].x, rh = Corners[2].y - Corners[0].y, rx = Corners[0].x, ry = Corners[0].y;
            b.GetWorldCorners(Corners);
            float bw = Corners[2].x - Corners[0].x, bh = Corners[2].y - Corners[0].y;
            float ix = Mathf.Max(0f, Mathf.Min(rx + rw, Corners[2].x) - Mathf.Max(rx, Corners[0].x));
            float iy = Mathf.Max(0f, Mathf.Min(ry + rh, Corners[2].y) - Mathf.Max(ry, Corners[0].y));
            return bw > 0f && bh > 0f && ix * iy >= 0.85f * bw * bh;
        }

        private static bool Contains(RectTransform r, Vector3 world)
        {
            r.GetWorldCorners(Corners);
            return world.x >= Corners[0].x && world.x <= Corners[2].x && world.y >= Corners[0].y && world.y <= Corners[2].y;
        }
    }
}
