# v14 zorluk tablosu (LevelLab `v14`, 2026-11-20 18:12)

Kaynak: `docs/v14/PLAN.md`. Bütün sayılar üreticinin ölçümüdür (`levels_src_v14/_olcum/*.tsv`), tahmin değildir.

- **Çözüm**: ışın çözücü (genişlik 60) + bir eksik hamle sınırıyla genişlik 150 ışınla sıkıştırma; kayıtlı çözüm gerçek motorla kazanır.
- **Pay** = hamle sınırı − en iyi bulunan çözüm (tepsi kullanımı hamle saymaz).
- **Rastgele bot**: her geçerli hamleden (grup başına bir dokunuş, tepsi, döndürme, boya) düzgün rastgele seçer; 200 oyun, sabit tohum.
- **Açgözlü**: (B) en çok katman soyan hamle, (H) en çok hedef ilerleten hamle; ikisi de sınırda oynar. "Kaybeder" = ikisi de kaybeder.
- **Yumuşak bot**: bir adım sonrası hedef değerine göre sıcaklıklı seçim (plan yapmayan iyi oyuncu yaklaşığı), 48 oyun. Bilgi amaçlı.
- Nefes: 10'dan sonra her 6. bölüm (x0 hariç). Usta: her x0 bölümü (tür `boss`, kesin hamle).

Denetim: 0 sorunlu bölüm (statik doğrulama, kayıtlı çözüm, iç boşluk, yıldız eşiği).

## Bölge tablosu

| Bölüm | Tahta | Kat | Renk | Görev | Çözüm (ort.) | Pay (nefes dışı) | Pay (nefes) | Rastgele bot ort. / en çok (nefes dışı) | Açgözlü kazanır (nefes dışı) | Yumuşak bot ort. | Nefes | Usta | Tür | Yedek |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1–5 | 7×7 (3), 7×8 (1), 8×8 (1) | 2-2 | 2–3 | 2 | 4,2 | 4–8 | — | %58,1 / %74,5 | 5/5 | %90 | 0 | 0 | 0 | 1 |
| 6–7 | 8×8 (2) | 2-2 | 2 | 2 | 4,0 | 8 | — | %56,3 / %59,5 | 2/2 | %94 | 0 | 0 | 0 | 0 |
| 8–14 | 7×7 (3), 8×8 (2), 8×9 (1), 7×8 (1) | 2-2, 1-2 | 3 | 1–2 | 3,9 | 3–8 | 9 | %30,4 / %58,0 | 4/6 | %71 | 1 | 0 | 0 | 1 |
| 15–30 | 7×8 (11), 7×7 (5) | 3-3, 1-2 | 3–4 | 2 | 4,9 | 1–5 | 9 | %13,1 / %61,5 | 6/14 | %37 | 2 | 2 | 2 | 1 |
| 31–60 | 7×8 (17), 8×8 (13) | 3-3, 1-2 | 3–4 | 2–3 | 6,0 | 1–3 | 7 | %1,2 / %11,0 | 0/26 | %9 | 4 | 3 | 5 | 0 |
| 61–100 | 8×8 (21), 8×9 (18), 7×9 (1) | 3-4, 3-3, 1-2 | 3–5 | 2–3 | 6,5 | 0–3 | 7 | %0,8 / %8,0 | 0/35 | %15 | 5 | 4 | 7 | 0 |
| 101–200 | 8×9 (82), 8×8 (10), 7×9 (8) | 3-4, 1-2 | 3–5 | 2–3 | 7,5 | 0–2 | 6 | %0,2 / %6,5 | 0/86 | %12 | 14 | 10 | 16 | 0 |
| 201–300 | 8×9 (79), 8×8 (12), 7×9 (9) | 4-4, 1-2 | 4–6 | 2–3 | 7,9 | 0–2 | 6 | %0,2 / %6,0 | 0/87 | %13 | 13 | 10 | 17 | 0 |
| 301–500 | 8×9 (163), 7×9 (19), 8×8 (18) | 4-4, 1-2 | 3–6 | 1–3 | 9,4 | 0–1 | 5 | %0,0 / %2,0 | 0/173 | %12 | 27 | 20 | 33 | 2 |
| 501–700 | 8×9 (162), 8×8 (20), 7×9 (18) | 4-4, 1-2 | 4–6 | 2–4 | 9,6 | 0–1 | 5 | %0,1 / %3,0 | 0/174 | %10 | 26 | 20 | 34 | 0 |
| 701–800 | 8×9 (83), 8×8 (9), 7×9 (8) | 4-4, 1-2 | 4–6 | 2–4 | 9,4 | 0–1 | 5 | %0,1 / %2,0 | 0/86 | %12 | 14 | 10 | 16 | 0 |
| 801–1000 | 8×9 (162), 7×9 (20), 8×8 (18) | 4-4, 1-2 | 4–6 | 2–4 | 9,7 | 0–1 | 5 | %0,1 / %2,5 | 0/174 | %10 | 26 | 20 | 34 | 0 |

## PLAN ölçütleri (sayım)

- Pay bandı dışında: 9 bölüm (kesin hamle türleri ve usta bölümleri: pay 1–99'da ≤ 1, sonra 0).
- Rastgele bot tavanı aşan: 3 bölüm (öğretici turlu 1–3 hariç).
- 31+ nefes dışı açgözlü botun kazandığı: 0 bölüm.
- Yedek aday (bir ölçüt tutmadı, en yakın aday): 5 bölüm.

## Örnek bölümler

| Bölüm | Tür | Tahta | Kat | Renk | Hedefler | Mekanik | Çözüm / sınır | Rastgele | Açgözlü | Şekil |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | vitrin | 7×8 | 2-2 | 3 | coral 8,blue 7 | - | 5 / 9 | %45,5 | kazanır | çatı (ayna) |
| 2 | normal | 7×7 | 2-2 | 2 | blue 21,yellow 26 | - | 4+2t / 12 | %58,0 | kazanır | artı 7×7 |
| 3 | normal | 7×7 | 2-2 | 2 | blue 36,yellow 34 | - | 4+2t / 12 | %48,5 | kazanır | yuvarlak 7×7 |
| 4 | normal | 7×7 | 2-2 | 2 | yellow 26,blue 33 | - | 4+2t / 12 | %74,5 | kazanır | çanak 7×7 |
| 5 | normal | 8×8 | 2-2 | 2 | blue 44,yellow 40 | - | 4+3t / 12 | %64,0 | kazanır | artı 8×8 |
| 6 | normal | 8×8 | 2-2 | 2 | yellow 47,blue 40 | - | 4+3t / 12 | %59,5 | kazanır | yuvarlak 8×8 |
| 9 | normal | 8×8 | 2-2 | 3 | blue 22,yellow 27 | - | 4+2t / 10 | %49,5 | kazanır | artı 8×8 |
| 15 | normal | 7×7 | 3-3 | 3 | yellow 38,blue 35 | - | 5+4t / 8 | %32,5 | kazanır | kesik köşe (ayna) |
| 18 | nefes | 7×8 | 1-2 | 3 | blue 15,coral 27 | - | 3+2t / 12 | %81,5 | kazanır | kum saati |
| 21 | normal | 7×8 | 3-3 | 4 | purple 44,yellow 21 | clip | 5+4t / 10 | %26,0 | kazanır | kum saati (ayna) |
| 27 | tür:exact | 7×8 | 3-3 | 4 | yellow 32,purple 39 | clip | 5+5t / 6 | %2,5 | kaybeder | kesik köşe |
| 40 | usta | 7×8 | 3-3 | 4 | yellow 36,coral 39 | clip | 7+5t / 8 | %0,5 | kaybeder | kum saati (ayna) |
| 41 | normal | 7×8 | 3-3 | 4 | purple 27,blue 47,star 3 | star | 7+5t / 9 | %0,5 | kaybeder | kum saati |
| 57 | tür:protect | 8×8 | 3-3 | 4 | purple 23,blue 28,star 2 | star,crystal | 6+5t / 8 | %0,0 | kaybeder | elmas |
| 66 | nefes | 8×8 | 1-2 | 3 | yellow 21,coral 15 | - | 3+2t / 10 | %68,0 | kazanır | L |
| 100 | usta | 8×8 | 3-4 | 3 | yellow 31,green 34,coral 20 | clip,ice,crystal | 11 / 11 | %0,0 | kaybeder | U (ayna) |
| 121 | normal | 8×9 | 3-4 | 5 | coral 24,green 25,star 3 | star | 12+9t / 14 | %2,0 | kaybeder | kum saati |
| 127 | tür:order | 8×9 | 3-4 | 5 | coral 25,yellow 26,purple 16 | ice,colorLock | 9+7t / 10 | %0,0 | kaybeder | kenar çentikleri |
| 201 | normal | 8×9 | 4-4 | 6 | navy 25,blue 33,envelope 3 | colorLock,envelope | 8+5t / 10 | %0,0 | kaybeder | U |
| 246 | nefes | 8×9 | 1-2 | 4 | purple 21,blue 22 | - | 3+2t / 9 | %33,0 | kazanır | L (ayna) |
| 301 | normal | 8×9 | 4-4 | 6 | navy 41,green 25,envelope 3 | ice,envelope | 10+8t / 11 | %0,0 | kaybeder | kenar çentikleri |
| 401 | normal | 8×9 | 4-4 | 6 | navy 37,purple 34,sponge 1 | colorLock,sponge | 10+7t / 11 | %0,0 | kaybeder | artı |
| 500 | usta | 8×9 | 4-4 | 6 | yellow 34,green 19,sponge 1 | sponge,crystal | 21+12t / 21 | %0,0 | kaybeder | dikdörtgen |
| 501 | normal | 8×9 | 4-4 | 6 | purple 22,coral 36,green 15 | linkedClip | 9+8t / 9 | %0,0 | kaybeder | çatı (ayna) |
| 601 | normal | 8×9 | 4-4 | 6 | purple 27,navy 36,blue 41 | colorLock,gate | 8+7t / 9 | %0,0 | kaybeder | kesik köşe (ayna) |
| 701 | normal | 8×9 | 4-4 | 6 | coral 18,navy 26,green 18 | clip,paint | 8+7t / 9 | %0,0 | kaybeder | artı |
| 801 | normal | 8×9 | 4-4 | 6 | green 30,yellow 26,navy 34,envelope 3 | envelope | 9+8t / 10 | %0,0 | kaybeder | çatı (ayna) |
| 900 | usta | 8×9 | 4-4 | 4 | navy 29,yellow 44,green 36 | paint,crystal | 15 / 15 | %0,0 | kaybeder | U (ayna) |
| 999 | normal | 8×9 | 4-4 | 6 | green 12,purple 30,coral 12 | linkedClip,gate | 9+8t / 9 | %0,0 | kaybeder | elmas |
| 1000 | usta | 8×9 | 4-4 | 4 | green 16,yellow 10,blue 16 | rotor,crystal | 23 / 23 | %0,0 | kaybeder | artı (ayna) |

## Bayraklı bölümler

- 3 (normal): YEDEK: zor;
- 9 (normal): YEDEK: zor;
- 17 (normal): YEDEK: rastgele;
- 302 (normal): döndürmeyle başlar;
- 303 (normal): döndürmeyle başlar;
- 304 (normal): döndürmeyle başlar;
- 307 (tür:pure): döndürmeyle başlar;
- 309 (normal): döndürmeyle başlar;
- 310 (usta): döndürmeyle başlar;
- 313 (normal): döndürmeyle başlar;
- 314 (normal): döndürmeyle başlar;
- 315 (normal): döndürmeyle başlar;
- 316 (normal): döndürmeyle başlar;
- 321 (normal): döndürmeyle başlar;
- 322 (normal): döndürmeyle başlar;
- 323 (normal): döndürmeyle başlar;
- 325 (normal): döndürmeyle başlar;
- 328 (tür:protect): döndürmeyle başlar;
- 329 (normal): döndürmeyle başlar;
- 330 (usta): döndürmeyle başlar;
- 332 (normal): döndürmeyle başlar;
- 334 (normal): döndürmeyle başlar;
- 335 (normal): döndürmeyle başlar;
- 340 (usta): döndürmeyle başlar;
- 344 (normal): döndürmeyle başlar;
- 345 (normal): döndürmeyle başlar;
- 346 (normal): döndürmeyle başlar;
- 352 (normal): döndürmeyle başlar;
- 353 (normal): döndürmeyle başlar; YEDEK: mek:rotor;
- 360 (usta): döndürmeyle başlar;
- 361 (normal): döndürmeyle başlar;
- 362 (normal): döndürmeyle başlar;
- 363 (normal): döndürmeyle başlar;
- 364 (normal): döndürmeyle başlar;
- 369 (normal): döndürmeyle başlar;
- 370 (usta): döndürmeyle başlar;
- 371 (normal): döndürmeyle başlar;
- 373 (normal): döndürmeyle başlar;
- 379 (normal): döndürmeyle başlar;
- 380 (usta): döndürmeyle başlar;
- 385 (normal): döndürmeyle başlar;
- 386 (normal): döndürmeyle başlar;
- 387 (tür:order): döndürmeyle başlar;
- 388 (tür:exact): döndürmeyle başlar;
- 389 (normal): döndürmeyle başlar;
- 390 (usta): döndürmeyle başlar;
- 391 (normal): döndürmeyle başlar;
- 393 (normal): döndürmeyle başlar;
- 395 (normal): döndürmeyle başlar;
- 398 (tür:protect): döndürmeyle başlar;
- 400 (usta): YEDEK: mek:rotor;
- 416 (normal): döndürmeyle başlar;
- 417 (tür:resource): döndürmeyle başlar;
- 419 (normal): döndürmeyle başlar;
- 435 (normal): döndürmeyle başlar;
- 459 (normal): döndürmeyle başlar;
- 464 (normal): döndürmeyle başlar;
- 513 (normal): döndürmeyle başlar;
- 514 (normal): döndürmeyle başlar;
- 535 (normal): döndürmeyle başlar;
- 572 (normal): döndürmeyle başlar;
- 575 (normal): döndürmeyle başlar;
- 596 (normal): döndürmeyle başlar;
- 602 (normal): döndürmeyle başlar;
- 620 (usta): döndürmeyle başlar;
- 622 (normal): gate çıkarıldı;
- 628 (tür:order): döndürmeyle başlar;
- 629 (normal): gate çıkarıldı;
- 645 (normal): döndürmeyle başlar;
- 652 (normal): döndürmeyle başlar;
- 659 (normal): döndürmeyle başlar;
- 664 (normal): döndürmeyle başlar;
- 692 (normal): döndürmeyle başlar; gate çıkarıldı;
- 697 (tür:resource): döndürmeyle başlar;
- 724 (normal): döndürmeyle başlar;
- 758 (tür:pure): döndürmeyle başlar;
- 767 (tür:resource): döndürmeyle başlar;
- 769 (normal): döndürmeyle başlar;
- 776 (normal): döndürmeyle başlar;
- 778 (tür:reverse): döndürmeyle başlar;
- 789 (normal): döndürmeyle başlar;
- 796 (normal): döndürmeyle başlar;
- 799 (normal): döndürmeyle başlar;
- 802 (normal): döndürmeyle başlar;
- 805 (normal): döndürmeyle başlar;
- 806 (normal): döndürmeyle başlar;
- 819 (normal): döndürmeyle başlar;
- 821 (normal): döndürmeyle başlar;
- 839 (normal): döndürmeyle başlar;
- 857 (tür:protect): döndürmeyle başlar;
- 861 (normal): döndürmeyle başlar;
- 871 (normal): döndürmeyle başlar;
- 872 (normal): döndürmeyle başlar;
- 875 (normal): gate çıkarıldı;
- 879 (normal): döndürmeyle başlar;
- 881 (normal): döndürmeyle başlar;
- 883 (normal): döndürmeyle başlar;
- 892 (normal): döndürmeyle başlar;
- 901 (normal): döndürmeyle başlar;
- 908 (tür:order): döndürmeyle başlar;
- 915 (normal): döndürmeyle başlar;
- 916 (normal): döndürmeyle başlar;
- 934 (normal): döndürmeyle başlar;
- 937 (tür:pure): döndürmeyle başlar;
- 945 (normal): döndürmeyle başlar;
- 952 (normal): döndürmeyle başlar;
- 955 (normal): döndürmeyle başlar;
- 963 (normal): döndürmeyle başlar;
- 977 (tür:resource): döndürmeyle başlar;
- 985 (normal): döndürmeyle başlar;
- 989 (normal): döndürmeyle başlar;
