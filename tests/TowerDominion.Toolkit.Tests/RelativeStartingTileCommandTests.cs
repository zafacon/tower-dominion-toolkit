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
            state.SetRelative(3, "L.C,T.A,X");
            state.SetRelative(1, "S");
            Assert.True(state.Relative.ContainsKey(3));
            Assert.Throws<ArgumentException>(() => state.SetRelative(2, "S"));
            var reopened = new StartingTilePresets(directory);
            Assert.True(reopened.TryGetRelative(3, [StartPosition.S, StartPosition.W, StartPosition.E], out var mapped));
            Assert.Equal(StartShape.L, mapped[StartPosition.E].Shape);
            Assert.Equal(3, mapped.Count);
            reopened.ClearRelative(3);
            Assert.False(new StartingTilePresets(directory).Relative.ContainsKey(3));
            Assert.True(new StartingTilePresets(directory).Relative.ContainsKey(1));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
