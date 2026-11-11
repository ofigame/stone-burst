using System;
using System.Collections.Generic;
using KatKat.Meta;

namespace KatKat.Save
{
    /// <summary>Yerel, sürümlü kayıt. Yarım bölüm kaydı yoktur; oyuna dönünce bölüm baştan başlar.</summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>
        /// 1: ilerleme, ayarlar, bölüm başına en iyi yıldız. 2 (v9): yıldızlar normalize edilir (kimlik başına en yüksek
        /// geçerli 1–3), <see cref="meta"/> eklenir (koleksiyon, günlük haklar, ödül günlüğü).
        /// </summary>
        public const int CurrentVersion = 2;

        public int saveVersion = CurrentVersion;
        /// <summary>Yükseltmenin yapıldığı eski sürüm (0 = yeni kayıt ya da yükseltme yok). Bir kez yazılır.</summary>
        public int upgradedFrom;
        /// <summary>Açılmış en yüksek bölüm indeksi (0 tabanlı).</summary>
        public int highestUnlocked;
        public List<string> completed = new List<string>();
        public bool sound = true;
        public bool vibration = true;
        /// <summary>Azaltılmış hareket (2026-11-11, vr1): sarsıntı ve sürekli büyüyüp küçülme kapalı.</summary>
        public bool reducedMotion;

        /// <summary>v5: bölüm başına en iyi yıldız (paralel listeler; JsonUtility sözlük yazmaz). Eski kayıtta boş.</summary>
        public List<string> starIds = new List<string>();
        public List<int> starCounts = new List<int>();

        /// <summary>v9: koleksiyon, günlük bulmaca ve ödül durumu.</summary>
        public MetaSave meta = new MetaSave();

        // E paketi: 10.000 bölümde listede doğrusal arama haritayı ~1,2 sn bekletiyordu. Kaydedilmeyen arama
        // tabloları; liste nesnesi ya da eleman sayısı değişince yeniden kurulur (listelere yalnız ekleme yapılır,
        // normalize yeni liste/az eleman bırakır). Bulunan indeks her seferinde listeyle doğrulanır.
        [NonSerialized] private HashSet<string> _doneSet;
        [NonSerialized] private List<string> _doneRef;
        [NonSerialized] private int _doneCount = -1;
        [NonSerialized] private Dictionary<string, int> _starIndex;
        [NonSerialized] private List<string> _starRef;
        [NonSerialized] private int _starCount = -1;

        public bool IsCompleted(string id)
        {
            if (completed == null || id == null) return false;
            if (_doneSet == null || !ReferenceEquals(_doneRef, completed) || _doneCount != completed.Count)
            {
                _doneSet = new HashSet<string>(completed);
                _doneRef = completed; _doneCount = completed.Count;
            }
            return _doneSet.Contains(id);
        }

        public int StarsFor(string id)
        {
            if (starIds == null || starCounts == null || id == null) return 0;
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1 || _starIndex == null || !ReferenceEquals(_starRef, starIds) || _starCount != starIds.Count)
                {
                    _starIndex = new Dictionary<string, int>();
                    for (int k = 0; k < starIds.Count; k++)
                        if (starIds[k] != null && !_starIndex.ContainsKey(starIds[k])) _starIndex[starIds[k]] = k;
                    _starRef = starIds; _starCount = starIds.Count;
                }
                if (!_starIndex.TryGetValue(id, out int i)) return 0;
                if (i < starIds.Count && starIds[i] == id) return i < starCounts.Count ? starCounts[i] : 0;
            }
            int j = starIds.IndexOf(id);
            return j >= 0 && j < starCounts.Count ? starCounts[j] : 0;
        }

        /// <summary>Kimlik başına en yüksek geçerli yıldızların toplamı.</summary>
        public int TotalStars => ProgressRules.TotalStars(starIds, starCounts) + (meta != null ? Math.Max(0, meta.dailyStars) : 0);

        /// <summary>Yalnız bölümlerden gelen yıldızlar (günlük hariç).</summary>
        public int LevelStars => ProgressRules.TotalStars(starIds, starCounts);
    }
}
