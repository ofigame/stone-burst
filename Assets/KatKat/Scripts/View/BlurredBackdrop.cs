using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Oyun ekranı arka planının bulanık kopyası (kullanıcı isteği: tahta ve HUD öne çıksın). GPU ile yarı boyuta
    /// küçültülür ve hafif kutu bulanıklığı uygulanır; aynı dünya boyutunda sprite olur.
    /// Kaynak başına bir kez üretilir ve önbelleğe alınır. Başarısız olursa null (çağıran keskin resme düşer).
    /// </summary>
    public static class BlurredBackdrop
    {
        private static readonly Dictionary<Sprite, Sprite> Cache = new Dictionary<Sprite, Sprite>();
        // v6 (kullanıcı: "oyun arkaplanı bulanık olsun"): belirgin bulanıklık — 1/4 boyut, iki geçiş, yarıçap 4
        // (tam boyutta ~32 px yumuşaklık); yine de sahnenin renkleri ve büyük şekilleri okunur.
        private const int Downscale = 4, Radius = 4, Passes = 2;

        public static Sprite For(Sprite source)
        {
            if (source == null) return null;
            if (Cache.TryGetValue(source, out var cached) && cached != null) return cached;
            try
            {
                var tex = source.texture;
                int w = Mathf.Max(8, tex.width / Downscale), h = Mathf.Max(8, tex.height / Downscale);
                var half = RenderTexture.GetTemporary(tex.width / 2, tex.height / 2, 0, RenderTextureFormat.ARGB32);
                var small = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                half.filterMode = small.filterMode = FilterMode.Bilinear;
                Graphics.Blit(tex, half);
                Graphics.Blit(half, small);
                var prev = RenderTexture.active;
                RenderTexture.active = small;
                var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = source.name + "_blur", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
                outTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(half);
                RenderTexture.ReleaseTemporary(small);

                var px = outTex.GetPixels32();
                for (int p = 0; p < Passes; p++)
                {
                    px = BoxBlur(px, w, h, Radius, true);
                    px = BoxBlur(px, w, h, Radius, false);
                }
                outTex.SetPixels32(px);
                outTex.Apply(false, false);
                // Aynı dünya boyutu: piksel/birim oranı küçültmeyle aynı oranda düşer.
                float ppu = source.pixelsPerUnit * w / tex.width;
                var sp = Sprite.Create(outTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
                sp.name = outTex.name;
                Cache[source] = sp;
                return sp;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[KatKat] Arka plan bulanıklaştırılamadı: " + e.Message);
                return null;
            }
        }

        private static Color32[] BoxBlur(Color32[] src, int w, int h, int r, bool horizontal)
        {
            var dst = new Color32[src.Length];
            int n = 2 * r + 1;
            int lines = horizontal ? h : w, len = horizontal ? w : h;
            for (int line = 0; line < lines; line++)
            {
                int sr = 0, sg = 0, sb = 0;
                int Idx(int i) => horizontal ? line * w + Mathf.Clamp(i, 0, len - 1) : Mathf.Clamp(i, 0, len - 1) * w + line;
                for (int i = -r; i <= r; i++) { var c = src[Idx(i)]; sr += c.r; sg += c.g; sb += c.b; }
                for (int i = 0; i < len; i++)
                {
                    dst[Idx(i)] = new Color32((byte)(sr / n), (byte)(sg / n), (byte)(sb / n), 255);
                    var a = src[Idx(i - r)]; var b = src[Idx(i + r + 1)];
                    sr += b.r - a.r; sg += b.g - a.g; sb += b.b - a.b;
                }
            }
            return dst;
        }
    }
}
