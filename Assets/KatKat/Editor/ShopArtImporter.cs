using System;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Mağaza ve güçlendirici simgeleri (2026-11-20; Resources/Shop, kaynak docs/yeni_v2 → tools/shop_icons.ps1, 512 px
    /// saydam). Koleksiyon taşlarıyla aynı ayar: sprite, mip (Kaiser) + trilinear (alt menüde ~96 px, şeritte ~130 px).
    /// </summary>
    public sealed class ShopArtImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/KatKat/Resources/Shop/";

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder, StringComparison.Ordinal)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = true;
            ti.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear;
            ti.maxTextureSize = 512;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.isReadable = false;
            ti.SetPlatformTextureSettings(ShellAssetImporter.AndroidAstc(512, TextureImporterFormat.ASTC_4x4));
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
        }
    }
}
