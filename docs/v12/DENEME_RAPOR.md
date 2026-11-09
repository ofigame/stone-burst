# TriKat v12 — Deneme seti raporu (2026-11-08)

Plan: `docs/v12/PLAN.md` §6 (kural verilir, strateji verilmez). Üretim ölçümleri: [DENEME_URETIM.md](DENEME_URETIM.md).
Görüntüler: `docs/captures/v12/deneme/` (`tools/unity.ps1 capture-v12`).

## Nasıl girilir

Haritada (ana sayfa) sol sütunda Sıralama'nın altında **Deneme** düğmesi → "Deneme Bölümleri" listesi → bölüme dokun.
Ana 1000 bölüm, yıldızlar, ZPE ve sıralama etkilenmez; yalnız bitirilen denemeler kayıtta (`meta.trialDone`).

## 10 bölüm

| # | Tür | Kural | Ne ister (oyuncuya söylenmez) |
|---|---|---|---|
| 1 | Tek Dokunuş | kesin hamle (1) | 5 olası dokunuştan yalnız biri zinciri başlatıp hedefi doldurur |
| 2 | Kesin Hamle | kesin hamle (4) | 7 ilk hamleden 1'i kazandırır |
| 3 | Ters Düşünme | kesin hamle (4) | en büyük grubu ya da en çok hedef ilerlemesini seçen bot kaybeder |
| 4 | Renk Kıtlığı | kıt renk | hedef rengin neredeyse tamamı gerekir (ilk hamle seçeneği az: zayıf örnek) |
| 5 | Zincir Ustası | yalnız zincir sayılır | hedef yalnız domino dalgasıyla dolar |
| 6 | Kristal Koruma | kristal | kristal yığın soyulursa (zincirle de) kayıp |
| 7 | Saf Plan | özel taş yok + kesin hamle | 6×6, 3 ilk hamleden 1'i |
| 8 | Doğru An | kesin hamle | özel taşsız çözülemez; tepsi doğru yerde/anda |
| 9 | Kilit Sırası | kesin hamle | renk kilitleri; doğru renk önce toplanmalı |
| 10 | Patron: Liman Feneri | kristal + zincir + kesin hamle | 6×7, en kısa 5 hamle, açgözlü bot kaybeder |

Her bölüm LevelLab `trial` ile tohumlu üretildi ve çözücüyle ölçüldü (en kısa çözüm, kazandıran ilk hamle oranı, açgözlü bot, özel taşsız çözülebilirlik). Kayıtlı çözümlerin hepsi kazanıyor (test).

## Yeni kurallar (motor)

- **Kristal** (`protected` hücre): katmanı soyulursa `LostProtected` (hedef dolsa bile).
- **Zincir hedefi** (`collectChain`): yalnız zincir dalgasıyla soyulan katmanlar sayılır; hedef çipinde zincir işareti.
- **Kesin hamle**: hamle sınırı = en kısa çözüm. **Özel taş yok**: `specials` yeteneği kapalı.
- Deneme tahtaları dikdörtgen olabilir (5–8 × 5–9). Ana bölümlerin kuralları değişmedi; bölüm dosyaları aynı (özet testi).

## Oyuncuya gösterilen

- **Kural kartı** (bölüm açılırken): tür adı, türün tek cümlesi, kurallar (simge + cümle). Çözüm söylenmez.
- **Kalıcı kural simgeleri**: hedef kabuğunun sağında; dokununca kural cümlesi.
- **Katman görünümü (tam bilgi)**: yığına basılı tutunca (0,42 sn) alttan üste bütün katmanlar. Deneme bölümlerinde dokunuş bırakınca çalışır (ana oyunun dokunma hissi değişmedi; beğenilirse yayılır).
- **İpucu**: aynı denemede 2 kayıptan sonra kayıp kartında "İpucu" → yeniden başlar, yalnız bir sonraki doğru hamle gösterilir.

## Açık / sonra

- Renk Kıtlığı örneği zayıf (ilk hamle seçeneği 2): üretici ayarı ya da elle tasarım.
- Yeni mekanikler (saatli yığın, bukalemun, ayna çifti) ve adalar bu sette yok.
- Zorluk "15 dakika" hedefi bu sette yok: bölümler küçük (5×6), en kısa 1–5 hamle; önce türlerin anlaşılır olduğu doğrulansın, sonra büyütülsün.
- Kristal rozeti telefonda küçük kalabilir; cihazda bakılmalı.
