namespace KatKat.Core.Live
{
    /// <summary>
    /// Canlı maç prototipi kuralları (docs/vr1/CLAUDE_CANLI_MAC.md, kullanıcı 2026-11-13). Kesin kurallar sabittir
    /// (iki katman, zincir yok, en az 3'lü grup); belgede "önerilen varsayım" ya da "ürün kararı olarak açık" denen değerler
    /// burada yapılandırmadır. Bu değerler canlı maç standardı DEĞİLDİR; yalnız yerel deneme içindir.
    /// </summary>
    public sealed class LiveRules
    {
        /// <summary>
        /// Hücre başına başlangıç katmanı (ilk belge: 2). Kullanıcı 2026-11-14: "canlı oyun çabuk bitiyor. 3 katmanlı yapalım" →
        /// varsayılan 3; 2 eski havuzlar içindir.
        /// </summary>
        public int Layers = 3;
        /// <summary>Kesin kural: kenardan bağlı en az 3 aynı renk grup.</summary>
        public const int MinGroup = GroupFinder.MinGroup;
        /// <summary>Kesin kural: mevcut dört Stone Burst rengi.</summary>
        public const int ColorCount = 4;

        /// <summary>Tahta boyutu (prototip 6×6; kullanıcı 2026-11-14: "oyun tahtasına 1 sütun daha ekleyelim" → 7 sütun × 6 satır).</summary>
        public int Width = 7, Height = 6;

        /// <summary>Hamle süresi (sn; kullanıcı 2026-11-14: "sıra kimdeyse 5 sn süresi olsun", sonra "5 saniye çok az olmuş … 10 saniye yapalım"). Dolunca tur pas geçer. 0 = süresiz.</summary>
        public float TurnSeconds = 15f; // 2026-11-25 (kullanıcı: "Hamle süresini 15 saniyeye çıkartalım")
        /// <summary>El boyutu (belge: hızlı yerel test için 8; çizimdeki 8 yuva).</summary>
        public int HandSize = 8;
        /// <summary>Oyuncu başına tur (belge: hızlı yerel test için 8). Pas da bir tur harcar.</summary>
        public int TurnsPerPlayer = 8;
        /// <summary>
        /// Hazır gruba doğrudan dokunarak patlatma (tur harcar, el taşı harcamaz). Varsayılan açık (kullanıcı 2026-11-14: "Taş
        /// yerleştirmeden dokunarak da patlatma olsun … ya da direkt hazır patlamayı bekleyen taşlar patlatılır").
        /// </summary>
        public bool TapReadyGroups = true;
        /// <summary>İlk prototip: rakipte hedef hücrede yeni 3'lü kuran saldırı geçersiz.</summary>
        public bool ForbidAttackGroup = true;

        /// <summary>Eşit puanda karar (kullanıcı 2026-11-13: "Eşitlik bozma ekle"). Açık ürün kararı; ölçümle karşılaştırılır.</summary>
        public LiveTieBreak TieBreak = LiveTieBreak.BiggestGroupThenSecond;

        /// <summary>
        /// Farklı ama denk eller (kullanıcı 2026-11-13; ayna oyunu kırmak için). Açıkken iki oyuncunun eli aynı renk
        /// sayı profilinde ama renkleri farklı (renk değişimi), açılış değeri eşit (<see cref="LiveGenerator.Validate"/>). Belgedeki
        /// "aynı ilk el" kuralını değiştirir. Varsayılan açık (kullanıcı 2026-11-13: "Evet, varsayılan yap"; bot ölçümünde beraberlik %50 → %16).
        /// </summary>
        public bool DifferentHands = true;

        /// <summary>
        /// Alt katman üsttekinden farklı (kullanıcı 2026-11-13: "alt katmandaki taş dizilimi üstteki ile aynı olmayacak"): her hücrede
        /// alt renk üst renkten farklıdır; soyulan taşın altından hep başka bir renk çıkar. Varsayılan açık; kapalıyken eski
        /// (v1/v2) havuzlar aynen yeniden üretilir.
        /// </summary>
        public bool BottomDiffersFromTop = true;

        /// <summary>
        /// Tam dikdörtgen tahta (kullanıcı 2026-11-14: "Tahtaların ortasında boşluk olmasın tahtaların şekli tek tip olacak. Düz
        /// köşeli olacak"): delik ve kesik köşe yok, her hücre etkin. Varsayılan açık (havuz sürüm 4); kapalıyken eski havuzlar aynen.
        /// </summary>
        public bool FullBoard = true;

        /// <summary>
        /// İki raunt (kullanıcı 2026-11-14: "rövanşta el değişsin"; sonra "rövanş olmayacak 2 raunt olacak ilk raunt bitince direkt
        /// 2. raunt başlayacak"): maç aynı dizilimde iki raunttur; 2. rauntta eller ve ilk oynayan yer değiştirir, kazanan iki
        /// rauntun toplam puanıyla belirlenir (<see cref="LiveSeries"/>).
        /// </summary>
        public bool TwoRounds = true;

        /// <summary>
        /// Raunt sayısı (kullanıcı 2026-11-25: "3 raunta çıkartalım"); <see cref="TwoRounds"/> kapalıysa tek raunt. 1. raunt normal,
        /// 2. rauntta eller ve ilk oynayan değişir, 3. raunt havuzdaki sonraki dizilimde yeni ve denk ellerle, geride olan başlar.
        /// </summary>
        public int Rounds = 3;

        /// <summary>Rakibin tahtasına taş koyma (saldırı). Kullanıcı 2026-11-25: "rakibe taş koymayı iptal edelim" → kapalı.</summary>
        public bool AllowAttack = false;

        /// <summary>Oynanacak raunt sayısı.</summary>
        public int RoundCount => TwoRounds ? System.Math.Max(1, Rounds) : 1;

        public LiveRules Clone() => (LiveRules)MemberwiseClone();

        public static string TieBreakName(LiveTieBreak t) => t switch
        {
            LiveTieBreak.None => "yok (berabere)",
            LiveTieBreak.BiggestGroup => "en büyük tek grup",
            LiveTieBreak.SecondMover => "ikinci oynayan",
            _ => "en büyük tek grup, o da eşitse ikinci oynayan",
        };

        public override string ToString() =>
            $"tahta {Width}x{Height}, el {HandSize} (yenilemesiz), tur {TurnsPerPlayer}/oyuncu, dokunarak patlatma {(TapReadyGroups ? "açık" : "kapalı")}, " +
            $"rakipte 3'lü kuran saldırı {(ForbidAttackGroup ? "geçersiz" : "serbest")}, eşitlik bozma: {TieBreakName(TieBreak)}, eller {(DifferentHands ? "farklı ama denk" : "aynı")}, alt katman {(BottomDiffersFromTop ? "üstten farklı" : "serbest")}, tahta {(FullBoard ? "tam dikdörtgen" : "delikli olabilir")}, zincir yok, {Layers} katman";
    }

    /// <summary>
    /// Eşit puanda kazanan.
    /// None: berabere. BiggestGroup: maçtaki en büyük tek soyma (bir hamlede alınan puan) büyük olan; o da eşitse berabere.
    /// SecondMover: ikinci oynayan (ilk oynama avantajını dengeler). BiggestGroupThenSecond: önce en büyük grup, eşitse ikinci
    /// oynayan; beraberlik kalmaz.
    /// </summary>
    public enum LiveTieBreak { None, BiggestGroup, SecondMover, BiggestGroupThenSecond }
}
