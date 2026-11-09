# TriKat v10 · Koleksiyon görsel parçaları

Bu paketteki PNG'leri, `docs/v10/TriKat-Koleksiyon-Claude` referansı ve uygulama talimatıyla birlikte kullan. Görseller referans esas alınarak bağımsız varlıklar olarak yeniden üretildi. Unity'de yerleşim karşılaştırması için tam ekran referansı esas al.

| Dosya | Bağlantı ve çizim sırası |
|---|---|
| 01-alt-menu-kabugu.png | Dört sekmenin ortak dekor zemini. İkon, ayraç, aktif sekme kapsülü ve TMP etiketleri önde ayrı katmanlar. Mevcut tıklama alanlarını kullan. |
| 02-koleksiyon-cercevesi.png | Koleksiyon panelinin altın dış çerçevesi ve mor iç zemini. Kartların ScrollRect maskesi altın kenarın içinde kalır. Alt elmas rozet ve dalgalı uç korunur. |
| 03-koleksiyon-arkaplan.png | Yalnız Koleksiyon sayfasına bağlanan yazısız arka plan. Cover ölçeklemesi kullan. Panel arkasında, tüm dekor ve tıklama katmanlarının altında. |
| 04-acik-kristal-sandik.png | Koleksiyon başlığının üstündeki açık sandık. Mor kurdelenin arkasında, arka planın önünde. |
| 05-oduller-kasa-ikonu.png | Alt menüde Ödüller sekmesinin kırmızı-altın kapalı kasa ikonu. Mevcut sekme bağlantısını kullan. |
| 06-koleksiyon-kurdelesi.png | Açık sandığın önündeki yazısız mor-altın kurdele. `Koleksiyon` metnini mevcut yerelleştirme üzerinden çiz. |
| 07-kilitli-kart-buz-kabugu.png | Kilitli kartta taş siluetinin önüne, kilit, soru rozeti, ad ve koşul metninin arkasına yerleşen buzlu kenar. Kilitli durumun mevcut veri kuralları korunur. |

## Unity kabulü

- Şeffaf parçaları tek Sprite (2D and UI), alpha açık, sRGB, ilk görsel karşılaştırmada kayıpsız veya düşük sıkıştırma ile içe aktar. Uzun kenar 2048'i aşıyorsa yüksek çözünürlüklü kaynak için Max Size 4096. Arka plan ayrı opak görseldir. Simgelerde en-boy oranını koru.
- Dalgalı kabuk ve elmas rozetini düz kapsül veya düz dikdörtgenle değiştirme. Yatayda en-boy oranını mümkün olduğunca koru. Farklı oranlarda kenarları koruyan yerleşim kullan.
- Alt menüde `05` kapalı ikon. Üst başlıkta `04` açık sandık. İkisini karıştırma. Görsellerde metin bulunmaz. Tüm metinler canlı ve yerelleştirilmiş kalır.
- Ortak alt menünün kabuğu ve kasa ikonu güncellenebilir. Haritanın yolu, kaydırma davranışı, düğümleri ve arka planı korunur. Oyun taşları ve koleksiyon kazanım verileri aynı kalır.
- 941×1672 referans oranında ve dar çentikli telefonda ekran görüntüsü al. Koleksiyon çerçevesinin alt elması, kilitli kart buz kenarı, başlık sandığı ve alt menü kasasını ayrı ayrı karşılaştır. Kalan görsel farkları raporla.
