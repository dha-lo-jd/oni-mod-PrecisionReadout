#if DEBUG
// In-game tests run by Gravitas.TestHarness:
//   python Gravitas.TestProbe/probe.py run --project <this repo> --tests '{"tags":["precisionreadout"]}'
// Needs HarnessDllPath in Local.Build.props (Check / Wait come from the harness assembly).
using System;
using System.Linq;
using Gravitas.TestHarness.Core;
using Gravitas.TestHarness;
using HarmonyLib;

namespace PrecisionReadout;

internal static class HarnessTests
{
    [HarnessTest(Name = "precisionreadout.mod_loaded", Tags = "precisionreadout", RequiresGame = false)]
    public static void ModLoaded()
    {
        Check.True(PrecisionReadoutMod.modPath != null, "PrecisionReadoutMod.OnLoad has not run");
    }

    // UserMod2.OnLoad applies the patches; a second PatchAll would run every patch twice.
    [HarnessTest(Name = "precisionreadout.patches_applied_once", Tags = "precisionreadout", RequiresGame = false)]
    public static void PatchesAppliedOnce()
    {
        CheckOnce(AccessTools.Method(typeof(GameUtil), nameof(GameUtil.GetFormattedMass)), typeof(Patches.FormattedMassPatch), prefix: true);
        CheckOnce(AccessTools.Method(typeof(GameUtil), nameof(GameUtil.GetFormattedTemperature)), typeof(Patches.FormattedTemperaturePatch), prefix: true);
        CheckOnce(AccessTools.Method(typeof(SelectToolHoverTextCard), nameof(SelectToolHoverTextCard.UpdateHoverElements)), typeof(Patches.SurfaceScopePatch), prefix: true);
        CheckOnce(AccessTools.Method(typeof(StatusItem), nameof(StatusItem.GetName)), typeof(Patches.StatusItemScopePatch), prefix: true);
    }

    private static void CheckOnce(System.Reflection.MethodBase target, Type patch, bool prefix)
    {
        Check.NotNull(target, "patch target for " + patch.Name + " not found");
        var info = Harmony.GetPatchInfo(target);
        Check.NotNull(info, target.Name + " is not patched");
        var patches = prefix ? info.Prefixes : info.Postfixes;
        Check.Equal(1, patches.Count(p => p.PatchMethod.DeclaringType == patch), patch.Name + " on " + target.Name);
    }

    // The hotkey is a PLib action: it has a default binding (Ctrl+F7), shows up as a bindable entry, and no other
    // binding in this session (vanilla or any loaded mod) uses the same key with the same modifiers.
    [HarnessTest(Name = "precisionreadout.hotkey_registered", Tags = "precisionreadout")]
    public static void HotkeyRegistered()
    {
        var entry = GameInputMapping.FindEntry(ModActions.ToggleAction);
        Check.True(entry.mAction == ModActions.ToggleAction, "the toggle action has no binding entry");
        Check.True(entry.mRebindable, "the binding is not rebindable");
        Check.Equal(KKeyCode.F7, entry.mKeyCode, "default key");
        Check.Equal(Modifier.Ctrl, entry.mModifier, "default modifier");
        Check.Equal("Ctrl+F7", ModActions.KeyName(), "key name shown in the window");

        var clashes = GameInputMapping.KeyBindings
            .Where(b => b.mAction != ModActions.ToggleAction && b.mKeyCode == entry.mKeyCode && b.mModifier == entry.mModifier)
            .Select(b => b.mAction.ToString()).ToList();
        Check.True(clashes.Count == 0, "same key as: " + string.Join(", ", clashes));
    }

    // Pressing the bound action opens the window, pressing it again closes it.
    [HarnessTest(Name = "precisionreadout.hotkey_toggles_window", Tags = "precisionreadout", TimeoutSeconds = 30)]
    public static System.Collections.IEnumerator HotkeyTogglesWindow()
    {
        var panel = UnityEngine.Object.FindObjectOfType<Ui.SettingsPanel>();
        Check.NotNull(panel, "settings panel host");
        Check.True(!panel.IsOpen, "window should start closed");
        // The window polls the key state every frame, so hold the key across a few frames like a player does
        // (Play.Press goes down and up within one input update, which no frame could see).
        var (key, modifier) = Play.KeyFor(ModActions.ToggleAction);
        Play.KeyDown(key, modifier);
        for (var i = 0; i < 5; i++) yield return null;
        Play.KeyUp(key, modifier);
        for (var i = 0; i < 5; i++) yield return null;
        Check.True(panel.IsOpen, "window did not open");
        Play.KeyDown(key, modifier);
        for (var i = 0; i < 5; i++) yield return null;
        Play.KeyUp(key, modifier);
        for (var i = 0; i < 5; i++) yield return null;
        Check.True(!panel.IsOpen, "window did not close");
    }

    // The exact expansion shows the value the float really holds, not a 15-digit zero padding.
    [HarnessTest(Name = "precisionreadout.mass_digits", Tags = "precisionreadout", RequiresGame = false)]
    public static void MassDigits()
    {
        WithSettings(10, 0, () =>
        {
            Scope.Enter();
            try
            {
                Check.Equal("0.1000000015" + (string)STRINGS_KG(), GameUtil.GetFormattedMass(0.1f), "0.1f at 10 digits");
                Check.Equal("1234.5677490234" + (string)STRINGS_KG(), GameUtil.GetFormattedMass(1234.5678f), "1234.5678f at 10 digits");
            }
            finally { Scope.Exit(); }
        });
    }

    // Outside a scope (status bar, tooltips ...) the vanilla text stays.
    [HarnessTest(Name = "precisionreadout.vanilla_outside_scope", Tags = "precisionreadout", RequiresGame = false)]
    public static void VanillaOutsideScope()
    {
        WithSettings(10, 10, () =>
        {
            Check.True(!Scope.Active, "scope leaked");
            var mass = GameUtil.GetFormattedMass(0.1f);
            Check.True(!mass.Contains("0.1000000015"), "mass outside the scope was changed: " + mass);
            var temp = GameUtil.GetFormattedTemperature(300.15f);
            Check.True(!temp.Contains("26.9999938965") && !temp.Contains("300.1499938965"), "temperature outside the scope was changed: " + temp);
        });
    }

    // Temperatures convert in double: 300.15f is 300.1499938964844 K, i.e. 26.99999389648438 C.
    [HarnessTest(Name = "precisionreadout.temperature_digits", Tags = "precisionreadout", RequiresGame = false)]
    public static void TemperatureDigits()
    {
        var unit = GameUtil.temperatureUnit;
        try
        {
            WithSettings(0, 10, () =>
            {
                Scope.Enter();
                try
                {
                    GameUtil.temperatureUnit = GameUtil.TemperatureUnit.Celsius;
                    Check.Equal("26.9999938965" + GameUtil.GetTemperatureUnitSuffix(), GameUtil.GetFormattedTemperature(300.15f), "Celsius");
                    GameUtil.temperatureUnit = GameUtil.TemperatureUnit.Kelvin;
                    Check.Equal("300.1499938965" + GameUtil.GetTemperatureUnitSuffix(), GameUtil.GetFormattedTemperature(300.15f), "Kelvin");
                }
                finally { Scope.Exit(); }
            });
        }
        finally { GameUtil.temperatureUnit = unit; }
    }

    // A thrown exception inside a scoped method must not leave the scope open.
    [HarnessTest(Name = "precisionreadout.scope_closed_after_calls", Tags = "precisionreadout")]
    public static void ScopeClosed()
    {
        var misc = Db.Get().MiscStatusItems;
        misc.ElementalMass.GetName(null);
        Check.True(!Scope.Active, "scope still open after StatusItem.GetName");
        Check.NoLogErrors();
    }

    // Not a check: takes the Workshop thumbnail source shots (hover card at 10 digits, with and without the settings window).
    //   probe.py run --project <this repo> --tests '{"tags":["precisionreadout-shot"]}'
    // The shots land in the run's screenshots folder.
    [HarnessTest(Name = "precisionreadout.shoot_thumbnail", Tags = "precisionreadout-shot", TimeoutSeconds = 120)]
    public static System.Collections.IEnumerator ShootThumbnail()
    {
        // The test save has no suitable open water, so make a tile with an uneven mass right at the base.
        // Away from the duplicants, so their hover card does not cover the tile's card.
        var (hx, hy) = Arrange.XY(Arrange.HomeCell());
        var cell = Arrange.Cell(hx + 9, hy);
        Arrange.SetElement(cell, SimHashes.Water, 1234.5678f, 300.15f);
        yield return Wait.Ticks(5);

        var oldMass = Settings.MassDigits;
        var oldTemperature = Settings.TemperatureDigits;
        var oldDim = Settings.DimUntrusted;
        var panel = UnityEngine.Object.FindObjectOfType<Ui.SettingsPanel>();
        var openField = AccessTools.Field(typeof(Ui.SettingsPanel), "open");
        try
        {
            Settings.DimUntrusted = true;
            Settings.MassDigits = 10;
            Settings.TemperatureDigits = 10;
            Play.Hover(cell);
            for (var i = 0; i < 30; i++) yield return null;
            Screenshot.Capture("hover-card");
            for (var i = 0; i < 30; i++) yield return null;

            if (panel != null && openField != null) openField.SetValue(panel, true);
            for (var i = 0; i < 30; i++) yield return null;
            Screenshot.Capture("hover-card-with-panel");
            for (var i = 0; i < 30; i++) yield return null;
        }
        finally
        {
            if (panel != null && openField != null) openField.SetValue(panel, false);
            Play.ReleaseMouse();
            Settings.MassDigits = oldMass;
            Settings.TemperatureDigits = oldTemperature;
            Settings.DimUntrusted = oldDim;
        }
    }

    private static string STRINGS_KG() => (string)global::STRINGS.UI.UNITSUFFIXES.MASS.KILOGRAM;

    // The setters save to the user's real settings file, so put the old values back afterwards.
    private static void WithSettings(int mass, int temperature, System.Action body)
    {
        var oldMass = Settings.MassDigits;
        var oldTemperature = Settings.TemperatureDigits;
        var oldDim = Settings.DimUntrusted;
        try
        {
            Settings.DimUntrusted = false;
            Settings.MassDigits = mass;
            Settings.TemperatureDigits = temperature;
            body();
        }
        finally
        {
            Settings.MassDigits = oldMass;
            Settings.TemperatureDigits = oldTemperature;
            Settings.DimUntrusted = oldDim;
        }
    }
}
#endif
