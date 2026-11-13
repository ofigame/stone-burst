# Canlı maç ölçüm raporu (botlar)

Üretildi: 2026-11-13 19:21 · `LiveMatchTools.RunReport` (tools/unity.ps1 live-report) · havuz `Assets/KatKat/Resources/Live/live_pool_diffhands_v2.txt` (gen 2, tohum 20261113)

Bu sayılar **bot** maçlarıdır, gerçek oyuncu davranışı değildir. Kuralların kaba dengesini ve açık ayarların etkisini karşılaştırmak içindir.

## Kurallar

- tahta 6x6, el 8 (yenilemesiz), tur 8/oyuncu, dokunarak patlatma kapalı, rakipte 3'lü kuran saldırı geçersiz, eşitlik bozma: en büyük tek grup, o da eşitse ikinci oynayan, eller farklı ama denk, zincir yok, 2 katman
- Dokunarak patlatma her ölçümde açık ve kapalı ayrı çalıştırıldı.

## Havuz çeşitliliği

- Üretim: 163 aday denendi, 150 kabul, istenen tür yok 5, eşdeğer kopya 0; filtre retleri: denk ikinci el yok: 8, süre 2895 ms
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
| Random | 300 | 47,7 | 44,7 | 7,7 | 7,88 | 7,87 | 16,0 | 0,00 | 27,0 | 37,5 | 35,4 | 0,0 | 0,0 | 0,04 |
| Greedy | 300 | 43,7 | 45,7 | 10,7 | 34,95 | 35,05 | 16,0 | 0,00 | 91,2 | 8,8 | 0,0 | 0,0 | 0,0 | 0,00 |
| Aggressive | 300 | 58,7 | 34,3 | 7,0 | 21,38 | 19,16 | 16,0 | 0,00 | 60,1 | 4,4 | 35,6 | 0,0 | 0,0 | 1,49 |
| Balanced | 300 | 46,3 | 37,3 | 16,3 | 36,34 | 36,07 | 16,0 | 0,00 | 85,2 | 5,9 | 8,9 | 0,0 | 0,0 | 2,17 |
| Aggressive (ilk) – Greedy | 300 | 12,3 | 83,7 | 4,0 | 23,57 | 30,88 | 16,0 | 0,00 | 74,5 | 5,9 | 19,5 | 0,0 | 0,0 | 1,67 |
| Greedy (ilk) – Aggressive | 300 | 78,7 | 17,0 | 4,3 | 31,08 | 24,91 | 16,0 | 0,00 | 77,0 | 6,6 | 16,4 | 0,0 | 0,0 | 1,73 |
| Balanced (ilk) – Greedy | 300 | 74,3 | 17,7 | 8,0 | 37,27 | 34,01 | 16,0 | 0,00 | 87,8 | 8,3 | 3,9 | 0,0 | 0,0 | 2,71 |
| Greedy (ilk) – Balanced | 300 | 20,3 | 72,0 | 7,7 | 34,15 | 37,49 | 16,0 | 0,00 | 88,5 | 8,2 | 3,3 | 0,0 | 0,0 | 2,38 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 47,7 / 44,7 / 7,7 | 47,7 / 46,0 / 6,3 | 47,7 / 52,3 / 0,0 | 47,7 / 52,3 / 0,0 |
| Greedy | 43,7 / 45,7 / 10,7 | 44,0 / 47,3 / 8,7 | 43,7 / 56,3 / 0,0 | 44,0 / 56,0 / 0,0 |
| Aggressive | 58,7 / 34,3 / 7,0 | 60,7 / 35,0 / 4,3 | 58,7 / 41,3 / 0,0 | 60,7 / 39,3 / 0,0 |
| Balanced | 46,3 / 37,3 / 16,3 | 47,7 / 38,0 / 14,3 | 46,3 / 53,7 / 0,0 | 47,7 / 52,3 / 0,0 |
| Aggressive (ilk) – Greedy | 12,3 / 83,7 / 4,0 | 15,0 / 83,7 / 1,3 | 12,3 / 87,7 / 0,0 | 15,0 / 85,0 / 0,0 |
| Greedy (ilk) – Aggressive | 78,7 / 17,0 / 4,3 | 78,7 / 20,7 / 0,7 | 78,7 / 21,3 / 0,0 | 78,7 / 21,3 / 0,0 |
| Balanced (ilk) – Greedy | 74,3 / 17,7 / 8,0 | 77,7 / 18,3 / 4,0 | 74,3 / 25,7 / 0,0 | 77,7 / 22,3 / 0,0 |
| Greedy (ilk) – Balanced | 20,3 / 72,0 / 7,7 | 20,3 / 76,0 / 3,7 | 20,3 / 79,7 / 0,0 | 20,3 / 79,7 / 0,0 |

## Dokunarak patlatma AÇIK

Kazanma sütunları ham puanla (eşitlik bozmasız).

| Eşleşme | Maç | İlk kazanır % | İkinci kazanır % | Berabere % | İlk ort. puan | İkinci ort. puan | Hamle/maç | Pas/maç | Puan % | Hazırlık % | Saldırı % | Dokunma % | Pas % | Saldırı rakibin en iyisini düşürdü (ort.) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Random | 300 | 47,7 | 43,7 | 8,7 | 8,13 | 7,93 | 16,0 | 0,00 | 26,6 | 36,3 | 35,9 | 1,2 | 0,0 | 0,04 |
| Greedy | 300 | 41,0 | 47,3 | 11,7 | 38,03 | 38,17 | 16,0 | 0,00 | 84,8 | 2,0 | 0,0 | 13,1 | 0,0 | 0,00 |
| Aggressive | 300 | 61,0 | 32,7 | 6,3 | 21,90 | 19,35 | 16,0 | 0,00 | 55,6 | 0,8 | 38,6 | 5,0 | 0,0 | 1,47 |
| Balanced | 300 | 43,7 | 39,3 | 17,0 | 40,07 | 39,91 | 16,0 | 0,00 | 83,0 | 2,0 | 3,6 | 11,4 | 0,0 | 2,97 |
| Aggressive (ilk) – Greedy | 300 | 9,7 | 88,0 | 2,3 | 25,19 | 33,21 | 16,0 | 0,00 | 70,7 | 1,8 | 19,1 | 8,4 | 0,0 | 1,53 |
| Greedy (ilk) – Aggressive | 300 | 88,3 | 8,0 | 3,7 | 33,66 | 25,71 | 16,0 | 0,00 | 70,5 | 1,4 | 18,1 | 10,1 | 0,0 | 1,73 |
| Balanced (ilk) – Greedy | 300 | 75,3 | 14,3 | 10,3 | 40,47 | 37,22 | 16,0 | 0,00 | 83,9 | 3,1 | 1,8 | 11,2 | 0,0 | 3,06 |
| Greedy (ilk) – Balanced | 300 | 19,3 | 72,3 | 8,3 | 37,70 | 40,37 | 16,0 | 0,00 | 84,0 | 2,2 | 2,0 | 11,7 | 0,0 | 3,11 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 47,7 / 43,7 / 8,7 | 48,3 / 44,0 / 7,7 | 47,7 / 52,3 / 0,0 | 48,3 / 51,7 / 0,0 |
| Greedy | 41,0 / 47,3 / 11,7 | 42,3 / 48,3 / 9,3 | 41,0 / 59,0 / 0,0 | 42,3 / 57,7 / 0,0 |
| Aggressive | 61,0 / 32,7 / 6,3 | 62,0 / 34,0 / 4,0 | 61,0 / 39,0 / 0,0 | 62,0 / 38,0 / 0,0 |
| Balanced | 43,7 / 39,3 / 17,0 | 45,0 / 41,3 / 13,7 | 43,7 / 56,3 / 0,0 | 45,0 / 55,0 / 0,0 |
| Aggressive (ilk) – Greedy | 9,7 / 88,0 / 2,3 | 11,7 / 88,0 / 0,3 | 9,7 / 90,3 / 0,0 | 11,7 / 88,3 / 0,0 |
| Greedy (ilk) – Aggressive | 88,3 / 8,0 / 3,7 | 88,3 / 10,0 / 1,7 | 88,3 / 11,7 / 0,0 | 88,3 / 11,7 / 0,0 |
| Balanced (ilk) – Greedy | 75,3 / 14,3 / 10,3 | 80,3 / 14,7 / 5,0 | 75,3 / 24,7 / 0,0 | 80,3 / 19,7 / 0,0 |
| Greedy (ilk) – Balanced | 19,3 / 72,3 / 8,3 | 19,3 / 76,0 / 4,7 | 19,3 / 80,7 / 0,0 | 19,3 / 80,7 / 0,0 |

Ölçüm süresi: 283211 ms.
