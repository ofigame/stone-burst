using System;
using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Özel taş patlama sunumu (2026-11-26, kullanıcı: "Unity – Rainbow Blast Power-Up Animasyonu" ve "Horizontal &amp; Vertical Rocket
    /// Blast Animasyon Sistemi" tarifleri). Yalnız görünüm: mantık (hangi taş ne zaman soyulur) RulesEngine'de, soyulma anı
    /// <see cref="BoosterFxConfig"/> zamanlamasıyla BoardView'da; bu sınıf aynı zamanlamayla ışın, roket, iz, jöle parçacığı,
    /// halka ve final efektlerini çizer. Tahta zamanıyla ilerler (<see cref="Tick"/>), bölüm değişince ya da atlamada
    /// <see cref="Cancel"/> ile güvenle biter (tahta durumu etkilenmez). Parçacıklar tahtanın havuzlu sisteminde
    /// (<see cref="FxParticles"/>); ışın ve izler havuzlu LineRenderer.
    /// </summary>
    public sealed class BoosterFx : MonoBehaviour
    {
        /// <summary>Ses isteği: ad (V17 ses), perde (yarım ton), düzey.</summary>
        public Action<string, float, float> Sound;
        /// <summary>Titreşim: 0 hafif, 1 orta, 2 güçlü.</summary>
        public Action<int> Haptic;

        private BoardView _board;
        private float _clock;
        private readonly List<(float T, Action A)> _plan = new List<(float, Action)>();
        private readonly Stack<LineRenderer> _linePool = new Stack<LineRenderer>();
        private readonly List<Beam> _beams = new List<Beam>();
        private readonly List<RocketFx> _rockets = new List<RocketFx>();
        private readonly List<Wobble> _wobbles = new List<Wobble>();
        private readonly List<Token> _tokens = new List<Token>();
        private readonly Stack<SpriteRenderer> _spritePool = new Stack<SpriteRenderer>();
        private float _lastSound = -999f;
        private int _soundsThisBlast;
        private static Material _lineMat, _lineAdd;
        private Material _spriteMat;

        public bool Busy => _plan.Count > 0 || _beams.Count > 0 || _rockets.Count > 0 || _tokens.Count > 0 || _shovel != null || _flyers.Count > 0;

        public static BoosterFx Attach(BoardView board)
        {
            var go = new GameObject("BoosterFx");
            go.transform.SetParent(board.transform, false);
            var fx = go.AddComponent<BoosterFx>();
            fx._board = board;
            return fx;
        }

        private BoosterFxConfig Cfg => BoosterFxConfig.Current;
        private float Cell => _board.WorldCell;
        /// <summary>Ebeveyn (tahta) ölçeği: yerel ölçek = dünya boyu / bu (efekt nesneleri ölçekli tahtanın altında).</summary>
        private float Lossy => Mathf.Max(1e-4f, transform.lossyScale.x);

        /// <summary>Tahtanın havuzlu parçacığı; boylar dünya biriminde verilir (yerel ölçeğe çevrilir).</summary>
        private void Emit(Sprite sprite, bool additive, int order, Vector3 pos, Vector3 vel, float life, float size0, float size1,
            Color c0, Color c1, float gravity = 0f, float drag = 0f, float delay = 0f, float rot = 0f, float rotVel = 0f, float stretch = 0f)
        {
            float inv = 1f / Lossy;
            _board.Particles.Emit(sprite, additive, order, pos, vel, life, size0 * inv, size1 * inv, c0, c1, gravity, drag, delay, rot, rotVel, stretch * Lossy);
        }
        private int Order => PeelDirector.PeelSortingOrder + 1000;
        private int N(int n) => Mathf.Max(1, Mathf.RoundToInt(n * Cfg.ParticleK));
        private static float R(float a, float b) => UnityEngine.Random.Range(a, b);

        // ---- Zamanlama ----------------------------------------------------------------------------

        private void At(float ms, Action a) => _plan.Add((_clock + ms, a));

        public void Tick(float ms)
        {
            if (ms <= 0f) return;
            _clock += ms;
            for (int i = 0; i < _plan.Count;)
            {
                if (_plan[i].T <= _clock) { var a = _plan[i].A; _plan.RemoveAt(i); a(); }
                else i++;
            }
            TickTokens(ms);
            TickBeams(ms);
            TickRockets(ms);
            TickWobbles(ms);
            TickLaneGlows(ms);
            TickShovel(ms);
            TickFlyers(ms);
        }

        /// <summary>Bütün efektleri hemen bitirir (bölüm değişimi, atlama). Tahta yığınlarının ölçeği geri yüklenir.</summary>
        public void Cancel()
        {
            _plan.Clear();
            foreach (var b in _beams) { Release(b.Core); Release(b.Glow); }
            _beams.Clear();
            foreach (var r in _rockets) { Release(r.Core); Release(r.Glow); Release(r.Sr); }
            _rockets.Clear();
            foreach (var w in _wobbles) if (w.T != null) w.T.localScale = w.Base;
            _wobbles.Clear();
            foreach (var t in _tokens) Release(t.Sr);
            _tokens.Clear();
            foreach (var t in _laneGlows) Release(t.Sr);
            _laneGlows.Clear();
            if (_shovel != null) { Release(_shovel); _shovel = null; }
            _shovelPts.Clear();
            foreach (var f in _flyers) { Release(f.Sr); Release(f.Glow); f.OnArrive?.Invoke(); }
            _flyers.Clear();
        }

        private void PlaySound(string name, float pitch, float vol, float gapMs, int max)
        {
            if (_clock - _lastSound < gapMs || _soundsThisBlast >= max) return;
            _lastSound = _clock;
            _soundsThisBlast++;
            Sound?.Invoke(name, pitch + R(-0.25f, 0.25f), vol * R(0.9f, 1.05f));
        }

        // ---- Havuz --------------------------------------------------------------------------------

        private LineRenderer Line(bool additive, int order)
        {
            LineRenderer lr = _linePool.Count > 0 ? _linePool.Pop() : null;
            if (lr == null)
            {
                lr = new GameObject("FxLine").AddComponent<LineRenderer>();
                lr.transform.SetParent(transform, false);
                lr.useWorldSpace = true;
                lr.numCapVertices = 6;
                lr.numCornerVertices = 2;
                lr.textureMode = LineTextureMode.Stretch;
                lr.alignment = LineAlignment.TransformZ;
            }
            if (_lineMat == null) _lineMat = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
            if (_lineAdd == null) _lineAdd = FxKit.Additive != null ? FxKit.Additive : _lineMat;
            lr.sharedMaterial = additive ? _lineAdd : _lineMat;
            lr.sortingOrder = order;
            lr.gameObject.SetActive(true);
            lr.positionCount = 0;
            return lr;
        }

        private void Release(LineRenderer lr)
        {
            if (lr == null) return;
            lr.positionCount = 0;
            lr.gameObject.SetActive(false);
            _linePool.Push(lr);
        }

        private SpriteRenderer Sprite(Sprite s, int order)
        {
            SpriteRenderer sr = _spritePool.Count > 0 ? _spritePool.Pop() : null;
            if (sr == null)
            {
                sr = new GameObject("FxSprite").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(transform, false);
                if (_spriteMat == null) _spriteMat = sr.sharedMaterial;
            }
            sr.sharedMaterial = _spriteMat;
            sr.gameObject.SetActive(true);
            sr.sprite = s;
            sr.sortingOrder = order;
            sr.color = Color.white;
            sr.transform.rotation = Quaternion.identity;
            sr.transform.localScale = Vector3.zero; // boyu çağıran verir (havuzdan dönen eski boy bir kare görünmesin)
            return sr;
        }

        private void Release(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
            _spritePool.Push(sr);
        }

        // ---- Ortak parçalar ------------------------------------------------------------------------

        private sealed class Token { public SpriteRenderer Sr; public Vector3 Pos, Base; public float Age, Dur, Peak, Squash; public Vector2 Axis; public bool Spin; }

        /// <summary>Özel taşın kendi simgesi: büyür (%115), esneyerek sıkışır (squash &amp; stretch), titrer; sonunda söner.</summary>
        private void PunchToken(Sprite s, Vector3 pos, float sizeWorld, float angle, float durMs, float peak, Vector2 axis, bool spin)
        {
            if (s == null) return;
            var sr = Sprite(s, Order + 8);
            float k = sizeWorld / Lossy / Mathf.Max(0.0001f, Mathf.Max(s.bounds.size.x, s.bounds.size.y));
            sr.transform.position = pos;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            sr.transform.localScale = Vector3.one * k; // oluştuğu karede de doğru boy
            _tokens.Add(new Token { Sr = sr, Pos = pos, Base = Vector3.one * k, Dur = durMs, Peak = peak, Axis = axis, Spin = spin });
        }

        private void TickTokens(float ms)
        {
            for (int i = _tokens.Count - 1; i >= 0; i--)
            {
                var t = _tokens[i];
                t.Age += ms;
                float x = Mathf.Clamp01(t.Age / t.Dur);
                // 0–40 %: %115'e büyüme (EaseOutBack), 40–85 %: hafif sıkışma ve titreme (enerji toplama), son: kısa genişleyip söner.
                float s = x < 0.4f ? Mathf.LerpUnclamped(1f, t.Peak, EaseOutBack(x / 0.4f))
                    : x < 0.85f ? Mathf.Lerp(t.Peak, 0.94f, EaseInOutSine((x - 0.4f) / 0.45f))
                    : Mathf.Lerp(0.94f, 1.25f, (x - 0.85f) / 0.15f);
                float sq = x >= 0.4f && x < 0.85f ? 0.08f * Mathf.Sin((x - 0.4f) / 0.45f * Mathf.PI) : 0f;
                float jit = x >= 0.4f && x < 0.85f ? Mathf.Sin(t.Age * 0.9f) * 0.03f * Cell : 0f;
                Vector3 axis = t.Axis.sqrMagnitude > 0f ? (Vector3)t.Axis.normalized : Vector3.right;
                t.Sr.transform.position = t.Pos + axis * jit;
                // Sıkışma ekseni boyunca kısalır, dikinde genişler (rotasyona göre yerel x).
                bool alongX = Mathf.Abs(axis.x) >= Mathf.Abs(axis.y);
                var sc = new Vector3(s * (alongX ? 1f - sq : 1f + sq), s * (alongX ? 1f + sq : 1f - sq), 1f);
                t.Sr.transform.localScale = Vector3.Scale(t.Base, sc);
                if (t.Spin) t.Sr.transform.rotation = Quaternion.Euler(0f, 0f, -t.Age * 0.35f);
                var c = Color.white; c.a = x < 0.85f ? 1f : 1f - (x - 0.85f) / 0.15f;
                t.Sr.color = c;
                if (x >= 1f) { Release(t.Sr); _tokens.RemoveAt(i); }
            }
        }

        /// <summary>Jöle parçaları: taş renginde 4–7 yuvarlak parça (+ yıldız), yön verilirse çoğu o yöne savrulur.</summary>
        private void Jelly(Vector3 pos, Color col, int min, int max, float size, Vector3 bias, float strength)
        {
            float s = Cell * size;
            var bright = Color.Lerp(col, Color.white, 0.3f);
            int n = N(UnityEngine.Random.Range(min, max + 1));
            for (int i = 0; i < n; i++)
            {
                float a = R(0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                if (bias.sqrMagnitude > 0f) dir = (dir * 0.55f + bias * R(0.6f, 1.1f)).normalized;
                float sp = Cell * R(2.4f, 4.4f) * strength;
                Emit(FxKit.Dot, false, Order + 3, pos + dir * Cell * 0.1f, dir * sp + Vector3.up * Cell * 0.8f, R(0.38f, 0.55f),
                    s * R(0.85f, 1.25f), s * 0.15f, new Color(bright.r, bright.g, bright.b, 1f), new Color(col.r, col.g, col.b, 0f),
                    gravity: -Cell * 7f, drag: 3.2f);
            }
            int stars = N(2);
            for (int i = 0; i < stars; i++)
                Emit(FxKit.Star, true, Order + 4, pos + new Vector3(R(-0.3f, 0.3f), R(-0.2f, 0.3f), 0f) * Cell, (Vector3.up * 0.6f + bias * 0.8f) * Cell, R(0.3f, 0.45f),
                    Cell * 0.42f, Cell * 0.08f, new Color(1f, 1f, 0.92f, 0.95f), new Color(bright.r, bright.g, bright.b, 0f), delay: R(0f, 0.06f), rot: R(0f, 45f), rotVel: R(-140f, 140f));
        }

        private void Flash(Vector3 pos, Color col, float sizeCells, float life, float alpha)
        {
            var c0 = Color.Lerp(col, Color.white, 0.55f); c0.a = alpha;
            var c1 = col; c1.a = 0f;
            Emit(FxKit.Glow, true, Order + 2, pos, Vector3.zero, life, Cell * sizeCells * 0.4f, Cell * sizeCells, c0, c1);
        }

        private void RingFx(Vector3 pos, Color col, float fromCells, float toCells, float life, float alpha)
        {
            var c0 = Color.Lerp(col, Color.white, 0.4f); c0.a = alpha;
            var c1 = col; c1.a = 0f;
            Emit(FxKit.Ring, true, Order + 2, pos, Vector3.zero, life, Cell * fromCells, Cell * toCells, c0, c1);
        }

        // ---- Hafif titreşen komşular ----------------------------------------------------------------

        private sealed class Wobble { public Transform T; public Vector3 Base; public float Age, Dur, Amp; public Vector2 Axis; }

        private void AddWobble(Transform t, float delay, float amp, Vector2 axis)
        {
            if (t == null || amp <= 0f) return;
            At(delay, () =>
            {
                foreach (var w in _wobbles) if (w.T == t) { w.Age = 0f; return; }
                _wobbles.Add(new Wobble { T = t, Base = t.localScale, Dur = 220f, Amp = amp, Axis = axis });
            });
        }

        private void TickWobbles(float ms)
        {
            for (int i = _wobbles.Count - 1; i >= 0; i--)
            {
                var w = _wobbles[i];
                w.Age += ms;
                if (w.T == null) { _wobbles.RemoveAt(i); continue; }
                float x = Mathf.Clamp01(w.Age / w.Dur);
                float k = w.Amp * Mathf.Sin(x * Mathf.PI * 3f) * (1f - x);
                bool along = Mathf.Abs(w.Axis.x) >= Mathf.Abs(w.Axis.y);
                w.T.localScale = Vector3.Scale(w.Base, new Vector3(along ? 1f + k : 1f - k * 0.6f, along ? 1f - k * 0.6f : 1f + k, 1f));
                if (x >= 1f) { w.T.localScale = w.Base; _wobbles.RemoveAt(i); }
            }
        }

        // ---- Altın Rulo (Rainbow Blast) ---------------------------------------------------------

        private sealed class Beam
        {
            public LineRenderer Core, Glow;
            public Vector3 From, To, Ctrl;
            public float Age, Travel, Width, Wobble, Seed;
            public Color Color;
            public bool Arrived;
        }

        /// <summary>
        /// Altın Rulo: aktivasyon (%115, squash &amp; stretch, dönen enerji parçacıkları, ışık halkası), hedeflerin parlayıp şişmesi,
        /// kaynaktan hedeflere kıvrımlı enerji ışınları (eşzamanlı sayı sınırlı), hızlanan jöle patlama zinciri, yumuşak enerji
        /// dalgası ve kısa final. <paramref name="targets"/> soyulma sırasıyla (rank) dünya konumu, rengi ve yığını.
        /// </summary>
        public void Rainbow(Vector3 src, Sprite token, List<(Vector3 Pos, Color Color, StackView Stack)> targets)
        {
            var c = Cfg.Rainbow;
            int n = targets.Count;
            int tier = BoosterFxConfig.Tier(n, c.Tiers);
            _soundsThisBlast = 0;
            Color gold = new Color(1f, 0.86f, 0.38f, 1f);
            Color main = n > 0 ? targets[0].Color : gold;

            // Aşama 1 – aktivasyon (dokunuşla aynı kare).
            PunchToken(token, src, Cell * 0.95f, 0f, c.ActivateMs + c.TargetMs, c.ActivateScale, Vector2.right, true);
            // 2026-11-26 (kullanıcı: "Efektler çok pasif"): daha büyük halka ve parlama, ardından ikinci beyaz halka.
            RingFx(src, gold, 0.3f, c.ActivateRing, 0.38f, 1f);
            RingFx(src, Color.white, 0.2f, c.ActivateRing * 0.7f, 0.3f, 0.8f);
            Flash(src, gold, c.ActivateFlash, 0.3f, 0.9f);
            int orbit = N(c.OrbitParticles);
            for (int i = 0; i < orbit; i++)
            {
                float a = i / (float)orbit * Mathf.PI * 2f;
                var col = n > 0 ? targets[i % n].Color : gold;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var tan = new Vector3(-dir.y, dir.x, 0f);
                Emit(FxKit.Dot, true, Order + 5, src + dir * Cell * 0.55f, tan * Cell * 3.2f - dir * Cell * 0.6f, (c.ActivateMs + c.TargetMs) / 1000f,
                    Cell * 0.16f, Cell * 0.06f, Color.Lerp(col, Color.white, 0.4f), new Color(col.r, col.g, col.b, 0f), drag: 0.8f);
            }
            Sound?.Invoke("special-charge", 0f, c.Volume * 0.8f);
            Haptic?.Invoke(1);

            // Aşama 2 – hedefler parlar, şişer, titrer.
            At(c.ActivateMs, () =>
            {
                Sound?.Invoke("roll-lightning", -2f, c.Volume * 0.55f);
                foreach (var t in targets)
                {
                    var col = t.Color;
                    Emit(FxKit.Glow, true, Order - 2, t.Pos, Vector3.zero, (c.TargetMs + c.BeamWindowMs) / 1000f, Cell * 0.9f, Cell * 1.25f,
                        new Color(Mathf.Lerp(col.r, 1f, 0.35f), Mathf.Lerp(col.g, 1f, 0.35f), Mathf.Lerp(col.b, 1f, 0.35f), c.TargetGlow), new Color(col.r, col.g, col.b, 0.2f));
                    if (t.Stack != null) AddWobble(t.Stack.transform, 0f, (c.TargetGrow - 1f) * 0.6f, Vector2.up);
                }
            });

            // Aşama 3 – enerji ışınları (eşzamanlı en çok MaxBeams; kalan hedefler aynı kümede).
            int maxBeams = Mathf.Max(3, Mathf.RoundToInt(c.MaxBeams * (Cfg.EffectiveQuality == FxQuality.Low ? 0.5f : Cfg.EffectiveQuality == FxQuality.High ? 1.3f : 1f)));
            int step = Mathf.Max(1, Mathf.CeilToInt(n / (float)maxBeams));
            for (int i = 0; i < n; i += step)
            {
                int rank = i;
                var t = targets[i];
                At(Cfg.RainbowBeamStartMs(rank, n), () => LaunchBeam(src, t.Pos, t.Color, c, rank));
            }

            // Aşama 4 – jöle patlamaları (taş soyulma anıyla aynı: RainbowPopMs).
            for (int i = 0; i < n; i++)
            {
                int rank = i;
                var t = targets[i];
                float at = Cfg.RainbowPopMs(rank, n);
                At(at, () =>
                {
                    float k = tier >= 2 ? 1.25f : 1f;
                    // 2026-11-26: ortak taş patlaması açıkken jöle parçalarını StoneBlast çizer (aynı an, çift parça olmasın).
                    if (!_board.BlastOn) Jelly(t.Pos, t.Color, c.JellyMin, c.JellyMax + (tier >= 2 ? 1 : 0), c.JellySize * k, Vector3.zero, k);
                    PlaySound("jelly-pop", Mathf.Min(7f, rank * c.PitchStep), c.Volume * 0.75f, c.MinSoundGapMs, c.MaxPopSounds);
                    if (rank % 4 == 0) Haptic?.Invoke(0);
                });
            }

            // Aşama 5 – final (son taştan hemen sonra).
            float last = n > 0 ? Cfg.RainbowPopMs(n - 1, n) : c.ActivateMs + c.TargetMs;
            At(last + 60f, () =>
            {
                float big = tier == 3 ? 1.35f : tier == 2 ? 1.15f : 1f;
                RingFx(src, gold, 0.5f, c.FinalWave * big, c.FinalMs / 1000f + 0.1f, 0.55f);
                RingFx(src, main, 0.3f, c.FinalWave * 0.7f * big, c.FinalMs / 1000f, 0.4f);
                Emit(FxKit.Glow, true, Order - 3, _board.WorldCenter, Vector3.zero, c.FinalMs / 1000f, Cell * 6f, Cell * 9f,
                    new Color(1f, 0.96f, 0.85f, c.FinalFlash * (tier >= 2 ? 1.2f : 1f)), new Color(1f, 0.9f, 0.7f, 0f));
                int st = N(tier >= 2 ? c.FinalStarsBig : c.FinalStars);
                for (int i = 0; i < st; i++)
                {
                    float a = R(0f, Mathf.PI * 2f);
                    var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    Emit(FxKit.Star, true, Order + 4, src + d * Cell * 0.3f, d * Cell * R(2.5f, 4.5f), R(0.4f, 0.6f), Cell * 0.45f, Cell * 0.06f,
                        new Color(1f, 0.97f, 0.85f, 1f), new Color(1f, 0.85f, 0.4f, 0f), drag: 2.5f, rot: R(0f, 45f), rotVel: R(-160f, 160f));
                }
                if (tier >= 1) _board.ShakeBoard(c.Shake * (tier == 3 ? 1.6f : tier == 2 ? 1.25f : 1f), 260f);
                Sound?.Invoke("goal-collect", tier >= 2 ? -2f : 0f, c.Volume);
                Haptic?.Invoke(tier >= 2 ? 2 : 1);
            });
        }

        private void LaunchBeam(Vector3 from, Vector3 to, Color col, BoosterFxConfig.RainbowSettings c, int rank)
        {
            var d = to - from;
            var perp = new Vector3(-d.y, d.x, 0f).normalized;
            float side = rank % 2 == 0 ? 1f : -1f;
            var ctrl = (from + to) * 0.5f + perp * side * d.magnitude * c.BeamCurve * R(0.6f, 1.1f);
            var b = new Beam
            {
                Core = Line(false, Order + 1), Glow = Line(true, Order),
                From = from, To = to, Ctrl = ctrl, Travel = c.BeamTravelMs, Width = Cell * c.BeamWidth, Wobble = Cell * c.BeamWobble,
                Seed = R(0f, 10f), Color = col,
            };
            _beams.Add(b);
            if (rank % 3 == 0) PlaySound("roll-lightning", 3f + rank * 0.1f, c.Volume * 0.35f, c.MinSoundGapMs, 99);
        }

        private Vector3 BeamPoint(Beam b, float u, float time)
        {
            float m = 1f - u;
            var p = m * m * b.From + 2f * m * u * b.Ctrl + u * u * b.To;
            var d = b.To - b.From;
            var perp = new Vector3(-d.y, d.x, 0f).normalized;
            return p + perp * Mathf.Sin(u * 9f + time * 0.02f + b.Seed) * b.Wobble * Mathf.Sin(u * Mathf.PI);
        }

        private void TickBeams(float ms)
        {
            const int S = 18;
            for (int i = _beams.Count - 1; i >= 0; i--)
            {
                var b = _beams[i];
                b.Age += ms;
                float head = Mathf.Clamp01(b.Age / b.Travel);
                head = EaseOutCubic(head);
                float tail = Mathf.Clamp01((b.Age - b.Travel - 90f) / 140f);
                if (!b.Arrived && b.Age >= b.Travel)
                {
                    b.Arrived = true;
                    var rc = BoosterFxConfig.Current.Rainbow;
                    Flash(b.To, b.Color, rc.BeamArriveFlash, 0.22f, 1f);
                    RingFx(b.To, b.Color, 0.2f, rc.BeamArriveFlash * 0.9f, 0.22f, 0.7f);
                }
                if (tail >= 1f) { Release(b.Core); Release(b.Glow); _beams.RemoveAt(i); continue; }
                b.Core.positionCount = S + 1;
                b.Glow.positionCount = S + 1;
                for (int k = 0; k <= S; k++)
                {
                    float u = Mathf.Lerp(tail, head, k / (float)S);
                    var p = BeamPoint(b, u, b.Age);
                    b.Core.SetPosition(k, p);
                    b.Glow.SetPosition(k, p);
                }
                float fade = 1f - tail;
                var inner = Color.Lerp(b.Color, Color.white, 0.65f);
                b.Core.startColor = new Color(inner.r, inner.g, inner.b, 0.95f * fade);
                b.Core.endColor = new Color(1f, 1f, 1f, fade);
                b.Glow.startColor = new Color(b.Color.r, b.Color.g, b.Color.b, 0.25f * fade);
                b.Glow.endColor = new Color(b.Color.r, b.Color.g, b.Color.b, 0.55f * fade);
                b.Core.widthMultiplier = b.Width;
                b.Glow.widthMultiplier = b.Width * (BoosterFxConfig.Current.Rainbow.BeamGlowWidth / Mathf.Max(0.01f, BoosterFxConfig.Current.Rainbow.BeamWidth));
                // Işın üzerinde ilerleyen enerji parçacığı.
                if (!b.Arrived && UnityEngine.Random.value < ms * BoosterFxConfig.Current.Rainbow.BeamSparkRate * Cfg.ParticleK)
                    Emit(FxKit.Dot, true, Order + 3, BeamPoint(b, head, b.Age), Vector3.zero, 0.18f, Cell * 0.14f, Cell * 0.03f,
                        new Color(1f, 1f, 1f, 1f), new Color(b.Color.r, b.Color.g, b.Color.b, 0f));
            }
        }

        // ---- Roket (Horizontal & Vertical Rocket Blast) ---------------------------------------------

        private sealed class RocketFx
        {
            public SpriteRenderer Sr;
            public LineRenderer Core, Glow;
            public Vector3 From, Dir;
            public float Age, LaunchAt, EndCells, Scale, OutAge;
            public readonly List<Vector3> Hist = new List<Vector3>();
            public bool Out;
        }

        /// <summary>
        /// Roket: aktivasyon (simge %115, sıkışma, ters yönde esneme, motor parçacıkları, ince halka), iki zıt yöne aynı anda
        /// ivmelenerek fırlayan yarılar (hıza göre uzar, sarı-turuncu yumuşak iz, kıvılcımlar, hız çizgileri), roket vardığı
        /// anda taş patlar (jöle parçaları hareket yönüne), geçilen şeritte kısa ışık, komşu sıralarda hafif titreşim, kenardan
        /// kısa çıkış ve final. <paramref name="lanes"/>: her şerit için başlangıç (dünya) ve iki yöndeki hücre sayısı;
        /// <paramref name="hits"/>: patlayan hücreler (dünya konumu, rengi, roket yolu hücre uzaklığı, yön işareti).
        /// </summary>
        public void Rocket(Sprite token, float tokenAngle, Vector3 src, Vector3 axis, List<(Vector3 Start, float Back, float Fwd)> lanes,
            List<(Vector3 Pos, Color Color, float Cells, int Sign)> hits, List<(Transform T, float Cells)> neighbours, Func<int, Sprite> half)
        {
            var c = Cfg.Rocket;
            int tier = BoosterFxConfig.Tier(hits.Count, c.Tiers);
            _soundsThisBlast = 0;
            axis = axis.normalized;
            var flame = c.TrailFlame;

            // Aşama 1 – aktivasyon: %115, sıkışma, ters yönde esneme beklentisi, motor kıvılcımları, ince halka.
            PunchToken(token, src, Cell * 0.95f, tokenAngle, c.ActivateMs + 30f, c.ActivateScale, axis, false);
            // 2026-11-26 (kullanıcı: "Efektler çok pasif"): halka 1,4 → ActivateRing, motor kıvılcımı 5 → LaunchSparks.
            RingFx(src, new Color(1f, 0.85f, 0.4f, 1f), 0.25f, c.ActivateRing, 0.26f, 1f);
            Flash(src, new Color(1f, 0.9f, 0.55f, 1f), c.LaunchFlash * 0.6f, c.ActivateMs / 1000f + 0.06f, 0.7f);
            for (int s = -1; s <= 1; s += 2)
            {
                var end = src + axis * s * Cell * 0.45f;
                int k = N(c.LaunchSparks);
                for (int i = 0; i < k; i++)
                    Emit(FxKit.Dot, true, Order + 5, end, (axis * s * R(0.3f, 1.2f) + new Vector3(R(-0.6f, 0.6f), R(-0.6f, 0.6f), 0f)) * Cell, c.ActivateMs / 1000f + 0.05f,
                        Cell * 0.14f, Cell * 0.04f, new Color(1f, 0.92f, 0.5f, 1f), new Color(flame.r, flame.g, flame.b, 0f), drag: 2f);
            }
            Sound?.Invoke("special-charge", 2f, c.Volume * 0.7f);
            Haptic?.Invoke(0);

            // Aşama 2 – iki zıt yöne fırlatma (her şerit).
            At(c.ActivateMs, () =>
            {
                Flash(src, new Color(1f, 0.86f, 0.45f, 1f), c.LaunchFlash, 0.22f, 1f);
                RingFx(src, new Color(1f, 0.95f, 0.75f, 1f), 0.3f, c.LaunchFlash * 0.9f, 0.24f, 0.85f);
                Sound?.Invoke("rocket-sweep", 0f, c.Volume);
                Haptic?.Invoke(lanes.Count > 1 ? 2 : 1);
                foreach (var lane in lanes)
                {
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float cells = (s < 0 ? lane.Back : lane.Fwd) + c.ExitCells;
                        var hs = half != null ? half(s) : null;
                        var r = new RocketFx
                        {
                            Sr = hs != null ? Sprite(hs, Order + 7) : null,
                            Core = Line(false, Order + 6), Glow = Line(true, Order + 5),
                            From = lane.Start, Dir = axis * s, EndCells = cells,
                            Scale = hs != null ? Cell * 1.3f / Lossy / Mathf.Max(0.0001f, token != null ? Mathf.Max(token.bounds.size.x, token.bounds.size.y) : Mathf.Max(hs.bounds.size.x, hs.bounds.size.y)) : 1f,
                        };
                        _rockets.Add(r);
                    }
                }
            });

            // Aşama 4 – çarpışmalar: roketin vardığı anda (BoardView soyulmayı aynı ana koyar).
            hits.Sort((a, b) => a.Cells.CompareTo(b.Cells));
            int idx = 0;
            float lastHit = c.ActivateMs;
            foreach (var h in hits)
            {
                var hit = h;
                int rank = idx++;
                float at = Cfg.RocketArriveMs(hit.Cells);
                lastHit = Mathf.Max(lastHit, at);
                At(at, () =>
                {
                    var dir = axis * hit.Sign;
                    Flash(hit.Pos, Color.Lerp(hit.Color, Color.white, 0.5f), c.ImpactFlash, 0.18f, Mathf.Min(1f, 0.8f * c.Impact));
                    RingFx(hit.Pos, hit.Color, 0.2f, c.ImpactRing, 0.22f, 0.65f);
                    float k = tier >= 2 ? 1.2f : 1f;
                    if (!_board.BlastOn) Jelly(hit.Pos, hit.Color, c.JellyMin, c.JellyMax, c.JellySize * k, dir, c.Impact * k); // ortak patlama açıkken StoneBlast
                    PlaySound("jelly-pop", Mathf.Min(6f, rank * c.PitchStep), c.Volume * 0.7f, c.MinSoundGapMs, c.MaxImpactSounds);
                    if (rank % 3 == 2) Haptic?.Invoke(0);
                });
            }

            // Aşama 5 – komşu şeritler hafifçe titrer (yer değiştirmez).
            foreach (var nb in neighbours) AddWobble(nb.T, Cfg.RocketArriveMs(nb.Cells), c.NeighborWobble, axis);

            // Aşama 6 – final.
            At(lastHit + 40f, () =>
            {
                Sound?.Invoke("goal-collect", tier >= 2 ? -3f : -1f, c.Volume * 0.8f);
                Haptic?.Invoke(tier >= 2 ? 2 : 1);
                _board.ShakeBoard(c.Shake * (tier == 3 ? 1.6f : tier == 2 ? 1.3f : 1f), 220f, new Vector2(Mathf.Abs(axis.x) + 0.15f, Mathf.Abs(axis.y) + 0.15f));
            });
        }

        private void TickRockets(float ms)
        {
            var c = Cfg.Rocket;
            for (int i = _rockets.Count - 1; i >= 0; i--)
            {
                var r = _rockets[i];
                r.Age += ms;
                float cells = Cfg.RocketCellsAt(r.Age);
                float speed = (Cfg.RocketCellsAt(r.Age + 8f) - cells) / 8f * c.CellMs; // 0..1 (tam hız)
                bool done = cells >= r.EndCells;
                cells = Mathf.Min(cells, r.EndCells);
                var pos = r.From + r.Dir * cells * Cell;
                if (!r.Out && done)
                {
                    r.Out = true;
                    Flash(pos, new Color(1f, 0.85f, 0.45f, 1f), 1.6f, 0.2f, 0.9f);
                }
                float fadeK = r.Out ? 1f : 0f;
                // İz: geçmiş konumlar, uzunluk sınırlı; çekirdek beyaz-sarı, dış alev turuncu-altın; uca doğru incelir.
                // 2026-11-27 (kullanıcı: "roketleri kullandıktan sonra ekranda birşey kalıyor titreyen. köşelerde kalıyor"): çıkıştan
                // sonra yeni nokta eklenmez; önce her karede bir nokta eklenip bir nokta düştüğü için iz 3 noktada takılı kalıyordu.
                if (!r.Out || r.Hist.Count == 0) r.Hist.Insert(0, pos);
                if (r.Out) r.OutAge += ms;
                float maxLen = c.TrailLength * Cell * Mathf.Lerp(0.4f, 1f, speed);
                float len = 0f;
                int keep = 1;
                for (; keep < r.Hist.Count; keep++)
                {
                    len += (r.Hist[keep] - r.Hist[keep - 1]).magnitude;
                    if (len > maxLen) break;
                }
                if (r.Hist.Count > keep + 1) r.Hist.RemoveRange(keep + 1, r.Hist.Count - keep - 1);
                if (r.Out)
                {
                    // Çıkışta iz hızla kısalır.
                    int drop = Mathf.Max(2, r.Hist.Count / 3);
                    r.Hist.RemoveRange(r.Hist.Count - Mathf.Min(drop, r.Hist.Count - 1), Mathf.Min(drop, r.Hist.Count - 1));
                }
                int n = r.Hist.Count;
                r.Core.positionCount = n;
                r.Glow.positionCount = n;
                for (int k = 0; k < n; k++)
                {
                    var p = r.Hist[k];
                    var side = new Vector3(-r.Dir.y, r.Dir.x, 0f) * Mathf.Sin(k * 0.9f + r.Age * 0.05f) * Cell * 0.03f * (k / (float)Mathf.Max(1, n));
                    r.Core.SetPosition(k, p + side);
                    r.Glow.SetPosition(k, p - side);
                }
                float w = Cell * c.TrailWidth;
                r.Core.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.1f);
                r.Glow.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.25f);
                r.Core.widthMultiplier = w * 0.45f;
                r.Glow.widthMultiplier = w * 1.25f * c.Flame;
                r.Core.startColor = c.TrailCore; r.Core.endColor = new Color(c.TrailCore.r, c.TrailCore.g, c.TrailCore.b, 0f);
                r.Glow.startColor = c.TrailFlame; r.Glow.endColor = new Color(1f, 0.8f, 0.35f, 0f);
                // Roket yarısı: hız yönünde uzar; çıkışta söner.
                if (r.Sr != null)
                {
                    r.Sr.transform.position = pos;
                    float st = 1f + c.Stretch * speed;
                    bool horiz = Mathf.Abs(r.Dir.x) > 0.5f;
                    r.Sr.transform.localScale = new Vector3(r.Scale * (horiz ? st : 1f / Mathf.Sqrt(st)), r.Scale * (horiz ? 1f / Mathf.Sqrt(st) : st), 1f);
                    var col = Color.white; col.a = r.Out ? 0f : 1f;
                    r.Sr.color = col;
                }
                // Kıvılcımlar ve hız çizgileri.
                if (!r.Out && UnityEngine.Random.value < ms * c.SparkRate * Cfg.ParticleK)
                {
                    var back = -r.Dir;
                    Emit(FxKit.Dot, true, Order + 4, pos + back * Cell * 0.3f, (back * R(1f, 2.5f) + new Vector3(R(-0.9f, 0.9f), R(-0.9f, 0.9f), 0f)) * Cell, R(0.18f, 0.32f),
                        Cell * R(0.08f, 0.14f), Cell * 0.02f, new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.55f, 0.15f, 0f), drag: 3f, stretch: 0.12f / Mathf.Max(0.01f, Cell));
                }
                if (!r.Out && speed > 0.7f && UnityEngine.Random.value < ms * c.StreakRate * Cfg.ParticleK)
                {
                    var off = new Vector3(-r.Dir.y, r.Dir.x, 0f) * R(-0.45f, 0.45f) * Cell;
                    Emit(FxKit.Streak, true, Order + 3, pos - r.Dir * Cell * 0.8f + off, r.Dir * Cell * 4f, 0.14f, Cell * 0.5f, Cell * 0.2f,
                        new Color(1f, 1f, 0.9f, 0.7f), new Color(1f, 0.9f, 0.6f, 0f), stretch: 0.2f / Mathf.Max(0.01f, Cell));
                }
                if (r.Out && (n <= 2 || r.OutAge > 400f))
                {
                    Release(r.Core); Release(r.Glow); Release(r.Sr);
                    _rockets.RemoveAt(i);
                }
            }
        }

        /// <summary>Roket geçtikten sonra şeritte kalan ince ışık: merkezden dışa genişleyip söner.</summary>
        public void LaneGlow(Vector3 center, Vector3 axis, float cellsBack, float cellsFwd)
        {
            var c = Cfg.Rocket;
            if (c.LaneGlow <= 0f) return;
            float len = (cellsBack + cellsFwd + 1f) * Cell;
            var mid = center + axis.normalized * (cellsFwd - cellsBack) * 0.5f * Cell;
            At(c.ActivateMs + 40f, () =>
            {
                var sr = Sprite(FxKit.Streak, Order - 1);
                if (FxKit.Additive != null) sr.sharedMaterial = FxKit.Additive;
                sr.transform.position = mid;
                sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);
                var size = FxKit.Streak.bounds.size;
                sr.color = new Color(1f, 1f, 1f, 0f);
                var tk = new Token { Sr = sr, Pos = mid, Base = new Vector3(len / Lossy / size.x, Cell * 0.55f / Lossy / size.y, 1f), Dur = 380f };
                _laneGlows.Add(tk);
            });
        }

        private readonly List<Token> _laneGlows = new List<Token>();

        private void TickLaneGlows(float ms)
        {
            for (int i = _laneGlows.Count - 1; i >= 0; i--)
            {
                var t = _laneGlows[i];
                t.Age += ms;
                float x = Mathf.Clamp01(t.Age / t.Dur);
                t.Sr.transform.localScale = new Vector3(t.Base.x * Mathf.Lerp(0.2f, 1f, EaseOutCubic(Mathf.Min(1f, x * 2.2f))), t.Base.y * (1f - x * 0.6f), 1f);
                t.Sr.color = new Color(1f, 0.93f, 0.7f, Cfg.Rocket.LaneGlow * (1f - x));
                if (x >= 1f) { Release(t.Sr); _laneGlows.RemoveAt(i); }
            }
        }

        // ---- Kürek turu (2026-11-26, kullanıcı: "Kürekte tahtada olsun ve üzerine basıldığında kendisi tek tek gidip çakılları
        // temizlesin") ----

        private SpriteRenderer _shovel;
        private readonly List<Vector3> _shovelPts = new List<Vector3>();
        private float _shovelAge, _shovelHop, _shovelScale;

        /// <summary>Kürek simgesi sırayla her çakıla zıplar (hop başına <paramref name="hopMs"/>); kazma anını BoardView aynı sırayla çalar.</summary>
        public void ShovelTour(Sprite s, Vector3 from, List<Vector3> pts, float hopMs)
        {
            if (s == null || pts.Count == 0) return;
            if (_shovel != null) Release(_shovel);
            _shovel = Sprite(s, Order + 8);
            _shovelScale = Cell * 0.85f / Lossy / Mathf.Max(0.0001f, Mathf.Max(s.bounds.size.x, s.bounds.size.y));
            _shovelPts.Clear();
            _shovelPts.Add(from);
            _shovelPts.AddRange(pts);
            _shovelAge = 0f;
            _shovelHop = hopMs;
            _shovel.transform.position = from;
            _shovel.transform.localScale = Vector3.one * _shovelScale;
        }

        private void TickShovel(float ms)
        {
            if (_shovel == null) return;
            _shovelAge += ms;
            int hops = _shovelPts.Count - 1;
            float f = _shovelAge / _shovelHop;
            if (f >= hops + 0.6f) { Release(_shovel); _shovel = null; _shovelPts.Clear(); return; }
            int i = Mathf.Min(hops - 1, Mathf.FloorToInt(f));
            float u = Mathf.Clamp01(f - i);
            var a = _shovelPts[i]; var b = _shovelPts[i + 1];
            float e = EaseInOutSine(u);
            var p = Vector3.Lerp(a, b, e) + Vector3.up * Mathf.Sin(u * Mathf.PI) * Cell * 0.45f;
            _shovel.transform.position = p;
            // Varışta kazma: hafif eğilip sıkışır.
            float dig = u > 0.8f ? Mathf.Sin((u - 0.8f) / 0.2f * Mathf.PI) : 0f;
            _shovel.transform.rotation = Quaternion.Euler(0f, 0f, -25f * dig + Mathf.Sin(u * Mathf.PI) * 10f);
            _shovel.transform.localScale = new Vector3(_shovelScale * (1f + 0.12f * dig), _shovelScale * (1f - 0.12f * dig), 1f);
            var col = Color.white; col.a = f > hops ? 1f - (f - hops) / 0.6f : 1f;
            _shovel.color = col;
        }

        // ---- Simge uçuşu (2026-11-26, kullanıcı: "Roket ve rulo yine tahtanın üst kısmında olsun … Roketlere tıklandığında
        // anlaşılmıyor") ----

        private sealed class Flyer
        {
            public SpriteRenderer Sr, Glow;
            public Vector3 From, To, Ctrl;
            public float Age, Dur, K0, K1, Swell, TrailRate, GlowK, Angle;
            public Color Trail;
            public Action OnArrive;
            public bool Arrive;
        }

        private readonly List<Flyer> _flyers = new List<Flyer>();

        /// <summary>
        /// Simge yay çizerek uçar: hızlı başlar, ortada büyür, arkasında ışık izi ve yıldızcıklar; varışta (isteğe bağlı) parlama ve
        /// halka, sonra <paramref name="onArrive"/>. Boylar dünya biriminde (simgenin uzun kenarı). Tepsiden hedef hücreye kullanım
        /// uçuşu ve doğan taşın tepsiye uçuşu bunu kullanır.
        /// </summary>
        public void FlyIcon(Sprite s, float angle, Vector3 from, Vector3 to, float ms, float size0, float size1, float arcCells, float swell,
            float trailRate, Color trail, bool arriveFx, Action onArrive)
        {
            if (s == null || ms <= 0f) { onArrive?.Invoke(); return; }
            float span = Mathf.Max(0.0001f, Mathf.Max(s.bounds.size.x, s.bounds.size.y));
            var sr = Sprite(s, Order + 30);
            var glow = Sprite(FxKit.Glow, Order + 29);
            if (FxKit.Additive != null) glow.sharedMaterial = FxKit.Additive;
            float k0 = size0 / Lossy / span, k1 = size1 / Lossy / span;
            sr.transform.position = from;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            sr.transform.localScale = Vector3.one * k0; // ilk karede doğru boy
            float gk = 1f / Lossy / Mathf.Max(0.0001f, FxKit.Glow.bounds.size.x);
            glow.transform.position = from;
            glow.transform.localScale = Vector3.one * size0 * 1.9f * gk;
            glow.color = new Color(trail.r, trail.g, trail.b, 0.8f);
            var mid = (from + to) * 0.5f;
            var d = to - from;
            // Yay: uçuş yönüne dik, yukarı doğru (dik iniş ve yatay uçuşta da yukarı kabarır).
            var perp = new Vector3(-d.y, d.x, 0f).normalized;
            if (perp.y < 0f) perp = -perp;
            if (perp.sqrMagnitude < 0.01f) perp = Vector3.up;
            var ctrl = mid + perp * arcCells * Cell + Vector3.up * arcCells * Cell * 0.35f;
            _flyers.Add(new Flyer { Sr = sr, Glow = glow, From = from, To = to, Ctrl = ctrl, Dur = ms, K0 = k0, K1 = k1, Swell = swell, TrailRate = trailRate, Trail = trail, GlowK = gk, Angle = angle, OnArrive = onArrive, Arrive = arriveFx });
            Flash(from, trail, size0 / Mathf.Max(0.0001f, Cell) * 1.4f, 0.16f, 0.8f);
        }

        private void TickFlyers(float ms)
        {
            for (int i = _flyers.Count - 1; i >= 0; i--)
            {
                var f = _flyers[i];
                float prevU = Mathf.Clamp01(f.Age / f.Dur);
                f.Age += ms;
                float u = Mathf.Clamp01(f.Age / f.Dur);
                float e = 1f - (1f - u) * (1f - u) * 0.65f - (1f - u) * 0.35f; // hızlı çıkış, hedefe yumuşak oturuş
                e = Mathf.Clamp01(Mathf.Lerp(u, e, 0.7f));
                float m = 1f - e;
                var p = m * m * f.From + 2f * m * e * f.Ctrl + e * e * f.To;
                float k = Mathf.Lerp(f.K0, f.K1, e) * (1f + f.Swell * Mathf.Sin(u * Mathf.PI));
                f.Sr.transform.position = p;
                f.Sr.transform.localScale = Vector3.one * k;
                f.Sr.transform.rotation = Quaternion.Euler(0f, 0f, f.Angle + Mathf.Sin(u * Mathf.PI) * 12f);
                f.Glow.transform.position = p;
                float size = k * Lossy * Mathf.Max(f.Sr.sprite.bounds.size.x, f.Sr.sprite.bounds.size.y);
                f.Glow.transform.localScale = Vector3.one * size * 1.9f * f.GlowK;
                f.Glow.color = new Color(f.Trail.r, f.Trail.g, f.Trail.b, 0.85f);
                // İz: yol boyunca (kare hızından bağımsız) ışık noktaları ve ara ara yıldız.
                if (!KatKat.UI.Motion.Reduced && f.TrailRate > 0f)
                {
                    int n = Mathf.Max(1, Mathf.RoundToInt(ms * f.TrailRate * Cfg.ParticleK));
                    for (int j = 0; j < n; j++)
                    {
                        float uu = Mathf.Lerp(prevU, u, (j + 1f) / n);
                        float ee = Mathf.Clamp01(Mathf.Lerp(uu, 1f - (1f - uu) * (1f - uu) * 0.65f - (1f - uu) * 0.35f, 0.7f));
                        float mm = 1f - ee;
                        var q = mm * mm * f.From + 2f * mm * ee * f.Ctrl + ee * ee * f.To;
                        bool star = (j & 3) == 0;
                        Emit(star ? FxKit.Star : FxKit.Dot, true, Order + 28, q + new Vector3(R(-0.1f, 0.1f), R(-0.1f, 0.1f), 0f) * size,
                            new Vector3(R(-0.5f, 0.5f), R(-0.5f, 0.2f), 0f) * Cell, R(0.22f, 0.34f), size * (star ? 0.38f : 0.26f), size * 0.04f,
                            Color.Lerp(f.Trail, Color.white, 0.45f), new Color(f.Trail.r, f.Trail.g, f.Trail.b, 0f), drag: 2.5f, rot: R(0f, 45f), rotVel: R(-120f, 120f));
                    }
                }
                if (u >= 1f)
                {
                    if (f.Arrive)
                    {
                        var tc = Cfg.Tray;
                        Flash(f.To, f.Trail, tc.UseArriveFlash, 0.2f, 1f);
                        RingFx(f.To, f.Trail, 0.25f, tc.UseArriveRing, 0.24f, 0.9f);
                    }
                    Release(f.Sr); Release(f.Glow);
                    _flyers.RemoveAt(i);
                    f.OnArrive?.Invoke();
                }
            }
        }

        /// <summary>Uçan simge var mı (testler, yakalama).</summary>
        public int FlyingIcons => _flyers.Count;

        // ---- Yumuşatmalar -------------------------------------------------------------------------

        private static float EaseOutCubic(float x) { x = 1f - Mathf.Clamp01(x); return 1f - x * x * x; }
        private static float EaseInOutSine(float x) => -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(x)) - 1f) * 0.5f;
        private static float EaseOutBack(float x) { const float c1 = 1.70158f, c3 = c1 + 1f; x = Mathf.Clamp01(x) - 1f; return 1f + c3 * x * x * x + c1 * x * x; }
    }
}
