# 34 — Katman görünümü

Tür: layers. Kaynak ekran: docs/ui2/34_katman_gorunumu.png.

## Tam ekran alanları

Koordinatlar x,y,w,h; sol üst köken, 1080×1920.

| Öğe | Dikdörtgen | Stil rolü | Değişmeyen içerik/eylem | Not |
|---|---|---|---|---|
| pause | 48, 64, 112, 112 | icon | existing:PausePressed |  |
| undo | 920, 64, 112, 112 | icon | existing:OnUndoClicked |  |
| mascot | 424, 0, 232, 132 | image | existing:MascotView |  |
| moves | 286, 112, 508, 120 | moves | hud.moves + runtime MovesLeft |  |
| stars | 80, 256, 920, 84 | image | runtime score thresholds |  |
| goals | 48, 364, 984, 112 | goal | runtime GoalState[] |  |
| boardVisualEnvelope | 72, 508, 936, 1116 | runtime | existing BoardView | Yerleşim zarfı; hücre/şekil/mesh yeniden oluşturulmaz. Çerçeve ve gölge zarf içinde kalır. |
| powers | 96, 1664, 888, 160 | runtime | existing tray/stone/roll bindings | Yalnız kaynakta var olan güçler. Sabit yeni düğme veya alt tepsi ekleme. |
| peek | 16, 8, 120, 300 | runtime | existing ShowStackPeek | Ekrana clamp; hücre katman sırası kaynaktan; ikon64, aralık8, Üst etiketi26. Taşları yeniden sıralama. |


## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: trial.peek.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: HudView.Rules.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
