using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class OptionalPrefixOverlapTests
{
    [Fact]
    public void OptionalTrailingArgumentCanOverlapLongerDomainWithoutArguments()
    {
        var grammar = Create();
        grammar.Register(new("set", "frontier", "Short",
            [new ArgumentDefinition("mode", "Mode"), new ArgumentDefinition("state", "State", Required: false)]));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("set", "frontier diagnostics", "Long",
            Array.Empty<ArgumentDefinition>())));
    }

    [Fact]
    public void RequiredTrailingArgumentPreventsOverlapWithLongerDomainWithoutArguments()
    {
        var grammar = Create();
        grammar.Register(new("set", "frontier", "Short",
            [new ArgumentDefinition("mode", "Mode"), new ArgumentDefinition("state", "State")]));
        grammar.Register(new("set", "frontier diagnostics", "Long", Array.Empty<ArgumentDefinition>()));
        Assert.Equal(2, grammar.Operations.Count);
    }

    [Fact]
    public void OptionalTrailingArgumentCanOverlapLongerDomainOptionalArgument()
    {
        var grammar = Create();
        grammar.Register(new("set", "frontier", "Short",
            [new ArgumentDefinition("mode", "Mode"), new ArgumentDefinition("state", "State", Required: false)]));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("set", "frontier diagnostics", "Long",
            [new ArgumentDefinition("state", "State", Required: false)])));
    }

    private static CommandGrammar Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontier", "Short"));
        grammar.RegisterDomain(new("frontier diagnostics", "Long"));
        return grammar;
    }
}
