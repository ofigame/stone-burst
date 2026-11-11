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

        /// <summary>null: kayıtlı tercih (varsayılan açık); true/false: zorla.</summary>
        public static bool? Override;

        private static bool? _pref;

        /// <summary>Animasyon kodu her karede okur: tercih bir kez okunup bellekte tutulur.</summary>
        /// <summary>Varsayılan açık: kullanıcı cihazda inceleyip onayladı (2026-11-11: "test ettim sorun yok gayet güzel.
        /// Tüm oyuna uygulayabiliriz" → "Prototipi canlıya aç"). Kapatma yalnız deneme derlemesindeki Ayarlar düğmesi ve editör menüsüyle.</summary>
        public const int DefaultOn = 1;

        /// <summary>Pilot bölümler (12 ayrı deneme bölümü) yalnız deneme derlemesinde ve editörde; mağaza derlemesinde gizli.</summary>
#if KATKAT_VR1PROTO || UNITY_EDITOR
        public const bool PilotAllowed = true;
#else
        public const bool PilotAllowed = false;
#endif

        public static bool On => Override ?? (_pref ??= PlayerPrefs.GetInt(PrefKey, DefaultOn) == 1);

        /// <summary>Kayıtlı tercihi değiştirir (geliştirici menüsü).</summary>
        public static void SetPref(bool on)
        {
            PlayerPrefs.SetInt(PrefKey, on ? 1 : 0);
            PlayerPrefs.Save();
            _pref = on;
        }

        /// <summary>Prototip sesi çal (GameController bağlar; ses kapalıysa sessiz).</summary>
        public static System.Action<string> Sound;

        public static void Play(string clip) { if (On) Sound?.Invoke(clip); }

        private static Sprite _navMap, _navDaily, _iceRim;
        public static Sprite NavMap => _navMap ? _navMap : _navMap = Resources.Load<Sprite>("Vr1/nav-map");
        public static Sprite NavDaily => _navDaily ? _navDaily : _navDaily = Resources.Load<Sprite>("Vr1/nav-daily");
        /// <summary>Taşlar sekmesi adayı (04: "Taşlar: mevcut gerçek koleksiyon asset'i"); GameController katalogdan atar.</summary>
        public static Sprite NavStones;
        public static Sprite IceRim => _iceRim ? _iceRim : _iceRim = Resources.Load<Sprite>("Vr1/ice-rim");
    }
}
