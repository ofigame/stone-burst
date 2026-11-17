# TriKat — Onaylı tasarımın kesin şartnamesi

Tarih08.11.2026. Referans: docs/ui2_uyarlama_onizleme/00_dengeli_tasarim_panosu_v3.png. Sayısal değerler bu görsele uygun yeni uygulama kararıdır; rasterdan birebir ölçüm iddiası yok.

**Oyun koduna, sahneye, prefaba veya veriye müdahale edilmedi.** Kaynaklar yalnız okunup eşleştirildi. Bu klasör bir uygulama tarifi, yürütülmüş entegrasyon değildir. Yeni sprite/materyal üretimi henüz yapılmadı; üretim boyut ve importtarifleri hazırdır.

**Tahtanın şekli değişmez.** B01–B10 yalnız kenarlık. Bölümün dışkonturu,içdelikleri,hücreleri,taşları ve mekanikleri aynı kalır.

## Dosyalar

- [01_OLCU_TIPOGRAFI.md](01_OLCU_TIPOGRAFI.md): kenarboşluğu, fontboyut/ağırlık, satır ve metinkonumu, SafeArea ve taşma.
- [02_RENK_MALZEME.md](02_RENK_MALZEME.md): bütün boyalı yüzeylerin HEX/alpha/gradient/shadow reçeteleri.
- [03_ZEMIN_VE_BORDER.md](03_ZEMIN_VE_BORDER.md): 5rastgele zemin ve 10şekildenbağımsız border.
- [04_MODAL_DURUMLAR_ASSET.md](04_MODAL_DURUMLAR_ASSET.md): karartma, etkileşim, durumlar, katman,9-slice ve üretim ölçüleri.
- [05_CLAUDE_CODE_TALIMATI.md](05_CLAUDE_CODE_TALIMATI.md): yorum bırakmayan uygulama ve kabul sınırları.
- [tasarim-tokenlari.json](tasarim-tokenlari.json): ortak ölçü/renk/tipografi/durum makinaceokunabilir kaynağı.
- [tum-sayfalar.json](tum-sayfalar.json):35ekranın tamamı. sayfalar/ altında ayrıJSON ve açıklama.
- [icerik/METINLER.md](icerik/METINLER.md):302Türkçe kaynakkaydı; strings-kaynak.txt bütün9dil, source-kullanimlari.json kesin dosya/satırbağları.
- [kontrol/dogrulama.json](kontrol/dogrulama.json):1454oyunkaynağıönce/sonrahashkontrolü, değişen0dosya.

- [06_BILESEN_IC_YERLESIMLERI.md](06_BILESEN_IC_YERLESIMLERI.md): ikon/yazı/çubuk/thumbnail iç koordinatları; bilesenler/ortak-bilesenler.json ile eşleşir.

## Okuma sırası

Önce buREADME, ardından01–05; implementasyonda tokens ve ilgilisayfaJSON. Canlımetin/durumeylem kaynağının bu anlık referanstan önce geldiği unutulmaz. Onaylıestetik kararlar: olgun/sade oyunhissi, sakin doğalpalet, tamkarartılmış modalçevresi, özgüntaşseti.

## Sayfalar

| No | Ekran | Tarif |
|---|---|---|
| 01 | Harita / ana sayfa | [Açıklama](sayfalar/01_ACIKLAMA.md) |
| 02 | Günlük bulmaca | [Açıklama](sayfalar/02_ACIKLAMA.md) |
| 03 | Koleksiyon | [Açıklama](sayfalar/03_ACIKLAMA.md) |
| 04 | Taş vitrini | [Açıklama](sayfalar/04_ACIKLAMA.md) |
| 05 | Ayarlar | [Açıklama](sayfalar/05_ACIKLAMA.md) |
| 06 | ZPE istatistik | [Açıklama](sayfalar/06_ACIKLAMA.md) |
| 07 | Sıralama | [Açıklama](sayfalar/07_ACIKLAMA.md) |
| 08 | Sıralama bilgisi | [Açıklama](sayfalar/08_ACIKLAMA.md) |
| 09 | Deneme listesi | [Açıklama](sayfalar/09_ACIKLAMA.md) |
| 10 | Deneme kural kartı | [Açıklama](sayfalar/10_ACIKLAMA.md) |
| 11 | Deneme tahtası | [Açıklama](sayfalar/11_ACIKLAMA.md) |
| 12 | Oyun ekranı | [Açıklama](sayfalar/12_ACIKLAMA.md) |
| 13 | Özel taşlı oyun | [Açıklama](sayfalar/13_ACIKLAMA.md) |
| 14 | Duraklat | [Açıklama](sayfalar/14_ACIKLAMA.md) |
| 15 | Kayıp | [Açıklama](sayfalar/15_ACIKLAMA.md) |
| 16 | Zafer | [Açıklama](sayfalar/16_ACIKLAMA.md) |
| 17 | Günlük oyun | [Açıklama](sayfalar/17_ACIKLAMA.md) |
| 18 | Reklam aranıyor | [Açıklama](sayfalar/18_ACIKLAMA.md) |
| 19 | Ödüllü test reklamı | [Açıklama](sayfalar/19_ACIKLAMA.md) |
| 20 | Geçiş test reklamı | [Açıklama](sayfalar/20_ACIKLAMA.md) |
| 21 | Bölüm başlangıcı | [Açıklama](sayfalar/21_ACIKLAMA.md) |
| 22 | Dil seçimi | [Açıklama](sayfalar/22_ACIKLAMA.md) |
| 23 | Tür / kural tanıtımı | [Açıklama](sayfalar/23_ACIKLAMA.md) |
| 24 | Patron kartı | [Açıklama](sayfalar/24_ACIKLAMA.md) |
| 25 | Kayıp + ipucu | [Açıklama](sayfalar/25_ACIKLAMA.md) |
| 26 | İpucu gösterimi | [Açıklama](sayfalar/26_ACIKLAMA.md) |
| 27 | Kristal kaybı | [Açıklama](sayfalar/27_ACIKLAMA.md) |
| 28 | Günlük kural kartı | [Açıklama](sayfalar/28_ACIKLAMA.md) |
| 29 | Günlük zafer | [Açıklama](sayfalar/29_ACIKLAMA.md) |
| 30 | Günlük kayıp | [Açıklama](sayfalar/30_ACIKLAMA.md) |
| 31 | Günlük tamamlandı | [Açıklama](sayfalar/31_ACIKLAMA.md) |
| 32 | Deneme kayıp | [Açıklama](sayfalar/32_ACIKLAMA.md) |
| 33 | Boya paleti | [Açıklama](sayfalar/33_ACIKLAMA.md) |
| 34 | Katman görünümü | [Açıklama](sayfalar/34_ACIKLAMA.md) |
| 35 | Öğretici tur | [Açıklama](sayfalar/35_ACIKLAMA.md) |
