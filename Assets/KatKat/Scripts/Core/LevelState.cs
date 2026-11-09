using System.Collections.Generic;

namespace KatKat.Core
{
    /// <summary>Bir bölümün bütün mantıksal durumu. Sunum, kayıt ve zaman bilgisi içermez.</summary>
    public sealed class LevelState
    {
        public BoardState Board;
        public int MovesLeft;
        public readonly List<GoalState> Goals = new List<GoalState>();
        public bool RollEnabled;
        public bool RollSlotFull;
        /// <summary>v5 §2: büyük gruplar özel taş (rulo roketi, gökkuşağı) doğurur.</summary>
        public bool SpecialsEnabled;
        /// <summary>v5 §5: toplam puan (bitiş kutlaması dahil).</summary>
        public int Score;
        /// <summary>2 ve 3 yıldız puan eşikleri; yoksa null (kazanmak 3 yıldız).</summary>
        public int[] StarThresholds;
        /// <summary>M12: kalan boya damlası (bölüm başında verilir; biriktirme yok).</summary>
        public int PaintLeft;
        /// <summary>
        /// v6 özel taş tepsisi (kullanıcı: "taşların üzerinde olmasın, oyuncu istediği zaman kullansın"): büyük
        /// gruplarda doğan roket/Altın Rulo buraya girer; <see cref="CommandType.UseSpecial"/> ile hamle harcamadan
        /// seçilen hücrede ateşlenir. En çok <see cref="TrayCapacity"/>.
        /// </summary>
        public readonly List<SpecialKind> Tray = new List<SpecialKind>();
        public const int TrayCapacity = 4;
        public GameStatus Status;
        /// <summary>v12 deneme: bir kristal yığından katman soyuldu (bölüm kaybedilir).</summary>
        public bool ProtectedBroken;

        public bool IsTerminal => Status != GameStatus.Idle;
        public bool AllGoalsComplete
        {
            get
            {
                foreach (var g in Goals) if (!g.IsComplete) return false;
                return true;
            }
        }

        public int Stars => Scoring.Stars(Status == GameStatus.Won, Score, StarThresholds);

        public LevelState Clone()
        {
            var s = new LevelState
            {
                Board = Board.Clone(),
                MovesLeft = MovesLeft,
                RollEnabled = RollEnabled,
                RollSlotFull = RollSlotFull,
                SpecialsEnabled = SpecialsEnabled,
                Score = Score,
                StarThresholds = StarThresholds,
                PaintLeft = PaintLeft,
                Status = Status,
                ProtectedBroken = ProtectedBroken,
            };
            foreach (var g in Goals) s.Goals.Add(g.Clone());
            s.Tray.AddRange(Tray);
            return s;
        }

        public bool ContentEquals(LevelState o)
        {
            if (MovesLeft != o.MovesLeft || RollEnabled != o.RollEnabled || RollSlotFull != o.RollSlotFull || Status != o.Status
                || SpecialsEnabled != o.SpecialsEnabled || Score != o.Score || PaintLeft != o.PaintLeft || ProtectedBroken != o.ProtectedBroken) return false;
            if (!Board.ContentEquals(o.Board) || Goals.Count != o.Goals.Count || Tray.Count != o.Tray.Count) return false;
            for (int i = 0; i < Tray.Count; i++) if (Tray[i] != o.Tray[i]) return false;
            for (int i = 0; i < Goals.Count; i++) if (!Goals[i].ContentEquals(o.Goals[i])) return false;
            return true;
        }
    }
}
