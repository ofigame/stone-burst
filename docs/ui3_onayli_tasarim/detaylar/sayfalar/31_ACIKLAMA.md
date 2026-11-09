# 31 — Günlük tamamlandı

Tür: dailyDone. Kaynak ekran: docs/ui2/31_gunluk_bulmaca_tamamlandi.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| Arka sayfa | 0,0,1080,1920 | mevcut sayfa | mevcut durum | modal altındaki ekran aynen kalır |

## Modal

Genişlik 860, kart yüksekliği 1120; kart yerel koordinatları aşağıda. Kart+taşan crest+ZPE rozeti grubu güvenli alanda ortalanır. Metin içeriği uzarsa ortak taşma formülü uygulanır.

| Öğe | Yerel x,y,w,h | Stil rolü | İçerik kaynağı |
|---|---|---|---|
| date | 236, 48, 388, 80 | smallButton | runtime daily date |
| title | 56, 152, 748, 80 | modalTitle | daily.title |
| group | 56, 256, 748, 80 | caption | runtime group + difficulty |
| chest | 80, 390, 240, 260 | image | existing chest |
| body | 352, 368, 452, 464 | body | runtime daily body |
| divider | 56, 780, 748, 2 | paint | divider |
| note | 56, 980, 748, 72 | legal | runtime prototype note |

Kapat kontrolü yalnız kaynakta varsa; mevcut kapatma eylemi korunur. Scrim bütün ekranı kapsar, RGBA #111C24 / alpha0.72.

## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: daily.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: MenuView.Ui1.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
