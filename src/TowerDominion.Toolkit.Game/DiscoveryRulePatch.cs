using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.Zones;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

// Override the generation decision without writing native Discovery fields.
// The original roll and its probability bookkeeping are skipped while enabled.
[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.IsNeutralBuildingToPlace))]
internal static class DiscoveryRulePatch
{
    private static bool Prefix(ZoneManager __instance, ref bool __result)
    {
        var rules = ToolkitMod.SavedGameplayRules;
        bool full = rules?.IsEnabled("full-discovery") == true;
        if (!full && rules?.IsEnabled("no-discovery") != true) return true;
        try
        {
            __result = full;
            return false;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[Toolkit Discovery] Native fallback: {ex.Message}");
            return true;
        }
    }
}

// Limit the eligibility override to the synchronous execution of the neutral placement coroutine.
[HarmonyPatch(typeof(Il2CppNvizzio.Game.GamePlay.Contexts.PlaceTheNewZoneContext._SpawnNeutralBuilding_d__18), "MoveNext")]
internal static class DiscoveryNeutralSelectionScope
{
    [ThreadStatic] internal static int Depth;
    private static void Prefix() => Depth++;
    private static Exception? Finalizer(Exception? __exception)
    {
        Depth--;
        return __exception;
    }
}

[HarmonyPatch(typeof(Il2CppNvizzio.Game.Services.UnlockablesService), "IsBuildingUnlocked")]
internal static class DiscoveryNeutralEligibilityPatch
{
    private static void Postfix(string buildingName, ref bool __result)
    {
        if (__result && DiscoveryNeutralSelectionScope.Depth > 0 &&
            TowerDominion.Toolkit.Modules.DiscoveryRestrictions.IsNeutralExcluded(ToolkitMod.SavedGameplayRules, buildingName))
        {
            __result = false;
        }
    }
}
