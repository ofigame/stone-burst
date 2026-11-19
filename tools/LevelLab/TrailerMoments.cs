using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Levels;

// moments [adet]: tanıtım videosu için gösterişli anlar (2026-11-12, Arapça tanıtım). Her bölümün kayıtlı çözümü
// yeniden oynanır; adım başına soyulan taş, doğan özel taş ve kullanılan özel taş (Roket, Altın Rulo) puanlanır.
// Çıktı: en iyi adımlar (bölüm sırası 0 tabanlı, adım sırası) ve bölümün tamamı için toplam gösteriş puanı.
// 2026-11-19 (Meta reklamı): dosyalar sayısal sırayla (eskiden metin sırası: level_1000, level_100 ile level_101
// arasına girdiği için 100+ sıraları oyundaki dizinden bir kayıktı); Altın Rulo ve Roket anları ayrı listelenir;
// her satırda bölümün engelleri (kaplama türleri) yazılır.
static partial class Program
{
    static int LevelNum(string path)
    {
        var n = Path.GetFileNameWithoutExtension(path);
        return int.TryParse(n.Substring(n.LastIndexOf('_') + 1), out int v) ? v : int.MaxValue;
    }

    static int TrailerMoments(string[] args)
    {
        int top = args.Length > 1 ? int.Parse(args[1]) : 30;
        var files = Directory.GetFiles(LevelsDir, "level_*.json").OrderBy(LevelNum).ThenBy(f => f, StringComparer.Ordinal).ToList();
        var steps = new List<(int lvl, int step, int score, string desc, int stepsTotal, SpecialKind used, int peeled, string tags)>();
        var levelScore = new List<(int lvl, int score, int steps, int w, int h, string tags)>();
        for (int i = 0; i < files.Count; i++)
        {
            var txt = File.ReadAllText(files[i]);
            var d = LevelParser.Parse(txt, Path.GetFileName(files[i]));
            var s = LevelLoader.ToState(d);
            // Engeller: JSON'daki kaplama türleri (clip oyunda sarmaşık olarak çizilir).
            var kinds = new SortedSet<string>();
            foreach (var part in txt.Split(new[] { "\"kind\": \"" }, StringSplitOptions.None).Skip(1))
            {
                var k2 = part.Substring(0, part.IndexOf('"'));
                if (k2 != "none") kinds.Add(k2 == "clip" ? "sarmaşık" : k2);
            }
            string tags = $"{s.Board.Width}x{s.Board.Height} hamle={d.Solution.Count} [{string.Join(",", kinds)}]";
            int total = 0, k = 0;
            foreach (var step in d.Solution)
            {
                var r = RulesEngine.TryApply(s, LevelLoader.ToCommand(step));
                if (!r.Accepted) break;
                int peeled = r.TotalPeeled;
                int sc = peeled + (r.SpecialCreated == SpecialKind.None ? 0 : 15)
                         + (r.UsedSpecial != SpecialKind.None ? peeled / 2 + 10 : 0);
                string desc = $"{step.Type} soyulan={peeled} dalga={r.Waves.Count} doğan={r.SpecialCreated} kullanılan={r.UsedSpecial} durum={r.StatusAfter}";
                steps.Add((i, k, sc, desc, d.Solution.Count, r.UsedSpecial, peeled, tags));
                total += sc; k++;
            }
            levelScore.Add((i, total / Math.Max(1, d.Solution.Count), d.Solution.Count, s.Board.Width, s.Board.Height, tags));
        }
        string Id(int lvl) => Path.GetFileNameWithoutExtension(files[lvl]);
        Console.WriteLine("# En gösterişli adımlar");
        foreach (var x in steps.OrderByDescending(x => x.score).Take(top))
            Console.WriteLine($"bölüm {x.lvl} ({Id(x.lvl)}) adım {x.step}/{x.stepsTotal} puan {x.score}: {x.desc} {x.tags}");
        Console.WriteLine("# Altın Rulo (Rainbow) kullanımı, en çok soyan");
        foreach (var x in steps.Where(x => x.used == SpecialKind.Rainbow).OrderByDescending(x => x.peeled).Take(top))
            Console.WriteLine($"bölüm {x.lvl} ({Id(x.lvl)}) adım {x.step}/{x.stepsTotal}: {x.desc} {x.tags}");
        Console.WriteLine("# Roket kullanımı, en çok soyan");
        foreach (var x in steps.Where(x => x.used == SpecialKind.RocketRow || x.used == SpecialKind.RocketColumn).OrderByDescending(x => x.peeled).Take(top))
            Console.WriteLine($"bölüm {x.lvl} ({Id(x.lvl)}) adım {x.step}/{x.stepsTotal}: {x.desc} {x.tags}");
        Console.WriteLine("# Adım başına ortalama gösterişte en iyi bölümler (kısa çözümlü)");
        foreach (var x in levelScore.Where(x => x.steps <= 14).OrderByDescending(x => x.score).Take(top))
            Console.WriteLine($"bölüm {x.lvl} ({Id(x.lvl)}) ort {x.score} {x.tags}");
        return 0;
    }
}
