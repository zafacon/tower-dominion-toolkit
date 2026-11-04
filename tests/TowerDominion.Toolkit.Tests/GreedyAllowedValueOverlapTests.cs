using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class GreedyAllowedValueOverlapTests
{
    [Fact]
    public void DisjointGreedyAllowedValuesDoNotBlockRegistration()
    {
        var grammar = Create();
        grammar.Register(new("set", "seed", "Fixed",
            [new ArgumentDefinition("first", "First", AllowedValues: ["on"]),
             new ArgumentDefinition("second", "Second", AllowedValues: ["now"]) ]));
        grammar.Register(new("set", "seed", "Greedy",
            [new ArgumentDefinition("name", "Name", Completion: ArgumentCompletion.FreeForm,
                AllowedValues: ["off later"])], Variant: "named"));
        Assert.Equal(2, grammar.Operations.Count);
    }

    [Fact]
    public void MatchingGreedyAllowedValuesStillConflict()
    {
        var grammar = Create();
        grammar.Register(new("set", "seed", "Fixed",
            [new ArgumentDefinition("first", "First", AllowedValues: ["on"]),
             new ArgumentDefinition("second", "Second", AllowedValues: ["now"]) ]));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("set", "seed", "Greedy",
            [new ArgumentDefinition("name", "Name", Completion: ArgumentCompletion.FreeForm,
                AllowedValues: ["on now"])], Variant: "named")));
    }

    private static CommandGrammar Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("seed", "Seed"));
        return grammar;
    }
}
