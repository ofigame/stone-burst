using System.Collections.Generic;

namespace KatKat.Levels
{
    public sealed class CellData
    {
        public int X;
        public int Y;
        public bool Active;
        /// <summary>Alttan üste renk kimlikleri.</summary>
        public readonly List<string> Layers = new List<string>();
        public int ClipHitsRemaining;
        public bool HasBottomStar;
        /// <summary>schemaVersion 2: overlay {kind:"ice", hits}.</summary>
        public int IceHits;
        /// <summary>schemaVersion 2: overlay {kind:"colorLock", color, remaining}. Boşsa kilit yok.</summary>
        public string LockColor;
        public int LockRemaining;

        /// <summary>schemaVersion 2: overlay {kind:"envelope", sealColor}. Boşsa zarf yok.</summary>
        public string EnvelopeColor;
        /// <summary>schemaVersion 2: base {kind:"rotor"} döner taban. Taban etkisi en çok bir (yıldız ya da taban).</summary>
        public bool HasRotor;

        /// <summary>M09: overlay {kind:"sponge", colors:[...]} gereken renkler.</summary>
        public readonly List<string> SpongeColors = new List<string>();
        /// <summary>M10: overlay {kind:"linkedClip", linkId, hits}.</summary>
        public int LinkId, LinkHits;
        /// <summary>M11: base {kind:"button", gate}: yığın boşalınca bu geçidi açar.</summary>
        public int ButtonGate;
        /// <summary>v12 deneme: "protected": true — kristal yığın (soyulursa kayıp).</summary>
        public bool Protected;
        /// <summary>M11 geçit hücresi: active=false, gate, gateLayers (açılınca bu katmanlarla etkinleşir).</summary>
        public int GateId;
        /// <summary>vr1 P3 iki alan: hücrenin alanı (0/1; yoksa 0).</summary>
        public int Area;
        public readonly List<string> GateLayers = new List<string>();

        public int CoverCount => (ClipHitsRemaining > 0 ? 1 : 0) + (IceHits > 0 ? 1 : 0) + (LockRemaining > 0 || !string.IsNullOrEmpty(LockColor) ? 1 : 0)
            + (!string.IsNullOrEmpty(EnvelopeColor) ? 1 : 0) + (SpongeColors.Count > 0 ? 1 : 0) + (LinkHits > 0 ? 1 : 0);
    }
}
