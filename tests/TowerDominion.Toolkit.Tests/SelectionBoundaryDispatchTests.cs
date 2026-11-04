using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class SelectionBoundaryDispatchTests
{
    [Theory]
    [InlineData("all,one")]
    [InlineData("one,all")]
    public void DirectDispatchRejectsMixedReservedAll(string value)
    {
        var grammar = Create();
        Assert.False(grammar.Dispatch(new("select", "frontiers", [value])).Success);
    }

    [Fact]
    public void DirectDispatchAllowsQuotedLiteralAllWithStructuredMembers()
    {
        var grammar = Create();
        Assert.True(grammar.Dispatch(new("select", "frontiers", ["\"all\",one"],
            SelectionMembers: new Dictionary<string, IReadOnlyList<string>> { ["members"] = ["all", "one"] })).Success);
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
