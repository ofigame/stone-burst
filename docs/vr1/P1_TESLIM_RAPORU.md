# vr1 paketi — P1 prototip teslim raporu

Tarih: 2026-11-11. Paket: `docs/vr1/claude_code_paketi_v1`. Önceki aşama: [P0_TESLIM_RAPORU.md](P0_TESLIM_RAPORU.md).
Kapsam: P1 görsel/ses prototipi. Bunlar paketteki "uygulama taslağı" maddeleridir; görsel kabul bekler.

**Prototip anahtarı:** Her şey `Vr1Proto.On` anahtarının arkasında ve anahtar varsayılan olarak **kapalı**. Kapalıyken oyun önceki gibidir: kilitli bulanık taş kartı, eski Harita/Günlük ikonları, prototip sesleri yok.
- Editörde aç/kapat: **KatKat → vr1 prototipi (P1)** (PlayerPrefs `katkat.vr1proto`).
- Mağaza derlemesinde de varsayılan kapalı kalır; cihazda açmak için ayrı bir düğme eklenmedi.

Yayın ve derleme yapılmadı; telefonda denenmedi; sesler cihazda dinlenmedi.

## İnceleme ekranları (`docs/vr1/p1_inceleme/`, 1080×1920, editör yakalaması)

| Dosya | Ne gösterir |
|---|---|
| `p1_01_harita_prototip_kapali.png` | Anahtar kapalı: mevcut görünüm (karşılaştırma) |
| `p1_02_buz_evre0..3.png` | Buz evreleri: toplam yıldız 5, 20, 35, 55 (ilk yıldız taşı hedefi 60) → p ≈ 0,08 / 0,33 / 0,58 / 0,92 |
| `p1_03_donus_ucus.png` | Haritaya dönüş: yıldızlar sayaca uçuyor, özet yazısı, sayaç henüz eski değerde |
| `p1_04_donus_sonu.png` | Dönüş sonu: sayaç ve çubuk yeni değerde |
| `p1_05_acilis_kirilma.png` | 58 → 61 yıldız: kabuk kırılmış, taş yükseliyor (60/60) |
| `p1_06_acilis_vitrin.png` | Kırılmadan sonra mevcut taş vitrini ("Yeni koleksiyon taşı") |
| `p1_07_bolum_basi_bilgi.png` | Bölüm başı: "Bu bölümde Okyanus Taşı açılabilir" |

Yeniden üretmek için:

```bash
powershell -ExecutionPolicy Bypass -File tools/unity.ps1 capture-vr1-p1
```

## Uygulananlar

### Buzlu koleksiyon taşı (03, onaylı yön)
- Yer: haritanın sağ üstündeki yıldız hedefi kartı. Eskiden bulanık taş ve kilit vardı; prototipte taşın asıl görseli buz içinde.
- Hedef ve eşikler `stones.json`'dan gelir (`CollectionProgress.NextStarGoal`); kodda sayı sabiti yoktur.
- Mevcut `NextStarGoal` aynı hesaba bağlandı; davranışı değişmedi.
- İlerleme: p = clamp((yıldız − önceki eşik) / (hedef − önceki eşik), 0, 1). Taş kazanılmadıkça p 1'e ulaşmaz. Kazanımı yalnız mevcut `CollectionRules` verir.
- Evreler (paketteki prototip eşikleri):

| Evre | p aralığı | Görünüm |
|---|---|---|
| 0 | < 0,25 | Yoğun kırağı, taş koyu mavi siluet |
| 1 | 0,25–0,50 | İnce çatlak (3) |
| 2 | 0,50–0,75 | Saydam pencere, dallı çatlak (6) |
| 3 | 0,75–1 | Belirgin taş, ince kabuk (9 ince çatlak) |
| 4 | — | Yalnız kazanımdan sonra: kabuk yok |

- Katmanlar: mevcut koleksiyon görseli → kırağı maskesi → paketteki buz kenarı (`Resources/Vr1/ice-rim.png`) → çatlaklar → ışıltı.
- Kırağı, çatlak ve kırılma parçaları çalışma anında sabit tohumla üretilir (`Vr1IceArt`). Taş görseli yeniden çizilmedi. Normal taşlara dokunulmadı.

### Haritaya dönüş ödül akışı (03 "Haritaya dönüş", "Son hamle ve atomik ödül")
- Kayıtta haritanın son gördüğü değerler tutulur: `meta.mapSeenStars` ve `meta.mapSeenLevels` (yeni alanlar; eski kayıtta -1 olduğu için fark gösterilmez).
- Dönüş özeti bunlarla bugünkü kayıt arasındaki farktır (`CollectionProgress.Compare`). Animasyon veri yazmaz. Aynı fark ikinci kez gösterilmez; arka arkaya oynanan bölümler tek özette birleşir.
- Zaman çizgisi (taslak süreler):
  - En çok 3 yıldız sayaca yay çizerek uçar; her biri 550 ms, aralarında 120 ms. Her varışta `reward-arrival` sesi.
  - Sayaç bir kez artar.
  - Çubuk 400 ms'de eski değerden yenisine dolar.
  - Evre değişirse `ice-crack` sesi.
- Özet metinleri:
  - Yeni bölüm varsa: "Bu oturumda {n} yeni bölüm · {m} yeni yıldız".
  - Yalnız yıldız arttıysa: "Bu oturumda {m} yeni yıldız".
  - Artış yoksa (tekrar galibiyet): "Bölümü tekrar tamamladın". Sahte "+0" kutlaması yok.
- Taş açılırsa:
  - Kart, açılan taşı 60/60 olarak gösterir.
  - Kırılma zaman çizgisi: 0–180 ms ışık, 180–400 ms yalnız kabuk ±2 px titrer, 400–750 ms 10 buz kırıntısı dışa ayrılır, 550–1100 ms taş yükselir.
  - Sesler: `ice-shatter`, ardından `collection-reveal`.
  - 1100 ms'de mevcut taş vitrini açılır. Vitrin kapanınca harita sıradaki hedefle yenilenir.
  - Kazanım kayda vitrinden önce yazılmıştır (mevcut `CheckStoneUnlocks`); sunum taşı ikinci kez vermez. Ekran sarsılmaz, yalnız kabuk titrer.
- Bölüm başı: bu denemede alınabilecek en çok net yeni yıldız eşiği geçirebiliyorsa 2 sn "Bu bölümde {taş} açılabilir" gösterilir. Bu bilgi dokunuşu engellemez.

### Tahta ortasında buzlu taş (03 "Merkez görseli", 2026-11-11 ek)
- `BoardHoles.Largest` iç boşluğu bulur: kenara değmeyen, geçit olmayan, tamamen boş bölge. 1000 bölümün 305'inde en az 2×2 boşluk var (130'unda 2×2, 175'inde 3×3 ve üstü); 695 bölümde yok.
- Buzlu taş yalnız bu bölümlerde, boş karenin %78'i boyunda gösterilir. Hücre örtmez, dokunuşu engellemez. Altında küçük "Koleksiyon" etiketi (9 dil) var; güçlendirici sanılmasın diye.
- Boşlukta zaten bulunan süs (maskot, fener, çeşme) buzlu taş varken gizlenir. Hedef taş kalmazsa süs yerinde kalır. Boşluğu olmayan tahtada yeni delik açılmaz.
- Evre görünümü haritadaki kartla aynı kuralı kullanır. Son hamle kazandırınca kısa çatlak ışığı ve `ice-crack` sesi; tam kırılma ve vitrin haritada.
- Ekranlar: `p1_09_tahta_ortasi_buz.png` (bölüm 123), `p1_10_tahta_ortasi_buz_b.png` (bölüm 303).

### Animasyon süreleri (04 "Animasyon tablosu", 2026-11-11 ek; taslak)

Anahtar açıkken geçerli. Kapalıyken eski süreler aynen kalır. Motor sırası ve sonuç değişmez; yalnız oynatma süreleri kısalır.

| An | Tablo | Eski | Prototip | Yer |
|---|---|---|---|---|
| Dokunuş sıkışması | 90 ms, 1 → 0,96 → 1 | 126 ms, 0,94 | 54 ms, 0,96 | `PeelSequence.AnticipationEnd`, `SquashDepth` |
| Üst katman soyma | 220 ms | 574 ms (bitiş 896) | 246 ms (bitiş 384) | `PeelSequence.Slow` 1,4 → 0,6 |
| Alt rengin okunur olduğu an | — | 420 ms | 180 ms | `PeelSequence.ReadableAt` |
| Zincir dalga arası | 100 ms | 420 ms, en az 300 | okunur an + 100 ms (280), en az +80 | `BoardView.First/MinChainInterval` |
| Özel taşın tepsiye uçuşu | 420 ms | 500 ms | 420 ms | `BoardView.SpecialFlyMs` |
| Roket | öncü 120 ms + uçuş 350–600 ms | 260 ms + 100 ms/hücre | 120 ms + 60 ms/hücre (9 hücre ≈ 660 ms) | `SpecialLead`, `RocketCellMs` |
| Altın Rulo | toplam 500–800 ms | öncü 260 ms + 55 ms/hücre | öncü 120 ms + 45 ms/hücre | `SpecialLead`, `RainbowCellMs` |
| Hedef tamamlanma | 240 ms | 250 ms | değişmedi | `GoalChip` |
| Yıldız kutlaması | 1100 ms | 1050 ms | değişmedi | `ResultPanel` |

- Dokunuş sıkışması tablodan kısa kaldı: sıkışma, soymanın ilk evresiyle aynı zaman çizgisinde. 90 ms'ye çıkarmak için soyma sırasının yeniden düzenlenmesi gerekir.
- Ölçüm (editör, gerçek zaman): bölüm 2'nin ilk hamlesi eskiden 1418 ms, prototipte 715 ms sürdü ve iki durumda tahta birebir aynıydı (`Vr1FlowTests.AnimationTable_ShorterPlayback_SameResult`). Cihaz kare süresi ölçülmedi.
- Karar süresi P0'dan beri animasyondan ayrı sayıldığı için bu değişiklik ZPE'yi etkilemez.

### İkonlar (04)
- Alt menüde Harita ve Günlük için paketteki adaylar kullanıldı (`Resources/Vr1/nav-map.png`, `nav-daily.png`). Günlük ikonu güneşsiz (v2, beğenilen).
- Kullanıcı isteğiyle (2026-11-11: "iconlar büyük olsun ve alanı da taşsınlar") prototipte üç ikon da büyük ve menünün üst kenarından yaklaşık 40 birim taşıyor. Harita ve Günlük 168×150 (saydam pay nedeniyle), Taşlar 132×132; yazılar yerinde. Anahtar kapalıyken 88×88.
- Taşlar, ZPE ve Sıralama ikonlarına dokunulmadı.

**İçe aktarma (`Editor/Vr1AssetImporter.cs`):**
- Sprite, alpha saydamlık, sRGB, mip kapalı, bilinear.
- Çalışma dokusu: ikonlar 256, buz kabuğu 512. Kaynak PNG'ler orijinal boyutta.
- Android ASTC 4×4.

### Sesler (04)
- 7 WAV dosyası `Resources/Audio/Vr1/` içinde: mono, belleğe açılarak yüklenir, ADPCM.
- `SoundBank.PlayVr1` ile çalınır, kazanç 0,65. Mevcut 4 kaynaklı havuz kullanılır.

| Ses | Ne zaman çalar |
|---|---|
| `ui-stone-tap` | Alt menü dokunuşu |
| `special-charge` | Özel taş oluşumu |
| `gate-open` | Geçit açılışı |
| `reward-arrival` | Yıldızın sayaca varışı (en çok 3) |
| `ice-crack` | Evre değişimi |
| `ice-shatter` | Kabuk kırılması |
| `collection-reveal` | Taşın ortaya çıkışı |

- Anahtar kapalıyken ya da ses ayarı kapalıyken çalmaz.
- Mevcut müzik, peel-pop, chain-four ve level-reward sesleri değişmedi.

### Yerelleştirme
4 yeni anahtar, 9 dilde: `vr1.can.unlock`, `vr1.summary`, `vr1.summary.stars`, `vr1.replay`.

## Uygulanmayanlar
- **Sonuç kartında buz:** gösterilmiyor. İlerleme haritada ve (ortası boşsa) tahtada görünür.
- **Uygulama kapanınca bekleyen sunumun kalıcı olması (`pendingPresentation`):** yapılmadı. Taş kayda yazılıyor, ama uygulama sunumdan önce kapanırsa vitrin bir sonraki açılışta gösterilmez (eski davranış).
- **Azaltılmış hareket seçeneği:** oyunda böyle bir ayar yok, eklenmedi. Kırılma sırasında ekran sarsılmıyor, yalnız kabuk titriyor.
- **Diğerleri:**
  - Kritik açılışta sürekli efektleri 4 dB kısma yapılmadı.
  - Seçili ikonun 160 ms büyüme hareketi eklenmedi.
  - Taşlar, ZPE ve Sıralama ikonları değişmedi.
- **Cihazda yapılmayanlar:** ses dinleme, kare süresi ölçümü, 360×640 / tablet / Arapça ekranlarda inceleme.

## Testler

| Komut | Sonuç |
|---|---|
| `dotnet test tools/CoreTests` (`CollectionProgressTests`) | 5/5 |
| `tools/unity.ps1 test-play -Filter Vr1FlowTests` | 3/3 |
| `tools/unity.ps1 test-edit` | 217 test: 216 geçti, 1 atlandı (elle çalıştırılan ölçüm), 0 başarısız |
| `tools/unity.ps1 test-play` (50 bölüm örneklemi + akışlar) | 65 test: 64 geçti, 1 atlandı, 0 başarısız (P0 sonrası düzeltilen koleksiyon testi dahil) |
| `tools/unity.ps1 loc-check` | TAMAM |

**`CollectionProgressTests`:**
- Hedefler katalog sırasıyla gelir; açılmış taş geri kilitlenmez.
- C01 eşik senaryosu: 58→59 açılmaz, 58→60 eşiğe ulaşır.
- Evre eşikleri doğru.
- Net yeni yıldız hesabı: önceki en iyi 2 ise katkı 1; ipucu bedeli hesaba katılır.
- C06: harita özeti kayıt farkıdır.

**`Vr1FlowTests` (gerçek GameController ile):**
- Anahtar kapalıyken eski kart görünür, dönüş animasyonu yok.
- Dönüş özeti bir kez gösterilir: en çok 3 uçan yıldız, ikinci açılışta özet yok.
- 58→61 yıldızda taş bir kez kazanılır; vitrin kırılmadan önce açılmaz, sonra açılır, taş ikinci kez verilmez.

## Karar bekleyenler
- Buz görünümünün ve evre eşiklerinin görsel kabulü.
- Harita ikonu adayının kabulü (Günlük ikonu beğenilmişti).
- Seslerin cihazda dinlenip kabulü.
- Prototipin canlıda varsayılan açık olup olmayacağı.
- Animasyon sürelerinin cihazda hissedilerek kabulü (prototipte uygulandı).
- P2: 12 bölümlük pilot katalog.
