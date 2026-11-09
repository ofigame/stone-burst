# TriKat · Açık kararlar

Tarih: 2026-11-06. Kesinleşmiş 8 karar (ana plan §A) burada yeniden tartışılmaz.

Her kararda şunlar var: bugünkü prototip değeri, önerim ve kararın etkilediği iş (bkz. [IMPLEMENTATION_BACKLOG.md](IMPLEMENTATION_BACKLOG.md)). Karar verilene kadar değerler yapılandırılabilir kalır.

## Ürün ve oyun

| # | Karar | Bugün (prototip) | Önerim | Etkilediği iş |
|---|---|---|---|---|
| 1 | Günlük sıfırlama saat dilimi | Cihazın yerel gece yarısı | Sabit UTC ve ekranda "yeni bulmacaya kalan süre". Türkiye için UTC 00:00 = 03:00. Alternatif: UTC+3 gece yarısı (yalnız Türkiye odaklıysa). | P3.8, P6A |
| 2 | Reklam yüklenemezse günlük giriş | Giriş açılmıyor, hak gitmiyor | Kısa bir bekleme ve yeniden deneme; ikinci başarısızlıkta o gün için ücretsiz giriş (kötüye kullanım sınırıyla). | P3.6 |
| 3 | Taş ilk deneme ile tekrar arasında değişebilir mi? | Seçim yok | Evet, değişebilir (ikinci şans hissi); güç her denemede bir kez. | P3.3 |
| 4 | +1 hamle hangi bölümlere? | Yalnız henüz geçilmemiş normal bölümler | Plan "o gün oynanan her normal bölüm" diyor. Tekrar oynanan bölümde yıldız iyileştirmeye yarar; rekabette yardımlı sayılır. Plana uymayı öneriyorum. | P3.5 |
| 5 | Bilerek çıkış denemeyi harcar mı? | Evet | Evet kalsın (çık-gir ile bedava yeniden başlatma olmasın). Sunucu gelince kapanma ile bilerek çıkış ayrımı sunucuda. | P3.8 |
| 6 | Günlük içerik modeli | **Karar verildi (kullanıcı, 2026-11-06)** | Oyuncunun seviyesine göre: 8 grup, her gün her grup için ayrı bulmaca; grup o gün için sabit. Sonuç: ileride sıralama/paylaşım grup içinde yapılmalı. 30 günlük katalog. | P3.4 ✔ |
| 7 | Günlük zorluk hedefi ("10 kat zor hissi") | **Karar verildi (kullanıcı: "çok zor")** | Plansız bot kazanma oranı hedefi %4 (kabul %0,5–12), hamle payı +1/+2/+3; kayıtlı çözüm güçsüz kazanıyor. Ölçülen grup ortalaması %3,4–4,4. Gerçek oyuncuyla denenmedi. | P3.4 ✔ |
| 8 | Yıldız taşı kazanımı | **Kısmen karar verildi (kullanıcı, 2026-11-06):** ilk taş (Gelgit Taşı, `stone_tide`) 10. bölüm tamamlanınca verilmeye devam eder. | Kalan 9 yıldız taşının kuralı (yıldız eşiği mi, bölüm mü) taşlar eklenirken sorulacak. | P2.2 |
| 9 | Gizli hazine koşulu | Yok | Tahta dışı süs nesnesi; koşul "hamle payının en az yarısıyla bitirmek" gibi ölçülebilir bir eşik; yer: içi boş tahtalar. Bölüm dosyasına dokunulmaz. | P2.3 |
| 10 | Efsanevi taş koşulu | Yok | Dünyanın (500 bölüm) tamamlanması; ustalık şartı (ör. hepsi 3 yıldız) sonraki sürüm. | P2.4 |
| 11 | Dünya düzeni ve temalar | **Kısmen karar verildi (kullanıcı, 2026-11-07): adlar şimdilik gizli** | Mevcut görünüm aynen kalsın; 500'lük dünya yalnız gruplama (dünya 1 = bölgeler 1–5). "Akdeniz Sahili / Çiçek Bahçesi" adları dünya adı olarak eklenebilir. Temaları yeniden eşlemek görsel değişiklik olduğu için ayrı onay. | P1.5 ✔ (veri); adlar ve tema eşlemesi açık |
| 12 | 22 taşın ve iki dünya haritasının görselleri ilk sürüme yetişir mi? | **Taş görselleri geldi ve oyunda; eşleme kullanıcı onaylı (2026-11-07)**: `docs/taşlar/` içinde 22 saydam PNG (~1180×1330). 9 taşın 3D modeli de var (Tripo FBX + 4K dokular, taş başına ~45–55 MB). Kalan 13 taşın 3D modeli sonra yapılacak. Oyunda hâlâ geçici simge var. | Hangi görselin hangi taş olacağı (10 yıldız taşı, 10 gizli hazine, 2 efsanevi) ve güç eşlemesi karar bekliyor. 3D modeller bu hâliyle mobil için ağır; oyuna alınmadan önce sadeleştirilmeli. Dünya haritası görselleri yok. | P2.6 |
| 13 | Güç ailelerinin ilk sürüm listesi | Kodda hazır (P3.2): gelgit (rulo), okyanus (satır roketi), fırtına (sütun roketi), galaksi (Altın Rulo), ay (+2 hamle). Hepsi oyunun mevcut mekanikleriyle çalışıyor. | Ateş (3×3 alan) ve diğerleri kural motoruna yeni etki ekletir; ilk sürümde bu beşiyle yetinmeyi öneriyorum. Ay'ın +2 değeri denge kararı. | P3.2 |

## ZPE ve gizlilik

| # | Karar | Önerim | Etkilediği iş |
|---|---|---|---|
| 14 | Herkese açık ZPE kapatılabilir mi; rekabetçi ZPE için en az kaç bölüm? | Evet, profilde gizlenebilir; rekabetçi ZPE için en az 30 bölüm ve kalibrasyon sonrası. | P5.4, P5.5 |
| 15 | Yardımlı oyunlar ZPE'ye nasıl girer? | Rekabetçi ZPE'de yardımlı oturumlar (günlük +1, reklam, güç, geri alma) hesaba katılmaz; kişisel ZPE'de ayrı normalize edilir. | P5.3 |
| 16 | Çocuk kullanıcı ve bölge politikası | Hedef kitle 13+ olarak beyan edilsin; çocuklara yönelik oyun sınıfına girilmezse reklam ve analitik kuralları basitleşir. Hukuki görüş gerekir. | P6.5, P7.6 |

## Gelir

| # | Karar | Önerim | Etkilediği iş |
|---|---|---|---|
| 17 | Reklam ağı | Huawei için ayrı ağ gerekebilir (ör. Huawei Ads); Google/Apple için yaygın bir arabulucu (mediation). Seçim P0.4 ile birlikte. | P6.1 |
| 18 | Reklamsız paket mağazalar arası taşınır mı? | İlk sürümde hayır (her mağaza kendi satın alması); hesap bağlıysa ileride sunucuda birleştirme. | P6.3 |
| 19 | Reklamsız paketin ödüllü reklamlara etkisi | Günlük giriş reklamsız; ek hamle gibi isteğe bağlı ödüller yine isteğe bağlı reklamla (ya da paket sahibine ücretsiz). Mağaza metninde açıkça yazılmalı. | P6.3 |

## Teknik ve altyapı

| # | Karar | Önerim | Etkilediği iş |
|---|---|---|---|
| 20 | Sunucu ve servis seçimi | Önce araştırma raporu (kod yok), sonra karar. | P0.4 → P4 |
| 21 | Paket kimliği `com.ofigame.katkat` → `com.ofigame.trikat` | Oyun hiçbir mağazada yayımlanmadıysa **şimdi** değiştirelim (sonradan değiştirmek yeni uygulama demek). Yayımlandıysa mevcut kimlik kalsın. | P0.5 |
| 22 | Huawei bildirim ve analitik | HMS-only cihazda deneme yapılmadan karar yok; Huawei Push Kit büyük olasılıkla gerekli. | P6A.3 |
| 23 | "Aktif oyuncu" tanımı | Günlük/haftalık/aylık aktif oyuncu ve geri dönüş (kapsamda); canlı "çevrimiçi" bilgisi ayrı özellik, şimdilik yok. | P6A.2 |
| 24 | Günlük hatırlatıcı bildirimi | Yalnız oyuncu izin verirse; varsayılan saat 19:00 yerel, sessiz saatler 22:00–09:00, günde en fazla 1. | P6A.4 |
| 25 | Günlük bulmaca internet gerektirir mi? | Sunucu gelince evet (hak ve ödül sunucuda); normal bölümler her zaman çevrimdışı. | P3.8 |
| 26 | iOS derlemesi için Mac ve Apple geliştirici hesabı | Kullanıcının sağlaması gerekir. | P7.4 |
| 27 | Oyunun mağazada yayımlanıp yayımlanmadığı ve canlı oyuncu ilerlemesi olup olmadığı | Kullanıcı doğrulamalı; #21'i belirler. | P0.5 |

## Karara bağlananlar (kullanıcı, 2026-11-08) — ayrıntı: [v12/PLAN.md](../v12/PLAN.md)

- #2: Reklam 5 sn içinde gelmezse günlük bulmaca reklamsız oynanır ("Reklam aranıyor" + geri sayım).
- #15: Reklam yardımlı oturum ZPE'ye girmez; yardımsız bileşeni bölümün "gereken yardım" referansına göre (fazlası düşürür).
- #16: Hedef yaş 13+.
- #17: AdMob arabulucu; AdMob + Unity Ads.
- #18, #19: Reklamsız paket şimdilik yok.
