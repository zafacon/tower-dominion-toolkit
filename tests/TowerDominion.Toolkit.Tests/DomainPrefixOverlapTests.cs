using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class DomainPrefixOverlapTests
{
    [Fact]
    public void MultiArgumentShortDomainCannotConsumeLongerDomain()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontier", "Short"));
        grammar.RegisterDomain(new("frontier diagnostics", "Long"));
        grammar.Register(new("set", "frontier", "Short with arguments",
            [new ArgumentDefinition("mode", "Mode"), new ArgumentDefinition("state", "State")]));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("set", "frontier diagnostics", "Long with argument",
            [new ArgumentDefinition("state", "State")])));
    }

    [Fact]
    public void DisjointLiteralPreventsDomainPrefixCollision()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontier", "Short"));
        grammar.RegisterDomain(new("frontier diagnostics", "Long"));
        grammar.Register(new("set", "frontier", "Short with arguments",
            [new ArgumentDefinition("mode", "Mode", AllowedValues: ["other"]), new ArgumentDefinition("state", "State")]));
        grammar.Register(new("set", "frontier diagnostics", "Long with argument",
            [new ArgumentDefinition("state", "State")]));
        Assert.Equal(2, grammar.Operations.Count);
    }
}
