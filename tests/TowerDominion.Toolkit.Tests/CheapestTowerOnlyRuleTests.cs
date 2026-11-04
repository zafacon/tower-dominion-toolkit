using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CheapestTowerOnlyRuleTests
{
    private readonly CheapestTowerOnlyRule rule = new();

    [Fact]
    public void AllowsTiedCheapestAndBlocksMoreExpensive()
    {
        Dictionary<string, decimal> costs = new() { ["a"] = 150, ["b"] = 150, ["c"] = 200 };
        foreach (string id in new[] { "a", "b" })
            Assert.True(rule.Evaluate(new(ChallengeActionKind.PlaceTower, id, costs[id], costs)).Allowed);
        Assert.False(rule.Evaluate(new(ChallengeActionKind.PlaceTower, "c", 200, costs)).Allowed);
    }

    [Fact]
    public void UsesEffectiveCostsRatherThanBaseCostsOrAffordability()
    {
        Dictionary<string, decimal> costs = new() { ["discounted"] = 80, ["other"] = 100 };
        Assert.True(rule.Evaluate(new(ChallengeActionKind.PlaceTower, "discounted", 80, costs)).Allowed);
        Assert.False(rule.Evaluate(new(ChallengeActionKind.PlaceTower, "other", 100, costs)).Allowed);
    }

    [Fact]
    public void FailsClosedForIncompleteOrInconsistentHand()
    {
        Dictionary<string, decimal> costs = new() { ["a"] = 100 };
        Assert.False(rule.Evaluate(new(ChallengeActionKind.PlaceTower, "missing", 100, costs)).Allowed);
        Assert.False(rule.Evaluate(new(ChallengeActionKind.PlaceTower, "a", 50, costs)).Allowed);
        Assert.False(rule.Evaluate(new(ChallengeActionKind.PlaceTower, "a", 100)).Allowed);
    }

    [Fact]
    public void DoesNotRestrictOtherActions()
    {
        Assert.True(rule.Evaluate(new(ChallengeActionKind.SellBuilding)).Allowed);
        Assert.True(rule.Evaluate(new(ChallengeActionKind.Reroll)).Allowed);
    }
}
