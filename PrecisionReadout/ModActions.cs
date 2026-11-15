using System.Reflection;
using HarmonyLib;
using PeterHan.PLib.Actions;

namespace PrecisionReadout;

// The settings window's hotkey, registered through PLib so it shows up in the game's key bindings screen
// (group "PLib") and can be rebound there. The game's own screen warns about duplicates and unbinds the other action.
internal static class ModActions
{
    private static PAction toggle;

    // The LocString object PLib holds as the action title. A translation replaces the STRINGS field with a new object, so
    // this old one would keep the English text. Typed as object on purpose: the game scans every class in this namespace
    // for LocString fields when it registers the strings, and would trip over a LocString field that is still null.
    private static object toggleTitle;

    private static readonly FieldInfo TextField = AccessTools.Field(typeof(LocString), "_text");

    internal static Action ToggleAction => toggle.GetKAction();
    private static bool wasDown;

    internal static void Register()
    {
        toggleTitle = STRINGS.UI.TOGGLE_ACTION;
        toggle = new PActionManager().CreateAction("PrecisionReadout.Toggle", STRINGS.UI.TOGGLE_ACTION,
            new PKeyBinding(KKeyCode.F7, Modifier.Ctrl));
    }

    // Copies the translated action title into what PLib shows on the key bindings screen.
    internal static void RefreshTitle()
    {
        var held = toggleTitle as LocString;
        var current = STRINGS.UI.TOGGLE_ACTION;
        if (held == null || current == null || toggle == null) return;
        if (!ReferenceEquals(held, current) && TextField != null) TextField.SetValue(held, current.text);
        Strings.Add("STRINGS.INPUT_BINDINGS.PLIB." + toggle.GetKAction().ToString().ToUpperInvariant(), current.text);
    }

    // True on the frame the bound key goes down. Polled from the always-active host, like the other PLib hotkey mods.
    internal static bool TogglePressed()
    {
        var controller = Controller();
        if (toggle == null || controller == null) return false;
        var down = controller.IsActive(toggle.GetKAction());
        var pressed = down && !wasDown;
        wasDown = down;
        return pressed;
    }

    private static KInputController Controller() => Global.GetInputManager()?.GetDefaultController();

    // The binding currently in effect, as shown in the key bindings screen ("Ctrl + F7").
    internal static string KeyName()
    {
        if (toggle == null) return "Ctrl+F7";
        var entry = GameInputMapping.FindEntry(toggle.GetKAction());
        var key = GameUtil.GetKeycodeLocalized(entry.mKeyCode);
        var name = "";
        if ((entry.mModifier & Modifier.Ctrl) != 0) name += "Ctrl+";
        if ((entry.mModifier & Modifier.Alt) != 0) name += "Alt+";
        if ((entry.mModifier & Modifier.Shift) != 0) name += "Shift+";
        return name + key;
    }
}
