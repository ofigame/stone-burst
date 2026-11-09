# 05 — Ayarlar

Tür: settings. Kaynak ekran: docs/ui2/05_ayarlar.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| logo | 72, 48, 360, 108 | image | PremiumV4/ui/logo-trikat |  |
| stars | 700, 56, 208, 88 | metric | runtime star sum |  |
| settings | 936, 52, 96, 96 | icon | existing settings handler |  |
| nav | 0, 1712, 1080, 208 | nav | existing Harita/Günlük/Taşlar |  |
| title | 180, 188, 720, 92 | pageTitle | ui.settings |  |
| panel | 110, 444, 860, 640 | paint | ivory |  |
| row0 | 166, 500, 748, 132 | rowTitle | existing sound/vibration/language |  |
| row1 | 166, 664, 748, 132 | rowTitle | existing sound/vibration/language |  |
| row2 | 166, 828, 748, 132 | rowTitle | existing sound/vibration/language |  |


## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: ui.settings|settings.|ui.sound|ui.vibration. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: MenuView.Ui1.cs;MenuView.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
