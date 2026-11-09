# 07 — Sıralama

Tür: rank. Kaynak ekran: docs/ui2/07_siralama.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| logo | 72, 48, 360, 108 | image | PremiumV4/ui/logo-trikat |  |
| stars | 700, 56, 208, 88 | metric | runtime star sum |  |
| settings | 936, 52, 96, 96 | icon | existing settings handler |  |
| nav | 0, 1712, 1080, 208 | nav | existing Harita/Günlük/Taşlar |  |
| tabs | 48, 184, 984, 104 | smallButton | runtime world/country |  |
| podium | 96, 336, 888, 344 | runtime | existing top three |  |
| list | 48, 712, 984, 780 | runtime | existing ten rows | Satır104; isim38, ayrıntı28, puan44; liste kendi içinde kayar. |
| self | 48, 1532, 984, 132 | rowTitle | runtime self entry |  |


## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: rank.|country.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: MenuView.Rank.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
