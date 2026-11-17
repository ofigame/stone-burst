using System;
using System.IO;
using KatKat.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Ahşap tepsi (TrayArt, kullanıcı 2026-11-17) ekran denetimi: çeşitli şekilli bölümler, aynı bölümlerin taşsız hâli
    /// (yuvalar ve kenar görünsün). Çıktı docs/captures/tepsi/ (tools/unity.ps1 capture-trayart).
    /// </summary>
    public static class CaptureTrayArt
    {
        public static void RunBatch()
        {
            int code = 0;
            try { Run(); }
            catch (Exception e) { Debug.LogError("[KatKat] tepsi yakalama hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "tepsi"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const int W = 1080, H = 2340;
            var env = Environment.GetEnvironmentVariable("KATKAT_TRAY_LEVELS");
            var levels = string.IsNullOrEmpty(env) ? new[] { 1, 8, 21, 40, 61, 150, 300, 700 } : Array.ConvertAll(env.Split(','), int.Parse);
            foreach (var n in levels)
            {
                int ix = n - 1;
                GameCapture.ShotPublic($"tepsi/bolum_{n:0000}.png", W, H, gc => gc.StartLevel(ix));
                GameCapture.ShotPublic($"tepsi/bolum_{n:0000}_bos.png", W, H, gc =>
                {
                    gc.StartLevel(ix);
                    var b = gc.Board;
                    for (int y = 0; y < b.Height; y++)
                    for (int x = 0; x < b.Width; x++) b.StackAt(x, y).gameObject.SetActive(false);
                    if (Environment.GetEnvironmentVariable("KATKAT_TRAY_DUMP") == "1")
                    {
                        var tex = b.FloorSprite != null ? b.FloorSprite.texture : null;
                        if (tex != null) File.WriteAllBytes(Path.Combine(PeelCapture.OutDir, "tepsi", $"doku_{n:0000}.png"), tex.EncodeToPNG());
                        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                            if (r.bounds.size.x > 2f) Debug.Log($"[KatKat] R {PathOf(r.transform)} order={r.sortingOrder} layer={r.sortingLayerName} size={r.bounds.size} sprite={(r is SpriteRenderer s ? s.sprite?.name : r.sharedMaterial?.name)}");
                        foreach (var g in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                            if (g.rectTransform.rect.width > 600f) Debug.Log($"[KatKat] G {PathOf(g.transform)} rect={g.rectTransform.rect.size} tex={g.mainTexture?.name}");
                    }
                });
            }
            Debug.Log($"[KatKat] tepsi yakalamaları tamam (son pişirme {BoardView.LastTrayBakeMs} ms) → " + Path.Combine(PeelCapture.OutDir, "tepsi"));
        }
    }
}
