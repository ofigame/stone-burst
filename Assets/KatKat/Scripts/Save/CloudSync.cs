using System;
using UnityEngine;

namespace KatKat.Save
{
    /// <summary>
    /// Bulut kaydı kuralları (kullanıcı, 2026-11-09: "ilerleme kaydediliyor mu" → otomatik bulut kaydı ve geri yükleme).
    /// Saf mantık; ağ işi <see cref="KatKat.Game.IAccountService"/>'te.
    /// - Girişten sonra önce buluttaki kayıt okunur. Bulut yereldekinden ilerideyse (daha çok tamamlanmış bölüm; eşitse
    ///   daha çok yıldız) yerel kayıt buluttakiyle değiştirilir; ses ve titreşim ayarı cihazda kalır.
    /// - Bu kontrol bitmeden buluta hiç yazılmaz: yeni kurulumdaki boş kayıt buluttaki ilerlemeyi ezmesin.
    /// </summary>
    public static class CloudSync
    {
        /// <summary>İki otomatik yükleme arasında en az bu kadar saniye (uygulama arka plana geçince beklemeden).</summary>
        public const float MinUploadGap = 30f;

        /// <summary>İlerleme ölçüsü: önce tamamlanan bölüm sayısı, sonra bölüm yıldızı, sonra açılan en yüksek bölüm.</summary>
        public static long Progress(SaveData d)
        {
            if (d == null) return -1;
            long done = d.completed?.Count ?? 0;
            long stars = Math.Max(0, d.LevelStars);
            long unlocked = Math.Max(0, d.highestUnlocked);
            return done * 100_000_000L + Math.Min(stars, 99_999L) * 1_000L + Math.Min(unlocked, 999L);
        }

        /// <summary>Buluttaki JSON'u okur; boş, bozuk ya da bu sürümden yeni kayıtta null.</summary>
        public static SaveData Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d == null || d.saveVersion > SaveData.CurrentVersion) return null;
                d.completed ??= new System.Collections.Generic.List<string>();
                if (d.highestUnlocked < 0) d.highestUnlocked = 0;
                SaveService.Upgrade(d);
                return d;
            }
            catch (ArgumentException) { return null; }
        }

        /// <summary>Buluttaki kayıt yereldekinden ilerideyse true (yalnız o zaman geri yüklenir).</summary>
        public static bool CloudIsAhead(SaveData local, SaveData cloud) => cloud != null && Progress(cloud) > Progress(local);
    }
}
