using System;
using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// 2026-11-26 (kullanıcı: "123. bölümde kova tahtanın üstüne çıkıyor bir kısmı. Tüm bölümlerde kovanın ve torbanın bu durumlarını
    /// kontrol et. görsel hata olmasın"): seçili bölümlerin ekranı. KATKAT_LEVELS="123,19" (boşsa 123). Çıktı Logs/captures/kap/.
    /// </summary>
    public static class CaptureContainerAudit
    {
        private const int W = 1080, H = 2340;

        public static void RunBatch()
        {
            int code = 0;
            try { Run(); }
            catch (Exception e) { Debug.LogError("[KatKat] kap yakalama hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "kap"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string env = Environment.GetEnvironmentVariable("KATKAT_LEVELS");
            var levels = string.IsNullOrEmpty(env) ? new[] { 123 } : env.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
            string sz = Environment.GetEnvironmentVariable("KATKAT_SIZES");
            var sizes = string.IsNullOrEmpty(sz) ? new[] { new Vector2Int(W, H) } : sz.Split(',').Select(p => { var a = p.Split('x'); return new Vector2Int(int.Parse(a[0]), int.Parse(a[1])); }).ToArray();
            foreach (var size in sizes)
            foreach (int n in levels)
                GameCapture.ShotPublic($"kap/bolum_{n:000}_{size.x}x{size.y}.png", size.x, size.y, gc =>
                {
                    gc.StartLevel(n - 1);
                    gc.Hud.HideRuleCard(false);
                    float fill = float.Parse(Environment.GetEnvironmentVariable("KATKAT_FILL") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                    var st = gc.Session.State;
                    if (fill > 0f && st.WaterCap > 0) { st.WaterHave = Mathf.RoundToInt(st.WaterCap * fill); gc.Board.SetWater(st.WaterCap, st.WaterHave, st.Board); }
                    gc.Board.AdvanceWater(1.2f);
                    gc.Hud.AdvanceWaterBar(0.3f);
                    UnityEngine.Canvas.ForceUpdateCanvases();
                });
            if (Environment.GetEnvironmentVariable("KATKAT_STEPS") == "1")
                foreach (int n in levels)
                {
                    var d = KatKat.Levels.LevelParser.Parse(Resources.Load<TextAsset>($"Levels/level_{n:000}").text, "x");
                    for (int u = 1; u <= d.Solution.Count; u++)
                    {
                        int cnt = u;
                        GameCapture.ShotPublic($"kap/bolum_{n:000}_adim{u:00}.png", W, H, gc =>
                        {
                            gc.StartLevel(n - 1);
                            gc.Hud.HideRuleCard(false);
                            for (int i = 0; i < cnt; i++)
                            {
                                gc.PlayStep(d.Solution[i]);
                                for (int k = 0; k < 400 && (gc.Board.IsPlaying || gc.Board.HasFx); k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); gc.Board.AdvanceWater(0.016f); }
                                gc.Hud.AdvanceEffects(1500f);
                                gc.Board.AdvanceWater(1.5f);
                            }
                            UnityEngine.Canvas.ForceUpdateCanvases();
                        });
                    }
                }
            // Kova gönderimi kare kare (2026-11-26: kova uçar, karta boşaltır, döner).
            if (Environment.GetEnvironmentVariable("KATKAT_SENDFRAMES") == "1")
                foreach (int n in levels)
                    GameCapture.FramesPublic($"kap/gonder_{n:000}", W, H, gc =>
                    {
                        gc.StartLevel(n - 1);
                        gc.Hud.HideRuleCard(false);
                        var st = gc.Session.State;
                        st.WaterHave = st.WaterCap;
                        gc.Board.SetWater(st.WaterCap, st.WaterHave, st.Board);
                        gc.Board.AdvanceWater(1.2f);
                        gc.Hud.AdvanceWaterBar(0.3f);
                        gc.SendWaterBucket();
                    }, 18, (gc, f) => { gc.Board.AdvanceWater(0.13f); gc.Hud.AdvanceWaterBar(0.13f); UnityEngine.Canvas.ForceUpdateCanvases(); });
            // 2026-11-26 özel taş efektleri kare kare (test bölümleri; özel taşlar açılışta tepside): senaryo = lab no, tür, eylem.
            // Eylem: "use" yuva seç + hücreye dokun (yuvadan hedefe uçuş, sonra efekt), "drag" yuvadan sürükle (hayalet + önizleme,
            // 6. karede bırak), "hold" sürükle ve bırakma (yalnız önizleme), "create" büyük gruba dokun (doğan taş tepsiye uçar).
            if (Environment.GetEnvironmentVariable("KATKAT_FXFRAMES") == "1")
            {
                var scen = new (string Name, int Lab, SpecialKind Kind, string Act)[]
                {
                    ("roket_satir", 1, SpecialKind.RocketRow, "use"),
                    ("roket_sutun", 1, SpecialKind.RocketColumn, "use"),
                    ("cift_roket", 2, SpecialKind.RocketRow2, "use"),
                    ("cift_sutun", 2, SpecialKind.RocketColumn2, "use"),
                    ("altin_rulo", 3, SpecialKind.Rainbow, "use"),
                    ("bomba_imha", 4, SpecialKind.RocketRow, "use"),
                    ("kurek", 5, SpecialKind.Shovel, "use"),
                    ("torba", 7, SpecialKind.RocketRow, "use"),
                    ("surukle_roket", 1, SpecialKind.RocketRow, "drag"),
                    ("onizle_cift", 2, SpecialKind.RocketColumn2, "hold"),
                    ("onizle_rulo", 3, SpecialKind.Rainbow, "hold"),
                    ("onizle_kurek", 5, SpecialKind.Shovel, "hold"),
                    ("dogan_tepsi", 0, SpecialKind.None, "create"),
                };
                string only = Environment.GetEnvironmentVariable("KATKAT_FXONLY");
                foreach (var sc in scen)
                {
                    if (!string.IsNullOrEmpty(only) && !only.Split(',').Any(o => sc.Name.Contains(o.Trim()))) continue;
                    int slot = -1, tx = -1, ty = -1;
                    GameCapture.FramesPublic($"kap/fx_{sc.Name}", W, H, gc =>
                    {
                        gc.ShowPilots();
                        gc.StartTrial(sc.Lab);
                        gc.Hud.HideRuleCard(false);
                        UnityEngine.Canvas.ForceUpdateCanvases();
                        var st = gc.Session.State;
                        if (sc.Act == "create")
                        {
                            st.Tray.Clear();
                            gc.Hud.SetTray(st.Tray, -1);
                            var big = GroupFinder.AllComponents(st.Board).Where(c => RulesEngine.Validate(st, Command.Tap(st.Board.X(c[0]), st.Board.Y(c[0])), out _) == RejectReason.None)
                                .OrderByDescending(c => c.Count).FirstOrDefault();
                            if (big == null || big.Count < 5) { Debug.LogWarning($"[KatKat] fx {sc.Name}: 5+ grup yok ({big?.Count})"); if (big == null) return; }
                            Debug.Log($"[KatKat] fx {sc.Name}: grup {big.Count}");
                            gc.OnCellPressed(st.Board.X(big[0]), st.Board.Y(big[0]));
                            return;
                        }
                        slot = st.Tray.IndexOf(sc.Kind);
                        if (slot < 0) { Debug.LogWarning($"[KatKat] fx {sc.Name}: tepside {sc.Kind} yok (tepsi: {string.Join(",", st.Tray)})"); return; }
                        (tx, ty) = BestTarget(st, slot, sc.Name == "bomba_imha");
                        if (tx < 0) { Debug.LogWarning($"[KatKat] fx {sc.Name}: geçerli hedef yok"); return; }
                        Debug.Log($"[KatKat] fx {sc.Name}: yuva {slot} {sc.Kind} → ({tx},{ty})");
                        if (sc.Act == "use") { gc.PressTraySlot(slot); gc.OnCellPressed(tx, ty); return; }
                        // Sürükleme: yuvadan başlar, hedefe doğru kayar.
                        gc.TrayDragBegin(slot, gc.TraySlotScreenPoint(slot));
                    }, sc.Act == "hold" ? 8 : 22, (gc, fr) =>
                    {
                        if (sc.Act == "drag" || sc.Act == "hold")
                        {
                            if (slot >= 0 && tx >= 0 && (sc.Act == "hold" || fr <= 6))
                            {
                                var a = gc.TraySlotScreenPoint(slot);
                                var b = gc.CellScreenPoint(tx, ty);
                                float u = Mathf.Clamp01((fr + 1) / 4f);
                                var p = Vector2.Lerp(a, b, u * u * (3f - 2f * u));
                                if (sc.Act == "drag" && fr == 6) gc.TrayDragEnd(b);
                                else gc.TrayDragMove(p);
                            }
                        }
                        float ms = fr == 0 ? 16f : fr < 6 ? 45f : 100f;
                        gc.Board.Advance(ms);
                        gc.Board.Director.Advance(ms);
                        gc.Hud.AdvanceEffects(ms);
                        UnityEngine.Canvas.ForceUpdateCanvases();
                    });
                }
            }
            // 2026-11-26 ortak taş patlaması ve hedefe uçuş kare kare (KATKAT_BLASTFRAMES=1; KATKAT_FXONLY süzer): hedef renginde küçük
            // ve büyük grup, hedef dışı grup, hamlede dolan görev. Bölüm: KATKAT_BLASTLEVEL (yoksa uygun ilk bölüm aranır).
            if (Environment.GetEnvironmentVariable("KATKAT_BLASTFRAMES") == "1")
            {
                string only = Environment.GetEnvironmentVariable("KATKAT_FXONLY");
                foreach (var name in new[] { "hedef_kucuk", "hedef_buyuk", "hedef_disi", "hedef_tamam" })
                {
                    if (!string.IsNullOrEmpty(only) && !only.Contains(name)) continue;
                    GameCapture.FramesPublic($"kap/fx_{name}", W, H, gc => BlastScenario(gc, name), 30,
                        (gc, fr) => { float ms = fr == 0 ? 16f : 34f; gc.Board.Advance(ms); gc.Hud.AdvanceChips(ms); gc.Hud.AdvanceEffects(ms); UnityEngine.Canvas.ForceUpdateCanvases(); });
                }
            }
            Debug.Log("[KatKat] kap yakalamaları tamam");
        }

        /// <summary>
        /// Tepsi yuvasının en etkili hedefi (kopyada denenir): en çok hücre soyan / çakıl kaldıran; <paramref name="defuse"/> ise en çok
        /// bomba imha eden. Eşitlikte tahtanın ortasına yakın olan.
        /// </summary>
        private static (int, int) BestTarget(LevelState st, int slot, bool defuse)
        {
            int bx = -1, by = -1;
            float best = -1f;
            var b = st.Board;
            for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
            {
                var cmd = Command.UseSpecial(slot, x, y);
                if (RulesEngine.Validate(st, cmd, out _) != RejectReason.None) continue;
                var r = RulesEngine.TryApply(st.Clone(), cmd);
                if (r == null || !r.Accepted || r.Waves.Count == 0) continue;
                var w = r.Waves[0];
                float score = w.Cells.Count + w.GravelCleared.Count * 2f + (defuse ? w.Defused.Count * 100f : w.Defused.Count * 3f);
                score -= (Mathf.Abs(x - (b.Width - 1) * 0.5f) + Mathf.Abs(y - (b.Height - 1) * 0.5f)) * 0.1f;
                if (score > best) { best = score; bx = x; by = y; }
            }
            return (bx, by);
        }

        /// <summary>Senaryoya uygun bölüm ve grup bulur, dokunur. Hedef rengi: CollectColor görevi.</summary>
        private static void BlastScenario(GameController gc, string name)
        {
            string lv = Environment.GetEnvironmentVariable("KATKAT_BLASTLEVEL");
            int from = string.IsNullOrEmpty(lv) ? 6 : int.Parse(lv), to = string.IsNullOrEmpty(lv) ? Math.Min(80, gc.Catalog.Count) : from;
            for (int n = from; n <= to; n++)
            {
                gc.StartLevel(n - 1);
                gc.Hud.HideRuleCard(false);
                var st = gc.Session.State;
                if (st.Bucket != null || st.WaterCap > 0) continue;
                var goalIdx = Enumerable.Range(0, st.Goals.Count).Where(g => st.Goals[g].Type == GoalType.CollectColor).ToList();
                if (goalIdx.Count == 0) continue;
                var groups = GroupFinder.AllComponents(st.Board)
                    .Where(c => RulesEngine.Validate(st, Command.Tap(st.Board.X(c[0]), st.Board.Y(c[0])), out _) == RejectReason.None).ToList();
                bool IsGoal(System.Collections.Generic.List<int> c) => goalIdx.Any(g => st.Goals[g].Color == st.Board[c[0]].Top);
                System.Collections.Generic.List<int> pick = null;
                if (name == "hedef_kucuk") pick = groups.Where(c => IsGoal(c) && c.Count <= 4).OrderBy(c => c.Count).FirstOrDefault();
                else if (name == "hedef_buyuk") pick = groups.Where(c => IsGoal(c) && c.Count >= 6).OrderByDescending(c => c.Count).FirstOrDefault();
                else if (name == "hedef_disi") pick = groups.Where(c => !IsGoal(c) && c.Count >= 3).OrderByDescending(c => c.Count).FirstOrDefault();
                else pick = groups.Where(c => IsGoal(c) && c.Count >= 4).OrderByDescending(c => c.Count).FirstOrDefault();
                if (pick == null) continue;
                if (name == "hedef_tamam")
                {
                    // Görev bu dokunuşla dolsun: kalan ihtiyaç grubun yarısı (fazla taşlar yalnız patlar).
                    int g = goalIdx.First(i => st.Goals[i].Color == st.Board[pick[0]].Top);
                    gc.AuditSetGoalProgress(g, Math.Max(0, st.Goals[g].Amount - Math.Max(1, pick.Count / 2)));
                }
                int x = st.Board.X(pick[pick.Count / 2]), y = st.Board.Y(pick[pick.Count / 2]);
                Debug.Log($"[KatKat] blast {name}: bölüm {n}, grup {pick.Count}, renk {st.Board[pick[0]].Top}, hücre ({x},{y})");
                UnityEngine.Canvas.ForceUpdateCanvases(); // HUD hedef simgesi yerinde (uçuş hedefi)
                gc.OnCellPressed(x, y);
                return;
            }
            Debug.LogWarning("[KatKat] blast " + name + ": uygun bölüm yok");
        }
    }
}
