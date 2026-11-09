# 25 — Kayıp + ipucu

Tür: result. Kaynak ekran: docs/ui2/25_kayip_karti_ipucu.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| Arka sayfa | 0,0,1080,1920 | mevcut sayfa | mevcut durum | modal altındaki ekran aynen kalır |

## Modal

Genişlik 860, kart yüksekliği 920; kart yerel koordinatları aşağıda. Kart+taşan crest+ZPE rozeti grubu güvenli alanda ortalanır. Metin içeriği uzarsa ortak taşma formülü uygulanır.

| Öğe | Yerel x,y,w,h | Stil rolü | İçerik kaynağı |
|---|---|---|---|
| title | 56, 80, 748, 80 | modalTitle | runtime existing title |
| subtitle | 56, 184, 748, 104 | bodyCentered | runtime existing subtitle/goal chips |
| button0 | 56, 320, 748, 116 | button | runtime PanelButton[0] |
| button1 | 56, 460, 748, 116 | buttonSecondary | runtime PanelButton[1] |
| button2 | 56, 600, 748, 116 | buttonSecondary | runtime PanelButton[2] |
| button3 | 56, 740, 748, 116 | buttonSecondary | runtime PanelButton[3] |

Kapat kontrolü yalnız kaynakta varsa; mevcut kapatma eylemi korunur. Scrim bütün ekranı kapsar, RGBA #111C24 / alpha0.72.

## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.
- Ek hamle düğmesi yalnız mevcut ExtraMovesOffered koşulunda. Yoksa satır ve aralığı kaldır, aynı eylemi başka etikete dönüştürme.
- İpucu görünürlüğü mevcut kayıp sayısı/koşuluna bağlı; koşul değiştirilmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: lose.|ad.extra.|btn.|trial.hint. Düğme bağları: ad.extra.offer → btn.undo → btn.retry → trial.hint. Kaynak: ResultPanel.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
