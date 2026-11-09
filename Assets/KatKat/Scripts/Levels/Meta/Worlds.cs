using System;
using System.Collections.Generic;
using System.Globalization;
using KatKat.Levels;

namespace KatKat.Meta
{
    /// <summary>
    /// v10 P1.5: dünya katmanı (yalnız veri). Bir dünya ardışık bölüm aralığıdır (varsayılan 500 = 5 bölge).
    /// Mevcut 100'lük bölgeler, tema dönüşü ve harita görünümü aynen kalır; dünya yalnız üst gruplamadır
    /// (efsanevi taş, dünya tamamlama, ileride sayfalı harita ve içerik paketleri için).
    /// Yapılandırma: Resources/Worlds/worlds.json. Tanımlanmamış dünyalar (ör. 10.000 bölümde 3–20) aynı boyutla
    /// otomatik türetilir. Dünya adları açık karar (OPEN_DECISIONS #11); onaylanana kadar arayüzde gösterilmez.
    /// </summary>
    public sealed class WorldDefinition
    {
        /// <summary>Kalıcı kimlik ("world_01"); kayıtta ve sunucuda bu kullanılır, sıra numarası değil.</summary>
        public string WorldId;
        /// <summary>1 tabanlı dünya numarası.</summary>
        public int Number;
        /// <summary>0 tabanlı bölüm aralığı [FirstIndex, LastIndex].</summary>
        public int FirstIndex, LastIndex;
        /// <summary>Ad metin anahtarı; boşsa ad onaylanmamış (arayüzde dünya adı gösterilmez).</summary>
        public string NameKey = "";
        /// <summary>Yapılandırmadan değil, boyuttan türetildi.</summary>
        public bool Derived;

        public int LevelCount => LastIndex - FirstIndex + 1;
        public int FirstRegion(int regionSize) => FirstIndex / regionSize;
        public int LastRegion(int regionSize) => LastIndex / regionSize;
        public bool Contains(int levelIndex) => levelIndex >= FirstIndex && levelIndex <= LastIndex;
    }

    public sealed class WorldCatalog
    {
        public const int DefaultWorldSize = 500;
        /// <summary>Harita bölgesi boyutu (View.ThemeDefinition.RegionSize ile aynı olmalı; test denetler).</summary>
        public const int RegionSize = 100;

        public int WorldSize { get; private set; } = DefaultWorldSize;
        /// <summary>Yapılandırmada tanımlı dünyalar (sırayla, kimlik → ad).</summary>
        public readonly List<WorldDefinition> Configured = new List<WorldDefinition>();

        /// <summary>worlds.json: {"version":1,"worldSize":500,"worlds":[{"id":"world_01","nameKey":""},...]}. Boş metin = varsayılan.</summary>
        public static WorldCatalog Parse(string json)
        {
            var c = new WorldCatalog();
            if (string.IsNullOrEmpty(json)) return c;
            if (!(MiniJson.Parse(json) is Dictionary<string, object> root)) throw new FormatException("worlds.json: kök nesne olmalı");
            int version = Int(root, "version");
            if (version != 1) throw new FormatException("worlds.json: bilinmeyen sürüm " + version);
            int size = root.ContainsKey("worldSize") ? Int(root, "worldSize") : DefaultWorldSize;
            if (size <= 0 || size % RegionSize != 0) throw new FormatException($"worlds.json: worldSize {size} bölge boyutunun ({RegionSize}) katı olmalı");
            c.WorldSize = size;
            var ids = new HashSet<string>();
            if (root.TryGetValue("worlds", out var w) && w is List<object> arr)
                for (int i = 0; i < arr.Count; i++)
                {
                    if (!(arr[i] is Dictionary<string, object> o)) throw new FormatException($"worlds.json: dünya #{i + 1} nesne değil");
                    var id = o.TryGetValue("id", out var a) ? a as string : null;
                    if (string.IsNullOrEmpty(id)) throw new FormatException($"worlds.json: dünya #{i + 1} kimliksiz");
                    if (!ids.Add(id)) throw new FormatException("worlds.json: yinelenen kimlik " + id);
                    c.Configured.Add(new WorldDefinition
                    {
                        WorldId = id,
                        Number = i + 1,
                        FirstIndex = i * size,
                        LastIndex = (i + 1) * size - 1,
                        NameKey = o.TryGetValue("nameKey", out var n) && n is string s ? s : "",
                    });
                }
            return c;
        }

        /// <summary>Toplam bölüm sayısı için dünya sayısı (son dünya eksik olabilir).</summary>
        public int CountFor(int totalLevels) => totalLevels <= 0 ? 0 : (totalLevels + WorldSize - 1) / WorldSize;

        /// <summary>1 tabanlı numaradaki dünya; toplam bölüm sayısıyla son dünya kırpılır.</summary>
        public WorldDefinition World(int number, int totalLevels)
        {
            if (number < 1 || number > CountFor(totalLevels)) throw new ArgumentOutOfRangeException(nameof(number));
            WorldDefinition w;
            if (number <= Configured.Count)
            {
                var c = Configured[number - 1];
                w = new WorldDefinition { WorldId = c.WorldId, Number = c.Number, FirstIndex = c.FirstIndex, LastIndex = c.LastIndex, NameKey = c.NameKey };
            }
            else
                w = new WorldDefinition
                {
                    WorldId = "world_" + number.ToString("00", CultureInfo.InvariantCulture),
                    Number = number,
                    FirstIndex = (number - 1) * WorldSize,
                    LastIndex = number * WorldSize - 1,
                    Derived = true,
                };
            w.LastIndex = Math.Min(w.LastIndex, totalLevels - 1);
            return w;
        }

        /// <summary>0 tabanlı bölüm sırasının dünyası.</summary>
        public WorldDefinition WorldOf(int levelIndex, int totalLevels)
        {
            if (levelIndex < 0 || levelIndex >= totalLevels) throw new ArgumentOutOfRangeException(nameof(levelIndex));
            return World(levelIndex / WorldSize + 1, totalLevels);
        }

        public List<WorldDefinition> All(int totalLevels)
        {
            var list = new List<WorldDefinition>();
            for (int n = 1; n <= CountFor(totalLevels); n++) list.Add(World(n, totalLevels));
            return list;
        }

        /// <summary>Dünyada tamamlanan bölüm sayısı (kimlik dosya adından: levelIdAt).</summary>
        public static int CompletedIn(WorldDefinition w, Func<int, string> levelIdAt, Func<string, bool> isCompleted)
        {
            int n = 0;
            for (int i = w.FirstIndex; i <= w.LastIndex; i++) if (isCompleted(levelIdAt(i))) n++;
            return n;
        }

        private static int Int(Dictionary<string, object> o, string k) =>
            o.TryGetValue(k, out var v) && v != null ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : 0;
    }
}
