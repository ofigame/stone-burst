namespace KatKat.Core.Live
{
    /// <summary>
    /// Canlı maç prototipi kuralları (docs/vr1/CLAUDE_CANLI_MAC.md, kullanıcı 2026-11-13). Kesin kurallar sabittir
    /// (iki katman, zincir yok, en az 3'lü grup); belgede "önerilen varsayım" ya da "ürün kararı olarak açık" denen değerler
    /// burada yapılandırmadır. Bu değerler canlı maç standardı DEĞİLDİR; yalnız yerel deneme içindir.
    /// </summary>
    public sealed class LiveRules
    {
        /// <summary>Kesin kural: her etkin hücre başlangıçta tam iki katman.</summary>
        public const int LayersPerCell = 2;
        /// <summary>Kesin kural: kenardan bağlı en az 3 aynı renk grup.</summary>
        public const int MinGroup = GroupFinder.MinGroup;
        /// <summary>Kesin kural: mevcut dört Stone Burst rengi.</summary>
        public const int ColorCount = 4;

        /// <summary>Tahta boyutu (belgede yok; prototip varsayımı 6×6).</summary>
        public int Width = 6, Height = 6;
        /// <summary>El boyutu (belge: hızlı yerel test için 8; çizimdeki 8 yuva).</summary>
        public int HandSize = 8;
        /// <summary>Oyuncu başına tur (belge: hızlı yerel test için 8). Pas da bir tur harcar.</summary>
        public int TurnsPerPlayer = 8;
        /// <summary>Açık karar: hazır gruba doğrudan dokunarak patlatma (tur harcar, el taşı harcamaz).</summary>
        public bool TapReadyGroups;
        /// <summary>İlk prototip: rakipte hedef hücrede yeni 3'lü kuran saldırı geçersiz.</summary>
        public bool ForbidAttackGroup = true;

        public LiveRules Clone() => (LiveRules)MemberwiseClone();

        public override string ToString() =>
            $"tahta {Width}x{Height}, el {HandSize} (yenilemesiz), tur {TurnsPerPlayer}/oyuncu, dokunarak patlatma {(TapReadyGroups ? "açık" : "kapalı")}, " +
            $"rakipte 3'lü kuran saldırı {(ForbidAttackGroup ? "geçersiz" : "serbest")}, zincir yok, 2 katman";
    }
}
