using HarmonyLib;
using Il2CppNvizzio.Game.Services.Threats;

namespace TowerDominion.Toolkit.Game;

/// <summary>
/// Make every active round enter the native zone-placement branch. MainLoopContext
/// independently checks IsNestZonePlaceRound, so native nest rounds remain exactly one
/// nest placement rather than receiving an additional ordinary zone.
/// </summary>
[HarmonyPatch(typeof(GameRoundService), nameof(GameRoundService.IsZonePlaceRound), MethodType.Getter)]
internal static class AbundantTilesPatch
{
    private static void Postfix(ref bool __result)
    {
        if (__result) return;
        if (ToolkitMod.SavedGameplayRules?.IsEnabled("abundant-tiles") == true)
            __result = true;
    }
}
