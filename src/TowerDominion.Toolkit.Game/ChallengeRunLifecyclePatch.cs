using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.Contexts;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

/// <summary>Lock the selected challenges only when the blueprint hand becomes active for gameplay.</summary>
[HarmonyPatch(typeof(Il2CppNvizzio.Game.GamePlay.Blueprints.BlueprintHand), nameof(Il2CppNvizzio.Game.GamePlay.Blueprints.BlueprintHand.SetActive))]
internal static class ChallengeRunStartPatch
{
    private static void Prefix(bool isActive)
    {
        if (!isActive) return;
        var selections = ToolkitMod.SavedChallenges;
        if (selections == null || selections.RunActive) return;
        ChainOfCommandModalVisibility.Reset();
        selections.StartRun();
        MelonLogger.Msg($"[Toolkit Challenges] Run snapshot: {string.Join(", ", selections.Active)}");
    }
}

/// <summary>Release the snapshot when the native gameplay context stops.</summary>
[HarmonyPatch(typeof(MainLoopContext), nameof(MainLoopContext.Stop))]
internal static class ChallengeRunStopPatch
{
    private static void Postfix()
    {
        var selections = ToolkitMod.SavedChallenges;
        if (selections?.RunActive != true) return;
        ChainOfCommandModalVisibility.Reset();
        selections.EndRun();
        MelonLogger.Msg("[Toolkit Challenges] Run snapshot released.");
    }
}
