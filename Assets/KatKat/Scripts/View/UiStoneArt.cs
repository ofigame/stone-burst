using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Cached UI faces from the same masks, palette and front-face lighting as KatKat/Layer.
    /// HUD and miniature boards keep the actual gameplay colour/shape identity.
    /// </summary>
    public static class UiStoneArt
    {
        private static readonly Sprite[] Chips = new Sprite[8];
        public const int FacePixels = 512;

        public static Sprite Chip(LayerColor color) => Layer(color, true);

        public static Sprite Layer(LayerColor color, bool showSymbol)
        {
            int index = (int)color + (showSymbol ? 0 : 4);
            if (Chips[index]) return Chips[index];
            var stones = ProceduralArt.StoneAtlas;
            if (stones != null)
            {
                // v6: renkli taş çizimi (işaret çizime işli; işaretsiz istek de aynı taşı kullanır).
                var c = LayerPalette.IconCell(color);
                var v6 = Sprite.Create(stones, new Rect(c.x * FacePixels, c.y * FacePixels, FacePixels, FacePixels),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                v6.name = "KatKat UI v6 " + color;
                v6.hideFlags = HideFlags.DontSave;
                return Chips[index] = v6;
            }
            var mask = ProceduralArt.ShapeMask;
            var atlas = ProceduralArt.IconAtlas;
            var cell = LayerPalette.IconCell(color);
            Color baseColor = LayerPalette.FrontColor(color);
            Color rimColor = LayerPalette.RimColor(color);
            var pixels = new Color32[FacePixels * FacePixels];
            for (int y = 0; y < FacePixels; y++)
            for (int x = 0; x < FacePixels; x++)
            {
                float u = (x + 0.5f) / FacePixels;
                const int drop = 44;
                float v = (y - drop + 0.5f) / (FacePixels - drop);
                float sideV = (y + 0.5f) / (FacePixels - drop);
                var m = v >= 0f && v <= 1f ? mask.GetPixelBilinear(u, v) : Color.clear;
                var sideMask = sideV <= 1f ? mask.GetPixelBilinear(u, sideV) : Color.clear;
                var icon = showSymbol ? atlas.GetPixelBilinear((cell.x + u) * 0.5f, (cell.y + v) * 0.5f) : Color.clear;
                // Keep these coefficients aligned with Resources/Shaders/KatKatLayer.shader.
                var front = baseColor * (0.58f + 0.47f * m.r);
                Color groove = color == LayerColor.Yellow ? new Color(0.43f, 0.38f, 0.34f) : baseColor * 0.64f;
                front = Color.Lerp(front, groove, icon.r);
                front += new Color(0.18f, 0.18f, 0.18f, 0f) * icon.g;
                front += (Color.white - front) * (0.3f * m.b);
                front += new Color(0.21f, 0.21f, 0.21f, 0f) * m.g;
                // A thin shaded rim adds depth without baking a different stack or another colour.
                float lower = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sideV / 0.11f));
                var side = rimColor * Mathf.Lerp(0.70f, 0.93f, lower) * Mathf.Lerp(0.86f, 1f, sideMask.r);
                side += (Color.white - side) * (0.10f * sideMask.b);
                float alpha = m.a + sideMask.a * (1f - m.a);
                var result = alpha > 0.0001f ? (front * m.a + side * (sideMask.a * (1f - m.a))) / alpha : Color.clear;
                result.a = alpha;
                pixels[y * FacePixels + x] = result;
            }
            var texture = new Texture2D(FacePixels, FacePixels, TextureFormat.RGBA32, false)
            {
                name = "KatKat UI " + color, wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, FacePixels, FacePixels), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "KatKat UI " + color;
            sprite.hideFlags = HideFlags.DontSave;
            return Chips[index] = sprite;
        }
    }
}
