using System;
using System.Collections.Generic;
using System.IO;
using KatKat.Game;
using KatKat.UI;
using KatKat.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    // ---- Meta reklamı (2026-11-19; kullanıcı: "senden çok güzel reklam için edit istiyorum") -----------------------------
    // Mısır'daki Huawei kullanıcılarına Facebook/Instagram (Reels, Stories) reklamı, Arapça, 9:16. Vurucu kurgunun
    // (RunPunch, 2026-11-12) çekim altyapısı aynen kullanılır; sahneler güncel oyundan (zincir yok, her patlama oyuncunun
    // hamlesi; Altın Rulo, Roket, buz, sarmaşık). Anlar tools/LevelLab "moments" ile seçildi (Logs/ad_moments.txt).
    // Çıktı Logs/ad_<dil>/<sahne>/ (f_*.png, sfx.wav, meta.txt); eski Logs/punch_<dil>/ klasörlerine dokunulmaz.
    // Kurgu: tools/trailer-ad.ps1. Batch: tools/unity.ps1 trailer-ad (KATKAT_TRAILER_LANGS, KATKAT_TRAILER_ONLY geçerli).
    public static partial class TrailerCapture
    {
        [MenuItem("Kat Kat/Yakalama/Meta reklamı kareleri")]
        public static void RunAd()
        {
            var langs = (Environment.GetEnvironmentVariable("KATKAT_TRAILER_LANGS") ?? "ar").Split(',');
            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            // Müzik ve efektler vurucu kurguyla aynı üreteçten; ayrı adla (eski kurgunun dosyaları değişmesin).
            WriteWav(Path.Combine(logs, "ad_music.wav"), PunchMusic(20f, 8, 7), 2); // 14–16 sn gerilim (harita), 16 sn kapanış akoru (logo)
            WriteWav(Path.Combine(logs, "ad_whoosh.wav"), Whoosh(), 2);
            WriteWav(Path.Combine(logs, "ad_impact.wav"), Impact(), 2);
            Fps = 90;
            try { foreach (var l in langs) RunAdLang(l.Trim(), Path.Combine(logs, "ad_" + l.Trim())); }
            finally { Fps = 30; _dense = true; BgMotion.ClockOverride = null; }
        }

        private static void RunAdLang(string lang, string root)
        {
            _root = root;
            var only = Environment.GetEnvironmentVariable("KATKAT_TRAILER_ONLY");
            var pick = string.IsNullOrEmpty(only) ? null : new HashSet<string>(only.Split(','));
            if (pick == null && Directory.Exists(_root)) Directory.Delete(_root, true);
            Directory.CreateDirectory(_root);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set(lang, false);
            GameController.ToursOverride = false;
            try
            {
                // Arka plan ışıltıları (BgMotion) toplu modda gerçek saatle ilerlemez: kare saatine bağlanır.
                void Scene(string n, Action<GameController, Action> b)
                {
                    if (pick != null && !pick.Contains(n)) return;
                    TrailerCapture.Scene(n, (gc, rec) =>
                    {
                        float clock = 0f;
                        var bd = gc.transform.Find("Backdrop");
                        Action rec2 = () =>
                        {
                            clock += Dt / 1000f;
                            BgMotion.ClockOverride = clock;
                            var bm = bd != null ? bd.GetComponent<BgMotion>() : null;
                            if (bm == null) { bd = gc.transform.Find("Backdrop"); bm = bd != null ? bd.GetComponent<BgMotion>() : null; }
                            if (bm != null) bm.Tick(clock);
                            rec();
                        };
                        b(gc, rec2);
                    });
                }
                // PlayRange(bölüm sırası 0 tabanlı, ilk adım, son adım (hariç), hız, bekleme sn, kahraman adım (ilk adıma göre)).
                Scene("a1_rulo_l174", (gc, rec) => PlayRange(gc, rec, 173, 1, 2, 1.6f, 1.4f, 0));   // Altın Rulo, 35 taş
                Scene("a2_temel_l017", (gc, rec) => PlayRange(gc, rec, 16, 0, 3, 1.7f, 0.4f, -1));  // temel kural, 7x7
                Scene("a3_roket_l095", (gc, rec) => PlayRange(gc, rec, 94, 3, 4, 1.6f, 1.2f, 0));   // Roket (sütun), buzlu bölüm
                Scene("a4_buz_l087", (gc, rec) => PlayRange(gc, rec, 86, 0, 4, 1.7f, 0.4f, -1));    // buz
                Scene("a5_sarmasik_l059", (gc, rec) => PlayRange(gc, rec, 58, 0, 3, 1.7f, 0.6f, -1)); // sarmaşık (+ adım 2 Altın Rulo)
                Scene("a4b_engel_l860", (gc, rec) => PlayRange(gc, rec, 859, 0, 5, 1.7f, 0.4f, -1)); // 7 buz + 7 sarmaşık (en çok)
                Scene("a6_zafer_l042", (gc, rec) => PlayRange(gc, rec, 41, -2, 99, 1.6f, 0f, 99));  // son hamleler ve kutlama
                Scene("a7_harita", (gc, rec) =>
                {
                    _dense = false; // 90 fps yakalamada her 3. kare (normal hız)
                    // AppGallery derlemesinde olmayan girişler (canlı maç, pilot/konsept denemeleri) haritada görünmesin.
                    gc.Menu.LivePressed = null; gc.Menu.PilotPressed = null; gc.Menu.ConceptPressed = null;
                    int cur = 236;
                    gc.Menu.ShowLevels(gc.Catalog.Count, cur, i => i < cur, () => { });
                    float yCur = gc.Menu.ScrollYForTest, yLow = Mathf.Max(0f, yCur - 16000f);
                    int down = (int)(Fps * 3.2f);
                    for (int f = 0; f <= down; f++)
                    {
                        gc.Menu.ScrollMapForTest(Mathf.Lerp(yCur, yLow, Ease(f / (float)down)));
                        gc.Menu.Tick(Dt / 1000f); rec();
                    }
                    for (int f = 0; f < Fps; f++) { gc.Menu.Tick(Dt / 1000f); rec(); }
                });
            }
            finally
            {
                GameController.ToursOverride = null;
                Loc.Set(keepLang, false);
                SafeAreaFitter.NormalizedOverride = null;
                BgMotion.ClockOverride = null;
                _dense = true;
            }
            Debug.Log("[KatKat] Meta reklamı kareleri tamam → " + _root);
        }
    }
}
