using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Blueprints;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Doctrines;
using Il2CppNvizzio.Game.Services.Blueprints;
using Il2CppNvizzio.Game.Services.Doctrines;
using MelonLoader;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

/// <summary>Union manual and challenge-owned bans without changing persisted manual selections.</summary>
internal static class BlueprintBanGuard
{
    internal static bool IsBanned(string id)
    {
        if (ToolkitMod.SavedBans?.IsBanned("blueprints", id) == true) return true;
        var selections = ToolkitMod.SavedChallenges;
        if (selections == null) return false;
        var ids = selections.RunActive ? selections.Active : selections.Selected;
        return ChallengeRestrictions.For(ids, ToolkitMod.ChallengeCatalog).IsBanned("blueprints", id);
    }
}

/// <summary>Reject banned random blueprint rewards without recursively entering the postfix.</summary>
[HarmonyPatch(typeof(BlueprintsService), nameof(BlueprintsService.GetRandomBlueprintData))]
internal static class BlueprintBanPatch
{
    [ThreadStatic] private static bool rerolling;

    private static void Postfix(BlueprintsService __instance, ref BlueprintData __result)
    {
        if (rerolling || __result == null || !BlueprintBanGuard.IsBanned(__result.UniqueID)) return;
        string rejected = __result.UniqueID;
        rerolling = true;
        try
        {
            for (int attempt = 0; attempt < 256; attempt++)
            {
                BlueprintData? candidate = __instance.GetRandomBlueprintData();
                if (candidate == null || BlueprintBanGuard.IsBanned(candidate.UniqueID)) continue;
                __result = candidate;
                MelonLogger.Msg($"[Toolkit Bans] Replaced blueprint {rejected} with {candidate.UniqueID}.");
                return;
            }
            MelonLogger.Warning($"[Toolkit Bans] Could not replace banned blueprint {rejected} after 256 attempts.");
        }
        finally { rerolling = false; }
    }
}

/// <summary>Preserve the requested doctrine skill type while rerolling banned rewards.</summary>
[HarmonyPatch(typeof(SkillService), nameof(SkillService.GetRandomDoctrineData))]
internal static class DoctrineBanPatch
{
    [ThreadStatic] private static bool rerolling;

    private static void Postfix(SkillService __instance, SkillType skillRequested, ref SkillData __result)
    {
        var bans = ToolkitMod.SavedBans;
        if (rerolling || bans == null || __result == null || !bans.IsBanned("doctrines", __result.UniqueID)) return;
        string rejected = __result.UniqueID;
        rerolling = true;
        try
        {
            for (int attempt = 0; attempt < 256; attempt++)
            {
                SkillData? candidate = __instance.GetRandomDoctrineData(skillRequested);
                if (candidate == null || bans.IsBanned("doctrines", candidate.UniqueID)) continue;
                __result = candidate;
                MelonLogger.Msg($"[Toolkit Bans] Replaced doctrine {rejected} with {candidate.UniqueID}.");
                return;
            }
            MelonLogger.Warning($"[Toolkit Bans] Could not replace banned doctrine {rejected} after 256 attempts.");
        }
        finally { rerolling = false; }
    }
}
