using KatKat.UI;
using UnityEditor;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>vr1 P1 prototipini editörde açar/kapatır (PlayerPrefs; mağaza derlemesinde varsayılan kapalı).</summary>
    public static class Vr1ProtoMenu
    {
        private const string Item = "KatKat/vr1 prototipi (P1)";

        [MenuItem(Item)]
        private static void Toggle()
        {
            bool on = !Vr1Proto.On;
            Vr1Proto.SetPref(on);
            Debug.Log("[KatKat] vr1 prototipi " + (on ? "AÇIK" : "kapalı"));
        }

        [MenuItem(Item, true)]
        private static bool ToggleCheck()
        {
            Menu.SetChecked(Item, Vr1Proto.On);
            return true;
        }
    }
}
