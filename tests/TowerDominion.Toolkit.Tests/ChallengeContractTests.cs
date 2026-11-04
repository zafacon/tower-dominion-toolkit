using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Tests;

public sealed class ChallengeContractTests
{
    private sealed class StubRule(string id, ChallengeActionKind kind, bool allow) : IChallengeRule
    {
        public ChallengeDescriptor Descriptor { get; } = new(id, id, "Test rule", new HashSet<ChallengeActionKind> { kind });
        public ChallengeDecision Evaluate(ChallengeAction action) => allow ? ChallengeDecision.Permit() : ChallengeDecision.Block(id);
    }

    [Fact]
    public void RegistryRejectsDuplicateIds()
    {
        ChallengeRegistry registry = new();
        registry.Register(new StubRule("cheapest-tower", ChallengeActionKind.PlaceTower, true));
        Assert.Throws<InvalidOperationException>(() => registry.Register(new StubRule("CHEAPEST-TOWER", ChallengeActionKind.PlaceTower, false)));
        Assert.Single(registry.List());
    }

    [Fact]
    public void AnyApplicableChallengeCanVeto()
    {
        IChallengeRule[] rules = [new StubRule("allow", ChallengeActionKind.PlaceTower, true), new StubRule("block", ChallengeActionKind.PlaceTower, false)];
        ChallengeDecision result = ChallengeEvaluator.Evaluate(rules, new ChallengeAction(ChallengeActionKind.PlaceTower, "tower-a"));
        Assert.False(result.Allowed);
        Assert.Equal("block", result.Reason);
    }

    [Fact]
    public void UnrelatedActionsAreNotBlocked()
    {
        IChallengeRule[] rules = [new StubRule("no-selling", ChallengeActionKind.SellBuilding, false)];
        Assert.True(ChallengeEvaluator.Evaluate(rules, new ChallengeAction(ChallengeActionKind.PlaceTower)).Allowed);
    }
}
