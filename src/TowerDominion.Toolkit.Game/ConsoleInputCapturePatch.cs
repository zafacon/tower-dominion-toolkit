using HarmonyLib;
using Il2CppNvizzio.Game.Input;

namespace TowerDominion.Toolkit.Game;

// The console reads Unity input directly. Suppress only native mapped gameplay queries.
[HarmonyPatch(typeof(InputManager), nameof(InputManager.IsClicked))]
internal static class ConsoleClickedCapturePatch
{
    private static bool Prefix(ref bool __result) => ConsoleInputCapture.Allow(ref __result);
}

[HarmonyPatch(typeof(InputManager), nameof(InputManager.IsHolding))]
internal static class ConsoleHoldingCapturePatch
{
    private static bool Prefix(ref bool __result) => ConsoleInputCapture.Allow(ref __result);
}

[HarmonyPatch(typeof(InputManager), nameof(InputManager.IsHoldingAction))]
internal static class ConsoleHoldingActionCapturePatch
{
    private static bool Prefix(ref bool __result) => ConsoleInputCapture.Allow(ref __result);
}

internal static class ConsoleInputCapture
{
    internal static bool Allow(ref bool result)
    {
        if (!ToolkitConsoleOverlay.BlocksGameplayInput) return true;
        result = false;
        return false;
    }
}
