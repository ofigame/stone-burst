using System;
using System.Collections.Generic;

namespace KatKat.Meta
{
    /// <summary>Sıradaki yıldız taşı hedefi: önceki eşik (ya da 0) → bu eşik.</summary>
    public struct StarGoal
    {
        public bool Has;
        public string StoneId;
        public int Stars, From, To;
    }

    /// <summary>Haritaya dönüş özeti (03_KOLEKSIYON_ODUL WinReceipt'in birikmiş hali): kayıt gerçeklerinden üretilir.</summary>
    public struct MapReturn
    {
        /// <summary>Net yeni yıldız (aynı yıldız tekrar sayılmaz).</summary>
        public int NetStars;
        /// <summary>Eşsiz ilk tamamlama sayısı.</summary>
        public int NewLevels;
        public int StarsBefore, StarsAfter;
        /// <summary>Galibiyet var ama yeni yıldız/bölüm yok ("Bölümü tekrar tamamladın").</summary>
        public bool ReplayOnly;
        public bool HasChange => NetStars > 0 || NewLevels > 0;
    }

    /// <summary>
    /// vr1 P1 (docs/vr1/claude_code_paketi_v1/belgeler/03_KOLEKSIYON_ODUL.md): buzlu koleksiyon taşı ilerlemesi ve haritaya
    /// dönüş özeti. Saf hesap: kimlikler ve kazanım koşulları <see cref="StoneCatalog"/>'dan; eşik sayıları kodda yazılmaz.
    /// Kazanımı yalnız <see cref="CollectionRules"/> verir; buradaki ilerleme yalnız gösterimdir.
    /// </summary>
    public static class CollectionProgress
    {
        /// <summary>Buz evresi eşikleri (prototip görsel eşikleri, 03 "Buz evreleri"): 0–3; 4 yalnız kazanım sonrası.</summary>
        public const float Stage1 = 0.25f, Stage2 = 0.50f, Stage3 = 0.75f;
        public const int Unlocked = 4;

        /// <summary>
        /// Katalog sırasındaki (eşiğe göre) kazanılmamış ilk yıldız taşı. <paramref name="ignoreOwned"/> içindeki taşlar
        /// sahip olunmamış sayılır (haritaya dönüşte açılış öncesi görünümü kurmak için).
        /// </summary>
        public static StarGoal NextStarGoal(StoneCatalog catalog, ICollection<string> owned, int stars, ICollection<string> ignoreOwned = null)
        {
            var g = new StarGoal { Stars = stars };
            if (catalog == null) return g;
            var list = catalog.Stones.FindAll(s => s.Category == StoneCategory.StarStone && s.UnlockType == StoneUnlock.StarsTotal);
            list.Sort((a, b) => a.UnlockValue.CompareTo(b.UnlockValue));
            int from = 0;
            foreach (var s in list)
            {
                bool have = owned != null && owned.Contains(s.StoneId) && (ignoreOwned == null || !ignoreOwned.Contains(s.StoneId));
                bool reached = stars >= s.UnlockValue && (ignoreOwned == null || !ignoreOwned.Contains(s.StoneId));
                if (have || reached) { from = s.UnlockValue; continue; }
                g.Has = true;
                g.StoneId = s.StoneId;
                g.From = from;
                g.To = s.UnlockValue;
                return g;
            }
            return g;
        }

        /// <summary>İlerleme p = clamp((yıldız − önceki eşik) / (hedef − önceki eşik), 0, 1). Kazanılmadıkça 1'e ulaşmaz.</summary>
        public static float Progress(StarGoal g, bool unlocked = false)
        {
            if (unlocked) return 1f;
            if (!g.Has) return 0f;
            float p = g.To > g.From ? (g.Stars - g.From) / (float)(g.To - g.From) : 0f;
            if (p < 0f) p = 0f;
            return p >= 1f ? 0.999f : p;
        }

        /// <summary>Buz evresi: 0 yoğun kırağı, 1 ince çatlak, 2 saydam pencere, 3 ince kabuk, 4 açıldı (yalnız kazanımla).</summary>
        public static int IceStage(float p, bool unlocked = false)
        {
            if (unlocked) return Unlocked;
            return p < Stage1 ? 0 : p < Stage2 ? 1 : p < Stage3 ? 2 : 3;
        }

        /// <summary>
        /// Bu denemenin en çok net yeni yıldızı (önceki en iyi 2, en çok 3 → 1; ipucu bedeli varsa 1 eksik) eşiği geçirebilir mi?
        /// </summary>
        public static bool CanUnlockThisAttempt(StarGoal g, int previousBest, int maxStars = 3, int penalty = 0)
        {
            if (!g.Has) return false;
            int best = Math.Max(1, maxStars - Math.Max(0, penalty));
            int net = Math.Max(0, best - Math.Max(0, previousBest));
            return net > 0 && g.Stars + net >= g.To;
        }

        /// <summary>Haritanın son gördüğü değerlerle bugünkü kayıt arasındaki fark (eski kayıtta -1: fark yok).</summary>
        public static MapReturn Compare(int seenStars, int seenLevels, int stars, int levels, int winsSinceMap)
        {
            var r = new MapReturn { StarsBefore = stars, StarsAfter = stars };
            if (seenStars < 0 || seenLevels < 0) return r;
            r.StarsBefore = Math.Min(seenStars, stars);
            r.NetStars = Math.Max(0, stars - seenStars);
            r.NewLevels = Math.Max(0, levels - seenLevels);
            r.ReplayOnly = !r.HasChange && winsSinceMap > 0;
            return r;
        }
    }
}
