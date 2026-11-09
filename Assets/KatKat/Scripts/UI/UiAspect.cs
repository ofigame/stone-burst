using UnityEngine;

namespace KatKat.UI
{
    /// <summary>
    /// Ekran oranı kuralları (kullanıcı, 2026-11-09: iPad desteği kalsın). Arayüz 1080×1920 telefon tasarımıdır.
    /// Telefon oranında ve daha uzun ekranda (en/boy ≤ 9/16) genişliğe eşlenir; değişiklik yok.
    /// Daha geniş ekranda (iPad, tablet) yüksekliğe eşlenir: tuval 1920 birim yüksek, (1920 × oran) geniş olur;
    /// güvenli alan ortada 1080 birimlik sütuna daraltılır, kenarları zemin doldurur. Böylece kartlar taşmaz,
    /// harita ve tahta telefondaki gibi yerleşir.
    /// </summary>
    public static class UiAspect
    {
        public const float Phone = 9f / 16f;

        /// <summary>Ekran yakalama: çekilen görüntünün oranı (editörde Screen gerçek ekranı vermez).</summary>
        public static float? Override;

        public static float Current =>
            Override ?? (Screen.width > 0 && Screen.height > 0 ? (float)Screen.width / Screen.height : Phone);

        public static bool IsWide(float aspect) => aspect > Phone + 0.001f;

        /// <summary>CanvasScaler.matchWidthOrHeight: telefonda 0 (genişlik), geniş ekranda 1 (yükseklik).</summary>
        public static float Match => IsWide(Current) ? 1f : 0f;

        /// <summary>Verilen oranda tuvalin referans birim boyutu.</summary>
        public static Vector2 CanvasRef(float aspect)
        {
            if (aspect <= 0f) aspect = Phone;
            return IsWide(aspect)
                ? new Vector2(HudView.RefHeight * aspect, HudView.RefHeight)
                : new Vector2(HudView.RefWidth, HudView.RefWidth / aspect);
        }

        /// <summary>Elle ölçek (oynatma dışında CanvasScaler çalışmaz; yakalama aracı).</summary>
        public static float ScaleFactor(int w, int h) => w / CanvasRef(h > 0 ? (float)w / h : Phone).x;

        /// <summary>Normalize güvenli alanı geniş ekranda ortadaki 1080 birimlik sütunla keser.</summary>
        public static Rect Column(Rect n, float aspect)
        {
            if (!IsWide(aspect)) return n;
            float w = HudView.RefWidth / CanvasRef(aspect).x;
            float x0 = (1f - w) * 0.5f;
            float xMin = Mathf.Max(n.xMin, x0), xMax = Mathf.Min(n.xMax, x0 + w);
            return Rect.MinMaxRect(xMin, n.yMin, xMax, n.yMax);
        }
    }
}
