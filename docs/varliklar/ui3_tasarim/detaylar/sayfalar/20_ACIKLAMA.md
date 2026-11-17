# 20 — Geçiş test reklamı

Tür: testAd. Kaynak ekran: docs/ui2/20_test_reklam_gecis.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| Arka sayfa | 0,0,1080,1920 | mevcut sayfa | mevcut durum | modal altındaki ekran aynen kalır |

## Modal

Genişlik 860, kart yüksekliği 1480; kart yerel koordinatları aşağıda. Kart+taşan crest+ZPE rozeti grubu güvenli alanda ortalanır. Metin içeriği uzarsa ortak taşma formülü uygulanır.

| Öğe | Yerel x,y,w,h | Stil rolü | İçerik kaynağı |
|---|---|---|---|
| test | 150, 64, 560, 88 | smallButton | runtime TEST REKLAMI |
| type | 56, 200, 748, 80 | modalTitle | runtime ad type |
| placement | 56, 300, 748, 56 | caption | runtime existing placement id |
| indicator | 300, 568, 260, 260 | image | runtime existing timer/placeholder |
| note | 56, 1100, 748, 112 | legal | runtime SDK note |

Kapat kontrolü yalnız kaynakta varsa; mevcut kapatma eylemi korunur. Scrim bütün ekranı kapsar, RGBA #111C24 / alpha0.72.

## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: ad.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: TestAdScreen.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
