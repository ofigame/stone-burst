# TriKat v8 — 1000 bölüm yeni kurallarla (2026-11-08)

Kullanıcı onayı: "Onay, başla" (plan: [PLAN.md](PLAN.md) §6, kararlar 1–6). Deneme setinin bölüm türleri 1000 bölüme ve günlük bulmacalara yayıldı.

## Ne değişti

- **Bölümler (v8):** v7 tahtaları, şekilleri ve mekanikleri korundu. Takvimdeki bölümler bir tür aldı ve çözücüyle yeniden ölçüldü.
  - 10'luk döngüde 7. ve 8. bölüm tür bölümü, 10. bölüm patron, 100. bölümler 3 kurallı sınav.
  - Ayrıntı ve tanıtım bölümleri: [OYUN.md](../OYUN.md) "Bölüm türleri (v8)".
- **Sayılar:** 1000 bölümün 295'i türlü, 705'i v7 bölümü (bayt bayt aynı, 646 hariç).
  - Türler: patron 90, kesin hamle 38, ters düşünme 24, doğru an 23, kıt renk 23, kilit sırası 22, kristal 22, tek dokunuş 20, saf plan 17, zincir 16.
- **Üretim:** `LevelLab v8` ([V8_URETIM.md](V8_URETIM.md)).
  - İstenen tür tutmazsa yedek uygulandı: kesin hamle, ya da yalnız kesin hamleli patron.
  - İki bölümde (468, 997) tür hiç uygulanamadı; v7 kaldı.
- **Adillik** (kullanıcı: "adil zorluk"; "tüm bölümlerin çözümünü, zorluğunu, seviyeye uygunluğunu test et"):
  - **Üretim tabanı:** hamle sınırı, yumuşak açgözlü bot 48 oyunda tabana ulaşana kadar artar. Taban bölge 1'de %10, sonra %6, patronda %4.
  - **Onarım:** `LevelLab fairfix`, iki tur ([V8_ADILLIK.md](V8_ADILLIK.md)). İki bağımsız tohum dizisiyle 112 bot oyunu ölçüldü.
    - Tabanlar: tanıtım ≥ %12,5, patron ≥ 5 galibiyet, bölge 1 ≥ %8, diğer ≥ %5.
    - +4 tavanında da adil olmayan 17 tür bölümü v7 haline döndü.
  - **Bot değişikliği:** kristali hemen kıran hamleyi seçmez (yalnız kristalli bölümleri etkiler).
- **Kural metni:** "Hamle payı yok" → "Dar hamle: en çok N hamlede bitir, boşa hamle bırakma" (9 dil). Adillik payı yüzünden sınır, kayıtlı çözümden 1–4 hamle fazla olabilir.
- **Oyunda:**
  - Kural simgeleri hedef kabuğunun sağında.
  - Kural kartı yalnız türün ilk görüldüğü bölümde ve patronlarda (`meta.seenTypes`); patron başlığı "Patron Bölümü".
  - Aynı bölümde 2 kayıptan sonra ipucu: bölüm baştan başlar, kayıtlı çözümün ilk hamlesi gösterilir; ZPE'de yardımlı.
  - Kristal kaybı kartı.
  - Uzun basınca katman görünümü artık bütün oturumlarda; dokunuş parmak kalkınca işler.
  - Haritadaki Deneme düğmesi kalktı; deneme listesi kodda duruyor.
- **Günlük `daily-v3`** ([GUNLUK_V3_URETIM.md](GUNLUK_V3_URETIM.md)):
  - 240 bulmacanın 235'i türlü (5'i v2 kaldı).
  - Gün türü haftalık döner; oyuncunun grubunda açılmamış tür atlanır.
  - Taban %4 ("çok zor"); her günlükte kural kartı ve simgeler.
  - `daily-v2` arşivde; kayıt içerik sürümüyle tutulduğu için mevcut kayıtlar bozulmaz.
- **Kayıt ve veri:**
  - Özet testi `Tests/EditMode/Data/levels_v8.sha256`'ya geçti (v7 özeti arşivde).
  - Bölüm dizini ve ZPE referansı (`Resources/Zpe/level_ref.json`) yeniden üretildi.

## Doğrulama (2026-11-08)

| Denetim | Sonuç |
|---|---|
| Kayıtlı çözüm (LevelLab `report`, kural motoru) | 1000/1000 kazanıyor; tekrar eden bölüm 0 |
| Zorluk ve uygunluk (LevelLab `audit`, 48 bot oyunu, farklı tohum) | 1000/1000 uygun ([V8_ZORLUK.md](V8_ZORLUK.md)) |
| Stres (LevelLab `stress 60`: 60.000 oyun, 1,02 milyon adım) | 1000/1000; bot her bölümde en az bazen kazanıyor |
| Mekanik/tür tanıtım takvimi | ihlal yok (denetim içinde) |

**Zorluk eğrisi** (ortalama bot kazanma oranı; düşük = zor):

| Dilim | Normal | Tür | Patron |
|---|---|---|---|
| 1–100 | 0,51 | 0,22 | 0,18 |
| 101–200 | 0,37 | 0,13 | 0,08 |
| 201–300 | 0,26 | 0,15 | 0,07 |
| 301–400 | 0,23 | 0,10 | 0,09 |
| 401–500 | 0,23 | 0,11 | 0,08 |
| 501–600 | 0,19 | 0,08 | 0,07 |
| 601–700 | 0,17 | 0,09 | 0,08 |
| 701–800 | 0,16 | 0,08 | 0,05 |
| 801–900 | 0,14 | 0,08 | 0,08 |
| 901–1000 | 0,14 | 0,09 | 0,07 |

Her dilimde tür bölümleri normalden, patronlar türden zor (ya da yakın). Bölge 1 öğretici kalıyor. Bot zorluk ölçüsüdür, oyuncu değildir: kesin hamle ve zincir türleri plan ister; bot düşük, oyuncu daha yüksek kazanır. Cihazda oynanmadı.

## Açık / sonra

- Zincir türü botla zor ölçülüyor (bot zincir dalgası kurmayı nadiren başarıyor). 3 zincir bölümü (118, 497, 977) v7'ye döndü. Elle denenmeli.
- 8 patron (120, 250, 420, 480, 850, 880, 930, 1000) v7'ye döndü (adil patron bulunamadı); 1000. bölüm artık normal bölüm.
- Yeni mekanikler (saatli yığın, bukalemun, ayna çifti, beşinci renk, kilitli tepsi): karar 1 gereği bu turda yok.

## Unity testleri (2026-11-08)

- CoreTests 194/194, EditMode 194/194 (v8 özet testi dahil), loc-check TAMAM (Japonca metindeki 内 harfi kana ile değiştirildi).
- PlayMode **tam** (`test-play -All`): 48/48 (+1 atlanan ölçek ölçümü). 1000 bölümün kayıtlı çözümü gerçek oyun denetleyicisiyle kazanıyor (~53 dk). Günlük akış testi daily-v3 ile geçti.
- Ana oyundaki kural kartı / ipucu / kristal kaybı kartı ekranda görsel olarak denenmedi. APK (123,6 MB) ve exe bu setle 11:14'te derlendi; exe açılış denemesi hatasız.
