# AppGallery 1.0.3 — hazırlık notları

Kullanıcı (2026-11-20): mağazada ikon güncellenmemiş → "1.0.3 ile düzelt".

## Mağaza ikonu (zorunlu)
Durum (2026-11-20 kontrol):
- Mağaza sayfası (`appgallery.huawei.com/app/C119766511`, tr ve en_US) 1.0.1'in eski ikonunu gösteriyor: mavi ışınlı zeminde katmanlı kırmızı taş (`appimg-dre.dbankcdn.com/application/icon144/…/1d6fe854ae4a4a029524c0ea884f235b.png`).
- Huawei aynı sayfanın ekran görüntülerini 20 Kasım 09:16 GMT'de yenilemiş (yeni `android_sahne` görselleri); ikon dosyası da aynı saatte yeniden yazılmış ama içeriği hâlâ eski.
- AppGallery Connect → App information (1.0.2, onaylı, salt okunur): varsayılan dil en-US ikonu yeni v5 (216×216, yükleme 2026-11-18 19:xx).
- 1.0.2 APK'sındaki başlatıcı ikonu da v5 (uyarlanabilir: `app-icon-bg` + `app-icon-fg`).

Sonuç: eksik bizde değil, Huawei ikonu yayına almamış. 1.0.3 güncellemesinde:
1. App information → her dil için (en-US, en-GB, tr, ar, de, es-ES, es-419, fr, it, ja, ko) ikonu sil, `docs/varliklar/magaza/appgallery/icon-216.png` (v5) yeniden yükle. Dil açılır listesi koddan açılmıyor, gerçek tıklama gerekir (bkz. bellek appgallery-upload).
2. Kaydet, sonra sayfayı yenileyip her dilde yeni ikonun göründüğünü kontrol et.
3. Onaydan sonra mağaza sayfasındaki `icon144` görselini indirip v5 olduğunu doğrula. Hâlâ eskiyse Huawei'ye destek talebi aç (kullanıcı onayıyla).

APK'ya klasik PNG ikon eklenmedi: Unity 6.6 Android'de yalnız uyarlanabilir ikon türünü destekliyor, minSdk 26 olduğu için her cihaz zaten uyarlanabilir ikonu kullanıyor. Mağaza ikonu Connect'te yüklenen dosyadan geliyor.

## 1.0.3'ün içeriği (henüz derlenmedi)
- Mağaza, külçe, güçlendiriciler (gerçek para satın alma kapalı), öğretici tek bölüm, 2–7. bölümler iki katlı, ipucusuz alt şerit, oyuncu adını kendisi yazar, sıralama puanı = yıldız + ZPE.
- Derleme: `KATKAT_ANDROID_VERSION_CODE=4 KATKAT_ANDROID_VERSION_NAME=1.0.3 tools/unity.ps1 build-android-appgallery`.
