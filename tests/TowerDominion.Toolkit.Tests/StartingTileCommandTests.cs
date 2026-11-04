using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public class StartingTileCommandTests
{
    [Fact]
    public void ActivatedTilesetSurvivesRestartAndCanBeDeactivated()
    {
        string directory = Path.Combine(Path.GetTempPath(), "td-tile-active-" + Guid.NewGuid());
        try
        {
            var state = new StartingTilePresets(directory);
            state.Set("N:S,E:X,S:S,W:X");
            state.Save("test-layout");
            state.Activate("test-layout");
            var reopened = new StartingTilePresets(directory);
            Assert.Equal("test-layout", reopened.ActiveName);
            Assert.True(reopened.TryGet(2, 0, out var tile));
            Assert.Equal(StartShape.X, tile.Shape);
            reopened.Clear();
            Assert.Null(new StartingTilePresets(directory).Active);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void ServiceManagesActiveTilesAndNamedPresets()
    {
        string directory = Path.Combine(Path.GetTempPath(), "td-tile-cmd-" + Guid.NewGuid());
        try
        {
            var state = new StartingTilePresets(directory);
            state.Set("N:S,E:X,S:S,W:S");
            state.Save("simple");
            Assert.Contains("simple", state.Names());
            Assert.Contains("E:X", state.DescribeSaved("simple"));
            state.Clear();
            Assert.Null(state.Active);
            state.Load("simple");
            Assert.Contains("N:S", string.Join(",", state.Active!.Values.Select(StartingTilePresets.Format)));
            Assert.True(state.Delete("simple"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
