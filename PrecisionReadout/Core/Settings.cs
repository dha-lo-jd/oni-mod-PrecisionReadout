using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace PrecisionReadout;

// User settings: decimal places shown for mass (kg) and temperature, 0 = vanilla formatting.
internal static class Settings
{
    private sealed class Data
    {
        public int massDigits;
        public int temperatureDigits;
        public bool dimUntrusted = true;
    }

    internal const int MaxDigits = 30;

    private static Data data = new Data();

    internal static int MassDigits
    {
        get => data.massDigits;
        set { data.massDigits = Clamp(value); Changed(); }
    }

    internal static int TemperatureDigits
    {
        get => data.temperatureDigits;
        set { data.temperatureDigits = Clamp(value); Changed(); }
    }

    internal static bool DimUntrusted
    {
        get => data.dimUntrusted;
        set { data.dimUntrusted = value; Changed(); }
    }

    private static int Clamp(int v) => Math.Max(0, Math.Min(MaxDigits, v));

    private static string FilePath => Path.Combine(Util.RootFolder(), "PrecisionReadout.json");

    internal static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(FilePath)) ?? new Data();
                data.massDigits = Clamp(data.massDigits);
                data.temperatureDigits = Clamp(data.temperatureDigits);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[PrecisionReadout] Could not read settings: " + e.Message);
            data = new Data();
        }
    }

    private static void Changed()
    {
        try { File.WriteAllText(FilePath, JsonConvert.SerializeObject(data, Formatting.Indented)); }
        catch (Exception e) { Debug.LogWarning("[PrecisionReadout] Could not save settings: " + e.Message); }
        Patches.HoverCachePatch.Invalidate();
    }
}
