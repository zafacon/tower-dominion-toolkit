using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Blueprints;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Bundles;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Doctrines;
using Il2CppNvizzio.Game.Services.Blueprints;
using Il2CppNvizzio.Game.Services.Doctrines;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

/// <summary>
/// Tracks resolved random reward identities only while one native BundleDataRoot is generating
/// its cards. Native weighted selectors remain authoritative; duplicates are simply redrawn.
/// </summary>
internal static class NoDuplicateRewardScope
{
    [ThreadStatic] private static int depth;
    [ThreadStatic] private static HashSet<string>? blueprints;
    [ThreadStatic] private static HashSet<string>? doctrines;

    internal static bool Active =>
        depth > 0 &&
        ToolkitMod.SavedGameplayRules?.IsEnabled("no-duplicate-rewards") == true;

    internal static void Enter()
    {
        if (depth++ != 0) return;
        blueprints = new(StringComparer.Ordinal);
        doctrines = new(StringComparer.Ordinal);
    }

    internal static void Exit()
    {
        if (depth <= 0) return;
        if (--depth != 0) return;
        blueprints = null;
        doctrines = null;
    }

    internal static bool AcceptBlueprint(string id) => blueprints?.Add(id) ?? true;
    internal static bool AcceptDoctrine(string id) => doctrines?.Add(id) ?? true;
}

[HarmonyPatch(typeof(BundleDataRoot), "CreateCards")]
internal static class NoDuplicateRewardBundleScopePatch
{
    private static void Prefix() => NoDuplicateRewardScope.Enter();
    private static void Postfix() => NoDuplicateRewardScope.Exit();

    private static Exception? Finalizer(Exception? __exception)
    {
        // Postfix owns normal cleanup; Finalizer owns exceptional cleanup only.
        if (__exception is not null)
            NoDuplicateRewardScope.Exit();
        return __exception;
    }
}

[HarmonyPatch(typeof(BlueprintsService), nameof(BlueprintsService.GetRandomBlueprintData))]
internal static class NoDuplicateBlueprintRewardPatch
{
    [ThreadStatic] private static bool rerolling;

    [HarmonyPriority(Priority.First)]
    private static void Postfix(BlueprintsService __instance, ref BlueprintData __result)
    {
        if (rerolling || !NoDuplicateRewardScope.Active || __result == null) return;
        if (NoDuplicateRewardScope.AcceptBlueprint(__result.UniqueID)) return;

        string duplicate = __result.UniqueID;
        rerolling = true;
        try
        {
            for (int attempt = 0; attempt < 256; attempt++)
            {
                BlueprintData? candidate = __instance.GetRandomBlueprintData();
                if (candidate == null || !NoDuplicateRewardScope.AcceptBlueprint(candidate.UniqueID)) continue;
                __result = candidate;
                MelonLogger.Msg($"[Toolkit Rules] No Duplicate Rewards replaced blueprint {duplicate} with {candidate.UniqueID}.");
                return;
            }

            MelonLogger.Warning($"[Toolkit Rules] No Duplicate Rewards could not replace blueprint {duplicate} after 256 weighted redraws.");
        }
        finally { rerolling = false; }
    }
}

[HarmonyPatch(typeof(SkillService), nameof(SkillService.GetRandomDoctrineData))]
internal static class NoDuplicateDoctrineRewardPatch
{
    [ThreadStatic] private static bool rerolling;

    [HarmonyPriority(Priority.First)]
    private static void Postfix(SkillService __instance, SkillType skillRequested, ref SkillData __result)
    {
        if (rerolling || !NoDuplicateRewardScope.Active || __result == null) return;
        if (NoDuplicateRewardScope.AcceptDoctrine(__result.UniqueID)) return;

        string duplicate = __result.UniqueID;
        rerolling = true;
        try
        {
            for (int attempt = 0; attempt < 256; attempt++)
            {
                SkillData? candidate = __instance.GetRandomDoctrineData(skillRequested);
                if (candidate == null || !NoDuplicateRewardScope.AcceptDoctrine(candidate.UniqueID)) continue;
                __result = candidate;
                MelonLogger.Msg($"[Toolkit Rules] No Duplicate Rewards replaced doctrine {duplicate} with {candidate.UniqueID}.");
                return;
            }

            MelonLogger.Warning($"[Toolkit Rules] No Duplicate Rewards could not replace doctrine {duplicate} after 256 weighted redraws.");
        }
        finally { rerolling = false; }
    }
}
