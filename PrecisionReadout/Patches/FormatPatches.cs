using System.Reflection;
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

// Since U60 the hover card and the info panel print masses and temperatures through the allocation-free formatters
// (KleiStringBuilder.Append<MassFormatter, ...>) instead of calling GameUtil, so the same replacement is made here. The
// shorter overloads of both formatters call the five-value one, which is the one patched.
[HarmonyPatch]
public static class MassFormatterPatch
{
    public static MethodBase TargetMethod() => AccessTools.Method(typeof(KleiFormatters.MassFormatter), "AppendValue",
        new[] { typeof(KleiStringBuilder).MakeByRefType(), typeof((float, GameUtil.TimeSlice, GameUtil.MetricMassFormat, bool, string)) });

    public static bool Prefix(ref KleiStringBuilder builder, (float mass, GameUtil.TimeSlice timeSlice, GameUtil.MetricMassFormat massFormat, bool includeSuffix, string floatFormat) args)
    {
        if (!Scope.Active || args.timeSlice != GameUtil.TimeSlice.None) return true;
        if (args.massFormat != GameUtil.MetricMassFormat.UseThreshold && args.massFormat != GameUtil.MetricMassFormat.Kilogram) return true;
        var text = Readout.Mass(args.mass, args.includeSuffix);
        if (text == null) return true;
        builder.Append(text);
        return false;
    }
}

[HarmonyPatch]
public static class TemperatureFormatterPatch
{
    public static MethodBase TargetMethod() => AccessTools.Method(typeof(KleiFormatters.TemperatureFormatter), "AppendValue",
        new[] { typeof(KleiStringBuilder).MakeByRefType(), typeof((float, GameUtil.TimeSlice, GameUtil.TemperatureInterpretation, bool, bool)) });

    public static bool Prefix(ref KleiStringBuilder builder, (float temp, GameUtil.TimeSlice timeSlice, GameUtil.TemperatureInterpretation interpretation, bool displayUnits, bool roundInDestinationFormat) args)
    {
        if (!Scope.Active || args.timeSlice != GameUtil.TimeSlice.None
            || args.interpretation != GameUtil.TemperatureInterpretation.Absolute || args.roundInDestinationFormat) return true;
        var text = Readout.Temperature(args.temp, args.displayUnits);
        if (text == null) return true;
        builder.Append(text);
        return false;
    }
}
