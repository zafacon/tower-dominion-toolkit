using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class GreedyDomainSuffixTests
{
    [Theory]
    [InlineData("diagnostics", false)]
    [InlineData("diagnostics off", false)]
    [InlineData("diagnostics on", true)]
    public void FiniteGreedyValuesRespectLongerDomainArguments(string value, bool overlap)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontier", "Short"));
        grammar.RegisterDomain(new("frontier diagnostics", "Long"));
        grammar.Register(new("set", "frontier", "Short",
            [new ArgumentDefinition("name", "Name", Completion: ArgumentCompletion.FreeForm,
                AllowedValues: [value])]));
        Action register = () => grammar.Register(new("set", "frontier diagnostics", "Long",
            [new ArgumentDefinition("mode", "Mode", AllowedValues: ["on"])]));
        if (overlap) Assert.Throws<InvalidOperationException>(register);
        else register();
    }
}
