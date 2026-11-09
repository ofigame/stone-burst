# v8 bölüm üretimi (LevelLab v8, 2026-11-08 07:24)

Kaynak: v7 (`levels_src`). Normal bölümler aynen; tür bölümleri dönüştürülüp ışın çözücüyle (genişlik 60) yeniden çözüldü. Bot: yumuşak açgözlü, 12 oyun, son hamle sınırında.

## Sayım
- normal: 686
- boss: 78
- yedek: 31
- exact: 27
- protect: 26
- reverse: 24
- scarce: 24
- resource: 23
- order: 23
- aha: 20
- chain: 19
- pure: 17
- hata: 2

## Tür bölümleri
| Bölüm | Tür [kurallar] | Ölçüm | Bot |
|---|---|---|---|
| 27 | exact [exact] · **tanıtım** | çözüm 6 / sınır 8 (v7 10); açgözlü kazanır; bot48 0,23 (taban 0,10) | 0,17 |
| 28 | exact [exact] | çözüm 5 / sınır 6 (v7 9); açgözlü kaybeder; bot48 0,21 (taban 0,10) | 0,25 |
| 30 | boss [exact] | çözüm 4 / sınır 5 (v7 8); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,08 |
| 37 | reverse [exact] · **tanıtım** | çözüm 5 / sınır 7 (v7 9); açgözlü kaybeder; bot48 0,21 (taban 0,10) | 0,33 |
| 38 | exact [exact] | çözüm 5 / sınır 6 (v7 8); açgözlü kazanır; bot48 0,27 (taban 0,10) | 0,33 |
| 40 | boss [exact] | çözüm 6 / sınır 7 (v7 11); açgözlü kaybeder; bot48 0,15 (taban 0,04) | 0,25 |
| 47 | chain [chainOnly] · **tanıtım** | zincir coral 6; çözüm 5 / sınır 8 (v7 11); açgözlü kaybeder; bot48 0,10 (taban 0,10) | 0,00 |
| 48 | exact [exact] | çözüm 4 / sınır 5 (v7 11); açgözlü kaybeder; bot48 0,15 (taban 0,10) | 0,17 |
| 49 | aha [exact] · **tanıtım** | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 1412 | 1,00 |
| 50 | boss [chainOnly+exact] | zincir blue 6; çözüm 6 / sınır 7 (v7 12); açgözlü kaybeder; bot48 0,60 (taban 0,04) | 0,75 |
| 57 | protect [protect] · **tanıtım** | kristal 2; çözüm 9 / sınır 14 (v7 13); açgözlü kaybeder; bot48 0,10 (taban 0,10) | 0,00 |
| 58 | exact [exact] | çözüm 4 / sınır 6 (v7 9); açgözlü kaybeder; bot48 0,13 (taban 0,10) | 0,17 |
| 60 | boss [protect+exact] | kristal 2; çözüm 7 / sınır 8 (v7 10); açgözlü kaybeder; bot48 0,15 (taban 0,04) | 0,08 |
| 67 | scarce [scarce] · **tanıtım** | kıt renk coral 19/16; çözüm 4 / sınır 7 (v7 9); açgözlü kaybeder; bot48 0,13 (taban 0,10) | 0,17 |
| 68 | protect [protect] | kristal 2; çözüm 4 / sınır 7 (v7 7); açgözlü kazanır; bot48 0,65 (taban 0,10) | 0,33 |
| 70 | boss [chainOnly+exact] | zincir yellow 6; çözüm 4 / sınır 6 (v7 17); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 77 | pure [noSpecials] · **tanıtım** | çözüm 11 / sınır 14 (v7 13); açgözlü kaybeder; bot48 0,25 (taban 0,10) | 0,17 |
| 78 | protect [protect] | kristal 2; çözüm 7 / sınır 11 (v7 12); açgözlü kaybeder; bot48 0,10 (taban 0,10) | 0,08 |
| 80 | boss [protect+exact] | kristal 2; çözüm 8 / sınır 10 (v7 10); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 87 | resource [exact] · **tanıtım** | çözüm 5 / sınır 8 (v7 9); açgözlü kaybeder; bot48 0,13 (taban 0,10) | 0,25 |
| 88 | protect [protect] | kristal 2; çözüm 5 / sınır 8 (v7 9); açgözlü kaybeder; bot48 0,33 (taban 0,10) | 0,42 |
| 90 | boss [chainOnly+exact] | zincir yellow 6; çözüm 7 / sınır 14 (v7 12); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 97 | scarce [scarce] | kıt renk yellow 21/18; çözüm 5 / sınır 8 (v7 11); açgözlü kaybeder; bot48 0,21 (taban 0,10) | 0,17 |
| 98 | pure [noSpecials] | çözüm 12 / sınır 15 (v7 9); açgözlü kazanır; bot48 0,10 (taban 0,10) | 0,42 |
| 99 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 602 | 1,00 |
| 100 | boss [exact] (yedek; istenen boss [noSpecials+scarce+exact]) | çözüm 9 / sınır 9 (v7 15); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 107 | resource [exact] | çözüm 7 / sınır 9 (v7 13); açgözlü kaybeder; bot48 0,25 (taban 0,06) | 0,00 |
| 108 | exact [exact] | çözüm 10 / sınır 11 (v7 18); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,00 |
| 110 | boss [scarce+exact] | kıt renk yellow 40/38; çözüm 11 / sınır 11 (v7 10); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 117 | reverse [exact] | çözüm 10 / sınır 10 (v7 14); açgözlü kaybeder; bot48 0,23 (taban 0,06) | 0,08 |
| 118 | chain [chainOnly] | zincir yellow 8; çözüm 5 / sınır 8 (v7 12); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 120 | boss [protect+exact] | kristal 2; çözüm 12 / sınır 13 (v7 14); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 127 | order · **tanıtım** | kilit purple→blue; çözüm 5 / sınır 7 (v7 8); açgözlü kazanır; bot48 0,44 (taban 0,06) | 0,33 |
| 128 | reverse [exact] | çözüm 7 / sınır 8 (v7 9); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,08 |
| 130 | boss [chainOnly+exact] | zincir yellow 6; çözüm 4 / sınır 8 (v7 15); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 137 | exact [exact] (yedek; istenen chain [chainOnly]) | çözüm 9 / sınır 10 (v7 15); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 138 | protect [protect] | kristal 2; çözüm 6 / sınır 8 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 140 | boss [exact] (yedek; istenen boss [noSpecials+exact]) | çözüm 5 / sınır 6 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,42 |
| 147 | scarce [scarce] | kıt renk coral 33/31; çözüm 8 / sınır 12 (v7 14); açgözlü kaybeder; bot48 0,19 (taban 0,06) | 0,08 |
| 148 | pure [noSpecials] | çözüm 15 / sınır 18 (v7 16); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,17 |
| 149 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/6; deneme 517 | 1,00 |
| 150 | boss [scarce+exact] | kıt renk yellow 24/22; çözüm 8 / sınır 11 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 157 | resource [exact] | çözüm 7 / sınır 9 (v7 10); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,42 |
| 158 | order | kilit purple→blue; çözüm 6 / sınır 11 (v7 12); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,00 |
| 160 | boss [exact] (yedek; istenen boss [protect+exact]) | çözüm 6 / sınır 8 (v7 11); açgözlü kazanır; bot48 0,13 (taban 0,04) | 0,25 |
| 167 | exact [exact] | çözüm 7 / sınır 7 (v7 10); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,08 |
| 168 | reverse [exact] | çözüm 12 / sınır 12 (v7 16); açgözlü kaybeder; bot48 0,17 (taban 0,06) | 0,00 |
| 170 | boss [exact] (yedek; istenen boss [chainOnly+exact]) | çözüm 8 / sınır 9 (v7 14); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 177 | chain [chainOnly] | zincir yellow 3; çözüm 6 / sınır 8 (v7 11); açgözlü kaybeder; bot48 0,25 (taban 0,06) | 0,25 |
| 178 | protect [protect] | kristal 2; çözüm 4 / sınır 6 (v7 9); açgözlü kaybeder; bot48 0,31 (taban 0,06) | 0,42 |
| 180 | boss [exact] (yedek; istenen boss [noSpecials+exact]) | çözüm 5 / sınır 6 (v7 9); açgözlü kaybeder; bot48 0,17 (taban 0,04) | 0,08 |
| 187 | scarce [scarce] | kıt renk coral 34/32; çözüm 9 / sınır 11 (v7 8); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,33 |
| 188 | pure [noSpecials] | çözüm 18 / sınır 22 (v7 13); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 190 | boss [scarce+exact] | kıt renk coral 33/31; çözüm 6 / sınır 10 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,08 |
| 197 | resource [exact] | çözüm 6 / sınır 8 (v7 16); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 198 | order | kilit yellow→blue; çözüm 7 / sınır 9 (v7 10); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,25 |
| 199 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 313 | 1,00 |
| 200 | boss [exact] (yedek; istenen boss [protect+chainOnly+exact]) | çözüm 6 / sınır 9 (v7 11); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,08 |
| 207 | exact [exact] | çözüm 6 / sınır 8 (v7 10); açgözlü kaybeder; bot48 0,17 (taban 0,06) | 0,17 |
| 208 | reverse [exact] | çözüm 5 / sınır 6 (v7 8); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 210 | boss [exact] (yedek; istenen boss [chainOnly+exact]) | çözüm 8 / sınır 11 (v7 11); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 217 | chain [chainOnly] | zincir purple 3; çözüm 7 / sınır 9 (v7 9); açgözlü kaybeder; bot48 0,27 (taban 0,06) | 0,17 |
| 218 | protect [protect] | kristal 3; çözüm 4 / sınır 7 (v7 11); açgözlü kaybeder; bot48 0,17 (taban 0,06) | 0,00 |
| 220 | boss [exact] (yedek; istenen boss [noSpecials+exact]) | çözüm 6 / sınır 7 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 227 | scarce [scarce] | kıt renk blue 45/43; çözüm 6 / sınır 8 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 228 | exact [exact] (yedek; istenen pure [noSpecials+exact]) | çözüm 5 / sınır 7 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 230 | boss [scarce+exact] | kıt renk blue 54/52; çözüm 11 / sınır 11 (v7 9); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,17 |
| 237 | resource [exact] | çözüm 7 / sınır 7 (v7 15); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 238 | order | kilit blue→yellow; çözüm 9 / sınır 11 (v7 13); açgözlü kaybeder; bot48 0,21 (taban 0,06) | 0,17 |
| 240 | boss [protect+exact] | kristal 3; çözüm 13 / sınır 15 (v7 14); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 247 | exact [exact] | çözüm 7 / sınır 8 (v7 10); açgözlü kaybeder; bot48 0,25 (taban 0,06) | 0,17 |
| 248 | reverse [exact] | çözüm 5 / sınır 7 (v7 9); açgözlü kaybeder; bot48 0,38 (taban 0,06) | 0,25 |
| 249 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 627 | 1,00 |
| 250 | boss [chainOnly+exact] | zincir blue 8; çözüm 6 / sınır 11 (v7 9); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 257 | exact [exact] (yedek; istenen chain [chainOnly]) | çözüm 6 / sınır 7 (v7 10); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 258 | protect [protect] | kristal 3; çözüm 7 / sınır 11 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 260 | boss [exact] (yedek; istenen boss [noSpecials+exact]) | çözüm 7 / sınır 8 (v7 13); açgözlü kaybeder; bot48 0,15 (taban 0,04) | 0,00 |
| 267 | scarce [scarce] | kıt renk purple 24/22; çözüm 7 / sınır 9 (v7 10); açgözlü kaybeder; bot48 0,33 (taban 0,06) | 0,42 |
| 268 | pure [noSpecials+exact] | çözüm 17 / sınır 20 (v7 10); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 270 | boss [scarce+exact] | kıt renk yellow 89/87; çözüm 10 / sınır 12 (v7 15); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,17 |
| 277 | resource [exact] | çözüm 7 / sınır 8 (v7 13); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 278 | order | kilit yellow→coral; çözüm 6 / sınır 8 (v7 10); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,00 |
| 280 | boss [exact] (yedek; istenen boss [protect+exact]) | çözüm 7 / sınır 10 (v7 11); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 287 | exact [exact] | çözüm 11 / sınır 11 (v7 17); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 288 | reverse [exact] | çözüm 10 / sınır 11 (v7 13); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 290 | boss [chainOnly+exact] | zincir coral 3; çözüm 13 / sınır 13 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,25 |
| 297 | chain [chainOnly] | zincir purple 6; çözüm 6 / sınır 8 (v7 10); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 298 | protect [protect] | kristal 3; çözüm 11 / sınır 16 (v7 15); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 299 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 362 | 1,00 |
| 300 | boss [exact] (yedek; istenen boss [noSpecials+scarce+exact]) | çözüm 8 / sınır 9 (v7 12); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 307 | scarce [scarce] | kıt renk purple 41/40; çözüm 5 / sınır 9 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,25 |
| 308 | pure [noSpecials+exact] | çözüm 10 / sınır 14 (v7 9); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 310 | boss [scarce+exact] | kıt renk coral 41/40; çözüm 7 / sınır 8 (v7 9); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 317 | resource [exact] | çözüm 7 / sınır 9 (v7 13); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 318 | order [exact] | kilit coral→blue; çözüm 6 / sınır 7 (v7 10); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,17 |
| 320 | boss [protect+exact] | kristal 3; çözüm 11 / sınır 12 (v7 20); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 327 | exact [exact] | çözüm 6 / sınır 8 (v7 12); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 328 | reverse [exact] | çözüm 6 / sınır 7 (v7 9); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,08 |
| 330 | boss [chainOnly+exact] | zincir yellow 8; çözüm 9 / sınır 9 (v7 18); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 337 | chain [chainOnly+exact] | zincir blue 4; çözüm 8 / sınır 12 (v7 16); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 338 | protect [protect+exact] | kristal 3; çözüm 7 / sınır 10 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 340 | boss [noSpecials+exact] | çözüm 18 / sınır 24 (v7 15); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,08 |
| 347 | scarce [scarce] | kıt renk purple 44/43; çözüm 7 / sınır 14 (v7 20); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 348 | pure [noSpecials+exact] | çözüm 18 / sınır 22 (v7 17); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 349 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 158 | 1,00 |
| 350 | boss [scarce+exact] | kıt renk blue 40/39; çözüm 6 / sınır 8 (v7 10); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,08 |
| 357 | resource [exact] | çözüm 9 / sınır 13 (v7 12); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 358 | order [exact] | kilit coral→blue; çözüm 8 / sınır 12 (v7 19); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 360 | boss [protect+exact] | kristal 3; çözüm 10 / sınır 13 (v7 12); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 367 | exact [exact] | çözüm 6 / sınır 10 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 368 | reverse [exact] | çözüm 10 / sınır 13 (v7 12); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,00 |
| 370 | boss [chainOnly+exact] | zincir coral 10; çözüm 7 / sınır 10 (v7 7); açgözlü kazanır; bot48 0,04 (taban 0,04) | 0,00 |
| 377 | chain [chainOnly+exact] | zincir purple 10; çözüm 7 / sınır 11 (v7 14); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,00 |
| 378 | protect [protect+exact] | kristal 3; çözüm 9 / sınır 14 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 380 | boss [exact] (yedek; istenen boss [noSpecials+exact]) | çözüm 8 / sınır 8 (v7 14); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 387 | scarce [scarce] | kıt renk blue 63/62; çözüm 10 / sınır 11 (v7 13); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 388 | pure [noSpecials+exact] | çözüm 13 / sınır 17 (v7 8); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 390 | boss [scarce+exact] | kıt renk yellow 38/37; çözüm 11 / sınır 14 (v7 9); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 397 | resource [exact] | çözüm 10 / sınır 13 (v7 14); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,25 |
| 398 | order [exact] | kilit blue→coral; çözüm 6 / sınır 11 (v7 14); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,17 |
| 399 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 243 | 1,00 |
| 400 | boss [exact] (yedek; istenen boss [protect+chainOnly+exact]) | çözüm 10 / sınır 10 (v7 13); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 407 | exact [exact] | çözüm 8 / sınır 9 (v7 12); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 408 | reverse [exact] | çözüm 6 / sınır 6 (v7 8); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,08 |
| 410 | boss [chainOnly+exact] | zincir blue 10; çözüm 7 / sınır 8 (v7 12); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,00 |
| 417 | chain [chainOnly+exact] | zincir blue 4; çözüm 5 / sınır 10 (v7 8); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 418 | protect [protect+exact] | kristal 3; çözüm 13 / sınır 14 (v7 12); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 420 | boss [noSpecials+exact] | çözüm 18 / sınır 23 (v7 11); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 427 | scarce [scarce] | kıt renk coral 37/36; çözüm 8 / sınır 9 (v7 12); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 428 | pure [noSpecials+exact] | çözüm 17 / sınır 21 (v7 10); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,00 |
| 430 | boss [scarce+exact] | kıt renk coral 53/52; çözüm 11 / sınır 12 (v7 12); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 437 | resource [exact] | çözüm 7 / sınır 10 (v7 11); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,25 |
| 438 | order [exact] | kilit purple→blue; çözüm 8 / sınır 10 (v7 11); açgözlü kaybeder; bot48 0,17 (taban 0,06) | 0,00 |
| 440 | boss [protect+exact] | kristal 3; çözüm 6 / sınır 9 (v7 9); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,00 |
| 447 | exact [exact] | çözüm 6 / sınır 6 (v7 8); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 448 | reverse [exact] | çözüm 10 / sınır 12 (v7 16); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,17 |
| 449 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/6; deneme 816 | 1,00 |
| 450 | boss [chainOnly+exact] | zincir yellow 4; çözüm 16 / sınır 16 (v7 15); açgözlü kaybeder; bot48 0,10 (taban 0,04) | 0,00 |
| 457 | chain [chainOnly+exact] | zincir purple 8; çözüm 9 / sınır 12 (v7 13); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 458 | protect [protect+exact] | kristal 3; çözüm 7 / sınır 7 (v7 13); açgözlü kazanır; bot48 0,15 (taban 0,06) | 0,33 |
| 460 | boss [exact] (yedek; istenen boss [noSpecials+exact]) | çözüm 7 / sınır 8 (v7 10); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,08 |
| 467 | scarce [scarce] | kıt renk blue 56/55; çözüm 10 / sınır 11 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,25 |
| 468 | pure [noSpecials+exact] | HATA: dönüşüm bulunamadı: pure [noSpecials+exact] (v7 korundu) | — |
| 470 | boss [scarce+exact] | kıt renk blue 53/52; çözüm 12 / sınır 14 (v7 15); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 477 | resource [exact] | çözüm 9 / sınır 12 (v7 12); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,17 |
| 478 | order [exact] | kilit purple→yellow; çözüm 9 / sınır 11 (v7 12); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,17 |
| 480 | boss [protect+exact] | kristal 3; çözüm 12 / sınır 14 (v7 11); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 487 | exact [exact] | çözüm 9 / sınır 11 (v7 13); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 488 | reverse [exact] | çözüm 6 / sınır 7 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 490 | boss [chainOnly+exact] | zincir yellow 8; çözüm 11 / sınır 14 (v7 14); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 497 | chain [chainOnly+exact] | zincir purple 10; çözüm 10 / sınır 16 (v7 16); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 498 | protect [protect+exact] | kristal 3; çözüm 9 / sınır 9 (v7 10); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 499 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 772 | 1,00 |
| 500 | boss [exact] (yedek; istenen boss [noSpecials+scarce+exact]) | çözüm 7 / sınır 9 (v7 10); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 507 | scarce [scarce] | kıt renk purple 41/40; çözüm 8 / sınır 10 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,17 |
| 508 | pure [noSpecials+exact] | çözüm 15 / sınır 20 (v7 17); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 510 | boss [scarce+exact] | kıt renk yellow 68/67; çözüm 10 / sınır 11 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 517 | resource [exact] | çözüm 6 / sınır 8 (v7 10); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 518 | order [exact] | kilit purple→coral; çözüm 9 / sınır 14 (v7 15); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 520 | boss [protect+exact] | kristal 3; çözüm 7 / sınır 14 (v7 15); açgözlü kaybeder; bot48 0,10 (taban 0,04) | 0,00 |
| 527 | exact [exact] | çözüm 10 / sınır 11 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 528 | reverse [exact] | çözüm 6 / sınır 7 (v7 8); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 530 | boss [chainOnly+exact] | zincir coral 4; çözüm 11 / sınır 12 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 537 | chain [chainOnly+exact] | zincir blue 8; çözüm 8 / sınır 11 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 538 | protect [protect+exact] | kristal 3; çözüm 10 / sınır 13 (v7 11); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 540 | boss [noSpecials+exact] | çözüm 21 / sınır 22 (v7 18); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,25 |
| 547 | scarce [scarce] | kıt renk coral 49/48; çözüm 9 / sınır 10 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 548 | pure [noSpecials+exact] | çözüm 20 / sınır 24 (v7 16); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,08 |
| 549 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 1271 | 1,00 |
| 550 | boss [scarce+exact] | kıt renk coral 48/47; çözüm 9 / sınır 13 (v7 14); açgözlü kaybeder; bot48 0,19 (taban 0,04) | 0,08 |
| 557 | resource [exact] | çözüm 5 / sınır 6 (v7 7); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 558 | order [exact] | kilit yellow→blue; çözüm 8 / sınır 12 (v7 12); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 560 | boss [protect+exact] | kristal 3; çözüm 8 / sınır 11 (v7 16); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 567 | exact [exact] | çözüm 8 / sınır 10 (v7 11); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,17 |
| 568 | reverse [exact] | çözüm 6 / sınır 9 (v7 9); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,25 |
| 570 | boss [chainOnly+exact] | zincir yellow 10; çözüm 7 / sınır 9 (v7 17); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,25 |
| 577 | exact [exact] (yedek; istenen chain [chainOnly+exact]) | çözüm 9 / sınır 11 (v7 11); açgözlü kaybeder; bot48 0,21 (taban 0,06) | 0,25 |
| 578 | protect [protect+exact] | kristal 3; çözüm 7 / sınır 9 (v7 15); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 580 | boss [noSpecials+exact] | çözüm 17 / sınır 20 (v7 11); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,17 |
| 587 | scarce [scarce] | kıt renk blue 58/57; çözüm 16 / sınır 19 (v7 18); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 588 | exact [exact] (yedek; istenen pure [noSpecials+exact]) | çözüm 7 / sınır 10 (v7 13); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 590 | boss [scarce+exact] | kıt renk yellow 62/61; çözüm 13 / sınır 13 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,08 |
| 597 | resource [exact] | çözüm 9 / sınır 12 (v7 15); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 598 | order [exact] | kilit blue→yellow; çözüm 6 / sınır 8 (v7 10); açgözlü kaybeder; bot48 0,17 (taban 0,06) | 0,17 |
| 599 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 308 | 1,00 |
| 600 | boss [protect+chainOnly+exact] | kristal 3; zincir purple 4; çözüm 8 / sınır 13 (v7 14); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,00 |
| 607 | exact [exact] | çözüm 6 / sınır 8 (v7 8); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,25 |
| 608 | reverse [exact] | çözüm 7 / sınır 8 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 610 | boss [chainOnly+exact] | zincir coral 3; çözüm 9 / sınır 15 (v7 13); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 617 | chain [chainOnly+exact] | zincir yellow 3; çözüm 7 / sınır 15 (v7 13); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 618 | protect [protect+exact] | kristal 3; çözüm 8 / sınır 12 (v7 12); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,08 |
| 620 | boss [noSpecials+exact] | çözüm 16 / sınır 19 (v7 12); açgözlü kaybeder; bot48 0,13 (taban 0,04) | 0,17 |
| 627 | scarce [scarce] | kıt renk purple 27/26; çözüm 7 / sınır 9 (v7 12); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 628 | pure [noSpecials+exact] | çözüm 11 / sınır 14 (v7 10); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 630 | boss [scarce+exact] | kıt renk yellow 35/34; çözüm 8 / sınır 11 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 637 | resource [exact] | çözüm 7 / sınır 10 (v7 15); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,00 |
| 638 | order [exact] | kilit yellow→coral; çözüm 11 / sınır 13 (v7 17); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 640 | boss [protect+exact] | kristal 3; çözüm 8 / sınır 11 (v7 20); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,08 |
| 647 | exact [exact] | çözüm 8 / sınır 10 (v7 16); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 648 | reverse [exact] | çözüm 11 / sınır 12 (v7 14); açgözlü kaybeder; bot48 0,23 (taban 0,06) | 0,00 |
| 649 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 848 | 1,00 |
| 650 | boss [chainOnly+exact] | zincir yellow 4; çözüm 13 / sınır 13 (v7 12); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,08 |
| 657 | exact [exact] (yedek; istenen chain [chainOnly+exact]) | çözüm 6 / sınır 10 (v7 9); açgözlü kaybeder; bot48 0,19 (taban 0,06) | 0,33 |
| 658 | protect [protect+exact] | kristal 3; çözüm 8 / sınır 10 (v7 10); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,00 |
| 660 | boss [exact] (yedek; istenen boss [noSpecials+exact]) | çözüm 14 / sınır 17 (v7 20); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,08 |
| 667 | scarce [scarce] | kıt renk coral 45/44; çözüm 10 / sınır 13 (v7 16); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 668 | pure [noSpecials+exact] | çözüm 13 / sınır 13 (v7 11); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,08 |
| 670 | boss [scarce+exact] | kıt renk blue 43/42; çözüm 10 / sınır 12 (v7 8); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 677 | resource [exact] | çözüm 10 / sınır 13 (v7 13); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,25 |
| 678 | order [exact] | kilit coral→blue; çözüm 6 / sınır 7 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 680 | boss [protect+exact] | kristal 3; çözüm 8 / sınır 11 (v7 21); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 687 | exact [exact] | çözüm 10 / sınır 12 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 688 | reverse [exact] | çözüm 8 / sınır 11 (v7 10); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 690 | boss [chainOnly+exact] | zincir yellow 10; çözüm 12 / sınır 12 (v7 12); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 697 | exact [exact] (yedek; istenen chain [chainOnly+exact]) | çözüm 9 / sınır 9 (v7 11); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,00 |
| 698 | protect [protect+exact] | kristal 3; çözüm 7 / sınır 12 (v7 15); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 699 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 557 | 1,00 |
| 700 | boss [noSpecials+scarce+exact] | kıt renk purple 66/65; çözüm 11 / sınır 14 (v7 9); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 707 | scarce [scarce] | kıt renk blue 52/51; çözüm 13 / sınır 14 (v7 11); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,25 |
| 708 | exact [exact] (yedek; istenen pure [noSpecials+exact]) | çözüm 8 / sınır 13 (v7 16); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,08 |
| 710 | boss [scarce+exact] | kıt renk blue 43/42; çözüm 7 / sınır 9 (v7 8); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,08 |
| 717 | resource [exact] | çözüm 9 / sınır 12 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 718 | order [exact] | kilit coral→yellow; çözüm 10 / sınır 13 (v7 18); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,08 |
| 720 | boss [protect+exact] | kristal 3; çözüm 12 / sınır 18 (v7 16); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 727 | exact [exact] | çözüm 13 / sınır 16 (v7 16); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,33 |
| 728 | reverse [exact] | çözüm 10 / sınır 11 (v7 12); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,17 |
| 730 | boss [chainOnly+exact] | zincir blue 7; çözüm 16 / sınır 16 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,08 |
| 737 | chain [chainOnly+exact] | zincir blue 10; çözüm 10 / sınır 12 (v7 14); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,08 |
| 738 | protect [protect+exact] | kristal 3; çözüm 10 / sınır 13 (v7 13); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 740 | boss [noSpecials+exact] | çözüm 15 / sınır 18 (v7 13); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 747 | scarce [scarce] | kıt renk purple 32/31; çözüm 12 / sınır 13 (v7 12); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,08 |
| 748 | pure [noSpecials+exact] | çözüm 14 / sınır 16 (v7 8); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,50 |
| 749 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 601 | 1,00 |
| 750 | boss [scarce+exact] | kıt renk yellow 47/46; çözüm 11 / sınır 14 (v7 11); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,00 |
| 757 | resource [exact] | çözüm 8 / sınır 10 (v7 12); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 758 | order [exact] | kilit blue→coral; çözüm 10 / sınır 11 (v7 13); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,17 |
| 760 | boss [protect+exact] | kristal 3; çözüm 10 / sınır 13 (v7 9); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,08 |
| 767 | exact [exact] | çözüm 6 / sınır 9 (v7 10); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 768 | reverse [exact] | çözüm 9 / sınır 13 (v7 12); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,17 |
| 770 | boss [chainOnly+exact] | zincir yellow 3; çözüm 8 / sınır 8 (v7 11); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,25 |
| 777 | chain [chainOnly+exact] | zincir yellow 10; çözüm 10 / sınır 13 (v7 15); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,00 |
| 778 | protect [protect+exact] | kristal 3; çözüm 6 / sınır 11 (v7 10); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,08 |
| 780 | boss [exact] (yedek; istenen boss [noSpecials+exact]) | çözüm 12 / sınır 15 (v7 18); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 787 | scarce [scarce] | kıt renk coral 64/63; çözüm 9 / sınır 11 (v7 12); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,00 |
| 788 | exact [exact] (yedek; istenen pure [noSpecials+exact]) | çözüm 7 / sınır 11 (v7 12); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 790 | boss [scarce+exact] | kıt renk yellow 49/48; çözüm 13 / sınır 15 (v7 14); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 797 | resource [exact] | çözüm 6 / sınır 8 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 798 | order [exact] | kilit purple→blue; çözüm 8 / sınır 10 (v7 13); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 799 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/6; deneme 59 | 1,00 |
| 800 | boss [protect+chainOnly+exact] | kristal 3; zincir yellow 3; çözüm 9 / sınır 11 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 807 | exact [exact] | çözüm 7 / sınır 8 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 808 | reverse [exact] | çözüm 8 / sınır 13 (v7 14); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,00 |
| 810 | boss [chainOnly+exact] | zincir blue 10; çözüm 11 / sınır 14 (v7 16); açgözlü kaybeder; bot48 0,10 (taban 0,04) | 0,00 |
| 817 | chain [chainOnly+exact] | zincir purple 7; çözüm 9 / sınır 11 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 818 | protect [protect+exact] | kristal 3; çözüm 8 / sınır 11 (v7 19); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 820 | boss [exact] (yedek; istenen boss [noSpecials+exact]) | çözüm 9 / sınır 13 (v7 17); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 827 | scarce [scarce] | kıt renk blue 67/66; çözüm 9 / sınır 12 (v7 15); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 828 | exact [exact] (yedek; istenen pure [noSpecials+exact]) | çözüm 10 / sınır 13 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 830 | boss [scarce+exact] | kıt renk coral 44/43; çözüm 10 / sınır 11 (v7 12); açgözlü kazanır; bot48 0,04 (taban 0,04) | 0,17 |
| 837 | resource [exact] | çözüm 7 / sınır 9 (v7 9); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,17 |
| 838 | order [exact] | kilit purple→yellow; çözüm 13 / sınır 13 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 840 | boss [protect+exact] | kristal 3; çözüm 11 / sınır 13 (v7 11); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 847 | exact [exact] | çözüm 8 / sınır 11 (v7 10); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 848 | reverse [exact] | çözüm 9 / sınır 14 (v7 19); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 849 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 1386 | 1,00 |
| 850 | boss [chainOnly+exact] | zincir yellow 7; çözüm 8 / sınır 16 (v7 15); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,00 |
| 857 | chain [chainOnly+exact] | zincir blue 8; çözüm 7 / sınır 9 (v7 11); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,08 |
| 858 | protect [protect+exact] | kristal 3; çözüm 10 / sınır 16 (v7 15); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 860 | boss [noSpecials+exact] | çözüm 17 / sınır 20 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 867 | scarce [scarce] | kıt renk purple 35/34; çözüm 7 / sınır 11 (v7 11); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,00 |
| 868 | pure [noSpecials+exact] | çözüm 20 / sınır 21 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 870 | boss [scarce+exact] | kıt renk blue 34/33; çözüm 8 / sınır 11 (v7 11); açgözlü kaybeder; bot48 0,25 (taban 0,04) | 0,17 |
| 877 | resource [exact] | çözüm 9 / sınır 13 (v7 17); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,00 |
| 878 | order [exact] | kilit purple→coral; çözüm 6 / sınır 9 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 880 | boss [protect+exact] | kristal 3; çözüm 11 / sınır 14 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 887 | exact [exact] | çözüm 8 / sınır 13 (v7 11); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,17 |
| 888 | reverse [exact] | çözüm 7 / sınır 11 (v7 10); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,08 |
| 890 | boss [chainOnly+exact] | zincir coral 10; çözüm 9 / sınır 10 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 897 | chain [chainOnly+exact] | zincir blue 10; çözüm 6 / sınır 11 (v7 9); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 898 | protect [protect+exact] | kristal 3; çözüm 9 / sınır 12 (v7 12); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,08 |
| 899 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 247 | 1,00 |
| 900 | boss [exact] (yedek; istenen boss [noSpecials+scarce+exact]) | çözüm 10 / sınır 12 (v7 14); açgözlü kaybeder; bot48 0,10 (taban 0,04) | 0,00 |
| 907 | scarce [scarce] | kıt renk blue 54/53; çözüm 6 / sınır 11 (v7 10); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,17 |
| 908 | pure [noSpecials+exact] | çözüm 20 / sınır 21 (v7 16); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,25 |
| 910 | boss [scarce+exact] | kıt renk yellow 71/70; çözüm 13 / sınır 15 (v7 12); açgözlü kaybeder; bot48 0,08 (taban 0,04) | 0,00 |
| 917 | resource [exact] | çözüm 8 / sınır 9 (v7 10); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,00 |
| 918 | order [exact] | kilit blue→yellow; çözüm 10 / sınır 11 (v7 16); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 920 | boss [protect+exact] | kristal 3; çözüm 11 / sınır 13 (v7 10); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
| 927 | exact [exact] | çözüm 8 / sınır 9 (v7 10); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,00 |
| 928 | reverse [exact] | çözüm 10 / sınır 12 (v7 14); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 930 | boss [chainOnly+exact] | zincir yellow 10; çözüm 6 / sınır 12 (v7 12); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 937 | chain [chainOnly+exact] | zincir blue 10; çözüm 8 / sınır 12 (v7 13); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 938 | protect [protect+exact] | kristal 3; çözüm 7 / sınır 11 (v7 17); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,08 |
| 940 | boss [noSpecials+exact] | çözüm 16 / sınır 19 (v7 10); açgözlü kaybeder; bot48 0,13 (taban 0,04) | 0,00 |
| 947 | scarce [scarce] | kıt renk blue 62/61; çözüm 10 / sınır 15 (v7 20); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 948 | exact [exact] (yedek; istenen pure [noSpecials+exact]) | çözüm 9 / sınır 10 (v7 11); açgözlü kazanır; bot48 0,06 (taban 0,06) | 0,08 |
| 949 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 716 | 1,00 |
| 950 | boss [scarce+exact] | kıt renk yellow 66/65; çözüm 12 / sınır 12 (v7 15); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 957 | resource [exact] | çözüm 7 / sınır 11 (v7 11); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,33 |
| 958 | order [exact] | kilit blue→yellow; çözüm 9 / sınır 12 (v7 14); açgözlü kaybeder; bot48 0,15 (taban 0,06) | 0,00 |
| 960 | boss [protect+exact] | kristal 3; çözüm 14 / sınır 14 (v7 18); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 967 | exact [exact] | çözüm 7 / sınır 9 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 968 | reverse [exact] | çözüm 8 / sınır 12 (v7 12); açgözlü kaybeder; bot48 0,13 (taban 0,06) | 0,17 |
| 970 | boss [chainOnly+exact] | zincir yellow 10; çözüm 8 / sınır 9 (v7 10); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 977 | chain [chainOnly+exact] | zincir blue 8; çözüm 12 / sınır 16 (v7 14); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 978 | protect [protect+exact] | kristal 3; çözüm 12 / sınır 17 (v7 18); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 980 | boss [exact] (yedek; istenen boss [noSpecials+exact]) | çözüm 11 / sınır 13 (v7 15); açgözlü kaybeder; bot48 0,10 (taban 0,04) | 0,00 |
| 987 | scarce [scarce] | kıt renk purple 31/30; çözüm 9 / sınır 12 (v7 12); açgözlü kaybeder; bot48 0,06 (taban 0,06) | 0,00 |
| 988 | pure [noSpecials+exact] | çözüm 18 / sınır 19 (v7 12); açgözlü kaybeder; bot48 0,08 (taban 0,06) | 0,00 |
| 990 | boss [scarce+exact] | kıt renk blue 55/54; çözüm 11 / sınır 11 (v7 12); açgözlü kaybeder; bot48 0,04 (taban 0,04) | 0,00 |
| 997 | resource [exact] | HATA: dönüşüm bulunamadı: resource [exact] (v7 korundu) | — |
| 998 | order [exact] | kilit yellow→coral; çözüm 9 / sınır 12 (v7 12); açgözlü kaybeder; bot48 0,10 (taban 0,06) | 0,08 |
| 999 | aha [exact] | 5×6 tek dokunuş; ilk dokunuş 1/5; deneme 89 | 1,00 |
| 1000 | boss [protect+chainOnly+exact] | kristal 3; zincir coral 3; çözüm 11 / sınır 13 (v7 11); açgözlü kaybeder; bot48 0,06 (taban 0,04) | 0,00 |
