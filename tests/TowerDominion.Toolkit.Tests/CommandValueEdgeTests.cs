using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandValueEdgeTests
{
    private static (CommandArgumentCompletion Completion, CommandTextParser Parser) Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select", [new ArgumentDefinition("names", "Names",
            Completion: ArgumentCompletion.Catalog, CommaSeparated: true,
            CatalogProvider: () => ["Alpha", "Beta Prime", "all", "A \"Quote\"", "Gamma"])]));
        grammar.Register(new("load", "frontiers", "Load", [new ArgumentDefinition("name", "Name",
            Completion: ArgumentCompletion.Preset, CatalogProvider: () => ["My Setup"])]));
        return (new(grammar), new(grammar));
    }

    [Theory]
    [InlineData("select frontiers Alpha, \"Be", "select frontiers Alpha, \"Beta Prime\"")]
    [InlineData("select frontiers Alpha, all", "select frontiers Alpha, \"all\"")]
    [InlineData("select frontiers Alpha, \"all", "select frontiers Alpha, \"all\"")]
    [InlineData("select frontiers A \\\"Q", "select frontiers \"A \\\"Quote\\\"\"")]
    [InlineData("load frontiers \"My", "load frontiers \"My Setup\"")]
    public void CompletionProducesParsableValues(string input, string expected)
    {
        var (completion, parser) = Create();
        var result = completion.Complete(input, input.Length);
        Assert.Equal(expected, result.Completion.Text);
        Assert.NotNull(parser.Parse(result.Completion.Text).Invocation);
    }

    [Fact]
    public void CursorInsideQuotedMemberPreservesOtherMembers()
    {
        var (completion, parser) = Create();
        const string input = "select frontiers Alpha, \"Be Prime\", Gamma";
        var result = completion.Complete(input, input.IndexOf(" Prime", StringComparison.Ordinal));
        Assert.Equal("select frontiers Alpha, \"Beta Prime\", Gamma", result.Completion.Text);
        Assert.NotNull(parser.Parse(result.Completion.Text).Invocation);
    }

    [Fact]
    public void QuotedAllIsLiteralInCommaList()
    {
        var (completion, parser) = Create();
        var result = completion.Complete("select frontiers Alpha, all", 27);
        var parsed = parser.Parse(result.Completion.Text);
        Assert.NotNull(parsed.Invocation);
        Assert.DoesNotContain("names", parsed.Invocation.AllSelections!);
        Assert.Contains("all", parsed.Invocation.SelectionMembers!["names"]);
    }
}
