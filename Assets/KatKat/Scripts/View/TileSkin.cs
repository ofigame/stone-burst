using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>Common visible bounds and anchors for the board and its miniature preview.</summary>
    public static class TileSkin
    {
        public const float IceWidth = 1.02f;
        public const float ClipWidth = 0.44f;
        public const float RibbonWidth = 0.98f;
        public const float LockWidth = 0.30f;
        public const float EnvelopeWidth = 0.99f;
        public static readonly Vector2 ClipOffset = new Vector2(0.25f, 0.24f);
        public static readonly Vector2 RibbonOffset = new Vector2(0f, -0.12f);
        public static readonly Vector2 LockOffset = new Vector2(-0.04f, -0.04f);

        private static readonly Dictionary<string, Vector4> Trim = new Dictionary<string, Vector4>
        {
            { "mechanics/clip-closed", new Vector4(50f/512, 72f/493, 414f/512, 366f/493) },
            { "mechanics/clip-open", new Vector4(38f/512, 27f/505, 412f/512, 452f/505) },
            { "mechanics/envelope-open", new Vector4(55f/512, 33f/510, 395f/512, 434f/510) },
            { "mechanics/envelope-closed", new Vector4(46f/507, 61f/512, 408f/507, 364f/512) },
            { "mechanics/lock-ribbon", new Vector4(19f/512, 161f/505, 473f/512, 174f/505) },
            { "icons/lock", new Vector4(69f/512, 23f/512, 372f/512, 468f/512) },
            { "satin/gold-curl", new Vector4(64f/1268, 123f/1240, 1153f/1268, 1018f/1240) },
            { "satin/line-curl", new Vector4(87f/2172, 123f/724, 2002f/2172, 487f/724) },
            { "satin/mascot-happy", new Vector4(334f/1312, 205f/1199, 631f/1312, 721f/1199) },
            { "satin/mascot-cheer", new Vector4(107f/1312, 92f/1199, 1102f/1312, 986f/1199) },
            { "satin/mascot-worried", new Vector4(172f/1166, 174f/1349, 784f/1166, 942f/1349) },
        };
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        /// <summary>v6 renkli görselleri (zaten görünen sınıra kırpılmış) v4 anahtarlarının yerine geçer.</summary>
        private static readonly Dictionary<string, string> V6Override = new Dictionary<string, string>
        {
            { "mechanics/envelope-closed", "envelope" },
            { "mechanics/clip-closed", "clip" },
            { "mechanics/clip-open", "clip" },
        };

        /// <summary>v6 zarfında mühür yüzün ortasında (v4'te biraz aşağıdaydı); renk mührü buraya oturur.</summary>
        public static Vector2 SealOffset => SpecialArt.V6("envelope") != null ? new Vector2(0f, -0.01f) : new Vector2(0f, -0.05f);

        public static Sprite Sprite(string key)
        {
            if (V6Override.TryGetValue(key, out var v6Key))
            {
                var v6 = SpecialArt.V6(v6Key);
                if (v6 != null) return v6;
            }
            if (!Trim.TryGetValue(key, out var visible)) return PremiumArt.V4(key);
            if (Sprites.TryGetValue(key, out var cached) && cached) return cached;
            var original = PremiumArt.V4(key);
            if (original == null) return null;
            var full = original.rect;
            var rect = new Rect(full.x + full.width * visible.x, full.y + full.height * visible.y,
                full.width * visible.z, full.height * visible.w);
            var sprite = UnityEngine.Sprite.Create(original.texture, rect, new Vector2(0.5f, 0.5f),
                original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = original.name + "_visible";
            sprite.hideFlags = HideFlags.DontSave;
            Sprites[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// v11 (docs/v11 §Özel güç): simge üst yüzün MERKEZİNE oturur (eski köşe/bant ofseti uygulanmaz). Görünür kutu
        /// (yüz ölçüsüne oran): yatay ≈ %85 × %30, dikey yer değiştirmiş, renk temizleyici ≈ %70.
        /// </summary>
        public static Vector2 SpecialOffset(SpecialKind kind)
        {
            if (SpecialArt.HasV11) return Vector2.zero;
            return kind == SpecialKind.Rainbow ? Vector2.zero :
                kind == SpecialKind.RocketColumn ? new Vector2(0.29f, 0f) : new Vector2(0f, -0.29f);
        }

        public static Vector2 SpecialBox(SpecialKind kind)
        {
            if (SpecialArt.HasV11)
                return kind == SpecialKind.Rainbow ? new Vector2(0.70f, 0.70f)
                    : kind == SpecialKind.RocketColumn ? new Vector2(0.30f, 0.85f) : new Vector2(0.85f, 0.30f);
            return kind == SpecialKind.Rainbow ? Vector2.one * 1.02f : new Vector2(0.92f, 0.34f);
        }

        public static float SpecialAngle(SpecialKind kind) => SpecialArt.Angle(kind);
    }
}
