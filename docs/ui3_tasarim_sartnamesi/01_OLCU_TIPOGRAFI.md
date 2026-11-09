# Ölçü, yazı ve yerleşim standardı

Bu değerler onaylanan görsele uygun KESİN UYGULAMA KARARLARIDIR; düşük çözünürlüklü panodan piksel ölçülmüş gibi sunulmaz. JSON değerleri bağlayıcıdır. Kaynakta olmayan içerik ekleme kararı içermez.

## Koordinat ve ölçek

Referans1080×1920, köken sol üst, birim reference-px. Tuple x,y,width,height. Temel aralık8; dış yatay48; minimum temas96×96. Grafik taşması temas alanını büyütmez. Menü altında208px navigasyon.

Unity: mevcut CanvasScaler çıktısında SafeArea ekran koordinatlarını canvas yerel birimine bir kez çevir. SafeDesignRoot sabit1080×1920; s=min(safeCanvasWidth/1080,safeCanvasHeight/1920), merkez SafeRect merkezi; root uniform(s,s,1). Mevcut scaler dönüşümü ikinci kez uygulanmaz. Arka plan tüm fiziksel ekrana taşar; UI güvenli alandadır. Çentik alanında ekstra kontrol yok. 1080×2400 ekranda tam ekran desen uzar ama1080×1920 içerik dikey ortalanır. Landmark harita ve tahta geometrisi eski veriden gelir.

Element rect için anchorMin=anchorMax=(0,1), pivot=(0,1), anchoredPosition=(x,-y), sizeDelta=(w,h). Merkezli sayısal plakalarda textRect ayrıca aşağıdaki insets ile kurulacak; parent anchor başka seçilip koordinat karıştırılmaz. Görseller preserveAspect=true; paneller Image.Sliced; tahta çerçevesi Sliced DEĞİL kontur bazlı.

## Yazı rolleri

Nunito ExtraBold800 ve SemiBold600. Sahte Bold yok; ekstra FaceDilate0, karakter aralığı0, kontur0. Başlıklarda sadece gerektiğinde rol bazlı mevcut SDF underlay: renk #302C2A, alpha0.16, offset(0,-1), blur2; gövde ve sayıda yok.

| Rol | Boyut | Ağırlık | Satır yüksekliği | Hizalama | Renk |
|---|---:|---:|---:|---|---|
| pageTitle | 56 | 800 | 68 | center | #203846 |
| modalTitle | 54 | 800 | 66 | center | #203846 |
| ribbonTitle | 58 | 800 | 70 | center | #FFF9ED |
| sectionTitle | 40 | 800 | 50 | left | #203846 |
| button | 44 | 800 | 54 | center | #FFF9ED |
| buttonSecondary | 44 | 800 | 54 | center | #203846 |
| body | 36 | 600 | 46 | left | #203846 |
| bodyCentered | 36 | 600 | 46 | center | #203846 |
| caption | 28 | 600 | 36 | center | #53646D |
| metric | 50 | 800 | 62 | center | #203846 |
| moves | 64 | 800 | 78 | center | #FFF9ED |
| goal | 38 | 800 | 48 | left | #203846 |
| nav | 32 | 800 | 40 | center | #203846 |
| rowTitle | 38 | 800 | 48 | left | #203846 |
| rowSub | 28 | 600 | 36 | left | #53646D |
| smallButton | 34 | 800 | 44 | center | #203846 |
| legal | 26 | 600 | 34 | center | #53646D |
| zpeValue | 88 | 800 | 104 | center | #118C83 |

TMP satır yüksekliği hedefi tabloda belirtilen px; font metriklerine bağlı varsayılan lineSpacing bırakılmaz. Aynı rol için baseline-to-baseline mesafe hedefe ayarlanır. Orta hizalı kutuda TMP Midline kullanılacak; optik offset y=-2px, diğer kutularda0. Metin kutusu içinde glyph outline değil TMP padding hesabı kullanılır.

## İç boşluklar / metin konumu

- Büyük düğme:116yükseklik; dış radius58; metin insets L36,T20,R36,B20; metin84? değil kalan76px yüksek kutuda midline-center; optik yukarı2px. Primary44/800, secondary44/800. İkon varsa40×40, ikon+yazıgrubu merkezde, araları16px.
- Round kontrol:112×112; glyph56×56 ortalı; dokunma112; close88grafik/104hit; icon rays çizime eklenmez.
- Kapsül:104yükseklik; yatay24 düşey16; hedef ikonu72×72; ikon sonrası16; rakam38/800 soldan; bölücü çizgi eklenmez.
- Modal:860genişlik; içerik x56..804 (748); başlık x56,y80,w748,h80; subtitle y184,h104; eylemler y320+140i. Kart toplam h=384+116n+24max(n-1,0). 3düğme için780; 4 için920. Başlık iki satır olursa ek66 ve sonraki bloklar66 aşağı.
- Ayar satırı:132yükseklik; ikon72sol; label x104,w396,h64 ortalı; sağtoggle176×88; ikon/label arası32, sağkenar0. Dil düğmesi280×104.
- Liste satırı: title38/800; secondary28/600; titlekutusu y20,h48, altmetin y76,h72; satır metin alanı ikondan24 sonra başlar.
- Koleksiyon kartı:468×484; padding24; taşgörsel340×252, x64,y24; adx24,y292,w420,h56; açıklamax24,y360,w420,h100. Kilit88×88, artalanının merkezinde; bulanık kaynağın kendisi değiştirilmez.

## Uzayan metin

Türkçede sabit boyut, kelime sarmalama. Gövde kutusunun gerçek height=max(tablodaki height, TMPpreferredHeight+32); takip eden blokların y değeri aynı artışla ilerler. Kart grubunun tamamı SafeRect yüksekliği-96'dan uzunsa yalnız gövde/rule/liste bölgesi scroll olur; başlık ve düğmeler sabit. Metin kesilmez, üç nokta yapılıp bilgi gizlenmez, yeni kısaltma yazılmaz. Düğme etiketinde önce aynı116yükseklikte iki satıra izin ver; iki satırda 38/800, baseline46. İki satır da sığmazsa yatay padding24, ardından font36 alt sınır; hâlâ taşarsa hata raporla, metin değiştirme. Rakam38altına düşürülmez. Arapça şekillendirme ve RTL mevcut Loc/ArabicShaper; sayı yönü mevcut davranış.9dilin tamamında Noto fallback korunur.
