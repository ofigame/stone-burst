using System.Diagnostics;
using KatKat.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;

namespace KatKat.Tests
{
    /// <summary>v10 P1.6: katalog açılışta dizin dosyasını okur; sonuç eski taramayla birebir aynıdır.</summary>
    public class CatalogIndexTests
    {
        [Test]
        public void Load_UsesIndex_AndMatchesScan()
        {
            var c = LevelCatalog.Load();
            Assert.AreEqual("index", LevelCatalog.LastSource, "dizin dosyası kullanıldı");
            var s = LevelCatalog.Scan();
            Assert.AreEqual(1000, c.Count);
            Assert.AreEqual(s.Count, c.Count);
            for (int i = 0; i < c.Count; i++) Assert.AreEqual(s.IdAt(i), c.IdAt(i), "sıra #" + i);
            Assert.AreEqual("level_038", c.Levels[37].Id, "bölüm istenince okunuyor");
        }

        /// <summary>Ölçüm (rapor için): dizinle ve taramayla açılış süresi ve anlık bellek.</summary>
        [Test]
        public void Measure_IndexVsScan()
        {
            Resources.UnloadUnusedAssets();
            long m0 = Profiler.GetMonoUsedSizeLong(), n0 = Profiler.GetTotalAllocatedMemoryLong();
            var sw = Stopwatch.StartNew();
            var a = LevelCatalog.Load();
            double tIndex = sw.Elapsed.TotalMilliseconds;
            long nIndex = Profiler.GetTotalAllocatedMemoryLong() - n0;
            Resources.UnloadUnusedAssets();
            n0 = Profiler.GetTotalAllocatedMemoryLong();
            sw.Restart();
            var b = LevelCatalog.Scan();
            double tScan = sw.Elapsed.TotalMilliseconds;
            long nScan = Profiler.GetTotalAllocatedMemoryLong() - n0;
            UnityEngine.Debug.Log($"[KatKat] P1.6 ölçüm: dizin {tIndex:0.0} ms / anlık +{nIndex / 1024} KB; tarama {tScan:0.0} ms / anlık +{nScan / 1024} KB (mono başlangıç {m0 / 1024} KB)");
            Assert.AreEqual(a.Count, b.Count);
        }
    }
}

namespace KatKat.Tests
{
    /// <summary>v10 P1.5: dünya katmanı oyunun bölge yapısıyla uyumlu; harita koduna dokunulmadı.</summary>
    public class WorldWiringTests
    {
        [NUnit.Framework.Test]
        public void WorldConfig_LoadsFromResources_AndMatchesMapRegions()
        {
            NUnit.Framework.Assert.AreEqual(KatKat.View.ThemeDefinition.RegionSize, KatKat.Meta.WorldCatalog.RegionSize, "harita bölgesi = dünya bölgesi");
            var w = KatKat.Meta.WorldCatalog.Parse(Resources.Load<TextAsset>("Worlds/worlds")?.text);
            var c = LevelCatalog.Load();
            var all = w.All(c.Count);
            NUnit.Framework.Assert.AreEqual(2, all.Count);
            NUnit.Framework.Assert.AreEqual("level_500", c.IdAt(all[0].LastIndex));
            NUnit.Framework.Assert.AreEqual("level_501", c.IdAt(all[1].FirstIndex));
            // Dünya sınırı bölge sınırıyla çakışır: harita bölge levhaları ve temalar dünya içinde bölünmez.
            foreach (var d in all)
                NUnit.Framework.Assert.AreEqual(0, d.FirstIndex % KatKat.View.ThemeDefinition.RegionSize);
        }
    }
}
