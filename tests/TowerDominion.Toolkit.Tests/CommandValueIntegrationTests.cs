using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandValueIntegrationTests
{
    private static (CommandArgumentCompletion Completion, CommandTextParser Parser) Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select", [new ArgumentDefinition("names", "Names",
            Completion: ArgumentCompletion.Catalog, CommaSeparated: true,
            CatalogProvider: () => ["Alpha", "Beta Prime", "all", "A Quote", "Gamma"])]));
        grammar.Register(new("load", "frontiers", "Load", [new ArgumentDefinition("name", "Name",
            Completion: ArgumentCompletion.Preset, CatalogProvider: () => ["My Setup"])]));
        return (new(grammar), new(grammar));
    }

    [Theory]
    [InlineData("select frontiers Alpha, Be", "select frontiers Alpha, \"Beta Prime\"")]
    [InlineData("select frontiers \"Be\"", "select frontiers \"Beta Prime\"")]
    [InlineData("load frontiers My", "load frontiers \"My Setup\"")]
    [InlineData("select frontiers Alpha, Ga", "select frontiers Alpha, Gamma")]
    public void FullLineCompletionParses(string input, string expected)
    {
        var (completion, parser) = Create();
        var result = completion.Complete(input, input.Length);
        Assert.Equal(expected, result.Completion.Text);
        Assert.NotNull(parser.Parse(result.Completion.Text).Invocation);
    }

    [Fact]
    public void ReservedAllCanBeCompletedAlone()
    {
        var (completion, parser) = Create();
        var result = completion.Complete("select frontiers all", 20);
        Assert.Equal("select frontiers all", result.Completion.Text);
        Assert.Contains("names", parser.Parse(result.Completion.Text).Invocation!.AllSelections!);
    }
}
