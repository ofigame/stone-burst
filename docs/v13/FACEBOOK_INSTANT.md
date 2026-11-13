# Facebook Instant Games — yol haritası (2026-11-13)

Karar: oyun Facebook **içinde oynanacak** (Instant Games). Henüz başlanmadı; bu belge ön incelemedir.

## Platform şartları (araştırma, 2026-11-13)
- Meta resmî Unity eklentisi: `meta/meta-instant-games-unity-plugin` (Instant Games SDK v8.0, Unity 2022+, WebGL hedefi).
  Oyuncu kimliği/veri saklama (`FBInstant.Player`), ödeme (`FBInstant.Payment`), reklam, turnuva API'leri var.
- Boyut: toplam üst sınır 200 MB; **ilk indirme önerisi < 5 MB (ideal < 3 MB)**.
- Risk (doğrulanmadı, forum): Unity 6.2 WebGL 2-yalnız derleme Meta incelemesinde bazı test cihazlarında açılmadığı için reddedilmiş.
  Bizim Unity 6000.6.4f1; Meta'nın şu anki tutumu kontrol edilmeli.

## Bizim oyunda durum
| Konu | Durum | Gereken iş |
|---|---|---|
| Unity WebGL modülü | **Kurulu değil** | Unity Hub → 6000.6.4f1 → WebGL Build Support ekle |
| Resources klasörü | **~204 MB** (PremiumV6 71, PremiumV4 40, Models 35, Collection 13, Levels 12, UI3 11 MB…) | Resources'taki her şey ilk indirmeye girer. Addressables ile parça parça yükleme + doku sıkıştırma; kullanılmayan premium setler web'de çıkarılmalı |
| İpucu planlayıcı | `GameController.Hint.cs:144` `Task.Run` ile arka iş parçacığında | WebGL'de iş parçacığı yok → ana iş parçacığında kare kare (coroutine) çalışacak sürüm |
| Reklam | AdMob (`KATKAT_ADMOB`) | Web'de yok → `IAdNetwork` için FBInstant reklam uygulaması |
| Hesap/kayıt/sıralama | Firebase + Play Games / Game Center (`KATKAT_ONLINE`) | Web'de kapalı; FBInstant.Player (bulut kayıt) + kendi sıralama/turnuva |
| Titreşim | `Haptics.cs` | Web'de etkisiz; satır gizlenmeli |
| Hesap silme / gizlilik | Var | Meta için gizlilik URL'si + veri silme yolu yeterli olmalı |
| Kayıt (SaveService) | Dosya/PlayerPrefs | WebGL'de IndexedDB üstünden çalışır; FB kaydına da yazılmalı |

## Önerilen sıra
1. WebGL modülünü kur, **ayrı bir kopya/çalışma ağacında** deneme WebGL derlemesi al (ana projenin hedef platformunu değiştirmeden). Boyut ve açılış süresini ölç.
2. `KATKAT_FBINSTANT` tanımı ile web derlemesi: AdMob/Firebase/Play Games kapalı, ipucu planlayıcı tek iş parçacıklı.
3. Varlık küçültme: Addressables, doku sıkıştırma, premium setleri sonradan yükleme → ilk indirme < 5 MB hedefi.
4. Meta eklentisini ekle: oyuncu, kayıt, reklam, (sonra) ödeme.
5. Meta for Developers'ta Instant Game uygulaması aç (kullanıcı), test sürümü yükle, cihazlarda dene, incelemeye gönder (onayla).

Tahmin: birkaç haftalık iş; en büyük belirsizlik varlık boyutu ve WebGL 1/2 inceleme riski. Ölçülmedi.
