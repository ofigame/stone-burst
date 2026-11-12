using System;
using System.Collections.Generic;
using System.IO;
using KatKat.Core;
using KatKat.Game;
using KatKat.Meta;
using KatKat.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.EditorTools
{
    /// <summary>
    /// App Store ekran görüntüleri (kullanıcı, 2026-11-09: "mağaza için ekran görüntüleri", "iPad de olsun").
    /// 1) Ham çekim: iPhone 6,3" 1206×2622 ve iPad 13" 2064×2752, gerçek oyun ekranı, cihaz güvenli alanıyla
    ///    → docs/magaza/ham/(cihaz)/(dil)/NN_ad.png.
    /// 2) Mağaza görseli: degrade zemin, üstte o dilde başlık (oyunun fontu, Arapça şekillendirme), altta yuvarlak
    ///    köşeli ekran → docs/magaza/(cihaz)/(dil)/NN_ad.png (aynı boyut, RGB).
    /// Ortam: KATKAT_STORE_DEVICES ("iphone,ipad"), KATKAT_STORE_LANGS ("en,tr,de,es,ar,ja"), KATKAT_STORE_ONLY ("1,3").
    /// </summary>
    public static class CaptureStore
    {
        private struct Device
        {
            public string Name; public int W, H; public Rect Safe; public bool Pad;
        }

        // App Store Connect (2026) zorunlu iPhone alanı "Dynamic Island, orta ekran" 6,1"/6,3": 1206×2622.
        // Güvenli alan (normalize, alttan): iPhone 6,3" üstte 62 pt ada, altta 34 pt ev çubuğu (×3);
        // iPad 13" üstte 24 pt durum çubuğu, altta 20 pt ev çubuğu (×2).
        private static readonly Device IPhone = new Device { Name = "iphone", W = 1206, H = 2622, Safe = Safe(2622, 186, 102) };
        private static readonly Device IPad = new Device { Name = "ipad", W = 2064, H = 2752, Safe = Safe(2752, 48, 40), Pad = true };
        // Google Play telefon (2026-11-10): en/boy en çok 2:1 olmalı (iPhone 1206×2622 kabul edilmez); 1080×1920, üstte
        // 24 dp durum çubuğu (×3), altta hareket çubuğu 16 dp (×3).
        private static readonly Device Android = new Device { Name = "android", W = 1080, H = 1920, Safe = Safe(1920, 72, 48) };

        private static Rect Safe(int h, int top, int bottom) => new Rect(0f, bottom / (float)h, 1f, (h - top - bottom) / (float)h);

        private static string[] Env(string key, string def) =>
            (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)) ? def : Environment.GetEnvironmentVariable(key)).Split(',');

        /// <summary>Başlıklar: 6 ekran × mağaza dilleri. Ana dili o dil olan biri kontrol etmedi.</summary>
        private static readonly Dictionary<string, string[]> Captions = new Dictionary<string, string[]>
        {
            ["tr"] = new[] { "Katmanları soy, renkleri ortaya çıkar!", "Altın Rulo bir rengi tamamen temizler!", "Keşfedilecek 1.000 bölüm", "Zincirleme patlamaları planla, 3 yıldız kazan!", "Her gün yeni bir bulmaca", "Nadir taşları topla" },
            ["en"] = new[] { "Peel the layers, reveal the colors!", "The Golden Roll clears a whole color!", "1,000 levels to explore", "Plan chain reactions, earn 3 stars!", "A new puzzle every day", "Collect rare stones" },
            ["de"] = new[] { "Schichten abtragen, Farben entdecken!", "Die Goldrolle räumt eine ganze Farbe ab!", "1.000 Level zum Entdecken", "Plane Kettenreaktionen, hol dir 3 Sterne!", "Jeden Tag ein neues Rätsel", "Sammle seltene Steine" },
            ["es"] = new[] { "¡Pela las capas y descubre los colores!", "¡El Rollo Dorado elimina todo un color!", "1.000 niveles por descubrir", "¡Planea reacciones en cadena y gana 3 estrellas!", "Un nuevo puzle cada día", "Colecciona piedras raras" },
            ["ar"] = new[] { "قشّر الطبقات واكشف الألوان!", "اللفافة الذهبية تزيل لونا كاملا!", "1000 مرحلة لتستكشفها", "خطّط لسلسلة الانفجارات واربح 3 نجوم!", "لغز جديد كل يوم", "اجمع الأحجار النادرة" },
            ["ja"] = new[] { "レイヤーをはがして色を見つけよう！", "ゴールドロールで1色まるごと消せる！", "1,000ステージであそぼう！", "連鎖を計画して星3つをゲット！", "まいにち新しいパズル", "レアな石をコレクション" },
        };

        public static void Run()
        {
            var devices = Env("KATKAT_STORE_DEVICES", "iphone,ipad");
            var langs = Env("KATKAT_STORE_LANGS", "en,tr,de,es,ar,ja");
            var onlyRaw = Environment.GetEnvironmentVariable("KATKAT_STORE_ONLY");
            var only = string.IsNullOrEmpty(onlyRaw) ? null : onlyRaw.Split(',');
            // KATKAT_STORE_STYLE=sahne: eğik sahne + maskot + alt şerit (docs/magaza/sahne). KATKAT_STORE_RAW=1: ham çekim varsa yeniden çekme.
            bool scene = Environment.GetEnvironmentVariable("KATKAT_STORE_STYLE") == "sahne";
            bool reuseRaw = Environment.GetEnvironmentVariable("KATKAT_STORE_RAW") == "1";
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            GameController.ToursOverride = false;
            GameController.ClockOverride = new FixedDayClock { DayId = "2026-11-08" };
            GameController.CountryOverride = "TR";
            try
            {
                foreach (var dn in devices)
                {
                    var dev = dn.Trim() == "ipad" ? IPad : dn.Trim() == "android" ? Android : IPhone;
                    foreach (var l in langs)
                    {
                        string lang = l.Trim();
                        Loc.Set(lang, false);
                        string dir = "../magaza/ham/" + dev.Name + "/" + lang + "/";
                        string outDir = Path.Combine("docs", "magaza", dev.Name, lang);
                        Directory.CreateDirectory(Path.Combine(PeelCapture.OutDir, dir));
                        void Shot(int n, string name, int bg, Action<GameController> act)
                        {
                            if (only != null && Array.IndexOf(only, n.ToString()) < 0) return;
                            string file = n.ToString("00") + "_" + name + ".png";
                            string rawPath = Path.Combine(PeelCapture.OutDir, dir + file);
                            GameController.BackgroundOverride = bg;
                            try
                            {
                                if (!(reuseRaw && File.Exists(rawPath)))
                                GameCapture.ShotPublic(dir + file, dev.W, dev.H, dev.Safe, gc =>
                                {
                                    GameController.HeartClock = null;
                                    gc.SaveService.Data.meta.heartsMissing = 0;
                                    gc.SaveService.Data.meta.heartsStamp = 0;
                                    act(gc);
                                });
                            }
                            finally { GameController.BackgroundOverride = null; }
                            if (scene)
                            {
                                string sc = SceneCaptions.TryGetValue(lang, out var s2) ? s2[n - 1] : SceneCaptions["en"][n - 1];
                                ComposeScene(rawPath, Path.Combine("docs", "magaza", "sahne", dev.Name, lang, file), dev, n, sc);
                                return;
                            }
                            string caption = Captions.TryGetValue(lang, out var c) ? c[n - 1] : Captions["en"][n - 1];
                            Compose(rawPath, Path.Combine(outDir, file), dev.W, dev.H, caption, dev.Pad);
                        }
                        // 1: oyun ekranı (renkli kalabalık tahta).
                        Shot(1, "oyun", 0, gc => { CaptureV13Pages.Progress(gc, 37); gc.StartLevel(37); gc.Hud.HideRuleCard(false); });
                        // 2: Altın Rulo patlaması (özel taş etkisi).
                        Shot(2, "rulo", 1, gc => CaptureV13Pages.FireSpecial(gc, SpecialKind.Rainbow, 260f));
                        // 3: harita (bölge yolu).
                        Shot(3, "harita", 0, gc => { CaptureV13Pages.Progress(gc, 42); gc.ShowLevels(); });
                        // 4: zafer kartı.
                        Shot(4, "zafer", 2, gc => { CaptureV13Pages.WinLevel(gc, 1); CaptureV13Pages.Panels(gc); });
                        // 5: günlük bulmaca oyun ekranı (giriş kartında "deneme/prototip" yazısı olduğu için tahta).
                        Shot(5, "gunluk", 3, gc =>
                        {
                            CaptureV13Pages.Progress(gc, 42); CaptureV13Pages.Own(gc);
                            gc.ShowDaily(); gc.PressDailyEnterForTest(); gc.StartDaily(); gc.Hud.HideRuleCard(false);
                        });
                        // 6: taş vitrini (3B koleksiyon taşı).
                        Shot(6, "vitrin", 0, gc =>
                        {
                            CaptureV13Pages.Progress(gc, 42); CaptureV13Pages.Own(gc);
                            gc.SaveService.Data.meta.stones.Add("stone_ocean");
                            gc.ShowCollection(); gc.ShowStone("stone_ocean", true); gc.Showcase.Advance(1500f);
                        });
                    }
                }
            }
            finally
            {
                GameController.CountryOverride = null;
                GameController.ToursOverride = null;
                GameController.ClockOverride = null;
                GameController.HeartClock = null;
                GameController.BackgroundOverride = null;
                Loc.Set(keepLang, false);
            }
        }

        // ---- Mağaza kompozisyonu ----------------------------------------------------------------------------

        private static Sprite _round;

        /// <summary>Yuvarlak köşeli 9 dilimli beyaz kare (köşe yarıçapı 64 px).</summary>
        private static Sprite Round()
        {
            if (_round) return _round;
            const int S = 256, R = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, R, S - R), cy = Mathf.Clamp(y + 0.5f, R, S - R);
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(R - d + 0.5f)));
                }
            tex.SetPixels32(px);
            tex.Apply();
            _round = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
            _round.hideFlags = HideFlags.DontSave;
            return _round;
        }

        private static Texture2D Gradient(Color top, Color bottom)
        {
            var t = new Texture2D(1, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < 256; y++) t.SetPixel(0, y, Color.Lerp(bottom, top, y / 255f));
            t.Apply();
            return t;
        }

        /// <summary>Ham çekimi mağaza görseline çevirir: degrade zemin, üstte başlık, altta yuvarlak köşeli ekran.</summary>
        private static void Compose(string rawFile, string outFile, int w, int h, string caption, bool pad)
        {
            var raw = new Texture2D(2, 2, TextureFormat.RGB24, false) { hideFlags = HideFlags.DontSave };
            raw.LoadImage(File.ReadAllBytes(rawFile));
            var grad = Gradient(new Color32(0x2C, 0xB5, 0xA9, 0xFF), new Color32(0x0D, 0x52, 0x63, 0xFF));
            var camGo = new GameObject("ComposeCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x0D, 0x52, 0x63, 0xFF);
            cam.orthographic = true;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.aspect = (float)w / h;
            var root = new GameObject("Compose", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.scaleFactor = 1f;
            try
            {
                var bg = new GameObject("Bg", typeof(RectTransform)).AddComponent<RawImage>();
                bg.transform.SetParent(root.transform, false);
                bg.texture = grad;
                UiFactory.Stretch(bg.rectTransform, 0f, 0f, 0f, 0f);

                // Ekran: yüksekliğin %74–77'si, ortada, alttan %3 boşluk; beyaz çerçeve ve yumuşak gölge.
                float sh = h * (pad ? 0.74f : 0.77f), sw = sh * w / h;
                float bottom = h * 0.03f, radius = sw * (pad ? 0.03f : 0.05f), rim = radius * 0.18f;
                var shadow = UiFactory.Image("Shadow", root.transform, Round(), new Color(0f, 0.1f, 0.13f, 0.22f), true);
                shadow.pixelsPerUnitMultiplier = 64f / (radius * 1.3f);
                Place(shadow.rectTransform, sw + rim * 4f, sh + rim * 4f, bottom - rim * 3f);
                var frame = UiFactory.Image("Frame", root.transform, Round(), Color.white, true);
                frame.pixelsPerUnitMultiplier = 64f / (radius + rim);
                Place(frame.rectTransform, sw + rim * 2f, sh + rim * 2f, bottom - rim);
                var mask = UiFactory.Image("Mask", root.transform, Round(), Color.white, true);
                mask.pixelsPerUnitMultiplier = 64f / radius;
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                Place(mask.rectTransform, sw, sh, bottom);
                var shot = new GameObject("Shot", typeof(RectTransform)).AddComponent<RawImage>();
                shot.transform.SetParent(mask.transform, false);
                shot.texture = raw;
                UiFactory.Stretch(shot.rectTransform, 0f, 0f, 0f, 0f);

                // Başlık: ekranın üstündeki alanda, en çok iki satır, beyaz, koyu konturlu.
                float top = h * 0.03f, capH = h - (bottom + sh + rim) - top - h * 0.02f;
                int size = pad ? 150 : 128;
                var t = UiFactory.Text("Caption", root.transform, "", size, Color.white);
                var tr = t.rectTransform;
                tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
                tr.pivot = new Vector2(0.5f, 1f);
                tr.sizeDelta = new Vector2(w * (pad ? 0.8f : 0.88f), capH);
                tr.anchoredPosition = new Vector2(0f, -top);
                t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = 60;
                t.outlineWidth = 0.16f;
                t.outlineColor = new Color32(0x0A, 0x3E, 0x4A, 0xFF);
                Loc.Set(t, caption);

                Canvas.ForceUpdateCanvases();
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var outTex = new Texture2D(w, h, TextureFormat.RGB24, false);
                outTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                outTex.Apply();
                RenderTexture.active = prev;
                Directory.CreateDirectory(Path.GetDirectoryName(outFile));
                File.WriteAllBytes(outFile, outTex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(outTex);
            }
            finally
            {
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(raw);
                UnityEngine.Object.DestroyImmediate(grad);
            }
        }

        // ---- Sahne kompozisyonu (kullanıcı, 2026-11-12: "mağaza görsellerimizi bu şekilde yapalım, maskotumuzu kullan") ----
        // Tam ekran eğik oyun sahnesi, köşede onaylı maskot, altta koyu şeritte kısa büyük başlık. Ham çekimden kırpılır.

        /// <summary>Kısa, büyük harf başlıklar (şerit için). Ana dili o dil olan biri kontrol etmedi.</summary>
        private static readonly Dictionary<string, string[]> SceneCaptions = new Dictionary<string, string[]>
        {
            ["tr"] = new[] { "KATMANLARI SOY", "ALTIN RULO", "1.000 BÖLÜM", "3 YILDIZ KAZAN", "GÜNLÜK BULMACA", "TAŞLARI TOPLA" },
            ["en"] = new[] { "PEEL THE LAYERS", "GOLDEN ROLL", "1,000 LEVELS", "EARN 3 STARS", "DAILY PUZZLE", "COLLECT STONES" },
            ["de"] = new[] { "SCHICHTEN ABTRAGEN", "GOLDROLLE", "1.000 LEVEL", "HOL DIR 3 STERNE", "TÄGLICHES RÄTSEL", "STEINE SAMMELN" },
            ["es"] = new[] { "¡PELA LAS CAPAS!", "ROLLO DORADO", "1.000 NIVELES", "GANA 3 ESTRELLAS", "PUZLE DIARIO", "COLECCIONA PIEDRAS" },
            ["ar"] = new[] { "قشّر الطبقات", "اللفافة الذهبية", "1000 مرحلة", "اربح 3 نجوم", "لغز يومي", "اجمع الأحجار" },
            ["ja"] = new[] { "レイヤーをはがそう", "ゴールドロール", "1,000ステージ", "星3つをゲット", "デイリーパズル", "石をコレクション" },
        };

        /// <summary>Ham çekimde (telefon yerleşimi) kahraman bölge, normalize, üstten: x0, y0, x1, y1.</summary>
        private static readonly Vector4[] HeroCrop =
        {
            new Vector4(0.17f, 0.34f, 0.83f, 0.85f), // 1 tahta
            new Vector4(0.19f, 0.35f, 0.81f, 0.84f), // 2 rulo
            new Vector4(0.08f, 0.27f, 1.00f, 0.86f), // 3 harita
            new Vector4(0.09f, 0.15f, 0.91f, 0.83f), // 4 zafer kartı
            new Vector4(0.20f, 0.38f, 0.80f, 0.84f), // 5 günlük tahta
            new Vector4(0.10f, 0.27f, 0.90f, 0.66f), // 6 taş
        };

        private static Texture2D LoadPng(string path)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            t.LoadImage(File.ReadAllBytes(path));
            return t;
        }

        private static void ComposeScene(string rawFile, string outFile, Device dev, int n, string caption)
        {
            int w = dev.W, h = dev.H;
            var raw = LoadPng(rawFile);
            var blur = new RenderTexture(24, 42, 0) { filterMode = FilterMode.Bilinear };
            var mid = RenderTexture.GetTemporary(96, 170, 0);
            Graphics.Blit(raw, mid);
            Graphics.Blit(mid, blur);
            RenderTexture.ReleaseTemporary(mid);
            bool left = n % 2 == 1;
            var mascot = LoadPng(left ? "docs/gorseller/maskot/01-onayli-maskot.png" : "docs/gorseller/maskot/03-karsiya-bakan.png");

            var camGo = new GameObject("ComposeCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x1B, 0x18, 0x45, 0xFF);
            cam.orthographic = false;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = rt;
            cam.aspect = (float)w / h;
            var root = new GameObject("Compose", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 6f;
            try
            {
                // Zemin: aynı çekimin bulanık, koyulaştırılmış hâli.
                var bg = new GameObject("Bg", typeof(RectTransform)).AddComponent<RawImage>();
                bg.transform.SetParent(root.transform, false);
                bg.texture = blur;
                bg.color = new Color(0.62f, 0.62f, 0.7f, 1f);
                UiFactory.Stretch(bg.rectTransform, 0f, 0f, 0f, 0f);

                float band = h * (dev.Pad ? 0.13f : 0.15f);
                var c = HeroCrop[n - 1];
                if (dev.Pad) { float col = h * 9f / 16f / w; c.x = 0.5f + (c.x - 0.5f) * col; c.z = 0.5f + (c.z - 0.5f) * col; }
                float cw = (c.z - c.x) * raw.width, ch = (c.w - c.y) * raw.height;
                float scale = (dev.Pad ? 0.95f : 1.16f) * w / cw;
                float maxH = (h - band) * 1.02f;
                if (ch * scale > maxH) scale = maxH / ch;
                float hw = cw * scale, hh = ch * scale;

                // Eğik kahraman sahne: üst kenar geriye yatar, hafif dönük; gölge ve yuvarlak köşe.
                var hero = new GameObject("Hero", typeof(RectTransform)).GetComponent<RectTransform>();
                hero.SetParent(root.transform, false);
                hero.anchorMin = hero.anchorMax = new Vector2(0.5f, 0f);
                hero.pivot = new Vector2(0.5f, 0.5f);
                hero.sizeDelta = new Vector2(hw, hh);
                hero.anchoredPosition = new Vector2(0f, band + (h - band) * 0.5f + h * 0.01f);
                hero.localRotation = Quaternion.Euler(22f, left ? -8f : 8f, left ? 3f : -3f);
                float radius = hw * 0.045f;
                var shadow = UiFactory.Image("Shadow", hero, Round(), new Color(0.03f, 0.02f, 0.12f, 0.45f), true);
                shadow.pixelsPerUnitMultiplier = 64f / (radius * 1.6f);
                UiFactory.Stretch(shadow.rectTransform, -radius * 0.5f, -radius * 1.4f, -radius * 0.5f, radius * 0.2f);
                var rim = UiFactory.Image("Rim", hero, Round(), new Color(1f, 1f, 1f, 0.9f), true);
                rim.pixelsPerUnitMultiplier = 64f / (radius * 1.12f);
                UiFactory.Stretch(rim.rectTransform, -radius * 0.18f, -radius * 0.18f, -radius * 0.18f, -radius * 0.18f);
                var mask = UiFactory.Image("Mask", hero, Round(), Color.white, true);
                mask.pixelsPerUnitMultiplier = 64f / radius;
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                UiFactory.Stretch(mask.rectTransform, 0f, 0f, 0f, 0f);
                var shot = new GameObject("Shot", typeof(RectTransform)).AddComponent<RawImage>();
                shot.transform.SetParent(mask.transform, false);
                shot.texture = raw;
                shot.uvRect = new Rect(c.x, 1f - c.w, c.z - c.x, c.w - c.y);
                UiFactory.Stretch(shot.rectTransform, 0f, 0f, 0f, 0f);

                // Alt şerit: koyu mor degrade, üstte turkuaz ışık çizgisi.
                var bandImg = new GameObject("Band", typeof(RectTransform)).AddComponent<RawImage>();
                bandImg.transform.SetParent(root.transform, false);
                var grad = Gradient(new Color32(0x3A, 0x2C, 0x7A, 0xFF), new Color32(0x16, 0x12, 0x3A, 0xFF));
                bandImg.texture = grad;
                Place(bandImg.rectTransform, w * 1.02f, band, 0f);
                var line = UiFactory.Image("Line", root.transform, null, new Color32(0x3F, 0xE6, 0xD2, 0xFF));
                Place(line.rectTransform, w * 1.02f, h * 0.0045f, band - h * 0.002f);
                var glow = UiFactory.Image("Glow", root.transform, null, new Color32(0x3F, 0xE6, 0xD2, 0x40));
                Place(glow.rectTransform, w * 1.02f, h * 0.014f, band - h * 0.007f);

                // Maskot: şeridin üstünde, sahneye taşar; gölgeli.
                float mw = Mathf.Min(h * (dev.Pad ? 0.22f : 0.23f) * mascot.width / mascot.height, w * (dev.Pad ? 0.24f : 0.34f)), mh = mw * mascot.height / mascot.width;
                float mx = left ? w * 0.04f + mw * 0.5f : w * 0.96f - mw * 0.5f;
                for (int pass = 0; pass < 2; pass++)
                {
                    var m = new GameObject(pass == 0 ? "MascotShadow" : "Mascot", typeof(RectTransform)).AddComponent<RawImage>();
                    m.transform.SetParent(root.transform, false);
                    m.texture = mascot;
                    m.color = pass == 0 ? new Color(0.05f, 0.02f, 0.15f, 0.45f) : Color.white;
                    var r = m.rectTransform;
                    r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
                    r.pivot = new Vector2(0.5f, 0f);
                    r.sizeDelta = new Vector2(mw, mh);
                    r.anchoredPosition = new Vector2(mx + (pass == 0 ? w * 0.012f : 0f), h * 0.012f - (pass == 0 ? h * 0.008f : 0f));
                    r.localRotation = Quaternion.Euler(0f, 0f, left ? 6f : -6f);
                }

                // Başlık: şeritte, maskotun karşı tarafında, beyaz, koyu kontur.
                float tx0 = left ? w * 0.04f + mw * 1.0f : w * 0.04f, tx1 = left ? w * 0.96f : w * 0.95f - mw * 1.0f;
                int size = dev.Pad ? 170 : 136;
                var t = UiFactory.Text("Caption", root.transform, "", size, Color.white);
                var tr = t.rectTransform;
                tr.anchorMin = tr.anchorMax = new Vector2(0f, 0f);
                tr.pivot = new Vector2(0.5f, 0.5f);
                tr.sizeDelta = new Vector2(tx1 - tx0, band * 0.88f);
                tr.anchoredPosition = new Vector2((tx0 + tx1) * 0.5f, band * 0.5f);
                t.textWrappingMode = TMPro.TextWrappingModes.Normal;
                t.lineSpacing = -18f;
                t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = 40;
                t.characterSpacing = 2f;
                t.enableVertexGradient = true;
                t.colorGradient = new TMPro.VertexGradient(Color.white, Color.white, new Color(1f, 0.93f, 0.62f), new Color(1f, 0.93f, 0.62f));
                t.outlineWidth = 0.14f;
                t.outlineColor = new Color32(0x10, 0x0C, 0x2E, 0xFF);
                Loc.Set(t, caption);

                Canvas.ForceUpdateCanvases();
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var outTex = new Texture2D(w, h, TextureFormat.RGB24, false);
                outTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                outTex.Apply();
                RenderTexture.active = prev;
                Directory.CreateDirectory(Path.GetDirectoryName(outFile));
                File.WriteAllBytes(outFile, outTex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(outTex);
                UnityEngine.Object.DestroyImmediate(grad);
            }
            finally
            {
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(camGo);
                UnityEngine.Object.DestroyImmediate(raw);
                UnityEngine.Object.DestroyImmediate(mascot);
                blur.Release();
                UnityEngine.Object.DestroyImmediate(blur);
            }
        }

        /// <summary>Alt kenara göre ortalı yerleşim (tuval pikselinde).</summary>
        private static void Place(RectTransform r, float w, float h, float bottom)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(w, h);
            r.anchoredPosition = new Vector2(0f, bottom);
        }
    }
}
