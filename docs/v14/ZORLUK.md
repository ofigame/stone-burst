# v14 zorluk tablosu (LevelLab `v14`, 2026-11-27 01:47)

Kaynak: `docs/v14/PLAN.md`. Bütün sayılar üreticinin ölçümüdür (`levels_src_v14/_olcum/*.tsv`), tahmin değildir.

- **Çözüm**: ışın çözücü (genişlik 60) + bir eksik hamle sınırıyla genişlik 150 ışınla sıkıştırma; kayıtlı çözüm gerçek motorla kazanır.
- **Pay** = hamle sınırı − en iyi bulunan çözüm (roket ve Altın Rulo hamle saymaz, Kürek 1 hamle; 2026-11-27).
- **Rastgele bot**: her geçerli hamleden (grup başına bir dokunuş, tepsi, döndürme, boya) düzgün rastgele seçer; 200 oyun, sabit tohum.
- **Açgözlü**: (B) en çok katman soyan hamle, (H) en çok hedef ilerleten hamle; ikisi de sınırda oynar. "Kaybeder" = ikisi de kaybeder.
- **Yumuşak bot**: bir adım sonrası hedef değerine göre sıcaklıklı seçim (plan yapmayan iyi oyuncu yaklaşığı), 48 oyun. Bilgi amaçlı.
- Nefes: 10'dan sonra her 6. bölüm (x0 hariç). Usta: her x0 bölümü (tür `boss`, kesin hamle).

Denetim: 0 sorunlu bölüm (statik doğrulama, kayıtlı çözüm, iç boşluk, yıldız eşiği).

## Bölge tablosu

| Bölüm | Tahta | Kat | Renk | Görev | Çözüm (ort.) | Pay (nefes dışı) | Pay (nefes) | Rastgele bot ort. / en çok (nefes dışı) | Açgözlü kazanır (nefes dışı) | Yumuşak bot ort. | Nefes | Usta | Tür | Yedek |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1–5 | 7×7 (3), 7×8 (1), 8×8 (1) | 2-2 | 2–3 | 2 | 4,0 | 4–6 | — | %85,4 / %100,0 | 5/5 | %100 | 0 | 0 | 0 | 1 |
| 6–7 | 8×8 (2) | 2-2 | 2 | 2 | 4,0 | 6 | — | %95,0 / %100,0 | 2/2 | %100 | 0 | 0 | 0 | 0 |
| 8–14 | 7×7 (3), 8×8 (2), 8×9 (1), 7×8 (1) | 2-2 | 3 | 1–2 | 3,7 | 6–7 | 7 | %50,3 / %68,5 | 5/6 | %98 | 1 | 0 | 0 | 1 |
| 15–30 | 7×8 (11), 7×7 (5) | 2-2 | 3–4 | 2 | 3,8 | 5–7 | 7 | %23,9 / %41,0 | 12/14 | %93 | 2 | 2 | 2 | 1 |
| 31–60 | 7×8 (17), 8×8 (13) | 3-3, 2-2 | 3–4 | 2–3 | 4,8 | 4–6 | 6–7 | %4,3 / %17,5 | 0/26 | %55 | 4 | 3 | 5 | 0 |
| 61–100 | 8×8 (21), 8×9 (18), 7×9 (1) | 2-2, 3-3 | 3–5 | 2–3 | 5,0 | 2–6 | 6 | %3,8 / %17,5 | 0/35 | %57 | 5 | 4 | 7 | 1 |
| 101–200 | 8×9 (82), 8×8 (10), 7×9 (8) | 2-2, 3-3 | 3–5 | 2–3 | 5,6 | 0–5 | 6–7 | %0,6 / %3,5 | 0/86 | %40 | 14 | 10 | 16 | 0 |
| 201–300 | 8×9 (79), 8×8 (12), 7×9 (9) | 2-2, 3-3 | 4–5 | 2–3 | 5,9 | 0–5 | 6–7 | %0,5 / %5,0 | 0/87 | %42 | 13 | 10 | 17 | 0 |
| 301–500 | 8×9 (163), 7×9 (19), 8×8 (18) | 3-3, 2-2 | 4–5 | 2–3 | 7,0 | 0–4 | 6 | %0,1 / %2,5 | 0/173 | %27 | 27 | 20 | 33 | 0 |
| 501–700 | 8×9 (162), 8×8 (20), 7×9 (18) | 3-3, 2-2 | 4–5 | 2–4 | 6,8 | 0–4 | 6 | %0,1 / %1,0 | 0/174 | %24 | 26 | 20 | 34 | 0 |
| 701–800 | 8×9 (83), 8×8 (9), 7×9 (8) | 3-3, 2-2 | 4–5 | 2–4 | 6,6 | 0–5 | 6 | %0,1 / %2,5 | 2/86 | %21 | 14 | 10 | 16 | 4 |
| 801–1000 | 8×9 (162), 7×9 (20), 8×8 (18) | 3-3, 2-2 | 4–5 | 2–4 | 7,1 | 0–4 | 6–7 | %0,1 / %3,0 | 0/174 | %22 | 26 | 20 | 34 | 1 |

## PLAN ölçütleri (sayım)

- Pay bandı dışında: 788 bölüm (kesin hamle türleri ve usta bölümleri: pay 1–99'da ≤ 1, sonra 0).
- Rastgele bot tavanı aşan: 9 bölüm (öğretici turlu 1–3 hariç).
- 31+ nefes dışı açgözlü botun kazandığı: 2 bölüm.
- Yedek aday (bir ölçüt tutmadı, en yakın aday): 9 bölüm.

## Örnek bölümler

| Bölüm | Tür | Tahta | Kat | Renk | Hedefler | Mekanik | Çözüm / sınır | Rastgele | Açgözlü | Şekil |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | vitrin | 7×8 | 2-2 | 3 | coral 12,blue 11 | refill | 4 / 8 | %33,0 | kazanır | dalga |
| 2 | normal | 7×7 | 2-2 | 2 | yellow 42,blue 38 | refill | 4+2t / 10 | %100,0 | kazanır | artı 7×7 |
| 3 | normal | 7×7 | 2-2 | 2 | blue 43,yellow 46 | refill | 4+2t / 10 | %99,5 | kazanır | yuvarlak 7×7 |
| 4 | normal | 7×7 | 2-2 | 2 | blue 50,yellow 53 | refill | 4+2t / 10 | %99,0 | kazanır | çanak 7×7 |
| 5 | normal | 8×8 | 2-2 | 2 | yellow 59,blue 61 | refill | 4+2t / 10 | %95,5 | kazanır | artı 8×8 |
| 6 | normal | 8×8 | 2-2 | 2 | blue 64,yellow 61 | refill | 4+2t / 10 | %100,0 | kazanır | yuvarlak 8×8 |
| 9 | normal | 8×8 | 2-2 | 3 | coral 37,yellow 23 | refill | 4+3t / 11 | %68,5 | kazanır | artı 8×8 |
| 15 | normal | 7×7 | 2-2 | 3 | yellow 39,blue 39 | refill | 4+3t / 10 | %41,0 | kazanır | dikdörtgen |
| 18 | nefes | 7×8 | 2-2 | 3 | blue 45,coral 37 | refill | 4+4t / 11 | %54,5 | kazanır | çatı |
| 21 | normal | 7×8 | 2-2 | 4 | purple 24,yellow 29 | clip,refill | 3+3t / 10 | %20,0 | kazanır | U (ayna) |
| 27 | tür:exact | 7×8 | 2-2 | 4 | blue 21,yellow 25 | clip,refill | 4+3t / 10 | %11,0 | kazanır | kesik köşe (ayna) |
| 40 | usta | 7×8 | 3-3 | 4 | blue 41,purple 37 | clip,refill | 5+5t / 10 | %4,5 | kaybeder | kesik köşe (ayna) |
| 41 | normal | 7×8 | 3-3 | 4 | coral 24,blue 20,star 3 | star,refill | 5+4t / 10 | %0,0 | kaybeder | dalga |
| 57 | tür:protect | 8×8 | 2-2 | 4 | yellow 32,coral 33,blue 27 | gravel,refill,crystal | 6+5t / 10 | %0,0 | kaybeder | kenar çentikleri |
| 66 | nefes | 8×8 | 2-2 | 3 | yellow 42,coral 41 | refill | 4+3t / 10 | %39,5 | kaybeder | L |
| 100 | usta | 8×8 | 3-3 | 3 | yellow 27,navy 28,coral 29 | clip,ice,refill,crystal | 11 / 15 | %0,0 | kaybeder | çatı (ayna) |
| 121 | normal | 8×9 | 2-2 | 5 | coral 32,navy 30,yellow 24 | ice,refill | 5+5t / 10 | %0,0 | kaybeder | U |
| 127 | tür:order | 8×9 | 2-2 | 5 | coral 42,yellow 29,star 2 | star,colorLock,refill | 6+6t / 10 | %0,0 | kaybeder | dalga |
| 201 | normal | 8×9 | 3-3 | 5 | navy 18,yellow 13,envelope 3 | clip,envelope,refill | 5+4t / 10 | %0,0 | kaybeder | elmas (ayna) |
| 246 | nefes | 8×9 | 2-2 | 4 | blue 43,yellow 35 | refill | 4+4t / 10 | %5,5 | kaybeder | zikzak |
| 301 | normal | 8×9 | 3-3 | 5 | blue 40,yellow 36,coral 32 | ice,refill | 6+6t / 10 | %0,0 | kaybeder | merdiven (ayna) |
| 401 | normal | 8×9 | 3-3 | 5 | navy 32,blue 30,sponge 1 | sponge,gravel,refill | 11+6t / 13 | %0,0 | kaybeder | L (ayna) |
| 500 | usta | 8×9 | 3-3 | 5 | blue 32,purple 30,sponge 1 | sponge,refill,crystal | 6+5t / 10 | %0,0 | kaybeder | dikdörtgen |
| 501 | normal | 8×9 | 3-3 | 5 | yellow 36,navy 28,coral 29 | colorLock,linkedClip,refill | 6+5t / 10 | %0,0 | kaybeder | kenar çentikleri |
| 601 | normal | 8×9 | 3-3 | 5 | purple 26,navy 24,blue 27 | rotor,gate,refill | 7+5t / 10 | %0,0 | kaybeder | artı |
| 701 | normal | 8×9 | 3-3 | 5 | purple 25,blue 30,coral 30,star 3 | star,paint,refill | 7+6t / 10 | %0,0 | kaybeder | artı |
| 801 | normal | 8×9 | 3-3 | 5 | yellow 38,purple 27,blue 40 | colorLock,refill | 6+6t / 10 | %0,0 | kaybeder | dalga |
| 900 | usta | 8×9 | 3-3 | 4 | blue 28,navy 39,purple 39 | gate,refill,crystal | 25 / 29 | %0,0 | kaybeder | U |
| 999 | normal | 8×9 | 3-3 | 5 | coral 34,navy 23,purple 25,sponge 2 | sponge,paint,refill | 7+6t / 10 | %0,0 | kaybeder | artı |
| 1000 | usta | 8×9 | 3-3 | 4 | blue 21,purple 23,coral 31,envelope 2 | envelope,refill,crystal | 19 / 23 | %0,0 | kaybeder | elmas (ayna) |

## Bayraklı bölümler

- 1 (vitrin): YEDEK: vitrin1 özel taş;
- 11 (normal): YEDEK: rastgele;
- 15 (normal): YEDEK: rastgele;
- 33 (normal): clip1 çıkarıldı; clip2 çıkarıldı;
- 99 (normal): YEDEK: sert;
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
- 392 (normal): döndürmeyle başlar;
- 393 (normal): döndürmeyle başlar;
- 394 (normal): döndürmeyle başlar;
- 395 (normal): döndürmeyle başlar;
- 398 (tür:protect): döndürmeyle başlar;
- 435 (normal): döndürmeyle başlar;
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
- 735 (normal): YEDEK: kısa;
- 740 (usta): döndürmeyle başlar;
- 746 (normal): döndürmeyle başlar;
- 751 (normal): döndürmeyle başlar;
- 761 (normal): döndürmeyle başlar;
- 763 (normal): YEDEK: kısa;
- 773 (normal): döndürmeyle başlar;
- 784 (normal): YEDEK: açgözlü;
- 785 (normal): YEDEK: kısa;
- 788 (tür:scarce): döndürmeyle başlar;
- 803 (normal): döndürmeyle başlar;
- 805 (normal): döndürmeyle başlar;
- 821 (normal): döndürmeyle başlar;
- 836 (normal): döndürmeyle başlar;
- 844 (normal): döndürmeyle başlar;
- 851 (normal): döndürmeyle başlar;
- 855 (normal): YEDEK: kısa;
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
