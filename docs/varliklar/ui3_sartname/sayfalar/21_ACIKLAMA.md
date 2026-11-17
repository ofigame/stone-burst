# 21 — Bölüm başlangıcı

Tür: start. Kaynak ekran: docs/ui2/21_bolum_baslangic_karti.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| Arka sayfa | 0,0,1080,1920 | mevcut sayfa | mevcut durum | modal altındaki ekran aynen kalır |

## Modal

Genişlik 860, kart yüksekliği 600; kart yerel koordinatları aşağıda. Kart+taşan crest+ZPE rozeti grubu güvenli alanda ortalanır. Metin içeriği uzarsa ortak taşma formülü uygulanır.

| Öğe | Yerel x,y,w,h | Stil rolü | İçerik kaynağı |
|---|---|---|---|
| title | 56, 56, 748, 80 | modalTitle | runtime level number |
| stars | 176, 164, 508, 104 | image | runtime existing star state |
| summary | 56, 300, 748, 88 | bodyCentered | runtime mechanics names |
| play | 56, 428, 748, 116 | button | ui.playbig |

Kapat kontrolü yalnız kaynakta varsa; mevcut kapatma eylemi korunur. Scrim bütün ekranı kapsar, RGBA #111C24 / alpha0.72.

## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.
- Yıldızlar o bölümün gerçek durumunu gösterir; panodaki altın yıldızlar otomatik kazanılmış sayılmaz.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: ui.playbig|level.. Düğme bağları: ui.playbig. Kaynak: MenuView.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
