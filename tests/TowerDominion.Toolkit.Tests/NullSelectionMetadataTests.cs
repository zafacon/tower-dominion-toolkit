using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class NullSelectionMetadataTests
{
    [Fact]
    public void NullMemberListFailsWithoutCallingHandler()
    {
        int calls = 0;
        var grammar = Create(() => calls++);
        var metadata = new Dictionary<string, IReadOnlyList<string>> { ["members"] = null! };
        var outcome = grammar.Dispatch(new("select", "frontiers", ["one"], SelectionMembers: metadata));
        Assert.False(outcome.Success);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void NullMemberFailsWithoutCallingHandler()
    {
        int calls = 0;
        var grammar = Create(() => calls++);
        var metadata = new Dictionary<string, IReadOnlyList<string>> { ["members"] = [null!] };
        var outcome = grammar.Dispatch(new("select", "frontiers", ["one"], SelectionMembers: metadata));
        Assert.False(outcome.Success);
        Assert.Equal(0, calls);
    }

    private static CommandGrammar Create(Action invoked)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", CommaSeparated: true)],
            Handler: _ => { invoked(); return CommandOutcome.Ok("ok"); }));
        return grammar;
    }
}
