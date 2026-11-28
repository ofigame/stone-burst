using System;
using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// 2026-11-26 (kullanıcı: "Normal Taş Patlatma ve Hedefe Taş Toplama Animasyonu"; "hedef dışı taşlar yukarı uçmasın, yalnız
    /// patlasın"): normal dokunuş, roket ve Altın Rulo patlamalarının ORTAK taş patlaması, hedefe uçan mini taş ve hedef isabeti.
    /// Yalnız görünüm: hangi taşın ne zaman soyulduğu ve görev ilerlemesi mantıkta (RulesEngine) zaten olmuştur; BoardView taşı
    /// zamanında buraya verir, uçuşun varış anını da önceden hesaplar (HUD sayacı varışta bir artar, dalga sonunda mantıksal değere
    /// oturur).
    /// Taş evreleri (taş başlangıcından ms; <see cref="BoosterFxConfig.BlastSettings"/>): 0–60 dokunuş sıkışması ve parlama, 60–130
    /// kristal gibi esneme (squash &amp; stretch) ve kendi renginde kenar ışığı, 130–250 patlama: taş büyüyüp parlar, sonra küçülerek
    /// kaybolur; kendi renginde 4–8 yuvarlak jöle/kristal parça, ışık noktaları, damlalar, birkaç küçük yıldız, kısa ışık çizgileri,
    /// ince yumuşak halka. Hedef rengindeki taştan (görevin kalan ihtiyacı kadar) taşın kendi çiziminin küçük kopyası çıkar, hafif
    /// dışa sıçrar, esneyerek seker ve taşa özgü kontrol noktalı kübik Bézier yolla (yavaş başlar, hızlanır, hedefe mıknatısla
    /// çekilir) görev simgesine uçar; varışta simge seker, halka ve pırıltı. Bağımsız havuzlu katman: yerçekimi/dolma ve girdiyi
    /// beklemez; <see cref="Cancel"/> ile güvenle biter. Her nesne havuzdan (taş başına Instantiate/Destroy ya da malzeme örneği yok).
    /// </summary>
    public sealed class StoneBlast : MonoBehaviour
    {
        /// <summary>Uçuş planı: BoardView varış anını önceden hesaplar (sayacın oturma anı).</summary>
        public struct FlightPlan
        {
            public int Goal;
            public Vector3 Target;
            /// <summary>Hedef simgesinin canlı konumu (varsa uçuş simgeyi izler; HUD yerleşimi değişse de içine iner).</summary>
            public Func<Vector3> TargetFn;
            /// <summary>Patlamadan kalkışa kadar (sıçrama + sıra bekleme) ve uçuş süresi (ms).</summary>
            public float HoldMs, FlyMs;
            public float Seed;
            public bool Visible;
        }

        public struct Args
        {
            public float BurstAt;
            public bool Tapped, Special;
            public int Tier;
            /// <summary>Roket: hareket yönü (dünya, birim); Altın Rulo / dokunuş: dışa yön. Sıfırsa yukarı.</summary>
            public Vector3 Nudge;
            /// <summary>Zaman çizgisindeki gecikme (ms): taş bu kadar ileriden başlar (kare kayması).</summary>
            public float LateMs;
            /// <summary>2026-11-28 mağaza bombası: BurstAt boyunca taş yerinden yükselir, artan titremeyle parlar, havada patlar.</summary>
            public bool Charge;
            public bool HasFlight;
            public FlightPlan Flight;
        }

        /// <summary>Kopya hedefe vardı: görev indeksi; dönüş true ise bu varış görevi tamamladı (güçlü efekt).</summary>
        public Func<int, bool> Arrived;

        private BoardView _board;
        private readonly List<Burst> _bursts = new List<Burst>();
        private readonly List<Flight> _flights = new List<Flight>();
        private readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>(), _poolAdd = new Stack<SpriteRenderer>();
        private readonly Stack<Burst> _burstPool = new Stack<Burst>();
        private readonly Stack<Flight> _flightPool = new Stack<Flight>();
        private Material _alphaMat;
        private static Sprite _jelly;

        public static StoneBlast Attach(BoardView board)
        {
            var go = new GameObject("StoneBlast");
            go.transform.SetParent(board.transform, false);
            var b = go.AddComponent<StoneBlast>();
            b._board = board;
            return b;
        }

        /// <summary>Taşın yığın tarafı sürüyor mu (tahta bu sürede yeniden çizilmez).</summary>
        public bool StackBusy => _bursts.Count > 0;
        public bool Flying => _flights.Count > 0;
        public int FlightCount => _flights.Count;
        /// <summary>2026-11-28 tanı: görünür uçuşlardan taş çizicisi kapalı olanların sayısı (0 olmalı; havuz hatası denetimi).</summary>
        public int HiddenFlightFaces { get { int n = 0; foreach (var f in _flights) if (f.Visible && f.Sr != null && !f.Sr.enabled) n++; return n; } }

        private static BoosterFxConfig.BlastSettings C => BoosterFxConfig.Current.Blast;
        private float Cell => _board.WorldCell;
        private float Lossy => Mathf.Max(1e-4f, transform.lossyScale.x);
        private const int FaceOrder = PeelDirector.PeelSortingOrder - 20;
        private const int FxOrder = PeelDirector.PeelSortingOrder + 30;
        private const int FlyOrder = PeelDirector.PeelSortingOrder + 1200;
        private static float R(float a, float b) => UnityEngine.Random.Range(a, b);

        // ---- Havuz ---------------------------------------------------------------------------------

        private readonly List<(SpriteRenderer Sr, bool Add)> _warm = new List<(SpriteRenderer, bool)>();

        /// <summary>
        /// 2026-11-27 ön ısıtma: renklerin taş çizimi, parça/ışık görselleri ve havuz nesneleri şimdi üretilir; her malzeme görünmez
        /// (saydamlık ~0) bir kez çizilir ki gölgelendirici ilk patlamada derlenmesin. <see cref="EndWarm"/> havuza geri verir.
        /// </summary>
        public void Warm(IEnumerable<LayerColor> colors, Vector3 at)
        {
            EndWarm();
            var sprites = new List<Sprite> { Jelly, StoneShatter.Chip(0), StoneShatter.Chip(1), StoneShatter.Chip(2), FxKit.Glow, FxKit.Ring, FxKit.Dot, FxKit.Star, FxKit.Streak };
            foreach (var c in colors) sprites.Add(UiStoneArt.Chip(c));
            at.z -= 0.05f;
            int alpha = 0, add = 0;
            void One(Sprite sp, bool additive)
            {
                var sr = Take(sp, additive, FaceOrder);
                sr.transform.position = at;
                Size(sr, Cell * 0.2f);
                sr.color = new Color(1f, 1f, 1f, 0.004f);
                sr.enabled = true;
                _warm.Add((sr, additive));
                if (additive) add++; else alpha++;
            }
            foreach (var sp in sprites) { One(sp, false); One(sp, true); }
            // Havuz: tipik büyük patlamada aynı anda gereken kadar (taş başına 4, uçuş başına 2).
            while (alpha < 20) One(FxKit.Dot, false);
            while (add < 40) One(FxKit.Glow, true);
        }

        public void EndWarm()
        {
            for (int i = 0; i < _warm.Count; i++)
            {
                var sr = _warm[i].Sr;
                if (sr != null) Give(ref sr, _warm[i].Add);
            }
            _warm.Clear();
        }

        private SpriteRenderer Take(Sprite s, bool additive, int order)
        {
            var pool = additive ? _poolAdd : _pool;
            SpriteRenderer sr = null;
            while (pool.Count > 0 && sr == null) sr = pool.Pop();
            if (sr == null)
            {
                sr = new GameObject(additive ? "BlastAdd" : "Blast").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(transform, false);
                if (_alphaMat == null) _alphaMat = sr.sharedMaterial;
                if (additive) FxKit.MakeAdditive(sr);
                else sr.sharedMaterial = _alphaMat;
            }
            sr.sprite = s;
            sr.sortingOrder = order;
            sr.color = new Color(1f, 1f, 1f, 0f);
            sr.transform.rotation = Quaternion.identity;
            sr.transform.localScale = Vector3.zero; // boyu çağıran verir (havuzdan dönen eski boy bir kare görünmesin)
            // 2026-11-28 (kullanıcı: "ilk başta taş gidiyor sonra ufak ışın topu gidiyor"): sönen patlama kopyası kapalı (enabled=false)
            // havuza dönüyordu; uçuş onu alınca taş görünmüyor, yalnız ışık izi uçuyordu. Havuzdan çıkan her çizici açık başlar.
            sr.enabled = true;
            sr.gameObject.SetActive(true);
            return sr;
        }

        private void Give(ref SpriteRenderer sr, bool additive)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
            (additive ? _poolAdd : _pool).Push(sr);
            sr = null;
        }

        /// <summary>Dünya boyu (en uzun kenar) ve ölçek çarpanı (x, y).</summary>
        private void Size(SpriteRenderer sr, float world, float sx = 1f, float sy = 1f)
        {
            var b = sr.sprite != null ? sr.sprite.bounds.size : Vector3.one;
            float k = world / (Mathf.Max(1e-4f, Mathf.Max(b.x, b.y)) * Lossy);
            sr.transform.localScale = new Vector3(k * sx, k * sy, 1f);
        }

        /// <summary>Tahtanın havuzlu parçacığı; boylar dünya biriminde (yerel ölçeğe çevrilir).</summary>
        private void Emit(Sprite sprite, bool additive, int order, Vector3 pos, Vector3 vel, float life, float size0, float size1,
            Color c0, Color c1, float gravity = 0f, float drag = 0f, float delay = 0f, float rot = 0f, float rotVel = 0f, float stretch = 0f)
        {
            float inv = 1f / Lossy;
            _board.Particles.Emit(sprite, additive, order, pos, vel, life, size0 * inv, size1 * inv, c0, c1, gravity, drag, delay, rot, rotVel, stretch * Lossy);
        }

        // ---- Renk karakteri --------------------------------------------------------------------------

        /// <summary>Taş rengine göre ana ve vurgu rengi; mor ve lacivertte ek kristal/yıldız pırıltısı.</summary>
        public static void Palette(LayerColor c, out Color main, out Color accent, out int extraStars)
        {
            main = (Color)(Color32)LayerPalette.FrontColor(c);
            extraStars = 0;
            switch (c)
            {
                case LayerColor.Coral: accent = new Color(1f, 0.62f, 0.3f, 1f); break;            // mercan + sıcak turuncu
                case LayerColor.Blue: accent = new Color(0.62f, 1f, 0.84f, 1f); break;              // turkuaz + nane
                case LayerColor.Yellow:                                                            // inci: krem ve pastel inci
                    main = Color.Lerp(main, new Color(1f, 0.96f, 0.86f, 1f), 0.55f);
                    accent = new Color(0.97f, 0.9f, 1f, 1f); break;
                case LayerColor.Purple: accent = new Color(0.86f, 0.72f, 1f, 1f); extraStars = 2; break; // mor + lila, kristal parıltı
                case LayerColor.Green: accent = new Color(0.82f, 1f, 0.56f, 1f); break;
                case LayerColor.Navy: accent = new Color(0.6f, 0.9f, 1f, 1f); extraStars = 2; break;     // mavi + açık camgöbeği, yıldız
                default: accent = Color.Lerp(main, Color.white, 0.4f); break;
            }
            main.a = 1f;
        }

        /// <summary>Yuvarlak jöle parçası: yumuşak kenarlı damla, sol üstte parlak benek (beyaz; renk köşe renginden).</summary>
        private static Sprite Jelly
        {
            get
            {
                if (_jelly != null) return _jelly;
                const int n = 48;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "blast_jelly", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
                var px = new Color32[n * n];
                for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = (i + 0.5f) / n * 2f - 1f, y = (j + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(x * x / 0.9f + y * y);
                    float a = Mathf.Clamp01((0.92f - r) * 14f);
                    float shade = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01((-x * 0.4f - y * 0.6f + 0.6f) * 0.7f));
                    float hl = Mathf.Exp(-((x + 0.3f) * (x + 0.3f) + (y - 0.35f) * (y - 0.35f)) * 22f);
                    float v = Mathf.Clamp01(shade + hl * 0.6f);
                    px[j * n + i] = new Color(v, v, v, a);
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return _jelly = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            }
        }

        // ---- Taş patlaması --------------------------------------------------------------------------

        private sealed class Burst
        {
            public StackView Stack;
            public LayerColor Color;
            public Color Main, Accent;
            public int ExtraStars;
            public Vector3 Pos;
            public float Size, Age, BurstAt;
            public bool Tapped, Special, Popped, Charge;
            public int Tier;
            public Vector3 Nudge;
            public bool HasFlight;
            public FlightPlan Plan;
            public Sprite FaceSprite;
            public SpriteRenderer Face, Bright, Glow, Rim;
        }

        /// <summary>
        /// Yığının üst taşını patlatır: üst katman hemen yığından ayrılır (alt renk görünür), taş kopyası yerinde evreleri oynar.
        /// </summary>
        public void Play(StackView stack, Args a)
        {
            if (stack == null || stack.Layers.Count == 0) return;
            var b = _burstPool.Count > 0 ? _burstPool.Pop() : new Burst();
            int top = stack.Layers.Count - 1;
            b.Stack = stack;
            b.Pos = stack.transform.TransformPoint(stack.FaceCenterLocal(top));
            b.Color = stack.DetachTop(b);
            Palette(b.Color, out b.Main, out b.Accent, out b.ExtraStars);
            b.Size = stack.Size * Mathf.Abs(stack.transform.lossyScale.x);
            b.Age = Mathf.Max(0f, a.LateMs);
            b.BurstAt = Mathf.Max(1f, a.BurstAt);
            b.Tapped = a.Tapped;
            b.Special = a.Special;
            b.Popped = false;
            b.Charge = a.Charge;
            b.Tier = a.Tier;
            b.Nudge = a.Nudge;
            b.HasFlight = a.HasFlight;
            b.Plan = a.Flight;
            b.FaceSprite = UiStoneArt.Chip(b.Color);
            b.Face = Take(b.FaceSprite, false, FaceOrder);
            b.Bright = Take(b.FaceSprite, true, FaceOrder + 1);
            b.Glow = Take(FxKit.Glow, true, FaceOrder - 1);
            b.Rim = Take(FxKit.Ring, true, FaceOrder + 2);
            _bursts.Add(b);
            PoseBurst(b);
        }

        private void PoseBurst(Burst b)
        {
            var c = C;
            float a = b.Age, B = b.BurstAt;
            float react = b.Special ? Mathf.Min(c.TapReactMs, B * 0.45f) : Mathf.Min(c.TapReactMs, B * 0.5f);
            float sx = 1f, sy = 1f, bright = 0f, glow = 0f, rim = 0f, alpha = 1f;
            Vector3 off = Vector3.zero;
            if (b.Charge && a < B)
            {
                // Bomba yüklenmesi: ilk %57'de yerinden yükselip biraz büyür, sonra havada artan titreme, kenar ışığı ve parlama.
                float riseEnd = B * (PeelSequence.ChargeRiseEnd / PeelSequence.ChargeMs);
                float ru = Mathf.Clamp01(a / Mathf.Max(1f, riseEnd));
                float re = EaseOutCubic(ru);
                float hold = Mathf.Clamp01((a - riseEnd) / Mathf.Max(1f, B - riseEnd));
                float shake = Mathf.Sin(a * 0.42f) * b.Size * (0.012f + 0.05f * hold);
                float shakeY = Mathf.Sin(a * 0.31f + 1f) * b.Size * 0.02f * hold;
                off = new Vector3(shake, PeelSequence.LiftHeight * b.Size * re + shakeY, 0f);
                float g = 1f + PeelSequence.LiftGrow * re + 0.04f * hold * Mathf.Sin(a * 0.5f);
                sx = sy = g;
                bright = 0.08f + 0.35f * hold;
                rim = c.RimGlow * (0.3f + 0.7f * hold);
                glow = 0.25f + 0.45f * hold;
            }
            else if (a < react)
            {
                // 1 – dokunuş tepkisi: sıkışıp toparlanır, kısa parlama (dokunulan taşta daha belirgin).
                float u = a / react;
                float depth = 1f - (b.Tapped ? c.TapSquash : c.RippleSquash);
                if (b.Special) depth *= 0.5f;
                float k = Mathf.Sin(Mathf.PI * u) * depth;
                sx = 1f + k * 0.55f; sy = 1f - k;
                bright = c.TapHighlight * Mathf.Sin(Mathf.PI * u) * (b.Tapped ? 1f : 0.6f);
            }
            else if (a < B)
            {
                // 2 – esneme: kristal gibi kısa ve sert squash & stretch, kendi renginde kenar ışığı.
                float u = (a - react) / Mathf.Max(1f, B - react);
                float g = Mathf.Lerp(1f, c.StretchPeak, EaseOutCubic(u));
                float w = Mathf.Sin(2f * Mathf.PI * u) * c.StretchSquash * (1f - 0.5f * u);
                sx = g * (1f + w); sy = g * (1f - w);
                bright = Mathf.Lerp(0.05f, 0.2f, u);
                rim = c.RimGlow * EaseOutCubic(u);
                glow = 0.3f * u;
                if (b.Nudge.sqrMagnitude > 0f && b.Special) off = b.Nudge * Cell * 0.05f * u;
            }
            else
            {
                if (!b.Popped) Pop(b);
                // 3 – patlama: önce büyüyüp parlar, sonra küçülerek kaybolur (yalnız saydamlıkla sönmez).
                float u = Mathf.Clamp01((a - B) / Mathf.Max(1f, c.BurstMs));
                float g;
                if (u < 0.3f)
                {
                    float v = u / 0.3f;
                    g = Mathf.Lerp(c.StretchPeak, c.BurstExpand, EaseOutCubic(v));
                    bright = Mathf.Lerp(0.2f, c.BurstBrighten, v);
                }
                else
                {
                    float v = (u - 0.3f) / 0.7f;
                    g = c.BurstExpand * Mathf.Lerp(1f, 0.05f, v * v);
                    bright = c.BurstBrighten * (1f - v);
                    alpha = v < 0.55f ? 1f : 1f - (v - 0.55f) / 0.45f;
                }
                sx = sy = g;
                rim = c.RimGlow * (1f - u);
                glow = 0.4f * (1f - u);
                if (b.Nudge.sqrMagnitude > 0f) off = b.Nudge * Cell * (b.Special ? 0.05f + 0.12f * u : 0.04f * u);
            }
            // Açık renkli taşta (inci) eklemeli parlama beyaz plakaya dönmesin: parlaklık rengin açıklığıyla azalır.
            float lum = b.Main.r * 0.3f + b.Main.g * 0.59f + b.Main.b * 0.11f;
            float pale = 1f - 0.7f * Mathf.Clamp01((lum - 0.45f) / 0.5f);
            bright *= pale; glow *= pale;
            var p = b.Pos + off;
            p.z -= 0.01f;
            Place(b.Face, p, b.Size, sx, sy, new Color(1f, 1f, 1f, alpha));
            Place(b.Bright, p, b.Size, sx, sy, new Color(1f, 1f, 1f, Mathf.Clamp01(bright) * alpha));
            var gc = Color.Lerp(b.Main, Color.white, 0.3f);
            float gs = Mathf.Max(sx, sy);
            Place(b.Glow, p, b.Size * 1.7f * gs, 1f, 1f, new Color(gc.r, gc.g, gc.b, glow * alpha));
            var rc = Color.Lerp(b.Main, b.Accent, 0.4f);
            Place(b.Rim, p, b.Size * 1.12f * gs, 1f, 1f, new Color(rc.r, rc.g, rc.b, rim * alpha));
        }

        private void Place(SpriteRenderer sr, Vector3 p, float world, float sx, float sy, Color col)
        {
            if (sr == null) return;
            sr.transform.position = p;
            Size(sr, world, sx, sy);
            sr.color = col;
            sr.enabled = col.a > 0.003f;
        }

        /// <summary>Patlama anı: parçalar, mikro parçacıklar, ince halka; yığın kenarı bırakılır; hedef taşından kopya çıkar.</summary>
        private void Pop(Burst b)
        {
            b.Popped = true;
            // Bomba: taş havada patlar; parçalar ve hedef uçuşu yükseldiği yerden başlar.
            if (b.Charge) b.Pos += new Vector3(0f, PeelSequence.LiftHeight * b.Size, 0f);
            if (b.Stack != null) b.Stack.ReleaseDetachedRim(b);
            var c = C;
            if (b.HasFlight) Launch(b);
            if (KatKat.UI.Motion.Reduced) return;
            var cfg = BoosterFxConfig.Current;
            float tierK = b.Tier <= 0 ? c.ParticleByTier.x : b.Tier == 1 ? c.ParticleByTier.y : b.Tier == 2 ? c.ParticleByTier.z : c.ParticleByTier.w;
            // 2026-11-26 (kullanıcı: "Efektler çok pasif"): roket / Altın Rulo vuruşunda daha çok ve hızlı parça, daha büyük halka ve ışık.
            float boost = b.Special ? c.SpecialBoost : 1f;
            float k = cfg.ParticleK * tierK * boost * (_bursts.Count > c.MaxConcurrent ? 0.5f : 1f);
            int N(float n) => Mathf.Max(0, Mathf.RoundToInt(n * k));
            float s = b.Size;
            var pos = b.Pos; pos.z -= 0.02f;
            var bias = b.Nudge.sqrMagnitude > 0f ? b.Nudge : Vector3.zero;

            // Jöle / kristal parçalar: değişken boy, dönerek dışa, yavaşlayarak; küçülüp söner.
            int chips = Mathf.Max(b.Tier >= 2 ? 3 : 2, N(Mathf.Lerp(c.ChipMin, c.ChipMax, b.Tier / 3f) + R(-0.5f, 0.5f)));
            float a0 = R(0f, Mathf.PI * 2f);
            for (int i = 0; i < chips; i++)
            {
                float ang = a0 + (i + R(-0.3f, 0.3f)) / chips * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                if (bias.sqrMagnitude > 0f) dir = (dir * 0.6f + bias * R(0.5f, 0.9f)).normalized;
                float sz = s * c.ChipSize * R(0.7f, 1.35f) * 1.6f;
                var col = Color.Lerp(b.Main, b.Accent, R(0f, 0.5f));
                var spr = (i & 1) == 0 ? Jelly : StoneShatter.Chip(i);
                Emit(spr, false, FxOrder + 2, pos + dir * s * 0.18f, (dir * R(0.75f, 1.2f) * c.ChipSpeed * (b.Special ? c.SpecialChipSpeed : 1f) + Vector3.up * 0.5f) * s, c.ChipLife * R(0.85f, 1.15f),
                    sz, sz * 0.25f, new Color(col.r, col.g, col.b, 1f), new Color(col.r, col.g, col.b, 0f),
                    gravity: -s * 5f, drag: 3.6f, rot: R(0f, 360f), rotVel: R(-380f, 380f));
            }
            // Mikro parçacıklar: ışık noktaları (eklemeli) ve yuvarlak damlalar (kendi renginde).
            int micro = N(c.MicroCount);
            for (int i = 0; i < micro; i++)
            {
                float ang = R(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                if (bias.sqrMagnitude > 0f) dir = (dir * 0.7f + bias * 0.6f).normalized;
                float sp = s * c.MicroSpeed * R(0.6f, 1.2f);
                bool light = (i & 1) == 0;
                var col = light ? Color.Lerp(b.Accent, Color.white, 0.5f) : b.Main;
                float sz = s * c.MicroSize * R(0.7f, 1.3f) * (light ? 1.4f : 1f);
                Emit(FxKit.Dot, light, FxOrder + (light ? 4 : 3), pos + dir * s * 0.12f, dir * sp + Vector3.up * s * 0.6f, c.MicroLife * R(0.8f, 1.2f),
                    sz, sz * 0.3f, new Color(col.r, col.g, col.b, 1f), new Color(b.Accent.r, b.Accent.g, b.Accent.b, 0f), gravity: -s * 6f, drag: 3.2f);
            }
            // Birkaç küçük yıldız (mor: kristal parıltı, lacivert: yıldız pırıltısı daha çok).
            int stars = N(c.StarCount + b.ExtraStars);
            for (int i = 0; i < stars; i++)
            {
                var off = new Vector3(R(-0.4f, 0.4f), R(-0.25f, 0.45f), 0f) * s;
                Emit(FxKit.Star, true, FxOrder + 5, pos + off, (off.normalized + Vector3.up * 0.4f) * s * 0.8f, R(0.24f, 0.36f), s * 0.34f, s * 0.06f,
                    new Color(1f, 1f, 1f, 0.95f), new Color(b.Accent.r, b.Accent.g, b.Accent.b, 0f), drag: 3f, delay: R(0f, 0.05f), rot: R(0f, 45f), rotVel: R(-160f, 160f));
            }
            // Kısa ışık çizgileri (düşük kalitede yok).
            if (!cfg.LowQuality)
            {
                int streaks = N(c.StreakCount);
                for (int i = 0; i < streaks; i++)
                {
                    float ang = R(0f, Mathf.PI * 2f);
                    var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                    Emit(FxKit.Streak, true, FxOrder + 4, pos + dir * s * 0.3f, dir * s * 4.2f, 0.12f, s * 0.32f, s * 0.12f,
                        new Color(1f, 1f, 1f, 0.75f), new Color(b.Accent.r, b.Accent.g, b.Accent.b, 0f), drag: 5f, stretch: 0.12f / Mathf.Max(0.01f, s));
                }
            }
            // İnce, yumuşak, kendi renginde genişleyen halka ve hafif ışık (parlama/flaş yok).
            float ringK = b.Tier >= 3 ? 1.25f : b.Tier == 2 ? 1.12f : 1f;
            var rc = Color.Lerp(b.Main, b.Accent, 0.35f);
            if (b.Special) ringK *= c.SpecialRingK;
            Emit(FxKit.Ring, true, FxOrder + 1, pos, Vector3.zero, c.RingLife, s * c.RingFrom, s * c.RingTo * ringK,
                new Color(rc.r, rc.g, rc.b, b.Special ? c.SpecialRingAlpha : c.RingAlpha), new Color(rc.r, rc.g, rc.b, 0f));
            var gl = Color.Lerp(b.Main, Color.white, 0.35f);
            Emit(FxKit.Glow, true, FxOrder, pos, Vector3.zero, b.Special ? 0.24f : 0.18f, s * 0.9f, s * (b.Special ? 1.9f : 1.5f), new Color(gl.r, gl.g, gl.b, b.Special ? c.SpecialGlow : c.Glow), new Color(b.Main.r, b.Main.g, b.Main.b, 0f));
        }

        // ---- Hedefe uçan mini taş ---------------------------------------------------------------------

        private sealed class Flight
        {
            public SpriteRenderer Sr, Halo;
            public Vector3 From, PopTo, P1, P2, Target;
            public Func<Vector3> TargetFn;
            public float Age, PopMs, Hold, Fly, Size, Spin, TrailAt, SparkAt, WavePhase;
            public Color Main, Accent;
            public int Goal;
            public bool Visible;
        }

        private void Launch(Burst b)
        {
            var c = C;
            var plan = b.Plan;
            var f = _flightPool.Count > 0 ? _flightPool.Pop() : new Flight();
            float s = b.Size;
            var rng = new System.Random(Mathf.RoundToInt(plan.Seed * 7919f) + 17);
            float Rn(float lo, float hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var up = Vector3.up;
            // Sıçrama yönü: roket taşında önce roketin yönünde, diğerlerinde dışa ve yukarı.
            Vector3 dir;
            if (b.Special && b.Nudge.sqrMagnitude > 0f) dir = (b.Nudge * 1f + up * 0.35f).normalized;
            else if (b.Nudge.sqrMagnitude > 0f) dir = (b.Nudge * 0.6f + up).normalized;
            else dir = (new Vector3(Rn(-0.5f, 0.5f), 0f, 0f) + up).normalized;
            float push = (b.Special && b.Nudge.sqrMagnitude > 0f ? c.RocketNudge : c.PopOut) * Cell;
            f.From = b.Pos;
            f.PopTo = b.Pos + dir * push;
            f.TargetFn = plan.TargetFn;
            f.Target = plan.TargetFn != null ? plan.TargetFn() : plan.Target;
            var d = f.Target - f.PopTo;
            float len = d.magnitude;
            var side = len > 1e-4f ? new Vector3(-d.y, d.x, 0f) / len : Vector3.right;
            // Taşa özgü kontrol noktaları: biri çıkış yönünde ve yukarıda, biri hedefin altında yanlara kaymış.
            f.P1 = f.PopTo + dir * Cell * 0.6f + up * Cell * c.ArcHeight * Rn(0.55f, 1.1f) + side * Cell * c.ArcWidth * Rn(-1f, 1f);
            f.P2 = f.Target - d.normalized * Mathf.Min(len * 0.35f, Cell * 2.2f) + side * Cell * c.ArcWidth * Rn(-0.8f, 0.8f) - up * Cell * 0.4f;
            f.Age = 0f;
            f.PopMs = Mathf.Min(c.PopMs, Mathf.Max(1f, plan.HoldMs));
            f.Hold = Mathf.Max(f.PopMs, plan.HoldMs);
            f.Fly = Mathf.Max(1f, plan.FlyMs);
            f.Size = s;
            f.Spin = (rng.NextDouble() < 0.5 ? -1f : 1f) * c.Spin * Rn(0.6f, 1.1f);
            f.TrailAt = 0f;
            f.SparkAt = 0f;
            f.WavePhase = rng.NextDouble() < 0.5 ? 0f : Mathf.PI; // komşu taşlar ters yönde dalgalanır
            f.Main = b.Main; f.Accent = b.Accent;
            f.Goal = plan.Goal;
            f.Visible = plan.Visible && _flights.Count < C.MaxConcurrent;
            if (f.Visible)
            {
                f.Sr = Take(b.FaceSprite, false, FlyOrder + 2);
                f.Halo = Take(FxKit.Glow, true, FlyOrder);
            }
            _flights.Add(f);
            PoseFlight(f);
        }

        /// <returns>true: vardı (kaldırılmalı).</returns>
        private bool PoseFlight(Flight f)
        {
            var c = C;
            Vector3 pos;
            float size, sx = 1f, sy = 1f, rot = 0f, alpha = 1f;
            float t = f.Age;
            if (t < f.PopMs)
            {
                // Dışa sıçrama ve esneyerek sekme.
                float u = t / f.PopMs;
                pos = Vector3.Lerp(f.From, f.PopTo, EaseOutCubic(u));
                size = f.Size * c.MiniStart * Mathf.Lerp(0.8f, 1f, EaseOutCubic(u));
                float k = Mathf.Sin(u * Mathf.PI * 2f) * 0.16f * (1f - 0.5f * u);
                sx = 1f + k; sy = 1f - k;
                alpha = Mathf.Clamp01(u * 4f);
            }
            else if (t < f.Hold)
            {
                // Sıra bekleme: yerinde hafif süzülme.
                float h = (t - f.PopMs) / Mathf.Max(1f, f.Hold - f.PopMs);
                pos = f.PopTo + Vector3.up * Mathf.Sin(h * Mathf.PI) * Cell * 0.04f;
                size = f.Size * c.MiniStart;
            }
            else
            {
                float u = Mathf.Clamp01((t - f.Hold) / f.Fly);
                // Yavaş başlar, hızlanır, hedefe mıknatısla çekilir.
                float e = Mathf.Pow(u, Mathf.Max(1f, c.FlightEase)) * 0.75f + u * 0.25f;
                if (f.TargetFn != null) { var nt = f.TargetFn(); f.P2 += nt - f.Target; f.Target = nt; }
                pos = Cubic(f.PopTo, f.P1, f.P2, f.Target, e);
                // 2026-11-27 dalgalı uçuş: yola dik yönde sinüs dalgası; başta ve hedefte sıfır (simgenin içine iner).
                var chord = f.Target - f.PopTo;
                if (chord.sqrMagnitude > 1e-6f && !KatKat.UI.Motion.Reduced)
                {
                    var perp = new Vector3(-chord.y, chord.x, 0f).normalized;
                    pos += perp * (Mathf.Sin(e * Mathf.PI * 2f * c.WaveCycles + f.WavePhase) * Mathf.Sin(e * Mathf.PI) * Cell * c.WaveAmp);
                }
                size = f.Size * Mathf.Lerp(c.MiniStart, c.MiniEnd, e) * (u > 0.85f ? Mathf.Lerp(1f, 0.75f, (u - 0.85f) / 0.15f) : 1f); // 2026-11-27: hedefe büyük varır
                float st = 1f + c.Stretch * Mathf.Sin(u * Mathf.PI) * e;
                sx = 1f / Mathf.Sqrt(st); sy = st;
                rot = f.Spin * Mathf.Sin(u * Mathf.PI);
                // Kısa, yumuşak, kendi renginde iz.
                if (f.Visible && !KatKat.UI.Motion.Reduced && u > 0.04f && u < 0.96f && t - f.TrailAt >= (BoosterFxConfig.Current.LowQuality ? 36f : 18f))
                {
                    f.TrailAt = t;
                    var tc = Color.Lerp(f.Main, f.Accent, 0.3f);
                    Emit(FxKit.Glow, true, FlyOrder - 1, pos, Vector3.zero, c.TrailLife, size * c.TrailSize * 1.4f, size * 0.15f,
                        new Color(tc.r, tc.g, tc.b, c.TrailAlpha), new Color(tc.r, tc.g, tc.b, 0f));
                    // Işık çekirdeği: beyaza yakın, ince, izin ortasında.
                    Emit(FxKit.Glow, true, FlyOrder - 1, pos, Vector3.zero, c.TrailLife * 0.7f, size * c.TrailSize * 0.75f, size * 0.12f,
                        new Color(1f, 1f, 1f, 0.95f), new Color(tc.r, tc.g, tc.b, 0f)); // 2026-11-28: daha parlak çekirdek
                }
                // İz boyunca dökülen küçük pırıltılar.
                if (f.Visible && !KatKat.UI.Motion.Reduced && u > 0.08f && u < 0.92f && t - f.SparkAt >= c.SparkleEveryMs)
                {
                    f.SparkAt = t;
                    var sc = Color.Lerp(f.Accent, Color.white, 0.5f);
                    Emit(FxKit.Star, true, FlyOrder + 1, pos, new Vector3(R(-0.6f, 0.6f), R(-0.9f, -0.2f), 0f) * Cell, R(0.22f, 0.32f), size * 0.42f, size * 0.05f,
                        new Color(sc.r, sc.g, sc.b, 1f), new Color(f.Accent.r, f.Accent.g, f.Accent.b, 0f), drag: 2.5f, rot: R(0f, 45f), rotVel: R(-200f, 200f));
                }
                if (u >= 1f) return true;
            }
            if (f.Sr != null)
            {
                var p = pos; p.z -= 0.03f;
                f.Sr.transform.position = p;
                f.Sr.transform.rotation = Quaternion.Euler(0f, 0f, rot);
                Size(f.Sr, size, sx, sy);
                f.Sr.color = new Color(1f, 1f, 1f, alpha);
                var hc = Color.Lerp(f.Main, Color.white, 0.35f);
                f.Halo.transform.position = p;
                Size(f.Halo, size * 2.4f);
                f.Halo.color = new Color(hc.r, hc.g, hc.b, 0.7f * alpha); // 2026-11-27: daha görünür hale
            }
            return false;
        }

        /// <summary>Varış: simgeye emilir, küçük halka ve pırıltı; görev tamamlandıysa altın halka ve yıldızlar.</summary>
        private void Land(Flight f)
        {
            bool done = Arrived != null && Arrived(f.Goal);
            if (!f.Visible || KatKat.UI.Motion.Reduced) return;
            var c = C;
            float s = f.Size * 0.9f;
            var p = f.Target; p.z -= 0.04f;
            var rc = Color.Lerp(f.Main, f.Accent, 0.3f);
            Emit(FxKit.Ring, true, FlyOrder + 4, p, Vector3.zero, 0.32f, s * 0.35f, s * c.ImpactRing, new Color(rc.r, rc.g, rc.b, 1f), new Color(rc.r, rc.g, rc.b, 0f));
            Emit(FxKit.Glow, true, FlyOrder + 3, p, Vector3.zero, 0.26f, s * 0.6f, s * 1.9f, new Color(1f, 1f, 1f, 0.8f), new Color(rc.r, rc.g, rc.b, 0f)); // 2026-11-27: güçlü isabet
            int sp = Mathf.Max(1, Mathf.RoundToInt(c.ImpactSparkles * BoosterFxConfig.Current.ParticleK));
            for (int i = 0; i < sp; i++)
            {
                float a = (i + R(0f, 0.6f)) / sp * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Emit(FxKit.Star, true, FlyOrder + 5, p + d * s * 0.2f, d * s * 2.2f, 0.3f, s * 0.36f, s * 0.06f,
                    Color.white, new Color(f.Accent.r, f.Accent.g, f.Accent.b, 0f), drag: 4f, rot: R(0f, 45f));
            }
            if (!done) return;
            // Görev tamamlandı: daha güçlü, tek seferlik — küçük altın halka, birkaç yıldız.
            var gold = new Color(1f, 0.85f, 0.35f, 1f);
            float k = Mathf.Max(0.2f, c.CompleteIntensity);
            Emit(FxKit.Ring, true, FlyOrder + 6, p, Vector3.zero, 0.42f, s * 0.4f, s * 2.1f * k, new Color(1f, 0.93f, 0.6f, 0.95f), new Color(gold.r, gold.g, gold.b, 0f));
            Emit(FxKit.Glow, true, FlyOrder + 5, p, Vector3.zero, 0.34f, s * 0.8f, s * 2.4f * k, new Color(1f, 0.95f, 0.75f, 0.6f), new Color(gold.r, gold.g, gold.b, 0f));
            int st = Mathf.Max(3, Mathf.RoundToInt(6 * k * BoosterFxConfig.Current.ParticleK));
            for (int i = 0; i < st; i++)
            {
                float a = (i + R(0f, 0.5f)) / st * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Emit(FxKit.Star, true, FlyOrder + 7, p + d * s * 0.3f, d * s * R(2.6f, 3.8f), R(0.42f, 0.56f), s * 0.5f, s * 0.08f,
                    new Color(1f, 0.98f, 0.85f, 1f), new Color(gold.r, gold.g, gold.b, 0f), drag: 3.2f, rot: R(0f, 45f), rotVel: R(-120f, 120f));
            }
        }

        // ---- Zaman ---------------------------------------------------------------------------------

        public void Tick(float ms)
        {
            if (ms <= 0f) return;
            var c = C;
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                var b = _bursts[i];
                b.Age += ms;
                if (b.Stack == null) { EndBurst(i); continue; }
                PoseBurst(b);
                if (b.Age >= b.BurstAt + c.BurstMs) EndBurst(i);
            }
            for (int i = _flights.Count - 1; i >= 0; i--)
            {
                var f = _flights[i];
                f.Age += ms;
                if (PoseFlight(f))
                {
                    _flights.RemoveAt(i);
                    ReleaseFlight(f);
                    Land(f);
                }
            }
        }

        private void EndBurst(int i)
        {
            var b = _bursts[i];
            _bursts.RemoveAt(i);
            if (!b.Popped) Pop(b);
            if (b.Stack != null) b.Stack.ReleaseDetachedRim(b);
            Give(ref b.Face, false); Give(ref b.Bright, true); Give(ref b.Glow, true); Give(ref b.Rim, true);
            b.Stack = null;
            _burstPool.Push(b);
        }

        private void ReleaseFlight(Flight f)
        {
            Give(ref f.Sr, false);
            Give(ref f.Halo, true);
            _flightPool.Push(f);
        }

        /// <summary>
        /// Bütün patlama ve uçuşları hemen bitirir (yeniden başlat, geri al, bölüm değişimi, atlama). Varış geri çağrısı yapılmaz:
        /// çağıran mantıksal son durumu doğrudan gösterir (HUD SetGoalsImmediate).
        /// </summary>
        public void Cancel()
        {
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                var b = _bursts[i];
                if (b.Stack != null) b.Stack.ReleaseDetachedRim(b);
                b.Popped = true; // iptalde parçacık yok
                Give(ref b.Face, false); Give(ref b.Bright, true); Give(ref b.Glow, true); Give(ref b.Rim, true);
                b.Stack = null;
                _burstPool.Push(b);
            }
            _bursts.Clear();
            foreach (var f in _flights) ReleaseFlight(f);
            _flights.Clear();
        }

        // ---- Yardımcılar ----------------------------------------------------------------------------

        private static Vector3 Cubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float m = 1f - t;
            return m * m * m * a + 3f * m * m * t * b + 3f * m * t * t * c + t * t * t * d;
        }

        private static float EaseOutCubic(float x) { x = 1f - Mathf.Clamp01(x); return 1f - x * x * x; }
    }
}
