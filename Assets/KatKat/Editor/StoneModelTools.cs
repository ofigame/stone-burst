using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// v10 taş 3D modelleri (Resources/Models/Stones/&lt;stoneId&gt;/): Blender ile sadeleştirilmiş model.fbx
    /// (tools/blender/decimate_stones.py, ~30 bin üçgen), color.png ve normal.png (1024). İçe aktarma: malzeme ve
    /// animasyon yok, örgü sıkıştırma orta; renk sRGB, normal NormalMap. Malzeme (Standard) <see cref="BuildMaterials"/>
    /// ile mat.mat olarak üretilir (shader ve varyantları derlemeye girsin diye varlık olarak).
    /// </summary>
    public sealed class StoneModelImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/KatKat/Resources/Models/Stones/";

        public override uint GetVersion() => 2;

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder, StringComparison.Ordinal)) return;
            var mi = (ModelImporter)assetImporter;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importCameras = false;
            mi.importLights = false;
            mi.isReadable = false;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.globalScale = 1f;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder, StringComparison.Ordinal)) return;
            var ti = (TextureImporter)assetImporter;
            bool normal = assetPath.EndsWith("/normal.png", StringComparison.Ordinal);
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !normal;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear;
            // APK bütçesi: vitrinde model ekranın ~yarısı; Android ASTC 6×6 yeterli (normal haritada da).
            ti.SetPlatformTextureSettings(ShellAssetImporter.AndroidAstc(1024, TextureImporterFormat.ASTC_6x6));
        }
    }

    public static class StoneModelTools
    {
        /// <summary>Tanılama: her modelin dünya sınırı (boyut, merkez) ve üçgen sayısı.</summary>
        public static void ReportBounds()
        {
            foreach (var dir in Directory.GetDirectories(StoneModelImporter.Folder))
            {
                string id = Path.GetFileName(dir);
                var prefab = Resources.Load<GameObject>("Models/Stones/" + id + "/model");
                if (prefab == null) { Debug.Log($"[KatKat] model yok: {id}"); continue; }
                var go = UnityEngine.Object.Instantiate(prefab);
                var rs = go.GetComponentsInChildren<Renderer>();
                var b = rs.Length > 0 ? rs[0].bounds : new Bounds();
                foreach (var r in rs) b.Encapsulate(r.bounds);
                int tris = 0;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
                Debug.Log($"[KatKat] model {id}: boyut {b.size} merkez {b.center} üçgen {tris} kök ölçek {go.transform.localScale} dönüş {go.transform.localEulerAngles}");
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [MenuItem("Kat Kat/Taşlar/3D malzemelerini üret")]
        public static void BuildMaterials()
        {
            AssetDatabase.Refresh();
            var shader = Shader.Find("Standard");
            int n = 0;
            foreach (var dir in Directory.GetDirectories(StoneModelImporter.Folder))
            {
                string d = dir.Replace('\\', '/');
                if (!File.Exists(d + "/model.fbx")) continue;
                var color = AssetDatabase.LoadAssetAtPath<Texture2D>(d + "/color.png");
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(d + "/normal.png");
                string path = d + "/mat.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool create = mat == null;
                if (create) mat = new Material(shader);
                mat.shader = shader;
                mat.SetTexture("_MainTex", color);
                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.SetFloat("_BumpScale", 1f);
                    mat.EnableKeyword("_NORMALMAP");
                }
                mat.SetFloat("_Metallic", 0.3f);
                mat.SetFloat("_Glossiness", 0.68f);
                // Işıltı vitrinde çalışma anında renklenir; anahtar kelime burada açık ki varyant derlemeye girsin.
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                if (create) AssetDatabase.CreateAsset(mat, path);
                EditorUtility.SetDirty(mat);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[KatKat] taş 3D malzemeleri: {n}");
        }
    }
}
