# iOS yayın planı (v13)

Tarih: 2026-11-09.

Kullanıcı: "ilk ios a yayınlayacağız", "ios derleme ayarlarını hazır et sonra githuba bağlayacağız orada derlenecek".

**Durum:** iOS ayarları ve Xcode projesi hazır. Windows'ta dışa aktarma denendi ve başarılı oldu. GitHub iş akışı yazıldı ama hiç çalıştırılmadı. IPA üretilmedi, iPhone'da denenmedi.

## 1. Hazır olanlar

**Oyuncu ayarları** (`ProjectSetup.ApplyIosSettings`, her derlemede uygulanır)
- Paket kimliği `com.ofigame.stoneburst` (kullanıcı, 2026-11-09; Android de aynı kimliğe geçti); IL2CPP, ARM64.
- En düşük iOS 15.0; iPhone ve iPad.
- Yalnız dikey; iPad'de tam ekran (bölünmüş ekran yok).
- Durum çubuğu gizli; alt kenar hareketi ertelenir.
- Otomatik imzalama kapalı: imza CI'da atılır.
- Ekip kimliği `KATKAT_IOS_TEAM`, yapı numarası `KATKAT_BUILD_NUMBER` ortam değişkenlerinden gelir.

**Dokular** (`BuildScripts.ApplyIosTextureBudget`)
- Android'de ASTC'ye ayarlı her doku iOS'ta da aynı ASTC biçiminde (174 doku).

**Xcode son ayarları** (`Editor/IosPostBuild.cs`)
- `ITSAppUsesNonExemptEncryption = false`: yalnız HTTPS kullanıldığı için şifreleme beyanı muaf.
- 9 dil listesi; görünen ad "TriKat".
- Game Center yetkisi (`TriKat.entitlements`).

**Yerel eklenti** (`Assets/Plugins/iOS/KatKatNative.mm`)
- Dokunsal geri bildirim (hafif / orta).
- Cihaz ülkesi (NSLocale); sıralamanın ülke sekmesi bunu kullanır.

**Hesap** (`Scripts/Online/GameCenterAccountService.cs`, kapalı: `KATKAT_ONLINE`)
- Game Center girişi açılışta sessiz yapılır; ad Game Center takma adıdır.
- Firebase Auth'a Game Center kimliğiyle girilir.
- Sıralama Android ile aynı Firestore tablosundadır.
- Bulut kaydı Firestore `saves/{uid}`'e yazılır; kuralı `firebase/firestore.rules` içinde.
- Apple kısıtı: oyuncu açılıştaki Game Center penceresini kapatırsa pencere o oturumda yeniden açılmaz. "Kaydet" düğmesi o zaman başarısız bildirim gösterir.

**Derleme**
- Yerelde: `tools/unity.ps1 build-ios` komutu `Builds/iOS` (Xcode projesi, yaklaşık 910 MB) üretir.
- 2026-11-09 denemesi: 1 dk 48 sn, hata yok.

**GitHub**
- `.github/workflows/ios.yml` iki işten oluşur:
  1. Ubuntu: GameCI ile Unity, Xcode projesini çıkarır.
  2. macOS 15: ikonlardan saydamlık kanalı kaldırılır, sertifika ve profil kurulur, `xcodebuild archive` ve export çalışır, IPA indirilebilir dosya olarak eklenir. "TestFlight'a yükle" seçilirse App Store Connect'e yüklenir.
- Elle başlatılır (Actions → iOS → Run workflow).
- `tools/ios/ExportOptions.plist`
- `.gitignore`: büyük kaynaklar dışarıda (`docs/taşlar`, `docs/captures`, ui1/ui2/ui3_uygulama, zip dosyaları, `ThirdParty/indirilen`). Depoya girecek boyut yaklaşık 650 MB (Assets 204 MB + kalan docs).

## 2. Kullanıcının yapacakları (Apple)

1. **Apple Developer Program** üyeliği (yıllık 99 $; şirket olarak katılım için D-U-N-S numarası gerekir).
2. **Identifiers** → App ID `com.ofigame.stoneburst`, Game Center yeteneği açık.
3. **Sertifika:** Apple Distribution sertifikası. Mac olmadan da olur: CSR'yi ben `openssl` ile üretirim, sertifika `.cer` olarak iner, ben `.p12`'ye çeviririm.
4. **Profil:** App Store dağıtım profili (App ID + sertifika) → `.mobileprovision`.
5. **App Store Connect:**
   - Uygulama: ad StoneBurst, paket `com.ofigame.stoneburst`, birincil dil English (U.S.).
   - Game Center'ı aç.
   - Users and Access → Integrations → App Store Connect API anahtarı (rol: App Manager) → `.p8`, Key ID, Issuer ID.
6. **Firebase:** projeye iOS uygulaması ekle (`com.ofigame.stoneburst`) → `GoogleService-Info.plist`. Authentication'da Game Center sağlayıcısını aç.

## 3. Kullanıcının yapacakları (GitHub ve Unity)

1. GitHub'da özel depo aç (adını bana söyle). Ben yerelde `git init` yapar, ilk gönderimi senin onayınla yaparım.
2. Unity lisansı (GameCI): Personal lisansta `UNITY_EMAIL` ve `UNITY_PASSWORD` yeterli; ilk etkinleştirme için GameCI'nin `.ulf` adımı gerekebilir.

## 4. GitHub gizli anahtarları (Settings → Secrets and variables → Actions)

| Ad | İçerik |
|---|---|
| `UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_LICENSE` | Unity hesabı / lisans dosyası (`.ulf` içeriği) |
| `APPLE_TEAM_ID` | 10 karakterlik ekip kimliği |
| `IOS_DIST_CERT_P12` | Dağıtım sertifikası `.p12`, base64 |
| `IOS_DIST_CERT_PASSWORD` | `.p12` parolası |
| `IOS_PROVISION_PROFILE` | `.mobileprovision`, base64 |
| `IOS_PROVISION_PROFILE_NAME` | Profilin Apple'daki adı |
| `APPSTORE_KEY_ID`, `APPSTORE_ISSUER_ID`, `APPSTORE_P8` | App Store Connect API anahtarı |

Anahtarları sen girersin; ben görmem ve hiçbir yere yazmam.

## 5. Yayından önce kalanlar (iOS)

- **Eklentiler:** Firebase 14.1.0 (app, auth, firestore) ve External Dependency Manager projeye eklenecek, `GoogleService-Info.plist` konacak, `KATKAT_ONLINE` açılacak. iOS'ta Play Games eklentisi gerekmez; Android'e geçince eklenir.
- **CI'da Unity sürümü:** GameCI'de `6000.6.4f1` görüntüsü yoksa ilk çalıştırma Unity'yi bulamaz. O durumda en yakın 6000.6 görüntüsü ya da macOS'ta doğrudan Unity kurulumu kullanılır. Henüz denenmedi.
- **Reklam SDK'sı:** iOS'ta App Tracking Transparency izni (`NSUserTrackingUsageDescription`) ve SKAdNetwork listesi gerekir; `IosPostBuild`'e eklenecek. Sahte reklamla yayın olmaz: ya gerçek SDK gelir ya reklamlı özellikler kapatılır.
- **`TestUnlockAll = false`.**
- **App Store sayfası:**
  - Ekran görüntüleri: iPhone 6,9" (1320×2868); iPad desteklendiği için iPad 13" (2064×2752) de gerekir. iPad'i istemiyorsan yalnız iPhone'a çeviririm.
  - Gizlilik etiketleri: Game Center kimliği ve adı, sıralama puanı, reklam SDK'sının topladıkları.
  - Yaş derecelendirmesi; gizlilik politikası adresi.
- **Sürüm:** `bundleVersion` 0.1.0. İlk mağaza sürümü için 1.0.0 önerilir.
- **iPhone'da deneme:** TestFlight ile.

## 6. Apple hesabı bilgileri (2026-11-09)

- Team ID: `W8BFR222TC` (gizli değil; GitHub'da `APPLE_TEAM_ID` olarak girilecek).
- App ID `com.ofigame.stoneburst` (marka StoneBurst). Eski `com.ofigame.trikat` App ID kullanılmayacak; App Store Connect kaydı ilk derleme yüklenmeden yeni kimliğe geçirilir.
- Dağıtım sertifikası CSR'si (2026-11-09): `E:\OFIGAME\Apple_Imza\stoneburst_dagitim.csr`. Özel anahtar aynı klasörde (`stoneburst_dagitim.key`). Proje ve depo dışında; silinmemeli ve paylaşılmamalı. Kaybolursa sertifika yeniden alınır.
- İmza (2026-11-09): Apple Distribution sertifikası (Firat Isler, W8BFR222TC, 2027-11-09'a kadar), `.p12` (parolası `p12_parola.txt`) ve base64 kopyası; App Store profili "Stone Burst App Store" (Game Center yetkili, 2027-11-09'a kadar) ve base64 kopyası. Hepsi `E:\OFIGAME\Apple_Imza\` içinde, depo dışında.
- App Store Connect API (kullanıcı, 2026-11-09: "aynı anahtar"): foi ile ortak "GitHub iOS" takım anahtarı (Admin). Issuer ID `a6ee2ad5-c997-4a9f-9fb4-7d5f49b9645f`, Key ID `8XKG85YHVR`, anahtar dosyası `E:OFIGAMEKeysAuthKey_8XKG85YHVR.p8` (içeriği gizli; GitHub `APPSTORE_P8`).
