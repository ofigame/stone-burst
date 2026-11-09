using System.Collections.Generic;

namespace KatKat.View
{
    /// <summary>
    /// Çalışma anında Resources'tan istenen yolların kaydı (varlık temizliği için: yalnızca editör yakalamalarında
    /// okunur, oyunda maliyeti bir HashSet eklemesi). "Klasör/ad" biçiminde, uzantısız.
    /// </summary>
    public static class ResourceUsage
    {
        public static readonly HashSet<string> Paths = new HashSet<string>();
        public static void Note(string path)
        {
            if (!string.IsNullOrEmpty(path)) Paths.Add(path);
        }
    }
}
