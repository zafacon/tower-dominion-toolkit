using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class StructuredResetChangesTests
{
    private sealed class Source : ICommandCatalogSource
    {
        public bool IsReady => true;
        public IReadOnlyList<CatalogEntity> List(string domain) =>
            domain is "foes" or "frontiers" or "waves" ? [new("a", "Alpha", [])] : [];
    }

    [Fact]
    public void ClearOperationsReportOnlyActualStateChanges()
    {
        string path = Path.Combine(Path.GetTempPath(), "td-reset-" + Guid.NewGuid() + ".json");
        try
        {
            var catalog = new CommandCatalogService(new Source());
            var bans = new BanSelections("unused", persist: false);
            var frontiers = new FrontierSelections(path);
            var grammar = new CommandGrammar();
            CatalogSelectionRegistration.Register(grammar, catalog,
                new CatalogSelectionHandlers(new CatalogSelectionService(catalog), bans, frontiers));
            CatalogQueryRegistration.Register(grammar, catalog, bans, frontiers);
            var parser = new CommandTextParser(grammar);
            Assert.True(parser.Execute("ban foes Alpha").Success);
            Assert.True(parser.Execute("ban waves Alpha").Success);
            Assert.True(parser.Execute("select frontiers Alpha").Success);
            var resetBans = parser.Execute("clear bans");
            Assert.True(resetBans.Success);
            Assert.Equal(2, resetBans.Details.Data!["cleared"]);
            Assert.Equal(2, resetBans.Details.Changes!.Count);
            Assert.All(resetBans.Details.Changes, change =>
            {
                Assert.Equal("banned", change.Property);
                Assert.Equal(["a"], Assert.IsType<string[]>(change.Before));
                Assert.Empty(Assert.IsType<string[]>(change.After));
            });
            Assert.Empty(parser.Execute("clear bans").Details.Changes!);
            var resetFrontiers = parser.Execute("clear frontiers");
            Assert.True(resetFrontiers.Success);
            Assert.Equal(1, resetFrontiers.Details.Data!["cleared"]);
            Assert.Equal("selected", resetFrontiers.Details.Changes![0].Property);
            Assert.Equal(["a"], Assert.IsType<string[]>(resetFrontiers.Details.Changes[0].Before));
            Assert.Empty(Assert.IsType<string[]>(resetFrontiers.Details.Changes[0].After));
            Assert.Empty(parser.Execute("clear frontiers").Details.Changes!);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
