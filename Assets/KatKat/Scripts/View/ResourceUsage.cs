using System;
using System.Collections.Generic;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Çalışma anında Resources'tan istenen yolların kaydı (varlık temizliği için: yalnızca editör yakalamalarında
    /// okunur, oyunda maliyeti bir HashSet eklemesi). "Klasör/ad" biçiminde, uzantısız. Editörde KATKAT_RES_LOG ortam
    /// değişkeni bir dosya yolu ise her yeni yol o dosyaya eklenir (test ve yakalama koşularının ortak kaydı).
    /// </summary>
    public static class ResourceUsage
    {
        public static readonly HashSet<string> Paths = new HashSet<string>();
        private static string _log;
        private static bool _logChecked;

        public static void Note(string path)
        {
            if (string.IsNullOrEmpty(path) || !Paths.Add(path)) return;
#if UNITY_EDITOR
            if (!_logChecked) { _logChecked = true; _log = Environment.GetEnvironmentVariable("KATKAT_RES_LOG"); }
            if (!string.IsNullOrEmpty(_log)) { try { System.IO.File.AppendAllText(_log, path + "\n"); } catch { } }
#endif
        }
    }

    /// <summary>Resources yüklemesi + kullanım kaydı (bütün oyun kodu bunu kullanır).</summary>
    public static class Res
    {
        public static T Load<T>(string path) where T : UnityEngine.Object
        {
            ResourceUsage.Note(path);
            return Resources.Load<T>(path);
        }

        public static T[] LoadAll<T>(string path) where T : UnityEngine.Object
        {
            ResourceUsage.Note(path + "/*");
            return Resources.LoadAll<T>(path);
        }
    }
}
