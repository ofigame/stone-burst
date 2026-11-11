using System;
using System.Collections.Generic;
using System.IO;
using KatKat.Game;
using KatKat.UI;
using KatKat.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Tanıtım videosu (YouTube Shorts) için kare dizisi: 1080×1920, 30 fps, gerçek oyun render'ı.
    /// Zaman elle ilerletilir (editör toplu modu). Çıktı: Logs/trailer/&lt;sahne&gt;/f_00000.png; birleştirme tools/trailer.ps1.
    /// Batch: -executeMethod KatKat.EditorTools.TrailerCapture.Run
    /// </summary>
    public static class TrailerCapture
    {
        private const int W = 1080, H = 1920, Fps = 30;
        private const float Dt = 1000f / Fps;
        private static string _root;

        /// <summary>Diller: KATKAT_TRAILER_LANGS (virgülle; varsayılan "tr,en"). Her dil Logs/trailer_&lt;dil&gt;/ altına.</summary>
        [MenuItem("Kat Kat/Yakalama/Tanıtım videosu kareleri")]
        public static void Run()
        {
            var langs = (Environment.GetEnvironmentVariable("KATKAT_TRAILER_LANGS") ?? "tr,en").Split(',');
            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            WriteWav(Path.Combine(logs, "trailer_music.wav"), Music(75f), 2);
            foreach (var l in langs) RunLang(l.Trim(), Path.Combine(logs, "trailer_" + l.Trim()));
        }

        private static void RunLang(string lang, string root)
        {
            _root = root;
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            Directory.CreateDirectory(_root);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set(lang, false);
            GameController.ToursOverride = false;
            try
            {
                // 1) Harita yolculuğu: çölden (bölge 3) aşağı, bahçe ve sahil boyunca mevcut bölüme.
                Scene("01_harita", (gc, rec) =>
                {
                    int cur = 236;
                    gc.Menu.ShowLevels(gc.Catalog.Count, cur, i => i < cur, () => { });
                    float yCur = gc.Menu.ScrollYForTest;
                    float yLow = Mathf.Max(0f, yCur - 12500f); // çölden bahçeye: bölge sınırı ve köprü görünür
                    // Açılış: mevcut bölümde 1 sn (maskot zıplar), sonra 4 sn aşağı akış, 1 sn yavaşlama, sonra geri 2,5 sn.
                    for (int f = 0; f < Fps; f++) { gc.Menu.Tick(Dt / 1000f); gc.Menu.ScrollMapForTest(yCur); rec(); }
                    int down = (int)(Fps * 4.5f);
                    for (int f = 0; f <= down; f++)
                    {
                        float t = Ease(f / (float)down);
                        gc.Menu.ScrollMapForTest(Mathf.Lerp(yCur, yLow, t));
                        gc.Menu.Tick(Dt / 1000f); rec();
                    }
                    int up = (int)(Fps * 2.5f);
                    for (int f = 0; f <= up; f++)
                    {
                        float t = Ease(f / (float)up);
                        gc.Menu.ScrollMapForTest(Mathf.Lerp(yLow, yCur, t));
                        gc.Menu.Tick(Dt / 1000f); rec();
                    }
                    for (int f = 0; f < 10; f++) { gc.Menu.Tick(Dt / 1000f); rec(); }
                    // El girer ve mevcut bölüme dokunur.
                    _hand.Enter(new Vector2(W + 80f, 420f));
                    var node = FindDeep(gc.Menu.transform, "L" + (cur + 1));
                    var nodeBtn = node != null ? node.GetComponentInChildren<Button>() : null; // katman tam boy; düğmenin kendisi hedef
                    if (nodeBtn != null) TapAt(UiPx(nodeBtn.transform), () => gc.Menu.Tick(Dt / 1000f), rec, null);
                    for (int f = 0; f < 8; f++) { gc.Menu.Tick(Dt / 1000f); rec(); }
                });

                // 2) Oynanış: farklı tema ve mekanikten birkaç bölüm, kayıtlı çözümle, gerçek hızda; kazanma kartıyla biter.
                int n = 0;
                foreach (var idx in new[] { 37, 104, 220, 51 })
                {
                    int i = idx;
                    Scene($"0{2 + n}_oyun_l{i + 1:000}", (gc, rec) => PlayLevel(gc, rec, i, n == 0 ? 99 : 5));
                    n++;
                }

                // 3) Kapanış: harita en üstte, logo.
                Scene("09_kapanis", (gc, rec) =>
                {
                    gc.Menu.ShowLevels(gc.Catalog.Count, 6, i => i < 6, () => { });
                    for (int f = 0; f < Fps * 3; f++) { gc.Menu.Tick(Dt / 1000f); rec(); }
                });
            }
            finally
            {
                GameController.ToursOverride = null;
                Loc.Set(keepLang, false);
                SafeAreaFitter.NormalizedOverride = null;
            }
            Debug.Log("[KatKat] Tanıtım kareleri tamam → " + _root);
        }

        // ---- Vurucu kurgu (2026-11-12, Arapça tanıtım; kullanıcı: "güzel vurucu bir edit olsun") ---------------------
        // Piyasadaki bulmaca reklamları gibi: ilk saniyede en gösterişli an, kısa kesimler, ritme oturan müzik.
        // Anlar LevelLab "moments" ile seçildi (kayıtlı çözümde en çok zincir/Altın Rulo). Kurgu: tools/trailer-punch.ps1.

        /// <summary>Dil KATKAT_TRAILER_LANGS (varsayılan "ar"). Çıktı Logs/punch_&lt;dil&gt;/, müzik ve efektler Logs/punch_*.wav.</summary>
        [MenuItem("Kat Kat/Yakalama/Vurucu tanıtım kareleri")]
        public static void RunPunch()
        {
            var langs = (Environment.GetEnvironmentVariable("KATKAT_TRAILER_LANGS") ?? "ar").Split(',');
            var logs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            WriteWav(Path.Combine(logs, "punch_music.wav"), PunchMusic(32f), 2);
            WriteWav(Path.Combine(logs, "punch_whoosh.wav"), Whoosh(), 2);
            WriteWav(Path.Combine(logs, "punch_impact.wav"), Impact(), 2);
            foreach (var l in langs) RunPunchLang(l.Trim(), Path.Combine(logs, "punch_" + l.Trim()));
        }

        private static void RunPunchLang(string lang, string root)
        {
            _root = root;
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            Directory.CreateDirectory(_root);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keepLang = Loc.Code;
            Loc.Set(lang, false);
            GameController.ToursOverride = false;
            try
            {
                Scene("p1_kanca_l791", (gc, rec) => PlayRange(gc, rec, 790, 0, 1, 1.6f, 1.8f));
                Scene("p2_temel_l122", (gc, rec) => PlayRange(gc, rec, 121, 0, 99, 1.6f, 0f));
                Scene("p3_rulo_l480", (gc, rec) => PlayRange(gc, rec, 479, 0, 2, 1.6f, 1.2f));
                Scene("p4_rulo_l769", (gc, rec) => PlayRange(gc, rec, 768, 2, 3, 1.6f, 1.4f));
                Scene("p5_buz_l105", (gc, rec) => PlayRange(gc, rec, 104, 0, 4, 1.7f, 0.3f));
                Scene("p6_zarf_l221", (gc, rec) => PlayRange(gc, rec, 220, 0, 4, 1.7f, 0.3f));
                Scene("p7_zafer_l131", (gc, rec) => PlayRange(gc, rec, 130, 0, 99, 1.6f, 0f));
                Scene("p8_harita", (gc, rec) =>
                {
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
            }
            Debug.Log("[KatKat] Vurucu tanıtım kareleri tamam → " + _root);
        }

        /// <summary>
        /// Bölümü açar; [from, to) adımlarını el ile ve kayıtla oynar (öncekiler kayıtsız, hızlıca uygulanır).
        /// Bölüm kazanılırsa kutlama ve kart kaydedilir (Devam'a dokunulmaz); değilse hold sn bekler.
        /// </summary>
        private static void PlayRange(GameController gc, Action rec, int index, int from, int to, float speed, float hold)
        {
            // Tür/patron tanıtım kartı tahtayı örtmesin: hepsi görülmüş sayılır.
            var seen = gc.SaveService.Data.meta.seenTypes;
            foreach (var t in new[] { "aha", "bridge", "chain", "compact", "exact", "order", "protect", "pure", "resource", "reverse",
                                      "scarce", "seal", "seal_room", "stages", "twin", "twin_seal", "twin_stages", "wide" })
                if (!seen.Contains(t)) seen.Add(t);
            seen.Add("boss@" + gc.Catalog.IdAt(index));
            gc.StartLevel(index);
            for (float t = 0; t < 400f; t += Dt) { gc.Hud.AdvanceVeil(Dt); TickAll(gc, Dt); }
            var sol = gc.CurrentLevel.Solution;
            gc.Board.Director.TimeScale = 4f;
            for (int s = 0; s < Mathf.Min(from, sol.Count); s++)
            {
                gc.PlayStep(sol[s]);
                int g = 0;
                while ((gc.CurrentPhase == GameController.Phase.Animating || gc.Board.IsPlaying) && g++ < Fps * 20) TickAll(gc, Dt * 4f);
                for (float t = 0; t < 1500f; t += Dt) TickAll(gc, Dt); // tepsiye uçuş ve parıltılar otursun
            }
            gc.Board.Director.TimeScale = speed;
            for (int f = 0; f < Fps * 0.35f; f++) { TickAll(gc, Dt); rec(); }
            Action tick = () => TickAll(gc, Dt);
            _hand.Enter(new Vector2(W + 80f, 300f));
            int end = Mathf.Min(to, sol.Count);
            for (int s = from; s < end; s++)
            {
                var step = sol[s];
                Vector2 at;
                if (step.Type == "use")
                {
                    var slot = FindDeep(gc.Hud.transform, "Slot" + step.Row);
                    if (slot != null) TapAt(UiPx(slot), tick, rec, null);
                    at = CellPx(gc, step.X, step.Y);
                }
                else if (step.Type == "roll")
                {
                    var roll = FindDeep(gc.Hud.transform, "RollSlot");
                    if (roll != null) TapAt(UiPx(roll), tick, rec, null);
                    at = CellPx(gc, gc.Session.State.Board.Width / 2, step.Row);
                }
                else at = CellPx(gc, step.X, step.Y);
                TapAt(at, tick, rec, () => gc.PlayStep(step));
                var away = at + new Vector2(140f, -200f);
                int guard = 0;
                while ((gc.CurrentPhase == GameController.Phase.Animating || gc.Board.IsPlaying) && guard++ < Fps * 8)
                {
                    _hand.Pos = Vector2.Lerp(_hand.Pos, away, 0.15f);
                    TickAll(gc, Dt * speed); rec();
                }
                for (int f = 0; f < 2; f++) { TickAll(gc, Dt); rec(); }
                if (gc.IsCelebrating || gc.Hud.PanelOpen) break;
            }
            _hand.Leave();
            if (!(gc.IsCelebrating || gc.Hud.PanelOpen))
            {
                for (int f = 0; f < Fps * hold; f++) { TickAll(gc, Dt * speed); rec(); }
                return;
            }
            int g2 = 0;
            while (!gc.Hud.PanelOpen && g2++ < Fps * 8) { TickAll(gc, Dt * speed); rec(); }
            for (int f = 0; f < Fps * 1.6f; f++) { gc.Hud.AdvancePanel(Dt); TickAll(gc, Dt); rec(); }
        }

        /// <summary>
        /// Vurucu müzik (koddan, telif yok): 120 BPM (vuruş 0,5 sn, ölçü 2 sn), La minör Am–F–C–G.
        /// Ölçü 0: sert giriş (davul + bas + akor vuruşu); 1–6: dörtlük davul, el çırpma, ofbit bas, 16'lık arpej;
        /// 7: gerilim (yükselen gürültü, davul seyrekleşir); 8–11: tam enerji; 12+: kapanış akoru ve çan, sönüş.
        /// </summary>
        private static float[] PunchMusic(float seconds)
        {
            const int Sr = SoundBank.SampleRate;
            int n = (int)(seconds * Sr);
            var L = new float[n]; var R = new float[n];
            const float beat = 0.5f, bar = 2f;
            int[][] chords = { new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 48, 52, 55 }, new[] { 55, 59, 62 } };
            int[] bass = { 45, 41, 48, 43 };
            float Hz(int m) => 440f * Mathf.Pow(2f, (m - 69) / 12f);
            void Add(float[] buf, float t0, float dur, Func<float, float> voice, float gain)
            {
                int s0 = (int)(t0 * Sr), len = (int)(dur * Sr);
                for (int k = 0; k < len && s0 + k < buf.Length; k++) { if (s0 + k < 0) continue; buf[s0 + k] += voice(k / (float)Sr) * gain; }
            }
            void Both(float t0, float dur, Func<float, float> v, float g) { Add(L, t0, dur, v, g); Add(R, t0, dur, v, g); }
            var rnd = new System.Random(7);
            float Noise() => (float)rnd.NextDouble() * 2f - 1f;
            Func<float, float> kick = t => Mathf.Sin(2f * Mathf.PI * (45f * t + 90f * (1f - Mathf.Exp(-t * 28f)) / 28f)) * Mathf.Exp(-t * 7f) * Mathf.Clamp01(t / 0.002f);
            Func<float, float> clap = t => Noise() * (Mathf.Exp(-t * 26f) + 0.5f * Mathf.Exp(-Mathf.Abs(t - 0.012f) * 200f));
            Func<float, float> hat = t => Noise() * Mathf.Exp(-t * 90f);
            int bars = Mathf.CeilToInt(seconds / bar);
            const int Outro = 12;
            for (int b = 0; b < bars; b++)
            {
                float t0 = b * bar;
                var ch = chords[b % 4];
                bool riser = b == 7, outro = b >= Outro;
                // Pad (testere benzeri, yumuşak; her vuruşta "pompalama").
                foreach (var m in ch)
                {
                    float f = Hz(m);
                    Func<float, float> pad(float det) => t =>
                    {
                        float ph = f * det * t; float saw = 0f;
                        for (int h = 1; h <= 6; h++) saw += Mathf.Sin(2f * Mathf.PI * ph * h) / h;
                        float pump = outro ? 1f : 0.35f + 0.65f * Mathf.Clamp01((t % beat) / 0.18f);
                        return saw * pump * Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((bar + 0.05f - t) / 0.05f);
                    };
                    float g = outro ? (b == Outro ? 0.05f : 0.035f) : 0.026f;
                    float dur = outro ? bar * 2f : bar;
                    if (outro && b > Outro) continue;
                    Add(L, t0, dur, pad(0.997f), g); Add(R, t0, dur, pad(1.003f), g);
                }
                if (outro)
                {
                    if (b == Outro)
                    {
                        Both(t0, 1.2f, kick, 0.5f);
                        Both(t0, 2.5f, t => Mathf.Sin(2f * Mathf.PI * Hz(33) * t) * Mathf.Exp(-t * 1.6f), 0.28f);
                        foreach (var m in new[] { 81, 88, 93 })
                        {
                            float f = Hz(m);
                            Func<float, float> bell = t => (Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t)) * Mathf.Exp(-t * 1.4f);
                            Add(L, t0, 3.5f, bell, 0.035f); Add(R, t0 + 0.015f, 3.5f, bell, 0.035f);
                        }
                    }
                    continue;
                }
                // Davul.
                for (int v = 0; v < 4; v++)
                {
                    float tv = t0 + v * beat;
                    if (!riser || v < 2) Both(tv, 0.45f, kick, 0.55f);
                    if (v % 2 == 1 && b >= 1 && !riser) { Add(L, tv, 0.25f, clap, 0.16f); Add(R, tv + 0.004f, 0.25f, clap, 0.16f); }
                    if (b >= 1) { Add(L, tv + beat * 0.5f, 0.06f, hat, 0.06f); Add(R, tv + beat * 0.5f, 0.06f, hat, 0.05f); }
                }
                // Bas: ofbit 8'likler (kick'in arasında), kalın.
                if (!riser)
                    for (int e = 0; e < 8; e++)
                    {
                        if (e % 2 == 0 && b > 0) continue;
                        float f = Hz(bass[b % 4]);
                        Func<float, float> bv = t => { float s = 0f; for (int h = 1; h <= 4; h++) s += Mathf.Sin(2f * Mathf.PI * f * h * t) / (h * h); return s * Mathf.Exp(-t * 5f) * Mathf.Clamp01(t / 0.004f); };
                        Both(t0 + e * beat * 0.5f, 0.24f, bv, 0.2f);
                    }
                // Arpej (16'lık pluck), ölçü 1'den itibaren; ölçü 8+ bir oktav yukarı katman.
                if (b >= 1)
                {
                    int[] pat = { 0, 1, 2, 1, 0, 2, 1, 2, 0, 1, 2, 1, 2, 1, 0, 1 };
                    for (int e = 0; e < 16; e++)
                    {
                        if (riser && e >= 8) break;
                        int m = ch[pat[e]] + 12;
                        float f = Hz(m), te = t0 + e * beat * 0.25f;
                        Func<float, float> pl = t => (Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * f * t) * Mathf.Exp(-t * 20f)) * Mathf.Exp(-t * 11f) * Mathf.Clamp01(t / 0.002f);
                        float g = e % 4 == 0 ? 0.07f : 0.05f;
                        Add(L, te, 0.4f, pl, g); Add(R, te + 0.006f, 0.4f, pl, g * 0.85f);
                        if (b >= 8) { float f2 = f * 2f; Func<float, float> p2 = t => Mathf.Sin(2f * Mathf.PI * f2 * t) * Mathf.Exp(-t * 14f); Add(R, te, 0.3f, p2, 0.025f); Add(L, te + 0.01f, 0.3f, p2, 0.02f); }
                    }
                }
                // Gerilim: yükselen filtreli gürültü ve hızlanan tıkırtı.
                if (riser)
                {
                    float prev = 0f;
                    Func<float, float> rise = t => { float a = Mathf.Lerp(0.02f, 0.6f, t / bar); prev = Mathf.Lerp(prev, Noise(), a); return prev * (t / bar) * (t / bar); };
                    Both(t0, bar, rise, 0.35f);
                    for (int e = 0; e < 16; e++) Both(t0 + bar * 0.5f + e * beat / 8f, 0.05f, hat, 0.05f + 0.006f * e);
                }
            }
            return Normalize(L, R, n);
        }

        private static float[] Normalize(float[] L, float[] R, int n)
        {
            var o = new float[n * 2];
            float peak = 0f;
            for (int k = 0; k < n; k++) peak = Mathf.Max(peak, Mathf.Abs(L[k]), Mathf.Abs(R[k]));
            float norm = peak > 0.89f ? 0.89f / peak : 1f;
            for (int k = 0; k < n; k++) { o[2 * k] = L[k] * norm; o[2 * k + 1] = R[k] * norm; }
            return o;
        }

        /// <summary>Geçiş "vuuş"u (0,45 sn): gürültü, alçak geçiren süzgeç kesimi yukarı-aşağı süpürür.</summary>
        private static float[] Whoosh()
        {
            const int Sr = SoundBank.SampleRate; int n = (int)(0.45f * Sr);
            var L = new float[n]; var R = new float[n];
            var rnd = new System.Random(3); float a = 0f, b2 = 0f;
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)n, env = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.7f));
                float cut = 0.03f + 0.35f * env;
                float x = (float)rnd.NextDouble() * 2f - 1f;
                a = Mathf.Lerp(a, x, cut); b2 = Mathf.Lerp(b2, a, cut);
                L[k] = b2 * env * (1f - t * 0.6f); R[k] = b2 * env * (0.4f + t * 0.6f);
            }
            return Normalize(L, R, n);
        }

        /// <summary>Darbe (1,2 sn): derin düşen sinüs, kısa gürültü patlaması.</summary>
        private static float[] Impact()
        {
            const int Sr = SoundBank.SampleRate; int n = (int)(1.2f * Sr);
            var L = new float[n]; var R = new float[n];
            var rnd = new System.Random(5); float lp = 0f;
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)Sr;
                float boom = Mathf.Sin(2f * Mathf.PI * (38f * t + 120f * (1f - Mathf.Exp(-t * 18f)) / 18f)) * Mathf.Exp(-t * 3.2f);
                lp = Mathf.Lerp(lp, (float)rnd.NextDouble() * 2f - 1f, 0.25f);
                float crack = lp * Mathf.Exp(-t * 14f) * 0.7f;
                L[k] = boom + crack; R[k] = boom + crack * 0.9f;
            }
            return Normalize(L, R, n);
        }

        private static float Ease(float t) => t * t * (3f - 2f * t);

        // ---- Arka plan müziği (koddan; telif yok) ----------------------------------------------------------------
        // 96 BPM, C–G–Am–F (her akor bir ölçü). Katmanlar: yumuşak pad, bas, marimba benzeri arpej (8'lik),
        // hafif ritim tıkırtısı, her iki ölçüde bir çan. Çıkış stereo; arpej sağ-sol gecikmeli yankılı.

        private static float[] Music(float seconds)
        {
            const int Sr = SoundBank.SampleRate;
            int n = (int)(seconds * Sr);
            var L = new float[n]; var R = new float[n];
            const float Bpm = 96f; float beat = 60f / Bpm, bar = beat * 4f;
            // MIDI: C4=60. Akorlar (kök, üçlü, beşli) ve bas kökleri.
            int[][] chords = { new[] { 60, 64, 67 }, new[] { 59, 62, 67 }, new[] { 57, 60, 64 }, new[] { 57, 60, 65 } };
            int[] bass = { 48, 43, 45, 41 };
            float Hz(int m) => 440f * Mathf.Pow(2f, (m - 69) / 12f);
            void Add(float[] buf, float t0, float dur, Func<float, float> voice, float gain)
            {
                int s0 = (int)(t0 * Sr), len = (int)(dur * Sr);
                for (int k = 0; k < len && s0 + k < buf.Length; k++) { if (s0 + k < 0) continue; buf[s0 + k] += voice(k / (float)Sr) * gain; }
            }
            int bars = Mathf.CeilToInt(seconds / bar);
            for (int b = 0; b < bars; b++)
            {
                float t0 = b * bar;
                var ch = chords[b % 4];
                // Pad: üç ses, hafif ayrık iki kopya (genişlik), yavaş giriş ve çıkış.
                foreach (var m in ch)
                {
                    float f = Hz(m);
                    Func<float, float> pad(float det) => t => (Mathf.Sin(2f * Mathf.PI * f * det * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * f * det * t))
                        * Mathf.Clamp01(t / 0.6f) * Mathf.Clamp01((bar + 0.4f - t) / 0.6f);
                    Add(L, t0, bar + 0.4f, pad(0.998f), 0.035f);
                    Add(R, t0, bar + 0.4f, pad(1.002f), 0.035f);
                }
                // Bas: 1. ve 3. vuruş.
                for (int v = 0; v < 4; v += 2)
                {
                    float f = Hz(bass[b % 4]);
                    Func<float, float> bv = t => (Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * f * t)) * Mathf.Exp(-t * 3.2f) * Mathf.Clamp01(t / 0.01f);
                    Add(L, t0 + v * beat, beat * 2f, bv, 0.11f); Add(R, t0 + v * beat, beat * 2f, bv, 0.11f);
                }
                // Arpej (8'lik): akor sesleri bir oktav yukarıda, yukarı-aşağı; marimba tınısı. İlk iki ölçü yok (giriş).
                if (b >= 1)
                {
                    int[] pat = { 0, 1, 2, 1, 0, 2, 1, 2 };
                    for (int e = 0; e < 8; e++)
                    {
                        int m = ch[pat[e]] + 12 + (e == 5 && b % 2 == 1 ? 12 : 0);
                        float f = Hz(m);
                        Func<float, float> mar = t => (Mathf.Sin(2f * Mathf.PI * f * t) + 0.18f * Mathf.Sin(2f * Mathf.PI * f * 3.9f * t) * Mathf.Exp(-t * 30f))
                            * Mathf.Exp(-t * 7f) * Mathf.Clamp01(t / 0.003f);
                        float te = t0 + e * beat * 0.5f;
                        float g = e % 2 == 0 ? 0.075f : 0.055f;
                        Add(L, te, 0.7f, mar, g); Add(R, te, 0.7f, mar, g * 0.8f);
                        // Yankı: 3/16 vuruş sonra karşı kanal.
                        Add(R, te + beat * 0.75f, 0.7f, mar, g * 0.35f); Add(L, te + beat * 1.5f, 0.7f, mar, g * 0.18f);
                    }
                }
                // Ritim: ara vuruşlarda yumuşak gürültü tıkırtısı (3. ölçüden sonra).
                if (b >= 2)
                    for (int e = 1; e < 8; e += 2)
                    {
                        float te = t0 + e * beat * 0.5f;
                        var rnd = new System.Random(b * 31 + e);
                        Func<float, float> hat = t => ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 60f);
                        Add(L, te, 0.08f, hat, 0.03f); Add(R, te, 0.08f, hat, 0.03f);
                    }
                // Çan: iki ölçüde bir, akorun beşlisi iki oktav yukarı.
                if (b % 2 == 0 && b >= 2)
                {
                    float f = Hz(ch[2] + 24);
                    Func<float, float> bell = t => (Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t)) * Mathf.Exp(-t * 2.5f) * Mathf.Clamp01(t / 0.002f);
                    Add(L, t0, 2f, bell, 0.03f); Add(R, t0 + 0.02f, 2f, bell, 0.03f);
                }
            }
            // Birleştir (aralıklı stereo) ve tepe sınırla.
            var o = new float[n * 2];
            float peak = 0f;
            for (int k = 0; k < n; k++) peak = Mathf.Max(peak, Mathf.Abs(L[k]), Mathf.Abs(R[k]));
            float norm = peak > 0.89f ? 0.89f / peak : 1f;
            for (int k = 0; k < n; k++) { o[2 * k] = L[k] * norm; o[2 * k + 1] = R[k] * norm; }
            return o;
        }

        /// <summary>16 bit PCM WAV (44,1 kHz). samples aralıklı (stereo: L,R,L,R…).</summary>
        private static void WriteWav(string path, float[] samples, int channels)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            int sr = SoundBank.SampleRate, bytes = samples.Length * 2;
            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)channels);
                w.Write(sr); w.Write(sr * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (var s in samples) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
            }
        }

        /// <summary>Bölümü açar; çözümün ilk maxSteps adımını (ya da tamamını) oynar. Bölüm biterse kutlama ve kart.</summary>
        private static void PlayLevel(GameController gc, Action rec, int index, int maxSteps)
        {
            gc.StartLevel(index);
            gc.Board.Director.TimeScale = 1.45f;
            for (float t = 0; t < 400f; t += Dt) { gc.Hud.AdvanceVeil(Dt); TickAll(gc, Dt); }
            for (int f = 0; f < Fps * 0.6f; f++) { TickAll(gc, Dt); rec(); }
            var sol = gc.CurrentLevel.Solution;
            int steps = Mathf.Min(sol.Count, maxSteps);
            Action tick = () => TickAll(gc, Dt);
            _hand.Enter(new Vector2(W + 80f, 300f));
            for (int s = 0; s < steps; s++)
            {
                var step = sol[s];
                Vector2 at;
                if (step.Type == "use")
                {
                    var slot = FindDeep(gc.Hud.transform, "Slot" + step.Row);
                    if (slot != null) TapAt(UiPx(slot), tick, rec, null);
                    at = CellPx(gc, step.X, step.Y);
                }
                else if (step.Type == "roll")
                {
                    var roll = FindDeep(gc.Hud.transform, "RollSlot");
                    if (roll != null) TapAt(UiPx(roll), tick, rec, null);
                    at = CellPx(gc, gc.Session.State.Board.Width / 2, step.Row);
                }
                else at = CellPx(gc, step.X, step.Y);
                TapAt(at, tick, rec, () => gc.PlayStep(step));
                // Animasyon boyunca el hafifçe geri çekilir (taşlar görünsün).
                var away = at + new Vector2(120f, -170f);
                int guard = 0;
                while ((gc.CurrentPhase == GameController.Phase.Animating || gc.Board.IsPlaying) && guard++ < Fps * 8)
                {
                    _hand.Pos = Vector2.Lerp(_hand.Pos, away, 0.12f);
                    TickAll(gc, Dt * gc.Board.Director.TimeScale); rec();
                }
                for (int f = 0; f < 3; f++) { TickAll(gc, Dt); rec(); } // dokunuşlar arası kısa nefes
                if (gc.IsCelebrating || gc.Hud.PanelOpen) break;
            }
            if (steps < sol.Count) { _hand.Leave(); return; } // bölüm yarıda kesildi: yalnız oynanış
            _hand.Leave();
            int g2 = 0;
            while (!gc.Hud.PanelOpen && g2++ < Fps * 8) { TickAll(gc, Dt); rec(); }
            Action panelTick = () => { gc.Hud.AdvancePanel(Dt); TickAll(gc, Dt); };
            for (int f = 0; f < Fps * 1.3f; f++) { panelTick(); rec(); }
            // Kazanma kartında "Devam"a dokunuş (en üstteki düğme).
            var cont = TopButton(gc.Hud.transform);
            if (cont != null)
            {
                _hand.Enter(new Vector2(W + 80f, 500f));
                TapAt(UiPx(cont), panelTick, rec, null);
                for (int f = 0; f < 8; f++) { panelTick(); rec(); }
            }
        }

        // ---- El (dokunma işareti) ---------------------------------------------------------------------------------

        private static HandRig _hand;

        /// <summary>Eli hedefe götürür (yumuşak yay), bastırır, onPress'i çağırır, dalga halkası bırakır ve bırakır.</summary>
        private static void TapAt(Vector2 target, Action perFrame, Action rec, Action onPress)
        {
            var from = _hand.Pos;
            float dist = Vector2.Distance(from, target);
            int frames = Mathf.Clamp(Mathf.RoundToInt(dist / 85f), 5, 12);
            var mid = (from + target) * 0.5f + new Vector2(0f, Mathf.Min(120f, dist * 0.15f));
            for (int f = 1; f <= frames; f++)
            {
                float t = Ease(f / (float)frames);
                var a = Vector2.Lerp(from, mid, t); var b = Vector2.Lerp(mid, target, t);
                _hand.Pos = Vector2.Lerp(a, b, t);
                perFrame?.Invoke(); rec();
            }
            for (int f = 1; f <= 3; f++) { _hand.Press = f / 3f; perFrame?.Invoke(); rec(); }
            onPress?.Invoke();
            _hand.Ripple(target);
            for (int f = 3; f >= 0; f--) { _hand.Press = f / 3f; perFrame?.Invoke(); rec(); }
        }

        /// <summary>Hücre merkezinin video pikseli (sol alt köken).</summary>
        private static Vector2 CellPx(GameController gc, int x, int y)
        {
            var w = gc.Board.transform.TransformPoint(gc.Board.CellCenter(x, y));
            var sp = _cam.WorldToScreenPoint(w);
            return new Vector2(sp.x, sp.y);
        }

        /// <summary>UI öğesinin merkezinin video pikseli.</summary>
        private static Vector2 UiPx(Transform t)
        {
            Canvas.ForceUpdateCanvases();
            var rt = (RectTransform)t;
            var c = rt.TransformPoint(rt.rect.center);
            return RectTransformUtility.WorldToScreenPoint(_cam, c);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { var r = FindDeep(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        /// <summary>Sonuç kartının en üstteki etkin düğmesi ("Devam").</summary>
        private static Transform TopButton(Transform hud)
        {
            var panel = hud.GetComponentInChildren<KatKat.UI.ResultPanel>(false);
            if (panel == null) return null;
            Transform best = null; float by = float.MinValue;
            foreach (var b in panel.GetComponentsInChildren<UnityEngine.UI.Button>(false))
            {
                float y = UiPx(b.transform).y;
                if (y > by) { by = y; best = b.transform; }
            }
            return best;
        }

        private static Camera _cam;

        /// <summary>Video üstü katman: işaret eden çizgi film eli ve dokunma halkaları. Birim = video pikseli.</summary>
        private sealed class HandRig
        {
            private readonly RectTransform _root, _hand;
            private readonly Image _img;
            private readonly List<(Image img, float t)> _ripples = new List<(Image, float)>();
            public Vector2 Pos;
            public float Press;
            private float _alpha, _alphaTarget;

            public HandRig(Camera cam)
            {
                var go = new GameObject("TrailerHand");
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 5000;
                canvas.scaleFactor = 1f;
                _root = (RectTransform)go.transform;
                var h = new GameObject("Hand", typeof(RectTransform));
                h.transform.SetParent(_root, false);
                _img = h.AddComponent<Image>();
                _img.sprite = HandSprite();
                _img.raycastTarget = false;
                _hand = (RectTransform)h.transform;
                _hand.anchorMin = _hand.anchorMax = Vector2.zero;
                _hand.sizeDelta = new Vector2(250f, 250f);
                _hand.pivot = new Vector2(0.40f, 0.97f); // parmak ucu
                _alpha = _alphaTarget = 0f;
                Apply();
            }

            public void Enter(Vector2 from) { if (_alpha <= 0.01f) Pos = from; _alphaTarget = 1f; }
            public void Leave() => _alphaTarget = 0f;

            public void Ripple(Vector2 at)
            {
                var r = new GameObject("Ripple", typeof(RectTransform));
                r.transform.SetParent(_root, false);
                var img = r.AddComponent<Image>();
                img.sprite = RingSprite();
                img.raycastTarget = false;
                var rt = (RectTransform)r.transform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.anchoredPosition = at;
                r.transform.SetSiblingIndex(0); // elin altında
                _ripples.Add((img, 0f));
            }

            public void Tick(float dt)
            {
                _alpha = Mathf.MoveTowards(_alpha, _alphaTarget, dt / 0.18f);
                if (_alphaTarget <= 0f && _alpha <= 0f) Pos += Vector2.zero;
                for (int i = _ripples.Count - 1; i >= 0; i--)
                {
                    var (img, t) = _ripples[i];
                    t += dt;
                    float k = t / 0.45f;
                    if (k >= 1f) { UnityEngine.Object.DestroyImmediate(img.gameObject); _ripples.RemoveAt(i); continue; }
                    float s = Mathf.Lerp(60f, 230f, 1f - (1f - k) * (1f - k));
                    img.rectTransform.sizeDelta = new Vector2(s, s);
                    img.color = new Color(1f, 1f, 1f, 0.85f * (1f - k));
                    _ripples[i] = (img, t);
                }
                Apply();
            }

            private void Apply()
            {
                _hand.anchoredPosition = Pos + new Vector2(0f, -10f * Press);
                _hand.localScale = Vector3.one * (1f - 0.12f * Press);
                _hand.localRotation = Quaternion.Euler(0f, 0f, 14f + 6f * Press);
                _img.color = new Color(1f, 1f, 1f, _alpha);
            }

            public void Destroy() => UnityEngine.Object.DestroyImmediate(_root.gameObject);
        }

        private static Sprite _handSprite, _ringSprite;

        /// <summary>
        /// Çizgi film işaret eli (256 px, koddan): yukarı uzanan işaret parmağı, kıvrık parmaklar, başparmak, turkuaz kol
        /// bandı; ten dolgusu, koyu kahve kontur, yumuşak gölge. Parmak ucu üstte (pivot 0.40, 0.97).
        /// </summary>
        private static Sprite HandSprite()
        {
            if (_handSprite) return _handSprite;
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "trailer_hand", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color[N * N];
            float Cap(Vector2 p, Vector2 a, Vector2 b, float r)
            {
                var pa = p - a; var ba = b - a;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
                return (pa - ba * h).magnitude - r;
            }
            float Box(Vector2 p, Vector2 c, Vector2 half, float r)
            {
                var q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(r, r);
                return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
            }
            Color skin = new Color32(0xFF, 0xDB, 0xBE, 0xFF), skinDark = new Color32(0xF0, 0xB8, 0x96, 0xFF);
            Color line = new Color32(0x5E, 0x34, 0x26, 0xFF), cuff = new Color32(0x1E, 0x9C, 0x92, 0xFF), cuffDark = new Color32(0x14, 0x74, 0x6C, 0xFF);
            const float Lw = 0.022f;
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                var p = new Vector2((i + 0.5f) / N, (j + 0.5f) / N);
                float index = Cap(p, new Vector2(0.40f, 0.45f), new Vector2(0.40f, 0.885f), 0.078f);
                float f1 = Cap(p, new Vector2(0.53f, 0.44f), new Vector2(0.55f, 0.50f), 0.07f);
                float f2 = Cap(p, new Vector2(0.65f, 0.42f), new Vector2(0.66f, 0.47f), 0.066f);
                float f3 = Cap(p, new Vector2(0.75f, 0.38f), new Vector2(0.76f, 0.42f), 0.058f);
                float thumb = Cap(p, new Vector2(0.40f, 0.27f), new Vector2(0.27f, 0.42f), 0.062f);
                float palm = Box(p, new Vector2(0.56f, 0.30f), new Vector2(0.20f, 0.15f), 0.10f);
                float cf = Box(p, new Vector2(0.58f, 0.10f), new Vector2(0.19f, 0.075f), 0.04f);
                float handD = Mathf.Min(Mathf.Min(Mathf.Min(index, f1), Mathf.Min(f2, f3)), Mathf.Min(thumb, palm));
                float all = Mathf.Min(handD, cf);
                // Gölge (sağ alta kaymış, yumuşak).
                var ps = p - new Vector2(0.025f, -0.03f);
                float sh = Mathf.Min(
                    Mathf.Min(Cap(ps, new Vector2(0.40f, 0.45f), new Vector2(0.40f, 0.885f), 0.078f), Box(ps, new Vector2(0.56f, 0.30f), new Vector2(0.20f, 0.15f), 0.10f)),
                    Mathf.Min(Cap(ps, new Vector2(0.65f, 0.42f), new Vector2(0.66f, 0.47f), 0.066f), Box(ps, new Vector2(0.58f, 0.10f), new Vector2(0.19f, 0.075f), 0.04f)));
                Color c = new Color(0f, 0f, 0f, Mathf.Clamp01((0.03f - sh) / 0.05f) * 0.28f);
                if (all < 0f)
                {
                    bool isCuff = cf < handD && cf < 0f && p.y < 0.19f;
                    if (isCuff) c = Color.Lerp(cuff, cuffDark, Mathf.Clamp01((0.10f - p.y) / 0.12f));
                    else
                    {
                        // Gölgelendirme: kenara yakın ve sağ tarafta koyu.
                        float edge = Mathf.Clamp01(-handD / 0.06f);
                        c = Color.Lerp(skinDark, skin, edge * 0.8f + (p.x < 0.5f ? 0.2f : 0f));
                        // Tırnak.
                        if (Box(p, new Vector2(0.40f, 0.89f), new Vector2(0.042f, 0.045f), 0.035f) < 0f) c = Color.Lerp(c, new Color32(0xFF, 0xF0, 0xE6, 0xFF), 0.75f);
                    }
                    // İç çizgiler: kıvrık parmakların ve başparmağın kendi kenarları, kol bandı sınırı.
                    float inner = Mathf.Min(Mathf.Min(Mathf.Abs(f1), Mathf.Abs(f2)), Mathf.Min(Mathf.Abs(f3), Mathf.Abs(thumb)));
                    if (inner < Lw * 0.7f && p.y > 0.2f) c = Color.Lerp(c, line, 0.85f);
                    if (Mathf.Abs(cf) < Lw * 0.7f && p.y > 0.12f) c = Color.Lerp(c, line, 0.9f);
                }
                // Dış kontur (kenar yumuşatmalı).
                float o = Mathf.Abs(all) - Lw;
                if (o < 0.004f)
                {
                    float a = Mathf.Clamp01((0.004f - o) / 0.006f);
                    var lc = new Color(line.r, line.g, line.b, 1f);
                    c = all < 0f ? Color.Lerp(c, lc, a) : new Color(lc.r, lc.g, lc.b, Mathf.Max(c.a, a));
                }
                px[j * N + i] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return _handSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.40f, 0.97f), 100f);
        }

        private static Sprite RingSprite()
        {
            if (_ringSprite) return _ringSprite;
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "trailer_ring", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            var px = new Color[N * N];
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float x = (i + 0.5f) / N - 0.5f, y = (j + 0.5f) / N - 0.5f, d = Mathf.Sqrt(x * x + y * y);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.40f) / 0.06f);
                float fill = d < 0.40f ? 0.25f : 0f;
                px[j * N + i] = new Color(1f, 1f, 1f, Mathf.Max(ring, fill * Mathf.Clamp01((0.4f - d) / 0.05f)));
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return _ringSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void TickAll(GameController gc, float ms)
        {
            gc.Board.Advance(ms);
            gc.Board.Director.Advance(ms);
            gc.Hud.AdvanceChips(ms);
            gc.Hud.AdvanceEffects(ms);
            if (gc.Board.Decor != null) gc.Board.Decor.Tick(ms / 1000f);
        }

        /// <summary>Sahne kurar, kare kaydedici verir, bitince temizler.</summary>
        private static void Scene(string name, Action<GameController, Action> body)
        {
            SafeAreaFitter.NormalizedOverride = new Rect(0f, 0f, 1f, 1f);
            var dir = Path.Combine(_root, name);
            Directory.CreateDirectory(dir);
            var camGo = new GameObject("TrailerCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = rt;
            cam.aspect = (float)W / H;
            var saveDir = Path.Combine(Path.GetTempPath(), "katkat_trailer");
            if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
            // Oyun sesleri: çalındığı karenin zamanına miksajlanır (ses aygıtı gerekmez).
            const int Spf = SoundBank.SampleRate / Fps; // kare başına örnek
            var mix = new List<float>();
            int frame = 0;
            SoundBank.CaptureTap = (clip, pitch, vol) =>
            {
                if (!SoundBank.Samples.TryGetValue(clip, out var src)) return;
                int at = frame * Spf;
                int len = (int)(src.Length / pitch);
                while (mix.Count < at + len) mix.Add(0f);
                for (int k = 0; k < len; k++)
                {
                    float p = k * pitch; int i0 = (int)p; float fr = p - i0;
                    float s = i0 + 1 < src.Length ? Mathf.Lerp(src[i0], src[i0 + 1], fr) : src[Mathf.Min(i0, src.Length - 1)];
                    mix[at + k] += s * vol;
                }
            };
            var root = new GameObject("Game");
            var gc = root.AddComponent<GameController>();
            gc.Setup(cam, new KatKat.Save.SaveService(saveDir));
            for (int i = 0; i < 240; i++) { gc.SaveService.MarkCompleted(gc.Catalog.IdAt(i), i, gc.Catalog.Count); gc.SaveService.RecordStars(gc.Catalog.IdAt(i), i % 7 == 3 ? 2 : 3); }
            gc.Hud.Canvas.scaleFactor = W / HudView.RefWidth;
            gc.Menu.Canvas.scaleFactor = W / HudView.RefWidth;
            gc.Hud.ApplySafeArea();
            gc.Menu.ApplySafeArea();
            _cam = cam;
            _hand = new HandRig(cam);
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            void Rec()
            {
                _hand.Tick(Dt / 1000f);
                RecRaw();
            }
            void RecRaw()
            {
                Canvas.ForceUpdateCanvases();
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, $"f_{frame++:00000}.png"), tex.EncodeToPNG());
            }
            try
            {
                body(gc, Rec);
                // Sahnenin sesi tam kare süresi kadar (taşan efekt kuyruğu kesilir).
                var a = new float[frame * Spf];
                for (int k = 0; k < a.Length && k < mix.Count; k++) a[k] = mix[k];
                WriteWav(Path.Combine(dir, "sfx.wav"), a, 1);
            }
            finally
            {
                SoundBank.CaptureTap = null;
                _hand.Destroy();
                SoundBank.Samples.Clear();
                Debug.Log($"[KatKat] tanıtım {name}: {frame} kare");
                UnityEngine.Object.DestroyImmediate(tex);
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(camGo);
                foreach (var es in UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>()) UnityEngine.Object.DestroyImmediate(es.gameObject);
            }
        }
    }
}
