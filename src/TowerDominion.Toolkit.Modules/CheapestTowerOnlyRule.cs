using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Allows placement only for towers tied at the minimum effective hand cost.</summary>
public sealed class CheapestTowerOnlyRule : IChallengeRule
{
    public const string Id = "cheapest-tower-only";

    public ChallengeDescriptor Descriptor { get; } = new(
        Id, "Cheapest Tower Only",
        "Only towers tied for the lowest effective cost in hand may be placed.",
        new HashSet<ChallengeActionKind> { ChallengeActionKind.PlaceTower },
        Author: "DiVerrencia",
        KnownIncompatibilities: new[] { ForcedDeploymentRule.Id });

    public ChallengeDecision Evaluate(ChallengeAction action)
    {
        if (action.Kind != ChallengeActionKind.PlaceTower) return ChallengeDecision.Permit();
        if (string.IsNullOrWhiteSpace(action.EntityId) || action.EffectiveCost is null ||
            action.AvailableTowerCosts is null || action.AvailableTowerCosts.Count == 0 ||
            !action.AvailableTowerCosts.TryGetValue(action.EntityId, out decimal handCost) ||
            handCost != action.EffectiveCost.Value)
            return ChallengeDecision.Block("Tower hand or effective cost is unavailable.");

        decimal cheapest = action.AvailableTowerCosts.Values.Min();
        return action.EffectiveCost.Value == cheapest
            ? ChallengeDecision.Permit()
            : ChallengeDecision.Block($"Only towers costing {cheapest} may be placed.");
    }
}
