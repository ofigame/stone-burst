using KatKat.UI;
using KatKat.View;
using NUnit.Framework;
using UnityEngine;

namespace KatKat.Tests
{
    /// <summary>VR2 oyun ekranı: tema eşlemesi ve tahta iç alanı (docs/vr2/CLAUDE_CODE.md "Kabul kontrolü").</summary>
    public class Vr2Tests
    {
        [Test]
        public void SkinFollowsRegionThemeOrder()
        {
            // Bölge adları: 0 Mavi Liman, 1 Yeşil Vadi, 2 Kum Vadisi, 3 Fener Koyu, 5 Vaha Yolu, 6 İnci Sahili.
            int[] expected = { 0, 1, 2, 3, 1, 4, 5, 1, 2, 0 };
            for (int r = 0; r < expected.Length; r++)
            {
                Assert.AreEqual(expected[r], Vr2.SkinFor(r * 100), "bölge " + r);
                Assert.AreEqual(expected[r], Vr2.SkinFor(r * 100 + 99), "bölge sonu " + r);
            }
        }

        [Test]
        public void PaletteMatchesPaletJson()
        {
            string[] floors = { "3FC9BB", "78C889", "F7AE40", "2B4E62", "D68156", "B7B3DB" };
            for (int i = 0; i < floors.Length; i++)
                Assert.AreEqual(floors[i], ColorUtility.ToHtmlStringRGB(Vr2.Skins[i].Floor), Vr2.Skins[i].Name);
        }

        [Test]
        public void EmptySlotIsVisiblyDarkerThanFloor()
        {
            for (int i = 0; i < Vr2.Skins.Length; i++)
            {
                Color f = Vr2.InnerFloor(i), s = Vr2.EmptySlot(i);
                Assert.AreEqual(1f, s.a, 0.01f, Vr2.Skins[i].Name + " opak yuva");
                Assert.Less(s.r + s.g + s.b, f.r + f.g + f.b - 0.1f, Vr2.Skins[i].Name + " yuva iç alandan koyu");
            }
        }

        [Test]
        public void InnerFloorIsLighterToneOfThemeColor()
        {
            for (int i = 0; i < Vr2.Skins.Length; i++)
            {
                Color f = Vr2.Skins[i].Floor, inner = Vr2.InnerFloor(i);
                Color.RGBToHSV(f, out float h0, out _, out float v0);
                Color.RGBToHSV(inner, out float h1, out _, out float v1);
                Assert.Greater(inner.r + inner.g + inner.b, f.r + f.g + f.b + 0.2f, Vr2.Skins[i].Name + " açık ton");
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(h0 * 360f, h1 * 360f)), 4f, Vr2.Skins[i].Name + " aynı renk tonu");
                Assert.GreaterOrEqual(v1, v0, Vr2.Skins[i].Name);
            }
        }

        // 4×3 merdiven + pasif hücre:  111 / 101 / 1111 değil — 4 sütun: "1110", "1010", "1111".
        private static readonly string[] Rows = { "1110", "1010", "1111" };

        private static bool[] Mask()
        {
            var m = new bool[12];
            for (int y = 0; y < 3; y++) for (int x = 0; x < 4; x++) m[y * 4 + x] = Rows[y][x] == '1';
            return m;
        }

        /// <summary>(cx, cy) hücre koordinatı (y aşağı) → doku pikseli (Unity: alt satır önce).</summary>
        private static Color32 At(Color32[] px, int tw, int th, int res, int pad, float cx, float cy)
        {
            int x = Mathf.FloorToInt(pad + cx * res), y = Mathf.FloorToInt(pad + cy * res);
            return px[(th - 1 - y) * tw + x];
        }

        private static float Lum(Color32 c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        [Test]
        public void FloorIsFlatThemeColorWithInsetShadowOnly()
        {
            const int res = 64, pad = 60;
            var floor = Vr2.Skins[0].Floor;
            var px = Vr2Floor.Bake(Mask(), 4, 3, res, pad, floor, out int tw, out int th);

            // Kenardan uzak iç nokta: düz tema rengi (gölge 0,30 hücre bandı + 3σ dışında).
            var mid = At(px, tw, th, res, pad, 0.5f, 2.6f);
            Assert.AreEqual(255, mid.a);
            Assert.LessOrEqual(Mathf.Abs(mid.r - floor.r) + Mathf.Abs(mid.g - floor.g) + Mathf.Abs(mid.b - floor.b), 6, "iç alan tek renk");

            // Pasif hücre (1,1) ve tahta dışı (3,0): tamamen saydam → arka plan görünür.
            Assert.AreEqual(0, At(px, tw, th, res, pad, 1.5f, 1.5f).a, "pasif hücre");
            Assert.AreEqual(0, At(px, tw, th, res, pad, 3.85f, 0.3f).a, "maske dışı (payın ötesi)");
            // Kenar payı (Vr2.Inset): hücre kenarının dışında da iç alan var (taşlar kenara değmez).
            float m = Vr2.Inset;
            Assert.AreEqual(255, At(px, tw, th, res, pad, 0.5f, -m * 0.5f).a, "üstte iç pay");
            Assert.AreEqual(255, At(px, tw, th, res, pad, -m * 0.5f, 2.5f).a, "solda iç pay");
            // Dış çerçeve/gölge yok: payın hemen dışı (0,04 hücre) saydam.
            Assert.AreEqual(0, At(px, tw, th, res, pad, 2.0f, -m - 0.04f).a, "üstte dış gölge/çerçeve yok");
            Assert.AreEqual(0, At(px, tw, th, res, pad, 2.0f, 3f + m + 0.04f).a, "altta dış gölge yok");

            // Köşe yuvarlak (0,22 hücre): payla genişlemiş dış köşenin tam ucu saydam.
            Assert.AreEqual(0, At(px, tw, th, res, pad, -m + 0.02f, -m + 0.02f).a, "dış köşe yuvarlak");

            // Gölge aşağı kayık: üst iç kenar alt iç kenardan belirgin koyu; alt kenar iç renkten açık değil.
            float top = Lum(At(px, tw, th, res, pad, 0.5f, -m + 0.06f));
            float bottom = Lum(At(px, tw, th, res, pad, 0.5f, 3f + m - 0.06f));
            float flat = Lum(floor);
            Assert.Less(top, bottom - 5f, "üst iç kenar daha koyu (yumuşak geçiş, yön korunur)");
            Assert.Less(bottom, flat + 0.5f, "alt kenar iç renkten açık değil (parlak kenar yok)");
        }
    }
}
