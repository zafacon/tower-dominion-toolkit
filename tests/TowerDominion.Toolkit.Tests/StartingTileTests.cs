using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public class StartingTileTests
{
    [Theory]
    [InlineData("N:L.C,E:S,S:T.A,W:X")]
    [InlineData("N:T,E:T.C,S:T.A,W:S")]
    [InlineData("N:S,E:S,S:S,W:S")]
    public void ParserAndFormatterRoundTrip(string specification)
    {
        var parsed = StartingTiles.ParseSet(specification);
        Assert.Equal(4, parsed.Count);
        var formatted = string.Join(",", parsed.Values.Select(StartingTilePresets.Format));
        var reparsed = StartingTiles.ParseSet(formatted);
        foreach (var pair in parsed) Assert.Equal(pair.Value.Paths, reparsed[pair.Key].Paths);
    }

    [Fact]
    public void PresetsPersistButActiveSelectionDoesNot()
    {
        var directory = Path.Combine(Path.GetTempPath(), "td-tiles-" + Guid.NewGuid());
        try
        {
            var presets = new StartingTilePresets(directory);
            presets.Set("N:S,E:T.C,S:L.A,W:X");
            Assert.True(presets.TryGet(0, 2, out var north));
            Assert.Equal(StartShape.S, north.Shape);
            presets.Save("test");
            var reopened = new StartingTilePresets(directory);
            Assert.Null(reopened.Active);
            Assert.Equal(4, reopened.Load("test"));
            Assert.True(reopened.TryGet(2, 0, out var east));
            Assert.Equal(StartShape.T, east.Shape);
            reopened.Clear();
            Assert.Null(reopened.Active);
            Assert.Single(reopened.Names());
            Assert.True(reopened.Delete("test"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void OverridesAreLimitedToRequestedHqEntranceCoordinates()
    {
        var directory = Path.Combine(Path.GetTempPath(), "td-entrances-" + Guid.NewGuid());
        try
        {
            var presets = new StartingTilePresets(directory);
            presets.Set("N:L.C,E:T.C,S:T.A,W:X");
            foreach (var (x, y, position) in new[]
            {
                (0, 2, StartPosition.N), (2, 0, StartPosition.E),
                (0, -2, StartPosition.S), (-2, 0, StartPosition.W)
            })
            {
                Assert.True(presets.TryGet(x, y, out var tile));
                Assert.Equal(position, tile.Position);
            }
            foreach (var (x, y) in new[] { (0, 0), (1, 0), (0, 1), (1, 2), (2, 2), (-2, -2), (3, 0) })
                Assert.False(presets.TryGet(x, y, out _));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("N:S,E:S,S:S")]
    [InlineData("N:S,N:T,E:S,S:S,W:S")]
    [InlineData("N:L,E:S,S:S,W:S")]
    public void InvalidConfigurationsRejected(string specification) => Assert.Throws<ArgumentException>(() => StartingTiles.ParseSet(specification));
}
