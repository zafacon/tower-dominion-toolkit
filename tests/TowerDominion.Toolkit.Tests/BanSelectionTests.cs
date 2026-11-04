using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class BanSelectionTests
{
    [Fact]
    public void PersistsIndependentCategoriesAndUnbans()
    {
        string path = Path.Combine(Path.GetTempPath(), "toolkit-bans-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            BanSelections selections = new(path);
            Assert.Equal(1, selections.Change("foes", ["foe-1"], true).Changed);
            Assert.Equal(1, selections.Change("waves", ["wave-1"], true).Changed);
            Assert.True(new BanSelections(path).IsBanned("foes", "foe-1"));
            Assert.False(new BanSelections(path).IsBanned("waves", "foe-1"));
            Assert.Equal(1, selections.Change("foes", ["foe-2"], true).Changed);
            Assert.Equal(2, selections.Change("foes", ["foe-1", "foe-2"], false).Changed);
            Assert.False(new BanSelections(path).IsBanned("foes", "foe-1"));
            Assert.True(new BanSelections(path).IsBanned("waves", "wave-1"));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void InvalidIdsDoNotPartiallyChangeBans()
    {
        string path = Path.Combine(Path.GetTempPath(), "toolkit-bans-" + Guid.NewGuid().ToString("N") + ".json");
        BanSelections selections = new(path, persist: false);
        Assert.Throws<ArgumentException>(() => selections.Change("foes", ["foe-1", ""], true));
        Assert.Empty(selections.List("foes"));
    }
}
