using System;
using System.IO;
using KatKat.Core;
using KatKat.UI;
using KatKat.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace KatKat.EditorTools
{
    /// <summary>Independent visual smoke checks; does not generate or modify levels or save data.</summary>
    public static class DesignVisualVerification
    {
        [MenuItem("Kat Kat/Yakalama/Tasarım düzeltmelerini doğrula")]
        public static void Run()
        {
            if (!UiTypography.Font.HasCharacters("ÇçĞğİıÖöŞşÜü0123456789"))
                throw new InvalidOperationException("Turkish SDF glyph coverage failed.");
            if (ProceduralArt.ShapeMask.width != 512 || ProceduralArt.IconAtlas.width != 1024)
                throw new InvalidOperationException("Layer mask/atlas resolution regressed.");
            for (int i = 0; i < 4; i++)
            {
                var chip = PremiumArt.Chip((LayerColor)i);
                if (!chip || chip.rect.width != UiStoneArt.FacePixels || chip != UiStoneArt.Chip((LayerColor)i))
                    throw new InvalidOperationException("HUD/gameplay chip sharing failed.");
            }
            var root = new GameObject("TypographyProbe");
            try
            {
                var label = UiFactory.Title("Probe", root.transform, "ÇÖL · BAHÇE · SAHİL", 48, UiArt.Cream, UiArt.HudTealDark);
                if (!label.fontSharedMaterial.IsKeywordEnabled("OUTLINE_ON") ||
                    !label.fontSharedMaterial.IsKeywordEnabled("UNDERLAY_ON") ||
                    label.GetComponent<UnityEngine.UI.Outline>() != null)
                    throw new InvalidOperationException("SDF title styling failed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            // A serialized collection keeps dynamically enabled title variants in player builds.
            const string variantsPath = "Assets/KatKat/Resources/Typography/KatKatSdfVariants.shadervariants";
            if (!AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(variantsPath))
            {
                var variants = new ShaderVariantCollection();
                var shader = UiTypography.Font.material.shader;
                foreach (var keywords in new[] { new string[0], new[] { "OUTLINE_ON" }, new[] { "UNDERLAY_ON" }, new[] { "OUTLINE_ON", "UNDERLAY_ON" } })
                    variants.Add(new ShaderVariantCollection.ShaderVariant(shader, PassType.Normal, keywords));
                AssetDatabase.CreateAsset(variants, variantsPath);
                AssetDatabase.SaveAssets();
            }
            GameCapture.CaptureVisualV3();
            File.WriteAllText("docs/captures/visual-v3/DESIGN_CHECKS.txt",
                "PASS: Turkish SDF glyphs; 512 face / 1024 atlas; four shared 512 UI chips; SDF title shader keywords.\n" +
                "CaptureVisualV3 completed. Captures are editor renders, not Android device performance results.\n");
            Debug.Log("[KatKat Design] All visual smoke checks passed.");
        }
    }
}
