using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using KatKat.Game;
using KatKat.Levels;
using KatKat.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KatKat.Tests
{
    /// <summary>
    /// v9 E paketi ölçümü: 1.000 gerçek ve 10.000 sanal bölümle katalog yükleme, bellek, kayıt ve harita kaydırma.
    /// Sanal 10.000: gerçek 1.000 dosya metni 10 kez ayrıştırılır (Resources'a dosya eklenmez, üretim bölümleri değişmez).
    /// Editör ölçümüdür; telefon değerleri farklıdır. Normal koşuda çalışmaz: -testFilter ScalePerfTests.
    /// Sonuç: Logs/batch/olcum_E.txt
    /// </summary>
    [Explicit("Ölçüm; yalnız istenince")]
    public class ScalePerfTests
    {
        private readonly StringBuilder _out = new StringBuilder();

        private void Line(string s) { _out.AppendLine(s); UnityEngine.Debug.Log("[olcum] " + s); }

        private static long Mono() { System.GC.Collect(); System.GC.WaitForPendingFinalizers(); System.GC.Collect(); return Profiler.GetMonoUsedSizeLong(); }

        private static string Mb(long b) => (b / (1024.0 * 1024.0)).ToString("0.0") + " MB";

        [UnityTest]
        public IEnumerator Measure()
        {
            GameController.RequestedLevel = -1;
            GameController.ToursOverride = false;
            GameController.SaveDirectoryOverride = Path.Combine(Application.temporaryCachePath, "katkat_perf");
            if (Directory.Exists(GameController.SaveDirectoryOverride)) Directory.Delete(GameController.SaveDirectoryOverride, true);
            Directory.CreateDirectory(GameController.SaveDirectoryOverride);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            var gc = Object.FindAnyObjectByType<GameController>();
            Assert.IsNotNull(gc);
            Line($"Ortam: {Application.platform}, Unity {Application.unityVersion}, {SystemInfo.processorType}, {SystemInfo.systemMemorySize} MB RAM");

            // 1) Katalog (gerçek 1.000). Önbelleği boşaltıp soğuk yükleme de ölçülür.
            var sw = new Stopwatch();
            yield return Resources.UnloadUnusedAssets();
            {
                long m0 = Mono();
                sw.Restart();
                var cold = LevelCatalog.Load();
                sw.Stop();
                long m1 = Mono();
                Line($"Katalog yükleme 1000 (soğuk): {sw.Elapsed.TotalMilliseconds:0} ms, bellek +{Mb(m1 - m0)}, bölüm {cold.Count}");
                sw.Restart();
                var l500 = cold.Levels[500];
                sw.Stop();
                Line($"Tek bölüm ilk erişim (level_501): {sw.Elapsed.TotalMilliseconds:0.0} ms");
                Assert.AreEqual("level_501", l500.Id);
            }
            for (int r = 0; r < 3; r++)
            {
                long m0 = Mono();
                sw.Restart();
                var cat = LevelCatalog.Load();
                sw.Stop();
                long m1 = Mono();
                Line($"Katalog yükleme 1000 (deneme {r + 1}): {sw.Elapsed.TotalMilliseconds:0} ms, bellek +{Mb(m1 - m0)}, bölüm {cat.Count}");
                cat = null;
            }

            // 2) Sanal 10.000: Resources okuma ayrı, ayrıştırma ayrı.
            sw.Restart();
            var assets = Resources.LoadAll<TextAsset>("Levels");
            sw.Stop();
            Line($"Resources.LoadAll(Levels) {assets.Length} dosya: {sw.Elapsed.TotalMilliseconds:0} ms");
            long bytes = 0; foreach (var a in assets) bytes += a.bytes.Length;
            Line($"Bölüm JSON toplamı: {Mb(bytes)} (10.000 için ~{Mb(bytes * 10)})");
            {
                long m0 = Mono();
                sw.Restart();
                var list = new List<LevelData>(10000);
                for (int k = 0; k < 10; k++)
                    foreach (var a in assets)
                        if (a.name.StartsWith("level_")) list.Add(LevelParser.Parse(a.text, a.name + ".json"));
                sw.Stop();
                long m1 = Mono();
                Line($"Hepsini ayrıştırma 10000 (sanal; eski yöntemin maliyeti): {sw.Elapsed.TotalMilliseconds:0} ms, bellek +{Mb(m1 - m0)}, bölüm {list.Count}");
                list = null;
            }

            // 3) 10.000 bölümlük kayıt: listelerde arama, toplam yıldız, JSON yazma/okuma.
            var ids = new string[10000];
            for (int i = 0; i < ids.Length; i++) ids[i] = $"level_{i + 1:000}";
            var save = new SaveData();
            for (int i = 0; i < 10000; i++) { save.completed.Add(ids[i]); save.starIds.Add(ids[i]); save.starCounts.Add(1 + i % 3); }
            sw.Restart();
            int sum = 0;
            for (int i = 0; i < 10000; i++) sum += save.StarsFor(ids[i]);
            sw.Stop();
            Line($"Kayıt 10000: StarsFor × 10000 (haritadaki yıldız toplamı döngüsü): {sw.Elapsed.TotalMilliseconds:0} ms");
            sw.Restart();
            int done = 0;
            for (int i = 0; i < 10000; i++) if (save.IsCompleted(ids[i])) done++;
            sw.Stop();
            Line($"Kayıt 10000: IsCompleted × 10000: {sw.Elapsed.TotalMilliseconds:0} ms");
            sw.Restart();
            int total = save.TotalStars;
            sw.Stop();
            Line($"Kayıt 10000: TotalStars: {sw.Elapsed.TotalMilliseconds:0.0} ms");
            sw.Restart();
            string json = JsonUtility.ToJson(save);
            var back = JsonUtility.FromJson<SaveData>(json);
            sw.Stop();
            Line($"Kayıt 10000: JSON yaz+oku {sw.Elapsed.TotalMilliseconds:0} ms, dosya {Mb(Encoding.UTF8.GetByteCount(json))}");
            Assert.AreEqual(10000, back.completed.Count);

            // 4) Harita: 1.000 ve 10.000 düğüm. Yıldızlar 10.000 kayıttan (gerçekçi en kötü durum).
            foreach (int n in new[] { 1000, 10000 })
            {
                var menu = gc.Menu;
                menu.StarsOf = i => i < n ? save.StarsFor(ids[i]) : 0;
                int cur = n - 50;
                long m0 = Mono();
                sw.Restart();
                menu.ShowLevels(n, cur, i => i < cur, () => { });
                sw.Stop();
                yield return null;
                long m1 = Mono();
                Line($"Harita {n}: açılış {sw.Elapsed.TotalMilliseconds:0} ms (son bölüme yakın), bellek +{Mb(m1 - m0)}");
                float h = menu.MapHeightForTest;
                foreach (var start in new[] { 0.02f, 0.5f, 0.97f })
                {
                    menu.ScrollMapForTest(h * start);
                    var r = menu.MeasureMapScroll(300, 45f);
                    Line($"Harita {n}: kaydırma (başlangıç %{start * 100:0}): ortalama {r[0]:0.00} ms, en yüksek {r[1]:0.0} ms / adım");
                }
                sw.Restart();
                for (int j = 0; j < 20; j++) menu.ScrollMapForTest(h * (j % 2 == 0 ? 0.05f : 0.95f));
                sw.Stop();
                Line($"Harita {n}: uç uca sıçrama (baştan sona, 20 kez): ortalama {sw.Elapsed.TotalMilliseconds / 20:0.0} ms");
                yield return null;
            }

            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "batch", "olcum_E.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, _out.ToString(), Encoding.UTF8);
            GameController.ToursOverride = null;
        }
    }
}
