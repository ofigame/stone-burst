# Tasarım teslimi — kod uygulama izni değildir

Bu paket yalnız tasarım incelemesi içindir. Kullanıcı ayrıca uygulama istemeden oyun koduna, Assets içine, sahneye, prefab'a, kaynak localization veya bölüm verisine yazma.

Yetkili referanslar:
1. Görsel dil:00_onaylanan_referans.png.
2. Sayfa kapsamı ve içerik:mevcut/ ve gerçek oyun kaynakları.
3. Yerleşim/font/boşluk/renk:detaylar/sayfalar/*.json, tasarim-tokenlari.json,01–06 MD.
4. Gerçek sprite boyutu ve9-slice:parcalar/asset-manifest.json; eski tahmini üretim boyutları yerine bu dosya.
5. Malzeme uygulaması:parcalar/KULLANIM.md.

35 maket görüntüsü yalnız görsel görünüş içindir. İçindeki raster yazıları, sayıları, taşları veya model çizimlerini yeni oyun içeriği diye kullanma. Mevcut sprite ve metin bağlamaları aynı kalır. Tahta şekli, delikler, taş sayısı ve katmanları değişmez. B01–B10 yalnız border malzemesi; zorunlu bölüm aralığı yok.

Kod uygulaması ayrıca yetkilendirilirse kabul kontrolü: bütün35sayfanın tüm mevcut butonları/koşulları korunmuş; yeni eylem yok; tüm modallar tam ekran scrim;5zemin oyun boyunca sabit; tümprofil uygulamalarında cell/holes/collider aynı; fontlar Nunito800/600, mevcutfallback; lokalizasyon ve ödüller kaynakla aynı.
