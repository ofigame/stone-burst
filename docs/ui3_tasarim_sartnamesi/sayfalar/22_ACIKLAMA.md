# 22 — Dil seçimi

Tür: language. Kaynak ekran: docs/ui2/22_dil_secimi.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| Arka sayfa | 0,0,1080,1920 | mevcut sayfa | mevcut durum | modal altındaki ekran aynen kalır |

## Modal

Genişlik 860, kart yüksekliği 1120; kart yerel koordinatları aşağıda. Kart+taşan crest+ZPE rozeti grubu güvenli alanda ortalanır. Metin içeriği uzarsa ortak taşma formülü uygulanır.

| Öğe | Yerel x,y,w,h | Stil rolü | İçerik kaynağı |
|---|---|---|---|
| title | 56, 56, 748, 80 | modalTitle | runtime language title |
| language0 | 56, 192, 236, 116 | smallButton | Loc.Languages[0].NativeName |
| language1 | 312, 192, 236, 116 | smallButton | Loc.Languages[1].NativeName |
| language2 | 568, 192, 236, 116 | smallButton | Loc.Languages[2].NativeName |
| language3 | 56, 356, 236, 116 | smallButton | Loc.Languages[3].NativeName |
| language4 | 312, 356, 236, 116 | smallButton | Loc.Languages[4].NativeName |
| language5 | 568, 356, 236, 116 | smallButton | Loc.Languages[5].NativeName |
| language6 | 56, 520, 236, 116 | smallButton | Loc.Languages[6].NativeName |
| language7 | 312, 520, 236, 116 | smallButton | Loc.Languages[7].NativeName |
| language8 | 568, 520, 236, 116 | smallButton | Loc.Languages[8].NativeName |
| closeButton | 56, 824, 748, 116 | buttonSecondary | runtime close |

Kapat kontrolü yalnız kaynakta varsa; mevcut kapatma eylemi korunur. Scrim bütün ekranı kapsar, RGBA #111C24 / alpha0.72.

## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.
- Dokuz dilin native adları Loc.Languages içinden; Fransızca/İspanyolca aksanlar ve Arapça/CJK harfler aynen korunur.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: ui.language|ui.close. Düğme bağları: runtime native languages → ui.close. Kaynak: MenuView.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
