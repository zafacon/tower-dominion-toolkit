using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ProductionCapTests
{
    [Theory]
    [InlineData(150, 100, 100)]
    [InlineData(100, 100, 100)]
    [InlineData(60, 100, 60)]
    [InlineData(-10, 100, -10)]
    [InlineData(0, 0, 0)]
    public void CapsIncreasesButPreservesReductions(long current, long baseline, long expected)
    {
        Assert.Equal(expected, ProductionCap.Apply(current, baseline));
    }
}
