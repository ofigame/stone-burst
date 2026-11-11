using System.Collections.Generic;

namespace KatKat.Core
{
    /// <summary>
    /// Tek hücre. Layers alttan üste tutulur; son eleman üst renktir.
    /// Bir hücrede en çok bir fiziksel örtü bulunur (ataş, buz, renk kilidi); örtülü yığın seçilemez ve
    /// rulo onu atlar. Örtü açılması katman kaldırmaz ve hedef saymaz (Premium-v2 rehberi §9).
    /// </summary>
    public sealed class CellState
    {
        public bool Active;
        public readonly List<LayerColor> Layers = new List<LayerColor>(4);
        /// <summary>0 serbest; 1-2 ataşlı. Ataşlı yığın seçilemez; komşu soyulma dalgası başına bir hasar alır.</summary>
        public int ClipHitsRemaining;
        /// <summary>0 serbest; 1-2 buzlu. Ataşla aynı hasar kuralı: kenar komşusu soyulan dalga başına bir hasar, renk önemsiz.</summary>
        public int IceHitsRemaining;
        /// <summary>Renk kilidi: tahtanın herhangi bir yerinden bu renkte kaldırılan her gerçek katman sayacı bir azaltır.</summary>
        public LayerColor LockColor;
        /// <summary>0 kilitsiz; pozitifse kilit açılana kadar kalan katman sayısı.</summary>
        public int LockRemaining;
        /// <summary>Mühürlü zarf (M07): kenar komşusundan mühür renginde katman kaldırılan dalgada açılır, bir zarf toplanır.</summary>
        public bool HasEnvelope;
        public LayerColor EnvelopeColor;
        /// <summary>Son katman kaldırılınca bir kez yıldız verir; toplanınca false olur.</summary>
        public bool HasBottomStar;
        /// <summary>Döner taban (M08): kalıcıdır; döndürme komutu eski üstü en alta indirir.</summary>
        public bool HasRotor;
        /// <summary>Üstte duran özel taş (v5 §2). Varken hücre renk grubuna katılmaz; dokunulunca ateşlenir.</summary>
        public SpecialKind Special;
        /// <summary>Gökkuşağının rengi (yalnızca Rainbow için anlamlı).</summary>
        public LayerColor SpecialColor;

        public bool HasSpecial => Special != SpecialKind.None;

        /// <summary>M09 sünger: gerekli renkler (bit = 1 &lt;&lt; renk) ve dolan yuvalar. Need != 0 iken örtüdür.</summary>
        public int SpongeNeed, SpongeHave;
        /// <summary>M10 bağlı ataş: aynı LinkId'li üyeler ortak sayaç taşır (motor hepsini birlikte günceller).</summary>
        public int LinkId, LinkHits;
        /// <summary>M11: bu yığının dibinde düğme; yığın boşalınca bu geçidi bir kez açar.</summary>
        public int ButtonGate;
        /// <summary>v12 deneme "koruma": kristal yığın; bir katmanı bile soyulursa bölüm kaybedilir (zincir dahil).</summary>
        public bool Protected;
        /// <summary>M11 geçit hücresi: Active=false iken bekler; açılınca GateLayers ile etkinleşir.</summary>
        public int GateId;
        public readonly List<LayerColor> GateLayers = new List<LayerColor>(4);
        /// <summary>vr1 P3 iki alan: hücrenin alanı (0/1). Roket ve Altın Rulo yalnız hedef hücrenin alanında işler; alanlar
        /// boş sütunla ayrılır (gruplar ve zincir zaten kenar komşuluğuyla sınırlı).</summary>
        public int Area;

        public static int Bit(LayerColor c) => 1 << (int)c;

        /// <summary>Döndürülebilir: tabanlı, etkin, örtüsüz, özel taşsız ve en az iki katman.</summary>
        public bool IsRotatable => HasRotor && Active && !HasCover && !HasSpecial && Layers.Count >= 2;

        public bool IsEmpty => Layers.Count == 0;
        public bool HasCover => ClipHitsRemaining > 0 || IceHitsRemaining > 0 || LockRemaining > 0 || HasEnvelope || SpongeNeed != 0 || LinkHits > 0;
        public bool IsSelectable => Active && Layers.Count > 0 && !HasCover && !HasSpecial;
        public LayerColor Top => Layers[Layers.Count - 1];

        public CellState Clone()
        {
            var c = new CellState
            {
                Active = Active, ClipHitsRemaining = ClipHitsRemaining, IceHitsRemaining = IceHitsRemaining,
                LockColor = LockColor, LockRemaining = LockRemaining, HasBottomStar = HasBottomStar,
                HasEnvelope = HasEnvelope, EnvelopeColor = EnvelopeColor, HasRotor = HasRotor,
                Special = Special, SpecialColor = SpecialColor,
                SpongeNeed = SpongeNeed, SpongeHave = SpongeHave, LinkId = LinkId, LinkHits = LinkHits,
                ButtonGate = ButtonGate, GateId = GateId, Protected = Protected, Area = Area,
            };
            c.Layers.AddRange(Layers);
            c.GateLayers.AddRange(GateLayers);
            return c;
        }

        public bool ContentEquals(CellState o)
        {
            if (Active != o.Active || ClipHitsRemaining != o.ClipHitsRemaining || IceHitsRemaining != o.IceHitsRemaining
                || LockRemaining != o.LockRemaining || (LockRemaining > 0 && LockColor != o.LockColor) || HasBottomStar != o.HasBottomStar
                || HasEnvelope != o.HasEnvelope || (HasEnvelope && EnvelopeColor != o.EnvelopeColor) || HasRotor != o.HasRotor
                || Special != o.Special || (Special == SpecialKind.Rainbow && SpecialColor != o.SpecialColor)
                || SpongeNeed != o.SpongeNeed || SpongeHave != o.SpongeHave || LinkId != o.LinkId || LinkHits != o.LinkHits
                || ButtonGate != o.ButtonGate || GateId != o.GateId || Protected != o.Protected || Area != o.Area || GateLayers.Count != o.GateLayers.Count) return false;
            for (int i = 0; i < GateLayers.Count; i++) if (GateLayers[i] != o.GateLayers[i]) return false;
            if (Layers.Count != o.Layers.Count) return false;
            for (int i = 0; i < Layers.Count; i++) if (Layers[i] != o.Layers[i]) return false;
            return true;
        }
    }
}
