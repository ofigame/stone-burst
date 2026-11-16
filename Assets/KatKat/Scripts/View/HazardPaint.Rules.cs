using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Kural simgeleri (kullanıcı, 2026-11-16: hedef tahtası / tek mücevher / yasaklı roket "amacını anlatamıyor"). Simge
    /// oyunun kendi parçalarından kurulur: hamle sayacı, taşlar, tepsi. Aynı parlak kabartmalı çizici. Tripo parçaları
    /// gelince (boş rozet, altın onay, altın çerçeve, asma kilit) bu çizimlerin yerine birleştirilebilir.
    /// </summary>
    public static partial class HazardPaint
    {
        private static Sprite _ruleExact, _ruleScarce, _ruleNoSpecials;

        private static Sd Box(float cx, float cy, float hx, float hy, float r) => (x, y) =>
        {
            float qx = Mathf.Abs(x - cx) - hx + r, qy = Mathf.Abs(y - cy) - hy + r;
            return Len(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        };

        private static Sd Ring(Sd s, float w) => (x, y) => Mathf.Abs(s(x, y)) - w;

        private static readonly Color Gold = Hex(0xF2B22E), Teal = Hex(0x079E9B), Purple = Hex(0x9B5BD6), Wood = Hex(0xC98C4E), Ink = Hex(0x122E6E);

        /// <summary>Dar hamle: hamle sayacı "0" + altın onay ("son hamlede bitir, fazlası yok").</summary>
        public static Sprite RuleExact => _ruleExact ? _ruleExact : _ruleExact = Render("rule-exact",
            P(Box(-0.04f, 0.06f, 0.36f, 0.22f, 0.20f), Teal, 0.08f, 0.7f),
            P(Box(-0.04f, 0.08f, 0.29f, 0.15f, 0.14f), Hex(0x0BB8B4), 0.03f, 0.3f),
            P(Ring(Ellipse(-0.04f, 0.07f, 0.075f, 0.11f), 0.032f), Color.white, 0.03f, 0.5f),
            P(Circle(0.25f, -0.20f, 0.16f), Gold, 0.07f, 0.9f),
            P(Union(Capsule(0.17f, -0.20f, 0.23f, -0.27f, 0.028f), Capsule(0.23f, -0.27f, 0.34f, -0.13f, 0.028f)), Color.white, 0.02f, 0.3f));

        /// <summary>Kıt renk: altın çerçeve içinde 3 taş ("hepsi bu") ve kırmızı eksi rozeti ("harcanan geri gelmez").</summary>
        public static Sprite RuleScarce => _ruleScarce ? _ruleScarce : _ruleScarce = Render("rule-scarce",
            P(Ring(Box(0f, -0.02f, 0.42f, 0.18f, 0.08f), 0.024f), Gold, 0.03f, 0.9f),
            P(Box(-0.25f, -0.02f, 0.105f, 0.105f, 0.045f), Purple, 0.06f, 0.8f),
            P(Box(0f, -0.02f, 0.105f, 0.105f, 0.045f), Purple, 0.06f, 0.8f),
            P(Box(0.25f, -0.02f, 0.105f, 0.105f, 0.045f), Purple, 0.06f, 0.8f),
            P(Circle(0.33f, 0.24f, 0.12f), Hex(0xE0453E), 0.06f, 0.8f),
            P(Capsule(0.27f, 0.24f, 0.39f, 0.24f, 0.026f), Color.white, 0.02f, 0.3f));

        /// <summary>Özel taş yok: oyunun tepsisi ve üstünde altın asma kilit ("yardım taşı gelmez").</summary>
        public static Sprite RuleNoSpecials => _ruleNoSpecials ? _ruleNoSpecials : _ruleNoSpecials = Render("rule-nospecials",
            P(Box(0f, -0.20f, 0.43f, 0.13f, 0.08f), Wood, 0.07f, 0.6f),
            P(Box(0f, -0.17f, 0.35f, 0.06f, 0.05f), Hex(0x7A4C27), 0.03f, 0.2f),
            P((x, y) => Mathf.Max(Mathf.Abs(Circle(0f, 0.12f, 0.10f)(x, y)) - 0.035f, 0.10f - y), Hex(0xB9BCC6), 0.04f, 0.9f),
            P(Box(0f, 0.05f, 0.16f, 0.13f, 0.05f), Gold, 0.07f, 0.9f),
            P(Union(Circle(0f, 0.08f, 0.032f), Capsule(0f, 0.07f, 0f, 0.0f, 0.016f)), Ink, 0.01f, 0.2f));
    }
}
