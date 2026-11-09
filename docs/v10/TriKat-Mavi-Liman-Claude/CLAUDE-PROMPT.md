# TriKat · Mavi Liman üst başlık ve alt menü

## Claude Code görevi

KatKat Unity projesinin kökünde çalış. Bu belgeyle aynı klasördeki **01-onayli-referans.png** dosyasını aç ve ekranı parça parça karşılaştır. Onaylanan kapsam, harita olarak kullanılan ana sayfanın **sabit üst başlığı ve dört sekmeli alt menüsü**. Harita manzarası, yol, bulutlar, taş düğümleri ve maskot ekran bağlamıdır. Mevcut çalışan harita içeriğini, oyun içi HUD'ı, bölüm dosyalarını ve kullanıcı değişikliklerini koru.

### Önce mevcut akışı bul

- `Assets/KatKat/Scripts/Game/GameController.cs` içindeki `ShowHome`, `UseMapAsHome`, `ShowLevels` ve `ShowSettings` akışlarını kontrol et. Ana ekran olarak gösterilen haritayı `MenuView.ShowLevels` kuruyor. Yalnız eski ayrı `ShowHome` ekranını değiştirip görevi tamamlanmış sayma.
- `Assets/KatKat/Scripts/UI/MenuView.cs` içinde `HeaderCard`, `Tabs`, `SideTab`, `TopGear` ve harita başlığını yenileyen çağrıyı takip et. Üst ve alt kabuk harita kaydırma içeriğinin dışında, mevcut safe area sistemiyle kurulmalı.
- `MenuView.Meta.cs` ve `GameController` partial dosyalarında Günlük/Taşlar dönüş yollarını kontrol et. Mevcut `DailyPressed` ve `CollectionPressed` çağrılarını kullan. İncelenen alt menü üç sekmeli. Ödüller için doğrulanmış mevcut hedef bulunamadı. Varsa gerçek ekrana bağla. Yoksa yeni ödül ekonomisi veya sahte kazanım tasarlama. Dördüncü yuvayı görsel önizlemede göster, üretim etkileşimini açık iş olarak raporla.

## Ölçü kılavuzu

Referans **940 × 1673 px**, başlangıç noktası sol üst. Kutular yaklaşık görsel sınırdır ve parlama/gölgeyi içerebilir. Tasarımı 1080 birim genişliğe taşıyorsan x ve genişlikleri 1080/940 ile ölçekle. Dikey konumları ilgili kenardan al. Cihazda gerçek güvenli alanı esas al. Referans cihazda görünür öğelerin merkezleri hedefe yaklaşık ±8 px içinde kalmalı. Taşma veya metin çakışmasında okunabilirliğe öncelik ver.

| Öğe | Referans x | Referans y | Genişlik × yükseklik |
|---|---:|---:|---:|
| Mor bulutlu üst sahne | 0 | 0 | 940 × 252 |
| TriKat logosunun görünür alanı | 252 | 10 | 430 × 116 |
| Deniz feneri ve kayalık küçük sahnesi | 12 | 16 | 230 × 219 |
| Ayarlar dairesi | 825 | 11 | 101 × 99 |
| Mor yıldız kapsülü | 716 | 128 | 196 × 80 |
| Altın dalgalı üst şerit | 0 | 152 | 940 × 141 |
| Ortadaki turkuaz bölge etiketi | 375 | 140 | 188 × 49 |
| Bölge başlığı metin kutusu | 336 | 196 | 268 × 46 |
| Bölüm aralığı metin kutusu | 391 | 245 | 158 × 29 |
| Alt kabuğun görünür alanı | 0 | 1480 | 940 × 193 |

Alt ikon merkezleri yaklaşık **x 145, 369, 583, 805** ve **y 1524**. Etiketlerin orta hattı **y 1592**. En alttan ölçüldüğünde kabuk yaklaşık 193 px, ikon merkezi 149 px, etiket merkezi 81 px yukarıda. Ayırıcılar yaklaşık x 265, 473 ve 692. Sekmelerin dokunma alanları simge ve yazıyı birlikte kapsamalı.

## Üst başlık · Katman sırası

1. Mor bulut sahnesi arka planda. Üst sınır ekran kenarına kadar uzanabilir, yalnız dekor güvenli alan dışına taşabilir.
2. Sol deniz feneri tematik bir illüstrasyon. Ortadaki **TriKat** için mevcut `Resources/PremiumV4/ui/logo-trikat.png` dosyasının görünümünü referansla karşılaştır ve onaylıysa kullan. Şeffaf paya göre görünür alanı ölç. Logo ve fener en-boy oranlarını koru.
3. Önde soldan sağa kesintisiz altın dalgalı şerit. Uçlar yukarı kıvrılır. Ortada geniş krem oyuk, iki yanda ayrı altın defne motifi, altta sıcak turuncu kenar ve kısa yumuşak gölge. Düz kapsül görünümü bu referansı karşılamaz.
4. Ortada parlak turkuaz bölge etiketi. Altında **Mavi Liman** koyu lacivert, altında **1–100** koyu amber. 940 px referansta sırasıyla yaklaşık 26, 41 ve 26 px görünür yazı ölçeği. Mevcut `UiFactory` ve TMP font ailesini kullan. Kalın/yuvarlak başlık, aralık daha hafif. Yerelleştirmede sığdırmayı etkinleştir, metin çerçeveyle kesişmesin.
5. Sağ üstte turkuaz gövdeli, altın çerçeveli ve krem dişli **Ayarlar**. Altında ayrı mor kapsül, altın yıldız ve beyaz toplam sayı. **12** örnek veridir. `StarsOf` ile mevcut toplamı göster. `BÖLGE 1`, bölge adı ve aralık da görünen bölgeden dinamik hesaplanmalı. Harita kayınca bölge güncellensin, yıldız toplamı bölgeye göre sıfırlanmasın.
6. Harita bulutları şeridin arkasında kalır. Haritadaki numara ve maskot üst başlığın üzerine çıkmamalı. Görsel dekorların `raycastTarget` değeri kapalı olsun.

## Alt menü · Dört sekme

- Sıra **Harita · Günlük · Taşlar · Ödüller**. Harita açılışta seçili. Ayarlar yalnız üstte. Bölümler mevcut harita düğümleriyle açılır.
- Alt gövde tek parça kıvrımlı krem şerit. Altın kenar, sıcak alt bevel, üç ince ayraç. Simge ve yazı ayrı UI elemanları. Simgeler üst kenardan hafifçe taşabilir, ekran veya güvenli alan tarafından kırpılmamalı.
- Simgeler referanstaki sırayla harita ve kırmızı pin, kırmızı üstlü takvim, mavi kristal, kırmızı-altın sandık. Seçili sekmenin etiket arkasında turkuaz kapsül ve krem yazı. Pasif yazılar koyu lacivert. Görsel referansta etiketler yaklaşık 32–34 px.
- Dört eşit dokunma alanı kur. Bir tıklama bir kez çalışsın. Sayfa değişince seçili durum güncellensin. Aynı sekmeye yeniden basmak sayfayı yığmamalı. Haritaya dönüş mevcut odak davranışını korusun.
- Dekor en alta kadar uzayabilir. Dokunulan sekmeler ve etiketler sistem hareket çubuğunun üstünde kalmalı. Alt ve üst dekorun şeffaf alanı harita kaydırmasını engellememeli. Alt kabuğun arkasındaki görünmez düğümler tıklanmamalı.

## Varlık kontrolü ve uygulama sırası

1. Önce mevcut logoyu, bölge illüstrasyonlarını, dişliyi, yıldızı ve dört sekme ikonunu envanterle. Tam ekran PNG, **görsel doğrulama referansıdır**. Dinamik arayüzde bunu arka plan olarak kullanma. Metinler, rakamlar, logo, fener, bulut, kapsüller, defneler, ikonlar ve iki dalgalı kabuk ayrı katmanlar olmalı.
2. Eksik olabilecek parçalar için isim ve dosya hedefi bildir. Özellikle yazısız şeffaf üst kabuk, yazısız şeffaf alt kabuk, mor bulut plağı, fener ve sandık ikonu. Bu pakette kaynak sprite'lar ayrıca üretilmedi. Eksik final parçayı geçici görselle yerleştirebilirsin, fakat geçici olanı final kabul etme.
3. Dalgalı kabuklarda referans konturunu koru. Tek bir kapsül sprite'ını yatay esneterek benzerlik sağlamaya çalışma. Farklı oranlar için sol, orta ve sağ parça ya da konturu koruyan ölçeklendirme kullan. Simgeleri `preserveAspect` ile yerleştir. Yazısız kaynaklar alfa kanallı PNG, tercihen 2× referans çizim ölçüsünde veya vektör kaynak olsun. Metinler runtime ve `Loc` üzerinden gelsin.
4. Önce haritanın mevcut görünümünü aynı kayıt ve çözünürlükte yakala. Sonra yalnız kabuğu ve bağlarını değiştir. Günlük ve Taşlar ekranlarında ortak alt menüye etkisini de test et.

## Kabul ve teslim

- **940×1673** karşılaştırma görüntüsü, **1080×1920** normal telefon ve **dar çentikli telefon** görüntüsü. Referans durumuna yaklaşmak için geçici test kaydında 1–100 bölgesi, aktif bölüm 5 ve toplam 12 yıldız kullan. Canlı kayıt verisine bu değerleri yazma.
- Üst ve alt parçaların sabit kaldığı bir kaydırma kontrolü yap. Açık düğümler numaralı, açılmamış düğümler mevcut numarasız görünümünü korur. En az iki bölgede başlık güncellemesini doğrula.
- Ayarlar, Harita, Günlük, Taşlar ve varsa gerçek Ödüller ekranının tıklama ve geri dönüş kontrolü. En az bir uzun yerelleştirilmiş etiket, bir büyük yıldız sayısı ve güvenli alan kontrolü.
- Derleme sonucunu, değişen dosyaları, ekran görüntülerini ve referanstan kalan farkları ver. Eksik sprite, bağlanmamış Ödüller veya görünür hizalama farkı varsa bunu açık yaz. Mevcut oyun ekranı ve bölüm verilerinde değişiklik olmamalı.
