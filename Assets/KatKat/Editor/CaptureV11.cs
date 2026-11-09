using System;
using System.IO;
using KatKat.Core;
using KatKat.Game;
using KatKat.UI;
using KatKat.View;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v11 yakalamaları (docs/v11/CLAUDE-v11-PROMPT.md). Aynı geçici kayıt ve kodla önce (v10 kabuk + eski simgeler,
    /// <see cref="MenuView.ForceLegacyShell"/> / <see cref="SpecialArt.ForceLegacy"/>) ve sonra (v11).
    /// Kayıt: bölge 1, bölüm 5 aktif, 12 yıldız, 4 oyunluk ZPE. Çıktı: docs/captures/v11/&lt;alan&gt;/&lt;faz&gt;_*.png.
    /// Canlı kayda ve bölüm dosyalarına yazılmaz.
    /// </summary>
    public static class CaptureV11
    {
        private static readonly (string tag, int w, int h, Rect? safe)[] Sizes =
        {
            ("941x1672", 941, 1672, null),
            ("1080x1920", 1080, 1920, null),
            ("1080x2400_centik", 1080, 2400, new Rect(0f, 48f / 2400f, 1f, (2400f - 110f - 48f) / 2400f)),
        };

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v11", "ana"));
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v11", "oyun"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set("tr", false);
            GameController.ToursOverride = false;
            GameController.ClockOverride = new KatKat.Meta.FixedDayClock { DayId = "2026-11-08" };
            try
            {
                foreach (bool legacy in new[] { true, false })
                {
                    MenuView.ForceLegacyShell = legacy;
                    SpecialArt.ForceLegacy = legacy;
                    string ph = legacy ? "once" : "sonra";
                    foreach (var s in Sizes)
                    {
                        Shot($"v11/ana/{ph}_{s.tag}.png", s, gc => { Home(gc); gc.ShowLevels(); });
                        Shot($"v11/oyun/{ph}_l219_tepsi_{s.tag}.png", s, gc => Tray(gc));
                    }
                    Shot($"v11/oyun/{ph}_yakin_ozel.png", Sizes[1], gc => CloseUp(gc));
                }
                MenuView.ForceLegacyShell = false;
                SpecialArt.ForceLegacy = false;
                var n = Sizes[1];
                Shot("v11/ana/sonra_koleksiyon.png", n, gc => { Home(gc); gc.ShowCollection(); });
                Shot("v11/ana/sonra_zpe_panel.png", n, gc => { Home(gc); gc.ShowLevels(); gc.ShowZpeStats(); });
                Shot("v11/ana/sonra_zpe_yok.png", n, gc => { Home(gc, 0); gc.ShowLevels(); });
                Shot("v11/ana/sonra_gunluk_modal.png", n, gc => { Home(gc); gc.ShowDaily(); });
                Shot("v11/ana/sonra_bolge2.png", n, gc => { Home(gc); gc.ShowLevels(); gc.Menu.ScrollMapForTest(23600f); });
                for (int k = 3; k <= 5; k++)
                {
                    int count = k;
                    Shot($"v11/ana/sonra_menu_{count}oge.png", n, gc => { Home(gc); gc.ShowLevels(); gc.Menu.PreviewTabBar(count); });
                }
                Shot("v11/ana/sonra_en_uzun_de.png", n, gc => { Loc.Set("de", false); Home(gc); gc.ShowLevels(); });
                Loc.Set("tr", false);
                // Günlük: seçili taş alt güç alanında gerçek görseliyle; kullanılınca soluk.
                Shot("v11/oyun/sonra_gunluk_tas_yuvasi.png", n, gc => { Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); });
                Shot("v11/oyun/sonra_gunluk_tas_kullanildi.png", n, gc => { Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); gc.UseDailyPowerForTest(); });
                Shot("v11/oyun/sonra_gunluk_tas_centik.png", Sizes[2], gc => { Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); });
                // Roket bölünmesi: yatay ve dikey, ateşlemeden sonra dört an.
                foreach (var kind in new[] { SpecialKind.RocketRow, SpecialKind.RocketColumn })
                    foreach (int ms in new[] { 60, 160, 300, 480 })
                    {
                        var k2 = kind;
                        int t = ms;
                        Shot($"v11/oyun/sonra_bolunme_{(k2 == SpecialKind.RocketRow ? "yatay" : "dikey")}_{t:000}ms.png", n, gc => Fire(gc, k2, t));
                    }
            }
            finally
            {
                MenuView.ForceLegacyShell = false;
                SpecialArt.ForceLegacy = false;
                GameController.ToursOverride = null;
                GameController.ClockOverride = null;
                Loc.Set(keepLang, false);
            }
        }

        /// <summary>Referans kayıt: 4 bölüm × 3 yıldız (12), bölüm 5 aktif; ZPE için <paramref name="zpeGames"/> sonuç.</summary>
        private static void Home(GameController gc, int zpeGames = 4)
        {
            var d = gc.SaveService.Data;
            d.completed.Clear(); d.starIds.Clear(); d.starCounts.Clear(); d.highestUnlocked = 0;
            for (int i = 0; i < 4; i++)
            {
                gc.SaveService.MarkCompleted(gc.Catalog.IdAt(i), i, gc.Catalog.Count);
                gc.SaveService.RecordStars(gc.Catalog.IdAt(i), 3);
            }
            var meta = d.meta;
            meta.zpeHistory.Clear();
            meta.zpeSum = 0; meta.zpeGames = 0;
            var rng = new System.Random(7);
            for (int i = 0; i < zpeGames; i++)
                KatKat.Meta.Zpe.Add(meta, new KatKat.Meta.ZpeRecord
                {
                    levelId = gc.Catalog.IdAt(i), success = true, firstAttempt = rng.NextDouble() < 0.7,
                    movesUsed = 5 + rng.Next(4), moveLimit = 10, seconds = 25 + rng.Next(40),
                });
        }

        private static void Own(GameController gc)
        {
            for (int i = 0; i < 10; i++) gc.SaveService.MarkCompleted(gc.Catalog.Levels[i].Id, i, gc.Catalog.Count);
            KatKat.Meta.CollectionRules.GrantUnlocked(gc.SaveService.Data.meta, gc.Stones, gc.SaveService.Data.IsCompleted);
        }

        /// <summary>Bölüm 219: tepside yatay, dikey ve renk temizleyici.</summary>
        private static void Tray(GameController gc)
        {
            gc.StartLevel(218);
            var st = gc.Session.State;
            st.Tray.Clear();
            st.Tray.Add(SpecialKind.RocketRow);
            st.Tray.Add(SpecialKind.RocketColumn);
            if (KatKat.Core.LevelState.TrayCapacity > 2) st.Tray.Add(SpecialKind.Rainbow);
            gc.Hud.SetTrayVisible(true);
            gc.Hud.SetTray(st.Tray, -1);
        }

        /// <summary>Normal taşlar yanında üç özel taş tahtada (yalnız görünüm; mantık durumuna yazılmaz).</summary>
        private static void CloseUp(GameController gc)
        {
            Tray(gc);
            var b = gc.Session.State.Board;
            int placed = 0;
            var kinds = new[] { SpecialKind.RocketRow, SpecialKind.RocketColumn, SpecialKind.Rainbow };
            for (int y = 0; y < b.Height && placed < 3; y++)
            for (int x = 1; x < b.Width && placed < 3; x += 2)
            {
                if (!b[x, y].Active || b[x, y].Layers.Count == 0) continue;
                gc.Board.StackAt(x, y).SetSpecial(kinds[placed], b[x, y].Layers[b[x, y].Layers.Count - 1]);
                placed++;
            }
        }

        /// <summary>Tepsideki roketi bölüm 38'de ortadaki dolu hücreye uygular; <paramref name="ms"/> sonra durur.</summary>
        private static void Fire(GameController gc, SpecialKind kind, int ms)
        {
            gc.StartLevel(37);
            var st = gc.Session.State;
            st.Tray.Clear();
            st.Tray.Add(kind);
            gc.Hud.SetTrayVisible(true);
            gc.Hud.SetTray(st.Tray, -1);
            var b = st.Board;
            int cx = b.Width / 2, cy = b.Height / 2;
            for (int r = 0; r < Math.Max(b.Width, b.Height) && !(b[cx, cy].Active && b[cx, cy].Layers.Count > 0); r++)
            {
                cx = (cx + 1) % b.Width;
                if (cx == 0) cy = (cy + 1) % b.Height;
            }
            gc.PlayStep(new KatKat.Levels.StepData { Type = "use", Row = 0, X = cx, Y = cy });
            for (int k = 0; k < ms / 16; k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
        }

        private static void Shot(string file, (string tag, int w, int h, Rect? safe) s, Action<GameController> act) =>
            GameCapture.ShotPublic(file, s.w, s.h, s.safe, act);
    }
}
