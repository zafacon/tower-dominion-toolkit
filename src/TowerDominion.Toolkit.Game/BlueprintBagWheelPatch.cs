using HarmonyLib;
using Il2CppNvizzio.Game.Camera;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

// Suppress only the camera wheel contribution, preserving native zoom interpolation and existing freeze state.
[HarmonyPatch(typeof(CameraMovement), nameof(CameraMovement.ZoomManagement))]
internal static class BlueprintBagWheelPatch
{
    private static bool warned;
    private static void Prefix(CameraMovement __instance, out bool? __state)
    {
        __state = null;
        try
        {
            if (!BlueprintBagPanel.CapturesWheel()) return;
            __state = __instance.isMouseWheelFrozen;
            __instance.isMouseWheelFrozen = true;
        }
        catch (Exception ex)
        {
            if (!warned) { warned = true; MelonLogger.Warning($"[Blueprint Bag] Wheel capture unavailable: {ex.Message}"); }
        }
    }

    private static Exception? Finalizer(CameraMovement __instance, bool? __state, Exception? __exception)
    {
        if (__state.HasValue) __instance.isMouseWheelFrozen = __state.Value;
        return __exception;
    }
}
