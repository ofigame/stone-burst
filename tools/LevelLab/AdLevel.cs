using System;
using System.IO;
using System.Linq;
using System.Text;
using KatKat.Core;
using KatKat.Levels;

// adshow <kaynak.txt> [çıktı.json] (2026-11-28 reklam bölümü; kullanıcı: "Büyük patlatma gruplarının olmadığı … üst katmanda küçük
// grupları soyduğumuz ve büyük gruba ulaştığımız bir edit yapalım. Gerekirse reklam için bölüm üret."): kaynağı oyunun kurallarıyla
// adım adım oynatır, her adımda tahtayı (ÜSTTEN alta katmanlar), dalgaları, reddi ve en büyük üst grubu yazar; istenirse JSON üretir.
static partial class Program
{
    static int AdShow(string[] args)
    {
        var d = ParseSource(args[1]);
        var s = LevelLoader.ToState(d);
        string Ch(LayerColor c) => c switch { LayerColor.Coral => "M", LayerColor.Blue => "T", LayerColor.Yellow => "i", LayerColor.Purple => "L", LayerColor.Navy => "Y", _ => "?" };
        void Print()
        {
            for (int y = 0; y < s.Board.Height; y++)
            {
                var sb = new StringBuilder("  ");
                for (int x = 0; x < s.Board.Width; x++)
                {
                    var c = s.Board[x, y];
                    var st = !c.Active ? "#" : string.Concat(Enumerable.Reverse(c.Layers).Select(Ch)) + (c.HasSpecial ? "@" : "");
                    sb.Append(st.Length == 0 ? "." : st).Append(new string(' ', Math.Max(1, 6 - st.Length)));
                }
                Console.WriteLine(sb);
            }
            var groups = GroupFinder.AllComponents(s.Board).Select(g => g.Count).OrderByDescending(n => n).ToList();
            Console.WriteLine($"  üst gruplar: {string.Join(" ", groups.Where(n => n >= 2))}  tepsi: {string.Join(",", s.Tray)}  hedef: " +
                string.Join(", ", s.Goals.Select(g => $"{g.Color} {g.Progress}/{g.Amount}")) + $"  hamle {s.MovesLeft}");
        }
        Console.WriteLine($"{d.Id}  (x soldan, y üstten; hücrede katmanlar ÜSTTEN alta: M=mercan T=turkuaz i=inci L=mor Y=lacivert yıldız, @=özel)");
        Print();
        int n = 0;
        foreach (var step in d.Solution)
        {
            var r = RulesEngine.TryApply(s, LevelLoader.ToCommand(step));
            Console.WriteLine($"\n{++n}. {step.Type} ({step.X},{step.Y}){(step.Type == "use" ? " yuva " + step.Row : "")}: " + (r.Accepted ? "kabul" : "RED " + r.Reason) +
                (r.SpecialCreated != SpecialKind.None ? $"  özel: {r.SpecialCreated}{(r.SpecialToTray ? " → tepsi" : "")}" : ""));
            foreach (var w in r.Waves)
                Console.WriteLine($"   dalga {w.Index} [{w.Kind}] {w.Cells.Count}: " + string.Join(" ", w.Cells.Select(c => $"({c.X},{c.Y}){Ch(c.Removed)}")));
            if (r.Accepted) Print();
        }
        Console.WriteLine("\nsonuç: " + s.Status);
        if (args.Length > 2)
        {
            d.StarThresholds = StarThresholds(d);
            File.WriteAllText(args[2], ToJson(d), new UTF8Encoding(false));
            Console.WriteLine("JSON → " + args[2]);
        }
        return 0;
    }
}
