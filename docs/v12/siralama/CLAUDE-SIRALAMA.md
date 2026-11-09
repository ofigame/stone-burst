# TriKat v12 — Sıralama ekranı görsel parçaları

## Kapsam
Yeni sıralama ekranı için görsel paket. Oyun kodu değiştirilmedi. `00-onayli-referans.png` kullanıcının seçtiği tasarımdır; `01-parcalar-onizleme.jpg` parçaları birlikte gösterir. PNG parçaları referans üzerinden bağımsız yeniden üretilmiştir, orijinal katmanlı kaynak dosya değildir; birebir piksel eşleşmesi garanti edilmez. Tam referansı ekran arka planı yapıp üzerine etkileşim ekleme.

Mevcut `docs/v12/PLAN.md` korunur. Buradaki görsel teslimi oradaki reklam, can, ZPE veya bölüm sistemlerine uygulama izni vermez. Harita yolu, mevcut taş modelleri ve diğer ekranlar bu iş kapsamında değişmez.

## Dosyalar
`arkaplan/01-arkaplan.png`: yazısız ve arayüzsüz deniz/gökyüzü; ekranı doldur, oranı koruyarak kırp.

`png/02-liste-paneli.png`: ilk 10 listesinin inci beyazı dış kabuğu. İç alanında da alfa bulunduğu için arkasına, çerçevenin sınırları içinde kalan düz #F7F8F8 UI Image dolgusu eklenmeli. Böylece deniz arka planı liste içinden görünmez. Çerçeve dosyasını tek başına opak bir panel kabul etme.
`png/03-oyuncu-paneli.png`: ekran altında sabit kendi sıram kartı.
`png/04-sekme-secili.png`, `05-sekme-normal.png`: seçili ve pasif sekme zeminleri. İki sekme aynı ölçüde olmalı.
`png/06-satir-mavi.png`, `07-satir-beyaz.png`: tekrarlanan boş satır zeminleri.
`png/08-altin-podyum.png`, `09-gumus-podyum.png`, `10-bronz-podyum.png`: isim, avatar ve puanı dinamik eklenecek boş podyum tabanları.
`png/11-geri-butonu.png`, `12-bilgi-butonu.png`: geri ve bilgi simgeleri dahil buton görselleri.
`png/13-kupa.png`: başlığın yanındaki kupa.
`png/14-maskot.png`: kendi sıra kartının üstünden bakan maskot; statik PNG, animasyon dosyası değildir.
`png/15-baslik-kapsulu.png`: başlık ve kendi sıra etiketi için boş inci kapsül.
`png/16-metrik-kapsulu.png`: alt karttaki üç veri için turkuaz kapsül.

`png/17-avatar-cercevesi.png`: ortası şeffaf altın avatar halkası; avatar resmi ayrı dairesel maskenin içinde kalır.
`png/18-madalya.png`: numarasız altın madalya, sıra numarası ayrı TMP metnidir.
`png/19-sira-rozet.png`: normal sıralar için numarasız turkuaz yuvarlak rozet.

Logo, mevcut oyuncu avatarları, ülke bayrakları ve yıldız simgesi mevcut oyundaki varlıklardan kullanılmalı. Referanstaki örnek insan avatarları yeni zorunlu karakter tasarımları değildir. Sıra rakamları ve madalya numaraları metin bileşenleridir. Gümüş/bronz avatar halkaları ve madalyalar bu pakette ayrı sprite değildir; mevcut uygun varlıklar varsa onları kullan, yoksa ayrı materyal renklendirmesiyle referansa yaklaş ve görüntüyü kontrol et. Altın görsele düz mavi tint basıp gümüş elde edildiğini varsayma.

## Yerleşim
Referans 940×1672 oranındadır. Canvas Scaler referansı 1080×1920; Match 0.5. SafeArea içinde yerleştir. Dikey bölgeler yaklaşık: başlık %0–7, sekmeler %7–13, podyum %13–30, liste %30–81, oyuncu kartı %83–99. Bu oranlar referansa göre başlangıç rehberidir; farklı ekranlarda ölçülerek ayarlanır.

Üstte geri butonu sol, mevcut TriKat logosu ve Sıralama başlığı orta, bilgi butonu sağ. Sekmeler Dünya ve oyuncunun ülkesi; Türkiye yalnız örnek. Bölge, Mavi Liman gibi oyun dünyasını ifade etmez.

Podyum solda ikinci, ortada birinci, sağda üçüncü. Birinci daha yüksekte. Podyum bilgileri listenin ilk üç kaydıyla aynı veri kaynağından gelmeli. İlk 10 listesinde 1–10 eksiksiz bulunmalı. Her satırda sıra, avatar, ad, küçük ZPE/Bölüm/Yıldız değerleri ve sağda büyük sıralama puanı. Alttaki kart liste kaydırılırken sabit kalır. Oyuncu ilk 10 içindeyse de kendi kartı aynı gerçek sırayı gösterir.

Dar ve kısa ekranlarda metni okunamaz ölçüye küçültmek yerine liste alanı dikey kaydırılabilir; kendi kartı ve sekmeler sabit. 10 kayıt veri setinde bulunmalı. İsim tek satır ve taşarsa üç nokta; puan alanını ezmemeli.

## Metin ve veri
Tüm başlıklar, ülke adı, oyuncu adları, sıra ve sayılar TextMeshPro ile dinamik olmalı; PNG üzerine yazı ekleme. Mevcut lisanslı yuvarlak font tercih edilmeli, Türkçe karakter desteği doğrulanmalı. Tasarım referansında başlık kalın lacivert; isim ve puan kalın, ikincil metin koyu gri-mavi. Ana renk #073A68, ikincil #50677A başlangıç değeridir.

1080 referans genişliğinde başlangıç boyutları: başlık 48–56 px, sekme 38–44 px, satır adı 30–34 px, puan 36–42 px, ikincil değer 22–26 px, kendi puanı 48–56 px. Yerelleştirme ve cihaz kontrolüyle netleştir. Sayılar locale'a uygun biçimlenmeli.

Referanstaki puanlar temsilidir. ZPE, ulaşılan bölüm ve yıldızdan türeyen ana değer `Sıralama puanı`dır. Yıldız toplamıyla karışmaması için ana puanda kupa simgesi kullan; yıldız simgesi yalnız yıldız metriğinde kalsın. Formül bu PNG paketiyle kesinleşmez: PLAN.md içindeki ağırlıklı öneri ile sohbetteki eşit normalize ortalama önerisi farklıdır; mevcut formülü sessizce değiştirme. Sunucu sıralama verisi esas alınır; görseldeki örnek kayıtlar canlı veri değildir.

## Unity içe aktarma ve ölçekleme
PNG'ler kaynak çözünürlükte korunur. Gerçek en-boy, alfa ve üretim istemleri VARLIKLAR.json içindedir. Sprite (2D and UI), sRGB açık, UI için mipmap kapalı, Bilinear, Clamp, Alpha Is Transparency açık. Max Size kaynak uzun kenarını gereksiz düşürmesin: 2172 px için 4096. İlk kalite kontrolünde sıkıştırma kapalı; cihaz belleği ölçülerek sonradan optimize edilir.

Görünür şeklin dışında şeffaf boşluk olabilir. RectTransform'a oturturken texture tamamına değil görünür alfa sınırına bak. Podyum ve maskotta Preserve Aspect kullan; bunları 9-slice yapma. Kapsül/satır/çerçevede 9-slice sınırları Sprite Editor'da gerçek köşeler incelenerek ayarlanmalı; otomatik tahmini border ile köşeler bozulmamalı. Şeffaf dış boşluğu keserken kenar yumuşatmasını koru.

## Etkileşim ve hafif animasyon
Sekme/buton basışında 0.96 ölçek, 80 ms küçülme ve 120 ms dönüş. Sekme seçimi 160 ms renk/görünüm geçişi. Ekran ilk girişinde 180 ms yumuşak görünme; her yenilemede bütün satırları zıplatma. Maskot statik görseli 2.8 saniyelik +/-3 px hafif salınımla kullanılabilir. Göz hareketi, göz kırpma ve el hareketi bu PNG'de ayrı katman olmadığı için uygulanmış sayılmaz; ayrı sprite/rig gerekir. Azaltılmış hareket seçeneğinde salınımı kapat. Sürekli yanıp sönen parıltı veya tekrar eden ses kullanma.

## Kontrol
Ülke değiştirilince liste ve kendi sıra kartı birlikte güncellenir. Yükleniyor, bağlantı hatası/yeniden dene, henüz sıralama yok ve oyuncunun sırası henüz oluşmadı durumları ayrı metin durumlarıdır. Sahte örnek veri ile hata gizleme. Oyuncu sırası yoksa 0 yazma; 'Henüz sıralaman yok' göster. Ülke verisi bulunmuyorsa ülke uydurma. Dünya listesi kullanılabilir kalsın.

Referansla ekran görüntüsü karşılaştırmasında köşe biçimleri, beyaz/cyan malzeme, boşluklar, podyum sırası, ilk 10, ülke kapsamı ve sabit oyuncu kartı kontrol edilmeli. Mevcut taş/harita tasarımının değişmediği ayrıca doğrulanmalı.
