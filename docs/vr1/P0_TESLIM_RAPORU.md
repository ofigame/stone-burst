# vr1 paketi — P0 teslim raporu

Tarih: 2026-11-11. Paket: `docs/vr1/claude_code_paketi_v1` (CLAUDE_CODE_BASLANGIC.md iş sırası).
Kapsam: P0 (ipucu güvenilirliği, animasyon süresinin ZPE'den ayrılması, test ve telemetri). P1–P3 başlamadı.
Yayınlama, mağaza gönderimi ve derleme yapılmadı. Bu değişiklikler incelemedeki 1.0.0 sürümünde yok; sonraki derlemeye girer.

## 1. Koddan doğrulanan bulgular

| Kimlik | Paket iddiası | Koddaki durum |
|---|---|---|
| H01 | İpucu yalnız dokunuş döndürüyor | Doğru: `FindHintMove`, çözücünün ilk adımı dokunuş değilse onu eliyordu |
| H02 | Açgözlü seçim kaybı filtrelemiyor | Doğru: kristali kıran ya da hamleyi bitiren dokunuş da puanlanıyordu |
| H03 | Öneri bulunmadan bedel işleniyor | Doğru: `GrantHint` önce `_paidHintUsed` ve RewardedAd bayrağını kuruyordu; öneri null olsa da |
| H04 | Yeniden hesap yalnız yapılabilirliğe bakıyor | Doğru; aynı grubun başka hücresi de tanınmıyordu |
| H05 | Kayıp ipucu yalnız dokunuş | Doğru: ilk adım dokunuş değilse halka gizleniyordu |
| H06 | Çözücü ana iş parçacığında | Doğru: 40.000 düğüm, süre sınırı yoktu |
| ZPE | `OutcomeTick` animasyonu sayıyor | Doğru: `Phase.Animating` hız süresine giriyordu. Bitiş kutlaması (`Phase.Ended`) sayılmıyordu. |

**Yeni bulunan hata (B1):** Reklamlı ipucundan sonra yeniden başlatınca ipucu bedeli (−1 yıldız) yeni denemeye taşınıyordu. Ancak `OutcomeRestart` reklam yardımı bayrağını düşürüyordu; sonuçta o deneme sıralamaya ve ZPE'ye yardımsız yazılıyordu. Düzeltildi: yeniden başlatma yeni denemedir, ipucu hakkı ve bedeli sıfırlanır.

## 2. Uygulananlar

### İpucu (01_IPUCU, onaylı)

**`Scripts/Levels/HintPlanner.cs` (yeni, saf C#):**
- Önce aday yollar denenir: kayıtlı çözüm ve önceki planın kalanı.
- Aday yoksa en-iyi-önce arama yapılır: hedef ilerlemesi yüksek ve kullanılan adım az olan önce açılır.
- Bütçe: 40.000 düğüm + 750 ms. Bulunan yol motorla baştan yeniden yürütülür; kabul edilmeyen, arada biten ya da kazanmayan yol önerilmez.
- Sonuç durumları:
  - Verified
  - BudgetExceeded
  - CandidatesExhausted ("çözümsüz" sayılmaz)
  - Cancelled
  - Terminal
- "En iyi hamle" iddiası yoktur.

**`Scripts/Game/GameController.Hint.cs` (yeniden yazıldı):**
- Akış: Hazırlanıyor → plan yoksa uyarı → plan varsa reklam → ödülde yeniden doğrulama → teslim.
- Hazırlık, tahtanın kopyasıyla `Task.Run` üzerinde çalışır. Sonucu `Update` alır. Deneme, sahne ya da tahta değişince sonuç bayat sayılır.
- Plan yoksa reklam açılmaz. Gösterilen metin: "Bu konum için güvenilir ipucu hazırlanamadı. İpucu hakkın kullanılmadı."
- Reklam iptal olur ya da başarısız olursa bayrak, yıldız ve hak değişmez.
- Yinelenen ya da eskimiş ödül geri çağrısı tek teslim üretir (jeton + `handled`).
- Bedel ve RewardedAd bayrağı yalnız `DeliverHint` sırasında işlenir.
- Ödül gelince tahta değişmişse aynı denemede ücretsiz yeni plan çözülür.
- Öneri uygulandı sayılması için hamleden sonraki durum, planın beklediği durumla aynı olmalı (`LevelSolver.StateKey`). Aynı grubun başka hücresine dokunuş da aynı sonucu veriyorsa sayılır. Özel taşın doğduğu yer gibi sonucu değiştiren fark sayılmaz.
- Başka bir hamle, geri alma ya da ek hamle sonrası:
  - Eski işaret gizlenir.
  - Tahta durunca aynı hakla ücretsiz yeniden çözülür.
  - Başarısız olursa aynı tahtada tekrar denenmez. Uyarı: "Hakkın duruyor; bir hamle sonra tekrar dene."

**Gösterim (tek görselleştirici):**
- Dokunuş: hücrede güçlü altın işaret.
- Tepsi: yuvada altın halka; aynı türde yuva seçilince hedef hücre. Yuva seçimi hakkı bitirmez.
- Döndürme: taban düğmesi.
- Boya: boya düğmesi → hücre → "bu taşı {renk} renge boya" yazısı.
- Rulo: rulo düğmesi → hedefleme sırasında satırın orta hedefi.
- Arayüz halkası için `HudView.ShowHintMark` eklendi.

**Kayıp sonrası ücretsiz ipucu (`GameController.Rules.cs`):** Kayıtlı çözümün ilk adımı doğrulanır ve bütün komut türleri aynı görselle gösterilir. İlk kabul edilen hamlede söner.

**Telemetri (yeni olaylar):**

| Olay | Alanlar |
|---|---|
| `hint_request` | owned |
| `hint_plan_result` | status, nodes, ms, command |
| `hint_unavailable` | status, owned |
| `hint_granted` | plan_id, command |
| `hint_revalidated` | plan_id, command |
| `hint_consumed` | plan_id |

- Tahta ya da kişisel veri gönderilmez.
- Reklam sonucu mevcut `ad_rewarded` olayında kalır.

**Yerelleştirme:** 6 yeni anahtar, 9 dilde:
- `hint.preparing`
- `hint.unavailable`
- `hint.unavailable.kept`
- `hint.got.other`
- `hint.look.other`
- `hint.paint`

### ZPE süre ayrımı (02_ZPE P0)

- `SessionOutcome` üç sayaç tutar: `DecisionSeconds`, `AnimationSeconds`, `TutorialSeconds`.
- `Tick(PlayTime, dt)` bir karede en çok 0,5 sn sayar.
- Karar süresi: yalnız Idle ve Targeting, tur kapalıyken.
- Animasyon süresi: Animating.
- Öğretici süresi: zorunlu tur açıkken.
- Hiç sayılmayanlar:
  - menü ve bütün kartlar (duraklatma, kural, kayıp)
  - test reklam ekranı
  - uygulama odak kaybı ya da arka plan (`OnApplicationFocus`/`Pause`)
  - ipucu hazırlığı ve reklam beklemesi
- `ZpeRecord.From`: `seconds` = karar süresi, `timeVer` = 2. Bilgi alanları `animSeconds` ve `tutorialSeconds` eklendi.
- Eski kayıtlarda `timeVer` 0'dır ve olduğu gibi okunur; süreleri geriye dönük düzeltilemez.
- Ağırlıklar, pencere, `AlgoVersion` ve config değişmedi.
- Normal roket ve Altın Rulo kullanımı zaten yardım sayılmıyordu; değişiklik gerekmedi.

## 3. Uygulanmayanlar ve karar bekleyenler

**Açık ürün kararları (dokunulmadı):**
- Yeni ZPE ağırlıkları (deney A), son 50 bölüm penceresi (deney B), legacy geçişi.
- İpucu −1 yıldız bedelinin kaldırılması.
- Günlük +1 hamle bonusunun ve zorunlu öğreticinin (Guidance) yardım sayılması. Paket bunları "prototip, canlı varsayılan değil" bölümünde veriyor. Davranış aynı kaldı; yalnız öğretici süresi artık karar süresine girmiyor.
- Günlük ücretsiz giriş deneyi.
- Pilot mekaniklerin dağılımı.

**P0'da eksik kalanlar:**
- Dokunuş önerisinde grubun tamamına ince sınır çizilmedi. Hâlâ temsilci hücrede güçlü altın işaret var.
- Kısa neden metni ("Bu roket … açar") yazılmadı.
- Bedel, reklamdan önce yalnız düğmedeki mevcut "−1 ★" rozetiyle görünüyor; ayrıca onay kartı eklenmedi.
- Deneme setinin ipucu (`ShowTrialHint`) eski çözücüyle kaldı. Deneme düğmesi haritadan kaldırılmış olduğundan oyuncu bu ipucuna ulaşamaz.

**Hiç yapılmayanlar:**
- Cihazda süre ölçümü yapılmadı. 750 ms taslak bütçesi cihazda ayarlanmalı.
- Telefonda deneme yapılmadı.
- Yeni ipucu görünümünün ekran görüntüsü alınmadı.

**P1–P3:** başlamadı (buzlu koleksiyon, ikonlar, sesler, pilot katalog).

## 4. Testler

| Komut | Sonuç |
|---|---|
| `dotnet test tools/CoreTests -c Release` (HintPlanner + ZPE filtreli) | 24/24 |
| `tools/unity.ps1 test-edit` | 212 test: 211 geçti, 1 atlandı (elle çalıştırılan ölçüm), 0 başarısız |
| `tools/unity.ps1 test-play` (50 bölüm örneklemi) | 62 test: 60 geçti, 1 atlandı, 1 başarısız. Başarısız test `Collection_Shows22Items_WithRealArt`. |
| `tools/unity.ps1 test-play -Filter DailyFlowTests` | Koleksiyon testi düzeltildikten sonra 4/4 geçti |
| `tools/unity.ps1 loc-check` | TAMAM |

Başarısız koleksiyon testinin nedeni: aynı gün daha önce yapılan "gizli hazineleri gizle" değişikliği. Test hâlâ 22 öğe bekliyordu; o değişiklikten sonra PlayMode çalıştırılmamıştı. Test, 12 öğeyi ve gizli hazinenin görünmemesini bekleyecek şekilde güncellendi. Tam PlayMode bu düzeltmeden sonra yeniden çalıştırılmadı.

**Yeni testler:**
- `HintPlannerTests`:
  - kayıtlı çözüm adayı her adımda doğrulanır
  - dokunuş dışı ilk adımlar (tepsi) önerilir
  - arama yolu yeniden yürütülerek doğrulanır ve ilk hamle kaybettirmez
  - küçük bütçede sonuç "BudgetExceeded", öneri yok
  - kaybettiren yol doğrulanmaz
  - hamle 0 + tepsi durumunda öneri tepsi komutudur
  - elle çalıştırılan ölçüm testi
- `ZpeTimeSplitTests`:
  - Z01: aynı karar süresiyle 0, 5 ve 20 sn animasyon aynı puanı verir
  - 8 adım → 38 sn
  - Z02: None ve öğretici süresi karar süresine girmez
  - büyük kare kırpılır
  - Z04: eski kayıt JSON'u değişmeden okunur
  - 70,0995 örneği
- PlayMode `GameFlowTests`:
  - doğrulanmış plan: başka hamlede ücretsiz yeniden çözüm, reklamsız tekrar gösterim, uygulanınca hak biter
  - iptal ya da başarısız reklam bir şeyi değiştirmez
  - çift ödül geri çağrısı tek teslim
  - plan yoksa reklam açılmaz
  - yeniden başlatma denemeyi sıfırlar

**Ölçüm (masaüstü, Release; 100 bölüm, her biri kayıtlı çözümün yarısından sonra bir sapma hamlesiyle):**
- Eski çözücü 56 durumda dokunuşla başlayan çözüm buldu.
- Yeni planlayıcı 99 durumda doğrulanmış yol buldu. Ortalama 51 ms, en çok 752 ms; level_891 bütçeyi aştı.

## 5. Geriye uyumluluk

- 1000 bölüm verisi değişmedi. `ProductionLevelChecksumTests` EditMode içinde geçti.
- Kayıt şeması yalnız ekleme yaptı: `ZpeRecord.timeVer`, `animSeconds`, `tutorialSeconds`.
- Eski kayıt alanları ve ZPE toplamları (`zpeSum`, `zpeGames`) değişmedi. Yeniden hesap ya da sıfırlama yok.
- Sıralama şeması ve sunucu tarafı değişmedi.
- Değişen ya da eklenen dosyalar: `Scripts/Game/GameController{,.Hint,.Meta,.Rules}.cs`, `Scripts/Levels/{HintPlanner,LevelSolver}.cs`, `Scripts/Levels/Meta/{SessionOutcome,Zpe}.cs`, `Scripts/UI/HudView{,.Hint}.cs`, `Resources/Localization/strings.txt`, testler. Asset GUID'i değişmedi.
