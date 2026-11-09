# TriKat — onaylı tasarım paketi

Tek referans 00_onaylanan_referans.png. Önceki pembe/lavanta ui2_uyarlama_onizleme maketleri bu paketin referansı değildir.

- sayfalar: 35 ekran için stil uyarlama PNG'leri; mevcut klasörü birebir kaynak ekranları içerir.
- arkaplanlar: Z1–Z5, yazısız ve taşsız tam ekran PNG. Rastgele seçim sadece görünüm durumudur, bölüm girişinde seçilir, deneme/tekrar boyunca sabit kalır.
- parcalar: alfa kanallı panel, buton ve 10 malzeme profili; import bilgileri asset-manifest.json içinde.
- korunan-orijinaller: taş atlası, özel taşlar, logo, maskot ve fontların değiştirilmemiş kopyaları.
- detaylar: 35 sayfa için konum/ölçü JSON ve açıklamalar; tipografi, boşluk, HEX renk, gölge, modal, zemin ve border tarifleri.
- uretim/istemler.json: dahili imagegen ile kullanılan istemler ve çıktı eşlemesi.

## Kaynak önceliği

İçerik, metin, buton sayısı, oyun davranışı, hücre dizilimi ve taş görünüşünde mevcut proje kaynağı esastır. Onaylı pano malzeme ve tasarım dilini tanımlar. Maket üretiminde küçük harf/çizim farkları olabilir; bunlardan yeni içerik veya yeni taş türetilmez. Dinamik sayılar maketten sabitlenmez.

## Ölçülerin anlamı

Detaylar 1080×1920 mantıksal tuval için hazırlanmış uygulama ölçüleridir; düşük çözünürlüklü onay panosundan kusursuz ölçülmüş değerler olduğu iddia edilmez. Maket PNG'lerinin üretim çözünürlüğü farklı olabilir. Tüm UI ölçeklemesi SafeArea içinde tek katsayıyla yapılır; scrim fiziksel ekranı kaplar.

## Çerçeveler

B01–B10 birer kaplamadır, tahta şekli değildir. Mevcut dış kontur ve iç delikler korunur. Border malzemesi kontur boyunca uygulanır; düz profil PNG'si dikdörtgen board sprite'ı olarak gerilmez. Dış/İç köşe UV ve ek yeri talimatı parcalar/KULLANIM.md içindedir. Bölümlere otomatik malzeme ataması bu teslimin kapsamı değildir; önceki spec içindeki 100'lük bölüm aralıkları zorunlu değildir ve kaldırılmıştır.

## Kod sınırı

Bu teslim yalnız tasarım dokümanı ve görsel dosyalardır. Assets, sahneler, prefablar, oyun mantığı, localization ve bölüm verilerine yazılmaz. Claude Code'a kod uygulama izni vermez.
