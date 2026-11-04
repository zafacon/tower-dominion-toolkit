using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class SelectionMetadataTests
{
    [Fact]
    public void DirectDispatchRejectsContradictoryAllMetadata()
    {
        var grammar = Create();
        Assert.False(grammar.Dispatch(new("select", "frontiers", ["one"],
            AllSelections: ["members"])).Success);
        Assert.False(grammar.Dispatch(new("select", "frontiers", ["all"])).Success);
        Assert.True(grammar.Dispatch(new("select", "frontiers", ["all"],
            AllSelections: ["members"])).Success);
    }

    [Fact]
    public void DirectDispatchRejectsContradictoryMemberMetadata()
    {
        var grammar = Create();
        Assert.False(grammar.Dispatch(new("select", "frontiers", ["one,two"],
            SelectionMembers: new Dictionary<string, IReadOnlyList<string>> { ["members"] = ["one", "three"] })).Success);
        Assert.True(grammar.Dispatch(new("select", "frontiers", ["one,two"],
            SelectionMembers: new Dictionary<string, IReadOnlyList<string>> { ["members"] = ["one", "two"] })).Success);
    }

    private static CommandGrammar Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", CommaSeparated: true)],
            Handler: _ => CommandOutcome.Ok("ok")));
        return grammar;
    }
}
