# TriKat · Uygulama iş listesi (P0–P7)

Tarih: 2026-11-06. Kaynak: v10 ana planı §11. Durum: [GAP_ANALYSIS.md](GAP_ANALYSIS.md). Kararlar: [OPEN_DECISIONS.md](OPEN_DECISIONS.md).

Kurallar:
- Süre tahmini yok; sıra bağımlılığa göre.
- Her görev küçük ve testli.
- Her görev raporu şunları içerir: değişen dosyalar, koşulan ve koşulamayan testler, 1.000 bölümün ve kayıt dosyasının etkilenip etkilenmediği, bir sonraki küçük adım.
- Etiketler:
  - **[yerel]** canlı servis olmadan yapılabilir
  - **[karar]** önce OPEN_DECISIONS'ta karar gerekir
  - **[hesap]** gerçek hesap ya da anahtar gerekir
  - **[cihaz]** gerçek cihaz gerekir
- ✔ = v9'da yapıldı (yeniden yapılmayacak).

## P0 · Denetim ve temel çizgi

| # | Görev | Giriş noktası | Kabul |
|---|---|---|---|
| P0.1 ✔ | Salt okunur denetim, eksik matrisi | `docs/v10/AUDIT.md`, `GAP_ANALYSIS.md` | Bu belgeler. |
| P0.2 ✔ | **Bölüm değişmezlik testi:** 1.000 JSON'un SHA-256 özet listesi `Tests/EditMode/Data/levels_v7.sha256` dosyasına yazılır; test her koşuda karşılaştırır. | Yeni `ProductionLevelChecksumTests` (EditMode + CoreTests) | Bir bölüm baytı değişirse test düşer; bilinçli güncelleme yalnız yeni özet dosyasıyla yapılır. |
| P0.3 [yerel] ✔ | Kayıt dosyası örnekleri: v1 ve v2 örnek kayıtları test verisi olarak eklenir; yükseltme bunlar üstünden test edilir. | `Tests/PlayMode/SaveServiceTests` | Örnek kayıtlar her sürümde kayıpsız açılır. |
| P0.4 [yerel] | **Sunucu ve servis araştırması** (kod yok): hazır servis seçenekleri (ör. PlayFab, Firebase+Cloud Functions, Nakama, Supabase) ve kendi sunucumuz. Huawei, kimlik doğrulama, maliyet ve güvenlik açısından karşılaştırılır. | `docs/v10/BACKEND_ARASTIRMA.md` | Öneri ve gerekçe; karar kullanıcıda (OPEN_DECISIONS #8). |
| P0.5 [karar] | Paket kimliği kararı (`com.ofigame.katkat` → `com.ofigame.trikat`?) | `ProjectSettings.asset` | Mağaza durumu doğrulandıktan sonra tek seferlik değişiklik ya da bilinçli koruma. |

## P1 · Temel ve kalıcılık

| # | Görev | Giriş noktası | Kabul |
|---|---|---|---|
| P1.1 ✔ | Sürümlü kayıt ve güvenli yükseltme | `SaveData`, `SaveService.Upgrade` | Testli. |
| P1.2 ✔ | En iyi yıldız toplamı, kazanımın bir kez verilmesi | `ProgressRules`, `CollectionRules` | Testli. |
| P1.3 ✔ | Yükseltme öncesi kalıcı kopya (`katkat_save.v1.json`) ve bozuk dosyanın kopyası (`.corrupt`). Dönen `.bak` ve yedekten dönüş zaten vardı. | `SaveService.Load` | `SaveServiceTests`: kopya bir kez ve değişmeden; bozuk dosya saklanır; yedekten devam yazılır. |
| P1.4 ✔ | 1 → 3 → 2 yıldız testi; çevrimdışı tekrar oynayınca toplamın değişmemesi | `MetaTests` | Planın istediği senaryo testli. |
| P1.5 [yerel] ✔ | **Dünya katmanı (yalnız veri):** `WorldDefinition` (dünya kimliği, bölüm aralığı 500, tema anahtarları). Mevcut 100'lük bölgeler ve temalar aynen kalır; dünya yalnız gruplama olur. | `View/ThemeDefinition.cs` yanına yeni sınıf | `RegionSize` 100 kalır; harita görünümü değişmez (görsel karşılaştırma %0). |
| P1.6 [yerel] ✔ | 10.000'e hazırlık: bölüm dizin dosyası (yapıldı: `Resources/LevelIndex/levels_index.json`, `LevelLab index` ile üretilir; `Levels` klasörü dışında, çünkü `report` o klasördeki her JSON'u bölüm sayar); `LevelCatalog` önce dizini okur | `LevelCatalog.Load`, `tools/LevelLab` | Açılışta `LoadAll` yok; 1.000 bölüm özeti aynı; ölçüm testi anlık belleği gösterir. |

## P2 · Koleksiyon ve gizli hazine

| # | Görev | Giriş noktası | Kabul |
|---|---|---|---|
| P2.1 [yerel] ✔ | Katalog şemasını genişletme: `category` (StarStone/HiddenTreasure/LegendaryStone), `rarity`, kazanım türleri (`starsTotal`, `levelCompleted`, `treasure`, `worldCompleted`). Eski `stones.json` geçerli kalır. | `Levels/Meta/Stones.cs` | Eski ve yeni şema testli. |
| P2.2 [karar] (kısmi) | **Uygulandı (geçici):** Gelgit 10. bölümde kalır; diğer 9 yıldız taşı 250, 500, …, 2.250 yıldızda (`stones.json`). Eşiklerin onayı bekliyor. Yıldız eşiğiyle taş kazanımı (250, 500, …): `stone_tide` kuralının yıldız eşiğine taşınıp taşınmayacağı | `stones.json`, `CollectionRules` | Eski oyuncunun taşı korunur (kazanılmış taş geri alınmaz). |
| P2.3 [karar][yerel] | **Gizli hazine:** bölümün kuralını değiştirmeyen bir katman. Tahta dışındaki süs nesnesi (`BoardDecor`) bir performans koşuluyla kırılır ve hazineyi verir. Koşul (ör. hamle payı, ilk deneme) yapılandırmada. Bölüm dosyasına dokunulmaz; hazine listesi `Resources/Collection/treasures.json`. | `View/BoardDecor.cs`, `GameController.OnLevelWon` | 10 hazine bölümünde kayıtlı çözüm aynen kazanır; hazine bir kez verilir; kaçırılan hazine tekrar oynayınca alınabilir. |
| P2.4 [karar] | Efsanevi taş: dünya tamamlama ya da ustalık koşulu | P1.5'e bağlı | Kural yapılandırmada; testli. |
| P2.5 [yerel] (kısmi) | **Yapıldı:** kaydırmalı 3 sütun ızgara, kategori başlıkları, sayaç, kilitli/gizli görünüm. **Kalan:** filtre, nadirlik gösterimi, seçme, 220 öğe ölçümü. Koleksiyon ekranı: ızgara, kategori filtresi, kilitli/açık görünüm, kazanım koşulu, nadirlik, seçme | `MenuView.ShowCollection` | 22 öğe ve 220 öğe (sanal) ekranda akıcı; görsel karşılaştırma. |
| P2.6 [hesap/varlık] (kısmi) | **Yapıldı:** 22 PNG oyunda (512 px). **Kalan:** açılış gösterisi, 3D modeller (9 hazır, 13 sonra; mobil için sadeleştirilmeli). 22 öğenin gerçek görselleri ve açılış gösterisi | `artKey` eşlemesi | Görseller gelince yalnız eşleme değişir. |

## P3 · Günlük bulmaca ve güçler

| # | Görev | Giriş noktası | Kabul |
|---|---|---|---|
| P3.1 ✔ | Giriş, iki deneme, günde bir ödül, çift geri çağrı koruması (yerel) | `DailyService` | Testli. |
| P3.2 ✔ (kısmi) | **Yapıldı:** `IStonePower` + kayıtlı aileler: dailyRoll (gelgit), ocean (tepsiye satır roketi), storm (sütun roketi), galaxy (Altın Rulo), moon (+2 hamle). Kural motoruna dokunulmadı; testler `StonePowerTests`. **Kalan:** ateş, yaşam, kristal, rüzgâr, dönüşüm, yıldız (kural motorunda karşılığı yok, ayrı karar); rulo dışı güçler için günlük ekranında güç düğmesi (yeni taşlar eklenince). Önceki plan: **Ortak güç arayüzü:** `IStonePower` (CanApply/Apply, belirlenimli); `dailyRoll` buna taşınır. Sonra ilk aileler: okyanus (satır), fırtına (sütun), ay (+hamle), ateş (alan). | `Levels/Meta/Stones.cs` (`StonePowers`), `RulesEngine` | Her güç kural motoruyla testli; geçersiz hedef hak harcamaz. |
| P3.3 [karar][yerel] | Denemede taş seçme ekranı; ilk deneme ile tekrar arasında değişim politikası | `MenuView.ShowDaily`, `DailyService` | Seçim kayda geçer; politika yapılandırmada. |
| P3.4 [yerel] ✔ | **Günlük içerik hattı:** LevelLab'e `daily` komutu (aday üretme + çözülebilirlik + zorluk ölçümü: bot kazanma oranı, en kısa çözüm/limit payı); 30–90 günlük katalog | `tools/LevelLab`, `Resources/Daily` | Her bulmaca güçsüz çözülebilir; zorluk eşiği raporlanır. |
| P3.5 [karar] | +1 hamlenin kapsamı: "o gün oynanan her normal bölüm" mü, "geçilmemiş bölüm" mü | `DailyService.MoveBonusFor` | Karara göre testli. |
| P3.6 [karar] | Reklam yoksa yedek giriş yolu | `DailyService.RequestEntry` | Politika yapılandırmada; testli. |
| P3.7 [yerel] | 7 günlük seri rozeti (cezasız) | `MetaSave` | İsteğe bağlı. |
| P3.8 [hesap] | Sunucu gününe geçiş (`IDayClock` sunucu uygulaması), giriş, deneme ve ödülün sunucuda tek seferlik olması | P0.4 kararına bağlı | Saat değiştirme ve iki cihaz testleri. |

## P4 · Kimlik, sunucu, sıralama

| # | Görev | Kabul |
|---|---|---|
| P4.1 [hesap] | Misafir oyuncu kimliği (sunucuda kalıcı), yerel kayda bağlanması | Yeniden kurulumda misafir ilerlemesi kurtarma politikası tanımlı. |
| P4.2 [hesap][cihaz] | Google Play Games, Game Center, Huawei hesap bağlama | Gerçek cihazda bağlama ve geri yükleme. |
| P4.3 [hesap] | Bulut kaydı ve birleştirme (bölüm başına en yüksek yıldız; kazanımlar kimlikle tek) | İki cihaz senaryosu; bağlama hatası yerel ilerlemeyi silmez. |
| P4.4 [hesap] | Toplam yıldız sıralaması: oyuncunun sırası ve çevresi, sayfalama | Sunucu puanı yeniden hesaplar; istemcinin gönderdiği toplam doğrudan yazılmaz. |
| P4.5 [yerel] | Profil ekranı (takma ad, yıldız, koleksiyon; ZPE sonra) | Görsel karşılaştırma. |

## P5 · ZPE

| # | Görev | Kabul |
|---|---|---|
| P5.1 [yerel] | Oturum sonuçlarının kalıcı saklanması (sürüm, zaman, yardım bayrakları; son N oturum) | Kayıt boyutu sınırlı; yükseltme testli. |
| P5.2 [yerel] | Bölüm referans verisi: kayıtlı çözüm uzunluğu, hamle sınırı, LevelLab bot kazanma oranı (zorluk) → `level_ref.json` | 1.000 bölüm dosyasına dokunulmaz. |
| P5.3 [yerel] | ZPE hesap modülü (ağırlıklar yapılandırmada, sürümlü), kalibrasyon durumu, son 30–50 bölüm ağırlığı | Kolay/zor, tekrar, hız, yardım ve eksik veri testleri. |
| P5.4 [yerel] | Gösterim: ana ekran, bölüm sonu değişimi, profil; "IQ değildir" açıklaması | Metin 9 dilde; loc-check. |
| P5.5 [hesap] | Herkese açık ZPE ve sıralama | Yalnız adalet ve gizlilik kontrolünden sonra. |

## P6 · Reklam, satın alma, analitik

| # | Görev | Kabul |
|---|---|---|
| P6.1 [karar][hesap] | Reklam ağı seçimi ve ödüllü reklam adaptörü (`IRewardedAds` gerçek uygulaması) | Gerçek cihazda iptal, hata ve çift geri çağrı. |
| P6.2 [hesap] | Geçiş reklamı kuralları, uzaktan ayar (ilk 5 bölüm yok, 3–4 bölüm, 180 sn) | Oyun sırasında asla çıkmaz. |
| P6.3 [hesap][cihaz] | Reklamsız satın alma: Google Play Billing, App Store, Huawei IAP; doğrulama, geri yükleme, iade | Her mağazada gerçek test hesabıyla. |
| P6.4 [yerel] | `IGameAnalytics` arayüzü, olay listesi ve şema sürümü; çevrimdışı tampon; yerel günlük uygulaması | Oyun kodu yalnız arayüzü çağırır; olaylar testli. |
| P6.5 [yerel] | İzin ve gizlilik: ilk açılış izin ekranı, ayarlarda aç/kapa, gizlilik metni bağlantısı | 9 dilde metin; izin yokken analitik kapalı. |

## P6A · Firebase analitik ve bildirimler

| # | Görev | Kabul |
|---|---|---|
| P6A.1 [hesap] | Firebase projeleri (geliştirme/canlı), Unity SDK sürüm uyumu | Sürümler belgelendi; gizli anahtar istemcide yok. |
| P6A.2 [hesap][cihaz] | Firebase Analytics, `IGameAnalytics` arkasında; aktif oyuncu ve geri dönüş panosu test kullanıcılarıyla | Raporlama gecikmesi belgelenmiş. |
| P6A.3 [cihaz] | Huawei HMS-only cihazda bildirim ve analitik denemesi; gerekirse Huawei Push Kit | Sonuç belgelendi. |
| P6A.4 [yerel] | Yerel bildirim: isteğe bağlı günlük hatırlatıcı, saat seçimi, sessiz saatler | İzin vermeyene hiçbir şey gitmez. |
| P6A.5 [hesap] | Uzaktan bildirim, bildirimden doğrudan günlük bulmaca, koleksiyon ya da harita ekranına açılma; sunucu zamanlayıcı | Ön planda, arka planda ve kapalıyken açılış testleri. |

## P7 · Kalite ve yayın

| # | Görev | Kabul |
|---|---|---|
| P7.1 [yerel] | `TestUnlockAll = false`, yayın derleme yapılandırması | Yayın derlemesinde test anahtarları kapalı. |
| P7.2 [hesap] | Android imza anahtarı, AAB derlemesi, Play Console iç test kanalı | İç testte kurulum. |
| P7.3 [cihaz] | Düşük donanımlı Android ve Huawei cihazlarda bellek, kare süresi ve güvenli alan testleri | Ölçümler raporlanır. |
| P7.4 [hesap][cihaz] | iOS derlemesi (Mac, Apple geliştirici hesabı) | TestFlight. |
| P7.5 [cihaz] | Ağ kesintisi, saat oynama, hesap birleştirme, iade, güncelleme testleri | Senaryo listesi geçer. |
| P7.6 | Mağaza bilgileri, gizlilik beyanları, kapalı beta | Yayın kontrol listesi onaylı. |

## Önerilen ilk küçük ve güvenli görev

**P0.2: Bölüm değişmezlik testi.**
- Kod ve oyun davranışı değişmez.
- 1.000 bölümün korunduğunu her test koşusunda otomatik kanıtlar.
- Sonraki bütün işlerin (hazine katmanı, dizin dosyası, dünya katmanı) güvenlik ağı olur.

Ardından:
- **P0.4:** Sunucu araştırması; kod yok, yalnız rapor.
- **P1.3:** Kayıt yedeği.
