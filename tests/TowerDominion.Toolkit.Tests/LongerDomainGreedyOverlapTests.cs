using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class LongerDomainGreedyOverlapTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("on now", true)]
    [InlineData("off later", false)]
    public void LongerDomainGreedyArgumentOverlapsShorterMultiArgumentPattern(string? allowed, bool overlap)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontier", "Short"));
        grammar.RegisterDomain(new("frontier diagnostics", "Long"));
        grammar.Register(new("set", "frontier", "Short",
            [new ArgumentDefinition("target", "Target", AllowedValues: ["diagnostics"]),
             new ArgumentDefinition("mode", "Mode", AllowedValues: ["on"]),
             new ArgumentDefinition("time", "Time", AllowedValues: ["now"])]));
        Action register = () => grammar.Register(new("set", "frontier diagnostics", "Long",
            [new ArgumentDefinition("name", "Name", Completion: ArgumentCompletion.FreeForm,
                AllowedValues: allowed is null ? null : [allowed])]));
        if (overlap) Assert.Throws<InvalidOperationException>(register);
        else
        {
            register();
            Assert.Equal(2, grammar.Operations.Count);
        }
    }
}
