# v8 adillik onarımı (LevelLab fairfix, 2026-11-08)

İki tur. Her tür bölümü iki bağımsız tohum dizisiyle 112 bot oyunuyla ölçüldü; altındaysa hamle sınırı birer arttı (tur başına en çok +4).

- Tur 1 tabanı: patron ≥ 3, bölge 1 ≥ 9, diğer ≥ 6 galibiyet. Sınırı artan 82, tavanda kalan 8.
- Tur 2 tabanı: tanıtım ≥ 14 (%12,5), patron ≥ 5, bölge 1 ≥ 9 (%8), diğer ≥ 6 (%5). Sınırı artan 55; tavanda da adil olmayan **17 bölüm v7 haline döndü**.
- Ek: level_646 (v7 normal bölümü; denetimde bot 88 oyunda kazanamadı) sınır 16 → 17 (112 oyunda 6 galibiyet).

## Tur 2 ayrıntısı
| Bölüm | Tür | Hamle sınırı | Galibiyet |
|---|---|---|---|
| 47 | chain | 8 → 10 | 18/112 |
| 57 | protect | 14 → 18 (tavan) | 12/112 — taban 14 altında: **v7 bölümüne döndü** |
| 57 | protect | 14 → 18 | 12/112 |
| 67 | scarce | 7 → 8 | 25/112 |
| 87 | resource | 8 → 9 | 26/112 |
| 90 | boss | 15 → 19 | 5/112 |
| 118 | chain | 12 → 16 (tavan) | 4/112 — taban 6 altında: **v7 bölümüne döndü** |
| 118 | chain | 12 → 16 | 4/112 |
| 120 | boss | 17 → 21 (tavan) | 3/112 — taban 5 altında: **v7 bölümüne döndü** |
| 120 | boss | 17 → 21 | 3/112 |
| 150 | boss | 11 → 12 | 12/112 |
| 250 | boss | 13 → 17 (tavan) | 1/112 — taban 5 altında: **v7 bölümüne döndü** |
| 250 | boss | 13 → 17 | 1/112 |
| 258 | protect | 15 → 19 (tavan) | 2/112 — taban 6 altında: **v7 bölümüne döndü** |
| 258 | protect | 15 → 19 | 2/112 |
| 270 | boss | 12 → 13 | 5/112 |
| 300 | boss | 9 → 10 | 10/112 |
| 320 | boss | 13 → 14 | 9/112 |
| 350 | boss | 8 → 9 | 9/112 |
| 360 | boss | 13 → 16 | 5/112 |
| 370 | boss | 13 → 14 | 6/112 |
| 380 | boss | 9 → 10 | 10/112 |
| 390 | boss | 15 → 16 | 5/112 |
| 410 | boss | 8 → 9 | 8/112 |
| 418 | protect | 18 → 22 (tavan) | 2/112 — taban 6 altında: **v7 bölümüne döndü** |
| 418 | protect | 18 → 22 | 2/112 |
| 420 | boss | 25 → 29 (tavan) | 3/112 — taban 5 altında: **v7 bölümüne döndü** |
| 420 | boss | 25 → 29 | 3/112 |
| 480 | boss | 18 → 22 (tavan) | 3/112 — taban 5 altında: **v7 bölümüne döndü** |
| 480 | boss | 18 → 22 | 3/112 |
| 490 | boss | 14 → 17 | 6/112 |
| 497 | chain | 20 → 24 (tavan) | 0/112 — taban 6 altında: **v7 bölümüne döndü** |
| 497 | chain | 20 → 24 | 0/112 |
| 500 | boss | 9 → 10 | 17/112 |
| 510 | boss | 11 → 12 | 9/112 |
| 530 | boss | 12 → 13 | 5/112 |
| 560 | boss | 12 → 13 | 5/112 |
| 587 | scarce | 23 → 27 (tavan) | 3/112 — taban 6 altında: **v7 bölümüne döndü** |
| 587 | scarce | 23 → 27 | 3/112 |
| 600 | boss | 14 → 15 | 7/112 |
| 610 | boss | 15 → 16 | 7/112 |
| 630 | boss | 11 → 12 | 7/112 |
| 650 | boss | 13 → 14 | 5/112 |
| 670 | boss | 12 → 13 | 7/112 |
| 680 | boss | 12 → 13 | 13/112 |
| 690 | boss | 12 → 13 | 7/112 |
| 700 | boss | 15 → 16 | 9/112 |
| 740 | boss | 18 → 19 | 7/112 |
| 780 | boss | 15 → 17 | 7/112 |
| 790 | boss | 15 → 16 | 6/112 |
| 800 | boss | 11 → 15 | 5/112 |
| 810 | boss | 14 → 15 | 6/112 |
| 850 | boss | 16 → 20 (tavan) | 4/112 — taban 5 altında: **v7 bölümüne döndü** |
| 850 | boss | 16 → 20 | 4/112 |
| 860 | boss | 20 → 21 | 12/112 |
| 880 | boss | 14 → 18 (tavan) | 3/112 — taban 5 altında: **v7 bölümüne döndü** |
| 880 | boss | 14 → 18 | 3/112 |
| 900 | boss | 12 → 13 | 5/112 |
| 918 | order | 15 → 19 (tavan) | 3/112 — taban 6 altında: **v7 bölümüne döndü** |
| 918 | order | 15 → 19 | 3/112 |
| 920 | boss | 13 → 15 | 7/112 |
| 930 | boss | 12 → 16 (tavan) | 3/112 — taban 5 altında: **v7 bölümüne döndü** |
| 930 | boss | 12 → 16 | 3/112 |
| 970 | boss | 10 → 12 | 5/112 |
| 977 | chain | 20 → 24 (tavan) | 0/112 — taban 6 altında: **v7 bölümüne döndü** |
| 977 | chain | 20 → 24 | 0/112 |
| 978 | protect | 21 → 25 (tavan) | 1/112 — taban 6 altında: **v7 bölümüne döndü** |
| 978 | protect | 21 → 25 | 1/112 |
| 980 | boss | 14 → 15 | 11/112 |
| 990 | boss | 12 → 13 | 8/112 |
| 1000 | boss | 16 → 20 (tavan) | 1/112 — taban 5 altında: **v7 bölümüne döndü** |
| 1000 | boss | 16 → 20 | 1/112 |

## Tur 1 ayrıntısı
| Bölüm | Tür | Hamle sınırı | Galibiyet |
|---|---|---|---|
| 90 | boss | 14 → 15 | 3/112 |
| 118 | chain | 8 → 12 (tavan) | 0/112 — taban 6 altında |
| 118 | chain | 8 → 12 | 0/112 |
| 120 | boss | 13 → 17 | 3/112 |
| 138 | protect | 8 → 9 | 9/112 |
| 197 | resource | 8 → 9 | 9/112 |
| 208 | reverse | 6 → 7 | 13/112 |
| 237 | resource | 7 → 9 | 12/112 |
| 250 | boss | 11 → 13 | 3/112 |
| 257 | exact | 7 → 8 | 17/112 |
| 258 | protect | 11 → 15 (tavan) | 2/112 — taban 6 altında |
| 258 | protect | 11 → 15 | 2/112 |
| 278 | order | 8 → 9 | 21/112 |
| 320 | boss | 12 → 13 | 3/112 |
| 327 | exact | 8 → 9 | 8/112 |
| 330 | boss | 9 → 11 | 10/112 |
| 337 | chain | 12 → 13 | 12/112 |
| 338 | protect | 10 → 14 | 8/112 |
| 357 | resource | 13 → 14 | 6/112 |
| 367 | exact | 10 → 11 | 13/112 |
| 370 | boss | 10 → 13 | 3/112 |
| 378 | protect | 14 → 17 | 6/112 |
| 380 | boss | 8 → 9 | 4/112 |
| 387 | scarce | 11 → 13 | 7/112 |
| 388 | pure | 17 → 18 | 11/112 |
| 390 | boss | 14 → 15 | 3/112 |
| 407 | exact | 9 → 10 | 6/112 |
| 408 | reverse | 6 → 7 | 18/112 |
| 418 | protect | 14 → 18 (tavan) | 2/112 — taban 6 altında |
| 418 | protect | 14 → 18 | 2/112 |
| 420 | boss | 23 → 25 | 3/112 |
| 447 | exact | 6 → 7 | 20/112 |
| 480 | boss | 14 → 18 | 3/112 |
| 487 | exact | 11 → 12 | 10/112 |
| 488 | reverse | 7 → 8 | 10/112 |
| 497 | chain | 16 → 20 (tavan) | 2/112 — taban 6 altında |
| 497 | chain | 16 → 20 | 2/112 |
| 507 | scarce | 10 → 11 | 12/112 |
| 527 | exact | 11 → 12 | 9/112 |
| 528 | reverse | 7 → 8 | 25/112 |
| 537 | chain | 11 → 12 | 8/112 |
| 547 | scarce | 10 → 11 | 7/112 |
| 560 | boss | 11 → 12 | 3/112 |
| 578 | protect | 9 → 11 | 9/112 |
| 587 | scarce | 19 → 23 (tavan) | 3/112 — taban 6 altında |
| 587 | scarce | 19 → 23 | 3/112 |
| 588 | exact | 10 → 11 | 7/112 |
| 600 | boss | 13 → 14 | 4/112 |
| 638 | order | 13 → 14 | 7/112 |
| 647 | exact | 10 → 11 | 10/112 |
| 658 | protect | 10 → 12 | 11/112 |
| 678 | order | 7 → 8 | 9/112 |
| 680 | boss | 11 → 12 | 4/112 |
| 687 | exact | 12 → 13 | 9/112 |
| 698 | protect | 12 → 13 | 6/112 |
| 700 | boss | 14 → 15 | 3/112 |
| 717 | resource | 12 → 13 | 9/112 |
| 757 | resource | 10 → 11 | 6/112 |
| 760 | boss | 13 → 14 | 5/112 |
| 807 | exact | 8 → 9 | 13/112 |
| 827 | scarce | 12 → 13 | 8/112 |
| 828 | exact | 13 → 14 | 7/112 |
| 840 | boss | 13 → 16 | 8/112 |
| 847 | exact | 11 → 12 | 16/112 |
| 848 | reverse | 14 → 15 | 9/112 |
| 857 | chain | 9 → 11 | 13/112 |
| 858 | protect | 16 → 19 | 6/112 |
| 867 | scarce | 11 → 14 | 9/112 |
| 868 | pure | 21 → 22 | 7/112 |
| 877 | resource | 13 → 14 | 10/112 |
| 890 | boss | 10 → 11 | 6/112 |
| 897 | chain | 11 → 12 | 11/112 |
| 918 | order | 11 → 15 (tavan) | 1/112 — taban 6 altında |
| 918 | order | 11 → 15 | 1/112 |
| 928 | reverse | 12 → 13 | 9/112 |
| 937 | chain | 12 → 13 | 12/112 |
| 938 | protect | 11 → 12 | 6/112 |
| 947 | scarce | 15 → 16 | 8/112 |
| 948 | exact | 10 → 11 | 11/112 |
| 950 | boss | 12 → 15 | 9/112 |
| 960 | boss | 14 → 15 | 7/112 |
| 967 | exact | 9 → 10 | 12/112 |
| 970 | boss | 9 → 10 | 3/112 |
| 977 | chain | 16 → 20 (tavan) | 1/112 — taban 6 altında |
| 977 | chain | 16 → 20 | 1/112 |
| 978 | protect | 17 → 21 (tavan) | 1/112 — taban 6 altında |
| 978 | protect | 17 → 21 | 1/112 |
| 980 | boss | 13 → 14 | 4/112 |
| 990 | boss | 11 → 12 | 4/112 |
| 1000 | boss | 13 → 16 | 3/112 |
