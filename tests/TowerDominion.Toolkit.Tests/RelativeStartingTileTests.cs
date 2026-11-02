using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public class RelativeStartingTileTests
{
    [Theory]
    [InlineData(StartPosition.N)]
    [InlineData(StartPosition.E)]
    [InlineData(StartPosition.S)]
    [InlineData(StartPosition.W)]
    public void D1MapsToOnlyActiveEntrance(StartPosition entrance)
    {
        var result = RelativeStartingTiles.Map(1, RelativeStartingTiles.Parse(1, "L.C"), new[] { entrance });
        Assert.Equal(entrance, Assert.Single(result).Key);
        Assert.Equal(StartShape.L, result[entrance].Shape);
    }

    [Theory]
    [InlineData(StartPosition.N, StartPosition.S)]
    [InlineData(StartPosition.W, StartPosition.E)]
    public void D2MapsOpposingPairIndependentOfInputOrder(StartPosition first, StartPosition second)
    {
        var shapes = RelativeStartingTiles.Parse(2, "S,T.A");
        var a = RelativeStartingTiles.Map(2, shapes, new[] { first, second });
        var b = RelativeStartingTiles.Map(2, shapes, new[] { second, first });
        Assert.Equal(a[first].Paths, b[first].Paths);
        Assert.Equal(a[second].Paths, b[second].Paths);
        Assert.Equal(2, a.Count);
    }

    [Theory]
    [InlineData(StartPosition.N, StartPosition.E, StartPosition.S, StartPosition.W)]
    [InlineData(StartPosition.E, StartPosition.S, StartPosition.W, StartPosition.N)]
    [InlineData(StartPosition.S, StartPosition.W, StartPosition.N, StartPosition.E)]
    [InlineData(StartPosition.W, StartPosition.N, StartPosition.E, StartPosition.S)]
    public void D3OrdersClockwiseFromMissingEntrance(StartPosition missing, StartPosition first, StartPosition second, StartPosition third)
    {
        var result = RelativeStartingTiles.Map(3, RelativeStartingTiles.Parse(3, "L.C,T.A,X"), new[] { third, first, second });
        Assert.False(result.ContainsKey(missing));
        Assert.Equal(StartShape.L, result[first].Shape);
        Assert.Equal(StartShape.T, result[second].Shape);
        Assert.Equal(StartShape.X, result[third].Shape);
    }

    [Fact]
    public void D4MapsAllFourEntrancesClockwiseIndependentOfInputOrder()
    {
        var shapes = RelativeStartingTiles.Parse(4, "L.C,T.A,X,S");
        var result = RelativeStartingTiles.Map(4, shapes, new[] { StartPosition.W, StartPosition.S, StartPosition.N, StartPosition.E });
        Assert.Equal(4, result.Count);
        Assert.Equal(StartShape.L, result[StartPosition.N].Shape);
        Assert.Equal(StartShape.T, result[StartPosition.E].Shape);
        Assert.Equal(StartShape.X, result[StartPosition.S].Shape);
        Assert.Equal(StartShape.S, result[StartPosition.W].Shape);
    }

    [Fact]
    public void RejectsInvalidCountsAndAdjacentD2()
    {
        Assert.Throws<ArgumentException>(() => RelativeStartingTiles.Parse(3, "S,T"));
        Assert.Throws<ArgumentException>(() => RelativeStartingTiles.Map(2, ["S", "X"], [StartPosition.N, StartPosition.E]));
    }
}
