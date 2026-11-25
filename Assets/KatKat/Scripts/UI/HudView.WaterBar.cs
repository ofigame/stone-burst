using System;
using KatKat.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Su kovası arayüzü (2026-11-26, kullanıcı: "görev çubuğunda kovayı suyla doldur kaldıralım. Onun yerine kova doldukça gönder
    /// diyelim ve görev çubuğu yatayda su dolan progress bar gibi olsun").
    /// <para>Görev çubuğu: koyu kanal içinde soldan sağa dolan su (derin mavi → açık turkuaz), dolum ucunda dalgalı köpük, üstünde
    /// kayan parıltı, ortada yüzde. Kova gönderilince su çubuğa varır, çubuk sayarak dolar.</para>
    /// <para>"Gönder" düğmesi kovanın önünde (tahta dünyasından kanvasa her karede yerleştirilir): kovada su varken görünür, kova
    /// dolunca nabız atar ve parlar. Basınca <see cref="WaterSendPressed"/>.</para>
    /// </summary>
    public sealed partial class HudView
    {
        public event Action WaterSendPressed;
        /// <summary>Düğmenin duracağı dünya noktası (kovanın önü) ve kovanın durumu (0 boş, 1 su var, 2 dolu).</summary>
        public Func<Vector3> WaterSendAnchor;
        public Func<int> WaterSendState;
        /// <summary>Kovanın dünya alanı (alt-sol, üst-sağ): kovaya dokunmak da gönderir (kullanıcı 2026-11-26: "su kovasında kovaya bastığımda da göndermeli suyu").</summary>
        public Func<Vector3> WaterBucketMin, WaterBucketMax;
        private RectTransform _wTap;

        private RectTransform _wBar;
        private TextMeshProUGUI _wBarText;
        private float _wBarShown, _wBarTarget, _wBarPulse = 9f, _wBarClock;
        private int _wBarPct = -1;

        private RectTransform _wSend;
        private Image _wSendBg;
        private float _wSendClock;

        public float WaterBarShown => _wBarShown;
        /// <summary>Ekran yakalama: çubuk ve düğme animasyonunu ilerletir.</summary>
        public void AdvanceWaterBar(float sec) { for (float t = 0f; t < sec; t += 0.02f) TickWaterBar(0.02f); }

        private void ShowWaterBar(bool on, bool otherGoals)
        {
            if (on && _wBar == null) BuildWaterBar();
            if (_wBar != null) _wBar.gameObject.SetActive(on);
            if (_wBarText != null) _wBarText.gameObject.SetActive(on);
            if (on && _wSend == null) BuildWaterSend();
            if (_wSend != null) _wSend.gameObject.SetActive(false);
            _wBarShown = _wBarTarget = 0f;
            _wBarPct = -1;
        }

        /// <summary>Kartın hedef doluluğu (0–1); <paramref name="instant"/> değilse yaylı dolar ve kart zıplar.</summary>
        public void SetWaterBar(float fill, bool instant)
        {
            _wBarTarget = Mathf.Clamp01(fill);
            if (instant) _wBarShown = _wBarTarget;
            else _wBarPulse = 0f;
        }

        /// <summary>Gönderilen suyun varacağı ekran noktası: kartın dolum ucu (kanvas ve tahta dünyaları farklı; ekran üzerinden).</summary>
        public Vector2 WaterBarScreen()
        {
            if (_wBar == null) return Vector2.zero;
            float w = _wBar.rect.width;
            float x = Mathf.Lerp(-w * 0.5f + 30f, w * 0.5f - 30f, Mathf.Max(_wBarShown, 0.05f));
            var world = _wBar.TransformPoint(new Vector3(x, _wBar.rect.height * 0.15f, 0f)); // dolum ucunun üst yarısı
            var cam = _canvas != null ? _canvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(cam, world);
        }

        private void TickWaterBar(float dt)
        {
            if (_wBar != null && _wBar.gameObject.activeInHierarchy)
            {
                _wBarClock += dt;
                _wBarShown = Mathf.MoveTowards(_wBarShown, _wBarTarget, dt * 0.8f);
                float w = _wBar.rect.width, h = _wBar.rect.height;
                if (w > 1f && h > 1f && Mathf.Abs(w / h - _wBodyAspect) > 0.02f)
                {
                    _wBodyAspect = w / h;
                    _wBody.sprite = WaterArt.BarBody(_wBodyAspect);
                }
                // Gövde: kapsül, soldan dolar (kenarları yumuşak; maske yok).
                _wBody.fillAmount = _wBarShown;
                _wBody.enabled = _wBarShown > 0.002f;
                float fw = w * _wBarShown;
                // Dolum ucu: yalnız kapsülün düz kısmında (uçlarda kapsülün yuvarlağı ile çakışmasın); hafif soluk salınır.
                bool edge = fw > h * 0.55f && fw < w - h * 0.55f;
                _wBarEdge.gameObject.SetActive(edge);
                if (edge)
                {
                    float ew = h * 0.34f;
                    _wBarEdgeRt.sizeDelta = new Vector2(ew, h);
                    _wBarEdgeRt.anchoredPosition = new Vector2(fw - ew * 0.5f + Mathf.Sin(_wBarClock * 2.2f) * 1.5f, 0f);
                    _wBarEdge.uvRect = new Rect(0f, 0f, 1f, 1f); // dikey kaydırma yok: renk geçişi y ile değişir
                }
                // Işık: suyun düz kısmında yavaşça kayan yumuşak parıltı (üst yarıda).
                float lo = h * 0.6f, hi = fw - h * 0.6f;
                bool shine = hi - lo > h * 0.5f;
                _wBarShine.gameObject.SetActive(shine);
                if (shine)
                {
                    float sx = Mathf.Lerp(lo, hi, Mathf.PingPong(_wBarClock * 0.12f, 1f));
                    _wBarShine.anchoredPosition = new Vector2(sx, h * 0.16f);
                    _wBarShine.sizeDelta = new Vector2(Mathf.Min(h * 2.6f, hi - lo), h * 0.22f);
                }
                if (_wBarPulse < 1f) _wBarPulse += dt / 0.35f;
                float k = 1f + 0.035f * Mathf.Sin(Mathf.Clamp01(_wBarPulse) * Mathf.PI);
                _goalsCard.localScale = new Vector3(k, k, 1f);
                int pct = Mathf.RoundToInt(_wBarShown * 100f);
                if (pct != _wBarPct) { _wBarPct = pct; _wBarText.text = "%" + pct; }
            }
            TickWaterSend(dt);
        }

        private Image _wBody;
        private float _wBodyAspect;
        private RawImage _wBarEdge;
        private RectTransform _wBarEdgeRt, _wBarShine;

        /// <summary>
        /// 2026-11-26 (kullanıcı: "görev çubuğunun direkt kendisi su ile dolsun … Borderını da su ile doldurma … Görev çubuğundaki su
        /// efekti kötü ve sorunlu duruyor … kusursuz bir görünüm olsun"): kenarlığın içinde, kartın oranında yumuşak kenarlı kapsül su;
        /// soldan dolar, dolum ucu dalgalı menisküs, düz kısımda kayan ışık. Ayrı kanal ya da maske yok.
        /// </summary>
        private void BuildWaterBar()
        {
            var card = _goalsCard != null ? _goalsCard : _goalsRow;
            _wBar = UiFactory.Rect("WaterFill", card);
            UiFactory.Stretch(_wBar, 9f, 9f, 8f, 8f);
            _wBar.SetSiblingIndex(0);
            _wBody = UiFactory.Image("Water", _wBar, WaterArt.BarBody(8f), Color.white);
            _wBodyAspect = 8f;
            _wBody.raycastTarget = false;
            _wBody.type = Image.Type.Filled;
            _wBody.fillMethod = Image.FillMethod.Horizontal;
            _wBody.fillOrigin = (int)Image.OriginHorizontal.Left;
            UiFactory.Stretch(_wBody.rectTransform);
            var eg = new GameObject("Edge", typeof(RectTransform), typeof(RawImage));
            eg.transform.SetParent(_wBar, false);
            _wBarEdge = eg.GetComponent<RawImage>();
            _wBarEdge.texture = WaterArt.BarEdge;
            _wBarEdge.raycastTarget = false;
            _wBarEdgeRt = (RectTransform)eg.transform;
            _wBarEdgeRt.anchorMin = _wBarEdgeRt.anchorMax = new Vector2(0f, 0.5f);
            _wBarEdgeRt.pivot = new Vector2(0.5f, 0.5f);
            var shine = UiFactory.Image("Shine", _wBar, SpecialArt.Glow, new Color(1f, 1f, 1f, 0.28f));
            shine.raycastTarget = false;
            _wBarShine = shine.rectTransform;
            _wBarShine.anchorMin = _wBarShine.anchorMax = new Vector2(0f, 0.5f);
            // Yüzde: kartın ortasında (hedef satırının üstünde).
            _wBarText = UiFactory.Title("Pct", card, "%0", 44, Color.white, new Color32(0x10, 0x2E, 0x55, 0xFF));
            _wBarText.raycastTarget = false;
            _wBarText.alignment = TextAlignmentOptions.Center;
            UiFactory.Stretch(_wBarText.rectTransform);
        }

        private void BuildWaterSend()
        {
            var canvas = _canvas != null ? _canvas : _goalsRow.GetComponentInParent<Canvas>();
            var btn = UiFactory.SpriteButton("WaterSend", canvas != null ? canvas.transform : _goalsRow, Ui3.Rounded(26f, Ui3.Hex("#FFD86A"), Ui3.Hex("#F0A92E"), 3f, Ui3.Hex("#9A6A1C")), new Vector2(170f, 64f), () => WaterSendPressed?.Invoke());
            _wSend = (RectTransform)btn.transform;
            _wSend.anchorMin = _wSend.anchorMax = new Vector2(0.5f, 0.5f);
            _wSendBg = btn.GetComponent<Image>();
            PressFeedback.Attach(btn, null);
            // Kovanın üstünde görünmez dokunma alanı (düğmenin altında kalır).
            var tap = UiFactory.SpriteButton("WaterBucketTap", canvas != null ? canvas.transform : _goalsRow, null, new Vector2(100f, 100f), () => WaterSendPressed?.Invoke());
            tap.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            _wTap = (RectTransform)tap.transform;
            _wTap.anchorMin = _wTap.anchorMax = new Vector2(0.5f, 0.5f);
            _wTap.SetSiblingIndex(_wSend.GetSiblingIndex());
            var txt = UiFactory.Title("Text", _wSend, Loc.T("bucket.send"), 32, new Color32(0x5A, 0x34, 0x08, 0xFF), new Color(1f, 1f, 1f, 0f));
            txt.raycastTarget = false;
            txt.alignment = TextAlignmentOptions.Center;
            UiFactory.Stretch(txt.rectTransform);
        }

        private void TickWaterSend(float dt)
        {
            if (_wSend == null) return;
            int st = WaterSendState != null ? WaterSendState() : 0;
            bool show = st > 0 && WaterSendAnchor != null && _wBar != null && _wBar.gameObject.activeInHierarchy;
            if (_wSend.gameObject.activeSelf != show) _wSend.gameObject.SetActive(show);
            bool tapOn = show && WaterBucketMin != null && WaterBucketMax != null;
            if (_wTap != null && _wTap.gameObject.activeSelf != tapOn) _wTap.gameObject.SetActive(tapOn);
            if (!show) return;
            _wSendClock += dt;
            // Kovanın önüne: dünya → ekran → kanvas.
            var cam = _canvas != null ? _canvas.worldCamera : null;
            if (cam != null && _wSend.parent == _canvas.transform)
            {
                float sf = Mathf.Max(0.0001f, _canvas.scaleFactor);
                Vector2 sp = cam.WorldToScreenPoint(WaterSendAnchor());
                var center = new Vector2(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f);
                _wSend.anchoredPosition = (sp - center) / sf;
                if (tapOn)
                {
                    Vector2 a0 = cam.WorldToScreenPoint(WaterBucketMin()), a1 = cam.WorldToScreenPoint(WaterBucketMax());
                    _wTap.anchoredPosition = ((a0 + a1) * 0.5f - center) / sf;
                    _wTap.sizeDelta = new Vector2(Mathf.Abs(a1.x - a0.x), Mathf.Abs(a1.y - a0.y)) / sf;
                }
            }
            bool full = st >= 2;
            float k = full ? 1f + 0.08f * Mathf.Sin(_wSendClock * 7f) : 1f + 0.03f * Mathf.Sin(_wSendClock * 3f);
            _wSend.localScale = new Vector3(k, k, 1f);
            _wSendBg.color = full ? Color.Lerp(Color.white, new Color(1f, 0.85f, 0.5f), 0.5f + 0.5f * Mathf.Sin(_wSendClock * 7f)) : Color.white;
        }
    }
}
