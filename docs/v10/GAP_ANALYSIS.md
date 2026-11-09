# TriKat · Eksik analizi (v10 ana planı ↔ mevcut proje)

Tarih: 2026-11-06. Kanıtlar için: [AUDIT.md](AUDIT.md).

Durum etiketleri:
- **Var:** Planın istediği gibi çalışıyor.
- **Kısmi:** Bir kısmı var ya da yalnız yerel/sahte.
- **Yok**
- **Doğrulanamadı:** Telefon, mağaza ya da canlı servis olmadan bilinemez.
- **Farklı:** Var ama plandakinden farklı yapılmış; karar gerekir.

## 1. Kesinleşmiş 8 karar (plan §A)

| # | Karar | Durum | Not |
|---|---|---|---|
| 1 | Koleksiyon taşı kalıcı, gücü her gün yeniden kullanılır | **Kısmi** | Kalıcılık ve günlük kullanım var (`MetaSave.stones`, `StonePowers.DailyRoll`). Tek taş, tek güç. |
| 2 | Gizli hazine koleksiyona girer ve günlükte güç verir | **Yok** | Gizli hazine sistemi yok. |
| 3 | Günlüğe ödüllü reklamla giriş, bir ücretsiz tekrar | **Kısmi** | Kural var (`DailyService.RequestEntry`, `Grant`); reklam SAHTE (`FakeRewardedAds`). |
| 4 | Reklamsız paket sahibi reklamsız girer | **Kısmi** | Kural var (`IAdFreeEntitlement`); satın alma yok (`FakeAdFree`). |
| 5 | Yalnız en yüksek yıldız sayılır | **Var** | `ProgressRules`, `SaveService.RecordStars`, testli. |
| 6 | ZPE profilde ve sıralamada görünür | **Yok** | Ham veri modeli var (`SessionOutcome`), hesap, saklama ve ekran yok. |
| 7 | Misafir başlar, sıralama için hesap bağlar | **Yok** | Kimlik ve sıralama yok. Misafir oyun fiilen var (her şey yerel). |
| 8 | Temel sistemler ilk sürümde; Facebook sonra | — | Kapsam kararı. Facebook yok, uygun. |

## 2. Mevcut projenin korunması (plan §B, §0)

| Konu | Durum | Not |
|---|---|---|
| 1.000 bölüm korunuyor | **Var** | v9 boyunca dosya özetleri aynı kaldı (elle karşılaştırma). |
| Bölüm değişmezliği için otomatik test | **Var** | v10 P0.2: `ProductionLevelChecksumTests` (SHA-256, 1.000 dosya). |
| Sürümlü kayıt ve yükseltme testi | **Var** | `SaveData.CurrentVersion = 2`, `SaveServiceTests`. |
| Kayıt yedeği (backup) | **Var** | Dönen `.bak` (önceden vardı) + v10 P1.3: yükseltme öncesi kalıcı kopya ve `.corrupt` kopyası. |
| Paket kimliği `com.ofigame.trikat` | **Farklı** | Mevcut: `com.ofigame.katkat`. Mağaza durumu bilinmeden değiştirilmemeli. |
| Teknik adlar KatKat kalır | **Var** | Ad alanı, klasör ve kayıt dosyası (`katkat_save.json`) değişmedi. |

## 3. Bölümler, dünyalar, yıldızlar (plan §4)

| Özellik | Durum | Not |
|---|---|---|
| 10.000 bölüme kadar sabit kimlik | **Var** | Kimlik = dosya adı; sayısal sıralama 10.000'e hazır (`LevelCatalog`). |
| Veri odaklı yükleme, bütün haritayı birden kurmama | **Var** | Gerektiğinde okuma; haritada yalnız görünen düğümler kuruluyor (`MenuView.UpdateMap`). |
| 10.000 için dizin dosyası ya da paketli yükleme | **Yok** | Açılışta anlık 82 MB risk (bkz. AUDIT §8). |
| 500 bölümlük dünya × 20 | **Var (yalnız veri, P1.5)** | `WorldCatalog`: 500 bölüm = 5 bölge; 1.000 bölümde 2 dünya, 10.000'de 20. Harita görünümü aynı; dünya adları açık karar (#11). |
| Akdeniz Sahili / Çiçek Bahçesi temaları | **Farklı** | Sahil, bahçe, çöl temaları 100'lük bölgelerde dönüyor. Plan bunu öneri sayıyor. |
| Sayfalı harita | **Kısmi** | Sürekli kayan harita, bölge ‹ › düğmeleri; dünya seçimi yok. |
| En iyi yıldız toplamı, tekrarda yıldız biriktirmeme | **Var** | Testli. |
| 1 → 3 → 2 yıldız senaryosu testi | **Var** | v10 P1.4: `MetaTests`, `SaveServiceTests`. |
| Cihazlar arası eşitleme ve çevrimdışı yeniden bağlanma testi | **Yok** | Sunucu yok. |

## 4. Koleksiyon (plan §5)

| Özellik | Durum | Not |
|---|---|---|
| Veri odaklı katalog (kimlik, kategori, kazanım, güç) | **Var (P2.1)** | Şema 2: kategori (yıldız taşı, gizli hazine, efsanevi), nadirlik, kazanım türleri (bölüm, yıldız toplamı, hazine, dünya). Eski dosya geçerli. İçerik (22 taş) henüz yok. |
| 22 öğelik ilk sürüm içeriği | **Yok** | 1 taş var. |
| Yıldız eşiğiyle taş (250, 500, …) | **Farklı** | İlk taş "10. bölüm tamamlandı" kuralıyla veriliyor. `levelCompleted` dışında kazanım türü yok. |
| Gizli hazine (kırılan nesne, performans koşulu, tekrar oynayınca alınabilme) | **Yok** | Uygun zemin: içi boş tahtalardaki süs nesneleri (`BoardDecor`: maskot, fener, kaya, çeşme). Bunlar tahtanın dışında, kuralları değiştirmeyen bir katman; bölüm çözülebilirliğini bozmadan hazineye dönüşebilir. |
| Efsanevi taş (dünya tamamlama) | **Yok** | Dünya katmanına bağlı. |
| Kazanımın bir kez verilmesi | **Var** | `CollectionRules.GrantUnlocked`, testli. |
| Güç arayüzü ve güç aileleri (ateş, okyanus, fırtına, ay, yaşam, galaksi, kristal, rüzgâr, dönüşüm, yıldız) | **Kısmi** | Tek güç: `dailyRoll` (ruloyu doldurur; okyanus/satır ailesine yakın). Ortak güç arayüzü yok; güçler kod içinde tek tek tanımlı. |
| Gücün kural motoruna uyumu ve belirlenimli testi | **Kısmi** | `dailyRoll` testli (`MetaTests`). |
| Koleksiyon ekranı: ızgara, filtre, nadirlik, önizleme, seçme | **Kısmi** | Kaydırmalı 3 sütun ızgara, kategori başlıkları, sayaç, gerçek görseller (22). Filtre, nadirlik gösterimi ve seçme yok. |
| Gerçek taş görselleri ve 3B açılış gösterisi | **Yok** | Geçici simge. Görsel varlığı doğrulanamadı. |

## 5. Günlük bulmaca (plan §6)

| Özellik | Durum | Not |
|---|---|---|
| Ayrı katalog | **Var** | `Resources/Daily`. |
| Her gün yeni içerik | **Var** (P3.4) | 30 gün × 8 seviye grubu = 240 bulmaca; 30 günde bir döner. |
| Belirgin şekilde zor, ölçülmüş zorluk | **Var** (P3.4) | Oyuncu seviyesine göre grup; plansız bot kazanma oranı grup ortalaması %3,4–4,4. Gerçek oyuncu testi yok. |
| Çözülebilirlik | **Var** | Kayıtlı çözüm güç kullanmadan kazanıyor (LevelLab + `MetaTests`). |
| Ödüllü reklamla giriş, toplam 2 deneme, tekrar için reklam yok | **Kısmi** | Kural var, reklam sahte. |
| Reklam yoksa yedek yol | **Yok** | Karar gerekli (OPEN_DECISIONS #2). |
| Denemede sahip olunan taşlardan birini seçme | **Farklı** | Seçim ekranı yok; seçili taş (`meta.selectedStone`) ya da ilk taş kullanılıyor. |
| Taşın ilk deneme ile tekrar arasında değişmesi | **Yok** | Karar gerekli. |
| Kazanınca o gün oynanan normal bölümlere +1 hamle | **Farklı** | Yalnız henüz geçilmemiş bölümlere veriliyor (`DailyService.MoveBonusFor(levelAlreadyCompleted)`). |
| Ödülün sunucu gününe göre bitmesi, cihaz saatiyle oynanamaması | **Kısmi** | Gün sağlayıcısı soyut (`IDayClock`); şu an cihaz saati (`LocalDayClock`). |
| 7 günlük seri rozeti | **Yok** | İsteğe bağlı. |
| Sunucu: giriş, deneme ve ödülün tek seferlik ve hilesiz olması | **Yok** | İstemci tarafında tek seferlik kural var (`rewardLog`, çift geri çağrı koruması); sunucu yok. |
| Çevrimdışı davranış tanımı | **Kısmi** | Şu an tamamen çevrimdışı. Planın "ağ gerekebilir" seçeneği karar ister. |
| Günlük olayları (görüntüleme, reklam, giriş, deneme, taş, kazanma, ödül) | **Yok** | Analitik yok. |
| Bilerek çıkınca denemenin harcanması | — | Bizde var; planda geçmiyor (OPEN_DECISIONS #14). |

## 6. ZPE (plan §7)

| Özellik | Durum | Not |
|---|---|---|
| Ham girdiler ve yardım bayrakları | **Kısmi** | `SessionOutcome`, `AssistFlags`; yalnız bellekte. |
| Ham girdilerin hesap sürümü ve geçmişiyle saklanması | **Yok** | |
| Bölüm zorluğu ve referans hamle sayısı | **Kısmi** | Her bölümde kayıtlı çözüm uzunluğu ve hamle sınırı var (`LevelData.Solution`, `MoveLimit`). Kayıtlı çözüm en kısa çözüm olarak doğrulanmadı. LevelLab stres sonuçları (bot kazanma oranı) zorluk göstergesi olabilir. |
| Ağırlıklı hesap (35/25/20/10/10), ayarlanabilir | **Yok** | |
| "Kalibrasyon yapılıyor" (ilk 10–20 bölüm) | **Yok** | |
| Ana ekran, bölüm sonu ve profil gösterimi | **Yok** | |
| "IQ değildir" açıklaması ve gizlilik ayarı | **Yok** | |
| Yardımlı oyunun rekabeti bozmaması | **Kısmi** | Yardımlar işaretleniyor; hesapta kullanımı yok. |
| ZPE testleri | **Yok** | |

## 7. Kimlik ve sıralama (plan §8)

| Özellik | Durum |
|---|---|
| Misafir kimlik (kalıcı TriKat oyuncu kimliği) | **Yok** |
| Game Center / Play Games / Huawei hesap bağlama | **Yok** (Doğrulanamadı: Huawei için özel entegrasyon gerekebilir) |
| Hesap birleştirme, cihaz çakışması, kurtarma | **Yok** |
| Ortak sıralama (toplam yıldız), oyuncunun sırası ve çevresi | **Yok** |
| Takma ad, profil ekranı, görünürlük ayarı | **Yok** |
| Sunucu doğrulaması ve hileye karşı kayıt | **Yok** |

## 8. Gelir (plan §9)

| Özellik | Durum |
|---|---|
| Ana ekran, harita ve oyunda banner yok | **Var** (hiç reklam yok) |
| Geçiş reklamı kuralları (ilk 5 bölüm yok, 3–4 bölümde bir, en az 180 sn) | **Yok** |
| Ödüllü reklam (ek hamle, günlük giriş), doğrulanmış geri çağrı | **Kısmi** (yalnız günlük giriş, sahte) |
| Reklamsız tek seferlik satın alma (99,99 TL denemesi), geri yükleme, iade | **Yok** |
| Mağazalar arası hak politikası | **Yok** (karar) |
| Satın almanın yıldız, koleksiyon ya da sıralama vermemesi | — (ilke; uygulanacak sistem yok) |

## 9. Analitik ve canlı yönetim (plan §10, §10A)

| Özellik | Durum |
|---|---|
| Analitik arayüzü (`IGameAnalytics`) ve olay listesi | **Yok** |
| Firebase Analytics | **Yok** |
| Günlük/haftalık/aylık aktif oyuncu, geri dönüş ve bölüm terk panoları | **Yok** |
| Firebase bildirimleri (FCM), iOS bildirimleri (APNs), Huawei Push Kit | **Yok** (Doğrulanamadı: HMS-only cihaz gerekir) |
| Bildirim izni, ayarlarda bildirim aç/kapa, bildirimden doğrudan ekrana açılma | **Yok** |
| Yerel bildirim (günlük hatırlatıcı) | **Yok** |
| Çökme ve performans raporlama | **Yok** |
| Sunucu tarafı bildirim zamanlayıcısı | **Yok** |

## 10. Gizlilik, yayın, kalite (plan §10A, §11 P7, §F)

| Özellik | Durum |
|---|---|
| KVKK/GDPR, izin yönetimi, gizlilik metni | **Yok** |
| Hesap ve veri silme, veri dışa aktarma | **Yok** |
| Çocuk kullanıcı ve bölge kuralları | **Yok** |
| iOS derleme ve imza | **Yok** (Mac gerekir) |
| Huawei AppGallery derlemesi | **Yok** |
| Android imza anahtarı, mağaza bilgileri | **Yok** |
| `TestUnlockAll` kapalı | **Yok** (açık) |
| Otomatik testler | **Var** (CoreTests 133, EditMode 133, PlayMode 21) |
| Gerçek cihazda duman testi | **Yok** |
| Düşük donanımda bellek ve kare süresi ölçümü | **Yok** (editör ölçümü var) |
| Saat oynama, ağ kesintisi, hesap birleştirme, iade testleri | **Yok** |
| Kapalı beta ve telemetri incelemesi | **Yok** |
| Başarı hedefleri (ilk bölüm %90+, D1 %35+, çökmesiz oturum %99,5+) | **Doğrulanamadı** (analitik gerekir) |

## 11. Planın istediği belgeler (plan §D)

| Belge | Durum |
|---|---|
| `AUDIT.md` | Bu klasörde (`docs/v10/`); plan `docs/trikat/` diyordu, kullanıcı isteğiyle burada. |
| `GAP_ANALYSIS.md` | Bu belge. |
| `IMPLEMENTATION_BACKLOG.md` | [IMPLEMENTATION_BACKLOG.md](IMPLEMENTATION_BACKLOG.md) |
| `OPEN_DECISIONS.md` | [OPEN_DECISIONS.md](OPEN_DECISIONS.md) |

## 12. Başlıca riskler

1. **Paket kimliği:** Yayından sonra değiştirmek yeni uygulama demek. Karar yayından önce verilmeli.
2. **Huawei:** Huawei cihazlarında Google servisleri olmadığı için Firebase bildirimleri ve belki analitik çalışmaz. Gerçek HMS cihazında denemeden plan kesinleşmez.
3. **Gizli hazine:** Test edilmiş bölümlere nesne eklemek çözülebilirliği bozabilir. Kural dışı bir katman (`BoardDecor` benzeri) daha güvenli.
4. **Günlük içerik:** Tek bulmaca var. Her gün yeni ve zorluğu ölçülmüş içerik için bir üretim hattı gerekir (LevelLab ile mümkün).
5. **ZPE:** Referans hamle sayısı "en iyi" değil. Kalibrasyonsuz bir ZPE yanıltıcı olur ve "IQ" algısı yasal ve etik risk taşır.
6. **iOS:** Windows'ta imzalanamaz; Mac ve Apple geliştirici hesabı gerekir.
