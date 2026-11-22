# v14 zorluk tablosu (LevelLab `v14`, 2026-11-22 05:12)

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
| 1–5 | 7×7 (3), 7×8 (1), 8×8 (1) | 2-2 | 2–3 | 2 | 4,2 | 4–8 | — | %58,0 / %74,5 | 5/5 | %90 | 0 | 0 | 0 | 1 |
| 6–7 | 8×8 (2) | 2-2 | 2 | 2 | 4,0 | 8 | — | %56,3 / %59,5 | 2/2 | %94 | 0 | 0 | 0 | 0 |
| 8–14 | 7×7 (3), 8×8 (2), 8×9 (1), 7×8 (1) | 2-2 | 3 | 1–2 | 3,9 | 3–8 | 9 | %30,1 / %58,0 | 4/6 | %73 | 1 | 0 | 0 | 1 |
| 15–30 | 7×8 (11), 7×7 (5) | 2-2 | 3–4 | 2 | 4,8 | 1–6 | 9 | %37,0 / %94,5 | 9/14 | %58 | 2 | 2 | 2 | 6 |
| 31–60 | 7×8 (17), 8×8 (13) | 3-3, 2-2 | 3–4 | 2–3 | 6,0 | 1–4 | 7 | %3,7 / %23,0 | 2/26 | %22 | 4 | 3 | 5 | 2 |
| 61–100 | 8×8 (21), 8×9 (18), 7×9 (1) | 2-2, 3-3 | 3–5 | 2–3 | 6,3 | 1–5 | 7 | %1,0 / %6,0 | 3/35 | %24 | 5 | 4 | 7 | 3 |
| 101–200 | 8×9 (82), 8×8 (10), 7×9 (8) | 2-2, 3-3 | 3–5 | 2–3 | 7,0 | 0–5 | 7 | %0,7 / %8,5 | 3/86 | %19 | 14 | 10 | 16 | 7 |
| 201–300 | 8×9 (79), 8×8 (12), 7×9 (9) | 2-2, 3-3 | 4–5 | 2–3 | 7,1 | 0–5 | 7 | %0,7 / %10,0 | 7/87 | %20 | 13 | 10 | 17 | 13 |
| 301–500 | 8×9 (163), 7×9 (19), 8×8 (18) | 3-3, 2-2 | 4–5 | 2–3 | 8,4 | 0–4 | 6 | %0,5 / %15,5 | 9/173 | %19 | 27 | 20 | 33 | 30 |
| 501–700 | 8×9 (162), 8×8 (20), 7×9 (18) | 3-3, 2-2 | 4–5 | 2–4 | 8,0 | 0–4 | 6 | %0,1 / %2,0 | 4/174 | %15 | 26 | 20 | 34 | 14 |
| 701–800 | 8×9 (83), 8×8 (9), 7×9 (8) | 3-3, 2-2 | 4–5 | 2–4 | 8,0 | 0–4 | 6 | %0,2 / %3,0 | 5/86 | %15 | 14 | 10 | 16 | 21 |
| 801–1000 | 8×9 (162), 7×9 (20), 8×8 (18) | 3-3, 2-2 | 3–5 | 2–4 | 8,2 | 0–4 | 6 | %0,1 / %2,0 | 1/174 | %14 | 26 | 20 | 34 | 8 |

## PLAN ölçütleri (sayım)

- Pay bandı dışında: 107 bölüm (kesin hamle türleri ve usta bölümleri: pay 1–99'da ≤ 1, sonra 0).
- Rastgele bot tavanı aşan: 12 bölüm (öğretici turlu 1–3 hariç).
- 31+ nefes dışı açgözlü botun kazandığı: 34 bölüm.
- Yedek aday (bir ölçüt tutmadı, en yakın aday): 106 bölüm.

## Örnek bölümler

| Bölüm | Tür | Tahta | Kat | Renk | Hedefler | Mekanik | Çözüm / sınır | Rastgele | Açgözlü | Şekil |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | vitrin | 7×8 | 2-2 | 3 | coral 10,blue 8 | - | 5 / 9 | %45,0 | kazanır | dalga |
| 2 | normal | 7×7 | 2-2 | 2 | blue 21,yellow 26 | - | 4+2t / 12 | %58,0 | kazanır | artı 7×7 |
| 3 | normal | 7×7 | 2-2 | 2 | blue 36,yellow 34 | - | 4+2t / 12 | %48,5 | kazanır | yuvarlak 7×7 |
| 4 | normal | 7×7 | 2-2 | 2 | yellow 26,blue 33 | - | 4+2t / 12 | %74,5 | kazanır | çanak 7×7 |
| 5 | normal | 8×8 | 2-2 | 2 | blue 44,yellow 40 | - | 4+3t / 12 | %64,0 | kazanır | artı 8×8 |
| 6 | normal | 8×8 | 2-2 | 2 | yellow 47,blue 40 | - | 4+3t / 12 | %59,5 | kazanır | yuvarlak 8×8 |
| 9 | normal | 8×8 | 2-2 | 3 | blue 24,coral 20 | - | 4+2t / 9 | %49,0 | kazanır | artı 8×8 |
| 15 | normal | 7×7 | 2-2 | 3 | blue 21,yellow 27 | - | 4+1t / 8 | %94,5 | kazanır | T |
| 18 | nefes | 7×8 | 2-2 | 3 | yellow 30,coral 29 | - | 3+2t / 12 | %89,5 | kazanır | T |
| 21 | normal | 7×8 | 2-2 | 4 | coral 20,purple 21 | clip | 5+2t / 10 | %34,5 | kazanır | kesik köşe |
| 27 | tür:exact | 7×8 | 2-2 | 4 | blue 19,yellow 22 | clip | 5+4t / 6 | %0,5 | kaybeder | T (ayna) |
| 40 | usta | 7×8 | 3-3 | 4 | yellow 42,purple 19 | clip | 6+5t / 10 | %5,5 | kaybeder | zikzak |
| 41 | normal | 7×8 | 3-3 | 4 | coral 45,blue 46,star 3 | star | 6+6t / 8 | %0,0 | kaybeder | dikdörtgen |
| 57 | tür:protect | 8×8 | 2-2 | 4 | purple 18,blue 16,yellow 25 | gravel,crystal | 7+6t / 9 | %0,0 | kaybeder | kenar çentikleri |
| 66 | nefes | 8×8 | 2-2 | 3 | yellow 37,blue 33 | - | 3+2t / 10 | %65,5 | kazanır | T |
| 100 | usta | 8×8 | 3-3 | 3 | purple 24,navy 24,coral 30 | clip,ice,crystal | 8 / 12 | %5,0 | kaybeder | dalga |
| 121 | normal | 8×9 | 2-2 | 5 | coral 18,yellow 24,purple 22 | ice | 7+5t / 10 | %0,0 | kaybeder | dalga |
| 127 | tür:order | 8×9 | 2-2 | 5 | purple 14,navy 28,star 2 | star,colorLock | 7+6t / 9 | %0,0 | kaybeder | zikzak |
| 201 | normal | 8×9 | 3-3 | 5 | navy 19,yellow 11,envelope 3 | clip,envelope | 7+4t / 9 | %0,0 | kaybeder | elmas (ayna) |
| 246 | nefes | 8×9 | 2-2 | 4 | blue 27,yellow 23 | - | 3+2t / 10 | %76,5 | kazanır | zikzak |
| 301 | normal | 8×9 | 3-3 | 5 | blue 49,yellow 38,coral 34 | ice | 10+10t / 11 | %0,0 | kaybeder | merdiven (ayna) |
| 401 | normal | 8×9 | 3-3 | 5 | navy 20,purple 24,sponge 1 | sponge,gravel | 10+8t / 12 | %1,0 | kaybeder | L (ayna) |
| 500 | usta | 8×9 | 3-3 | 5 | coral 20,yellow 26,sponge 1 | sponge,crystal | 8+5t / 12 | %0,0 | kaybeder | kesik köşe (ayna) |
| 501 | normal | 8×9 | 3-3 | 5 | yellow 39,navy 29,coral 28 | colorLock,linkedClip | 8+5t / 10 | %0,0 | kaybeder | kenar çentikleri |
| 601 | normal | 8×9 | 3-3 | 5 | coral 19,navy 55,yellow 59 | rotor,gate | 12+7t / 14 | %0,0 | kaybeder | dalga |
| 701 | normal | 8×9 | 3-3 | 5 | purple 23,blue 29,coral 26,star 3 | star,paint | 8+7t / 9 | %0,0 | kaybeder | artı |
| 801 | normal | 8×9 | 3-3 | 5 | yellow 50,purple 23,blue 40 | colorLock | 9+6t / 11 | %0,5 | kaybeder | dalga |
| 900 | usta | 8×9 | 3-3 | 4 | blue 22,navy 37,purple 35 | gate,crystal | 12 / 15 | %0,0 | kaybeder | U |
| 999 | normal | 8×9 | 3-3 | 5 | coral 33,navy 23,purple 24,sponge 2 | sponge,paint | 11+6t / 12 | %0,0 | kaybeder | artı |
| 1000 | usta | 8×9 | 3-3 | 4 | blue 24,purple 15,yellow 21,envelope 2 | envelope,crystal | 9 / 13 | %0,0 | kaybeder | T |

## Bayraklı bölümler

- 3 (normal): YEDEK: zor;
- 9 (normal): YEDEK: zor;
- 15 (normal): YEDEK: kısa;
- 16 (normal): YEDEK: rastgele;
- 17 (normal): YEDEK: rastgele;
- 19 (normal): YEDEK: rastgele;
- 20 (usta): YEDEK: rastgele;
- 30 (usta): YEDEK: kısa;
- 32 (normal): YEDEK: kısa;
- 38 (tür:reverse): YEDEK: açgözlü;
- 94 (normal): YEDEK: açgözlü;
- 97 (tür:exact): YEDEK: açgözlü;
- 98 (tür:reverse): YEDEK: açgözlü;
- 119 (normal): YEDEK: kısa;
- 134 (normal): YEDEK: açgözlü;
- 136 (normal): YEDEK: açgözlü;
- 154 (normal): YEDEK: mek:örtü;
- 177 (tür:order): YEDEK: kısa;
- 182 (normal): YEDEK: kısa;
- 197 (tür:scarce): YEDEK: açgözlü;
- 202 (normal): YEDEK: mek:örtü;
- 219 (normal): YEDEK: kısa;
- 229 (normal): YEDEK: kısa;
- 237 (tür:pure): YEDEK: mek:örtü;
- 238 (tür:resource): YEDEK: kısa;
- 241 (normal): YEDEK: açgözlü;
- 254 (normal): YEDEK: açgözlü;
- 257 (tür:reverse): YEDEK: kısa;
- 269 (normal): YEDEK: kısa;
- 279 (normal): YEDEK: açgözlü;
- 284 (normal): YEDEK: mek:örtü;
- 285 (normal): YEDEK: açgözlü;
- 296 (normal): YEDEK: mek:örtü;
- 303 (normal): döndürmeyle başlar;
- 304 (normal): döndürmeyle başlar;
- 305 (normal): döndürmeyle başlar;
- 307 (tür:pure): döndürmeyle başlar;
- 309 (normal): döndürmeyle başlar;
- 310 (usta): döndürmeyle başlar;
- 313 (normal): döndürmeyle başlar;
- 316 (normal): döndürmeyle başlar;
- 317 (tür:order): YEDEK: kısa;
- 319 (normal): döndürmeyle başlar;
- 320 (usta): döndürmeyle başlar;
- 321 (normal): döndürmeyle başlar;
- 322 (normal): döndürmeyle başlar;
- 323 (normal): döndürmeyle başlar;
- 325 (normal): döndürmeyle başlar;
- 328 (tür:protect): döndürmeyle başlar;
- 329 (normal): döndürmeyle başlar;
- 330 (usta): döndürmeyle başlar;
- 331 (normal): döndürmeyle başlar;
- 334 (normal): döndürmeyle başlar; YEDEK: mek:rotor;
- 337 (tür:scarce): döndürmeyle başlar;
- 339 (normal): döndürmeyle başlar; YEDEK: mek:rotor;
- 340 (usta): döndürmeyle başlar;
- 343 (normal): döndürmeyle başlar;
- 345 (normal): döndürmeyle başlar; YEDEK: mek:rotor;
- 346 (normal): döndürmeyle başlar;
- 350 (usta): döndürmeyle başlar;
- 351 (normal): YEDEK: kısa;
- 352 (normal): döndürmeyle başlar;
- 353 (normal): döndürmeyle başlar;
- 355 (normal): döndürmeyle başlar;
- 357 (tür:exact): döndürmeyle başlar;
- 359 (normal): döndürmeyle başlar; YEDEK: mek:rotor;
- 360 (usta): döndürmeyle başlar;
- 361 (normal): döndürmeyle başlar;
- 362 (normal): döndürmeyle başlar;
- 363 (normal): döndürmeyle başlar; YEDEK: mek:rotor;
- 364 (normal): döndürmeyle başlar;
- 370 (usta): YEDEK: rastgele;
- 375 (normal): döndürmeyle başlar;
- 376 (normal): döndürmeyle başlar;
- 377 (tür:pure): döndürmeyle başlar;
- 379 (normal): döndürmeyle başlar;
- 385 (normal): YEDEK: mek:rotor;
- 388 (tür:exact): döndürmeyle başlar; YEDEK: mek:rotor;
- 389 (normal): döndürmeyle başlar;
- 390 (usta): döndürmeyle başlar;
- 395 (normal): döndürmeyle başlar;
- 398 (tür:protect): döndürmeyle başlar;
- 399 (normal): YEDEK: açgözlü;
- 400 (usta): döndürmeyle başlar; YEDEK: mek:rotor;
- 405 (normal): YEDEK: rastgele;
- 409 (normal): YEDEK: kısa;
- 411 (normal): YEDEK: kısa;
- 412 (normal): YEDEK: açgözlü;
- 419 (normal): döndürmeyle başlar;
- 421 (normal): YEDEK: mek:örtü;
- 423 (normal): YEDEK: mek:örtü;
- 431 (normal): YEDEK: kısa;
- 436 (normal): YEDEK: kısa;
- 437 (tür:protect): döndürmeyle başlar;
- 443 (normal): YEDEK: kısa;
- 446 (normal): YEDEK: kısa;
- 455 (normal): döndürmeyle başlar;
- 457 (tür:order): YEDEK: rastgele;
- 461 (normal): YEDEK: mek:rotor;
- 466 (normal): YEDEK: rastgele;
- 469 (normal): YEDEK: mek:örtü;
- 473 (normal): döndürmeyle başlar; rotor çıkarıldı; YEDEK: mek:rotor;
- 481 (normal): YEDEK: kısa;
- 483 (normal): YEDEK: kısa;
- 485 (normal): YEDEK: kısa;
- 488 (tür:order): döndürmeyle başlar;
- 490 (usta): döndürmeyle başlar;
- 493 (normal): döndürmeyle başlar;
- 505 (normal): döndürmeyle başlar;
- 514 (normal): döndürmeyle başlar;
- 527 (tür:order): YEDEK: mek:örtü;
- 531 (normal): döndürmeyle başlar;
- 535 (normal): YEDEK: açgözlü;
- 536 (normal): döndürmeyle başlar;
- 544 (normal): YEDEK: kısa;
- 545 (normal): döndürmeyle başlar;
- 554 (normal): döndürmeyle başlar;
- 557 (tür:resource): YEDEK: kısa;
- 559 (normal): YEDEK: açgözlü;
- 569 (normal): döndürmeyle başlar;
- 572 (normal): YEDEK: kısa;
- 581 (normal): döndürmeyle başlar;
- 597 (tür:order): YEDEK: açgözlü;
- 601 (normal): döndürmeyle başlar;
- 611 (normal): YEDEK: kısa;
- 614 (normal): döndürmeyle başlar;
- 620 (usta): döndürmeyle başlar;
- 625 (normal): YEDEK: kısa;
- 627 (tür:resource): döndürmeyle başlar;
- 628 (tür:order): YEDEK: mek:kilit;
- 633 (normal): YEDEK: mek:örtü;
- 638 (tür:reverse): döndürmeyle başlar;
- 643 (normal): döndürmeyle başlar;
- 644 (normal): YEDEK: kısa;
- 649 (normal): döndürmeyle başlar;
- 664 (normal): YEDEK: kısa;
- 674 (normal): döndürmeyle başlar;
- 693 (normal): döndürmeyle başlar; YEDEK: mek:rotor;
- 694 (normal): döndürmeyle başlar;
- 698 (tür:order): döndürmeyle başlar;
- 703 (normal): döndürmeyle başlar;
- 712 (normal): YEDEK: kısa;
- 716 (normal): döndürmeyle başlar;
- 717 (tür:protect): YEDEK: kısa;
- 728 (tür:resource): YEDEK: kısa;
- 729 (normal): YEDEK: kısa;
- 734 (normal): YEDEK: kısa;
- 735 (normal): YEDEK: kısa;
- 739 (normal): döndürmeyle başlar;
- 740 (usta): döndürmeyle başlar;
- 742 (normal): YEDEK: mek:gate;
- 746 (normal): döndürmeyle başlar; rotor çıkarıldı; YEDEK: mek:rotor;
- 747 (tür:reverse): YEDEK: kısa;
- 749 (normal): YEDEK: kısa;
- 754 (normal): YEDEK: açgözlü;
- 763 (normal): YEDEK: kısa;
- 765 (normal): YEDEK: kısa;
- 766 (normal): YEDEK: kısa;
- 769 (normal): YEDEK: mek:örtü;
- 772 (normal): YEDEK: kısa;
- 779 (normal): YEDEK: kısa;
- 781 (normal): YEDEK: kısa;
- 783 (normal): YEDEK: kısa;
- 784 (normal): YEDEK: kısa;
- 785 (normal): YEDEK: kısa;
- 788 (tür:scarce): döndürmeyle başlar;
- 805 (normal): döndürmeyle başlar;
- 820 (usta): gate çıkarıldı;
- 821 (normal): döndürmeyle başlar;
- 830 (usta): YEDEK: kısa;
- 833 (normal): YEDEK: kısa;
- 836 (normal): döndürmeyle başlar;
- 837 (tür:resource): YEDEK: kısa;
- 844 (normal): döndürmeyle başlar;
- 851 (normal): döndürmeyle başlar;
- 866 (normal): döndürmeyle başlar;
- 875 (normal): YEDEK: kısa;
- 892 (normal): YEDEK: kısa;
- 895 (normal): döndürmeyle başlar;
- 896 (normal): döndürmeyle başlar;
- 904 (normal): döndürmeyle başlar;
- 909 (normal): döndürmeyle başlar;
- 910 (usta): döndürmeyle başlar;
- 913 (normal): YEDEK: kısa;
- 925 (normal): döndürmeyle başlar;
- 929 (normal): YEDEK: kısa;
- 940 (usta): döndürmeyle başlar;
- 941 (normal): döndürmeyle başlar;
- 971 (normal): döndürmeyle başlar;
- 983 (normal): YEDEK: kısa;
- 986 (normal): döndürmeyle başlar;
- 987 (tür:exact): döndürmeyle başlar;
