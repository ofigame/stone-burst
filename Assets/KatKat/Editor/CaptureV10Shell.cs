using System;
using System.IO;
using KatKat.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v10 Mavi Liman kabuğu yakalamaları (docs/v10/TriKat-Mavi-Liman-Claude). Geçici kayıt: bölge 1, aktif bölüm 5,
    /// toplam 12 yıldız (canlı kayda yazılmaz). Çıktı: docs/captures/v10/kabuk/&lt;faz&gt;_*.png.
    /// </summary>
    public static class CaptureV10Shell
    {
        private static readonly (string tag, int w, int h, Rect? safe)[] Sizes =
        {
            ("940x1673", 940, 1673, null),
            ("1080x1920", 1080, 1920, null),
            ("1080x2400_centik", 1080, 2400, new Rect(0f, 48f / 2400f, 1f, (2400f - 110f - 48f) / 2400f)),
        };

        public static void Before() { Capture("once", false); Collection("once"); }
        public static void After() { Capture("sonra", true); Collection("sonra"); }

        /// <summary>v10 taş vitrini: 3D modelli taş (önizleme), yalnız PNG'li taş, yeni taş kazanımı; geçici kayıt.</summary>
        public static void Showcase()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = KatKat.UI.Loc.Code;
            KatKat.UI.Loc.Set("tr", false);
            GameController.ToursOverride = false;
            Action<GameController, string, bool, float> show = (gc, id, isNew, ms) =>
            {
                ReferenceSave(gc, 10, 3);
                var meta = gc.SaveService.Data.meta;
                meta.stones.Clear();
                meta.stones.AddRange(new[] { "stone_tide", "stone_ocean", "stone_ember", "treasure_ruby_cup", "treasure_obelisk", "treasure_rose_throne" });
                gc.ShowCollection();
                gc.ShowStone(id, isNew);
                gc.Showcase.Advance(ms);
            };
            try
            {
                Shot("v10/vitrin/okyanus_3d.png", Sizes[1], gc => show(gc, "stone_ocean", false, 600f));
                Shot("v10/vitrin/okyanus_3d_donmus.png", Sizes[1], gc => show(gc, "stone_ocean", false, 4200f));
                Shot("v10/vitrin/yakut_kupa_3d.png", Sizes[1], gc => show(gc, "treasure_ruby_cup", false, 1500f));
                Shot("v10/vitrin/gul_tahti_3d.png", Sizes[1], gc => show(gc, "treasure_rose_throne", false, 1500f));
                Shot("v10/vitrin/obelisk_3d.png", Sizes[1], gc => show(gc, "treasure_obelisk", false, 1500f));
                Shot("v10/vitrin/gelgit_png.png", Sizes[1], gc => show(gc, "stone_tide", false, 900f));
                Shot("v10/vitrin/yeni_alev_3d.png", Sizes[1], gc => show(gc, "stone_ember", true, 900f));
                Shot("v10/vitrin/yeni_alev_3d_centik.png", Sizes[2], gc => show(gc, "stone_ember", true, 900f));
                // v10 P5 ZPE: geçici kayıtta 30 örnek bölüm sonucu (yalnız görüntü için).
                Action<GameController, int> zpe = (gc, n) =>
                {
                    ReferenceSave(gc, 40, 3);
                    var meta = gc.SaveService.Data.meta;
                    meta.zpeHistory.Clear();
                    var rng = new System.Random(7);
                    for (int i = 0; i < n; i++)
                        KatKat.Meta.Zpe.Add(meta, new KatKat.Meta.ZpeRecord
                        {
                            levelId = gc.Catalog.IdAt(i), success = rng.NextDouble() < 0.85, firstAttempt = rng.NextDouble() < 0.6,
                            assisted = rng.NextDouble() < 0.1, movesUsed = 5 + rng.Next(5), moveLimit = 10, seconds = 20 + rng.Next(60),
                        });
                };
                Shot("v10/vitrin/zpe_anasayfa.png", Sizes[1], gc => { zpe(gc, 30); gc.ShowLevels(); });
                Shot("v10/vitrin/zpe_panel.png", Sizes[1], gc => { zpe(gc, 30); gc.ShowLevels(); gc.ShowZpeStats(); });
                Shot("v10/vitrin/zpe_kalibrasyon.png", Sizes[1], gc => { zpe(gc, 4); gc.ShowLevels(); gc.ShowZpeStats(); });
                Shot("v10/vitrin/zpe_anasayfa_centik.png", Sizes[2], gc => { zpe(gc, 30); gc.ShowLevels(); });
            }
            finally
            {
                GameController.ToursOverride = null;
                KatKat.UI.Loc.Set(keepLang, false);
            }
        }

        /// <summary>Koleksiyon referans durumu: yıldız kategorisinde üç kazanılmış taş (geçici kayıt).</summary>
        private static void Collection(string phase)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = KatKat.UI.Loc.Code;
            KatKat.UI.Loc.Set("tr", false);
            GameController.ToursOverride = false;
            Action<GameController> setup = gc =>
            {
                ReferenceSave(gc, 10, 3);
                var meta = gc.SaveService.Data.meta;
                meta.stones.Clear();
                meta.stones.AddRange(new[] { "stone_tide", "stone_ocean", "stone_storm" });
                meta.selectedStone = "stone_tide";
                gc.ShowCollection();
            };
            var sizes = new[] { ("941x1672", 941, 1672, (Rect?)null), Sizes[1], Sizes[2] };
            try
            {
                foreach (var s in sizes) Shot($"v10/kabuk/{phase}_koleksiyon_{s.Item1}.png", s, setup);
                Shot($"v10/kabuk/{phase}_koleksiyon_orta.png", Sizes[1], gc => { setup(gc); gc.Menu.ScrollCollectionForTest(0.5f); });
                Shot($"v10/kabuk/{phase}_koleksiyon_son.png", Sizes[1], gc => { setup(gc); gc.Menu.ScrollCollectionForTest(0f); });
                KatKat.UI.Loc.Set("de", false);
                Shot($"v10/kabuk/{phase}_koleksiyon_de.png", Sizes[1], setup);
            }
            finally
            {
                GameController.ToursOverride = null;
                KatKat.UI.Loc.Set(keepLang, false);
            }
        }

        /// <summary>Referans durumu: ilk 4 bölüm 3'er yıldızla geçilmiş (toplam 12), aktif bölüm 5.</summary>
        private static void ReferenceSave(GameController gc, int completed = 4, int stars = 3)
        {
            var d = gc.SaveService.Data;
            d.completed.Clear(); d.starIds.Clear(); d.starCounts.Clear(); d.highestUnlocked = 0;
            for (int i = 0; i < completed; i++)
            {
                gc.SaveService.MarkCompleted(gc.Catalog.IdAt(i), i, gc.Catalog.Count);
                gc.SaveService.RecordStars(gc.Catalog.IdAt(i), stars);
            }
        }

        private static void Capture(string phase, bool extras)
        {
            string dir = Path.Combine(PeelCapture.OutDir, "v10", "kabuk");
            Directory.CreateDirectory(dir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = KatKat.UI.Loc.Code;
            KatKat.UI.Loc.Set("tr", false);
            GameController.ToursOverride = false;
            GameController.ClockOverride = new KatKat.Meta.FixedDayClock { DayId = "2026-11-07" };
            try
            {
                foreach (var s in Sizes)
                    Shot($"v10/kabuk/{phase}_{s.tag}.png", s, gc => { ReferenceSave(gc); gc.ShowLevels(); });
                if (!extras) return;
                var n = Sizes[1];
                // Kaydırma: üst/alt kabuk sabit, başlık bölge 2'ye geçer.
                Shot($"v10/kabuk/{phase}_kaydirma_bolge2.png", n, gc => { ReferenceSave(gc); gc.ShowLevels(); gc.Menu.ScrollMapForTest(23600f); });
                Shot($"v10/kabuk/{phase}_gunluk.png", n, gc => { ReferenceSave(gc); gc.ShowDaily(); });
                Shot($"v10/kabuk/{phase}_taslar.png", n, gc => { ReferenceSave(gc); gc.ShowCollection(); });
                Shot($"v10/kabuk/{phase}_ayarlar.png", n, gc => { ReferenceSave(gc); gc.ShowSettingsForCapture(); });
                // Uzun yerelleştirme + büyük yıldız sayısı.
                foreach (var lang in new[] { "de", "es", "ar" })
                {
                    KatKat.UI.Loc.Set(lang, false);
                    Shot($"v10/kabuk/{phase}_uzun_{lang}.png", n, gc => { ReferenceSave(gc, 999, 3); gc.ShowLevels(); });
                }
            }
            finally
            {
                GameController.ClockOverride = null;
                GameController.ToursOverride = null;
                KatKat.UI.Loc.Set(keepLang, false);
            }
        }

        private static void Shot(string file, (string tag, int w, int h, Rect? safe) s, Action<GameController> act) =>
            GameCapture.ShotPublic(file, s.w, s.h, s.safe, act);
    }
}
