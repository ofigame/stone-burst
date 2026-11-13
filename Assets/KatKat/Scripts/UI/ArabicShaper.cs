using System.Collections.Generic;
using System.Text;

namespace KatKat.UI
{
    /// <summary>
    /// Arapça metni, harfleri yalnızca yan yana dizen yazı çizicisi (TextMeshPro) için hazırlar:
    /// 1) Bağlam biçimi: her harf komşularına göre yalın / son / baş / orta biçimine çevrilir (Unicode Arapça Sunum
    ///    Biçimleri-B; çift bağlanan harflerde dört biçim taban koddan ardışık: +0 yalın, +1 son, +2 baş, +3 orta).
    ///    Lam + elif tek bağ harfine dönüşür.
    /// 2) Görsel sıra: satır sağdan sola çevrilir; Latin harf ve rakam öbekleri ("Stone Burst", "+30", "3/5", "%50")
    ///    kendi soldan sağa sıralarını korur, ayna karakterler (parantez vb.) çevrilir.
    /// Stone Burst için 2026-11-13'te sıfırdan yazıldı (eski sürümle bütün yerelleştirme metinlerinde aynı çıktı).
    /// </summary>
    public static class ArabicShaper
    {
        /// <summary>Harfin bağlanma türü: yalnız sağa (önceki harfe) ya da iki yöne.</summary>
        private enum Joining : byte { None, Right, Dual }

        private readonly struct Letter
        {
            public readonly char Isolated;
            public readonly Joining Kind;
            public Letter(int isolated, Joining kind) { Isolated = (char)isolated; Kind = kind; }
        }

        private const char Hamza = 'ء', Lam = 'ل', Tatweel = 'ـ';

        /// <summary>Temel harf → yalın biçimin kodu ve bağlanma türü.</summary>
        private static readonly Dictionary<char, Letter> Letters = BuildLetters();

        /// <summary>Lam'dan sonra gelen elif türleri → bağ harfinin yalın biçimi (son biçim +1).</summary>
        private static readonly Dictionary<char, char> LamAlefLigature = new Dictionary<char, char>
        {
            ['آ'] = 'ﻵ', // آ
            ['أ'] = 'ﻷ', // أ
            ['إ'] = 'ﻹ', // إ
            ['ا'] = 'ﻻ', // ا
        };

        private static Dictionary<char, Letter> BuildLetters()
        {
            const Joining R = Joining.Right, D = Joining.Dual;
            var t = new Dictionary<char, Letter>();
            void Add(int letter, int isolated, Joining kind) => t[(char)letter] = new Letter(isolated, kind);
            Add(0x0621, 0xFE80, Joining.None); // ء
            Add(0x0622, 0xFE81, R); Add(0x0623, 0xFE83, R); Add(0x0624, 0xFE85, R); Add(0x0625, 0xFE87, R);
            Add(0x0626, 0xFE89, D); Add(0x0627, 0xFE8D, R); Add(0x0628, 0xFE8F, D); Add(0x0629, 0xFE93, R);
            Add(0x062A, 0xFE95, D); Add(0x062B, 0xFE99, D); Add(0x062C, 0xFE9D, D); Add(0x062D, 0xFEA1, D);
            Add(0x062E, 0xFEA5, D); Add(0x062F, 0xFEA9, R); Add(0x0630, 0xFEAB, R); Add(0x0631, 0xFEAD, R);
            Add(0x0632, 0xFEAF, R); Add(0x0633, 0xFEB1, D); Add(0x0634, 0xFEB5, D); Add(0x0635, 0xFEB9, D);
            Add(0x0636, 0xFEBD, D); Add(0x0637, 0xFEC1, D); Add(0x0638, 0xFEC5, D); Add(0x0639, 0xFEC9, D);
            Add(0x063A, 0xFECD, D); Add(0x0641, 0xFED1, D); Add(0x0642, 0xFED5, D); Add(0x0643, 0xFED9, D);
            Add(0x0644, 0xFEDD, D); Add(0x0645, 0xFEE1, D); Add(0x0646, 0xFEE5, D); Add(0x0647, 0xFEE9, D);
            Add(0x0648, 0xFEED, R); Add(0x0649, 0xFEEF, R); Add(0x064A, 0xFEF1, D);
            return t;
        }

        private static bool InArabicBlock(char c) => c >= '؀' && c <= 'ۿ';

        public static bool HasArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++) if (InArabicBlock(text[i])) return true;
            return false;
        }

        /// <summary>Her satır ayrı biçimlenir ve çevrilir; Arapça harf içermeyen metin olduğu gibi döner.</summary>
        public static string ShapeLines(string text)
        {
            if (!HasArabic(text)) return text;
            var sb = new StringBuilder(text.Length);
            int start = 0;
            while (true)
            {
                int nl = text.IndexOf('\n', start);
                string line = nl < 0 ? text.Substring(start) : text.Substring(start, nl - start);
                sb.Append(ShapeLine(line));
                if (nl < 0) break;
                sb.Append('\n');
                start = nl + 1;
            }
            return sb.ToString();
        }

        /// <summary>Tek satır: önce bağlam biçimleri, sonra görsel sıra.</summary>
        public static string ShapeLine(string line) => HasArabic(line) ? Visual(Join(line)) : line;

        // ---- 1) Bağlam biçimleri --------------------------------------------------------------------------

        /// <summary>Hareke (fetha, kesra, şedde…): bağlanmayı bozmaz, komşu ararken atlanır.</summary>
        private static bool IsMark(char c) => (c >= 'ً' && c <= 'ٟ') || c == 'ٰ';

        /// <summary>Sonraki harfe bağlanabilir mi (çift bağlanan harf ya da uzatma çizgisi).</summary>
        private static bool LinksToNext(char c) => c == Tatweel || (Letters.TryGetValue(c, out var l) && l.Kind == Joining.Dual);

        /// <summary>Önceki harfe bağlanabilir mi (hemze dışındaki her harf ya da uzatma çizgisi).</summary>
        private static bool LinksToPrevious(char c) => c == Tatweel || (Letters.TryGetValue(c, out var l) && l.Kind != Joining.None);

        /// <summary>i'den <paramref name="dir"/> yönünde hareke olmayan ilk karakterin yeri; yoksa -1.</summary>
        private static int Skip(string s, int i, int dir)
        {
            int j = i + dir;
            while (j >= 0 && j < s.Length && IsMark(s[j])) j += dir;
            return j >= 0 && j < s.Length ? j : -1;
        }

        /// <summary>Bağlam biçimleri (mantıksal sıra korunur).</summary>
        public static string Join(string s)
        {
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (!Letters.TryGetValue(c, out var letter))
                {
                    sb.Append(c);
                    i++;
                    continue;
                }
                int p = Skip(s, i, -1), n = Skip(s, i, +1);
                bool joinPrev = p >= 0 && LinksToNext(s[p]) && LinksToPrevious(c);
                if (c == Lam && n >= 0 && LamAlefLigature.TryGetValue(s[n], out char lig))
                {
                    // Lam ile elif arasındaki harekeler bağ harfinde düşer; elif de tüketilir.
                    sb.Append(joinPrev ? (char)(lig + 1) : lig);
                    i = n + 1;
                    continue;
                }
                bool joinNext = letter.Kind == Joining.Dual && n >= 0 && LinksToPrevious(s[n]);
                int form = letter.Kind == Joining.None ? 0
                    : joinPrev && joinNext ? 3
                    : joinPrev ? 1
                    : joinNext ? 2
                    : 0;
                sb.Append((char)(letter.Isolated + form));
                i++;
            }
            return sb.ToString();
        }

        // ---- 2) Görsel sıra ---------------------------------------------------------------------------------

        /// <summary>Soldan sağa güçlü karakter: Arapça bloğundan önceki harf/rakamlar ve tam genişlikli Latin.</summary>
        private static bool IsLatinStrong(char c) => (c < '؀' && char.IsLetterOrDigit(c)) || (c >= '０' && c <= 'ｚ');

        /// <summary>Sağdan sola güçlü karakter: Arapça bloğu ve sunum biçimleri.</summary>
        private static bool IsArabicStrong(char c) => InArabicBlock(c) || c >= 'ﭐ';

        /// <summary>Rakamdan hemen önceki artı/eksi rakamla birlikte soldan sağa öbeğe girer.</summary>
        private static bool IsSignedDigit(string s, int i) => (s[i] == '+' || s[i] == '-') && i + 1 < s.Length && char.IsDigit(s[i + 1]);

        private static bool OpensLatin(string s, int i) => IsLatinStrong(s[i]) || IsSignedDigit(s, i);

        private static char Mirror(char c)
        {
            const string pairs = "()[]<>«»";
            int k = pairs.IndexOf(c);
            return k < 0 ? c : pairs[k ^ 1];
        }

        /// <summary>
        /// Sağdan sola satırın görsel sırası. Soldan sağa öbek bir Latin harf/rakamla (ya da işaretli rakamla) başlar,
        /// Arapça harfe kadar uzanır ama son güçlü karakterde biter; hemen ardındaki "%" öbeğe katılır. Aradaki nötrler
        /// ("Stone Burst", "3/5", "12.5") öbekte kalır; öbek dışındaki nötrler sağdan sola akar.
        /// </summary>
        public static string Visual(string s)
        {
            // Öbekler sırayla bulunur, sonra ters sırada yazılır: soldan sağa öbek olduğu gibi, diğerleri tersten ve aynalı.
            var spans = new List<(int start, int length, bool latin)>();
            int i = 0;
            while (i < s.Length)
            {
                if (OpensLatin(s, i))
                {
                    int last = i;
                    for (int j = i + 1; j < s.Length && !IsArabicStrong(s[j]); j++)
                        if (IsLatinStrong(s[j])) last = j;
                    int end = last + 1;
                    if (end < s.Length && s[end] == '%') end++;
                    spans.Add((i, end - i, true));
                    i = end;
                }
                else
                {
                    int j = i + 1;
                    while (j < s.Length && !OpensLatin(s, j)) j++;
                    spans.Add((i, j - i, false));
                    i = j;
                }
            }
            var sb = new StringBuilder(s.Length);
            for (int k = spans.Count - 1; k >= 0; k--)
            {
                var (start, length, latin) = spans[k];
                if (latin) sb.Append(s, start, length);
                else for (int j = start + length - 1; j >= start; j--) sb.Append(Mirror(s[j]));
            }
            return sb.ToString();
        }
    }
}
