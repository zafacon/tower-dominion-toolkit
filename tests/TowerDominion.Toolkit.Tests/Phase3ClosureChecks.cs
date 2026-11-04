using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class Phase3ClosureChecks
{
    [Theory]
    [InlineData("set", false)]
    [InlineData("set starting", false)]
    [InlineData("set starting tiles", false)]
    [InlineData("set starting tiles \"", false)]
    [InlineData("set starting tiles L.C,T.A,X", true)]
    public void StartingTileParserPassesShapesWithoutInferringDifficulty(string input, bool valid)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("starting tiles", "Tiles"));
        grammar.Register(new("set", "starting tiles", "Shapes",
            [new ArgumentDefinition("shapes", "Shapes", CommaSeparated: true)],
            Handler: invocation => CommandOutcome.Ok(string.Join("|", invocation.SelectionMembers!["shapes"]))));
        var result = new CommandTextParser(grammar).Execute(input);
        Assert.Equal(valid, result.Success);
        if (valid) Assert.Equal("L.C|T.A|X", result.Message);
    }

    [Theory]
    [InlineData("list selected")]
    [InlineData("list active frontiers")]
    [InlineData("load frontiers")]
    [InlineData("save frontiers")]
    [InlineData("save frontiers as")]
    [InlineData("inspect frontiers preset")]
    [InlineData("delete frontiers preset")]
    [InlineData("list frontiers preset")]
    public void IncompleteCommandsFailWithoutInvokingHandlers(string input)
    {
        int calls = 0;
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("list", "frontiers", "Selected", [], Qualifier: "selected",
            Handler: _ => { calls++; return CommandOutcome.Ok("ok"); }));
        Func<CommandInvocation, CommandOutcome> handler = _ => { calls++; return CommandOutcome.Ok("ok"); };
        grammar.RegisterPresets("frontiers", new(handler, handler, handler, handler, handler));
        var parser = new CommandTextParser(grammar);
        Assert.Null(parser.Parse(input).Invocation);
        Assert.False(parser.Execute(input).Success);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void AmbiguousOptionalDomainPrefixIsRejectedWithoutMutation()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontier", "Base"));
        grammar.RegisterDomain(new("frontier diagnostics", "Diagnostics"));
        grammar.Register(new("set", "frontier", "Base",
            [new ArgumentDefinition("name", "Name", AllowedValues: ["diagnostics"]),
             new ArgumentDefinition("state", "State", Required: false, AllowedValues: ["on"])]));
        int count = grammar.Operations.Count;
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("set", "frontier diagnostics", "Diagnostics",
            [new ArgumentDefinition("state", "State", Required: false, AllowedValues: ["on"])])));
        Assert.Equal(count, grammar.Operations.Count);
    }
}
