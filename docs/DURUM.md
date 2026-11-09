# Stone Burst (eski adı TriKat) — Durum

Son güncelleme: 2026-11-09. Oyunun tam tanımı: [OYUN.md](OYUN.md). Kararlar: [KARARLAR.md](KARARLAR.md).

## Şu an

- **2026-11-08 (gece), zincir kuralı v13** (kullanıcı: "Altından birlikte çıkan 3+ aynı renk patlar"):
  - 1000 bölüm, 240 günlük ve deneme seti yeni kurala uyarlandı; tahtalar korundu. Rapor: `docs/v13/ZINCIR_RAPOR.md`.
  - report, stres, Core ve EditMode testleri, PlayMode 48/48 geçti.
  - Bot kazanma oranı eski setten düşük (ör. 21–100: %43 → %32); zincir iki kat sık.
  - Ayarlar sayfası Sıralama zemininde (Z1).
  - APK derlenmedi.
- **2026-11-08 (akşam), canlı öncesi istekler:**
  - Açılış logosu artık basık değil. Neden: logo dokusu 2440×1490 iken 2048×1024'e esnetiliyordu; içe aktarma düzeltildi ve oran koddan sabitlendi.
  - Yeni uygulama ikonu: Android uyarlanabilir katmanları ışın zemin + güvenli alanda ikon; eskiler arşivde (`ikon_eski_2026-11-08`).
  - Tepsideki özel taşlar daha yukarıda, ışıltılı; seçimde büyüme ve dönen halka var.
  - Roket önce büyüyüp parlıyor, sonra ikiye bölünüp yavaşça uçuyor. Altın Rulo hedef taşlara yıldırım atıyor.
  - Reklamlı ipucu eklendi (−1 yıldız).
  - Hesap katmanı (test hesabı), Ayarlar'da "İlerlemeyi kaydet", Sıralama'da "Sıralama için kayıt ol". Gerçek Play Games bağlantısı yok; plan: `docs/v13/KAYIT_PLANI.md`.
  - Oyun arka planları zaten `arkaplanlar` klasörünün 5 görseliyle birebir aynıydı; bölüm açılınca rastgele seçiliyor (değişiklik gerekmedi).
  - Testler: EditMode 194/194, PlayMode 48/48 (+1 atlanan), loc-check TAMAM. Yakalamalar: `docs/ui3_uygulama` 05, 07, 12, 13, 36–40.
  - APK/exe bu değişikliklerle derlenmedi, telefonda denenmedi.
- 1000 bölüm, v7 bölüm seti (6–1000 yeniden üretildi; 1–5 aynı). `LevelLab report`: 1000/1000 kayıtlı çözüm kazanıyor. `LevelLab stress 60`: 1000/1000 (60.000 oyun, 1,1 milyon adım, istisna yok, boya çıkmazı 0).
- Bölüm seti v13 (2026-11-08): zincir kuralı değişti, çözüm/sınır/hedefler yeniden ayarlandı (docs/v13/ZINCIR_RAPOR.md).
- Testler (2026-11-06): CoreTests 117/117, EditMode 117/117, PlayMode 16/16 (1000 bölüm oyun akışı dahil, 51 dk), `loc-check` TAMAM (9 dil, 120 anahtar).
- Android: tam ekran (sanal tuşlar gizli), Unity açılış logosu kapalı, uyarlanabilir ikon, ASTC doku sıkıştırması.
- Bütün bölümler test için açık (`TestUnlockAll = true`).
- APK ve exe v7 değişiklikleriyle henüz derlenmedi.

## 2026-11-06 v10 (ana plan: `docs/v10/`)

- Denetim belgeleri: [AUDIT](v10/AUDIT.md), [GAP_ANALYSIS](v10/GAP_ANALYSIS.md), [IMPLEMENTATION_BACKLOG](v10/IMPLEMENTATION_BACKLOG.md), [OPEN_DECISIONS](v10/OPEN_DECISIONS.md).
- P0.2: `ProductionLevelChecksumTests`: 1.000 bölümün SHA-256 özetleri `Tests/EditMode/Data/levels_v7.sha256` ile her koşuda karşılaştırılıyor. Tek bayt değişikliğiyle denendi; test düştü.
- P1.3: yükseltme öncesi kalıcı kayıt kopyası (`katkat_save.v1.json`) ve bozuk ana dosyanın kopyası (`.corrupt`). Dönen `.bak` ve yedekten dönüş önceden vardı.
- P1.4: 1 → 3 → 2 yıldız ve 1.000 bölüm × tekrar = 3.000 tavanı testleri.
- Karar (kullanıcı): Gelgit Taşı 10. bölümde kalır. Kalan yıldız taşlarının kuralı taşlar eklenirken sorulacak.
- P3.2: taş gücü ortak yapısı `IStonePower` (`Levels/Meta/Stones.cs`). Aileler: gelgit (rulo), okyanus (satır roketi), fırtına (sütun roketi), galaksi (Altın Rulo), ay (+2 hamle). Kural motoru değişmedi. Güç kullanılınca HUD tepsi ve hamleyi de günceller (`GameController.Meta.TryDailyPower`).
- Testler: CoreTests 147/147, EditMode 147/147, PlayMode kayıt + günlük + açılış 16/16. Tam PlayMode (1.000 bölüm akışı) bu adımlarda koşulmadı.
- P3.4: günlük içerik hattı. `LevelLab daily` 30 gün × 8 seviye grubu = 240 bulmaca üretir (`daily_src/` → `Resources/Daily/daily_dNNN_tK.json`, `daily_index.json`, contentVersion `daily-v2`). Karar (kullanıcı): günlük bulmaca oyuncunun seviyesine göre ve çok zor. Grup, oyuncunun ulaştığı en yüksek bölüme göre seçilir (1–40, 41–100, 101–200, 201–350, 351–500, 501–700, 701–850, 851–1000) ve o gün için kayda sabitlenir (`DailySave.tier`). Bulmacada yalnız oyuncunun gördüğü mekanikler var. Hamle sınırı en kısa çözüm +1/+2/+3; plansız botun kazanma oranı grup ortalaması %3,4–4,4 (tek tek %1–11,5). Her bulmacanın kayıtlı çözümü güç kullanmadan kazanıyor. Eski `daily_001` arşivde (`gunluk_v1/`).
- Testler (P3.4 sonrası): CoreTests 149/149, EditMode 149/149, PlayMode günlük + kayıt 15/15. Tam PlayMode koşulmadı; cihazda denenmedi.
- Günlük ekranında seviye grubu rozeti: "Seviyene göre: Bölüm 501–700 · Çok zor" (9 dil, `daily.tier`). Editör yakalaması: `tools/unity.ps1 capture-daily-tier` → `docs/captures/v9/yeni/gunluk_grup_*.png` (tr, en, ar, ja). Arapçada rozet yazısı küçük görünüyor (otomatik boyut alt sınırı); cihazda bakılmalı. PlayMode günlük + kayıt 15/15, dil denetimi temiz.
- P0.3: gerçek biçimde örnek kayıtlar `Tests/PlayMode/Data/save_v1_sample.json` (v8 dönemi, 120 bölüm, yinelenen/geçersiz yıldız) ve `save_v2_sample.json` (v9, grup alanı öncesi). Testler: v1 kayıpsız yükselir (toplam 240 yıldız, yükseltme öncesi kopya birebir); v2 olduğu gibi açılır, yazıp açınca alan kaybı yok. Yeni kayıt sürümünde yeni örnek eklenir, eskiler değişmez.
- P1.6: bölüm dizini `Resources/LevelIndex/levels_index.json` (`tools/LevelLab index`). Katalog açılışta yalnız dizini okur; dizin yoksa ya da bozuksa eski taramaya döner (`LevelCatalog.Scan`, hata günlüğü). `LevelIndexTests` dizinin bölüm dosyalarıyla birebir aynı olduğunu denetler; bölüm eklenip dizin yeniden üretilmezse test düşer. Editör ölçümü: açılış dizinle 8 ms, taramayla 56 ms ve anlık +28 MB (dizinin anlık belleği güvenilir ölçülemedi). Bölüm dosyaları değişmedi (özet testi geçti).
- Arka planda süren ek günlük analizi durduruldu (LevelLab exe dosyasını kilitliyordu). 240 bulmaca üretimde zaten çözücü ve botla ölçülmüştü; testler kayıtlı çözümü yeniden doğruluyor.
- Testler (P0.3 + P1.6 sonrası): CoreTests 152/152, EditMode 152/152, PlayMode katalog + kayıt + günlük + açılış 20/20. Tam PlayMode 30/30 (1 atlanan: elle çalışan ölçek ölçümü); 1.000 bölümün kayıtlı çözümü gerçek denetleyiciyle dizinden açılarak kazanıyor (51 dk). Cihazda denenmedi.
- Windows derlemesi (2026-11-07): `Builds/Windows/TriKat.exe` (P0.3 + P1.6 dahil). 12 sn açılış denemesi: çalışıyor, `Player.log` hatasız ("Boot tamam"). Elle oynanmadı.
- P1.5: dünya katmanı yalnız veri (`Levels/Meta/Worlds.cs`, `Resources/Worlds/worlds.json`). Dünya = 500 bölüm = 5 bölge; 1.000 bölümde world_01 (1–500, bölge 1–5) ve world_02 (501–1000, bölge 6–10). Yapılandırılmamış dünyalar aynı boyutla türetilir (10.000'de world_03–world_20). Kalıcı kimlik `world_NN`. `WorldCatalog.CompletedIn` dünya tamamlamayı sayar (efsanevi taş için). Harita, bölge levhaları ve tema dönüşü değişmedi; görünüm koduna dokunulmadı. Dünya adları (Akdeniz Sahili / Çiçek Bahçesi) açık karar (#11): `nameKey` boş, arayüzde gösterilmiyor. Oyunda `GameController.Worlds` olarak yüklü.
- Testler (P1.5 sonrası): CoreTests 158/158, EditMode 158/158, PlayMode katalog + dünya + kayıt + günlük + açılış 21/21.
- Karar (kullanıcı, 2026-11-07): dünya adları şimdilik oyunda gösterilmez.
- P2.1: taş kataloğu şeması 2 (`Levels/Meta/Stones.cs`). Yeni isteğe bağlı alanlar:
  - `category`: starStone, hiddenTreasure, legendaryStone
  - `rarity`: common, rare, epic, legendary
  - kazanım türleri: `levelCompleted` (bölüm), `starsTotal` (yıldız toplamı), `treasure` (hazine kimliği), `worldCompleted` (dünya kimliği)
  - Eski v9 dosyası (şema alanı yok) aynen okunur: varsayılanlar starStone/common, davranış aynı. Bilinmeyen kazanım türü okunur ama kazandırmaz; bilinmeyen kategori/nadirlik, gelecek şema ve eksik değer içerik hatasıdır.
  - Oyun `UnlockContext` ile bağlı: bölüm, toplam yıldız, dünya tamamlama (`WorldCatalog`). Hazine durumu P2.3'te gelecek; o zamana kadar hazine koşulu hiç sağlanmaz.
  - Koleksiyon ekranında kilitli taşın koşul metni türüne göre (9 dil: `stone.unlock.stars`, `stone.unlock.world`, `stone.unlock.treasure`; hazinenin yeri açık edilmez).
  - `stones.json` şema 2'ye taşındı; Gelgit Taşı'nın anlamı değişmedi (test eski ve yeni dosyayı alan alan karşılaştırır). Yeni taş eklenmedi; nadirlik ve kategori henüz ekranda gösterilmiyor (P2.5).
- Testler (P2.1 sonrası): CoreTests 164/164, EditMode 164/164, PlayMode 21/21, dil denetimi temiz.
- Taş görselleri (kullanıcı, 2026-11-07): `docs/taşlar/` içinde 22 saydam PNG ve 9 taşın 3D modeli (Tripo FBX + 4K dokular, taş başına ~90 MB). Kalan 13 taşın 3D modeli sonra yapılacak.
- İlk sürüm koleksiyonu oyunda (kullanıcı eşlemeyi onayladı):
  - Görseller 512 px'e küçültülüp `Resources/Collection/stones/<stoneId>.png` olarak alındı (toplam 8,4 MB kaynak; içe aktarma `Editor/CollectionArtImporter.cs`: sprite, en çok 512, sıkıştırılmış, mip). 3D modeller oyuna alınmadı (mobil için sadeleştirme gerekir).
  - `stones.json` 22 öğe: 10 yıldız taşı, 10 gizli hazine, 2 efsanevi taş. Eşleme:
    - Gelgit (rulo) = Turkuaz Dalgalar; Okyanus (satır roketi) = Kanatlı Sunak; Fırtına (sütun roketi) = Şimşek Relik; Ay (+2 hamle) = Ay ve Yıldız Küresi; Galaksi (Altın Rulo) = Galaksi Kum Saati.
    - Gücü henüz olmayan yıldız taşları: Alev, Yaşam, Ayaz, Güneş, Kozmos ("Gücü yakında").
    - Efsanevi: Anka Kristali (dünya 1), Buz Ejderhası Tacı (dünya 2).
    - Gizli hazineler (tr_01–tr_10): Yakut Kupa, Buzul Kaidesi, Gül Tahtı, Zümrüt Taht, Kanatlı Obelisk, Rünlü Kaide, Taçlı Kristal, Lotus Tahtı, Ay Anıtı, Orman Reliki. P2.3 gelene kadar kazanılamaz.
  - Kazanım: Gelgit 10. bölümde kalır. Diğer 9 yıldız taşı toplam yıldız eşikleriyle (250, 500, …, 2.250; ana planın 250'lik adımı). Bu eşikler P2.2 kararıyla değişebilir.
  - Günlükte kullanılan taş artık ilk kazanılan güçlü taş (gücü olmayan taş seçilmez). Taş seçme ekranı yok (P3.3).
  - Adlar ve açıklamalar 9 dilde (32 metin). Japonca yazı tipinde olmayan 横/縦/説 karakterleri yerine kana kullanıldı.
  - Koleksiyon ekranı: kaydırılabilir 3 sütunlu ızgara, kategori başlıkları ve sahip olunan/toplam sayısı. Kilitli öğe soluk ve koşul yazılı; bulunmamış hazine karanlık siluet, adı "???". Yakalama: `tools/unity.ps1 capture-collection` → `docs/captures/v9/yeni/koleksiyon_izgara_*.png`.
  - Testler: CoreTests 165/165, EditMode 165/165, PlayMode günlük + kayıt + katalog + dünya + açılış 22/22, dil denetimi temiz. Cihazda denenmedi.

## 2026-11-06 v9 A/B (yerel prototip)

- Yol haritası: `docs/v9/`. Rapor: [v9/V9_AB_RAPOR.md](v9/V9_AB_RAPOR.md).
- A: sürümlü kayıt (v2, bir kez yükseltme), yıldız toplamı bölüm başına en iyi 1–3, oturum sonucu modeli (`SessionOutcome`, yardım bayrakları, etkin süre).
- B: ilk koleksiyon taşı (Gelgit Taşı, 10. bölüm sonrası, yer tutucu simge), günlük bulmaca (ayrı katalog, ilk deneme + ücretsiz tekrar, SAHTE reklam, taş gücü denemede bir kez), başarıda aynı gün yeni normal bölümlerde +1 hamle.
- Giriş (kullanıcı, 2026-11-06): alt menü Harita · Günlük · Taşlar; Oyna düğmesi kalktı; Ayarlar sağ üstte dişli. Günlük/Taşlar ekranlarında geri düğmesi (önceden alt menü sekmeleri çalışmıyordu, haritaya dönülemiyordu).
- Logo: kullanıcının yeni TriKat logosu (`docs/gorseller/arayuz/logo.png`, saçakları temizlenmiş, 1280×380). Önceki logo arşivde (`logo_trikat_ilk`).
- E paketi (yerel): katalog bölümü ilk istendiğinde okur, kayıt aramaları sözlükle, harita görünür aralığı ikili aramayla. Ölçüm ve kalanlar: [v9/V9_E_RAPOR.md](v9/V9_E_RAPOR.md).
- Testler (2026-11-06 akşam): CoreTests 133/133, EditMode 133/133, PlayMode 21/21 (1000 bölüm akışı dahil; ölçüm testi yalnız istenince çalışır), `loc-check` TAMAM.
- APK: `Builds/Android/TriKat.apk` (84 MB, 17:41). Exe v9 ile derlenmedi. Telefonda denenmedi.
- Tanıtım videosu (Shorts): `Builds/Video/TriKat_short_en.mp4` ve `_tr.mp4` (58,6 sn, sesli: oyunun kendi efektleri görüntüyle eş zamanlı + koddan üretilmiş müzik, -14 LUFS; koddan çizilmiş el taşlara, tepsi/rulo düğmelerine, haritadaki bölüme ve "Devam"a dokunur). Yeniden üretmek: `tools/unity.ps1 trailer-frames` (iki dil), sonra `tools/trailer.ps1 -Lang en` / `-Lang tr`. Ses kaydı için `SoundBank.CaptureTap` kancası (oyunda boş).
- Görsel regresyon: oyun ve sonuç kartı × 2 boyut fark %0; ana ve harita karelerinde yalnız iki yeni düğme.

## 2026-11-06 v7

- Oyunun adı TriKat: giriş ekranı ve ana sayfa logosu yeni, ayarlardaki sürüm yazısı, ürün adı, `Builds/Android/TriKat.apk`, `Builds/Windows/TriKat.exe`. Eski logolar arşivde (`KatKat_Arsiv_2026-11-06/logo_katkat_eski`). Uygulama ikonu değişmedi.
- Oyun ekranı: alttaki tepsi ve geri alma 70 birim yukarı.
- Yazı tipi: Nunito ExtraBold/SemiBold (TMP dinamik SDF), Arapça için Noto Sans Arabic, Japonca/Korece için Noto Sans KR yedekleri (~6 MB). Hap butonlarda yazı yüksekliğe oranlı, sığmazsa küçülür; buton yükseklikleri büyütüldü.
- Ayarlar yeniden tasarlandı: resimsiz renk geçişi + hareketli kabarcıklar, ses/titreşim anahtarları, dil seçimi.
- 9 dil (tr, en, es, ar, de, it, fr, ja, ko): `Resources/Localization/strings.txt`, `Loc`, Arapça sağdan sola (`ArabicShaper`, `LocText`). Çeviriler taslak; anadil kontrolü yapılmadı.
- Açılış ekranı: OFIGAME logosu (batch modda atlanır).
- Öğretici turlar: 1, 2, 3, 6, 7, 9. bölümlerde yönlendirmeli oynatma, 12. bölümde tepsi bilgisi, her yeni mekaniğin ilk bölümünde anlatım; odak açık, çevre karartılmış. Her bölümde bir kez gösterilir.
- İçi boş tahtalar (halka, çift göz, kesik halka; 21+ bölümlerin %32'si) ve boşlukta animasyonlu süs (maskot, fener, kaya, çeşme; `BoardDecor`).
- Zorluk mantık ağırlıklı: hamle sınırı = çözüm + pay (+2…+6), bölümlerin %69'u açgözlü oyuncuyla çözülmüyor. Ayrıntı: OYUN.md §6.
- `LevelSolver.Moves()`: hamle 0 iken reddedilecek dokunuşları önermiyor.
- Ekran görüntüleri: `docs/captures/v7/` (editör çizimi; cihazda denenmedi).

## 2026-11-06 temizliği

- Resources'ta kullanılmayan 65 görsel (eski v2 paket, eski tahta çerçeveleri/tepsileri, satin maskotları, eski mekanik görselleri) proje dışına arşivlendi. Kullanım, bütün ekranlar ve her mekanik bölümü oynatılarak ölçüldü (`tools/unity.ps1 capture-usage` → `Logs/resource_usage.txt`).
- Eski dokümanlar, ekran görüntüleri, eski bölüm kaynak arşivleri, `ArtSource`, `tools/ArtSlicer`, eski tek seferlik betikler ve iki test sahnesi arşivlendi: `E:\OFIGAME\OYUNLAR\KatKat_Arsiv_2026-11-06`.
- Kullanılan görsellerin kaynakları `docs/gorseller/` altında konulara göre.
- APK 196 MB → 76 MB. Android'de Resources dokuları ASTC (tam ekran 5×5, diğerleri 4×4). Unity 6.6'da mipmap'li NPOT dokuya ASTC uygulanmıyor: NPOT dokularda mipmap kapalı (`BuildScripts.ApplyTextureBudget`, her derlemede çalışır). Kalan büyük kalem: ARMv7+ARM64 yerel kütüphaneler (~30 MB).
- Sanal tuşlar: `requestedVisibleInsets = 0` + `systemBarsBehavior = ShowTransientBarsBySwipe` (Unity 6.6'da `startInFullscreen` kalktı), çalışırken `AndroidApplication.currentWindowInsets.Hide` (`ImmersiveMode`).
- İkon zemini: sahil_c mavi deniz kesiti, bulanık, parlaklık −0,16.

## Bilinen eksikler

- Telefonda doğrulama kullanıcıda (Redmi): tam ekran ve ikon.
- Yayın öncesi: `TestUnlockAll = false`, imza anahtarı.
- Telefonda denenmedi: v7 arayüzü, öğretici turlar, Arapça/Japonca/Korece yazılar, açılış ekranı.
- Boya çıkmazı (karar bekliyor): `RulesEngine.HasLegalMove` boya hakkı kaldıkça oyunu sürdürür, boyayla grup kurulamasa bile; oyuncu boyayı harcayınca kaybeder. v7 setinde görülmedi.
- 654 ve 970: plansız bot bağımsız tohumlarda %2–3 kazanıyor (en zor uçtalar; çözümleri doğrulandı).
- Windows derlemesi (2026-11-07 02:27): `Builds/Windows/TriKat.exe`, 22 taşlı koleksiyon dahil (kabuk tasarımından önce). Tam PlayMode 32/32 (1 atlanan ölçüm) geçti.
- v10 kabuk (kullanıcı referansları: Mavi Liman ana sayfa + Koleksiyon): sabit üst başlık, ortak 4 sekmeli alt menü (Harita · Günlük · Taşlar · Ödüller) ve yeni Koleksiyon ekranı. Rapor, eksik final görsellerin listesi ve kalan farklar: [v10/KABUK_RAPOR.md](v10/KABUK_RAPOR.md). Görüntüler: `docs/captures/v10/kabuk/`.
  - Final görseli olmayan 12 parça koddan GEÇİCİ çiziliyor; final PNG belirtilen yola konunca kendiliğinden kullanılır.
  - Ödüller sekmesi: gerçek ekran yok, "yakında" bildirimi gösteriyor (açık iş).
  - Seçili sekme stili iki referansta farklı (turkuaz / sarı-altın): karar bekliyor.
  - Testler: EditMode 165/165, PlayMode 22/22, dil denetimi temiz. Bu değişiklikten sonra exe derlenmedi; cihazda denenmedi.
- v10 oyun içi kabuk (kullanıcı referansı) + alt katman okunurluğu (kullanıcı isteği) + Günlük modalı (kullanıcı isteği ve görseli): [v10/OYUN_ICI_RAPOR.md](v10/OYUN_ICI_RAPOR.md). Görüntüler `docs/captures/v10/oyun/`.
  - Oyun arka planı bütün temalarda mor bulutlu (geçici çizim; final `PremiumV6/game/bg.png`). Harita temaları değişmedi.
  - Alt katman yan şeritleri daha kalın ve doygun; krem taşın şeridi bej. Taş yüzleri, atlaslar ve kaplamalar değişmedi (SHA-256 ile doğrulandı).
  - Günlük bulmaca haritanın üstünde modal.
  - Testler: CoreTests/EditMode 165/165, PlayMode 24/24. Tam PlayMode ve exe arka planda.
- v10 parça paketleri bağlandı (kullanıcı, 2026-11-07; rehberler `docs/v10/TriKat-Mavi-Liman-Ust-Parcalar/`, `docs/v10/TriKat-Koleksiyon-Parcalari/`):
  - Ana sayfa üst başlık: altın dalgalı şerit (defneler dahil), mor bulut zemini, yıldız kapsülü, turkuaz bölge etiketi → `Resources/PremiumV6/shell/`. Katman: bulut < fener/logo < şerit < yazılar.
  - Alt menü kabuğu ve Ödüller kasa ikonu (menüler ve oyun içi alt kabuk ortak) → `shell/bottom-bar.png`, `shell/icon-chest.png`.
  - Koleksiyon: arka plan (menünün tam ekran cover katmanında), açık sandık, kurdele, 9 dilimli çerçeve (üst 130 / alt 250, yan 0; köşe, alt elmas ve dalga ezilmez), kilitli kart buz kabuğu → `Resources/PremiumV6/collection/`. İçe aktarma: `Editor/ShellAssetImporter.cs`.
  - Kilitli taşlar tanınmasın (kullanıcı): önceden bulanıklaştırılmış sürüm (`Resources/Collection/stones_blur/`) + buzlu örtü; gizli hazine karanlık siluet.
  - Sesler (`Resources/Audio/TriKat/`): menü döngüsü (tek 2D kaynak, `Game/MenuMusic.cs`; menüde çalar, yeniden çizimde baştan başlamaz, bölüm başında 200 ms söner, arka planda durur), tek taş pop'u (Sfx.Peel), dört vuruşlu zincir (hamlenin ilk zincir dalgasında bir kez, perde yükseltmesi yok, eski Sfx.Chain çalmaz), bölüm sonu ödülü (normal zafer kartında bir kez; günlük zaferde eski ses). Ses ayarı müziği ve efektleri birlikte anında susturur. WAV yoksa eski sentez sesler. Kayıt aracı için stereo → mono ortalama (normalize yok).
  - Testler: EditMode 165/165, PlayMode 28/28 (yeni `AudioShellTests` 4/4), dil denetimi temiz. Telefonda dinleme testi YAPILMADI (kullanıcı yapacak). Görüntüler `docs/captures/v10/kabuk/` (sonra) ve `docs/captures/v10/parca/` (parçalardan önce).
- v10 taş vitrini ve 3D modeller (kullanıcı, 2026-11-07):
  - Blender 4.2.9 LTS taşınabilir indirildi: `E:\ARAC\blender-4.2.9-windows-x64` (resmî SHA-256 doğrulandı; sisteme kurulmadı). Betik: `tools/blender/decimate_stones.py`.
  - 9 Tripo modeli 70–80 bin → 30 bin üçgen, en uzun kenar 1 birim, taban 0; dokular renk + normal 1024 (metallik/pürüzlülük kullanılmadı). `Resources/Models/Stones/<stoneId>/` (model.fbx, color.png, normal.png, mat.mat; Standard). İçe aktarma ve malzeme: `Editor/StoneModelTools.cs` (`tools/unity.ps1 stone-materials`).
  - Vitrin (`Scripts/UI/StoneShowcase.cs`): koleksiyonda kazanılmış karta dokununca ve taş kazanılınca açılır. 3D model ayrı katmanda (31) kendi kamerasıyla döner, parmakla çevrilir; modeli olmayan 13 taş büyük PNG. Işıltı taşın renginde: iki renkli nokta ışık + hafif emisyon + nabız atan hale + yörüngede parıltılar. Renkler `stones.json` "glow" (PNG'nin baskın renginden; Galaksi, Ay, Yaşam, Orman Reliki elle düzeltildi). Kilitli taş vitrine çıkmaz.
  - Taş kazanımı artık bildirim yerine vitrinle gösterilir (birden fazlaysa sırayla).
  - Görüntüler `docs/captures/v10/vitrin/`. Testler: EditMode 165/165, PlayMode 30/30. Tam PlayMode, APK ve telefonda deneme bekliyor.
- Kullanıcı istekleri (2026-11-07, devam): seçili sekme turkuaz ve alanını aşmaz; Ödüller sekmesi kaldırıldı (3 sekme); yıldız taşı eşikleri 60…2250 (KARARLAR); ana sayfada yıldız çubuğu (sıradaki taşın renginde dolum, sonunda taş görseli).
- v10 P5 ZPE (Zekâ Performans Endeksi; IQ değildir): `Levels/Meta/Zpe.cs`, `Resources/Zpe/zpe_config.json`, `Resources/Zpe/level_ref.json` (`tools/LevelLab zperef`), kayıtta `meta.zpeHistory`. Ana sayfa kapsülü + istatistik paneli + bölüm sonu rozeti; 9 dilde metin. Görüntüler `docs/captures/v10/vitrin/zpe_*`. Ağırlıklar gerçek oyun verisiyle kalibre edilmedi (plan gereği sonra).
- Tam PlayMode (vitrin + ses kodu): 38/38 geçti (1 atlanan ölçüm), 1000 bölüm akışı dahil.
- Derlemeler (2026-11-07 05:33+): `Builds/Android/TriKat.apk` 142,7 MB (önceki 76 MB; artış çoğunlukla yeni kabuk görselleri — Android'de sıkıştırılmadan 7–8 MB'lık dokular; küçültme ayrı iş) ve `Builds/Windows/TriKat.exe`. İkisi de ZPE, yıldız çubuğu, 3 sekme, 3D vitrin ve sesleri içerir. Testler: CoreTests/EditMode 176/176, PlayMode 30/30, dil denetimi temiz. Telefonda deneme bekliyor.
- APK küçültme (kullanıcı, 2026-11-07): 142,7 MB → 104 MB. Neden: Unity 6.6'da 2'nin kuvveti olmayan dokuda mipmap açıkken ASTC uygulanmıyor, yeni kabuk/koleksiyon görselleri ham RGBA giriyordu (7–8 MB'lık). Düzeltme: kabuk görsellerinde NPOT'ta mipmap kapalı, en çok 2048, Android ASTC 5×5/4×4 (`ShellAssetImporter`); taş PNG'leri saydam payla 512×512'ye tamamlandı (mipmap + ASTC 4×4); bulanık taşlar 256 (ASTC 6×6); 3D model dokuları Android ASTC 6×6. `BuildScripts.ApplyTextureBudget` bu klasörleri atlar (çekişme yok). Kalan büyük dokular eski PremiumV4 varlıkları (maskot, modal panel, satin şerit; aynı NPOT+mipmap sorunu) — taş/maskot görselleri olduğu için dokunulmadı; istenirse ~20 MB daha kazanılır.
- Kullanıcı istekleri (2026-11-07, devam 2):
  - Ana sayfa başlığı: "BÖLGE n / bölge adı / aralık" kaldırıldı; altın plakada ZPE (etikette "ZPE", ortada puan ya da "Kalibrasyon x/10", altında "Zekâ Performans Endeksi"; dokununca istatistik paneli). Soldaki ayrı ZPE kapsülü kaldırıldı.
  - Yıldız eşiği göstergesi sağda dikey kart: üstte kazanılacak taş buzlu çerçevede (buzun üstünde kilit), altında taşın renginde dolan seviye çubuğu ve "yıldız/eşik"; dokununca Koleksiyon.
  - Taş kırılma efekti (`View/PeelSequence.cs`, `View/StoneShatter.cs`): kâğıt kıvrılması yerine çatlak → taşın kendi renginde yontulmuş parçalar + toz → dağılma → hedefe uçuş. Görev rengiyse parçaların yarısı görev simgesine, yarısı yıldız (puan) çubuğuna; görev değilse hepsi yıldız çubuğuna (`HudView.IsGoalColor`, `HudView.ScoreTarget`). Süreler aynı (zincir zamanlaması değişmedi). Taş yüzü çizimleri/atlas/shader değişmedi. Kareler `docs/captures/v10/oyun/sonra_kirilma_*`.
  - Testler: EditMode 176/176, PlayMode 30/30. Tam PlayMode (1000 bölüm) kırılma efektiyle yeniden koşulmadı; telefonda denenmedi.

## 2026-11-08 v11 (rapor: [v11/V11_RAPOR.md](v11/V11_RAPOR.md))

- Mavi-beyaz ana sayfa (`docs/v11` seçilen tasarım): mavi üst panel, beyaz bölge kartı (fener, canlı bölge adı, "BÖLGE n • ilk–son"), krem yıldız kapsülü, düz mavi alt menü (seçili sekmede krem kart, Taşlar sekmesinde kullanıcının elmas simgesi). 3 sekme. Önce/sonra: `tools/unity.ps1 capture-v11` → `docs/captures/v11/`.
- ZPE: 100 üzerinden, oynanan bütün normal bölümlerin düz ortalaması (kayıtta `zpeSum`/`zpeGames`), ilk bölümden görünür; ana sayfada kilitli taş kartının üstünde özel gösterge simgesi + "72/100".
- Kilitli taş: bulanıklık yoğunlaştırıldı (22 taş), ana sayfa kartında da bulanık, kilit çerçevenin ortasında.
- Özel güç simgeleri (renk temizleyici, yatay, dikey) yeni PNG'lerle, yüz merkezinde, dikey döndürülmeden; yatay/dikey ateşlenince simge ikiye bölünüp yarılar zıt yönlere gider. Rulo için kullanıcının ürettiği yeni simge (`skiller/04-rulo.png`).
- Güç alanı: krem kabuk, yuva ve bulut yok; güçler doğrudan zeminde, büyük, kendi renklerinde ışıltılı, yerinde büyüyüp küçülür. Günlükte seçili koleksiyon taşı bu alanda kendi PNG'siyle; dokununca gücü kullanılır.
- PlayMode bölüm testi: geliştirmede her 100 bölümden rastgele 5 (50 bölüm, ~3 dk); bütün bölümler yalnız mağaza derlemesi öncesi (`test-play -All`).
- Testler: CoreTests 178/178, EditMode 178/178, PlayMode 41/41 (+1 atlanan elle ölçüm; 50 bölüm örneklemi, tohum 320648828), `loc-check` TAMAM. Telefonda denenmedi (ışıltı/nefes hareketi, bölünme akıcılığı, ses).

## 2026-11-08 v12 (devam) — rapor: [v12/SIRALAMA_RAPOR.md](v12/SIRALAMA_RAPOR.md), plan: [v12/PLAN.md](v12/PLAN.md)

- Sıralama ekranı (onaylı referans): Dünya / ülke sekmeleri, podyum 2-1-3, ilk 10, sabit "Senin sıran" kartı, bütün durumlar. Haritada kupa düğmesiyle açılır. Puan %50 ZPE + %30 bölüm + %20 yıldız (0–1000, 20 bölüm eşiği). Sunucu (Firebase) bağlı değil: oyuncu "bağlı değil" görür; örnek veri yalnız geliştirmede ve etiketli.
- Reklam altyapısı: günlük giriş (5 sn arama, yoksa reklamsız), kayıp kartında +3 hamle (ZPE'ye girmez), Devam'da geçiş reklamı; oyunda TEST reklam ekranı. Gerçek SDK hesap gelince.
- Font: bütün oyunda Nunito (ana 800, ikincil 600); kilit sayacı da Nunito.
- Testler: CoreTests 189/189, EditMode 189/189, PlayMode 45/45 (+1 atlanan). Derleme (2026-11-08 03:13): `Builds/Android/TriKat.apk` 119,8 MB (önceki 107,9; sıralama görselleri), `Builds/Windows/TriKat.exe` 12 sn açılış denemesi hatasız. Telefonda denenmedi.

## 2026-11-08 v12 deneme seti — rapor: [v12/DENEME_RAPOR.md](v12/DENEME_RAPOR.md)

- Haritada sol sütunda **Deneme** düğmesi → 10 bölümlük deneme listesi (her türden bir + patron). Ana bölümler, yıldız, ZPE, sıralama etkilenmez.
- Motor: kristal hücre (`protected`, soyulursa `LostProtected`), zincir hedefi (`collectChain`). Kural kartı, kalıcı kural simgeleri, uzun basınca katman görünümü (yalnız denemede), 2 kayıptan sonra istenirse bir hamlelik ipucu.
- Üretim: `tools/LevelLab trial` → `Resources/Trial/`, ölçümler `docs/v12/DENEME_URETIM.md`.
- Testler: CoreTests 193/193, EditMode 193/193, PlayMode 47/47 (+1 atlanan), loc-check TAMAM. Derleme 03:41: APK 119,9 MB, exe açılış denemesi hatasız. Telefonda denenmedi.

## 2026-11-08 ui1 arayüz paketi — analiz ve düzeltmeler: [ui1/uygulama_sonuclari/FARK_ANALIZI.md](ui1/uygulama_sonuclari/FARK_ANALIZI.md)

- `docs/ui1` uygulandı. Ana sayfada yalnız ZPE simgesi değişti; 02–17 her sayfanın `yerlesim.json` ölçülerine göre ayarlandı. Ortak kit: `Scripts/UI/Ui1.cs`; sprite'lar: `Resources/UI1`.
- Kullanıcı kararı: oyun içi hamle kapsülü mor (krem sprite'ın mor türevi, `tools/ui1_mor_kapsul.ps1`).
- Yakalamalar: `tools/unity.ps1 capture-ui1` → `docs/ui1/uygulama_sonuclari/*.png`. Tasarımla kutulu karşılaştırmalar: `karsilastirma/NN.jpg`.
- Yeni dizgiler: `collection.unlock.stars`, `daily.power.caption` (9 dil).
- Testler: CoreTests 193/193, EditMode 193/193, PlayMode 47/47 (+1 atlanan), loc-check TAMAM. Testlerden sonra yalnız 08'de metin konumu oynatıldı (derlendi, yakalandı).
- 17: günlük başlığı plakası büyütüldü, hamle/yıldız/hedef grubu aşağı kaydı (başlık artık sığıyor). Günlük bulmaca performansa göre 1–3 yıldız veriyor (günde bir kez; toplam yıldıza ve taş eşiklerine sayılır, sıralamaya sayılmaz). Testler: CoreTests 194/194, EditMode 194/194, loc-check TAMAM. PlayMode 48/48 (+1 atlanan).
- Derleme 05:27: APK 123,6 MB (`Builds/Android/TriKat.apk`), exe 05:18 (`Builds/Windows/TriKat.exe`, 12 sn açılış denemesi: "Boot tamam", hata yok). Telefonda denenmedi; `TestUnlockAll` hâlâ açık.
- Cihazda ve farklı ekran oranlarında bakılmadı.

## 2026-11-08 v8 bölüm seti — rapor: [v12/V8_RAPOR.md](v12/V8_RAPOR.md)

- Deneme türleri 1000 bölüme yayıldı (295 türlü, 705 v7). Günlük daily-v3 (240'ın 235'i türlü). v7 ve daily-v2 arşivde (`KatKat_Arsiv_2026-11-06/bolumler_v7_2026-11-08`, `gunluk_v2_2026-11-08`).
- Doğrulama: LevelLab report 1000/1000, audit (zorluk + uygunluk) 1000/1000, stress 60 1000/1000; CoreTests 194/194, EditMode 194/194, PlayMode tam 48/48, loc-check TAMAM.
- Ana sayfa: ZPE sol sütunun en üstünde; kilitli taş kartı sağda en üstte.
- Derleme 11:14: APK 123,6 MB (`Builds/Android/TriKat.apk`), exe 11:09 (12 sn açılış denemesi: "Boot tamam", hata yok). Cihazda oynanmadı; kural kartı/ipucu görsel olarak denenmedi; `TestUnlockAll` hâlâ açık.
- Kabuk zeminleri (kullanıcı isteği): üst mavi panel çizgili zemin `docs/v11/arayuz/01-mavi-panel-zemini_üst.png` → `Resources/PremiumV6/shell/v11-panel-top.png`; alt menü düz zemin `01-mavi-panel-zemini-menü.png` (zaten `v11-panel.png` ile aynı dosya). `MenuView.Shell.cs` `V11Panel(..., top)`; görsel yoksa düz zemine düşer. Yakalama: `docs/ui1/uygulama_sonuclari/01_harita_ana_sayfa.png`. Bu değişiklikle exe/APK yeniden derlenmedi.
- ui3 onaylı tasarım uygulandı (docs/ui3_uygulama/RAPOR.md): 35 ekran `docs/ui3_uygulama/`, maketle yan yana `karsilastirma/`. Araçlar: `tools/ui3_hazirla.ps1`, `tools/unity.ps1 capture-ui3`, `tools/ui3_karsilastir.ps1`. Testler: EditMode 194/194, PlayMode (örneklem) 48/48 + 1 atlanan. Cihazda ve farklı ekran oranlarında denenmedi; exe/APK derlenmedi.
- Derleme 18:05 (ui3 tasarımı): APK 130,9 MB (`Builds/Android/TriKat.apk`, 18:04), exe 17:58 (30 sn açılış denemesi: "Boot tamam", hata yok). Cihazda oynanmadı; `TestUnlockAll` hâlâ açık.
- APK temizliği (kullanıcı, 2026-11-08: "gereksiz kullanılmayan şeyleri yok et"): güncel tasarımda hiç yüklenmeyen 45 Resources görseli (eski v10/v11 kabuk, eski sıralama paketi, eski koleksiyon sayfası, ui3'e eşlenen UI1 kopyaları, eski ana sayfa/zafer görselleri; 28,8 MB kaynak) `KatKat_Arsiv_2026-11-06/apk_temizlik_2026-11-08/Resources` altına taşındı. Kullanım `tools/unity.ps1 usage-ui3` ile ölçüldü (35 ekran, mekanik bölümleri, bütün bölge haritaları, koleksiyon, vitrin) ve koddan tek tek doğrulandı; sesler, logo, taş modelleri, hazine görselleri, Altın Rulo korundu. APK 130,9 → 114,5 MB (18:37). Testler EditMode 194/194, PlayMode 48/48 (+1 atlanan); 35 ekran öncekiyle aynı (fark yalnız rastgele oyun zemini).
- Hesap/sıralama (kullanıcı, 2026-11-09): sıralama Firebase, ad Play Games adı. Bağlantı kodu `Scripts/Online/` (yalnız `KATKAT_ONLINE` ile derlenir; şu an kapalı, oyunda test hesabı), Firestore kuralları `firebase/`. Eklentiler indirildi, projeye eklenmedi: `ThirdParty/indirilen/` (PGS 2.3.0, Firebase 14.1.0 app/auth/firestore, EDM 1.2.190). Açmak için Play Console + Firebase yapılandırma dosyaları bekleniyor (`docs/v13/KAYIT_PLANI.md` §8).
- Derleme 2026-11-09 00:30: APK 114,4 MB (`Builds/Android/TriKat.apk`): zincir v13, reklamlı ipucu, özel taş sunumu, yeni açılış ve ikon. EditMode 194/194. Cihazda denenmedi; `TestUnlockAll` hâlâ açık; reklamlar sahte; hesap test hesabı.
- iOS hazırlığı (kullanıcı, 2026-11-09: iOS ilk yayın): iOS ayarları, ASTC dokular, Game Center yetkisi, yerel titreşim/ülke eklentisi, Game Center hesap kodu (kapalı), `tools/unity.ps1 build-ios` (Windows'ta Xcode projesi çıktı: 910 MB, hata yok), `.github/workflows/ios.yml` (çalıştırılmadı). IPA yok, iPhone'da denenmedi. Ayrıntı ve kullanıcı adımları: `docs/v13/IOS_PLANI.md`.
- Reklamlı ipucu: önerilen taş oynanana kadar kalıcı, düğmeyle reklamsız yeniden gösterilir; altın halka. PlayMode testi `PaidHint_StaysUntilHintedTileIsPlayed_ButtonShowsItAgain`.
- Derleme 2026-11-09 00:58: APK 114,4 MB (`Builds/Android/TriKat.apk`), kalıcı reklamlı ipucu dahil. EditMode 194/194, PlayMode 49/50 (+1 atlanan). Cihazda denenmedi.
- Reklam (2026-11-09): AdMob iOS uygulaması, ödüllü + geçiş birimleri, GDPR ve IDFA mesajları yayında; Unity Ads uygulaması ve placement'ları; AdMob uyumlulaştırma grupları (teklif sistemi: AdMob + Unity Ads). Oyunda iOS gerçek kimlikler, Unity Ads adaptörü 3.22.0. Ayrıntı `docs/v13/YAYIN_EKSIKLERI.md`. Cihazda denenmedi; cihaz kodu henüz derlenmedi.
- Firebase (2026-11-09): proje `stone-burst` (eur3), Game Center girişi, Firestore kuralları ve ülke dizini yayında. Oyunda Firebase 14.1.0 paketleri (`Packages/firebase`), `KATKAT_ONLINE` yalnız iOS. iOS Xcode dışa aktarımı derleniyor (Firebase SPM, AdMob/Unity Ads CocoaPods). Cihazda denenmedi.
- GitHub (2026-11-09): depo https://github.com/ofigame/stone-burst (public, ofigame hesabı), ilk commit `39e422e`. 11 Actions gizli anahtarı girildi (kullanıcı). İlk iOS iş akışı çalıştırıldı (TestFlight yüklemesi kapalı): https://github.com/ofigame/stone-burst/actions/runs/42548932970 — sonucu henüz bilinmiyor.
- Oyun içi düzeltmeler ve dil denetimi (2026-11-09 sabah, kullanıcı isteği):
  - Geri al / durdur düğmelerinin simgesi basınca kaybolmuyor (`PressFeedback` yalnız basılı görsel varsa değiştiriyor). PlayMode testi `HudRoundButtons_KeepIconSpriteAfterPress` geçti.
  - İpucu göstergesi belirgin: altın çift çerçeve, nabız halkası, parıltı ve zıplayan ok (`TapHint` güçlü kip).
  - Bilgi şeridi hedef taşlarla tahta arasında; yazı sarılıp küçülüyor, tahta şeride yer ayırıyor.
  - Dil yakalamaları 9 dilde (`docs/ui3_uygulama/diller/<dil>/`, `KATKAT_CAPTURE_LANG`). Arapça: şekillendirilmemiş metinler, bozulan renk etiketleri, Ellipsis yüzünden boş görünen etiketler ve ayarlar dil etiketi düzeltildi. Almanca: ad büyük harfi ("Keine Züge mehr", "Gratis-Versuch"). Günlük kart satırları panel kenarına dayanıyordu (de, es); daraltıldı.
  - 90. bölüm: kayıtlı 13 adımlık çözüm kazanıyor; bot 200 oyunda 10 kazandı (%5). Geçilebilir ama çok zor; kolaylaştırma kararı kullanıcıda.
  - Testler: EditMode 194/194, PlayMode 50/51 (+1 atlanan). Cihazda denenmedi.
  - CI: iOS iş akışı run 5 (https://github.com/ofigame/stone-burst/actions/runs/42555311028) başarılı; imzalı IPA (~161 MB) artifact olarak üretildi, TestFlight yüklemesi kapalıydı. Düzeltmeler: `-customBuildPath`, macos-26 + Xcode 26.6 (Firebase SDK 13), Library önbelleği kaldırıldı (137), dağıtım profili yalnız Unity-iPhone hedefinde.
- Kalp, reklam ve öğretici turu (2026-11-09 öğle, kullanıcı isteği; kararlar KARARLAR.md):
  - Kalp sistemi: `Levels/Meta/Hearts.cs` (saf mantık), `Game/GameController.Hearts.cs`, `UI/MenuView.Hearts.cs` (kalp göstergesi, kalp kartı, koddan kalp görseli).
  - Kayıpta reklam: +1 hamle ve taş yerleştirme (`GameSession.PlaceLayer`, `HudView.Place.cs`, `GameController.Ads.cs`).
  - Öğretici sadeleştirildi; geçiş reklamı seyreltildi (`ad_config.json`); reklam düğmelerine oynat simgesi.
  - iOS: Oyun Modu için Info.plist oyun kategorisi; App Store Connect'te Game Center açıldı. Telefonda denenmedi.
  - TestFlight (2026-11-09 14:42): run 9 (https://github.com/ofigame/stone-burst/actions/runs/42607190257, commit `5c72dd2`, yapı no 9) "UPLOAD SUCCEEDED" ile yüklendi. Run 8 derlendi ama yükleme kutusu işaretsiz gittiği için yüklenmedi. Apple işlemesi, sürüm 1.0'a bağlama ve telefonda deneme henüz yapılmadı.
- Otomatik bulut kaydı ve geri yükleme (2026-11-09 akşam; kural OYUN.md "İlerleme kaydı ve bulut", karar KARARLAR.md):
  - Önceden buluta yalnız Ayarlar düğmesiyle yazılıyordu, buluttan okuma hiç çağrılmıyordu. Şimdi girişten sonra bulut okunur, ilerideyse geri yüklenir (haritada); sonra her kazanımda ve arka plana geçişte yüklenir. Okumadan önce yazılmaz.
  - Dosyalar: `Save/CloudSync.cs` (yeni), `SaveService.Replace`, `GameController.Account.cs`, `CreateDaily` (geri yüklemeden sonra günlük hizmet yeniden bağlanır), metin `acct.restored` (9 dil).
  - Testler: EditMode 197/197, PlayMode 56/57 (+1 atlanan; 50 bölümlük örneklem), yeni `CloudSyncTests` (3) ve GameFlow bulut testleri (2) geçti; loc-check temiz. Gerçek Game Center/Firestore ile telefonda denenmedi; TestFlight'taki yapı 9 bu değişikliği içermiyor.
- Mağaza ekran görüntüleri ve iPad yerleşimi (2026-11-09 akşam; kararlar KARARLAR.md, yükleme adımları v13/MAGAZA_METINLERI.md):
  - iPad'de arayüz taşıyordu (genişliğe eşleme). Geniş ekranda artık yüksekliğe eşlenir, ortada telefon genişliğinde sütun (`UI/UiAspect.cs`; SafeAreaFitter, HudView tahta alanı, kamera, CanvasScaler'lar, yakalama aracı). Telefonda değişiklik yok.
  - 72 görsel üretildi: 2 cihaz × 6 dil × 6 ekran, `docs/magaza/` (git dışında, ~160 MB; ham çekimler `docs/magaza/ham/`). Görsel olarak kontrol edildi. iPhone seti 1206×2622 olarak yeniden üretildi (App Store Connect zorunlu alanı). Yükleme denendi: uygulamanın tarayıcısı dosya seçemiyor, sayfa yerel sunucuya bağlanmıyor; yüklenmedi.
  - Testler: EditMode 197/197, PlayMode 56/57 (+1 atlanan; 50 bölümlük örneklem). Gerçek iPad'de denenmedi.
  - Yakalamalar: `docs/ui3_uygulama` 01, 15, 42–45 (tr) ve ar/de/ja.
  - Tam test (2026-11-09 sabah): CoreTests 197/197, EditMode 197/197, PlayMode 51/52 (+1 atlanan; 1000 bölümün hepsi gerçek kontrolcüyle, 56 dk), LevelLab report 1000/1000, stress 60 1000/1000 (60.000 oyun), loc-check ve bölüm doğrulama temiz. Cihazda denenmedi.
  - App Store Connect girişleri: `docs/v13/MAGAZA_METINLERI.md` sonu.
