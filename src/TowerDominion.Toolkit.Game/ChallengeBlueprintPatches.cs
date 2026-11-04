using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.Blueprints;
using MelonLoader;
using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

/// <summary>Read only the current native hand, using each blueprint's effective cost.</summary>
internal static class ChallengeBlueprintAdapter
{
    internal static bool IsAllowed(BlueprintHand hand, BlueprintUI candidate)
    {
        ChallengeEnforcement? enforcement = ToolkitMod.ChallengeRules;
        ChallengeSelections? selections = ToolkitMod.SavedChallenges;
        if (enforcement == null || selections?.RunActive != true) return true;
        bool cheapest = selections.Active.Contains(CheapestTowerOnlyRule.Id, StringComparer.OrdinalIgnoreCase);
        bool forced = selections.Active.Contains(ForcedDeploymentRule.Id, StringComparer.OrdinalIgnoreCase);
        if (!cheapest && !forced) return true;

        try
        {
            var blueprints = hand.Blueprints;
            if (blueprints == null || candidate?.Data == null) return false;
            var costs = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var order = new List<(int InstanceId, float ScreenX, int Count)>();
            foreach (var pair in blueprints)
            {
                if (pair.Key == null || pair.Value == null || pair.Value.Count <= 0) continue;
                string id = pair.Key.UniqueID;
                if (string.IsNullOrWhiteSpace(id)) continue;
                costs[id] = pair.Value.Cost;
                if (forced)
                    order.Add((pair.Value.GetInstanceID(), pair.Value.transform.position.x, pair.Value.Count));
            }
            string candidateId = candidate.Data.UniqueID;
            if (forced && ForcedDeploymentOrder.LeftmostInstance(order) != candidate.GetInstanceID())
                return false;
            return !cheapest || enforcement.Evaluate(new ChallengeAction(
                ChallengeActionKind.PlaceTower, candidateId, candidate.Cost, costs)).Allowed;
        }
        catch (Exception exception)
        {
            MelonLogger.Warning($"[Toolkit Challenges] Blueprint evaluation failed: {exception.Message}");
            return false;
        }
    }
}

[HarmonyPatch(typeof(BlueprintUI), nameof(BlueprintUI.SetToggleInteractable))]
internal static class ChallengeBlueprintTogglePatch
{
    private static void Prefix(BlueprintUI __instance, ref bool isPurchasable)
    {
        if (!isPurchasable) return;
        var hand = BlueprintHand.Instance;
        if (hand != null && !ChallengeBlueprintAdapter.IsAllowed(hand, __instance)) isPurchasable = false;
    }
}

[HarmonyPatch(typeof(BlueprintHand), nameof(BlueprintHand.OnToggleValueChange))]
internal static class ChallengeBlueprintSelectionPatch
{
    private static bool Prefix(BlueprintHand __instance, BlueprintUI blueprint, bool isSelected)
    {
        if (!isSelected || ChallengeBlueprintAdapter.IsAllowed(__instance, blueprint)) return true;
        blueprint.SetToggleSelected(false);
        return false;
    }
}
