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
    /// 2026-11-25 su kipi ekran denetimi (test bölümleri): bölüm başı (sulu hücrelerde ışıltı ve damla), damlalar kovaya uçarken,
    /// yarım ve dolu tepsi, dökülme anı, örtülü ve patron bölümü. Çıktı Logs/captures/su/ (tools/unity.ps1 capture-water).
    /// </summary>
    public static class CaptureWater
    {
        private const int W = 1080, H = 2340;

        public static void RunBatch()
        {
            int code = 0;
            try { Run(); }
            catch (Exception e) { Debug.LogError("[KatKat] su yakalama hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "su"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shot("01_ilk_damlalar", 0, gc => { });
            Shot("02_derin_kuyular", 1, gc => { });
            Shot("03_damlalar_ucuyor", 0, gc => { TapWaterGroup(gc); Run(gc, 900f); });
            Shot("04_tepsi_yarim", 1, gc => { Fill(gc, 0.45f); });
            Shot("05_tepsi_neredeyse_dolu", 2, gc => { Fill(gc, 0.85f); });
            Shot("05b_dokulurken", 0, gc => { Fill(gc, 0.3f); Pour(gc); gc.Board.AdvanceWater(0.15f); });
            Shot("05c_dokulurken", 0, gc => { Fill(gc, 0.3f); Pour(gc); gc.Board.AdvanceWater(0.3f); });
            Shot("05d_dokulurken", 0, gc => { Fill(gc, 0.3f); Pour(gc); gc.Board.AdvanceWater(0.45f); });
            Shot("05e_dokulurken", 0, gc => { Fill(gc, 0.3f); Pour(gc); gc.Board.AdvanceWater(0.7f); });
            Shot("09_egim_sag", 1, gc => { KatKat.View.BoardView.TiltOverride = new Vector3(0.4f, -0.9f, 0f); Fill(gc, 0.5f); gc.Board.AdvanceWater(2f); KatKat.View.BoardView.TiltOverride = null; });
            Shot("10_yarik_sizinti", 1, gc => { Fill(gc, 0.6f); gc.Board.OpenCrack(2); gc.Board.AdvanceWater(1.2f); });
            Shot("11_yarik_egim_ters", 1, gc => { KatKat.View.BoardView.TiltOverride = new Vector3(0.5f, -0.85f, 0f); Fill(gc, 0.3f); gc.Board.OpenCrack(1); gc.Board.AdvanceWater(1.5f); KatKat.View.BoardView.TiltOverride = null; });
            Shot("12_gonderim", 1, gc => { Fill(gc, 1f); gc.Hud.AdvanceWaterBar(0.1f); gc.SendWaterBucket(); gc.Board.AdvanceWater(0.3f); });
            Shot("13_gonder_hazir", 1, gc => { Fill(gc, 1f); gc.Hud.AdvanceWaterBar(0.3f); });
            Shot("14_cubuk_yarim", 1, gc => { gc.Hud.SetWaterBar(0.55f, true); Fill(gc, 0.4f); gc.Hud.AdvanceWaterBar(0.3f); });
            Shot("15_cubuk_dolu", 1, gc => { gc.Hud.SetWaterBar(1f, true); gc.Hud.AdvanceWaterBar(0.3f); });
            foreach (int n in new[] { 19, 115, 331, 683 }) GameCapture.ShotPublic($"su/katalog_{n:000}.png", W, H, gc => { gc.StartLevel(n - 1); gc.Hud.HideRuleCard(false); Fill(gc, 1f); gc.Hud.SetWaterBar(0.4f, true); gc.Hud.AdvanceWaterBar(0.3f); gc.Board.AdvanceWater(0.5f); UnityEngine.Canvas.ForceUpdateCanvases(); });
            Shot("06_buzun_alti", 5, gc => { });
            Shot("07_su_ve_renk", 7, gc => { });
            Shot("08_patron", 9, gc => { });
            Debug.Log("[KatKat] su yakalamaları tamam → " + Path.Combine(PeelCapture.OutDir, "su"));
        }

        private static void Shot(string name, int lab, Action<GameController> act)
        {
            GameCapture.ShotPublic($"su/{name}.png", W, H, gc =>
            {
                gc.ShowPilots();
                gc.StartTrial(lab);
                gc.Hud.HideRuleCard(false);
                act(gc);
                UnityEngine.Canvas.ForceUpdateCanvases();
                gc.Board.AdvanceWater(0.02f);
                gc.Hud.AdvanceWaterBar(0.04f);
            });
        }

        /// <summary>Tepsiyi verilen orana kadar doldurur (gizli sayaç), seviyeyi oturtur.</summary>
        private static void Fill(GameController gc, float f)
        {
            var st = gc.Session.State;
            var g = st.Goals.First(x => x.Type == GoalType.FillWater);
            int cap = st.WaterCap > 0 ? st.WaterCap : g.Amount;
            st.WaterHave = Mathf.RoundToInt(cap * f);
            gc.Board.SetWater(cap, st.WaterHave, st.Board);
            gc.Board.AdvanceWater(1.2f);
        }

        /// <summary>Üst satırlardaki üç hücreden su dökülür (yakalama için doğrudan).</summary>
        private static void Pour(GameController gc)
        {
            gc.Board.DebugPour(2, 1, 2);
            gc.Board.DebugPour(5, 2, 1);
            gc.Board.DebugPour(6, 0, 3);
        }

        /// <summary>Suyu akıtan bir grup (sulu tek katmanlı hücre içeren); yoksa sulu hücreye en çok değen grup.</summary>
        private static void TapWaterGroup(GameController gc)
        {
            var b = gc.Session.State.Board;
            var groups = GroupFinder.AllComponents(b).Where(c => c.Count >= GroupFinder.MinGroup).ToList();
            var g = groups.FirstOrDefault(c => c.Any(i => b[i].Water > 0 && b[i].Layers.Count == 1))
                ?? groups.OrderByDescending(c => c.Count(i => b[i].Water > 0)).FirstOrDefault();
            if (g == null) { Debug.LogWarning("[KatKat] su grubu yok"); return; }
            gc.OnCellPressed(b.X(g[0]), b.Y(g[0]));
        }

        private static void Run(GameController gc, float ms)
        {
            for (float t = 0f; t < ms; t += 20f)
            {
                gc.Board.Advance(20f);
                gc.Board.Director.Advance(20f);
                gc.Board.AdvanceWater(0.02f);
            }
        }
    }
}
