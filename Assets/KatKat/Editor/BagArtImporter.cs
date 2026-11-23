using System;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Torba görselleri (kullanıcı 2026-11-23, docs/varliklar/gorseller/torba): Resources/Bag altındaki arka gövde, ağzı oyulmuş ön
    /// katman ve boyanan kurdele maskesi (512 px). Tek Sprite, alfa saydamlık, mip yok; Android/iOS ASTC 4×4 (kenar yumuşak kalsın).
    /// </summary>
    public sealed class BagArtImporter : AssetPostprocessor
    {
        private const string Dir = "Assets/KatKat/Resources/Bag/";

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dir, StringComparison.Ordinal)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 512;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.isReadable = false;
            ti.spritePixelsPerUnit = 100f;
            ti.SetPlatformTextureSettings(ShellAssetImporter.AndroidAstc(512, TextureImporterFormat.ASTC_4x4));
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "iPhone", overridden = true, maxTextureSize = 512, format = TextureImporterFormat.ASTC_4x4, compressionQuality = 100,
            });
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
        }
    }
}
