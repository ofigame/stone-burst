# KAT KAT — Codex Ürün Kararları, Mevcut Proje Karşılaştırması ve Claude Code Devir Talimatı

**Amaç:** Bu belge, ChatGPT ile kararlaştırılan Kat Kat V1 ürün hedeflerini Codex'in erişebildiği gerçek Unity projesiyle karşılaştırmak ve Claude Code'a devredilecek doğrulanmış bir geliştirme planı çıkarmak içindir.

> **ÖNEMLİ:** Aşağıdaki ürün kararları hedef gereksinimlerdir; mevcut projede uygulandıkları varsayılmamalıdır. Codex kodu, sahneleri, prefabları, varlıkları, bölüm verilerini ve testleri inceleyerek gerçek durumu doğrulamalıdır. Analiz aşamasında **hiçbir kod veya bölüm verisi değiştirilmemelidir**.

## 1. Projenin bilinen durumu ve sınırlar

- Oyun: **Kat Kat**, Unity tabanlı Android/iOS mobil bulmaca oyunu; Huawei AppGallery de hedefleniyor.
- **1.000 bölüm mevcut ve test edildi.** Bunları yeniden üretme, yeniden tasarlama, topluca değiştirme veya mevcut testleri geçersiz kılma.
- Mimari hedef **10.000 bölüm**; **ilk yayın 1.000 bölüm** içeriyor.
- Oynanış kuralları, taş hareketleri, kombinasyonlar, kazanma/kaybetme koşulları, engeller, gerçek bölüm biçimi **Codex tarafından mevcut projeden tespit edilmeli**; burada uydurulmamalı.
- Facebook entegrasyonu ilk sürüm kapsamı dışında.
- İlk sürümde aşağıda belirtilen bütün **temel sistemler** hedefleniyor; ancak uygulama planı mevcut durum ve bağımlılıklara göre çıkarılacak.
- Çalışan kodu ve kullanıcı deneyimini koru; gereksiz yeniden yazım, kapsam genişletme ve sırf mimari amaçlı büyük refaktör yapma.

## 2. Kesinleşmiş ürün kararları

1. **Koleksiyon taşı yetenekleri:** Taşlar kalıcı olarak kazanılır; yetenekleri her gün kullanılabilir. Günlük bulmaca içinde kullanım sınırı/denge mekanizması ayrıca tanımlanmalıdır.
2. **Gizli hazine taşları:** Hem koleksiyon nesnesidir hem de günlük bulmacada özel güç sağlar.
3. **Günlük bulmaca:** İlk giriş için ödüllü reklam; bu girişle ilk deneme + **1 ücretsiz tekrar** hakkı. Reklamsız paket sahipleri reklamsız girer.
4. **Reklamsız paket:** Zorunlu/geçiş reklamlarını kaldırır; günlük bulmacaya reklam izlemeden giriş sağlar. Diğer isteğe bağlı ödüllü reklamların hakları açıkça tasarlanmalıdır.
5. **Yıldızlar:** Her bölüm için yalnızca elde edilmiş **en yüksek yıldız sonucu** toplamda sayılır; tekrar oynayarak yıldız biriktirilemez. Her bölüm en çok 3 yıldız verir.
6. **ZPE (Zekâ Performans Endeksi):** Oyuncunun profilinde ve sıralamada herkese açık, oyun içi performans metriğidir; **gerçek IQ ölçümü gibi sunulmaz**.
7. **Hesap:** Oyuncu misafir başlayabilir; küresel sıralamaya katılmak için platform hesabını bağlar. Hesap/cihaz geçişi ve veri birleştirme güvenli olmalıdır.
8. **İlk yayın:** Bütün temel sistemler ilk 1.000 bölümlük yayında bulunacaktır.

## 3. İlerleme, dünyalar ve koleksiyon hedefleri

- 10.000 bölüm için **20 dünya × 500 bölüm** yapısı bir **tasarım önerisidir**; mevcut dünya/bölüm düzeniyle uyum Codex tarafından kontrol edilmeli, çatışma varsa değişiklik önerilmeden önce raporlanmalıdır.
- Önerilen dünya sırası: Akdeniz Sahili, Çiçek Bahçesi, Çöl Vahası, Büyülü Orman, Kristal Mağaraları, Karlı Zirveler, Tropik Adalar, Volkan Vadisi, Ay Işığı Ormanı, Altın Kanyon, Mercan Krallığı, Bulut Bahçeleri, Zümrüt Vadisi, Yıldız Çölü, Donmuş Kristaller, Gökkuşağı Diyarı, Kayıp Cennet, Kozmik Adalar, Efsanevi Tapınak, Sonsuz Işık.
- Bu öneriye göre ilk 1.000 bölüm iki dünya içerir: Akdeniz Sahili (1–500) ve Çiçek Bahçesi (501–1.000). **Mevcut projeyle uyuşmuyorsa otomatik uygulama.**
- Uzun vadeli koleksiyon önerisi: **220 taş** = 100 yıldız taşı + 100 gizli hazine + 20 efsanevi taş. İlk sürüm için **22 taş** = 10 yıldız taşı + yaklaşık 10 gizli hazine + 2 efsanevi taş. Bunlar içerik hedefleridir; var olan taş sayısını ve yapısını önce doğrula.
- Önerilen yıldız taşı eşikleri: her 250 toplam yıldızda bir açılma; ilk 1.000 bölümde toplam 3.000 olası yıldız, 10 taş için 2.500'e kadar eşikler. Taş açılma eşiklerinin mevcut ilerleme eğrisiyle uyumunu incele.
- Gizli hazine fikri: özel bölümlerde etkileşimli/kırılabilir nesneler arkasından taş keşfi; oyuncu kaçırırsa tekrar oynayabilsin. Gerçek oynanış mekaniğine uygunluğunu kontrol et; mevcut bulmacaya aykırı nesne ekleme.
- Efsanevi taşlar dünya tamamlamaya bağlanabilir; kesin kazanım kuralı **henüz kararlaştırılmadı**.
- Görsel hedef: premium, renkli, oyuncak benzeri 3D estetik; keşif sırasında 3D gösterim, koleksiyon ekranında performans için optimize ikon/görsel kullanımı önerilir.

## 4. Günlük özel bulmaca

- Oyuncu seviyesine ve gerçek zorluk verisine göre **çok zor** bir günlük mücadele; “10 kat zor” ifadesi matematiksel sabit çarpan olarak uygulanmamalı.
- Günlük giriş: ödüllü reklam; reklamsız paket sahibi için reklamsız giriş. Reklam izleme başarısı güvenilir biçimde doğrulanmalı, başarısız reklam yüklemesi oyuncuyu kilitlememeli.
- Bir giriş, ilk deneme ve **bir ücretsiz tekrar** açar. Aynı gün ödül tekrar tekrar kazanılamaz.
- Oyuncu sahip olduğu kalıcı koleksiyon taşlarından birini seçip günlük bulmacada özel gücünü kullanır. **Öneri:** seçili gücü mücadelede en fazla bir kez kullanmak; kesin sınır oyun dengesi testinden sonra onaylanacak.
- Günlük mücadeleyi başaran oyuncuya **o takvim günü boyunca oynayacağı kalan normal bölümlerde başlangıçta +1 hamle** verilir. Geçmişte tamamlanmış bölümlere geriye dönük etki yoktur. Günlük reset saati ve saat dilimi sunucu tarafından belirlenmelidir.
- Taşı olmayan oyuncu için de günlük bulmaca çözülebilir olmalı; farklı taşların güçleri kazanmayı garanti etmemeli.
- İstemci saati değişimi, tekrar istekleri, çevrimdışı kullanım ve çoklu cihaz ödül istismarına karşı durum yönetimi tasarlanmalı.
- Açık kararlar: ücretsiz tekrar sonrası yeni reklamla ek deneme olup olmayacağı (şimdilik **hayır varsayımı**); günlük bulmaca reset saati; güçlerin tam listesi ve dengesi.

## 5. ZPE ve sıralamalar

- ZPE, bölüm zorluğuna göre kalibre edilen **oyun içi performans endeksi** olmalı. İleri bölüm oyuncularına sırf ilerledikleri için haksız avantaj sağlamamalı.
- Başlangıç **öneri ağırlıkları**: hamle verimliliği %35, zorluğa göre başarı %25, ilk denemede başarı %20, süre %10, ipucusuz başarı %10. Bu katsayılar kesin değil; gerçek oyun verisiyle test edilmeli.
- İlk 10–20 bölümde kalibrasyon göstergesi; son 30–50 bölüm gibi kayan geçmişle güncelleme **öneri**. Bölüm sonu ZPE değişimi ve ana ekranda güncel ZPE görünürlüğü hedefleniyor.
- Yardımcı güç, ekstra hamle, ipucu ve reklamla alınan avantajların rekabetçi ZPE'yi yapay yükseltmemesi gerekir. Tekrar oynama ve başarısız denemelerin puanlaması açıkça belirlenmeli.
- **Küresel yıldız sıralaması:** platformlar arası ortak, oyuncunun kümülatif en iyi yıldızları esas alır; profil, sıra ve yakın rakipler gösterilir.
- ZPE sıralaması ayrıca planlanabilir; **ilk sürümde tam kapsam ve sıralama kuralları** mevcut altyapıya göre Codex tarafından önerilmelidir.
- Haftalık ligler ve ek koleksiyon sıralamaları sonraki sürüm adaylarıdır; ilk sürümün zorunlu kapsamına sessizce ekleme.
- Reklam veya ödeme ile doğrudan sıralama puanı satılmamalı.

## 6. Hesap, backend ve platformlar

- Hedef mağazalar: Apple App Store, Google Play, Huawei AppGallery.
- Misafir başlangıç, ardından Apple Game Center / Google Play Games Services / Huawei hesap veya oyun hizmetleri üzerinden bağlama akışı. Bu servislerin gerçek kimlik doğrulama ve token doğrulama yeteneklerini mevcut SDK/sürüm bazında kontrol et.
- Tek oyuncu profili ve **ortak platformlar arası liderlik tablosu**. Hesap bağlama, çoklu sağlayıcı bağlama, cihaz değiştirme, misafir hesabını birleştirme, çakışan kayıtlar, hesap silme senaryolarını ele al.
- Olası teknoloji: Unity Authentication/Leaderboard gibi yönetilen servisler veya ASP.NET Core backend; **henüz kesin teknoloji kararı yok**. Var olan altyapıyı inceleyip maliyet, Huawei uyumu, güvenlik, operasyon ve entegrasyon karmaşıklığıyla karşılaştır.
- Ödüller, ilerleme, yıldızlar, taş kazanımları, günlük mücadele ve rekabetçi sonuçlar yalnızca istemci beyanına körü körüne güvenilerek kabul edilmemeli. Hile riskine uygun sunucu doğrulama stratejisi tasarlanmalı.
- Normal bölümlerde çevrimdışı oynanış korunması önerilir; çevrimdışı verilerin senkronizasyonu ve hile riski analiz edilmeli.

## 7. Reklam ve gelir modeli

- Ana menü, harita ve aktif bulmaca oynanışında **banner reklam yok**.
- İsteğe bağlı ödüllü reklamlar: destekleyici haklar/ekstra hamle, uygun güçlendiriciler ve günlük mücadeleye giriş.
- Geçiş reklamları yalnızca doğal bölüm sınırlarında; **ilk 5 bölümde yok**. Başlangıç test önerisi: 3–4 tamamlanan bölümde bir, en az 180 saniye arayla, ödüllü reklamdan hemen sonra değil. Kesin sıklık retention/gelir testleriyle ayarlanır.
- Tek seferlik reklamsız paket; **99,99 TL önceki fiyat hipotezidir, kesin fiyat değil**. Platforma ve ülkeye göre mağaza fiyatlandırması doğrulanmalı.
- Reklamsız paket zorunlu reklamları kaldırır ve günlük bulmaca girişini reklamsız yapar. Diğer isteğe bağlı ödüllü reklamların kaldırılması/alternatif ödüller açık karara bağlanmalı.
- Satın alma doğrulaması, restore, platformlar arası entitlement ve başarısız reklam akışları planlanmalı.

## 8. Firebase, bildirim ve kullanıcı aktifliği

**İlk yayın gereksinimi:** Firebase Analytics + bildirim altyapısı.

- Firebase Analytics: DAU/WAU/MAU, oturumlar, D1/D7/D30 retention, bölüm başlama/başarma/kaybetme, hamle/yardım kullanımı, günlük mücadele giriş/başarı, koleksiyon kazanımları, reklam etkileşimleri, satın alma hunisi ve bildirim dönüşleri.
- Ölçüm olayları ve parametreleri tek bir sürümlü **analytics event sözlüğü** üzerinden yönetilsin; kişisel veri gereksiz gönderilmesin; onay ve gizlilik gereksinimleri karşılanmalı.
- Firebase Cloud Messaging (FCM): günlük mücadele hatırlatması, yeni dünya/bölüm, özel etkinlik; oyuncu bildirimleri kapatabilmeli. Bildirim izni platforma uygun zamanda istenmeli, sıklık sınırlanmalı.
- **Huawei:** Google Mobile Services olmayan cihazlarda FCM'ye güvenme; Huawei Push Kit ve analitik uyumluluğu için ayrı adaptör/alternatif tasarla ve gerçek cihazda doğrula.
- Firebase Analytics **anlık çevrimiçi kullanıcı sayısı** vermez. “Şu anda çevrimiçi” durumu istenirse ayrıca presence/heartbeat sistemi gerekir; şu anda kesin ilk sürüm gereksinimi değildir.
- Bildirim gönderim yetkileri, token yenileme, izin kaldırma, kullanıcı hesabından çıkış, platform hedefleme ve sunucu güvenliği tasarlanmalı.

## 9. Teknik kalite ve sürümleme

- Mevcut Unity sürümünü, render pipeline'ı, paketleri, platform build ayarlarını, minimum işletim sistemi sürümlerini ve kullanılan SDK'ları tespit et.
- 10.000 bölüm ölçeğinde harita yükleme, bölüm veri saklama, asset yönetimi, bellek ve performans yaklaşımını incele; 1.000 bölümü yeniden üretme.
- Oyun verisi, ödül kuralları, ZPE formülü ve hesap senkronizasyonu için sürümleme/migrasyon stratejisi oluştur.
- Analiz sırasında API anahtarı veya servis hesabı isteme; gerekli bağlantıları, kullanıcıdan alınacak yapılandırmaları ve güvenli saklama yolunu yalnızca raporla.
- Otomatik testler, regresyon testleri, gerçek cihaz testleri, mağaza uyumluluğu ve gizlilik/hesap silme gereksinimlerini kapsa.
- Başlangıç test KPI **önerileri**: ilk bölüm tamamlama %90+, ilk 10 bölüm %60+, D1 %35+, D7 %12–15+, crash-free sessions %99,5+. Bunlar garanti veya nihai yayın şartı değildir.

## 10. Codex'ten istenen analiz ve çıktı dosyaları

**Bu aşamada yalnızca inceleme ve dokümantasyon yap; kaynak kodu, sahneleri, assetleri ve bölüm verilerini değiştirme.** Mevcut dokümanları gerektiğinde oku; mevcut proje dosyalarına müdahale etme.

1. `KAT_KAT_CURRENT_GAME_DESIGN.md` — Gerçek oynanış: tahta/taş kuralları, hamle, kombinasyon, engel, kazanma/kaybetme, bölüm yapısı, arayüz, var olan sistemler. Her iddiaya ilgili dosya/sınıf/sahne referansı ekle.
2. `KAT_KAT_GAP_ANALYSIS.md` — Bu belgedeki her gereksinim için **Mevcut / Kısmen mevcut / Eksik / Çelişkili / Doğrulanamadı** statüsü, kanıt, etkilenen dosyalar, risk ve öneri. **Kesin karar** ile **öneri/açık karar** ayrımını koru.
3. `KAT_KAT_CLAUDE_IMPLEMENTATION_ROADMAP.md` — Claude Code için bağımlılık sıralı küçük görevler, her görevin amacı, giriş/çıkış koşulu, dosya/komponent kapsamı, regresyon riski, otomatik/manuel test ve kabul kriterleri. Haftalar üzerinden tahmin yürütme; AI destekli uygulanabilir iş paketleri oluştur.
4. `KAT_KAT_OPEN_DECISIONS.md` — Sadece gerçekten eksik veya çelişkili kararları, varsayılan öneriyi ve etkisini listele. Kullanıcı tarafından kesinleştirilmiş 8 kararı yeniden tartışmaya açma.

**Karşılaştırma tablosunda özellikle kontrol et:**
- 1.000 bölümün varlığı, test kapsamı ve formatı
- Bölüm motorunun 10.000'e ölçeklenebilirliği
- Yıldız hesaplama ve tekrar oynama davranışı
- Harita/dünya sistemi ve önerilen 20×500 yapıyla uyum
- Koleksiyon/gizli hazine/taş yetenekleri
- Günlük bulmaca ve +1 hamle etkisi
- ZPE hesaplama için gereken gerçek oyun verileri
- Misafir hesap, platform kimliği, bulut kayıt ve sıralama
- Reklamlar, reklamsız paket ve satın alma
- Firebase Analytics/FCM ve Huawei alternatifleri
- Hile önleme, veri migrasyonu, offline senkronizasyon

## 11. Codex çalışma kuralları

- İlk önce mevcut projeyi oku, ardından bu dokümanla karşılaştır.
- **Kanıta dayalı raporla:** Bir özelliğin var olduğunu söylemek için dosya/konum göster; bulamadığını otomatik “yok” diye kesinleştirme.
- Oynanış kurallarını varsayma; gerçek koddan çıkar. Projede belirsizlik varsa kullanıcıya sorulacak sorulara ekle.
- Geliştirmeyi bu aşamada başlatma; yalnızca dört çıktı dokümanını hazırla ve özetle.
- Mevcut çalışan sistemleri yeniden yazmayı önermeden önce gerekçe ve daha küçük alternatifleri belirt.
- Yeni özelliklerin birbiriyle çeliştiği noktaları açıkça işaretle.
- Tüm raporlar **Türkçe** olsun; kod/sınıf/teknik terimler özgün isimleriyle kalabilir.
- Claude Code'a aktarım için yapılacak işleri net, bağımsız, doğrulanabilir görevler olarak yaz.

## 12. Codex'e gönderilecek başlangıç promptu

> Kat Kat Unity projesinin mevcut kodunu, sahnelerini, prefablarını, bölüm verilerini ve testlerini incele. Proje kökündeki `KAT_KAT_CODEX_KARSILASTIRMA_VE_DEVIR_PLANI.md` dosyasını eksiksiz oku. Bu belgede ChatGPT ile kararlaştırılmış ürün hedefleri bulunuyor; bunları mevcut oyunun gerçek durumu ile karşılaştır. 1.000 mevcut ve test edilmiş bölümü, çalışan oyun mekaniklerini ve kaynak kodunu **değiştirme**. Önce oyunun gerçek mekaniklerini belgele, sonra gereksinim bazlı boşluk/çelişki analizi yap ve Claude Code'a verilecek bağımlılık sıralı geliştirme görevlerini hazırla. `KAT_KAT_CURRENT_GAME_DESIGN.md`, `KAT_KAT_GAP_ANALYSIS.md`, `KAT_KAT_CLAUDE_IMPLEMENTATION_ROADMAP.md` ve `KAT_KAT_OPEN_DECISIONS.md` dosyalarını üret. Her mevcut özellik tespiti için dosya veya sınıf kanıtı ver; belirsizlikleri varsayımla doldurma. Kod değişikliğine geçmeden raporunu Türkçe sun ve onay bekle.
