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
        private static LiveRules Variant(out string poolPath, out string reportPath)
        {
            var rules = new LiveRules();
            poolPath = PoolPath;
            reportPath = ReportPath;
            // Farklı ama denk eller (kullanıcı 2026-11-13): KATKAT_LIVE_HANDS=diff. 6×6 havuzu oyuna girer (ekrandaki "El" düğmesi).
            bool diff = Environment.GetEnvironmentVariable("KATKAT_LIVE_HANDS") == "diff";
            rules.DifferentHands = diff;
            if (diff)
            {
                poolPath = DiffPoolPath;
                reportPath = "docs/vr1/canli_mac/OLCUM_RAPORU_farkli_el.md";
            }
            if (int.TryParse(Environment.GetEnvironmentVariable("KATKAT_LIVE_SIZE"), out int n) && n >= 3 && n != rules.Width)
            {
                rules.Width = rules.Height = n;
                string tag = diff ? "_farkli_el" : "";
                poolPath = $"docs/vr1/canli_mac/havuz_{n}x{n}{tag}_v{(diff ? 2 : 1)}.txt";
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
            sb.Append($"# gen={(rules.DifferentHands ? LiveGenerator.VersionDifferentHands : LiveGenerator.Version)} seed={PoolSeed} count={pool.Count} kurallar=[{rules}]\n");
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
            sb.Append($"Üretildi: {DateTime.Now:yyyy-MM-dd HH:mm} · `LiveMatchTools.RunReport` (tools/unity.ps1 live-report) · havuz `{PoolPath}` (gen {(baseRules.DifferentHands ? LiveGenerator.VersionDifferentHands : LiveGenerator.Version)}, tohum {PoolSeed})\n\n");
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
                sb.Append('\n');
            }
            sw.Stop();
            sb.Append($"Ölçüm süresi: {sw.ElapsedMilliseconds} ms.\n");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[KatKat] Canlı maç ölçüm raporu → " + ReportPath);
        }

        public const string ScenePath = "Assets/KatKat/Scenes/LiveMatch.unity";
        public const string ShotDir = "docs/vr1/canli_mac/ekranlar";

        /// <summary>Tek cihaz prototipi: LiveMatch sahnesini (yoksa kurar) açar ve oynatır. Mağaza derlemesinde sahne yok.</summary>
        [MenuItem("Kat Kat/Canlı maç/Prototipi oyna")]
        public static void Play()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
                var camGo = new UnityEngine.GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<UnityEngine.Camera>();
                cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
                cam.backgroundColor = new UnityEngine.Color32(0x16, 0x2A, 0x33, 0xFF);
                cam.orthographic = true;
                new UnityEngine.GameObject("LiveMatch").AddComponent<KatKat.UI.LiveMatchScreen>();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Prototip ekran görüntüleri (toplu): açılış, el seçili, puan önizleme, saldırı önizleme, dokunma açık, maç sonu.</summary>
        public static void CaptureBatch()
        {
            int code = 0;
            try { Capture(); }
            catch (Exception e) { Debug.LogError("[KatKat] Canlı maç yakalama hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        [MenuItem("Kat Kat/Canlı maç/Ekran görüntüleri")]
        public static void Capture()
        {
            const int w = 1080, h = 1920;
            KatKat.UI.UiAspect.Override = (float)w / h;
            var camGo = new UnityEngine.GameObject("LiveCam");
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
            cam.backgroundColor = UnityEngine.Color.black;
            cam.orthographic = true;
            cam.transform.position = new UnityEngine.Vector3(0, 0, -10);
            var rt = new UnityEngine.RenderTexture(w, h, 24, UnityEngine.RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = (float)w / h;
            var go = new UnityEngine.GameObject("LiveMatchCapture");
            try
            {
                var screen = go.AddComponent<KatKat.UI.LiveMatchScreen>();
                screen.Build(cam);
                go.GetComponentInChildren<UnityEngine.Canvas>().scaleFactor = 1f;
                Directory.CreateDirectory(ShotDir);
                void Shot(string name)
                {
                    screen.Refresh();
                    UnityEngine.Canvas.ForceUpdateCanvases();
                    cam.Render();
                    var prev = UnityEngine.RenderTexture.active;
                    UnityEngine.RenderTexture.active = rt;
                    var tex = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGB24, false);
                    tex.ReadPixels(new UnityEngine.Rect(0, 0, w, h), 0, 0);
                    tex.Apply();
                    UnityEngine.RenderTexture.active = prev;
                    File.WriteAllBytes(Path.Combine(ShotDir, name + ".png"), tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);
                }

                screen.NewMatch(0, 0);
                Shot("01_acilis");
                var m = screen.Match;
                var opps = LiveMoves.Opportunities(m.Boards[0], m.Hands[0]);
                var best = opps[0];
                foreach (var o in opps) if (o.Group.Count > best.Group.Count) best = o;
                int slot = m.Hands[0].IndexOf(best.Color);
                screen.SelectSlot(slot);
                Shot("02_el_secili");
                screen.TapCell(LiveSide.Own, best.Target);
                Shot("03_puan_onizleme");
                screen.TapCell(LiveSide.Own, best.Target); // onay: P1 puan alır, sıra P2
                Shot("04_puan_sonrasi_sira_p2");
                var m2 = screen.Match;
                var mv = m2.LegalMoves(m2.Current).Find(x => x.Side == LiveSide.Opponent);
                screen.SelectSlot(mv.Slot);
                screen.TapCell(LiveSide.Opponent, mv.Target);
                Shot("05_saldiri_onizleme");

                // Dokunma açık: botlarla hazır grup çıkana kadar oyna.
                screen.Rules.TapReadyGroups = true;
                screen.NewMatch(3, 0);
                var rng = new LiveGenerator.Rng(5);
                for (int k = 0; k < 10 && !LiveMoves.HasReadyGroup(screen.Match.Boards[screen.Match.Current]); k++)
                    screen.Match.Submit(LiveBots.Choose(screen.Match, LiveBotKind.Greedy, rng));
                Shot("06_dokunma_acik_hazir_grup");
                while (!screen.Match.IsOver) screen.Match.Submit(LiveBots.Choose(screen.Match, LiveBotKind.Balanced, rng));
                Shot("07_mac_sonu");
                screen.Rules.TapReadyGroups = false;
                // Farklı ama denk eller (havuz Resources'ta varsa).
                screen.ToggleHands();
                if (screen.Match != null && screen.Match.Layout.DifferentHands) Shot("08_farkli_eller");
                screen.ToggleHands();
                Debug.Log("[KatKat] Canlı maç ekranları → " + ShotDir);
            }
            finally
            {
                KatKat.UI.UiAspect.Override = null;
                UnityEngine.Object.DestroyImmediate(go);
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
