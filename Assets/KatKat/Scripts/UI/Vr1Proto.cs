using UnityEngine;

namespace KatKat.UI
{
    /// <summary>
    /// vr1 P1 prototip anahtarı (docs/vr1/claude_code_paketi_v1/CLAUDE_CODE_BASLANGIC.md: "Prototipte kullan ve görsel
    /// kabulden sonra sonlandır"; "mevcut görselleri sessizce değiştirme"). Kapalıyken oyun 2026-11-11 öncesi görünümdedir.
    /// Açılış: geliştirici menüsü (KatKat/vr1 prototipi) ya da <see cref="Override"/> (testler, yakalama aracı).
    /// Kapsam: buzlu koleksiyon taşı, haritaya dönüş ödül akışı, Harita/Günlük ikon adayları, prototip sesleri.
    /// </summary>
    public static class Vr1Proto
    {
        public const string PrefKey = "katkat.vr1proto";

        /// <summary>null: kayıtlı tercih (varsayılan kapalı); true/false: zorla.</summary>
        public static bool? Override;

        public static bool On => Override ?? PlayerPrefs.GetInt(PrefKey, 0) == 1;

        /// <summary>Prototip sesi çal (GameController bağlar; ses kapalıysa sessiz).</summary>
        public static System.Action<string> Sound;

        public static void Play(string clip) { if (On) Sound?.Invoke(clip); }

        private static Sprite _navMap, _navDaily, _iceRim;
        public static Sprite NavMap => _navMap ? _navMap : _navMap = Resources.Load<Sprite>("Vr1/nav-map");
        public static Sprite NavDaily => _navDaily ? _navDaily : _navDaily = Resources.Load<Sprite>("Vr1/nav-daily");
        public static Sprite IceRim => _iceRim ? _iceRim : _iceRim = Resources.Load<Sprite>("Vr1/ice-rim");
    }
}
