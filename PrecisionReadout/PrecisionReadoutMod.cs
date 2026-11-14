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
        new PLocalization().Register(); // strings/<language code>.po
        ModActions.Register();
        Settings.Load();

        var host = new GameObject("PrecisionReadoutPanel");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<Ui.SettingsPanel>();
    }
}
