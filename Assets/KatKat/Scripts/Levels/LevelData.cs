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
        /// <summary>v12 deneme: oyuncuya gösterilen kurallar ("exact", "protect", "chainOnly", "noSpecials", "scarce").</summary>
        public readonly List<string> Rules = new List<string>();
        public readonly List<GoalData> Goals = new List<GoalData>();
        public readonly List<CellData> Cells = new List<CellData>();
        public readonly List<StepData> Solution = new List<StepData>();
        /// <summary>vr1 P3 zincir mührü: {wave, gate}; 0 = yok. Ana katalogda kullanılmaz (pilot içerik).</summary>
        public int SealWave, SealGate;
    }
}
