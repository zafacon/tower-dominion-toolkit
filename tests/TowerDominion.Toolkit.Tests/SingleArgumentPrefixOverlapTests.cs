using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class SingleArgumentPrefixOverlapTests
{
    [Fact]
    public void SingleOrdinaryArgumentCannotConsumeLongerDomain()
    {
        var grammar = Create();
        grammar.Register(new("set", "frontier", "Short",
            [new ArgumentDefinition("name", "Name")]));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("set", "frontier diagnostics", "Long",
            Array.Empty<ArgumentDefinition>())));
    }

    [Fact]
    public void SingleOrdinaryArgumentWithDisjointAllowedValueIsSafe()
    {
        var grammar = Create();
        grammar.Register(new("set", "frontier", "Short",
            [new ArgumentDefinition("name", "Name", AllowedValues: ["other"])]));
        grammar.Register(new("set", "frontier diagnostics", "Long", Array.Empty<ArgumentDefinition>()));
        Assert.Equal(2, grammar.Operations.Count);
    }

    private static CommandGrammar Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontier", "Short"));
        grammar.RegisterDomain(new("frontier diagnostics", "Long"));
        return grammar;
    }
}
