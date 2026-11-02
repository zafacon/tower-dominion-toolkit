using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public class RelativeStartingTileCommandTests
{
    [Fact]
    public void RelativeSettingsPersistAndMapWithoutChangingCompassPreset()
    {
        var directory = Path.Combine(Path.GetTempPath(), "td-relative-" + Guid.NewGuid());
        try
        {
            var state = new StartingTilePresets(directory);
            var commands = new StartingTileCommands(state);
            Assert.Contains("D3 relative tiles saved", commands.Execute("set relative tiles D3 L.C,T.A,X"));
            Assert.Contains("D1 relative tiles saved", commands.Execute("set relative tiles D1 S"));
            Assert.Contains("D3", commands.Execute("list relative tiles"));
            Assert.Contains("requires 2", commands.Execute("set relative tiles D2 S"));
            var reopened = new StartingTilePresets(directory);
            Assert.True(reopened.TryGetRelative(3, [StartPosition.S, StartPosition.W, StartPosition.E], out var mapped));
            Assert.Equal(StartShape.L, mapped[StartPosition.E].Shape);
            Assert.Equal(3, mapped.Count);
            Assert.Contains("cleared", new StartingTileCommands(reopened).Execute("clear relative tiles D3"));
            Assert.False(new StartingTilePresets(directory).Relative.ContainsKey(3));
            Assert.True(new StartingTilePresets(directory).Relative.ContainsKey(1));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
