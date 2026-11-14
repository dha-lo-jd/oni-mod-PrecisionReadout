using HarmonyLib;

namespace PrecisionReadout.Patches;

// The settings window is IMGUI, which the game's UI never sees, so the camera would zoom under the wheel.
// While the pointer is over the window the wheel zoom is consumed here (the window uses the wheel itself).
[HarmonyPatch(typeof(CameraController), nameof(CameraController.OnKeyDown))]
public static class CameraZoomPatch
{
    public static bool Prefix(KButtonEvent e)
    {
        if (!Ui.SettingsPanel.PointerOver) return true;
        if (e.IsAction(Action.ZoomIn) || e.IsAction(Action.ZoomOut))
        {
            e.TryConsume(Action.ZoomIn);
            e.TryConsume(Action.ZoomOut);
            return false;
        }
        return true;
    }
}
