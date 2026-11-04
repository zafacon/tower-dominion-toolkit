using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CatalogSelectionRegistrationTests
{
    private sealed class FakeSource : ICommandCatalogSource
    {
        public bool IsReady { get; set; } = true;
        public IReadOnlyList<CatalogEntity> List(string domain) =>
            domain is "foes" or "frontiers" ? [new("a", "Alpha", ["first"]), new("b", "Beta Prime", ["second"]),
                new("5e17d633-d0ae-4777-915a-74bebe281b3f", "5e17d633-d0ae-4777-915a-74bebe281b3f", [])] : [];
    }

    private static (CommandGrammar Grammar, BanSelections Bans, FrontierSelections Frontiers, FakeSource Source) Create()
    {
        var source = new FakeSource();
        var catalog = new CommandCatalogService(source);
        var bans = new BanSelections("unused.json", persist: false);
        var frontiers = new FrontierSelections(Path.Combine(Path.GetTempPath(), "td-reg-" + Guid.NewGuid() + ".json"));
        var grammar = new CommandGrammar();
        CatalogSelectionRegistration.Register(grammar, catalog,
            new CatalogSelectionHandlers(new CatalogSelectionService(catalog), bans, frontiers));
        return (grammar, bans, frontiers, source);
    }

    [Fact]
    public void ParsedBanNamesResolveAtomically()
    {
        var (grammar, bans, _, _) = Create();
        var parser = new CommandTextParser(grammar);
        Assert.True(parser.Execute("ban foes Alpha, \"Beta Prime\"").Success);
        Assert.Equal(2, bans.List("foes").Count);
        Assert.False(parser.Execute("ban foes Alpha, unknown").Success);
        Assert.Equal(2, bans.List("foes").Count);
        Assert.True(parser.Execute("unban foes all").Success);
        Assert.Empty(bans.List("foes"));
    }

    [Fact]
    public void TypedFrontierSelectionsSupportAliasesAndAll()
    {
        var (grammar, _, frontiers, _) = Create();
        var parser = new CommandTextParser(grammar);
        Assert.True(parser.Execute("select frontiers first, second").Success);
        Assert.Equal(2, frontiers.SelectedIds.Count);
        Assert.True(parser.Execute("unselect frontiers all").Success);
        Assert.Empty(frontiers.SelectedIds);
    }

    [Fact]
    public void InvalidOrUnavailableCatalogNeverMutates()
    {
        var (grammar, bans, _, source) = Create();
        var parser = new CommandTextParser(grammar);
        Assert.False(parser.Execute("ban foes Alpha, missing").Success);
        Assert.Empty(bans.List("foes"));
        source.IsReady = false;
        Assert.False(parser.Execute("ban foes all").Success);
        Assert.Empty(bans.List("foes"));
    }

    [Fact]
    public void RegistryHelpAndCompletionUseSameCatalog()
    {
        var (grammar, _, _, _) = Create();
        Assert.Contains("ban foes", new CommandHelp(grammar).Query("ban foes").Syntax.Single());
        var result = new CommandArgumentCompletion(grammar).Complete("select frontiers Be", 19);
        Assert.Equal("select frontiers \"Beta Prime\"", result.Completion.Text);
        Assert.True(new CommandTextParser(grammar).Execute(result.Completion.Text).Success);
    }

    [Fact]
    public void CompletionShowsNamesNotIdsOrAliasesWhileParserStillAcceptsThem()
    {
        var (grammar, bans, _, _) = Create();
        var completion = new CommandArgumentCompletion(grammar).Complete("ban foes ", 9);
        Assert.Contains("Alpha", completion.Completion.Candidates);
        Assert.Contains("Beta Prime", completion.Completion.Candidates);
        Assert.DoesNotContain("a", completion.Completion.Candidates);
        Assert.DoesNotContain("b", completion.Completion.Candidates);
        Assert.DoesNotContain("first", completion.Completion.Candidates);
        Assert.DoesNotContain("5e17d633-d0ae-4777-915a-74bebe281b3f", completion.Completion.Candidates);
        Assert.True(new CommandTextParser(grammar).Execute("ban foes a").Success);
        Assert.True(new CommandTextParser(grammar).Execute("ban foes second").Success);
        Assert.Equal(2, bans.List("foes").Count);
    }

    [Fact]
    public void AllOperationsAreRegisteredForExpectedDomains()
    {
        var (grammar, _, _, _) = Create();
        Assert.Equal(10, grammar.Operations.Count);
        Assert.True(grammar.Supports("ban", "waves"));
        Assert.True(grammar.Supports("unban", "doctrines"));
        Assert.True(grammar.Supports("select", "frontiers"));
        Assert.False(grammar.Supports("ban", "frontiers"));
    }
}
