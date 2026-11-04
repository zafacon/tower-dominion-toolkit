using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandDiagnosticsTests
{
    private static CommandDiagnostics Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.RegisterDomain(new("starting tiles", "Tiles"));
        grammar.Register(new("select", "frontiers", "Select", [new ArgumentDefinition("names", "Names", CommaSeparated: true)]));
        grammar.Register(new("set", "starting tiles", "Shapes", [new ArgumentDefinition("shape", "Shape", AllowedValues: ["corner", "straight"])]));
        grammar.Register(new("list", "frontiers", "List", []));
        grammar.Register(new("list", "frontiers", "Selected", [], Qualifier: "selected"));
        return new(grammar);
    }

    [Fact]
    public void ValidCommandHasNoDiagnostic()
    {
        var result = Create().Analyze("set starting tiles corner");
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.Empty(result.Syntax);
    }

    [Theory]
    [InlineData("set starting tiles", "help set starting tiles")]
    [InlineData("set starting tiles diagonal", "help set starting tiles")]
    [InlineData("select frontiers Alpha, all", "help select frontiers")]
    public void InvalidArgumentsHaveSpecificHelp(string input, string expectedHelp)
    {
        var result = Create().Analyze(input);
        Assert.False(result.Success);
        Assert.Equal(expectedHelp, result.HelpCommand);
        Assert.NotEmpty(result.Syntax);
        Assert.All(result.Syntax, syntax => Assert.StartsWith(input.Split(' ')[0], syntax));
    }

    [Fact]
    public void UnsupportedDomainShowsOnlyVerbSyntax()
    {
        var result = Create().Analyze("set frontiers something");
        Assert.Equal("help set", result.HelpCommand);
        Assert.Single(result.Syntax);
        Assert.Contains("starting tiles", result.Syntax[0]);
    }

    [Fact]
    public void UnknownVerbPointsToOverview()
    {
        var result = Create().Analyze("explode frontiers");
        Assert.Equal("help", result.HelpCommand);
        Assert.NotEmpty(result.Syntax);
    }
}
