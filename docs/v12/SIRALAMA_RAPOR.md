# TriKat v12 — Sıralama ekranı ve reklam altyapısı raporu (2026-11-08)

Kaynak: `docs/v12/siralama/CLAUDE-SIRALAMA.md`, `00-onayli-referans.png`, `VARLIKLAR.json`; plan `docs/v12/PLAN.md`.
Görüntüler: `docs/captures/v12/` (araç `tools/unity.ps1 capture-v12`); karşılaştırma `siralama/karsilastirma_ref_sonra.png`.

## Tamamlanan davranışlar

- Erişim: harita (ana sayfa) sol sütununda kupa düğmesi + "Sıralama". Alt menü (3 sekme) değişmedi.
- Başlık: geri · TriKat logosu · kupa + "Sıralama" kapsülü · bilgi (puan formülü açıklaması).
- Sekmeler: Dünya ve oyuncunun ülkesi (Android `Locale`, Windows `RegionInfo`; Türkiye örnek). Ülke bilinmiyorsa sekme "Ülke", içerik "Ülke bilgisi bulunamadı. Dünya sıralaması açık." — ülke uydurulmaz. Sekme değişince liste ve kendi kartı birlikte güncellenir.
- Podyum 2 · 1 · 3 (birinci ortada, yüksek); podyum ve liste aynı ilk 10 verisinden.
- İlk 10: sıra (1–3 madalya, 4–10 turkuaz rozet), avatar, ad (tek satır, taşarsa üç nokta), ZPE · Bölüm · yıldız (yıldız simgesi yalnız burada), sağda kupa + sıralama puanı. Kısa ekranda liste kayar.
- "Senin sıran" kartı altta sabit (kaydırma içeriğinde değil): sıra, avatar (maskot), "Sen", kupa + puan, ZPE / Bölüm / yıldız kapsülleri. Sıra yoksa "—" (0 yazılmaz) ve "Henüz sıralaman yok"; 20 bölümden az ise "Sıralamaya girmek için 12/20 bölüm".
- Durumlar: yükleniyor, boş liste, bağlantı hatası + Yeniden dene, sunucu bağlı değil + Yeniden dene, ülke yok, sıralaması yok.
- Animasyon: ilk girişte 180 ms görünme, sekme değişiminde 160 ms içerik geçişi, düğme/sekme basışı 0,96 (80 / 120 ms), maskot 2,8 sn ±3 birim salınım (`RankFx.ReducedMotion` ile kapanır; oyunda henüz "azaltılmış hareket" ayarı yok). Satırlar zıplatılmaz; maskotta göz/el hareketi yok (ayrı varlık gerekir).
- Metin: hepsi dinamik TMP, 9 dil (`rank.*`, `country.TR`). Font Nunito: ana 800, ikincil 600 (kullanıcı kararı).

## Puan

Kullanıcının bu konuşmadaki kesin kararı uygulandı (belge formülü kesinleşmemiş sayıyordu): `RankScore` = 10 × (0,5 × ZPE + 0,3 × ulaşılan bölüm/toplam × 100 + 0,2 × sıralama yıldızı/(3 × tamamlanan) × 100), 0–1000; giriş eşiği 20 bölüm; reklamla kazanılan yıldızlar sıralama yıldızına yazılmaz (`MetaSave.rankStars`). ZPE 0–100 olduğu için referanstaki "ZPE 910" gibi 3 haneli örnekler bizde 2 hanedir.

## Veri

- `ILeaderboardProvider` arayüzü. Sunucu yok → `NotConnectedLeaderboard` (oyuncu "Sıralama sunucusu henüz bağlı değil" görür; sahte satır yok).
- `DevSampleLeaderboard` yalnız `GameController.DevLeaderboardSample` açıkken (editör yakalaması/test) ve listede kırmızı "Geliştirme verisi · gerçek sıralama değil" etiketiyle.

## Varlıklar

- 19 PNG görünür sınıra (+4 px) kırpıldı → `Resources/PremiumV6/rank/`. Sprite, sRGB, mipmap kapalı, Bilinear, Clamp, en çok 4096; editör/Windows sıkıştırmasız (ilk kalite kontrolü), Android ASTC 4×4 (APK boyutu; cihazda bakılmalı).
- 9 dilim (köşeler incelendi): satır/sekme/kapsül uçları yükseklik yarısı (yalnız yatay esner), liste paneli 130 px, oyuncu kartı 200 px. Podyum, maskot, kupa, düğme, halka, madalya, rozet tek parça + Preserve Aspect.
- Liste çerçevesinin iç alfası için #F7F8F8 dolgu (arkada) + iç gri gölgeyi örten ikinci dolgu (kenar şeridinin içinde).
- Gümüş/bronz avatar halkası ve madalya pakette yoktu: altın görselden düz renk basmadan, altın pikseller parlaklığa göre gümüş/bakır geçişine eşlendi (gümüş kurdele mavi, bronz kırmızı) → `ring-silver/bronze`, `medal-silver/bronze`.

## Eksik bağımlılıklar

- **Sunucu:** Firebase bağlanmadı (hesap kullanıcıda). Firebase Spark (ücretsiz) planında Firestore kotası küçük ölçek için yeterli; sunucu tarafı puan doğrulaması için Cloud Functions gerekir ve bu Blaze (kullandıkça öde, ücretsiz kotalı) planı ister — karar kullanıcıda.
- **Avatarlar:** oyunda oyuncu avatarı yok → diğer oyuncularda adın baş harfi renkli dairede, kendi kartında maskot.
- **Bayraklar:** projede bayrak varlığı yok → ülke sekmesinde ISO kodu ("TR").
- **Kayıp kartı görüntüsü** (`reklam/kayip_kart_3_hamle.png`): editör yakalamasında HUD paneli giriş animasyonu ilerlemediği için kart görünmüyor; davranış PlayMode testinde doğrulandı.

## Reklam altyapısı (aynı turda)

Tek yönlendirici; günlük giriş (5 sn "Reklam aranıyor", bulunamazsa reklamsız giriş), kayıp kartında "Reklam izle: +3 hamle" (bölüm başına 1, hamle bitince; ZPE'ye girmez, kayıp ZPE'den geri çekilir), zafer kartında Devam'da geçiş reklamı (ilk 5 bölüm yok, 3 bölümde bir, ≥180 sn, ödüllüden 60 sn sonra, oturum sınırı 12). Oyunda TEST reklam ekranı; gerçek AdMob + Unity Ads SDK'ları hesap/kimlik gelince.

## Kontroller

- CoreTests 189/189, EditMode 189/189 (yeni: `RankScoreTests`, `AdsTests`), PlayMode 45/45 + 1 atlanan elle ölçüm (yeni: `RankScreenTests` — ilk 10 eksiksiz, podyum 2-1-3, podyum = liste verisi, sabit kart kaydırmada değil, sekme geçişi, sunucu yok, ülke yok, uygunluk; `V12_LossCard_AdExtraMoves...`; 50 bölüm örneklemi tohum 326906546).
- Görüntüler: 941×1672, 1080×1920, 1080×2400 çentikli, 1200×1600 kısa (liste kayar), Almanca uzun metin, bilgi paneli, harita düğmesi, test reklam ekranları.
- Telefonda denenmedi. Harita yolu, taşlar, bölüm dosyaları değişmedi (özet testi geçti).
