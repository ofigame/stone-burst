using System.Collections.Generic;

namespace KatKat.Core
{
    /// <summary>
    /// Yukarıdan dolma (kullanıcı 2026-11-24: "torba bölümlerinde katlar boşaldıkça üstten alta dolsun. Belli sayıda yeni taşlar da en
    /// üstten oyuna girsin. Belli sayıda girsin ve ondan sonra girmesin. Yine üstten alta dolsun ama yeni taş girmesin").
    /// <para>Her komutun sonunda boşalan hücrelere sütundaki üst yığınlar kayar; sütunun en üst bölümünde kalan boşluklara, hak
    /// bitene dek yeni yığın girer (<see cref="Left"/>). Renkler ve kat sayısı durumdaki tohumla belirlenimli (geri al ve ipucu
    /// aynı sonucu görür). Kural <see cref="RulesEngine"/>'de.</para>
    /// </summary>
    public sealed class RefillRun
    {
        /// <summary>Girebilecek yeni yığın sayısı (kalan).</summary>
        public int Left;
        public int MinLayers = 2, MaxLayers = 3;
        public readonly List<LayerColor> Colors = new List<LayerColor>();
        public uint Rng;

        public int Next(int n)
        {
            Rng = Rng * 1664525u + 1013904223u;
            return n <= 1 ? 0 : (int)((Rng >> 8) % (uint)n);
        }

        public RefillRun Clone()
        {
            var r = new RefillRun { Left = Left, MinLayers = MinLayers, MaxLayers = MaxLayers, Rng = Rng };
            r.Colors.AddRange(Colors);
            return r;
        }

        public bool ContentEquals(RefillRun o)
        {
            if (o == null || Left != o.Left || MinLayers != o.MinLayers || MaxLayers != o.MaxLayers || Rng != o.Rng || Colors.Count != o.Colors.Count) return false;
            for (int i = 0; i < Colors.Count; i++) if (Colors[i] != o.Colors[i]) return false;
            return true;
        }
    }

    /// <summary>Hamle sonu kayma: yığın From'dan To'ya kaydı; Spawned ise tahtanın üstünden (From tahta dışı) yeni yığın girdi.</summary>
    public struct FallMove
    {
        public Coord From, To;
        public bool Spawned;
    }
}
