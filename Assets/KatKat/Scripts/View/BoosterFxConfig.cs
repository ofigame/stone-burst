using System;
using UnityEngine;

namespace KatKat.View
{
    public enum FxQuality { Low = 0, Normal = 1, High = 2 }

    /// <summary>
    /// Özel taş efektlerinin ayarları (2026-11-26, kullanıcı: Altın Rulo "Rainbow Blast" ve "Horizontal &amp; Vertical Rocket Blast"
    /// tarifleri: "Animasyon değerlerini kod içerisinde sabit olarak tanımlama … ScriptableObject … kod değiştirmeden animasyonun oyun
    /// hissiyatını test edebileyim"). Dosya: Resources/BoosterFx/BoosterFxConfig.asset (yoksa bu varsayılanlar). Süreler ms, uzunluklar
    /// hücre boyu birimi. Kalite: düşük cihazda parçacık sayısı ve ışın sınırı azalır.
    /// </summary>
    [CreateAssetMenu(menuName = "Kat Kat/Özel Taş Efekt Ayarları", fileName = "BoosterFxConfig")]
    public sealed class BoosterFxConfig : ScriptableObject
    {
        [Tooltip("Efekt kalitesi (Otomatik: düşük bellekli cihazda Düşük).")]
        public FxQuality Quality = FxQuality.Normal;
        public bool AutoLowOnWeakDevice = true;
        [Tooltip("Kaliteye göre parçacık çarpanı: Düşük, Normal, Yüksek.")]
        public Vector3 ParticleScaleByQuality = new Vector3(0.45f, 1f, 1.4f);

        [Serializable]
        public sealed class RainbowSettings
        {
            [Header("Aşama 1 – Aktivasyon")]
            public float ActivateMs = 150f;
            public float ActivateScale = 1.15f;
            public int OrbitParticles = 10;
            [Header("Aşama 2 – Hedefler")]
            public float TargetMs = 200f;
            public float TargetGrow = 1.1f;
            public float TargetGlow = 0.75f;
            [Header("Aşama 3 – Enerji ışınları")]
            public float BeamTravelMs = 190f;
            public float BeamWindowMs = 300f;
            public int MaxBeams = 14;
            public float BeamWidth = 0.13f;
            public float BeamGlowWidth = 0.42f;
            [Range(0f, 1f)] public float BeamCurve = 0.32f;
            public float BeamWobble = 0.12f;
            [Header("Aşama 4 – Jöle patlamaları")]
            public float ChainMs = 450f;
            [Tooltip("<1: patlamalar başta seyrek, sonra hızlanır.")]
            [Range(0.3f, 1f)] public float ChainAccel = 0.58f;
            public int JellyMin = 4, JellyMax = 7;
            public float JellySize = 0.2f;
            [Header("Aşama 5 – Final")]
            public float FinalMs = 250f;
            public float FinalWave = 4.5f;
            public float FinalFlash = 0.16f;
            public float Shake = 0.014f;
            [Tooltip("Hedef sayısı eşikleri: küçük ≤, orta ≤, büyük ≤ (üstü dev).")]
            public Vector3Int Tiers = new Vector3Int(5, 12, 20);
            [Header("Ses")]
            public float Volume = 0.9f;
            public float PitchStep = 0.35f;
            public int MaxPopSounds = 7;
            public float MinSoundGapMs = 55f;
            public float MaxTotalMs = 1400f;
        }

        [Serializable]
        public sealed class RocketSettings
        {
            [Header("Aşama 1 – Aktivasyon")]
            public float ActivateMs = 120f;
            public float ActivateScale = 1.15f;
            [Tooltip("Fırlatma öncesi ters yönde esneme (hücre).")]
            public float Recoil = 0.1f;
            [Header("Aşama 2-3 – Fırlatma ve hareket")]
            [Tooltip("Tam hıza çıkma süresi.")]
            public float AccelMs = 110f;
            [Tooltip("Tam hızda hücre başına süre.")]
            public float CellMs = 38f;
            public float TrailLength = 2.4f;
            public float TrailWidth = 0.34f;
            public Color TrailCore = new Color(1f, 0.98f, 0.82f, 1f);
            public Color TrailFlame = new Color(1f, 0.62f, 0.18f, 0.85f);
            public float Flame = 1f;
            public float Stretch = 0.3f;
            [Header("Aşama 4 – Çarpışma")]
            public int JellyMin = 4, JellyMax = 7;
            public float JellySize = 0.2f;
            public float Impact = 1f;
            [Header("Aşama 5-6 – Çevre ve çıkış")]
            public float NeighborWobble = 0.05f;
            public float LaneGlow = 0.55f;
            public float ExitCells = 1.2f;
            public float Shake = 0.012f;
            public Vector3Int Tiers = new Vector3Int(4, 8, 12);
            [Header("Ses")]
            public float Volume = 0.9f;
            public float PitchStep = 0.3f;
            public int MaxImpactSounds = 6;
            public float MinSoundGapMs = 45f;
            public float MaxTotalMs = 850f;
        }

        /// <summary>
        /// 2026-11-26 (kullanıcı: "Normal Taş Patlatma ve Hedefe Taş Toplama Animasyonu"): normal dokunuş, roket ve Altın Rulo
        /// patlamalarının ortak taş patlaması, hedefe uçan mini taş ve hedef isabeti ayarları (<see cref="StoneBlast"/>).
        /// Süreler ms, uzunluklar hücre boyu (taş boyu) birimi.
        /// </summary>
        [Serializable]
        public sealed class BlastSettings
        {
            [Tooltip("Kapalıysa eski kırılma/akış sunumu.")]
            public bool Enabled = true;
            [Header("1 – Dokunuş tepkisi (0–60 ms)")]
            public float TapReactMs = 60f;
            [Tooltip("Dokunulan taşın sıkışma ölçeği.")]
            public float TapSquash = 0.92f;
            [Tooltip("Gruptaki diğer taşların tepki sıkışması (dalga).")]
            public float RippleSquash = 0.96f;
            public float TapHighlight = 0.3f;
            [Header("2 – Patlama öncesi esneme (60–130 ms)")]
            [Tooltip("Dokunuşta patlama anı (taş başlangıcından).")]
            public float BurstAtMs = 130f;
            [Tooltip("Roket / Altın Rulo taşında patlama anı (roket ya da zincir vardığında patlar; önce kısa gerilme).")]
            public float SpecialBurstAtMs = 45f;
            public float StretchPeak = 1.12f;
            [Tooltip("Esnemede kristal gibi kısa sıkışma-uzama genliği.")]
            public float StretchSquash = 0.06f;
            public float RimGlow = 0.6f;
            [Header("3 – Patlama (130–250 ms)")]
            public float BurstMs = 120f;
            public float BurstExpand = 1.18f;
            public float BurstBrighten = 0.45f;
            public int ChipMin = 4, ChipMax = 8;
            public float ChipSize = 0.2f;
            public float ChipSpeed = 3.4f;
            public float ChipLife = 0.42f;
            public int MicroCount = 6;
            public float MicroSize = 0.075f;
            public float MicroSpeed = 3.8f;
            public float MicroLife = 0.34f;
            public int StarCount = 2;
            public int StreakCount = 2;
            public float RingFrom = 0.35f, RingTo = 1.2f;
            public float RingLife = 0.26f;
            public float RingAlpha = 0.5f;
            public float Glow = 0.35f;
            [Header("4 – Grup dalgası")]
            [Tooltip("Dokunulan hücreden uzaklık halkası başına gecikme (en çok / en az).")]
            public float RingStepMs = 22f;
            public float RingStepMinMs = 12f;
            [Tooltip("Büyük grupta dalganın toplam süresi bunu geçmez.")]
            public float MaxWaveMs = 200f;
            [Tooltip("Grup boyu eşikleri: küçük ≤ x (2–4), orta ≤ y (5–8), büyük ≤ z (9–12), üstü dev (13+).")]
            public Vector3Int Tiers = new Vector3Int(4, 8, 12);
            [Tooltip("Kademeye göre parçacık çarpanı: küçük, orta, büyük, dev.")]
            public Vector4 ParticleByTier = new Vector4(0.8f, 1f, 1.15f, 1.3f);
            [Tooltip("Kademeye göre titreşim: 0 yok, 1 tık, 2 hafif, 3 orta (küçük, orta, büyük, dev).")]
            public Vector4 HapticByTier = new Vector4(0f, 1f, 2f, 3f);
            [Tooltip("9+ grupta kontrollü küçük tahta sarsıntısı (dev grupta 1,5 kat).")]
            public float BigShake = 0.012f;
            [Header("5 – Hedefe uçuş")]
            [Tooltip("Mini kopyanın başlangıç boyu (taş boyu oranı).")]
            public float MiniStart = 0.58f;
            public float MiniEnd = 0.4f;
            public float PopMs = 90f;
            public float PopOut = 0.32f;
            [Tooltip("Roket taşında kopya önce roketin yönünde itilir (hücre).")]
            public float RocketNudge = 0.35f;
            public float FlightMinMs = 250f, FlightMaxMs = 450f;
            [Tooltip("Uçuşta bu uzaklıktan (hücre) uzun yol en uzun süreyi alır.")]
            public float FlightFarCells = 12f;
            public float ArcHeight = 1.4f;
            public float ArcWidth = 0.9f;
            public float StaggerMinMs = 15f, StaggerMaxMs = 35f;
            [Tooltip("Büyük grupta kopyalar arası aralık sıkışır: toplam en çok bu kadar.")]
            public float StaggerTotalMs = 260f;
            [Tooltip("Hızlanma üssü: >1 yavaş başlar, hızlanır, hedefe mıknatısla çekilir.")]
            public float FlightEase = 1.8f;
            public float Spin = 22f;
            public float Stretch = 0.14f;
            public float TrailLife = 0.16f;
            public float TrailSize = 0.55f;
            public float TrailAlpha = 0.45f;
            [Header("6 – Hedef isabeti")]
            public float IconBounce = 1.16f;
            public float IconBounceMs = 200f;
            public float CompletePunch = 1.26f;
            public float CompleteMs = 340f;
            [Tooltip("İsabet halkası (hedef simgesi boyu oranı).")]
            public float ImpactRing = 1.3f;
            public int ImpactSparkles = 3;
            [Tooltip("Görev tamamlanma efektinin şiddeti (halka, yıldız sayısı).")]
            public float CompleteIntensity = 1f;
            [Header("Ses ve sınırlar")]
            public float CollectVolume = 0.55f;
            public float CollectPitch = 2f;
            public float CollectPitchStep = 0.35f;
            public float CollectPitchJitter = 0.3f;
            public float CollectGapMs = 55f;
            [Tooltip("Aynı anda (250 ms içinde) en çok bu kadar toplama sesi.")]
            public int CollectVoices = 4;
            public float CompleteVolume = 0.9f;
            [Tooltip("Eşzamanlı görünür patlama/uçuş sınırı; aşan kopya görünmeden sayılır.")]
            public int MaxConcurrent = 60;
        }

        public RainbowSettings Rainbow = new RainbowSettings();
        public RocketSettings Rocket = new RocketSettings();
        public BlastSettings Blast = new BlastSettings();

        // ---- Erişim ----

        private static BoosterFxConfig _current;
        /// <summary>Editör deneme penceresi ve testler: kaliteyi zorla.</summary>
        public static FxQuality? QualityOverride;

        public static BoosterFxConfig Current
        {
            get
            {
                if (_current != null) return _current;
                _current = Resources.Load<BoosterFxConfig>("BoosterFx/BoosterFxConfig");
                if (_current == null) { _current = CreateInstance<BoosterFxConfig>(); _current.hideFlags = HideFlags.DontSave; }
                return _current;
            }
        }

        public FxQuality EffectiveQuality
        {
            get
            {
                if (QualityOverride.HasValue) return QualityOverride.Value;
                if (AutoLowOnWeakDevice && Application.isMobilePlatform && SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3000) return FxQuality.Low;
                return Quality;
            }
        }

        /// <summary>Parçacık sayısı çarpanı.</summary>
        public float ParticleK => EffectiveQuality == FxQuality.Low ? ParticleScaleByQuality.x : EffectiveQuality == FxQuality.High ? ParticleScaleByQuality.z : ParticleScaleByQuality.y;

        /// <summary>Düşük kalitede ince ayrıntılar (iz, hız çizgisi) azalır.</summary>
        public bool LowQuality => EffectiveQuality == FxQuality.Low;

        /// <summary>Hedef sayısına göre yoğunluk kademesi: 0 küçük, 1 orta, 2 büyük, 3 dev.</summary>
        public static int Tier(int count, Vector3Int t) => count <= t.x ? 0 : count <= t.y ? 1 : count <= t.z ? 2 : 3;

        // ---- Roket hareketi (mantıkla aynı zamanlama: taş roket vardığında patlar) ----

        /// <summary>Roketin fırlatmadan sonra <paramref name="cells"/> hücre yol alma süresi (ms): ivmelenme sonra sabit hız.</summary>
        public float RocketTravelMs(float cells)
        {
            var r = Rocket;
            float v = 1f / Mathf.Max(1f, r.CellMs); // hücre / ms
            float sa = 0.5f * v * r.AccelMs;          // ivmelenmede alınan yol
            if (cells <= sa) return Mathf.Sqrt(2f * cells * r.AccelMs / v);
            return r.AccelMs + (cells - sa) / v;
        }

        /// <summary>Fırlatmadan <paramref name="ms"/> sonra alınan yol (hücre).</summary>
        public float RocketCellsAt(float ms)
        {
            var r = Rocket;
            if (ms <= 0f) return 0f;
            float v = 1f / Mathf.Max(1f, r.CellMs);
            if (ms <= r.AccelMs) return 0.5f * v / r.AccelMs * ms * ms;
            return 0.5f * v * r.AccelMs + (ms - r.AccelMs) * v;
        }

        /// <summary>Taşın roketle temas anı (dalga başından ms).</summary>
        public float RocketArriveMs(float cells) => Rocket.ActivateMs + RocketTravelMs(Mathf.Max(0f, cells - 0.35f));

        /// <summary>Altın Rulo: sıradaki (0..n-1) hedefin patlama anı (dalga başından ms). Önce seyrek, sonra hızlanan zincir.</summary>
        public float RainbowPopMs(int rank, int count)
        {
            var r = Rainbow;
            float beamsEnd = r.ActivateMs + r.TargetMs + Mathf.Min(r.BeamWindowMs, r.BeamTravelMs + 40f * Mathf.Max(0, count - 1));
            int tier = Tier(count, r.Tiers);
            float chain = r.ChainMs * (tier == 0 ? 0.55f : tier == 1 ? 0.8f : 1f);
            float u = count <= 1 ? 0f : rank / (float)(count - 1);
            return beamsEnd + chain * Mathf.Pow(u, r.ChainAccel);
        }

        /// <summary>Altın Rulo: sıradaki hedefe ışının çıkış anı (dalga başından ms).</summary>
        public float RainbowBeamStartMs(int rank, int count)
        {
            var r = Rainbow;
            float start = r.ActivateMs + r.TargetMs;
            float stagger = count <= 1 ? 0f : Mathf.Min(40f, (r.BeamWindowMs - r.BeamTravelMs) / (count - 1));
            return start + rank * stagger;
        }
    }
}
