# VR2 oyun ekranı — uygulama raporu (2026-11-14)

İstek: "Stone Burst projesinde VR2 oyun ekranı tasarımını uygula" (belge `docs/vr2/CLAUDE_CODE.md`). Çalışma sırasında kullanıcı üç düzeltme istedi:
- "tahtanın borderlarından içe doğru boşluğu olmalı"
- "üst alanın alt kısmında dış gölge olmalı ki üst bir katman hissi versin"
- "tahtanın iç gölgesi fazla koyu … kusursuz bir geçişle olsun"

## Yapılanlar

| Parça | Uygulama |
|---|---|
| Arka plan | Altı 2160×2880 PNG `Assets/KatKat/Resources/VR2/bg/`. İçe aktarma `Editor/Vr2AssetImporter.cs`: Max Size 4096, sRGB, Clamp, mip yok, Android ve iOS için ASTC 6×6. Ekranı oran korunarak kaplar: ölçek max(ekranG/2160, ekranY/2880), mevcut `Backdrop.Fit`. Bellekte yalnız etkin tema durur (`Vr2.Background` öncekini boşaltır). |
| Tema seçimi | `Vr2.SkinFor`, bölge (100 bölüm) sırasıyla. Adlar eşleşir: 0 Mavi Liman, 1 Yeşil Vadi, 2 Kum Vadisi, 3 Fener Koyu, 5 Vaha Yolu, 6 İnci Sahili. Dokusu olmayan bölgeler kendi türünün dokusunu kullanır: 4 ve 7 → Yeşil Vadi, 8 → Kum Vadisi, 9 → Mavi Liman. |
| Üst panel | Krem dikey gradyan (ust-panel.svg), ekranın tepesinden tam genişlikte. Güvenli alanın üstü de aynı renkle dolu. Köşeler kare, çizgi yok. Altında belirgin dış gölge (28 birim, %55, aşağı sönümlü); kullanıcı isteği belgedeki 4–8 px'in yerine geçti. Yerleşim 941 referansın ×1,148 katı: duraklat (129, 122), geri al sağdan 127, hamle kapsülü merkezi 176, yıldız çubuğu merkezi 291, panel altı 350. Kontroller ortada 1080 birimlik grupta; tablette panel tam genişlikte, kontroller ortalı kalır. |
| Hedef yuvası | Panelin dışında, panelden ~40 birim aşağıda bağımsız şerit (hedef-yuvasi.svg). Ölçü 993×125, merkez y 453. Kenar rengi temanın paletinden gelir (base → shadow → highlight). İçte koyu duvar #704C32, gömülü krem zemin; 9 dilim 72/20. Çipler aynı canlı hedeflerden; VR2'de ayrı hap zemini yok. |
| Tahta iç alanı | Ayrıntılar aşağıda. |

Tahta iç alanı (`View/Vr2Floor.cs`, `BoardView.BakeFloorVr2`):
- Bölümün mevcut maskesi aynen kullanılır (`Active` ya da kapı hücresi); yeni şekil üretilmez.
- Köşeler 0,14 hücre yuvarlanır (belge 0,22; kullanıcı istekleriyle 0,10 → 0,14).
- İç alan hücre kenarından 0,24 hücre taşar (0,18 → 0,28 → 0,56 → 0,42 → 0,32 → 0,24; kullanıcı: "border ile iç boşluğu arttıralım", "şuankinin 2 katı olsun", "%25 azaltalım", "%25 daha azaltalım", "boşluğu da azaltalım"); pay kare genişletmeyle eklenir; dış ve iç köşe yarıçapı 0,14 hücre (belge 0,22; kullanıcı: "yuvarlama çok az olsun" → 0,10, "radiusu çok az arttıralım" → 0,14); taşlar kenara değmez. Tahta yerleşimi bu payı da sığdırır (taşlar biraz küçülür). Hiçbir 2×2 boş hücre bloğuna girmeyen pasif hücrede (tek hücrelik delik, tek hücre genişliğinde oyuk) pay 0,28 ile sınırlı: en az 0,44 hücre açıklık kalır ve arka plan görünür.
- Dolgu tek renk: arka plan renginin açık tonu. Paletteki floorColor %35 beyaza açılır (`Vr2.InnerFloor`); kullanıcı 2026-11-14: "iç tahtanın tonu arkaplan renginin açık tonu olsun".
- Gölge yalnız iç alana kırpılır ve iki katmandır (kullanıcı 2026-11-15: "çok uyumsuz … daha kısa ve daha az belirgin olmalı ama derinliği de yansıtmalı"): kenar dibinde ince temas gölgesi (bant 0,10 hücre, aşağı 0,025, σ 0,025, %42) ve onun altında kısa yumuşak geçiş (bant 0,16, aşağı 0,05, σ 0,07, %20). Renk temanın paletteki koyu tonu (`Vr2.ShadowTint`). Önceki denemeler: tek katman #101C27 %76 (belge), %40/σ 0,17, %55/σ 0,12, %55/σ 0,07.
- Dış çerçeve, parlak kenar ve dış gölge yok.
- Pasif hücre ve boşluklar saydam; VR2'de boşluk süsü ve buzlu koleksiyon taşı (vr1) konmaz (kullanıcı 2026-11-15: "ortası boş olan tahtalara obje koymayalım"). Taşı biten hücrede taş biçiminde yuva görünür: iç alanın temanın koyu tonuna doğru %18 koyusu (`Vr2.EmptySlot`; kullanıcı: "taşlar tamamen kalkınca alt zemininde yerini belli eden hücresi olsun").

Belgedeki gölge değerleri (%76, 0,30 / 0,085 / 0,10) kullanıcının "fazla koyu" isteğiyle değiştirildi; yön ve renk aynı kaldı.

Korunanlar:
- taş görselleri, metinler, düğme işlevleri ve dokunma alanları,
- bölüm verisi, oyun kuralları ve kayıt.

Hamle ve hedef sayıları canlı veriden gelir. `Vr2.Enabled = false` eski ui3 görünümüne döner.

## Değişen ve eklenen dosyalar

- Yeni:
  - `Scripts/UI/Vr2.cs`
  - `Scripts/View/Vr2Floor.cs`
  - `Editor/Vr2AssetImporter.cs`
  - `Editor/CaptureVr2.cs`
  - `Tests/PlayMode/Vr2Tests.cs`
  - `Resources/VR2/bg/*.png` (6 dosya)
- Değişen:
  - `Scripts/UI/HudView.cs` (panel, yerleşim, hedef yuvası, `SetVr2Skin`)
  - `Scripts/UI/GoalChip.cs` (VR2'de hap zemini saydam)
  - `Scripts/View/BoardView.cs` (VR2 iç alanı, süs yok, yuva saydam)
  - `Scripts/Game/GameController.cs` (tema → doku, tahta, HUD; `Vr2SkinOverride`)
  - `tools/unity.ps1` (`capture-vr2`)
  - `docs/DURUM.md`, `docs/KARARLAR.md`, `docs/OYUN.md`

## Kontroller (gerçekten çalıştırılanlar)

- `tools/unity.ps1 test-edit`: 265/267 geçti, 2 eski test atlandı. Kenar payı ve gölge değişikliklerinden önce çalıştırıldı; bu değişiklikler EditMode koduna dokunmuyor.
- `tools/unity.ps1 test-play` (tümü): 82/84 geçti, 2 atlandı. Kenar payı ve gölge değişikliklerinden önce çalıştırıldı.
- `test-play -Filter Vr2`: 3/3 geçti, son hâliyle. Kapsadığı kontroller:
  - tema eşlemesi, palet renkleri,
  - iç alanın tek renk olması, pasif hücre ve maske dışının saydamlığı,
  - kenar payı, dış çerçeve ve dış gölge yokluğu, yuvarlak köşe,
  - üst iç kenarın alt iç kenardan koyu olması.
- `tools/unity.ps1 capture-vr2`, son hâliyle:
  - 6 tema × (19.5:9 1080×2340, 20:9 1080×2400, dikey 4:3 1536×2048),
  - çentikli güvenli alan, 16:9 ve 5 hedefli bölüm.
  - Görseller bu klasörde. Tek tek dosyalar `docs/captures/vr2/` altında (git dışı).
- `OLCUM.md`: altı temada iç alan rengi beklenen floorColor ile birebir aynı; maske dışı alfa 0; üst iç kenar alt iç kenardan koyu.

## Kalanlar / denenmeyenler

- Gerçek cihazda denenmedi: ASTC 6×6 sıkıştırmanın görünümü ve bellek (belge cihazda kontrol istiyor).
- APK boyutu ölçülmedi. Altı doku ASTC 6×6 ile her biri ~2,8 MB.
- Uzun yerelleştirmeler ayrıca çekilmedi. Hamle yazısı ve sayaçlar önceki gibi otomatik küçülür.
- Kapalı ya da açılan geçitler çekimde ayrıca denenmedi. İnci Sahili bölüm 611'de üst sıradaki kapı hücreleri iç alanda görünüyor.
- Beş hedefli bölümde son çipin (zarf + aşama simgeleri) içeriği yuvanın sağ ucuna yakın duruyor. Çip içeriği eskisiyle aynı.
