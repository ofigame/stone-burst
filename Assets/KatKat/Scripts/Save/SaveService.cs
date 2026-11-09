using System;
using System.IO;
using UnityEngine;

namespace KatKat.Save
{
    /// <summary>
    /// Kat Kat'ın kendi persistent data klasöründe tek dosya. Yazarken geçici dosya + değiştir/yedek;
    /// eksik veya bozuk kayıtta yedeğe, o da yoksa güvenli başlangıca döner ve günlüğe yazar.
    /// </summary>
    public sealed class SaveService
    {
        public const string FileName = "katkat_save.json";

        private readonly string _path;

        public SaveData Data { get; private set; }
        public string Path => _path;

        public SaveService(string directory = null)
        {
            _path = System.IO.Path.Combine(directory ?? Application.persistentDataPath, FileName);
            Data = Load(out bool upgraded);
            // Yükseltme kalıcı yazılır; ikinci açılışta kayıt zaten güncel sürümde olduğu için tekrar uygulanmaz.
            if (upgraded) Save();
        }

        private SaveData Load(out bool upgraded)
        {
            upgraded = false;
            foreach (var p in new[] { _path, _path + ".bak" })
            {
                if (!File.Exists(p)) continue;
                try
                {
                    var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(p));
                    if (d == null) throw new FormatException("boş kayıt");
                    if (d.saveVersion > SaveData.CurrentVersion) throw new FormatException("daha yeni kayıt sürümü " + d.saveVersion);
                    if (d.completed == null) d.completed = new System.Collections.Generic.List<string>();
                    if (d.highestUnlocked < 0) d.highestUnlocked = 0;
                    int fromVersion = d.saveVersion <= 0 ? 1 : d.saveVersion;
                    upgraded = Upgrade(d);
                    // v10 P1.3: yükseltmeden önceki dosyanın kalıcı kopyası (bir kez). .bak her yazışta döner;
                    // bu kopya yükseltmede bir sorun çıkarsa eski sürüme dönebilmek için kalır.
                    if (upgraded) KeepCopy(p, PreUpgradePath(fromVersion), overwrite: false);
                    if (p != _path) Debug.LogWarning("[KatKat] Ana kayıt okunamadı, yedekten yüklendi.");
                    return d;
                }
                catch (Exception e) when (e is FormatException || e is ArgumentException || e is IOException)
                {
                    Debug.LogWarning($"[KatKat] Kayıt okunamadı ({p}): {e.Message}");
                    // v10 P1.3: bozuk ana dosya bir sonraki yazışta .bak'a döner ve kaybolurdu; inceleme için kopyası.
                    if (p == _path) KeepCopy(p, _path + ".corrupt", overwrite: true);
                }
            }
            return new SaveData();
        }

        /// <summary>Yükseltme öncesi kopyanın yolu: katkat_save.v1.json gibi.</summary>
        public string PreUpgradePath(int version) =>
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_path), System.IO.Path.GetFileNameWithoutExtension(_path) + ".v" + version + ".json");

        private static void KeepCopy(string from, string to, bool overwrite)
        {
            try
            {
                if (!overwrite && File.Exists(to)) return;
                File.Copy(from, to, overwrite);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("[KatKat] Kayıt kopyası alınamadı: " + e.Message);
            }
        }

        /// <summary>
        /// Eski sürümden yükseltme (yalnız saveVersion &lt; güncel ise; bir kez). v1 → v2: yıldız listeleri normalize
        /// edilir (kimlik başına en yüksek geçerli 1–3), meta bölümü güvenli varsayılanlarla eklenir. İlerleme ve
        /// ayarlar olduğu gibi kalır. Her yüklemede eksik listeler onarılır (bu, sürüm yükseltmesi sayılmaz).
        /// </summary>
        public static bool Upgrade(SaveData d)
        {
            d.starIds ??= new System.Collections.Generic.List<string>();
            d.starCounts ??= new System.Collections.Generic.List<int>();
            d.meta ??= new Meta.MetaSave();
            d.meta.Sanitize();
            if (d.saveVersion >= SaveData.CurrentVersion) return false;
            int from = d.saveVersion <= 0 ? 1 : d.saveVersion;
            int dropped = Meta.ProgressRules.Normalize(d.starIds, d.starCounts);
            if (dropped > 0) Debug.LogWarning($"[KatKat] Kayıt yükseltmesi: {dropped} geçersiz/yinelenen yıldız kaydı ayıklandı.");
            d.upgradedFrom = from;
            d.saveVersion = SaveData.CurrentVersion;
            return true;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path));
                var tmp = _path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
                if (!File.Exists(_path)) { File.Move(tmp, _path); return; }
                try
                {
                    File.Replace(tmp, _path, _path + ".bak");
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(_path, _path + ".bak", true);
                    File.Delete(_path);
                    File.Move(tmp, _path);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError("[KatKat] Kayıt yazılamadı: " + e.Message);
            }
        }

        /// <summary>
        /// Buluttan geri yükleme: kayıt verilenle değiştirilir ve yazılır. Ses ve titreşim cihaz ayarıdır, yerelde kalır.
        /// Önceki dosya .bak olarak kalır (Save her yazışta yedeği döndürür) ve ayrıca bir kez .precloud kopyası alınır.
        /// </summary>
        public void Replace(SaveData d)
        {
            if (d == null) return;
            Upgrade(d);
            d.sound = Data.sound;
            d.vibration = Data.vibration;
            if (File.Exists(_path)) KeepCopy(_path, _path + ".precloud", overwrite: true);
            Data = d;
            Save();
        }

        /// <summary>Bölüm tamamlandı. Tekrar çağrılması ek etki üretmez.</summary>
        public bool MarkCompleted(string levelId, int index, int levelCount)
        {
            bool changed = false;
            if (!Data.completed.Contains(levelId)) { Data.completed.Add(levelId); changed = true; }
            int unlock = Math.Min(index + 1, Math.Max(0, levelCount - 1));
            if (unlock > Data.highestUnlocked) { Data.highestUnlocked = unlock; changed = true; }
            if (changed) Save();
            return changed;
        }

        /// <summary>
        /// v5/v9: bölümün en iyi yıldızını (1–3) yazar; yalnızca artışta kaydeder. Toplam yıldızdaki artışı döndürür
        /// (aynı bölümü 3, 1, 3 yıldızla kazanmak toplamı yalnız 3 artırır; 1 → 3 artışı 2'dir).
        /// </summary>
        public int RecordStars(string levelId, int stars)
        {
            int delta = Meta.ProgressRules.RecordBest(Data.starIds, Data.starCounts, levelId, stars);
            if (delta > 0) Save();
            return delta;
        }

        /// <summary>v9: bölümün ilk kez açıldığını kaydeder; daha önce açılmamışsa true (ilk deneme).</summary>
        public bool MarkAttempted(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || Data.meta.attemptedLevels.Contains(levelId)) return false;
            Data.meta.attemptedLevels.Add(levelId);
            Save();
            return true;
        }
    }
}
