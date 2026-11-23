using System;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Tek yığının tek katman "soyulması". v10 (kullanıcı, 2026-11-07): kâğıt gibi kıvrılma yerine TAŞ KIRILMASI.
    /// 2026-11-19 (kullanıcı: "ilk patlatmada parçalanacak. Yavaşça havaya kalkacaklar ve taş bütün ve büyük hale
    /// gelecek havadan ise görev çubuğuna hangi taşsa o ve yıldıza yıldız akacak. Kombolarda çok daha fazla taş akacak"):
    /// dokunuşta sıkışma ve çatlak → taş kendi renginde parçalara ayrılır (kısa dışa patlama, toz) → parçalar yavaşça
    /// yükselip taşın üstünde toplanır → orada BÜTÜN ve BÜYÜK taş belirir (HUD hedef simgesiyle aynı çizim) → kısa
    /// salınım → görev rengiyse taş yay çizerek görev simgesine uçar, aynı anda altın yıldızlar yıldız çubuğuna akar;
    /// görev değilse taş yıldızlara dönüşür (daha çok yıldız). Kombo/büyük hamlede <c>stones</c> kadar taş akar.
    /// Zamanın saf fonksiyonudur (Evaluate(ms) pozu baştan hesaplar; kare yakalama ve yavaş çekim aynı kod); yalnız
    /// toplanma parlaması parçacık sistemine bir kez gönderilir. Zincir zamanlaması (ReadableAt, BreakAt) değişmez;
    /// hedefe varış <see cref="ArriveAt"/>. Kural çalıştırmaz.
    /// </summary>
    public sealed class PeelSequence
    {
        /// <summary>
        /// Genel süre çarpanı. vr1 P1 prototipi (04_ANIMASYON_GORSEL_SES "Animasyon tablosu", taslak): anahtar açıkken 0,6
        /// (soyma ~246 ms, okunur an 180 ms); kapalıyken eski 1,4 (soyma 574 ms, okunur an 420 ms). Sıra ve oranlar aynı.
        /// </summary>
        public static float Slow => KatKat.UI.Vr1Proto.On ? 0.6f : 1.4f;
        /// <summary>Dokunuş sıkışması: prototipte en az 1 − 0,04 (tablo: 1 → 0,96 → 1), eskisi 0,94.</summary>
        /// <summary>
        /// Kırılma sonrası akışın (yükselme, toplanma, uçuş) süre çarpanı; vr1 hızından bağımsız (2026-11-19, kullanıcı:
        /// "çok hızlı akıyor"). Akış girdiyi kilitlemez (<see cref="StackDoneAt"/>).
        /// </summary>
        public static float Flow => 1.4f;
        public static float SquashDepth => KatKat.UI.Vr1Proto.On ? 0.04f : 0.06f;
        /// <summary>Dokunuş sıkışması süresi: prototipte tablodaki 90 ms (soyma çarpanından bağımsız; kırılma 90 ms'de, kalkış
        /// bitişinden önce); kapalıyken eski 126 ms.</summary>
        public static float AnticipationEnd => KatKat.UI.Vr1Proto.On ? 90f : 90f * Slow;
        public static float LiftStart => 50f * Slow;
        public static float LiftEnd => 160f * Slow;
        public static float PeelEnd => 410f * Slow;
        public static float BounceStart => 360f * Slow;
        public static float BounceEnd => 560f * Slow;
        public static float ReleaseStart => 400f * Slow;
        /// <summary>Eski (2026-11-19 öncesi) parça varış anı; yeni akışta <see cref="ArriveAt"/> kullanılır.</summary>
        public static float ReleaseEnd => 640f * Slow;
        /// <summary>Alt renk okunur hâle geldiği an: zincirin sonraki dalgası buradan başlayabilir.</summary>
        public static float ReadableAt => 300f * Slow;

        /// <summary>Kırılma anı: çatlak tamamlanır, yüz parçalara ayrılır. Tahta sarsıntısı da bu anda (BoardView).</summary>
        public static float BreakAt => AnticipationEnd;
        /// <summary>
        /// Yalnız mağaza bombası (kullanıcı, 2026-11-21: "O sadece güçlendiricideki bombada olacak. Bomba kullanıldığı anda
        /// hemen patlama veya tahtanın sarsılması olmayacak. Taşlar yukarı çıkıp titreyecek ve patlayacaklar o anda tüm
        /// animasyonlar devreye girerek tahta sarsılacak kıvılcım alevler çıkacak"): <see cref="Charged"/> taş önce kendi
        /// yerinde yükselir (0–<see cref="ChargeRiseEnd"/>), havada artan titremeyle ışık saçar ve <see cref="ChargeMs"/> anında
        /// patlar; sonrası normal akışın <see cref="BreakAt"/>'ten sonrası (<see cref="ChargeShift"/> kadar kayık).
        /// </summary>
        public static float ChargeRiseEnd => 300f * Flow;
        public static float ChargeMs => ChargeRiseEnd + 230f * Flow;
        /// <summary>Yüklenmiş taşın zaman çizgisinin normal akışa göre kayması: yüklü taşta patlama <c>ChargeMs</c>'te,
        /// normal taşta <c>BreakAt</c>'te.</summary>
        public static float ChargeShift => ChargeMs - BreakAt;
        /// <summary>Havada yükselme yüksekliği ve büyüme (taş boyu oranı).</summary>
        public const float LiftHeight = 0.85f, LiftGrow = 0.14f;
        /// <summary>Parçaların kısa dışa patlamasının sonu; ardından yavaşça yükselirler.</summary>
        public static float BurstEnd => BreakAt + 60f * Flow;
        /// <summary>Parçaların havadaki noktada toplanma sonu.</summary>
        public static float GatherAt => 520f * Flow;
        /// <summary>Bütün taşın belirmeye başladığı an ve büyüme süresi.</summary>
        public static float FormStart => 430f * Flow;
        public static float FormDur => 140f * Flow;
        /// <summary>Taşın (ve yıldızların) hedefe uçuşa başladığı an.</summary>
        public static float FlyStart => 620f * Flow;
        /// <summary>Taşlar ve yıldızlar hedefe varır (HUD sayacı bu anda güncellenir).</summary>
        public static float ArriveAt => 960f * Flow;
        /// <summary>2026-11-22 (kullanıcı: "Görev taşları hedefe gittikten sonra hedeften yıldıza akış olsun"): görev taşının
        /// yıldızları hedefe varıştan sonra görev simgesinden yıldız çubuğuna akar; bu ek süre.</summary>
        public static float StarLegMs => 330f * Flow;
        public static float Duration => ArriveAt + StarLegMs;
        /// <summary>2026-11-22 (kullanıcı: "direk oldukları yerden biraz yükselip titreyip hedefe gitsinler"): taş kendi yerinin
        /// bu kadar (taş boyu) üstünde belirir, titrer, oradan hedefe uçar (eski ortak toplanma noktası yok).</summary>
        public const float RiseInPlace = 0.55f;
        /// <summary>Kombo taşları arasındaki gecikme.</summary>
        public static float StoneLag => 45f * Flow;
        /// <summary>Yığın tarafının bittiği an (sekme sonu): bundan sonra tahta serbest, yalnız uçuş sürer.</summary>
        public static float StackDoneAt => BounceEnd;

        /// <summary>Mağaza bombasının yüklenme evresi (yükselme + titreme) bu taşta oynar; bkz. <see cref="ChargeMs"/>.</summary>
        public bool Charged { get; set; }

        /// <summary>
        /// 2026-11-23 kova kipi (kullanıcı: "taşlar hiç kırılmadan direk kovaya gidecek"): taş kırılmaz, çatlak/parça/toz yok; yığından
        /// bütün olarak sıçrar ve yay çizerek hedefe (kova ağzı) uçar, küçülerek girer. Varış <see cref="ArriveAt"/>'te (sesler ve
        /// sayaçlar aynı zaman çizelgesinde).
        /// </summary>
        public bool Intact { get; set; }
        /// <summary>Toplanma noktası: kaynaktan görev simgesine doğru yatayda %65, dikeyde %50, en az 2,2 taş yukarı (aynı renk bir araya gelir, hedefe uzun uçuş kalır).</summary>
        private const float GatherX = 0.65f, GatherY = 0.5f;

        public const int ShardCount = 9, DustCount = 4, MaxStones = 4;
        private const float BigScale = 1.08f, IconScale = 0.6f, ExtraScale = 0.8f; // 2026-11-22: yerinde yükselen taş komşularını örtmesin (1,35 → 1,08)

        private readonly StackView _stack;
        private readonly PeelSheet _sheet;
        private readonly Vector3 _goalWorld, _starWorld;
        private readonly int _layerIndex, _order, _stoneCount, _starCount;
        private readonly bool _toGoal;
        private readonly Action<PeelSequence> _onArrive;
        private readonly Transform _fxParent;
        private readonly Color _color;
        private readonly Sprite _stoneSprite, _starSprite;
        private bool _rimReleased, _arrived, _broken, _stackDone;
        private readonly float _size0;
        private Vector3 _origin;
        private SpriteRenderer _crack;
        private readonly SpriteRenderer[] _shards = new SpriteRenderer[ShardCount];
        private readonly SpriteRenderer[] _dust = new SpriteRenderer[DustCount];
        private readonly SpriteRenderer[] _stones = new SpriteRenderer[MaxStones];
        private readonly bool[] _glowed = new bool[MaxStones];
        private SpriteRenderer[] _stars;
        // Işıltı (2026-11-19, kullanıcı: "taşlar ve hedefler daha ışıltılı olsun"): eklemeli hale, kenar ışığı, iz, varış patlaması.
        private readonly SpriteRenderer[] _halos = new SpriteRenderer[MaxStones], _rims = new SpriteRenderer[MaxStones], _cores = new SpriteRenderer[MaxStones];
        private readonly float[] _trailMs = new float[MaxStones];
        private readonly bool[] _landed = new bool[MaxStones];
        private SpriteRenderer[] _starHalos;
        private SpriteRenderer _preGlow, _preCore;
        private float _sparkMs = -1000f;
        private float[] _starTrailMs;
        private bool[] _starLanded;
        private static readonly System.Collections.Generic.Stack<SpriteRenderer> _haloPool = new System.Collections.Generic.Stack<SpriteRenderer>();
        // Belirlenimli parametreler (yığın konumundan tohum).
        private readonly Vector3[] _p0 = new Vector3[ShardCount], _out = new Vector3[ShardCount], _gatherOff = new Vector3[ShardCount];
        private readonly float[] _spin = new float[ShardCount], _size = new float[ShardCount], _lag = new float[ShardCount];
        private readonly Vector3[] _stoneOff = new Vector3[MaxStones];
        // Ortak toplanma noktası (ekranın ortası, tahtanın biraz üstü; 2026-11-19 üçüncü tur) ve bu taşın kümedeki yeri.
        private readonly Vector3? _gatherWorld;
        private Vector3 _cluster;
        private readonly float[] _stoneArc = new float[MaxStones];
        private Vector3[] _starOff;
        private float[] _starArc, _starSpin;

        public LayerColor Color { get; }
        public bool IsDone { get; private set; }

        public PeelSequence(StackView stack, PeelSheet sheet, Vector3 goalWorld, int sortingOrder, Action<PeelSequence> onArrive)
            : this(stack, sheet, goalWorld, goalWorld, 1f, sortingOrder, onArrive, null) { }

        /// <param name="goalShare">0: görev rengi değil (taş yıldızlara dönüşür); &gt;0: taş görev simgesine uçar.</param>
        /// <param name="fxParent">Parçaların dünya uzayındaki üst nesnesi (yönetmen).</param>
        /// <param name="stones">Akan taş sayısı (1–4): kombo ve büyük hamlede artar.</param>
        public PeelSequence(StackView stack, PeelSheet sheet, Vector3 goalWorld, Vector3 starWorld, float goalShare, int sortingOrder,
            Action<PeelSequence> onArrive, Transform fxParent, int stones = 1, Vector3? gatherWorld = null)
        {
            _stack = stack;
            _sheet = sheet;
            _goalWorld = goalWorld;
            _starWorld = starWorld;
            _onArrive = onArrive;
            _order = sortingOrder;
            _fxParent = fxParent != null ? fxParent : stack.transform.parent;
            _layerIndex = stack.Layers.Count - 1;
            _size0 = stack.Size;
            Color = stack.DetachTop(this);
            // Parça rengi (kullanıcı, 2026-11-07): taşın kendi rengiyle aynı (yüz rengi); yüzey gölgelemesi sprite'ta.
            _color = LayerPalette.FrontColor(Color);
            sheet.transform.SetParent(stack.transform, false);
            sheet.Begin(stack.Size, LayerPalette.FrontColor(Color), LayerPalette.IconCell(Color), sortingOrder);

            _toGoal = goalShare > 0f;
            _gatherWorld = gatherWorld;
            _stoneCount = Mathf.Clamp(stones, 1, MaxStones);
            _starCount = (_toGoal ? 2 : 4) + 2 * (_stoneCount - 1);
            _stoneSprite = PremiumArt.Chip(Color) ?? KatKat.UI.UiArt.Chip(Color);
            _starSprite = PremiumArt.V4("icons/star") ?? PremiumArt.Get("icon_star") ?? ProceduralArt.StarSprite;

            var p = stack.transform.position;
            var rng = new System.Random(Mathf.RoundToInt(p.x * 97f) * 7919 + Mathf.RoundToInt(p.y * 89f) * 31 + _layerIndex * 13 + 5);
            float Rnd() => (float)rng.NextDouble();
            float s = stack.Size;
            // Ortak kümede bu taşın yeri: hepsi aynı noktaya yığılmasın, yuvarlak bir küme olsun.
            _cluster = new Vector3((Rnd() - 0.5f) * 1.8f * s, (Rnd() - 0.5f) * 1.1f * s, 0f);
            for (int i = 0; i < ShardCount; i++)
            {
                // 3×3 ızgarada sapmalı başlangıç (yüzü kaplar), merkezden dışa kısa patlama.
                float gx = (i % 3 - 1) * 0.30f + (Rnd() - 0.5f) * 0.12f;
                float gy = (i / 3 - 1) * 0.30f + (Rnd() - 0.5f) * 0.12f;
                _p0[i] = new Vector3(gx * s, gy * s, 0f);
                var dir = new Vector3(gx, gy + 0.1f, 0f).normalized;
                _out[i] = dir * (0.35f + Rnd() * 0.3f) * s;
                // Toplanma noktası çevresinde hafif dağınık son konum (taşın içine girer).
                _gatherOff[i] = new Vector3(gx * 0.35f, gy * 0.35f, 0f) * s;
                _spin[i] = (Rnd() - 0.5f) * 540f;
                _size[i] = s * (0.36f + Rnd() * 0.16f);
                _lag[i] = Rnd() * 50f * Flow;
            }
            for (int k = 0; k < _stoneCount; k++)
            {
                // Ana taş tam üstte; kombo taşları iki yana kayık, biraz aşağıda (akış gibi).
                float side = k == 0 ? 0f : ((k % 2 == 1) ? -1f : 1f) * (0.32f + 0.12f * ((k + 1) / 2));
                _stoneOff[k] = new Vector3(side * s, -0.12f * k * s, 0f);
                _stoneArc[k] = (Rnd() - 0.5f) * 0.8f * s;
            }
            _stars = new SpriteRenderer[_starCount];
            _starHalos = new SpriteRenderer[_starCount];
            _starTrailMs = new float[_starCount];
            _starLanded = new bool[_starCount];
            for (int j = 0; j < _starCount; j++) _starTrailMs[j] = -1000f;
            for (int k = 0; k < MaxStones; k++) _trailMs[k] = -1000f;
            _starOff = new Vector3[_starCount];
            _starArc = new float[_starCount];
            _starSpin = new float[_starCount];
            for (int j = 0; j < _starCount; j++)
            {
                float a = (j + Rnd() * 0.5f) / _starCount * Mathf.PI * 2f;
                _starOff[j] = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.7f, 0f) * s * (0.25f + Rnd() * 0.3f);
                _starArc[j] = (Rnd() - 0.5f) * 1.6f * s;
                _starSpin[j] = (Rnd() - 0.5f) * 360f;
            }
            Evaluate(0f);
        }

        public void Evaluate(float ms)
        {
            if (IsDone) return;
            float size = _size0;
            if (Intact) { IntactFlight(ms, size); return; }

            // Mağaza bombası: önce yükselip titrer; patlamadan sonra normal akış ChargeShift kadar kayık sürer.
            if (Charged)
            {
                if (ms < ChargeMs)
                {
                    _stack.SetBodyScale(this, Vector2.one);
                    ChargePose(ms, size);
                    return;
                }
                ms -= ChargeShift;
            }

            // Yığın işi bitti: kalan yalnız havadaki taş/yıldız uçuşu (yığına artık dokunulmaz; tahta yeniden çizilebilir).
            if (_stackDone)
            {
                Flight(ms, size);
                return;
            }

            // Gövde: dokunuşta hafif sıkışma; alt katman açılınca tek yumuşak sekme.
            var body = Vector2.one;
            if (ms < AnticipationEnd)
            {
                float k = Mathf.Sin(Mathf.PI * ms / AnticipationEnd) * SquashDepth;
                body = new Vector2(1f + k * 0.55f, 1f - k);
            }
            else if (ms >= BounceStart && ms < BounceEnd)
            {
                float x = (ms - BounceStart) / (BounceEnd - BounceStart);
                float k = Mathf.Sin(2f * Mathf.PI * x) * (1f - x) * 0.07f;
                body = new Vector2(1f + k * 0.5f, 1f - k);
            }
            _stack.SetBodyScale(this, body);

            var faceLocal = _stack.FaceCenterLocal(_layerIndex);
            if (ms < BreakAt)
            {
                // Kırılmadan önce: yüz yerinde (düz), üstünde çatlak belirir ve sarsılır.
                var t = _sheet.transform;
                float shake = Mathf.Sin(ms * 0.35f) * 0.012f * size * Mathf.Clamp01(ms / BreakAt);
                t.localPosition = _stack.BodyLocalToRoot(faceLocal + new Vector3(shake, size * 0.5f, 0f));
                t.localScale = new Vector3(body.x, body.y, 1f);
                _sheet.Pose(0f, 0.24f * size, 0f, Vector2.zero, 1f, 0f);
                float ck = Mathf.Clamp01((ms - 25f) / (BreakAt - 40f));
                if (ck > 0f)
                {
                    if (_crack == null)
                    {
                        _crack = StoneShatter.Take(_stack.transform);
                        _crack.sprite = StoneShatter.Crack;
                        _crack.sortingOrder = _order + 1;
                    }
                    _crack.transform.localPosition = t.localPosition - new Vector3(0f, size * 0.5f, 0f) + new Vector3(0f, 0f, -0.001f);
                    _crack.transform.localScale = new Vector3(size * body.x * (0.6f + 0.4f * ck), size * body.y * (0.6f + 0.4f * ck), 1f);
                    _crack.color = new Color(1f, 1f, 1f, ck);
                }
                return;
            }

            if (!_broken) Break(Charged ? faceLocal + new Vector3(0f, LiftHeight * size, 0f) : faceLocal);

            if (ms >= StackDoneAt)
            {
                // Yığın tarafı bitti (sekme sonu): gövde ve kenar serbest; uçuş tahtadan bağımsız sürer.
                _stack.SetBodyScale(this, Vector2.one);
                _stack.ReleaseDetachedRim(this);
                _sheet.End();
                _stackDone = true;
            }
            Flight(ms, size);
        }

        /// <summary>
        /// Mağaza bombasının yüklenmesi (2026-11-21): taş kendi yerinde havaya kalkar ve büyür; havada artan titremeyle
        /// sarsılır, arkasında taş renginde ışık büyür, çevresine pırıltı saçar; üstünde çatlak belirir. Patlama yok.
        /// </summary>
        private void ChargePose(float ms, float size)
        {
            var faceLocal = _stack.FaceCenterLocal(_layerIndex);
            {
                var t = _sheet.transform;
                float rise = Mathf.Clamp01(ms / ChargeRiseEnd);
                float re = 1f - (1f - rise) * (1f - rise) * (1f - rise);
                float lift = LiftHeight * size * re;
                float tremble = Mathf.Clamp01((ms - (ChargeRiseEnd - 90f)) / (ChargeMs - ChargeRiseEnd + 90f));
                float tr = KatKat.UI.Motion.Reduced ? 0f : tremble * tremble;
                float shx = (Mathf.Sin(ms * 0.95f) + 0.5f * Mathf.Sin(ms * 1.7f + 1.3f)) * 0.045f * size * tr;
                float shy = Mathf.Sin(ms * 1.25f + 0.7f) * 0.025f * size * tr;
                float grow = 1f + LiftGrow * re + 0.05f * tr;
                t.localPosition = _stack.BodyLocalToRoot(faceLocal + new Vector3(shx, size * 0.5f + lift + shy, 0f));
                t.localScale = new Vector3(grow, grow, 1f);
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(ms * 1.1f + 0.4f) * 7f * tr);
                _sheet.Pose(0f, 0.24f * size, 0f, Vector2.zero, 1f, 0f);
                var center = _stack.transform.TransformPoint(faceLocal + new Vector3(shx, lift + shy, 0f));
                center.z -= 0.01f;
                var bright = UnityEngine.Color.Lerp(_color, UnityEngine.Color.white, 0.45f);
                float glow = Mathf.Clamp01(rise * 0.6f + tremble * 0.9f);
                float flick = 0.85f + 0.15f * Mathf.Sin(ms * 0.6f);
                Halo(ref _preGlow, FxKit.Glow, center, size * grow * (1.6f + 1.1f * tremble), new Color(bright.r, bright.g, bright.b, 0.85f * glow * flick), _order - 1);
                Halo(ref _preCore, FxKit.Glow, center, size * grow * 1.1f, new Color(1f, 1f, 1f, 0.45f * tremble * flick), _order + 4);
                var pfx = FxParticles.Active;
                if (pfx != null && !KatKat.UI.Motion.Reduced && rise > 0.3f && ms - _sparkMs >= 34f)
                {
                    _sparkMs = ms;
                    float a = Hash(_layerIndex + 3, ms) * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    pfx.Emit(FxKit.Star, true, _order + 5, center + dir * size * 0.42f * grow, dir * size * (1.2f + 1.6f * tremble), 0.38f,
                        size * (0.22f + 0.18f * tremble), size * 0.04f, UnityEngine.Color.white, new Color(bright.r, bright.g, bright.b, 0f), drag: 2.5f, rot: a * 57f);
                    if (tremble > 0.2f)
                        pfx.Emit(FxKit.Dot, true, _order + 5, center, -dir * size * 1.4f, 0.3f, size * 0.12f, size * 0.02f,
                            new Color(bright.r, bright.g, bright.b, 1f), new Color(_color.r, _color.g, _color.b, 0f), drag: 3f);
                }
                float ck = Mathf.Clamp01((ms - ChargeRiseEnd + 40f) / (ChargeMs - ChargeRiseEnd));
                if (ck > 0f)
                {
                    if (_crack == null)
                    {
                        _crack = StoneShatter.Take(_stack.transform);
                        _crack.sprite = StoneShatter.Crack;
                        _crack.sortingOrder = _order + 1;
                    }
                    _crack.transform.localPosition = t.localPosition - new Vector3(0f, size * 0.5f * grow, 0f) + new Vector3(0f, 0f, -0.001f);
                    _crack.transform.localRotation = t.localRotation;
                    _crack.transform.localScale = new Vector3(size * t.localScale.x * (0.6f + 0.4f * ck), size * t.localScale.y * (0.6f + 0.4f * ck), 1f);
                    _crack.color = new Color(1f, 1f, 1f, ck);
                }
            }
        }

        /// <summary>
        /// Kırılma sonrası (2026-11-19, ikinci tur; kullanıcı: "çok hızlı akıyor. daha yukarı gelsin ve bir araya
        /// toplanıp yerlerine gitsinler"): toplanma noktası taşın çok üstünde, görev simgesine doğru yarı yolda; aynı
        /// renkteki taşlar yataylamasına aynı yere yaklaştığı için havada bir araya kümelenir.
        /// </summary>
        private void Flight(float ms, float size)
        {
            var origin = _origin;
            var gather = origin + new Vector3(0f, RiseInPlace * size, 0f);
            float z = origin.z - 0.01f;

            Shards(ms, origin, gather, size, z);
            Dust(ms, origin, size);
            for (int k = 0; k < _stoneCount; k++) Stone(k, ms, gather, size, z);
            for (int j = 0; j < _starCount; j++) Star(j, ms, gather, size, z);

            // Hedefe varış (HUD odağı, sayaç) taşların varışında; yıldız akışı sürerken nesne yaşamaya devam eder.
            if (ms >= ArriveAt && !_arrived)
            {
                _arrived = true;
                _onArrive?.Invoke(this);
            }
            if (ms >= Duration)
            {
                if (!_stackDone)
                {
                    _stack.SetBodyScale(this, Vector2.one);
                    _stack.ReleaseDetachedRim(this);
                    _sheet.End();
                    _stackDone = true;
                }
                Cleanup();
                IsDone = true;
                if (!_arrived)
                {
                    _arrived = true;
                    _onArrive?.Invoke(this);
                }
            }
        }

        /// <summary>Parçalar: kısa sönümlü dışa patlama → yavaşça yükselip toplanma noktasına süzülme → taş belirirken sönme.</summary>
        private void Shards(float ms, Vector3 origin, Vector3 gather, float size, float z)
        {
            float burstDur = BurstEnd - BreakAt;
            for (int i = 0; i < ShardCount; i++)
            {
                var sr = _shards[i];
                if (sr == null) continue;
                float u = Mathf.Clamp01((ms - BreakAt) / burstDur);
                float uo = 1f - (1f - u) * (1f - u) * (1f - u); // hızlı çıkış, sönümlü duruş
                var burst = origin + _p0[i] + _out[i] * uo;
                float r = Mathf.Clamp01((ms - BurstEnd - _lag[i]) / (GatherAt - BurstEnd - _lag[i]));
                float e = 1f - (1f - r) * (1f - r) * (1f - r); // yükseliş: hızlı çıkış, yumuşak duruş
                var pos = Vector3.Lerp(burst, gather + _gatherOff[i], e);
                sr.transform.position = new Vector3(pos.x, pos.y, z);
                float sc = _size[i] * Mathf.Lerp(1f, 0.55f, e);
                sr.transform.localScale = new Vector3(sc, sc, 1f);
                float tSec = (ms - BreakAt) / 1000f;
                sr.transform.rotation = Quaternion.Euler(0f, 0f, _spin[i] * (tSec + 0.2f) * (1f - 0.7f * e));
                float a = 1f - Mathf.Clamp01((ms - FormStart - FormDur * 0.2f) / (FormDur * 0.6f));
                var c = _color;
                c.a = a;
                sr.color = c;
                sr.enabled = a > 0.001f;
            }
        }

        /// <summary>Toz: kısa, açık renkli bulut (taş tozu), 260 ms'de söner.</summary>
        private void Dust(float ms, Vector3 origin, float size)
        {
            float dt = (ms - BreakAt) / 260f;
            for (int i = 0; i < DustCount; i++)
            {
                var d = _dust[i];
                if (d == null) continue;
                float ang = i * Mathf.PI * 2f / DustCount + 0.4f;
                float r = size * (0.15f + 0.35f * Mathf.Clamp01(dt));
                d.transform.position = origin + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r * 0.7f, -0.005f);
                float sc = size * (0.35f + 0.5f * Mathf.Clamp01(dt));
                d.transform.localScale = new Vector3(sc, sc, 1f);
                Color dc = Color32.Lerp((Color32)_color, new Color32(255, 250, 240, 255), 0.7f);
                dc.a = Mathf.Clamp01(1f - dt) * 0.55f;
                d.color = dc;
                d.enabled = dc.a > 0.001f;
            }
        }

        /// <summary>
        /// Bütün taş <paramref name="k"/>: toplanma noktasında büyüyerek belirir, salınır; görev rengiyse yay çizerek
        /// görev simgesine uçar ve simge boyuna küçülür, değilse küçülüp söner (yıldızlara dönüşür).
        /// </summary>
        private void Stone(int k, float ms, Vector3 gather, float size, float z)
        {
            float form0 = FormStart + k * StoneLag;
            if (ms < form0)
            {
                if (_stones[k] != null) _stones[k].enabled = false;
                return;
            }
            var sr = _stones[k];
            if (sr == null)
            {
                sr = _stones[k] = StoneShatter.Take(_fxParent);
                sr.sprite = _stoneSprite != null ? _stoneSprite : StoneShatter.Chip(0);
                sr.sortingOrder = _order + 5 + (_stoneCount - k);
            }
            sr.enabled = true;
            float big = (k == 0 ? 1f : ExtraScale) * BigScale * size;
            var at = gather + _stoneOff[k];
            if (!_glowed[k])
            {
                _glowed[k] = true;
                // Toplanma parlaması: taş renginde yumuşak ışık ve birkaç kıvılcım (bir kez).
                var pfx = FxParticles.Active;
                if (pfx != null && !KatKat.UI.Motion.Reduced)
                {
                    var bright = UnityEngine.Color.Lerp(_color, UnityEngine.Color.white, 0.5f);
                    pfx.Emit(FxKit.Glow, true, _order + 4, new Vector3(at.x, at.y, z), Vector3.zero, 0.32f, big * 0.5f, big * 1.8f,
                        new Color(bright.r, bright.g, bright.b, 1f), new Color(_color.r, _color.g, _color.b, 0f));
                    pfx.Emit(FxKit.Ring, true, _order + 4, new Vector3(at.x, at.y, z), Vector3.zero, 0.3f, big * 0.3f, big * 1.4f,
                        new Color(1f, 1f, 1f, 0.7f), new Color(bright.r, bright.g, bright.b, 0f));
                }
            }

            float f = Mathf.Clamp01((ms - form0) / FormDur);
            float grow = Mathf.Lerp(0.5f, 1f, BackOut(f));
            float alpha = Mathf.Clamp01(f * 3f);
            var pos = at;
            float scale = big * grow;
            float rot = 0f;

            float fly0 = FlyStart + k * StoneLag;
            if (ms < fly0)
            {
                // Yerinde titreme: uçuşa doğru artan sarsıntı.
                float tr = Mathf.Clamp01((ms - form0) / Mathf.Max(1f, fly0 - form0));
                pos.x += Mathf.Sin(ms * 0.55f + k) * 0.035f * size * tr;
                pos.y += Mathf.Sin(ms * 0.71f + k * 2f) * 0.025f * size * tr;
                rot = Mathf.Sin(ms * 0.6f + k * 1.3f) * 6f * tr;
            }
            else if (_toGoal)
            {
                float u = Mathf.Clamp01((ms - fly0) / (ArriveAt - fly0));
                float e = u * u; // hızlanan uçuş (ease-in)
                var ctrl = Vector3.Lerp(at, _goalWorld, 0.5f) + new Vector3(_stoneArc[k], 0.9f * size, 0f);
                pos = Bezier(at, ctrl, _goalWorld, e);
                scale = Mathf.Lerp(big, IconScale * size, e);
                rot = Mathf.Lerp(0f, 20f * (k % 2 == 0 ? 1f : -1f), Mathf.Sin(u * Mathf.PI));
                alpha = u < 0.9f ? 1f : Mathf.Lerp(1f, 0f, (u - 0.9f) / 0.1f);
            }
            else
            {
                // Görev rengi değil: taş parlayıp küçülür, yerini yıldızlar alır.
                float u = Mathf.Clamp01((ms - fly0) / (140f * Flow));
                scale = big * Mathf.Lerp(1f, 0.15f, u * u);
                alpha = 1f - u;
                rot = u * 30f;
            }
            sr.transform.position = new Vector3(pos.x, pos.y, z - 0.002f * (k + 1));
            sr.transform.rotation = Quaternion.Euler(0f, 0f, rot);
            WorldSize(sr, scale);
            sr.color = new Color(1f, 1f, 1f, alpha);
            sr.enabled = alpha > 0.001f;

            // Işıltı: taş renginde nabız gibi atan yumuşak hale (arkada) ve ince açık kenar ışığı (önde).
            var hp = new Vector3(pos.x, pos.y, z);
            float pulse = 0.8f + 0.2f * Mathf.Sin(ms * 0.012f + k * 1.7f);
            var glowCol = UnityEngine.Color.Lerp(_color, UnityEngine.Color.white, 0.35f);
            // 2026-11-19 (kullanıcı: "taşları yüksek ışıltılı yapalım"): güçlü renkli hale + beyaz çekirdek parıltısı + parlak kenar.
            Halo(ref _halos[k], FxKit.Glow, hp, scale * 2.4f, new Color(glowCol.r, glowCol.g, glowCol.b, 0.9f * alpha * pulse), _order + 4);
            Halo(ref _cores[k], FxKit.Glow, hp, scale * 1.3f, new Color(1f, 1f, 1f, 0.32f * alpha * pulse), _order + 4);
            Halo(ref _rims[k], FxKit.Ring, hp, scale * 1.08f, new Color(1f, 1f, 1f, 0.5f * alpha * pulse), _order + 6 + _stoneCount);

            var fx = FxParticles.Active;
            if (fx == null || KatKat.UI.Motion.Reduced || ms < fly0 || !_toGoal) return;
            float uu = Mathf.Clamp01((ms - fly0) / (ArriveAt - fly0));
            // Uçuş izi: taş renginde ince ışık izi ve ara ara pırıltı (ilk iki taş; ~35 ms'de bir).
            if (k < 3 && uu < 0.95f && ms - _trailMs[k] >= 25f)
            {
                _trailMs[k] = ms;
                fx.Emit(FxKit.Glow, true, _order + 3, hp, Vector3.zero, 0.3f, scale * 0.6f, scale * 0.12f,
                    new Color(glowCol.r, glowCol.g, glowCol.b, 0.6f), new Color(_color.r, _color.g, _color.b, 0f));
                if (((int)(ms / 35f) & 1) == 0)
                    fx.Emit(FxKit.Star, true, _order + 3, hp + new Vector3((Hash(k, ms) - 0.5f) * scale * 0.5f, (Hash(k + 7, ms) - 0.5f) * scale * 0.5f, 0f),
                        Vector3.zero, 0.35f, scale * 0.35f, scale * 0.05f, new Color(1f, 1f, 1f, 0.9f), new Color(glowCol.r, glowCol.g, glowCol.b, 0f), rot: Hash(k, ms) * 45f);
            }
            // Varış: hedefte küçük ışık patlaması (bir kez).
            if (uu >= 0.97f && !_landed[k])
            {
                _landed[k] = true;
                float s = IconScale * size;
                fx.Emit(FxKit.Glow, true, _order + 8, _goalWorld, Vector3.zero, 0.35f, s * 0.6f, s * 2.4f,
                    new Color(1f, 1f, 1f, 0.8f), new Color(glowCol.r, glowCol.g, glowCol.b, 0f));
                if (k == 0)
                {
                    fx.Emit(FxKit.Ring, true, _order + 8, _goalWorld, Vector3.zero, 0.4f, s * 0.5f, s * 2.2f,
                        new Color(glowCol.r, glowCol.g, glowCol.b, 0.9f), new Color(_color.r, _color.g, _color.b, 0f));
                    for (int i = 0; i < 3; i++)
                    {
                        float a = (i + 0.3f) / 3f * Mathf.PI * 2f;
                        fx.Emit(FxKit.Star, true, _order + 9, _goalWorld, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * s * 2.2f, 0.4f, s * 0.5f, s * 0.08f,
                            UnityEngine.Color.white, new Color(glowCol.r, glowCol.g, glowCol.b, 0f), drag: 4f, rot: a * 30f);
                    }
                }
            }
        }

        /// <summary>Belirlenimli 0–1 sözde rastgele (zaman ve indeksten).</summary>
        private static float Hash(int k, float ms)
        {
            float v = Mathf.Sin(k * 12.9898f + Mathf.Floor(ms / 35f) * 78.233f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        /// <summary>Eklemeli hale sprite'ı (kendi havuzundan; parça havuzunu eklemeli malzemeyle kirletmez).</summary>
        private void Halo(ref SpriteRenderer h, Sprite sprite, Vector3 pos, float world, Color c, int order)
        {
            if (KatKat.UI.Motion.Reduced || c.a <= 0.002f)
            {
                if (h != null) h.enabled = false;
                return;
            }
            if (h == null)
            {
                while (_haloPool.Count > 0 && h == null) h = _haloPool.Pop();
                if (h == null)
                {
                    h = new GameObject("Halo").AddComponent<SpriteRenderer>();
                    FxKit.MakeAdditive(h);
                }
                h.transform.SetParent(_fxParent, false);
                h.gameObject.SetActive(true);
            }
            h.sprite = sprite;
            h.sortingOrder = order;
            h.transform.position = pos;
            h.transform.rotation = Quaternion.identity;
            WorldSize(h, world);
            h.color = c;
            h.enabled = true;
        }

        private static void GiveHalo(SpriteRenderer h)
        {
            if (h == null) return;
            h.gameObject.SetActive(false);
            _haloPool.Push(h);
        }

        /// <summary>Altın yıldız <paramref name="j"/>: taşın çevresinde belirir ve yay çizerek yıldız çubuğuna akar.</summary>
        private void Star(int j, float ms, Vector3 gather, float size, float z)
        {
            // Görev taşı: yıldızlar taş görev simgesine vardıktan sonra oradan çıkar; görev olmayan taş: doğrudan yıldıza.
            float leg0 = _toGoal ? ArriveAt : FlyStart, leg1 = _toGoal ? Duration : ArriveAt;
            float window = leg1 - leg0;
            float stagger = Mathf.Min(40f * Flow, window * 0.45f / Mathf.Max(1, _starCount));
            float s0 = leg0 + j * stagger;
            if (ms < s0)
            {
                if (_stars[j] != null) _stars[j].enabled = false;
                return;
            }
            var sr = _stars[j];
            if (sr == null)
            {
                sr = _stars[j] = StoneShatter.Take(_fxParent);
                sr.sprite = _starSprite;
                sr.sortingOrder = _order + 7;
            }
            sr.enabled = true;
            var from = _toGoal ? _goalWorld + _starOff[j] * 0.5f : gather + _starOff[j];
            float u = Mathf.Clamp01((ms - s0) / (leg1 - s0));
            float e = u * u; // hızlanan akış (ease-in)
            var ctrl = Vector3.Lerp(from, _starWorld, 0.5f) + new Vector3(_starArc[j], 0.7f * size, 0f);
            var pos = Bezier(from, ctrl, _starWorld, e);
            sr.transform.position = new Vector3(pos.x, pos.y, z - 0.02f);
            float pop = Mathf.Clamp01(u * 6f);
            WorldSize(sr, size * Mathf.Lerp(0.45f, 0.28f, e) * Mathf.Lerp(0.4f, 1f, pop));
            sr.transform.rotation = Quaternion.Euler(0f, 0f, _starSpin[j] * u);
            float a = Mathf.Min(pop, u < 0.88f ? 1f : Mathf.Lerp(1f, 0f, (u - 0.88f) / 0.12f));
            sr.color = new Color(1f, 1f, 1f, a);

            // Altın hale (her ikinci yıldız), ince altın iz (her üçüncü) ve çubukta varış pırıltısı (ilk ve son yıldız).
            var hp = new Vector3(pos.x, pos.y, z - 0.02f);
            float ss = size * Mathf.Lerp(0.45f, 0.28f, e) * Mathf.Lerp(0.4f, 1f, pop);
            Halo(ref _starHalos[j], FxKit.Glow, hp, ss * 2.8f, new Color(1f, 0.85f, 0.4f, 0.8f * a), _order + 6);
            var fx = FxParticles.Active;
            if (fx == null || KatKat.UI.Motion.Reduced) return;
            if (j % 3 == 0 && u < 0.93f && ms - _starTrailMs[j] >= 45f)
            {
                _starTrailMs[j] = ms;
                fx.Emit(FxKit.Glow, true, _order + 6, hp, Vector3.zero, 0.25f, ss * 1.1f, ss * 0.2f,
                    new Color(1f, 0.85f, 0.4f, 0.55f), new Color(1f, 0.6f, 0.15f, 0f));
            }
            if (u >= 0.97f && !_starLanded[j] && (j == 0 || j == _starCount - 1))
            {
                _starLanded[j] = true;
                fx.Emit(FxKit.Glow, true, _order + 8, _starWorld, Vector3.zero, 0.3f, size * 0.25f, size * 1.1f,
                    new Color(1f, 0.95f, 0.7f, 0.85f), new Color(1f, 0.7f, 0.2f, 0f));
                fx.Emit(FxKit.Star, true, _order + 9, _starWorld, Vector3.up * size * 0.6f, 0.35f, size * 0.4f, size * 0.06f,
                    UnityEngine.Color.white, new Color(1f, 0.8f, 0.3f, 0f), drag: 3f, rot: 20f);
            }
        }

        private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float m = 1f - t;
            return m * m * a + 2f * m * t * b + t * t * c;
        }

        private static float BackOut(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float y = x - 1f;
            return 1f + c3 * y * y * y + c1 * y * y;
        }

        /// <summary>Sprite'ı en uzun kenarı <paramref name="world"/> dünya birimi olacak biçimde ölçekler.</summary>
        private static void WorldSize(SpriteRenderer sr, float world)
        {
            var b = sr.sprite != null ? sr.sprite.bounds.size : Vector3.one;
            float m = Mathf.Max(1e-4f, Mathf.Max(b.x, b.y));
            var parent = sr.transform.parent;
            float ps = parent != null ? Mathf.Max(1e-4f, Mathf.Abs(parent.lossyScale.x)) : 1f;
            float sc = world / (m * ps);
            sr.transform.localScale = new Vector3(sc, sc, 1f);
        }

        private void Break(Vector3 faceLocal)
        {
            _broken = true;
            _origin = _stack.transform.TransformPoint(faceLocal);
            if (!_rimReleased) { _rimReleased = true; _stack.ReleaseDetachedRim(this); }
            _sheet.End();
            if (_crack != null) { _crack.transform.localRotation = Quaternion.identity; StoneShatter.Give(_crack); _crack = null; }
            GiveHalo(_preGlow); _preGlow = null; GiveHalo(_preCore); _preCore = null;
            _sheet.transform.localRotation = Quaternion.identity;
            // 2026-11-18 ("efektleri daha profesyonel"): taş renginde parlak patlama, şok halkası, kıvılcımlar, yıldızlar.
            if (!KatKat.UI.Motion.Reduced)
                BoardView.StonePop(FxParticles.Active, _origin, _color,
                    _stack.Size, _order + 6);
            for (int i = 0; i < ShardCount; i++)
            {
                var sr = StoneShatter.Take(_fxParent);
                sr.sprite = StoneShatter.Chip(i);
                sr.sortingOrder = _order + 2 + i % 2;
                sr.enabled = true;
                _shards[i] = sr;
            }
            for (int i = 0; i < DustCount; i++)
            {
                var d = StoneShatter.Take(_fxParent);
                d.sprite = SpecialArt.Glow;
                d.sortingOrder = _order + 1;
                d.enabled = true;
                _dust[i] = d;
            }
        }

        /// <summary>Kırılmadan uçuş: 0–160 ms yerinde sıçrama, sonra yay çizerek hedefe; varışta küçülüp kaybolur.</summary>
        private void IntactFlight(float ms, float size)
        {
            if (!_stackDone)
            {
                _origin = _stack.transform.TransformPoint(_stack.FaceCenterLocal(_layerIndex));
                _stack.SetBodyScale(this, Vector2.one);
                _stack.ReleaseDetachedRim(this);
                _sheet.End();
                _stackDone = true;
                _broken = true;
            }
            var sr = _stones[0];
            if (sr == null)
            {
                sr = _stones[0] = StoneShatter.Take(_fxParent);
                sr.sprite = _stoneSprite != null ? _stoneSprite : StoneShatter.Chip(0);
                sr.sortingOrder = _order + 6;
            }
            sr.enabled = true;
            float z = _origin.z - 0.02f;
            const float Hop = 160f;
            float full = size * BigScale * 0.92f;
            if (ms < Hop)
            {
                float h = Mathf.Sin(Mathf.Clamp01(ms / Hop) * Mathf.PI * 0.5f);
                var p = _origin + new Vector3(0f, size * 0.35f * h, 0f);
                sr.transform.position = new Vector3(p.x, p.y, z);
                float sq = 1f + 0.12f * Mathf.Sin(Mathf.Clamp01(ms / Hop) * Mathf.PI);
                WorldSize(sr, full);
                sr.transform.localScale = new Vector3(sr.transform.localScale.x * sq, sr.transform.localScale.y / sq, 1f);
                sr.transform.rotation = Quaternion.identity;
                sr.color = UnityEngine.Color.white;
                return;
            }
            float u = Mathf.Clamp01((ms - Hop) / (ArriveAt - Hop));
            float e = u * u * (3f - 2f * u);
            var start = _origin + new Vector3(0f, size * 0.35f, 0f);
            var ctrl = Vector3.Lerp(start, _goalWorld, 0.45f) + new Vector3(_stoneArc[0], size * 1.6f, 0f);
            var a = Vector3.Lerp(start, ctrl, e);
            var b = Vector3.Lerp(ctrl, _goalWorld, e);
            var pos = Vector3.Lerp(a, b, e);
            sr.transform.position = new Vector3(pos.x, pos.y, z);
            WorldSize(sr, Mathf.Lerp(full, IconScale * size, e));
            sr.transform.rotation = Quaternion.Euler(0f, 0f, _stoneArc[0] * 40f * Mathf.Sin(e * Mathf.PI));
            if (ms >= ArriveAt && !_arrived)
            {
                _arrived = true;
                sr.enabled = false;
                _onArrive?.Invoke(this);
            }
            if (ms >= ArriveAt)
            {
                Cleanup();
                IsDone = true;
            }
        }

        private void Cleanup()
        {
            if (_crack != null) { StoneShatter.Give(_crack); _crack = null; }
            for (int i = 0; i < ShardCount; i++) { Give(_shards[i]); _shards[i] = null; }
            for (int i = 0; i < DustCount; i++) { Give(_dust[i]); _dust[i] = null; }
            for (int k = 0; k < MaxStones; k++) { Give(_stones[k]); _stones[k] = null; }
            if (_stars != null) for (int j = 0; j < _stars.Length; j++) { Give(_stars[j]); _stars[j] = null; }
            for (int k = 0; k < MaxStones; k++) { GiveHalo(_halos[k]); _halos[k] = null; GiveHalo(_rims[k]); _rims[k] = null; GiveHalo(_cores[k]); _cores[k] = null; }
            if (_starHalos != null) for (int j = 0; j < _starHalos.Length; j++) { GiveHalo(_starHalos[j]); _starHalos[j] = null; }
            GiveHalo(_preGlow); _preGlow = null; GiveHalo(_preCore); _preCore = null;
        }

        /// <summary>Havuza iade; havuzdaki nesne sonraki kullanımda ölçek/dönüş/görünürlük taşımasın.</summary>
        private static void Give(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.enabled = true;
            sr.transform.rotation = Quaternion.identity;
            StoneShatter.Give(sr);
        }

        /// <summary>Animasyonu atlayıp son duruma geçer (ör. uygulama arka plana gittiğinde).</summary>
        public void Complete() => Evaluate(Duration + (Charged ? ChargeShift : 0f));

        public PeelSheet Sheet => _sheet;
    }
}
