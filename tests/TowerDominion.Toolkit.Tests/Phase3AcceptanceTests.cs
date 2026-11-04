using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class Phase3AcceptanceTests
{
    [Theory]
    [InlineData("select frontiers one,two", true)]
    [InlineData("select frontiers ONE,Two", true)]
    [InlineData("select frontiers all", true)]
    [InlineData("select frontiers \"all\"", false)]
    [InlineData("select frontiers one,missing", false)]
    [InlineData("select frontiers all,one", false)]
    [InlineData("select frontiers one,all", false)]
    [InlineData("select frontiers one,,two", false)]
    public void ParseExecuteAndDirectDispatchAgreeOnSelectionValidation(string command, bool expected)
    {
        int calls = 0;
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", CommaSeparated: true,
                CatalogProvider: () => ["one", "two"])],
            Handler: _ => { calls++; return CommandOutcome.Ok("ok"); }));
        var parser = new CommandTextParser(grammar);
        var parsed = parser.Parse(command);
        if (parsed.Invocation is not null)
            Assert.Equal(expected, grammar.Dispatch(parsed.Invocation).Success);
        Assert.Equal(expected, parser.Execute(command).Success);
        Assert.Equal(expected ? 2 : 0, calls);
    }

    [Fact]
    public void DynamicCatalogChangesAreObservedByBothExecutionPaths()
    {
        IReadOnlyList<string> available = ["one"];
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", CommaSeparated: true,
                CatalogProvider: () => available)], Handler: _ => CommandOutcome.Ok("ok")));
        var parser = new CommandTextParser(grammar);
        var parsed = parser.Parse("select frontiers two");
        Assert.NotNull(parsed.Invocation);
        Assert.False(grammar.Dispatch(parsed.Invocation).Success);
        Assert.False(parser.Execute("select frontiers two").Success);
        available = ["one", "two"];
        Assert.True(grammar.Dispatch(parsed.Invocation).Success);
        Assert.True(parser.Execute("select frontiers two").Success);
    }
}
