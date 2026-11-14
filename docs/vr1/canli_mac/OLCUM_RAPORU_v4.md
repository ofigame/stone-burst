# Canlı maç ölçüm raporu (botlar)

Üretildi: 2026-11-14 03:25 · `LiveMatchTools.RunReport` (tools/unity.ps1 live-report) · havuz `Assets/KatKat/Resources/Live/live_pool_v4.txt` (gen 4, tohum 20261113)

Bu sayılar **bot** maçlarıdır, gerçek oyuncu davranışı değildir. Kuralların kaba dengesini ve açık ayarların etkisini karşılaştırmak içindir.

## Kurallar

- tahta 6x6, el 8 (yenilemesiz), tur 8/oyuncu, dokunarak patlatma açık, rakipte 3'lü kuran saldırı geçersiz, eşitlik bozma: en büyük tek grup, o da eşitse ikinci oynayan, eller farklı ama denk, alt katman üstten farklı, tahta tam dikdörtgen, zincir yok, 2 katman
- Dokunarak patlatma her ölçümde açık ve kapalı ayrı çalıştırıldı.

## Havuz çeşitliliği

- Üretim: 165 aday denendi, 150 kabul, istenen tür yok 5, eşdeğer kopya 0; filtre retleri: açılış değeri farklı: 1, denk ikinci el yok: 9, süre 3211 ms
- Dizilim: 150
- Farklı ama denk eller: 150 dizilim; iki elin en iyi hemen puanı hepsinde eşit; fırsat sayısı farkı ortalama 0,00 (en çok 0; ortalama fırsat 42,5); hedef hücre farkı ortalama 0,05
- Açılışta geçerli hedef hücresi (ortalama): 30,5; fırsat (renk+hedef): 42,5; fırsatlı renk: 3,54; en büyük grup ort.: 5,51
- Yasal açılış saldırısı (ortalama): 53,6; tek saldırıdan sonra savunanın puan hamlesi: en az 13, ortalama 42,4
- Grup büyüklüğü dağılımı (bütün fırsatlar): 3=4102, 4=1529, 5=634, 6=95, 7+=10
- Fırsat konumu (hedef satırı): üst üçte bir 2010, orta 2393, alt 1967
- Açılış türleri (dizilim sayısı): uc=149 ara=140 L=150 iki_bolge=150 3_4=150 
- Geçerli hedef sayısı dağılımı: 15:1 19:1 21:2 23:2 24:4 25:8 26:6 27:3 28:11 29:13 30:8 31:21 32:14 33:27 34:19 35:8 36:2 

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
| Random | 300 | 43,7 | 50,0 | 6,3 | 8,16 | 8,47 | 16,0 | 0,00 | 27,9 | 35,9 | 36,2 | 0,0 | 0,0 | 0,05 |
| Greedy | 300 | 46,7 | 43,0 | 10,3 | 39,49 | 39,31 | 16,0 | 0,00 | 91,1 | 8,9 | 0,0 | 0,0 | 0,0 | 0,00 |
| Aggressive | 300 | 63,3 | 32,0 | 4,7 | 23,63 | 19,14 | 16,0 | 0,00 | 58,3 | 4,3 | 37,4 | 0,0 | 0,0 | 1,64 |
| Balanced | 300 | 44,7 | 37,7 | 17,7 | 41,17 | 40,82 | 16,0 | 0,00 | 84,5 | 6,6 | 8,9 | 0,0 | 0,0 | 2,20 |
| Aggressive (ilk) – Greedy | 300 | 12,7 | 83,0 | 4,3 | 25,83 | 33,89 | 16,0 | 0,00 | 73,9 | 6,0 | 20,1 | 0,0 | 0,0 | 1,86 |
| Greedy (ilk) – Aggressive | 300 | 84,3 | 12,0 | 3,7 | 34,79 | 25,70 | 16,0 | 0,00 | 75,8 | 6,4 | 17,8 | 0,0 | 0,0 | 1,86 |
| Balanced (ilk) – Greedy | 300 | 79,3 | 11,7 | 9,0 | 42,30 | 38,15 | 16,0 | 0,00 | 88,2 | 7,7 | 4,1 | 0,0 | 0,0 | 2,69 |
| Greedy (ilk) – Balanced | 300 | 16,7 | 76,0 | 7,3 | 38,35 | 42,18 | 16,0 | 0,00 | 88,1 | 8,0 | 4,0 | 0,0 | 0,0 | 2,29 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 43,7 / 50,0 / 6,3 | 44,7 / 50,0 / 5,3 | 43,7 / 56,3 / 0,0 | 44,7 / 55,3 / 0,0 |
| Greedy | 46,7 / 43,0 / 10,3 | 47,3 / 46,3 / 6,3 | 46,7 / 53,3 / 0,0 | 47,3 / 52,7 / 0,0 |
| Aggressive | 63,3 / 32,0 / 4,7 | 65,3 / 32,7 / 2,0 | 63,3 / 36,7 / 0,0 | 65,3 / 34,7 / 0,0 |
| Balanced | 44,7 / 37,7 / 17,7 | 45,3 / 38,0 / 16,7 | 44,7 / 55,3 / 0,0 | 45,3 / 54,7 / 0,0 |
| Aggressive (ilk) – Greedy | 12,7 / 83,0 / 4,3 | 17,0 / 83,0 / 0,0 | 12,7 / 87,3 / 0,0 | 17,0 / 83,0 / 0,0 |
| Greedy (ilk) – Aggressive | 84,3 / 12,0 / 3,7 | 84,3 / 14,7 / 1,0 | 84,3 / 15,7 / 0,0 | 84,3 / 15,7 / 0,0 |
| Balanced (ilk) – Greedy | 79,3 / 11,7 / 9,0 | 83,3 / 12,0 / 4,7 | 79,3 / 20,7 / 0,0 | 83,3 / 16,7 / 0,0 |
| Greedy (ilk) – Balanced | 16,7 / 76,0 / 7,3 | 17,0 / 79,3 / 3,7 | 16,7 / 83,3 / 0,0 | 17,0 / 83,0 / 0,0 |

### El adaleti (aynı bot iki tarafta; P1 = A eli, P2 = B eli, her dizilim iki sırayla)

50 / 50'ye yakınsa iki el denk. "Dizilim" sütunu: aynı dizilimin iki maçını (sıra değişerek) da aynı elin kazandığı dizilimler; bu oran yüksekse o dizilimde el, sıradan daha belirleyici.

| Eşleşme | Maç | A eli / B eli / berabere % (ham) | A / B % (varsayılan eşitlik) | Ort. puan A / B | Dizilim: A iki maçı da / B iki maçı da / bölündü % |
|---|---|---|---|---|---|
| Random | 300 | 51,7 / 42,0 / 6,3 | 56,0 / 44,0 | 8,63 / 8,00 | 30,0 / 18,0 / 52,0 |
| Greedy | 300 | 49,0 / 40,7 / 10,3 | 53,3 / 46,7 | 39,57 / 39,24 | 34,7 / 28,0 / 37,3 |
| Aggressive | 300 | 49,7 / 45,7 / 4,7 | 51,3 / 48,7 | 21,46 / 21,31 | 22,0 / 19,3 / 58,7 |
| Balanced | 300 | 46,0 / 36,3 / 17,7 | 56,7 / 43,3 | 41,11 / 40,88 | 42,7 / 29,3 / 28,0 |

### Rövanşlı maç (kullanıcı 2026-11-14: "rövanşta el değişsin")

Her dizilimde bir seri: 1. el P1 A eliyle ilk oynar; rövanşta eller ve ilk oynayan değişir. Kazanan iki elin toplam puanı; eşitlikte en büyük tek soyma; o da eşitse berabere. Aynı bot iki taraftayken 50 / 50 beklenir (koşullar simetrik); farklı botlarda güçlü botun seri kazanma oranı tek maçtakiyle karşılaştırılır.

| Eşleşme (P1 – P2) | Seri | P1 / P2 / berabere % | Ort. toplam puan farkı |
|---|---|---|---|
| Random | 150 | 50,0 / 46,0 / 4,0 | 7,08 |
| Greedy | 150 | 48,7 / 42,7 / 8,7 | 3,65 |
| Aggressive | 150 | 49,3 / 50,0 / 0,7 | 10,07 |
| Balanced | 150 | 38,7 / 21,3 / 40,0 | 2,09 |
| Aggressive (ilk) – Greedy | 150 | 4,7 / 95,3 / 0,0 | 17,35 |
| Greedy (ilk) – Aggressive | 150 | 93,3 / 6,7 / 0,0 | 17,97 |
| Balanced (ilk) – Greedy | 150 | 92,7 / 6,0 / 1,3 | 8,08 |
| Greedy (ilk) – Balanced | 150 | 7,3 / 92,0 / 0,7 | 8,24 |

## Dokunarak patlatma AÇIK

Kazanma sütunları ham puanla (eşitlik bozmasız).

| Eşleşme | Maç | İlk kazanır % | İkinci kazanır % | Berabere % | İlk ort. puan | İkinci ort. puan | Hamle/maç | Pas/maç | Puan % | Hazırlık % | Saldırı % | Dokunma % | Pas % | Saldırı rakibin en iyisini düşürdü (ort.) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Random | 300 | 46,7 | 44,7 | 8,7 | 8,82 | 9,17 | 16,0 | 0,00 | 28,3 | 37,3 | 33,0 | 1,5 | 0,0 | 0,09 |
| Greedy | 300 | 42,0 | 46,0 | 12,0 | 43,39 | 43,38 | 16,0 | 0,00 | 83,9 | 1,1 | 0,0 | 15,1 | 0,0 | 0,00 |
| Aggressive | 300 | 65,7 | 30,7 | 3,7 | 24,56 | 19,84 | 16,0 | 0,00 | 52,9 | 0,5 | 40,4 | 6,2 | 0,0 | 1,61 |
| Balanced | 300 | 46,0 | 36,7 | 17,3 | 46,19 | 45,87 | 16,0 | 0,00 | 81,5 | 1,3 | 3,2 | 13,9 | 0,0 | 3,02 |
| Aggressive (ilk) – Greedy | 300 | 10,0 | 88,3 | 1,7 | 27,08 | 37,17 | 16,0 | 0,00 | 68,0 | 0,7 | 20,6 | 10,6 | 0,0 | 1,73 |
| Greedy (ilk) – Aggressive | 300 | 92,7 | 4,3 | 3,0 | 38,20 | 27,29 | 16,0 | 0,00 | 67,8 | 0,5 | 19,9 | 11,8 | 0,0 | 1,81 |
| Balanced (ilk) – Greedy | 300 | 80,0 | 12,3 | 7,7 | 46,83 | 43,00 | 16,0 | 0,00 | 83,6 | 1,1 | 0,7 | 14,5 | 0,0 | 2,51 |
| Greedy (ilk) – Balanced | 300 | 18,3 | 73,0 | 8,7 | 43,10 | 46,62 | 16,0 | 0,00 | 83,7 | 0,9 | 1,1 | 14,4 | 0,0 | 3,28 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 46,7 / 44,7 / 8,7 | 48,0 / 45,0 / 7,0 | 46,7 / 53,3 / 0,0 | 48,0 / 52,0 / 0,0 |
| Greedy | 42,0 / 46,0 / 12,0 | 44,3 / 47,3 / 8,3 | 42,0 / 58,0 / 0,0 | 44,3 / 55,7 / 0,0 |
| Aggressive | 65,7 / 30,7 / 3,7 | 67,0 / 31,7 / 1,3 | 65,7 / 34,3 / 0,0 | 67,0 / 33,0 / 0,0 |
| Balanced | 46,0 / 36,7 / 17,3 | 46,7 / 38,0 / 15,3 | 46,0 / 54,0 / 0,0 | 46,7 / 53,3 / 0,0 |
| Aggressive (ilk) – Greedy | 10,0 / 88,3 / 1,7 | 11,7 / 88,3 / 0,0 | 10,0 / 90,0 / 0,0 | 11,7 / 88,3 / 0,0 |
| Greedy (ilk) – Aggressive | 92,7 / 4,3 / 3,0 | 93,0 / 6,7 / 0,3 | 92,7 / 7,3 / 0,0 | 93,0 / 7,0 / 0,0 |
| Balanced (ilk) – Greedy | 80,0 / 12,3 / 7,7 | 83,7 / 13,3 / 3,0 | 80,0 / 20,0 / 0,0 | 83,7 / 16,3 / 0,0 |
| Greedy (ilk) – Balanced | 18,3 / 73,0 / 8,7 | 19,0 / 76,3 / 4,7 | 18,3 / 81,7 / 0,0 | 19,0 / 81,0 / 0,0 |

### El adaleti (aynı bot iki tarafta; P1 = A eli, P2 = B eli, her dizilim iki sırayla)

50 / 50'ye yakınsa iki el denk. "Dizilim" sütunu: aynı dizilimin iki maçını (sıra değişerek) da aynı elin kazandığı dizilimler; bu oran yüksekse o dizilimde el, sıradan daha belirleyici.

| Eşleşme | Maç | A eli / B eli / berabere % (ham) | A / B % (varsayılan eşitlik) | Ort. puan A / B | Dizilim: A iki maçı da / B iki maçı da / bölündü % |
|---|---|---|---|---|---|
| Random | 300 | 48,0 / 43,3 / 8,7 | 52,7 / 47,3 | 8,93 / 9,06 | 26,7 / 21,3 / 52,0 |
| Greedy | 300 | 43,3 / 44,7 / 12,0 | 48,3 / 51,7 | 43,26 / 43,50 | 27,3 / 30,7 / 42,0 |
| Aggressive | 300 | 46,3 / 50,0 / 3,7 | 47,7 / 52,3 | 22,18 / 22,22 | 16,0 / 20,7 / 63,3 |
| Balanced | 300 | 39,3 / 43,3 / 17,3 | 50,0 / 50,0 | 45,94 / 46,12 | 33,3 / 33,3 / 33,3 |

### Rövanşlı maç (kullanıcı 2026-11-14: "rövanşta el değişsin")

Her dizilimde bir seri: 1. el P1 A eliyle ilk oynar; rövanşta eller ve ilk oynayan değişir. Kazanan iki elin toplam puanı; eşitlikte en büyük tek soyma; o da eşitse berabere. Aynı bot iki taraftayken 50 / 50 beklenir (koşullar simetrik); farklı botlarda güçlü botun seri kazanma oranı tek maçtakiyle karşılaştırılır.

| Eşleşme (P1 – P2) | Seri | P1 / P2 / berabere % | Ort. toplam puan farkı |
|---|---|---|---|
| Random | 150 | 46,7 / 51,3 / 2,0 | 8,21 |
| Greedy | 150 | 42,0 / 50,7 / 7,3 | 3,95 |
| Aggressive | 150 | 47,3 / 50,7 / 2,0 | 10,63 |
| Balanced | 150 | 32,7 / 31,3 / 36,0 | 1,88 |
| Aggressive (ilk) – Greedy | 150 | 2,0 / 98,0 / 0,0 | 20,54 |
| Greedy (ilk) – Aggressive | 150 | 99,3 / 0,7 / 0,0 | 21,93 |
| Balanced (ilk) – Greedy | 150 | 93,3 / 6,0 / 0,7 | 8,17 |
| Greedy (ilk) – Balanced | 150 | 9,3 / 90,0 / 0,7 | 7,67 |

Ölçüm süresi: 476777 ms.
