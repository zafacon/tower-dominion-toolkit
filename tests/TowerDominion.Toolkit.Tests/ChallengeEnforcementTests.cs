using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ChallengeEnforcementTests
{
    private sealed class Rule(string id, ChallengeActionKind kind, bool blocks, bool throws = false) : IChallengeRule
    {
        public ChallengeDescriptor Descriptor { get; } = new(id, id, "Test", new HashSet<ChallengeActionKind> { kind });
        public ChallengeDecision Evaluate(ChallengeAction action)
        {
            if (throws) throw new InvalidOperationException("Broken rule");
            return blocks ? ChallengeDecision.Block(Descriptor.Id) : ChallengeDecision.Permit();
        }
    }

    [Fact]
    public void SimulatedActionsAreCheckedOnlyDuringRun()
    {
        string root = Path.Combine(Path.GetTempPath(), "toolkit-enforcement-" + Guid.NewGuid().ToString("N"));
        try
        {
            ChallengeRegistry registry = new();
            registry.Register(new Rule("no-selling", ChallengeActionKind.SellBuilding, true));
            registry.Register(new Rule("no-rerolls", ChallengeActionKind.Reroll, true));
            ChallengeSelections selections = new(Path.Combine(root, "selected.json"), registry);
            selections.Change(["no-selling", "no-rerolls"], true);
            ChallengeEnforcement enforcement = new(registry, selections);
            Assert.True(enforcement.Evaluate(new ChallengeAction(ChallengeActionKind.SellBuilding)).Allowed);
            selections.StartRun();
            Assert.False(enforcement.Evaluate(new ChallengeAction(ChallengeActionKind.SellBuilding)).Allowed);
            Assert.False(enforcement.Evaluate(new ChallengeAction(ChallengeActionKind.Reroll)).Allowed);
            Assert.True(enforcement.Evaluate(new ChallengeAction(ChallengeActionKind.PlaceTower)).Allowed);
            selections.EndRun();
            Assert.True(enforcement.Evaluate(new ChallengeAction(ChallengeActionKind.SellBuilding)).Allowed);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void BrokenChallengeFailsClosedWithoutThrowing()
    {
        string root = Path.Combine(Path.GetTempPath(), "toolkit-enforcement-" + Guid.NewGuid().ToString("N"));
        try
        {
            ChallengeRegistry registry = new();
            registry.Register(new Rule("broken", ChallengeActionKind.PlaceTower, false, true));
            ChallengeSelections selections = new(Path.Combine(root, "selected.json"), registry);
            selections.Change(["broken"], true);
            selections.StartRun();
            ChallengeDecision decision = new ChallengeEnforcement(registry, selections).Evaluate(new ChallengeAction(ChallengeActionKind.PlaceTower));
            Assert.False(decision.Allowed);
            Assert.Contains("failed", decision.Reason);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
