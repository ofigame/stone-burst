using System;
using System.Collections.Generic;

namespace KatKat.Meta
{
    /// <summary>
    /// v9 kayıt bölümü (saveVersion 2): koleksiyon taşları, günlük bulmaca hakları ve ödül günlüğü. Bölüm ilerlemesinden
    /// ayrı tutulur; normal ilerleme ve ödül günlüğü birbirine karışmaz. JsonUtility uyumlu (yalnız alanlar ve listeler).
    /// </summary>
    [Serializable]
    public sealed class MetaSave
    {
        /// <summary>Kalıcı kazanılmış koleksiyon taşı kimlikleri (tek seferlik; kazanım sırasıyla).</summary>
        public List<string> stones = new List<string>();
        /// <summary>Günlük bulmacada gücü kullanılacak seçili taş (boşsa ilk taş).</summary>
        public string selectedStone = "";
        /// <summary>Oyuncunun en az bir kez açtığı ana bölümler ("ilk deneme" ayrımı için).</summary>
        public List<string> attemptedLevels = new List<string>();
        /// <summary>Bugünün günlük bulmaca durumu (gün değişince sıfırlanır).</summary>
        public DailySave daily = new DailySave();
        /// <summary>Günlük başarı ödülleri: "gün|bulmaca|içerik sürümü" (gün başına bir kayıt).</summary>
        public List<string> rewardLog = new List<string>();
        /// <summary>+1 hamle ödülünün geçerli olduğu gün (dayId); başka günde etkisiz.</summary>
        public string moveBonusDay = "";
        /// <summary>v10 P5: ZPE ham girdileri (son 60 normal bölüm; algoritma değişince yeniden hesaplanır).</summary>
        public List<ZpeRecord> zpeHistory = new List<ZpeRecord>();
        /// <summary>v11 ZPE: oynanan bütün normal bölümlerin puan toplamı (her biri 0–1) ve sayısı → düz ortalama.</summary>
        public double zpeSum;
        public int zpeGames;
        /// <summary>v12 sıralama yıldızı: reklam yardımı olmadan kazanılmış en iyi yıldız ("bölümKimliği|n").</summary>
        public List<string> rankStars = new List<string>();
        /// <summary>v12 deneme seti: bitirilen deneme bölümleri (kimlik).</summary>
        public List<string> trialDone = new List<string>();
        /// <summary>v8: ana bölümlerde görülmüş bölüm türleri (tür tanıtım kartı yalnız ilk kez).</summary>
        public List<string> seenTypes = new List<string>();
        /// <summary>Günlük bulmacadan kazanılan yıldızların toplamı (kullanıcı, 2026-11-08: performansa göre 1–3, günde bir kez,
        /// ilk galibiyette). Toplam yıldıza ve taş eşiklerine sayılır; sıralamada sayılmaz.</summary>
        public int dailyStars;
        /// <summary>Kalp sistemi (2026-11-09): eksik kalp sayısı (0 = dolu) ve dolum sayacının başladığı an (Unix sn). Bkz. <see cref="Hearts"/>.</summary>
        public int heartsMissing;
        public long heartsStamp;
        /// <summary>Uygulama içi puan isteği (2026-11-09): kaç kez istendi, son istek (Unix sn).</summary>
        public int reviewAsks;
        public long reviewLast;
        /// <summary>vr1 (onay 2026-11-11, 07 "Günlük ilk giriş ücretsiz"): oyuncunun ilk günlük girişi reklamsız yapıldı mı.</summary>
        public bool dailyFreeUsed;
        /// <summary>vr1 P1 (03_KOLEKSIYON_ODUL "Haritaya dönüş"): haritanın en son gösterdiği toplam yıldız ve tamamlanan bölüm
        /// sayısı. Haritaya dönüşte aradaki net fark bir kez özetlenir; -1 = henüz yok (eski kayıt: fark gösterilmez).</summary>
        public int mapSeenStars = -1;
        public int mapSeenLevels = -1;
        /// <summary>vr1 P1 (03 "pendingPresentation"): kazanılmış ama sunumu (vitrin) henüz görülmemiş taşlar. Uygulama
        /// sunumdan önce kapanırsa sonraki açılışta yeniden gösterilir; ödül yeniden verilmez (taş zaten <see cref="stones"/>'da).</summary>
        public List<string> pendingReveal = new List<string>();

        public void Sanitize()
        {
            stones ??= new List<string>();
            selectedStone ??= "";
            attemptedLevels ??= new List<string>();
            daily ??= new DailySave();
            daily.Sanitize();
            rewardLog ??= new List<string>();
            moveBonusDay ??= "";
            zpeHistory ??= new List<ZpeRecord>();
            rankStars ??= new List<string>();
            trialDone ??= new List<string>();
            seenTypes ??= new List<string>();
            pendingReveal ??= new List<string>();
            if (zpeGames < 0 || double.IsNaN(zpeSum) || zpeSum < 0) { zpeGames = 0; zpeSum = 0; }
            if (dailyStars < 0) dailyStars = 0;
            if (heartsMissing < 0 || heartsMissing > Hearts.Max) heartsMissing = 0;
            if (heartsStamp < 0) heartsStamp = 0;
            if (reviewAsks < 0) reviewAsks = 0;
            if (reviewLast < 0) reviewLast = 0;
        }
    }

    /// <summary>Bir günün günlük bulmaca durumu. Kimlik: dayId + challengeId + contentVersion.</summary>
    [Serializable]
    public sealed class DailySave
    {
        public string dayId = "";
        public string challengeId = "";
        public string contentVersion = "";
        /// <summary>v10 P3.4: o günün seviye grubu (ilk açılışta sabitlenir; gün içinde ilerleme değiştirmez). 0 = yok.</summary>
        public int tier;
        /// <summary>Günlük giriş hakkı açıldı (ödüllü reklam ya da reklamsız hak).</summary>
        public bool entered;
        /// <summary>"ad" | "adFree" | "debug".</summary>
        public string entrySource = "";
        /// <summary>Açılan deneme sayısı (ilk deneme + ücretsiz tekrar).</summary>
        public int attemptsUsed;
        /// <summary>İzin verilen deneme sayısı (giriş açılınca yazılır).</summary>
        public int attemptsAllowed;
        /// <summary>Kalıcı oturum kaydı açık: uygulama kapanırsa aynı deneme yeniden açılır, hak yeniden harcanmaz.</summary>
        public bool attemptOpen;
        /// <summary>Açık denemede taş gücü harcandı.</summary>
        public bool powerUsed;
        public bool won;
        /// <summary>Bu gün kazanılan günlük yıldızı (0 = henüz yok; 1–3).</summary>
        public int stars;
        /// <summary>İşlenmiş reklam ödül kimlikleri (aynı geri çağrı iki kez gelirse yeni hak üretmez).</summary>
        public List<string> adTokens = new List<string>();

        public string Key => DailyKey(dayId, challengeId, contentVersion);

        public static string DailyKey(string day, string challenge, string version) => $"{day}|{challenge}|{version}";

        public void Sanitize()
        {
            dayId ??= ""; challengeId ??= ""; contentVersion ??= ""; entrySource ??= "";
            adTokens ??= new List<string>();
            if (attemptsUsed < 0) attemptsUsed = 0;
            if (attemptsAllowed < 0) attemptsAllowed = 0;
        }
    }
}
