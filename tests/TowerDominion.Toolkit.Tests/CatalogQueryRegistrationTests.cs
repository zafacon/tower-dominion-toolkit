using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CatalogQueryRegistrationTests
{
    private sealed class FakeSource : ICommandCatalogSource
    {
        public bool IsReady { get; set; } = true;
        public IReadOnlyList<CatalogEntity> List(string domain) => domain is "foes" or "frontiers"
            ? [new("id-a", "Alpha", []), new("id-b", "Beta Prime", [])] : [];
    }

    private static (CommandGrammar Grammar, BanSelections Bans, FrontierSelections Frontiers, FakeSource Source) Create()
    {
        var source = new FakeSource();
        var catalog = new CommandCatalogService(source);
        var bans = new BanSelections("unused.json", persist: false);
        var frontiers = new FrontierSelections(Path.Combine(Path.GetTempPath(), "td-query-" + Guid.NewGuid() + ".json"));
        var grammar = new CommandGrammar();
        CatalogSelectionRegistration.Register(grammar, catalog,
            new CatalogSelectionHandlers(new CatalogSelectionService(catalog), bans, frontiers));
        CatalogQueryRegistration.Register(grammar, catalog, bans, frontiers);
        return (grammar, bans, frontiers, source);
    }

    [Fact]
    public void ListCatalogAndQualifiedBansUseCurrentState()
    {
        var (grammar, _, _, _) = Create();
        var parser = new CommandTextParser(grammar);
        Assert.Contains("Beta Prime", parser.Execute("list foes").Message);
        Assert.True(parser.Execute("ban foes Alpha").Success);
        Assert.Contains("Alpha", parser.Execute("list banned foes").Message);
        Assert.DoesNotContain("Beta Prime", parser.Execute("list banned foes").Message);
    }

    [Fact]
    public void GlobalBansAndClearResetAllCategories()
    {
        var (grammar, bans, _, _) = Create();
        var parser = new CommandTextParser(grammar);
        Assert.True(parser.Execute("ban foes all").Success);
        Assert.Contains("foes: Alpha", parser.Execute("list bans").Message);
        Assert.True(parser.Execute("clear bans").Success);
        Assert.Empty(bans.List("foes"));
        Assert.DoesNotContain("foes:", parser.Execute("list bans").Message);
    }

    [Fact]
    public void SelectedFrontiersCanBeListedAndCleared()
    {
        var (grammar, _, frontiers, _) = Create();
        var parser = new CommandTextParser(grammar);
        Assert.True(parser.Execute("select frontiers Alpha").Success);
        Assert.Contains("Alpha", parser.Execute("list selected frontiers").Message);
        Assert.True(parser.Execute("clear frontiers").Success);
        Assert.Empty(frontiers.SelectedIds);
    }

    [Fact]
    public void UnavailableCatalogFailsQueriesButStillAllowsClearing()
    {
        var (grammar, bans, _, source) = Create();
        var parser = new CommandTextParser(grammar);
        Assert.True(parser.Execute("ban foes Alpha").Success);
        source.IsReady = false;
        Assert.False(parser.Execute("list foes").Success);
        Assert.False(parser.Execute("list banned foes").Success);
        Assert.Contains("id-a", parser.Execute("list bans").Message);
        Assert.True(parser.Execute("clear bans").Success);
        Assert.Empty(bans.List("foes"));
    }

    [Fact]
    public void AllReadOnlyAndResetCommandsHaveRegistryHelp()
    {
        var (grammar, _, _, _) = Create();
        Assert.Equal(23, grammar.Operations.Count);
        foreach (string command in new[] { "list foes", "list banned waves", "list frontiers", "list selected frontiers", "list bans", "clear bans", "clear frontiers" })
        {
            var parsed = new CommandTextParser(grammar).Parse(command);
            Assert.NotNull(parsed.Invocation);
            Assert.True(new CommandHelp(grammar).Query($"{parsed.Invocation.Verb} {parsed.Invocation.Domain}").Success);
        }
    }
}
