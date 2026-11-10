# Play Store yayın planı (Stone Burst Android 1.0)

Tarih: 2026-11-10. Kullanıcı: "play store hazırlığımızı da yapalım". Kararlar: iOS ile aynı kapsam (Play Games girişi, Firestore sıralama ve bulut kaydı, Firebase Analytics), imza anahtarını Claude oluşturur, doğrudan üretim kanalı.

## Durum (başlangıç)

- Hazır: paket `com.ofigame.stoneburst`, yeni ikon (uyarlanabilir), IL2CPP ARMv7+ARM64, minSdk 26, mağaza metinleri (en, tr, de, es, ar, ja), gizlilik politikası, AdMob/UMP altyapısı, `Online/PlayGamesAccountService.cs` (yazılı, derlenmedi).
- Eksik: yükleme imzası, AAB, hedef API seviyesinin sabitlenmesi, Play Games eklentisi, Firebase Android uygulaması, AdMob Android uygulaması/birimleri, Play Console kaydı ve beyanlar, Android görselleri.

## Adımlar

### A. Yerelde (Claude)
1. **Yükleme anahtarı:** `E:\OFIGAME\OYUNLAR\keys\stoneburst-upload.jks` (takma ad `stoneburst`), şifre aynı klasörde `stoneburst-upload-sifre.txt`; ekrana, belgeye, git'e yazılmaz. foi anahtarı kullanılmaz. Play App Signing açık olacağı için yükleme anahtarı kaybolursa Play Console'dan sıfırlanabilir.
2. **Derleme:** `BuildScripts` mağaza modunda AAB üretir, anahtarı ortamdan/yerel dosyadan okur; `bundleVersionCode` artar; hedef API Play'in istediği seviyeye sabitlenir.
3. **Play Games Services v2 eklentisi** (GitHub `playgameservices/play-games-plugin-for-unity` sürümü) projeye eklenir; `KATKAT_ONLINE` Android'de de açılır.
4. **Firebase Android:** `google-services.json` (stoneburst Android uygulaması); Analytics Android'de de açılır (`FirebaseAnalyticsSink`).
5. **Gerçek AdMob Android kimlikleri** (`AdMobSetup.AndroidAppId`, `AdUnits`).
6. **Görseller:** telefon ekran görüntüleri (1080×1920, 6 dil, mevcut başlıklı kompozisyon), tablet (isteğe bağlı), tanıtım görseli 1024×500, ikon 512×512.
7. **Testler** ve cihazda deneme (APK/AAB iç kurulum).

### B. Konsollarda (onayla; beyanlar kullanıcının)
1. **Play Console:** "Stone Burst" uygulaması (oyun, ücretsiz) → mağaza sayfası (6 dil), kategori Bulmaca, iletişim, gizlilik URL'si.
   - Kullanıcı beyanları: İçerik derecelendirmesi (IARC), Veri güvenliği, Reklam içeriyor, Hedef kitle (13+), Uygulama erişimi (giriş gerekmez), Haber/hükümet/sağlık değil.
2. **Play Games Services:** oyun projesi, Android kimlik bilgisi (paket + uygulama imzalama ve yükleme anahtarlarının SHA-1'i), oyun sunucusu (web) kimlik bilgisi → Firebase Auth "Play Games" sağlayıcısı için istemci kimliği/sırrı.
3. **Firebase:** Android uygulaması (SHA-1/SHA-256), Authentication → Play Games etkin.
4. **AdMob:** Android uygulaması (yayında değil → sınırlı sunum; yayından sonra mağaza bağlanır), ödüllü + geçiş birimleri; GDPR mesajına Android uygulamasını ekle.
5. **Yükleme:** AAB → Üretim kanalı (yeni kişisel hesapsa önce 14 gün/12 testçi kapalı test şartı).

## Notlar
- Android'de ATT yok; UMP rıza formu AB'de gösterilir.
- Hesap türü (kişisel/kurum) ve kapalı test şartı Play Console'da uygulama oluşturulurken görülecek.

## İlerleme

- 2026-11-10: Yükleme anahtarı oluşturuldu (`E:\OFIGAME\OYUNLAR\keys`, depo dışında; kullanıcı: "E:\OFIGAME\OYUNLAR keys olsun"). Parmak izleri (gizli değil):
  - SHA-1: `FF:16:02:22:84:09:67:F7:96:61:B0:57:DB:D2:31:63:A9:5A:0A:1A`
  - SHA-256: `23:B9:2E:95:E9:67:25:07:22:D9:BF:6D:3A:AE:11:88:B6:9B:BA:06:52:E4:5B:D4:91:3D:2B:20:DC:FA:28:B0`
  - Play App Signing açılınca Google'ın uygulama imzalama anahtarının parmak izleri de Play Games / Firebase'e eklenecek.
- `BuildScripts.BuildAndroidStore` (menü: Kat Kat/Build/Android Play Store (AAB)): imzalı AAB, `KATKAT_ANDROID_VERSION_CODE`; komut `tools/unity.ps1 build-android-store`.
- Hedef API 36 (`ProjectSetup`).
- İmzalı AAB denendi (sürüm kodu 1, yüklenmedi): `Builds/Android/StoneBurst-1.0.0-1.aab` 302 MB; içinde veri paketi 3 doku biçimiyle (varsayılan/ETC2/ASTC, her biri ~78 MB) — Play cihaza birini gönderir, cihaz başına indirme ≈110 MB. İmza SHA-1 yükleme anahtarıyla eşleşiyor. Derleme sonrası imza ayarları ProjectSettings'ten temizleniyor (toplu modda çıkış temizlikten sonra).
- 2026-11-10: **Play Console'da uygulama oluşturuldu** (kullanıcı onayı; beyanları kullanıcı işaretledi): "Stone Burst: Puzzle Game", `com.ofigame.stoneburst`, oyun, ücretsiz, varsayılan dil en-US. Uygulama kimliği 4972081418673444833, geliştirici hesabı "ofigame" (kişisel, 8462007382825876941).
  - **Kapalı test şartı VAR:** üretime başvurmadan önce en az 12 test kullanıcısıyla en az 14 gün kapalı test. "Doğrudan üretim" kararı bu yüzden: kurulum → kapalı test (12+ kişi, 14 gün) → üretim başvurusu.
  - Hesap uyarısı: satıcı statüsü Şubat 2027'ye kadar beyan edilmeli (kullanıcı).
  - Kurulum görevleri: gizlilik politikası, oturum açma bilgileri (uygulama erişimi), reklam, içerik derecelendirme, hedef kitle, veri güvenliği, resmi kurum / finans / sağlık beyanları, kategori ve iletişim, mağaza girişi. Ardından kapalı test kanalı, ülkeler, test kullanıcıları, sürüm.
- 2026-11-10 Play Console (Claude girdi, taslak kaydedildi):
  - Gizlilik politikası URL'si; Oturum açma bilgileri: kısıtlı bölüm yok; Kategori: Oyun → Bulmaca; İletişim: info@ofigame.com, https://ofigame.com (foi ile aynı).
  - Mağaza girişi 6 dil (en-US varsayılan, de-DE, tr-TR, es-ES, ar, ja-JP): ad (iOS'takiyle aynı), kısa açıklama (yeni, ≤80), uzun açıklama (iOS metinleri).
  - Görseller: ikon 512 (`docs/magaza/play/icon-512.png`), tanıtım görseli 1024×500 (`feature-1024x500.png`), en-US telefon ekran görüntüleri 1080×1920 (`docs/magaza/android/en`, sıra 03,02,01,05,04,06). Diğer 5 dile de kendi telefon ekran görüntüleri yüklendi (2026-11-10, aynı sıra; `docs/magaza/android/(dil)`).
  - Kullanıcının beyanları (Claude'un tıklaması engellendi): Reklam (Evet), İçerik derecelendirme (IARC), Hedef kitle, Veri güvenliği, Resmi kurum / Finans / Sağlık.
- 2026-11-10: **Kurulum görevleri 11/11 tamam** (beyanları kullanıcı işaretledi, Claude ekrandan yönlendirdi):
  - Reklam: içeriyor.
  - IARC (info@ofigame.com, kategori Oyun): IARC 3+, ESRB E, PEGI 3, USK 0.
  - Hedef kitle: 13-15, 16-17, 18+ (13 altı yok → Aileler politikası dışı).
  - Veri güvenliği: paylaşım yok, aktarımda şifreli, hesap OAuth (Play Games), hesap/veri silme bağlantısı gizlilik politikası (e-postayla 30 gün içinde). Toplanan (hepsi zorunlu, kısa süreli değil): Ad ve Kullanıcı kimlikleri (uygulama işlevi, hesap yönetimi); Yaklaşık konum (analiz, reklam); Teşhisler (analiz); Uygulama etkileşimleri (analiz, reklam); Diğer işlemler (uygulama işlevi, analiz); Cihaz kimlikleri (analiz, sahtekarlık önleme, reklam). Android'de Play Games / Firebase / Analytics henüz açık değil; beyan tam kapsama göre yapıldı.
  - Resmi kurum: Hayır; Finans: özellik yok; Sağlık: özellik yok.
  - Mağaza girişi taslağı kaydedildi; yapay zeka öğe beyanı: "Öğeleri etiketleme" (kullanıcı).
  - Sırada: Play Games Services + Firebase Android + AdMob Android kimlikleri, sonra kapalı test (12 kişi, 14 gün).
- 2026-11-10: **Play Games + Firebase Android kuruldu** (ayrıntı `docs/KARARLAR.md`). Play Oyun Hizmetleri projesi (216600100064, taslak, yayınlanmadı), OAuth rıza ekranı (External, test modu), Android kimlik bilgisi (yükleme anahtarı SHA-1), oyun sunucusu kimlik bilgisi, Kaydedilmiş oyunlar açık; Firebase Android uygulaması (`google-services.json`), Auth'ta Play Games etkin. Test APK derlendi (`Builds/Android/StoneBurst.apk`, 11 dk, hata yok; izinler: INTERNET, ağ durumu, reklam kimliği, titreşim vb., konum/bluetooth yok). Cihazda giriş DENENMEDİ.
  - Kalan: ilk AAB'yi kapalı teste yükle → Play App Signing SHA-1'ini ikinci Android kimlik bilgisi olarak ekle; Play Games projesini yayınla; test kullanıcıları (12+, 14 gün). Gerçek AdMob Android kimlikleri üretim başvurusundan önce (karar: kapalı testte test reklamları).
