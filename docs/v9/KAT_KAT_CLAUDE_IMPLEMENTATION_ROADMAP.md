# Kat Kat V1 · Claude Code uygulama yol haritası

## Başlangıç talimatı

KatKat Unity projesinin kökünde çalış. Önce bu dosyayı ve aynı klasördeki KAT_KAT_CODEX_KARSILASTIRMA_VE_DEVIR_PLANI.md belgesini oku. Ürün hedefleri o belgede, kullanıcı tarafından onaylanan uygulama sırası ve kapsam sınırları burada. Bu oturumda önce A ve B paketlerini uygula. Her paketin sonunda değişen dosyaları, testleri ve açık kalan noktaları bildir. C ve sonrası ayrı iş paketleridir. Dış servis seçimine, canlı servis yapılandırmasına ve mağaza yayınlamaya geçmeden ilgili kararları raporla.

### Değişiklik sınırı

- Mevcut 1.000 bölümün JSON dosyalarını, kimliklerini, kayıtlı çözümlerini ve motor kurallarını koru. Günlük bulmaca içeriğini ayrı katalogda tut. +1 hamleyi normal bölüm oturumu açılırken uygula. Kaynak bölüm verisine yazma.
- Mevcut harita, maskot, taş görselleri, yerelleştirme ve kullanıcı değişikliklerini koru. Önce çalışma ağacını ve mevcut dosyaları incele. Başka işe ait değişiklikleri geri alma.
- **Görsel regresyon kontrolü.** A ve B öncesinde mevcut ana ekran, harita, normal oyun ekranı ve sonuç kartından referans görüntü al. Sonrasında aynı bölüm, tema ve ekran boyutuyla karşılaştır. En az bir dar veya çentikli ekranı kontrol et. Mevcut taş sprite'ları, logo, maskot, üst şerit, hedefler, yıldız çubuğu, arka planlar, butonlar ve animasyonların görünümü korunmalı. Yeni yer tutucular yalnız koleksiyon ve günlük bulmaca alanlarında görünür. Yeni akışa erişim eklemek için mevcut ekranın yerleşimini değiştirmek gerekiyorsa önce öneriyi göster ve onay al. Görsel fark bulunursa paketi tamamlanmış saymadan düzelt veya açıkça raporla.
- Koleksiyon modelleri ve görselleri kullanıcı tarafından daha sonra hazırlanacak. Geçici olarak mevcut taş simgesi veya nötr yer tutucu kullan. Taşın mantıksal kimliğini görsel anahtarından ayır. Eksik sprite için ortak fallback kullan. Yeni 3D model, animasyon ya da nihai taş görseli üretme.
- Firebase, mağaza, kimlik ve reklam için gerçek anahtarları isteme veya repoya yazma. Yerelde sahte adaptör kullanıldığı her yerde bunu açıkça işaretle.

## A · Mevcut davranışı doğrula ve kayıt temeli oluştur

1. Başlangıçta `Assets/KatKat/Scripts/Game/LevelCatalog.cs`, `Save/SaveData.cs`, `Save/SaveService.cs`, `Game/GameController.cs` ve `Tests/EditMode/ProductionLevelTests.cs` dosyalarını incele. Kısa `Save/` ve `Game/` yolları `Assets/KatKat/Scripts/` altında. Test dosyası `Assets/KatKat/Tests/EditMode/` altında.
2. Mevcut 1.000 üretim dosyasının sayısını doğrula. Üretim testi ile motor ve kayıt testlerini çalıştır. Başlangıçtaki bir hata varsa kaydet ve ilgili bağımlılığı izole et. Testleri gevşetme.
3. `RecordStars` davranışını koru. Yeni toplamı bölüm kimliği başına en yüksek geçerli 1–3 yıldızdan hesapla. Geçerli eski kayıtlardaki yıldızları taşı. Aynı bölümü 3, sonra 1, sonra 3 yıldızla kazanmak toplamı yalnız 3 artırmalı. Aynı bölümün yıldızı 1'den 3'e çıktığında artış 2 olmalı.
4. Sürüm numaralı, geriye uyumlu kayıt ekle. Koleksiyon taş kimlikleri, günlük haklar ve ödül durumu yeni alanlarda dursun. Eski kayıt yoksa güvenli varsayılanlar kullan. Yükseltmeyi ikinci açılışta tekrar uygulama. Bölüm ilerlemesi, ses ve diğer ayarlar korunmalı.
5. Oyun akışında yardım kullanımını ve oturum türünü tanımlayan küçük bir sonuç modeli hazırla. Hamle limiti, kullanılan hamleler, bölüm ve içerik sürümü, ilk deneme, başarı ve yardım türleri ayrışsın. Kullanım süresi ölçülebilir. Bu pakette ZPE puanı hesaplanmaz.

**Kabul:** Mevcut üretim testleri ve ilgili kural testleri geçer. Eski kayıt açılır, normal bölüm sonucu eskisi gibi yazılır ve üretim JSON'larında fark oluşmaz.

## B · Koleksiyon ve günlük bulmaca dilimini yerelde çalıştır

### B1 · İlk koleksiyon taşı

- İlk taşı öğretici tamamlandıktan kısa süre sonra ver. Kesin bölüm henüz seçilmedi. Yerel test için en erken uygun öğretici sonrası bölümü yapılandırılabilir bir eşik olarak kullan ve raporda sayıyı belirt. Bu eşiği yayın kuralı olarak sabitleme.
- Taş kaydı en az `stoneId`, yerelleştirme anahtarları, `artKey`, `powerId` ve kazanım koşulu içersin. Kazanım tek seferliktir. Yeni oyun açıp kapatınca kaybolmaz.
- İlk taş için mevcut oyun kurallarıyla uyumlu tek bir güç ailesi prototiple. Önerilen test gücü, mevcut satır rulosunu günlük bulmacada bir kez kullanma hakkıdır. Mevcut işlem ancak o günlük düzen için kurallara uygunsa bağla. Normal seviyeye taşıma. Gücü kullanmayan oyuncu da aynı düzeni çözebilmeli.
- Birkaç iyi dengelenmiş güç ailesi hedefle. 22 taşın her birine ayrı kod yolu açma. İlk paketin kabulü için bir taş ve bir güç yeterli. Nihai koleksiyon sayıları ve açılma kuralları içerik yapılandırması olarak kalır.

### B2 · Günlük oturum

- Kayıtlı çözümü doğrulanan en az bir günlük bulmaca hazırla. Ana 1.000 dosyaya dokunma. İlk deneme ve ücretsiz tekrar aynı günlük düzeni ve aynı başlangıç koşullarını açar.
- Günlük durum kimliği `dayId + challengeId + contentVersion` bileşenleriyle izlenir. İlk giriş, bir ilk deneme ve bir ücretsiz tekrar hakkı açar. Ek reklamla üçüncü deneme prototipte kapalıdır. Bu sınır yapılandırılabilir.
- Yerelde sahte ödüllü reklam ve sahte reklamsız hak adaptörü kullan. Ücretsiz tekrar ilk girişle gelir, ayrı reklam istemez. Reklam başarısızlığı veya iptali hakkı eksiltmez. Bir hakkı ancak kalıcı oturum kaydı açıldığında kullanılmış say. Kapanma sonrası aynı oturuma dön veya hakkı koru. Reklam geri çağrısının iki kez gelmesi yeni hak oluşturmamalı.
- Günlükte seçili taş gücü prototipte deneme başına en fazla bir kez kullanılabilir. Kullanılabilirlik doğrulaması ve gücün harcanması aynı işlem içinde olmalı. Geçersiz hedef hakkı tüketmez. Bu sınır oyun dengesiyle tekrar değerlendirilecek.
- Başarı ödülü gün başına bir kez yazılır. Gün içinde henüz kazanılmamış normal bölümler için açılan yeni oturumların hamle bütçesine +1 ekle. Açık oturumun ortasında limit değiştirme. Gün değişiminde eski ödülü yeni oturumlara taşıma. Geçmiş yıldız kayıtlarını değiştirme. Yardımlı sonucu rekabetçi ZPE için işaretle.
- Yerelde test edilebilir bir saat/gün sağlayıcısı kullan. Canlıda resetin kaynağı sunucu olacak. Yerel saati canlı ödül veya hak otoritesi olarak kullanma. Saat dilimi ve reset saati yayın öncesi karar gerektirir.

**Kabul:** Eski kayıtla açılan oyun → ilk taş kazanımı → günlük giriş → ilk deneme → ücretsiz tekrar → başarı → normal bölümde +1 hamle akışı çalışır. Taşsız kazanım, güç kullanım sınırı, iptal edilen reklam, tekrar gelen olay, uygulama kapanması ve gün sınırı otomatik veya kontrollü elle test edilir. Normal ilerleme ve ödül günlüğü birbirine karışmaz.

## C · Sunucu doğrulamasını iki cihazla kanıtla

- Önce küçük bir teknik doğrulama yap. Misafir profil, hesap bağlama, yetkili gün kimliği, günlük giriş hakkı ve tek seferlik ödül. Yönetilen servis ve özel backend seçeneklerini Huawei, kimlik doğrulama, işletim maliyeti ve güvenlik açısından doğrula. Teknoloji seçimini bulgularla bildir.
- Aynı hesapta bölüm yıldızlarını bölüm kimliği başına maksimumla birleştir. Kalıcı kazanımları benzersiz kimlikle tut. Günlük hak, harcama ve ödül için sunucu kaydı esas alınır. İstek kimliği veya benzersiz işlem anahtarıyla yeniden denemeleri güvenli yap.
- Normal çevrimdışı oyun akışını koru. Çevrimdışı sonuçların rekabetçi tabloda hangi doğrulamadan sonra sayılacağı, yayın öncesi açık bir karar. İstemcinin gönderdiği toplam yıldızı veya ZPE değerini sıralamaya doğrudan yazma.
- İki cihazda aynı anda ödül talebi, misafir hesabını mevcut hesaba bağlama, oturum yenileme, satın alma geri yükleme ve hesap silme için test senaryoları hazırla.

**Kabul:** A cihazındaki geçerli ödül B cihazına taşınır. Eşzamanlı ve tekrarlanan istekler ikinci ödül veya fazladan deneme üretmez. Bağlama hatası yerel ilerlemeyi silmez.

## D · İlk yayın entegrasyonları

- Ortak platformlar arası **yıldız sıralamasını** önce kur. Kaynak en iyi bölüm yıldızlarıdır. ZPE profilinde kalibrasyon durumunu gösterebilirsin. ZPE sıralaması, bölüm zorluğu ve yardımlı denemeler için veri doğrulandıktan sonra açılır. ZPE bir oyun içi performans ölçüsüdür.
- Süreyi ilk aşamada telemetri olarak kaydet. Puan formülüne otomatik ekleme. İlk deneme, hamle verimi, zorluk, ipucu ve yardım etkilerini ayrı tut. Günlük +1 hamle ve reklam yardımı, rekabetçi ZPE'yi yapay yükseltmemeli. Yıldız puanı ile ZPE formülü ayrı sürümlenir.
- Banner ekleme. Geçiş reklamını yalnız bölüm sınırlarında ve ilk beş bölümün dışında değerlendir. Ürün belgesindeki sıklık başlangıç hipotezidir. Reklamsız paket, günlük giriş dahil belirlenmiş reklam haklarını sunucu ve mağaza doğrulamasıyla uygular. Diğer ödüllü reklamların paketle ilişkisi yayına kadar netleşir.
- Sürümlü olay sözlüğü hazırla. Bölüm, yardım, günlük deneme, taş kazanımı, reklam ve satın alma olayları ayrışsın. Firebase Analytics, bildirim, Huawei adaptörü ve her mağazada satın alma geri yükleme akışları gerçek cihazda doğrulanır. Test ve canlı ortamlar ayrılır. Kimlik ve bildirim izinleri ürün belgesine göre ele alınır.

## E · Harita ölçeği ve koleksiyon içeriği

- Mevcut 100 bölümlük harita bölgelerini koru. İleride beş bölgeyi tek bir 500 bölümlük dünya altında grupla. Dünya metaverisi eklenirken eski bölüm ve tema kimliklerini taşı. `ThemeDefinition.RegionSize` değerini 500 yapıp mevcut temaları topluca değiştirme.
- 1.000 ve sentetik 10.000 kayıtla açılış, bellek ve harita kaydırma ölçümleri al. `LevelCatalog` bugün tüm bölüm dosyalarını açılışta yükler. `MenuView` görünür düğümleri oluşturur, görünür aralığı bulmak için baştan tarar. Profil sonuçları gerektirirse önce görünür aralığı doğrudan hesapla, sonra katalog için indeksli yüklemeyi değerlendir. Üretim bölümlerini yeniden üretme.
- Nihai koleksiyon modelleri ve ikonlar geldiğinde taş kimliği ve kazanım kuralları değişmeden `artKey` eşlemesini güncelle. Dünya tamamlama efsanevi ödül koşulu ve gizli hazine tetikleyicisi ayrı ürün kararıdır.

## Her paket sonunda rapor

- Değişen dosyalar ve görünen davranış.
- Çalıştırılan testler ve sonuçları. Çalıştırılamayanları belirt.
- Açık ürün kararları ile geçici prototip değerlerini ayrı listele.
- Üretim bölüm dosyalarında değişiklik olmadığını doğrula. Canlı servis veya mağaza yayını yapılmışsa bunun için verilmiş ayrı yetkiyi belirt.


