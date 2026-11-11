# Pilot katalog üretim raporu (LevelLab pilot, tohum 20261111)

Canlı 1–1000 bölüme dokunulmadı. P10, derin mühür için ön elemeyle ayrıca üretildi (`pilot 20261111 10`). Her bölümün kayıtlı çözümü motorla yeniden yürütülüp kazandığı doğrulandı.
"Amaç" tasarım varsayımıdır; "Ölçüm" sütunları bot ve çözücüyle bulunan gerçek değerlerdir (oyuncu testi değildir).

| # | Tür | Amaç (varsayım) | Tahta | Hamle | Kayıtlı çözüm | Kazandıran ilk hamle | Açgözlü | Büyük grup önce | En derin zincir | Deneme |
|---|---|---|---|---|---|---|---|---|---|---|
| P01 | pilot_compact | Kompakt-derin: dar tahta, derin yığın; güvenli ve ustalık yolu | 4×4 | 8 | 6 adım | 3/3 | kazanır | kazanır | 1 | 1 |
| P02 | pilot_wide | Geniş-sığ rahatlama: büyük tahta, 1–2 katman | 8×8 | 9 | 6 adım | 7/7 | kazanır | kazanır | 1 | 2 |
| P03 | pilot_bridge | İki oda, 1 hücrelik bağlantı; bağlantıyı erken boşaltmanın sonucu görünür | 7×7 | 11 | 9 adım | 6/6 | kaybeder | kaybeder | 1 | 1 |
| P04 | pilot_wing | Düğmeyle kanat açma + buz (mevcut engeller) | 6×7 | 10 | 8 adım | 2/3 | kaybeder | kaybeder | 1 | 1 |
| P05 | pilot_stages | Aşamalı hedef tanışma: 2 renk sırası, tahta sıfırlanmaz | 5×6 | 8 | 6 adım | 6/6 | kaybeder | kaybeder | 1 | 1 |
| P06 | pilot_stages | Aşamalı hedef uygulama: 3 renk sırası | 6×6 | 8 | 6 adım | 5/6 | kazanır | kaybeder | 1 | 2 |
| P07 | pilot_stages | Aşamalı hedef + mevcut engel (buz) | 6×7 | 8 | 6 adım | 5/5 | kazanır | kaybeder | 1 | 1 |
| P08 | pilot_seal | Zincir mührü: 1. otomatik dalga kanadı açar | 7×7 | 12 | 10 adım | 6/6 | kaybeder | kaybeder | 1 | 8 |
| P09 | pilot_seal | Zincir mührü: 2. dalga gerekir (alt katmanları hazırla) | 7×7 | 10 | 8 adım | 5/6 | kaybeder | kaybeder | 2 | 2 |
| P10 | pilot_seal | Zincir mührü: 3. dalga (zor; planlı zincir) | 7×7 | 13 | 10 adım | 4/4 | kazanır | kaybeder | 3 | 152 |
| P11 | pilot_twin | İki alan, ortak hamle ve tepsi; roket yalnız seçilen alanda | 7×6 | 6 | 4 adım | 10/10 | kazanır | kazanır | 1 | 1 |
| P12 | pilot_twin | İki alan: bir alanda üretilen özel taş diğerinde kullanılır | 7×6 | 5 | 4 adım (tepsi) | 7/7 | kazanır | kaybeder | 1 | 1 |

Notlar: "Kazandıran ilk hamle" = ilk hamleden sonra bütçe içinde doğrulanmış çözüm bulunan yasal hamle sayısı / yasal hamle (alternatif yol göstergesi; bütçe dolan dal kazandırmaz sayılır). "En derin zincir" = kayıtlı çözümdeki en uzun otomatik zincir dalgası sayısı.
