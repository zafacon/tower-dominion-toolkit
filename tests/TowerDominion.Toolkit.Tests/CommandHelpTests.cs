using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandHelpTests
{
    private static CommandHelp Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.RegisterDomain(new("starting tiles", "Tiles"));
        grammar.Register(new("list", "frontiers", "All", []));
        grammar.Register(new("list", "frontiers", "Selected", [], Qualifier: "selected"));
        grammar.Register(new("set", "starting tiles", "Shapes",
            [new ArgumentDefinition("shapes", "Shapes", CommaSeparated: true)]));
        grammar.RegisterPresets("frontiers", new(
            _ => CommandOutcome.Ok("ok"), _ => CommandOutcome.Ok("ok"),
            _ => CommandOutcome.Ok("ok"), _ => CommandOutcome.Ok("ok"),
            _ => CommandOutcome.Ok("ok")));
        return new CommandHelp(grammar);
    }

    [Fact]
    public void OverviewIncludesEveryRegisteredOperation()
    {
        var help = Create().Query("help");
        Assert.True(help.Success);
        Assert.Equal(8, help.Syntax.Count);
        Assert.Contains("starting tiles", help.Domains!);
        Assert.Contains("save", help.Verbs!);
    }

    [Theory]
    [InlineData("list", 3)]
    [InlineData("frontiers", 7)]
    [InlineData("list frontiers", 3)]
    [InlineData("set starting tiles", 1)]
    [InlineData("help set starting tiles", 1)]
    public void BidirectionalQueriesIncludeOnlyMatchingOperations(string target, int count)
    {
        var help = Create().Query(target);
        Assert.True(help.Success);
        Assert.Equal(count, help.Operations.Count);
    }

    [Fact]
    public void PresetAndQualifiedSyntaxComeFromDefinitions()
    {
        var help = Create().Query("frontiers");
        Assert.Contains("list selected frontiers", help.Syntax);
        Assert.Contains("save frontiers as <name>", help.Syntax);
        Assert.Contains("list frontiers presets", help.Syntax);
    }

    [Theory]
    [InlineData("list starting tiles")]
    [InlineData("set frontiers")]
    [InlineData("nonexistent")]
    [InlineData("help set nonexistent")]
    public void UnknownOrUnsupportedQueriesDoNotInventCommands(string target)
    {
        var help = Create().Query(target);
        Assert.False(help.Success);
        Assert.Empty(help.Operations);
    }
}
