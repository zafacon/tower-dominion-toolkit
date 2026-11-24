using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class NativeRunSeedTests
{
    [Theory]
    [InlineData(0, 0UL)]
    [InlineData(1, 1UL)]
    [InlineData(int.MaxValue, 0x7FFFFFFFUL)]
    [InlineData(-1, 0xFFFFFFFFUL)]
    [InlineData(int.MinValue, 0x80000000UL)]
    public void PreservesNativeInt32BitPattern(int nativeSeed, ulong expectedRoot)
    {
        Assert.Equal(expectedRoot, NativeRunSeed.ToRootSeed(nativeSeed));
    }
}
