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
            // 2026-11-26 (kullanıcı: "Efektler çok pasif. Mevcut efektleride koruyarak daha yoğun efekt yapalım"): değerler yükseldi
            // (önce 1,15 ölçek, 10 parçacık; halka 2,2, parlama 1,6 hücre).
            public float ActivateScale = 1.3f;
            public int OrbitParticles = 18;
            [Tooltip("Aktivasyon halkası ve parlaması (hücre).")]
            public float ActivateRing = 3.2f, ActivateFlash = 2.6f;
            [Header("Aşama 2 – Hedefler")]
            public float TargetMs = 200f;
            public float TargetGrow = 1.1f;
            public float TargetGlow = 0.95f;
            [Header("Aşama 3 – Enerji ışınları")]
            public float BeamTravelMs = 190f;
            public float BeamWindowMs = 300f;
            public int MaxBeams = 20;
            public float BeamWidth = 0.19f;
            public float BeamGlowWidth = 0.66f;
            [Tooltip("Işın hedefe varınca parlama boyu (hücre) ve ışın üstündeki parçacık sıklığı (ms başına).")]
            public float BeamArriveFlash = 1.6f, BeamSparkRate = 0.07f;
            [Range(0f, 1f)] public float BeamCurve = 0.32f;
            public float BeamWobble = 0.12f;
            [Header("Aşama 4 – Jöle patlamaları")]
            public float ChainMs = 450f;
            [Tooltip("<1: patlamalar başta seyrek, sonra hızlanır.")]
            [Range(0.3f, 1f)] public float ChainAccel = 0.58f;
            public int JellyMin = 5, JellyMax = 9;
            public float JellySize = 0.26f;
            [Header("Aşama 5 – Final")]
            public float FinalMs = 250f;
            public float FinalWave = 6f;
            public float FinalFlash = 0.28f;
            [Tooltip("Finalde saçılan yıldız: küçük/orta hamle, büyük hamle.")]
            public int FinalStars = 10, FinalStarsBig = 20;
            public float Shake = 0.022f;
            [Tooltip("Taşlar patladığı anda tahta sarsıntısı (önce 0,025, sabit).")]
            public float PopShake = 0.034f;
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
            // 2026-11-26 (kullanıcı: "Efektler çok pasif"): önce 1,15 ölçek, halka 1,4, parlama 1,6 hücre, 5 motor kıvılcımı.
            public float ActivateScale = 1.3f;
            [Tooltip("Fırlatma öncesi ters yönde esneme (hücre).")]
            public float Recoil = 0.14f;
            [Tooltip("Aktivasyon halkası, fırlatma parlaması (hücre) ve her uçtaki motor kıvılcımı.")]
            public float ActivateRing = 2.4f, LaunchFlash = 2.6f;
            public int LaunchSparks = 10;
            [Header("Aşama 2-3 – Fırlatma ve hareket")]
            [Tooltip("Tam hıza çıkma süresi.")]
            public float AccelMs = 110f;
            [Tooltip("Tam hızda hücre başına süre.")]
            public float CellMs = 38f;
            public float TrailLength = 3.3f;
            public float TrailWidth = 0.5f;
            public Color TrailCore = new Color(1f, 0.98f, 0.82f, 1f);
            public Color TrailFlame = new Color(1f, 0.55f, 0.12f, 1f);
            public float Flame = 1.35f;
            [Tooltip("Uçuşta kıvılcım ve hız çizgisi sıklığı (ms başına; önce 0,05 ve 0,012).")]
            public float SparkRate = 0.12f, StreakRate = 0.03f;
            public float Stretch = 0.3f;
            [Header("Aşama 4 – Çarpışma")]
            public int JellyMin = 5, JellyMax = 9;
            public float JellySize = 0.26f;
            public float Impact = 1.4f;
            [Tooltip("Çarpışma parlaması ve halkası (hücre; önce yalnız 0,9 parlama).")]
            public float ImpactFlash = 1.35f, ImpactRing = 1.5f;
            [Header("Aşama 5-6 – Çevre ve çıkış")]
            public float NeighborWobble = 0.08f;
            public float LaneGlow = 0.85f;
            public float ExitCells = 1.2f;
            public float Shake = 0.02f;
            [Tooltip("Taşlar patladığı anda tahta sarsıntısı (önce 0,022, sabit); çift roket 1,25 kat.")]
            public float PopShake = 0.03f;
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
            /// <summary>2026-11-27 (kullanıcı: "büyük değişiklikten önceki animasyonlarımız seslerimizde güzeldi ikisini harmanla"):
            /// eski akıştan dokunulan grubun parlayıp titremesi (patlamadan önce) ve varışta tane tane, tizden pese inen jöle sesi +
            /// son taşta dolma sesi. Yeni patlama parçaları, halka ve hedefe uçan kopya aynen kalır.</summary>
            public bool OldTapCharge = true, OldStreamSound = true;
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
            public int ChipMin = 7, ChipMax = 12; // 2026-11-27 (kullanıcı: "az gruplu taşları patlatırken çok ufak şekilde hedefe gidiyor … daha görünür ve şiddetli yapalım")
            public float ChipSize = 0.28f;
            public float ChipSpeed = 4.2f;
            public float ChipLife = 0.5f;
            public int MicroCount = 12;
            public float MicroSize = 0.075f;
            public float MicroSpeed = 3.8f;
            public float MicroLife = 0.34f;
            public int StarCount = 4;
            public int StreakCount = 4;
            public float RingFrom = 0.35f, RingTo = 1.75f;
            public float RingLife = 0.32f;
            public float RingAlpha = 0.85f;
            public float Glow = 0.65f;
            [Tooltip("2026-11-26 (kullanıcı: Efektler çok pasif): roket / Altın Rulo vuruşunda parçacık çarpanı, halka, ışık ve parça hızı (dokunuş patlaması aynı kalır).")]
            public float SpecialBoost = 1.5f, SpecialRingK = 1.35f, SpecialRingAlpha = 0.75f, SpecialGlow = 0.6f, SpecialChipSpeed = 1.2f;
            [Header("4 – Grup dalgası")]
            [Tooltip("Dokunulan hücreden uzaklık halkası başına gecikme (en çok / en az).")]
            public float RingStepMs = 22f;
            public float RingStepMinMs = 12f;
            [Tooltip("Büyük grupta dalganın toplam süresi bunu geçmez.")]
            public float MaxWaveMs = 200f;
            [Tooltip("Grup boyu eşikleri: küçük ≤ x (2–4), orta ≤ y (5–8), büyük ≤ z (9–12), üstü dev (13+).")]
            public Vector3Int Tiers = new Vector3Int(4, 8, 12);
            [Tooltip("Kademeye göre parçacık çarpanı: küçük, orta, büyük, dev.")]
            public Vector4 ParticleByTier = new Vector4(1.25f, 1.35f, 1.45f, 1.6f); // 2026-11-27 (kullanıcı: "az gruplu taşları patlatırken çok ufak şekilde hedefe gidiyor … daha görünür ve şiddetli yapalım"): küçük grup da dolu
            [Tooltip("Kademeye göre titreşim: 0 yok, 1 tık, 2 hafif, 3 orta (küçük, orta, büyük, dev).")]
            public Vector4 HapticByTier = new Vector4(0f, 1f, 2f, 3f);
            [Tooltip("9+ grupta kontrollü küçük tahta sarsıntısı (dev grupta 1,5 kat).")]
            public float BigShake = 0.016f;
            [Tooltip("2026-11-27: küçük ve orta grupta da kısa tahta sarsıntısı (kullanıcı: şiddetli).")]
            public float SmallShake = 0.007f;
            [Header("5 – Hedefe uçuş")]
            [Tooltip("Mini kopyanın başlangıç boyu (taş boyu oranı).")]
            public float MiniStart = 0.95f; // 2026-11-27 (kullanıcı: "az gruplu taşları patlatırken çok ufak şekilde hedefe gidiyor … daha görünür ve şiddetli yapalım")
            public float MiniEnd = 0.62f;
            public float PopMs = 120f;
            public float PopOut = 0.55f;
            [Tooltip("Roket taşında kopya önce roketin yönünde itilir (hücre).")]
            public float RocketNudge = 0.35f;
            public float FlightMinMs = 320f, FlightMaxMs = 520f;
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
            public float TrailLife = 0.26f;
            public float TrailSize = 0.95f;
            public float TrailAlpha = 0.75f;
            [Header("6 – Hedef isabeti")]
            public float IconBounce = 1.32f;
            public float IconBounceMs = 260f;
            public float CompletePunch = 1.26f;
            public float CompleteMs = 340f;
            [Tooltip("İsabet halkası (hedef simgesi boyu oranı).")]
            public float ImpactRing = 1.9f;
            public int ImpactSparkles = 7;
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

        /// <summary>
        /// 2026-11-26 (kullanıcı: "Roket ve rulo yine tahtanın üst kısmında olsun … Belli olmuyorlar … Roketlere tıklandığında
        /// anlaşılmıyor"): tepsiden sürükleme hayaleti, hedef önizlemesi, yuvadan hedefe uçuş ve doğan taşın tepsiye uçuşu.
        /// </summary>
        [Serializable]
        public sealed class TraySettings
        {
            [Header("Kullanım uçuşu (yuva → hedef hücre)")]
            public float UseFlyMs = 180f;
            [Tooltip("Uçuş yayının yüksekliği (hücre).")]
            public float UseFlyArc = 0.9f;
            [Tooltip("Uçuşun ortasında simgenin büyümesi.")]
            public float UseFlySwell = 0.25f;
            [Tooltip("İz parçacığı (ms başına) ve varışta parlama / halka (hücre).")]
            public float UseTrailRate = 0.16f, UseArriveFlash = 2.2f, UseArriveRing = 2.4f;
            [Header("Sürükleme hayaleti ve önizleme")]
            [Tooltip("Hayaletin yuvadaki simgeye oranı ve ışık halesi.")]
            public float GhostScale = 1.7f, GhostGlow = 0.9f;
            [Tooltip("Bırakma iptalinde hayaletin yuvaya dönüşü.")]
            public float SnapBackMs = 220f;
            [Tooltip("Önizleme nabız süresi; hücre ışığı, çerçeve ve şerit saydamlığı.")]
            public float PreviewPulseMs = 560f, PreviewGlow = 1f, PreviewFrame = 1f, PreviewLane = 0.55f;
            [Tooltip("Kürek önizlemesinde sıradaki çakıla geçen nabız (ms).")]
            public float PreviewGravelStepMs = 110f;
            [Header("Doğan özel taşın tepsiye uçuşu")]
            public float CreateFlyMs = 520f;
            [Tooltip("Uçan simgenin boyu (hücre), yay (hücre) ve iz parçacığı (ms başına).")]
            public float CreateToken = 1.3f, CreateArc = 1.6f, CreateTrailRate = 0.12f;
            [Tooltip("Yuvaya varışta büyüme (ölçek), süre (ms) ve kıvılcım sayısı.")]
            public float ArrivePop = 1.55f, ArrivePopMs = 380f;
            public int ArriveSparks = 16;
            [Header("Seçili yuva")]
            [Tooltip("Seçili yuvanın yükselmesi (px) ve yukarı-aşağı salınımı (px).")]
            public float SelectLift = 8f, SelectBob = 4f;
        }

        /// <summary>2026-11-26 (kullanıcı: "temizlediği çakıl efektle yok olsun"): Kürek kazma efekti.</summary>
        [Serializable]
        public sealed class ShovelSettings
        {
            [Tooltip("Kazmada toz bulutu sayısı, parlama (hücre), toz halkası (hücre) ve yığının sekmesi.")]
            public int DigDust = 5;
            public float DigFlash = 1.5f, DigRing = 1.7f, DigBounce = 0.14f;
            [Tooltip("Her kazmada tahtanın hafif sarsıntısı.")]
            public float DigShake = 0.006f;
        }

        public TraySettings Tray = new TraySettings();
        public ShovelSettings Shovel = new ShovelSettings();
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
