# TriKat · Proje denetimi (v10 ana planına göre)

Tarih: 2026-11-06. Kaynak: `docs/v10/TRIKAT_CLAUDE_MASTER_PLAN.md`.
Bu belge salt okunur bir denetimdir; kod, sahne, prefab ve bölüm verisi değiştirilmedi.

Her bulgu gerçek dosya ya da sınıf adıyla verilmiştir. "Doğrulanamadı" ifadesi, telefonda ya da canlı serviste denenemeyen durumlar için kullanılır.

## 1. Motor, paketler, platformlar

| Konu | Bulgu | Kanıt |
|---|---|---|
| Unity | 6000.6.4f1 | `tools/unity.ps1` (`$Unity` yolu), `docs/KARARLAR.md` |
| Render hattı | Built-in RP; URP/HDRP paketi yok | `Packages/manifest.json` |
| Arayüz | uGUI 2.6.0 + TextMesh Pro | `Packages/manifest.json`, `Assets/TextMesh Pro` |
| Giriş | Eski Input Manager (`activeInputHandler: 0`); Input System paketi yok | `ProjectSettings/ProjectSettings.asset:843` |
| Ürün adı | TriKat, şirket OFIGAME, sürüm 0.1.0, Android sürüm kodu 1 | `ProjectSettings.asset:15-16, 152, 181` |
| Paket kimliği | `com.ofigame.katkat` (Android ve Standalone) | `ProjectSettings.asset:172-174` |
| Android | IL2CPP, ARMv7+ARM64 (`AndroidTargetArchitectures: 3`), en düşük SDK 26, ASTC doku | `ProjectSettings.asset:182, 274, 748-749`, `Editor/BuildScripts.cs` |
| iOS | Derleme yapılandırması ve derleme yok (Windows ortamı) | `Editor/BuildScripts.cs` yalnız Android + Windows |
| Huawei | Ayrı derleme ya da HMS yapılandırması yok | — |
| Üçüncü taraf SDK | **Yok.** Firebase, reklam, satın alma, Play Games, Game Center, Huawei, Facebook bulunmuyor. Yalnız Unity'nin yerleşik `unityanalytics` modülü listede; kodda kullanılmıyor. | `Packages/manifest.json`, `Assets/` (Plugins klasörü yok) |
| İmza | Özel keystore tanımlı değil (hata ayıklama imzası) | `ProjectSettings.asset` (keystore alanı yok) |
| Derleme durumu | Derleniyor. Son APK 2026-11-06 17:41, 84 MB. | `Builds/Android/TriKat.apk`, `Logs/batch/build-android.log` |

## 2. Kod yapısı

Derleme birimleri (asmdef):

| Birim | İçerik |
|---|---|
| `KatKat.Core` (`Scripts/Core`) | Unity'siz, deterministik kural motoru: `RulesEngine`, `BoardState`, `LevelState`, `GameSession`, `GroupFinder`, `Scoring`, `Command`, `MoveResult`, `PeelWave` |
| `KatKat.Levels` (`Scripts/Levels`, motor referansı yok) | `LevelParser`, `LevelLoader`, `LevelValidator`, `LevelSolver`, `MiniJson`. v9 meta katmanı `Levels/Meta`: `ProgressRules`, `SessionOutcome`, `MetaSave`, `Stones`, `DailyService` |
| `KatKat.Runtime` (`Scripts/Game`, `UI`, `View`, `Save`) | `GameController` (+ `.Meta`, `.Tutorial`), `LevelCatalog`, `SoundBank`, `Haptics`, `ImmersiveMode`, `MenuView` (+ `.Meta`), `HudView`, `ResultPanel`, `TutorialOverlay`, `BoardView`, `BoardDecor`, `PeelDirector`, `ThemeDefinition`, `SaveService`, `SaveData` |
| `KatKat.Editor` | Derleme (`BuildScripts`), ekran yakalama (`GameCapture`, `CaptureV7`, `CaptureV9`, `TrailerCapture`), dil denetimi (`LocCheck`), bölüm doğrulama menüsü |
| Testler | `Tests/EditMode` (13 dosya), `Tests/PlayMode` (6 dosya) |

Unity dışında çalışan araçlar:
- `tools/CoreTests`: dotnet; Core + Levels + EditMode testleri, 133 test.
- `tools/LevelLab`: bölüm üretme, doğrulama ve stres testi.
- `tools/unity.ps1`: Unity toplu komutları.
- `tools/trailer.ps1`: tanıtım videosu.

## 3. Oyun akışı ve mekanikler

- **Sahneler:**
  - `Scenes/Boot.unity`: OFIGAME açılışı (`Bootstrap.cs`).
  - `Scenes/Game.unity`: oyun.
  - `Scenes/PeelLab.unity`: soyulma animasyonu deneme sahnesi.
- **Akış:**
  - Ana ekran doğrudan haritadır (`GameController.UseMapAsHome = true`, `MenuView.ShowLevels`).
  - Bölüm başlatma `GameController.StartLevel`.
  - Kazanma kartı `ResultPanel` (`HudView.ShowVictory`); kayıp `GameController.ShowLost`.
  - Ayarlar `MenuView.ShowSettings`. Günlük ve Taşlar `MenuView.Meta.cs`.
- **Temel kural:** Aynı renkte en az 3 kenar komşusu üst katmana dokunulur ve her yığından bir katman soyulur. Yeni açılan grup kendiliğinden soyulur (zincir) ve hamle harcamaz (`RulesEngine`, `docs/OYUN.md` §2).
- **Mekanikler (kodda):**
  - ataş ve yıldız (`ClipHit`)
  - zarf
  - buz ve kilit (`LockHit`)
  - sünger (`SpongeHit`)
  - döndürücü (rotor)
  - boya
  - roket ve Altın Rulo (`SpecialKind`)
  - tepsi (en çok 4 özel taş)
  - satır rulosu (`RollGroupSize = 5`)
  - geri alma

  Testleri: `RotorRulesTests`, `IceLockRulesTests`, `EnvelopeRulesTests`, `SpecialRulesTests`, `ClipStarRulesTests`, `NewMechanicsTests`.
- **Yıldız:** Bölüm sonunda 1–3 yıldız (`Scoring`, `GameController`). Kalan hamleye göre hesaplanır; kural `docs/OYUN.md`'de.
- **Öğretici:** 1, 2, 3, 6, 7, 9 ve 12. bölümlerde tur, her yeni mekaniğin ilk bölümünde tanıtım (`GameController.Tutorial.cs`, `TutorialOverlay`).
- **Dil:** 9 dil, 160 anahtar (`Resources/Localization/strings.txt`, `UI/Loc.cs`). Arapça sağdan sola yazılıyor (`ArabicShaper`). Denetim komutu `tools/unity.ps1 loc-check`.
- **Ses:** Koddan sentezlenen efektler (`SoundBank`). Müzik yok.

## 4. Bölümler (1.000)

- **Kaynak ve veri:**
  - Kaynak: `levels_src/level_001…1000.txt`.
  - Oyunun okuduğu JSON: `Resources/Levels/level_001…1000.json`, `LevelLab build` ile üretilir; elle düzenlenmez (`docs/KARARLAR.md`).
- **Kimlik:** Dosya adı (`level_001`). JSON'daki `id` alanıyla aynı; `LevelCatalog.ParseAll` ve `GameFlowTests` bunu denetliyor.
- **Yükleme:** v9 E paketinden beri bölüm ilk istendiğinde okunuyor (`Game/LevelCatalog.cs`).
- **Doğrulama:**
  - `docs/SEVIYE_DOGRULAMA.md`: 1000/1000 kayıtlı çözüm gerçek kural motoruyla kazanıyor.
  - `LevelLab stress 60`: 1000/1000, 60.000 oyun, istisna yok.
  - `PlayMode GameFlowTests`: 1.000 bölümün hepsi oyun akışında kayıtlı çözümle oynanıyor (~51 dk).
  - Bu sonuçlar çözüm olduğunu kanıtlar; en kısa çözüm ya da eğlence kanıtı değildir.
- **Değişmezlik kontrolü:** v10 P0.2'den beri `ProductionLevelChecksumTests` her test koşusunda 1.000 dosyanın SHA-256 özetini `Tests/EditMode/Data/levels_v7.sha256` ile karşılaştırıyor (Unity'de ve Unity'siz).
- **Dünya düzeni:**
  - Her 100 bölüm bir bölge (`ThemeDefinition.RegionSize = 100`).
  - Üç tema (sahil, bahçe, çöl) sırayla dönüyor; bölge adları `ThemeDefinition.RegionTitle` ile.
  - 500 bölümlük dünya gruplaması **yok**. Planın "Akdeniz Sahili / Çiçek Bahçesi" temaları yok.

## 5. Kayıt

- **Dosya:** `persistentDataPath/katkat_save.json`. JsonUtility kullanılıyor (`Save/SaveService.cs`, `Save/SaveData.cs`).
- **Sürüm:** `saveVersion = 2`. v1 → v2 yükseltmesi bir kez yapılıyor (`SaveService.Upgrade`, `upgradedFrom`). Yükseltmede yinelenen ve geçersiz yıldızlar ayıklanıyor (`ProgressRules.Normalize`).
- **Alanlar:**
  - `highestUnlocked`, `completed`, `sound`, `vibration`
  - `starIds` / `starCounts`: bölüm başına en iyi yıldız
  - `meta` (`MetaSave`): taşlar, seçili taş, denenen bölümler, günlük durum, ödül günlüğü, +1 hamle günü
- **Testler:** `SaveServiceTests` (v1 yükseltme, yıldız artışları), `MetaTests`.
- **Yerel yedek:**
  - Her yazışta önceki kayıt `.bak` dosyasına dönüyor. Ana kayıt bozuksa `.bak` yükleniyor, o da bozuksa güvenli başlangıç (`SaveService.Save`, `Load`; testli).
  - v10 P1.3 ile ek olarak yükseltme öncesi kalıcı kopya (`katkat_save.v1.json`) ve bozuk dosyanın kopyası (`.corrupt`) saklanıyor.
- **Bulut kaydı:** Yok.
- **Canlı oyuncu:** Oyun yayımlanmadı. Mağazada canlı oyuncu ilerlemesi olup olmadığı **doğrulanamadı** (kullanıcıya sorulmalı).

## 6. v9'da eklenen meta sistemler (yerel prototip)

- **Yıldız toplamı:** Bölüm başına en yüksek geçerli 1–3 değer (`ProgressRules.TotalStars`). 3 → 1 → 3 tekrarı toplamı yalnız 3 artırıyor (`MetaTests`, `SaveServiceTests`).
- **Oturum sonucu:**
  - `SessionOutcome` + `AssistFlags` (geri alma, günlük +1, taş gücü, öğretici, ödüllü reklam).
  - Taban ve ek hamle, kullanılan hamle, ilk deneme, deneme sırası, etkin süre.
  - `GameController.OutcomeRecorded` olayıyla yayınlanıyor ama **kalıcı olarak saklanmıyor**.
- **Koleksiyon:**
  - `Resources/Collection/stones.json`: tek taş, `stone_tide`, `levelCompleted: 10`, güç `dailyRoll`.
  - Kuralları `CollectionRules`; ekranı `MenuView.ShowCollection` (basit liste).
  - Simge geçici.
- **Günlük bulmaca:**
  - Ayrı katalog: `Resources/Daily/daily_index.json` + `daily_001.json`. **Tek bulmaca var.**
  - `DailyService`: giriş, ilk deneme ve bir ücretsiz tekrar, bilerek çıkışta denemenin harcanması, kapanma sonrasında aynı denemeye dönüş, günde bir ödül, +1 hamle hakkı.
  - Gün `LocalDayClock` ile cihazın yerel gece yarısından belirleniyor.
  - Reklam ve reklamsız hak SAHTE adaptör: `FakeRewardedAds`, `FakeAdFree`.
  - Kabul akışı testi: `DailyFlowTests`.
- **Menü:** Alt menü Harita · Günlük · Taşlar; Ayarlar sağ üstte (`MenuView.Tabs`, `MenuView.TopGear`).

## 7. Reklam, satın alma, kimlik, analitik, bildirim, sunucu

**Hiçbiri yok.**
- **Reklam ve satın alma:** Yalnız arayüzler ve sahte adaptörler var (`IRewardedAds`, `IAdFreeEntitlement`, `Levels/Meta/DailyService.cs`).
- **Kimlik:** Kimlik, sıralama ve profil kodu yok.
- **Analitik:** Analitik arayüzü yok.
- **Bildirim:** Bildirim ya da yerel bildirim yok.
- **Ağ:** Ağ çağrısı yapan kod yok; oyun tamamen çevrimdışı çalışıyor.

## 8. Performans ve test olanakları

- **Ölçüm** (`Tests/PlayMode/ScalePerfTests.cs`, editörde, i5-11400H):
  - Katalog açılışı ~13 ms, +0,1 MB.
  - Harita 10.000 sanal bölümle 144 ms; kaydırma adımı ~0,2 ms.
  - Ayrıntı: `docs/v9/V9_E_RAPOR.md`.
  - Telefonda ölçülmedi.
- **Bilinen risk:** Açılışta `Resources.LoadAll("Levels")` bütün bölüm metinlerini bir anlığına belleğe alıyor: 1.000 bölümde 8 MB, 10.000'de ~82 MB.
- **Görsel karşılaştırma:** `tools/unity.ps1 capture-v9-before/after`, editör çizimiyle (`docs/captures/v9/KARSILASTIRMA.md`).
- **Sürekli entegrasyon (CI):** Yok. Testler yerelde toplu komutlarla koşuluyor (`tools/unity.ps1 test-edit/test-play`). Git deposu da yok.
- **Son test sonucu** (2026-11-06):
  - CoreTests 133/133
  - EditMode 133/133
  - PlayMode 21/21 (ölçüm testi yalnız istenince çalışıyor)
  - `loc-check` TAMAM
- **Cihaz testi:** Yapılmadı. Kullanıcı zaman zaman Redmi'de deniyor; sonuçlar raporlanmadı.

## 9. Yayın öncesi açık teknik işler

- `GameController.TestUnlockAll = true`: bütün bölümler açık; yayından önce `false` yapılmalı.
- İmza anahtarı yok.
- Paket kimliği planla farklı.
- iOS ve Huawei derleme hattı yok.
- Gizlilik metni, izin yönetimi ve hesap silme yok.
