# Görsel kaynakları

Oyunda kullanılan görsellerin kaynakları. Bir görseli değiştirmek için dosyayı **aynı adla** buraya koy ve `powershell -File tools/ArtV6/prepare.ps1` çalıştır (oyuna hazırlanır, `Assets/KatKat/Resources/PremiumV6` altına yazılır).

| Klasör | Dosyalar | Oyundaki yeri |
|---|---|---|
| `taslar/` | `01-mercan-daire`, `02-turkuaz-ucgen`, `03-inci-dalga`, `04-mor-elmas` (1024, şeffaf) | 4 renk taşı (ince opal set) → `PremiumV6/stones` atlası |
| `mekanikler/` | `05-buz-saglam`, `06-buz-catlak`, `07-altin-rulo`, `08-sunger-halka`, `09-zarf-kaplama`, `10-gumus-atas` | Buz, Altın Rulo, sünger, zarf, ataş |
| `maskot/` | `01-onayli-maskot` (tam), `02-animasyon-govdesi` (yüzsüz gövde), `03-karsiya-bakan` (ikon için) | Haritada ve oyunda parçalı hareketli maskot; uygulama ikonu |
| `harita/` | `sahil_a/b/c`, `bahce_a/b/c`, `col_a/b/c` | Harita şeritleri (nehir/köprü ek yerleriyle döngü); `_kontrol-*.png` yol çizgisi ölçümü |
| `arayuz/` | `logo`, `region-coast/garden/desert`, `map`, `uygulama-ikonu`, `ofigame-logo` | Ana sayfa logosu, bölge kartı simgeleri ve içi boş tahta süsleri (fener, çeşme, kaya kemeri), alt menü harita simgesi, ikon, OFIGAME açılış logosu |
| `oyun_ici/` | `Premium/`, `PremiumV4/…` | Önceki paketlerden hâlâ kullanılan arayüz parçaları (düğmeler, paneller, simgeler, efektler, arka planlar, taş maskesi). Doğrudan `Resources`'tan kopya; değiştirmek için aynı yoldaki `Resources` dosyasını değiştir. |

Kullanılmayan eski görseller ve dokümanlar proje dışına arşivlendi: `E:\OFIGAME\OYUNLAR\KatKat_Arsiv_2026-11-06`.
