using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// v10 oyun içi kabuk (docs/v10/TriKat-Oyun-Ici-Claude): mor bulutlu oyun arka planı ve krem/altın tahta zemini.
    /// Bütün oyun temalarına uygulanır (sahil/bahçe/çöl dünyaları harita tarafında aynen kalır). Final görsel
    /// Resources/PremiumV6/game/bg.png varsa o, yoksa koddan çizilmiş GEÇİCİ arka plan. <see cref="Enabled"/> false
    /// ise eski davranış (temanın bulanık oyun resmi, bölüme göre pastel zemin) geri gelir.
    /// </summary>
    public static class GameShellArt
    {
        public static bool Enabled = true;

        /// <summary>Tahta zemini: krem iç tepsi, sarı-altın ince kontur.</summary>
        public static readonly Color TrayFill = new Color(1f, 0.953f, 0.874f, 0.97f);
        public static readonly Color TrayRim = new Color32(0xF2, 0xC0, 0x4C, 0xFF);
        public static readonly Color TrayShadow = new Color(0.18f, 0.06f, 0.30f, 0.30f);

        private static Sprite _bg;

        public static bool HasFinalBackground => Resources.Load<Sprite>("PremiumV6/game/bg") != null;

        public static Sprite Background()
        {
            if (!Enabled) return null;
            ResourceUsage.Note("PremiumV6/game/bg");
            var final = Resources.Load<Sprite>("PremiumV6/game/bg");
            if (final != null) return final;
            if (_bg) return _bg;
            const int w = 360, h = 640; // yumuşak sahne; düşük çözünürlük yeterli
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "game_bg_tmp", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[w * h];
            Color center = new Color32(0x7A, 0x52, 0xD8, 0xFF), edge = new Color32(0x4C, 0x2C, 0xA4, 0xFF), cloud = new Color32(0xC9, 0xB2, 0xF4, 0xFF), cloudHi = new Color32(0xE8, 0xDE, 0xFC, 0xFF);
            // Kenarlardaki lavanta bulutları (x, y [0..1 alttan], yarıçap px).
            var blobs = new[]
            {
                new Vector3(0f, 0.86f, 70), new Vector3(40f, 0.80f, 55), new Vector3(360f, 0.88f, 70), new Vector3(320f, 0.82f, 50),
                new Vector3(0f, 0.30f, 80), new Vector3(50f, 0.24f, 60), new Vector3(360f, 0.36f, 85), new Vector3(310f, 0.28f, 60),
                new Vector3(0f, 0.04f, 90), new Vector3(120f, 0.0f, 80), new Vector3(240f, 0.0f, 80), new Vector3(360f, 0.05f, 90),
            };
            var sparks = new[] { new Vector2(40f, 0.72f), new Vector2(320f, 0.70f), new Vector2(36f, 0.20f), new Vector2(328f, 0.18f), new Vector2(60f, 0.95f), new Vector2(300f, 0.97f) };
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = i + 0.5f, y = (j + 0.5f) / h;
                float dx = (x - w * 0.5f) / (w * 0.5f), dy = (y - 0.55f) / 0.6f;
                Color c = Color.Lerp(center, edge, Mathf.Clamp01(Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 0.5f)));
                float best = 0f, hi = 0f;
                foreach (var b in blobs)
                {
                    float ex = x - b.x, ey = (y - b.y) * h;
                    float r = Mathf.Sqrt(ex * ex + ey * ey);
                    float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(b.z - 18f, b.z + 6f, r));
                    if (a > best) { best = a; hi = Mathf.Clamp01((ey + b.z * 0.4f) / b.z); }
                }
                if (best > 0f) c = Color.Lerp(c, Color.Lerp(cloud, cloudHi, hi * 0.7f), best * 0.85f);
                foreach (var s in sparks)
                {
                    float ex = Mathf.Abs(x - s.x), ey = Mathf.Abs((y - s.y) * h);
                    float star = Mathf.Max(0f, 1f - (ex * ey) / 6f - (ex + ey) / 14f);
                    if (star > 0f) c = Color.Lerp(c, Color.white, Mathf.Clamp01(star * 1.4f));
                }
                c.a = 1f;
                px[j * w + i] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return _bg = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
