# Parça kullanım sözleşmesi

Bu klasördeki PNG'ler yazısız gerçek alfa kanallı parçalardır. Tüm ölçüler asset-manifest.json içinde, PNG pikseli cinsindendir. Tam ekran maketlerden metin veya taş kesilerek sprite yapılmaz.

## Panel ve buton

| Parça | PNG ölçüsü | L / T / R / B dilim sınırı | Kullanım |
|---|---|---|---|
| modal-seramik | 635×752 | 150 / 125 / 150 / 65 | 9-slice, yazı ayrı |
| kart-seramik | 439×569 | 50 / 50 / 50 / 50 | 9-slice, yazı ayrı |
| buton-birincil | 619×161 | 75 / 40 / 75 / 40 | 9-slice, yazı ayrı |
| buton-ikincil | 491×162 | 75 / 40 / 75 / 40 | 9-slice, yazı ayrı |
| baslik-plakasi | 736×141 | 75 / 35 / 75 / 35 | 9-slice, yazı ayrı |
| ikon-dugmesi | 219×221 | 0 / 0 / 0 / 0 | Oranı korunur; 9-slice yok |

9slice klasöründe beş parçanın dokuzar fiziksel PNG dilimi bulunur. Dosya adı satır-sütun: 0-0 üst sol, 0-1 üst, 0-2 üst sağ; 1-0 sol, 1-1 merkez, 1-2 sağ; 2-0 alt sol, 2-1 alt, 2-2 alt sağ. Aynı anda ya tam sprite + border metadatası ya dokuz parça kullanılır; üst üste kullanılmaz. Köşeler boyut değiştirmez; yanlar tek eksende uzar; merkez iki eksende uzar. Normal durum tint #FFFFFF. Baked alt gölgeye ikinci güçlü gölge ekleme.

Modal sprite üst omuzu kendi içinde taşır; ayrıca ikinci crest eklenmez. Başka dosyadaki eski 1024×1536 veya 1024×192 değerleri üretim hedefiydi; gerçek PNG ve kesim ölçüsünde BU MANIFEST geçerlidir. UI'nin 1080×1920 mantıksal yerleşim ölçüleri bundan bağımsızdır. Minimum sprite genişliği L+R+32, yüksekliği T+B+32; gerekiyorsa tüm panel ölçeği birlikte azaltılır.

Metin sprite içine basılmaz. Nunito ExtraBold 800 başlık ve buton; Nunito SemiBold 600 açıklama. Düğme yazısı yatay/dikey merkezde, sağ/sol en az32 mantıksal px; ikon varsa solda56px, ikon–yazı16px. 116px buton yüksekliği, 44px font,54px satır; butonlar arası24px. Basma anında mevcut sprite RGB tint #D9E8E2, y+2px; bırakınca #FFFFFF/y0. Devre dışı tint #B3BFBA, metin #5D6E68; state yeni eylem yaratmaz.

## 10 çerçeve profili

B01–B10-profil.png yalnız malzeme şerididir; tahtanın alpha maskesi, cell grid veya halka şekli değildir. PNG'nin üstü iç dudak, altı dış yan yüzdür. Sabit dikdörtgene gerip tahta üretme. Mevcut konturu ve tüm iç delikleri aynen kullan. Profil toplam genişliği24 mantıksal px: dudak4, pirinç3, üst pah10, yan yüz7. Mevcut boşluk yetersizse genişlik küçülür; taşlar kaydırılmaz.

UV: U kontur üzerindeki yay uzunluğu, V içeriden dışarı0–1. Dış köşede profil, mevcut köşenin normaline göre döner. İçbükey köşede bevel birleşim; en fazla1.5 miter, delik kapanmaz. Tahtanın deliğine bakan kenarda V yönü iç boşluğa doğru çevrilir. Aydınlatma ekran sol üstten; köşeye yeni taş/bulb/çıkıntı ekleme. B05 saydam delik oluşturmaz.

Şeritler malzeme örnekleridir; yatay dikişsiz repeat oldukları doğrulanmış değildir. Bu nedenle wrap Clamp. Mevcut renderer kontur boyunca tekrar gerektiriyorsa düz renk/pah reçetesi taban olarak kullanılır ve oymanın ayrı decal aralığı detaylar/03_ZEMIN_VE_BORDER.md'deki periyoda göre yerleştirilir. Yeni tahta mesh'i veya collider üretme. Bu teslim renderer kodu içermez.

## Arkaplan

Z1–Z5 tam ekran opaque PNG'dir, Repeat yerine Clamp/Cover kullan. Oran korunur, SafeArea dışına da uzanır; metin güvenli alanda kalır. Rastgele seçim eşit1/5, bağımsız kozmetik durum; aynı oyun/retry süresince sabit. Tahta delikleri aynı ekran zeminini gösterir. Taşların altındaki iç yüzey #FFF3DE; daha hafif doku istenirse aynı zemin alpha0.08 ile bu düz rengin üstüne bindirilir. Taş görselleri tintlenmez.

## Modal karartma

Tam ekran düz #111C24, alpha0.72; SafeArea ile kırpma yok. İkinci modalda toplam0.82 için ikinci kat alpha=(0.82-0.72)/(1-0.72)=0.357142857. Önceki panel de ikinci scrim altında kalır. Ön plan panel, mevcut maskot, yazı ve butonlar karartılmaz. Modalın dışına basmanın davranışı mevcut kaynakta neyse aynı kalır.
