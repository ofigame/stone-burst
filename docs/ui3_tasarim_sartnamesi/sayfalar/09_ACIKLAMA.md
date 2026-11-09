# 09 — Deneme listesi

Tür: trials. Kaynak ekran: docs/ui2/09_deneme_listesi.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| logo | 72, 48, 360, 108 | image | PremiumV4/ui/logo-trikat |  |
| stars | 700, 56, 208, 88 | metric | runtime star sum |  |
| settings | 936, 52, 96, 96 | icon | existing settings handler |  |
| nav | 0, 1712, 1080, 208 | nav | existing Harita/Günlük/Taşlar |  |
| title | 120, 184, 840, 96 | pageTitle | trial title |  |
| intro | 96, 304, 888, 92 | bodyCentered | existing description |  |
| list | 48, 428, 984, 1252 | runtime | existing ten trials | Satır156, boşluk20; ikon88; metin x136 width684; ok x880 size64. Başlık38 altmetin28. |


## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: trial.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: MenuView.Ui1.cs;MenuView.Trial.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
