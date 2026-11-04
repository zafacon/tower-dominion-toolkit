using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Bundles;
using Il2CppNvizzio.Game.UI.Views;
using MelonLoader;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

/// <summary>Only regular wave-end reward generation is affected; special rewards use a separate view.</summary>
internal static class EightBallDraftScope
{
    [ThreadStatic] internal static bool InRegularDraft;
    internal static bool Enabled => InRegularDraft && ToolkitMod.SavedChallenges is { RunActive: true } selections &&
        selections.Active.Contains(EightBallRule.Id, StringComparer.OrdinalIgnoreCase);
}

[HarmonyPatch(typeof(WaveEndView), "CreateContent")]
internal static class EightBallRegularDraftScopePatch
{
    private static void Prefix() => EightBallDraftScope.InRegularDraft = true;
    private static void Postfix() => EightBallDraftScope.InRegularDraft = false;
    private static Exception? Finalizer(Exception? __exception)
    {
        EightBallDraftScope.InRegularDraft = false;
        return __exception;
    }
}

[HarmonyPatch(typeof(BundleDataRoot), "CreateCards")]
internal static class EightBallCardCountPatch
{
    private static void Postfix(Il2CppSystem.Collections.Generic.List<BundleCard> __result)
    {
        if (!EightBallDraftScope.Enabled || __result == null || __result.Count <= 1) return;
        int removed = __result.Count - 1;
        while (__result.Count > 1) __result.RemoveAt(__result.Count - 1);
        MelonLogger.Msg($"[Toolkit 8-Ball] Regular reinforcement draft reduced by {removed} card(s).");
    }
}
