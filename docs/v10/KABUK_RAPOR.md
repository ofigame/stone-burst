# v10 Kabuk: Ana sayfa (Mavi Liman) ve Koleksiyon

Tarih: 2026-11-07. Referanslar:
- `TriKat-Mavi-Liman-Claude/01-onayli-referans.png` (940×1673)
- `TriKat-Koleksiyon-Claude/01-onayli-referans.png` (941×1672)

Görüntüler `docs/captures/v10/kabuk/` altında:
- Önce: `once_*`
- Sonra: `sonra_*`
- Yeniden üretmek için: `tools/unity.ps1 capture-shell-before` ve `tools/unity.ps1 capture-shell-after`

Geçici kayıt kullanıldı, canlı kayda yazılmadı:
- Ana sayfa: bölge 1, aktif bölüm 5, toplam 12 yıldız.
- Koleksiyon: üç kazanılmış yıldız taşı.

## Ne yapıldı

**Ana sayfa (harita) üst kabuğu** (`Scripts/UI/MenuView.Shell.cs`). Katman sırası:
1. Mor bulut sahnesi
2. Logo
3. Altın dalgalı şerit ve iki defne
4. Bölge illüstrasyonu (fener; bölgeye göre değişir)
5. Bölge etiketi, bölge adı ve aralık
6. Ayarlar (turkuaz gövde, altın çerçeve)
7. Mor yıldız kapsülü (toplam yıldız)

Davranış:
- Metinler canlı ve `Loc` üzerinden geliyor.
- Bölge etiketi, adı ve aralığı harita kaydıkça güncelleniyor (bölge 2 görüntüsüyle doğrulandı).
- Dekorlar tıklama yakalamıyor.
- Opak şerit ve bulut sahnesi, arkasındaki görünmez düğümleri tıklatmıyor.

**Ortak alt menü** (bütün menü sayfalarında):
- Sıra Harita · Günlük · Taşlar · Ödüller.
- Dalgalı krem/altın gövde, üç ayraç, dört eşit dokunma alanı.
- Seçili sekmeye basmak sayfayı yeniden kurmuyor.
- Ödüller sekmesi kullanıcı kararıyla kaldırıldı (2026-11-07); alt menü üç sekme.

**Koleksiyon ekranı** (`Scripts/UI/MenuView.CollectionV10.cs`):

Sabit parçalar:
- Arka plan, sandık ve kristaller, "Koleksiyon" kurdelesi
- Geri ve Ayarlar düğmeleri
- Çift altın kenarlı mor panel ve alt ortada elmas rozet
- Kategori kapsülü ve sayaç. Bunlar panelin üst kenarına biniyor ve görünen kategoriye göre güncelleniyor (Gizli Hazineler 0/10 doğrulandı).
- Alt menü

Kaydırma:
- Yalnız kartlar ve sonraki kategori başlıkları panel maskesi içinde kayıyor.
- Son kart kaydırmanın sonunda tam görünüyor.

Kart durumları:
- **Kazanılmış:** ışıklı çerçeve ve yeşil onay. Mavi taşlarda mavi ışık; Fırtına, Alev, Güneş, Anka ve Yakut Kupa'da altın ışık. Bu vurgu `stones.json` içindeki `accent` alanından geliyor ve yalnız görünümü etkiliyor.
- **Kilitli, adı bilinen:** buzlu cam katmanı, soluk görsel, kilit, soru rozeti, gerçek açılma koşulu.
- **Gizli hazine:** karanlık siluet, "???".
- **Seçili (günlükte kullanılan) taş:** sahiplikten ayrı, küçük turkuaz nokta.

## Kontroller

| Kontrol | Sonuç |
|---|---|
| 940×1673, 1080×1920, çentikli 1080×2400 ana sayfa | Alındı (`sonra_940x1673`, `sonra_1080x1920`, `sonra_1080x2400_centik`) |
| Kaydırma: üst ve alt kabuk sabit, başlık bölge 2'ye geçiyor | Doğrulandı (`sonra_kaydirma_bolge2`) |
| Uzun yerelleştirme ve büyük yıldız sayısı (de, es, ar; 2997 yıldız) | Taşma yok (`sonra_uzun_*`) |
| Günlük, Taşlar ve Ayarlar sayfalarında ortak alt menü | `sonra_gunluk`, `sonra_taslar`, `sonra_ayarlar` |
| Koleksiyon: 941×1672, 1080×1920, çentikli; kaydırma ortası ve sonu; Almanca | Alındı (`sonra_koleksiyon_*`) |
| Geri, sekmeler, Ayarlar dönüşleri | PlayMode `DailyAndCollection_BackButtonAndTabs_ReturnToMap` geçti |
| Testler | EditMode 165/165, PlayMode (günlük, kayıt, katalog, dünya, açılış) 22/22, dil denetimi temiz |
| Harita yolu, düğümler, maskot | Değişmedi (önce/sonra karşılaştırması). Kod değişikliği yalnız kabukta. |
| Cihaz | Denenmedi |

## Eksik final görseller (şu an koddan çizilmiş GEÇİCİ sürüm)

Final dosya belirtilen yola konunca kod onu otomatik kullanır. Hepsi yazısız ve alfa kanallı PNG olmalı, tercihen 2× ölçüde.

| Parça | Hedef dosya | Kullanım ölçüsü (1080 birim) |
|---|---|---|
| Mor bulut sahnesi (üst) | `Resources/PremiumV6/shell/top-clouds.png` | 1080 × 460 (160'ı güvenli alanın üstüne taşar) |
| Altın dalgalı üst şerit, krem oyuk | `Resources/PremiumV6/shell/top-ribbon.png` | 1080 × 200 |
| Defne (sol; sağ taraf aynalanır) | `Resources/PremiumV6/shell/laurel.png` | 104 × 146 |
| Alt menü gövdesi (dalgalı krem/altın) | `Resources/PremiumV6/shell/bottom-bar.png` | 1080 × 282 (60'ı alt kenarın altına taşar) |
| Mavi kristal sekme simgesi | `Resources/PremiumV6/shell/icon-crystal.png` | 112 × 112 |
| Kırmızı-altın sandık sekme simgesi | `Resources/PremiumV6/shell/icon-chest.png` | 112 × 112 |
| Koleksiyon arka planı (kemer, yüzen kale, taş duvar) | `Resources/PremiumV6/collection/bg.png` | tam ekran, 1200 × 2440 |
| Açık sandık ve kristaller | `Resources/PremiumV6/collection/chest.png` | 406 × 189 |
| "Koleksiyon" kurdelesi | `Resources/PremiumV6/collection/ribbon.png` | 660 × 128 |
| Panel kabuğu (9 dilim, köşe ≥ 68) | `Resources/PremiumV6/collection/panel.png` | 192 × 192 |
| Yeşil onay rozeti | `Resources/PremiumV6/collection/badge-check.png` | 64 × 64 |
| Mor elmas rozet | `Resources/PremiumV6/collection/badge-diamond.png` | 96 × 96 |

Takvim simgesi hâlâ koddan çiziliyor. Harita simgesi, fener, logo, yıldız, dişli ve kilit mevcut görseller.

## Referanstan kalan farklar

- **Geçici çizimler:** Yukarıdaki parçalar düz ve sade. Referanstaki doku, parıltı ve illüstrasyon yok.
- **Harita odağı:** Referansta aktif bölüm 5 ekranın üst-ortasında. Bizde mevcut odak davranışı korundu: düğüm biraz daha aşağıda ve keşfedilmemiş bölgenin bulutları görünüyor. Harita içeriği kapsam dışıydı.
- **Kilitli kartlar:** Referanstaki bulanık renk lekesi yerine görsel soluk ama net görünüyor. Gerçek bulanıklaştırma ayrı bir görsel ya da shader ister.
- **Seçili sekme stili (karar verildi 2026-11-07: turkuaz, alanı aşmaz). Eski not:**
  - Ana sayfa: turkuaz kapsül, krem yazı.
  - Koleksiyon: sarı-altın kapsül, lacivert yazı.
  
  Şimdilik her ekran kendi referansına uyuyor; Günlük ve Ayarlar turkuaz stili kullanıyor. **Tek stil kararı bekliyor.**
- **Taş eşlemesi:** Referanstaki taş adları, görselleri ve eşikleri örnek. Katalog değiştirilmedi; ekranda gerçek veri var.
