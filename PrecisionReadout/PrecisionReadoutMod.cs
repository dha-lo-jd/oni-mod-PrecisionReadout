using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Database;
using UnityEngine;

namespace PrecisionReadout;

public sealed class PrecisionReadoutMod : UserMod2
{
    internal static string modPath;

    public override void OnLoad(Harmony harmony)
    {
        base.OnLoad(harmony);
        modPath = path;
        PUtil.InitLibrary();
        new PLocalization().Register(); // translations/<language code>.po
        ModActions.Register();
        Settings.Load();

        var host = new GameObject("PrecisionReadoutPanel");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<Ui.SettingsPanel>();
    }

    // PLib applies the translation in its own Localization.Initialize postfix; this one runs after it.
    [HarmonyPatch(typeof(Localization), nameof(Localization.Initialize))]
    private static class Localization_Initialize_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix() => ModActions.RefreshTitle();
    }
}
