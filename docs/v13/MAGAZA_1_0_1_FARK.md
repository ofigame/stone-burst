# App Store 1.0.1 — 4.3 reddi sonrası mağaza metinleri ve görseller

Tarih: 2026-11-13. Sebep: 1.0 (yapı 12) **Guideline 4.3(a) Design – Spam** ile reddedildi (Apple: "benzer ikili, meta veri ve/veya konsept"; DURUM.md). Amaç: oyunu "3'lü dokun-patlat" türdeşlerinden ayıran özellikleri metinde ve görsellerde öne çıkarmak. Kullanıcı kararı (2026-11-12) korunur: "1000 bölüm" ifadesi yok.

Ana dili o dil olan biri kontrol etmedi (özellikle Arapça ve Japonca). Alanlar App Store Connect → 1.0.1 (ya da reddedilen 1.0) → dil → Promotional Text / Description / Keywords. Ad ve alt başlık değişmiyor.

Eski metinlerden farklar:
- Anahtar kelimelerden türün kalabalık terimleri çıktı: `match`, `blast`, `tiles`, `tap` (Apple'ın "trending topics, popular search terms" uyarısı). Yerine oyunun kendi kavramları: katman, yığın, soyma, planlama, strateji.
- Açıklama "rahatlatıcı eşleştirme" yerine oyunun kuralıyla açılıyor: yığın, gizli alt renk, zincir kuralı, sınırlı hamlede planlama.
- Özellik listesi oyunda gerçekten olan ve türde yaygın olmayan mekanikleri sayıyor (hepsi koddan/OYUN.md'den doğrulandı): özel taş tepsisi (hamle harcamadan istenince), engeller (buz, renk kilidi, zarf, sünger, geçit ve düğme, döner taban, ataş, kristal), boya, günlük kurallı bulmaca, ZPE beceri endeksi, 3B taş koleksiyonu, her bölümün kayıtlı çözümle doğrulanması, doğrulanmış ipucu.

## Görseller (`docs/magaza/fark/`)

Sahne stili (kullanıcı onaylı 2026-11-12 stili: maskot + mor şerit), 8 ekran, 6 dil (en, tr, de, es, ar, ja), iPhone 1206×2622 ve iPad 2064×2752:

| No | Ekran | EN başlık |
|---|---|---|
| 01 | Katmanlı tahta + uzun basma katman görünümü | EVERY STONE HIDES ANOTHER COLOR |
| 02 | 60. bölüm, son hamle kala (zincir planı) | PLAN YOUR CHAIN REACTIONS |
| 03 | Altın Rulo ateşi | THE GOLDEN ROLL CLEARS A COLOR |
| 04 | Buzlu taşlar, buzlu koleksiyon taşı, geçit (dizin 635) | BREAK THE ICE, OPEN NEW PATHS |
| 05 | Harita | JOURNEY FROM REGION TO REGION |
| 06 | Günlük bulmaca kural kartı | A DAILY PUZZLE WITH NEW RULES |
| 07 | ZPE beceri endeksi | RAISE YOUR SKILL SCORE |
| 08 | 3B taş vitrini | COLLECT 3D STONES |

**Yüklendi (2026-11-13/14, Claude, Chrome; ASC verisinden doğrulandı: 6 dil × iPhone/iPad × 8 görsel, metinler kayıtlı).** Yükleme yolu: App Store Connect → dil → Previews and Screenshots → "iPhone with Dynamic Island (medium display)" sekmesine `fark/iphone/(dil)`, "iPad 13\" display" sekmesine `fark/ipad/(dil)`; eski 6 görsel silinir, yeni 8'i 01–08 sırasıyla. Spanish (Spain) için `es`. Üretmek: `KATKAT_STORE_SET=fark tools/unity.ps1 capture-store` (pilot girişi çekimde gizlenir).

---

## English (U.S.)

**Promotional Text** (170)
Every stone hides another color. Peel layered stacks, plan chain reactions in limited moves and save power stones for the right moment.

**Description**
Stone Burst is a puzzle game about what lies beneath.

Each cell on the board is a stack of up to 4 stone layers, and you only see the top one. Tap 3 or more touching stones of the same color to peel one layer from each. What comes up next is up to the board: if the newly revealed stones form a group of 3, they burst on their own, wave after wave. With a limited number of moves, the real puzzle is choosing which group to peel first. Long-press any stack to see its hidden layers.

WHAT MAKES IT DIFFERENT
• Layered stacks: plan two or three layers ahead, not just the next tap
• Chain rule: only freshly revealed stones chain, so every cascade can be planned
• Power stone tray: big groups create rockets and the Golden Roll; they wait in your tray and fire only when you choose, without using a move
• Obstacles that change how you think: ice, color locks, envelopes, sponges, gates with buttons, turning bases, paperclips and fragile crystals
• Paint drops: recolor a single stone when one color is missing
• Daily puzzle with its own rule every day
• ZPE skill index: move efficiency, first-try wins, solving speed and no-help finishes, all in one score
• 3D stone collection: earn stars to unlock collectible stones you can rotate and inspect
• Every level is checked by a solver with a recorded solution; hints show a verified move, never a guess
• Game Center leaderboards and cloud save, fully playable offline, in 9 languages

No timers, no pressure: think, plan, peel.

**Keywords** (100)
layers,stack,peel,strategy,logic,brain,chain,planning,stones,daily,offline,colors,mind,cascade

---

## Turkish

**Promotional Text**
Her taşın altında başka bir renk var. Katmanlı yığınları soy, sınırlı hamlede zincirleri planla, özel taşları doğru an için sakla.

**Description**
Stone Burst, alttakinin önemli olduğu bir bulmaca oyunu.

Tahtadaki her hücre 4 katmana kadar çıkan bir taş yığınıdır ve yalnız en üsttekini görürsün. Aynı renkte en az 3 bitişik taşa dokun, her birinden bir katman soyulsun. Sonrası tahtaya bağlı: yeni açılan taşlar 3'lü grup kurarsa kendiliğinden patlar, dalga dalga. Hamle sınırlı olduğu için asıl bulmaca hangi grubu önce soyacağını seçmek. Bir yığına uzun basarak gizli katmanlarını görebilirsin.

ONU FARKLI KILAN
• Katmanlı yığınlar: yalnız sonraki dokunuşu değil, iki üç katman ilerisini planla
• Zincir kuralı: yalnız yeni açılan taşlar zincir kurar, her patlama planlanabilir
• Özel taş tepsisi: büyük gruplar roket ve Altın Rulo doğurur; tepside bekler, hamle harcamadan istediğin an ateşlenir
• Düşünme biçimini değiştiren engeller: buz, renk kilidi, zarf, sünger, düğmeli geçit, döner taban, ataş ve kırılgan kristal
• Boya damlası: bir renk eksikse tek taşı boya
• Her gün kendi kuralı olan günlük bulmaca
• ZPE beceri endeksi: hamle verimi, ilk denemede başarı, çözüm hızı ve yardımsız bitirme tek puanda
• 3B taş koleksiyonu: yıldız kazan, döndürüp inceleyebileceğin taşların kilidini aç
• Her bölüm kayıtlı çözümüyle çözücüden geçer; ipucu tahmin değil, doğrulanmış hamle gösterir
• Game Center sıralaması ve bulut kaydı, internetsiz oynanır, 9 dilde

Süre yok, baskı yok: düşün, planla, soy.

**Keywords**
katman,yığın,soy,strateji,mantık,zeka,zincir,plan,taş,günlük,internetsiz,renk,akıl,hamle

---

## German

**Promotional Text**
Unter jedem Stein liegt eine andere Farbe. Trage Schichten ab, plane Kettenreaktionen mit wenigen Zügen und spare Spezialsteine für den richtigen Moment.

**Description**
Stone Burst ist ein Rätselspiel darüber, was darunter liegt.

Jedes Feld ist ein Stapel aus bis zu 4 Steinschichten, sichtbar ist nur die oberste. Tippe 3 oder mehr verbundene Steine derselben Farbe an, um von jedem eine Schicht abzutragen. Bilden die frisch aufgedeckten Steine eine Dreiergruppe, platzen sie von selbst, Welle für Welle. Da die Züge begrenzt sind, liegt das eigentliche Rätsel in der Reihenfolge. Halte einen Stapel gedrückt, um seine verborgenen Schichten zu sehen.

WAS ES ANDERS MACHT
• Geschichtete Stapel: plane zwei, drei Schichten voraus
• Kettenregel: nur frisch aufgedeckte Steine verketten sich, jede Kaskade ist planbar
• Spezialstein-Ablage: große Gruppen erzeugen Raketen und die Goldrolle; sie warten in der Ablage und zünden erst, wenn du willst, ohne Zug
• Hindernisse, die dein Denken verändern: Eis, Farbschlösser, Umschläge, Schwämme, Tore mit Schaltern, Drehsockel, Büroklammern und zerbrechliche Kristalle
• Farbtropfen: färbe einen einzelnen Stein um
• Tägliches Rätsel mit eigener Regel
• ZPE-Könnensindex: Zugeffizienz, Erfolge im ersten Versuch, Lösungstempo und Siege ohne Hilfe in einem Wert
• 3D-Steinsammlung: verdiene Sterne und schalte drehbare Sammelsteine frei
• Jedes Level wurde von einem Löser mit gespeicherter Lösung geprüft; Hinweise zeigen einen geprüften Zug
• Game-Center-Ranglisten und Cloud-Speicherung, offline spielbar, in 9 Sprachen

Kein Timer, kein Druck: denken, planen, abtragen.

**Keywords**
schichten,stapel,strategie,logik,denken,kette,planen,steine,täglich,offline,farben,kaskade,rätsel

---

## Spanish (Spain)

**Promotional Text**
Cada piedra esconde otro color. Quita capas de pilas, planea reacciones en cadena con pocos movimientos y guarda piedras especiales para el momento justo.

**Description**
Stone Burst es un puzle sobre lo que hay debajo.

Cada casilla es una pila de hasta 4 capas de piedra y solo ves la de arriba. Toca 3 o más piedras unidas del mismo color para quitar una capa de cada una. Si las piedras recién descubiertas forman un grupo de 3, estallan solas, ola tras ola. Con movimientos limitados, el verdadero puzle es elegir el orden. Mantén pulsada una pila para ver sus capas ocultas.

QUÉ LO HACE DIFERENTE
• Pilas por capas: planea dos o tres capas por delante
• Regla de cadena: solo las piedras recién descubiertas encadenan, cada cascada se puede planear
• Bandeja de piedras especiales: los grupos grandes crean cohetes y el Rollo Dorado; esperan en la bandeja y se usan cuando quieras, sin gastar movimiento
• Obstáculos que cambian tu forma de pensar: hielo, candados de color, sobres, esponjas, puertas con botones, bases giratorias, clips y cristales frágiles
• Gotas de pintura: cambia el color de una sola piedra
• Puzle diario con su propia regla
• Índice de habilidad ZPE: eficiencia, victorias al primer intento, velocidad y partidas sin ayuda en una sola puntuación
• Colección de piedras 3D: gana estrellas y desbloquea piedras que puedes girar
• Cada nivel está comprobado por un solucionador con solución guardada; las pistas muestran un movimiento verificado
• Clasificaciones de Game Center y guardado en la nube, sin conexión, en 9 idiomas

Sin tiempo, sin presión: piensa, planea, pela.

**Keywords**
capas,pila,estrategia,lógica,mente,cadena,planear,piedras,diario,sin conexión,colores,cascada

---

## Arabic

**Promotional Text**
تحت كل حجر لون آخر. انزع طبقات الأكوام، وخطّط لسلاسل الانفجار بحركات محدودة، واحتفظ بالأحجار الخاصة للحظة المناسبة.

**Description**
Stone Burst لعبة ألغاز عمّا يختبئ في الأسفل.

كل خانة على اللوحة كومة من الأحجار تصل إلى أربع طبقات، ولا ترى إلا الطبقة العليا. المس 3 أحجار متجاورة أو أكثر من اللون نفسه لتنزع طبقة من كل منها. إذا شكّلت الأحجار المكشوفة حديثا مجموعة من 3، تنفجر وحدها موجة بعد موجة. ولأن الحركات محدودة، فاللغز الحقيقي هو اختيار الترتيب. اضغط مطولا على أي كومة لترى طبقاتها المخفية.

ما الذي يميزها
• أكوام من طبقات: خطّط لطبقتين أو ثلاث مقدما
• قاعدة السلسلة: الأحجار المكشوفة حديثا فقط تصنع سلسلة، فيمكن التخطيط لكل انفجار
• صينية الأحجار الخاصة: المجموعات الكبيرة تصنع صواريخ واللفافة الذهبية، تنتظر في الصينية وتُطلق متى شئت دون استهلاك حركة
• عقبات تغيّر طريقة تفكيرك: جليد، أقفال ألوان، أظرف، إسفنج، بوابات بأزرار، قواعد دوّارة، مشابك وكريستال هش
• قطرات الطلاء: غيّر لون حجر واحد
• لغز يومي بقاعدة خاصة كل يوم
• مؤشر المهارة ZPE: كفاءة الحركات والفوز من المحاولة الأولى وسرعة الحل والإنهاء دون مساعدة في نتيجة واحدة
• مجموعة أحجار ثلاثية الأبعاد: اربح النجوم وافتح أحجارا يمكنك تدويرها
• كل مرحلة يتحقق منها حلّال بحل محفوظ، والتلميح يعرض حركة مؤكدة
• لوائح Game Center والحفظ السحابي، تعمل دون إنترنت، بـ9 لغات

بلا مؤقت وبلا ضغط: فكّر، خطّط، انزع.

**Keywords**
طبقات,كومة,استراتيجية,منطق,ذكاء,سلسلة,تخطيط,أحجار,يومي,دون إنترنت,ألوان

---

## Japanese

**Promotional Text**
石の下には別の色。重なったレイヤーをはがし、限られた手数で連鎖を計画し、特別な石は最高のタイミングまで取っておこう。

**Description**
Stone Burst は「下に何があるか」を考えるパズルゲームです。

盤面の各マスは最大4層の石の山で、見えるのは一番上だけ。同じ色の石を3つ以上つなげてタップすると、それぞれ1層ずつはがれます。新しく出てきた石が3つそろうと自動で弾け、連鎖が続きます。手数は限られているので、どの順番ではがすかが本当のパズル。山を長押しすると隠れた層を確認できます。

ここがちがう
• 重なった石の山：次の一手だけでなく2〜3層先まで計画
• 連鎖ルール：新しく出た石だけが連鎖するので、連鎖を計画できる
• 特別な石のトレイ：大きなグループでロケットやゴールドロールが生まれ、好きなときに手数を使わず発動
• 考え方が変わるギミック：氷、カラーロック、封筒、スポンジ、ボタン付きゲート、回転台、クリップ、こわれやすいクリスタル
• ペイント：1つの石の色を変える
• 毎日ルールが変わるデイリーパズル
• ZPEスキル指数：手数効率、初回クリア、解く速さ、ヒントなしクリアをひとつのスコアに
• 3Dの石コレクション：星を集めて、回して眺められる石をアンロック
• 全ステージをソルバーで検証済み。ヒントは推測ではなく検証済みの一手
• Game Centerランキングとクラウド保存、オフラインでプレイ可能、9言語対応

時間制限なし、プレッシャーなし。考えて、計画して、はがそう。

**Keywords**
レイヤー,重ねる,戦略,ロジック,脳トレ,連鎖,計画,石,デイリー,オフライン,色,パズル
