using System;
using System.IO;
using KatKat.Core;
using KatKat.Game;
using KatKat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Konsept pilotu ekran kontrolü: harita girişi, liste, her konseptin başlangıcı ve kayıtlı çözümün yarısı (tehdit
    /// işaretleri ve durum satırı), bir kural kartı ve bir tehdit kaybı kartı. Çıktı docs/captures/konsept/.
    /// </summary>
    public static class CaptureConcept
    {
        public static void RunBatch()
        {
            int code = 0;
            try { Run(); }
            catch (Exception e) { Debug.LogError("[KatKat] Konsept yakalama hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "konsept"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Vr1Proto.Override = true;
            const int W = 1080, H = 2340;
            KatKat.View.HazardPaint.ExportPng(Path.Combine(PeelCapture.OutDir, "konsept", "gorsel"));
            GameCapture.ShotPublic("konsept/00_harita_girisi.png", W, H, gc => gc.ShowLevels());
            GameCapture.ShotPublic("konsept/01_liste.png", W, H, gc => { gc.ShowLevels(); gc.ShowConcepts(); });
            for (int k = 0; k < GameController.ConceptCount; k++)
            {
                int i = k;
                GameCapture.ShotPublic($"konsept/k{i + 1:00}_a_baslangic.png", W, H, gc => { gc.ShowLevels(); gc.ShowConcepts(); gc.StartTrial(i); gc.Hud.HideRuleCard(false); });
                GameCapture.ShotPublic($"konsept/k{i + 1:00}_b_ortasi.png", W, H, gc =>
                {
                    gc.ShowLevels(); gc.ShowConcepts(); gc.StartTrial(i); gc.Hud.HideRuleCard(false);
                    int n = gc.Trial(i).Solution.Count;
                    gc.ApplySolutionForTest(Math.Max(1, n / 2));
                });
            }
            GameCapture.ShotPublic("konsept/k01_kural_karti.png", W, H, gc => { gc.ShowLevels(); gc.ShowConcepts(); gc.StartTrial(0); });
            GameCapture.ShotPublic("konsept/k04_kayip_karti.png", W, H, gc =>
            {
                gc.ShowLevels(); gc.ShowConcepts(); gc.StartTrial(3); gc.Hud.HideRuleCard(false);
                var h = gc.Session.State.Hazard;
                h.Lost = true; h.Loss = HazardLoss.Raven;
                gc.ShowLostForCapture(GameStatus.LostHazard);
                CaptureV13Pages.Panels(gc);
            });
            Vr1Proto.Override = null;
            Debug.Log("[KatKat] Konsept yakalamaları tamam → " + Path.Combine(PeelCapture.OutDir, "konsept"));
        }
    }
}
