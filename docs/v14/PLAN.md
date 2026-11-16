# v14: Zincirsiz, plan gerektiren 1000 bölüm (2026-11-16)

## Kullanıcının istekleri (2026-11-16)

- "oyunumuzun ana mantığından kopmadan görsel zenginliği … tahtalar büyür, taş sayıları artar, zincir patlamaları iptal edelim, zorluğu arttıralım full mantık gerektiren bir çözüm olsun, arada nefes alınacak bölümler koyalım ve o bölümler biraz işin eğlencesi olsun."
- "ilk 5 bölümde öğretici ile çeşitli seviyede ve oyunun özelliklerini tanıtmak amacıyla tüm özellikleri koyalım, geniş tahtalar koyalım, oyuncu neler olduğunu görsün."
- Tahta büyümesi: "6. bölümde tek katlı ve 3 renk ile başlasın, 8. bölümde 2 kat olsun, 15. bölümde 3 kat olsun; tahtalar büyümeye başlasın, katlar artmaya başlasın, zorluk artsın, görev sayıları artsın."
- "Oyuncu rasgele patlatmalar ile oyunu bitirdiğini değil plan ve mantık yürüterek bitirdiğini hissetsin."
- "oyuna ekleyelim … tüm bölümleri gözden geçirip istediğim her şeyi uygulayalım."
- Sorulan kararlara verilen yanıtlar:
  - Zincir: **tamamen kaldır**.
  - Geçiş: **doğrudan 1000 bölüm**.
  - İlerleme: **henüz yayında oyuncu yok**.
- "ortası delik tahtaları da kaldıralım", "dış hatlar çeşitli şekillerde olabilir".
- "ileri bölümler kolay olmasın lütfen, zorla".
- "bilgi metinleri bazen alanına sığmıyor, o alanları da yazıya göre ayarlayalım".
- 6 taş rengi: yeşil yaprak ve lacivert yıldız eklendi (`LayerColor.Green`, `Navy`).

## Kurallar

1. **Zincir yok.** Soyulan taşların altından çıkan aynı renk grubu kendiliğinden patlamaz. Her patlama oyuncunun dokunuşudur ya da oyuncunun kullandığı özel taşın etkisidir.
   - Bu kurala bağlı şunlar kaldırılır:
     - "yalnız zincir sayılır" kuralı (`chainOnly`/`collectChain`);
     - Zincir Mührü (`seal`);
     - Tek Dokunuş (`aha`);
     - günlük "zincir" türü;
     - zincir dersi (öğretici 2. bölüm).
   - Özel taşlar (5–7 roket, 8+ Altın Rulo) ve tepsi aynen kalır.
2. **Tahta:** ortası delik yok. Etkin olmayan hücre, dört yanı tahtayla çevrili bir boşluk oluşturamaz. Dış hat çeşitli olabilir: köşe kesik, T, L, artı, basamak.
3. **Renk sayısı 3'ten 6'ya çıkar.** Kat sayısı 1'den 4'e çıkar (doğrulayıcı sınırı 4).
   - Tahta 6×7'den 8×9'a büyür.
   - Görev sayısı 1'den 3'e çıkar; HUD izin verirse 4.

## Eğri (bölüm numarası 1'den başlar)

| Bölüm | İçerik |
|---|---|
| 1–5 | **Vitrin**: geniş tahta (7×8), 2–3 kat, 4 renk, bol hamle (kolay). Her bölüm bir grup özelliği gösterir: 1 dokun-soy ve katlar · 2 roket ve tepsi · 3 Altın Rulo, dip yıldızı, ataç · 4 buz, kilit, zarf, sünger · 5 kristal koruma, dar hamle. |
| 6–7 | 1 kat, 3 renk, 6×7 |
| 8–14 | 2 kat; 3 renk, 11'den sonra 4 renk; 6×7 → 7×7 |
| 15–30 | 3 kat, 4 renk, 2 görev |
| 31+ | Her 10 bölümde bir boyut artar: tahta, kat (en çok 4), renk (5 renk ~60, 6 renk ~200) ya da görev (3 görev ~40). |

**Mekanik takvimi** (vitrinden sonra gerçek tanıtım):
- ataç 21, dip yıldızı 41, buz 61, kilit 121, zarf 201, rotor 301, sünger 401, bağlı ataç 501, geçit 601, boya 701;
- 801'den sonra karışık.

**Bölüm türleri** (kural kartlı):
- exact (dar hamle), protect (kristal), scarce (kıt renk), reverse, pure (özel taş yok), resource, order;
- zincir türleri yok.

**Nefes bölümleri** (10'dan sonra her 6. bölüm; patron bölümü olan x0 bölümlerine denk gelmez):
- 1–2 kat, büyük tahta, büyük gruplar;
- bol özel taş, geniş hamle;
- kısa ve eğlenceli.

**Usta bölümü:** her x0 bölümü, tek doğru yola yakın.

## Zorluk kabul ölçütleri (üretici her bölümde ölçer)

- **Çözülebilir:** çözücü (beam) kazanır, kayıtlı çözüm motorla doğrulanır.
- **Hamle payı** (sınır − en iyi çözüm):
  - 6–30: 3–5;
  - 31–100: 2–3;
  - 101–300: 1–2;
  - 301+: 0–1;
  - nefes bölümlerinde +4.
- **Rastgele bot kazanma oranı** (geçerli hamlelerden rastgele):
  - 1–30: ≤ %60;
  - 31–100: ≤ %25;
  - 101–300: ≤ %10;
  - 301+: ≤ %3;
  - nefes bölümlerinde sınır yok.
- **Açgözlü bot** (en büyük grubu hemen patlatır): 31'den sonra nefes dışı bölümlerde kaybeder.
- **Yıldız eşikleri:** kayıtlı çözüm + kutlama puanından, bütün bölümlerde aynı formül (eski `StarThresholds`).

## Ekran ve metin

- Bilgi şeridi ve öğretici bant yazının ölçülen yüksekliğine göre büyür (`HudView.FitBand`); en çok 4 satır.
- Öğretici turlar yeni bölümlere göre: 1 dokunuş; 2 roket ve tepsi; 3 Altın Rulo. Mekanik tanıtımları ilk geçtiği bölümde otomatik çıkar.

## Diğer kataloglar

- **Günlük bulmaca:** zincir türü döngüden çıkar; 240 bulmaca yeni ana bölümlerden yeniden üretilir.
- **Deneme seti** (Resources/Trial): zincir ve Tek Dokunuş bölümleri başka türe çevrilir.
- **Pilot** (Resources/Pilot): mühür bölümleri çıkar ya da dönüştürülür.
- **Konsept** (yedekte): Kombo Ateşi zincir yerine 5+ grupla beslenir.
- **Canlı maç:** zincirsiz zaten, değişmez.

### Faz B2 uygulaması (2026-11-16)

- **Günlük:** `LevelLab daily14` (tools/LevelLab/DailyV14.cs) → `daily_src_v14`, kurulum `daily14install` → `daily_src`, `Resources/Daily`, katalog `daily-v4`.
  - Tarif: grubun alt sınırına yakın bir v14 bölüm numarası (grup 1: 15–30), v14 üreticisi (`V14Make`) ve ölçütleri; 6 renge kadar.
  - Grubun alt sınırından sonra tanıtılan mekanik çıkarılır.
  - Tür haftalık döner: exact → protect → reverse → scarce → pure → resource → order (grupta açılmamış tür atlanır).
  - Rulo açık: taş gücü başta uygulanabilir; kayıtlı çözüm güç kullanmadan kazanır.
- **Deneme seti:** 1 exact, 2 compact (Derin Kuyu), 3 reverse, 4 scarce, 5 wide (Geniş Kıyı), 6 protect, 7 pure, 8 resource, 9 order, 10 boss (protect + noSpecials + exact).
  - Kural kartı sırası korunur: 1 dar hamle, 4 kıt renk, 7 özel taş yok, 10 kristal.
- **Pilot:** mühür pilotları 08–10 düğmeli kanada (`pilot_wing`) dönüştü; bütün pilotlar zincirsiz motorla yeniden çözüldü.
- **Konsept:** kayıtlı çözümler zincirsiz motorla yeniden üretildi; Gölge Rakip 4 renkli tahtaya geçti (5+ grubu oyuncu kurar), rakip bitişi 2–10, pay 4.
- Denetim: `LevelLab b2check [trial|pilot|concept|daily]`.
- Eski veri: `KatKat_Arsiv_2026-11-06/Assets/KatKat/Resources/{Daily,Trial,Pilot,Concept}_v13/` ve `KatKat_Arsiv_2026-11-06/daily_src_v13/`.

## Kontrol

- EditMode, PlayMode ve bütün bölüm tekrarı.
- Bölüm sağlama toplamı (`levels_v14.sha256`) yeniden üretilir: kullanıcı 1000 bölümün yenilenmesini istedi.
- Ekranlar: ilk 30 bölüm, örnek ileri bölümler, nefes bölümü.
- Zorluk tablosu: `docs/v14/ZORLUK.md`.
