using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.GameEntities;
using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

internal static class ChallengeBuildingRemovalGuard
{
    internal static bool Permit(ChallengeActionKind kind)
    {
        var selections = ToolkitMod.SavedChallenges;
        if (selections?.RunActive != true) return true;
        var restrictions = ChallengeRestrictions.For(selections.Active);
        if ((kind == ChallengeActionKind.SellBuilding && !restrictions.NoSelling) ||
            (kind == ChallengeActionKind.CancelBuilding && !restrictions.NoCancelling)) return true;
        return ToolkitMod.ChallengeRules?.Evaluate(new ChallengeAction(kind)).Allowed == true;
    }
}

[HarmonyPatch(typeof(BuildingEntity), nameof(BuildingEntity.Sell))]
internal static class ChainOfCommandSellPatch
{
    private static bool Prefix() => ChallengeBuildingRemovalGuard.Permit(ChallengeActionKind.SellBuilding);
}

[HarmonyPatch(typeof(BuildingEntity), nameof(BuildingEntity.Cancel))]
internal static class ChainOfCommandCancelPatch
{
    private static bool Prefix() => ChallengeBuildingRemovalGuard.Permit(ChallengeActionKind.CancelBuilding);
}

/// <summary>Mirror the enforced Chain of Command restriction in the native building panel.</summary>
[HarmonyPatch(typeof(Il2CppNvizzio.Game.UI.Views.BuildingView), "UpdateFooterSection")]
internal static class ChainOfCommandRemovalButtonVisualPatch
{
    private static void Postfix(Il2CppNvizzio.Game.UI.Views.BuildingView __instance)
    {
        if (ChallengeBuildingRemovalGuard.Permit(ChallengeActionKind.SellBuilding) &&
            ChallengeBuildingRemovalGuard.Permit(ChallengeActionKind.CancelBuilding)) return;
        if (__instance.sellButton != null) __instance.sellButton.interactable = false;
        if (__instance.cancelButton != null) __instance.cancelButton.interactable = false;
    }
}
