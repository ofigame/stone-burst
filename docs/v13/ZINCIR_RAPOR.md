# Zincir kuralı v13 — geçiş raporu (2026-11-08)

Kullanıcı kararı: "Altından birlikte çıkan 3+ aynı renk patlar".

## Kural

Soyduğun taşların altından **yan yana en az 3 aynı renk** taş birlikte çıkarsa, onlar da kendiliğinden patlar. Dalga dalga sürer, hamle harcamaz.

Önceki kuraldan farkı: yanlarındaki eski açık aynı renk taş artık zinciri engellemiyor. Bu eski taşlar zincire katılmıyor, yerinde kalıyor.

- Kod: `RulesEngine.ChainNewOnly` (−2).
- Eski kural ölçüm için duruyor (`ChainTouch = 0`).

## Bölümlere ne oldu

| Adım | Bölüm (1000) | Günlük (240) |
|---|---|---|
| Kayıtlı çözüm yeni kuralla da kazandı | 76 | 22 |
| Işın aramasıyla yeni çözüm; sınır = yeni çözüm + bölümün eski payı | 917 | 218 |
| Yeni kuralla çözülemedi, hedef düşürülerek çözüldü | 7 | 0 |
| Adillik tabanı için sınır en çok +4 artırıldı | 507 | 165 |
| Hamle yetmedi, renk hedefi düşürüldü | 329 (240'ı −%6, 70'i −%12, 19'u −%18 ile −%36 arası) | 73 |

Tahtalar, hedef türleri, bölüm türleri ve kuralları korundu.

Özel durumlar:
- **Bölüm 296:** Rulo açıldı, renk hedefi −%30. Yeni kuralla tahta botta her oyunda kilitleniyordu.
- **Bölüm 4:** Öğretici; tam 2 hamle sınırı korundu.
- **Deneme seti (10, haritada gizli):** `LevelLab trial` ile yeni kurala göre yeniden üretildi.

Ayrıntılar: [ZINCIR_GECIS_BOLUM.md](ZINCIR_GECIS_BOLUM.md), [ZINCIR_KOLAY_BOLUM.md](ZINCIR_KOLAY_BOLUM.md), [ZINCIR_GECIS_GUNLUK.md](ZINCIR_GECIS_GUNLUK.md), [ZINCIR_KOLAY_GUNLUK.md](ZINCIR_KOLAY_GUNLUK.md).

Eski veri arşivde: `KatKat_Arsiv_2026-11-06/zincir_v12_kurali_2026-11-08` (bölüm, günlük, deneme, ZPE, dizin, v8 özeti).

## Doğrulama

| Denetim | Sonuç |
|---|---|
| LevelLab report | 1000/1000; tekrar 0 |
| LevelLab stress 60 | 1000/1000; 60.000 oyun, 973.016 adım |
| CoreTests | 194/194 (5 kural testi yeni kurala göre güncellendi) |
| Unity EditMode | 194/194 |
| Unity PlayMode | 48/48, 1 atlanan |

Yeniden üretilen veriler:
- ZPE referansı (`zperef`).
- Bölüm ve günlük dizini (içerik aynı).
- Özet dosyası `levels_v13.sha256`.

## Zincir ve kolaylık (12 bot oyunu × 1000 bölüm)

| Dilim | Eski: bot kazanma | Yeni: bot kazanma | Eski: zincirli hamle | Yeni: zincirli hamle |
|---|---|---|---|---|
| 1–20 | %66 | %55 | %15 | %33 |
| 21–100 | %43 | %32 | %15 | %30 |
| 101–300 | %28 | %18 | %13 | %29 |
| 301–600 | %22 | %15 | %15 | %31 |
| 601–1000 | %16 | %12 | %15 | %31 |

**Okuma:**
- Zincir artık iki kat sık; kural tek cümle.
- Açgözlü bot ise eskisinden daha az kazanıyor. Sık zincir tahtayı çabuk dağıtıyor ve "yasal hamle kalmadı" kaybı artıyor.
- Her bölüm adillik tabanının üstünde, ama ortalama zorluk eski setin biraz üstünde.
- Bot gerçek oyuncu değil; cihazda denenmedi.
