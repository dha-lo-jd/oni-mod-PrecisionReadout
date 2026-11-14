using HarmonyLib;

namespace PrecisionReadout.Patches;

[HarmonyPatch(typeof(GameUtil), nameof(GameUtil.GetFormattedMass))]
public static class FormattedMassPatch
{
    public static bool Prefix(float mass, GameUtil.TimeSlice timeSlice, GameUtil.MetricMassFormat massFormat, bool includeSuffix, ref string __result)
    {
        if (!Scope.Active || timeSlice != GameUtil.TimeSlice.None) return true;
        if (massFormat != GameUtil.MetricMassFormat.UseThreshold && massFormat != GameUtil.MetricMassFormat.Kilogram) return true;
        var text = Readout.Mass(mass, includeSuffix);
        if (text == null) return true;
        __result = text;
        return false;
    }
}

[HarmonyPatch(typeof(GameUtil), nameof(GameUtil.GetFormattedTemperature))]
public static class FormattedTemperaturePatch
{
    public static bool Prefix(float temp, GameUtil.TimeSlice timeSlice, GameUtil.TemperatureInterpretation interpretation, bool displayUnits, bool roundInDestinationFormat, ref string __result)
    {
        if (!Scope.Active || timeSlice != GameUtil.TimeSlice.None
            || interpretation != GameUtil.TemperatureInterpretation.Absolute || roundInDestinationFormat) return true;
        var text = Readout.Temperature(temp, displayUnits);
        if (text == null) return true;
        __result = text;
        return false;
    }
}
