using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class Phase5AcceptanceTests
{
    private sealed class Source : ICommandCatalogSource
    {
        public bool IsReady => true;
        public IReadOnlyList<CatalogEntity> List(string domain) =>
            domain is "foes" or "frontiers" ? [new("a", "Alpha", [])] : [];
    }

    private sealed class Rule : IChallengeRule
    {
        public ChallengeDescriptor Descriptor { get; } = new("one", "One", "Test", new HashSet<ChallengeActionKind>());
        public ChallengeDecision Evaluate(ChallengeAction action) => ChallengeDecision.Permit();
    }

    [Fact]
    public void CanonicalDomainsCoexistAndShareStateWithPresets()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-phase5-" + Guid.NewGuid());
        try
        {
            var grammar = new CommandGrammar();
            var catalog = new CommandCatalogService(new Source());
            var bans = new BanSelections(Path.Combine(root, "bans.json"));
            var frontiers = new FrontierSelections(Path.Combine(root, "frontiers.json"));
            var registry = new ChallengeRegistry();
            registry.Register(new Rule());
            var challenges = new ChallengeSelections(Path.Combine(root, "challenges.json"), registry);
            var tiles = new StartingTilePresets(Path.Combine(root, "tiles"));
            CatalogSelectionRegistration.Register(grammar, catalog,
                new CatalogSelectionHandlers(new CatalogSelectionService(catalog), bans, frontiers));
            CatalogQueryRegistration.Register(grammar, catalog, bans, frontiers);
            ChallengeCommandRegistration.Register(grammar, registry, challenges);
            StartingTileCommandRegistration.Register(grammar, tiles);
            var seeds = new SeedService();
            bool diagnostics = false;
            RuntimeCommandRegistration.Register(grammar, seeds, () => diagnostics, value => diagnostics = value, () => "ready");
            PresetCommandRegistration.Register(grammar,
                new SelectionPresetAdapters(Path.Combine(root, "presets"), bans, frontiers),
                new ChallengePresetAdapter(Path.Combine(root, "challenge-presets"), registry, challenges),
                new StartingTilePresetAdapter(tiles));
            var parser = new CommandTextParser(grammar);
            var ban = parser.Execute("ban foes Alpha");
            Assert.True(ban.Success);
            Assert.Single(ban.Details.Changes!);
            Assert.Empty(parser.Execute("ban foes Alpha").Details.Changes!);
            Assert.True(parser.Execute("select frontiers Alpha").Success);
            Assert.True(parser.Execute("select challenges one").Success);
            Assert.True(parser.Execute("set starting tiles S").Success);
            var seedSet = parser.Execute("set seed 123");
            Assert.True(seedSet.Success);
            Assert.Single(seedSet.Details.Changes!);
            Assert.True(parser.Execute("set frontier diagnostics on").Success);
            Assert.True(diagnostics);
            Assert.Equal(true, parser.Execute("status frontier diagnostics").Details.Data!["enabled"]);
            foreach (string domain in new[] { "bans", "frontiers", "challenges", "starting tiles" })
                Assert.True(parser.Execute($"save {domain} as My Setup").Success);
            Assert.Contains("My Setup", new CommandArgumentCompletion(grammar)
                .Complete("load frontiers My", "load frontiers My".Length).Completion.Candidates);
            Assert.Contains("load frontiers <name>", new CommandHelp(grammar).Query("frontiers").Syntax);
            var invalidBan = parser.Execute("ban foes missing");
            Assert.False(invalidBan.Success);
            Assert.Equal(CommandErrorKind.Validation, invalidBan.Details.Error);
            Assert.Null(invalidBan.Details.Changes);
            Assert.True(bans.IsBanned("foes", "a"));
            var clearBans = parser.Execute("clear bans");
            Assert.True(clearBans.Success);
            Assert.NotEmpty(clearBans.Details.Changes!);
            Assert.Empty(parser.Execute("clear bans").Details.Changes!);
            Assert.True(parser.Execute("clear frontiers").Success);
            Assert.True(parser.Execute("clear challenges").Success);
            Assert.True(parser.Execute("clear starting tiles").Success);
            foreach (string domain in new[] { "bans", "frontiers", "challenges", "starting tiles" })
            {
                var loaded = parser.Execute($"load {domain} My Setup");
                Assert.True(loaded.Success);
                Assert.NotEmpty(loaded.Details.Changes!);
                Assert.Empty(parser.Execute($"load {domain} My Setup").Details.Changes!);
            }
            Assert.True(bans.IsBanned("foes", "a"));
            Assert.True(frontiers.IsSelected("a"));
            Assert.Equal(["one"], challenges.Selected);
            Assert.Equal(["S"], tiles.Relative[1]);
            Assert.Equal("ready", parser.Execute("status toolkit").Message);
            challenges.StartRun();
            var blocked = parser.Execute("unselect challenges one");
            Assert.False(blocked.Success);
            Assert.Equal(CommandErrorKind.Conflict, blocked.Details.Error);
            Assert.Null(blocked.Details.Changes);
            Assert.False(parser.Execute("load challenges My Setup").Success);
            Assert.Equal(["one"], challenges.Active);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
