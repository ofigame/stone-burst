# Canlı maç ölçüm raporu (botlar)

Üretildi: 2026-11-14 03:08 · `LiveMatchTools.RunReport` (tools/unity.ps1 live-report) · havuz `Assets/KatKat/Resources/Live/live_pool_v3.txt` (gen 3, tohum 20261113)

Bu sayılar **bot** maçlarıdır, gerçek oyuncu davranışı değildir. Kuralların kaba dengesini ve açık ayarların etkisini karşılaştırmak içindir.

## Kurallar

- tahta 6x6, el 8 (yenilemesiz), tur 8/oyuncu, dokunarak patlatma açık, rakipte 3'lü kuran saldırı geçersiz, eşitlik bozma: en büyük tek grup, o da eşitse ikinci oynayan, eller farklı ama denk, alt katman üstten farklı, zincir yok, 2 katman
- Dokunarak patlatma her ölçümde açık ve kapalı ayrı çalıştırıldı.

## Havuz çeşitliliği

- Üretim: 163 aday denendi, 150 kabul, istenen tür yok 5, eşdeğer kopya 0; filtre retleri: denk ikinci el yok: 8, süre 2593 ms
- Dizilim: 150
- Farklı ama denk eller: 150 dizilim; iki elin en iyi hemen puanı hepsinde eşit; fırsat sayısı farkı ortalama 0,00 (en çok 0; ortalama fırsat 37,4); hedef hücre farkı ortalama 0,03
- Açılışta geçerli hedef hücresi (ortalama): 27,9; fırsat (renk+hedef): 37,4; fırsatlı renk: 3,49; en büyük grup ort.: 5,36
- Yasal açılış saldırısı (ortalama): 50,8; tek saldırıdan sonra savunanın puan hamlesi: en az 13, ortalama 37,3
- Grup büyüklüğü dağılımı (bütün fırsatlar): 3=3774, 4=1227, 5=535, 6=61, 7+=7
- Fırsat konumu (hedef satırı): üst üçte bir 1799, orta 2086, alt 1719
- Açılış türleri (dizilim sayısı): uc=149 ara=134 L=150 iki_bolge=150 3_4=150 
- Geçerli hedef sayısı dağılımı: 15:1 17:2 19:4 20:2 21:5 22:3 23:5 24:6 25:7 26:9 27:14 28:12 29:22 30:19 31:16 32:9 33:6 34:7 35:1 

## Yöntem

- Havuzdaki her dizilim **iki kez** oynatılır: bir kez P1 önce, bir kez P2 önce (aynı tahta, aynı el). Böylece tahtanın kendisinden gelen fark iki sırada da bulunur, kalan fark sıradan gelir.
- **İlk oyuncu avantajı** = ilk oynayanın kazanma yüzdesi − ikinci oynayanınki (beraberlik ayrı). Puan farkı da ortalama olarak verilir.
- **Maç süresi** = maç başına kabul edilen hamle (her oyuncu 8, toplam 16; pas dahil). Gerçek saniye için prototip ekranı her hamlenin süresini maç kaydına yazar (`LiveRecord.ElapsedMs`); bu süre ancak insanlarla yapılan yerel denemelerde ölçülebilir.
- **Saldırı tercihi** = kabul edilen hamlelerin türü (puan, hazırlık, saldırı, dokunma, pas) ve saldırının rakibin o anki en iyi hemen puanını ortalama kaç katman düşürdüğü.
- **Eşitlik bozma**: botlar kuralı hesaba katmaz; her maç bir kez oynanır ve sonucu dört kurala göre ayrı okunur (yok, en büyük tek grup, ikinci oynayan, önce en büyük grup sonra ikinci oynayan).
- Botlar: Random (rastgele yasal hamle), Greedy (en çok puan; yoksa sonraki tura en iyi hazırlık; bilerek saldırmaz), Aggressive (rakibin en iyi puanı kendisininkinden küçük değilse onu en çok düşüren saldırı), Balanced (kazanç − rakibin ardından en iyisi + 0,5 × kendi sonraki en iyisi).

## Dokunarak patlatma KAPALI

Kazanma sütunları ham puanla (eşitlik bozmasız).

| Eşleşme | Maç | İlk kazanır % | İkinci kazanır % | Berabere % | İlk ort. puan | İkinci ort. puan | Hamle/maç | Pas/maç | Puan % | Hazırlık % | Saldırı % | Dokunma % | Pas % | Saldırı rakibin en iyisini düşürdü (ort.) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Random | 300 | 48,0 | 46,3 | 5,7 | 7,75 | 7,52 | 16,0 | 0,00 | 26,0 | 38,3 | 35,7 | 0,0 | 0,0 | 0,03 |
| Greedy | 300 | 43,3 | 43,0 | 13,7 | 36,74 | 36,73 | 16,0 | 0,00 | 89,7 | 10,3 | 0,0 | 0,0 | 0,0 | 0,00 |
| Aggressive | 300 | 66,3 | 30,7 | 3,0 | 22,26 | 17,91 | 16,0 | 0,00 | 57,4 | 4,8 | 37,8 | 0,0 | 0,0 | 1,56 |
| Balanced | 300 | 46,0 | 36,0 | 18,0 | 38,14 | 37,75 | 16,0 | 0,00 | 82,0 | 7,9 | 10,1 | 0,0 | 0,0 | 2,28 |
| Aggressive (ilk) – Greedy | 300 | 14,3 | 81,3 | 4,3 | 23,44 | 31,58 | 16,0 | 0,00 | 71,2 | 7,8 | 21,0 | 0,0 | 0,0 | 1,79 |
| Greedy (ilk) – Aggressive | 300 | 80,3 | 16,3 | 3,3 | 32,13 | 24,20 | 16,0 | 0,00 | 74,0 | 7,8 | 18,2 | 0,0 | 0,0 | 1,90 |
| Balanced (ilk) – Greedy | 300 | 74,3 | 18,3 | 7,3 | 39,12 | 35,43 | 16,0 | 0,00 | 85,2 | 9,8 | 5,0 | 0,0 | 0,0 | 2,68 |
| Greedy (ilk) – Balanced | 300 | 14,3 | 77,7 | 8,0 | 35,34 | 39,09 | 16,0 | 0,00 | 85,2 | 10,7 | 4,1 | 0,0 | 0,0 | 2,51 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 48,0 / 46,3 / 5,7 | 48,7 / 46,7 / 4,7 | 48,0 / 52,0 / 0,0 | 48,7 / 51,3 / 0,0 |
| Greedy | 43,3 / 43,0 / 13,7 | 44,7 / 44,7 / 10,7 | 43,3 / 56,7 / 0,0 | 44,7 / 55,3 / 0,0 |
| Aggressive | 66,3 / 30,7 / 3,0 | 67,3 / 31,3 / 1,3 | 66,3 / 33,7 / 0,0 | 67,3 / 32,7 / 0,0 |
| Balanced | 46,0 / 36,0 / 18,0 | 46,7 / 36,3 / 17,0 | 46,0 / 54,0 / 0,0 | 46,7 / 53,3 / 0,0 |
| Aggressive (ilk) – Greedy | 14,3 / 81,3 / 4,3 | 18,7 / 81,3 / 0,0 | 14,3 / 85,7 / 0,0 | 18,7 / 81,3 / 0,0 |
| Greedy (ilk) – Aggressive | 80,3 / 16,3 / 3,3 | 80,3 / 18,7 / 1,0 | 80,3 / 19,7 / 0,0 | 80,3 / 19,7 / 0,0 |
| Balanced (ilk) – Greedy | 74,3 / 18,3 / 7,3 | 78,3 / 19,3 / 2,3 | 74,3 / 25,7 / 0,0 | 78,3 / 21,7 / 0,0 |
| Greedy (ilk) – Balanced | 14,3 / 77,7 / 8,0 | 14,7 / 82,0 / 3,3 | 14,3 / 85,7 / 0,0 | 14,7 / 85,3 / 0,0 |

### El adaleti (aynı bot iki tarafta; P1 = A eli, P2 = B eli, her dizilim iki sırayla)

50 / 50'ye yakınsa iki el denk. "Dizilim" sütunu: aynı dizilimin iki maçını (sıra değişerek) da aynı elin kazandığı dizilimler; bu oran yüksekse o dizilimde el, sıradan daha belirleyici.

| Eşleşme | Maç | A eli / B eli / berabere % (ham) | A / B % (varsayılan eşitlik) | Ort. puan A / B | Dizilim: A iki maçı da / B iki maçı da / bölündü % |
|---|---|---|---|---|---|
| Random | 300 | 51,7 / 42,7 / 5,7 | 54,0 / 46,0 | 7,87 / 7,39 | 31,3 / 23,3 / 45,3 |
| Greedy | 300 | 41,7 / 44,7 / 13,7 | 48,0 / 52,0 | 36,53 / 36,94 | 30,0 / 34,0 / 36,0 |
| Aggressive | 300 | 50,3 / 46,7 / 3,0 | 52,0 / 48,0 | 20,30 / 19,87 | 22,0 / 18,0 / 60,0 |
| Balanced | 300 | 43,7 / 38,3 / 18,0 | 51,3 / 48,7 | 37,96 / 37,92 | 37,3 / 34,7 / 28,0 |

## Dokunarak patlatma AÇIK

Kazanma sütunları ham puanla (eşitlik bozmasız).

| Eşleşme | Maç | İlk kazanır % | İkinci kazanır % | Berabere % | İlk ort. puan | İkinci ort. puan | Hamle/maç | Pas/maç | Puan % | Hazırlık % | Saldırı % | Dokunma % | Pas % | Saldırı rakibin en iyisini düşürdü (ort.) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Random | 300 | 43,0 | 50,3 | 6,7 | 7,84 | 8,60 | 16,0 | 0,00 | 26,5 | 36,4 | 35,9 | 1,2 | 0,0 | 0,08 |
| Greedy | 300 | 48,7 | 38,7 | 12,7 | 41,02 | 40,62 | 16,0 | 0,00 | 82,6 | 1,8 | 0,0 | 15,6 | 0,0 | 0,00 |
| Aggressive | 300 | 66,3 | 30,7 | 3,0 | 22,54 | 19,12 | 16,0 | 0,00 | 52,3 | 0,7 | 40,4 | 6,6 | 0,0 | 1,55 |
| Balanced | 300 | 45,7 | 36,3 | 18,0 | 43,18 | 42,83 | 16,0 | 0,00 | 78,6 | 2,1 | 4,5 | 14,8 | 0,0 | 2,87 |
| Aggressive (ilk) – Greedy | 300 | 8,0 | 89,7 | 2,3 | 25,37 | 35,25 | 16,0 | 0,00 | 66,1 | 1,4 | 21,0 | 11,5 | 0,0 | 1,63 |
| Greedy (ilk) – Aggressive | 300 | 86,7 | 10,0 | 3,3 | 35,63 | 26,12 | 16,0 | 0,00 | 67,0 | 0,9 | 19,6 | 12,4 | 0,0 | 1,79 |
| Balanced (ilk) – Greedy | 300 | 79,0 | 14,7 | 6,3 | 43,68 | 40,29 | 16,0 | 0,00 | 80,8 | 2,1 | 1,5 | 15,5 | 0,0 | 3,01 |
| Greedy (ilk) – Balanced | 300 | 18,7 | 76,0 | 5,3 | 40,37 | 43,67 | 16,0 | 0,00 | 80,8 | 1,9 | 1,7 | 15,6 | 0,0 | 2,90 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 43,0 / 50,3 / 6,7 | 43,0 / 51,3 / 5,7 | 43,0 / 57,0 / 0,0 | 43,0 / 57,0 / 0,0 |
| Greedy | 48,7 / 38,7 / 12,7 | 51,7 / 41,0 / 7,3 | 48,7 / 51,3 / 0,0 | 51,7 / 48,3 / 0,0 |
| Aggressive | 66,3 / 30,7 / 3,0 | 67,7 / 31,3 / 1,0 | 66,3 / 33,7 / 0,0 | 67,7 / 32,3 / 0,0 |
| Balanced | 45,7 / 36,3 / 18,0 | 46,3 / 37,7 / 16,0 | 45,7 / 54,3 / 0,0 | 46,3 / 53,7 / 0,0 |
| Aggressive (ilk) – Greedy | 8,0 / 89,7 / 2,3 | 10,3 / 89,7 / 0,0 | 8,0 / 92,0 / 0,0 | 10,3 / 89,7 / 0,0 |
| Greedy (ilk) – Aggressive | 86,7 / 10,0 / 3,3 | 86,7 / 12,7 / 0,7 | 86,7 / 13,3 / 0,0 | 86,7 / 13,3 / 0,0 |
| Balanced (ilk) – Greedy | 79,0 / 14,7 / 6,3 | 83,3 / 15,0 / 1,7 | 79,0 / 21,0 / 0,0 | 83,3 / 16,7 / 0,0 |
| Greedy (ilk) – Balanced | 18,7 / 76,0 / 5,3 | 19,0 / 79,0 / 2,0 | 18,7 / 81,3 / 0,0 | 19,0 / 81,0 / 0,0 |

### El adaleti (aynı bot iki tarafta; P1 = A eli, P2 = B eli, her dizilim iki sırayla)

50 / 50'ye yakınsa iki el denk. "Dizilim" sütunu: aynı dizilimin iki maçını (sıra değişerek) da aynı elin kazandığı dizilimler; bu oran yüksekse o dizilimde el, sıradan daha belirleyici.

| Eşleşme | Maç | A eli / B eli / berabere % (ham) | A / B % (varsayılan eşitlik) | Ort. puan A / B | Dizilim: A iki maçı da / B iki maçı da / bölündü % |
|---|---|---|---|---|---|
| Random | 300 | 47,0 / 46,3 / 6,7 | 51,0 / 49,0 | 8,16 / 8,28 | 27,3 / 25,3 / 47,3 |
| Greedy | 300 | 43,7 / 43,7 / 12,7 | 49,7 / 50,3 | 40,91 / 40,73 | 29,3 / 30,0 / 40,7 |
| Aggressive | 300 | 45,7 / 51,3 / 3,0 | 46,3 / 53,7 | 20,64 / 21,02 | 18,0 / 25,3 / 56,7 |
| Balanced | 300 | 40,0 / 42,0 / 18,0 | 49,7 / 50,3 | 42,88 / 43,13 | 32,0 / 32,7 / 35,3 |

Ölçüm süresi: 286436 ms.
