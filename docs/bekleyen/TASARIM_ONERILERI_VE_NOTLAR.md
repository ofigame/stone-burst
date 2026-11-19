# Stone Burst — Tasarım önerileri ve değerlendirme notları

Tarih: 2026-11-11
Durum: Tartışma notları. Kullanıcı bu önerilerin kaydedilmesini istedi; uygulama veya oyun kuralı değişikliği onaylanmış değildir.
Kaynak: PROMPT.md, OYUN_MANTIGI.md, EKRANLAR.md ve tasarım görüşmesi. Bu değerlendirmede oyun çalıştırılmadı, kod doğrulanmadı. Riskler ölçülmüş oyuncu sorunları değil, belgelerden çıkarılan hipotezlerdir.

## 1. Tasarım amacı

Mantık ve eğlenceyi birlikte korumak; bölümler ilerledikçe oyuncuya farklı kararlar ve yenilik hissi vermek. Katman soyma, alttaki renkleri planlama, kenar komşuluğu ve zincir oluşturma temel kimlik olarak korunmalı. Yenilik yalnız daha fazla engel veya daha zor bölümler anlamına gelmemeli.

## 2. İlk değerlendirme: dikkatimi çeken noktalar ve önerilerim

### 2.1 Zincirin anlaşılması
- Mevcut durum: Zincire yalnız o dalgada yeni açılan taşlar katılır; önceden açık aynı renk komşular katılmaz (Oyun Mantığı §3.4, §20).
- Risk: Oyuncu aynı renk komşunun neden patlamadığını anlayamayabilir.
- Öneri: Kuralı değiştirmeden öğreticide iki durumu karşılaştırmalı göster.
- Beklenen etki: Sonucu öngörme ve kuralı anlama artar; “neden patlamadı?” hissi azalır.

### 2.2 Sakin oyun ile hız puanı
- Mevcut durum: Süre sınırı yoktur; çözüm hızı ZPE'nin %10'udur (§17).
- Risk: Sıralamayı önemseyen oyuncu acele etmeye yöneldiğini hissedebilir.
- Öneri: Hız bileşenini kaldırmayı veya ayrı bir istatistik olarak göstermeyi değerlendir.
- Beklenen etki: Düşünerek oynama ile performans değerlendirmesi daha tutarlı olur.

### 2.3 Kaybetmenin etkisi
- Mevcut durum: Başarısız deneme ZPE'ye sıfır olarak girer; endeks bütün geçmişin ortalamasıdır (§17).
- Risk: Öğrenmek için deneme yapan oyuncu sert etkilenebilir; eski başarısızlıkların etkisi taşınır.
- Öneri: Oyuncu testinde bu sistemin hissettirdiği cezayı ve ilerleme algısını özellikle gözlemle. Henüz alternatif formül seçilmedi.
- Beklenen etki: Yeni strateji deneme isteğinin korunması.

### 2.4 İpucunun bedeli
- Mevcut durum: Reklamlı ipucu hem reklam izletir hem bir yıldız azaltır. Çözücü sonuç bulamazsa seçilen hamle çözümü garanti etmez (§15).
- Risk: Yardımın bedeli ve sağladığı değer arasında güvensizlik oluşabilir.
- Öneri: İpucunun bedelini ve güvenilirliğini birlikte değerlendir; garanti edilmeyen hamleyi garanti çözüm gibi sunma. Nihai bedel değişikliği kararlaştırılmadı.
- Beklenen etki: İpucu sonrası memnuniyet ve oyuna devam etme.

### 2.5 21. bölüm geçişi
- Mevcut durum: Ataş mekaniği ve kalp sistemi 21. bölümde devreye girer; bu aralıkta renk ve katman karmaşıklığı da artar (§7, §9, §14).
- Risk: Buradaki oyuncu kaybı yalnız bölüm zorluğuna bağlanabilir; aynı anda birkaç değişken vardır.
- Öneri: Yeni kuralı anlama, kalp kaynaklı kesinti ve bölüm zorluğunu ayrı değerlendir.
- Beklenen etki: Zorlanmanın nedenini doğru saptama.

### 2.6 Günlük bulmacaya giriş
- Mevcut durum: Günlük bulmacaya giriş ödüllü reklamla açılır; reklam bulunamazsa reklamsız girilir (§11).
- Risk: Girişte reklam, günlük alışkanlığın oluşmasını zorlaştırabilir.
- Öneri: İlk günlük girişin ücretsiz olduğu seçeneği mevcut akışla karşılaştırmalı test et.
- Beklenen etki: Günlük bulmacaya katılım ve ertesi gün geri dönüş.

### 2.7 Öncelik ve doğrulama yaklaşımı
- İlk öncelikler: zincirin anlaşılabilirliği, ZPE'nin oyuncuya hissettirdiği şey, ipucunun bedeli.
- Kayıtlı çözüm ve bot başarısı, bölümün insana adil hissettirdiğini tek başına kanıtlamaz (§9.4).
- İlk 20 bölümde zincir ve tepsinin öğrenilmesi; 21–60 arasında yeni kuralların anlaşılması incelenmeli.

## 3. Yenilik önerileri

Aşağıdakiler mevcut özelliklerin açıklaması değil, yeni tasarım fikirleridir. Sayılar örnektir; dengelenmiş bölüm verileri değildir.

### 3.1 Bölüm içinde değişen hedefler
- Örnek: Önce 6 mercan, sonra 6 turkuaz topla.
- Aşamalar baştan görünür; tahta yenilenmez. İlk aşamadaki hamleler sonraki aşamadaki kaynakları etkiler.
- Aktif olmayan hedefin taşlarını erken soymak sonraki aşamayı zorlaştırabilir.
- Sürekli değil, aralıklı özel bölüm türü olarak kullanılır.
- Amaç: Sonraki aşamaya iyi bir tahta bırakmayı düşündürmek.

### 3.2 Zincirle açılan mühür
- Mühür belirli bir otomatik zincir derinliğine ulaşıldığında açılır.
- Örnek: İki halkalı mühür ikinci otomatik zincir dalgasında açılır.
- İlk dokunuş ve otomatik dalgalar görsel olarak açıkça ayrılır.
- İlk örnekler küçük tahtalarda, kısa ve okunur zincirlerle öğretilir.
- Amaç: Büyük gruba basmak yerine alt katmanları birlikte açılacak şekilde hazırlamak.
- Açık tasarım sorusu: Mührün konumu, açılmanın tahtaya etkisi ve dalga tetikleme kapsamı prototip öncesi netleştirilmeli.

### 3.3 Birbirine bağlı iki küçük tahta
- Aynı bölümde iki ayrı alan; ortak hamle sayısı ve özel taş tepsisi.
- Bir alanda üretilen roket diğerinde kullanılabilir.
- Karar: Doğrudan hedefe ilerlemek mi, başka alanda yardım üretmek mi?
- Toplam hücre sayısı normal tahtayı aşmamalı; mobilde taşlar küçülmemeli.
- Ara sıra gelen önemli bölümlere uygun.
- Açık tasarım sorusu: Roketlerin iki alan arasındaki etki sınırı açıkça belirlenmeli.

### 3.4 Köprü katmanı
- Bazı yığınlarda belirli derinlikte köprü işareti olur; o katman açığa çıkınca iki bölge bağlanır.
- Mevcut düğme/geçitten farkı: Tamamen boşaltmak yerine doğru derinliğe ulaşmak ve bağlantı hazırlamak.
- Kenar komşuluğu korunur; uzaktan eşleşme eklenmez.
- Önce mevcut geçidin bir çeşidi olarak değerlendirilir.
- Amaç: Köprü açılmadan iki taraftaki renkleri planlamak.

### 3.5 İki çözüm yolu bulunan bölümler
- Güvenli yol: Küçük gruplarla hedefi tamamlamak.
- Ustalık yolu: Hazırlık hamleleriyle büyük zincir ve daha iyi sonuç.
- Normal ilerleme kusursuz çözümü gerektirmemeli.
- İyi çözümün fırsatı okunabilir olmalı; gizli tek doğru cevap hissi oluşmamalı.
- Amaç: Rahat oynayanı ve derin planlama isteyen oyuncuyu aynı yapıda buluşturmak.

### 3.6 İsteğe bağlı ustalık görevleri
- Örnekler: Roketsiz bitir; tek hamlede iki otomatik zincir dalgası oluştur; tepside iki özel taş bırak.
- Bölümü geçmek için zorunlu değildir.
- Ödül önerisi: Bölüm düğümünde küçük ustalık rozeti; başlangıçta yeni para birimi yok.
- Rastgele dağıtılmamalı; tahtanın ilginç bir çözümüne karşılık gelmeli.
- Yıldız sisteminin ödüllendirdiği şeyi tekrar ödüllendirmemeli.
- Amaç: Aynı kurallarla farklı oynama biçimleri denetmek.

### 3.7 Gerilim ve rahatlama ritmi
- Zor planlamadan sonra öğrenilen beceriyi güçlü zincirlerle kullandıran rahatlama bölümleri.
- Örnek roller: Tanışma → Deneme → Eski mekanikle birleşim → Beklentiyi değiştiren çözüm → Rahatlama → Final.
- Bu sıra her altı bölümde aynen tekrarlanan katı şablon değildir.
- Amaç: Sürekli zorluk artışı yerine değişen deneyim ve ustalık hissi.

### 3.8 Bölge finallerine belirgin amaç
- Mevcut hedef ilerlemesi çevrede görünür değişime dönüşür.
- Örnekler: Fener Koyu'nda fener aydınlanır; Vaha Yolu'nda çeşme açılır; Mavi Liman'da köprü tamamlanır.
- Taş kuralları korunur; uzun ara sahneler yerine kısa ve okunur değişimler kullanılır.
- Amaç: Sayaç doldurmaktan öte bir işi tamamlama ve bölge ilerlemesi hissi.

## 4. Önerilen ilk deney paketi

Henüz onaylanmadı. Küçük kapsamlı değerlendirme önerisi:
- 4 bölüm: İki çözüm yolu ve farklı zorluk ritimleri.
- 3 bölüm: Aşamalı hedefler.
- 3 bölüm: Zincir mühürleri.
- 2 bölüm: Ortak tepsili iki oyun alanı.
- Toplam 12 bölüm; sayılar içerik taahhüdü değil, fikirleri test etme kapsamıdır.
- Köprü katmanı ve ustalık görevleri bu denemelerden sonra ele alınabilir.

Test soruları:
1. Oyuncu kuralı kendi sözleriyle açıklayabiliyor mu?
2. Kaybettiğinde nedenini anlıyor mu?
3. İkinci denemesinde farklı bir plan kuruyor mu?
4. Yenilik, aynı oyunun doğal devamı gibi hissediliyor mu?

İlk görüşmedeki tercih: Aşamalı hedefler ve zincir mühürleri Stone Burst'ün katman/planlama kimliğine en uygun iki aday olarak değerlendirildi. Nihai seçim yapılmadı.

## 5. Kapsam

Bu dosya yalnız görüşme notudur. Oyun kodu, sahneler, bölüm verileri ve mevcut kurallar değiştirilmedi. Sonraki konuşmada fikirler geliştirilebilir, elenebilir veya önceliklendirilebilir.

## 6. Animasyon, ZPE, ikonlar ve kazanma hissi — 2026-11-11

Durum: Kullanıcının kaydedilmesini istediği araştırma ve öneriler. Uygulama onayı değildir. Önceki öneriler korunmuştur. Bu turda ZPE ve ilgili oyun akışı koddan incelendi, mevcut harita ekran görüntüsü görüldü ve dış kaynak araştırması yapıldı; cihazda oynanarak süre veya oyuncu davranışı ölçülmedi.

### 6.1 ZPE: kod incelemesi bulguları

İncelenen kaynaklar (proje köküne göre):
- Assets/KatKat/Scripts/Levels/Meta/Zpe.cs
- Assets/KatKat/Scripts/Levels/Meta/SessionOutcome.cs
- Assets/KatKat/Scripts/Game/GameController.Meta.cs
- Assets/KatKat/Scripts/Game/GameController.cs
- Assets/KatKat/Resources/Zpe/zpe_config.json
- Assets/KatKat/Scripts/View/BoardView.cs

Bulgular:
1. Normal eşleştirmelerle kazanılan roket ve Altın Rulo, tepsiden kullanıldığında doğrudan yardım cezası almıyor. Kullanım akışında bunları yardım olarak işaretleyen bir çağrı bulunmadı. Günlük koleksiyon taşı gücü ayrı bir kavramdır.
2. OutcomeTick, Phase.Animating sırasında geçen süreyi ActiveSeconds içine ekliyor. Böylece özel taş ve zincir animasyonlarını beklemek ZPE'nin hız bileşenini düşürebiliyor. Her özel taş kullanımının toplam puanı mutlaka düşürdüğü söylenemez; hamle verimi kazancı bunu telafi edebilir.
3. Günlük ödülünden gelen +1 hamle, bölüm başında DailyMoveBonus yardım bayrağı oluşturuyor. Ek hamleye ihtiyaç duyulmadan kazanılsa bile yardımsız bitirme bileşeni düşüyor.
4. Öğretici veya yönlendirme Guidance olarak yardım sayılabiliyor.
5. Yardım bayrağı, yardımsız bileşenini 1'den 0,3'e düşürüyor: mevcut %10 ağırlıkla diğer koşullar eşitken 7 bölüm puanı fark.
6. İlk denemeden sonraki başarıda ilk-deneme bileşeni 1 yerine 0,4: mevcut %20 ağırlıkla diğer koşullar eşitken 12 bölüm puanı fark.
7. Endeks tüm kayıtların ortalaması. Yeni başarıların görünür etkisi zamanla azalıyor.
8. Reklam yardımlı normal oturumlar ZPE'ye yazılmıyor. Günlük bulmaca da normal bölüm hesabına dahil değil.
9. Bitiş kutlamasına geçerken Phase.Ended ayarlanıyor. Tespit edilen süre etkisi oyun içi hamle/özel taş animasyonlarıyla ilgilidir; bitiş kutlamasının da sayıldığı iddia edilmedi.

Formülü açıklayan varsayımsal örnekler (cihaz ölçümü değil):
- Referans çözüm 8 adımsa beklenen süre 6 + 4×8 = 38 saniye. 60 saniyelik başarı hızdan yaklaşık 6,3 puan; 80 saniyelik başarı yaklaşık 4,8 puan alır. Fark yaklaşık 1,6 bölüm puanıdır; genel ZPE değişimi değildir.
- 200 kayıt sonunda ortalama 70 iken yeni 90 puanlık kayıt ortalamayı yaklaşık 70,10 yapar. Tam sayı gösteriminde artış görünmeyebilir.

Öneriler:
- Animasyon ve zorunlu öğretici sürelerini performans ölçümünden çıkar.
- Sakin mantık oyunu kimliği nedeniyle hız bileşenini tümden kaldırmayı değerlendir.
- Oyunda üretilen özel taşlar stratejinin parçası olarak kalsın, yardım cezası almasın.
- Günlük ödülü kazanmış olmak tek başına ceza doğurmasın. Gerçek ek hamle kullanımı ve rekabet dengesi ayrı değerlendirilsin.
- ZPE biriktirilen ödül gibi sunulmasın; güncel performansı ölçsün. Kalıcı kazanımı yıldız, koleksiyon ve bölge ilerlemesi taşısın.
- Tüm geçmiş yerine yakın dönemi temsil eden model test edilsin; kolay bölümü tekrar ederek puan yükseltme riski ayrıca çözülsün. Yeni formül henüz seçilmedi.
- Animasyon kalitesi artırılmadan önce animasyon süresinin ZPE üzerindeki etkisi giderilsin.

### 6.2 Animasyon araştırması ve hareket dili

Araştırmadan alınan yaklaşım: Eyleme belirgin görsel/işitsel karşılık vermek, sonuçları okunur tutmak ve oyuncuyu uzun gösterilerle bekletmemek. Kaynakların tam oyun animasyonları kare kare ölçülmedi; aşağıdaki süreler rakiplerden alınmış ölçümler değil, Stone Burst için prototip önerileridir.

Stone Burst hareket dili: Ağırlığı olan parlak taş, kısa enerji birikimi, temiz kırılma ve alttaki katmanın belirgin açılması. Çok esneyen jöle hissinden kaçın. Mevcut roket bölünmesi ve Altın Rulo yıldırımı korunarak zamanlama, ağırlık, ışık ve ses eşleşmesi geliştirilebilir.

| An | Öneri | Başlangıç süresi |
|---|---|---|
| Geçerli grup dokunuşu | Hafif sıkışma, beyaz kenar ışığı | 80–120 ms |
| Katman soyulması | Yalnız üst katman parçalanır; alt taş okunur biçimde açılır | 180–260 ms |
| Zincir devamı | Yeni açılan grup kısa kenar parlamasıyla anlaşılır, sonra soyulur | Dalgalar arası 80–140 ms |
| Özel taş oluşumu | Grup ışığı birleşir, taş kısa yayla tepsiye yerleşir | 350–500 ms |
| Roket | Kısa gerilme, ikiye ayrılma, yönü okunur ışık izi | Ana hareket 350–600 ms |
| Altın Rulo | Hedef renkler işaretlenir, enerji bağlantılarıyla soyulur | 500–800 ms |
| Hedef tamamlanması | Simge hafif büyür, tamamlanma işareti yerleşir | 200–300 ms |
| Zafer | Kısa sakinleşme, yıldızların ses vurgularıyla sırayla yerleşmesi | 900–1.400 ms |

İlkeler:
- Zincirde ses kademeli yükselsin; uzun zincirde sürekli tizleşmesin.
- Parçacıklar ve parlamalar alt katmanları ve hamlenin sonucunu kapatmasın.
- Büyük kutlama büyük başarıya ayrılsın; her küçük eşleşmede ekran sarsılmasın.
- Kutlamayı hızlandırma/atlama ve azaltılmış hareket seçenekleri korunsun veya değerlendirilsin.
- Süreler cihaz üzerinde oynanabilirlik ve okunabilirlik testleriyle ayarlanmalı.

Araştırma kaynakları:
- GDC, Martin Jonasson ve Petri Purho, Juice It or Lose It: etkileşimleri tatmin edici kılan geri bildirim yaklaşımı. https://www.gdcvault.com/play/1016487/juice-it-or-lose
- King, Candy Crush Friends erişilebilirlik: büyük zincir/güçlendirici sarsıntıları, parçacıklar ve bunları azaltma seçenekleri. https://myling-d48d063d979546f4b73465486ee27d4d.zendesk.com/hc/en-us/articles/360013919137-Play-Your-Way-Accessibility-Features
- King topluluğu, animasyon hızlandırma geri bildirimleri: oyuncu anekdotlarıdır, bütün oyuncuları temsil eden ölçüm değildir. https://community.king.com/en/candy-crush-saga/discussion/428913/speed-up-animation-missing

### 6.3 Menü ikonlarının bütünlüğü

İncelenen ekran: docs/vr1/ekranlar/01_harita_ana_sayfa.jpg.
Gözlem: Hacimli harita ve parlak taşlara karşılık alt menüdeki Harita/Günlük ikonları daha düz ve genel amaçlı simgeler gibi duruyor. İşlevleri okunur kalmalı, görsel dilleri oyuna ait olmalı.

| İkon | Tasarım yönü |
|---|---|
| Harita | Krem küçük ada parçası, turkuaz kıvrımlı yol, üç bölüm düğümü; son düğümde mevcut taşlardan küçük işaret |
| Günlük | İnci renkli kalın takvim levhası, turkuaz bağlantılar, ortada mercan taşı ve küçük güneş işareti |
| Taşlar | Genel mavi elmas yerine mevcut koleksiyon taşlarından biri, küçük kaide üzerinde |
| ZPE | Finans grafiği yerine katmanlardan yükselen ustalık amblemi; anlamı yanında yazıyla korunur |
| Sıralama | Kupa korunabilir; malzeme, ışık ve kenar kalınlığı diğer ikonlarla eşleşir |

Ortak stil: Aynı ışık yönü, benzer perspektif, krem–turkuaz malzeme, kontrollü altın ayrıntılar. Seçili ikon kısa yükselme ve ışık geçişi yapabilir; hepsi sürekli hareket etmemeli. Menü işlevlerini değiştirme önerisi yoktur.

Referans: Dream Games'in Royal Match tasarım anlatımında taşlar, güçlendiriciler ve çevre aynı dünyanın nesneleri olarak ilişkilendirilir. Stone Burst için çıkarım, menü ikonlarının da aynı dünyaya ait görünmesidir. https://www.dreamgames.com/games/royal-match

### 6.4 Zaferden ana sayfaya kazanma hissi

Amaç: Oyuncu ödülün nereye gittiğini görsün ve haritada kalıcı izini bulsun.

Önerilen akış:
1. Bölüm biter: Yıldızların yanında gerçek bir başarı belirtilir; örneğin ilk denemede tamamlamak veya en iyi yıldız sonucunu geliştirmek.
2. Haritaya dönüş: Yalnız yeni kazanılan yıldızlar üst sayaca uçar.
3. Koleksiyon: Aynı kazanımın koleksiyon eşiğine katkısı kısa dolum animasyonuyla görünür; ayrı ödül gibi iki kez sayılmaz.
4. Yol: Tamamlanan bölümün yıldızları yerleşir, sonraki düğüm açılır, maskot ilerler.
5. Kalıcı iz: Açılan yol, yıldızlar ve kazanılan taş ekranda kalır.

Doğruluk: Daha önce 2 yıldız alınmış bölüm 3 yıldızla tamamlanırsa toplam artış +1'dir; 3 yeni yıldız kazanılmış gibi gösterilmez.

Kısa oturum özeti önerisi (değerler yalnız örnek):
“Bu oturumda: 4 yeni bölüm tamamladın · 9 yeni yıldız kazandın”
“Okyanus Taşı'na: 6 yıldız kaldı”

Gerçek kayıt farkları hesaplanmalı. Rekor ve benzeri ifadeler yalnız ölçülüyorsa gösterilmeli; yeni ölçümler gerekiyorsa ayrıca tasarlanmalı.

Büyük başarıların kalıcı görsel izi: Fenerin yanması, köprünün tamamlanması veya harita alanının canlanması. Hemen yeni para birimi ya da inşaat sistemi kurmak gerekmiyor.

### 6.5 Önerilen öncelik

1. ZPE'deki oyuncunun kontrolü dışındaki olumsuz etkileri gidermek.
2. Taşların hareket ve ses kalitesini geliştirmek.
3. Menü ikonlarını ortak görsel dilde birleştirmek.
4. Zaferden haritaya ödül akışını kurmak.

Bu sıra bir öneridir, uygulama kararı değildir.

## 7. İleride ele alınabilecek özel taş tasarımları

Kullanıcı, oyuna uygun özel taş görsel tasarımlarını ileride isteyebileceğini belirtti. Şu an tasarım üretme talebi veya onayı yoktur.
- Olası kapsam: Satır roketi, sütun roketi ve Altın Rulo.
- Mevcut oyunun malzeme, renk, ışık ve perspektif diliyle uyum hedeflenir.
- Küçük ekranda işlev ve yön okunabilirliği, normal taşlardan ayırt edilme ve animasyona uygunluk birlikte değerlendirilir.
- Yeni görsel, oyun mekaniğini kendiliğinden değiştirmez.
- Kullanıcı tasarım istediğinde önce görsel öneriler sunulur; seçilen tasarımın parçaları ve animasyon detayları daha sonra hazırlanabilir.
