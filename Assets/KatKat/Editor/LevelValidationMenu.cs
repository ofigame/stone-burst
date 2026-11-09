using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using KatKat.Levels;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Editör menüsü: bütün seviyeleri statik doğrula + kayıtlı çözümleri gerçek kural motoruyla oynat,
    /// docs/SEVIYE_DOGRULAMA.md raporunu yaz. Batch: -executeMethod KatKat.EditorTools.LevelValidationMenu.RunBatch
    /// </summary>
    public static class LevelValidationMenu
    {
        public const string LevelsDir = "Assets/KatKat/Resources/Levels";
        public const string ReportPath = "docs/SEVIYE_DOGRULAMA.md";

        [MenuItem("Kat Kat/Seviyeler/Doğrula ve çözümleri oynat")]
        public static void ValidateFromMenu()
        {
            var entries = Run();
            int bad = entries.Count(e => !e.Passed);
            EditorUtility.DisplayDialog("Kat Kat seviye doğrulama",
                bad == 0 ? $"{entries.Count}/{entries.Count} bölüm geçti.\n{ReportPath} güncellendi." : $"{bad} bölüm HATALI. Ayrıntı: Console ve {ReportPath}.", "Tamam");
        }

        public static void RunBatch()
        {
            var entries = Run();
            EditorApplication.Exit(entries.All(e => e.Passed) ? 0 : 1);
        }

        public static List<ValidationReport.Entry> Run()
        {
            var entries = new List<ValidationReport.Entry>();
            foreach (var f in Directory.GetFiles(LevelsDir, "level_*.json").OrderBy(f => f, StringComparer.Ordinal))
            {
                var e = ValidationReport.Check(Path.GetFileName(f), File.ReadAllText(f));
                entries.Add(e);
                if (e.Passed) Debug.Log($"[KatKat] {e.Id}: OK, {e.Replay.Steps}/{e.Replay.MoveLimit} adım, {e.Replay.Waves} dalga, Won");
                else Debug.LogError($"[KatKat] {e.Source}: {string.Join(" | ", e.StaticErrors)} {e.Replay?.Error}");
            }
            var md = ValidationReport.Markdown(entries, "Unity editör menüsü (Kat Kat/Seviyeler)", DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            File.WriteAllText(ReportPath, md, new System.Text.UTF8Encoding(false));
            Debug.Log($"[KatKat] Seviye doğrulama: {entries.Count(e => e.Passed)}/{entries.Count} geçti → {ReportPath}");
            return entries;
        }
    }
}
