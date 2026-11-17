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

## Üretilmiş varlıklar ve şeffaflık

Gerçek sprite ölçüleri, borderLTRB ve import bilgileri ../parcalar/asset-manifest.json dosyasındadır. Kullanım ../parcalar/KULLANIM.md içindedir. 6 ortak PNG,45 adet9-slice dilimi,10 kontur malzeme profili ve5 opaque arkaplan üretilmiştir. Normal durumda baked renklere ikinci tint uygulanmaz. Metin, logo, maskot, taşlar ve güç ikonları ayrı mevcut kaynaklardır. Oyun zemini Cover/Clamp, UI bilinear/mipmap off/sRGB on/alphaIsTransparency on.

## Aktarım yasakları

PNG içine metin/sayı/dinamikhedef basma. Panonun3/7 gibi temsili zaferhedefini oyun verisine kopyalama. Kaynaktaki görünmeyen geri/kapat/ipucu/tekrar eylemini otomatik yaratma. Case/localizationformatı değiştirme. Modeli olmayan taşa yeni3Bmodel varmış gibi davranma. Haritada yeni Deneme shortcut ekleme.
