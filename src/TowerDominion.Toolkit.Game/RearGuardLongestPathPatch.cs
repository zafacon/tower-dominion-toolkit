using HarmonyLib;
using Il2CppNvizzio.Core.Singleton;
using Il2CppNvizzio.Game.GamePlay.Paths;
using Il2CppNvizzio.Game.GamePlay.Threats;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

// Native RangeOnLongPath dispatches here; ordinary and other spawn modes are untouched.
[HarmonyPatch(typeof(EnemySwarm), nameof(EnemySwarm.GetNextLongPathIndex))]
internal static class RearGuardLongestPathPatch
{
    private static bool Prefix(EnemySwarm __instance, ref int __result)
    {
        if (ToolkitMod.SavedGameplayRules?.IsEnabled("rear-guard-longest-path") != true) return true;
        try
        {
            var paths = MonoSingleton<PathsManager>.Instance?.Paths;
            var data = __instance.Data;
            if (paths == null || data == null) return true;
            int best = -1, length = -1;
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                if (path == null || path.IsRemoved || path.Tiles == null ||
                    !data.IsSupportedNestZoneType(path.nestZoneName)) continue;
                // Prefer ordinary Zone routes, matching native longest-path policy.
                if (path.nestZoneName != "Zone") continue;
                int count = path.Tiles.Count;
                if (count <= length) continue;
                best = i;
                length = count;
            }
            if (best < 0) return true;
            __result = best;
            return false;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[Toolkit Rear Guard] Native fallback: {ex.Message}");
            return true;
        }
    }
}
