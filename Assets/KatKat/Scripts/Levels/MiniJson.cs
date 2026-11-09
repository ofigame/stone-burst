using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KatKat.Levels
{
    /// <summary>
    /// Küçük, bağımlılıksız JSON okuyucu. Nesne → Dictionary&lt;string, object&gt;, dizi → List&lt;object&gt;,
    /// sayı → double, metin → string, true/false → bool, null → null.
    /// UnityEngine.JsonUtility kullanılmaz; böylece seviye kodu motor dışında da test edilir.
    /// </summary>
    public sealed class MiniJson
    {
        private readonly string _s;
        private int _i;

        private MiniJson(string s) { _s = s; }

        public static object Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var p = new MiniJson(text);
            p.SkipWs();
            var v = p.Value();
            p.SkipWs();
            if (p._i != p._s.Length) throw p.Error("Fazla karakter");
            return v;
        }

        private FormatException Error(string msg)
        {
            int line = 1;
            for (int k = 0; k < _i && k < _s.Length; k++) if (_s[k] == '\n') line++;
            return new FormatException($"JSON hatası (satır {line}): {msg}");
        }

        private void SkipWs()
        {
            while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
        }

        private object Value()
        {
            if (_i >= _s.Length) throw Error("Beklenmeyen son");
            char c = _s[_i];
            switch (c)
            {
                case '{': return Obj();
                case '[': return Arr();
                case '"': return Str();
                case 't': Lit("true"); return true;
                case 'f': Lit("false"); return false;
                case 'n': Lit("null"); return null;
                default:
                    if (c == '-' || (c >= '0' && c <= '9')) return Num();
                    throw Error($"Beklenmeyen karakter '{c}'");
            }
        }

        private void Lit(string word)
        {
            if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw Error($"'{word}' bekleniyordu");
            _i += word.Length;
        }

        private Dictionary<string, object> Obj()
        {
            var d = new Dictionary<string, object>();
            _i++;
            SkipWs();
            if (_i < _s.Length && _s[_i] == '}') { _i++; return d; }
            while (true)
            {
                SkipWs();
                if (_i >= _s.Length || _s[_i] != '"') throw Error("Alan adı bekleniyordu");
                var key = Str();
                SkipWs();
                if (_i >= _s.Length || _s[_i] != ':') throw Error("':' bekleniyordu");
                _i++;
                SkipWs();
                if (d.ContainsKey(key)) throw Error($"Tekrarlanan alan '{key}'");
                d[key] = Value();
                SkipWs();
                if (_i >= _s.Length) throw Error("'}' bekleniyordu");
                if (_s[_i] == ',') { _i++; continue; }
                if (_s[_i] == '}') { _i++; return d; }
                throw Error("',' veya '}' bekleniyordu");
            }
        }

        private List<object> Arr()
        {
            var list = new List<object>();
            _i++;
            SkipWs();
            if (_i < _s.Length && _s[_i] == ']') { _i++; return list; }
            while (true)
            {
                SkipWs();
                list.Add(Value());
                SkipWs();
                if (_i >= _s.Length) throw Error("']' bekleniyordu");
                if (_s[_i] == ',') { _i++; continue; }
                if (_s[_i] == ']') { _i++; return list; }
                throw Error("',' veya ']' bekleniyordu");
            }
        }

        private string Str()
        {
            var sb = new StringBuilder();
            _i++;
            while (_i < _s.Length)
            {
                char c = _s[_i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (_i >= _s.Length) break;
                char e = _s[_i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (_i + 4 > _s.Length) throw Error("Eksik \\u kaçışı");
                        sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        _i += 4;
                        break;
                    default: throw Error($"Geçersiz kaçış \\{e}");
                }
            }
            throw Error("Kapanmamış metin");
        }

        private double Num()
        {
            int start = _i;
            if (_s[_i] == '-') _i++;
            while (_i < _s.Length && "0123456789.eE+-".IndexOf(_s[_i]) >= 0) _i++;
            var token = _s.Substring(start, _i - start);
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) throw Error($"Geçersiz sayı '{token}'");
            return v;
        }
    }
}
