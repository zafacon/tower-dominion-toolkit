using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ChallengeCommandRegistrationTests
{
    private sealed class Rule(string id) : IChallengeRule
    {
        public ChallengeDescriptor Descriptor { get; } = new(id, id, "Test", new HashSet<ChallengeActionKind>());
        public ChallengeDecision Evaluate(ChallengeAction action) => ChallengeDecision.Permit();
    }

    [Fact]
    public void SelectListClearAndRunLock()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-challenge-commands-" + Guid.NewGuid());
        try
        {
            var registry = new ChallengeRegistry();
            registry.Register(new Rule("alpha"));
            registry.Register(new Rule("beta"));
            var selections = new ChallengeSelections(Path.Combine(root, "selected.json"), registry);
            var grammar = new CommandGrammar();
            ChallengeCommandRegistration.Register(grammar, registry, selections);
            var parser = new CommandTextParser(grammar);
            Assert.Contains("alpha", parser.Execute("list challenges").Message);
            Assert.True(parser.Execute("select challenges alpha,beta").Success);
            Assert.Equal(2, selections.Selected.Count);
            Assert.True(parser.Execute("unselect challenges alpha").Success);
            Assert.Equal(["beta"], selections.Selected);
            Assert.True(parser.Execute("select challenges all").Success);
            Assert.Equal(2, selections.Selected.Count);
            selections.StartRun();
            Assert.Contains("alpha", parser.Execute("list active challenges").Message);
            Assert.False(parser.Execute("clear challenges").Success);
            Assert.False(parser.Execute("unselect challenges all").Success);
            Assert.Equal(2, selections.Active.Count);
            selections.EndRun();
            Assert.True(parser.Execute("clear challenges").Success);
            Assert.Empty(selections.Selected);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void UnknownIdsDoNotPartiallyChangeSelection()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-challenge-commands-" + Guid.NewGuid());
        try
        {
            var registry = new ChallengeRegistry();
            registry.Register(new Rule("alpha"));
            var selections = new ChallengeSelections(Path.Combine(root, "selected.json"), registry);
            var grammar = new CommandGrammar();
            ChallengeCommandRegistration.Register(grammar, registry, selections);
            var parser = new CommandTextParser(grammar);
            Assert.False(parser.Execute("select challenges alpha,missing").Success);
            Assert.Empty(selections.Selected);
            Assert.True(parser.Execute("select challenges alpha").Success);
            Assert.Equal(["alpha"], new ChallengeSelections(Path.Combine(root, "selected.json"), registry).Selected);
            Assert.Equal(7, grammar.Operations.Count);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Fact]
    public void IncompatibleSelectionAllIsAtomicAndShowExplainsConflict()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-challenge-conflict-" + Guid.NewGuid());
        try
        {
            var registry = new ChallengeRegistry();
            registry.Register(new CheapestTowerOnlyRule());
            registry.Register(new ForcedDeploymentRule());
            var selections = new ChallengeSelections(Path.Combine(root, "selected.json"), registry);
            var grammar = new CommandGrammar();
            ChallengeCommandRegistration.Register(grammar, registry, selections);
            var parser = new CommandTextParser(grammar);
            Assert.True(parser.Execute("select challenges cheapest-tower-only").Success);
            var rejected = parser.Execute("select challenges all");
            Assert.False(rejected.Success);
            Assert.Contains("Forced Deployment", rejected.Message);
            Assert.Contains("Cheapest Tower Only", rejected.Message);
            Assert.Equal(new[] { CheapestTowerOnlyRule.Id }, selections.Selected);
            Assert.Equal(new[] { CheapestTowerOnlyRule.Id }, new ChallengeSelections(Path.Combine(root, "selected.json"), registry).Selected);
            Assert.Contains("Forced Deployment", parser.Execute("show challenges cheapest-tower-only").Message);
            Assert.Contains("Cheapest Tower Only", parser.Execute("show challenges forced-deployment").Message);
            Assert.Contains("Incompatible with: Forced Deployment", ChallengeDisplay.Describe(registry, selections.Selected));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
