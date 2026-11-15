using System;
using System.IO;
using System.Text;
using KatKat.Game;
using KatKat.Levels;
using KatKat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// VR2 oyun ekranı kontrolü (docs/vr2/CLAUDE_CODE.md "Kabul kontrolü"): altı tema × 19.5:9 / 20:9 telefon ve dikey
    /// 4:3 tablet (+ 16:9 telefon ve çentikli güvenli alan). Her tema kendi bölgesinden, maskesinde pasif hücre ya da iç
    /// boşluk olan gerçek bir bölümle çekilir (mevcut maske; yeni şekil üretilmez). Çıktı docs/captures/vr2/ ve ölçüm
    /// raporu OLCUM.md (iç alan rengi, maske dışı saydamlık, üst/alt iç kenar gölgesi, hedef yuvası ile panel arası).
    /// </summary>
    public static class CaptureVr2
    {
        private static readonly (string tag, int w, int h, Rect? safe)[] Sizes =
        {
            ("telefon_19.5x9_1080x2340", 1080, 2340, null),
            ("telefon_20x9_1080x2400", 1080, 2400, null),
            ("tablet_4x3_1536x2048", 1536, 2048, null),
        };

        public static void RunBatch()
        {
            int code = 0;
            try { Run(); }
            catch (Exception e) { Debug.LogError("[KatKat] VR2 yakalama hata: " + e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, "vr2"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var report = new StringBuilder("# VR2 ölçüm raporu\n\n| Ekran | Bölüm | Tema | İç renk (beklenen) | İç renk (doku ortası) | Maske dışı alfa | Üst iç kenar | Alt iç kenar |\n|---|---|---|---|---|---|---|---|\n");
            var catalog = LevelCatalog.Load();
            for (int skin = 0; skin < Vr2.Skins.Length; skin++)
            {
                int level = PickLevel(catalog, skin);
                foreach (var s in Sizes)
                {
                    string file = $"vr2/{skin + 1:00}_{Vr2.Skins[skin].Id}_{s.tag}_l{level + 1:000}.png";
                    GameController.Vr2SkinOverride = skin;
                    GameCapture.ShotPublic(file, s.w, s.h, s.safe, gc =>
                    {
                        CaptureV13Pages.Progress(gc, level);
                        gc.StartLevel(level);
                        gc.Hud.HideRuleCard(false);
                        if (s.tag.StartsWith("telefon_20x9")) Measure(report, file, level, skin, gc);
                    });
                }
            }
            // Çentikli telefon (güvenli alan üstte %5, altta %3) ve 16:9: panel rengi çentiğe uzanır, düğmeler içeride.
            GameController.Vr2SkinOverride = 3;
            int l3 = PickLevel(catalog, 3);
            GameCapture.ShotPublic($"vr2/ek_centik_1080x2400_l{l3 + 1:000}.png", 1080, 2400, new Rect(0f, 0.03f, 1f, 0.92f), gc => { CaptureV13Pages.Progress(gc, l3); gc.StartLevel(l3); gc.Hud.HideRuleCard(false); });
            GameController.Vr2SkinOverride = 0;
            GameCapture.ShotPublic("vr2/ek_16x9_1080x1920_l005.png", 1080, 1920, null, gc => { gc.StartLevel(4); gc.Hud.HideRuleCard(false); });
            // Boş hücre yuvası (kullanıcı 2026-11-14): bazı hücrelerin taşları yalnız görünümde kaldırılır (durum değişmez).
            for (int sk = 0; sk < Vr2.Skins.Length; sk += 3)
            {
                int skin = sk, lv = PickLevel(catalog, sk);
                GameController.Vr2SkinOverride = skin;
                GameCapture.ShotPublic($"vr2/ek_bos_hucre_{skin + 1:00}_1080x2340_l{lv + 1:000}.png", 1080, 2340, null, gc =>
                {
                    CaptureV13Pages.Progress(gc, lv); gc.StartLevel(lv); gc.Hud.HideRuleCard(false);
                    int k = 0;
                    foreach (var st in gc.Board.GetComponentsInChildren<KatKat.View.StackView>(false))
                        if (st.Layers.Count > 0 && (k++ % 3 == 1)) st.SetLayers(new KatKat.Core.LayerColor[0]);
                });
            }
            // Ortası boş tahta (kullanıcı 2026-11-15: boşluğa obje konmaz; yalnız bulanık arka plan).
            int hollow = -1;
            for (int i = 100; i < Math.Min(400, catalog.Count) && hollow < 0; i++)
            {
                var hole = BoardHoles.Largest(LevelLoader.ToState(catalog.Levels[i]).Board);
                if (hole.Has && hole.Side >= 2) hollow = i;
            }
            if (hollow >= 0)
            {
                GameController.Vr2SkinOverride = Vr2.SkinFor(hollow);
                GameCapture.ShotPublic($"vr2/ek_ortasi_bos_1080x2340_l{hollow + 1:000}.png", 1080, 2340, null, gc => { CaptureV13Pages.Progress(gc, hollow); gc.StartLevel(hollow); gc.Hud.HideRuleCard(false); });
            }
            // Beş hedefli bölüm (yuvaya sığma).
            int many = PickMany(catalog);
            if (many >= 0)
            {
                GameController.Vr2SkinOverride = Vr2.SkinFor(many);
                GameCapture.ShotPublic($"vr2/ek_cok_hedef_1080x2340_l{many + 1:000}.png", 1080, 2340, null, gc => { CaptureV13Pages.Progress(gc, many); gc.StartLevel(many); gc.Hud.HideRuleCard(false); });
            }
            GameController.Vr2SkinOverride = null;
            File.WriteAllText(Path.Combine(PeelCapture.OutDir, "vr2", "OLCUM.md"), report.ToString());
            Debug.Log("[KatKat] VR2 yakalamaları tamam → " + Path.Combine(PeelCapture.OutDir, "vr2"));
        }

        /// <summary>Temanın bölgesinden (bölge = 100 bölüm), maskesinde pasif hücre olan ilk bölüm (11. bölümden sonra).</summary>
        private static int PickLevel(LevelCatalog catalog, int skin)
        {
            int[] regionOf = { 0, 1, 2, 3, 5, 6 };
            int start = regionOf[skin] * 100;
            int fallback = start + 10;
            for (int i = start + 10; i < Math.Min(start + 100, catalog.Count); i++)
            {
                var st = LevelLoader.ToState(catalog.Levels[i]);
                int inactive = 0;
                for (int k = 0; k < st.Board.Count; k++) { var c = st.Board[k]; if (!c.Active && c.GateId <= 0) inactive++; }
                if (inactive >= 2 && st.Goals.Count >= 2) return i;
            }
            return Math.Min(fallback, catalog.Count - 1);
        }

        private static int PickMany(LevelCatalog catalog)
        {
            int best = -1, bestN = 0;
            for (int i = 10; i < Math.Min(400, catalog.Count); i++)
            {
                int g = catalog.Levels[i].Goals.Count;
                if (g > bestN) { bestN = g; best = i; }
                if (g >= 5) return i;
            }
            return best;
        }

        private static void Measure(StringBuilder report, string file, int level, int skin, GameController gc)
        {
            var board = gc.Board;
            var sp = board.FloorSprite;
            if (sp == null) { report.AppendLine($"| {file} | {level + 1} | {Vr2.Skins[skin].Name} | — | doku yok | | | |"); return; }
            var tex = sp.texture;
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            // Doku ortasından aşağı/yukarı tarayarak iç alanın en açık (gölgesiz) pikseli: beklenen floorColor.
            Color32 best = default; int bestSum = -1;
            float alphaOut = 0f;
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a == 255) { int sum = px[i].r + px[i].g + px[i].b; if (sum > bestSum) { bestSum = sum; best = px[i]; } }
            }
            alphaOut = Mathf.Max(px[0].a, px[w - 1].a, px[(h - 1) * w].a, px[px.Length - 1].a) / 255f;
            // Ortadaki sütunda ilk/son opak pikselin 6 px içi: üst iç kenar koyu, alt iç kenar daha açık olmalı.
            int cx = w / 2, top = -1, bottom = -1;
            for (int y = h - 1; y >= 0; y--) if (px[y * w + cx].a == 255) { top = y; break; }
            for (int y = 0; y < h; y++) if (px[y * w + cx].a == 255) { bottom = y; break; }
            string Lum(int y) => y < 0 || y >= h ? "—" : (0.2126f * px[y * w + cx].r + 0.7152f * px[y * w + cx].g + 0.0722f * px[y * w + cx].b).ToString("0");
            var exp = Vr2.InnerFloor(skin);
            report.AppendLine($"| {file} | {level + 1} | {Vr2.Skins[skin].Name} | #{exp.r:X2}{exp.g:X2}{exp.b:X2} | #{best.r:X2}{best.g:X2}{best.b:X2} | {alphaOut:0.00} | {Lum(top - 6)} | {Lum(bottom + 6)} |");
        }
    }
}
