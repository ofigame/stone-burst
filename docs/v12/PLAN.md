# TriKat v12 — Plan (2026-11-08, henüz başlanmadı)

Kullanıcının 2026-11-08 kararları ve sıradaki işler. Kod yazılmadı. Açık sorular en altta.

## 1. Reklam (karar)

| Konu | Karar |
|---|---|
| Arabulucu | **AdMob** (mediation). Ağlar: **AdMob + Unity Ads**. |
| Reklamsız paket | Şimdilik **yok** (OPEN_DECISIONS #18, #19 ertelendi). |
| Hedef yaş | **13+** (OPEN_DECISIONS #16). Mağaza beyanı ve reklam isteğinde buna uygun ayar; hukuki metin ayrıca. |
| Banner | Yok (ana plan §9). |
| Günlük giriş | Ödüllü reklam. Reklam aranırken **"Reklam aranıyor" + 5 sn geri sayım**; 5 sn içinde reklam gelmezse oyuncu günlük bulmacayı **reklamsız oynar** (OPEN_DECISIONS #2). İptal eden oyuncu girmez (kötüye kullanım). |
| Ödüllü reklamlar | Günlük giriş, +1 hak (bkz. §3), ek hamle (yerleşim §6'da açık). Ödül yalnız tamamlanma geri çağrısında, bir kez. |
| Geçiş reklamı | Ana plan §9 çerçevesi (ilk 5 bölüm yok, ~3–4 bölümde bir, en az 180 sn, oyun sırasında asla, ödüllüden hemen sonra değil); tam an §6'da açık. |
| Reklam ve ZPE | Reklamla (ek hamle vb.) kazanılan bölüm/yıldız **ZPE'yi etkilemez**; ZPE yalnız gerçek oyun performansı. |

İş kalemleri: P6.1 gerçek `IRewardedAds` (AdMob + Unity Ads arabulucu), geçiş reklamı denetleyicisi (uzaktan ayar), reklam arama ekranı + 5 sn geri sayım + yedek giriş, rıza/UMP akışı (13+), cihazda iptal/hata/çift geri çağrı testleri. Hesap ve uygulama kimlikleri kullanıcıdan (canlı anahtar onaysız eklenmez).

## 2. ZPE değişiklikleri (karar)

- **Reklam yardımı hariç:** reklamla ek hamle alınan oturum ZPE'ye girmez (puan da oyun sayısı da değişmez).
- **Yardımsız bitirme bileşeni (yeni kural):** her bölüm için "gereken yardım" referansı (LevelLab, doğrulanmış çözümden). Oyuncunun kullandığı yardım ≤ gereken ise bileşen **tam (yardımsız sayılır)**; yalnız gerekenden **fazlası** düşürür. Bölüm yardımsız çözülebiliyorsa (gereken 0) her yardım düşürür.
- "Yardım" tanımı netleşecek (aşağıda açık soru 3).

## 3. Hak (can) sistemi (öneri — kullanıcı seçecek)

Kullanıcının önerisi: 5 hak; bölüm çözülmeden biterse 1 hak gider; reklamla +1; her hak 30 dk'da geri gelir.

Claude önerisi (aynı temel, sinir bozmayan ayrıntılarla):
- 5 hak, 30 dk'da bir dolar (tam dolum 2,5 saat). Sayaç cihaz saati değil, mümkünse sunucu saati (hile).
- Hak **yalnız kayıpta** (hamle bitti) ya da **ilk hamleden sonra çıkışta/yeniden başlatmada** gider; hiç hamle yapmadan çıkmak ücretsiz.
- Kazanınca hak gitmez.
- İlk 20 bölüm (öğrenme) hak harcamaz.
- Reklamla +1 hak; günde en çok 5 reklam hakkı.
- Hak bitince: geri sayım + "Reklam izle, +1 hak" + Günlük bulmaca (ayrı hak sistemiyle) ve Koleksiyon açık kalır.
- Günlük bulmaca kendi haklarıyla kalır (bu sistemden ayrı).

## 4. Dünya ve bölge sıralaması (karar + öneri)

- Gerçek kişiler, gerçek puanlar (sunucu gerekir; öneri: Unity Gaming Services Leaderboards ya da Firebase; AdMob/Unity Ads ile uyumlu).
- Ekran: genel tasarıma uygun; **dünya** ve **bölge** sekmeleri; her birinde ilk 10 + oyuncunun kendi sırası. Tasarımı kullanıcı verecek.
- Sıralama puanı = ZPE, ulaşılan bölüm ve yıldızın ortalaması. Öneri (her biri 0–100'e normalize, ağırlıklı):
  `Puan = 0,4 × ZPE + 0,4 × (ulaşılan bölüm / toplam bölüm × 100) + 0,2 × (yıldız / (3 × tamamlanan bölüm) × 100)`, ekranda ×10 (0–1000).
  Yeni oyuncunun tek iyi bölümle zirveye çıkmaması için sıralamaya giriş eşiği (ör. 20 bölüm).
- Hile: istemciden gelen puan sunucuda sınırlarla doğrulanır (bölüm/yıldız tavanı, zaman, olağan dışı sıçrama).

## 5. Açılış logosu

OFIGAME logosu basık görünüyor: oran korunarak çizilecek (`preserveAspect`, kaynak en-boy oranı), farklı en-boy oranlı ekranlarda denenecek.

## 6. Bölüm tasarımı (v8 uygulandı, 2026-11-08: [V8_RAPOR.md](V8_RAPOR.md))

1000 bölüm zorluk ve strateji çeşitliliği için yeniden ele alınacak; öneriler sohbette. Not: üretim bölümleri değişmez kuralı (özet testi) var; yeniden üretim kullanıcının açık onayıyla yeni sürüm (v8) olarak yapılır.

### 6.1 Kural verilir, strateji verilmez (karar, kullanıcı 2026-11-08)

- Kural, kısıt ve hedef oyuncuya her zaman açıkça gösterilir; strateji (nasıl çözüleceği) asla söylenmez.
- **Kural kartı:** yalnız kısıtlı bölümde (koruma, özel taş yok, yalnız zincir sayılır…) ve patron bölümünde, bölüm açılırken; büyük simge + tek cümle; dokununca kapanır. Normal bölümde kart yok.
- **Kalıcı kural simgeleri:** hedef alanının yanında; dokununca kural cümlesi yeniden görünür. Simge dili (9 dilde aynı).
- **Yeni tür tanıtımı:** bir türün ilk bölümünde bir kez, türü tanıtır, çözümü değil.
- **İpucu:** yalnız oyuncu isterse, birkaç kayıptan sonra önerilir; yalnız bir sonraki doğru hamle; ipucuyla çözülen bölüm ZPE'de yardımlı.
- Kısıt türü sayısı az ve sabit (6–8), hep aynı simgelerle; çeşitliliği tahta kurgusu yaratır.

### 6.2 Bölüm tasarımı önerileri (kullanıcı beğendi, 2026-11-08)

Tam bilgi (uzun basınca yığının katman sırası), bölüm türleri (kesin hamle, tek dokunuş, zincir mühendisliği, renk kıtlığı, sıralama/bağımlılık, kaynak saklama, koruma, ters düşünme, adalar), yeni mekanikler (saatli yığın, bukalemun katman, ayna çifti, beşinci renk, kilitli tepsi), testere dişi eğri (10'luk döngü + patron, 100'lük bölge sınavı), LevelLab zorluk ölçüleri (öngörü derinliği, tuzak oranı, çözüm sayısı, sezgiye ters hamle, tür etiketi), görünüm çeşitliliği, zor bölüm + hak dengesi (ipucu, patronda ücretsiz yeniden başlatma). İlk adım: 10 bölümlük deneme seti; onaydan sonra 1000 bölüm v8 olarak yeniden üretim (özet testi kuralı gereği açık onayla).

### 6.3 Günlük bulmacalar da yeni kurallarla (kullanıcı, 2026-11-08)

- Günlük bulmacalar ana bölümlerle aynı yeni tasarım kurallarına uyar: bölüm türleri, tam bilgi, kural kartı ve kalıcı kural simgeleri (strateji verilmez), yeni mekanikler, LevelLab zorluk ölçüleri, yalnız istenince bir hamlelik ipucu.
- Günlük içerik oyuncunun seviye grubuna göre kalır (8 grup); her grupta yalnız oyuncunun gördüğü türler/mekanikler. Günün türü haftalık döner (ör. Pazartesi kesin hamle, Salı zincir mühendisliği…), aynı tür art arda gelmez.
- "Çok zor" kararı korunur; zorluk artık bot kazanma oranı yerine yeni ölçülerle (öngörü derinliği, tuzak oranı, sezgiye ters hamle) ayarlanır.
- Uygulama sırası: 10 bölümlük deneme seti onaylanınca aynı üretici günlük içeriği de yeniden üretir (yeni içerik sürümü `daily-v3`; eski `daily-v2` arşivlenir). Günlük ilerleme kaydı içerik sürümüyle tutulduğu için mevcut kayıtlar bozulmaz.

## Sıra

Dünya sıralaması ekran tasarımı kullanıcıdan gelince başlanır.

## Açık sorular

1. "Bölge sıralaması" ülke mi, oyundaki harita bölgesi (Mavi Liman…) mi?
2. Hak sistemi: kullanıcının önerisi mi, Claude önerisi mi?
3. ZPE'de "yardım" sayılanlar: geri alma, günlük +1 hamle, taş gücü, reklam hamlesi. Oyunda kazanılan roket/rulo/renk temizleyici yardım değil, oyunun parçası. Onay?
4. Reklamla kazanılan yıldızlar sıralamadaki yıldız puanına girsin mi?
5. Ek hamle reklamı: kayıp kartında mı, kaç hamle, bölüm başına kaç kez?
6. Geçiş reklamının anı: zafer kartından sonra "Devam"da mı, haritaya dönüşte mi?

## Karara bağlananlar (kullanıcı, 2026-11-08, ikinci tur)

1. Bölge sıralaması **ülkeye** göre.
2. Sıralama puanı (0–1000): **%50 ZPE + %30 ulaşılan bölüm + %20 yıldız oranı**; sıralamaya giriş eşiği 20 bölüm (öneri, itiraz edilmedi).
3. Reklamla (ek hamle) kazanılan yıldızlar sıralamada **sayılmaz** (kayıtta reklamsız en iyi yıldız ayrıca tutulur).
4. Hak sistemi: Claude önerisi kabul (5 hak, 30 dk dolum, yalnız kayıpta ya da ilk hamleden sonra çıkışta gider, ilk 20 bölüm harcamaz, reklamla günde en çok 5 hak, patronda yeniden başlatma ücretsiz).
5. Yardım tanımı (ZPE): geri alma, günlük +1 hamle, taş gücü, reklam hamlesi, ipucu. Oyunda kazanılan roket/rulo/renk temizleyici yardım değil.
6. Ek hamle reklamı: kayıp kartında "Reklam izle, +3 hamle", bölüm başına 1 kez (yalnız hamle bitince; hamle kalmadıysa değil, hamle yok kaybında değil).
7. Geçiş reklamı: zafer kartında **"Devam"** basılınca (ilk 5 bölüm yok, ~3 bölümde bir, en az 180 sn, ödüllüden hemen sonra değil).
8. Hesapları kullanıcı açacak. Şimdi: altyapı + her reklam yerinde **test reklam ekranı**. Sıralama: Firebase ücretsizse Firebase.
