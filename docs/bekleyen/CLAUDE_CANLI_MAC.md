# Stone Burst canlı maç prototipi

Claude Code uygulama belgesi · 13 Kasım 2026

Proje kökü E:/OFIGAME/OYUNLAR/KatKat. Önce bu dosyayı ve [oyun mantığını](OYUN_MANTIGI.md) oku. Canlı maç, mevcut bulmaca modundan ayrı bir oyun modu olarak tasarlanır. Bu belge ilk oynanabilir prototipin kapsamını tarif eder.

## 1. Kararlaştırılan kurallar

- Maç iki oyuncu arasında sırayla oynanır.
- Her maçta iki oyuncunun başlangıç tahtası bire bir aynıdır. Hücre maskesi, üst renkler ve alt renkler aynı koordinatlarda eşleşir. Oyun sırasında tahtalar bağımsız değişir.
- Her etkin hücre başlangıçta tam **iki katmandır**. Dört mevcut Stone Burst rengi ve görseli kullanılır.
- **Canlı maçta zincir yoktur.** Eşleşen gruptan tek katman soyulur. Alttan çıkan taşlar bu hamlede ikinci bir patlama tetiklemez.
- Maçtan maça çok sayıda farklı ortak dizilim kullanılacak. Çeşitlilik, tamamlanabilen grupların konumunu ve oyuncunun saldırı ya da puan alma kararını değiştirmeli.

## 2. Başlangıç dizilimi

Konuştuğumuz açılış kurallarını prototipin üretim filtresi olarak kullan:

1. Açılışta kenardan bağlı üç veya daha fazla aynı renk bulunmaz. Köşegen temas sayılmaz. Tekliler ve ikililer serbesttir. Bütün tahtanın ikililerden oluşması gerekmez.
2. İlk el iki tarafta aynı olur. Eldeki taşlarla **en az iki farklı renkte**, farklı hedef hücrelerinde üçlü veya daha büyük grup tamamlanabilmeli.
3. Bir yasal rakip değişimi yapıldığında savunan tarafın elinde en az bir hemen puan getiren kendi tahta hamlesi kalmalı. Bu kontrolü her yasal açılış saldırısını ayrı ayrı simüle ederek yap. İki ayrı hedef hücresinin bulunması tek başına bu koşulu sağlamaz.
4. Fırsatlar düz çizgi ve L biçimi dahil kenardan bağlı şekillerde olabilir. Uçtan tamamlama örneği mercan–mercan–turkuaz dizisinin son hücresine mercan koymaktır. Aradan tamamlama örneği mercan–turkuaz–mercan dizisinin orta hücresine mercan koymaktır.

Her aday için yasal kendi tahta hamlelerini gerçek eşleşme kurallarıyla hesapla. Bir fırsatı el rengi, hedef koordinat ve oluşan grup ile tanımla. Aynı renkteki birden çok el taşı aynı hedefte ek fırsat sayılmaz.

## 3. Bir turun akışı

Aşağıdaki ayrıntılar ilk prototip için önerilen varsayımlardır. Maç kurallarını yapılandırmada tut ve sonuç raporunda kullanılan değerleri belirt.

1. Oyuncu elindeki bir rengi ve hedef tahtadaki etkin, dolu hücreyi seçer.
2. Farklı renkteki üst taş seçilen el rengiyle değiştirilir. Alt katman aynı kalır. Eldeki taş harcanır. Eski üst taş ele dönmez. Değişim sırasında çıkarılan eski üst taş puan getirmez.
3. **Kendi tahtasında** yeni renkteki ve hedef hücreyi içeren kenar bağlantılı grup en az üç hücreyse grubun her hücresinden bir katman soyulur. Oyuncu soyulan her katman için bir puan alır. Eşleşme yoksa değişim hazırlık hamlesi olarak kalır.
4. **Rakibin tahtasında** yalnız renk değişir. Puan ve patlama oluşmaz. İlk prototipte hedef hücrede yeni üçlü oluşturan saldırı geçersizdir. Tahtanın başka yerinde önceki turdan kalmış bir hazır üçlü, saldırıyı tek başına geçersiz kılmaz.
5. Kabul edilen hamle sırayı rakibe verir. Reddedilen hamle eli, tahtayı, puanı veya sırayı etkilemez. Hücre iki katmanını kaybettiğinde boşalır ve değişim hedefi olamaz.

### Patlama sonrası hazır üçlü

Kesin kural, hazır grubun kendiliğinden patlamamasıdır. **Doğrudan dokunarak patlatmanın serbest olup olmayacağı henüz seçilmedi.** İlk prototip için önerim bunu ayrı bir deneme ayarı olarak tutmak. Açık ayarda oyuncu kendi turunda hazır gruba dokunarak bir katman soyabilir. Bu, turu harcar ve el taşı harcamaz. Kapalı ayarda yalnız elden yapılan değişim puan hamlesi başlatır. İki seçeneğin tempo ve saldırı dengesi aynı dizilimde karşılaştırılabilir.

### Maç sonu ve el

Prototipte her iki oyuncuya eşit tur hakkı ver. Kendi tahtasında en çok katmanı soyan kazanır. Eşit puan beraberliktir. Sekiz el yuvası çizimdeki yerleşim referansıdır. El boyutu, taş yenileme, tur sayısı ve süre sınırı ürün kararı olarak açık. Hızlı yerel test için **sekiz taşlık aynı ilk el, yenilemesiz el ve oyuncu başına sekiz tur** kullanılabilir. Yasal hamlesi olmayan oyuncu pas geçer ve tur hakkı kullanılır. Bu değerleri canlı maç standardı olarak sabitleme.

## 4. Çok sayıda anlamlı dizilim

- İlk sürümde doğrulanmış bir tahta havuzu kullan. Tohumlu üretici her aday için hücre maskesi, iki renk katmanı ve ilk eli üretir. Doğrulamayı bitirmeden adayı havuza alma.
- Üretimde açılış türlerini karıştır. Uçtan tamamlama, aradan tamamlama, L biçimli grup, farklı bölgelerde iki fırsat ve üçlüyle dörtlü arasında seçim örnek türlerdir.
- Aynı dizilimin renk adları değiştirilmiş kopyalarını ayıkla. Aynalama veya döndürme seçeneklerini, tahta maskesi ve kurallar o dönüşüm altında eşdeğerse aynı aile olarak say. Karşılaştırmada ilk eli ve alt katmanı da hesaba kat.
- Tahta çeşitliliğini açılıştaki geçerli hedef sayısı, grup büyüklükleri, fırsatların konumu ve tek saldırıdan sonra kalan seçeneklerle raporla. Tohum sayısını çeşitlilik ölçüsü olarak kullanma.
- Maç kaydında dizilim kimliği, üretim sürümü, başlangıç eli ve ilk oynayan taraf bulunur. Aynı dizilimle sıra ters çevrilerek yapılan maçlarda ilk oyuncunun avantajı ölçülür.

## 5. Ekran ve prototip sırası

Üstte rakibin tahtası, altta kendi tahta, en altta el taşları. Sıra sahibi ve iki tarafın puanı görünür. El rengi seçildiğinde kendi tahtasında oluşacak grup, rakip tahtasında ise değişecek hücre önizlenir. Hazır grup göstergesi, doğrudan dokunma seçeneğinin açık olup olmadığıyla uyumlu olmalı. İlk yerel denemede iki eli açık göstermek önerilir. Rakip elinin görünürlüğü canlı sürüm için açık karardır.

Önce tek cihazda iki oyuncunun sırayla kullanabildiği bir prototip kur. Mevcut renk, hücre ve grup bulma yardımcılarını uygun yerlerde yeniden kullan. Zincir çözümünü ana oyun için değiştirme. Maç durumu, geçerli komutlar ve puan hesabı arayüzden bağımsız doğrulanabilmeli.

Ağlı prototipe geçildiğinde sunucu başlangıç tahtasını ve elleri belirler, komutun sıra sahibini ve geçerliliğini denetler, sonuç durumunu iki oyuncuya gönderir. Yinelenen komut ikinci kez işlenmez. Kopma, yeniden bağlanma ve zaman aşımı kuralları ağlı sürümün ürün kararlarıdır.

## 6. Kabul kontrolleri

| Senaryo | Beklenen sonuç |
|---|---|
| Maç açılışı | İki oyuncuda aynı maske, iki katman ve aynı ilk el |
| Hazır üçlü içeren aday | Üretim filtresi reddeder |
| Açılıştaki tek saldırı | Rakibin en az bir puan hamlesi kalır |
| Kendi tahtasında üçlü | Hedefi içeren grup bir kez soyulur ve doğru puanı verir |
| Alt katmandan yeni üçlü çıkar | Taşlar yerinde kalır. Zincir oluşmaz |
| Hazır üçlüye dokunma | Deneme ayarına uygun çalışır |
| Rakipte geçerli değişim | Yalnız üst renk değişir. Puan oluşmaz ve sıra ilerler |
| Aynı renk, boş hücre veya rakipte yeni üçlü | Durum değişmeden komut reddedilir |
| Hazırlık hamlesi | Renk değişir, puan oluşmaz ve sıra ilerler |
| Son katman soyulur | Hücre boşalır ve artık değiştirilemez |
| Maç sonucu | Her oyuncu aynı sayıda tur oynar. Soyulan katman sayısı sonucu belirler |
| Dizilim havuzu | Eşdeğer renk kopyaları ayıklanır ve doğrulama tekrar üretilebilir |

## Claude Code’a başlangıç istemi

> Bu belgeyi ve OYUN_MANTIGI.md dosyasını oku. Canlı maç için mevcut motorda yeniden kullanılabilecek kodu incele. Önce doğrulanmış ortak tahta havuzu ve tek cihazda sırayla oynanan prototipi hazırla. İki katmanı ve zincirsiz çözümü zorunlu tut. Prototip varsayımlarını yapılandırmaya al. Kabul kontrollerini çalıştır ve ilk oyuncu avantajını, maç süresini ve saldırı tercihlerini ölçmek için kullandığın yöntemi raporla.
