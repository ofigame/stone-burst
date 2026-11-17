# Yayın eksikleri (iOS ilk yayın, Stone Burst 1.0.0)

Tarih: 2026-11-09. Kullanıcı: "yayın için eksikleri tamamlayalım". Kararlar: reklamla çıkılacak ("reklamsız çıkmayalım çünkü güncellemeyle reklam gelirse hoş karşılanmaz").

## Yapıldı (2026-11-09)

- **Ad ve kimlik:** Stone Burst; `com.ofigame.stoneburst` (iOS + Android); yeni logo; sürüm 1.0.0.
- **Bölüm kilidi:** `TestUnlockAll = false`; bölümler oynadıkça açılır.
- **Sıralama ekranı:** "Kayıt ol" düğmesi "SENİN SIRAN" başlığının üstünden alındı.
- **AdMob:**
  - Google Mobile Ads 11.6.0 eklentisi içe aktarıldı; EDM 2.1.0 Unity paket yöneticisinde.
  - `Scripts/Ads/AdMobNetwork.cs`: UMP rıza akışı, ödüllü ve geçiş reklamı.
  - `Editor/AdMobSetup.cs`: uygulama kimlikleri, ATT metni, `KATKAT_ADMOB` tanımı.
  - Şu an Google **TEST** kimlikleri kullanılıyor.
- **CI:** `.github/workflows/ios.yml` CocoaPods kuruyor, varsa workspace derliyor.
- **Metinler:**
  - Mağaza açıklamaları `MAGAZA_ACIKLAMALARI.md` (en, tr, de, es, ar).
  - Gizlilik politikası taslağı ve App Privacy yanıtları `GIZLILIK_POLITIKASI.md`.
- **Sertifika:** CSR `E:\OFIGAME\Apple_Imza\`.

## Benim kalan işlerim

1. **Gizlilik seçenekleri düğmesi:** Ayarlarda, AB'de zorunlu (`AdMobNetwork.PrivacyOptionsRequired` / `ShowPrivacyOptions`).
2. **Gerçek AdMob kimlikleri:** Kullanıcı verince `AdMobSetup` ve `AdUnits` değişir; `UsingTestIds = false`.
3. **Firebase:** `GoogleService-Info.plist` gelince Firebase paketlerini ekle, `KATKAT_ONLINE`'ı aç (Game Center girişi, sıralama, bulut kaydı).
4. **Sertifika ve imza:** `.cer` gelince `.p12` üret; profil ve API anahtarıyla GitHub gizli anahtarları için talimat.
5. **Git ve GitHub:** depo adı gelince `git init`, ilk gönderim kullanıcı onayıyla.
6. **Ekran görüntüleri:** App Store iPhone 6,9" (1320×2868), en az 3 tane. Yakalama aracıyla üretilecek.
7. **Doğrulama:** Tam testler; Android APK; ilk CI iOS derlemesi; TestFlight'ta iPhone'da deneme.

## Kullanıcının yapacakları

1. **AdMob:**
   - admob.google.com'da hesap aç; iOS uygulaması ekle (App Store'daki uygulamaya bağla ya da "henüz yayında değil").
   - Ödüllü ve Geçiş reklam birimlerini aç.
   - Uygulama kimliğini ve iki birim kimliğini bana ver.
   - Gizlilik ve mesajlaşma: AB rıza mesajı ve iOS IDFA (ATT) açıklama mesajı oluştur.
2. **Firebase:** iOS uygulaması (`com.ofigame.stoneburst`) ekle; `GoogleService-Info.plist`'i ver; Authentication'da Game Center'ı aç; Firestore'u oluştur; `firebase/firestore.rules` kurallarını yayınla.
3. **Apple:**
   - CSR ile Apple Distribution sertifikası (`.cer`).
   - App Store dağıtım profili (`com.ofigame.stoneburst`).
   - App Store Connect API anahtarı (`.p8`, Key ID, Issuer ID).
4. **App Store Connect:**
   - Açıklama, anahtar kelimeler ve tanıtım metni (taslak hazır).
   - Destek adresi ve gizlilik politikası adresi.
   - Yaş derecelendirmesi anketi, App Privacy anketi (taslak yanıtlar hazır).
   - Fiyat: ücretsiz.
   - Game Center'ın açık olduğunu kontrol et.
5. **GitHub:** özel depo; Actions gizli anahtarları (`IOS_PLANI.md` §4); Unity lisansı.
6. **Gizlilik politikası:** taslaktaki `[…]` alanlarını doldur; metni bir web adresinde yayınla.

## AdMob kimlikleri (2026-11-09, kullanıcının hesabından; reklam kimlikleri gizli değil, uygulamaya gömülür)

- iOS App ID: `ca-app-pub-2558895513508689~7151786494`
- iOS Rewarded (`stoneburst_rewarded`): `ca-app-pub-2558895513508689/3403938167`
- iOS Interstitial (`stoneburst_interstitial`): `ca-app-pub-2558895513508689/2320142547`

## AdMob gizlilik mesajları (2026-11-09, yayında)

- Avrupa tüzükleri (GDPR): "Stone Burst GDPR". Yalnız Stone Burst (iOS), İngilizce + 26 Avrupa dili. Seçenekler: İzin ver, Seçenekleri yönet ve İzin verme açık; Kapat kapalı. Gizlilik politikası: https://ofigame.com/legal/privacy?lang=en
- IDFA açıklayıcı: "Stone Burst IDFA". Yalnız Stone Burst (iOS), İngilizce (ABD) + 47 dil, Google'ın hazır metni.
- Oyunda karşılığı: `AdMobNetwork` UMP akışı ve `NSUserTrackingUsageDescription` (`AdMobSetup.TrackingText`). Ayarlarda "Gizlilik seçenekleri" düğmesi henüz yok.

## Unity Ads (2026-11-09; AdMob arabuluculuğuyla çağrılır)

- Unity Dashboard: proje "Stone Burst", uygulama "Stone Burst iOS" (genel kitle, çocuklara yönelik değil).
- Game ID: `800429766` · Organization core ID: `18968515891482`.
- Placement'lar: `BP_Rewarded_iOS` (ödüllü), `BP_Interstitial_iOS` (geçiş). Banner yok.
- Stats API Key gizlidir; kayıtlara yazılmaz. AdMob arabuluculuk ayarına kullanıcı kendisi yapıştırır.
- AdMob uyumlulaştırma grupları (2026-11-09, etkin): "Stone Burst Ödüllü iOS - Unity" (`stoneburst_rewarded` ↔ `BP_Rewarded_iOS`) ve "Stone Burst Geçiş iOS - Unity" (`stoneburst_interstitial` ↔ `BP_Interstitial_iOS`). Teklif sistemi: AdMob Network + Unity Ads; şelale boş. Unity–AdMob iş ortaklığı foi için önceden etkin olduğundan API anahtarı yeniden girilmedi.
- Oyunda (2026-11-09): iOS gerçek AdMob kimlikleri (`AdMobSetup.IosAppId`, `AdUnits`); Android test kimliği (AdMob Android uygulaması yok). Unity Ads uyumlulaştırma eklentisi 3.22.0 (AdMob 11.6.0 ile test edilmiş) `Assets/GoogleMobileAds/Mediation/UnityAds`. EditMode 194/194. `AdMobNetwork.cs` yalnız cihaz derlemesinde derlenir; henüz derlenmedi.
- Kontrol edildi (2026-11-09): AdMob Avrupa tüzükleri reklam iş ortakları listesinde Unity Ads seçili (Unity Technologies SF, IAB 1549; 210 iş ortağı otomatik, reklam kaynakları otomatik ekleniyor).
- Ayarlar → "Gizlilik seçenekleri" (2026-11-09): 5. satır, yalnız UMP gerekli gördüğünde (AB/İngiltere/İsviçre, ABD eyaletleri); düğme UMP formunu açar (`Scripts/Ads/PrivacyOptions.cs`). Görüntü `docs/ui3_uygulama/41_ayarlar_gizlilik.png`. 9 dil, loc-check TAMAM, EditMode 194/194. Cihazda denenmedi.

## Firebase (2026-11-09)

- Proje "Stone Burst" (`stone-burst`), Spark (ücretsiz). Google Analytics ve Gemini kapalı (politikada "analitik yok").
- iOS uygulaması `com.ofigame.stoneburst`. `GoogleService-Info.plist` → `Assets/KatKat/Firebase/`.
- Authentication: Game Center etkin.
- Firestore: (default), eur3 (Avrupa; kullanıcı kararı), üretim modu.
  - Kurallar `firebase/firestore.rules` ile yayınlandı (`scores`, `saves`).
  - Birleşik dizin `scores`: country ↑, score ↓ (oluşturuluyor).
- Oyunda (2026-11-09):
  - Firebase 14.1.0 (app, auth, firestore) ve Google EDM 1.2.190 `Packages/firebase/*.tgz` (depoya girer; app 61 MB).
  - AdMob eklentisinin kendi EDM kurulumu kapatıldı (`enableExternalDependencyManager=false`); Unity EDM ile Google EDM çakışmasın diye.
  - `KATKAT_ONLINE` yalnız iOS: Game Center girişi, Firestore sıralaması, Firestore bulut kaydı açık. Android test hesabıyla kalır.
  - iOS dışa aktarımı başarılı: Firebase iOS SDK 13.1.1 Swift Package Manager ile, AdMob ve Unity Ads CocoaPods ile ekleniyor. "REVERSED_CLIENT_ID yok" uyarısı zararsız (Google ile giriş kullanılmıyor).
  - EditMode 194/194. Cihazda denenmedi.

## Android reklamları: gerçek AdMob + Unity Ads (2026-11-12, kullanıcı: "androidde de unity ads'i aktif edelim")

- **AdMob:**
  - Android uygulaması "Stone Burst": `ca-app-pub-2558895513508689~5521782694`. Mağazada listelenmiyor; Play yayınından sonra mağaza bağlanmalı, o zamana kadar sınırlı reklam sunumu.
  - Birimler:
    - `stoneburst_rewarded`: `ca-app-pub-2558895513508689/9721541294`
    - `stoneburst_interstitial`: `ca-app-pub-2558895513508689/6260662649`
- **Unity:**
  - Uygulama "Stone Burst Android" (proje Stone Burst; genel kitle, çocuklara yönelik değil). Oyun kimliği `800434959`.
  - Teklifli yerler: `BP_Rewarded_Android`, `BP_Interstitial_Android`.
- **AdMob uyumlulaştırma grupları (etkin):**
  - "Stone Burst Ödüllü Android - Unity" (grup 3664556984).
  - "Stone Burst Geçiş Android - Unity" (6552802227).
  - Teklif: AdMob Network + Unity Ads.
- **Oyunda:**
  - `AdMobSetup.AndroidAppId` ve `AdUnits` Android gerçek kimlikler.
  - Google Play APK derlendi; manifestte gerçek uygulama kimliği, Unity arabulucu adaptörü içeride.
  - Cihazda denenmedi. Test cihazı AdMob'da tanımlanmalı; kendi reklamına tıklanmamalı.
- **AppGallery:** Unity'nin yeni paneli "yalnız arabuluculuk kullananları destekliyor"; yeni yerler yalnız teklifli (`BP_`). Doğrudan Unity Ads entegrasyonu belirsiz; karar kullanıcıda.
