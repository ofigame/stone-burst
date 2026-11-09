using System.Collections.Generic;
using System.IO;
using KatKat.Core;
using KatKat.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Soyulma animasyonunu belirli zamanlarda gerçekten render edip PNG olarak kaydeder.
    /// Batch: -executeMethod KatKat.EditorTools.PeelCapture.CaptureAll  → docs/captures/
    /// </summary>
    public static class PeelCapture
    {
        public const string OutDir = "docs/captures";
        private static readonly float[] StoryTimes = { 0f, 60f, 130f, 210f, 290f, 380f, 470f, 650f };

        [MenuItem("Kat Kat/Yakalama/Soyulma kareleri")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1) Zaman şeridi: 9:16, ilk düzen (coral üstte, blue orta, yellow alt).
            var frames = new List<Texture2D>();
            foreach (var t in StoryTimes) frames.Add(RenderLab(432, 768, 0, 0, t, cropTop: true));
            SaveSheet(frames, 4, Path.Combine(OutDir, "peel_timeline_9x16.png"));

            // 2) Dört renk: her düzenin üstü soyulurken aynı an (290 ms).
            frames.Clear();
            for (int p = 0; p < PeelLab.Presets.Length; p++) frames.Add(RenderLab(432, 768, p, 0, 290f, cropTop: true));
            SaveSheet(frames, 4, Path.Combine(OutDir, "peel_four_colors.png"));

            // 3) İkinci soyulma (blue üstte → yellow açılıyor) zaman şeridi.
            frames.Clear();
            foreach (var t in StoryTimes) frames.Add(RenderLab(432, 768, 0, 1, t, cropTop: true));
            SaveSheet(frames, 4, Path.Combine(OutDir, "peel_timeline_second_layer.png"));

            // 4) Farklı en-boy oranlarında tam ekran.
            SavePng(RenderLab(1080, 1920, 0, 0, 290f, false), Path.Combine(OutDir, "lab_9x16_1080x1920.png"));
            SavePng(RenderLab(1080, 2400, 0, 0, 290f, false), Path.Combine(OutDir, "lab_20x9_1080x2400.png"));
            SavePng(RenderLab(1536, 2048, 0, 0, 290f, false), Path.Combine(OutDir, "lab_tablet_1536x2048.png"));

            Debug.Log("[KatKat] PeelCapture tamam → " + Path.GetFullPath(OutDir));
        }

        /// <param name="preset">PeelLab düzeni.</param>
        /// <param name="prePeels">Yakalanan soyulmadan önce anında tamamlanan soyulma sayısı.</param>
        private static Texture2D RenderLab(int w, int h, int preset, int prePeels, float ms, bool cropTop)
        {
            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ProjectSetup.Ivory;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = (float)w / h;

            var lab = new GameObject("PeelLab").AddComponent<PeelLab>();
            lab.Build(cam);
            lab.Stack.SetLayers(PeelLab.Presets[preset]);
            for (int i = 0; i < prePeels; i++) lab.PeelTop().Complete();
            var seq = lab.PeelTop();
            seq.Evaluate(ms);

            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex;
            if (cropTop)
            {
                // Hedef simgeleri + yığın: üst %75.
                int ch = Mathf.RoundToInt(h * 0.72f);
                tex = new Texture2D(w, ch, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, h - ch - Mathf.RoundToInt(h * 0.06f), w, ch), 0, 0);
            }
            else
            {
                tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            }
            tex.Apply();
            RenderTexture.active = prev;

            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(lab.gameObject);
            Object.DestroyImmediate(camGo);
            return tex;
        }

        private static void SaveSheet(List<Texture2D> frames, int cols, string path)
        {
            int fw = frames[0].width, fh = frames[0].height, gap = 6;
            int rows = (frames.Count + cols - 1) / cols;
            var sheet = new Texture2D(cols * fw + (cols + 1) * gap, rows * fh + (rows + 1) * gap, TextureFormat.RGB24, false);
            var fill = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(0x9A, 0x8E, 0x80, 0xFF);
            sheet.SetPixels32(fill);
            for (int i = 0; i < frames.Count; i++)
            {
                int cx = i % cols, cy = rows - 1 - i / cols;
                sheet.SetPixels(gap + cx * (fw + gap), gap + cy * (fh + gap), fw, fh, frames[i].GetPixels());
                Object.DestroyImmediate(frames[i]);
            }
            sheet.Apply();
            SavePng(sheet, path);
        }

        private static void SavePng(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
