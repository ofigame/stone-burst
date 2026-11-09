# TriKat v9 · A ve B paketleri raporu

Tarih: 2026-11-06. Kapsam: `KAT_KAT_CLAUDE_IMPLEMENTATION_ROADMAP.md` A ve B. C ve sonrası yapılmadı. Canlı servis, mağaza yayını ya da gerçek anahtar kullanılmadı.

## A · Mevcut davranışın doğrulanması ve kayıt temeli

### Başlangıç doğrulaması

- Ana katalog: 1000 bölüm (`Resources/Levels/level_001…1000.json`).
- Başlangıç testleri: CoreTests 117/117, EditMode 117/117, PlayMode 16/16 (aynı gün, değişiklik öncesi). Başlangıçta hata yoktu.
- Değişiklik öncesinde 1000 bölüm dosyasının özeti alındı. Değişiklik sonrası özetler birebir aynı (fark 0).

### Yapılanlar

- **Yıldız toplamı.** `ProgressRules` (`Scripts/Levels/Meta/ProgressRules.cs`):
  - Toplam, bölüm kimliği başına en yüksek geçerli (1–3) yıldızdan hesaplanır.
  - `SaveService.RecordStars` aynı davranışı korur, ek olarak artışı döndürür. 3 → 1 → 3 toplamı yalnız 3 artırır; 1 → 3 artışı 2'dir.
- **Sürümlü kayıt.** `SaveData.CurrentVersion = 2`.
  - Yükseltme v1 → v2: yıldız listeleri normalize edilir. Yinelenen kimlikte en yüksek değer kalır; 1–3 dışı ve boş kimlik atılır.
  - Yeni `meta` bölümü eklenir: taşlar, seçili taş, açılmış bölümler, günlük durum, ödül günlüğü, +1 hamle günü.
  - Yükseltme bir kez yazılır (`upgradedFrom`). İkinci açılışta kayıt zaten v2 olduğu için tekrar uygulanmaz.
  - İlerleme, ses ve titreşim ayarları korunur.
- **Oturum sonucu modeli.** `SessionOutcome` + `AssistFlags` (`Scripts/Levels/Meta/SessionOutcome.cs`):
  - Ayrıştırılan alanlar: oturum türü (Normal/Daily/Custom), bölüm, içerik sürümü (`levels-v7` / `daily-v1`), taban limit + ek hamle, kullanılan hamle (kutlama hamleleri yemeden önce), ilk deneme, deneme sırası, başarı.
  - Yardımlar ayrı bayraklarda tutulur: geri alma, günlük +1 hamle, taş gücü, öğretici/ipucu; ödüllü reklam için yer ayrıldı.
  - Etkin süre ölçülür (menü, kart açıkken ve arka planda sayılmaz).
  - `GameController.OutcomeRecorded` olayıyla yayınlanır. ZPE hesaplanmaz.

### A testleri

- `MetaTests` (EditMode + CoreTests): 3-1-3 senaryosu, 1→3 senaryosu, geçersiz değer ve normalize.
- `SaveServiceTests` (PlayMode):
  - v1 kaydı yükseltilir; ilerleme ve ayarlar korunur.
  - İkinci açılışta dosya yeniden yazılmaz.
  - `RecordStars` artışları doğru.

## B · Koleksiyon ve günlük bulmaca (yerel prototip)

### B1 · İlk koleksiyon taşı

- **İçerik yapılandırması.** `Resources/Collection/stones.json`. Tek taş:
  - kimlik `stone_tide` ("Gelgit Taşı")
  - `artKey: collection/stone_tide` (görsel yok → ortak yer tutucu: mevcut turkuaz renk taşı simgesi)
  - `powerId: dailyRoll`
  - kazanım `levelCompleted: 10`
- **Eşik: 10. bölüm.** Yönlendirmeli öğretici turları 1–9. bölümlerde. Bu, öğretici sonrası ilk uygun bölüm ve yerel test eşiğidir; yayın kuralı değildir.
- **Kazanım tek seferliktir.** Kayıtta kalır; eski kayıtta koşul sağlanmışsa ilk açılışta verilir.
- **Güç ailesi `dailyRoll`.** Günlük bulmacada boş rulo yuvasını bir kez doldurur, oyuncu mevcut satır rulosunu kurallara göre kullanır.
  - Yalnız rulosu açık düzende ve hedefi olan bir satır varken geçerlidir.
  - Normal bölümlere taşınmadı.
- **Erişim.** Günlük oyun ekranında boş rulo düğmesine dokunmak gücü kullanır.
  - Günlük bulmacada tepsi de olduğu için rulo düğmesi tepsinin sağındaki boşluğa alınır (`HudView.PlaceRollBesideTray`).
  - Normal bölümlerin yerleşimi değişmedi.

### B2 · Günlük oturum

- **Ayrı katalog.** `Resources/Daily/daily_index.json` + `daily_001.json`.
  - Kaynak `daily_src/daily_001.txt`: `level_918`'in yatay aynası, renk döngüsü ve rulo açık.
  - Kayıtlı çözüm güç kullanmadan kazanıyor (LevelLab analyze + EditMode testi). Ana 1000 dosyaya dokunulmadı.
- **Durum anahtarı.** `dayId|challengeId|contentVersion`.
  - Giriş bir ilk deneme ve bir ücretsiz tekrar açar.
  - Ek reklamla üçüncü deneme kapalı (`DailyConfig.AdExtraAttempts = 0`).
- **SAHTE reklam ve reklamsız hak.** `FakeRewardedAds`, `FakeAdFree` (ekranda "Prototip: reklam ve gün bilgisi sahte/yerel" yazar).
  - Reklam iptali ya da hatası hakkı eksiltmez.
  - Aynı ödül geri çağrısı iki kez gelirse ya da ödül gün değiştikten sonra gelirse yeni hak oluşmaz.
- **Hak, kalıcı oturum kaydı açılınca harcanır.**
  - Uygulama kapanırsa aynı deneme yeniden açılır ve hak tekrar harcanmaz.
  - Oyuncu bilerek çıkarsa (harita, kayıp kartında "Çık") deneme biter. Bu, yeniden girerek bedava yeniden başlatmayı engeller.
- **Taş gücü.** Deneme başına bir kez. Doğrulama ve harcama aynı işlemde; geçersiz hedef hakkı tüketmez.
- **Başarı ödülü.**
  - Gün başına bir kez `rewardLog`'a yazılır.
  - O gün açılan, henüz kazanılmamış normal bölüm oturumlarına +1 hamle eklenir. Hamle oturum açılırken eklenir; aynı oturumda yeniden başlatmada limit değişmez.
  - Gün değişince yeni oturumlara taşınmaz. Geçmiş yıldızlar değişmez.
  - Sonuç `DailyMoveBonus` ile yardımlı işaretlenir.
- **Gün sağlayıcısı.** `LocalDayClock` (YEREL; canlıda sunucu), testlerde `FixedDayClock`.
- **Normal ilerleme ayrılığı.** Günlük sonucu tamamlanan bölümlere ve yıldızlara yazılmaz.

### B testleri

- `MetaTests` (EditMode + CoreTests):
  - giriş + ücretsiz tekrar + üçüncü deneme yok
  - reklam iptali/hatası
  - çift geri çağrı
  - gün değişiminden sonra gelen ödül
  - reklamsız giriş
  - kapanma sonrası aynı deneme
  - ödül gün başına bir kez ve gün sınırı
  - taş tek seferlik
  - güç: taşsız, denemesiz, geçersiz hedef, bir kez, tekrarla yenilenme, rulosuz düzen
  - içerik dosyaları
- `DailyFlowTests` (PlayMode, gerçek `GameController`), kabul akışı uçtan uca:
  - eski kayıt → taş → iptal edilen reklam → giriş → ilk deneme
  - sahne yeniden yükleme (kapanma) → aynı deneme → güç bir kez → ücretsiz tekrar
  - kayıtlı çözümle başarı → normal bölümde +1, yeniden başlatmada sabit, kazanılmış bölümde yok → gün değişimi
- İkinci test: bilerek çıkış denemeyi harcar.

### Görsel regresyon

- `tools/unity.ps1 capture-v9-before` / `capture-v9-after`:
  - ekranlar: ana, harita, 38. bölüm oyun ekranı, 38. bölüm sonuç kartı
  - boyutlar: 1080×1920 ve çentikli 1080×2400
- Sekiz karede fark %0 (`docs/captures/v9/KARSILASTIRMA.md`).
- Yeni ekranlar `docs/captures/v9/yeni/`.

## Test sonuçları (2026-11-06, değişiklik sonrası)

| Koşu | Sonuç |
|---|---|
| CoreTests (dotnet) | 133/133 (117 eski + 16 yeni) |
| EditMode (Unity) | 133/133 |
| PlayMode (Unity) | 20/20: 18 test tam koşuda geçti (1000 bölüm akışı dahil, 51 dk). `DailyFlowTests` ilk koşuda testin kendi hatası yüzünden (sahne yeniden yüklenince eski kayıt nesnesine bakıyordu) düştü, düzeltildi; 2/2 ayrıca yeniden koşuldu ve geçti. |
| Görsel regresyon | 8/8 kare fark %0 (harita girişi kapalıyken). Giriş açıldıktan sonra: oyun ve sonuç 4/4 %0; ana ve harita 4 karede yalnız iki yeni düğme (%0,6–0,8). |
| Dil denetimi | TAMAM (9 dil, 160 anahtar; fontta olmayan iki Japonca karakter değiştirildi) |
| Ana bölüm dosyaları | 1000/1000 özet aynı |

## Açık ürün kararları

1. ~~Günlük/Koleksiyon girişi haritada nerede?~~ **Karar verildi (2026-11-06):** önce logonun iki yanında düğmeler denendi; kullanıcı sonra alt menüye taşıttı: alt menü Harita · Günlük · Taşlar, Oyna kalktı, Ayarlar sağ üstte (görüntü `docs/captures/v9/yeni/harita_giris*.png`). Ana/harita karelerinde beklenen fark (alt menü, dişli, yeni logo).
2. **Günlük reset saati ve saat dilimi** (sunucu kaynağı). Prototip: cihazın yerel gece yarısı.
3. **Bilerek çıkış denemeyi harcasın mı?** Prototip: evet (bedava yeniden başlatmayı önler). Uygulamayı kapatıp açmak hâlâ aynı denemeyi baştan açar; C paketinde sunucu oturumu ile ele alınmalı.
4. **Ücretsiz tekrar sonrası reklamla ek deneme.** Prototip: kapalı.
5. **Taş gücü deneme başına bir kez** ve güç listesi/dengesi.
6. **Günlük bulmacanın "çok zor" ayarı.** daily_001 bölüm 918'den türetildi (kaynakta plansız bot %8, rulo kapalı). Rulo açılınca kolaylaşmış olabilir; günlük için zorluk ölçümü yapılmadı.
7. **İlk taşın kesin bölümü** (prototip 10) ve 22 taşlık ilk sürüm içeriği.

## Geçici prototip değerleri

| Değer | Prototip |
|---|---|
| İlk taş eşiği | 10. bölüm |
| Ücretsiz tekrar | 1 |
| Reklamla ek deneme | 0 (kapalı) |
| Güç kullanımı | deneme başına 1 |
| Başarı ödülü | +1 hamle, aynı gün, kazanılmamış normal bölümler |
| Gün | yerel saat, sıfırlama 00:00 |
| Reklam / reklamsız hak | SAHTE adaptör (her zaman başarılı; testte iptal/hata/çift geri çağrı) |
| Taş görseli | mevcut turkuaz renk taşı simgesi (yer tutucu) |

## Doğrulama durumu

- Üretim bölüm dosyalarında değişiklik yok (1000/1000 özet aynı).
- Canlı servis veya mağaza yayını yapılmadı; gerçek anahtar istenmedi ya da yazılmadı.
- Cihazda denenmedi; tüm görüntüler editör render'ı.
