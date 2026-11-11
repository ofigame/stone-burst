using System;
using System.Collections.Generic;

namespace KatKat.Meta
{
    /// <summary>Oturum türü: ana bölüm kataloğu, günlük bulmaca ya da katalog dışı (yakalama/deneme).</summary>
    /// <summary>Trial: v12 deneme seti (ana ilerlemeye, yıldızlara, ZPE'ye yazılmaz).</summary>
    public enum SessionKind { Normal, Daily, Custom, Trial }

    /// <summary>
    /// Oturumda kullanılan yardımlar. Rekabetçi ölçüm (ileride ZPE) bunları ayrı değerlendirir; bu pakette puan hesaplanmaz.
    /// </summary>
    [Flags]
    public enum AssistFlags
    {
        None = 0,
        /// <summary>Geri alma kullanıldı.</summary>
        Undo = 1,
        /// <summary>Günlük bulmaca ödülü: oturum +1 hamleyle açıldı.</summary>
        DailyMoveBonus = 2,
        /// <summary>Koleksiyon taşı gücü kullanıldı (günlük bulmaca).</summary>
        StonePower = 4,
        /// <summary>Öğretici turu ya da dokunma ipucu halkası gösterildi.</summary>
        Guidance = 8,
        /// <summary>Ödüllü reklamla alınan yardım (henüz yok; sözlükte ayrılmış).</summary>
        RewardedAd = 16,
    }

    /// <summary>
    /// v9 A: bir bölüm oturumunun sonucu. Hamle limiti (taban + ek), kullanılan hamle, bölüm ve içerik sürümü, kaçıncı
    /// deneme, başarı, yardımlar ve etkin süre. Yerel telemetri temeli; sunucuya gönderilmez, ZPE hesaplanmaz.
    /// </summary>
    public sealed class SessionOutcome
    {
        public SessionKind Kind;
        public string LevelId;
        public string ContentVersion;
        /// <summary>Bölüm verisindeki hamle limiti.</summary>
        public int BaseMoveLimit;
        /// <summary>Oturum açılırken eklenen hamle (günlük ödül +1).</summary>
        public int MoveBonus;
        public int MoveLimit => BaseMoveLimit + MoveBonus;
        public int MovesUsed;
        /// <summary>Bu bölüm kimliğinde oyuncunun ilk oturumu ve ilk denemesi mi.</summary>
        public bool FirstAttempt;
        /// <summary>1 tabanlı deneme sırası (aynı oturumda yeniden başlatma yeni deneme sayılır).</summary>
        public int AttemptNumber = 1;
        public bool Finished;
        public bool Success;
        public AssistFlags Assists;
        /// <summary>Oyuncunun etkin süresi (duraklatma ve uygulama arka plandayken sayılmaz): karar + animasyon + öğretici.</summary>
        public double ActiveSeconds;
        /// <summary>vr1 P0 (02_ZPE): oyuncunun gerçekten girdi verebildiği karar süresi (Idle/Targeting). ZPE hız
        /// bileşeni yalnız bunu kullanır; hamle animasyonu, reklam, modal, zorunlu öğretici ve ipucu hazırlığı girmez.</summary>
        public double DecisionSeconds;
        /// <summary>Hamle/zincir/özel taş oynatma süresi (ZPE dışı; telemetri).</summary>
        public double AnimationSeconds;
        /// <summary>Zorunlu öğretici turunda geçen süre (ZPE dışı).</summary>
        public double TutorialSeconds;
        /// <summary>Günlük oturumda: gün + bulmaca + içerik sürümü kimliği.</summary>
        public string DailyKey;

        /// <summary>Süre ölçüm sürümü: 0 = eski (animasyon dahil tek sayaç), 2 = karar süresi ayrı (2026-11-11).</summary>
        public const int TimeMeasureVersion = 2;

        /// <summary>Bir karelik süreyi doğru sayaca ekler. Büyük kare (arka plandan dönüş) en çok <see cref="MaxFrame"/> sayılır.</summary>
        public void Tick(PlayTime kind, double dt)
        {
            if (Finished || kind == PlayTime.None || dt <= 0) return;
            if (dt > MaxFrame) dt = MaxFrame;
            ActiveSeconds += dt;
            if (kind == PlayTime.Decision) DecisionSeconds += dt;
            else if (kind == PlayTime.Animation) AnimationSeconds += dt;
            else TutorialSeconds += dt;
        }

        public const double MaxFrame = 0.5;

        public bool Assisted => Assists != AssistFlags.None;
        public bool Has(AssistFlags f) => (Assists & f) == f;

        public override string ToString() =>
            $"{Kind} {LevelId} [{ContentVersion}] deneme {AttemptNumber}{(FirstAttempt ? " (ilk)" : "")} hamle {MovesUsed}/{MoveLimit}" +
            $"{(MoveBonus > 0 ? $" (+{MoveBonus})" : "")} {(Finished ? (Success ? "başarı" : "başarısız") : "sürüyor")} yardım {Assists} süre {ActiveSeconds:0.0}s (karar {DecisionSeconds:0.0}s, anim {AnimationSeconds:0.0}s, öğretici {TutorialSeconds:0.0}s)";
    }

    /// <summary>Bir karelik sürenin türü (<see cref="SessionOutcome.Tick"/>). None: sayılmaz (duraklatma, modal, reklam,
    /// odak kaybı, ipucu hazırlığı, bitiş kutlaması).</summary>
    public enum PlayTime { None, Decision, Animation, Tutorial }

    /// <summary>İçerik sürüm etiketleri (sonuç modeli ve günlük durum anahtarı için).</summary>
    public static class ContentVersions
    {
        /// <summary>Ana 1.000 bölüm seti (v7 üretimi, 2026-11-06).</summary>
        public const string Levels = "levels-v7";
    }
}
