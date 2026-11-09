using System;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v10 P2.6: koleksiyon taşı görselleri (Resources/Collection/stones, kaynak: docs/taşlar, 512 px yüksekliğe
    /// küçültülmüş saydam PNG). Sprite, mip (Kaiser) + trilinear: koleksiyon ızgarasında ~200 px, bildirimde ~96 px
    /// gösterildiği için küçültmede kenar tırtığı olmasın.
    /// </summary>
    public sealed class CollectionArtImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/KatKat/Resources/Collection/stones/";
        /// <summary>Kilitli kartlar için önceden bulanıklaştırılmış sürümler (taş tanınmasın; yalnız renk lekesi).</summary>
        public const string BlurFolder = "Assets/KatKat/Resources/Collection/stones_blur/";

        public override uint GetVersion() => 5;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder, StringComparison.Ordinal) && !assetPath.StartsWith(BlurFolder, StringComparison.Ordinal)) return;
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
            // 450×512 gibi 4'ün katı olmayan boyutlar ETC2 ile sıkışmıyordu; Android'de ASTC 6×6.
            ti.SetPlatformTextureSettings(ShellAssetImporter.AndroidAstc(512, assetPath.StartsWith(BlurFolder, StringComparison.Ordinal) ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4));
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
        }
    }
}
