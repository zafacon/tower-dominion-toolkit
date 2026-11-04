using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public class ChallengeDisplayTests
{
    [Fact]
    public void UsesSelectedBeforeRunAndLockedSnapshotDuringRun()
    {
        var registry = new ChallengeRegistry();
        registry.Register(new CheapestTowerOnlyRule());
        string path = Path.Combine(Path.GetTempPath(), "td-display-" + Guid.NewGuid() + ".json");
        try
        {
            var selections = new ChallengeSelections(path, registry);
            Assert.Empty(ChallengeDisplay.VisibleIds(selections, false));
            selections.Change(new[] { CheapestTowerOnlyRule.Id }, true);
            Assert.Single(ChallengeDisplay.VisibleIds(selections, false));
            selections.StartRun();
            Assert.Single(ChallengeDisplay.VisibleIds(selections, true));
            Assert.Contains("Cheapest", ChallengeDisplay.Describe(registry, ChallengeDisplay.VisibleIds(selections, true)));
            selections.EndRun();
            Assert.Empty(ChallengeDisplay.VisibleIds(selections, true));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
