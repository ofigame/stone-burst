# v14 zorluk tablosu (LevelLab `v14`, 2026-11-26 20:23)

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
| 1–5 | 7×7 (3), 7×8 (1), 8×8 (1) | 2-2 | 2–3 | 2 | 4,4 | 5–6 | — | %70,3 / %87,5 | 5/5 | %100 | 0 | 0 | 0 | 0 |
| 6–7 | 8×8 (2) | 2-2 | 2 | 2 | 5,0 | 5–6 | — | %77,8 / %90,0 | 2/2 | %100 | 0 | 0 | 0 | 0 |
| 8–14 | 7×7 (3), 8×8 (2), 8×9 (1), 7×8 (1) | 2-2 | 3 | 1–2 | 5,6 | 4–8 | 7 | %38,0 / %54,5 | 6/6 | %96 | 1 | 0 | 0 | 0 |
| 15–30 | 7×8 (11), 7×7 (5) | 2-2 | 3–4 | 2 | 4,9 | 4–6 | 7 | %22,8 / %34,5 | 14/14 | %93 | 2 | 2 | 2 | 0 |
| 31–60 | 7×8 (17), 8×8 (13) | 3-3, 2-2 | 3–4 | 2–3 | 7,3 | 2–7 | 6–7 | %1,1 / %4,5 | 0/26 | %40 | 4 | 3 | 5 | 0 |
| 61–100 | 8×8 (21), 8×9 (18), 7×9 (1) | 2-2, 3-3 | 3–5 | 2–3 | 7,4 | 1–7 | 6–7 | %0,5 / %6,0 | 0/35 | %38 | 5 | 4 | 7 | 0 |
| 101–200 | 8×9 (82), 8×8 (10), 7×9 (8) | 2-2, 3-3 | 3–5 | 2–3 | 7,9 | 0–5 | 6–7 | %0,3 / %5,5 | 0/86 | %31 | 14 | 10 | 16 | 0 |
| 201–300 | 8×9 (79), 8×8 (12), 7×9 (9) | 2-2, 3-3 | 4–5 | 2–3 | 8,6 | 0–5 | 7 | %0,2 / %2,0 | 0/87 | %26 | 13 | 10 | 17 | 0 |
| 301–500 | 8×9 (163), 7×9 (19), 8×8 (18) | 3-3, 2-2 | 4–5 | 2–3 | 10,0 | 0–4 | 6 | %0,1 / %2,0 | 0/173 | %19 | 27 | 20 | 33 | 0 |
| 501–700 | 8×9 (162), 8×8 (20), 7×9 (18) | 3-3, 2-2 | 4–5 | 2–4 | 10,4 | 0–4 | 6 | %0,0 / %1,0 | 0/174 | %14 | 26 | 20 | 34 | 0 |
| 701–800 | 8×9 (83), 8×8 (9), 7×9 (8) | 3-3, 2-2 | 4–5 | 2–4 | 10,1 | 0–4 | 6 | %0,1 / %2,5 | 0/86 | %15 | 14 | 10 | 16 | 0 |
| 801–1000 | 8×9 (162), 7×9 (20), 8×8 (18) | 3-3, 2-2 | 4–5 | 2–4 | 11,0 | 0–4 | 6–7 | %0,1 / %3,0 | 0/174 | %14 | 26 | 20 | 34 | 0 |

## PLAN ölçütleri (sayım)

- Pay bandı dışında: 248 bölüm (kesin hamle türleri ve usta bölümleri: pay 1–99'da ≤ 1, sonra 0).
- Rastgele bot tavanı aşan: 7 bölüm (öğretici turlu 1–3 hariç).
- 31+ nefes dışı açgözlü botun kazandığı: 0 bölüm.
- Yedek aday (bir ölçüt tutmadı, en yakın aday): 0 bölüm.

## Örnek bölümler

| Bölüm | Tür | Tahta | Kat | Renk | Hedefler | Mekanik | Çözüm / sınır | Rastgele | Açgözlü | Şekil |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | vitrin | 7×8 | 2-2 | 3 | coral 8,yellow 10 | refill | 5 / 10 | %45,5 | kazanır | T |
| 2 | normal | 7×7 | 2-2 | 2 | blue 26,yellow 27 | refill | 4+1t / 10 | %81,0 | kazanır | artı 7×7 |
| 3 | normal | 7×7 | 2-2 | 2 | blue 29,yellow 29 | refill | 4+1t / 10 | %87,5 | kazanır | yuvarlak 7×7 |
| 4 | normal | 7×7 | 2-2 | 2 | blue 34,yellow 35 | refill | 5+1t / 10 | %78,5 | kazanır | çanak 7×7 |
| 5 | normal | 8×8 | 2-2 | 2 | yellow 37,blue 41 | refill | 4+1t / 10 | %59,0 | kazanır | artı 8×8 |
| 6 | normal | 8×8 | 2-2 | 2 | blue 42,yellow 40 | refill | 5 / 10 | %90,0 | kazanır | yuvarlak 8×8 |
| 9 | normal | 8×8 | 2-2 | 3 | coral 32,yellow 20 | refill | 6+2t / 14 | %50,0 | kazanır | artı 8×8 |
| 15 | normal | 7×7 | 2-2 | 3 | yellow 21,blue 17 | refill | 5+2t / 10 | %26,0 | kazanır | kesik köşe (ayna) |
| 18 | nefes | 7×8 | 2-2 | 3 | blue 25,coral 21 | refill | 5+2t / 12 | %53,0 | kazanır | çatı |
| 21 | normal | 7×8 | 2-2 | 4 | purple 16,yellow 20 | clip,refill | 5+2t / 10 | %20,0 | kazanır | U (ayna) |
| 27 | tür:exact | 7×8 | 2-2 | 4 | blue 12,yellow 14 | clip,refill | 4+1t / 10 | %24,0 | kazanır | kesik köşe (ayna) |
| 40 | usta | 7×8 | 3-3 | 4 | blue 27,purple 23 | clip,refill | 6+3t / 10 | %3,5 | kaybeder | kesik köşe (ayna) |
| 41 | normal | 7×8 | 3-3 | 4 | coral 20,blue 17,star 3 | star,refill | 9+3t / 13 | %0,0 | kaybeder | dalga |
| 57 | tür:protect | 8×8 | 2-2 | 4 | purple 14,blue 15,yellow 15 | gravel,refill,crystal | 6+2t / 10 | %0,5 | kaybeder | kenar çentikleri |
| 66 | nefes | 8×8 | 2-2 | 3 | yellow 23,coral 23 | refill | 5+1t / 12 | %58,5 | kazanır | L |
| 100 | usta | 8×8 | 3-3 | 3 | yellow 27,navy 28,coral 29 | clip,ice,refill,crystal | 11 / 15 | %0,0 | kaybeder | çatı (ayna) |
| 121 | normal | 8×9 | 2-2 | 5 | coral 24,navy 23,yellow 18 | ice,refill | 8+3t / 10 | %0,0 | kaybeder | U |
| 127 | tür:order | 8×9 | 2-2 | 5 | navy 22,coral 21,star 2 | star,colorLock,refill | 8+3t / 10 | %0,0 | kaybeder | zikzak |
| 201 | normal | 8×9 | 3-3 | 5 | navy 18,yellow 13,envelope 3 | clip,envelope,refill | 9+4t / 12 | %0,0 | kaybeder | elmas (ayna) |
| 246 | nefes | 8×9 | 2-2 | 4 | blue 26,yellow 21 | refill | 5+1t / 12 | %38,5 | kazanır | zikzak |
| 301 | normal | 8×9 | 3-3 | 5 | blue 30,yellow 27,coral 24 | ice,refill | 10+4t / 12 | %0,0 | kaybeder | merdiven (ayna) |
| 401 | normal | 8×9 | 3-3 | 5 | navy 32,blue 31,sponge 1 | sponge,gravel,refill | 17+7t / 19 | %0,0 | kaybeder | L (ayna) |
| 500 | usta | 8×9 | 3-3 | 5 | blue 32,purple 30,sponge 1 | sponge,refill,crystal | 11+5t / 15 | %0,0 | kaybeder | dikdörtgen |
| 501 | normal | 8×9 | 3-3 | 5 | yellow 32,navy 25,coral 25 | colorLock,linkedClip,refill | 10+5t / 12 | %0,0 | kaybeder | kenar çentikleri |
| 601 | normal | 8×9 | 3-3 | 5 | purple 26,navy 24,blue 27 | rotor,gate,refill | 12+5t / 14 | %0,0 | kaybeder | artı |
| 701 | normal | 8×9 | 3-3 | 5 | purple 23,blue 27,coral 27,star 3 | star,paint,refill | 15+5t / 16 | %2,0 | kaybeder | artı |
| 801 | normal | 8×9 | 3-3 | 5 | yellow 33,purple 24,blue 35 | colorLock,refill | 10+5t / 12 | %0,0 | kaybeder | dalga |
| 900 | usta | 8×9 | 3-3 | 4 | blue 28,navy 39,purple 39 | gate,refill,crystal | 25 / 29 | %0,0 | kaybeder | U |
| 999 | normal | 8×9 | 3-3 | 5 | coral 34,navy 23,purple 25,sponge 2 | sponge,paint,refill | 13+6t / 14 | %0,0 | kaybeder | artı |
| 1000 | usta | 8×9 | 3-3 | 4 | blue 21,purple 23,coral 31,envelope 2 | envelope,refill,crystal | 19 / 23 | %0,0 | kaybeder | elmas (ayna) |

## Bayraklı bölümler

- 302 (normal): döndürmeyle başlar;
- 303 (normal): döndürmeyle başlar;
- 305 (normal): döndürmeyle başlar;
- 309 (normal): döndürmeyle başlar;
- 310 (usta): döndürmeyle başlar;
- 313 (normal): döndürmeyle başlar;
- 314 (normal): döndürmeyle başlar;
- 315 (normal): döndürmeyle başlar;
- 316 (normal): döndürmeyle başlar;
- 319 (normal): döndürmeyle başlar;
- 320 (usta): döndürmeyle başlar;
- 321 (normal): döndürmeyle başlar;
- 322 (normal): döndürmeyle başlar;
- 323 (normal): döndürmeyle başlar;
- 325 (normal): döndürmeyle başlar;
- 327 (tür:reverse): döndürmeyle başlar;
- 328 (tür:protect): döndürmeyle başlar;
- 329 (normal): döndürmeyle başlar;
- 330 (usta): döndürmeyle başlar;
- 331 (normal): döndürmeyle başlar;
- 332 (normal): döndürmeyle başlar;
- 334 (normal): döndürmeyle başlar;
- 335 (normal): döndürmeyle başlar;
- 337 (tür:scarce): döndürmeyle başlar;
- 338 (tür:pure): döndürmeyle başlar;
- 339 (normal): döndürmeyle başlar;
- 341 (normal): döndürmeyle başlar;
- 343 (normal): döndürmeyle başlar;
- 344 (normal): döndürmeyle başlar;
- 345 (normal): döndürmeyle başlar;
- 346 (normal): döndürmeyle başlar;
- 347 (tür:resource): döndürmeyle başlar;
- 350 (usta): döndürmeyle başlar;
- 352 (normal): döndürmeyle başlar;
- 353 (normal): döndürmeyle başlar;
- 355 (normal): döndürmeyle başlar;
- 357 (tür:exact): döndürmeyle başlar;
- 359 (normal): döndürmeyle başlar;
- 360 (usta): döndürmeyle başlar;
- 361 (normal): döndürmeyle başlar;
- 362 (normal): döndürmeyle başlar;
- 363 (normal): döndürmeyle başlar;
- 364 (normal): döndürmeyle başlar;
- 368 (tür:scarce): döndürmeyle başlar;
- 369 (normal): döndürmeyle başlar;
- 371 (normal): döndürmeyle başlar;
- 373 (normal): döndürmeyle başlar;
- 375 (normal): döndürmeyle başlar;
- 376 (normal): döndürmeyle başlar;
- 377 (tür:pure): döndürmeyle başlar;
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
- 394 (normal): döndürmeyle başlar;
- 395 (normal): döndürmeyle başlar;
- 398 (tür:protect): döndürmeyle başlar;
- 419 (normal): döndürmeyle başlar;
- 437 (tür:protect): döndürmeyle başlar;
- 441 (normal): döndürmeyle başlar;
- 445 (normal): döndürmeyle başlar;
- 455 (normal): döndürmeyle başlar;
- 458 (tür:exact): döndürmeyle başlar;
- 461 (normal): döndürmeyle başlar;
- 471 (normal): döndürmeyle başlar;
- 473 (normal): döndürmeyle başlar;
- 476 (normal): döndürmeyle başlar;
- 488 (tür:order): döndürmeyle başlar;
- 493 (normal): döndürmeyle başlar;
- 505 (normal): döndürmeyle başlar;
- 514 (normal): döndürmeyle başlar;
- 531 (normal): döndürmeyle başlar;
- 532 (normal): döndürmeyle başlar;
- 536 (normal): döndürmeyle başlar;
- 545 (normal): döndürmeyle başlar;
- 554 (normal): döndürmeyle başlar;
- 569 (normal): döndürmeyle başlar;
- 581 (normal): döndürmeyle başlar;
- 601 (normal): döndürmeyle başlar;
- 614 (normal): döndürmeyle başlar;
- 626 (normal): döndürmeyle başlar;
- 627 (tür:resource): döndürmeyle başlar;
- 635 (normal): döndürmeyle başlar;
- 638 (tür:reverse): döndürmeyle başlar;
- 643 (normal): döndürmeyle başlar;
- 649 (normal): döndürmeyle başlar;
- 673 (normal): döndürmeyle başlar;
- 674 (normal): döndürmeyle başlar;
- 693 (normal): döndürmeyle başlar;
- 694 (normal): döndürmeyle başlar;
- 698 (tür:order): döndürmeyle başlar;
- 703 (normal): döndürmeyle başlar;
- 716 (normal): döndürmeyle başlar;
- 740 (usta): döndürmeyle başlar;
- 746 (normal): döndürmeyle başlar;
- 751 (normal): döndürmeyle başlar;
- 761 (normal): döndürmeyle başlar;
- 773 (normal): döndürmeyle başlar;
- 788 (tür:scarce): döndürmeyle başlar;
- 803 (normal): döndürmeyle başlar;
- 805 (normal): döndürmeyle başlar;
- 821 (normal): döndürmeyle başlar;
- 836 (normal): döndürmeyle başlar;
- 844 (normal): döndürmeyle başlar;
- 851 (normal): döndürmeyle başlar;
- 866 (normal): döndürmeyle başlar;
- 895 (normal): döndürmeyle başlar;
- 896 (normal): döndürmeyle başlar;
- 904 (normal): döndürmeyle başlar;
- 909 (normal): döndürmeyle başlar;
- 911 (normal): döndürmeyle başlar;
- 925 (normal): döndürmeyle başlar;
- 926 (normal): döndürmeyle başlar;
- 941 (normal): döndürmeyle başlar;
- 943 (normal): döndürmeyle başlar;
- 953 (normal): döndürmeyle başlar;
- 969 (normal): döndürmeyle başlar;
- 971 (normal): döndürmeyle başlar;
- 980 (usta): döndürmeyle başlar;
- 985 (normal): döndürmeyle başlar;
- 986 (normal): döndürmeyle başlar;
- 987 (tür:exact): döndürmeyle başlar;
