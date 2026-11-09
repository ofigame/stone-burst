# Zemin ve yalnız kenarlık standardı

## Kesin sınır

B01–B10 birer TAHTA ŞEKLİ DEĞİLDİR. Panodaki halka/çıkıntılar yalnız malzeme örneğidir. Hiçbirinde şekil maskesi, cells listesi, ızgara boyu, hücre konumu, iç delik veya collider tanımı bulunmaz. JSON boardShapePreset=null. Mevcut BoardView dış konturları ve iç delik konturları okunur; yalnız görünüş bunlara giydirilir.

## Geometri ve kenar profili

P0=mevcut tahta zemin konturu. Aynı P0 bütün seçeneklerde korunur. Dışarı doğru referans px uzaklık:0–4 iç dudak;4–7pirinçhat;7–17üstpah;17–24yan yüz. Gölge(0,12),blur18,alpha0.26; etkisi max30px. Board mesh/taş sprite/pitch/katman kalınlığı/hit-test değişmez. Her dış ve iç kontur aynı profili alır. Delik tarafında boşluğa dekor taşması en fazla min(24,mevcutboşluk/2-8); negatifse0. Boşluk8px altına iniyorsa profili içi boş alana yayma; yalnız mevcut rim içinde kırp. İç delik kapanmaz, genişlemez veya yeniden yuvarlatılmaz. Pah çizgileri tile yüzüne taşmaz.

Concave köşelerde kontur intersection çözümü bevel join, miterLimit1.5; cell union ve bağlantı topolojisi asla tekrar yorumlanmaz. Görsel kenar radius min(8,mevcutkonturköşeyarıçapı); tile tıklama alanına ek radius uygulanmaz. Tahta görüntüsü kontur-mesh/shader ile; standart dikdörtgen9-slice veya panodaki halka PNGsi ile yapılmaz. Normalized textureUV kontur boyunca arcLength, ölçek1referanspx; şekil uzayınca oyma/faset gerilmez.

| Kimlik | Malzeme | Yan yüz | Gölge | Görünüş | Normal bölüm aralığı |
|---|---|---|---|---|---|
| B01 | Katmanlı Seramik | #D0B18D | #A8896F | 3 basamaklı düz seramik profil; desen yok | 1–100 |
| B02 | İnci Kıvrım | #DFD8BE | #ADAA90 | Dış dudakta 24 px periyotlu 2 px sığ kıvrım; iri dalga yok | 101–200 |
| B03 | Bazalt Altın | #4F514D | #313B39 | Dış basamak bazalt; içte 3 px fırçalı pirinç şerit | 201–300 |
| B04 | Adaçayı Oyma | #8E9D7E | #5E725B | Yalnız yan yüzde 48 px periyotlu 1.5 px geometrik oyma | 301–400 |
| B05 | Buzlu Kristal | #B3CCD7 | #758E9D | Dışta 48 px periyotlu sığ faset; min alpha0.88; kırılma efekti yok | 401–500 |
| B06 | Bal Pirinç | #C5A260 | #8D713E | Çift pah; dış bantta 48 px aralıklı 3 px nokta kakma | 501–600 |
| B07 | Mercan Kil | #C58A70 | #8E5C4D | Yuvarlatılmış pişmiş kil yan profil; 2 px krem iç hat | 601–700 |
| B08 | Petrol Dalga | #477D75 | #2E5752 | 64 px periyotta 2 px sığ dalga kabartması yalnız yan yüzde | 701–800 |
| B09 | Gece Opal | #706579 | #494451 | 96 px aralıklı 4 px pirinç geometrik kakma, parıltı animasyonu yok | 801–900 |
| B10 | Kumtaşı | #C8B28D | #978061 | 48 px aralıklı sığ segment derzi; taş çıkıntısı/hücre oluşturma yok | 901–1000 |

Aralıklar yalnız görsel kaplama eşlemesidir; bölüm kataloğuna, zorluk veya şekle yazılmaz. Deneme/günlük aynı sıradan bir kaplamayı mevcut bölümün görselindeksiyle deterministik kullanır; ayrı güç veya ödül yok.1000sonrası sonprofilB10korunur; veri artışı içerik kararıdır. Zemin seçimi ile border seçimi bağımsızdır.

## Beş zemin

| Kimlik | Ad | Taban | Açık şerit | Koyu şerit |
|---|---|---|---|---|
| Z1 | Şeftali Kum | #F0D8C0 | #E9CFB3 | #E1C3A2 |
| Z2 | Adaçayı | #E0E4D2 | #D4DCC4 | #C8D2B4 |
| Z3 | Petrol İnci | #D1E2DA | #BFD6CC | #B1CBBF |
| Z4 | Dumanlı Lavanta | #DFD8E5 | #D1C7DC | #C4B9D1 |
| Z5 | Taş Mavisi | #D8E2E7 | #C8D6E0 | #BACBD6 |

Şeritler dikey; genişlik60, periyot120, dönüş0°. Görsel motif kenarda72pxşerit içinde; her192pxte yarıçap96yay, merkezsolx=-40,sağx1120;stroke2pxalpha0.12. MerkezveilivoryTopalpha0.12,merkez540/850,elipsr500/900. Göz alıcı parlama, çiçek, fuşya, şeker pembesi, kaydırmalı desenanimasyonu yok. Tahta içdolguivory, pinstripe1px/periyot16/45°/sandalpha0.07. Desen taş PNGlerinin altındadır.

Yeni oyun girişi başında ayrı KOZMETİK RNG ile Z1–Z5 eşit1/5olasılık. Tekrar seçilmesi serbest. Oyun boyunca, pause/resume, ek hamle, geri al, kayıp sonrası aynı bölüm retry boyunca sabit. Menüye dönüp yeni girişte yeniden seç. Core RNG, bot/çözüm tohumu, bölümJSON, kayıtşeması veya oyun sonuçları etkilenmez. Arka plan seçimi yalnız sunum durumunda tutulur.
