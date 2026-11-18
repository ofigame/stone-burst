using System;
using System.IO;
using System.Linq;
using KatKat.Game;
using KatKat.UI;
using KatKat.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Yeni arka plan denemesi (kullanıcı 2026-11-18: docs/yeni_v1/arkaplan "bu arkaplanları dener misin 6 tane deneme oluştur
    /// göreyim … Oyun tepsisinin renkleri yine arkaplana göre ayarla"). Her görsel geçici olarak VR2 dokusu yapılır; tema paleti
    /// (tepsi kenarı, zemin, hedef yuvası) görselin oyun alanı bölgesinden türetilir. Oyun dosyaları değişmez.
    /// Çıktı Logs/captures/arkaplan_deneme/ (tools/unity.ps1 capture-bgtrial); paletler günlüğe ve palet.txt'ye yazılır.
    /// </summary>
    public static class CaptureBgTrial
    {
        public const string SourceDir = "docs/yeni_v1/arkaplan";

        public static void RunBatch()
        {
            int code = 0;
            try { Run(); }
            catch (Exception e) { Debug.LogError("[KatKat] arka plan denemesi hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        /// <summary>
        /// Oyunun kendisiyle kısa video (KATKAT_GAMEVIDEO_LEVEL=N): gerçek tema ve arka plan hareketi, her 2 sn'de kayıtlı çözümün
        /// bir adımı (tepsi ışıltısı grup boyutuna göre). 10 sn, 30 kare/sn → Logs/captures/oyun_video/kare_NNNN.jpg.
        /// </summary>
        private static void GameVideo(int level)
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "oyun_video"));
            int k = 0;
            GameCapture.FramesPublic("oyun_video/kare", 720, 1560, gc => gc.StartLevel(level - 1), 300, (gc, f) =>
            {
                BgMotion.ClockOverride = f / 30f;
                var bm = gc.transform.Find("Backdrop").GetComponent<BgMotion>();
                if (bm != null) bm.Tick(f / 30f);
                // KATKAT_GAMEVIDEO_TIERS=1: çözüm yerine üç kademe ışıltı sırayla (3'lü mor, 5'li mercan, 6+ turkuaz).
                if (Environment.GetEnvironmentVariable("KATKAT_GAMEVIDEO_TIERS") == "1")
                {
                    int tier = f == 20 ? 1 : f == 110 ? 2 : f == 200 ? 3 : 0;
                    if (tier > 0)
                    {
                        var w = new KatKat.Core.PeelWave { Kind = KatKat.Core.WaveKind.Tap };
                        var col = tier == 1 ? KatKat.Core.LayerColor.Purple : tier == 2 ? KatKat.Core.LayerColor.Coral : KatKat.Core.LayerColor.Blue;
                        for (int i = 0; i < 3 + tier * 2; i++) w.Cells.Add(new KatKat.Core.CellPeel(0, 0, col, 0, default));
                        gc.Board.TrayBurst(w, tier);
                        gc.Board.ShakeBoard(0.012f * tier, 260f);
                        Debug.Log($"[KatKat] video kademe {tier} kare {f}");
                    }
                    gc.Board.Advance(1000f / 30f);
                    return;
                }
                // KATKAT_GAMEVIDEO_SPECIALS=1 (2026-11-18, tempo + yıldırım denemesi): tepsiden Altın Rulo, sonra roket,
                // sonra kayıtlı çözümün ilk adımı.
                if (Environment.GetEnvironmentVariable("KATKAT_GAMEVIDEO_SPECIALS") == "1")
                {
                    if (f == 1) gc.Hud.HideRuleCard(true); // kural kartı tahtayı kapatmasın
                    if ((f == 15 || f == 125) && gc.CurrentPhase == GameController.Phase.Idle)
                    {
                        var st = gc.Session.State;
                        st.Tray.Clear();
                        st.Tray.Add(f == 15 ? KatKat.Core.SpecialKind.Rainbow : KatKat.Core.SpecialKind.RocketRow);
                        gc.Hud.SetTrayVisible(true);
                        gc.Hud.SetTray(st.Tray, -1);
                        var b = st.Board;
                        int cx = b.Width / 2, cy = b.Height / 2 + (f == 15 ? 0 : -1);
                        for (int r = 0; r < b.Width * b.Height && !b[cx, cy].IsSelectable; r++)
                        {
                            cx = (cx + 1) % b.Width;
                            if (cx == 0) cy = (cy + 1) % b.Height;
                        }
                        gc.PlayStep(new KatKat.Levels.StepData { Type = "use", Row = 0, X = cx, Y = cy });
                        Debug.Log($"[KatKat] video özel taş kare {f}");
                    }
                    else if (f == 225 && gc.CurrentPhase == GameController.Phase.Idle && gc.CurrentLevel.Solution.Count > 0)
                        gc.PlayStep(gc.CurrentLevel.Solution[0]);
                    gc.Board.Advance(1000f / 30f);
                    gc.Board.Director.Advance(1000f / 30f);
                    return;
                }
                var sol = gc.CurrentLevel.Solution;
                if (f % 60 == 20 && k < sol.Count && gc.CurrentPhase == GameController.Phase.Idle)
                {
                    Debug.Log($"[KatKat] video adım {k} kare {f}");
                    gc.PlayStep(sol[k++]);
                }
                gc.Board.Advance(1000f / 30f);
                gc.Board.Director.Advance(1000f / 30f);
            });
            BgMotion.ClockOverride = null;
        }

        public static void Run()
        {
            // KATKAT_WIDE_SHOT=1 (2026-11-18, üst panel geniş ekranda tam genişlik mi): iPad ve katlanabilir oranda oyun ekranı.
            if (Environment.GetEnvironmentVariable("KATKAT_WIDE_SHOT") == "1")
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "genis"));
                foreach (var (name, w, h) in new[] { ("ipad", 2064, 2752), ("katlanabilir", 1800, 2000), ("telefon", 1080, 2340) })
                    GameCapture.ShotPublic("genis/" + name + ".png", w, h, gc => { gc.StartLevel(37); gc.Hud.HideRuleCard(true); });
                return;
            }
            var gv = Environment.GetEnvironmentVariable("KATKAT_GAMEVIDEO_LEVEL");
            if (!string.IsNullOrEmpty(gv)) { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); GameVideo(int.Parse(gv)); return; }
            string outDir = Path.Combine(PeelCapture.OutDir, "arkaplan_deneme");
            Directory.CreateDirectory(outDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var files = Directory.GetFiles(SourceDir, "*.png").OrderBy(f => f, StringComparer.Ordinal).Take(Vr2.Skins.Length).ToArray();
            var env = Environment.GetEnvironmentVariable("KATKAT_BGTRIAL_LEVEL");
            int level = string.IsNullOrEmpty(env) ? 21 : int.Parse(env);
            var saved = (Vr2.Skin[])Vr2.Skins.Clone();
            bool motion = Environment.GetEnvironmentVariable("KATKAT_BGTRIAL_MOTION") == "1";
            bool burst = Environment.GetEnvironmentVariable("KATKAT_BGTRIAL_BURST") == "1";
            var onlyEnv = Environment.GetEnvironmentVariable("KATKAT_BGTRIAL_ONLY");
            int only = string.IsNullOrEmpty(onlyEnv) ? 1 : int.Parse(onlyEnv);
            bool savedBlur = Vr2.BlurBackground;
            // Kullanıcı 2026-11-18: "bulanıklığı kaldır öyle göreyim" → KATKAT_BGTRIAL_BLUR=1 değilse keskin doku.
            Vr2.BlurBackground = Environment.GetEnvironmentVariable("KATKAT_BGTRIAL_BLUR") == "1";
            var report = new System.Text.StringBuilder();
            // KATKAT_BGTRIAL_FRAMES=1 (kullanıcı 2026-11-18: docs/yeni_v1/tahtalar "bu tahtaları dener misin"): 6 çerçeve, her biri
            // uyumlu yeni arka planla; tepsi gizlenir, çerçeve tahtanın taş alanını iç bölümüyle sarar.
            Frame[] frames = null;
            if (Environment.GetEnvironmentVariable("KATKAT_BGTRIAL_FRAMES") == "1")
            {
                frames = LoadFrames(Directory.GetFiles(FrameDir, "*.png")[0]);
                Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "tahta_deneme"));
            }
            try
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = Path.GetFileNameWithoutExtension(files[i]) };
                    tex.LoadImage(File.ReadAllBytes(files[i]));
                    tex.wrapMode = TextureWrapMode.Clamp;
                    var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    sprite.name = tex.name;
                    var pal = PaletteFrom(tex);
                    Vr2.Skins[i] = new Vr2.Skin(saved[i].Id, tex.name, Hex(pal.hi), Hex(pal.b), Hex(pal.sh), Hex(pal.floor));
                    Vr2.PreviewBackground = sprite;
                    GameController.Vr2SkinOverride = i;
                    string line = $"{i + 1}. {tex.name}: highlight #{Hex(pal.hi):X6}, base #{Hex(pal.b):X6}, shadow #{Hex(pal.sh):X6}, floor #{Hex(pal.floor):X6}";
                    report.AppendLine(line);
                    Debug.Log("[KatKat] " + line);
                    if (frames != null)
                    {
                        for (int j = 0; j < frames.Length; j++)
                        {
                            if (FrameBg[j] != i) continue;
                            var fr = frames[j];
                            GameCapture.ShotPublic($"tahta_deneme/tahta_{j + 1}.png", 1080, 2340, gc => { gc.StartLevel(level - 1); ApplyFrame(gc, fr); });
                        }
                        continue;
                    }
                    if (motion)
                    {
                        // Kullanıcı 2026-11-18: arka plana hareket ("önerini yap") → 8 sn, 30 kare/sn kare dizisi (ffmpeg ile mp4).
                        if (i + 1 != only) continue;
                        var srcTex = tex;
                        BgMotion bm = null;
                        Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "arkaplan_hareket"));
                        GameCapture.FramesPublic($"arkaplan_hareket/deneme_{i + 1}", 720, 1560, gc =>
                        {
                            gc.StartLevel(level - 1);
                            var sr = gc.transform.Find("Backdrop").GetComponent<SpriteRenderer>();
                            BgMotion.ClockOverride = 0f;
                            bm = BgMotion.Attach(sr, srcTex, BgMotion.Garden);
                        }, 240, (gc, f) =>
                        {
                            BgMotion.ClockOverride = f / 30f; bm.Tick(f / 30f);
                            // KATKAT_BGTRIAL_BURST=1: 1. ve 4. sn'de kayıtlı çözümün adımları oynanır (büyük hamle ışıltısı ve sarsıntı
                            // görünsün; KATKAT_BGTRIAL_LEVEL=3: ilk dokunuş 8+ taşlık grup). Tahta zamanı kareyle ilerletilir.
                            if (burst && (f == 30 || f == 120))
                            {
                                var sol = gc.CurrentLevel.Solution;
                                int k = f == 30 ? 0 : 1;
                                if (k < sol.Count) gc.PlayStep(sol[k]);
                            }
                            gc.Board.Advance(1000f / 30f);
                        });
                        BgMotion.ClockOverride = null;
                        continue;
                    }
                    GameCapture.ShotPublic($"arkaplan_deneme/deneme_{i + 1}.png", 1080, 2340, gc => gc.StartLevel(level - 1));
                }
            }
            finally
            {
                for (int i = 0; i < saved.Length; i++) Vr2.Skins[i] = saved[i];
                Vr2.PreviewBackground = null;
                Vr2.BlurBackground = savedBlur;
                GameController.Vr2SkinOverride = null;
            }
            File.WriteAllText(Path.Combine(outDir, "palet.txt"), report.ToString());
            Debug.Log("[KatKat] arka plan denemesi tamam → " + outDir);
        }

        // ---- Çerçeve denemesi ------------------------------------------------------------------------------------------

        public const string FrameDir = "docs/yeni_v1/tahtalar";
        /// <summary>Çerçeve → arka plan eşlemesi (0 tabanlı, dosya adı sırası): ahşap-çiçek → büyülü bahçe, turkuaz → gizemli
        /// şelale, mermer sütun → yeşil vaha, mor kristal → kozmik, harabe taşı → mistik orman, altın-mor → çöl.</summary>
        private static readonly int[] FrameBg = { 0, 1, 5, 3, 4, 2 };

        public sealed class Frame { public Sprite Sprite; public Rect Inner; } // Inner: piksel, sprite dokusunda

        /// <summary>3×2 tabakayı keser; kenardan bağlı beyaz zemin saydam olur, iç alan merkezden taramayla bulunur.</summary>
        public static Frame[] LoadFrames(string sheetPath)
        {
            var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            sheet.LoadImage(File.ReadAllBytes(sheetPath));
            int W = sheet.width, H = sheet.height, cw = W / 3, ch = H / 2;
            var all = sheet.GetPixels32();
            var res = new Frame[6];
            for (int k = 0; k < 6; k++)
            {
                int col = k % 3, row = k / 3; // satır 0 üstte; doku altta başlar
                int x0 = col * cw, y0 = (1 - row) * ch;
                var px = new Color32[cw * ch];
                for (int y = 0; y < ch; y++) for (int x = 0; x < cw; x++) px[y * cw + x] = all[(y0 + y) * W + x0 + x];
                // Beyaz zemin: kenardan taşma doldurma.
                var bg = new bool[cw * ch];
                var q = new System.Collections.Generic.Queue<int>();
                bool White(Color32 c) { int mn = Math.Min(c.r, Math.Min(c.g, c.b)), mx = Math.Max(c.r, Math.Max(c.g, c.b)); return mn >= 222 && mx - mn < 28; }
                for (int x = 0; x < cw; x++) { q.Enqueue(x); q.Enqueue((ch - 1) * cw + x); }
                for (int y = 0; y < ch; y++) { q.Enqueue(y * cw); q.Enqueue(y * cw + cw - 1); }
                while (q.Count > 0)
                {
                    int p = q.Dequeue();
                    if (bg[p] || !White(px[p])) continue;
                    bg[p] = true;
                    int x = p % cw, y = p / cw;
                    if (x > 0) q.Enqueue(p - 1); if (x < cw - 1) q.Enqueue(p + 1);
                    if (y > 0) q.Enqueue(p - cw); if (y < ch - 1) q.Enqueue(p + cw);
                }
                int minX = cw, minY = ch, maxX = 0, maxY = 0;
                for (int y = 0; y < ch; y++)
                for (int x = 0; x < cw; x++)
                {
                    int p = y * cw + x;
                    if (bg[p]) { px[p] = new Color32(0, 0, 0, 0); continue; }
                    // Zemine komşu açık pikseller yarı saydam (kenar yumuşar).
                    bool edge = (x > 0 && bg[p - 1]) || (x < cw - 1 && bg[p + 1]) || (y > 0 && bg[p - cw]) || (y < ch - 1 && bg[p + cw]);
                    if (edge && White(px[p])) px[p].a = 90;
                    minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                }
                int tw = maxX - minX + 1, th = maxY - minY + 1;
                var t = new Texture2D(tw, th, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = $"cerceve_{k + 1}" };
                var o = new Color32[tw * th];
                for (int y = 0; y < th; y++) for (int x = 0; x < tw; x++) o[y * tw + x] = px[(minY + y) * cw + minX + x];
                t.SetPixels32(o); t.Apply(false, false);
                res[k] = new Frame { Sprite = Sprite.Create(t, new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), 100f), Inner = InnerRect(o, tw, th) };
                Debug.Log($"[KatKat] çerçeve {k + 1}: {tw}x{th}, iç {res[k].Inner}");
            }
            return res;
        }

        /// <summary>İç alan: merkezden dört yöne, ani renk değişimine (iç kenar gölgesi/çerçeve) kadar tarama; üç çizginin en içtekisi.</summary>
        private static Rect InnerRect(Color32[] px, int w, int h)
        {
            int cx = w / 2, cy = h / 2;
            var c0 = px[cy * w + cx];
            int Scan(int x, int y, int dx, int dy)
            {
                var prev = px[y * w + x];
                while (true)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) return dx != 0 ? x : y;
                    var c = px[ny * w + nx];
                    if (Dist(c, prev) > 22 || Dist(c, c0) > 120 || c.a < 200) return dx != 0 ? x : y;
                    prev = c; x = nx; y = ny;
                }
            }
            int l = int.MinValue, r = int.MaxValue, b = int.MinValue, tp = int.MaxValue;
            foreach (float f in new[] { 0.4f, 0.5f, 0.6f })
            {
                int yy = (int)(h * f), xx = (int)(w * f);
                l = Math.Max(l, Scan(cx, yy, -1, 0)); r = Math.Min(r, Scan(cx, yy, 1, 0));
                b = Math.Max(b, Scan(xx, cy, 0, -1)); tp = Math.Min(tp, Scan(xx, cy, 0, 1));
            }
            return new Rect(l, b, r - l + 1, tp - b + 1);
        }

        private static int Dist(Color32 a, Color32 b) => Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b);

        /// <summary>Tepsiyi gizler; çerçeveyi iç alanı taşların kapladığı alanı (küçük payla) saracak biçimde tahtanın arkasına koyar.</summary>
        private static void ApplyFrame(GameController gc, Frame fr)
        {
            var board = gc.Board;
            foreach (var sr in board.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.gameObject.name == "FloorShape" || sr.gameObject.name == "FloorShadow") sr.enabled = false;
            bool any = false; var bb = new Bounds();
            foreach (var s in board.GetComponentsInChildren<StackView>())
            foreach (var r in s.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (!any) { bb = r.bounds; any = true; } else bb.Encapsulate(r.bounds);
            }
            if (!any) return;
            float gap = board.Pitch * 0.14f;
            var go = new GameObject("TrialFrame");
            go.transform.SetParent(board.transform, false);
            var fsr = go.AddComponent<SpriteRenderer>();
            fsr.sprite = fr.Sprite; fsr.sortingOrder = 1;
            float sx = (bb.size.x + 2 * gap) / (fr.Inner.width / 100f), sy = (bb.size.y + 2 * gap) / (fr.Inner.height / 100f);
            // Gerilme sınırı: eksenler arası oran en çok 1,12 (fazlası iç alanda pay olarak kalır).
            float sxF = Mathf.Max(sx, sy / 1.12f), syF = Mathf.Max(sy, sx / 1.12f);
            sx = sxF; sy = syF;
            // Çerçeve ekrana sığsın: dış genişlik görüşün %97'si, yükseklik %64'ü; gerekirse tahta (taşlar + çerçeve) küçülür.
            var cam = Camera.allCameras.Length > 0 ? Camera.allCameras[0] : null;
            if (cam != null)
            {
                float viewH = cam.orthographicSize * 2f, viewW = viewH * cam.aspect;
                var tx0 = fr.Sprite.texture;
                float outerW = tx0.width / 100f * sx, outerH = tx0.height / 100f * sy;
                float k = Mathf.Min(1f, viewW * 0.97f / outerW, viewH * 0.64f / outerH);
                if (k < 1f)
                {
                    var c = bb.center;
                    board.transform.position = c + (board.transform.position - c) * k;
                    board.transform.localScale *= k;
                    bb = new Bounds(c, bb.size * k);
                    sx *= k; sy *= k;
                }
            }
            var lossy = board.transform.lossyScale;
            go.transform.localScale = new Vector3(sx / lossy.x, sy / lossy.y, 1f);
            var tex = fr.Sprite.texture;
            var innerC = new Vector2(fr.Inner.center.x - tex.width * 0.5f, fr.Inner.center.y - tex.height * 0.5f) / 100f;
            go.transform.position = new Vector3(bb.center.x - innerC.x * sx, bb.center.y - innerC.y * sy, board.transform.position.z);
        }

        /// <summary>Oyun alanı bölgesinin (yatay %15–85, dikey %25–80) ortalama rengi zemin; aynı ton ailesinden açık, ana ve koyu ton.</summary>
        public static (Color hi, Color b, Color sh, Color floor) PaletteFrom(Texture2D tex)
        {
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            double r = 0, g = 0, bl = 0; int n = 0;
            for (int y = (int)(h * 0.20f); y < (int)(h * 0.75f); y += 2)
            for (int x = (int)(w * 0.15f); x < (int)(w * 0.85f); x += 2)
            {
                var c = px[y * w + x];
                r += c.r; g += c.g; bl += c.b; n++;
            }
            var floor = new Color((float)(r / n / 255.0), (float)(g / n / 255.0), (float)(bl / n / 255.0));
            Color.RGBToHSV(floor, out float hh, out float s, out float v);
            var b = Color.HSVToRGB(hh, Mathf.Clamp(s * 1.2f, 0.35f, 0.85f), Mathf.Clamp(v, 0.38f, 0.62f));
            var hi = Color.HSVToRGB(hh, Mathf.Clamp(s * 0.6f, 0.18f, 0.6f), Mathf.Clamp(v + 0.35f, 0.75f, 0.95f));
            var sh = Color.HSVToRGB(hh, Mathf.Clamp(s * 1.1f, 0.35f, 0.85f), Mathf.Clamp(v * 0.5f, 0.18f, 0.32f));
            return (hi, b, sh, floor);
        }

        private static uint Hex(Color c)
        {
            Color32 k = c;
            return ((uint)k.r << 16) | ((uint)k.g << 8) | k.b;
        }
    }
}
