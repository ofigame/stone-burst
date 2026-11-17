# 16 — Zafer

Tür: victory. Kaynak ekran: docs/ui2/16_zafer_karti.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| Arka sayfa | 0,0,1080,1920 | mevcut sayfa | mevcut durum | modal altındaki ekran aynen kalır |

## Modal

Genişlik 860, kart yüksekliği 1232; kart yerel koordinatları aşağıda. Kart+taşan crest+ZPE rozeti grubu güvenli alanda ortalanır. Metin içeriği uzarsa ortak taşma formülü uygulanır.

| Öğe | Yerel x,y,w,h | Stil rolü | İçerik kaynağı |
|---|---|---|---|
| stars | 230, -124, 400, 172 | image | runtime actual Stars |
| ribbon | 16, 16, 828, 152 | ribbonTitle | runtime existing victory title |
| goals | 96, 204, 668, 104 | goal | runtime GoalState[] |
| preview | 96, 344, 668, 300 | runtime | NextLevelPreviewView.Data |
| zpe | 240, 680, 380, 72 | smallButton | runtime actual ZPE |
| button0 | 56, 804, 748, 116 | button | runtime PanelButton[0] |
| button1 | 56, 944, 748, 116 | buttonSecondary | runtime PanelButton[1] |

Kapat kontrolü yalnız kaynakta varsa; mevcut kapatma eylemi korunur. Scrim bütün ekranı kapsar, RGBA #111C24 / alpha0.72.

## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: win.|btn.|zpe.. Düğme bağları: btn.continue → btn.replay. Kaynak: ResultPanel.cs;NextLevelPreviewView.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
