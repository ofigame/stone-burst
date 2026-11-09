using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.View;
using UnityEngine;

namespace KatKat.UI
{
    public enum UiIcon
    {
        Back,
        Pause,
        Undo,
        Restart,
        Play,
        Check,
        SoundOn,
        SoundOff,
        VibrateOn,
        VibrateOff,
        Lock,
        Roll,
        Ring,
        Menu,
    }

    /// <summary>
    /// Arayüz görselleri koddan üretilir: tek ikon ailesi (beyaz, yuvarlak uçlu çizgiler), 9-dilimli kutu,
    /// hedef çipleri. Harici sprite atlası gerekmez.
    /// </summary>
    public static class UiArt
    {
        public static readonly Color Teal = new Color32(0x1F, 0x78, 0x7C, 0xFF);
        public static readonly Color TealDark = new Color32(0x15, 0x5C, 0x60, 0xFF);
        public static readonly Color Pill = new Color32(0xE4, 0xF2, 0xEF, 0xFF);
        public static readonly Color Ivory = new Color32(0xFB, 0xF6, 0xEE, 0xFF);
        public static readonly Color Ink = new Color32(0x24, 0x4E, 0x52, 0xFF);
        // Premium-v2 rehberi §2 başlangıç renkleri.
        public static readonly Color HudTeal = new Color32(0x07, 0x9E, 0x9B, 0xFF);
        public static readonly Color HudTealDark = new Color32(0x03, 0x5C, 0x5A, 0xFF);
        public static readonly Color Cream = new Color32(0xFF, 0xF0, 0xD5, 0xFF);
        public static readonly Color Brown = new Color32(0x49, 0x36, 0x2E, 0xFF);
        public static readonly Color Gold = new Color32(0xF8, 0xBD, 0x46, 0xFF);
        public static readonly Color MintDark = new Color32(0x0A, 0x6E, 0x4E, 0xFF);
        public static readonly Color CoralDark = new Color32(0xB8, 0x2E, 0x3E, 0xFF);
        public static readonly Color CheckGreen = new Color32(0x3C, 0xC2, 0x52, 0xFF);

        private static Sprite _box;
        private static Sprite _circle;
        private static readonly Dictionary<UiIcon, Sprite> Icons = new Dictionary<UiIcon, Sprite>();
        private static readonly Dictionary<LayerColor, Sprite> Chips = new Dictionary<LayerColor, Sprite>();
        private static Font _font;

        public static Font Font => _font ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        /// <summary>9-dilimli yuvarlak köşeli beyaz kutu.</summary>
        public static Sprite Box
        {
            get
            {
                if (_box) return _box;
                const int s = 96, r = 36;
                var tex = Draw(s, (x, y) =>
                {
                    var q = new Vector2(x - s * 0.5f, y - s * 0.5f);
                    float sd = ProceduralArt.RoundedBoxSdf(q / s, 0.5f - 1f / s, (float)r / s) * s;
                    return 1f - Ease.Smooth(-0.8f, 0.8f, sd);
                });
                _box = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
                return _box;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle) return _circle;
                const int s = 128;
                var tex = Draw(s, (x, y) => 1f - Ease.Smooth(-0.8f, 0.8f, new Vector2(x - s * 0.5f, y - s * 0.5f).magnitude - (s * 0.5f - 1.5f)));
                _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
                return _circle;
            }
        }

        public static Sprite Icon(UiIcon icon)
        {
            if (Icons.TryGetValue(icon, out var sp) && sp) return sp;
            const int s = 128;
            var tex = Draw(s, (x, y) =>
            {
                var p = new Vector2((x + 0.5f) / s, (y + 0.5f) / s);
                return 1f - Ease.Smooth(-1.2f / s, 1.2f / s, IconSdf(icon, p));
            });
            sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            Icons[icon] = sp;
            return sp;
        }

        /// <summary>Hedef/HUD için küçük katman çipi: kenar şeridi + işaretli yüz (oyun taşlarıyla aynı maske ve işaret).</summary>
        public static Sprite Chip(LayerColor color)
        {
            if (Chips.TryGetValue(color, out var sp) && sp) return sp;
            const int s = 128, drop = 10;
            var mask = ProceduralArt.ShapeMask;
            var atlas = ProceduralArt.IconAtlas;
            var cell = LayerPalette.IconCell(color);
            Color front = LayerPalette.FrontColor(color), rim = LayerPalette.RimColor(color);
            var px = new Color[s * s];
            int inner = s - drop;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                // Yüz: üstte, kenar şeridi aşağıda 'drop' piksel kaymış.
                float fu = (x + 0.5f) / s, fv = (y - drop + 0.5f) / inner;
                float rv = (y + 0.5f) / inner;
                var faceM = fv >= 0f && fv <= 1f ? mask.GetPixelBilinear(fu, fv) : Color.clear;
                var rimM = rv <= 1f ? mask.GetPixelBilinear(fu, rv) : Color.clear;
                var c = new Color(rim.r, rim.g, rim.b, rimM.a);
                if (faceM.a > 0f)
                {
                    var ic = atlas.GetPixelBilinear((cell.x + fu) * 0.5f, (cell.y + fv) * 0.5f);
                    var f = front * faceM.r;
                    f = Color.Lerp(f, front * 0.74f, ic.r);
                    f = Color.Lerp(f, f + new Color(0.16f, 0.16f, 0.16f), ic.g);
                    f.a = 1f;
                    c = Color.Lerp(c, f, faceM.a);
                    c.a = Mathf.Max(rimM.a, faceM.a);
                }
                px[y * s + x] = c;
            }
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tex.SetPixels(px);
            tex.Apply(false, false);
            sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            Chips[color] = sp;
            return sp;
        }

        private static Texture2D Draw(int s, Func<int, int, float> alpha)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f));
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        // ---- İkon SDF'leri (p ∈ [0,1]², y yukarı). Çizgi kalınlığı ortak. ----------------------------

        private const float W = 0.055f;

        private static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        private static float Arc(Vector2 p, Vector2 c, float r, float fromDeg, float toDeg)
        {
            var d = p - c;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            bool inside = fromDeg <= toDeg ? ang >= fromDeg && ang <= toDeg : ang >= fromDeg || ang <= toDeg;
            if (inside) return Mathf.Abs(d.magnitude - r);
            var a = c + r * new Vector2(Mathf.Cos(fromDeg * Mathf.Deg2Rad), Mathf.Sin(fromDeg * Mathf.Deg2Rad));
            var b = c + r * new Vector2(Mathf.Cos(toDeg * Mathf.Deg2Rad), Mathf.Sin(toDeg * Mathf.Deg2Rad));
            return Mathf.Min((p - a).magnitude, (p - b).magnitude);
        }

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static float IconSdf(UiIcon icon, Vector2 p)
        {
            switch (icon)
            {
                case UiIcon.Back:
                    return Mathf.Min(Seg(p, V(0.28f, 0.5f), V(0.74f, 0.5f)),
                        Mathf.Min(Seg(p, V(0.28f, 0.5f), V(0.48f, 0.7f)), Seg(p, V(0.28f, 0.5f), V(0.48f, 0.3f)))) - W;
                case UiIcon.Pause:
                    return Mathf.Min(Seg(p, V(0.38f, 0.3f), V(0.38f, 0.7f)), Seg(p, V(0.62f, 0.3f), V(0.62f, 0.7f))) - W * 1.25f;
                case UiIcon.Undo:
                {
                    // Klasik geri al: sağda yarım yay, üstte sola bakan ok, altta kısa kuyruk.
                    float arc = Arc(p, V(0.56f, 0.44f), 0.19f, 270f, 90f);
                    float topLine = Seg(p, V(0.56f, 0.63f), V(0.27f, 0.63f));
                    float head = Mathf.Min(Seg(p, V(0.27f, 0.63f), V(0.41f, 0.77f)), Seg(p, V(0.27f, 0.63f), V(0.41f, 0.49f)));
                    float tail = Seg(p, V(0.56f, 0.25f), V(0.36f, 0.25f));
                    return Mathf.Min(Mathf.Min(arc, topLine), Mathf.Min(head, tail)) - W;
                }
                case UiIcon.Restart:
                {
                    float arc = Arc(p, V(0.5f, 0.5f), 0.24f, 110f, 70f);
                    var tip = V(0.5f, 0.5f) + 0.24f * V(Mathf.Cos(70f * Mathf.Deg2Rad), Mathf.Sin(70f * Mathf.Deg2Rad));
                    float head = Mathf.Min(Seg(p, tip, tip + V(-0.17f, 0.04f)), Seg(p, tip, tip + V(-0.03f, -0.16f)));
                    return Mathf.Min(arc, head) - W;
                }
                case UiIcon.Play:
                {
                    var q = p - V(0.54f, 0.5f);
                    float tri = Mathf.Max(Mathf.Abs(q.y) * 1.15f + q.x * 0.6f - 0.14f, -q.x - 0.17f);
                    return tri - 0.02f;
                }
                case UiIcon.Check:
                    return Mathf.Min(Seg(p, V(0.27f, 0.5f), V(0.44f, 0.33f)), Seg(p, V(0.44f, 0.33f), V(0.75f, 0.68f))) - W * 1.1f;
                case UiIcon.SoundOn:
                case UiIcon.SoundOff:
                {
                    var q = p - V(0.36f, 0.5f);
                    float body = Mathf.Max(Mathf.Abs(q.x + 0.06f) - 0.06f, Mathf.Abs(q.y) - 0.08f);
                    float cone = Mathf.Max(Mathf.Max(-q.x - 0.0f, q.x - 0.12f), Mathf.Abs(q.y) - (0.08f + q.x * 1.1f));
                    float spk = Mathf.Min(body, cone) - 0.01f;
                    if (icon == UiIcon.SoundOn)
                        return Mathf.Min(spk, Mathf.Min(Arc(p, V(0.48f, 0.5f), 0.13f, 315f, 45f), Arc(p, V(0.48f, 0.5f), 0.24f, 315f, 45f)) - W * 0.8f);
                    return Mathf.Min(spk, Mathf.Min(Seg(p, V(0.6f, 0.38f), V(0.8f, 0.62f)), Seg(p, V(0.6f, 0.62f), V(0.8f, 0.38f))) - W * 0.8f);
                }
                case UiIcon.VibrateOn:
                case UiIcon.VibrateOff:
                {
                    var q = p - V(0.5f, 0.5f);
                    float phone = Mathf.Abs(ProceduralArt.RoundedBoxSdf(new Vector2(q.x / 0.6f, q.y), 0.21f, 0.05f)) - W * 0.8f;
                    if (icon == UiIcon.VibrateOn)
                        return Mathf.Min(phone, Mathf.Min(Seg(p, V(0.2f, 0.4f), V(0.2f, 0.6f)), Seg(p, V(0.8f, 0.4f), V(0.8f, 0.6f))) - W * 0.8f);
                    return Mathf.Min(phone, Seg(p, V(0.22f, 0.22f), V(0.78f, 0.78f)) - W * 0.8f);
                }
                case UiIcon.Lock:
                {
                    float body = ProceduralArt.RoundedBoxSdf((p - V(0.5f, 0.4f)) / 0.44f, 0.5f, 0.12f) * 0.44f;
                    float shackle = Arc(p, V(0.5f, 0.58f), 0.13f, 0f, 180f);
                    shackle = Mathf.Min(shackle, Mathf.Min(Seg(p, V(0.37f, 0.58f), V(0.37f, 0.5f)), Seg(p, V(0.63f, 0.58f), V(0.63f, 0.5f)))) - W;
                    return Mathf.Min(body, shackle);
                }
                case UiIcon.Roll:
                {
                    // Yandan görülen rulo: spiral daireler + uzanan şerit.
                    float outer = Mathf.Abs((p - V(0.36f, 0.5f)).magnitude - 0.17f) - W * 0.8f;
                    float inner = Mathf.Abs((p - V(0.36f, 0.5f)).magnitude - 0.07f) - W * 0.7f;
                    float tail = Mathf.Min(Seg(p, V(0.36f, 0.33f), V(0.8f, 0.33f)), Seg(p, V(0.8f, 0.33f), V(0.8f, 0.42f))) - W * 0.8f;
                    return Mathf.Min(Mathf.Min(outer, inner), tail);
                }
                case UiIcon.Ring:
                    return Mathf.Abs((p - V(0.5f, 0.5f)).magnitude - 0.4f) - 0.045f;
                default: // Menu: üç yatay çizgi
                    return Mathf.Min(Seg(p, V(0.28f, 0.32f), V(0.72f, 0.32f)), Mathf.Min(Seg(p, V(0.28f, 0.5f), V(0.72f, 0.5f)), Seg(p, V(0.28f, 0.68f), V(0.72f, 0.68f)))) - W;
            }
        }
    }
}
