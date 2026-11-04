using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class StructuredChallengeChangesTests
{
    private sealed class Rule : IChallengeRule
    {
        public ChallengeDescriptor Descriptor { get; } = new("one", "One", "Test", new HashSet<ChallengeActionKind>());
        public ChallengeDecision Evaluate(ChallengeAction action) => ChallengeDecision.Permit();
    }

    [Fact]
    public void SelectionClearNoopAndRunLockHaveTypedResults()
    {
        string path = Path.Combine(Path.GetTempPath(), "td-changes-" + Guid.NewGuid() + ".json");
        try
        {
            var registry = new ChallengeRegistry();
            registry.Register(new Rule());
            var selections = new ChallengeSelections(path, registry);
            var grammar = new CommandGrammar();
            ChallengeCommandRegistration.Register(grammar, registry, selections);
            var parser = new CommandTextParser(grammar);
            var selected = parser.Execute("select challenges one");
            Assert.True(selected.Success);
            Assert.Equal("selected", selected.Details.Changes![0].Property);
            Assert.Empty(Assert.IsType<string[]>(selected.Details.Changes[0].Before));
            Assert.Equal(["one"], Assert.IsType<string[]>(selected.Details.Changes[0].After));
            Assert.Empty(parser.Execute("select challenges one").Details.Changes!);
            var cleared = parser.Execute("clear challenges");
            Assert.True(cleared.Success);
            Assert.Equal(["one"], Assert.IsType<string[]>(cleared.Details.Changes![0].Before));
            Assert.Empty(Assert.IsType<string[]>(cleared.Details.Changes[0].After));
            Assert.Empty(parser.Execute("clear challenges").Details.Changes!);
            Assert.True(parser.Execute("select challenges one").Success);
            selections.StartRun();
            var locked = parser.Execute("clear challenges");
            Assert.False(locked.Success);
            Assert.Equal(CommandErrorKind.Conflict, locked.Details.Error);
            Assert.Null(locked.Details.Changes);
            Assert.Equal(["one"], selections.Selected);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
