using System.IO;
using KatKat.Game;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// 2026-11-28 açılış denetimi: ofigame açılışı ve Stone Burst zarf yükleme ekranı (ilerleme ve açılış anları) 1080×1920 PNG.
    /// Çıktı: Logs/yukleme/. Çalıştırma: tools/unity.ps1 capture-loading
    /// </summary>
    public static class CaptureLoading
    {
        [MenuItem("Kat Kat/Yakalama/Açılış ve yükleme ekranı")]
        public static void RunBatch()
        {
            var dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "yukleme");
            Directory.CreateDirectory(dir);
            CaptureV7.SplashTo(Path.Combine(dir, "01_ofigame.png"));
            Envelope(Path.Combine(dir, "02_zarf_yuzde_0.png"), 0f, -1f);
            Envelope(Path.Combine(dir, "03_zarf_yuzde_64.png"), 0.64f, -1f);
            Envelope(Path.Combine(dir, "04_zarf_yuzde_100.png"), 1f, -1f);
            Envelope(Path.Combine(dir, "05_acilis_0_15.png"), 1f, 0.15f);
            Envelope(Path.Combine(dir, "06_acilis_0_35.png"), 1f, 0.35f);
            Envelope(Path.Combine(dir, "07_acilis_0_55.png"), 1f, 0.55f);
            Envelope(Path.Combine(dir, "08_acilis_0_80.png"), 1f, 0.8f);
            Debug.Log("[KatKat] Açılış/yükleme kareleri → " + dir);
        }

        private static void Envelope(string path, float progress, float openAt)
        {
            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.8f, 0.55f); // ana sayfa yerine yeşil zemin (açılışta görünen alan)
            cam.transform.position = new Vector3(0, 0, -10);
            var rt = new RenderTexture(1080, 1920, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            var env = LoadingEnvelope.Create(cam);
            env.PoseForTest(progress, openAt);
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            rt.Release();
            if (env != null) Object.DestroyImmediate(env.gameObject);
            Object.DestroyImmediate(camGo);
        }
    }
}
