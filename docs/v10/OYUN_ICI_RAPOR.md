# v10 Oyun içi kabuk, alt katman okunurluğu ve Günlük modalı

Tarih: 2026-11-07.

- Referans: `TriKat-Oyun-Ici-Claude/01-onayli-referans.png` (941×1672).
- Görüntüler `docs/captures/v10/oyun/` altında: önce `once_*`, sonra `sonra_*`. Yeniden üretmek için `tools/unity.ps1 capture-game-before` ve `capture-game-after`.
- Önce/sonra aynı bölüm ve aynı durumda alındı:
  - Bölüm 38 açılış: 941×1672, 1080×1920, çentikli 1080×2400.
  - Bölüm 38 soyulma karesi: ilk adım, 12 kare sonra.
  - Bölüm 219: 5 hedef, 7×9 şekilli tahta, ortasında boşluk, tepsi dolu (roket + Altın Rulo).
- Tepsi içeriği yalnız görüntü için oturuma yazıldı. Bölüm dosyalarına ve kayda bir şey yazılmadı.

## Oyun içi kabuk

| Parça | Ne oldu |
|---|---|
| Arka plan | Mor-mavi merkez, kenarlarda lavanta bulutları, birkaç sabit ışık (`Scripts/View/GameShellArt.cs`). **Bütün oyun temalarında** kullanılıyor. Harita temaları (sahil, bahçe, çöl) değişmedi. `GameShellArt.Enabled = false` ile eski bulanık tema resmine dönülür. |
| Üst | Sol üstte duraklat, sağ üstte geri al (turkuaz gövde, altın kenar). Ortada maskotlu mor hamle kurdelesi ("Hamle" beyaz, sayı altın), arkasında yapraklar ve iki yıldız. Altında yıldız kabuğu (krem/altın gövde, koyu mor oyuk, sarı dolum, üç eşik yıldızı). Onun altında hedef kabuğu. |
| Geri al | Alttan üste taşındı. Ekranda tek geri al var; aksiyon ve kurallar aynı (`UndoPressed`). |
| Tahta | Krem iç tepsi, sarı-altın kontur, aşağı yönlü yumuşak gölge. Kontur mantıksal matristen türüyor; şekilli tahtalarda boşluklar korunuyor. Hücre merkezi, adım, isabet alanı ve yığın geometrisi konturdan bağımsız. |
| Alt | Dalgalı krem/altın kabuk, mor dairesel yuvalar. Yalnız bölümde gerçekten olan kontroller görünüyor: rulo, 4 tepsi yuvası, boya. Bunlar eşit aralıkla diziliyor. Boş, dolu, seçili ve devre dışı durumlar ayrı tonda. Boş kabuk tıklama yakalamıyor. |
| Korunanlar | Mekanikler, güçler, hedef simgeleri, maskot akışı (göz takibi, heyecan, son hamle endişesi), bölüm JSON'ları. |

Referanstaki çekiç, roket, renk topu ve bomba gibi rozetler eklenmedi. Sahte düğme, mağaza ya da sayaç yok.

## Alt katman okunurluğu (kullanıcı isteği)

"Altta olan taşların renkleri bazen anlaşılmıyor" isteği üzerine bilerek yapılan istisna. Oyun içi belgesindeki "taş görsellerine dokunma" kuralının dışında; yalnız alt katmanların yan şeritlerini etkiliyor.

| Dosya | Değişiklik |
|---|---|
| `Scripts/View/LayerPalette.cs` | Yan şerit rengi artık her renk için ayrı ve doygun. Eskiden yüz renginin ×0,9'u kullanılıyordu. Krem taşın şeridi sıcak bej oldu, böylece krem zeminden ayrılıyor. |
| `Resources/Shaders/KatKatLayer.shader` | Yan şeridin alta doğru koyulaşması azaltıldı (0,70–0,93 → 0,84–1,0). |
| `Scripts/View/StackView.cs` | Katman kalınlığı 0,084 → 0,10. Şeritler daha geniş; tahta düzeni yığın yüksekliğinden kendiliğinden hesaplanıyor. |

Değişmeyenler: üst yüz çizimleri, işaret atlası, satin/tile PNG'leri, kaplamalar (buz, zarf, sünger, ataş, yıldız, Altın Rulo) ve `TileSkin`. Bunlar SHA-256 ile önce/sonra karşılaştırıldı; 17 dosyanın 14'ü aynı, yalnız yukarıdaki 3 dosya değişti.

Yakın plan karşılaştırma: `katman_yakin_once_sonra.png`.

## Günlük modalı (kullanıcı isteği ve görseli)

- Günlük artık ayrı sayfa değil, haritanın üstünde modal.
- Görünüm kullanıcının görseline göre:
  - takvimli tarih kapsülü
  - lavanta seviye kapsülü ("Çok zor" kırmızı)
  - kalın lacivert durum yazısı
  - taş/sandık satırı
  - yıldızlı ayraç
  - yeşil oyna düğmesi
- Kapatma: sağ üst düğme ya da karartmaya dokunma. Harita yerinde kalıyor.
- Koleksiyondan Günlük sekmesine basınca önce harita kuruluyor, modal onun üstünde açılıyor.
- Deneme, reklam ve güç akışları değişmedi.

## Testler

- CoreTests 165/165, EditMode 165/165.
- PlayMode 24/24: günlük, kayıt, katalog, dünya, açılış ve soyulma laboratuvarı. Günlük testleri modal davranışına göre güncellendi: kapat, karartma, sekmeden açılış.
- Tam PlayMode (1000 bölüm akışı) ve yeni exe derlemesi arka planda çalışıyor; sonuç DURUM'a yazılacak.
- Cihazda denenmedi.

## Eksik final görseller (şu an koddan çizilmiş GEÇİCİ sürüm)

| Parça | Hedef dosya | Kullanım ölçüsü |
|---|---|---|
| Oyun arka planı (mor bulutlar, yazısız) | `Resources/PremiumV6/game/bg.png` | tam ekran, 1080 × 2400 ya da 2× |
| Mor hamle kurdelesi (dikişli) | `Resources/PremiumV6/game/moves-ribbon.png` | 377 × 106 |
| Yaprak demeti (sol; sağ aynalanır) | `Resources/PremiumV6/game/leaves.png` | 150 × 110 |
| Alt kabuk | Ana sayfayla ortak: `Resources/PremiumV6/shell/bottom-bar.png` | 1080 × 282 |

Yıldız kabuğu, hedef kabuğu, mor yuvalar ve yuvarlak düğmeler kodla kurulan canlı arayüz öğeleri; ayrı görsel gerekmiyor.

## Referanstan kalan farklar

- **Tahta şekli:** Referanstaki basamaklı tahta ve üstteki boş yuvalar örnek. Bölüm verisi değiştirilmedi; her bölüm kendi şekliyle görünüyor.
- **Geçici çizimler:** Arka plan, kurdele ve yapraklar referanstaki illüstrasyon kalitesinde değil.
- **Alt yuvalar:** Referansta dört güç rozeti ve artı işaretleri var. Bizde yalnız bölümdeki gerçek kontroller görünüyor; özel taşsız bölümde alt kabuk boş kalabiliyor.
- **Hedef çipleri:** Mevcut krem çip görünümünde. Referanstaki ince dikey ayraçlar eklenmedi.
