using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KatKat.Core;
using KatKat.Levels;

// moments [adet]: tanıtım videosu için gösterişli anlar (2026-11-12, Arapça tanıtım). Her bölümün kayıtlı çözümü
// yeniden oynanır; adım başına zincir dalgası, soyulan taş, doğan özel taş ve Altın Rulo kullanımı puanlanır.
// Çıktı: en iyi adımlar (bölüm sırası 0 tabanlı, adım sırası) ve bölümün tamamı için toplam gösteriş puanı.
static partial class Program
{
    static int TrailerMoments(string[] args)
    {
        int top = args.Length > 1 ? int.Parse(args[1]) : 30;
        var files = Directory.GetFiles(LevelsDir, "level_*.json").OrderBy(f => f, StringComparer.Ordinal).ToList();
        var steps = new List<(int lvl, int step, int score, string desc, int stepsTotal)>();
        var levelScore = new List<(int lvl, int score, int steps, int w, int h)>();
        for (int i = 0; i < files.Count; i++)
        {
            var d = LevelParser.Parse(File.ReadAllText(files[i]), Path.GetFileName(files[i]));
            var s = LevelLoader.ToState(d);
            int total = 0, k = 0;
            foreach (var step in d.Solution)
            {
                var r = RulesEngine.TryApply(s, LevelLoader.ToCommand(step));
                if (!r.Accepted) break;
                int chains = r.Waves.Count(w => w.Kind == WaveKind.Chain);
                int peeled = r.TotalPeeled;
                int sc = chains * 10 + peeled + (r.SpecialCreated == SpecialKind.None ? 0 : 15)
                         + (r.UsedSpecial != SpecialKind.None ? peeled / 2 + 10 : 0);
                string desc = $"{step.Type} zincir={chains} soyulan={peeled} doğan={r.SpecialCreated} kullanılan={r.UsedSpecial}";
                steps.Add((i, k, sc, desc, d.Solution.Count));
                total += sc; k++;
            }
            levelScore.Add((i, total / Math.Max(1, d.Solution.Count), d.Solution.Count, s.Board.Width, s.Board.Height));
        }
        Console.WriteLine("# En gösterişli adımlar");
        foreach (var x in steps.OrderByDescending(x => x.score).Take(top))
            Console.WriteLine($"bölüm {x.lvl} (L{x.lvl + 1}) adım {x.step}/{x.stepsTotal} puan {x.score}: {x.desc}");
        Console.WriteLine("# Adım başına ortalama gösterişte en iyi bölümler (kısa çözümlü)");
        foreach (var x in levelScore.Where(x => x.steps <= 14).OrderByDescending(x => x.score).Take(top))
            Console.WriteLine($"bölüm {x.lvl} (L{x.lvl + 1}) ort {x.score} adım {x.steps} tahta {x.w}x{x.h}");
        return 0;
    }
}
