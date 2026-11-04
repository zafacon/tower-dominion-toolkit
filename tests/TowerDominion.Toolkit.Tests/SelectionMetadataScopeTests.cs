using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class SelectionMetadataScopeTests
{
    [Fact]
    public void UnknownAllSelectionKeyIsRejectedBeforeHandler()
    {
        int calls = 0;
        var grammar = Create(() => calls++);
        Assert.False(grammar.Dispatch(new("select", "frontiers", ["one"],
            AllSelections: ["other"])).Success);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void UnknownMemberSelectionKeyIsRejectedBeforeHandler()
    {
        int calls = 0;
        var grammar = Create(() => calls++);
        Assert.False(grammar.Dispatch(new("select", "frontiers", ["one"],
            SelectionMembers: new Dictionary<string, IReadOnlyList<string>> { ["other"] = ["one"] })).Success);
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
