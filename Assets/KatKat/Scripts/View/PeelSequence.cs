using System;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Tek yığının tek katman "soyulması". v10 (kullanıcı, 2026-11-07): kâğıt gibi kıvrılma yerine TAŞ KIRILMASI —
    /// dokunuşta sıkışma, yüzde çatlak, taş kendi renginde yontulmuş parçalara ayrılır (toz), parçalar dağılır ve
    /// hedeflere uçar: renk görevse bir kısmı görev simgesine, kalanı yıldız (puan) çubuğuna; görev değilse hepsi
    /// yıldız çubuğuna. Zamanın saf fonksiyonudur (Evaluate(ms) pozu baştan hesaplar; kare yakalama ve yavaş çekim
    /// aynı kod). Süreler eskisiyle aynı: zincir zamanlaması (ReadableAt) ve oyun akışı değişmez. Kural çalıştırmaz.
    /// </summary>
    public sealed class PeelSequence
    {
        public const float Slow = 1.4f;
        public const float AnticipationEnd = 90f * Slow;
        public const float LiftStart = 50f * Slow;
        public const float LiftEnd = 160f * Slow;
        public const float PeelEnd = 410f * Slow;
        public const float BounceStart = 360f * Slow;
        public const float BounceEnd = 560f * Slow;
        public const float ReleaseStart = 400f * Slow;
        public const float ReleaseEnd = 640f * Slow;
        /// <summary>Alt renk okunur hâle geldiği an: zincirin sonraki dalgası buradan başlayabilir.</summary>
        public const float ReadableAt = 300f * Slow;
        public const float Duration = ReleaseEnd;

        /// <summary>Kırılma anı: çatlak tamamlanır, yüz parçalara ayrılır.</summary>
        public const float BreakAt = AnticipationEnd;
        /// <summary>Parçaların dağılma sonu / hedefe uçuş başlangıcı.</summary>
        public const float HomeStart = 300f * Slow;
        public const int ShardCount = 9, DustCount = 4;

        private readonly StackView _stack;
        private readonly PeelSheet _sheet;
        private readonly Vector3 _goalWorld, _starWorld;
        private readonly int _layerIndex, _order;
        private readonly Action<PeelSequence> _onArrive;
        private readonly Transform _fxParent;
        private readonly Color _color;
        private bool _rimReleased, _arrived, _broken;
        private SpriteRenderer _crack;
        private readonly SpriteRenderer[] _shards = new SpriteRenderer[ShardCount];
        private readonly SpriteRenderer[] _dust = new SpriteRenderer[DustCount];
        // Belirlenimli parça parametreleri (yığın konumundan tohum).
        private readonly Vector3[] _p0 = new Vector3[ShardCount], _v = new Vector3[ShardCount];
        private readonly float[] _spin = new float[ShardCount], _size = new float[ShardCount], _lag = new float[ShardCount];
        private readonly bool[] _toGoal = new bool[ShardCount];

        public LayerColor Color { get; }
        public bool IsDone { get; private set; }

        public PeelSequence(StackView stack, PeelSheet sheet, Vector3 goalWorld, int sortingOrder, Action<PeelSequence> onArrive)
            : this(stack, sheet, goalWorld, goalWorld, 1f, sortingOrder, onArrive, null) { }

        /// <param name="goalShare">Görev simgesine giden parça oranı (0: hepsi yıldız çubuğuna).</param>
        /// <param name="fxParent">Parçaların dünya uzayındaki üst nesnesi (yönetmen).</param>
        public PeelSequence(StackView stack, PeelSheet sheet, Vector3 goalWorld, Vector3 starWorld, float goalShare, int sortingOrder, Action<PeelSequence> onArrive, Transform fxParent)
        {
            _stack = stack;
            _sheet = sheet;
            _goalWorld = goalWorld;
            _starWorld = starWorld;
            _onArrive = onArrive;
            _order = sortingOrder;
            _fxParent = fxParent != null ? fxParent : stack.transform.parent;
            _layerIndex = stack.Layers.Count - 1;
            Color = stack.DetachTop(this);
            // Parça rengi (kullanıcı, 2026-11-07): taşın kendi rengiyle aynı (yüz rengi); yüzey gölgelemesi sprite'ta.
            _color = LayerPalette.FrontColor(Color);
            sheet.transform.SetParent(stack.transform, false);
            sheet.Begin(stack.Size, LayerPalette.FrontColor(Color), LayerPalette.IconCell(Color), sortingOrder);

            var p = stack.transform.position;
            var rng = new System.Random(Mathf.RoundToInt(p.x * 97f) * 7919 + Mathf.RoundToInt(p.y * 89f) * 31 + _layerIndex * 13 + 5);
            int toGoal = Mathf.RoundToInt(ShardCount * Mathf.Clamp01(goalShare));
            float s = stack.Size;
            for (int i = 0; i < ShardCount; i++)
            {
                // 3×3 ızgarada sapmalı başlangıç (yüzü kaplar), merkezden dışa hız + yukarı itiş.
                float gx = (i % 3 - 1) * 0.30f + ((float)rng.NextDouble() - 0.5f) * 0.12f;
                float gy = (i / 3 - 1) * 0.30f + ((float)rng.NextDouble() - 0.5f) * 0.12f;
                _p0[i] = new Vector3(gx * s, gy * s, 0f);
                var dir = new Vector3(gx, gy + 0.15f, 0f).normalized;
                float speed = (1.3f + (float)rng.NextDouble() * 1.1f) * s;
                _v[i] = dir * speed + new Vector3(0f, 1.2f * s, 0f);
                _spin[i] = ((float)rng.NextDouble() - 0.5f) * 720f;
                _size[i] = s * (0.36f + (float)rng.NextDouble() * 0.16f);
                _lag[i] = (float)rng.NextDouble() * 70f;
            }
            // Görev parçaları: karışık sırayla (iki yöne de yayılsın).
            for (int i = 0; i < toGoal; i++) _toGoal[(i * 4) % ShardCount] = true;
            Evaluate(0f);
        }

        public void Evaluate(float ms)
        {
            if (IsDone) return;
            float size = _stack.Size;

            // Gövde: dokunuşta hafif sıkışma; alt katman açılınca tek yumuşak sekme.
            var body = Vector2.one;
            if (ms < AnticipationEnd)
            {
                float k = Mathf.Sin(Mathf.PI * ms / AnticipationEnd) * 0.06f;
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

            if (!_broken) Break(faceLocal);

            // Parçalar: dağılma (yerçekimi) → hedefe süzülme; sonda küçülüp söner.
            var origin = _stack.transform.TransformPoint(faceLocal);
            float tb = (ms - BreakAt) / 1000f;
            float g = -5.5f * size;
            for (int i = 0; i < ShardCount; i++)
            {
                var sr = _shards[i];
                if (sr == null) continue;
                float local = Mathf.Max(0f, tb - _lag[i] / 1000f);
                float burstT = Mathf.Min(local, (HomeStart - BreakAt) / 1000f);
                var burst = origin + _p0[i] + _v[i] * burstT + new Vector3(0f, 0.5f * g * burstT * burstT, 0f);
                float home = Mathf.Clamp01((ms - HomeStart - _lag[i]) / (Duration - HomeStart - 70f));
                var target = _toGoal[i] ? _goalWorld : _starWorld;
                float e = home * home * (3f - 2f * home);
                var pos = Vector3.Lerp(burst, target, e);
                pos.y += Mathf.Sin(home * Mathf.PI) * 0.35f * size;
                sr.transform.position = new Vector3(pos.x, pos.y, origin.z - 0.01f);
                float sc = _size[i] * Mathf.Lerp(1f, 0.45f, e);
                sr.transform.localScale = new Vector3(sc, sc, 1f);
                sr.transform.rotation = Quaternion.Euler(0f, 0f, _spin[i] * (tb + 0.2f));
                float a = home < 0.85f ? 1f : Mathf.Lerp(1f, 0f, (home - 0.85f) / 0.15f);
                var c = _color;
                c.a = a;
                sr.color = c;
            }
            // Toz: kısa, açık renkli bulut (taş tozu), 250 ms'de söner.
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
                var dc = Color32ToColor(Color32.Lerp((Color32)_color, new Color32(255, 250, 240, 255), 0.7f));
                dc.a = Mathf.Clamp01(1f - dt) * 0.55f;
                d.color = dc;
            }

            if (ms >= ReleaseEnd)
            {
                _stack.SetBodyScale(this, Vector2.one);
                _stack.ReleaseDetachedRim(this);
                _sheet.End();
                Cleanup();
                IsDone = true;
                if (!_arrived)
                {
                    _arrived = true;
                    _onArrive?.Invoke(this);
                }
            }
        }

        private static Color Color32ToColor(Color32 c) => c;

        private void Break(Vector3 faceLocal)
        {
            _broken = true;
            if (!_rimReleased) { _rimReleased = true; _stack.ReleaseDetachedRim(this); }
            _sheet.End();
            if (_crack != null) { StoneShatter.Give(_crack); _crack = null; }
            for (int i = 0; i < ShardCount; i++)
            {
                var sr = StoneShatter.Take(_fxParent);
                sr.sprite = StoneShatter.Chip(i);
                sr.sortingOrder = _order + 2 + i % 2;
                _shards[i] = sr;
            }
            for (int i = 0; i < DustCount; i++)
            {
                var d = StoneShatter.Take(_fxParent);
                d.sprite = SpecialArt.Glow;
                d.sortingOrder = _order + 1;
                _dust[i] = d;
            }
        }

        private void Cleanup()
        {
            if (_crack != null) { StoneShatter.Give(_crack); _crack = null; }
            for (int i = 0; i < ShardCount; i++) { StoneShatter.Give(_shards[i]); _shards[i] = null; }
            for (int i = 0; i < DustCount; i++) { StoneShatter.Give(_dust[i]); _dust[i] = null; }
        }

        /// <summary>Animasyonu atlayıp son duruma geçer (ör. uygulama arka plana gittiğinde).</summary>
        public void Complete() => Evaluate(Duration);

        public PeelSheet Sheet => _sheet;
    }
}
