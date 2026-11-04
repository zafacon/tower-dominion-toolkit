using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class FrontierSelectionTests
{
    [Fact]
    public void PersistsSelectionsAcrossInstancesAndSupportsAll()
    {
        string path = Path.Combine(Path.GetTempPath(), "toolkit-frontier-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            FrontierSelections selections = new(path);
            Assert.Equal(2, selections.Change(["one", "two"], true).Changed);
            Assert.Equal(2, new FrontierSelections(path).SelectedIds.Count);
            Assert.Equal(0, selections.Change(["one", "two"], true).Changed);
            Assert.Equal(2, selections.Change(["one", "two"], false).Changed);
            Assert.Empty(new FrontierSelections(path).SelectedIds);
            Assert.Equal(1, selections.Change(["one"], true).Changed);
            Assert.Contains("one", new FrontierSelections(path).SelectedIds);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void InvalidIdsCannotPartiallyMutateState()
    {
        string path = Path.Combine(Path.GetTempPath(), "toolkit-frontier-" + Guid.NewGuid().ToString("N") + ".json");
        FrontierSelections selections = new(path);
        Assert.Throws<ArgumentException>(() => selections.Change(["one", ""], true));
        Assert.Empty(selections.SelectedIds);
        Assert.False(File.Exists(path));
    }
}
