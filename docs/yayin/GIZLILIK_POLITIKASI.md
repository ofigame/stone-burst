# Gizlilik politikası taslağı (Stone Burst)

Tarih: 2026-11-09.

**Durum:** Taslak. Hukuki kontrolden geçmedi. App Store "Privacy Policy URL" alanı için bu metnin herkese açık bir web sayfasında yayınlanması gerekir; yayınlama kullanıcı onayıyla yapılır. `[…]` alanlarını kullanıcı doldurur.

Oyunun gerçekte topladığı veriler (koddan çıkarıldı):
- Game Center / Play Games kimliği ve takma adı (giriş, sıralama).
- Cihaz ülke kodu (sıralamada ülke sekmesi).
- Sıralama puanı, ZPE, ulaşılan bölüm, yıldız sayısı (Firestore `scores`).
- Oyun ilerlemesi (cihazda; isteğe bağlı bulut kaydı Firestore `saves` / Play Games Kayıtlı Oyunlar).
- Reklam: AdMob; rıza verilirse reklam kimliği (IDFA/GAID), cihaz ve kullanım verileri.
- Analitik ya da çökme raporu yok.

---

## Privacy Policy — Stone Burst

Last updated: [date]

Stone Burst ("the Game") is developed by OFIGAME ("we", "us"). This policy explains what information the Game uses and why.

**1. Information we collect**
- **Game account:** If you sign in with Apple Game Center (iOS) or Google Play Games (Android), we receive your player ID and nickname from that service. We use them to save your progress and to show your nickname on leaderboards.
- **Leaderboard data:** Your leaderboard score, level reached, stars and in-game performance index, together with your country code (taken from your device's region setting), are stored on our server (Google Firebase) so they can be shown on world and country leaderboards.
- **Cloud save:** If you choose "Save progress", your game progress is stored in your Game Center / Google account storage or on our Firebase server, so you can restore it on a new device.
- **Advertising:** The Game shows ads through Google AdMob. With your consent (and, on iOS, your App Tracking Transparency permission), AdMob may use your device's advertising identifier and device information to show relevant ads and measure them. Without consent, AdMob shows non-personalized ads. See Google's policy: https://policies.google.com/technologies/ads
- We do not collect your name, e-mail address, contacts, location or photos. We do not use analytics or crash-reporting services.

**2. How we use information**
Only to run the Game: saving progress, showing leaderboards and showing ads.

**3. Sharing**
We do not sell your data. Data is processed by our service providers: Apple (Game Center), Google (Play Games, Firebase, AdMob).

**4. Retention and deletion**
Leaderboard and cloud-save data are kept while you play. To delete them, contact us at [e-mail address] with your in-game nickname; we will delete them within 30 days. Data stored on your device is removed when you delete the Game.

**5. Children**
The Game is not directed at children under 13. We do not knowingly collect personal data from children under 13.

**6. Your choices**
You can withdraw ad consent at any time from Settings → Privacy options in the Game, and turn off tracking in your device settings.

**7. Contact**
OFIGAME — [e-mail address] — [address / country]

---

## App Store "App Privacy" anketi için yanıt taslağı

- **Data Used to Track You:** Identifiers → Device ID (AdMob, kullanıcı ATT izni verirse).
- **Data Linked to You:**
  - Identifiers → User ID (Game Center / Firebase kimliği)
  - Usage Data → Gameplay content: sıralama puanı, bölüm, yıldız
  - Location → Coarse Location: hayır; ülke kodu cihaz bölge ayarından gelir, konum değildir. Apple bunu konum saymaz.
- **Data Not Linked to You:**
  - Diagnostics: yok
  - Advertising Data (AdMob)
- AdMob'un kendi beyan listesi: https://developers.google.com/admob/ios/privacy/data-disclosure (kesin liste için kontrol edilmeli).
