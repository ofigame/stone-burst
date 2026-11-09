# TriKat v9 · E paketi raporu (harita ölçeği)

Tarih: 2026-11-06. Kapsam: yol haritası E paketinin yerel kısmı: 1.000 ve sanal 10.000 bölümle ölçüm ve gereken hızlandırmalar.
Yapılmayanlar:
- Beş bölgeyi 500 bölümlük dünya altında gruplama: dünya metaverisi ürün kararı bekliyor.
- Nihai taş görselleri: henüz yok.

Canlı servis kullanılmadı. Üretim bölüm dosyaları değişmedi.

## Ölçüm yöntemi

- Ölçüm testi `Tests/PlayMode/ScalePerfTests.cs`. Yalnız istenince çalışır: `tools/unity.ps1 test-play -Filter ScalePerfTests`. Sonuçlar `Logs/batch/olcum_E.txt` dosyasına yazılır.
- Sanal 10.000 bölüm: gerçek 1.000 dosyanın metni 10 kez ayrıştırılır. Resources'a dosya eklenmez.
- Sanal kayıt: 10.000 tamamlanmış bölüm ve 10.000 yıldız kaydı.
- **Ortam:** Windows editörü, i5-11400H. Telefon değerleri daha yavaş olur; telefonda ölçülmedi.

## Sonuçlar

| Ölçüm | Önce | Sonra |
|---|---|---|
| Katalog açılışı, 1.000 bölüm | 590–820 ms, +29 MB | 12–15 ms (soğuk 59 ms), +0,1 MB |
| Hepsini açılışta okuma, 10.000 (eski yöntem) | 5,7 sn, +297 MB | gerekmiyor |
| Tek bölümün ilk açılışı | — | 0,5 ms |
| Kayıtta 10.000 bölümün yıldızını okuma | 1.197 ms | 2 ms |
| Kayıtta 10.000 bölümün tamamlandı bilgisini okuma | 1.341 ms | 1 ms |
| Harita açılışı, 10.000 bölüm (son bölüme yakın) | 1.566 ms | 144 ms |
| Harita açılışı, 1.000 bölüm | 114 ms | 95 ms |
| Kaydırma adımı, 10.000, haritanın üst ucu | ort. 5,6 ms, en çok 9,9 ms | ort. 0,2 ms, en çok 1,3 ms |
| Kaydırma adımı, 1.000 | ort. 0,2–0,6 ms | ort. 0,3 ms |
| 10.000 bölümlük kayıt dosyası, JSON yazma + okuma | 14 ms, 0,3 MB | aynı |

## Yapılan değişiklikler

- **Katalog (`Scripts/Game/LevelCatalog.cs`):**
  - Açılışta yalnız dosya adları alınır.
  - Bölüm ilk istendiğinde okunur ve saklanır.
  - Bölüm kimliği dosya adıdır (`IdAt`).
  - `IndexOf` sözlükle çalışır.
  - Okunamayan bölüm hata ekranı açar; eskiden sessizce atlanıyordu ve sonraki bölümlerin sırası kayıyordu.
  - `ParseAll()` bütün bölümleri okur ve kimlik = dosya adı kuralını denetler; `GameFlowTests` bunu kullanır.
- **Kayıt (`Scripts/Save/SaveData.cs`):**
  - `IsCompleted` ve `StarsFor` dosyaya yazılmayan arama tablolarıyla çalışır.
  - Liste değişince tablolar yeniden kurulur; bulunan sonuç listeyle doğrulanır.
  - Kayıt dosyasının biçimi değişmedi.
- **Harita (`Scripts/UI/MenuView.cs`):**
  - İlk görünür düğüm ikili aramayla bulunur.
  - Yol parçaları görünür aralıktan başlar ve aralık bitince durur.
  - Bölge hesabı doğrudan formülle yapılır; eskiden her düğümde 0'dan döngüyle bulunuyordu.
- **GameController:** Bölüm kimliği gereken yerler (yıldız toplamı, tamamlanan sayısı, harita) bölümü okumadan `IdAt` kullanır.

## Kalan konular (değerlendirme)

- **10.000 bölümde açılış maliyeti:** Açılışta dosya adlarını almak için `Resources.LoadAll` bütün metinleri bir an belleğe alıyor, sonra bırakıyor. 1.000 bölümde 8 MB ve 8–59 ms. 10.000 bölümde yaklaşık 82 MB anlık bellek olur. 10.000'e çıkmadan önce bölüm listesini tek bir dizin dosyasından okumak ya da bölümleri paketler hâlinde yüklemek (Addressables / AssetBundle) gerekir. Yeni paket gerektirebilir; ayrı karar.
- **`ThemeDefinition.RegionSize`:** Değer 100'de kaldı. Yol haritası 500 yapılmasını ve mevcut temaların topluca değiştirilmesini yasaklıyor. Dünya gruplaması dünya metaverisiyle eklenecek.
- **Taş görselleri:** Nihai taş görselleri gelince yalnız `stones.json` içindeki `artKey` eşlemesi değişecek.
