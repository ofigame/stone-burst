# Canlı maç prototipi: sonuç raporu

13 Kasım 2026 · Kaynak belge: [CLAUDE_CANLI_MAC.md](../CLAUDE_CANLI_MAC.md) · Bot ölçüm tabloları: [OLCUM_RAPORU.md](OLCUM_RAPORU.md) · Ekranlar: [ekranlar/](ekranlar/)

## Ne yapıldı

| Parça | Yer | Not |
|---|---|---|
| Maç motoru | `Assets/KatKat/Scripts/Core/Live/` (`LiveMatch`, `LiveMoves`, `LiveRules`, `LiveLayout`) | Unity'siz, arayüzden bağımsız. Ana oyunun `GroupFinder`, `BoardState`, `CellState` ve renk kimlikleri kullanıldı. Ana oyunun zincir çözümüne (`RulesEngine`) dokunulmadı. |
| Tahta üretici ve doğrulama | `LiveGenerator.cs` | Tohumlu aday üretimi, üç açılış filtresi, açılış türü etiketleri, eşdeğer kopya ayıklama. |
| Doğrulanmış havuz | `Assets/KatKat/Resources/Live/live_pool_v1.txt` | 150 dizilim, üretim sürümü 1, tohum 20261113. |
| Botlar ve ölçüm | `LiveBots.cs` (`LiveBots`, `LiveSim`) | 4 bot türü; her dizilim iki sırayla oynatılır. |
| Tek cihaz ekranı | `Assets/KatKat/Scripts/UI/LiveMatchScreen.cs` | Sahne `Assets/KatKat/Scenes/LiveMatch.unity` ilk açılışta kurulur. Mağaza derlemesine girmez. Metinler yalnız Türkçe. |
| Araçlar | `Assets/KatKat/Editor/LiveMatchTools.cs` | Unity menüsü: **Kat Kat → Canlı maç** (Prototipi oyna, Havuzu üret, Ölçüm raporu, Ekran görüntüleri). Toplu: `tools/unity.ps1 live-report`, `tools/unity.ps1 live-capture`. |
| Testler | `Tests/EditMode/LiveMatchTests.cs` (15), `Tests/PlayMode/LiveMatchScreenTests.cs` (1) | Belgenin 12 kabul kontrolünün hepsi kapsanır (aşağıdaki tablo). |

## Kullanılan değerler (prototip varsayımları, yapılandırmada)

`LiveRules` sınıfında, maç başında sabitlenir. Hiçbiri canlı maç standardı olarak sabitlenmedi.

| Ayar | Değer | Kaynak |
|---|---|---|
| Tahta | 6×6 | Belgede yok; benim varsayımım. Maske kalıpları: tam, köşeler kesik, ortada iki delik, rastgele üç delik. |
| El | 8 taş, iki tarafta aynı, yenilemesiz | Belge önerisi |
| Tur | Oyuncu başına 8, sırayla | Belge önerisi |
| Pas | Yalnız yasal hamle yokken; tur harcar | Belge |
| Dokunarak patlatma | Varsayılan **kapalı**; ekranda "Dokunma: Açık/Kapalı" düğmesiyle değişir (maç yeniden başlar) | Belge: deneme ayarı |
| Rakipte hedefte 3'lü kuran saldırı | Geçersiz | Belge (ilk prototip) |
| Kesin kurallar | İki katman, zincir yok, kenardan bağlı en az 3, 4 renk | Belge |

Belgede açık kalan bir nokta için seçim: **kendi tahtasında** değişim hiçbir grup kurmasa da yasaldır (hazırlık hamlesi), hedef hücre farklı renkte ve dolu olduğu sürece.

## Kabul kontrolleri

| Senaryo | Test | Sonuç |
|---|---|---|
| Maç açılışı: aynı maske, iki katman, aynı el | `Opening_BothPlayers_SameMask_TwoLayers_SameHand` | Geçti |
| Hazır üçlü içeren aday reddedilir | `Generator_RejectsReadyTriple` | Geçti |
| Açılıştaki tek saldırıdan sonra rakibin puan hamlesi kalır | `Pool_EveryOpeningAttack_LeavesDefenderAScoringMove` (havuzun 150 dizilimi, her yasal saldırı ayrı) | Geçti |
| Kendi tahtada üçlü: grup bir kez soyulur, doğru puan | `OwnTriple_PeelsGroupOnce_ScoresLayers` | Geçti |
| Alt katmandan yeni üçlü çıkar: zincir yok | `BottomLayerMakesNewTriple_NoChain_StonesStay` | Geçti |
| Hazır üçlüye dokunma ayara uygun | `TapReadyGroup_Off_Rejected_On_PeelsWithoutHand_UsesTurn_LastLayerEmpties` | Geçti |
| Rakipte geçerli değişim: yalnız üst renk, puan yok, sıra ilerler | `OpponentSwap_OnlyTopChanges_NoScore_TurnAdvances` | Geçti |
| Aynı renk, boş hücre, rakipte yeni üçlü reddedilir, durum değişmez | `Rejects_SameColor_EmptyCell_AttackTriple_NotYourTurn_Duplicate_StateUnchanged` | Geçti |
| Hazırlık hamlesi | `PrepMove_ChangesColor_NoScore_TurnAdvances` | Geçti |
| Son katman soyulur: hücre boşalır, değiştirilemez | Dokunma testinin içinde | Geçti |
| Maç sonucu: eşit tur, puan = soyulan katman | `MatchResult_EqualTurns_ScoreIsPeeledLayers` (4 bot × 2 ayar × 10 dizilim) | Geçti |
| Havuz: eşdeğer renk kopyaları ayıklanır, doğrulama tekrar üretilebilir | `CanonicalKey_ColorRenamedMirroredRotated_SameFamily`, `Pool_IsReproducible_FromRecordedSeed` | Geçti |
| Ek: başka yerdeki hazır üçlü saldırıyı geçersiz kılmaz; yinelenen komut işlenmez; yasal hamle yokken pas | `AttackElsewhereReadyTriple_...`, `Rejects_...`, `NoLegalMove_PassAccepted_UsesTurn` | Geçti |
| Ekran: seç → önizle → onayla; geçersiz dokunuş durumu değiştirmez | PlayMode `LiveMatchScreenTests` | Geçti |

Bütün EditMode koşusu: 254 geçti, 0 düştü, 2 atlandı (eski atlananlar). PlayMode'da yalnız yeni ekran testi çalıştırıldı; tam PlayMode koşusu yapılmadı (ana oyun kodu değişmedi).

## Doğrulanmış havuz

- **Üretim:** tohum sırayla denenir. Her yuva sıradaki açılış türünü ister: uçtan, aradan, L, iki bölge, 3–4 seçimi. Türü taşımayan aday o yuvaya alınmaz.
- **Filtreler:**
  1. Hazır 3'lü yok.
  2. En az iki farklı renkte, farklı hedeflerde fırsat var.
  3. Her yasal açılış saldırısı tek tek uygulanır; savunanın elinde en az bir puan hamlesi kalmalı.
- **Fırsat tanımı:** el rengi + hedef hücre + oluşan grup. Aynı renkteki el taşları bir kez sayılır.
- **Eşdeğer kopyalar:** kanonik anahtarla ayıklanır. Anahtar, maskeyi koruyan aynalama/döndürme ve renk yeniden adlandırması altında en küçük gösterimdir; alt katman ve el de anahtara girer. Dizilim kimliği anahtarın özetidir (`LM1-xxxxxxxx`).
- **Kayıt:** maç kaydında dizilim kimliği, üretim sürümü, başlangıç eli, ilk oynayan, kurallar, hamleler (her biri süresiyle) ve sonuç bulunur. Ekran bitişte kaydı `persistentDataPath/live_match_records.txt` dosyasına ekler.

Çeşitlilik ölçüleri tohum sayısıyla değil açılış ölçüleriyle verildi ([OLCUM_RAPORU.md](OLCUM_RAPORU.md) "Havuz çeşitliliği"):

| Ölçü | Değer |
|---|---|
| Geçerli hedef hücresi (dizilim başına) | 15–36, ortalama 28 |
| Fırsat (renk + hedef) | Ortalama 37,8 |
| Fırsatlı renk | Ortalama 3,5 |
| Grup büyüklüğü (tüm fırsatlar) | 3'lü 3808, 4'lü 1253, 5'li 547, 6'lı 61, 7+ 7 |
| Konum (hedef satırı) | Üst 1819, orta 2126, alt 1731 |
| Tek saldırıdan sonra savunanın puan hamlesi | En az 13, ortalama 37,8 |

## Ölçüm yöntemi

- **İlk oyuncu avantajı:**
  - Havuzdaki her dizilim iki kez oynanır, bir kez P1 önce, bir kez P2 önce. Tahta ve el aynıdır, yalnız sıra değişir.
  - Avantaj = ilk oynayanın kazanma yüzdesi − ikincinin. Beraberlik ayrı, ortalama puanlar da verilir.
  - Her satır 300 maçtır, kabaca ±5 puanlık gürültü beklenir.
- **Maç süresi:**
  - Botlarda hamle ve pas sayısı ölçülür.
  - Gerçek saniye ancak insanlarla ölçülebilir. Ekran her hamlenin süresini (`ElapsedMs`) maç kaydına yazar, yerel denemelerde bu dosyadan okunur.
- **Saldırı tercihi:**
  - Hamle türlerinin payı (puan, hazırlık, saldırı, dokunma, pas).
  - Her saldırının rakibin o anki en iyi hemen puanını kaç katman düşürdüğü.
  - Saldırgan botun açgözlü bota karşı sonucu, iki sırada.
- **Botlar:** Random, Greedy, Aggressive, Balanced (tanımları ölçüm raporunda). Gerçek oyuncu modeli değildirler; kuralların kaba dengesini ve ayarların etkisini karşılaştırmak içindir.

## Bulgular (bot verisi, yorum)

1. **Açılış filtreleri 6×6'da neredeyse hiç reddetmiyor.**
   - 152 adaydan 150'si geçti; 2 aday yalnız istenen türü taşımadığı için atlandı.
   - Dört renk, 8 taşlık el ve 6×6 tahtada açılış çok zengin: ortalama 38 fırsat var. Tek saldırıdan sonra savunanın elinde en az 13 puan hamlesi kalıyor.
   - Belgenin istediği "saldırı ile puan arasında seçim" baskısı bu boyutta zayıf.
2. **Saldırı az işe yarıyor.**
   - Saldırgan bot açgözlü bota iki sırada da büyük farkla yeniliyor: Greedy %83–90 kazanıyor, Aggressive %7–11.
   - Saldırı rakibin en iyi hemen puanını ortalama 1,4–1,6 katman düşürüyor, ama saldıran kendi taşını ve turunu harcıyor.
   - Dengeli bot hamlelerinin yalnız %3–4'ünde saldırıyor.
3. **İlk oyuncu avantajı (aynı bot iki tarafta, dokunma kapalı):**

   | Bot | İlk oynayanın avantajı |
   |---|---|
   | Random | +3 puan |
   | Greedy | −5 puan |
   | Balanced | +2 puan |
   | Aggressive | +25 puan |

   Yalnız saldırgan botta gürültüden büyük. Dokunma açıkken avantaj biraz artıyor: Greedy +8, Random +9, Balanced +3, Aggressive +34.
4. **Beraberlik çok.** Dengeli botlarda maçların yaklaşık yarısı berabere bitiyor (dokunma kapalı %50, açık %46). Eşitlik bozma kuralı ürün kararı olarak gerekebilir.
5. **Dokunarak patlatma açıkken:**
   - Ortalama puan yaklaşık +3 artıyor.
   - Hamlelerin %11–12'si dokunma oluyor, hazırlık hamleleri %6–9'dan %2'ye düşüyor.
   - Oyun daha çok "hazır grubu topla" yönüne kayıyor.
6. **Maç süresi:**
   - Her maç 16 hamle (oyuncu başına 8); pas hiç olmadı, çünkü el 8 taş ve tur 8.
   - Saniye cinsinden süre ölçülmedi.

Bu bulgular kuralları değiştirmedi. Denenebilecek yönler (karar sizin):
- Tahta küçültülebilir (ör. 5×5 ya da daha çok delikli maske).
- El küçültülebilir ya da renk sayısı azaltılabilir. Böylece fırsat sayısı düşer, saldırı ağırlık kazanır.
- Filtre 3 sıkılaştırılabilir. Örneğin "savunanın en iyi hamlesi en fazla N katman düşebilir" ya da "en az 2 fırsat kalır" yerine üst sınır konabilir.

Hepsi `LiveRules` ve `LiveGenerator` üzerinden denenebilir; `KATKAT_LIVE_HANDS=same` ile `tools/unity.ps1 live-report` aynı tabloları yeniden üretir (yaklaşık 5 dakika).

## Denenmeyenler ve açık kararlar

- **İnsanlarla deneme:** ekran yalnız editörde ve toplu yakalamayla doğrulandı. İnsanlarla oynanmadı, telefonda denenmedi; cihaz derlemesi yok.
- **Ürün kararı olarak açık olanlar:**
  - El boyutu, taş yenileme, tur sayısı, süre sınırı.
  - Rakip elinin görünürlüğü (prototipte iki el de açık).
  - Doğrudan dokunarak patlatma.
  - Eşitlik bozma.
  - Ağlı sürümün kopma, yeniden bağlanma ve zaman aşımı kuralları.
- **Ağlı sürüme hazırlık:** komutlarda artan sıra numarası var. Aynı ya da eski numara ikinci kez işlenmez, test edildi. Sunucunun tahtayı ve eli belirlemesi, komutu denetleyip iki tarafa göndermesi henüz yok.

## Deneme: 5×5 tahta (kullanıcı 2026-11-13: "5×5 dene")

- Diğer bütün ayarlar aynı: 8 taşlık el, 8 tur, 4 renk, aynı filtreler, aynı botlar ve tohumlar.
- Havuz ayrı tutuldu: `havuz_5x5_v1.txt`, oyuna girmez. Tablolar [OLCUM_RAPORU_5x5.md](OLCUM_RAPORU_5x5.md) dosyasında.
- Yeniden üretmek için `KATKAT_LIVE_HANDS=same` ve `KATKAT_LIVE_SIZE=5` ortam değişkenleriyle `tools/unity.ps1 live-report`.
- Varsayılan prototip hâlâ 6×6.

Aşağıdaki satırlarda dokunarak patlatma kapalı.

| Ölçü | 6×6 | 5×5 |
|---|---|---|
| Filtrelerden geçen aday | 150 / 152 | 150 / 177 (26 tür uymadı, 1 iki renkli fırsat yok) |
| Açılışta fırsat (renk + hedef), ortalama | 37,8 | 23,8 |
| Tek saldırıdan sonra savunanın puan hamlesi: en az / ortalama | 13 / 37,8 | 6 / 23,7 |
| Dengeli bot: saldırı payı | %4,3 | %11,3 |
| Dengeli bot: bir saldırı rakibin en iyi puanını düşürdü | 1,34 | 1,89 |
| Yalnız puan toplayan bot (Greedy) saldırgana karşı kazanır | %83–90 | %81–85 |
| Dengeli bot Greedy'e karşı kazanır | %75 | %67–69 |
| Beraberlik: Greedy / Dengeli | %20 / %50 | %33 / %55 |
| İlk oynayanın avantajı: Random / Greedy / Saldırgan / Dengeli | +3 / −5 / +25 / +2 | +1 / −4 / +17 / +2 |
| Maç başına ortalama puan (Greedy) | 35,2 | 29,1 |
| Greedy'nin hazırlık hamlesi payı | %8,6 | %18,0 |

"Fırsat konumu" satırı 5×5'te satırları eşit bölmüyor (üçte birlere 2-2-1 satır düşüyor). O satır iki boy arasında karşılaştırılmamalı.

### Yorum

1. **Saldırı daha anlamlı ama hâlâ tek başına kazandırmıyor.**
   - Dengeli bot 5×5'te 2,6 kat daha sık saldırıyor ve saldırı rakibe daha çok zarar veriyor.
   - Yalnız saldıran bot ise yine büyük farkla yeniliyor.
   - Puan alıp gerektiğinde saldırmak en iyisi; 6×6'da saldırı neredeyse hiç gerekmiyordu.
2. **Planlama arttı.** Fırsatlar azalınca hazırlık hamlesi payı ikiye katlandı (%8,6 → %18). Belgenin "plan yaparak" hedefine daha yakın.
3. **Filtre 3 hâlâ bağlayıcı değil.** Saldırıdan sonra savunanın en az 6 puan hamlesi kalıyor. Filtreler 177 adaydan yalnız 1'ini reddetti.
4. **Beraberlik daha da arttı** (Greedy %20 → %33). Puanlar küçülüp birbirine yaklaşınca eşitlik sıklaşıyor. 5×5'e geçilirse eşitlik bozma kuralı daha gerekli olur.
5. **İlk oynayan avantajı** saldırgan botta azaldı (+25 → +17); diğerlerinde gürültü düzeyinde.

### Sonraki denemeler (karar sizin)

- **Eşitlik bozma** örnekleri:
  - En büyük tek grup.
  - Rakip tahtada kalan katman.
  - İkinci oynayana yarım puan.
- **Eli küçültme.** 6 taş ve 6 tur: fırsat ve hazırlık dengesini daha da sıkılaştırır.
- **Saldırıya küçük ödül.** Örneğin rakipte değişen taş ele döner. Bu belgedeki "eski üst taş ele dönmez" kuralını değiştirir, ürün kararıdır.

## Eşitlik bozma (kullanıcı 2026-11-13: "Eşitlik bozma ekle")

`LiveRules.TieBreak` ayarı eklendi. Dört kural var:
- **Yok:** eşit puan berabere.
- **En büyük tek grup:** maçta tek hamlede en çok katman soyan kazanır; o da eşitse berabere.
- **İkinci oynayan:** eşit puanda ikinci oynayan kazanır.
- **En büyük tek grup, o da eşitse ikinci oynayan:** prototipin varsayılanı. Beraberlik kalmaz.

Uygulama:
- Ekran maç sonunda gerekçeyi yazar, örneğin "puan eşit; en büyük tek grup 5–4".
- Maç kaydına kazanan da yazılır.
- Botlar kuralı hesaba katmaz. Her maç bir kez oynanır ve sonucu dört kurala göre ayrı okunur.
- Tablolar: [OLCUM_RAPORU.md](OLCUM_RAPORU.md) ve [OLCUM_RAPORU_5x5.md](OLCUM_RAPORU_5x5.md), "Eşitlik bozma kurallarına göre" başlıkları.

Aşağıdaki tablo dokunarak patlatma kapalı, iki tarafta aynı bot. Hücrelerdeki biçim: ilk oynayan kazanır % / ikinci kazanır % / berabere %.

| Bot, tahta | Yok | En büyük tek grup | İkinci oynayan | Grup, sonra ikinci |
|---|---|---|---|---|
| Greedy 6×6 | 37 / 42 / 20 | 39 / 43 / 19 | 37 / 63 / 0 | 39 / 61 / 0 |
| Dengeli 6×6 | 26 / 24 / 50 | 27 / 24 / 49 | 26 / 74 / 0 | 27 / 73 / 0 |
| Greedy 5×5 | 32 / 35 / 33 | 33 / 37 / 30 | 32 / 68 / 0 | 33 / 67 / 0 |
| Dengeli 5×5 | 23 / 22 / 55 | 25 / 22 / 53 | 23 / 77 / 0 | 25 / 75 / 0 |

### Yorum

1. **"En büyük tek grup" beraberliği çok az çözüyor** (Dengeli botta %50 → %49).
   - Eşit biten maçlarda iki taraf aynı tahtada aynı elle genelde aynı büyüklükte grupları buluyor, yani en büyük grup da eşit çıkıyor.
   - Beraberliğin asıl nedeni şu: tahtalar ve eller bire bir aynı ve taraflar az etkileşiyor (saldırı nadir). İki iyi oyuncu benzer en iyi yolu bulunca maç ayna gibi oynanıyor.
2. **"İkinci oynayan" bütün beraberlikleri bitiriyor ama adil değil.** Dengeli botta ikinci oynayan maçların %73–77'sini kazanıyor; eşitlik bozmasız tabloda fark yalnız +2 puandı. Kural bu oyun düzeninde kazananı çoğu zaman sıraya bırakıyor.
3. **Yalnız puana bakan bir eşitlik kuralı yetmiyor.** Ayna oyunda puan, en büyük grup ve kalan el de eşit çıkar; ayna maçı yalnız sıraya bağlı bir kural ayırabilir. Asıl çözüm aynalığı kırmaktır.
   - Saldırıyı daha değerli kılmak; 5×5 bu yönde küçük bir adımdı.
   - Taraflara farklı ama denk eller vermek. Belgedeki "aynı ilk el" kuralını değiştirir, ürün kararıdır.
   - İlk oynayana küçük bir dezavantaj vermek, örneğin bir tur eksik. Bu da sıraya bağlıdır ama denetlenebilir.

Varsayılan prototipte "en büyük tek grup, o da eşitse ikinci oynayan" duruyor. Yukarıdaki nedenle bu kural kesin karar değildir.

## Deneme: farklı ama denk eller (kullanıcı 2026-11-13: "Farklı ama denk eller")

Amaç ayna oyunu kırmak. Belgedeki "ilk el iki tarafta aynı" kuralını değiştirir. Deneme ayarı: `LiveRules.DifferentHands`; ekranda "El: Aynı/Farklı" düğmesi.

**Denklik tanımı** (`LiveGenerator.Validate`):
- **Aynı renk sayı profili:** ikinci el birincinin renk değişimidir. Örneğin P1'de 4 mercan + 2 turkuaz + 1 inci + 1 mor, P2'de 4 turkuaz + 2 mercan + 1 inci + 1 mor. Taş "malzemesi" eşit.
- **Renk kümesi farklı:** iki el aynı renk çoklu kümesi olamaz.
- **Açılış değeri eşit:** en iyi hemen puan aynı, fırsat (renk + hedef) sayısı farkı en çok %15 (en az 2). Uygun değişimler arasından değeri en yakın olan seçilir.
- **Filtreler her el için ayrı:** saldırı denetimi iki yönde yapılır. P1'in her açılış saldırısından sonra P2'nin puan hamlesi kalır, tersi de geçerli.

**Havuzlar:**
- 6×6: `Resources/Live/live_pool_diffhands_v2.txt` (prototipin varsayılanı).
- 5×5: `docs/vr1/canli_mac/havuz_5x5_farkli_el_v2.txt`.
- Üretim sürümü 2, kimlik öneki `LM2`. İki havuzda da 150 dizilim.
- Sonuçta fırsat sayısı farkı her dizilimde 0 çıktı: eller açılışta tam denk.
- Üretmek için: `tools/unity.ps1 live-report` (5×5 için `KATKAT_LIVE_SIZE=5`). Aynı el havuzu ve raporu için `KATKAT_LIVE_HANDS=same`.
- Tablolar: [OLCUM_RAPORU_farkli_el.md](OLCUM_RAPORU_farkli_el.md), [OLCUM_RAPORU_farkli_el_5x5.md](OLCUM_RAPORU_farkli_el_5x5.md).

Aşağıdaki tablolarda dokunarak patlatma kapalı. Hücrelerdeki biçim: ilk oynayan kazanır % / ikinci kazanır % / berabere %.

**Beraberlik ve sıra etkisi (eşitlik bozmasız):**

| Bot | 6×6 aynı el | 6×6 farklı el | 5×5 aynı el | 5×5 farklı el |
|---|---|---|---|---|
| Greedy | 37 / 42 / 20 | 44 / 46 / 11 | 32 / 35 / 33 | 40 / 47 / 13 |
| Dengeli | 26 / 24 / 50 | 46 / 37 / 16 | 23 / 22 / 55 | 43 / 39 / 18 |
| Random | 47 / 44 / 9 | 48 / 45 / 8 | 47 / 46 / 7 | 48 / 42 / 10 |

**Varsayılan eşitlik bozmayla** (en büyük grup, sonra ikinci oynayan):

| Bot | 6×6 aynı el | 6×6 farklı el | 5×5 aynı el | 5×5 farklı el |
|---|---|---|---|---|
| Greedy | 39 / 61 | 44 / 56 | 33 / 67 | 41 / 59 |
| Dengeli | 27 / 73 | 48 / 52 | 25 / 75 | 44 / 56 |

**Saldırı** (dengeli botun hamle payı / bir saldırının rakibin en iyi puanını düşürdüğü):

| Tahta | Aynı el | Farklı el |
|---|---|---|
| 6×6 | %4,3 / 1,3 | %8,9 / 2,2 |
| 5×5 | %11,3 / 1,9 | %16,3 / 2,2 |

Saldırgan bot yine Greedy'e yeniliyor: Greedy %78–84 kazanıyor.

### Yorum

1. **Ayna oyun kırıldı.** Dengeli botlarda beraberlik %50 → %16 (6×6) ve %55 → %18 (5×5) düştü. Greedy'de %20 → %11 ve %33 → %13. Asıl sorunun aynı el olduğu doğrulandı.
2. **Eşitlik bozma artık adil.**
   - 6×6 farklı el + varsayılan kural: ikinci oynayan dengeli botta %52, Greedy'de %56. Aynı elde %73 ve %61'di.
   - 5×5'te ikinci oynayan %56–59 alıyor; 6×6 daha dengeli.
3. **Etkileşim arttı.** Eller farklı olunca rakibin hangi renge ihtiyaç duyduğu ayrışıyor, saldırı daha hedefli oluyor. Dengeli bot iki kat sık saldırıyor ve her saldırı rakibe daha çok zarar veriyor.
4. **İlk oynayan avantajı:**
   - Eşitlik bozmasız: dengeli botta 6×6'da +9, 5×5'te +4 puan; bu kez ilk oynayan lehine. Önceden beraberliğe giden maçlar artık çoğunlukla ilk oynayana gidiyor.
   - Eşitlik kuralı bunu dengeliyor: 6×6'da −5.
   - Saldırgan botta avantaj büyük kalıyor (+24 ile +26 arası), ama bu bot zaten kötü oynuyor.
5. **Denklik ölçüsü yalnız açılışı kapsıyor.** İki elin fırsat sayısı ve en iyi puanı açılışta eşit. İlerleyen turlarda hangi rengin daha değerli olacağı oyuna bağlı. Bot verisinde farklı elle büyük bir taraf avantajı görünmüyor, ama bunu ayrıca ölçmedim (P1 eli / P2 eli kazanma oranı).

### Öneri (karar sizin)

Bot verisine göre en dengeli birleşim:
- 6×6 tahta.
- Farklı ama denk eller.
- Eşitlik bozma "en büyük tek grup, o da eşitse ikinci oynayan".

Dengeli bot bu birleşimde %48 / %52 dağılıyor, beraberlik kalmıyor, saldırı hamlelerin yaklaşık %9'u. Bunu insanlarla denemek gerekiyor.

**Varsayılan yapıldı** (kullanıcı 2026-11-13: "Evet, varsayılan yap"): prototip artık bu birleşimle açılır. "El: Aynı" düğmesi eski kurala döner. Ekranlar 01–07 farklı elle, `ekranlar/08_ayni_eller.png` aynı elle.

---

## Yeniden tasarım (2026-11-14)

Kullanıcı (2026-11-13): "hiç hayal ettiğim gibi değil. Tasarım mevcut tasarımımızla olacak. 2 katman taş olacak ve mevcut tahtamız gibi altta gözükecek…". Eski prototip ekranı arşive kaldırıldı; yerine oyunun kendi görünümüyle yeni ekran.

**Ekran (`UI/LiveMatchView.cs`)**
- Oyunun tahta görünümü (`BoardView`): yığınlar, altta alt katman şeridi, zemin plakası; eşit boyda iki tahta (üstte rakip, altta sen).
- Aradaki şeritte iki skor, sıranın kimde olduğu ve tur ("Tur 3/8"). Sırası olanın tahtasında nabız gibi atan renkli kenar (sen turkuaz, rakip mercan).
- Altta elin (8 taş); üstte rakibin eli küçük ve açık (neyle saldırabileceği görünür).
- Elden taş seç → bir hücreye dokun. Kendi tahtanda 3'lü olursa hemen patlar (zincir yok); rakibin tahtasında yalnız renk değişir (3'lü kuramaz). **Öneri ya da önizleme yok**; geçersiz dokunuşta kısa sallanma ve sebep yazısı.
- Animasyonlar oyunun kendi boya ve soyulma animasyonları; puan parçacıkları skor kutusuna uçar.
- Rakip: Dengeli bot (~0,9 sn düşünür). Hamle yoksa pas kendiliğinden. Maç sonu kartı: sonuç, skor, eşitlikte gerekçe; "Yeni maç" (sonraki dizilim, ilk oynayan değişir) ve "Ana sayfa".
- Giriş: anasayfada Sıralama'nın altında "Canlı Maç".

**Kural: alt katman üstten farklı.** Her hücrede alt renk üst renkten farklı (`LiveRules.BottomDiffersFromTop`); iki oyuncunun tahtası birebir aynı başlar. Havuz sürüm 3 (`Resources/Live/live_pool_v3.txt`, LM3, 150 dizilim). Bot ölçümü ([OLCUM_RAPORU_v3.md](OLCUM_RAPORU_v3.md)): Dengeli–Dengeli eşitlik bozmasız %46 / %36 / %18 berabere, varsayılan eşitlik kuralıyla %47 / %53; saldırı payı %10.

**Ekranlar** (`ekranlar/`): 01 açılış, 02 el taşı seçili, 03 patlama sonrası sıra rakipte, 04 botun hamlesi, 05 rakibe saldırı, 06 maç sonu.

**Denenmedi:** telefonda, insanlarla; ağlı (iki gerçek oyuncu) sürüm yok.

### Dokunarak patlatma, parçalar skora, el adaleti (2026-11-14)

- Hamle üç türlü: kendi tahtana taş koy, rakibe taş koy, ya da hazır 3'lüye taş koymadan dokun (`TapReadyGroups` açık; tur harcar, el taşı harcamaz). Dengeli bot hamlelerinin ~%15'i dokunarak patlatma.
- Patlayan taşların parçaları skor kutusuna uçar; skor yazısı varınca artar ve zıplar (ekran 03a, 08).
- Yerleşim ortalı ve ekran boyuna göre; 1080×2400'de tahtalar tam genişlik (`ekranlar_1080x2400/`).
- **El adaleti** ([OLCUM_RAPORU_v3.md](OLCUM_RAPORU_v3.md), dokunarak patlatma açık, 150 dizilim × 2 sıra, aynı bot iki tarafta):

| Bot | A / B kazanır % (eşitlik kuralıyla) | Ort. puan A / B | Dizilimde iki maçı da A / B / bölündü % |
|---|---|---|---|
| Random | 51,0 / 49,0 | 8,2 / 8,3 | 27 / 25 / 47 |
| Greedy | 49,7 / 50,3 | 40,9 / 40,7 | 29 / 30 / 41 |
| Aggressive | 46,3 / 53,7 | 20,6 / 21,0 | 18 / 25 / 57 |
| Dengeli | 49,7 / 50,3 | 42,9 / 43,1 | 32 / 33 / 35 |

Yorum: toplamda iki el denk (300 maçta ±%3 gürültü içinde). Ama tek tek dizilimlerde Dengeli botla %65'inde iki maçı da aynı el kazanıyor; el, sıradan daha belirleyici. Yani adil ortalamada, tek maçta şans payı var. Seçenekler (karar sizin): (1) maç iki el olarak oynanır, ikinci elde eller değişir; (2) eski "aynı el" kuralına dönmek (beraberlik artar); (3) olduğu gibi bırakmak.

### Rövanş ve tam dikdörtgen tahta (2026-11-14)

Kullanıcı: "rövanşta el değişsin. Tahtaların ortasında boşluk olmasın tahtaların şekli tek tip olacak. Düz köşeli olacak".

- Maç iki el: 1. el bitince ara kart ("1. el bitti", "Rövanş"). Rövanşta aynı dizilim; eller ve ilk oynayan yer değiştirir. Skor kutuları iki elin toplamı; sonuç kartı toplam ve el el skor ("1. el 40 – 50 · Rövanş 42 – 48").
- Tahtalar tam 6×6, delik ve kesik köşe yok (havuz v4, `Resources/Live/live_pool_v4.txt`).
- Ölçüm ([OLCUM_RAPORU_v4.md](OLCUM_RAPORU_v4.md), dokunarak patlatma açık, 150 seri):

| Eşleşme (P1 – P2) | P1 / P2 / berabere % | Ort. toplam fark |
|---|---|---|
| Random | 46,7 / 51,3 / 2,0 | 8,2 |
| Greedy | 42,0 / 50,7 / 7,3 | 4,0 |
| Aggressive | 47,3 / 50,7 / 2,0 | 10,6 |
| Dengeli | 32,7 / 31,3 / 36,0 | 1,9 |
| Dengeli (P1) – Greedy | 93,3 / 6,0 / 0,7 | 8,2 |
| Greedy (P1) – Dengeli | 9,3 / 90,0 / 0,7 | 7,7 |

Yorum: aynı bot iki taraftayken dengede (koşullar simetrik). Dengeli–Dengeli beraberliği yüksek, çünkü aynı deterministik bot aynı koşulda aynı hamleleri yapar; insanlarda bu beklenmez. Güçlü bot tek maçta %73–80, rövanşlı maçta %90–93 kazanıyor: el ve sıra şansı azaldı, beceri öne çıktı.

Ekranlar: `06` ara kart, `10` rövanş başı (eller değişmiş), `11` toplam sonuç.
