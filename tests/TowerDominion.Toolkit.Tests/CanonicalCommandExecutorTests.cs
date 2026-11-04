using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CanonicalCommandExecutorTests
{
    [Fact]
    public void ExecutesOnlyRegisteredCanonicalCommands()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("foes", "Foes"));
        grammar.Register(new("list", "foes", "List foes", Array.Empty<ArgumentDefinition>(),
            Handler: _ => CommandOutcome.Ok("canonical foes")));
        var executor = new CanonicalCommandExecutor(grammar);
        Assert.Equal("canonical foes", executor.Execute("list foes"));
        Assert.False(executor.ExecuteDetailed("version").Success);
        Assert.False(executor.ExecuteDetailed("list banned").Success);
    }

    [Fact]
    public void RequiresGrammar()
    {
        Assert.Throws<ArgumentNullException>(() => new CanonicalCommandExecutor(null!));
    }
}
