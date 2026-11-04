using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class GreedyOverlapTests
{
    [Theory]
    [InlineData(ArgumentCompletion.FreeForm)]
    [InlineData(ArgumentCompletion.Preset)]
    public void GreedySingleArgumentOverlapsTwoFixedArguments(ArgumentCompletion completion)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("seed", "Seed"));
        grammar.Register(new("set", "seed", "Two parts",
            [new ArgumentDefinition("first", "First"), new ArgumentDefinition("second", "Second")]));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("set", "seed", "Name",
            [new ArgumentDefinition("name", "Name", Completion: completion)], Variant: "named")));
        Assert.Single(grammar.Operations);
    }
}
