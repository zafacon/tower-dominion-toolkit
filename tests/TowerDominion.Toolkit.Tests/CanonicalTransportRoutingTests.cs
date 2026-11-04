using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CanonicalTransportRoutingTests
{
    [Fact]
    public void BothTransportsCanUseOneCanonicalExecutor()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("foes", "Foes"));
        grammar.Register(new("list", "foes", "List foes", Array.Empty<ArgumentDefinition>(),
            Handler: _ => CommandOutcome.Ok("canonical foes")));
        var executor = new CanonicalCommandExecutor(grammar);
        Assert.Equal("canonical foes", executor.Execute("list foes"));
        Assert.Equal("canonical foes", executor.Execute("  list\tfoes"));
        Assert.Equal("canonical foes", executor.ExecuteDetailed("list foes").Message);
        Assert.False(executor.ExecuteDetailed("version").Success);
        Assert.False(executor.ExecuteDetailed("list banned").Success);
        Assert.False(executor.ExecuteDetailed("list invalid").Success);
    }
}
