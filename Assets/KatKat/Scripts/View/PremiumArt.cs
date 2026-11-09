using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Premium-v2 paketinden kesilen sprite'lar (Resources/Premium, tools/ArtSlicer üretir). Bulunamayan
    /// sprite null döner; çağıran koddan üretilen eski görünüme düşer, böylece oyun varlıksız da çalışır.
    /// </summary>
    public static class PremiumArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name) => Load("Premium/" + name);

        /// <summary>v4 bağımsız varlık: "klasör/ad" (ör. "icons/roll"). Bulunamazsa null + uyarı.</summary>
        public static Sprite V4(string key) => Load("PremiumV4/" + key);

        private static Sprite Load(string path)
        {
            ResourceUsage.Note(path);
            if (Cache.TryGetValue(path, out var s)) return s;
            s = Resources.Load<Sprite>(path);
            if (s == null) Debug.LogWarning("[KatKat] Premium sprite yok: " + path);
            Cache[path] = s;
            return s;
        }

        public static Sprite Frame => Get("frame");
        public static Sprite Panel => Get("panel");

        /// <summary>Hedef/HUD için renk taşı (paketteki 10-icons-and-goals sayfası).</summary>
        public static Sprite Chip(LayerColor c)
        {
            return UiStoneArt.Chip(c);
        }

        /// <summary>Oyun arka planı: bölgenin teması (v3 şartname); v4 yoksa eski gündüz/akşam dönüşümü.</summary>
        public static Sprite Background(int levelIndex) =>
            ThemeDefinition.ForLevel(levelIndex).GameSprite ?? Get((levelIndex / 20) % 2 == 0 ? "bg_day" : "bg_evening");
    }
}
