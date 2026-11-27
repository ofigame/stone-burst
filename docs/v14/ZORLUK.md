# v14 zorluk tablosu (LevelLab `v14`, 2026-11-27 15:02)

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
| 1–5 | 7×7 (3), 7×8 (1), 8×8 (1) | 2-2 | 2–3 | 2 | 5,0 | 4–6 | — | %62,4 / %85,5 | 5/5 | %95 | 0 | 0 | 0 | 2 |
| 6–7 | 8×8 (2) | 2-2 | 3 | 2 | 5,0 | 5 | — | %15,3 / %15,5 | 1/2 | %73 | 0 | 0 | 0 | 0 |
| 8–14 | 7×7 (3), 8×8 (2), 8×9 (1), 7×8 (1) | 2-2 | 3 | 1–2 | 4,7 | 4–7 | 7 | %14,1 / %25,0 | 3/6 | %61 | 1 | 0 | 0 | 2 |
| 15–30 | 7×8 (11), 7×7 (5) | 2-2 | 3–4 | 2 | 5,0 | 3–6 | 7 | %8,1 / %26,5 | 7/14 | %47 | 2 | 2 | 2 | 2 |
| 31–60 | 7×8 (17), 8×8 (13) | 2-2, 3-3 | 3–4 | 2–3 | 5,9 | 4–5 | 6 | %1,0 / %7,5 | 0/26 | %24 | 4 | 3 | 5 | 0 |
| 61–100 | 8×8 (21), 8×9 (18), 7×9 (1) | 2-2, 3-3 | 3–5 | 2–3 | 6,6 | 1–6 | 6 | %0,4 / %3,0 | 0/35 | %21 | 5 | 4 | 7 | 1 |
| 101–200 | 8×9 (82), 8×8 (10), 7×9 (8) | 2-2, 3-3 | 3–5 | 2–3 | 6,9 | 2–5 | 6–7 | %0,1 / %2,0 | 0/86 | %13 | 14 | 10 | 16 | 0 |
| 201–300 | 8×9 (79), 8×8 (12), 7×9 (9) | 2-2, 3-3 | 4–5 | 2–3 | 7,3 | 0–5 | 6–7 | %0,1 / %3,5 | 0/87 | %12 | 13 | 10 | 17 | 0 |
| 301–500 | 8×9 (163), 7×9 (19), 8×8 (18) | 3-3, 2-2 | 4–5 | 2–3 | 8,3 | 0–4 | 5–8 | %0,0 / %0,5 | 0/173 | %10 | 27 | 20 | 33 | 1 |
| 501–700 | 8×9 (162), 8×8 (20), 7×9 (18) | 3-3, 2-2 | 4–5 | 2–4 | 8,4 | 0–4 | 5–8 | %0,1 / %2,0 | 0/174 | %9 | 26 | 20 | 34 | 0 |
| 701–800 | 8×9 (83), 8×8 (9), 7×9 (8) | 3-3, 2-2 | 4–5 | 2–4 | 8,3 | 0–4 | 5–8 | %0,0 / %0,5 | 0/86 | %10 | 14 | 10 | 16 | 0 |
| 801–1000 | 8×9 (162), 7×9 (20), 8×8 (18) | 3-3, 2-2 | 4–5 | 2–4 | 8,6 | 0–4 | 5–8 | %0,0 / %1,0 | 0/174 | %9 | 26 | 20 | 34 | 1 |

## PLAN ölçütleri (sayım)

- Pay bandı dışında: 544 bölüm (kesin hamle türleri ve usta bölümleri: pay 1–99'da ≤ 1, sonra 0).
- Rastgele bot tavanı aşan: 2 bölüm (öğretici turlu 1–3 hariç).
- 31+ nefes dışı açgözlü botun kazandığı: 0 bölüm.
- Yedek aday (bir ölçüt tutmadı, en yakın aday): 9 bölüm.

## Örnek bölümler

| Bölüm | Tür | Tahta | Kat | Renk | Hedefler | Mekanik | Çözüm / sınır | Rastgele | Açgözlü | Şekil |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | vitrin | 7×8 | 2-2 | 3 | coral 12,blue 11 | refill | 4 / 10 | %68,5 | kazanır | dalga |
| 2 | normal | 7×7 | 2-2 | 2 | blue 35,yellow 41 | refill | 5+2t / 10 | %69,0 | kazanır | artı 7×7 |
| 3 | normal | 7×7 | 2-2 | 2 | yellow 47,blue 41 | refill | 5+2t / 10 | %69,5 | kazanır | yuvarlak 7×7 |
| 4 | normal | 7×7 | 2-2 | 2 | blue 47,yellow 46 | refill | 6+1t / 10 | %85,5 | kazanır | çanak 7×7 |
| 5 | normal | 8×8 | 2-2 | 3 | blue 41,yellow 34 | refill | 5+3t / 10 | %19,5 | kazanır | artı 8×8 |
| 6 | normal | 8×8 | 2-2 | 3 | coral 35,yellow 35 | refill | 5+3t / 10 | %15,5 | kaybeder | yuvarlak 8×8 |
| 9 | normal | 8×8 | 2-2 | 3 | coral 33,yellow 26 | refill | 5+3t / 10 | %18,0 | kazanır | artı 8×8 |
| 15 | normal | 7×7 | 2-2 | 3 | coral 31,blue 32 | refill | 4+3t / 10 | %18,5 | kazanır | zikzak |
| 18 | nefes | 7×8 | 2-2 | 3 | yellow 42,coral 35 | refill | 5+4t / 12 | %17,0 | kazanır | kum saati |
| 21 | normal | 7×8 | 2-2 | 4 | purple 27,yellow 29 | clip,refill | 5+3t / 10 | %0,5 | kaybeder | U (ayna) |
| 27 | tür:exact | 7×8 | 2-2 | 4 | blue 18,yellow 22 | clip,refill | 5+3t / 10 | %3,0 | kaybeder | kesik köşe (ayna) |
| 40 | usta | 7×8 | 3-3 | 4 | blue 27,purple 20 | clip,refill | 6+4t / 10 | %1,0 | kaybeder | kesik köşe (ayna) |
| 41 | normal | 7×8 | 3-3 | 4 | coral 16,blue 15,star 3 | star,refill | 8+4t / 13 | %0,0 | kaybeder | dalga |
| 57 | tür:protect | 8×8 | 3-3 | 4 | yellow 33,purple 37,coral 27 | refill,crystal | 6+5t / 10 | %0,5 | kaybeder | çatı (ayna) |
| 66 | nefes | 8×8 | 2-2 | 3 | yellow 39,coral 39 | refill | 4+3t / 10 | %9,5 | kazanır | L |
| 100 | usta | 8×8 | 3-3 | 3 | yellow 27,navy 29,coral 28 | clip,ice,refill,crystal | 16 / 20 | %0,0 | kaybeder | çatı (ayna) |
| 121 | normal | 8×9 | 2-2 | 5 | coral 25,navy 28,yellow 17 | ice,refill | 6+4t / 10 | %0,0 | kaybeder | U |
| 127 | tür:order | 8×9 | 2-2 | 5 | coral 24,yellow 17,star 2 | star,colorLock,refill | 6+3t / 10 | %0,0 | kaybeder | dalga |
| 201 | normal | 8×9 | 3-3 | 5 | navy 19,yellow 14,envelope 3 | clip,envelope,refill | 8+4t / 11 | %0,5 | kaybeder | elmas (ayna) |
| 246 | nefes | 8×9 | 2-2 | 4 | blue 36,yellow 30 | refill | 5+3t / 11 | %6,5 | kaybeder | zikzak |
| 301 | normal | 8×9 | 3-3 | 5 | blue 28,yellow 25,coral 28 | ice,refill | 8+5t / 10 | %0,0 | kaybeder | merdiven (ayna) |
| 401 | normal | 8×9 | 3-3 | 5 | navy 31,blue 31,sponge 1 | sponge,refill | 8+6t / 10 | %0,0 | kaybeder | L (ayna) |
| 500 | usta | 8×9 | 3-3 | 5 | blue 35,purple 33,sponge 1 | sponge,refill,crystal | 8+5t / 12 | %0,0 | kaybeder | dikdörtgen |
| 501 | normal | 8×9 | 3-3 | 5 | yellow 32,navy 25,coral 25 | colorLock,linkedClip,refill | 7+5t / 10 | %0,0 | kaybeder | kenar çentikleri |
| 601 | normal | 8×9 | 3-3 | 5 | purple 27,blue 32,navy 27 | rotor,gate,refill | 9+5t / 11 | %0,0 | kaybeder | kum saati (ayna) |
| 701 | normal | 8×9 | 3-3 | 5 | purple 21,blue 26,coral 25,star 3 | star,paint,refill | 7+5t / 10 | %0,0 | kaybeder | artı |
| 801 | normal | 8×9 | 3-3 | 5 | yellow 34,purple 24,blue 33 | colorLock,refill | 8+6t / 10 | %0,0 | kaybeder | dalga |
| 900 | usta | 8×9 | 3-3 | 4 | blue 31,coral 28,yellow 33 | gate,refill,crystal | 18 / 22 | %0,5 | kaybeder | çatı (ayna) |
| 999 | normal | 8×9 | 3-3 | 5 | coral 33,navy 25,purple 24,sponge 2 | sponge,paint,refill | 8+6t / 10 | %0,0 | kaybeder | artı |
| 1000 | usta | 8×9 | 3-3 | 4 | blue 19,purple 21,coral 24,envelope 3 | envelope,gravel,refill,crystal | 22+1t / 26 | %0,0 | kaybeder | elmas (ayna) |

## Bayraklı bölümler

- 1 (vitrin): YEDEK: vitrin1 özel taş;
- 4 (normal): YEDEK: rastgele;
- 11 (normal): YEDEK: kolay;
- 13 (normal): YEDEK: kolay;
- 18 (nefes): YEDEK: kolay;
- 20 (usta): YEDEK: rastgele;
- 86 (normal): YEDEK: sert;
- 302 (normal): döndürmeyle başlar;
- 303 (normal): döndürmeyle başlar;
- 304 (normal): döndürmeyle başlar;
- 305 (normal): döndürmeyle başlar;
- 310 (usta): döndürmeyle başlar;
- 314 (normal): döndürmeyle başlar;
- 315 (normal): döndürmeyle başlar;
- 316 (normal): döndürmeyle başlar;
- 319 (normal): döndürmeyle başlar;
- 322 (normal): döndürmeyle başlar;
- 323 (normal): döndürmeyle başlar;
- 327 (tür:reverse): döndürmeyle başlar;
- 328 (tür:protect): döndürmeyle başlar;
- 329 (normal): döndürmeyle başlar;
- 331 (normal): döndürmeyle başlar;
- 332 (normal): döndürmeyle başlar;
- 335 (normal): döndürmeyle başlar;
- 337 (tür:scarce): döndürmeyle başlar;
- 339 (normal): döndürmeyle başlar;
- 341 (normal): döndürmeyle başlar;
- 344 (normal): döndürmeyle başlar;
- 345 (normal): döndürmeyle başlar;
- 346 (normal): döndürmeyle başlar;
- 347 (tür:resource): döndürmeyle başlar;
- 352 (normal): döndürmeyle başlar;
- 353 (normal): döndürmeyle başlar;
- 355 (normal): döndürmeyle başlar;
- 359 (normal): döndürmeyle başlar;
- 362 (normal): döndürmeyle başlar;
- 363 (normal): döndürmeyle başlar;
- 364 (normal): döndürmeyle başlar;
- 368 (tür:scarce): döndürmeyle başlar;
- 371 (normal): döndürmeyle başlar;
- 373 (normal): döndürmeyle başlar;
- 375 (normal): döndürmeyle başlar;
- 376 (normal): döndürmeyle başlar;
- 379 (normal): döndürmeyle başlar;
- 380 (usta): döndürmeyle başlar;
- 386 (normal): döndürmeyle başlar;
- 390 (usta): döndürmeyle başlar;
- 391 (normal): döndürmeyle başlar;
- 392 (normal): döndürmeyle başlar;
- 393 (normal): döndürmeyle başlar;
- 394 (normal): döndürmeyle başlar;
- 400 (usta): döndürmeyle başlar;
- 435 (normal): döndürmeyle başlar;
- 441 (normal): döndürmeyle başlar;
- 445 (normal): döndürmeyle başlar;
- 455 (normal): döndürmeyle başlar;
- 461 (normal): döndürmeyle başlar;
- 471 (normal): döndürmeyle başlar;
- 473 (normal): döndürmeyle başlar;
- 476 (normal): döndürmeyle başlar; rotor çıkarıldı; YEDEK: mek:rotor;
- 488 (tür:order): döndürmeyle başlar;
- 493 (normal): döndürmeyle başlar;
- 494 (normal): döndürmeyle başlar;
- 514 (normal): döndürmeyle başlar;
- 531 (normal): döndürmeyle başlar;
- 536 (normal): döndürmeyle başlar;
- 545 (normal): döndürmeyle başlar;
- 569 (normal): döndürmeyle başlar;
- 581 (normal): döndürmeyle başlar;
- 601 (normal): döndürmeyle başlar;
- 626 (normal): döndürmeyle başlar;
- 627 (tür:resource): döndürmeyle başlar;
- 631 (normal): döndürmeyle başlar;
- 635 (normal): döndürmeyle başlar;
- 638 (tür:reverse): döndürmeyle başlar;
- 643 (normal): döndürmeyle başlar;
- 645 (normal): gate çıkarıldı;
- 673 (normal): döndürmeyle başlar;
- 693 (normal): döndürmeyle başlar;
- 694 (normal): döndürmeyle başlar;
- 698 (tür:order): döndürmeyle başlar;
- 703 (normal): döndürmeyle başlar;
- 739 (normal): döndürmeyle başlar;
- 740 (usta): döndürmeyle başlar;
- 746 (normal): döndürmeyle başlar;
- 751 (normal): döndürmeyle başlar;
- 761 (normal): döndürmeyle başlar;
- 773 (normal): döndürmeyle başlar;
- 788 (tür:scarce): döndürmeyle başlar;
- 803 (normal): döndürmeyle başlar;
- 805 (normal): döndürmeyle başlar;
- 844 (normal): döndürmeyle başlar;
- 851 (normal): döndürmeyle başlar;
- 866 (normal): döndürmeyle başlar;
- 896 (normal): döndürmeyle başlar;
- 904 (normal): döndürmeyle başlar;
- 909 (normal): döndürmeyle başlar;
- 910 (usta): YEDEK: mek:rotor;
- 911 (normal): döndürmeyle başlar;
- 925 (normal): döndürmeyle başlar;
- 943 (normal): döndürmeyle başlar;
- 953 (normal): döndürmeyle başlar;
- 969 (normal): döndürmeyle başlar;
- 971 (normal): döndürmeyle başlar;
- 980 (usta): döndürmeyle başlar;
- 986 (normal): döndürmeyle başlar;
- 997 (tür:protect): gate çıkarıldı;
