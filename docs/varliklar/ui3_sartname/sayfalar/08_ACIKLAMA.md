# 08 — Sıralama bilgisi

Tür: rankInfo. Kaynak ekran: docs/ui2/08_siralama_bilgi.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| Arka sayfa | 0,0,1080,1920 | mevcut sayfa | mevcut durum | modal altındaki ekran aynen kalır |

## Modal

Genişlik 860, kart yüksekliği 1120; kart yerel koordinatları aşağıda. Kart+taşan crest+ZPE rozeti grubu güvenli alanda ortalanır. Metin içeriği uzarsa ortak taşma formülü uygulanır.

| Öğe | Yerel x,y,w,h | Stil rolü | İçerik kaynağı |
|---|---|---|---|
| title | 56, 48, 748, 80 | modalTitle | runtime rank info title |
| trophy | 320, 144, 220, 200 | image | existing trophy |
| intro | 56, 364, 748, 112 | bodyCentered | runtime intro |
| component0 | 56, 500, 748, 92 | rowSub | runtime component |
| component1 | 56, 616, 748, 92 | rowSub | runtime component |
| component2 | 56, 732, 748, 92 | rowSub | runtime component |
| note | 56, 856, 748, 88 | legal | runtime min-level/ads rule |
| ok | 56, 960, 748, 116 | button | runtime existing OK |

Kapat kontrolü yalnız kaynakta varsa; mevcut kapatma eylemi korunur. Scrim bütün ekranı kapsar, RGBA #111C24 / alpha0.72.

## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: rank.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: MenuView.Ui1.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
