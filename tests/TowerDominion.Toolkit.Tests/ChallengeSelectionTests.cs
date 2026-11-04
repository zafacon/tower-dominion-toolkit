using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ChallengeSelectionTests
{
    private sealed class Rule(string id) : IChallengeRule
    {
        public ChallengeDescriptor Descriptor { get; } = new(id, id, "Test", new HashSet<ChallengeActionKind> { ChallengeActionKind.PlaceTower });
        public ChallengeDecision Evaluate(ChallengeAction action) => ChallengeDecision.Permit();
    }

    [Fact]
    public void SelectionPersistsAndRunSnapshotIsLocked()
    {
        string root = Path.Combine(Path.GetTempPath(), "toolkit-challenges-" + Guid.NewGuid().ToString("N"));
        try
        {
            ChallengeRegistry registry = new();
            registry.Register(new Rule("cheapest-tower"));
            registry.Register(new Rule("no-selling"));
            string path = Path.Combine(root, "selected.json");
            ChallengeSelections selections = new(path, registry);
            selections.Change(["cheapest-tower"], true);
            Assert.Equal("cheapest-tower", Assert.Single(new ChallengeSelections(path, registry).Selected));
            selections.StartRun();
            Assert.Equal("cheapest-tower", Assert.Single(selections.Active));
            Assert.Throws<InvalidOperationException>(() => selections.Change(["no-selling"], true));
            Assert.Throws<InvalidOperationException>(() => selections.Clear());
            selections.EndRun();
            selections.Change(["no-selling"], true);
            Assert.Equal(2, new ChallengeSelections(path, registry).Selected.Count);
            Assert.Empty(selections.Active);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void InvalidSelectionDoesNotPartiallyChangeState()
    {
        string root = Path.Combine(Path.GetTempPath(), "toolkit-challenges-" + Guid.NewGuid().ToString("N"));
        try
        {
            ChallengeRegistry registry = new();
            registry.Register(new Rule("cheapest-tower"));
            ChallengeSelections selections = new(Path.Combine(root, "selected.json"), registry);
            Assert.Throws<ArgumentException>(() => selections.Change(["cheapest-tower", "missing"], true));
            Assert.Empty(selections.Selected);
            Assert.Contains(registry.List(), rule => rule.Id == "cheapest-tower");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
