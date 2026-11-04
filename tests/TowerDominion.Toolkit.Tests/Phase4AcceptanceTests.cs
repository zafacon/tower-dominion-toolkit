using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class Phase4AcceptanceTests
{
    private static (CommandGrammar Grammar, List<string> Calls) Create()
    {
        var grammar = new CommandGrammar();
        var calls = new List<string>();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.RegisterDomain(new("starting tiles", "Tiles"));
        grammar.Register(new("list", "frontiers", "List", [], Handler: _ => { calls.Add("list"); return CommandOutcome.Ok("ok"); }));
        grammar.Register(new("list", "frontiers", "Selected", [], Qualifier: "selected", Handler: _ => { calls.Add("selected"); return CommandOutcome.Ok("ok"); }));
        grammar.Register(new("select", "frontiers", "Select", [new ArgumentDefinition("names", "Names", Completion: ArgumentCompletion.Catalog,
            CommaSeparated: true, CatalogProvider: () => ["Alpha", "Beta Prime", "all"])],
            Handler: invocation => { calls.Add(string.Join(",", invocation.SelectionMembers?["names"] ?? [])); return CommandOutcome.Ok("ok"); }));
        grammar.Register(new("set", "starting tiles", "Set", [new ArgumentDefinition("shape", "Shape", AllowedValues: ["corner", "straight"])],
            Handler: invocation => { calls.Add(invocation.Arguments[0]); return CommandOutcome.Ok("ok"); }));
        grammar.RegisterPresets("frontiers", new(
            invocation => { calls.Add("save:" + invocation.Arguments[0]); return CommandOutcome.Ok("ok"); },
            invocation => { calls.Add("load:" + invocation.Arguments[0]); return CommandOutcome.Ok("ok"); },
            _ => CommandOutcome.Ok("ok"), _ => CommandOutcome.Ok("ok"), _ => CommandOutcome.Ok("ok")));
        return (grammar, calls);
    }

    [Theory]
    [InlineData("list selected fr", "list selected frontiers", "selected")]
    [InlineData("set starting ti", "set starting tiles", null)]
    [InlineData("save frontiers a", "save frontiers as", null)]
    public void StructuralCompletionAgreesWithRegisteredHelpAndParsing(string input, string expected, string? dispatchCall)
    {
        var (grammar, calls) = Create();
        var result = new CommandStructuralCompletion(grammar).Complete(input, input.Length);
        Assert.Equal(expected, result.Text);
        Assert.Contains(grammar.Operations, operation => operation.Syntax.StartsWith(expected, StringComparison.OrdinalIgnoreCase));
        if (dispatchCall is not null)
        {
            Assert.True(new CommandTextParser(grammar).Execute(result.Text).Success);
            Assert.Contains(dispatchCall, calls);
        }
        else Assert.NotNull(new CommandDiagnostics(grammar).Analyze(result.Text).HelpCommand);
    }

    [Theory]
    [InlineData("select frontiers Alpha, Be", "select frontiers Alpha, \"Beta Prime\"", "Alpha,Beta Prime")]
    [InlineData("set starting tiles co", "set starting tiles corner", "corner")]
    [InlineData("save frontiers as My Setup", "save frontiers as My Setup", "save:My Setup")]
    public void ArgumentCompletionHelpParserAndDispatchAgree(string input, string expected, string call)
    {
        var (grammar, calls) = Create();
        var completion = new CommandArgumentCompletion(grammar).Complete(input, input.Length);
        Assert.Equal(expected, completion.Completion.Text);
        var parsed = new CommandTextParser(grammar).Parse(expected);
        Assert.NotNull(parsed.Invocation);
        var help = new CommandHelp(grammar).Query($"{parsed.Invocation.Verb} {parsed.Invocation.Domain}");
        Assert.True(help.Success);
        Assert.Contains(help.Operations, operation => operation.Variant == parsed.Invocation.Variant && operation.Qualifier == parsed.Invocation.Qualifier);
        Assert.True(grammar.Dispatch(parsed.Invocation).Success);
        Assert.Contains(call, calls);
    }

    [Fact]
    public void InvalidInputCannotDispatchAndDiagnosticsUseRegisteredSyntax()
    {
        var (grammar, calls) = Create();
        var input = "set starting tiles diagonal";
        var diagnostic = new CommandDiagnostics(grammar).Analyze(input);
        Assert.False(diagnostic.Success);
        Assert.Equal("help set starting tiles", diagnostic.HelpCommand);
        Assert.Equal(new CommandHelp(grammar).Query("set starting tiles").Syntax, diagnostic.Syntax);
        Assert.False(new CommandTextParser(grammar).Execute(input).Success);
        Assert.Empty(calls);
    }

    [Fact]
    public void OverviewHelpEnumeratesEveryRegisteredOperation()
    {
        var (grammar, _) = Create();
        Assert.Equal(grammar.Operations.Select(operation => operation.Syntax), new CommandHelp(grammar).Query().Syntax);
    }
}
