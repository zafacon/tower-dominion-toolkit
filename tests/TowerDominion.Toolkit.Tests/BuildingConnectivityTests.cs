using static TowerDominion.Toolkit.Modules.BuildingConnectivity;

namespace TowerDominion.Toolkit.Tests;

public sealed class BuildingConnectivityTests
{
    private static readonly Cell[] Hq = [new(0, 0)];

    [Fact]
    public void OrthogonalContactWithHqAllowed() =>
        Assert.True(CanPlace([new(1, 0)], Hq, []));

    [Fact]
    public void DiagonalContactRejected() =>
        Assert.False(CanPlace([new(1, 1)], Hq, []));

    [Fact]
    public void DisconnectedBuildingsCannotAnchorPlacement() =>
        Assert.False(CanPlace([new(4, 0)], Hq, [[new Cell(3, 0)]]));

    [Fact]
    public void ConnectedChainAllowsExtensionRegardlessOfEnumerationOrder() =>
        Assert.True(CanPlace([new(3, 0)], Hq, [[new Cell(2, 0)], [new Cell(1, 0)]]));

    [Fact]
    public void MultiTileFootprintCanBridgeToNetwork() =>
        Assert.True(CanPlace([new(3, 1)], Hq, [[new Cell(1, 0), new Cell(2, 0), new Cell(3, 0)]]));

    [Fact]
    public void OverlapRejected() =>
        Assert.False(CanPlace([new(0, 0)], Hq, []));

    [Fact]
    public void OverlapWithDisconnectedBuildingRejected() =>
        Assert.False(CanPlace([new(1, 0)], Hq, [[new Cell(1, 0)]]));

    [Fact]
    public void MissingHeadquartersRejected() =>
        Assert.False(CanPlace([new(1, 0)], [], []));
}
