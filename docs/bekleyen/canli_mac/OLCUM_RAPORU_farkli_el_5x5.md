# Canlı maç ölçüm raporu (botlar)

Üretildi: 2026-11-13 19:26 · `LiveMatchTools.RunReport` (tools/unity.ps1 live-report) · havuz `docs/vr1/canli_mac/havuz_5x5_farkli_el_v2.txt` (gen 2, tohum 20261113)

Bu sayılar **bot** maçlarıdır, gerçek oyuncu davranışı değildir. Kuralların kaba dengesini ve açık ayarların etkisini karşılaştırmak içindir.

## Kurallar

- tahta 5x5, el 8 (yenilemesiz), tur 8/oyuncu, dokunarak patlatma kapalı, rakipte 3'lü kuran saldırı geçersiz, eşitlik bozma: en büyük tek grup, o da eşitse ikinci oynayan, eller farklı ama denk, zincir yok, 2 katman
- Dokunarak patlatma her ölçümde açık ve kapalı ayrı çalıştırıldı.

## Havuz çeşitliliği

- Üretim: 178 aday denendi, 150 kabul, istenen tür yok 19, eşdeğer kopya 0; filtre retleri: denk ikinci el yok: 8, iki renkli fırsat yok: 1, süre 1759 ms
- Dizilim: 150
- Farklı ama denk eller: 150 dizilim; iki elin en iyi hemen puanı hepsinde eşit; fırsat sayısı farkı ortalama 0,00 (en çok 0; ortalama fırsat 23,8); hedef hücre farkı ortalama 0,06
- Açılışta geçerli hedef hücresi (ortalama): 18,1; fırsat (renk+hedef): 23,8; fırsatlı renk: 3,48; en büyük grup ort.: 5,09
- Yasal açılış saldırısı (ortalama): 37,4; tek saldırıdan sonra savunanın puan hamlesi: en az 6, ortalama 23,7
- Grup büyüklüğü dağılımı (bütün fırsatlar): 3=2434, 4=823, 5=270, 6=44, 7+=4
- Fırsat konumu (hedef satırı): üst üçte bir 1332, orta 1728, alt 515
- Açılış türleri (dizilim sayısı): uc=145 ara=111 L=150 iki_bolge=150 3_4=150 
- Geçerli hedef sayısı dağılımı: 9:3 10:1 11:2 12:2 13:5 14:7 15:10 16:8 17:18 18:22 19:23 20:18 21:10 22:10 23:5 24:6 

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
| Random | 300 | 48,3 | 42,0 | 9,7 | 6,97 | 6,57 | 16,0 | 0,00 | 23,7 | 39,7 | 36,6 | 0,0 | 0,0 | 0,02 |
| Greedy | 300 | 40,0 | 47,3 | 12,7 | 28,78 | 29,10 | 16,0 | 0,00 | 81,6 | 18,4 | 0,0 | 0,0 | 0,0 | 0,00 |
| Aggressive | 300 | 60,3 | 34,0 | 5,7 | 17,49 | 15,14 | 16,0 | 0,00 | 51,9 | 10,2 | 38,0 | 0,0 | 0,0 | 1,64 |
| Balanced | 300 | 42,7 | 39,0 | 18,3 | 27,73 | 27,59 | 16,0 | 0,00 | 69,4 | 14,3 | 16,3 | 0,0 | 0,0 | 2,24 |
| Aggressive (ilk) – Greedy | 300 | 14,7 | 81,7 | 3,7 | 19,84 | 24,90 | 16,0 | 0,00 | 65,5 | 14,1 | 20,4 | 0,0 | 0,0 | 1,84 |
| Greedy (ilk) – Aggressive | 300 | 78,0 | 15,0 | 7,0 | 25,41 | 20,94 | 16,0 | 0,00 | 68,1 | 15,0 | 16,9 | 0,0 | 0,0 | 1,96 |
| Balanced (ilk) – Greedy | 300 | 73,3 | 17,7 | 9,0 | 29,64 | 26,99 | 16,0 | 0,00 | 75,2 | 17,2 | 7,6 | 0,0 | 0,0 | 2,68 |
| Greedy (ilk) – Balanced | 300 | 16,3 | 73,7 | 10,0 | 26,89 | 29,45 | 16,0 | 0,00 | 74,9 | 17,1 | 8,0 | 0,0 | 0,0 | 2,41 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 48,3 / 42,0 / 9,7 | 48,7 / 42,3 / 9,0 | 48,3 / 51,7 / 0,0 | 48,7 / 51,3 / 0,0 |
| Greedy | 40,0 / 47,3 / 12,7 | 40,7 / 49,3 / 10,0 | 40,0 / 60,0 / 0,0 | 40,7 / 59,3 / 0,0 |
| Aggressive | 60,3 / 34,0 / 5,7 | 60,7 / 34,7 / 4,7 | 60,3 / 39,7 / 0,0 | 60,7 / 39,3 / 0,0 |
| Balanced | 42,7 / 39,0 / 18,3 | 43,7 / 40,0 / 16,3 | 42,7 / 57,3 / 0,0 | 43,7 / 56,3 / 0,0 |
| Aggressive (ilk) – Greedy | 14,7 / 81,7 / 3,7 | 17,3 / 81,7 / 1,0 | 14,7 / 85,3 / 0,0 | 17,3 / 82,7 / 0,0 |
| Greedy (ilk) – Aggressive | 78,0 / 15,0 / 7,0 | 78,0 / 19,3 / 2,7 | 78,0 / 22,0 / 0,0 | 78,0 / 22,0 / 0,0 |
| Balanced (ilk) – Greedy | 73,3 / 17,7 / 9,0 | 76,0 / 18,0 / 6,0 | 73,3 / 26,7 / 0,0 | 76,0 / 24,0 / 0,0 |
| Greedy (ilk) – Balanced | 16,3 / 73,7 / 10,0 | 16,7 / 77,0 / 6,3 | 16,3 / 83,7 / 0,0 | 16,7 / 83,3 / 0,0 |

## Dokunarak patlatma AÇIK

Kazanma sütunları ham puanla (eşitlik bozmasız).

| Eşleşme | Maç | İlk kazanır % | İkinci kazanır % | Berabere % | İlk ort. puan | İkinci ort. puan | Hamle/maç | Pas/maç | Puan % | Hazırlık % | Saldırı % | Dokunma % | Pas % | Saldırı rakibin en iyisini düşürdü (ort.) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Random | 300 | 44,7 | 44,0 | 11,3 | 6,96 | 7,15 | 16,0 | 0,00 | 23,5 | 37,7 | 37,9 | 0,9 | 0,0 | 0,05 |
| Greedy | 300 | 40,7 | 42,3 | 17,0 | 31,34 | 31,32 | 16,0 | 0,00 | 79,2 | 11,8 | 0,0 | 9,0 | 0,0 | 0,00 |
| Aggressive | 300 | 61,3 | 34,3 | 4,3 | 18,28 | 16,05 | 16,0 | 0,00 | 50,0 | 5,5 | 39,9 | 4,6 | 0,0 | 1,61 |
| Balanced | 300 | 41,3 | 35,3 | 23,3 | 30,03 | 29,92 | 16,0 | 0,00 | 69,5 | 10,3 | 14,0 | 6,3 | 0,0 | 2,46 |
| Aggressive (ilk) – Greedy | 300 | 11,7 | 85,7 | 2,7 | 21,52 | 27,07 | 16,0 | 0,00 | 64,2 | 8,6 | 20,3 | 6,9 | 0,0 | 1,81 |
| Greedy (ilk) – Aggressive | 300 | 83,0 | 13,0 | 4,0 | 27,72 | 22,28 | 16,0 | 0,00 | 65,8 | 8,5 | 18,0 | 7,7 | 0,0 | 1,89 |
| Balanced (ilk) – Greedy | 300 | 72,3 | 17,3 | 10,3 | 31,80 | 29,48 | 16,0 | 0,00 | 73,6 | 12,4 | 6,1 | 7,9 | 0,0 | 2,69 |
| Greedy (ilk) – Balanced | 300 | 21,3 | 66,7 | 12,0 | 29,62 | 31,50 | 16,0 | 0,00 | 73,9 | 11,8 | 6,8 | 7,6 | 0,0 | 2,70 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 44,7 / 44,0 / 11,3 | 44,7 / 44,3 / 11,0 | 44,7 / 55,3 / 0,0 | 44,7 / 55,3 / 0,0 |
| Greedy | 40,7 / 42,3 / 17,0 | 45,0 / 44,3 / 10,7 | 40,7 / 59,3 / 0,0 | 45,0 / 55,0 / 0,0 |
| Aggressive | 61,3 / 34,3 / 4,3 | 62,0 / 35,0 / 3,0 | 61,3 / 38,7 / 0,0 | 62,0 / 38,0 / 0,0 |
| Balanced | 41,3 / 35,3 / 23,3 | 42,3 / 36,0 / 21,7 | 41,3 / 58,7 / 0,0 | 42,3 / 57,7 / 0,0 |
| Aggressive (ilk) – Greedy | 11,7 / 85,7 / 2,7 | 13,7 / 85,7 / 0,7 | 11,7 / 88,3 / 0,0 | 13,7 / 86,3 / 0,0 |
| Greedy (ilk) – Aggressive | 83,0 / 13,0 / 4,0 | 83,3 / 16,0 / 0,7 | 83,0 / 17,0 / 0,0 | 83,3 / 16,7 / 0,0 |
| Balanced (ilk) – Greedy | 72,3 / 17,3 / 10,3 | 77,7 / 17,7 / 4,7 | 72,3 / 27,7 / 0,0 | 77,7 / 22,3 / 0,0 |
| Greedy (ilk) – Balanced | 21,3 / 66,7 / 12,0 | 21,7 / 71,7 / 6,7 | 21,3 / 78,7 / 0,0 | 21,7 / 78,3 / 0,0 |

Ölçüm süresi: 132754 ms.
