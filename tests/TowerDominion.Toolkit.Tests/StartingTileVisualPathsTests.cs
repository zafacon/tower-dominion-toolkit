using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public class StartingTileVisualPathsTests
{
    [Theory]
    [InlineData("N:L.C", CardinalPaths.N | CardinalPaths.E)]
    [InlineData("E:T", CardinalPaths.N | CardinalPaths.S | CardinalPaths.W)]
    [InlineData("S:T", CardinalPaths.S | CardinalPaths.E | CardinalPaths.W)]
    [InlineData("W:L.A", CardinalPaths.N | CardinalPaths.E)]
    public void NativeBoundaryUsesExpectedOrientation(string specification, CardinalPaths expected)
    {
        Assert.Equal(expected, StartingTileVisualPaths.ForNativeZone(StartingTiles.Parse(specification)));
    }
    [Theory]
    [InlineData("N:L.C")]
    [InlineData("E:T")]
    [InlineData("S:T")]
    [InlineData("W:L.A")]
    [InlineData("N:T.C")]
    [InlineData("S:T.A")]
    public void RequestedPreviewRoadsAlwaysConnectToHq(string specification)
    {
        var tile = StartingTiles.Parse(specification);
        Assert.NotEqual(CardinalPaths.None, tile.Paths & StartingTiles.HqFacing(tile.Position));
    }
    [Theory]
    [InlineData("N:L.C", CardinalPaths.S | CardinalPaths.E)]
    [InlineData("E:T", CardinalPaths.W | CardinalPaths.N | CardinalPaths.S)]
    [InlineData("S:T", CardinalPaths.N | CardinalPaths.E | CardinalPaths.W)]
    [InlineData("W:L.A", CardinalPaths.E | CardinalPaths.S)]
    [InlineData("N:T.C", CardinalPaths.S | CardinalPaths.N | CardinalPaths.E)]
    [InlineData("N:T.A", CardinalPaths.S | CardinalPaths.N | CardinalPaths.W)]
    public void PreviewUsesHqOutwardConvention(string specification, CardinalPaths expected)
    {
        var tile = StartingTiles.Parse(specification);
        Assert.Equal(expected, StartingTileVisualPaths.ForPreview(tile));
    }

}
