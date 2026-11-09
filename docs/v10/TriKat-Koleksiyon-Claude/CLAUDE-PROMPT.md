# TriKat v10 · Koleksiyon ekranı

Bu belgeyi ve yanındaki **01-onayli-referans.png** görselini okuyarak mevcut Koleksiyon ekranının görünümünü uygula. Çalışma kapsamı Taşlar sekmesinde açılan Koleksiyon sayfasıdır. Harita yolu, arka planı, düğümleri, kaydırması ve mevcut üst başlığı kapsam dışıdır. Oyun içi HUD, bölüm dosyaları, taş kazanım koşulları ve güç davranışları korunur.

## Unity bağlantıları

- Açılış yolu `Assets/KatKat/Scripts/Game/GameController.Meta.cs` → `ShowCollection()` → `Assets/KatKat/Scripts/UI/MenuView.Meta.cs` içindeki `ShowCollection(...)`. Mevcut `StoneItem` verisini yeniden kullan. Genel `MetaPage` görünümünü değiştirerek Günlük ekranını etkileme. Gereken kabuğu Koleksiyon sayfasında oluştur.
- `GameController.Meta.cs` mevcut üç kategoriyi ve sahiplik/gizlilik durumlarını sağlıyor. İçerik kaynağı `Assets/KatKat/Resources/Collection/stones.json`, mevcut görseller `Resources/Collection/stones/`. Önce bunları envanterle. İsim, açıklama, kilit eşiği, güç, sıralama ve sayılar katalog ve kayıt üzerinden gelsin.
- `Tabs` ortak menü noktasıdır. v10 içindeki `TriKat-Mavi-Liman-Claude` paketiyle uyumlu olarak Harita, Günlük, Taşlar, Ödüller sırasını koru. Koleksiyon ekranında Taşlar seçili. Bu görev sırasında Harita sayfasının görünümüne dokunma. Ortak bar hazır değilse Koleksiyon varyantını mevcut navigasyon bağlarıyla sınırlı kur. Ödüller için mevcut işlevi bulup bağla. İşlev doğrulanamıyorsa sahte ödül akışı oluşturma ve eksik bağı raporla.
- Sol üst geri oku mevcut `back`, sağ üst dişli mevcut `settings` aksiyonunu çağırır. Bir tıklama bir aksiyon. Aynı sekmeye basmak kopya sayfa açmaz. Görsel katmanlar kaydırma ve tıklamaları engellemez.

## Ölçü ve katman sırası

Referans **941×1672 px**, koordinat başlangıcı sol üst. Aşağıdaki kutular görselden yaklaşık okunmuştur. 1080 tasarım genişliğinde x ve genişlikleri 1080/941 ile ölçekle. Sabit üst/alt parçaları kendi kenarlarına bağla. Kart bölgesinin yüksekliğini gerçek güvenli alan ve kabuk boyutlarından hesapla.

| Parça | x | y | Genişlik × yükseklik |
|---|---:|---:|---:|
| Sol geri düğmesi | 24 | 20 | 96×96 |
| Sağ ayarlar düğmesi | 821 | 20 | 96×96 |
| Açık sandık ve kristaller | 287 | 6 | 354×165 |
| Mor Koleksiyon kurdelesi | 183 | 142 | 575×111 |
| Dış altın çerçeve, tepe kategorisi hariç | 13 | 304 | 915×1158 |
| Yıldız Taşları kapsülü | 30 | 257 | 455×94 |
| Sahip olunan/toplam kapsülü | 690 | 265 | 198×85 |
| Üç kart sütununun başlangıcı | 57 / 340 / 623 | 375 | yaklaşık 264 px kart genişliği |
| Birinci / ikinci / üçüncü kart satırı | — | 375 / 722 / 1042 | yaklaşık 328 / 305 / 307 px görünür kart yüksekliği |
| Alt menü | 0 | 1480 | yaklaşık 941×192 |

1. Ekranın arkasında mavi fantastik manzara, solda yaprak ve taş kemer, sağ üstte uzaktaki yüzen kale, altta koyu mor taş duvar. Arka plan yazısız, ayrı ve tam ekran. Kart bölgesinde düşük görsel yoğunluk.
2. Üstte sandık ve kristaller, önünde altın kenarlı mor kurdele. **Koleksiyon** canlı TMP metni. Referansta yaklaşık 53 px, sıcak krem ön yüz ve hafif koyu gölge. İki köşe düğmesi turkuaz gövdeli, altın kenarlı, açık renk simgeli. Safe area dışında yalnız dekor uzayabilir.
3. Kart paneli mor ve lacivert geçişli, çift altın kenarlı, alt orta noktası aşağı kıvrımlı ve mor elmas rozetli. Başlık kapsülü ve kategori sayacı aynı yatay hizada. Sol kapsül yıldız ve yerelleştirilmiş kategori adını, sağ kapsül gerçek sahip olunan/toplam sayısını gösterir.
4. Çerçeve, üst başlık ve alt menü sabit kalır. Kartlar ve kategori başlıkları yalnız panelin içindeki dikey `ScrollRect` alanında hareket eder. Yıldız kategorisinin başlık kapsülünü panelin üst kenarına bindir. Diğer kategorilere kayınca başlığı ve sayacı görünen kategoriyle eşleştir. İçerikte son kartın tamamına ulaşılabilsin. Mevcut kategoriler ve tüm öğeler erişilebilir olsun.
5. Alt bar altın kenarlı dalgalı krem gövde ve üç ince ayırıcı. Harita, takvim, mavi kristal ve sandık simgeleri ayrı. Referansta aktif Taşlar sekmesi parlak sarı-altın bir kapsülle seçili, etiket koyu lacivert. Diğer etiketler de koyu lacivert ve yaklaşık 32 px. Simgeler biraz yukarı taşar, ekran kenarında kırpılmaz.

## Kartlar ve durumlar

- Üç eşit sütun, yaklaşık 18–20 px yatay ve dikey boşluk. Sütun genişliğini panelin kullanılabilir eninden hesapla. Satırlar aynı hizada olsun. Dar ekran veya uzun dilde önce iç boşlukları ve TMP boyutunu makul ölçüde uyumla, gerekirse kart yüksekliğini büyüt. Alt çubuğu ve güvenli alanı ihlal etme.
- **Kazanılmış kart:** okunaklı taş silueti ve kaidesi üstte, sağ üstte yeşil onay rozeti. Mavi taşlarda mavi/turkuaz kenar ışığı, sıcak taşlarda kontrollü altın vurgu. Altın isim şeridi, altında kontrastlı beyaz açıklama. Kart adı yaklaşık 24–26 px, açıklama 18–20 px. Renk tanımlı mevcut sanat/metaveriden gelsin veya mevcut görselle uyumlu tema seçilsin.
- **Kilitli, adı bilinen kart:** soğuk mavi buzlu cam çerçeve, düşük kontrastlı renk silueti, sağ üstte soru rozeti ve ortada ayrı kilit simgesi. Altın isim şeridi ve gerçek açılma koşulu okunur. Cam veya buz etkisi yalnız görsel katman, yeni oyun mekaniği ekleme.
- **Gizli hazine:** mevcut `Hidden` davranışı sürer. Bulunmamış hazinenin adı ve gerçek taş görseli gizli kalır. Genel siluet kullan.
- **Aktif güç ile sahiplik ayrı durumlar.** Yeşil tik sahipliği gösterir. Aktif taşı belirtmek gerekiyorsa mevcut seçili verisini ayrı bir işaretle göster. Yeni seçim veya satın alma davranışı icat etme.
- Görseldeki **3/10**, taş adları ve eşikler örnek. Kadrajda dokuz kart var; kalan kart ve diğer kategoriler kaydırmayla erişilir. Görseli eşlemek için üretim kataloğundaki eşikleri, güçleri veya adları değiştirme.

## Görsel dosyalar

Tam ekran referans karşılaştırma içindir. Tek parça ekran görüntüsünü arayüz olarak kullanma. Yazılar, rakamlar, kartlar, ikonlar ve rozetler bağımsız olmalı. Mevcut taş PNG'lerini `preserveAspect` ile kullan. Yeni taş modeli üretme.

Önce yazısız arka plan, sandık illüstrasyonu, kurdele, mor/altın panel kabuğu, kategori kapsülleri, kart çerçeveleri, buzlu katman, kilit ve onay rozetleri ile alt bar kaynaklarını envanterle. Bu pakette sadece tam ekran referans ve talimat var. Eksik kaynakları dosya adı ve beklenen ölçüyle listele. Geçici parçaları final görünüm olarak raporlama. 2× alfa PNG veya vektör kullan. Çerçeve konturlarını esneterek bozma. Yerelleştirme mevcut `Loc` ve TMP ile canlı metin olarak çalışır.

## Kabul ve teslim

1. Referans oranı **941×1672**, normal telefon **1080×1920** ve dar çentikli telefon ekranı al. Karşılaştırma için geçici kayıtla yıldız kategorisinde üç kazanılmış taş ve kilitli kartları göster. Gerçek oyuncu kaydına örnek değer yazma.
2. Scroll başlangıcı ve sonunu doğrula. Başlık ve alt çubuk sabit, kartlar panel maskesinin içinde. Her kategoriye geçilir ve gerçek sayacı görülür. En uzun taş adı ve uzun açıklama kesilmez.
3. Harita ve Günlük dönüşünü, ayarları ve varsa Ödüller bağını dene. Aynı kayıtla haritanın önce ve sonra görünümünü karşılaştır. Harita yolu ve görünümü korunmuş olmalı.
4. Derleme ve test sonuçlarını, değişen dosyaları, ekran görüntülerini ve kalan görsel farkları raporla. Eksik yazısız sprite, bağlı olmayan ekran veya hedef ölçüden sapma varsa ayrı belirt.
