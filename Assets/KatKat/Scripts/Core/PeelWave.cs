using System.Collections.Generic;

namespace KatKat.Core
{
    /// <summary>Bir soyulma dalgası: eşzamanlı soyulan hücreler (y, sonra x sırasında), ataş hasarları ve dalga sonrası hedefler.</summary>
    public sealed class PeelWave
    {
        public WaveKind Kind;
        public int Index;
        public readonly List<CellPeel> Cells = new List<CellPeel>();
        /// <summary>Bu dalganın verdiği ataş hasarları (y, sonra x sırasında); ataş başına en çok bir.</summary>
        public readonly List<ClipHit> ClipHits = new List<ClipHit>();
        /// <summary>Bu dalganın verdiği buz hasarları; ataşla aynı kural, buz başına en çok bir.</summary>
        public readonly List<ClipHit> IceHits = new List<ClipHit>();
        /// <summary>Renk kilidi sayaç düşüşleri (dalgada kaldırılan ilgili renk katmanı kadar, sıfırda durur).</summary>
        public readonly List<LockHit> LockHits = new List<LockHit>();
        /// <summary>Bu dalgada açılan zarflar; her biri bir zarf toplar (y, sonra x sırasında).</summary>
        public readonly List<Coord> EnvelopeOpens = new List<Coord>();
        /// <summary>Bu dalga bittiğinde her hedefin ilerlemesi (LevelState.Goals sırasıyla).</summary>
        public int[] GoalProgressAfter;
        /// <summary>Özel taş dalgası: ateşlenen taş, yeri ve (birleşimse) ortağı. Kind == Special değilse None.</summary>
        public SpecialKind Fired;
        public Coord FiredAt;
        public SpecialKind ComboWith;
        public Coord ComboAt;
        /// <summary>Etki alanında tetiklenip sıraya giren diğer özel taşlar.</summary>
        public readonly List<Coord> Triggered = new List<Coord>();
        /// <summary>Örtüye doğrudan vuruşlar (roket/gökkuşağı örtülü yığından geçti).</summary>
        public readonly List<Coord> DirectHits = new List<Coord>();
        /// <summary>M09: süngere yeni dolan yuvalar (her biri: hücre, dolu maske, gereken maske) ve açılan süngerler.</summary>
        public readonly List<SpongeHit> SpongeHits = new List<SpongeHit>();
        public readonly List<Coord> SpongeOpens = new List<Coord>();
        /// <summary>M10: bağlı ataş grubunun her üyesi için sayaç (grup başına dalgada bir hasar).</summary>
        public readonly List<ClipHit> LinkHits = new List<ClipHit>();
        /// <summary>M11: basılan düğmeler ve etkinleşen geçit hücreleri.</summary>
        public readonly List<Coord> ButtonPresses = new List<Coord>();
        public readonly List<Coord> GateOpens = new List<Coord>();
        /// <summary>vr1 P3: bu dalgada zincir mührü açıldı.</summary>
        public bool SealOpened;
        /// <summary>vr1 P3: bu dalgada aşama tamamlandı; dalga sonrası etkin aşama.</summary>
        public bool StageAdvanced;
        public int StageAfter;
        /// <summary>Bu dalganın kazandırdığı puan.</summary>
        public int Score;

        public int StarsCollected
        {
            get
            {
                int n = 0;
                foreach (var c in Cells) if (c.StarCollected) n++;
                return n;
            }
        }
    }
}
