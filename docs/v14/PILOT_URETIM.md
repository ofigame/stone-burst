# Pilot katalog üretim raporu (LevelLab pilot, tohum 20261111)

Canlı 1–1000 bölüme dokunulmadı. Her bölümün kayıtlı çözümü motorla yeniden yürütülüp kazandığı doğrulandı.
"Amaç" tasarım varsayımıdır; "Ölçüm" sütunları bot ve çözücüyle bulunan gerçek değerlerdir (oyuncu testi değildir).

| # | Tür | Amaç (varsayım) | Tahta | Hamle | Kayıtlı çözüm | Kazandıran ilk hamle | Açgözlü | Büyük grup önce | En derin zincir | Deneme |
|---|---|---|---|---|---|---|---|---|---|---|
| P01 | pilot_compact | Kompakt-derin: dar tahta, derin yığın; güvenli ve ustalık yolu | 4×4 | 9 | 7 adım | 3/3 | kazanır | kazanır | 0 | 1 |
| P02 | pilot_wide | Geniş-sığ rahatlama: büyük tahta, 1–2 katman | 8×8 | 9 | 6 adım | 7/7 | kazanır | kazanır | 0 | 2 |
| P03 | pilot_bridge | İki oda, 1 hücrelik bağlantı; bağlantıyı erken boşaltmanın sonucu görünür | 7×7 | 11 | 9 adım | 6/6 | kaybeder | kaybeder | 0 | 1 |
| P04 | pilot_wing | Düğmeyle kanat açma + buz (mevcut engeller) | 6×7 | 8 | 6 adım | 3/3 | kaybeder | kaybeder | 0 | 1 |
| P05 | pilot_stages | Aşamalı hedef tanışma: 2 renk sırası, tahta sıfırlanmaz | 5×6 | 7 | 5 adım | 6/6 | kaybeder | kaybeder | 0 | 1 |
| P06 | pilot_stages | Aşamalı hedef uygulama: 3 renk sırası | 6×6 | 8 | 6 adım | 6/6 | kaybeder | kaybeder | 0 | 2 |
| P07 | pilot_stages | Aşamalı hedef + mevcut engel (buz) | 6×7 | 6 | 4 adım | 5/5 | kazanır | kaybeder | 0 | 1 |
| P08 | pilot_wing | Düğmeli kanat: hedefin bir kısmı kanatta, düğmeyi doğru anda boşalt | 7×7 | 16 | 14 adım | 2/6 | kaybeder | kaybeder | 0 | 4 |
| P09 | pilot_wing | Düğmeli kanat + buz: düğmeye giden yol buzla kapalı | 7×7 | 13 | 11 adım | 4/5 | kaybeder | kaybeder | 0 | 2 |
| P10 | pilot_wing | Düğmeli kanat, derin yığınlar (zor; düğme yığını 3–4 katman) | 7×7 | 15 | 13 adım | 6/7 | kaybeder | kaybeder | 0 | 1 |
| P11 | pilot_twin | İki alan, ortak hamle ve tepsi; roket yalnız seçilen alanda | 7×6 | 6 | 4 adım | 10/10 | kazanır | kazanır | 0 | 1 |
| P12 | pilot_twin | İki alan: bir alanda üretilen özel taş diğerinde kullanılır | — | — | — | — | — | — | — | 589 (bulunamadı) |

Notlar: "Kazandıran ilk hamle" = ilk hamleden sonra bütçe içinde doğrulanmış çözüm bulunan yasal hamle sayısı / yasal hamle (alternatif yol göstergesi; bütçe dolan dal kazandırmaz sayılır). "En derin zincir" = kayıtlı çözümdeki en uzun otomatik zincir dalgası sayısı.
