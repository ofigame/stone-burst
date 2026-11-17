# Canlı maç ölçüm raporu (botlar)

Üretildi: 2026-11-14 13:20 · `LiveMatchTools.RunReport` (tools/unity.ps1 live-report) · havuz `Assets/KatKat/Resources/Live/live_pool_v5.txt` (gen 5, tohum 20261113)

Bu sayılar **bot** maçlarıdır, gerçek oyuncu davranışı değildir. Kuralların kaba dengesini ve açık ayarların etkisini karşılaştırmak içindir.

## Kurallar

- tahta 7x6, el 8 (yenilemesiz), tur 8/oyuncu, dokunarak patlatma açık, rakipte 3'lü kuran saldırı geçersiz, eşitlik bozma: en büyük tek grup, o da eşitse ikinci oynayan, eller farklı ama denk, alt katman üstten farklı, tahta tam dikdörtgen, zincir yok, 3 katman
- Dokunarak patlatma her ölçümde açık ve kapalı ayrı çalıştırıldı.

## Havuz çeşitliliği

- Üretim: 158 aday denendi, 150 kabul, istenen tür yok 4, eşdeğer kopya 0; filtre retleri: denk ikinci el yok: 4, süre 3818 ms
- Dizilim: 150
- Farklı ama denk eller: 150 dizilim; iki elin en iyi hemen puanı hepsinde eşit; fırsat sayısı farkı ortalama 0,00 (en çok 0; ortalama fırsat 52,9); hedef hücre farkı ortalama 0,05
- Açılışta geçerli hedef hücresi (ortalama): 37,2; fırsat (renk+hedef): 52,9; fırsatlı renk: 3,68; en büyük grup ort.: 5,73
- Yasal açılış saldırısı (ortalama): 62,9; tek saldırıdan sonra savunanın puan hamlesi: en az 28, ortalama 52,8
- Grup büyüklüğü dağılımı (bütün fırsatlar): 3=5135, 4=1811, 5=849, 6=126, 7+=15
- Fırsat konumu (hedef satırı): üst üçte bir 2528, orta 2959, alt 2449
- Açılış türleri (dizilim sayısı): uc=149 ara=143 L=150 iki_bolge=150 3_4=150 
- Geçerli hedef sayısı dağılımı: 26:1 27:1 28:1 29:1 30:6 31:4 32:5 33:4 34:5 35:11 36:10 37:13 38:16 39:35 40:23 41:10 42:4 

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
| Random | 300 | 47,3 | 44,7 | 8,0 | 8,45 | 8,29 | 16,0 | 0,00 | 28,0 | 36,5 | 35,5 | 0,0 | 0,0 | 0,06 |
| Greedy | 300 | 47,0 | 47,3 | 5,7 | 49,44 | 49,14 | 16,0 | 0,00 | 96,5 | 3,5 | 0,0 | 0,0 | 0,0 | 0,00 |
| Aggressive | 300 | 73,0 | 26,0 | 1,0 | 27,38 | 18,47 | 16,0 | 0,00 | 58,2 | 1,6 | 40,1 | 0,0 | 0,0 | 1,65 |
| Balanced | 300 | 45,7 | 47,7 | 6,7 | 55,21 | 55,28 | 16,0 | 0,00 | 93,6 | 2,4 | 4,0 | 0,0 | 0,0 | 2,45 |
| Aggressive (ilk) – Greedy | 300 | 11,3 | 87,3 | 1,3 | 24,60 | 40,27 | 16,0 | 0,00 | 72,8 | 2,5 | 24,7 | 0,0 | 0,0 | 1,92 |
| Greedy (ilk) – Aggressive | 300 | 90,7 | 7,0 | 2,3 | 41,38 | 24,70 | 16,0 | 0,00 | 74,8 | 2,4 | 22,9 | 0,0 | 0,0 | 1,96 |
| Balanced (ilk) – Greedy | 300 | 75,3 | 21,3 | 3,3 | 55,71 | 49,07 | 16,0 | 0,00 | 94,8 | 3,4 | 1,8 | 0,0 | 0,0 | 2,62 |
| Greedy (ilk) – Balanced | 300 | 17,7 | 79,3 | 3,0 | 49,21 | 55,97 | 16,0 | 0,00 | 95,1 | 3,6 | 1,3 | 0,0 | 0,0 | 2,22 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 47,3 / 44,7 / 8,0 | 47,3 / 45,7 / 7,0 | 47,3 / 52,7 / 0,0 | 47,3 / 52,7 / 0,0 |
| Greedy | 47,0 / 47,3 / 5,7 | 48,7 / 48,3 / 3,0 | 47,0 / 53,0 / 0,0 | 48,7 / 51,3 / 0,0 |
| Aggressive | 73,0 / 26,0 / 1,0 | 73,3 / 26,0 / 0,7 | 73,0 / 27,0 / 0,0 | 73,3 / 26,7 / 0,0 |
| Balanced | 45,7 / 47,7 / 6,7 | 45,7 / 48,7 / 5,7 | 45,7 / 54,3 / 0,0 | 45,7 / 54,3 / 0,0 |
| Aggressive (ilk) – Greedy | 11,3 / 87,3 / 1,3 | 12,3 / 87,3 / 0,3 | 11,3 / 88,7 / 0,0 | 12,3 / 87,7 / 0,0 |
| Greedy (ilk) – Aggressive | 90,7 / 7,0 / 2,3 | 90,7 / 9,0 / 0,3 | 90,7 / 9,3 / 0,0 | 90,7 / 9,3 / 0,0 |
| Balanced (ilk) – Greedy | 75,3 / 21,3 / 3,3 | 76,3 / 22,3 / 1,3 | 75,3 / 24,7 / 0,0 | 76,3 / 23,7 / 0,0 |
| Greedy (ilk) – Balanced | 17,7 / 79,3 / 3,0 | 18,0 / 80,3 / 1,7 | 17,7 / 82,3 / 0,0 | 18,0 / 82,0 / 0,0 |

### El adaleti (aynı bot iki tarafta; P1 = A eli, P2 = B eli, her dizilim iki sırayla)

50 / 50'ye yakınsa iki el denk. "Dizilim" sütunu: aynı dizilimin iki maçını (sıra değişerek) da aynı elin kazandığı dizilimler; bu oran yüksekse o dizilimde el, sıradan daha belirleyici.

| Eşleşme | Maç | A eli / B eli / berabere % (ham) | A / B % (varsayılan eşitlik) | Ort. puan A / B | Dizilim: A iki maçı da / B iki maçı da / bölündü % |
|---|---|---|---|---|---|
| Random | 300 | 44,0 / 48,0 / 8,0 | 46,7 / 53,3 | 8,28 / 8,46 | 24,0 / 30,7 / 45,3 |
| Greedy | 300 | 53,3 / 41,0 / 5,7 | 56,0 / 44,0 | 49,72 / 48,86 | 40,7 / 28,7 / 30,7 |
| Aggressive | 300 | 54,0 / 45,0 / 1,0 | 54,7 / 45,3 | 23,68 / 22,17 | 24,0 / 14,7 / 61,3 |
| Balanced | 300 | 54,7 / 38,7 / 6,7 | 57,7 / 42,3 | 56,11 / 54,37 | 48,7 / 33,3 / 18,0 |

### İki raunt (kullanıcı 2026-11-14: "rövanş olmayacak 2 raunt olacak")

Her dizilimde bir maç: 1. rauntta P1 A eliyle ilk oynar; 2. rauntta eller ve ilk oynayan değişir. Kazanan iki rauntun toplam puanı; eşitlikte en büyük tek soyma; o da eşitse berabere. Aynı bot iki taraftayken 50 / 50 beklenir (koşullar simetrik); farklı botlarda güçlü botun iki rauntlu kazanma oranı tek rauntla karşılaştırılır. Ayrıca raunt sonunda tahtada kalan katman oranı (maç uzunluğu).

| Eşleşme (P1 – P2) | Maç | P1 / P2 / berabere % (iki raunt toplamı) | Ort. toplam puan farkı | Raunt sonu tahtada kalan katman % |
|---|---|---|---|---|
| Random | 150 | 48,0 / 51,3 / 0,7 | 7,83 | 93,2 |
| Greedy | 150 | 42,7 / 50,7 / 6,7 | 5,88 | 60,7 |
| Aggressive | 150 | 58,0 / 42,0 / 0,0 | 13,71 | 81,4 |
| Balanced | 150 | 34,0 / 35,3 / 30,7 | 2,91 | 56,1 |
| Aggressive (ilk) – Greedy | 150 | 2,7 / 97,3 / 0,0 | 32,66 | 74,3 |
| Greedy (ilk) – Aggressive | 150 | 97,3 / 2,7 / 0,0 | 32,68 | 73,9 |
| Balanced (ilk) – Greedy | 150 | 92,7 / 6,7 / 0,7 | 14,60 | 58,4 |
| Greedy (ilk) – Balanced | 150 | 6,7 / 93,3 / 0,0 | 13,91 | 58,4 |

## Dokunarak patlatma AÇIK

Kazanma sütunları ham puanla (eşitlik bozmasız).

| Eşleşme | Maç | İlk kazanır % | İkinci kazanır % | Berabere % | İlk ort. puan | İkinci ort. puan | Hamle/maç | Pas/maç | Puan % | Hazırlık % | Saldırı % | Dokunma % | Pas % | Saldırı rakibin en iyisini düşürdü (ort.) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Random | 300 | 51,0 | 45,0 | 4,0 | 9,20 | 8,94 | 16,0 | 0,00 | 29,0 | 35,3 | 34,6 | 1,2 | 0,0 | 0,06 |
| Greedy | 300 | 50,3 | 44,0 | 5,7 | 56,18 | 55,88 | 16,0 | 0,00 | 83,6 | 0,0 | 0,0 | 16,4 | 0,0 | 0,00 |
| Aggressive | 300 | 72,0 | 26,3 | 1,7 | 28,09 | 18,20 | 16,0 | 0,00 | 48,7 | 0,0 | 44,6 | 6,7 | 0,0 | 1,75 |
| Balanced | 300 | 47,3 | 44,0 | 8,7 | 63,20 | 63,36 | 16,0 | 0,00 | 82,4 | 0,0 | 0,5 | 17,1 | 0,0 | 5,68 |
| Aggressive (ilk) – Greedy | 300 | 7,7 | 91,3 | 1,0 | 25,65 | 44,45 | 16,0 | 0,00 | 62,8 | 0,0 | 26,5 | 10,7 | 0,0 | 1,98 |
| Greedy (ilk) – Aggressive | 300 | 94,3 | 5,3 | 0,3 | 45,21 | 24,81 | 16,0 | 0,00 | 61,9 | 0,0 | 26,6 | 11,5 | 0,0 | 2,03 |
| Balanced (ilk) – Greedy | 300 | 83,0 | 13,3 | 3,7 | 63,86 | 56,33 | 16,0 | 0,00 | 82,6 | 0,0 | 0,0 | 17,3 | 0,0 | 4,00 |
| Greedy (ilk) – Balanced | 300 | 12,7 | 82,7 | 4,7 | 56,65 | 63,80 | 16,0 | 0,00 | 82,7 | 0,0 | 0,1 | 17,2 | 0,0 | 6,75 |

### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)

| Eşleşme | yok (berabere) | en büyük tek grup | ikinci oynayan | en büyük tek grup, o da eşitse ikinci oynayan |
|---|---|---|---|---|
| Random | 51,0 / 45,0 / 4,0 | 51,3 / 45,7 / 3,0 | 51,0 / 49,0 / 0,0 | 51,3 / 48,7 / 0,0 |
| Greedy | 50,3 / 44,0 / 5,7 | 51,0 / 45,0 / 4,0 | 50,3 / 49,7 / 0,0 | 51,0 / 49,0 / 0,0 |
| Aggressive | 72,0 / 26,3 / 1,7 | 72,7 / 27,0 / 0,3 | 72,0 / 28,0 / 0,0 | 72,7 / 27,3 / 0,0 |
| Balanced | 47,3 / 44,0 / 8,7 | 49,0 / 44,7 / 6,3 | 47,3 / 52,7 / 0,0 | 49,0 / 51,0 / 0,0 |
| Aggressive (ilk) – Greedy | 7,7 / 91,3 / 1,0 | 8,3 / 91,3 / 0,3 | 7,7 / 92,3 / 0,0 | 8,3 / 91,7 / 0,0 |
| Greedy (ilk) – Aggressive | 94,3 / 5,3 / 0,3 | 94,3 / 5,7 / 0,0 | 94,3 / 5,7 / 0,0 | 94,3 / 5,7 / 0,0 |
| Balanced (ilk) – Greedy | 83,0 / 13,3 / 3,7 | 83,7 / 14,3 / 2,0 | 83,0 / 17,0 / 0,0 | 83,7 / 16,3 / 0,0 |
| Greedy (ilk) – Balanced | 12,7 / 82,7 / 4,7 | 13,7 / 85,0 / 1,3 | 12,7 / 87,3 / 0,0 | 13,7 / 86,3 / 0,0 |

### El adaleti (aynı bot iki tarafta; P1 = A eli, P2 = B eli, her dizilim iki sırayla)

50 / 50'ye yakınsa iki el denk. "Dizilim" sütunu: aynı dizilimin iki maçını (sıra değişerek) da aynı elin kazandığı dizilimler; bu oran yüksekse o dizilimde el, sıradan daha belirleyici.

| Eşleşme | Maç | A eli / B eli / berabere % (ham) | A / B % (varsayılan eşitlik) | Ort. puan A / B | Dizilim: A iki maçı da / B iki maçı da / bölündü % |
|---|---|---|---|---|---|
| Random | 300 | 49,3 / 46,7 / 4,0 | 52,7 / 47,3 | 9,27 / 8,87 | 30,0 / 24,7 / 45,3 |
| Greedy | 300 | 52,7 / 41,7 / 5,7 | 55,7 / 44,3 | 56,50 / 55,56 | 35,3 / 24,0 / 40,7 |
| Aggressive | 300 | 46,7 / 51,7 / 1,7 | 47,3 / 52,7 | 22,52 / 23,77 | 19,3 / 24,7 / 56,0 |
| Balanced | 300 | 53,3 / 38,0 / 8,7 | 59,0 / 41,0 | 63,95 / 62,61 | 44,7 / 26,7 / 28,7 |

### İki raunt (kullanıcı 2026-11-14: "rövanş olmayacak 2 raunt olacak")

Her dizilimde bir maç: 1. rauntta P1 A eliyle ilk oynar; 2. rauntta eller ve ilk oynayan değişir. Kazanan iki rauntun toplam puanı; eşitlikte en büyük tek soyma; o da eşitse berabere. Aynı bot iki taraftayken 50 / 50 beklenir (koşullar simetrik); farklı botlarda güçlü botun iki rauntlu kazanma oranı tek rauntla karşılaştırılır. Ayrıca raunt sonunda tahtada kalan katman oranı (maç uzunluğu).

| Eşleşme (P1 – P2) | Maç | P1 / P2 / berabere % (iki raunt toplamı) | Ort. toplam puan farkı | Raunt sonu tahtada kalan katman % |
|---|---|---|---|---|
| Random | 150 | 50,0 / 49,3 / 0,7 | 8,60 | 92,8 |
| Greedy | 150 | 57,3 / 36,7 / 6,0 | 6,21 | 55,5 |
| Aggressive | 150 | 44,7 / 54,7 / 0,7 | 18,20 | 81,5 |
| Balanced | 150 | 38,7 / 37,3 / 24,0 | 3,47 | 49,7 |
| Aggressive (ilk) – Greedy | 150 | 2,7 / 97,3 / 0,0 | 38,75 | 72,2 |
| Greedy (ilk) – Aggressive | 150 | 98,7 / 1,3 / 0,0 | 40,49 | 72,2 |
| Balanced (ilk) – Greedy | 150 | 94,7 / 5,3 / 0,0 | 16,03 | 52,4 |
| Greedy (ilk) – Balanced | 150 | 4,0 / 95,3 / 0,7 | 14,67 | 52,4 |

Ölçüm süresi: 769041 ms.
