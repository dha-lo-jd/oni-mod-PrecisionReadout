using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace PrecisionReadout.Patches;

// Hover card and the info panel parts that print stored items / contents.
[HarmonyPatch]
public static class SurfaceScopePatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(SelectToolHoverTextCard), nameof(SelectToolHoverTextCard.UpdateHoverElements));
        yield return AccessTools.Method(typeof(SimpleInfoScreen), "RefreshStoragePanel");
        yield return AccessTools.Method(typeof(SimpleInfoScreen), "BuildDisplayStrings");
        yield return AccessTools.Method(typeof(SimpleInfoScreen), "OnStorageCollapsibleRowExpanded");
    }

    public static void Prefix() => Scope.Enter();

    public static Exception Finalizer(Exception __exception)
    {
        Scope.Exit();
        return __exception;
    }
}

// Status lines that carry a tile's / item's mass and temperature (shown in hover cards and the info panel).
[HarmonyPatch]
public static class StatusItemScopePatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(StatusItem), nameof(StatusItem.GetName));
        yield return AccessTools.Method(typeof(StatusItem), nameof(StatusItem.GetTooltip));
    }

    private static bool IsReadoutItem(StatusItem item)
    {
        var misc = Db.Get()?.MiscStatusItems;
        return misc != null && (item == misc.ElementalMass || item == misc.ElementalTemperature
            || item == misc.OreMass || item == misc.OreTemp
            || item == misc.BackwallMass || item == misc.BackwallTemperature);
    }

    public static void Prefix(StatusItem __instance, out bool __state)
    {
        __state = IsReadoutItem(__instance);
        if (__state) Scope.Enter();
    }

    public static Exception Finalizer(bool __state, Exception __exception)
    {
        if (__state) Scope.Exit();
        return __exception;
    }
}

// The hover card caches the tile temperature string until the temperature changes; drop it when settings change.
public static class HoverCachePatch
{
    private static readonly FieldInfo CachedTemperature = AccessTools.Field(typeof(SelectToolHoverTextCard), "cachedTemperature");

    internal static void Invalidate()
    {
        var card = UnityEngine.Object.FindObjectOfType<SelectToolHoverTextCard>();
        if (card != null && CachedTemperature != null) CachedTemperature.SetValue(card, float.MinValue);
    }
}
