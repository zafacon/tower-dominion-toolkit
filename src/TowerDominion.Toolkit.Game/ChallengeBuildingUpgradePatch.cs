using HarmonyLib;
using Il2CppNvizzio.Game.UI.Views;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Buildings;
using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

/// <summary>Native UI indexes: first upgrade button = 0, second = 1.
/// Enforce at purchase entry, not merely at interactability presentation.</summary>
[HarmonyPatch(typeof(BuildingView), nameof(BuildingView.OnClickUpgradeButton))]
internal static class LeftTurnOnlyUpgradePatch
{
    private static bool Prefix(BuildingUpgradeItemData upgradeData, int levelIndex, int itemIndex)
    {
        var selections = ToolkitMod.SavedChallenges;
        if (selections?.RunActive != true ||
            !selections.Active.Contains(LeftTurnOnlyRule.Id, StringComparer.OrdinalIgnoreCase)) return true;
        try
        {
            if (upgradeData == null || levelIndex < 0 || itemIndex is < 0 or > 1) return false;
            return ToolkitMod.ChallengeRules?.Evaluate(new ChallengeAction(
                ChallengeActionKind.UpgradeBuilding, itemIndex == 0 ? "left" : "right")).Allowed == true;
        }
        catch (Exception exception)
        {
            MelonLogger.Warning($"[Toolkit Challenges] Upgrade guard failed: {exception.Message}");
            return false;
        }
    }
}
