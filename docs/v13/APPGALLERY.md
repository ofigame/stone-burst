# Huawei AppGallery sürümü

Kullanıcı isteği (2026-11-11): "oyunumuzu App Gallery'ye de yükleyelim", "App Gallery için reklamları, Google bağlılıklarını gözden geçir, App Gallery'ye uygun bir versiyon oluştur".

## Derleme

```
powershell -ExecutionPolicy Bypass -File tools/unity.ps1 build-android-appgallery
```

- Çıktı: `Builds/Android/StoneBurst-AppGallery-<sürüm>-<kod>.apk`.
- İmza: Play ile aynı yükleme anahtarı (`stoneburst-upload.jks`).
- Sürüm kodu `KATKAT_ANDROID_VERSION_CODE` ile verilir; her yüklemede artmalı.
- Kod: `Assets/KatKat/Editor/AppGalleryBuild.cs`, `BuildScripts.BuildAndroidAppGallery`.
- Derlemeden sonra bütün ayarlar geri alınır. Google Play derlemesi değişmez.

## Google Play sürümünden farkları

| Konu | Google Play | AppGallery |
|---|---|---|
| Reklam | AdMob (+ Unity Ads arabulucu), UMP rıza formu | Huawei Ads (`ads-lite 13.4.73.301`), kişiselleştirilmemiş reklam, rıza formu yok |
| Hesap / bulut kaydı | Play Games | Kapalı: "İlerlemeyi kaydet" satırı ve sıralama kayıt düğmesi gizli |
| Çevrimiçi sıralama | Firestore | Bağlı değil (yerel ilerleme görünür) |
| Analitik | Firebase | Yok |
| Tanımlar | `KATKAT_ADMOB;KATKAT_ONLINE` | `KATKAT_APPGALLERY` |

APK doğrulaması (2026-11-11):
- AdMob, Firebase, Play Games, UMP ve Unity Ads sınıfları yok. Manifest'te Google girişi yok.
- Huawei Ads ve `HwAdsBridge` içeride.
- Google Play APK'sında Huawei kodu yok.

## Huawei Ads

- Java köprüsü: `Assets/Plugins/Android/HwAdsBridge.java`. Yalnız AppGallery derlemesinde Android'e girer; meta varsayılanı kapalı.
- C# tarafı: `Scripts/Ads/HuaweiAdsNetwork.cs`.
- Reklam yoksa ödüllü reklam "bulunamadı" sonucunu döner ve kayıp kartı açık kalır. Geçiş reklamı atlanır.
- Kimlikler şimdilik Huawei TEST birimleri (`AdUnits.cs`): ödüllü `testx9dtjwj8hp`, geçiş `teste9ih9j0rc3`.
- Gerçek gelir için kullanıcının Huawei Ads Publisher Service hesabında Stone Burst için ödüllü ve geçiş birimleri açılmalı. Kimlikleri `AdUnits.cs`'ye yazılır.
- Huawei SDK'nın eklediği izinler: QUERY_ALL_PACKAGES, REQUEST_INSTALL_PACKAGES (uygulama yükleme reklamları için), ACCESS_WIFI_STATE ve diğerleri. Gizlilik beyanında belirtilmeli.

## AppGallery Connect

- 2026-11-11: "Stone Burst" projesi ve uygulaması oluşturuldu.
  - Uygulama kimliği 119766511, paket `com.ofigame.stoneburst`.
  - Kategori Oyun, varsayılan dil Türkçe.
- `agconnect-services.json` indirilmedi. Yalnız Huawei Ads (ads-lite) için gerekmiyor.

## Açık işler

1. Huawei Ads Publisher hesabı ve gerçek reklam birimleri (kullanıcı).
2. Mağaza bilgileri: açıklama, ekran görüntüleri, gizlilik politikası, içerik derecelendirmesi, ülkeler. Yükleme ve "incelemeye gönder" kullanıcı onayıyla.
3. Huawei cihazda deneme (HMS Core'lu telefon). Bu sürüm Huawei cihazda hiç denenmedi.
4. İleride: Huawei Account/Game Service ile hesap ve sıralama.

## Mağaza bilgileri (2026-11-12, Claude girdi, taslak kaydedildi)

- **Uyumlu cihaz:** cep telefonu.
- **Diller:** Türkçe (varsayılan), English (US), English (UK). Her dilde uygulama adı "Stone Burst".
- **Kategori:** Oyunlar → Bulmaca ve gündelik → Bulmaca; "Casual game".
- **Ülkeler:** 199 ülke. Çin anakarası hariç; orada oyun yayın onay numarası (ISBN) gerekiyor.
- **Ücret:** ücretsiz. Uygulama içi satın alma işaretlenmedi.
- **Gizlilik politikası:** `https://ofigame.com/legal/privacy`
- **Veri silme:** `https://ofigame.com/legal/data-deletion`
- **Uyarı:** AppGallery Connect veri silme adresine erişemediğini söyledi. Buradan iki adres de 200 dönüyor; büyük olasılıkla Huawei sunucularının erişim sorunu.

Kısa tanıtım (≤25 önerilir):
- tr: "Katları soy, zincir kur"
- en-US: "Peel layers, chain colors"
- en-GB: "Peel layers, make chains"

Tam tanıtım (tr):

> Stone Burst, katman katman taş yığınlarını soyduğun bir bulmaca oyunu.
>
> Bir yığına dokun: en üstteki taş ve ona bağlı aynı renk taşlar soyulur. Altından birlikte çıkan üç ya da daha fazla aynı renk taş kendiliğinden patlar ve zincir kurar. Hamlelerin sınırlı; hedef renkleri toplamak için doğru sırayı bulman gerekir.
>
> Neler var:
> • 1000 bölüm; her bölümün çözülebildiği doğrulandı
> • Farklı tahta türleri: iki ayrı alan, zincirle açılan mühürlü geçitler, aşamalı hedefler, geniş ve derin tahtalar
> • Özel taşlar: roket ve Altın Rulo
> • Her gün yeni bir günlük bulmaca
> • Toplanacak koleksiyon taşları ve Yolculuk haritası
> • Zekâ Performans Endeksi (ZPE): bölümleri ne kadar verimli çözdüğünü izle
> • Normal bölümler internet olmadan oynanır
>
> Stone Burst ücretsizdir. İsteğe bağlı ödüllü reklamlar (ek hamle, ipucu) ve bölüm aralarında reklam içerir.

İngilizce tam tanıtım aynı metnin çevirisi. en-GB'de "colour" yazımı kullanıldı.

### Kullanıcıya kalanlar

Yerleşik tarayıcı yerel dosya seçemiyor. Ayrıca beyanları kullanıcı işaretler.

1. **App information → Icon:** `docs/magaza/play/icon-512.png`.
2. **App information → Screenshots:** 3–8 adet. Kaynaklar:
   - Türkçe: `docs/magaza/android/tr/01..06`
   - İngilizce: `docs/magaza/android/en/01..06`
   - Görseller 1080×1920 ve her biri 1,5 MB'ın altında.
3. **Version → Manage packages:** APK `Builds/Android/StoneBurst-AppGallery-1.0.0-1.apk`. Önce gerçek Huawei reklam birimleriyle yeniden derlenmeli.
4. **İçerik derecelendirmesi:** anket.
5. **Privacy tags:** kişisel veri toplanıyor mu sorusu. Huawei Ads reklam kimliği kullanır.
6. **Yapay zeka beyanı.**
7. **Telif kanıtı (Proof of copyright):** zorunlu alan.
8. ~~Gizlilik politikası~~ **Yapıldı (2026-11-12, kullanıcı: "siteye ekle"):**
   - `ofigame.com/legal/privacy` (TR + EN): 1, 2, 3 ve 6. bölümlere Huawei AppGallery ekleri (`GIZLILIK_HUAWEI_TASLAK.md`).
   - `/legal/advertising`: Huawei Ads ortağı ve OAID satırı.
   - Canlı sayfalar doğrulandı; "Son güncelleme" tarihi kendiliğinden değişti.
   - Eski metinlerin yedeği arşivde: `site_gizlilik_2026-11-12`.
   - Kalan: app-ads.txt satırı (Huawei Ads hesabı açılınca).
9. **Submit** (incelemeye gönder).

## Unity Ads (doğrudan; 2026-11-12, kullanıcı: "app gallery içinde unity ads aktif olsun", "Doğrudan deneyelim")

- **Akış:** Önce Huawei Ads; Huawei'de hazır reklam yoksa Unity Ads; ikisinde de yoksa "reklam bulunamadı".
- **Kod:**
  - Unity tarafı: `HuaweiAdsNetwork.cs`.
  - Java köprüsü: `Assets/Plugins/Android/UnityAdsBridge.java`. Yalnız AppGallery derlemesinde açık.
  - Gradle'da `com.unity3d.ads:unity-ads` artık kalıyor.
- **Kimlikler:** Unity oyun kimliği `800434959` ("Stone Burst Android"), reklam yerleri `BP_Rewarded_Android` ve `BP_Interstitial_Android`. Bunlar Google Play'de AdMob'un kullandığı yerlerle aynı; Unity aynı Android uygulamasına ikinci uygulama açtırmıyor.
- **Rıza:** Unity'ye GDPR ve CCPA rızası "hayır" bildiriliyor (`gdpr.consent`, `privacy.consent` false), yani yalnız kişiselleştirilmemiş reklam.
- **Risk:**
  - Unity'nin yeni paneli "yalnız arabuluculuk kullananları destekliyor" ve yalnız teklifli (`BP_`) yer açıyor.
  - Doğrudan SDK ile bu yerlerden reklam gelip gelmediği belgelerden doğrulanamadı; Huawei cihazda denenmeli.
  - Gelmezse Huawei Ads tek başına çalışmaya devam eder.
- **APK doğrulaması:**
  - Unity Ads ve iki köprü içeride.
  - AdMob, Play Games, UMP ve arabulucu adaptörü yok.
  - Unity Ads kendi bağımlılığı olarak Google Play Hizmetleri'nin temel parçasını (`gms.common`, `GoogleApiActivity`) ve `AD_ID` iznini getiriyor. Bunlar GMS'siz cihazda çalışmaz ama oyunu durdurmaz.
- **Gizlilik politikası:** AppGallery bölümüne Unity Ads eklenmeli (sitede şu an "Huawei Ads" yazıyor).
