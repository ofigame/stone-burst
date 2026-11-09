# Stone Burst — Oyun Dokümanı

Son güncelleme: 2026-11-06 · Unity 6000.6.4f1 · Android (APK) ve Windows (önizleme exe)

## 1. Oyun nedir?

Stone Burst (2026-11-09; önceki adları TriKat ve Kat Kat), üst üste dizilmiş renkli taş katmanlarını soyarak hedef toplanan, sıra tabanlı bir bulmaca oyunudur. Tahtadaki her hücrede bir **taş yığını** vardır; yalnız en üstteki katman görünür ve seçilebilir. Oyuncu aynı renkte en az **3 bitişik** üst katmana dokunur, o grubun üst katmanları birlikte soyulur ve alttaki renkler ortaya çıkar. Amaç, sınırlı hamle içinde bölümün hedeflerini (belirli sayıda renk, yıldız, zarf, sünger…) toplamaktır.

- **Tür:** sakin, düşünceli eşleştir-soy bulmacası; zaman baskısı yok.
- **Bölüm sayısı:** 1000 (kayıtlı çözümle doğrulanmış).
- **Dil:** 9 dil (Crate Boss ile aynı set): Türkçe (kaynak), İngilizce (yedek), İspanyolca, Arapça (sağdan sola), Almanca, İtalyanca, Fransızca, Japonca, Korece. İlk açılışta cihaz dili, sonra Ayarlar → Dil. Metinler `Resources/Localization/strings.txt`; denetim `tools/unity.ps1 loc-check`. Türkçe dışındaki çeviriler taslaktır (ana dili olan biri gözden geçirmeli).

## 2. Temel kurallar

| Kural | Açıklama |
|---|---|
| Grup | Aynı renkte en az 3 kenar-bitişik üst katman. Dokunulan grubun her yığınından **bir** katman soyulur. |
| Hamle | Her geçerli dokunuş 1 hamle. Hamle bitince ve tepside kullanılabilir özel taş yoksa bölüm kaybedilir. |
| Zincir (v13, 2026-11-08) | Soyduğun taşların altından **yan yana en az 3 aynı renk** taş birlikte çıkarsa onlar da kendiliğinden patlar; dalga dalga sürer, hamle harcamaz. Yalnız o dalgada altından yeni renk çıkan taşlar sayılır; yanlarındaki eski açık taşlar ne engeller ne katılır. (Önceki kural: yeni grubun tamamı yeni olmalı ve eski aynı renge değmemeli; arşivde.) |
| Hedef | Renk sayısı, dip yıldızı, zarf ve sünger hedefleri. Hepsi dolunca bölüm kazanılır. |
| Geri al | Son tam hamle (bütün zincirleriyle) geri alınabilir. |

### Taşlar (4 renk, renk + şekil ile okunur)

| Kimlik | Ekranda | Şekil |
|---|---|---|
| Coral | Mercan | Daire |
| Blue | Turkuaz | Üçgen |
| Yellow | İnci (beyaz) | Çift dalga |
| Purple | Mor | Elmas |

Veri kimlikleri (`Coral/Blue/Yellow/Purple`, `Rainbow`) eski adlarıyla kalır; ekranda inci ve "Altın Rulo" görünür.

## 3. Özel taşlar ve tepsi (v6)

- **5–7 taşlık grup →** Rulo roketi. Grup yatay genişse **satır roketi**, değilse **sütun roketi**.
- **8+ taşlık grup →** **Altın Rulo**.
- Doğan özel taş tahtada durmaz; ekranın sol altındaki **tepsiye** uçar (en çok **4** yuva; doluysa yalnız puan).
- **Kullanım:** tepsideki taşa, sonra tahtada bir taşa dokun.
  - Roket o taşın satırını / sütununu soyar, örtülere (buz, ataş…) vurur.
  - Altın Rulo dokunulan taşın **rengindeki** bütün açık üst katmanları toplar.
- Tepsi kullanımı **hamle harcamaz**. Hamle bitse de tepside işe yarayan taş varsa oyun sürer.
- Kazanınca tepside kalan özel taşlar ve kalan her hamle (roket olarak) **bitiş kutlamasında** ateşlenir (puan).
- Görünüm (2026-11-08): tepsideki taşlar alt çubuktan biraz yukarıda, sürekli altın hale ve çevresinde dönen ışıltılarla durur. Seçili taş büyür, yükselir, dönen altın halkayla belirginleşir. Roket ateşlenince önce yerinde büyüyüp parlar (0,26 sn), sonra ikiye bölünüp altın parıltıyla yavaşça uçar (hücre başına 0,1 sn). Altın Rulo patlatacağı her taşa kaynaktan titreşen bir yıldırım atar.

### Reklamlı ipucu (2026-11-08)

- Normal bölümde alt çubukta ampul düğmesi; üzerinde bedeli gösteren "−1 ★" rozeti.
- Ödüllü reklam izlenince sıradaki önerilen dokunuş halkayla gösterilir. Hamle önerisini önce kısa bütçeli çözücü bulur; bulamazsa hedefe en çok katkı yapan dokunuş seçilir. Halka, oyuncu bir hamle yapınca söner.
- Deneme başına 1 ipucu. Bedel: o bölümden kazanılan yıldız 1 azalır (en az 1). Reklam yardımlı oturum sayıldığı için sıralama yıldızına yazılmaz (reklam kuralı).

## 4. Mekanikler ve bölüm takvimi

| Bölüm | Mekanik | Kısa kural |
|---|---|---|
| 1–5 | Öğretici, rulo | Temel soyma; 5'li grup rulo yuvasını doldurur, rulo bir satırı soyar. |
| 6+ | Özel taşlar ve tepsi | Roket, Altın Rulo (yukarıda). |
| 21+ | Ataş | Ataşlı yığın seçilemez; yanında soyma yapılınca ataşa vurulur (1–2). |
| 41+ | Dip yıldızı | Yığın tamamen boşalınca yıldız toplanır. |
| 61+ | Buz | Kenar komşusunda soyma olunca kırılır (1–2 vuruş). |
| 121+ | Renk kilidi | Kendi renginden belirtilen sayıda katman toplanınca açılır. |
| 201+ | Zarf | Yanında mühür renginde katman soyulunca açılır, hedefe uçar. |
| 301+ | Döner taban | Yığının katman sırasını döndürür (1 hamle). |
| 401+ | Sünger | Yanından istenen renkler soyulunca dolar, toplanır. |
| 501+ | Bağlı ataş | Aynı gruptaki ataşlar birlikte hasar alır. |
| 601+ | Düğme / geçit | Dipteki düğme basılınca kapalı geçit hücreleri açılır. |
| 701+ | Boya damlası | Bir yığının üst rengini değiştirir (1 hamle, sınırlı). |
| 801+ | Karışım | Önceki mekaniklerin birleşimi. |

**Tahta şekilleri:** Bölümler dikdörtgen değil; karelerden oluşan dış hatlar: kesik köşe, L, T, artı, merdiven, kum saati, U, çatı, elmas (aynalı). 21. bölümden sonra her üçüncü bölüm (n % 3 = 0, art arda gelmez) **ortası boş** tahtadır: **halka** (ortada 2×2…3×3 boşluk), **çift göz** (yan yana ya da üst üste iki ayrı boşluk), **kesik halka** (ortada boşluk + iki çapraz köşe kesik). Boşluk her yönde en az bir sıra taşla çevrilidir, tahta kenarına değmez, tahta tek parça kalır; mekanik hücreleri oyulmaz (gerekirse boşluk bir hücre kayar, olmazsa dış hat şekli seçilir). Oyun, kapalı boş bölgeyi kendisi bulur ve içine süs (maskot, fener, kaya) çizer. Bunun dışında açılışta tahta içinde boşluk yoktur.

### Bölüm türleri (v8, 2026-11-08)

Kural verilir, strateji verilmez. v7 tahtaları korunur; takvimdeki bölümler bir **tür** alır (`docs/v12/V8_URETIM.md`):

| Tür | Kural (oyuncuya gösterilen) | İlk bölüm |
|---|---|---|
| Kesin Hamle | Hamle sınırı = çözüm uzunluğu (1–99'da +1) | 27 |
| Ters Düşünme | Kesin hamle; göz alıcı (açgözlü) hamle kaybettirir | 37 |
| Tek Dokunuş | Küçük tahta, 1 hamle; doğru taş zinciri başlatır | 49, sonra her 50'de (99, 149…) |
| Zincir Ustası | Hedef yalnız zincirle soyulan taşlarla dolar | 47 |
| Kristal Koruma | Kristal yığından katman soyulursa kayıp (zincir/roket dahil) | 57 |
| Renk Kıtlığı | Hedef rengin neredeyse hepsi gerekir | 67 |
| Saf Plan | Özel taş doğmaz | 77 |
| Doğru An | Özel taşsız çözülemez; kesin hamle | 87 |
| Kilit Sırası | Renk kilitleri doğru sırayla açılmalı | 127 |
| Patron | Her 10. bölüm: kurallar birleşir + kesin hamle; her 100. bölüm 3 kural | 30 |

- 10'luk döngü: 7. ve 8. bölüm tür bölümü, 10. bölüm patron; diğerleri normal (v7 aynen).
- Kural simgeleri hedef kabuğunun sağında (dokununca kural cümlesi). Kural kartı yalnız türün ilk görüldüğü bölümde ve patronlarda (`meta.seenTypes`).
- Aynı bölümde 2 kayıptan sonra kayıp kartında **İpucu**: bölüm baştan başlar, yalnız ilk doğru hamle gösterilir (ZPE'de yardımlı).
- Yığına basılı tutunca (0,42 sn) bütün katmanlar görünür; dokunuş parmak kalkınca işler (bütün oturumlarda).
- Günlük bulmaca (`daily-v3`): günün türü haftalık döner (kesin hamle → zincir → kristal → ters → kıt renk → saf plan → doğru an → kilit); oyuncunun grubunda açılmamış tür atlanır; her günlükte kural kartı.

## 5. Puan ve yıldız

| Olay | Puan |
|---|---|
| Katman (dalga *i*) | 10 × (1 + *i*) — domino uzadıkça her katman değerlenir |
| Örtü vuruşu | 20 |
| Zarf, dip yıldızı, sünger, geçit | 50 |
| Roket doğumu | 50 |
| Altın Rulo doğumu | 100 |

Yıldız eşikleri bölüm başına kayıtlı çözüm + kutlama puanından hesaplanır: kazanmak 1 yıldız, eşiğin %75'i 2 yıldız, tamamı 3 yıldız.

**Günlük bulmaca yıldızı (2026-11-08):** günlük bulmaca da aynı eşiklerle 1–3 yıldız kazandırır, günde bir kez (günün ilk galibiyeti; ücretsiz tekrar yeni yıldız vermez). Bu yıldızlar toplam yıldıza (harita, yıldız kapsülü, koleksiyon taşı eşikleri) eklenir (`meta.dailyStars`), sıralamaya sayılmaz (günlüğe reklamla giriliyor).

## 6. Zorluk

Bölümler `LevelLab rebuild` (v7) ile üretildi. Zorluk hamle kıtlığından değil **plandan** gelir: hamle limiti, geniş ışın aramasının bulduğu çözüme makul pay eklenerek konur (6–20: +4–6, 21–60: +3–5, 61–300: +3–4, sonrası +2–4; x1–x2 bölümlerinde +1; bot çok zorlanırsa pay en çok +10'a açılır). Renk hedefi çarpanı, yumuşak açgözlü botun (24 oyun) kazanma oranı hedefe yaklaşacak biçimde seçilir: 6–15 ~%60, sonra v6'dan 0,06 daha düşük; ölçülen ortalama 6–20 %58, 21–100 %49, 101–300 %33, 301–600 %23, 601–1000 %17 (hiçbir bölüm %8'in altında değil). Hedefe yakın adaylar arasında açgözlü oyuncunun (ışın 1) ya da dar ışının (3, 10) çözemediği, yani doğru sıra / zincir kurma / tepsiyi doğru anda kullanma isteyen bölüm tercih edilir; her bölümün kaynağındaki `plan 0–3` bu derinliği yazar. Her 10'un katı bölüm biraz zor, x1–x2 bölümleri nefes alır. Her bölümün kayıtlı çözümü kazandığı ve botun en az bazen kazandığı otomatik doğrulanır (`LevelLab stress`).

## 7. Ekranlar ve görünüm

- **Ana sayfa / harita:** Üstte iki satırlı Stone Burst logosu ve sağında Ayarlar dişlisi, bölge kartı (bölge simgesi, BÖLGE n, bölge adı, aralık, toplam yıldız). Kesintisiz dikey yol: üç bölge (sahil, bahçe, çöl) kendi el çizimi şeridiyle döner; bölgeler nehir ve köprüde birleşir. Düğümler resimdeki yolun ortasından geçer. Açılmamış bölümler numarasız ve doğal bulut örtüsü altında; ilerledikçe bulutlar açılır. Maskot mevcut bölümün üstünde zıplar; kaydırınca "bölümüme dön" düğmesi çıkar.
- **Alt menü:** Harita · Günlük · Taşlar (2026-11-06; Oyna kalktı, bölüme haritadaki düğümden girilir; Ayarlar sağ üstte). Günlük ve Taşlar ekranlarında sol üstte geri düğmesi, sağ üstte Ayarlar.
- **Açılış:** Beyaz zeminde OFIGAME logosu (~1,6 sn; oyun sahnesi arkada yüklenir). Kaynak: `E:\OFIGAME\Kurumsal\ofigame-logo-acik-zemin.png` → `Resources/Branding/ofigame-logo.png`.
- **Oyun ekranı:** Üst kart (hamle hapı + hareketli maskot, yıldız çubuğu, duraklat), hedef kartı (çipler), ortada şekilli tahta (açık pastel tepsi, bölüme göre renk), altta özel taş tepsisi ve geri al (ekran altından 70 birim yukarıda). Arka plan bölgenin bulanık resmi.
- **İçi boş tahtalar:** Tahta şeklinin içinde kalan boşluklara hareketli süs konur (`BoardDecor`): zıplayan maskot, ışığı dönen fener (sahil), su sıçratan çeşme (bahçe), sallanan kaya kemeri/vaha (çöl). Bölüm numarası ve tema seçer; iki boşlukta biri maskot olur. Süs yalnız görseldir.
- **Öğretici turlar:** Odak alanı açık, çevresi karartılmış; yanında maskotlu konuşma kartı ve zıplayan ok (`TutorialOverlay`, `GameController.Tutorial`). Bölüm 1–3 baştan sona yönlendirmeli (yalnız gösterilen gruba dokunulur; 1'de ilk hamleden sonra hedef kartı ve hamle sayacı tanıtılır). Bölüm 6 (5'li grup → roket tepsiye), 7 (tepsi kullanımı iki adım), 9 (Altın Rulo), 12 (biriktirme). Her mekaniğin ilk çıktığı bölüm: o mekanikli hücre açık, kısa açıklama. Bilgi adımı ekrana dokununca geçer; tur bölüm kazanılınca bir daha gösterilmez.
- **Ayarlar:** Resimsiz; turkuaz→krem renk geçişi, süzülen kabarcıklar. Krem kartta Ses ve Titreşim anahtarları, Dil (dokuz dil, her biri kendi yazısıyla), kısa not; altta sürüm. Dil değişince ekranlar yeni dille yeniden kurulur.
- **Yazı tipi:** Nunito ExtraBold (başlık, düğme, sayılar) ve Nunito SemiBold (açıklama), OFL lisanslı. Arapça için Noto Sans Arabic, Japonca/Korece için Noto Sans KR yedek fontları. Hap düğmelerde yazı yüksekliğin ~%32'si, uzun metin sığana dek küçülür.
- **Maskot:** Kristal maskot; gözleri etrafa bakar, göz kırpar; zincir, özel taş ve yıldızda zıplar; son 1–3 hamlede endişelenir.
- **Uygulama ikonu:** Bulanık mavi su üstünde karşıya bakan maskot (Android uyarlanabilir ikon).

## 8. Proje yapısı

| Yol | İçerik |
|---|---|
| `Assets/KatKat/Scripts/Core` | Kural motoru (Unity'siz, deterministik): `RulesEngine`, `LevelState`, puan. |
| `Assets/KatKat/Scripts/Levels` | Bölüm verisi, ayrıştırma, doğrulama, çözücü. |
| `Assets/KatKat/Scripts/View` | Tahta, yığın, soyulma animasyonu, özel görseller. |
| `Assets/KatKat/Scripts/UI` | HUD, menü/harita, maskot, paneller. |
| `Assets/KatKat/Scripts/Game` | `GameController` (akış), `Bootstrap`, tam ekran. |
| `Assets/KatKat/Resources` | Oyunda yüklenen görseller (`PremiumV4`, `PremiumV6`, `Premium`), bölümler (`Levels/*.json`), shader, yazı tipi. **Yalnız kullanılanlar** (2026-11-06 temizliği). |
| `Assets/KatKat/Art` | Uygulama ikonu katmanları. |
| `levels_src/` | Bölüm kaynakları (metin). JSON'lar bunlardan üretilir; değişiklik buraya yapılır. |
| `tools/LevelLab` | Bölüm aracı: `build`, `report`, `rebuild` (şekil + zorluk + çözüm), `stress` (bütün bölümlere bot oyunları + değişmez denetimi). |
| `tools/ArtV6/prepare.ps1` | `docs/gorseller` kaynaklarını oyuna hazırlar (atlas, kırpma, harita şeridi + yol çizgisi). |
| `tools/unity.ps1` | Unity komutları: `test-edit`, `test-play`, `build-android`, `build-windows`, yakalamalar. |
| `docs/gorseller/` | Kullanılan görsellerin kaynakları (bkz. `docs/gorseller/README.md`). |

## 9. Sık işler

```text
Bölüm kaynağı değişti:     cd tools/LevelLab ; dotnet run -c Release -- report
Görsel değişti:            powershell -File tools/ArtV6/prepare.ps1
Testler:                   cd tools/CoreTests ; dotnet test      ve   tools/unity.ps1 test-edit
Bölüm sınaması:            cd tools/LevelLab ; dotnet run -c Release -- stress 60
APK:                       tools/unity.ps1 build-android   → Builds/Android/KatKat.apk
Exe:                       tools/unity.ps1 build-windows   → Builds/Windows/KatKat.exe
```

Android derlemesinde görseller ASTC ile sıkıştırılır (`BuildScripts.ApplyTextureBudget`), ikon katmanları ve tam ekran (sanal tuşlar gizli, kaydırınca geçici) uygulanır.

## 10. Yayından önce

- `GameController.TestUnlockAll` → `false` (şu an bütün bölümler test için açık).
- Hesap: gerçek Google Play Games bağlantısı ve sıralama sunucusu yok; test hesabı çalışıyor (plan: `docs/v13/KAYIT_PLANI.md`).
- Paket kimliği, sürüm numarası ve imza anahtarı (`ProjectSetup.ApplyPlayerSettings`).
