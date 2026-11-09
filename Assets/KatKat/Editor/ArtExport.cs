using System.IO;
using System.Text;
using KatKat.Core;
using KatKat.View;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// Oyun alanı görsellerini docs/gorseller/ altına PNG olarak çıkarır (kullanıcı yeniden çizecek).
    /// Kodla çizilenler (SpecialArt, ProceduralArt, UiStoneArt) çalışma anında üretilip kaydedilir;
    /// dosya olanlar (Resources/PremiumV4, Resources/Premium) olduğu gibi kopyalanır.
    /// Komut: tools/unity.ps1 export-art
    /// </summary>
    public static class ArtExport
    {
        private static string _root;
        private static readonly StringBuilder Manifest = new StringBuilder();

        [MenuItem("Kat Kat/Görselleri dışa aktar")]
        public static void ExportAll()
        {
            // docs/gorseller artık elle düzenlenen kaynak görseller; dışa aktarım ayrı klasöre (silinip yeniden yazılır).
            _root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "disa_aktarim"));
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            Directory.CreateDirectory(_root);
            Manifest.Clear();
            Manifest.AppendLine("dosya\tgenislik\tyukseklik\tkaynak");

            // Taşlar (katman yüzü + sembol; renk kodda boyanıyor).
            foreach (LayerColor c in System.Enum.GetValues(typeof(LayerColor)))
            {
                Save("tas", "tas_" + c.ToString().ToLowerInvariant(), UiStoneArt.Layer(c, true), "kod: UiStoneArt.Layer");
                Save("tas", "tas_" + c.ToString().ToLowerInvariant() + "_sembolsuz", UiStoneArt.Layer(c, false), "kod: UiStoneArt.Layer");
            }
            Save("tas", "yuz_maskesi", ProceduralArt.ShapeMask, "kod: ProceduralArt.ShapeMask (tiles/face-neutral'dan)");
            Save("tas", "sembol_atlasi", ProceduralArt.IconAtlas, "kod: ProceduralArt.IconAtlas (tiles/symbol-*'dan)");
            Save("tas", "golge_maskesi", ProceduralArt.ShadowMask, "kod: ProceduralArt.ShadowMask");
            CopyDir("tas", "PremiumV4/tiles");

            // Özel taşlar.
            Save("ozel", "roket", SpecialArt.Rocket, "kod: SpecialArt.Rocket");
            Save("ozel", "gokkusagi", SpecialArt.Rainbow, "kod: SpecialArt.Rainbow");
            Save("ozel", "ozel_cekirdek", SpecialArt.Disc, "kod: SpecialArt.Disc");

            // Mekanikler.
            Save("mekanik", "buz_saglam", SpecialArt.Ice(false), "kod: SpecialArt.Ice(false)");
            Save("mekanik", "buz_catlak", SpecialArt.Ice(true), "kod: SpecialArt.Ice(true)");
            Save("mekanik", "sunger", SpecialArt.Sponge, "kod: SpecialArt.Sponge");
            Save("mekanik", "dugme", SpecialArt.Button, "kod: SpecialArt.Button");
            Save("mekanik", "gecit_hayalet", SpecialArt.Square, "kod: SpecialArt.Square");
            Save("mekanik", "boya_damlasi", SpecialArt.Drop, "kod: SpecialArt.Drop");
            Save("mekanik", "atas_yedek", ProceduralArt.ClipSprite, "kod: ProceduralArt.ClipSprite");
            Save("mekanik", "atas_noktasi", ProceduralArt.DotSprite, "kod: ProceduralArt.DotSprite");
            Save("mekanik", "yildiz", ProceduralArt.StarSprite, "kod: ProceduralArt.StarSprite");
            Save("mekanik", "zarf_muhur_zemini", ProceduralArt.DiscSprite, "kod: ProceduralArt.DiscSprite");
            CopyDir("mekanik", "PremiumV4/mechanics");
            CopyFile("mekanik", "PremiumV4/icons/lock.png");

            // Tahta, efekt, maskot.
            Save("tahta", "tepsi", ProceduralArt.TraySprite, "kod: ProceduralArt.TraySprite");
            CopyDir("tahta", "PremiumV4/boards");
            Save("efekt", "parilti", SpecialArt.Glow, "kod: SpecialArt.Glow");
            CopyDir("efekt", "PremiumV4/effects");
            Save("maskot", "maskot_mutlu", SpecialArt.Mascot(SpecialArt.Mood.Happy), "kod: SpecialArt.Mascot");
            Save("maskot", "maskot_sevincli", SpecialArt.Mascot(SpecialArt.Mood.Cheer), "kod: SpecialArt.Mascot");
            Save("maskot", "maskot_endiseli", SpecialArt.Mascot(SpecialArt.Mood.Worried), "kod: SpecialArt.Mascot");

            File.WriteAllText(Path.Combine(_root, "liste.tsv"), Manifest.ToString(), new UTF8Encoding(false));
            Debug.Log("[KatKat] Görseller dışa aktarıldı → " + _root);
        }

        private static void Save(string group, string name, Sprite s, string src)
        {
            if (s == null) { Debug.LogWarning("[KatKat] boş görsel: " + name); return; }
            var r = new Rect(0, 0, s.texture.width, s.texture.height); // tam tuval: konum/merkez korunur
            Save(group, name, s.texture, src, new RectInt((int)r.x, (int)r.y, (int)r.width, (int)r.height));
        }

        private static void Save(string group, string name, Texture tex, string src, RectInt? rect = null)
        {
            if (tex == null) { Debug.LogWarning("[KatKat] boş doku: " + name); return; }
            var rr = rect ?? new RectInt(0, 0, tex.width, tex.height);
            var outTex = new Texture2D(rr.width, rr.height, TextureFormat.RGBA32, false);
            if (tex is Texture2D t2 && t2.isReadable)
            {
                outTex.SetPixels(t2.GetPixels(rr.x, rr.y, rr.width, rr.height, 0));
            }
            else
            {
                // Okunamaz dokular için GPU üzerinden kopya.
                var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(tex, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                outTex.ReadPixels(new Rect(rr.x, rr.y, rr.width, rr.height), 0, 0);
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
            outTex.Apply();
            var dir = Path.Combine(_root, group);
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), outTex.EncodeToPNG());
            Manifest.AppendLine($"{group}/{name}.png\t{rr.width}\t{rr.height}\t{src}");
            Object.DestroyImmediate(outTex);
        }

        private static void CopyFile(string group, string resFile)
        {
            var f = Path.Combine(Application.dataPath, "KatKat", "Resources", resFile);
            if (!File.Exists(f)) return;
            var dir = Path.Combine(_root, group);
            Directory.CreateDirectory(dir);
            File.Copy(f, Path.Combine(dir, Path.GetFileName(f)), true);
            var t = new Texture2D(2, 2);
            t.LoadImage(File.ReadAllBytes(f));
            Manifest.AppendLine($"{group}/{Path.GetFileName(f)}\t{t.width}\t{t.height}\tdosya: Resources/{resFile}");
            Object.DestroyImmediate(t);
        }

        private static void CopyDir(string group, string resDir)
        {
            var src = Path.Combine(Application.dataPath, "KatKat", "Resources", resDir);
            if (!Directory.Exists(src)) return;
            var dir = Path.Combine(_root, group);
            Directory.CreateDirectory(dir);
            foreach (var f in Directory.GetFiles(src, "*.png"))
            {
                var dst = Path.Combine(dir, Path.GetFileName(f));
                File.Copy(f, dst, true);
                var t = new Texture2D(2, 2);
                t.LoadImage(File.ReadAllBytes(f));
                Manifest.AppendLine($"{group}/{Path.GetFileName(f)}\t{t.width}\t{t.height}\tdosya: Resources/{resDir}/{Path.GetFileName(f)}");
                Object.DestroyImmediate(t);
            }
        }
    }
}
