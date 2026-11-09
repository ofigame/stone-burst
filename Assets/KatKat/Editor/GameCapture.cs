using System;
using System.Collections.Generic;
using System.IO;
using KatKat.Game;
using KatKat.UI;
using KatKat.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Oyun ve menü ekranlarını gerçek render ile farklı oranlarda yakalar (docs/captures/game_*.png).
    /// Zaman elle ilerletilir; sahne oynatılmadan GameController doğrudan kurulur, gerçek kayda yazılmaz.
    /// Batch: -executeMethod KatKat.EditorTools.GameCapture.CaptureAll
    /// </summary>
    public static class GameCapture
    {
        private static readonly Rect Notch = new Rect(0f, 0.02f, 1f, 0.94f);

        /// <summary>Görsel Şartname v3 kanıtları (docs/captures/visual-v3). Batch: -executeMethod KatKat.EditorTools.GameCapture.CaptureVisualV3</summary>
        [MenuItem("Kat Kat/Yakalama/Görsel v3 kanıtları")]
        public static void CaptureVisualV3()
        {
            const string V = "visual-v3/";
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "visual-v3"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // V05–V07: aynı tahta durumu (bölüm 20) üç temada; tema bölüm numarasından (1/101/201).
            var probe = Probe();
            var same = probe.Catalog.Levels[19];
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            Shot(V + "coast_game_1080x1920.png", 1080, 1920, null, gc => gc.StartCustom(same, 0));
            Shot(V + "garden_game_1080x1920.png", 1080, 1920, null, gc => gc.StartCustom(same, 100));
            Shot(V + "desert_game_1080x1920.png", 1080, 1920, null, gc => gc.StartCustom(same, 200));
            // V19: art arda tema değişimi ve geri dönüş aynı GameController üzerinde (renk sızıntısı yok).
            Shot(V + "theme_cycle_back_to_coast_1080x1920.png", 1080, 1920, null, gc => { gc.StartCustom(same, 200); gc.StartCustom(same, 100); gc.StartCustom(same, 0); });
            Shot(V + "coast_game_1080x2400_notch.png", 1080, 2400, Notch, gc => gc.StartCustom(same, 0));
            Shot(V + "desert_game_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartCustom(same, 200));
            Shot(V + "coast_6x6_l52_1080x1920.png", 1080, 1920, null, gc => gc.StartLevel(51));
            Shot(V + "garden_ice_l105_1080x1920.png", 1080, 1920, null, gc => gc.StartLevel(104));
            Shot(V + "desert_envelope_l221_1080x1920.png", 1080, 1920, null, gc => gc.StartLevel(220));

            // V08–V09: alt düğme durumları (bölüm 11: ilk hamle ruloyu kazandırır).
            Shot(V + "buttons_roll_empty_undo_disabled.png", 1080, 1920, null, gc => gc.StartLevel(10), Bottom);
            Shot(V + "buttons_roll_earned_pop_100ms.png", 1080, 1920, null, gc => { gc.StartLevel(10); Steps(gc, 1, -1f); gc.Hud.PulseRoll(); gc.Hud.AdvanceButtons(100f); }, Bottom);
            Shot(V + "buttons_ready_native.png", 1080, 1920, null, gc => { gc.StartLevel(10); Steps(gc, 1, -1f); }, Bottom);
            Shot(V + "buttons_roll_targeting.png", 1080, 1920, null, gc => { gc.StartLevel(10); Steps(gc, 1, -1f); gc.PressRollSlot(); gc.OnCellPressed(0, 2); }, Bottom);
            Shot(V + "buttons_undo_kick_85ms.png", 1080, 1920, null, gc => { gc.StartLevel(10); Steps(gc, 1, -1f); gc.Hud.KickUndoForCapture(); gc.Hud.AdvanceButtons(85f); }, Bottom);
            Shot(V + "buttons_roll_used_110ms.png", 1080, 1920, null, gc => { gc.StartLevel(10); Steps(gc, 1, -1f); gc.Hud.PlayRollUsed(); gc.Hud.AdvanceButtons(110f); }, Bottom);

            // V10–V13: zafer kartı ve gerçek sonraki bölüm önizlemesi.
            Shot(V + "next_preview_level_A_l2_to_l3.png", 1080, 1920, null, gc => { gc.StartLevel(1); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            Shot(V + "next_preview_level_B_l51_to_l52_6x6.png", 1080, 1920, null, gc => { gc.StartLevel(50); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            Shot(V + "next_preview_level_C_l120_to_l121_lock.png", 1080, 1920, null, gc => { gc.StartLevel(119); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            Shot(V + "boundary_100_101.png", 1080, 1920, null, gc => { gc.StartLevel(99); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            Shot(V + "boundary_200_201.png", 1080, 1920, null, gc => { gc.StartLevel(199); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            Shot(V + "last_available_level.png", 1080, 1920, null, gc => { gc.StartLevel(gc.Catalog.Count - 1); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            foreach (var ms in new[] { 120f, 430f, 900f })
            {
                float at = ms;
                Shot(V + $"win_intro_{(int)at}ms.png", 1080, 1920, null, gc => { gc.StartLevel(1); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(at); });
            }
            Shot(V + "win_1080x2400_notch.png", 1080, 2400, Notch, gc => { gc.StartLevel(1); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            Shot(V + "win_tablet_1536x2048.png", 1536, 2048, null, gc => { gc.StartLevel(1); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            // V16: Türkçe glifler ve büyük bölüm sayısı canlı metin olarak.
            Shot(V + "glyphs_turkish_1080x1920.png", 1080, 1920, null, gc => { gc.StartLevel(gc.Catalog.Count - 1); gc.Hud.ShowTutorial("ÇĞİÖŞÜ çğıöşü • Seviye 1000 • Hamle: 24"); gc.Hud.ShowToast("Yeni bölge: Çöl • Bahçe • Sahil"); });
            // Tema geçişi (v3 §11): 100 → 101; yeni dünya kurulur, perde 320 ms'de açılır.
            foreach (var ms in new[] { 0f, 160f, 320f })
            {
                float at = ms;
                Shot(V + $"theme_transition_100_to_101_{(int)at}ms.png", 1080, 1920, null, gc => { gc.StartLevel(99); gc.StartLevel(100); gc.Hud.AdvanceVeil(at); });
            }
            // V01–V04: ana ekran ve bölüm haritası üç temada (devam bölümü / sayfa ilk bölümüne göre).
            Shot(V + "coast_home_1080x1920.png", 1080, 1920, null, gc => gc.Menu.ShowHome(24, 24, gc.Catalog.Count, () => { }, () => { }, () => { }));
            Shot(V + "garden_home_1080x1920.png", 1080, 1920, null, gc => gc.Menu.ShowHome(124, 124, gc.Catalog.Count, () => { }, () => { }, () => { }));
            Shot(V + "desert_home_1080x1920.png", 1080, 1920, null, gc => gc.Menu.ShowHome(224, 224, gc.Catalog.Count, () => { }, () => { }, () => { }));
            Shot(V + "coast_home_1080x2400_notch.png", 1080, 2400, Notch, gc => gc.Menu.ShowHome(24, 24, gc.Catalog.Count, () => { }, () => { }, () => { }));
            Shot(V + "coast_map_1080x1920.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 24, i => i < 24, () => { }, 0));
            Shot(V + "garden_map_1080x1920.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 124, i => i < 124, () => { }, 1));
            Shot(V + "desert_map_1080x1920.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 224, i => i < 224, () => { }, 2));
            Shot(V + "desert_map_tablet_1536x2048.png", 1536, 2048, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 224, i => i < 224, () => { }, 2));
            // Kesintisiz harita: bölge tabelası (100→101), yolun sonu, çentikli ekran; ayarlar, duraklatma, dört hedefli başlık.
            Shot(V + "map_region_sign_100_101.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 100, i => i < 100, () => { }, 1));
            // Mevcut bölüm (25) görünüm dışında: "bölümüme dön" maskot düğmesi alt menünün üstünde (aşağı ok).
            Shot(V + "map_return_button.png", 1080, 1920, null, gc => { gc.Menu.ShowLevels(gc.Catalog.Count, 24, i => i < 24, () => { }, 2); for (int k = 0; k < 20; k++) gc.Menu.Tick(0.02f); });
            Shot(V + "map_road_end.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, gc.Catalog.Count - 1, i => i < gc.Catalog.Count - 1, () => { }));
            Shot(V + "map_1080x2400_notch.png", 1080, 2400, Notch, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 224, i => i < 224, () => { }));
            Shot(V + "settings_1080x1920.png", 1080, 1920, null, gc => gc.Menu.ShowSettings(true, false, () => { }));
            Shot(V + "pause_panel_1080x1920.png", 1080, 1920, null, gc => { gc.StartLevel(19); gc.OpenPauseForCapture(); gc.Hud.AdvancePanel(1000f); });
            Shot(V + "hud_four_goals_l340.png", 1080, 1920, null, gc => gc.StartLevel(339));
            Shot(V + "lose_panel_garden.png", 1080, 1920, null, gc => { gc.StartCustom(LoseBoard(), 103); LoseQuickly(gc); gc.Hud.AdvancePanel(1000f); });
            // V15: ölçülmüş varlık boyutu raporu (oyun + zafer kartı + ana ekran, 1080×1920).
            var rows = new System.Text.StringBuilder();
            Shot(V + "size_report_game.png", 1080, 1920, null, gc => { gc.StartLevel(10); Steps(gc, 1, -1f); Measure(gc, rows, "oyun"); });
            Shot(V + "size_report_win.png", 1080, 1920, null, gc => { gc.StartLevel(1); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); Measure(gc, rows, "zafer"); });
            Shot(V + "size_report_home.png", 1080, 1920, null, gc => { gc.Menu.ShowHome(24, 24, gc.Catalog.Count, () => { }, () => { }, () => { }); Measure(gc, rows, "ana"); });
            File.WriteAllText(Path.Combine(PeelCapture.OutDir, "visual-v3", "SIZE_REPORT.md"),
                "# Ölçülmüş varlık boyutları (1080×1920, editör toplu yakalama)\n\n" +
                "Kaynak içerik ve kırpım: `V4_IMPORT_SIZES.md`. Burada çalışma zamanı doku boyutu ve ekranda kapladığı gerçek piksel; " +
                "`ekran/doku` 1'in altındaysa doku ekranda küçültülür (ayrıntı yeterli), üstündeyse büyütülür. Sıkıştırma: CompressedHQ (masaüstü BC7); " +
                "Android ASTC karşılaştırması cihazda yapılmadı.\n\n" +
                "| Ekran | Nesne | Sprite | Doku (çalışma zamanı) | Ekranda (px) | ekran/doku | En büyük anim. ölçeği |\n|---|---|---|---|---|---|---|\n" + rows);
            // v5 çoklu çözünürlük taşma taraması: küçük telefon, 16:10 tablet (Huawei), büyük tablet.
            foreach (var (rw, rh, tag) in new[] { (720, 1280, "720x1280"), (1200, 1920, "1200x1920"), (2048, 2732, "2048x2732") })
            {
                int ww = rw, hh = rh;
                Shot(V + $"res_{tag}_map.png", ww, hh, null, gc => gc.ShowHome());
                Shot(V + $"res_{tag}_game.png", ww, hh, null, gc => gc.StartLevel(Math.Min(339, gc.Catalog.Count - 1)));
                Shot(V + $"res_{tag}_win.png", ww, hh, null, gc => { gc.StartLevel(1); Steps(gc, 99, -1f); Pump(gc, 60000f); gc.Hud.AdvancePanel(2400f); });
                Shot(V + $"res_{tag}_lose.png", ww, hh, null, gc => { gc.StartCustom(LoseBoard(), 103); LoseQuickly(gc); gc.Hud.AdvancePanel(1000f); });
                Shot(V + $"res_{tag}_settings.png", ww, hh, null, gc => gc.Menu.ShowSettings(true, true, () => { }));
            }
            WriteOverflowReport("visual-v3 + çoklu çözünürlük");
            SafeAreaFitter.NormalizedOverride = null;
            Debug.Log("[KatKat] Görsel v3 yakalama tamam → " + Path.GetFullPath(Path.Combine(PeelCapture.OutDir, "visual-v3")));
        }

        /// <summary>Etkin UI Image ve dünya SpriteRenderer'larının ekranda kapladığı pikseli doku boyutuyla karşılaştırır.</summary>
        private static void Measure(GameController gc, System.Text.StringBuilder rows, string screen)
        {
            Canvas.ForceUpdateCanvases();
            var seen = new System.Collections.Generic.HashSet<string>();
            var cam = gc.Hud.Canvas.worldCamera;
            void Row(string obj, Sprite sp, float w, float h, string anim)
            {
                if (sp == null || sp.texture == null) return;
                string key = screen + sp.name + obj;
                if (!seen.Add(key)) return;
                float ratio = Mathf.Max(w / sp.rect.width, h / sp.rect.height);
                rows.AppendLine($"| {screen} | {obj} | {sp.name} | {sp.texture.width}×{sp.texture.height} | {w:0}×{h:0} | {ratio:0.00} | {anim} |");
            }
            foreach (var img in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Exclude))
            {
                // Kod üretimi dokular (kutu, daire, ikon ailesi) hariç: yalnızca dosyadan gelen varlıklar ölçülür.
                if (img.sprite == null || string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(img.sprite))) continue;
                // Tuval birimi × ölçek faktörü = ekran pikseli (iç içe ölçekler dahil).
                var canvas = img.canvas.rootCanvas;
                float k = canvas.scaleFactor * img.rectTransform.lossyScale.x / canvas.transform.lossyScale.x;
                var r = img.rectTransform.rect;
                if (img.preserveAspect && img.sprite.rect.height > 0f)
                {
                    float aspect = img.sprite.rect.width / img.sprite.rect.height;
                    if (r.width / r.height > aspect) r.width = r.height * aspect; else r.height = r.width / aspect;
                }
                string anim = img.name == "Icon" ? "1.10" : img.name == "Medal" || img.name == "Check" ? "1.14" : img.GetComponent<UnityEngine.UI.Button>() != null ? "1.02" : "1.00";
                Row(img.name, img.sprite, r.width * k, r.height * k, anim);
            }
            foreach (var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))
            {
                if (sr.sprite == null) continue;
                var bnd = sr.bounds;
                Vector2 a = cam.WorldToScreenPoint(bnd.min), b = cam.WorldToScreenPoint(bnd.max);
                Row(sr.name, sr.sprite, Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), "1.00");
            }
        }

        /// <summary>Alt düğmelerin yakın planı (1080x1920'de alt 360 px).</summary>
        private static readonly RectInt Bottom = new RectInt(0, 0, 1080, 360);

        private static GameController Probe()
        {
            var go = new GameObject("Probe");
            var cam = new GameObject("ProbeCam").AddComponent<Camera>();
            cam.transform.SetParent(go.transform);
            var gc = go.AddComponent<GameController>();
            gc.Setup(cam, new KatKat.Save.SaveService(Path.Combine(Path.GetTempPath(), "katkat_capture_probe")));
            return gc;
        }

        /// <summary>Kayıp kartı için bölüm 4'ün tahtası (bahçe temasında gösterilir).</summary>
        private static KatKat.Levels.LevelData LoseBoard()
        {
            var p = Probe();
            var d = p.Catalog.Levels[3];
            UnityEngine.Object.DestroyImmediate(p.gameObject);
            return d;
        }

        [MenuItem("Kat Kat/Yakalama/Oyun ekranları")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(PeelCapture.OutDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shot("game_l1_9x16_1080x1920.png", 1080, 1920, null, gc => gc.StartLevel(0));
            Shot("game_l2_20x9_notch_1080x2400.png", 1080, 2400, Notch, gc => gc.StartLevel(1));
            Shot("game_l3_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartLevel(2));
            Shot("game_l2_chain_mid_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(1); Steps(gc, 1, 470f); });
            Shot("game_l4_after_first_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(3); Steps(gc, 1, -1f); });
            Shot("game_l1_win_panel_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(0); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            // Premium-v2: zafer kartı giriş akışı anları, kayıp ve duraklatma kartları, hedef parlaması.
            foreach (var ms in new[] { 240f, 520f, 900f })
            {
                float at = ms;
                Shot($"game_l2_win_intro_{(int)at}ms_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(1); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(at); });
            }
            Shot("game_l2_win_20x9_notch.png", 1080, 2400, Notch, gc => { gc.StartLevel(1); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            Shot("game_l20_win_tablet_1536x2048.png", 1536, 2048, null, gc => { gc.StartLevel(19); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            Shot("game_l4_lose_panel_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(3); LoseQuickly(gc); gc.Hud.AdvancePanel(1000f); });
            Shot("game_l20_pause_panel_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(19); gc.OpenPauseForCapture(); gc.Hud.AdvancePanel(1000f); });
            Shot("game_l3_goal_flash_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(2); Steps(gc, 1, PeelSequence.ReleaseEnd + 120f); });
            Shot("game_l11_roll_ready_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(10); Steps(gc, 1, -1f); });
            Shot("game_l11_roll_preview_20x9_notch.png", 1080, 2400, Notch, gc => { gc.StartLevel(10); Steps(gc, 1, -1f); gc.PressRollSlot(); gc.OnCellPressed(0, 2); });
            Shot("game_l11_roll_mid_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(10); Steps(gc, 2, 300f); });
            Shot("game_l20_9x16.png", 1080, 1920, null, gc => gc.StartLevel(19));
            Shot("game_l20_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartLevel(19));
            // Faz 5: ataş, yıldız, 6x6 ve dört katman.
            Shot("game_l21_clip_9x16.png", 1080, 1920, null, gc => gc.StartLevel(20));
            Shot("game_l21_clip_after2_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(20); Steps(gc, 2, -1f); });
            Shot("game_l24_clips_9x16.png", 1080, 1920, null, gc => gc.StartLevel(23));
            Shot("game_l41_star_9x16.png", 1080, 1920, null, gc => gc.StartLevel(40));
            for (int s = 1; s <= 3; s++)
            {
                int step = s;
                Shot($"game_l41_star_step{step}_450ms.png", 1080, 1920, null, gc => { gc.StartLevel(40); Steps(gc, step, 450f); });
            }
            Shot("game_l45_holes_9x16.png", 1080, 1920, null, gc => gc.StartLevel(44));
            Shot("game_l52_6x6_9x16.png", 1080, 1920, null, gc => gc.StartLevel(51));
            Shot("game_l60_20x9_notch.png", 1080, 2400, Notch, gc => gc.StartLevel(59));
            Shot("game_l60_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartLevel(59));
            Shot("menu_levels_sheet3_9x16.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 45, i => i < 40, () => { }, 0));
            // Premium-v2 Aşama B: buz ve renk kilidi (katalog dışı önizleme tahtası + 61+ bölümler).
            var lockPreview = KatKat.Levels.LevelParser.Parse(File.ReadAllText("Assets/KatKat/Editor/Fixtures/lock_preview.json"), "lock_preview");
            Shot("game_lock_ice_preview_9x16.png", 1080, 1920, null, gc => gc.StartCustom(lockPreview, 120));
            Shot("game_lock_ice_preview_hit_330ms.png", 1080, 1920, null, gc => { gc.StartCustom(lockPreview, 120); Steps(gc, 1, 330f); });
            Shot("game_lock_ice_preview_after.png", 1080, 1920, null, gc => { gc.StartCustom(lockPreview, 120); Steps(gc, 1, -1f); });
            if (System.IO.File.Exists("Assets/KatKat/Resources/Levels/level_061.json"))
            {
                Shot("game_l61_ice_9x16.png", 1080, 1920, null, gc => gc.StartLevel(60));
                Shot("game_l61_ice_after1_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(60); Steps(gc, 1, -1f); });
                Shot("game_l69_ice_9x16.png", 1080, 1920, null, gc => gc.StartLevel(68));
                Shot("game_l80_ice_20x9_notch.png", 1080, 2400, Notch, gc => gc.StartLevel(79));
            }
            if (System.IO.File.Exists("Assets/KatKat/Resources/Levels/level_120.json"))
            {
                Shot("game_l81_star_path_9x16.png", 1080, 1920, null, gc => gc.StartLevel(80));
                Shot("game_l91_ice_clip_9x16.png", 1080, 1920, null, gc => gc.StartLevel(90));
                Shot("game_l100_9x16.png", 1080, 1920, null, gc => gc.StartLevel(99));
                Shot("game_l105_four_layers_9x16.png", 1080, 1920, null, gc => gc.StartLevel(104));
                Shot("game_l110_20x9_notch.png", 1080, 2400, Notch, gc => gc.StartLevel(109));
                Shot("game_l120_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartLevel(119));
                Shot("game_l120_win_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(119); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
                Shot("menu_levels_sheet6_9x16.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 119, i => i < 100, () => { }, 1));
            }
            if (System.IO.File.Exists("Assets/KatKat/Resources/Levels/level_160.json"))
            {
                Shot("game_l121_lock_9x16.png", 1080, 1920, null, gc => gc.StartLevel(120));
                // İlk hamle sarıyı soyar, alttan açılan mercanlar zincirle kalkar: kilit izleri zincir dalgasında (~360–600 ms).
                Shot("game_l121_lock_streak_480ms.png", 1080, 1920, null, gc => { gc.StartLevel(120); Steps(gc, 1, 480f); });
                Shot("game_l121_lock_after1_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(120); Steps(gc, 1, -1f); });
                Shot("game_l131_lock_other_9x16.png", 1080, 1920, null, gc => gc.StartLevel(130));
                Shot("game_l141_two_locks_9x16.png", 1080, 1920, null, gc => gc.StartLevel(140));
                Shot("game_l151_lock_ice_9x16.png", 1080, 1920, null, gc => gc.StartLevel(150));
                Shot("game_l150_20x9_notch.png", 1080, 2400, Notch, gc => gc.StartLevel(149));
                Shot("game_l160_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartLevel(159));
                Shot("game_l160_win_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(159); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            }
            if (System.IO.File.Exists("Assets/KatKat/Resources/Levels/level_200.json"))
            {
                Shot("game_l161_lock_star_9x16.png", 1080, 1920, null, gc => gc.StartLevel(160));
                Shot("game_l177_four_layer_locks_9x16.png", 1080, 1920, null, gc => gc.StartLevel(176));
                Shot("game_l181_lock_clip_9x16.png", 1080, 1920, null, gc => gc.StartLevel(180));
                Shot("game_l190_20x9_notch.png", 1080, 2400, Notch, gc => gc.StartLevel(189));
                Shot("game_l200_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartLevel(199));
                Shot("game_l200_win_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(199); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
                Shot("menu_levels_sheet10_9x16.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 199, i => i < 190, () => { }, 1));
            }
            if (System.IO.File.Exists("Assets/KatKat/Resources/Levels/level_240.json"))
            {
                Shot("game_l201_envelope_9x16.png", 1080, 1920, null, gc => gc.StartLevel(200));
                for (int k = 0; k < 3; k++)
                {
                    float at = new[] { 260f, 420f, 640f }[k];
                    Shot($"game_l201_envelope_open_{(int)at}ms.png", 1080, 1920, null, gc => { gc.StartLevel(200); StepsUntilEnvelope(gc, at); });
                }
                Shot("game_l221_two_seals_9x16.png", 1080, 1920, null, gc => gc.StartLevel(220));
                Shot("game_l230_three_seals_20x9_notch.png", 1080, 2400, Notch, gc => gc.StartLevel(229));
                Shot("game_l232_envelope_lock_9x16.png", 1080, 1920, null, gc => gc.StartLevel(231));
                Shot("game_l240_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartLevel(239));
                Shot("game_l240_win_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(239); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            }
            if (System.IO.File.Exists("Assets/KatKat/Resources/Levels/level_300.json"))
            {
                Shot("game_l249_three_seals_stars_9x16.png", 1080, 1920, null, gc => gc.StartLevel(248));
                Shot("game_l259_four_layers_20x9_notch.png", 1080, 2400, Notch, gc => gc.StartLevel(258));
                Shot("game_l271_envelope_lock_9x16.png", 1080, 1920, null, gc => gc.StartLevel(270));
                Shot("game_l283_envelope_ice_9x16.png", 1080, 1920, null, gc => gc.StartLevel(282));
                Shot("game_l300_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartLevel(299));
                Shot("game_l300_win_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(299); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
                Shot("menu_levels_sheet15_9x16.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 299, i => i < 290, () => { }, 2));
            }
            if (System.IO.File.Exists("Assets/KatKat/Resources/Levels/level_340.json"))
            {
                Shot("game_l301_rotor_9x16.png", 1080, 1920, null, gc => gc.StartLevel(300));
                foreach (var ms in new[] { 120f, 210f, 330f })
                {
                    float at = ms;
                    Shot($"game_l301_rotate_{(int)at}ms.png", 1080, 1920, null, gc => { gc.StartLevel(300); StepsUntilRotate(gc, at); });
                }
                Shot("game_l301_rotate_after.png", 1080, 1920, null, gc => { gc.StartLevel(300); StepsUntilRotate(gc, -1f); });
                Shot("game_l321_two_rotors_9x16.png", 1080, 1920, null, gc => gc.StartLevel(320));
                Shot("game_l330_20x9_notch.png", 1080, 2400, Notch, gc => gc.StartLevel(329));
                Shot("game_l339_rotor_covers_9x16.png", 1080, 1920, null, gc => gc.StartLevel(338));
                Shot("game_l340_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartLevel(339));
                Shot("game_l340_win_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(339); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
            }
            if (System.IO.File.Exists("Assets/KatKat/Resources/Levels/level_400.json"))
            {
                Shot("game_l345_rotor_four_layers_9x16.png", 1080, 1920, null, gc => gc.StartLevel(344));
                Shot("game_l361_rotor_lock_9x16.png", 1080, 1920, null, gc => gc.StartLevel(360));
                Shot("game_l371_rotor_envelope_9x16.png", 1080, 1920, null, gc => gc.StartLevel(370));
                Shot("game_l389_rotor_ice_clip_20x9_notch.png", 1080, 2400, Notch, gc => gc.StartLevel(388));
                Shot("game_l400_tablet_1536x2048.png", 1536, 2048, null, gc => gc.StartLevel(399));
                Shot("game_l400_win_9x16.png", 1080, 1920, null, gc => { gc.StartLevel(399); Steps(gc, 99, -1f); gc.Hud.AdvancePanel(2400f); });
                Shot("menu_levels_sheet20_9x16.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 399, i => i < 390, () => { }, 3));
            }
            Shot("menu_home_9x16.png", 1080, 1920, null, gc => gc.ShowHome());
            Shot("menu_levels_20x9_notch.png", 1080, 2400, Notch, gc => gc.ShowLevels());
            Shot("menu_levels_tablet_1536x2048.png", 1536, 2048, null, gc => gc.ShowLevels());
            SafeAreaFitter.NormalizedOverride = null;
            Debug.Log("[KatKat] GameCapture tamam → " + Path.GetFullPath(PeelCapture.OutDir));
        }

        /// <summary>Kayıtlı çözümden n adım oynar; son adımdan sonra afterMs kadar (negatifse sonuna kadar) ilerletir.</summary>
        /// <summary>
        /// v5 deneme bölümleri (Editor/Fixtures/v5): özel taş doğuşu, roket/gökkuşağı ateşleme anları, övgü yazısı,
        /// puan çubuğu, bitiş kutlaması ve yıldızlı zafer kartı. Çıktı docs/captures/v5/.
        /// </summary>
        public static void CaptureV5()
        {
            const string V = "v5/";
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v5"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var files = Directory.GetFiles("Assets/KatKat/Editor/Fixtures/v5", "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            var report = new System.Text.StringBuilder("# v5 yakalama raporu\n\n");
            foreach (var f in files)
            {
                var d = KatKat.Levels.LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f));
                int num = KatKat.Levels.LevelValidator.LevelNumber(d.Id);
                string n = num.ToString("000");
                Shot(V + $"l{n}_start.png", 1080, 1920, null, gc => gc.StartCustom(d, num - 1));
                // Kayıtlı çözümde ilk özel taş doğuşu ve ilk ateşleme anı (dalga zaman çizgisi üzerinden).
                int create = -1, fire = -1;
                {
                    var s = KatKat.Levels.LevelLoader.ToState(d);
                    for (int i = 0; i < d.Solution.Count; i++)
                    {
                        var r = KatKat.Core.RulesEngine.TryApply(s, KatKat.Levels.LevelLoader.ToCommand(d.Solution[i]));
                        if (create < 0 && r.SpecialCreated != KatKat.Core.SpecialKind.None) create = i;
                        if (fire < 0 && r.Waves.Count > 0 && r.Waves[0].Kind == KatKat.Core.WaveKind.Special) fire = i;
                    }
                    report.AppendLine($"- {d.Id}: {d.Width}×{d.Height}, hamle {d.MoveLimit}, çözüm {d.Solution.Count}, ilk doğuş adımı {create + 1}, ilk ateşleme adımı {fire + 1}, yıldız eşikleri {(d.StarThresholds == null ? "-" : d.StarThresholds[0] + "/" + d.StarThresholds[1])}");
                }
                if (create >= 0)
                {
                    int c = create;
                    Shot(V + $"l{n}_create_{c + 1}_640ms.png", 1080, 1920, null, gc => { gc.StartCustom(d, num - 1); Steps(gc, c, -1f); Steps5(gc, c, 640f); });
                }
                if (fire >= 0)
                {
                    int c = fire;
                    foreach (var ms in new[] { 200f, 420f })
                    {
                        float at = ms;
                        Shot(V + $"l{n}_fire_{c + 1}_{(int)at}ms.png", 1080, 1920, null, gc => { gc.StartCustom(d, num - 1); Steps(gc, c, -1f); Steps5(gc, c, at); });
                    }
                }
                Shot(V + $"l{n}_celebration.png", 1080, 1920, null, gc => { gc.StartCustom(d, num - 1); Steps(gc, d.Solution.Count, -1f); Pump(gc, 700f); });
                Shot(V + $"l{n}_win.png", 1080, 1920, null, gc => { gc.StartCustom(d, num - 1); Steps(gc, d.Solution.Count, -1f); Pump(gc, 60000f); gc.Hud.AdvancePanel(2400f); });
            }
            File.WriteAllText(Path.Combine(PeelCapture.OutDir, "v5", "RAPOR.md"), report.ToString());
            Debug.Log("[KatKat] v5 yakalama bitti: " + files.Length + " bölüm");
        }

        /// <summary>Tek adımı oynatıp tam <paramref name="ms"/> sonra durur (tahta + HUD efektleri).</summary>
        private static void Steps5(GameController gc, int index, float ms)
        {
            gc.PlayStep(gc.CurrentLevel.Solution[index]);
            for (float t = 0f; t < ms; t += 16f) Tick(gc, 16f);
        }

        /// <summary>Kutlama dahil oynatmayı süre kadar ilerletir (faz Ended olsa da tahta oynuyor olabilir).</summary>
        private static void Pump(GameController gc, float ms)
        {
            for (float t = 0f; t < ms; t += 16f)
            {
                Tick(gc, 16f);
                if (gc.Hud.PanelOpen && t > 200f) break;
            }
        }

        private static void Tick(GameController gc, float ms)
        {
            gc.Board.Advance(ms);
            gc.Board.Director.Advance(ms);
            gc.Hud.AdvanceChips(ms);
            gc.Hud.AdvanceEffects(ms);
        }

        private static void Steps(GameController gc, int n, float afterMs)
        {
            var sol = gc.CurrentLevel.Solution;
            for (int i = 0; i < n && i < sol.Count; i++)
            {
                bool last = i == n - 1 || i == sol.Count - 1;
                gc.PlayStep(sol[i]);
                float run = last && afterMs >= 0f ? afterMs : float.MaxValue;
                for (float t = 0f; t < run && gc.CurrentPhase == GameController.Phase.Animating; t += 16f)
                {
                    gc.Board.Advance(16f);
                    gc.Board.Director.Advance(16f);
                    gc.Hud.AdvanceChips(16f);
                }
            }
        }

        /// <summary>Kayıtlı çözümü zarfı açan hamleye kadar oynar; o hamleden sonra afterMs kadar ilerletir.</summary>
        private static void StepsUntilEnvelope(GameController gc, float afterMs)
        {
            var sol = gc.CurrentLevel.Solution;
            for (int i = 0; i < sol.Count; i++)
            {
                var probe = gc.Session.State.Clone();
                var r = KatKat.Core.RulesEngine.TryApply(probe, KatKat.Levels.LevelLoader.ToCommand(sol[i]));
                bool opens = r.Waves.Exists(w => w.EnvelopeOpens.Count > 0);
                Steps1(gc, sol[i], opens ? afterMs : -1f);
                if (opens) return;
            }
        }

        /// <summary>Kayıtlı çözümü ilk döndürme adımına kadar oynar; döndürmeden sonra afterMs kadar ilerletir.</summary>
        private static void StepsUntilRotate(GameController gc, float afterMs)
        {
            foreach (var step in gc.CurrentLevel.Solution)
            {
                bool rot = step.Type == "rotate";
                Steps1(gc, step, rot ? afterMs : -1f);
                if (rot) return;
            }
        }

        private static void Steps1(GameController gc, KatKat.Levels.StepData step, float afterMs)
        {
            gc.PlayStep(step);
            float run = afterMs >= 0f ? afterMs : float.MaxValue;
            for (float t = 0f; t < run && gc.CurrentPhase == GameController.Phase.Animating; t += 16f)
            {
                gc.Board.Advance(16f);
                gc.Board.Director.Advance(16f);
                gc.Hud.AdvanceChips(16f);
            }
        }

        /// <summary>Kayıp kartı için: hamle bitene kadar geçerli ilk grubu soyar (çözüme bakmadan).</summary>
        private static void LoseQuickly(GameController gc)
        {
            for (int guard = 0; guard < 60 && gc.CurrentPhase != GameController.Phase.Ended; guard++)
            {
                var b = gc.Session.State.Board;
                bool played = false;
                for (int y = b.Height - 1; y >= 0 && !played; y--)
                for (int x = 0; x < b.Width && !played; x++)
                {
                    if (KatKat.Core.GroupFinder.Component(b, x, y).Count < KatKat.Core.GroupFinder.MinGroup) continue;
                    gc.OnCellPressed(x, y);
                    played = true;
                }
                if (!played) break;
                while (gc.CurrentPhase == GameController.Phase.Animating)
                {
                    gc.Board.Advance(16f);
                    gc.Board.Director.Advance(16f);
                }
            }
        }

        /// <summary>
        /// Kristal maskot doğrulaması (docs/Kristal-Maskot-Claude): oyun içi üst şerit (normal, yana bakış, göz kırpma,
        /// heyecan, son üç hamle), beş hedefli dar ekran, çentikli ekran, harita düğümü ve dönüş düğmesi.
        /// Her oyun içi karenin üst 430 pikseli ayrıca "_crop" olarak kaydedilir. Çıktı docs/captures/mascot/.
        /// </summary>
        public static void CaptureMascot()
        {
            const string V = "mascot/";
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "mascot"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var top = new RectInt(0, 1920 - 430, 1080, 430);
            void Hud(string name, Action<GameController> after) =>
                Shot(V + name, 1080, 1920, null, gc => { gc.StartLevel(400); after(gc); }, top);
            void Run(GameController gc, float ms) { for (float t = 0f; t < ms; t += 20f) gc.Hud.Mascot.Advance(20f); }
            Hud("hud_normal.png", gc => Run(gc, 100f));
            Hud("hud_bakis.png", gc => Run(gc, 2000f));
            Hud("hud_kirpma.png", gc => Run(gc, 2670f));
            Hud("hud_heyecan.png", gc => { Run(gc, 100f); gc.Hud.Mascot.Excite(3); Run(gc, 330f); });
            Hud("hud_son_hamleler.png", gc => { gc.Hud.SetMoves(2); Run(gc, 700f); });
            // Beş hedefli bölüm, dar ekran (20:9) ve çentikli ekran.
            int five = -1;
            Shot(V + "hud_bes_hedef_1080x2400.png", 1080, 2400, null, gc =>
            {
                for (int i = 0; i < gc.Catalog.Count && five < 0; i++) if (gc.Catalog.Levels[i].Goals.Count >= 5) five = i;
                gc.StartLevel(five >= 0 ? five : 400);
                Run(gc, 100f);
            });
            Shot(V + "hud_centikli_1080x2400.png", 1080, 2400, Notch, gc => { gc.StartLevel(five >= 0 ? five : 400); Run(gc, 100f); });
            Shot(V + "harita_dugum.png", 1080, 1920, null, gc => gc.Menu.ShowLevels(gc.Catalog.Count, 24, i => i < 24, () => { }, 0));
            Shot(V + "harita_donus.png", 1080, 1920, null, gc => { gc.Menu.ShowLevels(gc.Catalog.Count, 24, i => i < 24, () => { }, 2); for (int k = 0; k < 20; k++) gc.Menu.Tick(0.02f); });
            File.WriteAllText(Path.Combine(PeelCapture.OutDir, "mascot", "RAPOR.md"),
                $"# Kristal maskot yakalamaları\n\nBeş hedefli bölüm indeksi: {five} (bölüm {five + 1}).\nKareler editör render'ıdır; cihaz performansı değildir.\n");
            Debug.Log("[KatKat] Maskot yakalamaları tamam (beş hedef: " + five + ")");
        }

        /// <summary>
        /// v6 tepsi + şekilli tahta doğrulaması: Temp/v6json altındaki (LevelLab build, KATKAT_SRC=levels_src_v6)
        /// bölümleri katalog dışı oynatır: başlangıç, kayıtlı çözümün ilk tepsi kullanımından önceki an (tepside taş),
        /// yuva seçili, kullanım sonrası. Çıktı docs/captures/v6tray/.
        /// </summary>
        public static void CaptureTray()
        {
            const string V = "v6tray/";
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v6tray"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var files = Directory.GetFiles("Logs/v6json", "level_*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var f in files)
            {
                var d = KatKat.Levels.LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f));
                int num = KatKat.Levels.LevelValidator.LevelNumber(d.Id);
                string n = num.ToString("000");
                int use = d.Solution.FindIndex(s => s.Type == "use");
                Shot(V + $"l{n}_start.png", 1080, 1920, null, gc => gc.StartCustom(d, num - 1));
                if (use < 0) continue;
                int u = use;
                // Tepsi dolu an: kullanımdan önceki adımlar oynanmış, animasyon bitmiş.
                Shot(V + $"l{n}_tepsi.png", 1080, 1920, null, gc => { gc.StartCustom(d, num - 1); PlaySteps(gc, d, u); });
                Shot(V + $"l{n}_secili.png", 1080, 1920, null, gc => { gc.StartCustom(d, num - 1); PlaySteps(gc, d, u); gc.PressTraySlot(d.Solution[u].Row); });
                Shot(V + $"l{n}_kullanim_300ms.png", 1080, 1920, null, gc =>
                {
                    gc.StartCustom(d, num - 1); PlaySteps(gc, d, u);
                    gc.PlayStep(d.Solution[u]);
                    for (float t = 0; t < 300f; t += 16f) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
                });
            }
            Debug.Log("[KatKat] v6 tepsi yakalamaları tamam");
        }

        private static void PlaySteps(GameController gc, KatKat.Levels.LevelData d, int count)
        {
            for (int i = 0; i < count; i++)
            {
                gc.PlayStep(d.Solution[i]);
                for (int k = 0; k < 400 && (gc.Board.IsPlaying || gc.Board.HasFx); k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
                gc.Hud.AdvanceEffects(1500f);
            }
        }

        /// <summary>
        /// Varlık kullanım taraması (temizlik): bütün yakalama akışları + her mekanik bloğundan bir bölümün kayıtlı
        /// çözümü sonuna (kutlama, zafer kartı) kadar oynatılır; Resources'tan istenen yollar
        /// Logs/resource_usage.txt'ye yazılır.
        /// </summary>
        public static void CaptureUsage()
        {
            KatKat.View.ResourceUsage.Paths.Clear();
            CaptureVisualV3();
            CaptureV5();
            CaptureMascot();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "kullanim"));
            // 1–5 öğretici (rulo), 6 roket, 9 Altın Rulo, 21 ataş, 41 yıldız, 61 buz, 121 kilit, 201 zarf, 301 döner,
            // 401 sünger, 501 bağlı ataş, 601 geçit, 701 boya, 1000 final.
            foreach (int n in new[] { 1, 2, 3, 4, 5, 6, 9, 21, 41, 61, 121, 201, 301, 401, 501, 601, 701, 1000 })
            {
                int idx = n - 1;
                Shot($"kullanim/l{n:000}_son.png", 540, 960, null, gc =>
                {
                    if (idx >= gc.Catalog.Count) return;
                    gc.StartLevel(idx);
                    var d = gc.Catalog.Levels[idx];
                    foreach (var step in d.Solution)
                    {
                        gc.PlayStep(step);
                        for (int k = 0; k < 600 && (gc.Board.IsPlaying || gc.Board.HasFx); k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
                    }
                    if (gc.IsCelebrating) gc.SkipCelebration();
                    gc.Hud.AdvancePanel(2000f);
                });
            }
            var list = new List<string>(KatKat.View.ResourceUsage.Paths);
            list.Sort(StringComparer.Ordinal);
            File.WriteAllLines("Logs/resource_usage.txt", list);
            Debug.Log("[KatKat] Kullanım taraması: " + list.Count + " yol → Logs/resource_usage.txt");
        }

        /// <summary>
        /// Tahta boyutu seçenekleri (kullanıcı onayı için): A mevcut, B alan %88 + en çok 7 sütun boyu,
        /// C alan %78 + en çok 8 sütun boyu; 1. ve 1000. bölüm. Çıktı docs/captures/tahta_boyu/.
        /// </summary>
        public static void CaptureBoardSize()
        {
            const string V = "tahta_boyu/";
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "tahta_boyu"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var opts = new (string tag, float scale, float cols)[] { ("A", 1f, 0f), ("B", 0.88f, 7f), ("Secilen", 0.88f, 5.5f) };
            float keepScale = KatKat.View.BoardView.AreaScale, keepCols = KatKat.View.BoardView.MinColumns;
            try
            {
                foreach (var o in opts)
                {
                    KatKat.View.BoardView.AreaScale = o.scale;
                    KatKat.View.BoardView.MinColumns = o.cols;
                    Shot(V + $"{o.tag}_l001.png", 1080, 1920, null, gc => gc.StartLevel(0));
                    Shot(V + $"{o.tag}_l1000.png", 1080, 1920, null, gc => gc.StartLevel(gc.Catalog.Count - 1));
                }
            }
            finally
            {
                KatKat.View.BoardView.AreaScale = keepScale;
                KatKat.View.BoardView.MinColumns = keepCols;
            }
            Debug.Log("[KatKat] Tahta boyu yakalamaları tamam");
        }

        /// <summary>Tam ekran yakalama + aynı anın doğal piksel kırpımı (kırpım dosyası "_crop" ekiyle).</summary>
        private static void Shot(string file, int w, int h, Rect? safe, Action<GameController> act, RectInt crop)
        {
            Shot(file, w, h, safe, act);
            var full = new Texture2D(2, 2);
            full.LoadImage(File.ReadAllBytes(Path.Combine(PeelCapture.OutDir, file)));
            var part = new Texture2D(crop.width, crop.height, TextureFormat.RGB24, false);
            part.SetPixels(full.GetPixels(crop.x, crop.y, crop.width, crop.height));
            part.Apply();
            File.WriteAllBytes(Path.Combine(PeelCapture.OutDir, file.Replace(".png", "_crop.png")), part.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(full);
            UnityEngine.Object.DestroyImmediate(part);
        }

        // ---- Otomatik yazı taşma denetimi (v5: "yazılar taşıyor" geri bildirimi; gözle kontrol yetmiyor) -------------

        private static readonly System.Text.StringBuilder Overflow = new System.Text.StringBuilder();
        private static int _overflowCount, _textCount;

        /// <summary>Ekrandaki bütün görünür TMP yazıları: alanına sığmayan (taşan ya da kesilen) varsa rapora yazar.</summary>
        private static void OverflowCheck(string shot, GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false))
            {
                if (string.IsNullOrEmpty(t.text) || !t.isActiveAndEnabled || t.alpha <= 0.01f) continue;
                _textCount++;
                t.ForceMeshUpdate();
                var r = t.rectTransform.rect;
                var b = t.textBounds;
                bool over = t.isTextOverflowing || b.size.x > r.width + 4f || b.size.y > r.height + 6f;
                if (!over) continue;
                _overflowCount++;
                Overflow.AppendLine($"| {shot} | {t.transform.parent?.name}/{t.name} | \"{t.text.Replace("\n", " ")}\" | metin {b.size.x:0}×{b.size.y:0}, alan {r.width:0}×{r.height:0} |");
            }
        }

        /// <summary>Taşma raporunu yazar (docs/captures/TASMA_RAPORU.md) ve sayaçları sıfırlar.</summary>
        private static void WriteOverflowReport(string title)
        {
            var path = Path.Combine(PeelCapture.OutDir, "TASMA_RAPORU.md");
            var head = $"# Yazı taşma raporu — {title}\n\nDenetlenen yazı: {_textCount}, taşan: {_overflowCount}\n\n| Ekran | Nesne | Metin | Ölçü |\n|---|---|---|---|\n";
            File.WriteAllText(path, head + Overflow);
            Debug.Log($"[KatKat] Taşma raporu: {_overflowCount}/{_textCount}");
            Overflow.Clear();
            _overflowCount = _textCount = 0;
        }

        public static void ShotPublic(string file, int w, int h, Action<GameController> act) => Shot(file, w, h, null, act);
        public static void ShotPublic(string file, int w, int h, Rect? safe, Action<GameController> act) => Shot(file, w, h, safe, act);

        private static void Shot(string file, int w, int h, Rect? safe, Action<GameController> act)
        {
            SafeAreaFitter.NormalizedOverride = safe ?? new Rect(0f, 0f, 1f, 1f);
            UiAspect.Override = (float)w / h; // iPad: tuval yüksekliğe eşlenir, güvenli alan ortada sütun
            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = (float)w / h;

            var saveDir = Path.Combine(Path.GetTempPath(), "katkat_capture");
            if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
            var root = new GameObject("Game");
            var gc = root.AddComponent<GameController>();
            gc.Setup(cam, new KatKat.Save.SaveService(saveDir));
            // Örnek ilerleme: menü ekranlarında kilitli/tamamlanmış durumlar görünsün.
            for (int i = 0; i < 6; i++) gc.SaveService.MarkCompleted(gc.Catalog.Levels[i].Id, i, gc.Catalog.Count);
            // Oynatma dışında CanvasScaler çalışmaz; aynı ölçeği elle ver (genişliğe eşleme).
            gc.Hud.Canvas.scaleFactor = UiAspect.ScaleFactor(w, h);
            gc.Menu.Canvas.scaleFactor = UiAspect.ScaleFactor(w, h);
            foreach (var sc in root.GetComponentsInChildren<Canvas>(true)) if (sc.isRootCanvas && sc != gc.Hud.Canvas && sc != gc.Menu.Canvas) sc.scaleFactor = UiAspect.ScaleFactor(w, h);
            gc.Hud.ApplySafeArea();
            gc.Menu.ApplySafeArea();
            act(gc);

            Canvas.ForceUpdateCanvases();
            Debug.Log($"[KatKat] {file}: faz={gc.CurrentPhase} durum={gc.Session?.State.Status} mantık katman={gc.Session?.State.Board.TotalLayers()}");
            OverflowCheck(file, root);
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(PeelCapture.OutDir, file), tex.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(camGo);
            UiAspect.Override = null;
            foreach (var es in UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>()) UnityEngine.Object.DestroyImmediate(es.gameObject);
        }
    }
}
