using System.Collections.Generic;

namespace KatKat.Levels
{
    /// <summary>
    /// Seviye dosyasının alan tabanlı karşılığı. schemaVersion 1: bölüm 1–60 biçimi (kapsam bölüm numarasından).
    /// schemaVersion 2: hücre örtüsü (overlay) ve açık yetenek listesi (capabilities). Oyun sırasında değiştirilmez.
    /// </summary>
    public sealed class LevelData
    {
        public int SchemaVersion;
        /// <summary>schemaVersion 2: bölümün kullandığı mekanikler ("clip", "star", "ice", "colorLock").</summary>
        public readonly List<string> Capabilities = new List<string>();
        public string Id;
        /// <summary>İsteğe bağlı oyuncuya gösterilen ad; yoksa sıra numarası kullanılır.</summary>
        public string Title;
        /// <summary>İsteğe bağlı kısa öğretici metni.</summary>
        public string Tutorial;
        public int Width;
        public int Height;
        public int MoveLimit;
        /// <summary>2026-11-21 süreli bölüm (kullanıcı: "süreli bölümlerde hamle sınırsız olsun süre olsun sadece"): saniye; 0 → süresiz.</summary>
        public int TimeLimit;
        public bool RollEnabled;
        public bool StartingRoll;
        /// <summary>v5 §5: 2 ve 3 yıldız puan eşikleri; yoksa null.</summary>
        public int[] StarThresholds;
        /// <summary>M12: başlangıç boya damlası sayısı (0 = yok).</summary>
        public int Paint;
        /// <summary>v5 §2: "specials" yeteneği — büyük gruplar özel taş doğurur.</summary>
        public bool SpecialsEnabled => Capabilities.Contains("specials");
        /// <summary>v12 deneme: bölüm türü (ör. "exact", "aha", "reverse"…); kural kartı başlığı. Boşsa normal bölüm.</summary>
        public string TrialType;
        /// <summary>v12 deneme: oyuncuya gösterilen kurallar ("exact", "protect", "noSpecials", "scarce"). v14: "chainOnly" kaldırıldı, doğrulayıcı reddeder.</summary>
        public readonly List<string> Rules = new List<string>();
        public readonly List<GoalData> Goals = new List<GoalData>();
        public readonly List<CellData> Cells = new List<CellData>();
        public readonly List<StepData> Solution = new List<StepData>();
        /// <summary>vr1 P3 zincir mührü: {wave, gate}; 0 = yok. v14 (kullanıcı 2026-11-16: zincir tamamen kaldırılsın): motor
        /// kullanmaz; eski veri çökmesin diye okunur, doğrulayıcı "desteklenmiyor" der.</summary>
        public int SealWave, SealGate;
        /// <summary>Konsept pilotu: hamle başı tehdit (yoksa null; ana katalogda kullanılmaz).</summary>
        public HazardData Hazard;
        /// <summary>2026-11-23 kova kipi: sırayla gelen kova siparişleri (renk kimliği, miktar). Boşsa kova yok.</summary>
        public readonly List<KeyValuePair<string, int>> Buckets = new List<KeyValuePair<string, int>>();
        /// <summary>2026-11-24 yukarıdan dolma: girecek yeni yığın sayısı (0 → dolma yok) ve yeni yığının kat aralığı.</summary>
        public int RefillCount, RefillMinLayers = 2, RefillMaxLayers = 3;
        /// <summary>2026-11-26 su kovası kapasitesi ("waterCap"; 0 → kovasız).</summary>
        public int WaterCap;
        public bool HasRefill;
    }
}
