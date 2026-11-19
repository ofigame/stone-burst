# Site ikon seti (v5, mavi yıldız taşı)

Kaynak: `docs/varliklar/gorseller/arayuz/uygulama-ikonu-v5.png` (2026-11-19).

| Dosya | Boyut | Nerede kullanılır |
|---|---|---|
| `favicon.ico` | 16 + 32 + 48 | Tarayıcı sekmesi (eski tarayıcılar dahil) |
| `favicon-16.png`, `favicon-32.png`, `favicon-48.png` | | Sekme / yer imi |
| `apple-touch-icon.png` | 180×180 | iPhone/iPad ana ekrana ekle |
| `icon-192.png`, `icon-512.png` | | Android ana ekran, `site.webmanifest` |
| `icon-1024.png` | 1024×1024 | Paylaşım görseli, basın kiti |
| `favicon-512-kaynak.png` | 512×512 | Küçük ikonların ana çizimi (yalnız mavi taş) |

16–48 px boyutlarda ikonun tamamı seçilmediği için yalnız mavi yıldız taşı turkuaz zemin üzerinde kullanıldı. Büyük boyutlar ikonun tamamıdır.

## ofigame.com oyun kartı

Site oyun ikonlarının köşelerini kendisi yuvarlatıyor (CSS `border-radius: 22%`). Bu yüzden siteye kare `icon-512.png` yüklenmeli; yuvarlatılmış dosya yüklenirse köşeler iki kez kırpılır. 2026-11-19'da sitedeki Stone Burst ikonu hâlâ eski (katmanlı taş) ikondu. Nasıl görüneceği: `site-onizleme.png`.

## Sayfaya eklenecek kod

Dosyaları sitenin kök klasörüne koyun ve `<head>` içine ekleyin:

```html
<link rel="icon" href="/favicon.ico" sizes="any">
<link rel="icon" type="image/png" sizes="32x32" href="/favicon-32.png">
<link rel="icon" type="image/png" sizes="16x16" href="/favicon-16.png">
<link rel="apple-touch-icon" href="/apple-touch-icon.png">
<link rel="manifest" href="/site.webmanifest">
<meta name="theme-color" content="#22b8d6">
```

Dosyalar başka bir klasöre konursa yollar (ve `site.webmanifest` içindeki `src` değerleri) ona göre değiştirilmelidir.
