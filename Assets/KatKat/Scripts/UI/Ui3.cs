using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// ui3 onaylı tasarım (docs/ui3_onayli_tasarim, kullanıcı 2026-11-08: "yeni tasarımı birebir uygula"):
    /// tasarim-tokenlari.json renkleri ve PNG olmayan boyalı yüzeyler (02_RENK_MALZEME.md reçeteleri). Yüzeyler koddan
    /// çizilen 9 dilimli yuvarlak dikdörtgen (dikey gradyan, iç kontur, isteğe bağlı ikinci kontur); köşe yarıçapı
    /// referans px. Seramik panel/düğme/plaka PNG'leri Resources/UI3 (Ui1 kiti bunları kullanır).
    /// </summary>
    public static class Ui3
    {
        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out var c);
            return c;
        }

        public static readonly Color Ink = Hex("#203846");
        public static readonly Color InkSecondary = Hex("#53646D");
        public static readonly Color InkMuted = Hex("#66747B");
        public static readonly Color Ivory = Hex("#FFF3DE");
        public static readonly Color IvoryTop = Hex("#FFF7E8");
        public static readonly Color IvoryBottom = Hex("#F1DFC0");
        public static readonly Color Sand = Hex("#D8C09C");
        public static readonly Color Brass = Hex("#B99760");
        public static readonly Color RimHighlight = Hex("#FFF9ED");
        public static readonly Color RimShadow = Hex("#8E785C");
        public static readonly Color Teal = Hex("#118C83");
        public static readonly Color TealTop = Hex("#1BA99E");
        public static readonly Color TealBottom = Hex("#08766F");
        public static readonly Color TealPressed = Hex("#06645E");
        public static readonly Color OnTeal = Hex("#FFF9ED");
        public static readonly Color Coral = Hex("#CC6956");
        public static readonly Color Purple = Hex("#685680");
        public static readonly Color PurpleDark = Hex("#42374F");
        public static readonly Color Success = Hex("#32866D");
        public static readonly Color Warning = Hex("#AD7838");
        public static readonly Color Error = Hex("#A44E43");
        public static readonly Color Track = Hex("#D9CEB9");
        public static readonly Color Divider = Hex("#D6C6AD");
        public static readonly Color Scrim = Hex("#111C24");
        public static readonly Color Shadow = Hex("#302C2A");
        public static readonly Color HomeTop = Hex("#697F73");
        public static readonly Color HomeBottom = Hex("#435E55");
        public static readonly Color TabUnselected = Hex("#F0E6D3");
        public static readonly Color TabSelected = Hex("#147E75");
        public static readonly Color LockedVeil = Hex("#5E6C70");
        public static readonly Color UnearnedStar = Hex("#D4C8AA");

        /// <summary>Modal karartması: #111C24, alfa 0.72 (iç içe ikinci kat 0.3571429 → toplam 0.82).</summary>
        public static Color ScrimColor => new Color(Scrim.r, Scrim.g, Scrim.b, 0.72f);
        public static Color NestedScrimColor => new Color(Scrim.r, Scrim.g, Scrim.b, 0.3571429f);
        public static Color TutorialScrimColor => new Color(Scrim.r, Scrim.g, Scrim.b, 0.76f);

        public static bool Ready => Art("modal-seramik") != null;

        public static Sprite Art(string key) { KatKat.View.ResourceUsage.Note("UI3/" + key); return Resources.Load<Sprite>("UI3/" + key); }

        /// <summary>Oyun zemini Z1–Z5 (0..4).</summary>
        public static Sprite GameBackground(int i)
        {
            for (int z = 1; z <= 5; z++) KatKat.View.ResourceUsage.Note("UI3/bg/Z" + z); // rastgele: beşi de kullanılır
            return Resources.Load<Sprite>("UI3/bg/Z" + (Mathf.Clamp(i, 0, 4) + 1));
        }

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Yuvarlak dikdörtgen sprite (1 doku px = 1 referans px): dikey gradyan top→bottom, dışta <paramref name="outline"/>
        /// px kontur, konturun hemen içinde isteğe bağlı <paramref name="innerW"/> px ikinci hat.
        /// <paramref name="height"/> &gt; 0 ise doku tam yükseklikte üretilir ve yalnız yatay 9 dilim olur (gradyan korunur);
        /// 0 ise kare 9 dilim (düz dolgu için).
        /// </summary>
        public static Sprite Rounded(float radius, Color top, Color bottom, float outline = 0f, Color outlineColor = default, float innerW = 0f, Color inner = default, int height = 0)
        {
            string key = $"{radius}|{top}|{bottom}|{outline}|{outlineColor}|{innerW}|{inner}|{height}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            int r = Mathf.CeilToInt(radius);
            int w = 2 * r + 4;
            int h = height > 0 ? Mathf.Max(height, 2 * r) : w;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "ui3_round", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[w * h];
            float hw = w * 0.5f, hh = h * 0.5f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                // Kenardan içeri uzaklık (yuvarlak köşeli kutu SDF'i).
                float qx = Mathf.Abs(fx - hw) - (hw - radius), qy = Mathf.Abs(fy - hh) - (hh - radius);
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f);
                float d = radius - outside;
                float a = Mathf.Clamp01(d + 0.5f);
                if (a <= 0f) { px[y * w + x] = new Color32(0, 0, 0, 0); continue; }
                Color col = Color.Lerp(top, bottom, 1f - fy / h);
                if (innerW > 0f) col = Color.Lerp(col, inner, Mathf.Clamp01(Mathf.Min(d - outline + 0.5f, outline + innerW - d + 0.5f)));
                if (outline > 0f) col = Color.Lerp(col, outlineColor, Mathf.Clamp01(outline - d + 0.5f));
                col.a *= a;
                px[y * w + x] = col;
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            var border = height > 0 ? new Vector4(r + 2, 0f, r + 2, 0f) : new Vector4(r + 2, r + 2, r + 2, r + 2);
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sp.hideFlags = HideFlags.DontSave;
            Cache[key] = sp;
            return sp;
        }

        /// <summary>Düz ya da dikey gradyanlı yuvarlak yüzey (Image.Sliced, ölçek 1). Gradyan için gerçek yükseklik verilir.</summary>
        public static Image Surface(string name, Transform parent, float radius, Color top, Color bottom, float height = 0f, float outline = 0f, Color outlineColor = default, float innerW = 0f, Color inner = default)
        {
            var sp = Rounded(radius, top, bottom, outline, outlineColor, innerW, inner, top == bottom ? 0 : Mathf.RoundToInt(height));
            var img = UiFactory.Image(name, parent, sp, Color.white, true);
            img.pixelsPerUnitMultiplier = 1f;
            img.raycastTarget = false;
            return img;
        }

        public static Image Surface(string name, Transform parent, float radius, Color fill, float outline = 0f, Color outlineColor = default) =>
            Surface(name, parent, radius, fill, fill, 0f, outline, outlineColor);
        private static Sprite _vfade;
        /// <summary>Üstte opak, altta saydam dikey geçiş (4×64).</summary>
        public static Sprite VerticalFade()
        {
            if (_vfade) return _vfade;
            var tex = new Texture2D(4, 64, TextureFormat.RGBA32, false) { name = "ui3_vfade", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[4 * 64];
            for (int y = 0; y < 64; y++)
            {
                byte a = (byte)Mathf.RoundToInt(255f * (y / 63f));
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return _vfade = Sprite.Create(tex, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 64f);
        }

        /// <summary>Ana sayfa üst yüzeyi: homeTop→homeBottom dikey gradyan (koddan, 4×256).</summary>
        public static Sprite HomeGradient()
        {
            const string key = "home-gradient";
            if (Cache.TryGetValue(key, out var c) && c != null) return c;
            var tex = new Texture2D(4, 256, TextureFormat.RGBA32, false) { name = "ui3_home", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[4 * 256];
            for (int y = 0; y < 256; y++)
            {
                Color col = Color.Lerp(HomeBottom, HomeTop, y / 255f);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = col;
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, 4, 256), new Vector2(0.5f, 0.5f), 100f);
            sp.hideFlags = HideFlags.DontSave;
            return Cache[key] = sp;
        }

        /// <summary>Metin rolleri (01_OLCU_TIPOGRAFI.md): boyut, kalınlık (800 → Bold, 600 → Normal), renk.</summary>
        public static void Role(TMPro.TextMeshProUGUI t, int size, bool heavy, Color color)
        {
            if (t == null) return;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = heavy ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
        }
    }
}
