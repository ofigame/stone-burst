using System;
using System.IO;
using KatKat.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v14 (2026-11-16) ekran denetimi: ilk 30 bölüm, örnek ileri bölümler (8×9, 6 renk, 4 kat), nefes bölümleri.
    /// Çıktı Logs/captures/v14/ (tools/unity.ps1 capture-v14).
    /// </summary>
    public static class CaptureV14
    {
        public static void RunBatch()
        {
            int code = 0;
            try { Run(); }
            catch (Exception e) { Debug.LogError("[KatKat] v14 yakalama hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v14"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const int W = 1080, H = 2340;
            for (int i = 0; i < 30; i++)
            {
                int ix = i;
                GameCapture.ShotPublic($"v14/bolum_{ix + 1:000}.png", W, H, gc => gc.StartLevel(ix));
            }
            foreach (var n in new[] { 50, 100, 150, 201, 300, 456, 500, 700, 906, 1000 })
            {
                int ix = n - 1;
                GameCapture.ShotPublic($"v14/ileri_{n:0000}.png", W, H, gc => gc.StartLevel(ix));
            }
            Debug.Log("[KatKat] v14 yakalamaları tamam → " + Path.Combine(PeelCapture.OutDir, "v14"));
        }
    }
}
