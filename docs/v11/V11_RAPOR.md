# TriKat v11 — Uygulama raporu (2026-11-08)

Kaynak: `docs/v11/CLAUDE-v11-PROMPT.md` ve kullanıcının bu turdaki ek istekleri. Görüntüler:
`docs/captures/v11/` (araç: `tools/unity.ps1 capture-v11`). Önce/sonra aynı geçici kayıtla ve aynı kodla alındı
(önce = v10 kabuğu ve eski simgeler zorlanarak).

## 1. Mavi-beyaz ana ekran

| Parça | Kaynak | Unity'de |
|---|---|---|
| Mavi panel zemini | `arayuz/01` → `PremiumV6/shell/v11-panel.png` | Üst panel ve alt menü; oran korunarak "cover" (yanlar kırpılır). Alt menü cihazın gerçek alt kenarına kadar iner; üstü çentiğe taşar. Panellerin haritaya düşen kısa gölgesi koddan. |
| Beyaz bölge kartı | `arayuz/02` → `v11-region.png` | Fener (mevcut `ui/region-coast`), canlı bölge adı, ince ayırıcı, sağda kapsül. |
| Turkuaz kapsül | `arayuz/03` → `v11-region-chip.png` | "BÖLGE 1 • 1–100" canlı (Loc `ui.region`); harita kayınca güncellenir. |
| Krem yıldız kapsülü | `arayuz/04` → `v11-star-capsule.png` | Mevcut yıldız simgesi + gerçek toplam. |
| Seçili menü kartı | `arayuz/05` → `v11-tab-card.png` | Seçili sekmenin simge+yazı grubunun arkasında. |
| Taşlar menü simgesi | `arayuz/menu_taslar_icon.png` → `v11-icon-stones.png` | Kullanıcı isteği. |

- PNG'ler görünür sınırlarına (+4 px) kırpıldı; özel simgeler merkezde kaldı. İçe aktarma: Sprite, sRGB, alfa,
  uzun kenarı 2048 üstü olanlar en çok 4096 (`ShellAssetImporter` sürüm 6), Android ASTC.
- Alt menü: N öğe iç genişliğe eşit dağılır (x0 + (i+½)·W/N); ayırıcı yok (seçilen tasarımda yok); N=0 boş, yeniden
  çizimde birikme yok. 3/4/5 öğe yerleşimi yakalandı (`sonra_menu_3oge/4oge/5oge`); üretimde 3 öğe.
- Referansla fark (açıkça): referansta 4 sekme (Ödüller) var; kullanıcının 2026-11-07 kararıyla Ödüller yok.
  Ayarlar düğmesi mevcut altın halkalı düğme (referansta turkuaz halka). Logo, fener ve harita mevcut varlıklar.
  Bölge adı yazı ağırlığı referanstan biraz ince. Birebir uyum iddia edilmez.
- Alt menü bütün menü sayfalarında ortak bileşen olduğundan Koleksiyon sayfasında da mavi; Koleksiyon içeriği ve
  oyun içi HUD değişmedi.

## 2. ZPE (kullanıcı, 2026-11-08)

- Puan 100 üzerinden, oynanan bütün normal bölümlerin düz ortalaması. Kayıtta `zpeSum` + `zpeGames` (eski kayıtta
  geçmişten kurulur); ilk oyundan itibaren görünür. Bileşen dökümü ve eğilim son kayıtlardan.
- Ana sayfada sağ sütunda kilitli taş kartının üstünde: ZPE'ye özel gösterge (ibre) simgesi, altında "72/100".
  Dokununca istatistik paneli ("72/100"). Bölüm sonu rozeti aynı ölçekte.

## 3. Kilitli taş

- Bulanıklık yoğunlaştırıldı (22 taş, `Collection/stones_blur`): yalnız renk ve kaba siluet kalıyor.
- Ana sayfa kartında da bulanık sürüm; kilit buzlu çerçevenin tam ortasında.

## 4. Özel güç simgeleri

- `skiller/01–03` → `PremiumV6/skills/special-color|row|column.png`. Tek kaynak `SpecialArt.For` (tahta, tepsi,
  sonraki bölüm önizlemesi, doğma uçuşu, ateşleme efekti; bir kez yüklenip önbellekte).
- Yerleşim: yüz merkezinde (eski köşe/bant ofseti kaldırıldı); yatay %85×%30, dikey %30×%85, renk %70. Dikey simge
  döndürülmez (tepsideki 90° dönüş kaldırıldı).
- Ateşleme: yatay simge ortadan ikiye bölünür, yarılar sağa ve sola; dikey yarılar yukarı ve aşağı
  (`sonra_bolunme_*`). Roket+roket birleşiminde dört yarı. Kural motoru, hedefleme ve zamanlama değişmedi.

## 5. Güç alanı (kullanıcı, 2026-11-08)

- Yuva, halka, alttaki krem/altın kabuk ve bulutlar kaldırıldı (kullanıcı, 2026-11-08: önce bulut denendi, beğenilmedi): güç taşları doğrudan oyun zemininde, daha büyük.
- Her biri kendi fazında yerinde büyüyüp küçülür ve içindeki renge göre ışıldar (roket turkuaz, rulo altın, boya
  turkuaz, renk temizleyici kırmızı/turkuaz/mor arasında dolaşır, koleksiyon taşı kendi ışıltı renginde).
  Hareket oyun akarken çalışır; editör görüntüleri durağan.
- Günlük bulmaca: seçili koleksiyon taşı bu alanda kendi PNG'siyle; dokununca günlük gücü kullanılır (doğrulama ve
  harcama aynı işlemde), kullanılınca soluk ve rozet 0. Normal bölümde görünmez.

## 6. Sesler

v11 paketindeki dört WAV v10'da bağlanan dosyalarla aynı (değişmedi): harita döngüsü tek kaynak, ilk soyulma dalgası
pop, hamle başına bir zincir, zafer kartında bir ödül; ses ayarı müziği ve efektleri birlikte susturur. Testler:
`AudioShellTests`. Telefonda dinlenmedi.

## 7. Doğrulama

- Testler: bkz. DURUM.md (bu turun sayıları).
- Yeni testler: ZPE düz ortalama / ilk oyun / eski kayıt (EditMode), özel simgeler tek kaynak ve yarılar,
  özel taşın normal yoldan oluşup tepsiye gelmesi → hedefleme → çalışma → bölünen yarılar → geri alma (PlayMode),
  günlük taş yuvası (PlayMode). Birleşim kuralları motor testlerinde (`SpecialRulesTests`).
- Telefonda denenmedi: nefes/ışıltı hareketi, bölünme akıcılığı, dokunma alanları cihazda kontrol edilmeli.
