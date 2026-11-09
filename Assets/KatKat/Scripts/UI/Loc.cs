using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace KatKat.UI
{
    /// <summary>Bir dil: kod, kendi dilindeki adı, kültür, yazı yönü.</summary>
    public sealed class GameLanguage
    {
        public string Code, NativeName, Culture;
        public bool RightToLeft;
    }

    /// <summary>
    /// Oyunun metinleri dokuz dilde (Crate Boss ile aynı dil seti: tr, en, es, ar, de, it, fr, ja, ko).
    /// Tablo: Resources/Localization/strings.txt (sekmeyle ayrılmış; ilk satır "key" + dil kodları; "#" yorum; "\n" satır sonu).
    /// Kaynak Türkçe, yedek İngilizce. İlk açılışta cihaz dili (desteklenmiyorsa İngilizce), sonra oyuncunun seçimi.
    /// Sayılar her dilde Batı rakamı.
    /// </summary>
    public static class Loc
    {
        public const string Source = "tr", Fallback = "en";
        private const string PrefKey = "katkat.lang";

        public static readonly GameLanguage[] Languages =
        {
            new GameLanguage { Code = "tr", NativeName = "Türkçe", Culture = "tr-TR" },
            new GameLanguage { Code = "en", NativeName = "English", Culture = "en-US" },
            new GameLanguage { Code = "es", NativeName = "Español", Culture = "es-ES" },
            new GameLanguage { Code = "ar", NativeName = "العربية", Culture = "ar-SA", RightToLeft = true },
            new GameLanguage { Code = "de", NativeName = "Deutsch", Culture = "de-DE" },
            new GameLanguage { Code = "it", NativeName = "Italiano", Culture = "it-IT" },
            new GameLanguage { Code = "fr", NativeName = "Français", Culture = "fr-FR" },
            new GameLanguage { Code = "ja", NativeName = "日本語", Culture = "ja-JP" },
            new GameLanguage { Code = "ko", NativeName = "한국어", Culture = "ko-KR" },
        };

        private static Dictionary<string, string[]> _rows;
        private static string[] _columns;
        private static GameLanguage _current;
        private static CultureInfo _culture;

        public static event Action Changed;

        public static GameLanguage Current
        {
            get
            {
                if (_current == null) Set(Initial(), false);
                return _current;
            }
        }

        public static string Code => Current.Code;
        public static bool IsRtl => Current.RightToLeft;
        public static CultureInfo Culture => _culture ??= MakeCulture(Current.Culture);

        public static GameLanguage Find(string code)
        {
            foreach (var l in Languages) if (l.Code == code) return l;
            return null;
        }

        public static void Set(string code, bool save = true)
        {
            var language = Find(code) ?? Find(Fallback);
            bool changed = _current != null && _current != language;
            _current = language;
            _culture = null;
            if (save) { PlayerPrefs.SetString(PrefKey, language.Code); PlayerPrefs.Save(); }
            if (changed) Changed?.Invoke();
        }

        /// <summary>Anahtarın metni: geçerli dil, yoksa İngilizce, yoksa Türkçe, yoksa <paramref name="fallback"/> (ya da anahtar).</summary>
        public static string T(string key, string fallback = null)
        {
            Load();
            if (key != null && _rows.TryGetValue(key, out var row))
                return Cell(row, Code) ?? Cell(row, Fallback) ?? Cell(row, Source) ?? fallback ?? key;
            return fallback ?? key;
        }

        public static bool Has(string key) { Load(); return key != null && _rows.ContainsKey(key); }

        public static string F(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, T(key), args);

        /// <summary>Geçerli dilin büyük harf kuralı (Türkçe i → İ).</summary>
        public static string Upper(string s) => string.IsNullOrEmpty(s) ? s : s.ToUpper(Culture);

        /// <summary>Tablodaki bütün diller ve anahtarlar (test için).</summary>
        public static IEnumerable<KeyValuePair<string, string[]>> Rows { get { Load(); return _rows; } }
        public static string[] Columns { get { Load(); return _columns; } }

        private static string Cell(string[] row, string lang)
        {
            for (int i = 0; i < _columns.Length; i++)
                if (_columns[i] == lang) return i < row.Length && !string.IsNullOrEmpty(row[i]) ? row[i] : null;
            return null;
        }

        private static void Load()
        {
            if (_rows != null) return;
            _rows = new Dictionary<string, string[]>();
            _columns = new string[0];
            var asset = Resources.Load<TextAsset>("Localization/strings");
            if (asset == null) return;
            foreach (var raw in asset.text.Replace("\r\n", "\n").Split('\n'))
            {
                if (raw.Length == 0 || raw[0] == '#') continue;
                var cells = raw.Split('\t');
                if (_columns.Length == 0) { _columns = new string[cells.Length - 1]; Array.Copy(cells, 1, _columns, 0, _columns.Length); continue; }
                var values = new string[_columns.Length];
                for (int i = 0; i < values.Length; i++) values[i] = i + 1 < cells.Length ? cells[i + 1].Replace("\\n", "\n") : "";
                _rows[cells[0]] = values;
            }
        }

        private static string Initial()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-lang");
            if (i >= 0 && i < args.Length - 1 && Find(args[i + 1]) != null) return args[i + 1];
            string saved = PlayerPrefs.GetString(PrefKey, "");
            if (Find(saved) != null) return saved;
            return FromSystem(Application.systemLanguage);
        }

        public static string FromSystem(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Turkish: return "tr";
                case SystemLanguage.Spanish: return "es";
                case SystemLanguage.Arabic: return "ar";
                case SystemLanguage.German: return "de";
                case SystemLanguage.Italian: return "it";
                case SystemLanguage.French: return "fr";
                case SystemLanguage.Japanese: return "ja";
                case SystemLanguage.Korean: return "ko";
                default: return Fallback;
            }
        }

        private static CultureInfo MakeCulture(string name)
        {
            try { return new CultureInfo(name); }
            catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
        }

        /// <summary>
        /// Metni etikete yazar. Arapçada harfler birleştirilir ve satırlar sağdan sola görsel sıraya çevrilir; kaydırmalı
        /// etikette satır sonları önce mantıksal metinde bulunur (LocText genişlik değişince yeniden dizer).
        /// </summary>
        public static void Set(TextMeshProUGUI t, string text)
        {
            if (t == null) return;
            if (!IsRtl || !ArabicShaper.HasArabic(text))
            {
                var lt0 = t.GetComponent<LocText>();
                if (lt0 != null) lt0.Logical = null;
                t.text = text;
                return;
            }
            var lt = t.GetComponent<LocText>() ?? t.gameObject.AddComponent<LocText>();
            // Zengin metin etiketleri (<color=…>, <b>) sağdan sola çevirmede ters çevrilip bozuluyordu ("colo>" kalıntısı,
            // 2026-11-09 dil denetimi). Arapçada etiketler atılır; yazı aynı, yalnız vurgu rengi kalkar.
            if (text.IndexOf('<') >= 0) text = System.Text.RegularExpressions.Regex.Replace(text, "<[^<>]+>", "");
            lt.Logical = text;
            lt.Apply();
        }
    }

    /// <summary>Sağdan sola (Arapça) etiket: mantıksal metni tutar, genişliğe göre satırlara böler, her satırı şekillendirir.</summary>
    [DisallowMultipleComponent, ExecuteAlways]
    public sealed class LocText : MonoBehaviour
    {
        public string Logical;
        private float _laidOut = -1f;
        private bool _wrapKnown;
        private TextWrappingModes _origWrap;
        private TextMeshProUGUI _t;

        private TextWrappingModes _appliedWrap;
        private TextOverflowModes _appliedOverflow;

        /// <summary>
        /// Çağıran Loc.Set'ten SONRA satır kaydırma/taşma ayarını değiştirirse (ör. NoWrap + Ellipsis) yeniden dizer.
        /// 2026-11-09 dil denetimi: deneme listesi başlıkları ve sıralama sekmeleri Arapçada bu yüzden boş görünüyordu.
        /// </summary>
        public void Refresh()
        {
            if (Logical == null) return;
            if (_t == null) _t = GetComponent<TextMeshProUGUI>();
            if (_t == null) return;
            if (_t.textWrappingMode != _appliedWrap || _t.overflowMode != _appliedOverflow)
            {
                _origWrap = _t.textWrappingMode;
                _wrapKnown = true;
                Apply();
            }
        }

        // Çizimden hemen önce bütün etkin Arapça etiketler denetlenir (kare güncellemesi beklenmez; yakalama aracı da kapsanır).
        private static readonly HashSet<LocText> Active = new HashSet<LocText>();
        private static bool _hooked;

        private static void BeforeRender()
        {
            if (Active.Count == 0) return;
            foreach (var lt in new List<LocText>(Active)) if (lt != null) lt.Refresh();
        }

        private void OnEnable()
        {
            Active.Add(this);
            if (!_hooked) { Canvas.willRenderCanvases += BeforeRender; _hooked = true; }
            Refresh();
        }

        private void OnDisable() => Active.Remove(this);

        public void Apply()
        {
            if (Logical == null) return;
            if (_t == null) _t = GetComponent<TextMeshProUGUI>();
            if (!_wrapKnown) { _origWrap = _t.textWrappingMode; _wrapKnown = true; }
            try { ApplyCore(); }
            finally { _appliedWrap = _t.textWrappingMode; _appliedOverflow = _t.overflowMode; }
        }

        private void ApplyCore()
        {
            // Arapça yedek yazı tipinin satır yüksekliği Nunito'dan büyük: TMP "Ellipsis" satırı kutuya dikey sığmıyor sayıp
            // tamamen gizliyordu (görünen karakter 0; sıralama sekmeleri ve deneme başlıkları boştu). Arapçada taşma serbest,
            // genişliği otomatik küçültme karşılar.
            if (_t.overflowMode == TextOverflowModes.Ellipsis) _t.overflowMode = TextOverflowModes.Overflow;
            _t.textWrappingMode = _origWrap;
            _t.alignment = Mirror(_t.alignment);
            float width = _t.rectTransform.rect.width;
            _laidOut = width;
            var joined = ArabicShaper.Join(Logical);
            // Boyutlanmamış (varsayılan 100 birim) ya da tek satıra sığan metin bölünmez (sonra boyutlanınca yeniden dizilir).
            _t.text = ArabicShaper.ShapeLines(Logical);
            float size = _t.enableAutoSizing ? _t.fontSizeMax : _t.fontSize;
            bool fits = _t.GetPreferredValues(_t.text, float.PositiveInfinity, float.PositiveInfinity).x * (size / Mathf.Max(1f, _t.enableAutoSizing ? _t.fontSizeMax : _t.fontSize)) <= width + 1f;
            if (_t.textWrappingMode == TextWrappingModes.NoWrap || width <= 120f || fits || Logical.IndexOf('\n') >= 0)
            {
                if (fits && width > 120f) _t.textWrappingMode = TextWrappingModes.NoWrap; // tek satır: TMP sarmak yerine küçültsün
                _t.text = ArabicShaper.ShapeLines(Logical);
                return;
            }
            // Satır sonlarını soldan sağa (birleşik harflerle, aynı genişlik) TMP'ye buldurur.
            _t.text = joined;
            _t.ForceMeshUpdate(true, true);
            var info = _t.textInfo;
            var lines = new List<string>();
            for (int i = 0; i < info.lineCount; i++)
            {
                var li = info.lineInfo[i];
                int a = li.firstCharacterIndex, b = li.lastCharacterIndex;
                if (a < 0 || b < a || a >= joined.Length) continue;
                b = Mathf.Min(b, joined.Length - 1);
                lines.Add(joined.Substring(a, b - a + 1).Trim());
            }
            if (lines.Count == 0) lines.Add(joined);
            for (int i = 0; i < lines.Count; i++) lines[i] = ArabicShaper.Visual(lines[i]);
            _t.text = string.Join("\n", lines);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (Logical == null || !isActiveAndEnabled) return;
            if (_t == null) _t = GetComponent<TextMeshProUGUI>();
            if (_t != null && Mathf.Abs(_t.rectTransform.rect.width - _laidOut) > 0.5f) Apply();
        }

        private static TextAlignmentOptions Mirror(TextAlignmentOptions a)
        {
            switch (a)
            {
                case TextAlignmentOptions.TopLeft: return TextAlignmentOptions.TopRight;
                case TextAlignmentOptions.MidlineLeft: return TextAlignmentOptions.MidlineRight;
                case TextAlignmentOptions.Left: return TextAlignmentOptions.Right;
                case TextAlignmentOptions.BottomLeft: return TextAlignmentOptions.BottomRight;
                default: return a;
            }
        }
    }
}
