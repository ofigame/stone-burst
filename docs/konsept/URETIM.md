# Konsept pilotu üretim raporu (LevelLab concept, tohum 20261115)

Ana 1–1000 bölüme dokunulmadı. Her bölümün kayıtlı çözümü motorla (ve JSON geri okunarak) yeniden yürütülüp kazandığı doğrulandı.

**Kabul şartları:**
- Açgözlü bot tehditle **kaybeder**. Açgözlü bot her hamlede hedefe en çok katkı yapan hamleyi seçer.
- Tehdit kaldırılınca aynı bot, aynı hamle sınırıyla **kazanır**. Yani bölümü kaybettiren tehdittir, hamle sınırı değil; oyuncu tehdide göre plan yapmak zorundadır.
- En kısa çözüm en az 5 hamledir.
- Yasal ilk hamlelerin en çok yarısı kazandırır.

Ölçümler bot ve çözücüyle yapıldı; oyuncu testi değildir.

| # | Konsept | Amaç | Tahta | Hamle | En kısa çözüm | Tehdit ayarı | Kazandıran ilk hamle | Açgözlü botun kaybı |
|---|---|---|---|---|---|---|---|---|
| K01 | Çürüyen Zemin | Alttan baskı: önce alt sıraları temizle | 6×7 | 12 | 10 | her 2 hamlede çöküş | 1/6 | hedef taşları kaybolur |
| K02 | Fitil | Yerel geri sayım: doğru yığını zamanında boşalt | 6×6 | 8 | 5 | fitil 3 ve 5 | 2/7 | fitil patlar |
| K03 | Dönen Hedef | Zamanlama: doğru renk etkinken topla | 6×6 | 8 | 8 | 2 hamlede renk değişir | 1/3 | hedef taşları kaybolur |
| K04 | Kuzgun | Hareketli düşman: yolunu bir yığını boşaltarak kes | 7×7 | 11 | 9 | yol 7 hücre | 2/4 | kuzgun yuvaya varır |
| K05 | Yağmur | Taş eklenir: yağmur alan yığınları alçak tut | 6×6 | 9 | 6 | her hamlede yağmur, sınır 4 | 1/6 | yığın taşar |
| K06 | Gölge Rakip | Saf yarış: büyük gruplarla rakibi geri it | 7×7 | 11 | 5 | rakip bitişi 3 | 2/6 | rakip önce bitirir |
| K07 | Yayılan Yosun | Yayılma: yosunu kaynağında söndür | 6×7 | 10 | 7 | her hamlede yayılır, sınır 6 | 2/6 | yosun tahtayı sarar |
| K08 | Balonlar | Kaçan hedef: balonun yolundaki yığını boşalt | 6×7 | 8 | 6 | 3 balon, her hamlede yükselir | 1/2 | balon kaçar |
| K09 | Sipariş Kuyruğu | Sıralama: siparişleri süresinde teslim et | 6×6 | 10 | 7 | sipariş süreleri 3/2/3 | 1/4 | sipariş yanar |
| K10 | Kombo Ateşi | Ritim: zincirleri kesmeden sürdür | 6×6 | 10 | 8 | ateş 2 hamle | 1/3 | ateş söner |

Notlar:
- "Kazandıran ilk hamle", ilk hamleden sonra bütçe içinde doğrulanmış bir çözüm bulunan yasal hamle sayısının, bütün yasal hamlelere oranıdır. Bütçe dolan dal kazandırmaz sayılır.
- Komut: `dotnet run -c Release -- concept 20261115 NN` (NN boşsa 10 bölüm sırayla). Paralel üretimde rapor bu dosyaya elle aktarıldı.
