using KatKat.View;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>
    /// 2026-11-26 (kullanıcı: "Normal Taş Patlatma ve Hedefe Taş Toplama Animasyonu"; efekt kalitesi Düşük/Normal/Yüksek denenebilsin):
    /// oynatma kipinde efekt kalitesini zorlar (<see cref="BoosterFxConfig.QualityOverride"/>), ayar dosyasını oluşturur/seçer.
    /// </summary>
    public static class BlastFxMenu
    {
        private const string Root = "Kat Kat/Efekt Denemesi/";
        private const string AssetPath = "Assets/KatKat/Resources/BoosterFx/BoosterFxConfig.asset";

        [MenuItem(Root + "Kalite: Düşük")] private static void Low() => Set(FxQuality.Low);
        [MenuItem(Root + "Kalite: Normal")] private static void Normal() => Set(FxQuality.Normal);
        [MenuItem(Root + "Kalite: Yüksek")] private static void High() => Set(FxQuality.High);
        [MenuItem(Root + "Kalite: Otomatik (ayar dosyası)")] private static void Auto() { BoosterFxConfig.QualityOverride = null; Debug.Log("[KatKat] Efekt kalitesi: otomatik"); }

        [MenuItem(Root + "Kalite: Düşük", true)] [MenuItem(Root + "Kalite: Normal", true)] [MenuItem(Root + "Kalite: Yüksek", true)]
        private static bool PlayingOnly() => Application.isPlaying;

        private static void Set(FxQuality q)
        {
            BoosterFxConfig.QualityOverride = q;
            Debug.Log("[KatKat] Efekt kalitesi zorlandı: " + q);
        }

        /// <summary>Ayar dosyası yoksa varsayılanlarla oluşturur (Resources/BoosterFx) ve seçer: değerler kod değiştirmeden denenir.</summary>
        [MenuItem(Root + "Ayar Dosyasını Aç")]
        private static void OpenConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<BoosterFxConfig>(AssetPath);
            if (cfg == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AssetPath));
                cfg = ScriptableObject.CreateInstance<BoosterFxConfig>();
                AssetDatabase.CreateAsset(cfg, AssetPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = cfg;
            EditorGUIUtility.PingObject(cfg);
        }
    }
}
