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
            var commands = new StartingTileCommands(state);
            commands.Execute("set starting tiles N:S,E:X,S:S,W:X");
            commands.Execute("save starting tiles as test-layout");
            Assert.Contains("restored", commands.Execute("activate tileset test-layout"));
            var reopened = new StartingTilePresets(directory);
            Assert.Equal("test-layout", reopened.ActiveName);
            Assert.True(reopened.TryGet(2, 0, out var tile));
            Assert.Equal(StartShape.X, tile.Shape);
            Assert.Contains("deactivated", new StartingTileCommands(reopened).Execute("deactivate tileset"));
            Assert.Null(new StartingTilePresets(directory).Active);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void CommandsManageActiveTilesAndNamedPresets()
    {
        string directory = Path.Combine(Path.GetTempPath(), "td-tile-cmd-" + Guid.NewGuid());
        try
        {
            var state = new StartingTilePresets(directory);
            var commands = new StartingTileCommands(state);
            Assert.Contains("N:S", commands.Execute("set starting tiles N:S,E:X,S:S,W:S"));
            Assert.Contains("Saved", commands.Execute("save starting tiles as simple"));
            Assert.Contains("simple", commands.Execute("list starting-tile-presets"));
            Assert.Contains("E:X", commands.Execute("list starting-tile-preset simple"));
            Assert.Contains("cleared", commands.Execute("unload starting tiles"));
            Assert.Contains("Loaded", commands.Execute("load starting tiles simple"));
            Assert.Contains("N:S", commands.Execute("list starting-tiles"));
            Assert.Contains("Deleted", commands.Execute("delete starting-tile-preset simple"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
