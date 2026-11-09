using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace KatKat.UI
{
    /// <summary>
    /// Oyunun yazı tipleri (kullanıcı: "fontlar basit ve oyunun tasarımına uyumsuz"): yuvarlak hatlı Nunito (OFL).
    /// Kalın metin ve başlıklar Nunito ExtraBold, normal metin Nunito SemiBold. Arapça (Noto Sans Arabic) ile
    /// Japonca/Korece (Noto Sans KR) dinamik yedek font olarak eklenir. Türkçe harflerin hepsi ana fontta vardır.
    /// </summary>
    public static class UiTypography
    {
        private static TMP_FontAsset _bold, _body, _arabic, _cjk;
        private static readonly Dictionary<Color32, Material> Titles = new Dictionary<Color32, Material>();

        /// <summary>Kalın (başlık, düğme, sayılar).</summary>
        public static TMP_FontAsset Font => Ready(_bold) ? _bold : (_bold = Make("Typography/Nunito-ExtraBold", "KatKat Nunito ExtraBold SDF", true));

        /// <summary>Normal ağırlık (açıklama metinleri).</summary>
        public static TMP_FontAsset Body => Ready(_body) ? _body : (_body = Make("Typography/Nunito-SemiBold", "KatKat Nunito SemiBold SDF", true));

        private static Font _worldBold;

        /// <summary>
        /// Dünya uzayı metni (TextMesh: tahtadaki kilit sayacı) için Nunito ExtraBold (800). Kullanıcı (2026-11-08):
        /// bütün oyunda Nunito — ana metin 800, ikincil 600. Kaynak yoksa yerleşik font.
        /// </summary>
        public static Font WorldBold => _worldBold ? _worldBold
            : (_worldBold = Resources.Load<Font>("Typography/Nunito-ExtraBold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        private static bool Ready(TMP_FontAsset f) => f && f.material && f.atlasTexture;

        private static TMP_FontAsset Make(string path, string name, bool withFallbacks)
        {
            var source = Resources.Load<Font>(path);
            if (!source) source = Resources.Load<Font>("Typography/LiberationSans");
            if (!source) throw new MissingReferenceException(path + " font source is missing.");
            var f = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            f.name = name;
            f.hideFlags = HideFlags.DontSave;
            string chars = "";
            for (int c = 32; c < 127; c++) chars += (char)c;
            chars += "ÇçĞğİıÖöŞşÜü·–—…×✓";
            f.TryAddCharacters(chars, out string missing);
            f.material.hideFlags = HideFlags.DontSave;
            foreach (var atlas in f.atlasTextures) if (atlas) atlas.hideFlags = HideFlags.DontSave;
            foreach (char c in "ÇçĞğİıÖöŞşÜü")
                if (missing != null && missing.IndexOf(c) >= 0)
                    Debug.LogError("[KatKat] Missing Turkish glyph: " + c);
            if (withFallbacks)
            {
                f.fallbackFontAssetTable = new List<TMP_FontAsset>();
                var ar = Fallback(ref _arabic, "Typography/NotoSansArabic", "KatKat Arabic SDF");
                var kr = Fallback(ref _cjk, "Typography/NotoSansKR", "KatKat CJK SDF");
                if (ar) f.fallbackFontAssetTable.Add(ar);
                if (kr) f.fallbackFontAssetTable.Add(kr);
            }
            return f;
        }

        private static TMP_FontAsset Fallback(ref TMP_FontAsset slot, string path, string name)
        {
            if (Ready(slot)) return slot;
            var source = Resources.Load<Font>(path);
            if (!source) return null;
            slot = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            slot.name = name;
            slot.hideFlags = HideFlags.DontSave;
            slot.material.hideFlags = HideFlags.DontSave;
            foreach (var atlas in slot.atlasTextures) if (atlas) atlas.hideFlags = HideFlags.DontSave;
            return slot;
        }

        /// <summary>Metnin bütün karakterleri ana font ya da yedeklerde var mı (dil tablosu testi için).</summary>
        public static bool CanRender(string text, out string missing)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (char.IsControl(c) || char.IsWhiteSpace(c)) continue;
                if (Font.HasCharacter(c, true, true)) continue;
                sb.Append(c);
            }
            missing = sb.ToString();
            return missing.Length == 0;
        }

        public static TextAlignmentOptions Alignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Midline;
            }
        }

        public static void StyleTitle(TextMeshProUGUI text, Color outline)
        {
            Color32 key = outline;
            if (!Titles.TryGetValue(key, out var material) || !material)
            {
                material = new Material(Font.material) { name = "KatKat SDF Title", hideFlags = HideFlags.DontSave };
                material.EnableKeyword("OUTLINE_ON");
                material.SetColor(ShaderUtilities.ID_OutlineColor, outline);
                // Nunito ExtraBold zaten tok: hafif genişletme + belirgin, yumuşak kontur ve alt gölge (şeker görünüm).
                material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.08f);
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
                material.EnableKeyword("UNDERLAY_ON");
                material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(outline.r, outline.g, outline.b, 0.45f));
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.10f);
                Titles[key] = material;
            }
            text.fontSharedMaterial = material;
            text.characterSpacing = -1f;
            text.UpdateMeshPadding();
        }
    }
}
