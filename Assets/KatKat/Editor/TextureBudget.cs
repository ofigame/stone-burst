using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Ölçülmüş doku bütçesi (2026-11-23, kullanıcı: "uygulama boyutunu küçültebiliyorsak küçültelim"). Yakalamalarda
    /// <see cref="DisplaySizeProbe"/> her arayüz görselinin ekrandaki en büyük boyunu ölçtü; TextureBudget.tsv her doku
    /// için 1,34 kat pay bırakılmış ikinin kuvveti üst sınırı tutar (en az 256). Bu içe aktarıcı diğerlerinden sonra çalışır
    /// ve varsayılan ile Android/iOS en büyük boyunu bu sınıra indirir (kaynak dosya değişmez).
    /// </summary>
    public sealed class TextureBudget : AssetPostprocessor
    {
        public const string TablePath = "Assets/KatKat/Editor/TextureBudget.tsv";
        private static Dictionary<string, int> _table;

        public override int GetPostprocessOrder() => 1000;

        private static Dictionary<string, int> Table
        {
            get
            {
                if (_table != null) return _table;
                _table = new Dictionary<string, int>(StringComparer.Ordinal);
                if (!File.Exists(TablePath)) return _table;
                foreach (var line in File.ReadAllLines(TablePath))
                {
                    var p = line.Split('\t');
                    if (p.Length == 2 && int.TryParse(p[1], out var v)) _table[p[0].Trim()] = Mathf.Max(256, v);
                }
                return _table;
            }
        }

        public static bool TryCap(string path, out int cap) => Table.TryGetValue(path, out cap);

        private void OnPreprocessTexture()
        {
            if (!TryCap(assetPath, out int cap)) return;
            var ti = (TextureImporter)assetImporter;
            ti.maxTextureSize = Mathf.Min(ti.maxTextureSize, cap);
            foreach (var plat in new[] { "Android", "iPhone", "Standalone" })
            {
                var s = ti.GetPlatformTextureSettings(plat);
                if (!s.overridden) continue;
                s.maxTextureSize = Mathf.Min(s.maxTextureSize, cap);
                ti.SetPlatformTextureSettings(s);
            }
        }

        /// <summary>Tablodaki dokuları yeniden içe aktarır (toplu: tools/unity.ps1 texture-budget).</summary>
        [MenuItem("Kat Kat/Build/Ölçülmüş doku bütçesini uygula")]
        public static void Apply()
        {
            _table = null;
            int n = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var kv in Table)
                {
                    if (!File.Exists(kv.Key)) { Debug.LogWarning("[KatKat] bütçe: dosya yok " + kv.Key); continue; }
                    AssetDatabase.ImportAsset(kv.Key, ImportAssetOptions.ForceUpdate);
                    n++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            Debug.Log("[KatKat] Ölçülmüş doku bütçesi: " + n + " doku yeniden içe aktarıldı");
        }

        public static void ApplyBatch()
        {
            Apply();
            foreach (var kv in Table)
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(kv.Key);
                if (t != null) Debug.Log($"[KatKat] bütçe {kv.Key} → {t.width}x{t.height} (sınır {kv.Value})");
            }
        }
    }
}
