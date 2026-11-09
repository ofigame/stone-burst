# 01 — Harita / ana sayfa

Tür: menu. Kaynak ekran: docs/ui2/01_harita_ana_sayfa.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| logo | 72, 48, 360, 108 | image | PremiumV4/ui/logo-trikat |  |
| stars | 700, 56, 208, 88 | metric | runtime star sum |  |
| settings | 936, 52, 96, 96 | icon | existing settings handler |  |
| region | 48, 176, 984, 120 | sectionTitle | existing region bindings |  |
| map | 0, 312, 1080, 1392 | runtime | existing map geometry | Harita içeriği kaynak geometri; zemin çizgisi haritanın üzerine çizilmez. |
| nav | 0, 1712, 1080, 208 | nav | existing Harita/Günlük/Taşlar |  |
| zpe | 48, 332, 132, 180 | goal | existing ZPE 0–100 |  |
| rank | 48, 548, 132, 164 | nav | existing rank handler |  |
| lockedStone | 884, 332, 148, 160 | image | existing blurred stone |  |
| stoneProgress | 844, 512, 188, 64 | caption | existing threshold progress |  |


## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: ui.map|map.|zpe.|rank.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: MenuView.Shell.cs;MenuView.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
