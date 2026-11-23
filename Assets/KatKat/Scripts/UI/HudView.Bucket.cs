using System;
using System.Collections.Generic;
using KatKat.Core;
using KatKat.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.UI
{
    /// <summary>
    /// Kova kipi arayüzü (kullanıcı 2026-11-23: "Kova dediğim alanın tam ortasında duracak. Sağında ve solunda ise birer tane altın rulo
    /// ve roket alanı olacak"). Güç bandının ortasında kova; solda Altın Rulo, sağda roket yuvası (tepsinin ilk iki yuvası).
    /// <para>Kova kendi gösterim modelini tutar: tahtadan kovaya varan her taş bir artırır (dolum yükselir, kova zıplar); hamlenin
    /// kova gönderimleri sırayla oynatılır: kova dolunca rengin görev simgesine uçar, yerine yenisi aşağıdan gelir. Kovaya dokunmak
    /// <see cref="BucketPressed"/> (yarım gönderim). Uçuş ve oynatım bitince gösterim mantık durumuna eşitlenir.</para>
    /// </summary>
    public sealed partial class HudView
    {
        public event Action BucketPressed;

        private RectTransform _bucketRoot, _bucketBody;
        private Image _bucketShell, _bucketFill, _bucketStone, _bucketGlow, _bucketSendTag;
        private TextMeshProUGUI _bucketSendText;
        private Image _bucketSurface;
        private float _bSplash = 9f;

        /// <summary>Görsel kapasite: kova en çok bu kadar taşta "dolu" görünür (rengin kalan görevi daha azsa o kadar).</summary>
        public const int BucketVisualCap = 12;
        private bool _bucketMode;
        private readonly int[] _slotMap = { -1, -1 };

        /// <summary>Gösterilen kova.</summary>
        private bool _bActive;
        private LayerColor _bColor;
        private int _bNeed, _bHave;
        private float _bFillShown;

        /// <summary>Oynatılmayı bekleyen gönderimler (mantıkta sırayla) ve kovaya uçmakta olan taş sayısı.</summary>
        private readonly Queue<BucketSend> _bSends = new Queue<BucketSend>();
        private int _bInFlight, _bEarly;
        /// <summary>Gönderim animasyonu: 0..1 kalkış, 1..2 yeni kova gelişi; &lt;0 yok.</summary>
        private float _bAnim = -1f;
        private BucketSend _bAnimSend;
        private Vector3 _bFlyFrom, _bFlyTo;
        private float _bPunch = 9f;
        /// <summary>Eşitlenecek mantık durumu (uçuşlar/animasyon bitince).</summary>
        private BucketRun _bTarget;
        private float _bSettleWait;

        public const float BucketSize = 176f;
        /// <summary>Kova kipinde yan yuvaların merkezden uzaklığı ve kovanın ölçeği (güç bandı 128 px).</summary>
        private const float BucketSideStep = 270f, BucketScale = 1.12f;
        private const float SendRise = 0.42f, SendArrive = 0.34f;

        public bool BucketMode => _bucketMode;
        /// <summary>Testler: gösterilen kova.</summary>
        public bool BucketShownActive => _bActive;
        public LayerColor BucketShownColor => _bColor;
        public int BucketShownHave => _bHave;
        public int BucketShownNeed => _bNeed;
        public bool BucketBusy => _bAnim >= 0f || _bSends.Count > 0 || _bInFlight > 0;

        /// <summary>Kova kipini açar/kapatır ve başlangıç kovasını gösterir (bölüm başı, geri al, yeniden başla).</summary>
        public void SetBucketMode(BucketRun run)
        {
            _bucketMode = run != null;
            if (_bucketMode && _bucketRoot == null) BuildBucket();
            if (_bucketRoot != null) _bucketRoot.gameObject.SetActive(_bucketMode);
            _bSends.Clear();
            _bInFlight = 0;
            _bEarly = 0;
            _bAnim = -1f;
            _bTarget = null;
            if (_bucketMode) ShowBucket(run.Active, run.Color, run.Need, run.Have, true);
            LayoutBottom();
        }

        /// <summary>Kova kipinde tepsi yuvası → gerçek tepsi dizini (sol Altın Rulo, sağ roket).</summary>
        private int MapTraySlot(int slot) => _bucketMode ? (slot < 2 ? _slotMap[slot] : -1) : slot;

        /// <summary>Hamlenin kova gönderimlerini ve kovaya uçacak taş sayısını kuyruğa alır.</summary>
        public void BucketQueue(MoveResult r)
        {
            if (!_bucketMode || r == null) return;
            foreach (var w in r.Waves) _bInFlight += w.BucketCells.Count;
            foreach (var s in r.BucketSends) _bSends.Enqueue(s);
            TryStartSend();
        }

        /// <summary>Tahtadan bir taş kovaya vardı.</summary>
        public void BucketStoneIn()
        {
            if (!_bucketMode) return;
            if (_bInFlight > 0) _bInFlight--;
            if (_bAnim >= 0f) { _bEarly++; return; } // gönderim sürerken gelen taşlar (aynı renk sonraki kova) yeni kovada sayılır
            if (_bActive && _bHave < _bNeed) _bHave++;
            _bPunch = 0f;
            _bSplash = 0f;
            if (_bActive) SparkAt(_bucketBody.TransformPoint(new Vector3(0f, BucketSize * 0.2f, 0f)), _bColor, 5);
            RefreshBucket();
            TryStartSend();
        }

        /// <summary>Hamle sonu mantık durumu: uçuşlar ve animasyonlar bitince gösterim buna eşitlenir.</summary>
        public void BucketSettle(BucketRun run)
        {
            if (!_bucketMode || run == null) return;
            _bTarget = run.Clone();
            _bSettleWait = 0f;
        }

        /// <summary>Kovanın dünya konumu (taşlar buraya uçar).</summary>
        public Vector3 BucketTarget() => _bucketBody != null ? _bucketBody.TransformPoint(new Vector3(0f, BucketSize * 0.18f, 0f)) : Vector3.zero; // kovanın ağzı

        /// <summary>Ekran yakalama: kova animasyonunu ilerletir (oynatma dışında Update çalışmaz).</summary>
        public void AdvanceBucket(float sec) => TickBucket(sec);

        /// <summary>Testler: bütün kova animasyonlarını hemen bitirir.</summary>
        public void BucketFinishForTest()
        {
            for (int i = 0; i < 400 && (BucketBusy || _bTarget != null); i++) TickBucket(0.05f);
        }

        private void TryStartSend()
        {
            if (_bAnim >= 0f || _bSends.Count == 0) return;
            var s = _bSends.Peek();
            // Kendiliğinden gönderim kova dolunca; yarım gönderim (oyuncu) hemen.
            if (!s.Partial && _bHave < s.Have) return;
            _bSends.Dequeue();
            _bAnimSend = s;
            _bHave = s.Have;
            _bAnim = 0f;
            _bFlyFrom = _bucketBody.position;
            _bFlyTo = GoalTarget(s.Color);
            RefreshBucket();
            SparkBucket(s.Color, 14);
        }

        private void TickBucket(float dt)
        {
            if (!_bucketMode || _bucketRoot == null) return;
            if (_bAnim >= 0f)
            {
                _bAnim += dt / (_bAnim < 1f ? SendRise : SendArrive);
                if (_bAnim < 1f)
                {
                    // Kalkış: kova görev simgesine doğru yay çizerek uçar, küçülür.
                    float t = _bAnim;
                    float e = t * t * (3f - 2f * t);
                    var p = Vector3.Lerp(_bFlyFrom, _bFlyTo, e);
                    p += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.9f;
                    _bucketBody.position = p;
                    _bucketBody.localScale = Vector3.one * Mathf.Lerp(1.08f, 0.35f, e);
                    _bucketBody.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * -14f);
                }
                else
                {
                    if (_bAnim - dt / SendArrive < 1f)
                    {
                        // Varış: görev simgesinde ışıltı, yeni kova aşağıdan gelir.
                        SparkAt(_bFlyTo, _bAnimSend.Color, 10);
                        _bucketBody.localRotation = Quaternion.identity;
                        _bucketBody.anchoredPosition = new Vector2(0f, -BucketSize * 0.9f);
                        ShowBucket(_bAnimSend.NextActive, _bAnimSend.NextColor, _bAnimSend.NextNeed, Mathf.Min(_bEarly, _bAnimSend.NextNeed), false);
                        _bEarly = 0;
                    }
                    float t = Mathf.Clamp01(_bAnim - 1f);
                    float back = 1f + 2.2f * Mathf.Pow(t - 1f, 3f) + 1.2f * Mathf.Pow(t - 1f, 2f); // hafif taşma
                    _bucketBody.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-BucketSize * 0.9f, 0f, back));
                    _bucketBody.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, t);
                    if (_bAnim >= 2f)
                    {
                        _bAnim = -1f;
                        _bucketBody.anchoredPosition = Vector2.zero;
                        _bucketBody.localScale = Vector3.one;
                        TryStartSend();
                    }
                }
            }
            else if (_bPunch < 1f)
            {
                _bPunch += dt / 0.22f;
                float k = 1f + 0.12f * Mathf.Sin(Mathf.Min(1f, _bPunch) * Mathf.PI);
                _bucketBody.localScale = new Vector3(k, 2f - k, 1f);
            }
            else _bucketBody.localScale = Vector3.one;

            // Dolum göstergesi yumuşak yükselir.
            float want = FillOf(_bHave, _bNeed);
            _bFillShown = Mathf.MoveTowards(_bFillShown, want, dt * 1.6f);
            // Gövdenin iç kısmı (y 0,13–0,68) dolum aralığı; yüzey dolum seviyesinde dalgalanır.
            const float lo = 0.13f, hi = 0.68f;
            float lvl = lo + (hi - lo) * _bFillShown;
            _bucketFill.fillAmount = _bFillShown <= 0f ? 0f : lvl;
            if (_bucketSurface != null)
            {
                bool show = _bFillShown > 0.01f && _bActive;
                _bucketSurface.enabled = show;
                if (show)
                {
                    float wob = Motion.Reduced ? 0f : Mathf.Sin(Time.unscaledTime * 5f) * 0.012f + (_bSplash < 1f ? Mathf.Sin(_bSplash * 18f) * (1f - _bSplash) * 0.03f : 0f);
                    float halfW = Mathf.Lerp(0.29f, 0.40f, Mathf.InverseLerp(0.10f, 0.70f, lvl)) - 0.03f;
                    var rt = _bucketSurface.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, lvl + wob);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(BucketSize * halfW * 2f, BucketSize * 0.1f);
                    var c = _bucketFill.color;
                    _bucketSurface.color = new Color(Mathf.Lerp(c.r, 1f, 0.55f), Mathf.Lerp(c.g, 1f, 0.55f), Mathf.Lerp(c.b, 1f, 0.55f), 0.95f);
                }
            }
            if (_bSplash < 1f) _bSplash += dt / 0.5f;

            // Yarım gönderim ipucu: kovada taş varken "Gönder" etiketi nefes alır.
            bool canSend = _bActive && _bHave > 0 && _bAnim < 0f;
            _bucketSendTag.gameObject.SetActive(canSend);
            if (canSend) _bucketSendTag.transform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f));
            _bucketGlow.color = new Color(_bucketGlow.color.r, _bucketGlow.color.g, _bucketGlow.color.b, _bActive ? 0.55f + 0.2f * Mathf.Sin(Time.unscaledTime * 2.2f) : 0f);

            // Eşitleme: uçuş ve gönderim bitince (ya da taş kaybolduysa 1,5 sn sonra) mantık durumuna.
            if (_bTarget != null && _bAnim < 0f && _bSends.Count == 0)
            {
                _bSettleWait += dt;
                if (_bInFlight == 0 || _bSettleWait > 1.5f)
                {
                    var t = _bTarget;
                    _bTarget = null;
                    _bInFlight = 0;
                    if (t.Active != _bActive || t.Color != _bColor || t.Need != _bNeed || t.Have != _bHave) ShowBucket(t.Active, t.Color, t.Need, t.Have, true);
                }
            }
        }

        private void ShowBucket(bool active, LayerColor color, int need, int have, bool instant)
        {
            _bActive = active;
            _bColor = color;
            _bNeed = need;
            _bHave = have;
            if (instant) _bFillShown = FillOf(have, need);
            else _bFillShown = 0f;
            _bucketBody.gameObject.SetActive(active);
            _bucketSendTag.gameObject.SetActive(active && have > 0);
            if (active)
            {
                var c = (Color)LayerPalette.FrontColor(color);
                _bucketShell.color = Color.Lerp(c, Color.white, 0.55f);
                _bucketFill.color = c;
                _bucketGlow.color = new Color(c.r, c.g, c.b, 0.6f);
                _bucketStone.sprite = GoalChip.IconFor(new GoalState { Type = GoalType.CollectColor, Color = color });
            }
            RefreshBucket();
        }

        private void RefreshBucket() { }

        /// <summary>Gösterilen dolum oranı (0–1): görsel kapasiteye göre.</summary>
        private float FillOf(int have, int need) => need > 0 ? Mathf.Clamp01(have / (float)Mathf.Min(need, BucketVisualCap)) : 0f;

        private void SparkBucket(LayerColor color, int n) => SparkAt(_bucketBody.position, color, n);

        private void SparkAt(Vector3 world, LayerColor color, int n)
        {
            if (Motion.Reduced || _bucketRoot == null) return;
            var c = (Color)LayerPalette.FrontColor(color);
            for (int i = 0; i < n; i++)
            {
                var sp = UiFactory.Image("BucketSpark", _bucketRoot.parent, SpecialArt.Glow, Color.Lerp(c, Color.white, 0.4f));
                sp.raycastTarget = false;
                sp.rectTransform.sizeDelta = Vector2.one * UnityEngine.Random.Range(18f, 34f);
                sp.transform.position = world;
                float a = i / (float)n * Mathf.PI * 2f + UnityEngine.Random.value * 0.4f;
                sp.gameObject.AddComponent<BucketSpark>().Init(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * UnityEngine.Random.Range(160f, 320f));
            }
        }

        private void BuildBucket()
        {
            _bucketRoot = UiFactory.Rect("Bucket", _skillBar);
            _bucketRoot.anchorMin = _bucketRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _bucketRoot.sizeDelta = new Vector2(BucketSize, BucketSize);
            _bucketBody = UiFactory.Rect("Body", _bucketRoot);
            UiFactory.Place(_bucketBody, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BucketSize, BucketSize));

            _bucketGlow = UiFactory.Image("Glow", _bucketBody, SpecialArt.Glow, new Color(1f, 1f, 1f, 0.5f));
            _bucketGlow.raycastTarget = false;
            UiFactory.Place(_bucketGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), Vector2.one * BucketSize * 1.55f);

            var btn = UiFactory.SpriteButton("BucketButton", _bucketBody, BucketArt.Shell, new Vector2(BucketSize, BucketSize), () => BucketPressed?.Invoke());
            UiFactory.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BucketSize, BucketSize));
            _bucketShell = btn.GetComponent<Image>();
            _bucketShell.preserveAspect = true;
            PressFeedback.Attach(btn, null);

            _bucketFill = UiFactory.Image("Fill", btn.transform, BucketArt.Inner, Color.white);
            _bucketFill.raycastTarget = false;
            _bucketFill.type = Image.Type.Filled;
            _bucketFill.fillMethod = Image.FillMethod.Vertical;
            _bucketFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            UiFactory.Stretch(_bucketFill.rectTransform);

            var rim = UiFactory.Image("Rim", btn.transform, BucketArt.Rim, Color.white);
            rim.raycastTarget = false;
            UiFactory.Stretch(rim.rectTransform);

            _bucketStone = UiFactory.Image("Stone", btn.transform, null, Color.white);
            _bucketStone.preserveAspect = true;
            _bucketStone.raycastTarget = false;
            UiFactory.Place(_bucketStone.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), Vector2.one * BucketSize * 0.36f);

            // Dolum yüzeyi (2026-11-23 ikinci tur, kullanıcı: "kovada sayı olmayacak … kovada dolma efekti olacak"): sayı yok; dolum
            // seviyesinde açık renkli, dalgalanan yüzey ve her taşta ağızdan sıçrama.
            _bucketSurface = UiFactory.Image("Surface", btn.transform, SpecialArt.Glow, Color.white);
            _bucketSurface.raycastTarget = false;
            _bucketSurface.rectTransform.sizeDelta = new Vector2(BucketSize * 0.62f, BucketSize * 0.09f);

            // "Gönder" etiketi (yarım gönderim): kovanın üstünde.
            _bucketSendTag = UiFactory.Image("SendTag", _bucketBody, Ui3.Rounded(18f, Ui3.Hex("#FFD86A"), Ui3.Hex("#F0A92E"), 2f, Ui3.Hex("#9A6A1C")), Color.white);
            _bucketSendTag.raycastTarget = false;
            UiFactory.Place(_bucketSendTag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, BucketSize * 0.5f), new Vector2(118f, 38f));
            _bucketSendText = UiFactory.Title("Text", _bucketSendTag.transform, Loc.T("bucket.send"), 24, new Color32(0x5A, 0x34, 0x08, 0xFF), new Color(1f, 1f, 1f, 0f));
            _bucketSendText.raycastTarget = false;
            UiFactory.Stretch(_bucketSendText.rectTransform);
        }
    }

    /// <summary>Kova kıvılcımı: dağılır, yavaşlar, söner.</summary>
    public sealed class BucketSpark : MonoBehaviour
    {
        private Vector2 _v;
        private float _t;
        private Image _img;

        public void Init(Vector2 v) { _v = v; _img = GetComponent<Image>(); }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            ((RectTransform)transform).anchoredPosition += _v * dt;
            _v *= Mathf.Exp(-3.5f * dt);
            if (_img != null) { var c = _img.color; c.a = Mathf.Clamp01(1f - _t / 0.6f); _img.color = c; }
            if (_t > 0.6f) Destroy(gameObject);
        }
    }

    /// <summary>Kova görselleri (koddan, 192 px): kova gövdesi (kulp, gövde, alt halka), iç dolum şekli ve ağız kenarı.</summary>
    internal static class BucketArt
    {
        private const int N = 192;
        private static Sprite _shell, _inner, _rim;

        // Gövde: üst (ağız) y=0.70, alt y=0.10; üst yarı genişlik 0.40, alt 0.29 (normalize, merkez 0.5).
        private const float TopY = 0.70f, BotY = 0.10f, TopW = 0.40f, BotW = 0.29f;

        private static float HalfW(float y) => Mathf.Lerp(BotW, TopW, Mathf.InverseLerp(BotY, TopY, y));

        public static Sprite Shell => _shell ? _shell : (_shell = Make("bucket_shell", ShellPx));
        public static Sprite Inner => _inner ? _inner : (_inner = Make("bucket_inner", InnerPx));
        public static Sprite Rim => _rim ? _rim : (_rim = Make("bucket_rim", RimPx));

        private static Color ShellPx(float x, float y)
        {
            float dx = x - 0.5f;
            // Kulp: ağzın üstünde yarım elips çizgisi.
            float ex = dx / (TopW * 0.92f), ey = (y - TopY) / 0.24f;
            float r = Mathf.Sqrt(ex * ex + ey * ey);
            float handle = y > TopY - 0.01f ? Edge(0.035f - Mathf.Abs(r - 1f) * 0.24f) : 0f;
            var col = new Color(0f, 0f, 0f, 0f);
            if (handle > 0f) col = Over(col, new Color(0.55f, 0.58f, 0.62f, handle));
            // Gövde (beyaz; renk Image.color ile): soldan sağa hafif gölge, kenarda koyu çizgi.
            if (y >= BotY - 0.02f && y <= TopY + 0.02f)
            {
                float hw = HalfW(Mathf.Clamp(y, BotY, TopY));
                float inside = Mathf.Min(hw - Mathf.Abs(dx), Mathf.Min(y - BotY, TopY - y) + 0.02f);
                float a = Edge(inside);
                if (a > 0f)
                {
                    float shade = 1f - 0.22f * Mathf.InverseLerp(-hw, hw, dx);
                    float line = inside < 0.018f ? 0.62f : 1f;
                    // Gövde bantları (iki yatay şerit).
                    float band = Mathf.Abs(y - 0.30f) < 0.018f || Mathf.Abs(y - 0.52f) < 0.018f ? 0.86f : 1f;
                    float v = shade * line * band;
                    col = Over(col, new Color(v, v, v, a));
                }
            }
            return col;
        }

        private static Color InnerPx(float x, float y)
        {
            float dx = x - 0.5f;
            if (y < BotY + 0.03f || y > TopY - 0.02f) return new Color(1f, 1f, 1f, 0f);
            float hw = HalfW(y) - 0.035f;
            float a = Edge(Mathf.Min(hw - Mathf.Abs(dx), y - BotY - 0.03f));
            float v = 0.82f + 0.18f * Mathf.InverseLerp(BotY, TopY, y);
            return new Color(v, v, v, a * 0.92f);
        }

        private static Color RimPx(float x, float y)
        {
            // Ağız: üstte açık renkli elips halka; altta koyu taban halkası.
            float dx = (x - 0.5f) / (TopW + 0.02f), dy = (y - TopY) / 0.06f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float top = Edge((0.16f - Mathf.Abs(r - 0.88f)) * 0.12f);
            var col = new Color(0f, 0f, 0f, 0f);
            if (top > 0f) col = Over(col, new Color(1f, 1f, 1f, top * 0.95f));
            float bx = (x - 0.5f) / (BotW + 0.01f), by = (y - BotY) / 0.035f;
            float br = Mathf.Sqrt(bx * bx + by * by);
            float bot = y < BotY + 0.01f ? Edge((1f - br) * 0.05f) : 0f;
            if (bot > 0f) col = Over(col, new Color(0f, 0f, 0f, bot * 0.25f));
            return col;
        }

        private static float Edge(float d) => Mathf.Clamp01(d * N / 1.5f + 0.5f);

        private static Color Over(Color dst, Color src)
        {
            float a = src.a + dst.a * (1f - src.a);
            if (a <= 0f) return new Color(0f, 0f, 0f, 0f);
            var c = (src * src.a + dst * dst.a * (1f - src.a)) / a;
            c.a = a;
            return c;
        }

        private static Sprite Make(string name, Func<float, float, Color> f)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
                px[j * N + i] = f((i + 0.5f) / N, (j + 0.5f) / N);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = name;
            return s;
        }
    }
}
