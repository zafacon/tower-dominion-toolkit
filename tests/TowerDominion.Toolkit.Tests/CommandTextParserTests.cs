using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandTextParserTests
{
    private static readonly string[] selected = ["one,two"];
    private static CommandTextParser Create()
    {
        CommandGrammar grammar = new();
        foreach (string domain in new[] { "frontiers", "starting tiles", "frontier diagnostics" })
            grammar.RegisterDomain(new(domain, domain));
        grammar.Register(new("list", "frontiers", "All", Array.Empty<ArgumentDefinition>(),
            Handler: _ => CommandOutcome.Ok("all")));
        grammar.Register(new("list", "frontiers", "Selected", Array.Empty<ArgumentDefinition>(),
            Qualifier: "selected", Handler: _ => CommandOutcome.Ok("selected")));
        grammar.Register(new("select", "frontiers", "Select",
            new[] { new ArgumentDefinition("names", "Names") }, Handler: i => CommandOutcome.Ok(i.Arguments[0])));
        grammar.Register(new("set", "starting tiles", "Shapes",
            new[] { new ArgumentDefinition("shapes", "Shapes") }, Handler: i => CommandOutcome.Ok(i.Arguments[0])));
        grammar.Register(new("set", "frontier diagnostics", "Toggle",
            new[] { new ArgumentDefinition("enabled", "On/off") }, Handler: i => CommandOutcome.Ok(i.Arguments[0])));
        grammar.RegisterPresets("frontiers", new(
            _ => CommandOutcome.Ok("saved"), i => CommandOutcome.Ok(i.Arguments[0]),
            _ => CommandOutcome.Ok("presets"), i => CommandOutcome.Ok(i.Arguments[0]),
            _ => CommandOutcome.Ok("deleted")));
        return new(grammar);
    }

    [Theory]
    [InlineData("list frontiers", "all")]
    [InlineData("list selected frontiers", "selected")]
    [InlineData("list frontiers presets", "presets")]
    [InlineData("load frontiers \"My Setup\"", "My Setup")]
    [InlineData("save frontiers as My Setup", "saved")]
    [InlineData("inspect frontiers preset \"My Setup\"", "My Setup")]
    [InlineData("set starting tiles L.C,T.A,X", "L.C,T.A,X")]
    [InlineData("set frontier diagnostics on", "on")]
    [InlineData("select frontiers one,two", "one,two")]
    public void ParsesAndExecutesCanonicalCommands(string input, string expected)
    {
        Assert.Equal(expected, Create().Execute(input).Message);
    }

    [Theory]
    [InlineData("list selected")]
    [InlineData("list active frontiers")]
    [InlineData("list frontiers preset")]
    [InlineData("load frontiers")]
    [InlineData("set starting tiles")]
    [InlineData("load frontiers \"unterminated")]
    [InlineData("unknown frontiers")]
    public void RejectsInvalidCommands(string input)
    {
        Assert.False(Create().Execute(input).Success);
    }

    [Fact]
    public void QuotedReservedTokensStayArguments()
    {
        var parser = Create();
        Assert.Equal("all", parser.Execute("load frontiers \"all\"").Message);
        Assert.Equal("one,two", parser.Execute("select frontiers one,two").Message);
    }
}

public sealed class CommandValidationTests
{
    private static readonly string[] toggleValues = ["on", "off"];
    [Theory]
    [InlineData("on", true)]
    [InlineData("off", true)]
    [InlineData("maybe", false)]
    public void ValidatesAllowedValues(string value, bool expected)
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("frontier diagnostics", "Diagnostics"));
        grammar.Register(new("set", "frontier diagnostics", "Toggle",
            new[] { new ArgumentDefinition("state", "State", AllowedValues: toggleValues) },
            Handler: _ => CommandOutcome.Ok("ok")));
        Assert.Equal(expected, new CommandTextParser(grammar).Execute("set frontier diagnostics " + value).Success);
    }

    [Theory]
    [InlineData("a,b", true)]
    [InlineData("a,,b", false)]
    [InlineData("a,", false)]
    public void ValidatesCommaLists(string value, bool expected)
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("frontiers", "Frontiers"));
        grammar.Register(new("select", "frontiers", "Select",
            new[] { new ArgumentDefinition("members", "Members", CommaSeparated: true) },
            Handler: _ => CommandOutcome.Ok("ok")));
        Assert.Equal(expected, new CommandTextParser(grammar).Execute("select frontiers " + value).Success);
    }
}

public sealed class QuotedSelectionTests
{
    [Fact]
    public void QuotedCommaWithinMemberSurvivesTokenization()
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("frontiers", "Frontiers"));
        grammar.Register(new("select", "frontiers", "Select",
            new[] { new ArgumentDefinition("members", "Members", CommaSeparated: true) },
            Handler: i => CommandOutcome.Ok(i.Arguments[0])));
        var parser = new CommandTextParser(grammar);
        Assert.True(parser.Execute("select frontiers \"First, Second\",Third").Success);
        Assert.False(parser.Execute("select frontiers \"First, Second\",,").Success);
    }

    [Fact]
    public void DomainValidatorRejectsInvalidValuesBeforeDispatch()
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("seed", "Seed"));
        int calls = 0;
        grammar.Register(new("set", "seed", "Set seed",
            new[] { new ArgumentDefinition("value", "Integer seed", Validator: value =>
                int.TryParse(value, out _) ? null : "Seed must be an integer.") },
            Handler: _ => { calls++; return CommandOutcome.Ok("ok"); }));
        var parser = new CommandTextParser(grammar);
        Assert.False(parser.Execute("set seed invalid").Success);
        Assert.True(parser.Execute("set seed 42").Success);
        Assert.Equal(1, calls);
    }
}

public sealed class StructuredParserTests
{
    [Fact]
    public void ExposesQuotedSelectionMembersToHandler()
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("frontiers", "Frontiers"));
        grammar.Register(new("select", "frontiers", "Select",
            new[] { new ArgumentDefinition("members", "Members", CommaSeparated: true) },
            Handler: invocation => CommandOutcome.Ok(string.Join("|", invocation.SelectionMembers!["members"]))));
        Assert.Equal("First, Second|Third", new CommandTextParser(grammar)
            .Execute("select frontiers \"First, Second\",Third").Message);
    }

    [Fact]
    public void ReportsSpecificArgumentValidationErrors()
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("seed", "Seed"));
        grammar.Register(new("set", "seed", "Set seed",
            new[] { new ArgumentDefinition("value", "Integer seed", Validator: value =>
                int.TryParse(value, out _) ? null : "Seed must be an integer.") },
            Handler: _ => CommandOutcome.Ok("ok")));
        Assert.Equal("Seed must be an integer.", new CommandTextParser(grammar).Execute("set seed invalid").Message);
    }
}

public sealed class ContextualParserErrorTests
{
    [Theory]
    [InlineData("unknown frontiers", "Available verbs:")]
    [InlineData("list nowhere", "Supported domains: frontiers")]
    [InlineData("save frontiers missing-marker", "Expected 'as'")]
    [InlineData("inspect frontiers", "Expected 'preset'")]
    [InlineData("list frontiers preset", "Invalid argument count")]
    public void ReportsActionableErrors(string command, string expected)
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("list", "frontiers", "All", Array.Empty<ArgumentDefinition>()));
        grammar.RegisterPresets("frontiers", new(
            _ => CommandOutcome.Ok(""), _ => CommandOutcome.Ok(""), _ => CommandOutcome.Ok(""),
            _ => CommandOutcome.Ok(""), _ => CommandOutcome.Ok("")));
        Assert.Contains(expected, new CommandTextParser(grammar).Execute(command).Message);
    }
}
