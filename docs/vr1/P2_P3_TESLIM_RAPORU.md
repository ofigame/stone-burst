# vr1 paketi — P2 ve P3 teslim raporu

Tarih: 2026-11-11. Paket: `docs/vr1/claude_code_paketi_v1` (belge 05_TAHTA_YENILIKLER).
Kullanıcı isteği: "tüm talimatları uygulayalım ve p2 p3 e geçelim".

**Kapsam ve sınırlar**
- Canlı 1–1000 bölüme ve kayıtlara dokunulmadı.
- Yeni kurallar yalnız ayrı pilot katalogda ve yetenek anahtarıyla (`stages`, `seal`, `areas`) çalışır.
- Pilot listesi yalnız prototip anahtarı açıkken haritada görünür.
- Pilot bölümler ana ilerlemeye, yıldıza, ZPE'ye ve sıralamaya yazılmaz (deneme seti akışı).
- Yayın yapılmadı. Telefonda denenmedi. Oyuncu testi yapılmadı.

**Paketin "açık ürün kararı" dediği madde (dokunulmadı):** pilot mekaniklerin nihai kapsamı, zorunlu mu isteğe bağlı mı olacakları ve 1000 bölüme dağılımı.

## 1. Kural motoru (P3)

| Kural | Davranış | Paket varsayımı |
|---|---|---|
| **Aşamalı hedef** (`goals[].stage`, yetenek `stages`) | Bütün aşamalar görünür; tahta sıfırlanmaz. İlerleme yalnız etkin aşamanın hedeflerine sayılır. Etkin aşama, dalga sonunda tamamlanınca sonraki aşamaya geçilir; aynı hamlenin sonraki zincir dalgası yeni aşamaya sayılır. | "Aynı dalganın fazlası aktarılmaz" uygulandı. Paket bunu başlangıç kuralı sayıyor; final tasarım kabulü gerekir. |
| **Zincir mührü** (`seal: {wave, gate}`, yetenek `seal`) | Bir hamlenin otomatik zincir derinliği `wave`'e (1–3) ulaşınca bağlı geçit açılır. Doğrudan dokunuş derinlik 0 sayılır. Mührün komşuluk şartı yoktur. Açılan hücreler o dalgada soyulmaz (mevcut geçit kuralı). | Tahtadaki global mühür, mevcut geçidi kullanır. |
| **İki alan** (`cells[].area`, yetenek `areas`) | Alanlar boş sütunla ayrılır; hamle ve tepsi ortaktır. Roket (satır/sütun) ve Altın Rulo yalnız ateşlendiği hücrenin alanında işler (birleşimler dahil). | "Renk temizleyicinin kapsamı seçilen alan" uygulandı. Küresel temizleme ayrı bir karar. |

**Entegrasyon**
- Durum kopyalama, eşitlik ve çözücü anahtarı (`LevelSolver.StateKey`): aşama ve mühür eklendi. Anahtar eski bölümlerde değişmez.
- Çözücü aday listesi: roket ve Altın Rulo adayları artık alan başına üretiliyor. Tek alanlı tahtada sıra ve sonuç değişmedi.
- Statik doğrulama:
  - Yeni veri ilgili yetenek yoksa reddedilir.
  - Aşamalar ardışık olmalı.
  - Mühür derinliği 1–3 olmalı ve geçidi bulunmalı.
  - Alan değeri 0 ya da 1 olmalı.
  - Yinelenen hedef denetimi aşamayı da dikkate alır.
- İpucu (`HintPlanner`) yeni kurallarla çalışır, çünkü motoru kullanır. Pilot bölümler deneme akışında oynanır; reklamlı ipucu yalnız normal bölümde.

**Arayüz**
- Aşamalı hedefte sonraki aşama soluk (%38), tamamlanan aşama %70.
- Aşama geçişinde "Aşama 2/3" bildirimi; mühür açılınca "Mühür açıldı!" bildirimi.
- Kural kartında yeni kuralların açık cümlesi ve simgesi (9 dil).

## 2. Pilot katalog (P2 + P3 örnekleri)

- Üretici: `tools/LevelLab` → `dotnet run -- pilot [tohum] [yalnız NN]`.
- Çıktı: `Assets/KatKat/Resources/Pilot/pilot_01..12.json` ve rapor [PILOT_URETIM.md](PILOT_URETIM.md). Raporda tasarım amacı ile ölçümler ayrı sütunlarda.
- Her bölümün kayıtlı çözümü motorla yeniden yürütülüp kazandığı doğrulandı.

| # | Tür | Ne |
|---|---|---|
| P01 | Kompakt-derin | 4×4, 3–4 katman |
| P02 | Geniş-sığ | 8×8, 1–2 katman; rahatlama, açgözlü bot kazanır |
| P03 | Dar geçit | İki oda, tek hücrelik bağlantı |
| P04 | Kanat | Düğmeyle açılan kanat + buz (mevcut engeller) |
| P05–P07 | Aşamalı hedef | 2 aşama, 3 aşama, buzlu 2 aşama |
| P08–P10 | Zincir mührü | Derinlik 1, 2, 3; hedef rengin bir kısmı yalnız mühürle açılan kanatta |
| P11–P12 | İki alan | Ortak hamle ve tepsi |

**Dürüst notlar**
- P01'in "güvenli ve ustalık yolu" niteliği ölçülmedi. Ölçülen yalnız: 3 farklı ilk hamleden kazanılabiliyor.
- P03'te "bağlantıyı erken boşaltmanın sonucu görünür" amacı ölçülmedi.
- P12'nin "bir alanda üretilen özel taş diğerinde kullanılır" amacı doğrulanmadı. Kayıtlı çözüm tepsi kullanıyor, ama taşın hangi alanda üretilip hangisinde kullanıldığı denetlenmedi.
- Zorluk iddiası yok. Ölçümler bot ve çözücüyle; oyuncu testi değil.
- P10 (derinlik 3) rastgele tahtada nadir. "Tek hamlede 3 dalga mümkün mü" ön elemesiyle 152 denemede bulundu.

## 3. Testler

| Test | Ne doğrular |
|---|---|
| `P3RulesTests` (5) | Aşama dalga sonunda ilerler; fazla aktarılmaz; etkin olmayan aşama sayılmaz; mühür derinliğe ulaşınca geçit açılır, yetmezse açılmaz; roket yalnız kendi alanında; yeni veri yetenek ister |
| `PilotCatalogTests` (2) | Her pilot doğrulamadan geçer, kayıtlı çözümü kazanır ve kendi kuralını gerçekten kullanır (aşama ilerler / mühür açılır / iki alan var) |
| `Vr1FlowTests.Pilot_StagedLevel_SavedSolutionWins_MainProgressUntouched` | Haritada Pilot düğmesi yalnız prototipte; aşamalı pilot kayıtlı çözümle kazanılır; sonraki aşama soluk; ana ilerleme ve yıldız değişmez |
| Ana katalog | `ProductionLevelChecksumTests` (1000 bölüm SHA-256) ve `EveryLevel_PassesStaticValidation_AndRecordedSolutionWins` — kural motoru değişikliğinden sonra da |

Son tam koşu: CoreTests 223/223, EditMode 224/226 (2 elle çalıştırılan atlandı), PlayMode 71/72 (1 atlandı), loc-check TAMAM.

İnceleme ekranları: `docs/vr1/p1_inceleme/p2_*.png`
- Harita Pilot düğmesi
- Pilot listesi
- Aşama kural kartı
- 12 pilot tahtası

## 4. Yapılmayanlar

- Yeni kurallar için öğretici turu ya da tanıtım animasyonu yapılmadı; yalnız kural kartı ve kural simgeleri var.
- Mührün tahtada ayrı bir görseli yok. Kapalı kanat mevcut geçit önizlemesiyle soluk görünüyor; mühür, kural simgesiyle gösteriliyor.
- Aşama geçişi için ayrı kutlama animasyonu yok; yalnız bildirim ve çiplerin saydamlığı değişiyor.
- Pilot oturumları için ayrı analytics olayları yok; mevcut `level_start/level_end` olayları `kind = trial` ile gidiyor.
- "Sonraki adaylar" yapılmadı: köprü katmanı, ustalık görevleri, bölge finali, ritim.
