using System;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Su kovası görselleri (kullanıcı 2026-11-26, docs/varliklar/gorseller/kova): Resources/Bucket altında arka (kova), ön (çember,
    /// parlaklık, cam kenarı), iç maske ve cam çatlağı (512/384 px; kaynaktan katmanlara ayrıldı). Tek Sprite, 512 px = 1 birim (kova
    /// katmanları alt-orta pivot, çatlak orta), mip yok; Android/iOS ASTC 4×4.
    /// </summary>
    public sealed class BucketArtImporter : AssetPostprocessor
    {
        private const string Dir = "Assets/KatKat/Resources/Bucket/";

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dir, StringComparison.Ordinal)) return;
            var ti = (TextureImporter)assetImporter;
            bool crack = assetPath.EndsWith("glass-crack.png", StringComparison.Ordinal);
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
            ti.spritePixelsPerUnit = crack ? 384f : 512f;
            ti.SetPlatformTextureSettings(ShellAssetImporter.AndroidAstc(512, TextureImporterFormat.ASTC_4x4));
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "iPhone", overridden = true, maxTextureSize = 512, format = TextureImporterFormat.ASTC_4x4, compressionQuality = 100,
            });
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = crack ? (int)SpriteAlignment.Center : (int)SpriteAlignment.BottomCenter;
            ti.SetTextureSettings(s);
        }
    }
}
