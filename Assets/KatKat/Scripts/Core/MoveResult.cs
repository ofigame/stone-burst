using System.Collections.Generic;

namespace KatKat.Core
{
    /// <summary>
    /// Komut sonucu ve sunumun oynatacağı sıralı olay planı. Mantık bu planı üretirken bütün dalgaları
    /// çözmüştür; sunum yalnızca oynatır, kural çalıştırmaz.
    /// </summary>
    public sealed class MoveResult
    {
        public bool Accepted;
        public RejectReason Reason;
        public Command Command;
        /// <summary>Doğrudan dokunulan grup (Tap). Geçersiz küçük grup için de doldurulur (geri bildirim için).</summary>
        public readonly List<Coord> TappedGroup = new List<Coord>();
        public readonly List<PeelWave> Waves = new List<PeelWave>();
        public bool RollAwarded;
        public bool RollConsumed;
        /// <summary>Döndürme komutu: döndürülen hücre (Rotate değilse anlamsız). Dalga yoktur.</summary>
        public Coord Rotated;
        /// <summary>Döndürme sonrası katmanlar (alttan üste); sunum animasyonun ortasında yığını bununla çizer.</summary>
        public readonly List<LayerColor> RotatedLayers = new List<LayerColor>();
        public int MovesLeftAfter;
        public GameStatus StatusAfter;
        /// <summary>v5: bu hamlede doğan özel taş (yoksa None) ve yeri; ilk dalgadan hemen sonra belirir.</summary>
        public SpecialKind SpecialCreated;
        public Coord SpecialCreatedAt;
        public LayerColor SpecialCreatedColor;
        /// <summary>v6 tepsi: doğan özel taş tepsiye girdiyse true (tepsi doluysa false, yalnız puan).</summary>
        public bool SpecialToTray;
        /// <summary>v6 UseSpecial: tepsiden kullanılan özel taş (yoksa None) ve hedef hücre.</summary>
        public SpecialKind UsedSpecial;
        public Coord UsedAt;
        /// <summary>M12: boyanan hücre ve eski/yeni üst renk (Paint değilse anlamsız). Dalga yoktur.</summary>
        public Coord Painted;
        public LayerColor PaintFrom, PaintTo;
        /// <summary>Bu hamlenin toplam puanı (özel taş oluşma puanı dahil).</summary>
        public int ScoreGained;

        public int TotalPeeled
        {
            get
            {
                int n = 0;
                foreach (var w in Waves) n += w.Cells.Count;
                return n;
            }
        }

        public static MoveResult Reject(Command cmd, RejectReason reason) => new MoveResult { Accepted = false, Reason = reason, Command = cmd };
    }
}
