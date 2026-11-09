using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KatKat.Core;
using KatKat.Levels;

/// <summary>
/// v10 P5.2: ZPE bölüm referans verisi (Resources/Zpe/level_ref.json). Her bölüm için doğrulanmış kayıtlı çözüm
/// harcadığı hamle (en kısa iddiası değil), hamle sınırı ve plansız botun kazanma oranı (zorluk). Bölüm dosyalarına dokunmaz.
/// Kullanım: dotnet run -c Release -- zperef [oyun_sayısı=64]
/// </summary>
static partial class Program
{
    static int ZpeRef(string[] args)
    {
        int games = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 64;
        var dir = Path.Combine(Root, "Assets", "KatKat", "Resources", "Levels");
        var files = Directory.GetFiles(dir, "level_*.json").OrderBy(f => LevelValidator.LevelNumber(Path.GetFileNameWithoutExtension(f))).ToList();
        var sb = new StringBuilder();
        sb.Append("{\n  \"version\": 1,\n  \"games\": ").Append(games).Append(",\n  \"levels\": [\n");
        var t0 = DateTime.Now;
        for (int i = 0; i < files.Count; i++)
        {
            var d = LevelParser.Parse(File.ReadAllText(files[i]), Path.GetFileName(files[i]));
            int n = LevelValidator.LevelNumber(d.Id);
            int limit = d.MoveLimit;
            // Referans: kayıtlı çözümün gerçekten harcadığı hamle (kural motoruyla tekrar; tepsi kullanımı hamle harcamaz).
            var rep = LevelValidator.ReplaySolution(d);
            if (!rep.Success) throw new InvalidOperationException(d.Id + ": kayıtlı çözüm kazanmıyor: " + rep.Error);
            int steps = Math.Max(1, limit - rep.MovesLeft);
            double wr = BotRate(d, limit, n, games);
            sb.Append("    {\"id\":\"").Append(d.Id).Append("\",\"steps\":").Append(steps).Append(",\"limit\":").Append(limit)
              .Append(",\"bot\":").Append(wr.ToString("0.000", CultureInfo.InvariantCulture)).Append('}');
            sb.Append(i < files.Count - 1 ? ",\n" : "\n");
            if (i % 100 == 99) Console.WriteLine($"zperef {i + 1}/{files.Count} ({(DateTime.Now - t0).TotalSeconds:0} sn)");
        }
        sb.Append("  ]\n}\n");
        var outDir = Path.Combine(Root, "Assets", "KatKat", "Resources", "Zpe");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "level_ref.json"), sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"zperef: {files.Count} bölüm, {(DateTime.Now - t0).TotalMinutes:0.0} dk");
        return 0;
    }
}
