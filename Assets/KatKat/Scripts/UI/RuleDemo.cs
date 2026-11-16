using System.Collections.Generic;
using KatKat.Core;
using KatKat.View;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Kural kartındaki mini canlandırma (kullanıcı, 2026-11-16: simge kuralın "amacını anlatamıyor"). Oyunun kendi
    /// taşlarıyla 4–10 sn'lik döngü; dokunuş halkası, soyulma, sayaç ve hedef değişimi gösterilir. Durum zamandan
    /// hesaplanır (durumsuz): her karede t'ye göre ölçek, sayı ve renk yeniden kurulur.
    /// - exact: 2 hamle, 2 grup; son hamlede hedef dolar ("boşa hamle yok").
    /// - scarce: önce yanlış sıra (mor yetmez, kırmızı), sonra doğru sıra (önce maviyi kaz, mor birleşir, hedef dolar).
    /// - noSpecials: 5'li grup patlar, roket tepsiye uçar ama kilide çarpıp söner.
    /// </summary>
    public sealed class RuleDemo : MonoBehaviour
    {
        public const float Height = 236f;
        private static readonly string[] Order = { "noSpecials", "scarce", "exact" };

        /// <summary>Kartta canlandırılacak kural (öncelik sırasıyla ilki) ya da null.</summary>
        public static string Pick(IList<HudView.RuleLine> rules)
        {
            foreach (var k in Order)
                foreach (var r in rules)
                    if (r.Rule == k) return k;
            return null;
        }

        /// <summary>Yakalama için sabit an (sn); negatif → gerçek zaman.</summary>
        public static float Freeze = -1f;

        private string _rule;
        private float _start;
        private float _period;
        private const float Chip = 68f;
        private readonly List<Image> _tops = new List<Image>();
        private readonly List<Image> _unders = new List<Image>();
        private readonly List<Vector2> _pos = new List<Vector2>();
        private Image _ring, _goalIcon, _check, _pill, _rocket, _lock;
        private TMPro.TextMeshProUGUI _goalText, _pillText, _caption;

        public static RuleDemo Build(RectTransform parent, string rule, float width)
        {
            var root = UiFactory.Rect("RuleDemo", parent);
            root.sizeDelta = new Vector2(width, Height);
            var bg = UiFactory.Image("Bg", root, UiArt.Box, new Color(0.85f, 0.75f, 0.58f, 0.32f), true);
            bg.raycastTarget = false;
            UiFactory.Stretch(bg.rectTransform);
            var d = root.gameObject.AddComponent<RuleDemo>();
            d.Setup(rule);
            return d;
        }

        private static Sprite ChipOf(LayerColor c) => PremiumArt.Chip(c) ?? UiArt.Chip(c);

        private Image Img(string name, Sprite s, Vector2 pos, float size)
        {
            var i = UiFactory.Image(name, transform, s, Color.white);
            i.raycastTarget = false;
            i.preserveAspect = true;
            UiFactory.Place(i.rectTransform, new Vector2(0.5f, 0.5f), pos, Vector2.one * size);
            return i;
        }

        private TMPro.TextMeshProUGUI Txt(string name, Vector2 pos, Vector2 size, int fs, Color c)
        {
            var t = UiFactory.Text(name, transform, "", fs, c);
            t.raycastTarget = false;
            t.richText = true;
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
            return t;
        }

        private void Board(LayerColor[] tops, LayerColor?[] unders, float cx, float y, float step)
        {
            for (int i = 0; i < tops.Length; i++)
            {
                var p = new Vector2(cx + (i - (tops.Length - 1) * 0.5f) * step, y);
                _pos.Add(p);
                _unders.Add(unders != null && unders[i].HasValue ? Img("Under" + i, ChipOf(unders[i].Value), p, Chip) : null);
                _tops.Add(Img("Top" + i, ChipOf(tops[i]), p, Chip));
            }
        }

        private void Goal(LayerColor c, Vector2 pos)
        {
            _goalIcon = Img("GoalIcon", ChipOf(c), pos + new Vector2(0f, 14f), 54f);
            _goalText = Txt("GoalText", pos + new Vector2(0f, -40f), new Vector2(100f, 40f), 34, Ui3.Ink);
            _check = Img("Check", UiArt.Icon(UiIcon.Check), pos + new Vector2(0f, -40f), 44f);
            _check.color = new Color32(0x2E, 0xA0, 0x4E, 0xFF);
        }

        private void Setup(string rule)
        {
            _rule = rule;
            _start = Time.unscaledTime;
            const float by = -18f;
            switch (rule)
            {
                case "exact":
                    _period = 5f;
                    _pill = UiFactory.Image("Pill", transform, UiArt.Box, UiArt.HudTeal, true);
                    _pill.raycastTarget = false;
                    UiFactory.Place(_pill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-312f, by), new Vector2(104f, 84f));
                    _pillText = Txt("PillText", new Vector2(-312f, by), new Vector2(100f, 80f), 46, Color.white);
                    Board(new[] { LayerColor.Purple, LayerColor.Purple, LayerColor.Purple, LayerColor.Coral, LayerColor.Purple, LayerColor.Purple, LayerColor.Purple }, null, 0f, by, 68f); // v14: hap ile ilk taş üst üste biniyordu (eski -10, 72)
                    Goal(LayerColor.Purple, new Vector2(290f, by));
                    _caption = Txt("Caption", new Vector2(-312f, 74f), new Vector2(160f, 36f), 22, Ui3.InkSecondary);
                    Loc.Set(_caption, Loc.T("trial.demo.moves"));
                    break;
                case "scarce":
                    _period = 10f;
                    Board(new[] { LayerColor.Purple, LayerColor.Purple, LayerColor.Purple, LayerColor.Blue, LayerColor.Blue, LayerColor.Blue },
                          new LayerColor?[] { null, null, null, LayerColor.Purple, null, null }, -60f, by, 76f);
                    Goal(LayerColor.Purple, new Vector2(290f, by));
                    _caption = Txt("Caption", new Vector2(-60f, 80f), new Vector2(560f, 40f), 26, Ui3.Ink);
                    break;
                default: // noSpecials
                    _period = 4.2f;
                    Board(new[] { LayerColor.Purple, LayerColor.Purple, LayerColor.Purple, LayerColor.Purple, LayerColor.Purple }, null, -130f, by, 74f);
                    // Tepsi + altın kilit: kural simgesinin kendisi (kart satırıyla aynı görsel), büyük.
                    _lock = Img("Lock", HazardPaint.RuleNoSpecials, new Vector2(270f, by), 170f);
                    _rocket = Img("Rocket", SpecialArt.For(SpecialKind.RocketRow) ?? SpecialArt.Rocket, _pos[2], 150f);
                    break;
            }
            _ring = Img("Tap", UiArt.Icon(UiIcon.Ring), Vector2.zero, 90f);
            _ring.color = UiArt.Gold;
            _ring.transform.SetAsLastSibling();
            Update();
        }

        // ---- yardımcılar ----

        private static float Pop(float t, float at)
        {
            if (t < at) return 1f;
            float k = Mathf.Clamp01((t - at) / 0.28f);
            return k < 0.25f ? 1f + 0.8f * k : 1.2f * (1f - (k - 0.25f) / 0.75f);
        }

        private void SetPop(int i, float s) => _tops[i].rectTransform.localScale = Vector3.one * Mathf.Max(0f, s);

        private void Tap(float t, float at, int from, int to)
        {
            float u = (t - (at - 0.55f)) / 0.55f;
            bool on = u >= 0f && u <= 1.05f;
            _ring.enabled = on;
            if (!on) return;
            var c = (_pos[from] + _pos[to]) * 0.5f;
            _ring.rectTransform.anchoredPosition = c;
            float w = (_pos[to].x - _pos[from].x) + Chip + 24f;
            float pulse = 1.15f - 0.15f * Mathf.Clamp01(u);
            _ring.rectTransform.sizeDelta = new Vector2(w, Chip + 26f) * pulse;
            _ring.preserveAspect = false;
            _ring.type = Image.Type.Simple;
        }

        private void GoalState(int left, bool done, bool fail)
        {
            _check.enabled = done;
            _goalText.enabled = !done;
            _goalText.text = left.ToString();
            _goalText.color = fail ? new Color32(0xD8, 0x3A, 0x32, 0xFF) : (Color)Ui3.Ink;
            float s = done ? 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            _goalIcon.rectTransform.localScale = Vector3.one * s;
        }

        private void Update()
        {
            float t = Freeze >= 0f ? Freeze : (Time.unscaledTime - _start) % _period;
            switch (_rule)
            {
                case "exact": Exact(t); break;
                case "scarce": Scarce(t); break;
                default: NoSpecials(t); break;
            }
        }

        private void Exact(float t)
        {
            const float a = 1.1f, b = 2.5f;
            for (int i = 0; i < 3; i++) SetPop(i, Pop(t, a));
            SetPop(3, 1f);
            for (int i = 4; i < 7; i++) SetPop(i, Pop(t, b));
            if (t < a) Tap(t, a, 0, 2); else Tap(t, b, 4, 6);
            int moves = t < a ? 2 : t < b ? 1 : 0;
            _pillText.text = moves.ToString();
            bool done = t >= b + 0.3f;
            _pill.color = done ? new Color32(0x2E, 0xA0, 0x4E, 0xFF) : (Color)UiArt.HudTeal;
            GoalState(t < a ? 6 : t < b ? 3 : 0, done, false);
        }

        private void Scarce(float t)
        {
            bool right = t >= 5f;
            float u = right ? t - 5f : t;
            Loc.Set(_caption, right ? Loc.T("trial.demo.right") : Loc.T("trial.demo.wrong"));
            _caption.color = right ? new Color32(0x2E, 0xA0, 0x4E, 0xFF) : new Color32(0xD8, 0x3A, 0x32, 0xFF);
            var under = _unders[3];
            under.rectTransform.anchoredPosition = _pos[3];
            under.rectTransform.localScale = Vector3.one;
            if (!right)
            {
                const float p = 1.1f, q = 2.4f;
                for (int i = 0; i < 3; i++) SetPop(i, Pop(u, p));
                for (int i = 3; i < 6; i++) SetPop(i, Pop(u, q));
                if (u < p) Tap(u, p, 0, 2); else Tap(u, q, 3, 5);
                bool fail = u >= q + 0.35f;
                if (fail) under.rectTransform.anchoredPosition = _pos[3] + new Vector2(Mathf.Sin(u * 40f) * 5f, 0f);
                GoalState(u < p ? 4 : 1, false, fail);
            }
            else
            {
                const float p = 1.1f, q = 2.5f;
                for (int i = 3; i < 6; i++) SetPop(i, Pop(u, p));
                for (int i = 0; i < 3; i++) SetPop(i, Pop(u, q));
                under.rectTransform.localScale = Vector3.one * Pop(u, q);
                if (u < p) Tap(u, p, 3, 5); else Tap(u, q, 0, 3);
                GoalState(u < q ? 4 : 0, u >= q + 0.3f, false);
            }
        }

        private void NoSpecials(float t)
        {
            const float a = 1.1f, fly = 0.65f;
            for (int i = 0; i < 5; i++) SetPop(i, Pop(t, a));
            Tap(t, a, 0, 4);
            float r = t - (a + 0.2f);
            var target = _lock.rectTransform.anchoredPosition - new Vector2(70f, 0f);
            _rocket.enabled = r >= 0f && r < fly + 0.5f;
            if (_rocket.enabled)
            {
                float k = Mathf.Clamp01(r / fly);
                var p = Vector2.Lerp(_pos[2], target, k * k * (3f - 2f * k)) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 50f);
                float fade = r > fly ? 1f - (r - fly) / 0.5f : 1f;
                if (r > fly) p += new Vector2(-30f * (r - fly), -40f * (r - fly));
                _rocket.rectTransform.anchoredPosition = p;
                _rocket.rectTransform.localScale = Vector3.one * (0.6f + 0.4f * Mathf.Min(1f, k * 3f)) * Mathf.Max(0.3f, fade);
                _rocket.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fade));
            }
            float sh = r - fly;
            _lock.rectTransform.localEulerAngles = new Vector3(0f, 0f, sh >= 0f && sh < 0.5f ? Mathf.Sin(sh * 45f) * 14f * (1f - sh / 0.5f) : 0f);
        }
    }
}
