# Zincir kuralı ölçümü (2026-11-08)

Kullanıcı: "zincir kuralı anlaşılmıyor sorun o. daha anlaşılır ve biraz kolay olmalı".

**Araç:** `tools/LevelLab chaincmp 12 <kurallar>`. Kural, motorda `RulesEngine.ChainTouch` ile seçilir; oyunda değeri 0, yani oyun değişmedi.

**Yöntem:** 1000 bölümde iki şey ölçüldü:
- Kayıtlı çözüm kazanıyor mu?
- Yumuşak açgözlü bot (her bölümde 12 oyun) ne sıklıkla kazanıyor, ne kadar zincir yapıyor?

## Denenen kurallar

| Kod | Kural |
|---|---|
| 0 | **Mevcut.** Altından çıkan aynı renkli 3+ grubun **tamamı** yeni açılmış ve rengi değişmiş olmalı. Gruba açık bir eski taş değiyorsa zincir yok. |
| 1 / 2 / 3 | "Dokunma": 3+ grupta en az 1 / 2 / 3 yeni açılmış taş varsa grup patlar, açık eski komşular da katılır. |
| −1 | Yalnız birlikte açılanlar: yan yana 3+ yeni açılmış aynı renk taş patlar. Eski komşu ne engeller ne katılır. Renk şartı yok. |
| −2 | −1 ile aynı, ama hücrenin rengi de değişmiş olmalı ("altından yeni renk çıktı"). |

## Sonuçlar

Bot kazanma oranı (%), bölüm dilimine göre:

| Kural | 1–20 | 21–100 | 101–300 | 301–600 | 601–1000 | Zincirli hamle | Kazanan kayıtlı çözüm |
|---|---|---|---|---|---|---|---|
| 0 (mevcut) | 66 | 43 | 28 | 22 | 16 | %13–15 | 1000/1000 |
| −2 | 51 | 25 | 16 | 10 | 9 | %30–33 | 76/1000 |
| −1 | 50 | 24 | 15 | 11 | 9 | %38–40 | 39/1000 |
| 3 | 55 | 29 | 18 | 13 | 10 | %37–47 | 29/1000 |
| 2 | 47 | 24 | 12 | 11 | 8 | %54–64 | 21/1000 |
| 1 | 60 | 22 | 12 | 10 | 8 | %71–88 | 21/1000 |

## Sonuç

- **Her gevşetme zinciri 2–6 kat artırıyor ama bölümleri zorlaştırıyor.** Zincirler hedefte gereken renkleri ve kurulmak istenen grupları oyuncu kullanmadan yiyor.
- **Kayıtlı çözümlerin %92–98'i bozuluyor.** Bölüm tasarımı (hamle sınırı, hedef) mevcut kurala göre yapıldığı için kural değişirse 1000 bölüm yeniden üretilmeli.
- **Bölümlere dokunmadan anlaşılırlık ve kolaylık:** zincir başlatacak grupları tahtada işaretlemek ve kuralı tek cümleyle, görsel bir örnekle anlatmak. Bu yol kullanıcı kararını bekliyor.
