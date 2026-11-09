# TriKat v10 · Mavi Liman üst başlık parçaları

Bu paketi `docs/v10/TriKat-Mavi-Liman-Claude/CLAUDE-PROMPT.md` ve yanındaki tam ekran referansla birlikte kullan. Mevcut deniz feneri, TriKat logosu, dişli ve yıldız simgesini koru. Yeni çizim gerektiren üst başlık kabukları aşağıda.

| Dosya | Unity yerleşimi | 940×1673 referansında yaklaşık görünür sınır |
|---|---|---|
| 01-ust-altin-dalgali-serit.png | Harita üzerinde sabit, bulut zemininden ve fenerden önde. Defneler gövdeye dahildir. Bölge adı ve aralığı öne canlı TMP olarak çizilir. | x 0, y 152, 940×141 |
| 02-mor-bulut-ust-zemin.png | Üst sabit kabuğun en arka katmanı, fener ve logo altında. Alt alfa geçişi haritaya yumuşak bağlanır. Mevcut harita bulutlarına müdahale etme. | x 0, y 0, yaklaşık 940×252 |
| 03-yildiz-mor-kapsul.png | Sağ üst dişlinin altında. Mevcut yıldızı ve toplamı kapsülün önünde çiz. | x 716, y 128, 196×80 |
| 04-bolge-turkuaz-etiket.png | Altın orta plakanın üst sınırına bindir. Bölge metni ve numarası mevcut veriden gelsin. | x 375, y 140, 188×49 |

- `MenuView.ShowLevels` ana haritayı kurar. `HeaderCard` ve üst başlık katmanı kapsamında çalış. ScrollRect içeriğini, harita yolunu, bölüm düğümlerini, maskotu ve harita bulutlarını koru.
- Alt menü için `docs/v10/TriKat-Koleksiyon-Parcalari/png/01-alt-menu-kabugu.png` ve `05-oduller-kasa-ikonu.png` kullan. Kapalı kasa Ödüller sekmesi için. Koleksiyonun açık sandığı üst başlığa taşınmaz.
- PNG'leri tek Sprite (2D and UI), alpha açık, sRGB olarak içe aktar. Dalgalı şeridin uçlarını ve merkez plakasını görünür tut. Şeffaf payı görünür ölçüye katma. Aspect farkı nedeniyle dalgalı konturu ezme. Üst yazılar, yıldız toplamı ve dişli güvenli alanda kalır.
- Referans oranında aynı kayıtla önce–sonra Unity görüntüsü al. Haritayı kaydırırken üst başlık sabit kalmalı ve mevcut bölge verisi güncellenmeli. Dekorlar tıklama almamalı. Var olan yol/scroll alanı veya veri davranışı bu paketin kapsamı dışında.

## Dört WAV dosyasını uygula

`ses/` klasöründeki özgün WAV'ları Unity'ye ekle. Kaynakları örneğin `Assets/KatKat/Resources/Audio/TriKat/` altında tut. Mevcut `Game/SoundBank.cs`, `GameController.cs` ve ilgili partial akışları incele. Ses değişikliklerini görsel yerleşimden ayrı bir değişiklik kümesinde doğrula.

| WAV | Bağlanacak olay |
|---|---|
| 01-anasayfa-opal-esinti-loop.wav | Ana ekran olarak açılan haritada düşük seviyede, kesintisiz 32 sn döngü. Menüye geri gelince tek oynatıcı devam eder veya yumuşak başlar. Bölüm başlayınca 150–250 ms fade ile kapanır. Scroll, bölge değişimi veya menü tekrar çizimi döngüyü baştan başlatmaz. |
| 02-tek-tas-yumusak-pop.wav | Geçerli ilk normal soyulma dalgasındaki mevcut `Sfx.Peel`. Görsel soyulma anında dalga başına bir kez. Taş başına çalma. Boş/geçersiz dokunuşta mevcut davranış kalır. |
| 03-zincir-dort-tas.wav | Bir hamlenin ilk otomatik `WaveKind.Chain` dalgasında bir kez. Dosyada zaten dört yükselen vuruş var. Sonraki zincir dalgalarında bu WAV'ı üst üste bindirme. Eski `Sfx.Chain` aynı olay için ayrıca çalmasın. |
| 04-bolum-sonu-odul.wav | Normal bölüm zafer/ödül kartı ilk gösterildiğinde bir kez, mevcut `Sfx.Win` yerine. Her yıldız, kart yeniden çizimi veya onay butonunda tekrar başlatma. Mağaza, reklam veya günlük haklarını değiştirme. |

- Müzik için ayrı tek 2D `AudioSource` kullan. Efektlerde mevcut ses bankasını ve `CaptureTap` entegrasyonunu koru. Kayıt aracı WAV örneklerini kullanıyorsa stereo sesin nasıl işlendiğini doğrula. Dosyalar 44,1 kHz 16 bit PCM stereo. Sesleri normalize edip yükseltme. Kısa efektleri önceden yükle. Yeni ses yüklenemezse mevcut efekt güvenli geri dönüş olabilir.
- Mevcut ses ayarı hem müziği hem efektleri anında susturmalı. Ayar kapalıyken yeni ses başlatma. Uygulama arka plana geçtiğinde müziği duraklat. Öne dönünce yalnız harita açıksa ve ses ayarı açıksa sürdür. Aktif birden çok müzik kaynağı oluşmamalı.
- Zincir dalgasında aynı WAV için mevcut perde yükseltmesini ayrıca uygulama. Yeni pop ve zincir seslerini mevcut eşzamanlı efektlerin üstünde rahatsız edici hale getirme. Üst üste üç hamle, dört dalgalı zincir ve ödül kartında gerçek telefonda dinleme kontrolü yap.
- Haritaya giriş/çıkış, ayarı kapat/aç, arka plan/ön plan ve ödül ekranını yeniden çizme senaryolarını test et. Mevcut ses, titreşim, görsel ve oyun kurallarını koru. Çalışan ve çalıştırılamayan testleri raporla.
