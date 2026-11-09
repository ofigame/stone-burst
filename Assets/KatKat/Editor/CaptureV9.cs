using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KatKat.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v9 görsel regresyon denetimi (docs/v9 yol haritası): A/B paketleri öncesi ve sonrası aynı ekranlar, aynı bölüm,
    /// aynı tema ve aynı boyutla yakalanır, piksel farkı raporlanır. İki boyut: 1080×1920 ve çentikli dar 1080×2400.
    /// Batch: KatKat.EditorTools.CaptureV9.Before / After / Compare → docs/captures/v9/
    /// </summary>
    public static class CaptureV9
    {
        private const string Dir = "v9/";

        private static readonly (string tag, int w, int h, Rect? safe)[] Sizes =
        {
            ("1080x1920", 1080, 1920, null),
            // Çentikli dar ekran: üstte ~110 px çentik, altta ~48 px hareket çubuğu.
            ("1080x2400_centik", 1080, 2400, new Rect(0f, 48f / 2400f, 1f, (2400f - 110f - 48f) / 2400f)),
        };

        [MenuItem("Kat Kat/Yakalama/v9 referans (önce)")]
        public static void Before() => Capture("once");

        [MenuItem("Kat Kat/Yakalama/v9 karşılaştırma (sonra)")]
        public static void After() { Capture("sonra"); Compare(); Meta(); }

        /// <summary>Yeni alanlar: günlük ekran durumları, koleksiyon, günlük oyun ekranı, harita giriş önerisi (onaya sunulur).</summary>
        [MenuItem("Kat Kat/Yakalama/v9 günlük ve koleksiyon")]
        public static void Meta()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v9", "yeni"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = KatKat.UI.Loc.Code;
            KatKat.UI.Loc.Set("tr", false);
            GameController.ToursOverride = false;
            GameController.ClockOverride = new KatKat.Meta.FixedDayClock { DayId = "2026-11-06" };
            var s = Sizes[0];
            try
            {
                Shot("yeni/gunluk_giris_tassiz.png", s, gc => gc.ShowDaily());
                Shot("yeni/gunluk_giris.png", s, gc => { Own(gc); gc.ShowDaily(); });
                Shot("yeni/gunluk_hak_var.png", s, gc => { Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); });
                Shot("yeni/gunluk_oyun.png", s, gc => { Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); });
                Shot("yeni/gunluk_guc_kullanildi.png", s, gc => { Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); gc.UseDailyPowerForTest(); });
                Shot("yeni/gunluk_basarili.png", s, gc =>
                {
                    Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily();
                    foreach (var step in gc.CurrentLevel.Solution)
                    {
                        gc.PlayStep(step);
                        for (int k = 0; k < 600 && (gc.Board.IsPlaying || gc.Board.HasFx); k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
                    }
                    if (gc.IsCelebrating) gc.SkipCelebration();
                    gc.Hud.AdvancePanel(2000f);
                });
                Shot("yeni/gunluk_odul_aktif.png", s, gc =>
                {
                    Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily();
                    gc.Daily.FinishAttempt(true);
                    gc.ShowDaily();
                });
                Shot("yeni/normal_bolum_arti1.png", s, gc =>
                {
                    Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily();
                    gc.Daily.FinishAttempt(true);
                    gc.StartLevel(11);
                });
                Shot("yeni/koleksiyon_kilitli.png", s, gc => gc.ShowCollection());
                Shot("yeni/koleksiyon.png", s, gc => { Own(gc); gc.ShowCollection(); });
                Shot("yeni/harita_giris.png", s, gc => gc.ShowLevels());
                Shot("yeni/harita_giris_centik.png", Sizes[1], gc => gc.ShowLevels());
            }
            finally
            {
                GameController.ClockOverride = null;
                GameController.ToursOverride = null;
                KatKat.UI.Loc.Set(keepLang, false);
            }
        }

        /// <summary>v10 P3.4: günlük ekranındaki seviye grubu rozeti (farklı grup ve diller).</summary>
        [MenuItem("Kat Kat/Yakalama/v10 günlük seviye grubu")]
        public static void DailyTier()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v9", "yeni"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = KatKat.UI.Loc.Code;
            GameController.ToursOverride = false;
            GameController.ClockOverride = new KatKat.Meta.FixedDayClock { DayId = "2026-11-06" };
            try
            {
                foreach (var (lang, reached) in new[] { ("tr", 0), ("tr", 599), ("en", 599), ("ar", 599), ("ja", 900) })
                {
                    KatKat.UI.Loc.Set(lang, false);
                    Shot($"yeni/gunluk_grup_{lang}_{reached + 1}.png", Sizes[0], gc =>
                    {
                        Own(gc);
                        if (reached > 0) gc.SaveService.Data.highestUnlocked = reached;
                        gc.SaveService.Data.meta.daily = new KatKat.Meta.DailySave(); // açılışta kilitlenen grubu sıfırla
                        gc.ShowDaily();
                    });
                }
            }
            finally
            {
                GameController.ClockOverride = null;
                GameController.ToursOverride = null;
                KatKat.UI.Loc.Set(keepLang, false);
            }
        }

        /// <summary>v10 P2.6: koleksiyon ızgarası (22 öğe, gerçek görseller). Gösterim için birkaç öğe sahipli yapılır.</summary>
        [MenuItem("Kat Kat/Yakalama/v10 koleksiyon ızgarası")]
        public static void CollectionGrid()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v9", "yeni"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = KatKat.UI.Loc.Code;
            GameController.ToursOverride = false;
            GameController.ClockOverride = new KatKat.Meta.FixedDayClock { DayId = "2026-11-07" };
            Action<GameController> demo = gc =>
            {
                Own(gc);
                foreach (var id in new[] { "stone_ocean", "stone_storm", "treasure_ruby_cup", "legend_phoenix" })
                    if (!gc.SaveService.Data.meta.stones.Contains(id)) gc.SaveService.Data.meta.stones.Add(id);
                gc.ShowCollection();
            };
            try
            {
                foreach (var lang in new[] { "tr", "en", "ja" })
                {
                    KatKat.UI.Loc.Set(lang, false);
                    Shot($"yeni/koleksiyon_izgara_{lang}.png", Sizes[0], demo);
                }
                KatKat.UI.Loc.Set("tr", false);
                Shot("yeni/koleksiyon_izgara_alt_tr.png", Sizes[0], gc => { demo(gc); gc.Menu.ScrollCollectionForTest(0f); });
                Shot("yeni/koleksiyon_izgara_centik.png", Sizes[1], demo);
            }
            finally
            {
                GameController.ClockOverride = null;
                GameController.ToursOverride = null;
                KatKat.UI.Loc.Set(keepLang, false);
            }
        }

        /// <summary>Yakalama kaydında taşı kazandırır (10. bölüm tamamlanmış sayılır).</summary>
        private static void Own(GameController gc)
        {
            for (int i = 0; i < 10; i++) gc.SaveService.MarkCompleted(gc.Catalog.Levels[i].Id, i, gc.Catalog.Count);
            KatKat.Meta.CollectionRules.GrantUnlocked(gc.SaveService.Data.meta, gc.Stones, gc.SaveService.Data.IsCompleted);
        }

        private static void Capture(string phase)
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v9", phase));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = KatKat.UI.Loc.Code;
            KatKat.UI.Loc.Set("tr", false);
            GameController.ToursOverride = false;
            try
            {
                foreach (var s in Sizes)
                {
                    Shot($"{phase}/ana_{s.tag}.png", s, gc => gc.ShowHome());
                    Shot($"{phase}/harita_{s.tag}.png", s, gc => gc.ShowLevels());
                    Shot($"{phase}/oyun_l038_{s.tag}.png", s, gc => gc.StartLevel(37));
                    Shot($"{phase}/sonuc_l038_{s.tag}.png", s, gc => Win(gc, 37));
                }
            }
            finally
            {
                GameController.ToursOverride = null;
                KatKat.UI.Loc.Set(keepLang, false);
            }
            Debug.Log("[KatKat] v9 yakalama tamam: " + phase);
        }

        private static void Shot(string file, (string tag, int w, int h, Rect? safe) s, Action<GameController> act) =>
            GameCapture.ShotPublic(Dir + file, s.w, s.h, s.safe, act);

        private static void Win(GameController gc, int idx)
        {
            gc.StartLevel(idx);
            foreach (var step in gc.Catalog.Levels[idx].Solution)
            {
                gc.PlayStep(step);
                for (int k = 0; k < 600 && (gc.Board.IsPlaying || gc.Board.HasFx); k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
            }
            if (gc.IsCelebrating) gc.SkipCelebration();
            gc.Hud.AdvancePanel(2000f);
        }

        /// <summary>once/ ve sonra/ çiftlerini karşılaştırır: farklı piksel oranı (kanal farkı &gt; 24) ve fark görüntüsü.</summary>
        [MenuItem("Kat Kat/Yakalama/v9 farkları raporla")]
        public static void Compare()
        {
            string root = Path.Combine(PeelCapture.OutDir, "v9");
            var sb = new StringBuilder("# v9 görsel regresyon karşılaştırması\n\n");
            sb.AppendLine("Editör render'ı; aynı bölüm (38), tema, dil (tr) ve boyut. Eşik: kanal farkı > 24. Cihaz denetimi değildir.\n");
            sb.AppendLine("| Ekran | Farklı piksel | Sonuç |\n|---|---|---|");
            var before = Path.Combine(root, "once");
            if (!Directory.Exists(before)) { Debug.LogError("[KatKat] v9/once yok"); return; }
            foreach (var f in Directory.GetFiles(before, "*.png"))
            {
                var name = Path.GetFileName(f);
                var after = Path.Combine(root, "sonra", name);
                if (!File.Exists(after)) { sb.AppendLine($"| {name} | — | sonra yok |"); continue; }
                var a = new Texture2D(2, 2); a.LoadImage(File.ReadAllBytes(f));
                var b = new Texture2D(2, 2); b.LoadImage(File.ReadAllBytes(after));
                if (a.width != b.width || a.height != b.height) { sb.AppendLine($"| {name} | boyut farklı | FARK |"); continue; }
                var pa = a.GetPixels32(); var pb = b.GetPixels32();
                var diff = new Color32[pa.Length];
                int bad = 0;
                for (int i = 0; i < pa.Length; i++)
                {
                    int d = Math.Max(Math.Abs(pa[i].r - pb[i].r), Math.Max(Math.Abs(pa[i].g - pb[i].g), Math.Abs(pa[i].b - pb[i].b)));
                    if (d > 24) { bad++; diff[i] = new Color32(255, 0, 60, 255); }
                    else diff[i] = new Color32((byte)(pa[i].r / 3 + 160), (byte)(pa[i].g / 3 + 160), (byte)(pa[i].b / 3 + 160), 255);
                }
                float pct = 100f * bad / pa.Length;
                if (bad > 0)
                {
                    var t = new Texture2D(a.width, a.height, TextureFormat.RGB24, false);
                    t.SetPixels32(diff); t.Apply();
                    Directory.CreateDirectory(Path.Combine(root, "fark"));
                    File.WriteAllBytes(Path.Combine(root, "fark", name), t.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(t);
                }
                sb.AppendLine($"| {name} | {pct:0.000}% | {(pct < 0.05f ? "aynı" : "FARK")} |");
                UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
            }
            File.WriteAllText(Path.Combine(root, "KARSILASTIRMA.md"), sb.ToString());
            Debug.Log("[KatKat] v9 karşılaştırma yazıldı");
        }
    }
}
