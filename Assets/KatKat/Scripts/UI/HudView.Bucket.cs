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
    /// Kova (torba) kipi arayüzü (kullanıcı 2026-11-23: "Kova dediğim alanın tam ortasında duracak. Sağında ve solunda ise birer tane
    /// altın rulo ve roket alanı olacak"; aynı gün: "kovaya değilde torbaya dolduralım"). Güç bandının ortasında torba; solda Altın
    /// Rulo, sağda roket yuvası (tepsinin ilk iki yuvası). Kodda adı hâlâ "bucket" (kural motoru ve testlerle aynı ad).
    /// <para>Torba görseli katmanlı (Resources/Bag): arka gövde, ağız içine yığılan taşlar, ağzı oyulmuş ön katman ve rengine
    /// boyanan kurdele. Doldukça torba şişer ve ağızda taş yığını yükselir.</para>
    /// <para>Kova kendi gösterim modelini tutar: tahtadan kovaya varan her taş bir artırır (yığın büyür, torba zıplar); hamlenin
    /// kova gönderimleri sırayla oynatılır: kova dolunca rengin görev simgesine uçar, yerine yenisi aşağıdan gelir. Kovaya dokunmak
    /// <see cref="BucketPressed"/> (yarım gönderim). Uçuş ve oynatım bitince gösterim mantık durumuna eşitlenir.</para>
    /// </summary>
    public sealed partial class HudView
    {
        public event Action BucketPressed;
        /// <summary>Gönderilen torba görev simgesine vardı (2026-11-24: son torbanın sayısı da artsın).</summary>
        public Action BucketDelivered;

        /// <summary>Torba teslim sayacı: çip, başlangıç, hedef, geçen süre, son adım.</summary>
        private sealed class GoalCount { public GoalChip Chip; public int From, To, Last; public float T, Dur; public LayerColor Color; }
        private readonly List<GoalCount> _goalCounts = new List<GoalCount>();

        /// <summary>
        /// Görev sayaçları torba simgeye varınca sayarak artar (kullanıcı 2026-11-24: "torbayı gönderdiğimde görev sayısı efektif
        /// artmıyor. anlaşılmıyor artıp artmadığı"): sayı tek tek yükselir, her adımda büyüyüp yerine oturur, simge zıplar ve
        /// sonunda parlar.
        /// </summary>
        public void CountGoalsTo(IList<GoalState> goals)
        {
            for (int i = 0; i < goals.Count && i < _chips.Count; i++)
            {
                var chip = _chips[i];
                int to = Mathf.Min(goals[i].Progress, goals[i].Amount);
                if (to <= chip.Shown) { chip.Set(goals[i].Progress, false); continue; }
                _goalCounts.RemoveAll(g => g.Chip == chip);
                int delta = to - chip.Shown;
                _goalCounts.Add(new GoalCount { Chip = chip, From = chip.Shown, To = to, Last = chip.Shown, Dur = Mathf.Clamp(0.25f + delta * 0.03f, 0.3f, 1.1f), Color = goals[i].Color });
            }
        }

        private void TickGoalCounts(float dt)
        {
            for (int i = _goalCounts.Count - 1; i >= 0; i--)
            {
                var g = _goalCounts[i];
                if (g.Chip == null || g.Chip.Root == null) { _goalCounts.RemoveAt(i); continue; } // bölüm değişti, çip yok
                g.T += dt;
                float u = Mathf.Clamp01(g.T / g.Dur);
                int v = Mathf.RoundToInt(Mathf.Lerp(g.From, g.To, 1f - (1f - u) * (1f - u)));
                if (v != g.Last)
                {
                    g.Last = v;
                    g.Chip.Set(v, true);
                }
                if (u >= 1f)
                {
                    g.Chip.Set(g.To, true);
                    SparkAt(g.Chip.Icon.position, g.Color, 12);
                    _goalCounts.RemoveAt(i);
                }
            }
        }

        private RectTransform _bucketRoot, _bucketBody;
        private RectTransform _bagArt;
        private Image _bucketGlow, _bucketSendTag, _bagBadge, _bagCountBubble;
        private TextMeshProUGUI _bagCountText;
        private int _bagCountShown = -1;
        private float _bagCountPunch = 9f;
        /// <summary>Boş ve dolu torba katmanları (arka, ön, kurdele); doldukça boştan doluya şişerek geçer.</summary>
        private Image[] _bagEmpty, _bagFull;
        private Color _bagTint = Color.white;
        private float _bagMorph;
        private TextMeshProUGUI _bucketSendText;
        private Image[] _bagHeap;
        private float[] _bagPop;

        /// <summary>
        /// Ağız içi yığın: kırık taş parçaları (kullanıcı 2026-11-23: "torbada taşlar parçalanmış olarak bulunsun"). Taşlar torbaya
        /// bütün uçar, içeride parça olarak birikir. Yuvalar ağız merkezine göre, torba boyu biriminde; alttan üste dolar, alt sıralar
        /// ön dudağın ardında kalır. Biçim, açı ve boy sabit (her bölümde aynı yığın).
        /// </summary>
        private static readonly Vector2[] HeapSlots = BuildHeapSlots(out HeapSpin, out HeapScale);
        private static float[] HeapSpin, HeapScale;

        private static Vector2[] BuildHeapSlots(out float[] spin, out float[] scale)
        {
            int[] rows = { 6, 5, 5, 4, 3, 2 };
            float[] ys = { -0.10f, -0.075f, -0.05f, -0.025f, 0.0f, 0.025f };
            float[] half = { 0.20f, 0.18f, 0.16f, 0.13f, 0.09f, 0.05f };
            var rng = new System.Random(7);
            var pts = new List<Vector2>();
            var sp = new List<float>();
            var sc = new List<float>();
            for (int r = 0; r < rows.Length; r++)
            for (int i = 0; i < rows[r]; i++)
            {
                float t = rows[r] == 1 ? 0.5f : i / (float)(rows[r] - 1);
                float x = Mathf.Lerp(-half[r], half[r], t) + ((float)rng.NextDouble() - 0.5f) * 0.03f;
                float y = ys[r] + ((float)rng.NextDouble() - 0.5f) * 0.014f;
                pts.Add(new Vector2(x, y));
                sp.Add((float)rng.NextDouble() * 360f);
                sc.Add(0.85f + (float)rng.NextDouble() * 0.35f);
            }
            spin = sp.ToArray();
            scale = sc.ToArray();
            return pts.ToArray();
        }
        /// <summary>
        /// Şişme (kullanıcı 2026-11-23: "Torba boş gelsin ve doldukça animasyonla şişir torbayı"): boş torba (Krem Saten Büzgülü)
        /// ile dolu torba aynı çerçevede tabanları hizalı. Doluluk m (0–1) ile boş torba dolunun boyuna gerilir, dolu torba boşun
        /// boyundan büyür; ikisi ortada karışır. Ağız merkezi, ağız genişliği ve yığın birlikte kayar. Ölçüler 512 px dokudan
        /// (alt-orta pivottan, boy biriminde).
        /// </summary>
        private static readonly Vector2 BagMouth = new Vector2(0.008f, 0.80f), BagMouthEmpty = new Vector2(0f, 0.535f);
        /// <summary>Boş torbanın dolu torbaya oranı (genişlik, yükseklik) ve ağız oyuğunun oranı (yatay, dikey).</summary>
        private const float EmptyW = 0.787f, EmptyH = 0.617f, MouthEmptyX = 0.79f, MouthEmptyY = 0.46f;
        private const float HeapStone = 0.105f;

        /// <summary>Görsel kapasite: kova en çok bu kadar taşta "dolu" görünür (rengin kalan görevi daha azsa o kadar).</summary>
        public const int BucketVisualCap = 30; // 2026-11-23: görevler 50–60 taş; torba ~30 taşta tam şişer
        private bool _bucketMode;

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
        /// <summary>
        /// Torbanın yeri (kullanıcı 2026-11-23, çizim: tepsinin üst kenarı ortada basamaklı oyuk, torba oyuğun içinde): torbanın
        /// dünya tabanı ve dünya yüksekliği her karede buradan (tahta yerleşimi değişse de oturur). Atanmamışsa güç bandının ortası.
        /// </summary>
        public Func<Vector3> BucketBottom;
        public Func<float> BucketWorldHeight;
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
            _goalCounts.Clear();
            _bInFlight = 0;
            _bEarly = 0;
            _bAnim = -1f;
            _bTarget = null;
            if (_bucketMode) { ShowBucket(run.Active, run.Color, run.Need, run.Have, true); PlaceBucket(); }
            LayoutBottom();
        }


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
            if (_bActive) SparkAt(BucketTarget(), _bColor, 5);
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
        public Vector3 BucketTarget()
        {
            if (_bagArt == null) return Vector3.zero;
            var m = Vector2.Lerp(BagMouthEmpty, BagMouth, _bagMorph); // torbanın ağzı (şişmeyle yükselir)
            return _bagArt.TransformPoint(new Vector3(m.x * BucketSize, m.y * BucketSize, 0f));
        }

        /// <summary>Testler: gösterilen şişme (0 boş, 1 dolu).</summary>
        public float BucketShownSwell => _bagMorph;

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
            // Gönderim yalnız oyuncunun dokunuşuyla (boş torba da); hemen oynar.
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
            TickGoalCounts(dt);
            PlaceBucket();
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
                        BucketDelivered?.Invoke(); // görev sayacı torba simgeye varınca (son torba dahil)
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

            // Dolum (2026-11-23 torba): torba şişer, ağızda taş yığını yükselir (sayı yok).
            float want = FillOf(_bHave, _bNeed);
            _bFillShown = Mathf.MoveTowards(_bFillShown, want, dt * 1.6f);
            ApplyBagShape(dt);

            // Torbadaki taş sayısı: değişince rozet zıplar.
            if (_bagCountBubble != null)
            {
                _bagCountBubble.gameObject.SetActive(_bActive);
                if (_bHave != _bagCountShown)
                {
                    if (_bagCountShown >= 0 && _bHave > _bagCountShown) _bagCountPunch = 0f;
                    _bagCountShown = _bHave;
                    _bagCountText.text = _bHave.ToString();
                }
                if (_bagCountPunch < 1f) _bagCountPunch += dt / 0.2f;
                float cp = 1f + 0.25f * Mathf.Sin(Mathf.Clamp01(_bagCountPunch) * Mathf.PI);
                _bagCountBubble.transform.localScale = Vector3.one * cp;
            }

            // Yarım gönderim ipucu: kovada taş varken "Gönder" etiketi nefes alır.
            bool canSend = _bActive && _bAnim < 0f; // boş torba da gönderilir
            _bucketSendTag.gameObject.SetActive(canSend);
            if (canSend) _bucketSendTag.transform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f));
            _bucketGlow.color = new Color(_bucketGlow.color.r, _bucketGlow.color.g, _bucketGlow.color.b, _bActive ? 0.55f + 0.2f * Mathf.Sin(Time.unscaledTime * 2.2f) : 0f);

            // Eşitleme: uçuş ve gönderim bitince (ya da taş kaybolduysa 1,5 sn sonra) mantık durumuna.
            if (_bTarget != null && _bAnim < 0f && _bSends.Count == 0)
            {
                _bSettleWait += dt;
                if (_bInFlight == 0 || _bSettleWait > 3f)
                {
                    var t = _bTarget;
                    _bTarget = null;
                    _bInFlight = 0;
                    if (t.Active != _bActive || t.Color != _bColor || t.Need != _bNeed || t.Have != _bHave) ShowBucket(t.Active, t.Color, t.Need, t.Have, true);
                }
            }
        }

        /// <summary>Şişme, karışım, ağız, rozet ve yığın: gösterilen dolumdan (ShowBucket da hemen uygular; ilk kare doğru görünür).</summary>
        private void ApplyBagShape(float dt)
        {
            if (_bagArt == null) return;
            float m = _bActive ? _bFillShown : 0f;
            _bagMorph = m;
            float mix = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.75f, m));
            float fx = Mathf.Lerp(EmptyW, 1f, m), fy = Mathf.Lerp(EmptyH, 1f, m);
            for (int i = 0; i < 3; i++)
            {
                _bagFull[i].rectTransform.localScale = new Vector3(fx, fy, 1f);
                _bagEmpty[i].rectTransform.localScale = new Vector3(fx / EmptyW, fy / EmptyH, 1f);
                var cf = i == 2 ? _bagTint : Color.white;
                _bagFull[i].color = new Color(cf.r, cf.g, cf.b, mix);
                _bagEmpty[i].color = new Color(cf.r, cf.g, cf.b, 1f - mix);
            }
            var mouth = Vector2.Lerp(BagMouthEmpty, BagMouth, m);
            float spX = Mathf.Lerp(MouthEmptyX, 1f, m), spY = Mathf.Lerp(MouthEmptyY, 1f, m), chip = Mathf.Lerp(0.8f, 1f, m);
            int show = _bActive && _bHave > 0 ? Mathf.Clamp(Mathf.CeilToInt(_bFillShown * HeapSlots.Length - 0.001f), 1, HeapSlots.Length) : 0;
            for (int i = 0; i < _bagHeap.Length; i++)
            {
                bool on = i < show;
                var h = _bagHeap[i];
                if (on != h.gameObject.activeSelf)
                {
                    h.gameObject.SetActive(on);
                    _bagPop[i] = on ? 0f : 1f;
                }
                if (!on) continue;
                if (_bagPop[i] < 1f) _bagPop[i] = Mathf.Min(1f, _bagPop[i] + dt / 0.28f);
                float p = _bagPop[i];
                float k = Motion.Reduced ? 1f : 1f + 2.4f * Mathf.Pow(p - 1f, 3f) + 1.4f * Mathf.Pow(p - 1f, 2f);
                h.rectTransform.localScale = Vector3.one * (k * HeapScale[i] * chip);
                var slot = HeapSlots[i];
                h.rectTransform.anchoredPosition = new Vector2((mouth.x + slot.x * spX) * BucketSize, (mouth.y + slot.y * spY) * BucketSize + (1f - p) * BucketSize * 0.18f);
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
            _bucketSendTag.gameObject.SetActive(active);
            if (active)
            {
                var c = (Color)LayerPalette.FrontColor(color);
                _bagTint = Color.Lerp(c, Color.white, 0.12f);
                _bucketGlow.color = new Color(c.r, c.g, c.b, 0.6f);
                var icon = GoalChip.IconFor(new GoalState { Type = GoalType.CollectColor, Color = color });
                _bagBadge.sprite = icon;
                foreach (var h in _bagHeap) h.color = c;
            }
            ApplyBagShape(0f);
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
            // Torba tahtadaki oyukta: güç bandının ölçeğinden bağımsız olsun diye doğrudan kanvasa (bandın üst öğesinin hemen
            // ardına; paneller yine üstte).
            var canvas = _canvas != null ? _canvas : _skillBar.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var root = canvas.transform;
                Transform anc = _skillBar;
                while (anc.parent != null && anc.parent != root) anc = anc.parent;
                _bucketRoot.SetParent(root, false);
                if (anc.parent == root) _bucketRoot.SetSiblingIndex(anc.GetSiblingIndex() + 1);
            }
            _bucketRoot.anchorMin = _bucketRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _bucketRoot.sizeDelta = new Vector2(BucketSize, BucketSize);
            _bucketBody = UiFactory.Rect("Body", _bucketRoot);
            UiFactory.Place(_bucketBody, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BucketSize, BucketSize));

            _bucketGlow = UiFactory.Image("Glow", _bucketBody, SpecialArt.Glow, new Color(1f, 1f, 1f, 0.5f));
            _bucketGlow.raycastTarget = false;
            UiFactory.Place(_bucketGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), Vector2.one * BucketSize * 1.55f);

            // Dokunma alanı (görünmez); görsel katmanlar alttan büyüyen "Art" içinde (şişme ölçeği basma geri bildirimine karışmaz).
            var btn = UiFactory.SpriteButton("BucketButton", _bucketBody, null, new Vector2(BucketSize, BucketSize), () => BucketPressed?.Invoke());
            UiFactory.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BucketSize, BucketSize));
            btn.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            PressFeedback.Attach(btn, null);

            _bagArt = UiFactory.Rect("Art", btn.transform);
            _bagArt.anchorMin = _bagArt.anchorMax = new Vector2(0.5f, 0f);
            _bagArt.pivot = new Vector2(0.5f, 0f);
            _bagArt.anchoredPosition = Vector2.zero;
            _bagArt.sizeDelta = new Vector2(BucketSize, BucketSize);

            _bagEmpty = new Image[3];
            _bagFull = new Image[3];
            _bagEmpty[0] = BagLayer("EmptyBack", BagArt.EmptyBack);
            _bagFull[0] = BagLayer("Back", BagArt.Back);
            var heap = UiFactory.Rect("Heap", _bagArt);
            heap.anchorMin = heap.anchorMax = heap.pivot = new Vector2(0.5f, 0f);
            heap.anchoredPosition = Vector2.zero;
            heap.sizeDelta = Vector2.zero;
            _bagHeap = new Image[HeapSlots.Length];
            _bagPop = new float[HeapSlots.Length];
            for (int i = 0; i < HeapSlots.Length; i++)
            {
                var h = UiFactory.Image("Chip" + i, heap, StoneShatter.Chip(i), Color.white);
                h.raycastTarget = false;
                h.preserveAspect = true;
                h.rectTransform.anchorMin = h.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                h.rectTransform.sizeDelta = Vector2.one * BucketSize * HeapStone;
                h.rectTransform.localRotation = Quaternion.Euler(0f, 0f, HeapSpin[i]);
                h.gameObject.SetActive(false);
                _bagHeap[i] = h;
            }
            _bagEmpty[1] = BagLayer("EmptyFront", BagArt.EmptyFront);
            _bagFull[1] = BagLayer("Front", BagArt.Front);
            _bagEmpty[2] = BagLayer("EmptyRibbon", BagArt.EmptyRibbon);
            _bagFull[2] = BagLayer("Ribbon", BagArt.Ribbon);

            // "Gönder" etiketi torbanın karnında (torba tahtadaki oyukta; üstü görev şeridi, altı taşlar). Solunda rengin taşı
            // (boş torbada da renk okunur; inci kurdelesi kumaşta az seçilir).
            _bucketSendTag = UiFactory.Image("SendTag", _bucketBody, Ui3.Rounded(18f, Ui3.Hex("#FFD86A"), Ui3.Hex("#F0A92E"), 2f, Ui3.Hex("#9A6A1C")), Color.white);
            _bucketSendTag.raycastTarget = false;
            UiFactory.Place(_bucketSendTag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -BucketSize * 0.36f), new Vector2(124f, 36f));
            _bagBadge = UiFactory.Image("Badge", _bucketSendTag.transform, null, Color.white);
            _bagBadge.raycastTarget = false;
            _bagBadge.preserveAspect = true;
            UiFactory.Place(_bagBadge.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 0f), Vector2.one * 40f);
            _bucketSendText = UiFactory.Title("Text", _bucketSendTag.transform, Loc.T("bucket.send"), 22, new Color32(0x5A, 0x34, 0x08, 0xFF), new Color(1f, 1f, 1f, 0f));
            _bucketSendText.raycastTarget = false;
            UiFactory.Stretch(_bucketSendText.rectTransform);
            _bucketSendText.rectTransform.offsetMin = new Vector2(30f, 0f);

            // Torbadaki taş sayısı (kullanıcı 2026-11-24: "Torbanın içindeki taş sayısı görünsün"): sağ üstte yuvarlak rozet.
            _bagCountBubble = UiFactory.Image("Count", _bucketBody, Ui3.Rounded(30f, Ui3.Hex("#FFFFFF"), Ui3.Hex("#F3E6CF"), 3f, Ui3.Hex("#9A6A1C")), Color.white);
            _bagCountBubble.raycastTarget = false;
            UiFactory.Place(_bagCountBubble.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(BucketSize * 0.36f, BucketSize * 0.2f), new Vector2(66f, 54f));
            _bagCountText = UiFactory.Title("Text", _bagCountBubble.transform, "0", 34, new Color32(0x5A, 0x34, 0x08, 0xFF), new Color(1f, 1f, 1f, 0f));
            _bagCountText.raycastTarget = false;
            UiFactory.Stretch(_bagCountText.rectTransform);
        }

        /// <summary>Torbayı tahtadaki oyuğa oturtur: tabanı oyuğun dibinde, boyu verilen dünya yüksekliğinde.</summary>
        private void PlaceBucket()
        {
            if (BucketBottom == null || BucketWorldHeight == null) { PlaceBucketInBand(); return; }
            float h = BucketWorldHeight();
            if (h <= 0f) return;
            // Tahta dünyası → ekran pikseli (kamera) → kök kanvas birimi (ölçek çarpanı). Kanvasın dünya dönüşümüne dayanmaz
            // (ekran yakalamada ve ilk karede kanvas henüz kameraya oturmamış olabilir).
            var cam = _canvas != null ? _canvas.worldCamera : null;
            if (cam == null || _bucketRoot.parent != _canvas.transform) return;
            float sf = Mathf.Max(0.0001f, _canvas.scaleFactor);
            var b = BucketBottom();
            Vector2 lo = cam.WorldToScreenPoint(b), hi = cam.WorldToScreenPoint(b + Vector3.up * h);
            var center = new Vector2(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f);
            float hc = (hi.y - lo.y) / sf;
            if (hc <= 0f) return;
            _bucketRoot.anchorMin = _bucketRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _bucketRoot.localScale = Vector3.one * (hc / BucketSize);
            _bucketRoot.anchoredPosition = (lo - center) / sf + new Vector2(0f, hc * 0.5f);
        }

        /// <summary>
        /// 2026-11-26: torba üst güç bandının ortasında (roket ve Altın Rulo yuvalarının yeri). Boyu bandın yüksekliğinden: tam şişmiş
        /// torba ve %12 zıplama bandın içinde kalır (ne hedef kartına ne tahtaya taşar).
        /// </summary>
        private void PlaceBucketInBand()
        {
            if (_skillBar == null || _bucketRoot == null || !(_bucketRoot.parent is RectTransform parent)) return;
            var c = new Vector3[4];
            _skillBar.GetWorldCorners(c);
            Vector3 lo = parent.InverseTransformPoint(c[0]), hi = parent.InverseTransformPoint(c[2]);
            float h = (hi.y - lo.y) * BagBandFill;
            if (h <= 1f) return;
            _bucketRoot.anchorMin = _bucketRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _bucketRoot.localScale = Vector3.one * (h / BucketSize);
            _bucketRoot.localPosition = new Vector3((lo.x + hi.x) * 0.5f, (lo.y + hi.y) * 0.5f, 0f);
        }

        /// <summary>Denetim: torbanın tuvaldeki (kök) dikdörtgeni ve bandın dikdörtgeni (aynı uzayda).</summary>
        public bool AuditBagInBand(out Rect bag, out Rect band)
        {
            bag = band = Rect.zero;
            if (!_bucketMode || _bucketRoot == null || _skillBar == null || !(_bucketRoot.parent is RectTransform parent)) return false;
            var c = new Vector3[4];
            _skillBar.GetWorldCorners(c);
            Vector3 lo = parent.InverseTransformPoint(c[0]), hi = parent.InverseTransformPoint(c[2]);
            band = Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
            float s = _bucketRoot.localScale.y * 1.12f; // en büyük zıplama
            var p = _bucketRoot.localPosition;
            // 2026-11-26: yatayda sayı rozeti (sağ üst) gövdeden taşar; denetim onu da kapsar.
            float hw = BagHalfWidthUnits * s;
            bag = new Rect(p.x - hw, p.y - BucketSize * s * 0.5f, 2f * hw, BucketSize * s);
            return true;
        }

        /// <summary>
        /// 2026-11-26 denetim (kullanıcı: "Bantta, iki yanında yuvalar"): tepsi yuvalarının en geniş görünür alanı (seçili roket simgesi
        /// boyu) torbayla aynı uzayda. Torba kipinde değilse false.
        /// </summary>
        public bool AuditTraySlots(List<Rect> slots)
        {
            slots.Clear();
            if (!_bucketMode || _bucketRoot == null || _trayRoot == null || !_trayRoot.gameObject.activeInHierarchy || !(_bucketRoot.parent is RectTransform parent)) return false;
            for (int i = 0; i < LevelState.TrayCapacity; i++)
            {
                var srt = (RectTransform)_traySlots[i].transform;
                var c = parent.InverseTransformPoint(srt.position);
                // Yuvanın kendi ölçeği (seçim dahil değil) → en geniş simge seçili haliyle.
                var w = new Vector3[4];
                srt.GetWorldCorners(w);
                float slotW = (parent.InverseTransformPoint(w[2]).x - parent.InverseTransformPoint(w[0]).x) / (i == _traySelected ? TraySelScale : 1f);
                float half = slotW * 1.6f * 0.5f * TraySelScale;
                slots.Add(new Rect(c.x - half, c.y - half, 2f * half, 2f * half));
            }
            return true;
        }

        private Image BagLayer(string name, Sprite sprite)
        {
            var img = UiFactory.Image(name, _bagArt, sprite, Color.white);
            img.raycastTarget = false;
            UiFactory.Stretch(img.rectTransform);
            img.rectTransform.pivot = new Vector2(0.5f, 0f); // şişme tabandan
            return img;
        }
    }

    /// <summary>Kova kıvılcımı: dağılır, yavaşlar, söner.</summary>
    public sealed class BucketSpark : MonoBehaviour
    {
        private Vector2 _v;
        private float _t;
        private Image _img;

        public void Init(Vector2 v) { _v = v; _img = GetComponent<Image>(); }

        private void Update() => Step(Time.unscaledDeltaTime);

        /// <summary>Bir adım ilerletir (oyunda Update; ekran yakalamada oynatma dışı olduğundan <see cref="StepAll"/> ile).</summary>
        public void Step(float dt)
        {
            _t += dt;
            ((RectTransform)transform).anchoredPosition += _v * dt;
            _v *= Mathf.Exp(-3.5f * dt);
            if (_img != null) { var c = _img.color; c.a = Mathf.Clamp01(1f - _t / 0.6f); _img.color = c; }
            if (_t > 0.6f) { if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject); }
        }

        /// <summary>2026-11-28 reklam çekimi: oynatma dışında Update çalışmaz; kıvılcımlar torbanın üstünde yığılı kalıyordu.</summary>
        public static void StepAll(float dt)
        {
            foreach (var s in FindObjectsByType<BucketSpark>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (s != null) s.Step(dt);
        }
    }

    /// <summary>Torba görselleri (Resources/Bag, kullanıcının ürettiği torbadan ayrıldı; kaynak docs/varliklar/gorseller/torba).</summary>
    internal static class BagArt
    {
        private static Sprite _back, _front, _ribbon, _eBack, _eFront, _eRibbon;
        public static Sprite EmptyBack => _eBack ? _eBack : (_eBack = Resources.Load<Sprite>("Bag/bagempty-back"));
        public static Sprite EmptyFront => _eFront ? _eFront : (_eFront = Resources.Load<Sprite>("Bag/bagempty-front"));
        public static Sprite EmptyRibbon => _eRibbon ? _eRibbon : (_eRibbon = Resources.Load<Sprite>("Bag/bagempty-ribbon"));
        public static Sprite Back => _back ? _back : (_back = Resources.Load<Sprite>("Bag/bag-back"));
        public static Sprite Front => _front ? _front : (_front = Resources.Load<Sprite>("Bag/bag-front"));
        public static Sprite Ribbon => _ribbon ? _ribbon : (_ribbon = Resources.Load<Sprite>("Bag/bag-ribbon"));
    }
}
