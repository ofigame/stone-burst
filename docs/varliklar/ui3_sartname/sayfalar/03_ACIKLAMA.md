# 03 — Koleksiyon

Tür: collection. Kaynak ekran: docs/ui2/03_koleksiyon.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| logo | 72, 48, 360, 108 | image | PremiumV4/ui/logo-trikat |  |
| stars | 700, 56, 208, 88 | metric | runtime star sum |  |
| settings | 936, 52, 96, 96 | icon | existing settings handler |  |
| nav | 0, 1712, 1080, 208 | nav | existing Harita/Günlük/Taşlar |  |
| title | 180, 188, 720, 92 | pageTitle | collection title |  |
| category | 48, 316, 984, 88 | sectionTitle | runtime section + count |  |
| scroll | 48, 428, 984, 1252 | runtime | existing all 22 items | 2 sütun; kart468×484; yatay48, düşey32. Kart içi padding24; art[64,24,340,252], name[24,292,420,56], desc[24,360,420,100]. |


## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: collection.|stone.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: MenuView.Ui1.cs;MenuView.CollectionV10.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
