using System;
using System.IO;
using KatKat.Game;
using KatKat.UI;
using KatKat.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    // ---- Taş akışı animasyonu izleme videosu (2026-11-19; kullanıcı: "animasyonu görmek istiyorum video oluşturur musun") ----
    // Yeni akış (kırılma → havada bütün büyük taş → görev simgesine taş, yıldız çubuğuna yıldız; kombolarda çok taş) gerçek
    // oyundan, 30 fps, yazısız. Normal hızda sahneler + aynı temel hamlelerin 0,4× ağır çekimi.
    // Çıktı Logs/anim_demo/<sahne>/ (f_*.png, sfx.wav). Birleştirme: tools/anim-demo.ps1.
    public static partial class TrailerCapture
    {
        /// <summary>
        /// Yolculuk haritası hareketi videosu (2026-11-19; kullanıcı: "evet bahçeden başla"): bahçe bölgesinde (bölüm 100–199)
        /// 8 sn durgun izleme, sonra yavaş kaydırma. Çıktı Logs/map_demo/m1_bahce/; birleştirme tools/map-demo.ps1.
        /// </summary>
        [MenuItem("Kat Kat/Yakalama/Harita hareketi videosu kareleri")]
        public static void RunMapDemo()
        {
            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            _root = Path.Combine(logs, "map_demo");
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            Directory.CreateDirectory(_root);
            Fps = 30; _dense = true;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set("tr", false);
            GameController.ToursOverride = false;
            try
            {
                // 2026-11-19 (kullanıcı: "sahil ve çöl haritalarına da hareket ekle"): sahil 0–99, bahçe 100–199, çöl 200–299.
                // KATKAT_MAP_ONLY ile tek sahne (ör. "m3_col").
                var only = Environment.GetEnvironmentVariable("KATKAT_MAP_ONLY");
                foreach (var (name, cur) in new[] { ("m1_bahce", 162), ("m2_sahil", 62), ("m3_col", 262) })
                {
                    if (!string.IsNullOrEmpty(only) && only != name) continue;
                    TrailerCapture.Scene(name, (gc, rec) =>
                    {
                        gc.Menu.LivePressed = null; gc.Menu.PilotPressed = null; gc.Menu.ConceptPressed = null;
                        gc.Menu.ShowLevels(gc.Catalog.Count, cur, i => i < cur, () => { });
                        float y0 = gc.Menu.ScrollYForTest;
                        for (int f = 0; f < Fps * 8; f++) { gc.Menu.Tick(Dt / 1000f); rec(); }
                        int n = Fps * 7;
                        for (int f = 0; f <= n; f++)
                        {
                            gc.Menu.ScrollMapForTest(Mathf.Lerp(y0, y0 - 2600f, Ease(f / (float)n)));
                            gc.Menu.Tick(Dt / 1000f); rec();
                        }
                        for (int f = 0; f < Fps * 4; f++) { gc.Menu.Tick(Dt / 1000f); rec(); }
                    });
                }
            }
            finally
            {
                _dense = true;
                Loc.Set(keepLang, false);
                SafeAreaFitter.NormalizedOverride = null;
                GameController.ToursOverride = null;
            }
        }

        [MenuItem("Kat Kat/Yakalama/Taş akışı videosu kareleri")]
        public static void RunAnimDemo()
        {
            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            _root = Path.Combine(logs, "anim_demo");
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            Directory.CreateDirectory(_root);
            Fps = 30;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set("tr", false);
            GameController.ToursOverride = false;
            try
            {
                void Shot(string n, Action<GameController, Action> b) =>
                    TrailerCapture.Scene(n, (gc, rec) =>
                    {
                        float clock = 0f;
                        Action rec2 = () =>
                        {
                            clock += Dt / 1000f;
                            BgMotion.ClockOverride = clock;
                            var bd = gc.transform.Find("Backdrop");
                            var bm = bd != null ? bd.GetComponent<BgMotion>() : null;
                            if (bm != null) bm.Tick(clock);
                            rec();
                        };
                        b(gc, rec2);
                    });
                // PlayRange(bölüm 0 tabanlı, ilk adım, son adım (hariç), hız, bekleme sn, kahraman).
                Shot("s1_temel_l017", (gc, rec) => PlayRange(gc, rec, 16, 0, 4, 1f, 0.8f, -1));     // aynı renk patlamaları, görev
                Shot("s2_roket_l095", (gc, rec) => PlayRange(gc, rec, 94, 2, 5, 1f, 0.8f, -1));     // Roket
                Shot("s3_rulo_l174", (gc, rec) => PlayRange(gc, rec, 173, 0, 3, 1f, 1.0f, -1));     // Altın Rulo, büyük kombo
                Shot("s4_zafer_l042", (gc, rec) => PlayRange(gc, rec, 41, -3, 99, 1f, 0f, -1));     // son hamleler ve kutlama
                Shot("s5_agir_l017", (gc, rec) => PlayRange(gc, rec, 16, 0, 2, 0.4f, 1.0f, -1));    // ağır çekim (0,4×)
            }
            finally
            {
                BgMotion.ClockOverride = null;
                _dense = true;
                Loc.Set(keepLang, false);
            }
        }
    }
}
