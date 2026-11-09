using System;
using KatKat.Meta;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace KatKat.UI
{
    /// <summary>
    /// Kalp göstergesi ve kalp kartı (kullanıcı, 2026-11-09: "Kalp içinde güzel kalın bir fontla canın rakamı yazsın
    /// yanında da 5 taneyse dolu, 5'ten az ise yarım saate bir +1 eklenecek ve yanında o geri sayım olacak. Ana sayfada
    /// logo ile yıldız arasındaki boşluğa koyalım. Reklam izleyerek 5 kalbi fullesin").
    /// </summary>
    public sealed partial class MenuView
    {
        public struct HeartsModel
        {
            public int Count, Max, SecondsToNext;
            public bool Full => Count >= Max;
        }

        /// <summary>Güncel kalp durumu (GameController); null ise gösterge çizilmez.</summary>
        public Func<HeartsModel> HeartsInfo;
        public Action HeartsPressed;

        private RectTransform _heartsCard;
        public bool IsHeartsCardShown => _heartsCard != null;

        /// <summary>Ana sayfa üst yüzeyinde kalp kapsülü (x 372–676, y 56–144).</summary>
        private void BuildHeartsWidget(RectTransform shell)
        {
            if (HeartsInfo == null) return;
            var cap = U3Plate("HeartsCapsule", shell, 44f, 88f);
            U3Place(cap.rectTransform, 384f, 56f, 296f, 88f);
            var btn = cap.gameObject.AddComponent<Button>();
            btn.targetGraphic = cap;
            cap.raycastTarget = true;
            btn.onClick.AddListener(() => HeartsPressed?.Invoke());
            PressFeedback.Attach(btn, null, 70f, 110f);
            var heart = UiFactory.Image("Heart", cap.transform, HeartSprite(), Color.white);
            heart.preserveAspect = true;
            heart.raycastTarget = false;
            UiFactory.Place(heart.rectTransform, new Vector2(0f, 0.5f), new Vector2(50f, 2f), new Vector2(108f, 100f));
            var n = HeartNumber(heart.transform, 50);
            var t = U3Text("State", cap.transform, "", 38, true, Ui3.Ink);
            UiFactory.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(194f, 0f), new Vector2(176f, 62f));
            var tick = cap.gameObject.AddComponent<HeartTicker>();
            tick.Info = HeartsInfo;
            tick.Number = n;
            tick.State = t;
            tick.Refresh();
        }

        /// <summary>Kalbin içindeki kalın, beyaz, koyu kırmızı konturlu sayı.</summary>
        private static Text HeartNumber(Transform heart, int size)
        {
            var n = UiFactory.Text("Count", heart, "5", size, Color.white);
            UiFactory.Place(n.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(90f, 80f));
            n.outlineWidth = 0.28f;
            n.outlineColor = new Color32(0x7A, 0x0E, 0x22, 0xFF);
            n.raycastTarget = false;
            return n;
        }

        /// <summary>Kalp kartı: büyük kalp ve sayı, sonraki kalbe kalan süre, reklamla 5 kalp, Kapat.</summary>
        public void ShowHeartsCard(Action watchAd)
        {
            CloseHeartsCard();
            if (HeartsInfo == null) return;
            var m = HeartsInfo();
            const float W = 860f, H = 760f;
            var card = U3Modal(_page, "HeartsCard", W, H, CloseHeartsCard, out var root);
            _heartsCard = root;
            var title = U3Text("Title", card, Loc.T(m.Count == 0 ? "hearts.empty" : "hearts.title"), 54, true, Ui3.Ink);
            U3In(title.rectTransform, W, 56f, 52f, 748f, 80f);
            FitText(title, 34);
            var heart = UiFactory.Image("Heart", card, HeartSprite(), Color.white);
            heart.preserveAspect = true;
            heart.raycastTarget = false;
            U3In(heart.rectTransform, W, 320f, 148f, 220f, 200f);
            var n = HeartNumber(heart.transform, 96);
            n.rectTransform.sizeDelta = new Vector2(180f, 140f);
            var state = U3Text("State", card, "", 40, true, Ui3.Ink);
            U3In(state.rectTransform, W, 56f, 364f, 748f, 60f);
            var info = U3Text("Info", card, Loc.T("hearts.info"), 30, false, Ui3.InkSecondary);
            info.textWrappingMode = TMPro.TextWrappingModes.Normal;
            U3In(info.rectTransform, W, 60f, 420f, 740f, 112f);
            FitText(info, 22);
            var tick = card.gameObject.AddComponent<HeartTicker>();
            tick.Info = HeartsInfo;
            tick.Number = n;
            tick.State = state;
            tick.Long = true;
            tick.Refresh();
            if (!m.Full && watchAd != null)
                AdButton("Refill", card, Loc.F("hearts.refill", m.Max), new Vector2(0.5f, 1f), new Vector2(0f, -(540f + 62f)), new Vector2(748f, 124f), watchAd);
            Ui1.Button("Close", card, Loc.T("ui.close"), m.Full || watchAd == null, new Vector2(0.5f, 1f), new Vector2(0f, -(m.Full ? 560f : 672f) - 50f), new Vector2(748f, 100f), CloseHeartsCard);
        }

        public void CloseHeartsCard()
        {
            if (_heartsCard != null) UiFactory.Kill(_heartsCard.gameObject);
            _heartsCard = null;
        }

        /// <summary>Reklam düğmesi (2026-11-09: bütün reklam düğmelerinde oynat simgesi, biraz daha yüksek).</summary>
        internal static Button AdButton(string name, Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, Action act) =>
            Ui1.Button(name, parent, text, true, anchor, pos, size, act, UiArt.Icon(UiIcon.Play), 44);

        // ---- Kalp görseli ----------------------------------------------------------------------------------

        private static Sprite _heart;

        /// <summary>Koddan kalp: kırmızı dikey geçiş, koyu kontur, sol üstte parlama (256 px, yumuşatılmış kenar).</summary>
        public static Sprite HeartSprite()
        {
            if (_heart) return _heart;
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "heart", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[S * S];
            Color top = new Color32(0xFF, 0x6B, 0x7E, 0xFF), bottom = new Color32(0xD9, 0x1E, 0x3F, 0xFF), rim = new Color32(0x8E, 0x10, 0x26, 0xFF);
            // Kalp eğrisi (x²+y²−1)³ − x²y³ ≤ 0; alan [-1.25,1.25] × [-1.1,1.35]. Kenar uzaklığı örneklemeyle.
            float F(float x, float y) { float a = x * x + y * y - 1f; return a * a * a - x * x * y * y * y; }
            const int SS = 3;
            for (int py = 0; py < S; py++)
            for (int pxi = 0; pxi < S; pxi++)
            {
                int inside = 0, inner = 0;
                for (int sy = 0; sy < SS; sy++)
                for (int sx = 0; sx < SS; sx++)
                {
                    float u = (pxi + (sx + 0.5f) / SS) / S, v = (py + (sy + 0.5f) / SS) / S;
                    float x = (u - 0.5f) * 2.5f, y = v * 2.45f - 1.12f;
                    if (F(x, y) <= 0f) inside++;
                    float xi = x * 1.11f, yi = (y - 0.05f) * 1.11f; // içte küçültülmüş kalp → kontur kalınlığı
                    if (F(xi, yi) <= 0f) inner++;
                }
                if (inside == 0) { px[py * S + pxi] = new Color32(0, 0, 0, 0); continue; }
                float t = py / (float)S;
                Color fill = Color.Lerp(bottom, top, t);
                // Parlama: sol üstte yumuşak açık elips.
                float hx = (pxi / (float)S - 0.32f) / 0.13f, hy = (py / (float)S - 0.72f) / 0.08f;
                float h = Mathf.Clamp01(1f - (hx * hx + hy * hy));
                fill = Color.Lerp(fill, Color.white, h * 0.55f);
                Color c = Color.Lerp(rim, fill, inner / (float)(SS * SS));
                c.a = inside / (float)(SS * SS);
                px[py * S + pxi] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _heart = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
            _heart.hideFlags = HideFlags.DontSave;
            return _heart;
        }

        /// <summary>Saniyede iki kez sayı ve "Dolu"/geri sayım yazısını günceller.</summary>
        private sealed class HeartTicker : MonoBehaviour
        {
            public Func<HeartsModel> Info;
            public Text Number, State;
            public bool Long;
            private float _next;

            private void Update()
            {
                if (Time.unscaledTime < _next) return;
                _next = Time.unscaledTime + 0.5f;
                Refresh();
            }

            public void Refresh()
            {
                if (Info == null) return;
                var m = Info();
                if (Number != null) Loc.Set(Number, m.Count.ToString());
                if (State == null) return;
                string s = m.Full ? Loc.T(Long ? "hearts.fullnote" : "hearts.full")
                    : Long ? Loc.F("hearts.next", Hearts.Clock(m.SecondsToNext)) : Hearts.Clock(m.SecondsToNext);
                Loc.Set(State, s);
            }
        }
    }
}
