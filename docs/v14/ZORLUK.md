# v14 zorluk tablosu (LevelLab `v14`, 2026-11-16 05:11)

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
| 1–5 | 7×8 (5) | 2-3, 2-2 | 4 | 2–3 | 4,2 | 1–4 | — | %24,3 / %95,5 | 4/5 | %62 | 0 | 0 | 0 | 0 |
| 6–7 | 6×7 (2) | 1-1 | 3 | 1 | 3,0 | 3 | — | %33,8 / %35,0 | 2/2 | %100 | 0 | 0 | 0 | 0 |
| 8–14 | 6×7 (3), 7×7 (3), 7×8 (1) | 2-2, 1-2 | 3–4 | 1–2 | 3,9 | 1–5 | 9 | %7,8 / %33,0 | 3/6 | %37 | 1 | 1 | 0 | 0 |
| 15–30 | 7×8 (11), 7×7 (5) | 3-3, 1-2 | 3–4 | 2 | 4,8 | 1–5 | 9 | %3,6 / %13,5 | 7/14 | %29 | 2 | 2 | 2 | 0 |
| 31–60 | 7×8 (17), 8×8 (13) | 3-3, 1-2 | 3–4 | 2–3 | 5,8 | 1–3 | 7 | %2,0 / %12,5 | 0/26 | %14 | 4 | 3 | 5 | 0 |
| 61–100 | 8×8 (21), 8×9 (18), 7×9 (1) | 3-4, 3-3, 1-2 | 3–5 | 2–3 | 6,7 | 0–3 | 7 | %0,4 / %6,0 | 0/35 | %14 | 5 | 4 | 7 | 0 |
| 101–200 | 8×9 (82), 8×8 (10), 7×9 (8) | 3-4, 1-2 | 3–5 | 2–3 | 7,7 | 0–2 | 6 | %0,3 / %8,0 | 0/86 | %14 | 14 | 10 | 16 | 0 |
| 201–300 | 8×9 (79), 8×8 (12), 7×9 (9) | 4-4, 1-2 | 4–6 | 2–3 | 7,9 | 0–2 | 6 | %0,2 / %6,0 | 0/87 | %13 | 13 | 10 | 17 | 0 |
| 301–500 | 8×9 (163), 7×9 (19), 8×8 (18) | 4-4, 1-2 | 3–6 | 1–3 | 9,4 | 0–1 | 5 | %0,0 / %2,0 | 0/173 | %12 | 27 | 20 | 33 | 2 |
| 501–700 | 8×9 (162), 8×8 (20), 7×9 (18) | 4-4, 1-2 | 4–6 | 2–4 | 9,6 | 0–1 | 5 | %0,1 / %3,0 | 0/174 | %10 | 26 | 20 | 34 | 0 |
| 701–800 | 8×9 (83), 8×8 (9), 7×9 (8) | 4-4, 1-2 | 4–6 | 2–4 | 9,4 | 0–1 | 5 | %0,1 / %2,0 | 0/86 | %12 | 14 | 10 | 16 | 0 |
| 801–1000 | 8×9 (162), 7×9 (20), 8×8 (18) | 4-4, 1-2 | 4–6 | 2–4 | 9,7 | 0–1 | 5 | %0,1 / %2,5 | 0/174 | %10 | 26 | 20 | 34 | 0 |

## PLAN ölçütleri (sayım)

- Pay bandı dışında: 0 bölüm (kesin hamle türleri ve usta bölümleri: pay 1–99'da ≤ 1, sonra 0).
- Rastgele bot tavanı aşan: 0 bölüm (öğretici turlu 1–3 hariç).
- 31+ nefes dışı açgözlü botun kazandığı: 0 bölüm.
- Yedek aday (bir ölçüt tutmadı, en yakın aday): 2 bölüm.

## Örnek bölümler

| Bölüm | Tür | Tahta | Kat | Renk | Hedefler | Mekanik | Çözüm / sınır | Rastgele | Açgözlü | Şekil |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | vitrin | 7×8 | 2-2 | 4 | purple 6,navy 6 | - | 4 / 8 | %95,5 | kazanır | kesik köşe |
| 2 | vitrin | 7×8 | 2-3 | 4 | green 15,purple 18 | - | 4+3t / 8 | %19,5 | kazanır | artı |
| 3 | vitrin | 7×8 | 2-3 | 4 | coral 8,star 2 | clip,star,linkedClip | 5+5t / 9 | %3,5 | kaybeder | merdiven (ayna) |
| 4 | vitrin | 7×8 | 2-3 | 4 | yellow 19,envelope 2,sponge 1 | ice,colorLock,envelope,sponge | 4+3t / 8 | %3,0 | kazanır | merdiven |
| 5 | vitrin | 7×8 | 2-3 | 4 | coral 18,yellow 12 | rotor,gate,paint,crystal | 4+2t / 5 | %0,0 | kazanır | merdiven (ayna) |
| 6 | normal | 6×7 | 1-1 | 3 | navy 12 | - | 3 / 6 | %35,0 | kazanır | kenar çentikleri |
| 9 | normal | 6×7 | 2-2 | 3 | blue 12 | - | 4+1t / 7 | %33,0 | kaybeder | kesik köşe |
| 15 | normal | 7×7 | 3-3 | 4 | coral 30,navy 38 | - | 6+5t / 10 | %1,5 | kaybeder | kum saati (ayna) |
| 18 | nefes | 7×8 | 1-2 | 3 | coral 33,navy 4 | - | 3+1t / 12 | %98,0 | kazanır | merdiven |
| 21 | normal | 7×8 | 3-3 | 4 | coral 25,yellow 45 | clip | 5+5t / 10 | %9,5 | kazanır | L |
| 27 | tür:exact | 7×8 | 3-3 | 4 | coral 30,yellow 40 | clip | 5+4t / 6 | %0,0 | kaybeder | merdiven |
| 40 | usta | 7×8 | 3-3 | 4 | coral 38,blue 33 | clip | 6+4t / 7 | %0,0 | kaybeder | T |
| 41 | normal | 7×8 | 3-3 | 4 | purple 30,yellow 23,star 3 | star | 6+6t / 8 | %0,0 | kaybeder | T |
| 57 | tür:protect | 8×8 | 3-3 | 4 | green 43,purple 45,star 2 | star,crystal | 8+6t / 10 | %0,0 | kaybeder | kum saati (ayna) |
| 66 | nefes | 8×8 | 1-2 | 3 | blue 20,yellow 27 | - | 3+2t / 10 | %50,5 | kazanır | T (ayna) |
| 100 | usta | 8×8 | 3-4 | 3 | yellow 22,green 25,coral 21 | clip,ice,crystal | 9 / 9 | %0,0 | kaybeder | artı |
| 121 | normal | 8×9 | 3-4 | 5 | yellow 29,green 42,star 3 | star | 7+6t / 9 | %0,0 | kaybeder | çatı |
| 127 | tür:order | 8×9 | 3-4 | 5 | coral 29,blue 29,navy 40 | ice,colorLock | 8+6t / 9 | %0,0 | kaybeder | L |
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
