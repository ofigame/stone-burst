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
