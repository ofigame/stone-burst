# TriKat v10 · Oyun içi arayüz tasarımı

## Claude Code görevi

KatKat Unity projesinin kökünde çalış. Bu dosyayı ve yanındaki **01-onayli-referans.png** dosyasını oku. Referansın **üst HUD, dekoratif arka plan, tahta çerçevesi ve alt kontrol kabuğu** görsel dilini mevcut oyun ekranına uygula.

### Kesin kapsam sınırı · Taşlar korunacak

- Referansta görünen taşlar yalnızca örnektir. Unity'de şu an aktif olan **dört ana taşı ve bütün özel taşları/kaplamaları birebir koru**. Sprite, kaynak PNG, atlas, renk, parlaklık, sembol, malzeme, shader, yüz/yan oranı, katman kalınlığı ve soyulma animasyonu değişmez. Buz, zarf, sünger, ataş, roket, rulo ve diğer mevcut mekaniklerin kendi görselleri korunur.
- `StackView`, `TileSkin`, `LayerPalette`, taş shader'ları, taş atlasları ve animasyonlarında görsel değişiklik yapma. Görev sayaçlarında da mevcut hedef sprite'larını kullan. Görsel farkı kapatmak için üstteki referans taşlarını kesip kullanma.
- Tahta matrisi, aktif/pasif hücreler, katman sayıları, bölüm JSON'ları, hamle kuralları ve güçler korunur. Referansın basamaklı tahtasını üretmek için bölüm verisini değiştirme. Harita yolu, ana sayfa, Koleksiyon ve diğer menüler kapsam dışıdır.
- Referanstaki çekiç, roket, renk topu, bomba ve artı rozetleri yeni mekanik talebi sayılmaz. Mevcut çalışan butonlar ve yetenek yuvaları kendi simgeleriyle yeni görsel kabuğa taşınır. Sahte buton, mağaza, sayaç veya güç ekleme.

## Mevcut Unity bağlantıları

- `Assets/KatKat/Scripts/UI/HudView.cs` içinde `BuildHeader`, `BuildScoreBar`, `BoardWorldRect` ve `BuildRoll`, `BuildUndo`, `BuildPaint` ile mevcut özel yetenek tepsisini bul. Yeni düzeni burada mevcut veriye bağla. Üstteki geri al, alttaki mevcut geri al ile aynı aksiyonu ve etkileşim kurallarını kullanır. Aynı ekranda ikinci geri al gösterme. Duraklatma mevcut `PausePressed` bağlantısını korur.
- `MascotView` ve güncel maskot sprite'ları korunur. Maskotu mor hamle şeridinin üstüne yerleştir. Göz takibi, kırpma, heyecan ve son hamle durumları mevcut akıştan çalışır. Referanstaki maskotu keserek kullanma.
- `GameController.cs` içinde temadan gelen arka plan seçimini takip et. Mor bulutlu referans, oyun içi görsel kabuk için onaylı görünüm. Mevcut tema varlıklarını ezme. Bu arka planı yazısız, ayrı bir varlık olarak ve tema mekanizmasıyla uyumlu bağla. Harita ile ortak tema ayarını değiştirerek haritayı etkileme. Farklı oyun temalarına uygulanacak kapsamı açıkça raporla.
- `BoardView.cs` içindeki `LayoutFrame`, `ApplyThemeSprites`, `LayoutFloorTiles` ve aktif hücre temsillerini incele. Görsel çerçeve mantıksal matristen türesin. Dış kontur, kullanılan gerçek hücrelerin çevresini izleyebilir. İç boşluk ve geçitler mevcut kurallara göre kalır. Kontur üretimi tamamlanamıyorsa geçici olarak mevcut çerçeveyi koru ve farkı bildir. Hücre merkezi, tahta adımı, isabet alanı ve yığın geometrisi görsel konturdan bağımsız kalmalı.

## Referans ölçüleri

PNG **941×1672 px**. Koordinat başlangıcı sol üst. Ölçüler görünen sınırlar üzerinden yaklaşık. 1080 birim genişliğinde yatay değerleri 1080/941 ile ölçekle. Üst ve alt bileşenleri kendi güvenli ekran kenarlarına bağla. Tahtanın ölçeklenmesini mevcut `BoardWorldRect` akışı üzerinden ayarla.

| Öğe | x | y | Görünen ölçü |
|---|---:|---:|---:|
| Duraklatma | 42 | 49 | 106×107 |
| Geri al | 795 | 49 | 106×107 |
| Maskot ve dekor dahil hamle grubu | 180 | 8 | yaklaşık 580×175 |
| Mor hamle şeridi | 307 | 88 | yaklaşık 328×92 |
| Yıldız kabuğu | 75 | 181 | 793×89 |
| Hedef kabuğu | 31 | 275 | 880×92 |
| Örnek tahtanın dış sınırı | 85 | 382 | yaklaşık 771×1052 |
| Alt kontrol kabuğu | 0 | 1461 | yaklaşık 941×207 |

Tahta için kutu bir örnek, sabit boyut şartı değildir. Normal ve dar telefonlarda üst/alt grupların gerçek yüksekliği ve mevcut matrisin oranı tahtaya ayrılan alanı belirler.

## Görsel kurgu ve etkileşim

1. **Arka plan:** mor-mavi merkez, kenarlarda lavanta bulutları, birkaç sabit yumuşak ışık. Tahta altında yazı, simge veya yoğun desen bulunmaz. Dekor kaydırma ve tıklamayı kapatmaz.
2. **Hamle:** ortada dikiş detaylı mor şerit, arkasında yeşil yapraklar ve iki küçük altın yıldız. Maskot üst kenardan çıkar. Etiket canlı TMP/Loc, beyaz yaklaşık 45 px. Sayaç altın sarısı, kalın, yaklaşık 64 px. Görseldeki 4 örnek. Maskot oynarken sayı ve butonlar sabit.
3. **Yıldız:** krem/altın dış gövde, koyu mor oyuk, sarı dolgu ve üç ayrı eşik yıldızı. Mevcut puan, eşik, dolum ve kazanım animasyonunu bağla. Eşikler örnek PNG'deki mesafeye göre puanlama değiştirmez.
4. **Hedefler:** geniş krem kabuk, içte soluk krem yuvalar ve ince dikey ayraçlar. Her hedef mevcut simge ve canlı ilerleme sayısı. Dört hedef PNG'de örnek. Beş veya daha fazla hedef ve dar ekran için mevcut düzenin kapasitesini koru. Gerekiyorsa ikinci satır kullanıp HUD yüksekliğini hesapla. Sayıları kesme, hedefleri gizleme ve tahtayla çakıştırma.
5. **Tahta:** krem iç tepsi, sarı-altın ince kontur ve aşağı yönlü yumuşak gölge. Kontur dekoratif. Hücreler, gerçek boşluklar ve yığınlar mevcut görselleriyle okunaklı kalır. Taşın parlaklığını değiştirerek çerçeveyi dengeleme. Referansta üstte görünen boş yuvaları üretmek için matrise hücre ekleme.
6. **Alt kabuk:** geniş dalgalı krem/altın gövde ve mor dairesel yuvalar. Sadece o bölümde mevcut ve kullanılabilir gerçek kontrol/tepsi yuvalarına bağla. Etkin, seçili ve devre dışı durumlar ayırt edilir. Boş dekoratif alan etkileşim yakalamaz. Sistem hareket çubuğu üstünde yeterli boşluk bırak.

## Ayrı görseller

Paket tam ekran referansı ve talimatı içerir. Tam ekranı tek PNG olarak Unity'ye yerleştirme. Arka plan, hamle kurdelesi, yapraklar, yıldız kabuğu/dolgusu, hedef kabuğu, tahta kenarı ve alt kabuk için önce mevcut yazısız varlıkları envanterle. Eksik final parçaları dosya adı ve kullanım ölçüsüyle raporla. Yazısız alfa PNG veya vektör kaynak kullan. Dinamik metin ve sayılar mevcut yerelleştirme üzerinden gelir. Geçici görseli final kabul etme.

## Doğrulama ve teslim

- Değişiklikten önce aynı kayıt, bölüm, animasyon durumu ve ekran boyutunda Unity görüntüsü al. Mevcut dört ana taş, en az bir özel taş/kaplama ve soyulma karesi için yakın plan karşılaştırması yap. Sadece zemin/çerçeve ve HUD yerleşimi farklı olabilir. Taşların kaynak dosyalarında, shader'larında, atlaslarında ve bölüm JSON'larında fark olmadığını doğrula.
- **941×1672**, **1080×1920** ve dar çentikli telefon görüntüsü al. Dar telefonda en az beş hedef, farklı bir tahta şekli ve aktif yetenek tepsisini kontrol et. Gerçek veriye Hamle 4 veya referans hedef sayıları yazma.
- Duraklatma, üst geri al, mevcut özel güç/tepsi etkileşimleri, hamle düşümü, yıldız artışı, hedef tamamlanması, maskot ve tahta dokunma hizasını doğrula. Hedef veya şerit altında taş tıklaması kaybolmamalı.
- Harita ve Koleksiyon ekranlarını önce/sonra karşılaştır. Derleme ve ilgili test sonucunu, değişen dosyaları ve referansla kalan farkları raporla. Görsel benzerlik için çalışan oyun davranışını değiştirme.
