# Kullanıcı kaydı ve sıralama planı (v13)

Tarih: 2026-11-08 · Güncelleme 2026-11-09 · Durum: **kararlar verildi, bağlantı kodu ve eklentiler hazır; oyunda hâlâ test hesabı çalışıyor** (Play Console / Firebase yapılandırma dosyaları bekleniyor)

Kullanıcının isteği:
- Sıralamanın oluşabilmesi için kullanıcı kaydı.
- Ayarlara "İlerlemeyi kaydet" düğmesi.
- Sıralama ekranına "Sıralama için kayıt yapın" düğmesi. Kayıt hızlı olmalı.
- Oyun açıldığında Game Center kaydı.

## 1. Önerilen yol: Google Play Games Services v2

Oyun şu an yalnız Android'de çıkıyor. Android'deki "Game Center" karşılığı **Google Play Games** (PGS).

| İhtiyaç | PGS ile karşılığı |
|---|---|
| Açılışta kayıt | PGS v2 oyun açılınca oyuncuyu **kendisi, sessizce** giriş yaptırır. Cihazda Play Games hesabı olan oyuncuda form ya da pencere yoktur. |
| Hızlı kayıt | Sessiz giriş olmadıysa "Kayıt ol" düğmesi tek dokunuşla Play Games giriş penceresini açar. Ad ve avatar Play'den gelir; e-posta veya şifre istenmez. |
| İlerlemeyi kaydet | PGS **Kayıtlı Oyunlar** (Saved Games): kayıt dosyası (`katkat_save.json`) oyuncunun Google hesabına yazılır. Telefon değişince geri yüklenir. |
| Sıralama | İki seçenek var (§3). |

iOS ileride çıkarsa aynı arayüz Apple **Game Center**'a bağlanır. Oyun kodu değişmez; yalnız yeni bir `IAccountService` eklenir.

## 2. Kodda hazır olanlar (bu sürüm)

- `IAccountService` (`Scripts/Game/Account.cs`): `SignIn(sessiz)`, `SaveProgress`, `LoadProgress`, `PlayerName`, `PlayerId`.
- `TestAccountService`: gerçek hizmet gelene kadar çalışır. Tek dokunuşla "Oyuncu 1234 (test)" oluşturur. "Bulut" kaydını cihaza (PlayerPrefs) yazar, bildirimlerde **(test)** etiketi görünür.
- Açılışta sessiz giriş denemesi (`GameController.SetupAccount`).
- Ayarlar kartında 4. satır: **İlerlemeyi kaydet → Kaydet**. Giriş yoksa önce tek dokunuşla giriş yapılır, sonra kayıt yazılır.
- Sıralama ekranında, oyuncu girişli değilse kendi kartının üstünde **Sıralama için kayıt ol** düğmesi. Kayıttan sonra ekran yenilenir ve düğme kaybolur.
- Gerçek hizmet tek satırla bağlanır: `GameController.AccountProvider = new PlayGamesAccountService();`. Sıralama için `GameController.LeaderboardProvider` atanır.

## 3. Sıralama sunucusu: karar gerekiyor

Bizim sıralamanın özel kuralları var:
- puan 0–1000;
- dünya / ülke sekmeleri;
- 20 bölüm eşiği;
- reklamlı kazanımlar sayılmaz.

| Seçenek | Artı | Eksi |
|---|---|---|
| **A. PGS Leaderboards** | Ücretsiz, sunucu yok, Play'in hazır sıralama penceresi | Ülke sekmesi yok, ekran Google'ın penceresi (bizim tasarımımız değil), puan tek sayı |
| **B. Firebase (Play Games ile giriş) + Firestore** | Bizim ekranımız, ülke sekmesi ve kurallar korunur; PLAN v12 §8 "Firebase ücretsizse Firebase" | Firebase projesi, güvenlik kuralları ve hile koruması gerekir; ücretsiz kotası küçük oyun için yeterli |

**Karar (kullanıcı, 2026-11-09): B — Firebase.** Kimlik PGS'den gelir (Firebase Auth'a Play Games kimliğiyle giriş), puanlar Firestore'a yazılır.

## 4. Kullanıcının yapması gerekenler (hesaplar kullanıcıda)

1. Play Console → oyun → **Play Games Services** → yapılandırma oluştur (paket `com.ofigame.katkat`).
2. OAuth istemcisi (Android, yükleme ve Play imza anahtarı SHA-1'leri).
3. Kayıtlı Oyunlar'ı aç.
4. (Seçenek B için) Firebase projesi; Authentication'da Play Games sağlayıcısını aç; Firestore oluştur.
5. Test kullanıcılarını ekle (yayından önce yalnız onlar giriş yapabilir).

## 5. Benim yapacaklarım (onaydan sonra)

1. **İndirme izni gerekir:** `play-games-plugin-for-unity` (Google, GitHub sürümü, yaklaşık 10 MB). Seçenek B için ayrıca Firebase Unity SDK (Auth + Firestore).
2. `PlayGamesAccountService : IAccountService`: v2 otomatik giriş, `ManuallyAuthenticate`, Saved Games ile yaz/oku.
3. Açılışta buluttaki kayıt yereldekinden ilerideyse "Buluttaki ilerlemeyi yükle?" sorusu. Kural: en çok tamamlanan bölüm kazanır, kayıt sessizce ezilmez.
4. `FirebaseLeaderboard : ILeaderboardProvider` (seçenek B) ya da PGS leaderboard gönderimi (seçenek A).
5. Testler: sahte hizmetle giriş, kayıt ve çakışma akışları; cihazda gerçek hesapla deneme.

## 6. Kararlar (kullanıcı, 2026-11-09)

- Sıralama: **Firebase** (seçenek B).
- Oyuncu adı: **Play Games adı** (takma ad sorulmaz, kayıt tek dokunuş kalır).

## 7. Hazırlanan (2026-11-09)

- **İndirilenler** (kullanıcı izniyle) `ThirdParty/indirilen/`, henüz projeye eklenmedi:
  - `GooglePlayGamesPlugin-2.3.0.unitypackage` (3,4 MB, GitHub playgameservices)
  - Firebase Unity SDK 14.1.0 UPM paketleri: app (61 MB), auth (3,5 MB), firestore (16 MB) (dl.google.com)
  - External Dependency Manager 1.2.190 (0,4 MB)
- **Kod** `Scripts/Online/`: yalnız `KATKAT_ONLINE` tanımlıyken derlenir, şu anki derlemeyi etkilemez.
  - `PlayGamesAccountService`: PGS v2 sessiz/elle giriş, ad Play Games'ten, sunucu erişim koduyla Firebase Auth, Kayıtlı Oyunlar'a yaz/oku.
  - `FirestoreLeaderboard`: `scores/{uid}` kaydını yazar, dünya/ülke ilk 10 ve kendi sırasını (sayım sorgusu) okur.
  - `OnlineSetup`: açılışta iki sağlayıcıyı bağlar.
  - SDK projede olmadığı için derlenmedi; cihazda denenmedi.
- **Sunucu** `firebase/`: `firestore.rules` (yalnız kendi kaydını yazar, alan ve değer sınırları), `firestore.indexes.json` (country + score dizini), `firebase.json`.

## 8. Açmak için sıradaki adımlar

1. Kullanıcı: §4'teki Play Console ve Firebase kurulumu. Bana `google-services.json`, Play Games kaynak XML'i (Play Console → Play Games Services → Yapılandırma → Kaynakları al) ve Web istemci kimliği gelecek.
2. Ben: paketleri projeye ekle, Android bağımlılıklarını çöz, `KATKAT_ONLINE` tanımını aç, derle.
3. Firestore kuralları ve dizini: `firebase deploy --only firestore` (Firebase CLI, kullanıcı hesabıyla).
4. Bulut kaydı çakışma sorusu ("Buluttaki ilerlemeyi yükle?") ve cihazda gerçek hesapla deneme.
