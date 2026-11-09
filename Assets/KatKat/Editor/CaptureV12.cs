using System;
using System.IO;
using KatKat.Core;
using KatKat.Game;
using KatKat.Meta;
using KatKat.UI;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v12 yakalamaları: sıralama ekranı (docs/v12/siralama) ve test reklam ekranları. Geçici kayıt; canlı kayda ve bölüm
    /// dosyalarına yazılmaz. Sıralama listesi GELİŞTİRME örnek verisiyle (ekranda etiketli) ya da gerçek durum metinleriyle.
    /// Çıktı: docs/captures/v12/&lt;alan&gt;/*.png.
    /// </summary>
    public static class CaptureV12
    {
        private static readonly (string tag, int w, int h, Rect? safe)[] Sizes =
        {
            ("941x1672", 941, 1672, null),
            ("1080x1920", 1080, 1920, null),
            ("1080x2400_centik", 1080, 2400, new Rect(0f, 48f / 2400f, 1f, (2400f - 110f - 48f) / 2400f)),
            ("1200x1600_kisa", 1200, 1600, null),
        };

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v12", "siralama"));
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v12", "reklam"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set("tr", false);
            GameController.ToursOverride = false;
            GameController.ClockOverride = new FixedDayClock { DayId = "2026-11-08" };
            try
            {
                GameController.DevLeaderboardSample = true;
                GameController.CountryOverride = "TR";
                foreach (var s in Sizes)
                    Shot($"v12/siralama/dunya_{s.tag}.png", s, gc => { Progress(gc, 40); gc.ShowRanking(LeaderboardScope.World); });
                var n = Sizes[1];
                Shot("v12/siralama/ulke_TR.png", n, gc => { Progress(gc, 40); gc.ShowRanking(LeaderboardScope.Country); });
                Shot("v12/siralama/bilgi.png", n, gc => { Progress(gc, 40); gc.ShowRanking(); gc.Menu.ShowRankInfo(); });
                Shot("v12/siralama/uygun_degil_12bolum.png", n, gc => { Progress(gc, 12); gc.ShowRanking(); });
                Shot("v12/siralama/en_uzun_de.png", n, gc => { Loc.Set("de", false); Progress(gc, 40); gc.ShowRanking(); });
                Loc.Set("tr", false);
                GameController.CountryOverride = "";
                Shot("v12/siralama/ulke_yok.png", n, gc => { Progress(gc, 40); gc.ShowRanking(LeaderboardScope.Country); });
                GameController.CountryOverride = "TR";
                // Gerçek durum: sunucu bağlı değil (oyuncunun göreceği hâl).
                GameController.DevLeaderboardSample = false;
                Shot("v12/siralama/sunucu_yok.png", n, gc => { Progress(gc, 40); gc.ShowRanking(); });
                Shot("v12/siralama/harita_dugmesi.png", n, gc => { Progress(gc, 4); gc.ShowLevels(); });

                // v12 deneme seti: harita düğmesi, liste, kural kartları, tahta (kristal, zincir), katman görünümü.
                Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v12", "deneme"));
                Shot("v12/deneme/harita_dugmesi.png", n, gc => { Progress(gc, 4); gc.ShowLevels(); });
                Shot("v12/deneme/liste.png", n, gc => { Progress(gc, 4); gc.SaveService.Data.meta.trialDone.Add("trial_01"); gc.ShowLevels(); gc.ShowTrials(); });
                for (int t = 0; t < GameController.TrialCount; t++)
                {
                    int ti = t;
                    Shot($"v12/deneme/kart_{ti + 1:00}.png", n, gc => gc.StartTrial(ti));
                    Shot($"v12/deneme/tahta_{ti + 1:00}.png", n, gc => { gc.StartTrial(ti); gc.Hud.HideRuleCard(false); });
                }
                Shot("v12/deneme/katman_gorunumu.png", n, gc => { gc.StartTrial(5); gc.Hud.HideRuleCard(false); gc.PeekForTest(2, 2, true); });
                Shot("v12/deneme/tahta_centik_10.png", Sizes[2], gc => { gc.StartTrial(9); gc.Hud.HideRuleCard(false); });

                // Test reklam ekranları (oyunda gerçek ağ bağlanana kadar görünen).
                GameController.AdScreenOverride = true;
                Shot("v12/reklam/gunluk_araniyor.png", n, gc => { Own(gc); gc.ShowDaily(); TestAdScreen.FillSeconds = 99f; gc.PressDailyEnterForTest(); gc.AdScreen.Advance(1600f); });
                Shot("v12/reklam/odullu_geri_sayim.png", n, gc => { Own(gc); gc.ShowDaily(); TestAdScreen.FillSeconds = 0.5f; gc.PressDailyEnterForTest(); gc.AdScreen.Advance(600f); gc.AdScreen.Advance(2000f); });
                Shot("v12/reklam/odullu_odul_al.png", n, gc => { Own(gc); gc.ShowDaily(); TestAdScreen.FillSeconds = 0.5f; gc.PressDailyEnterForTest(); gc.AdScreen.Advance(600f); gc.AdScreen.Advance(5200f); });
                Shot("v12/reklam/gecis.png", n, gc => { gc.StartLevel(10); gc.AdScreen.ShowInterstitial(AdPlacements.LevelComplete, _ => { }); gc.AdScreen.Advance(3200f); });
                GameController.AdScreenOverride = false;
                Shot("v12/reklam/kayip_kart_3_hamle.png", n, gc => LoseLevel4(gc));
            }
            finally
            {
                TestAdScreen.FillSeconds = 1.2f;
                GameController.AdScreenOverride = null;
                GameController.DevLeaderboardSample = false;
                GameController.CountryOverride = null;
                GameController.ToursOverride = null;
                GameController.ClockOverride = null;
                Loc.Set(keepLang, false);
            }
        }

        /// <summary>Geçici kayıt: n bölüm × 3 yıldız, birkaç ZPE sonucu.</summary>
        private static void Progress(GameController gc, int n)
        {
            var d = gc.SaveService.Data;
            d.completed.Clear(); d.starIds.Clear(); d.starCounts.Clear(); d.highestUnlocked = 0;
            for (int i = 0; i < n; i++)
            {
                gc.SaveService.MarkCompleted(gc.Catalog.IdAt(i), i, gc.Catalog.Count);
                gc.SaveService.RecordStars(gc.Catalog.IdAt(i), 2 + i % 2);
                RankStars.Record(d.meta, gc.Catalog.IdAt(i), 2 + i % 2);
            }
            d.meta.zpeHistory.Clear(); d.meta.zpeSum = 0; d.meta.zpeGames = 0;
            for (int i = 0; i < Math.Min(n, 30); i++)
                Zpe.Add(d.meta, new ZpeRecord { levelId = gc.Catalog.IdAt(i), success = true, firstAttempt = i % 3 != 0, movesUsed = 5 + i % 4, moveLimit = 10, seconds = 30 });
        }

        private static void Own(GameController gc)
        {
            for (int i = 0; i < 10; i++) gc.SaveService.MarkCompleted(gc.Catalog.Levels[i].Id, i, gc.Catalog.Count);
            CollectionRules.GrantUnlocked(gc.SaveService.Data.meta, gc.Stones, gc.SaveService.Data.IsCompleted);
        }

        /// <summary>Bölüm 4: mavi grubu önce soymak kaybettirir → kayıp kartında +3 hamle teklifi.</summary>
        private static void LoseLevel4(GameController gc)
        {
            gc.StartLevel(3);
            gc.Board.Director.TimeScale = 50f;
            gc.OnCellPressed(0, 0);
            for (int k = 0; k < 400 && gc.CurrentPhase == GameController.Phase.Animating; k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
            var next = KatKat.Levels.LevelSolver.Moves(gc.Session.State);
            if (next.Count > 0) gc.OnCellPressed(next[0].X, next[0].Y);
            for (int k = 0; k < 400 && gc.CurrentPhase == GameController.Phase.Animating; k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
        }

        private static void Shot(string file, (string tag, int w, int h, Rect? safe) s, Action<GameController> act) =>
            GameCapture.ShotPublic(file, s.w, s.h, s.safe, gc =>
            {
                if (gc.AdScreen != null) gc.AdScreen.Canvas.scaleFactor = s.w / HudView.RefWidth;
                act(gc);
            });
    }
}
