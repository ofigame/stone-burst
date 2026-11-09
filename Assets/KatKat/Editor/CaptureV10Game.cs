using System;
using System.IO;
using KatKat.Core;
using KatKat.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v10 oyun içi kabuk yakalamaları (docs/v10/TriKat-Oyun-Ici-Claude). Aynı bölüm, aynı durum ve boyutta
    /// önce/sonra: bölüm 38 (açılış), bölüm 38 soyulma karesi, bölüm 219 (5 hedef, 7×9) tepsi dolu.
    /// Çıktı: docs/captures/v10/oyun/&lt;faz&gt;_*.png. Bölüm dosyalarına ve kayda yazılmaz.
    /// </summary>
    public static class CaptureV10Game
    {
        private static readonly (string tag, int w, int h, Rect? safe)[] Sizes =
        {
            ("941x1672", 941, 1672, null),
            ("1080x1920", 1080, 1920, null),
            ("1080x2400_centik", 1080, 2400, new Rect(0f, 48f / 2400f, 1f, (2400f - 110f - 48f) / 2400f)),
        };

        public static void Before() => Capture("once");
        public static void After() => Capture("sonra");

        private static void Capture(string phase)
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "v10", "oyun"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = KatKat.UI.Loc.Code;
            KatKat.UI.Loc.Set("tr", false);
            GameController.ToursOverride = false;
            try
            {
                foreach (var s in Sizes)
                    Shot($"v10/oyun/{phase}_l038_{s.tag}.png", s, gc => gc.StartLevel(37));
                // Soyulma karesi: ilk adım oynanır, animasyon ortasında durdurulur.
                Shot($"v10/oyun/{phase}_l038_soyulma.png", Sizes[1], gc =>
                {
                    gc.StartLevel(37);
                    gc.PlayStep(gc.CurrentLevel.Solution[0]);
                    for (int k = 0; k < 12; k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
                });
                // v10 taş kırılması: aynı hamlenin dört anı (çatlak, kırılma, dağılma, hedefe uçuş).
                foreach (int ms in new[] { 96, 176, 400, 720 })
                    Shot($"v10/oyun/{phase}_kirilma_{ms:000}ms.png", Sizes[1], gc =>
                    {
                        gc.StartLevel(37);
                        gc.PlayStep(gc.CurrentLevel.Solution[0]);
                        for (int k = 0; k < ms / 16; k++) { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
                    });
                // 5 hedef, farklı tahta, tepsi dolu (yalnız görüntü için oturum durumuna yazılır).
                Action<GameController> l219 = gc =>
                {
                    gc.StartLevel(218);
                    var st = gc.Session.State;
                    st.Tray.Clear();
                    st.Tray.Add(SpecialKind.RocketRow);
                    st.Tray.Add(SpecialKind.Rainbow);
                    gc.Hud.SetTrayVisible(true);
                    gc.Hud.SetTray(st.Tray, 1);
                };
                foreach (var s in Sizes) Shot($"v10/oyun/{phase}_l219_{s.tag}.png", s, l219);
            }
            finally
            {
                GameController.ToursOverride = null;
                KatKat.UI.Loc.Set(keepLang, false);
            }
        }

        private static void Shot(string file, (string tag, int w, int h, Rect? safe) s, Action<GameController> act) =>
            GameCapture.ShotPublic(file, s.w, s.h, s.safe, act);
    }
}
