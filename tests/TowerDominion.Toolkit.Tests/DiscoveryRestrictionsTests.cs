using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class DiscoveryRestrictionsTests
{
    [Fact]
    public void ExclusionsFollowExclusiveRuleState()
    {
        string dir = Path.Combine(Path.GetTempPath(), "td-discovery-" + Guid.NewGuid().ToString("N"));
        try
        {
            var rules = new GameplayRules(Path.Combine(dir, "rules.json"));
            string[] shared = ["D1_05", "D1_37", "D2_18", "TBCB07D7"];
            string[] dependent = ["D1_09", "D1_23", "D1_32"];
            Assert.All(shared.Concat(dependent), id => Assert.False(DiscoveryRestrictions.IsDoctrineExcluded(rules, id)));
            rules.Change(["no-discovery"], true);
            Assert.All(shared.Concat(dependent), id => Assert.True(DiscoveryRestrictions.IsDoctrineExcluded(rules, id)));
            Assert.False(DiscoveryRestrictions.IsNeutralExcluded(rules, "NB15"));
            rules.Change(["full-discovery"], true);
            Assert.False(rules.IsEnabled("no-discovery"));
            Assert.All(shared, id => Assert.True(DiscoveryRestrictions.IsDoctrineExcluded(rules, id)));
            Assert.All(dependent, id => Assert.False(DiscoveryRestrictions.IsDoctrineExcluded(rules, id)));
            Assert.True(DiscoveryRestrictions.IsNeutralExcluded(rules, "NB15"));
            Assert.False(DiscoveryRestrictions.IsNeutralExcluded(rules, "FB05"));
            Assert.False(DiscoveryRestrictions.IsDoctrineExcluded(rules, "D1_01"));
            Assert.Throws<ArgumentException>(() => rules.Replace(["no-discovery", "full-discovery"]));
            Assert.True(new GameplayRules(Path.Combine(dir, "rules.json")).IsEnabled("full-discovery"));
            rules.Change(GameplayRules.Catalog.Select(r => r.Id), false);
            Assert.Empty(rules.Enabled);
            Assert.False(DiscoveryRestrictions.IsNeutralExcluded(rules, "NB15"));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}
