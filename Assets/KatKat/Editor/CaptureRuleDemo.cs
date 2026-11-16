using System;
using System.IO;
using KatKat.Game;
using KatKat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Kural kartı canlandırması ve yeni görseller (2026-11-16) kontrolü: deneme 1 (dar hamle), 4 (kıt renk), 7 (özel taş
    /// yok) kural kartları canlandırmanın birkaç anında; Tripo görselleri tahtada (ataç bölüm 21, dip yıldızı bölüm 41,
    /// kristal deneme 10). Çıktı docs/captures/kural/.
    /// </summary>
    public static class CaptureRuleDemo
    {
        public static void RunBatch()
        {
            int code = 0;
            try { Run(); }
            catch (Exception e) { Debug.LogError("[KatKat] Kural yakalama hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "kural"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const int W = 1080, H = 2340;
            KatKat.View.HazardPaint.ExportPng(Path.Combine(PeelCapture.OutDir, "kural", "gorsel"));
            var cards = new (int trial, string tag, float[] times)[]
            {
                (0, "dar_hamle", new[] { 0.6f, 1.25f, 3.2f }),
                (3, "kit_renk", new[] { 0.6f, 3.4f, 6.3f, 8.2f }),
                (6, "ozel_tas_yok", new[] { 0.7f, 1.7f, 2.0f }),
            };
            foreach (var c in cards)
                foreach (var t in c.times)
                {
                    var cc = c; float tt = t;
                    GameCapture.ShotPublic($"kural/{cc.tag}_{tt:0.0}s.png", W, H, gc => { RuleDemo.Freeze = tt; gc.ShowLevels(); gc.ShowTrials(); gc.StartTrial(cc.trial); });
                }
            RuleDemo.Freeze = -1f;
            GameCapture.ShotPublic("kural/tahta_atac_l21.png", W, H, gc => gc.StartLevel(20));
            GameCapture.ShotPublic("kural/tahta_yildiz_l41.png", W, H, gc => gc.StartLevel(40));
            GameCapture.ShotPublic("kural/tahta_kristal_d10.png", W, H, gc => { gc.ShowLevels(); gc.ShowTrials(); gc.StartTrial(9); gc.Hud.HideRuleCard(false); });
            Debug.Log("[KatKat] Kural yakalamaları tamam → " + Path.Combine(PeelCapture.OutDir, "kural"));
        }
    }
}
