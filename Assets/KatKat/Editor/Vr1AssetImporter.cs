using System;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// vr1 P1 prototip varlıkları (docs/vr1/claude_code_paketi_v1/belgeler/04_ANIMASYON_GORSEL_SES.md "PNG import"):
    /// Resources/Vr1 görselleri Sprite 2D/UI, alpha saydamlık, sRGB, UI için mip kapalı, bilinear; kaynak orijinal boyutta
    /// saklanır, çalışma dokusu küçük ölçekte (ikon 256, buz kabuğu 512). Resources/Audio/Vr1 kısa sesleri belleğe açılır.
    /// </summary>
    public sealed class Vr1AssetImporter : AssetPostprocessor
    {
        private const string Art = "Assets/KatKat/Resources/Vr1/";
        private const string Audio = "Assets/KatKat/Resources/Audio/Vr1/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Art, StringComparison.Ordinal)) return;
            var ti = (TextureImporter)assetImporter;
            bool icon = assetPath.Contains("/nav-");
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = icon ? 256 : 512;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.isReadable = false;
            ti.SetPlatformTextureSettings(ShellAssetImporter.AndroidAstc(icon ? 256 : 512, TextureImporterFormat.ASTC_4x4));
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Audio, StringComparison.Ordinal)) return;
            var ai = (AudioImporter)assetImporter;
            ai.forceToMono = true;
            ai.loadInBackground = false;
            var s = ai.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.preloadAudioData = true;
            s.compressionFormat = AudioCompressionFormat.ADPCM;
            ai.defaultSampleSettings = s;
        }
    }
}
