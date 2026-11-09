using System;
using System.Collections;
using System.Collections.Generic;
using KatKat.Levels;
using UnityEngine;

namespace KatKat.Game
{
    /// <summary>
    /// Resources/Levels altındaki level_###.json dosyaları, sayısal sırayla.
    /// E paketi (2026-11-06): açılışta yalnız dosya adları alınır; bölüm ilk istendiğinde ayrıştırılır ve saklanır.
    /// Ölçüm: 1.000 bölümün hepsini açılışta ayrıştırmak ~600 ms ve +29 MB; 10.000'de ~5,7 sn ve +297 MB.
    /// Bölüm kimliği dosya adıdır (level_001); <see cref="ParseAll"/> hepsini doğrular (testler).
    /// </summary>
    public sealed class LevelCatalog
    {
        public readonly LazyLevels Levels;
        public readonly List<string> LoadErrors = new List<string>();
        private readonly string[] _names;
        private readonly Dictionary<string, int> _index;

        public int Count => _names.Length;

        private LevelCatalog(string[] names)
        {
            _names = names;
            _index = new Dictionary<string, int>(names.Length);
            for (int i = 0; i < names.Length; i++) _index[names[i]] = i;
            Levels = new LazyLevels(this);
        }

        /// <summary>Son yüklemenin kaynağı: "index" (dizin dosyası) ya da "scan" (yedek: bütün metinleri tarama).</summary>
        public static string LastSource { get; private set; } = "";

        public static LevelCatalog Load()
        {
            // v10 P1.6: önce dizin dosyası (açılışta bölüm metni yüklenmez).
            var idx = Resources.Load<TextAsset>(LevelIndex.ResourcePath);
            if (idx != null)
            {
                try
                {
                    var names = LevelIndex.Parse(idx.text);
                    Resources.UnloadAsset(idx);
                    LastSource = "index";
                    return new LevelCatalog(names);
                }
                catch (FormatException e)
                {
                    Debug.LogError("[KatKat] Bölüm dizini okunamadı, tarama kullanılıyor: " + e.Message);
                    Resources.UnloadAsset(idx);
                }
            }
            LastSource = "scan";
            return Scan();
        }

        /// <summary>Yedek yol: Resources/Levels altındaki bütün metinleri yükleyip adları alır (dizin yoksa ya da bozuksa).</summary>
        public static LevelCatalog Scan()
        {
            var assets = Resources.LoadAll<TextAsset>("Levels");
            var names = new List<string>(assets.Length);
            foreach (var a in assets) if (a.name.StartsWith("level_", StringComparison.Ordinal)) names.Add(a.name);
            // Sayısal sıra: level_1000 metin sırasında level_101'den önce gelirdi (10 000 bölüme hazır).
            names.Sort(LevelIndex.Compare);
            // Metinler bellekte tutulmaz: bölüm istenince yeniden yüklenir (Resources önbelleğinden).
            foreach (var a in assets) Resources.UnloadAsset(a);
            return new LevelCatalog(names.ToArray());
        }

        /// <summary>Bölüm kimliği (ayrıştırmadan): dosya adı.</summary>
        public string IdAt(int i) => _names[i];

        public int IndexOf(string id) => id != null && _index.TryGetValue(id, out int i) ? i : -1;

        /// <summary>Bütün bölümleri ayrıştırır (testler/araçlar). Hataları döndürür; kimlik ≠ dosya adı da hatadır.</summary>
        public List<string> ParseAll()
        {
            for (int i = 0; i < Count; i++)
            {
                var l = Levels[i];
                if (l != null && l.Id != _names[i]) LoadErrors.Add($"{_names[i]}.json: kimlik '{l.Id}' dosya adıyla aynı değil");
            }
            return LoadErrors;
        }

        private readonly Dictionary<int, LevelData> _parsed = new Dictionary<int, LevelData>();

        private LevelData Get(int i)
        {
            if (i < 0 || i >= _names.Length) throw new ArgumentOutOfRangeException(nameof(i));
            if (_parsed.TryGetValue(i, out var d)) return d;
            var ta = Resources.Load<TextAsset>("Levels/" + _names[i]);
            try
            {
                d = ta != null ? LevelParser.Parse(ta.text, _names[i] + ".json") : null;
                if (d == null) LoadErrors.Add(_names[i] + ".json: bulunamadı");
            }
            catch (FormatException e)
            {
                d = null;
                LoadErrors.Add(e.Message);
                Debug.LogError("[KatKat] Seviye okunamadı: " + e.Message);
            }
            if (ta != null) Resources.UnloadAsset(ta);
            _parsed[i] = d; // hatalı bölüm de saklanır (her erişimde yeniden denenmez)
            return d;
        }

        /// <summary>Levels[i]: ilk erişimde ayrıştırır. Okunamayan bölüm null döner (StartLevel hata ekranı gösterir).</summary>
        public sealed class LazyLevels : IReadOnlyList<LevelData>
        {
            private readonly LevelCatalog _c;
            internal LazyLevels(LevelCatalog c) { _c = c; }
            public LevelData this[int index] => _c.Get(index);
            public int Count => _c.Count;
            public IEnumerator<LevelData> GetEnumerator() { for (int i = 0; i < Count; i++) yield return this[i]; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
