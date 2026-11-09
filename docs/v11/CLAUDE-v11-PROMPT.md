# TriKat v11 · Seçilen mavi-beyaz ana ekran ve özel taş simgeleri

## Görev ve tasarım kaynağı

Bu klasördeki **01-secilen-ana-ekran.png**, üç tasarım arasından seçilen **ilk mavi-beyaz oyuncak tasarımıdır**. Eski mor-altın kabukları esas alma. Yeni PNG'leri mevcut Unity arayüzüne bağla. Aynı ekran görüntüsünü tek parça oyun arka planı olarak kullanma. Gerçek harita, düğümler ve maskot mevcut sistemde kalır.

Üstte mavi zemin üzerinde mevcut TriKat logosu, krem yıldız kapsülü ve mevcut ayarlar düğmesi; altında beyaz-mavi bölge kartı. Altta ekran kenarına kadar uzanan düz mavi menü, seçili Harita için krem kart. Bölge ve alt menü arasında sağ/sol çerçevesi olmayan mevcut bölüm haritası görünür. Üst ve alt panellerin kısa gölgeleri orta haritanın içeride olduğu hissini verir.

## Ayrı görseller

| Dosya | Kullanımı |
|---|---|
| arayuz/01-mavi-panel-zemini.png | Yazısız mavi zemin. Üst ve alt panelde uygun ölçek/kırpma ile kullan. Alt menü sol, sağ ve alt ekran kenarına kadar ulaşır. |
| arayuz/02-bolge-beyaz-kabuk.png | Bölge kartının beyaz yüzeyi, turkuaz alt kenarı ve gölgesi. Fener, başlık ve bölge kapsülü bunun önünde ayrı çizilir. |
| arayuz/03-bolge-turkuaz-kapsul.png | Bölge numarası ve aralık için boş kapsül. Metni canlı Loc/TMP üzerinden koy. |
| arayuz/04-yildiz-krem-kapsul.png | Mevcut yıldız simgesi ve gerçek toplam için boş krem kapsül. |
| arayuz/05-secili-menu-karti.png | O an seçilen sekmenin ikon ve etiket grubunun arkasındaki krem kart. Aktif sekmeyle birlikte yer değiştirir. |
| skiller/01-renk-temizleyici.png | Mevcut bir renk seçilip eşleşen taşların temizlendiği özel güç için yeni görsel. |
| skiller/02-yatay-temizleyici.png | Mevcut yatay sıra temizleyici, sol-sağ yönünü gösterir. |
| skiller/03-dikey-temizleyici.png | Mevcut dikey sütun temizleyici, yukarı-aşağı yönünü gösterir. |

Mevcut logo, fener, maskot ve dört menü ikonunu kullan. Aynı ikon için birden fazla kalitesiz vekil çizim yapma. Kaynakların alfa sınırlarını ve en-boy oranını incele. Görünür ölçüyü boş PNG payıyla karıştırma. Şeffaf parçalar Sprite (2D and UI), sRGB, alpha açık. Son karşılaştırmada yüksek kaliteli sıkıştırma kullan. Arayüz kaynaklarının uzun kenarı 2048 üstündeyse max size 4096 seç. Özel taş simgeleri 1024×1024, merkez pivotludur.

## Ana ekran uygulaması

Mevcut GameController.ShowHome → ShowLevels ve MenuView.ShowLevels akışını doğrula. HeaderCard, Tabs ve SideTab çağrılarını yeni kabuğa bağla. Ana sayfa olarak görülen gerçek haritayı güncelle. Bölge kartı örnek ekranda solunda fener, ortasında Mavi Liman, sağında BÖLGE 1 • 1–100 kapsülü gösterir. Bu metinler kayıt ve kaydırılan bölgeden canlı gelir. Yıldız 12 örnektir.

- Alt menü düz ve alttan bağlıdır. Alt bordür, dalgalı uç, boşluk veya menünün altında başka şerit bulunmaz. İkon ve metinler güvenli alanın üstünde kalır; mavi zemin cihazın gerçek alt kenarına uzanır.
- Ayırıcıları PNG'ye gömme. Menüde N görünür öğe ve W iç genişlik varsa i=1..N-1 sınırlarını x0+i*W/N ile koddan oluştur. Seçilen örnekte ayırıcı görünmüyor. Üretimde ayırıcı gerekiyorsa çok ince ve düşük kontrastlı olsun. N=1'de çizgi yok, N=0 güvenli boş durum. Menü yeniden çiziminde birikme yok.
- Görseldeki dört sekme Harita, Günlük, Taşlar, Ödüller. Mevcut ekran bağlantılarını koru. Eksik hedefi sahte veri veya yeni ekonomiyle doldurma; açık iş olarak raporla.
- Harita yolu, arka planı, bulutları, düğüm konumları, numaraları, scroll davranışı ve bölüm dosyaları korunur. Haritaya yan çerçeve ekleme. Koleksiyon ve oyun içi HUD bu ana ekran kabuğu değişikliğinden etkilenmemeli.

## Özel güç görsellerinin bağlanması

Bu üç PNG mevcut özel güçler için görsel yenilemedir. Dört normal opal taşın sprite, shader, malzeme, sembol, renk, kalınlık ve soyulması korunur. Güncel kodda SpecialArt.For, SpecialKind.Rainbow / RocketRow / RocketColumn ve TileSkin.SpecialBox / SpecialOffset / SpecialAngle bağlantılarını incele. İsimler mevcut sürümde değişmişse gerçek çağrıları izleyerek eşle.

1. Renk temizleyici, dokunulan/seçilen rengin ilgili bütün taşlarına mevcut motorun uyguladığı etkiyi korur. Üç küçük renk öğesi seçim yeteneğini anlatır. Dört rengi aynı anda patlatan yeni bir kurala dönüştürme. Mevcut kayıt kimliğini, üretim koşulunu ve tüketim davranışını değiştirme.
2. Yatay ve dikey simgeler yönü kendi görsellerinde içerir. Dikey PNG'ye ayrıca 90 derece dönüş uygulayıp yatay hale getirme. İki yönün etkisi mevcut motorla aynı kalır.
3. Simge üst taşın gerçek yüzüne oturur. Yatay simgenin görünür kutusu yüz genişliğinin yaklaşık %85'i, yüz yüksekliğinin yaklaşık %30'u; dikeyde bu oranlar yer değiştirir. Renk simgesi yüzün yaklaşık %70'i. Bunlar başlangıç yerleşim değerleri, son oranı normal taş üstünde ve tepsi yuvasında görsel olarak doğrula. Normal taş geometrisini bu simgelere uydurmak için değiştirme.
4. Kaynak saydam payını hesaba katan mevcut Fit/Trim yolunu yeniden kullan. Simgeler taş yüzünün merkeziyle hizalanır. Mevcut eski köşe/bant offset'i yeni merkezli görsele doğrudan uygulanmaz. Yalnız özel taşın sunum ölçüleri güncellenebilir.
5. Oyun tahtası, kazanılan özel güç yuvası, varsa eğitim gösterimi ve özel efektte kullanılan simge aynı kaynaktan gelsin. Aynı aktif nesnede eski simgeyle yenisi birlikte görünmesin. Yeni simgeler bir kez yüklenip önbelleğe alınır. Mevcut hareket/soyulma/özel güç çalıştırma zamanı ve hedefleme davranışı korunur.

## Sesler

ses/ klasöründeki dört WAV v11 paketine de taşındı.

- 01-anasayfa-opal-esinti-loop.wav haritada düşük düzeyde kesintisiz 32 sn döngü. Tek müzik kaynağı, harita yeniden çizilince veya bölge kayınca baştan başlamaz. Bölüme girişte yumuşak kapanır.
- 02-tek-tas-yumusak-pop.wav mevcut ilk normal soyulma dalgasında bir kez, taş başına değil.
- 03-zincir-dort-tas.wav dosyası zaten dört yükselen nota içerir. Hamlenin ilk otomatik zincir dalgasında bir kez çal. Sonraki zincirlerde üst üste yığma, mevcut pitch artışını ekleme.
- 04-bolum-sonu-odul.wav normal zafer kartı ilk gösterildiğinde bir kez çal.
- SoundBank ve mevcut ses ayarıyla bağla. Ayarı kapatmak müzik ve efektleri anında susturur. Arka planda müzik duraklar, öne gelince yalnız ilgili menüde ve ses açıksa devam eder. Mevcut WAV'ları yeniden normalize edip yükseltme.

## Teslim kontrolü

Ana ekranı aynı kayıtla önce/sonra, 941×1672 referans oranı, 1080×1920 ve dar çentikli telefonda doğrula. Üç/dört/beş menü öğesiyle yerleşimi kontrol et, üretime yeni sekme ekleme. Normal bölüm JSON'larında fark oluşmamalı.

Her özel taşın normal yoldan oluşmasını, tepsiye gelmesini, hedeflenmesini ve çalıştırılmasını test et. Yatay yalnız satıra, dikey yalnız sütuna mevcut etkiyi uygular. Renk simgesi seçili renk üzerinden çalışır. Eski motorun katman/engel ve sınır davranışı aynı kalır. En az bir özel güç birleşimi ve geri alma senaryosunu doğrula. Normal taş ve özel taş yakın plan ekran görüntüleri al.

Eksik arayüz bağlantılarını ve görsel farkları açıkça raporla. PNG'ler referansa göre ayrı varlıklar halinde yeniden üretildi. En son Unity ekranı ile referansın ölçü ve malzeme uyumu kontrol edilmeden birebir sonuç iddiasında bulunma.

Ek görsel. arayuz/06-taslar-mavi-elmas.png, Taşlar sekmesinin mavi elmas ikonudur. 1024×1024 şeffaf PNG, merkez pivot. Menüde mevcut Taşlar simgesinin yerine aspect oranını koruyarak yerleştir. Diğer sekme ikonlarıyla görünür yüksekliğini eşitle.
