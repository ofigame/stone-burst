# 06 — ZPE istatistik

Tür: zpe. Kaynak ekran: docs/ui2/06_zpe_istatistik.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| Arka sayfa | 0,0,1080,1920 | mevcut sayfa | mevcut durum | modal altındaki ekran aynen kalır |

## Modal

Genişlik 860, kart yüksekliği 1120; kart yerel koordinatları aşağıda. Kart+taşan crest+ZPE rozeti grubu güvenli alanda ortalanır. Metin içeriği uzarsa ortak taşma formülü uygulanır.

| Öğe | Yerel x,y,w,h | Stil rolü | İçerik kaynağı |
|---|---|---|---|
| title | 56, 56, 748, 68 | modalTitle | zpe title |
| value | 180, 148, 500, 112 | zpeValue | runtime ZPE |
| metric0 | 56, 308, 748, 88 | rowSub | runtime ZPE component[0] |
| metric1 | 56, 420, 748, 88 | rowSub | runtime ZPE component[1] |
| metric2 | 56, 532, 748, 88 | rowSub | runtime ZPE component[2] |
| metric3 | 56, 644, 748, 88 | rowSub | runtime ZPE component[3] |
| metric4 | 56, 756, 748, 88 | rowSub | runtime ZPE component[4] |
| notes | 56, 900, 748, 144 | legal | runtime ZPE note incl IQ disclaimer |

Kapat kontrolü yalnız kaynakta varsa; mevcut kapatma eylemi korunur. Scrim bütün ekranı kapsar, RGBA #111C24 / alpha0.72.

## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: zpe.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: MenuView.Ui1.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
