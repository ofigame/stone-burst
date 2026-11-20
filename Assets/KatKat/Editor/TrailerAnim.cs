using System;
using System.Collections.Generic;
using System.IO;
using KatKat.Core;
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

        /// <summary>
        /// Mağaza ekran görüntüleri (2026-11-20): harita üst kısmı (külçe kapsülü, hediye işareti), mağaza, koleksiyon
        /// hediyesi, oyun içi güçlendirici şeridi ve "Bomba bitti" penceresi. Her sahnenin son karesi Logs/shop_shots/&lt;ad&gt;.png.
        /// </summary>
        [MenuItem("Kat Kat/Yakalama/Mağaza ekranları")]
        public static void RunShopShots()
        {
            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            _root = Path.Combine(logs, "shop_shots");
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            Directory.CreateDirectory(_root);
            Fps = 30; _dense = true;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set("tr", false);
            GameController.ToursOverride = false;
            void SeeAll(GameController gc, int index)
            {
                var seen = gc.SaveService.Data.meta.seenTypes;
                foreach (var t in new[] { "aha", "bridge", "chain", "compact", "exact", "order", "protect", "pure", "resource", "reverse", "scarce", "seal", "seal_room", "stages", "twin", "twin_seal", "twin_stages", "wide" })
                    if (!seen.Contains(t)) seen.Add(t);
                seen.Add("boss@" + gc.Catalog.IdAt(index));
            }
            void Hold(GameController gc, Action rec, int frames) { for (int f = 0; f < frames; f++) { gc.Menu.Tick(Dt / 1000f); TickAll(gc, Dt); rec(); } }
            try
            {
                TrailerCapture.Scene("s1_harita", (gc, rec) =>
                {
                    gc.Menu.LivePressed = null; gc.Menu.PilotPressed = null; gc.Menu.ConceptPressed = null;
                    gc.AddIngotsForTest(350);
                    gc.SaveService.Data.meta.stones.Add("stone_tide");
                    gc.ShowLevels();
                    Hold(gc, rec, 20);
                });
                // 2026-11-20 bölge simgesi denetimi: Mavi Liman (fener ışığı) ve Yeşil Vadi (çeşme suyu).
                foreach (var (name, hi) in new[] { ("s1b_liman", 40), ("s1c_vadi", 140) })
                    TrailerCapture.Scene(name, (gc, rec) =>
                    {
                        gc.Menu.LivePressed = null; gc.Menu.PilotPressed = null; gc.Menu.ConceptPressed = null;
                        gc.SaveService.Data.highestUnlocked = hi;
                        gc.ShowLevels();
                        Hold(gc, rec, 4);
                    });
                TrailerCapture.Scene("s2_magaza", (gc, rec) =>
                {
                    gc.AddIngotsForTest(350);
                    gc.ShowLevels(); gc.ShowShop();
                    Hold(gc, rec, 12);
                });
                TrailerCapture.Scene("s3_magaza_alt", (gc, rec) =>
                {
                    gc.AddIngotsForTest(350);
                    gc.ShowLevels(); gc.ShowShop();
                    var sr = UnityEngine.Object.FindAnyObjectByType<UnityEngine.UI.ScrollRect>();
                    if (sr != null) { Canvas.ForceUpdateCanvases(); sr.verticalNormalizedPosition = 0f; }
                    Hold(gc, rec, 12);
                });
                TrailerCapture.Scene("s4_koleksiyon", (gc, rec) =>
                {
                    gc.SaveService.Data.meta.stones.Add("stone_tide");
                    gc.SaveService.Data.meta.stones.Add("stone_ocean");
                    gc.ShowLevels(); gc.ShowCollection();
                    Hold(gc, rec, 20);
                });
                TrailerCapture.Scene("s5_oyun", (gc, rec) =>
                {
                    SeeAll(gc, 29);
                    gc.StartLevel(29);
                    for (float t = 0; t < 600f; t += Dt) { gc.Hud.AdvanceVeil(Dt); TickAll(gc, Dt); }
                    Hold(gc, rec, 12);
                });
                TrailerCapture.Scene("s7_serit", (gc, rec) =>
                {
                    // Dolu tepsi + güçlendiriciler (2026-11-20: "skiller üst üste biniyor" düzeltmesinin denetimi).
                    SeeAll(gc, 29);
                    gc.StartLevel(29);
                    for (float t = 0; t < 600f; t += Dt) { gc.Hud.AdvanceVeil(Dt); TickAll(gc, Dt); }
                    var tray = gc.Session.State.Tray;
                    tray.Clear();
                    tray.Add(KatKat.Core.SpecialKind.RocketRow); tray.Add(KatKat.Core.SpecialKind.Rainbow);
                    tray.Add(KatKat.Core.SpecialKind.RocketColumn); tray.Add(KatKat.Core.SpecialKind.Rainbow);
                    gc.Hud.SetTray(tray, -1);
                    Hold(gc, rec, 12);
                });
                TrailerCapture.Scene("s6_bitti", (gc, rec) =>
                {
                    SeeAll(gc, 29);
                    gc.StartLevel(29);
                    for (float t = 0; t < 600f; t += Dt) { gc.Hud.AdvanceVeil(Dt); TickAll(gc, Dt); }
                    gc.SaveService.Data.meta.boostBomb = 0;
                    gc.AddIngotsForTest(120);
                    gc.PressBoosterForTest(KatKat.Meta.Booster.Bomb);
                    Hold(gc, rec, 12);
                });
            }
            finally
            {
                _dense = true;
                Loc.Set(keepLang, false);
                SafeAreaFitter.NormalizedOverride = null;
                GameController.ToursOverride = null;
            }
        }

        /// <summary>
        /// Güçlendirici efektleri videosu (2026-11-20; kullanıcı: "onların efektleri nasıl merak ediyorum"): bölüm 30'da
        /// +3 Hamle, Bomba (3×3 patlama) ve Taş Değiştir (3 taş). Çıktı Logs/booster_demo/b1/; birleştirme tools/booster-demo.ps1.
        /// </summary>
        [MenuItem("Kat Kat/Yakalama/Güçlendirici efektleri videosu")]
        public static void RunBoosterDemo()
        {
            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            _root = Path.Combine(logs, "booster_demo");
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            Directory.CreateDirectory(_root);
            Fps = 30; _dense = true;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set("tr", false);
            GameController.ToursOverride = false;
            try
            {
                TrailerCapture.Scene("b1", (gc, rec) =>
                {
                    const int index = 29;
                    var seen = gc.SaveService.Data.meta.seenTypes;
                    foreach (var t in new[] { "aha", "bridge", "chain", "compact", "exact", "order", "protect", "pure", "resource", "reverse", "scarce", "seal", "seal_room", "stages", "twin", "twin_seal", "twin_stages", "wide" })
                        if (!seen.Contains(t)) seen.Add(t);
                    seen.Add("boss@" + gc.Catalog.IdAt(index));
                    gc.StartLevel(index);
                    for (float t = 0; t < 600f; t += Dt) { gc.Hud.AdvanceVeil(Dt); TickAll(gc, Dt); }
                    void Frames(float sec) { for (int f = 0; f < Fps * sec; f++) { TickAll(gc, Dt); rec(); } }
                    void UntilStable()
                    {
                        int g = 0;
                        while ((gc.CurrentPhase == GameController.Phase.Animating || gc.Board.IsPlaying) && g++ < Fps * 10) { TickAll(gc, Dt); rec(); }
                    }
                    Frames(1.0f);
                    // 1) +3 Hamle
                    gc.PressBoosterForTest(KatKat.Meta.Booster.Moves);
                    Frames(1.6f);
                    // 2) Bomba: tahtanın ortasına yakın seçilebilir taş.
                    var b = gc.Session.State.Board;
                    int bx = b.Width / 2, by = b.Height / 2;
                    for (int r = 0; r < 4 && !b[bx, by].IsSelectable; r++) { bx = Mathf.Clamp(bx + 1, 0, b.Width - 1); by = Mathf.Clamp(by + (r % 2 == 0 ? 1 : -1), 0, b.Height - 1); }
                    gc.PressBoosterForTest(KatKat.Meta.Booster.Bomb);
                    Frames(0.8f);
                    gc.OnCellPressed(bx, by);
                    UntilStable();
                    Frames(1.2f);
                    // 3) Taş Değiştir: üç taş, tek renk.
                    b = gc.Session.State.Board;
                    var picks = new List<Coord>();
                    for (int i = 0; i < b.Count && picks.Count < 3; i++)
                        if (GameSession.CanRecolor(b[i]) && b[i].Top != LayerColor.Green) picks.Add(new Coord(b.X(i), b.Y(i)));
                    gc.PressBoosterForTest(KatKat.Meta.Booster.Recolor);
                    Frames(0.8f);
                    foreach (var p in picks) { gc.OnCellPressed(p.X, p.Y); Frames(0.45f); }
                    Frames(0.5f);
                    gc.ChooseRecolorForTest(LayerColor.Green);
                    Frames(2.0f);
                });
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
