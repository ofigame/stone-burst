# Modallar, durumlar ve görsel üretim sözleşmesi

## Katman sırası

- 0: backdrop
- 100: mapOrBoard
- 200: hud
- 250: floatingPalette
- 400: modalScrim
- 410: modalShadow
- 420: modalFrame
- 430: modalArt
- 440: modalText
- 450: modalControls
- 500: nestedScrim
- 520: nestedDialog
- 600: toast
- 700: tutorial

ResultPanel mevcut DimEnd120/PopEnd340/SettleEnd430/Medal430/Ribbon500/Checks600/Buttons760ms ve Skip/IntroDone akışı korunur; yeni daha geç etkinleşen düğme veya ödül gecikmesi eklenmez. Tasarımda hareket ölçeği göz yormayan mevcut değerlerle sınırlıdır. Basma görseli70ms, bırakma110ms; callback mevcut anda, animasyon sonuna taşınmaz. CanvasGroup bütünlüğü korunur. Modal açılması pause/daily/ads gibi mevcut oyun mantığının zaman durumunu değiştirmez.

## Modal dikey hesap

Kartyerel köken0/0. Metin/body PreferredHeight hesabı sonra bütün dependent bloklar aynı delta kadar aşağı kaydırılır. Grupbounds=kart + yalnız mevcut crest/mascot + mevcutZPEfooter. Top=max(48,(1920-groupHeight)/2), grubun merkezi540. Kartgövdesinin sonundaki56pxpadding her zaman kalır. Türkçe ilk yerleşimJSONunda ölçü var; isteğe göre hizalama/boşluk seçilmez. Title/body alanı boşsa onun gap'i de çıkarılır. FooterZPE varsa kartaltından24px,64pxyükseklik. Close yalnız gerçek kaynaktaki ekranlarda; kendi104×104hitrectine sahip, scrim altında kalmaz.

## Varlık listesi / şeffaflık

- Oyun zeminiZ1–Z5:1080×1920, opaquePNG; repeat garantisi yok, fiziksel ekranıcover.
- Modalpanel:1024×1536RGBA, yazısız; köşesınırlarıL96 B96 R96 T144;9-slice. Üstcrest ayrı512×192RGBA; yalnız üstortaya, paneli uzatırken crest gerilmez.
- Primary/secondary düğme:1024×192RGBA;9-slice L96 B48 R96 T48; ikon ve yazı ayrı; Normal/Pressed/Disabled görselstate kaynakları veya tanımlı boyalı gradient.
- Kapsül1024×160RGBA;9-slice L80 B40 R80 T40. Row1024×192RGBA; L64 B48 R64 T48.
- TahtakenarıB01–B10:10malzeme reçetesi, şekilPNGi YOK. Gerekirse256×64yazısız yanprofilstrip ve256×256repeatdetaydokusu; contourUV hesabı yukarıdaki standarda göre. Dışkontur/içdelik rendererı için aynı materyal, ayrı şekilasset üretme.
- Renkleri kaynakPNGde baked ise image.color beyaz; boyama yalnız düz/generatedUIyüzeylere, baked görselikinci kez tintlenmez.
- Logo, maskot, taşatlası, özelgüçikonları, mevcut3Bmodeller kopyalanıp yeniden çizilmez. VarolanResources yolları kullanılır.
- Filtrebilinear, mipmapUIoff, sRGBon, alphaisTransparencyon; mevcut AndroidASTCpolitikasına uy. MaxSizezemin2048, panel2048, buton1024. ReferansçözünürlüktestteRGBAuncompressed. Boarddetailseam/normalorientation görünür doğrulanır. Yeni paket/renderpipeline yok.

## Aktarım yasakları

PNG içine metin/sayı/dinamikhedef basma. Panonun3/7 gibi temsili zaferhedefini oyun verisine kopyalama. Kaynaktaki görünmeyen geri/kapat/ipucu/tekrar eylemini otomatik yaratma. Case/localizationformatı değiştirme. Modeli olmayan taşa yeni3Bmodel varmış gibi davranma. Haritada yeni Deneme shortcut ekleme.
