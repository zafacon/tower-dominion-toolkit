using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class QuotedVerbTests
{
    [Theory]
    [InlineData("\"list\" frontiers")]
    [InlineData("l\"ist\" frontiers")]
    public void QuotedCommandVerbIsRejected(string command)
    {
        int calls = 0;
        var grammar = Create(() => calls++);
        var parser = new CommandTextParser(grammar);
        Assert.Null(parser.Parse(command).Invocation);
        Assert.False(parser.Execute(command).Success);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void OrdinaryVerbStillExecutes()
    {
        int calls = 0;
        var grammar = Create(() => calls++);
        Assert.True(new CommandTextParser(grammar).Execute("list frontiers").Success);
        Assert.Equal(1, calls);
    }

    private static CommandGrammar Create(Action invoked)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("list", "frontiers", "List", [],
            Handler: _ => { invoked(); return CommandOutcome.Ok("ok"); }));
        return grammar;
    }
}
