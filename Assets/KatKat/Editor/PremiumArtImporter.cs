using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Premium görsellerin içe aktarma ayarı: UI sprite'ı, mip yok, bilinear, sRGB, kenarlıklar ArtSlicer'ın
    /// yazdığı JSON'dan. Resources/Premium (v2 sayfa kesimleri) ve Resources/PremiumV4 (bağımsız varlıklar).
    /// ArtSlicer zaten kullanım bütçesine küçülttüğü için Max Size kaynağı küçültmez (2048 ≥ en uzun kenar);
    /// v4 arka planları 941×1672 doğal boyuttadır, büyütülmez.
    /// </summary>
    public sealed class PremiumArtImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/KatKat/Resources/Premium/";
        public const string FolderV4 = "Assets/KatKat/Resources/PremiumV4/";
        private const string SliceJson = "Assets/KatKat/Editor/PremiumSlices.json";
        private const string SliceJsonV4 = "Assets/KatKat/Editor/PremiumV4Slices.json";

        [Serializable] private sealed class Item { public string name; public int w, h; public int[] border; }
        [Serializable] private sealed class List { public Item[] items; }

        public override uint GetVersion() => 6;

        private void OnPreprocessTexture()
        {
            bool v4 = assetPath.StartsWith(FolderV4, StringComparison.Ordinal);
            if (!v4 && !assetPath.StartsWith(Folder, StringComparison.Ordinal)) return;
            var ti = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            string key = v4 ? Path.GetFileName(Path.GetDirectoryName(assetPath)) + "/" + name : name;
            bool background = v4 ? key.StartsWith("backgrounds/", StringComparison.Ordinal) : name.StartsWith("bg_", StringComparison.Ordinal);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            // v5 piksel düzeltmesi: 768–1536 px UI/ikon dokuları ekranda 5–10 kat küçük gösteriliyor; mip'siz bilinear
            // küçültme kenarlarda tırtık ve parlak noktalarda kırılma (kullanıcı: "piksel bozukluk") üretiyordu.
            // Arka planlar ~1:1 gösterildiği için mip'siz kalır; diğerlerinde Kaiser süzgeçli mip + trilinear.
            ti.mipmapEnabled = !background;
            ti.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            ti.borderMipmap = false;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = !background;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = background ? FilterMode.Bilinear : FilterMode.Trilinear;
            ti.spritePixelsPerUnit = 100f;
            // Kesimler en çok ~1400 px; küçültme olmasın ki 9-dilim kenarlıkları kaynak pikselleriyle eşleşsin.
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            // Taş yüzü ve şekil işaretleri çalışma anında maske/atlas üretmek için okunur (sıkıştırmasız kalite referansı).
            bool tile = v4 && key.StartsWith("tiles/", StringComparison.Ordinal);
            ti.isReadable = tile;
            if (tile) ti.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = BorderFor(v4 ? SliceJsonV4 : SliceJson, key);
            ti.SetTextureSettings(settings);
        }

        private static Vector4 BorderFor(string json, string key)
        {
            if (!File.Exists(json)) return Vector4.zero;
            var list = JsonUtility.FromJson<List>(File.ReadAllText(json));
            foreach (var it in list.items)
                if (it.name == key && it.border != null && it.border.Length == 4)
                    return new Vector4(it.border[0], it.border[1], it.border[2], it.border[3]);
            return Vector4.zero;
        }
    }
}
