using HarmonyLib;

namespace PrecisionReadout.Patches;

// The hover card does not use GameUtil for a tile's mass: HoverTextHelper builds the text itself as four parts
// (integer part, ".d", unit, breathability) and the hover card draws them one after another.
// The whole text, unit included, goes into the first part: a unit in a later part of a dimmed number did not show.
[HarmonyPatch(typeof(HoverTextHelper), nameof(HoverTextHelper.MassStringsReadOnly))]
public static class HoverMassPatch
{
    private static readonly string[] Parts = new string[4];

    public static void Postfix(int cell, ref string[] __result)
    {
        if (!Scope.Active || !Grid.IsValidCell(cell)) return;
        var element = Grid.Element[cell];
        if (element == null || element.id == SimHashes.Vacuum || element.id == SimHashes.Unobtanium) return;
        var text = Readout.Mass(Grid.Mass[cell], includeSuffix: true);
        if (text == null) return;

        Parts[0] = text;
        Parts[1] = "";
        Parts[2] = "";
        Parts[3] = __result.Length > 3 ? __result[3] : "";
        __result = Parts;
    }
}
