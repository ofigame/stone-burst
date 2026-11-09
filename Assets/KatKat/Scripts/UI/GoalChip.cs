using KatKat.Core;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// Tek hedef göstergesi: renk/şekil taşı + "ilerleme/miktar"; tamamlanınca yeşil onay rozeti.
    /// Uçuş vardığında kısa parlama (dört uçlu ışık + hale). Sayı gerçek metindir.
    /// </summary>
    public sealed class GoalChip
    {
        public const float FlashMs = 360f;

        public RectTransform Root;
        public RectTransform Icon;
        public GoalState Goal;
        private Text _count;
        private RectTransform _check;
        private Image _sparkle, _glow;
        private float _pulse;
        private float _flash = 1f;

        public static GoalChip Create(RectTransform parent, GoalState goal, bool compact = false, bool divider = false)
        {
            var c = new GoalChip { Goal = goal.Clone() };
            c.Root = UiFactory.Rect("Goal", parent);
            // 200×84: hap (100) içinde üst/alt 8 pay; sayı "14/14" 42 punto ~105 genişlik, alanı 116.
            c.Root.sizeDelta = new Vector2(200f, 84f);
            if (divider)
            {
                var rule = UiFactory.Image("Divider", c.Root, UiArt.Box, new Color(0.64f, 0.37f, 0.19f, 0.25f));
                UiFactory.Place(rule.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3f, 65f));
            }
            var sprite = IconFor(goal);

            c._glow = UiFactory.Image("Glow", c.Root, PremiumArt.Get("fx_glow") ?? UiArt.Circle, new Color(1f, 0.95f, 0.75f, 0f));
            UiFactory.Place(c._glow.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(150f, 150f));
            var icon = UiFactory.Image("Chip", c.Root, sprite, Color.white);
            icon.preserveAspect = true;
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(72f, 72f));
            c.Icon = icon.rectTransform;
            c._sparkle = UiFactory.Image("Sparkle", c.Root, PremiumArt.Get("fx_sparkle") ?? UiArt.Icon(UiIcon.Ring), new Color(1f, 1f, 1f, 0f));
            UiFactory.Place(c._sparkle.rectTransform, new Vector2(0f, 0.5f), new Vector2(64f, 22f), new Vector2(60f, 60f));

            c._count = UiFactory.Text("Count", c.Root, "", 42, UiArt.Brown, TextAnchor.MiddleLeft);
            UiFactory.Place(c._count.rectTransform, new Vector2(0f, 0.5f), new Vector2(142f, 0f), new Vector2(116f, 72f));
            c._count.fontSizeMax = 42;
            c._count.fontSizeMin = 26;
            c._count.enableAutoSizing = true;

            // Onay rozeti: yeşil daire + beyaz işaret, taşın sağ altında (taşın içinde kalır, hapı taşmaz).
            var badge = UiFactory.Image("Check", icon.transform, UiArt.Circle, UiArt.CheckGreen);
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 0f), new Vector2(-6f, 10f), new Vector2(36f, 36f));
            var ring = badge.gameObject.AddComponent<Outline>();
            ring.effectColor = Color.white;
            ring.effectDistance = new Vector2(2f, -2f);
            var mark = UiFactory.Image("Mark", badge.transform, UiArt.Icon(UiIcon.Check), Color.white);
            UiFactory.Stretch(mark.rectTransform, 3, 3, 3, 3);
            c._check = badge.rectTransform;
            AddChainMark(goal, icon);
            if (compact)
            {
                c.Root.sizeDelta = new Vector2(110f, 88f);
                UiFactory.Place(c._glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 13f), Vector2.one * 93f);
                UiFactory.Place(c._sparkle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(18f, 29f), Vector2.one * 43f);
                UiFactory.Place(c.Icon, new Vector2(0.5f, 0.5f), new Vector2(0f, 13f), Vector2.one * 55f);
                UiFactory.Place(c._count.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -31f), new Vector2(90f, 30f));
                c._count.alignment = TMPro.TextAlignmentOptions.Center;
                c._count.fontSizeMax = 22;
                c._count.fontSizeMin = 15;
                c._check.localScale = Vector3.one * 0.72f;
            }
            c.Set(goal.Progress, false);
            return c;
        }

        /// <summary>
        /// v6 oyun üstü hedef çipi (kullanıcı referansı): açık krem hap, solda simge, sağda koyu turkuaz "12/48".
        /// Tamamlanınca simgede yeşil onay. Uçuş hedefi Icon'dur.
        /// </summary>
        public static GoalChip CreatePill(RectTransform parent, GoalState goal, Vector2 size)
        {
            var c = new GoalChip { Goal = goal.Clone() };
            c.Root = UiFactory.Rect("Goal", parent);
            c.Root.sizeDelta = size;
            var bg = UiFactory.Image("Bg", c.Root, UiArt.Box, new Color32(0xFF, 0xF7, 0xEA, 0xFF), true);
            bg.pixelsPerUnitMultiplier = UiArt.Box.border.x / (size.y * 0.5f);
            UiFactory.Stretch(bg.rectTransform);
            var o = bg.gameObject.AddComponent<Outline>();
            o.effectColor = new Color32(0xE2, 0xC3, 0x95, 0xFF);
            o.effectDistance = new Vector2(2f, -2f);
            var sh = bg.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.45f, 0.3f, 0.12f, 0.18f);
            sh.effectDistance = new Vector2(0f, -3f);
            if (Ui3.Ready)
            {
                // ui3 goalChip: ivoryTop→ivoryBottom, yarıçap yüksekliğin yarısı, 2 px kum kontur; gölge yok.
                bg.sprite = Ui3.Rounded(size.y * 0.5f, Ui3.IvoryTop, Ui3.IvoryBottom, 2f, Ui3.Sand, 0f, default, Mathf.RoundToInt(size.y));
                bg.color = Color.white;
                bg.pixelsPerUnitMultiplier = 1f;
                o.enabled = false;
                sh.enabled = false;
            }

            float iconSize = Ui1.Ready ? size.y - 16f : Mathf.Min(66f, size.y - 14f); // ui1/12–13: simge ~78
            float iconX = 14f + iconSize * 0.5f;
            var sprite = IconFor(goal);
            c._glow = UiFactory.Image("Glow", c.Root, PremiumArt.Get("fx_glow") ?? UiArt.Circle, new Color(1f, 0.95f, 0.75f, 0f));
            UiFactory.Place(c._glow.rectTransform, new Vector2(0f, 0.5f), new Vector2(iconX, 0f), Vector2.one * (iconSize * 1.7f));
            var icon = UiFactory.Image("Chip", c.Root, sprite, Color.white);
            icon.preserveAspect = true;
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(iconX, 0f), Vector2.one * iconSize);
            c.Icon = icon.rectTransform;
            c._sparkle = UiFactory.Image("Sparkle", c.Root, PremiumArt.Get("fx_sparkle") ?? UiArt.Icon(UiIcon.Ring), new Color(1f, 1f, 1f, 0f));
            UiFactory.Place(c._sparkle.rectTransform, new Vector2(0f, 0.5f), new Vector2(iconX + 20f, 20f), new Vector2(46f, 46f));

            float textX = iconX + iconSize * 0.5f + 6f;
            c._count = UiFactory.Text("Count", c.Root, "", Ui3.Ready ? 38 : Ui1.Ready ? 40 : 32, Ui1.Ready ? (Color32)Ui1.Navy : new Color32(0x1A, 0x4E, 0x4B, 0xFF), TextAnchor.MiddleLeft);
            UiFactory.Place(c._count.rectTransform, new Vector2(0f, 0.5f), new Vector2(textX + (size.x - textX - 8f) * 0.5f, 0f), new Vector2(size.x - textX - 8f, 50f));
            c._count.fontSizeMax = Ui3.Ready ? 38 : Ui1.Ready ? 40 : 32;
            c._count.fontSizeMin = 18;
            c._count.enableAutoSizing = true;

            var badge = UiFactory.Image("Check", icon.transform, UiArt.Circle, UiArt.CheckGreen);
            UiFactory.Place(badge.rectTransform, new Vector2(1f, 0f), new Vector2(-4f, 6f), new Vector2(28f, 28f));
            var ring = badge.gameObject.AddComponent<Outline>();
            ring.effectColor = Color.white;
            ring.effectDistance = new Vector2(2f, -2f);
            var mark = UiFactory.Image("Mark", badge.transform, UiArt.Icon(UiIcon.Check), Color.white);
            UiFactory.Stretch(mark.rectTransform, 3, 3, 3, 3);
            c._check = badge.rectTransform;
            AddChainMark(goal, icon);
            c.Set(goal.Progress, false);
            return c;
        }

        /// <summary>v12 deneme: zincir hedefinde (yalnız zincirle soyulanlar sayılır) renk taşının sol üstünde zincir işareti.</summary>
        private static void AddChainMark(GoalState goal, Image icon)
        {
            if (goal.Type != GoalType.CollectChain) return;
            var chain = UiFactory.Image("Chain", icon.transform, RuleIcons.For("chainOnly"), Color.white);
            chain.raycastTarget = false;
            UiFactory.Place(chain.rectTransform, new Vector2(0f, 1f), new Vector2(6f, -6f), new Vector2(34f, 34f));
        }

        /// <summary>Hedef simgesi: renk taşı, yıldız ya da zarf (HUD, zafer kartı ve uçuş hedefi aynı simge).</summary>
        public static Sprite IconFor(GoalState goal)
        {
            switch (goal.Type)
            {
                case GoalType.CollectStars: return PremiumArt.Get("icon_star") ?? ProceduralArt.StarSprite;
                case GoalType.CollectEnvelopes: return TileSkin.Sprite("mechanics/envelope-closed") ?? PremiumArt.Get("ov_envelope") ?? ProceduralArt.StarSprite;
                case GoalType.CollectSponges: return SpecialArt.SpongeGoal;
                default: return PremiumArt.Chip(goal.Color) ?? UiArt.Chip(goal.Color);
            }
        }

        public void Set(int progress, bool pulse)
        {
            int shown = Mathf.Min(progress, Goal.Amount);
            _count.text = shown + "/" + Goal.Amount;
            _check.gameObject.SetActive(progress >= Goal.Amount);
            if (pulse)
            {
                _pulse = 1f;
                _flash = 0f;
            }
        }

        public void Tick(float dt)
        {
            if (_pulse > 0f)
            {
                _pulse = Mathf.Max(0f, _pulse - dt * 4f);
                float s = 1f + 0.22f * Mathf.Sin(_pulse * Mathf.PI);
                Icon.localScale = new Vector3(s, s, 1f);
            }
            if (_flash < 1f)
            {
                _flash = Mathf.Min(1f, _flash + dt * 1000f / FlashMs);
                float a = Mathf.Sin(_flash * Mathf.PI);
                _sparkle.color = new Color(1f, 1f, 1f, a);
                float s = Mathf.Lerp(0.4f, 1.2f, _flash);
                _sparkle.rectTransform.localScale = new Vector3(s, s, 1f);
                _sparkle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 40f * _flash);
                _glow.color = new Color(1f, 0.95f, 0.75f, a * 0.8f);
            }
        }
    }
}
