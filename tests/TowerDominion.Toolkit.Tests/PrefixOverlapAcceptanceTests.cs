using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class PrefixOverlapAcceptanceTests
{
    [Fact]
    public void DisjointOptionalSuffixValuesAllowBothRegistrationsAndExecution()
    {
        var grammar = Create();
        grammar.Register(new("set", "frontier", "Short",
            [new ArgumentDefinition("target", "Target", AllowedValues: ["diagnostics"]),
             new ArgumentDefinition("state", "State", AllowedValues: ["off"])],
            Handler: _ => CommandOutcome.Ok("short")));
        grammar.Register(new("set", "frontier diagnostics", "Long",
            [new ArgumentDefinition("state", "State", Required: false, AllowedValues: ["on"])],
            Handler: _ => CommandOutcome.Ok("long")));
        var parser = new CommandTextParser(grammar);
        Assert.Equal("short", parser.Execute("set frontier diagnostics off").Message);
        Assert.Equal("long", parser.Execute("set frontier diagnostics on").Message);
    }

    [Fact]
    public void MatchingOptionalSuffixValuesMustBeRejected()
    {
        var grammar = Create();
        grammar.Register(new("set", "frontier", "Short",
            [new ArgumentDefinition("target", "Target", AllowedValues: ["diagnostics"]),
             new ArgumentDefinition("state", "State", Required: false, AllowedValues: ["on"])]));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("set", "frontier diagnostics", "Long",
            [new ArgumentDefinition("state", "State", Required: false, AllowedValues: ["on"])])));
    }

    private static CommandGrammar Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontier", "Short"));
        grammar.RegisterDomain(new("frontier diagnostics", "Long"));
        return grammar;
    }
}
