using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class GameplayRulesTests
{
    [Fact]
    public void AbundantTilesIsAStandardPersistentGameplayRule()
    {
        string dir = Path.Combine(Path.GetTempPath(), "td-toolkit-rules-" + Guid.NewGuid());
        try
        {
            string path = Path.Combine(dir, "rules.json");
            var rules = new GameplayRules(path);
            Assert.Contains(GameplayRules.Catalog, r => r.Id == "abundant-tiles");

            rules.Change(["abundant-tiles"], true);

            Assert.True(rules.IsEnabled("abundant-tiles"));
            Assert.True(new GameplayRules(path).IsEnabled("abundant-tiles"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void NoDuplicateRewardsIsAStandardPersistentGameplayRule()
    {
        string dir = Path.Combine(Path.GetTempPath(), "td-toolkit-rules-" + Guid.NewGuid());
        try
        {
            string path = Path.Combine(dir, "rules.json");
            var rules = new GameplayRules(path);
            Assert.Contains(GameplayRules.Catalog, r => r.Id == "no-duplicate-rewards");

            rules.Change(["no-duplicate-rewards"], true);

            Assert.True(rules.IsEnabled("no-duplicate-rewards"));
            Assert.True(new GameplayRules(path).IsEnabled("no-duplicate-rewards"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    private static readonly string[] NoDiscovery = ["no-discovery"];
    private static readonly string[] Bryta = ["bryta-nest-reroll"];
    private static readonly string[] Invalid = ["no-discovery", "not-a-rule"];
    [Fact]
    public void DefaultsOffAndPersistsAcrossRestart()
    {
        string dir = Path.Combine(Path.GetTempPath(), "td-rules-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(dir, "rules.json");
            var rules = new GameplayRules(path);
            Assert.Empty(rules.Enabled);
            rules.Change(NoDiscovery, true);
            Assert.True(new GameplayRules(path).IsEnabled("no-discovery"));
            rules.Change(NoDiscovery, false);
            Assert.Empty(new GameplayRules(path).Enabled);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void InvalidBatchDoesNotMutateState()
    {
        string dir = Path.Combine(Path.GetTempPath(), "td-rules-" + Guid.NewGuid().ToString("N"));
        try
        {
            var rules = new GameplayRules(Path.Combine(dir, "rules.json"));
            Assert.Throws<ArgumentException>(() => rules.Change(Invalid, true));
            Assert.Empty(rules.Enabled);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public void PresetRegistrationAndReplacementWork()
    {
        string dir = Path.Combine(Path.GetTempPath(), "td-rules-" + Guid.NewGuid().ToString("N"));
        try
        {
            var rules = new GameplayRules(Path.Combine(dir, "rules.json"));
            var grammar = new CommandGrammar();
            GameplayRuleCommandRegistration.Register(grammar, rules, Path.Combine(dir, "presets"));
            Assert.Contains(grammar.ForDomain("rules"), x => x.Verb == "enable");
            Assert.Contains(grammar.ForDomain("rules"), x => x.Verb == "load");
            rules.Change(NoDiscovery, true);
            var store = new CommandPresetStore<string[]>(Path.Combine(dir, "presets"), ids =>
            {
                try { GameplayRules.Validate(ids); return null; }
                catch (ArgumentException e) { return e.Message; }
            });
            store.Save("example", rules.Enabled.ToArray());
            rules.Replace(Bryta);
            rules.Replace(store.Read("example"));
            Assert.Equal(NoDiscovery, rules.Enabled);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}
