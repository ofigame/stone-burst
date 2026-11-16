# Konsept pilotu: 10 konsept, 10 bölüm (2026-11-15)

## İstek

Kullanıcının isteği (2026-11-15) dört parçadan oluşuyordu:
- Zorluk 1000 bölüm boyunca dengesiz ve yeterince artmıyor.
- Oyunun "mantık + yarış" olması isteniyor. Konseptler bölümlere dağıtılacak, aynı konsept arka arkaya gelmeyecek.
- İlk fikir "çürüyen zemin" oldu: alt kısım çürür, taşlar düşer, alttan temizlemek gerekir.
- "10 konsept yapsak", "mantık zeka gerektiren bulmaca olacaklar aynı zamanda. Haydi 10 bölüm yap pilota koy kontrol edeyim."

## Temel karar: tehdit hamleye bağlı

- **Tehdit her harcanan hamlede bir adım ilerler**, gerçek zamanla ilerlemez. Oyuncu düşünürken saat işlemez; mantık oyunu bozulmaz.
- Çözücü her bölümü doğrular ve ipucu çalışır.
- Tepsi kullanımı hamle harcamadığı için tehdidi ilerletmez.
- Tehdit **hep önceden görünür**:
  - çökecek satır çatlaklı ve sayaçlı;
  - fitil sayacı görünür;
  - kuzgunun yolu ve yuvası görünür;
  - yağmurun düşeceği yığın görünür.
- Hedef kartının altında bir durum satırı her hamlede güncellenir.
- Tehdit kazanırsa yeni bir kayıp durumu oluşur (`LostHazard`). Kayıp kartı nedenini söyler ve "Geri Al" düğmesi sunar.
- Hedef rengin tahtada kalanı hedefe yetmezse bölüm hemen kaybedilir. Bu durumlar düşen, çalınan ya da yanlış zamanda harcanan taşlardır. Oyuncu boşuna oynamaz.

## 10 konsept

| # | Konsept | Kural (oyuncuya gösterilen) | Oyuncuyu zorlayan |
|---|---|---|---|
| 1 | Çürüyen Zemin | Her K hamlede en alt sıra çöker, taşlar düşer. Alt sırada boşaltılan her yığın çöküşü 1 hamle erteler. | Önce alttan temizlemek |
| 2 | Fitil | Sayaçlı yığın her hamlede 1 azalır. 0 olmadan yığın tamamen boşaltılmalı. | Doğru yığını zamanında boşaltmak |
| 3 | Dönen Hedef | Yalnız etkin renk sayılır. Etkin renk her K hamlede değişir. | Zamanlama |
| 4 | Kuzgun | Her hamlede bir hücre ilerler ve üst taşı çalar. Önündeki yığın boşsa bekler. Yuvaya varırsa kayıp. | Yolunu kesmek |
| 5 | Yağmur | Her K hamlede işaretli yığına taş düşer. Bir yığın sınırı aşarsa kayıp. | Yığınları alçak tutmak |
| 6 | Gölge Rakip | Rakip her hamlede 1 adım ilerler. 5+ taşlık grup patlatılan hamlede 1 adım geri düşer. | Büyük grup kurmak |
| 7 | Yayılan Yosun | Yosun her K hamlede komşu yığına yayılır, yosunlu yığın seçilemez. Yanında soyma yapılınca temizlenir. Sınıra ulaşırsa kayıp. | Kaynağında söndürmek |
| 8 | Balonlar | Balonlar her K hamlede bir sıra yükselir. Altındaki yığın boşalınca patlar ve toplanır. Tahtadan çıkarsa kayıp. | Balonun yolunu açmak |
| 9 | Sipariş Kuyruğu | Aşamalı hedeflerin her birinin kendi hamle süresi var. Süre biterse sipariş yanar. | Sıralama |
| 10 | Kombo Ateşi | Zincirli hamle ateşi doldurur, zincirsiz hamle 1 azaltır. Ateş sönerse kayıp. | Ritim: zinciri sürdürmek |

## Bölümler

- Bölümler `Resources/Concept/concept_01..10.json` dosyalarında; üreten komut `LevelLab concept`.
- Kabul şartı ve ölçümler [URETIM.md](URETIM.md) dosyasında. Özetle: açgözlü bot tehditle kaybeder, tehditsiz kazanır. Yani her bölümde tehdide göre plan yapmak gerekir.

## Nerede

- **Haritada:** Pilot düğmesinin altında kırmızı etiketli **Konsept** düğmesi var.
  - Pilot ile aynı koşulla görünür: deneme derlemesi (`build-android-vr1`) ve editör. Mağaza derlemesinde gizlidir.
  - Deneme seti akışıyla oynanır: kural kartı, uzun basışla katman görünümü, 2 kayıptan sonra ipucu.
  - Ana ilerlemeye, yıldızlara ve sıralamaya yazılmaz.
- **Kod:**
  - `Core/Hazards.cs`: kurallar.
  - `Levels/HazardData.cs`: JSON biçimi.
  - `View/HazardView.cs` ve `View/HazardArt.cs`: tahta üstü işaretler.
  - `UI/HudView.Hazard.cs`: durum satırı.
  - `Game/GameController.Concept.cs`: akış, kayıp kartı.
- **Ekran görüntüleri:** `docs/captures/konsept/`, komut `tools/unity.ps1 capture-concept`.

## Sınırlar (pilot)

- Görseller kodla çizildi: çatlak, fitil, kuzgun, damla, yosun ve balon. Görsel kabul sonrası çizimle değiştirilebilir.
- Çöküş, yağmur ve yayılma anlık değişir; hücreler kısa süre parlar ve bildirim çıkar. Ayrı düşme animasyonu yok.
- Metinler Türkçe ve İngilizce. Diğer diller İngilizceye düşer.
- Bir bölümde bir konsept var. Karışımlar (600+) ve 1000 bölümlük zorluk ölçümü sonraki adım.
- Zorluk ve eğlence iddiası yok. Ölçümler bot ve çözücüyle yapıldı; oyuncu testi cihazda kullanıcıda.

## Kullanıcı seçimi ve görselleştirme (2026-11-15)

Kullanıcı **Dönen Hedef, Kuzgun, Balonlar, Sipariş Kuyruğu** konseptlerini beğendi; görevin oyunda görsellerle anlatılmasını istedi.

- **Görev şeridi** (hedef kartının altı): üstte simge ve sayı, altta `ne yapmalı` satırı.
  - Dönen Hedef: [etkin taş] `2 hamle bu taş sayılır` › [sıradaki taş]; alt satır `Diğer taşları da patlatabilirsin ama şimdi hedefe sayılmaz`. Etkin hedef çipi altın halkalı, diğeri soluk.
  - Kuzgun: [kuzgun] `yuvaya N adım` [yuva]; alt satır `Oklu yoldaki bir yığını tamamen boşalt, kuzgun orada bekler`.
  - Balonlar: [balon] `N balon · K hamle sonra yükselir`; alt satır `Balonun altındaki yığını boşalt; balon patlar ve toplanır`.
  - Sipariş: [saat] `Sipariş 2/3: [taş] 6 · 2 hamle`.
- **Görseller** (`View/HazardPaint.cs`): Higgsfield hesabı askıda olduğu için kodla, oyunun parlak kabartmalı tarzında. Kuzgun gittiği yöne bakar; yol altın yön oklarıyla, yuva son hücrede örgü çanak ve yumurtalarla; balon parlak, kaçmak üzereyken altın ve kırmızı halkalı. Önizleme `docs/captures/konsept/gorsel/`.
- Hesap düzelince aynı görseller Higgsfield ile üretilip değiştirilebilir.
