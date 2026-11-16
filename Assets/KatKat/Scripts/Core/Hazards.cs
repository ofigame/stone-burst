using System;
using System.Collections.Generic;
using System.Text;

namespace KatKat.Core
{
    /// <summary>
    /// Konsept pilotu (kullanıcı, 2026-11-15: "Mantık + yarış, belli konseptler olacak"; docs/konsept/RAPOR.md). Her konsept
    /// bir tehdittir: oyuncunun harcadığı her hamleden sonra bir adım ilerler (gerçek zaman yok; düşünürken saat işlemez).
    /// Tehdit belirlenimlidir ve önceden görünür; çözücü ve ipucu aynı motorla doğrular. Tepsi kullanımı hamle harcamadığı
    /// için tehdidi ilerletmez. Bölümde en çok bir konsept (pilot).
    /// </summary>
    public enum HazardKind
    {
        None = 0,
        /// <summary>Çürüyen Zemin: alt sıra her K hamlede çöker, üstündeki taşlar düşer (kaybolur). Alt sırada boşaltılan her yığın çöküşü 1 hamle erteler.</summary>
        Rot = 1,
        /// <summary>Fitil: sayaçlı yığınlar; sayaç 0'a inmeden yığın tamamen boşaltılmazsa patlar (kayıp).</summary>
        Fuse = 2,
        /// <summary>Dönen Hedef: yalnız etkin renk sayılır; etkin renk K hamlede bir sırayla değişir.</summary>
        Cycle = 3,
        /// <summary>Kuzgun: yol boyunca her hamlede bir adım ilerler, konduğu yığının üst taşını çalar; önündeki yığın boşsa bekler. Yuvaya varırsa kayıp.</summary>
        Raven = 4,
        /// <summary>Yağmur: K hamlede bir, önceden gösterilen yığınlara taş düşer; bir yığın sınırı aşarsa kayıp.</summary>
        Rain = 5,
        /// <summary>Gölge Rakip: her hamlede bir adım; 5+ grup patlatılan hamlede rakip ilerlemez ve bir adım geri düşer. Rakip bitişe varırsa kayıp.</summary>
        Rival = 6,
        /// <summary>Yayılan Yosun: K hamlede bir her yosun komşu yığına yayılır (yosunlu yığın seçilemez); yanında soyma yosunu temizler. Sınıra ulaşırsa kayıp.</summary>
        Moss = 7,
        /// <summary>Balonlar: K hamlede bir sıra yükselir; altındaki yığın boşalınca patlar (toplanır). Tahtanın üstünden çıkarsa kaçar (kayıp).</summary>
        Balloon = 8,
        /// <summary>Sipariş Kuyruğu: aşamalı hedeflerin her biri kendi hamle süresiyle; süre biterse sipariş yanar (kayıp).</summary>
        Orders = 9,
        /// <summary>Kombo Ateşi: 5+ taşlık grup patlatan dokunuş ateşi doldurur, küçük grup ya da başka hamle bir azaltır; sönerse kayıp.
        /// v14 (kullanıcı 2026-11-16: zincir tamamen kaldırılsın): eskiden zincir dalgasıyla beslenirdi.</summary>
        Fire = 10,
    }

    public enum HazardLoss
    {
        None = 0,
        /// <summary>Hedef rengin tahtada kalanı hedefe yetmiyor (düşen/çalınan taşlar).</summary>
        Unreachable = 1,
        Fuse = 2,
        Raven = 3,
        Overflow = 4,
        Rival = 5,
        Moss = 6,
        Balloon = 7,
        Order = 8,
        Fire = 9,
    }

    /// <summary>Bu hamlenin tehdit olayı (sunum bildirim ve vurgu için; mantık bunu okumaz).</summary>
    public enum HazardNote
    {
        None = 0,
        RotCrumble, RotDelayed, FuseDefused, CycleSwitch, RavenStep, RavenBlocked, RainFell, RivalBack, MossSpread, BalloonPopped, OrderNext, FireFed,
    }

    /// <summary>Konseptin oyun içi durumu. Paylaşılan (değişmeyen) diziler kopyalanmaz; hücre başına dizi kopyalanır.</summary>
    public sealed class HazardState
    {
        public HazardKind Kind;
        /// <summary>Dönem (K): Çürüme, Dönen Hedef, Yağmur, Yosun, Balon.</summary>
        public int Every = 1;
        /// <summary>Sonraki olaya kalan hamle.</summary>
        public int Counter;
        /// <summary>Çürüme: sıradaki çökecek satır (y); -1 = bitti.</summary>
        public int Row = -1;
        /// <summary>Çürüme: son adımda çökecek satırdaki boş etkin hücre sayısı (yeni boşalanlar erteler).</summary>
        public int Cleared;
        /// <summary>Dönen Hedef: etkin renk sırası; Kuzgun: yoldaki konum; Yağmur: sıradaki sağanak.</summary>
        public int Index;
        /// <summary>Rakip: konum; Ateş: kalan; Sipariş: etkin siparişin kalan hamlesi.</summary>
        public int Value;
        /// <summary>Rakip: bitiş; Ateş: dolu; Yağmur: yığın sınırı; Yosun: sınır.</summary>
        public int Max;
        /// <summary>Sipariş: son adımda görülen aşama.</summary>
        public int Stage;
        public int Fallen, Stolen, Popped;
        public LayerColor[] Colors;
        public int[] Path;
        /// <summary>Yağmur: sağanaklar; her eleman hücre*8 + renk.</summary>
        public int[][] Volleys;
        public int[] Orders;
        public int[] Fuse;
        public bool[] Balloon;
        public bool Lost;
        public HazardLoss Loss;

        public LayerColor ActiveColor => Colors != null && Colors.Length > 0 ? Colors[Index % Colors.Length] : default;
        public LayerColor NextColor => Colors != null && Colors.Length > 0 ? Colors[(Index + 1) % Colors.Length] : default;
        public int RavenCell => Path != null && Path.Length > 0 ? Path[Math.Min(Index, Path.Length - 1)] : -1;
        public int[] NextVolley => Volleys != null && Volleys.Length > 0 ? Volleys[Index % Volleys.Length] : null;

        public HazardState Clone()
        {
            var h = (HazardState)MemberwiseClone();
            if (Fuse != null) h.Fuse = (int[])Fuse.Clone();
            if (Balloon != null) h.Balloon = (bool[])Balloon.Clone();
            return h;
        }

        public bool ContentEquals(HazardState o)
        {
            if (o == null || Kind != o.Kind || Counter != o.Counter || Row != o.Row || Cleared != o.Cleared || Index != o.Index || Value != o.Value
                || Stage != o.Stage || Lost != o.Lost || Loss != o.Loss || Fallen != o.Fallen || Stolen != o.Stolen || Popped != o.Popped) return false;
            if (Fuse != null) for (int i = 0; i < Fuse.Length; i++) if (Fuse[i] != o.Fuse[i]) return false;
            if (Balloon != null) for (int i = 0; i < Balloon.Length; i++) if (Balloon[i] != o.Balloon[i]) return false;
            return true;
        }

        /// <summary>Çözücü durum anahtarı parçası (puan ve sayaçlar dahil; istatistik sayaçları hariç).</summary>
        public void AppendKey(StringBuilder sb)
        {
            sb.Append('h').Append((int)Kind).Append(':').Append(Counter).Append(',').Append(Row).Append(',').Append(Cleared).Append(',')
              .Append(Index).Append(',').Append(Value).Append(',').Append(Stage).Append(Lost ? 'L' : '-');
            if (Fuse != null) { sb.Append('f'); foreach (var f in Fuse) if (f > 0) sb.Append(f); else sb.Append('.'); }
            if (Balloon != null) { sb.Append('b'); foreach (var b in Balloon) sb.Append(b ? '1' : '0'); }
            sb.Append('|');
        }
    }

    /// <summary>Konsept kuralları (RulesEngine hamle sonunda çağırır; deterministik).</summary>
    public static class HazardRules
    {
        /// <summary>Kombo Ateşi'ni dolduran en küçük dokunuş grubu (v14: zincir yerine).</summary>
        public const int FireGroupSize = 5;

        /// <summary>Hamle harcayan komuttan sonra, hedefler tamamlanmadıysa: tehdit bir adım.</summary>
        public static void Tick(LevelState s, MoveResult r)
        {
            var h = s.Hazard;
            if (h == null || h.Lost) return;
            var b = s.Board;
            switch (h.Kind)
            {
                case HazardKind.Rot: TickRot(s, h, b, r); break;
                case HazardKind.Fuse:
                    for (int i = 0; i < b.Count; i++)
                    {
                        if (h.Fuse[i] <= 0) continue;
                        if (!b[i].Active || b[i].Layers.Count == 0) { h.Fuse[i] = 0; Note(r, HazardNote.FuseDefused, b, i); continue; }
                        if (--h.Fuse[i] <= 0) { Lose(h, HazardLoss.Fuse); r.HazardCells.Add(new Coord(b.X(i), b.Y(i))); }
                    }
                    break;
                case HazardKind.Cycle:
                    if (--h.Counter <= 0) { h.Index = (h.Index + 1) % h.Colors.Length; h.Counter = h.Every; r.HazardNote = HazardNote.CycleSwitch; }
                    break;
                case HazardKind.Raven:
                    if (h.Index < h.Path.Length - 1)
                    {
                        int next = h.Path[h.Index + 1];
                        var c = b[next];
                        if (c.Active && c.Layers.Count > 0 && !c.HasCover && !c.HasSpecial)
                        {
                            h.Index++;
                            c.Layers.RemoveAt(c.Layers.Count - 1);
                            h.Stolen++;
                            Note(r, HazardNote.RavenStep, b, next);
                            if (h.Index >= h.Path.Length - 1) Lose(h, HazardLoss.Raven);
                        }
                        else Note(r, HazardNote.RavenBlocked, b, h.Path[h.Index]);
                    }
                    break;
                case HazardKind.Rain:
                    if (--h.Counter <= 0)
                    {
                        foreach (var code in h.Volleys[h.Index % h.Volleys.Length])
                        {
                            int i = code >> 3;
                            var c = b[i];
                            if (!c.Active || c.HasCover || c.HasSpecial) continue;
                            c.Layers.Add((LayerColor)(code & 7));
                            Note(r, HazardNote.RainFell, b, i);
                            if (c.Layers.Count > h.Max) Lose(h, HazardLoss.Overflow);
                        }
                        h.Index++;
                        h.Counter = h.Every;
                    }
                    break;
                case HazardKind.Rival:
                    if (r.TappedGroup.Count >= RulesEngine.RocketGroupSize && r.Command.Type == CommandType.Tap) { h.Value = Math.Max(0, h.Value - 1); r.HazardNote = HazardNote.RivalBack; }
                    else if (++h.Value >= h.Max) Lose(h, HazardLoss.Rival);
                    break;
                case HazardKind.Moss: TickMoss(h, b, r); break;
                case HazardKind.Balloon: TickBalloon(s, h, b, r); break;
                case HazardKind.Orders:
                    if (s.ActiveStage != h.Stage)
                    {
                        h.Stage = s.ActiveStage;
                        h.Value = h.Orders[Math.Min(h.Stage, h.Orders.Length - 1)];
                        r.HazardNote = HazardNote.OrderNext;
                    }
                    else if (--h.Value <= 0) Lose(h, HazardLoss.Order);
                    break;
                case HazardKind.Fire:
                    // v14 (kullanıcı 2026-11-16: zincir tamamen kaldırılsın): ateşi zincir değil 5+ taşlık dokunuş grubu besler.
                    if (r.Command.Type == CommandType.Tap && r.TappedGroup.Count >= FireGroupSize) { h.Value = h.Max; r.HazardNote = HazardNote.FireFed; }
                    else if (--h.Value <= 0) Lose(h, HazardLoss.Fire);
                    break;
            }
            if (!h.Lost && h.Kind != HazardKind.Rain && !s.AllGoalsComplete && Unreachable(s)) Lose(h, HazardLoss.Unreachable);
        }

        private static void TickRot(LevelState s, HazardState h, BoardState b, MoveResult r)
        {
            if (h.Row < 0) return;
            int empty = EmptyInRow(b, h.Row);
            if (empty > h.Cleared) { h.Counter = Math.Min(h.Every, h.Counter + (empty - h.Cleared)); r.HazardNote = HazardNote.RotDelayed; }
            h.Cleared = empty;
            if (--h.Counter > 0) return;
            for (int x = 0; x < b.Width; x++)
            {
                var c = b[x, h.Row];
                if (!c.Active) continue;
                h.Fallen += c.Layers.Count;
                c.Layers.Clear();
                c.Active = false;
                c.Special = SpecialKind.None;
                c.ClipHitsRemaining = c.IceHitsRemaining = c.LockRemaining = 0;
                c.HasEnvelope = c.HasBottomStar = false;
                c.Moss = false;
                r.HazardCells.Add(new Coord(x, h.Row));
            }
            r.HazardNote = HazardNote.RotCrumble;
            h.Row = NextRotRow(b, h.Row - 1);
            h.Counter = h.Every;
            h.Cleared = h.Row >= 0 ? EmptyInRow(b, h.Row) : 0;
        }

        /// <summary>Bu satırdan yukarı doğru içinde etkin hücre olan ilk satır.</summary>
        public static int NextRotRow(BoardState b, int from)
        {
            for (int y = from; y >= 0; y--)
                for (int x = 0; x < b.Width; x++)
                    if (b[x, y].Active) return y;
            return -1;
        }

        public static int EmptyInRow(BoardState b, int y)
        {
            int n = 0;
            for (int x = 0; x < b.Width; x++) if (b[x, y].Active && b[x, y].Layers.Count == 0) n++;
            return n;
        }

        private static readonly int[] Dx = { 0, 1, 0, -1 }, Dy = { -1, 0, 1, 0 };

        private static void TickMoss(HazardState h, BoardState b, MoveResult r)
        {
            if (--h.Counter <= 0)
            {
                h.Counter = h.Every;
                var src = new List<int>();
                for (int i = 0; i < b.Count; i++) if (b[i].Moss) src.Add(i);
                foreach (var i in src)
                {
                    int x = b.X(i), y = b.Y(i);
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + Dx[d], ny = y + Dy[d];
                        if (!b.InBounds(nx, ny)) continue;
                        var c = b[nx, ny];
                        if (!c.Active || c.Layers.Count == 0 || c.HasCover || c.HasSpecial || c.Protected) continue;
                        c.Moss = true;
                        Note(r, HazardNote.MossSpread, b, b.Index(nx, ny));
                        break;
                    }
                }
            }
            if (MossCount(b) >= h.Max) Lose(h, HazardLoss.Moss);
        }

        public static int MossCount(BoardState b)
        {
            int n = 0;
            for (int i = 0; i < b.Count; i++) if (b[i].Moss) n++;
            return n;
        }

        private static void TickBalloon(LevelState s, HazardState h, BoardState b, MoveResult r)
        {
            PopEmpty(s, h, b, r);
            if (s.AllGoalsComplete) return;
            if (--h.Counter > 0) return;
            h.Counter = h.Every;
            for (int y = 0; y < b.Height; y++)
                for (int x = 0; x < b.Width; x++)
                {
                    int i = b.Index(x, y);
                    if (!h.Balloon[i]) continue;
                    h.Balloon[i] = false;
                    int ny = y - 1;
                    if (ny < 0 || !b[x, ny].Active) { Lose(h, HazardLoss.Balloon); r.HazardCells.Add(new Coord(x, y)); continue; }
                    h.Balloon[b.Index(x, ny)] = true;
                }
            PopEmpty(s, h, b, r);
        }

        private static void PopEmpty(LevelState s, HazardState h, BoardState b, MoveResult r)
        {
            for (int i = 0; i < b.Count; i++)
            {
                if (!h.Balloon[i] || (b[i].Active && b[i].Layers.Count > 0)) continue;
                h.Balloon[i] = false;
                h.Popped++;
                foreach (var g in s.Goals) if (g.Type == GoalType.CollectBalloons && g.Stage == s.ActiveStage) g.Progress++;
                Note(r, HazardNote.BalloonPopped, b, i);
            }
        }

        /// <summary>Renk hedeflerinden biri tahtada kalan (geçit dahil) taşlarla artık tamamlanamıyor mu.</summary>
        public static bool Unreachable(LevelState s)
        {
            var b = s.Board;
            foreach (var g in s.Goals)
            {
                if (g.IsComplete || g.Type != GoalType.CollectColor) continue;
                int avail = 0;
                for (int i = 0; i < b.Count; i++)
                {
                    var c = b[i];
                    if (c.Active) foreach (var l in c.Layers) if (l == g.Color) avail++;
                    if (!c.Active && c.GateId > 0) foreach (var l in c.GateLayers) if (l == g.Color) avail++;
                }
                if (g.Progress + avail < g.Amount) return true;
            }
            return false;
        }

        /// <summary>Dönen Hedef: bu renkteki soyulma hedefe sayılır mı.</summary>
        public static bool Counts(LevelState s, LayerColor color) =>
            s.Hazard == null || s.Hazard.Kind != HazardKind.Cycle || s.Hazard.ActiveColor == color;

        private static void Lose(HazardState h, HazardLoss why)
        {
            if (h.Lost) return;
            h.Lost = true;
            h.Loss = why;
        }

        private static void Note(MoveResult r, HazardNote note, BoardState b, int i)
        {
            r.HazardNote = note;
            r.HazardCells.Add(new Coord(b.X(i), b.Y(i)));
        }
    }
}
