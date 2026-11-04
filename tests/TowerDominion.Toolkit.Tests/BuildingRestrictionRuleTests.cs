using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class BuildingRestrictionRuleTests
{
    [Theory]
    [InlineData(ChallengeActionKind.SellBuilding, false)]
    [InlineData(ChallengeActionKind.CancelBuilding, false)]
    [InlineData(ChallengeActionKind.UpgradeBuilding, true)]
    [InlineData(ChallengeActionKind.PlaceTower, true)]
    public void ChainOfCommandBlocksRemovalOnly(ChallengeActionKind kind, bool expected)
    {
        Assert.Equal(expected, new ChainOfCommandRule().Evaluate(new ChallengeAction(kind)).Allowed);
    }

    [Theory]
    [InlineData("left", true)]
    [InlineData("right", false)]
    [InlineData(null, false)]
    public void LeftTurnOnlyRequiresVerifiedLeftPath(string? path, bool expected)
    {
        Assert.Equal(expected, new LeftTurnOnlyRule().Evaluate(
            new ChallengeAction(ChallengeActionKind.UpgradeBuilding, path)).Allowed);
    }

    [Fact]
    public void LeftTurnOnlyDoesNotBlockUnrelatedActions()
    {
        Assert.True(new LeftTurnOnlyRule().Evaluate(new ChallengeAction(ChallengeActionKind.SellBuilding)).Allowed);
    }
}
