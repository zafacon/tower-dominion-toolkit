using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ConsoleCommandAssistanceTests
{
    private static ConsoleCommandAssistance Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select", [new ArgumentDefinition("names", "Names",
            Completion: ArgumentCompletion.Catalog, CommaSeparated: true,
            CatalogProvider: () => ["Alpha", "Beta Prime", "Gamma"])]));
        grammar.Register(new("load", "frontiers", "Load", [new ArgumentDefinition("name", "Name",
            Completion: ArgumentCompletion.Preset, CatalogProvider: () => ["My Setup"])]));
        return new(grammar);
    }

    [Theory]
    [InlineData("sel", 3, "select")]
    [InlineData("select fr", 9, "select frontiers")]
    [InlineData("select frontiers Alpha, Be", 26, "select frontiers Alpha, \"Beta Prime\"")]
    [InlineData("load frontiers My", 17, "load frontiers \"My Setup\"")]
    [InlineData("select fr trailing", 9, "select frontiers trailing")]
    public void CompletesStructuralAndQuotedArguments(string input, int cursor, string expected)
    {
        var result = Create().Complete(input, cursor);
        Assert.Equal(expected, result.Text);
    }

    [Fact]
    public void HelpIsRegistryDerived()
    {
        var service = Create();
        Assert.Contains("select frontiers", service.Help("frontiers"));
        Assert.Contains("Canonical command reference", service.Help(null));
        Assert.Contains("Unknown help target", service.Help("unknown"));
    }
}
