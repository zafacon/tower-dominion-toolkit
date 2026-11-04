using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class DirectDispatchTests
{
    [Theory]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData("42", true)]
    public void DirectInvocationCannotBypassSemanticValidation(string value, bool valid)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("seed", "Seed"));
        int calls = 0;
        grammar.Register(new("set", "seed", "Set seed",
            [new ArgumentDefinition("value", "Integer", Validator: s => int.TryParse(s, out _) ? null : "Invalid seed")],
            Handler: _ => { calls++; return CommandOutcome.Ok("ok"); }));
        Assert.Equal(valid, grammar.Dispatch(new("set", "seed", [value])).Success);
        Assert.Equal(valid ? 1 : 0, calls);
    }

    [Fact]
    public void DirectInvocationRejectsMalformedSelection()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        int calls = 0;
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", CommaSeparated: true)],
            Handler: _ => { calls++; return CommandOutcome.Ok("ok"); }));
        Assert.False(grammar.Dispatch(new("select", "frontiers", ["a,,b"])).Success);
        Assert.Equal(0, calls);
    }
}
