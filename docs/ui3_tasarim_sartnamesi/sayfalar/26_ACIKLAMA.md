# 26 — İpucu gösterimi

Tür: hint. Kaynak ekran: docs/ui2/26_ipucu_gosterimi.png.

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
| hint | 80, 1460, 920, 96 | bodyCentered | trial.hint.toast | İşaret gerçek önerilen hücrede 4 px teal halka; yeni öneri algoritması yok. |


## Sabit davranışlar

- Metin, sayı, durum, düğme sayısı, sırası ve eylemi mevcut çalışma zamanından gelir; örnek panodaki içerik kullanılmaz.
- Otomatik yeni kapat/geri düğmesi, yardım, ödeme, güç veya ayar eklenmez.

## Metin ve eylem eşlemesi

Yerelleştirme grubu: trial.hint.. Düğme bağları: Kaynak yöntemlerin mevcut görünür kontrolleri. Kaynak: HudView.cs;HudView.Rules.cs. Alanın görünen metni JSON anahtarı değil, mevcut Loc.T/Loc.F çıktısıdır. source-kullanimlari.json dosyası kesin satırları, icerik/tr.json kesin Türkçe dizgileri verir.
