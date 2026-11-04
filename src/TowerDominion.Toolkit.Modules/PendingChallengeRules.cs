using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Tech resource cap is enforced by the native resource adapter.</summary>
public sealed class LowTechRule : IChallengeRule
{
    public const string Id = "low-tech";
    public ChallengeDescriptor Descriptor { get; } = new(Id, "Low Tech",
        "Tech is capped at 0.", new HashSet<ChallengeActionKind>(), Author: "cookiebat");
    public ChallengeDecision Evaluate(ChallengeAction action) => ChallengeDecision.Permit();
}

/// <summary>Native hand adapter must supply the leftmost blueprint's current identity.</summary>
public sealed class ForcedDeploymentRule : IChallengeRule
{
    public const string Id = "forced-deployment";
    public ChallengeDescriptor Descriptor { get; } = new(Id, "Forced Deployment",
        "Only the leftmost available blueprint may be placed.",
        new HashSet<ChallengeActionKind> { ChallengeActionKind.PlaceTower, ChallengeActionKind.SellBuilding, ChallengeActionKind.CancelBuilding },
        Author: "darksoulsgreat6205",
        KnownIncompatibilities: new[] { CheapestTowerOnlyRule.Id });

    public ChallengeDecision Evaluate(ChallengeAction action) => action.Kind switch
    {
        ChallengeActionKind.SellBuilding or ChallengeActionKind.CancelBuilding =>
            ChallengeDecision.Block("Forced Deployment prevents selling or cancelling buildings."),
        ChallengeActionKind.PlaceTower when string.IsNullOrWhiteSpace(action.EntityId) ||
            string.IsNullOrWhiteSpace(action.LeftmostBlueprintId) =>
            ChallengeDecision.Block("Current blueprint hand order is unavailable."),
        ChallengeActionKind.PlaceTower when !string.Equals(action.EntityId, action.LeftmostBlueprintId, StringComparison.OrdinalIgnoreCase) =>
            ChallengeDecision.Block("Only the leftmost available blueprint may be placed."),
        _ => ChallengeDecision.Permit()
    };
}

/// <summary>Choose the current leftmost available native hand instance, not a blueprint type.</summary>
public static class ForcedDeploymentOrder
{
    public static int? LeftmostInstance(IEnumerable<(int InstanceId, float ScreenX, int Count)> hand)
    {
        var available = hand.Where(item => item.Count > 0).OrderBy(item => item.ScreenX).ToArray();
        return available.Length == 0 ? null : available[0].InstanceId;
    }
}
