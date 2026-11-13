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

Hepsi `LiveRules` ve `LiveGenerator` üzerinden denenebilir; `tools/unity.ps1 live-report` aynı tabloları yeniden üretir (yaklaşık 5 dakika).

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
- Yeniden üretmek için `KATKAT_LIVE_SIZE=5` ortam değişkeniyle `tools/unity.ps1 live-report`.
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
