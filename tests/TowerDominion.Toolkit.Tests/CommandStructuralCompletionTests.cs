using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandStructuralCompletionTests
{
    private static CommandStructuralCompletion Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.RegisterDomain(new("starting tiles", "Tiles"));
        grammar.Register(new("list", "frontiers", "All", []));
        grammar.Register(new("list", "frontiers", "Selected", [], Qualifier: "selected"));
        grammar.Register(new("set", "starting tiles", "Shapes", [new ArgumentDefinition("shapes", "Shapes")]));
        grammar.RegisterPresets("frontiers", new(
            _ => CommandOutcome.Ok("ok"), _ => CommandOutcome.Ok("ok"),
            _ => CommandOutcome.Ok("ok"), _ => CommandOutcome.Ok("ok"),
            _ => CommandOutcome.Ok("ok")));
        return new(grammar);
    }

    [Theory]
    [InlineData("li", "list")]
    [InlineData("list sel", "list selected")]
    [InlineData("list selected fr", "list selected frontiers")]
    [InlineData("set sta", "set starting")]
    [InlineData("set starting ti", "set starting tiles")]
    [InlineData("save frontiers a", "save frontiers as")]
    [InlineData("inspect frontiers pre", "inspect frontiers preset")]
    [InlineData("list frontiers pre", "list frontiers presets")]
    [InlineData("help sta", "help starting")]
    [InlineData("help starting ti", "help starting tiles")]
    [InlineData("help set sta", "help set starting")]
    [InlineData("help set starting ti", "help set starting tiles")]
    public void CompletesOnlyRegisteredStructuralTokens(string input, string expected)
    {
        var result = Create().Complete(input, input.Length);
        Assert.Equal(expected, result.Text);
    }

    [Theory]
    [InlineData("list active ")]
    [InlineData("set frontiers ")]
    [InlineData("inspect frontiers presets ")]
    [InlineData("help set frontiers ")]
    public void DoesNotSuggestUnsupportedPaths(string input)
    {
        Assert.Empty(Create().Complete(input, input.Length).Candidates);
    }

    [Fact]
    public void CompletionPreservesTextAfterCursor()
    {
        var result = Create().Complete("set sta other", 7);
        Assert.Equal("set starting other", result.Text);
        Assert.Equal(12, result.Cursor);
    }
}
