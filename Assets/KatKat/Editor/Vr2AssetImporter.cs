using System;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// VR2 oyun ekranı arka planları (docs/vr2/CLAUDE_CODE.md "Arka plan ve tablet"): Resources/VR2/bg altındaki altı
    /// 2160×2880 PNG tek Sprite; Max Size 4096 (tam çözünürlük kalır), sRGB açık, Wrap Clamp, mip kapalı, bilinear,
    /// alfa yok (yalnız doku). Android ASTC 6×6 ve iOS ASTC 6×6 (her biri ~2,8 MB bellek). Sıkıştırmanın görünümü
    /// gerçek cihazda kontrol edilecek (belge). Yalnız etkin tema yüklenir (KatKat.UI.Vr2.Background).
    /// </summary>
    public sealed class Vr2AssetImporter : AssetPostprocessor
    {
        private const string Bg = "Assets/KatKat/Resources/VR2/bg/";

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Bg, StringComparison.Ordinal)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 4096;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.isReadable = false;
            ti.spritePixelsPerUnit = 100f;
            ti.SetPlatformTextureSettings(ShellAssetImporter.AndroidAstc(4096, TextureImporterFormat.ASTC_6x6));
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "iPhone", overridden = true, maxTextureSize = 4096, format = TextureImporterFormat.ASTC_6x6, compressionQuality = 100,
            });
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
        }
    }
}
