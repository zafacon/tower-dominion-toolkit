using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class AllowedValueUniquenessTests
{
    [Theory]
    [InlineData("on", "on")]
    [InlineData("on", "ON")]
    public void DuplicateAllowedValuesAreRejectedWithoutMutation(string first, string second)
    {
        var grammar = Create();
        Assert.Throws<ArgumentException>(() => grammar.Register(new("set", "frontiers", "Set",
            [new ArgumentDefinition("mode", "Mode", AllowedValues: [first, second])])));
        Assert.Empty(grammar.Operations);
    }

    [Fact]
    public void DistinctAllowedValuesRemainCaseInsensitiveAtExecution()
    {
        var grammar = Create();
        grammar.Register(new("set", "frontiers", "Set",
            [new ArgumentDefinition("mode", "Mode", AllowedValues: ["on", "off"])],
            Handler: _ => CommandOutcome.Ok("ok")));
        Assert.True(new CommandTextParser(grammar).Execute("set frontiers ON").Success);
        Assert.True(grammar.Dispatch(new("set", "frontiers", ["OFF"])).Success);
    }

    private static CommandGrammar Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        return grammar;
    }
}
