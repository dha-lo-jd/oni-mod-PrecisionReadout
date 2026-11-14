namespace PrecisionReadout;

internal static class Readout
{
    // Mass in kg with a fixed unit; null means "leave the vanilla text alone".
    internal static string Mass(float mass, bool includeSuffix)
    {
        if (GameUtil.massUnit != GameUtil.MassUnit.Kilograms) return null;
        var digits = Settings.MassDigits;
        if (digits <= 0 || mass == float.MinValue || float.IsInfinity(mass) || float.IsNaN(mass)) return null;
        var text = ExactFormat.Fixed(mass, digits, Settings.DimUntrusted);
        return includeSuffix ? text + (string)global::STRINGS.UI.UNITSUFFIXES.MASS.KILOGRAM : text;
    }

    // Absolute temperature given in Kelvin, converted in double precision (vanilla converts in float).
    internal static string Temperature(float kelvin, bool displayUnits)
    {
        var digits = Settings.TemperatureDigits;
        if (digits <= 0 || float.IsInfinity(kelvin) || float.IsNaN(kelvin)) return null;
        double value = kelvin;
        switch (GameUtil.temperatureUnit)
        {
            case GameUtil.TemperatureUnit.Celsius: value -= 273.15; break;
            case GameUtil.TemperatureUnit.Fahrenheit: value = value * 1.8 - 459.67; break;
        }
        var text = ExactFormat.Fixed(value, digits, Settings.DimUntrusted);
        return displayUnits ? text + GameUtil.GetTemperatureUnitSuffix() : text;
    }
}
