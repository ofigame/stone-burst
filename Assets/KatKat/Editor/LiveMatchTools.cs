using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using KatKat.Core.Live;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Canlı maç prototipi araçları (docs/vr1/CLAUDE_CANLI_MAC.md, kullanıcı 2026-11-13).
    /// - Havuz: doğrulanmış ortak tahtalar → Assets/KatKat/Resources/Live/live_pool_v1.txt (tohumlu, tekrar üretilebilir).
    /// - Ölçüm: botlarla iki sıralı maçlar → docs/vr1/canli_mac/OLCUM_RAPORU.md.
    /// </summary>
    public static class LiveMatchTools
    {
        public const string PoolPath = "Assets/KatKat/Resources/Live/live_pool_v1.txt";
        public const string ReportPath = "docs/vr1/canli_mac/OLCUM_RAPORU.md";
        public const string DiffPoolPath = "Assets/KatKat/Resources/Live/live_pool_diffhands_v2.txt";
        public const long PoolSeed = 20261113;
        public const int PoolCount = 150;

        /// <summary>
        /// Deneme boyu (kullanıcı 2026-11-13: "5×5 dene"): KATKAT_LIVE_SIZE=N ise N×N kurallarla ayrı havuz
        /// (docs/vr1/canli_mac/havuz_NxN_v1.txt, oyuna girmez) ve ayrı rapor (OLCUM_RAPORU_NxN.md). Boşsa varsayılan 6×6.
        /// </summary>
        /// <summary>Varsayılan havuz (2026-11-14): farklı ama denk eller + alt katman üstten farklı (LM3); oyun bunu yükler.</summary>
        public const string PoolV3Path = "Assets/KatKat/Resources/Live/live_pool_v3.txt";

        /// <summary>Havuz sürümü: KATKAT_LIVE_POOL=v1|v2|v3 (varsayılan v3). Eski ad KATKAT_LIVE_HANDS=same → v1.</summary>
        private static string PoolVariant()
        {
            var v = Environment.GetEnvironmentVariable("KATKAT_LIVE_POOL");
            if (string.IsNullOrEmpty(v) && Environment.GetEnvironmentVariable("KATKAT_LIVE_HANDS") == "same") v = "v1";
            return v == "v1" || v == "v2" ? v : "v3";
        }

        private static int GenOf(LiveRules r) =>
            r.BottomDiffersFromTop ? LiveGenerator.VersionBottomDiffers : r.DifferentHands ? LiveGenerator.VersionDifferentHands : LiveGenerator.Version;

        private static LiveRules Variant(out string poolPath, out string reportPath)
        {
            var rules = new LiveRules();
            string v = PoolVariant();
            // v1: aynı eller, alt serbest · v2: farklı ama denk eller, alt serbest · v3 (varsayılan): farklı eller + alt üstten farklı.
            rules.DifferentHands = v != "v1";
            rules.BottomDiffersFromTop = v == "v3";
            poolPath = v == "v1" ? PoolPath : v == "v2" ? DiffPoolPath : PoolV3Path;
            reportPath = v == "v1" ? ReportPath : v == "v2" ? "docs/vr1/canli_mac/OLCUM_RAPORU_farkli_el.md" : "docs/vr1/canli_mac/OLCUM_RAPORU_v3.md";
            if (int.TryParse(Environment.GetEnvironmentVariable("KATKAT_LIVE_SIZE"), out int n) && n >= 3 && n != rules.Width)
            {
                rules.Width = rules.Height = n;
                string tag = v == "v1" ? "" : v == "v2" ? "_farkli_el" : "_alt_farkli";
                int gen = GenOf(rules);
                poolPath = $"docs/vr1/canli_mac/havuz_{n}x{n}{tag}_v{gen}.txt";
                reportPath = $"docs/vr1/canli_mac/OLCUM_RAPORU{tag}_{n}x{n}.md";
            }
            return rules;
        }

        [MenuItem("Kat Kat/Canlı maç/Havuzu üret")]
        public static void GeneratePool()
        {
            var rules = Variant(out var PoolPath, out _);
            var stats = new LiveGenerator.Stats();
            var sw = Stopwatch.StartNew();
            var pool = LiveGenerator.BuildPool(PoolCount, PoolSeed, rules, stats);
            sw.Stop();
            var sb = new StringBuilder();
            sb.Append("# Stone Burst canlı maç doğrulanmış tahta havuzu. Elle düzenleme: LiveMatchTools.GeneratePool ile yeniden üret.\n");
            sb.Append($"# gen={GenOf(rules)} seed={PoolSeed} count={pool.Count} kurallar=[{rules}]\n");
            sb.Append("# id|gen|seed|W|H|maske|alt|üst|el|türler[|P2 eli]  (renk: 0 mercan, 1 turkuaz, 2 inci, 3 mor; . etkin değil)\n");
            foreach (var l in pool) sb.Append(l.ToLine()).Append('\n');
            Directory.CreateDirectory(Path.GetDirectoryName(PoolPath));
            File.WriteAllText(PoolPath, sb.ToString(), new UTF8Encoding(false));
            if (PoolPath.StartsWith("Assets/")) AssetDatabase.ImportAsset(PoolPath);
            var rej = new StringBuilder();
            foreach (var kv in stats.Rejects) rej.Append(kv.Key).Append('=').Append(kv.Value).Append(' ');
            Debug.Log($"[KatKat] Canlı maç havuzu: {pool.Count} dizilim, {stats.Tried} aday, tür uymadı {stats.WrongTag}, eşdeğer kopya {stats.Duplicates}, " +
                      $"ret: {rej}süre {sw.ElapsedMilliseconds} ms → {PoolPath}");
            _lastStats = stats;
            _lastMs = sw.ElapsedMilliseconds;
        }

        private static LiveGenerator.Stats _lastStats;
        private static long _lastMs;

        [MenuItem("Kat Kat/Canlı maç/Ölçüm raporu")]
        public static void RunReport()
        {
            var baseRules = Variant(out var PoolPath, out var ReportPath);
            if (!File.Exists(PoolPath)) GeneratePool();
            var pool = LiveLayout.ParsePool(File.ReadAllText(PoolPath));
            var sw = Stopwatch.StartNew();
            var sb = new StringBuilder();
            sb.Append("# Canlı maç ölçüm raporu (botlar)\n\n");
            sb.Append($"Üretildi: {DateTime.Now:yyyy-MM-dd HH:mm} · `LiveMatchTools.RunReport` (tools/unity.ps1 live-report) · havuz `{PoolPath}` (gen {GenOf(baseRules)}, tohum {PoolSeed})\n\n");
            sb.Append("Bu sayılar **bot** maçlarıdır, gerçek oyuncu davranışı değildir. Kuralların kaba dengesini ve açık ayarların etkisini karşılaştırmak içindir.\n\n");
            sb.Append("## Kurallar\n\n- ").Append(baseRules).Append("\n- Dokunarak patlatma her ölçümde açık ve kapalı ayrı çalıştırıldı.\n\n");
            sb.Append("## Havuz çeşitliliği\n\n");
            if (_lastStats != null)
            {
                var rej = new StringBuilder();
                foreach (var kv in _lastStats.Rejects) rej.Append(kv.Key).Append(": ").Append(kv.Value).Append(", ");
                sb.Append($"- Üretim: {_lastStats.Tried} aday denendi, {_lastStats.Accepted} kabul, istenen tür yok {_lastStats.WrongTag}, eşdeğer kopya {_lastStats.Duplicates}; filtre retleri: {rej}süre {_lastMs} ms\n");
            }
            sb.Append(LiveSim.Describe(pool, baseRules)).Append('\n');
            sb.Append("## Yöntem\n\n");
            sb.Append("- Havuzdaki her dizilim **iki kez** oynatılır: bir kez P1 önce, bir kez P2 önce (aynı tahta, aynı el). Böylece tahtanın kendisinden gelen fark iki sırada da bulunur, kalan fark sıradan gelir.\n");
            sb.Append("- **İlk oyuncu avantajı** = ilk oynayanın kazanma yüzdesi − ikinci oynayanınki (beraberlik ayrı). Puan farkı da ortalama olarak verilir.\n");
            sb.Append("- **Maç süresi** = maç başına kabul edilen hamle (her oyuncu 8, toplam 16; pas dahil). Gerçek saniye için prototip ekranı her hamlenin süresini maç kaydına yazar (`LiveRecord.ElapsedMs`); bu süre ancak insanlarla yapılan yerel denemelerde ölçülebilir.\n");
            sb.Append("- **Saldırı tercihi** = kabul edilen hamlelerin türü (puan, hazırlık, saldırı, dokunma, pas) ve saldırının rakibin o anki en iyi hemen puanını ortalama kaç katman düşürdüğü.\n");
            sb.Append("- **Eşitlik bozma**: botlar kuralı hesaba katmaz; her maç bir kez oynanır ve sonucu dört kurala göre ayrı okunur (yok, en büyük tek grup, ikinci oynayan, önce en büyük grup sonra ikinci oynayan).\n");
            sb.Append("- Botlar: Random (rastgele yasal hamle), Greedy (en çok puan; yoksa sonraki tura en iyi hazırlık; bilerek saldırmaz), Aggressive (rakibin en iyi puanı kendisininkinden küçük değilse onu en çok düşüren saldırı), Balanced (kazanç − rakibin ardından en iyisi + 0,5 × kendi sonraki en iyisi).\n\n");
            var kinds = new[] { LiveBotKind.Random, LiveBotKind.Greedy, LiveBotKind.Aggressive, LiveBotKind.Balanced };
            foreach (bool tap in new[] { false, true })
            {
                var rules = baseRules.Clone();
                rules.TapReadyGroups = tap;
                sb.Append($"## Dokunarak patlatma {(tap ? "AÇIK" : "KAPALI")}\n\n");
                var runs = new System.Collections.Generic.List<LiveSim.Summary>();
                foreach (var k in kinds) runs.Add(LiveSim.Run(pool, rules, k, k, 1000 + (tap ? 7 : 0)));
                runs.Add(LiveSim.Run(pool, rules, LiveBotKind.Aggressive, LiveBotKind.Greedy, 2000));
                runs.Add(LiveSim.Run(pool, rules, LiveBotKind.Greedy, LiveBotKind.Aggressive, 3000));
                runs.Add(LiveSim.Run(pool, rules, LiveBotKind.Balanced, LiveBotKind.Greedy, 4000));
                runs.Add(LiveSim.Run(pool, rules, LiveBotKind.Greedy, LiveBotKind.Balanced, 5000));
                sb.Append("Kazanma sütunları ham puanla (eşitlik bozmasız).\n\n");
                sb.Append(LiveSim.Summary.MarkdownHeader).Append('\n');
                foreach (var r in runs) sb.Append(r.ToMarkdownRow()).Append('\n');
                sb.Append("\n### Eşitlik bozma kurallarına göre aynı maçlar (ilk kazanır % / ikinci kazanır % / berabere %)\n\n");
                sb.Append(LiveSim.Summary.TieHeader()).Append('\n');
                foreach (var r in runs) sb.Append(r.ToTieRow()).Append('\n');
                if (baseRules.DifferentHands)
                {
                    sb.Append("\n### El adaleti (aynı bot iki tarafta; P1 = A eli, P2 = B eli, her dizilim iki sırayla)\n\n");
                    sb.Append("50 / 50'ye yakınsa iki el denk. \"Dizilim\" sütunu: aynı dizilimin iki maçını (sıra değişerek) da aynı elin kazandığı dizilimler; ");
                    sb.Append("bu oran yüksekse o dizilimde el, sıradan daha belirleyici.\n\n");
                    sb.Append(LiveSim.Summary.HandHeader).Append('\n');
                    for (int i = 0; i < kinds.Length; i++) sb.Append(runs[i].HandRow()).Append('\n');
                }
                sb.Append('\n');
            }
            sw.Stop();
            sb.Append($"Ölçüm süresi: {sw.ElapsedMilliseconds} ms.\n");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[KatKat] Canlı maç ölçüm raporu → " + ReportPath);
        }

        public const string ShotDir = "docs/vr1/canli_mac/ekranlar";

        /// <summary>Canlı maç ekran görüntüleri (toplu).</summary>
        public static void CaptureBatch()
        {
            int code = 0;
            try { Capture(); }
            catch (Exception e) { Debug.LogError("[KatKat] Canlı maç yakalama hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        /// <summary>
        /// 2026-11-14 yeni ekran (<see cref="KatKat.UI.LiveMatchView"/>, oyunun tahta görünümüyle): açılış, el taşı seçili,
        /// kendi tahtada patlama sonrası, botun hamlesi, rakibe saldırı, maç sonu. Oyuncu tarafını Greedy bot oynar.
        /// </summary>
        [MenuItem("Kat Kat/Canlı maç/Ekran görüntüleri")]
        public static void Capture()
        {
            // KATKAT_LIVE_SHOT_H=2400 gibi: uzun ekran denetimi (ayrı klasöre).
            int w = 1080, h = 1920;
            string shotDir = ShotDir;
            if (int.TryParse(Environment.GetEnvironmentVariable("KATKAT_LIVE_SHOT_H"), out int hh) && hh >= 1200 && hh != h) { h = hh; shotDir = ShotDir + "_" + w + "x" + h; }
            KatKat.UI.UiAspect.Override = (float)w / h;
            KatKat.UI.SafeAreaFitter.NormalizedOverride = new UnityEngine.Rect(0f, 0f, 1f, 1f);
            var camGo = new UnityEngine.GameObject("LiveCam");
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
            cam.backgroundColor = UnityEngine.Color.black;
            cam.transform.position = new UnityEngine.Vector3(0, 0, -10);
            var rt = new UnityEngine.RenderTexture(w, h, 24, UnityEngine.RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = (float)w / h;
            KatKat.UI.LiveMatchView view = null;
            try
            {
                view = KatKat.UI.LiveMatchView.Create(cam, null);
                view.BotDelay = 0f;
                foreach (var c in view.GetComponentsInChildren<UnityEngine.Canvas>(true)) if (c.isRootCanvas) c.scaleFactor = 1f;
                Directory.CreateDirectory(shotDir);
                foreach (var old in Directory.GetFiles(shotDir, "*.png")) File.Delete(old);
                void Shot(string name)
                {
                    view.Refresh();
                    UnityEngine.Canvas.ForceUpdateCanvases();
                    cam.Render();
                    var prev = UnityEngine.RenderTexture.active;
                    UnityEngine.RenderTexture.active = rt;
                    var tex = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGB24, false);
                    tex.ReadPixels(new UnityEngine.Rect(0, 0, w, h), 0, 0);
                    tex.Apply();
                    UnityEngine.RenderTexture.active = prev;
                    File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);
                }
                var rng = new LiveGenerator.Rng(7);
                void HumanMove(bool preferAttack)
                {
                    var m = view.Match;
                    var moves = m.LegalMoves(KatKat.UI.LiveMatchView.Human);
                    LiveMove pick = default; bool found = false;
                    foreach (var mv in moves)
                    {
                        if (preferAttack && mv.Side != LiveSide.Opponent) continue;
                        if (!found || mv.Gain > pick.Gain) { pick = mv; found = true; }
                    }
                    if (!found) { var cmd = LiveBots.Choose(m, LiveBotKind.Greedy, rng); if (cmd.Type != LiveCommandType.Swap) { m.Submit(LiveCommand.Pass(0, m.NextSeq(0))); return; } view.SelectSlot(cmd.HandSlot); view.TapCell(cmd.Side, cmd.X, cmd.Y); }
                    else
                    {
                        var b = m.Boards[pick.Side == LiveSide.Own ? 0 : 1];
                        view.SelectSlot(pick.Slot);
                        view.TapCell(pick.Side, b.X(pick.Target), b.Y(pick.Target));
                    }
                    view.CompleteAnimations();
                }

                view.NewMatch(0, 0);
                Shot("01_acilis");
                var m0 = view.Match;
                var best = default(LiveMove); bool any = false;
                foreach (var mv in m0.LegalMoves(0)) if (!any || mv.Gain > best.Gain) { best = mv; any = true; }
                view.SelectSlot(best.Slot);
                Shot("02_el_tasi_secili");
                var b0 = m0.Boards[0];
                view.TapCell(LiveSide.Own, b0.X(best.Target), b0.Y(best.Target));
                view.AdvanceAnimations(0.6f);
                Shot("03a_parcalar_skora_ucuyor_1");
                view.AdvanceAnimations(0.35f);
                Shot("03a_parcalar_skora_ucuyor_2");
                view.AdvanceAnimations(0.35f);
                Shot("03a_parcalar_skora_ucuyor_3");
                view.CompleteAnimations();
                Shot("03_patlama_sonrasi_sira_rakipte");
                view.BotMove();
                view.CompleteAnimations();
                Shot("04_botun_hamlesi");
                HumanMove(true);
                Shot("05_rakibe_saldiri");
                bool tapShot = false;
                for (int k = 0; k < 40 && !view.Match.IsOver; k++)
                {
                    if (view.Match.Current == 1) { view.BotMove(); view.CompleteAnimations(); continue; }
                    if (!tapShot)
                    {
                        var m = view.Match;
                        LiveMove tap = default; bool has = false;
                        foreach (var mv in m.LegalMoves(0)) if (mv.Type == LiveCommandType.TapGroup && (!has || mv.Gain > tap.Gain)) { tap = mv; has = true; }
                        if (has)
                        {
                            Shot("07_hazir_grup_dokunmayi_bekliyor");
                            var b = m.Boards[0];
                            view.TapCell(LiveSide.Own, b.X(tap.Target), b.Y(tap.Target));
                            view.AdvanceAnimations(0.3f);
                            Shot("08_dokunarak_patlatma_parcalar_1");
                            view.AdvanceAnimations(0.3f);
                            Shot("08_dokunarak_patlatma_parcalar_2");
                            view.CompleteAnimations();
                            Shot("09_dokunarak_patlatma_sonrasi");
                            tapShot = true;
                            continue;
                        }
                    }
                    HumanMove(false);
                }
                Shot("06_mac_sonu");
                Debug.Log("[KatKat] Canlı maç ekranları → " + shotDir + (tapShot ? "" : " (hazır grup çıkmadı; 07–09 yok)"));
            }
            finally
            {
                KatKat.UI.UiAspect.Override = null;
                KatKat.UI.SafeAreaFitter.NormalizedOverride = null;
                if (view != null) UnityEngine.Object.DestroyImmediate(view.gameObject);
                UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release();
            }
        }

        /// <summary>Toplu çalıştırma: havuzu üret, sonra ölç.</summary>
        public static void RunBatch()
        {
            try
            {
                GeneratePool();
                RunReport();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[KatKat] Canlı maç aracı hata: " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}
