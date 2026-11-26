using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KatKat.Game;
using KatKat.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// 2026-11-26 (kullanıcı: "123. bölümde kova tahtanın üstüne çıkıyor bir kısmı. Tüm bölümlerde kovanın ve torbanın bu durumlarını
    /// kontrol et. görsel hata olmasın"; "Sen yinede tüm ihtimalleri test et"): bütün su (kova) ve torba bölümlerinde, birkaç ekran
    /// oranında ve güvenli alanla, şu durumlarda kap dikdörtgeninin etkin hücrelere ve tahta alanının dışına taşmadığı denetlenir:
    /// önceki bölümden geçiş (ilk kare ve oturmuş), başka bölümden giriş, yeniden başlatma, kayıtlı çözümün her hamlesi, geri alma.
    /// Rapor Logs/kap_denetim.md; hatalı durumların ekranı Logs/captures/kap_hata/. (tools/unity.ps1 audit-kap)
    /// </summary>
    public static class ContainerAudit
    {
        private sealed class Size { public int W, H; public Rect Safe; public string Name; }

        private static readonly Size[] Sizes =
        {
            new Size { W = 1080, H = 2340, Safe = new Rect(0, 0, 1, 1), Name = "telefon" },
            new Size { W = 1170, H = 2532, Safe = new Rect(0f, 0.0134f, 1f, 0.9483f), Name = "iphone13" },
            new Size { W = 750, H = 1334, Safe = new Rect(0, 0, 1, 1), Name = "kisa" },
            new Size { W = 1640, H = 2360, Safe = new Rect(0, 0, 1, 1), Name = "tablet" },
            new Size { W = 1536, H = 2048, Safe = new Rect(0, 0, 1, 1), Name = "ipad43" },
        };

        private sealed class Fail { public int Level; public string Size, Case, Detail; public int Step = -1; }

        public static void RunBatch()
        {
            int code = 0;
            try { code = Run(); }
            catch (Exception e) { Debug.LogError("[KatKat] kap denetimi hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static int Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "kap_hata"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string only = Environment.GetEnvironmentVariable("KATKAT_LEVELS");
            bool steps = Environment.GetEnvironmentVariable("KATKAT_NOSTEPS") != "1";
            var fails = new List<Fail>();
            var counts = new Dictionary<string, int>();
            List<int> levels = null;
            string onlySizes = Environment.GetEnvironmentVariable("KATKAT_AUDITSIZES");
            foreach (var sz in Sizes.Where(z => string.IsNullOrEmpty(onlySizes) || onlySizes.Contains(z.Name)))
            {
                GameCapture.ShotPublic($"kap_hata/_son_{sz.Name}.png", sz.W, sz.H, sz.Safe, gc =>
                {
                    if (levels == null)
                    {
                        levels = new List<int>();
                        for (int i = 0; i < gc.Catalog.Count; i++)
                        {
                            var d = gc.Catalog.Levels[i];
                            if (d.Buckets.Count > 0 || d.Goals.Exists(g => g.Type == "fillWater")) levels.Add(i + 1);
                        }
                        if (!string.IsNullOrEmpty(only)) levels = only.Split(',').Select(s => int.Parse(s.Trim())).ToList();
                    }
                    int done = 0;
                    foreach (int n in levels) { AuditLevel(gc, n, sz, fails, counts, steps && sz.Name == "telefon"); if (++done % 25 == 0) Debug.Log($"[KatKat] kap denetimi {sz.Name}: {done}/{levels.Count}, hata {fails.Count}"); }
                });
            }
            var sb = new StringBuilder();
            sb.AppendLine("# Kova ve torba yerleşim denetimi");
            sb.AppendLine();
            sb.AppendLine($"Tarih: {DateTime.Now:yyyy-MM-dd HH:mm}. Bölüm: {levels?.Count ?? 0} (su + torba). Ekran: {string.Join(", ", Sizes.Select(s => $"{s.Name} {s.W}x{s.H}"))}.");
            sb.AppendLine();
            sb.AppendLine("| Durum | Denetim |");
            sb.AppendLine("|---|---|");
            foreach (var kv in counts.OrderBy(k => k.Key)) sb.AppendLine($"| {kv.Key} | {kv.Value} |");
            sb.AppendLine();
            sb.AppendLine($"**Hata: {fails.Count}**");
            sb.AppendLine();
            foreach (var g in fails.GroupBy(f => f.Level).Take(400))
            {
                sb.AppendLine($"- Bölüm {g.Key}:");
                foreach (var f in g.Take(12)) sb.AppendLine($"  - {f.Size} / {f.Case}{(f.Step >= 0 ? " adım " + f.Step : "")}: {f.Detail}");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/kap_denetim.md", sb.ToString());
            Debug.Log($"[KatKat] kap denetimi: {fails.Count} hata, {counts.Values.Sum()} denetim → Logs/kap_denetim.md");

            // Hatalı ilk birkaç durumun ekranı.
            foreach (var f in fails.GroupBy(x => x.Level + "/" + x.Size + "/" + x.Case).Select(g => g.First()).Take(12))
            {
                var sz = Sizes.First(s => s.Name == f.Size);
                GameCapture.ShotPublic($"kap_hata/b{f.Level:000}_{f.Size}_{f.Case}.png", sz.W, sz.H, sz.Safe, gc => Replay(gc, f));
            }
            return 0;
        }

        private static void Replay(GameController gc, Fail f)
        {
            int idx = f.Level - 1;
            if (f.Case.StartsWith("onceki")) gc.StartLevel(Math.Max(0, idx - 1));
            else if (f.Case.StartsWith("giris")) gc.StartLevel(9);
            gc.StartLevel(idx);
            gc.Hud.HideRuleCard(false);
            if (!f.Case.EndsWith("ilk")) Settle(gc);
        }

        private static void AuditLevel(GameController gc, int n, Size sz, List<Fail> fails, Dictionary<string, int> counts, bool steps)
        {
            int idx = n - 1;
            // Denetim yüzlerce kez bölüm bırakıp girer: kalp kapısı bölümü açmasın diye sınırsız kalp.
            gc.SaveService.Data.meta.heartsInfiniteUntil = long.MaxValue / 4;
            // 1) Önceki bölümden geçiş: ilk kare (kurulumdan hemen sonra) ve oturmuş.
            gc.StartLevel(Math.Max(0, idx - 1));
            gc.Hud.HideRuleCard(false);
            Settle(gc);
            gc.StartLevel(idx);
            gc.Hud.HideRuleCard(false);
            Check(gc, n, sz, "onceki_ilk", -1, fails, counts);
            Settle(gc);
            Check(gc, n, sz, "onceki", -1, fails, counts);
            // 2) Başka (normal) bölümden giriş.
            gc.StartLevel(9);
            Settle(gc);
            gc.StartLevel(idx);
            gc.Hud.HideRuleCard(false);
            Check(gc, n, sz, "giris_ilk", -1, fails, counts);
            Settle(gc);
            Check(gc, n, sz, "giris", -1, fails, counts);
            // 3) Yeniden başlat.
            gc.Restart();
            gc.Hud.HideRuleCard(false);
            Settle(gc);
            Check(gc, n, sz, "yeniden", -1, fails, counts);
            if (!steps) return;
            // 4) Kayıtlı çözüm, her hamleden sonra; ilk hamleden sonra geri al ve yeniden oyna.
            var d = gc.Catalog.Levels[idx];
            for (int i = 0; i < d.Solution.Count; i++)
            {
                if (gc.Session.State.Status != KatKat.Core.GameStatus.Idle) break;
                gc.PlayStep(d.Solution[i]);
                for (int k = 0; k < 400 && (gc.Board.IsPlaying || gc.Board.HasFx); k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); gc.Board.AdvanceWater(0.016f); }
                gc.Hud.AdvanceEffects(1500f);
                Settle(gc);
                Check(gc, n, sz, "hamle", i + 1, fails, counts);
                if (i == 0 && gc.Session.State.Status == KatKat.Core.GameStatus.Idle)
                {
                    gc.Undo();
                    Settle(gc);
                    Check(gc, n, sz, "geri_al", 1, fails, counts);
                    gc.PlayStep(d.Solution[0]);
                    for (int k = 0; k < 400 && (gc.Board.IsPlaying || gc.Board.HasFx); k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); gc.Board.AdvanceWater(0.016f); }
                    gc.Hud.AdvanceEffects(1500f);
                    Settle(gc);
                }
            }
        }

        private static void Settle(GameController gc)
        {
            Canvas.ForceUpdateCanvases();
            gc.Board.AdvanceWater(0.05f);
            if (gc.BucketLevel) gc.Hud.AdvanceBucket(0.02f);
            Canvas.ForceUpdateCanvases();
        }

        private static void Check(GameController gc, int n, Size sz, string kase, int step, List<Fail> fails, Dictionary<string, int> counts)
        {
            string key = (gc.BucketLevel ? "torba " : "kova ") + kase;
            counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
            Rect r;
            bool bag = gc.BucketLevel;
            if (bag)
            {
                // 2026-11-26: torba üst güç bandında; tam şişmiş ve en büyük zıplamada da bandın içinde kalmalı (ne hedef kartına
                // ne tahtaya taşar). Bant tahtanın üstünde ayrılmış alandadır.
                if (!gc.Hud.AuditBagInBand(out var bagR, out var band)) { fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = "torba bantta değil" }); return; }
                const float tolPx = 1.5f;
                if (bagR.yMin < band.yMin - tolPx || bagR.yMax > band.yMax + tolPx || bagR.xMin < band.xMin - tolPx || bagR.xMax > band.xMax + tolPx)
                    fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = $"torba banttan taşıyor: torba {Fmt(bagR)} bant {Fmt(band)}" });
                // 2026-11-26 (kullanıcı: "Bantta, iki yanında yuvalar"): tepsi yuvaları (en geniş simge, seçili) torbaya binmez, bantta kalır.
                var slots = new List<Rect>();
                if (gc.Hud.AuditTraySlots(slots))
                    for (int i = 0; i < slots.Count; i++)
                    {
                        var s = slots[i];
                        if (s.Overlaps(bagR)) fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = $"yuva {i} torbaya biniyor: yuva {Fmt(s)} torba {Fmt(bagR)}" });
                        if (s.xMin < band.xMin - tolPx || s.xMax > band.xMax + tolPx || s.yMin < band.yMin - tolPx || s.yMax > band.yMax + tolPx)
                            fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = $"yuva {i} banttan taşıyor: yuva {Fmt(s)} bant {Fmt(band)}" });
                        for (int j = i + 1; j < slots.Count; j++)
                            if (s.Overlaps(slots[j]) && Mathf.Min(s.xMax, slots[j].xMax) - Mathf.Max(s.xMin, slots[j].xMin) > s.width * 0.25f)
                                fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = $"yuva {i} ile {j} üst üste" });
                    }
                else fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = "torba bölümünde tepsi görünmüyor" });
                return;
            }
            else r = gc.AuditWaterBucketWorldRect();
            if (r.width <= 0f || r.height <= 0f) { fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = "kap yok / boyu 0" }); return; }
            var cells = gc.AuditCellWorldRects(0.86f);
            if (cells.Count == 0) return;
            float cellArea = cells[0].width * cells[0].height;
            var hits = new List<string>();
            foreach (var cell in cells)
            {
                float ix = Mathf.Min(r.xMax, cell.xMax) - Mathf.Max(r.xMin, cell.xMin);
                float iy = Mathf.Min(r.yMax, cell.yMax) - Mathf.Max(r.yMin, cell.yMin);
                if (ix > 0f && iy > 0f && ix * iy > cellArea * 0.03f) hits.Add($"%{Mathf.RoundToInt(100f * ix * iy / cellArea)}");
            }
            if (hits.Count > 0)
                fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = $"{hits.Count} hücreye biniyor ({string.Join(" ", hits.Take(4))}); kap {Fmt(r)}" });
            var area = gc.AuditBoardArea();
            float tol = cells[0].height * 0.15f;
            if (!bag && r.yMin < area.yMin - tol)
                fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = $"kova tahta alanının altına taşıyor ({(area.yMin - r.yMin) / cells[0].height:0.00} hücre)" });
            if (r.xMin < area.xMin - tol || r.xMax > area.xMax + tol)
                fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = "kap yanlardan taşıyor" });
            if (bag && r.yMax > area.yMax + cells[0].height * 1.2f)
                fails.Add(new Fail { Level = n, Size = sz.Name, Case = kase, Step = step, Detail = $"torba tahta alanının çok üstüne çıkıyor ({(r.yMax - area.yMax) / cells[0].height:0.00} hücre)" });
        }

        private static string Fmt(Rect r) => $"({r.xMin:0.00},{r.yMin:0.00})–({r.xMax:0.00},{r.yMax:0.00})";
    }
}
