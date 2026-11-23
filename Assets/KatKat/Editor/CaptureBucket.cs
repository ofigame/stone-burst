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
    /// 2026-11-23 kova kipi ekran denetimi (test bölümleri): bölüm başı, taşlar kovaya uçarken, yarım kova ("Gönder"), gönderim
    /// anı, yeni kova, tepside Altın Rulo + roket. Çıktı Logs/captures/kova/ (tools/unity.ps1 capture-bucket).
    /// </summary>
    public static class CaptureBucket
    {
        private const int W = 1080, H = 2340;

        public static void RunBatch()
        {
            int code = 0;
            try { Run(); }
            catch (Exception e) { Debug.LogError("[KatKat] kova yakalama hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "kova"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shot("01_baslangic", 0, gc => { });
            Shot("02_taslar_kovaya_ucuyor", 0, gc => { TapBucketGroup(gc); Run(gc, 700f); });
            Shot("02a_torba_az", 0, gc => { for (int i = 0; i < 2; i++) gc.Hud.BucketStoneIn(); gc.Hud.AdvanceBucket(2f); });
            Shot("02b_torbada_parcalar", 0, gc => { for (int i = 0; i < 6; i++) gc.Hud.BucketStoneIn(); gc.Hud.AdvanceBucket(2f); });
            Shot("02c_torba_dolu", 9, gc => { for (int i = 0; i < 12; i++) gc.Hud.BucketStoneIn(); gc.Hud.AdvanceBucket(3f); });
            Shot("03_kova_yarim_gonder", 1, gc => { TapBucketGroup(gc); Run(gc, 4000f); });
            Shot("04_gonderim_ani", 1, gc => { TapBucketGroup(gc); Run(gc, 4000f); gc.SendBucket(); Run(gc, 250f); });
            Shot("05_yeni_kova", 1, gc => { TapBucketGroup(gc); Run(gc, 4000f); gc.SendBucket(); Run(gc, 1500f); });
            Shot("06_rulo_roket_yuvasi", 6, gc =>
            {
                var s = gc.Session.State;
                s.Tray.Add(SpecialKind.Rainbow);
                s.Tray.Add(SpecialKind.RocketColumn);
                gc.Hud.SetTray(s.Tray, -1);
            });
            Shot("07_patron", 9, gc => { });
            Debug.Log("[KatKat] kova yakalamaları tamam → " + Path.Combine(PeelCapture.OutDir, "kova"));
        }

        private static void Shot(string name, int lab, Action<GameController> act)
        {
            GameCapture.ShotPublic($"kova/{name}.png", W, H, gc =>
            {
                gc.ShowPilots();
                gc.StartTrial(lab);
                gc.Hud.HideRuleCard(false);
                act(gc);
                UnityEngine.Canvas.ForceUpdateCanvases(); // torba yerleşimi son kanvas ölçeğiyle
                gc.Hud.AdvanceBucket(0f);
            });
        }

        private static void TapBucketGroup(GameController gc)
        {
            var s = gc.Session.State;
            var b = s.Board;
            var g = GroupFinder.AllComponents(b).Where(c => c.Count >= GroupFinder.MinGroup && b[c[0]].Top == s.Bucket.Color).OrderBy(c => c.Count < s.Bucket.Need ? 0 : 1).ThenBy(c => c.Count).FirstOrDefault();
            if (g == null) { Debug.LogWarning("[KatKat] kova renginde grup yok"); return; }
            gc.OnCellPressed(b.X(g[0]), b.Y(g[0]));
        }

        /// <summary>Tahta zaman çizgisi, soyulma uçuşları ve kova animasyonu birlikte ilerler.</summary>
        private static void Run(GameController gc, float ms)
        {
            for (float t = 0f; t < ms; t += 20f)
            {
                gc.Board.Advance(20f);
                gc.Board.Director.Advance(20f);
                gc.Hud.AdvanceBucket(0.02f);
            }
        }
    }
}
