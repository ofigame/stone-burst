# Canlı maç ölçüm raporu (botlar)

Üretildi: 2026-11-13 18:49 · `LiveMatchTools.RunReport` (tools/unity.ps1 live-report) · havuz `Assets/KatKat/Resources/Live/live_pool_v1.txt` (gen 1, tohum 20261113)

Bu sayılar **bot** maçlarıdır, gerçek oyuncu davranışı değildir. Kuralların kaba dengesini ve açık ayarların etkisini karşılaştırmak içindir.

## Kurallar

- tahta 6x6, el 8 (yenilemesiz), tur 8/oyuncu, dokunarak patlatma kapalı, rakipte 3'lü kuran saldırı geçersiz, eşitlik bozma: en büyük tek grup, o da eşitse ikinci oynayan, zincir yok, 2 katman
- Dokunarak patlatma her ölçümde açık ve kapalı ayrı çalıştırıldı.

## Havuz çeşitliliği

- Üretim: 152 aday denendi, 150 kabul, istenen tür yok 2, eşdeğer kopya 0; filtre retleri: süre 2009 ms
- Dizilim: 150
- Açılışta geçerli hedef hücresi (ortalama): 28,0; fırsat (renk+hedef): 37,8; fırsatlı renk: 3,51; en büyük grup ort.: 5,35
- Yasal açılış saldırısı (ortalama): 50,7; tek saldırıdan sonra savunanın puan hamlesi: en az 13, ortalama 37,8
- Grup büyüklüğü dağılımı (bütün fırsatlar): 3=3808, 4=1253, 5=547, 6=61, 7+=7
- Fırsat konumu (hedef satırı): üst üçte bir 1819, orta 2126, alt 1731
- Açılış türleri (dizilim sayısı): uc=149 ara=132 L=150 iki_bolge=150 3_4=150 
- Geçerli hedef sayısı dağılımı: 15:1 17:2 19:3 20:2 21:5 22:3 23:5 24:5 25:7 26:8 27:15 28:13 29:21 30:20 31:15 32:9 33:7 34:7 35:1 36:1 

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
| Random | 300 | 47,3 | 44,0 | 8,7 | 7,91 | 7,74 | 16,0 | 0,00 | 27,2 | 37,4 | 35,4 | 0,0 | 0,0 | -0,01 |
| Greedy | 300 | 37,3 | 42,3 | 20,3 | 35,20 | 35,37 | 16,0 | 0,00 | 91,4 | 8,6 | 0,0 | 0,0 | 0,0 | 0,00 |
| Aggressive | 300 | 58,0 | 33,3 | 8,7 | 22,42 | 19,98 | 16,0 | 0,00 | 62,9 | 4,1 | 33,0 | 0,0 | 0,0 | 1,40 |
| Balanced | 300 | 25,7 | 24,0 | 50,3 | 37,68 | 37,69 | 16,0 | 0,00 | 89,8 | 6,0 | 4,3 | 0,0 | 0,0 | 1,34 |
| Aggressive (ilk) – Greedy | 300 | 6,7 | 90,0 | 3,3 | 23,17 | 31,53 | 16,0 | 0,00 | 76,0 | 5,6 | 18,4 | 0,0 | 0,0 | 1,52 |
| Greedy (ilk) – Aggressive | 300 | 83,0 | 10,7 | 6,3 | 32,08 | 25,13 | 16,0 | 0,00 | 78,9 | 6,2 | 14,9 | 0,0 | 0,0 | 1,57 |
| Balanced (ilk) – Greedy | 300 | 74,7 | 10,0 | 15,3 | 38,15 | 34,93 | 16,0 | 0,00 | 90,9 | 7,5 | 1,6 | 0,0 | 0,0 | 1,63 |
| Greedy (ilk) – Balanced | 300 | 10,7 | 74,7 | 14,7 | 35,18 | 38,13 | 16,0 | 0,00 | 90,9 | 7,2 | 1,9 | 0,0 | 0,0 | 1,04 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 47,3 / 44,0 / 8,7 | 48,0 / 44,0 / 8,0 | 47,3 / 52,7 / 0,0 | 48,0 / 52,0 / 0,0 |
| Greedy | 37,3 / 42,3 / 20,3 | 38,7 / 42,7 / 18,7 | 37,3 / 62,7 / 0,0 | 38,7 / 61,3 / 0,0 |
| Aggressive | 58,0 / 33,3 / 8,7 | 58,7 / 35,0 / 6,3 | 58,0 / 42,0 / 0,0 | 58,7 / 41,3 / 0,0 |
| Balanced | 25,7 / 24,0 / 50,3 | 26,7 / 24,3 / 49,0 | 25,7 / 74,3 / 0,0 | 26,7 / 73,3 / 0,0 |
| Aggressive (ilk) – Greedy | 6,7 / 90,0 / 3,3 | 9,3 / 90,0 / 0,7 | 6,7 / 93,3 / 0,0 | 9,3 / 90,7 / 0,0 |
| Greedy (ilk) – Aggressive | 83,0 / 10,7 / 6,3 | 83,0 / 14,7 / 2,3 | 83,0 / 17,0 / 0,0 | 83,0 / 17,0 / 0,0 |
| Balanced (ilk) – Greedy | 74,7 / 10,0 / 15,3 | 80,0 / 10,0 / 10,0 | 74,7 / 25,3 / 0,0 | 80,0 / 20,0 / 0,0 |
| Greedy (ilk) – Balanced | 10,7 / 74,7 / 14,7 | 11,0 / 77,3 / 11,7 | 10,7 / 89,3 / 0,0 | 11,0 / 89,0 / 0,0 |

## Dokunarak patlatma AÇIK

Kazanma sütunları ham puanla (eşitlik bozmasız).

| Eşleşme | Maç | İlk kazanır % | İkinci kazanır % | Berabere % | İlk ort. puan | İkinci ort. puan | Hamle/maç | Pas/maç | Puan % | Hazırlık % | Saldırı % | Dokunma % | Pas % | Saldırı rakibin en iyisini düşürdü (ort.) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Random | 300 | 51,0 | 41,7 | 7,3 | 8,84 | 8,08 | 16,0 | 0,00 | 28,3 | 34,9 | 35,8 | 1,1 | 0,0 | 0,03 |
| Greedy | 300 | 40,0 | 32,0 | 28,0 | 38,24 | 38,07 | 16,0 | 0,00 | 85,5 | 2,2 | 0,0 | 12,3 | 0,0 | 0,00 |
| Aggressive | 300 | 64,0 | 29,7 | 6,3 | 23,01 | 19,53 | 16,0 | 0,00 | 57,0 | 0,7 | 37,0 | 5,3 | 0,0 | 1,44 |
| Balanced | 300 | 28,3 | 25,3 | 46,3 | 40,49 | 40,41 | 16,0 | 0,00 | 84,3 | 1,9 | 3,0 | 10,8 | 0,0 | 2,71 |
| Aggressive (ilk) – Greedy | 300 | 7,0 | 89,3 | 3,7 | 24,52 | 33,50 | 16,0 | 0,00 | 70,5 | 1,3 | 19,4 | 8,8 | 0,0 | 1,56 |
| Greedy (ilk) – Aggressive | 300 | 86,7 | 12,3 | 1,0 | 34,08 | 26,37 | 16,0 | 0,00 | 71,7 | 1,1 | 17,1 | 10,0 | 0,0 | 1,61 |
| Balanced (ilk) – Greedy | 300 | 80,0 | 9,7 | 10,3 | 41,04 | 37,76 | 16,0 | 0,00 | 84,7 | 2,4 | 0,8 | 12,1 | 0,0 | 2,85 |
| Greedy (ilk) – Balanced | 300 | 12,0 | 73,3 | 14,7 | 38,27 | 40,80 | 16,0 | 0,00 | 84,6 | 1,9 | 1,3 | 12,2 | 0,0 | 2,92 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 51,0 / 41,7 / 7,3 | 51,3 / 42,3 / 6,3 | 51,0 / 49,0 / 0,0 | 51,3 / 48,7 / 0,0 |
| Greedy | 40,0 / 32,0 / 28,0 | 42,3 / 36,0 / 21,7 | 40,0 / 60,0 / 0,0 | 42,3 / 57,7 / 0,0 |
| Aggressive | 64,0 / 29,7 / 6,3 | 64,7 / 30,7 / 4,7 | 64,0 / 36,0 / 0,0 | 64,7 / 35,3 / 0,0 |
| Balanced | 28,3 / 25,3 / 46,3 | 29,3 / 25,7 / 45,0 | 28,3 / 71,7 / 0,0 | 29,3 / 70,7 / 0,0 |
| Aggressive (ilk) – Greedy | 7,0 / 89,3 / 3,7 | 10,3 / 89,3 / 0,3 | 7,0 / 93,0 / 0,0 | 10,3 / 89,7 / 0,0 |
| Greedy (ilk) – Aggressive | 86,7 / 12,3 / 1,0 | 86,7 / 13,0 / 0,3 | 86,7 / 13,3 / 0,0 | 86,7 / 13,3 / 0,0 |
| Balanced (ilk) – Greedy | 80,0 / 9,7 / 10,3 | 83,3 / 9,7 / 7,0 | 80,0 / 20,0 / 0,0 | 83,3 / 16,7 / 0,0 |
| Greedy (ilk) – Balanced | 12,0 / 73,3 / 14,7 | 12,7 / 78,7 / 8,7 | 12,0 / 88,0 / 0,0 | 12,7 / 87,3 / 0,0 |

Ölçüm süresi: 322349 ms.
