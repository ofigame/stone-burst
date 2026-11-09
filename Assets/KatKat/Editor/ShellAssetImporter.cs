using System;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v10 kabuk varlıkları (docs/v10/TriKat-Mavi-Liman-Ust-Parcalar, TriKat-Koleksiyon-Parcalari):
    /// PremiumV6/shell, collection, game altındaki PNG'ler tek Sprite (2D and UI), alfa saydamlığı açık, sRGB, düşük
    /// sıkıştırma (CompressedHQ), uzun kenar 2048'i aşarsa en çok 4096. Koleksiyon çerçevesi 9 dilim: sol/sağ 0
    /// (yatayda eşit ölçek), üst ve alt sabit (köşeler, alt elmas ve dalgalı uç ezilmez); yalnız orta dikey uzar.
    /// Audio/TriKat: kısa efektler belleğe açılır ve önceden yüklenir; döngü müziği akıştan çalınır.
    /// </summary>
    public sealed class ShellAssetImporter : AssetPostprocessor
    {
        private const string Root = "Assets/KatKat/Resources/PremiumV6/";
        private const string Audio = "Assets/KatKat/Resources/Audio/TriKat/";

        public override uint GetVersion() => 10;

        /// <summary>
        /// v12 sıralama (docs/v12/siralama/CLAUDE-SIRALAMA.md): Sprite, sRGB, mipmap kapalı, Bilinear, Clamp, alfa;
        /// en çok 4096; ilk kalite kontrolü için sıkıştırmasız (editör/Windows). Android'de ASTC 4×4 (yüksek kalite;
        /// APK boyutu için, cihazda ölçülüp gözden geçirilecek). 9 dilim sınırları köşeler incelenerek (px, kırpılmış):
        /// hap/satır/sekme uçları yüksekliğin yarısı (yalnız yatay esner); liste paneli 130; oyuncu kartı 200.
        /// Podyum, maskot, kupa, düğme, halka, madalya, rozet: tek parça (Preserve Aspect).
        /// </summary>
        private bool RankTexture()
        {
            if (!assetPath.StartsWith(Root + "rank/", StringComparison.Ordinal)) return false;
            var ti = (TextureImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = name != "bg";
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.isReadable = false;
            ti.SetPlatformTextureSettings(AndroidAstc(4096, TextureImporterFormat.ASTC_4x4));
            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            float half = h * 0.5f;
            Vector4 border = name switch
            {
                "row-blue" or "row-white" or "tab-on" or "tab-off" or "title-capsule" or "metric-pill" => new Vector4(half, 0f, half, 0f),
                "list-panel" => new Vector4(130f, 130f, 130f, 130f),
                "self-panel" => new Vector4(200f, 200f, 200f, 200f),
                _ => Vector4.zero,
            };
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteBorder = border;
            ti.SetTextureSettings(s);
            return true;
        }

        /// <summary>
        /// ui1 tasarım paketi (docs/ui1/_ortak/sprite → Resources/UI1): Sprite, sRGB, alfa, mipmap kapalı, Bilinear,
        /// Clamp; Android ASTC 4×4. 9 dilim sınırları paketin borderHint değerleri (yeni-sprite-import.json; sol, alt,
        /// sağ, üst) ve hap biçimlilerde yüksekliğin yarısı; kurdele/ikon/başlık plakası tek parça (Preserve Aspect).
        /// </summary>
        private bool Ui1Texture()
        {
            if (!assetPath.StartsWith("Assets/KatKat/Resources/UI1/", StringComparison.Ordinal)) return false;
            var ti = (TextureImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.isReadable = false;
            ti.SetPlatformTextureSettings(AndroidAstc(2048, TextureImporterFormat.ASTC_4x4));
            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            Vector4 border = name switch
            {
                "panel-modal" or "card-ivory" => new Vector4(40f, 40f, 40f, 40f),
                "button-primary" or "button-secondary" or "chip-objective" or "row-ivory" => new Vector4(60f, 24f, 60f, 24f),
                "progress-track" => new Vector4(h * 0.5f, 0f, h * 0.5f, 0f),
                "hud-moves-pill" or "hud-moves-pill-purple" => new Vector4(200f, 0f, 200f, 0f),
                "next-preview-panel" => new Vector4(90f, 90f, 90f, 90f),
                _ => Vector4.zero,
            };
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteBorder = border;
            ti.SetTextureSettings(s);
            return true;
        }

        /// <summary>
        /// ui3 onaylı tasarım (docs/ui3_onayli_tasarim/parcalar/asset-manifest.json → Resources/UI3): Sprite, sRGB,
        /// alfa, mipmap kapalı, Bilinear, Clamp. 9 dilim manifestteki borderLTRB (Unity sırası sol, alt, sağ, üst).
        /// bg/Z1–Z5 opak tam ekran zemin (alfa yok). Android ASTC.
        /// </summary>
        private bool Ui3Texture()
        {
            if (!assetPath.StartsWith("Assets/KatKat/Resources/UI3/", StringComparison.Ordinal)) return false;
            var ti = (TextureImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool bg = assetPath.Contains("/UI3/bg/");
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = !bg;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.isReadable = false;
            ti.SetPlatformTextureSettings(AndroidAstc(2048, TextureImporterFormat.ASTC_4x4));
            Vector4 border = name switch
            {
                "modal-seramik" => new Vector4(150f, 65f, 150f, 125f),
                "kart-seramik" => new Vector4(50f, 50f, 50f, 50f),
                "buton-birincil" or "buton-ikincil" => new Vector4(75f, 40f, 75f, 40f),
                "baslik-plakasi" => new Vector4(75f, 35f, 75f, 35f),
                _ => Vector4.zero,
            };
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteBorder = border;
            ti.SetTextureSettings(s);
            return true;
        }

        private void OnPreprocessTexture()
        {
            if (Ui3Texture()) return;
            if (Ui1Texture()) return;
            if (RankTexture()) return;
            bool shell = assetPath.StartsWith(Root + "shell/", StringComparison.Ordinal)
                || assetPath.StartsWith(Root + "collection/", StringComparison.Ordinal)
                || assetPath.StartsWith(Root + "game/", StringComparison.Ordinal)
                || assetPath.StartsWith(Root + "skills/", StringComparison.Ordinal);
            if (!shell) return;
            var ti = (TextureImporter)assetImporter;
            bool background = assetPath.EndsWith("collection/bg.png", StringComparison.Ordinal) || assetPath.EndsWith("game/bg.png", StringComparison.Ordinal);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = !background;
            // Unity 6.6: 2'nin kuvveti olmayan dokuda mipmap açıkken ASTC uygulanmıyor (ham RGBA kalıyordu; bkz.
            // BuildScripts.ApplyTextureBudget). Kabuk görselleri ekranda ~yarı boyutta çizilir; NPOT'ta mipmap kapalı.
            bool pot = IsPotSource(ti);
            ti.mipmapEnabled = pot;
            ti.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear;
            // Kullanıcı (2026-11-07: APK küçült): en çok 2048. Android varsayılanı ETC2 kenarı 4'ün katı olmayan dokuyu
            // sıkıştıramıyor (941×1672 gibi) → sıkıştırmasız RGBA giriyordu. Android'de ASTC 6×6 her boyutta sıkıştırır.
            // v11: uzun kenarı 2048 üstündeki arayüz kaynağı (mavi panel 2172, beyaz bölge 2159) en çok 4096.
            ti.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
            int maxSize = Mathf.Max(srcW, srcH) > 2048 ? 4096 : 2048;
            ti.maxTextureSize = maxSize;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.isReadable = false;
            ti.SetPlatformTextureSettings(AndroidAstc(maxSize, BudgetFormat(ti)));
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteBorder = assetPath.EndsWith("collection/panel.png", StringComparison.Ordinal) ? new Vector4(0f, 250f, 0f, 130f) : Vector4.zero;
            ti.SetTextureSettings(s);
        }

        /// <summary>Kaynak doku kenarları 2'nin kuvveti mi.</summary>
        internal static bool IsPotSource(TextureImporter ti)
        {
            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            return w > 0 && Mathf.IsPowerOfTwo(w) && Mathf.IsPowerOfTwo(h);
        }

        /// <summary>Doku bütçesiyle aynı seçim (BuildScripts.ApplyTextureBudget): büyük (>1024) ASTC 5×5, diğerleri 4×4.</summary>
        internal static TextureImporterFormat BudgetFormat(TextureImporter ti)
        {
            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            return Mathf.Max(w, h) > 1024 ? TextureImporterFormat.ASTC_5x5 : TextureImporterFormat.ASTC_4x4;
        }

        /// <summary>Android ASTC geçersiz kılma (her doku boyutunda sıkıştırır; çoğu Android cihazı destekler).</summary>
        internal static TextureImporterPlatformSettings AndroidAstc(int maxSize, TextureImporterFormat format) => new TextureImporterPlatformSettings
        {
            name = "Android",
            overridden = true,
            maxTextureSize = maxSize,
            format = format,
            compressionQuality = 100,
        };

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Audio, StringComparison.Ordinal)) return;
            var ai = (AudioImporter)assetImporter;
            bool music = assetPath.EndsWith("menu-loop.wav", StringComparison.Ordinal);
            ai.forceToMono = false; // stereo kaynak korunur
            ai.loadInBackground = music;
            var set = ai.defaultSampleSettings;
            set.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            set.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            set.quality = music ? 0.7f : 1f;
            set.preloadAudioData = !music;
            set.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            ai.defaultSampleSettings = set;
        }
    }
}
