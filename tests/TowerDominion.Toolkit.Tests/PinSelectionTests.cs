using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class PinSelectionTests
{
    private sealed class Source : ICommandCatalogSource
    {
        public bool IsReady => true;
        public IReadOnlyList<CatalogEntity> List(string domain) => domain is "foes" or "waves"
            ? [new("a", "Alpha", []), new("b", "Beta", [])] : [];
    }

    [Fact]
    public void PinBanConflictsRejectAtomicallyInBothDirections()
    {
        var pins = new PinSelections();
        var bans = new BanSelections("unused", persist: false);
        var catalog = new CommandCatalogService(new Source());
        var grammar = new CommandGrammar();
        CatalogSelectionRegistration.Register(grammar, catalog,
            new CatalogSelectionHandlers(new CatalogSelectionService(catalog), bans,
                new FrontierSelections(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")), pins));
        var parser = new CommandTextParser(grammar);
        Assert.True(parser.Execute("ban foes Alpha").Success);
        var blocked = parser.Execute("pin foes Alpha, Beta");
        Assert.False(blocked.Success);
        Assert.Contains("Alpha", blocked.Message);
        Assert.DoesNotContain("Cannot pin a:", blocked.Message);
        Assert.Contains("banned", blocked.Message);
        Assert.Empty(pins.List("foes"));
        Assert.True(parser.Execute("pin foes Beta").Success);
        blocked = parser.Execute("ban foes Beta");
        Assert.False(blocked.Success);
        Assert.Contains("Beta", blocked.Message);
        Assert.DoesNotContain("Cannot ban b:", blocked.Message);
        Assert.Contains("pinned", blocked.Message);
        Assert.False(bans.IsBanned("foes", "b"));
        Assert.True(parser.Execute("unpin foes Beta").Success);
        Assert.True(parser.Execute("ban foes Beta").Success);
    }

    [Theory]
    [InlineData("foes")]
    [InlineData("waves")]
    public void BanPinConflictsAreAtomicForBothEncounterCatalogs(string domain)
    {
        var pins = new PinSelections();
        var bans = new BanSelections("unused", persist: false);
        var catalog = new CommandCatalogService(new Source());
        var grammar = new CommandGrammar();
        CatalogSelectionRegistration.Register(grammar, catalog,
            new CatalogSelectionHandlers(new CatalogSelectionService(catalog), bans,
                new FrontierSelections(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")), pins));
        var parser = new CommandTextParser(grammar);
        Assert.True(parser.Execute($"ban {domain} Alpha").Success);
        var conflict = parser.Execute($"pin {domain} Alpha, Beta");
        Assert.False(conflict.Success);
        Assert.Contains("Alpha", conflict.Message);
        Assert.Empty(pins.List(domain));
        Assert.True(parser.Execute($"unban {domain} Alpha").Success);
        Assert.True(parser.Execute($"pin {domain} Beta").Success);
        conflict = parser.Execute($"ban {domain} Alpha, Beta");
        Assert.False(conflict.Success);
        Assert.Contains("Beta", conflict.Message);
        Assert.Empty(bans.List(domain));
        Assert.True(parser.Execute($"unpin {domain} all").Success);
        Assert.Empty(pins.List(domain));
    }

    [Fact]
    public void ClearPinsRemovesBothDomainsWithoutAffectingSavedPresetsOrBans()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-pin-clear-" + Guid.NewGuid());
        try
        {
            var pins = new PinSelections();
            var bans = new BanSelections("unused", persist: false);
            var catalog = new CommandCatalogService(new Source());
            var frontiers = new FrontierSelections(Path.Combine(root, "frontiers.json"));
            var presets = new SelectionPresetAdapters(root, bans, frontiers, pins);
            var grammar = new CommandGrammar();
            CatalogSelectionRegistration.Register(grammar, catalog,
                new CatalogSelectionHandlers(new CatalogSelectionService(catalog), bans, frontiers, pins));
            CatalogQueryRegistration.Register(grammar, catalog, bans, frontiers, pins);
            var parser = new CommandTextParser(grammar);
            Assert.True(parser.Execute("pin foes Alpha").Success);
            Assert.True(parser.Execute("pin waves Beta").Success);
            presets.Save("pins", "saved");
            bans.Change("doctrines", ["unrelated"], true);

            var listing = parser.Execute("list pins");
            Assert.True(listing.Success);
            Assert.Contains("foes: Alpha", listing.Message);
            Assert.Contains("waves: Beta", listing.Message);
            Assert.DoesNotContain("foes: a", listing.Message);
            Assert.DoesNotContain("waves: b", listing.Message);

            var cleared = parser.Execute("clear pins");
            Assert.True(cleared.Success);
            Assert.Empty(pins.List("foes"));
            Assert.Empty(pins.List("waves"));
            Assert.True(bans.IsBanned("doctrines", "unrelated"));
            Assert.Contains("saved", presets.Names("pins"));
            Assert.True(parser.Execute("clear pins").Success);
            presets.Load("pins", "saved");
            Assert.True(pins.IsPinned("foes", "a"));
            Assert.True(pins.IsPinned("waves", "b"));
            Assert.True(parser.Execute("unpin foes all").Success);
            Assert.Empty(pins.List("foes"));
            Assert.True(pins.IsPinned("waves", "b"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void PinPresetsAreExplicitAndConflictCheckedOnLoad()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-pin-" + Guid.NewGuid());
        try
        {
            var pins = new PinSelections();
            var bans = new BanSelections("unused", persist: false);
            var frontiers = new FrontierSelections(Path.Combine(root, "frontiers.json"));
            var presets = new SelectionPresetAdapters(root, bans, frontiers, pins);
            pins.Change("waves", ["a"], true);
            presets.Save("pins", "Example");
            pins.Replace(new Dictionary<string, string[]>());
            Assert.Empty(pins.List("waves"));
            presets.Load("pins", "Example");
            Assert.True(pins.IsPinned("waves", "a"));
            pins.Replace(new Dictionary<string, string[]>());
            bans.Change("waves", ["a"], true);
            Assert.Throws<InvalidOperationException>(() => presets.Load("pins", "Example"));
            Assert.Empty(pins.List("waves"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
