using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Onaylı saten görseller ve yeniden kullanılabilir mekanik maskeleri.
    /// Rainbow kayıt kimliği oyun arayüzünde Altın Rulo olarak görünür.
    /// </summary>
    public static class SpecialArt
    {
        private const int N = 256;
        private static Sprite _rocket, _rainbow, _disc, _glow;

        public static Sprite Rocket => _rocket ? _rocket : (_rocket = TileSkin.Sprite("satin/line-curl") ?? Make("rocket", RocketPixel));
        public static Sprite Rainbow => _rainbow ? _rainbow : (_rainbow = V6("gold-roll") ?? Make("gold_roll", GoldRollPixel));

        private static Sprite _rollIcon;
        /// <summary>v11 rulo simgesi (docs/v11/skiller/04-rulo.png, kullanıcı 2026-11-08): turkuaz silindir, altın sap, inci iz. Yoksa eski rozet.</summary>
        public static Sprite RollIcon => ForceLegacy ? GoldRollBadge : _rollIcon ? _rollIcon : (_rollIcon = V6("skills/special-roll") ?? GoldRollBadge);
        public static bool HasRollIcon => !ForceLegacy && V6("skills/special-roll") != null;

        /// <summary>v6 renkli mekanik görseli (Resources/PremiumV6, tools/ArtV6/prepare.ps1 görünen sınıra kırpar). Yoksa null.</summary>
        public static Sprite V6(string key)
        {
            ResourceUsage.Note("PremiumV6/" + key);
            var s = Resources.Load<Sprite>("PremiumV6/" + key);
            return s;
        }
        /// <summary>Yumuşak kenarlı beyaz disk (göbek ve parıltılar için, renk SpriteRenderer.color ile).</summary>
        public static Sprite Disc => _disc ? _disc : (_disc = Make("disc", (x, y) => Over(Color.clear, Color.white, Cov(Circle(x, y, 0f, 0f, 0.46f)))));
        /// <summary>Merkezden dışa sönen ışık (etki parlaması).</summary>
        public static Sprite Glow => _glow ? _glow : (_glow = Make("glow", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y) / 0.5f;
            float a = Mathf.Clamp01(1f - r);
            return new Color(1f, 1f, 1f, a * a);
        }));

        // v11 özel güç simgeleri (docs/v11/skiller, 1024 kaynak; görünür sınıra kırpılmış, merkez pivot):
        // renk temizleyici (Rainbow kimliği), yatay (RocketRow), dikey (RocketColumn; yön görselde, döndürülmez).
        private static Sprite _v11Color, _v11Row, _v11Col;
        private static Sprite V11Color => _v11Color ? _v11Color : (_v11Color = V6("skills/special-color"));
        private static Sprite V11Row => _v11Row ? _v11Row : (_v11Row = V6("skills/special-row"));
        private static Sprite V11Col => _v11Col ? _v11Col : (_v11Col = V6("skills/special-column"));

        /// <summary>v11 simgeleri yüklü mü (yoksa eski Altın Rulo / rulo roketi; o durumda sütun 90° döner).</summary>
        public static bool HasV11 => !ForceLegacy && V11Row != null && V11Col != null && V11Color != null;
        /// <summary>Yakalama aracı için: eski simgeleri zorla (aynı bölümde önce/sonra). Oyunda false.</summary>
        public static bool ForceLegacy;

        /// <summary>
        /// Özel taşın tek görsel kaynağı: tahta, tepsi yuvası, sonraki bölüm önizlemesi, doğma uçuşu ve ateşleme
        /// efekti bunu kullanır (bir kez yüklenip önbellekte).
        /// </summary>
        public static Sprite For(SpecialKind k)
        {
            if (HasV11)
                return k == SpecialKind.Rainbow ? V11Color : k == SpecialKind.RocketRow ? V11Row : k == SpecialKind.RocketColumn ? V11Col : null;
            return k == SpecialKind.Rainbow ? Rainbow : k.IsRocket() ? Rocket : null;
        }

        /// <summary>Simgenin görüntü dönüşü: v11'de hiç (yön görselde), eski rulo roketinde sütun 90°.</summary>
        public static float Angle(SpecialKind k) => !HasV11 && k == SpecialKind.RocketColumn ? 90f : 0f;

        private static readonly System.Collections.Generic.Dictionary<int, Sprite> _halves = new System.Collections.Generic.Dictionary<int, Sprite>();

        /// <summary>
        /// Kullanıcı (2026-11-08): yatay/dikey ateşlenince simge ortadan ikiye bölünür, yarılar zıt yönlere gider.
        /// side −1: sol (yatay) / alt (dikey) yarı; +1: sağ / üst yarı. Pivot kesik kenarda değil, yarının kendi
        /// merkezinde; ayrılırken kaynak konumdan ±çeyrek genişlik kaydırılarak yerleştirilir.
        /// </summary>
        public static Sprite Half(SpecialKind k, int side)
        {
            var full = For(k);
            if (full == null) return null;
            int key = (int)k * 4 + (side < 0 ? 0 : 1);
            if (_halves.TryGetValue(key, out var s) && s) return s;
            var r = full.textureRect;
            bool horiz = k != SpecialKind.RocketColumn || !HasV11;
            Rect rect = horiz
                ? new Rect(side < 0 ? r.x : r.x + r.width * 0.5f, r.y, r.width * 0.5f, r.height)
                : new Rect(r.x, side < 0 ? r.y : r.y + r.height * 0.5f, r.width, r.height * 0.5f);
            s = Sprite.Create(full.texture, rect, new Vector2(0.5f, 0.5f), full.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            s.name = full.name + (side < 0 ? "_a" : "_b");
            s.hideFlags = HideFlags.DontSave;
            return _halves[key] = s;
        }

        private static Sprite _sponge, _goldRollBadge, _square, _button, _drop;
        private static Sprite _clipClosed, _clipOpen;

        public static Sprite Clip(bool open)
        {
            // v6 tek ataş görseli var; açık hâl aynı görselle (BoardView açılışı hareketle anlatır).
            if (open)
                return _clipOpen ? _clipOpen : (_clipOpen = V6("clip") ?? Make("clip_open", (x, y) => ClipPixel(x, y, true)));
            return _clipClosed ? _clipClosed : (_clipClosed = V6("clip") ?? Make("clip_closed", (x, y) => ClipPixel(x, y, false)));
        }

        private static Color ClipPixel(float x, float y, bool open)
        {
            const float cos = 0.82f, sin = 0.5724f;
            float u = x * cos + y * sin, v = -x * sin + y * cos;
            float outer = Mathf.Abs(RoundRect(u, v, 0.45f, 0.20f, 0.19f));
            float inner = Mathf.Abs(RoundRect(u + 0.052f, v - 0.026f - (open ? 0.064f : 0f), 0.30f, 0.102f, 0.10f));
            if (u > 0.09f && v < -0.14f) outer = 1f;
            if (u < -0.14f && v > 0.045f) inner = 1f;
            float wire = Mathf.Min(outer, inner);
            float alpha = Cov(wire - 0.037f);
            float light = Mathf.Clamp01(0.32f + 0.60f * (1f - wire / 0.037f) + 0.22f * v);
            Color metal = Color.Lerp(new Color(0.32f, 0.38f, 0.43f), new Color(0.97f, 0.98f, 1f), light);
            metal.a = alpha;
            return metal;
        }

        /// <summary>Thin satin ring with an open center to preserve the face and its symbol.</summary>
        public static Sprite Sponge => _sponge ? _sponge : (_sponge = V6("sponge-ring") ?? Make("sponge", SpongeRing));

        public static Sprite SpongeGoal => Sponge;

        public static Sprite GoldRollBadge => _goldRollBadge ? _goldRollBadge : (_goldRollBadge = Make("gold_roll_badge", (x, y) =>
        {
            float rim = Circle(x, y, 0f, 0f, 0.47f);
            var color = GoldRollPixel(x * 0.9f, y * 0.9f);
            float bevel = Mathf.Clamp01(1f + rim / 0.037f);
            color = Color.Lerp(color, new Color(0.99f, 0.86f, 0.62f), bevel * 0.83f);
            color.a = Cov(rim);
            return color;
        }));

        private static Color SpongeRing(float x, float y)
        {
            float outer = Circle(x, y, 0f, 0f, 0.485f);
            float inner = Circle(x, y, 0f, 0f, 0.326f);
            float ring = Cov(outer) * (1f - Cov(inner));
            if (ring <= 0f) return Color.clear;

            float light = Mathf.InverseLerp(-0.55f, 0.55f, y - x * 0.32f);
            var shade = new Color32(87, 127, 52, 255);
            var highlight = new Color32(193, 224, 126, 255);
            Color body = Color.Lerp(shade, highlight, light);
            float bevel = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(-outer, inner) / 0.04f));
            body = Color.Lerp(shade, body, 0.56f + 0.44f * bevel);

            // Small, fixed indentations soften the surface without competing with its status dots.
            float pores = 0f;
            for (int i = 0; i < 31; i++)
            {
                float a = i * Mathf.PI * 2f / 31f;
                float sx = Mathf.Cos(a), sy = Mathf.Sin(a);
                float r = 0.402f + 0.036f * Mathf.Sin(i * 6.37f + i * 0.81f);
                float rx = r * sx;
                float ry = r * sy;
                pores = Mathf.Max(pores, Cov(Circle(x, y, rx, ry, 0.009f + (i % 3) * 0.003f)));
            }
            body = Color.Lerp(body, shade, pores * 0.73f);
            body.a = ring;
            return body;
        }

        /// <summary>Yuvarlatılmış kare (geçit hayaleti, renk SpriteRenderer.color ile).</summary>
        public static Sprite Square => _square ? _square : (_square = Make("square", (x, y) => Over(Color.clear, Color.white, Cov(RoundRect(x, y, 0.44f, 0.44f, 0.12f)))));

        /// <summary>M11 düğme: mercan kubbe, krem halka.</summary>
        public static Sprite Button => _button ? _button : (_button = Make("button", (x, y) =>
        {
            var c = Over(Color.clear, new Color(0.3f, 0.12f, 0.1f, 0.35f), Mathf.Clamp01(0.5f - Circle(x, y + 0.04f, 0f, 0f, 0.42f) / 0.03f));
            c = Over(c, Cream, Cov(Circle(x, y, 0f, 0f, 0.42f)));
            c = Over(c, new Color32(0xB8, 0x3A, 0x30, 0xFF), Cov(Circle(x, y, 0f, 0f, 0.33f)));
            c = Over(c, new Color32(0xF2, 0x6A, 0x5C, 0xFF), Cov(Circle(x, y + 0.03f, 0f, 0f, 0.29f)));
            c = Over(c, new Color(1f, 1f, 1f, 0.5f), Cov(Circle(x, y, -0.09f, 0.12f, 0.08f)));
            return c;
        }));

        /// <summary>M12 boya damlası simgesi.</summary>
        public static Sprite Drop => _drop ? _drop : (_drop = Make("drop", (x, y) =>
        {
            // Damla: alt daire + üst sivri uç.
            float circle = Circle(x, y, 0f, -0.1f, 0.26f);
            float tip = Mathf.Max(Mathf.Abs(x) - (0.36f - (y + 0.1f)) * 0.62f, -(y + 0.1f));
            tip = Mathf.Max(tip, y - 0.36f);
            float d = Mathf.Min(circle, tip);
            var c = Over(Color.clear, new Color32(0x1E, 0x7F, 0x8A, 0xFF), Cov(d));
            c = Over(c, new Color32(0x3F, 0xD1, 0xC6, 0xFF), Cov(d + 0.03f));
            c = Over(c, new Color(1f, 1f, 1f, 0.6f), Cov(Circle(x, y, -0.09f, -0.04f, 0.06f)));
            return c;
        }));

        // ---- Geçici maskot "Katçık" (kullanıcı: "maskotu rastgele bir şey üret, sonra değişiriz") ----------------

        public enum Mood { Happy, Cheer, Worried }
        private static readonly Sprite[] _mascot = new Sprite[3];

        /// <summary>Dört yumuşak katmandan yığın karakter: mercan, sarı, turkuaz, lila; üst katmanda gözler ve ağız.</summary>
        public static Sprite Mascot(Mood mood)
        {
            int i = (int)mood;
            if (_mascot[i]) return _mascot[i];
            // Kristal maskot (docs/Kristal-Maskot-Claude): onaylı tam karakter; harita düğümü ve dönüş düğmesi bunu
            // gösterir. Yalnızca Happy kullanılıyor; diğer ruh hâlleri de eski maskota düşmesin diye aynı görsel.
            var crystal = PremiumArt.V4("mascot-crystal/mascot-full-v1");
            if (crystal != null) return _mascot[i] = crystal;
            return _mascot[i] = TileSkin.Sprite("satin/mascot-" + mood.ToString().ToLowerInvariant())
                ?? Make("mascot_" + mood, (x, y) => MascotPixel(x, y, mood));
        }

        private static Color MascotPixel(float x, float y, Mood mood)
        {
            var c = Over(Color.clear, new Color(0.2f, 0.12f, 0.08f, 0.25f), Mathf.Clamp01(0.5f - (Mathf.Sqrt(x * x * 1.6f + (y + 0.4f) * (y + 0.4f) * 12f) - 0.34f) / 0.04f));
            // Kollar (arkada): neşede yukarı, diğerlerinde yanlara.
            float armY = mood == Mood.Cheer ? 0.12f : -0.08f;
            for (int s = -1; s <= 1; s += 2)
            {
                float ax = s * 0.34f, ay = armY + (mood == Mood.Cheer ? 0.08f : 0f);
                c = Over(c, new Color32(0x3A, 0xB8, 0xB0, 0xFF), Cov(Mathf.Sqrt((x - ax) * (x - ax) * 2.2f + (y - ay) * (y - ay)) - 0.07f));
            }
            Color[] cols = { new Color32(0xF2, 0x6A, 0x5C, 0xFF), new Color32(0xFF, 0xCF, 0x4A, 0xFF), new Color32(0x3F, 0xC6, 0xBE, 0xFF), new Color32(0xB9, 0x8A, 0xE8, 0xFF) };
            for (int k = 0; k < 4; k++)
            {
                float cy = -0.27f + k * 0.13f, hw = 0.3f - k * 0.015f;
                float d = RoundRect(x, y - cy, hw, 0.085f, 0.07f);
                var dark = Color.Lerp(cols[k], Color.black, 0.25f);
                c = Over(c, dark, Cov(d));
                c = Over(c, cols[k], Cov(RoundRect(x, y - cy - 0.012f, hw - 0.01f, 0.07f, 0.06f)));
                c = Over(c, new Color(1f, 1f, 1f, 0.35f), Cov(RoundRect(x - 0.08f, y - cy - 0.05f, hw * 0.45f, 0.012f, 0.012f)));
            }
            // Yüz (üst katman bölgesi, ön yüz): gözler.
            float ey = 0.16f, ex = 0.11f;
            for (int s = -1; s <= 1; s += 2)
            {
                float dx = x - s * ex, dy = y - ey;
                if (mood == Mood.Cheer)
                {
                    // Kapalı mutlu gözler: ters yay.
                    float arc = Mathf.Abs(Mathf.Sqrt(dx * dx + (dy + 0.03f) * (dy + 0.03f)) - 0.045f) - 0.011f;
                    if (dy > -0.03f) c = Over(c, new Color32(0x2B, 0x1E, 0x2E, 0xFF), Cov(arc));
                    continue;
                }
                c = Over(c, Color.white, Cov(Mathf.Sqrt(dx * dx * 1.1f + dy * dy * 0.8f) - 0.06f));
                float px = mood == Mood.Worried ? 0.008f * s : 0.01f;
                c = Over(c, new Color32(0x2B, 0x1E, 0x2E, 0xFF), Cov(Mathf.Sqrt((dx - px) * (dx - px) + (dy + 0.005f) * (dy + 0.005f)) - 0.034f));
                c = Over(c, Color.white, Cov(Mathf.Sqrt((dx - px + 0.012f) * (dx - px + 0.012f) + (dy - 0.016f) * (dy - 0.016f)) - 0.011f));
                if (mood == Mood.Worried)
                {
                    // Endişeli kaşlar.
                    float bx = dx, by = dy - 0.085f + bx * s * 0.5f;
                    c = Over(c, new Color32(0x2B, 0x1E, 0x2E, 0xFF), Cov(Mathf.Max(Mathf.Abs(by) - 0.008f, Mathf.Abs(bx) - 0.045f)));
                }
            }
            // Ağız.
            float my = 0.065f;
            if (mood == Mood.Worried)
            {
                float wob = y - my - 0.012f * Mathf.Sin(x * 70f);
                c = Over(c, new Color32(0x2B, 0x1E, 0x2E, 0xFF), Cov(Mathf.Max(Mathf.Abs(wob) - 0.008f, Mathf.Abs(x) - 0.05f)));
            }
            else
            {
                float r = mood == Mood.Cheer ? 0.06f : 0.045f;
                float dm = Mathf.Sqrt(x * x + (y - my - r * 0.4f) * (y - my - r * 0.4f)) - r;
                if (y < my + r * 0.4f) c = Over(c, new Color32(0x8A, 0x22, 0x2E, 0xFF), Cov(dm));
                if (y < my + r * 0.4f) c = Over(c, new Color32(0xF2, 0x8A, 0x8A, 0xFF), Cov(Mathf.Sqrt(x * x + (y - my + r * 0.55f) * (y - my + r * 0.55f)) - r * 0.45f) * Cov(dm));
            }
            // Yanaklar.
            for (int s = -1; s <= 1; s += 2)
                c = Over(c, new Color(1f, 0.55f, 0.6f, 0.5f), Cov(Mathf.Sqrt((x - s * 0.19f) * (x - s * 0.19f) * 0.8f + (y - 0.1f) * (y - 0.1f) * 1.6f) - 0.035f));
            return c;
        }

        private static Sprite _ice2, _ice1;

        /// <summary>
        /// Buz bloğu: taş yüzüyle aynı yuvarlatılmış kare (kenar boşluğu yok), yarı saydam buz dolgusu, beyaz kenar,
        /// üstten parlama ve kristal yüzeyler. Tek vuruşu kalan buz çatlaklı. Taş rengi altından okunur.
        /// </summary>
        public static Sprite Ice(bool cracked) => cracked
            ? (_ice1 ? _ice1 : (_ice1 = V6("ice-cracked") ?? Make("ice1", (x, y) => IcePixel(x, y, true))))
            : (_ice2 ? _ice2 : (_ice2 = V6("ice-solid") ?? Make("ice2", (x, y) => IcePixel(x, y, false))));

        private static Color IcePixel(float x, float y, bool cracked)
        {
            float d = RoundRect(x, y, 0.49f, 0.49f, 0.15f);
            float body = Cov(d);
            if (body <= 0f) return Color.clear;
            float edge = Mathf.Exp(-Mathf.Abs(d + 0.053f) * 26f);
            float diagonal = Mathf.Clamp01(1f - Mathf.Abs(x + y * 1.3f - 0.14f) * 4f);
            var c = new Color(0.12f, 0.62f, 0.87f, 0.37f + edge * 0.34f);
            c = Over(c, new Color(0.86f, 0.98f, 1f, 0.36f), diagonal);
            c = Over(c, new Color(1f, 1f, 1f, 0.87f), edge * (0.34f + 0.56f * Mathf.Clamp01(y + 0.55f)));
            c = Over(c, new Color(1f, 1f, 1f, 0.79f), Cov(RoundRect(x + 0.09f, y - 0.365f, 0.25f, 0.032f, 0.03f)));
            if (cracked)
            {
                float a = Seg(x, y, 0.10f, 0.49f, 0.16f, 0.20f);
                float b = Seg(x, y, 0.16f, 0.20f, -0.09f, -0.02f);
                float e = Seg(x, y, -0.09f, -0.02f, -0.20f, -0.49f);
                float branch = Seg(x, y, 0.16f, 0.20f, 0.43f, 0.07f);
                float crack = Mathf.Min(Mathf.Min(a, b), Mathf.Min(e, branch));
                c = Over(c, new Color(0.02f, 0.31f, 0.51f, 0.86f), Cov(crack - 0.018f));
                c = Over(c, new Color(0.87f, 0.99f, 1f, 0.93f), Cov(crack - 0.006f));
            }
            c.a *= body;
            return c;
        }

        private static Color GoldRollPixel(float x, float y)
        {
            float d = RoundRect(x, y, 0.485f, 0.485f, 0.145f);
            float outer = Cov(d);
            if (outer <= 0f) return Color.clear;
            float diagonal = y - (0.73f * x + 0.13f - 0.23f * x * x);
            var teal = Color.Lerp(new Color(0.02f, 0.39f, 0.37f), new Color(0.12f, 0.76f, 0.72f),
                Mathf.Clamp01(0.67f - y * 0.64f - x * 0.55f));
            var gold = Color.Lerp(new Color(0.63f, 0.31f, 0.035f), new Color(1f, 0.78f, 0.22f),
                Mathf.Clamp01(0.56f + y * 0.88f - x * 0.42f));
            float flare = Mathf.Exp(-Mathf.Pow((x + y * 0.73f + 0.20f) * 8f, 2f));
            gold = Color.Lerp(gold, new Color(1f, 0.97f, 0.76f), flare * 0.89f);
            var color = Color.Lerp(teal, gold, Cov(-diagonal));
            float seam = Mathf.Exp(-Mathf.Abs(diagonal) * 62f);
            color = Color.Lerp(color, new Color(1f, 0.94f, 0.71f), seam * 0.95f);

            float sx = x - 0.15f, sy = y + 0.19f;
            float spiral = 1f;
            Vector2 previous = new Vector2(0.015f, 0f);
            for (int i = 1; i <= 27; i++)
            {
                float t = i / 27f;
                float a = -t * 8.5f + 0.4f;
                var point = (0.015f + 0.195f * t) * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                spiral = Mathf.Min(spiral, Seg(sx, sy, previous.x, previous.y, point.x, point.y));
                previous = point;
            }
            color = Color.Lerp(color, new Color(0.018f, 0.26f, 0.26f), Cov(spiral - 0.017f) * Cov(diagonal));
            float cream = 1f - Mathf.SmoothStep(0f, 1f, -d / 0.042f);
            color = Color.Lerp(color, new Color(0.99f, 0.88f, 0.68f), cream);
            color.a = outer;
            return color;
        }

        private static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay, wx = px - ax, wy = py - ay;
            float t = Mathf.Clamp01((wx * vx + wy * vy) / (vx * vx + vy * vy));
            float dx = wx - vx * t, dy = wy - vy * t;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static float RoundRect(float x, float y, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x) - hx + r, qy = Mathf.Abs(y) - hy + r;
            return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private delegate Color Pixel(float x, float y);

        private static Sprite Make(string name, Pixel f)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "special_" + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                // Merkez (0,0), kenarlar ±0.5.
                float x = (i + 0.5f) / N - 0.5f, y = (j + 0.5f) / N - 0.5f;
                px[j * N + i] = f(x, y);
            }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            sp.name = tex.name;
            return sp;
        }

        // ---- SDF yardımcıları (birim: doku genişliği) ---------------------------------------------------

        private const float Aa = 1.2f / N;
        private static float Cov(float d) => Mathf.Clamp01(0.5f - d / Aa);
        private static float Circle(float x, float y, float cx, float cy, float r) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        private static float Capsule(float x, float y, float halfLen, float r)
        {
            float qx = Mathf.Max(Mathf.Abs(x) - halfLen, 0f);
            return Mathf.Sqrt(qx * qx + y * y) - r;
        }

        private static Color Over(Color dst, Color src, float a)
        {
            a *= src.a;
            float outA = a + dst.a * (1f - a);
            if (outA <= 1e-5f) return Color.clear;
            var c = (src * a + dst * dst.a * (1f - a)) / outA;
            c.a = outA;
            return c;
        }

        // ---- Rulo roketi -----------------------------------------------------------------------------------

        private static readonly Color GoldTop = new Color32(0xFF, 0xDD, 0x6B, 0xFF);
        private static readonly Color GoldBottom = new Color32(0xF0, 0x9A, 0x22, 0xFF);
        private static readonly Color Rim = new Color32(0xA8, 0x5C, 0x0C, 0xFF);
        private static readonly Color Cream = new Color32(0xFF, 0xF4, 0xD6, 0xFF);

        private static Color RocketPixel(float x, float y)
        {
            const float half = 0.2f, r = 0.215f;
            var c = Color.clear;
            // Yumuşak gölge.
            c = Over(c, new Color(0.35f, 0.18f, 0f, 0.35f), Mathf.Clamp01(0.5f - Capsule(x, y + 0.035f, half, r + 0.01f) / 0.03f));
            float d = Capsule(x, y, half, r);
            // Kenar (koyu altın) ve gövde.
            c = Over(c, Rim, Cov(d));
            float inner = Cov(d + 0.022f);
            if (inner > 0f)
            {
                float t = Mathf.InverseLerp(r, -r, y);
                var body = Color.Lerp(GoldTop, GoldBottom, t);
                // Çapraz krem şeritler (sarılmış silikon).
                float s = Mathf.Repeat((x + y * 0.8f) * 9f, 1f);
                float stripe = Mathf.Clamp01(1f - Mathf.Abs(s - 0.5f) * 6f);
                body = Color.Lerp(body, Cream, stripe * 0.55f);
                c = Over(c, body, inner);
                // Üst parlama.
                float hl = Cov(Capsule(x + 0.02f, y - r * 0.5f, half * 0.8f, r * 0.22f));
                c = Over(c, new Color(1f, 1f, 1f, 0.55f), hl);
            }
            // Sağ uçta sarmal rulo kesiti.
            float cx = half + 0.02f;
            float dc = Circle(x, y, cx, 0f, r * 0.82f);
            c = Over(c, Rim, Cov(dc));
            if (dc < -0.012f)
            {
                float ang = Mathf.Atan2(y, x - cx);
                float rad = Mathf.Sqrt((x - cx) * (x - cx) + y * y);
                float spiral = Mathf.Repeat(rad * 26f - ang / (2f * Mathf.PI), 1f);
                var face = Color.Lerp(Cream, GoldTop, spiral < 0.5f ? 0f : 0.7f);
                c = Over(c, face, Cov(dc + 0.012f));
            }
            // Yön okları (iki uçta dışa bakan).
            for (int side = -1; side <= 1; side += 2)
            {
                float ax = x * side - 0.445f, ay = Mathf.Abs(y);
                // Üçgen: tepe dışarıda.
                float tri = Mathf.Max(ax - 0.04f, ay - (0.045f - ax) * 1.2f);
                tri = Mathf.Max(tri, -ax - 0.04f);
                c = Over(c, Color.white, Cov(tri));
            }
            return c;
        }

    }
}
