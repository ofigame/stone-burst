# 04 — Taş vitrini

Tür: showcase. Kaynak ekran: docs/ui2/04_tas_vitrini_3d.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| back | 48, 48, 112, 112 | icon | existing back |  |
| name | 148, 188, 784, 112 | pageTitle | runtime stone name |  |
| newNote | 96, 328, 888, 92 | bodyCentered | runtime new-stone note |  |
| model | 160, 452, 760, 800 | runtime | existing 3D model or PNG |  |
| desc | 96, 1300, 888, 176 | bodyCentered | runtime power description |  |
| swipe | 96, 1500, 888, 72 | caption | runtime rotate hint |  |
| continue | 166, 1620, 748, 116 | button | runtime continue |  |


## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: stone.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: StoneShowcase.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
