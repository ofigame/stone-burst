# docs düzeni

2026-11-18'de sadeleştirildi. Eski sürüm klasörleri (ui1, ui2, v9–v13, vr1, vr2, captures, konsept, mağaza ara çekimleri) silinmedi;
`E:\OFIGAME\OYUNLAR\KatKat_Arsiv_2026-11-06\docs_temizlik_2026-11-18` altına taşındı.

## Güncel belgeler

| Dosya | İçerik |
|---|---|
| `DURUM.md` | Ne yapıldı, ne bekliyor (her işten sonra güncellenir) |
| `KARARLAR.md` | Verilen kararlar ve gerekçeleri |
| `OYUN.md` | Oyunun bugünkü kuralları, ekranları, bölüm eğrisi |
| `SEVIYE_DOGRULAMA.md` | Bölüm doğrulama raporu (LevelLab üretir) |
| `YAPILACAKLAR.md` | Sonra yapılacaklar listesi |
| `v14/` | Bölüm üretim planı ve zorluk raporu (LevelLab v14 yazar) |
| `yayin/` | Mağaza, iOS, Play, AppGallery, Facebook planları; gizlilik politikası; mağaza metinleri |
| `bekleyen/` | Ertelenen işler: canlı maç tasarımı ve ölçümleri |

## varliklar/ — oyunda kullanılan görsel ve sesler

| Klasör | İçerik |
|---|---|
| `LISTE.md` | Oyunun yüklediği bütün görsel, ses, yazı tipi ve 3B modellerin listesi |
| `oyunda/` | O dosyaların yerel kopyası (depoya girmez) |
| `gorseller/` | Elle düzenlenen kaynak görseller: arayüz ve ikon, harita, maskot, mekanikler, oyun içi, taşlar, tepsi |
| `taslar_3d/` | Taşların 3B ve PNG kaynakları (depoya girmez) |
| `ui3_tasarim/`, `ui3_sartname/` | Onaylı ui3 arayüz tasarımı ve ölçü şartnamesi (`tools/ui3_hazirla.ps1` buradan okur) |
| `magaza/` | Mağaza ekran görüntülerinin son hali: iphone, ipad, android, play (depoya girmez) |
| `facebook/` | Facebook Instant ikon, kapak ve tanıtım görselleri |

Oyunun asıl dosyaları `Assets/KatKat/Resources` ve `Assets/KatKat/Art` altında kalır. Oyuna görsel ya da ses eklenince,
çıkarılınca veya değişince şunu çalıştır; `oyunda/` kopyasını ve `LISTE.md` dosyasını yeniler:

```
powershell -NoProfile -File tools/varliklar_guncelle.ps1
```

## Araç çıktıları

Yakalama ve ara çıktılar artık `docs` içine yazılmaz, `Logs/` altına gider (depoya girmez):
`Logs/captures` (ekran yakalamaları), `Logs/ui3_uygulama` (ui3 karşılaştırma), `Logs/magaza` (ham/fark/sahne/review çekimleri),
`Logs/raporlar` (eski LevelLab komutlarının raporları).
