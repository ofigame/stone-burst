using UnityEngine;

namespace KatKat.View
{
    internal static class ReferenceArt
    {
        private static Sprite _cream, _frame;
        public static Sprite Cream => _cream ?? (_cream = Build(false));
        public static Sprite Frame => _frame ?? (_frame = Build(true));

        public static Color FrameTint(string theme) => theme == "garden" ? new Color(0.69f, 0.84f, 0.57f) :
            theme == "desert" ? new Color(1f, 0.79f, 0.56f) : new Color(0.38f, 0.75f, 1f);

        private static Sprite Build(bool frame)
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = frame ? "KatKat Gloss Rim" : "KatKat Cream",
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
                float radius = frame ? 0.16f : 0.46f;
                var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * (0.491f - radius);
                float inset = radius - new Vector2(Mathf.Max(0, q.x), Mathf.Max(0, q.y)).magnitude
                    - Mathf.Min(0, Mathf.Max(q.x, q.y));
                float outer = Mathf.Clamp01(0.5f + inset * size);
                Color color;
                if (frame)
                {
                    const float width = 0.078f;
                    float t = Mathf.Clamp01(inset / width);
                    float gleam = Mathf.Exp(-Mathf.Pow((t - 0.22f) * 10f, 2f));
                    float inner = Mathf.Exp(-Mathf.Pow((t - 0.84f) * 17f, 2f));
                    float value = Mathf.Lerp(0.64f, 0.28f, t) + gleam * 0.52f + inner * 0.47f;
                    color = new Color(value, value, value, outer * Mathf.Clamp01(0.5f + (width - inset) * size));
                }
                else
                {
                    float light = Mathf.InverseLerp(-0.5f, 0.5f, p.y);
                    color = Color.Lerp(new Color(0.91f, 0.76f, 0.57f), new Color(1f, 0.94f, 0.83f), light);
                    float edge = 1f - Mathf.SmoothStep(0f, 1f, inset / 0.055f);
                    color = Color.Lerp(color, p.y >= 0f ? new Color(1f, 0.985f, 0.935f) :
                        new Color(0.68f, 0.45f, 0.27f), edge * 0.85f);
                    color.a = outer;
                }
                pixels[y * size + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var border = Vector4.one * (frame ? 46f : 126f);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f, 0,
                SpriteMeshType.FullRect, border);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
