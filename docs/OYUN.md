# Stone Burst — Oyun Dokümanı

Son güncelleme: 2026-11-16 · Unity 6000.6.4f1 · Android (APK) ve Windows (önizleme exe)

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
| Zincir yok (v14, 2026-11-16) | Kullanıcı: "zincir patlamaları iptal edelim … full mantık gerektiren bir çözüm olsun". Soyulan taşların altından çıkan aynı renk grubu kendiliğinden patlamaz; her patlama oyuncunun dokunuşu ya da kullandığı özel taşın etkisidir (roket, Altın Rulo). Büyük hamle (6+ grup ya da 8+ katman temizleyen özel taş) övgü ve ses alır (`Core/BigMove`). (v13 zincir kuralı arşivde.) |
| Hedef | Renk sayısı, dip yıldızı, zarf ve sünger hedefleri. Hepsi dolunca bölüm kazanılır. |
| Geri al | Son tam hamle (özel taş etkisiyle birlikte) geri alınabilir. |

### Taşlar (6 renk, renk + şekil ile okunur; 2026-11-16 öncesi bölümler 4 renk)

| Kimlik | Ekranda | Şekil |
|---|---|---|
| Coral | Mercan | Daire |
| Blue | Turkuaz | Üçgen |
| Yellow | İnci (beyaz) | Çift dalga |
| Purple | Mor | Elmas |
| Green | Yeşil | Yaprak (2026-11-16) |
| Navy | Lacivert | Yıldız (2026-11-16) |

**Renklerin açılışı (2026-11-16):** renkler bölümlere sırayla girer.

| Bölüm | Renkler |
|---|---|
| 1–3 | Öğretici: 6 rengin hepsi (2026-11-17) |
| 4–7 | İnci (beyaz), turkuaz |
| 8–20 | + mercan (kırmızı) |
| 21–60 | + mor |
| 61–200 | + yeşil |
| 201+ | + lacivert (mavi) |

Bir bölüm açılmış renklerin hepsini kullanmayabilir (ör. nefes ve saf plan bölümleri daha az renkle). Günlük bulmaca da aynı sırayı izler.

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

- Normal bölümde alt çubukta ampul düğmesi; üzerinde reklamla açıldığını gösteren oynat rozeti (2026-11-11 öncesi "−1 ★" bedel rozeti).
- **Güvenilirlik düzeltmesi (2026-11-11, vr1 P0; `docs/vr1/claude_code_paketi_v1/belgeler/01_IPUCU.md`):**
  - Basınca önce "İpucu hazırlanıyor…" görünür. Tahtanın kopyasında arka planda doğrulanmış bir çözüm yolu aranır: önce kayıtlı çözüm ve önceki planın kalanı, sonra hedef ilerlemesine göre en-iyi-önce arama (40.000 durum + 750 ms).
  - Öneri, motorla yeniden yürütülüp kazanca ulaştığı doğrulanan yolun ilk komutudur. Kayba götüren ya da doğrulanamayan hamle önerilmez; tahmine geçilmez.
- Plan bulunamazsa reklam açılmaz: "Bu konum için güvenilir ipucu hazırlanamadı. İpucu hakkın kullanılmadı." Oyun çözümsüz ilan edilmez.
- Plan varsa ödüllü reklam açılır. Ödül gelince plan mevcut tahtada yeniden doğrulanır, sonra gösterilir.
- Öneri her komut türünde olabilir:
  - Dokunuş: taşta altın çerçeve.
  - Tepsi: önce yuvada altın halka; yuva seçilince hedef hücre.
  - Döndürme: taban düğmesi.
  - Boya: boya düğmesi → hücre → renk yazısı.
  - Rulo: rulo düğmesi → satır.
- "Reklam yardımlı" bayrağı yalnız öneri teslim edilince işlenir. Reklam iptal olur ya da başarısız olursa hiçbir şey değişmez. Yinelenen ödül tek teslim sayılır.
- Öneri uygulanana kadar düğme reklamsız aynı öneriyi gösterir. Başka hamle ya da geri almadan sonra eski öneri gizlenir ve aynı hakla ücretsiz yeni plan çözülür. Aynı grubun başka hücresine dokunmak aynı sonucu veriyorsa öneri uygulanmış sayılır.
- Deneme başına 1 ipucu; **yeniden başlatma yeni denemedir**. 2026-11-11'e kadar bedel yeniden başlatılan denemeye taşınıyordu, sıralama ise o denemeyi yardımsız sayıyordu.
- **Yıldız bedeli yok** (2026-11-11, kullanıcı onayı; önceden o bölümden 1 yıldız düşüyordu). Reklam yardımlı oturum sıralama yıldızına ve ZPE'ye yazılmaz.

### Kalpler (2026-11-09)

- En çok 5 kalp; eksik kalp her 30 dakikada 1 dolar. Ana sayfada logo ile yıldız arasında kalp (içinde sayı) ve "Dolu" ya da geri sayım. Dokununca kalp kartı; ödüllü reklam 5 kalbi doldurur.
- 21. bölümden itibaren, ilk hamleden sonra deneme bırakılınca (yeniden başlat, başka bölüm, menü) 1 kalp gider. Kazanınca, geri alıp sürdürünce ya da reklamla devam edince gitmez. Kalp yoksa bölüm açılmaz.

### Kayıpta reklam: +1 hamle ve taş (2026-11-09)

- Hamle bitince kayıp kartında, bölüm başına bir kez: reklam izlenirse +1 hamle ve tahtanın üstünde 4 taş (önce hedef renkleri). Oyuncu birini seçip bir yığının üstüne koyar; diğerleri kaybolur. Konan taş yeni üst katmandır, soyma tetiklemez; hamle harcamaz, geri alınmaz.
- Geçiş reklamı yalnız zafer kartında Devam'da: ilk 10 bölümde yok, 4 bölümde bir, en az 4 dakika arayla, oturumda en çok 8.

### İlerleme kaydı ve bulut (2026-11-09)

- Cihazda: her önemli anda (bölüm kazanma, yıldız, kalp, ayar) kayıt dosyasına yazılır; iPhone yedeği bu dosyayı da alır.
- Bulut (Game Center / Play Games girişiyle, Firestore `saves/{uid}`): açılıştaki girişten sonra önce buluttaki kayıt okunur. Bulut daha ilerideyse (daha çok tamamlanmış bölüm; eşitse daha çok yıldız, sonra açılan bölüm) cihazdaki kaydın yerine geçer ve "İlerlemen buluttan geri yüklendi" bildirimi çıkar. Bölüm oynanırken gelirse haritaya dönünce uygulanır. Ses ve titreşim ayarı cihazda kalır; eski dosya `.precloud` kopyası olarak saklanır.
- Okuma bitmeden buluta hiç yazılmaz (yeni kurulumdaki boş kayıt buluttakini ezmez). Sonra her kazanılan bölümde (en çok 30 sn'de bir) ve uygulama arka plana geçince kayıt sessizce buluta gider. Ayarlar'daki "İlerlemeyi kaydet" düğmesi de aynı kurala uyar.

## 4. Mekanikler ve bölüm takvimi

| Bölüm | Mekanik | Kısa kural |
|---|---|---|
| 1–3 | Vitrin (v14; 2026-11-17 kullanıcı: "ilk 5 bölüm öğreticisini 3 e düşürelim", "o 3 bölümde tüm taş renkleri de olsun") | 7×8 tahta, 6 rengin hepsi: 1 dokun-soy ve katlar (tur) · 2 roket ve tepsi (tur) · 3 Altın Rulo, dip yıldızı, ataş, bağlı ataş (tur). Eski 4–5 vitrinleri (buz, kilit, zarf, sünger, kristal, kesin hamle, döner taban, geçit, boya) kalktı; bu mekanikler takvimdeki ilk bölümlerinde tanıtılır. 4–5 normal bölüm. |
| 6+ | Özel taşlar ve tepsi | Roket, Altın Rulo (yukarıda). 6–7 tek kat 3 renk, 8–14 iki kat, 15–30 üç kat 4 renk 2 görev; sonra her ~10 bölümde bir boyut (`docs/v14/ZORLUK.md`). |
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

**Tahta şekilleri (v14, 2026-11-16; kullanıcı: "ortası delik tahtaları kaldıralım", "dış hatlar çeşitli şekillerde olabilir"):** karelerden oluşan dış hatlar: kesik köşe, L, T, artı, merdiven, kum saati, U, çatı, elmas (aynalı), dikdörtgen ve kenar çentikleri. **İç boşluk yok:** her boş hücre boş hücrelerden geçerek tahta kutusunun dışına ulaşır; tahta tek parçadır, açılışta boş hücre yoktur (geçit hücresi tahtanın parçasıdır). Eski halka / çift göz / kesik halka tahtaları ve boşluğa konan süs ana katalogda yok (`LevelLab v14`, denetim: `ProductionLevelTests`, `BoardHolesTests`).

### Bölüm türleri (v8, 2026-11-08; v14 zincirsiz, 2026-11-16)

Kural verilir, strateji verilmez. v14'te 1000 bölüm `LevelLab v14` ile yeniden üretildi (`docs/v14/PLAN.md`, ölçümler `docs/v14/ZORLUK.md`); takvimdeki bölümler bir **tür** alır:

| Tür | Kural (oyuncuya gösterilen) | İlk bölüm |
|---|---|---|
| Kesin Hamle | Hamle sınırı = çözüm uzunluğu (1–99'da +1) | 27 |
| Ters Düşünme | Kesin hamle; göz alıcı (açgözlü) hamle kaybettirir | 37 |
| Kristal Koruma | Kristal yığından katman soyulursa kayıp (roket dahil) | 57 |
| Renk Kıtlığı | Hedef rengin neredeyse hepsi gerekir | 67 |
| Saf Plan | Özel taş doğmaz | 77 |
| Doğru An | Özel taşsız çözülemez; kesin hamle | 87 |
| Kilit Sırası | Renk kilitleri doğru sırayla açılmalı | 127 |
| Usta (patron) | Her x0 bölümü: kesin hamle + kurallar birleşir (her 100. bölümde iki kural) | 10 |

- 10'luk döngü: 7. ve 8. bölüm tür bölümü, x0 usta; 10'dan sonra her 6. bölüm (x0 hariç) **nefes** bölümü: 1–2 kat, büyük tahta ve gruplar, bol özel taş, +4 hamle.
- v14'te kaldırılanlar: Tek Dokunuş ve Zincir Ustası (zincire bağlıydı), vr1 tahta aileleri (aşağıdaki tablo arşiv bilgisidir; v14 ana kataloğunda aile yok).
- **Tahta aileleri (vr1, 2026-11-11, `levels-v14` vr1 listesi; v14 üretimiyle ana katalogdan çıktı):** 21'den sonraki sade bölümlerin her ikincisi bir tahta ailesi taşıyordu (307 bölüm):

  | Aile | Bölüm | İlk bölüm |
  |---|---|---|
  | Geniş Kıyı | 125 | 23 |
  | Derin Kuyu | 37 | 32 |
  | İki Alan | 42 | 44 |
  | Dar Geçit | 12 | 65 |
  | Aşamalı Hedef | 7 | 102 |
  | Zincir Mührü | 52 | 164 |
  | İki Alan, Sıralı Hedef | 12 | 219 |
  | Mühürlü İki Alan | 19 | 373 |
  | Mühürlü Oda | 1 | 971 |

  - Kurallar: aşamalı hedef, zincir mührü, iki alan (P3 kural motoru, yetenek anahtarıyla).
  - Ailenin kartı, tür ilk görüldüğünde çıkar.
  - 200 bölümden sonra bazı tahtalarda buz bulunur.
  - Rapor: `docs/vr1/YENI_TAHTALAR_RAPORU.md`.
- Kural simgeleri hedef kabuğunun sağında (dokununca kural cümlesi). Kural kartı yalnız türün ilk görüldüğü bölümde ve patronlarda (`meta.seenTypes`).
- Aynı bölümde 2 kayıptan sonra kayıp kartında **İpucu**: bölüm baştan başlar, yalnız ilk doğru hamle gösterilir (ZPE'de yardımlı). 2026-11-11'den beri kayıtlı çözümün ilk adımı yeniden doğrulanır. Her komut türü (tepsi, döndürme, boya, rulo) reklamlı ipucuyla aynı görselle gösterilir; önceden yalnız dokunuş gösteriliyordu.
- Yığına basılı tutunca (0,42 sn) bütün katmanlar görünür; dokunuş parmak kalkınca işler (bütün oturumlarda).
- Günlük bulmaca (`daily-v4`, v14 2026-11-16; 240 bulmaca `LevelLab daily14`, 6 renge kadar): günün türü döner (kesin hamle → kristal → ters → kıt renk → saf plan → doğru an → kilit sırası; zincir türü çıktı); oyuncunun grubunda açılmamış tür atlanır; her günlükte kural kartı; kayıtlı çözüm güçsüz kazanır.
- Deneme seti (v14): 1 kesin hamle, 2 Derin Kuyu, 3 ters, 4 kıt renk, 5 Geniş Kıyı, 6 kristal, 7 saf plan, 8 doğru an, 9 kilit sırası, 10 patron (kristal + özel taş yok + kesin hamle). Pilot 08–10 mühür yerine düğmeli kanat.
- **İlk günlük giriş ücretsiz** (2026-11-11): oyuncunun ilk günlük girişi reklamsız; sonraki günler ödüllü reklamla (reklam bulunamazsa reklamsız).

### Konsept pilotu (2026-11-15)

Ana 1000 bölümde yok; haritadaki **Konsept** düğmesiyle (deneme derlemesi) 10 bölüm. Tehdit her harcanan hamlede bir adım ilerler (tepsi ilerletmez), önceden görünür; hedef kartının altında durum satırı. Tehdit kazanırsa bölüm kaybedilir (kart nedeni söyler, Geri Al). Hedef rengin tahtada kalanı hedefe yetmezse de kayıp.

| Konsept | Kural |
|---|---|
| Çürüyen Zemin | Her K hamlede en alt sıra çöker, taşlar düşer; alt sırada boşaltılan her yığın 1 hamle erteler. |
| Fitil | Sayaçlı yığın 0 olmadan tamamen boşaltılmalı. |
| Dönen Hedef | Yalnız etkin renk sayılır; renk K hamlede bir değişir. |
| Kuzgun | Yol boyunca her hamlede bir hücre, konduğu taşı çalar; önündeki yığın boşsa bekler; yuvaya varırsa kayıp. |
| Yağmur | K hamlede bir işaretli yığına taş düşer; sınırı aşan yığın kaybettirir. |
| Gölge Rakip | Her hamlede 1 adım; 5+ grup hamlesinde 1 geri; bitişe varırsa kayıp. |
| Yayılan Yosun | K hamlede komşuya yayılır (seçilemez örtü); yanında soyma temizler; sınır kaybettirir. |
| Balonlar | K hamlede bir sıra yükselir; altındaki yığın boşalınca patlar (hedef); tahtadan çıkarsa kayıp. |
| Sipariş Kuyruğu | Aşamalı hedeflerin her birinin hamle süresi var. |
| Kombo Ateşi | 5+ grup hamlesi ateşi doldurur, küçük grup 1 azaltır; sönerse kayıp (v14: zincir yerine). |

Ayrıntı: `docs/konsept/RAPOR.md`.

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

**v14 (2026-11-16, geçerli):** kullanıcı: "ileri bölümler kolay olmasın lütfen zorla", "oyuncu rastgele patlatmalarla değil plan ve mantık yürüterek bitirdiğini hissetsin". 1000 bölüm `LevelLab v14` ile yeniden üretildi (ayrıntı `docs/v14/PLAN.md`, ölçüm `docs/v14/ZORLUK.md`).
- Eğri: 1–3 vitrin (7×8, 2–3 kat, 6 renk); 4–7 tek kat 6×7; 8–14 iki kat; 15–30 üç kat 2 görev; sonra tahta 8×9'a, kat 4'e, görev 3–4'e çıkar. Renkler yukarıdaki açılış sırasıyla (vitrin 1–3 hariç: 6 renk): 4–7 iki renk, 8 mercan, 21 mor, 61 yeşil, 201 lacivert.
- Hamle payı (sınır − en iyi çözüm): 6–30 3–5, 31–100 2–3, 101–300 1–2, 301+ 0–1; nefes bölümlerinde +4.
- Rastgele dokunan bot nefes dışı bölümlerde ortalama 6–7 %34, 15–30 %3,6, 301+ ≈%0,1 kazanır; 31'den sonra açgözlü botlar nefes dışı 829 bölümün hepsinde kaybeder.
- 132 nefes, 100 usta (x0), 164 tür bölümü.

Aşağısı v7 dönemi (arşiv bilgisi): Bölümler `LevelLab rebuild` (v7) ile üretildi. Zorluk hamle kıtlığından değil **plandan** gelir: hamle limiti, geniş ışın aramasının bulduğu çözüme makul pay eklenerek konur (6–20: +4–6, 21–60: +3–5, 61–300: +3–4, sonrası +2–4; x1–x2 bölümlerinde +1; bot çok zorlanırsa pay en çok +10'a açılır). Renk hedefi çarpanı, yumuşak açgözlü botun (24 oyun) kazanma oranı hedefe yaklaşacak biçimde seçilir: 6–15 ~%60, sonra v6'dan 0,06 daha düşük; ölçülen ortalama 6–20 %58, 21–100 %49, 101–300 %33, 301–600 %23, 601–1000 %17 (hiçbir bölüm %8'in altında değil). Hedefe yakın adaylar arasında açgözlü oyuncunun (ışın 1) ya da dar ışının (3, 10) çözemediği, yani doğru sıra / zincir kurma / tepsiyi doğru anda kullanma isteyen bölüm tercih edilir; her bölümün kaynağındaki `plan 0–3` bu derinliği yazar. Her 10'un katı bölüm biraz zor, x1–x2 bölümleri nefes alır. Her bölümün kayıtlı çözümü kazandığı ve botun en az bazen kazandığı otomatik doğrulanır (`LevelLab stress`).

## 7. Ekranlar ve görünüm

- **Ana sayfa / harita:** Üstte iki satırlı Stone Burst logosu ve sağında Ayarlar dişlisi, bölge kartı (bölge simgesi, BÖLGE n, bölge adı, aralık, toplam yıldız). Kesintisiz dikey yol: üç bölge (sahil, bahçe, çöl) kendi el çizimi şeridiyle döner; bölgeler nehir ve köprüde birleşir. Düğümler resimdeki yolun ortasından geçer. Açılmamış bölümler numarasız ve doğal bulut örtüsü altında; ilerledikçe bulutlar açılır. Maskot mevcut bölümün üstünde zıplar; kaydırınca "bölümüme dön" düğmesi çıkar.
- **Alt menü:** Yolculuk · Günlük · Taşlar (2026-11-11: "Harita" sekmesinin adı 9 dilde Yolculuk oldu. Seçili ikon büyüyüp küçülür. Günlük modalı açıkken Günlük seçili görünür ve menü modalın üstünde kalır. 2026-11-06: Oyna kalktı, bölüme haritadaki düğümden girilir; Ayarlar sağ üstte). Günlük ve Taşlar ekranlarında sol üstte geri düğmesi, sağ üstte Ayarlar.
- **Açılış:** Beyaz zeminde OFIGAME logosu (~1,6 sn; oyun sahnesi arkada yüklenir). Kaynak: `E:\OFIGAME\Kurumsal\ofigame-logo-acik-zemin.png` → `Resources/Branding/ofigame-logo.png`.
- **Oyun ekranı (VR2, 2026-11-14):** Ekranın tepesinden tam genişlikte krem üst panel: solda duraklat, sağda geri al, ortada maskotlu mor hamle kapsülü, altında yıldız çubuğu. Panelin altında, temanın taş zemini üstünde ayrı gömülü krem hedef yuvası (hedef simgeleri ve sayaçlar). Arka plan bölgenin teması: altı doku (Mavi Liman, Yeşil Vadi, Kum Vadisi, Fener Koyu, Vaha Yolu, İnci Sahili), bulanık, ekranı oran korunarak kaplar (tablette tam kompozisyon, telefonda orta bölüm). Tahta (2026-11-17, ahşap tepsi): bölümün kendi şeklini izleyen kalın, parlak cilalı ahşap kenar (rengi bölgenin arka plan tonunda, biraz koyu ve doygun: Mavi Liman turkuaz, Yeşil Vadi yeşil, Fener Koyu lacivert, Vaha Yolu turuncu…) (dış köşeler 0,26, iç köşeler 0,14 hücre yuvarlak), içi kenar renginin açık tonunda zemin ("tepsinin iç rengi taşların rengini kapatıyor"), kenarın altında yumuşak iç gölge, hafif dış gölge; her hücrede aynı açık tonda yumuşak çukur yuva; kenarla taşlar arası 0,14 hücre (taşı biten hücrede görünür). Görseller `docs/gorseller/tepsi`. Pasif hücrelerde ve boşluklarda arka plan görünür. Altta özel taş tepsisi ve ipucu.
- **İçi boş tahtalar (v14 ana katalogda yok; yalnız eski görünüm/arşiv):** VR2'de boşluklar yalnız arka planı gösterir; süs ve buzlu koleksiyon taşı konmaz (kullanıcı 2026-11-15). Eski görünümde (`Vr2.Enabled = false`) boşluklara hareketli süs konur (`BoardDecor`): zıplayan maskot, fener, çeşme, kaya kemeri/vaha.
- **Öğretici turlar:** Odak alanı açık, çevresi karartılmış; yanında maskotlu konuşma kartı ve zıplayan ok (`TutorialOverlay`, `GameController.Tutorial`). Bölüm 1–3 baştan sona yönlendirmeli (v14: 1 dokunuş ve katlar, 2 roket ve tepsi, 3 Altın Rulo; yalnız gösterilen gruba dokunulur). Diğer mekanikler takvimdeki ilk bölümlerinde tanıtılır (2026-11-17: vitrin 5 → 3 bölüm). Bir bölümde birden çok yeni mekanik varsa hepsi sırayla tanıtılır. Her mekaniğin ilk çıktığı bölüm: o mekanikli hücre açık, kısa açıklama. Bilgi adımı ekrana dokununca geçer; tur bölüm kazanılınca bir daha gösterilmez.
- **Ayarlar:** Resimsiz; turkuaz→krem renk geçişi, süzülen kabarcıklar. Krem kartta Ses ve Titreşim anahtarları, Dil (dokuz dil, her biri kendi yazısıyla), kısa not; altta sürüm. Dil değişince ekranlar yeni dille yeniden kurulur.
- **Yazı tipi:** Nunito ExtraBold (başlık, düğme, sayılar) ve Nunito SemiBold (açıklama), OFL lisanslı. Arapça için Noto Sans Arabic, Japonca/Korece için Noto Sans KR yedek fontları. Hap düğmelerde yazı yüksekliğin ~%32'si, uzun metin sığana dek küçülür.
- **Maskot:** Kristal maskot; gözleri etrafa bakar, göz kırpar; büyük hamle, özel taş ve yıldızda zıplar; son 1–3 hamlede endişelenir.
- **Uygulama ikonu (2026-11-17):** Turkuaz zemin, taş platform üstünde gülen maskot + mor (baklava), mercan (daire), turkuaz (üçgen) taşlar (`docs/gorseller/arayuz/uygulama-ikonu-v3.png`). Android uyarlanabilir ikonda orijinal görsel ortada %68, dışı kenar uzatma. Bildirim büyük simgesi aynı görsel.

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
| `Scripts/Core/Live/`, `UI/LiveMatchView.cs`, `Game/GameController.Live.cs` | Canlı maç (iki oyuncu, sırayla, ayrı mod; rakip şimdilik bot): motor, doğrulanmış tahta havuzu (varsayılan `Resources/Live/live_pool_v5.txt`: 3 katman, 7×6 tam tahta, farklı ama denk eller, her katman üstündekinden farklı), botlar, oyunun tahta görünümüyle ekran (hamle: kendi tahtana ya da rakibe elden taş koy, ya da hazır 3'lüye dokunarak patlat; parçalar skor kutusuna uçar; maç iki raunt, 2. rauntta eller ve ilk oynayan değişir, toplam kazanır; hamle süresi 10 sn); giriş anasayfada Sıralama altı. Ayrıntı `docs/vr1/canli_mac/SONUC_RAPORU.md`. |
| `docs/gorseller/` | Kullanılan görsellerin kaynakları (bkz. `docs/gorseller/README.md`). |

## 9. Sık işler

```text
Bölüm kaynağı değişti:     cd tools/LevelLab ; dotnet run -c Release -- report
Görsel değişti:            powershell -File tools/ArtV6/prepare.ps1
Testler:                   cd tools/CoreTests ; dotnet test      ve   tools/unity.ps1 test-edit
Bölüm sınaması:            cd tools/LevelLab ; dotnet run -c Release -- stress 60
APK:                       tools/unity.ps1 build-android   → Builds/Android/KatKat.apk
Exe:                       tools/unity.ps1 build-windows   → Builds/Windows/KatKat.exe
Canlı maç havuz + ölçüm:   tools/unity.ps1 live-report     → docs/vr1/canli_mac/OLCUM_RAPORU_v5.md (KATKAT_LIVE_POOL=v1..v4 eski havuzlar)
Canlı maç ekranları:       tools/unity.ps1 live-capture    → docs/vr1/canli_mac/ekranlar/ (oyunda: anasayfa → Canlı Maç; KATKAT_LIVE_SHOT_H=2400 → ekranlar_1080x2400/)
Tablet harita denetimi:    tools/unity.ps1 capture-tablet-map → docs/captures/tablet_harita/ (telefon, iPad, 10:16 tablet; geniş ekranda harita tam genişliğe büyür)
```

Android derlemesinde görseller ASTC ile sıkıştırılır (`BuildScripts.ApplyTextureBudget`), ikon katmanları ve tam ekran (sanal tuşlar gizli, kaydırınca geçici) uygulanır.

## 10. Yayından önce

- `GameController.TestUnlockAll` → `false` (şu an bütün bölümler test için açık).
- Hesap: gerçek Google Play Games bağlantısı ve sıralama sunucusu yok; test hesabı çalışıyor (plan: `docs/v13/KAYIT_PLANI.md`).
- Paket kimliği, sürüm numarası ve imza anahtarı (`ProjectSetup.ApplyPlayerSettings`).
