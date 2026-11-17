using System;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Tepsi görselleri (kullanıcı 2026-11-17, docs/varliklar/gorseller/tepsi): Resources/TrayArt altındaki kenar şeridi, zemin ve
    /// hücre yuvası. Tahta dokusu oyun içinde CPU'da bunlardan örneklenerek pişirilir (TrayArt.Bake) → okunabilir,
    /// sıkıştırmasız, mip yok. Doku bütçesi (BuildScripts.ApplyTextureBudget) okunabilir dokuları atlar.
    /// </summary>
    public sealed class TrayArtImporter : AssetPostprocessor
    {
        private const string Dir = "Assets/KatKat/Resources/TrayArt/";

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dir, StringComparison.Ordinal)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.mipmapEnabled = false;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = assetPath.EndsWith("socket.png", StringComparison.Ordinal);
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 2048;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.isReadable = true;
        }
    }
}
