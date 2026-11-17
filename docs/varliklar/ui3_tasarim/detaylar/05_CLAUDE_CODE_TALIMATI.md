# Uygulayıcıya kesin sınırlar

Bu klasör şu anda yalnız TASARIM ŞARTNAMESİDİR. Bu çalışmada oyun kodu değiştirilmemiştir. İleride uygulama talimatı verilirse:

1. Önce OKU.md, tasarim-tokenlari.json, bilesenler/modal-varyantlari.json ve ilgili sayfalar JSONunu oku. Onaylıgörsel yalnız malzeme referansıdır; kesin sayısaldeğer bu dosyalardadır.
2. ui2, mevcutLoc.T/Loc.F/Case, runtime PanelButton dizisi ve görünürlükkoşulları içerik otoritesidir. Kaynak anahtar, sayı, metin veya action yeniden icat edilmez. Ekran başına kaynakdosyası ve satırlar icerik/source-kullanimlari.json içinde.
3. B01–B10 yalnızmalzeme. BoardShapePresetnull ve mevcutcells/hole konturlarını aynen kullan. Panodaki örnekhalkaları levelmask olarak aktarma.
4. Taşatlası, taşyüzü, yanrenkler, pitch, katman ve hit-test mevcutkalır. Core, LevelData, save,ads,reward,ZPE,rank hesapları değişmez. KaynakRGBA/mesh işleyişini bozacak globalUIshader değişikliği yapma.
5. Tasarımpaletine göre görseldekiler haricinde kalan düz boyalı yüzeyleri açık tokenla boya. Font, padding, alignment, gap için bu sözleşmede verilen değerler dışında estetik karar verme.
6. Çakışmada öncelik: kullanıcı sabitleri / mevcutoyunmantığı > mevcutmetinveeylem > tokensJSON > sayfalayoutJSON > malzemepanosu. Genelrol ile sayfaoverrideçelişirse sayfaoverride geçerli; kaynakmantığına aykırıysa dur ve somutçelişkiyi bildir, içerikdeğiştirerek çözme.
7. Değişiklikler ayrıca yetkilendirilmeden bu belgeden uygulamaya geçme. Yetki gelince mevcutcheckout/kullanıcıdeğişikliklerini koru; yeni paket ekleme.

## Kabul ölçütleri

- Bütün35ekran, mevcuttüm yazı/düğme/özelliğiyle görünür; yeni içerik yok.
- Aynı bölüm tümB01–B10görsellerinde aynıcells,holes,tilecount ve colliderlara sahip. Asimetrik,içdelikli,L/U/darboğaz şekilleri ayrıca sınanır.
- BütünZ1–Z5 + borderkombinasyonlarında hedef/taş/renklerokunur; motifstoneüstüneçıkmaz.
- Her modalın ekran kenarları ve delikleridahil dışboşluğu0.72karanlık, inputblockmevcutduruma uygun; nested toplam0.82.
- TürkçeÇĞİıÖŞÜ ve9dilfallbacktaşıyor mu kontrol; tammetin kesilmez.
-1080×1920,1080×2400,720×1280,tablet4:3 safearea fit/letterboxpattern kontrol; boyutlar tek kez ölçeklenir.
- Rastgelezemin sunumRNGsi CoreRNG/oyunsonucunuetkilemez, attemptboyunca sabittir.
- Kaynak ekran yakalama akışı capture-ui2 ile yeni görseller üret; yapılmayan cihaz/performanskontrolünü yapılmış bildirme.
