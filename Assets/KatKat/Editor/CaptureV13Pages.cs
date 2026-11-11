using System;
using System.IO;
using System.Reflection;
using KatKat.Core;
using KatKat.Game;
using KatKat.Meta;
using KatKat.UI;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v13: oyundaki bütün sayfalardan birer örnek (kullanıcı, 2026-11-08). 1080×1920, Türkçe, geçici kayıt
    /// (canlı kayda ve bölüm dosyalarına yazılmaz). Çıktı: docs/v13/NN_sayfa.png.
    /// </summary>
    public static class CaptureV13Pages
    {
        private static string Out = "../v13/"; // PeelCapture.OutDir (docs/captures) göreli → docs/v13

        /// <summary>ui1 uygulama sonuçları: aynı sayfalar docs/ui1/uygulama_sonuclari klasörüne.</summary>
        public static void RunUi1() { Out = "../ui1/uygulama_sonuclari/"; try { Run(); } finally { Out = "../v13/"; } }

        /// <summary>
        /// ui2 (kullanıcı, 2026-11-08: "tüm sayfaların ve modal olarak açılan ekranların tamının ekran görüntüsü"):
        /// 01–20 aynı liste + 21–33 modal/kart ekranları. Çıktı docs/ui2/.
        /// </summary>
        public static void RunUi2()
        {
            Out = "../ui2/";
            try
            {
                Run();
                Extras();
            }
            finally { Out = "../v13/"; }
        }

        /// <summary>
        /// ui3 uygulama sonuçları (docs/ui3_onayli_tasarim): 35 ekran docs/ui3_uygulama/. KATKAT_CAPTURE_ONLY ortam
        /// değişkeni (ör. "01,12,14") verilirse yalnız o numaralar.
        /// </summary>
        public static void RunUi3()
        {
            Out = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KATKAT_CAPTURE_LANG")) ? "../ui3_uygulama/" : "../ui3_uygulama/diller/" + CapLang + "/";
            var only = Environment.GetEnvironmentVariable("KATKAT_CAPTURE_ONLY");
            _only = string.IsNullOrEmpty(only) ? null : only.Split(',');
            try
            {
                Run();
                Extras();
            }
            finally { Out = "../v13/"; _only = null; }
        }

        /// <summary>vr1 belgeleri (kullanıcı, 2026-11-11: her ekranı anlatan belge): aynı ekranlar docs/vr1/ekranlar/.</summary>
        public static void RunVr1()
        {
            Out = "../vr1/ekranlar/";
            try
            {
                Run();
                Extras();
            }
            finally { Out = "../v13/"; }
        }

        /// <summary>
        /// vr1 P1 inceleme ekranları (docs/vr1/p1_inceleme/): prototip anahtarı kapalı/açık harita, buz evreleri 0–3,
        /// haritaya dönüş (yıldız uçuşu ortası ve sonu), taş açılışında kabuk kırılması ve vitrin, bölüm başı bilgisi.
        /// </summary>
        public static void RunVr1P1()
        {
            Out = "../vr1/p1_inceleme/";
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, Out));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set(CapLang, false);
            GameController.ToursOverride = false;
            GameController.ClockOverride = new FixedDayClock { DayId = "2026-11-08" };
            try
            {
                Vr1Proto.Override = false;
                Shot("p1_01_harita_prototip_kapali", gc => { Progress(gc, 12); gc.ShowLevels(); });
                Vr1Proto.Override = true;
                // Evreler: ilk yıldız taşı hedefinde toplam yıldız → p ≈ 0,08 / 0,33 / 0,58 / 0,92.
                int[] totals = { 5, 20, 35, 55 };
                for (int e = 0; e < totals.Length; e++)
                {
                    int total = totals[e];
                    Shot($"p1_02_buz_evre{e}", gc => { Progress(gc, 2); TopUp(gc, total); Seen(gc); gc.ShowLevels(); });
                }
                Shot("p1_03_donus_ucus", gc => { Progress(gc, 12); Seen(gc, -6, -2); gc.ShowLevels(); gc.Menu.Vr1Fx?.Advance(420f); });
                Shot("p1_04_donus_sonu", gc => { Progress(gc, 12); Seen(gc, -6, -2); gc.ShowLevels(); gc.Menu.Vr1Fx?.Advance(1800f); });
                Shot("p1_05_acilis_kirilma", gc => { Unlock(gc); gc.ShowLevels(); gc.Menu.Vr1Fx?.Advance(1600f); });
                Shot("p1_06_acilis_vitrin", gc => { Unlock(gc); gc.ShowLevels(); gc.Menu.Vr1Fx?.Advance(3500f); gc.Showcase.Advance(1500f); });
                Shot("p1_07_bolum_basi_bilgi", gc => { Progress(gc, 12); TopUp(gc, 58); Seen(gc); gc.StartLevel(12); });
                Shot("p1_08_gunluk_sekme_secili", gc => { Progress(gc, 12); Own(gc); gc.ShowLevels(); gc.ShowDaily(); });
            }
            finally
            {
                Vr1Proto.Override = null;
                GameController.ToursOverride = null;
                GameController.ClockOverride = null;
                Out = "../v13/";
                Loc.Set(keepLang, false);
            }
        }

        /// <summary>Toplam yıldızı günlük yıldızla <paramref name="total"/>'a tamamlar.</summary>
        private static void TopUp(GameController gc, int total)
        {
            var d = gc.SaveService.Data;
            d.meta.dailyStars = 0;
            d.meta.dailyStars = Math.Max(0, total - d.TotalStars);
        }

        /// <summary>Haritanın son gördüğü değerler: bugünkü + fark (dönüş animasyonu için negatif).</summary>
        private static void Seen(GameController gc, int stars = 0, int levels = 0)
        {
            var d = gc.SaveService.Data;
            d.meta.mapSeenStars = d.TotalStars + stars;
            d.meta.mapSeenLevels = d.completed.Count + levels;
        }

        /// <summary>58 yıldızda görülmüş harita, sonra 61 yıldız: 60 eşikli taş bu dönüşte açılır.</summary>
        private static void Unlock(GameController gc)
        {
            Progress(gc, 12);
            Own(gc);
            TopUp(gc, 58);
            Seen(gc);
            TopUp(gc, 61);
            Call(gc, "CheckStoneUnlocks");
        }

        /// <summary>
        /// Varlık kullanım taraması (ui3, APK küçültme): 35 ekran + her mekanik bloğundan bir bölümün çözümü sonuna kadar +
        /// her bölgenin haritası (ilerleme 1, 120, 220 → sahil/bahçe/çöl) + koleksiyon sonuna kadar kaydırma + her taşın
        /// vitrini. Resources'tan istenen yollar Logs/resource_usage_ui3.txt.
        /// </summary>
        public static void UsageUi3()
        {
            KatKat.View.ResourceUsage.Paths.Clear();
            Out = "../ui3_uygulama/kullanim/";
            try
            {
                Run();
                Extras();
                Loc.Set("tr", false);
                foreach (int n in new[] { 1, 2, 3, 4, 5, 6, 9, 21, 41, 61, 121, 201, 301, 401, 501, 601, 701, 1000 })
                {
                    int idx = n - 1;
                    Shot($"m{n:000}_son", gc =>
                    {
                        if (idx >= gc.Catalog.Count) return;
                        gc.StartLevel(idx);
                        gc.Hud.HideRuleCard(false);
                        gc.Board.Director.TimeScale = 50f;
                        foreach (var step in gc.Catalog.Levels[idx].Solution) { gc.PlayStep(step); Drain(gc); }
                        if (gc.IsCelebrating) Call(gc, "SkipCelebration");
                        Drain(gc);
                        Panels(gc);
                    });
                }
                foreach (int p in new[] { 1, 105, 125, 205, 225, 999 })
                    Shot($"h{p:000}_harita", gc => { Progress(gc, p); gc.ShowLevels(); for (float y = 0f; y <= 1f; y += 0.1f) gc.Menu.ScrollMapForTest(y); });
                Shot("k_koleksiyon", gc => { Progress(gc, 12); Own(gc); gc.ShowCollection(); for (float y = 1f; y >= 0f; y -= 0.1f) gc.Menu.ScrollCollectionForTest(y); });
                Shot("v_vitrin", gc =>
                {
                    Progress(gc, 12);
                    foreach (var s in gc.Stones.Stones) { gc.ShowStone(s.StoneId, true); gc.Showcase.Advance(600f); }
                });
            }
            finally { Out = "../v13/"; GameController.ToursOverride = null; GameController.ClockOverride = null; GameController.CountryOverride = null; }
            var list = new System.Collections.Generic.List<string>(KatKat.View.ResourceUsage.Paths);
            list.Sort(StringComparer.Ordinal);
            File.WriteAllLines("Logs/resource_usage_ui3.txt", list);
            Debug.Log("[KatKat] ui3 kullanım taraması: " + list.Count + " yol → Logs/resource_usage_ui3.txt");
        }
        private static string[] _only;

        /// <summary>Yakalama dili (2026-11-09 dil denetimi): KATKAT_CAPTURE_LANG (ör. "ar"); yoksa tr. Dilli çıktı docs/ui3_uygulama/diller/(dil)/.</summary>
        private static string CapLang => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KATKAT_CAPTURE_LANG")) ? "tr" : Environment.GetEnvironmentVariable("KATKAT_CAPTURE_LANG");

        private static void Extras()
        {
            var keepLang = Loc.Code;
            Loc.Set(CapLang, false);
            GameController.ToursOverride = false;
            GameController.ClockOverride = new FixedDayClock { DayId = "2026-11-08" };
            GameController.CountryOverride = "TR";
            try
            {
                Shot("21_bolum_baslangic_karti", gc => { Progress(gc, 12); gc.ShowLevels(); CallMenu(gc, "ShowStartCard", 12); });
                Shot("22_dil_secimi", gc => { Progress(gc, 12); Call(gc, "ShowSettings"); gc.Menu.OpenLanguagePickerForCapture(); });
                Shot("23_kural_karti_tur_tanitimi", gc => { Progress(gc, 26); gc.StartLevel(26); });
                Shot("24_kural_karti_patron", gc => { Progress(gc, 29); gc.StartLevel(29); });
                Shot("25_kayip_karti_ipucu", gc => { gc.StartLevel(11); gc.ShowLostForCapture(GameStatus.LostOutOfMoves); gc.Hud.HidePanel(); gc.ShowLostForCapture(GameStatus.LostOutOfMoves); Panels(gc); });
                // 2026-11-09: kayıpta reklamla +1 hamle ve taş yerleştirme (dört taş tahtanın üstünde; ikincisi seçili).
                Shot("45_tas_yerlestirme", gc => { gc.StartLevel(11); gc.ShowLostForCapture(GameStatus.LostOutOfMoves); Invoke(gc, "GrantExtraMoves", 1); Invoke(gc, "OnPlaceStoneChosen", 1); });
                Shot("26_ipucu_gosterimi", gc => { gc.StartLevel(11); Call(gc, "ShowMainHint"); });
                Shot("27_kristal_kaybi_karti", gc => { Progress(gc, 67); gc.StartLevel(67); gc.Hud.HideRuleCard(false); gc.ShowLostForCapture(GameStatus.LostProtected); Panels(gc); });
                Shot("28_gunluk_kural_karti", gc => { Progress(gc, 12); Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); });
                Shot("29_gunluk_kazanma", gc => { Progress(gc, 12); Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); gc.Hud.HideRuleCard(false); PlayToWin(gc); Panels(gc); });
                Shot("30_gunluk_kayip", gc => { Progress(gc, 12); Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); gc.Hud.HideRuleCard(false); gc.ShowLostForCapture(GameStatus.LostOutOfMoves); Panels(gc); });
                Shot("31_gunluk_bulmaca_tamamlandi", gc => { Progress(gc, 12); Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); gc.Hud.HideRuleCard(false); PlayToWin(gc); gc.Hud.HidePanel(); gc.ShowDaily(); });
                Shot("32_deneme_kayip_karti", gc => { gc.StartTrial(5); gc.Hud.HideRuleCard(false); gc.ShowLostForCapture(GameStatus.LostOutOfMoves); Panels(gc); });
                Shot("33_boya_paleti", gc => { gc.StartLevel(37); gc.Hud.HideRuleCard(false); gc.Hud.ShowPalette(LayerColor.Coral, new[] { LayerColor.Blue, LayerColor.Yellow, LayerColor.Purple }); });
                Shot("34_katman_gorunumu", gc => { gc.StartLevel(37); gc.Hud.HideRuleCard(false); Peek(gc); });
                GameController.ToursOverride = true; GameController.TourIgnoreSaved = true;
                Shot("35_ogretici_tur", gc => gc.StartLevel(0));
                // 2026-11-08: özel taş görünürlüğü, reklamlı ipucu, açılış.
                Shot("36_ozel_tas_secili", gc =>
                {
                    gc.StartLevel(218);
                    var st = gc.Session.State;
                    st.Tray.Clear(); st.Tray.Add(SpecialKind.RocketRow); st.Tray.Add(SpecialKind.RocketColumn); st.Tray.Add(SpecialKind.Rainbow);
                    gc.Hud.SetTrayVisible(true); gc.Hud.SetTray(st.Tray, 2);
                });
                Shot("37_reklamli_ipucu", gc => { gc.StartLevel(37); gc.Hud.HideRuleCard(false); Call(gc, "GrantHint"); });
                Shot("39_roket_bolunme", gc => FireSpecial(gc, SpecialKind.RocketRow, 420f, 3, 7));
                Shot("40_rulo_yildirim", gc => FireSpecial(gc, SpecialKind.Rainbow, 260f));
                if (_only == null || Array.IndexOf(_only, "38") >= 0) CaptureV7.SplashTo(System.IO.Path.Combine(PeelCapture.OutDir, Out + "38_acilis.png"));
            }
            finally
            {
                GameController.TourIgnoreSaved = false;
                GameController.ToursOverride = null;
                GameController.CountryOverride = null;
                GameController.ClockOverride = null;
                Loc.Set(keepLang, false);
            }
        }

        private static void CallMenu(GameController gc, string method, int arg) =>
            typeof(MenuView).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(int) }, null)?.Invoke(gc.Menu, new object[] { arg });

        internal static void PlayToWin(GameController gc)
        {
            gc.Board.Director.TimeScale = 50f;
            foreach (var step in gc.CurrentLevel.Solution) { gc.PlayStep(step); Drain(gc); }
            if (gc.IsCelebrating) Call(gc, "SkipCelebration");
            Drain(gc);
        }

        /// <summary>En çok katmanlı yığına uzun basma görünümü.</summary>
        private static void Peek(GameController gc)
        {
            var b = gc.Session.State.Board;
            int bx = 0, by = 0, best = -1;
            for (int x = 0; x < b.Width; x++)
                for (int y = 0; y < b.Height; y++)
                    if (b[x, y].Layers != null && b[x, y].Layers.Count > best) { best = b[x, y].Layers.Count; bx = x; by = y; }
            gc.PeekForTest(bx, by, true);
        }
        private static readonly (string tag, int w, int h, Rect? safe) S = ("1080x1920", 1080, 1920, null);

        public static void Run()
        {
            Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, Out));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set(CapLang, false);
            GameController.ToursOverride = false;
            GameController.ClockOverride = new FixedDayClock { DayId = "2026-11-08" };
            GameController.CountryOverride = "TR";
            try
            {
                Shot("01_harita_ana_sayfa", gc => { Progress(gc, 12); gc.ShowLevels(); });
                Shot("02_gunluk_bulmaca", gc => { Progress(gc, 12); gc.ShowDaily(); });
                Shot("03_koleksiyon", gc => { Progress(gc, 12); Own(gc); gc.ShowCollection(); });
                Shot("04_tas_vitrini_3d", gc => { Progress(gc, 12); Own(gc); gc.SaveService.Data.meta.stones.Add("stone_ocean"); gc.ShowCollection(); gc.ShowStone("stone_ocean", true); gc.Showcase.Advance(1500f); });
                Shot("05_ayarlar", gc => { Progress(gc, 12); Call(gc, "ShowSettings"); });
                // 2026-11-09: AB görünümü (UMP "Gizlilik seçenekleri" satırı zorunlu bölge).
                Shot("41_ayarlar_gizlilik", gc => { Progress(gc, 12); KatKat.Game.Ads.PrivacyOptions.ForceVisible = true; try { Call(gc, "ShowSettings"); } finally { KatKat.Game.Ads.PrivacyOptions.ForceVisible = false; } });
                // 2026-11-09 kalp sistemi: haritada eksik kalp + geri sayım, kalp kartı, kalp bitti kartı.
                Shot("42_harita_kalp_sayac", gc => { Progress(gc, 30); HeartsAt(gc, 2, 7 * 60); gc.ShowLevels(); });
                Shot("43_kalp_karti", gc => { Progress(gc, 30); HeartsAt(gc, 2, 7 * 60); gc.ShowLevels(); Invoke(gc, "ShowHeartsCard", (Action)null); });
                Shot("44_kalp_bitti", gc => { Progress(gc, 30); HeartsAt(gc, 5, 12 * 60); gc.StartLevel(25); });
                Shot("06_zpe_istatistik", gc => { Progress(gc, 12); gc.ShowLevels(); gc.ShowZpeStats(); });
                GameController.DevLeaderboardSample = true;
                Shot("07_siralama", gc => { Progress(gc, 25); gc.ShowRanking(); });
                Shot("08_siralama_bilgi", gc => { Progress(gc, 25); gc.ShowRanking(); gc.Menu.ShowRankInfo(); });
                GameController.DevLeaderboardSample = false;
                Shot("09_deneme_listesi", gc => { Progress(gc, 12); gc.ShowLevels(); gc.ShowTrials(); });
                Shot("10_deneme_kural_karti", gc => gc.StartTrial(9));
                Shot("11_deneme_tahtasi", gc => { gc.StartTrial(5); gc.Hud.HideRuleCard(false); });
                Shot("12_oyun_ekrani", gc => gc.StartLevel(37));
                Shot("13_oyun_ozel_taslar", gc =>
                {
                    gc.StartLevel(218);
                    var st = gc.Session.State;
                    st.Tray.Clear(); st.Tray.Add(SpecialKind.RocketRow); st.Tray.Add(SpecialKind.RocketColumn); st.Tray.Add(SpecialKind.Rainbow);
                    gc.Hud.SetTrayVisible(true); gc.Hud.SetTray(st.Tray, -1);
                });
                Shot("14_duraklat", gc => { gc.StartLevel(37); Call(gc, "OpenPause"); Panels(gc); });
                Shot("15_kayip_karti", gc => { LoseLevel4(gc); Panels(gc); });
                Shot("16_zafer_karti", gc => { WinLevel(gc, 1); Panels(gc); });
                Shot("17_gunluk_oyun_ekrani", gc => { Progress(gc, 12); Own(gc); gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); });
                GameController.AdScreenOverride = true;
                Shot("18_test_reklam_araniyor", gc => { Progress(gc, 12); Own(gc); gc.ShowDaily(); TestAdScreen.FillSeconds = 99f; gc.PressDailyEnterForTest(); gc.AdScreen.Advance(1600f); });
                Shot("19_test_reklam_odullu", gc => { Progress(gc, 12); Own(gc); gc.ShowDaily(); TestAdScreen.FillSeconds = 0.5f; gc.PressDailyEnterForTest(); gc.AdScreen.Advance(600f); gc.AdScreen.Advance(5200f); });
                Shot("20_test_reklam_gecis", gc => { gc.StartLevel(10); gc.AdScreen.ShowInterstitial(AdPlacements.LevelComplete, _ => { }); gc.AdScreen.Advance(3200f); });
            }
            finally
            {
                TestAdScreen.FillSeconds = 1.2f;
                GameController.AdScreenOverride = null;
                GameController.DevLeaderboardSample = false;
                GameController.CountryOverride = null;
                GameController.ToursOverride = null;
                GameController.ClockOverride = null;
                Loc.Set(keepLang, false);
            }
        }

        /// <summary>Bölüm 218'de tepsiye <paramref name="kind"/> koyup ortadaki seçilebilir taşta ateşler; efekti <paramref name="ms"/> ilerletir.</summary>
        internal static void FireSpecial(GameController gc, SpecialKind kind, float ms, int fx = -1, int fy = -1)
        {
            gc.StartLevel(218);
            gc.Hud.HideRuleCard(false);
            var st = gc.Session.State;
            st.Tray.Clear(); st.Tray.Add(kind);
            gc.Hud.SetTrayVisible(true); gc.Hud.SetTray(st.Tray, -1);
            var b = st.Board;
            int bx = -1, by = -1; float best = float.MaxValue;
            for (int x = 0; x < b.Width; x++)
                for (int y = 0; y < b.Height; y++)
                {
                    if (!b[x, y].IsSelectable) continue;
                    float d = (x - b.Width * 0.5f) * (x - b.Width * 0.5f) + (y - b.Height * 0.5f) * (y - b.Height * 0.5f);
                    if (d < best) { best = d; bx = x; by = y; }
                }
            if (fx >= 0) { bx = fx; by = fy; }
            gc.PlayStep(new KatKat.Levels.StepData { Type = "use", Row = 0, X = bx, Y = by });
            for (float t = 0f; t < ms; t += 20f) gc.Board.Advance(20f); // etkiler adım adım ilerlesin
        }

        private static void Invoke(GameController gc, string method, params object[] args)
        {
            foreach (var m in typeof(GameController).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                if (m.Name == method && m.GetParameters().Length == args.Length) { m.Invoke(gc, args); return; }
            throw new MissingMethodException(method);
        }

        /// <summary>Kalp durumu: <paramref name="missing"/> eksik, sonraki kalbe <paramref name="left"/> sn (saat sabit).</summary>
        private static void HeartsAt(GameController gc, int missing, int left)
        {
            const long now = 1_800_000_000;
            GameController.HeartClock = () => now;
            gc.SaveService.Data.meta.heartsMissing = missing;
            gc.SaveService.Data.meta.heartsStamp = now - (Hearts.RegenSeconds - left);
        }

        internal static void Call(GameController gc, string method) =>
            typeof(GameController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null)?.Invoke(gc, null);

        /// <summary>Sonuç kartlarının giriş animasyonunu sona ilerletir (editörde Update çalışmaz).</summary>
        internal static void Panels(GameController gc)
        {
            foreach (var p in gc.Hud.GetComponentsInChildren<ResultPanel>(true)) p.Advance(5000f);
        }

        internal static void Progress(GameController gc, int n)
        {
            var d = gc.SaveService.Data;
            d.completed.Clear(); d.starIds.Clear(); d.starCounts.Clear(); d.highestUnlocked = 0;
            for (int i = 0; i < n; i++)
            {
                gc.SaveService.MarkCompleted(gc.Catalog.IdAt(i), i, gc.Catalog.Count);
                gc.SaveService.RecordStars(gc.Catalog.IdAt(i), 2 + i % 2);
                RankStars.Record(d.meta, gc.Catalog.IdAt(i), 2 + i % 2);
            }
            d.meta.zpeHistory.Clear(); d.meta.zpeSum = 0; d.meta.zpeGames = 0;
            for (int i = 0; i < n; i++)
                Zpe.Add(d.meta, new ZpeRecord { levelId = gc.Catalog.IdAt(i), success = true, firstAttempt = i % 3 != 0, movesUsed = 5 + i % 4, moveLimit = 10, seconds = 30 });
        }

        internal static void Own(GameController gc) =>
            CollectionRules.GrantUnlocked(gc.SaveService.Data.meta, gc.Stones, gc.SaveService.Data.IsCompleted);

        internal static void Drain(GameController gc)
        {
            for (int k = 0; k < 600 && (gc.CurrentPhase == GameController.Phase.Animating || gc.IsCelebrating); k++)
            { gc.Board.Advance(16f); gc.Board.Director.Advance(16f); }
        }

        private static void LoseLevel4(GameController gc)
        {
            gc.StartLevel(3);
            gc.Board.Director.TimeScale = 50f;
            gc.OnCellPressed(0, 0);
            Drain(gc);
            var next = KatKat.Levels.LevelSolver.Moves(gc.Session.State);
            if (next.Count > 0) gc.OnCellPressed(next[0].X, next[0].Y);
            Drain(gc);
        }

        internal static void WinLevel(GameController gc, int index)
        {
            gc.StartLevel(index);
            gc.Board.Director.TimeScale = 50f;
            foreach (var step in gc.CurrentLevel.Solution) { gc.PlayStep(step); Drain(gc); }
            if (gc.IsCelebrating) Call(gc, "SkipCelebration");
            Drain(gc);
        }

        private static void Shot(string name, Action<GameController> act)
        {
            if (_only != null && Array.IndexOf(_only, name.Substring(0, 2)) < 0) return;
            GameCapture.ShotPublic(Out + name + ".png", S.w, S.h, S.safe, gc =>
            {
                if (gc.AdScreen != null) gc.AdScreen.Canvas.scaleFactor = S.w / HudView.RefWidth;
                // Kalp durumu yakalamalar arasında taşınmasın (dolu kalp, gerçek saat).
                GameController.HeartClock = null;
                gc.SaveService.Data.meta.heartsMissing = 0;
                gc.SaveService.Data.meta.heartsStamp = 0;
                act(gc);
            });
        }
    }
}
