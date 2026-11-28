using System;
using System.Collections.Generic;
using System.IO;
using KatKat.Game;
using KatKat.Levels;
using KatKat.UI;
using KatKat.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    // ---- Reklam videosu v2 (2026-11-28; kullanıcı: "oyunumuzun son haliyle reklam videosu oluşturalım … anasayfada yolculuk
    // haritasından karakterimiz bölüm bölüm zıplayarak sulu bölümlerden birine konacak ve oyunun içerisine girecek. sulu bölümde
    // suyun kovaya akışını kovanın hedefe doluşunu gösterecek. Bölüm tamamlanacak. maskot torbalı bir bölüme gidecek … Video
    // içerisinde el olarak masaüstü 'Yukarı İşaret Eden 3D El İmleci.png' yi kullanalım. arapça ve türkçe"; biçim: 9:16 tam ekran).
    // Kareler: tools/unity.ps1 trailer-promo (KATKAT_TRAILER_LANGS=tr,ar; KATKAT_TRAILER_ONLY=p1_su,p2_torba).
    // Çıktı Logs/promo2_<dil>/<sahne>/ (f_*.png 30 fps, sfx.wav, meta.txt). Kurgu (kapak, pencere açılışı, kapanış): tools/trailer-promo.ps1.
    public static partial class TrailerCapture
    {
        /// <summary>El olarak kullanıcının 3D el görseli (docs/varliklar/gorseller/reklam/el-3d.png).</summary>
        private static bool UseHand3D;
        /// <summary>3D elin parmak ucu (görselde ~ 440, 190 / 1254).</summary>
        private static readonly Vector2 Hand3DTip = new Vector2(0.351f, 0.85f);
        private static Sprite _hand3d;

        /// <summary>
        /// 3D el: arka plan temizliğinden kalan kenar lekeleri (sarı/kırmızı serpintiler, sarı kenar şeridi) ayıklanır: yalnız en büyük
        /// dolu parça ve çevresi kalır; yarı saydam kenar pikselleri komşu dolu piksellerin rengini alır.
        /// </summary>
        private static Sprite Hand3DSprite()
        {
            if (_hand3d) return _hand3d;
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "docs", "varliklar", "gorseller", "reklam", "el-3d.png");
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(path));
            int w = src.width, h = src.height;
            var px = src.GetPixels32();
            var solid = new bool[w * h];
            for (int i = 0; i < px.Length; i++) solid[i] = px[i].a > 128;
            // En büyük dolu parça (4-komşulu).
            var comp = new int[w * h];
            int best = 0, bestSize = 0, id = 0;
            var stack = new Stack<int>();
            for (int i = 0; i < px.Length; i++)
            {
                if (!solid[i] || comp[i] != 0) continue;
                id++; int size = 0;
                stack.Push(i); comp[i] = id;
                while (stack.Count > 0)
                {
                    int p = stack.Pop(); size++;
                    int x = p % w, y = p / w;
                    void Try(int q) { if (solid[q] && comp[q] == 0) { comp[q] = id; stack.Push(q); } }
                    if (x > 0) Try(p - 1); if (x < w - 1) Try(p + 1); if (y > 0) Try(p - w); if (y < h - 1) Try(p + w);
                }
                if (size > bestSize) { bestSize = size; best = id; }
            }
            // Ana parçanın 6 px çevresi (kenar yumuşaklığı) kalır; geri kalan her şey silinir.
            var keep = new bool[w * h];
            const int R = 6;
            for (int i = 0; i < px.Length; i++)
            {
                if (comp[i] != best) continue;
                int x = i % w, y = i / w;
                bool edge = (x > 0 && comp[i - 1] != best) || (x < w - 1 && comp[i + 1] != best) || (y > 0 && comp[i - w] != best) || (y < h - 1 && comp[i + w] != best);
                keep[i] = true;
                if (!edge) continue;
                for (int dy = -R; dy <= R; dy++)
                for (int dx = -R; dx <= R; dx++)
                {
                    int xx = x + dx, yy = y + dy;
                    if (xx >= 0 && yy >= 0 && xx < w && yy < h && dx * dx + dy * dy <= R * R) keep[yy * w + xx] = true;
                }
            }
            var outPx = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                if (!keep[i]) { outPx[i] = new Color32(0, 0, 0, 0); continue; }
                var c = px[i];
                if (c.a < 250 && comp[i] != best || c.a < 250)
                {
                    // Kenar: rengi en yakın ana parça piksellerinin ortalamasından (sarı/kırmızı saçak kalkar), saydamlık korunur.
                    int r = 0, g = 0, b = 0, n = 0;
                    int x = i % w, y = i / w;
                    for (int rad = 2; rad <= 10 && n == 0; rad += 4)
                        for (int dy = -rad; dy <= rad; dy++)
                        for (int dx = -rad; dx <= rad; dx++)
                        {
                            int xx = x + dx, yy = y + dy;
                            if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                            int q = yy * w + xx;
                            if (comp[q] != best || px[q].a < 250) continue;
                            r += px[q].r; g += px[q].g; b += px[q].b; n++;
                        }
                    if (n > 0) c = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), c.a);
                }
                outPx[i] = c;
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "trailer_hand3d", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(outPx);
            tex.Apply(true);
            UnityEngine.Object.DestroyImmediate(src);
            return _hand3d = Sprite.Create(tex, new Rect(0, 0, w, h), Hand3DTip, 100f);
        }

        [MenuItem("Kat Kat/Yakalama/Reklam videosu v2 kareleri")]
        public static void RunPromo2()
        {
            var langs = (Environment.GetEnvironmentVariable("KATKAT_TRAILER_LANGS") ?? "tr,ar").Split(',');
            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            Fps = 30; _dense = true; UseHand3D = true;
            try { foreach (var l in langs) RunPromo2Lang(l.Trim(), Path.Combine(logs, "promo2_" + l.Trim())); }
            finally { Fps = 30; _dense = true; UseHand3D = false; CompletedForScene = 240; BgMotion.ClockOverride = null; }
        }

        private static void RunPromo2Lang(string lang, string root)
        {
            _root = root;
            var only = Environment.GetEnvironmentVariable("KATKAT_TRAILER_ONLY");
            var pick = string.IsNullOrEmpty(only) ? null : new HashSet<string>(only.Split(','));
            if (pick == null && Directory.Exists(_root)) Directory.Delete(_root, true);
            Directory.CreateDirectory(_root);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set(lang, false);
            GameController.ToursOverride = false;
            try
            {
                void Scene(string n, int completed, Action<GameController, Action> b)
                {
                    if (pick != null && !pick.Contains(n)) return;
                    CompletedForScene = completed;
                    TrailerCapture.Scene(n, (gc, rec) =>
                    {
                        float clock = 0f; _marks.Clear(); _promoFrame = 0;
                        Action rec2 = () =>
                        {
                            clock += Dt / 1000f;
                            BgMotion.ClockOverride = clock;
                            var bd = gc.transform.Find("Backdrop");
                            var bm = bd != null ? bd.GetComponent<BgMotion>() : null;
                            if (bm != null) bm.Tick(clock);
                            rec(); _promoFrame++;
                        };
                        // Mağaza derlemesinde olmayan girişler haritada görünmesin.
                        gc.Menu.LivePressed = null; gc.Menu.PilotPressed = null; gc.Menu.ConceptPressed = null;
                        b(gc, rec2);
                        if (_marks.Count > 0) File.WriteAllLines(Path.Combine(_root, n, "marks.txt"), _marks);
                    });
                }
                // Sahneler kısa parçalar (her biri ayrı Unity çalıştırmasında; uzun çekimde GPU aygıtı düşüyordu).
                // 1) Ana sayfa haritası: maskot kendi yerinde (19) normal hâliyle; el düğüme ve OYNA'ya dokunur, reklam bölümü açılır.
                // Kullanıcı (2026-11-28): kurgu için eklenen bölüm bölüm zıplama olmasın, maskot normal hâliyle görünsün.
                Scene("p1_harita", 18, (gc, rec) => { gc.ShowLevels(); MapHold(gc, rec, 1.8f); EnterLevel(gc, rec, 18, () => gc.StartCustom(AdLevel(), 18)); });
                // 5) Reklam bölümü (asıl oynanış): düşünen el, 2'li gruba dokunuş (uyarı), küçük grupları soyma, dev grup, Altın Rulo.
                Scene("p5_reklam", 18, (gc, rec) => { StartCustomDirect(gc, AdLevel(), 18); PlayAd(gc, rec); });
                // 2) Su kovası bölümü (gerçek hız): su kovaya akar, Gönder ile görev çubuğu dolar.
                Scene("p2_su", 18, (gc, rec) => { StartDirect(gc, 18); PlayWhole(gc, rec, 1.0f); });
                // 6) Kullanıcı (2026-11-28): "kova bölümüne geçerken haritadan bölümü parmak seçsin" → harita, el düğüme ve OYNA'ya
                // dokunur, kural kartında Başla, su kovası bölümü gerçek hızda (kurgu iki gönderimden sonra keser).
                Scene("p6_harita_su", 18, (gc, rec) => { gc.ShowLevels(); MapHold(gc, rec, 0.5f); EnterLevel(gc, rec, 18); Mark("su"); PlayWhole(gc, rec, 1.0f); });
                // 3) Harita: maskot kendi yerinde (23, torba).
                Scene("p3_harita", 22, (gc, rec) => { gc.ShowLevels(); MapHold(gc, rec, 1.4f); EnterLevel(gc, rec, 22); });
                // 4) Torbalı bölüm.
                Scene("p4_torba", 22, (gc, rec) => { StartDirect(gc, 22); PlayWhole(gc, rec, 1.5f); });
            }
            finally
            {
                GameController.ToursOverride = null;
                Loc.Set(keepLang, false);
                SafeAreaFitter.NormalizedOverride = null;
                BgMotion.ClockOverride = null;
            }
            Debug.Log("[KatKat] Reklam v2 kareleri tamam → " + _root);
        }

        /// <summary>Harita: maskot kendi düğümünde, oyundaki normal hâliyle (menünün kendi saatiyle) bekler.</summary>
        private static void MapHold(GameController gc, Action rec, float holdSec)
        {
            for (int f = 0; f < Fps * holdSec; f++) { gc.Menu.Tick(Dt / 1000f); rec(); }
        }

        /// <summary>El düğüme dokunur (başlangıç kartı), sonra OYNA'ya; bölüm açılır, kural kartında Başla'ya basılır.</summary>
        private static void EnterLevel(GameController gc, Action rec, int index, Action startOverride = null)
        {
            Action tickMenu = () => gc.Menu.Tick(Dt / 1000f);
            _hand.Enter(new Vector2(W * 0.85f, 80f));
            var node = gc.Menu.NodeForTest(index);
            if (node != null)
                TapAt(UiPx(node), tickMenu, rec, () => { var b = node.GetComponentInChildren<UnityEngine.UI.Button>(); b?.onClick.Invoke(); });
            for (int f = 0; f < Fps * 0.7f; f++) { tickMenu(); rec(); }
            var card = FindDeep(gc.Menu.transform, "StartCard");
            var play = card != null ? FindDeep(card, "Play") : null;
            Action press = startOverride != null
                ? () => { startOverride(); gc.Hud.HideToast(); }
                : (Action)(() => play.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke());
            if (play != null) TapAt(UiPx(play), tickMenu, rec, press);
            else if (startOverride != null) startOverride();
            else gc.StartLevel(index);
            _hand.Leave();
            Action tick = () => TickPromo(gc, Dt);
            for (int f = 0; f < Fps * 1.4f; f++) { tick(); rec(); }
            if (gc.Hud.RuleCardOpen)
            {
                var start = FindDeep(gc.Hud.transform, "Start");
                _hand.Enter(new Vector2(W * 0.85f, 80f));
                if (start != null) TapAt(UiPx(start), tick, rec, () => start.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke());
                else gc.Hud.HideRuleCard(false);
                for (int f = 0; f < Fps * 0.5f; f++) { tick(); rec(); }
            }
        }

        private static readonly System.Reflection.MethodInfo[] BoardLateTicks = {
            typeof(BoardView).GetMethod("TickFall", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
            typeof(BoardView).GetMethod("TickDrops", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
            typeof(BoardView).GetMethod("TickWater", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
            typeof(BoardView).GetMethod("TickHeartbeat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
        };
        private static readonly System.Reflection.MethodInfo TimedTickM = typeof(GameController).GetMethod("TimedTick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        /// <summary>Oyun ekranı saati: tahta, yönetmen, HUD (çipler, efektler, kova/torba, su çubuğu, alt düğmeler, kart) ve süre.</summary>
        private static void TickPromo(GameController gc, float ms)
        {
            TickAll(gc, ms);
            gc.Hud.AdvanceBucket(ms / 1000f);
            gc.Hud.AdvanceWaterBar(ms / 1000f);
            gc.Hud.AdvanceButtons(ms);
            gc.Hud.AdvancePanel(ms);
            TimedTickM?.Invoke(gc, new object[] { ms / 1000f });
            BucketSpark.StepAll(ms / 1000f); // torba/görev çipi kıvılcımları (Update oynatma dışında çalışmaz)
            // Tahtanın LateUpdate işleri (yukarıdan düşen taşlar, damlalar, kovadaki su, süre nabzı) toplu modda çalışmaz: kare saatiyle.
            foreach (var m in BoardLateTicks) m?.Invoke(gc.Board, new object[] { ms / 1000f });
        }

        /// <summary>Kayıtlı çözümün tamamı elle oynanır: dokunuş, tepsi, kova/torba gönderimi; bölüm sonu kartı da görünür.</summary>
        /// <summary>Bölümü haritasız açar; kural kartı ve ipucu yazıları kapalı, açılış dökülmesi oturur.</summary>
        private static void StartDirect(GameController gc, int index)
        {
            gc.StartLevel(index);
            gc.Hud.HideRuleCard(false);
            gc.Hud.HideToast();
            for (float t = 0; t < 600f; t += Dt) TickPromo(gc, Dt);
            gc.Hud.HideToast();
        }

        /// <summary>Reklam bölümü (docs/reklam_bolum/level_reklam.json; kaynak reklam_bolum.txt, LevelLab adshow ile derlenir).</summary>
        private static LevelData AdLevel()
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "docs", "reklam_bolum", "level_reklam.json");
            return LevelParser.Parse(File.ReadAllText(path), "level_reklam.json");
        }

        private static void StartCustomDirect(GameController gc, LevelData level, int index)
        {
            gc.StartCustom(level, index);
            gc.Hud.HideRuleCard(false);
            gc.Hud.HideToast();
            for (float t = 0; t < 600f; t += Dt) TickPromo(gc, Dt);
            gc.Hud.HideToast();
        }

        /// <summary>Kurgu için an işaretleri (kare numarası) → sahne klasöründe marks.txt.</summary>
        private static readonly List<string> _marks = new List<string>();
        private static int _promoFrame;
        private static void Mark(string key) => _marks.Add(key + "=" + _promoFrame);

        /// <summary>El dokunmadan hedefe süzülür (yumuşak yay).</summary>
        private static void Glide(Vector2 target, float sec, Action tick, Action rec)
        {
            var from = _hand.Pos;
            int n = Mathf.Max(1, Mathf.RoundToInt(Fps * sec));
            var mid = (from + target) * 0.5f + new Vector2(0f, Mathf.Min(90f, Vector2.Distance(from, target) * 0.12f));
            for (int f = 1; f <= n; f++)
            {
                float t = Ease(f / (float)n);
                _hand.Pos = Vector2.Lerp(Vector2.Lerp(from, mid, t), Vector2.Lerp(mid, target, t), t);
                tick(); rec();
            }
        }

        /// <summary>Kararsız bekleyiş: parmak hedefin üstünde hafifçe gezinir (dokunmaz).</summary>
        private static void Ponder(float sec, Action tick, Action rec)
        {
            var c = _hand.Pos;
            int n = Mathf.RoundToInt(Fps * sec);
            for (int f = 0; f < n; f++)
            {
                float t = f / (float)Fps;
                float fade = Mathf.Min(1f, Mathf.Min(f, n - f) / (Fps * 0.15f));
                _hand.Pos = c + new Vector2(Mathf.Sin(t * 2.3f) * 18f, Mathf.Sin(t * 1.7f + 1f) * 11f) * fade; // yavaş, kararsız gezinme
                tick(); rec();
            }
            _hand.Pos = c;
        }

        private static void Hold(float sec, Action tick, Action rec) { for (int f = 0; f < Fps * sec; f++) { tick(); rec(); } }

        private static void WaitIdle(GameController gc, Action tick, Action rec)
        {
            int guard = 0;
            while ((gc.CurrentPhase == GameController.Phase.Animating || gc.Board.IsPlaying || gc.Hud.BucketBusy) && guard++ < Fps * 8) { tick(); rec(); }
        }

        /// <summary>Hücre üstüne parmağın bastığı nokta (taşın üst yüzü, biraz yukarı).</summary>
        private static Vector2 CellTip(GameController gc, int x, int y) => CellPx(gc, x, y) + new Vector2(0f, 6f);

        /// <summary>Reddedilen dokunuşta küçük grubun taşları sallanır (oyunda BoardView.Update; oynatma dışında kare saatiyle).</summary>
        private static void ShakeStacks(GameController gc, (int x, int y)[] cells, Action tick, Action rec)
        {
            int n = Mathf.RoundToInt(Fps * 0.36f);
            for (int f = 0; f <= n; f++)
            {
                float x = f / (float)n;
                float a = Mathf.Sin(x * Mathf.PI * 4f) * (1f - x) * 9f;
                foreach (var c in cells) gc.Board.StackAt(c.x, c.y).transform.localRotation = Quaternion.Euler(0f, 0f, f == n ? 0f : a);
                tick(); rec();
            }
        }

        /// <summary>
        /// Reklam oynanışı (2026-11-28; kullanıcı: "Büyük patlatma gruplarının olmadığı. Parmağın düşünüyormuş gibi başka gruba gidip
        /// karar veremeden döndüğü, 2 li tekli gruba tıkladığı ve uyarı sesi çıktığı, üst katmanda küçük grupları soyduğumuz ve büyük
        /// gruba ulaştığımız"). Bölüm bitmez; son Altın Rulo ile kapanır.
        /// </summary>
        private static void PlayAd(GameController gc, Action rec)
        {
            Action tick = () => TickPromo(gc, Dt);
            gc.Board.Director.TimeScale = 1f;
            Action<int, int> tap = (x, y) => gc.PlayStep(new StepData { Type = "tap", X = x, Y = y });
            // Kullanıcı (2026-11-28): "Düşünme ve yavaş hareket etme güzel fakat çok fazla yavaş … İlk anda 2li gruba tıklasın
            // patlamayınca düşünsün ve doğru hamleleri yapıp devam etsin ama normal akışta ne hızlı ne yavaş" → el mesafeye göre
            // 0,45–0,8 sn süzülür; düşünme yalnız ilk yanlış dokunuştan sonra belirgin, sonra kısa duraksamalar.
            Action<Vector2, float> go = (to, ponder) =>
            {
                float sec = Mathf.Clamp(Vector2.Distance(_hand.Pos, to) / 650f, 0.45f, 0.8f);
                Glide(to, sec, tick, rec);
                if (ponder > 0f) Ponder(ponder, tick, rec);
            };
            Action<Vector2, Action> press = (to, act) => { go(to, 0f); Hold(0.12f, tick, rec); TapAt(to, tick, rec, act); };
            _hand.Enter(new Vector2(W * 0.9f, H * 0.3f));
            Hold(0.25f, tick, rec);
            // İlk anda 2'li mercana basar: patlamaz, uyarı sesi ve "En az 3" yazısı.
            Mark("kucuk");
            press(CellTip(gc, 5, 3), () => tap(5, 3));
            ShakeStacks(gc, new[] { (5, 3), (6, 3) }, tick, rec);
            // Düşünür: soldaki mercan üçlüsü, ortadaki inci üçlüsü.
            Mark("dusun");
            go(CellTip(gc, 2, 4), 0.7f);
            go(CellTip(gc, 4, 4) + new Vector2(20f, -15f), 0.6f);
            // Soyma 1: inci → altında 6'lık mercan.
            Mark("soy");
            press(CellTip(gc, 4, 4), () => tap(4, 4));
            WaitIdle(gc, tick, rec); Hold(0.3f, tick, rec);
            // Soyma 2: turkuaz üçlü → mercan 11'e birleşir.
            go(CellTip(gc, 3, 3), 0.4f);
            press(CellTip(gc, 4, 2), () => tap(4, 2));
            WaitIdle(gc, tick, rec); Hold(0.4f, tick, rec);
            // Dev mercan grubu (11): patlar, Altın Rulo tepsiye.
            Mark("buyuk");
            press(CellTip(gc, 3, 3), () => tap(3, 3));
            WaitIdle(gc, tick, rec); Hold(0.5f, tick, rec);
            // Altın Rulo: yuvaya, sonra ortaya çıkan turkuaz alana.
            Mark("rulo");
            var slot = FindDeep(gc.Hud.transform, "Slot0");
            if (slot != null) press(UiPx(slot), () => gc.PressTraySlot(0));
            Hold(0.25f, tick, rec);
            press(CellTip(gc, 4, 4), () => gc.PlayStep(new StepData { Type = "use", Row = 0, X = 4, Y = 4 }));
            Mark("rulo_at");
            _hand.Leave();
            WaitIdle(gc, tick, rec);
            Hold(1.2f, tick, rec);
            Mark("son");
        }

        private static void PlayWhole(GameController gc, Action rec, float speed)
        {
            Action tick = () => TickPromo(gc, Dt * speed);
            var sol = gc.CurrentLevel.Solution;
            gc.Board.Director.TimeScale = speed;
            _hand.Enter(new Vector2(W + 80f, 300f));
            foreach (var step in sol)
            {
                if (gc.IsCelebrating || gc.Hud.PanelOpen) break;
                Vector2 at;
                if (step.Type == "send") Mark("gonder");
                if (step.Type == "send")
                {
                    var btn = gc.WaterLevel ? FindDeep(gc.Hud.transform, "WaterSend") : FindDeep(gc.Hud.transform, "SendTag");
                    at = btn != null ? UiPx(btn) : new Vector2(W * 0.5f, H * 0.2f);
                }
                else if (step.Type == "use")
                {
                    var slot = FindDeep(gc.Hud.transform, "Slot" + step.Row);
                    if (slot != null) TapAt(UiPx(slot), tick, rec, null);
                    at = CellPx(gc, step.X, step.Y);
                }
                else at = CellPx(gc, step.X, step.Y);
                var s = step;
                TapAt(at, tick, rec, () => gc.PlayStep(s));
                var away = at + new Vector2(150f, -220f);
                int guard = 0;
                while ((gc.CurrentPhase == GameController.Phase.Animating || gc.Board.IsPlaying || gc.Hud.BucketBusy) && guard++ < Fps * 8)
                {
                    _hand.Pos = Vector2.Lerp(_hand.Pos, away, PerFrame(0.15f));
                    tick(); rec();
                }
                for (int f = 0; f < Fps * 0.12f; f++) { tick(); rec(); }
            }
            _hand.Leave();
            int g2 = 0;
            while (!gc.Hud.PanelOpen && g2++ < Fps * 8) { tick(); rec(); }
            for (int f = 0; f < Fps * 2.2f; f++) { tick(); rec(); }
        }
    }
}
