# Ortak parçaların iç yerleşimleri

Bu dosya dış kutu içindeki görsel ve yazının kesin konumlarını tamamlar. JSON kaynağı: bilesenler/ortak-bilesenler.json. Bütün koordinatlar ilgili bileşenin sol üstünden x,y,w,h; renk adları tasarim-tokenlari.json içindeki HEX değerlerine karşılık gelir.

| Parça | Dış ölçü | Görsel / ikon | Yazı alanı ve rol | İç boşluk / durum |
|---|---|---|---|---|
| Alt menü öğesi | 296×176 | 104,8,88,88 | 16,108,264,48; nav32/800 | Seçili kapsül28,100,240,64; diğer öğelerin arka planı saydam |
| Hedef kapsülü | 240×104 başlangıç | 16,16,72,72 | 104,16,120,72; goal38/800 | Değişken genişlik formülü JSON'da; kapsüller arası16 |
| Kural satırı | 748×160 başlangıç | 20,36,88,88 | 132,20,592,120; body36/600 | Gerçek preferred height+40; satırlar arası16 |
| Ayar satırı | 748×132 | 0,30,72,72 | 104,30,396,72; rowTitle38/800 | Toggle572,22,176,88; thumb72; track kenar2 |
| Sıralama satırı | 936×104 | Baş harf avatar96,20,64,64 | Ad184,8,484,48; ayrıntı184,56,484,36; puan724,20,192,64 | Sayı ve puan sağa/ilgili hizaya; gerçek durumlar korunur |
| ZPE bileşeni | 748×88 | 0,16,56,56 | Ad76,10,412,36; değer648,12,100,56 | Çubuk76,56,548,14; değerin alanına girmez |
| Koleksiyon kartı | 468×484 | 64,24,340,252; kilit190,106,88,88 | Ad24,292,420,56; açıklama24,360,420,100 | Kilitli durum mevcut bulanık PNG; yeni blur filtresi yok |
| Sonraki bölüm | 668×300 | Canlı mini tahta220,24,352,252 | Label24,24,172,240; rowSub28/600 | Ok596,118,48,64; gerçek kaynak düğmesi varsa etkileşim |
| Başlık plakası | 784×112 | Metinsiz yüzey | 32,20,720,72; pageTitle56/800 | Kenar3brass; gölge aşağı6 blur12 alpha0.20 |
| Öğretici kutusu | 888×200 başlangıç | Maskot16,36,128,144 | 168,24,672,152; body36/600 | Ekran kenarından32; delik/ok kaynak kümeden |
| Bilgi bandı | 920×96 | Yeni dekor yok | Insets28,20,28,20; bodyCentered36/600 | Sadece mevcut toast içeriği ve süresi |
| Güç alanı | 888×160 | Her gerçek simge144×144 | Sadece mevcut caption | En fazla4; gap48; grup merkezde; yeni altlık/yuva yok |

## Metin ve durum ayrıntıları

Satır listelerinin puan/ikon/sayaçları sağa hizalıdır; isim/açıklama alanı soldan sabittir. Görsel kutu büyürken yazı alanının anchor'u değişmez. SDF kontur gövdeye eklenmez. Sekme seçimi yalnız görsel dolgu değiştirir; sekmelerin tıklama eylemleri aynı kalır. Modal içindeki bilgi kartları birbirinin içinde ikinci bir ağır çerçeve oluşturmaz: yalnız sand2px ve radius28 kullanır.

Beşli hedef satırı984px içine sığmadığında önce goal fontu38 olarak korunur, kapsül yataypadding16, ikon64, gap12 olur. Hâlâ sığmıyorsa mevcut hedef alanı iki satıra açılır; ikinci satır toplam alanı104+16 kadar büyütür, tahta zarfının üst sınırı bu artış kadar aşağı kayar. Tahta hücreleri yeniden düzenlenmez. Genel tahta zarfı kısa kalırsa mevcut kamera/fit davranışı ile aynı kontur ölçeklenir; pitch verisi değişmez. Tahtanın fiziksel hit alanı ve kamera birlikte aynı dönüşümü kullanır.

Başlıklarda maksimum satır sayısı bir varsayılan tasarım sınırıdır; gerçek çeviri taşarsa iki satıra genişler, son kelimeler gizlenmez. En küçük açıklama26px; daha küçük metinle sıkıştırmak yasaktır.
