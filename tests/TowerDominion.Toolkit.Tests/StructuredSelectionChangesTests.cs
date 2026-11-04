using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class StructuredSelectionChangesTests
{
    private sealed class Source : ICommandCatalogSource
    {
        public bool IsReady => true;
        public IReadOnlyList<CatalogEntity> List(string domain) =>
            domain is "foes" or "frontiers" ? [new("alpha", "Alpha", [])] : [];
    }

    [Fact]
    public void BanAndFrontierMutationsReturnCommittedBeforeAfterSnapshots()
    {
        string path = Path.Combine(Path.GetTempPath(), "td-structured-" + Guid.NewGuid() + ".json");
        try
        {
            var catalog = new CommandCatalogService(new Source());
            var bans = new BanSelections("unused", persist: false);
            var frontiers = new FrontierSelections(path);
            var grammar = new CommandGrammar();
            CatalogSelectionRegistration.Register(grammar, catalog,
                new CatalogSelectionHandlers(new CatalogSelectionService(catalog), bans, frontiers));
            var parser = new CommandTextParser(grammar);
            var ban = parser.Execute("ban foes Alpha");
            Assert.True(ban.Success);
            Assert.Equal(1, ban.Details.Data!["changed"]);
            Assert.Equal("banned", ban.Details.Changes![0].Property);
            Assert.Empty(Assert.IsType<string[]>(ban.Details.Changes[0].Before));
            Assert.Equal(["alpha"], Assert.IsType<string[]>(ban.Details.Changes[0].After));
            var repeated = parser.Execute("ban foes Alpha");
            Assert.True(repeated.Success);
            Assert.Equal(0, repeated.Details.Data!["changed"]);
            Assert.Empty(repeated.Details.Changes!);
            var selected = parser.Execute("select frontiers Alpha");
            Assert.True(selected.Success);
            Assert.Equal("selected", selected.Details.Changes![0].Property);
            Assert.Equal(["alpha"], Assert.IsType<string[]>(selected.Details.Changes[0].After));
            var removed = parser.Execute("unselect frontiers Alpha");
            Assert.True(removed.Success);
            Assert.Equal(["alpha"], Assert.IsType<string[]>(removed.Details.Changes![0].Before));
            Assert.Empty(Assert.IsType<string[]>(removed.Details.Changes[0].After));
            var rejected = parser.Execute("ban foes unknown");
            Assert.False(rejected.Success);
            Assert.Null(rejected.Details.Changes);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
