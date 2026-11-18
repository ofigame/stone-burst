using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Parçacık efektleri (kullanıcı 2026-11-18: "efektleri daha profesyonel yapmanı istiyorum. Patlama efekti, yıldırım, özel
    /// patlatma taşlarının efektleri. Hepsi daha profesyonel ve bu tarz oyunlara uygun olsun"; alev "çok bütün duruyor ateşe
    /// benzemiyor daha şeffaf ve yaygın olmalı"). Eklemeli malzemeyle parlayan katmanlar: ışık patlaması, şok dalgası halkası,
    /// hız izli kıvılcımlar, yıldız parıltıları, yükselen saydam alev pufları ve közler, roket izi.
    /// </summary>
    public sealed partial class BoardView
    {
        private FxParticles _particles;

        /// <summary>Tahtanın parçacık sistemi (tahta zamanıyla ilerler).</summary>
        public FxParticles Particles
        {
            get
            {
                if (_particles == null)
                {
                    _particles = new GameObject("Particles").AddComponent<FxParticles>();
                    _particles.transform.SetParent(transform, false);
                }
                FxParticles.Active = _particles;
                return _particles;
            }
        }

        private float WorldPitch => _pitch * Mathf.Max(1e-4f, transform.lossyScale.x);
        private static float R(float a, float b) => UnityEngine.Random.Range(a, b);

        /// <summary>
        /// Taş patlaması (her soyulan taş): taş renginde ışık patlaması, ince şok halkası, hız izli kıvılcımlar ve 2 yıldız.
        /// <paramref name="size"/> taş yüzü boyu (dünya).
        /// </summary>
        public static void StonePop(FxParticles fx, Vector3 pos, Color col, float size, int order)
        {
            if (fx == null) return;
            var bright = Color.Lerp(col, Color.white, 0.45f);
            var clear = col; clear.a = 0f;
            fx.Emit(FxKit.Glow, true, order, pos, Vector3.zero, 0.24f, size * 0.9f, size * 2.0f, new Color(bright.r, bright.g, bright.b, 0.85f), new Color(col.r, col.g, col.b, 0f));
            fx.Emit(FxKit.Ring, true, order + 1, pos, Vector3.zero, 0.32f, size * 0.4f, size * 1.9f, new Color(bright.r, bright.g, bright.b, 0.9f), clear);
            int n = 7;
            for (int i = 0; i < n; i++)
            {
                float a = (i + R(-0.35f, 0.35f)) / n * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                float sp = size * R(3.2f, 5.2f);
                fx.Emit(FxKit.Dot, true, order + 2, pos + dir * size * 0.2f, dir * sp + Vector3.up * size * 1.2f, R(0.32f, 0.5f),
                    size * R(0.16f, 0.24f), size * 0.05f, new Color(1f, 1f, 1f, 1f), new Color(bright.r, bright.g, bright.b, 0f),
                    gravity: -size * 9f, drag: 3.5f, stretch: 0.18f / Mathf.Max(0.01f, size));
            }
            for (int i = 0; i < 2; i++)
            {
                var off = new Vector3(R(-0.35f, 0.35f), R(-0.2f, 0.4f), 0f) * size;
                fx.Emit(FxKit.Star, true, order + 3, pos + off, Vector3.up * size * 0.6f, R(0.35f, 0.5f), size * 0.55f, size * 0.1f,
                    new Color(1f, 1f, 1f, 0.95f), new Color(bright.r, bright.g, bright.b, 0f), delay: R(0.02f, 0.12f), rot: R(0f, 45f), rotVel: R(-120f, 120f));
            }
        }

        /// <summary>Özel taş vuruşu / büyük patlama: güçlü ışık, iki halka, çok kıvılcım, yıldızlar.</summary>
        private void ImpactBurst(Vector3 pos, Color col, float sizeCells, int order)
        {
            var fx = Particles;
            float s = WorldPitch * sizeCells;
            var bright = Color.Lerp(col, Color.white, 0.5f);
            fx.Emit(FxKit.Glow, true, order, pos, Vector3.zero, 0.35f, s * 0.6f, s * 2.4f, new Color(1f, 1f, 1f, 0.95f), new Color(col.r, col.g, col.b, 0f));
            fx.Emit(FxKit.Ring, true, order + 1, pos, Vector3.zero, 0.42f, s * 0.3f, s * 2.6f, new Color(bright.r, bright.g, bright.b, 1f), new Color(col.r, col.g, col.b, 0f));
            fx.Emit(FxKit.Ring, true, order + 1, pos, Vector3.zero, 0.55f, s * 0.2f, s * 1.8f, new Color(1f, 1f, 1f, 0.6f), new Color(bright.r, bright.g, bright.b, 0f), delay: 0.08f);
            int n = 14;
            for (int i = 0; i < n; i++)
            {
                float a = (i + R(-0.4f, 0.4f)) / n * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                fx.Emit(FxKit.Dot, true, order + 2, pos, dir * s * R(3.5f, 6f), R(0.4f, 0.65f), s * R(0.12f, 0.2f), s * 0.03f,
                    Color.white, new Color(bright.r, bright.g, bright.b, 0f), gravity: -s * 4f, drag: 3f, stretch: 0.22f / Mathf.Max(0.01f, s));
            }
            for (int i = 0; i < 4; i++)
                fx.Emit(FxKit.Star, true, order + 3, pos + new Vector3(R(-0.5f, 0.5f), R(-0.5f, 0.5f), 0f) * s, Vector3.zero, R(0.4f, 0.6f), s * 0.7f, s * 0.1f,
                    Color.white, new Color(bright.r, bright.g, bright.b, 0f), delay: R(0f, 0.15f), rot: R(0f, 45f), rotVel: R(-90f, 90f));
        }

        /// <summary>Roket izi (her karede): sıcak ışık pufu ve geriye savrulan kıvılcımlar.</summary>
        private void RocketTrail(Vector3 pos, Vector3 dir, int order)
        {
            var fx = Particles;
            float s = WorldPitch;
            fx.Emit(FxKit.Glow, true, order - 1, pos, -dir * s * 0.3f, 0.28f, s * 0.7f, s * 0.2f, new Color(1f, 0.85f, 0.45f, 0.55f), new Color(1f, 0.4f, 0.1f, 0f));
            var side = new Vector3(-dir.y, dir.x, 0f);
            fx.Emit(FxKit.Dot, true, order, pos, (-dir * R(1.5f, 3f) + side * R(-1.2f, 1.2f)) * s, R(0.25f, 0.4f), s * 0.12f, s * 0.02f,
                new Color(1f, 0.95f, 0.7f, 1f), new Color(1f, 0.5f, 0.1f, 0f), gravity: -s * 3f, drag: 2.5f, stretch: 0.2f / Mathf.Max(0.01f, s));
        }

        /// <summary>
        /// Saydam, yaygın ateş: bir noktadan yükselen eklemeli puflar (sarı-beyaz çekirdek → turuncu → koyu kırmızı, sönerek
        /// genişler) ve közler. <paramref name="outward"/> tepsi kenarının dış normali.
        /// </summary>
        private void EmitFire(Vector3 pos, Vector3 outward, int order, float delay, float scale = 1f)
        {
            var fx = Particles;
            float s = WorldPitch * scale;
            for (int k = 0; k < 4; k++)
            {
                var vel = (Vector3.up * R(1.4f, 2.4f) + outward * R(0.5f, 1.0f) + Vector3.right * R(-0.35f, 0.35f)) * s;
                fx.Emit(FxKit.Puff, true, order, pos + new Vector3(R(-0.2f, 0.2f), R(-0.1f, 0.1f), 0f) * s, vel, R(0.5f, 0.8f),
                    s * R(0.6f, 0.85f), s * R(1.2f, 1.6f), new Color(1f, 0.78f, 0.38f, 0.75f), new Color(0.9f, 0.18f, 0.03f, 0f),
                    gravity: s * 1.2f, drag: 1.4f, delay: delay + k * R(0.04f, 0.09f), rot: R(0f, 360f), rotVel: R(-140f, 140f));
            }
            // Parlak çekirdek: küçük, hızlı sönen sarı-beyaz ışık.
            fx.Emit(FxKit.Glow, true, order + 1, pos, Vector3.up * s * 1.1f, 0.35f, s * 0.5f, s * 0.25f, new Color(1f, 0.95f, 0.7f, 0.6f), new Color(1f, 0.5f, 0.1f, 0f), delay: delay);
            EmitEmber(pos, outward, order + 1, delay + R(0f, 0.2f));
        }

        private void EmitEmber(Vector3 pos, Vector3 outward, int order, float delay)
        {
            float s = WorldPitch;
            Particles.Emit(FxKit.Dot, true, order, pos, (Vector3.up * R(1.2f, 2.2f) + outward * R(0.3f, 0.8f) + Vector3.right * R(-0.5f, 0.5f)) * s,
                R(0.7f, 1.2f), s * R(0.09f, 0.14f), s * 0.03f, new Color(1f, 0.9f, 0.55f, 1f), new Color(1f, 0.3f, 0.05f, 0f),
                gravity: s * 0.6f, drag: 1.2f, delay: delay, stretch: 0.08f / Mathf.Max(0.01f, s));
        }

        /// <summary>
        /// Sarmaşık vuruşu (2026-11-18): yapraklar dışa savrulur, dönerek ve sallanarak düşer, söner; birkaç yeşil
        /// toz zerresi. <paramref name="size"/> taş yüzü (dünya).
        /// </summary>
        private void LeafBurst(Vector3 pos, float size, int count, int order)
        {
            if (KatKat.UI.Motion.Reduced) return;
            var leaf = SpecialArt.VineLeaf;
            if (leaf == null) return;
            var fx = Particles;
            for (int i = 0; i < count; i++)
            {
                float a = (i + R(-0.4f, 0.4f)) / count * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.7f + 0.35f, 0f);
                float s = size * R(0.18f, 0.28f);
                var tint = Color.Lerp(Color.white, new Color(0.8f, 1f, 0.6f), R(0f, 0.5f));
                fx.Emit(leaf, false, order, pos + dir * size * 0.15f, dir * size * R(1.6f, 2.8f), R(0.9f, 1.3f), s, s * 0.85f,
                    tint, new Color(tint.r, tint.g, tint.b, 0f), gravity: -size * 3.2f, drag: 2.4f, rot: R(0f, 360f), rotVel: R(-260f, 260f));
            }
            for (int i = 0; i < 5; i++)
            {
                var d = new Vector3(R(-1f, 1f), R(-0.2f, 1f), 0f);
                fx.Emit(FxKit.Dot, true, order + 1, pos, d * size * R(1.5f, 2.5f), R(0.35f, 0.55f), size * 0.07f, size * 0.02f,
                    new Color(0.75f, 1f, 0.5f, 0.9f), new Color(0.3f, 0.8f, 0.2f, 0f), gravity: -size * 2f, drag: 2f);
            }
        }

        /// <summary>Tepsi ışıltısı (tepsinin arkasından dışa): eklemeli yıldız + yumuşak hale, hız izli.</summary>
        private void EmitTraySpark(Vector3 pos, Vector3 vel, Color col, float size, float life)
        {
            var fx = Particles;
            var bright = Color.Lerp(col, Color.white, 0.35f);
            fx.Emit(FxKit.Star, true, 0, pos, vel, life, size, size * 0.15f, new Color(bright.r, bright.g, bright.b, 1f), new Color(col.r, col.g, col.b, 0f),
                gravity: -WorldPitch * 0.8f, drag: 2.6f, rot: R(0f, 45f), rotVel: R(-100f, 100f));
            fx.Emit(FxKit.Glow, true, 0, pos, vel, life * 0.8f, size * 1.4f, size * 0.4f, new Color(col.r, col.g, col.b, 0.35f), new Color(col.r, col.g, col.b, 0f),
                gravity: -WorldPitch * 0.8f, drag: 2.6f);
        }
    }
}
