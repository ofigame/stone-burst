# Canlı maç ölçüm raporu (botlar)

Üretildi: 2026-11-13 18:55 · `LiveMatchTools.RunReport` (tools/unity.ps1 live-report) · havuz `docs/vr1/canli_mac/havuz_5x5_v1.txt` (gen 1, tohum 20261113)

Bu sayılar **bot** maçlarıdır, gerçek oyuncu davranışı değildir. Kuralların kaba dengesini ve açık ayarların etkisini karşılaştırmak içindir.

## Kurallar

- tahta 5x5, el 8 (yenilemesiz), tur 8/oyuncu, dokunarak patlatma kapalı, rakipte 3'lü kuran saldırı geçersiz, eşitlik bozma: en büyük tek grup, o da eşitse ikinci oynayan, zincir yok, 2 katman
- Dokunarak patlatma her ölçümde açık ve kapalı ayrı çalıştırıldı.

## Havuz çeşitliliği

- Üretim: 177 aday denendi, 150 kabul, istenen tür yok 26, eşdeğer kopya 0; filtre retleri: iki renkli fırsat yok: 1, süre 1072 ms
- Dizilim: 150
- Açılışta geçerli hedef hücresi (ortalama): 18,1; fırsat (renk+hedef): 23,8; fırsatlı renk: 3,49; en büyük grup ort.: 5,05
- Yasal açılış saldırısı (ortalama): 37,8; tek saldırıdan sonra savunanın puan hamlesi: en az 6, ortalama 23,7
- Grup büyüklüğü dağılımı (bütün fırsatlar): 3=2449, 4=816, 5=266, 6=41, 7+=4
- Fırsat konumu (hedef satırı): üst üçte bir 1337, orta 1727, alt 512
- Açılış türleri (dizilim sayısı): uc=145 ara=117 L=150 iki_bolge=150 3_4=150 
- Geçerli hedef sayısı dağılımı: 8:1 9:3 10:1 11:2 12:1 13:5 14:8 15:9 16:8 17:18 18:22 19:22 20:18 21:11 22:9 23:5 24:7 

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
| Random | 300 | 46,7 | 46,0 | 7,3 | 6,74 | 7,01 | 16,0 | 0,00 | 24,0 | 38,7 | 37,3 | 0,0 | 0,0 | -0,02 |
| Greedy | 300 | 31,7 | 35,3 | 33,0 | 29,07 | 29,25 | 16,0 | 0,00 | 82,0 | 18,0 | 0,0 | 0,0 | 0,0 | 0,00 |
| Aggressive | 300 | 53,3 | 36,7 | 10,0 | 18,12 | 16,64 | 16,0 | 0,00 | 55,8 | 9,4 | 34,9 | 0,0 | 0,0 | 1,52 |
| Balanced | 300 | 23,3 | 21,7 | 55,0 | 29,29 | 29,22 | 16,0 | 0,00 | 75,2 | 13,6 | 11,3 | 0,0 | 0,0 | 1,89 |
| Aggressive (ilk) – Greedy | 300 | 11,3 | 84,7 | 4,0 | 20,92 | 26,14 | 16,0 | 0,00 | 69,7 | 12,0 | 18,3 | 0,0 | 0,0 | 1,62 |
| Greedy (ilk) – Aggressive | 300 | 80,7 | 14,7 | 4,7 | 26,26 | 21,19 | 16,0 | 0,00 | 70,2 | 14,4 | 15,4 | 0,0 | 0,0 | 1,77 |
| Balanced (ilk) – Greedy | 300 | 69,3 | 13,0 | 17,7 | 30,46 | 28,34 | 16,0 | 0,00 | 79,0 | 15,6 | 5,4 | 0,0 | 0,0 | 1,83 |
| Greedy (ilk) – Balanced | 300 | 15,0 | 66,7 | 18,3 | 28,08 | 30,27 | 16,0 | 0,00 | 78,4 | 16,2 | 5,5 | 0,0 | 0,0 | 1,69 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 46,7 / 46,0 / 7,3 | 46,7 / 46,3 / 7,0 | 46,7 / 53,3 / 0,0 | 46,7 / 53,3 / 0,0 |
| Greedy | 31,7 / 35,3 / 33,0 | 33,0 / 37,3 / 29,7 | 31,7 / 68,3 / 0,0 | 33,0 / 67,0 / 0,0 |
| Aggressive | 53,3 / 36,7 / 10,0 | 56,0 / 37,3 / 6,7 | 53,3 / 46,7 / 0,0 | 56,0 / 44,0 / 0,0 |
| Balanced | 23,3 / 21,7 / 55,0 | 24,7 / 22,0 / 53,3 | 23,3 / 76,7 / 0,0 | 24,7 / 75,3 / 0,0 |
| Aggressive (ilk) – Greedy | 11,3 / 84,7 / 4,0 | 14,3 / 84,7 / 1,0 | 11,3 / 88,7 / 0,0 | 14,3 / 85,7 / 0,0 |
| Greedy (ilk) – Aggressive | 80,7 / 14,7 / 4,7 | 80,7 / 17,7 / 1,7 | 80,7 / 19,3 / 0,0 | 80,7 / 19,3 / 0,0 |
| Balanced (ilk) – Greedy | 69,3 / 13,0 / 17,7 | 73,7 / 13,0 / 13,3 | 69,3 / 30,7 / 0,0 | 73,7 / 26,3 / 0,0 |
| Greedy (ilk) – Balanced | 15,0 / 66,7 / 18,3 | 15,3 / 72,7 / 12,0 | 15,0 / 85,0 / 0,0 | 15,3 / 84,7 / 0,0 |

## Dokunarak patlatma AÇIK

Kazanma sütunları ham puanla (eşitlik bozmasız).

| Eşleşme | Maç | İlk kazanır % | İkinci kazanır % | Berabere % | İlk ort. puan | İkinci ort. puan | Hamle/maç | Pas/maç | Puan % | Hazırlık % | Saldırı % | Dokunma % | Pas % | Saldırı rakibin en iyisini düşürdü (ort.) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Random | 300 | 45,7 | 46,0 | 8,3 | 7,20 | 7,44 | 16,0 | 0,00 | 24,2 | 37,1 | 37,6 | 1,0 | 0,0 | 0,04 |
| Greedy | 300 | 34,7 | 40,7 | 24,7 | 31,28 | 31,41 | 16,0 | 0,00 | 79,9 | 11,6 | 0,0 | 8,5 | 0,0 | 0,00 |
| Aggressive | 300 | 59,0 | 35,3 | 5,7 | 18,58 | 16,80 | 16,0 | 0,00 | 52,4 | 5,0 | 38,5 | 4,1 | 0,0 | 1,53 |
| Balanced | 300 | 27,0 | 23,0 | 50,0 | 30,74 | 30,56 | 16,0 | 0,00 | 72,5 | 10,2 | 11,7 | 5,6 | 0,0 | 2,21 |
| Aggressive (ilk) – Greedy | 300 | 12,3 | 83,3 | 4,3 | 22,21 | 27,79 | 16,0 | 0,00 | 67,2 | 7,3 | 19,0 | 6,5 | 0,0 | 1,62 |
| Greedy (ilk) – Aggressive | 300 | 82,7 | 11,0 | 6,3 | 28,05 | 22,92 | 16,0 | 0,00 | 68,0 | 8,1 | 16,6 | 7,3 | 0,0 | 1,77 |
| Balanced (ilk) – Greedy | 300 | 67,0 | 17,0 | 16,0 | 32,26 | 30,17 | 16,0 | 0,00 | 76,8 | 11,4 | 4,6 | 7,1 | 0,0 | 2,32 |
| Greedy (ilk) – Balanced | 300 | 18,3 | 65,3 | 16,3 | 30,16 | 32,10 | 16,0 | 0,00 | 76,8 | 11,8 | 4,5 | 6,9 | 0,0 | 2,17 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 45,7 / 46,0 / 8,3 | 47,3 / 46,3 / 6,3 | 45,7 / 54,3 / 0,0 | 47,3 / 52,7 / 0,0 |
| Greedy | 34,7 / 40,7 / 24,7 | 36,3 / 42,7 / 21,0 | 34,7 / 65,3 / 0,0 | 36,3 / 63,7 / 0,0 |
| Aggressive | 59,0 / 35,3 / 5,7 | 59,7 / 36,3 / 4,0 | 59,0 / 41,0 / 0,0 | 59,7 / 40,3 / 0,0 |
| Balanced | 27,0 / 23,0 / 50,0 | 28,0 / 24,0 / 48,0 | 27,0 / 73,0 / 0,0 | 28,0 / 72,0 / 0,0 |
| Aggressive (ilk) – Greedy | 12,3 / 83,3 / 4,3 | 16,0 / 83,3 / 0,7 | 12,3 / 87,7 / 0,0 | 16,0 / 84,0 / 0,0 |
| Greedy (ilk) – Aggressive | 82,7 / 11,0 / 6,3 | 82,7 / 16,0 / 1,3 | 82,7 / 17,3 / 0,0 | 82,7 / 17,3 / 0,0 |
| Balanced (ilk) – Greedy | 67,0 / 17,0 / 16,0 | 71,0 / 17,0 / 12,0 | 67,0 / 33,0 / 0,0 | 71,0 / 29,0 / 0,0 |
| Greedy (ilk) – Balanced | 18,3 / 65,3 / 16,3 | 18,3 / 70,0 / 11,7 | 18,3 / 81,7 / 0,0 | 18,3 / 81,7 / 0,0 |

Ölçüm süresi: 145486 ms.
