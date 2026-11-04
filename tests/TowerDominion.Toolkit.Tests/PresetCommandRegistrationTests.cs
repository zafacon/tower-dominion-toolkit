using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class PresetCommandRegistrationTests
{
    private sealed class Rule(string id) : IChallengeRule
    {
        public ChallengeDescriptor Descriptor { get; } = new(id, id, "Test", new HashSet<ChallengeActionKind>());
        public ChallengeDecision Evaluate(ChallengeAction action) => ChallengeDecision.Permit();
    }

    [Fact]
    public void AllFourDomainsRegisterAndRoundTripThroughParser()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-preset-cmd-" + Guid.NewGuid());
        try
        {
            var bans = new BanSelections("unused", persist: false);
            var frontiers = new FrontierSelections(Path.Combine(root, "frontiers.json"));
            var registry = new ChallengeRegistry();
            registry.Register(new Rule("one"));
            var challenges = new ChallengeSelections(Path.Combine(root, "challenges.json"), registry);
            var tiles = new StartingTilePresets(Path.Combine(root, "tiles"));
            var grammar = new CommandGrammar();
            grammar.RegisterDomain(new("bans", "bans"));
            grammar.RegisterDomain(new("frontiers", "frontiers"));
            PresetCommandRegistration.Register(grammar,
                new SelectionPresetAdapters(Path.Combine(root, "presets"), bans, frontiers),
                new ChallengePresetAdapter(Path.Combine(root, "challenge-presets"), registry, challenges),
                new StartingTilePresetAdapter(tiles));
            Assert.Equal(20, grammar.Operations.Count);
            var parser = new CommandTextParser(grammar);
            bans.Change("foes", ["foe"], true);
            frontiers.Change(["frontier"], true);
            challenges.Change(["one"], true);
            tiles.SetRelative(1, "S");
            foreach (string domain in new[] { "bans", "frontiers", "challenges", "starting tiles" })
            {
                Assert.True(parser.Execute($"save {domain} as My Setup").Success);
                Assert.Contains("My Setup", parser.Execute($"list {domain} presets").Message);
                Assert.True(parser.Execute($"inspect {domain} preset My Setup").Success);
            }
            bans.Replace(new Dictionary<string, string[]>());
            frontiers.Replace([]);
            challenges.Clear();
            tiles.ClearRelative(1);
            foreach (string domain in new[] { "bans", "frontiers", "challenges", "starting tiles" })
            {
                var loaded = parser.Execute($"load {domain} My Setup");
                Assert.True(loaded.Success);
                Assert.Equal("My Setup", loaded.Details.Data!["preset"]);
                Assert.NotEmpty(loaded.Details.Changes!);
                Assert.All(loaded.Details.Changes!, change => Assert.NotEqual(
                    string.Join(",", Assert.IsType<string[]>(change.Before)),
                    string.Join(",", Assert.IsType<string[]>(change.After))));
                var repeated = parser.Execute($"load {domain} My Setup");
                Assert.True(repeated.Success);
                Assert.Empty(repeated.Details.Changes!);
            }
            Assert.True(bans.IsBanned("foes", "foe"));
            Assert.True(frontiers.IsSelected("frontier"));
            Assert.Equal(["one"], challenges.Selected);
            Assert.Equal(["S"], tiles.Relative[1]);
            Assert.True(parser.Execute("delete bans preset My Setup").Success);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void MissingPresetAndRunLockProduceFailures()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-preset-cmd-" + Guid.NewGuid());
        try
        {
            var bans = new BanSelections("unused", persist: false);
            var frontiers = new FrontierSelections(Path.Combine(root, "frontiers.json"));
            var registry = new ChallengeRegistry();
            registry.Register(new Rule("one"));
            var challenges = new ChallengeSelections(Path.Combine(root, "challenges.json"), registry);
            var grammar = new CommandGrammar();
            PresetCommandRegistration.Register(grammar,
                new SelectionPresetAdapters(Path.Combine(root, "presets"), bans, frontiers),
                new ChallengePresetAdapter(Path.Combine(root, "challenge-presets"), registry, challenges),
                new StartingTilePresetAdapter(new StartingTilePresets(Path.Combine(root, "tiles"))));
            var parser = new CommandTextParser(grammar);
            var missing = parser.Execute("load bans missing");
            Assert.False(missing.Success);
            Assert.Equal(CommandErrorKind.Validation, missing.Details.Error);
            Assert.Null(missing.Details.Changes);
            challenges.Change(["one"], true);
            Assert.True(parser.Execute("save challenges as Locked").Success);
            challenges.StartRun();
            var locked = parser.Execute("load challenges Locked");
            Assert.False(locked.Success);
            Assert.Equal(CommandErrorKind.Conflict, locked.Details.Error);
            Assert.Null(locked.Details.Changes);
            Assert.Equal(["one"], challenges.Active);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
