using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class PendingChallengeRuleTests
{
    [Fact]
    public void ForcedDeploymentBlocksOtherBlueprintsAndRemoval()
    {
        var rule = new ForcedDeploymentRule();
        Assert.True(rule.Evaluate(new(ChallengeActionKind.PlaceTower, "first", LeftmostBlueprintId: "first")).Allowed);
        Assert.False(rule.Evaluate(new(ChallengeActionKind.PlaceTower, "second", LeftmostBlueprintId: "first")).Allowed);
        Assert.False(rule.Evaluate(new(ChallengeActionKind.PlaceTower, "first")).Allowed);
        Assert.False(rule.Evaluate(new(ChallengeActionKind.SellBuilding)).Allowed);
        Assert.False(rule.Evaluate(new(ChallengeActionKind.CancelBuilding)).Allowed);
        Assert.True(rule.Evaluate(new(ChallengeActionKind.UpgradeBuilding)).Allowed);
    }

    [Fact]
    public void LowTechDeclaresCapWithoutUnrelatedActionVetoes()
    {
        var rule = new LowTechRule();
        Assert.Contains("0", rule.Descriptor.Description);
        Assert.True(rule.Evaluate(new(ChallengeActionKind.UpgradeBuilding)).Allowed);
    }
    [Fact]
    public void NativeHandOrderUsesInstanceAndIgnoresDepletedBlueprints()
    {
        var hand = new (int InstanceId, float ScreenX, int Count)[]
        {
            (11, 300f, 1), (12, 100f, 0), (13, 200f, 1), (14, 400f, 1)
        };
        Assert.Equal(13, ForcedDeploymentOrder.LeftmostInstance(hand));
        Assert.Equal(11, ForcedDeploymentOrder.LeftmostInstance(hand.Where(x => x.InstanceId != 13)));
        Assert.Null(ForcedDeploymentOrder.LeftmostInstance(Array.Empty<(int, float, int)>()));
    }
}
